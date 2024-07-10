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
    public class ChangeRefResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentNullException>(() => new ChangeReferenceDataResponse(responseApdu: null));
#pragma warning restore CS8625
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var changeRefDataResponse = new ChangeReferenceDataResponse(responseApdu);

            Assert.Equal(SWConstants.Success, changeRefDataResponse.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var changeRefDataResponse = new ChangeReferenceDataResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, changeRefDataResponse.Status);
        }

        [Fact]
        public void Constructor_WrongPinResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = 0x63;
            byte sw2 = 0xC1;
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var changeRefDataResponse = new ChangeReferenceDataResponse(responseApdu);

            Assert.Equal(ResponseStatus.AuthenticationRequired, changeRefDataResponse.Status);
        }

        [Fact]
        public void Constructor_AuthenticationMethodBlockedResponseApdu_SetsStatusCorrectly()
        {
            var statusWord = SWConstants.AuthenticationMethodBlocked;
            var responseApdu = new ResponseApdu(Array.Empty<byte>(), statusWord);

            var changeRefDataResponse = new ChangeReferenceDataResponse(responseApdu);

            Assert.Equal(ResponseStatus.AuthenticationRequired, changeRefDataResponse.Status);
        }

        [Fact]
        public void Constructor_SecurityStatusNotSatisfiedResponseApdu_SetsStatusCorrectly()
        {
            var statusWord = SWConstants.SecurityStatusNotSatisfied;
            var responseApdu = new ResponseApdu(Array.Empty<byte>(), statusWord);

            var changeRefDataResponse = new ChangeReferenceDataResponse(responseApdu);

            Assert.Equal(ResponseStatus.AuthenticationRequired, changeRefDataResponse.Status);
        }

        [Fact]
        public void Constructor_ErrorResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = SWConstants.ExecutionError >> 8;
            var sw2 = unchecked((byte)SWConstants.ExecutionError);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var changeRefDataResponse = new ChangeReferenceDataResponse(responseApdu);

            Assert.Equal(ResponseStatus.Failed, changeRefDataResponse.Status);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_GetDataCorrectInt()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var changeRefDataResponse = new ChangeReferenceDataResponse(responseApdu);

            var retryCount = changeRefDataResponse.GetData();

            Assert.Null(retryCount);
        }

        [Fact]
        public void Constructor_BadPinResponseApdu_GetDataCorrectInt()
        {
            byte sw1 = 0x63;
            byte sw2 = 0xCA;
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var changeRefDataResponse = new ChangeReferenceDataResponse(responseApdu);

            var retryCount = changeRefDataResponse.GetData();

            Assert.Equal(expected: 10, retryCount);
        }

        [Fact]
        public void Constructor_BlockedPinResponseApdu_GetDataCorrectInt()
        {
            byte sw1 = SWConstants.AuthenticationMethodBlocked >> 8;
            var sw2 = unchecked((byte)SWConstants.AuthenticationMethodBlocked);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var changeRefDataResponse = new ChangeReferenceDataResponse(responseApdu);

            var retryCount = changeRefDataResponse.GetData();

            Assert.Equal(expected: 0, retryCount);
        }

        [Fact]
        public void GetData_SuccessResponseApdu_NoExceptionThrown()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var changeRefDataResponse = new ChangeReferenceDataResponse(responseApdu);

            void action()
            {
                changeRefDataResponse.GetData();
            }

            var ex = Record.Exception(action);
            Assert.Null(ex);
        }

        [Fact]
        public void GetData_WrongPinResponseApdu_NoExceptionThrown()
        {
            byte sw1 = 0x63;
            byte sw2 = 0xC2;
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var changeRefDataResponse = new ChangeReferenceDataResponse(responseApdu);

            void action()
            {
                changeRefDataResponse.GetData();
            }

            var ex = Record.Exception(action);
            Assert.Null(ex);
        }

        [Fact]
        public void GetData_FailResponseApdu_ThrowsException()
        {
            byte sw1 = SWConstants.RecordNotFound >> 8;
            var sw2 = unchecked((byte)SWConstants.RecordNotFound);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var changeRefDataResponse = new ChangeReferenceDataResponse(responseApdu);

            _ = Assert.Throws<InvalidOperationException>(() => changeRefDataResponse.GetData());
        }
    }
}
