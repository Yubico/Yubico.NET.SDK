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
    public class GetAssertionResponseTests
    {
        [Fact]
        public void GetData_GivenEmptyResponseData_ThrowsMalformedYubiKeyResponseException()
        {
            var getAssertionResponse = new GetAssertionResponse(new ResponseApdu(Array.Empty<byte>(), SWConstants.Success));
            _ = Assert.Throws<MalformedYubiKeyResponseException>(() => getAssertionResponse.GetData());
        }

        [Theory]
        [InlineData("01")]
        [InlineData("0102")]
        [InlineData("7002")]
        public void GetData_GivenBadFidoStatus_Throws(string responseData)
        {
            var getAssertionResponse = new GetAssertionResponse(new ResponseApdu(Hex.HexToBytes(responseData), SWConstants.Success));
            _ = Assert.Throws<BadFido2StatusException>(() => getAssertionResponse.GetData());
        }

        [Theory]
        [InlineData("00A00101")]
        [InlineData("00A50101")]
        public void GetData_GivenBadCbor_ThrowsCborContentException(string responseData)
        {
            var getAssertionResponse = new GetAssertionResponse(new ResponseApdu(Hex.HexToBytes(responseData), SWConstants.Success));
            _ = Assert.Throws<CborContentException>(() => getAssertionResponse.GetData());
        }

        // nb: further testing of CBOR deserialization is in Ctap2CborSerializerTests
        [Theory]
        [InlineData("00A501A26269645840F22006DE4F905AF68A43942F024F2A5ECE603D9C6D4B3DF8BE08ED01FC442646D034858AC75BED3FD580BF9808D94FCBEE82B9B2EF6677AF0ADCC35852EA6B9E64747970656A7075626C69632D6B6579025825625DDADF743F5727E66BBA8C2E387922D1AF43C503D9114A8FBA104D84D02BFA0100000011035847304502204A5A9DD39298149D904769B51A451433006F182A34FBDF66DE5FC717D75FB350022100A46B8EA3C3B933821C6E7F5EF9DAAE94AB47F18DB474C74790EAABB14411E7A004A462696458203082019330820138A0030201023082019330820138A0030201023082019330826469636F6E782B68747470733A2F2F706963732E6578616D706C652E636F6D2F30302F702F61426A6A6A707150622E706E67646E616D65766A6F686E70736D697468406578616D706C652E636F6D6B646973706C61794E616D656D4A6F686E20502E20536D6974680501")]
        public void GetData_GivenCorrectResponse_ReturnsDecodedCbor(string responseData)
        {
            var getAssertionResponse = new GetAssertionResponse(new ResponseApdu(Hex.HexToBytes(responseData), SWConstants.Success));
            GetAssertionOutput parsedOutput = getAssertionResponse.GetData();

            Assert.Equal("John P. Smith", parsedOutput.User!.DisplayName);
        }
    }
}
