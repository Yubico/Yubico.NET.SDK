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
        public static void WriteMapEntry(CborWriter cbor, uint key, string value)
        {
            cbor.WriteUInt32(key);
            cbor.WriteTextString(value);
        }

        public static void WriteMapEntry(CborWriter cbor, uint key, uint value)
        {
            cbor.WriteUInt32(key);
            cbor.WriteUInt32(value);
        }

        public static void WriteMapEntry(CborWriter cbor, uint key, ReadOnlyMemory<byte> value)
        {
            cbor.WriteUInt32(key);
            cbor.WriteByteString(value.Span);
        }
    }
}
