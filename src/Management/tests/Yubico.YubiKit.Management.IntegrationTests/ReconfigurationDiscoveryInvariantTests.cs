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

using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Management.IntegrationTests;

/// <summary>
///     Discovery identity invariants under a real USB reconfiguration reboot — the hardware companion to
///     the <c>Merge_ContestedSerial_*</c> unit vectors and guarantee G5 in
///     <c>docs/architecture/device-discovery-guarantees.md</c>.
/// </summary>
/// <remarks>
///     <para>
///         Disabling a USB application changes the key's PID and reboots it, so a scan loop running
///         across the transition observes the enumeration states discovery must never mis-handle: stale
///         interfaces dying, the key absent, fresh interfaces arriving one by one. The invariants under
///         test are the contract, not any particular intermediate shape: within every single scan, output
///         <c>DeviceId</c>s are pairwise distinct and at most one device carries this key's
///         <c>ykphysical:{serial}</c> identity. The intermediate shapes themselves (standalone fragments,
///         pid-form ids) are legitimate and deliberately not asserted — pinning them would overfit the
///         test to enumeration timing.
///     </para>
///     <para>
///         First validated as a manual 37,083-scan run across two reboot cycles on macOS (three keys,
///         zero violations) before being automated here. Honest bound, matching the unit vectors' role:
///         on rigs where the stale enumeration dies before the fresh one arrives, the contested-serial
///         winner branch itself is not entered — the transitions are sequential. This test then validates
///         the invariant the winner rule protects and the transition machinery around it; the contested
///         branch stays deterministically covered by the unit vectors.
///     </para>
///     <para>
///         Configuration-mutating and self-restoring: the original USB capability set is reapplied in
///         <c>finally</c> with its own reboot, and restoration is verified. <c>Slow</c> — two device
///         reboots (~3s each) plus re-enumeration polling.
///     </para>
/// </remarks>
public class ReconfigurationDiscoveryInvariantTests
{
    private static readonly TimeSpan TransitionObservationWindow = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan RediscoveryDeadline = TimeSpan.FromSeconds(30);

    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.3.0", ConnectionType = ConnectionType.SmartCard)]
    [Trait("Category", "Slow")]
    public async Task UsbReconfigurationReboot_DiscoveryIdentityInvariantsHoldThroughTheTransition(
        YubiKeyTestState state)
    {
        Skip.If(state.SerialNumber is null or 0, "The invariant under test is keyed by serial.");
        var serial = state.SerialNumber!.Value;

        // Snapshot the original configuration over a session we dispose BEFORE the reboot.
        DeviceCapabilities originalUsbEnabled;
        await using (var mgmt = await state.Device.CreateManagementSessionAsync())
        {
            var info = await mgmt.GetDeviceInfoAsync();
            originalUsbEnabled = info.UsbEnabled;
        }

        Skip.If((originalUsbEnabled & DeviceCapabilities.Otp) == 0,
            "OTP is not enabled on USB; disabling it is the PID-changing transition under test.");
        var withoutOtp = originalUsbEnabled & ~DeviceCapabilities.Otp;
        Skip.If(withoutOtp == DeviceCapabilities.None,
            "OTP is the only USB capability; cannot disable it.");

        try
        {
            // Trigger the transition: PID changes, key reboots, interfaces re-enumerate.
            await ApplyConfigWithRebootAsync(serial, withoutOtp);

            // Observe the whole transition. The window is generous relative to the observed cycle
            // (death + reboot + re-enumeration completed within ~9s on the validation rig) and the loop
            // asserts invariants on every scan it manages to run, whatever the enumeration timing.
            var deadline = DateTime.UtcNow + TransitionObservationWindow;
            var scans = 0;
            while (DateTime.UtcNow < deadline)
            {
                var devices = await YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true);
                scans++;
                AssertIdentityInvariants(devices, serial);
            }

            Assert.True(scans > 0, "The observation loop never completed a scan.");
        }
        finally
        {
            // Restore-what-you-changed, with its own reboot, and verify.
            await ApplyConfigWithRebootAsync(serial, originalUsbEnabled);
            var restored = await WaitForDeviceInfoBySerialAsync(serial);
            Assert.Equal(originalUsbEnabled, restored.UsbEnabled);
        }

