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

using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    public class ResetCommandTests
    {
        private const int offsetCla = 0;
        private const int offsetIns = 1;
        private const int offsetP1 = 2;
        private const int offsetP2 = 3;

        private const int lengthHeader = 4; // APDU header is 4 bytes (Cla, Ins, P1, P2)

        private const int offsetLc = 4;

        private const int offsetData = offsetLc;

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var command = new ResetCommand();

            Assert.Equal(expected: 0, command.CreateCommandApdu().Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_Returns0x03()
        {
            var command = new ResetCommand();

            Assert.Equal(expected: 0x03, command.CreateCommandApdu().Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsZero()
        {
            var command = new ResetCommand();

            Assert.Equal(expected: 0, command.CreateCommandApdu().P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var command = new ResetCommand();

            Assert.Equal(expected: 0, command.CreateCommandApdu().P2);
        }

        [Fact]
        public void CreateCommandApdu_GetNcProperty_ReturnsCorrectLength()
        {
            var command = new ResetCommand();
            var commandApdu = command.CreateCommandApdu();

            Assert.Equal(lengthHeader, commandApdu.Nc);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetClaProperty_ReturnsZero()
        {
            var command = new ResetCommand();
            var commandApdu = command.CreateCommandApdu();

            var actualInnerCommandApdu = commandApdu.Data;
            var actualInnerCommandCla = actualInnerCommandApdu.Span[offsetCla];

            Assert.Equal(expected: 0, actualInnerCommandCla);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetInsProperty_Returns0x45()
        {
            var command = new ResetCommand();
            var commandApdu = command.CreateCommandApdu();

            var actualInnerCommandApdu = commandApdu.Data;
            var actualInnerCommandIns = actualInnerCommandApdu.Span[offsetIns];

            Assert.Equal(expected: 0x45, actualInnerCommandIns);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetP1Property_ReturnsZero()
        {
            var command = new ResetCommand();
            var commandApdu = command.CreateCommandApdu();

            var actualInnerCommandApdu = commandApdu.Data;
            var actualInnerCommandP1 = actualInnerCommandApdu.Span[offsetP1];

            Assert.Equal(expected: 0, actualInnerCommandP1);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetP2Property_ReturnsZero()
        {
            var command = new ResetCommand();
            var commandApdu = command.CreateCommandApdu();

            var actualInnerCommandApdu = commandApdu.Data;
            var actualInnerCommandP2 = actualInnerCommandApdu.Span[offsetP2];

            Assert.Equal(expected: 0, actualInnerCommandP2);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetNcProperty_ReturnsNoLength()
        {
            var command = new ResetCommand();
            var commandApdu = command.CreateCommandApdu();

            var actualInnerCommandLc = commandApdu.Data.Slice(offsetLc, length: 0);

            Assert.True(actualInnerCommandLc.IsEmpty);
        }

        [Fact]
        public void CreateCommandApdu_InnerCommandGetData_ReturnsNoData()
        {
            var command = new ResetCommand();
            var commandApdu = command.CreateCommandApdu();

            var actualInnerCommandData = commandApdu.Data.Slice(offsetData, length: 0);

            Assert.True(actualInnerCommandData.IsEmpty);
        }

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var command = new ResetCommand();
#pragma warning disable IDE0008 // Use explicit type
            var response = command.CreateResponseForApdu(responseApdu);
#pragma warning restore IDE0008 // Justification: testing the type

            _ = Assert.IsType<ResetResponse>(response);
        }
    }
}
