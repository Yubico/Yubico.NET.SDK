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

namespace Yubico.YubiKey.Fido2.Commands
{
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

        public static MapWriter BeginMap(CborWriter cbor) => new MapWriter(cbor);
    }
}
