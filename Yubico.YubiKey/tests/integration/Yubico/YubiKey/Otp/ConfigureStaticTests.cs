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
using Yubico.Core.Devices.Hid;
using Yubico.YubiKey.Otp.Operations;
using Yubico.YubiKey.Scp;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Otp
{
    public class ConfigureStaticTests
    {

        [Trait(TraitTypes.Category, TestCategories.Simple)]
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void ConfigureStaticPassword_Succeeds(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var otpSession = new OtpSession(testDevice, Scp03KeyParameters.DefaultKey))
            {
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
        }
    }
}
