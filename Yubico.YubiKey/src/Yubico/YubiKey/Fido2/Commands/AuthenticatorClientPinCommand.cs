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

namespace Yubico.YubiKey.Fido2.Commands
{
    public enum PinUvAuthProtocol
    {
        ProtocolOne,
        ProtocolTwo,
    }

    [Flags]
    public enum PinUvAuthTokenPermissions
    {
        MakeCredential = 0x01,
        GetAssertion = 0x02,
        CredentialManagement = 0x04,
        BioEnrollment = 0x08,
        LargeBlobWrite = 0x10,
        AuthenticatorConfiguration = 0x20,
    }

    public class AuthenticatorClientPinCommand : IYubiKeyCommand<IYubiKeyResponse>
    {
        // Command constants
        private const byte CmdAuthenticatorClientPin = 0x06;

        protected const int SubCmdGetPinRetries = 0x01;
        protected const int SubCmdGetKeyAgreement = 0x02;
        protected const int SubCmdSetPin = 0x03;
        protected const int SubCmdChangePin = 0x04;
        protected const int SubCmdGetPinToken = 0x05;
        protected const int SubCmdGetPinUvAuthTokenUsingUvWithPermissions = 0x06;
        protected const int SubCmdGetUvRetries = 0x07;
        protected const int SubCmdGetPinUvAuthTokenUsingPinWithPermissions = 0x09;

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

        public PinUvAuthProtocol? PinUvAuthProtocol { get; set; }

        public int SubCommand { get; set; }

        public ReadOnlyMemory<byte>? KeyAgreement { get; set; }

        public ReadOnlyMemory<byte>? PinUvAuthParam { get; set; }

        public ReadOnlyMemory<byte>? NewPinEnc { get; set; }

        public ReadOnlyMemory<byte>? PinHashEnc { get; set; }

        public PinUvAuthTokenPermissions? Permissions { get; set; }

        public string? RpId { get; set; }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);

            cbor.WriteStartMap(null);

            if (PinUvAuthProtocol is { })
            {
                WriteMapEntry(cbor, TagPinUvAuthProtocol, (uint)PinUvAuthProtocol.Value);
            }

            WriteMapEntry(cbor, TagSubCommand, (uint)SubCommand);

            if (KeyAgreement is { })
            {
                WriteMapEntry(cbor, TagKeyAgreement, KeyAgreement.Value);
            }

            if (PinUvAuthParam is { })
            {
                WriteMapEntry(cbor, TagPinUvAuthParam, PinUvAuthParam.Value);
            }

            if (NewPinEnc is { })
            {
                WriteMapEntry(cbor, TagNewPinEnc, NewPinEnc.Value);
            }

            if (PinHashEnc is { })
            {
                WriteMapEntry(cbor, TagPinHashEnc, PinHashEnc.Value);
            }

            if (Permissions is { })
            {
                WriteMapEntry(cbor, TagPermissions, (uint)Permissions.Value);
            }

            if (RpId is { })
            {
                WriteMapEntry(cbor, TagRpId, RpId);
            }

            cbor.WriteEndMap();

            byte[] data = new byte[1 + cbor.BytesWritten];
            int bytesWritten = cbor.Encode(data.AsSpan(1));

            if (bytesWritten != data.Length - 1)
            {
                throw new InvalidOperationException("Encoding error."); // TODO
            }

            data[0] = CmdAuthenticatorClientPin;

            return new CommandApdu()
            {
                Ins = (byte)CtapHidCommand.Cbor,
                Data = cbor.Encode()
            };
        }

        private static void WriteMapEntry(CborWriter cbor, uint key, string value)
        {
            cbor.WriteUInt32(key);
            cbor.WriteTextString(value);
        }

        private static void WriteMapEntry(CborWriter cbor, uint key, uint value)
        {
            cbor.WriteUInt32(key);
            cbor.WriteUInt32(value);
        }

        private static void WriteMapEntry(CborWriter cbor, uint key, ReadOnlyMemory<byte> value)
        {
            cbor.WriteUInt32(key);
            cbor.WriteByteString(value.Span);
        }

        /// <inheritdoc />
        public IYubiKeyResponse CreateResponseForApdu(ResponseApdu responseApdu) => throw new System.NotImplementedException();
    }
}
