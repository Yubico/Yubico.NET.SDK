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

namespace Yubico.YubiKey.Otp.Commands
{
    public class QueryFipsModeResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            static void action() => _ = new QueryFipsModeResponse(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            _ = Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var queryFipsModeResponse = new QueryFipsModeResponse(responseApdu);

            Assert.Equal(SWConstants.Success, queryFipsModeResponse.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var queryFipsModeResponse = new QueryFipsModeResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, queryFipsModeResponse.Status);
        }

        [Fact]
        public void GetData_ResponseApduFailed_ThrowsInvalidOperationException()
        {
            var responseApdu = new ResponseApdu(new byte[] { SW1Constants.NoPreciseDiagnosis, 0x00 });
            var queryFipsModeResponse = new QueryFipsModeResponse(responseApdu);

            void action() => _ = queryFipsModeResponse.GetData();

            _ = Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void GetData_ResponseApduWithWrongSizedBuffer_ThrowsMalformedYubiKeyResponseException()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x00, 0x00, 0x90, 0x00 });
            var queryFipsModeResponse = new QueryFipsModeResponse(responseApdu);

            void action() => _ = queryFipsModeResponse.GetData();

            _ = Assert.Throws<MalformedYubiKeyResponseException>(action);
        }

        [Fact]
        public void GetData_ResponseApduWithModeSetToZero_ReturnsFalse()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x00, 0x90, 0x00 });
            var queryFipsModeResponse = new QueryFipsModeResponse(responseApdu);

            bool fipsMode = queryFipsModeResponse.GetData();

            Assert.False(fipsMode);
        }

        [Fact]
        public void GetData_ResponseApduWithModeSetToOne_ReturnsTrue()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x01, 0x90, 0x00 });
            var queryFipsModeResponse = new QueryFipsModeResponse(responseApdu);

            bool fipsMode = queryFipsModeResponse.GetData();

            Assert.True(fipsMode);
        }
    }
}
