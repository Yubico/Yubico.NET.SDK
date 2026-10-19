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

using NSubstitute;
using System.Buffers.Binary;
using System.Formats.Cbor;
using System.IO.Compression;
using System.Security.Cryptography;
using Yubico.YubiKit.Fido2.LargeBlobs;

namespace Yubico.YubiKit.Fido2.UnitTests.LargeBlobs;

/// <summary>
///     CTAP 2.1-2.3 §6.10 large-blob format and storage rules, written before the rewrite that implements them
///     (#13 format, #12 fragment length, #14 corrupt array, #16 zeroing). The reference encoder below is a
///     transliteration of python-fido2 blob.py (_compress, _lb_ad, _lb_pack, write_blob_array).
/// </summary>
public class LargeBlobSpecFormatTests
{
    private const int HashSize = 16;
    private const int TagSize = 16;
    private const int NonceSize = 12;
    private const int DefaultMaxMsgSize = 1024;          // CTAP §6.4 default when GetInfo omits maxMsgSize
    private const int FragmentOverhead = 64;              // CTAP §6.10.2: maxFragmentLength = maxMsgSize − 64
    private const int CtapCommandLargeBlobs = 0x0C;
    private const int KeyGet = 0x01;
    private const int KeyCiphertext = 1, KeyNonce = 2, KeyOrigSize = 3;

    private static readonly byte[] KeyA = Enumerable.Repeat((byte)0xA1, 32).ToArray();
    private static readonly byte[] KeyB = Enumerable.Repeat((byte)0xB2, 32).ToArray();
    private static readonly byte[] BlobA = "hello from credential A"u8.ToArray();
    private static readonly byte[] BlobB = "a different blob for B"u8.ToArray();

    // ---- reference encoder (python-fido2 blob.py) ----

