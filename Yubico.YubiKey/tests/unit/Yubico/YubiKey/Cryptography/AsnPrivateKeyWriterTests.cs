using System;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Cryptography;

public class AsnPrivateKeyWriterTests
{
    [Theory]
    [InlineData(KeyType.RSA1024)]
    [InlineData(KeyType.RSA2048)]
    [InlineData(KeyType.RSA3072)]
    [InlineData(KeyType.RSA4096)]
    public void RsaKeyRoundtrip_With_RSAParameters_ShouldMatchOriginal(
        KeyType keyType)
    {
        // Arrange
        using var rsa = keyType switch
        {
            KeyType.RSA1024 => RSA.Create(1024),
            KeyType.RSA2048 => RSA.Create(2048),
            KeyType.RSA3072 => RSA.Create(3072),
            KeyType.RSA4096 => RSA.Create(4096),
            _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null)
        };

        var parameters = rsa.ExportParameters(includePrivateParameters: true);

        // Act
        var encoded = AsnPrivateKeyWriter.EncodeToPkcs8(parameters);
        var decodedParams = AsnPrivateKeyReader.DecodePkcs8EncodedKey(encoded);

        // Assert
        Assert.IsType<RSAPrivateKeyParameters>(decodedParams);
        var rsaParams = (RSAPrivateKeyParameters)decodedParams;

        // Import the parameters into a new RSA instance for functional verification
        using var roundtrippedRsa = RSA.Create();
        roundtrippedRsa.ImportParameters(rsaParams.Parameters);

