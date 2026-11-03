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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Management.Examples.ManagementTool.Features.Results;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.Features;

/// <summary>
/// Demonstrates the Bio Multi-protocol Edition combined FIDO/PIV reset over SmartCard.
/// </summary>
/// <remarks>
/// <para>
/// Reset destroys FIDO credentials, fingerprints and the shared PIN, and PIV keys, certificates and
/// user data except the F9 attestation slot. It cannot be undone. It is not a documented recovery
/// procedure for a lost Management configuration lock code.
/// </para>
/// <para>
/// The helper checks firmware 5.6.0, not product-family support. A passing check does not establish
/// Bio MPE eligibility. The SDK additionally requires SmartCard; a device rejection can still fail.
/// Operation exceptions, including cancellation, are converted to failure results.
/// </para>
/// </remarks>
public static class DeviceReset
{
    /// <summary>
    /// The minimum firmware version required for factory reset.
    /// </summary>
    public static readonly FirmwareVersion MinimumResetVersion = new(5, 6, 0);

    /// <summary>
    /// Checks the helper's minimum firmware comparison, not device reset capability.
    /// </summary>
    /// <param name="firmwareVersion">The device firmware version.</param>
    /// <returns>True if the supplied firmware is at least the helper's minimum; no family or transport check.</returns>
    public static bool IsResetSupported(FirmwareVersion firmwareVersion)
    {
        return firmwareVersion >= MinimumResetVersion;
    }

    /// <summary>
    /// Requests the combined reset after a firmware-only helper check.
    /// </summary>
    /// <param name="session">A Management session.</param>
    /// <param name="deviceInfo">Device information (to check firmware version).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    /// <remarks>
    /// <para>
    /// Callers must confirm Bio MPE applicability and authorize destructive application reset before calling.
    /// </para>
    /// <list type="bullet">
    /// <item>Deletes PIV keys, certificates and user data, except the F9 attestation slot.</item>
    /// <item>Deletes FIDO credentials and fingerprint templates, and removes the shared PIN.</item>
    /// </list>
    /// <para>
    /// Requires a SmartCard Management session on applicable Bio MPE hardware. Firmware alone is insufficient.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Prerequisite: caller confirmed Bio MPE and authorized reset; session uses SmartCard.
    /// // Metadata below supplies the helper's firmware check, not a product-family check.
    /// var infoResult = await DeviceInfoQuery.GetDeviceInfoAsync(session, ct);
    /// if (!infoResult.Success || !infoResult.DeviceInfo.HasValue)
    /// {
    ///     Console.WriteLine("Failed to get device info");
    ///     return;
    /// }
    /// 
    /// // Request reset; the helper returns failure results for operation exceptions.
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

        // Firmware-only helper preflight; this does not establish product or transport eligibility.
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