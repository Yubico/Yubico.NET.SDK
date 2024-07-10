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
    public class GetMetadataCommandTests
    {
        [Fact]
        public void ClassType_DerivedFromPivCommand_IsTrue()
        {
            var metadataCommand = new GetMetadataCommand(PivSlot.Authentication);

            Assert.True(metadataCommand is IYubiKeyCommand<GetMetadataResponse>);
        }

        [Fact]
        public void NoArgConstructor_DerivedFromPivCommand_IsTrue()
        {
            var metadataCommand = new GetMetadataCommand();

            Assert.True(metadataCommand is IYubiKeyCommand<GetMetadataResponse>);
        }

        [Fact]
        public void Constructor_Application_Piv()
        {
            var command = new GetMetadataCommand();

            var application = command.Application;

            Assert.Equal(YubiKeyApplication.Piv, application);
        }

        [Fact]
        public void Constructor_Property_SlotNum()
        {
            var slotNumber = PivSlot.Retired11;
            var command = new GetMetadataCommand(slotNumber);

            var getSlotNum = command.SlotNumber;

            Assert.Equal(slotNumber, getSlotNum);
        }

        [Theory]
        [InlineData(1, 0x9B)]
        [InlineData(2, 0x80)]
        [InlineData(3, 0x81)]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero(int cStyle, byte slotNumber)
        {
            var cmdApdu = GetMetadataCommandApdu(cStyle, slotNumber);

            var Cla = cmdApdu.Cla;

            Assert.Equal(expected: 0, Cla);
        }

        [Theory]
        [InlineData(1, 0x9A)]
        [InlineData(2, 0x9C)]
        [InlineData(3, 0x9D)]
        public void CreateCommandApdu_GetInsProperty_ReturnsHexF7(int cStyle, byte slotNumber)
        {
            var cmdApdu = GetMetadataCommandApdu(cStyle, slotNumber);

            var Ins = cmdApdu.Ins;

            Assert.Equal(expected: 0xF7, Ins);
        }

        [Theory]
        [InlineData(1, 0x9E)]
        [InlineData(2, 0x90)]
        [InlineData(3, 0x91)]
        public void CreateCommandApdu_GetP1Property_ReturnsZero(int cStyle, byte slotNumber)
        {
            var cmdApdu = GetMetadataCommandApdu(cStyle, slotNumber);

            var P1 = cmdApdu.P1;

            Assert.Equal(expected: 0, P1);
        }

        [Theory]
        [InlineData(1, 0x92)]
        [InlineData(2, 0x93)]
        [InlineData(3, 0x94)]
        public void CreateCommandApdu_GetP2Property_ReturnsSlot(int cStyle, byte slotNumber)
        {
            var cmdApdu = GetMetadataCommandApdu(cStyle, slotNumber);

            var P2 = cmdApdu.P2;

            Assert.Equal(slotNumber, P2);
        }

        [Theory]
        [InlineData(1, 0x95)]
        [InlineData(2, 0x92)]
        [InlineData(3, 0x83)]
        public void CreateCommandApdu_GetData_ReturnsEmpty(int cStyle, byte slotNumber)
        {
            var cmdApdu = GetMetadataCommandApdu(cStyle, slotNumber);

            var data = cmdApdu.Data;

            Assert.True(data.IsEmpty);
        }

        [Theory]
        [InlineData(1, 0x84)]
        [InlineData(2, 0x85)]
        [InlineData(3, 0x86)]
        public void CreateCommandApdu_GetNc_ReturnsZero(int cStyle, byte slotNumber)
        {
            var cmdApdu = GetMetadataCommandApdu(cStyle, slotNumber);

            var Nc = cmdApdu.Nc;

            Assert.Equal(expected: 0, Nc);
        }

        [Theory]
        [InlineData(1, 0x87)]
        [InlineData(2, 0x88)]
        [InlineData(3, 0x89)]
        public void CreateCommandApdu_GetNe_ReturnsZero(int cStyle, byte slotNumber)
        {
            var cmdApdu = GetMetadataCommandApdu(cStyle, slotNumber);

            var Ne = cmdApdu.Ne;

            Assert.Equal(expected: 0, Ne);
        }

        [Fact]
        public void CreateResponseForApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x90, 0x00 });
            var metadataCommand = new GetMetadataCommand(PivSlot.Retired4);

            var metadataResponse = metadataCommand.CreateResponseForApdu(responseApdu);

            Assert.True(metadataResponse is GetMetadataResponse);
        }

        [Fact]
        public void Constructor_BadSlot_CorrectException()
        {
            _ = Assert.Throws<ArgumentException>(() => new GetMetadataCommand(slotNumber: 0));
        }

        [Fact]
        public void NoArgConstructor_NoSlot_CorrectException()
        {
            var metadataCommand = new GetMetadataCommand();
            _ = Assert.Throws<InvalidOperationException>(() => metadataCommand.CreateCommandApdu());
        }

        [Fact]
        public void NoArgConstructor_BadSlot_CorrectException()
        {
            var metadataCommand = new GetMetadataCommand();
            _ = Assert.Throws<ArgumentException>(() => metadataCommand.SlotNumber = 0);
        }

        private static CommandApdu GetMetadataCommandApdu(int cStyle, byte pivSlot)
        {
            var metadataCommand = GetCommandObject(cStyle, pivSlot);
            return metadataCommand.CreateCommandApdu();
        }

        // Construct a GetMetadataCommand using the style specified.
        // If the style arg is 1, this will build using the full constructor.
        // If it is 2, it will build it using object initializer constructor.
        // If it is 3, create it using the empty constructor and set the
        // properties later.
        private static GetMetadataCommand GetCommandObject(int cStyle, byte slotNumber)
        {
            GetMetadataCommand cmd;

            switch (cStyle)
            {
                default:
                    cmd = new GetMetadataCommand(slotNumber);
                    break;

                case 2:
                    cmd = new GetMetadataCommand
                    {
                        SlotNumber = slotNumber
                    };
                    break;

                case 3:
#pragma warning disable IDE0017 // Testing this specific construction
                    cmd = new GetMetadataCommand();
                    cmd.SlotNumber = slotNumber;
                    break;
#pragma warning restore IDE0017
            }

            return cmd;
        }
    }
}
