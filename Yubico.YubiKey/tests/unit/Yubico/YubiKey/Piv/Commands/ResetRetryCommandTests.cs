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
    public class ResetRetryCommandTests
    {
        [Fact]
        public void ClassType_DerivedFromPivCommand_IsTrue()
        {
            byte[] puk = GetPinArray(1, 8);
            byte[] newPin = GetPinArray(0, 7);
            var resetRetryCommand = new ResetRetryCommand(puk, newPin);

            Assert.True(resetRetryCommand is IYubiKeyCommand<ResetRetryResponse>);
        }

        [Fact]
        public void Constructor_Application_Piv()
        {
            byte[] puk = GetPinArray(1, 8);
            byte[] newPin = GetPinArray(0, 7);
            var command = new ResetRetryCommand(puk, newPin);

            YubiKeyApplication application = command.Application;

            Assert.Equal(YubiKeyApplication.Piv, application);
        }

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            CommandApdu cmdApdu = GetResetRetryCommandApdu();

            byte Cla = cmdApdu.Cla;

            Assert.Equal(0, Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_ReturnsHex2C()
        {
            CommandApdu cmdApdu = GetResetRetryCommandApdu();

            byte Ins = cmdApdu.Ins;

            Assert.Equal(0x2C, Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsZero()
        {
            CommandApdu cmdApdu = GetResetRetryCommandApdu();

            byte P1 = cmdApdu.P1;

            Assert.Equal(0, P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsSlotNum()
        {
            CommandApdu cmdApdu = GetResetRetryCommandApdu();

            byte P2 = cmdApdu.P2;

            Assert.Equal(0x80, P2);
        }

        [Fact]
        public void CreateCommandApdu_GetNc_Returns16()
        {
            CommandApdu cmdApdu = GetResetRetryCommandApdu();

            int Nc = cmdApdu.Nc;

            Assert.Equal(16, Nc);
        }

        [Fact]
        public void CreateCommandApdu_GetNe_ReturnsZero()
        {
            CommandApdu cmdApdu = GetResetRetryCommandApdu();

            int Ne = cmdApdu.Ne;

            Assert.Equal(0, Ne);
        }

        [Theory]
        [InlineData(6, 6)]
        [InlineData(6, 7)]
        [InlineData(6, 8)]
        [InlineData(7, 6)]
        [InlineData(7, 7)]
        [InlineData(7, 8)]
        [InlineData(8, 6)]
        [InlineData(8, 7)]
        [InlineData(8, 8)]
        public void CreateCommandApdu_GetDataProperty_ReturnsPukAndPin(int pukLength, int pinLength)
        {
            byte[] puk = GetPinArray(1, pukLength);
            byte[] newPin = GetPinArray(0, pinLength);

            var resetRetryCommand = new ResetRetryCommand(puk, newPin);
            CommandApdu cmdApdu = resetRetryCommand.CreateCommandApdu();

            ReadOnlyMemory<byte> data = cmdApdu.Data;

            Assert.False(data.IsEmpty);
            if (data.IsEmpty)
            {
                return;
            }

            Assert.Equal(16, data.Length);

            // Verify the first 8 bytes in the Data are the PUK + pad.
            bool compareResult = true;
            int index = 0;
            for (; index < puk.Length; index++)
            {
                if (data.Span[index] != puk[index])
                {
                    compareResult = false;
                }
            }

            for (; index < 8; index++)
            {
                if (data.Span[index] != 0xFF)
                {
                    compareResult = false;
                }
            }

            // Verify the next 8 bytes in the Data are the PIN + pad.
            for (index = 0; index < newPin.Length; index++)
            {
                if (data.Span[index + 8] != newPin[index])
                {
                    compareResult = false;
                }
            }

            for (; index < 8; index++)
            {
                if (data.Span[index + 8] != 0xFF)
                {
                    compareResult = false;
                }
            }

            Assert.True(compareResult);
        }

        [Fact]
        public void CreateResponseForApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            byte[] puk = GetPinArray(1, 8);
            byte[] newPin = GetPinArray(0, 7);
            var resetRetryCommand = new ResetRetryCommand(puk, newPin);

            ResetRetryResponse resetRetryResponse = resetRetryCommand.CreateResponseForApdu(responseApdu);

            Assert.True(resetRetryResponse is ResetRetryResponse);
        }

        [Theory]
        [InlineData(6, 1)]
        [InlineData(6, 2)]
        [InlineData(6, 3)]
        [InlineData(6, 4)]
        [InlineData(6, 5)]
        [InlineData(6, 9)]
        [InlineData(1, 6)]
        [InlineData(2, 6)]
        [InlineData(3, 6)]
        [InlineData(4, 6)]
        [InlineData(5, 6)]
        [InlineData(9, 6)]
        public void Constructor_BadPin_CorrectException(int pukLength, int pinLength)
        {
            byte[] puk = GetPinArray(1, pukLength);
            byte[] newPin = GetPinArray(0, pinLength);
            _ = Assert.Throws<ArgumentException>(() => new ResetRetryCommand(puk, newPin));
        }

        [Fact]
        public void Constructor_NullPuk_CorrectException()
        {
            byte[] newPin = GetPinArray(0, 6);
            _ = Assert.Throws<ArgumentException>(() => new ResetRetryCommand(null, newPin));
        }

        [Fact]
        public void Constructor_NullNewPin_CorrectException()
        {
            byte[] puk = GetPinArray(1, 8);
            _ = Assert.Throws<ArgumentException>(() => new ResetRetryCommand(puk, null));
        }

        private static CommandApdu GetResetRetryCommandApdu()
        {
            byte[] puk = GetPinArray(1, 8);
            byte[] newPin = GetPinArray(0, 7);
            var resetRetryCommand = new ResetRetryCommand(puk, newPin);
            CommandApdu returnValue = resetRetryCommand.CreateCommandApdu();

            return returnValue;
        }

        // If pinOrPuk is 0, produce values 1, 2, ... (0x31, 32, ...)
        // If pinOrPuk is not 0, produce values a, b, ... (0x61, 62, ...)
        private static byte[] GetPinArray(int pinOrPuk, int pinLength)
        {
            byte baseVal = 0x31;
            if (pinOrPuk != 0)
            {
                baseVal = 0x61;
            }

            byte[] returnValue = new byte[pinLength];
            for (int index = 0; index < pinLength; index++)
            {
                byte value = (byte)(index & 15);
                value += baseVal;
                returnValue[index] = value;
            }

            return returnValue;
        }
    }
}
