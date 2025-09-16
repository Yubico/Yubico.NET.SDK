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

namespace Yubico.YubiKey.Piv.Commands
{
    public class ResetRetryResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentNullException>(() => new ResetRetryResponse(null));
#pragma warning restore CS8625
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var resetRetryResponse = new ResetRetryResponse(responseApdu);

            Assert.Equal(SWConstants.Success, resetRetryResponse.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var resetRetryResponse = new ResetRetryResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, resetRetryResponse.Status);
        }

        [Fact]
        public void Constructor_WrongPinResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = 0x63;
            byte sw2 = 0xC1;
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var resetRetryResponse = new ResetRetryResponse(responseApdu);

            Assert.Equal(ResponseStatus.AuthenticationRequired, resetRetryResponse.Status);
        }

        [Fact]
        public void Constructor_AuthenticationMethodBlockedResponseApdu_SetsStatusCorrectly()
        {
            short statusWord = SWConstants.AuthenticationMethodBlocked;
            var responseApdu = new ResponseApdu(Array.Empty<byte>(), statusWord);

            var changeRefDataResponse = new ResetRetryResponse(responseApdu);

            Assert.Equal(ResponseStatus.AuthenticationRequired, changeRefDataResponse.Status);
        }

        [Fact]
        public void Constructor_SecurityStatusNotSatisfiedResponseApdu_SetsStatusCorrectly()
        {
            short statusWord = SWConstants.SecurityStatusNotSatisfied;
            var responseApdu = new ResponseApdu(Array.Empty<byte>(), statusWord);

            var changeRefDataResponse = new ResetRetryResponse(responseApdu);

            Assert.Equal(ResponseStatus.AuthenticationRequired, changeRefDataResponse.Status);
        }

        [Fact]
        public void Constructor_ErrorResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.ExecutionError >> 8));
            byte sw2 = unchecked((byte)SWConstants.ExecutionError);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var resetRetryResponse = new ResetRetryResponse(responseApdu);

            Assert.Equal(ResponseStatus.Failed, resetRetryResponse.Status);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_GetDataCorrectInt()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var resetRetryResponse = new ResetRetryResponse(responseApdu);

            int? retryCount = resetRetryResponse.GetData();

            Assert.Null(retryCount);
        }

        [Fact]
        public void Constructor_BadPinResponseApdu_GetDataCorrectInt()
        {
            byte sw1 = 0x63;
            byte sw2 = 0xCF;
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var resetRetryResponse = new ResetRetryResponse(responseApdu);

            int? retryCount = resetRetryResponse.GetData();

            Assert.Equal(15, retryCount);
        }

        [Fact]
        public void Constructor_BlockedPinResponseApdu_GetDataCorrectInt()
        {
            byte sw1 = unchecked((byte)(SWConstants.AuthenticationMethodBlocked >> 8));
            byte sw2 = unchecked((byte)SWConstants.AuthenticationMethodBlocked);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var resetRetryResponse = new ResetRetryResponse(responseApdu);

            int? retryCount = resetRetryResponse.GetData();

            Assert.Equal(0, retryCount);
        }

        [Fact]
        public void Constructor_FailResponseApdu_ThrowIfGetData()
        {
            byte sw1 = unchecked((byte)(SWConstants.RecordNotFound >> 8));
            byte sw2 = unchecked((byte)SWConstants.RecordNotFound);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var resetRetryResponse = new ResetRetryResponse(responseApdu);

            _ = Assert.Throws<InvalidOperationException>(() => resetRetryResponse.GetData());
        }
    }
}
