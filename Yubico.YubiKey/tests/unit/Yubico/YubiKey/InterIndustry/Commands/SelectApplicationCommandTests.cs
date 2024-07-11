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

namespace Yubico.YubiKey.InterIndustry.Commands
{
    public class SelectApplicationCommandTests
    {
        [Fact]
        public void Constructor_GivenInterIndustryApplication_ThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => new SelectApplicationCommand(YubiKeyApplication.InterIndustry));
        }

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            Assert.Equal(0, GetSelectApplicationCommandApdu().Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_ReturnsHexA4()
        {
            Assert.Equal(0xA4, GetSelectApplicationCommandApdu().Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_Returns4()
        {
            Assert.Equal(0x04, GetSelectApplicationCommandApdu().P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            Assert.Equal(0, GetSelectApplicationCommandApdu().P2);
        }

        [Fact]
        public void CreateCommandApdu_GetDataConstructedWithBytes_ReturnsBytes()
        {
            byte[] fakeAid = new byte[] { 1, 2, 3, 4 };
            var selectApplicationCommand = new SelectApplicationCommand(fakeAid);

            CommandApdu commandApdu = selectApplicationCommand.CreateCommandApdu();

            Assert.Equal(fakeAid, commandApdu.Data);
            Assert.Equal(fakeAid.Length, commandApdu.Nc);
        }

        [Fact]
        public void CreateCommandApdu_GetDataConstructedWithEnum_ReturnsApplicationIdInBytes()
        {
            byte[] pivAid = new byte[] { 0xa0, 0x00, 0x00, 0x03, 0x08 };
            var selectApplicationCommand = new SelectApplicationCommand(YubiKeyApplication.Piv);

            CommandApdu commandApdu = selectApplicationCommand.CreateCommandApdu();

            Assert.True(commandApdu.Data.Span.SequenceEqual(pivAid));
            Assert.Equal(pivAid.Length, commandApdu.Nc);
        }

        [Fact]
        public void CreateCommandApdu_GetNe_ReturnsZero()
        {
            Assert.Equal(0, GetSelectApplicationCommandApdu().Ne);
        }

        private static CommandApdu GetSelectApplicationCommandApdu()
        {
            var selectApplicationCommand = new SelectApplicationCommand(YubiKeyApplication.OpenPgp);
            return selectApplicationCommand.CreateCommandApdu();
        }
    }
}
