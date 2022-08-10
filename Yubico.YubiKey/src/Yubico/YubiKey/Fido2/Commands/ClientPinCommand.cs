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
using System.Formats.Cbor;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// The <see cref="ClientPinCommand"/> allows a client or platform to use a PIN/UV auth protocol to
    /// perform a number of actions such as authenticating the PIN, setting and changing the PIN, and getting the number
    /// of PIN retries left.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The `authenticatorClientPin (0x06)` FIDO2 command can be thought of more as a "meta" command. That is, it provides
    /// the structure and mechanism for performing a number of sub-commands. These sub-commands are:
    ///
    /// - GetPinRetries (0x01)
    /// - GetKeyAgreement (0x02)
    /// - SetPIN (0x03)
    /// - ChangePIN (0x04)
    /// - GetPinToken (0x05)
    /// - GetPinUvAuthTokenUsingUvWithPermissions (0x06)
    /// - GetUVRetries (0x07)
    /// - GetPinUvAuthTokenUsingPinWithPermissions (0x09)
    ///
    /// Since the SDK does not have the concept of a sub-command natively, these are all exposed as their own separate
    /// commands.
    /// </para>
    /// <para>
    /// This command should seldom be used directly. It is exposed for completeness. The sub-commands exposed in this
    /// namespace use it as their implementation and expose a pared down version of the parameters.
    /// </para>
    /// <para>
    /// See the user manual entry on <xref href="Fido2PinProtocol">PIN protocols</xref> for a much more in depth guide
    /// to working with PINs within FIDO2. For more information on a particular sub-command, see the API reference
    /// documentation for that command class (linked above).
    /// </para>
    /// </remarks>
    public class ClientPinCommand : IYubiKeyCommand<IYubiKeyResponse>
    {
        // Command constants
        private const byte CmdAuthenticatorClientPin = 0x06;

        private const int TagPinUvAuthProtocol = 0x01;
        private const int TagSubCommand = 0x02;
        private const int TagKeyAgreement = 0x03;
        private const int TagPinUvAuthParam = 0x04;
        private const int TagNewPinEnc = 0x05;
        private const int TagPinHashEnc = 0x06;
        private const int TagPermissions = 0x09;
        private const int TagRpId = 0x0A;

        /// <inheritdoc />
        public YubiKeyApplication Application => YubiKeyApplication.Fido2;

        /// <summary>
        /// An optional PIN/UV protocol version chosen by the platform.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A PIN/UV protocol must be used when working with a PIN. The specified protocol must be one of the protocols
        /// that are supported by the YubiKey. This can be determined by issuing the AuthenticatorGetInfo command.
        /// </para>
        /// <para>
        /// This parameter is optional for the GetPinRetries and GetUvRetries sub-commands, and is mandatory for all
        /// others.
        /// </para>
        /// </remarks>
        public PinUvAuthProtocol? PinUvAuthProtocol { get; set; }

        /// <summary>
        /// The Client PIN sub-command to issue to the YubiKey.
        /// </summary>
        /// <remarks>
        /// This is a mandatory parameter, and must be one of the following values:
        ///
        /// - GetPinRetries (0x01)
        /// - GetKeyAgreement (0x02)
        /// - SetPIN (0x03)
        /// - ChangePIN (0x04)
        /// - GetPinToken (0x05)
        /// - GetPinUvAuthTokenUsingUvWithPermissions (0x06)
        /// - GetUVRetries (0x07)
        /// - GetPinUvAuthTokenUsingPinWithPermissions (0x09)
        ///
        /// Alternatively - you can use one of the command classes exposed by the SDK that represents the sub-command
        /// itself. This method is recommended as these command classes will only expose the parameters that are
        /// relevant to that sub-command.
        /// </remarks>
        public int SubCommand { get; set; }

        /// <summary>
        /// The platform key-agreement key.
        /// </summary>
        /// <remarks>
        /// This is a COSE_Key-encoded public key, derived using the current PIN/UV protocol in use. See the
        /// user manual entry on <xref href="Fido2PinProtocol">PIN protocols</xref> for a much more in depth guide
        /// to working with PINs within FIDO2.
        /// </remarks>
        public ReadOnlyMemory<byte>? KeyAgreement { get; set; }

        /// <summary>
        /// The output of calling authenticate on the PIN/UV protocol specific to a particular sub-command.
        /// </summary>
        /// <remarks>
        /// See the user manual entry on <xref href="Fido2PinProtocol">PIN protocols</xref> for a much more in depth
        /// guide to working with PINs within FIDO2.
        /// </remarks>
        public ReadOnlyMemory<byte>? PinUvAuthParam { get; set; }

        /// <summary>
        /// An encrypted PIN.
        /// </summary>
        /// <remarks>
        /// See the user manual entry on <xref href="Fido2PinProtocol">PIN protocols</xref> for a much more in depth
        /// guide to working with PINs within FIDO2.
        /// </remarks>
        public ReadOnlyMemory<byte>? NewPinEnc { get; set; }

        /// <summary>
        /// An encrypted proof-of-knowledge of a PIN.
        /// </summary>
        /// <remarks>
        /// See the user manual entry on <xref href="Fido2PinProtocol">PIN protocols</xref> for a much more in depth
        /// guide to working with PINs within FIDO2.
        /// </remarks>
        public ReadOnlyMemory<byte>? PinHashEnc { get; set; }

        /// <summary>
        /// A set of permission flags. If present, it must not be zero.
        /// </summary>
        /// <remarks>
        /// See the user manual entry on <xref href="Fido2PinProtocol">PIN protocols</xref> for a much more in depth
        /// guide to working with PINs within FIDO2.
        /// </remarks>
        public PinUvAuthTokenPermissions? Permissions { get; set; }

        /// <summary>
        /// The Relying Party ID (RP ID) to assign as the permissions RP ID.
        /// </summary>
        /// <remarks>
        /// See the user manual entry on <xref href="Fido2PinProtocol">PIN protocols</xref> for a much more in depth
        /// guide to working with PINs within FIDO2.
        /// </remarks>
        public string? RpId { get; set; }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);

            CborHelpers.BeginMap(cbor)
                .OptionalEntry(TagPinUvAuthProtocol, (uint?)PinUvAuthProtocol)
                .Entry(TagSubCommand, (uint)SubCommand)
                .OptionalEntry(TagKeyAgreement, KeyAgreement)
                .OptionalEntry(TagPinUvAuthParam, PinUvAuthParam)
                .OptionalEntry(TagNewPinEnc, NewPinEnc)
                .OptionalEntry(TagPinHashEnc, PinHashEnc)
                .OptionalEntry(TagPermissions, (uint?)Permissions)
                .OptionalEntry(TagRpId, RpId)
                .EndMap();

            byte[] data = new byte[1 + cbor.BytesWritten];
            int bytesWritten = cbor.Encode(data.AsSpan(1));

            if (bytesWritten != data.Length - 1)
            {
                throw new Ctap2DataException(ExceptionMessages.CborLengthMismatch);
            }

            data[0] = CmdAuthenticatorClientPin;

            return new CommandApdu()
            {
                Ins = CtapConstants.CtapHidCbor,
                Data = data
            };
        }

        /// <inheritdoc />
        public IYubiKeyResponse CreateResponseForApdu(ResponseApdu responseApdu) => new ClientPinResponse(responseApdu);
    }
}
