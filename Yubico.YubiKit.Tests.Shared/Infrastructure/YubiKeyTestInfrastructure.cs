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
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     Centralized infrastructure for YubiKey integration tests.
///     Provides shared static instances of logger, allow list, device discovery, and filtering logic.
/// </summary>
/// <remarks>
///     This class is used by <see cref="YubiKeyTheoryDiscoverer" />
///     to ensure consistent behavior and avoid code duplication.
/// </remarks>
internal static class YubiKeyTestInfrastructure
{
    private static readonly ILoggerFactory s_loggerFactory = LoggerFactory.Create(builder =>
    {
        builder
            .AddConsole()
            .SetMinimumLevel(LogLevel.Information);
    });

    /// <summary>
    ///     Gets the shared AllowList instance for test infrastructure.
    /// </summary>
    /// <remarks>
    ///     Initialized once per test run with AppSettingsAllowListProvider.
    ///     Thread-safe via static initialization.
    /// </remarks>
    public static AllowList AllowList { get; } = new(
        new AppSettingsAllowListProvider(),
        s_loggerFactory.CreateLogger<AllowList>());

    /// <summary>
    ///     Gets all authorized YubiKey devices discovered during test run initialization.
    /// </summary>
    /// <remarks>
    ///     Devices are discovered once per test run via lazy initialization.
    ///     Includes only devices that passed allow list verification.
    /// </remarks>
    public static IReadOnlyList<YubiKeyTestState> AllAuthorizedDevices { get; } =
        InitializeDevicesAsync();

    /// <summary>
    ///     Filters devices based on test attribute criteria.
    /// </summary>
    /// <param name="devices">Devices to filter.</param>
    /// <param name="minFirmware">Minimum firmware version string (e.g., "5.7.2").</param>
    /// <param name="formFactor">Required form factor.</param>
    /// <param name="requireUsb">Whether USB transport is required.</param>
    /// <param name="requireNfc">Whether NFC transport is required.</param>
    /// <param name="capability">Required capability.</param>
    /// <param name="fipsCapable">Required FIPS-capable capability.</param>
    /// <param name="fipsApproved">Required FIPS-approved capability.</param>
    /// <param name="customFilterType">Optional custom filter type implementing IYubiKeyFilter.</param>
    /// <returns>Filtered list of devices matching all criteria.</returns>
    public static IEnumerable<YubiKeyTestState> FilterDevices(
        IEnumerable<YubiKeyTestState> devices,
        string? minFirmware,
        FormFactor formFactor,
        bool requireUsb,
        bool requireNfc,
        DeviceCapabilities capability,
        DeviceCapabilities fipsCapable,
        DeviceCapabilities fipsApproved,
        Type? customFilterType = null)
    {
        var filtered = devices;

        // Filter by minimum firmware
        if (!string.IsNullOrEmpty(minFirmware))
        {
            var minFw = FirmwareVersion.FromString(minFirmware);
            if (minFw is not null)
                filtered = filtered.Where(d => d.FirmwareVersion >= minFw);
        }

        // Filter by form factor
        if (formFactor != FormFactor.Unknown)
            filtered = filtered.Where(d => d.FormFactor == formFactor);

        // Filter by USB transport
        if (requireUsb)
            filtered = filtered.Where(d => d.IsUsbTransport);

        // Filter by NFC transport
        if (requireNfc)
            filtered = filtered.Where(d => d.IsNfcTransport);

        // Filter by capability
        if (capability != DeviceCapabilities.None)
            filtered = filtered.Where(d => d.HasCapability(capability));

        // Filter by FIPS-capable
        if (fipsCapable != DeviceCapabilities.None)
            filtered = filtered.Where(d => d.IsFipsCapable(fipsCapable));

        // Filter by FIPS-approved
        if (fipsApproved != DeviceCapabilities.None)
            filtered = filtered.Where(d => d.IsFipsApproved(fipsApproved));

        // Apply custom filter if provided
        if (customFilterType is not null)
        {
            var filter = InstantiateCustomFilter(customFilterType);
            if (filter is not null)
                filtered = filtered.Where(d => filter.Matches(d));
        }

        return filtered;
    }

    /// <summary>
    ///     Gets a human-readable description of filter criteria for diagnostic output.
    /// </summary>
    public static string GetFilterCriteriaDescription(
        string? minFirmware,
        FormFactor formFactor,
        bool requireUsb,
        bool requireNfc,
        DeviceCapabilities capability,
        DeviceCapabilities fipsCapable,
        DeviceCapabilities fipsApproved,
        Type? customFilterType = null)
    {
        var criteria = new List<string>();

        if (minFirmware is not null)
            criteria.Add($"MinFirmware >= {minFirmware}");

        if (formFactor != FormFactor.Unknown)
            criteria.Add($"FormFactor = {formFactor}");

        if (requireUsb)
            criteria.Add("Transport = USB");

        if (requireNfc)
            criteria.Add("Transport = NFC");

        if (capability != DeviceCapabilities.None)
            criteria.Add($"Capability = {capability}");

        if (fipsCapable != DeviceCapabilities.None)
            criteria.Add($"FipsCapable = {fipsCapable}");

        if (fipsApproved != DeviceCapabilities.None)
            criteria.Add($"FipsApproved = {fipsApproved}");

        if (customFilterType is not null)
        {
            var filter = InstantiateCustomFilter(customFilterType);
            if (filter is not null)
                criteria.Add($"CustomFilter = {filter.GetDescription()}");
            else
                criteria.Add($"CustomFilter = {customFilterType.Name} (failed to instantiate)");
        }

        return criteria.Count > 0 ? string.Join(", ", criteria) : "None (all devices)";
    }

