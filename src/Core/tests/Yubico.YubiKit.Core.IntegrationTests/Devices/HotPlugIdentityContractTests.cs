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

using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit.Abstractions;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;

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
///     elapsed time, not on a prompt. Assertion messages include the captured timeline to distinguish
///     observed snapshot membership from stream delivery; the timeline alone does not establish a native cause.
/// </remarks>
public class HotPlugIdentityContractTests(ITestOutputHelper output) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Discovery)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task RemoveAndReinsert_RemovalRetainsSerial_ReinsertionPublishesNewCorrelatedObject()
    {
        var authorized = AuthorizedDevices.GetAll();
        if (authorized is not [{ SerialNumber: { } authorizedSerial, IsUsbTransport: true }])
        {
            throw new Xunit.SkipException(
                "This hot-plug protocol requires exactly one authorized USB device with a readable serial number.");
        }

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
        var arrivals = new ConcurrentDictionary<IYubiKey, TaskCompletionSource>(ReferenceEqualityComparer.Instance);
        TaskCompletionSource Arrival(IYubiKey key) => arrivals.GetOrAdd(key,
            static _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));

        using var watching = new CancellationTokenSource();

        await using var monitor = await YubiKeyManager.CreateMonitorAsync(watching.Token);
        // Initial replay and subsequent changes share the monitor's publication boundary.
        var enumerator = monitor.WatchAsync(watching.Token).GetAsyncEnumerator(watching.Token);
        var firstMove = enumerator.MoveNextAsync();
        var pump = Task.Run(
            async () =>
            {
                try
                {
                    var move = firstMove;
                    while (await move)
                    {
                        var evt = enumerator.Current;
                        Note($"CHANGE {evt}");
                        if (evt is DeviceRemoved removed)
                            removedTcs.TrySetResult(removed.Device);
                        else if (evt is DeviceAdded added)
                            Arrival(added.Device).TrySetResult();

                        move = enumerator.MoveNextAsync();
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync();
                }
            },
            CancellationToken.None);

        Note("owned monitor initialized (5 s fallback interval)");

        // Establish one published key with a known serial before the operator removes it.
        IYubiKey? original = null;
        while (clock.Elapsed < TimeSpan.FromSeconds(20))
        {
            var devices = await MonitorSnapshot.ReadAsync(monitor, watching.Token);
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
        Assert.Equal(authorizedSerial, originalSerial);
        // Initial replay or a legitimate replacement during convergence must deliver this exact object.
        await Arrival(original).Task.WaitAsync(TimeSpan.FromSeconds(10));
        Note($"BASELINE {original.DeviceId} connections={original.AvailableConnections} serial={originalSerial}");

        // The operator unplugs the key. Read a subscription snapshot alongside the event
        // stream: if the snapshot empties without a Removed event, the event plumbing lost it; if
        // neither happens, the monitor never observed the removal.
        var snapshotEmptied = false;
        var phase2Deadline = clock.Elapsed + TimeSpan.FromSeconds(120);
        while (clock.Elapsed < phase2Deadline && !removedTcs.Task.IsCompleted)
        {
            var snapshot = await MonitorSnapshot.ReadAsync(monitor, watching.Token);
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
            var devices = await MonitorSnapshot.ReadAsync(monitor, watching.Token);
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
        await Arrival(reinserted).Task.WaitAsync(TimeSpan.FromSeconds(10));

        // A successful identity ceremony must also leave a healthy event lifetime. Do not suppress
        // overflow or another stream failure merely because the snapshot assertions succeeded.
        await monitor.DisposeAsync();
        await pump.WaitAsync(TimeSpan.FromSeconds(10));

        // Full captured timeline for the hardware-evidence record.
        output.WriteLine("Hot-plug protocol timeline:");
        output.WriteLine(Timeline());
        output.WriteLine("watcher pump completed normally after owned monitor disposal");
    }
}