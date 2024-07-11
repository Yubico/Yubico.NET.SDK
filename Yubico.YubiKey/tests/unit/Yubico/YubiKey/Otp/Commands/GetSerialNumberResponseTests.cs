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

namespace Yubico.YubiKey.Otp.Commands
{
    public class GetSerialNumberResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            static void action()
            {
                _ = new GetSerialNumberResponse(responseApdu: null);
            }
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            _ = Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var versionResponse = new GetSerialNumberResponse(responseApdu);

            Assert.Equal(SWConstants.Success, versionResponse.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var versionResponse = new GetSerialNumberResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, versionResponse.Status);
        }

        [Fact]
        public void GetData_ResponseApduFailed_ThrowsInvalidOperationException()
        {
            var responseApdu = new ResponseApdu(new byte[] { SW1Constants.NoPreciseDiagnosis, 0x00 });
            var getSerialNumberResponse = new GetSerialNumberResponse(responseApdu);

            void action()
            {
                _ = getSerialNumberResponse.GetData();
            }

            _ = Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void GetData_ResponseApduContainingNotEnoughData_ThrowsMalformedYubiKeyResponseException()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x00, 0x90, 0x00 });
            var getSerialNumberResponse = new GetSerialNumberResponse(responseApdu);

            void action()
            {
                _ = getSerialNumberResponse.GetData();
            }

            var exception = Assert.Throws<MalformedYubiKeyResponseException>(action);
            Assert.Equal(expected: 1, exception.ActualDataLength);
            Assert.Equal(expected: 4, exception.ExpectedDataLength);
        }

        [Fact]
        public void GetData_ResponseApduWithData_ReturnsCorrectData()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x00, 0xBB, 0xCC, 0xDD, 0x90, 0x00 });
            var serialResponse = new GetSerialNumberResponse(responseApdu);

            var serialNum = serialResponse.GetData();

            Assert.Equal(expected: 0x00BBCCDD, serialNum);
        }
    }
}
