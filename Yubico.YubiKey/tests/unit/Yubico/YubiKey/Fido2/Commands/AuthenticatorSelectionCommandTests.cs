// Copyright 2026 Yubico AB
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
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands
{
    // Unit tests for AuthenticatorSelectionCommand (CTAP 2.1 §6.9 authenticatorSelection, command byte 0x0B).
    public class AuthenticatorSelectionCommandTests
    {
        [Fact]
        public void Constructor_Succeeds()
        {
            var command = new AuthenticatorSelectionCommand();

            Assert.NotNull(command);
        }

        [Fact]
        public void CreateCommandApdu_CreatesCorrectApdu()
        {
            var command = new AuthenticatorSelectionCommand();
            CommandApdu apdu = command.CreateCommandApdu();

            // Payload is the CTAP command byte only; no CBOR parameters (CTAP 2.1 §6.9 authenticatorSelection).
            byte[] expectedData = new byte[]
            {
                0x0B, // authenticatorSelection
            };

            Assert.Equal(0, apdu.Cla);
            Assert.Equal(0x10, apdu.Ins); // CTAPHID_CBOR / FIDO2 extended APDU INS, same as other CTAP-via-APDU commands
            Assert.Equal(0, apdu.P1);
            Assert.Equal(0, apdu.P2);
            Assert.True(apdu.Data.Span.SequenceEqual(expectedData));
        }

        [Fact]
        public void CreateResponseForApdu_ReturnsAuthenticatorSelectionResponse()
        {
            var command = new AuthenticatorSelectionCommand();
            // Empty response body and 9000; maps to CTAP OK in AuthenticatorSelectionResponse.
            var responseApdu = new ResponseApdu(System.Array.Empty<byte>(), SWConstants.Success);

            AuthenticatorSelectionResponse response = command.CreateResponseForApdu(responseApdu);

            Assert.NotNull(response);
        }
    }
}