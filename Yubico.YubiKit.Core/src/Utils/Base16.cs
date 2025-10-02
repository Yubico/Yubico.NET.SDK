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

using System.Globalization;

namespace Yubico.YubiKit.Core.Utils;

/// <summary>
///     Class for encoding and decoding bytes into base-16 encoded text, otherwise known as
///     hexadecimal.
/// </summary>
/// <remarks>
///     <para>
///         This base class is a fully functional encoder/decoder for base-16, also known as
///         hexadecimal. The class <see cref="Hex" /> is an alias so that code using that class
///         can continue unmodified. New code should use this class.
///     </para>
///     <para>
///         See RFC4648 for details (https://datatracker.ietf.org/doc/html/rfc4648) on base-16.
///     </para>
/// </remarks>
public class Base16 : ITextEncoding
{
    private readonly Memory<char> _characterSet = "0123456789ABCDEF".ToCharArray();

    /// <summary>
    ///     The set of characters that correspond to numbers 0 - 16.
    /// </summary>
    protected virtual Span<char> CharacterSet => _characterSet.Span;

    /// <summary>
    ///     Indicates the default case of characters for this encoding.
    /// </summary>
    /// <remarks>
    ///     This is used when decoding data to check for characters that are in
    ///     an unexpected case. To match nibbles (4-bit values) to a character,
    ///     we must change the case of the character to match what's expected.
    ///     For example, the reference string for ModHex is <c>cbdefghijklnrtuv</c>.
    ///     If we receive a 16-bit value in ModHex that looks like <c>CCCB</c>,
    ///     then the matching algorithm needs to know to change each character
    ///     as it is being evaluated to <c>cccb</c>.
    /// </remarks>
    protected virtual bool DefaultLowerCase => false;

    #region ITextEncoding Version

    /// <inheritdoc />
    public void Encode(ReadOnlySpan<byte> data, Span<char> encoded)
    {
        if (data.Length > encoded.Length * 2)
            throw new ArgumentException(
                nameof(encoded),
                "ExceptionMessages.EncodingOverflow");

        for (var i = 0; i < data.Length; ++i)
        {
            var highestDigit = CharacterSet.Length - 1;
            var digit1 = data[i] >> 4;
            // Checking these so that BCD encode throws the right exception.
            if (digit1 > highestDigit)
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "ExceptionMessages.InvalidDigit",
                        _characterSet.Span[digit1],
                        _characterSet.Span[highestDigit]),
                    nameof(data));

            encoded[i * 2] = CharacterSet[digit1];

            var digit2 = data[i] & 0x0f;
            if (digit2 > highestDigit)
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "ExceptionMessages.InvalidDigit",
                        _characterSet.Span[digit2],
                        _characterSet.Span[highestDigit]),
                    nameof(data));

            encoded[(i * 2) + 1] = CharacterSet[digit2];
        }
    }

    /// <inheritdoc />
    public string Encode(ReadOnlySpan<byte> data)
    {
        var encoded = new char[data.Length * 2];
        Encode(data, encoded.AsSpan());
        return new string(encoded);
    }

    /// <inheritdoc />
    public void Decode(ReadOnlySpan<char> encoded, Span<byte> data)
    {
        if (encoded.Length % 2 != 0) throw new ArgumentException("ExceptionMessages.HexNotEvenLength");

        if (data.Length < encoded.Length / 2) throw new ArgumentException("ExceptionMessages.DecodingOverflow");

        for (var i = 0; i < encoded.Length; ++i)
            data[i / 2] = (byte)((GetNibble(encoded[i]) << 4) + GetNibble(encoded[++i]));

        // The decoding needs to be case-insensitive. This lets us handle that
        // in cases where the norm is upper or lower.
        char HandleCase(char c)
        {
            return DefaultLowerCase
                ? (char)((c - 0x40) * (0x5b - c) > 0 ? c + 0x20 : c)
                : (char)((c - 0x60) * (0x7b - c) > 0 ? c - 0x20 : c);
        }

        int GetNibble(char c)
        {
            var n = CharacterSet.IndexOf(HandleCase(c));
            if (n == -1)
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "ExceptionMessages.IllegalCharacter",
                        c));

            return n;
        }
    }

    /// <inheritdoc />
    public byte[] Decode(string encoded)
    {
        ArgumentNullException.ThrowIfNull(encoded);

        var bytes = new byte[encoded.Length / 2];
        Decode(encoded.AsSpan(), bytes);
        return bytes;
    }

    #endregion

    #region Static Version

    /// <inheritdoc cref="Encode(ReadOnlySpan{byte}, Span{char})" />
    public static void EncodeBytes(ReadOnlySpan<byte> data, Span<char> encoded) => new Base16().Encode(data, encoded);

    /// <inheritdoc cref="Encode(ReadOnlySpan{byte})" />
    public static string EncodeBytes(ReadOnlySpan<byte> data) => new Base16().Encode(data);

    /// <inheritdoc cref="Decode(ReadOnlySpan{char}, Span{byte})" />
    public static void DecodeText(ReadOnlySpan<char> encoded, Span<byte> data) => new Base16().Decode(encoded, data);

    /// <inheritdoc cref="Decode(string)" />
    public static byte[] DecodeText(string encoded) => new Base16().Decode(encoded);

    #endregion
}