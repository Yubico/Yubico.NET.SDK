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

using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     Provides access to authorized YubiKey devices for integration tests.
///     Use this class when you need AllowList validation without the [WithYubiKey] attribute.
/// </summary>
/// <remarks>
///     <para>
///         This class wraps <see cref="YubiKeyTestInfrastructure"/> to provide public access
///         to authorized devices. It enforces AllowList verification for all devices.
///     </para>
///     <para>
///         <strong>Usage Examples:</strong>
///         <code>
///     // Get all authorized devices
///     var devices = AuthorizedDevices.GetAll();
///     
///     // Get the first authorized device (throws if none available)
///     var device = AuthorizedDevices.GetFirstOrSkip();
///     
///     // Filter authorized devices
///     var filtered = AuthorizedDevices.GetFiltered(criteria);
///     
///     // Check if a specific device is authorized
///     var isAuthorized = AuthorizedDevices.IsAllowed(serialNumber);
///         </code>
///     </para>
/// </remarks>
public static class AuthorizedDevices
{
    /// <summary>
    ///     Gets all authorized YubiKey devices discovered during test run initialization.
    /// </summary>
    /// <remarks>
    ///     Devices are discovered lazily on first access. Only devices that passed
    ///     AllowList verification are included.
    /// </remarks>
    /// <returns>A read-only list of all authorized devices.</returns>
    public static IReadOnlyList<YubiKeyTestState> GetAll() =>
        YubiKeyTestInfrastructure.AllAuthorizedDevices;

    /// <summary>
    ///     Gets the first authorized device, or skips the test if none available.
    /// </summary>
    /// <returns>The first authorized device.</returns>
    /// <exception cref="Xunit.SkipException">Thrown when no authorized devices are available.</exception>
    public static YubiKeyTestState GetFirstOrSkip()
    {
        var devices = GetAll();
        if (devices.Count == 0)
        {
            throw new Xunit.SkipException(
                "No authorized YubiKey devices available. " +
                "Add device serial numbers to appsettings.json AllowedSerialNumbers array.");
        }

        return devices[0];
    }

    /// <summary>
    ///     Gets the first authorized device matching the specified criteria, or skips the test.
    /// </summary>
    /// <param name="criteria">The filter criteria to apply.</param>
    /// <returns>The first matching authorized device.</returns>
    /// <exception cref="Xunit.SkipException">Thrown when no matching authorized devices are available.</exception>
    public static YubiKeyTestState GetFirstOrSkip(FilterCriteria criteria)
    {
        var filtered = GetFiltered(criteria).ToList();
        if (filtered.Count == 0)
        {
            var available = string.Join(", ", GetAll().Select(d => d.ToString()));
            throw new Xunit.SkipException(
                $"No YubiKey matches filter criteria: {criteria.GetDescription()}. " +
                $"Available devices: [{available}]");
        }

        return filtered[0];
    }

    /// <summary>
    ///     Gets authorized devices filtered by the specified criteria.
    /// </summary>
    /// <param name="criteria">The filter criteria to apply.</param>
    /// <returns>An enumerable of devices matching the criteria.</returns>
    public static IEnumerable<YubiKeyTestState> GetFiltered(FilterCriteria criteria) =>
        YubiKeyTestInfrastructure.FilterDevices(GetAll(), criteria);

    /// <summary>
    ///     Gets authorized devices filtered by minimum firmware version.
    /// </summary>
    /// <param name="minFirmware">Minimum firmware version (e.g., "5.0.0").</param>
    /// <returns>An enumerable of devices with firmware >= minFirmware.</returns>
    public static IEnumerable<YubiKeyTestState> GetByMinFirmware(string minFirmware) =>
        GetFiltered(new FilterCriteria { MinFirmware = minFirmware });

    /// <summary>
    ///     Gets authorized devices filtered by connection type.
    /// </summary>
    /// <param name="connectionType">The required connection type.</param>
    /// <returns>An enumerable of devices with the specified connection type.</returns>
    public static IEnumerable<YubiKeyTestState> GetByConnectionType(ConnectionType connectionType) =>
        GetFiltered(new FilterCriteria { ConnectionType = connectionType });

    /// <summary>
    ///     Gets authorized devices filtered by form factor.
    /// </summary>
    /// <param name="formFactor">The required form factor.</param>
    /// <returns>An enumerable of devices with the specified form factor.</returns>
    public static IEnumerable<YubiKeyTestState> GetByFormFactor(FormFactor formFactor) =>
        GetFiltered(new FilterCriteria { FormFactor = formFactor });

    /// <summary>
    ///     Gets authorized devices filtered by required capability.
    /// </summary>
    /// <param name="capability">The required capability.</param>
    /// <returns>An enumerable of devices with the specified capability enabled.</returns>
    public static IEnumerable<YubiKeyTestState> GetByCapability(DeviceCapabilities capability) =>
        GetFiltered(new FilterCriteria { Capability = capability });

    /// <summary>
    ///     Checks if a specific serial number is in the AllowList.
    /// </summary>
    /// <param name="serialNumber">The serial number to check.</param>
    /// <returns>True if the device is allowed; otherwise, false.</returns>
    public static bool IsAllowed(int? serialNumber) =>
        YubiKeyTestInfrastructure.AllowList.IsDeviceAllowed(serialNumber);

    /// <summary>
    ///     Gets whether any authorized devices are available.
    /// </summary>
    public static bool HasDevices => GetAll().Count > 0;

    /// <summary>
    ///     Gets the count of authorized devices.
    /// </summary>
    public static int Count => GetAll().Count;
}

