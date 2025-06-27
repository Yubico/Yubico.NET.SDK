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
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography;

internal static class AsnUtilities
{
    public static ReadOnlySpan<byte> TrimLeadingZeroes(ReadOnlySpan<byte> data)
    {
        int startIndex = GetLeadingZeroCount(data);
        return data.Slice(startIndex);
    }

    public static Span<byte> TrimLeadingZeroes(Span<byte> data)
    {
        int startIndex = GetLeadingZeroCount(data);
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
        if (value == null || value.Length == 0)
        {
            return [];
        }
        // Check if the most significant bit is set, indicating a negative number in two's complement
        if ((value[0] & 0x80) != 0)
        {
            byte[] padded = new byte[value.Length + 1];
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
            byte[] padded = new byte[trimmedBytes.Length + 1];
            padded[0] = 0x00;
            trimmedBytes.CopyTo(padded.AsSpan(1));
            return padded;
        }

        return trimmedBytes;
    }

    /// <summary>
    /// Verifies that the given X25519 private key meets the bit clamping requirements of RFC 7748.
    /// </summary>
    /// <param name="x25519PrivateKey">The X25519 private key to verify.</param>
    /// <exception cref="CryptographicException">If the private key does not meet the bit clamping requirements.</exception>
    public static void VerifyX25519PrivateKey(ReadOnlySpan<byte> x25519PrivateKey)
    {
        if ((x25519PrivateKey[0] & 0b111) != 0 || // Check that the 3 least significant bits are set
            (x25519PrivateKey[31] & 0x80) != 0 || // Check most significant bit is set
            (x25519PrivateKey[31] & 0x40) != 0x40) // Check second most significant bit not set
        {
            throw new CryptographicException("Invalid X25519 private key: improper bit clamping");
        }
    }

    private static int GetLeadingZeroCount(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return 0;
        }

        int startIndex = 0;
        while (startIndex < data.Length && data[startIndex] == 0)
        {
            startIndex++;
        }

        return startIndex == data.Length // reached the end, all bytes were zero
            ? data.Length - 1 // return last byte position
            : startIndex; // return first non-zero byte position
    }
}
