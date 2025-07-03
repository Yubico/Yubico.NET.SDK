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

namespace Yubico.YubiKey.Piv.Commands
{
    public class GetSerialNumberCommandTests
    {
        [Fact]
        public void ClassType_DerivedFromPivCommand_IsTrue()
        {
            var serialCommand = new GetSerialNumberCommand();

            Assert.True(serialCommand is IYubiKeyCommand<GetSerialNumberResponse>);
        }

        [Fact]
        public void Constructor_Application_Piv()
        {
            var command = new GetSerialNumberCommand();

            YubiKeyApplication application = command.Application;

            Assert.Equal(YubiKeyApplication.Piv, application);
        }

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            CommandApdu cmdApdu = GetSerialCommandApdu();

            byte Cla = cmdApdu.Cla;

            Assert.Equal(0, Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_ReturnsHexF8()
        {
            CommandApdu cmdApdu = GetSerialCommandApdu();

            byte Ins = cmdApdu.Ins;

            Assert.Equal(0xF8, Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsZero()
        {
            CommandApdu cmdApdu = GetSerialCommandApdu();

            byte P1 = cmdApdu.P1;

            Assert.Equal(0, P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            CommandApdu cmdApdu = GetSerialCommandApdu();

            byte P2 = cmdApdu.P2;

            Assert.Equal(0, P2);
        }

        [Fact]
        public void CreateCommandApdu_GetData_ReturnsEmpty()
        {
            CommandApdu cmdApdu = GetSerialCommandApdu();

            ReadOnlyMemory<byte> data = cmdApdu.Data;

            Assert.True(data.IsEmpty);
        }

        [Fact]
        public void CreateCommandApdu_GetNc_ReturnsZero()
        {
            CommandApdu cmdApdu = GetSerialCommandApdu();

            int Nc = cmdApdu.Nc;

            Assert.Equal(0, Nc);
        }

        [Fact]
        public void CreateCommandApdu_GetNe_ReturnsZero()
        {
            CommandApdu cmdApdu = GetSerialCommandApdu();

            int Ne = cmdApdu.Ne;

            Assert.Equal(0, Ne);
        }

        [Fact]
        public void CreateResponseForApdu_ReturnsCorrectType()
        {
            // Arrange
            var responseApdu = new ResponseApdu(new byte[] { 0x00, 0x81, 0xA4, 0x99, 0x90, 0x00 });
            var serialCommand = new GetSerialNumberCommand();

            // Act
            GetSerialNumberResponse serialResponse = serialCommand.CreateResponseForApdu(responseApdu);

            // Assert
            Assert.True(serialResponse is GetSerialNumberResponse);
        }

        private static CommandApdu GetSerialCommandApdu()
        {
            var serialCommand = new GetSerialNumberCommand();
            return serialCommand.CreateCommandApdu();
        }
    }
}