    private static byte[] Deflate(byte[] data)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(data);
        }
        return output.ToArray();
    }

    private static byte[] AssociatedData(long origSize)
    {
        var ad = new byte[12];
        "blob"u8.CopyTo(ad);
        BinaryPrimitives.WriteUInt64LittleEndian(ad.AsSpan(4), (ulong)origSize);
        return ad;
    }

    private static void WriteSpecEntry(CborWriter writer, byte[] key, byte[] blob, byte[]? unknownElementValue = null)
    {
        var compressed = Deflate(blob);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[compressed.Length];
        var tag = new byte[TagSize];
        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Encrypt(nonce, compressed, ciphertext, tag, AssociatedData(blob.Length));
        }
        writer.WriteStartMap(unknownElementValue is null ? 3 : 4);
        writer.WriteInt32(KeyCiphertext); writer.WriteByteString([.. ciphertext, .. tag]);
        writer.WriteInt32(KeyNonce); writer.WriteByteString(nonce);
        writer.WriteInt32(KeyOrigSize); writer.WriteInt32(blob.Length);
        if (unknownElementValue is not null)
        {
            writer.WriteInt32(99); writer.WriteByteString(unknownElementValue); // §6.10.3: "MAY include unknown elements"
        }
        writer.WriteEndMap();
    }

    private static byte[] Finish(CborWriter writer)
    {
        var array = writer.Encode();
        return [.. array, .. SHA256.HashData(array).AsSpan(0, HashSize)];
    }

    private static IFidoSession SessionWithInfo(int? maxMsgSize = null, int? maxSerializedLargeBlobArray = null)
    {
        var session = Substitute.For<IFidoSession>();
        session.GetInfoAsync(Arg.Any<CancellationToken>())
            .Returns(new AuthenticatorInfo { MaxMsgSize = maxMsgSize, MaxSerializedLargeBlobArray = maxSerializedLargeBlobArray });
        return session;
    }

    // ---- #13 format ----

    [Fact]
    [Trait("Audit", "YESDK-1621")]
    public void Entry_RoundTrip_ThroughTheSpecFields()
    {
        var entry = LargeBlobEntry.Encrypt(KeyA, BlobA);

        Assert.Equal(NonceSize, entry.Nonce.Length);
        Assert.Equal(BlobA.Length, entry.OriginalSize);
        Assert.True(entry.Ciphertext.Length > TagSize);
        Assert.Equal(BlobA, entry.TryDecrypt(KeyA));
        Assert.Null(entry.TryDecrypt(KeyB));
    }

    /// <summary>§6.10.3 AAD is "blob" || uint64LE(origSize): an entry whose origSize was tampered must not decrypt.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1621")]
    public void Entry_TamperedOrigSize_FailsAuthentication()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartArray(1);
        WriteSpecEntry(writer, KeyA, BlobA);
        writer.WriteEndArray();
        var array = LargeBlobArray.Deserialize(Finish(writer));
        var genuine = Assert.Single(array.Entries);

        var tampered = new LargeBlobEntry(genuine.Ciphertext, genuine.Nonce, genuine.OriginalSize + 1);

        Assert.Null(tampered.TryDecrypt(KeyA));
    }

    /// <summary>§6.10.2/§6.10.3: an entry written by another client for another key is carried through byte-for-byte, unknown elements included.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1621")]
    public void Array_RewriteKeepsForeignEntriesByteForByte()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartArray(1);
        WriteSpecEntry(writer, KeyB, BlobB, unknownElementValue: [0xEE]);
        writer.WriteEndArray();
        var original = Finish(writer);
        var foreignEntryBytes = original[1..^HashSize]; // array header (1 byte) ... hash

        var array = LargeBlobArray.Deserialize(original).WithEntry(LargeBlobEntry.Encrypt(KeyA, BlobA));
        var rewritten = array.Serialize();

        Assert.Equal(BlobA, array.FindAndDecrypt(KeyA));
        Assert.Equal(BlobB, array.FindAndDecrypt(KeyB));
        Assert.Contains(foreignEntryBytes.AsSpan().ToArray(), rewritten); // the foreign map is embedded unchanged
    }

    /// <summary>§6.10.4: "If the element is not a map conforming to the large-blob map structure ... skip this array element."</summary>
    [Fact]
    [Trait("Audit", "YESDK-1621")]
    public void Array_NonConformingElement_IsSkipped()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartArray(2);
        writer.WriteByteString(new byte[40]); // the SDK's former private format: not a map
        WriteSpecEntry(writer, KeyA, BlobA);
        writer.WriteEndArray();

        var array = LargeBlobArray.Deserialize(Finish(writer));

        Assert.Single(array.Entries);
        Assert.Equal(BlobA, array.FindAndDecrypt(KeyA));
    }

    /// <summary>§6.10.4 note: "Platforms SHOULD limit the maximum permitted value of origSize and that maximum SHOULD be at least 1MiB."</summary>
    [Fact]
    [Trait("Audit", "YESDK-1621")]
    public void Entry_OrigSizeAboveTheCap_IsNotDecrypted()
    {
        var genuine = LargeBlobEntry.Encrypt(KeyA, BlobA);
        var huge = new LargeBlobEntry(genuine.Ciphertext, genuine.Nonce, LargeBlobEntry.MaxOriginalSize + 1);

        Assert.Null(huge.TryDecrypt(KeyA));
    }

    // ---- #12 fragment length ----

    [Theory]
    [Trait("Audit", "YESDK-1620")]
    [InlineData(null, DefaultMaxMsgSize - FragmentOverhead)]
    [InlineData(1536, 1536 - FragmentOverhead)]
    public async Task Read_UsesMaxMsgSizeMinus64AsFragmentLength(int? maxMsgSize, int expectedFragment)
    {
        var session = SessionWithInfo(maxMsgSize);
        ReadOnlyMemory<byte> request = default;
        session.SendCborRequestAsync(Arg.Do<ReadOnlyMemory<byte>>(r => request = r.ToArray()), Arg.Any<CancellationToken>())
            .Returns(ReadOnlyMemory<byte>.Empty);

        await new LargeBlobStorage(session).ReadLargeBlobArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(CtapCommandLargeBlobs, request.Span[0]);
        var reader = new CborReader(request[1..]);
        reader.ReadStartMap();
        Assert.Equal(KeyGet, reader.ReadInt32());
        Assert.Equal(expectedFragment, reader.ReadInt32());
    }

    /// <summary>§6.10.2: "Platforms MUST NOT attempt to write a serialized large-blob array that exceeds the maxSerializedLargeBlobArray".</summary>
    [Fact]
    [Trait("Audit", "YESDK-1620")]
    public async Task Write_LargerThanMaxSerializedLargeBlobArray_IsRefusedBeforeAnyCommand()
    {
        var session = SessionWithInfo(maxSerializedLargeBlobArray: 64);
        var storage = new LargeBlobStorage(session, new TestProtocol(), new byte[32]);
        var array = LargeBlobArray.CreateEmpty().WithEntry(LargeBlobEntry.Encrypt(KeyA, RandomNumberGenerator.GetBytes(200))); // incompressible

        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.WriteLargeBlobArrayAsync(array, TestContext.Current.CancellationToken));
        await session.DidNotReceive().SendCborRequestAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>());
    }

    // ---- #16 zeroing ----

    /// <summary>Encrypt leaves no plaintext or compressed copy behind in SDK-owned buffers (probe via the deflate output the SDK allocates).</summary>
    [Fact]
    [Trait("Audit", "YESDK-1624")]
    public void Decrypt_ReturnsACopyAndZeroesItsTemporaries()
    {
        var entry = LargeBlobEntry.Encrypt(KeyA, BlobA);
        var observed = new List<byte[]>();

        var result = entry.TryDecrypt(KeyA, observed.Add);

        Assert.Equal(BlobA, result);
        Assert.NotEmpty(observed);
        Assert.All(observed, buffer => Assert.All(buffer, b => Assert.Equal(0, b)));
    }

    private sealed class TestProtocol : Yubico.YubiKit.Fido2.Pin.IPinUvAuthProtocol
    {
        public int Version => 2;
        public int AuthenticationTagLength => 16;
        public byte[] Authenticate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message) => new byte[32];
        public bool Verify(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature) => true;
        public byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext) => throw new NotSupportedException();
        public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext) => throw new NotSupportedException();
        public (Dictionary<int, object?> KeyAgreement, byte[] SharedSecret) Encapsulate(IReadOnlyDictionary<int, object?> peerCoseKey) => throw new NotSupportedException();
        public byte[] Kdf(ReadOnlySpan<byte> z) => throw new NotSupportedException();
        public void Dispose() { }
    }
}