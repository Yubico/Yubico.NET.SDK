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
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Set the YubiKey's FIDO2 application to be PIN-protected.
    /// </summary>
    /// <remarks>
    /// Upon manufacture, the YubiKey's FIDO2 application has no PIN set as there
    /// is no default PIN defined. Any FIDO2 operation is possible by simply
    /// inserting the YubiKey and possibly touching the contact.
    /// <para>
    /// However, it is possible to set the application to require a PIN to
    /// perform many of the operations. Use this command to set the PIN.
    /// </para>
    /// <para>
    /// Note that this command is possible only if no PIN is currently set. To
    /// change a PIN, use the <see cref="ChangePinCommand"/>. To remove a PIN,
    /// reset the application. Note that resetting the application will mean
    /// losing all credentials as well as removing the PIN.
    /// </para>
    /// </remarks>
    public class SetPinCommand : IYubiKeyCommand<SetPinResponse>
    {
        private readonly ClientPinCommand _command;

        private const int SubCmdSetPin = 0x03;
        private const int MaximumPinLength = 63;
        private const int PinBlockLength = 64;

        /// <inheritdoc />
        public YubiKeyApplication Application => _command.Application;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        // Note that there is no object-initializer constructor. All the
        // constructor inputs have no default or are secret byte arrays.
        private SetPinCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="SetPinCommand"/>.
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
        /// In order to set the PIN, the caller must supply the new PIN at
        /// construction. In this class, the PIN is supplied as
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
        /// This class will encrypt the PIN and will not copy a reference. That
        /// means you can overwrite the PIN in your byte array after calling the
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
        /// <param name="newPin">
        /// The PIN to set. This is a byte array with the PIN provided as the
        /// UTF-8 encoding of Unicode characters in Normalization Form C.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>pinProtocol</c> arg is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The PIN is an incorrect length.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <c>pinProtocol</c> is in a state indicating <c>Encapsulate</c>
        /// has not executed.
        /// </exception>
        public SetPinCommand(PinUvAuthProtocolBase pinProtocol, ReadOnlyMemory<byte> newPin)
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
            if (newPin.Length > MaximumPinLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidFido2Pin));
            }

            byte[] dataToEncrypt = new byte[PinBlockLength];
            newPin.CopyTo(dataToEncrypt.AsMemory());

            byte[] encryptedPin = pinProtocol.Encrypt(dataToEncrypt, 0, dataToEncrypt.Length);
            CryptographicOperations.ZeroMemory(dataToEncrypt);
            byte[] pinAuthentication = pinProtocol.Authenticate(encryptedPin);

            _command = new ClientPinCommand()
            {
                SubCommand = SubCmdSetPin,
                PinUvAuthProtocol = pinProtocol.Protocol,
                KeyAgreement = pinProtocol.PlatformPublicKey,
                NewPinEnc = encryptedPin,
                PinUvAuthParam = pinAuthentication,
            };
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => _command.CreateCommandApdu();

        /// <inheritdoc />
        public SetPinResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new SetPinResponse(responseApdu);
    }
}
