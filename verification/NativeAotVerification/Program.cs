// Internal Native AOT verification host. The default mode provides link evidence for every SDK
// library and runs Core discovery. The optional --monitor mode is an operator-driven diagnostic,
// not part of the recurring workflow. See docs/NATIVE-AOT.md for the evidence boundaries.

using System.Globalization;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.OpenPgp;
using Yubico.YubiKit.Oath;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.SecurityDomain;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.YubiHsm;
using Yubico.YubiKit.YubiOtp;

Console.WriteLine("Native AOT verification host starting...");

// Reference ten public entry-point types to exercise selected linker/ILC reachability. This does
// not root or execute every public method. Management is
// anchored explicitly here because Core does not reference Management (the dependency flows
// Management -> Core), so device discovery alone would let ILC trim the Management assembly out
// of this host's whole-program closure.
Type[] linkedModuleEntryTypes =
[
    typeof(YubiKeyManager),
    typeof(ManagementSession),
    typeof(PivSession),
    typeof(Ctap2Session),
    typeof(WebAuthnClient),
    typeof(OathSession),
    typeof(OpenPgpSession),
    typeof(SecurityDomainSession),
    typeof(YubiOtpSession),
    typeof(HsmAuthSession)
];

foreach (Type type in linkedModuleEntryTypes)
{
    Console.WriteLine($"  Linked module entry type: {type.FullName}");
}

// Discover across Core's enabled interface routes. An empty successful scan returns no devices;
// discovery can still open interfaces for metadata and propagate failures. This is not a full API,
// applet-ceremony or all-platform native qualification run.
IReadOnlyList<IYubiKey> devices = await YubiKeyManager.FindAllAsync(cancellationToken: CancellationToken.None);
Console.WriteLine($"Found {devices.Count} YubiKey(s).");
foreach (IYubiKey device in devices)
{
    Console.WriteLine($"  {device}");
}

if (args.Contains("--monitor", StringComparer.Ordinal))
{
    // Optional: advance steps by dropping a marker file instead of pressing Enter, so the protocol
    // can be driven by a supervising process while a human performs the physical actions.
    var signalIndex = Array.IndexOf(args, "--signal-dir");
    var signalDir = signalIndex >= 0 && signalIndex + 1 < args.Length ? args[signalIndex + 1] : null;

    var exitCode = await MonitorVerification.RunAsync(signalDir);
    Console.WriteLine("Native AOT verification host completed.");
    return exitCode;
}

Console.WriteLine("Native AOT verification host completed without crashing.");
return 0;

/// <summary>
/// Interactive diagnostic with three WatchAsync subscribers on one owned monitor. Checks selected
/// membership events, subscription stop and monitor shutdown/recreation. Cross-sink snapshots are
/// compared while pumps are running, not after a barrier establishing an identical complete prefix.
/// </summary>
internal static class MonitorVerification
{
    private const int StepTimeoutSeconds = 120;

    private static string? _signalDir;

