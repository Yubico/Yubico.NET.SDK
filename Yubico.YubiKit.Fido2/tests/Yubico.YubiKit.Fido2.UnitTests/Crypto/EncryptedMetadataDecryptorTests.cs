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
using Xunit;
using Yubico.YubiKit.Fido2.Crypto;

namespace Yubico.YubiKit.Fido2.UnitTests.Crypto;

/// <summary>
/// Unit tests for <see cref="EncryptedMetadataDecryptor"/>.
/// </summary>
public class EncryptedMetadataDecryptorTests
{
    private static byte[] CreateRandomPpuat()
    {
        var ppuat = new byte[32];
        RandomNumberGenerator.Fill(ppuat);
        return ppuat;
    }
    
    #region DeriveKey Tests
    
    [Fact]
    public void DeriveKey_Returns16ByteKey()
    {
        // Arrange
        var ppuat = CreateRandomPpuat();
        var info = "test"u8;
        
        // Act
        var key = EncryptedMetadataDecryptor.DeriveKey(ppuat, info);
        
        // Assert
        Assert.NotNull(key);
        Assert.Equal(16, key.Length);
    }
    
    [Fact]
    public void DeriveKey_SamePpuatAndInfo_ReturnsSameKey()
    {
        // Arrange
        var ppuat = CreateRandomPpuat();
        var info = "encIdentifier"u8;
        
        // Act
        var key1 = EncryptedMetadataDecryptor.DeriveKey(ppuat, info);
        var key2 = EncryptedMetadataDecryptor.DeriveKey(ppuat, info);
        
        // Assert
        Assert.Equal(key1, key2);
    }
    
    [Fact]
    public void DeriveKey_DifferentInfo_ReturnsDifferentKeys()
    {
        // Arrange
        var ppuat = CreateRandomPpuat();
        
        // Act
        var key1 = EncryptedMetadataDecryptor.DeriveKey(ppuat, "encIdentifier"u8);
        var key2 = EncryptedMetadataDecryptor.DeriveKey(ppuat, "encCredStoreState"u8);
        
        // Assert
        Assert.NotEqual(key1, key2);
    }
    
    [Fact]
    public void DeriveKey_DifferentPpuat_ReturnsDifferentKeys()
    {
        // Arrange
        var ppuat1 = CreateRandomPpuat();
        var ppuat2 = CreateRandomPpuat();
        var info = "encIdentifier"u8;
        
        // Act
        var key1 = EncryptedMetadataDecryptor.DeriveKey(ppuat1, info);
        var key2 = EncryptedMetadataDecryptor.DeriveKey(ppuat2, info);
        
        // Assert
        Assert.NotEqual(key1, key2);
    }
    
    [Fact]
    public void DeriveKey_ThrowsOnEmptyPpuat()
    {
        Assert.Throws<ArgumentException>(
            () => EncryptedMetadataDecryptor.DeriveKey(ReadOnlySpan<byte>.Empty, "test"u8));
    }
    
    #endregion
    
    #region DecryptIdentifier Tests
    
    [Fact]
    public void DecryptIdentifier_EmptyPpuat_ReturnsNull()
    {
        // Arrange
        var ciphertext = new byte[16];
        
        // Act
        var result = EncryptedMetadataDecryptor.DecryptIdentifier(
            ReadOnlySpan<byte>.Empty, ciphertext);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void DecryptIdentifier_EmptyCiphertext_ReturnsNull()
    {
        // Arrange
        var ppuat = CreateRandomPpuat();
        
        // Act
        var result = EncryptedMetadataDecryptor.DecryptIdentifier(
            ppuat, ReadOnlySpan<byte>.Empty);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void DecryptIdentifier_ValidInput_ReturnsDecryptedData()
    {
        // Arrange
        var ppuat = CreateRandomPpuat();
        var plaintext = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                                      0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };
        
        // Encrypt first
        var key = EncryptedMetadataDecryptor.DeriveKey(ppuat, "encIdentifier"u8);
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        var ciphertext = aes.EncryptEcb(plaintext, PaddingMode.None);
        
        // Act
        var decrypted = EncryptedMetadataDecryptor.DecryptIdentifier(ppuat, ciphertext);
        
        // Assert
        Assert.NotNull(decrypted);
        Assert.Equal(plaintext, decrypted);
    }
    
    [Fact]
    public void DecryptIdentifier_WrongPpuat_ReturnsWrongData()
    {
        // Arrange
        var correctPpuat = CreateRandomPpuat();
        var wrongPpuat = CreateRandomPpuat();
        var plaintext = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                                      0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };
        
        // Encrypt with correct PPUAT
        var key = EncryptedMetadataDecryptor.DeriveKey(correctPpuat, "encIdentifier"u8);
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        var ciphertext = aes.EncryptEcb(plaintext, PaddingMode.None);
        
        // Act - decrypt with wrong PPUAT
        var decrypted = EncryptedMetadataDecryptor.DecryptIdentifier(wrongPpuat, ciphertext);
        
        // Assert - decryption succeeds but result is garbage
        Assert.NotNull(decrypted);
        Assert.NotEqual(plaintext, decrypted);
    }
    
