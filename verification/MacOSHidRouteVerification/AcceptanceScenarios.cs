using System.Diagnostics;
using System.Globalization;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Management;

internal static class AcceptanceScenarios
{
    internal static int TimingSelfTest()
    {
        long tick = Stopwatch.Frequency / 1000;
        var resolvedFirst = CreateResolvedFirstFixture(tick);
        AssertFirstEvent(resolvedFirst, tick);
        var caughtFirst = new CancelTiming();
        caughtFirst.MarkCancelInvocation(100 * tick);
        caughtFirst.MarkTerminalCatch(150 * tick);
        caughtFirst.MarkResolved(200 * tick);
        caughtFirst.MarkSameSessionInfo(300 * tick);
        var simultaneous = new CancelTiming();
        simultaneous.MarkCancelInvocation(100 * tick);
        simultaneous.MarkTerminalCatch(150 * tick);
        simultaneous.MarkResolved(150 * tick);
        simultaneous.MarkSameSessionInfo(300 * tick);
        var late = caughtFirst.Snapshot();
        var equal = simultaneous.Snapshot();
        var missing = new CancelTiming().Snapshot();
        AssertCallbackOrdering(late, equal, missing, simultaneous);
        Console.WriteLine("PASS timing self-test: resolved-before-catch, caught-before-resolved, and equal timestamps");
        return 0;
    }

    private static CancelTiming CreateResolvedFirstFixture(long tick)
    {
        var timing = new CancelTiming();
        timing.MarkCancelInvocation(100 * tick);
        timing.MarkCancelInvocation(400 * tick);
        timing.MarkResolved(150 * tick);
        timing.MarkTerminalCatch(200 * tick);
        timing.MarkSameSessionInfo(300 * tick);
        timing.MarkResolved(400 * tick);
        timing.MarkTerminalCatch(400 * tick);
        timing.MarkSameSessionInfo(400 * tick);
        return timing;
    }

    private static void AssertFirstEvent(CancelTiming timing, long tick)
    {
        var early = timing.Snapshot();
        if (early.Request != 100 * tick ||
            early.Resolved != 150 * tick || early.Terminal != 200 * tick || early.Reuse != 300 * tick ||
            CancelTiming.IntervalMs(early.Request, early.Resolved) != 50 ||
            CancelTiming.IntervalMs(early.Request, early.Terminal) != 100 ||
            CancelTiming.IntervalMs(early.Terminal, early.Resolved) != -50 ||
            CancelTiming.IntervalMs(early.Request, early.Reuse) != 200)
            throw new InvalidOperationException("Resolved-before-catch timing or first-event recording failed");
    }

    private static void AssertCallbackOrdering(
        (long Request, long Terminal, long Resolved, long Reuse) late,
        (long Request, long Terminal, long Resolved, long Reuse) equal,
        (long Request, long Terminal, long Resolved, long Reuse) missing, CancelTiming simultaneous)
    {
        if (CancelTiming.IntervalMs(late.Terminal, late.Resolved) != 50 ||
            CancelTiming.IntervalMs(equal.Terminal, equal.Resolved) != 0 ||
            !simultaneous.Format().Contains("terminalCatchToResolvedCallbackMs=0.000", StringComparison.Ordinal) ||
            CancelTiming.IntervalMs(missing.Request, missing.Terminal) is not null)
            throw new InvalidOperationException("Caught-before-resolved, equal, or missing timing failed");
    }

