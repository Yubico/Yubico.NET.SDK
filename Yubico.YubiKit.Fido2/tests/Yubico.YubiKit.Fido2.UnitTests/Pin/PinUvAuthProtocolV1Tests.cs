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

using System.Security.Cryptography;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.UnitTests.Pin;

/// <summary>
/// Unit tests for <see cref="PinUvAuthProtocolV1"/>.
/// </summary>
public sealed class PinUvAuthProtocolV1Tests
{
    [Fact]
    public void Version_Returns1()
    {
        using var protocol = new PinUvAuthProtocolV1();
        
        Assert.Equal(1, protocol.Version);
    }
    
    [Fact]
    public void AuthenticationTagLength_Returns16()
    {
        using var protocol = new PinUvAuthProtocolV1();
        
        Assert.Equal(16, protocol.AuthenticationTagLength);
    }
    
    [Fact]
    public void Encapsulate_ValidPeerKey_ReturnsKeyAgreementAndSecret()
    {
        using var protocol = new PinUvAuthProtocolV1();
        
        using var peerEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var peerParams = peerEcdh.ExportParameters(includePrivateParameters: false);
        
        var peerCoseKey = new Dictionary<int, object?>
        {
            { 1, 2 },
            { -1, 1 },
            { -2, peerParams.Q.X },
            { -3, peerParams.Q.Y }
        };
        
        var (keyAgreement, sharedSecret) = protocol.Encapsulate(peerCoseKey);
        
        Assert.NotNull(keyAgreement);
        Assert.NotNull(sharedSecret);
        Assert.Equal(32, sharedSecret.Length); // V1 uses 32-byte shared secret
        Assert.Equal(2, keyAgreement[1]);
        Assert.Equal(-25, keyAgreement[3]);
        Assert.Equal(1, keyAgreement[-1]);
    }
    
    [Fact]
    public void Encapsulate_NullPeerKey_ThrowsArgumentNullException()
    {
        using var protocol = new PinUvAuthProtocolV1();
        
        Assert.Throws<ArgumentNullException>(() => protocol.Encapsulate(null!));
    }
    
    [Fact]
    public void Encapsulate_MissingXCoordinate_ThrowsArgumentException()
    {
        using var protocol = new PinUvAuthProtocolV1();
        
        var peerCoseKey = new Dictionary<int, object?>
        {
            { 1, 2 },
            { -1, 1 },
            { -3, new byte[32] }
        };
        
        var ex = Assert.Throws<ArgumentException>(() => protocol.Encapsulate(peerCoseKey));
        Assert.Contains("X coordinate", ex.Message);
    }
    
    [Fact]
    public void Kdf_ValidInput_Returns32Bytes()
    {
        using var protocol = new PinUvAuthProtocolV1();
        var z = RandomNumberGenerator.GetBytes(32);
        
        var result = protocol.Kdf(z);
        
        Assert.Equal(32, result.Length);
    }
    
    [Fact]
    public void Kdf_EmptyInput_ThrowsArgumentException()
    {
        using var protocol = new PinUvAuthProtocolV1();
        
        Assert.Throws<ArgumentException>(() => protocol.Kdf([]));
    }
    
    [Fact]
    public void Kdf_SameInput_ProducesSameOutput()
    {
        using var protocol = new PinUvAuthProtocolV1();
        var z = RandomNumberGenerator.GetBytes(32);
        
        var result1 = protocol.Kdf(z);
        var result2 = protocol.Kdf(z);
        
        Assert.Equal(result1, result2);
    }
    
