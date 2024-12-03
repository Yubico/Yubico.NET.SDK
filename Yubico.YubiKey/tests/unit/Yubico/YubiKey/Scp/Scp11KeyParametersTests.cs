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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Scp
{
    public class Scp11KeyParametersTests : IDisposable
    {
        private readonly ECCurve _curve = ECCurve.NamedCurves.nistP256;
        private readonly ECParameters _sdParams;
        private readonly ECParameters _oceParams;
        private readonly X509Certificate2[] _certificates;

        public Scp11KeyParametersTests()
        {
            using var sdKey = ECDsa.Create(_curve);
            _sdParams = sdKey.ExportParameters(false);

            using var oceKey = ECDsa.Create(_curve);
            _oceParams = oceKey.ExportParameters(true);

            // Create a self-signed cert for testing
            var req = new CertificateRequest(
                "CN=Test",
                oceKey,
                HashAlgorithmName.SHA256);

            _certificates = new[]
            {
                req.CreateSelfSigned(
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow.AddYears(1))
            };
        }

        [Fact]
        public void Constructor_Scp11b_ValidParameters_Succeeds()
        {
            // Arrange
            var keyRef = new KeyReference(ScpKeyIds.Scp11B, 0x01);
            var pkSdEcka = new ECPublicKeyParameters(_sdParams);

            // Act
            var keyParams = new Scp11KeyParameters(keyRef, pkSdEcka);

            // Assert
            Assert.NotNull(keyParams);
            Assert.Equal(ScpKeyIds.Scp11B, keyParams.KeyReference.Id);
            Assert.Equal(0x01, keyParams.KeyReference.VersionNumber);
            Assert.NotNull(keyParams.PkSdEcka);
            Assert.Null(keyParams.OceKeyReference);
            Assert.Null(keyParams.SkOceEcka);
            Assert.Null(keyParams.OceCertificates);
        }

        [Theory]
        [InlineData(ScpKeyIds.Scp11A)]
        [InlineData(ScpKeyIds.Scp11C)]
        public void Constructor_Scp11ac_ValidParameters_Succeeds(byte keyId)
        {
            // Arrange
            var keyRef = new KeyReference(keyId, 0x01);
            var oceKeyRef = new KeyReference(0x01, 0x01);
            var pkSdEcka = new ECPublicKeyParameters(_sdParams);
            var skOceEcka = new ECPrivateKeyParameters(_oceParams);

            // Act
            var keyParams = new Scp11KeyParameters(
                keyRef,
                pkSdEcka,
                oceKeyRef,
                skOceEcka,
                _certificates);

            // Assert
            Assert.NotNull(keyParams);
            Assert.Equal(keyId, keyParams.KeyReference.Id);
            Assert.Equal(0x01, keyParams.KeyReference.VersionNumber);
            Assert.NotNull(keyParams.PkSdEcka);
            Assert.NotNull(keyParams.OceKeyReference);
            Assert.NotNull(keyParams.SkOceEcka);
            Assert.NotNull(keyParams.OceCertificates);
            Assert.Single(keyParams.OceCertificates);
        }

        [Fact]
        public void Constructor_Scp11b_WithOptionalParams_ThrowsArgumentException()
        {
            // Arrange
            var keyRef = new KeyReference(ScpKeyIds.Scp11B, 0x01);
            var oceKeyRef = new KeyReference(0x01, 0x01);
            var pkSdEcka = new ECPublicKeyParameters(_sdParams);
            var skOceEcka = new ECPrivateKeyParameters(_oceParams);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Scp11KeyParameters(
                keyRef,
                pkSdEcka,
                oceKeyRef,
                skOceEcka,
                _certificates));
        }

        [Theory]
        [InlineData(ScpKeyIds.Scp11A)]
        [InlineData(ScpKeyIds.Scp11C)]
        public void Constructor_Scp11ac_MissingParams_ThrowsArgumentException(byte keyId)
        {
            // Arrange
            var keyRef = new KeyReference(keyId, 0x01);
            var pkSdEcka = new ECPublicKeyParameters(_sdParams);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Scp11KeyParameters(
                keyRef,
                pkSdEcka));
        }

        [Theory]
        [InlineData(0x00)]  // Invalid key ID
        [InlineData(0x10)]  // Invalid key ID
        [InlineData(0xFF)]  // Invalid key ID
        public void Constructor_InvalidKeyId_ThrowsArgumentException(byte keyId)
        {
            // Arrange
            var keyRef = new KeyReference(keyId, 0x01);
            var pkSdEcka = new ECPublicKeyParameters(_sdParams);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Scp11KeyParameters(
                keyRef,
                pkSdEcka));
        }

        [Fact]
        public void Dispose_ClearsPrivateKey()
        {
            // Arrange
            var keyRef = new KeyReference(ScpKeyIds.Scp11A, 0x01);
            var oceKeyRef = new KeyReference(0x01, 0x01);
            var pkSdEcka = new ECPublicKeyParameters(_sdParams);
            var skOceEcka = new ECPrivateKeyParameters(_oceParams);

            Scp11KeyParameters keyParams = new Scp11KeyParameters(
                keyRef,
                pkSdEcka,
                oceKeyRef,
                skOceEcka,
                _certificates);

            // Act
            keyParams.Dispose();

            // Assert
            Assert.Null(keyParams.SkOceEcka);
            Assert.Null(keyParams.OceKeyReference);
        }

        [Fact]
        public void Dispose_MultipleCalls_DoesNotThrow()
        {
            // Arrange
            var keyRef = new KeyReference(ScpKeyIds.Scp11A, 0x01);
            var oceKeyRef = new KeyReference(0x01, 0x01);
            var pkSdEcka = new ECPublicKeyParameters(_sdParams);
            var skOceEcka = new ECPrivateKeyParameters(_oceParams);

            var keyParams = new Scp11KeyParameters(
                keyRef,
                pkSdEcka,
                oceKeyRef,
                skOceEcka,
                _certificates);

            // Act & Assert - Should not throw
            keyParams.Dispose();
            keyParams.Dispose();
        }

        public void Dispose()
        {
            foreach (var cert in _certificates)
            {
                cert.Dispose();
            }
        }
    }
}
