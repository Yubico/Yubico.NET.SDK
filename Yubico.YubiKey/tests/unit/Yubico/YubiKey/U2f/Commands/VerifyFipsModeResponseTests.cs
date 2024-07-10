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

namespace Yubico.YubiKey.U2f.Commands
{
    public class VerifyFipsModeResponseTests
    {
        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new VerifyFipsModeResponse(responseApdu);

            Assert.Equal(SWConstants.Success, response.StatusWord);
        }

        [Fact]
        public void Constructor_FunctionNotSupportedResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = SWConstants.FunctionNotSupported >> 8;
            var sw2 = unchecked((byte)SWConstants.FunctionNotSupported);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new VerifyFipsModeResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, response.Status);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_NoThrowIfFailed()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new VerifyFipsModeResponse(responseApdu);

            void action()
            {
                response.GetData();
            }

            var ex = Record.Exception(action);
            Assert.Null(ex);
        }

        [Fact]
        public void Constructor_FunctionNotSupportedResponseApdu_NoThrowIfFailed()
        {
            byte sw1 = SWConstants.FunctionNotSupported >> 8;
            var sw2 = unchecked((byte)SWConstants.FunctionNotSupported);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new VerifyFipsModeResponse(responseApdu);

            void action()
            {
                response.GetData();
            }

            var ex = Record.Exception(action);
            Assert.Null(ex);
        }

        [Fact]
        public void ResponseApduFailed_ThrowsException()
        {
            byte sw1 = SWConstants.FunctionError >> 8;
            var sw2 = unchecked((byte)SWConstants.FunctionError);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new VerifyFipsModeResponse(responseApdu);

            _ = Assert.Throws<InvalidOperationException>(() => response.GetData());
        }

        [Fact]
        public void Constructor_FunctionNotSupportedResponseApdu_GetCorrectData()
        {
            byte sw1 = SWConstants.FunctionNotSupported >> 8;
            var sw2 = unchecked((byte)SWConstants.FunctionNotSupported);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new VerifyFipsModeResponse(responseApdu);

            Assert.False(response.GetData());
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_GetCorrectData()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new VerifyFipsModeResponse(responseApdu);

            Assert.True(response.GetData());
        }
    }
}
