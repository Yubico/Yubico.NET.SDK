
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
using System.Security;
using System.Text;
using Xunit;
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey
{
    /// <summary>
    /// Tests device that it will not accept PINs or PUKs which violate PIN complexity
    /// Before running the tests reset the device
    /// </summary>
    public class PinComplexityTests
    {

        private readonly ReadOnlyMemory<byte> _defaultPin = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("123456"));
        private readonly ReadOnlyMemory<byte> _complexPin = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("11234567"));
        private readonly ReadOnlyMemory<byte> _invalidPin = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("33333333"));

        private readonly ReadOnlyMemory<byte> _defaultPuk = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("12345678"));
        private readonly ReadOnlyMemory<byte> _complexPuk = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("11234567"));
        private readonly ReadOnlyMemory<byte> _invalidPuk = new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes("33333333"));

        [SkippableFact]
        public void ChangePivPinToInvalidValue_ThrowsSecurityException()
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5Fips);
            Skip.IfNot(testDevice.IsPinComplexityEnabled);

            using var pivSession = new PivSession(testDevice);
            pivSession.ResetApplication();

            Assert.True(pivSession.TryChangePin(_defaultPin, _complexPin, out _));
            int? retriesRemaining = 3;
            var e = Assert.Throws<SecurityException>(() => pivSession.TryChangePin(_complexPin, _invalidPin, out retriesRemaining));
            Assert.Equal(ExceptionMessages.PinComplexityViolation, e.Message);
            Assert.Null(retriesRemaining);
        }

        [SkippableFact]
        public void ChangePivPukToInvalidValue_ThrowsSecurityException()
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5Fips);
            Skip.IfNot(testDevice.IsPinComplexityEnabled);

            using var pivSession = new PivSession(testDevice);
            pivSession.ResetApplication();

            Assert.True(pivSession.TryChangePuk(_defaultPuk, _complexPuk, out _));
            int? retriesRemaining = 3;

            var e = Assert.Throws<SecurityException>(() => pivSession.TryChangePuk(_complexPuk, _invalidPuk, out retriesRemaining));
            Assert.Equal(ExceptionMessages.PinComplexityViolation, e.Message);
            Assert.Null(retriesRemaining);
        }

        [SkippableFact]
        public void ResetPivPinToInvalidValue_ThrowsSecurityException()
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5Fips);
            Skip.IfNot(testDevice.IsPinComplexityEnabled);

            using var pivSession = new PivSession(testDevice);
            pivSession.ResetApplication();

            Assert.True(pivSession.TryResetPin(_defaultPuk, _complexPin, out _));
            int? retriesRemaining = 3;
            var e = Assert.Throws<SecurityException>(() => pivSession.TryResetPin(_defaultPuk, _invalidPin, out retriesRemaining));
            Assert.Equal(ExceptionMessages.PinComplexityViolation, e.Message);
            Assert.Null(retriesRemaining);
        }

        [SkippableFact]
        public void SetFido2PinToInvalidValue_ThrowsFido2Exception()
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5Fips);
            Skip.IfNot(testDevice.IsPinComplexityEnabled);

            using var fido2Session = new Fido2Session(testDevice);

            // set violating PIN
            var fido2Exception = Assert.Throws<Fido2Exception>(() => fido2Session.TrySetPin(_invalidPin));
            Assert.Equal(CtapStatus.PinPolicyViolation, fido2Exception.Status);
            // set complex PIN to be able to try to change it later
            Assert.True(fido2Session.TrySetPin(_complexPin));
            // change to violating PIN
            fido2Exception = Assert.Throws<Fido2Exception>(() => fido2Session.TryChangePin(_complexPin, _invalidPin));
            Assert.Equal(CtapStatus.PinPolicyViolation, fido2Exception.Status);
        }
    }
}
