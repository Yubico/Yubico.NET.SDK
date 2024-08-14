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

using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    public class AddCredentialResponseTests
    {
        [Fact]
        public void Constructor_ReturnsObject()
        {
            var apdu = new ResponseApdu(new byte[0], SWConstants.Success);

            var response = new AddCredentialResponse(apdu);

            Assert.NotNull(response);
        }

        [Fact]
        public void ResponseStatus_GivenStatusWord0x6983_ReturnsFailed()
        {
            var apdu = new ResponseApdu(new byte[0], SWConstants.AuthenticationMethodBlocked);

            var response = new AddCredentialResponse(apdu);

            Assert.Equal(ResponseStatus.Failed, response.Status);
        }

        [Fact]
        public void StatusMessage_GivenStatusWord0x6983_ReturnsCorrectMessage()
        {
            string expectedMessage = "A credential with that label already exists.";

            var apdu = new ResponseApdu(new byte[0], SWConstants.AuthenticationMethodBlocked);

            var response = new AddCredentialResponse(apdu);

            Assert.Equal(expectedMessage, response.StatusMessage);
        }

        // Test pass through to base class status word handling
        [Fact]
        public void ResponseStatus_GivenStatusWord0x63C0_ReturnsAuthenticationRequired()
        {
            var apdu = new ResponseApdu(new byte[0], SWConstants.VerifyFail);

            var response = new AddCredentialResponse(apdu);

            Assert.Equal(ResponseStatus.AuthenticationRequired, response.Status);
        }
    }
}
