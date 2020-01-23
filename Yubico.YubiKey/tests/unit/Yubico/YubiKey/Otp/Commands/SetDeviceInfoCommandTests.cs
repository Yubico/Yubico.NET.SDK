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
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Otp.Commands
{
    public class SetDeviceInfoCommandTests
    {
        [Fact]
        public void EnabledUsbCapabilities_Get_DefaultIsNull()
        {
            var command = new SetDeviceInfoCommand();

            Assert.Null(command.EnabledUsbCapabilities);
        }

        [Fact]
        public void EnabledUsbCapabilities_SetGet_ReturnsSetValue()
        {
            YubiKeyCapabilities expectedCapabilities = YubiKeyCapabilities.All;
            var command = new SetDeviceInfoCommand()
            {
                EnabledUsbCapabilities = expectedCapabilities
            };

            Assert.Equal(expectedCapabilities, command.EnabledUsbCapabilities);
        }

        [Fact]
        public void EnabledNfcCapabilities_Get_DefaultIsNull()
        {
            var command = new SetDeviceInfoCommand();

            Assert.Null(command.EnabledNfcCapabilities);
        }

        [Fact]
        public void EnabledNfcCapabilities_SetGet_ReturnsSetValue()
        {
            YubiKeyCapabilities expectedCapabilities = YubiKeyCapabilities.All;
            var command = new SetDeviceInfoCommand()
            {
                EnabledNfcCapabilities = expectedCapabilities
            };
            Assert.Equal(expectedCapabilities, command.EnabledNfcCapabilities);
        }

        [Fact]
        public void ChallengeResponseTimeout_Get_DefaultIsNull()
        {
            var command = new SetDeviceInfoCommand();

            Assert.Null(command.ChallengeResponseTimeout);
        }

        [Fact]
        public void ChallengeResponseTimeout_SetGet_ReturnsSetValue()
        {
            byte expectedTimeout = 5;
            var command = new SetDeviceInfoCommand
            {
                ChallengeResponseTimeout = expectedTimeout
            };

            Assert.Equal(expectedTimeout, command.ChallengeResponseTimeout);
        }

        [Fact]
        public void AutoEjectTimeout_Get_DefaultIsNull()
        {
            var command = new SetDeviceInfoCommand();

            Assert.Null(command.AutoEjectTimeout);
        }

        [Fact]
        public void AutoEjectTimeout_SetGet_ReturnsSetValue()
        {
            int expectedTimeout = ushort.MaxValue;
            var command = new SetDeviceInfoCommand
            {
                AutoEjectTimeout = expectedTimeout
            };

            Assert.Equal(expectedTimeout, command.AutoEjectTimeout);
        }

        [Theory]
        [InlineData(ushort.MinValue - 1)]
        [InlineData(ushort.MaxValue + 1)]
        public void AutoEjectTimeout_SetValueOutOfRange_ThrowsArgumentRangeException(int value)
        {
            var command = new SetDeviceInfoCommand();

            void Action() => command.AutoEjectTimeout = value;

            _ = Assert.Throws<ArgumentOutOfRangeException>(Action);
        }

        [Fact]
        public void DeviceFlags_Get_DefaultIsNull()
        {
            var command = new SetDeviceInfoCommand();

            Assert.Null(command.DeviceFlags);
        }

        [Fact]
        public void DeviceFlags_SetGet_ReturnsSetValue()
        {
            DeviceFlags expectedFlags = DeviceFlags.RemoteWakeup;
            var command = new SetDeviceInfoCommand
            {
                DeviceFlags = expectedFlags
            };

            Assert.Equal(expectedFlags, command.DeviceFlags);
        }

        [Fact]
        public void ResetAfterConfig_Get_DefaultIsFalse()
        {
            var command = new SetDeviceInfoCommand();

            Assert.False(command.ResetAfterConfig);
        }

        [Fact]
        public void ResetAfterConfig_SetGet_ReturnsSetValue()
        {
            var command = new SetDeviceInfoCommand
            {
                ResetAfterConfig = true
            };

            Assert.True(command.ResetAfterConfig);
        }

        [Fact]
        public void CopyConstructor_EnabledUsbCapabilities_SetGet_ReturnsSetValue()
        {
            YubiKeyCapabilities expectedCapabilities = YubiKeyCapabilities.All;
            var command = new SetDeviceInfoCommand()
            {
                EnabledUsbCapabilities = expectedCapabilities
            };

            var newCommand = new SetDeviceInfoCommand(command);

            Assert.Equal(expectedCapabilities, newCommand.EnabledUsbCapabilities);
        }

        [Fact]
        public void CopyConstructor_EnabledNfcCapabilities_SetGet_ReturnsSetValue()
        {
            YubiKeyCapabilities expectedCapabilities = YubiKeyCapabilities.All;
            var command = new SetDeviceInfoCommand()
            {
                EnabledNfcCapabilities = expectedCapabilities
            };

            var newCommand = new SetDeviceInfoCommand(command);

            Assert.Equal(expectedCapabilities, newCommand.EnabledNfcCapabilities);
        }

        [Fact]
        public void CopyConstructor_ChallengeResponseTimeout_SetGet_ReturnsSetValue()
        {
            byte expectedTimeout = 5;
            var command = new SetDeviceInfoCommand
            {
                ChallengeResponseTimeout = expectedTimeout
            };

            var newCommand = new SetDeviceInfoCommand(command);

            Assert.Equal(expectedTimeout, newCommand.ChallengeResponseTimeout);
        }

        [Fact]
        public void CopyConstructor_AutoEjectTimeout_SetGet_ReturnsSetValue()
        {
            int expectedTimeout = ushort.MaxValue;
            var command = new SetDeviceInfoCommand
            {
                AutoEjectTimeout = expectedTimeout
            };

            var newCommand = new SetDeviceInfoCommand(command);

            Assert.Equal(expectedTimeout, newCommand.AutoEjectTimeout);
        }

        [Fact]
        public void CopyConstructor_DeviceFlags_SetGet_ReturnsSetValue()
        {
            DeviceFlags expectedFlags = DeviceFlags.RemoteWakeup;
            var command = new SetDeviceInfoCommand
            {
                DeviceFlags = expectedFlags
            };

            var newCommand = new SetDeviceInfoCommand(command);

            Assert.Equal(expectedFlags, newCommand.DeviceFlags);
        }

        [Fact]
        public void CopyConstructor_ResetAfterConfig_SetGet_ReturnsSetValue()
        {
            var command = new SetDeviceInfoCommand
            {
                ResetAfterConfig = true
            };

            var newCommand = new SetDeviceInfoCommand(command);

            Assert.True(newCommand.ResetAfterConfig);
        }

        [Fact]
        public void Application_Get_AlwaysReturnsOtp()
        {
            var command = new SetDeviceInfoCommand();

            Assert.Equal(YubiKeyApplication.Otp, command.Application);
        }

        [Fact]
        public void SetLockCode_IncorrectCodeLength_ThrowsArgumentException()
        {
            var command = new SetDeviceInfoCommand();

            void Action() => command.SetLockCode(Array.Empty<byte>());

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void ApplyLockCode_IncorrectCodeLength_ThrowsArgumentException()
        {
            var command = new SetDeviceInfoCommand();

            void Action() => command.ApplyLockCode(Array.Empty<byte>());

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var command = new SetDeviceInfoCommand();

            byte cla = command.CreateCommandApdu().Cla;

            Assert.Equal(0, cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_Returns0x01()
        {
            var command = new SetDeviceInfoCommand();

            byte ins = command.CreateCommandApdu().Ins;

            Assert.Equal(1, ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_Returns0x15()
        {
            var command = new SetDeviceInfoCommand();

            byte p1 = command.CreateCommandApdu().P1;

            Assert.Equal(0x15, p1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var command = new SetDeviceInfoCommand();

            byte p2 = command.CreateCommandApdu().P2;

            Assert.Equal(0, p2);
        }

        [Fact]
        public void CreateCommandApdu_EnabledUsbCapabilitiesPresent_EncodesCorrectTlv()
        {
            YubiKeyCapabilities expectedCapabilities = YubiKeyCapabilities.All;
            var command = new SetDeviceInfoCommand
            {
                EnabledUsbCapabilities = expectedCapabilities
            };

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

            Assert.Equal(0x04, data.Span[0]);
            Assert.Equal(0x03, data.Span[1]);
            Assert.Equal(0x02, data.Span[2]);
            Assert.Equal(
                expectedCapabilities,
                (YubiKeyCapabilities)BinaryPrimitives.ReadInt16BigEndian(data.Span.Slice(3)));
        }

        [Fact]
        public void CreateCommandApdu_EnabledNfcCapabilitiesPresent_EncodesCorrectTlv()
        {
            YubiKeyCapabilities expectedCapabilities = YubiKeyCapabilities.All;
            var command = new SetDeviceInfoCommand
            {
                EnabledNfcCapabilities = expectedCapabilities
            };

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

            Assert.Equal(0x04, data.Span[0]);
            Assert.Equal(0x0E, data.Span[1]);
            Assert.Equal(0x02, data.Span[2]);
            Assert.Equal(
                expectedCapabilities,
                (YubiKeyCapabilities)BinaryPrimitives.ReadInt16BigEndian(data.Span.Slice(3)));
        }

        [Fact]
        public void CreateCommandApdu_ChallengeResponseTimeoutPresent_EncodesCorrectTlv()
        {
            byte expectedTimeout = 5;
            var command = new SetDeviceInfoCommand
            {
                ChallengeResponseTimeout = expectedTimeout
            };

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

            Assert.Equal(0x03, data.Span[0]);
            Assert.Equal(0x07, data.Span[1]);
            Assert.Equal(0x01, data.Span[2]);
            Assert.Equal(expectedTimeout, data.Span[3]);
        }

        [Fact]
        public void CreateCommandApdu_AutoEjectTimeoutPresent_EncodesCorrectTlv()
        {
            int expectedTimeout = ushort.MaxValue - 1;
            var command = new SetDeviceInfoCommand
            {
                AutoEjectTimeout = expectedTimeout
            };

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

            Assert.Equal(0x04, data.Span[0]);
            Assert.Equal(0x06, data.Span[1]);
            Assert.Equal(0x02, data.Span[2]);
            Assert.Equal(
                expectedTimeout,
                BinaryPrimitives.ReadUInt16BigEndian(data.Span.Slice(3)));
        }

        [Fact]
        public void CreateCommandApdu_DeviceFlagsPresent_EncodesCorrectTlv()
        {
            DeviceFlags expectedFlags = DeviceFlags.RemoteWakeup;
            var command = new SetDeviceInfoCommand
            {
                DeviceFlags = expectedFlags
            };

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

            Assert.Equal(0x03, data.Span[0]);
            Assert.Equal(0x08, data.Span[1]);
            Assert.Equal(0x01, data.Span[2]);
            Assert.Equal((byte)expectedFlags, data.Span[3]);
        }

        [Fact]
        public void CreateCommandApdu_ResetAfterConfigFalse_OneByteValueZero()
        {
            var command = new SetDeviceInfoCommand();

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

            Assert.Equal(1, data.Length);
            Assert.Equal(0, data.Span[0]);
        }

        [Fact]
        public void CreateCommandApdu_ResetAfterConfigTrue_EncodesCorrectTlv()
        {
            var command = new SetDeviceInfoCommand()
            {
                ResetAfterConfig = true
            };

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

            Assert.Equal(0x02, data.Span[0]);
            Assert.Equal(0x0C, data.Span[1]);
            Assert.Equal(0, data.Span[2]);
        }

        [Fact]
        public void CreateCommandApdu_SetLockCodeCalled_EncodesCorrectTlv()
        {
            byte[] expectedCode = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            var command = new SetDeviceInfoCommand();
            command.SetLockCode(expectedCode);

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

            Assert.Equal(0x12, data.Span[0]);
            Assert.Equal(0x0A, data.Span[1]);
            Assert.Equal(0x10, data.Span[2]);
            Assert.True(data.Slice(3).Span.SequenceEqual(expectedCode));
        }

        [Fact]
        public void CreatecommandApdu_ApplyLockCodeCalled_EncodesCorrectTlv()
        {
            byte[] expectedCode = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            var command = new SetDeviceInfoCommand();
            command.ApplyLockCode(expectedCode);

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

            Assert.Equal(0x12, data.Span[0]);
            Assert.Equal(0x0B, data.Span[1]);
            Assert.Equal(0x10, data.Span[2]);
            Assert.True(data.Slice(3).Span.SequenceEqual(expectedCode));
        }

        [Fact]
        public void CreateCommandApdu_GetNe_ReturnsZero()
        {
            var command = new SetDeviceInfoCommand();

            int ne = command.CreateCommandApdu().Ne;

            Assert.Equal(0, ne);
        }

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            // Arrange
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var command = new SetDeviceInfoCommand();

            // Act
            OtpResponse response = command.CreateResponseForApdu(responseApdu);

            // Assert
            Assert.True(response is ReadStatusResponse);
        }
    }
}
