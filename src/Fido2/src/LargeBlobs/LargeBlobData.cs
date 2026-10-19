// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Buffers.Binary;
using System.Formats.Cbor;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Yubico.YubiKit.Fido2.LargeBlobs;

/// <summary>
/// One element of the CTAP large-blob array (CTAP 2.1-2.3 §6.10.3 "large-blob map structure"):
/// <c>{1: ciphertext, 2: nonce, 3: origSize}</c>.
/// </summary>
/// <remarks>
/// <para>
/// Writing (§6.10.5): the data is compressed with raw DEFLATE (RFC 1951), then encrypted with
/// AEAD_AES_256_GCM under the credential's largeBlobKey with a fresh 12-byte nonce and the associated
/// data <c>"blob" || uint64LittleEndian(origSize)</c>; the 16-byte tag is appended to the ciphertext.
/// </para>
/// <para>
/// Reading (§6.10.4): an entry that does not decrypt under the key is simply not this credential's
/// ("skip this array element"); a decrypted entry whose inflation fails or whose inflated length is
/// not origSize is an error. origSize is capped before decryption, as the §6.10.4 note asks.
/// </para>
/// <para>
/// Unknown map elements are kept as raw CBOR so an entry written by another client survives a rewrite
/// unchanged (§6.10.3: a conforming map "MAY include unknown elements"). This is the format python-fido2
/// (blob.py <c>_lb_pack</c>/<c>_lb_unpack</c>) and every spec-conformant client use.
/// </para>
/// </remarks>
public sealed class LargeBlobEntry
{
    private const int KeyCiphertext = 1;
    private const int KeyNonce = 2;
    private const int KeyOrigSize = 3;
    private const int KeyLength = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    /// <summary>
    /// The largest origSize this SDK will inflate: 1 MiB, the minimum CTAP §6.10.4 asks platforms to
    /// permit ("that maximum SHOULD be at least 1MiB").
    /// </summary>
    public const long MaxOriginalSize = 1024 * 1024;

    private readonly IReadOnlyList<(ReadOnlyMemory<byte> Key, ReadOnlyMemory<byte> Value)> _unknownElements;

    /// <summary>Creates an entry from its spec fields (for example, from a deserialized array).</summary>
    /// <param name="ciphertext">AEAD_AES_256_GCM ciphertext with the 16-byte tag at the end.</param>
    /// <param name="nonce">The 12-byte AEAD nonce.</param>
    /// <param name="originalSize">The length of the uncompressed data.</param>
    public LargeBlobEntry(ReadOnlyMemory<byte> ciphertext, ReadOnlyMemory<byte> nonce, long originalSize)
        : this(ciphertext, nonce, originalSize, [])
    {
    }

