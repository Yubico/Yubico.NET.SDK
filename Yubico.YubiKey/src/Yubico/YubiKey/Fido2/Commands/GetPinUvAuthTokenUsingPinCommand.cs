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
using System.Globalization;
using System.Security.Cryptography;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.PinProtocols;


namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Gets a new PIN/UV Auth token using the subcommand
    /// <c>getPinUvAuthTokenUsingPinWithPermissions</c>.
    /// </summary>
    /// <remarks>
    /// Note that a YubiKey might not support this command. Sections 6.5.5.7.2,
    /// 6.5.5.7.3, and 6.4 in the FIDO2 standard describe the prerequisites for
    /// supporting this command. A program can determine if a YubiKey supports
    /// this command by getting device info (<see cref="GetInfoCommand"/> and
    /// checking the <c>Options</c> supported.
    /// </remarks>
    public class GetPinUvAuthTokenUsingPinCommand : IYubiKeyCommand<GetPinUvAuthTokenResponse>
    {
        private readonly ClientPinCommand _command;

        private const int SubCmdGetTokenUsingPin = 0x09;
        private const int MaximumPinLength = 63;
        private const int PinHashLength = 16;

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        // Note that there is no object-initializer constructor. All the
        // constructor inputs have no default or are secret byte arrays.
        private GetPinUvAuthTokenUsingPinCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs a new instance of
        /// <see cref="GetPinUvAuthTokenUsingPinCommand"/>.
        /// </summary>
        /// <remarks>
        /// The subcommand in the standard is called
        /// <c>getPinUvAuthTokenUsingPinWithPermissions</c>. The
        /// <c>WithPermissions</c> means the token will be associated with
        /// permissions. The caller must specify the permissions as a bit field,
        /// the <c>PinUvAuthTokenPermissions</c> enum. Note that with some
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
        /// <para>
        /// In order to get the token, the caller must supply the current PIN at
        /// construction. In this class, the PIN is supplied as
        /// <c>ReadOnlyMemory&lt;byte&gt;</c>. It is possible to pass a
        /// <c>byte[]</c>, because it will be automatically cast.
        /// </para>
        /// <para>
        /// This class will encrypt the PIN and will not copy a reference. That
        /// means you can overwrite the PIN in your byte array after calling the
        /// constructor.
        /// </para>
        /// </remarks>
        /// <param name="pinProtocol">
        /// An object defining the PIN protocol the command will use. The
        /// <see cref="PinUvAuthProtocolBase.Encapsulate"/> method must have been
        /// successfully executed before passing it to this constructor.
        /// </param>
        /// <param name="currentPin">
        /// The PIN. This is a byte array with the PIN provided as the UTF-8
        /// encoding of Unicode characters in Normalization Form C.
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
        /// <exception cref="ArgumentException">
        /// The PIN is an incorrect length.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The <c>pinProtocol</c> is in a state indicating <c>Encapsulate</c>
        /// has not executed.
        /// </exception>
        public GetPinUvAuthTokenUsingPinCommand(
            PinUvAuthProtocolBase pinProtocol,
            ReadOnlyMemory<byte> currentPin,
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
            if (currentPin.Length > MaximumPinLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidFido2Pin));
            }

            using var sha256Object = CryptographyProviders.Sha256Creator();
            byte[] pin = currentPin.ToArray();
            byte[] digest = sha256Object.ComputeHash(pin);
            CryptographicOperations.ZeroMemory(pin);
            byte[] encryptedPinHash = pinProtocol.Encrypt(digest, 0, PinHashLength);

            _command = new ClientPinCommand()
            {
                SubCommand = SubCmdGetTokenUsingPin,
                PinUvAuthProtocol = pinProtocol.Protocol,
                KeyAgreement = pinProtocol.PlatformPublicKey,
                PinHashEnc = encryptedPinHash,
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
