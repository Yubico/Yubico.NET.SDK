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
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.LargeBlobs;

/// <summary>
/// Provides operations for reading and writing large blobs on FIDO2 authenticators.
/// </summary>
/// <remarks>
/// <para>
/// The authenticatorLargeBlobs command allows storing arbitrary data associated with
/// discoverable credentials. Data is encrypted using the credential's largeBlobKey,
/// which is derived from the credential private key.
/// </para>
/// <para>
/// Large blobs are stored in a single array on the authenticator. Reading is always
/// allowed; writing requires PIN/UV authentication with the LargeBlobWrite permission.
/// </para>
/// <para>
/// For credentials created with the largeBlob extension, use
/// <see cref="GetBlobAsync(ReadOnlyMemory{byte}, CancellationToken)"/> and
/// <see cref="SetBlobAsync(ReadOnlyMemory{byte}, ReadOnlyMemory{byte}, CancellationToken)"/>
/// to read/write blobs for specific credentials.
/// </para>
/// </remarks>
public sealed class LargeBlobStorage
{
    // CBOR map keys for authenticatorLargeBlobs
    private const int KeyGet = 0x01;
    private const int KeySet = 0x02;
    private const int KeyOffset = 0x03;
    private const int KeyLength = 0x04;
    private const int KeyPinUvAuthParam = 0x05;
    private const int KeyPinUvAuthProtocol = 0x06;
    
    // Response keys
    private const int KeyConfig = 0x01;
    
    // Hash size at the end of serialized array
    private const int ArrayHashSize = 16;
    
    private readonly FidoSession _session;
    private readonly IPinUvAuthProtocol? _protocol;
    private readonly ReadOnlyMemory<byte> _pinUvAuthToken;
    private readonly int _maxFragmentLength;
    
