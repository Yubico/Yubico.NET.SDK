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

namespace Yubico.YubiKey.Piv
{
    public class PivSessionKeyPairTests
    {
        [Fact]
        public void Generate_BadSlot_ThrowsArgException()
        {
            var yubiKey = new HollowYubiKeyDevice(alwaysAuthenticatePiv: true);

            using (var pivSession = new PivSession(yubiKey))
            {
                var simpleCollector = new SimpleKeyCollector(allowRetry: false);
                pivSession.KeyCollector = simpleCollector.SimpleKeyCollectorDelegate;
                _ = Assert.Throws<ArgumentException>(() =>
                    pivSession.GenerateKeyPair(slotNumber: 0x81, PivAlgorithm.EccP256));
            }
        }

        [Fact]
        public void Generate_BadAlg_ThrowsArgException()
        {
            var yubiKey = new HollowYubiKeyDevice(alwaysAuthenticatePiv: true);

            using (var pivSession = new PivSession(yubiKey))
            {
                var simpleCollector = new SimpleKeyCollector(allowRetry: false);
                pivSession.KeyCollector = simpleCollector.SimpleKeyCollectorDelegate;
                _ = Assert.Throws<ArgumentException>(() =>
                    pivSession.GenerateKeyPair(slotNumber: 0x9A, PivAlgorithm.Pin));
            }
        }

        [Fact]
        public void Generate_NoCollector_ThrowsInvalidOpException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<InvalidOperationException>(() =>
                    pivSession.GenerateKeyPair(slotNumber: 0x9A, PivAlgorithm.EccP256));
            }
        }

        [Fact]
        public void Generate_CollectorFalse_ThrowsCancelException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                _ = Assert.Throws<OperationCanceledException>(() =>
                    pivSession.GenerateKeyPair(slotNumber: 0x9A, PivAlgorithm.EccP256));
            }
        }

        [Fact]
        public void ImportKey_BadSlot_ThrowsArgException()
        {
            var yubiKey = new HollowYubiKeyDevice(alwaysAuthenticatePiv: true);

            using (var pivSession = new PivSession(yubiKey))
            {
                var simpleCollector = new SimpleKeyCollector(allowRetry: false);
                pivSession.KeyCollector = simpleCollector.SimpleKeyCollectorDelegate;
                var privateKey = SampleKeyPairs.GetPivPrivateKey(PivAlgorithm.EccP256);

                _ = Assert.Throws<ArgumentException>(() => pivSession.ImportPrivateKey(slotNumber: 0x81, privateKey));
            }
        }

        [Fact]
        public void ImportKey_NullKey_ThrowsNullArgException()
        {
            var yubiKey = new HollowYubiKeyDevice(alwaysAuthenticatePiv: true);

            using (var pivSession = new PivSession(yubiKey))
            {
                var simpleCollector = new SimpleKeyCollector(allowRetry: false);
                pivSession.KeyCollector = simpleCollector.SimpleKeyCollectorDelegate;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                _ = Assert.Throws<ArgumentNullException>(() =>
                    pivSession.ImportPrivateKey(slotNumber: 0x85, privateKey: null));
#pragma warning restore CS8625 // Testing null input.
            }
        }

        [Fact]
        public void ImportKey_EmptyKey_ThrowslArgException()
        {
            var yubiKey = new HollowYubiKeyDevice(alwaysAuthenticatePiv: true);

            using (var pivSession = new PivSession(yubiKey))
            {
                var simpleCollector = new SimpleKeyCollector(allowRetry: false);
                pivSession.KeyCollector = simpleCollector.SimpleKeyCollectorDelegate;
                var privateKey = new PivPrivateKey();

                _ = Assert.Throws<ArgumentException>(() => pivSession.ImportPrivateKey(slotNumber: 0x85, privateKey));
            }
        }

        [Fact]
        public void ImportKey_NoCollector_ThrowsInvalidOpException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                var privateKey = SampleKeyPairs.GetPivPrivateKey(PivAlgorithm.EccP256);
                _ = Assert.Throws<InvalidOperationException>(() =>
                    pivSession.ImportPrivateKey(slotNumber: 0x85, privateKey));
            }
        }

        [Fact]
        public void ImportKey_CollectorFalse_ThrowsCancelException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                var privateKey = SampleKeyPairs.GetPivPrivateKey(PivAlgorithm.EccP256);
                _ = Assert.Throws<OperationCanceledException>(() =>
                    pivSession.ImportPrivateKey(slotNumber: 0x85, privateKey));
            }
        }

        [Fact]
        public void ImportCert_BadSlot_ThrowsArgException()
        {
            var yubiKey = new HollowYubiKeyDevice(alwaysAuthenticatePiv: true);

            using (var pivSession = new PivSession(yubiKey))
            {
                var simpleCollector = new SimpleKeyCollector(allowRetry: false);
                pivSession.KeyCollector = simpleCollector.SimpleKeyCollectorDelegate;
                var isValid =
                    SampleKeyPairs.GetMatchingKeyAndCert(PivAlgorithm.Rsa2048, out var cert, out _);

                _ = Assert.Throws<ArgumentException>(() => pivSession.ImportCertificate(slotNumber: 0x81, cert));
            }
        }

        [Fact]
        public void ImportCert_NullCert_ThrowsNullArgException()
        {
            var yubiKey = new HollowYubiKeyDevice(alwaysAuthenticatePiv: true);

            using var pivSession = new PivSession(yubiKey);
            var simpleCollector = new SimpleKeyCollector(allowRetry: false);
            pivSession.KeyCollector = simpleCollector.SimpleKeyCollectorDelegate;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() =>
                pivSession.ImportCertificate(slotNumber: 0x85, certificate: null));
#pragma warning restore CS8625 // Testing null input.
        }

        [Fact]
        public void ImportCert_NoCollector_ThrowsInvalidOpException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using var pivSession = new PivSession(yubiKey);
            var isValid = SampleKeyPairs.GetMatchingKeyAndCert(PivAlgorithm.Rsa2048, out var cert, out _);
            Assert.True(isValid);
            _ = Assert.Throws<InvalidOperationException>(() => pivSession.ImportCertificate(slotNumber: 0x85, cert));
        }

        [Fact]
        public void ImportCert_CollectorFalse_ThrowsCancelException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                var isValid =
                    SampleKeyPairs.GetMatchingKeyAndCert(PivAlgorithm.Rsa2048, out var cert, out _);
                Assert.True(isValid);
                _ = Assert.Throws<OperationCanceledException>(
                    () => pivSession.ImportCertificate(slotNumber: 0x85, cert));
            }
        }

        [Fact]
        public void GetCert_BadSlot_ThrowsArgException()
        {
            var yubiKey = new HollowYubiKeyDevice(alwaysAuthenticatePiv: true);

            using var pivSession = new PivSession(yubiKey);
            _ = Assert.Throws<ArgumentException>(() => pivSession.GetCertificate(slotNumber: 0x81));
        }

        private static bool ReturnFalseKeyCollectorDelegate(KeyEntryData _)
        {
            return false;
        }
    }
}
