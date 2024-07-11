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
    public class GetPinRetriesResponseTests
    {
        [Fact]
        public void Constructor_SuccessApdu_CreatesObject()
        {
            var response = new GetPinRetriesResponse(new ResponseApdu(new byte[] { 0x90, 0x00 }));

            Assert.NotNull(response);
        }

        [Fact]
        public void GetData_WithRetriesAndPowerCycle_ReturnsCorrectData()
        {
            var response = new GetPinRetriesResponse(
                new ResponseApdu(
                    new byte[]
                    {
                        0xA2, // Map (2 entries)
                        0x03, 0x08, // TagPinRetries = 8
                        0x04, 0xF5 // TagPowerCycleState = true
                    },
                    SWConstants.Success));

            (int retriesRemaining, bool? powerCycleRequired) = response.GetData();

            Assert.Equal(8, retriesRemaining);
            Assert.Equal(true, powerCycleRequired);
        }

        [Fact]
        public void GetData_WithRetriesOnly_ReturnsCorrectData()
        {
            var response = new GetPinRetriesResponse(
                new ResponseApdu(
                    new byte[]
                    {
                        0xA1, // Map (1 entry)
                        0x03, 0x08, // TagPinRetries = 8
                    },
                    SWConstants.Success));

            (int retriesRemaining, bool? powerCycleRequired) = response.GetData();

            Assert.Equal(8, retriesRemaining);
            Assert.Null(powerCycleRequired);
        }

        [Fact]
        public void GetData_MissingRetries_ThrowsException()
        {
            var response = new GetPinRetriesResponse(
                new ResponseApdu(
                    new byte[]
                    {
                        0xA1, // Map (1 entry)
                        0x04, 0xF5 // TagPowerCycleState = true
                    },
                    SWConstants.Success));

            void Action() { _ = response.GetData(); }

            _ = Assert.Throws<Ctap2DataException>(Action);
        }
    }
}
