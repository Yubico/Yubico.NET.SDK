// Copyright 2026 Yubico AB
// SPDX-License-Identifier: Apache-2.0

using System.Formats.Asn1;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Piv.Backends;
using Yubico.YubiKit.Piv.DataObjects;

namespace Yubico.YubiKit.Piv.Authentication;

// PIN-only reads and edits share one field scan. Preserve opaque child encodings for edits;
// recovery borrows the key and salt directly from the owned response.
internal sealed class PinOnlyMetadata : IDisposable
{
    private const byte AdminTag = 0x80;
    private const byte PrintedTag = 0x88;
    private const byte FlagsTag = 0x81;
    private const byte SaltTag = 0x82;
    private const byte TimestampTag = 0x83;
    private const byte ManagementKeyTag = 0x89;
    internal const byte PukBlocked = 0x01;
    internal const byte KeyStored = 0x02;

    private readonly ApduResponse _response;
    private readonly byte _wrapper;
    private readonly List<Field> _fields = [];

    private PinOnlyMetadata(ApduResponse response, byte wrapper, ReadOnlyMemory<byte> data)
    {
        _response = response;
        _wrapper = wrapper;
        while (!data.IsEmpty)
        {
            byte tag = data.Span[0];
            var value = ReadField(data, out int consumed);
            if (IsModeled(wrapper, tag) && (!IsValidLength(tag, value.Length) || Find(tag) >= 0))
                throw new ApduException("Invalid or duplicated PIN-only metadata field.");
            _fields.Add(new Field(tag, value, data[..consumed]));
            data = data[consumed..];
        }
    }

    internal bool IsEmpty => _fields.Count == 0;
    internal byte Flags => Get(FlagsTag).Span is [byte flags] ? flags : (byte)0;
    internal ReadOnlyMemory<byte> Salt => Get(SaltTag);
    internal ReadOnlyMemory<byte> StoredKey => Get(ManagementKeyTag);

    internal static async Task<PinOnlyMetadata> ReadAsync(IBackend backend, int objectId, CancellationToken cancellationToken) =>
        await ReadCoreAsync(backend, objectId, ignoreMalformed: false, cancellationToken).ConfigureAwait(false)
        ?? throw new InvalidOperationException("PIN-only metadata could not be decoded.");

    // Recovery can skip an unsupported or malformed object, but not a failed GET DATA.
    internal static Task<PinOnlyMetadata?> ReadForRecoveryAsync(IBackend backend, int objectId, CancellationToken cancellationToken) =>
        ReadCoreAsync(backend, objectId, ignoreMalformed: true, cancellationToken);

    private static async Task<PinOnlyMetadata?> ReadCoreAsync(
        IBackend backend, int objectId, bool ignoreMalformed, CancellationToken cancellationToken)
    {
        var response = await DataObjectProtocol.GetObjectResponseAsync(backend, objectId, cancellationToken).ConfigureAwait(false);
        try
        {
            byte wrapper = objectId == DataObject.AdminData ? AdminTag : PrintedTag;
            var data = response.Data;
            if (response.SW == SWConstants.FileOrApplicationNotFound && data.IsEmpty)
                return new PinOnlyMetadata(response, wrapper, data);
            if (!response.IsOK())
                throw ApduException.FromStatusWord(response.SW, "Could not read PIN-only metadata.");

            try
            {
                return new PinOnlyMetadata(response, wrapper, ReadContents(data, wrapper));
            }
            catch (ApduException) when (ignoreMalformed)
            {
                response.Clear();
                return null;
            }
        }
        catch
        {
            response.Clear();
            throw;
        }
    }

    private static ReadOnlyMemory<byte> ReadContents(ReadOnlyMemory<byte> data, byte wrapper)
    {
        if (!data.IsEmpty && data.Span[0] == 0x53)
            data = Unwrap(data, 0x53);
        return data.IsEmpty ? data : Unwrap(data, wrapper);
    }

