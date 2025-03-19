using System;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Cryptography
{
    public class ECPublicKeyParametersTests
    {
        
        [Theory]
        [InlineData(KeyType.P256)]
        [InlineData(KeyType.P384)]
        [InlineData(KeyType.P521)]
        public void CreateFromPivEncoding_WithValidParameters_CreatesInstance(KeyType keyType)
        {
            // Arrange
            var testKey = TestKeys.GetTestPublicKey(keyType);
            var pivPublicKey = testKey.AsPivPublicKey();
            var pivPublicKeyEncoded = pivPublicKey.PivEncodedPublicKey;

            // Act
            var publicKeyParams = KeyParametersPivHelper.CreatePublicParametersFromPivEncoding<ECPublicKeyParameters>(pivPublicKeyEncoded);
            var resultParameters = publicKeyParams.Parameters;

            // Assert
            var testKeyParameters = testKey.AsECDsa().ExportParameters(false);
            Assert.Equal(testKeyParameters.D, resultParameters.D);
            Assert.Equal(testKeyParameters.Curve.Oid.Value, resultParameters.Curve.Oid.Value);
            Assert.Equal(testKeyParameters.Q.X, resultParameters.Q.X);
            Assert.Equal(testKeyParameters.Q.Y, resultParameters.Q.Y);
        }
        
        
        // [Fact]
        // public void CreateFromPkcs8_WithValidParameters_CreatesInstance()
        // {
        //     // Arrange
        //     using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        //     var parameters = ecdsa.ExportParameters(true);
        //
        //     // Act
        //     var publicKey = ecdsa.ExportSubjectPublicKeyInfo();
        //     var publicKeyParams = ECPublicKeyParameters.CreateFromPkcs8(publicKey);
        //
        //     // Assert
        //     Assert.NotNull(publicKeyParams.Parameters.D);
        //     Assert.Equal(parameters.D, publicKeyParams.Parameters.D);
        //     Assert.Equal(parameters.Q.X, publicKeyParams.Parameters.Q.X);
        //     Assert.Equal(parameters.Q.Y, publicKeyParams.Parameters.Q.Y);
        // }
        //
        // [Fact]
        // public void CreateFromValue_WithValidParameters_CreatesInstance()
        // {
        //     // Arrange
        //     using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        //     var parameters = ecdsa.ExportParameters(true);
        //
        //     // Act
        //     var publicKeyParams =
        //         ECPublicKeyParameters.CreateFromValue(parameters.D!, KeyType.P256);
        //
        //     // Assert
        //     Assert.NotNull(publicKeyParams.Parameters.D);
        //     Assert.Equal(parameters.D, publicKeyParams.Parameters.D);
        //     Assert.Equal(parameters.Q.X, publicKeyParams.Parameters.Q.X);
        //     Assert.Equal(parameters.Q.Y, publicKeyParams.Parameters.Q.Y);
        // }
        
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
        public void Constructor_WithPublicKeyData_ThrowsArgumentException()
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
        public void PublicPoint_ReturnsCorrectFormat()
        {
            // Arrange
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var publicKeyParams = new ECPublicKeyParameters(ecdsa);

            // Act
            var publicPoint = publicKeyParams.PublicPoint;

            // Assert
            Assert.NotNull(publicKeyParams.Parameters.Q.X);
            Assert.NotNull(publicKeyParams.Parameters.Q.Y);
            Assert.Equal(1 + publicKeyParams.Parameters.Q.X.Length + publicKeyParams.Parameters.Q.Y.Length, publicPoint.Length);
            Assert.Equal(0x04, publicPoint.Span[0]); // Check format identifier

            // Verify X and Y coordinates are correctly copied
            var xCoord = publicPoint.Slice(1, publicKeyParams.Parameters.Q.X.Length);
            var yCoord = publicPoint.Slice(1 + publicKeyParams.Parameters.Q.X.Length);

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
