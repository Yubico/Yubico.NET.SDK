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

namespace Yubico.YubiKey.Scp03.Commands
{
    public class InitializeUpdateCommandTests
    {
        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsHex80()
        {
            Assert.Equal(0x80, GetInitializeUpdateCommandApdu().Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_ReturnsHex50()
        {
            Assert.Equal(0x50, GetInitializeUpdateCommandApdu().Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsZero()
        {
            Assert.Equal(0, GetInitializeUpdateCommandApdu().P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            Assert.Equal(0, GetInitializeUpdateCommandApdu().P2);
        }

        [Fact]
        public void CreateCommandApdu_GetData_ReturnsChallenge()
        {
            CommandApdu commandApdu = GetInitializeUpdateCommandApdu();
            byte[] challenge = GetChallenge();

            Assert.False(commandApdu.Data.IsEmpty);
            Assert.True(commandApdu.Data.Span.SequenceEqual(challenge));
        }

        [Fact]
        public void CreateCommandApdu_GetNc_Returns8()
        {
            Assert.Equal(8, GetInitializeUpdateCommandApdu().Nc);
        }

        [Fact]
        public void CreateCommandApdu_GetNe_ReturnsZero()
        {
            Assert.Equal(0, GetInitializeUpdateCommandApdu().Ne);
        }

        private static byte[] GetChallenge()
        {
            return new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        }

        private static InitializeUpdateCommand GetInitializeUpdateCommand()
        {
            return new InitializeUpdateCommand(0, GetChallenge());
        }

        private static CommandApdu GetInitializeUpdateCommandApdu()
        {
            return GetInitializeUpdateCommand().CreateCommandApdu();
        }
    }
}
