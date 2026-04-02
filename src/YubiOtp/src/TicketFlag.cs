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
/// Ticket flags (TKTFLAG) controlling OTP ticket generation behavior.
/// </summary>
[Flags]
public enum TicketFlag : byte
{
    None = 0x00,
    TabFirst = 0x01,
    AppendTab1 = 0x02,
    AppendTab2 = 0x04,
    AppendDelay1 = 0x08,
    AppendDelay2 = 0x10,
    AppendCr = 0x20,
    ProtectSlot2 = 0x40,
    OathHotp = 0x40,
    ChalResp = 0x40
}

/// <summary>
/// Defines which <see cref="TicketFlag"/> values are valid for update operations.
/// </summary>
public static class TicketFlagMasks
{
    public const TicketFlag UpdateMask =
        TicketFlag.TabFirst |
        TicketFlag.AppendTab1 |
        TicketFlag.AppendTab2 |
        TicketFlag.AppendDelay1 |
        TicketFlag.AppendDelay2 |
        TicketFlag.AppendCr;
}