    internal static async Task<int> RunAsync(string mode, int serial)
    {
        try
        {
            return mode switch
            {
                "--active-cancel" => await PresenceAsync(serial, cancel: true),
                "--touch" => await PresenceAsync(serial, cancel: false),
                "--otp-info" => await OtpInfoAsync(serial),
                "--removal" => await RemovalAsync(serial),
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL mode={mode} serial={serial}: {ex}");
            return 1;
        }
        finally
        {
            await YubiKeyManager.ShutdownAsync();
        }
    }

    private static async Task<IYubiKey> SelectedAsync(int serial, ConnectionType type, bool forceRescan = false)
    {
        var devices = await YubiKeyManager.FindAllAsync(forceRescan: forceRescan);
        var matches = devices.Where(d => d.SerialNumber == serial && d.SupportsConnection(type)).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException($"Selected serial={serial} matched {matches.Length} {type} devices");
        return matches[0];
    }

    private static async Task PresenceAsyncOnSession(FidoSession session, PresencePrompt prompt, bool cancel,
        CancellationToken token, CancelTiming? timing)
    {
        if (cancel)
            await CancelSelectionAsync(session, prompt, token, timing);
        else
        {
            try { await session.SelectionAsync(token); }
            catch (CtapException ex) when (ex.Status == CtapStatus.InvalidCommand)
            {
                throw new NotSupportedException("BLOCKED: selected firmware does not support CTAP Selection", ex);
            }
        }

        RequirePairedPresence(prompt, cancel);

        var info = await session.GetInfoAsync();
        if (info.Versions.Count == 0)
            throw new InvalidOperationException("Same-session getInfo returned no versions");
        timing?.MarkSameSessionInfo();
        Console.WriteLine($"PHASE_SAME_SESSION_INFO serial={prompt.Serial}");
    }

    private static async Task CancelSelectionAsync(FidoSession session, PresencePrompt prompt,
        CancellationToken token, CancelTiming? timing)
    {
        var cancelled = false;
        try
        {
            await session.SelectionAsync(token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            timing?.MarkTerminalCatch();
            await prompt.CancellationTask;
            cancelled = true;
        }
        catch (CtapException ex) when (ex.Status == CtapStatus.InvalidCommand)
        {
            throw new NotSupportedException("BLOCKED: selected firmware does not support CTAP Selection", ex);
        }
        if (!cancelled)
            throw new InvalidOperationException("Selection succeeded despite cancellation");
    }

    private static void RequirePairedPresence(PresencePrompt prompt, bool cancel)
    {
        if (prompt.Requested.Count != 1 || prompt.Requested[0].Basis != UserPresenceBasis.DeviceWaiting ||
            prompt.Resolved.Count != 1 || !ReferenceEquals(prompt.Requested[0], prompt.Resolved[0].Context) ||
            prompt.Resolved[0].Outcome != (cancel ? UserPresenceOutcome.Cancelled : UserPresenceOutcome.Completed) ||
            prompt.Resolved[0].Token != CancellationToken.None)
            throw new InvalidOperationException("User presence request/resolution did not pair exactly once");
    }

    private static async Task<int> PresenceAsync(int serial, bool cancel)
    {
        var selected = await SelectedAsync(serial, ConnectionType.HidFido);
        using var operation = new CancellationTokenSource();
        if (!cancel)
            operation.CancelAfter(TimeSpan.FromSeconds(50));
        CancelTiming? timing = cancel ? new CancelTiming() : null;
        var prompt = new PresencePrompt(serial, cancel ? "ACTIVE_CANCEL" : "TOUCH_SELECTED",
            cancel ? () => { timing?.MarkCancelInvocation(); operation.Cancel(); } : null, timing);
        await using (var connection = await selected.ConnectAsync<IFidoHidConnection>())
        {
            if (connection.GetType().Name != "MacOSFidoHidConnection")
                throw new InvalidOperationException("Selected FIDO route is not the built-in macOS connection");
            await using (var session = await FidoSession.CreateAsync(connection,
                new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidFido, UserPresencePrompt = prompt }))
            {
                await PresenceAsyncOnSession(session, prompt, cancel, operation.Token, timing);
            }
        }

        await using (var reopened = await selected.ConnectAsync<IFidoHidConnection>())
        {
            if (reopened.GetType().Name != "MacOSFidoHidConnection")
                throw new InvalidOperationException("Reopened FIDO route is not built-in macOS");
            await using var session = await FidoSession.CreateAsync(reopened);
            if ((await session.GetInfoAsync()).Versions.Count == 0)
                throw new InvalidOperationException("Reopened getInfo returned no versions");
        }
        if (timing is not null)
            Console.WriteLine($"TIMING mode=active-cancel serial={serial} {timing.Format()}");
        Console.WriteLine($"PASS mode={(cancel ? "active-cancel" : "touch")} serial={serial} resolved=1 sameSessionInfo=1 reopenInfo=1");
        return 0;
    }

    private static async Task<int> OtpInfoAsync(int serial)
    {
        var selected = await SelectedAsync(serial, ConnectionType.HidOtp);
        for (var cycle = 1; cycle <= 3; cycle++)
        {
            await using (var connection = await selected.ConnectAsync<IOtpHidConnection>())
            {
                if (connection.GetType().Name != "MacOSOtpHidConnection")
                    throw new InvalidOperationException("Selected OTP route is not the built-in macOS connection");
                await using var session = await ManagementSession.CreateAsync(connection,
                    new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp });
                if (session.ConnectionType != ConnectionType.HidOtp)
                    throw new InvalidOperationException("Management selected a different transport");
                var info = await session.GetDeviceInfoAsync();
                if (info.SerialNumber != serial)
                    throw new InvalidOperationException("OTP device-info serial does not match selected serial");
                Console.WriteLine($"PHASE_OTP_INFO serial={serial} cycle={cycle} transport=HidOtp");
            }
        }
        Console.WriteLine($"PASS mode=otp-info serial={serial} cycles=3 deviceInfo=3");
        return 0;
    }

