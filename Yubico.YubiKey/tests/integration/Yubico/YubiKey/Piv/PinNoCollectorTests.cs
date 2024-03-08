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
using Yubico.YubiKey.Piv.Objects;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class PinNoCollectorTests
    {
        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void VerifyPin_Sign_Succeeds(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var pin = new ReadOnlyMemory<byte>(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 });

                pivSession.ResetApplication();
                bool isValid = pivSession.TryVerifyPin(pin, out int? retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                PinProtectedData pinProtect = pivSession.ReadObject<PinProtectedData>();

                Assert.True(pivSession.PinVerified);
                Assert.True(pinProtect.IsEmpty);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void VerifyPin_WrongPin_ReturnsFalse(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var pin = new ReadOnlyMemory<byte>(new byte[] { 0x41, 0x32, 0x33, 0x34, 0x35, 0x36 });

                pivSession.ResetApplication();
                bool isValid = pivSession.TryVerifyPin(pin, out int? retriesRemaining);
                Assert.False(isValid);
                _ = Assert.NotNull(retriesRemaining);
                if (!(retriesRemaining is null))
                {
                    Assert.Equal(2, retriesRemaining);
                }
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ChangePin_Succeeds(StandardTestDevice testDeviceType)
        {
            var oldPin = new ReadOnlyMemory<byte>(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 });
            var newPin = new ReadOnlyMemory<byte>(new byte[] { 0x41, 0x42, 0x43, 0x44, 0x45, 0x46 });

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.ResetApplication();
                bool isValid = pivSession.TryVerifyPin(oldPin, out int? retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                bool isValid = pivSession.TryChangePin(oldPin, newPin, out int? retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                isValid = pivSession.TryVerifyPin(newPin, out retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                PinProtectedData pinProtect = pivSession.ReadObject<PinProtectedData>();

                Assert.True(pivSession.PinVerified);
                Assert.True(pinProtect.IsEmpty);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ChangePuk_Succeeds(StandardTestDevice testDeviceType)
        {
            var oldPuk = new ReadOnlyMemory<byte>(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38 });
            var newPuk = new ReadOnlyMemory<byte>(new byte[] { 0x70, 0xE4, 0x82, 0x7D, 0x5C, 0xA1, 0x04 });

            var newPin = new ReadOnlyMemory<byte>(new byte[] { 0xE4, 0x82, 0x7D, 0x5C, 0xA1, 0x04, 0x70 });

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.ResetApplication();
                bool isValid = pivSession.TryChangePuk(oldPuk, newPuk, out int? retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                bool isValid = pivSession.TryResetPin(newPuk, newPin, out int? retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                isValid = pivSession.TryVerifyPin(newPin, out retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                PinProtectedData pinProtect = pivSession.ReadObject<PinProtectedData>();

                Assert.True(pivSession.PinVerified);
                Assert.True(pinProtect.IsEmpty);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void ChangeRetryCounts_Succeeds(StandardTestDevice testDeviceType)
        {
            var mgmtKey = new ReadOnlyMemory<byte>(new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            });
            var oldPin = new ReadOnlyMemory<byte>(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 });
            var newPin = new ReadOnlyMemory<byte>(new byte[] { 0xE4, 0x82, 0x7D, 0x5C, 0xA1, 0x04, 0x70 });
            var oldPuk = new ReadOnlyMemory<byte>(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38 });
            var newPuk = new ReadOnlyMemory<byte>(new byte[] { 0x70, 0xE4, 0x82, 0x7D, 0x5C, 0xA1, 0x04 });

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                pivSession.ResetApplication();

                bool isValid = pivSession.TryChangePin(oldPin, newPin, out int? retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                isValid = pivSession.TryChangePuk(oldPuk, newPuk, out retriesRemaining);
                Assert.Null(retriesRemaining);
                Assert.True(isValid);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                bool isValid = pivSession.TryVerifyPin(newPin, out int? retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                isValid = pivSession.TryResetPin(newPuk, oldPin, out retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                bool isValid = pivSession.TryVerifyPin(oldPin, out int? retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                isValid = pivSession.TryChangePin(oldPin, newPin, out retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                isValid = pivSession.TryVerifyPin(newPin, out retriesRemaining);
                Assert.Null(retriesRemaining);
                Assert.True(isValid);
            }

            using (var pivSession = new PivSession(testDevice))
            {
                bool isValid = pivSession.TryChangePinAndPukRetryCounts(mgmtKey, newPin, 7, 8, out int? retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                isValid = pivSession.TryVerifyPin(oldPin, out retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                isValid = pivSession.TryResetPin(oldPuk, newPin, out retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);

                isValid = pivSession.TryChangePin(newPin, oldPin, out retriesRemaining);
                Assert.True(isValid);
                Assert.Null(retriesRemaining);
            }
        }
    }
}
