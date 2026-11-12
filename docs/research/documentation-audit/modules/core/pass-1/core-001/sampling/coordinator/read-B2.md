PROPOSED SAMPLE — no verdict: core-001:unit:80c8b4e5e4742b49 / B2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/ILinuxHidReadReactor.cs:69–72
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>Release exactly once on the native owner worker after Quiescence; never on a reactor thread.</summary>
    /// <remarks>Early, concurrent, and reactor-thread calls throw. Repeated completed release is a no-op.</remarks>
    void ReleaseReference();


PROPOSED SAMPLE — no verdict: core-001:unit:ffa89fbb1f0e0006 / B2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxFidoHidConnection.cs:70–87
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <inheritdoc cref="IFidoHidConnection.ReceiveAsync" />
    /// <remarks>
    ///     Successful receipt transfers an exact-length array to the caller through a read-only view; the
    ///     connection no longer owns or clears it, including on disposal. The view does not make the backing
    ///     storage immutable or provide automatic sensitive-data cleanup. The caller owns its lifetime and
    ///     clearing of sensitive contents. Cancellation detaches only this reader; native input remains active.
    ///     A non-64-byte report terminates input rather than returning a partial typed packet.
    ///     For these built-in array-backed results, use <c>MemoryMarshal.TryGetArray</c> to obtain the owned
    ///     segment, then <c>CryptographicOperations.ZeroMemory(segment.AsSpan())</c> after use. Clearing a
    ///     separate copy does not clear the original. Do not apply this ownership assumption to arbitrary
    ///     custom-provider results without that provider's contract.
    /// </remarks>
    /// <param name="cancellationToken">Cancels this pending receive, not the device's protocol exchange.</param>
    /// <returns>A task yielding 64 caller-owned payload bytes, with no synthetic report-ID prefix.</returns>
    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
        _owner.ReceiveAsync(true, cancellationToken);


PROPOSED SAMPLE — no verdict: core-001:unit:b4a0cd296ec54259 / B2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxFidoHidNativeOwner.cs:1–22
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using NativeMethods = Yubico.YubiKit.Core.Native.Linux.Libc.NativeMethods;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>Owns the shared native lifetime behind typed Linux FIDO and direct synchronous IO-report access.</summary>
/// <remarks>
/// Roots the owner before native acquisition and retains the original physical claim through
/// cleanup. One dedicated worker owns open, metadata, output, activation/drain waits and checked
/// close; the reactor only performs bounded nonblocking input. Send and receive share one ordinary
/// operation slot with no backlog. Terminal tasks are not native-release evidence: only actual
/// registration quiescence followed by checked close permits claim release. Unknown release retains
/// and quarantines the rooted lifetime, without a replacement worker, close retry or timeout escape.
/// </remarks>



PROPOSED SAMPLE — no verdict: core-001:unit:03d13df41d843868 / B2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxFidoHidNativeOwner.cs:65–67
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>Gets bounded descriptor output metadata (1..4096); raw sends need not match this length.</summary>
    internal int OutputReportSize { get; private set; }


PROPOSED SAMPLE — no verdict: core-001:unit:5c0f333d06ef9126 / B2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidDeviceListener.cs:54–67
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Creates a new instance with a custom event source factory. Used by tests to inject a
    /// fake <see cref="ILinuxHidEventSource"/> for no-hardware fault injection.
    /// </summary>
    /// <param name="eventSourceFactory">
    /// Factory invoked once per <see cref="Start"/> to create the session's event source,
    /// or null to use the udev-backed production source.
    /// </param>
    internal LinuxHidDeviceListener(Func<ILinuxHidEventSource>? eventSourceFactory)
    {
        _eventSourceFactory = eventSourceFactory ?? (static () => new LinuxUdevHidEventSource());
    }


PROPOSED SAMPLE — no verdict: core-001:unit:7a90c15f7fb4a767 / B2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs:55–66
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


/// <summary>
/// Abstraction over the native udev monitor, poll(), and shutdown eventfd used by
/// <see cref="LinuxHidDeviceListener"/>. Enables no-hardware fault-injection tests of the
/// listener's loop policy (bounded exit, error transitions, hint emission).
/// </summary>
/// <remarks>
/// Ownership: the listener session's thread owns the source and disposes it when the loop
/// exits. <see cref="SignalShutdown"/> may be called concurrently from other threads and
/// must tolerate a source that is concurrently disposing.
/// </remarks>



PROPOSED SAMPLE — no verdict: core-001:unit:2ab3cc1db2ed4269 / B2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs:86–92
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Wakes a thread blocked in <see cref="WaitForEvent"/> so it can observe cancellation.
    /// Safe to call from any thread, including concurrently with <see cref="IDisposable.Dispose"/>.
    /// </summary>
    void SignalShutdown();


