using System;
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Piv.Converters;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Cryptography;

public class RSAPublicKeyTests
{
    [Fact]
    public void CreateFromPivEncoding_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var testKey = TestKeys.GetTestPublicKey(KeyType.RSA2048);
        var pivPublicKeyEncoded = testKey.AsPublicKey().EncodeAsPiv();

        // Act
        var publicKeyParams = PivKeyDecoder.CreateRSAPublicKey(pivPublicKeyEncoded);
        var resultParameters = publicKeyParams.Parameters;

        // Assert
        var testKeyParameters = testKey.AsRSA().ExportParameters(false);
        Assert.Equal(testKeyParameters.Exponent, resultParameters.Exponent);
        Assert.Equal(testKeyParameters.Modulus, resultParameters.Modulus);
    }

    [Fact]
    public void CreateFromPkcs8_WithValidParameters_CreatesInstance()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(true);
        var publicKeyPkcs = rsa.ExportSubjectPublicKeyInfo();

        // Act
        var publicKey = RSAPublicKey.CreateFromSubjectPublicKeyInfo(publicKeyPkcs);

        // Assert
        Assert.Null(publicKey.Parameters.D);
        Assert.Equal(parameters.Modulus, publicKey.Parameters.Modulus);
        Assert.Equal(parameters.Exponent, publicKey.Parameters.Exponent);
    }

    [Fact]
    public void CreateFromParameters_WithValidPublicParameters_CreatesInstance()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(false);

        // Act
        var publicKeyParams = RSAPublicKey.CreateFromParameters(parameters);

        // Assert
        Assert.Null(publicKeyParams.Parameters.D);
        Assert.NotNull(publicKeyParams.Parameters.Modulus);
        Assert.NotNull(publicKeyParams.Parameters.Exponent);
    }

    [Fact]
    public void CreateFromParameters_WithPrivateKeyData_ThrowsArgumentException()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(true);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => RSAPublicKey.CreateFromParameters(parameters));
    }

    [Fact]
    public void CreateFromParameters_WithRSA_CreatesValidInstance()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(false);

        // Act
        var publicKeyParams = RSAPublicKey.CreateFromParameters(parameters);

        // Assert
        Assert.Null(publicKeyParams.Parameters.D);
        Assert.NotNull(publicKeyParams.Parameters.Modulus);
        Assert.NotNull(publicKeyParams.Parameters.Exponent);
    }

    [Fact]
    public void CreateFromParameters_PerformsDeepCopy()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var originalParams = rsa.ExportParameters(false);
        var publicKeyParams = RSAPublicKey.CreateFromParameters(originalParams);

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
    public void ExportSubjectPublicKeyInfo_ReturnsCorrectFormat()
    {
        // Arrange
        var testPublicKey = TestKeys.GetTestPublicKey(KeyType.RSA2048);
        var parameters = testPublicKey.AsRSA().ExportParameters(false);

        // Act
        var publicKeyParams = RSAPublicKey.CreateFromParameters(parameters);

        // Act
        var subjectPublicKeyInfo = publicKeyParams.ExportSubjectPublicKeyInfo();

        // Assert
        Assert.NotNull(publicKeyParams.Parameters.Modulus);
        Assert.NotNull(publicKeyParams.Parameters.Exponent);
        Assert.True(testPublicKey.GetModulus().SequenceEqual(publicKeyParams.Parameters.Modulus));
        Assert.True(testPublicKey.GetExponent().SequenceEqual(publicKeyParams.Parameters.Exponent));
        Assert.Equal(testPublicKey.EncodedKey, subjectPublicKeyInfo);

        // Verify the bytes can be used to recreate the RSA key
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out _);
        var rsaParams2 = rsa.ExportParameters(false);

        Assert.True(publicKeyParams.Parameters.Modulus.AsSpan().SequenceEqual(rsaParams2.Modulus));
        Assert.True(publicKeyParams.Parameters.Exponent.AsSpan().SequenceEqual(rsaParams2.Exponent));
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(2048)]
    [InlineData(4096)]
    public void CreateFromParameters_WithDifferentKeySizes_CreatesValidInstance(
        int keySize)
    {
        // Arrange
        using var rsa = RSA.Create(keySize);
        var parameters = rsa.ExportParameters(false);

        // Act
        var publicKeyParams = RSAPublicKey.CreateFromParameters(parameters);

        // Assert
        Assert.Equal(keySize, rsa.KeySize);
        Assert.NotNull(publicKeyParams.Parameters.Modulus);
        Assert.NotNull(publicKeyParams.Parameters.Exponent);
        Assert.Equal(keySize / 8, publicKeyParams.Parameters.Modulus.Length);
    }
}
