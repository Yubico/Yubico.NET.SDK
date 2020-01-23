// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Source: https://github.com/dotnet/runtime/blob/53976d38b1bd6917b8fa4d1dd4f009728ece3adb/src/libraries/Common/tests/System/Security/Cryptography/ByteUtils.cs

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Formats.Cbor.UnitTests
{
    public static class TestExtensions
    {
        public static byte[] HexToByteArray(this string str)
        {
            if (str is null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            return HexToBytes(str);
        }

        public static string ByteArrayToHex(this byte[] arr)
        {
            if (arr is null)
            {
                throw new ArgumentNullException(nameof(arr));
            }

            return BytesToHex(arr);
        }

        public static string ByteArrayToHex(this ReadOnlyMemory<byte> arr)
        {
            return BytesToHex(arr.ToArray());
        }

        public static string ByteArrayToHex(this Span<byte> arr)
        {
            return BytesToHex(arr.ToArray());
        }

        private static string BytesToHex(byte[] bs)
        {
            return BitConverter.ToString(bs).Replace("-", "", StringComparison.Ordinal);
        }

        private static byte[] HexToBytes(string hexString)
        {
            if (hexString is null)
            {
                throw new ArgumentNullException(nameof(hexString));
            }

            int byteLen = hexString.Length / 2;
            byte[] bytes = new byte[byteLen];
            for (int i = 0; i < byteLen; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(2 * i, 2), 16);
            }

            return bytes;
        }
    }
}
