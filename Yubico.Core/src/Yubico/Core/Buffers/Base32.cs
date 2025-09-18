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
using System.Globalization;

namespace Yubico.Core.Buffers;

/// <summary>
///     Class for encoding and decoding bytes into Base32.
/// </summary>
/// <remarks>See RFC4648 for details (https://datatracker.ietf.org/doc/html/rfc4648) on base-32.</remarks>
public class Base32 : ITextEncoding
{
    private const int Base32Mask = 0x1f;

    #region ITextEncoding Version

    /// <inheritdoc />
    public string Encode(ReadOnlySpan<byte> data)
    {
        // Each char represents one Base32 digit (five bytes).
        int encodedSize = GetEncodedSize(data.Length);
        char[] encoded = new char[encodedSize];

        Encode(data, encoded.AsSpan());

        return new string(encoded);
    }

    /// <inheritdoc />
    public void Encode(ReadOnlySpan<byte> data, Span<char> encoded)
    {
        int encodedSize = GetEncodedSize(data.Length);
        if (encoded.Length < encodedSize)
        {
            throw new ArgumentException(
                nameof(encoded),
                ExceptionMessages.EncodingOverflow);
        }

        int ch = 0, bits = 5, index = 0;
        foreach (byte b in data)
        {
            ch |= b >> 8 - bits;
            encoded[index++] = EncodeBase32Digit(ch);

            if (bits < 4)
            {
                ch = b >> 3 - bits & Base32Mask;
                encoded[index++] = EncodeBase32Digit(ch);
                bits += 5;
            }

            bits -= 3;
            ch = b << bits & Base32Mask;
        }

        // Handle stray bits at the end.
        if (index != encodedSize)
        {
            encoded[index++] = EncodeBase32Digit(ch);
            encoded[index..].Fill('=');
        }
    }

    /// <inheritdoc />
    public void Decode(ReadOnlySpan<char> encoded, Span<byte> data)
    {
        int byteCount = GetDecodedSize(encoded);
        if (byteCount > data.Length)
        {
            throw new ArgumentException(nameof(data), ExceptionMessages.DecodingOverflow);
        }

        // If we were being pedantic, we could verify that the string has
        // the right padding. I don't think we need to, though.
        encoded = StripPadding(encoded);

        byte curByte = 0, bits = 8;
        int mask, index = 0;
        foreach (int c in encoded)
        {
            // Base32 isn't supposed to include lower-case letters, but
            // some of our tests do, so we'll just do a cheap ToUpper.
            int base32 = (c - 0x60) * (0x7b - c) > 0
                ? c - 0x20
                : c;

            // The five bits (0 - 0x1f) are represented by A-Z and 2-7.
            if ((base32 - 0x40) * (0x5b - base32) > 0) // Handle A-Z.
            {
                base32 -= 0x41;
            }
            else if ((base32 - 0x31) * (0x38 - base32) > 0) // Handle 2 - 7.
            {
                base32 -= 0x18;
            }
            else
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IllegalCharacter,
                        c),
                    nameof(encoded));
            }

            if (bits > 5)
            {
                mask = base32 << bits - 5;
                curByte = (byte)(curByte | mask);
                bits -= 5;
            }
            else
            {
                mask = base32 >> 5 - bits;
                curByte = (byte)(curByte | mask);
                data[index++] = curByte;
                curByte = (byte)(base32 << 3 + bits);
                bits += 3;
            }
        }

        // If there are stray bits, handle them.
        if (index != byteCount)
        {
            data[index] = curByte;
        }
    }

    /// <inheritdoc />
    public byte[] Decode(string encoded)
    {
        if (encoded is null)
        {
            throw new ArgumentNullException(nameof(encoded));
        }

        byte[] data = new byte[GetDecodedSize(encoded.AsSpan())];
        Decode(encoded.AsSpan(), data);
        return data;
    }

    #endregion

    #region Static Version

    /// <inheritdoc cref="Encode(ReadOnlySpan{byte}, Span{char})" />
    public static void EncodeBytes(ReadOnlySpan<byte> data, Span<char> encoded) => new Base32().Encode(data, encoded);

    /// <inheritdoc cref="Encode(ReadOnlySpan{byte})" />
    public static string EncodeBytes(ReadOnlySpan<byte> data) => new Base32().Encode(data);

    /// <inheritdoc cref="Decode(ReadOnlySpan{char}, Span{byte})" />
    public static void DecodeText(ReadOnlySpan<char> encoded, Span<byte> data) => new Base32().Decode(encoded, data);

    /// <inheritdoc cref="Decode(string)" />
    public static byte[] DecodeText(string encoded) => new Base32().Decode(encoded);

    #endregion

    #region Static Utility Methods

    /// <summary>
    ///     Gets the number of characters needed to encode the data.
    /// </summary>
    /// <remarks>
    ///     The other encoding classes don't have the Get*Size methods. However,
    ///     base-32 encoding represents five bits per character, and padding has
    ///     to be accounted for, so it's a complex enough of an operation to
    ///     merit a helper.
    /// </remarks>
    /// <param name="lengthInBytes">The length of the data to encode.</param>
    /// <returns>The number of characters needed.</returns>
    public static int GetEncodedSize(int lengthInBytes) =>
        (lengthInBytes / 5 + (lengthInBytes % 5 > 0
            ? 1
            : 0)) * 8;

    /// <summary>
    ///     Get the number of bytes in data represented by base-32 encoded text.
    /// </summary>
    /// <inheritdoc cref="GetEncodedSize(int)" path="/remarks" />
    /// <param name="encoded">The text to be decoded.</param>
    /// <returns>An <see cref="int" /> representing the number of bytes.</returns>
    public static int GetDecodedSize(ReadOnlySpan<char> encoded) => StripPadding(encoded).Length * 5 / 8;

    private static ReadOnlySpan<char> StripPadding(ReadOnlySpan<char> encoded)
    {
        int length = encoded.Length;
        if (length > 0)
        {
            while (encoded[length - 1] == '=')
            {
                --length;
            }
        }

        return encoded[..length];
    }

    private static char EncodeBase32Digit(int b) =>
        b < 0x1a // If the value is less than 0x1a, it's an upper case letter.
            ? (char)(b + 0x41) // So add the value of 'A'.
            : b < 0x20 // Otherwise, if the value is less than 0x20, it's a number (2 - 7).
                ? (char)(b + 0x18) // So add the value of '2'.
                : throw new ArgumentException( // Nope, no soup for you.
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidBase32Digit, b),
                    nameof(b));

    #endregion
}
