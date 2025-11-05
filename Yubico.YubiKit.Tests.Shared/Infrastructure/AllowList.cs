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

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     Implements allow list verification to prevent integration tests from running on production YubiKeys.
///     This is a CRITICAL safety mechanism that validates device serial numbers before any test operations.
/// </summary>
/// <remarks>
///     <para>
///     The AllowList performs a hard fail (Environment.Exit(-1)) if:
///     - No allowed serial numbers are configured
///     - A device's serial number is not in the allow list
///     </para>
///     <para>
///     This aggressive approach prevents accidental test execution on production keys, which could
///     lead to data loss or device misconfiguration.
///     </para>
/// </remarks>
public class AllowList
{
    private readonly IReadOnlyList<int> _allowedSerials;
    private readonly IAllowListProvider _provider;
    private readonly ILogger<AllowList>? _logger;

    /// <summary>
    ///     Initializes a new instance of <see cref="AllowList"/> with the specified provider.
    /// </summary>
    /// <param name="provider">The provider that supplies allowed serial numbers.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when provider is null.</exception>
    /// <remarks>
    ///     Constructor performs hard fail (Environment.Exit(-1)) if allow list is empty.
    /// </remarks>
    public AllowList(IAllowListProvider provider, ILogger<AllowList>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(provider);

        _provider = provider;
        _logger = logger;
        _allowedSerials = provider.GetList();

        if (_allowedSerials.Count == 0)
        {
            string errorMessage = provider.OnInvalidInputErrorMessage();
            _logger?.LogCritical("{ErrorMessage}", errorMessage);
            Console.Error.WriteLine(errorMessage);
            Environment.Exit(-1); // Hard fail - cannot continue without allow list
        }

        _logger?.LogInformation("AllowList initialized with {Count} allowed serial numbers", _allowedSerials.Count);
    }

    /// <summary>
    ///     Verifies that the specified device is in the allow list.
    /// </summary>
    /// <param name="device">The YubiKey device to verify.</param>
    /// <exception cref="ArgumentNullException">Thrown when device is null.</exception>
    /// <remarks>
    ///     This method performs hard fail (Environment.Exit(-1)) if device is not allowed.
    ///     It attempts to read the serial number via SmartCard connection first, falling back
    ///     to other connection types if SmartCard is unavailable.
    /// </remarks>
    public async Task VerifyAsync(IYubiKey device)
    {
        ArgumentNullException.ThrowIfNull(device);

        int? serialNumber = await GetDeviceSerialNumberAsync(device).ConfigureAwait(false);

        if (serialNumber is null)
        {
            string errorMessage = "Unable to read device serial number. Cannot verify against allow list.";
            _logger?.LogCritical("{ErrorMessage}", errorMessage);
            Console.Error.WriteLine(errorMessage);
            Environment.Exit(-1);
        }

        if (!IsDeviceAllowed(serialNumber.Value))
        {
            string errorMessage = _provider.OnNotAllowedErrorMessage(serialNumber.Value);
            _logger?.LogCritical("{ErrorMessage}", errorMessage);
            Console.Error.WriteLine(errorMessage);
            Environment.Exit(-1); // Hard fail - device not authorized
        }

        _logger?.LogInformation("Device with serial number {SerialNumber} verified successfully", serialNumber.Value);
    }

    /// <summary>
    ///     Determines whether the specified serial number is in the allow list.
    /// </summary>
    /// <param name="serialNumber">The serial number to check.</param>
    /// <returns>True if the serial number is allowed; otherwise, false.</returns>
    public bool IsDeviceAllowed(int serialNumber) => _allowedSerials.Contains(serialNumber);

    /// <summary>
    ///     Attempts to read the serial number from a YubiKey device without verification.
    /// </summary>
    /// <param name="device">The device to read from.</param>
    /// <returns>The device serial number, or null if it could not be read.</returns>
    /// <remarks>
    ///     This method does NOT verify against the allow list. It only reads the serial number.
    ///     Use <see cref="VerifyAsync"/> for verification with hard fail.
    /// </remarks>
    public async Task<int?> GetSerialNumberAsync(IYubiKey device)
    {
        ArgumentNullException.ThrowIfNull(device);
        return await GetDeviceSerialNumberAsync(device).ConfigureAwait(false);
    }

    /// <summary>
    ///     Attempts to read the serial number from a YubiKey device.
    /// </summary>
    /// <param name="device">The device to read from.</param>
    /// <returns>The device serial number, or null if it could not be read.</returns>
    /// <remarks>
    ///     Tries SmartCard connection first (most reliable), then falls back to other
    ///     connection types if available.
    /// </remarks>
    private async Task<int?> GetDeviceSerialNumberAsync(IYubiKey device)
    {
        try
        {
            // Try SmartCard connection first (most common and reliable)
            using var smartCardConnection = await device.ConnectAsync<ISmartCardConnection>().ConfigureAwait(false);
            return await TryGetSerialViaManagementAsync(smartCardConnection).ConfigureAwait(false);
        }
        catch (NotSupportedException)
        {
            _logger?.LogWarning(
                "SmartCard connection not supported for device. Cannot read serial number.");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error reading device serial number");
            return null;
        }
    }

    /// <summary>
    ///     Attempts to read serial number via Management application.
    /// </summary>
    private async Task<int?> TryGetSerialViaManagementAsync(ISmartCardConnection connection)
    {
        try
        {
            using var mgmt = await ManagementSession<ISmartCardConnection>.CreateAsync(connection).ConfigureAwait(false);
            DeviceInfo deviceInfo = await mgmt.GetDeviceInfoAsync().ConfigureAwait(false);
            return deviceInfo.SerialNumber;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to read serial number via Management session");
            return null;
        }
    }
}