    private static async Task<int> RemovalAsync(int serial)
    {
        var selected = await SelectedAsync(serial, ConnectionType.HidFido);
        IFidoHidConnection connection = await selected.ConnectAsync<IFidoHidConnection>();
        bool observedAbsent = false;
        try
        {
            if (connection.GetType().Name != "MacOSFidoHidConnection")
                throw new InvalidOperationException("Selected FIDO route is not built-in macOS");
            // No command was sent on this fresh connection; this is a pending native input wait.
            Task<ReadOnlyMemory<byte>> reading = connection.ReceiveAsync();
            if (reading.IsCompleted)
                throw new InvalidOperationException("Raw receive completed before removal checkpoint");
            Console.WriteLine($"READY_UNPLUG serial={serial} utc={DateTimeOffset.UtcNow:O}");
            try
            {
                _ = await reading.WaitAsync(TimeSpan.FromSeconds(60));
                throw new InvalidOperationException("Raw receive returned a report instead of terminal removal");
            }
            catch (InvalidOperationException ex) when (ex.Message == "FIDO input terminated: 1")
            {
                // A disposal-induced terminal is not evidence of physical removal.
                var absent = await YubiKeyManager.FindAllAsync(forceRescan: true);
                if (absent.Any(d => d.SerialNumber == serial))
                    throw new InvalidOperationException("Selected device was not observed absent after terminal input");
                observedAbsent = true;
                Console.WriteLine($"PHASE_REMOVAL_TERMINAL serial={serial} exception={ex.GetType().Name} utc={DateTimeOffset.UtcNow:O}");
            }
        }
        finally
        {
            // A failed native close retains the claim; never treat that failure as safe reuse.
            await connection.DisposeAsync();
        }
        Console.WriteLine($"PHASE_REMOVAL_DISPOSED serial={serial}");

        // Require an observed absent snapshot before accepting a replacement, not just a cached device.
        if (!observedAbsent)
            throw new InvalidOperationException("Physical absence was not observed");
        Console.WriteLine($"READY_REPLUG serial={serial} utc={DateTimeOffset.UtcNow:O}");

        IYubiKey? replacement = null;
        using var rescan = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        while (!rescan.IsCancellationRequested)
        {
            var found = await YubiKeyManager.FindAllAsync(forceRescan: true, cancellationToken: rescan.Token);
            var matches = found.Where(d => d.SerialNumber == serial && d.SupportsConnection(ConnectionType.HidFido)).ToArray();
            if (matches.Length > 1)
                throw new InvalidOperationException("More than one matching replacement device");
            if (matches.Length == 1)
            {
                replacement = matches[0];
                break;
            }
            await Task.Delay(250, rescan.Token);
        }
        if (replacement is null || ReferenceEquals(replacement, selected))
            throw new InvalidOperationException("Fresh replacement generation was not observed");
        await using (var reopened = await replacement.ConnectAsync<IFidoHidConnection>())
        {
            if (reopened.GetType().Name != "MacOSFidoHidConnection")
                throw new InvalidOperationException("Replacement route is not built-in macOS");
            await using var session = await FidoSession.CreateAsync(reopened);
            if ((await session.GetInfoAsync()).Versions.Count == 0)
                throw new InvalidOperationException("Replacement getInfo returned no versions");
        }
        Console.WriteLine($"PASS mode=removal serial={serial} terminal=1 nativeDispose=1 freshGeneration=1 getInfo=1");
        return 0;
    }

