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
using System.Security.Cryptography;

namespace Yubico.YubiKit.Fido2.LargeBlobs;

/// <summary>
/// Represents a single entry in the large blob array.
/// </summary>
/// <remarks>
/// <para>
/// Each entry is encrypted with AES-GCM using a credential's largeBlobKey.
/// The plaintext is a CBOR map containing the data and optionally the origSize.
/// </para>
/// <para>
/// Structure: nonce (12 bytes) || AES-GCM ciphertext || AES-GCM tag (16 bytes)
/// </para>
/// </remarks>
public sealed class LargeBlobEntry
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    
    /// <summary>
    /// Gets the encrypted blob entry data.
    /// </summary>
    public ReadOnlyMemory<byte> EncryptedData { get; init; }
    
    /// <summary>
    /// Attempts to decrypt this entry using the provided large blob key.
    /// </summary>
    /// <param name="largeBlobKey">The 32-byte large blob key from a credential.</param>
    /// <returns>The decrypted blob data, or null if decryption fails.</returns>
    /// <remarks>
    /// <para>
    /// Decryption uses AES-256-GCM with:
    /// - Key: largeBlobKey (32 bytes)
    /// - Nonce: first 12 bytes of encrypted data
    /// - Ciphertext: middle bytes
    /// - Tag: last 16 bytes
    /// - AAD: empty
    /// </para>
    /// </remarks>
    public byte[]? TryDecrypt(ReadOnlySpan<byte> largeBlobKey)
    {
        if (largeBlobKey.Length != 32)
        {
            throw new ArgumentException("Large blob key must be 32 bytes.", nameof(largeBlobKey));
        }
        
        var encryptedSpan = EncryptedData.Span;
        if (encryptedSpan.Length < NonceSize + TagSize)
        {
            return null;
        }
        
        var nonce = encryptedSpan[..NonceSize];
        var ciphertext = encryptedSpan[NonceSize..^TagSize];
        var tag = encryptedSpan[^TagSize..];
        
        try
        {
            using var aesGcm = new AesGcm(largeBlobKey, TagSize);
            var plaintext = new byte[ciphertext.Length];
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
            
            // Parse the CBOR plaintext to extract the actual blob data
            return ParseDecryptedBlob(plaintext);
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    /// <summary>
    /// Creates an encrypted large blob entry from plaintext data.
    /// </summary>
    /// <param name="largeBlobKey">The 32-byte large blob key from a credential.</param>
    /// <param name="data">The data to store in the blob.</param>
    /// <returns>The encrypted entry ready for storage.</returns>
    public static LargeBlobEntry Encrypt(ReadOnlySpan<byte> largeBlobKey, ReadOnlySpan<byte> data)
    {
        if (largeBlobKey.Length != 32)
        {
            throw new ArgumentException("Large blob key must be 32 bytes.", nameof(largeBlobKey));
        }
        
        // Create the CBOR plaintext: { data, origSize (optional if compressed) }
        var plaintext = CreatePlaintext(data);
        
        // Generate random nonce
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);
        
        // Encrypt with AES-GCM
        using var aesGcm = new AesGcm(largeBlobKey, TagSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        
        // Combine: nonce || ciphertext || tag
        var encrypted = new byte[NonceSize + ciphertext.Length + TagSize];
        nonce.CopyTo(encrypted, 0);
        ciphertext.CopyTo(encrypted.AsSpan(NonceSize));
        tag.CopyTo(encrypted.AsSpan(NonceSize + ciphertext.Length));
        
        return new LargeBlobEntry { EncryptedData = encrypted };
    }
    
    private static byte[] CreatePlaintext(ReadOnlySpan<byte> data)
    {
        // CBOR map with data field
        // We don't compress, so no origSize needed
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteByteString(ReadOnlySpan<byte>.Empty); // Key "" (empty string as bytes for canonical ordering)
        writer.WriteByteString(data);
        writer.WriteEndMap();
        return writer.Encode();
    }
    
    private static byte[]? ParseDecryptedBlob(ReadOnlySpan<byte> plaintext)
    {
        try
        {
            var reader = new CborReader(plaintext.ToArray(), CborConformanceMode.Ctap2Canonical);
            var mapCount = reader.ReadStartMap() ?? 0;
            
            byte[]? data = null;
            
            for (var i = 0; i < mapCount; i++)
            {
                var key = reader.PeekState() == CborReaderState.ByteString
                    ? reader.ReadByteString()
                    : null;
                
                if (key is { Length: 0 })
                {
                    // Empty byte string key = data field
                    data = reader.ReadByteString();
                }
                else
                {
                    reader.SkipValue();
                    reader.SkipValue();
                }
            }
            
            reader.ReadEndMap();
            return data;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Represents the large blob array stored on the authenticator.
/// </summary>
/// <remarks>
/// <para>
/// The large blob array is a CBOR array of encrypted entries, followed by
/// a 16-byte truncated SHA-256 hash of the serialized array (excluding the hash itself).
/// </para>
/// <para>
/// Format: CBOR array || LEFT(SHA-256(CBOR array), 16)
/// </para>
/// </remarks>
public sealed class LargeBlobArray
{
    private const int HashSize = 16;
    
    /// <summary>
    /// Gets the entries in the large blob array.
    /// </summary>
    public IReadOnlyList<LargeBlobEntry> Entries { get; init; } = [];
    
    /// <summary>
    /// Deserializes a large blob array from raw bytes.
    /// </summary>
    /// <param name="data">The serialized large blob array including the hash.</param>
    /// <returns>The parsed array.</returns>
    /// <exception cref="ArgumentException">Thrown when the data is invalid or the hash doesn't match.</exception>
    public static LargeBlobArray Deserialize(ReadOnlyMemory<byte> data)
    {
        if (data.Length < HashSize + 2) // Minimum: 2-byte empty CBOR array + 16-byte hash
        {
            throw new ArgumentException("Data too short for valid large blob array.", nameof(data));
        }
        
        var span = data.Span;
        var arrayData = span[..^HashSize];
        var expectedHash = span[^HashSize..];
        
        // Verify hash
        Span<byte> computedHash = stackalloc byte[32];
        SHA256.HashData(arrayData, computedHash);
        
        if (!computedHash[..HashSize].SequenceEqual(expectedHash))
        {
            throw new ArgumentException("Large blob array hash verification failed.", nameof(data));
        }
        
        // Parse CBOR array
        var entries = new List<LargeBlobEntry>();
        var reader = new CborReader(arrayData.ToArray(), CborConformanceMode.Ctap2Canonical);
        
        var arrayLength = reader.ReadStartArray() ?? 0;
        for (var i = 0; i < arrayLength; i++)
        {
            var entryData = reader.ReadByteString();
            entries.Add(new LargeBlobEntry { EncryptedData = entryData });
        }
        reader.ReadEndArray();
        
        return new LargeBlobArray { Entries = entries };
    }
    
    /// <summary>
    /// Serializes this large blob array to bytes including the hash.
    /// </summary>
    /// <returns>The serialized array ready for storage.</returns>
    public byte[] Serialize()
    {
        // Encode CBOR array
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartArray(Entries.Count);
        
        foreach (var entry in Entries)
        {
            writer.WriteByteString(entry.EncryptedData.Span);
        }
        
        writer.WriteEndArray();
        var arrayData = writer.Encode();
        
        // Compute hash
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(arrayData, hash);
        
        // Combine: array || LEFT(hash, 16)
        var result = new byte[arrayData.Length + HashSize];
        arrayData.CopyTo(result, 0);
        hash[..HashSize].CopyTo(result.AsSpan(arrayData.Length));
        
        return result;
    }
    
    /// <summary>
    /// Creates an empty large blob array.
    /// </summary>
    /// <returns>A new empty array.</returns>
    public static LargeBlobArray CreateEmpty() => new() { Entries = [] };
    
    /// <summary>
    /// Creates a new array with an additional entry.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <returns>A new array with the entry added.</returns>
    public LargeBlobArray WithEntry(LargeBlobEntry entry)
    {
        var newEntries = new List<LargeBlobEntry>(Entries) { entry };
        return new LargeBlobArray { Entries = newEntries };
    }
    
    /// <summary>
    /// Creates a new array with the specified entry removed.
    /// </summary>
    /// <param name="index">The index of the entry to remove.</param>
    /// <returns>A new array without the entry.</returns>
    public LargeBlobArray WithoutEntry(int index)
    {
        if (index < 0 || index >= Entries.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        
        var newEntries = new List<LargeBlobEntry>(Entries);
        newEntries.RemoveAt(index);
        return new LargeBlobArray { Entries = newEntries };
    }
    
    /// <summary>
    /// Finds and decrypts the blob for the given large blob key.
    /// </summary>
    /// <param name="largeBlobKey">The 32-byte key to try.</param>
    /// <returns>The decrypted data, or null if no matching entry was found.</returns>
    public byte[]? FindAndDecrypt(ReadOnlySpan<byte> largeBlobKey)
    {
        foreach (var entry in Entries)
        {
            var decrypted = entry.TryDecrypt(largeBlobKey);
            if (decrypted is not null)
            {
                return decrypted;
            }
        }
        
        return null;
    }
}
