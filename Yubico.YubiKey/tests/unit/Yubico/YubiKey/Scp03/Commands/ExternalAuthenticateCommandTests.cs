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
    public class ExternalAuthenticateCommandTests
    {
        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsHex84()
        {
            Assert.Equal(0x84, GetExternalAuthenticateCommandApdu().Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_ReturnsHex82()
        {
            Assert.Equal(0x82, GetExternalAuthenticateCommandApdu().Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsHex33()
        {
            Assert.Equal(0x33, GetExternalAuthenticateCommandApdu().P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            Assert.Equal(0, GetExternalAuthenticateCommandApdu().P2);
        }

        [Fact]
        public void CreateCommandApdu_GetData_ReturnsData()
        {
            ReadOnlyMemory<byte> data = GetExternalAuthenticateCommandApdu().Data;
            Assert.True(data.Span.SequenceEqual(GetData()));
        }

        [Fact]
        public void CreateCommandApdu_GetNc_ReturnsDataLength()
        {
            Assert.Equal(GetData().Length, GetExternalAuthenticateCommandApdu().Nc);
        }

        [Fact]
        public void CreateCommandApdu_GetNe_ReturnsZero()
        {
            Assert.Equal(0, GetExternalAuthenticateCommandApdu().Ne);
        }

        private static byte[] GetData()
        {
            return new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        }
        private static ExternalAuthenticateCommand GetExternalAuthenticateCommand()
        {
            return new ExternalAuthenticateCommand(GetData());
        }
        private static CommandApdu GetExternalAuthenticateCommandApdu()
        {
            return GetExternalAuthenticateCommand().CreateCommandApdu();
        }
    }
}
