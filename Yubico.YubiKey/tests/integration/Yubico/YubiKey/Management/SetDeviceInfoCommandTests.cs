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

using Xunit;
using Yubico.YubiKey.Management.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Management
{
    public class SetDeviceInfoCommandTests
    {
        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void SetDeviceInfo_NoData_ResponseStatusSuccess(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using IYubiKeyConnection connection = testDevice.Connect(YubiKeyApplication.Management);

            var setCommand = new SetDeviceInfoCommand();

            YubiKeyResponse setDeviceInfoResponse = connection.SendCommand(setCommand);
            Assert.Equal(ResponseStatus.Success, setDeviceInfoResponse.Status);
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void SetDeviceInfo_NoChanges_DeviceInfoNotChanged(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice beginningTestDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            int testDeviceSerialNumber = beginningTestDevice.SerialNumber!.Value;

            using (IYubiKeyConnection connection = beginningTestDevice.Connect(YubiKeyApplication.Management))
            {
                var setCommand = new SetDeviceInfoCommand { ResetAfterConfig = true };

                YubiKeyResponse setDeviceInfoResponse = connection.SendCommand(setCommand);
                Assert.Equal(ResponseStatus.Success, setDeviceInfoResponse.Status);
            }

            IYubiKeyDevice endingTestDevice =
                TestDeviceSelection.RenewDeviceEnumeration(testDeviceSerialNumber);

            AssertDeviceInfoValueEquals(beginningTestDevice, endingTestDevice);
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void SetDeviceInfo_SameAsCurrentDeviceInfo_NoChange(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice beginningTestDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            int testDeviceSerialNumber = beginningTestDevice.SerialNumber!.Value;

            using (IYubiKeyConnection connection = beginningTestDevice.Connect(YubiKeyApplication.Management))
            {
                SetDeviceInfoCommand setCommand = CreateSetDeviceInfoCommand(beginningTestDevice);
                setCommand.ResetAfterConfig = true;

                YubiKeyResponse setDeviceInfoResponse = connection.SendCommand(setCommand);
                Assert.Equal(ResponseStatus.Success, setDeviceInfoResponse.Status);
            }

            IYubiKeyDevice endingTestDevice =
                TestDeviceSelection.RenewDeviceEnumeration(testDeviceSerialNumber);

            AssertDeviceInfoValueEquals(beginningTestDevice, endingTestDevice);
        }

        private static void AssertDeviceInfoValueEquals(
            IYubiKeyDeviceInfo expectedDeviceInfo,
            IYubiKeyDeviceInfo actualDeviceInfo)
        {
            Assert.Equal(expectedDeviceInfo.AvailableUsbCapabilities, actualDeviceInfo.AvailableUsbCapabilities);
            Assert.Equal(expectedDeviceInfo.EnabledUsbCapabilities, actualDeviceInfo.EnabledUsbCapabilities);
            Assert.Equal(expectedDeviceInfo.AvailableNfcCapabilities, actualDeviceInfo.AvailableNfcCapabilities);
            Assert.Equal(expectedDeviceInfo.EnabledNfcCapabilities, actualDeviceInfo.EnabledNfcCapabilities);
            Assert.Equal(expectedDeviceInfo.SerialNumber, actualDeviceInfo.SerialNumber);
            Assert.Equal(expectedDeviceInfo.IsFipsSeries, actualDeviceInfo.IsFipsSeries);
            Assert.Equal(expectedDeviceInfo.IsSkySeries, actualDeviceInfo.IsSkySeries);
            Assert.Equal(expectedDeviceInfo.FormFactor, actualDeviceInfo.FormFactor);
            Assert.Equal(expectedDeviceInfo.FirmwareVersion, actualDeviceInfo.FirmwareVersion);
            Assert.Equal(expectedDeviceInfo.AutoEjectTimeout, actualDeviceInfo.AutoEjectTimeout);
            Assert.Equal(expectedDeviceInfo.ChallengeResponseTimeout, actualDeviceInfo.ChallengeResponseTimeout);
            Assert.Equal(expectedDeviceInfo.DeviceFlags, actualDeviceInfo.DeviceFlags);
            Assert.Equal(expectedDeviceInfo.ConfigurationLocked, actualDeviceInfo.ConfigurationLocked);
        }

        private static SetDeviceInfoCommand CreateSetDeviceInfoCommand(IYubiKeyDeviceInfo deviceInfo) =>
            new SetDeviceInfoCommand
            {
                EnabledUsbCapabilities = deviceInfo.EnabledUsbCapabilities,
                EnabledNfcCapabilities = deviceInfo.EnabledNfcCapabilities,
                ChallengeResponseTimeout = deviceInfo.ChallengeResponseTimeout,
                AutoEjectTimeout = deviceInfo.AutoEjectTimeout,
                DeviceFlags = deviceInfo.DeviceFlags
            };
    }
}
