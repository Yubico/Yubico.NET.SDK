﻿using System;
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Cryptography
{
    public class ECPrivateKeyParametersTests
    {
        
        [Fact]
        public void CreateFromPivEncoding_WithValidParameters_CreatesInstance()
        {
            // Arrange
            var testKey = TestKeys.GetTestPrivateKey(KeyDefinitions.KeyType.P256);
            var pivPrivateKey = testKey.AsPivPrivateKey();
            var pivPrivateKeyEncoded = pivPrivateKey.EncodedPrivateKey;

            // Act
            var privateKeyParams = KeyParametersPivHelper.CreatePrivateParametersFromPivEncoding<ECPrivateKeyParameters>(pivPrivateKeyEncoded);
            var parameters = privateKeyParams.Parameters;

            // Assert
            Assert.Equal(parameters.D!.Length, privateKeyParams.Parameters.D!.Length);
            Assert.Equal(parameters.D, privateKeyParams.Parameters.D);
        }
        
        
        [Fact]
        public void CreateFromPkcs8_WithValidParameters_CreatesInstance()
        {
            // Arrange
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var parameters = ecdsa.ExportParameters(true);

            // Act
            var privateKey = ecdsa.ExportPkcs8PrivateKey();
            var privateKeyParams = ECPrivateKeyParameters.CreateFromPkcs8(privateKey);

            // Assert
            Assert.NotNull(privateKeyParams.Parameters.D);
            Assert.Equal(parameters.D, privateKeyParams.Parameters.D);
            Assert.Equal(parameters.Q.X, privateKeyParams.Parameters.Q.X);
            Assert.Equal(parameters.Q.Y, privateKeyParams.Parameters.Q.Y);
        }

        [Fact]
        public void CreateFromValue_WithValidParameters_CreatesInstance()
        {
            // Arrange
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var parameters = ecdsa.ExportParameters(true);

            // Act
            var privateKeyParams =
                ECPrivateKeyParameters.CreateFromValue(parameters.D!, KeyDefinitions.KeyType.P256);

            // Assert
            Assert.NotNull(privateKeyParams.Parameters.D);
            Assert.Equal(parameters.D, privateKeyParams.Parameters.D);
            Assert.Equal(parameters.Q.X, privateKeyParams.Parameters.Q.X);
            Assert.Equal(parameters.Q.Y, privateKeyParams.Parameters.Q.Y);
        }

        [Fact]
        public void Constructor_WithValidECParameters_CreatesInstance()
        {
            // Arrange
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var parameters = ecdsa.ExportParameters(true);

            // Act
            var privateKeyParams = new ECPrivateKeyParameters(parameters);

            // Assert
            Assert.NotNull(privateKeyParams.Parameters.D);
            Assert.Equal(parameters.D, privateKeyParams.Parameters.D);
            Assert.Equal(parameters.Q.X, privateKeyParams.Parameters.Q.X);
            Assert.Equal(parameters.Q.Y, privateKeyParams.Parameters.Q.Y);
        }

        [Fact]
        public void Constructor_WithECDsaObject_CreatesInstance()
        {
            // Arrange
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            // Act
            var privateKeyParams = new ECPrivateKeyParameters(ecdsa);

            // Assert
            Assert.NotNull(privateKeyParams.Parameters.D);
            Assert.NotNull(privateKeyParams.Parameters.Q.X);
            Assert.NotNull(privateKeyParams.Parameters.Q.Y);
        }

        [Fact]
        public void Constructor_WithNullDValue_ThrowsArgumentException()
        {
            // Arrange
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var parameters = ecdsa.ExportParameters(true);
            parameters.D = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new ECPrivateKeyParameters(parameters));
            Assert.Equal("parameters", exception.ParamName);
            Assert.Contains("D value", exception.Message);
        }

        [Fact]
        public void Constructor_PerformsDeepCopy()
        {
            // Arrange
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var originalParams = ecdsa.ExportParameters(true);
            var privateKeyParams = new ECPrivateKeyParameters(originalParams);

            Assert.NotNull(originalParams.D);
            Assert.NotNull(originalParams.Q.X);
            Assert.NotNull(originalParams.Q.Y);
            Assert.NotNull(privateKeyParams.Parameters.D);
            Assert.NotNull(privateKeyParams.Parameters.Q.X);
            Assert.NotNull(privateKeyParams.Parameters.Q.Y);

            // Act - Modify original parameters
            originalParams.D[0] = (byte)(originalParams.D[0] + 1);
            originalParams.Q.X[0] = (byte)(originalParams.Q.X[0] + 1);
            originalParams.Q.Y[0] = (byte)(originalParams.Q.Y[0] + 1);

            // Assert - Verify that the stored parameters weren't affected
            Assert.NotEqual(originalParams.D[0], privateKeyParams.Parameters.D[0]);
            Assert.NotEqual(originalParams.Q.X[0], privateKeyParams.Parameters.Q.X[0]);
            Assert.NotEqual(originalParams.Q.Y[0], privateKeyParams.Parameters.Q.Y[0]);
        }

        [Fact]
        public void Constructor_WithNullECDsaObject_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ECPrivateKeyParameters(null!));
        }

        [Theory]
        [InlineData("1.2.840.10045.3.1.7")] // NIST P-256
        [InlineData("1.3.132.0.34")] // NIST P-384
        [InlineData("1.3.132.0.35")] // NIST P-512
        public void Constructor_WithDifferentCurves_CreatesValidInstance(
            string oid)
        {
            // Arrange
            using var ecdsa = ECDsa.Create(ECCurve.CreateFromOid(Oid.FromOidValue(oid, OidGroup.PublicKeyAlgorithm)));

            // Act
            var privateKeyParams = new ECPrivateKeyParameters(ecdsa);

            // Assert
            Assert.Equal(oid, privateKeyParams.Parameters.Curve.Oid.Value);
            Assert.NotNull(privateKeyParams.Parameters.D);
        }
    }
}
