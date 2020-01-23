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
    public class VerifyPinCommandTests
    {
        private const int offsetCla = 0;
        private const int offsetIns = 1;
        private const int offsetP1 = 2;
        private const int offsetP2 = 3;
        private const int lengthHeader = 4; // APDU header is 4 bytes (Cla, Ins, P1, P2)
        private const int offsetLc = 4;
        private const int lengthLc = 3;

        private const int offsetData = offsetLc + lengthLc;

        private readonly byte[] Pin = new byte[] { 1, 2, 3, 4, 5, 6 };

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var command = new VerifyPinCommand(Pin);

            Assert.Equal(0, command.CreateCommandApdu().Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_Returns0x03()
        {
            var command = new VerifyPinCommand(Pin);

            Assert.Equal(0x03, command.CreateCommandApdu().Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsZero()
        {
            var command = new VerifyPinCommand(Pin);

            Assert.Equal(0, command.CreateCommandApdu().P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var command = new VerifyPinCommand(Pin);

            Assert.Equal(0, command.CreateCommandApdu().P2);
        }

        [Fact]
        public void CreateCommandApdu_GetNcProperty_ReturnsCorrectLength()
        {
            byte[] expectedInnerLc = new byte[] { 0x00, 0x00, (byte)Pin.Length };
            int expectedCommandLength = lengthHeader + expectedInnerLc.Length + Pin.Length;

            var command = new VerifyPinCommand(Pin);
            CommandApdu commandApdu = command.CreateCommandApdu();

            Assert.Equal(commandApdu.Nc, expectedCommandLength);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetClaProperty_ReturnsZero()
        {
            var command = new VerifyPinCommand(Pin);
            CommandApdu commandApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> actualInnerCommandApdu = commandApdu.Data;
            byte actualInnerCommandCla = actualInnerCommandApdu.Span[offsetCla];

            Assert.Equal(0, actualInnerCommandCla);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetInsProperty_Returns0x43()
        {
            var command = new VerifyPinCommand(Pin);
            CommandApdu commandApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> actualInnerCommandApdu = commandApdu.Data;
            byte actualInnerCommandIns = actualInnerCommandApdu.Span[offsetIns];

            Assert.Equal(0x43, actualInnerCommandIns);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetP1Property_ReturnsZero()
        {
            var command = new VerifyPinCommand(Pin);
            CommandApdu commandApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> actualInnerCommandApdu = commandApdu.Data;
            byte actualInnerCommandP1 = actualInnerCommandApdu.Span[offsetP1];

            Assert.Equal(0, actualInnerCommandP1);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetP2Property_ReturnsZero()
        {
            var command = new VerifyPinCommand(Pin);
            CommandApdu commandApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> actualInnerCommandApdu = commandApdu.Data;
            byte actualInnerCommandP2 = actualInnerCommandApdu.Span[offsetP2];

            Assert.Equal(0, actualInnerCommandP2);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetNcProperty_ReturnsCorrectLength()
        {
            byte[] expectedInnerLc = new byte[] { 0x00, 0x00, (byte)Pin.Length };

            var command = new VerifyPinCommand(Pin);
            CommandApdu commandApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> actualInnerCommandApdu = commandApdu.Data;
            ReadOnlySpan<byte> actualInnerCommandLc = actualInnerCommandApdu.Slice(offsetLc, lengthLc).Span;

            Assert.True(actualInnerCommandLc.SequenceEqual(expectedInnerLc));
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetData_ReturnsCorrectData()
        {
            var command = new VerifyPinCommand(Pin);
            CommandApdu commandApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> actualInnerCommandApdu = commandApdu.Data;
            ReadOnlySpan<byte> actualInnerCommandData = actualInnerCommandApdu.Slice(offsetData, Pin.Length).Span;

            Assert.True(actualInnerCommandData.SequenceEqual(Pin));
        }

        [Fact]
        public void CreateCommandApdu_PinIsEmpty_ThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => new VerifyPinCommand(Array.Empty<byte>()));
        }

        [Fact]
        public void CreateCommandApdu_PinIsNull_ThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => new VerifyPinCommand(null));
        }

        [Fact]
        public void CreateCommandApdu_PinLengthLessThan6_ThrowsArgumentException()
        {
            var pin = new byte[] { 1, 2, 3, 4 };

            _ = Assert.Throws<ArgumentException>(() => new VerifyPinCommand(pin));
        }

        [Fact]
        public void CreateCommandApdu_PinLengthMoreThan32_ThrowsArgumentException()
        {
            var pin = new byte[33];

            _ = Assert.Throws<ArgumentException>(() => new VerifyPinCommand(pin));
        }

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var command = new VerifyPinCommand(Pin);
            var response = command.CreateResponseForApdu(responseApdu);

            _ = Assert.IsType<U2fResponse>(response);
        }
    }
}
