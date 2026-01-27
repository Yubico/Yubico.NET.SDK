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

using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management.Examples.ManagementTool.ManagementExamples.Results;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.ManagementExamples;

/// <summary>
/// Demonstrates factory resetting a YubiKey.
/// </summary>
/// <remarks>
/// <para>
/// <b>WARNING:</b> Factory reset is a destructive operation that erases all data
/// on the device. This includes all credentials, keys, and configuration.
/// This operation cannot be undone.
/// </para>
/// <para>
/// Factory reset is only available on YubiKeys with firmware version 5.6.0 or later.
/// </para>
/// </remarks>
public static class DeviceReset
{
    /// <summary>
    /// The minimum firmware version required for factory reset.
    /// </summary>
    public static readonly FirmwareVersion MinimumResetVersion = new(5, 6, 0);

    /// <summary>
    /// Checks if the device supports factory reset.
    /// </summary>
    /// <param name="firmwareVersion">The device firmware version.</param>
    /// <returns>True if factory reset is supported.</returns>
    public static bool IsResetSupported(FirmwareVersion firmwareVersion)
    {
        return firmwareVersion >= MinimumResetVersion;
    }

    /// <summary>
    /// Performs a factory reset on the YubiKey.
    /// </summary>
    /// <param name="session">A Management session.</param>
    /// <param name="deviceInfo">Device information (to check firmware version).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    /// <remarks>
    /// <para>
    /// <b>WARNING:</b> This operation will:
    /// </para>
    /// <list type="bullet">
    /// <item>Erase all PIV keys and certificates</item>
    /// <item>Erase all FIDO2 credentials</item>
    /// <item>Erase all OATH accounts</item>
    /// <item>Erase all OpenPGP keys</item>
    /// <item>Reset all PINs and PUKs to default values</item>
    /// <item>Remove configuration lock code</item>
    /// <item>Reset all device configuration to defaults</item>
    /// </list>
    /// <para>
    /// Requires firmware version 5.6.0 or later.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // First get device info to check firmware version
    /// var infoResult = await DeviceInfoQuery.GetDeviceInfoAsync(session, ct);
    /// if (!infoResult.Success || !infoResult.DeviceInfo.HasValue)
    /// {
    ///     Console.WriteLine("Failed to get device info");
    ///     return;
    /// }
    /// 
    /// // Check firmware version and perform reset
    /// var resetResult = await DeviceReset.ResetDeviceAsync(
    ///     session, 
    ///     infoResult.DeviceInfo.Value, 
    ///     ct);
    /// 
    /// if (resetResult.Success)
    /// {
    ///     Console.WriteLine("Device reset successfully");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Reset failed: {resetResult.ErrorMessage}");
    /// }
    /// </code>
    /// </example>
    public static async Task<ResetResult> ResetDeviceAsync(
        IManagementSession session,
        DeviceInfo deviceInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        // Check firmware version
        if (!IsResetSupported(deviceInfo.FirmwareVersion))
        {
            return ResetResult.Failed(
                $"Factory reset requires firmware 5.6.0 or later. " +
                $"Device firmware is {deviceInfo.FirmwareVersion}.");
        }

        try
        {
            await session.ResetDeviceAsync(cancellationToken);
            return ResetResult.Succeeded();
        }
        catch (Exception ex)
        {
            return ResetResult.Failed($"Failed to reset device: {ex.Message}");
        }
    }
}
