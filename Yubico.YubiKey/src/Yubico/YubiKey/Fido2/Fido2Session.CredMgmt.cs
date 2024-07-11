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
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Yubico.Core.Logging;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2
{
    // This portion of the Fido2Session class deals with the Credential
    // Management operations.
    public sealed partial class Fido2Session
    {
        /// <summary>
        /// This performs the <c>getCredsMetadata</c> subcommand of the
        /// <c>authenticatorCredentialManagement</c> command. It gets
        /// metadata for all the credentials on the YubiKey.
        /// </summary>
        /// <remarks>
        /// See the <xref href="Fido2CredentialManagement">User's Manual entry</xref>
        /// on credential management.
        /// <para>
        /// This method returns a Tuple of two integers, the number of
        /// discoverable credentials and the number of "slots" remaining. The
        /// number of slots is the number of discoverable credentials the YubiKey
        /// can still hold.
        /// </para>
        /// <para>
        /// In order to execute, this method will need a PIN/UV auth param, which
        /// is built using an AuthToken, which itself is built from the PIN and the
        /// permissions, or UV and permissions. This method will need an
        /// AuthToken with the permission
        /// <see cref="PinUvAuthTokenPermissions.CredentialManagement"/>.
        /// </para>
        /// <para>
        /// If there is no <c>Fido2Session</c> property <see cref="AuthToken"/>,
        /// or it does not work (i.e. it is expired or does not have the
        /// appropriate permission), this method will use the <c>KeyCollector</c>
        /// to obtain a new one.
        /// </para>
        /// <para>
        /// If you do not want to use a KeyCollector, you must verify the PIN
        /// before calling, making sure the <c>CredentialManagement</c>
        /// permission is set. See
        /// <see cref="TryVerifyPin(ReadOnlyMemory{byte}, PinUvAuthTokenPermissions?, string?, out int?, out bool?)"/>
        /// <code language="csharp">
        ///   bool isVerified = fido2Session.TryVerifyPin(
        ///       currentPin, PinUvAuthTokenPermissions.CredentialManagement,
        ///       null, out int _, out bool _);
        /// </code>
        /// </para>
        /// </remarks>
        /// <returns>
        /// Two integers, the number of discoverable credentials in the YubiKey's
        /// FIDO2 application and the number of discoverable credentials for
        /// which it has space.
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
        public (int discoverableCredentialCount, int remainingCredentialCount) GetCredentialMetadata()
        {
            _log.LogInformation("Get credential metadata.");

            bool isPreview = CredMgmtGetIsPreview();

            ReadOnlyMemory<byte> currentToken = GetAuthToken(
                false, PinUvAuthTokenPermissions.CredentialManagement, null);

            var cmd = new GetCredentialMetadataCommand(currentToken, AuthProtocol)
            {
                IsPreview = isPreview
            };
            GetCredentialMetadataResponse rsp = Connection.SendCommand(cmd);

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
                    cmd = new GetCredentialMetadataCommand(currentToken, AuthProtocol)
                    {
                        IsPreview = isPreview
                    };
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

        /// <summary>
        /// This performs the <c>enumerateRPs</c> (Begin and GetNextRP)
        /// subcommands of the <c>authenticatorCredentialManagement</c> command.
        /// It gets a list of all the relying parties represented in all the
        /// discoverable credentials on the YubiKey.
        /// </summary>
        /// <remarks>
        /// See the <xref href="Fido2CredentialManagement">User's Manual entry</xref>
        /// on credential management.
        /// <para>
        /// This method returns a list of <see cref="RelyingParty"/> objects.
        /// Each object contains information about one of the relying parties
        /// represented on the YubiKey. If there are no discoverable credentials
        /// on the YubiKey, then the list will have no elements (<c>Count</c>
        /// will be zero).
        /// </para>
        /// <para>
        /// Note that other FIDO2 operations require the "RelyingPartyIdHash",
        /// which is one of the properties of the RelyingParty object.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A list of <c>RelyingParty</c> objects.
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
        public IReadOnlyList<RelyingParty> EnumerateRelyingParties()
        {
            _log.LogInformation("Enumerate relying parties.");

            bool isPreview = CredMgmtGetIsPreview();

            ReadOnlyMemory<byte> currentToken = GetAuthToken(
                false, PinUvAuthTokenPermissions.CredentialManagement, null);

            var cmd = new EnumerateRpsBeginCommand(currentToken, AuthProtocol)
            {
                IsPreview = isPreview
            };
            EnumerateRpsBeginResponse rsp = Connection.SendCommand(cmd);

            // If the error is PinAuthInvalid, try again.
            // If the result is not PinAuthInvalid, we know we're not going
            // to try again, error or no error.
            if (rsp.CtapStatus == CtapStatus.PinAuthInvalid)
            {
                // EnumerateRPs is an odd one. The standard says that the
                // RpId is optional with CredentialManagement. Except the
                // standard also says it is not possible to enumerate RPs if the
                // RpId is present. So to guarantee a null RpId when we get the
                // AuthToken now, yet have the RpId be the same as it was at the
                // beginning, we'll save the AuthTokenRelyingPartyId, set the
                // property to null, get the AuthToken, perform the operation,
                // then restore the AuthTokenRelyingPartyId. But that's not
                // enough. Suppose the current permissions include something that
                // requires RpId. If we just add "cm" we won't be able to get a
                // token, so we need to make sure the permissions only say "cm".
                // So we'll have to save (and then restore) the
                // AuthTokenPermissions as well.
                // Note that this method adds "cm", so make sure the original
                // restored includes this.
                PinUvAuthTokenPermissions? savePermissions = AuthTokenPermissions;
                string? saveRpId = AuthTokenRelyingPartyId;
                AuthTokenPermissions = null;
                AuthTokenRelyingPartyId = null;
                try
                {
                    currentToken = GetAuthToken(true, PinUvAuthTokenPermissions.CredentialManagement, null);
                    cmd = new EnumerateRpsBeginCommand(currentToken, AuthProtocol)
                    {
                        IsPreview = isPreview
                    };
                    rsp = Connection.SendCommand(cmd);
                }
                finally
                {
                    AuthTokenPermissions = savePermissions | PinUvAuthTokenPermissions.CredentialManagement;
                    AuthTokenRelyingPartyId = saveRpId;
                }
            }

            // If the response is NoCredentials, return an empty list.
            if (rsp.CtapStatus == CtapStatus.NoCredentials)
            {
                return new List<RelyingParty>();
            }

            // This will return the data or throw an exception. We either have
            // the data, have an error other than PinAuthInvalid, or we do have
            // the error PinAuthInvalid but only after trying twice.
            (int rpCount, RelyingParty firstRp) = rsp.GetData();

            var returnValue = new List<RelyingParty>(rpCount)
            {
                firstRp
            };

            // Get the rest of the RPs. The EnumerateRpsGetNextCommand does not
            // need the AuthToken.
            var nextCmd = new EnumerateRpsGetNextCommand()
            {
                IsPreview = isPreview
            };
            for (int index = 1; index < rpCount; index++)
            {
                EnumerateRpsGetNextResponse nextRsp = Connection.SendCommand(nextCmd);
                returnValue.Add(nextRsp.GetData());
            }

            return returnValue;
        }

        /// <summary>
        /// This performs the <c>enumerateCredentials</c> (Begin and
        /// GetNextCredential) subcommands of the
        /// <c>authenticatorCredentialManagement</c> command. It gets a list of
        /// all the credentials associated with a specified relying party.
        /// </summary>
        /// <remarks>
        /// See the <xref href="Fido2CredentialManagement">User's Manual entry</xref>
        /// on credential management.
        /// <para>
        /// This method returns a list of <see cref="CredentialUserInfo"/>
        /// objects. Each object contains information about one of the
        /// discoverable credentials associated with the specified relying party
        /// on the YubiKey. If there are no discoverable credentials on the
        /// YubiKey associated with the relying party, then the list will have no
        /// elements (<c>Count</c> will be zero).
        /// </para>
        /// <para>
        /// The <c>CredentialUserInfo</c> object contains properties for
        /// <see cref="CredentialUserInfo.CredentialId"/>,
        /// <see cref="CredentialUserInfo.CredentialPublicKey"/>,
        /// <see cref="CredentialUserInfo.CredProtectPolicy"/>, and possibly
        /// <see cref="CredentialUserInfo.LargeBlobKey"/>,
        /// </para>
        /// </remarks>
        /// <param name="relyingParty">
        /// The relying party for which the list of credentials is requested.
        /// </param>
        /// <returns>
        /// A list of <c>CredentialUserInfo</c> objects, one for each
        /// credential.
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
        public IReadOnlyList<CredentialUserInfo> EnumerateCredentialsForRelyingParty(RelyingParty relyingParty)
        {
            if (relyingParty is null)
            {
                throw new ArgumentNullException(nameof(relyingParty));
            }

            bool isPreview = CredMgmtGetIsPreview();

            _log.LogInformation("Enumerate credentials for relying party: " + relyingParty.Id + ".");

            ReadOnlyMemory<byte> currentToken = GetAuthToken(
                false, PinUvAuthTokenPermissions.CredentialManagement, relyingParty.Id);

            using SHA256 digester = CryptographyProviders.Sha256Creator();
            digester.Initialize();
            byte[] utf = Encoding.UTF8.GetBytes(relyingParty.Id);
            byte[] digest = digester.ComputeHash(utf);

            var cmd = new EnumerateCredentialsBeginCommand(relyingParty, currentToken, AuthProtocol)
            {
                IsPreview = isPreview
            };
            EnumerateCredentialsBeginResponse rsp = Connection.SendCommand(cmd);

            // If the error is PinAuthInvalid, try again.
            // If the result is not PinAuthInvalid, we know we're not going
            // to try again, error or no error.
            if (rsp.CtapStatus == CtapStatus.PinAuthInvalid)
            {
                // In order to enumerate the credentials, we need the relying
                // party. The standard specifies the RpIdHash as the way to
                // specify the RP. But there is also the possibility that the
                // permissions lists an RP. The standard says that the RpId is
                // optional with the CredentialManagement permission. However, if
                // it is given, the RP in the permissions must match the RP
                // specified as the RP of interest. If there's currently no RP in
                // the permissions, leave it blank. If there is, set it to what
                // the caller specified.
                if (!(AuthTokenRelyingPartyId is null))
                {
                    AuthTokenRelyingPartyId = relyingParty.Id;
                }
                currentToken = GetAuthToken(true, PinUvAuthTokenPermissions.CredentialManagement, null);
                cmd = new EnumerateCredentialsBeginCommand(relyingParty, currentToken, AuthProtocol)
                {
                    IsPreview = isPreview
                };
                rsp = Connection.SendCommand(cmd);
            }

            // If the response is NoCredentials, return an empty list.
            if (rsp.CtapStatus == CtapStatus.NoCredentials)
            {
                return new List<CredentialUserInfo>();
            }

            // This will return the data or throw an exception. We either have
            // the data, have an error other than PinAuthInvalid, or we do have
            // the error PinAuthInvalid but only after trying twice.
            (int credCount, CredentialUserInfo userInfo) = rsp.GetData();

            var returnValue = new List<CredentialUserInfo>(credCount)
            {
                userInfo
            };

            // Get the rest of the credentials. The
            // EnumerateCredentialsGetNextCommand does not need the AuthToken.
            var nextCmd = new EnumerateCredentialsGetNextCommand()
            {
                IsPreview = isPreview
            };
            for (int index = 1; index < credCount; index++)
            {
                EnumerateCredentialsGetNextResponse nextRsp = Connection.SendCommand(nextCmd);
                returnValue.Add(nextRsp.GetData());
            }

            return returnValue;
        }

        /// <summary>
        /// This performs the <c>deleteCredential</c> subcommand of the
        /// <c>authenticatorCredentialManagement</c> command. It deletes the one
        /// credential represented by the given <c>credentialId</c>.
        /// </summary>
        /// <remarks>
        /// See the <xref href="Fido2CredentialManagement">User's Manual entry</xref>
        /// on credential management.
        /// <para>
        /// If there is no credential with the given <c>credentialId</c> on the
        /// YubiKey, this method will do nothing.
        /// </para>
        /// </remarks>
        /// <param name="credentialId">
        /// The ID of the credential to delete.
        /// </param>
        /// <exception cref="Fido2Exception">
        /// The YubiKey was not able to delete the specified credential.
        /// </exception>
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
        public void DeleteCredential(CredentialId credentialId)
        {
            _log.LogInformation("Delete credential.");

            bool isPreview = CredMgmtGetIsPreview();

            ReadOnlyMemory<byte> currentToken = GetAuthToken(
                false, PinUvAuthTokenPermissions.CredentialManagement, null);

            var cmd = new DeleteCredentialCommand(credentialId, currentToken, AuthProtocol)
            {
                IsPreview = isPreview
            };
            Fido2Response rsp = Connection.SendCommand(cmd);

            // If the error is PinAuthInvalid, try again.
            // If the result is not PinAuthInvalid, we know we're not going
            // to try again, error or no error.
            if (rsp.CtapStatus == CtapStatus.PinAuthInvalid)
            {
                currentToken = GetAuthToken(true, PinUvAuthTokenPermissions.CredentialManagement, null);
                cmd = new DeleteCredentialCommand(credentialId, currentToken, AuthProtocol)
                {
                    IsPreview = isPreview
                };
                rsp = Connection.SendCommand(cmd);
            }

            // If the response is Success, we're done.
            if (rsp.Status == ResponseStatus.Success || rsp.CtapStatus == CtapStatus.NoCredentials)
            {
                // After a credential has been deleted, the number of
                // discoverable credentials can change. Hence, this operation can
                // change the AuthenticatorInfo, so make sure if someone gets it,
                // they get a new one.
                _authenticatorInfo = null;
                return;
            }

            // If the response is not Success, throw an exception.
            throw new Fido2Exception(rsp.StatusMessage);
        }

        /// <summary>
        /// This performs the <c>updateUserInformation</c> subcommand of the
        /// <c>authenticatorCredentialManagement</c> command. It replaces the
        /// user info in the credential represented by the given
        /// <c>credentialId</c> with the given user data.
        /// </summary>
        /// <remarks>
        /// See the <xref href="Fido2CredentialManagement">User's Manual entry</xref>
        /// on credential management.
        /// <para>
        /// This method will replace all the user information currently stored
        /// against the <c>credentialId</c> on the YubiKey. That is, it does not
        /// "edit" the information. Hence, the <c>userEntity</c> you supply
        /// should contain all the information you want stored, even if some of
        /// that information is currently stored on the YubiKey.
        /// </para>
        /// <para>
        /// If there is no credential with the given <c>credentialId</c> on the
        /// YubiKey, this method will throw an exception
        /// </para>
        /// </remarks>
        /// <param name="credentialId">
        /// The ID of the credential to update.
        /// </param>
        /// <param name="newUserInfo">
        /// An object containing the information that will replace the currently
        /// stored info.
        /// </param>
        /// <exception cref="Fido2Exception">
        /// There was no credential with the given ID.
        /// </exception>
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
        public void UpdateUserInfoForCredential(CredentialId credentialId, UserEntity newUserInfo)
        {
            _log.LogInformation("Update user information.");

            ReadOnlyMemory<byte> currentToken = GetAuthToken(
                false, PinUvAuthTokenPermissions.CredentialManagement, null);

            var cmd = new UpdateUserInfoCommand(credentialId, newUserInfo, currentToken, AuthProtocol);
            Fido2Response rsp = Connection.SendCommand(cmd);

            // If the error is PinAuthInvalid, try again.
            // If the result is not PinAuthInvalid, we know we're not going
            // to try again, error or no error.
            if (rsp.CtapStatus == CtapStatus.PinAuthInvalid)
            {
                currentToken = GetAuthToken(true, PinUvAuthTokenPermissions.CredentialManagement, null);
                cmd = new UpdateUserInfoCommand(credentialId, newUserInfo, currentToken, AuthProtocol);
                rsp = Connection.SendCommand(cmd);
            }

            // If the response is Success, we're done.
            if (rsp.Status == ResponseStatus.Success)
            {
                return;
            }

            // If the response is not Success, throw an exception.
            throw new Fido2Exception(rsp.StatusMessage);
        }

        // Determine if the YubiKey supports "credMgmt or "CredentialMgmtPreview".
        // If it supports "credMgmt", then return false.
        // If it supports only "CredentialMgmtPreview", return true.
        // If it supports neither, throw an exception.
        private bool CredMgmtGetIsPreview()
        {
            OptionValue cmValue = AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.credMgmt);
            if (cmValue == OptionValue.True)
            {
                return false;
            }

            cmValue = AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.credentialMgmtPreview);
            if (cmValue == OptionValue.True)
            {
                return true;
            }

            throw new NotSupportedException(ExceptionMessages.NotSupportedByYubiKeyVersion);
        }
    }
}