    private sealed class PresencePrompt(int serial, string phase, Action? cancel, CancelTiming? timing) : IUserPresencePrompt
    {
        public int Serial { get; } = serial;
        public List<UserPresenceContext> Requested { get; } = [];
        public List<(UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken Token)> Resolved { get; } = [];
        public Task CancellationTask { get; private set; } = Task.CompletedTask;

        public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken)
        {
            if (cancel is not null && cancellationToken.IsCancellationRequested)
                throw new InvalidOperationException("Selection was cancelled before device-waiting notification");
            Requested.Add(context);
            Console.WriteLine($"{phase} serial={Serial} basis={context.Basis}");
            // Avoid running cancellation callbacks inside the presence prompt.
            if (cancel is not null)
                CancellationTask = Task.Run(cancel);
            return default;
        }

        public ValueTask OnUserPresenceResolvedAsync(UserPresenceContext context, UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            if (outcome == UserPresenceOutcome.Cancelled)
                timing?.MarkResolved();
            Resolved.Add((context, outcome, cancellationToken));
            Console.WriteLine($"PHASE_PRESENCE_RESOLVED serial={Serial} outcome={outcome}");
            return default;
        }
    }

    // Stopwatch timestamps are monotonic across the callback worker and the awaiting thread.
    // Zero denotes missing; each event is recorded at most once, even when callbacks race.
    private sealed class CancelTiming
    {
        private long _cancelInvocation;
        private long _terminalCatch;
        private long _resolved;
        private long _sameSessionInfo;

        public void MarkCancelInvocation() => MarkCancelInvocation(Stopwatch.GetTimestamp());
        public void MarkTerminalCatch() => MarkTerminalCatch(Stopwatch.GetTimestamp());
        public void MarkResolved() => MarkResolved(Stopwatch.GetTimestamp());
        public void MarkSameSessionInfo() => MarkSameSessionInfo(Stopwatch.GetTimestamp());

        public void MarkCancelInvocation(long tick) => RecordFirst(ref _cancelInvocation, tick);
        public void MarkTerminalCatch(long tick) => RecordFirst(ref _terminalCatch, tick);
        public void MarkResolved(long tick) => RecordFirst(ref _resolved, tick);
        public void MarkSameSessionInfo(long tick) => RecordFirst(ref _sameSessionInfo, tick);

        private static void RecordFirst(ref long field, long tick) => Interlocked.CompareExchange(ref field, tick, 0);

        public (long Request, long Terminal, long Resolved, long Reuse) Snapshot() =>
            (Volatile.Read(ref _cancelInvocation), Volatile.Read(ref _terminalCatch),
                Volatile.Read(ref _resolved), Volatile.Read(ref _sameSessionInfo));

        public string Format()
        {
            var (request, terminal, resolved, reuse) = Snapshot();
            return $"clock=Stopwatch ticksFrequencyHz={Stopwatch.Frequency} cancelInvokeTicks={Tick(request)} " +
                $"terminalCatchTicks={Tick(terminal)} resolvedCallbackTicks={Tick(resolved)} sameSessionInfoVerifiedTicks={Tick(reuse)} " +
                $"cancelInvokeToTerminalCatchMs={Interval(request, terminal)} " +
                $"cancelInvokeToResolvedCallbackMs={Interval(request, resolved)} " +
                $"terminalCatchToResolvedCallbackMs={Interval(terminal, resolved)} " +
                $"cancelInvokeToSameSessionInfoVerifiedMs={Interval(request, reuse)} " +
                "nativeOnlyMs=null nativeOnlyReason=managedCallbackAndSchedulingBoundariesNotNativeIsolation";
        }

        public static double? IntervalMs(long start, long end) => start == 0 || end == 0
            ? null : Stopwatch.GetElapsedTime(start, end).TotalMilliseconds;

        private static string Interval(long start, long end) => IntervalMs(start, end)?.ToString("F3", CultureInfo.InvariantCulture) ?? "null";

        private static string Tick(long value) => value == 0 ? "null" : value.ToString(CultureInfo.InvariantCulture);
    }
}
