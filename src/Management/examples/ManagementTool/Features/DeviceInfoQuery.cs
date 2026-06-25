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

using Yubico.YubiKit.Management.Examples.ManagementTool.Features.Results;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.Features;

/// <summary>
/// Demonstrates retrieving device information from a YubiKey.
/// </summary>
/// <remarks>
/// <para>
/// This class provides examples for querying device metadata including
/// serial number, firmware version, form factor, and enabled capabilities.
/// </para>
/// </remarks>
public static class DeviceInfoQuery
{
    /// <summary>
    /// Gets comprehensive device information from a Management session.
    /// </summary>
    /// <param name="session">A Management session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing device information or error.</returns>
    /// <example>
    /// <code>
    /// await using var connection = await device.ConnectAsync&lt;ISmartCardConnection&gt;(ct);
    /// await using var session = await ManagementSession.CreateAsync(connection, ct);
    /// 
    /// var result = await DeviceInfoQuery.GetDeviceInfoAsync(session, ct);
    /// if (result.Success &amp;&amp; result.DeviceInfo.HasValue)
    /// {
    ///     var info = result.DeviceInfo.Value;
    ///     Console.WriteLine($"Serial: {info.SerialNumber}");
    ///     Console.WriteLine($"Firmware: {info.FirmwareVersion}");
    /// }
    /// </code>
    /// </example>
    public static async Task<DeviceInfoResult> GetDeviceInfoAsync(
        IManagementSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var deviceInfo = await session.GetDeviceInfoAsync(cancellationToken);
            return DeviceInfoResult.Succeeded(deviceInfo);
        }
        catch (Exception ex)
        {
            return DeviceInfoResult.Failed($"Failed to get device info: {ex.Message}");
        }
    }
}
