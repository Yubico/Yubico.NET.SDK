// Copyright 2025 Yubico AB
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
using System.Linq;
using Xunit;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class ClientPinCommandTests
    {
        [Fact]
        public void AllOptionalProperties_AreNullByDefault()
        {
            var command = new ClientPinCommand();

            Assert.Null(command.Permissions);
            Assert.Null(command.KeyAgreement);
            Assert.Null(command.RpId);
            Assert.Null(command.NewPinEnc);
            Assert.Null(command.PinHashEnc);
            Assert.Null(command.PinUvAuthParam);
            Assert.Null(command.PinUvAuthProtocol);
        }

        [Fact]
        public void Application_SetToFido2()
        {
            var command = new ClientPinCommand();

            Assert.Equal(YubiKeyApplication.Fido2, command.Application);
        }

        [Fact]
        public void CreateCommandApdu_SubcommandSet_CorrectlySerializesApdu()
        {
            var command = new ClientPinCommand()
            {
                SubCommand = 0xFF
            };

            // clientAuthenticatorPin (0x06)
            // A1       # map(1)
            //    02    #   unsigned(2)
            //    18 FF #   unsigned(255)
            byte[] expectedData = { 0x06, 0xA1, 0x02, 0x18, 0xFF };

            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0, apdu.Cla);
            Assert.Equal(0x10, apdu.Ins);
            Assert.Equal(0, apdu.P1);
            Assert.Equal(0, apdu.P2);
            Assert.True(expectedData.SequenceEqual(apdu.Data.ToArray()));
        }

        [Fact]
        public void CreateCommandApdu_AllPropertiesSet_CorrectlySerializesApdu()
        {
            var command = new ClientPinCommand()
            {
                PinUvAuthProtocol = PinUvAuthProtocol.ProtocolOne,
                SubCommand = 0xFF,
                PinUvAuthParam = new byte[] { 4, 5, 6 },
                NewPinEnc = new byte[] { 3, 2, 1 },
                PinHashEnc = new byte[] { 6, 5, 4 },
                Permissions = PinUvAuthTokenPermissions.MakeCredential,
                RpId = "test"
            };

            byte[] expectedData =
            {
                0x06, // clientAuthenticatorPin
                0xA7, // map (8 entries)
                0x01, 0x01, // TagPinUvAuthProtocol = PinProtocolOne
                0x02, 0x18, 0xFF, // TagSubCommand = 0xFF
                0x04, 0x43, 0x04, 0x05, 0x06, // TagPinUvAuthParam = 4, 5, 6
                0x05, 0x43, 0x03, 0x02, 0x01, // TagNewPinEnc = 3, 2, 1
                0x06, 0x43, 0x06, 0x05, 0x04, // TagPinHashEnc = 6, 5, 4
                0x09, 0x01, // TagPermissions = MakeCredential
                0x0A, 0x64, 0x74, 0x65, 0x73, 0x74 // RpId = "test"
            };

            CommandApdu apdu = command.CreateCommandApdu();

            Assert.True(expectedData.SequenceEqual(apdu.Data.ToArray()));
        }

        [Fact]
        public void CreateResponseForApdu_EmptyApdu_ReturnsClientPinResponse()
        {
            var command = new ClientPinCommand();

            IYubiKeyResponse response = command.CreateResponseForApdu(new ResponseApdu(new byte[] { 0x90, 0x00 }));

            _ = Assert.IsType<ClientPinResponse>(response);
        }

        [Fact]
        public void NullKeyAgreement_CorrectApdu()
        {
            byte[] expectedValue = new byte[] {
                0x06, 0xA2, 0x01, 0x01, 0x02, 0x10
            };

            var command = new ClientPinCommand()
            {
                PinUvAuthProtocol = PinUvAuthProtocol.ProtocolOne,
                SubCommand = 16,
            };

            CommandApdu cmdApdu = command.CreateCommandApdu();

            bool isValid = MemoryExtensions.SequenceEqual(new Span<byte>(expectedValue), cmdApdu.Data.Span);
            Assert.True(isValid);
        }
    }
}
