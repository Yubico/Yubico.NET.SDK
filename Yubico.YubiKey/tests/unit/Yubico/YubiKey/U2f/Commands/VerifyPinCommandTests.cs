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

        private readonly byte[] Pin = { 1, 2, 3, 4, 5, 6 };

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var command = new VerifyPinCommand(Pin);

            Assert.Equal(expected: 0, command.CreateCommandApdu().Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_Returns0x03()
        {
            var command = new VerifyPinCommand(Pin);

            Assert.Equal(expected: 0x03, command.CreateCommandApdu().Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsZero()
        {
            var command = new VerifyPinCommand(Pin);

            Assert.Equal(expected: 0, command.CreateCommandApdu().P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var command = new VerifyPinCommand(Pin);

            Assert.Equal(expected: 0, command.CreateCommandApdu().P2);
        }

        [Fact]
        public void CreateCommandApdu_GetNcProperty_ReturnsCorrectLength()
        {
            byte[] expectedInnerLc = { 0x00, 0x00, (byte)Pin.Length };
            var expectedCommandLength = lengthHeader + expectedInnerLc.Length + Pin.Length;

            var command = new VerifyPinCommand(Pin);
            var commandApdu = command.CreateCommandApdu();

            Assert.Equal(commandApdu.Nc, expectedCommandLength);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetClaProperty_ReturnsZero()
        {
            var command = new VerifyPinCommand(Pin);
            var commandApdu = command.CreateCommandApdu();

            var actualInnerCommandApdu = commandApdu.Data;
            var actualInnerCommandCla = actualInnerCommandApdu.Span[offsetCla];

            Assert.Equal(expected: 0, actualInnerCommandCla);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetInsProperty_Returns0x43()
        {
            var command = new VerifyPinCommand(Pin);
            var commandApdu = command.CreateCommandApdu();

            var actualInnerCommandApdu = commandApdu.Data;
            var actualInnerCommandIns = actualInnerCommandApdu.Span[offsetIns];

            Assert.Equal(expected: 0x43, actualInnerCommandIns);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetP1Property_ReturnsZero()
        {
            var command = new VerifyPinCommand(Pin);
            var commandApdu = command.CreateCommandApdu();

            var actualInnerCommandApdu = commandApdu.Data;
            var actualInnerCommandP1 = actualInnerCommandApdu.Span[offsetP1];

            Assert.Equal(expected: 0, actualInnerCommandP1);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetP2Property_ReturnsZero()
        {
            var command = new VerifyPinCommand(Pin);
            var commandApdu = command.CreateCommandApdu();

            var actualInnerCommandApdu = commandApdu.Data;
            var actualInnerCommandP2 = actualInnerCommandApdu.Span[offsetP2];

            Assert.Equal(expected: 0, actualInnerCommandP2);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetNcProperty_ReturnsCorrectLength()
        {
            byte[] expectedInnerLc = { 0x00, 0x00, (byte)Pin.Length };

            var command = new VerifyPinCommand(Pin);
            var commandApdu = command.CreateCommandApdu();

            var actualInnerCommandApdu = commandApdu.Data;
            var actualInnerCommandLc = actualInnerCommandApdu.Slice(offsetLc, lengthLc).Span;

            Assert.True(actualInnerCommandLc.SequenceEqual(expectedInnerLc));
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetData_ReturnsCorrectData()
        {
            var command = new VerifyPinCommand(Pin);
            var commandApdu = command.CreateCommandApdu();

            var actualInnerCommandApdu = commandApdu.Data;
            var actualInnerCommandData = actualInnerCommandApdu.Slice(offsetData, Pin.Length).Span;

            Assert.True(actualInnerCommandData.SequenceEqual(Pin));
        }

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var command = new VerifyPinCommand(Pin);
#pragma warning disable IDE0008 // Use explicit type
            var response = command.CreateResponseForApdu(responseApdu);
#pragma warning restore IDE0008 // Justification: testing the type

            _ = Assert.IsType<VerifyPinResponse>(response);
        }
    }
}
