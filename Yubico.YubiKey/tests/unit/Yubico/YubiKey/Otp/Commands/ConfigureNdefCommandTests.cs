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

namespace Yubico.YubiKey.Otp.Commands
{
    public class ConfigureNdefCommandTests
    {
        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var command = new ConfigureNdefCommand(Slot.LongPress, new byte[62]);

            byte cla = command.CreateCommandApdu().Cla;

            Assert.Equal(0, cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_ReturnsHex01()
        {
            var command = new ConfigureNdefCommand(Slot.LongPress, new byte[62]);

            byte ins = command.CreateCommandApdu().Ins;

            Assert.Equal(1, ins);
        }

        [Theory]
        [InlineData(Slot.ShortPress, 0x08)]
        [InlineData(Slot.LongPress, 0x09)]
        public void CreateCommandApdu_GetP1Property_ReturnsCorrectSlot(Slot otpSlot, byte expectedSlotValue)
        {
            var command = new ConfigureNdefCommand(otpSlot, new byte[62]);

            byte p1 = command.CreateCommandApdu().P1;

            Assert.Equal(expectedSlotValue, p1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var command = new ConfigureNdefCommand(Slot.LongPress, new byte[62]);

            byte p2 = command.CreateCommandApdu().P2;

            Assert.Equal(0, p2);
        }

        [Fact]
        public void FullConstructor_ConfigurationIncorrectSize_ThrowsArgumentExcetion()
        {
            static void Action() => _ = new ConfigureNdefCommand(Slot.LongPress, Array.Empty<byte>());

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void FullConstructor_WithoutAccessCode_WritesConfigurationAsIs()
        {
            byte[] expectedConfig = new byte[62]
            {
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
                11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
                21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
                31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
                41, 42, 43, 44, 45, 46, 47, 48, 49, 50,
                51, 52, 53, 54, 55, 56, 57, 58, 59, 60,
                61, 62
            };

            var command = new ConfigureNdefCommand(Slot.LongPress, expectedConfig, Array.Empty<byte>());

            ReadOnlyMemory<byte> actualConfig = command.CreateCommandApdu().Data;

            Assert.True(actualConfig.Span.SequenceEqual(expectedConfig));
        }

        [Fact]
        public void FullConstructor_WithAccessCode_WritesAccessCodeToBuffer()
        {
            byte[] accessCode = new byte[6] { 1, 2, 3, 4, 5, 6 };
            var command = new ConfigureNdefCommand(Slot.LongPress, new byte[62], accessCode);

            byte[] data = command.CreateCommandApdu().Data.ToArray();

            ReadOnlySpan<byte> expectedAccessCode = accessCode.AsSpan();
            ReadOnlySpan<byte> actualAccessCode = data.AsSpan(56);

            Assert.True(expectedAccessCode.SequenceEqual(actualAccessCode));
        }

        [Fact]
        public void FullConstructor_IncorrectAccessCodeSize_ThrowsArgumentException()
        {
            static void Action() => _ = new ConfigureNdefCommand(Slot.LongPress, new byte[62], new byte[1]);

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void CreateCommandApdu_GetNe_ReturnsZero()
        {
            var command = new ConfigureNdefCommand(Slot.LongPress, new byte[62]);

            int ne = command.CreateCommandApdu().Ne;

            Assert.Equal(0, ne);
        }

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var command = new ConfigureNdefCommand(Slot.LongPress, new byte[62]);

            IYubiKeyResponse response = command.CreateResponseForApdu(responseApdu);

            _ = Assert.IsAssignableFrom<ReadStatusResponse>(response);
        }
    }
}
