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

namespace Yubico.YubiKey.Oath.Commands
{
    public class SelectOathCommandTests
    {
        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            Assert.Equal(expected: 0, GetSelectApplicationCommandApdu().Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_Returns0xA4()
        {
            Assert.Equal(expected: 0xA4, GetSelectApplicationCommandApdu().Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_Returns0x04()
        {
            Assert.Equal(expected: 0x04, GetSelectApplicationCommandApdu().P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            Assert.Equal(expected: 0, GetSelectApplicationCommandApdu().P2);
        }

        [Fact]
        public void CreateCommandApdu_GetDataConstructedWithBytes_ReturnsBytes()
        {
            byte[] aid = { 0xa0, 0x00, 0x00, 0x05, 0x27, 0x21, 0x01 };
            var selectApplicationCommand = new SelectOathCommand();

            var commandApdu = selectApplicationCommand.CreateCommandApdu();

            Assert.True(commandApdu.Data.Span.SequenceEqual(aid));
            Assert.Equal(aid.Length, commandApdu.Nc);
        }

        [Fact]
        public void CreateCommandApdu_GetNe_ReturnsZero()
        {
            Assert.Equal(expected: 0, GetSelectApplicationCommandApdu().Ne);
        }

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            var command = new SelectOathCommand();
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var response = command.CreateResponseForApdu(responseApdu);

            Assert.True(response is SelectOathResponse);
        }

        private static CommandApdu GetSelectApplicationCommandApdu()
        {
            var selectApplicationCommand = new SelectOathCommand();
            return selectApplicationCommand.CreateCommandApdu();
        }
    }
}
