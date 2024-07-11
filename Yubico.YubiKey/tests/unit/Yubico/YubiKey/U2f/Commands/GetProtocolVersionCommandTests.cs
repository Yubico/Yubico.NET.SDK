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
    public class GetProtocolVersionCommandTests
    {
        private const int offsetCla = 0;
        private const int offsetIns = 1;
        private const int offsetP1 = 2;
        private const int offsetP2 = 3;

        private const int lengthHeader = 4; // APDU header is 4 bytes (Cla, Ins, P1, P2)

        private const int offsetLc = 4;
        private const int lengthLc = 3;

        private const int offsetData = offsetLc + lengthLc;

        #region Outer APDU

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var command = new GetProtocolVersionCommand();

            Assert.Equal(0, command.CreateCommandApdu().Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_Returns0x03()
        {
            var command = new GetProtocolVersionCommand();

            Assert.Equal(0x03, command.CreateCommandApdu().Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsZero()
        {
            var command = new GetProtocolVersionCommand();

            Assert.Equal(0, command.CreateCommandApdu().P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var command = new GetProtocolVersionCommand();

            Assert.Equal(0, command.CreateCommandApdu().P2);
        }

        [Fact]
        public void CreateCommandApdu_GetNcProperty_Returns4()
        {
            byte[] expectedInnerData = Array.Empty<byte>();
            byte[] expectedInnerLc = Array.Empty<byte>();

            int expectedInnerCommandLength = lengthHeader + expectedInnerLc.Length + expectedInnerData.Length;

            var command = new EchoCommand(expectedInnerData);
            CommandApdu commandApdu = command.CreateCommandApdu();

            Assert.Equal(commandApdu.Nc, expectedInnerCommandLength);
        }

        [Fact]
        public void CreateCommandApdu_GetDataProperty_ReturnsInnerCommandApdu()
        {
            var command = new GetProtocolVersionCommand();

            Assert.False(command.CreateCommandApdu().Data.IsEmpty);
        }

        #endregion Outer APDU

        #region Inner APDU

        [Fact]
        public void CreateCommandApdu_InnerCommandCla0x00()
        {
            byte expectedInnerCla = 0;

            var command = new GetProtocolVersionCommand();
            CommandApdu commandApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> actualInnerCommandApdu = commandApdu.Data;
            byte actualInnerCommandCla = actualInnerCommandApdu.Span[offsetCla];

            Assert.Equal(actualInnerCommandCla, expectedInnerCla);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandIns0x03()
        {
            byte expectedInnerIns = 0x03;

            var command = new GetProtocolVersionCommand();
            CommandApdu commandApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> actualInnerCommandApdu = commandApdu.Data;
            byte actualInnerCommandIns = actualInnerCommandApdu.Span[offsetIns];

            Assert.Equal(actualInnerCommandIns, expectedInnerIns);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandP1Hex00()
        {
            byte expectedInnerP1 = 0;

            var command = new GetProtocolVersionCommand();
            CommandApdu commandApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> actualInnerCommandApdu = commandApdu.Data;
            byte actualInnerCommandP1 = actualInnerCommandApdu.Span[offsetP1];

            Assert.Equal(actualInnerCommandP1, expectedInnerP1);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandP2Hex00()
        {
            byte expectedInnerP2 = 0;

            var command = new GetProtocolVersionCommand();
            CommandApdu commandApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> actualInnerCommandApdu = commandApdu.Data;
            byte actualInnerCommandP2 = actualInnerCommandApdu.Span[offsetP2];

            Assert.Equal(actualInnerCommandP2, expectedInnerP2);
        }

        #endregion Inner APDU

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var command = new GetProtocolVersionCommand();
            GetProtocolVersionResponse response = command.CreateResponseForApdu(responseApdu);

            _ = Assert.IsType<GetProtocolVersionResponse>(response);
        }
    }
}
