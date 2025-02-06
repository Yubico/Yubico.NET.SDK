using System;
using System.Buffers.Text;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Yubico.YubiKey.Fido2.Cbor;

namespace Yubico.YubiKey.Fido2.Cose
{
    public class CoseEdDsaPublicKeyTests
    {
        private const string ValidEd25519PublicKeyPem = @"
        -----BEGIN PUBLIC KEY-----
        MCowBQYDK2VwAyEAGD3XimAmDGEYgGkS7BQIi6YJsm8bkJdo6rbHl2z4qao=
        -----END PUBLIC KEY-----
        ";

        private static byte[] ValidEd25519PublicKey => Convert
        .FromBase64String(ValidEd25519PublicKeyPem
            .Replace("-----BEGIN PUBLIC KEY-----", "")
            .Replace("-----END PUBLIC KEY-----", "")
            .Replace("\n", "")
            .Trim())
        .Skip(12) // Skip the ASN.1 metadata bytes and keep only the public key data (32 bytes)
        .ToArray();

        [Fact]
        public void CreateFromPublicKeyData_ValidKey_ReturnsExpectedKey()
        {
            // Act
            var coseKey = CoseEdDsaPublicKey.CreateFromPublicKeyData(ValidEd25519PublicKey);

            // Assert
            Assert.Equal(CoseKeyType.Okp, coseKey.Type);
            Assert.Equal(CoseEcCurve.Ed25519, coseKey.Curve);
            Assert.Equal(CoseAlgorithmIdentifier.EdDSA, coseKey.Algorithm);
            Assert.True(coseKey.PublicKey.Span.SequenceEqual(ValidEd25519PublicKey));
        }

        [Fact]
        public void CreateFromPublicKeyData_InvalidLength_ThrowsArgumentException()
        {
            // Arrange
            byte[] invalidKey = new byte[31]; // Wrong length

            // Act & Assert
            Assert.Throws<ArgumentException>(() => CoseEdDsaPublicKey.CreateFromPublicKeyData(invalidKey));
        }

        [Fact]
        public void EncodingRoundtrip_ReturnsMatchingKey()
        {
            // Arrange
            var originalKey = CoseEdDsaPublicKey.CreateFromPublicKeyData(ValidEd25519PublicKey);

            // Act
            var encodedKey = originalKey.Encode();
            var decodedKey = CoseEdDsaPublicKey.CreateFromEncodedKey(encodedKey);

            // Assert
            Assert.Equal(originalKey.Type, decodedKey.Type);
            Assert.Equal(originalKey.Curve, decodedKey.Curve);
            Assert.Equal(originalKey.Algorithm, decodedKey.Algorithm);
            Assert.True(originalKey.PublicKey.Span.SequenceEqual(decodedKey.PublicKey.Span));
        }

        [Fact]
        public void Encode_ValidKey_ContainsRequiredMapEntries()
        {
            // Arrange
            var coseKey = CoseEdDsaPublicKey.CreateFromPublicKeyData(ValidEd25519PublicKey);

            // Act
            var encoded = coseKey.Encode();
            var map = new CborMap<int>(encoded);

            var keyType = map.ReadInt32(1);
            var algorithm = map.ReadInt32(3);
            var curve = map.ReadInt32(-1);
            var publicKey = map.ReadByteString(-2).Span;

            // Assert
            Assert.Equal((int)CoseKeyType.Okp, keyType); // kty
            Assert.Equal((int)CoseAlgorithmIdentifier.EdDSA, algorithm); // alg
            Assert.Equal((int)CoseEcCurve.Ed25519, curve); // crv
            Assert.True(publicKey.SequenceEqual(ValidEd25519PublicKey)); // x
        }

        [Fact]
        public void PublicKey_SetInvalidLength_ThrowsArgumentException()
        {
            // Arrange
            var coseKey = CoseEdDsaPublicKey.CreateFromPublicKeyData(ValidEd25519PublicKey);
            var invalidKey = new byte[31];

            // Act & Assert
            Assert.Throws<ArgumentException>(() => coseKey.PublicKey = invalidKey);
        }

        [Fact]
        public void Curve_SetInvalidCurve_ThrowsArgumentException()
        {
            // Arrange
            var coseKey = CoseEdDsaPublicKey.CreateFromPublicKeyData(ValidEd25519PublicKey);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => coseKey.Curve = CoseEcCurve.P256);
        }
    }
}