PROPOSED SAMPLE — no verdict: core-001:unit:ad5d17565598504e / B2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs:328–347
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Trims the hidraw-specific suffix from a hidraw syspath so the remaining prefix
    /// identifies the parent HID device stably across hidraw node renumbering.
    /// </summary>
    internal static string TrimHidrawSyspath(string syspath)
    {
        const string hidrawSegment = "/hidraw/";
        var hidrawIndex = syspath.LastIndexOf(hidrawSegment, StringComparison.Ordinal);
        if (hidrawIndex >= 0)
        {
            return syspath[..hidrawIndex];
        }

        const string hidrawDirectorySuffix = "/hidraw";
        return syspath.EndsWith(hidrawDirectorySuffix, StringComparison.Ordinal)
            ? syspath[..^hidrawDirectorySuffix.Length]
            : syspath;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:e49c9459b92a84fe / B2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidIOReportConnection.cs:47–55
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>Synchronously waits for native open, report-size discovery and input activation.</summary>
    /// <remarks>Failure waits for cleanup of partially acquired native state; there is no constructor timeout.</remarks>
    /// <param name="devNode">The hidraw device-node path.</param>
    /// <param name="api">The borrowed native report seam; this connection does not dispose it.</param>
    /// <param name="reactor">The borrowed readiness reactor; disposal removes only this lifetime's registration.</param>
    internal LinuxHidIOReportConnection(string devNode, ILinuxHidReportApi api, ILinuxHidReadReactor reactor) =>
        _owner = LinuxFidoHidNativeOwner.OpenAsync(devNode, api, reactor, CancellationToken.None).GetAwaiter().GetResult();


PROPOSED SAMPLE — no verdict: core-001:unit:619f21dd8869ecc6 / B2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidIOReportConnection.cs:118–130
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>Stops admission and asynchronously observes the same native shutdown as <see cref="Dispose" />.</summary>
    /// <remarks>
    ///     Waits for held output, actual reactor quiescence and checked close. Close diagnostics can fault
    ///     disposal after proven release; unproven release retains the native owner instead.
    /// </remarks>
    /// <returns>The shared shutdown outcome, potentially pending indefinitely while native drain is held.</returns>
    public async ValueTask DisposeAsync()
    {
        await _owner.ShutdownAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:7075351c8f5cf706 / B2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidReadReactor.cs:478–480
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        /// <summary>Gets the bounded internal readiness mechanics, never an application event handler.</summary>
        internal Action<long, short> OnReady { get; }


PROPOSED SAMPLE — no verdict: core-001:unit:d882128459cef5b6 / B2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidReportApi.cs:128–132
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <inheritdoc />
    public void SetFeature(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload) =>
        SendFramedReport(handle, payload, feature: true);


PROPOSED SAMPLE — no verdict: core-001:unit:c165b37fa4483b00 / B2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxOtpHidConnection.cs:19–22
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>Gets the eight-byte OTP payload size, excluding the native zero report-ID prefix.</summary>
    public int FeatureReportSize => 8;


PROPOSED SAMPLE — no verdict: core-001:unit:1683ba031d3bcddb / B2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxOtpHidConnection.cs:25–49
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>Opens a native feature-report lifetime on its dedicated worker.</summary>
    /// <remarks>
    ///     Cancellation requests shutdown; failed or canceled opens await cleanup rather than detaching held
    ///     native work. The native owner accepts the physical claim even on failure and retains it if native
    ///     release cannot be proven.
    /// </remarks>
    /// <param name="path">The hidraw device-node path.</param>
    /// <param name="reportApi">The borrowed native report seam; this connection does not dispose it.</param>
    /// <param name="cancellationToken">Requests cancellation of open and cleanup of acquired native state.</param>
    /// <param name="physicalClaim">An optional claim transferred to the native owner, not retained by the caller.</param>
    /// <returns>The caller-owned connection, which must be disposed, preferably asynchronously.</returns>
    internal static async Task<LinuxOtpHidConnection> OpenAsync(string path, ILinuxHidReportApi reportApi,
        CancellationToken cancellationToken, IDisposable? physicalClaim = null)
    {
        var owner = await LinuxOtpHidNativeOwner.OpenAsync(path, reportApi, cancellationToken, physicalClaim).ConfigureAwait(false);
        try { return new LinuxOtpHidConnection(owner); }
        catch (Exception constructionError)
        {
            try { await owner.ShutdownAsync().ConfigureAwait(false); }
            catch (Exception releaseError) { throw new AggregateException(constructionError, releaseError); }
            throw;
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:bbc2caa0424d0694 / B2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxOtpHidConnection.cs:49–68
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <inheritdoc cref="IOtpHidConnection.SendAsync" />
    /// <remarks>
    ///     The owner copies the eight payload bytes before publishing work to its worker and clears that copy
    ///     before task completion. Native framing adds a zero report-ID byte. The caller retains its source
    ///     memory and must clear sensitive input after the task is terminal; the SDK never clears that source.
    ///     Cancellation before dispatch skips SET; after dispatch the task waits for native completion.
    ///     The caller remains responsible for OTP sequencing and must not interleave session or raw I/O.
    /// </remarks>
    /// <param name="report">Exactly eight caller-owned payload bytes, without a native report-ID prefix.</param>
    /// <param name="cancellationToken">Cancels SET only before native dispatch.</param>
    /// <returns>A task terminal only after any dispatched SET returns and its staging copy is cleared.</returns>
    public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return report.Length != 8
            ? throw new ArgumentException("OTP feature reports must contain exactly 8 bytes.", nameof(report))
            : _owner.SendAsync(report, cancellationToken);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:93ecc26f4e0dd820 / B2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxOtpHidNativeOwner.cs:47–83
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>Creates a rooted owner and opens its descriptor on the original dedicated worker.</summary>
    /// <param name="path">hidraw path to open.</param>
    /// <param name="api">Borrowed synchronous report seam; not disposed by this owner.</param>
    /// <param name="token">Checked before/after native open; cancellation requests shutdown but cannot detach submitted open.</param>
    /// <param name="claim">Optional original physical/discovery claim transferred to this lifetime, including failed-open cleanup.</param>
    /// <returns>The opened owner; failed/canceled preparation awaits cleanup before propagating its error.</returns>
    /// <remarks>Preserves both open and cleanup failures. A held open/close can keep this task and the original claim pending indefinitely.</remarks>
    internal static async Task<LinuxOtpHidNativeOwner> OpenAsync(string path, ILinuxHidReportApi api,
        CancellationToken token, IDisposable? claim = null)
    {
        LinuxOtpHidNativeOwner owner;
        try { owner = new LinuxOtpHidNativeOwner(api, claim); }
        catch (Exception constructionError)
        {
            try { claim?.Dispose(); }
            catch (Exception releaseError) { throw new AggregateException(constructionError, releaseError); }
            throw;
        }
        try
        {
            using var cancellation = token.UnsafeRegister(_ => owner.RequestShutdown(), null);
            await owner.StartOpen(path, token).ConfigureAwait(false);
            return owner;
        }
        catch (Exception openError)
        {
            try { await owner.ShutdownAsync().ConfigureAwait(false); }
            catch (Exception cleanupError)
            {
                if (ReferenceEquals(openError, cleanupError)) throw;
                throw new AggregateException(openError, cleanupError);
            }
            throw;
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:07d157697b0b7bf6 / B2 / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxOtpHidNativeOwner.cs:315–323
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>Requests shutdown for facade finalization without waiting for native work or propagating exceptions.</summary>
    /// <remarks>Takes the admission lock; does not promise lock-free invocation or acknowledge release.</remarks>
    internal void RequestShutdown()
    {
        try { _ = ShutdownAsync(); }
        catch { /* Nonthrowing finalizer backstop with no drain wait; never assert native release. */ }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:2e88f97d2c6a2093 / B2 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/ILinuxHidReportApi.cs:63–65
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>Nonblocking read returned EAGAIN; no payload was delivered.</summary>
    WouldBlock,


PROPOSED SAMPLE — no verdict: core-001:unit:01b6c348853851b0 / B2 / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/ILinuxHidReportApi.cs:69–71
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>Read failed with another captured native error.</summary>
    Error



PROPOSED SAMPLE — no verdict: core-001:unit:452d5a7d1e85fa15 / B2 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/LinuxHidRouteVerification/DescriptorReportApi.cs:17–18
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    internal int ReadCalls { get; private set; }


PROPOSED SAMPLE — no verdict: core-001:unit:91b29ba69cecef54 / B2 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/LinuxHidRouteVerification/DescriptorReportApi.cs:39–40
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public void GetFeature(LinuxFileSafeHandle handle, Span<byte> payload) => throw new NotSupportedException("Not an OTP fixture");


PROPOSED SAMPLE — no verdict: core-001:unit:f687d465a7684888 / B2 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/LinuxHidRouteVerification/DescriptorReportApi.cs:41–47
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public LinuxReleaseOutcome CloseOnce(LinuxFileSafeHandle handle)
    {
        CloseThread = Environment.CurrentManagedThreadId;
        Release = _real.CloseOnce(handle);
        return Release;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:c90c88a41999d609 / B2 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/LinuxHidRouteVerification/Evidence.cs:15–16
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public string Baseline { get; } = "3de42bb54c5e8c986364f0f04398d4e85a66a596";


PROPOSED SAMPLE — no verdict: core-001:unit:f9d1a1faca427e57 / B2 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/LinuxHidRouteVerification/HardwareScenarios.cs:201–223
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static async Task<int> ExpertFeatureAsync(IYubiKey selected, int keyCount, int serial, Evidence evidence)
    {
        _ = selected;
        IHidInterface? hid = await InterfaceAsync(keyCount, HidInterfaceType.Otp);
        if (hid is null) return 2;
        for (int cycle = 1; cycle <= 3; cycle++)
        {
            await using IHidConnection native = hid.ConnectToFeatureReports();
            evidence.Check(native is LinuxHidFeatureReportConnection && native.InputReportSize == 8 && native.OutputReportSize == 8,
                $"direct-feature-Linux-metadata-{cycle}");
            byte[] report = native.GetReport();
            evidence.Check(report.Length == 8, $"direct-feature-GET8-{cycle}");
            await using var adapter = new OtpHidConnection(native);
            await using ManagementSession session = await ManagementSession.CreateAsync(adapter,
                new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp });
            DeviceInfo info = await session.GetDeviceInfoAsync();
            evidence.Check(info.SerialNumber == serial, $"direct-feature-selected-info-{cycle}");
            Console.WriteLine($"FIRMWARE route=LinuxHidFeatureReportConnection firmware={info.FirmwareVersion}");
        }
        return 0;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:1bfd7941724527c9 / B2 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/LinuxHidRouteVerification/NativeDescriptors.cs:8–9
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    internal LinuxFileSafeHandle Input { get; }


PROPOSED SAMPLE — no verdict: core-001:unit:0d9113578140c285 / B2 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/LinuxHidRouteVerification/NativeScenarios.cs:160–216
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


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


PROPOSED SAMPLE — no verdict: core-001:unit:219683fbfc255c91 / B2 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/LinuxHidRouteVerification/NativeScenarios.cs:252–267
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


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


PROPOSED SAMPLE — no verdict: core-001:unit:00d808339ba79b32 / B2 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/LinuxHidRouteVerification/OperatorScenarios.cs:15–21
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    internal static async Task<int> RunAsync(string mode, IYubiKey selected, int serial, Evidence evidence)
    {
        if (mode == "--removal") await RemovalAsync(selected, serial, evidence);
        else await PresenceAsync(selected, mode == "--active-cancel", evidence);
        return 0;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:369bda1d0cd58318 / B2 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/LinuxHidRouteVerification/OperatorScenarios.cs:77–90
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static async Task<IYubiKey> ReplacementAsync(int serial)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        while (true)
        {
            IReadOnlyList<IYubiKey> devices = await YubiKeyManager.FindAllAsync(cancellationToken: deadline.Token);
            IYubiKey[] matches = [.. devices.Where(device => device.SerialNumber == serial && device.SupportsConnection(ConnectionType.HidFido))];
            if (matches.Length > 1) throw new InvalidOperationException("Ambiguous replacement");
            if (matches.Length == 1) return matches[0];
            await Task.Delay(250, deadline.Token);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:299ddffe24179413 / B2 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/LinuxHidRouteVerification/evidence_gate.py:1–72
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
"""Shared result contract for local capture and Linux CI. No device I/O."""

REQUIRED_NATIVE_CHECKS = frozenset("""
libc-failed-open-negative-fd-and-errno-ENOENT
libc-real-eventfd-write
libc-real-eventfd-read
libc-eventfd-checked-close
libc-fd-zero-fixture-child-stdin-closed
libc-real-fd-zero-valid-and-checked-close
reactor-real-registration-pending-readiness
reactor-explicit-eventfd-signal-not-data
reactor-real-poll-readiness-generation
reactor-removal-eventfd-wake
reactor-quiescence-owner-release-before-checked-close
reactor-idle-registration-activated
reactor-shutdown-eventfd-wake-actual-quiescence
reactor-shutdown-reference-release-checked-close
reactor-real-eventfd-control-syscalls-observed
reactor-held-internal-read-callback-entered
reactor-no-false-quiescence-while-callback-held
reactor-early-reference-release-rejected
reactor-held-callback-real-exit-quiescence-release-close
fd-reuse-predecessor-close-proven
fd-reuse-real-same-integer-descriptor
fd-reuse-stale-token-generation-isolation
fd-reuse-successor-and-sentinel-drained-close
owner-output-entered-report-seam
owner-report-entry-off-caller-and-returned-task-still-pending
owner-one-pending-output-no-replacement-worker
owner-real-native-input-await-is-pending
owner-held-output-cannot-stall-other-input-progress
owner-owned-output-copy-production-L1-zero-ID-framing-no-drop
owner-40-owned-input-buffers-no-drop-reorder-or-reuse
owner-pending-reader-cancel-detaches
owner-cancel-recovery-real-reactor
owner-dispose-read-was-pending
owner-dispose-terminalizes-real-pending-read
owner-actual-quiescence-worker-release-checked-close
owner-original-output-worker-performs-checked-close
owner-after-pending-dispose-reopen-real-reactor-read
owner-reopened-checked-close
""".split())


def validate_result(result, kind, mode, exit_code, source_hash):
    if kind not in ("jit", "aot") or result.get("DynamicCode") is not (kind == "jit"):
        raise ValueError(f"DynamicCode mismatch for {kind}")
    if result.get("Mode") != mode or result.get("SourceHash") != source_hash:
        raise ValueError("mode/source label mismatch (label is not build attestation)")
    checks = result.get("Checks")
    if not isinstance(checks, list) or not all(isinstance(item, str) and item for item in checks):
        raise ValueError("named checks must be nonempty strings")
    if len(set(checks)) != len(checks):
        raise ValueError("duplicate named checks")
    if mode.startswith("--sensitivity-"):
        blocking = mode == "--sensitivity-blocking"
        expected = "owner-report-entry-off-caller-and-returned-task-still-pending" if blocking else "reactor-no-false-quiescence-while-callback-held"
        cleanup = [] if blocking else ["reactor-held-internal-read-callback-entered", "reactor-held-callback-real-exit-quiescence-release-close"]
        failure = result.get("Failure")
        if (exit_code != 1 or result.get("Outcome") != "FAILED" or checks != cleanup or
                not isinstance(failure, str) or not failure.startswith("System.InvalidOperationException: " + expected + "\n")):
            raise ValueError("exact intended negative assertion/cleanup contract")
        return 1
    if mode == "--list" and exit_code == 2 and result.get("Outcome") == "BLOCKED":
        if checks or result.get("Failure") is not None:
            raise ValueError("blocked enumeration contract")
        return 2
    if exit_code != 0 or result.get("Outcome") != "PASSED" or not checks or result.get("Failure") is not None:
        raise ValueError("positive result contract: PASSED and nonempty named checks required")
    if mode == "--native" and set(checks) != REQUIRED_NATIVE_CHECKS:
        raise ValueError("native required named checks mismatch")
    return 0



PROPOSED SAMPLE — no verdict: core-001:unit:ede5800cd9c43e80 / B2 / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/LinuxHidRouteVerification/test_evidence_gates.py:1–439
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
#!/usr/bin/env python3
"""Fail-closed result/provenance regressions; no device operations or source mutations."""

import copy
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

import capture
import publish_contract
from evidence_gate import REQUIRED_NATIVE_CHECKS, validate_result
from publish_contract import seal, sha, verify_publish, STAMP

RECORDS = Path(sys.argv.pop(1)) if len(sys.argv) > 1 else None


class RuntimeGateTests(unittest.TestCase):
    def setUp(self):
        self.jit = json.loads((RECORDS / "jit-native.json").read_text())
        self.aot = json.loads((RECORDS / "aot-native.json").read_text())

    def test_real_jit_record_cannot_be_substituted_for_aot(self):
        with self.assertRaisesRegex(ValueError, "DynamicCode"):
            validate_result(self.jit, "aot", "--native", 0, self.jit["SourceHash"])

    def test_real_aot_record_cannot_be_substituted_for_jit(self):
        with self.assertRaisesRegex(ValueError, "DynamicCode"):
            validate_result(self.aot, "jit", "--native", 0, self.aot["SourceHash"])

    def test_real_native_records_pass_their_own_runtime(self):
        for kind, record in (("jit", self.jit), ("aot", self.aot)):
            validate_result(record, kind, "--native", 0, record["SourceHash"])

    def test_missing_required_named_check_is_rejected(self):
        record = copy.deepcopy(self.aot)
        record["Checks"].remove(next(iter(REQUIRED_NATIVE_CHECKS)))
        with self.assertRaisesRegex(ValueError, "named checks"):
            validate_result(record, "aot", "--native", 0, record["SourceHash"])

    def test_invalid_positive_records_are_rejected(self):
        for field, value in (("Checks", []), ("Outcome", "FAILED"), ("DynamicCode", None),
                             ("DynamicCode", "false"), ("DynamicCode", 0), ("Mode", "--list"),
                             ("SourceHash", "unbound-label")):
            with self.subTest(field=field, value=value):
                record = copy.deepcopy(self.aot)
                record[field] = value
                with self.assertRaises(ValueError):
                    validate_result(record, "aot", "--native", 0, self.aot["SourceHash"])

    def test_duplicate_check_is_rejected(self):
        record = copy.deepcopy(self.aot)
        record["Checks"].append(record["Checks"][0])
        with self.assertRaisesRegex(ValueError, "duplicate"):
            validate_result(record, "aot", "--native", 0, record["SourceHash"])

    def test_real_intended_negative_assertions_and_cleanup_are_preserved(self):
        for kind in ("jit", "aot"):
            for mode in ("--sensitivity-blocking", "--sensitivity-quiescence"):
                result = json.loads((RECORDS / f"{kind}-{mode[2:]}.json").read_text())
                self.assertEqual(1, validate_result(result, kind, mode, 1, result["SourceHash"]))
                with self.assertRaises(ValueError):
                    validate_result(result, kind, mode, 3, result["SourceHash"])
                result["Failure"] = "System.InvalidOperationException: some other failure\n"
                with self.assertRaisesRegex(ValueError, "exact intended negative"):
                    validate_result(result, kind, mode, 1, result["SourceHash"])


class PublishBindingTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="linux-hid-gates-", dir="/tmp/opencode" if Path("/tmp/opencode").is_dir() else None)
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.source = self.root / "Fixture.cs"
        self.source.write_text("// original synthetic source\n")
        for kind in ("jit", "aot"):
            folder = self.root / kind
            folder.mkdir()
            binary = folder / "LinuxHidRouteVerification"
            binary.write_bytes(b"synthetic executable artifact identity, not executed")
            binary.chmod(0o700)
        (self.root / "jit/LinuxHidRouteVerification.dll").write_bytes(b"synthetic assembly identity")
        self.before = self.inputs()
        seal(self.root, self.before, self.inputs())

    def inputs(self):
        return {"source": {"sourceHash": sha(self.source)}, "effectiveBuild": {"DefineConstants": "TRACE"},
                "dependencies": {"nativeArchiveSha256": "synthetic-package-identity"}}

    def assert_capture_refuses_before_operation(self, reason):
        # Exercise capture's real entry point, resolving a real stale/missing filesystem stamp.
        # Only the location provider is redirected to this isolated fixture. No source/artifact
        # in the working checkout is modified, and no verification child may start.
        with patch.object(sys, "argv", ["capture.py", "--native-only"]), \
             patch.object(capture, "verify_publish", lambda: verify_publish(self.root, self.inputs())), \
             patch.object(capture.subprocess, "run") as child, \
             patch.object(capture, "hardware_snapshot") as snapshot:
            with self.assertRaisesRegex(ValueError, reason):
                capture.main()
            child.assert_not_called()
            snapshot.assert_not_called()

    def test_unchanged_source_and_artifacts_pass(self):
        self.assertEqual(self.before, verify_publish(self.root, self.inputs())["inputs"])

    def test_stale_source_is_rejected_before_execution(self):
        self.source.write_text("// edited after publish\n")
        with self.assertRaisesRegex(ValueError, "stale publish source"):
            verify_publish(self.root, self.inputs())
        self.assert_capture_refuses_before_operation("stale publish source")

    def test_stale_binary_is_rejected(self):
        (self.root / "aot/LinuxHidRouteVerification").write_bytes(b"replaced executable")
        with self.assertRaisesRegex(ValueError, "stale published binary"):
            verify_publish(self.root, self.inputs())
        self.assert_capture_refuses_before_operation("stale published binary")

    def test_stale_jit_assembly_is_rejected(self):
        (self.root / "jit/LinuxHidRouteVerification.dll").write_bytes(b"replaced assembly")
        with self.assertRaisesRegex(ValueError, "stale published binary"):
            verify_publish(self.root, self.inputs())

    def test_missing_publish_manifest_is_rejected(self):
        (self.root / STAMP).unlink()
        with self.assertRaisesRegex(ValueError, "missing publish manifest"):
            verify_publish(self.root, self.inputs())
        self.assert_capture_refuses_before_operation("missing publish manifest")

    def test_source_change_during_publish_cannot_be_sealed(self):
        (self.root / STAMP).unlink()
        self.source.write_text("// changed during publish\n")
        with self.assertRaisesRegex(ValueError, "inputs changed during publish"):
            seal(self.root, self.before, self.inputs())
        self.assertFalse((self.root / STAMP).exists())

    def test_effective_build_change_is_rejected(self):
        current = self.inputs()
        current["effectiveBuild"]["DefineConstants"] = "DIFFERENT"
        with self.assertRaisesRegex(ValueError, "effective inputs"):
            verify_publish(self.root, current)

    def test_package_identity_change_is_rejected(self):
        current = self.inputs()
        current["dependencies"]["nativeArchiveSha256"] = "different-package-identity"
        with self.assertRaisesRegex(ValueError, "effective inputs"):
            verify_publish(self.root, current)

    def test_extra_publish_artifact_is_rejected(self):
        (self.root / "jit/unexpected.dll").write_bytes(b"unbound assembly")
        with self.assertRaisesRegex(ValueError, "stale published binary"):
            verify_publish(self.root, self.inputs())

    def test_publish_input_difference_names_category_without_printing_values(self):
        after = self.inputs()
        after["dependencies"]["nativeArchiveSha256"] = "do-not-print-this-value"
        with self.assertRaisesRegex(ValueError, "INPUT dependencies/nativeArchiveSha256") as error:
            seal(self.root, self.before, after)
        self.assertNotIn("do-not-print-this-value", str(error.exception))
        self.assertFalse((self.root / STAMP).exists())


class EvaluatedImportTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="linux-hid-imports-", dir="/tmp/opencode" if Path("/tmp/opencode").is_dir() else None)
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.props = self.root / "Fixture.nuget.g.props"
        self.targets = self.root / "Fixture.nuget.g.targets"
        self.props.write_text("<Project><PropertyGroup><DefineConstants>INITIAL</DefineConstants></PropertyGroup></Project>")
        self.targets.write_text("<Project><Target Name='Unused'/></Project>")
        (self.root / "Fixture.proj").write_text(
            "<Project><Import Project='Fixture.nuget.g.props'/><Import Project='Fixture.nuget.g.targets'/></Project>")
        os.utime(self.root / "Fixture.proj", (999999999, 999999999))
        for name, value in (("ROOT", self.root), ("PROJECT", "Fixture.proj"), ("consumed_projects", lambda: ("Fixture.proj",))):
            patcher = patch.object(publish_contract, name, value)
            patcher.start()
            self.addCleanup(patcher.stop)
        os.utime(self.props, (1000000000, 1000000000))
        os.utime(self.targets, (1000000001, 1000000001))

    def test_restore_timestamp_reordering_does_not_change_consumed_imports(self):
        before = publish_contract.effective_build_inputs()
        os.utime(self.props, (1000000002, 1000000002))
        self.assertEqual(before, publish_contract.effective_build_inputs())

    def test_non_newest_import_content_change_is_bound(self):
        before = publish_contract.effective_build_inputs()
        self.props.write_text(self.props.read_text().replace("INITIAL", "DIFFERENT"))
        os.utime(self.props, (1000000000, 1000000000))
        after = publish_contract.effective_build_inputs()
        for kind in ("jit", "aot"):
            self.assertNotEqual(before[kind]["Fixture.proj"]["importedFileHashes"], after[kind]["Fixture.proj"]["importedFileHashes"])

    def test_generated_restore_import_initialization_is_not_silently_accepted(self):
        before = publish_contract.effective_build_inputs()
        extra = self.root / "RestoredPackage.targets"
        extra.write_text("<Project><Target Name='FromPackage'/></Project>")
        self.targets.write_text("<Project><Import Project='RestoredPackage.targets'/></Project>")
        after = publish_contract.effective_build_inputs()
        self.assertNotEqual(before, after)
        for kind in ("jit", "aot"):
            self.assertIn(str(extra), after[kind]["Fixture.proj"]["importedFileHashes"])


class RestoreContextTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="linux-hid-restore-", dir="/tmp/opencode" if Path("/tmp/opencode").is_dir() else None)
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        patcher = patch.object(publish_contract, "ROOT", self.root)
        patcher.start()
        self.addCleanup(patcher.stop)
        (self.root / "Directory.Packages.props").write_text("<Project><YubiKitVersion>fixture</YubiKitVersion></Project>")
        self.package = self.root / "packages/fixture/1.0"
        self.package.mkdir(parents=True)
        (self.package / "native.a").write_bytes(b"original native dependency")
        self.assets = []
        projects = [publish_contract.PROJECT] + [f"src/{name}/src/Yubico.YubiKit.{name}.csproj" for name in ("Core", "Ctap2", "Management")]
        for project in projects:
            value = {"libraries": {"Fixture/1.0": {"type": "package", "path": "fixture/1.0"}},
                     "packageFolders": {str(self.root / "packages"): {}}, "targets": {"fixture": {}}}
            for kind in ("jit", "aot"):
                path = self.root / f"artifacts/linux-hid-verification/build/{kind}/obj/{Path(project).stem}/project.assets.json"
                path.parent.mkdir(parents=True)
                path.write_text(json.dumps(value))
                self.assets.append(path)
            legacy = self.root / Path(project).parent / "obj/project.assets.json"
            legacy.parent.mkdir(parents=True)
            legacy.write_text(json.dumps(value))

    def test_both_prepared_restore_contexts_are_bound(self):
        actual = publish_contract.package_inputs()
        self.assertEqual({"jit", "aot"}, set(actual["resolvedClosure"]))
        self.assertEqual(4, len(actual["resolvedClosure"]["jit"]))
        self.assertEqual(4, len(actual["resolvedClosure"]["aot"]))

    def test_consumed_assets_change_without_identity_change_is_bound(self):
        before = publish_contract.package_inputs()
        assets = json.loads(self.assets[0].read_text())
        assets["targets"]["fixture"] = {"differentCompileAsset": {}}
        self.assets[0].write_text(json.dumps(assets))
        self.assertNotEqual(before, publish_contract.package_inputs())

    def test_real_package_content_change_is_bound(self):
        before = publish_contract.package_inputs()
        (self.package / "native.a").write_bytes(b"replaced native dependency")
        self.assertNotEqual(before, publish_contract.package_inputs())

    def test_unprepared_runtime_context_is_rejected(self):
        self.assets[0].unlink()
        with self.assertRaises(FileNotFoundError):
            publish_contract.package_inputs()


class ReferencedProjectImportTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="linux-hid-reference-imports-", dir="/tmp/opencode" if Path("/tmp/opencode").is_dir() else None)
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        patcher = patch.object(publish_contract, "ROOT", self.root)
        patcher.start()
        self.addCleanup(patcher.stop)
        self.host = publish_contract.PROJECT
        self.references = [f"src/{name}/src/Yubico.YubiKit.{name}.csproj" for name in ("Core", "Ctap2", "Management")]
        (self.root / "Directory.Build.props").write_text(
            "<Project><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>")
        (self.root / "Directory.Packages.props").write_text("<Project><PropertyGroup><YubiKitVersion>fixture</YubiKitVersion></PropertyGroup></Project>")
        package = self.root / "packages/fixture/1.0"
        package.mkdir(parents=True)
        (package / "native.a").write_bytes(b"unchanged package content")
        self.generated = []
        for project in [self.host] + self.references:
            path = self.root / project
            path.parent.mkdir(parents=True)
            references = self.references if project == self.host else self.references[:1] if project != self.references[0] else []
            items = "".join(f"<ProjectReference Include='{self.root / reference}'/>" for reference in references)
            path.write_text(f"<Project Sdk='Microsoft.NET.Sdk'><ItemGroup>{items}</ItemGroup></Project>")
            for kind in ("jit", "aot"):
                obj = self.root / f"artifacts/linux-hid-verification/build/{kind}/obj/{path.stem}"
                obj.mkdir(parents=True)
                (obj / "project.assets.json").write_text(json.dumps({
                    "libraries": {"Fixture/1.0": {"type": "package", "path": "fixture/1.0"}},
                    "packageFolders": {str(self.root / "packages"): {}}, "targets": {"fixture": {}}}))
                for suffix in ("props", "targets"):
                    imported = obj / f"{path.name}.nuget.g.{suffix}"
                    imported.write_text("<Project><PropertyGroup><DefineConstants>ORIGINAL</DefineConstants></PropertyGroup></Project>")
                    if project != self.host:
                        self.generated.append(imported)
        self.publish = self.root / "published"
        for kind in ("jit", "aot"):
            folder = self.publish / kind
            folder.mkdir(parents=True)
            binary = folder / "LinuxHidRouteVerification"
            binary.write_bytes(b"synthetic executable identity, never executed")
            binary.chmod(0o700)
        (self.publish / "jit/LinuxHidRouteVerification.dll").write_bytes(b"synthetic assembly identity")

    def inputs(self):
        return {"source": {"sourceHash": "unchanged-fixture-source"},
                "dependencies": publish_contract.package_inputs(), "effectiveBuild": publish_contract.effective_build_inputs()}

    def test_each_referenced_generated_import_change_rejects_seal_and_verification(self):
        before = self.inputs()
        for imported in self.generated:
            with self.subTest(imported=imported.relative_to(self.root)):
                seal(self.publish, before, before)
                original = imported.read_bytes()
                try:
                    # Preserve evaluated properties too: this changes only an imported target's content.
                    imported.write_bytes(original.replace(b"</Project>", b"<!-- changed consumed import --> </Project>"))
                    after = self.inputs()
                    self.assertEqual(before["source"], after["source"])
                    self.assertEqual(before["dependencies"], after["dependencies"])
                    with self.assertRaisesRegex(ValueError, "stale publish source/effective inputs"):
                        verify_publish(self.publish, after)
                    with self.assertRaisesRegex(ValueError, "INPUT effectiveBuild"):
                        seal(self.publish, before, after)
                    self.assertFalse((self.publish / STAMP).exists())
                finally:
                    imported.write_bytes(original)

    def test_referenced_import_timestamp_reordering_does_not_change_binding(self):
        before = publish_contract.effective_build_inputs()
        for index, imported in enumerate(self.generated):
            os.utime(imported, (1000000000 + index, 1000000000 + index))
        self.assertEqual(before, publish_contract.effective_build_inputs())

    def test_host_and_references_use_their_actual_sdk_build_context(self):
        actual = publish_contract.effective_build_inputs()
        for kind, aot in (("jit", "false"), ("aot", "true")):
            self.assertEqual({self.host, *self.references}, set(actual[kind]))
            self.assertEqual("linux-x64", actual[kind][self.host]["Properties"]["RuntimeIdentifier"])
            self.assertEqual("true", actual[kind][self.host]["Properties"]["SelfContained"])
            for project in self.references:
                properties = actual[kind][project]["Properties"]
                self.assertEqual("", properties["RuntimeIdentifier"])
                self.assertEqual("", properties["SelfContained"])
                self.assertEqual("Release", properties["Configuration"])
                self.assertEqual(aot, properties["PublishAot"])
                self.assertEqual(str(self.root / f"artifacts/linux-hid-verification/build/{kind}"), properties["ArtifactsPath"])


class CapturePackageProvenanceTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="linux-hid-capture-", dir="/tmp/opencode" if Path("/tmp/opencode").is_dir() else None)
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        (self.root / "Directory.Packages.props").write_text(
            "<Project><YubiKitVersion>fixture</YubiKitVersion><PackageVersion Include='Yubico.NativeShims' Version='1.18.1'/></Project>")
        self.publish = self.root / "artifacts/linux-hid-verification"
        for kind in ("jit", "aot"):
            folder = self.publish / kind
            folder.mkdir(parents=True)
            binary = folder / "LinuxHidRouteVerification"
            binary.write_bytes(b"fixture executable identity, never executed")
            binary.chmod(0o700)
        (self.publish / "jit/LinuxHidRouteVerification.dll").write_bytes(b"fixture assembly")
        self.identity = "Yubico.NativeShims/9.8.7-fixture"
        self.package_files = {}
        for name in ("fixture.nupkg", "runtimes/linux-x64/native/libfixture.so",
                     "static/linux-x64/libfixture.a", "runtimes/osx-arm64/native/libfixture.dylib"):
            path = self.root / "non-default-package-cache" / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(f"fixture bound package file: {name}".encode())
            self.package_files[name] = sha(path)
        self.inputs = {"source": {"sourceHash": sha(self.root / "Directory.Packages.props")},
                       "dependencies": {"sdkPackageVersion": "fixture", "resolvedClosure": {
                           kind: {"src/Core/src/Yubico.YubiKit.Core.csproj": [self.identity]} for kind in ("jit", "aot")},
                           "packageFiles": {self.identity: self.package_files}}}
        self.reseal()

    def reseal(self):
        seal(self.publish, self.inputs, self.inputs)

    def run_capture(self):
        def child(args, **kwargs):
            kind = Path(args[0]).parent.name
            mode = args[1]
            record = json.loads((RECORDS / f"{kind}-{mode[2:]}.json").read_text())
            record["SourceHash"] = self.inputs["source"]["sourceHash"]
            Path(args[-1]).write_text(json.dumps(record))
            return capture.subprocess.CompletedProcess(args, 0 if mode == "--native" else 1, "", "")

        with patch.object(sys, "argv", ["capture.py", "--native-only"]), \
             patch.object(capture, "ROOT", self.root), \
             patch.object(capture, "verify_publish", lambda: verify_publish(self.publish, self.inputs)), \
             patch.object(capture, "command", return_value={"fixture": "diagnostic not executed"}), \
             patch.object(capture, "hardware_snapshot", return_value={}), \
             patch.object(capture.subprocess, "run", side_effect=child) as run, \
             patch("builtins.print"):
            try:
                outcome = capture.main()
            except ValueError:
                run.assert_not_called()
                raise
            self.assertEqual(0, outcome)
            self.assertEqual(6, run.call_count)
        records = next((self.publish / "evidence").iterdir())
        return json.loads((records / "provenance.json").read_text())

    def assert_bound_package_provenance(self, actual):
        self.assertEqual([self.identity], actual["resolvedNativeShims"])
        self.assertEqual("1.18.1", actual["nativeShimsPin"])
        expected = {f"packages/{self.identity}/{name}": value for name, value in self.package_files.items()
                    if not name.endswith(".dylib")}
        self.assertEqual(expected, {name: value for name, value in actual["artifacts"].items()
                                    if name.startswith("packages/")})
        self.assertNotIn(str(self.root / "non-default-package-cache"), json.dumps(actual))

    def test_standalone_capture_without_default_core_assets_uses_bound_package(self):
        self.assertFalse((self.root / "src/Core/src/obj/project.assets.json").exists())
        self.assert_bound_package_provenance(self.run_capture())

    def test_stale_default_core_assets_cannot_override_bound_package(self):
        legacy = self.root / "src/Core/src/obj/project.assets.json"
        legacy.parent.mkdir(parents=True)
        legacy.write_text(json.dumps({"libraries": {"Yubico.NativeShims/0.0.0-stale": {}}}))
        self.assert_bound_package_provenance(self.run_capture())

    def test_missing_runtime_package_closure_fails_before_child_execution(self):
        self.inputs["dependencies"]["resolvedClosure"]["jit"]["src/Core/src/Yubico.YubiKit.Core.csproj"] = []
        self.reseal()
        with self.assertRaisesRegex(ValueError, "NativeShims.*jit"):
            self.run_capture()

    def test_missing_bound_package_content_fails_before_child_execution(self):
        self.inputs["dependencies"]["packageFiles"] = {}
        self.reseal()
        with self.assertRaisesRegex(ValueError, "NativeShims.*content"):
            self.run_capture()


if __name__ == "__main__":
    if RECORDS is None:
        raise SystemExit("Usage: test_evidence_gates.py CAPTURE_DIRECTORY")
    unittest.main()



PROPOSED SAMPLE — no verdict: core-001:unit:4a4f4fb2ee3e00b0 / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/ILinuxHidReadReactor.cs:25–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal interface ILinuxHidReadReactor : IAsyncDisposable
{


PROPOSED SAMPLE — no verdict: core-001:unit:9599c37631e3b2ce / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxFidoHidNativeOwner.cs:34–35
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private readonly Queue<byte[]> _reports = new();


PROPOSED SAMPLE — no verdict: core-001:unit:f31714ee80fcdd9c / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxFidoHidNativeOwner.cs:509–512
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static Exception? Combine(Exception? first, Exception? second) =>
        first is null ? second : second is null ? first : new AggregateException(first, second);


PROPOSED SAMPLE — no verdict: core-001:unit:d8e54703866180d9 / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidDeviceListener.cs:218–264
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private void ProcessUdevEvent(ListenerSession session)
    {
        try
        {
            var udevEvent = session.Source.ReceiveEvent();
            if (udevEvent is null)
            {
                // Receive failures matter most during event storms (e.g. ENOBUFS after the kernel
                // dropped notifications) — exactly when a removal may have been lost. Never
                // suppress: hint a rescan so discovery re-syncs with reality.
                Logger.LogWarning("Failed to receive udev event; emitting unknown-change rescan hint");
                OnDeviceEvent(HidDeviceRescanHint.Unknown);
                return;
            }

            switch (udevEvent.Action)
            {
                case "add":
                    HandleDeviceAdd(session, udevEvent);
                    break;

                case "remove":
                    HandleDeviceRemove(udevEvent);
                    break;

                case null:
                    Logger.LogDebug("udev event missing action; emitting unknown-change rescan hint");
                    OnDeviceEvent(HidDeviceRescanHint.Unknown);
                    break;

                default:
                    // Recognized non-topology actions ("change", "bind", ...) do not affect discovery.
                    Logger.LogTrace(
                        "Ignoring udev action '{Action}' for {Identity}",
                        udevEvent.Action,
                        udevEvent.StableIdentity);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to process udev event; emitting unknown-change rescan hint");
            OnDeviceEvent(HidDeviceRescanHint.Unknown);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:a5d65fe0ab80f2ee / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidDeviceListener.cs:343–344
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class ListenerSession
    {


PROPOSED SAMPLE — no verdict: core-001:unit:df36d6f917e324ff / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidDeviceListener.cs:352–354
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public Thread? Thread { get; set; }


PROPOSED SAMPLE — no verdict: core-001:unit:2ec84f79a016d9ee / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs:27–28
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal enum LinuxHidPollOutcome
{


PROPOSED SAMPLE — no verdict: core-001:unit:5a9e64bc80430a1f / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidFeatureReportConnection.cs:18–19
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal sealed class LinuxHidFeatureReportConnection : IHidConnection
{


PROPOSED SAMPLE — no verdict: core-001:unit:75027cec36d20d7e / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidInterface.cs:31–33
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public string ReaderName => _devNode;


PROPOSED SAMPLE — no verdict: core-001:unit:9922abc84a69e7ef / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidReadReactor.cs:31–32
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private readonly Lock _gate = new();


PROPOSED SAMPLE — no verdict: core-001:unit:82e49d4a6292a59a / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidReadReactor.cs:134–145
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private void Signal(Registration registration)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(registration.Removing, registration);
            Exception? error = _run is { Retiring: false } run ? TrySignal(run) : _failure;
            if (error is not null)
                throw new IOException("Read reactor wake failed; quiescence is not acknowledged.", error);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:ca89fa5459284c88 / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidReadReactor.cs:290–310
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private void Dispatch(Registration[] snapshot, NativeMethods.PollFd[] descriptors, int offset)
    {
        int dispatched = 0;
        for (int step = 0; step < snapshot.Length && dispatched < BatchSize; step++)
        {
            int index = (offset + step) % snapshot.Length;
            var registration = snapshot[index];
            short flags = descriptors[index + 1].revents;
            lock (_gate)
            {
                if (_shutdown || _failure is not null)
                    return;
                if (registration.Removing || flags == 0)
                    continue;
            }
            dispatched++;
            DispatchOne(registration, flags);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:11c91ef6840e9deb / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidReadReactor.cs:408–423
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private void StopRegistrations()
    {
        Registration[] terminal;
        lock (_gate)
        {
            terminal = _failure is null ? [] : [.. _registrations.Where(item => !item.Removing)];
            foreach (var registration in terminal)
                registration.Removing = true;
        }
        foreach (var registration in terminal)
            NotifyTerminal(registration, _failure ?? new IOException("Read reactor stopped."));
        lock (_gate)
            QuiesceRemoved();
    }


PROPOSED SAMPLE — no verdict: core-001:unit:9d959fcda5cb4947 / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxHidReportApi.cs:175–182
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static void RequireComplete(LinuxIoResult result, int expected, string operation)
    {
        if (result.Count != expected)
            throw new PlatformApiException(operation, result.Count < 0 ? result.Error : 0,
                $"Linux HID report transfer incomplete: expected {expected} native bytes, got {result.Count}.");
    }


PROPOSED SAMPLE — no verdict: core-001:unit:6bcbf73be9feb50f / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxOtpHidConnection.cs:14–15
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal sealed class LinuxOtpHidConnection : IOtpHidConnection
{


PROPOSED SAMPLE — no verdict: core-001:unit:8eb6258e1c70c357 / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxOtpHidNativeOwner.cs:23–24
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private readonly IDisposable? _claim;


PROPOSED SAMPLE — no verdict: core-001:unit:0c159721aed9ae3b / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxOtpHidNativeOwner.cs:25–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private readonly AutoResetEvent _signal = new(false);


PROPOSED SAMPLE — no verdict: core-001:unit:feddcabc6205e907 / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxOtpHidNativeOwner.cs:30–31
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private Action? _work;


PROPOSED SAMPLE — no verdict: core-001:unit:43d291b5ded400cc / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxOtpHidNativeOwner.cs:176–195
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private void StopAfterWorkerFailure(Exception failure)
    {
        lock (_sync)
        {
            _stopping = true;
            _workerFailure ??= failure;
            _work = null;
            if (_outputCopy is not null) CryptographicOperations.ZeroMemory(_outputCopy);
            if (_inputCopy is not null) CryptographicOperations.ZeroMemory(_inputCopy);
            _outputCopy = null;
            _inputCopy = null;
            _active = false;
            _ = (_workCompletion?.TrySetException(failure));
            _ = (_reader?.TrySetException(failure));
            _workCompletion = null;
            _reader = null;
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:7b5c151d9a3ebbdc / B2 / lifetime-protocol-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Transports/Hid/Linux/LinuxOtpHidNativeOwner.cs:223–241
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private void Send(byte[] copy, CancellationToken token, TaskCompletionSource completion)
    {
        Exception? error = null;
        bool cancelled = token.IsCancellationRequested;
        try { if (!cancelled) _api.SetFeature(Handle, copy); }
        catch (Exception ex) { error = ex; }
        finally { CryptographicOperations.ZeroMemory(copy); }
        lock (_sync)
        {
            _outputCopy = null;
            _workCompletion = null;
            _active = false;
            if (cancelled) _ = completion.TrySetCanceled(token);
            else if (error is not null) _ = completion.TrySetException(error);
            else _ = completion.TrySetResult();
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:95ad4b56865f1456 / B2 / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/research/linux-fido-async-lifetime.md:78–93
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
### Unexpected worker-loop failure

The owner tracks accepted open/send completion and its owned output copy independently of the
dispatch delegate, including after that delegate is removed for dispatch. An outer loop failure
atomically closes admission, faults the actual pending public read and open/send tasks, clears
queued output, and requests reactor removal under the same owner lock. An already-terminal
operation is not retroactively changed. The still-running original worker then executes the same
true-Quiescence / ReleaseReference / checked-close path; no replacement or cleanup job is created.

If native cleanup succeeds, the original claim and root are released but disposal still faults
with the **original loop exception**. Additional wake, close or release errors preserve that cause.
Failed/interrupted cleanup quarantines uncertain ownership, without retry or cross-thread close.
The signal is retired under the admission lock after stopping is visible, serializing retirement
with every accepted setter. Subsequent disposal observes the same completion, not a disposed-event
exception. Finalizer RequestShutdown contains a nonthrowing backstop and cannot assert release.




PROPOSED SAMPLE — no verdict: core-001:unit:b37c3e449550734a / B2 / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/research/linux-managed-acceptance.md:9–30
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Independent gates

* `AdapterContractRegistry` / `adapter-contracts.v1.json`: **43 required operation rows / 76 runnable
  method links**, up from 27/49. The **16 new Linux rows / 27 links** cover typed FIDO/OTP
  open/send/receive/dispose (8), and direct synchronous IO/feature open/get/set/dispose (8).
  Production owner and declared symbol resolution is separate from required role/test resolution.
  Linux exclusion is replaced only for these rows; Windows HID remains deferred/rejected.
* Existing macOS, listener and portable PC/SC requirements are preserved. Missing Linux operation,
  missing/empty role, wrong role, stale owner/test and duplicate-profile checks fail closed. Sixteen
  explicit Linux row-deletion cases test the independently required list, including every profile role.
* `PublicSyncBoundaryRegistry`: **24 required public reachability entries**, up from 16; eight new
  retained Linux synchronous paths map public interfaces independently to Linux implementation owners.
  Eight explicit deletion cases prevent silent shrinkage. These rows link reachability and existing
  behavioral evidence; they do not execute profiles recursively or attest native release.
* No shipping boundary site changed. The reviewed source manifest remains **219 sites / 132 imports /
  37 waits / 18 scheduling sites**, documented/outstanding. No source-site status was promoted.

`LinuxDirectFacadeAcceptanceTests` adds 6 concrete behaviors: partial IO-open metadata failure, feature
open failure, held IO write, held feature SET, IO true-quiescence disposal, and held feature close with
shared disposal. Existing direct-facade tests supply descriptor-sized IO and fixed-eight-byte feature
GET/metadata evidence. Direct SET preserves expert lengths. These direct opens still take no grouped claim.




PROPOSED SAMPLE — no verdict: core-001:unit:cfea2933573b757f / B2 / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/research/linux-native-acceptance.md:411–446
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Hardware and operator gap

The final read-only sysfs snapshot finds Yubico `1050:0407`, product
`YubiKey OTP+FIDO+CCID`, USB `3-1.3`, with interfaces `1.0`, `1.1`, `1.2` all bound
to **usbfs**. `/dev/hidraw0` and `/dev/hidraw5` are absent again; existing hidraw1–4
are inaccessible to this user and were not selected/opened arbitrarily. This differs
from L0's earlier usbhid snapshot. JIT and AOT SDK discovery both return zero keys.
**No positive serial, firmware, physical transport path, hardware operation, or
hardware reopen is verified (physical path checks = 0).** No system remediation
was attempted.

Built, **not executed**, serial-selected read-only modes:

| Mode | Intended physical evidence, still pending |
|---|---|
| `--fido` | Public typed factory → actual Linux FIDO owner; three CTAP INIT/GetInfo/open/close cycles; two sequential borrowed sessions with idle reuse; pending raw read/dispose/reopen |
| `--otp` | Actual Linux OTP owner; three typed feature GET8 / Management device-info / close/reopen cycles |
| `--raw` | Public selected-device raw CTAP session factory, GetInfo and reopen over three cycles |
| `--expert-io` | Uniquely associated public interface → Linux direct I/O facade; indefinite pending GetReport woken by disposal from another thread; descriptor metadata and INIT/GetInfo/reopen |
| `--expert-feature` | Linux direct feature facade; unchanged 8-byte metadata, GET8 and selected Management info, three cycles |
| `--smartcard` | Unchanged shared scenario: context/connection open, BeginTransaction, select/read Management info, EndTransaction, dispose/reopen, three cycles |

Direct modes refuse an ambiguous association when multiple keys/interfaces exist.
Linux typed routes assert the real Linux owner type; direct modes deliberately use
the retained synchronous compatibility adapter over the actual Linux facade, not
macOS classes or substitute typed-route evidence. No six-second macOS input timeout
or macOS-specific terminal reason is assumed.

`--touch`, `--active-cancel` and `--removal` are also compiled but **unexecuted**.
They refuse to run unless `LINUX_VERIFY_OPERATOR_COORDINATED=1` is deliberately set
after separate coordination. Touch/cancel use read-only CTAP Selection, paired
presence/recovery/GetInfo, never provisioning. Removal requires terminal input
**before disposal**, observed physical absence and a fresh serial-matched generation
before reopen/GetInfo. Merely timing out cannot pass. The capture script never runs
these modes, even if the environment flag is set.




PROPOSED SAMPLE — no verdict: core-001:unit:6612f3e188e74e2b / B2 / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/research/linux-native-report-access.md:1–7
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
# Linux native report access — L1

Scope: native ABI, SafeHandle release evidence, and a synchronous borrowed-report seam.
Linux production HID remains synchronous. No reactor, FIDO/OTP lifetime worker, claim-transfer
protocol, native-AOT acceptance row, or universal Linux verification is introduced here.
The accepted [L0 baseline](linux-async-boundaries-baseline.md) remains separate.




PROPOSED SAMPLE — no verdict: core-001:unit:094e7f926bf428c5 / B2 / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/research/linux-otp-async-lifetime.md:8–30
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Exact seam and ownership

```csharp
internal static Task<LinuxOtpHidConnection> OpenAsync(
    string path, ILinuxHidReportApi reportApi, CancellationToken cancellationToken,
    IDisposable? physicalClaim = null);
```

`LinuxOtpHidConnection : IOtpHidConnection` has FeatureReportSize8 and Type HidOtp. Its private
`LinuxOtpHidNativeOwner` also backs `LinuxHidFeatureReportConnection`. The owner roots itself with
GCHandle before accepting the claim/acquiring native state; it does not root a facade. One dedicated
background thread starts only for open, without flowing the opening caller's ExecutionContext.
That same worker performs `open(O_RDWR|O_CLOEXEC)`, feature GET/SET and checked close. There is one
ordinary operation slot, no queue/backlog, per-call Task.Run, replacement worker, generic command
framework or reuse of the FIDO reactor owner. No feature ioctl runs on the read reactor.

Construction/thread-start failure before native acquisition releases only the original supplied
claim; failed claim release retains the root where an owner exists. Open exceptions/cancellation
await owner cleanup and preserve initial and cleanup failures. Cancellation while open is held
cannot release the claim before a later acquired descriptor is checked-closed. A fake L1 returning
an invalid SafeHandle is rejected: dispose that no-descriptor handle without native close, then
release the claim. It cannot be mistaken for an acquired descriptor requiring release evidence.




PROPOSED SAMPLE — no verdict: core-001:unit:ebf81d7eeea93827 / B2 / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:docs/research/linux-otp-async-lifetime.md:95–108
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## L5 handoff and remaining limits

L5 must select this internal OpenAsync from the typed Linux OTP factory and transfer real grouped
connection/discovery claims before acquisition. Direct expert opens still acquire no implicit grouped
claim. No public `ConnectAsync<IOtpHidConnection>` responsiveness claim follows from L4 alone.
Preserve this owner and exact original claim through late open cancellation and checked close; do
not wrap it in a per-operation worker or release a registration solely because DisposeAsync faulted.

The report seam remains synchronous and borrowed. A genuinely hung feature ioctl/open/close can
keep the sole worker and shutdown pending indefinitely. There is no native abort guarantee, hardware
latency bound, starvation benchmark, musl claim, selected-key reopen measurement or native-AOT run.
Thread construction/start failure is handled in production but not fault-injected with an extra
production scheduler seam. Fake tests do not establish device/kernel timing or whole-Linux safety.




PROPOSED SAMPLE — no verdict: core-001:unit:dd174674513eff1e / B2 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Linux/Libc/Libc.Interop.cs:1–20
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

using System.Runtime.InteropServices;

namespace Yubico.YubiKit.Core.Native.Linux.Libc;

// glibc libc.so.6 ABI. Raw descriptor ints are hidden behind retained SafeHandle
// wrappers; these synchronous entry points do not establish worker ownership.



PROPOSED SAMPLE — no verdict: core-001:unit:27680597d9ca147d / B2 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Linux/Libc/Libc.Interop.cs:25–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    public enum OpenFlags
    {


PROPOSED SAMPLE — no verdict: core-001:unit:19339d4b89793206 / B2 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Linux/Libc/Libc.Interop.cs:86–93
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static int Poll(PollFd[] fds, int nfds, int timeout)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(nfds);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nfds, fds.Length);
        return PollNative(fds, (nuint)nfds, timeout);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:0118113189c88a86 / B2 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Linux/Libc/Libc.Interop.cs:110–111
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public const long HIDIOCGRDESCSIZE = 0x0000000080044801;


PROPOSED SAMPLE — no verdict: core-001:unit:988ca0b5bd1e1aa4 / B2 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Linux/Libc/Libc.Interop.cs:125–126
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public const int MaxFeatureBufferSize = 256;


PROPOSED SAMPLE — no verdict: core-001:unit:c7edeed9fe6d2e6c / B2 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Linux/Libc/LibcFcntlConstants.cs:1–18
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

#pragma warning disable CA1707

namespace Yubico.YubiKit.Core.Native.Linux.Libc;




PROPOSED SAMPLE — no verdict: core-001:unit:d7636cd1bd712128 / B2 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Linux/Libc/LinuxDescriptorLease.cs:34–44
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // libudev owns its monitor fd; retain that SafeHandle without treating its native
    // pointer as an int descriptor. The source owner supplies the associated fd.
    public LinuxDescriptorLease(SafeHandle owner, int descriptor)
    {
        bool acquired = false;
        owner.DangerousAddRef(ref acquired);
        _handle = owner;
        Descriptor = descriptor;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:3404421c88096564 / B2 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Linux/Libc/LinuxDescriptorLease.cs:44–53
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public void Dispose()
    {
        // The last borrower can execute close here. Do not overwrite the syscall's errno
        // with that separate release operation before the caller captures it.
        int error = Marshal.GetLastPInvokeError();
        _handle.DangerousRelease();
        Marshal.SetLastPInvokeError(error);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:73088629e7e5a9ce / B2 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Linux/LinuxUnmanagedDynamicLibrary.cs:38–52
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public override bool TryGetFunction<TDelegate>(string functionName, out TDelegate? d) where TDelegate : class
    {
        var p = NativeMethods.linux_dlsym(_handle, functionName);

        if (p != IntPtr.Zero)
        {
            d = Marshal.GetDelegateForFunctionPointer<TDelegate>(p);
            return true;
        }

        d = null;
        return false;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:db8b04f37f7bd313 / B2 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Linux/Udev/LinuxUdev.cs:58–70
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // Throw the PlatformApiException(LinuxUdevError) if the value is < 0.
    // Otherwise, just return.
    protected static int ThrowIfFailedNegative(int value)
    {
        if (value >= 0) return value;

        throw new PlatformApiException(
            string.Format(
                CultureInfo.CurrentCulture,
                "ExceptionMessages.LinuxUdevError"));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:bc8b0f8d9f94511f / B2 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Linux/Udev/LinuxUdevSafeHandle.cs:20–21
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal class LinuxUdevSafeHandle : SafeHandle
{


PROPOSED SAMPLE — no verdict: core-001:unit:09d455b96b0731b1 / B2 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Linux/Udev/LinuxUdevSafeHandle.cs:27–33
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    protected override bool ReleaseHandle()
    {
        _ = NativeMethods.udev_unref(handle);
        return true;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:30aeca91d375799b / B2 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Linux/Udev/Udev.Interop.cs:187–190
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static IntPtr udev_device_get_parent(LinuxUdevDeviceSafeHandle deviceObject) =>
        udev_device_get_parent(deviceObject.DangerousGetHandle());


PROPOSED SAMPLE — no verdict: core-001:unit:d6382b511d208a17 / B2 / native-interop-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Native/Linux/Udev/Udev.Interop.cs:259–268
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // Set the monitor object to be able to receive reports.
    // If the result is < 0, error.
    // The C signature is
    //   int udev_monitor_enable_receiving(struct udev_monitor *udev_monitor);
    [LibraryImport(Libraries.LinuxUdevLib, EntryPoint = "udev_monitor_enable_receiving",
        SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static partial int udev_monitor_enable_receiving(LinuxUdevMonitorSafeHandle monitorObject);


PROPOSED SAMPLE — no verdict: core-001:unit:d9a881e7b8d313d1 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/LinuxHidRoutingTests.cs:212–252
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 9)]
    [InlineData(true, 0)]
    [InlineData(true, 9)]
    public async Task PublicCanceledHeldOpen_DrainsOriginalClaimAndPreservesCleanupFailure(bool otp, int closeError)
    {
        using var probe = new WithheldNativeProbe();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var fidoApi = new FidoApi { CloseError = closeError };
        var otpApi = new OtpApi { CloseError = closeError };
        void before(string operation) { if (operation == "open") probe.EnterNative(); }
        fidoApi.Before = otpApi.Before = before;
        var device = Device(Slot(false, fidoApi, new Reactor()), Slot(true, otpApi));
        probe.Start(() => OpenAsync(device, otp, cancellation.Token));
        Exception? failure = null;
        try
        {
            probe.AssertResponsive();
            cancellation.Cancel();
            Assert.All(device.InterfaceIds, id => Assert.True(DeviceConnectionRegistry.IsInUse(id)));
            await Assert.ThrowsAsync<ConnectionInUseException>(() => OpenAsync(device, !otp, Token));
        }
        finally { failure = await Assert.ThrowsAnyAsync<Exception>(probe.ReleaseAndDrainAsync); }
        if (closeError == 9)
        {
            var aggregate = Assert.IsType<AggregateException>(failure);
            Assert.IsType<OperationCanceledException>(aggregate.InnerExceptions[0], exactMatch: false);
            Assert.IsType<UnrecoveredConnectionException>(aggregate.InnerExceptions[1]);
            await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => OpenAsync(device, !otp, Token));
        }
        else
        {
            Assert.IsType<OperationCanceledException>(failure, exactMatch: false);
            fidoApi.Before = otpApi.Before = null;
            await using var retry = await OpenAsync(device, !otp, Token);
        }
        Assert.Equal(1, otp ? otpApi.Closes : fidoApi.Closes);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:5b3552bacdbc0934 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/LinuxHidRoutingTests.cs:477–494
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static void QueueOtpMetadata(OtpApi api, int serial)
    {
        api.Reports.Enqueue([0, 5, 8, 0, 0, 0, 0, 0]);
        for (int i = 0; i < 11; i++) api.Reports.Enqueue([0, 5, 8, 0, 1, 0, 0, 0]);
        byte[] response = [11, 5, 3, 5, 8, 0, 2, 4, 0, 0, 0, 0, 0, 0];
        BinaryPrimitives.WriteInt32BigEndian(response.AsSpan(8), serial);
        BinaryPrimitives.WriteUInt16LittleEndian(response.AsSpan(12), (ushort)~ChecksumUtils.CalculateCrc(response.AsSpan(0, 12), 12));
        for (int sequence = 0; sequence < 2; sequence++)
        {
            var report = new byte[8];
            response.AsSpan(sequence * 7, 7).CopyTo(report);
            report[7] = (byte)(OtpConstants.ResponsePendingFlag | sequence);
            api.Reports.Enqueue(report);
        }
        api.Reports.Enqueue([0, 0, 0, 0, 0, 0, 0, OtpConstants.ResponsePendingFlag]);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:a2cb8ebfb76024bf / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/LinuxHidRoutingTests.cs:504–505
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public LinuxReadResult TryRead(LinuxFileSafeHandle handle, Span<byte> payload) => Fido.TryRead(handle, payload);


PROPOSED SAMPLE — no verdict: core-001:unit:3c8c5a92530e6a78 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/LinuxHidRoutingTests.cs:509–531
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public void Write(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload)
        {
            Fido.Write(handle, payload);
            var response = new byte[64];
            payload[..5].CopyTo(response);
            if (payload[4] == 0x86)
            {
                response[6] = 17;
                payload.Slice(7, 8).CopyTo(response.AsSpan(7));
                BinaryPrimitives.WriteUInt32BigEndian(response.AsSpan(15), 0x01020304);
                response[19] = 2; response[20] = 5; response[21] = 8;
            }
            else if (payload[4] == 0xC2)
            {
                response[6] = 12;
                new byte[] { 11, 5, 3, 5, 8, 0, 2, 4 }.CopyTo(response, 7);
                BinaryPrimitives.WriteInt32BigEndian(response.AsSpan(15), Serial);
            }
            else { response[6] = 1; response[7] = 0xAC; }
            Fido.Reports.Enqueue(response);
            Reactor.Ready();
        }


PROPOSED SAMPLE — no verdict: core-001:unit:9e44429263919c04 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/LinuxHidRoutingTests.cs:541–543
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public Task<IReadOnlyList<IHidInterface>> FindAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IHidInterface>>([hid]);


PROPOSED SAMPLE — no verdict: core-001:unit:b47685856a8443f5 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/LinuxHidRoutingTests.cs:547–549
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public Task<IReadOnlyList<IHidInterface>> FindAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(hids);


PROPOSED SAMPLE — no verdict: core-001:unit:6faa75eb756e9f3e / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxDirectFacadeAcceptanceTests.cs:158–159
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);


PROPOSED SAMPLE — no verdict: core-001:unit:395e018fc7e2d73a / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxDirectFacadeAcceptanceTests.cs:159–160
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private int _invoking, _returned;


PROPOSED SAMPLE — no verdict: core-001:unit:8383c5433a62e079 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxFidoHidConnectionTests.cs:196–213
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task OverflowAndTerminalInput_FaultRatherThanDrop()
    {
        var api = new ReportApi();
        var reactor = new Reactor();
        await using var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token);
        for (int i = 0; i < 161; i++) api.Reports.Enqueue(new byte[64]);
        for (int i = 0; i < 6; i++)
        {
            int before = api.Reads;
            reactor.Ready();
            Assert.InRange(api.Reads - before, 0, 32);
        }
        Assert.ThrowsAny<Exception>(() => { _ = connection.ReceiveAsync(Token); });
        Assert.True(reactor.RemovalRequested);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:3c2c6c9d37bbe9a3 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxFidoHidConnectionTests.cs:362–379
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task MaximumMessageFrameBurst_IsPreservedInOrderWithoutOverflow()
    {
        var api = new ReportApi();
        var reactor = new Reactor();
        await using var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token);
        for (int i = 0; i < 129; i++)
        {
            var report = new byte[64];
            report[0] = (byte)i;
            api.Reports.Enqueue(report);
        }
        for (int i = 0; i < 5; i++) reactor.Ready();
        for (int i = 0; i < 129; i++) Assert.Equal((byte)i, (await connection.ReceiveAsync(Token)).Span[0]);
        Assert.False(reactor.RemovalRequested);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:058831bc607e39bf / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxFidoHidConnectionTests.cs:520–542
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task CancellationDeliveryRace_CompletesOnceAndPreservesUndeliveredReport()
    {
        var api = new ReportApi();
        var reactor = new Reactor();
        await using var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token);
        for (int i = 0; i < 80; i++)
        {
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
            var read = connection.ReceiveAsync(cancellation.Token);
            api.Reports.Enqueue(new byte[64]);
            await Task.WhenAll(Task.Run(cancellation.Cancel, Token), Task.Run(() => reactor.Ready(), Token));
            if (read.IsCanceled)
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
                Assert.Equal(64, (await connection.ReceiveAsync(Token)).Length);
            }
            else Assert.Equal(64, (await read).Length);
        }
        Assert.False(reactor.RemovalRequested);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:38107865bd5f9cc4 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxFidoHidConnectionTests.cs:616–617
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        internal Exception? WriteError { get; set; }


PROPOSED SAMPLE — no verdict: core-001:unit:6c9be3cfd34e0b43 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxFidoHidConnectionTests.cs:668–669
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        internal Exception? ActivationError, RemovalError;


PROPOSED SAMPLE — no verdict: core-001:unit:ac82c5fd3a2083b2 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxHidReadReactorTests.Native.cs:129–135
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public LinuxFileSafeHandle CreateWake()
        {
            var wake = _native.CreateWake();
            Wakes.Add(wake);
            return wake;
        }


PROPOSED SAMPLE — no verdict: core-001:unit:2264327db253f484 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxHidReadReactorTests.Native.cs:178–179
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private readonly Thread _thread;


PROPOSED SAMPLE — no verdict: core-001:unit:44d48f1f907a4c54 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxHidReadReactorTests.Native.cs:199–205
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public void Dispose()
        {
            _work.CompleteAdding();
            if (_thread.Join(TimeSpan.FromSeconds(5)))
                _work.Dispose();
        }


PROPOSED SAMPLE — no verdict: core-001:unit:21b391fa4dca9bab / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxHidReadReactorTests.cs:196–232
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task HeldOwnerReferenceRelease_DoesNotBlockReactorOrCreateCleanupJobs()
    {
        using var probe = new WithheldNativeProbe();
        using var native = new ReadinessNative();
        using var worker = new OwnerWorker();
        await using var reactor = new LinuxHidReadReactor(native);
        int deviceCloseThread = 0;
        using var first = Descriptor(41, () => { deviceCloseThread = Environment.CurrentManagedThreadId; probe.EnterNative(); });
        using var second = Descriptor(42);
        var a = reactor.Register(first, 1, (_, _) => { }, _ => { });
        await native.NextPoll();
        var ready = Completion();
        var b = reactor.Register(second, 2, (_, _) => ready.TrySetResult(), error => ready.TrySetException(error));
        await native.NextPoll();
        Assert.False(first.CloseOnce().Executed); // deliberately early owner disposal, reference still retained
        Assert.Null(a.RequestRemoval());
        await Wait(a.Quiescence);
        probe.Start(() => worker.Invoke(a.ReleaseReference));
        try
        {
            probe.AssertResponsive();
            Assert.Throws<InvalidOperationException>(a.ReleaseReference); // concurrent release is misuse, not another job
            (await native.NextPoll()).Complete(42, NativeMethods.POLLIN);
            await Wait(ready.Task);
            Assert.Equal(1, native.Created);
            Assert.Equal(0, native.PoolCleanupCalls);
        }
        finally { await probe.ReleaseAndDrainAsync(); }
        Assert.Equal(worker.ThreadId, deviceCloseThread);
        Assert.DoesNotContain(deviceCloseThread, native.PollThreads);
        Assert.True(first.CloseOnce().Released);
        a.ReleaseReference(); // exactly-once completed release is idempotent
        await Drain(b);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:050924edfdbb9b99 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxHidReadReactorTests.cs:232–257
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task ReleaseFromSiblingReactorCallback_IsRejectedEvenAfterQuiescence()
    {
        using var native = new ReadinessNative();
        await using var reactor = new LinuxHidReadReactor(native);
        using var first = Descriptor(41);
        using var second = Descriptor(42);
        var a = reactor.Register(first, 1, (_, _) => { }, _ => { });
        await native.NextPoll();
        var checkedGuard = Completion();
        var b = reactor.Register(second, 2, (_, _) =>
        {
            Assert.Throws<InvalidOperationException>(a.ReleaseReference);
            Assert.Throws<InvalidOperationException>(() => reactor.Register(first, 9, (_, _) => { }, _ => { }));
            checkedGuard.TrySetResult();
        }, error => checkedGuard.TrySetException(error));
        await native.NextPoll();
        a.RequestRemoval();
        await Wait(a.Quiescence);
        (await native.NextPoll()).Complete(42, NativeMethods.POLLIN);
        await Wait(checkedGuard.Task);
        a.ReleaseReference();
        await Drain(b);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:95b76832a71d899e / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxHidReadReactorTests.cs:360–390
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(NativeMethods.POLLERR)]
    [InlineData(NativeMethods.POLLHUP)]
    [InlineData(NativeMethods.POLLNVAL)]
    [InlineData(NativeMethods.POLLIN | NativeMethods.POLLHUP)]
    [InlineData(0)] // callback exception rather than native terminal flags
    public async Task TerminalOrCallbackFault_RemovesOnlyAffectedIdentity(short flags)
    {
        using var native = new ReadinessNative { HoldNextPoll = true };
        await using var reactor = new LinuxHidReadReactor(native);
        using var first = Descriptor(41);
        using var second = Descriptor(42);
        int faults = 0;
        var a = reactor.Register(first, 1, (_, _) => throw new IOException("callback fault"), _ =>
        {
            Interlocked.Increment(ref faults);
            throw new IOException("terminal callback fault");
        });
        var oldPoll = await native.NextPoll();
        var ready = Completion();
        var b = reactor.Register(second, 2, (_, _) => ready.TrySetResult(), error => ready.TrySetException(error));
        oldPoll.Complete(41, flags == 0 ? NativeMethods.POLLIN : flags);
        await Wait(a.Quiescence);
        (await native.NextPoll()).Complete(42, NativeMethods.POLLIN);
        await Wait(ready.Task);
        Assert.Equal(1, faults);
        await Drain(a);
        await Drain(b);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:746c6ecb75508240 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxHidReadReactorTests.cs:493–511
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task InterruptedWakeAndDrainWouldBlock_AreBoundedAndBenign()
    {
        using var native = new ReadinessNative();
        await using var reactor = new LinuxHidReadReactor(native);
        using var handle = Descriptor(42);
        var token = reactor.Register(handle, 1, (_, _) => { }, _ => { });
        await native.NextPoll();
        native.Writes.Enqueue(new LinuxIoResult(-1, NativeMethods.EINTR));
        native.Writes.Enqueue(new LinuxIoResult(-1, NativeMethods.EINTR));
        native.Reads.Enqueue(new LinuxIoResult(-1, NativeMethods.EAGAIN));
        token.Signal();
        await native.NextPoll();
        Assert.Equal(3, native.WriteCalls);
        Assert.Equal(1, native.ReadCalls);
        await Drain(token);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:75da4c68bcfef47d / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxHidReadReactorTests.cs:636–656
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task RealPoll_ControlAndShutdownWakeWithoutPeriodicTimeouts()
    {
        if (!OperatingSystem.IsLinux()) return;
        var native = new ObservedRealNative();
        await using var reactor = new LinuxHidReadReactor(native);
        using var first = NativeMethods.EventFd(0, NativeMethods.EFD_NONBLOCK | NativeMethods.EFD_CLOEXEC);
        using var second = NativeMethods.EventFd(0, NativeMethods.EFD_NONBLOCK | NativeMethods.EFD_CLOEXEC);
        var a = reactor.Register(first, 1, (_, _) => { }, _ => { });
        Assert.Equal(2, (await native.NextPoll()).Length);
        var b = reactor.Register(second, 2, (_, _) => { }, _ => { });
        Assert.Equal(3, (await native.NextPoll()).Length);
        await Drain(b);
        Assert.Equal(2, (await native.NextPoll()).Length);
        await Wait(reactor.DisposeAsync().AsTask());
        await Drain(a);
        Assert.All(native.Wakes, wake => Assert.True(wake.CloseOnce().Released));
        Assert.Throws<ObjectDisposedException>(() => reactor.Register(first, 3, (_, _) => { }, _ => { }));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:47669f96fd907b74 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxHidReportApiTests.cs:215–223
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(new byte[] { 0x77, 0xFF, 0xFF, 0xFF, 0xFF, 0x97, 0xFF, 0xFF, 0xFF, 0xFF, 0x81, 2 })]
    [InlineData(new byte[] { 0x75, 8, 0x97, 0, 0, 0, 0x80, 0x91, 2 })]
    public void DescriptorParser_RejectsUnboundedReportArithmeticRatherThanFallingBack(byte[] descriptor)
    {
        Assert.Throws<InvalidDataException>(() => LinuxHidReportSizes.Parse(descriptor));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:fcfe703561725672 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxOtpHidConnectionTests.cs:87–98
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task FailedGet_ClearsPartialOwnedReportBeforeTaskFaults()
    {
        var api = new ReportApi { GetError = new IOException("partial GET") };
        await using var connection = await OpenAsync(api, Token);
        api.Before = operation => { if (operation == "get") api.GetBuffer = Field<byte[]>(Field<object>(connection, "_owner"), "_inputCopy"); };
        await Assert.ThrowsAsync<IOException>(() => connection.ReceiveAsync(Token));
        // The fake captures the production destination, not a second fake-owned copy.
        Assert.All(api.GetBuffer ?? throw new InvalidOperationException("Missing GET staging."), value => Assert.Equal(0, value));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:2bce912e1f9afd3c / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxOtpHidConnectionTests.cs:98–122
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(0, true)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    [InlineData(9, false)]
    [InlineData(22, false)]
    public async Task CheckedClose_SharesOutcomeAndReleasesOnlyWithProof(int error, bool released)
    {
        var api = new ReportApi { CloseError = error };
        var claim = new Claim();
        var connection = await OpenAsync(api, Token, claim);
        if (error == 0) { await connection.DisposeAsync(); connection.Dispose(); }
        else
        {
            var failure = await Assert.ThrowsAnyAsync<Exception>(() => connection.DisposeAsync().AsTask());
            if (!released) Assert.IsType<UnrecoveredConnectionException>(failure);
            Assert.Same(failure, await Assert.ThrowsAnyAsync<Exception>(() => connection.DisposeAsync().AsTask()));
            Assert.Same(failure, Assert.ThrowsAny<Exception>(connection.Dispose));
        }
        Assert.Equal(released ? 1 : 0, claim.Releases);
        Assert.Equal(1, api.Closes);
        GC.SuppressFinalize(connection);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:3c9a7c8b541cca31 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxOtpHidConnectionTests.cs:122–135
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task PreCanceledOpen_AcquiresNoDescriptorAndReleasesClaim()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        cancellation.Cancel();
        var api = new ReportApi();
        var claim = new Claim();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => OpenAsync(api, cancellation.Token, claim));
        Assert.Equal(0, api.Opens);
        Assert.Equal(0, api.Closes);
        Assert.Equal(1, claim.Releases);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:089ef2e148592b06 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxOtpHidConnectionTests.cs:135–157
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelBeforeDispatch_SubmitsNoFeatureCall(bool send)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var api = new ReportApi();
        await using var connection = await OpenAsync(api, Token);
        var owner = Field<object>(connection, "_owner");
        Task operation;
        using (Field<Lock>(owner, "_sync").EnterScope())
        {
            operation = send ? connection.SendAsync(new byte[8], cancellation.Token) : connection.ReceiveAsync(cancellation.Token);
            cancellation.Cancel();
        }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.Equal(0, api.Gets);
        Assert.Equal(0, api.Sets);
        // Cancellation does not poison the reusable single operation slot.
        Assert.Equal(8, (await connection.ReceiveAsync(Token)).Length);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:1466040321727ca1 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxOtpHidConnectionTests.cs:224–237
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedOpenWithoutDescriptor_ReleasesClaimWithoutNativeClose(bool invalid)
    {
        var api = new ReportApi { InvalidHandle = invalid, OpenError = invalid ? null : new IOException("open failed") };
        var claim = new Claim();
        await Assert.ThrowsAsync<IOException>(() => OpenAsync(api, Token, claim));
        Assert.Equal(1, api.Opens);
        Assert.Equal(0, api.Closes);
        Assert.Equal(1, claim.Releases);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:db2e439fd0b74649 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxOtpHidConnectionTests.cs:343–348
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    internal static async Task<IOtpHidConnection> OpenAsync(ReportApi api, CancellationToken token, IDisposable? claim = null)
    {
        return await LinuxOtpHidConnection.OpenAsync("fake", api, token, claim);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:32b260c58ffe3225 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxOtpHidConnectionTests.cs:354–355
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        internal Action<string>? Before;


PROPOSED SAMPLE — no verdict: core-001:unit:8f26a9a7db7a0a71 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxOtpHidConnectionTests.cs:393–401
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public LinuxReleaseOutcome CloseOnce(LinuxFileSafeHandle handle)
        {
            Assert.Same(Worker, Thread.CurrentThread);
            Closes++;
            Before?.Invoke("close");
            handle.Dispose();
            return new(true, CloseError is 0 or 4 or 5, CloseError == 0 ? 0 : -1, CloseError);
        }


PROPOSED SAMPLE — no verdict: core-001:unit:29a9ae241fca9754 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxOtpRecoveryTests.cs:11–12
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public class LinuxOtpRecoveryTests
{


PROPOSED SAMPLE — no verdict: core-001:unit:f1a0056cc1ac63d2 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxOtpWorkerFailureTests.cs:131–177
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static async Task FinalizerProbeAsync()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var api = new ReportApi { CloseError = 9 };
        var claim = new Claim();
        api.Before = operation =>
        {
            if (operation != "close") return;
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(5), Token)) throw new TimeoutException("Finalizer close barrier not released.");
        };
        var (facade, owner) = await AbandonAsync(api, claim);
        await Task.Yield();
        try
        {
            for (int i = 0; i < 50 && facade.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(1, Token);
            }
            Assert.False(facade.IsAlive);
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5), Token));
            Assert.Equal(0, claim.Releases);
            Assert.True(owner.IsAlive);
        }
        finally { release.Set(); }
        await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => ObserveShutdown(owner).WaitAsync(TimeSpan.FromSeconds(5), Token));
        Assert.Equal(0, claim.Releases);
        Assert.Equal(1, api.Closes);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Assert.True(owner.IsAlive); // Owner root, not facade or a live worker, retains unknown release.

        var faultedFacade = await AbandonFaultedAsync();
        await Task.Yield();
        for (int i = 0; i < 50 && faultedFacade.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Task.Delay(1, Token);
        }
        Assert.False(faultedFacade.IsAlive); // Actual finalizer after loop fault/event retirement cannot throw.
    }


PROPOSED SAMPLE — no verdict: core-001:unit:ac7381252ca00098 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxReportConnectionTests.cs:53–54
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private bool _read;


PROPOSED SAMPLE — no verdict: core-001:unit:ffaec5087ccd9b2e / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxReportConnectionTests.cs:68–77
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public LinuxReadResult TryRead(LinuxFileSafeHandle handle, Span<byte> payload)
        {
            if (_read) return new(LinuxReadDisposition.WouldBlock, 0, 11);
            _read = true;
            Operations.Add("read");
            Assert.Equal(64, payload.Length);
            "*+,"u8.ToArray().CopyTo(payload);
            return new LinuxReadResult(LinuxReadDisposition.Data, 3, 0);
        }


PROPOSED SAMPLE — no verdict: core-001:unit:7c0317e63f6ccad4 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxReportConnectionTests.cs:82–88
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public void GetFeature(LinuxFileSafeHandle handle, Span<byte> payload)
        {
            Operations.Add("get-feature");
            Assert.Equal(8, payload.Length);
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }.CopyTo(payload);
        }


PROPOSED SAMPLE — no verdict: core-001:unit:7914e385fc0fae13 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/Linux/LinuxReportConnectionTests.cs:88–93
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public void SetFeature(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload)
        {
            Operations.Add("set-feature");
            Assert.Equal(8, payload.Length);
        }


PROPOSED SAMPLE — no verdict: core-001:unit:b026a1d3f3cba7dd / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/LinuxHidDescriptorParsingTests.cs:77–80
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // A truncated long-item payload stops parsing after preserving preceding values.
    public static readonly byte[] TruncatedLongItemPayload = [0x05, 0x01, 0xFE, 0x05, 0xAA, 0xBB, 0xCC];


PROPOSED SAMPLE — no verdict: core-001:unit:582b0239814eeab7 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/LinuxHidDescriptorParsingTests.cs:82–84
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public static readonly byte[] LongItemHeaderTruncated = [0xFE, 0x02];


PROPOSED SAMPLE — no verdict: core-001:unit:3905ec0b40f3903a / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/LinuxHidDeviceListenerFaultInjectionTests.cs:43–68
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static void AssertFailureOutcomeStopsListener(LinuxHidPollOutcome failureOutcome)
    {
        // Arrange
        var source = new FakeLinuxHidEventSource();
        source.ScriptOutcomes(failureOutcome);
        using var listener = new LinuxHidDeviceListener(() => source);

        // Act
        listener.Start();

        // Assert - the loop must exit on the first failure, not hot-spin retrying a broken fd.
        Assert.True(
            SpinWait.SpinUntil(() => listener.Status == DeviceListenerStatus.Error, DefaultTimeout),
            $"Listener did not reach Error after {failureOutcome}");
        Assert.True(
            SpinWait.SpinUntil(() => source.Disposed, DefaultTimeout),
            "Listener thread did not dispose its event source on the error path");
        Assert.Equal(1, source.WaitCalls);

        // Stop after Error must still land in Stopped without double-disposing the source.
        listener.Stop();
        Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
        Assert.Equal(1, source.DisposeCalls);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:6681a2a4ace73933 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/LinuxHidDeviceListenerFaultInjectionTests.cs:118–141
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void EventWithoutAction_EmitsUnknownRescanHint()
    {
        // Arrange
        var source = new FakeLinuxHidEventSource();
        source.ScriptOutcomes(LinuxHidPollOutcome.Event);
        source.ScriptReceives(new LinuxHidUdevEvent(Action: null, "/sys/devices/hid-1", "/dev/hidraw0"));
        using var listener = new LinuxHidDeviceListener(() => source);
        var hints = new ConcurrentQueue<bool>();
        listener.Changed = () => hints.Enqueue(true);

        // Act
        listener.Start();

        // Assert
        Assert.True(
            SpinWait.SpinUntil(() => !hints.IsEmpty, DefaultTimeout),
            "Action-less udev event did not produce a rescan hint");
        Assert.True(hints.TryDequeue(out _));

        listener.Stop();
    }


PROPOSED SAMPLE — no verdict: core-001:unit:c300adac76f89b3f / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/LinuxHidDeviceListenerFaultInjectionTests.cs:191–216
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void AddEventWithUnreadyHidrawNode_EmitsDelayedFallbackHint()
    {
        // Arrange - the hidraw node is not ready at notification time; the listener must
        // re-hint after the readiness delay so discovery retries once permissions settle.
        var source = new FakeLinuxHidEventSource { HidrawReady = false };
        source.ScriptOutcomes(LinuxHidPollOutcome.Event);
        source.ScriptReceives(new LinuxHidUdevEvent("add", "/sys/devices/hid-1", "/dev/hidraw0"));
        using var listener = new LinuxHidDeviceListener(() => source);
        var hints = new ConcurrentQueue<bool>();
        listener.Changed = () => hints.Enqueue(true);

        // Act
        listener.Start();

        // Assert
        Assert.True(
            SpinWait.SpinUntil(() => hints.Count >= 2, DefaultTimeout),
            "Unready hidraw node did not produce a delayed fallback hint");
        Assert.True(hints.TryDequeue(out _));
        Assert.True(hints.TryDequeue(out _));

        listener.Stop();
    }


PROPOSED SAMPLE — no verdict: core-001:unit:7a82b232c6cd76a2 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/LinuxHidDeviceListenerFaultInjectionTests.cs:262–276
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData("/sys/devices/pci0/usb1/1-1/1-1:1.0/0003:1050:0407.0001/hidraw/hidraw3",
        "/sys/devices/pci0/usb1/1-1/1-1:1.0/0003:1050:0407.0001")]
    [InlineData("/sys/devices/pci0/usb1/1-1/1-1:1.0/0003:1050:0407.0001/hidraw",
        "/sys/devices/pci0/usb1/1-1/1-1:1.0/0003:1050:0407.0001")]
    [InlineData("/sys/devices/pci0/usb1/1-1/1-1:1.0/0003:1050:0407.0001",
        "/sys/devices/pci0/usb1/1-1/1-1:1.0/0003:1050:0407.0001")]
    public void TrimHidrawSyspath_MapsHidrawNodesToStableParentIdentity(string syspath, string expected)
    {
        // add and remove notifications arrive on the hidraw child; identity must resolve to
        // the parent HID device path so both sides of a plug/unplug pair correlate.
        Assert.Equal(expected, LinuxUdevHidEventSource.TrimHidrawSyspath(syspath));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:5f228badc05c83c6 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/LinuxHidDeviceListenerFaultInjectionTests.cs:292–294
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public bool HidrawReady { get; set; } = true;


PROPOSED SAMPLE — no verdict: core-001:unit:b9f1af2ad8097ba1 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/LinuxHidDeviceListenerFaultInjectionTests.cs:296–298
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public int DisposeCalls => Volatile.Read(ref _disposeCalls);


PROPOSED SAMPLE — no verdict: core-001:unit:8bf36df32b3a4156 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/LinuxHidDeviceListenerFaultInjectionTests.cs:318–320
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public bool Initialize() => InitializeResult;


PROPOSED SAMPLE — no verdict: core-001:unit:16841907c5b3f5d2 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/LinuxUdevHidEventSourceTests.cs:1–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Native.Linux.Udev;
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using LibcNativeMethods = Yubico.YubiKit.Core.Native.Linux.Libc.NativeMethods;
using UdevNativeMethods = Yubico.YubiKit.Core.Native.Linux.Udev.NativeMethods;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

/// <summary>
/// Cross-platform policy tests for the udev monitor descriptor and eventfd shutdown write.
/// Scripted delegates ensure these tests never call Linux libc or require HID hardware.
/// </summary>
[Trait("Category", "RuntimeResilience")]



PROPOSED SAMPLE — no verdict: core-001:unit:a9463dcf672055d2 / B2 / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/Hid/LinuxUdevHidEventSourceTests.cs:129–145
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void WriteShutdownSignal_Eagain_IsAcceptedAsAlreadySignaled()
    {
        var script = new ScriptedEventFdWrite((-1, LibcNativeMethods.EAGAIN));
        var failures = new List<(int Result, int Error)>();

        LinuxUdevHidEventSource.WriteShutdownSignal(
            script.Write,
            script.GetLastError,
            (result, error) => failures.Add((result, error)));

        Assert.Equal(1, script.WriteCalls);
        Assert.Equal(1, script.ErrorReads);
        Assert.Empty(failures);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:9d688231691df7a1 / B2 / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/LinuxHidRouteVerification/Evidence.cs:8–8
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal sealed record Observation(string Name, double? CallerReturnMs, double? TotalMs, long? AllocatedBytes);


PROPOSED SAMPLE — no verdict: core-001:unit:fc1fb9627bba154a / B2 / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:verification/LinuxHidRouteVerification/Evidence.cs:61–61
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal sealed partial class EvidenceJson : JsonSerializerContext;
