// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.Utilities;

/// <summary>Decodes RFC 4648 Base32 text without creating transformed text copies.</summary>
/// <remarks>
///     Accepts ASCII upper/lower case, ignores literal ASCII spaces, and accepts either complete padding
///     or no padding. Unused bits in the last symbol are ignored, not required to be zero. Empty text is valid.
/// </remarks>
public static class Base32
{
    /// <summary>Decodes Base32 text into independently owned storage.</summary>
    /// <param name="encoded">The borrowed encoded text.</param>
    /// <returns>The decoded bytes. The caller must clear them when they contain secret material.</returns>
    /// <exception cref="ArgumentException">The text has invalid characters, length, or padding.</exception>
    public static byte[] Decode(ReadOnlySpan<char> encoded)
    {
        int symbols = 0;
        foreach (char c in encoded)
        {
            if (c is not (' ' or '=')) { symbols++; }
        }

        byte[] result = new byte[(int)((long)symbols * 5 / 8)];
        Decode(encoded, result); // The span overload clears result on every failure.
        return result;
    }

    /// <summary>Decodes Base32 text into caller-owned storage and returns the number of bytes written.</summary>
    /// <param name="encoded">The borrowed encoded text.</param>
    /// <param name="destination">The output buffer; unused bytes are unchanged on success.</param>
    /// <returns>The number of decoded bytes written.</returns>
    /// <remarks>
    ///     On any decoding failure, the entire destination is cleared, including any partially decoded prefix.
    ///     On success, the caller owns the output and must clear secret material after use.
    /// </remarks>
    /// <exception cref="ArgumentException">The text is malformed or the destination is too small.</exception>
    public static int Decode(ReadOnlySpan<char> encoded, Span<byte> destination)
    {
        try { return DecodeCore(encoded, destination); }
        catch
        {
            CryptographicOperations.ZeroMemory(destination);
            throw;
        }
    }

    private static int DecodeCore(ReadOnlySpan<char> encoded, Span<byte> destination)
    {
        int symbols = 0, padding = 0, buffer = 0, bits = 0, written = 0;
        foreach (char c in encoded)
        {
            if (c == ' ') { continue; }
            if (c == '=') { padding++; continue; }
            int value = DecodeSymbol(c);
            if (padding != 0 || value < 0)
            {
                throw new ArgumentException("Invalid Base32 character or padding.", nameof(encoded));
            }

            symbols++;
            buffer = (buffer << 5) | value;
            bits += 5;
            if (bits >= 8)
            {
                if (written == destination.Length)
                {
                    throw new ArgumentException("Base32 destination is too small.", nameof(destination));
                }

                bits -= 8;
                destination[written++] = (byte)(buffer >> bits);
                buffer &= (1 << bits) - 1;
            }
        }

        ValidateTerminalLength(symbols, padding);
        return written;
    }

    private static void ValidateTerminalLength(int symbols, int padding)
    {
        int requiredPadding = (symbols % 8) switch
        {
            0 => 0,
            2 => 6,
            4 => 4,
            5 => 3,
            7 => 1,
            _ => -1
        };
        if (requiredPadding < 0 || (padding != 0 && padding != requiredPadding))
        {
            throw new ArgumentException("Invalid Base32 terminal length or padding.");
        }
    }

    private static int DecodeSymbol(char value) => value switch
    {
        >= 'A' and <= 'Z' => value - 'A',
        >= 'a' and <= 'z' => value - 'a',
        >= '2' and <= '7' => value - '2' + 26,
        _ => -1
    };
}