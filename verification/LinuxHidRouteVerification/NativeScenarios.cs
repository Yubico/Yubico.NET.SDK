using System.Diagnostics;
using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.Hid.Linux;

namespace LinuxHidRouteVerification;

internal static class NativeScenarios
{
    internal static async Task<int> RunAsync(Evidence evidence)
    {
        Console.WriteLine("FIXTURE real libc/socketpair/eventfd/poll; synthetic FIDO64 metadata; NO hardware");
        Libc(evidence);
        await ReadinessAsync(evidence);
        await HeldCallbackAsync(evidence, false);
        await ReuseAsync(evidence);
        await OwnerAsync(evidence);
        return 0;
    }

    private static void Libc(Evidence evidence)
    {
        using LinuxFileSafeHandle missing = NativeMethods.Open("/definitely-no-linux-verification-device", NativeMethods.OpenFlags.O_RDONLY);
        int error = Marshal.GetLastPInvokeError();
        evidence.Check(missing.IsInvalid && error == 2, "libc-failed-open-negative-fd-and-errno-ENOENT");
        using LinuxEventFdSafeHandle wake = NativeMethods.EventFd(0, NativeMethods.EFD_NONBLOCK | NativeMethods.EFD_CLOEXEC);
        Span<byte> value = stackalloc byte[8];
        _ = BitConverter.TryWriteBytes(value, 7UL);
        evidence.Check(!wake.IsInvalid && NativeMethods.Write(wake, value) == 8, "libc-real-eventfd-write");
        value.Clear();
        evidence.Check(NativeMethods.Read(wake, value) == 8 && BitConverter.ToUInt64(value) == 7,
            "libc-real-eventfd-read");
        evidence.Check(wake.CloseOnce().Released, "libc-eventfd-checked-close");
        // Only this bounded verification child owns stdin. No parent/system descriptors change.
        evidence.Check(NativeMethods.CloseNative(0) == 0, "libc-fd-zero-fixture-child-stdin-closed");
        using LinuxFileSafeHandle zero = NativeMethods.Open("/dev/null", NativeMethods.OpenFlags.O_RDONLY);
        evidence.Check(zero.DangerousGetHandle() == IntPtr.Zero && !zero.IsInvalid && zero.CloseOnce().Released,
            "libc-real-fd-zero-valid-and-checked-close");
    }

    private static TaskCompletionSource<T> Completion<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task ReadinessAsync(Evidence evidence)
    {
        var native = new ObservedReadinessNative();
        await using var reactor = new LinuxHidReadReactor(native);
        using var sockets = new NativeDescriptors();
        var ready = Completion<long>();
        int calls = 0;
        var registration = reactor.Register(sockets.Input, 101, (generation, _) =>
        {
            Span<byte> input = stackalloc byte[64];
            nint count = NativeMethods.Read(sockets.Input, input);
            if (count == 64) { Interlocked.Increment(ref calls); ready.TrySetResult(generation); }
        }, error => ready.TrySetException(error));
        evidence.Check(await registration.Activation is null && !ready.Task.IsCompleted,
            "reactor-real-registration-pending-readiness");
        registration.Signal();
        // Explicit control wake cannot manufacture device data.
        evidence.Check(!ready.Task.IsCompleted, "reactor-explicit-eventfd-signal-not-data");
        sockets.Send(1);
        evidence.Check(await ready.Task.WaitAsync(TimeSpan.FromSeconds(5)) == 101 && calls == 1,
            "reactor-real-poll-readiness-generation");
        evidence.Check(registration.RequestRemoval() is null, "reactor-removal-eventfd-wake");
        await registration.Quiescence.WaitAsync(TimeSpan.FromSeconds(5));
        await ReleaseAsync(registration);
        evidence.Check(sockets.Input.CloseOnce().Released, "reactor-quiescence-owner-release-before-checked-close");
        // A second registration has no input. Reactor shutdown must wake an indefinite native poll.
        using var idle = new NativeDescriptors();
        var shutdownRegistration = reactor.Register(idle.Input, 102, (_, _) => { }, _ => { });
        evidence.Check(await shutdownRegistration.Activation is null, "reactor-idle-registration-activated");
        await evidence.MeasureAsync("reactor-shutdown", () => reactor.DisposeAsync().AsTask());
        evidence.Check(shutdownRegistration.Quiescence.IsCompletedSuccessfully, "reactor-shutdown-eventfd-wake-actual-quiescence");
        await ReleaseAsync(shutdownRegistration);
        evidence.Check(idle.Input.CloseOnce().Released, "reactor-shutdown-reference-release-checked-close");
        evidence.Check(native.Polls > 0 && native.Writes >= 3 && native.Reads > 0,
            "reactor-real-eventfd-control-syscalls-observed");
        Console.WriteLine($"REACTOR realPollCalls={native.Polls} realEventfdWrites={native.Writes} realEventfdReads={native.Reads}");
    }

