using NSubstitute;
using System.Buffers.Binary;
using System.Formats.Cbor;
using System.IO.Compression;
using System.Security.Cryptography;
using Yubico.YubiKit.Fido2.LargeBlobs;

namespace Yubico.YubiKit.Fido2.UnitTests.AuditV2;

public class Fido2LargeBlobAuditReproTests
{
    private static readonly byte[] Key = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
    private static readonly byte[] Blob = "interop-large-blob"u8.ToArray();

    [Fact]
    [Trait("Audit", "YESDK-1620")]
    public async Task YESDK1620_DefaultReadFragmentIs960Bytes()
    {
        var session = Substitute.For<IFidoSession>();
        ReadOnlyMemory<byte> request = default;
        session.SendCborRequestAsync(Arg.Do<ReadOnlyMemory<byte>>(x => request = x.ToArray()), Arg.Any<CancellationToken>())
            .Returns(ReadOnlyMemory<byte>.Empty);

        await new LargeBlobStorage(session).ReadLargeBlobArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0x0c, request.Span[0]);
        var reader = new CborReader(request[1..]);
        Assert.Equal(2, reader.ReadStartMap());
        Assert.Equal(1, reader.ReadInt32());
        Assert.Equal(960, reader.ReadInt32());
    }

    [Fact]
    [Trait("Audit", "YESDK-1621")]
    public void YESDK1621_ReadsPythonFido2CompatibleArray()
    {
        var serialized = BuildSpecArray(Key, Blob);
        var array = LargeBlobArray.Deserialize(serialized);
        Assert.Equal(Blob, array.FindAndDecrypt(Key));
    }

    [Fact]
    [Trait("Audit", "YESDK-1621")]
    public void YESDK1621_SdkWrittenArrayIsSpecDecodable()
    {
        var serialized = LargeBlobArray.CreateEmpty().WithEntry(LargeBlobEntry.Encrypt(Key, Blob)).Serialize();
        var reader = new CborReader(serialized.AsMemory(0, serialized.Length - 16), CborConformanceMode.Ctap2Canonical);
        Assert.Equal(1, reader.ReadStartArray());
        Assert.Equal(3, reader.ReadStartMap());
        Assert.Equal(1, reader.ReadInt32());
        var ciphertextAndTag = reader.ReadByteString();
        Assert.Equal(2, reader.ReadInt32());
        var nonce = reader.ReadByteString();
        Assert.Equal(3, reader.ReadInt32());
        var originalSize = reader.ReadInt32();
        reader.ReadEndMap();
        reader.ReadEndArray();
        Assert.Equal(Blob.Length, originalSize);
        Span<byte> ad = stackalloc byte[12];
        "blob"u8.CopyTo(ad);
        BinaryPrimitives.WriteUInt64LittleEndian(ad[4..], (ulong)originalSize);
        var compressed = new byte[ciphertextAndTag.Length - 16];
        using var aes = new AesGcm(Key, 16);
        aes.Decrypt(nonce, ciphertextAndTag.AsSpan(0, compressed.Length), ciphertextAndTag.AsSpan(compressed.Length), compressed, ad);
        using var deflate = new DeflateStream(new MemoryStream(compressed), CompressionMode.Decompress);
        using var result = new MemoryStream();
        deflate.CopyTo(result);
        Assert.Equal(Blob, result.ToArray());
    }

    [Fact]
    [Trait("Audit", "YESDK-1622")]
    public async Task YESDK1622_CorruptChecksumActsAsInitialArray()
    {
        var session = Substitute.For<IFidoSession>();
        byte[] corrupt = LargeBlobArray.CreateEmpty().Serialize();
        corrupt[^1] ^= 1;
        var writer = new CborWriter();
        writer.WriteStartMap(1);
        writer.WriteInt32(1);
        writer.WriteByteString(corrupt);
        writer.WriteEndMap();
        session.SendCborRequestAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(writer.Encode().AsMemory());

        var array = await new LargeBlobStorage(session).ReadLargeBlobArrayAsync(TestContext.Current.CancellationToken);
        Assert.Empty(array.Entries);
        Assert.Equal(LargeBlobArray.CreateEmpty().Serialize(), array.Serialize());
    }

    [Fact]
    [Trait("Audit", "YESDK-1624")]
    public void YESDK1624_DecryptedTemporaryPlaintextIsCleared()
    {
        // Every SDK-owned temporary (the AES-GCM output and the inflate buffer) is zeroed before TryDecrypt returns;
        // the caller receives a fresh copy it owns.
        var entry = LargeBlobEntry.Encrypt(Key, Blob);
        var temporaries = new List<byte[]>();

        var result = entry.TryDecrypt(Key, temporaries.Add);

        Assert.Equal(Blob, result);
        Assert.NotEmpty(temporaries);
        Assert.All(temporaries, buffer => Assert.All(buffer, b => Assert.Equal(0, b)));
    }

    // Equivalent to python-fido2 blob.py _compress, _lb_ad, _lb_pack, write_blob_array.
    private static byte[] BuildSpecArray(byte[] key, byte[] blob)
    {
        using var compressedStream = new MemoryStream();
        using (var deflate = new DeflateStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
            deflate.Write(blob);
        byte[] compressed = compressedStream.ToArray();
        byte[] nonce = Enumerable.Range(0, 12).Select(i => (byte)i).ToArray();
        Span<byte> ad = stackalloc byte[12];
        "blob"u8.CopyTo(ad);
        BinaryPrimitives.WriteUInt64LittleEndian(ad[4..], (ulong)blob.Length);
        var ciphertext = new byte[compressed.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(key, 16))
            aes.Encrypt(nonce, compressed, ciphertext, tag, ad);
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartArray(1);
        writer.WriteStartMap(3);
        writer.WriteInt32(1);
        writer.WriteByteString([.. ciphertext, .. tag]);
        writer.WriteInt32(2);
        writer.WriteByteString(nonce);
        writer.WriteInt32(3);
        writer.WriteInt32(blob.Length);
        writer.WriteEndMap();
        writer.WriteEndArray();
        byte[] array = writer.Encode();
        return [.. array, .. SHA256.HashData(array).AsSpan(0, 16)];
    }
}