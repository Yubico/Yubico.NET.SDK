// Copyright (c) Yubico AB
// Licensed under the Apache License, Version 2.0.

using System;
using System.Security.Cryptography;
using System.Text;

namespace Yubico.YubiKit.Piv;

/// <summary>
/// Utilities for encoding PIN and PUK values according to PIV specification.
/// </summary>
/// <remarks>
/// <para>
/// PIV PIN/PUK encoding requires:
/// - UTF-8 encoding of the PIN characters
/// - Maximum 8 bytes
/// - Padding with 0xFF to reach 8-byte length
/// - Empty PIN is encoded as 8 bytes of 0xFF
/// </para>
/// </remarks>
internal static class PivPinUtilities
{
    /// <summary>
    /// The fixed length for encoded PIN/PUK values per PIV specification.
    /// </summary>
    public const int PinLength = 8;

    /// <summary>
    /// Encodes a PIN or PUK value into the 8-byte format required by PIV specification.
    /// </summary>
    /// <param name="pin">The PIN/PUK characters to encode. Empty array encodes as all 0xFF.</param>
    /// <param name="destination">The destination buffer (must be at least 8 bytes).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the PIN exceeds 8 bytes when UTF-8 encoded.
    /// </exception>
    /// <remarks>
    /// The PIN is UTF-8 encoded, then padded with 0xFF bytes to reach 8 bytes.
    /// An empty PIN results in 8 bytes of 0xFF.
    /// </remarks>
    public static void EncodePinBytes(ReadOnlySpan<char> pin, Span<byte> destination)
    {
        if (destination.Length < PinLength)
        {
            throw new ArgumentException($"Destination must be at least {PinLength} bytes.", nameof(destination));
        }

        // Fill with 0xFF padding first
        destination[..PinLength].Fill(0xFF);

        if (pin.IsEmpty)
        {
            // Empty PIN = all 0xFF (already filled)
            return;
        }

        // Encode PIN as UTF-8
        int byteCount = Encoding.UTF8.GetByteCount(pin);
        if (byteCount > PinLength)
        {
            throw new ArgumentException($"PIN/PUK must be no longer than {PinLength} bytes when UTF-8 encoded.", nameof(pin));
        }

        Encoding.UTF8.GetBytes(pin, destination);
        // Remainder is already 0xFF from the Fill() above
    }

    /// <summary>
    /// Encodes two PIN/PUK values for change reference operations.
    /// </summary>
    /// <param name="currentPin">The current PIN/PUK value.</param>
    /// <param name="newPin">The new PIN/PUK value.</param>
    /// <param name="destination">The destination buffer (must be at least 16 bytes).</param>
    /// <remarks>
    /// Used for CHANGE REFERENCE DATA (INS 0x24) and RESET RETRY COUNTER (INS 0x2C) commands.
    /// The result is 16 bytes: first 8 bytes for current PIN, second 8 bytes for new PIN.
    /// </remarks>
    public static void EncodePinPair(ReadOnlySpan<char> currentPin, ReadOnlySpan<char> newPin, Span<byte> destination)
    {
        const int pairLength = PinLength * 2;
        if (destination.Length < pairLength)
        {
            throw new ArgumentException($"Destination must be at least {pairLength} bytes.", nameof(destination));
        }

        EncodePinBytes(currentPin, destination[..PinLength]);
        EncodePinBytes(newPin, destination[PinLength..pairLength]);
    }

    /// <summary>
    /// Encodes a PIN/PUK and returns a newly allocated array.
    /// </summary>
    /// <param name="pin">The PIN/PUK characters to encode.</param>
    /// <returns>An 8-byte array containing the encoded PIN.</returns>
    /// <remarks>
    /// Prefer <see cref="EncodePinBytes(ReadOnlySpan{char}, Span{byte})"/> with stackalloc
    /// to avoid heap allocations. This method is provided for convenience when allocation
    /// is acceptable.
    /// </remarks>
    public static byte[] EncodePinBytes(ReadOnlySpan<char> pin)
    {
        byte[] result = new byte[PinLength];
        EncodePinBytes(pin, result);
        return result;
    }

    /// <summary>
    /// Encodes a PIN pair and returns a newly allocated array.
    /// </summary>
    /// <param name="currentPin">The current PIN/PUK value.</param>
    /// <param name="newPin">The new PIN/PUK value.</param>
    /// <returns>A 16-byte array containing the encoded PIN pair.</returns>
    public static byte[] EncodePinPair(ReadOnlySpan<char> currentPin, ReadOnlySpan<char> newPin)
    {
        byte[] result = new byte[PinLength * 2];
        EncodePinPair(currentPin, newPin, result);
        return result;
    }

    /// <summary>
    /// Encodes a PIN pair from raw bytes and returns a newly allocated array.
    /// </summary>
    /// <param name="currentPin">The current PIN/PUK value as raw bytes.</param>
    /// <param name="newPin">The new PIN/PUK value as raw bytes.</param>
    /// <returns>A 16-byte array containing the encoded PIN pair.</returns>
    /// <remarks>
    /// This overload is for when the PIN/PUK is already available as raw bytes.
    /// The bytes are padded with 0xFF to reach 8 bytes each.
    /// </remarks>
    public static byte[] EncodePinPairBytes(ReadOnlySpan<byte> currentPin, ReadOnlySpan<byte> newPin)
    {
        if (currentPin.Length > PinLength)
        {
            throw new ArgumentException($"PIN/PUK must be no longer than {PinLength} bytes.", nameof(currentPin));
        }
        if (newPin.Length > PinLength)
        {
            throw new ArgumentException($"PIN/PUK must be no longer than {PinLength} bytes.", nameof(newPin));
        }

        byte[] result = new byte[PinLength * 2];
        result.AsSpan().Fill(0xFF);
        currentPin.CopyTo(result.AsSpan(0, currentPin.Length));
        newPin.CopyTo(result.AsSpan(PinLength, newPin.Length));
        return result;
    }

    /// <summary>
    /// Parses the number of remaining retry attempts from a PIV status word.
    /// </summary>
    /// <param name="statusWord">The status word (SW1-SW2) from the response.</param>
    /// <returns>
    /// The number of remaining attempts, 0 if blocked, or -1 if the status word
    /// does not indicate retry information.
    /// </returns>
    /// <remarks>
    /// PIV uses SW 0x63CX where X is the remaining retry count (0-15).
    /// SW 0x6983 indicates the authentication method is blocked.
    /// </remarks>
    public static int GetRetriesFromStatusWord(int statusWord)
    {
        // 0x6983 = Authentication method blocked
        if (statusWord == 0x6983)
        {
            return 0;
        }

        // 0x63CX = X retries remaining
        if (statusWord >= 0x63C0 && statusWord <= 0x63CF)
        {
            return statusWord & 0x0F;
        }

        return -1;
    }
}
