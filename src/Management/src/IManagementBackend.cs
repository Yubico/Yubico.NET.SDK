// Copyright 2024 Yubico AB
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

namespace Yubico.YubiKit.Management;

/// <summary>
/// Abstraction for protocol-specific Management operations.
/// Encapsulates differences between SmartCard (APDU), FIDO (CTAP), and OTP (HID reports).
/// </summary>
internal interface IManagementBackend : IDisposable
{
    /// <summary>
    /// Read configuration data from the specified page.
    /// </summary>
    /// <param name="page">Page number to read (0-based).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Raw TLV-encoded configuration data.</returns>
    ValueTask<byte[]> ReadConfigAsync(int page, CancellationToken cancellationToken);

    /// <summary>
    /// Write configuration data to the device.
    /// </summary>
    /// <param name="config">TLV-encoded configuration data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask WriteConfigAsync(byte[] config, CancellationToken cancellationToken);

    /// <summary>
    /// Set device mode (legacy operation for older YubiKeys).
    /// </summary>
    /// <param name="data">Mode configuration data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask SetModeAsync(byte[] data, CancellationToken cancellationToken);

    /// <summary>
    /// Perform device reset (factory reset).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask DeviceResetAsync(CancellationToken cancellationToken);
}
