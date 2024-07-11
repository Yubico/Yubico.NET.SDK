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

namespace Yubico.YubiKey.Piv.Commands
{
    public class GetSerialNumberResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentNullException>(() => new GetSerialNumberResponse(null));
#pragma warning restore CS8625
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, 0, sw1, sw2 });

            var serialResponse = new GetSerialNumberResponse(responseApdu);

            Assert.Equal(SWConstants.Success, serialResponse.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, 0, sw1, sw2 });

            var serialResponse = new GetSerialNumberResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, serialResponse.Status);
        }

        [Fact]
        public void Serial_GivenResponseApdu_NumberEqualsInput()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0x00, 0xBB, 0xCC, 0xDD, sw1, sw2 });

            var serialResponse = new GetSerialNumberResponse(responseApdu);

            int serialNum = serialResponse.GetData();

            Assert.Equal(0x00BBCCDD, serialNum);
        }

        [Fact]
        public void Serial_ExtraData_NumberEqualsFirstFour()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0x00, 0xBB, 0xCC, 0xDD, 0xEE, sw1, sw2 });

            var serialResponse = new GetSerialNumberResponse(responseApdu);

            int serialNum = serialResponse.GetData();

            Assert.Equal(0x00BBCCDD, serialNum);
        }

        [Fact]
        public void Constructor_FailResponseApdu_SetsStatusWordCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.WarningNvmUnchanged >> 8));
            byte sw2 = unchecked((byte)SWConstants.WarningNvmUnchanged);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var serialResponse = new GetSerialNumberResponse(responseApdu);

            Assert.Equal(SWConstants.WarningNvmUnchanged, serialResponse.StatusWord);
        }

        [Fact]
        public void Constructor_FailResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.WarningNvmUnchanged >> 8));
            byte sw2 = unchecked((byte)SWConstants.WarningNvmUnchanged);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var serialResponse = new GetSerialNumberResponse(responseApdu);

            Assert.Equal(ResponseStatus.Failed, serialResponse.Status);
        }

        [Fact]
        public void Construct_FailResponseApdu_ExceptionOnGetData()
        {
            byte sw1 = unchecked((byte)(SWConstants.WarningNvmUnchanged >> 8));
            byte sw2 = unchecked((byte)SWConstants.WarningNvmUnchanged);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var serialResponse = new GetSerialNumberResponse(responseApdu);

            _ = Assert.Throws<InvalidOperationException>(() => serialResponse.GetData());
        }

        [Fact]
        public void FailResponseApdu_WithData_ExceptionOnGetData()
        {
            byte sw1 = unchecked((byte)(SWConstants.WarningNvmUnchanged >> 8));
            byte sw2 = unchecked((byte)SWConstants.WarningNvmUnchanged);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, 0, sw1, sw2 });

            var serialResponse = new GetSerialNumberResponse(responseApdu);

            _ = Assert.Throws<InvalidOperationException>(() => serialResponse.GetData());
        }

        [Fact]
        public void Construct_ShortResponseApdu_ExceptionOnGetData()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, sw1, sw2 });

            var serialResponse = new GetSerialNumberResponse(responseApdu);

            _ = Assert.Throws<MalformedYubiKeyResponseException>(() => serialResponse.GetData());
        }
    }
}
