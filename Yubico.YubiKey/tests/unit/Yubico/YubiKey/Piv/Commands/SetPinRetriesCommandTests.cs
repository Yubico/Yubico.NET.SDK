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
    public class SetPinRetriesCommandTests
    {
        [Fact]
        public void ClassType_DerivedFromPivCommand_IsTrue()
        {
            var setPinRetriesCommand = new SetPinRetriesCommand(5, 5);

            Assert.True(setPinRetriesCommand is IYubiKeyCommand<SetPinRetriesResponse>);
        }

        [Fact]
        public void Constructor_Application_Piv()
        {
            var command = new SetPinRetriesCommand(5, 5);

            YubiKeyApplication application = command.Application;

            Assert.Equal(YubiKeyApplication.Piv, application);
        }

        [Fact]
        public void Constructor_Property_PinRetryCount()
        {
            byte pinCount = 44;
            byte pukCount = 55;
            var command = new SetPinRetriesCommand(pinCount, pukCount);

            byte count = command.PinRetryCount;

            Assert.Equal(pinCount, count);
        }

        [Fact]
        public void Constructor_Property_PukRetryCount()
        {
            byte pinCount = 44;
            byte pukCount = 55;
            var command = new SetPinRetriesCommand(pinCount, pukCount);

            byte count = command.PukRetryCount;

            Assert.Equal(pukCount, count);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero(int cStyle)
        {
            CommandApdu cmdApdu = GetPinRetriesCommandApdu(cStyle, 6, 4);

            byte Cla = cmdApdu.Cla;

            Assert.Equal(0, Cla);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void CreateCommandApdu_GetInsProperty_ReturnsHexFA(int cStyle)
        {
            CommandApdu cmdApdu = GetPinRetriesCommandApdu(cStyle, 22, 100);

            byte Ins = cmdApdu.Ins;

            Assert.Equal(0xFA, Ins);
        }

        [Theory]
        [InlineData(1, 20, 20)]
        [InlineData(2, 1, 1)]
        [InlineData(3, 255, 255)]
        [InlineData(4, 33, 3)]
        [InlineData(5, 51, 3)]
        [InlineData(6, 100, 100)]
        [InlineData(7, 44, 44)]
        [InlineData(8, 88, 3)]
        public void CreateCommandApdu_GetP1Property_ReturnsPinRetries(int cStyle, byte count, byte expected)
        {
            CommandApdu cmdApdu = GetPinRetriesCommandApdu(cStyle, count, 11);

            byte P1 = cmdApdu.P1;

            Assert.Equal(expected, P1);
        }

        [Theory]
        [InlineData(1, 20, 20)]
        [InlineData(2, 1, 1)]
        [InlineData(3, 255, 255)]
        [InlineData(4, 33, 33)]
        [InlineData(5, 51, 51)]
        [InlineData(6, 100, 3)]
        [InlineData(7, 44, 3)]
        [InlineData(8, 88, 3)]
        public void CreateCommandApdu_GetP2Property_ReturnsPukRetries(int cStyle, byte count, byte expected)
        {
            CommandApdu cmdApdu = GetPinRetriesCommandApdu(cStyle, 50, count);

            byte P2 = cmdApdu.P2;

            Assert.Equal(expected, P2);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void CreateCommandApdu_GetNc_ReturnsZero(int cStyle)
        {
            CommandApdu cmdApdu = GetPinRetriesCommandApdu(cStyle, 255, 1);

            int Nc = cmdApdu.Nc;

            Assert.Equal(0, Nc);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void CreateCommandApdu_GetNe_ReturnsZero(int cStyle)
        {
            CommandApdu cmdApdu = GetPinRetriesCommandApdu(cStyle, 16, 15);

            int Ne = cmdApdu.Ne;

            Assert.Equal(0, Ne);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void CreateCommandApdu_GetData_ReturnsEmpty(int cStyle)
        {
            CommandApdu cmdApdu = GetPinRetriesCommandApdu(cStyle, 4, 255);

            ReadOnlyMemory<byte> data = cmdApdu.Data;

            Assert.True(data.Span.IsEmpty);
        }

        [Fact]
        public void CreateResponseForApdu_ReturnsCorrectType()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });
            var setPinRetriesCommand = new SetPinRetriesCommand(2, 3);

            SetPinRetriesResponse setPinRetriesResponse = setPinRetriesCommand.CreateResponseForApdu(responseApdu);

            Assert.True(setPinRetriesResponse is SetPinRetriesResponse);
        }

        [Theory]
        [InlineData(0, 4)]
        [InlineData(4, 0)]
        public void Constructor_BadCount_CorrectException(byte pinCount, byte pukCount)
        {
            _ = Assert.Throws<ArgumentException>(() => new SetPinRetriesCommand(pinCount, pukCount));
        }

        private static CommandApdu GetPinRetriesCommandApdu(int cStyle, byte pinRetries, byte pukRetries)
        {
            SetPinRetriesCommand command = GetCommandObject(cStyle, pinRetries, pukRetries);
            CommandApdu returnValue = command.CreateCommandApdu();

            return returnValue;
        }

        // Construct a SetPinRetriesCommand using the style specified.
        // If the style arg is 1, this will build using the full constructor.
        // If it is 2, it will build it using object initializer constructor.
        // If it is 3, create it using the empty constructor and set the
        // properties later.
        // If it is 4, create it using the object initializer constructor but
        // don't set the PinRetryCount (it should be default).
        // If it is 5, create it using the empty constructor and set the
        // properties later, except don't set the PinRetryCount.
        // If it is 6, create it using the object initializer constructor but
        // don't set the PukRetryCount (it should be default).
        // If it is 7, create it using the empty constructor and set the
        // properties later, except don't set the PukRetryCount.
        // If it is 8, create it using the empty constructor and don't set the
        // properties.
        private static SetPinRetriesCommand GetCommandObject(
            int cStyle,
            byte pinRetryCount,
            byte pukRetryCount)
        {
            SetPinRetriesCommand cmd;

#pragma warning disable IDE0017 // Testing this specific construction
            switch (cStyle)
            {
                default:
                    cmd = new SetPinRetriesCommand(pinRetryCount, pukRetryCount);
                    break;

                case 2:
                    cmd = new SetPinRetriesCommand()
                    {
                        PinRetryCount = pinRetryCount,
                        PukRetryCount = pukRetryCount,
                    };
                    break;

                case 3:
                    cmd = new SetPinRetriesCommand();
                    cmd.PinRetryCount = pinRetryCount;
                    cmd.PukRetryCount = pukRetryCount;
                    break;

                case 4:
                    cmd = new SetPinRetriesCommand()
                    {
                        PukRetryCount = pukRetryCount,
                    };
                    break;

                case 5:
                    cmd = new SetPinRetriesCommand();
                    cmd.PukRetryCount = pukRetryCount;
                    break;

                case 6:
                    cmd = new SetPinRetriesCommand()
                    {
                        PinRetryCount = pinRetryCount,
                    };
                    break;

                case 7:
                    cmd = new SetPinRetriesCommand();
                    cmd.PinRetryCount = pinRetryCount;
                    break;

                case 8:
                    cmd = new SetPinRetriesCommand();
                    break;
            }
#pragma warning restore IDE0017

            return cmd;
        }
    }
}