    private LargeBlobEntry(
        ReadOnlyMemory<byte> ciphertext,
        ReadOnlyMemory<byte> nonce,
        long originalSize,
        IReadOnlyList<(ReadOnlyMemory<byte> Key, ReadOnlyMemory<byte> Value)> unknownElements)
    {
        if (nonce.Length != NonceSize)
        {
            throw new ArgumentException($"Nonce must be exactly {NonceSize} bytes (CTAP §6.10.3).", nameof(nonce));
        }

        if (ciphertext.Length < TagSize)
        {
            throw new ArgumentException("Ciphertext must include the 16-byte AEAD tag (CTAP §6.10.3).", nameof(ciphertext));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(originalSize);

        Ciphertext = ciphertext;
        Nonce = nonce;
        OriginalSize = originalSize;
        _unknownElements = unknownElements;
    }

    /// <summary>§6.10.3 element 1: the AEAD ciphertext, "implicitly including the AEAD authentication tag at the end".</summary>
    public ReadOnlyMemory<byte> Ciphertext { get; }

    /// <summary>§6.10.3 element 2: the AEAD nonce, "MUST be exactly 12 bytes long".</summary>
    public ReadOnlyMemory<byte> Nonce { get; }

    /// <summary>§6.10.3 element 3: "the length, in bytes, of the uncompressed data".</summary>
    public long OriginalSize { get; }

    /// <summary>
    /// Attempts to decrypt this entry with a credential's largeBlobKey.
    /// </summary>
    /// <param name="largeBlobKey">The 32-byte largeBlobKey.</param>
    /// <returns>A fresh copy of the blob, owned by the caller, or null when the entry is not for this key.</returns>
    /// <exception cref="InvalidDataException">
    /// The entry decrypted under this key but its content is not valid: inflation failed or the inflated
    /// length differs from <see cref="OriginalSize"/> (CTAP §6.10.4: "return an error").
    /// </exception>
    public byte[]? TryDecrypt(ReadOnlySpan<byte> largeBlobKey) => TryDecrypt(largeBlobKey, temporaryObserver: null);

    /// <summary>Test seam: <paramref name="temporaryObserver"/> sees every SDK-owned temporary after it has been zeroed.</summary>
    internal byte[]? TryDecrypt(ReadOnlySpan<byte> largeBlobKey, Action<byte[]>? temporaryObserver)
    {
        if (largeBlobKey.Length != KeyLength)
        {
            throw new ArgumentException($"Large blob key must be {KeyLength} bytes.", nameof(largeBlobKey));
        }

        if (OriginalSize > MaxOriginalSize)
        {
            return null;
        }

        var ciphertext = Ciphertext.Span[..^TagSize];
        var tag = Ciphertext.Span[^TagSize..];
        var compressed = new byte[ciphertext.Length];
        try
        {
            try
            {
                using var aesGcm = new AesGcm(largeBlobKey, TagSize);
                aesGcm.Decrypt(Nonce.Span, ciphertext, tag, compressed, AssociatedData(OriginalSize));
            }
            catch (CryptographicException)
            {
                return null; // not this credential's entry
            }

            return Inflate(compressed, OriginalSize, temporaryObserver);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(compressed);
            temporaryObserver?.Invoke(compressed);
        }
    }

    /// <summary>
    /// Creates an entry for <paramref name="data"/> under a credential's largeBlobKey (CTAP §6.10.5).
    /// </summary>
    public static LargeBlobEntry Encrypt(ReadOnlySpan<byte> largeBlobKey, ReadOnlySpan<byte> data)
    {
        if (largeBlobKey.Length != KeyLength)
        {
            throw new ArgumentException($"Large blob key must be {KeyLength} bytes.", nameof(largeBlobKey));
        }

        var compressed = Deflate(data);
        try
        {
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var ciphertextAndTag = new byte[compressed.Length + TagSize];
            using var aesGcm = new AesGcm(largeBlobKey, TagSize);
            aesGcm.Encrypt(nonce, compressed, ciphertextAndTag.AsSpan(0, compressed.Length), ciphertextAndTag.AsSpan(compressed.Length), AssociatedData(data.Length));
            return new LargeBlobEntry(ciphertextAndTag, nonce, data.Length);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(compressed);
        }
    }

    /// <summary>Encodes the §6.10.3 map (canonical CBOR), unknown elements included.</summary>
    internal void Encode(CborWriter writer)
    {
        writer.WriteStartMap(3 + _unknownElements.Count);
        writer.WriteInt32(KeyCiphertext);
        writer.WriteByteString(Ciphertext.Span);
        writer.WriteInt32(KeyNonce);
        writer.WriteByteString(Nonce.Span);
        writer.WriteInt32(KeyOrigSize);
        writer.WriteInt64(OriginalSize);
        foreach (var (key, value) in _unknownElements)
        {
            writer.WriteEncodedValue(key.Span);
            writer.WriteEncodedValue(value.Span);
        }

        writer.WriteEndMap();
    }

    /// <summary>
    /// Decodes one array element. Returns null when the element is not a conforming map (§6.10.4: "skip
    /// this array element"); the reader is left after the element either way.
    /// </summary>
    internal static LargeBlobEntry? TryDecode(CborReader reader)
    {
        if (reader.PeekState() != CborReaderState.StartMap)
        {
            reader.SkipValue();
            return null;
        }

        ReadOnlyMemory<byte>? ciphertext = null, nonce = null;
        long? origSize = null;
        var unknown = new List<(ReadOnlyMemory<byte> Key, ReadOnlyMemory<byte> Value)>();
        var count = reader.ReadStartMap();
        for (var i = 0; i < count; i++)
        {
            if (reader.PeekState() is not (CborReaderState.UnsignedInteger or CborReaderState.NegativeInteger))
            {
                // Non-integer key: keep it as an unknown element verbatim.
                unknown.Add((reader.ReadEncodedValue(), reader.ReadEncodedValue()));
                continue;
            }

            var encodedKey = reader.ReadEncodedValue();
            var key = new CborReader(encodedKey, CborConformanceMode.Lax).ReadInt64();
            switch (key)
            {
                case KeyCiphertext when reader.PeekState() == CborReaderState.ByteString:
                    ciphertext = reader.ReadByteString();
                    break;
                case KeyNonce when reader.PeekState() == CborReaderState.ByteString:
                    nonce = reader.ReadByteString();
                    break;
                case KeyOrigSize when reader.PeekState() == CborReaderState.UnsignedInteger:
                    origSize = reader.ReadInt64();
                    break;
                case KeyCiphertext or KeyNonce or KeyOrigSize:
                    reader.SkipValue(); // required element of the wrong type → not conforming
                    ciphertext = nonce = null;
                    origSize = null;
                    i = count!.Value; // stop scanning; the map is rejected below
                    break;
                default:
                    unknown.Add((encodedKey, reader.ReadEncodedValue()));
                    break;
            }
        }

        // Drain any elements left after an early stop so the reader stays positioned.
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            reader.SkipValue();
            reader.SkipValue();
        }

        reader.ReadEndMap();

        if (ciphertext is null || nonce is null || origSize is null
            || nonce.Value.Length != NonceSize || ciphertext.Value.Length < TagSize)
        {
            return null;
        }

        return new LargeBlobEntry(ciphertext.Value, nonce.Value, origSize.Value, unknown);
    }

