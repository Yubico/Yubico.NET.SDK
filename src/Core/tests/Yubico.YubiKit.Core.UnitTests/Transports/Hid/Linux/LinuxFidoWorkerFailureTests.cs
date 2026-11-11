// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Reflection;
using System.Runtime.CompilerServices;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;
using Claim = Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux.LinuxFidoHidConnectionTests.Claim;
using Reactor = Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux.LinuxFidoHidConnectionTests.Reactor;
using ReportApi = Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux.LinuxFidoHidConnectionTests.ReportApi;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux;

[Trait("Category", "RuntimeResilience")]
public class LinuxFidoWorkerFailureTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task IdleInterrupt_TerminatesPublicReaderAndCleansUpOnOriginalWorker()
    {
        var api = new ReportApi();
        var reactor = new Reactor { HoldDrain = true };
        var claim = new Claim();
        var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token, claim);
        try
        {
            var read = connection.ReceiveAsync(Token);
            IdleWorker(api).Interrupt();
            var initial = await Assert.ThrowsAsync<ThreadInterruptedException>(() => read.WaitAsync(TimeSpan.FromSeconds(5), Token));
            Assert.Throws<ObjectDisposedException>(() => { _ = connection.SendAsync(new byte[64], Token); });
            Assert.Throws<ObjectDisposedException>(() => { _ = connection.ReceiveAsync(Token); });
            await reactor.Removal.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            var closing = connection.DisposeAsync().AsTask();
            Assert.False(closing.IsCompleted);
            Assert.Equal(0, api.Closes);
            Assert.Equal(0, claim.Releases);
            reactor.Drain.TrySetResult();
            var disposed = await Assert.ThrowsAsync<ThreadInterruptedException>(() => closing.WaitAsync(TimeSpan.FromSeconds(5), Token));
            Assert.Same(initial, disposed);
            Assert.Same(disposed, await Assert.ThrowsAsync<ThreadInterruptedException>(() => connection.DisposeAsync().AsTask()));
            Assert.Same(disposed, Assert.Throws<ThreadInterruptedException>(connection.Dispose));
            Assert.Equal(1, api.Closes);
            Assert.Equal(1, claim.Releases);
            Assert.Equal(api.NativeThread, reactor.ReleaseThread);
        }
        finally
        {
            reactor.Drain.TrySetResult();
            // A failing regression must not run a known-broken finalizer in the test host.
            GC.SuppressFinalize(connection);
        }
    }

    [Fact]
    public async Task InterruptRacingQueuedSend_FaultsTaskWithoutDispatchAndClearsOwnedCopy()
    {
        var api = new ReportApi();
        var reactor = new Reactor();
        var claim = new Claim();
        var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token, claim);
        try
        {
            var worker = IdleWorker(api);
            var owner = Field<LinuxFidoHidNativeOwner>(connection, "_owner");
            Task send;
            byte[] copy;
            // Hold the real admission lock across interrupt and publication. The idle
            // worker cannot handle the fault until the queued copy/task are observable.
            // This is test-only placement, not an added production scheduling hook.
            using (Field<Lock>(owner, "_sync").EnterScope())
            {
                worker.Interrupt();
                send = connection.SendAsync(Enumerable.Repeat((byte)0xA7, 64).ToArray(), Token);
                copy = Field<byte[]>(owner, "_outputCopy");
                Assert.All(copy, value => Assert.Equal(0xA7, value));
            }
            var failure = await Assert.ThrowsAsync<ThreadInterruptedException>(() => send.WaitAsync(TimeSpan.FromSeconds(5), Token));
            Assert.Same(failure, await Assert.ThrowsAsync<ThreadInterruptedException>(() => connection.DisposeAsync().AsTask()));
            Assert.All(copy, value => Assert.Equal(0, value));
            Assert.Equal(0, api.Writes);
            Assert.Equal(1, api.Closes);
            Assert.Equal(1, claim.Releases);
        }
        finally { GC.SuppressFinalize(connection); }
    }

    [Fact]
    public async Task InterruptBeforeOpenDispatch_CompletesOpenAndShutdownWithoutNativeAcquisition()
    {
        var api = new ReportApi();
        var claim = new Claim();
        // The private-constructor setup lets the test hold admission before startup.
        // No fake worker/queue replaces the production StartOpen/WorkLoop machinery.
        var owner = Assert.IsType<LinuxFidoHidNativeOwner>(Activator.CreateInstance(typeof(LinuxFidoHidNativeOwner),
            BindingFlags.Instance | BindingFlags.NonPublic, null, [api, new Reactor(), claim], null));
        Task opened;
        using (Field<Lock>(owner, "_sync").EnterScope())
        {
            var start = typeof(LinuxFidoHidNativeOwner).GetMethod("StartOpen", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Missing owner startup method.");
            opened = Assert.IsType<Task>(start.Invoke(owner, ["fake", Token]), exactMatch: false);
            var worker = Field<Thread>(owner, "_worker");
            WaitUntilParked(worker); // Startup signal is set; it can only block on admission.
            worker.Interrupt();
        }
        var failure = await Assert.ThrowsAsync<ThreadInterruptedException>(() => opened.WaitAsync(TimeSpan.FromSeconds(5), Token));
        Assert.Same(failure, await Assert.ThrowsAsync<ThreadInterruptedException>(() => owner.ShutdownAsync()));
        Assert.Equal(0, api.Opens);
        Assert.Equal(0, api.Closes);
        Assert.Equal(1, claim.Releases);
        owner.RequestShutdown();
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(9, false)]
    public async Task WorkerFault_PreservesInitialErrorAlongsideCloseEvidence(int closeError, bool released)
    {
        var api = new ReportApi { CloseError = closeError };
        var claim = new Claim();
        var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, new Reactor(), Token, claim);
        try
        {
            var read = connection.ReceiveAsync(Token);
            IdleWorker(api).Interrupt();
            var initial = await Assert.ThrowsAsync<ThreadInterruptedException>(() => read.WaitAsync(TimeSpan.FromSeconds(5), Token));
            var disposal = await Assert.ThrowsAnyAsync<Exception>(() => connection.DisposeAsync().AsTask());
            Assert.Contains(initial, Causes(disposal));
            if (!released) Assert.IsType<UnrecoveredConnectionException>(disposal);
            Assert.Same(disposal, await Assert.ThrowsAnyAsync<Exception>(() => connection.DisposeAsync().AsTask()));
            Assert.Equal(released ? 1 : 0, claim.Releases);
            Assert.Equal(1, api.Closes);
        }
        finally { GC.SuppressFinalize(connection); }
    }

    [Fact]
    public async Task InterruptDuringFaultCleanup_QuarantinesWithoutRetryOrFalseRelease()
    {
        var api = new ReportApi();
        var reactor = new Reactor { HoldDrain = true };
        string id = $"linux-fido-worker-interrupt-{Guid.NewGuid()}";
        var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([id], Token);
        var connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token, claim);
        try
        {
            var read = connection.ReceiveAsync(Token);
            var worker = IdleWorker(api);
            worker.Interrupt();
            var initial = await Assert.ThrowsAsync<ThreadInterruptedException>(() => read.WaitAsync(TimeSpan.FromSeconds(5), Token));
            await reactor.Removal.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            WaitUntilParked(worker); // Now parked in true Quiescence, not an idle native loop.
            worker.Interrupt();
            var failure = await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), Token));
            Assert.Contains(initial, Causes(failure));
            Assert.Equal(0, api.Closes);
            Assert.Equal(0, reactor.ReferenceReleases);
            await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => DeviceConnectionRegistry.AcquireConnectionAsync([id], Token).AsTask());
            reactor.Drain.TrySetResult();
            Assert.Same(failure, await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask()));
            Assert.Equal(0, api.Closes);
            Assert.Equal(0, reactor.ReferenceReleases);
            Assert.Same(worker, api.WorkerThread);
        }
        finally
        {
            reactor.Drain.TrySetResult();
            GC.SuppressFinalize(connection);
        }
    }

    [Fact]
    public Task FaultedOwner_FinalizerIsSafeWhileOriginalWorkerStillDrains() =>
        PcscIsolatedProbe.RunAsync("linux-fido-worker-fault-finalizer", typeof(LinuxFidoWorkerFailureTests),
            nameof(FaultedOwner_FinalizerIsSafeWhileOriginalWorkerStillDrains), FaultedOwnerFinalizerProbeAsync);

    private static async Task FaultedOwnerFinalizerProbeAsync()
    {
        var api = new ReportApi();
        var reactor = new Reactor { HoldDrain = true };
        var claim = new Claim();
        var (weak, closing) = await AbandonFaultedAsync(api, reactor, claim);
        await Task.Yield();
        try
        {
            // Collection must also let the opening/completing producer stacks unwind.
            // This finite GC watchdog is not a native responsiveness assertion.
            for (int i = 0; i < 50 && weak.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(1, Token);
            }
            Assert.False(weak.IsAlive);
            Assert.False(closing.IsCompleted);
            Assert.Equal(0, claim.Releases);
            Assert.Equal(0, api.Closes);
        }
        finally { reactor.Drain.TrySetResult(); }
        await Assert.ThrowsAsync<ThreadInterruptedException>(() => closing.WaitAsync(TimeSpan.FromSeconds(5), Token));
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, claim.Releases);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(WeakReference Weak, Task Closing)> AbandonFaultedAsync(ReportApi api, Reactor reactor, Claim claim)
    {
        LinuxFidoHidConnection? connection = await LinuxFidoHidConnection.OpenAsync("fake", api, reactor, Token, claim);
        var pending = connection.ReceiveAsync(Token);
        var owner = Field<LinuxFidoHidNativeOwner>(connection, "_owner");
        var weak = new WeakReference(connection);
        IdleWorker(api).Interrupt();
        connection = null; // No facade reference may survive the failure-await boundary.
        await Assert.ThrowsAsync<ThreadInterruptedException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5), Token));
        // An async instance DisposeAsync task would itself retain the facade across
        // its await. Observe the owner task instead so the actual finalizer can run.
        var closing = owner.ShutdownAsync();
        return (weak, closing);
    }

    private static IEnumerable<Exception> Causes(Exception error)
    {
        yield return error;
        IEnumerable<Exception> children = error is AggregateException aggregate ? aggregate.InnerExceptions : error.InnerException is { } inner ? [inner] : Array.Empty<Exception>();
        foreach (var child in children)
            foreach (var cause in Causes(child)) yield return cause;
    }

    private static T Field<T>(object instance, string name) where T : class =>
        Assert.IsType<T>(instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance), exactMatch: false);

    private static Thread IdleWorker(ReportApi api)
    {
        var worker = api.WorkerThread ?? throw new InvalidOperationException("Native open did not capture its worker.");
        WaitUntilParked(worker);
        return worker;
    }

    private static void WaitUntilParked(Thread worker)
    {
        // The fake has no other blocking calls after Open completes. This is a fault-
        // injection placement watchdog, not a responsiveness budget or production wait.
        Assert.True(SpinWait.SpinUntil(() => (worker.ThreadState & ThreadState.WaitSleepJoin) != 0, TimeSpan.FromSeconds(5)),
            "Native owner did not park in its idle wait.");
    }
}