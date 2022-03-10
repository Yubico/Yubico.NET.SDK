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
using Yubico.YubiKey.TestUtilities;
using Yubico.YubiKey.Piv.Objects;
using Xunit;

namespace Yubico.YubiKey.Piv
{
    public class PinNoCollectorTests
    {
        [Fact]
        public void VerifyPin_Sign_Succeeds()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                var pin = new ReadOnlyMemory<byte>(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 });

                pivSession.ResetApplication();
                bool isValid = pivSession.TryVerifyPin(pin);
                Assert.True(isValid);

                PinProtectedData pinProtect = pivSession.ReadObject<PinProtectedData>();

                Assert.True(pivSession.PinVerified);
                Assert.True(pinProtect.IsEmpty);
            }
        }

        [Fact]
        public void VerifyPin_WrongPin_ReturnsFalse()
        {
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                var pin = new ReadOnlyMemory<byte>(new byte[] { 0x41, 0x32, 0x33, 0x34, 0x35, 0x36 });

                pivSession.ResetApplication();
                bool isValid = pivSession.TryVerifyPin(pin);
                Assert.False(isValid);
            }
        }

        [Fact]
        public void ChangePin_Succeeds()
        {
            var oldPin = new ReadOnlyMemory<byte>(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 });
            var newPin = new ReadOnlyMemory<byte>(new byte[] { 0x41, 0x42, 0x43, 0x44, 0x45, 0x46 });

            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();
                bool isValid = pivSession.TryVerifyPin(oldPin);
                Assert.True(isValid);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                bool isValid = pivSession.TryChangePin(oldPin, newPin);
                Assert.True(isValid);

                isValid = pivSession.TryVerifyPin(newPin);
                Assert.True(isValid);

                PinProtectedData pinProtect = pivSession.ReadObject<PinProtectedData>();

                Assert.True(pivSession.PinVerified);
                Assert.True(pinProtect.IsEmpty);
            }
        }

        [Fact]
        public void ChangePuk_Succeeds()
        {
            var oldPuk = new ReadOnlyMemory<byte>(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38 });
            var newPuk = new ReadOnlyMemory<byte>(new byte[] { 0x70, 0xE4, 0x82, 0x7D, 0x5C, 0xA1, 0x04 });

            var newPin = new ReadOnlyMemory<byte>(new byte[] { 0xE4, 0x82, 0x7D, 0x5C, 0xA1, 0x04, 0x70 });

            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();
                bool isValid = pivSession.TryChangePuk(oldPuk, newPuk);
                Assert.True(isValid);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                bool isValid = pivSession.TryResetPin(newPuk, newPin);
                Assert.True(isValid);

                isValid = pivSession.TryVerifyPin(newPin);
                Assert.True(isValid);

                PinProtectedData pinProtect = pivSession.ReadObject<PinProtectedData>();

                Assert.True(pivSession.PinVerified);
                Assert.True(pinProtect.IsEmpty);
            }
        }

        [Fact]
        public void ChangeRetryCounts_Succeeds()
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
            IYubiKeyDevice yubiKey = SelectSupport.GetFirstYubiKey(Transport.UsbSmartCard);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();

                bool isValid = pivSession.TryChangePin(oldPin, newPin);
                Assert.True(isValid);

                isValid = pivSession.TryChangePuk(oldPuk, newPuk);
                Assert.True(isValid);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                bool isValid = pivSession.TryVerifyPin(newPin);
                Assert.True(isValid);

                isValid = pivSession.TryResetPin(newPuk, oldPin);
                Assert.True(isValid);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                bool isValid = pivSession.TryVerifyPin(oldPin);
                Assert.True(isValid);

                isValid = pivSession.TryChangePin(oldPin, newPin);
                Assert.True(isValid);

                isValid = pivSession.TryVerifyPin(newPin);
                Assert.True(isValid);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                bool isValid = pivSession.TryChangePinAndPukRetryCounts(mgmtKey, newPin, 7, 8);
                Assert.True(isValid);

                isValid = pivSession.TryVerifyPin(oldPin);
                Assert.True(isValid);

                isValid = pivSession.TryResetPin(oldPuk, newPin);
                Assert.True(isValid);

                isValid = pivSession.TryChangePin(newPin, oldPin);
                Assert.True(isValid);
            }
        }
    }
}
