// Copyright 2021 Yubico AB
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
using System.Formats.Cbor;
using Yubico.Core.Iso7816;
using Yubico.Core.Buffers;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class GetInfoResponseTests
    {
        [Fact]
        public void GetData_GivenEmptyResponseData_ThrowsMalformedYubiKeyResponseException()
        {
            var getInfoResponse = new GetInfoResponse(new ResponseApdu(Array.Empty<byte>(), SWConstants.Success));
            _ = Assert.Throws<MalformedYubiKeyResponseException>(() => getInfoResponse.GetData());
        }

        [Theory]
        [InlineData("01")]
        [InlineData("0102")]
        [InlineData("7002")]
        public void GetData_GivenBadFidoStatus_Throws(string responseData)
        {
            var getInfoResponse = new GetInfoResponse(new ResponseApdu(Hex.HexToBytes(responseData), SWConstants.Success));
            _ = Assert.Throws<BadFido2StatusException>(() => getInfoResponse.GetData());
        }

        [Theory]
        [InlineData("00A00101")]
        [InlineData("00A50101")]
        public void GetData_GivenBadCbor_ThrowsCborContentException(string responseData)
        {
            var getInfoResponse = new GetInfoResponse(new ResponseApdu(Hex.HexToBytes(responseData), SWConstants.Success));
            _ = Assert.Throws<CborContentException>(() => getInfoResponse.GetData());
        }

        // nb: further testing of CBOR deserialization is in Ctap2CborSerializerTests
        [Theory]
        [InlineData("00A60183665532465F5632684649444F5F325F306C4649444F5F325F315F50524502826B6372656450726F746563746B686D61632D73656372657403502FC0579F811347EAB116BB5A8DB9202A04A562726BF5627570F564706C6174F469636C69656E7450696EF47563726564656E7469616C4D676D7450726576696577F5051904B0068101")]
        public void GetData_GivenCorrectResponse_ReturnsDecodedCbor(string responseData)
        {
            var getInfoResponse = new GetInfoResponse(new ResponseApdu(Hex.HexToBytes(responseData), SWConstants.Success));
            DeviceInfo parsedDeviceInfo = getInfoResponse.GetData();

            Assert.Equal(1200, parsedDeviceInfo.MaxMessageSize);
        }
    }
}
