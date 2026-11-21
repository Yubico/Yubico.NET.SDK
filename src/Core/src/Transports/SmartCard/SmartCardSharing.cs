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
/// Other clients using a shared card can select applications and affect application security state.
/// A PC/SC handle does not provide an independent application-state boundary. The built-in factory requires
/// exclusive PC/SC access and fails with <see cref="Devices.ConnectionInUseException" /> on contention,
/// without a shared retry. PC/SC exclusivity excludes other PC/SC clients through the resource manager;
/// it is not a guarantee about other transports or arbitrary custom connection implementations.
/// All built-in modes request power-off on release (<c>SCARD_UNPOWER_CARD</c>) and check native release
/// statuses. The requested disposition does not establish observed power-off or physical PIN-state clearing.
/// Shared access accepts interference from other card clients;
/// <see cref="ISmartCardConnection.IsExclusive" /> reports the acquired mode, and an explicitly selected
/// fallback to shared access is logged as a warning.
/// </para>
/// <para>
/// Set the process-wide mode through <see cref="SmartCardConnectionFactory.DefaultSharing" />, or pass one
/// to <see cref="SmartCardConnectionFactory(SmartCardSharing)" />. Applications migrating from the former
/// fallback default must opt in explicitly if they need coexistence with other smart-card clients.
/// The factory policy is initially <see cref="Exclusive" />; the zero-valued enum member remains
/// <see cref="ExclusiveWithSharedFallback" />.
/// </para>
/// </remarks>
public enum SmartCardSharing
{
    /// <summary>Explicit opt-in: open exclusively; on a sharing violation, retry shared and accept shared application state.</summary>
    ExclusiveWithSharedFallback,

    /// <summary>The factory default: open exclusively or fail with <see cref="Devices.ConnectionInUseException" />; never retry shared.</summary>
    Exclusive,

    /// <summary>
    /// Open shared, for applications that must coexist with other smart-card users (gpg-agent, browsers,
    /// the OS smart-card service) and accept shared application state and application-selection interference.
    /// </summary>
    Shared
}