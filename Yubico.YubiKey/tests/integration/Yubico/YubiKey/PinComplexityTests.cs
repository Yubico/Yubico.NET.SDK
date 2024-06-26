
// Copyright 2024 Yubico AB
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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Xunit;
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Otp;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.TestUtilities;
using Log = Yubico.Core.Logging.Log;

namespace Yubico.YubiKey
{
    /// <summary>
    /// Tests device that it will not accept PINs or PUKs which violate PIN complexity
    /// Before running the tests reset the device
    /// </summary>
    public class PinComplexityTests
    {

        private readonly ReadOnlyMemory<byte> defaultPin = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("123456"));
        private readonly ReadOnlyMemory<byte> complexPin = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("11234567"));
        private readonly ReadOnlyMemory<byte> invalidPin = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("33333333"));

        private readonly ReadOnlyMemory<byte> defaultPuk = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("12345678"));
        private readonly ReadOnlyMemory<byte> complexPuk = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("11234567"));
        private readonly ReadOnlyMemory<byte> invalidPuk = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("33333333"));

        [SkippableFact]
        public void SettingInvalidPivPin_Throws()
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5Fips);
            Skip.IfNot(testDevice.IsPinComplexityEnabled);

            using var pivSession = new PivSession(testDevice);
            pivSession.ResetApplication();

            Assert.True(pivSession.TryChangePin(currentPin: defaultPin, newPin: complexPin, out _));
            _ = Assert.Throws<ArgumentException>(() => pivSession.TryChangePin(currentPin: complexPin, newPin: invalidPin, out _));
        }

        [SkippableFact]
        public void SettingInvalidPivPuk_Throws()
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5Fips);

            Skip.IfNot(testDevice.IsPinComplexityEnabled);

            using var pivSession = new PivSession(testDevice);
            pivSession.ResetApplication();

            Assert.True(pivSession.TryChangePuk(currentPuk: defaultPuk, newPuk: complexPuk, out _));
            _ = Assert.Throws<ArgumentException>(() => pivSession.TryChangePuk(currentPuk: complexPuk, newPuk: invalidPuk, out _));
        }

        [SkippableFact]
        public void SettingInvalidFido2Pin_Throws()
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5Fips);
            Skip.IfNot(testDevice.IsPinComplexityEnabled);

            using var fido2Session = new Fido2Session(testDevice);

            // set violating PIN
            _ = Assert.Throws<ArgumentException>(() => fido2Session.TrySetPin(invalidPin));
            // set complex PIN to be able to try to change it later
            Assert.True(fido2Session.TrySetPin(complexPin));
            // change to violating PIN
            _ = Assert.Throws<ArgumentException>(() => fido2Session.TryChangePin(complexPin, invalidPin));
        }
    }
}
