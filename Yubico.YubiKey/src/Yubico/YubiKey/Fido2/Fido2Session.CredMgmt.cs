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
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
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
        /// This method returns an instance of the
        /// <see cref="CredentialManagementData"/> class.
        /// </para>
        /// <para>
        /// Many credential management subcommands return data. The data
        /// returned as defined in the standard is represented in the SDK as
        /// <c>CredentialManagementData</c>. Each subcommand returns only a
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
            _log.LogInformation("Get credential metadata.");

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
        /// This method returns a list of <see cref="CredentialManagementData"/>
        /// objects. Each object contains information about one of the relying
        /// parties represented on the YubiKey. If there are no discoverable
        /// credentials on the YubiKey, then the list will have no elements
        /// (<c>Count</c> will be zero).
        /// </para>
        /// <para>
        /// Many credential management subcommands return data. The data
        /// returned as defined in the standard is represented in the SDK as
        /// <c>CredentialManagementData</c>. Each subcommand returns only a
        /// subset of all the data in this class.
        /// </para>
        /// <para>
        /// For <c>EnumerateRPs</c>, the data returned will be the properties
        /// <see cref="CredentialManagementData.RelyingParty"/>,
        /// <see cref="CredentialManagementData.RelyingPartyIdHash"/>,
        /// and possibly
        /// <see cref="CredentialManagementData.TotalRelyingPartyCount"/>.
        /// All other properties will be null. The <c>TotalRelyingPartyCount</c>
        /// property is returned only for the first relying party returned by the
        /// YubiKey. Hence, you will be able to find that number in the element
        /// at index zero of the returned list, but no other (the rest will be
        /// null). Of course, the total number of relying parties is also the
        /// list's Count.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A list of <c>CredentialManagementData</c> objects, one for each
        /// relying party. Only the <c>RelyingParty</c>,
        /// <c>RelyingPartyIdHash</c>, and <c>TotalRelyingPartyCount</c>
        /// properties will have values.
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
        public IReadOnlyList<CredentialManagementData> EnumerateRelyingParties()
        {
            _log.LogInformation("Enumerate relying parties.");

            ReadOnlyMemory<byte> currentToken = GetAuthToken(
                false, PinUvAuthTokenPermissions.CredentialManagement, null);

            var cmd = new EnumerateRpsBeginCommand(currentToken, AuthProtocol);
            CredentialManagementResponse rsp = Connection.SendCommand(cmd);

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
                    cmd = new EnumerateRpsBeginCommand(currentToken, AuthProtocol);
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
                return new List<CredentialManagementData>();
            }

            // This will return the data or throw an exception. We either have
            // the data, have an error other than PinAuthInvalid, or we do have
            // the error PinAuthInvalid but only after trying twice.
            CredentialManagementData mgmtData = rsp.GetData();

            // Get the count. The return from the Begin call has the total number
            // of RPs.
            int count = mgmtData.TotalRelyingPartyCount ?? 0;

            var returnValue = new List<CredentialManagementData>(count)
            {
                mgmtData
            };

            // Get the rest of the RPs. The EnumerateRpsGetNextCommand does not
            // need the AuthToken.
            var nextCmd = new EnumerateRpsGetNextCommand();
            for (int index = 1; index < count; index++)
            {
                rsp = Connection.SendCommand(nextCmd);
                mgmtData = rsp.GetData();
                returnValue.Add(mgmtData);
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
        /// This method returns a list of <see cref="CredentialManagementData"/>
        /// objects. Each object contains information about one of the
        /// discoverable credentials associated with the relying party on the
        /// YubiKey. If there are no discoverable credentials on the YubiKey,
        /// then the list will have no elements (<c>Count</c> will be zero).
        /// </para>
        /// <para>
        /// Many credential management subcommands return data. The data
        /// returned as defined in the standard is represented in the SDK as
        /// <c>CredentialManagementData</c>. Each subcommand returns only a
        /// subset of all the data in this class.
        /// </para>
        /// <para>
        /// For <c>EnumerateCredentials</c>, the data returned will be the
        /// properties <see cref="CredentialManagementData.User"/>,
        /// <see cref="CredentialManagementData.CredentialId"/>,
        /// <see cref="CredentialManagementData.CredentialPublicKey"/>,
        /// <see cref="CredentialManagementData.CredProtectPolicy"/>,
        /// <see cref="CredentialManagementData.LargeBlobKey"/>,
        /// and possibly
        /// <see cref="CredentialManagementData.TotalCredentialsForRelyingParty"/>.
        /// All other properties will be null. The
        /// <c>TotalCredentialsForRelyingParty</c> property is returned only for
        /// the first credential returned by the YubiKey. Hence, you will be able
        /// to find that number in the element at index zero of the returned
        /// list, but no other (for the rest the property will be null). Of
        /// course, the total number of credentials is also the list's Count.
        /// </para>
        /// </remarks>
        /// <param name="relyingPartyId">
        /// The relying party for which the list of credentials is requested.
        /// </param>
        /// <returns>
        /// A list of <c>CredentialManagementData</c> objects, one for each
        /// credential. Only the <c>User</c>, <c>CredentialId</c>,
        /// <c>CredentialPublicKey</c>, <c>CredProtectPolicy</c>,
        /// <c>LargeBlobKey</c>, and <c>TotalCredentialsForRelyingParty</c>
        /// properties will have values.
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
        public IReadOnlyList<CredentialManagementData> EnumerateCredentialsForRelyingParty(string relyingPartyId)
        {
            _log.LogInformation("Enumerate credentials for relying party: " + relyingPartyId + ".");

            ReadOnlyMemory<byte> currentToken = GetAuthToken(
                false, PinUvAuthTokenPermissions.CredentialManagement, relyingPartyId);

            using SHA256 digester = CryptographyProviders.Sha256Creator();
            digester.Initialize();
            byte[] utf = Encoding.UTF8.GetBytes(relyingPartyId);
            byte[] digest = digester.ComputeHash(utf);

            var cmd = new EnumerateCredentialsBeginCommand(digest, currentToken, AuthProtocol);
            CredentialManagementResponse rsp = Connection.SendCommand(cmd);

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
                    AuthTokenRelyingPartyId = relyingPartyId;
                }
                currentToken = GetAuthToken(true, PinUvAuthTokenPermissions.CredentialManagement, null);
                cmd = new EnumerateCredentialsBeginCommand(digest, currentToken, AuthProtocol);
                rsp = Connection.SendCommand(cmd);
            }

            // If the response is NoCredentials, return an empty list.
            if (rsp.CtapStatus == CtapStatus.NoCredentials)
            {
                return new List<CredentialManagementData>();
            }

            // This will return the data or throw an exception. We either have
            // the data, have an error other than PinAuthInvalid, or we do have
            // the error PinAuthInvalid but only after trying twice.
            CredentialManagementData mgmtData = rsp.GetData();

            // Get the count. The return from the Begin call has the total number
            // of RPs.
            int count = mgmtData.TotalCredentialsForRelyingParty ?? 0;

            var returnValue = new List<CredentialManagementData>(count)
            {
                mgmtData
            };

            // Get the rest of the credentials. The
            // EnumerateCredentialsGetNextCommand does not need the AuthToken.
            var nextCmd = new EnumerateCredentialsGetNextCommand();
            for (int index = 1; index < count; index++)
            {
                rsp = Connection.SendCommand(nextCmd);
                mgmtData = rsp.GetData();
                returnValue.Add(mgmtData);
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

            ReadOnlyMemory<byte> currentToken = GetAuthToken(
                false, PinUvAuthTokenPermissions.CredentialManagement, null);

            var cmd = new DeleteCredentialCommand(credentialId, currentToken, AuthProtocol);
            Fido2Response rsp = Connection.SendCommand(cmd);

            // If the error is PinAuthInvalid, try again.
            // If the result is not PinAuthInvalid, we know we're not going
            // to try again, error or no error.
            if (rsp.CtapStatus == CtapStatus.PinAuthInvalid)
            {
                currentToken = GetAuthToken(true, PinUvAuthTokenPermissions.CredentialManagement, null);
                cmd = new DeleteCredentialCommand(credentialId, currentToken, AuthProtocol);
                rsp = Connection.SendCommand(cmd);
            }

            // If the response is Success, we're done.
            if ((rsp.Status == ResponseStatus.Success) || (rsp.CtapStatus == CtapStatus.NoCredentials))
            {
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
            _log.LogInformation("Update user information");

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
    }
}
