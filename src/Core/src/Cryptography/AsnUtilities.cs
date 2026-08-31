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

namespace Yubico.YubiKit.Core.Cryptography;

internal static class AsnUtilities
{
    public static ReadOnlySpan<byte> TrimLeadingZeroes(ReadOnlySpan<byte> data)
    {
        var startIndex = GetLeadingZeroCount(data);
        return data.Slice(startIndex);
    }

    public static Span<byte> TrimLeadingZeroes(Span<byte> data)
    {
        var startIndex = GetLeadingZeroCount(data);
        return data.Slice(startIndex);
    }

    public static int GetCoordinateSizeFromCurve(string curveOid)
    {
        var keyDef = KeyDefinitions.GetByOid(curveOid);
        return keyDef.LengthInBytes;
    }

    // Ensures the integer value is treated as positive by adding a leading zero if needed
    public static byte[] EnsurePositive(byte[]? value)
    {
        if (value is null || value.Length == 0)
        {
            return [];
        }
        // Check if the most significant bit is set, indicating a negative number in two's complement
        if ((value[0] & 0x80) != 0)
        {
            var padded = new byte[value.Length + 1];
            padded[0] = 0x0; // Add leading zero
            Buffer.BlockCopy(value, 0, padded, 1, value.Length);
            return padded;
        }

        return value;
    }

    public static Span<byte> GetIntegerBytes(Span<byte> value)
    {
        if (value.Length == 0)
        {
            return new byte[] { 0x00 }.AsSpan();
        }

        // Check if the most significant bit is set, indicating a negative number
        // if so, add a leading zero to indicate a positive number
        var trimmedBytes = TrimLeadingZeroes(value);
        if ((trimmedBytes[0] & 0x80) != 0)
        {
            var padded = new byte[trimmedBytes.Length + 1];
            padded[0] = 0x00;
            trimmedBytes.CopyTo(padded.AsSpan(1));
            return padded;
        }

        return trimmedBytes;
    }

    /// <summary>
    /// Returns a newly allocated buffer containing the minimal positive ASN.1 INTEGER content octets.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="GetIntegerBytes(Span{byte})"/>, this method never aliases the input.
    /// The caller owns the returned buffer and is responsible for zeroing it when it contains
    /// sensitive material.
    /// </remarks>
    public static byte[] GetOwnedIntegerBytes(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
        {
            return [0x00];
        }

        var trimmedBytes = TrimLeadingZeroes(value);
        var paddingLength = (trimmedBytes[0] & 0x80) != 0 ? 1 : 0;
        var ownedBytes = new byte[trimmedBytes.Length + paddingLength];
        trimmedBytes.CopyTo(ownedBytes.AsSpan(paddingLength));
        return ownedBytes;
    }
    private static int GetLeadingZeroCount(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return 0;
        }

        var startIndex = 0;
        while (startIndex < data.Length && data[startIndex] == 0)
        {
            startIndex++;
        }

        return startIndex == data.Length // reached the end, all bytes were zero
            ? data.Length - 1 // return last byte position
            : startIndex; // return first non-zero byte position
    }
}