    internal static async Task<int> RunAsync(string? signalDir = null)
    {
        _signalDir = signalDir;

        if (_signalDir is not null)
        {
            // Output is redirected to a log the supervisor tails, so it must not sit in a buffer.
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            _ = Directory.CreateDirectory(_signalDir);
        }

        Console.WriteLine();
        Console.WriteLine("=== Device monitoring verification (interactive) ===");
        Console.WriteLine("Follow each prompt, then press Enter. Ctrl+C aborts.");
        Console.WriteLine();

        // All consumers share one explicitly owned monitor; each gets initial replay plus changes.
        var monitor = await YubiKeyManager.CreateMonitorAsync().ConfigureAwait(false);
        await using var monitorLifetime = monitor.ConfigureAwait(false);
        await using var sinkA = Sink.Start("watcher-A", monitor);
        await using var sinkB = Sink.Start("watcher-B", monitor);
        await using var sinkTransient = Sink.Start("watcher-transient", monitor);

        var failures = new List<string>();

        // Clearing the field is a setup action, not an assertion: keys attached before monitoring
        // started are already cached, so removing them legitimately emits events. Everything after
        // this point is measured from the resulting baseline.
        Step(1, "Remove ALL YubiKeys (USB and NFC reader) to establish a clean baseline.",
            sinkA, expected: -1, failures);

        var baselineA = sinkA.Snapshot().Count;
        var baselineB = sinkB.Snapshot().Count;

        Step(2, "Insert USB key A. A composite key (CCID + HID FIDO + HID OTP) must appear ONCE.",
            sinkA, expected: 1, failures, "Added");

        Step(3, "Insert a second USB key (key B).",
            sinkA, expected: 1, failures, "Added");

        Step(4, "Remove key A.", sinkA, expected: 1, failures, "Removed");

        Step(5, "Tap/hold the NFC key on the reader (SmartCard-only; contrast with composite USB). "
              + "Skip with Enter if unavailable.", sinkA, expected: -1, failures);

        var rapidActivityStart = sinkA.Snapshot().Count;
        Step(6, "Remove key B and wait for Removed output, then reinsert it and wait for Added "
              + "output. Repeat three times.", sinkA, expected: -1, failures);
        var rapidActivity = sinkA.Snapshot().Skip(rapidActivityStart).ToList();
        // Despite the three-cycle instruction, this oracle requires only one Added and one Removed.
        if (!rapidActivity.Any(e => e.StartsWith("Added|", StringComparison.Ordinal))
            || !rapidActivity.Any(e => e.StartsWith("Removed|", StringComparison.Ordinal)))
        {
            failures.Add("step 6: rapid activity did not produce both Added and Removed events");
            Console.WriteLine("  FAIL  step 6: expected both Added and Removed events");
        }
        else
        {
            Console.WriteLine("  ok    step 6: observed both Added and Removed events");
        }

        await sinkTransient.StopAsync();
        var transientAtStop = sinkTransient.Snapshot().Count;
        var activeAtStop = sinkA.Snapshot().Count;

        // Count is informational: with a two-key setup the operator may be moving one key between
        // transports, which legitimately produces a Removed and an Added.
        Step(7, "One watcher just stopped. Attach a key that is not currently attached "
              + "(moving one off the NFC reader to USB is fine).", sinkA, expected: -1, failures);

        if (sinkA.Snapshot().Count == activeAtStop)
        {
            failures.Add("step 7: no event observed after the transient watcher stopped");
            Console.WriteLine("  FAIL  step 7: no post-stop event observed");
        }

        if (sinkTransient.Snapshot().Count != transientAtStop)
        {
            failures.Add("a cancelled watcher kept receiving events");
            Console.WriteLine("  FAIL  watcher-transient received events after it stopped");
        }
        else
        {
            Console.WriteLine("  ok    watcher-transient silent since it stopped");
        }

        // Compare independently sampled sequences while the two pumps are still active. A mismatch
        // can reflect snapshot timing; this is not a post-shutdown equal-prefix or full-race proof.
        Console.WriteLine();
        Console.WriteLine("--- Cross-sink consistency ---");
        var reference = sinkA.Snapshot().Skip(baselineA).ToList();
        foreach (var (other, baseline) in new[] { (sinkB, baselineB) })
        {
            var seq = other.Snapshot().Skip(baseline).ToList();
            if (!seq.SequenceEqual(reference, StringComparer.Ordinal))
            {
                failures.Add($"{other.Name} diverged from {sinkA.Name}");
                Console.WriteLine($"  FAIL  {other.Name}: {seq.Count} events, expected identical to {reference.Count}");
                Console.WriteLine($"        {sinkA.Name}: {string.Join(", ", reference)}");
                Console.WriteLine($"        {other.Name}: {string.Join(", ", seq)}");
            }
            else
            {
                Console.WriteLine($"  ok    {other.Name}: identical sequence ({seq.Count} events)");
            }
        }

        // --- Step 8: shutdown with a key attached ---
        Console.WriteLine();
        Prompt(8, "Leave a key attached. Press Enter to shut down.");
        await monitor.DisposeAsync().ConfigureAwait(false);

        // Shutdown must end every active watcher normally - not cancelled, not faulted.
        foreach (var sink in new[] { sinkA, sinkB })
        {
            if (!await sink.WaitForExitAsync(TimeSpan.FromSeconds(10)))
            {
                failures.Add($"{sink.Name} did not exit within 10s of shutdown");
                Console.WriteLine($"  FAIL  {sink.Name} did not exit within 10s of shutdown");
            }
            else if (!sink.EndedNormally)
            {
                failures.Add($"{sink.Name} did not end normally on shutdown");
                Console.WriteLine($"  FAIL  {sink.Name} ended abnormally on shutdown");
            }
            else
            {
                Console.WriteLine($"  ok    {sink.Name} ended normally on shutdown");
            }
        }

        // --- Step 9: restart after shutdown ---
        Console.WriteLine();
        Prompt(9, "Press Enter to create a new initialized monitor after disposal.");
        var replacement = await YubiKeyManager.CreateMonitorAsync().ConfigureAwait(false);
        await using var replacementLifetime = replacement.ConfigureAwait(false);
        var count = 0;
        await foreach (var change in replacement.WatchAsync().ConfigureAwait(false))
        {
            if (change is SnapshotComplete) break;
            if (change is DeviceAdded) count++;
        }
        Console.WriteLine($"  ok    new monitor initialized with {count} device(s)");

        Console.WriteLine();
        Console.WriteLine("=== Result ===");
        Console.WriteLine($"Total events observed: {reference.Count}");
        foreach (var e in reference)
        {
            Console.WriteLine($"  {e}");
        }

        if (failures.Count == 0)
        {
            Console.WriteLine("PASS - all configured monitoring checks passed.");
            return 0;
        }

        Console.WriteLine($"FAIL - {failures.Count} invariant(s) violated:");
        foreach (var f in failures)
        {
            Console.WriteLine($"  - {f}");
        }

        return 1;
    }

