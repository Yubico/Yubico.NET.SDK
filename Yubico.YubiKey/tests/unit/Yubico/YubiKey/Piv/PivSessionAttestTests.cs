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
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class PivSessionAttestationTests
    {
        [Theory]
        [InlineData(PivSlot.Pin)]
        [InlineData(PivSlot.Puk)]
        [InlineData(PivSlot.Management)]
        [InlineData(PivSlot.Attestation)]
        [InlineData(0x96)]
        public void CreateAttest_BadSlot_ThrowsArgException(byte slotNumber)
        {
            var yubiKey = new HollowYubiKeyDevice
            {
                FirmwareVersion =
                {
                    Major = 4,
                    Minor = 3
                },
                AvailableUsbCapabilities = YubiKeyCapabilities.Piv
            };

            using var pivSession = new PivSession(yubiKey);
            _ = Assert.Throws<ArgumentException>(() => pivSession.CreateAttestationStatement(slotNumber));
        }

        [Fact]
        public void CreateAttest_BadVersion_ThrowsNotSupportedException()
        {
            var yubiKey = new HollowYubiKeyDevice
            {
                FirmwareVersion =
                {
                    Major = 4,
                    Minor = 2
                }
            };

            using var pivSession = new PivSession(yubiKey);
            _ = Assert.Throws<NotSupportedException>(() => pivSession.CreateAttestationStatement(0x9A));
        }

        [Fact]
        public void GetAttest_BadVersion_ThrowsNotSupportedException()
        {
            var yubiKey = new HollowYubiKeyDevice
            {
                FirmwareVersion =
                {
                    Major = 4,
                    Minor = 2
                }
            };

            using var pivSession = new PivSession(yubiKey);
            _ = Assert.Throws<NotSupportedException>(() => pivSession.GetAttestationCertificate());
        }

        [Fact]
        public void ReplaceAttest_BadVersion_ThrowsNotSupportedException()
        {
            var yubiKey = new HollowYubiKeyDevice
            {
                FirmwareVersion =
                {
                    Major = 4,
                    Minor = 2
                }
            };

            var privateKey = new PivPrivateKey();
#pragma warning disable SYSLIB0026
            var cert = new X509Certificate2();
#pragma warning restore SYSLIB0026
            using var pivSession = new PivSession(yubiKey);
            _ = Assert.Throws<NotSupportedException>(() => pivSession.ReplaceAttestationKeyAndCertificate(privateKey, cert));
        }

        [Fact]
        public void ReplaceAttest_NullKey_ThrowsException()
        {
            var yubiKey = new HollowYubiKeyDevice
            {
                FirmwareVersion =
                {
                    Major = 4,
                    Minor = 3
                },
                AvailableUsbCapabilities = YubiKeyCapabilities.Piv
            };
            var isValid = SampleKeyPairs.GetMatchingKeyAndCert(PivAlgorithm.Rsa2048, out X509Certificate2? cert, out _);
            Assert.True(isValid);

            using var pivSession = new PivSession(yubiKey);
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => pivSession.ReplaceAttestationKeyAndCertificate(null, cert!));
#pragma warning restore CS8625 // testing a null input.
        }

        [Fact]
        public void ReplaceAttest_NullCert_ThrowsException()
        {
            var yubiKey = new HollowYubiKeyDevice
            {
                FirmwareVersion =
                {
                    Major = 4,
                    Minor = 3
                },
                AvailableUsbCapabilities = YubiKeyCapabilities.Piv
            };
            var isValid = SampleKeyPairs.GetMatchingKeyAndCert(PivAlgorithm.Rsa2048, out _, out PivPrivateKey? privateKey);
            Assert.True(isValid);

            using var pivSession = new PivSession(yubiKey);
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => pivSession.ReplaceAttestationKeyAndCertificate(privateKey!, null));
#pragma warning restore CS8625 // testing a null input.
        }

        [Fact]
        public void ReplaceAttest_Rsa1024_ThrowsException()
        {
            var yubiKey = new HollowYubiKeyDevice(true)
            {
                FirmwareVersion =
                {
                    Major = 4,
                    Minor = 3
                },
                AvailableUsbCapabilities = YubiKeyCapabilities.Piv
            };

            BadAttestationPairs.GetPair(
                BadAttestationPairs.KeyRsa1024CertValid, out var privateKeyPem, out var certPem);

            var priKey = new KeyConverter(privateKeyPem.ToCharArray());
            PivPrivateKey pivPrivateKey = priKey.GetPivPrivateKey();

            var certChars = certPem.ToCharArray();
            var certDer = Convert.FromBase64CharArray(certChars, 27, certChars.Length - 52);
            var certObj = new X509Certificate2(certDer);

            using var pivSession = new PivSession(yubiKey);
            var simpleCollector = new SimpleKeyCollector(false);
            pivSession.KeyCollector = simpleCollector.SimpleKeyCollectorDelegate;

            _ = Assert.Throws<ArgumentException>(() => pivSession.ReplaceAttestationKeyAndCertificate(pivPrivateKey, certObj));
        }

        [Theory]
        [InlineData(BadAttestationPairs.KeyRsa2048CertVersion1)]
        [InlineData(BadAttestationPairs.KeyEccP256CertVersion1)]
        [InlineData(BadAttestationPairs.KeyEccP384CertVersion1)]
        public void ReplaceAttest_Version1Cert_ThrowsException(int whichPair)
        {
            var yubiKey = new HollowYubiKeyDevice(true)
            {
                FirmwareVersion =
                {
                    Major = 4,
                    Minor = 3
                },
                AvailableUsbCapabilities = YubiKeyCapabilities.Piv
            };

            BadAttestationPairs.GetPair(whichPair, out var privateKeyPem, out var certPem);

            var priKey = new KeyConverter(privateKeyPem.ToCharArray());
            PivPrivateKey pivPrivateKey = priKey.GetPivPrivateKey();

            var certChars = certPem.ToCharArray();
            var certDer = Convert.FromBase64CharArray(certChars, 27, certChars.Length - 52);
            var certObj = new X509Certificate2(certDer);

            using var pivSession = new PivSession(yubiKey);
            var simpleCollector = new SimpleKeyCollector(false);
            pivSession.KeyCollector = simpleCollector.SimpleKeyCollectorDelegate;

            _ = Assert.Throws<ArgumentException>(() => pivSession.ReplaceAttestationKeyAndCertificate(pivPrivateKey, certObj));
        }

        [Fact]
        public void ReplaceAttest_BigName_ThrowsException()
        {
            var yubiKey = new HollowYubiKeyDevice(true)
            {
                FirmwareVersion =
                {
                    Major = 4,
                    Minor = 3
                },
                AvailableUsbCapabilities = YubiKeyCapabilities.Piv
            };

            BadAttestationPairs.GetPair(
                BadAttestationPairs.KeyRsa2048CertBigName, out var privateKeyPem, out var certPem);

            var priKey = new KeyConverter(privateKeyPem.ToCharArray());
            PivPrivateKey pivPrivateKey = priKey.GetPivPrivateKey();

            var certChars = certPem.ToCharArray();
            var certDer = Convert.FromBase64CharArray(certChars, 27, certChars.Length - 52);
            var certObj = new X509Certificate2(certDer);

            using var pivSession = new PivSession(yubiKey);
            var simpleCollector = new SimpleKeyCollector(false);
            pivSession.KeyCollector = simpleCollector.SimpleKeyCollectorDelegate;

            _ = Assert.Throws<ArgumentException>(() => pivSession.ReplaceAttestationKeyAndCertificate(pivPrivateKey, certObj));
        }
    }
}