    #endregion
    
    #region DecryptCredStoreState Tests
    
    [Fact]
    public void DecryptCredStoreState_EmptyPpuat_ReturnsNull()
    {
        // Arrange
        var ciphertext = new byte[16];
        
        // Act
        var result = EncryptedMetadataDecryptor.DecryptCredStoreState(
            ReadOnlySpan<byte>.Empty, ciphertext);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void DecryptCredStoreState_EmptyCiphertext_ReturnsNull()
    {
        // Arrange
        var ppuat = CreateRandomPpuat();
        
        // Act
        var result = EncryptedMetadataDecryptor.DecryptCredStoreState(
            ppuat, ReadOnlySpan<byte>.Empty);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void DecryptCredStoreState_ValidInput_ReturnsDecryptedData()
    {
        // Arrange
        var ppuat = CreateRandomPpuat();
        var plaintext = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE, 0xDE, 0xAD, 0xBE, 0xEF,
                                      0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        
        // Encrypt first
        var key = EncryptedMetadataDecryptor.DeriveKey(ppuat, "encCredStoreState"u8);
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        var ciphertext = aes.EncryptEcb(plaintext, PaddingMode.None);
        
        // Act
        var decrypted = EncryptedMetadataDecryptor.DecryptCredStoreState(ppuat, ciphertext);
        
        // Assert
        Assert.NotNull(decrypted);
        Assert.Equal(plaintext, decrypted);
    }
    
    [Fact]
    public void DecryptCredStoreState_DifferentFromIdentifier()
    {
        // Arrange - using same PPUAT and plaintext
        var ppuat = CreateRandomPpuat();
        var plaintext = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                                      0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };
        
        // Encrypt as identifier
        var identifierKey = EncryptedMetadataDecryptor.DeriveKey(ppuat, "encIdentifier"u8);
        using var aes1 = Aes.Create();
        aes1.Key = identifierKey;
        aes1.Mode = CipherMode.ECB;
        aes1.Padding = PaddingMode.None;
        var identifierCiphertext = aes1.EncryptEcb(plaintext, PaddingMode.None);
        
        // Encrypt as credStoreState
        var stateKey = EncryptedMetadataDecryptor.DeriveKey(ppuat, "encCredStoreState"u8);
        using var aes2 = Aes.Create();
        aes2.Key = stateKey;
        aes2.Mode = CipherMode.ECB;
        aes2.Padding = PaddingMode.None;
        var stateCiphertext = aes2.EncryptEcb(plaintext, PaddingMode.None);
        
        // Assert - different ciphertexts due to different derived keys
        Assert.NotEqual(identifierCiphertext, stateCiphertext);
        
        // Verify each decrypts correctly with its own method
        var decryptedIdentifier = EncryptedMetadataDecryptor.DecryptIdentifier(ppuat, identifierCiphertext);
        var decryptedState = EncryptedMetadataDecryptor.DecryptCredStoreState(ppuat, stateCiphertext);
        
        Assert.Equal(plaintext, decryptedIdentifier);
        Assert.Equal(plaintext, decryptedState);
    }
    
    #endregion
    
    #region RoundTrip Tests
    
    [Fact]
    public void RoundTrip_MultipleBlockSize()
    {
        // Test with 32-byte plaintext (2 AES blocks)
        var ppuat = CreateRandomPpuat();
        var plaintext = new byte[32];
        RandomNumberGenerator.Fill(plaintext);
        
        // Encrypt
        var key = EncryptedMetadataDecryptor.DeriveKey(ppuat, "encIdentifier"u8);
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        var ciphertext = aes.EncryptEcb(plaintext, PaddingMode.None);
        
        // Decrypt
        var decrypted = EncryptedMetadataDecryptor.DecryptIdentifier(ppuat, ciphertext);
        
        // Assert
        Assert.NotNull(decrypted);
        Assert.Equal(plaintext, decrypted);
    }
    
    [Fact]
    public void RoundTrip_LargePpuat()
    {
        // Test with larger PPUAT (64 bytes)
        var ppuat = new byte[64];
        RandomNumberGenerator.Fill(ppuat);
        var plaintext = new byte[16];
        RandomNumberGenerator.Fill(plaintext);
        
        // Encrypt
        var key = EncryptedMetadataDecryptor.DeriveKey(ppuat, "encCredStoreState"u8);
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        var ciphertext = aes.EncryptEcb(plaintext, PaddingMode.None);
        
        // Decrypt
        var decrypted = EncryptedMetadataDecryptor.DecryptCredStoreState(ppuat, ciphertext);
        
        // Assert
        Assert.NotNull(decrypted);
        Assert.Equal(plaintext, decrypted);
    }
    
    #endregion
}
