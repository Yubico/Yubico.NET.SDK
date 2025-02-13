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

using System;
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Oath.Commands
{
    public class CalculateCredentialResponseTests
    {
        private const short StatusWordNoSuchObject = 0x6984;

        readonly Credential credential = new Credential("Microsoft", "test@outlook.com", CredentialType.Totp, CredentialPeriod.Period30);

        [Fact]
        public void SuccessResponseApdu_NoCredential_ListCredentialsCorrectly()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var calculateCredentialResponse = new CalculateCredentialResponse(responseApdu, credential);

            Assert.Equal(SWConstants.Success, calculateCredentialResponse.StatusWord);
        }

        [Fact]
        public void Status_SuccessResponseApdu_ReturnsSuccess()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var calculateCredentialResponse = new CalculateCredentialResponse(responseApdu, credential);

            Assert.Equal(ResponseStatus.Success, calculateCredentialResponse.Status);
        }

        [Fact]
        public void Status_NoSuchObjectResponseApdu_ReturnsNoData()
        {
            const byte sw1 = unchecked((byte)(StatusWordNoSuchObject >> 8));
            const byte sw2 = unchecked((byte)StatusWordNoSuchObject);

            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var calculateCredentialResponse = new CalculateCredentialResponse(responseApdu, credential);

            Assert.Equal(ResponseStatus.NoData, calculateCredentialResponse.Status);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_FullResponse_ReturnResponseCorrectly()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[] {
                0x75, 0x15, 0x06, 0x8A, 0x9B, 0x0D, 0xF3, 0xD7, 0x18, 0x43,
                0x96, 0x40, 0xA6, 0x58, 0x6F, 0x89, 0xD4, 0x03, 0x1D, 0xC4,
                0xC4, 0x9F, 0x6C, sw1, sw2
            });

            var calculateCredentialResponse = new CalculateCredentialResponse(responseApdu, credential);

            Code? data = calculateCredentialResponse.GetData();

            Assert.Equal(SWConstants.Success, calculateCredentialResponse.StatusWord);
            Assert.NotNull(data.Value);
            Assert.NotEmpty(data.Value);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_TruncatedResponse_ReturnResponseCorrectly()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[] {
                0x76, 0x05, 0x06, 0x8A, 0x9B, 0x0D, 0xF3, sw1, sw2
            });

            var calculateCredentialResponse = new CalculateCredentialResponse(responseApdu, credential);

            Code? data = calculateCredentialResponse.GetData();

            Assert.Equal(SWConstants.Success, calculateCredentialResponse.StatusWord);
            Assert.NotNull(data.Value);
            Assert.NotEmpty(data.Value);
        }

        [Fact]
        public void ResponseApduFailed_ThrowsMalformedYubiKeyResponseException_InvalidApduLength()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0x75, 0x02, 0x06, 0x8A, sw1, sw2 });

            var calculateCredentialResponse = new CalculateCredentialResponse(responseApdu, credential);

            _ = Assert.Throws<MalformedYubiKeyResponseException>(() => calculateCredentialResponse.GetData());
        }

        [Fact]
        public void ResponseApduFailed_ThrowsMalformedYubiKeyResponseException_InvalidApdu()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, sw1, sw2 });

            var calculateCredentialResponse = new CalculateCredentialResponse(responseApdu, credential);

            _ = Assert.Throws<MalformedYubiKeyResponseException>(() => calculateCredentialResponse.GetData());
        }

        [Fact]
        public void ResponseApduFailed_ThrowsInvalidOperationException()
        {
            var responseApdu = new ResponseApdu(new byte[] { SW1Constants.NoPreciseDiagnosis, 0x00 });

            var calculateCredentialResponse = new CalculateCredentialResponse(responseApdu, credential);

            _ = Assert.Throws<InvalidOperationException>(() => calculateCredentialResponse.GetData());
        }
    }
}
