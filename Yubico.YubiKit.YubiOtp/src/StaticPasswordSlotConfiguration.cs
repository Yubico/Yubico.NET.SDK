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

        // Distribute scan codes across fixed, uid, and key fields
        int remaining = scanCodes.Length;

        int fixedLen = Math.Min(remaining, YubiOtpConstants.FixedSize);
        scanCodes[..fixedLen].CopyTo(_fixed);
        remaining -= fixedLen;

        if (remaining > 0)
        {
            int uidLen = Math.Min(remaining, YubiOtpConstants.UidSize);
            scanCodes.Slice(fixedLen, uidLen).CopyTo(_uid);
            remaining -= uidLen;

            if (remaining > 0)
            {
                int keyLen = Math.Min(remaining, YubiOtpConstants.KeySize);
                scanCodes.Slice(fixedLen + YubiOtpConstants.UidSize, keyLen).CopyTo(_key);
            }
        }

        _fixedSize = (byte)Math.Min(scanCodes.Length, YubiOtpConstants.FixedSize);
        _cfgFlags |= ConfigFlag.ShortTicket | ConfigFlag.StaticTicket;
    }
}
