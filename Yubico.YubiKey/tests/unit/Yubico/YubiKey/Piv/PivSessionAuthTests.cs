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
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class PivSessionAuthTests
    {
        [Fact]
        public void PivSession_NullYubiKey_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => new PivSession(null));
#pragma warning restore CS8625 // Specifically testing null input.
        }

        [Fact]
        public void TryAuthenticateManagementKey_NullKeyCollector_ThrowsInvalidOperationException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.TryAuthenticateManagementKey());
            }
        }

        [Fact]
        public void AuthenticateManagementKey_NullKeyCollector_ThrowsInvalidOperationException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.AuthenticateManagementKey());
            }
        }

        [Fact]
        public void TryAuthenticateManagementKey_KeyCollectorFalse_ReturnsFalse()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                bool isValid = pivSession.TryAuthenticateManagementKey();

                Assert.False(isValid);
            }
        }

        [Fact]
        public void AuthenticateManagementKey_KeyCollectorFalse_ThrowsOperationCanceledException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                _ = Assert.Throws<OperationCanceledException>(() => pivSession.AuthenticateManagementKey());
            }
        }

        [Fact]
        public void TryChangeManagementKey_NullKeyCollector_ThrowsInvalidOperationException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.TryChangeManagementKey());
            }
        }

        [Fact]
        public void ChangeManagementKey_NullKeyCollector_ThrowsInvalidOperationException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.ChangeManagementKey());
            }
        }

        [Fact]
        public void TryChangeManagementKey_KeyCollectorFalse_ReturnsFalse()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                bool isValid = pivSession.TryChangeManagementKey();

                Assert.False(isValid);
            }
        }

        [Fact]
        public void ChangeManagementKey_KeyCollectorFalse_ThrowsOperationCanceledException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                _ = Assert.Throws<OperationCanceledException>(() => pivSession.ChangeManagementKey());
            }
        }

        [Fact]
        public void TryVerifyPin_NullKeyCollector_ThrowsInvalidOperationException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.TryVerifyPin());
            }
        }

        [Fact]
        public void VerifyPin_NullKeyCollector_ThrowsInvalidOperationException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.VerifyPin());
            }
        }

        [Fact]
        public void TryVerifyPin_KeyCollectorFalse_ReturnsFalse()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                bool isValid = pivSession.TryVerifyPin();

                Assert.False(isValid);
            }
        }

        [Fact]
        public void VerifyPin_KeyCollectorFalse_ThrowsOperationCanceledException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                _ = Assert.Throws<OperationCanceledException>(() => pivSession.VerifyPin());
            }
        }

        [Fact]
        public void TryChangePin_NullKeyCollector_ThrowsInvalidOperationException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.TryChangePin());
            }
        }

        [Fact]
        public void ChangePin_NullKeyCollector_ThrowsInvalidOperationException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.ChangePin());
            }
        }

        [Fact]
        public void TryChangePin_KeyCollectorFalse_ReturnsFalse()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                bool isValid = pivSession.TryChangePin();

                Assert.False(isValid);
            }
        }

        [Fact]
        public void ChangePin_KeyCollectorFalse_ThrowsOperationCanceledException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                _ = Assert.Throws<OperationCanceledException>(() => pivSession.ChangePin());
            }
        }

        [Fact]
        public void TryResetPin_NullKeyCollector_ThrowsInvalidOperationException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.TryResetPin());
            }
        }

        [Fact]
        public void ResetPin_NullKeyCollector_ThrowsInvalidOperationException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.ResetPin());
            }
        }

        [Fact]
        public void TryResetPin_KeyCollectorFalse_ReturnsFalse()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                bool isValid = pivSession.TryResetPin();

                Assert.False(isValid);
            }
        }

        [Fact]
        public void ResetPin_KeyCollectorFalse_ThrowsOperationCanceledException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                _ = Assert.Throws<OperationCanceledException>(() => pivSession.ResetPin());
            }
        }

        [Fact]
        public void TryChangePuk_NullKeyCollector_Throws()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.TryChangePuk());
            }
        }

        [Fact]
        public void ChangePuk_NullKeyCollector_ThrowsInvalidOperationException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.ChangePuk());
            }
        }

        [Fact]
        public void TryChangePuk_KeyCollectorFalse_ReturnsFalse()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                bool isValid = pivSession.TryChangePuk();

                Assert.False(isValid);
            }
        }

        [Fact]
        public void ChangePuk_KeyCollectorFalse_ThrowsOperationCanceledException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                _ = Assert.Throws<OperationCanceledException>(() => pivSession.ChangePuk());
            }
        }

        [Theory]
        [InlineData(0, 5)]
        [InlineData(6, 0)]
        [InlineData(0, 0)]
        public void RetryCount_Zero_ThrowsArgumentException(byte pinRetry, byte pukRetry)
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<ArgumentException>(() => pivSession.ChangePinAndPukRetryCounts(pinRetry, pukRetry));
            }
        }

        public static bool ReturnFalseKeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            return false;
        }
    }
}
