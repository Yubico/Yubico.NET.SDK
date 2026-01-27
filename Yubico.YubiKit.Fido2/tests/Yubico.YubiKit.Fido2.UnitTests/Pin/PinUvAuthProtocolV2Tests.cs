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
/// Unit tests for <see cref="PinUvAuthProtocolV2"/>.
/// </summary>
public sealed class PinUvAuthProtocolV2Tests
{
    [Fact]
    public void Version_Returns2()
    {
        using var protocol = new PinUvAuthProtocolV2();
        
        Assert.Equal(2, protocol.Version);
    }
    
    [Fact]
    public void AuthenticationTagLength_Returns32()
    {
        using var protocol = new PinUvAuthProtocolV2();
        
        Assert.Equal(32, protocol.AuthenticationTagLength);
    }
    
    [Fact]
    public void Encapsulate_ValidPeerKey_ReturnsKeyAgreementAndSecret()
    {
        using var protocol = new PinUvAuthProtocolV2();
        
        // Generate a peer key (simulating authenticator's key)
        using var peerEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var peerParams = peerEcdh.ExportParameters(includePrivateParameters: false);
        
        var peerCoseKey = new Dictionary<int, object?>
        {
            { 1, 2 },      // kty: EC2
            { -1, 1 },     // crv: P-256
            { -2, peerParams.Q.X },
            { -3, peerParams.Q.Y }
        };
        
        var (keyAgreement, sharedSecret) = protocol.Encapsulate(peerCoseKey);
        
        Assert.NotNull(keyAgreement);
        Assert.NotNull(sharedSecret);
        Assert.Equal(64, sharedSecret.Length); // HMAC key (32) + AES key (32)
        Assert.Equal(2, keyAgreement[1]);      // kty = EC2
        Assert.Equal(-25, keyAgreement[3]);    // alg = ECDH-ES+HKDF-256
        Assert.Equal(1, keyAgreement[-1]);     // crv = P-256
        Assert.NotNull(keyAgreement[-2]);      // X coordinate
        Assert.NotNull(keyAgreement[-3]);      // Y coordinate
    }
    
    [Fact]
    public void Encapsulate_NullPeerKey_ThrowsArgumentNullException()
    {
        using var protocol = new PinUvAuthProtocolV2();
        
        Assert.Throws<ArgumentNullException>(() => protocol.Encapsulate(null!));
    }
    
    [Fact]
    public void Encapsulate_MissingXCoordinate_ThrowsArgumentException()
    {
        using var protocol = new PinUvAuthProtocolV2();
        
        var peerCoseKey = new Dictionary<int, object?>
        {
            { 1, 2 },
            { -1, 1 },
            { -3, new byte[32] } // Only Y, no X
        };
        
        var ex = Assert.Throws<ArgumentException>(() => protocol.Encapsulate(peerCoseKey));
        Assert.Contains("X coordinate", ex.Message);
    }
    
    [Fact]
    public void Encapsulate_MissingYCoordinate_ThrowsArgumentException()
    {
        using var protocol = new PinUvAuthProtocolV2();
        
        var peerCoseKey = new Dictionary<int, object?>
        {
            { 1, 2 },
            { -1, 1 },
            { -2, new byte[32] } // Only X, no Y
        };
        
        var ex = Assert.Throws<ArgumentException>(() => protocol.Encapsulate(peerCoseKey));
        Assert.Contains("Y coordinate", ex.Message);
    }
    
    [Fact]
    public void Encapsulate_InvalidCoordinateLength_ThrowsArgumentException()
    {
        using var protocol = new PinUvAuthProtocolV2();
        
        var peerCoseKey = new Dictionary<int, object?>
        {
            { 1, 2 },
            { -1, 1 },
            { -2, new byte[16] }, // Wrong X length
            { -3, new byte[32] }
        };
        
        var ex = Assert.Throws<ArgumentException>(() => protocol.Encapsulate(peerCoseKey));
        Assert.Contains("X coordinate length", ex.Message);
    }
    
    [Fact]
    public void Kdf_ValidInput_Returns64Bytes()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var z = RandomNumberGenerator.GetBytes(32);
        
