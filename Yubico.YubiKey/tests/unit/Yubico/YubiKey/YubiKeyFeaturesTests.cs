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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey
{
    public class YubiKeyFeaturesTests
    {
        [Fact]
        public void YubiKey_Null_Throws()
        {
            IYubiKeyDevice? yubiKey = null;
            _ = Assert.Throws<ArgumentNullException>(() => yubiKey!.HasFeature(YubiKeyFeature.OtpApplication));
        }

        [Fact]
        public void CheckOtpApplication_ReturnsTrue()
        {
            var yubiKey = new HollowYubiKeyDevice
            {
                AvailableUsbCapabilities = YubiKeyCapabilities.Otp
            };
            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OtpApplication));
        }

        [Fact]
        public void CheckOathApplication()
        {
            var yubiKey = new HollowYubiKeyDevice
            {
                AvailableUsbCapabilities = YubiKeyCapabilities.Oath
            };

            yubiKey.FirmwareVersion.Major = 3;
            yubiKey.FirmwareVersion.Minor = 1;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OathApplication));
        }

        [Fact]
        public void CheckPivApplication()
        {
            var yubiKey = new HollowYubiKeyDevice
            {
                AvailableUsbCapabilities = YubiKeyCapabilities.Piv
            };

            yubiKey.FirmwareVersion.Major = 3;
            yubiKey.FirmwareVersion.Minor = 1;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.PivApplication));
        }

        [Fact]
        public void CheckManagementApplication()
        {
            var yubiKey = new HollowYubiKeyDevice();

            yubiKey.FirmwareVersion.Major = 5;
            yubiKey.FirmwareVersion.Minor = 0;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.ManagementApplication));

            yubiKey.FirmwareVersion.Major = 4;
            yubiKey.FirmwareVersion.Minor = 0;

            Assert.False(yubiKey.HasFeature(YubiKeyFeature.ManagementApplication));
        }

        [Fact]
        public void CheckSerialNumberVisabilityControls()
        {
            var yubiKey = new HollowYubiKeyDevice
            {
                AvailableUsbCapabilities = YubiKeyCapabilities.Otp
            };

            yubiKey.FirmwareVersion.Major = 2;
            yubiKey.FirmwareVersion.Minor = 2;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.SerialNumberVisibilityControls));

            yubiKey.FirmwareVersion.Major = 2;
            yubiKey.FirmwareVersion.Minor = 1;

            Assert.False(yubiKey.HasFeature(YubiKeyFeature.SerialNumberVisibilityControls));
        }

        [Fact]
        public void CheckScp03s()
        {
            var yubiKey = new HollowYubiKeyDevice
            {
                AvailableUsbCapabilities = YubiKeyCapabilities.Piv
            };

            yubiKey.FirmwareVersion.Major = 5;
            yubiKey.FirmwareVersion.Minor = 3;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.Scp03));

            yubiKey.FirmwareVersion.Major = 5;
            yubiKey.FirmwareVersion.Minor = 2;

            Assert.False(yubiKey.HasFeature(YubiKeyFeature.Scp03));
        }

        [Fact]
        public void CheckOathFeatures()
        {
            var yubiKey = new HollowYubiKeyDevice
            {
                AvailableUsbCapabilities = YubiKeyCapabilities.Oath
            };

            yubiKey.FirmwareVersion.Major = 5;
            yubiKey.FirmwareVersion.Minor = 3;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OathRenameCredential));

            yubiKey.FirmwareVersion.Major = 5;
            yubiKey.FirmwareVersion.Minor = 2;

            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OathRenameCredential));

            yubiKey.FirmwareVersion.Major = 4;
            yubiKey.FirmwareVersion.Minor = 3;
            yubiKey.FirmwareVersion.Patch = 1;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OathTouchCredential));

            yubiKey.FirmwareVersion.Major = 4;
            yubiKey.FirmwareVersion.Minor = 3;
            yubiKey.FirmwareVersion.Patch = 4;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OathSha512));

            yubiKey.FirmwareVersion.Major = 4;
            yubiKey.FirmwareVersion.Minor = 2;
            yubiKey.FirmwareVersion.Patch = 0;

            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OathSha512));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OathTouchCredential));
        }

        [Fact]
        public void CheckPivFeatures()
        {
            var yubiKey = new HollowYubiKeyDevice
            {
                AvailableUsbCapabilities = YubiKeyCapabilities.Piv
            };

            yubiKey.FirmwareVersion.Major = 5;
            yubiKey.FirmwareVersion.Minor = 3;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.PivMetadata));

            yubiKey.FirmwareVersion.Major = 5;
            yubiKey.FirmwareVersion.Minor = 0;

            Assert.False(yubiKey.HasFeature(YubiKeyFeature.PivMetadata));

            yubiKey.FirmwareVersion.Major = 4;
            yubiKey.FirmwareVersion.Minor = 3;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.PivAttestation));
            Assert.True(yubiKey.HasFeature(YubiKeyFeature.PivTouchPolicyCached));
            Assert.True(yubiKey.HasFeature(YubiKeyFeature.PivPrivateKeyTouchPolicyCached));

            yubiKey.FirmwareVersion.Major = 4;
            yubiKey.FirmwareVersion.Minor = 2;
            yubiKey.FirmwareVersion.Patch = 4;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.PivEccP256));
            Assert.True(yubiKey.HasFeature(YubiKeyFeature.PivEccP384));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.PivAttestation));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.PivTouchPolicyCached));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.PivPrivateKeyTouchPolicyCached));

            yubiKey.FirmwareVersion.Major = 4;
            yubiKey.FirmwareVersion.Minor = 2;
            yubiKey.FirmwareVersion.Patch = 0;

            Assert.False(yubiKey.HasFeature(YubiKeyFeature.PivEccP256));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.PivEccP384));

            yubiKey.FirmwareVersion.Major = 4;
            yubiKey.FirmwareVersion.Minor = 0;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.PivManagementKeyTouchPolicy));

            yubiKey.FirmwareVersion.Major = 3;
            yubiKey.FirmwareVersion.Minor = 1;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.PivRsa1024));
            Assert.True(yubiKey.HasFeature(YubiKeyFeature.PivRsa2048));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.PivManagementKeyTouchPolicy));

            yubiKey.FirmwareVersion.Major = 2;
            yubiKey.FirmwareVersion.Minor = 4;

            Assert.False(yubiKey.HasFeature(YubiKeyFeature.PivRsa1024));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.PivRsa2048));
        }
        [Fact]
        public void CheckOtpFeatures()
        {
            var yubiKey = new HollowYubiKeyDevice
            {
                AvailableUsbCapabilities = YubiKeyCapabilities.Otp
            };

            yubiKey.FirmwareVersion.Major = 2;
            yubiKey.FirmwareVersion.Minor = 4;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OtpInvertLed));

            yubiKey.FirmwareVersion.Major = 2;
            yubiKey.FirmwareVersion.Minor = 3;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OtpNumericKeypad));
            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OtpFastTrigger));
            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OtpUpdatableSlots));
            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OtpDormantSlots));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OtpInvertLed));

            yubiKey.FirmwareVersion.Major = 2;
            yubiKey.FirmwareVersion.Minor = 2;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OtpVariableSizeHmac));
            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OtpButtonTrigger));
            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OtpChallengeResponseMode));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OtpNumericKeypad));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OtpFastTrigger));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OtpUpdatableSlots));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OtpDormantSlots));

            yubiKey.FirmwareVersion.Major = 2;
            yubiKey.FirmwareVersion.Minor = 1;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OtpOathHotpMode));
            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OtpFixedModhex));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OtpVariableSizeHmac));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OtpButtonTrigger));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OtpChallengeResponseMode));


            yubiKey.FirmwareVersion.Major = 2;
            yubiKey.FirmwareVersion.Minor = 0;

            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OtpProtectedLongPressSlot));
            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OtpShortTickets));
            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OtpStaticPasswordMode));
            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OtpMixedCasePasswords));
            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OtpAlphaNumericPasswords));
            Assert.True(yubiKey.HasFeature(YubiKeyFeature.OtpPasswordManualUpdates));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OtpOathHotpMode));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OtpFixedModhex));

            yubiKey.FirmwareVersion.Major = 1;
            yubiKey.FirmwareVersion.Minor = 9;

            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OtpProtectedLongPressSlot));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OtpShortTickets));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OtpStaticPasswordMode));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OtpMixedCasePasswords));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OtpAlphaNumericPasswords));
            Assert.False(yubiKey.HasFeature(YubiKeyFeature.OtpPasswordManualUpdates));
        }

        [Theory]
        [InlineData(5, 4, 3, true)]
        [InlineData(5, 4, 4, true)]
        [InlineData(5, 4, 2, false)]
        public void HasFeature_YubiHsmAuthApplication(
            byte major,
            byte minor,
            byte patch,
            bool expectedResult)
        {
            var yubiKey = new HollowYubiKeyDevice()
            {
                AvailableUsbCapabilities = YubiKeyCapabilities.YubiHsmAuth
            };

            yubiKey.FirmwareVersion.Major = major;
            yubiKey.FirmwareVersion.Minor = minor;
            yubiKey.FirmwareVersion.Patch = patch;

            bool actualResult = yubiKey.HasFeature(YubiKeyFeature.YubiHsmAuthApplication);

            Assert.Equal(expectedResult, actualResult);
        }
    }
}
