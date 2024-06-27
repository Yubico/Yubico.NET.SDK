
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
using System.Security;
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
        public void ChangePivPinToInvalidValue_ThrowsSecurityException()
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5Fips);
            Skip.IfNot(testDevice.IsPinComplexityEnabled);

            using var pivSession = new PivSession(testDevice);
            pivSession.ResetApplication();

            Assert.True(pivSession.TryChangePin(defaultPin, complexPin, out _));
            int? retriesRemaining = 3;
            var e = Assert.Throws<SecurityException>(() => pivSession.TryChangePin(complexPin, invalidPin, out retriesRemaining));
            Assert.Equal(ExceptionMessages.PinComplexityViolation, e.Message);
            Assert.Null(retriesRemaining);
        }

        [SkippableFact]
        public void ChangePivPukToInvalidValue_ThrowsSecurityException()
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5Fips);
            Skip.IfNot(testDevice.IsPinComplexityEnabled);

            using var pivSession = new PivSession(testDevice);
            pivSession.ResetApplication();

            Assert.True(pivSession.TryChangePuk(defaultPuk, complexPuk, out _));
            int? retriesRemaining = 3;

            var e = Assert.Throws<SecurityException>(() => pivSession.TryChangePuk(complexPuk, invalidPuk, out retriesRemaining));
            Assert.Equal(ExceptionMessages.PinComplexityViolation, e.Message);
            Assert.Null(retriesRemaining);
        }

        [SkippableFact]
        public void ResetPivPinToInvalidValue_ThrowsSecurityException()
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5Fips);
            Skip.IfNot(testDevice.IsPinComplexityEnabled);

            using var pivSession = new PivSession(testDevice);
            pivSession.ResetApplication();

            Assert.True(pivSession.TryResetPin(defaultPuk, complexPin, out _));
            int? retriesRemaining = 3;
            var e = Assert.Throws<SecurityException>(() => pivSession.TryResetPin(defaultPuk, invalidPin, out retriesRemaining));
            Assert.Equal(ExceptionMessages.PinComplexityViolation, e.Message);
            Assert.Null(retriesRemaining);
        }        

        [SkippableFact]
        public void SetFido2PinToInvalidValue_ThrowsFido2Exception()
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5Fips);
            Skip.IfNot(testDevice.IsPinComplexityEnabled);

            using var fido2Session = new Fido2Session(testDevice);

            // set violating PIN
            var fido2Exception = Assert.Throws<Fido2Exception>(() => fido2Session.TrySetPin(invalidPin));
            Assert.Equal(CtapStatus.PinPolicyViolation, fido2Exception.Status);
            // set complex PIN to be able to try to change it later
            Assert.True(fido2Session.TrySetPin(complexPin));
            // change to violating PIN
            fido2Exception = Assert.Throws<Fido2Exception>(() => fido2Session.TryChangePin(complexPin, invalidPin));
            Assert.Equal(CtapStatus.PinPolicyViolation, fido2Exception.Status);
        }
    }
}
