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
using System.Diagnostics.CodeAnalysis;
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    public class GetDeviceInfoResponseTests
    {
        private const byte UsbPrePersCapabilitiesTag = 0x01;
        private const byte SerialNumberTag = 0x02;
        private const byte UsbEnabledCapabilitiesTag = 0x03;
        private const byte FormFactorTag = 0x04;
        private const byte FirmwareVersionTag = 0x05;
        private const byte AutoEjectTimeoutTag = 0x06;
        private const byte ChallengeResponseTimeoutTag = 0x07;
        private const byte DeviceFlagsTag = 0x08;
        private const byte ConfigurationLockPresentTag = 0x0a;
        //private const byte ConfigurationUnlockTag = 0x0b;
        //private const byte ResetTag = 0x0c;
        private const byte NfcPrePersCapabilitiesTag = 0x0d;
        private const byte NfcEnabledCapabilitiesTag = 0x0e;

        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            static void action() => _ = new GetDeviceInfoResponse(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            _ = Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var deviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            Assert.Equal(SWConstants.Success, deviceInfoResponse.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var deviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, deviceInfoResponse.Status);
        }

        [Fact]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Unit test")]
        public void Constructor_SuccessResponseApdu_NoThrowIfFailed()
        {
            bool isThrow = false;
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var deviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            try
            {
                deviceInfoResponse.ThrowIfFailed();
            }
            catch (Exception)
            {
                isThrow = true;
            }

            Assert.False(isThrow);
        }

        [Fact]
        public void GetData_ResponseApduFailed_ThrowsInvalidOperationException()
        {
            byte sw1 = unchecked((byte)(SWConstants.ExecutionError >> 8));
            byte sw2 = unchecked((byte)SWConstants.ExecutionError);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });
            var getDeviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            void action() => _ = getDeviceInfoResponse.GetData();

            _ = Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void GetData_UsbPrePersCapabilitiesTagPresentAsShort_SetsPropertyCorrectly()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x04, UsbPrePersCapabilitiesTag, 0x02, 0x03, 0x3F, 0x90, 0x00 });
            var getDeviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            YubiKeyDeviceInfo deviceInfo = getDeviceInfoResponse.GetData();

            Assert.Equal(YubiKeyCapabilities.All, deviceInfo.AvailableUsbCapabilities);
        }

        [Fact]
        public void GetData_UsbPrePersCapabilitiesTagPresentAsByte_SetsPropertyCorrectly()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x03, UsbPrePersCapabilitiesTag, 0x01, 0x3F, 0x90, 0x00 });
            var getDeviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            YubiKeyDeviceInfo deviceInfo = getDeviceInfoResponse.GetData();

            Assert.Equal((YubiKeyCapabilities)0x3F, deviceInfo.AvailableUsbCapabilities);
        }

        [Fact]
        public void GetData_SerialNumberTagPresent_SetsPropertyCorrectly()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x06, SerialNumberTag, 0x04, 0x01, 0x02, 0x03, 0x04, 0x90, 0x00 });
            var getDeviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            YubiKeyDeviceInfo deviceInfo = getDeviceInfoResponse.GetData();

            Assert.Equal(0x01020304, deviceInfo.SerialNumber);
        }

        [Fact]
        public void GetData_UsbEnabledCapabilitiesTagPresentAsShort_SetsPropertyCorrectly()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x04, UsbEnabledCapabilitiesTag, 0x02, 0x03, 0x3F, 0x90, 0x00 });
            var getDeviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            YubiKeyDeviceInfo deviceInfo = getDeviceInfoResponse.GetData();

            Assert.Equal(YubiKeyCapabilities.All, deviceInfo.EnabledUsbCapabilities);
        }

        [Fact]
        public void GetData_UsbEnabledCapabilitiesTagPresentAsByte_SetsPropertyCorrectly()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x03, UsbEnabledCapabilitiesTag, 0x01, 0x3F, 0x90, 0x00 });
            var getDeviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            YubiKeyDeviceInfo deviceInfo = getDeviceInfoResponse.GetData();

            Assert.Equal((YubiKeyCapabilities)0x3F, deviceInfo.EnabledUsbCapabilities);
        }

        [Fact]
        public void GetData_FormFactorTagPresent_SetsPropertyCorrectly()
        {
            FormFactor expectedFormFactor = FormFactor.UsbCLightning;
            var responseApdu = new ResponseApdu(new byte[] { 0x03, FormFactorTag, 0x01, (byte)expectedFormFactor, 0x90, 0x00 });
            var getDeviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            YubiKeyDeviceInfo deviceInfo = getDeviceInfoResponse.GetData();

            Assert.Equal(expectedFormFactor, deviceInfo.FormFactor);
        }

        [Fact]
        public void GetData_FirmwareVersionTagPresent_SetsPropertyCorrectly()
        {
            var expectedVersion = new FirmwareVersion() { Major = 0x01, Minor = 0x02, Patch = 0x03 };
            var responseApdu = new ResponseApdu(new byte[] { 0x05, FirmwareVersionTag, 0x03, expectedVersion.Major, expectedVersion.Minor, expectedVersion.Patch, 0x90, 0x00 });
            var getDeviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            YubiKeyDeviceInfo deviceInfo = getDeviceInfoResponse.GetData();

            Assert.Equal(expectedVersion, deviceInfo.FirmwareVersion);
        }

        [Fact]
        public void GetData_AutoEjectTimeoutTagPresent_SetsPropertyCorrectly()
        {
            short expectedTimeout = 0x1234;
            var responseApdu = new ResponseApdu(new byte[] { 0x04, AutoEjectTimeoutTag, 0x02, 0x12, 0x34, 0x90, 0x00 });
            var getDeviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            YubiKeyDeviceInfo deviceInfo = getDeviceInfoResponse.GetData();

            Assert.Equal(expectedTimeout, deviceInfo.AutoEjectTimeout);
        }

        [Fact]
        public void GetData_ChallengeResponseTimeoutTagPresent_SetsPropertyCorrectly()
        {
            byte expectedTimeout = 0x12;
            var responseApdu = new ResponseApdu(new byte[] { 0x03, ChallengeResponseTimeoutTag, 0x01, expectedTimeout, 0x90, 0x00 });
            var getDeviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            YubiKeyDeviceInfo deviceInfo = getDeviceInfoResponse.GetData();

            Assert.Equal(expectedTimeout, deviceInfo.ChallengeResponseTimeout);
        }

        [Fact]
        public void GetData_DeviceFlagsTagPresent_SetsPropertyCorrectly()
        {
            DeviceFlags deviceFlags = DeviceFlags.RemoteWakeup | DeviceFlags.TouchEject;
            var responseApdu = new ResponseApdu(new byte[] { 0x03, DeviceFlagsTag, 0x01, (byte)deviceFlags, 0x90, 0x00 });
            var getDeviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            YubiKeyDeviceInfo deviceInfo = getDeviceInfoResponse.GetData();

            Assert.Equal(deviceFlags, deviceInfo.DeviceFlags);
        }

        [Fact]
        public void GetData_ConfigurationLockPresentTagPresent_SetsPropertyCorrectly()
        {
            bool expectedValue = true;
            var responseApdu = new ResponseApdu(new byte[] { 0x03, ConfigurationLockPresentTag, 0x01, 0x01, 0x90, 0x00 });
            var getDeviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            YubiKeyDeviceInfo deviceInfo = getDeviceInfoResponse.GetData();

            Assert.Equal(expectedValue, deviceInfo.ConfigurationLocked);
        }

        [Fact]
        public void GetData_NfcPrePersCapabilitiesTagPresentAsShort_SetsPropertyCorrectly()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x04, NfcPrePersCapabilitiesTag, 0x02, 0x03, 0x3F, 0x90, 0x00 });
            var getDeviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            YubiKeyDeviceInfo deviceInfo = getDeviceInfoResponse.GetData();

            Assert.Equal(YubiKeyCapabilities.All, deviceInfo.AvailableNfcCapabilities);
        }

        [Fact]
        public void GetData_NfcPrePersCapabilitiesTagPresentAsByte_SetsPropertyCorrectly()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x03, NfcPrePersCapabilitiesTag, 0x01, 0x3F, 0x90, 0x00 });
            var getDeviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            YubiKeyDeviceInfo deviceInfo = getDeviceInfoResponse.GetData();

            Assert.Equal((YubiKeyCapabilities)0x3F, deviceInfo.AvailableNfcCapabilities);
        }

        [Fact]
        public void GetData_NfcEnabledCapabilitiesTagPresentAsShort_SetsPropertyCorrectly()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x04, NfcEnabledCapabilitiesTag, 0x02, 0x03, 0x3F, 0x90, 0x00 });
            var getDeviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            YubiKeyDeviceInfo deviceInfo = getDeviceInfoResponse.GetData();

            Assert.Equal(YubiKeyCapabilities.All, deviceInfo.EnabledNfcCapabilities);
        }

        [Fact]
        public void GetData_NfcEnabledCapabilitiesTagPresentAsByte_SetsPropertyCorrectly()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x03, NfcEnabledCapabilitiesTag, 0x01, 0x3F, 0x90, 0x00 });
            var getDeviceInfoResponse = new GetDeviceInfoResponse(responseApdu);

            YubiKeyDeviceInfo deviceInfo = getDeviceInfoResponse.GetData();

            Assert.Equal((YubiKeyCapabilities)0x3F, deviceInfo.EnabledNfcCapabilities);
        }
    }
}
