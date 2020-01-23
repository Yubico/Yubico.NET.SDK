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
    public class ListCredentialsResponseTests
    {
        [Fact]
        public void Status_SuccessResponseApdu_ReturnsSuccess()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var listCredentialsResponse = new ListResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, listCredentialsResponse.Status);
        }

        [Fact]
        public void SuccessResponseApdu_NoCredentials_ListCredentialsCorrectly()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var listCredentialsResponse = new ListResponse(responseApdu);

            var data = listCredentialsResponse.GetData();

            Assert.Empty(data);
            Assert.Equal(SWConstants.Success, listCredentialsResponse.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_OneCredential_ListCredentialsCorrectly()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[] {
                0x72, 0x1B, 0x21, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F,
                0x66, 0x74, 0x3A, 0x74, 0x65, 0x73, 0x74, 0x40, 0x6F, 0x75,
                0x74, 0x6C, 0x6F, 0x6F, 0x6B, 0x2E, 0x63, 0x6F, 0x6D,
                sw1, sw2
            });


            var listCredentialsResponse = new ListResponse(responseApdu);

            var data = listCredentialsResponse.GetData();

            Assert.Equal(SWConstants.Success, listCredentialsResponse.StatusWord);

            _ = Assert.Single(data);
            Assert.Equal("Microsoft", data[0].Issuer);
            Assert.Equal("test@outlook.com", data[0].AccountName);
            Assert.Equal(CredentialType.Totp, data[0].Type);
            Assert.Equal(CredentialPeriod.Period30, data[0].Period);
            Assert.Equal(HashAlgorithm.Sha1, data[0].Algorithm);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_MultipleCredentials_ListCredentialsCorrectly()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[] {
                0x72, 0x1B, 0x21, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F,
                0x66, 0x74, 0x3A, 0x74, 0x65, 0x73, 0x74, 0x40, 0x6F, 0x75,
                0x74, 0x6C, 0x6F, 0x6F, 0x6B, 0x2E, 0x63, 0x6F, 0x6D, 0x72, 
                0x16, 0x12, 0x47, 0x6F, 0x6F, 0x67, 0x6C, 0x65, 0x3A, 0x74, 
                0x65, 0x73, 0x74, 0x40, 0x67, 0x6D, 0x61, 0x69, 0x6C, 0x2E,
                0x63, 0x6F, 0x6D, sw1, sw2
            });


            var listCredentialsResponse = new ListResponse(responseApdu);

            var data = listCredentialsResponse.GetData();

            Assert.Equal(SWConstants.Success, listCredentialsResponse.StatusWord);

            Assert.Equal(2, data.Count);

            Assert.Equal("Microsoft", data[0].Issuer);
            Assert.Equal("test@outlook.com", data[0].AccountName);
            Assert.Equal(CredentialType.Totp, data[0].Type);
            Assert.Equal(CredentialPeriod.Period30, data[0].Period);
            Assert.Equal(HashAlgorithm.Sha1, data[0].Algorithm);

            Assert.Equal("Google", data[1].Issuer);
            Assert.Equal("test@gmail.com", data[1].AccountName);
            Assert.Equal(CredentialType.Hotp, data[1].Type);
            Assert.Equal(CredentialPeriod.Undefined, data[1].Period);
            Assert.Equal(HashAlgorithm.Sha256, data[1].Algorithm);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_Period60_ListCredentialsCorrectly()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[] {
                0x72, 0x1E, 0x21, 0x36, 0x30, 0x2F, 0x4D, 0x69, 0x63, 0x72,
                0x6F, 0x73, 0x6F, 0x66, 0x74, 0x3A, 0x74, 0x65, 0x73, 0x74, 
                0x40, 0x6F, 0x75, 0x74, 0x6C, 0x6F, 0x6F, 0x6B, 0x2E, 0x63, 
                0x6F, 0x6D, sw1, sw2
            });


            var listCredentialsResponse = new ListResponse(responseApdu);

            var data = listCredentialsResponse.GetData();

            Assert.Equal(SWConstants.Success, listCredentialsResponse.StatusWord);

            _ = Assert.Single(data);
            Assert.Equal("Microsoft", data[0].Issuer);
            Assert.Equal("test@outlook.com", data[0].AccountName);
            Assert.Equal(CredentialType.Totp, data[0].Type);
            Assert.Equal(CredentialPeriod.Period60, data[0].Period);
            Assert.Equal(HashAlgorithm.Sha1, data[0].Algorithm);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_IssuerHasSemicolon_ListCredentialsCorrectly()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[] {
                0x72, 0x20, 0x21, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F,
                0x66, 0x74, 0x3A, 0x64, 0x65, 0x6D, 0x6F, 0x3A, 0x74, 0x65,
                0x73, 0x74, 0x40, 0x6F, 0x75, 0x74, 0x6C, 0x6F, 0x6F, 0x6B,
                0x2E, 0x63, 0x6F, 0x6D, sw1, sw2
            });


            var listCredentialsResponse = new ListResponse(responseApdu);

            var data = listCredentialsResponse.GetData();

            Assert.Equal(SWConstants.Success, listCredentialsResponse.StatusWord);

            _ = Assert.Single(data);
            Assert.Equal("Microsoft:demo", data[0].Issuer);
            Assert.Equal("test@outlook.com", data[0].AccountName);
            Assert.Equal(CredentialType.Totp, data[0].Type);
            Assert.Equal(CredentialPeriod.Period30, data[0].Period);
            Assert.Equal(HashAlgorithm.Sha1, data[0].Algorithm);
        }

        [Fact]
        public void ResponseApduFailed_ThrowsMalformedYubiKeyResponseException_InvalidApdu()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, sw1, sw2 });
            var listResponse = new ListResponse(responseApdu);

            _ = Assert.Throws<MalformedYubiKeyResponseException>(() => listResponse.GetData());
        }

        [Fact]
        public void ResponseApduFailed_ThrowsInvalidOperationException()
        {
            var responseApdu = new ResponseApdu(new byte[] { SW1Constants.NoPreciseDiagnosis, 0x00 });
            var listResponse = new ListResponse(responseApdu);

            _ = Assert.Throws<InvalidOperationException>(() => listResponse.GetData());
        }
    }
}
