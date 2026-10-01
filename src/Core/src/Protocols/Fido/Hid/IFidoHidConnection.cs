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

using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Protocols.Fido.Hid;

/// <summary>
/// A FIDO HID connection to a YubiKey using CTAP HID protocol (64-byte packets).
/// Used for FIDO2/U2F and Management over FIDO interface.
/// </summary>
/// <remarks>
///     <para>
///         The physical FIDO HID interface admits exactly one live SDK connection and native HID handle.
///         A second connection attempt is refused with <see cref="Devices.ConnectionInUseException" />
///         before the native interface is opened. Dispose the current connection before reopening it.
///     </para>
/// </remarks>
public interface IFidoHidConnection : IConnection
{
    /// <summary>
    /// Size of HID packets for FIDO/CTAP protocol (always 64 bytes).
    /// </summary>
    int PacketSize { get; }

    /// <summary>
    /// Sends a 64-byte HID packet to the YubiKey.
    /// </summary>
    /// <remarks>
    ///     This Tier 2 method bypasses <see cref="Sessions.ApplicationSession" />,
    ///     <c>ConnectionSessionGuard</c>, and <c>ExchangeGuard</c>. The caller owns CTAP HID
    ///     framing, sequencing, response correlation, keep-alive handling, concurrency exclusion, and recovery.
    ///     Do not interleave it with a live session or another raw operation; dispose and reopen when state is uncertain.
    ///     Keep borrowed <paramref name="packet" /> memory valid until the returned task is terminal;
    ///     only then zero sensitive caller-owned input. This applies to every implementation, even though
    ///     built-in macOS output copies the packet. Built-in macOS rejects cancellation before dispatch;
    ///     after dispatch output may succeed, and the task waits for native completion and resource drain.
    ///     Cancellation alone does not make a raw connection reusable.
    /// </remarks>
    /// <param name="packet">The packet data (must be 64 bytes).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives a 64-byte HID packet from the YubiKey.
    /// </summary>
    /// <remarks>
    ///     This Tier 2 method bypasses <see cref="Sessions.ApplicationSession" />,
    ///     <c>ConnectionSessionGuard</c>, and <c>ExchangeGuard</c>. Pair receives with the
    ///     caller's own serialized send state. Built-in macOS cancellation of a pending read completes
    ///     that read and frees its overlap slot; input remains active and later reports are queued.
    ///     Cancellation does not abort an in-flight device protocol exchange. After interruption or
    ///     interleaving, dispose and reopen the connection; await disposal for native drain. Cancellation
    ///     alone does not make a raw protocol exchange reusable.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The received packet (64 bytes).</returns>
    Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default);
}