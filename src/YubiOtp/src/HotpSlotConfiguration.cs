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

using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Configures a slot for HOTP (HMAC-based One-Time Password, RFC 4226) mode.
/// </summary>
/// <remarks>
/// Wire format layout:
/// <list type="bullet">
/// <item><c>key[0..16]</c> — first 16 bytes of the HMAC key</item>
/// <item><c>uid[0..4]</c> — bytes 16–19 of the HMAC key</item>
/// <item><c>uid[4..6]</c> — two-byte initial-counter field (big-endian)</item>
/// </list>
/// The key must be exactly 20 bytes. Keys of any other length are rejected
/// before any device I/O is attempted; they are never hashed or padded to fit.
/// The configuration owns a copy of the key; dispose it and separately clear the source.
/// HOTP is keyboard output; synchronize the initial counter with your verifier. Nonzero counter
/// presets still require physical qualification on the target firmware.
/// </remarks>
public sealed class HotpSlotConfiguration : KeyboardSlotConfiguration
{
    /// <summary>
    /// Initializes a new HOTP slot configuration.
    /// </summary>
    /// <param name="hmacKey">
    /// The HMAC secret key. Must be exactly <see cref="YubiOtpConstants.HmacKeySize"/> (20) bytes.
    /// </param>
    /// <param name="imf">
    /// Initial moving factor, from 0 to 1,048,560 (0xFFFF0) in steps of 16. Defaults to zero.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the key is not exactly 20 bytes or the IMF cannot be represented.
    /// </exception>
    public HotpSlotConfiguration(ReadOnlySpan<byte> hmacKey, int imf = 0)
    {
        if (imf < 0 || imf > 0xFFFF0 || imf % 16 != 0)
        {
            throw new ArgumentException(
                "Initial moving factor must be between 0 and 1,048,560, in steps of 16.",
                nameof(imf));
        }

        SetHmacKey(hmacKey, (ushort)(imf / 16));

        _tktFlags |= TicketFlag.OathHotp;
    }

    public override FirmwareVersion MinimumFirmwareVersion => new(2, 2, 0);

    /// <summary>
    /// Configures the OTP output to use 8 digits instead of the default 6.
    /// </summary>
    public HotpSlotConfiguration Use8Digits(bool enable = true)
    {
        SetCfgFlag(ConfigFlag.OathHotp8, enable);
        return this;
    }

    /// <summary>
    /// Configures an OATH token ID prefix for the HOTP output.
    /// </summary>
    /// <param name="tokenId">The raw token ID value (up to 16 bytes).</param>
    /// <param name="fixedModhex1">Output the first byte of the token ID as modhex.</param>
    /// <param name="fixedModhex2">Output the first two bytes of the token ID as modhex.</param>
    /// <returns>This configuration for chaining.</returns>
    /// <remarks>
    /// If both <paramref name="fixedModhex1"/> and <paramref name="fixedModhex2"/> are set,
    /// the entire token ID is sent as modhex.
    /// </remarks>
    public HotpSlotConfiguration TokenId(
        ReadOnlySpan<byte> tokenId,
        bool fixedModhex1 = false,
        bool fixedModhex2 = false)
    {
        if (tokenId.Length > YubiOtpConstants.FixedSize)
        {
            throw new ArgumentException(
                $"Token ID must be at most {YubiOtpConstants.FixedSize} bytes.",
                nameof(tokenId));
        }

        SetFixed(tokenId);
        SetCfgFlag(ConfigFlag.OathFixedModhex1, fixedModhex1);
        SetCfgFlag(ConfigFlag.OathFixedModhex2, fixedModhex2);
        return this;
    }
}