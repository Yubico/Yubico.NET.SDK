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
/// Configuration flags (CFGFLAG) controlling OTP slot behavior.
/// </summary>
/// <remarks>
/// Names sharing a bit are mode-specific aliases, not independent options. For example, 0x02 selects
/// ticket ordering, short static output, or eight-digit HOTP depending on mode; 0x04/0x08 mean keyboard
/// pacing or HMAC short-challenge/challenge touch. Use the mode-specific configuration builders.
/// Legacy flags are retained for their firmware-specific uses, not guaranteed on every newer key.
/// </remarks>
[Flags]
public enum ConfigFlag : byte
{
    None = 0x00,
    SendRef = 0x01,
    TicketFirst = 0x02,
    PacingChar10 = 0x04,
    PacingChar20 = 0x08,
    AllowHidTrig = 0x10,
    StaticTicket = 0x20,
    ShortTicket = 0x02,
    StrongPw1 = 0x10,
    StrongPw2 = 0x40,
    ManUpdate = 0x80,
    OathFixedModhex1 = 0x10,
    OathFixedModhex2 = 0x40,
    OathFixedModhex = 0x50,
    OathFixedMask = 0x50,
    OathHotp8 = 0x02,
    ChalYubico = 0x20,
    ChalHmac = 0x22,
    HmacLt64 = 0x04,
    ChalBtnTrig = 0x08
}

/// <summary>
/// Defines keyboard pacing bits transmitted by update operations. Their HMAC/challenge aliases do not
/// make short-challenge or touch policy updateable; replace the configuration to change those policies.
/// </summary>
public static class ConfigFlagMasks
{
    public const ConfigFlag UpdateMask =
        ConfigFlag.PacingChar10 |
        ConfigFlag.PacingChar20;
}