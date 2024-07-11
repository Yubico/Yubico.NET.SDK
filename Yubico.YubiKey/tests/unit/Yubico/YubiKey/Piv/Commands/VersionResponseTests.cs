﻿// Copyright 2021 Yubico AB
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
    public class VersionResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentNullException>(() => new VersionResponse(responseApdu: null));
#pragma warning restore CS8625
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var versionResponse = new VersionResponse(responseApdu);

            Assert.Equal(SWConstants.Success, versionResponse.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var versionResponse = new VersionResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, versionResponse.Status);
        }

        [Fact]
        public void Version_GivenResponseApdu_MajorEqualsFirstByte()
        {
            var responseApdu = new ResponseApdu(new byte[] { 23, 45, 31, 0x90, 0x00 });

            var versionResponse = new VersionResponse(responseApdu);

            Assert.Equal(expected: 23, versionResponse.GetData().Major);
        }

        [Fact]
        public void Version_GivenResponseApdu_MinorEqualsSecondByte()
        {
            var responseApdu = new ResponseApdu(new byte[] { 42, 12, 45, 0x90, 0x00 });

            var versionResponse = new VersionResponse(responseApdu);

            Assert.Equal(expected: 12, versionResponse.GetData().Minor);
            Assert.Equal(expected: 12, versionResponse.GetData().Minor);
        }

        [Fact]
        public void Version_GivenResponseApdu_PatchEqualsThirdByte()
        {
            var responseApdu = new ResponseApdu(new byte[] { 25, 57, 97, 0x90, 0x00 });

            var versionResponse = new VersionResponse(responseApdu);

            Assert.Equal(expected: 97, versionResponse.GetData().Patch);
        }

        [Fact]
        public void Constructor_FailResponseApdu_SetsStatusWordCorrectly()
        {
            var sw1 = (byte)(SWConstants.WarningNvmUnchanged >> 8);
            var sw2 = unchecked((byte)SWConstants.WarningNvmUnchanged);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var versionResponse = new VersionResponse(responseApdu);

            Assert.Equal(SWConstants.WarningNvmUnchanged, versionResponse.StatusWord);
        }

        [Fact]
        public void Constructor_FailResponseApdu_SetsStatusCorrectly()
        {
            var sw1 = (byte)(SWConstants.WarningNvmUnchanged >> 8);
            var sw2 = unchecked((byte)SWConstants.WarningNvmUnchanged);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var versionResponse = new VersionResponse(responseApdu);

            Assert.Equal(ResponseStatus.Failed, versionResponse.Status);
        }

        [Fact]
        public void Construct_FailResponseApdu_ExceptionOnGetData()
        {
            var sw1 = (byte)(SWConstants.WarningNvmUnchanged >> 8);
            var sw2 = unchecked((byte)SWConstants.WarningNvmUnchanged);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var versionResponse = new VersionResponse(responseApdu);

            _ = Assert.Throws<InvalidOperationException>(() => versionResponse.GetData());
        }

        [Fact]
        public void FailResponseApdu_WithData_ExceptionOnGetData()
        {
            var sw1 = (byte)(SWConstants.WarningNvmUnchanged >> 8);
            var sw2 = unchecked((byte)SWConstants.WarningNvmUnchanged);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var versionResponse = new VersionResponse(responseApdu);

            _ = Assert.Throws<InvalidOperationException>(() => versionResponse.GetData());
        }

        [Fact]
        public void Construct_ShortResponseApdu_ExceptionOnGetData()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, sw1, sw2 });

            var versionResponse = new VersionResponse(responseApdu);

            _ = Assert.Throws<MalformedYubiKeyResponseException>(() => versionResponse.GetData());
        }
    }
}
