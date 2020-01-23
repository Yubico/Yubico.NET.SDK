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
    public class MakeCredentialResponseTests
    {
        [Fact]
        public void GetData_GivenEmptyResponseData_ThrowsMalformedYubiKeyResponseException()
        {
            var makeCredentialResponse = new MakeCredentialResponse(new ResponseApdu(Array.Empty<byte>(), SWConstants.Success));
            _ = Assert.Throws<MalformedYubiKeyResponseException>(() => makeCredentialResponse.GetData());
        }

        [Theory]
        [InlineData("01")]
        [InlineData("0102")]
        [InlineData("7002")]
        public void GetData_GivenBadFidoStatus_Throws(string responseData)
        {
            var makeCredentialResponse = new MakeCredentialResponse(new ResponseApdu(Hex.HexToBytes(responseData), SWConstants.Success));
            _ = Assert.Throws<BadFido2StatusException>(() => makeCredentialResponse.GetData());
        }

        [Theory]
        [InlineData("00A00101")]
        [InlineData("00A50101")]
        public void GetData_GivenBadCbor_ThrowsCborContentException(string responseData)
        {
            var makeCredentialResponse = new MakeCredentialResponse(new ResponseApdu(Hex.HexToBytes(responseData), SWConstants.Success));
            _ = Assert.Throws<CborContentException>(() => makeCredentialResponse.GetData());
        }

        // nb: further testing of CBOR deserialization is in Ctap2CborSerializerTests
        [Theory]
        [InlineData("00A301667061636B656402589AC289C5CA9B0460F9346AB4E42D842743404D31F4846825A6D065BE597A87051D410000000BF8A011F38C0A4D15800617111F9EDC7D00108959CEAD5B5C48164E8ABCD6D9435C6FA363616C6765455332353661785820F7C4F4A6F1D79538DFA4C9AC50848DF708BC1C99F5E60E51B42A521B35D3B69A61795820DE7B7D6CA564E70EA321A4D5D96EA00EF0E2DB89DD61D4894C15AC585BD2368403A363616C67266373696758473045022013F73C5D9D530E8CC15CC9BD96AD586D393664E462D5F0561235E6350F2B728902210090357FF910CCB56AC5B596511948581C8FDDB4A2B79959948078B09F4BDC622963783563815901973082019330820138A003020102020900859B726CB24B4C29300A06082A8648CE3D0403023047310B300906035504061302555331143012060355040A0C0B59756269636F205465737431223020060355040B0C1941757468656E74696361746F72204174746573746174696F6E301E170D3136313230343131353530305A170D3236313230323131353530305A3047310B300906035504061302555331143012060355040A0C0B59756269636F205465737431223020060355040B0C1941757468656E74696361746F72204174746573746174696F6E3059301306072A8648CE3D020106082A8648CE3D03010703420004AD11EB0E8852E53AD5DFED86B41E6134A18EC4E1AF8F221A3C7D6E636C80EA13C3D504FF2E76211BB44525B196C44CB4849979CF6F896ECD2BB860DE1BF4376BA30D300B30090603551D1304023000300A06082A8648CE3D0403020349003046022100E9A39F1B03197525F7373E10CE77E78021731B94D0C03F3FDA1FD22DB3D030E7022100C4FAEC3445A820CF43129CDB00AABEFD9AE2D874F9C5D343CB2F113DA23723F3")]
        public void GetData_GivenCorrectResponse_ReturnsDecodedCbor(string responseData)
        {
            var makeCredentialResponse = new MakeCredentialResponse(new ResponseApdu(Hex.HexToBytes(responseData), SWConstants.Success));
            IMakeCredentialOutput parsedOutput = makeCredentialResponse.GetData();

            MakeCredentialOutput<PackedAttestation> makeCredentialOutputPacked = Assert.IsType<MakeCredentialOutput<PackedAttestation>>(parsedOutput);
            Assert.Equal(Hex.HexToBytes("3045022013F73C5D9D530E8CC15CC9BD96AD586D393664E462D5F0561235E6350F2B728902210090357FF910CCB56AC5B596511948581C8FDDB4A2B79959948078B09F4BDC6229"), makeCredentialOutputPacked.AttestationStatement.Signature);
        }
    }
}
