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
using System.Linq;
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    public class GetDataResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentNullException>(() => new GetDataResponse(null));
#pragma warning restore CS8625
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0x30, 0x01, 0x05, sw1, sw2 });

            var response = new GetDataResponse(responseApdu);

            Assert.Equal(SWConstants.Success, response.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0x30, 0x01, 0x05, sw1, sw2 });

            var response = new GetDataResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, response.Status);
        }

        [Fact]
        public void Constructor_DataNotFoundResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.FileOrApplicationNotFound >> 8));
            byte sw2 = unchecked((byte)SWConstants.FileOrApplicationNotFound);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var response = new GetDataResponse(responseApdu);

            Assert.Equal(ResponseStatus.NoData, response.Status);
        }

        [Fact]
        public void Construct_FailResponseApdu_ExceptionOnGetData()
        {
            byte sw1 = unchecked((byte)(SWConstants.WarningNvmUnchanged >> 8));
            byte sw2 = unchecked((byte)SWConstants.WarningNvmUnchanged);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var response = new GetDataResponse(responseApdu);

            _ = Assert.Throws<InvalidOperationException>(() => response.GetData());
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_GetCorrectData()
        {
            byte[] testData = new byte[]
            {
                0x01, 0x01, 0x11, 0x02, 0x02, 0x03, 0x03, 0x90, 0x00
            };

            var expected = new Span<byte>(testData);
            expected = expected.Slice(0, testData.Length - 2);
            var responseApdu = new ResponseApdu(testData);

            var response = new GetDataResponse(responseApdu);

            ReadOnlyMemory<byte> getData = response.GetData();

            bool compareResult = expected.SequenceEqual(getData.Span);

            Assert.True(compareResult);
        }
    }
}