        var result = protocol.Kdf(z);
        
        Assert.Equal(64, result.Length);
    }
    
    [Fact]
    public void Kdf_EmptyInput_ThrowsArgumentException()
    {
        using var protocol = new PinUvAuthProtocolV2();
        
        var ex = Assert.Throws<ArgumentException>(() => protocol.Kdf([]));
        Assert.Contains("empty", ex.Message);
    }
    
    [Fact]
    public void Kdf_SameInput_ProducesSameOutput()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var z = RandomNumberGenerator.GetBytes(32);
        
        var result1 = protocol.Kdf(z);
        var result2 = protocol.Kdf(z);
        
        Assert.Equal(result1, result2);
    }
    
    [Fact]
    public void Encrypt_ValidInput_ReturnsIvPlusCiphertext()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(64);
        var plaintext = new byte[32]; // 2 blocks
        RandomNumberGenerator.Fill(plaintext);
        
        var ciphertext = protocol.Encrypt(key, plaintext);
        
        // Result should be IV (16) + ciphertext (32)
        Assert.Equal(48, ciphertext.Length);
    }
    
    [Fact]
    public void Encrypt_InvalidKeyLength_ThrowsArgumentException()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(32); // Should be 64
        var plaintext = new byte[16];
        
        var ex = Assert.Throws<ArgumentException>(() => protocol.Encrypt(key, plaintext));
        Assert.Contains("64 bytes", ex.Message);
    }
    
    [Fact]
    public void Encrypt_PlaintextNotMultipleOfBlockSize_ThrowsArgumentException()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(64);
        var plaintext = new byte[17]; // Not multiple of 16
        
        var ex = Assert.Throws<ArgumentException>(() => protocol.Encrypt(key, plaintext));
        Assert.Contains("multiple", ex.Message);
    }
    
    [Fact]
    public void Encrypt_EmptyPlaintext_ThrowsArgumentException()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(64);
        
        Assert.Throws<ArgumentException>(() => protocol.Encrypt(key, []));
    }
    
    [Fact]
    public void Decrypt_ValidInput_ReturnsPlaintext()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(64);
        var plaintext = new byte[32];
        RandomNumberGenerator.Fill(plaintext);
        
        var ciphertext = protocol.Encrypt(key, plaintext);
        var decrypted = protocol.Decrypt(key, ciphertext);
        
        Assert.Equal(plaintext, decrypted);
    }
    
    [Fact]
    public void Decrypt_InvalidKeyLength_ThrowsArgumentException()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(32);
        var ciphertext = new byte[32]; // IV + 1 block
        
        var ex = Assert.Throws<ArgumentException>(() => protocol.Decrypt(key, ciphertext));
        Assert.Contains("64 bytes", ex.Message);
    }
    
    [Fact]
    public void Decrypt_TooShortCiphertext_ThrowsArgumentException()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(64);
        var ciphertext = new byte[16]; // Only IV, no data
        
        var ex = Assert.Throws<ArgumentException>(() => protocol.Decrypt(key, ciphertext));
        Assert.Contains("at least", ex.Message);
    }
    
    [Fact]
    public void EncryptDecrypt_RoundTrip_ReturnsOriginal()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(64);
        var plaintext = new byte[64];
        RandomNumberGenerator.Fill(plaintext);
        
        var ciphertext = protocol.Encrypt(key, plaintext);
        var decrypted = protocol.Decrypt(key, ciphertext);
        
        Assert.Equal(plaintext, decrypted);
    }
    
    [Fact]
    public void EncryptDecrypt_MultipleBlocks_ReturnsOriginal()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(64);
        var plaintext = new byte[256]; // 16 blocks
        RandomNumberGenerator.Fill(plaintext);
        
        var ciphertext = protocol.Encrypt(key, plaintext);
        var decrypted = protocol.Decrypt(key, ciphertext);
        
        Assert.Equal(plaintext, decrypted);
    }
    
    [Fact]
    public void Authenticate_ValidInput_Returns32ByteMac()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(64);
        var message = "test message"u8.ToArray();
        
        var mac = protocol.Authenticate(key, message);
        
        Assert.Equal(32, mac.Length);
    }
    
    [Fact]
    public void Authenticate_InvalidKeyLength_ThrowsArgumentException()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(32);
        var message = "test"u8.ToArray();
        
        var ex = Assert.Throws<ArgumentException>(() => protocol.Authenticate(key, message));
        Assert.Contains("64 bytes", ex.Message);
    }
    
    [Fact]
    public void Authenticate_SameInputs_ProducesConsistentMac()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(64);
        var message = "test message"u8.ToArray();
        
        var mac1 = protocol.Authenticate(key, message);
        var mac2 = protocol.Authenticate(key, message);
        
        Assert.Equal(mac1, mac2);
    }
    
    [Fact]
    public void Authenticate_DifferentMessages_ProducesDifferentMacs()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(64);
        
        var mac1 = protocol.Authenticate(key, "message1"u8);
        var mac2 = protocol.Authenticate(key, "message2"u8);
        
        Assert.NotEqual(mac1, mac2);
    }
    
    [Fact]
    public void Verify_ValidSignature_ReturnsTrue()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(64);
        var message = "test message"u8.ToArray();
        
        var mac = protocol.Authenticate(key, message);
        var isValid = protocol.Verify(key, message, mac);
        
        Assert.True(isValid);
    }
    
    [Fact]
    public void Verify_InvalidSignature_ReturnsFalse()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(64);
        var message = "test message"u8.ToArray();
        var invalidMac = RandomNumberGenerator.GetBytes(32);
        
        var isValid = protocol.Verify(key, message, invalidMac);
        
        Assert.False(isValid);
    }
    
    [Fact]
    public void Verify_WrongSignatureLength_ReturnsFalse()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(64);
        var message = "test message"u8.ToArray();
        var wrongLengthMac = new byte[16]; // Should be 32
        
        var isValid = protocol.Verify(key, message, wrongLengthMac);
        
        Assert.False(isValid);
    }
    
    [Fact]
    public void Verify_TamperedMessage_ReturnsFalse()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(64);
        var message = "test message"u8.ToArray();
        var mac = protocol.Authenticate(key, message);
        
        // Tamper with message
        var tamperedMessage = "test massage"u8.ToArray();
        var isValid = protocol.Verify(key, tamperedMessage, mac);
        
        Assert.False(isValid);
    }
    
    [Fact]
    public void DisposedProtocol_ThrowsObjectDisposedException()
    {
        var protocol = new PinUvAuthProtocolV2();
        protocol.Dispose();
        
        Assert.Throws<ObjectDisposedException>(() => protocol.Kdf(new byte[32]));
        Assert.Throws<ObjectDisposedException>(() => protocol.Encrypt(new byte[64], new byte[16]));
        Assert.Throws<ObjectDisposedException>(() => protocol.Decrypt(new byte[64], new byte[32]));
        Assert.Throws<ObjectDisposedException>(() => protocol.Authenticate(new byte[64], new byte[16]));
        Assert.Throws<ObjectDisposedException>(() => protocol.Verify(new byte[64], new byte[16], new byte[32]));
    }
    
    [Fact]
    public void Encapsulate_ReturnsUniqueKeyAgreementEachCall()
    {
        using var protocol = new PinUvAuthProtocolV2();
        
        using var peerEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var peerParams = peerEcdh.ExportParameters(includePrivateParameters: false);
        
        var peerCoseKey = new Dictionary<int, object?>
        {
            { 1, 2 },
            { -1, 1 },
            { -2, peerParams.Q.X },
            { -3, peerParams.Q.Y }
        };
        
        var (keyAgreement1, _) = protocol.Encapsulate(peerCoseKey);
        var (keyAgreement2, _) = protocol.Encapsulate(peerCoseKey);
        
        // Each call should generate a new ephemeral key
        Assert.NotEqual(keyAgreement1[-2], keyAgreement2[-2]);
        Assert.NotEqual(keyAgreement1[-3], keyAgreement2[-3]);
    }
}
