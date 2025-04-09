using System;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Cryptography;

public class AsnPublicKeyEncoderTests
{
    [Theory]
    [InlineData(KeyType.RSA1024)]
    [InlineData(KeyType.RSA2048)]
    [InlineData(KeyType.RSA3072)]
    [InlineData(KeyType.RSA4096)]
    public void RsaKeyRoundtrip_With_ModulusAndExponent_ShouldMatchOriginal(KeyType keyType)
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
        var parameters = rsa.ExportParameters(false);
        
        // Act
        var encoded = AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(parameters.Modulus, parameters.Exponent);
        var decodedParams = AsnPublicKeyDecoder.CreatePublicKey(encoded);
        
        // Assert
        Assert.IsType<RSAPublicKey>(decodedParams);
        var rsaParams = (RSAPublicKey)decodedParams;
        
        Assert.Equal(parameters.Modulus, rsaParams.Parameters.Modulus);
        Assert.Equal(parameters.Exponent, rsaParams.Parameters.Exponent);
    }
    
    [Theory]
    [InlineData(KeyType.RSA1024)]
    [InlineData(KeyType.RSA2048)]
    [InlineData(KeyType.RSA3072)]
    [InlineData(KeyType.RSA4096)]
    public void RsaKeyRoundtrip_With_RSAParameters_ShouldMatchOriginal(KeyType keyType)
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
        
        var parameters = rsa.ExportParameters(false);
        
        // Act
        var encoded = AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(parameters);
        var decodedParams = AsnPublicKeyDecoder.CreatePublicKey(encoded);
        
        // Assert
        Assert.IsType<RSAPublicKey>(decodedParams);
        var rsaParams = (RSAPublicKey)decodedParams;
        
        Assert.Equal(parameters.Modulus, rsaParams.Parameters.Modulus);
        Assert.Equal(parameters.Exponent, rsaParams.Parameters.Exponent);
    }
    
    [Theory]
    [InlineData(KeyType.ECP256)]
    [InlineData(KeyType.ECP384)]
    [InlineData(KeyType.ECP521)]
    public void ECDsaKeyRoundtrip_With_ECParameters_ShouldMatchOriginal(KeyType keyType)
    {
        // Arrange
        using var ecdsa = keyType switch
        {
            KeyType.ECP256 => ECDsa.Create(ECCurve.NamedCurves.nistP256),
            KeyType.ECP384 => ECDsa.Create(ECCurve.NamedCurves.nistP384),
            KeyType.ECP521 => ECDsa.Create(ECCurve.NamedCurves.nistP521),
            _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null)
        };
        
        var parameters = ecdsa.ExportParameters(false);
        
        // Act
        var encoded = AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(parameters);
        var decodedParams = AsnPublicKeyDecoder.CreatePublicKey(encoded);
        
        // Assert
        Assert.IsType<ECPublicKey>(decodedParams);
        var ecParams = (ECPublicKey)decodedParams;
        
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
        var encoded = AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(publicKey, KeyType.Ed25519);
        var decodedParams = AsnPublicKeyDecoder.CreatePublicKey(encoded);
        
        // Assert
        Assert.IsType<Curve25519PublicKey>(decodedParams);
        var edParams = (Curve25519PublicKey)decodedParams;
        
        Assert.Equal(publicKey, edParams.PublicPoint.ToArray());
    }
    
    [Fact]
    public void X25519KeyRoundtrip_ShouldMatchOriginal()
    {
        // Create a random 32-byte key for testing
        var publicKey = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(publicKey);
        
        // Act
        var encoded = AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(publicKey, KeyType.X25519);
        var decodedParams = AsnPublicKeyDecoder.CreatePublicKey(encoded);
        
        // Assert
        Assert.IsType<Curve25519PublicKey>(decodedParams);
        var x25519Params = (Curve25519PublicKey)decodedParams;
        
        Assert.Equal(publicKey, x25519Params.PublicPoint.ToArray());
    }
    
    [Theory]
    [InlineData(31)] // Too short
    [InlineData(33)] // Too long
    public void Curve25519_InvalidKeySizes_ShouldThrow(int keySize)
    {
        // Arrange
        var invalidKey = new byte[keySize];
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(invalidKey, KeyType.Ed25519));
        Assert.Throws<ArgumentException>(() => AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(invalidKey, KeyType.Ed25519));
    }
    
    [Theory]
    [InlineData(KeyType.ECP256)]
    [InlineData(KeyType.ECP384)]
    [InlineData(KeyType.ECP521)]
    public void FromPublicPointAndKeyType_ECKeys_ShouldCreateValidEncoding(KeyType keyType)
    {
        // Arrange
        using var ecdsa = keyType switch
        {
            KeyType.ECP256 => ECDsa.Create(ECCurve.NamedCurves.nistP256),
            KeyType.ECP384 => ECDsa.Create(ECCurve.NamedCurves.nistP384),
            KeyType.ECP521 => ECDsa.Create(ECCurve.NamedCurves.nistP521),
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
        var encoded = AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(publicPoint, keyType);
        var decodedParams = AsnPublicKeyDecoder.CreatePublicKey(encoded);
        
        // Assert
        Assert.IsType<ECPublicKey>(decodedParams);
        var ecParams = (ECPublicKey)decodedParams;
        
        Assert.Equal(parameters.Q.X, ecParams.Parameters.Q.X);
        Assert.Equal(parameters.Q.Y, ecParams.Parameters.Q.Y);
    }
    
    [Theory]
    [InlineData(KeyType.ECP256)]
    [InlineData(KeyType.ECP384)]
    [InlineData(KeyType.ECP521)]
    public void FromECParameters_ShouldCreateValidEncoding(KeyType keyType)
    {
        // Arrange
        using var ecdsa = keyType switch
        {
            KeyType.ECP256 => ECDsa.Create(ECCurve.NamedCurves.nistP256),
            KeyType.ECP384 => ECDsa.Create(ECCurve.NamedCurves.nistP384),
            KeyType.ECP521 => ECDsa.Create(ECCurve.NamedCurves.nistP521),
            _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null)
        };
        
        var parameters = ecdsa.ExportParameters(false);
        
        // Act
        var encoded = AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(parameters);
        var decodedParams = AsnPublicKeyDecoder.CreatePublicKey(encoded);
        
        // Assert
        Assert.IsType<ECPublicKey>(decodedParams);
        var ecParams = (ECPublicKey)decodedParams;
        
        Assert.Equal(parameters.Q.X, ecParams.Parameters.Q.X);
        Assert.Equal(parameters.Q.Y, ecParams.Parameters.Q.Y);
    }
    
    [Theory]
    [InlineData(KeyType.ECP256)]
    [InlineData(KeyType.ECP384)]
    [InlineData(KeyType.ECP521)]
    public void FromECParameters_WithTestKeys_ShouldCreateValidEncoding(KeyType keyType)
    {
        // Arrange
        var testPublicKey = TestKeys.GetTestPublicKey(keyType);
        var testECDsa = testPublicKey.AsECDsa();
        var testEcParameters = testECDsa.ExportParameters(false);
        
        // Act
        var encoded = AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(testEcParameters);
        var decodedParams = AsnPublicKeyDecoder.CreatePublicKey(encoded);
        
        // Assert
        Assert.IsType<ECPublicKey>(decodedParams);
        var ecParams = (ECPublicKey)decodedParams;
        
        Assert.Equal(testEcParameters.Q.X, ecParams.Parameters.Q.X);
        Assert.Equal(testEcParameters.Q.Y, ecParams.Parameters.Q.Y);
        
        Assert.Equal(testPublicKey.EncodedKey, encoded);
        Assert.Equal(testPublicKey.EncodedKey, testECDsa.ExportSubjectPublicKeyInfo());
    }
    
    [Fact]
    public void FromPublicPointAndKeyType_Ed25519_ShouldCreateValidEncoding()
    {
        // Arrange - Create a random 32-byte Ed25519 public key
        var publicKey = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(publicKey);
        
        // Act
        var encoded = AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(publicKey, KeyType.Ed25519);
        var decodedParams = AsnPublicKeyDecoder.CreatePublicKey(encoded);
        
        // Assert
        Assert.IsType<Curve25519PublicKey>(decodedParams);
        var edParams = (Curve25519PublicKey)decodedParams;
        
        Assert.Equal(publicKey, edParams.PublicPoint.ToArray());
    }
}