    /// <summary>
    /// Initializes a new instance for read-only operations.
    /// </summary>
    /// <param name="session">The FIDO session to use for communication.</param>
    /// <param name="maxFragmentLength">Maximum bytes to read/write per request.</param>
    /// <remarks>
    /// Use this constructor when only reading large blobs.
    /// Writing requires PIN/UV authentication.
    /// </remarks>
    public LargeBlobStorage(FidoSession session, int maxFragmentLength = 1024)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _maxFragmentLength = maxFragmentLength > 0 
            ? maxFragmentLength 
            : throw new ArgumentOutOfRangeException(nameof(maxFragmentLength));
    }
    
    /// <summary>
    /// Initializes a new instance for read and write operations.
    /// </summary>
    /// <param name="session">The FIDO session to use for communication.</param>
    /// <param name="protocol">The PIN/UV auth protocol to use for writes.</param>
    /// <param name="pinUvAuthToken">The PIN/UV auth token with LargeBlobWrite permission.</param>
    /// <param name="maxFragmentLength">Maximum bytes to read/write per request.</param>
    /// <remarks>
    /// The PIN/UV auth token must have the
    /// <see cref="PinUvAuthTokenPermissions.LargeBlobWrite"/> permission.
    /// </remarks>
    public LargeBlobStorage(
        FidoSession session,
        IPinUvAuthProtocol protocol,
        ReadOnlyMemory<byte> pinUvAuthToken,
        int maxFragmentLength = 1024)
        : this(session, maxFragmentLength)
    {
        _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
        _pinUvAuthToken = pinUvAuthToken;
    }
    
    /// <summary>
    /// Reads the entire large blob array from the authenticator.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The large blob array.</returns>
    /// <exception cref="CtapException">Thrown when the operation fails.</exception>
    public async Task<LargeBlobArray> ReadLargeBlobArrayAsync(CancellationToken cancellationToken = default)
    {
        var data = await ReadRawBlobAsync(cancellationToken).ConfigureAwait(false);
        
        if (data.Length == 0)
        {
            return LargeBlobArray.CreateEmpty();
        }
        
        return LargeBlobArray.Deserialize(data);
    }
    
    /// <summary>
    /// Writes the entire large blob array to the authenticator.
    /// </summary>
    /// <param name="array">The array to write.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when PIN/UV auth is not configured.</exception>
    /// <exception cref="CtapException">Thrown when the operation fails.</exception>
    public async Task WriteLargeBlobArrayAsync(
        LargeBlobArray array,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(array);
        
        if (_protocol is null)
        {
            throw new InvalidOperationException(
                "PIN/UV authentication is required for writing large blobs. " +
                "Use the constructor that accepts a protocol and token.");
        }
        
        var serialized = array.Serialize();
        await WriteRawBlobAsync(serialized, cancellationToken).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Gets the blob data for a specific credential.
    /// </summary>
    /// <param name="largeBlobKey">The credential's largeBlobKey (32 bytes).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The decrypted blob data, or null if no blob exists for this credential.</returns>
    /// <exception cref="ArgumentException">Thrown when largeBlobKey is not 32 bytes.</exception>
    /// <exception cref="CtapException">Thrown when reading fails.</exception>
    public async Task<byte[]?> GetBlobAsync(
        ReadOnlyMemory<byte> largeBlobKey,
        CancellationToken cancellationToken = default)
    {
        if (largeBlobKey.Length != 32)
        {
            throw new ArgumentException("Large blob key must be 32 bytes.", nameof(largeBlobKey));
        }
        
        var array = await ReadLargeBlobArrayAsync(cancellationToken).ConfigureAwait(false);
        return array.FindAndDecrypt(largeBlobKey.Span);
    }
    
    /// <summary>
    /// Sets the blob data for a specific credential.
    /// </summary>
    /// <param name="largeBlobKey">The credential's largeBlobKey (32 bytes).</param>
    /// <param name="data">The data to store.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// <para>
    /// This operation replaces any existing blob for this credential.
    /// It reads the current array, removes any existing entry for this key,
    /// adds the new encrypted entry, and writes the array back.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when largeBlobKey is not 32 bytes.</exception>
    /// <exception cref="InvalidOperationException">Thrown when PIN/UV auth is not configured.</exception>
    /// <exception cref="CtapException">Thrown when the operation fails.</exception>
    public async Task SetBlobAsync(
        ReadOnlyMemory<byte> largeBlobKey,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        if (largeBlobKey.Length != 32)
        {
            throw new ArgumentException("Large blob key must be 32 bytes.", nameof(largeBlobKey));
        }
        
        // Read current array
        var array = await ReadLargeBlobArrayAsync(cancellationToken).ConfigureAwait(false);
        
        // Remove existing entry for this key (if any)
        var newEntries = new List<LargeBlobEntry>();
        foreach (var entry in array.Entries)
        {
            if (entry.TryDecrypt(largeBlobKey.Span) is null)
            {
                // Keep entries that don't decrypt with this key
                newEntries.Add(entry);
            }
        }
        
        // Add new encrypted entry
        var newEntry = LargeBlobEntry.Encrypt(largeBlobKey.Span, data.Span);
        newEntries.Add(newEntry);
        
        // Write back
        var newArray = new LargeBlobArray { Entries = newEntries };
        await WriteLargeBlobArrayAsync(newArray, cancellationToken).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Deletes the blob data for a specific credential.
    /// </summary>
    /// <param name="largeBlobKey">The credential's largeBlobKey (32 bytes).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if a blob was deleted, false if no blob existed for this key.</returns>
    /// <exception cref="ArgumentException">Thrown when largeBlobKey is not 32 bytes.</exception>
    /// <exception cref="InvalidOperationException">Thrown when PIN/UV auth is not configured.</exception>
    /// <exception cref="CtapException">Thrown when the operation fails.</exception>
    public async Task<bool> DeleteBlobAsync(
        ReadOnlyMemory<byte> largeBlobKey,
        CancellationToken cancellationToken = default)
    {
        if (largeBlobKey.Length != 32)
        {
            throw new ArgumentException("Large blob key must be 32 bytes.", nameof(largeBlobKey));
        }
        
        // Read current array
        var array = await ReadLargeBlobArrayAsync(cancellationToken).ConfigureAwait(false);
        
        // Find and remove entry for this key
        var newEntries = new List<LargeBlobEntry>();
        var found = false;
        
        foreach (var entry in array.Entries)
        {
            if (entry.TryDecrypt(largeBlobKey.Span) is not null)
            {
                found = true;
                // Skip this entry (delete it)
            }
            else
            {
                newEntries.Add(entry);
            }
        }
        
        if (!found)
        {
            return false;
        }
        
        // Write back
        var newArray = new LargeBlobArray { Entries = newEntries };
        await WriteLargeBlobArrayAsync(newArray, cancellationToken).ConfigureAwait(false);
        return true;
    }
    
    /// <summary>
    /// Reads raw blob data from the authenticator (for advanced use).
    /// </summary>
    private async Task<byte[]> ReadRawBlobAsync(CancellationToken cancellationToken)
    {
        using var result = new MemoryStream();
        var offset = 0;
        
        while (true)
        {
            var fragment = await ReadFragmentAsync(offset, _maxFragmentLength, cancellationToken)
                .ConfigureAwait(false);
            
            if (fragment.Length == 0)
            {
                break;
            }
            
            result.Write(fragment.Span);
            offset += fragment.Length;
            
            // If we got less than requested, we're done
            if (fragment.Length < _maxFragmentLength)
            {
                break;
            }
        }
        
        return result.ToArray();
    }
    
    /// <summary>
    /// Reads a single fragment from the blob.
    /// </summary>
    private async Task<ReadOnlyMemory<byte>> ReadFragmentAsync(
        int offset,
        int length,
        CancellationToken cancellationToken)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        
        // 0x01: get (number of bytes to read)
        writer.WriteInt32(KeyGet);
        writer.WriteInt32(length);
        
        // 0x03: offset
        writer.WriteInt32(KeyOffset);
        writer.WriteInt32(offset);
        
        writer.WriteEndMap();
        
        var response = await _session.SendCborAsync(
            CtapCommand.LargeBlobs,
            writer.Encode(),
            cancellationToken).ConfigureAwait(false);
        
        // Parse response - should have config (0x01) with the data
        return ParseReadResponse(response);
    }
    
    /// <summary>
    /// Writes raw blob data to the authenticator (for advanced use).
    /// </summary>
    private async Task WriteRawBlobAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken)
    {
        var totalLength = data.Length;
        var offset = 0;
        
        while (offset < totalLength)
        {
            var remaining = totalLength - offset;
            var fragmentLength = Math.Min(remaining, _maxFragmentLength);
            var fragment = data.Slice(offset, fragmentLength);
            
            await WriteFragmentAsync(fragment, offset, totalLength, cancellationToken)
                .ConfigureAwait(false);
            
            offset += fragmentLength;
        }
    }
    
    /// <summary>
    /// Writes a single fragment to the blob.
    /// </summary>
    private async Task WriteFragmentAsync(
        ReadOnlyMemory<byte> fragment,
        int offset,
        int totalLength,
        CancellationToken cancellationToken)
    {
        // Build the message for PIN/UV auth: 32-byte hash of (0xff || offset bytes || SHA-256(data))
        // First, compute SHA-256 of the fragment
        Span<byte> fragmentHash = stackalloc byte[32];
        SHA256.HashData(fragment.Span, fragmentHash);
        
        // Build auth message: 0xff (1) || 0x0C (1) || 0x00 (1) || length (int as u32 BE, 4) || offset (int as u32 BE, 4) || fragmentHash (32)
        // Per CTAP 2.1: authenticate(pinUvAuthToken, 32*0xff || 0x0c || uint32LittleEndian(offset) || SHA-256(contents of set byte string))
        // Actually: 32 bytes of 0xff, then the command byte, offset as little-endian u32, then hash
        var authMessage = new byte[32 + 1 + 4 + 32];
        authMessage.AsSpan(0, 32).Fill(0xff);
        authMessage[32] = CtapCommand.LargeBlobs;
        BitConverter.TryWriteBytes(authMessage.AsSpan(33, 4), offset);
        if (!BitConverter.IsLittleEndian)
        {
            authMessage.AsSpan(33, 4).Reverse();
        }
        fragmentHash.CopyTo(authMessage.AsSpan(37));
        
        var pinUvAuthParam = _protocol!.Authenticate(_pinUvAuthToken.Span, authMessage);
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        
        var mapCount = offset == 0 ? 5 : 4; // length only sent with first fragment
        writer.WriteStartMap(mapCount);
        
        // 0x02: set (data to write)
        writer.WriteInt32(KeySet);
        writer.WriteByteString(fragment.Span);
        
        // 0x03: offset
        writer.WriteInt32(KeyOffset);
        writer.WriteInt32(offset);
        
        // 0x04: length (only for first fragment)
        if (offset == 0)
        {
            writer.WriteInt32(KeyLength);
            writer.WriteInt32(totalLength);
        }
        
        // 0x05: pinUvAuthParam
        writer.WriteInt32(KeyPinUvAuthParam);
        writer.WriteByteString(pinUvAuthParam);
        
        // 0x06: pinUvAuthProtocol
        writer.WriteInt32(KeyPinUvAuthProtocol);
        writer.WriteInt32(_protocol.Version);
        
        writer.WriteEndMap();
        
        await _session.SendCborAsync(
            CtapCommand.LargeBlobs,
            writer.Encode(),
            cancellationToken).ConfigureAwait(false);
    }
    
    private static ReadOnlyMemory<byte> ParseReadResponse(ReadOnlyMemory<byte> response)
    {
        if (response.Length == 0)
        {
            return ReadOnlyMemory<byte>.Empty;
        }
        
        var reader = new CborReader(response, CborConformanceMode.Ctap2Canonical);
        var mapCount = reader.ReadStartMap() ?? 0;
        
        for (var i = 0; i < mapCount; i++)
        {
            var key = reader.ReadInt32();
            
            if (key == KeyConfig)
            {
                return reader.ReadByteString();
            }
            else
            {
                reader.SkipValue();
            }
        }
        
        return ReadOnlyMemory<byte>.Empty;
    }
}
