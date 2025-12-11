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
using Yubico.Core.Devices.Hid;
using Yubico.YubiKey.Otp.Operations;
using Yubico.YubiKey.Scp;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Otp
{
    public class OtpSessionTests
    {

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void ConfigureStaticPassword_Succeeds(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using var otpSession = new OtpSession(testDevice);
            if (otpSession.IsLongPressConfigured)
            {
                otpSession.DeleteSlot(Slot.LongPress);
            }

            ConfigureStaticPassword configObj = otpSession.ConfigureStaticPassword(Slot.LongPress);
            Assert.NotNull(configObj);

            var generatedPassword = new Memory<char>(new char[16]);

            configObj = configObj.WithKeyboard(KeyboardLayout.en_US);
            configObj = configObj.AllowManualUpdate(false);
            configObj = configObj.AppendCarriageReturn(false);
            configObj = configObj.SendTabFirst(false);
            configObj = configObj.SetAllowUpdate();
            configObj = configObj.GeneratePassword(generatedPassword);
            configObj.Execute();
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void ConfigureStaticPassword_WithWScp_Succeeds(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using var otpSession = new OtpSession(testDevice, Scp03KeyParameters.DefaultKey);
            if (otpSession.IsLongPressConfigured)
            {
                otpSession.DeleteSlot(Slot.LongPress);
            }

            ConfigureStaticPassword configObj = otpSession.ConfigureStaticPassword(Slot.LongPress);
            Assert.NotNull(configObj);

            var generatedPassword = new Memory<char>(new char[16]);

            configObj = configObj.WithKeyboard(KeyboardLayout.en_US);
            configObj = configObj.AllowManualUpdate(false);
            configObj = configObj.AppendCarriageReturn(false);
            configObj = configObj.SendTabFirst(false);
            configObj = configObj.SetAllowUpdate();
            configObj = configObj.GeneratePassword(generatedPassword);
            configObj.Execute();
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void ConfigureYubicoOtp_WithSerialNumberVisibility_Succeeds(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var otpSession = new OtpSession(testDevice))
            {
                if (otpSession.IsLongPressConfigured)
                {
                    otpSession.DeleteSlot(Slot.LongPress);
                }
            }

            using (var otpSession = new OtpSession(testDevice))
            {
                Memory<byte> privateId = new byte[ConfigureYubicoOtp.PrivateIdentifierSize];
                Memory<byte> aesKey = new byte[ConfigureYubicoOtp.KeySize];

                otpSession.ConfigureYubicoOtp(Slot.LongPress)
                    .SetSerialNumberApiVisible()
                    .SetSerialNumberButtonVisible()
                    .SetSerialNumberUsbVisible()
                    .UseSerialNumberAsPublicId()
                    .GeneratePrivateId(privateId)
                    .GenerateKey(aesKey)
                    .Execute();
            }

            using (var otpSession = new OtpSession(testDevice))
            {
                Assert.True(otpSession.IsLongPressConfigured, "Slot should be configured after Execute()");
            }
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void ConfigureStaticPassword_WithSerialNumberVisibility_Succeeds(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var otpSession = new OtpSession(testDevice))
            {
                if (otpSession.IsLongPressConfigured)
                {
                    otpSession.DeleteSlot(Slot.LongPress);
                }
            }

            using (var otpSession = new OtpSession(testDevice))
            {
                Memory<char> generatedPassword = new char[16];

                otpSession.ConfigureStaticPassword(Slot.LongPress)
                    .WithKeyboard(KeyboardLayout.en_US)
                    .SetSerialNumberApiVisible()
                    .SetSerialNumberButtonVisible()
                    .SetSerialNumberUsbVisible()
                    .GeneratePassword(generatedPassword)
                    .Execute();
            }

            using (var otpSession = new OtpSession(testDevice))
            {
                Assert.True(otpSession.IsLongPressConfigured, "Slot should be configured after Execute()");
            }
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void ConfigureHotp_WithSerialNumberVisibility_Succeeds(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var otpSession = new OtpSession(testDevice))
            {
                if (otpSession.IsLongPressConfigured)
                {
                    otpSession.DeleteSlot(Slot.LongPress);
                }
            }

            using (var otpSession = new OtpSession(testDevice))
            {
                Memory<byte> hmacKey = new byte[ConfigureHotp.HmacKeySize];

                otpSession.ConfigureHotp(Slot.LongPress)
                    .SetSerialNumberApiVisible()
                    .SetSerialNumberButtonVisible()
                    .SetSerialNumberUsbVisible()
                    .GenerateKey(hmacKey)
                    .Execute();
            }

            using (var otpSession = new OtpSession(testDevice))
            {
                Assert.True(otpSession.IsLongPressConfigured, "Slot should be configured after Execute()");
            }
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void ConfigureChallengeResponse_WithSerialNumberVisibility_Succeeds(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var otpSession = new OtpSession(testDevice))
            {
                if (otpSession.IsLongPressConfigured)
                {
                    otpSession.DeleteSlot(Slot.LongPress);
                }
            }

            using (var otpSession = new OtpSession(testDevice))
            {
                Memory<byte> hmacKey = new byte[ConfigureChallengeResponse.HmacSha1KeySize];

                otpSession.ConfigureChallengeResponse(Slot.LongPress)
                    .SetSerialNumberApiVisible()
                    .SetSerialNumberButtonVisible()
                    .SetSerialNumberUsbVisible()
                    .UseHmacSha1()
                    .GenerateKey(hmacKey)
                    .Execute();
            }

            using (var otpSession = new OtpSession(testDevice))
            {
                Assert.True(otpSession.IsLongPressConfigured, "Slot should be configured after Execute()");
            }
        }

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void ConfigureNdef_WithSerialNumberVisibility_Succeeds(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var otpSession = new OtpSession(testDevice))
            {
                if (otpSession.IsLongPressConfigured)
                {
                    otpSession.DeleteSlot(Slot.LongPress);
                }
            }

            // NDEF requires an already configured slot - configure with HOTP first
            using (var otpSession = new OtpSession(testDevice))
            {
                Memory<byte> hmacKey = new byte[ConfigureHotp.HmacKeySize];

                otpSession.ConfigureHotp(Slot.LongPress)
                    .GenerateKey(hmacKey)
                    .Execute();
            }

            // Configure NDEF to use that slot with serial number visibility settings
            // Note: NDEF configuration does not alter slot state, it only sets NDEF to use the slot.
            // Verification would require NFC hardware to read the NDEF tag, so we verify
            // that Execute() succeeds without throwing an exception.
            using (var otpSession = new OtpSession(testDevice))
            {
                otpSession.ConfigureNdef(Slot.LongPress)
                    .SetSerialNumberApiVisible()
                    .SetSerialNumberButtonVisible()
                    .SetSerialNumberUsbVisible()
                    .AsUri(new Uri("https://example.com"))
                    .Execute();
            }

            // Verify the slot is still configured (NDEF doesn't change slot configuration)
            using (var otpSession = new OtpSession(testDevice))
            {
                Assert.True(otpSession.IsLongPressConfigured, "Slot should remain configured");
            }
        }
    }
}
