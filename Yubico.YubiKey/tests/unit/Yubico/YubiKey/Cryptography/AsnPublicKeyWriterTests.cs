using System;
using System.Security.Cryptography;
using Xunit;

namespace Yubico.YubiKey.Cryptography;

public class AsnPublicKeyWriterTests
{
    [Theory]
    [InlineData(KeyDefinitions.KeyType.RSA1024)]
    [InlineData(KeyDefinitions.KeyType.RSA2048)]
    [InlineData(KeyDefinitions.KeyType.RSA3072)]
    [InlineData(KeyDefinitions.KeyType.RSA4096)]
    public void RsaKeyRoundtrip_With_ModulusAndExponent_ShouldMatchOriginal(KeyDefinitions.KeyType keyType)
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
        var parameters = rsa.ExportParameters(false);
        
        // Act
        var encoded = AsnPublicKeyWriter.EncodeToSpki(parameters.Modulus, parameters.Exponent);
        var decodedParams = AsnPublicKeyReader.DecodeFromSpki(encoded);
        
        // Assert
        Assert.IsType<RSAPublicKeyParameters>(decodedParams);
        var rsaParams = (RSAPublicKeyParameters)decodedParams;
        
        Assert.Equal(parameters.Modulus, rsaParams.Parameters.Modulus);
        Assert.Equal(parameters.Exponent, rsaParams.Parameters.Exponent);
    }
    
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
        
        var parameters = rsa.ExportParameters(false);
        
        // Act
        var encoded = AsnPublicKeyWriter.EncodeToSpki(parameters);
        var decodedParams = AsnPublicKeyReader.DecodeFromSpki(encoded);
        
        // Assert
        Assert.IsType<RSAPublicKeyParameters>(decodedParams);
        var rsaParams = (RSAPublicKeyParameters)decodedParams;
        
        Assert.Equal(parameters.Modulus, rsaParams.Parameters.Modulus);
        Assert.Equal(parameters.Exponent, rsaParams.Parameters.Exponent);
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
        
        var parameters = ecdsa.ExportParameters(false);
        
        // Act
        var encoded = AsnPublicKeyWriter.EncodeToSpki(parameters);
        var decodedParams = AsnPublicKeyReader.DecodeFromSpki(encoded);
        
        // Assert
        Assert.IsType<ECPublicKeyParameters>(decodedParams);
        var ecParams = (ECPublicKeyParameters)decodedParams;
        
        // The curve OID should be preserved in the round trip
        Assert.Equal(parameters.Curve.Oid.Value, ecParams.Parameters.Curve.Oid.Value);
        
        // Public point coordinates should match
        Assert.Equal(parameters.Q.X, ecParams.Parameters.Q.X);
        Assert.Equal(parameters.Q.Y, ecParams.Parameters.Q.Y);
    }
    
    [Fact]
    public void Ed25519KeyRoundtrip_ShouldMatchOriginal()
    {
        // Create a random 32-byte key for testing
        var publicKey = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(publicKey);
        
        // Act
        var encoded = AsnPublicKeyWriter.EncodeToSpki(publicKey, KeyDefinitions.KeyType.Ed25519);
        var decodedParams = AsnPublicKeyReader.DecodeFromSpki(encoded);
        
        // Assert
        Assert.IsType<EDsaPublicKeyParameters>(decodedParams);
        var edParams = (EDsaPublicKeyParameters)decodedParams;
        
        Assert.Equal(publicKey, edParams.GetPublicPoint().ToArray());
    }
    
    [Fact]
    public void X25519KeyRoundtrip_ShouldMatchOriginal()
    {
        // Create a random 32-byte key for testing
        var publicKey = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(publicKey);
        
        // Act
        var encoded = AsnPublicKeyWriter.EncodeToSpki(publicKey, KeyDefinitions.KeyType.X25519);
        var decodedParams = AsnPublicKeyReader.DecodeFromSpki(encoded);
        
        // Assert
        Assert.IsType<ECX25519PublicKeyParameters>(decodedParams);
        var x25519Params = (ECX25519PublicKeyParameters)decodedParams;
        
        Assert.Equal(publicKey, x25519Params.GetPublicPoint().ToArray());
    }
    
    [Theory]
    [InlineData(31)] // Too short
    [InlineData(33)] // Too long
    public void Curve25519_InvalidKeySizes_ShouldThrow(int keySize)
    {
        // Arrange
        var invalidKey = new byte[keySize];
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => AsnPublicKeyWriter.EncodeToSpki(invalidKey, KeyDefinitions.KeyType.Ed25519));
        Assert.Throws<ArgumentException>(() => AsnPublicKeyWriter.EncodeToSpki(invalidKey, KeyDefinitions.KeyType.Ed25519));
    }
    
    [Fact]
    public void ConvertViaExtensionMethod_ShouldWork()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(false);
        var rsaKeyParams = new RSAPublicKeyParameters(parameters);
        
        // Act - use the extension method
        var encoded = rsaKeyParams.ToEncodedKey();
        var decodedParams = AsnPublicKeyReader.DecodeFromSpki(encoded);
        
        // Assert
        Assert.IsType<RSAPublicKeyParameters>(decodedParams);
        var roundTrippedParams = (RSAPublicKeyParameters)decodedParams;
        
        Assert.Equal(parameters.Modulus, roundTrippedParams.Parameters.Modulus);
        Assert.Equal(parameters.Exponent, roundTrippedParams.Parameters.Exponent);
    }
    
    [Theory]
    [InlineData(KeyDefinitions.KeyType.P256)]
    [InlineData(KeyDefinitions.KeyType.P384)]
    [InlineData(KeyDefinitions.KeyType.P521)]
    public void FromPublicPointAndKeyType_ECKeys_ShouldCreateValidEncoding(KeyDefinitions.KeyType keyType)
    {
        // Arrange
        using var ecdsa = keyType switch
        {
            KeyDefinitions.KeyType.P256 => ECDsa.Create(ECCurve.NamedCurves.nistP256),
            KeyDefinitions.KeyType.P384 => ECDsa.Create(ECCurve.NamedCurves.nistP384),
            KeyDefinitions.KeyType.P521 => ECDsa.Create(ECCurve.NamedCurves.nistP521),
            _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null)
        };
        
        var parameters = ecdsa.ExportParameters(false);
        var keyDef = KeyDefinitions.GetByKeyType(keyType);
        var coordinateLength = keyDef.LengthInBytes;
        
        // Create uncompressed point format: 0x04 + X + Y
        var publicPoint = new byte[1 + coordinateLength + coordinateLength]; // P-256 coordinates are 32 bytes each
        publicPoint[0] = 0x04; // Uncompressed point indicator
        Buffer.BlockCopy(parameters.Q.X!, 0, publicPoint, 1, coordinateLength);
        Buffer.BlockCopy(parameters.Q.Y!, 0, publicPoint, coordinateLength+1, coordinateLength);
        
        // Act
        var encoded = AsnPublicKeyWriter.EncodeToSpki(publicPoint, keyType);
        var decodedParams = AsnPublicKeyReader.DecodeFromSpki(encoded);
        
        // Assert
        Assert.IsType<ECPublicKeyParameters>(decodedParams);
        var ecParams = (ECPublicKeyParameters)decodedParams;
        
        Assert.Equal(parameters.Q.X, ecParams.Parameters.Q.X);
        Assert.Equal(parameters.Q.Y, ecParams.Parameters.Q.Y);
    }
    
    [Fact]
    public void FromPublicPointAndKeyType_Ed25519_ShouldCreateValidEncoding()
    {
        // Arrange - Create a random 32-byte Ed25519 public key
        var publicKey = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(publicKey);
        
        // Act
        var encoded = AsnPublicKeyWriter.EncodeToSpki(publicKey, KeyDefinitions.KeyType.Ed25519);
        var decodedParams = AsnPublicKeyReader.DecodeFromSpki(encoded);
        
        // Assert
        Assert.IsType<EDsaPublicKeyParameters>(decodedParams);
        var edParams = (EDsaPublicKeyParameters)decodedParams;
        
        Assert.Equal(publicKey, edParams.GetPublicPoint().ToArray());
    }
}
