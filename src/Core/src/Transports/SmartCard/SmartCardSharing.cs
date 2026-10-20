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

namespace Yubico.YubiKit.Core.Transports.SmartCard;

/// <summary>
/// How the SDK opens a PC/SC card handle (SCardConnect share mode).
/// </summary>
/// <remarks>
/// <para>
/// Smart-card application state is card-wide, not per handle: NIST SP 800-73-5 Part 2 §2.4.2 lists the
/// PIV PIN among the application security status indicators, so once a PIN is verified any handle on
/// the card can use a PIN=ONCE key until the card is reset. Holding the card exclusively while a session
/// is open closes that window to other local processes; powering the card off on release
/// (<c>SCARD_UNPOWER_CARD</c>) closes it after the session.
/// </para>
/// <para>
/// The default, <see cref="ExclusiveWithSharedFallback"/>, is what ykman does (exclusive first, shared
/// when another client already holds the card). The fallback is logged and visible through
/// <see cref="ISmartCardConnection.IsExclusive"/>, because in shared mode the PIN window is open again.
/// </para>
/// </remarks>
public enum SmartCardSharing
{
    /// <summary>Open exclusively; if another client holds the card, open shared instead (ykman default).</summary>
    ExclusiveWithSharedFallback,

    /// <summary>Open exclusively or fail with <see cref="Devices.ConnectionInUseException"/>.</summary>
    Exclusive,

    /// <summary>
    /// Open shared. For applications that must coexist with other smart-card users (gpg-agent,
    /// browsers, the OS smart-card service) and accept the card-wide PIN state.
    /// </summary>
    Shared
}