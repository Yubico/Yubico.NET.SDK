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
    public class ReadNdefDataResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            static void Action()
            {
                _ = new ReadNdefDataResponse(responseApdu: null);
            }
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            _ = Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, sw1, sw2 });

            var readNdefDataResponse = new ReadNdefDataResponse(responseApdu);

            Assert.Equal(SWConstants.Success, readNdefDataResponse.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, sw1, sw2 });

            var readNdefDataResponse = new ReadNdefDataResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, readNdefDataResponse.Status);
        }

        [Fact]
        public void GetData_ResponseApduFailed_ThrowsInvalidOperationException()
        {
            var responseApdu = new ResponseApdu(new byte[] { SW1Constants.NoPreciseDiagnosis, 0x00 });
            var readNdefDataResponse = new ReadNdefDataResponse(responseApdu);

            void Action()
            {
                _ = readNdefDataResponse.GetData();
            }

            _ = Assert.Throws<InvalidOperationException>(Action);
        }

        [Fact]
        public void GetData_SuccessResponseApdu_ReturnsData()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 1, 2, 3, sw1, sw2 });
            var readNdefStatusResponse = new ReadNdefDataResponse(responseApdu);

            var data = readNdefStatusResponse.GetData();

            Assert.True(data.Span.SequenceEqual(new byte[] { 1, 2, 3 }));
        }
    }
}
