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
///     Integration tests for toggling USB/NFC capabilities and NFC restricted mode via
///     <see cref="ManagementSession.SetDeviceConfigAsync" />.
///     These tests modify configuration and attempt restoration in finally. USB restoration is read back;
///     NFC-restriction restoration is not verified. Helpers receive cached starting metadata and use default transport selection.
/// </summary>
public class ManagementSessionCapabilityTests
{
    /// <summary>
    ///     Clears the reported USB OATH bit and checks readback, then attempts restoration and readback in finally.
    /// </summary>
    /// <remarks>
    ///     This test disables OATH on USB, reads back the config, then restores. It requires a device
    ///     that has OATH currently enabled on USB. The test does NOT reboot the device because
    ///     rebooting would disrupt the test session; instead it leaves <c>SetDeviceConfigOptions.Reboot</c> false and
    ///     validates the persisted config via a fresh <see cref="ManagementSession.GetDeviceInfoAsync" /> call.
    ///     This does not verify active OATH transport behavior after re-enumeration; restoration can fail on device errors.
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.0.0", ConnectionType = ConnectionType.SmartCard)]
    public async Task SetDeviceConfigAsync_ToggleUsbCapability_ChangesAndRestores(YubiKeyTestState state)
    {
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            // Save original state
            var originalUsbEnabled = deviceInfo.UsbEnabled;

            // Skip if OATH is not currently enabled (nothing to toggle)
            Skip.If((originalUsbEnabled & DeviceCapabilities.Oath) == 0,
                "OATH is not enabled on USB; cannot toggle it.");

            // Also skip if OATH is the only capability (cannot disable all USB capabilities)
            var withoutOath = originalUsbEnabled & ~DeviceCapabilities.Oath;
            Skip.If(withoutOath == DeviceCapabilities.None,
                "OATH is the only USB capability; cannot disable it.");

            try
            {
                // Disable OATH on USB
                var configWithoutOath = new DeviceConfig { UsbEnabled = withoutOath };

                await mgmt.SetDeviceConfigAsync(configWithoutOath);

                // Read back and verify OATH is now disabled
                var updatedInfo = await mgmt.GetDeviceInfoAsync();
                Assert.Equal(DeviceCapabilities.None, updatedInfo.UsbEnabled & DeviceCapabilities.Oath);
            }
            finally
            {
                // Restore original capabilities
                var restoreConfig = new DeviceConfig { UsbEnabled = originalUsbEnabled };

                await mgmt.SetDeviceConfigAsync(restoreConfig);

                // Verify restoration
                var restoredInfo = await mgmt.GetDeviceInfoAsync();
                Assert.Equal(originalUsbEnabled, restoredInfo.UsbEnabled);
            }
        });
    }

    /// <summary>
    ///     Verifies that NFC restricted mode can be enabled and read back via
    ///     <see cref="ManagementSession.SetDeviceConfigAsync" />.
    /// </summary>
    /// <remarks>
    ///     Restricted NFC is available on NFC-capable devices with firmware 5.7.0 or later. It restricts
    ///     NFC operations except reading the configured NDEF URL. USB power for three seconds clears the
    ///     restriction; neither touch nor USB data communication is required.
    ///     This test checks true write/readback only, not NFC communication or USB-power activation.
    ///     It sends the cached starting value in finally without readback. The encoder sends an explicit
    ///     false byte when requested, but this test does not establish that it restores NFC access.
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.7.0", ConnectionType = ConnectionType.SmartCard)]
    public async Task SetDeviceConfigAsync_SetNfcRestricted_ReadsBackCorrectly(YubiKeyTestState state)
    {
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            // Skip if device does not support NFC at all
            Skip.If(deviceInfo.NfcSupported == DeviceCapabilities.None,
                "Device does not support NFC; cannot test NFC restricted mode.");

            var originalNfcRestricted = deviceInfo.IsNfcRestricted;

            try
            {
                // Enable NFC restricted mode
                var config = new DeviceConfig { NfcRestricted = true };

                await mgmt.SetDeviceConfigAsync(config);

                // Read back and verify the flag is set
                var updatedInfo = await mgmt.GetDeviceInfoAsync();
                Assert.True(updatedInfo.IsNfcRestricted,
                    "NFC restricted mode should be enabled after setting it to true.");
            }
            finally
            {
                // Attempt to restore the cached value; no readback or NFC-access check follows.
                // Direct false-write behavior is not established here; USB power is the documented activation route.
                var restoreConfig = new DeviceConfig { NfcRestricted = originalNfcRestricted };

                await mgmt.SetDeviceConfigAsync(restoreConfig);
            }
        });
    }
}