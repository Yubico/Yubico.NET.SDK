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
    public class PutDataResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentNullException>(() => new PutDataResponse(responseApdu: null));
#pragma warning restore CS8625
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new PutDataResponse(responseApdu);

            Assert.Equal(SWConstants.Success, response.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new PutDataResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, response.Status);
        }

        [Fact]
        public void Constructor_ErrorResponseApdu_SetsStatusCorrectly()
        {
            var sw1 = (byte)(SWConstants.FunctionNotSupported >> 8);
            var sw2 = unchecked((byte)SWConstants.FunctionNotSupported);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new PutDataResponse(responseApdu);

            Assert.Equal(ResponseStatus.Failed, response.Status);
        }

        [Fact]
        public void Constructor_AuthFailed_SetsStatusCorrectly()
        {
            var sw1 = (byte)(SWConstants.SecurityStatusNotSatisfied >> 8);
            var sw2 = unchecked((byte)SWConstants.SecurityStatusNotSatisfied);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new PutDataResponse(responseApdu);

            Assert.Equal(ResponseStatus.AuthenticationRequired, response.Status);
        }
    }
}
