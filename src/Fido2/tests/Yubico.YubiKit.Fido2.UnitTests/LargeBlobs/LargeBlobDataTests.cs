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
using Yubico.YubiKit.Fido2.LargeBlobs;

namespace Yubico.YubiKit.Fido2.UnitTests.LargeBlobs;

/// <summary>
/// Unit tests for <see cref="LargeBlobEntry"/> and <see cref="LargeBlobArray"/>.
/// </summary>
public class LargeBlobDataTests
{
    private static byte[] CreateRandomKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return key;
    }
    
    #region LargeBlobEntry Tests
    
    [Fact]
    public void LargeBlobEntry_Encrypt_CreatesValidEntry()
    {
        // Arrange
        var key = CreateRandomKey();
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        
        // Act
        var entry = LargeBlobEntry.Encrypt(key, data);
        
        // Assert
        Assert.NotNull(entry);
        Assert.True(entry.EncryptedData.Length > data.Length); // Includes nonce + tag
    }
    
    [Fact]
    public void LargeBlobEntry_TryDecrypt_ReturnsOriginalData()
    {
        // Arrange
        var key = CreateRandomKey();
        var originalData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var entry = LargeBlobEntry.Encrypt(key, originalData);
        
        // Act
        var decrypted = entry.TryDecrypt(key);
        
        // Assert
        Assert.NotNull(decrypted);
        Assert.Equal(originalData, decrypted);
    }
    
    [Fact]
    public void LargeBlobEntry_TryDecrypt_WithWrongKey_ReturnsNull()
    {
        // Arrange
        var correctKey = CreateRandomKey();
        var wrongKey = CreateRandomKey();
        var data = new byte[] { 0x01, 0x02, 0x03 };
        var entry = LargeBlobEntry.Encrypt(correctKey, data);
        
        // Act
        var result = entry.TryDecrypt(wrongKey);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void LargeBlobEntry_TryDecrypt_WithTruncatedData_ReturnsNull()
    {
        // Arrange
        var key = CreateRandomKey();
        var entry = new LargeBlobEntry { EncryptedData = new byte[10] }; // Too short
        
        // Act
        var result = entry.TryDecrypt(key);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void LargeBlobEntry_TryDecrypt_ThrowsOnInvalidKeyLength()
    {
        // Arrange
        var invalidKey = new byte[16]; // Should be 32
        var entry = new LargeBlobEntry { EncryptedData = new byte[50] };
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => entry.TryDecrypt(invalidKey));
    }
    
    [Fact]
    public void LargeBlobEntry_Encrypt_ThrowsOnInvalidKeyLength()
    {
        // Arrange
        var invalidKey = new byte[16]; // Should be 32
        var data = new byte[] { 0x01, 0x02 };
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => LargeBlobEntry.Encrypt(invalidKey, data));
    }
    
    [Fact]
    public void LargeBlobEntry_RoundTrip_EmptyData()
    {
        // Arrange
        var key = CreateRandomKey();
        var emptyData = Array.Empty<byte>();
        
        // Act
        var entry = LargeBlobEntry.Encrypt(key, emptyData);
        var decrypted = entry.TryDecrypt(key);
        
        // Assert
        Assert.NotNull(decrypted);
        Assert.Empty(decrypted);
    }
    
    [Fact]
    public void LargeBlobEntry_RoundTrip_LargeData()
    {
        // Arrange
        var key = CreateRandomKey();
        var largeData = new byte[4096];
        RandomNumberGenerator.Fill(largeData);
        
        // Act
        var entry = LargeBlobEntry.Encrypt(key, largeData);
        var decrypted = entry.TryDecrypt(key);
        
        // Assert
        Assert.NotNull(decrypted);
        Assert.Equal(largeData, decrypted);
    }
    
    #endregion
    
    #region LargeBlobArray Tests
    
    [Fact]
    public void LargeBlobArray_CreateEmpty_ReturnsEmptyArray()
    {
        // Act
        var array = LargeBlobArray.CreateEmpty();
        
        // Assert
        Assert.NotNull(array);
        Assert.Empty(array.Entries);
    }
    
    [Fact]
    public void LargeBlobArray_Serialize_EmptyArray()
    {
        // Arrange
        var array = LargeBlobArray.CreateEmpty();
        
        // Act
        var serialized = array.Serialize();
        
        // Assert
        Assert.NotNull(serialized);
        Assert.True(serialized.Length > 16); // At least hash size
    }
    
    [Fact]
    public void LargeBlobArray_SerializeAndDeserialize_EmptyArray()
    {
        // Arrange
        var original = LargeBlobArray.CreateEmpty();
        
        // Act
        var serialized = original.Serialize();
        var deserialized = LargeBlobArray.Deserialize(serialized);
        
        // Assert
        Assert.NotNull(deserialized);
        Assert.Empty(deserialized.Entries);
    }
    
    [Fact]
    public void LargeBlobArray_SerializeAndDeserialize_WithEntries()
    {
        // Arrange
        var key1 = CreateRandomKey();
        var key2 = CreateRandomKey();
        var data1 = new byte[] { 0x01, 0x02, 0x03 };
        var data2 = new byte[] { 0x04, 0x05, 0x06, 0x07 };
        
        var entry1 = LargeBlobEntry.Encrypt(key1, data1);
        var entry2 = LargeBlobEntry.Encrypt(key2, data2);
        
        var original = new LargeBlobArray { Entries = [entry1, entry2] };
        
        // Act
        var serialized = original.Serialize();
        var deserialized = LargeBlobArray.Deserialize(serialized);
        
        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Entries.Count);
        
        // Verify we can decrypt the entries
        Assert.Equal(data1, deserialized.Entries[0].TryDecrypt(key1));
        Assert.Equal(data2, deserialized.Entries[1].TryDecrypt(key2));
    }
    
    [Fact]
    public void LargeBlobArray_Deserialize_ThrowsOnTamperedHash()
    {
        // Arrange
        var array = LargeBlobArray.CreateEmpty();
        var serialized = array.Serialize();
        
        // Tamper with the hash (last 16 bytes)
        serialized[^1] ^= 0xFF;
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => LargeBlobArray.Deserialize(serialized));
    }
    
    [Fact]
    public void LargeBlobArray_Deserialize_ThrowsOnTooShortData()
    {
        // Arrange
        var tooShort = new byte[10]; // Less than minimum
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => LargeBlobArray.Deserialize(tooShort));
    }
    
    [Fact]
    public void LargeBlobArray_WithEntry_AddsEntry()
    {
        // Arrange
        var array = LargeBlobArray.CreateEmpty();
        var key = CreateRandomKey();
        var entry = LargeBlobEntry.Encrypt(key, new byte[] { 0x01 });
        
        // Act
        var newArray = array.WithEntry(entry);
        
        // Assert
        Assert.Empty(array.Entries); // Original unchanged
        Assert.Single(newArray.Entries);
    }
    
    [Fact]
    public void LargeBlobArray_WithoutEntry_RemovesEntry()
    {
        // Arrange
        var key = CreateRandomKey();
        var entry = LargeBlobEntry.Encrypt(key, new byte[] { 0x01 });
        var array = new LargeBlobArray { Entries = [entry] };
        
        // Act
        var newArray = array.WithoutEntry(0);
        
        // Assert
        Assert.Single(array.Entries); // Original unchanged
        Assert.Empty(newArray.Entries);
    }
    
    [Fact]
    public void LargeBlobArray_WithoutEntry_ThrowsOnInvalidIndex()
    {
        // Arrange
        var array = LargeBlobArray.CreateEmpty();
        
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => array.WithoutEntry(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.WithoutEntry(-1));
    }
    
    [Fact]
    public void LargeBlobArray_FindAndDecrypt_ReturnsData()
    {
        // Arrange
        var key = CreateRandomKey();
        var data = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE };
        var entry = LargeBlobEntry.Encrypt(key, data);
        var array = new LargeBlobArray { Entries = [entry] };
        
        // Act
        var result = array.FindAndDecrypt(key);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(data, result);
    }
    
    [Fact]
    public void LargeBlobArray_FindAndDecrypt_ReturnsNullForUnknownKey()
    {
        // Arrange
        var key1 = CreateRandomKey();
        var key2 = CreateRandomKey();
        var entry = LargeBlobEntry.Encrypt(key1, new byte[] { 0x01 });
        var array = new LargeBlobArray { Entries = [entry] };
        
        // Act
        var result = array.FindAndDecrypt(key2);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void LargeBlobArray_FindAndDecrypt_FindsCorrectEntryAmongMany()
    {
        // Arrange
        var key1 = CreateRandomKey();
        var key2 = CreateRandomKey();
        var key3 = CreateRandomKey();
        var data1 = new byte[] { 0x01 };
        var data2 = new byte[] { 0x02 };
        var data3 = new byte[] { 0x03 };
        
        var array = new LargeBlobArray
        {
            Entries =
            [
                LargeBlobEntry.Encrypt(key1, data1),
                LargeBlobEntry.Encrypt(key2, data2),
                LargeBlobEntry.Encrypt(key3, data3)
            ]
        };
        
        // Act & Assert
        Assert.Equal(data1, array.FindAndDecrypt(key1));
        Assert.Equal(data2, array.FindAndDecrypt(key2));
        Assert.Equal(data3, array.FindAndDecrypt(key3));
    }
    
    #endregion
}
