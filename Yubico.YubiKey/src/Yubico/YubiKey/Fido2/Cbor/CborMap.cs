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
using System.Linq;
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
        /// The number of key/value pairs in this map.
        /// </summary>
        public int Count => _dict.Count;

        /// <summary>
        /// Creates a new instance of <see cref="CborMap{TKey}"/> based on a dictionary.
        /// </summary>
        /// <param name="dict">An integer keyed dictionary of objects representing a CBOR map.</param>
        public CborMap(IDictionary<TKey, object?> dict)
        {
            _dict = dict;
        }

        /// <summary>
        /// Creates a new instance of <see cref="CborMap{TKey}"/> based on the
        /// encoding.
        /// </summary>
        /// <param name="encoding">
        /// A byte array containing a CBOR encoding that is a map.
        /// </param>
        public CborMap(ReadOnlyMemory<byte> encoding)
            : this(new CborReader(encoding, CborConformanceMode.Ctap2Canonical))
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="CborMap{TKey}"/> based on the
        /// given <c>CborReader</c>.
        /// </summary>
        /// <param name="cbor">
        /// A <c>CborReader</c> that holds the data to decode.
        /// </param>
        public CborMap(CborReader cbor)
        {
            if (cbor.PeekState() != CborReaderState.StartMap)
            {
                throw new Ctap2DataException(ExceptionMessages.Ctap2MissingRequiredField);
            }

            int? numberElements = cbor.ReadStartMap();
            if (numberElements is null)
            {
                throw new NotSupportedException(ExceptionMessages.Ctap2CborIndefiniteLength);
            }

            int count = numberElements.Value;
            _dict = new Dictionary<TKey, object?>(count);

            while (count > 0)
            {
                TKey currentKey = ReadKey<TKey>(cbor);
                object? currentValue = ProcessSingleElement(cbor);
                _dict.Add(currentKey, currentValue);

                count--;
            }

            cbor.ReadEndMap();
        }

        /// <summary>
        /// Checks to see whether a given key is present in the map, without throwing an exception.
        /// </summary>
        public bool Contains(TKey key) => _dict.ContainsKey(key);

        /// <summary>
        /// Returns this map as an IDictionary of key/value pairs where the keys
        /// are all of type TKey and the values are all of the type TValue. If
        /// one or more of the map's values is not of the specified type, this
        /// will throw an exception.
        /// </summary>
        /// <remarks>
        /// This is genrally used to get a sub map out. For example, one element
        /// of the main map is a map itself, a map of key/value pairs where each
        /// key is a string and each value is a boolean (or a string). So get the
        /// sub map out (e.g. subMap = mainMap.ReadMap(4)), then get that
        /// map as a dictionary (e.g. subMap.AsDictionary()).
        /// </remarks>
        /// <returns>
        /// A new IDictionary representing this map.
        /// </returns>
        public IReadOnlyDictionary<TKey,TValue> AsDictionary<TValue>()
        {
            var returnValue = new Dictionary<TKey,TValue>(_dict.Count);
            foreach (KeyValuePair<TKey, object?> entry in _dict)
            {
                if (!(entry.Value is TValue targetValue))
                {
                    throw new InvalidCastException();
                }

                returnValue.Add(entry.Key, targetValue);
            }

            return returnValue;
        }

        /// <summary>
        /// Read the value for the given key as a nested map.
        /// </summary>
        public CborMap<TNestedKey> ReadMap<TNestedKey>(TKey key)
        {
            object? value = _dict[key];

            if (value is CborMap<TNestedKey> nestedMap)
            {
                return nestedMap;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Read the value for the given key as an array of TValue.
        /// </summary>
        public IReadOnlyList<TValue> ReadArray<TValue>(TKey key)
        {
            object? value = _dict[key];

            if (value is List<object?> entries)
            {
                var returnValue = new List<TValue>(entries.Count);

                int index = 0;
                for (; index < entries.Count; index++)
                {
                    if (!(entries[index] is TValue currentValue))
                    {
                        break;
                    }

                    returnValue.Add(currentValue);
                }

                if (index >= entries.Count)
                {
                    return returnValue;
                }
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// If the map does not contain the given key, return null, otherwise
        /// read the value as a "T".
        /// </summary>
        public object? ReadOptional<T>(TKey key)
        {
            if (!_dict.ContainsKey(key))
            {
                return null;
            }

            object? value = _dict[key];
            if (value is T typedValue)
            {
                return typedValue;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// Read the value for the given key as a signed integer.
        /// </summary>
        public int ReadInt32(TKey key)
        {
            object? value = _dict[key];

            if (value is int unboxedValue)
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

        private static TReadKey ReadKey<TReadKey>(CborReader cbor)
        {
            if (typeof(TReadKey) == typeof(int))
            {
                return (TReadKey)Convert.ChangeType(cbor.ReadInt32(), typeof(TReadKey), CultureInfo.InvariantCulture);
            }

            if (typeof(TReadKey) == typeof(string))
            {
                return (TReadKey)Convert.ChangeType(cbor.ReadTextString(), typeof(TReadKey), CultureInfo.InvariantCulture);
            }

            throw new InvalidOperationException(ExceptionMessages.TypeNotSupported);
        }

        private static object? ProcessSingleElement(CborReader cbor) => cbor.PeekState() switch
        {
            CborReaderState.Undefined => null,
            CborReaderState.UnsignedInteger => cbor.ReadInt32(),
            CborReaderState.NegativeInteger => cbor.ReadInt32(),
            CborReaderState.ByteString => cbor.ReadByteString(),
            CborReaderState.TextString => cbor.ReadTextString(),
            CborReaderState.StartMap => ProcessSubMap(cbor),
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

        private static object? ProcessArray(CborReader cbor)
        {
            int? entryCount = cbor.ReadStartArray();
            int count = entryCount ?? 0;

            if (count == 0)
            {
                return null;
            }

            var entries = new List<object?>(count);
            while (count > 0)
            {
                entries.Add(ProcessSingleElement(cbor));
                count--;
            }

            cbor.ReadEndArray();

            return entries;
        }

        private static object? ProcessSubMap(CborReader cbor)
        {
            ReadOnlyMemory<byte> encodedMap = cbor.ReadEncodedValue();
            var subCbor = new CborReader(encodedMap, CborConformanceMode.Ctap2Canonical);
            _ = subCbor.ReadStartMap();

            CborReaderState cborType = subCbor.PeekState();
            switch (cborType)
            {
                case CborReaderState.UnsignedInteger:
                case CborReaderState.NegativeInteger:
                    return new CborMap<int>(encodedMap);
                case CborReaderState.TextString:
                    return new CborMap<string>(encodedMap);
                default:
                    throw new InvalidOperationException(ExceptionMessages.TypeNotSupported);
            }
        }
    }
}
