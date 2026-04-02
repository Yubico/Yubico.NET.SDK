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
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Configures a slot for HMAC-SHA1 challenge-response mode.
/// </summary>
/// <remarks>
/// The 20-byte HMAC key is split across the wire format:
/// <list type="bullet">
/// <item>Bytes 0–15 are stored in the <c>key</c> field (16 bytes)</item>
/// <item>Bytes 16–19 are stored in <c>uid[0..4]</c> (4 bytes)</item>
/// </list>
/// Keys longer than 20 bytes are shortened via SHA-1 hashing.
/// Keys shorter than 20 bytes are zero-padded.
/// </remarks>
public sealed class HmacSha1SlotConfiguration : SlotConfiguration
{
    /// <summary>
    /// Initializes a new HMAC-SHA1 challenge-response configuration.
    /// </summary>
    /// <param name="hmacKey">
    /// The HMAC-SHA1 secret key. Keys longer than 20 bytes are shortened via SHA-1.
    /// Keys shorter than 20 bytes are zero-padded.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when the key is empty.</exception>
    public HmacSha1SlotConfiguration(ReadOnlySpan<byte> hmacKey)
    {
        if (hmacKey.IsEmpty)
        {
            throw new ArgumentException("HMAC key must not be empty.", nameof(hmacKey));
        }

        Span<byte> processedKey = stackalloc byte[YubiOtpConstants.HmacKeySize];

        if (hmacKey.Length > YubiOtpConstants.HmacKeySize)
        {
            SHA1.HashData(hmacKey, processedKey);
        }
        else
        {
            hmacKey.CopyTo(processedKey);
            // Remaining bytes are already zero from stackalloc
        }

        // Split: first 16 bytes -> _key, next 4 bytes -> _uid[0..4]
        processedKey[..YubiOtpConstants.KeySize].CopyTo(_key);
        processedKey[YubiOtpConstants.KeySize..].CopyTo(_uid);

        CryptographicOperations.ZeroMemory(processedKey);

        _tktFlags |= TicketFlag.ChalResp;
    }

    public override FirmwareVersion MinimumFirmwareVersion => new(2, 2, 0);

    /// <summary>
    /// Requires physical touch to trigger the challenge-response.
    /// </summary>
    public HmacSha1SlotConfiguration RequireTouch(bool enable = true)
    {
        SetCfgFlag(ConfigFlag.ChalBtnTrig, enable);
        return this;
    }

    /// <summary>
    /// Allows challenges shorter than 64 bytes. The challenge is padded
    /// to 64 bytes with a byte value that differs from the last data byte.
    /// </summary>
    public HmacSha1SlotConfiguration UseShortChallenge(bool enable = true)
    {
        SetCfgFlag(ConfigFlag.HmacLt64, enable);
        return this;
    }
}
