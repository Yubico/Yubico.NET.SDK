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

using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Management.IntegrationTests;

/// <summary>
///     Integration tests for toggling USB/NFC capabilities and NFC restricted mode via
///     <see cref="ManagementSession.SetDeviceConfigAsync" />.
///     These tests are destructive: they modify device configuration, but always restore original state.
/// </summary>
public class ManagementSessionCapabilityTests
{
    /// <summary>
    ///     Toggles an individual USB capability (OATH) off and on, verifying the change takes effect.
    ///     The device is always restored to its original configuration.
    /// </summary>
    /// <remarks>
    ///     This test disables OATH on USB, reads back the config, then restores. It requires a device
    ///     that has OATH currently enabled on USB. The test does NOT reboot the device because
    ///     rebooting would disrupt the test session; instead it passes <c>reboot: false</c> and
    ///     validates the persisted config via a fresh <see cref="ManagementSession.GetDeviceInfoAsync" /> call.
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
                var configWithoutOath = DeviceConfig.CreateBuilder()
                    .WithCapabilities(Transport.Usb, (int)withoutOath)
                    .Build();

                await mgmt.SetDeviceConfigAsync(configWithoutOath, false);

                // Read back and verify OATH is now disabled
                var updatedInfo = await mgmt.GetDeviceInfoAsync();
                Assert.Equal(DeviceCapabilities.None, updatedInfo.UsbEnabled & DeviceCapabilities.Oath);
            }
            finally
            {
                // Restore original capabilities
                var restoreConfig = DeviceConfig.CreateBuilder()
                    .WithCapabilities(Transport.Usb, (int)originalUsbEnabled)
                    .Build();

                await mgmt.SetDeviceConfigAsync(restoreConfig, false);

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
    ///     NFC restricted mode (tag 0x17) is available on firmware 5.7.0+. When enabled, the device
    ///     will not respond over NFC until physically touched via USB first.
    ///     This test only verifies setting NFC restricted to <c>true</c> because disabling it
    ///     (setting to <c>false</c>) requires a device reboot to take effect, which would disrupt
    ///     the test session. This matches the Java SDK test behavior.
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
                var config = DeviceConfig.CreateBuilder()
                    .WithNfcRestricted(true)
                    .Build();

                await mgmt.SetDeviceConfigAsync(config, false);

                // Read back and verify the flag is set
                var updatedInfo = await mgmt.GetDeviceInfoAsync();
                Assert.True(updatedInfo.IsNfcRestricted,
                    "NFC restricted mode should be enabled after setting it to true.");
            }
            finally
            {
                // Best-effort restore: set back to original value.
                // Note: if original was false, this may not take effect without a reboot,
                // but we send it anyway so the config is queued for next reboot.
                var restoreConfig = DeviceConfig.CreateBuilder()
                    .WithNfcRestricted(originalNfcRestricted)
                    .Build();

                await mgmt.SetDeviceConfigAsync(restoreConfig, false);
            }
        });
    }
}
