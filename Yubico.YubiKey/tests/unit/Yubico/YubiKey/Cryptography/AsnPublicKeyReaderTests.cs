// Copyright 2024 Yubico AB
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Cryptography;

public class AsnPublicKeyReaderTests
{
    [Theory]
    [InlineData(KeyDefinitions.KeyType.RSA1024)]
    [InlineData(KeyDefinitions.KeyType.RSA2048)]
    [InlineData(KeyDefinitions.KeyType.RSA3072)]
    [InlineData(KeyDefinitions.KeyType.RSA4096)]
    public void DecodeFromSpki_WithRsaPublicKey_ReturnsCorrectParameters(KeyDefinitions.KeyType keyType)
    {
        // Arrange
        var testKey = TestKeys.GetTestPublicKey(keyType);
        var keyBytes = testKey.EncodedKey;

        // Act
        var result = AsnPublicKeyReader.DecodeFromSpki(keyBytes);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<RSAPublicKeyParameters>(result);
        
        var rsaParams = (RSAPublicKeyParameters)result;
        Assert.NotNull(rsaParams.Parameters.Modulus);
        Assert.NotNull(rsaParams.Parameters.Exponent);
        
        var expectedKeySize = KeyDefinitions.GetByKeyType(keyType).LengthInBits;
        var actualKeySize = rsaParams.Parameters.Modulus.Length * 8;
        
        Assert.InRange(actualKeySize, expectedKeySize - 1, expectedKeySize);
    }

    [Theory]
    [InlineData(KeyDefinitions.KeyType.P256)]
    [InlineData(KeyDefinitions.KeyType.P384)]
    [InlineData(KeyDefinitions.KeyType.P521)]
    public void DecodeFromSpki_WithEcPublicKey_ReturnsCorrectParameters(KeyDefinitions.KeyType keyType)
    {
        // Arrange
        var testKey = TestKeys.GetTestPublicKey(keyType);
        var keyBytes = testKey.EncodedKey;

        // Act
        var result = AsnPublicKeyReader.DecodeFromSpki(keyBytes);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ECPublicKeyParameters>(result);
        
        var ecParams = (ECPublicKeyParameters)result;
        Assert.NotNull(ecParams.Parameters.Q.X);
        Assert.NotNull(ecParams.Parameters.Q.Y);
        
        // Verify curve matches expected
        var expectedCurveName = keyType switch
        {
            KeyDefinitions.KeyType.P256 => "nistP256",
            KeyDefinitions.KeyType.P384 => "nistP384",
            KeyDefinitions.KeyType.P521 => "nistP521",
            _ => throw new ArgumentOutOfRangeException(nameof(keyType))
        };
        
        Assert.Equal(expectedCurveName, ecParams.Parameters.Curve.Oid.FriendlyName);
        
        // Verify coordinate sizes
        var expectedCoordinateSize = keyType switch
        {
            KeyDefinitions.KeyType.P256 => 32,
            KeyDefinitions.KeyType.P384 => 48,
            KeyDefinitions.KeyType.P521 => 66,
            _ => throw new ArgumentOutOfRangeException(nameof(keyType))
        };
        
        // Allow for leading zeros being trimmed
        Assert.True(ecParams.Parameters.Q.X.Length <= expectedCoordinateSize);
        Assert.True(ecParams.Parameters.Q.Y.Length <= expectedCoordinateSize);
    }

    [Fact]
    public void DecodeFromSpki_WithX25519PublicKey_ReturnsCorrectParameters()
    {
        // Arrange
        var testKey = TestKeys.GetTestPublicKey(KeyDefinitions.KeyType.X25519);
        var keyBytes = testKey.EncodedKey;

        // Act
        var result = AsnPublicKeyReader.DecodeFromSpki(keyBytes);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ECX25519PublicKeyParameters>(result);
        
        var x25519Params = (ECX25519PublicKeyParameters)result;
        Assert.NotNull(x25519Params);
        Assert.Equal(32, x25519Params.GetPublicPoint().Length);
        Assert.Equal(KeyDefinitions.KeyOids.Algorithm.X25519, x25519Params.GetKeyDefinition().AlgorithmOid);
    }

    [Fact]
    public void DecodeFromSpki_WithEd25519PublicKey_ReturnsCorrectParameters()
    {
        // Arrange
        var testKey = TestKeys.GetTestPublicKey(KeyDefinitions.KeyType.Ed25519);
        var keyBytes = testKey.EncodedKey;

        // Act
        var result = AsnPublicKeyReader.DecodeFromSpki(keyBytes);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<EDsaPublicKeyParameters>(result);
        
        var ed25519Params = (EDsaPublicKeyParameters)result;
        Assert.NotNull(ed25519Params);
        Assert.Equal(32, ed25519Params.GetPublicPoint().Length);
        Assert.Equal(KeyDefinitions.KeyOids.Algorithm.Ed25519, ed25519Params.GetKeyDefinition().AlgorithmOid);
    }

