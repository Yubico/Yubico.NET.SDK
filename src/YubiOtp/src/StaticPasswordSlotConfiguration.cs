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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Transports.Hid.Keyboard;

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Configures a slot for static password mode using keyboard scan codes.
/// </summary>
/// <remarks>
/// Scan codes are distributed across the 38-byte area (fixed + uid + key):
/// <list type="bullet">
/// <item><c>fixed[0..16]</c> — first 16 scan code bytes</item>
/// <item><c>uid[0..6]</c> — scan code bytes 16–21</item>
/// <item><c>key[0..16]</c> — scan code bytes 22–37</item>
/// </list>
/// </remarks>
public sealed class StaticPasswordSlotConfiguration : KeyboardSlotConfiguration
{
    /// <summary>
    /// Initializes a new static password configuration from a human-readable password,
    /// translating each character to a HID scan code for the given keyboard layout.
    /// </summary>
    /// <remarks>
    /// The YubiKey itself has no concept of a keyboard layout; it only emits HID scan
    /// codes over USB. The scan codes produced here will render as <paramref name="password"/>
    /// only on a host configured with the same <paramref name="keyboardLayout"/>. Use
    /// <see cref="KeyboardLayout.ModHex"/> for a layout-independent password.
    /// </remarks>
    /// <param name="password">The human-readable password (up to 38 characters).</param>
    /// <param name="keyboardLayout">The keyboard layout to translate characters with.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="password"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="password"/> is empty or exceeds 38 characters.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="password"/> contains a character not supported by
    /// <paramref name="keyboardLayout"/>.
    /// </exception>
    public StaticPasswordSlotConfiguration(string password, KeyboardLayout keyboardLayout)
        : this(password, keyboardLayout, ToScanCodes)
    {
    }

    internal StaticPasswordSlotConfiguration(
        string password,
        KeyboardLayout keyboardLayout,
        Func<string, KeyboardLayout, byte[]> scanCodeFactory)
    {
        ArgumentNullException.ThrowIfNull(scanCodeFactory);

        byte[] scanCodes = scanCodeFactory(password, keyboardLayout);
        try
        {
            Initialize(scanCodes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(scanCodes);
        }
    }

    /// <summary>
    /// Initializes a new static password configuration.
    /// </summary>
    /// <param name="scanCodes">
    /// The keyboard scan codes representing the password (up to 38 bytes).
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="scanCodes"/> is empty or exceeds 38 bytes.
    /// </exception>
    public StaticPasswordSlotConfiguration(ReadOnlySpan<byte> scanCodes)
    {
        Initialize(scanCodes);
    }

    private void Initialize(ReadOnlySpan<byte> scanCodes)
    {
        if (scanCodes.IsEmpty)
        {
            throw new ArgumentException("Scan codes must not be empty.", nameof(scanCodes));
        }

        if (scanCodes.Length > YubiOtpConstants.ScanCodesSize)
        {
            throw new ArgumentException(
                $"Scan codes must not exceed {YubiOtpConstants.ScanCodesSize} bytes.",
                nameof(scanCodes));
        }

        Span<byte> uid = stackalloc byte[YubiOtpConstants.UidSize];
        Span<byte> key = stackalloc byte[YubiOtpConstants.KeySize];
        uid.Clear();
        key.Clear();
        try
        {
            // Distribute scan codes across fixed, uid, and key fields.
            int remaining = scanCodes.Length;
            int fixedLen = Math.Min(remaining, YubiOtpConstants.FixedSize);
            remaining -= fixedLen;

            if (remaining > 0)
            {
                int uidLen = Math.Min(remaining, YubiOtpConstants.UidSize);
                scanCodes.Slice(fixedLen, uidLen).CopyTo(uid);
                remaining -= uidLen;

                if (remaining > 0)
                {
                    scanCodes[(fixedLen + YubiOtpConstants.UidSize)..].CopyTo(key);
                }
            }

            SetFixed(scanCodes[..fixedLen]);
            SetUid(uid);
            SetKey(key);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(uid);
            CryptographicOperations.ZeroMemory(key);
        }

        _cfgFlags |= ConfigFlag.ShortTicket;
    }

    /// <summary>
    /// Translates a human-readable password into HID scan codes for the given keyboard layout.
    /// </summary>
    private static byte[] ToScanCodes(string password, KeyboardLayout keyboardLayout)
    {
        ArgumentNullException.ThrowIfNull(password);

        if (password.Length == 0)
        {
            throw new ArgumentException("Password must not be empty.", nameof(password));
        }

        if (password.Length > YubiOtpConstants.ScanCodesSize)
        {
            throw new ArgumentException(
                $"Password must not exceed {YubiOtpConstants.ScanCodesSize} characters.",
                nameof(password));
        }

        var translator = HidCodeTranslator.GetInstance(keyboardLayout);
        // Validate before allocating so unsupported characters cannot strand a partially populated sensitive buffer.
        foreach (char character in password)
        {
            _ = translator[character];
        }

        var scanCodes = new byte[password.Length];
        for (int index = 0; index < password.Length; index++)
        {
            scanCodes[index] = translator[password[index]];
        }

        return scanCodes;
    }
}