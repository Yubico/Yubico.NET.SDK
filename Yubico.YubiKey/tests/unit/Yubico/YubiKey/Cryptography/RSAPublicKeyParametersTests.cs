using System;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Cryptography
{
    public class RSAPublicKeyParametersTests
    {
        [Fact]
        public void CreateFromPivEncoding_WithValidParameters_CreatesInstance()
        {
            // Arrange
            var testKey = TestKeys.GetTestPublicKey(KeyDefinitions.KeyType.RSA2048);
            var pivPublicKey = testKey.AsPivPublicKey();
            var pivPublicKeyEncoded = pivPublicKey.PivEncodedPublicKey;

            // Act
            var publicKeyParams = KeyParametersPivHelper.CreatePublicParametersFromPivEncoding<RSAPublicKeyParameters>(pivPublicKeyEncoded);
            var resultParameters = publicKeyParams.Parameters;

            // Assert
            var testKeyParameters = testKey.AsRSA().ExportParameters(false);
            Assert.Equal(testKeyParameters.D, resultParameters.D);
            Assert.Equal(testKeyParameters.DP, resultParameters.DP);
            Assert.Equal(testKeyParameters.DQ, resultParameters.DQ);
            Assert.Equal(testKeyParameters.InverseQ, resultParameters.InverseQ);
            Assert.Equal(testKeyParameters.Exponent, resultParameters.Exponent);
            Assert.Equal(testKeyParameters.Modulus, resultParameters.Modulus);
        }
        
        [Fact]
        public void Constructor_WithValidPublicParameters_CreatesInstance()
        {
            // Arrange
            using var rsa = RSA.Create(2048);
            var parameters = rsa.ExportParameters(false);

            // Act
            var publicKeyParams = new RSAPublicKeyParameters(parameters);

            // Assert
            Assert.Null(publicKeyParams.Parameters.D);
            Assert.NotNull(publicKeyParams.Parameters.Modulus);
            Assert.NotNull(publicKeyParams.Parameters.Exponent);
        }

        [Fact]
        public void Constructor_WithPrivateKeyData_ThrowsArgumentException()
        {
            // Arrange
            using var rsa = RSA.Create(2048);
            var parameters = rsa.ExportParameters(true);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new RSAPublicKeyParameters(parameters));
            Assert.Equal("parameters", exception.ParamName);
            Assert.Contains("D value", exception.Message);
        }

        [Fact]
        public void Constructor_WithRSA_CreatesValidInstance()
        {
            // Arrange
            using var rsa = RSA.Create(2048);
            var parameters = rsa.ExportParameters(false);
            
            // Act
            var publicKeyParams = new RSAPublicKeyParameters(parameters);

            // Assert
            Assert.Null(publicKeyParams.Parameters.D);
            Assert.NotNull(publicKeyParams.Parameters.Modulus);
            Assert.NotNull(publicKeyParams.Parameters.Exponent);
        }

        [Fact]
        public void Constructor_PerformsDeepCopy()
        {
            // Arrange
            using var rsa = RSA.Create(2048);
            var originalParams = rsa.ExportParameters(false);
            var publicKeyParams = new RSAPublicKeyParameters(originalParams);

            // Act - Modify original parameters
            Assert.NotNull(originalParams.Modulus);
            Assert.NotNull(originalParams.Exponent);
            originalParams.Modulus[0] = (byte)(originalParams.Modulus[0] + 1);
            originalParams.Exponent[0] = (byte)(originalParams.Exponent[0] + 1);

            // Assert
            Assert.NotNull(publicKeyParams.Parameters.Modulus);
            Assert.NotNull(publicKeyParams.Parameters.Exponent);
            Assert.NotEqual(originalParams.Modulus[0], publicKeyParams.Parameters.Modulus[0]);
            Assert.NotEqual(originalParams.Exponent[0], publicKeyParams.Parameters.Exponent[0]);
        }

        [Fact]
        public void GetBytes_ReturnsCorrectFormat()
        {
            // Arrange
            using var rsa = RSA.Create(2048);
            var parameters = rsa.ExportParameters(false);
            
            // Act
            var publicKeyParams = new RSAPublicKeyParameters(parameters);

            // Act
            var bytes = publicKeyParams.ExportSubjectPublicKeyInfo();

            // Assert
            Assert.NotNull(publicKeyParams.Parameters.Modulus);
            Assert.NotNull(publicKeyParams.Parameters.Exponent);

            // Check that the byte array contains the correct data
            // The format should be a DER-encoded sequence containing the modulus and exponent
            Assert.True(bytes.Length > 0);

            // Verify the bytes can be used to recreate the RSA key
            var rsa2 = RSA.Create();
                rsa2.ImportRSAPublicKey(bytes.Span, out _);
                var rsaParams2 = rsa2.ExportParameters(false);
                
            Assert.True(publicKeyParams.Parameters.Modulus.AsSpan().SequenceEqual(rsaParams2.Modulus));
            Assert.True(publicKeyParams.Parameters.Exponent.AsSpan().SequenceEqual(rsaParams2.Exponent));
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(2048)]
        [InlineData(4096)]
        public void Constructor_WithDifferentKeySizes_CreatesValidInstance(int keySize)
        {
            // Arrange
            using var rsa = RSA.Create(keySize);
            var parameters = rsa.ExportParameters(false);
            
            // Act
            var publicKeyParams = new RSAPublicKeyParameters(parameters);

            // Assert
            Assert.Equal(keySize, rsa.KeySize);
            Assert.NotNull(publicKeyParams.Parameters.Modulus);
            Assert.NotNull(publicKeyParams.Parameters.Exponent);
            Assert.Equal(keySize / 8, publicKeyParams.Parameters.Modulus.Length);
        }
        
        // [Fact]
        // public void Constructor_WithNullRSA_ThrowsArgumentNullException()
        // {
        //     Assert.Throws<ArgumentNullException>(() => new RSAPublicKeyParameters(null!));
        // }
    }
}
