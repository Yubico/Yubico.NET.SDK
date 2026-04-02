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
    StrongPw1 = 0x02,
    StrongPw2 = 0x08,
    ManUpdate = 0x20,
    OathFixedModhex1 = 0x10,
    OathFixedModhex2 = 0x40,
    OathFixedModhex = 0x50,
    OathFixedMask = 0x50,
    HmacLt64 = 0x04,
    ChalBtnTrig = 0x08
}

/// <summary>
/// Defines which <see cref="ConfigFlag"/> values are valid for update operations.
/// </summary>
public static class ConfigFlagMasks
{
    public const ConfigFlag UpdateMask =
        ConfigFlag.PacingChar10 |
        ConfigFlag.PacingChar20;
}
