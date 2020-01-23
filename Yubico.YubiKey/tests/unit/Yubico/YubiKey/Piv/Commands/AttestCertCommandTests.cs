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
    public class AttestCertCommandTests
    {
        [Fact]
        public void ClassType_DerivedFromPivCommand_IsTrue()
        {
            var command = new CreateAttestationStatementCommand(0x9A);

            Assert.True(command is IYubiKeyCommand<CreateAttestationStatementResponse>);
        }

        [Fact]
        public void Constructor_Application_Piv()
        {
            var command = new CreateAttestationStatementCommand(0x9C);

            YubiKeyApplication application = command.Application;

            Assert.Equal(YubiKeyApplication.Piv, application);
        }

        [Fact]
        public void Constructor_Property_SlotNum()
        {
            byte slotNumber = 0x9C;
            var command = new CreateAttestationStatementCommand(slotNumber);

            byte getSlotNum = command.SlotNumber;

            Assert.Equal(slotNumber, getSlotNum);
        }

        [Theory]
        [InlineData(1, 0xF9)]
        [InlineData(2, 0x9B)]
        [InlineData(3, 0x80)]
        [InlineData(1, 0x81)]
        [InlineData(2, 0x82)]
        [InlineData(3, 0x8C)]
        [InlineData(4, 0x9F)]
        [InlineData(2, 0x01)]
        public void Constructor_InvalidSlot_CorrectException(int cStyle, byte slotNum)
        {
            _ = Assert.Throws<ArgumentException>(() => GetCommandObject(cStyle, slotNum));
        }

        [Fact]
        public void DefaultConstructor_NoSlot_CorrectException()
        {
            var cmd = new CreateAttestationStatementCommand();
            _ = Assert.Throws<InvalidOperationException>(() => cmd.CreateCommandApdu());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero(int cStyle)
        {
            CreateAttestationStatementCommand command = GetCommandObject(cStyle, 0x9D);
            CommandApdu cmdApdu = command.CreateCommandApdu();

            byte Cla = cmdApdu.Cla;

            Assert.Equal(0, Cla);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void CreateCommandApdu_GetInsProperty_ReturnsHexF9(int cStyle)
        {
            CreateAttestationStatementCommand command = GetCommandObject(cStyle, 0x9E);
            CommandApdu cmdApdu = command.CreateCommandApdu();

            byte Ins = cmdApdu.Ins;

            Assert.Equal(0xF9, Ins);
        }

        [Theory]
        [InlineData(1, PivSlot.Authentication)]
        [InlineData(1, PivSlot.Signing)]
        [InlineData(1, PivSlot.KeyManagement)]
        [InlineData(1, PivSlot.CardAuthentication)]
        [InlineData(2, PivSlot.Authentication)]
        [InlineData(2, PivSlot.Signing)]
        [InlineData(2, PivSlot.KeyManagement)]
        [InlineData(2, PivSlot.CardAuthentication)]
        [InlineData(3, PivSlot.Authentication)]
        [InlineData(3, PivSlot.Signing)]
        [InlineData(3, PivSlot.KeyManagement)]
        [InlineData(3, PivSlot.CardAuthentication)]
        public void CreateCommandApdu_GetP1Property_ReturnsSlotNum(int cStyle, byte slotNum)
        {
            CreateAttestationStatementCommand command = GetCommandObject(cStyle, slotNum);
            CommandApdu cmdApdu = command.CreateCommandApdu();

            byte P1 = cmdApdu.P1;

            Assert.Equal(slotNum, P1);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void CreateCommandApdu_GetP2Property_ReturnsZero(int cStyle)
        {
            CreateAttestationStatementCommand command = GetCommandObject(cStyle, 0x9A);
            CommandApdu cmdApdu = command.CreateCommandApdu();

            byte P2 = cmdApdu.P2;

            Assert.Equal(0, P2);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void CreateCommandApdu_GetNc_ReturnsZero(int cStyle)
        {
            CreateAttestationStatementCommand command = GetCommandObject(cStyle, 0x9C);
            CommandApdu cmdApdu = command.CreateCommandApdu();

            int Nc = cmdApdu.Nc;

            Assert.Equal(0, Nc);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void CreateCommandApdu_GetData_ReturnsEmpty(int cStyle)
        {
            CreateAttestationStatementCommand command = GetCommandObject(cStyle, 0x9D);
            CommandApdu cmdApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> data = cmdApdu.Data;

            Assert.True(data.IsEmpty);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void CreateCommandApdu_GetNe_ReturnsZero(int cStyle)
        {
            CreateAttestationStatementCommand command = GetCommandObject(cStyle, 0x9e);
            CommandApdu cmdApdu = command.CreateCommandApdu();

            int Ne = cmdApdu.Ne;

            Assert.Equal(0, Ne);
        }

        [Fact]
        public void CreateResponseForApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x69, 0x84 });
            var command = new CreateAttestationStatementCommand(0x9c);

            CreateAttestationStatementResponse response = command.CreateResponseForApdu(responseApdu);

            Assert.True(response is CreateAttestationStatementResponse);
        }

        // Construct a CreateAttestationCertificateCommand using the style
        // specified.
        // If the style arg is 1, this will build using the full constructor.
        // If it is 2, it will build it using object initializer constructor.
        // If it is anything else, create it using the empty constructor and set
        // the slotNumber property later.
        private static CreateAttestationStatementCommand GetCommandObject(int constructorStyle, byte slotNumber)
        {
            CreateAttestationStatementCommand cmd;

            switch (constructorStyle)
            {
                default:
#pragma warning disable IDE0017 // Testing this specific construction
                    cmd = new CreateAttestationStatementCommand();
                    cmd.SlotNumber = slotNumber;
                    break;
#pragma warning restore IDE0017

                case 1:
                    cmd = new CreateAttestationStatementCommand(slotNumber);
                    break;

                case 2:
                    cmd = new CreateAttestationStatementCommand()
                    {
                        SlotNumber = slotNumber,
                    };
                    break;
            }

            return cmd;
        }
    }
}
