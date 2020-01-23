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

namespace Yubico.YubiKey.Fido2.Commands
{
    public class GetNextAssertionCommandTests
    {
        [Fact]
        public void Constructor_Succeeds()
        {
            _ = new GetNextAssertionCommand();
        }

        [Fact]
        public void CreateCommandApdu_Succeeds()
        {
            var GetNextAssertionCommand = new GetNextAssertionCommand();

            _ = GetNextAssertionCommand.CreateCommandApdu();
        }

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var GetnextAssertionCommand = new GetNextAssertionCommand();

            CommandApdu commandApdu = GetnextAssertionCommand.CreateCommandApdu();

            Assert.Equal(0, commandApdu.Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_Returns0x10()
        {

            var GetNextAssertionCommand = new GetNextAssertionCommand();

            CommandApdu commandApdu = GetNextAssertionCommand.CreateCommandApdu();

            Assert.Equal(0x10, commandApdu.Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsZero()
        {
            var GetnextAssertionCommand = new GetNextAssertionCommand();

            CommandApdu commandApdu = GetnextAssertionCommand.CreateCommandApdu();

            Assert.Equal(0, commandApdu.P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var GetnextAssertionCommand = new GetNextAssertionCommand();

            CommandApdu commandApdu = GetnextAssertionCommand.CreateCommandApdu();

            Assert.Equal(0, commandApdu.P2);
        }

        [Fact]
        public void CreateCommandApdu_GetDataProperty_ReturnsFirstByte0x08()
        {
            var GetNextAssertionCommand = new GetNextAssertionCommand();

            CommandApdu commandApdu = GetNextAssertionCommand.CreateCommandApdu();

            Assert.Equal(0x08, commandApdu.Data.Span[0]);
        }

        [Fact]
        public void CreateCommandApdu_GetNcProperty_ReturnsOne()
        {
            var GetNextAssertionCommand = new GetNextAssertionCommand();

            CommandApdu commandApdu = GetNextAssertionCommand.CreateCommandApdu();

            Assert.Equal(1, commandApdu.Nc);
        }

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var GetNextAssertionCommand = new GetNextAssertionCommand();
            var response = GetNextAssertionCommand.CreateResponseForApdu(responseApdu);

            _ = Assert.IsType<GetAssertionResponse>(response);
        }
    }
}
