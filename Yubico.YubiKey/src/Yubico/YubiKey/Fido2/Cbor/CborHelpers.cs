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
}
