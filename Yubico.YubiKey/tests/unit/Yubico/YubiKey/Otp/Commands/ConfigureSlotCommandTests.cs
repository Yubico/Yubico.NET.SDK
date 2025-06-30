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
using System.Buffers.Binary;
using System.Linq;
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Otp.Commands
{
    public class ConfigureSlotCommandTests
    {
        [Fact]
        public void OtpSlot_Default_ReturnsShortPress()
        {
            var command = new ConfigureSlotCommand();

            Slot otpSlot = command.OtpSlot;

            Assert.Equal(Slot.ShortPress, otpSlot);
        }

        [Fact]
        public void OtpSlot_SetInvalidOtpSlot_ThrowsInvalidOperationException()
        {
            var command = new ConfigureSlotCommand();

            void Action() => _ = command.OtpSlot = (Slot)0x5; // Some invalid slot

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void TicketFlags_GetSet_SameValue()
        {
            TicketFlags expectedFlags = TicketFlags.AppendDelayToOtp;

            var command = new ConfigureSlotCommand { TicketFlags = expectedFlags };
            TicketFlags actualFlags = command.TicketFlags;

            Assert.Equal(expectedFlags, actualFlags);
        }

        [Fact]
        public void ConfigurationFlags_GetSet_SameValue()
        {
            ConfigurationFlags expectedFlags = ConfigurationFlags.ChallengeResponse;

            var command = new ConfigureSlotCommand() { ConfigurationFlags = expectedFlags };
            ConfigurationFlags actualFlags = command.ConfigurationFlags;

            Assert.Equal(expectedFlags, actualFlags);
        }

        [Fact]
        public void ExtendedFlags_GetSet_SameValue()
        {
            ExtendedFlags expectedFlags = ExtendedFlags.Dormant;

            var command = new ConfigureSlotCommand() { ExtendedFlags = expectedFlags };
            ExtendedFlags actualFlags = command.ExtendedFlags;

            Assert.Equal(expectedFlags, actualFlags);
        }

        [Fact]
        public void SetFixedData_BufferIsTooLong_ThrowsArgumentException()
        {
            var command = new ConfigureSlotCommand();

            void Action() => command.SetFixedData(new byte[17]);

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void SetFixedData_ZeroBufferGiven_PlacedCorrectlyInDataBuffer()
        {
            byte[] expectedFixed = new byte[SlotConfigureBase.FixedDataLength];
            var command = new ConfigureSlotCommand();

            command.SetFixedData(Array.Empty<byte>());
            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;
            ReadOnlySpan<byte> dataSlice = data.Slice(0, SlotConfigureBase.FixedDataLength).Span;

            Assert.True(dataSlice.SequenceEqual(expectedFixed));
        }

        [Fact]
        public void SetFixedData_ValidFixedDataLength_PlacedCorrectlyInDataBuffer()
        {
            const byte expectedValidFixedDataLength = 7;
            var command = new ConfigureSlotCommand();

            command.SetFixedData(new byte[expectedValidFixedDataLength]);
            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;
            byte dataSlice = data.Slice(
                SlotConfigureBase.FixedDataLength
                + SlotConfigureBase.UidLength
                + SlotConfigureBase.AesKeyLength
                + SlotConfigureBase.AccessCodeLength).Span[0];

            Assert.Equal(expectedValidFixedDataLength, dataSlice);
        }

        [Fact]
        public void SetUid_BufferIsInvalidLength_ThrowsArgumentException()
        {
            var command = new ConfigureSlotCommand();

            void Action() => command.SetUid(Array.Empty<byte>());

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void SetUid_ValidBuffer_PlacedCorrectlyInDataBuffer()
        {
            byte[] expectedUid = Enumerable.Repeat((byte)0xFF, ConfigureSlotCommand.UidLength).ToArray();
            var command = new ConfigureSlotCommand();

            command.SetUid(expectedUid);
            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;
            ReadOnlySpan<byte> dataSlice = data.Slice(ConfigureSlotCommand.FixedDataLength, ConfigureSlotCommand.UidLength).Span;

            Assert.True(dataSlice.SequenceEqual(expectedUid));
        }

        [Fact]
        public void SetAccessCode_BufferIsInvalidLength_ThrowsArgumentException()
        {
            var command = new ConfigureSlotCommand();

            void Action() => command.SetAccessCode(Array.Empty<byte>());

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void SetAccessCode_ValidBuffer_PlacedCorrectlyInDataBuffer()
        {
            byte[] expectedAccessCode = Enumerable.Repeat((byte)0xFF, UpdateSlotCommand.AccessCodeLength).ToArray();
            var command = new ConfigureSlotCommand();

            command.SetAccessCode(expectedAccessCode);
            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;
            ReadOnlySpan<byte> dataSlice = data.Slice(ConfigureSlotCommand.FixedDataLength
                                                       + ConfigureSlotCommand.UidLength
                                                       + ConfigureSlotCommand.AesKeyLength,
                                                         ConfigureSlotCommand.AccessCodeLength).Span;

            Assert.True(dataSlice.SequenceEqual(expectedAccessCode));
        }

        [Fact]
        public void ApplyCurrentAccessCode_BufferIsInvalidLength_ThrowsArgumentException()
        {
            var command = new ConfigureSlotCommand();

            void Action() => command.ApplyCurrentAccessCode(Array.Empty<byte>());

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void ApplyCurrentAccessCode_ValidBuffer_PlacedCorrectlyInDataBuffer()
        {
            byte[] expectedAccessCode = Enumerable.Repeat((byte)0xFF, UpdateSlotCommand.AccessCodeLength).ToArray();
            var command = new ConfigureSlotCommand();

            command.ApplyCurrentAccessCode(expectedAccessCode);
            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;
            ReadOnlySpan<byte> dataSlice = data.Slice(52, ConfigureSlotCommand.AccessCodeLength).Span;

            Assert.True(dataSlice.SequenceEqual(expectedAccessCode));
        }

        [Fact]
        public void ApplyCurrentAccessCode_AfterCreateCommandApdu_CorrectlyResizesBuffer()
        {
            var command = new ConfigureSlotCommand();
            _ = command.CreateCommandApdu();

            command.ApplyCurrentAccessCode(Enumerable.Repeat((byte)0xFF, ConfigureSlotCommand.AccessCodeLength).ToArray());
            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

            Assert.Equal(58, data.Length);
        }

        [Fact]
        public void ApplyCurrentAccessCode_CreateCommandApduGetNc_Returns58()
        {
            var command = new ConfigureSlotCommand();

            command.ApplyCurrentAccessCode(Enumerable.Repeat((byte)0xFF, ConfigureSlotCommand.AccessCodeLength).ToArray());
            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(58, apdu.Nc);
        }

        [Fact]
        public void CreateCommandApdu_AesKeyOfInvalidLength_ThrowsArgumentException()
        {
            var command = new ConfigureSlotCommand();

            void Action() => command.SetAesKey(Array.Empty<byte>());

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void CreateCommandApdu_AesKey_PlacedCorrectlyInDataBuffer()
        {
            byte[] expectedAesKey = Enumerable.Repeat((byte)0xFF, ConfigureSlotCommand.AesKeyLength).ToArray();
            var command = new ConfigureSlotCommand();

            command.SetAesKey(expectedAesKey);
            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;
            ReadOnlySpan<byte> dataSlice = data.Slice(ConfigureSlotCommand.FixedDataLength
                                                      + ConfigureSlotCommand.UidLength,
                                                        ConfigureSlotCommand.AesKeyLength).Span;

            Assert.True(dataSlice.SequenceEqual(expectedAesKey));
        }

        [Fact]
        public void CreateCommandApdu_AccessCodeOfInvalidLength_ThrowArgumentException()
        {
            var command = new ConfigureSlotCommand();

            void Action() => command.SetAccessCode(Array.Empty<byte>());

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void CreateCommandApdu_AccessCode_PlacedCorrectlyInDataBuffer()
        {
            byte[] expectedAccessCode = Enumerable.Repeat((byte)0xFF, ConfigureSlotCommand.AccessCodeLength).ToArray();
            var command = new ConfigureSlotCommand();

            command.SetAccessCode(expectedAccessCode);
            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;
            ReadOnlySpan<byte> dataSlice = data.Slice(ConfigureSlotCommand.FixedDataLength
                                                      + ConfigureSlotCommand.UidLength
                                                      + ConfigureSlotCommand.AesKeyLength,
                                                        ConfigureSlotCommand.AccessCodeLength).Span;

            Assert.True(dataSlice.SequenceEqual(expectedAccessCode));
        }

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var command = new ConfigureSlotCommand();

            byte cla = command.CreateCommandApdu().Cla;

            Assert.Equal(0, cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_ReturnsHex01()
        {
            var command = new ConfigureSlotCommand();

            byte ins = command.CreateCommandApdu().Ins;

            Assert.Equal(1, ins);
        }

        [Theory]
        [InlineData(Slot.ShortPress, 0x01)]
        [InlineData(Slot.LongPress, 0x03)]
        public void CreateCommandApdu_GetP1Property_ReturnsCorrectValueForSlot(Slot otpSlot, byte expectedSlotValue)
        {
            var command = new ConfigureSlotCommand { OtpSlot = otpSlot };

            byte p1 = command.CreateCommandApdu().P1;

            Assert.Equal(expectedSlotValue, p1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var command = new ConfigureSlotCommand();

            byte p2 = command.CreateCommandApdu().P2;

            Assert.Equal(0, p2);
        }

        [Fact]
        public void CreateCommandApdu_GetNc_Returns52()
        {
            var command = new ConfigureSlotCommand();

            int nc = command.CreateCommandApdu().Nc;

            Assert.Equal(58, nc);
        }

        [Fact]
        public void CreateCommandApdu_ExtendedFlags_PlacedCorrectlyInDataBuffer()
        {
            ExtendedFlags expectedFlags = ExtendedFlags.AllowUpdate;
            var command = new ConfigureSlotCommand()
            {
                ExtendedFlags = expectedFlags
            };

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;
            byte dataSlice = data.Slice(ConfigureSlotCommand.FixedDataLength
                                        + ConfigureSlotCommand.UidLength
                                        + ConfigureSlotCommand.AesKeyLength
                                        + ConfigureSlotCommand.AccessCodeLength
                                        + 1).Span[0];

            Assert.Equal(expectedFlags, (ExtendedFlags)dataSlice);
        }

        [Fact]
        public void CreateCommandApdu_TicketFlags_PlacedCorrectlyInDataBuffer()
        {
            TicketFlags expectedFlags = TicketFlags.AppendCarriageReturn;
            var command = new ConfigureSlotCommand()
            {
                TicketFlags = expectedFlags
            };

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;
            byte dataSlice = data.Slice(ConfigureSlotCommand.FixedDataLength
                                        + ConfigureSlotCommand.UidLength
                                        + ConfigureSlotCommand.AesKeyLength
                                        + ConfigureSlotCommand.AccessCodeLength
                                        + 2).Span[0];

            Assert.Equal(expectedFlags, (TicketFlags)dataSlice);
        }

        [Fact]
        [Obsolete("ConfigurationFlags.AllowHidTrigger is reserved for YubiKey 1")]
        public void CreateCommandApdu_ConfigurationFlags_PlacedCorrectlyInDataBuffer()
        {
            ConfigurationFlags expectedFlags = ConfigurationFlags.AllowHidTrigger;
            var command = new ConfigureSlotCommand()
            {
                ConfigurationFlags = expectedFlags
            };

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;
            byte dataSlice = data.Slice(ConfigureSlotCommand.FixedDataLength
                                        + ConfigureSlotCommand.UidLength
                                        + ConfigureSlotCommand.AesKeyLength
                                        + ConfigureSlotCommand.AccessCodeLength
                                        + 3).Span[0];

            Assert.Equal(expectedFlags, (ConfigurationFlags)dataSlice);
        }

        [Fact]
        public void CreateCommandApdu_ReservedSection_IsAlwaysZeroInBuffer()
        {
            const short expectedReserved = 0;
            var command = new ConfigureSlotCommand();

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;
            ReadOnlySpan<byte> dataSlice = data.Slice(ConfigureSlotCommand.FixedDataLength
                                                      + ConfigureSlotCommand.UidLength
                                                      + ConfigureSlotCommand.AesKeyLength
                                                      + ConfigureSlotCommand.AccessCodeLength
                                                      + 4, 2).Span;

            short actualReserved = BinaryPrimitives.ReadInt16LittleEndian(dataSlice);

            Assert.Equal(expectedReserved, actualReserved);
        }

        [Fact]
        public void CreateCommandApdu_Crc16_PlacedCorrectlyInBufferAsLittleEndian()
        {
            var command = new ConfigureSlotCommand();

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;
            ReadOnlySpan<byte> dataSlice = data.Slice(ConfigureSlotCommand.FixedDataLength
                                                      + ConfigureSlotCommand.UidLength
                                                      + ConfigureSlotCommand.AesKeyLength
                                                      + ConfigureSlotCommand.AccessCodeLength
                                                      + 6, 2).Span;

            short actualCrc = BinaryPrimitives.ReadInt16LittleEndian(dataSlice);

            Assert.Equal(20245, actualCrc);
        }

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var command = new ConfigureSlotCommand();

            IYubiKeyResponse response = command.CreateResponseForApdu(responseApdu);

            _ = Assert.IsAssignableFrom<ReadStatusResponse>(response);
        }
    }
}
