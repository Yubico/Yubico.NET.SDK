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

namespace Yubico.YubiKey.Piv.Commands
{
    public class InitializeAuthenticateManagementKeyCommandTests
    {
        [Fact]
        public void ClassType_DerivedFromPivCommand_IsTrue()
        {
            var command = new InitializeAuthenticateManagementKeyCommand();

            Assert.True(command is IYubiKeyCommand<InitializeAuthenticateManagementKeyResponse>);
        }

        [Fact]
        public void Constructor_Application_Piv()
        {
            var command = new InitializeAuthenticateManagementKeyCommand();

            var application = command.Application;

            Assert.Equal(YubiKeyApplication.Piv, application);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero(int constructor)
        {
            var cmdApdu = GetInitAuthMgmtKeyCommandApdu(constructor);

            var Cla = cmdApdu.Cla;

            Assert.Equal(expected: 0, Cla);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void CreateCommandApdu_GetInsProperty_ReturnsHex87(int constructor)
        {
            var cmdApdu = GetInitAuthMgmtKeyCommandApdu(constructor);

            var Ins = cmdApdu.Ins;

            Assert.Equal(expected: 0x87, Ins);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void CreateCommandApdu_GetP1Property_ReturnsThree(int constructor)
        {
            var cmdApdu = GetInitAuthMgmtKeyCommandApdu(constructor);

            var P1 = cmdApdu.P1;

            Assert.Equal(expected: 3, P1);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void CreateCommandApdu_GetP2Property_ReturnsHex9B(int constructor)
        {
            var cmdApdu = GetInitAuthMgmtKeyCommandApdu(constructor);

            var P2 = cmdApdu.P2;

            Assert.Equal(expected: 0x9B, P2);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void CreateCommandApdu_GetNc_Returns4(int constructor)
        {
            var cmdApdu = GetInitAuthMgmtKeyCommandApdu(constructor);

            var Nc = cmdApdu.Nc;

            Assert.Equal(expected: 4, Nc);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void CreateCommandApdu_GetNe_ReturnsZero(int constructor)
        {
            var cmdApdu = GetInitAuthMgmtKeyCommandApdu(constructor);

            var Ne = cmdApdu.Ne;

            Assert.Equal(expected: 0, Ne);
        }

        [Theory]
        [InlineData(0, 0x81)]
        [InlineData(1, 0x80)]
        [InlineData(2, 0x80)]
        public void CreateCommandApdu_GetData_ReturnsCorrect(int constructor, byte tag2)
        {
            var expected = new byte[4]
            {
                0x7C, 0x02, tag2, 0x00
            };
            var cmdApdu = GetInitAuthMgmtKeyCommandApdu(constructor);

            var data = cmdApdu.Data;
            Assert.False(data.IsEmpty);
            if (data.IsEmpty)
            {
                return;
            }

            var compareResult = data.Span.SequenceEqual(expected);

            Assert.True(compareResult);
        }

        [Fact]
        public void CreateResponseForApdu_ReturnsCorrectType()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(
                new byte[] { 0x7C, 0x0A, 0x81, 0x08, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, sw1, sw2 });
            var command = new InitializeAuthenticateManagementKeyCommand();

            var response = command.CreateResponseForApdu(responseApdu);

            Assert.True(response is InitializeAuthenticateManagementKeyResponse);
        }

        // Get the CommandApdu from the command specified by constructor. The
        // constructor arg says which constructor to use:
        //  0: InitializeAuthenticateManagementKeyCommand(false)
        //  1: InitializeAuthenticateManagementKeyCommand(true)
        //  2: InitializeAuthenticateManagementKeyCommand()
        // This will actually use the no-arg constructor on any input other than
        // 0 or 1.
        private static CommandApdu GetInitAuthMgmtKeyCommandApdu(int constructor)
        {
            var command = constructor switch
            {
                0 => new InitializeAuthenticateManagementKeyCommand(mutualAuthentication: false),
                1 => new InitializeAuthenticateManagementKeyCommand(mutualAuthentication: true),
                _ => new InitializeAuthenticateManagementKeyCommand()
            };

            return command.CreateCommandApdu();
        }
    }
}
