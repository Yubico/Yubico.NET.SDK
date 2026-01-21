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
using Yubico.YubiKit.Core.Interfaces;
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
    ///     Gets or sets the required connection type.
    ///     Use ConnectionType.Unknown (default) to match any connection type.
    /// </summary>
    public ConnectionType ConnectionType { get; set; } = ConnectionType.Unknown;

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
    /// <remarks>
    ///     <para>
    ///         xUnit calls GetData() during BOTH discovery and execution phases.
    ///         We cannot reliably distinguish between them.
    ///     </para>
    ///     <para>
    ///         Strategy: Always return placeholders with filter descriptions.
    ///         The placeholder's Device property uses lazy binding - when the test
    ///         actually accesses Device during execution, it initializes the infrastructure
    ///         and binds to a real YubiKey.
    ///     </para>
    /// </remarks>
    public override IEnumerable<object[]>
        GetData(MethodInfo testMethod)
    {
        // Always return a placeholder - device binding happens lazily during execution
        // Create a unique placeholder for each attribute to avoid duplicate test IDs
        // when multiple [WithYubiKey] attributes are applied to the same test method
        var filterDescription = BuildFilterDescription();
        yield return [YubiKeyTestState.CreatePlaceholder(filterDescription)];
    }

    /// <summary>
    ///     Builds a description of the filter criteria for this attribute.
    ///     Used to create unique placeholders when multiple attributes are applied.
    /// </summary>
    private string BuildFilterDescription()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(MinFirmware))
            parts.Add($"FW>={MinFirmware}");

        if (FormFactor != FormFactor.Unknown)
            parts.Add($"FF={FormFactor}");

        if (RequireUsb)
            parts.Add("USB");

        if (RequireNfc)
            parts.Add("NFC");

        if (ConnectionType != ConnectionType.Unknown)
            parts.Add($"Conn={ConnectionType}");

        if (Capability != DeviceCapabilities.None)
            parts.Add($"Cap={Capability}");

        if (FipsCapable != DeviceCapabilities.None)
            parts.Add($"FipsCap={FipsCapable}");

        if (FipsApproved != DeviceCapabilities.None)
            parts.Add($"FipsAppr={FipsApproved}");

        if (CustomFilter is not null)
            parts.Add($"Filter={CustomFilter.Name}");

        return parts.Count > 0 ? string.Join(",", parts) : "All";
    }
}