    private static async Task HeldCallbackAsync(Evidence evidence, bool falseAcknowledgement)
    {
        await using var reactor = new LinuxHidReadReactor();
        using var sockets = new NativeDescriptors();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        int exited = 0;
        // Deliberately held INTERNAL assertion callback, never application callback behavior.
        var registration = reactor.Register(sockets.Input, 201, (_, _) =>
        {
            entered.Set();
            release.Wait();
            Span<byte> buffer = stackalloc byte[64];
            _ = NativeMethods.Read(sockets.Input, buffer);
            Volatile.Write(ref exited, 1);
        }, _ => { });
        try
        {
            if (await registration.Activation is not null) throw new IOException("Activation failed");
            sockets.Send(2);
            evidence.Check(entered.Wait(TimeSpan.FromSeconds(5)), "reactor-held-internal-read-callback-entered");
            _ = registration.RequestRemoval();
            Task acknowledgement = falseAcknowledgement ? Task.CompletedTask : registration.Quiescence;
            evidence.Check(!acknowledgement.IsCompleted && Volatile.Read(ref exited) == 0,
                "reactor-no-false-quiescence-while-callback-held");
            bool rejected = false;
            try { registration.ReleaseReference(); }
            catch (InvalidOperationException) { rejected = true; }
            evidence.Check(rejected, "reactor-early-reference-release-rejected");
        }
        finally
        {
            release.Set();
            _ = registration.RequestRemoval();
            await registration.Quiescence.WaitAsync(TimeSpan.FromSeconds(5));
            await ReleaseAsync(registration);
            evidence.Check(exited == 1 && sockets.Input.CloseOnce().Released,
                "reactor-held-callback-real-exit-quiescence-release-close");
        }
    }

    private static async Task ReuseAsync(Evidence evidence)
    {
        await using var reactor = new LinuxHidReadReactor();
        // Keep the same poller/wake descriptor alive while the data fd is closed and reused.
        using var sentinel = new NativeDescriptors();
        var keeper = reactor.Register(sentinel.Input, 300, (_, _) => { }, _ => { });
        if (await keeper.Activation is not null) throw new IOException("Sentinel failed");
        using var old = new NativeDescriptors();
        int descriptor = old.Input.DangerousGetHandle().ToInt32();
        int stale = 0;
        var predecessor = reactor.Register(old.Input, 301, (_, _) => Interlocked.Increment(ref stale), _ => { });
        if (await predecessor.Activation is not null) throw new IOException("Activation failed");
        _ = predecessor.RequestRemoval();
        await predecessor.Quiescence;
        await ReleaseAsync(predecessor);
        evidence.Check(old.Input.CloseOnce().Released, "fd-reuse-predecessor-close-proven");
        using var next = new NativeDescriptors();
        evidence.Check(next.Input.DangerousGetHandle().ToInt32() == descriptor, "fd-reuse-real-same-integer-descriptor");
        var ready = Completion<long>();
        var successor = reactor.Register(next.Input, 302, (generation, _) =>
        {
            Span<byte> buffer = stackalloc byte[64];
            if (NativeMethods.Read(next.Input, buffer) == 64) ready.TrySetResult(generation);
        }, error => ready.TrySetException(error));
        if (await successor.Activation is not null) throw new IOException("Activation failed");
        _ = predecessor.RequestRemoval(); // stale token must not remove the same-integer successor.
        await ReleaseAsync(predecessor);
        next.Send(3);
        evidence.Check(await ready.Task.WaitAsync(TimeSpan.FromSeconds(5)) == 302 && stale == 0,
            "fd-reuse-stale-token-generation-isolation");
        _ = successor.RequestRemoval();
        _ = keeper.RequestRemoval();
        await Task.WhenAll(successor.Quiescence, keeper.Quiescence);
        await ReleaseAsync(successor);
        await ReleaseAsync(keeper);
        evidence.Check(next.Input.CloseOnce().Released && sentinel.Input.CloseOnce().Released,
            "fd-reuse-successor-and-sentinel-drained-close");
    }