    private static void Step(
        int number,
        string instruction,
        Sink reference,
        int expected,
        List<string> failures,
        string? expectedAction = null)
    {
        var before = reference.Snapshot().Count;
        Prompt(number, instruction);

        // Allow one additional polling interval before inspecting the event snapshot.
        Thread.Sleep(TimeSpan.FromSeconds(6));

        // Updated entries are logged and compared across subscribers, but a late metadata read after an
        // insertion is not a membership change and must not fail an insertion/removal step.
        var after = reference.Snapshot();
        var membership = after.Skip(before).Where(e => !e.StartsWith("Updated|", StringComparison.Ordinal)).ToList();
        var delta = membership.Count;

        if (expected < 0)
        {
            Console.WriteLine($"  info  {delta} event(s) observed (not asserted)");
            return;
        }

        if (delta == expected)
        {
            Console.WriteLine($"  ok    {delta} event(s), as expected");
        }
        else
        {
            var message = $"step {number}: expected {expected} event(s), observed {delta}";
            failures.Add(message);
            Console.WriteLine($"  FAIL  {message}");
            foreach (var e in after.Skip(before))
            {
                Console.WriteLine($"        {e}");
            }
        }

        if (expectedAction is { } action && delta > 0)
        {
            var last = membership[^1];
            if (!last.StartsWith($"{action}|", StringComparison.Ordinal))
            {
                var message = $"step {number}: expected a {action} event, last was '{last}'";
                failures.Add(message);
                Console.WriteLine($"  FAIL  {message}");
            }
        }
    }

    private static void Prompt(int number, string instruction)
    {
        Console.WriteLine();
        Console.WriteLine($"Step {number}: {instruction}");

        if (_signalDir is null)
        {
            Console.Write("        Press Enter when done... ");
            _ = Console.ReadLine();
            return;
        }

        var marker = Path.Combine(_signalDir, "step.go");
        Console.WriteLine($"AWAITING-SIGNAL step={number}");

        var deadline = DateTime.UtcNow.AddMinutes(20);
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(marker))
            {
                try
                {
                    File.Delete(marker);
                }
#pragma warning disable CA1031 // A stale marker must not abort the protocol.
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    Console.WriteLine($"        (could not clear marker: {ex.Message})");
                }

                Console.WriteLine($"SIGNAL-RECEIVED step={number}");
                return;
            }

            Thread.Sleep(200);
        }

        Console.WriteLine($"SIGNAL-TIMEOUT step={number}");
    }

    /// <summary>Drains one <c>WatchAsync</c> enumeration and records the sequence it received.</summary>
    private sealed class Sink : IAsyncDisposable
    {
        private readonly Lock _gate = new();
        private readonly List<string> _events = [];
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _pump;

        private volatile bool _endedNormally;

        private Sink(string name, YubiKeyMonitor monitor)
        {
            Name = name;

            // Replay captures current membership atomically with registration on the first move.
            var enumerator = monitor.WatchAsync(_cts.Token).GetAsyncEnumerator(_cts.Token);
            var first = enumerator.MoveNextAsync();

            _pump = Task.Run(async () =>
            {
                try
                {
                    var move = first;
                    while (await move)
                    {
                        Record(enumerator.Current);
                        move = enumerator.MoveNextAsync();
                    }

                    _endedNormally = true;
                }
                catch (OperationCanceledException)
                {
                    // Expected when this watcher is stopped deliberately.
                }
                finally
                {
                    await enumerator.DisposeAsync();
                }
            });
        }

        public string Name { get; }

        /// <summary>True once the enumeration ended because the SDK completed the sequence.</summary>
        public bool EndedNormally => _endedNormally;

        public static Sink Start(string name, YubiKeyMonitor monitor) => new(name, monitor);

        /// <summary>Cancels this watcher only and waits for its pump to unwind.</summary>
        public async Task StopAsync()
        {
            await _cts.CancelAsync();
            await _pump;
        }

        public async Task<bool> WaitForExitAsync(TimeSpan timeout) =>
            await Task.WhenAny(_pump, Task.Delay(timeout)) == _pump;

        public IReadOnlyList<string> Snapshot()
        {
            lock (_gate)
            {
                return [.. _events];
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();

            try
            {
                await _pump;
            }
            catch (OperationCanceledException)
            {
                // Teardown; the run's assertions have already been made.
            }

            _cts.Dispose();
        }

        private void Record(DeviceChange value)
        {
            var (action, device) = value switch
            {
                DeviceAdded added => ("Added", added.Device),
                DeviceRemoved removed => ("Removed", removed.Device),
                DeviceUpdated updated => ("Updated", updated.Device),
                _ => (null, null)
            };
            if (device is null) return; // SnapshotComplete marks replay, not device activity.
            var entry = string.Create(
                CultureInfo.InvariantCulture,
                $"{action}|{device.DeviceId}");

            lock (_gate)
            {
                _events.Add(entry);
            }

            Console.WriteLine($"        [{Name}] {entry}");
        }
    }
}
