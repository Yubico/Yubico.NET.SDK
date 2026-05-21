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
/// Configures a slot for static ticket mode. This generates a fixed, unchanging
/// ticket using the configured key material.
/// </summary>
/// <remarks>
/// Wire format layout mirrors Yubico OTP:
/// <list type="bullet">
/// <item><c>fixed[0..publicId.Length]</c> — public identity prefix (up to 16 bytes)</item>
/// <item><c>uid</c> — 6-byte private identity</item>
/// <item><c>key</c> — 16-byte AES-128 key</item>
/// </list>
/// The <see cref="ConfigFlag.StaticTicket"/> flag is set to produce a non-incrementing ticket.
/// </remarks>
public sealed class StaticTicketSlotConfiguration : KeyboardSlotConfiguration
{
    /// <summary>
    /// Initializes a new static ticket configuration.
    /// </summary>
    /// <param name="publicId">
    /// The public identity prefix (up to 16 bytes).
    /// </param>
    /// <param name="privateId">The 6-byte private identity.</param>
    /// <param name="aesKey">The 16-byte AES-128 key.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="publicId"/> exceeds 16 bytes,
    /// <paramref name="privateId"/> is not 6 bytes, or
    /// <paramref name="aesKey"/> is not 16 bytes.
    /// </exception>
    public StaticTicketSlotConfiguration(
        ReadOnlySpan<byte> publicId,
        ReadOnlySpan<byte> privateId,
        ReadOnlySpan<byte> aesKey)
    {
        InitializeKeys(publicId, privateId, aesKey);
        _cfgFlags |= ConfigFlag.StaticTicket;
    }
}
