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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Management.IntegrationTests;

/// <summary>
///     Integration tests for Management lock code operations.
///     These tests set a lock code, exercise configuration writes, and attempt to clear the code in finally.
///     The helper passes cached starting metadata and uses default transport selection.
///     Cleanup can fail: lock-code zeroing currently follows fallible device restoration and can be skipped.
/// </summary>
public class ManagementLockCodeTests
{
    /// <summary>
    ///     Sets a lock, reads the locked flag, expects a missing-code write to fail, and writes with the code.
    ///     Attempts to clear the lock and reads the unlocked flag in finally; device failures can prevent cleanup.
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
                var configToLock = new DeviceConfig { UsbEnabled = deviceInfo.UsbEnabled };

                await mgmt.SetDeviceConfigAsync(configToLock, new SetDeviceConfigOptions { NewLockCode = lockCode });

                // Verify the device is now locked
                var lockedInfo = await mgmt.GetDeviceInfoAsync();
                Assert.True(lockedInfo.IsLocked);

                // Expect rejection without the code. Any exception satisfies this assertion,
                // so unrelated failures can pass it; the device error/status is not pinned.
                var configWithoutCode = new DeviceConfig { UsbEnabled = deviceInfo.UsbEnabled };

                await Assert.ThrowsAnyAsync<Exception>(async () =>
                    await mgmt.SetDeviceConfigAsync(configWithoutCode));

                // Verify that a config change WITH the lock code succeeds
                var configWithCode = new DeviceConfig { UsbEnabled = deviceInfo.UsbEnabled };

                await mgmt.SetDeviceConfigAsync(configWithCode, new SetDeviceConfigOptions { CurrentLockCode = lockCode });
            }
            finally
            {
                // Clear the lock code by setting it to all zeros
                var unlockConfig = new DeviceConfig { UsbEnabled = deviceInfo.UsbEnabled };

                await mgmt.SetDeviceConfigAsync(
                    unlockConfig,
                    new SetDeviceConfigOptions { CurrentLockCode = lockCode, NewLockCode = clearCode });

                // Verify the device is no longer locked
                var unlockedInfo = await mgmt.GetDeviceInfoAsync();
                Assert.False(unlockedInfo.IsLocked);

                // A preceding cleanup failure skips this zeroing; it needs an independent finally.
                CryptographicOperations.ZeroMemory(lockCode);
            }
        });
    }

    /// <summary>
    ///     Verifies that the auto-eject timeout can be changed while
    ///     a lock code is active, provided the lock code is supplied.
    ///     Teardown sends the cached timeout and requests lock removal without readback; it does not verify restoration.
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
                var lockConfig = new DeviceConfig { UsbEnabled = deviceInfo.UsbEnabled };

                await mgmt.SetDeviceConfigAsync(lockConfig, new SetDeviceConfigOptions { NewLockCode = lockCode });

                // Change auto-eject timeout with lock code
                var changeConfig = new DeviceConfig { UsbEnabled = deviceInfo.UsbEnabled, AutoEjectTimeout = newAutoEject };

                await mgmt.SetDeviceConfigAsync(changeConfig, new SetDeviceConfigOptions { CurrentLockCode = lockCode });

                // Verify the change took effect
                var updatedInfo = await mgmt.GetDeviceInfoAsync();
                Assert.Equal(newAutoEject, updatedInfo.AutoEjectTimeout);
            }
            finally
            {
                // Attempt to restore the cached timeout and clear the code; no restoration readback follows.
                var restoreConfig = new DeviceConfig { UsbEnabled = deviceInfo.UsbEnabled, AutoEjectTimeout = originalAutoEject };

                await mgmt.SetDeviceConfigAsync(
                    restoreConfig,
                    new SetDeviceConfigOptions { CurrentLockCode = lockCode, NewLockCode = clearCode });

                // A preceding cleanup failure skips this zeroing; it needs an independent finally.
                CryptographicOperations.ZeroMemory(lockCode);
            }
        });
    }
}