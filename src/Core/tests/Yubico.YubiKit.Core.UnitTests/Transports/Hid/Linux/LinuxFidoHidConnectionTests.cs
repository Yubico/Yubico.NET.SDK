// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using Yubico.YubiKit.Core.UnitTests.BoundaryInventory;
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux;

[Trait("Category", "RuntimeResilience")]
public class LinuxFidoHidConnectionTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("open")]
    [InlineData("sizes")]
    public async Task Open_ReturnsInvocationWhileNativeIsHeld(string stage)
    {
        using var probe = new WithheldNativeProbe();
        var api = new ReportApi { Before = operation => { if (operation == stage) probe.EnterNative(); } };
        var reactor = new Reactor();
        Task<LinuxFidoHidConnection>? opening = null;
        probe.Start(() => opening = LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token));
        try { probe.AssertResponsive(); }
        finally { await probe.ReleaseAndDrainAsync(); }
        await using var connection = await Assert.IsType<Task<LinuxFidoHidConnection>>(opening, exactMatch: false);
        Assert.Equal(1, api.Opens);
        Assert.True(api.Flags.HasFlag(NativeMethods.OpenFlags.O_NONBLOCK));
        Assert.True(api.Flags.HasFlag(NativeMethods.OpenFlags.O_CLOEXEC));
    }

    [Fact]
    public async Task HeldWrite_CancellationAndDisposalDoNotReleaseOrBorrowAfterCompletion()
    {
        using var probe = new WithheldNativeProbe();
        var api = new ReportApi();
        var reactor = new Reactor();
        var claim = new Claim();
        var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token, claim);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        byte[] source = new byte[64];
        source[0] = 77;
        api.Before = operation => { if (operation == "write") probe.EnterNative(); };
        probe.Start(() => connection.SendAsync(source, cancellation.Token));
        Task? shutdown = null;
        try
        {
            probe.AssertResponsive();
            source[0] = 99;
            cancellation.Cancel();
            Assert.Throws<InvalidOperationException>(() => { _ = connection.ReceiveAsync(Token); });
            shutdown = connection.DisposeAsync().AsTask();
            Assert.True(reactor.RemovalRequested);
            Assert.False(shutdown.IsCompleted);
            Assert.Equal(0, api.Closes);
            Assert.Equal(0, claim.Releases);
        }
        finally { await probe.ReleaseAndDrainAsync(); }
        await Assert.IsType<Task>(shutdown, exactMatch: false).WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.Equal(77, api.WrittenFirst);
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, claim.Releases);
        Assert.Equal(api.NativeThread, reactor.ReleaseThread);
    }

    [Fact]
    public async Task HeldCheckedClose_DisposeInvocationReturnsAndClaimWaitsForNativeResult()
    {
        using var probe = new WithheldNativeProbe();
        var api = new ReportApi();
        var claim = new Claim();
        var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, new Reactor(), Token, claim);
        api.Before = operation => { if (operation == "close") probe.EnterNative(); };
        probe.Start(() => connection.DisposeAsync().AsTask());
        try
        {
            probe.AssertResponsive();
            Assert.Equal(0, claim.Releases);
        }
        finally { await probe.ReleaseAndDrainAsync(); }
        Assert.Equal(1, claim.Releases);
        await connection.DisposeAsync();
        Assert.Equal(1, api.Closes);
    }

    [Theory]
    [InlineData("open")]
    [InlineData("sizes")]
    public async Task CancelDuringPreparation_WaitsForNativeReturnBeforeCleanup(string stage)
    {
        using var probe = new WithheldNativeProbe();
        var api = new ReportApi { Before = operation => { if (operation == stage) probe.EnterNative(); } };
        var claim = new Claim();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        probe.Start(() => LinuxFidoHidConnection.OpenAsync("fake", api, new Reactor(), cancellation.Token, claim));
        try
        {
            probe.AssertResponsive();
            cancellation.Cancel();
            var opening = await probe.Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.False(opening.IsCompleted);
            Assert.Equal(0, api.Closes);
            Assert.Equal(0, claim.Releases);
        }
        finally
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(probe.ReleaseAndDrainAsync);
        }
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, claim.Releases);
    }

    [Fact]
    public async Task CancelledReader_DetachesOnlyItselfAndLateInputReachesSuccessor()
    {
        var api = new ReportApi();
        var reactor = new Reactor();
        await using var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var first = connection.ReceiveAsync(cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        var successor = connection.ReceiveAsync(Token);
        reactor.Ready(reactor.Generation - 1);
        Assert.False(successor.IsCompleted);
        api.Reports.Enqueue(new byte[64]);
        reactor.Ready();
        Assert.Equal(64, (await successor).Length);
        var pending = connection.ReceiveAsync(Token);
        await connection.DisposeAsync();
        Assert.True(pending.IsCompleted);
        await Assert.ThrowsAnyAsync<Exception>(() => pending);
    }

    [Fact]
    public async Task Quiescence_NotWakeOutcome_GovernsExactClaimRelease()
    {
        var api = new ReportApi();
        var reactor = new Reactor { HoldDrain = true, RemovalError = new IOException("broken wake") };
        var claim = new Claim();
        var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token, claim);
        var shutdown = connection.DisposeAsync().AsTask();
        try
        {
            Assert.False(shutdown.IsCompleted);
            Assert.Equal(0, claim.Releases);
            Assert.Equal(0, api.Closes);
        }
        finally { reactor.Drain.TrySetResult(); }
        await Assert.ThrowsAsync<IOException>(() => shutdown);
        Assert.Equal(1, claim.Releases);
        Assert.Equal(1, api.Closes);
        await Assert.ThrowsAsync<IOException>(() => connection.DisposeAsync().AsTask());
    }

    [Theory]
    [InlineData(4, true)]
    [InlineData(5, true)]
    [InlineData(9, false)]
    public async Task CloseEvidence_ReleasesDespiteDiagnosticButNotUnknownOwnership(int error, bool released)
    {
        var api = new ReportApi { CloseError = error };
        var claim = new Claim();
        var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, new Reactor(), Token, claim);
        await Assert.ThrowsAnyAsync<Exception>(() => connection.DisposeAsync().AsTask());
        Assert.Equal(released ? 1 : 0, claim.Releases);
        Assert.Equal(1, api.Closes);
        await Assert.ThrowsAnyAsync<Exception>(() => connection.DisposeAsync().AsTask());
        Assert.Equal(1, api.Closes);
    }

    [Fact]
    public async Task ActivationFailure_RetainsTokenUntilLateDrain_AndPreservesCleanupError()
    {
        var api = new ReportApi { CloseError = 5 };
        var reactor = new Reactor { ActivationError = new IOException("activation"), HoldDrain = true };
        var claim = new Claim();
        var opening = LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token, claim);
        try
        {
            await reactor.Removal.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.False(opening.IsCompleted);
            Assert.Equal(0, api.Closes);
        }
        finally { reactor.Drain.TrySetResult(); }
        var failure = await Assert.ThrowsAsync<AggregateException>(() => opening);
        Assert.Contains(failure.InnerExceptions, ex => ex.Message == "activation");
        Assert.Equal(1, claim.Releases);
        Assert.Equal(1, reactor.ReferenceReleases);
    }

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

    [Fact]
    public async Task PreCancelledOpenAndSend_EnterNoNativeMutation()
    {
        var api = new ReportApi();
        var claim = new Claim();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => LinuxFidoHidConnection.OpenAsync("fake", api, new Reactor(), cancellation.Token, claim));
        Assert.Equal(0, api.Opens);
        Assert.Equal(1, claim.Releases);
        await using var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, new Reactor(), Token);
        Assert.ThrowsAny<OperationCanceledException>(() => { _ = connection.SendAsync(new byte[64], cancellation.Token); });
        Assert.Equal(0, api.Writes);
    }

    [Fact]
    public async Task CancellationDuringOwnedCopy_IsRejectedBeforeNativeDispatch()
    {
        var api = new ReportApi();
        await using var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, new Reactor(), Token);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        using var memory = new CancelOnSpan(cancellation);
        // Memory creation itself asks for GetSpan; arm only after creating the view.
        var packet = memory.Memory;
        memory.Armed = true;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connection.SendAsync(packet, cancellation.Token));
        Assert.Equal(0, api.Writes);
    }

    [Fact]
    public async Task MetadataFailure_PreservesOriginalAndCloseFailure()
    {
        var api = new ReportApi { MetadataError = new IOException("metadata"), CloseError = 5 };
        var claim = new Claim();
        var failure = await Assert.ThrowsAsync<AggregateException>(() => LinuxFidoHidConnection.OpenAsync("fake", api, new Reactor(), Token, claim));
        Assert.Equal("metadata", failure.InnerExceptions[0].Message);
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, claim.Releases);
    }

    [Fact]
    public async Task OpenFailure_NoHandleAcquired_ReleasesClaimWithoutClose()
    {
        var api = new ReportApi { Before = operation => { if (operation == "open") throw new IOException("open"); } };
        var claim = new Claim();
        await Assert.ThrowsAsync<IOException>(() => LinuxFidoHidConnection.OpenAsync("fake", api, new Reactor(), Token, claim));
        Assert.Equal(0, api.Closes);
        Assert.Equal(1, claim.Releases);
    }

    [Fact]
    public async Task ReaderDelivery_OldCancellationCannotDetachSuccessor()
    {
        var api = new ReportApi();
        var reactor = new Reactor();
        await using var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token);
        using var oldCancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var first = connection.ReceiveAsync(oldCancellation.Token);
        api.Reports.Enqueue(new byte[64]);
        reactor.Ready();
        var successor = connection.ReceiveAsync(Token);
        oldCancellation.Cancel();
        Assert.Equal(64, (await first).Length);
        Assert.False(successor.IsCompleted);
        api.Reports.Enqueue(new byte[64]);
        reactor.Ready();
        Assert.Equal(64, (await successor).Length);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Disposal_PublicReadTaskIsTerminalBeforeSyncOrAsyncReturn(bool synchronous)
    {
        var api = new ReportApi();
        var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, new Reactor(), Token);
        var pending = connection.ReceiveAsync(Token);
        if (synchronous) await Task.Run(connection.Dispose, Token);
        else await connection.DisposeAsync();
        Assert.True(pending.IsCompleted);
        await Assert.ThrowsAnyAsync<Exception>(() => pending);
        Assert.Equal(1, api.Closes);
    }

    [Fact]
    public async Task TerminalWake_IsTerminalButNotPhysicalRelease()
    {
        var api = new ReportApi();
        var reactor = new Reactor();
        var claim = new Claim();
        await using var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token, claim);
        var read = connection.ReceiveAsync(Token);
        connection.RequestTerminalWake();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => read);
        Assert.Equal(0, api.Closes);
        Assert.Equal(0, claim.Releases);
        Assert.ThrowsAny<Exception>(() => { _ = connection.SendAsync(new byte[64], Token); });
    }

    [Theory]
    [InlineData((int)LinuxReadDisposition.EndOfStream)]
    [InlineData((int)LinuxReadDisposition.Error)]
    [InlineData((int)LinuxReadDisposition.Interrupted)]
    public async Task TerminalReadAndPersistentEintr_AreBoundedAndRemoveInput(int disposition)
    {
        var api = new ReportApi { EmptyRead = new((LinuxReadDisposition)disposition, 0, 5) };
        var reactor = new Reactor();
        await using var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token);
        var pending = connection.ReceiveAsync(Token);
        reactor.Ready();
        await Assert.ThrowsAsync<IOException>(() => pending);
        Assert.InRange(api.Reads, 1, 4);
        Assert.True(reactor.RemovalRequested);
    }

    [Fact]
    public async Task ReactorTerminal_WakesReaderButDoesNotProveDrain()
    {
        var api = new ReportApi();
        var reactor = new Reactor { HoldDrain = true };
        var claim = new Claim();
        var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token, claim);
        var pending = connection.ReceiveAsync(Token);
        reactor.Terminal?.Invoke(new IOException("poll failed"));
        await Assert.ThrowsAsync<IOException>(() => pending);
        var shutdown = connection.DisposeAsync().AsTask();
        try
        {
            Assert.False(shutdown.IsCompleted);
            Assert.Equal(0, api.Closes);
        }
        finally { reactor.Drain.TrySetResult(); }
        await shutdown;
        Assert.Equal(1, claim.Releases);
    }

    [Fact]
    public async Task ShortTypedInput_FaultsAndDoesNotReturnPartialPacket()
    {
        var api = new ReportApi();
        var reactor = new Reactor();
        await using var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token);
        var pending = connection.ReceiveAsync(Token);
        api.Reports.Enqueue(new byte[3]);
        reactor.Ready();
        await Assert.ThrowsAsync<IOException>(() => pending);
        Assert.True(reactor.RemovalRequested);
    }

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

    [Fact]
    public async Task ReadCompletion_DoesNotRunApplicationContinuationOnCallbackThread()
    {
        var api = new ReportApi();
        var reactor = new Reactor();
        await using var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token);
        var read = connection.ReceiveAsync(Token);
        var continuation = read.ContinueWith(_ => Environment.CurrentManagedThreadId, Token,
            TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        var returned = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        api.Reports.Enqueue(new byte[64]);
        new Thread(() =>
        {
            try
            {
                reactor.Ready();
                returned.TrySetResult(Environment.CurrentManagedThreadId);
            }
            catch (Exception ex) { returned.TrySetException(ex); }
        })
        { IsBackground = true }.Start();
        int callbackThread = await returned.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.NotEqual(callbackThread, await continuation.WaitAsync(TimeSpan.FromSeconds(5), Token));
        Assert.Equal(64, (await read).Length);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SynchronousSelfDrain_IsRejectedOnNativeWorkerAndReadCallback(bool output)
    {
        var api = new ReportApi();
        var reactor = new Reactor();
        await using var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token);
        int guarded = 0;
        api.Before = operation =>
        {
            if (operation != (output ? "write" : "read")) return;
            Assert.Throws<InvalidOperationException>(connection.Dispose);
            guarded++;
        };
        if (output) await connection.SendAsync(new byte[64], Token);
        else
        {
            var read = connection.ReceiveAsync(Token);
            Assert.Throws<InvalidOperationException>(() => { _ = connection.SendAsync(new byte[64], Token); });
            Assert.Throws<InvalidOperationException>(() => { _ = connection.ReceiveAsync(Token); });
            api.Reports.Enqueue(new byte[64]);
            reactor.Ready();
            Assert.Equal(64, (await read).Length);
        }
        Assert.True(guarded > 0);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    [Fact]
    public async Task DirectFacade_DescriptorSizesAndArbitraryOutputLengthSurviveSharedOwner()
    {
        var api = new ReportApi { Sizes = (17, 29) };
        var reactor = new Reactor();
        await using var connection = new LinuxHidIOReportConnection("fake", api, reactor);
        Assert.Equal(17, connection.InputReportSize);
        Assert.Equal(29, connection.OutputReportSize);
        api.Reports.Enqueue(new byte[17]);
        reactor.Ready();
        Assert.Equal(17, connection.GetReport().Length);
        connection.SetReport(new byte[111]);
        Assert.Equal(111, api.WrittenLength);
        Assert.Equal(17, api.ReadCapacity);
    }

    [Fact]
    public async Task UnknownClose_QuarantinesRealRegistryClaim()
    {
        string id = $"l3-unknown-{Guid.NewGuid()}";
        var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([id], Token);
        var api = new ReportApi { CloseError = 9 };
        var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, new Reactor(), Token, claim);
        await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask());
        await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => DeviceConnectionRegistry.AcquireConnectionAsync([id], Token).AsTask());
        Assert.True(DeviceConnectionRegistry.IsInUse(id));
    }

    [Fact]
    public async Task FailedActivation_LateDrainReleasesOnlyOriginalRealClaim()
    {
        string id = $"l3-late-{Guid.NewGuid()}";
        var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([id], Token);
        var reactor = new Reactor { ActivationError = new IOException("failed activation"), HoldDrain = true };
        var opening = LinuxFidoHidConnection.OpenAsync("fake", new ReportApi(), reactor, Token, claim);
        try
        {
            await reactor.Removal.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            await Assert.ThrowsAsync<ConnectionInUseException>(() => DeviceConnectionRegistry.AcquireConnectionAsync([id], Token).AsTask());
        }
        finally { reactor.Drain.TrySetResult(); }
        await Assert.ThrowsAsync<IOException>(() => opening);
        using var successor = await DeviceConnectionRegistry.AcquireConnectionAsync([id], Token);
        reactor.Ready(reactor.Generation - 1);
        Assert.True(DeviceConnectionRegistry.IsInUse(id));
        await Assert.ThrowsAsync<ConnectionInUseException>(() => DeviceConnectionRegistry.AcquireConnectionAsync([id], Token).AsTask());
    }

    [Fact]
    public async Task RegisterFailure_ClosesUnregisteredHandleOnOriginalWorker()
    {
        var api = new ReportApi();
        var claim = new Claim();
        var reactor = new Reactor { RegisterError = new IOException("register") };
        await Assert.ThrowsAsync<IOException>(() => LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token, claim));
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, claim.Releases);
        Assert.Equal(0, reactor.ReferenceReleases);
    }

    [Fact]
    public async Task ReferenceReleaseFailure_RetainsOwnerAndClaimWithoutClosing()
    {
        var api = new ReportApi();
        var claim = new Claim();
        var reactor = new Reactor { ReleaseError = new IOException("reference release") };
        var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token, claim);
        await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask());
        Assert.Equal(0, api.Closes);
        Assert.Equal(0, claim.Releases);
        Assert.Equal(1, reactor.ReferenceReleases);
        await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask());
        Assert.Equal(1, reactor.ReferenceReleases);
    }

    [Fact]
    public async Task FailedWrite_IsNotReplayedAndCanBeSafelyDrained()
    {
        var api = new ReportApi { WriteError = new IOException("write failed") };
        var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, new Reactor(), Token);
        await Assert.ThrowsAsync<IOException>(() => connection.SendAsync(new byte[64], Token));
        await connection.DisposeAsync();
        Assert.Equal(1, api.Writes);
        Assert.Equal(1, api.Closes);
    }

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

    [Fact]
    public Task AbandonedFacade_FinalizerRequestsShutdownButRootRetainsHeldLifetime() =>
        PcscIsolatedProbe.RunAsync("linux-fido-finalizer", typeof(LinuxFidoHidConnectionTests),
            nameof(AbandonedFacade_FinalizerRequestsShutdownButRootRetainsHeldLifetime), AbandonedFacadeProbeAsync);

    private static async Task AbandonedFacadeProbeAsync()
    {
        var api = new ReportApi();
        var reactor = new Reactor { HoldDrain = true };
        var claim = new Claim();
        var weak = await AbandonAsync(api, reactor, claim);
        // Leave the opening task's completion stack (which can still hold its result).
        await Task.Yield();
        for (int i = 0; i < 4 && weak.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        try
        {
            Assert.False(weak.IsAlive);
            await reactor.Removal.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.Equal(0, api.Closes);
            Assert.Equal(0, claim.Releases);
        }
        finally { reactor.Drain.TrySetResult(); }
        await claim.Released.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, claim.Releases);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> AbandonAsync(ReportApi api, Reactor reactor, Claim claim)
    {
        LinuxFidoHidConnection? connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token, claim);
        var weak = new WeakReference(connection);
        return weak;
    }

    private sealed class CancelOnSpan(CancellationTokenSource cancellation) : MemoryManager<byte>
    {
        private readonly byte[] _buffer = new byte[64];
        internal bool Armed;
        public override Span<byte> GetSpan()
        {
            if (Armed) cancellation.Cancel();
            return _buffer;
        }
        public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();
        public override void Unpin() { }
        protected override void Dispose(bool disposing) { }
    }

    internal sealed class Claim : IDisposable
    {
        internal int Releases;
        internal TaskCompletionSource Released { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Dispose()
        {
            Interlocked.Increment(ref Releases);
            Released.TrySetResult();
        }
    }

    internal sealed class ReportApi : ILinuxHidReportApi
    {
        internal Action<string>? Before;
        internal ConcurrentQueue<byte[]> Reports { get; } = new();
        internal int Opens, Closes, WrittenFirst, WrittenLength, Writes, NativeThread, CloseError, Reads, ReadCapacity;
        internal LinuxReadResult EmptyRead = new(LinuxReadDisposition.WouldBlock, 0, 11);
        internal NativeMethods.OpenFlags Flags;
        internal (int, int) Sizes = (64, 64);
        internal Exception? MetadataError { get; set; }
        internal Exception? WriteError { get; set; }
        internal Thread? WorkerThread { get; private set; }
        public LinuxFileSafeHandle Open(string path, NativeMethods.OpenFlags flags)
        {
            WorkerThread = Thread.CurrentThread;
            Flags = flags;
            NativeThread = Environment.CurrentManagedThreadId;
            Interlocked.Increment(ref Opens);
            Before?.Invoke("open");
            return new LinuxFileSafeHandle();
        }
        public (int InputSize, int OutputSize) GetReportSizes(LinuxFileSafeHandle handle)
        {
            Assert.Equal(NativeThread, Environment.CurrentManagedThreadId);
            Before?.Invoke("sizes");
            return MetadataError is not null ? throw MetadataError : ((int InputSize, int OutputSize))Sizes;
        }
        public LinuxReadResult TryRead(LinuxFileSafeHandle handle, Span<byte> payload)
        {
            Before?.Invoke("read");
            Reads++;
            ReadCapacity = payload.Length;
            if (!Reports.TryDequeue(out var report)) return EmptyRead;
            report.CopyTo(payload);
            return new(LinuxReadDisposition.Data, report.Length, 0);
        }
        public void Write(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload)
        {
            Assert.Equal(NativeThread, Environment.CurrentManagedThreadId);
            Before?.Invoke("write");
            Writes++;
            if (WriteError is not null) throw WriteError;
            WrittenLength = payload.Length;
            WrittenFirst = payload[0];
        }
        public void GetFeature(LinuxFileSafeHandle handle, Span<byte> payload) => throw new NotSupportedException();
        public void SetFeature(LinuxFileSafeHandle handle, ReadOnlySpan<byte> payload) => throw new NotSupportedException();
        public LinuxReleaseOutcome CloseOnce(LinuxFileSafeHandle handle)
        {
            Assert.Equal(NativeThread, Environment.CurrentManagedThreadId);
            Before?.Invoke("close");
            Interlocked.Increment(ref Closes);
            handle.Dispose();
            return new(true, CloseError is 0 or 4 or 5, CloseError == 0 ? 0 : -1, CloseError);
        }
    }

    internal sealed class Reactor : ILinuxHidReadReactor, ILinuxHidReadRegistration
    {
        private Action<long, short>? _ready;
        internal Action<Exception>? Terminal;
        internal bool HoldDrain, RemovalRequested;
        internal Exception? ActivationError, RemovalError;
        internal Exception? RegisterError { get; set; }
        internal Exception? ReleaseError { get; set; }
        internal int ReferenceReleases, ReleaseThread;
        internal TaskCompletionSource Drain { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Removal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public long Generation { get; private set; }
        public Task<Exception?> Activation => Task.FromResult(ActivationError);
        public Task Quiescence => Drain.Task;
        public ILinuxHidReadRegistration Register(LinuxFileSafeHandle handle, long generation, Action<long, short> onReady, Action<Exception> onTerminal)
        {
            if (RegisterError is not null) throw RegisterError;
            Generation = generation;
            _ready = onReady;
            Terminal = onTerminal;
            return this;
        }
        internal void Ready(long? generation = null) => _ready?.Invoke(generation ?? Generation, 1);
        public Exception? RequestRemoval()
        {
            RemovalRequested = true;
            Removal.TrySetResult();
            if (!HoldDrain) Drain.TrySetResult();
            return RemovalError;
        }
        public void ReleaseReference()
        {
            Assert.True(Drain.Task.IsCompleted);
            ReferenceReleases++;
            if (ReleaseError is not null) throw ReleaseError;
            ReleaseThread = Environment.CurrentManagedThreadId;
        }
        public void Signal() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}