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
    /// Change the YubiKey's FIDO2 application PIN.
    /// </summary>
    /// <remarks>
    /// Upon manufacture, the YubiKey's FIDO2 application has no PIN set as there
    /// is no default PIN defined. Many FIDO2 operations are possible by simply
    /// inserting the YubiKey and possibly touching the contact.
    /// However, it is possible to set the application to require a PIN to
    /// perform many of the operations. Use <see cref="SetPinCommand"/> to set
    /// the PIN.
    /// <para>
    /// Use this command to change the PIN to a new value. Note that this command
    /// is possible only if a PIN is currently set. Note that it is not possible
    /// to remove a PIN, other than by resetting the entire application, which
    /// will mean losing all credentials as well as removing the PIN.
    /// </para>
    /// </remarks>
    public class ChangePinCommand : IYubiKeyCommand<ChangePinResponse>
    {
        private readonly ClientPinCommand _command;

        private const int SubCmdChangePin = 0x04;
        private const int MaximumPinLength = 63;
        private const int PinBlockLength = 64;
        private const int PinHashLength = 16;

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        // Note that there is no object-initializer constructor. All the
        // constructor inputs have no default or are secret byte arrays.
        private ChangePinCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="ChangePinCommand"/>.
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
        /// In order to change the PIN, the caller must supply both the current
        /// and new PINs at construction. In this class, the PINs are supplied as
        /// <c>ReadOnlyMemory&lt;byte&gt;</c>. It is possible to pass a
        /// <c>byte[]</c>, because it will be automatically cast.
        /// </para>
        /// <para>
        /// The standard specifies that the PIN must be "the UTF-8 representation
        /// of" the "Unicode characters in Normalization Form C." This
        /// constructor expects the PIN to already be in that representation. See
        /// the User's Manual entry on
        /// <xref href="TheFido2Pin"> the FIDO2 PIN</xref> for more information
        /// on what this means and how to build the PIN into the appropriate
        /// form. While this constructor will verify that the PIN is not too
        /// long, it will not verify the PIN is in the correct form. If it is
        /// invalid, the YubiKey might reject it and the response will indicate a
        /// failure.
        /// </para>
        /// <para>
        /// This class will encrypt the PINs and will not copy references. That
        /// means you can overwrite the PINs in your byte arrays after calling the
        /// constructor.
        /// </para>
        /// <para>
        /// The PIN is at least 4 unicode code points. If the YubiKey supports
        /// the "Set Minimum PIN Length" feature, it is possible to change this
        /// minimum to a bigger number, but never smaller than 4.
        /// </para>
        /// <para>
        /// Note that the minimum length is given in code points, not bytes. The
        /// PIN must be converted to a sequence of bytes representing the Unicode
        /// characters in Normalization Form C, then UTF-8 encoded.
        /// </para>
        /// <para>
        /// The maximum length is 63 bytes. This limit is in bytes, not code
        /// points. The standard also specifies that the last byte cannot be
        /// zero. Because the PIN must be UTF-8 encoded, this should never be an
        /// issue.
        /// </para>
        /// </remarks>
        /// <param name="pinProtocol">
        /// An object defining the PIN protocol the command will use. The
        /// <see cref="PinUvAuthProtocolBase.Encapsulate"/> method must have been
        /// successfully executed before passing it to this constructor.
        /// </param>
        /// <param name="currentPin">
        /// The current PIN that is to be changed. This is a byte array with the
        /// PIN provided as the UTF-8 encoding of Unicode characters in
        /// Normalization Form C.
        /// </param>
        /// <param name="newPin">
        /// The PIN to change to. This is a byte array with the PIN provided as
        /// the UTF-8 encoding of Unicode characters in Normalization Form C.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>pinProtocol</c> arg is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// A PIN is an incorrect length.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <c>pinProtocol</c> is in a state indicating <c>Encapsulate</c>
        /// has not executed.
        /// </exception>
        public ChangePinCommand(PinUvAuthProtocolBase pinProtocol, ReadOnlyMemory<byte> currentPin, ReadOnlyMemory<byte> newPin)
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
            if ((currentPin.Length > MaximumPinLength) || (newPin.Length > MaximumPinLength))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidFido2Pin));
            }

            using SHA256 sha256Object = CryptographyProviders.Sha256Creator();
            byte[] pin = currentPin.ToArray();
            byte[] digest = sha256Object.ComputeHash(pin);
            CryptographicOperations.ZeroMemory(pin);
            byte[] encryptedPinHash = pinProtocol.Encrypt(digest, 0, PinHashLength);

            byte[] dataToEncrypt = new byte[PinBlockLength];
            newPin.CopyTo(dataToEncrypt.AsMemory());
            byte[] encryptedPin = pinProtocol.Encrypt(dataToEncrypt, 0, dataToEncrypt.Length);
            CryptographicOperations.ZeroMemory(dataToEncrypt);

            byte[] dataToAuth = new byte[encryptedPin.Length + encryptedPinHash.Length];
            Array.Copy(encryptedPin, 0, dataToAuth, 0, encryptedPin.Length);
            Array.Copy(encryptedPinHash, 0, dataToAuth, encryptedPin.Length, encryptedPinHash.Length);
            byte[] pinAuthentication = pinProtocol.Authenticate(dataToAuth);

            _command = new ClientPinCommand()
            {
                SubCommand = SubCmdChangePin,
                PinUvAuthProtocol = pinProtocol.Protocol,
                KeyAgreement = pinProtocol.PlatformPublicKey,
                NewPinEnc = encryptedPin,
                PinHashEnc = encryptedPinHash,
                PinUvAuthParam = pinAuthentication,
            };
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public ChangePinResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ChangePinResponse(responseApdu);
    }
}

