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
using System.Formats.Cbor;

namespace Yubico.YubiKey.Fido2.Cbor;

internal static class CborExtensions
{
    public readonly static byte[] CborTrue = [CborHelpers.True];
    public readonly static byte[] CborFalse = [CborHelpers.False];
        
    public static byte[] ToCbor(this bool value) => value ? CborTrue : CborFalse;

    public static byte[] ToCbor(this string value)
    {
        var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
        cbor.WriteTextString(value);
        return cbor.Encode();
    }

    public static byte[] ToCbor(this int value)
    {
        var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
        cbor.WriteInt32(value);
        return cbor.Encode();
    }
        
    public static byte[] ToCbor(this byte value)
    {
        var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
        cbor.WriteInt32(value);
        return cbor.Encode();
    }

    public static byte[] ToCbor(this long value)
    {
        var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
        cbor.WriteInt64(value);
        return cbor.Encode();
    }

    public static byte[] ToCbor(this byte[] value)
    {
        var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
        cbor.WriteByteString(value);
        return cbor.Encode();
    }
    
    [CLSCompliant(false)]
    public static byte[] ToCbor(this ulong value)
    {
        var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
        cbor.WriteUInt64(value);
        return cbor.Encode();
    }
    [CLSCompliant(false)]
    public static byte[] ToCbor(this uint value)
    {
        var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
        cbor.WriteUInt32(value);
        return cbor.Encode();
    }
}
