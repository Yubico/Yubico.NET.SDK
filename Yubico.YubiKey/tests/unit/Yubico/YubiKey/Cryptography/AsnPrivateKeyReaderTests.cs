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
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Cryptography;

public class AsnPrivateKeyReaderTests
{
    [Theory]
    [InlineData(KeyDefinitions.KeyType.RSA1024)]
    [InlineData(KeyDefinitions.KeyType.RSA2048)]
    [InlineData(KeyDefinitions.KeyType.RSA3072)]
    [InlineData(KeyDefinitions.KeyType.RSA4096)]
    public void FromEncodedKey_WithRsaPrivateKey_ReturnsCorrectParameters(
        KeyDefinitions.KeyType keyType)
    {
        // Arrange
        var testKey = TestKeys.GetTestPrivateKey(keyType);
        var keyBytes = testKey.EncodedKey;

        // Act
        var result = AsnPrivateKeyReader.DecodePkcs8EncodedKey(keyBytes);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<RSAPrivateKeyParameters>(result);

        var rsaParams = (RSAPrivateKeyParameters)result;
        Assert.NotNull(rsaParams.Parameters.Modulus!);
        Assert.NotNull(rsaParams.Parameters.Exponent!);
        Assert.NotNull(rsaParams.Parameters.D);
        Assert.NotNull(rsaParams.Parameters.P);
        Assert.NotNull(rsaParams.Parameters.Q);
        Assert.NotNull(rsaParams.Parameters.DP);
        Assert.NotNull(rsaParams.Parameters.DQ);
        Assert.NotNull(rsaParams.Parameters.InverseQ);

        var expectedKeySize = KeyDefinitions.GetByKeyType(keyType).LengthInBits;

        // Calculate approximate key size from modulus length
        var actualKeySize = rsaParams.Parameters.Modulus!.Length * 8;
        Assert.InRange(actualKeySize, expectedKeySize - 8, expectedKeySize);
    }

    [Theory]
    [InlineData(KeyDefinitions.KeyType.P256)]
    [InlineData(KeyDefinitions.KeyType.P384)]
    [InlineData(KeyDefinitions.KeyType.P521)]
    public void FromEncodedKey_WithEcPrivateKey_ReturnsCorrectParameters(
        KeyDefinitions.KeyType keyType)
    {
        // Arrange
        var testKey = TestKeys.GetTestPrivateKey(keyType);
        var keyBytes = testKey.EncodedKey;

        // Act
        var privateKeyParameters = AsnPrivateKeyReader.DecodePkcs8EncodedKey(keyBytes);

        // Assert
        Assert.NotNull(privateKeyParameters);
        Assert.IsType<ECPrivateKeyParameters>(privateKeyParameters);

        var ecParams = (ECPrivateKeyParameters)privateKeyParameters;
        Assert.NotNull(ecParams.Parameters.D);

        // Verify curve matches expected
        var expectedCurveOid = keyType switch
        {
            KeyDefinitions.KeyType.P256 => KeyDefinitions.KeyOids.Curve.P256,
            KeyDefinitions.KeyType.P384 => KeyDefinitions.KeyOids.Curve.P384,
            KeyDefinitions.KeyType.P521 => KeyDefinitions.KeyOids.Curve.P521,
            _ => throw new ArgumentOutOfRangeException(nameof(keyType))
        };

        Assert.Equal(expectedCurveOid, ecParams.Parameters.Curve.Oid.Value);

        // Verify D size (private key component)
        var expectedDSize = KeyDefinitions.GetByKeyType(keyType).LengthInBytes;

        // Allow for leading zeros being trimmed
        Assert.True(ecParams.Parameters.D.Length <= expectedDSize,
            $"D component length {ecParams.Parameters.D.Length} exceeds expected max {expectedDSize}");
    }

    // [Fact]
    // public void FromEncodedKey_WithX25519PrivateKey_ReturnsCorrectParameters()
    // {
    //     // Arrange
    //     var testKey = TestKeys.GetTestPrivateKey(KeyDefinitions.KeyType.X25519);
    //     var keyBytes = testKey.KeyBytes;
    //
    //     // Act
    //     var result = AsnPrivateKeyReader.DecodePkcs8EncodedKey(keyBytes);
    //
    //     // Assert
    //     Assert.NotNull(result);
    //     Assert.IsType<ECX25519PrivateKeyParameters>(result);
    //
    //     var x25519Params = (ECX25519PrivateKeyParameters)result;
    //     Assert.Equal(32, x25519Params.GetPrivateKey().Length);
    //     Assert.Equal(KeyDefinitions.KeyOids.Algorithm.X25519, x25519Params.GetKeyDefinition().CurveOid);
    // }
    
