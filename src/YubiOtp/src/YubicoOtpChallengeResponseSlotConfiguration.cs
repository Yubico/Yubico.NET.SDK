// Copyright 2026 Yubico AB
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
/// Configures a slot for challenge-response mode using the Yubico OTP (AES-128) algorithm.
/// </summary>
/// <remarks>
/// This is distinct from <see cref="YubiOtpSlotConfiguration"/>, which configures a slot to
/// emit a Yubico OTP on touch. This configuration instead makes the slot respond to an
/// explicit 6-byte challenge sent via <see cref="YubiOtpSession.CalculateYubicoOtpAsync"/>,
/// returning a 16-byte AES-128 response.
/// <para>
/// The 16-byte AES key is stored directly in the <c>key</c> wire format field. The key must be
/// exactly 16 bytes; keys of any other length are rejected before any device I/O is attempted.
/// </para>
/// </remarks>
public sealed class YubicoOtpChallengeResponseSlotConfiguration : SlotConfiguration
{
    /// <summary>
    /// Initializes a new Yubico OTP challenge-response configuration.
    /// </summary>
    /// <param name="aesKey">
    /// The AES-128 secret key. Must be exactly <see cref="YubiOtpConstants.KeySize"/> (16) bytes.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="aesKey"/> is not exactly 16 bytes.
    /// </exception>
    public YubicoOtpChallengeResponseSlotConfiguration(ReadOnlySpan<byte> aesKey)
    {
        if (aesKey.Length != YubiOtpConstants.KeySize)
        {
            throw new ArgumentException(
                $"AES key must be exactly {YubiOtpConstants.KeySize} bytes, got {aesKey.Length}.",
                nameof(aesKey));
        }

        aesKey.CopyTo(_key);

        _tktFlags |= TicketFlag.ChalResp;
        _cfgFlags |= ConfigFlag.ChalYubico;
    }

    public override FirmwareVersion MinimumFirmwareVersion => new(2, 2, 0);

    /// <summary>
    /// Requires physical touch to trigger the challenge-response.
    /// </summary>
    public YubicoOtpChallengeResponseSlotConfiguration RequireTouch(bool enable = true)
    {
        SetCfgFlag(ConfigFlag.ChalBtnTrig, enable);
        return this;
    }
}