using System;
using System.Security.Cryptography;
using Xunit;

namespace Yubico.YubiKey.Cryptography
{
    public class ECPublicKeyParametersTests
    {
        [Fact]
        public void Constructor_WithValidPublicParameters_CreatesInstance()
        {
            // Arrange
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var parameters = ecdsa.ExportParameters(false);

            // Act
            var publicKeyParams = new ECPublicKeyParameters(parameters);

            // Assert
            Assert.Null(publicKeyParams.Parameters.D);
            Assert.NotNull(publicKeyParams.Parameters.Q.X);
            Assert.NotNull(publicKeyParams.Parameters.Q.Y);
        }

        [Fact]
        public void Constructor_WithPrivateKeyData_ThrowsArgumentException()
        {
            // Arrange
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var parameters = ecdsa.ExportParameters(true);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new ECPublicKeyParameters(parameters));
            Assert.Equal("parameters", exception.ParamName);
            Assert.Contains("D value", exception.Message);
        }

        [Fact]
        public void Constructor_WithECDsa_CreatesValidInstance()
        {
            // Arrange
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            // Act
            var publicKeyParams = new ECPublicKeyParameters(ecdsa);

            // Assert
            Assert.Null(publicKeyParams.Parameters.D);
            Assert.NotNull(publicKeyParams.Parameters.Q.X);
            Assert.NotNull(publicKeyParams.Parameters.Q.Y);
        }

        [Fact]
        public void Constructor_PerformsDeepCopy()
        {
            // Arrange
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var originalParams = ecdsa.ExportParameters(false);
            var publicKeyParams = new ECPublicKeyParameters(originalParams);

            // Act - Modify original parameters
            Assert.NotNull(originalParams.Q.X);
            Assert.NotNull(originalParams.Q.Y);
            originalParams.Q.X[0] = (byte)(originalParams.Q.X[0] + 1);
            originalParams.Q.Y[0] = (byte)(originalParams.Q.Y[0] + 1);

            // Assert
            Assert.NotNull(publicKeyParams.Parameters.Q.X);
            Assert.NotNull(publicKeyParams.Parameters.Q.Y);
            Assert.NotEqual(originalParams.Q.X[0], publicKeyParams.Parameters.Q.X[0]);
            Assert.NotEqual(originalParams.Q.Y[0], publicKeyParams.Parameters.Q.Y[0]);
        }

        [Fact]
        public void GetBytes_ReturnsCorrectFormat()
        {
            // Arrange
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var publicKeyParams = new ECPublicKeyParameters(ecdsa);

            // Act
            var bytes = publicKeyParams.GetBytes();

            // Assert
            Assert.NotNull(publicKeyParams.Parameters.Q.X);
            Assert.NotNull(publicKeyParams.Parameters.Q.Y);
            Assert.Equal(1 + publicKeyParams.Parameters.Q.X.Length + publicKeyParams.Parameters.Q.Y.Length, bytes.Length);
            Assert.Equal(0x04, bytes.Span[0]); // Check format identifier

            // Verify X and Y coordinates are correctly copied
            var xCoord = bytes.Slice(1, publicKeyParams.Parameters.Q.X.Length);
            var yCoord = bytes.Slice(1 + publicKeyParams.Parameters.Q.X.Length);

            Assert.True(xCoord.Span.SequenceEqual(publicKeyParams.Parameters.Q.X));
            Assert.True(yCoord.Span.SequenceEqual(publicKeyParams.Parameters.Q.Y));
        }

        [Theory]
        [InlineData("1.2.840.10045.3.1.7")] // NIST P-256
        [InlineData("1.3.132.0.34")] // NIST P-384
        [InlineData("1.3.132.0.35")] // NIST P-512
        public void Constructor_WithDifferentCurves_CreatesValidInstance(string oid)
        {
            // Arrange
            using var ecdsa = ECDsa.Create(ECCurve.CreateFromOid(Oid.FromOidValue(oid, OidGroup.PublicKeyAlgorithm)));

            // Act
            var publicKeyParams = new ECPublicKeyParameters(ecdsa);

            // Assert
            Assert.Equal(oid, publicKeyParams.Parameters.Curve.Oid.Value);
            Assert.NotNull(publicKeyParams.Parameters.Q.X);
            Assert.NotNull(publicKeyParams.Parameters.Q.Y);

        }

        [Fact]
        public void Constructor_WithNullECDsa_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ECPublicKeyParameters(null!));
        }
    }
}
