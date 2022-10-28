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
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class ClientPinResponseTests
    {
        [Fact]
        public void Constructor_SuccessApdu_Succeeds()
        {
            var apdu = new ResponseApdu(new byte[] { 0x90, 0x00 });

            var response = new ClientPinResponse(apdu);

            Assert.NotNull(response);
        }

        [Fact]
        public void GetData_FullCborData_ParsesCorrectlyAndReturnsClientPinData()
        {
            var apdu = new ResponseApdu(
                new byte[]
                {
                    0xA4, // Map (5 entries)
                    0x02, 0x43, 0x03, 0x02, 0x01, // TagPinUvAuthToken = 3, 2, 1
                    0x03, 0x08, // TagPinRetries = 8
                    0x04, 0xF5, // TagPowerCycleState = true
                    0x05, 0x08 // TagUvRetries = 8
                },
                SWConstants.Success);
            var response = new ClientPinResponse(apdu);

            ClientPinData data = response.GetData();

            Assert.True(data.PinUvAuthToken!.Value.Span.SequenceEqual(new byte[] { 3, 2, 1 }));
            Assert.Equal(8, data.PinRetries);
            Assert.Equal(true, data.PowerCycleState);
            Assert.Equal(8, data.UvRetries);
        }

        [Fact]
        public void GetData_EmptyCborMap_ThrowsException()
        {
            var apdu = new ResponseApdu(
                new byte[]
                {
                    0xA0 // Empty map
                },
                SWConstants.Success);

            var response = new ClientPinResponse(apdu);

            void Action() { _ = response.GetData(); }

            _ = Assert.Throws<Ctap2DataException>(Action);
        }

        [Fact]
        public void GetData_UnrecognizedMapEntry_ThrowsException()
        {
            var apdu = new ResponseApdu(
                new byte[]
                {
                    0xA1, // Map (1 entry)
                    0x0B, 0x00 // Unrecognized tag (0x0B)
                },
                SWConstants.Success);

            var response = new ClientPinResponse(apdu);

            void Action() { _ = response.GetData(); }

            _ = Assert.Throws<Ctap2DataException>(Action);
        }
    }
}
