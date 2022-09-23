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
using System.Globalization;
using System.Collections.Generic;
using System.Formats.Cbor;

namespace Yubico.YubiKey.Fido2.Cbor
{
    /// <summary>
    /// Represents a CBOR map as a random-access dictionary. This is used to read
    /// a map in a CBOR-encoded construction.
    /// </summary>
    internal class CborMap<TKey>
    {
        private readonly IDictionary<TKey, object?> _dict;

        /// <summary>
        /// Creates a new instance of <see cref="CborMap{TKey}"/> based on a dictionary.
        /// </summary>
        /// <param name="dict">An integer keyed dictionary of objects representing a CBOR map.</param>
        public CborMap(IDictionary<TKey, object?> dict)
        {
            _dict = dict;
        }

        /// <summary>
        /// Creates a new instance of <see cref="CborMap{TKey}"/> based on a CborReader.
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

            _dict = ProcessMap(reader) as IDictionary<TKey, object?> ??
                throw new InvalidOperationException(ExceptionMessages.TypeMismatch);
        }

        /// <summary>
        /// Checks to see whether a given key is present in the map, without throwing an exception.
        /// </summary>
        public bool Contains(TKey key) => _dict.ContainsKey(key);

        /// <summary>
        /// Read the value for the given key as a signed integer `long`.
        /// </summary>
        public long ReadInt64(TKey key)
        {
            object? value = _dict[key];

            if (value is long unboxedValue)
            {
                return unboxedValue;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Read the value for the given key as a byte array.
        /// </summary>
        public ReadOnlyMemory<byte> ReadByteString(TKey key)
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
        public string ReadTextString(TKey key)
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
        public CborMap<TNestedKey> ReadMap<TNestedKey>(TKey key)
        {
            object? value = _dict[key];

            if (value is IDictionary<TNestedKey, object?> nestedDict)
            {
                return new CborMap<TNestedKey>(nestedDict);
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Read the value for the given key as an array of objects.
        /// </summary>
        public object[] ReadArray(TKey key)
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
        public float ReadSingle(TKey key)
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
        public double ReadDouble(TKey key)
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
        public void ReadNull(TKey key)
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
        public bool ReadBoolean(TKey key)
        {
            object? value = _dict[key];

            if (value is bool unboxedValue)
            {
                return unboxedValue;
            }

            throw new InvalidCastException();
        }

        private object ProcessMap(CborReader cbor)
        {
            if (cbor.PeekState() != CborReaderState.StartMap)
            {
                throw new Ctap2DataException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.Ctap2MissingRequiredField));
            }

            int? numberElements = cbor.ReadStartMap();

            if (numberElements is null)
            {
                return new Dictionary<object, object>();
            }

            CborReaderState cborType = cbor.PeekState();

            switch (cborType)
            {
                case CborReaderState.UnsignedInteger:
                case CborReaderState.NegativeInteger:
                    return ProcessMap<long>(cbor, numberElements.Value);
                case CborReaderState.TextString:
                    return ProcessMap<string>(cbor, numberElements.Value);
                default:
                    throw new InvalidOperationException(ExceptionMessages.TypeNotSupported);
            }
        }

        private IDictionary<TNestedKey, object?> ProcessMap<TNestedKey>(CborReader cbor, int numberOfElements)
        {
            var dict = new Dictionary<TNestedKey, object?>();

            for (int i = 0; i < numberOfElements; i++)
            {
                // Technically the typecast from ulong -> long could truncate data, but in practice we do not expect
                // the map keys to be larger than a byte.
                TNestedKey key = ReadKey<TNestedKey>(cbor);

                object? value = ProcessSingleElement(cbor);

                dict.Add(key, value);
            }

            cbor.ReadEndMap();

            return dict;
        }

        private static TReadKey ReadKey<TReadKey>(CborReader cbor)
        {
            if (typeof(TReadKey) == typeof(long))
            {
                return (TReadKey)Convert.ChangeType(cbor.ReadInt64(), typeof(TReadKey), CultureInfo.InvariantCulture);
            }

            if (typeof(TReadKey) == typeof(string))
            {
                return (TReadKey)Convert.ChangeType(cbor.ReadTextString(), typeof(TReadKey), CultureInfo.InvariantCulture);
            }

            throw new InvalidOperationException(ExceptionMessages.TypeNotSupported);
        }

        private object? ProcessSingleElement(CborReader cbor) => cbor.PeekState() switch
        {
            CborReaderState.Undefined => null,
            CborReaderState.UnsignedInteger => cbor.ReadInt64(),
            CborReaderState.NegativeInteger => cbor.ReadInt64(),
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
                elements.Add(ProcessSingleElement(cbor));
            }

            cbor.ReadEndArray();

            return elements;
        }
    }
}
