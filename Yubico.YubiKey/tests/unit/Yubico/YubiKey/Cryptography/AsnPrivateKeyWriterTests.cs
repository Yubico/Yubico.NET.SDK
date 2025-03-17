using System;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Cryptography;

public class AsnPrivateKeyWriterTests
{
    [Theory]
    [InlineData(KeyDefinitions.KeyType.RSA1024)]
    [InlineData(KeyDefinitions.KeyType.RSA2048)]
    [InlineData(KeyDefinitions.KeyType.RSA3072)]
    [InlineData(KeyDefinitions.KeyType.RSA4096)]
    public void RsaKeyRoundtrip_With_RSAParameters_ShouldMatchOriginal(KeyDefinitions.KeyType keyType)
    {
        // Arrange
        using var rsa = keyType switch
        {
            KeyDefinitions.KeyType.RSA1024 => RSA.Create(1024),
            KeyDefinitions.KeyType.RSA2048 => RSA.Create(2048),
            KeyDefinitions.KeyType.RSA3072 => RSA.Create(3072),
            KeyDefinitions.KeyType.RSA4096 => RSA.Create(4096),
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
        bool verified = roundtrippedRsa.VerifyData(dataToSign, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
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
    [InlineData(KeyDefinitions.KeyType.P256)]
    [InlineData(KeyDefinitions.KeyType.P384)]
    [InlineData(KeyDefinitions.KeyType.P521)]
    public void ECDsaKeyRoundtrip_With_ECParameters_ShouldMatchOriginal(KeyDefinitions.KeyType keyType)
    {
        // Arrange
        using var ecdsa = keyType switch
        {
            KeyDefinitions.KeyType.P256 => ECDsa.Create(ECCurve.NamedCurves.nistP256),
            KeyDefinitions.KeyType.P384 => ECDsa.Create(ECCurve.NamedCurves.nistP384),
            KeyDefinitions.KeyType.P521 => ECDsa.Create(ECCurve.NamedCurves.nistP521),
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
    [InlineData(KeyDefinitions.KeyType.P256)]
    [InlineData(KeyDefinitions.KeyType.P384)]
    [InlineData(KeyDefinitions.KeyType.P521)]
    public void FromPrivateKeyAndPublicPoint_ECKeys_ShouldCreateValidEncoding(KeyDefinitions.KeyType keyType)
    {
        // Arrange
        using var ecdsa = keyType switch
        {
            KeyDefinitions.KeyType.P256 => ECDsa.Create(ECCurve.NamedCurves.nistP256),
            KeyDefinitions.KeyType.P384 => ECDsa.Create(ECCurve.NamedCurves.nistP384),
            KeyDefinitions.KeyType.P521 => ECDsa.Create(ECCurve.NamedCurves.nistP521),
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
    
    // [Fact]
    // public void Ed25519KeyRoundtrip_ShouldMatchOriginal()
    // {
    //     var privateKey = TestKeys.GetTestPrivateKey(KeyDefinitions.KeyType.Ed25519);
    //     
    //     // Ensure we're using exactly 32 bytes
    //     byte[] keyBytes = privateKey.KeyBytes.Length == 32 
    //         ? privateKey.KeyBytes 
    //         : privateKey.KeyBytes.Length > 32 
    //             ? privateKey.KeyBytes[^32..] 
    //             : throw new InvalidOperationException("Test key is too short");
    //             
    //     // Act
    //     var encoded = AsnPrivateKeyWriter.EncodeToPkcs8(keyBytes, KeyDefinitions.KeyType.Ed25519);
    //     var decodedParams = AsnPrivateKeyReader.DecodePkcs8EncodedKey(encoded);
    //     
    //     // Assert
    //     Assert.IsType<EDsaPrivateKeyParameters>(decodedParams);
    //     var edParams = (EDsaPrivateKeyParameters)decodedParams;
    //     
    //     // Verify private key material is preserved
    //     Assert.Equal(32, edParams.GetPrivateKey().Length);
    //     
    //     // Verify the decoded private key matches our input key, not the normalized original which might be longer
    //     Assert.Equal(keyBytes, edParams.GetPrivateKey().ToArray());
    // }
    [Fact]
    public void Ed25519KeyRoundtrip_ShouldMatchOriginal()
    {
        var privateKey = TestKeys.GetTestPrivateKey(KeyDefinitions.KeyType.Ed25519);
        
        // Ensure we're using exactly 32 bytes
        byte[] keyBytes = privateKey.KeyBytes.Length == 32 
            ? privateKey.KeyBytes 
            : privateKey.KeyBytes.Length > 32 
                ? privateKey.KeyBytes[^32..] 
                : throw new InvalidOperationException("Test key is too short");
                
        // Act
        var encoded = AsnPrivateKeyWriter.EncodeToPkcs8(keyBytes, KeyDefinitions.KeyType.Ed25519);
        var decodedParams = AsnPrivateKeyReader.DecodePkcs8EncodedKey(encoded);
        
        // Assert
        Assert.IsType<Curve25519PrivateKeyParameters>(decodedParams);
        var edParams = (Curve25519PrivateKeyParameters)decodedParams;
        
        // Verify private key material is preserved
        Assert.Equal(32, edParams.GetPrivateKey().Length);
        
        // Verify the decoded private key matches our input key, not the normalized original which might be longer
        Assert.Equal(keyBytes, edParams.GetPrivateKey().ToArray());
    }
    
    // [Fact]
    // public void X25519KeyRoundtrip_ShouldMatchOriginal()
    // {
    //     // Create a random 32-byte key for testing
    //     var privateKey = TestKeys.GetTestPrivateKey(KeyDefinitions.KeyType.X25519); // 32 bytes()
    //     
    //     // Ensure we're using exactly 32 bytes
    //     byte[] keyBytes = privateKey.KeyBytes.Length == 32 
    //         ? privateKey.KeyBytes 
    //         : privateKey.KeyBytes.Length > 32 
    //             ? privateKey.KeyBytes[^32..] 
    //             : throw new InvalidOperationException("Test key is too short");
    //             
    //     // Act
    //     var encoded = AsnPrivateKeyWriter.EncodeToPkcs8(keyBytes, KeyDefinitions.KeyType.X25519);
    //     var decodedParams = AsnPrivateKeyReader.DecodePkcs8EncodedKey(encoded);
    //     
    //     // Assert
    //     Assert.IsType<ECX25519PrivateKeyParameters>(decodedParams);
    //     var x25519Params = (ECX25519PrivateKeyParameters)decodedParams;
    //     
    //     // Verify private key material is preserved
    //     Assert.Equal(32, x25519Params.GetPrivateKey().Length);
    //     
    //     // Verify the decoded private key matches our input key
    //     Assert.Equal(keyBytes, x25519Params.GetPrivateKey().ToArray());
    // }
    
    [Fact]
    public void X25519KeyRoundtrip_ShouldMatchOriginal()
    {
        // Create a random 32-byte key for testing
        var privateKey = TestKeys.GetTestPrivateKey(KeyDefinitions.KeyType.X25519); // 32 bytes()
        
        // Ensure we're using exactly 32 bytes
        byte[] keyBytes = privateKey.KeyBytes.Length == 32 
            ? privateKey.KeyBytes 
            : privateKey.KeyBytes.Length > 32 
                ? privateKey.KeyBytes[^32..] 
                : throw new InvalidOperationException("Test key is too short");
                
        // Act
        var encoded = AsnPrivateKeyWriter.EncodeToPkcs8(keyBytes, KeyDefinitions.KeyType.X25519);
        var decodedParams = AsnPrivateKeyReader.DecodePkcs8EncodedKey(encoded);
        
        // Assert
        Assert.IsType<Curve25519PrivateKeyParameters>(decodedParams);
        var x25519Params = (Curve25519PrivateKeyParameters)decodedParams;
        
        // Verify private key material is preserved
        Assert.Equal(32, x25519Params.GetPrivateKey().Length);
        
        // Verify the decoded private key matches our input key
        Assert.Equal(keyBytes, x25519Params.GetPrivateKey().ToArray());
    }
    
    [Theory]
    [InlineData(31)] // Too short
    [InlineData(33)] // Too long
    public void Ed25519_InvalidKeySizes_ShouldThrow(int keySize)
    {
        // Arrange
        var invalidKey = new byte[keySize];
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            AsnPrivateKeyWriter.EncodeToPkcs8(invalidKey, KeyDefinitions.KeyType.Ed25519));
    }
    
    [Theory]
    [InlineData(31)] // Too short
    [InlineData(33)] // Too long
    public void X25519_InvalidKeySizes_ShouldThrow(int keySize)
    {
        // Arrange
        var invalidKey = new byte[keySize];
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            AsnPrivateKeyWriter.EncodeToPkcs8(invalidKey, KeyDefinitions.KeyType.X25519));
    }
    
    [Theory]
    [InlineData(KeyDefinitions.KeyType.RSA2048)]
    [InlineData(KeyDefinitions.KeyType.P256)]
    public void TestExtensionMethod_WithTestKeys(KeyDefinitions.KeyType keyType)
    {
        // Arrange - Get the test private key
        var testKey = TestKeys.GetTestPrivateKey(keyType);
        
        // Parse with AsnPrivateKeyReader to get the parameters
        var privateKeyParameters = AsnPrivateKeyReader.DecodePkcs8EncodedKey(testKey.KeyBytes);
        
        // Act - Use the extension method
        var encoded = privateKeyParameters.EncodeToPkcs8();
        
        // Parse again with AsnPrivateKeyReader
        var decodedParams = AsnPrivateKeyReader.DecodePkcs8EncodedKey(encoded);
        
        // Assert by verifying key type and capabilities
        Assert.Equal(privateKeyParameters.GetType(), decodedParams.GetType());
        
        // Use functional verification based on key type
        VerifyFunctionalEquivalence(privateKeyParameters, decodedParams, keyType);
    }
    
    // [Theory]
    // [InlineData(KeyDefinitions.KeyType.RSA1024)]
    // [InlineData(KeyDefinitions.KeyType.RSA2048)]
    // [InlineData(KeyDefinitions.KeyType.RSA3072)]
    // [InlineData(KeyDefinitions.KeyType.RSA4096)]
    // [InlineData(KeyDefinitions.KeyType.P256)]
    // [InlineData(KeyDefinitions.KeyType.P384)]
    // [InlineData(KeyDefinitions.KeyType.P521)]
    // [InlineData(KeyDefinitions.KeyType.Ed25519)]
    // [InlineData(KeyDefinitions.KeyType.X25519)]
    // public void Roundtrip_WithTestKeys_VerifyFunctionalEquivalence(KeyDefinitions.KeyType keyType)
    // {
    //     // Arrange - Get the test private key
    //     var testKey = TestKeys.GetTestPrivateKey(keyType);
    //     
    //     // Parse with AsnPrivateKeyReader to get the parameters
    //     var privateKeyParameters = AsnPrivateKeyReader.DecodePkcs8EncodedKey(testKey.KeyBytes);
    //     
    //     // Re-encode using our writer
    //     byte[] reencoded;
    //     
    //     switch (privateKeyParameters)
    //     {
    //         case RSAPrivateKeyParameters rsaParams:
    //             reencoded = AsnPrivateKeyWriter.EncodeToPkcs8(rsaParams.Parameters);
    //             break;
    //         case ECPrivateKeyParameters ecParams:
    //             reencoded = AsnPrivateKeyWriter.EncodeToPkcs8(ecParams.Parameters);
    //             break;
    //         case EDsaPrivateKeyParameters edParams:
    //             reencoded = AsnPrivateKeyWriter.EncodeToPkcs8(edParams.GetPrivateKey(), KeyDefinitions.KeyType.Ed25519);
    //             break;
    //         case ECX25519PrivateKeyParameters x25519Params:
    //             reencoded = AsnPrivateKeyWriter.EncodeToPkcs8(x25519Params.GetPrivateKey(), KeyDefinitions.KeyType.X25519);
    //             break;
    //         default:
    //             throw new NotSupportedException($"Key type {privateKeyParameters.GetType().Name} not supported in test");
    //     }
    //     
    //     // Parse again with AsnPrivateKeyReader
    //     var roundTrippedParams = AsnPrivateKeyReader.DecodePkcs8EncodedKey(reencoded);
    //     
    //     // Assert by verifying key type and capabilities
    //     Assert.Equal(privateKeyParameters.GetType(), roundTrippedParams.GetType());
    //     
    //     // Use functional verification based on key type
    //     VerifyFunctionalEquivalence(privateKeyParameters, roundTrippedParams, keyType);
    // }
    
    [Theory]
    [InlineData(KeyDefinitions.KeyType.RSA1024)]
    [InlineData(KeyDefinitions.KeyType.RSA2048)]
    [InlineData(KeyDefinitions.KeyType.RSA3072)]
    [InlineData(KeyDefinitions.KeyType.RSA4096)]
    [InlineData(KeyDefinitions.KeyType.P256)]
    [InlineData(KeyDefinitions.KeyType.P384)]
    [InlineData(KeyDefinitions.KeyType.P521)]
    [InlineData(KeyDefinitions.KeyType.Ed25519)]
    [InlineData(KeyDefinitions.KeyType.X25519)]
    public void Roundtrip_WithTestKeys_VerifyFunctionalEquivalence(KeyDefinitions.KeyType keyType)
    {
        // Arrange - Get the test private key
        var testKey = TestKeys.GetTestPrivateKey(keyType);
        
        // Parse with AsnPrivateKeyReader to get the parameters
        var privateKeyParameters = AsnPrivateKeyReader.DecodePkcs8EncodedKey(testKey.KeyBytes);
        
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
            case Curve25519PrivateKeyParameters edParams when keyType == KeyDefinitions.KeyType.Ed25519:
                reencoded = AsnPrivateKeyWriter.EncodeToPkcs8(edParams.GetPrivateKey(), KeyDefinitions.KeyType.Ed25519);
                break;
            case Curve25519PrivateKeyParameters x25519Params when keyType == KeyDefinitions.KeyType.X25519:
                reencoded = AsnPrivateKeyWriter.EncodeToPkcs8(x25519Params.GetPrivateKey(), KeyDefinitions.KeyType.X25519);
                break;
            default:
                throw new NotSupportedException($"Key type {privateKeyParameters.GetType().Name} not supported in test");
        }
        
        // Parse again with AsnPrivateKeyReader
        var roundTrippedParams = AsnPrivateKeyReader.DecodePkcs8EncodedKey(reencoded);
        
        // Assert by verifying key type and capabilities
        Assert.Equal(privateKeyParameters.GetType(), roundTrippedParams.GetType());
        
        // Use functional verification based on key type
        VerifyFunctionalEquivalence(privateKeyParameters, roundTrippedParams, keyType);
    }
    
    private void VerifyFunctionalEquivalence(
        IPrivateKeyParameters originalParams,
        IPrivateKeyParameters roundTrippedParams,
        KeyDefinitions.KeyType keyType)
    {
        switch (keyType)
        {
            case KeyDefinitions.KeyType.RSA1024:
            case KeyDefinitions.KeyType.RSA2048:
            case KeyDefinitions.KeyType.RSA3072:
            case KeyDefinitions.KeyType.RSA4096:
                VerifyRsaFunctionalEquivalence(
                    (RSAPrivateKeyParameters)originalParams,
                    (RSAPrivateKeyParameters)roundTrippedParams);
                break;
                
            case KeyDefinitions.KeyType.P256:
            case KeyDefinitions.KeyType.P384:
            case KeyDefinitions.KeyType.P521:
                VerifyEcFunctionalEquivalence(
                    (ECPrivateKeyParameters)originalParams,
                    (ECPrivateKeyParameters)roundTrippedParams);
                break;
                
            case KeyDefinitions.KeyType.Ed25519:
                VerifyEd25519KeyEquivalence(
                    (EDsaPrivateKeyParameters)originalParams,
                    (EDsaPrivateKeyParameters)roundTrippedParams);
                break;
                
            case KeyDefinitions.KeyType.X25519:
                VerifyX25519KeyEquivalence(
                    (ECX25519PrivateKeyParameters)originalParams,
                    (ECX25519PrivateKeyParameters)roundTrippedParams);
                break;
                
            default:
                throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null);
        }
    }
    
    private void VerifyRsaFunctionalEquivalence(
        RSAPrivateKeyParameters originalParams,
        RSAPrivateKeyParameters roundTrippedParams)
    {
        // Create two RSA instances with the parameters
        using var originalRsa = RSA.Create();
        originalRsa.ImportParameters(originalParams.Parameters);
        
        using var roundTrippedRsa = RSA.Create();
        roundTrippedRsa.ImportParameters(roundTrippedParams.Parameters);
        
        // Test signing/verification
        byte[] dataToSign = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(dataToSign);
        }
        
        // Sign with original
        byte[] signature = originalRsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        
        // Verify with roundtripped
        bool verified = roundTrippedRsa.VerifyData(dataToSign, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        Assert.True(verified, "Roundtripped RSA key should verify signature created with original key");
        
        // Sign with roundtripped
        byte[] roundTrippedSignature = roundTrippedRsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        
        // Verify with original
        bool verifiedReverse = originalRsa.VerifyData(dataToSign, roundTrippedSignature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        Assert.True(verifiedReverse, "Original RSA key should verify signature created with roundtripped key");
    }
    
    private void VerifyEcFunctionalEquivalence(
        ECPrivateKeyParameters originalParams,
        ECPrivateKeyParameters roundTrippedParams)
    {
        // Create two ECDsa instances with the parameters
        using var originalEcdsa = ECDsa.Create();
        originalEcdsa.ImportParameters(originalParams.Parameters);
        
        using var roundTrippedEcdsa = ECDsa.Create();
        roundTrippedEcdsa.ImportParameters(roundTrippedParams.Parameters);
        
        // Test signing/verification
        byte[] dataToSign = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(dataToSign);
        }
        
        // Sign with original
        byte[] signature = originalEcdsa.SignData(dataToSign, HashAlgorithmName.SHA256);
        
        // Verify with roundtripped
        bool verified = roundTrippedEcdsa.VerifyData(dataToSign, signature, HashAlgorithmName.SHA256);
        Assert.True(verified, "Roundtripped ECDsa key should verify signature created with original key");
        
        // Sign with roundtripped
        byte[] roundTrippedSignature = roundTrippedEcdsa.SignData(dataToSign, HashAlgorithmName.SHA256);
        
        // Verify with original
        bool verifiedReverse = originalEcdsa.VerifyData(dataToSign, roundTrippedSignature, HashAlgorithmName.SHA256);
        Assert.True(verifiedReverse, "Original ECDsa key should verify signature created with roundtripped key");
    }
    
    private void VerifyEd25519KeyEquivalence(
        EDsaPrivateKeyParameters originalParams,
        EDsaPrivateKeyParameters roundTrippedParams)
    {
        // Since .NET doesn't have built-in Ed25519 support, just compare normalized private keys
        var originalPrivateKey = originalParams.GetPrivateKey().ToArray();
        var roundTrippedPrivateKey = roundTrippedParams.GetPrivateKey().ToArray();
        
        var normalizedOriginal = NormalizeBytes(originalPrivateKey);
        var normalizedRoundtripped = NormalizeBytes(roundTrippedPrivateKey);
        
        Assert.Equal(normalizedOriginal, normalizedRoundtripped);
    }
    
    private void VerifyX25519KeyEquivalence(
        ECX25519PrivateKeyParameters originalParams,
        ECX25519PrivateKeyParameters roundTrippedParams)
    {
        // Since .NET doesn't have built-in X25519 support, just compare normalized private keys
        var originalPrivateKey = originalParams.GetPrivateKey().ToArray();
        var roundTrippedPrivateKey = roundTrippedParams.GetPrivateKey().ToArray();
        
        var normalizedOriginal = NormalizeBytes(originalPrivateKey);
        var normalizedRoundtripped = NormalizeBytes(roundTrippedPrivateKey);
        
        Assert.Equal(normalizedOriginal, normalizedRoundtripped);
    }
    
    // Helper method for normalized byte comparison for key material
    private byte[] NormalizeBytes(byte[] data)
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
        byte[] result = new byte[data.Length - startIndex];
        Buffer.BlockCopy(data, startIndex, result, 0, result.Length);
        return result;
    }
}
