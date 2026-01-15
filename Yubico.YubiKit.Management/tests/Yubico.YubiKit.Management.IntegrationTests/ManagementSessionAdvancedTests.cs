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

using System.Diagnostics;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Management.IntegrationTests;

/// <summary>
///     Advanced integration tests demonstrating YubiKeyTheoryAttribute for multi-device testing.
///     These tests showcase declarative device filtering and parameterized test execution.
/// </summary>
/// <remarks>
///     <para>
///         Each test method decorated with [YubiKeyTheory] will run on ALL devices matching the criteria.
///         For example, if you have 3 authorized devices (2 USB keys + 1 Bio key) and use
///         [YubiKeyTheory(FormFactor = FormFactor.UsbAKeychain)], the test runs twice (once per USB key).
///     </para>
///     <para>
///         This pattern is ideal for:
///         - Testing across different firmware versions
///         - Verifying behavior on different form factors
///         - Ensuring FIPS-capable keys work correctly
///         - Testing transport-specific features (USB vs NFC)
///     </para>
/// </remarks>
public class ManagementSessionAdvancedTests
{
    /// <summary>
    ///     Basic example: Runs on ALL authorized devices.
    ///     Test executes once per device in the allow list.
    /// </summary>
    [Theory]
    [WithYubiKey]
    public async Task GetDeviceInfo_AllDevices_ReturnsValidData(YubiKeyTestState state) =>
        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            // Act
            var deviceInfo = await mgmt.GetDeviceInfoAsync();

            // Assert - Verify device info matches what was cached during discovery
            Assert.Equal(state.SerialNumber, deviceInfo.SerialNumber);
            Assert.Equal(state.FirmwareVersion, deviceInfo.FirmwareVersion);
            Assert.Equal(state.FormFactor, deviceInfo.FormFactor);

