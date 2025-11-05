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

using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Management.IntegrationTests;

/// <summary>
///     Integration tests for Management application.
///     Demonstrates usage of the YubiKeyTheory attribute and extension methods.
/// </summary>
/// <remarks>
///     <para>
///         These tests use <see cref="YubiKeyTheoryAttribute" /> which provides:
///         - Automatic device discovery and allow list verification
///         - Declarative filtering via attribute properties
///         - Runs on ALL devices matching the specified criteria
///     </para>
///     <para>
///         IMPORTANT: Tests will only run on YubiKeys listed in appsettings.json.
///         Add your test device serial numbers to the AllowedSerialNumbers array.
///     </para>
/// </remarks>
public class ManagementIntegrationTests
{
    /// <summary>
    ///     Verify we can read device information.
    /// </summary>
    [YubiKeyTheory]
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
    [YubiKeyTheory(MinFirmware = "4.1.0")] // Management requires 4.1.0+
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
    [YubiKeyTheory]
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
                    or FormFactor.UsbCLightning
                    or FormFactor.Unknown);
        });

    /// <summary>
    ///     Test filtered by form factor - only runs on Bio keys.
    /// </summary>
    [YubiKeyTheory(FormFactor = FormFactor.UsbABiometricKeychain)]
    public async Task BiometricFeatures_RequiresBioKey(YubiKeyTestState state) =>
        await state.WithManagementAsync((mgmt, deviceInfo) =>
        {
            // If we get here, we know we have a Bio key (filter guaranteed it)
            Assert.Equal(FormFactor.UsbABiometricKeychain, deviceInfo.FormFactor);

            // Bio-specific tests would go here
        });

    /// <summary>
    ///     Test with multiple filter criteria.
    /// </summary>
    [YubiKeyTheory(MinFirmware = "5.0.0", RequireUsb = true)]
    public async Task AdvancedFeatures_RequireSpecificConfiguration(YubiKeyTestState state) =>
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            // Device meets all requirements (guaranteed by attribute filter)
            Assert.True(deviceInfo.FirmwareVersion.Major >= 5);
            Assert.True(state.IsUsbTransport);

            var info = await mgmt.GetDeviceInfoAsync();
            Assert.True(info.SerialNumber > 0);
        });
}