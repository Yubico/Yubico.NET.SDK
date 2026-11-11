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

/// <summary>
///     Reads device information and changes device-level configuration over a Management connection.
/// </summary>
/// <remarks>
///     Await each operation before starting the next. Overlapping exchanges are refused rather than queued.
///     A device-information read can span several exchanges; it is not an atomic snapshot, and concurrent
///     disposal does not guarantee completion of the whole read.
///     Cancellation is transport-specific: SmartCard checks cancellation before admitting an exchange,
///     then drains the admitted exchange without interrupting it. HID protocols have their own cancellation
///     signaling during device waits. Cancellation never rolls back configuration already sent to the device.
/// </remarks>
public interface IManagementSession : IApplicationSession
{
    /// <summary>Queries the device's current metadata, capabilities, and configuration.</summary>
    /// <remarks>
    ///     Each call queries the device rather than returning cached session metadata. Fields such as the
    ///     serial number are optional and depend on the device and transport. Firmware 4.1.0 or later is
    ///     supported; this method does not enforce that minimum before sending the request. Unsupported
    ///     devices can instead fail during communication or response parsing.
    ///     Device, transport, and parsing errors propagate from their respective layers.
    /// </remarks>
    /// <param name="cancellationToken">A token requesting cancellation, subject to the transport's exchange boundaries.</param>
    /// <returns>The device information reported by the key.</returns>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The session is not initialized, or another exchange is in progress.</exception>
    /// <exception cref="OperationCanceledException">Cancellation is observed at a transport-supported boundary.</exception>
    Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>Writes the specified configuration values and optional configuration-lock changes.</summary>
    /// <remarks>
    ///     <para>
    ///     Null configuration properties are omitted. Non-null application masks and flags replace their
    ///     respective sets, rather than adding bits. Preserve at least one supported USB application.
    ///     The SDK rejects an explicit USB mask of None, but does not check the mask against this device's
    ///     supported applications or enforce each field's firmware requirements.
    ///     The effective session firmware must be at least 5.0.0; major-zero development versions bypass
    ///     this check, and the session's firmware override applies. Passing the check does not guarantee
    ///     that the device supports every requested setting.
    ///     </para>
    ///     <para>
    ///     Supply the current 16-byte lock code on each protected write. This authorizes only that command;
    ///     it does not unlock the session or remove the lock. A new code replaces the lock; sixteen zero
    ///     bytes remove it. Null options leave the lock unchanged and do not request a reboot.
    ///     </para>
    ///     <para>
    ///     Lock-code memory remains caller-owned: keep it valid and unchanged until completion, then clear
    ///     it when no longer needed, including on failure or cancellation. The session clears its encoded
    ///     configuration buffer after the backend write completes, including failure or cancellation.
    ///     </para>
    ///     <para>
    ///     A requested reboot can disconnect the key; dispose stale sessions and rediscover/reopen it.
    ///     Device, transport, and response errors propagate; a failed or cancelled call is not a rollback.
    ///     </para>
    /// </remarks>
    /// <param name="config">The settings to write; null properties preserve existing settings.</param>
    /// <param name="options">Optional reboot request and borrowed current/replacement lock codes.</param>
    /// <param name="cancellationToken">A token requesting cancellation, subject to the transport's exchange boundaries.</param>
    /// <returns>A task representing the configuration write.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="config" /> is null.</exception>
    /// <exception cref="ArgumentException">A supplied lock code is not 16 bytes.</exception>
    /// <exception cref="NotSupportedException">The effective session firmware does not support configuration writes.</exception>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The USB mask is explicitly None, or another exchange is in progress.</exception>
    /// <exception cref="OperationCanceledException">Cancellation is observed; configuration already sent is not rolled back.</exception>
    Task SetDeviceConfigAsync(
        DeviceConfig config,
        SetDeviceConfigOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resets the FIDO and PIV applications together on a YubiKey Bio Multi-protocol Edition over SmartCard (CCID).
    /// </summary>
    /// <remarks>
    ///     This destructive operation removes FIDO credentials, fingerprints, and the shared PIN, and deletes
    ///     PIV keys, certificates, and data except the F9 attestation slot. It cannot be undone. Other YubiKey
    ///     families use per-application resets; this is not a general recovery procedure for a lost
    ///     Management configuration-lock code and does not promise to erase all device configuration.
    ///     The effective session firmware must be 5.6.0 or later; major-zero development versions bypass the check.
    ///     <see cref="Yubico.YubiKit.Core.Sessions.SessionCreationOptions.FirmwareVersionOverride" /> applies.
    ///     The SDK does not check the product family; passing the firmware check does not establish support.
    ///     HID FIDO and HID OTP sessions are rejected without sending a reset command.
    /// </remarks>
    /// <param name="cancellationToken">A token checked before exchange admission; it does not interrupt an admitted SmartCard reset or undo it.</param>
    /// <returns>A task representing the asynchronous reset operation.</returns>
    /// <exception cref="NotSupportedException">
    ///     The effective session firmware is a release version below 5.6.0, or the session does not use SmartCard (CCID).
    /// </exception>
    /// <exception cref="Yubico.YubiKit.Core.Protocols.SmartCard.Apdu.ApduException">
    ///     The device rejects the reset command, including when whole-device reset is not implemented.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Another exchange is in progress.</exception>
    /// <exception cref="OperationCanceledException">Cancellation is observed before the reset exchange is admitted.</exception>
    /// <example>
    ///     For a discovered Bio MPE <see cref="IYubiKey" /> named <c>device</c> and a
    ///     <see cref="CancellationToken" /> named <c>cancellationToken</c>, select SmartCard explicitly:
    ///     <code>
    /// using Yubico.YubiKit.Core.Devices;
    /// using Yubico.YubiKit.Core.Sessions;
    /// using Yubico.YubiKit.Management;
    ///
    /// await using var session = await device.CreateManagementSessionAsync(
    ///     new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard },
    ///     cancellationToken);
    ///
    /// // Destructive: confirm that FIDO/PIV data and the shared PIN may be removed.
    /// await session.ResetDeviceAsync(cancellationToken);
    ///     </code>
    /// </example>
    Task ResetDeviceAsync(CancellationToken cancellationToken = default);
}