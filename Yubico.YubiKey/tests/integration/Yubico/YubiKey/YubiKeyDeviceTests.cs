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
using System.Collections.Generic;
using Xunit;
using Yubico.YubiKey.Management.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey
{
    [Trait("Category", "RequiresStepDebug")] // Timing issues, may need to step with debugger
    public class YubiKeyDeviceTests
    {
        private static readonly byte[] LockCodeAllZero = new byte[16];

        private static byte[] TestLockCode =>
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

        [Fact]
        public void Sky_SetLock_Succeeds()
        {
            if (TryGetSkyDevice(out IYubiKeyDevice device))
            {
                if (!device.ConfigurationLocked)
                {
                    device.LockConfiguration(TestLockCode);
                }

                device.UnlockConfiguration(TestLockCode);

                // What we really want is to refresh the device and verify that
                // the ConfigurationLocked property is properly set. But until
                // that feature is available, just do this.
                Assert.False(device.IsFipsSeries);
            }
        }

        [Fact]
        public void Sky_SetAutoEjectTimeout_Succeeds()
        {
            if (TryGetSkyDevice(out IYubiKeyDevice device))
            {
                if (device.AutoEjectTimeout == 0)
                {
                    device.SetAutoEjectTimeout(5);
                }

                device.SetAutoEjectTimeout(0);

                // What we really want is to refresh the device and verify that
                // the ConfigurationLocked property is properly set. But until
                // that feature is available, just do this.
                Assert.False(device.IsFipsSeries);
            }
        }

        [Fact]
        public void Sky_SetChallengeResponseTimeout_Succeeds()
        {
            if (TryGetSkyDevice(out IYubiKeyDevice device))
            {
                if (device.ChallengeResponseTimeout == 15)
                {
                    device.SetChallengeResponseTimeout(20);
                }

                device.SetChallengeResponseTimeout(15);

                // What we really want is to refresh the device and verify that
                // the ConfigurationLocked property is properly set. But until
                // that feature is available, just do this.
                Assert.False(device.IsFipsSeries);
            }
        }

        [Fact]
        public void Sky_SetDeviceFlags_Succeeds()
        {
            if (TryGetSkyDevice(out IYubiKeyDevice device))
            {
                if (device.DeviceFlags == DeviceFlags.None)
                {
                    device.SetDeviceFlags(DeviceFlags.RemoteWakeup);
                }

                device.SetDeviceFlags(DeviceFlags.None);

                // What we really want is to refresh the device and verify that
                // the ConfigurationLocked property is properly set. But until
                // that feature is available, just do this.
                Assert.False(device.IsFipsSeries);
            }
        }

        [Fact]
        public void Sky_SetEnabledNfcCapabilities_Succeeds()
        {
            if (TryGetSkyDevice(out IYubiKeyDevice device))
            {
                if (device.EnabledNfcCapabilities == (YubiKeyCapabilities.Fido2 | YubiKeyCapabilities.FidoU2f))
                {
                    device.SetEnabledNfcCapabilities(YubiKeyCapabilities.Fido2);
                }

                device.SetEnabledNfcCapabilities(YubiKeyCapabilities.Fido2 | YubiKeyCapabilities.FidoU2f);

                // What we really want is to refresh the device and verify that
                // the ConfigurationLocked property is properly set. But until
                // that feature is available, just do this.
                Assert.False(device.IsFipsSeries);
            }
        }

        [Fact]
        public void Sky_SetEnabledUsbCapabilities_Succeeds()
        {
            if (TryGetSkyDevice(out IYubiKeyDevice device))
            {
                if (device.EnabledUsbCapabilities == (YubiKeyCapabilities.Fido2 | YubiKeyCapabilities.FidoU2f))
                {
                    device.SetEnabledUsbCapabilities(YubiKeyCapabilities.Fido2);
                }

                device.SetEnabledUsbCapabilities(YubiKeyCapabilities.Fido2 | YubiKeyCapabilities.FidoU2f);

                // What we really want is to refresh the device and verify that
                // the ConfigurationLocked property is properly set. But until
                // that feature is available, just do this.
                Assert.False(device.IsFipsSeries);
            }
        }

        // Set the out arg device to the first SKY device found, return true.
        // If no SKY is found, set device to Hollow and return false.
        private bool TryGetSkyDevice(out IYubiKeyDevice skyDevice)
        {
            IEnumerable<IYubiKeyDevice> devices = YubiKeyDevice.FindAll();
            foreach (IYubiKeyDevice device in devices)
            {
                if (device.IsSkySeries)
                {
                    skyDevice = device;
                    return true;
                }
            }

            skyDevice = new HollowYubiKeyDevice();
            return false;
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void SetEnabledNfcCapabilities_DisableFido2_OnlyFido2Disabled(
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            testDevice = ResetDeviceInfo(testDevice);

            // Turn off FIDO2
            YubiKeyCapabilities desiredCapabilities =
                testDevice.AvailableNfcCapabilities & ~YubiKeyCapabilities.Fido2;
            testDevice.SetEnabledNfcCapabilities(desiredCapabilities);
            testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);

            Assert.Equal(desiredCapabilities, testDevice.EnabledNfcCapabilities);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void SetEnabledUsbCapabilities_EnableFido2OverOtp_Fido2AndOtpEnabled(
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            testDevice = ResetDeviceInfo(testDevice);

            // Enable only Otp USB capabilities
            YubiKeyCapabilities desiredCapabilities = YubiKeyCapabilities.Otp;
            testDevice.SetEnabledUsbCapabilities(desiredCapabilities);
            testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);

            Assert.Equal(desiredCapabilities, testDevice.EnabledUsbCapabilities);

            // Turn on FIDO2
            desiredCapabilities = testDevice.EnabledUsbCapabilities | YubiKeyCapabilities.Fido2;
            testDevice.SetEnabledUsbCapabilities(desiredCapabilities);
            testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);

            Assert.Equal(desiredCapabilities, testDevice.EnabledUsbCapabilities);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void SetEnabledUsbCapabilities_DisableFido2_OnlyFido2Disabled(
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            testDevice = ResetDeviceInfo(testDevice);

            // Turn off FIDO2
            YubiKeyCapabilities desiredCapabilities =
                testDevice.AvailableUsbCapabilities & ~YubiKeyCapabilities.Fido2;
            testDevice.SetEnabledUsbCapabilities(desiredCapabilities);
            testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);

            Assert.Equal(desiredCapabilities, testDevice.EnabledUsbCapabilities);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void SetChallengeResponseTimeout_255seconds_ValueSetTo255(
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            testDevice = ResetDeviceInfo(testDevice);

            int expectedTimeout = 255;
            testDevice.SetChallengeResponseTimeout(expectedTimeout);
            testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);

            Assert.Equal(expectedTimeout, testDevice.ChallengeResponseTimeout);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void SetChallengeResponseTimeout_ZeroSeconds_DefaultValueSet(
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            testDevice = ResetDeviceInfo(testDevice);

            int requestedTimeout = 0;
            int expectedTimeout = 15;
            testDevice.SetChallengeResponseTimeout(requestedTimeout);
            testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);

            Assert.Equal(expectedTimeout, testDevice.ChallengeResponseTimeout);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5, ushort.MinValue)]
        [InlineData(StandardTestDevice.Fw5, ushort.MaxValue)]
        [InlineData(StandardTestDevice.Fw5Fips, ushort.MinValue)]
        [InlineData(StandardTestDevice.Fw5Fips, ushort.MaxValue)]
        public void SetAutoEjectTimeout_LimitValues_SetCorrectly(
            StandardTestDevice testDeviceType,
            int expectedTimeout)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            testDevice = ResetDeviceInfo(testDevice);

            testDevice.SetAutoEjectTimeout(expectedTimeout);

            // Must enable this flag in order to retrieve value
            testDevice.SetDeviceFlags(DeviceFlags.TouchEject);

            testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);

            Assert.Equal(expectedTimeout, testDevice.AutoEjectTimeout);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void SetDeviceFlags_RemoteWakeupAndTouchEject_BothFlagsSet(
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            testDevice = ResetDeviceInfo(testDevice);

            DeviceFlags expectedDeviceFlags = DeviceFlags.RemoteWakeup | DeviceFlags.TouchEject;
            testDevice.SetDeviceFlags(expectedDeviceFlags);
            testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);

            Assert.Equal(expectedDeviceFlags, testDevice.DeviceFlags);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void LockConfiguration_ValidLockCode_DeviceIsLocked(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            testDevice = ResetDeviceInfo(testDevice);

            testDevice.LockConfiguration(TestLockCode);

            testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);
            Assert.True(testDevice.ConfigurationLocked);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void LockConfiguration_SetLockCodeOnLockedDevice_ThrowsException(
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            testDevice = ResetDeviceInfo(testDevice);

            testDevice.LockConfiguration(TestLockCode);

            // Assert pre-conditions
            testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);
            Assert.True(testDevice.ConfigurationLocked);

            // Test
            _ = Assert.Throws<InvalidOperationException>(
                () => testDevice.LockConfiguration(TestLockCode));
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void UnlockConfiguration_CorrectLockCode_DeviceNotLocked(
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            testDevice = ResetDeviceInfo(testDevice);
            testDevice.LockConfiguration(TestLockCode);

            // Assert pre-conditions
            testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);
            Assert.True(testDevice.ConfigurationLocked);

            // Test
            testDevice.UnlockConfiguration(TestLockCode);

            testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);
            Assert.False(testDevice.ConfigurationLocked);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void UnlockConfiguration_IncorrectLockCode_ThrowsException(
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            testDevice = ResetDeviceInfo(testDevice);
            testDevice.LockConfiguration(TestLockCode);

            // Assert pre-conditions
            testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);
            Assert.True(testDevice.ConfigurationLocked);

            // Test
            _ = Assert.Throws<InvalidOperationException>(
                () => testDevice.UnlockConfiguration(LockCodeAllZero));
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void UnlockConfiguration_AllZeroLockCodeOnUnlockedDevice_CommandSuccessful(
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            testDevice = ResetDeviceInfo(testDevice);

            testDevice.UnlockConfiguration(LockCodeAllZero);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void UnlockConfiguration_NonZeroLockCodeOnUnlockedDevice_ThrowsException(
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            testDevice = ResetDeviceInfo(testDevice);

            _ = Assert.Throws<InvalidOperationException>(
                () => testDevice.UnlockConfiguration(TestLockCode));
        }

        [Theory(Skip = "NEO + yk4 require power cycle after each SetLegacyDeviceConfig, which is" +
            "currently only practical to do manually with debug breakpoints.")]
        [InlineData(StandardTestDevice.Fw3)]
        [InlineData(StandardTestDevice.Fw4Fips)]
        public void SetLegacyDeviceConfig_DisableFidoInterface_OnlyFidoDisabled(
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            testDevice = ResetDeviceInfo(testDevice);

            // Turn off FIDO2
            YubiKeyCapabilities desiredCapabilities =
                YubiKeyCapabilities.Otp | YubiKeyCapabilities.Ccid;
            testDevice.SetLegacyDeviceConfiguration(desiredCapabilities, 0, false);
            testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);

            Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(
                YubiKeyCapabilities.Otp | YubiKeyCapabilities.Ccid));
            Assert.False(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.FidoU2f));
        }

        [Theory(Skip = "NEO + yk4 require power cycle after each SetLegacyDeviceConfig, which is" +
            "currently only practical to do manually with debug breakpoints.")]
        [InlineData(StandardTestDevice.Fw3)]
        [InlineData(StandardTestDevice.Fw4Fips)]
        public void SetLegacyDeviceConfig_ChallengeResponse255Seconds_TimeoutSetTo255(
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            testDevice = ResetDeviceInfo(testDevice);

            byte expectedTimeout = 255;
            testDevice.SetLegacyDeviceConfiguration(YubiKeyCapabilities.All, expectedTimeout, false);
            testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);

            Assert.Equal(expectedTimeout, testDevice.ChallengeResponseTimeout);
        }

        [Theory(Skip = "NEO + yk4 require power cycle after each SetLegacyDeviceConfig, which is" +
            "currently only practical to do manually with debug breakpoints.")]
        [InlineData(StandardTestDevice.Fw3)]
        [InlineData(StandardTestDevice.Fw4Fips)]
        public void SetLegacyDeviceConfig_ChallengeResponseZeroSeconds_DefaultValueSet(
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            testDevice = ResetDeviceInfo(testDevice);

            byte requestedTimeout = 0;
            byte expectedTimeout = 15;
            testDevice.SetLegacyDeviceConfiguration(YubiKeyCapabilities.All, requestedTimeout, false);
            testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);

            Assert.Equal(expectedTimeout, testDevice.ChallengeResponseTimeout);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Gets_new_Fields( //TODO make tests that make sense
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            Assert.False(string.IsNullOrWhiteSpace(testDevice.PartNumber));
        }

        private static IYubiKeyDevice ResetDeviceInfo(IYubiKeyDevice testDevice)
        {
            IYubiKeyResponse response;

            if (testDevice.FirmwareVersion.Major >= 5)
            {
                var baseCommand = new SetDeviceInfoCommand
                {
                    EnabledNfcCapabilities = YubiKeyCapabilities.All,
                    EnabledUsbCapabilities = YubiKeyCapabilities.All,
                    ChallengeResponseTimeout = 0, // reset to default
                    AutoEjectTimeout = 0,
                    DeviceFlags = DeviceFlags.None,

                    ResetAfterConfig = true,
                };

                baseCommand.SetLockCode(LockCodeAllZero);

                if (testDevice.ConfigurationLocked)
                {
                    baseCommand.ApplyLockCode(TestLockCode);
                }

                response = SendConfiguration(testDevice, baseCommand);
            }
            else
            {
                var baseCommand = new SetLegacyDeviceConfigCommand(
                    YubiKeyCapabilities.All,
                    0,
                    false,
                    0);

                response = SendConfiguration(testDevice, baseCommand);
            }

            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }

            testDevice = TestDeviceSelection.RenewDeviceEnumeration(testDevice.SerialNumber!.Value);

            return testDevice;
        }

        private static IYubiKeyResponse SendConfiguration(
            IYubiKeyDevice yubiKey,
            SetDeviceInfoBaseCommand baseCommand)
        {
            IYubiKeyCommand<IYubiKeyResponse> command;

            if (yubiKey.TryConnect(YubiKeyApplication.Management, out IYubiKeyConnection? connection))
            {
                command = new SetDeviceInfoCommand(baseCommand);
            }
            else if (yubiKey.TryConnect(YubiKeyApplication.Otp, out connection))
            {
                command = new Otp.Commands.SetDeviceInfoCommand(baseCommand);
            }
            else
            {
                throw new NotSupportedException(ExceptionMessages.NoInterfaceAvailable);
            }

            using (connection)
            {
                return connection.SendCommand(command);
            }
        }

        private static IYubiKeyResponse SendConfiguration(
            IYubiKeyDevice yubiKey,
            SetLegacyDeviceConfigBase baseCommand)
        {
            IYubiKeyCommand<IYubiKeyResponse> command;

            if (yubiKey.TryConnect(YubiKeyApplication.Management, out IYubiKeyConnection? connection))
            {
                command = new SetLegacyDeviceConfigCommand(baseCommand);
            }
            else if (yubiKey.TryConnect(YubiKeyApplication.Otp, out connection))
            {
                command = new Otp.Commands.SetLegacyDeviceConfigCommand(baseCommand);
            }
            else
            {
                throw new NotSupportedException(ExceptionMessages.NoInterfaceAvailable);
            }

            using (connection)
            {
                return connection.SendCommand(command);
            }
        }
    }
}