        // Verify functional equivalence with a signing/verification test
        byte[] dataToSign = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(dataToSign);
        }

        // Sign with original
        byte[] signature = rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Verify with roundtripped
        bool verified =
            roundtrippedRsa.VerifyData(dataToSign, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        Assert.True(verified, "Roundtripped RSA key should verify signature created with original key");
    }

    [Fact]
    public void RsaKeyRoundtrip_MissingPrivateComponents_ShouldThrow()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var publicOnlyParams = rsa.ExportParameters(includePrivateParameters: false);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => AsnPrivateKeyWriter.EncodeToPkcs8(publicOnlyParams));
    }

    [Theory]
    [InlineData(KeyType.P256)]
    [InlineData(KeyType.P384)]
    [InlineData(KeyType.P521)]
    public void ECDsaKeyRoundtrip_With_ECParameters_ShouldMatchOriginal(
        KeyType keyType)
    {
        // Arrange
        using var ecdsa = keyType switch
        {
            KeyType.P256 => ECDsa.Create(ECCurve.NamedCurves.nistP256),
            KeyType.P384 => ECDsa.Create(ECCurve.NamedCurves.nistP384),
            KeyType.P521 => ECDsa.Create(ECCurve.NamedCurves.nistP521),
            _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null)
        };

        var parameters = ecdsa.ExportParameters(includePrivateParameters: true);

        // Act
        var encoded = AsnPrivateKeyWriter.EncodeToPkcs8(parameters);
        var decodedParams = AsnPrivateKeyReader.DecodePkcs8EncodedKey(encoded);

        // Assert
        Assert.IsType<ECPrivateKeyParameters>(decodedParams);
        var ecParams = (ECPrivateKeyParameters)decodedParams;

        // Import the parameters into a new ECDsa instance for functional verification
        using var roundtrippedEcdsa = ECDsa.Create();
        roundtrippedEcdsa.ImportParameters(ecParams.Parameters);

        // Verify functional equivalence with a signing/verification test
        byte[] dataToSign = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(dataToSign);
        }

        // Sign with original
        byte[] signature = ecdsa.SignData(dataToSign, HashAlgorithmName.SHA256);

        // Verify with roundtripped
        bool verified = roundtrippedEcdsa.VerifyData(dataToSign, signature, HashAlgorithmName.SHA256);
        Assert.True(verified, "Roundtripped ECDsa key should verify signature created with original key");
    }

    [Fact]
    public void ECDsaKeyRoundtrip_MissingPrivateComponent_ShouldThrow()
    {
        // Arrange
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publicOnlyParams = ecdsa.ExportParameters(includePrivateParameters: false);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => AsnPrivateKeyWriter.EncodeToPkcs8(publicOnlyParams));
    }

    [Theory]
    [InlineData(KeyType.P256)]
    [InlineData(KeyType.P384)]
    [InlineData(KeyType.P521)]
    public void FromPrivateKeyAndPublicPoint_ECKeys_ShouldCreateValidEncoding(
        KeyType keyType)
    {
        // Arrange
        using var ecdsa = keyType switch
        {
            KeyType.P256 => ECDsa.Create(ECCurve.NamedCurves.nistP256),
            KeyType.P384 => ECDsa.Create(ECCurve.NamedCurves.nistP384),
            KeyType.P521 => ECDsa.Create(ECCurve.NamedCurves.nistP521),
            _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null)
        };

        var parameters = ecdsa.ExportParameters(includePrivateParameters: true);
        var keyDef = KeyDefinitions.GetByKeyType(keyType);
        var coordinateLength = keyDef.LengthInBytes;

        // Create uncompressed point format: 0x04 + X + Y
        var publicPoint = new byte[1 + (coordinateLength * 2)];
        publicPoint[0] = 0x04; // Uncompressed point indicator
        Buffer.BlockCopy(parameters.Q.X!, 0, publicPoint, 1, coordinateLength);
        Buffer.BlockCopy(parameters.Q.Y!, 0, publicPoint, 1 + coordinateLength, coordinateLength);

        // Act
        var encoded = AsnPrivateKeyWriter.EncodeToPkcs8(parameters.D!, publicPoint, keyType);
        var decodedParams = AsnPrivateKeyReader.DecodePkcs8EncodedKey(encoded);

        // Assert
        Assert.IsType<ECPrivateKeyParameters>(decodedParams);
        var decodedEcSkParams = (ECPrivateKeyParameters)decodedParams;

        //      Import the parameters into a new ECDsa instance for functional verification
        using var roundtrippedEcdsa = ECDsa.Create();
        roundtrippedEcdsa.ImportParameters(decodedEcSkParams.Parameters);

        //      Verify functional equivalence with a signing/verification test
        var dataToSign = GetRandomData(32);

        //      Sign with original
        var signature = ecdsa.SignData(dataToSign, HashAlgorithmName.SHA256);

        //      Verify with roundtripped
        var verified = roundtrippedEcdsa.VerifyData(dataToSign, signature, HashAlgorithmName.SHA256);
        Assert.True(verified, "Roundtripped ECDsa key should verify signature created with original key");
    }

    [Fact]
    public void Ed25519KeyRoundtrip_ShouldMatchOriginal()
    {
        // Arrange
        var testPrivateKey = TestKeys.GetTestPrivateKey(KeyType.X25519);
        var testEncodedKeyData = testPrivateKey.EncodedKey.Length == 32
            ? testPrivateKey.EncodedKey
            : testPrivateKey.EncodedKey.Length > 32
                ? testPrivateKey.EncodedKey[^32..]
                : throw new InvalidOperationException("Test key is too short");

        // Act
        var encoded = AsnPrivateKeyWriter.EncodeToPkcs8(testEncodedKeyData, KeyType.Ed25519);
        var decodedParams = AsnPrivateKeyReader.DecodePkcs8EncodedKey(encoded);

        // Assert
        Assert.IsType<Curve25519PrivateKeyParameters>(decodedParams);
        var edParams = (Curve25519PrivateKeyParameters)decodedParams;

        Assert.Equal(32, edParams.PrivateKey.Length);
        Assert.Equal(testEncodedKeyData, edParams.PrivateKey.ToArray());
    }

    [Fact]
    public void X25519KeyRoundtrip_ShouldMatchOriginal()
    {
        // Arrange
        var testPrivateKey = TestKeys.GetTestPrivateKey(KeyType.X25519);
        var testEncodedKeyData = testPrivateKey.EncodedKey.Length == 32
            ? testPrivateKey.EncodedKey
            : testPrivateKey.EncodedKey.Length > 32
                ? testPrivateKey.EncodedKey[^32..]
                : throw new InvalidOperationException("Test key is too short");

        // Act
        var encoded = AsnPrivateKeyWriter.EncodeToPkcs8(testEncodedKeyData, KeyType.X25519);
        var decodedParams = AsnPrivateKeyReader.DecodePkcs8EncodedKey(encoded);

        // Assert
        Assert.IsType<Curve25519PrivateKeyParameters>(decodedParams);
        var x25519Params = (Curve25519PrivateKeyParameters)decodedParams;

        Assert.Equal(32, x25519Params.PrivateKey.Length);
        Assert.Equal(testEncodedKeyData, x25519Params.PrivateKey.ToArray());
    }

    [Theory]
    [InlineData(31)] // Too short
    [InlineData(33)] // Too long
    public void Ed25519_InvalidKeySizes_ShouldThrow(
        int keySize)
    {
        // Arrange
        var invalidKey = new byte[keySize];

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            AsnPrivateKeyWriter.EncodeToPkcs8(invalidKey, KeyType.Ed25519));
    }

    [Theory]
    [InlineData(31)] // Too short
    [InlineData(33)] // Too long
    public void X25519_InvalidKeySizes_ShouldThrow(
        int keySize)
    {
        // Arrange
        var invalidKey = new byte[keySize];

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            AsnPrivateKeyWriter.EncodeToPkcs8(invalidKey, KeyType.X25519));
    }

    [Theory]
    [InlineData(KeyType.RSA2048)]
    [InlineData(KeyType.P256)]
    public void TestExtensionMethod_WithTestKeys(
        KeyType keyType)
    {
        // Arrange - Get the test private key
        var testKey = TestKeys.GetTestPrivateKey(keyType);

        // Parse with AsnPrivateKeyReader to get the parameters
        var privateKeyParameters = AsnPrivateKeyReader.DecodePkcs8EncodedKey(testKey.EncodedKey);

        // Act - Use the extension method
        var encoded = privateKeyParameters.EncodeToPkcs8();

        // Parse again with AsnPrivateKeyReader
        var decodedParams = AsnPrivateKeyReader.DecodePkcs8EncodedKey(encoded);

        // Assert by verifying key type and capabilities
        Assert.Equal(privateKeyParameters.GetType(), decodedParams.GetType());

        // Use functional verification based on key type
        KeyEquivalenceTestHelper.VerifyFunctionalEquivalence(privateKeyParameters, decodedParams, keyType);
    }

    [Theory]
    [InlineData(KeyType.RSA1024)]
    [InlineData(KeyType.RSA2048)]
    [InlineData(KeyType.RSA3072)]
    [InlineData(KeyType.RSA4096)]
    [InlineData(KeyType.P256)]
    [InlineData(KeyType.P384)]
    [InlineData(KeyType.P521)]
    [InlineData(KeyType.Ed25519)]
    [InlineData(KeyType.X25519)]
    public void Roundtrip_WithTestKeys_VerifyFunctionalEquivalence(
        KeyType keyType)
    {
        // Arrange - Get the test private key
        var testKey = TestKeys.GetTestPrivateKey(keyType);

        // Parse with AsnPrivateKeyReader to get the parameters
        var privateKeyParameters = AsnPrivateKeyReader.DecodePkcs8EncodedKey(testKey.EncodedKey);

        // Re-encode using our writer
        byte[] reencoded;

        switch (privateKeyParameters)
        {
            case RSAPrivateKeyParameters rsaParams:
                reencoded = AsnPrivateKeyWriter.EncodeToPkcs8(rsaParams.Parameters);
                break;
            case ECPrivateKeyParameters ecParams:
                reencoded = AsnPrivateKeyWriter.EncodeToPkcs8(ecParams.Parameters);
                break;
            case Curve25519PrivateKeyParameters edParams when keyType == KeyType.Ed25519:
                reencoded = AsnPrivateKeyWriter.EncodeToPkcs8(edParams.PrivateKey, KeyType.Ed25519);
                break;
            case Curve25519PrivateKeyParameters x25519Params when keyType == KeyType.X25519:
                reencoded = AsnPrivateKeyWriter.EncodeToPkcs8(x25519Params.PrivateKey,
                    KeyType.X25519);
                break;
            case Ed25519PrivateKeyParameters edParams: // Keep?
                reencoded = AsnPrivateKeyWriter.EncodeToPkcs8(edParams.PrivateKey, KeyType.Ed25519);
                break;
            case X25519PrivateKeyParameters x25519Params: // Keep?
                reencoded = AsnPrivateKeyWriter.EncodeToPkcs8(x25519Params.PrivateKey,
                    KeyType.X25519);
                break;
            default:
                throw new NotSupportedException(
                    $"Key type {privateKeyParameters.GetType().Name} not supported in test");
        }

        // Parse again with AsnPrivateKeyReader
        var roundTrippedParams = AsnPrivateKeyReader.DecodePkcs8EncodedKey(reencoded);

        // Assert by verifying key type and capabilities
        Assert.Equal(privateKeyParameters.GetType(), roundTrippedParams.GetType());

        // Use functional verification based on key type
        KeyEquivalenceTestHelper.VerifyFunctionalEquivalence(privateKeyParameters, roundTrippedParams, keyType);
    }


    private static byte[] GetRandomData(
        int length)
    {
        var dataToSign = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(dataToSign);

        return dataToSign;
    }

    public static class KeyEquivalenceTestHelper
    {
        public static void VerifyFunctionalEquivalence(
            IPrivateKeyParameters originalParams,
            IPrivateKeyParameters roundTrippedParams,
            KeyType keyType)
        {
            switch (keyType)
            {
                case KeyType.RSA1024:
                case KeyType.RSA2048:
                case KeyType.RSA3072:
                case KeyType.RSA4096:
                    VerifyRsaFunctionalEquivalence(
                        (RSAPrivateKeyParameters)originalParams,
                        (RSAPrivateKeyParameters)roundTrippedParams);
                    break;

                case KeyType.P256:
                case KeyType.P384:
                case KeyType.P521:
                    VerifyEcFunctionalEquivalence(
                        (ECPrivateKeyParameters)originalParams,
                        (ECPrivateKeyParameters)roundTrippedParams);
                    break;

                // case KeyType.Ed25519: // TODO Keep the specific classes for ED and X or keep the Curve25519?
                //     VerifyEd25519KeyEquivalence(
                //         (EDsaPrivateKeyParameters)originalParams,
                //         (EDsaPrivateKeyParameters)roundTrippedParams);
                //     break;

                // case KeyType.X25519:
                //     VerifyX25519KeyEquivalence(
                //         (ECX25519PrivateKeyParameters)originalParams,
                //         (ECX25519PrivateKeyParameters)roundTrippedParams);
                //     break;

                case KeyType.Ed25519:
                case KeyType.X25519:

                    VerifyEd25519KeyEquivalence(
                        (Curve25519PrivateKeyParameters)originalParams,
                        (Curve25519PrivateKeyParameters)roundTrippedParams);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null);
            }
        }

        private static void VerifyRsaFunctionalEquivalence(
            RSAPrivateKeyParameters originalParams,
            RSAPrivateKeyParameters roundTrippedParams)
        {
            // Create two RSA instances with the parameters
            using var originalRsa = RSA.Create();
            originalRsa.ImportParameters(originalParams.Parameters);

            using var roundTrippedRsa = RSA.Create();
            roundTrippedRsa.ImportParameters(roundTrippedParams.Parameters);

            // Test signing/verification
            byte[] dataToSign = GetRandomData(32);

            // Sign with original
            byte[] signature = originalRsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            // Verify with roundtripped
            bool verified =
                roundTrippedRsa.VerifyData(dataToSign, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            Assert.True(verified, "Roundtripped RSA key should verify signature created with original key");

            // Sign with roundtripped
            byte[] roundTrippedSignature =
                roundTrippedRsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            // Verify with original
            bool verifiedReverse = originalRsa.VerifyData(dataToSign, roundTrippedSignature, HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            Assert.True(verifiedReverse, "Original RSA key should verify signature created with roundtripped key");
        }

        private static void VerifyEcFunctionalEquivalence(
            ECPrivateKeyParameters originalParams,
            ECPrivateKeyParameters roundTrippedParams)
        {
            // Create two ECDsa instances with the parameters
            using var originalEcdsa = ECDsa.Create();
            originalEcdsa.ImportParameters(originalParams.Parameters);

            using var roundTrippedEcdsa = ECDsa.Create();
            roundTrippedEcdsa.ImportParameters(roundTrippedParams.Parameters);

            // Test signing/verification
            byte[] dataToSign = GetRandomData(32);

            // Sign with original
            byte[] signature = originalEcdsa.SignData(dataToSign, HashAlgorithmName.SHA256);

            // Verify with roundtripped
            bool verified = roundTrippedEcdsa.VerifyData(dataToSign, signature, HashAlgorithmName.SHA256);
            Assert.True(verified, "Roundtripped ECDsa key should verify signature created with original key");

            // Sign with roundtripped
            byte[] roundTrippedSignature = roundTrippedEcdsa.SignData(dataToSign, HashAlgorithmName.SHA256);

            // Verify with original
            bool verifiedReverse =
                originalEcdsa.VerifyData(dataToSign, roundTrippedSignature, HashAlgorithmName.SHA256);
            Assert.True(verifiedReverse, "Original ECDsa key should verify signature created with roundtripped key");
        }

        // private static void VerifyEd25519KeyEquivalence(
        //     EDsaPrivateKeyParameters originalParams,
        //     EDsaPrivateKeyParameters roundTrippedParams)
        // {
        //     var originalPrivateKey = originalParams.GetPrivateKey().ToArray();
        //     var roundTrippedPrivateKey = roundTrippedParams.GetPrivateKey().ToArray();
        //
        //     var normalizedOriginal = NormalizeBytes(originalPrivateKey);
        //     var normalizedRoundtripped = NormalizeBytes(roundTrippedPrivateKey);
        //
        //     Assert.Equal(normalizedOriginal, normalizedRoundtripped);
        // }

        private static void VerifyEd25519KeyEquivalence(
            Curve25519PrivateKeyParameters originalParams,
            Curve25519PrivateKeyParameters roundTrippedParams)
        {
            var originalPrivateKey = originalParams.PrivateKey.ToArray();
            var roundTrippedPrivateKey = roundTrippedParams.PrivateKey.ToArray();

            var normalizedOriginal = NormalizeBytes(originalPrivateKey);
            var normalizedRoundtripped = NormalizeBytes(roundTrippedPrivateKey);

            Assert.Equal(normalizedOriginal, normalizedRoundtripped);
        }

        // public static void VerifyX25519KeyEquivalence(
        //     ECX25519PrivateKeyParameters originalParams,
        //     ECX25519PrivateKeyParameters roundTrippedParams)
        // {
        //     var originalPrivateKey = originalParams.GetPrivateKey().ToArray();
        //     var roundTrippedPrivateKey = roundTrippedParams.GetPrivateKey().ToArray();
        //
        //     var normalizedOriginal = NormalizeBytes(originalPrivateKey);
        //     var normalizedRoundtripped = NormalizeBytes(roundTrippedPrivateKey);
        //
        //     Assert.Equal(normalizedOriginal, normalizedRoundtripped);
        // }

        // Helper method for normalized byte comparison for key material
        public static byte[] NormalizeBytes(
            byte[] data)
        {
            // Trim leading zeros
            int startIndex = 0;
            while (startIndex < data.Length && data[startIndex] == 0)
            {
                startIndex++;
            }

            // If all zeros, return single zero byte
            if (startIndex == data.Length)
            {
                return new byte[] { 0 };
            }

            // Create new array without leading zeros
            var result = new byte[data.Length - startIndex];
            Buffer.BlockCopy(data, startIndex, result, 0, result.Length);

            return result;
        }
    }
}
