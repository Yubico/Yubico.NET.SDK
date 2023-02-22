// Copyright 2023 Yubico AB
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
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2
{
    // This portion of the Fido2Session class deals with the Credential
    // Management operations.
    public sealed partial class Fido2Session
    {
        /// <summary>
        /// This performs the <c>getCredsMetadata</c> sub command of the
        /// <c>authenticatorCredentialManagement</c> command. It gets
        /// metadata for all the credentials on the YubiKey.
        /// </summary>
        /// <remarks>
        /// See the <xref href="Fido2CredentialManagement">User's Manual entry</xref>
        /// on credential management.
        /// <para>
        /// This method returns an instance of the
        /// <see cref="CredentialManagementData"/> class.
        /// </para>
        /// <para>
        /// Many credential management sub commands return data. The data
        /// returned as defined in the standard is represented in the SDK as
        /// <c>CredentialManagementData</c>. Each sub command returns only a
        /// subset of all the data in this class.
        /// </para>
        /// <para>
        /// For <c>CredentialMetadata</c>, the data returned will be the
        /// properties
        /// <see cref="CredentialManagementData.NumberOfDiscoverableCredentials"/>
        /// and <see cref="CredentialManagementData.RemainingCredentialCount"/>.
        /// All other properties will be null.
        /// </para>
        /// <para>
        /// Note that the <c>NumberOfDiscoverableCredentials</c> and the
        /// <c>RemainingCredentialCount</c> might not add up to the total number
        /// of available FIDO2 slots on the YubiKey. It is possible that there
        /// are non-discoverable credentials on the YubiKey, which will not be
        /// reflected in the <c>NumberOfDiscoverableCredentials</c>, but will
        /// count towards the total number of credentials. A discoverable
        /// credential was created with the "rk" option set to <c>true</c>.
        /// </para>
        /// <para>
        /// In order to execute, this method will need a PIN/UV auth param, which
        /// is built using an AuthToken, which itself is built from the PIN and the
        /// permissions, or UV and permissions. This method will need an
        /// AuthToken with the permission
        /// <see cref="PinUvAuthTokenPermissions.CredentialManagement"/>.
        /// </para>
        /// <para>
        /// The <c>Fido2Session</c> property <see cref="AuthToken"/> is the
        /// AuthToken this method will use, but will work only if the
        /// <see cref="AuthTokenPermissions"/> property has the
        /// <c>CredentialManagement</c> flag.
        /// </para>
        /// <para>
        /// If that permission is not set, you must verify the PIN before
        /// calling, making sure the <c>CredentialManagement</c> permission is
        /// set. See
        /// <see cref="TryVerifyPin(ReadOnlyMemory{byte}, PinUvAuthTokenPermissions?, string?, out int?, out bool?)"/>
        /// <code language="csharp">
        ///   bool isVerified = fido2Session.TryVerifyPin(
        ///       currentPin, PinUvAuthTokenPermissions.CredentialManagement,
        ///       null, out int _, out bool _);
        /// </code>
        /// </para>
        /// </remarks>
        /// <returns>
        /// A new instance of the <c>CredentialManagementData</c> class. Only the
        /// <c>NumberOfDiscoverableCredentials</c> and
        /// <c>RemainingCredentialCount</c> will have values.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The connected YubiKey does not support CredentialManagement, or the
        /// PIN was invalid, or there was no KeyCollector.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled the operation while collecting the PIN.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// The PIN retry count was exhausted.
        /// </exception>
        public CredentialManagementData GetCredentialMetadata()
        {
            ReadOnlyMemory<byte> currentToken = GetAuthToken(
                false, PinUvAuthTokenPermissions.CredentialManagement, null);

            var cmd = new GetCredentialMetadataCommand(currentToken, AuthProtocol);
            CredentialManagementResponse rsp = Connection.SendCommand(cmd);

            // If the error is PinAuthInvalid, try again.
            // If the result is not PinAuthInvalid, we know we're not going
            // to try again, error or no error.
            if (rsp.CtapStatus == CtapStatus.PinAuthInvalid)
            {
                // Get Metadata is an odd one. The standard says that the RpId is
                // optional with CredentialManagement. Except the standard also
                // says it is not possible to get metadata if the RpId is
                // present. So to guarantee a null RpId when we get the AuthToken
                // now, yet have the RpId be the same as it was at the beginning,
                // we'll save the AuthTokenRelyingPartyId, set the property to
                // null, get the AuthToken, perform the operation, then restore
                // the AuthTokenRelyingPartyId. But that's not enough. Suppose
                // the current permissions include something that requires RpId.
                // If we just add "cm" we won't be able to get a token, so we
                // need to make sure the permissions only say "cm". So we'll have
                // to save (and then restore) the AuthTokenPermissions as well.
                // Note that this method adds "cm", so make sure the original
                // restored includes this.
                PinUvAuthTokenPermissions? savePermissions = AuthTokenPermissions;
                string? saveRpId = AuthTokenRelyingPartyId;
                AuthTokenPermissions = null;
                AuthTokenRelyingPartyId = null;
                try
                {
                    currentToken = GetAuthToken(true, PinUvAuthTokenPermissions.CredentialManagement, null);
                    cmd = new GetCredentialMetadataCommand(currentToken, AuthProtocol);
                    rsp = Connection.SendCommand(cmd);
                }
                finally
                {
                    AuthTokenPermissions = savePermissions | PinUvAuthTokenPermissions.CredentialManagement;
                    AuthTokenRelyingPartyId = saveRpId;
                }
            }

            // This will return the data or throw an exception. We either have
            // the data, have an error other than PinAuthInvalid, or we do have
            // the error PinAuthInvalid but only after trying twice.
            return rsp.GetData();
        }
    }
}
