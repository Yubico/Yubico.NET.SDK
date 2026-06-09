// Copyright 2025 Yubico AB
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

using System.Formats.Cbor;
using Xunit;
using Yubico.YubiKit.Fido2.Extensions;

namespace Yubico.YubiKit.Fido2.UnitTests.Extensions;

/// <summary>
/// Unit tests for the ExtensionBuilder class.
/// </summary>
public class ExtensionBuilderTests
{

    [Fact]
    public void Build_WithNoExtensions_ReturnsNull()
    {
        // Arrange
        var builder = new ExtensionBuilder();

        // Act
        var result = builder.Build();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Build_WithCredProtect_EncodesCorrectly()
    {
        // Arrange
        var builder = new ExtensionBuilder()
            .WithCredProtect(CredProtectPolicy.UserVerificationRequired);

        // Act
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        var reader = new CborReader(result.Value, CborConformanceMode.Lax);
        var count = reader.ReadStartMap();
        Assert.Equal(1, count);
        Assert.Equal("credProtect", reader.ReadTextString());
        Assert.Equal(3, reader.ReadInt32());
    }

    [Fact]
    public void Build_WithCredBlob_EncodesCorrectly()
    {
        // Arrange
        var blob = new byte[] { 1, 2, 3, 4, 5 };
        var builder = new ExtensionBuilder()
            .WithCredBlob(blob);

        // Act
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        var reader = new CborReader(result.Value, CborConformanceMode.Lax);
        reader.ReadStartMap();
        Assert.Equal("credBlob", reader.ReadTextString());
        Assert.Equal(blob, reader.ReadByteString());
    }

    [Fact]
    public void Build_WithLargeBlob_EncodesCorrectly()
    {
        // Arrange
        var builder = new ExtensionBuilder()
            .WithLargeBlob(LargeBlobSupport.Required);

        // Act
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        var reader = new CborReader(result.Value, CborConformanceMode.Lax);
        reader.ReadStartMap();
        Assert.Equal("largeBlob", reader.ReadTextString());
        reader.ReadStartMap();
        Assert.Equal("support", reader.ReadTextString());
        Assert.Equal("required", reader.ReadTextString());
    }

    [Fact]
    public void Build_WithLargeBlobRead_EncodesCorrectly()
    {
        // Arrange
        var builder = new ExtensionBuilder()
            .WithLargeBlobRead();

        // Act
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        var reader = new CborReader(result.Value, CborConformanceMode.Lax);
        reader.ReadStartMap();
        Assert.Equal("largeBlob", reader.ReadTextString());
        reader.ReadStartMap();
        Assert.Equal("read", reader.ReadTextString());
        Assert.True(reader.ReadBoolean());
    }

    [Fact]
    public void Build_WithMinPinLength_EncodesCorrectly()
    {
        // Arrange
        var builder = new ExtensionBuilder()
            .WithMinPinLength();

        // Act
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        var reader = new CborReader(result.Value, CborConformanceMode.Lax);
        reader.ReadStartMap();
        Assert.Equal("minPinLength", reader.ReadTextString());
        Assert.True(reader.ReadBoolean());
    }

    [Fact]
    public void Build_WithPrf_EncodesCorrectly()
    {
        // Arrange
        var builder = new ExtensionBuilder()
            .WithPrf();

        // Act
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        var reader = new CborReader(result.Value, CborConformanceMode.Lax);
        reader.ReadStartMap();
        Assert.Equal("prf", reader.ReadTextString());
        // Empty map for makeCredential
        var mapCount = reader.ReadStartMap();
        Assert.Equal(0, mapCount);
    }

    [Fact]
    public void Build_WithHmacSecretMc_EncodesCorrectly()
    {
        // Arrange
        var builder = new ExtensionBuilder()
            .WithHmacSecretMakeCredential();

        // Act
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        var reader = new CborReader(result.Value, CborConformanceMode.Lax);
        reader.ReadStartMap();
        Assert.Equal("hmac-secret-mc", reader.ReadTextString());
        Assert.True(reader.ReadBoolean());
    }

    [Fact]
    public void Build_WithLargeBlobKey_EncodesCorrectly()
    {
        // Arrange
        var builder = new ExtensionBuilder()
            .WithLargeBlobKey();

        // Act
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        var reader = new CborReader(result.Value, CborConformanceMode.Lax);
        reader.ReadStartMap();
        Assert.Equal("largeBlobKey", reader.ReadTextString());
        Assert.True(reader.ReadBoolean());
    }

    [Fact]
    public void Build_WithCredBlobOversized_AllowsOversizedInput()
    {
        // Arrange
        var oversizedBlob = new byte[128]; // Max spec limit is 64 bytes
        var builder = new ExtensionBuilder()
            .WithCredBlob(oversizedBlob);

        // Act
        var result = builder.Build();

        // Assert
        // SDK currently has no size validation - builder accepts any size
        Assert.NotNull(result);
        var reader = new CborReader(result.Value, CborConformanceMode.Lax);
        reader.ReadStartMap();
        Assert.Equal("credBlob", reader.ReadTextString());
        var decoded = reader.ReadByteString();
        Assert.Equal(128, decoded.Length);
    }



    [Fact]
    public void Build_WithMultipleExtensions_EncodesAllCorrectly()
    {
        // Arrange
        var builder = new ExtensionBuilder()
            .WithCredProtect(CredProtectPolicy.UserVerificationOptionalWithCredentialIdList)
            .WithCredBlob(new byte[] { 1, 2, 3 })
            .WithMinPinLength();

        // Act
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        var reader = new CborReader(result.Value, CborConformanceMode.Lax);
        var count = reader.ReadStartMap();
        Assert.Equal(3, count);

        // Keys should be sorted: credBlob < credProtect < minPinLength
        Assert.Equal("credBlob", reader.ReadTextString());
        reader.SkipValue(); // byte string

        Assert.Equal("credProtect", reader.ReadTextString());
        Assert.Equal(2, reader.ReadInt32());

        Assert.Equal("minPinLength", reader.ReadTextString());
        Assert.True(reader.ReadBoolean());
    }

    [Fact]
    public void Build_WithHmacSecret_EncodesCorrectly()
    {
        // Arrange
        var keyAgreement = new Dictionary<int, object?>
        {
            { 1, 2 },  // kty = EC2
            { 3, -25 }, // alg = ECDH-ES+HKDF-256
            { -1, 1 }, // crv = P-256
            { -2, new byte[32] }, // x
            { -3, new byte[32] }  // y
        };

        var hmacInput = new HmacSecretInput
        {
            KeyAgreement = keyAgreement,
            SaltEnc = new byte[48],
            SaltAuth = new byte[32],
            PinUvAuthProtocol = 2
        };

        var builder = new ExtensionBuilder()
            .WithHmacSecret(hmacInput);

        // Act
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        var reader = new CborReader(result.Value, CborConformanceMode.Lax);
        reader.ReadStartMap();
        Assert.Equal("hmac-secret", reader.ReadTextString());
        // hmac-secret value is a map
        var innerCount = reader.ReadStartMap();
        Assert.Equal(4, innerCount);
    }



    [Fact]
    public void Builder_SupportsChainingAllMethods()
    {
        // Arrange & Act
        var builder = new ExtensionBuilder()
            .WithCredProtect(CredProtectPolicy.UserVerificationRequired)
            .WithCredBlob(new byte[16])
            .WithLargeBlob(LargeBlobSupport.Preferred)
            .WithMinPinLength()
            .WithPrf();

        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        var reader = new CborReader(result.Value, CborConformanceMode.Lax);
        var count = reader.ReadStartMap();
        Assert.Equal(5, count); // All 5 extensions
    }



    [Fact]
    public void WithCredBlob_EmptyBlob_EncodesEmptyByteString()
    {
        // Arrange
        var builder = new ExtensionBuilder()
            .WithCredBlob(ReadOnlyMemory<byte>.Empty);

        // Act
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        var reader = new CborReader(result.Value, CborConformanceMode.Lax);
        reader.ReadStartMap();
        Assert.Equal("credBlob", reader.ReadTextString());
        var blob = reader.ReadByteString();
        Assert.Empty(blob);
    }

    [Theory]
    [InlineData(CredProtectPolicy.UserVerificationOptional, 1)]
    [InlineData(CredProtectPolicy.UserVerificationOptionalWithCredentialIdList, 2)]
    [InlineData(CredProtectPolicy.UserVerificationRequired, 3)]
    public void WithCredProtect_AllPolicies_EncodeCorrectValue(CredProtectPolicy policy, int expectedValue)
    {
        // Arrange
        var builder = new ExtensionBuilder()
            .WithCredProtect(policy);

        // Act
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        var reader = new CborReader(result.Value, CborConformanceMode.Lax);
        reader.ReadStartMap();
        Assert.Equal("credProtect", reader.ReadTextString());
        Assert.Equal(expectedValue, reader.ReadInt32());
    }

    [Fact]
    public void WithLargeBlob_PreferredSupport_EncodesPreferred()
    {
        // Arrange
        var builder = new ExtensionBuilder()
            .WithLargeBlob(LargeBlobSupport.Preferred);

        // Act
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        var reader = new CborReader(result.Value, CborConformanceMode.Lax);
        reader.ReadStartMap();
        Assert.Equal("largeBlob", reader.ReadTextString());
        reader.ReadStartMap();
        Assert.Equal("support", reader.ReadTextString());
        Assert.Equal("preferred", reader.ReadTextString());
    }

    [Fact]
    public void WithHmacSecret_InvalidSalt1Length_ThrowsArgumentException()
    {
        // Arrange
        var protocol = new MockPinProtocol();
        var sharedSecret = new byte[32];
        var keyAgreement = new Dictionary<int, object?> { { 1, 2 } };
        var invalidSalt1 = new byte[31]; // Wrong length, should be 32

        var builder = new ExtensionBuilder();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            builder.WithHmacSecret(protocol, sharedSecret, keyAgreement, invalidSalt1));

        Assert.Contains("Salt1 must be exactly 32 bytes", exception.Message);
    }

