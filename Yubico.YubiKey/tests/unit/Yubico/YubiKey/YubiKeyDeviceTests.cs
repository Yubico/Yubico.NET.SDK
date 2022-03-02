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

namespace Yubico.YubiKey
{
    public class YubiKeyDeviceTests
    {
        [Fact]
        public void AvailableUsbCapabilities_SetGet_ReturnsSetValue()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                AvailableUsbCapabilities = YubiKeyCapabilities.All,
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);

            Assert.Equal(ykDeviceInfo.AvailableUsbCapabilities, ykDevice.AvailableUsbCapabilities);
        }

        [Fact]
        public void EnabledUsbCapabilities_SetGet_ReturnsSetValue()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                EnabledUsbCapabilities = YubiKeyCapabilities.All,
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);

            Assert.Equal(ykDeviceInfo.EnabledUsbCapabilities, ykDevice.EnabledUsbCapabilities);
        }

        [Fact]
        public void AvailableNfcCapabilities_SetGet_ReturnsSetValue()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                AvailableNfcCapabilities = YubiKeyCapabilities.All,
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);

            Assert.Equal(ykDeviceInfo.AvailableNfcCapabilities, ykDevice.AvailableNfcCapabilities);
        }

        [Fact]
        public void EnabledNfcCapabilities_SetGet_ReturnsSetValue()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                EnabledNfcCapabilities = YubiKeyCapabilities.All,
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);

            Assert.Equal(ykDeviceInfo.EnabledNfcCapabilities, ykDevice.EnabledNfcCapabilities);
        }

        [Fact]
        public void SerialNumber_SetGet_ReturnsSetValue()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                SerialNumber = 12345678,
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);

            Assert.Equal(ykDeviceInfo.SerialNumber, ykDevice.SerialNumber);
        }

        [Fact]
        public void IsFipsSeries_SetGet_ReturnsSetValue()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                IsFipsSeries = true,
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);

            Assert.Equal(ykDeviceInfo.IsFipsSeries, ykDevice.IsFipsSeries);
        }

        [Fact]
        public void IsSkySeries_SetGet_ReturnsSetValue()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                IsSkySeries = true,
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);

            Assert.Equal(ykDeviceInfo.IsSkySeries, ykDevice.IsSkySeries);
        }

        [Fact]
        public void FormFactor_SetGet_ReturnsSetValue()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                FormFactor = FormFactor.UsbAKeychain,
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);

            Assert.Equal(ykDeviceInfo.FormFactor, ykDevice.FormFactor);
        }

        [Fact]
        public void FirmwareVersion_SetGet_ReturnsSetValue()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                FirmwareVersion = new FirmwareVersion(1, 2, 3),
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);

            Assert.Equal(ykDeviceInfo.FirmwareVersion, ykDevice.FirmwareVersion);
        }

        [Fact]
        public void AutoEjectTimeout_SetGet_ReturnsSetValue()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                AutoEjectTimeout = 10,
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);

            Assert.Equal(ykDeviceInfo.AutoEjectTimeout, ykDevice.AutoEjectTimeout);
        }

        [Fact]
        public void ChallengeResponseTimeout_SetGet_ReturnsSetValue()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                ChallengeResponseTimeout = 10,
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);

            Assert.Equal(ykDeviceInfo.ChallengeResponseTimeout, ykDevice.ChallengeResponseTimeout);
        }

        [Fact]
        public void DeviceFlags_SetGet_ReturnsSetValue()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                DeviceFlags = DeviceFlags.TouchEject,
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);

            Assert.Equal(ykDeviceInfo.DeviceFlags, ykDevice.DeviceFlags);
        }

        [Fact]
        public void ConfigurationLocked_SetGet_ReturnsSetValue()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                ConfigurationLocked = true,
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);

            Assert.Equal(ykDeviceInfo.ConfigurationLocked, ykDevice.ConfigurationLocked);
        }

        [Fact]
        public void SetEnabledNfcCapabilities_NoConnections_ThrowsException()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                AvailableUsbCapabilities = YubiKeyCapabilities.All,
                EnabledUsbCapabilities = YubiKeyCapabilities.All,
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);

            _ = Assert.Throws<NotSupportedException>(
                () => ykDevice.SetEnabledNfcCapabilities(YubiKeyCapabilities.Piv));
        }

        [Fact]
        public void SetEnabledUsbCapabilities_SetNone_ThrowsException()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                AvailableUsbCapabilities = YubiKeyCapabilities.All,
                EnabledUsbCapabilities = YubiKeyCapabilities.All,
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);

            _ = Assert.Throws<InvalidOperationException>(
                () => ykDevice.SetEnabledUsbCapabilities(YubiKeyCapabilities.None));
        }

        [Fact]
        public void SetEnabledUsbCapabilities_SetOnlyUnavailableCapability_ThrowsException()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                AvailableUsbCapabilities = YubiKeyCapabilities.Piv | YubiKeyCapabilities.Otp,
                EnabledUsbCapabilities = YubiKeyCapabilities.Piv,
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);

            _ = Assert.Throws<InvalidOperationException>(
                () => ykDevice.SetEnabledUsbCapabilities(YubiKeyCapabilities.OpenPgp));
        }

        [Fact]
        public void SetEnabledUsbCapabilities_NoConnections_ThrowsException()
        {
            var ykDeviceInfo = new YubiKeyDeviceInfo()
            {
                AvailableUsbCapabilities = YubiKeyCapabilities.All,
                EnabledUsbCapabilities = YubiKeyCapabilities.All,
            };

            var ykDevice = new YubiKeyDevice(null, null, null, ykDeviceInfo);

            _ = Assert.Throws<NotSupportedException>(
                () => ykDevice.SetEnabledUsbCapabilities(YubiKeyCapabilities.Piv));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(256)]
        public void SetChallengeResponseTimeout_InvalidSecondsValue_ThrowsException(int seconds)
        {
            var ykDevice = new YubiKeyDevice(null, null, null, new YubiKeyDeviceInfo());

            _ = Assert.Throws<ArgumentOutOfRangeException>(
                () => ykDevice.SetChallengeResponseTimeout(seconds));
        }

        [Fact]
        public void SetChallengeResponseTimeout_NoConnections_ThrowsException()
        {
            var ykDevice = new YubiKeyDevice(null, null, null, new YubiKeyDeviceInfo());

            _ = Assert.Throws<NotSupportedException>(
                () => ykDevice.SetChallengeResponseTimeout(15));
        }

        [Theory]
        [InlineData(ushort.MinValue - 1)]
        [InlineData(ushort.MaxValue + 1)]
        public void SetAutoEjectTimeout_InvalidSecondsValue_ThrowsException(int seconds)
        {
            var ykDevice = new YubiKeyDevice(null, null, null, new YubiKeyDeviceInfo());

            _ = Assert.Throws<ArgumentOutOfRangeException>(
                () => ykDevice.SetAutoEjectTimeout(seconds));
        }

        [Fact]
        public void SetAutoEjectTimeout_NoConnections_ThrowsException()
        {
            var ykDevice = new YubiKeyDevice(null, null, null, new YubiKeyDeviceInfo());

            _ = Assert.Throws<NotSupportedException>(
                () => ykDevice.SetAutoEjectTimeout(15));
        }

        [Fact]
        public void SetDeviceFlags_NoConnections_ThrowsException()
        {
            var ykDevice = new YubiKeyDevice(null, null, null, new YubiKeyDeviceInfo());

            _ = Assert.Throws<NotSupportedException>(
                () => ykDevice.SetDeviceFlags(DeviceFlags.None));
        }

        [Fact]
        public void LockConfiguration_SetEmptySpan_ThrowsArgException()
        {
            var ykDevice = new YubiKeyDevice(null, null, null, new YubiKeyDeviceInfo());

            _ = Assert.Throws<ArgumentException>(
                () => ykDevice.LockConfiguration(ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public void LockConfiguration_SetAllZero_ThrowsArgException()
        {
            var ykDevice = new YubiKeyDevice(null, null, null, new YubiKeyDeviceInfo());

            _ = Assert.Throws<ArgumentException>(
                () => ykDevice.LockConfiguration(new byte[16]));
        }

        [Fact]
        public void LockConfiguration_NoConnections_ThrowsException()
        {
            var ykDevice = new YubiKeyDevice(null, null, null, new YubiKeyDeviceInfo());

            byte[] lockCode = new byte[16];
            lockCode[0] = 1;

            _ = Assert.Throws<NotSupportedException>(
                () => ykDevice.LockConfiguration(lockCode));
        }

        [Fact]
        public void UnlockConfiguration_SetEmptySpan_ThrowsArgException()
        {
            var ykDevice = new YubiKeyDevice(null, null, null, new YubiKeyDeviceInfo());

            _ = Assert.Throws<ArgumentException>(
                () => ykDevice.LockConfiguration(ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public void UnlockConfiguration_NoConnections_ThrowsException()
        {
            var ykDevice = new YubiKeyDevice(null, null, null, new YubiKeyDeviceInfo());

            byte[] lockCode = new byte[16];
            lockCode[0] = 1;

            _ = Assert.Throws<NotSupportedException>(
                () => ykDevice.LockConfiguration(lockCode));
        }
    }
}
