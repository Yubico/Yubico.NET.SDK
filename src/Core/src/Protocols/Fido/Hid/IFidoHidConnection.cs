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
///         Unlike the CCID and OTP HID interfaces, the FIDO HID interface is <b>shared</b>: a second
///         connection to it is admitted rather than refused, which is what the Management-over-HID
///         fallback depends on.
///     </para>
///     <para>
///         <b>Admission is not a concurrency guarantee.</b> Two open FIDO HID handles do not
///         demultiplex: a request sent on one handle can be answered on the other, because the input
///         report is delivered to whichever handle reads first. Each connection has its own exchange
///         gate, so nothing serializes traffic between two handles. Drive CTAP over <b>one</b> FIDO
///         connection at a time; if two are open, do not use them concurrently.
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
    /// <param name="packet">The packet data (must be 64 bytes).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives a 64-byte HID packet from the YubiKey.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The received packet (64 bytes).</returns>
    Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default);
}