    [Fact]
    public void FromEncodedKey_WithX25519PrivateKey_ReturnsCorrectParameters()
    {
        // Arrange
        var testKey = TestKeys.GetTestPrivateKey(KeyDefinitions.KeyType.X25519);
        var keyBytes = testKey.EncodedKey;

        // Act
        var result = AsnPrivateKeyReader.DecodePkcs8EncodedKey(keyBytes);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Curve25519PrivateKeyParameters>(result);

        var x25519Params = (Curve25519PrivateKeyParameters)result;
        Assert.Equal(32, x25519Params.GetPrivateKey().Length);
        Assert.Equal(KeyDefinitions.KeyOids.Algorithm.X25519, x25519Params.GetKeyDefinition().AlgorithmOid);
    }

    // [Fact]
    // public void FromEncodedKey_WithEd25519PrivateKey_ReturnsCorrectParameters()
    // {
    //     // Arrange
    //     var testKey = TestKeys.GetTestPrivateKey(KeyDefinitions.KeyType.Ed25519);
    //     var keyBytes = testKey.KeyBytes;
    //
    //     // Act
    //     var result = AsnPrivateKeyReader.DecodePkcs8EncodedKey(keyBytes);
    //
    //     // Assert
    //     Assert.NotNull(result);
    //     Assert.IsType<EDsaPrivateKeyParameters>(result);
    //
    //     var ed25519Params = (EDsaPrivateKeyParameters)result;
    //     Assert.Equal(32, ed25519Params.GetPrivateKey().Length);
    //     Assert.Equal(KeyDefinitions.KeyOids.Algorithm.Ed25519, ed25519Params.GetKeyDefinition().CurveOid);
    // }
    
    [Fact]
    public void FromEncodedKey_WithEd25519PrivateKey_ReturnsCorrectParameters()
    {
        // Arrange
        var testKey = TestKeys.GetTestPrivateKey(KeyDefinitions.KeyType.Ed25519);
        var keyBytes = testKey.EncodedKey;

        // Act
        var result = AsnPrivateKeyReader.DecodePkcs8EncodedKey(keyBytes);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Curve25519PrivateKeyParameters>(result);

        var ed25519Params = (Curve25519PrivateKeyParameters)result;
        Assert.Equal(32, ed25519Params.GetPrivateKey().Length);
        Assert.Equal(KeyDefinitions.KeyOids.Algorithm.Ed25519, ed25519Params.GetKeyDefinition().AlgorithmOid);
    }

    [Fact]
    public void FromEncodedKey_WithInvalidVersion_ThrowsCryptographicException()
    {
        // Arrange
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            writer.WriteInteger(1); // Invalid version, should be 0

            using (writer.PushSequence())
            {
                writer.WriteObjectIdentifier(KeyDefinitions.KeyOids.Algorithm.Rsa);
                writer.WriteNull();
            }

            writer.WriteOctetString(Array.Empty<byte>()); // Empty key data
        }

        var invalidKeyDer = writer.Encode();

