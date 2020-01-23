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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Xunit;
using Yubico.Core.Buffers;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    public class RegisterResponseTests
    {
        private const string GoodResponseHex = "050460232F14DB552AF4CC16D14E65678567C1376C51230E53AF2E969A8A72E9CC218F0E6025129CB8B1A7F2861370DD819F12222CF3C9A261C2DC23A664D00381784056453E66362C84308D118E650F704619193B4FA6429243966CC4E0C360F116A9E5F9E43E52C5EA9C6C7E926AAB03AD117833BA6FBFA7DEF543118BFC9FBFBB2F308202B9308201A1A003020102020415124113300D06092A864886F70D01010B05003021311F301D06035504030C1659756269636F204649444F2050726576696577204341301E170D3139303231383132313330385A170D3230303231383132313330385A3079310B300906035504061302534531123010060355040A0C0959756269636F20414231223020060355040B0C1941757468656E74696361746F72204174746573746174696F6E3132303006035504030C2959756269636F205532462045452053657269616C2031313530313830333739323034343830323332333059301306072A8648CE3D020106082A8648CE3D03010703420004D4CFC033E28C28A842E2394828DC14BF3235CBFB309C062B3AB6A4020DF34B182C441151E46EEB0D59A403D4ADC155DEAC3031E2F50206B3ED9F08E5AAC5185DA36C306A302206092B0601040182C40A020415312E332E362E312E342E312E34313438322E312E373013060B2B0601040182E51C0201010404030205203021060B2B0601040182E51C01010404120410EE882879721C491397753DFCCE97072A300C0603551D130101FF04023000300D06092A864886F70D01010B050003820101004F9C37C312C1BAB573EA012F53B4EDCEC0515CCB490B852431EAC0305032AEB98F28F9A25DB55C9BB3F0FA38C640607D372FAD80D016B0F2F7AB818A52E97E68BDD932877BD143892FD0B11BCF772F1903F26EDC9335943FBC728FADDE736A5F17BAE034683989549072516C432587B5AA0F2E9A65F46C6F6F06C0E4FA60CDB4B67316D462CB2360B2B7669FF4BAF199DDC69CFA30FA1D53A61EF62E8314B24109FF4DF4984DC7F8DBB94A604C6997218E626C8E1949898F7F49621418ABAF09C74F013B4E9FB40BE177BC7D92E751D7B0E8DEC771796544247DC7694AEFB45324BDB53C94860723549702617540477827438032AE75DB0DA2D5C008310DFED03045022054C53FDD4076DB440A3922519A04EFC6B6EF9B8B38C3CC3B42F13BB72F8347DC0221009C55A5143BDC2B7312A6C1BB88A2B3E54985991465056B109F07771B4028D911";

        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            static void action() => _ = new RegisterResponse(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            _ = Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var registerResponse = new RegisterResponse(responseApdu);

            Assert.Equal(SWConstants.Success, registerResponse.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var registerResponse = new RegisterResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, registerResponse.Status);
        }

        [Fact]
        public void Constructor_ConditionsNotSatisfiedResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.ConditionsNotSatisfied >> 8));
            byte sw2 = unchecked((byte)SWConstants.ConditionsNotSatisfied);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var registerResponse = new RegisterResponse(responseApdu);

            Assert.Equal(ResponseStatus.ConditionsNotSatisfied, registerResponse.Status);
        }

        [Theory]
        [InlineData("07")]
        [InlineData("050000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")]
        [InlineData("050400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")]
        [InlineData("050400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000FF0000000000000000000000000000")]
        [InlineData("050400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000010000000000000000000000000000")]
        public void GetData_BadResponseData_ThrowsMalformedYubiKeyResponse(string responseHex)
        {
            var responseApdu = new ResponseApdu(Hex.HexToBytes(responseHex), SWConstants.Success);

            var registerResponse = new RegisterResponse(responseApdu);

            _ = Assert.Throws<MalformedYubiKeyResponseException>(() => registerResponse.GetData());
        }

        [Fact]
        public void GetData_GoodResponseData_Succeeds()
        {
            var responseApdu = new ResponseApdu(Hex.HexToBytes(GoodResponseHex), SWConstants.Success);

            var registerResponse = new RegisterResponse(responseApdu);

            _ = registerResponse.GetData();
        }

        [Fact]
        public void GetData_GoodResponseData_SetsUserPublicKeyCorrectly()
        {
            var responseApdu = new ResponseApdu(Hex.HexToBytes(GoodResponseHex), SWConstants.Success);

            var registerResponse = new RegisterResponse(responseApdu);

            RegistrationData data = registerResponse.GetData();
            Assert.Equal("60232F14DB552AF4CC16D14E65678567C1376C51230E53AF2E969A8A72E9CC21", Hex.BytesToHex(data.UserPublicKey.X));
            Assert.Equal("8F0E6025129CB8B1A7F2861370DD819F12222CF3C9A261C2DC23A664D0038178", Hex.BytesToHex(data.UserPublicKey.Y));
        }

        [Fact]
        public void GetData_GoodResponseData_SetsKeyHandleCorrectly()
        {
            var responseApdu = new ResponseApdu(Hex.HexToBytes(GoodResponseHex), SWConstants.Success);

            var registerResponse = new RegisterResponse(responseApdu);

            RegistrationData data = registerResponse.GetData();
            Assert.Equal("56453E66362C84308D118E650F704619193B4FA6429243966CC4E0C360F116A9E5F9E43E52C5EA9C6C7E926AAB03AD117833BA6FBFA7DEF543118BFC9FBFBB2F", Hex.BytesToHex(data.KeyHandle.ToArray()));
        }

        [Fact]
        public void GetData_GoodResponseData_SetsCertificateCorrectly()
        {
            var responseApdu = new ResponseApdu(Hex.HexToBytes(GoodResponseHex), SWConstants.Success);

            var registerResponse = new RegisterResponse(responseApdu);

            RegistrationData data = registerResponse.GetData();
            Assert.Equal("308202B9308201A1A003020102020415124113300D06092A864886F70D01010B05003021311F301D06035504030C1659756269636F204649444F2050726576696577204341301E170D3139303231383132313330385A170D3230303231383132313330385A3079310B300906035504061302534531123010060355040A0C0959756269636F20414231223020060355040B0C1941757468656E74696361746F72204174746573746174696F6E3132303006035504030C2959756269636F205532462045452053657269616C2031313530313830333739323034343830323332333059301306072A8648CE3D020106082A8648CE3D03010703420004D4CFC033E28C28A842E2394828DC14BF3235CBFB309C062B3AB6A4020DF34B182C441151E46EEB0D59A403D4ADC155DEAC3031E2F50206B3ED9F08E5AAC5185DA36C306A302206092B0601040182C40A020415312E332E362E312E342E312E34313438322E312E373013060B2B0601040182E51C0201010404030205203021060B2B0601040182E51C01010404120410EE882879721C491397753DFCCE97072A300C0603551D130101FF04023000300D06092A864886F70D01010B050003820101004F9C37C312C1BAB573EA012F53B4EDCEC0515CCB490B852431EAC0305032AEB98F28F9A25DB55C9BB3F0FA38C640607D372FAD80D016B0F2F7AB818A52E97E68BDD932877BD143892FD0B11BCF772F1903F26EDC9335943FBC728FADDE736A5F17BAE034683989549072516C432587B5AA0F2E9A65F46C6F6F06C0E4FA60CDB4B67316D462CB2360B2B7669FF4BAF199DDC69CFA30FA1D53A61EF62E8314B24109FF4DF4984DC7F8DBB94A604C6997218E626C8E1949898F7F49621418ABAF09C74F013B4E9FB40BE177BC7D92E751D7B0E8DEC771796544247DC7694AEFB45324BDB53C94860723549702617540477827438032AE75DB0DA2D5C008310DFED0", Hex.BytesToHex(data.AttestationCertificate.RawData.ToArray()));
        }

        [Fact]
        public void GetData_GoodResponseData_SetsSignatureCorrectly()
        {
            var responseApdu = new ResponseApdu(Hex.HexToBytes(GoodResponseHex), SWConstants.Success);

            var registerResponse = new RegisterResponse(responseApdu);

            RegistrationData data = registerResponse.GetData();
            Assert.Equal("3045022054C53FDD4076DB440A3922519A04EFC6B6EF9B8B38C3CC3B42F13BB72F8347DC0221009C55A5143BDC2B7312A6C1BB88A2B3E54985991465056B109F07771B4028D911", Hex.BytesToHex(data.Signature.ToArray()));
        }
    }
}
