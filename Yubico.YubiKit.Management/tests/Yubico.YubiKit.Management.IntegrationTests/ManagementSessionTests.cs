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
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Management.IntegrationTests;

/// <summary>
///     Example custom filter that only matches YubiKeys with firmware version 5.0 or higher.
///     This demonstrates the IYubiKeyFilter interface for custom filtering logic.
/// </summary>
public class ModernFirmwareFilter : IYubiKeyFilter
{
    #region IYubiKeyFilter Members

    public bool Matches(YubiKeyTestState device) =>
        device.FirmwareVersion >= new FirmwareVersion(5);

    public string GetDescription() => "Firmware >= 5.0.0";

    #endregion
}

/// <summary>
///     Integration tests for Management application.
///     Demonstrates usage of [Theory] + [WithYubiKey] attributes for device filtering.
/// </summary>
/// <remarks>
///     <para>
///         These tests use xUnit's [Theory] with [WithYubiKey] data attribute which provides:
///         - Automatic device discovery and allow list verification
///         - Declarative filtering via attribute properties
///         - Runs on ALL devices matching the specified criteria
///         - Support for multiple [WithYubiKey] attributes to test different configurations
///     </para>
///     <para>
///         IMPORTANT: Tests will only run on YubiKeys listed in appsettings.json.
///         Add your test device serial numbers to the AllowedSerialNumbers array.
///     </para>
/// </remarks>
public class ManagementSessionTests
{
    /// <summary>
    ///     Verify we can read device information.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey]
    public async Task GetDeviceInfo_ReturnsValidInformation(YubiKeyTestState state) =>
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            // Get device info via Management session
            var info = await mgmt.GetDeviceInfoAsync();

            // Verify we got valid device information
            Assert.True(info.SerialNumber > 0);

            // Device info should match what we cached
            Assert.Equal(deviceInfo.SerialNumber, info.SerialNumber);
            Assert.Equal(deviceInfo.FirmwareVersion, info.FirmwareVersion);
        });

    /// <summary>
    ///     Verify device has expected capabilities.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "4.1.0")] // Management requires 4.1.0+
    public async Task DeviceCapabilities_HasManagementSupport(YubiKeyTestState state) =>
        await state.WithManagementAsync((mgmt, deviceInfo) =>
        {
            // Verify device has USB support (all devices should have this)
            Assert.True(state.IsUsbTransport);

            // Verify firmware version is accessible
            Assert.NotNull(deviceInfo.FirmwareVersion);
            Assert.True(deviceInfo.FirmwareVersion.Major >= 4);
        });

    /// <summary>
    ///     Verify form factor matches expected type.
    /// </summary>
    [WithYubiKey(FormFactor = FormFactor.UsbAKeychain)]
    [WithYubiKey(FormFactor = FormFactor.UsbCKeychain)]
    [WithYubiKey(FormFactor = FormFactor.UsbABiometricKeychain)]
    [WithYubiKey(FormFactor = FormFactor.UsbCBiometricKeychain)]
    [WithYubiKey(FormFactor = FormFactor.UsbANano)]
    [WithYubiKey(FormFactor = FormFactor.UsbCNano)]
    [WithYubiKey(FormFactor = FormFactor.UsbCLightning)]
    [SkippableTheory]
    public async Task FormFactor_MatchesExpectedType(YubiKeyTestState state) =>
        await state.WithManagementAsync((mgmt, deviceInfo) =>
        {
            // Form factor should be one of the known types
            Assert.True(
                deviceInfo.FormFactor is FormFactor.UsbAKeychain
                    or FormFactor.UsbCKeychain
                    or FormFactor.UsbABiometricKeychain
                    or FormFactor.UsbCBiometricKeychain
                    or FormFactor.UsbANano
                    or FormFactor.UsbCNano
                    or FormFactor.UsbCLightning);
        });

    /// <summary>
    ///     Test filtered by form factor - only runs on Bio keys.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(FormFactor = FormFactor.UsbABiometricKeychain)]
    public async Task BiometricFeatures_RequiresBioKey(YubiKeyTestState state) =>
        await state.WithManagementAsync((mgmt, deviceInfo) =>
        {
            // If we get here, we know we have a Bio key (filter guaranteed it)
            Assert.Equal(FormFactor.UsbABiometricKeychain, deviceInfo.FormFactor);

            // Bio-specific tests would go here
        });

    /// <summary>
    ///     Test with multiple filter criteria in a single attribute.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(RequireUsb = true, CustomFilter = typeof(ModernFirmwareFilter))]
    public async Task AdvancedFeatures_RequireSpecificConfiguration(YubiKeyTestState state) =>
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            // Device meets all requirements (guaranteed by attribute filter)
            Assert.True(deviceInfo.FirmwareVersion.Major >= 5);
            Assert.True(state.IsUsbTransport);
            var info = await mgmt.GetDeviceInfoAsync();
            Assert.True(info.SerialNumber > 0);
        });

    /// <summary>
    ///     Test with multiple [WithYubiKey] attributes - demonstrates running test with different device configurations.
    ///     This test will run once for each device matching ANY of the specified criteria.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.0")]
    [WithYubiKey(FormFactor = FormFactor.UsbAKeychain)]
    public async Task MultipleConfigurations_TestRunsOnMatchingDevices(YubiKeyTestState state) =>
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            // This test runs on:
            // - All devices with firmware >= 5.0 (from first attribute)
            // - All USB-A Keychain devices (from second attribute)
            var info = await mgmt.GetDeviceInfoAsync();

            // At least one of the conditions must be true
            var hasModernFirmware = deviceInfo.FirmwareVersion.Major >= 5;
            var isUsbAKeychain = deviceInfo.FormFactor == FormFactor.UsbAKeychain;
            Assert.True(hasModernFirmware || isUsbAKeychain);
        });
}