    internal void SetFlags(byte flags) => Set(FlagsTag, new byte[] { flags });
    internal void SetSalt(ReadOnlyMemory<byte> salt) => Set(SaltTag, salt);
    internal void SetStoredKey(ReadOnlyMemory<byte> key) => Set(ManagementKeyTag, key);

    // The returned encoding is owned by the caller and can contain a management key.
    internal byte[] Encode()
    {
        if (IsEmpty)
            return [];

        int contentLength = _fields.Sum(static field => field.EncodedLength);
        int headerLength = 1 + BerLength.EncodingSize(contentLength);
        int totalLength = headerLength + contentLength;
        _ = BerLength.EncodingSize(totalLength); // Check capacity of the PUT DATA 0x53 envelope too.
        var encoded = new byte[totalLength];
        try
        {
            EncodeFields(encoded, contentLength, headerLength);
            return encoded;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(encoded);
            throw;
        }
    }

    private void EncodeFields(Span<byte> encoded, int contentLength, int headerLength)
    {
        encoded[0] = _wrapper;
        BerLength.Write(encoded[1..], contentLength);
        var remaining = encoded[headerLength..];
        foreach (var field in _fields)
        {
            if (field.Original.IsEmpty)
            {
                remaining[0] = field.Tag;
                int offset = 1 + BerLength.Write(remaining[1..], field.Value.Length);
                field.Value.Span.CopyTo(remaining[offset..]);
            }
            else
            {
                field.Original.Span.CopyTo(remaining);
            }
            remaining = remaining[field.EncodedLength..];
        }
    }

    private static ReadOnlyMemory<byte> Unwrap(ReadOnlyMemory<byte> data, byte tag)
    {
        if (data.Span[0] != tag)
            throw new ApduException("Unsupported PIN-only metadata wrapper.");
        var value = ReadField(data, out int consumed);
        if (consumed != data.Length)
            throw new ApduException("Unexpected bytes after PIN-only metadata wrapper.");
        return value;
    }

    private static ReadOnlyMemory<byte> ReadField(ReadOnlyMemory<byte> data, out int consumed)
    {
        // Use the consumed length of the original definite BER encoding. Child payloads are opaque.
        if (data.Span[0] == 0 || !AsnDecoder.TryReadEncodedValue(data.Span, AsnEncodingRules.BER,
            out _, out int offset, out int length, out consumed) || offset + length != consumed)
            throw new ApduException("Malformed PIN-only metadata TLV.");
        return data.Slice(offset, length);
    }

    private static bool IsModeled(byte wrapper, byte tag) => wrapper == AdminTag
        ? tag is FlagsTag or SaltTag or TimestampTag
        : tag == ManagementKeyTag;

    private static bool IsValidLength(byte tag, int length) => tag switch
    {
        FlagsTag => length == 1,
        SaltTag => length == 16,
        TimestampTag => length <= 8, // .NET V1 (trimmed LE) and Python (four-byte BE); do not interpret.
        ManagementKeyTag => length is 16 or 24 or 32,
        _ => false,
    };

    private int Find(byte tag) => _fields.FindIndex(field => field.Tag == tag);

    private ReadOnlyMemory<byte> Get(byte tag)
    {
        int index = Find(tag);
        return index >= 0 ? _fields[index].Value : default;
    }

    private void Set(byte tag, ReadOnlyMemory<byte> value)
    {
        int index = Find(tag);
        if (value.IsEmpty)
        {
            if (index >= 0)
                _fields.RemoveAt(index);
        }
        else if (index >= 0)
            _fields[index] = new Field(tag, value, default);
        else
            _fields.Add(new Field(tag, value, default));
    }

    public void Dispose() => _response.Clear();

    private readonly record struct Field(byte Tag, ReadOnlyMemory<byte> Value, ReadOnlyMemory<byte> Original)
    {
        internal int EncodedLength => Original.IsEmpty ? 1 + BerLength.EncodingSize(Value.Length) + Value.Length : Original.Length;
    }
}