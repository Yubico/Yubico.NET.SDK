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

using System.Reflection;
using Xunit;
using Xunit.Sdk;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     xUnit data attribute that provides filtered YubiKey devices as test parameters.
///     Use with [Theory] to run tests on devices matching specified criteria.
/// </summary>
/// <remarks>
///     <para>
///         This attribute works with xUnit's [Theory] to provide YubiKey devices as test data.
///         It automatically discovers, filters, and provides devices based on the specified criteria.
///         Multiple [WithYubiKey] attributes can be applied to run tests with different device configurations.
///     </para>
///     <para>
///         Usage:
///         <code>
///     // Single configuration
///     [Theory]
///     [WithYubiKey(MinFirmware = "5.7.2")]
///     public async Task TestName(YubiKeyTestState device)
///     {
///         // Test runs on devices with firmware >= 5.7.2
///     }
/// 
///     // Multiple configurations
///     [Theory]
///     [WithYubiKey(MinFirmware = "5.0")]
///     [WithYubiKey(FormFactor = FormFactor.UsbAKeychain)]
///     [WithYubiKey(CustomFilter = typeof(ProductionKeysOnly))]
///     public async Task TestName(YubiKeyTestState device)
///     {
///         // Test runs on devices matching ANY of the above criteria
///     }
/// 
///     // Custom filter example
///     public class ProductionKeysOnly : IYubiKeyFilter
///     {
///         public bool Matches(YubiKeyTestState device) => device.SerialNumber > 10_000_000;
///         public string GetDescription() => "Production keys (SN > 10000000)";
///     }
///         </code>
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class WithYubiKeyAttribute : DataAttribute
{
    /// <summary>
    ///     Gets or sets the minimum firmware version required.
    ///     Format: "major.minor.patch" (e.g., "5.7.2")
    /// </summary>
    public string? MinFirmware { get; set; }

    /// <summary>
    ///     Gets or sets the required form factor.
    ///     Use FormFactor.Unknown (default) to match any form factor.
    /// </summary>
    public FormFactor FormFactor { get; set; } = FormFactor.Unknown;

    /// <summary>
    ///     Gets or sets whether USB transport is required.
    /// </summary>
    public bool RequireUsb { get; set; }

    /// <summary>
    ///     Gets or sets whether NFC transport is required.
    /// </summary>
    public bool RequireNfc { get; set; }

    /// <summary>
    ///     Gets or sets the required capability (must be enabled).
    ///     Use DeviceCapabilities.None (default) to skip capability filtering.
    /// </summary>
    public DeviceCapabilities Capability { get; set; } = DeviceCapabilities.None;

    /// <summary>
    ///     Gets or sets whether FIPS-capable is required for the specified capability.
    ///     Use DeviceCapabilities.None (default) to skip FIPS-capable filtering.
    /// </summary>
    public DeviceCapabilities FipsCapable { get; set; } = DeviceCapabilities.None;

    /// <summary>
    ///     Gets or sets whether FIPS-approved mode is required for the specified capability.
    ///     Use DeviceCapabilities.None (default) to skip FIPS-approved filtering.
    /// </summary>
    public DeviceCapabilities FipsApproved { get; set; } = DeviceCapabilities.None;

    /// <summary>
    ///     Gets or sets a custom filter type for advanced filtering logic.
    ///     Must implement <see cref="IYubiKeyFilter" /> and have a parameterless constructor.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use this property to specify complex filtering logic that cannot be expressed
    ///         through the built-in properties. The custom filter is applied AFTER all
    ///         built-in filters (MinFirmware, FormFactor, etc.).
    ///     </para>
    ///     <para>
    ///         Example:
    ///         <code>
    ///     public class ProductionKeysOnly : IYubiKeyFilter
    ///     {
    ///         public bool Matches(YubiKeyTestState device) => device.SerialNumber > 10_000_000;
    ///         public string GetDescription() => "Production keys (SN > 10000000)";
    ///     }
    /// 
    ///     [Theory]
    ///     [WithYubiKey(CustomFilter = typeof(ProductionKeysOnly))]
    ///     public async Task Test(YubiKeyTestState device) { }
    ///         </code>
    ///     </para>
    /// </remarks>
    public Type? CustomFilter { get; set; }

    /// <summary>
    ///     Returns filtered YubiKey devices as test data.
    ///     Called by xUnit's Theory discoverer.
    /// </summary>
    public override IEnumerable<object[]>
        GetData(MethodInfo testMethod) // TODO why is this called for tests that I haven't specifically run? Inside Rider.
    {
        // Get all authorized devices from shared infrastructure
        var allDevices = YubiKeyTestInfrastructure.AllAuthorizedDevices;
        if (allDevices.Count == 0)
        {
            var errorMessage =
                $"No authorized YubiKey devices available for test '{testMethod.Name}'.\n" +
                $"\n" +
                $"This means either:\n" +
                $"  1. No YubiKeys are connected to this machine\n" +
                $"  2. Connected YubiKeys are not in the allow list (appsettings.json)\n" +
                $"\n" +
                $"To fix: Check YubiKeyTestInfrastructure initialization output for details.";

            Console.Error.WriteLine($"[WithYubiKey] {errorMessage}");

            throw new InvalidOperationException(errorMessage);
        }

        // Filter devices using shared infrastructure
        var filteredDevices = YubiKeyTestInfrastructure.FilterDevices(
            allDevices,
            MinFirmware,
            FormFactor,
            RequireUsb,
            RequireNfc,
            Capability,
            FipsCapable,
            FipsApproved,
            CustomFilter).ToList();

        if (filteredDevices.Count == 0)
        {
            var criteria = YubiKeyTestInfrastructure.GetFilterCriteriaDescription(
                MinFirmware,
                FormFactor,
                RequireUsb,
                RequireNfc,
                Capability,
                FipsCapable,
                FipsApproved,
                CustomFilter);

            var availableDevices = string.Join(", ", allDevices.Select(d =>
                $"SN:{d.SerialNumber} (FW:{d.FirmwareVersion}, {d.FormFactor})"));

            var errorMessage =
                $"No YubiKey devices match criteria for test '{testMethod.Name}'.\n" +
                $"\n" +
                $"Required criteria: {criteria}\n" +
                $"\n" +
                $"Available devices ({allDevices.Count}): {availableDevices}\n" +
                $"\n" +
                $"To fix: Connect a YubiKey matching the criteria, or adjust the test requirements.";

            Console.Error.WriteLine($"[WithYubiKey] {errorMessage}");

            throw new SkipException(errorMessage);
        }

        Console.WriteLine(
            $"[WithYubiKey] Test '{testMethod.Name}' will run on {filteredDevices.Count} device(s)");

        // Yield each matching device as test data
        foreach (var device in filteredDevices) yield return [device];
    }
}