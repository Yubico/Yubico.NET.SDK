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

using System;
using System.Collections.Generic;
using System.Formats.Cbor;

namespace Yubico.YubiKey.Fido2.Cbor;

/// <summary>
///     Some helpers to make writing CBOR maps a little easier.
/// </summary>
/// <remarks>
///     Note that the only types supported for <c>TKey</c> are <c>int</c> and
///     <c>string</c>.
/// </remarks>
internal class CborMapWriter<TKey>
{
    private readonly CborWriter _cbor;

    public CborMapWriter()
        : this(new CborWriter(CborConformanceMode.Ctap2Canonical, true))
    {
    }

    public CborMapWriter(CborWriter cbor)
    {
        if (!cbor.ConvertIndefiniteLengthEncodings)
        {
            throw new ArgumentException(ExceptionMessages.CborWriterMustConvertIdenfiteLengths);
        }

        _cbor = cbor;
        _cbor.WriteStartMap(null);
    }

    public CborMapWriter<TKey> Entry(TKey key, string value)
    {
        WriteKey(key);
        _cbor.WriteTextString(value);

        return this;
    }

    public CborMapWriter<TKey> Entry(TKey key, int value)
    {
        WriteKey(key);
        _cbor.WriteInt32(value);

        return this;
    }

    public CborMapWriter<TKey> Entry(TKey key, bool value)
    {
        WriteKey(key);
        _cbor.WriteBoolean(value);

        return this;
    }

    public CborMapWriter<TKey> Entry(TKey key, ReadOnlyMemory<byte> value)
    {
        WriteKey(key);
        _cbor.WriteByteString(value.Span);

        return this;
    }

    public CborMapWriter<TKey> Entry(TKey key, ICborEncode value)
    {
        WriteKey(key);
        _cbor.WriteEncodedValue(value.CborEncode());

        return this;
    }

    public CborMapWriter<TKey> Entry(TKey key, ReadOnlySpan<string> values)
    {
        WriteKey(key);
        _cbor.WriteStartArray(values.Length);
        foreach (string value in values)
        {
            _cbor.WriteTextString(value);
        }

        _cbor.WriteEndArray();

        return this;
    }

    public CborMapWriter<TKey> Entry(TKey key, ReadOnlySpan<int> values)
    {
        WriteKey(key);
        _cbor.WriteStartArray(values.Length);
        foreach (int value in values)
        {
            _cbor.WriteInt32(value);
        }

        _cbor.WriteEndArray();

        return this;
    }

    public CborMapWriter<TKey> Entry(TKey key, ReadOnlySpan<long> values)
    {
        WriteKey(key);
        _cbor.WriteStartArray(values.Length);
        foreach (long value in values)
        {
            _cbor.WriteInt64(value);
        }

        _cbor.WriteEndArray();

        return this;
    }

    public CborMapWriter<TKey> Entry<TKey2, TValue>(TKey key, IReadOnlyDictionary<TKey2, TValue> values)
    {
        WriteKey(key);
        _cbor.WriteStartMap(values.Count);
        foreach (var entry in values)
        {
            WriteEntry(entry.Key, entry.Value);
        }

        _cbor.WriteEndMap();

        return this;
    }

    public CborMapWriter<TKey> Entry<TKey2, TValue>(TKey key, (TKey2, TValue)[] values)
    {
        WriteKey(key);
        _cbor.WriteStartMap(values.Length);
        foreach (var entry in values)
        {
            WriteEntry(entry.Item1, entry.Item2);
        }

        _cbor.WriteEndMap();

        return this;
    }

    public CborMapWriter<TKey> Entry(int key, Dictionary<string, object?>[] values)
    {
        WriteKey(key);
        _cbor.WriteStartArray(values.Length);
        foreach (var item in values)
        {
            _cbor.WriteStartMap(item.Count);
            foreach (var entry in item)
            {
                WriteEntry(entry.Key, entry.Value);
            }

            _cbor.WriteEndMap();
        }

        _cbor.WriteEndArray();

        return this;
    }

    // If the Encoder writes out an empty array, this will throw an
    // exception.
    public CborMapWriter<TKey> Entry<T>(TKey key, CborHelpers.CborEncodeDelegate<T> encoder, T? localData)
        where T : class
    {
        WriteKey(key);
        _cbor.WriteEncodedValue(encoder(localData));

        return this;
    }

    public CborMapWriter<TKey> OptionalEntry(TKey key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            return Entry(key, value!);
        }

        return this;
    }

    public CborMapWriter<TKey> OptionalEntry(TKey key, int? value)
    {
        if (value.HasValue)
        {
            return Entry(key, value.Value);
        }

        return this;
    }

    public CborMapWriter<TKey> OptionalEntry(TKey key, bool? value)
    {
        if (value.HasValue)
        {
            return Entry(key, value.Value);
        }

        return this;
    }

    public CborMapWriter<TKey> OptionalEntry(TKey key, ReadOnlyMemory<byte>? value)
    {
        if (value.HasValue)
        {
            return Entry(key, value.Value);
        }

        return this;
    }

    public CborMapWriter<TKey> OptionalEntry(TKey key, ICborEncode? value)
    {
        if (value is not null)
        {
            return Entry(key, value);
        }

        return this;
    }

    // An encoder is always provided. If the return value is an empty
    // byte array, then treat it as an option not exercised, that is,
    // don't write anything out.
    public CborMapWriter<TKey> OptionalEntry<T>(TKey key, CborHelpers.CborEncodeDelegate<T> encoder, T? localData)
        where T : class
    {
        byte[] encoding = encoder(localData);
        if (encoding.Length != 0)
        {
            WriteKey(key);
            _cbor.WriteEncodedValue(encoding);
        }

        return this;
    }

    public void EndMap() => _cbor.WriteEndMap();

    public byte[] Encode()
    {
        _cbor.WriteEndMap();
        return _cbor.Encode();
    }

    private void WriteKey(object? key)
    {
        switch (key)
        {
            case int intKey:
                _cbor.WriteInt32(intKey);
                break;
            case string strKey:
                _cbor.WriteTextString(strKey);
                break;
            default:
                throw new ArgumentException("Unsupported key type.");
        }
    }

    private void WriteEntry<T, TValue>(T key, TValue value)
    {
        WriteKey(key);
        WriteValue(value);
    }

    private void WriteValue<TValue>(TValue value)
    {
        switch (value)
        {
            case string strValue:
                _cbor.WriteTextString(strValue);
                break;
            case int intValue:
                _cbor.WriteInt32(intValue);
                break;
            case bool boolValue:
                _cbor.WriteBoolean(boolValue);
                break;
            case ReadOnlyMemory<byte> byteStringValue:
                _cbor.WriteByteString(byteStringValue.Span);
                break;
            default:
                throw new ArgumentException("Unsupported value type.");
        }
    }
}
