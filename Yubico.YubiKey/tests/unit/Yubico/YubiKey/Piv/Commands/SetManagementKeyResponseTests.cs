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
    public class SetManagementKeyResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentNullException>(() => new SetManagementKeyResponse(null));
#pragma warning restore CS8625
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var setManagementKeyResponse = new SetManagementKeyResponse(responseApdu);

            Assert.Equal(SWConstants.Success, setManagementKeyResponse.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var setManagementKeyResponse = new SetManagementKeyResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, setManagementKeyResponse.Status);
        }

        [Fact]
        public void Constructor_AuthReqdResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.SecurityStatusNotSatisfied >> 8));
            byte sw2 = unchecked((byte)SWConstants.SecurityStatusNotSatisfied);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var setManagementKeyResponse = new SetManagementKeyResponse(responseApdu);

            Assert.Equal(ResponseStatus.AuthenticationRequired, setManagementKeyResponse.Status);
        }

        [Fact]
        public void Constructor_ErrorResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.FunctionNotSupported >> 8));
            byte sw2 = unchecked((byte)SWConstants.FunctionNotSupported);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var setManagementKeyResponse = new SetManagementKeyResponse(responseApdu);

            Assert.Equal(ResponseStatus.Failed, setManagementKeyResponse.Status);
        }
    }
}
