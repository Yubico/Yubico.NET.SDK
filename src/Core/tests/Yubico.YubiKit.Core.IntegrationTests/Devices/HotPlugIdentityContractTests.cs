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

using System.Diagnostics;
using Xunit.Abstractions;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

/// <summary>
///     Verifies identity retention and correlation across removal and reinsertion. Run one narrowly
///     filtered test per invocation with a human ready at the machine, never the whole user-presence
///     category. Requires a single-key USB rig.
/// </summary>
/// <remarks>
///     Operator choreography for <see cref="RemoveAndReinsert_RemovalRetainsSerial_ReinsertionPublishesNewCorrelatedObject" />:
///     start the run, wait until roughly five seconds have passed (the baseline phase needs the
///     serial to be read), UNPLUG the sole YubiKey, wait roughly five seconds, and PLUG IT BACK IN.
///     The test waits up to 120 s for the removal and up to 120 s for the reinsertion. The windows
///     are deliberately generous: xUnit buffers output until the run ends, so the operator acts on
///     elapsed time, not on a prompt. Every failure message carries the full captured timeline so a
///     failed ceremony still tells you which layer went quiet (monitor rescan, repository snapshot,
///     or event stream).
/// </remarks>
public class HotPlugIdentityContractTests(ITestOutputHelper output) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await YubiKeyManager.ShutdownAsync();

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task RemoveAndReinsert_RemovalRetainsSerial_ReinsertionPublishesNewCorrelatedObject()
    {
        var clock = Stopwatch.StartNew();
        var timeline = new List<string>();
        void Note(string message)
        {
            lock (timeline)
                timeline.Add($"[{clock.Elapsed:mm\\:ss\\.fff}] {message}");
        }

        string Timeline()
        {
            lock (timeline)
                return timeline.Count == 0
                    ? "(timeline empty - not even the initial Added event was observed)"
                    : string.Join(Environment.NewLine, timeline);
        }

        var removedTcs = new TaskCompletionSource<IYubiKey>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var subscription = YubiKeyManager.DeviceChanges.Subscribe(evt =>
        {
            Note($"EVENT {evt.Action,-7} {evt.Device.DeviceId} connections={evt.Device.AvailableConnections} " +
                 $"serialAtEvent={evt.Device.SerialNumber?.ToString() ?? "null"} ref={evt.Device.GetHashCode():x8}");
            if (evt.Action == DeviceAction.Removed)
                removedTcs.TrySetResult(evt.Device);
        });

        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));
        Note("monitoring started (1 s interval)");

        // Establish one published key with a known serial before the operator removes it.
        IYubiKey? original = null;
        while (clock.Elapsed < TimeSpan.FromSeconds(20))
        {
            var devices = await YubiKeyManager.FindAllAsync(ConnectionType.All);
            if (devices is [{ SerialNumber: not null } sole])
            {
                original = sole;
                break;
            }

            await Task.Delay(500);
        }

        Assert.True(
            original is not null,
            "Baseline failed: expected exactly one attached YubiKey with a readable serial within 20 s. " +
            $"Run this protocol on a single-key USB rig.{Environment.NewLine}{Timeline()}");
        var originalSerial = original.SerialNumber!.Value;
        Note($"BASELINE {original.DeviceId} connections={original.AvailableConnections} serial={originalSerial}");

        // The operator unplugs the key. Poll the repository snapshot alongside the event
        // stream: if the snapshot empties without a Removed event, the event plumbing lost it; if
        // neither happens, the monitor never observed the removal.
        var snapshotEmptied = false;
        var phase2Deadline = clock.Elapsed + TimeSpan.FromSeconds(120);
        while (clock.Elapsed < phase2Deadline && !removedTcs.Task.IsCompleted)
        {
            var snapshot = await YubiKeyManager.FindAllAsync(ConnectionType.All);
            if (snapshot.Count == 0 && !snapshotEmptied)
            {
                snapshotEmptied = true;
                Note("repository snapshot became empty (removal observed by scan)");
            }

            await Task.Delay(250);
        }

        Assert.True(
            removedTcs.Task.IsCompleted,
            $"No Removed event within 120 s (repository snapshot emptied: {snapshotEmptied}). The " +
            $"operator must unplug the key during this window.{Environment.NewLine}{Timeline()}");
        var removed = await removedTcs.Task;
        Note($"REMOVED delivered ref={removed.GetHashCode():x8} retainedSerial={removed.SerialNumber?.ToString() ?? "null"}");

        Assert.True(
            ReferenceEquals(original, removed),
            $"The Removed event must deliver the retained published object.{Environment.NewLine}{Timeline()}");
        Assert.Equal(originalSerial, removed.SerialNumber);

        // The operator reinserts the key; discovery must republish a new object and
        // re-establish the serial from hardware (hot-plug evicted all cached metadata).
        IYubiKey? reinserted = null;
        var phase3Deadline = clock.Elapsed + TimeSpan.FromSeconds(120);
        while (clock.Elapsed < phase3Deadline)
        {
            var devices = await YubiKeyManager.FindAllAsync(ConnectionType.All);
            if (devices is [{ SerialNumber: not null } sole])
            {
                reinserted = sole;
                break;
            }

            await Task.Delay(250);
        }

        Assert.True(
            reinserted is not null,
            "No re-added device with a re-read serial within 120 s of the removal. The operator must " +
            $"reinsert the key during this window.{Environment.NewLine}{Timeline()}");
        Note($"REINSERTED {reinserted.DeviceId} connections={reinserted.AvailableConnections} " +
             $"serial={reinserted.SerialNumber} ref={reinserted.GetHashCode():x8}");

        Assert.True(
            !ReferenceEquals(original, reinserted),
            $"Reinsertion must publish a new object.{Environment.NewLine}{Timeline()}");
        Assert.Equal(originalSerial, reinserted.SerialNumber);
        Assert.Equal(DeviceCorrelation.Same, original.SameDeviceAs(reinserted));
        Assert.Equal(DeviceCorrelation.Same, reinserted.SameDeviceAs(original));

        // Full captured timeline for the hardware-evidence record.
        output.WriteLine("Hot-plug protocol timeline:");
        output.WriteLine(Timeline());
    }
}