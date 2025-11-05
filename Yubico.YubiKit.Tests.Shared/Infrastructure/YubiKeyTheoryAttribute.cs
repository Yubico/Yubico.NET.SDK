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

using Xunit;
using Xunit.Sdk;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     xUnit theory attribute that provides YubiKey devices as test parameters.
///     Combines [Theory] and [YubiKeyData] into a single attribute for cleaner test syntax.
/// </summary>
/// <remarks>
///     <para>
///         This attribute simplifies YubiKey integration tests by eliminating the need for
///         both [Theory] and [YubiKeyData] attributes. It automatically discovers and filters
///         devices based on the specified criteria.
///     </para>
///     <para>
///         Usage:
///         <code>
///     [YubiKeyTheory]
///     public async Task TestName(YubiKeyTestDevice device)
///     {
///         // Test runs on ALL authorized devices
///     }
///
///     [YubiKeyTheory(MinFirmware = "5.7.2", FormFactor = FormFactor.UsbAKeychain)]
///     public async Task TestName(YubiKeyTestDevice device)
///     {
///         // Test runs only on matching devices
///     }
///         </code>
///     </para>
/// </remarks>
[XunitTestCaseDiscoverer("Yubico.YubiKit.Tests.Shared.Infrastructure.YubiKeyTheoryDiscoverer", "Yubico.YubiKit.Tests.Shared")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class YubiKeyTheoryAttribute : FactAttribute
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
}
