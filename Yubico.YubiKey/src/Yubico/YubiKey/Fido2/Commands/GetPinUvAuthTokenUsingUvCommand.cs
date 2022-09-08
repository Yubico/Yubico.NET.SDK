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
using System.Security.Cryptography;
using System.Globalization;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.PinProtocols;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Gets a new PIN/UV Auth token using
    /// <c>getPinUvAuthTokenUsingUvWithPermissions</c>.
    /// </summary>
    /// <remarks>
    /// Note that a YubiKey might not support this command. Sections 6.5.5.7.2,
    /// 6.5.5.7.3, and 6.4 in the FIDO2 standard describe the prerequisites for
    /// supporting this command. A program can determine if a YubiKey supports
    /// this command by getting device info (<see cref="GetInfoCommand"/> and
    /// checking the <c>Options</c> supported.
    /// </remarks>
    public class GetPinUvAuthTokenUsingUvCommand : IYubiKeyCommand<GetPinUvAuthTokenResponse>
    {
        private readonly ClientPinCommand _command;

        private const int SubCmdGetTokenUsingUv = 0x06;

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        // Note that there is no object-initializer constructor. All the
        // constructor inputs have no default or are secret byte arrays.
        private GetPinUvAuthTokenUsingUvCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="GetPinUvAuthTokenUsingUvCommand"/>.
        /// </summary>
        /// <remarks>
        /// The subcommand in the standard is called
        /// <c>getPinUvAuthTokenUsingUvWithPermissions</c>. The <c>UsingUv</c>
        /// means the authentication is built into the YubiKey, such as the
        /// YubiKey Bio series (fingerprint). Note that PIN verification is
        /// available on all YubiKeys that support FIDO2, including the YubiKey
        /// Bio Series. <c>WithPermissions</c> means the token will be associated
        /// with permissions. The caller must specify the permissions as a bit
        /// field, the <c>PinUvAuthTokenPermissions</c> enum. Note that with some
        /// permissions, the relying party ID (<c>rpId</c>) is required as well.
        /// For other permissions it is optional or ignored, so the <c>rpId</c>
        /// arg can be null.
        /// <para>
        /// The caller must specify which PIN protocol the command will use. This
        /// is done by passing in a subclass of <see cref="PinUvAuthProtocolBase"/>.
        /// This constructor requires the
        /// <see cref="PinUvAuthProtocolBase.Encapsulate"/> method to have been called
        /// before passing it in. Note that the <c>Encapsulate</c> method
        /// requires the YubiKey's public key, which is obtained by executing the
        /// <see cref="GetKeyAgreementCommand"/>.
        /// </para>
        /// </remarks>
        /// <param name="pinProtocol">
        /// An object defining the PIN protocol the command will use. The
        /// <see cref="PinUvAuthProtocolBase.Encapsulate"/> method must have been
        /// successfully executed before passing it to this constructor.
        /// </param>
        /// <param name="permissions">
        /// All the permissions necessary to complete the operations intended.
        /// </param>
        /// <param name="rpId">
        /// If at least one of the permissions chosen requires it or is optional
        /// and the feature is intended, supply it here. Otherwise, pass in null.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>pinProtocol</c> arg is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The <c>pinProtocol</c> is in a state indicating <c>Encapsulate</c>
        /// has not executed.
        /// </exception>
        public GetPinUvAuthTokenUsingUvCommand(
            PinUvAuthProtocolBase pinProtocol,
            PinUvAuthTokenPermissions permissions,
            string? rpId)
        {
            if (pinProtocol is null)
            {
                throw new ArgumentNullException(nameof(pinProtocol));
            }
            if (pinProtocol.PlatformPublicKey is null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidCallOrder));
            }

            _command = new ClientPinCommand()
            {
                SubCommand = SubCmdGetTokenUsingUv,
                PinUvAuthProtocol = pinProtocol.Protocol,
                KeyAgreement = pinProtocol.PlatformPublicKey,
                Permissions = permissions,
                RpId = rpId,
            };
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public GetPinUvAuthTokenResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetPinUvAuthTokenResponse(responseApdu);
    }
}
