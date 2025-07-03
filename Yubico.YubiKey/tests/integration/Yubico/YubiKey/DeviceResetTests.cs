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
using System.Text;
using Xunit;
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey
{
    /// <summary>
    /// Executes device wide reset and check that PINs are set to default values (where applicable).
    /// </summary>
    /// <remarks>
    /// Device wide reset is only available on YubiKey Bio Multi-protocol Edition devices.
    /// </remarks>
    public class DeviceResetTests
    {

        private readonly ReadOnlyMemory<byte> _defaultPin = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("123456"));
        private readonly ReadOnlyMemory<byte> _complexPin = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("11234567"));

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void Reset()
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5Bio);
            Skip.IfNot(testDevice.HasFeature(YubiKeyFeature.DeviceReset), "Device does not support DeviceReset.");
            Skip.IfNot(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv), "Device does not support DeviceReset.");

            testDevice.DeviceReset();

            /* set PIN for PIV - this will also set the FIDO2 PIN */
            using (var pivSession = new PivSession(testDevice))
            {
                Assert.True(pivSession.TryChangePin(_defaultPin, _complexPin, out _));
            }

            testDevice.DeviceReset();

            /* verify that PIV has default PIN */
            using (var pivSession = new PivSession(testDevice))
            {
                Assert.True(pivSession.TryVerifyPin(_defaultPin, out _));
            }

            /* verify that FIDO2 does not have a PIN set */
            using (var fido2Session = new Fido2Session(testDevice))
            {
                var optionValue = fido2Session.AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.clientPin);
                Assert.Equal(OptionValue.False, optionValue);
            }
        }
    }
}