    [Fact]
    public void Kdf_IsSha256()
    {
        using var protocol = new PinUvAuthProtocolV1();
        var z = RandomNumberGenerator.GetBytes(32);
        
        var result = protocol.Kdf(z);
        var expected = SHA256.HashData(z);
        
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void Encrypt_ValidInput_ReturnsCiphertextOnly()
    {
        using var protocol = new PinUvAuthProtocolV1();
        var key = RandomNumberGenerator.GetBytes(32);
        var plaintext = new byte[32];
        RandomNumberGenerator.Fill(plaintext);
        
        var ciphertext = protocol.Encrypt(key, plaintext);
        
        // V1 returns ciphertext only (no IV prefix)
        Assert.Equal(32, ciphertext.Length);
    }
    
    [Fact]
    public void Encrypt_InvalidKeyLength_ThrowsArgumentException()
    {
        using var protocol = new PinUvAuthProtocolV1();
        var key = RandomNumberGenerator.GetBytes(64);
        var plaintext = new byte[16];
        
        var ex = Assert.Throws<ArgumentException>(() => protocol.Encrypt(key, plaintext));
        Assert.Contains("32 bytes", ex.Message);
    }
    
    [Fact]
    public void Encrypt_PlaintextNotMultipleOfBlockSize_ThrowsArgumentException()
    {
        using var protocol = new PinUvAuthProtocolV1();
        var key = RandomNumberGenerator.GetBytes(32);
        var plaintext = new byte[17];
        
        Assert.Throws<ArgumentException>(() => protocol.Encrypt(key, plaintext));
    }
    
    [Fact]
    public void Decrypt_ValidInput_ReturnsPlaintext()
    {
        using var protocol = new PinUvAuthProtocolV1();
        var key = RandomNumberGenerator.GetBytes(32);
        var plaintext = new byte[32];
        RandomNumberGenerator.Fill(plaintext);
        
        var ciphertext = protocol.Encrypt(key, plaintext);
        var decrypted = protocol.Decrypt(key, ciphertext);
        
        Assert.Equal(plaintext, decrypted);
    }
    
    [Fact]
    public void Decrypt_InvalidKeyLength_ThrowsArgumentException()
    {
        using var protocol = new PinUvAuthProtocolV1();
        var key = RandomNumberGenerator.GetBytes(64);
        var ciphertext = new byte[16];
        
        var ex = Assert.Throws<ArgumentException>(() => protocol.Decrypt(key, ciphertext));
        Assert.Contains("32 bytes", ex.Message);
    }
    
    [Fact]
    public void EncryptDecrypt_RoundTrip_ReturnsOriginal()
    {
        using var protocol = new PinUvAuthProtocolV1();
        var key = RandomNumberGenerator.GetBytes(32);
        var plaintext = new byte[64];
        RandomNumberGenerator.Fill(plaintext);
        
        var ciphertext = protocol.Encrypt(key, plaintext);
        var decrypted = protocol.Decrypt(key, ciphertext);
        
        Assert.Equal(plaintext, decrypted);
    }
    
    [Fact]
    public void Authenticate_ValidInput_Returns16ByteMac()
    {
        using var protocol = new PinUvAuthProtocolV1();
        var key = RandomNumberGenerator.GetBytes(32);
        var message = "test message"u8.ToArray();
        
        var mac = protocol.Authenticate(key, message);
        
        Assert.Equal(16, mac.Length);
    }
    
    [Fact]
    public void Authenticate_InvalidKeyLength_ThrowsArgumentException()
    {
        using var protocol = new PinUvAuthProtocolV1();
        var key = RandomNumberGenerator.GetBytes(64);
        var message = "test"u8.ToArray();
        
        var ex = Assert.Throws<ArgumentException>(() => protocol.Authenticate(key, message));
        Assert.Contains("32 bytes", ex.Message);
    }
    
    [Fact]
    public void Authenticate_SameInputs_ProducesConsistentMac()
    {
        using var protocol = new PinUvAuthProtocolV1();
        var key = RandomNumberGenerator.GetBytes(32);
        var message = "test message"u8.ToArray();
        
        var mac1 = protocol.Authenticate(key, message);
        var mac2 = protocol.Authenticate(key, message);
        
        Assert.Equal(mac1, mac2);
    }
    
    [Fact]
    public void Authenticate_IsTruncatedHmacSha256()
    {
        using var protocol = new PinUvAuthProtocolV1();
        var key = RandomNumberGenerator.GetBytes(32);
        var message = "test message"u8.ToArray();
        
        var mac = protocol.Authenticate(key, message);
        
        // V1 should be first 16 bytes of HMAC-SHA-256
        Span<byte> fullHmac = stackalloc byte[32];
        HMACSHA256.HashData(key, message, fullHmac);
        
        Assert.Equal(fullHmac[..16].ToArray(), mac);
    }
    
    [Fact]
    public void Verify_ValidSignature_ReturnsTrue()
    {
        using var protocol = new PinUvAuthProtocolV1();
        var key = RandomNumberGenerator.GetBytes(32);
        var message = "test message"u8.ToArray();
        
        var mac = protocol.Authenticate(key, message);
        var isValid = protocol.Verify(key, message, mac);
        
        Assert.True(isValid);
    }
    
    [Fact]
    public void Verify_InvalidSignature_ReturnsFalse()
    {
        using var protocol = new PinUvAuthProtocolV1();
        var key = RandomNumberGenerator.GetBytes(32);
        var message = "test message"u8.ToArray();
        var invalidMac = RandomNumberGenerator.GetBytes(16);
        
        var isValid = protocol.Verify(key, message, invalidMac);
        
        Assert.False(isValid);
    }
    
    [Fact]
    public void Verify_WrongSignatureLength_ReturnsFalse()
    {
        using var protocol = new PinUvAuthProtocolV1();
        var key = RandomNumberGenerator.GetBytes(32);
        var message = "test message"u8.ToArray();
        var wrongLengthMac = new byte[32]; // Should be 16
        
        var isValid = protocol.Verify(key, message, wrongLengthMac);
        
        Assert.False(isValid);
    }
    
    [Fact]
    public void DisposedProtocol_ThrowsObjectDisposedException()
    {
        var protocol = new PinUvAuthProtocolV1();
        protocol.Dispose();
        
        Assert.Throws<ObjectDisposedException>(() => protocol.Kdf(new byte[32]));
        Assert.Throws<ObjectDisposedException>(() => protocol.Encrypt(new byte[32], new byte[16]));
        Assert.Throws<ObjectDisposedException>(() => protocol.Decrypt(new byte[32], new byte[16]));
        Assert.Throws<ObjectDisposedException>(() => protocol.Authenticate(new byte[32], new byte[16]));
        Assert.Throws<ObjectDisposedException>(() => protocol.Verify(new byte[32], new byte[16], new byte[16]));
    }
    
    [Fact]
    public void V1AndV2_ProduceDifferentSharedSecrets_FromSameKey()
    {
        using var v1 = new PinUvAuthProtocolV1();
        using var v2 = new PinUvAuthProtocolV2();
        
        var z = RandomNumberGenerator.GetBytes(32);
        
        var v1Secret = v1.Kdf(z);
        var v2Secret = v2.Kdf(z);
        
        // V1 = SHA-256(z) = 32 bytes
        // V2 = HKDF derived = 64 bytes
        Assert.Equal(32, v1Secret.Length);
        Assert.Equal(64, v2Secret.Length);
        
        // First 32 bytes should be different due to different KDF
        Assert.NotEqual(v1Secret, v2Secret[..32].ToArray());
    }
}
