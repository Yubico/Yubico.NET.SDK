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
/// the card can use a PIN=ONCE key until the card is reset. The SDK therefore holds the card exclusively
/// while a connection is open, which keeps other local processes out, and powers the card off on release
/// (<c>SCARD_UNPOWER_CARD</c>), which clears the state afterwards. A shared handle reopens the window
/// for as long as it is held; <see cref="ISmartCardConnection.IsExclusive" /> reports which one a
/// connection got, and a fallback to shared access is logged as a warning.
/// </para>
/// <para>
/// Set the process-wide mode through <see cref="SmartCardConnectionFactory.DefaultSharing" />, or pass one
/// to <see cref="SmartCardConnectionFactory(SmartCardSharing)" />.
/// </para>
/// </remarks>
public enum SmartCardSharing
{
    /// <summary>Open exclusively; if another client already holds the card, open shared instead. The default.</summary>
    ExclusiveWithSharedFallback,

    /// <summary>Open exclusively or fail with <see cref="Devices.ConnectionInUseException" />.</summary>
    Exclusive,

    /// <summary>
    /// Open shared, for applications that must coexist with other smart-card users (gpg-agent, browsers,
    /// the OS smart-card service) and accept that card-wide state is reachable from their handles.
    /// </summary>
    Shared
}