        // Act & Assert
        Assert.Throws<CryptographicException>(() => AsnPrivateKeyReader.DecodePkcs8EncodedKey(invalidKeyDer));
    }

    [Fact]
    public void FromEncodedKey_WithInvalidRsaKeyVersion_ThrowsCryptographicException()
    {
        // Arrange
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            writer.WriteInteger(0); // Correct outer version

            using (writer.PushSequence())
            {
                writer.WriteObjectIdentifier(KeyDefinitions.KeyOids.Algorithm.Rsa);
                writer.WriteNull();
            }

            // Create invalid RSA key with wrong version
            var innerWriter = new AsnWriter(AsnEncodingRules.DER);
            using (innerWriter.PushSequence())
            {
                innerWriter.WriteInteger(1); // Invalid version, should be 0

                // Write dummy values for RSA components
                innerWriter.WriteInteger(0); // modulus
                innerWriter.WriteInteger(0); // public exponent
                innerWriter.WriteInteger(0); // private exponent
                innerWriter.WriteInteger(0); // prime1
                innerWriter.WriteInteger(0); // prime2
                innerWriter.WriteInteger(0); // exponent1
                innerWriter.WriteInteger(0); // exponent2
                innerWriter.WriteInteger(0); // coefficient
            }

            writer.WriteOctetString(innerWriter.Encode());
        }

        var invalidKeyDer = writer.Encode();

        // Act & Assert
        Assert.Throws<CryptographicException>(() => AsnPrivateKeyReader.DecodePkcs8EncodedKey(invalidKeyDer));
    }

    [Fact]
    public void FromEncodedKey_WithInvalidEcKeyVersion_ThrowsCryptographicException()
    {
        // Arrange
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            writer.WriteInteger(0); // Correct outer version

            using (writer.PushSequence())
            {
                writer.WriteObjectIdentifier(KeyDefinitions.KeyOids.Algorithm.EllipticCurve);
                writer.WriteObjectIdentifier(KeyDefinitions.KeyOids.Curve.P256);
            }

            // Create invalid EC key with wrong version
            var innerWriter = new AsnWriter(AsnEncodingRules.DER);
            using (innerWriter.PushSequence())
            {
                innerWriter.WriteInteger(0); // Invalid version, should be 1

                // Write dummy private key
                innerWriter.WriteOctetString(new byte[32]);
            }

            writer.WriteOctetString(innerWriter.Encode());
        }

        var invalidKeyDer = writer.Encode();

        // Act & Assert
        Assert.Throws<CryptographicException>(() => AsnPrivateKeyReader.DecodePkcs8EncodedKey(invalidKeyDer));
    }

    [Fact]
    public void FromEncodedKey_WithUnsupportedCurve_ThrowsNotSupportedException()
    {
        // Arrange
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            writer.WriteInteger(0); // Correct version

            using (writer.PushSequence())
            {
                writer.WriteObjectIdentifier(KeyDefinitions.KeyOids.Algorithm.EllipticCurve);
                // Use secp256k1 (Bitcoin curve) which isn't supported in the implementation
                writer.WriteObjectIdentifier("1.3.132.0.10");
            }

            // Write dummy private key
            writer.WriteOctetString(new byte[32]);
        }

        var unsupportedCurveKeyDer = writer.Encode();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => AsnPrivateKeyReader.DecodePkcs8EncodedKey(unsupportedCurveKeyDer));
    }

    [Fact]
    public void FromEncodedKey_WithUnsupportedAlgorithm_ThrowsNotSupportedException()
    {
        // Arrange
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            writer.WriteInteger(0); // Correct version

            using (writer.PushSequence())
            {
                // Use DSA OID which isn't supported
                writer.WriteObjectIdentifier("1.2.840.10040.4.1");
                writer.WriteNull();
            }

            // Write dummy private key
            writer.WriteOctetString(new byte[32]);
        }

        var unsupportedAlgorithmKeyDer = writer.Encode();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => AsnPrivateKeyReader.DecodePkcs8EncodedKey(unsupportedAlgorithmKeyDer));
    }

    [Fact]
    public void FromEncodedKey_WithInvalidX25519KeyLength_ThrowsCryptographicException()
    {
        // Arrange
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            writer.WriteInteger(0);

            using (writer.PushSequence())
            {
                writer.WriteObjectIdentifier(KeyDefinitions.KeyOids.Algorithm.X25519);
            }

            // Write invalid X25519 key (should be 32 bytes)
            writer.WriteOctetString(new byte[16]);
        }

        var invalidKeyDer = writer.Encode();

        // Act & Assert
        Assert.Throws<CryptographicException>(() => AsnPrivateKeyReader.DecodePkcs8EncodedKey(invalidKeyDer));
    }

    [Fact]
    public void FromEncodedKey_WithInvalidEd25519KeyLength_ThrowsCryptographicException()
    {
        // Arrange
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            writer.WriteInteger(0);

            using (writer.PushSequence())
            {
                writer.WriteObjectIdentifier(KeyDefinitions.KeyOids.Algorithm.Ed25519);
            }

            // Write invalid Ed25519 key (should be 32 bytes)
            writer.WriteOctetString(new byte[16]);
        }

        var invalidKeyDer = writer.Encode();

        // Act & Assert
        Assert.Throws<CryptographicException>(() => AsnPrivateKeyReader.DecodePkcs8EncodedKey(invalidKeyDer));
    }

    // [Theory]
    // // [InlineData(KeyDefinitions.KeyType.RSA2048)] // TODO Because our SDK doesn't make use of the excluded RSA parameters in the CRT format, we cant make a full PKCS privateKeyInfo  
    // [InlineData(KeyDefinitions.KeyType.P256)]
    // [InlineData(KeyDefinitions.KeyType.P384)]
    // [InlineData(KeyDefinitions.KeyType.P521)]
    // [InlineData(KeyDefinitions.KeyType.Ed25519)]
    // [InlineData(KeyDefinitions.KeyType.X25519)]
    // public void Roundtrip_WithTestKeys_ShouldRetainKeyProperties(KeyDefinitions.KeyType keyType)  // Todo doesnt work with P256, P384, P521 because the format we write is different than the format we read. Why?
    // {
    //     // Arrange
    //     var testKey = TestKeys.GetTestPrivateKey(keyType);
    //     var testEncodedKey = testKey.EncodedKey;
    //
    //     // Act
    //     var privateKeyParameters = AsnPrivateKeyReader.DecodePkcs8EncodedKey(testEncodedKey);
    //
    //     // Check that the encoded key matches the original input
    //     // Assert.Equal(
    //     //     Convert.ToBase64String(keyBytes), 
    //     //     Convert.ToBase64String(privateKeyParameters.GetEncoded().ToArray())
    //     // );
    //
    //     var exportedEncodedKey = privateKeyParameters.ExportPkcs8PrivateKey();
    //     var expectedByteString = string.Join(" ", testEncodedKey.Select(p => p.ToString("X2")));
    //     var actualyByteString = string.Join(" ", exportedEncodedKey.ToArray().Select(p => p.ToString("X2")));
    //
    //     Assert.Equal(expectedByteString, actualyByteString);
    //     Assert.Equal(testEncodedKey.Length, exportedEncodedKey.Length);
    //     Assert.Equal(testEncodedKey, exportedEncodedKey);
    //     Assert.Equal(KeyDefinitions.GetByKeyType(keyType), privateKeyParameters.GetKeyDefinition());
    // }
    
    [Theory]
    // [InlineData(KeyDefinitions.KeyType.RSA2048)] // Because our SDK doesn't make use of the excluded RSA parameters in the CRT format, we cant make a full PKCS privateKeyInfo  
    [InlineData(KeyDefinitions.KeyType.P256)]
    [InlineData(KeyDefinitions.KeyType.P384)]
    [InlineData(KeyDefinitions.KeyType.P521)]
    [InlineData(KeyDefinitions.KeyType.Ed25519)]
    [InlineData(KeyDefinitions.KeyType.X25519)]
    public void Roundtrip_WithTestKeys_ShouldRetainKeyFunctionality(KeyDefinitions.KeyType keyType) 
    {
        // Arrange
        var testKey = TestKeys.GetTestPrivateKey(keyType);
        var testEncodedKey = testKey.EncodedKey;

        // Act
        var privateKeyParameters = AsnPrivateKeyReader.DecodePkcs8EncodedKey(testEncodedKey);
        var exportedEncodedKey = privateKeyParameters.ExportPkcs8PrivateKey();
        var decodedParams = AsnPrivateKeyReader.DecodePkcs8EncodedKey(exportedEncodedKey);
        AsnPrivateKeyWriterTests.KeyEquivalenceTestHelper.VerifyFunctionalEquivalence(privateKeyParameters, decodedParams, keyType);
    }

    [Fact]
    public void FromEncodedKey_WithKeyPair_CorrectlyExtractsPrivateAndPublicComponents()
    {
        // Arrange
        var (publicKey, privateKey) = TestKeys.GetKeyPair(KeyDefinitions.KeyType.RSA2048);
        var privateKeyBytes = privateKey.EncodedKey;
        var publicKeyBytes = publicKey.EncodedKey;

        // Act
        var privateParams = AsnPrivateKeyReader.DecodePkcs8EncodedKey(privateKeyBytes);
        var publicParams = AsnPublicKeyReader.DecodeFromSpki(publicKeyBytes);

        // Assert
        Assert.IsType<RSAPrivateKeyParameters>(privateParams);
        Assert.IsType<RSAPublicKeyParameters>(publicParams);

        var rsaPrivate = (RSAPrivateKeyParameters)privateParams;
        var rsaPublic = (RSAPublicKeyParameters)publicParams;

        // The modulus and exponent should match between public and private key
        Assert.Equal(
            Convert.ToBase64String(rsaPrivate.Parameters.Modulus!),
            Convert.ToBase64String(rsaPublic.Parameters.Modulus!));

        Assert.Equal(
            Convert.ToBase64String(rsaPrivate.Parameters.Exponent!),
            Convert.ToBase64String(rsaPublic.Parameters.Exponent!));
    }
}
