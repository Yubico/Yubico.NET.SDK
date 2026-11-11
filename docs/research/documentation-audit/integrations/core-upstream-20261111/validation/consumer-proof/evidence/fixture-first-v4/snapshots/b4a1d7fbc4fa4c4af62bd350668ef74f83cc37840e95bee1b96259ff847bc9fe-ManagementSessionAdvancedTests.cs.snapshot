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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Management.IntegrationTests;

/// <summary>
///     Integration tests using [SkippableTheory] and [WithYubiKey] for declarative device filtering.
/// </summary>
/// <remarks>
///     <para>
///         Each [WithYubiKey] attribute supplies one row, bound to the first matching authorized device.
///         Multiple attributes bind independently; they need not select the same physical key.
///         WithManagementAsync passes cached test-state metadata to the callback, not a fresh helper-entry read.
///     </para>
///     <para>
///         ConnectionType filters require an exposed interface but do not pin the operation's transport.
///         Form-factor and FIPS filters rely on Core's decoded metadata; these comparisons do not validate
///         that decoding. Flagged form-factor bytes and FIPS application-bit mapping require separate
///         decoder coverage. FIPS flag checks are not certification or approved-operation evidence.
///     </para>
/// </remarks>
public class ManagementSessionAdvancedTests
{
    /// <summary>
    ///     Compares a fresh query with cached discovery metadata for one authorized device row.
    /// </summary>
    [SkippableTheory]
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
    ///     Creates an SCP03 session using default keys; the selected device must have those keys configured.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.3.0", ConnectionType = ConnectionType.SmartCard)]
    public async Task ModernFeatures_FirmwareAtLeast530_SupportsAdvancedProtocols(YubiKeyTestState state)
    {
        // This test only runs on devices with firmware 5.3.0 or newer
        Assert.True(state.FirmwareVersion.IsAtLeast(5, 3, 0));

        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            var info = await mgmt.GetDeviceInfoAsync();
            Assert.True(info.FirmwareVersion.IsAtLeast(5, 0, 0));
        }, scpKeyParams: Scp03KeyParameters.Default);
    }

    /// <summary>
    ///     Form factor filtering: Only runs on Bio keys.
    ///     Checks reported form factor and firmware, not a biometric operation.
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

            // Compare the reported form factor with the row's filter.
            Assert.Equal(FormFactor.UsbABiometricKeychain, deviceInfo.FormFactor);

            // Bio keys typically have modern firmware
            Assert.True(deviceInfo.FirmwareVersion.IsAtLeast(5, 0, 0));
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
    ///     Filters on the decoded PIV FIPS-capability flag.
    ///     Compares decoded PIV capability flags, not FIPS compliance or decoder correctness.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(FipsCapable = DeviceCapabilities.Piv)]
    public async Task FipsCapable_PivDevices_HasFipsSupport(YubiKeyTestState state)
    {
        // The row is selected using the cached decoded PIV FIPS-capability flag.
        Assert.True(state.IsFipsCapable(DeviceCapabilities.Piv));

        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            var deviceInfo = await mgmt.GetDeviceInfoAsync();

            // Verify FIPS capabilities
            Assert.True((deviceInfo.FipsCapabilities & DeviceCapabilities.Piv) != 0);
        });
    }

    /// <summary>
    ///     Selects a row using the decoded PIV FIPS-approval flag and checks a fresh report of that flag.
    ///     Does not exercise a PIV operation or establish compliance with a FIPS certification target.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(FipsApproved = DeviceCapabilities.Piv)]
    public async Task FipsApproved_PivDevices_InFipsMode(YubiKeyTestState state)
    {
        // Selection and this assertion use the same cached decoded approval flag.
        Assert.True(state.IsFipsApproved(DeviceCapabilities.Piv));

        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            var deviceInfo = await mgmt.GetDeviceInfoAsync();

            // Compare the freshly decoded PIV approval flag; no approved operation is exercised.
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
            Assert.True(deviceInfo.FirmwareVersion.IsAtLeast(5, 0, 0));
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
            if (state.FirmwareVersion.IsAtLeast(5, 0, 0))
                // On modern firmware, check that the reported USB enabled mask is nonempty.
                Assert.True(deviceInfo.UsbEnabled != DeviceCapabilities.None);
            else
                // On older firmware, check that a firmware value is present.
                Assert.NotNull(deviceInfo.FirmwareVersion);

            if (state.FormFactor == FormFactor.UsbABiometricKeychain)
                // For the reported Bio form factor, check the firmware minimum, not biometric functionality.
                Assert.True(state.FirmwareVersion.IsAtLeast(5, 0, 0));

            // This selected device is expected to report a positive serial number.
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
    ///     Uses default Management transport selection for two sequential sessions.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey]
    public async Task SerialNumber_MultipleReads_RemainsConsistent(YubiKeyTestState state)
    {
        int? serial1 = null;
        int? serial2 = null;

        // Read serial number first time
        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            var deviceInfo = await mgmt.GetDeviceInfoAsync();
            serial1 = deviceInfo.SerialNumber;
        });

        // Read serial number second time
        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            var deviceInfo = await mgmt.GetDeviceInfoAsync();
            serial2 = deviceInfo.SerialNumber;
        });

        // Serial number should be consistent across reads and match cached state
        Assert.Equal(serial1, serial2);
        Assert.Equal(state.SerialNumber, serial1);
    }

    /// <summary>
    ///     Measures one device-information read against a one-second threshold on the selected rig.
    ///     This test threshold is not a general SDK latency guarantee.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task Performance_GetDeviceInfo_CompletesQuickly(YubiKeyTestState state) =>
        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            // Measure performance
            var sw = Stopwatch.StartNew();
            var deviceInfo = await mgmt.GetDeviceInfoAsync();
            sw.Stop();

            // Enforce this test's one-second budget; the helper uses default transport selection.
            Assert.True(sw.ElapsedMilliseconds < 1000,
                $"GetDeviceInfo took {sw.ElapsedMilliseconds}ms on device {state}");

            // Data should still be valid
            Assert.Equal(state.SerialNumber, deviceInfo.SerialNumber);
        });

    /// <summary>
    ///     Checks cached serial, firmware, and form factor, then compares them with a fresh query.
    ///     USB/NFC transport properties are read but not asserted.
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

    /// <summary>
    ///     Reads a device that exposes SmartCard using default Management selection.
    ///     The row filter is asserted, but the session's actual connection type is not.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task GetDeviceInfo_CcidOnly_UsesSmartCardConnection(YubiKeyTestState state)
    {
        // This is the requested row filter, not an assertion of the session's connection type.
        Assert.Equal(ConnectionType.SmartCard, state.ConnectionType);

        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            var deviceInfo = await mgmt.GetDeviceInfoAsync();

            // Compare serial and firmware using the helper's unpinned default session.
            Assert.Equal(state.SerialNumber, deviceInfo.SerialNumber);
            Assert.Equal(state.FirmwareVersion, deviceInfo.FirmwareVersion);
        });
    }

    /// <summary>
    ///     Supplies three independent rows and pins each session to its row's concrete connection type.
    ///     Compares fresh metadata with that row's cached device, not with another row's physical key.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [WithYubiKey(ConnectionType = ConnectionType.HidOtp)]
    public async Task GetDeviceInfo_AllTransports_ReturnsConsistentData(YubiKeyTestState state)
    {
        // Each attribute binds independently to a matching authorized device.

        await state.WithManagementAsync(async (mgmt, cachedDeviceInfo) =>
        {
            // The attribute filters devices; preferredConnection below pins the operation.
            // Assert the actual connection type so default selection cannot hide a missing pin.
            Assert.Equal(state.ConnectionType, mgmt.ConnectionType);

            var deviceInfo = await mgmt.GetDeviceInfoAsync();

            // Compare this transport's query with the selected row's cached identity metadata.
            Assert.Equal(state.SerialNumber, deviceInfo.SerialNumber);
            Assert.Equal(state.FirmwareVersion, deviceInfo.FirmwareVersion);
            Assert.Equal(state.FormFactor, deviceInfo.FormFactor);
        },
        preferredConnection: state.ConnectionType);
    }
}