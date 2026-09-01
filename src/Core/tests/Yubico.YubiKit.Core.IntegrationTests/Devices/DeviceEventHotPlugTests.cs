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
/// Drives repeated physical insert/remove cycles against the <see cref="IObservable{T}"/> device
/// event surface.
/// </summary>
/// <remarks>
/// <para>
/// This fills two gaps that no other test covers. <c>YubiKeyTests</c> exercises a single hot-plug
/// but only through <c>WatchAsync</c>, and <c>CoreTests</c> exercises <c>DeviceChanges</c> but only
/// through the initial rescan. Nothing exercised <c>DeviceChanges</c> under an actual hot-plug, and
/// nothing anywhere exercised more than one cycle.
/// </para>
/// <para>
/// Repetition is the point. Several documented hazards in <c>src/Core/CLAUDE.md</c> can only be
/// reached by removing and reinserting: the identity cache keys on interface id, and a same-slot
/// swap reuses that id, so a stale entry would attribute a departed key's serial to its successor.
/// The composite merger also flips a device's <c>DeviceId</c> between the serial tier and the PID
/// tier as sibling interfaces come and go. Both are unit-tested against fakes; this is the only
/// place either is checked against real hardware.
/// </para>
/// <para>
/// Requires manual interaction, so it is not part of any automated run. It is written for the
/// mandated pre-release pass over <c>RequiresUserPresence</c> tests. Prompts are written to
/// <see cref="Console"/> rather than <c>ITestOutputHelper</c> because the latter is buffered until
/// the test finishes, which is useless when the test is waiting on you.
/// </para>
/// </remarks>
public class DeviceEventHotPlugTests(ITestOutputHelper output) : IAsyncLifetime
{
    /// <summary>Insert/remove round trips to perform. Raise it when chasing an intermittent fault.</summary>
    private const int Cycles = 3;

    /// <summary>How long to wait for a human to unplug or replug the key.</summary>
    private static readonly TimeSpan HumanActionTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Quiet period after an arrival before its <c>DeviceId</c> is treated as settled. A composite
    /// key's interfaces do not all enumerate at once, so the merger can legitimately emit
    /// Removed/Added churn while the picture fills in.
    /// </summary>
    private static readonly TimeSpan SettleDelay = TimeSpan.FromSeconds(2);

    private readonly EventLog _log = new();

    // Same reasoning as CoreTests: YubiKeyManager is static and its device cache outlives a test
    // class, so a populated cache would suppress the initial Added this test uses as its baseline.
    public async Task InitializeAsync() => await YubiKeyManager.ShutdownAsync();

    public async Task DisposeAsync() => await YubiKeyManager.ShutdownAsync();

