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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Oath.Commands
{
    public class CalculateAllCredentialsResponseTests
    {
        [Fact]
        public void Status_SuccessResponseApdu_ReturnsSuccess()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var calculateAllCredentialsResponse = new CalculateAllCredentialsResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, calculateAllCredentialsResponse.Status);
        }

        [Fact]
        public void SuccessResponseApdu_NoCredentials_ListCredentialsCorrectly()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var calculateAllCredentialsResponse = new CalculateAllCredentialsResponse(responseApdu);

            Assert.Equal(SWConstants.Success, calculateAllCredentialsResponse.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_NoCredentials_ReturnResponseCorrectly()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var calculateAllCredentialsResponse = new CalculateAllCredentialsResponse(responseApdu);
            var data = calculateAllCredentialsResponse.GetData();

            Assert.Equal(SWConstants.Success, calculateAllCredentialsResponse.StatusWord);
            Assert.Empty(data);   
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_ReturnResponseCorrectly()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[] {
                0x71, 0x1A, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66, 0x74, 0x3A, 0x74, 0x65, 0x73, 0x74,
                0x40, 0x6F, 0x75, 0x74, 0x6C, 0x6F, 0x6F, 0x6B, 0x2E, 0x63, 0x6F, 0x6D, 0x75, 0x15, 0x06, 0x8A,
                0x9B, 0x0D, 0xF3, 0xD7, 0x18, 0x43, 0x96, 0x40, 0xA6, 0x58, 0x6F, 0x89, 0xD4, 0x03, 0x1D, 0xC4,
                0xC4, 0x9F, 0x6C, 0x71, 0x15, 0x41, 0x70, 0x70, 0x6C, 0x65, 0x3A, 0x74, 0x65, 0x73, 0x74, 0x40,
                0x69, 0x63, 0x6C, 0x6F, 0x75, 0x64, 0x2E, 0x63, 0x6F, 0x6D, 0x77, 0x01, 0x06, sw1, sw2
            });

            var calculateAllCredentialsResponse = new CalculateAllCredentialsResponse(responseApdu);

            var data = calculateAllCredentialsResponse.GetData();
            var credentialHotp = new Credential("Apple", "test@icloud.com", CredentialType.Hotp, CredentialPeriod.Undefined);
            var credentialTotp = new Credential {
                Issuer = "Microsoft",
                AccountName = "test@outlook.com",
                Type = CredentialType.Totp,
                Period = CredentialPeriod.Period30,
                Digits = 6
            };

            Assert.Equal(SWConstants.Success, calculateAllCredentialsResponse.StatusWord);
            Assert.Equal(2, data.Count);
            Assert.Contains(credentialTotp, data.Keys);
            Assert.Contains(credentialHotp, data.Keys);
        }

        [Fact]
        public void ResponseApduFailed_ThrowsMalformedYubiKeyResponseException_InvalidApdu1()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] {
                0x71, 0x15, 0x41, 0x70, 0x70, 0x6C, 0x65, 0x3A, 0x74, 0x65, 0x73, 0x74, 0x40,
                0x69, 0x63, 0x6C, 0x6F, 0x75, 0x64, 0x2E, 0x63, 0x6F, 0x6D, 0x78, 0x01, 0x06, sw1, sw2
            });

            var calculateAllCredentialsResponse = new CalculateAllCredentialsResponse(responseApdu);

            _ = Assert.Throws<MalformedYubiKeyResponseException>(() => calculateAllCredentialsResponse.GetData());
        }

        [Fact]
        public void ResponseApduFailed_ThrowsMalformedYubiKeyResponseException_InvalidApdu2()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] {
                0x1A, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, sw1, sw2
            });

            var calculateAllCredentialsResponse = new CalculateAllCredentialsResponse(responseApdu);

            _ = Assert.Throws<MalformedYubiKeyResponseException>(() => calculateAllCredentialsResponse.GetData());
        }

        [Fact]
        public void ResponseApduFailed_ThrowsInvalidOperationException()
        {
            var responseApdu = new ResponseApdu(new byte[] { SW1Constants.NoPreciseDiagnosis, 0x00 });
            var calculateAllCredentialsResponse = new CalculateAllCredentialsResponse(responseApdu);

            _ = Assert.Throws<InvalidOperationException>(() => calculateAllCredentialsResponse.GetData());
        }
    }
}
