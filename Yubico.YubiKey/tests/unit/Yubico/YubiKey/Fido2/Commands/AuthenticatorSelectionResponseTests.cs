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

using Xunit;
using Yubico.Core.Iso7816;
using Yubico.YubiKey;
using Yubico.YubiKey.Fido2;

namespace Yubico.YubiKey.Fido2.Commands
{
    // Unit tests for AuthenticatorSelectionResponse (CTAP 2.2 authenticatorSelection; partner to AuthenticatorSelectionCommand).
    public class AuthenticatorSelectionResponseTests
    {
        [Fact]
        public void Constructor_GivenSuccessApdu_SetsOkAndSuccess()
        {
            // Empty response body and 9000; maps to CTAP OK (mirrors successful authenticatorSelection).
            var responseApdu = new ResponseApdu(System.Array.Empty<byte>(), SWConstants.Success);
            var response = new AuthenticatorSelectionResponse(responseApdu);

            Assert.Equal(CtapStatus.Ok, response.CtapStatus);
            Assert.Equal(ResponseStatus.Success, response.Status);
        }

        [Fact]
        public void Constructor_GivenInvalidCommand_SetsCtapStatus()
        {
            // CTAP error encoding: SW1=0x6F, SW2=CTAP status byte (see CtapToApduResponse.GetSwForCtapError).
            short sw = unchecked((short)((SW1Constants.NoPreciseDiagnosis << 8) | (byte)CtapStatus.InvalidCommand)); // InvalidCommand / CTAP1_ERR_INVALID_COMMAND
            var responseApdu = new ResponseApdu(System.Array.Empty<byte>(), sw);
            var response = new AuthenticatorSelectionResponse(responseApdu);

            Assert.Equal(CtapStatus.InvalidCommand, response.CtapStatus);
            Assert.Equal(ResponseStatus.Failed, response.Status);
        }

        [Fact]
        public void Constructor_GivenOperationDenied_UsesSelectionDeniedMessage()
        {
            // Same SW packing as InvalidCommand test; OperationDenied when user presence is refused for selection.
            short sw = unchecked((short)((SW1Constants.NoPreciseDiagnosis << 8) | (byte)CtapStatus.OperationDenied)); // CTAP2_ERR_OPERATION_DENIED
            var responseApdu = new ResponseApdu(System.Array.Empty<byte>(), sw);
            var response = new AuthenticatorSelectionResponse(responseApdu);

            Assert.Equal(CtapStatus.OperationDenied, response.CtapStatus);
            Assert.Equal(ResponseStatus.Failed, response.Status);
            Assert.Equal(ResponseStatusMessages.Fido2AuthenticatorSelectionDenied, response.StatusMessage);
        }
    }
}