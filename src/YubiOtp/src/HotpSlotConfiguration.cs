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

using System.Buffers.Binary;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Configures a slot for HOTP (HMAC-based One-Time Password, RFC 4226) mode.
/// </summary>
/// <remarks>
/// Wire format layout:
/// <list type="bullet">
/// <item><c>key[0..16]</c> — first 16 bytes of the HMAC key</item>
/// <item><c>uid[0..4]</c> — bytes 16–19 of the HMAC key</item>
/// <item><c>uid[4..6]</c> — initial moving factor / 0x10000 (big-endian)</item>
/// </list>
/// Keys longer than 20 bytes are shortened via SHA-1.
/// Keys shorter than 20 bytes are zero-padded.
/// </remarks>
public sealed class HotpSlotConfiguration : KeyboardSlotConfiguration
{
    /// <summary>
    /// Initializes a new HOTP slot configuration.
    /// </summary>
    /// <param name="hmacKey">
    /// The HMAC secret key. Keys longer than 20 bytes are shortened via SHA-1.
    /// Keys shorter than 20 bytes are zero-padded.
    /// </param>
    /// <param name="imf">
    /// Initial moving factor. Must be 0 or a multiple of 0x10000, and at most 0xFFFF0000.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the key is empty, or IMF is not a valid multiple of 0x10000.
    /// </exception>
    public HotpSlotConfiguration(ReadOnlySpan<byte> hmacKey, int imf = 0)
    {
        if (hmacKey.IsEmpty)
        {
            throw new ArgumentException("HMAC key must not be empty.", nameof(hmacKey));
        }

        if (imf != 0 && (imf % 0x10000 != 0 || imf < 0))
        {
            throw new ArgumentException(
                "Initial moving factor must be 0 or a positive multiple of 0x10000.",
                nameof(imf));
        }

        Span<byte> processedKey = stackalloc byte[YubiOtpConstants.HmacKeySize];

        if (hmacKey.Length > YubiOtpConstants.HmacKeySize)
        {
            SHA1.HashData(hmacKey, processedKey);
        }
        else
        {
            hmacKey.CopyTo(processedKey);
        }

        // Split key: first 16 bytes -> _key, next 4 bytes -> _uid[0..4]
        processedKey[..YubiOtpConstants.KeySize].CopyTo(_key);
        processedKey[YubiOtpConstants.KeySize..].CopyTo(_uid);

        CryptographicOperations.ZeroMemory(processedKey);

        // Store IMF / 0x10000 as big-endian in uid[4..6]
        if (imf != 0)
        {
            ushort imfValue = (ushort)(imf / 0x10000);
            BinaryPrimitives.WriteUInt16BigEndian(_uid.AsSpan(4, 2), imfValue);
        }

        _tktFlags |= TicketFlag.OathHotp;
    }

    public override FirmwareVersion MinimumFirmwareVersion => new(2, 1, 0);

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

        _fixed.AsSpan().Clear();
        tokenId.CopyTo(_fixed);
        _fixedSize = (byte)tokenId.Length;
        SetCfgFlag(ConfigFlag.OathFixedModhex1, fixedModhex1);
        SetCfgFlag(ConfigFlag.OathFixedModhex2, fixedModhex2);
        return this;
    }
}
