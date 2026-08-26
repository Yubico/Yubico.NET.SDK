// Native AOT publish/link verification host.
//
// This is intentionally NOT a shipped CLI tool, NOT a test project, and NOT part of the SDK
// package surface. Its only job is to give CI something real to `dotnet publish -p:PublishAot=true`
// against, so the ILC (IL Compiler) trimming/AOT analysis has genuine code paths from every
// in-scope SDK library to walk during publish.
//
// Two modes:
//
//   (default)   Link verification + one-shot discovery. Runs safely with zero YubiKeys attached
//               (as on CI runners) - FindAllAsync simply returns an empty list. This is the
//               recurring CI gate; its behaviour is unchanged.
//
//   --monitor   Interactive, human-in-the-loop device-monitoring validation. Requires hardware and
//               an operator. Exercises the hot-plug path that CI structurally cannot: every CI
//               assertion runs with no keys attached, so multicast fan-out, event coalescing and
//               Added/Removed correlation are never observed there. See docs/NATIVE-AOT.md.
//
// The --monitor protocol attaches every consumer surface at once, so a single insert/removal
// exercises all of them. Its strongest assertion is that all sinks observe an identical, identically
// ordered event sequence: that one check catches nearly any multicast defect.

using System.Globalization;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.OpenPgp;
using Yubico.YubiKit.Oath;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.SecurityDomain;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.YubiHsm;
using Yubico.YubiKit.YubiOtp;

Console.WriteLine("Native AOT verification host starting...");

// Force the linker/ILC to keep and process each module's public entry-point type. Management is
// anchored explicitly here because Core does not reference Management (the dependency flows
// Management -> Core), so device discovery alone would let ILC trim the Management assembly out
// of this host's whole-program closure — defeating the point of exercising every in-scope module.
Type[] verifiedModuleEntryTypes =
[
    typeof(ManagementSession),
    typeof(PivSession),
    typeof(FidoSession),
    typeof(WebAuthnClient),
    typeof(OathSession),
    typeof(OpenPgpSession),
    typeof(SecurityDomainSession),
    typeof(YubiOtpSession),
    typeof(HsmAuthSession)
];

foreach (Type type in verifiedModuleEntryTypes)
{
    Console.WriteLine($"  Linked module entry type: {type.FullName}");
}

