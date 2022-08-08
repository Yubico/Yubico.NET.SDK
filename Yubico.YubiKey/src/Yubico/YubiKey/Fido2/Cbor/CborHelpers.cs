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
using System.Collections.Generic;
using System.Formats.Cbor;

namespace Yubico.YubiKey.Fido2.Cbor
{
    /// <summary>
    /// Some helpers to make working with CBOR a little easier.
    /// </summary>
    internal static class CborHelpers
    {
        public class MapWriter
        {
            private readonly CborWriter _cbor;

            public MapWriter(CborWriter cbor)
            {
                if (cbor.ConvertIndefiniteLengthEncodings == false)
                {
                    throw new ArgumentException(ExceptionMessages.CborWriterMustConvertIdenfiteLengths);
                }

                _cbor = cbor;
                _cbor.WriteStartMap(null);
            }

            public MapWriter Entry(uint key, string value)
            {
                _cbor.WriteUInt32(key);
                _cbor.WriteTextString(value);

                return this;
            }

            public MapWriter Entry(uint key, uint value)
            {
                _cbor.WriteUInt32(key);
                _cbor.WriteUInt32(value);

                return this;
            }

            public MapWriter Entry(uint key, ReadOnlyMemory<byte> value)
            {
                _cbor.WriteUInt32(key);
                _cbor.WriteByteString(value.Span);

                return this;
            }

            public MapWriter OptionalEntry(uint key, string? value)
            {
                if (value is { })
                {
                    return Entry(key, value);
                }

                return this;
            }

            public MapWriter OptionalEntry(uint key, uint? value)
            {
                if (value.HasValue)
                {
                    return Entry(key, value.Value);
                }

                return this;
            }

            public MapWriter OptionalEntry(uint key, ReadOnlyMemory<byte>? value)
            {
                if (value.HasValue)
                {
                    return Entry(key, value.Value);
                }

                return this;
            }

            public void EndMap() => _cbor.WriteEndMap();
        }

        /// <summary>
        /// Use this method to write CBOR maps (which is mostly what CTAP2 uses). This uses a builder-like pattern
        /// where you can chain calls to add additional entries.
        /// </summary>
        /// <param name="cbor">
        /// An instance of a CborWriter. It must have the `ConvertIndefiniteLengthEncodings` option enabled.
        /// </param>
        /// <returns>
        /// An instance of the `MapWriter` builder class. You should not need to store this value anywhere. The intended
        /// use is to chain calls to its method like the following:
        /// <code language="C#">
        /// CborHelper.BeginMap(cborWriter)
        ///     .Entry(123, "abc")
        ///     .Entry(456, "def")
        ///     .OptionalEntry(2, maybeNullVariable)
        ///     .EndMap();
        /// </code>
        /// </returns>
        public static MapWriter BeginMap(CborWriter cbor) => new MapWriter(cbor);

    }

    /// <summary>
    /// Represents a CBOR map as a random-access dictionary.
    /// </summary>
    internal class CborMap
    {
        private readonly IDictionary<long, object?> _dict;

        /// <summary>
        /// Creates a new instance of <see cref="CborMap"/> based on a dictionary.
        /// </summary>
        /// <param name="dict">An integer keyed dictionary of objects representing a CBOR map.</param>
        public CborMap(IDictionary<long, object?> dict)
        {
            _dict = dict;
        }

        /// <summary>
        /// Creates a new instance of <see cref="CborMap"/> based on a CborReader.
        /// </summary>
        /// <param name="reader">
        /// A CborReader that is queued up at the start of a map.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The reader instance is null.
        /// </exception>
        public CborMap(CborReader reader)
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            _dict = ProcessMap(reader);
        }

        /// <summary>
        /// Checks to see whether a given key is present in the map, without throwing an exception.
        /// </summary>
        public bool Contains(long key) => _dict.ContainsKey(key);

