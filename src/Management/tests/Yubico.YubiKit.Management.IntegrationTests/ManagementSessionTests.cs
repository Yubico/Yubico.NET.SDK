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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Management.IntegrationTests;

/// <summary>
///     Integration tests for Management application.
///     Demonstrates usage of [Theory] + [WithYubiKey] attributes for device filtering.
/// </summary>
/// <remarks>
///     <para>
///         These tests use [SkippableTheory] with the [WithYubiKey] data attribute, which provides:
///         - Automatic device discovery and allow list verification
///         - Declarative filtering via attribute properties
///         - One row per attribute, bound to the first matching authorized device
///         - Independent binding for multiple attributes, potentially to different physical devices
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
    [WithYubiKey(MinFirmware = "4.1.0")] // Supported read minimum; not a host-side read preflight assertion.
    public async Task DeviceCapabilities_HasManagementSupport(YubiKeyTestState state) =>
        await state.WithManagementAsync((mgmt, deviceInfo) =>
        {
            // Check this row's cached USB-transport metadata.
            Assert.True(state.IsUsbTransport);

            // Verify firmware version is accessible
            Assert.NotNull(deviceInfo.FirmwareVersion);
            Assert.True(deviceInfo.FirmwareVersion.IsAtLeast(4, 0, 0));
        });

    /// <summary>
    ///     Verify form factor matches expected type.
    /// </summary>
    /// <remarks>
    ///     Selection and assertions use decoded metadata. They do not validate Core's handling of
    ///     product flags in the wire form-factor byte; exact filters can miss flagged variants.
    /// </remarks>
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
    ///     Checks cached form-factor metadata for a row selected as a USB-A Bio key; no biometric operation is tested.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(FormFactor = FormFactor.UsbABiometricKeychain)]
    public async Task BiometricFeatures_RequiresBioKey(YubiKeyTestState state) =>
        await state.WithManagementAsync((mgmt, deviceInfo) =>
        {
            // The filter and assertion both use the same cached decoded form factor.
            Assert.Equal(FormFactor.UsbABiometricKeychain, deviceInfo.FormFactor);

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
            Assert.True(deviceInfo.FirmwareVersion.IsAtLeast(5, 0, 0));
            Assert.True(state.IsUsbTransport);
            var info = await mgmt.GetDeviceInfoAsync();
            Assert.True(info.SerialNumber > 0);
        });

    /// <summary>
    ///     Supplies two independently bound rows. The first attribute's two-component firmware string
    ///     is not parsed by the current harness, so its intended minimum is not applied. The assertion
    ///     below can therefore fail on an older device that is not reported as a USB-A keychain.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.0")]
    [WithYubiKey(FormFactor = FormFactor.UsbAKeychain)]
    public async Task MultipleConfigurations_TestRunsOnMatchingDevices(YubiKeyTestState state) =>
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            // One row has no effective firmware filter; the other requires the decoded USB-A keychain form factor.
            var info = await mgmt.GetDeviceInfoAsync();

            // Intended condition; not guaranteed for the first row while its firmware criterion is malformed.
            var hasModernFirmware = deviceInfo.FirmwareVersion.IsAtLeast(5, 0, 0);
            var isUsbAKeychain = deviceInfo.FormFactor == FormFactor.UsbAKeychain;
            Assert.True(hasModernFirmware || isUsbAKeychain);
        });
}