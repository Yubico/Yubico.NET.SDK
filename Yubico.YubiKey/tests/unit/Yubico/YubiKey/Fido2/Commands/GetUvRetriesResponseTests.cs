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

namespace Yubico.YubiKey.Fido2.Commands
{
    public class GetUvRetriesResponseTests
    {
        [Fact]
        public void Constructor_SuccessApdu_CreatesObject()
        {
            var response = new GetUvRetriesResponse(new ResponseApdu(new byte[] { 0x90, 0x00 }));

            Assert.NotNull(response);
        }

        [Fact]
        public void GetData_WithRetries_ReturnsCorrectData()
        {
            var response = new GetUvRetriesResponse(
                new ResponseApdu(
                    new byte[]
                    {
                        0xA1, // Map (1 entry)
                        0x05, 0x04, // TagUvRetries = 4,
                    },
                    SWConstants.Success));

            int retriesRemaining = response.GetData();

            Assert.Equal(4, retriesRemaining);
        }

        [Fact]
        public void GetData_MissingRetries_ThrowsException()
        {
            var response = new GetUvRetriesResponse(
                new ResponseApdu(
                    new byte[]
                    {
                        0xA1, // Map (1 entry)
                        0x03, 0x08 // PIN retries
                    },
                    SWConstants.Success));

            void Action() { _ = response.GetData(); }

            _ = Assert.Throws<Ctap2DataException>(Action);
        }
    }
}
