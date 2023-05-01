// Copyright 2022 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2
{
    // This portion of the Fido2Session class deals with asserting (authenticating)
    // a credential stored on the YubiKey.
    public sealed partial class Fido2Session
    {
        /// <summary>
        /// Gets one or more assertions for a particular relying party.
        /// &gt; [!NOTE]
        /// &gt; You must supply a <see cref="GetAssertionParameters"/> object to
        /// &gt; this method, however, you do not need to set the
        /// &gt; <see cref="GetAssertionParameters.PinUvAuthParam"/> property,
        /// &gt; the SDK will do so.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Detailed information about the parameters structure and its expected values can be found on the
        /// <see cref="GetAssertionParameters"/> page.
        /// </para>
        /// <para>
        /// To get an assertion requires "user presence", which for a YubiKey is
        /// touch. This method will call the KeyCollector when touch is required
        /// (<c>KeyEntryRequest.TouchRequest</c>).
        /// </para>
        /// <para>
        /// The SDK will automatically perform PIN or user verification using the
        /// KeyCollector if needed. That is, if this method determines that
        /// authentication has been successfully completed, it will not need the
        /// PIN or fingerprint, so will not call the KeyCollector. However, if it
        /// needs to perform authentication, it will request user verification
        /// and/or a PIN using the KeyCollector.
        /// </para>
        /// <para>
        /// Note that because the SDK will make the appropriate authentication
        /// calls, it will build the <c>PinUvAuthParam</c> in the
        /// <c>GetAssertionParameters</c> input arg, so you do not need to do so.
        /// </para>
        /// <para>
        /// It is still possible to call this method with a KeyCollector that
        /// does not collect a PIN (you will need to have one that supports at
        /// least <c>KeyEntryRequest.TouchRequest</c>). You must simply make sure
        /// the appropriate Verify method has been called. See the User's Manual
        /// entries on <xref href="Fido2AuthTokens">AuthTokens</xref> and
        /// <xref href="SdkAuthTokenLogic">the SDK AuthToken logic</xref> for
        /// more information on when to verify. If you do not provide a
        /// KeyCollector that can collect the PIN, and the method is not able to
        /// perform because of an autentication failure, it will throw an
        /// exception.
        /// </para>
        /// <para>
        /// If there are no credentials associated with the relying party, this
        /// method will return a List with no entries (Count = 0).
        /// </para>
        /// </remarks>
        /// <param name="parameters">
        /// An appropriately populated <see cref="GetAssertionParameters"/> structure that
        /// follows all of the rules set forth by that object.
        /// </param>
        /// <returns>
        /// A collection of objects that contain the credential assertion and supporting data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="parameters"/> argument was null.
        /// </exception>
        /// <exception cref="Fido2Exception">
        /// The YubiKey could not complete the operation, likely because of a
        /// wrong PIN or fingerprint.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// The YubiKey either required touch for a user presence check or a biometric touch for user authentication.
        /// The YubiKey timed out waiting for this action to be performed.
        /// </exception>
        public IReadOnlyList<GetAssertionData> GetAssertions(GetAssertionParameters parameters)
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            Func<KeyEntryData, bool> keyCollector = EnsureKeyCollector();

            byte[] token = new byte[MaximumAuthTokenLength];
            byte[] clientDataHash = parameters.ClientDataHash.ToArray();
            bool forceToken = false;
            string message = "";

            do
            {
                // The first time through, forceToken will be false.
                // If there is a second time, it will be true.
                ReadOnlyMemory<byte> currentToken = GetAuthToken(
                    forceToken, PinUvAuthTokenPermissions.GetAssertion, parameters.RelyingParty.Id);

                try
                {
                    currentToken.CopyTo(token.AsMemory());
                    parameters.Protocol = AuthProtocol.Protocol;
                    parameters.PinUvAuthParam = AuthProtocol.AuthenticateUsingPinToken(
                        token, 0, currentToken.Length, clientDataHash);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(token);
                }

                // If the hmac-secret extension was not requested, this call will
                // do nothing.
                parameters.EncodeHmacSecretExtension(AuthProtocol);

                GetAssertionResponse rsp = RunGetAssertion(parameters, keyCollector, out CtapStatus ctapStatus);

                switch (ctapStatus)
                {
                    case CtapStatus.Ok:
                        return CompleteGetAssertions(rsp.GetData());

                    case CtapStatus.PinAuthInvalid:
                        // If forceToken is false (its initial value), this
                        // will set it to true and we'll try the loop again,
                        // this time forcing a new AuthToken. If it is true,
                        // that means we have already tried once to get a new
                        // AuthToken, don't try again, so set forceToken to
                        // false and we'll break out of the for loop.
                        forceToken = !forceToken;
                        break;

                    case CtapStatus.NoCredentials:
                        return new List<GetAssertionData>();

                    case CtapStatus.OperationDenied:
                    case CtapStatus.ActionTimeout:
                    case CtapStatus.UserActionTimeout:
                        throw new TimeoutException(ExceptionMessages.Fido2TouchTimeout);

                    case CtapStatus.VendorUserCancel:
                        throw new OperationCanceledException(ExceptionMessages.OperationCancelled);

                    default:
                        // Any other error, make sure we break out of the for
                        // loop.
                        forceToken = false;
                        break;
                }

                message = rsp.StatusMessage;
            } while (forceToken);

            throw new Fido2Exception(message);
        }

        private GetAssertionResponse RunGetAssertion(
            GetAssertionParameters parameters,
            Func<KeyEntryData, bool> keyCollector,
            out CtapStatus ctapStatus)
        {
            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.TouchRequest,
            };
            var touchTask = new TouchFingerprintTask(keyCollector, keyEntryData);

            try
            {
                GetAssertionResponse rsp = Connection.SendCommand(new GetAssertionCommand(parameters));
                ctapStatus = touchTask.IsUserCanceled ? CtapStatus.VendorUserCancel : rsp.CtapStatus;
                return rsp;
            }
            finally
            {
                keyEntryData.Request = KeyEntryRequest.Release;
                touchTask.SdkUpdate(keyEntryData);
            }
        }

        private IReadOnlyList<GetAssertionData> CompleteGetAssertions(GetAssertionData getAssertionData)
        {
            int numberOfCredentials = getAssertionData.NumberOfCredentials ?? 1;
            var assertions = new List<GetAssertionData>(numberOfCredentials) { getAssertionData };

            for (int index = 1; index < numberOfCredentials; index++)
            {
                GetAssertionResponse response = Connection.SendCommand(new GetNextAssertionCommand());
                assertions.Add(response.GetData());
            }

            return assertions;
        }
    }
}
