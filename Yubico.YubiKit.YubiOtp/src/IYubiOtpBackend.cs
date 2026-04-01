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
/// Internal abstraction for YubiOTP transport-specific operations.
/// Implementations encode operations for SmartCard (APDU) or OTP HID (feature reports).
/// </summary>
internal interface IYubiOtpBackend
{
    /// <summary>
    /// Writes a configuration to a slot and returns the updated status bytes.
    /// Used for programming slots, swapping, deleting, setting scan maps, and NDEF configuration.
    /// </summary>
    /// <param name="slot">The config slot command byte.</param>
    /// <param name="data">The configuration data to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated status bytes from the YubiKey.</returns>
    ValueTask<ReadOnlyMemory<byte>> WriteUpdateAsync(
        ConfigSlot slot,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends data to a slot and receives a response of the expected length.
    /// Used for challenge-response and serial number read operations.
    /// </summary>
    /// <param name="slot">The config slot command byte.</param>
    /// <param name="data">The data to send (e.g., challenge bytes).</param>
    /// <param name="expectedLength">The expected response data length (excluding CRC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response data from the YubiKey.</returns>
    ValueTask<ReadOnlyMemory<byte>> SendAndReceiveAsync(
        ConfigSlot slot,
        ReadOnlyMemory<byte> data,
        int expectedLength,
        CancellationToken cancellationToken);
}
