// Copyright 2026 Yubico AB
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
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Management;

public interface IManagementSession : IApplicationSession
{
    Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default);

    Task SetDeviceConfigAsync(
        DeviceConfig config,
        SetDeviceConfigOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resets the whole device to factory settings over SmartCard (CCID).
    /// </summary>
    /// <remarks>
    ///     This destructive operation erases credentials, keys, and configuration and cannot be undone.
    ///     The effective session firmware must be 5.6.0 or later; alpha/beta versions bypass the check.
    ///     <see cref="Yubico.YubiKit.Core.Sessions.SessionCreationOptions.FirmwareVersionOverride" /> applies.
    ///     The firmware check does not guarantee that the device implements whole-device reset.
    ///     HID FIDO and HID OTP sessions are rejected without sending a reset command.
    /// </remarks>
    /// <param name="cancellationToken">An optional token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous reset operation.</returns>
    /// <exception cref="NotSupportedException">
    ///     The effective session firmware is a release version below 5.6.0, or the session does not use SmartCard (CCID).
    /// </exception>
    /// <exception cref="Yubico.YubiKit.Core.Protocols.SmartCard.Apdu.ApduException">
    ///     The device rejects the reset command, including when whole-device reset is not implemented.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <example>
    ///     For a discovered <see cref="IYubiKey" /> device and a cancellation token, select SmartCard explicitly:
    ///     <code>
    /// using Yubico.YubiKit.Core.Devices;
    /// using Yubico.YubiKit.Core.Sessions;
    /// using Yubico.YubiKit.Management;
    ///
    /// await using var session = await device.CreateManagementSessionAsync(
    ///     new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard },
    ///     cancellationToken);
    ///
    /// // Destructive: only call after confirming that all device data may be erased.
    /// await session.ResetDeviceAsync(cancellationToken);
    ///     </code>
    /// </example>
    Task ResetDeviceAsync(CancellationToken cancellationToken = default);
}