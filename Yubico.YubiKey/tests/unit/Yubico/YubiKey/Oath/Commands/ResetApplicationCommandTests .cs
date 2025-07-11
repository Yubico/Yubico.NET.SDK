// Copyright 2025 Yubico AB
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

namespace Yubico.YubiKey.Oath.Commands
{
    public class ResetApplicationCommandTests
    {
        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var command = new ResetCommand();

            Assert.Equal(0, command.CreateCommandApdu().Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_Returns0x04()
        {
            var command = new ResetCommand();

            Assert.Equal(0x04, command.CreateCommandApdu().Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_Returns0xDE()
        {
            var command = new ResetCommand();

            Assert.Equal(0xDE, command.CreateCommandApdu().P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_Returns0xAD()
        {
            var command = new ResetCommand();

            Assert.Equal(0xAD, command.CreateCommandApdu().P2);
        }

        [Fact]
        public void CreateCommandApdu_GetNcProperty_ReturnsZero()
        {
            var command = new ResetCommand();

            Assert.Equal(0, command.CreateCommandApdu().Nc);
        }

        [Fact]
        public void CreateCommandApdu_GetDataProperty_ReturnsEmpty()
        {
            var command = new ResetCommand();

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

            Assert.True(data.IsEmpty);
        }

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var command = new ResetCommand();
            OathResponse? response = command.CreateResponseForApdu(responseApdu);

            Assert.True(response is OathResponse);
        }
    }
}
