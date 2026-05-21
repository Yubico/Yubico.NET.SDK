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

namespace Yubico.YubiKit.Core.Utils;

/// <summary>
/// Utility for BER-TLV length encoding and decoding.
/// </summary>
public static class BerLength
{
    /// <summary>
    /// Returns the number of bytes needed to encode the given length (1, 2, or 3).
    /// </summary>
    public static int EncodingSize(int length) => length switch
    {
        > 0xFFFF => throw new ArgumentOutOfRangeException(nameof(length), "Length exceeds maximum BER-TLV encoding capacity."),
        > 255 => 3,
        > 127 => 2,
        _ => 1
    };

    /// <summary>
    /// Writes BER-TLV length into the destination span. Returns the number of bytes written.
    /// </summary>
    public static int Write(Span<byte> destination, int length)
    {
        switch (length)
        {
            case > 0xFFFF:
                throw new ArgumentOutOfRangeException(nameof(length));
            case > 255:
                destination[0] = 0x82;
                destination[1] = (byte)(length >> 8);
                destination[2] = (byte)(length & 0xFF);
                return 3;
            case > 127:
                destination[0] = 0x81;
                destination[1] = (byte)length;
                return 2;
            default:
                destination[0] = (byte)length;
                return 1;
        }
    }
}
