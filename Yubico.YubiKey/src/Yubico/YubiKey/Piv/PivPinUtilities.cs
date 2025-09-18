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

namespace Yubico.YubiKey.Piv;

/// <summary>
///     This internal class contains utilities to help process PIV PINs and PUKs.
/// </summary>
/// <remarks>
///     The standard specifies that a PIN is 6 to 8 characters, where each
///     character is an ASCII number. That is, it is 6 to 8 bytes, and each byte
///     must be from the set <c>{ 0x30, 31, ..., 39 }</c>. Tools from Yubico
///     generally allow any ASCII character for a PIN.
///     <para>
///         The standard specifies the PUK (PIN Unblocking Key) as being 8 bytes
///         long. The bytes can be any binary value from 0x00 to 0xFF, although often
///         a PUK is entered at a keyboard, so are usually ASCII characters. Tools
///         from Yubico generally allow 6- to 8-byte length PUKs, and pad with 0xFF
///         if less than 8.
///     </para>
///     <para>
///         This class will operate as if a PIN and PUK have the same restrictions.
///         That is, for this class, a PIN and PUK are the same thing: a 6- to 8-byte
///         value, and padded with 0xFF if 6 or 7 bytes long. The values and methods
///         in this class use the term "Pin" (such as "MinimumPinLength" and
///         "IsValidPinLength"), but they are valid for dealing with PUKs as well.
///         For example, if you want to copy a PUK into a new buffer with padding,
///         call <c>CopySinglePinWithPadding</c>.
///     </para>
/// </remarks>
internal static class PivPinUtilities
{
    /// <summary>
    ///     The pad byte used to make sure all PINs and PUKs are
    ///     <c>MaximumPinLength</c> bytes long.
    /// </summary>
    private const byte PivPinPad = 0xFF;

    /// <summary>
    ///     The 3 most significant nibbles of a StatusWord indicating an
    ///     incorrect PIN was entered (the least significant nibble is number of
    ///     retries remaining).
    /// </summary>
    private const short StatusWordWrongPin = 0x63C0;

    /// <summary>
    ///     Mask to isolate the WrongPin bits (exclude the retry count) when a
    ///     StatusWord is WrongPin.
    /// </summary>
    private const short StatusWordWrongPinMask = unchecked((short)0xFFF0);

    /// <summary>
    ///     Mask to isolate the retry count when a Status Word is WrongPin.
    /// </summary>
    public static short StatusWordRetryMask = 0x000F;

    /// <summary>
    ///     The minimum length a PIV PIN or PUK can be.
    /// </summary>
    public static int MinimumPinLength => 6;

    /// <summary>
    ///     The maximum length a PIV PIN or PUK can be.
    /// </summary>
    public static int MaximumPinLength => 8;

    /// <summary>
    ///     Is the given number a valid PIN length?
    /// </summary>
    /// <remarks>
    ///     This verifies that a number given is a valid PIN length. For example,
    ///     if the input is 6, it will return <c>true</c>. If the input is 1 or
    ///     10, it will return <c>false</c>.
    /// </remarks>
    /// <param name="pinLength">
    ///     The number to check.
    /// </param>
    /// <returns>
    ///     True if <c>pinLength</c> is a valid PIV PIN length, or False otherwise.
    /// </returns>
    public static bool IsValidPinLength(int pinLength) =>
        pinLength >= MinimumPinLength && pinLength <= MaximumPinLength;

    /// <summary>
    ///     Determine, based on the <paramref name="statusWord" />, what the
    ///     number of retries remaining are.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When verifying a PIN, PUK, or management key, the Status Word
    ///         returned by the YubiKey will indicate either success, wrong PIN with
    ///         retries remaining, PIN/PUK is blocked, incorrect management key, or
    ///         other error.
    ///     </para>
    ///     <para>
    ///         When <see cref="HasRetryCount(short)" /> returns <c>True</c>, this
    ///         function returns the number of retries remaining. Otherwise, it
    ///         will throw an exception because it is unable to parse the
    ///         <paramref name="statusWord" />.
    ///     </para>
    /// </remarks>
    /// <param name="statusWord">
    ///     The Status Word to parse.
    /// </param>
    /// <returns>
    ///     The number of retries remaining.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the <paramref name="statusWord" /> was unable to be parsed.
    ///     Use <see cref="HasRetryCount(short)" /> to ensure the <c>statusWord</c> is
    ///     valid to be parsed.
    /// </exception>
    public static int GetRetriesRemaining(short statusWord) =>
        HasRetryCount(statusWord)
            ? statusWord & StatusWordRetryMask
            : throw new InvalidOperationException();

    /// <summary>
    ///     Tests whether the <paramref name="statusWord" /> contains the number
    ///     of retries remaining.
    /// </summary>
    /// <param name="statusWord">
    ///     The Status Word to parse.
    /// </param>
    /// <returns>
    ///     <c>True</c> if the <paramref name="statusWord" /> contains the
    ///     number of retries remaining. <c>False</c> otherwise.
    /// </returns>
    public static bool HasRetryCount(short statusWord) => (statusWord & StatusWordWrongPinMask) == StatusWordWrongPin;

    /// <summary>
    ///     Create a new <c>byte[]</c> and set it with the input PIN bytes,
    ///     padding it if necessary.
    /// </summary>
    /// <remarks>
    ///     The return value will be <c>MaximumPinLength</c> bytes long.
    /// </remarks>
    /// <param name="pin">
    ///     The PIN to copy.
    /// </param>
    /// <returns>
    ///     A new byte array containing the padded PIN.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     The PIN supplied is not a valid length.
    /// </exception>
    public static byte[] CopySinglePinWithPadding(ReadOnlyMemory<byte> pin)
    {
        if (!IsValidPinLength(pin.Length))
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPinPukLength));
        }

        byte[] pinCopy = new byte[MaximumPinLength];
        var pinMemory = new Memory<byte>(pinCopy);
        pinMemory.Span.Fill(PivPinPad);
        pin.CopyTo(pinMemory);

        return pinCopy;
    }

    /// <summary>
    ///     Create a new <c>byte[]</c> and set it with the two input PIN bytes,
    ///     padding each if necessary.
    /// </summary>
    /// <remarks>
    ///     The result will be the first padded PIN followed by the second padded
    ///     PIN.
    ///     <para>
    ///         The return value will be (2 * <c>MaximumPinLength</c>) bytes long.
    ///     </para>
    /// </remarks>
    /// <param name="firstPin">
    ///     The first PIN to copy.
    /// </param>
    /// <param name="secondPin">
    ///     The second PIN to copy.
    /// </param>
    /// <returns>
    ///     A new byte array containing the padded pair.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     One of the PINs supplied is not a valid length.
    /// </exception>
    public static byte[] CopyTwoPinsWithPadding(ReadOnlyMemory<byte> firstPin, ReadOnlyMemory<byte> secondPin)
    {
        if (!IsValidPinLength(firstPin.Length)
            || !IsValidPinLength(secondPin.Length))
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidPinPukLength));
        }

        byte[] pinCopy = new byte[MaximumPinLength * 2];
        var pinMemory = new Memory<byte>(pinCopy);
        pinMemory.Span.Fill(PivPinPad);
        firstPin.CopyTo(pinMemory);
        secondPin.CopyTo(pinMemory[MaximumPinLength..]);

        return pinCopy;
    }
}
