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
using Yubico.Core.Logging;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2
{
    // This portion of the Fido2Session class deals with the AuthenticatorConfig
    // operations.
    public sealed partial class Fido2Session
    {
        /// <summary>
        /// Try to set the YubiKey to enable enterprise attestation. If the
        /// YubiKey selected does not support enterprise attestation, this method
        /// will return <c>false</c>.
        /// </summary>
        /// <remarks>
        /// See the FIDO2 standard, section 7.1, for a discussion of enterprise
        /// attestation.
        /// <para>
        /// It is possible to enable enterprise attestation only if the "ep"
        /// option is present. If the "ep" option is not present, this method
        /// will return <c>false</c>.
        /// </para>
        /// <para>
        /// If the "ep" option is present, this method will make sure the value
        /// is <c>true</c>. That is, if "ep" is <c>false</c>, this will call on
        /// the YubiKey to set it to <c>true</c>. If "ep" is already <c>true</c>,
        /// after calling this method, the value will still be <c>true</c>.
        /// </para>
        /// <para>
        /// Note that once a YubiKey has been set to enable enterprise
        /// attestation, it is not possible to disable it, other than resetting
        /// the entire Fido2 application on the YubiKey.
        /// </para>
        /// <para>
        /// The enable enterprise attestation operation requires a PinUvAuthToken
        /// with permission "acfg" (Authenticator Configuration). If the "ep"
        /// option is present and <c>false</c>, this method will need the
        /// AuthToken. Otherwise ("ep" is present and <c>true</c> or "ep" is not
        /// supported), this method will not perform any operation that requires
        /// an AuthToken. If the method needs an AuthToken, it will get one using
        /// the KeyCollector. If you do not want to use a KeyCollector, make sure
        /// you verify the PIN or UV with the
        /// <see cref="Commands.PinUvAuthTokenPermissions.AuthenticatorConfiguration"/>
        /// permission before calling this method.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A boolean, <c>true</c> if the YubiKey now has enterprise attestation
        /// enabled, <c>false</c> if the YubiKey does not support this feature.
        /// </returns>
        /// <exception cref="Ctap2DataException">
        /// The YubiKey could not perform the operation, even though enterprise
        /// attestation is supported.
        /// </exception>
        public bool TryEnableEnterpriseAttestation()
        {
            _log.LogInformation("Try to EnableEnterpriseAttestation.");

            OptionValue epValue = AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.ep);

            if (epValue == OptionValue.True)
            {
                return true;
            }
            // Anything other than true or false means the operation is not supported.
            if (epValue != OptionValue.False)
            {
                return false;
            }

            ReadOnlyMemory<byte> currentToken = GetAuthToken(
                false,
                PinUvAuthTokenPermissions.AuthenticatorConfiguration,
                null);

            var enableCmd = new EnableEnterpriseAttestationCommand(currentToken, AuthProtocol);
            Fido2Response enableRsp = Connection.SendCommand(enableCmd);
            if (enableRsp.CtapStatus == CtapStatus.PinAuthInvalid)
            {
                currentToken = GetAuthToken(true, PinUvAuthTokenPermissions.AuthenticatorConfiguration, null);
                enableCmd = new EnableEnterpriseAttestationCommand(currentToken, AuthProtocol);
                enableRsp = Connection.SendCommand(enableCmd);
            }

            if (enableRsp.Status == ResponseStatus.Success)
            {
                // This operation can change the AuthenticatorInfo, so make sure
                // if someone gets it, they get a new one.
                _authenticatorInfo = null;
                return true;
            }

            throw new Ctap2DataException(enableRsp.StatusMessage);
        }

        /// <summary>
        /// Try to toggle the YubiKey's "alwaysUv" option (set to <c>false</c> if
        /// currently <c>true</c> or set to <c>true</c> if currently
        /// <c>false</c>. If the YubiKey selected does not support the "alwaysUv"
        /// option, this method will return <c>false</c>.
        /// </summary>
        /// <remarks>
        /// See the FIDO2 standard, section 7.2, for a discussion of the always
        /// UV feature.
        /// <para>
        /// It is possible to toggle the feature only if the "alwaysUv" option is
        /// present on the YubiKey. If the "alwaysUv" option is not present, this
        /// method will return <c>false</c>.
        /// </para>
        /// <para>
        /// This method will also toggle the "makeCredUvNotReqd" option as well.
        /// This option will almost certainly be the opposite of "alwaysUv".
        /// </para>
        /// <para>
        /// Before calling this method, you should check the "alwaysUv" option in
        /// the <see cref="AuthenticatorInfo"/>. If it is already <c>true</c> and
        /// you want it <c>true</c>, don't call this method.
        /// <code language="csharp">
        ///    OptionValue optionValue = fido2Session.AuthenticatorInfo.GetOptionValue(
        ///        AuthenticatorOptions.alwaysUv);
        ///    if (optionValue == OptionValue.False)
        ///    {
        ///        _ = fido2Session.TryToggleAlwaysUv();
        ///    }
        /// </code>
        /// </para>
        /// <para>
        /// The enable toggle always UV operation requires a PinUvAuthToken with
        /// permission "acfg" (Authenticator Configuration). If the "alwaysUv"
        /// option is present, this method will need the AuthToken. If the method
        /// needs an AuthToken, it will get one using the KeyCollector. If you do
        /// not want to use a KeyCollector, make sure you verify the PIN or UV
        /// with thee
        /// <see cref="Commands.PinUvAuthTokenPermissions.AuthenticatorConfiguration"/>
        /// permission before calling this method.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A boolean, <c>true</c> if the YubiKey was able to toggle the
        /// "alwaysUv" and "makeCredUvNotReqd" options, <c>false</c> if the
        /// YubiKey does not support this feature.
        /// </returns>
        /// <exception cref="Ctap2DataException">
        /// The YubiKey could not perform the operation, even though the always
        /// UV toggle feature is supported.
        /// </exception>
        public bool TryToggleAlwaysUv()
        {
            _log.LogInformation("Try to ToggleAlwaysUv.");

            OptionValue alwaysUvValue = AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.alwaysUv);
            if (alwaysUvValue != OptionValue.True && alwaysUvValue != OptionValue.False)
            {
                return false;
            }

            ReadOnlyMemory<byte> currentToken = GetAuthToken(
                false,
                PinUvAuthTokenPermissions.AuthenticatorConfiguration,
                null);

            var toggleCmd = new ToggleAlwaysUvCommand(currentToken, AuthProtocol);
            Fido2Response toggleRsp = Connection.SendCommand(toggleCmd);
            if (toggleRsp.CtapStatus == CtapStatus.PinAuthInvalid)
            {
                currentToken = GetAuthToken(true, PinUvAuthTokenPermissions.AuthenticatorConfiguration, null);
                toggleCmd = new ToggleAlwaysUvCommand(currentToken, AuthProtocol);
                toggleRsp = Connection.SendCommand(toggleCmd);
            }

            if (toggleRsp.Status == ResponseStatus.Success)
            {
                // This operation can change the AuthenticatorInfo, so make sure
                // if someone gets it, they get a new one.
                _authenticatorInfo = null;
                return true;
            }

            throw new Ctap2DataException(toggleRsp.StatusMessage);
        }

        /// <summary>
        /// Perform the <c>authenticatorConfig</c> subcommand of
        /// <c>setMinPINLength</c>, which will set the minimum PIN length, and/or
        /// replace the list of relying parties that are allowed to see the
        /// minimum PIN length, and/or specify that the user must change the PIN.
        /// </summary>
        /// <remarks>
        /// This method will perform the operation only if the "setMinPINLength"
        /// option is present and set to <c>true</c>. Otherwise, it will return
        /// <c>false</c>
        /// <para>
        /// There are up to three elements to set with this command: a new
        /// minimum PIN length, a new list of relying party IDs, and an
        /// indication to require the user change the PIN. All three are
        /// optional, although this command will do nothing if none are set (i.e.
        /// the three args are <c>null, null, null</c>).
        /// </para>
        /// <para>
        /// If you want to set an element, provide a value for the corresponding
        /// argument, otherwise, pass in null.
        /// </para>
        /// <para>
        /// If you want to force a PIN change, pass in <c>true</c> for the
        /// <c>forceChangePin</c> arg. If you pass in <c>false</c>, this class
        /// will consider it the same as null. That is, the <c>forceChangePin</c>
        /// element of this command is optional, meaning the command does not
        /// need to include the element (i.e. leave it blank in the command sent
        /// to the YubiKey). If you pass in <c>false</c>, this method will send
        /// the command without that element (i.e., it will be left blank).
        /// </para>
        /// <para>
        /// The YubiKey's FIDO2 application is manufactured with a minimum PIN
        /// length. Users that want a different length can call this command.
        /// However, it is not possible to set the minimum PIN length to a value
        /// less than the current minimum. The only way to possibly set a shorter
        /// minimum PIN length is to reset the entire FIDO2 application on the
        /// given YubiKey. Even then, after reset, the minimum PIN will be the
        /// default length with which the YubiKey was originally manufactured.
        /// </para>
        /// <para>
        /// The PIN length is measured in code points. See the User's Manual entry on
        /// <xref href="TheFido2Pin">the FIDO2 PIN</xref> for more information on PIN
        /// composition.
        /// </para>
        /// <para>
        /// Note that the standard specifies that a PIN cannot be less than 4 Unicode
        /// characters and no more than 63 bytes when encoded as UTF-8. Hence, there
        /// are limits to the new minimum PIN length.
        /// </para>
        /// <para>
        /// The list of RP IDs will specify that any RP on the list is allowed to see
        /// the minimum PIN length of a YubiKey. That will be visible only during the
        /// MakeCredential process. Generally, it is used so that an RP will
        /// refuse to provide a credential to an authenticator if the minimum PIN
        /// length is too low.
        /// </para>
        /// <para>
        /// It is possible for a YubiKey to be manufactured with a pre-configured
        /// list of RP IDs. That list will never change, even after reset. If RP IDs
        /// are added using the SetMinPINLength command, they will be IDs in addition
        /// to the pre-configured list.
        /// </para>
        /// <para>
        /// If RP IDs are added using this command, they will replace any RP IDs that
        /// had been added during a previous call to this command. It will not
        /// replace the pre-configured list. Note that there is no way to get the
        /// current list.
        /// </para>
        /// <para>
        /// If the minimum PIN length is set, and if the current PIN is smaller than
        /// this value, then the YubiKey will require the user to change the PIN. It
        /// will not verify the current PIN and any operation that requires
        /// PIN verification will fail until the PIN is changed to a value that meets
        /// the new requirement. For example, suppose the current minimum PIN length
        /// is 4 and you have a PIN of length 6. You set the minimum PIN length to 7,
        /// but do not set <c>forceChangePin</c> (you pass in null for that arg). The
        /// YubiKey will still require the user change the PIN.
        /// </para>
        /// <para>
        /// If <c>forceChangePin</c> is true, then the caller is requiring the user
        /// to change the PIN, no matter what.
        /// </para>
        /// <para>
        /// You can know if a PIN must be changed (either because the min PIN length
        /// is now longer than the existing PIN or the <c>forceChangePin</c> was
        /// set), look at the <see cref="AuthenticatorInfo.ForcePinChange"/> property
        /// in the <c>AuthenticatorInfo</c>.
        /// </para>
        /// <para>
        /// Note that if you pass in null for all three arguments, this method
        /// will still check to see if the feature is supported and return
        /// <c>false</c> if it is not. If the feature is supported, this method
        /// will do nothing.
        /// </para>
        /// </remarks>
        /// <param name="newMinPinLength">
        /// The new PIN length, measured in code points. See the User's Manual
        /// entry on <xref href="TheFido2Pin">the FIDO2 PIN</xref> for more
        /// information on PIN composition. Pass in null to indicate the command
        /// should not change the minimum PIN length.
        /// </param>
        /// <param name="relyingPartyIds">
        /// A list of strings that are the relying party IDs for those RPs that
        /// are allowed to see the minimum PIN length. Pass in null to indicate
        /// the command should not add any RP IDs.
        /// </param>
        /// <param name="forceChangePin">
        /// If you want to set the YubiKey to require the user change the PIN
        /// before the next verification event, pass in <c>true</c>. If you pass
        /// in null or <c>false</c>, this command will consider the force PIN
        /// option not taken.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the YubiKey was able to set the YubiKey
        /// with the given input data,  <c>false</c> if the YubiKey does not
        /// support this feature.
        /// </returns>
        /// <exception cref="Ctap2DataException">
        /// The YubiKey could not perform the operation, even though the set min
        /// PIN length feature is supported. For example, if the input
        /// newMinPinLength arg is less than the current min PIN length.
        /// </exception>
        public bool TrySetPinConfig(
            int? newMinPinLength = null,
            IReadOnlyList<string>? relyingPartyIds = null,
            bool? forceChangePin = null)
        {
            _log.LogInformation("Try to set the PIN config (setMinPINLength).");

            OptionValue setMinPinValue = AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.setMinPINLength);

            if (setMinPinValue != OptionValue.True)
            {
                return false;
            }

            ReadOnlyMemory<byte> currentToken = GetAuthToken(
                false,
                PinUvAuthTokenPermissions.AuthenticatorConfiguration,
                null);

            var setCmd = new SetMinPinLengthCommand(
                newMinPinLength,
                relyingPartyIds,
                forceChangePin,
                currentToken,
                AuthProtocol);
            Fido2Response setRsp = Connection.SendCommand(setCmd);
            if (setRsp.CtapStatus == CtapStatus.PinAuthInvalid)
            {
                currentToken = GetAuthToken(true, PinUvAuthTokenPermissions.AuthenticatorConfiguration, null);
                setCmd = new SetMinPinLengthCommand(
                    newMinPinLength,
                    relyingPartyIds,
                    forceChangePin,
                    currentToken,
                    AuthProtocol);
                setRsp = Connection.SendCommand(setCmd);
            }

            if (setRsp.Status == ResponseStatus.Success)
            {
                // This operation can change the AuthenticatorInfo, so make sure
                // if someone gets it, they get a new one.
                _authenticatorInfo = null;
                return true;
            }

            throw new Ctap2DataException(setRsp.StatusMessage);
        }
    }
}
