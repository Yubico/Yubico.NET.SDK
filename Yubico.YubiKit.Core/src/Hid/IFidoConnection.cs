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

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// A FIDO HID connection to a YubiKey using CTAP HID protocol (64-byte packets).
/// Used for FIDO2/U2F and Management over FIDO interface.
/// </summary>
public interface IFidoConnection : IConnection
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
