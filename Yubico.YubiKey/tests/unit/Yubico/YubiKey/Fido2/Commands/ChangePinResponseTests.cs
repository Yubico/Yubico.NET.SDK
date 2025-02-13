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

using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class ChangePinResponseTests
    {
        [Fact]
        public void Constructor_SuccessApdu_CreatesObject()
        {
            var response = new ChangePinResponse(new ResponseApdu(new byte[] { 0x90, 0x00 }));

            Assert.NotNull(response);
        }

        [Fact]
        public void Constructor_SuccessApdu_SetsStatus()
        {
            var response = new ChangePinResponse(new ResponseApdu(new byte[] { 0x90, 0x00 }));

            Assert.Equal(ResponseStatus.Success, response.Status);
        }

        [Fact]
        public void Constructor_ErrorApdu_SetsStatus()
        {
            var response = new ChangePinResponse(new ResponseApdu(new byte[] { 0x6F, 0x33 }));

            Assert.Equal(ResponseStatus.AuthenticationRequired, response.Status);
        }
    }
}