    /// <summary>
    ///     Instantiates a custom filter from the specified type.
    /// </summary>
    /// <param name="filterType">The type implementing IYubiKeyFilter.</param>
    /// <returns>An instance of the filter, or null if instantiation fails.</returns>
    private static IYubiKeyFilter? InstantiateCustomFilter(Type filterType)
    {
        try
        {
            // Validate type implements IYubiKeyFilter
            if (!typeof(IYubiKeyFilter).IsAssignableFrom(filterType))
            {
                Console.Error.WriteLine(
                    $"[YubiKey Infrastructure] ERROR: Custom filter type '{filterType.FullName}' " +
                    $"does not implement IYubiKeyFilter");
                return null;
            }

            // Validate type has parameterless constructor
            var constructor = filterType.GetConstructor(Type.EmptyTypes);
            if (constructor is null)
            {
                Console.Error.WriteLine(
                    $"[YubiKey Infrastructure] ERROR: Custom filter type '{filterType.FullName}' " +
                    $"does not have a parameterless constructor");
                return null;
            }

            // Instantiate the filter
            var filter = Activator.CreateInstance(filterType) as IYubiKeyFilter;
            if (filter is null)
            {
                Console.Error.WriteLine(
                    $"[YubiKey Infrastructure] ERROR: Failed to instantiate custom filter '{filterType.FullName}'");
                return null;
            }

            return filter;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[YubiKey Infrastructure] ERROR: Exception while instantiating custom filter '{filterType.FullName}': {ex.Message}");
            return null;
        }
    }

    #region Static Device Initialization

    /// <summary>
    ///     Initializes all authorized YubiKey devices (runs once per test run).
    /// </summary>
    /// <remarks>
    ///     This method:
    ///     1. Discovers all connected YubiKey devices
    ///     2. Filters by allow list (hard fail if no authorized devices)
    ///     3. Retrieves device information for each authorized device
    ///     4. Caches devices for use in tests
    ///     Uses GetAwaiter().GetResult() to call async methods from static context.
    ///     This is acceptable for test infrastructure initialization (not hot path).
    /// </remarks>
    private static List<YubiKeyTestState> InitializeDevicesAsync()
    {
        Console.WriteLine("[YubiKey Infrastructure] Initializing devices (once per test run)...");

        try
        {
            // Discover all devices
            var allDevices = YubiKey.FindAllAsync().GetAwaiter().GetResult();
            Console.WriteLine($"[YubiKey Infrastructure] Found {allDevices.Count} device(s)");

            if (allDevices.Count == 0)
            {
                Console.WriteLine("[YubiKey Infrastructure] No YubiKey devices found");
                return [];
            }

            // Filter by allow list
            var authorizedDevices = new List<YubiKeyTestState>();
            var filteredCount = 0;

            foreach (var device in allDevices)
            {
                var serial = AllowList.GetSerialNumberAsync(device).GetAwaiter().GetResult();

                if (serial.HasValue)
                {
                    if (AllowList.IsDeviceAllowed(serial.Value))
                    {
                        // Get device info
                        var info = device.GetDeviceInfoAsync().GetAwaiter().GetResult();
                        var testDevice = new YubiKeyTestState(device, info);
                        authorizedDevices.Add(testDevice);

                        // Add to cache for deserialization
                        YubiKeyDeviceCache.AddDevice(testDevice);

                        Console.WriteLine(
                            $"[YubiKey Infrastructure] Device SN:{serial.Value} authorized " +
                            $"(FW:{info.FirmwareVersion}, {info.FormFactor})");
                    }
                    else
                    {
                        filteredCount++;
                        Console.WriteLine(
                            $"[YubiKey Infrastructure] Device SN:{serial.Value} FILTERED (not in allow list)");
                    }
                }
                else
                {
                    filteredCount++;
                    Console.WriteLine("[YubiKey Infrastructure] Device with unknown serial FILTERED");
                }
            }

            // Hard fail if no authorized devices
            if (authorizedDevices.Count == 0)
            {
                var errorMessage =
                    "═══════════════════════════════════════════════════════════════════════════\n" +
                    "                        NO AUTHORIZED DEVICES FOUND\n" +
                    "═══════════════════════════════════════════════════════════════════════════\n" +
                    "\n" +
                    $"Found {allDevices.Count} YubiKey device(s), but NONE are authorized for testing.\n" +
                    "\n" +
                    "Tests can only run on YubiKeys explicitly listed in the allow list.\n" +
                    "Add device serial numbers to appsettings.json in your test project:\n" +
                    "\n" +
                    "{\n" +
                    "  \"YubiKeyTests\": {\n" +
                    "    \"AllowedSerialNumbers\": [\n" +
                    "      12345678,\n" +
                    "      87654321\n" +
                    "    ]\n" +
                    "  }\n" +
                    "}\n" +
                    "\n" +
                    "═══════════════════════════════════════════════════════════════════════════\n" +
                    "                        TESTS WILL NOT RUN\n" +
                    "═══════════════════════════════════════════════════════════════════════════";

                Console.Error.WriteLine(errorMessage);
                Environment.Exit(-1); // Hard fail
            }

            Console.WriteLine(
                $"[YubiKey Infrastructure] Initialization complete: {authorizedDevices.Count} authorized, {filteredCount} filtered");

            return authorizedDevices;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[YubiKey Infrastructure] FATAL: Device initialization failed: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            Environment.Exit(-1);
            throw; // Unreachable, but needed for compiler
        }
    }

    #endregion
}