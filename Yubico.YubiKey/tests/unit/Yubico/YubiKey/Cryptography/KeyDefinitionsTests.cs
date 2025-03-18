using System;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Cryptography
{
    public class KeyDefinitionsTests
    {
        [Fact]
        public void GetByKeyType_ValidTypes_ReturnsCorrectDefinitions()
        {
            // Act & Assert
            Assert.Equal(KeyDefinitions.P256, KeyDefinitions.GetByKeyType(KeyDefinitions.KeyType.P256));
            Assert.Equal(KeyDefinitions.RSA2048, KeyDefinitions.GetByKeyType(KeyDefinitions.KeyType.RSA2048));
            Assert.Equal(KeyDefinitions.Ed25519, KeyDefinitions.GetByKeyType(KeyDefinitions.KeyType.Ed25519));
        }

        [Fact]
        public void GetByKeyType_InvalidType_ThrowsException()
        {
            // Arrange
            var invalidType = (KeyDefinitions.KeyType)999;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => KeyDefinitions.GetByKeyType(invalidType));
        }

        [Fact]
        public void GetByCoseCurveType_ValidCurves_ReturnsCorrectDefinitions()
        {
            // Act & Assert
            Assert.Equal(KeyDefinitions.P256, KeyDefinitions.GetByCoseCurve(CoseEcCurve.P256));
            Assert.Equal(KeyDefinitions.Ed25519, KeyDefinitions.GetByCoseCurve(CoseEcCurve.Ed25519));
        }

        [Fact]
        public void GetByCoseCurveType_InvalidCurve_ThrowsException()
        {
            // Arrange
            CoseEcCurve invalidCurve = (CoseEcCurve)999;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => KeyDefinitions.GetByCoseCurve(invalidCurve));
        }

        [Fact]
        public void GetByOid_ValidOids_ReturnsCorrectDefinitions()
        {
            // Act & Assert
            Assert.Equal(KeyDefinitions.P256, KeyDefinitions.GetByOid(KeyDefinitions.KeyOids.Curve.P256, OidType.CurveOid));
            Assert.Equal(KeyDefinitions.Ed25519, KeyDefinitions.GetByOid(KeyDefinitions.KeyOids.Algorithm.Ed25519, OidType.AlgorithmOid));
        }


        [Fact]
        public void GetByOid_P521_ReturnsCorrectDefinitions()
        {
            // Act & Assert
            Assert.Equal(KeyDefinitions.P521, KeyDefinitions.GetByOid(KeyDefinitions.KeyOids.Curve.P521, OidType.CurveOid));
            Assert.Equal(66, KeyDefinitions.GetByOid(KeyDefinitions.KeyOids.Curve.P521, OidType.CurveOid).LengthInBytes);
        }

        [Fact]
        public void GetByOid_RsaOid_ThrowsNotSupportedException()
        {
            // Act & Assert
            Assert.Throws<NotSupportedException>(() => KeyDefinitions.GetByOid(KeyDefinitions.KeyOids.Algorithm.Rsa, OidType.AlgorithmOid));
        }

        [Fact]
        public void GetByOid_InvalidOid_ThrowsNotSupportedException()
        {
            // Act & Assert
            Assert.Throws<NotSupportedException>(() => KeyDefinitions.GetByOid("1.2.3.4.5", OidType.CurveOid));
        }

        [Fact]
        public void GetRsaKeyDefinitions_ReturnsAllRsaDefinitions()
        {
            // Act
            var rsaDefinitions = KeyDefinitions.GetRsaKeyDefinitions();

            // Assert
            Assert.Equal(4, rsaDefinitions.Count);
            Assert.Contains(KeyDefinitions.RSA2048, rsaDefinitions);
            Assert.Contains(KeyDefinitions.RSA3072, rsaDefinitions);
            Assert.Contains(KeyDefinitions.RSA4096, rsaDefinitions);
        }
    }
}