// Real device discovery through Core's PC/SC + platform interop path — safe with zero attached
// YubiKeys (returns an empty list rather than throwing).
IReadOnlyList<IYubiKey> devices = await YubiKeyManager.FindAllAsync(CancellationToken.None);
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
/// Interactive device-monitoring protocol. Attaches every consumer surface simultaneously so one
/// physical action validates all of them, then checks cross-sink consistency at the end.
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

        using var cts = new CancellationTokenSource();
        var sinkA = new Sink("observer-A");
        var sinkB = new Sink("observer-B");
        var sinkWatch = new Sink("watchasync");
        var sinkTransient = new Sink("observer-transient");

        using var subA = YubiKeyManager.DeviceChanges.Subscribe(sinkA);
        using var subB = YubiKeyManager.DeviceChanges.Subscribe(sinkB);
        var subTransient = YubiKeyManager.DeviceChanges.Subscribe(sinkTransient);

        // Start the async consumer before monitoring, since WatchAsync subscribes on first
        // enumeration rather than at call time.
        var watcher = Task.Run(async () =>
        {
            try
            {
                await foreach (var e in YubiKeyManager.WatchAsync(cts.Token))
                {
                    sinkWatch.OnNext(e);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        });

        await Task.Delay(TimeSpan.FromMilliseconds(250));
        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));

        var failures = new List<string>();

        // Clearing the field is a setup action, not an assertion: keys attached before monitoring
        // started are already cached, so removing them legitimately emits events. Everything after
        // this point is measured from the resulting baseline.
        Step(1, "Remove ALL YubiKeys (USB and NFC reader) to establish a clean baseline.",
            sinkA, expected: -1, failures);

        var baseline = sinkA.Snapshot().Count;

        Step(2, "Insert USB key A. A composite key (CCID + HID FIDO + HID OTP) must appear ONCE.",
            sinkA, expected: 1, failures, DeviceAction.Added);

        Step(3, "Insert a second USB key (key B).",
            sinkA, expected: 1, failures, DeviceAction.Added);

        Step(4, "Remove key A.", sinkA, expected: 1, failures, DeviceAction.Removed);

        Step(5, "Tap/hold the NFC key on the reader (SmartCard-only; contrast with composite USB). "
              + "Skip with Enter if unavailable.", sinkA, expected: -1, failures);

        var rapidActivityStart = sinkA.Snapshot().Count;
        Step(6, "Rapidly insert and remove key B three times.", sinkA, expected: -1, failures);
        var rapidActivity = sinkA.Snapshot().Skip(rapidActivityStart).ToList();
        if (!rapidActivity.Any(e => e.StartsWith($"{DeviceAction.Added}|", StringComparison.Ordinal))
            || !rapidActivity.Any(e => e.StartsWith($"{DeviceAction.Removed}|", StringComparison.Ordinal)))
        {
            failures.Add("step 6: rapid activity did not produce both Added and Removed events");
            Console.WriteLine("  FAIL  step 6: expected both Added and Removed events");
        }
        else
        {
            Console.WriteLine("  ok    step 6: observed both Added and Removed events");
        }

        subTransient.Dispose();
        var transientAtUnsubscribe = sinkTransient.Snapshot().Count;
        var activeAtUnsubscribe = sinkA.Snapshot().Count;

        // Count is informational: with a two-key setup the operator may be moving one key between
        // transports, which legitimately produces a Removed and an Added.
        Step(7, "One observer just unsubscribed. Attach a key that is not currently attached "
              + "(moving one off the NFC reader to USB is fine).", sinkA, expected: -1, failures);

        if (sinkA.Snapshot().Count == activeAtUnsubscribe)
        {
            failures.Add("step 7: no event observed after transient observer unsubscribed");
            Console.WriteLine("  FAIL  step 7: no post-unsubscribe event observed");
        }

        if (sinkTransient.Snapshot().Count != transientAtUnsubscribe)
        {
            failures.Add("unsubscribed observer kept receiving events");
            Console.WriteLine("  FAIL  observer-transient received events after unsubscribe");
        }
        else
        {
            Console.WriteLine("  ok    observer-transient silent since unsubscribe");
        }

        // --- cross-sink consistency: the primary invariant ---
        Console.WriteLine();
        Console.WriteLine("--- Cross-sink consistency ---");
        var reference = sinkA.Snapshot().Skip(baseline).ToList();
        foreach (var other in new[] { sinkB, sinkWatch })
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

        // --- Added/Removed correlation ---
        var unmatched = Correlate(reference);
        if (unmatched.Count > 0)
        {
            failures.Add("Removed events without a matching Added");
            Console.WriteLine($"  FAIL  {unmatched.Count} Removed event(s) with no correlating Added: {string.Join(", ", unmatched)}");
        }
        else
        {
            Console.WriteLine("  ok    every Removed correlates to a prior Added");
        }

        // --- Step 8: shutdown with a key attached ---
        Console.WriteLine();
        Prompt(8, "Leave a key attached. Press Enter to shut down.");
        await YubiKeyManager.ShutdownAsync();
        await cts.CancelAsync();

        var watcherExited = await Task.WhenAny(watcher, Task.Delay(TimeSpan.FromSeconds(10))) == watcher;
        if (!watcherExited)
        {
            failures.Add("WatchAsync did not exit within 10s of shutdown");
            Console.WriteLine("  FAIL  WatchAsync consumer did not exit cleanly");
        }
        else
        {
            Console.WriteLine("  ok    WatchAsync consumer exited cleanly");
        }

        if (!sinkA.Completed || !sinkB.Completed)
        {
            failures.Add("observers did not receive OnCompleted on shutdown");
            Console.WriteLine("  FAIL  observers did not receive OnCompleted");
        }
        else
        {
            Console.WriteLine("  ok    observers received OnCompleted");
        }

        // --- Step 9: restart after shutdown ---
        Console.WriteLine();
        Prompt(9, "Press Enter to restart monitoring (verifies static state recreates).");
        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));
        var restarted = YubiKeyManager.IsMonitoring;
        Console.WriteLine(restarted ? "  ok    monitoring restarted" : "  FAIL  monitoring did not restart");
        if (!restarted)
        {
            failures.Add("monitoring did not restart after shutdown");
        }

        await YubiKeyManager.ShutdownAsync();

        Console.WriteLine();
        Console.WriteLine("=== Result ===");
        Console.WriteLine($"Total events observed: {reference.Count}");
        foreach (var e in reference)
        {
            Console.WriteLine($"  {e}");
        }

        if (failures.Count == 0)
        {
            Console.WriteLine("PASS - all monitoring invariants held.");
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
        DeviceAction? expectedAction = null)
    {
        var before = reference.Snapshot().Count;
        Prompt(number, instruction);

        // Give the monitor's interval scan a moment to settle after the operator's action.
        Thread.Sleep(TimeSpan.FromSeconds(2));

        var after = reference.Snapshot();
        var delta = after.Count - before;

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
            var last = after[^1];
            if (!last.Contains(action.ToString(), StringComparison.Ordinal))
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

    /// <summary>Returns device ids reported Removed without a preceding Added.</summary>
    private static List<string> Correlate(IReadOnlyList<string> sequence)
    {
        var present = new HashSet<string>(StringComparer.Ordinal);
        var unmatched = new List<string>();

        foreach (var entry in sequence)
        {
            var parts = entry.Split('|', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            var (action, id) = (parts[0], parts[1]);
            if (action == nameof(DeviceAction.Added))
            {
                _ = present.Add(id);
            }
            else if (!present.Remove(id))
            {
                unmatched.Add(id);
            }
        }

        return unmatched;
    }

    /// <summary>Records the event sequence one consumer surface observed.</summary>
    private sealed class Sink(string name) : IObserver<DeviceEvent>
    {
        private readonly Lock _gate = new();
        private readonly List<string> _events = [];

        public string Name { get; } = name;

        public bool Completed { get; private set; }

        public void OnNext(DeviceEvent value)
        {
            var entry = string.Create(
                CultureInfo.InvariantCulture,
                $"{value.Action}|{value.Device.DeviceId}");

            lock (_gate)
            {
                _events.Add(entry);
            }

            Console.WriteLine($"        [{Name}] {entry}");
        }

        public void OnCompleted()
        {
            lock (_gate)
            {
                Completed = true;
            }
        }

        public void OnError(Exception error)
        {
            // Not used; the SDK stream never faults.
        }

        public IReadOnlyList<string> Snapshot()
        {
            lock (_gate)
            {
                return [.. _events];
            }
        }
    }
}