        // Post-restore steady state: the key is whole again and the invariants still hold.
        var settled = await YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true);
        AssertIdentityInvariants(settled, serial);
    }

    /// <summary>
    ///     The two identity invariants every scan must uphold, whatever intermediate enumeration state it
    ///     catches: pairwise-distinct <c>DeviceId</c>s, and at most one holder of this key's serial-form
    ///     identity.
    /// </summary>
    private static void AssertIdentityInvariants(IReadOnlyList<IYubiKey> devices, int serial)
    {
        var ids = devices.Select(d => d.DeviceId).ToList();
        var duplicates = ids
            .GroupBy(id => id, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        Assert.True(
            duplicates.Count == 0,
            $"Duplicate DeviceIds in one scan: [{string.Join(", ", duplicates)}] — " +
            $"scan: {string.Join("; ", ids)}");

        var serialFormHolders = ids.Count(id => id == $"ykphysical:{serial}");
        Assert.True(
            serialFormHolders <= 1,
            $"{serialFormHolders} devices carry ykphysical:{serial} in one scan — " +
            $"scan: {string.Join("; ", ids)}");
    }

    /// <summary>
    ///     Applies a USB capability set with <c>reboot: true</c>, locating the key by serial through a
    ///     fresh scan. Retries until <see cref="RediscoveryDeadline" />: mid-transition the key may be
    ///     absent or its interfaces may refuse connections, and both are expected states, not failures.
    /// </summary>
    private static async Task ApplyConfigWithRebootAsync(int serial, DeviceCapabilities usbEnabled)
    {
        var config = DeviceConfig.CreateBuilder()
            .WithCapabilities(Transport.Usb, (int)usbEnabled)
            .Build();

        var deadline = DateTime.UtcNow + RediscoveryDeadline;
        Exception? lastFailure = null;
        while (DateTime.UtcNow < deadline)
        {
            var found = await TryFindBySerialAsync(serial);
            if (found is { } device)
            {
                try
                {
                    await using var mgmt = await device.CreateManagementSessionAsync();
                    await mgmt.SetDeviceConfigAsync(config, new SetDeviceConfigOptions { Reboot = true });
                    return;
                }
                catch (Exception ex)
                {
                    // The key can disappear between the scan and the connect, or mid-exchange, while it
                    // is still settling from a previous reboot. Bounded by the deadline.
                    lastFailure = ex;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException(
            $"Could not apply the configuration to serial {serial} within {RediscoveryDeadline}.",
            lastFailure);
    }

    private static async Task<DeviceInfo> WaitForDeviceInfoBySerialAsync(int serial)
    {
        var deadline = DateTime.UtcNow + RediscoveryDeadline;
        Exception? lastFailure = null;
        while (DateTime.UtcNow < deadline)
        {
            var found = await TryFindBySerialAsync(serial);
            if (found is { } device)
            {
                try
                {
                    await using var mgmt = await device.CreateManagementSessionAsync();
                    return await mgmt.GetDeviceInfoAsync();
                }
                catch (Exception ex)
                {
                    lastFailure = ex;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException(
            $"Serial {serial} did not answer a Management read within {RediscoveryDeadline}.",
            lastFailure);
    }

    /// <summary>
    ///     Finds the physical key carrying <paramref name="serial" /> via a fresh scan, matching by the
    ///     serial-form id when discovery minted one and by a Management read otherwise (single-key rigs
    ///     merge by PID and never read serials — cost discipline, not a gap).
    /// </summary>
    private static async Task<IYubiKey?> TryFindBySerialAsync(int serial)
    {
        IReadOnlyList<IYubiKey> devices;
        try
        {
            devices = await YubiKeyManager.FindAllAsync(ConnectionType.All, forceRescan: true);
        }
        catch
        {
            return null; // Scan faults mid-transition are retried by the caller's deadline.
        }

        var bySerialId = devices.FirstOrDefault(d => d.DeviceId == $"ykphysical:{serial}");
        if (bySerialId is not null) return bySerialId;

        foreach (var candidate in devices.Where(d =>
                     (d.AvailableConnections & ConnectionType.SmartCard) != 0))
        {
            try
            {
                await using var mgmt = await candidate.CreateManagementSessionAsync();
                var info = await mgmt.GetDeviceInfoAsync();
                if (info.SerialNumber == serial) return candidate;
            }
            catch
            {
                // A dying or mid-reboot candidate; the next one, or the caller's retry, resolves it.
            }
        }

        return null;
    }
}