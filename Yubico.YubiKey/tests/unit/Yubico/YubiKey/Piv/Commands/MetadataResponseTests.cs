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
    public class GetMetadataResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentNullException>(() => new GetMetadataResponse(null, 0x9C));
#pragma warning restore CS8625
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0x01, 0x01, 0xFF, 0x05, 0x01, 0x01, 0x06, 0x02, 0x05, 0x05, sw1, sw2 });

            var metadataResponse = new GetMetadataResponse(responseApdu, 0x80);

            Assert.Equal(SWConstants.Success, metadataResponse.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0x01, 0x01, 0xFF, 0x05, 0x01, 0x01, 0x06, 0x02, 0x05, 0x05, sw1, sw2 });

            var metadataResponse = new GetMetadataResponse(responseApdu, 0x80);

            Assert.Equal(ResponseStatus.Success, metadataResponse.Status);
        }

        [Fact]
        public void Constructor_DataNotFoundResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.DataNotFound >> 8));
            byte sw2 = unchecked((byte)SWConstants.DataNotFound);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var metadataResponse = new GetMetadataResponse(responseApdu, 0x9A);

            Assert.Equal(ResponseStatus.NoData, metadataResponse.Status);
        }

        [Fact]
        public void Construct_FailResponseApdu_ExceptionOnGetData()
        {
            byte sw1 = unchecked((byte)(SWConstants.WarningNvmUnchanged >> 8));
            byte sw2 = unchecked((byte)SWConstants.WarningNvmUnchanged);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var metadataResponse = new GetMetadataResponse(responseApdu, 0x88);

            _ = Assert.Throws<InvalidOperationException>(() => metadataResponse.GetData());
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_GetCorrectData()
        {
            byte[] testData = new byte[]
            {
                0x01, 0x01, 0x11, 0x02, 0x02, 0x03, 0x03, 0x03,
                0x01, 0x01, 0x04, 0x43, 0x86, 0x41, 0x04, 0xC4,
                0x17, 0x7F, 0x2B, 0x96, 0x8F, 0x9C, 0x00, 0x0C,
                0x4F, 0x3D, 0x2B, 0x88, 0xB0, 0xAB, 0x5B, 0x0C,
                0x3B, 0x19, 0x42, 0x63, 0x20, 0x8C, 0xA1, 0x2F,
                0xEE, 0x1C, 0xB4, 0xD8, 0x81, 0x96, 0x9F, 0xD8,
                0xC8, 0xD0, 0x8D, 0xD1, 0xBB, 0x66, 0x58, 0x00,
                0x26, 0x7D, 0x05, 0x34, 0xA8, 0xA3, 0x30, 0xD1,
                0x59, 0xDE, 0x66, 0x01, 0x0E, 0x3F, 0x21, 0x13,
                0x29, 0xC5, 0x98, 0x56, 0x07, 0xB5, 0x26,
                0x90, 0x00
            };

            var responseApdu = new ResponseApdu(testData);

            var metadataResponse = new GetMetadataResponse(responseApdu, 0x9C);
            PivMetadata pivMetadata = metadataResponse.GetData();

            Assert.True(pivMetadata is PivMetadata);
        }

        [Fact]
        public void Constructor_DataNotFoundResponseApdu_ThrowsException()
        {
            byte sw1 = unchecked((byte)(SWConstants.DataNotFound >> 8));
            byte sw2 = unchecked((byte)SWConstants.DataNotFound);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var metadataResponse = new GetMetadataResponse(responseApdu, 0x91);

            _ = Assert.Throws<InvalidOperationException>(() => metadataResponse.GetData());
        }
    }
}
