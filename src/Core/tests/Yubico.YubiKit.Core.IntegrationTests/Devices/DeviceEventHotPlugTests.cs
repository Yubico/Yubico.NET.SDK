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
using Xunit.Abstractions;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

/// <summary>
/// Verifies that the device-event surface remains live across repeated physical removal and
/// insertion cycles.
/// </summary>
/// <remarks>
/// This test requires an operator to remove and insert a YubiKey when prompted. It asserts event
/// sequence liveness after each prompt and does not attempt to correlate event identity.
/// </remarks>
public class DeviceEventHotPlugTests(ITestOutputHelper output) : IAsyncLifetime
{
    /// <summary>Insert/remove round trips to perform.</summary>
    private const int Cycles = 3;

    /// <summary>How long to wait for the requested operator action.</summary>
    private static readonly TimeSpan HumanActionTimeout = TimeSpan.FromSeconds(30);

    private readonly EventLog _log = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Discovery)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task WatchAsync_AcrossRepeatedHotPlugCycles_EmitsRemovalAndArrivalAfterEachPrompt()
    {
        Assert.False(
            Console.IsInputRedirected,
            "This test requires interactive console input for each remove and insert checkpoint.");

        using var watching = new CancellationTokenSource();

        await using var monitor = await YubiKeyManager.CreateMonitorAsync(watching.Token);
        // Capture the checkpoint before the pump can record initial replay.
        var initialCheckpoint = _log.CreateCheckpoint();
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
                        _log.Record(enumerator.Current);
                        move = enumerator.MoveNextAsync();
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync();
                }
            },
            CancellationToken.None);

        Prompt($"Leave your YubiKey plugged in. {Cycles} remove/insert cycles will be requested.");

        _ = await ExpectAsync<DeviceAdded>(initialCheckpoint, "initial discovery");

        for (var cycle = 1; cycle <= Cycles; cycle++)
        {
            Prompt($"[cycle {cycle}/{Cycles}] Press Enter when ready to remove a YubiKey.");
            _ = Console.ReadLine();
            var removalCheckpoint = _log.CreateCheckpoint();
            Prompt($"[cycle {cycle}/{Cycles}] REMOVE a YubiKey now.");
            var removed = await ExpectAsync<DeviceRemoved>(removalCheckpoint, $"cycle {cycle} removal");

            Prompt($"[cycle {cycle}/{Cycles}] Press Enter when ready to re-insert the YubiKey.");
            _ = Console.ReadLine();
            var insertionCheckpoint = _log.CreateCheckpoint();
            Prompt($"[cycle {cycle}/{Cycles}] RE-INSERT the same YubiKey now.");
            var added = await ExpectAsync<DeviceAdded>(insertionCheckpoint, $"cycle {cycle} insertion");

            output.WriteLine(
                $"[cycle {cycle}] removed as '{removed.Device.DeviceId}', returned as '{added.Device.DeviceId}'");

            // The hotplug invalidated every key's cached metadata, so the keys that stayed attached were
            // re-read on this scan. Equal metadata decoded from a fresh buffer must not publish an update.
            await Task.Delay(TimeSpan.FromSeconds(2));
            var sinceRemoval = _log.Since(removalCheckpoint);
            var spuriousUpdates = sinceRemoval.OfType<DeviceUpdated>()
                .Where(update => !ReferenceEquals(update.Device, added.Device) && !ReferenceEquals(update.Device, removed.Device))
                .ToList();
            Assert.True(spuriousUpdates.Count == 0,
                $"cycle {cycle}: {spuriousUpdates.Count} DeviceUpdated for keys that did not change:{Environment.NewLine}{_log}");
            var churn = sinceRemoval.Count(change => change is DeviceRemoved or DeviceAdded);
            Assert.True(churn == 2,
                $"cycle {cycle}: expected exactly one Removed and one Added, observed {churn} membership changes:{Environment.NewLine}{_log}");
        }

        output.WriteLine(_log.ToString());

        // Owned disposal must complete healthy streams normally. Overflow or another pump fault is
        // a real lifetime failure, not an outcome to swallow after the hotplug assertions passed.
        await monitor.DisposeAsync();
        await pump.WaitAsync(TimeSpan.FromSeconds(10));
        output.WriteLine("watcher pump completed normally after owned monitor disposal");
    }

    private async Task<TChange> ExpectAsync<TChange>(int checkpoint, string what) where TChange : DeviceChange
    {
        var deviceEvent = await _log.WaitForAsync(
            checkpoint,
            deviceEvent => deviceEvent is TChange,
            HumanActionTimeout);

        Assert.True(
            deviceEvent is not null,
            $"Timed out after {HumanActionTimeout.TotalSeconds:0}s waiting for " +
            $"{what}.{Environment.NewLine}{_log}");

        output.WriteLine($"  observed {deviceEvent}");

        return Assert.IsType<TChange>(deviceEvent);
    }

    private void Prompt(string message)
    {
        Console.WriteLine($">>> {message}");
        output.WriteLine($">>> {message}");
    }

    /// <summary>
    /// Records every event and lets the test await the next one of a given kind.
    /// </summary>
    /// <remarks>
    /// Records are appended from the watcher's pump task, so the completion source uses
    /// <see cref="TaskCreationOptions.RunContinuationsAsynchronously"/> to keep the resumed test off
    /// that task.
    /// </remarks>
    private sealed class EventLog
    {
        private readonly Lock _gate = new();
        private readonly List<(TimeSpan At, DeviceChange Event)> _entries = [];
        private readonly Stopwatch _clock = Stopwatch.StartNew();

        private Func<DeviceChange, bool>? _awaited;
        private TaskCompletionSource<DeviceChange>? _pending;

        public void Record(DeviceChange value)
        {
            TaskCompletionSource<DeviceChange>? toComplete = null;

            lock (_gate)
            {
                _entries.Add((_clock.Elapsed, value));

                if (_pending is not null && _awaited is not null && _awaited(value))
                {
                    toComplete = _pending;
                    _pending = null;
                    _awaited = null;
                }
            }

            _ = toComplete?.TrySetResult(value);
        }

        /// <summary>
        /// Returns the first event at or after <paramref name="checkpoint"/> that matches
        /// <paramref name="match"/>, waiting for one if necessary; null on timeout.
        /// </summary>
        public async Task<DeviceChange?> WaitForAsync(
            int checkpoint,
            Func<DeviceChange, bool> match,
            TimeSpan timeout)
        {
            TaskCompletionSource<DeviceChange> tcs;

            lock (_gate)
            {
                for (var i = checkpoint; i < _entries.Count; i++)
                {
                    if (match(_entries[i].Event))
                    {
                        return _entries[i].Event;
                    }
                }

                tcs = new TaskCompletionSource<DeviceChange>(TaskCreationOptions.RunContinuationsAsynchronously);
                _awaited = match;
                _pending = tcs;
            }

            try
            {
                return await tcs.Task.WaitAsync(timeout).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                lock (_gate)
                {
                    for (var i = checkpoint; i < _entries.Count; i++)
                    {
                        if (match(_entries[i].Event))
                        {
                            return _entries[i].Event;
                        }
                    }

                    return null;
                }
            }
            finally
            {
                lock (_gate)
                {
                    if (ReferenceEquals(_pending, tcs))
                    {
                        _awaited = null;
                        _pending = null;
                    }
                }
            }
        }

        public int CreateCheckpoint()
        {
            lock (_gate)
            {
                return _entries.Count;
            }
        }

        /// <summary>Every change recorded at or after <paramref name="checkpoint"/>.</summary>
        public IReadOnlyList<DeviceChange> Since(int checkpoint)
        {
            lock (_gate)
            {
                return [.. _entries.Skip(checkpoint).Select(entry => entry.Event)];
            }
        }

        /// <summary>Full timeline, attached to any failure so a manual run leaves a usable trace.</summary>
        public override string ToString()
        {
            lock (_gate)
            {
                var lines = _entries.Select(e =>
                    $"  {e.At.TotalSeconds,7:0.000}s  {e.Event}");

                return $"Device event timeline ({_entries.Count} events):{Environment.NewLine}" +
                       string.Join(Environment.NewLine, lines);
            }
        }
    }
}