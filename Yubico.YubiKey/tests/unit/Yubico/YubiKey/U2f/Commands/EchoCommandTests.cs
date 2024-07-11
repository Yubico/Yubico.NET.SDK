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
using Yubico.Core.Buffers;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    public class EchoCommandTests
    {
        private const int offsetCla = 0;
        private const int offsetIns = 1;
        private const int offsetP1 = 2;
        private const int offsetP2 = 3;

        private const int lengthHeader = 4; // APDU header is 4 bytes (Cla, Ins, P1, P2)

        private const int offsetLc = 4;
        private const int lengthLc = 3;

        private const int offsetData = offsetLc + lengthLc;

        // Data set/get & constructors

        [Fact]
        public void Data_PropertySetGetNonEmptyArray_ReturnsCorrectArray()
        {
            ReadOnlyMemory<byte> expectedData = new byte[] { 0x01, 0x02, 0x03 };

            var command = new EchoCommand
            {
                Data = expectedData
            };

            Assert.True(command.Data.Span.SequenceEqual(expectedData.Span));
        }

        [Fact]
        public void Data_DefaultConstructorDefaultValue_ReturnsEmptyArray()
        {
            ReadOnlyMemory<byte> expectedData = ReadOnlyMemory<byte>.Empty;

            var command = new EchoCommand();

            Assert.True(command.Data.Span.SequenceEqual(expectedData.Span));
        }

        [Fact]
        public void Data_NonDefaultConstructorSetGetNonEmptyArray_ReturnsCorrectArray()
        {
            ReadOnlyMemory<byte> expectedData = new byte[] { 0x01, 0x02, 0x03 };

            var command = new EchoCommand(expectedData);

            Assert.True(command.Data.Span.SequenceEqual(expectedData.Span));
        }

        [Fact]
        public void Application_DefaultValue_ReturnsFidoU2F()
        {
            YubiKeyApplication expectedApplication = YubiKeyApplication.FidoU2f;

            var command = new EchoCommand();

            Assert.Equal(command.Application, expectedApplication);
        }

        // CreateCommandApdu - outer command

        [Theory]
        [InlineData("")]
        [InlineData("0102030405")]
        public void CreateCommandApdu_SetData_OuterCommandCla0x00(string expectedData)
        {
            byte expectedCla = 0;

            var command = new EchoCommand(Hex.HexToBytes(expectedData));
            CommandApdu commandApdu = command.CreateCommandApdu();

            Assert.Equal(commandApdu.Cla, expectedCla);
        }

        [Theory]
        [InlineData("")]
        [InlineData("0102030405")]
        public void CreateCommandApdu_SetData_OuterCommandIns0x03(string expectedData)
        {
            byte expectedIns = 0x03;

            var command = new EchoCommand(Hex.HexToBytes(expectedData));
            CommandApdu commandApdu = command.CreateCommandApdu();

            Assert.Equal(commandApdu.Ins, expectedIns);
        }

        [Theory]
        [InlineData("")]
        [InlineData("0102030405")]
        public void CreateCommandApdu_SetData_OuterCommandP1Hex00(string expectedData)
        {
            byte expectedP1 = 0;

            var command = new EchoCommand(Hex.HexToBytes(expectedData));
            CommandApdu commandApdu = command.CreateCommandApdu();

            Assert.Equal(commandApdu.P1, expectedP1);
        }

        [Theory]
        [InlineData("")]
        [InlineData("0102030405")]
        public void CreateCommandApdu_SetData_OuterCommandP2Hex00(string expectedData)
        {
            byte expectedP2 = 0;

            var command = new EchoCommand(Hex.HexToBytes(expectedData));
            CommandApdu commandApdu = command.CreateCommandApdu();

            Assert.Equal(commandApdu.P2, expectedP2);
        }

        [Fact]
        public void CreateCommandApdu_SetEmptyData_OuterCommandNcCorrect()
        {
            byte[] expectedInnerData = Array.Empty<byte>();
            byte[] expectedInnerLc = Array.Empty<byte>();

            int expectedInnerCommandLength = lengthHeader + expectedInnerLc.Length + expectedInnerData.Length;

            var command = new EchoCommand(expectedInnerData);
            CommandApdu commandApdu = command.CreateCommandApdu();

            Assert.Equal(commandApdu.Nc, expectedInnerCommandLength);
        }

        [Fact]
        public void CreateCommandApdu_SetNonEmptyData_OuterCommandLcCorrect()
        {
            byte[] expectedInnerData = new byte[] { 0x01, 0x02, 0x03 };
            byte[] expectedInnerLc = new byte[] { 0x00, 0x00, (byte)expectedInnerData.Length };  // Assumes 0 < len < 256

            int expectedInnerCommandLength = lengthHeader + expectedInnerLc.Length + expectedInnerData.Length;

            var command = new EchoCommand(expectedInnerData);
            CommandApdu commandApdu = command.CreateCommandApdu();

            Assert.Equal(commandApdu.Nc, expectedInnerCommandLength);
        }

        // CreateCommandApdu - inner command

        [Theory]
        [InlineData("")]
        [InlineData("0102030405")]
        public void CreateCommandApdu_SetData_InnerCommandCla0x00(string expectedData)
        {
            byte expectedInnerCla = 0;

            var command = new EchoCommand(Hex.HexToBytes(expectedData));
            CommandApdu commandApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> actualInnerCommandApdu = commandApdu.Data;
            byte actualInnerCommandCla = actualInnerCommandApdu.Span[offsetCla];

            Assert.Equal(actualInnerCommandCla, expectedInnerCla);
        }

        [Theory]
        [InlineData("")]
        [InlineData("0102030405")]
        public void CreateCommandApdu_SetData_InnerCommandIns0x40(string expectedData)
        {
            byte expectedInnerIns = 0x40;

            var command = new EchoCommand(Hex.HexToBytes(expectedData));
            CommandApdu commandApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> actualInnerCommandApdu = commandApdu.Data;
            byte actualInnerCommandIns = actualInnerCommandApdu.Span[offsetIns];

            Assert.Equal(actualInnerCommandIns, expectedInnerIns);
        }

        [Theory]
        [InlineData("")]
        [InlineData("0102030405")]
        public void CreateCommandApdu_SetData_InnerCommandP1Hex00(string expectedData)
        {
            byte expectedInnerP1 = 0;

            var command = new EchoCommand(Hex.HexToBytes(expectedData));
            CommandApdu commandApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> actualInnerCommandApdu = commandApdu.Data;
            byte actualInnerCommandP1 = actualInnerCommandApdu.Span[offsetP1];

            Assert.Equal(actualInnerCommandP1, expectedInnerP1);
        }

        [Theory]
        [InlineData("")]
        [InlineData("0102030405")]
        public void CreateCommandApdu_SetData_InnerCommandP2Hex00(string expectedData)
        {
            byte expectedInnerP2 = 0;

            var command = new EchoCommand(Hex.HexToBytes(expectedData));
            CommandApdu commandApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> actualInnerCommandApdu = commandApdu.Data;
            byte actualInnerCommandP2 = actualInnerCommandApdu.Span[offsetP2];

            Assert.Equal(actualInnerCommandP2, expectedInnerP2);
        }

        [Fact]
        public void CreateCommandApdu_SetNonEmptyData_InnerCommandLcCorrect()
        {
            byte[] expectedInnerData = new byte[] { 0x01, 0x02, 0x03 };
            byte[] expectedInnerLc = new byte[] { 0x00, 0x00, (byte)expectedInnerData.Length };  // Assumes 0 < len < 256

            var command = new EchoCommand(expectedInnerData);
            CommandApdu commandApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> actualInnerCommandApdu = commandApdu.Data.ToArray();
            ReadOnlySpan<byte> actualInnerCommandLc = actualInnerCommandApdu.Slice(offsetLc, lengthLc).Span;

            Assert.True(actualInnerCommandLc.SequenceEqual(expectedInnerLc));
        }

        [Fact]
        public void CreateCommandApdu_SetNonEmptyData_InnerCommandDataCorrect()
        {
            byte[] expectedInnerData = new byte[] { 0x01, 0x02, 0x03 };

            var command = new EchoCommand(expectedInnerData);
            CommandApdu commandApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> actualInnerCommandApdu = commandApdu.Data;
            ReadOnlySpan<byte> actualInnerCommandData = actualInnerCommandApdu.Slice(offsetData, expectedInnerData.Length).Span;

            Assert.True(actualInnerCommandData.SequenceEqual(expectedInnerData));
        }

        // CreateResponseForApdu
        [Theory]
        [InlineData("")]
        [InlineData("0102030405")]
        public void CreateResponseForApdu_SetDataSuccessSW_ReturnsCorrectEchoResponse(string expectedResponseDataString)
        {
            byte[] expectedResponseDataBytes = Hex.HexToBytes(expectedResponseDataString);
            short sw = SWConstants.Success;

            var responseApdu = new ResponseApdu(expectedResponseDataBytes, sw);

            var command = new EchoCommand();
            EchoResponse echoResponse = command.CreateResponseForApdu(responseApdu);
            ReadOnlyMemory<byte> actualResponseDataBytes = echoResponse.GetData();

            Assert.True(actualResponseDataBytes.Span.SequenceEqual(expectedResponseDataBytes));
        }

        [Theory]
        [InlineData("")]
        [InlineData("0102030405")]
        public void CreateResponseForApdu_SetDataFailedSW_ReturnsCorrectEchoResponse(string expectedResponseDataString)
        {
            byte[] expectedResponseDataBytes = Hex.HexToBytes(expectedResponseDataString);
            short expectedSW = SWConstants.FunctionError;

            var responseApdu = new ResponseApdu(expectedResponseDataBytes, expectedSW);

            var command = new EchoCommand();
            EchoResponse echoResponse = command.CreateResponseForApdu(responseApdu);
            short actualSW = echoResponse.StatusWord;

            Assert.Equal(actualSW, expectedSW);
        }
    }
}
