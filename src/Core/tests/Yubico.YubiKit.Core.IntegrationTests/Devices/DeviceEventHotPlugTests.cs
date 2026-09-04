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
using Yubico.YubiKit.Tests.Shared.Infrastructure;

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

    public async Task InitializeAsync() => await YubiKeyManager.ShutdownAsync();

    public async Task DisposeAsync() => await YubiKeyManager.ShutdownAsync();

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task WatchAsync_AcrossRepeatedHotPlugCycles_EmitsRemovalAndArrivalAfterEachPrompt()
    {
        Assert.False(
            Console.IsInputRedirected,
            "This test requires interactive console input for each remove and insert checkpoint.");

        using var watching = new CancellationTokenSource();

        // WatchAsync subscribes on the first MoveNextAsync, and that call runs the iterator up to its
        // first suspension point before returning. Subscribing here rather than inside the pump task
        // is what stops the initial scan racing ahead of the watcher.
        var enumerator = YubiKeyManager.WatchAsync(watching.Token).GetAsyncEnumerator(watching.Token);
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

        var initialCheckpoint = _log.CreateCheckpoint();
        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));

        Prompt($"Leave your YubiKey plugged in. {Cycles} remove/insert cycles will be requested.");

        _ = await ExpectAsync(initialCheckpoint, DeviceAction.Added, "initial discovery");

        for (var cycle = 1; cycle <= Cycles; cycle++)
        {
            Prompt($"[cycle {cycle}/{Cycles}] Press Enter when ready to remove a YubiKey.");
            _ = Console.ReadLine();
            var removalCheckpoint = _log.CreateCheckpoint();
            Prompt($"[cycle {cycle}/{Cycles}] REMOVE a YubiKey now.");
            var removed = await ExpectAsync(removalCheckpoint, DeviceAction.Removed, $"cycle {cycle} removal");

            Prompt($"[cycle {cycle}/{Cycles}] Press Enter when ready to re-insert the YubiKey.");
            _ = Console.ReadLine();
            var insertionCheckpoint = _log.CreateCheckpoint();
            Prompt($"[cycle {cycle}/{Cycles}] RE-INSERT the same YubiKey now.");
            var added = await ExpectAsync(insertionCheckpoint, DeviceAction.Added, $"cycle {cycle} insertion");

            output.WriteLine(
                $"[cycle {cycle}] removed as '{removed.Device.DeviceId}', returned as '{added.Device.DeviceId}'");
        }

        output.WriteLine(_log.ToString());

        await watching.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pump);
    }

    private async Task<DeviceEvent> ExpectAsync(int checkpoint, DeviceAction action, string what)
    {
        var deviceEvent = await _log.WaitForAsync(
            checkpoint,
            deviceEvent => deviceEvent.Action == action,
            HumanActionTimeout);

        Assert.True(
            deviceEvent is not null,
            $"Timed out after {HumanActionTimeout.TotalSeconds:0}s waiting for " +
            $"{what}.{Environment.NewLine}{_log}");

        output.WriteLine($"  observed {deviceEvent!.Action}: {deviceEvent.Device.DeviceId}");

        return deviceEvent;
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
        private readonly List<(TimeSpan At, DeviceEvent Event)> _entries = [];
        private readonly Stopwatch _clock = Stopwatch.StartNew();

        private Func<DeviceEvent, bool>? _awaited;
        private TaskCompletionSource<DeviceEvent>? _pending;

        public void Record(DeviceEvent value)
        {
            TaskCompletionSource<DeviceEvent>? toComplete = null;

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
        public async Task<DeviceEvent?> WaitForAsync(
            int checkpoint,
            Func<DeviceEvent, bool> match,
            TimeSpan timeout)
        {
            TaskCompletionSource<DeviceEvent> tcs;

            lock (_gate)
            {
                for (var i = checkpoint; i < _entries.Count; i++)
                {
                    if (match(_entries[i].Event))
                    {
                        return _entries[i].Event;
                    }
                }

                tcs = new TaskCompletionSource<DeviceEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
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

        /// <summary>Full timeline, attached to any failure so a manual run leaves a usable trace.</summary>
        public override string ToString()
        {
            lock (_gate)
            {
                var lines = _entries.Select(e =>
                    $"  {e.At.TotalSeconds,7:0.000}s  {e.Event.Action,-7}  {e.Event.Device.DeviceId}");

                return $"Device event timeline ({_entries.Count} events):{Environment.NewLine}" +
                       string.Join(Environment.NewLine, lines);
            }
        }
    }
}