    [Fact]
    public void WithHmacSecret_InvalidSalt2Length_ThrowsArgumentException()
    {
        // Arrange
        var protocol = new MockPinProtocol();
        var sharedSecret = new byte[32];
        var keyAgreement = new Dictionary<int, object?> { { 1, 2 } };
        var validSalt1 = new byte[32];
        var invalidSalt2 = new byte[31]; // Wrong length, should be 32

        var builder = new ExtensionBuilder();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            builder.WithHmacSecret(protocol, sharedSecret, keyAgreement, validSalt1, invalidSalt2));

        Assert.Contains("Salt2 must be exactly 32 bytes", exception.Message);
    }

}

/// <summary>
/// Mock PIN/UV auth protocol for unit testing.
/// </summary>
internal class MockPinProtocol : Yubico.YubiKit.Fido2.Pin.IPinUvAuthProtocol
{
    public int Version => 2;

    public int AuthenticationTagLength => 16;

    public (Dictionary<int, object?> KeyAgreement, byte[] SharedSecret) Encapsulate(IReadOnlyDictionary<int, object?> peerCoseKey)
    {
        ArgumentNullException.ThrowIfNull(peerCoseKey);

        var keyAgreement = new Dictionary<int, object?>
        {
            { 1, 2 }, // kty = EC2
            { 3, -25 }, // alg = ECDH-ES+HKDF-256
            { -1, 1 }, // crv = P-256
            { -2, new byte[32] }, // x
            { -3, new byte[32] }  // y
        };
        var sharedSecret = new byte[32];
        return (keyAgreement, sharedSecret);
    }

    public byte[] Kdf(ReadOnlySpan<byte> z)
    {
        // Simple mock - return fixed key
        return new byte[32];
    }

    public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext)
    {
        // Simple XOR for testing
        var ciphertext = new byte[plaintext.Length];
        for (int i = 0; i < plaintext.Length; i++)
        {
            ciphertext[i] = (byte)(plaintext[i] ^ key[i % key.Length]);
        }
        return ciphertext;
    }

    public byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext)
    {
        // Simple XOR for testing (symmetric)
        return Encrypt(key, ciphertext);
    }

    public byte[] Authenticate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message)
    {
        // Simple hash for testing
        var auth = new byte[16];
        for (int i = 0; i < message.Length; i++)
        {
            auth[i % auth.Length] ^= message[i];
        }
        return auth;
    }

    public bool Verify(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
    {
        var expected = Authenticate(key, message);
        return expected.AsSpan().SequenceEqual(signature);
    }

    public void Dispose()
    {
        // No resources to dispose in mock
    }
}