    /// <summary>§6.10.3: "The value 0x626c6f62 ("blob") || uint64LittleEndian(origSize)".</summary>
    private static byte[] AssociatedData(long origSize)
    {
        var ad = new byte[4 + sizeof(ulong)];
        "blob"u8.CopyTo(ad);
        BinaryPrimitives.WriteUInt64LittleEndian(ad.AsSpan(4), (ulong)origSize);
        return ad;
    }

    /// <summary>§6.10.5: "Let plaintext equal origData after compression with DEFLATE [RFC1951]" (raw, no zlib header).</summary>
    private static byte[] Deflate(ReadOnlySpan<byte> data)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(data);
        }

        return output.ToArray();
    }

    /// <summary>
    /// §6.10.4: inflate, "If decompression fails, return an error. If the length of the decompression result
    /// is not equal to origSize, return an error." Reads at most origSize + 1 bytes so an inflation bomb
    /// cannot exceed the cap.
    /// </summary>
    private static byte[] Inflate(byte[] compressed, long originalSize, Action<byte[]>? temporaryObserver)
    {
        var expected = checked((int)originalSize);
        var buffer = new byte[expected + 1];
        try
        {
            int total;
            try
            {
                using var inflate = new DeflateStream(new MemoryStream(compressed, writable: false), CompressionMode.Decompress);
                total = 0;
                int read;
                while (total < buffer.Length && (read = inflate.Read(buffer, total, buffer.Length - total)) > 0)
                {
                    total += read;
                }
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidDataException("Large-blob entry failed to decompress (CTAP §6.10.4).", ex);
            }

            if (total != expected)
            {
                throw new InvalidDataException(
                    $"Large-blob entry inflated to {(total > expected ? "more than " : "")}{Math.Min(total, expected)} bytes but origSize is {expected} (CTAP §6.10.4).");
            }

            return buffer.AsSpan(0, expected).ToArray();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer);
            temporaryObserver?.Invoke(buffer);
        }
    }
}