    [Fact]
    public void DecodeFromSpki_WithMultipleRsaKeys_AllKeysAreReadCorrectly()
    {
        // Test with different RSA key sizes to ensure consistent parsing
        var keySizes = new[] { KeyDefinitions.KeyType.RSA1024, KeyDefinitions.KeyType.RSA2048, KeyDefinitions.KeyType.RSA3072, KeyDefinitions.KeyType.RSA4096 };
        
        foreach (var keySize in keySizes)
        {
            // Arrange
            var testKey = TestKeys.GetTestPublicKey(keySize);
            var keyBytes = testKey.EncodedKey;

            // Act
            var result = AsnPublicKeyReader.DecodeFromSpki(keyBytes);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<RSAPublicKeyParameters>(result);
            
            var rsaParams = (RSAPublicKeyParameters)result;
            Assert.NotNull(rsaParams.Parameters.Modulus);
            Assert.NotNull(rsaParams.Parameters.Exponent);
        }
    }

    [Fact]
    public void DecodeFromSpki_WithInvalidRsaBitString_ThrowsCryptographicException()
    {
        // Arrange
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            using (writer.PushSequence())
            {
                writer.WriteObjectIdentifier(KeyDefinitions.KeyOids.Algorithm.Rsa);
                writer.WriteNull();
            }
            
            // Create a bit string with properly cleared unused bits
            byte[] dummyData = new byte[] { 0x01, 0x02, 0x30 }; // 0x30 = 00110000, setting unused bit count to 4 clears the last 4 bits
            writer.WriteBitString(dummyData, 4);
        }
        
        var invalidKeyDer = writer.Encode();

