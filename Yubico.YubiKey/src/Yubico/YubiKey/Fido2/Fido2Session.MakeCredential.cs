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
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2
{
    // This portion of the Fido2Session class deals with creating (making) a
    // credential on the YubiKey.
    public sealed partial class Fido2Session
    {
        /// <summary>
        /// Creates a FIDO2 credential on the YubiKey given a parameters object.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Detailed information about the parameters structure and its expected values can be found on
        /// the <see cref="MakeCredentialParameters"/> page.
        /// </para>
        /// <para>
        /// To make a credential requires "user presence", which for a YubiKey is
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
        /// </remarks>
        /// <param name="parameters">
        /// A fully populated <see cref="MakeCredentialParameters"/> structure that
        /// follows all of the rules set forth by that object.
        /// </param>
        /// <returns>
        /// An object containing all of the relevant information returned by the YubiKey
        /// after calling MakeCredential. This includes the public key for the credential
        /// itself, along with supporting information like the attestation statement and
        /// other authenticator data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="parameters"/> argument was null.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The YubiKey has not been authenticated. Call <see cref="VerifyPin"/> or <see cref="VerifyUv"/> before
        /// calling this method.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// The YubiKey either required touch for a user presence check or a biometric touch for user verification.
        /// The YubiKey timed out waiting for this action to be performed.
        /// </exception>
        public MakeCredentialData MakeCredential(MakeCredentialParameters parameters)
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
                    forceToken, PinUvAuthTokenPermissions.MakeCredential, parameters.RelyingParty.Id);

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

                MakeCredentialResponse rsp = RunMakeCredential(parameters, keyCollector);

                switch (rsp.CtapStatus)
                {
                    case CtapStatus.Ok:
                        return rsp.GetData();

                    case CtapStatus.PinAuthInvalid:
                        // If forceToken is false (its initial value), this
                        // will set it to true and we'll try the loop again,
                        // this time forcing a new AuthToken. If it is true,
                        // that means we have already tried once to get a new
                        // AuthToken, don't try again, so set forceToken to
                        // false and we'll break out of the for loop.
                        forceToken = !forceToken;
                        break;

                    case CtapStatus.OperationDenied:
                    case CtapStatus.ActionTimeout:
                        throw new TimeoutException(ExceptionMessages.Fido2TouchTimeout);

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

        private MakeCredentialResponse RunMakeCredential(
            MakeCredentialParameters parameters, Func<KeyEntryData, bool> keyCollector)
        {
            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.TouchRequest,
            };
            using var tokenSource = new CancellationTokenSource();
            var touchNotifyTask = Task.Run(() =>
                RunKeyCollectorThread(keyCollector, keyEntryData, tokenSource.Token), tokenSource.Token);

            try
            {
                return Connection.SendCommand(new MakeCredentialCommand(parameters));
            }
            finally
            {
                tokenSource.Cancel();
            }
        }
    }
}