    private static async Task OwnerAsync(Evidence evidence)
    {
        await using var reactor = new LinuxHidReadReactor();
        using var sockets = new NativeDescriptors();
        using var sibling = new NativeDescriptors();
        using var api = new DescriptorReportApi(sockets) { HoldOutput = true };
        using var siblingApi = new DescriptorReportApi(sibling);
        await using var connection = await evidence.MeasureAsync("synthetic-owner-open", () =>
            LinuxFidoHidConnection.OpenAsync("synthetic-socket", api, reactor, CancellationToken.None));
        await using var input = await LinuxFidoHidConnection.OpenAsync("synthetic-sibling", siblingApi, reactor, CancellationToken.None);
        int callerThread = Environment.CurrentManagedThreadId;
        byte[] packet = new byte[64];
        packet.AsSpan().Fill(0x41);
        long start = Stopwatch.GetTimestamp();
        Task sending = connection.SendAsync(packet);
        double callerMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        try
        {
            evidence.Check(api.OutputEntered.Wait(TimeSpan.FromSeconds(5)), "owner-output-entered-report-seam");
            AssertResponsive(evidence, callerThread, api.OutputThread, sending);
            evidence.Check(api.PendingOutput == 1 && api.WriteCalls == 1, "owner-one-pending-output-no-replacement-worker");
            packet.AsSpan().Fill(0x7f); // Accepted output must already own its copy.
            Task<ReadOnlyMemory<byte>> receiving = input.ReceiveAsync();
            evidence.Check(!receiving.IsCompleted, "owner-real-native-input-await-is-pending");
            sibling.Send(0x52);
            ReadOnlyMemory<byte> report = await receiving.WaitAsync(TimeSpan.FromSeconds(5));
            evidence.Check(report.Length == 64 && report.Span.IndexOfAnyExcept((byte)0x52) < 0 && !sending.IsCompleted,
                "owner-held-output-cannot-stall-other-input-progress");
        }
        finally { api.OutputRelease.Set(); }
        await sending.WaitAsync(TimeSpan.FromSeconds(5));
        evidence.Measurements.Add(new("synthetic-held-output", callerMs, Stopwatch.GetElapsedTime(start).TotalMilliseconds, null));
        byte[] framed = new byte[65];
        evidence.Check(NativeMethods.Read(sockets.Peer, framed) == 65 && framed[0] == 0 &&
            framed.AsSpan(1).IndexOfAnyExcept((byte)0x41) < 0, "owner-owned-output-copy-production-L1-zero-ID-framing-no-drop");
        await QueueAsync(input, sibling, evidence);
        await CancelAndDrainInputAsync(input, sibling, siblingApi, evidence);
        await connection.DisposeAsync();
        evidence.Check(api.Release.Released && api.CloseThread == api.OutputThread,
            "owner-original-output-worker-performs-checked-close");
        evidence.NativeProbes.Add(new("synthetic-descriptor-production-L1-report-call-excludes-held-seam",
            api.ReadCalls + siblingApi.ReadCalls, api.WriteCalls, 1, api.OutputThread, api.CloseThread,
            api.NativeWriteTicks * 1000.0 / Stopwatch.Frequency));
        Console.WriteLine($"NATIVE probe readCalls={api.ReadCalls + siblingApi.ReadCalls} writeCalls={api.WriteCalls} maxPendingOutput=1 outputWorker={api.OutputThread} closeWorker={api.CloseThread}");
        using var reopenedSockets = new NativeDescriptors();
        using var reopenedApi = new DescriptorReportApi(reopenedSockets);
        await using (var reopened = await LinuxFidoHidConnection.OpenAsync("synthetic-reopen", reopenedApi, reactor, CancellationToken.None))
        {
            Task<ReadOnlyMemory<byte>> reading = reopened.ReceiveAsync();
            reopenedSockets.Send(0x62);
            evidence.Check((await reading).Span[0] == 0x62, "owner-after-pending-dispose-reopen-real-reactor-read");
            await IdleAsync(evidence);
        }
        evidence.Check(reopenedApi.Release.Released, "owner-reopened-checked-close");
    }