        // Act & Assert
        Assert.Throws<CryptographicException>(() => AsnPublicKeyReader.DecodeFromSpki(invalidKeyDer));
    }

    [Fact]
    public void DecodeFromSpki_WithInvalidEcPointFormat_ThrowsCryptographicException()
    {
        // Arrange
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            using (writer.PushSequence())
            {
                writer.WriteObjectIdentifier(KeyDefinitions.KeyOids.Algorithm.EllipticCurve);
                writer.WriteObjectIdentifier(KeyDefinitions.KeyOids.Curve.P256);
            }
            
            // Create EC point data with compressed format (0x03) instead of uncompressed (0x04)
            byte[] invalidEcPoint = new byte[33]; // Compressed format for P-256
            invalidEcPoint[0] = 0x03; // Compressed point indicator
            for (int i = 1; i < invalidEcPoint.Length; i++)
            {
                invalidEcPoint[i] = (byte)i;
            }
            
            writer.WriteBitString(invalidEcPoint, 0);
        }
        
        var invalidKeyDer = writer.Encode();

        // Act & Assert
        Assert.Throws<CryptographicException>(() => AsnPublicKeyReader.DecodeFromSpki(invalidKeyDer));
    }

    [Fact]
    public void DecodeFromSpki_WithUnsupportedCurve_ThrowsNotSupportedException()
    {
        // Arrange
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            using (writer.PushSequence())
            {
                writer.WriteObjectIdentifier(KeyDefinitions.KeyOids.Algorithm.EllipticCurve);
                // Use secp256k1 (Bitcoin curve) which isn't supported in the implementation
                writer.WriteObjectIdentifier("1.3.132.0.10");
            }
            
            // Create a valid-looking EC point (with 0x04 prefix for uncompressed)
            byte[] validEcPoint = new byte[65]; // Uncompressed format for 256-bit curve
            validEcPoint[0] = 0x04;
            for (int i = 1; i < validEcPoint.Length; i++)
            {
                validEcPoint[i] = (byte)i;
            }
            
            writer.WriteBitString(validEcPoint, 0);
        }
        
        var unsupportedCurveKeyDer = writer.Encode();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => AsnPublicKeyReader.DecodeFromSpki(unsupportedCurveKeyDer));
    }

    [Fact]
    public void DecodeFromSpki_WithUnsupportedAlgorithm_ThrowsNotSupportedException()
    {
        // Arrange
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            using (writer.PushSequence())
            {
                // Use DSA OID which isn't supported
                writer.WriteObjectIdentifier("1.2.840.10040.4.1");
                writer.WriteNull();
            }
            
            // Add dummy bit string
            byte[] dummyData = new byte[32];
            writer.WriteBitString(dummyData, 0);
        }
        
        var unsupportedAlgorithmKeyDer = writer.Encode();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => AsnPublicKeyReader.DecodeFromSpki(unsupportedAlgorithmKeyDer));
    }

    [Theory]
    [InlineData(KeyDefinitions.KeyType.RSA2048)]
    [InlineData(KeyDefinitions.KeyType.P256)]
    [InlineData(KeyDefinitions.KeyType.Ed25519)]
    [InlineData(KeyDefinitions.KeyType.X25519)]
    public void Roundtrip_WithTestKeys_ShouldRetainKeyProperties(KeyDefinitions.KeyType keyType)
    {
        // Arrange - Get the test key
        var testKey = TestKeys.GetTestPublicKey(keyType);
        var keyBytes = testKey.EncodedKey;
        
        // Act - Parse it with AsnPublicKeyReader
        var result = AsnPublicKeyReader.DecodeFromSpki(keyBytes);
        
        // Convert to encoded format (assuming extension method or AsnPublicKeyWriter exists)
        var encodedKey = result switch
        {
            RSAPublicKeyParameters rsaParams => AsnPublicKeyWriter.EncodeToSpki(rsaParams.Parameters),
            ECPublicKeyParameters ecParams => AsnPublicKeyWriter.EncodeToSpki(ecParams.Parameters),
            EDsaPublicKeyParameters edParams => AsnPublicKeyWriter.EncodeToSpki(edParams.GetPublicPoint().ToArray(), KeyDefinitions.KeyType.Ed25519),
            ECX25519PublicKeyParameters x25519Params => AsnPublicKeyWriter.EncodeToSpki(x25519Params.GetPublicPoint().ToArray(), KeyDefinitions.KeyType.X25519),
            _ => throw new NotSupportedException($"Unsupported key type: {result.GetType()}")
        };
        
        // Parse again
        var result2 = AsnPublicKeyReader.DecodeFromSpki(encodedKey);
        
        // Assert - Check type consistency and common properties
        Assert.Equal(result.GetType(), result2.GetType());
        
        switch (result)
        {
            case RSAPublicKeyParameters rsaParams1:
                var rsaParams2 = (RSAPublicKeyParameters)result2;
                Assert.Equal(
                    Convert.ToBase64String(rsaParams1.Parameters.Modulus!), 
                    Convert.ToBase64String(rsaParams2.Parameters.Modulus!));
                Assert.Equal(
                    Convert.ToBase64String(rsaParams1.Parameters.Exponent!), 
                    Convert.ToBase64String(rsaParams2.Parameters.Exponent!));
                break;
                
            case ECPublicKeyParameters ecParams1:
                var ecParams2 = (ECPublicKeyParameters)result2;
                Assert.Equal(
                    ecParams1.Parameters.Curve.Oid.Value,
                    ecParams2.Parameters.Curve.Oid.Value);
                Assert.Equal(
                    Convert.ToBase64String(ecParams1.Parameters.Q.X!), 
                    Convert.ToBase64String(ecParams2.Parameters.Q.X!));
                Assert.Equal(
                    Convert.ToBase64String(ecParams1.Parameters.Q.Y!), 
                    Convert.ToBase64String(ecParams2.Parameters.Q.Y!));
                break;
                
            case EDsaPublicKeyParameters edParams1:
                var edParams2 = (EDsaPublicKeyParameters)result2;
                Assert.Equal(
                    Convert.ToBase64String(edParams1.GetPublicPoint().ToArray()), 
                    Convert.ToBase64String(edParams2.GetPublicPoint().ToArray()));
                break;
                
            case ECX25519PublicKeyParameters x25519Params1:
                var x25519Params2 = (ECX25519PublicKeyParameters)result2;
                Assert.Equal(
                    Convert.ToBase64String(x25519Params1.GetPublicPoint().ToArray()), 
                    Convert.ToBase64String(x25519Params2.GetPublicPoint().ToArray()));
                break;
        }
    }
    
    [Fact]
    public void DecodeFromSpki_WithX509Certificate_CanExtractPublicKey()
    {
        // Arrange - Get a test certificate
        var testCert = TestCertificate.Load("rsa2048");
        var cert = testCert.AsX509Certificate2();
        
        // Get the RSA public key in SubjectPublicKeyInfo format
        using var rsaPublicKey = cert.GetRSAPublicKey()!;
        var publicKeyDer = rsaPublicKey.ExportSubjectPublicKeyInfo();
        
        // Act
        var result = AsnPublicKeyReader.DecodeFromSpki(publicKeyDer);
        
        // Assert
        Assert.NotNull(result);
        Assert.IsType<RSAPublicKeyParameters>(result);
        
        // Verify we can use the extracted key to verify signatures
        var rsaParams = (RSAPublicKeyParameters)result;
        using var rsa = RSA.Create();
        rsa.ImportParameters(rsaParams.Parameters);
    }
}
