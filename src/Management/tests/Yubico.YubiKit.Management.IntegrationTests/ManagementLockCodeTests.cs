// Copyright 2026 Yubico AB
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

using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Management.IntegrationTests;

/// <summary>
///     Integration tests for Management lock code operations.
///     These tests set a lock code, verify it is required for configuration changes,
///     and then clear the lock code to restore the device to its original state.
/// </summary>
public class ManagementLockCodeTests
{
    /// <summary>
    ///     Sets a configuration lock code on the device, verifies that subsequent
    ///     configuration changes require the lock code, then clears the lock code.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.0.0", ConnectionType = ConnectionType.SmartCard)]
    public async Task LockCode_SetAndClear_ProtectsConfigurationChanges(YubiKeyTestState state)
    {
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            // Skip if device is already locked (we cannot set a new lock code without the current one)
            Skip.If(deviceInfo.IsLocked,
                "Device already has a lock code set; cannot test lock code lifecycle.");

            // Generate a 16-byte lock code
            byte[] lockCode = new byte[16];
            RandomNumberGenerator.Fill(lockCode);

            // All-zero code is used to clear the lock
            byte[] clearCode = new byte[16];

            try
            {
                // Set a lock code on the device
                var configToLock = DeviceConfig.CreateBuilder()
                    .WithCapabilities(Transport.Usb, (int)deviceInfo.UsbEnabled)
                    .Build();

                await mgmt.SetDeviceConfigAsync(configToLock, false, newLockCode: lockCode);

                // Verify the device is now locked
                var lockedInfo = await mgmt.GetDeviceInfoAsync();
                Assert.True(lockedInfo.IsLocked);

                // Verify that a config change WITHOUT the lock code fails
                var configWithoutCode = DeviceConfig.CreateBuilder()
                    .WithCapabilities(Transport.Usb, (int)deviceInfo.UsbEnabled)
                    .Build();

                await Assert.ThrowsAnyAsync<Exception>(async () =>
                    await mgmt.SetDeviceConfigAsync(configWithoutCode, false));

                // Verify that a config change WITH the lock code succeeds
                var configWithCode = DeviceConfig.CreateBuilder()
                    .WithCapabilities(Transport.Usb, (int)deviceInfo.UsbEnabled)
                    .Build();

                await mgmt.SetDeviceConfigAsync(configWithCode, false, currentLockCode: lockCode);
            }
            finally
            {
                // Clear the lock code by setting it to all zeros
                var unlockConfig = DeviceConfig.CreateBuilder()
                    .WithCapabilities(Transport.Usb, (int)deviceInfo.UsbEnabled)
                    .Build();

                await mgmt.SetDeviceConfigAsync(
                    unlockConfig, false,
                    currentLockCode: lockCode,
                    newLockCode: clearCode);

                // Verify the device is no longer locked
                var unlockedInfo = await mgmt.GetDeviceInfoAsync();
                Assert.False(unlockedInfo.IsLocked);

                CryptographicOperations.ZeroMemory(lockCode);
            }
        });
    }

    /// <summary>
    ///     Verifies that the auto-eject timeout can be changed while
    ///     a lock code is active, provided the lock code is supplied.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.0.0", ConnectionType = ConnectionType.SmartCard)]
    public async Task LockCode_ConfigChangeWithLockCode_Succeeds(YubiKeyTestState state)
    {
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            Skip.If(deviceInfo.IsLocked,
                "Device already has a lock code set; cannot test lock code lifecycle.");

            byte[] lockCode = new byte[16];
            RandomNumberGenerator.Fill(lockCode);
            byte[] clearCode = new byte[16];

            var originalAutoEject = deviceInfo.AutoEjectTimeout;
            ushort newAutoEject = originalAutoEject == 10 ? (ushort)20 : (ushort)10;

            try
            {
                // Set a lock code
                var lockConfig = DeviceConfig.CreateBuilder()
                    .WithCapabilities(Transport.Usb, (int)deviceInfo.UsbEnabled)
                    .Build();

                await mgmt.SetDeviceConfigAsync(lockConfig, false, newLockCode: lockCode);

                // Change auto-eject timeout with lock code
                var changeConfig = DeviceConfig.CreateBuilder()
                    .WithCapabilities(Transport.Usb, (int)deviceInfo.UsbEnabled)
                    .WithAutoEjectTimeout(newAutoEject)
                    .Build();

                await mgmt.SetDeviceConfigAsync(changeConfig, false, currentLockCode: lockCode);

                // Verify the change took effect
                var updatedInfo = await mgmt.GetDeviceInfoAsync();
                Assert.Equal(newAutoEject, updatedInfo.AutoEjectTimeout);
            }
            finally
            {
                // Restore original timeout and clear lock code
                var restoreConfig = DeviceConfig.CreateBuilder()
                    .WithCapabilities(Transport.Usb, (int)deviceInfo.UsbEnabled)
                    .WithAutoEjectTimeout(originalAutoEject)
                    .Build();

                await mgmt.SetDeviceConfigAsync(
                    restoreConfig, false,
                    currentLockCode: lockCode,
                    newLockCode: clearCode);

                CryptographicOperations.ZeroMemory(lockCode);
            }
        });
    }
}
