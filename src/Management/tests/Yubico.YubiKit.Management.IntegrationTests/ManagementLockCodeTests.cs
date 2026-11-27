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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
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
        await state.WithManagementAsync(async (mgmt, _) =>
        {
            var deviceInfo = await mgmt.GetDeviceInfoAsync();
            // Skip if device is already locked (we cannot set a new lock code without the current one)
            Skip.If(deviceInfo.IsLocked,
                "Device already has a lock code set; cannot test lock code lifecycle.");

            byte[] lockCode = CreateFixtureCode();
            bool confirmed = false;
            Exception? primary = null;
            try
            {
                // Set a lock code on the device
                var configToLock = new DeviceConfig { UsbEnabled = deviceInfo.UsbEnabled };

                await mgmt.SetDeviceConfigAsync(configToLock, new SetDeviceConfigOptions { NewLockCode = lockCode });
                confirmed = true;

                // Verify the device is now locked
                var lockedInfo = await mgmt.GetDeviceInfoAsync();
                Assert.True(lockedInfo.IsLocked);

                // Require a SmartCard command rejection, not a validation or transport failure.
                // The exact device status remains a hardware qualification question.
                var configWithoutCode = new DeviceConfig { UsbEnabled = deviceInfo.UsbEnabled };

                await Assert.ThrowsAsync<ApduException>(async () =>
                    await mgmt.SetDeviceConfigAsync(configWithoutCode));

                // Verify that a config change WITH the lock code succeeds
                var configWithCode = new DeviceConfig { UsbEnabled = deviceInfo.UsbEnabled };

                await mgmt.SetDeviceConfigAsync(configWithCode, new SetDeviceConfigOptions { CurrentLockCode = lockCode });
            }
            catch (Exception exception)
            {
                primary = exception;
                throw;
            }
            finally
            {
                await RestoreAsync(mgmt, lockCode, confirmed, null, primary);
            }
        }, preferredConnection: ConnectionType.SmartCard);
    }

    /// <summary>
    ///     Verifies that the auto-eject timeout can be changed while
    ///     a lock code is active, provided the lock code is supplied.
    ///     Restores the original live timeout and removes the test code; checks both on readback.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.0.0", ConnectionType = ConnectionType.SmartCard)]
    public async Task LockCode_ConfigChangeWithLockCode_Succeeds(YubiKeyTestState state)
    {
        await state.WithManagementAsync(async (mgmt, _) =>
        {
            var deviceInfo = await mgmt.GetDeviceInfoAsync();
            Skip.If(deviceInfo.IsLocked,
                "Device already has a lock code set; cannot test lock code lifecycle.");

            var originalAutoEject = deviceInfo.AutoEjectTimeout;
            Assert.InRange(originalAutoEject, (ushort)0, (ushort)3600);
            ushort newAutoEject = originalAutoEject == 10 ? (ushort)20 : (ushort)10;
            byte[] lockCode = CreateFixtureCode();
            bool confirmed = false;
            Exception? primary = null;

            try
            {
                // Set a lock code
                var lockConfig = new DeviceConfig { UsbEnabled = deviceInfo.UsbEnabled };

                await mgmt.SetDeviceConfigAsync(lockConfig, new SetDeviceConfigOptions { NewLockCode = lockCode });
                confirmed = true;

                // Change auto-eject timeout with lock code
                var changeConfig = new DeviceConfig { UsbEnabled = deviceInfo.UsbEnabled, AutoEjectTimeout = newAutoEject };

                await mgmt.SetDeviceConfigAsync(changeConfig, new SetDeviceConfigOptions { CurrentLockCode = lockCode });

                // Verify the change took effect
                var updatedInfo = await mgmt.GetDeviceInfoAsync();
                Assert.Equal(newAutoEject, updatedInfo.AutoEjectTimeout);
            }
            catch (Exception exception)
            {
                primary = exception;
                throw;
            }
            finally
            {
                await RestoreAsync(mgmt, lockCode, confirmed, originalAutoEject, primary);
            }
        }, preferredConnection: ConnectionType.SmartCard);
    }

    // Dedicated, allow-listed keys only. Keep the fixture credential reproducible for a human recovery
    // if a write has an uncertain outcome; do not generate or log a code that cannot be recovered later.
    private static byte[] CreateFixtureCode() => "YK-MGMT-LOCKTEST"u8.ToArray();

    private static async Task RestoreAsync(
        IManagementSession session, byte[] code, bool confirmed, ushort? originalTimeout, Exception? primary)
    {
        try
        {
            if (!confirmed)
                return;

            await session.SetDeviceConfigAsync(
                new DeviceConfig { AutoEjectTimeout = originalTimeout },
                new SetDeviceConfigOptions { CurrentLockCode = code, NewLockCode = new byte[16] });
            var restored = await session.GetDeviceInfoAsync();
            Assert.False(restored.IsLocked);
            if (originalTimeout is { } timeout)
                Assert.Equal(timeout, restored.AutoEjectTimeout);
        }
        catch (Exception cleanup) when (primary is not null)
        {
            throw new AggregateException("Management fixture operation and restoration both failed.", primary, cleanup);
        }
        finally
        {
            // The test fixture code remains reconstructible for recovery; this operation's buffer does not.
            CryptographicOperations.ZeroMemory(code);
        }
    }
}