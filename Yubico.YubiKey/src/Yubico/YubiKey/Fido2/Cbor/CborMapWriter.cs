// Copyright 2022 Yubico AB
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
using System.Formats.Cbor;

namespace Yubico.YubiKey.Fido2.Cbor
{
    /// <summary>
    /// Some helpers to make writing CBOR maps a little easier.
    /// </summary>
    internal class CborMapWriter<TKey>
    {
        private readonly CborWriter _cbor;

        public CborMapWriter(CborWriter cbor)
        {
            if (cbor.ConvertIndefiniteLengthEncodings == false)
            {
                throw new ArgumentException(ExceptionMessages.CborWriterMustConvertIdenfiteLengths);
            }

            _cbor = cbor;
            _cbor.WriteStartMap(null);
        }

        private void WriteKey(TKey key)
        {
            if (key is long longKey)
            {
                _cbor.WriteInt64(longKey);
            }
            else if (key is string strKey)
            {
                _cbor.WriteTextString(strKey);
            }
            else
            {
                throw new ArgumentException("Unsupported key type.");
            }
        }

        public CborMapWriter<TKey> Entry(TKey key, string value)
        {
            WriteKey(key);
            _cbor.WriteTextString(value);

            return this;
        }

        public CborMapWriter<TKey> Entry(TKey key, long value)
        {
            WriteKey(key);
            _cbor.WriteInt64(value);

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

        // If the Encoder writes out an empty array, this will throw an
        // exception.
        public CborMapWriter<TKey> Entry<T>(TKey key, CborHelpers.CborEncodeDelegate<T> Encoder, T? localData) where T : class
        {
            WriteKey(key);
            _cbor.WriteEncodedValue(Encoder(localData));

            return this;
        }

        public CborMapWriter<TKey> OptionalEntry(TKey key, string? value)
        {
            if (value is { })
            {
                return Entry(key, value);
            }

            return this;
        }

        public CborMapWriter<TKey> OptionalEntry(TKey key, long? value)
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
            if (!(value is null))
            {
                return Entry(key, value);
            }

            return this;
        }

        // An encoder is always provided. If the return value is an empty
        // byte array, then treat it as an option not exercised, that is,
        // don't write anything out.
        public CborMapWriter<TKey> OptionalEntry<T>(TKey key, CborHelpers.CborEncodeDelegate<T> Encoder, T? localData) where T : class
        {
            byte[] encoding = Encoder(localData);
            if (encoding.Length != 0)
            {
                WriteKey(key);
                _cbor.WriteEncodedValue(encoding);
            }

            return this;
        }

        public void EndMap() => _cbor.WriteEndMap();
    }
}
