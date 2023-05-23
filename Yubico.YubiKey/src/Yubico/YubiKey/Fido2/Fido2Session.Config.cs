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
        /// enabled, <c>false</c> otherwise.
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
                currentToken = GetAuthToken(false, PinUvAuthTokenPermissions.AuthenticatorConfiguration, null);
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
        /// "alwaysUv" and "makeCredUvNotReqd" options, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="Ctap2DataException">
        /// The YubiKey could not perform the operation, even though enterprise
        /// attestation is supported.
        /// </exception>
        public bool TryToggleAlwaysUv()
        {
            _log.LogInformation("Try to ToggleAlwaysUv.");

            OptionValue alwaysUvValue = AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.alwaysUv);
            if ((alwaysUvValue != OptionValue.True) && (alwaysUvValue != OptionValue.False))
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
                currentToken = GetAuthToken(false, PinUvAuthTokenPermissions.AuthenticatorConfiguration, null);
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
    }
}
