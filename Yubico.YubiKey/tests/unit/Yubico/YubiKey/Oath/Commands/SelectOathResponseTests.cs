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
    public class SelectOathResponseTests
    {
        [Fact]
        public void Status_SuccessResponseApdu_ReturnsSuccess()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var selectOathResponse = new SelectOathResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, selectOathResponse.Status);
        }

        [Fact]
        public void SuccessResponseApdu_PasswordIsSet_OathResponseInfoCorrect()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[]
            {
                0x79, 0x03, 0x05, 0x02, 0x04, 0x71, 0x08, 0xE3, 0x0E, 0xB3,
                0x36, 0x5C, 0x8D, 0xF1, 0x44, 0x74, 0x08, 0xF1, 0xD3, 0xDA,
                0x89, 0x58, 0xE4, 0x40, 0x85, 0x7B, 0x01, 0x01, sw1, sw2
            });

            var selectOathResponse = new SelectOathResponse(responseApdu);
            var data = selectOathResponse.GetData();

            var version = new FirmwareVersion
            {
                Major = 0x05,
                Minor = 0x02,
                Patch = 0x04
            };

            byte[]? salt = { 0xE3, 0x0E, 0xB3, 0x36, 0x5C, 0x8D, 0xF1, 0x44 };
            byte[]? challenge = { 0xF1, 0xD3, 0xDA, 0x89, 0x58, 0xE4, 0x40, 0x85 };
            var algorithm = HashAlgorithm.Sha1;

            Assert.Equal(SWConstants.Success, selectOathResponse.StatusWord);
            Assert.Equal(version, data.Version);
            Assert.True(data.Salt.Span.SequenceEqual(salt));
            Assert.True(data.Challenge.Span.SequenceEqual(challenge));
            Assert.Equal(algorithm, data.Algorithm);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_NoPasswordSet_OathResponseInfoCorrect()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[]
            {
                0x79, 0x03, 0x05, 0x02, 0x04, 0x71, 0x08, 0xC0, 0xE3, 0xAF,
                0x27, 0xCC, 0x7A, 0x20, 0xEE, sw1, sw2
            });


            var selectOathResponse = new SelectOathResponse(responseApdu);
            var data = selectOathResponse.GetData();

            var version = new FirmwareVersion
            {
                Major = 0x05,
                Minor = 0x02,
                Patch = 0x04
            };

            byte[]? salt = { 0xC0, 0xE3, 0xAF, 0x27, 0xCC, 0x7A, 0x20, 0xEE };
            var algorithm = HashAlgorithm.Sha1;

            Assert.Equal(SWConstants.Success, selectOathResponse.StatusWord);
            Assert.Equal(version, data.Version);
            Assert.True(data.Salt.Span.SequenceEqual(salt));
            Assert.True(data.Challenge.IsEmpty);
            Assert.Equal(algorithm, data.Algorithm);
        }

        [Fact]
        public void ResponseApduFailed_ThrowsMalformedYubiKeyResponseException_InvalidApdu()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, sw1, sw2 });

            var selectResponse = new SelectOathResponse(responseApdu);

            _ = Assert.Throws<MalformedYubiKeyResponseException>(() => selectResponse.GetData());
        }

        [Fact]
        public void ResponseApduFailed_ThrowsInvalidOperationException()
        {
            var responseApdu = new ResponseApdu(new byte[] { SW1Constants.NoPreciseDiagnosis, 0x00 });
            var selectResponse = new SelectOathResponse(responseApdu);

            _ = Assert.Throws<InvalidOperationException>(() => selectResponse.GetData());
        }
    }
}