    [Fact]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task DeviceChanges_AcrossRepeatedHotPlugCycles_PairsEventsAndKeepsIdentityStable()
    {
        // This test reads back "the" device after each cycle, so a second connected key makes every
        // identity assertion ambiguous: the baseline can be captured from one key and the settled
        // read taken from the other, which looks exactly like an identity bug and is not one.
        // Fail fast and say so, rather than reporting a mismatch the operator cannot interpret.
        var connected = await YubiKeyManager.FindAllAsync();
        Assert.True(
            connected.Count == 1,
            $"This test drives a single key by hand and needs exactly one YubiKey connected; found " +
            $"{connected.Count} ({string.Join(", ", connected.Select(d => d.DeviceId))}). " +
            "Unplug the others and re-run.");

        // That probe re-populated the cache InitializeAsync had just cleared, and a populated cache
        // makes the initial rescan a no-op — no Added event, no baseline. Clear it again.
        await YubiKeyManager.ShutdownAsync();

        // Subscribe before monitoring starts, otherwise the initial rescan races the subscription.
        using var subscription = YubiKeyManager.DeviceChanges.Subscribe(_log);
        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));

        Prompt($"Leave your YubiKey plugged in. {Cycles} remove/insert cycles will be requested.");

        var baseline = await ExpectAsync(DeviceAction.Added, "initial discovery");
        await Task.Delay(SettleDelay);
        var baselineId = _log.LastDeviceId(DeviceAction.Added) ?? baseline.Device.DeviceId;
        output.WriteLine($"Baseline device: {baselineId}");

        for (var cycle = 1; cycle <= Cycles; cycle++)
        {
            Prompt($"[cycle {cycle}/{Cycles}] REMOVE the YubiKey now.");
            _ = await ExpectAsync(DeviceAction.Removed, $"cycle {cycle} removal");

            Prompt($"[cycle {cycle}/{Cycles}] RE-INSERT the YubiKey now.");
            _ = await ExpectAsync(DeviceAction.Added, $"cycle {cycle} insertion");

            // Let a composite key finish enumerating before reading the settled identity.
            await Task.Delay(SettleDelay);

            var settledId = _log.LastDeviceId(DeviceAction.Added);
            output.WriteLine($"[cycle {cycle}] settled device: {settledId}");

            Assert.True(
                settledId == baselineId,
                $"DeviceId changed across a same-slot reinsertion on cycle {cycle}: expected " +
                $"'{baselineId}', got '{settledId}'. Note that a tier change is not by itself a " +
                $"defect — the merger picks the strongest available identity, so a serial-tier id " +
                $"legitimately becomes a PID-tier one when the metadata read does not land. What " +
                $"this pins is that a settled key reaches the same identity it had before, given " +
                $"the same slot and the same readable metadata.{Environment.NewLine}{_log}");
        }

        output.WriteLine(_log.ToString());
    }

    private async Task<DeviceEvent> ExpectAsync(DeviceAction action, string what)
    {
        var deviceEvent = await _log.WaitForAsync(action, HumanActionTimeout);

        Assert.True(
            deviceEvent is not null,
            $"Timed out after {HumanActionTimeout.TotalSeconds:0}s waiting for {action} " +
            $"({what}).{Environment.NewLine}{_log}");

        output.WriteLine($"  observed {action}: {deviceEvent!.Device.DeviceId}");

        return deviceEvent;
    }

    private void Prompt(string message)
    {
        // Console, not ITestOutputHelper: the human needs to read this while the test is blocked.
        Console.WriteLine($">>> {message}");
        output.WriteLine($">>> {message}");
    }

    /// <summary>
    /// Records every event and lets the test await the next one of a given kind.
    /// </summary>
    /// <remarks>
    /// Observers are invoked inline on the publishing thread, so the completion source uses
    /// <see cref="TaskCreationOptions.RunContinuationsAsynchronously"/> to keep the resumed test off
    /// the monitor's thread.
    /// </remarks>
    private sealed class EventLog : IObserver<DeviceEvent>
    {
        private readonly Lock _gate = new();
        private readonly List<(TimeSpan At, DeviceEvent Event)> _entries = [];
        private readonly Stopwatch _clock = Stopwatch.StartNew();

        private DeviceAction? _awaited;
        private TaskCompletionSource<DeviceEvent>? _pending;

        /// <summary>
        /// Index of the first entry not yet handed to a <see cref="WaitForAsync"/> caller.
        /// </summary>
        /// <remarks>
        /// A human cannot be expected to stay in lockstep with the prompts, and acting early must not
        /// fail the test. Events are therefore consumed from the recorded log rather than only from
        /// the live callback, so a removal that happened before the prompt was printed still
        /// satisfies the wait for it.
        /// </remarks>
        private int _cursor;

        public void OnNext(DeviceEvent value)
        {
            TaskCompletionSource<DeviceEvent>? toComplete = null;

            lock (_gate)
            {
                _entries.Add((_clock.Elapsed, value));

                if (_awaited == value.Action && _pending is not null)
                {
                    toComplete = _pending;
                    _pending = null;
                    _awaited = null;
                    _cursor = _entries.Count;
                }
            }

            _ = toComplete?.TrySetResult(value);
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        /// <summary>
        /// Consumes the next unconsumed event of <paramref name="action"/>, waiting for one if it has
        /// not happened yet; null on timeout.
        /// </summary>
        public async Task<DeviceEvent?> WaitForAsync(DeviceAction action, TimeSpan timeout)
        {
            TaskCompletionSource<DeviceEvent> tcs;

            lock (_gate)
            {
                for (var i = _cursor; i < _entries.Count; i++)
                {
                    if (_entries[i].Event.Action == action)
                    {
                        _cursor = i + 1;
                        return _entries[i].Event;
                    }
                }

                tcs = new TaskCompletionSource<DeviceEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
                _awaited = action;
                _pending = tcs;
            }

            var winner = await Task.WhenAny(tcs.Task, Task.Delay(timeout)).ConfigureAwait(false);

            if (winner == tcs.Task)
            {
                return await tcs.Task.ConfigureAwait(false);
            }

            lock (_gate)
            {
                _awaited = null;
                _pending = null;
            }

            return null;
        }

        /// <summary>Most recent device id seen for <paramref name="action"/>, or null.</summary>
        public string? LastDeviceId(DeviceAction action)
        {
            lock (_gate)
            {
                for (var i = _entries.Count - 1; i >= 0; i--)
                {
                    if (_entries[i].Event.Action == action)
                    {
                        return _entries[i].Event.Device.DeviceId;
                    }
                }

                return null;
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