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
/// A HID keyboard connection to a YubiKey using feature reports (8-byte reports).
/// Used for OTP/YubiOTP and Management over OTP interface.
/// </summary>
public interface IOtpConnection : IConnection
{
    /// <summary>
    /// Size of feature reports for OTP protocol (always 8 bytes).
    /// </summary>
    int FeatureReportSize { get; }

    /// <summary>
    /// Sends an 8-byte feature report to the YubiKey.
    /// </summary>
    /// <param name="report">The report data (must be 8 bytes).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives an 8-byte feature report from the YubiKey.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The received report (8 bytes).</returns>
    Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default);
}