            // Additional validations
            Assert.True(deviceInfo.SerialNumber > 0);
            Assert.NotNull(deviceInfo.FirmwareVersion);
        });

    /// <summary>
    ///     Firmware filtering: Only runs on devices with firmware >= 5.3.0.
    ///     Demonstrates SCP03/SCP11 testing on modern firmware.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.3.0")]
    public async Task ModernFeatures_FirmwareAtLeast530_SupportsAdvancedProtocols(YubiKeyTestState state)
    {
        // This test only runs on devices with firmware 5.3.0 or newer
        Assert.True(state.FirmwareVersion.IsAtLeast(5, 3, 0));

        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            var info = await mgmt.GetDeviceInfoAsync();
            Assert.True(info.FirmwareVersion.Major >= 5);
        }, scpKeyParams: Scp03KeyParameters.Default);
    }

    /// <summary>
    ///     Form factor filtering: Only runs on Bio keys.
    ///     Perfect for testing biometric-specific features.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(FormFactor = FormFactor.UsbABiometricKeychain)]
    public async Task BiometricFeatures_BioKeys_HaveExpectedCapabilities(YubiKeyTestState state)
    {
        // This test only runs on USB-A Bio keys
        Assert.Equal(FormFactor.UsbABiometricKeychain, state.FormFactor);

        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            var deviceInfo = await mgmt.GetDeviceInfoAsync();

            // Bio keys should have specific capabilities
            Assert.Equal(FormFactor.UsbABiometricKeychain, deviceInfo.FormFactor);

            // Bio keys typically have modern firmware
            Assert.True(deviceInfo.FirmwareVersion.Major >= 5);
        });
    }

    /// <summary>
    ///     Transport filtering: Only runs on devices with USB transport.
    ///     Useful for USB-specific feature testing.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(RequireUsb = true)]
    public async Task UsbTransport_UsbDevices_SupportsFullCapabilities(YubiKeyTestState state)
    {
        // This test only runs on devices with USB transport
        Assert.True(state.IsUsbTransport);

        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            var deviceInfo = await mgmt.GetDeviceInfoAsync();

            // USB transport should have at least some capabilities enabled
            Assert.True(deviceInfo.UsbSupported != DeviceCapabilities.None);
            Assert.True(deviceInfo.UsbEnabled != DeviceCapabilities.None);
        });
    }

    /// <summary>
    ///     Capability filtering: Only runs on devices with PIV capability enabled.
    ///     Demonstrates capability-specific testing.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(Capability = DeviceCapabilities.Piv)]
    public async Task PivCapability_EnabledDevices_SupportsManagement(YubiKeyTestState state)
    {
        // This test only runs on devices with PIV capability enabled
        Assert.True(state.HasCapability(DeviceCapabilities.Piv));

        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            var deviceInfo = await mgmt.GetDeviceInfoAsync();

            // Verify PIV is enabled on USB or NFC
            var pivEnabled = (deviceInfo.UsbEnabled & DeviceCapabilities.Piv) != 0 ||
                             (deviceInfo.NfcEnabled & DeviceCapabilities.Piv) != 0;
            Assert.True(pivEnabled);
        });
    }

    /// <summary>
    ///     FIPS-capable filtering: Only runs on FIPS-capable devices for PIV.
    ///     Critical for FIPS compliance testing.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(FipsCapable = DeviceCapabilities.Piv)]
    public async Task FipsCapable_PivDevices_HasFipsSupport(YubiKeyTestState state)
    {
        // This test only runs on devices that are FIPS-capable for PIV
        Assert.True(state.IsFipsCapable(DeviceCapabilities.Piv));

        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            var deviceInfo = await mgmt.GetDeviceInfoAsync();

            // Verify FIPS capabilities
            Assert.True((deviceInfo.FipsCapabilities & DeviceCapabilities.Piv) != 0);
        });
    }

    /// <summary>
    ///     FIPS-approved filtering: Only runs on devices in FIPS-approved mode.
    ///     Ensures operations meet FIPS 140-2 requirements.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(FipsApproved = DeviceCapabilities.Piv)]
    public async Task FipsApproved_PivDevices_InFipsMode(YubiKeyTestState state)
    {
        // This test only runs on devices in FIPS-approved mode for PIV
        Assert.True(state.IsFipsApproved(DeviceCapabilities.Piv));

        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            var deviceInfo = await mgmt.GetDeviceInfoAsync();

            // Verify FIPS-approved mode
            Assert.True((deviceInfo.FipsApproved & DeviceCapabilities.Piv) != 0);
        });
    }

    /// <summary>
    ///     Combined filtering: Multiple requirements at once.
    ///     Only runs on modern USB keys with PIV capability.
    ///     Perfect for testing specific feature combinations.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.0.0", RequireUsb = true, Capability = DeviceCapabilities.Piv)]
    public async Task AdvancedPiv_ModernUsbKeysWithPiv_FullySupported(YubiKeyTestState state)
    {
        // This test has multiple requirements:
        // 1. Firmware >= 5.0.0
        // 2. USB transport
        // 3. PIV capability enabled

        Assert.True(state.FirmwareVersion.IsAtLeast(5, 0, 0));
        Assert.True(state.IsUsbTransport);
        Assert.True(state.HasCapability(DeviceCapabilities.Piv));

        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            var deviceInfo = await mgmt.GetDeviceInfoAsync();

            // Verify all requirements
            Assert.True(deviceInfo.FirmwareVersion.Major >= 5);
            Assert.True(deviceInfo.UsbSupported != DeviceCapabilities.None);
            Assert.True((deviceInfo.UsbEnabled & DeviceCapabilities.Piv) != 0);
        });
    }

    /// <summary>
    ///     Advanced example: Testing device-specific behavior differences.
    ///     Demonstrates how to handle different device characteristics.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey]
    public async Task DeviceCharacteristics_VariesByDevice_HandledCorrectly(YubiKeyTestState state) =>
        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            var deviceInfo = await mgmt.GetDeviceInfoAsync();

            // Test adapts based on device characteristics
            if (state.FirmwareVersion.Major >= 5)
                // Modern firmware: expect extended capabilities
                Assert.True(deviceInfo.UsbEnabled != DeviceCapabilities.None);
            else
                // Legacy firmware: basic capabilities
                Assert.NotNull(deviceInfo.FirmwareVersion);

            if (state.FormFactor == FormFactor.UsbABiometricKeychain)
                // Bio keys: verify biometric support expectations
                Assert.True(state.FirmwareVersion.Major >= 5);

            // Test that works on all devices
            Assert.True(deviceInfo.SerialNumber > 0);
        });

    /// <summary>
    ///     Form factor filtering: Runs on USB-A Keychain devices.
    ///     Tests device info consistency across multiple reads.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(FormFactor = FormFactor.UsbAKeychain)]
    public async Task DeviceInfo_UsbAKeychain_RemainsConsistent(YubiKeyTestState state)
    {
        Assert.Equal(FormFactor.UsbAKeychain, state.FormFactor);

        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            // Read device info multiple times
            var deviceInfo1 = await mgmt.GetDeviceInfoAsync();
            var deviceInfo2 = await mgmt.GetDeviceInfoAsync();

            // Verify consistency
            Assert.Equal(deviceInfo1.SerialNumber, deviceInfo2.SerialNumber);
            Assert.Equal(state.SerialNumber, deviceInfo1.SerialNumber);
            Assert.Equal(state.FormFactor, deviceInfo1.FormFactor);
        });
    }

    /// <summary>
    ///     Form factor filtering: Runs on USB-A Biometric devices.
    ///     Tests device info consistency across multiple reads.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(FormFactor = FormFactor.UsbABiometricKeychain)]
    public async Task DeviceInfo_UsbABiometric_RemainsConsistent(YubiKeyTestState state)
    {
        Assert.Equal(FormFactor.UsbABiometricKeychain, state.FormFactor);

        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            // Read device info multiple times
            var deviceInfo1 = await mgmt.GetDeviceInfoAsync();
            var deviceInfo2 = await mgmt.GetDeviceInfoAsync();

            // Verify consistency
            Assert.Equal(deviceInfo1.SerialNumber, deviceInfo2.SerialNumber);
            Assert.Equal(state.SerialNumber, deviceInfo1.SerialNumber);
            Assert.Equal(state.FormFactor, deviceInfo1.FormFactor);
        });
    }

    /// <summary>
    ///     Complex scenario: Testing across USB-C Keychain devices specifically.
    ///     Demonstrates form factor-specific testing.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(FormFactor = FormFactor.UsbCKeychain)]
    public async Task UsbCKeychain_UsbCKeychainDevices_ConsistentBehavior(YubiKeyTestState state)
    {
        // This test runs only on USB-C Keychain form factors
        Assert.Equal(FormFactor.UsbCKeychain, state.FormFactor);

        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            var deviceInfo = await mgmt.GetDeviceInfoAsync();

            // USB-C devices should have consistent management support
            Assert.NotNull(deviceInfo.FirmwareVersion);
            Assert.True(deviceInfo.SerialNumber > 0);
        });
    }

    /// <summary>
    ///     Real-world scenario: Verifying serial number consistency.
    ///     Ensures device identity remains stable across sessions.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey]
    public async Task SerialNumber_MultipleReads_RemainsConsistent(YubiKeyTestState state)
    {
        // Read serial number multiple times
        using var connection1 = await state.Device.ConnectAsync<ISmartCardConnection>();
        using var mgmt1 = await ManagementSession.CreateAsync(connection1);
        var deviceInfo1 = await mgmt1.GetDeviceInfoAsync();
        var serial1 = deviceInfo1.SerialNumber;

        using var connection2 = await state.Device.ConnectAsync<ISmartCardConnection>();
        using var mgmt2 = await ManagementSession.CreateAsync(connection2);
        var deviceInfo2 = await mgmt2.GetDeviceInfoAsync();
        var serial2 = deviceInfo2.SerialNumber;

        // Serial number should be consistent
        Assert.Equal(serial1, serial2);
        Assert.Equal(state.SerialNumber, serial1);
    }

    /// <summary>
    ///     Performance test: Measure device info retrieval time.
    ///     Useful for benchmarking across different device types.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey]
    public async Task Performance_GetDeviceInfo_CompletesQuickly(YubiKeyTestState state) =>
        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            // Measure performance
            var sw = Stopwatch.StartNew();
            var deviceInfo = await mgmt.GetDeviceInfoAsync();
            sw.Stop();

            // Device info retrieval should be fast (< 1 second)
            Assert.True(sw.ElapsedMilliseconds < 1000,
                $"GetDeviceInfo took {sw.ElapsedMilliseconds}ms on device {state}");

            // Data should still be valid
            Assert.Equal(state.SerialNumber, deviceInfo.SerialNumber);
        });

    /// <summary>
    ///     Comprehensive example: Full device validation across all properties.
    ///     Demonstrates accessing all device information through YubiKeyTestState.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey]
    public async Task ComprehensiveValidation_AllDeviceProperties_Accessible(YubiKeyTestState state)
    {
        // Demonstrate accessing device properties
        var serialNumber = state.SerialNumber;
        var firmwareVersion = state.FirmwareVersion;
        var formFactor = state.FormFactor;
        var isUsb = state.IsUsbTransport;
        var isNfc = state.IsNfcTransport;

        Assert.True(serialNumber > 0);
        Assert.NotNull(firmwareVersion);
        Assert.NotEqual(FormFactor.Unknown, formFactor);

        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            // Verify with actual device query
            var deviceInfo = await mgmt.GetDeviceInfoAsync();

            // Cross-validate cached data with fresh query
            Assert.Equal(serialNumber, deviceInfo.SerialNumber);
            Assert.Equal(firmwareVersion, deviceInfo.FirmwareVersion);
            Assert.Equal(formFactor, deviceInfo.FormFactor);
        });
    }
}