    private static async Task CancelAndDrainInputAsync(LinuxFidoHidConnection input, NativeDescriptors sibling,
        DescriptorReportApi siblingApi, Evidence evidence)
    {
        using var cancel = new CancellationTokenSource();
        Task<ReadOnlyMemory<byte>> pending = input.ReceiveAsync(cancel.Token);
        cancel.Cancel();
        bool cancelled = false;
        try { _ = await pending; }
        catch (OperationCanceledException) { cancelled = true; }
        evidence.Check(cancelled, "owner-pending-reader-cancel-detaches");
        sibling.Send(0x61);
        evidence.Check((await input.ReceiveAsync()).Span[0] == 0x61, "owner-cancel-recovery-real-reactor");
        Task<ReadOnlyMemory<byte>> disposingRead = input.ReceiveAsync();
        evidence.Check(!disposingRead.IsCompleted, "owner-dispose-read-was-pending");
        await evidence.MeasureAsync("synthetic-owner-dispose", () => input.DisposeAsync().AsTask());
        evidence.Check(disposingRead.IsCompleted, "owner-dispose-terminalizes-real-pending-read");
        bool terminal = false;
        try { _ = await disposingRead; }
        catch (ObjectDisposedException) { terminal = true; }
        catch (InvalidOperationException) { terminal = true; }
        evidence.Check(terminal && siblingApi.Release.Released && siblingApi.CloseThread != 0,
            "owner-actual-quiescence-worker-release-checked-close");
    }

    private static async Task QueueAsync(LinuxFidoHidConnection connection, NativeDescriptors sockets, Evidence evidence)
    {
        // Marker payloads remain owned and ordered; later reports cannot overwrite earlier returned memory.
        var retained = new List<ReadOnlyMemory<byte>>();
        for (byte marker = 1; marker <= 40; marker++) sockets.Send(marker);
        for (byte marker = 1; marker <= 40; marker++)
            retained.Add(await connection.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5)));
        evidence.Check(retained.Count == 40 && retained.Select((report, index) =>
            report.Length == 64 && report.Span.IndexOfAnyExcept((byte)(index + 1)) < 0).All(value => value),
            "owner-40-owned-input-buffers-no-drop-reorder-or-reuse");
    }

    private static async Task IdleAsync(Evidence evidence)
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        int threads = process.Threads.Count;
        TimeSpan cpu = process.TotalProcessorTime;
        long allocated = GC.GetTotalAllocatedBytes();
        var timer = Stopwatch.StartNew();
        await Task.Delay(250);
        process.Refresh();
        evidence.Idle.Add(new("whole-process-with-one-live-synthetic-owner-and-reactor",
            timer.Elapsed.TotalMilliseconds, (process.TotalProcessorTime - cpu).TotalMilliseconds,
            process.Threads.Count - threads, GC.GetTotalAllocatedBytes() - allocated));
    }

    private static void AssertResponsive(Evidence evidence, int caller, int native, Task operation) =>
        evidence.Check(caller != native && !operation.IsCompleted, "owner-report-entry-off-caller-and-returned-task-still-pending");

    internal static async Task<int> BlockingControlAsync(Evidence evidence)
    {
        // The real retained legacy adapter enters GetReport before returning its Task (L0 control).
        // A held assertion fixture makes this visible without relying on a speed threshold.
        using var report = new BlockingReportConnection();
        await using var legacy = new FidoHidConnection(report);
        var invocation = Completion<Task<ReadOnlyMemory<byte>>>();
        int caller = 0;
        var thread = new Thread(() =>
        {
            caller = Environment.CurrentManagedThreadId;
            try { invocation.SetResult(legacy.ReceiveAsync()); }
            catch (Exception error) { invocation.SetException(error); }
        })
        { IsBackground = true, Name = "Linux verification blocking sensitivity caller" };
        thread.Start();
        try
        {
            if (!report.Entered.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Blocking fixture did not enter");
            AssertResponsive(evidence, caller, report.EntryThread, invocation.Task);
        }
        finally
        {
            report.Release.Set();
            _ = await await invocation.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (!thread.Join(TimeSpan.FromSeconds(5))) throw new TimeoutException("Own caller did not drain");
        }
        return 0;
    }

    internal static async Task<int> QuiescenceControlAsync(Evidence evidence)
    {
        await HeldCallbackAsync(evidence, true);
        return 0;
    }

    private static Task ReleaseAsync(ILinuxHidReadRegistration registration)
    {
        var completion = Completion<int>();
        new Thread(() =>
        {
            try { registration.ReleaseReference(); completion.SetResult(0); }
            catch (Exception error) { completion.SetException(error); }
        })
        { IsBackground = true, Name = "Linux verification reference owner" }.Start();
        return completion.Task;
    }
}