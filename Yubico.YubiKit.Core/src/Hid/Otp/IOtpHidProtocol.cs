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

using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.Hid.Otp;

/// <summary>
/// Protocol interface for OTP HID communication using 8-byte feature reports.
/// Supports YubiKey Management operations over the OTP/Keyboard HID interface.
/// </summary>
public interface IOtpHidProtocol : IProtocol
{
    /// <summary>
    /// Sends a slot command and receives the response.
    /// Used for Management application over OTP HID.
    /// </summary>
    /// <param name="slot">The slot/command byte (e.g., 0x13 for READ_CAPABILITIES).</param>
    /// <param name="data">The command payload (up to 64 bytes, will be padded if shorter).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response data from the YubiKey (including CRC if present).</returns>
    Task<ReadOnlyMemory<byte>> SendAndReceiveAsync(
        byte slot,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the current status bytes from the YubiKey.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Status bytes (first 3 bytes are firmware version).</returns>
    Task<ReadOnlyMemory<byte>> ReadStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the firmware version reported during protocol initialization.
    /// </summary>
    FirmwareVersion? FirmwareVersion { get; }
}
