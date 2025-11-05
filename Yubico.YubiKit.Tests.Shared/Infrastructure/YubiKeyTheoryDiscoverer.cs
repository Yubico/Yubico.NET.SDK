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

using Xunit.Abstractions;
using Xunit.Sdk;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     xUnit test case discoverer for YubiKeyTheoryAttribute.
///     Discovers and creates test cases for each matching YubiKey device.
/// </summary>
public class YubiKeyTheoryDiscoverer(IMessageSink diagnosticMessageSink) : IXunitTestCaseDiscoverer
{
    /// <summary>
    ///     Discovers test cases for a test method with YubiKeyTheoryAttribute.
    /// </summary>
    public IEnumerable<IXunitTestCase> Discover(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        ITestMethod testMethod,
        IAttributeInfo factAttribute)
    {
        // Get all authorized devices from shared infrastructure
        var allDevices = YubiKeyTestInfrastructure.AllAuthorizedDevices;
        if (allDevices.Count == 0)
        {
            diagnosticMessageSink.OnMessage(new DiagnosticMessage(
                $"[YubiKeyTheory] No authorized devices available for test '{testMethod.Method.Name}'"));
            yield break;
        }

        // Get filter properties from attribute
        var minFirmware = factAttribute.GetNamedArgument<string?>(nameof(YubiKeyTheoryAttribute.MinFirmware));
        var formFactor = factAttribute.GetNamedArgument<FormFactor>(nameof(YubiKeyTheoryAttribute.FormFactor));
        var requireUsb = factAttribute.GetNamedArgument<bool>(nameof(YubiKeyTheoryAttribute.RequireUsb));
        var requireNfc = factAttribute.GetNamedArgument<bool>(nameof(YubiKeyTheoryAttribute.RequireNfc));
        var capability = factAttribute.GetNamedArgument<DeviceCapabilities>(nameof(YubiKeyTheoryAttribute.Capability));
        var fipsCapable = factAttribute.GetNamedArgument<DeviceCapabilities>(nameof(YubiKeyTheoryAttribute.FipsCapable));
        var fipsApproved = factAttribute.GetNamedArgument<DeviceCapabilities>(nameof(YubiKeyTheoryAttribute.FipsApproved));

        // Filter devices using shared infrastructure (materialize to avoid multiple enumerations)
        var filteredDevices = YubiKeyTestInfrastructure.FilterDevices(
            allDevices,
            minFirmware,
            formFactor,
            requireUsb,
            requireNfc,
            capability,
            fipsCapable,
            fipsApproved).ToList();

        if (filteredDevices.Count == 0)
        {
            var criteria = YubiKeyTestInfrastructure.GetFilterCriteriaDescription(
                minFirmware,
                formFactor,
                requireUsb,
                requireNfc,
                capability,
                fipsCapable,
                fipsApproved);

            diagnosticMessageSink.OnMessage(new DiagnosticMessage(
                $"[YubiKeyTheory] No devices match criteria for test '{testMethod.Method.Name}'. " +
                $"Criteria: {criteria}"));
            yield break;
        }

        diagnosticMessageSink.OnMessage(new DiagnosticMessage(
            $"[YubiKeyTheory] Test '{testMethod.Method.Name}' will run on {filteredDevices.Count} device(s)"));

        // Create one test case per matching device
        foreach (var device in filteredDevices)
        {
            yield return new XunitTestCase(
                diagnosticMessageSink,
                discoveryOptions.MethodDisplayOrDefault(),
                discoveryOptions.MethodDisplayOptionsOrDefault(),
                testMethod,
                [device]);
        }
    }
}
