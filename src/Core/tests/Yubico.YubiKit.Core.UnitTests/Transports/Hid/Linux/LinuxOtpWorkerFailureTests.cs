// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Reflection;
using System.Runtime.CompilerServices;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;
using static Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux.LinuxOtpHidConnectionTests;
using Claim = Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux.LinuxFidoHidConnectionTests.Claim;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux;

[Trait("Category", "RuntimeResilience")]
public class LinuxOtpWorkerFailureTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(0, true)]
    [InlineData(5, true)]
    [InlineData(9, false)]
    public async Task IdleInterrupt_StopsAdmissionPreservesFailureAndChecksCloseOnOriginalWorker(int closeError, bool released)
    {
        var api = new ReportApi { CloseError = closeError };
        var claim = new Claim();
        var connection = await OpenAsync(api, Token, claim);
        var worker = IdleWorker(api);
        worker.Interrupt();
        var failure = await Assert.ThrowsAnyAsync<Exception>(() => connection.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), Token));
        Assert.Contains(Causes(failure), error => error is ThreadInterruptedException);
        if (!released) Assert.IsType<UnrecoveredConnectionException>(failure);
        Assert.Throws<ObjectDisposedException>(() => { _ = connection.ReceiveAsync(Token); });
        Assert.Throws<ObjectDisposedException>(() => { _ = connection.SendAsync(new byte[8], Token); });
        Assert.Same(failure, await Assert.ThrowsAnyAsync<Exception>(() => connection.DisposeAsync().AsTask()));
        Assert.Same(failure, Assert.ThrowsAny<Exception>(connection.Dispose));
        Assert.Equal(1, api.Closes);
        Assert.Equal(released ? 1 : 0, claim.Releases);
        Assert.Same(worker, api.Worker);
        GC.SuppressFinalize(connection);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InterruptWithQueuedOperation_TerminalizesActualTaskAndClearsUnsubmittedOutput(bool send)
    {
        var api = new ReportApi();
        var claim = new Claim();
        var connection = await OpenAsync(api, Token, claim);
        var worker = IdleWorker(api);
        var owner = Field<LinuxOtpHidNativeOwner>(connection, "_owner");
        Task operation;
        byte[]? copy = null;
        using (Field<Lock>(owner, "_sync").EnterScope())
        {
            worker.Interrupt();
            operation = send ? connection.SendAsync(Enumerable.Repeat((byte)0xA7, 8).ToArray(), Token) : connection.ReceiveAsync(Token);
            if (send) copy = Field<byte[]>(owner, "_outputCopy");
        }
        var initial = await Assert.ThrowsAsync<ThreadInterruptedException>(() => operation.WaitAsync(TimeSpan.FromSeconds(5), Token));
        Assert.Same(initial, await Assert.ThrowsAsync<ThreadInterruptedException>(() => connection.DisposeAsync().AsTask()));
        if (copy is not null) Assert.All(copy, value => Assert.Equal(0, value));
        Assert.Equal(0, api.Gets);
        Assert.Equal(0, api.Sets);
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, claim.Releases);
        owner.RequestShutdown(); // Safe even after event retirement.
        GC.SuppressFinalize(connection);
    }

    [Fact]
    public async Task InterruptBeforeOpenDispatch_CompletesOpenAndReleasesClaimWithoutNativeAcquisition()
    {
        var api = new ReportApi();
        var claim = new Claim();
        var owner = Assert.IsType<LinuxOtpHidNativeOwner>(Activator.CreateInstance(typeof(LinuxOtpHidNativeOwner),
            BindingFlags.Instance | BindingFlags.NonPublic, null, [api, claim], null));
        Exception? primaryFailure = null;
        try
        {
            var (opening, worker) = await LinuxWorkerFailureTestSetup.InterruptBeforeOpenDispatchAsync(owner, Token);
            var initial = await Assert.ThrowsAsync<ThreadInterruptedException>(() => opening.WaitAsync(TimeSpan.FromSeconds(5), Token));
            var closing = owner.ShutdownAsync();
            Assert.Same(closing, owner.ShutdownAsync());
            Assert.Same(initial, await Assert.ThrowsAsync<ThreadInterruptedException>(() => closing));
            Assert.True(worker.Join(TimeSpan.FromSeconds(5)));
            Assert.Equal(0, api.Opens);
            Assert.Equal(0, api.Closes);
            Assert.Equal(1, claim.Releases);
        }
        catch (Exception error) { primaryFailure = error; throw; }
        finally { await LinuxWorkerFailureTestSetup.DrainAsync(owner, owner.ShutdownAsync, primaryFailure); }
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(9, false)]
    public async Task WorkerFaultWithHeldClose_KeepsPendingShutdownAndClaimUntilNativeReturn(int closeError, bool released)
    {
        using var closeEntered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var api = new ReportApi { CloseError = closeError };
        var claim = new Claim();
        var connection = await OpenAsync(api, Token, claim);
        api.Before = operation =>
        {
            if (operation != "close") return;
            closeEntered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(5), Token)) throw new TimeoutException("Close not released.");
        };
        IdleWorker(api).Interrupt();
        var closing = connection.DisposeAsync().AsTask();
        try
        {
            Assert.True(closeEntered.Wait(TimeSpan.FromSeconds(5), Token));
            Assert.False(closing.IsCompleted);
            Assert.Equal(0, claim.Releases);
        }
        finally { release.Set(); }
        var failure = await Assert.ThrowsAnyAsync<Exception>(() => closing.WaitAsync(TimeSpan.FromSeconds(5), Token));
        Assert.Contains(Causes(failure), error => error is ThreadInterruptedException);
        Assert.Equal(released ? 1 : 0, claim.Releases);
        Assert.Equal(1, api.Closes);
        GC.SuppressFinalize(connection);
    }

    [Fact]
    public Task Finalizer_PublishesNonblockingShutdownAndUnknownReleaseRetainsRoot() =>
        PcscIsolatedProbe.RunAsync("linux-otp-finalizer", typeof(LinuxOtpWorkerFailureTests),
            nameof(Finalizer_PublishesNonblockingShutdownAndUnknownReleaseRetainsRoot), FinalizerProbeAsync);

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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task ObserveShutdown(WeakReference owner)
    {
        var nativeOwner = Assert.IsType<LinuxOtpHidNativeOwner>(owner.Target);
        nativeOwner.RequestShutdown();
        return nativeOwner.ShutdownAsync(); // Return only the TCS task, not a facade/owner-capturing wrapper.
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> AbandonFaultedAsync()
    {
        var api = new ReportApi();
        var claim = new Claim();
        var connection = await OpenAsync(api, Token, claim);
        var owner = Field<LinuxOtpHidNativeOwner>(connection, "_owner");
        var worker = IdleWorker(api);
        worker.Interrupt();
        await Assert.ThrowsAsync<ThreadInterruptedException>(() => owner.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(5), Token));
        Assert.True(worker.Join(TimeSpan.FromSeconds(5))); // Worker and event have actually retired.
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, claim.Releases);
        return new WeakReference(connection);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(WeakReference Facade, WeakReference Owner)> AbandonAsync(ReportApi api, Claim claim)
    {
        var connection = await OpenAsync(api, Token, claim);
        return (new WeakReference(connection), new WeakReference(Field<object>(connection, "_owner")));
    }

    private static IEnumerable<Exception> Causes(Exception error)
    {
        yield return error;
        IEnumerable<Exception> children = error is AggregateException aggregate ? aggregate.InnerExceptions : error.InnerException is { } inner ? [inner] : [];
        foreach (var child in children)
            foreach (var cause in Causes(child)) yield return cause;
    }

    private static Thread IdleWorker(ReportApi api)
    {
        var worker = api.Worker ?? throw new InvalidOperationException("Open did not capture worker.");
        WaitUntilParked(worker);
        return worker;
    }

    private static void WaitUntilParked(Thread worker) =>
        Assert.True(SpinWait.SpinUntil(() => (worker.ThreadState & ThreadState.WaitSleepJoin) != 0, TimeSpan.FromSeconds(5)), "Worker did not park.");
}