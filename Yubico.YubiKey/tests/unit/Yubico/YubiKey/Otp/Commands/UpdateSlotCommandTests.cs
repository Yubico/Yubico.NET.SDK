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
using System.Buffers.Binary;
using System.Linq;
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Otp.Commands
{
    public class UpdateSlotCommandTests
    {
        [Fact]
        public void OtpSlot_Default_ReturnsShortPress()
        {
            var command = new UpdateSlotCommand();

            var otpSlot = command.OtpSlot;

            Assert.Equal(Slot.ShortPress, otpSlot);
        }

        [Fact]
        public void OtpSlot_SetInvalidOtpSlot_ThrowsArgumentException()
        {
            var command = new UpdateSlotCommand();

            void Action()
            {
                _ = command.OtpSlot = (Slot)0x5;
            }

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void TicketFlags_GetSet_SameValue()
        {
            TicketFlags expectedFlags = TicketFlags.AppendDelayToOtp;

            var command = new UpdateSlotCommand { TicketFlags = expectedFlags };
            var actualFlags = command.TicketFlags;

            Assert.Equal(expectedFlags, actualFlags);
        }

        [Fact]
        public void ConfigurationFlags_GetSet_SameValue()
        {
            ConfigurationFlags expectedFlags = ConfigurationFlags.Use10msPacing;

            var command = new UpdateSlotCommand { ConfigurationFlags = expectedFlags };
            var actualFlags = command.ConfigurationFlags;

            Assert.Equal(expectedFlags, actualFlags);
        }

        [Fact]
        public void ExtendedFlags_GetSet_SameValue()
        {
            ExtendedFlags expectedFlags = ExtendedFlags.AllowUpdate;

            var command = new UpdateSlotCommand { ExtendedFlags = expectedFlags };
            var actualFlags = command.ExtendedFlags;

            Assert.Equal(expectedFlags, actualFlags);
        }

        [Fact]
        public void SetAccessCode_BufferIsInvalidLength_ThrowsArgumentException()
        {
            var command = new UpdateSlotCommand();

            void Action()
            {
                command.SetAccessCode(Array.Empty<byte>());
            }

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void SetAccessCode_ValidBuffer_PlacedCorrectlyInDataBuffer()
        {
            var expectedAccessCode = Enumerable.Repeat((byte)0xFF, SlotConfigureBase.AccessCodeLength).ToArray();
            var command = new UpdateSlotCommand();

            command.SetAccessCode(expectedAccessCode);
            var data = command.CreateCommandApdu().Data;
            // The length constants only exist in the public interface of ConfigureSlotCommand. Use
            // them instead of introducing ones that are not needed on UpdateSlotCommand.
            var dataSlice = data.Slice(SlotConfigureBase.FixedDataLength
                                       + SlotConfigureBase.UidLength
                                       + SlotConfigureBase.AesKeyLength,
                SlotConfigureBase.AccessCodeLength).Span;

            Assert.True(dataSlice.SequenceEqual(expectedAccessCode));
        }

        [Fact]
        public void ApplyCurrentAccessCode_BufferIsInvalidLength_ThrowsArgumentException()
        {
            var command = new UpdateSlotCommand();

            void Action()
            {
                command.ApplyCurrentAccessCode(Array.Empty<byte>());
            }

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void ApplyCurrentAccessCode_ValidBuffer_PlacedCorrectlyInDataBuffer()
        {
            var expectedAccessCode = Enumerable.Repeat((byte)0xFF, SlotConfigureBase.AccessCodeLength).ToArray();
            var command = new UpdateSlotCommand();

            command.ApplyCurrentAccessCode(expectedAccessCode);
            var data = command.CreateCommandApdu().Data;
            var dataSlice = data.Slice(start: 52, SlotConfigureBase.AccessCodeLength).Span;

            Assert.True(dataSlice.SequenceEqual(expectedAccessCode));
        }

        [Fact]
        public void ApplyCurrentAccessCode_AfterCreateCommandApdu_CorrectlyResizesBuffer()
        {
            var command = new UpdateSlotCommand();
            _ = command.CreateCommandApdu();

            command.ApplyCurrentAccessCode(Enumerable.Repeat((byte)0xFF, SlotConfigureBase.AccessCodeLength).ToArray());
            var data = command.CreateCommandApdu().Data;

            Assert.Equal(expected: 58, data.Length);
        }

        [Fact]
        public void ApplyCurrentAccessCode_CreateCommandApduGetNc_Returns58()
        {
            var command = new UpdateSlotCommand();

            command.ApplyCurrentAccessCode(Enumerable.Repeat((byte)0xFF, SlotConfigureBase.AccessCodeLength).ToArray());
            var data = command.CreateCommandApdu().Data;

            Assert.Equal(expected: 58, data.Length);
        }

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var command = new UpdateSlotCommand();

            var cla = command.CreateCommandApdu().Cla;

            Assert.Equal(expected: 0, cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_ReturnsHex01()
        {
            var command = new UpdateSlotCommand();

            var ins = command.CreateCommandApdu().Ins;

            Assert.Equal(expected: 1, ins);
        }

        [Theory]
        [InlineData(Slot.ShortPress, 0x04)]
        [InlineData(Slot.LongPress, 0x05)]
        public void CreateCommandApdu_GetP1Property_ReturnsCorrectValueForSlot(Slot otpSlot, byte expectedSlotValue)
        {
            var command = new UpdateSlotCommand { OtpSlot = otpSlot };

            var p1 = command.CreateCommandApdu().P1;

            Assert.Equal(expectedSlotValue, p1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var command = new UpdateSlotCommand();

            var p2 = command.CreateCommandApdu().P2;

            Assert.Equal(expected: 0, p2);
        }

        [Fact]
        public void CreateCommandApdu_GetNc_Returns58()
        {
            var command = new UpdateSlotCommand();

            var nc = command.CreateCommandApdu().Nc;

            Assert.Equal(expected: 58, nc);
        }

        [Fact]
        public void CreateCommandApdu_ExtendedFlags_PlacedCorrectlyInDataBuffer()
        {
            ExtendedFlags expectedFlags = ExtendedFlags.AllowUpdate;
            var command = new UpdateSlotCommand { ExtendedFlags = expectedFlags };

            var data = command.CreateCommandApdu().Data;
            // The length constants only exist in the public interface of ConfigureSlotCommand. Use
            // them instead of introducing ones that are not needed on UpdateSlotCommand.
            var dataSlice = data.Slice(SlotConfigureBase.FixedDataLength
                                       + SlotConfigureBase.UidLength
                                       + SlotConfigureBase.AesKeyLength
                                       + SlotConfigureBase.AccessCodeLength
                                       + 1).Span[index: 0];

            Assert.Equal(expectedFlags, (ExtendedFlags)dataSlice);
        }

        [Fact]
        public void CreateCommandApdu_TicketFlags_PlacedCorrectlyInDataBuffer()
        {
            TicketFlags expectedFlags = TicketFlags.AppendCarriageReturn;
            var command = new UpdateSlotCommand { TicketFlags = expectedFlags };

            var data = command.CreateCommandApdu().Data;
            // The length constants only exist in the public interface of ConfigureSlotCommand. Use
            // them instead of introducing ones that are not needed on UpdateSlotCommand.
            var dataSlice = data.Slice(SlotConfigureBase.FixedDataLength
                                       + SlotConfigureBase.UidLength
                                       + SlotConfigureBase.AesKeyLength
                                       + SlotConfigureBase.AccessCodeLength
                                       + 2).Span[index: 0];

            Assert.Equal(expectedFlags, (TicketFlags)dataSlice);
        }

        [Fact]
        public void CreateCommandApdu_ConfigurationFlags_PlacedCorrectlyInDataBuffer()
        {
            ConfigurationFlags expectedFlags = ConfigurationFlags.Use10msPacing;
            var command = new UpdateSlotCommand { ConfigurationFlags = expectedFlags };

            var data = command.CreateCommandApdu().Data;
            // The length constants only exist in the public interface of ConfigureSlotCommand. Use
            // them instead of introducing ones that are not needed on UpdateSlotCommand.
            var dataSlice = data.Slice(SlotConfigureBase.FixedDataLength
                                       + SlotConfigureBase.UidLength
                                       + SlotConfigureBase.AesKeyLength
                                       + SlotConfigureBase.AccessCodeLength
                                       + 3).Span[index: 0];

            Assert.Equal(expectedFlags, (ConfigurationFlags)dataSlice);
        }

        [Fact]
        public void CreateCommandApdu_FlagsSet_AllPrecedingBytesZero()
        {
            var command = new UpdateSlotCommand
            {
                ExtendedFlags = ExtendedFlags.SerialNumberApiVisible,
                TicketFlags = TicketFlags.AppendTabToFixed,
                ConfigurationFlags = ConfigurationFlags.Use10msPacing
            };

            var data = command.CreateCommandApdu().Data;

            Assert.DoesNotContain(data.ToArray().Take(SlotConfigureBase.FixedDataLength
                                                      + SlotConfigureBase.UidLength
                                                      + SlotConfigureBase.AesKeyLength
                                                      + SlotConfigureBase.AccessCodeLength
                                                      + 1),
                currentByte => currentByte != 0);
        }

        [Fact]
        public void CreateCommandApdu_ReservedSection_IsAlwaysZeroInBuffer()
        {
            const short expectedReserved = 0;
            var command = new UpdateSlotCommand();

            var data = command.CreateCommandApdu().Data;
            var dataSlice = data.Slice(SlotConfigureBase.FixedDataLength
                                       + SlotConfigureBase.UidLength
                                       + SlotConfigureBase.AesKeyLength
                                       + SlotConfigureBase.AccessCodeLength
                                       + 4, length: 2).Span;

            var actualReserved = BinaryPrimitives.ReadInt16LittleEndian(dataSlice);

            Assert.Equal(expectedReserved, actualReserved);
        }

        [Fact]
        public void CreateCommandApdu_Crc16_PlacedCorrectlyInBufferAsLittleEndian()
        {
            var command = new UpdateSlotCommand();

            var data = command.CreateCommandApdu().Data;
            var dataSlice = data.Slice(SlotConfigureBase.FixedDataLength
                                       + SlotConfigureBase.UidLength
                                       + SlotConfigureBase.AesKeyLength
                                       + SlotConfigureBase.AccessCodeLength
                                       + 6, length: 2).Span;

            var actualCrc = BinaryPrimitives.ReadInt16LittleEndian(dataSlice);

            Assert.Equal(expected: 20245, actualCrc);
        }

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var command = new UpdateSlotCommand();

            IYubiKeyResponse response = command.CreateResponseForApdu(responseApdu);

            _ = Assert.IsAssignableFrom<ReadStatusResponse>(response);
        }
    }
}