/// <summary>
/// The large-blob array stored on the authenticator (CTAP 2.1-2.3 §6.10.2): a canonical CBOR array of
/// <see cref="LargeBlobEntry"/> maps followed by the first 16 bytes of SHA-256 over the array.
/// </summary>
public sealed class LargeBlobArray
{
    private const int HashSize = 16;

    /// <summary>The conforming entries, in array order.</summary>
    public IReadOnlyList<LargeBlobEntry> Entries { get; init; } = [];

    /// <summary>
    /// Deserializes a large-blob array, verifying the trailing hash. Elements that are not conforming
    /// large-blob maps are skipped (§6.10.4).
    /// </summary>
    /// <exception cref="ArgumentException">The data is too short, the hash does not match, or the CBOR is malformed.</exception>
    /// <remarks>
    /// Strict on purpose for direct callers. <see cref="LargeBlobStorage.ReadLargeBlobArrayAsync"/> applies
    /// §6.10.2's rule that a short or corrupt array is treated as the initial (empty) array.
    /// </remarks>
    public static LargeBlobArray Deserialize(ReadOnlyMemory<byte> data)
    {
        if (!HasValidHash(data))
        {
            throw new ArgumentException(
                data.Length < HashSize + 1
                    ? "Data too short for valid large blob array."
                    : "Large blob array hash verification failed.",
                nameof(data));
        }

        var entries = new List<LargeBlobEntry>();
        var reader = new CborReader(data[..^HashSize], CborConformanceMode.Ctap2Canonical);
        var arrayLength = reader.ReadStartArray() ?? 0;
        for (var i = 0; i < arrayLength; i++)
        {
            if (LargeBlobEntry.TryDecode(reader) is { } entry)
            {
                entries.Add(entry);
            }
        }

        reader.ReadEndArray();
        return new LargeBlobArray { Entries = entries };
    }

    /// <summary>§6.10.2: the trailing 16 bytes are LEFT(SHA-256(array), 16); anything else is "the initial large-blob array".</summary>
    public static bool HasValidHash(ReadOnlyMemory<byte> data)
    {
        if (data.Length < HashSize + 1)
        {
            return false;
        }

        Span<byte> computed = stackalloc byte[32];
        SHA256.HashData(data.Span[..^HashSize], computed);
        return CryptographicOperations.FixedTimeEquals(computed[..HashSize], data.Span[^HashSize..]);
    }

    /// <summary>Serializes the array with its trailing hash (canonical CBOR, §6.10.2).</summary>
    public byte[] Serialize()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartArray(Entries.Count);
        foreach (var entry in Entries)
        {
            entry.Encode(writer);
        }

        writer.WriteEndArray();
        var arrayData = writer.Encode();

        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(arrayData, hash);
        return [.. arrayData, .. hash[..HashSize]];
    }

    /// <summary>The initial large-blob array: no entries.</summary>
    public static LargeBlobArray CreateEmpty() => new() { Entries = [] };

    /// <summary>Returns a new array with <paramref name="entry"/> appended.</summary>
    public LargeBlobArray WithEntry(LargeBlobEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return new LargeBlobArray { Entries = [.. Entries, entry] };
    }

    /// <summary>Returns a new array without the entry at <paramref name="index"/>.</summary>
    public LargeBlobArray WithoutEntry(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Entries.Count);
        var entries = new List<LargeBlobEntry>(Entries);
        entries.RemoveAt(index);
        return new LargeBlobArray { Entries = entries };
    }

    /// <summary>Returns the first entry's blob that decrypts under <paramref name="largeBlobKey"/>, or null.</summary>
    public byte[]? FindAndDecrypt(ReadOnlySpan<byte> largeBlobKey)
    {
        foreach (var entry in Entries)
        {
            if (entry.TryDecrypt(largeBlobKey) is { } blob)
            {
                return blob;
            }
        }

        return null;
    }
}