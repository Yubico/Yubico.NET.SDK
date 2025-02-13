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
    /// Gets a new PIN token.
    /// </summary>
    public class GetPinTokenCommand : IYubiKeyCommand<GetPinUvAuthTokenResponse>
    {
        private readonly ClientPinCommand _command;

        private const int SubCmdGetPinToken = 0x05;
        private const int MaximumPinLength = 63;
        private const int PinHashLength = 16;

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        // Note that there is no object-initializer constructor. All the
        // constructor inputs have no default or are secret byte arrays.
        private GetPinTokenCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="GetPinTokenCommand"/>.
        /// </summary>
        /// <remarks>
        /// The caller must specify which PIN protocol the command will use. This
        /// is done by passing in a subclass of <see cref="PinUvAuthProtocolBase"/>.
        /// This constructor requires the
        /// <see cref="PinUvAuthProtocolBase.Encapsulate"/> method to have been called
        /// before passing it in. Note that the <c>Encapsulate</c> method
        /// requires the YubiKey's public key, which is obtained by executing the
        /// <see cref="GetKeyAgreementCommand"/>.
        /// <para>
        /// In order to get the PIN token, the caller must supply the new PIN at
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
        public GetPinTokenCommand(PinUvAuthProtocolBase pinProtocol, ReadOnlyMemory<byte> currentPin)
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
                SubCommand = SubCmdGetPinToken,
                PinUvAuthProtocol = pinProtocol.Protocol,
                KeyAgreement = pinProtocol.PlatformPublicKey,
                PinHashEnc = encryptedPinHash,
            };
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public GetPinUvAuthTokenResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetPinUvAuthTokenResponse(responseApdu);
    }
}
