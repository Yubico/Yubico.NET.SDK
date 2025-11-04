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
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Management.IntegrationTests;

/// <summary>
///     Integration tests for Management application.
///     Demonstrates usage of the test infrastructure.
/// </summary>
/// <remarks>
///     <para>
///         These tests use <see cref="ManagementTestFixture" /> which provides:
///         - Automatic device acquisition and allow list verification
///         - Device information via <see cref="ManagementTestFixture.State" />
///         - Test requirement helpers (RequireFirmware, RequireFormFactor, etc.)
///     </para>
///     <para>
///         IMPORTANT: Tests will only run on YubiKeys listed in appsettings.json.
///         Add your test device serial numbers to the AllowedSerialNumbers array.
///     </para>
/// </remarks>
public class ManagementIntegrationTests : ManagementTestFixture
{
    /// <summary>
    ///     Example test: Verify we can read device information.
    /// </summary>
    [SkippableFact]
    public async Task GetDeviceInfo_ReturnsValidInformation() =>
        // The fixture automatically acquires the device and verifies the allow list
        // before this test runs (via IAsyncLifetime.InitializeAsync)
        await State.WithManagementAsync(async (mgmt, state) =>
        {
            // Get device info via Management session
            var deviceInfo = await mgmt.GetDeviceInfoAsync();

            // Verify we got valid device information
            Assert.True(deviceInfo.SerialNumber > 0);

            // Device info should match what the fixture cached
            Assert.Equal(state.DeviceInfo.SerialNumber, deviceInfo.SerialNumber);
            Assert.Equal(state.DeviceInfo.FirmwareVersion, deviceInfo.FirmwareVersion);
        });

    /// <summary>
    ///     Example test: Verify device has expected capabilities.
    /// </summary>
    [SkippableFact]
    public async Task DeviceCapabilities_HasManagementSupport()
    {
        // Require minimum firmware version
        RequireFirmware(4, 1, 0); // Management application requires 4.1.0+

        await State.WithManagementAsync((mgmt, state) =>
        {
            // Verify device has USB support (all devices should have this)
            Assert.True(state.IsUsbTransport);

            // Verify firmware version is accessible
            Assert.NotNull(state.FirmwareVersion);
            Assert.True(state.FirmwareVersion.Major >= 4);
        });
    }

    /// <summary>
    ///     Example test: Conditional execution based on form factor.
    /// </summary>
    [SkippableFact]
    public async Task FormFactor_MatchesExpectedType() =>
        await State.WithManagementAsync((mgmt, state) =>
        {
            // Form factor should be one of the known types
            Assert.True(
                state.FormFactor is FormFactor.UsbAKeychain
                    or FormFactor.UsbCKeychain
                    or FormFactor.UsbABiometricKeychain
                    or FormFactor.UsbCBiometricKeychain
                    or FormFactor.UsbANano
                    or FormFactor.UsbCNano
                    or FormFactor.UsbCLightning
                    or FormFactor.Unknown);
        });

    /// <summary>
    ///     Example test: Skip test if device doesn't meet requirements.
    /// </summary>
    [SkippableFact]
    public async Task BiometricFeatures_RequiresBioKey()
    {
        // This test will be skipped unless the device is a Bio key
        RequireFormFactor(FormFactor.UsbABiometricKeychain);

        await State.WithManagementAsync((mgmt, state) =>
        {
            // If we get here, we know we have a Bio key
            Assert.Equal(FormFactor.UsbABiometricKeychain, state.FormFactor);

            // Bio-specific tests would go here
        });
    }

    /// <summary>
    ///     Example test: Multiple requirements at once.
    /// </summary>
    [SkippableFact]
    public async Task AdvancedFeatures_RequireSpecificConfiguration()
    {
        // Require minimum firmware AND specific transport
        RequireDevice(
            new FirmwareVersion(5),
            transport: Transport.Usb);

        await State.WithManagementAsync(async (mgmt, state) =>
        {
            // If we get here, device meets all requirements
            Assert.True(state.FirmwareVersion.Major >= 5);
            Assert.True(state.IsUsbTransport);

            var deviceInfo = await mgmt.GetDeviceInfoAsync();
            Assert.True(deviceInfo.SerialNumber > 0);
        });
    }
}