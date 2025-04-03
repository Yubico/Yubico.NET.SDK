using System;
using System.Linq;
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
            var testKey = TestKeys.GetTestPublicKey(KeyType.RSA2048);
            var pivPublicKey = testKey.AsPivPublicKey();
            var pivPublicKeyEncoded = pivPublicKey.PivEncodedPublicKey;

            // Act
            var publicKeyParams = KeyParametersPivHelper.CreatePublicRsaFromPivEncoding(pivPublicKeyEncoded);
            var resultParameters = publicKeyParams.Parameters;

            // Assert
            var testKeyParameters = testKey.AsRSA().ExportParameters(false); // Todo how is this working? We're not using the private key, aha, they are probably empty
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
            var publicKeyParams = RSAPublicKeyParameters.CreateFromParameters(parameters);

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
            Assert.Throws<ArgumentException>(() => RSAPublicKeyParameters.CreateFromParameters(parameters));
        }

        [Fact]
        public void Constructor_WithRSA_CreatesValidInstance()
        {
            // Arrange
            using var rsa = RSA.Create(2048);
            var parameters = rsa.ExportParameters(false);

            // Act
            var publicKeyParams = RSAPublicKeyParameters.CreateFromParameters(parameters);

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
            var publicKeyParams = RSAPublicKeyParameters.CreateFromParameters(originalParams);

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
            var testPublicKey = TestKeys.GetTestPublicKey(KeyType.RSA2048);
            var parameters = testPublicKey.AsRSA().ExportParameters(false);
            
            // Act
            var publicKeyParams = RSAPublicKeyParameters.CreateFromParameters(parameters);

            // Act
            var subjectPublicKeyInfo = publicKeyParams.ExportSubjectPublicKeyInfo();

            // Assert
            Assert.NotNull(publicKeyParams.Parameters.Modulus);
            Assert.NotNull(publicKeyParams.Parameters.Exponent);
            Assert.True(testPublicKey.GetModulus().SequenceEqual(publicKeyParams.Parameters.Modulus));
            Assert.True(testPublicKey.GetExponent().SequenceEqual(publicKeyParams.Parameters.Exponent));
            Assert.Equal(testPublicKey.EncodedKey, subjectPublicKeyInfo);

            // Verify the bytes can be used to recreate the RSA key
            var rsa2 = RSA.Create();
            rsa2.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out _);
            var rsaParams2 = rsa2.ExportParameters(false);

            Assert.True(publicKeyParams.Parameters.Modulus.AsSpan().SequenceEqual(rsaParams2.Modulus));
            Assert.True(publicKeyParams.Parameters.Exponent.AsSpan().SequenceEqual(rsaParams2.Exponent));
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(2048)]
        [InlineData(4096)]
        public void Constructor_WithDifferentKeySizes_CreatesValidInstance(
            int keySize)
        {
            // Arrange
            using var rsa = RSA.Create(keySize);
            var parameters = rsa.ExportParameters(false);

            // Act
            var publicKeyParams = RSAPublicKeyParameters.CreateFromParameters(parameters);

            // Assert
            Assert.Equal(keySize, rsa.KeySize);
            Assert.NotNull(publicKeyParams.Parameters.Modulus);
            Assert.NotNull(publicKeyParams.Parameters.Exponent);
            Assert.Equal(keySize / 8, publicKeyParams.Parameters.Modulus.Length);
        }
    }
}
