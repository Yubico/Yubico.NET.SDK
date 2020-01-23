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
using System.Diagnostics.CodeAnalysis;
using Xunit;
using Yubico.Core.Iso7816;
using Yubico.Core.Buffers;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class VersionResponseTests
    {
        private static ResponseApdu GetResponseApdu() =>
            new ResponseApdu(Hex.HexToBytes("000102030405060708090A0B0C05030100"), SWConstants.Success);

        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => new VersionResponse(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var versionResponse = new VersionResponse(responseApdu);

            Assert.Equal(SWConstants.Success, versionResponse.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var versionResponse = new VersionResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, versionResponse.Status);
        }

        [Fact]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Unit test")]
        public void Constructor_SuccessResponseApdu_NoThrowIfFailed()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var versionResponse = new VersionResponse(responseApdu);

            versionResponse.ThrowIfFailed();
        }

        [Fact]
        public void Version_GivenResponseApdu_MajorEqualsFirstByte()
        {
            ResponseApdu responseApdu = GetResponseApdu();

            var versionResponse = new VersionResponse(responseApdu);

            Assert.Equal(5, versionResponse.GetData().Major);
        }

        [Fact]
        public void Version_GivenResponseApdu_MinorEqualsSecondByte()
        {
            ResponseApdu responseApdu = GetResponseApdu();

            var versionResponse = new VersionResponse(responseApdu);

            Assert.Equal(3, versionResponse.GetData().Minor);
        }

        [Fact]
        public void Version_GivenResponseApdu_PatchEqualsThirdByte()
        {
            ResponseApdu responseApdu = GetResponseApdu();

            var versionResponse = new VersionResponse(responseApdu);

            Assert.Equal(1, versionResponse.GetData().Patch);
        }

        [Fact]
        public void Constructor_FailResponseApdu_ThrowIfFailedCorrect()
        {
            var responseApdu = new ResponseApdu(new byte[] { (byte)Fido2Status.Ctap2ErrUnsupportedAlgorithm, SW1Constants.Success, 0x00 });

            var versionResponse = new VersionResponse(responseApdu);

            Action actual = () => versionResponse.ThrowIfFailed();
            Assert.Throws<BadFido2StatusException>(actual);
        }

        [Fact]
        public void FailResponseApdu_WithData_ExceptionOnGetData()
        {
            byte sw1 = unchecked((byte)(SWConstants.WarningNvmUnchanged >> 8));
            byte sw2 = unchecked((byte)SWConstants.WarningNvmUnchanged);
            var responseApdu = new ResponseApdu(new byte[] { 0, sw1, sw2 });

            var versionResponse = new VersionResponse(responseApdu);

            Action actual = () => versionResponse.GetData();
            Assert.Throws<InvalidOperationException>(actual);
        }

        [Fact]
        public void Construct_ShortResponseApdu_ExceptionOnGetData()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, sw1, sw2 });

            var versionResponse = new VersionResponse(responseApdu);

            Action actual = () => versionResponse.GetData();
            Assert.Throws<MalformedYubiKeyResponseException>(actual);
        }

    }
}