        /// <summary>
        /// Read the value for the given key as a positive-integer `ulong`.
        /// </summary>
        public ulong ReadUInt64(long key)
        {
            object? value = _dict[key];

            if (value is ulong unboxedValue)
            {
                return unboxedValue;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Read the value for the given key as a negative-integer `ulong`.
        /// </summary>
        public ulong ReadNegativeInteger(long key)
        {
            object? value = _dict[key];

            if (value is ulong unboxedValue)
            {
                return unboxedValue;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Read the value for the given key as a byte array.
        /// </summary>
        public ReadOnlyMemory<byte> ReadByteString(long key)
        {
            object? value = _dict[key];

            if (value is byte[] bstr)
            {
                return bstr;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Read the value for the given key as a string.
        /// </summary>
        public string ReadTextString(long key)
        {
            object? value = _dict[key];

            if (value is string tstr)
            {
                return tstr;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Read the value for the given key as a nested map.
        /// </summary>
        public CborMap ReadMap(long key)
        {
            object? value = _dict[key];

            if (value is IDictionary<long, object?> nestedDict)
            {
                return new CborMap(nestedDict);
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Read the value for the given key as an array of objects.
        /// </summary>
        public object[] ReadArray(long key)
        {
            object? value = _dict[key];

            if (value is object[] arr)
            {
                return arr;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Read the value for the given key as a single-width floating point number.
        /// </summary>
        public float ReadSingle(long key)
        {
            object? value = _dict[key];

            if (value is float unboxedValue)
            {
                return unboxedValue;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Read the value for the given key as a double-width floating point number.
        /// </summary>
        public double ReadDouble(long key)
        {
            object? value = _dict[key];

            if (value is double unboxedValue)
            {
                return unboxedValue;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Read the given key as a "null" value - throw if there is a value.
        /// </summary>
        public void ReadNull(long key)
        {
            object? value = _dict[key];

            if (value is null)
            {
                return;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Read the value for the given key as a boolean.
        /// </summary>
        public bool ReadBoolean(long key)
        {
            object? value = _dict[key];

            if (value is bool unboxedValue)
            {
                return unboxedValue;
            }

            throw new InvalidCastException();
        }

        private IDictionary<long, object?> ProcessMap(CborReader cbor)
        {
            if (cbor.PeekState() != CborReaderState.StartMap)
            {
                throw new ArgumentException("Expected a CBOR map.");
            }

            var dict = new Dictionary<long, object?>();
            int? numberElements = cbor.ReadStartMap();

            if (numberElements is null)
            {
                return dict;
            }

            for (int i = 0; i < numberElements; i++)
            {
                // Technically the typecast from ulong -> long could truncate data, but in practice we do not expect
                // the map keys to be larger than a byte.
                long key;
                if (cbor.PeekState() == CborReaderState.NegativeInteger)
                {
                    key = 0 - (long)cbor.ReadCborNegativeIntegerRepresentation();
                }
                else
                {
                    key = (long)cbor.ReadUInt64();
                }

                object? value = ProcessSingleElement(cbor);

                dict.Add(key, value);
            }

            cbor.ReadEndMap();

            return dict;
        }

        private object? ProcessSingleElement(CborReader cbor) => cbor.PeekState() switch
        {
            CborReaderState.Undefined => null,
            CborReaderState.UnsignedInteger => cbor.ReadUInt64(),
            CborReaderState.NegativeInteger => cbor.ReadCborNegativeIntegerRepresentation(),
            CborReaderState.ByteString => cbor.ReadByteString(),
            CborReaderState.TextString => cbor.ReadTextString(),
            CborReaderState.StartMap => ProcessMap(cbor),
            CborReaderState.StartArray => ProcessArray(cbor),
            CborReaderState.SinglePrecisionFloat => cbor.ReadSingle(),
            CborReaderState.DoublePrecisionFloat => cbor.ReadDouble(),
            CborReaderState.Null => ProcessNull(cbor),
            CborReaderState.Boolean => cbor.ReadBoolean(),
            _ => throw new NotSupportedException()
        };

        private static object? ProcessNull(CborReader cbor)
        {
            cbor.ReadNull();
            return null;
        }

        private object? ProcessArray(CborReader cbor)
        {
            int? numberElements = cbor.ReadStartArray();

            if (numberElements is null)
            {
                throw new InvalidOperationException();
            }

            IList<object?> elements = new List<object?>(numberElements.Value);

            for (int i = 0; i < numberElements; i++)
            {
                elements[i] = ProcessSingleElement(cbor);
            }

            cbor.ReadEndArray();

            return elements;
        }
    }
}
