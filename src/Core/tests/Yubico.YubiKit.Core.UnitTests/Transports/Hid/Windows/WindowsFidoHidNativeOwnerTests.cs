// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Reflection;
using System.Runtime.CompilerServices;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.Hid.Windows;
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Windows;

public sealed class WindowsFidoHidNativeOwnerTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TestCleanup_EarlyExitWithAdmittedWork_DrainsBeforeDisposingGates(bool holdSubmit)
    {
        var api = new ReportApi { HoldSubmit = holdSubmit, HoldClose = true };
        var claim = new Claim();
        Task<ReadOnlyMemory<byte>>? receive = null;
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await using (api)
            {
                var owner = await OpenOwnerAsync("fake", api, Token, claim);
                receive = owner.ReceiveAsync(false, Token);
                await api.Submitted.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
                throw new InvalidOperationException("Injected exit before the assertion/close region.");
            }
        });
        Assert.False(Assert.IsType<Thread>(api.Worker).IsAlive);
        Assert.NotNull(receive);
        Assert.Equal(4, (await receive).Length);
        Assert.Equal(1, api.RetireCount);
        Assert.Equal(1, claim.Releases);
    }

    [Fact]
    public async Task Open_CanceledAfterSuccessPublicationBeforeFactoryContinuation_RemainsUsable()
    {
        await using var api = new ReportApi { CompleteInSubmit = true };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var claim = new Claim();
        var owner = await OpenOwnerAsync("fake", api, cancellation.Token, claim,
            async opened =>
            {
                await opened.WaitAsync(TimeSpan.FromSeconds(5), Token);
                cancellation.Cancel();
            });
        try
        {
            Assert.Equal(4, (await owner.ReceiveAsync(false, Token).WaitAsync(TimeSpan.FromSeconds(5), Token)).Length);
            Assert.Equal(0, claim.Releases);
        }
        finally
        {
            // Drain even if the runner cancels; the fake gates must outlive their worker.
            await owner.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
        Assert.Equal(1, claim.Releases);
    }

    [Fact]
    public async Task Send_CanceledAfterAdmissionBeforeDispatch_SubmitsNothing()
    {
        await using var api = new ReportApi();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var owner = await OpenOwnerAsync("fake", api, Token);
        var admission = Assert.IsType<Lock>(typeof(WindowsFidoHidNativeOwner)
            .GetField("_sync", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner));
        Task send;
        // Hold the real admission lock so the original worker cannot cross its dispatch boundary.
        lock (admission)
        {
            send = owner.SendAsync(new byte[4], cancellation.Token);
            cancellation.Cancel();
        }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send.WaitAsync(TimeSpan.FromSeconds(5), Token));
        await owner.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.Null(api.Operation);
        Assert.Equal(0, api.RetireCount);
    }

    [Fact]
    public async Task Worker_OpenAndReports_DoNotInheritCallerExecutionContext()
    {
        await using var api = new ReportApi { CompleteInSubmit = true };
        Ambient.Value = "caller";
        try
        {
            var owner = await OpenOwnerAsync("fake", api, Token);
            await owner.ReceiveAsync(false, Token);
            await owner.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.Null(api.AmbientAtOpen);
            Assert.Equal("caller", Ambient.Value);
        }
        finally { Ambient.Value = null; }
    }

    private static readonly AsyncLocal<string?> Ambient = new();

    [Fact]
    public async Task Receive_Removal_StopsAdmissionAndDrainsBeforeClose()
    {
        await using var api = new ReportApi { HoldClose = true };
        var owner = await OpenOwnerAsync("fake", api, Token);
        var receive = owner.ReceiveAsync(false, Token);
        await api.Submitted.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        api.Complete(1167);
        try
        {
            await Assert.ThrowsAsync<IOException>(() => receive.WaitAsync(TimeSpan.FromSeconds(5), Token));
            Assert.Throws<ObjectDisposedException>(() => { _ = owner.SendAsync(new byte[4], Token); });
            await api.Closing.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.Equal(1, api.RetireCount);
        }
        finally { api.CloseGate.Set(); }
        await owner.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(5), Token);
    }

    [Fact]
    public async Task Cancel_Failure_StopsAdmissionButRetainsNativeWorkUntilSuccess()
    {
        await using var api = new ReportApi { CancelError = 5, HoldClose = true };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var claim = new Claim();
        var owner = await OpenOwnerAsync("fake", api, Token, claim);
        var receive = owner.ReceiveAsync(false, cancellation.Token);
        await api.Submitted.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        cancellation.Cancel();
        await api.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.False(receive.IsCompleted);
        Assert.Equal(0, api.RetireCount);
        api.Complete(0);
        try
        {
            await api.Closing.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.Equal(4, (await receive).Length);
            Assert.Equal(0, claim.Releases);
            Assert.Throws<ObjectDisposedException>(() => { _ = owner.ReceiveAsync(false, Token); });
        }
        finally { api.CloseGate.Set(); }
        var error = await Assert.ThrowsAsync<IOException>(() => owner.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(5), Token));
        Assert.Contains("5", error.Message);
        Assert.Equal(1, claim.Releases);
    }

    [Fact]
    public async Task Worker_InterruptedWithAcceptedRead_DrainsOnOriginalWorker()
    {
        await using var api = new ReportApi();
        var claim = new Claim();
        var owner = await OpenOwnerAsync("fake", api, Token, claim);
        var receive = owner.ReceiveAsync(false, Token);
        await api.Submitted.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.IsType<Thread>(api.Worker).Interrupt();
        await api.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.False(receive.IsCompleted);
        Assert.Equal(0, api.RetireCount);
        api.Complete(0);
        Assert.Equal(4, (await receive.WaitAsync(TimeSpan.FromSeconds(5), Token)).Length);
        await Assert.ThrowsAsync<ThreadInterruptedException>(() => owner.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(5), Token));
        Assert.Equal(1, claim.Releases);
        Assert.Single(api.Threads.Distinct());
    }

    [Fact]
    public Task Submit_UnknownDispatchFailure_QuarantinesWithoutFreeOrClose() =>
        PcscIsolatedProbe.RunAsync("windows-fido-unknown-dispatch", typeof(WindowsFidoHidNativeOwnerTests),
            nameof(Submit_UnknownDispatchFailure_QuarantinesWithoutFreeOrClose), async () =>
            {
                await using var api = new ReportApi { ThrowSubmit = true };
                var claim = new Claim();
                var owner = await OpenOwnerAsync("fake", api, Token, claim);
                var send = owner.SendAsync(new byte[] { 1, 2, 3, 4 }, Token);
                await api.Submitted.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
                Assert.True(Assert.IsType<Thread>(api.Worker).Join(TimeSpan.FromSeconds(5)));
                Assert.False(send.IsCompleted);
                Assert.False(owner.ShutdownAsync().IsCompleted);
                Assert.Equal(0, api.CancelCount);
                Assert.Equal(0, api.RetireCount);
                Assert.Equal(0, claim.Releases);
                Assert.False(api.Closing.Task.IsCompleted);
                Assert.Equal(1, Assert.IsType<WindowsHidOverlappedOperation>(api.Operation).NativeReport[1]);
            });

    [Fact]
    public Task Finalizer_UnknownClose_RetainsOwnerButNotFacade() =>
        PcscIsolatedProbe.RunAsync("windows-fido-finalizer", typeof(WindowsFidoHidNativeOwnerTests),
            nameof(Finalizer_UnknownClose_RetainsOwnerButNotFacade), async () =>
            {
                await using var api = new ReportApi { ReleaseProven = false, HoldClose = true };
                var claim = new Claim();
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
                    await api.Closing.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
                    Assert.True(owner.IsAlive);
                    Assert.Equal(0, claim.Releases);
                }
                finally { api.CloseGate.Set(); }
                var nativeOwner = Assert.IsType<WindowsFidoHidNativeOwner>(owner.Target);
                await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => nativeOwner.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(5), Token));
                Assert.True(Assert.IsType<Thread>(api.Worker).Join(TimeSpan.FromSeconds(5)));
                nativeOwner.RequestShutdown(); // Signal is retired; finalizer request must remain safe.
                Assert.Equal(0, claim.Releases);
            });

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(WeakReference Facade, WeakReference Owner)> AbandonAsync(ReportApi api, Claim claim)
    {
        var connection = await WindowsFidoHidConnection.OpenAsync("fake", api, Token, claim);
        var owner = typeof(WindowsFidoHidConnection).GetField("_owner", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(connection);
        var nativeOwner = Assert.IsType<WindowsFidoHidNativeOwner>(owner);
        var weakOwner = new WeakReference(nativeOwner);
        api.Drain = () => weakOwner.Target is WindowsFidoHidNativeOwner retained
            ? retained.ShutdownAsync() : Task.CompletedTask;
        return (new WeakReference(connection), weakOwner);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Open_HeldNativeAcquisition_RetainsClaimThroughCanceledOrFailedCleanup(bool fail)
    {
        await using var api = new ReportApi { HoldOpen = true, HoldClose = true, FailOpen = fail };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var claim = new Claim();
        var opening = OpenOwnerAsync("fake", api, cancellation.Token, claim);
        try
        {
            await api.Opening.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.False(opening.IsCompleted);
            Assert.Equal(0, claim.Releases);
            cancellation.Cancel();
        }
        finally { api.OpenGate.Set(); }
        try
        {
            await api.Closing.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.False(opening.IsCompleted);
            Assert.Equal(0, claim.Releases);
        }
        finally { api.CloseGate.Set(); }
        if (fail) await Assert.ThrowsAsync<IOException>(() => opening.WaitAsync(TimeSpan.FromSeconds(5), Token));
        else await Assert.ThrowsAnyAsync<OperationCanceledException>(() => opening.WaitAsync(TimeSpan.FromSeconds(5), Token));
        Assert.Equal(1, claim.Releases);
    }

    [Fact]
    public async Task Receive_PreCanceled_SubmitsNothingAndLeavesSlotReusable()
    {
        await using var api = new ReportApi { CompleteInSubmit = true };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var owner = await OpenOwnerAsync("fake", api, Token);
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => { _ = owner.ReceiveAsync(false, cancellation.Token); });
        Assert.Null(api.Operation);
        await owner.ReceiveAsync(false, Token);
        await owner.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.Equal(1, api.RetireCount);
    }

    [Theory]
    [InlineData(5u, 5u, 0)]
    [InlineData(0u, 4u, 0)]
    [InlineData(0u, 5u, 1)]
    public async Task Receive_RejectionOrInvalidNativeReport_RetiresOnceAndClears(uint rejection, uint count, byte reportId)
    {
        await using var api = new ReportApi { Rejection = rejection, CompleteInSubmit = rejection == 0, Count = count, ReportId = reportId };
        var owner = await OpenOwnerAsync("fake", api, Token);
        await Assert.ThrowsAsync<IOException>(() => owner.ReceiveAsync(false, Token).WaitAsync(TimeSpan.FromSeconds(5), Token));
        await owner.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.Equal(1, api.RetireCount);
        Assert.All(Assert.IsType<WindowsHidOverlappedOperation>(api.Operation).NativeReport, b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task Send_CanceledDuringHeldSubmit_CancelRunsOnlyAfterAcceptedReturn()
    {
        await using var api = new ReportApi { HoldSubmit = true };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var owner = await OpenOwnerAsync("fake", api, Token);
        var send = owner.SendAsync(new byte[4], cancellation.Token);
        try
        {
            await api.Submitted.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            cancellation.Cancel();
            Assert.Equal(0, api.CancelCount);
            Assert.False(send.IsCompleted);
        }
        finally { api.SubmitGate.Set(); }
        await api.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        api.Complete(995);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send.WaitAsync(TimeSpan.FromSeconds(5), Token));
        await owner.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.Single(api.Threads.Distinct());
    }

    [Fact]
    public async Task TypedFacade_DescriptorSizedDirectReportsRemainAvailable_TypedBoundariesReject()
    {
        await using var api = new ReportApi();
        await using var connection = await OpenConnectionAsync("fake", api, Token);
        Assert.Throws<ArgumentException>(() => { _ = connection.SendAsync(new byte[4], Token); });
        Assert.Throws<IOException>(() => { _ = connection.ReceiveAsync(Token); });
        Assert.Null(api.Operation);
    }

    [Fact]
    public async Task TypedFacade_HeldClose_ActualReceiveTaskIsTerminalBeforeDisposal()
    {
        await using var api = new ReportApi { Length = 65, HoldClose = true };
        var connection = await OpenConnectionAsync("fake", api, Token);
        var receive = connection.ReceiveAsync(Token);
        await api.Submitted.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        var disposal = connection.DisposeAsync().AsTask();
        var secondDisposal = connection.DisposeAsync().AsTask();
        await api.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        api.Complete(0);
        try
        {
            await api.Closing.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.True(receive.IsCompletedSuccessfully);
            Assert.Equal(64, (await receive).Length);
            Assert.False(disposal.IsCompleted);
            Assert.False(secondDisposal.IsCompleted);
        }
        finally { api.CloseGate.Set(); }
        await Task.WhenAll(disposal, secondDisposal).WaitAsync(TimeSpan.FromSeconds(5), Token);
    }

    [Fact]
    public async Task Send_CallbackBeforeSubmitReturn_RetainsStorageAndPublicTask()
    {
        await using var api = new ReportApi { HoldSubmit = true, CompleteInSubmit = true };
        var owner = await OpenOwnerAsync("fake", api, Token);
        byte[] source = [1, 2, 3, 4];
        var send = owner.SendAsync(source, Token);
        try
        {
            await api.Submitted.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            source[0] = 99;
            Assert.False(send.IsCompleted);
            Assert.Equal(0, api.RetireCount);
            Assert.Equal(new byte[] { 0, 1, 2, 3, 4 }, api.Operation?.NativeReport);
            Assert.Throws<InvalidOperationException>(() => { _ = owner.ReceiveAsync(false, Token); });
        }
        finally { api.SubmitGate.Set(); }
        await send.WaitAsync(TimeSpan.FromSeconds(5), Token);
        await owner.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.Equal(1, api.RetireCount);
        Assert.All(Assert.IsType<WindowsHidOverlappedOperation>(api.Operation).NativeReport, b => Assert.Equal(0, b));
        Assert.Single(api.Threads.Distinct());
    }

    [Theory]
    [InlineData(0u, true)]
    [InlineData(1168u, true)]
    [InlineData(0u, false)]
    public async Task Receive_CancellationWaitsForCompletion_ValidSuccessWins(uint cancelError, bool success)
    {
        await using var api = new ReportApi { CancelError = cancelError };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var owner = await OpenOwnerAsync("fake", api, Token);
        var receive = owner.ReceiveAsync(false, cancellation.Token);
        await api.Submitted.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        cancellation.Cancel();
        await api.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        try
        {
            Assert.False(receive.IsCompleted);
            Assert.Equal(0, api.RetireCount);
        }
        finally { api.Complete(success ? 0u : 995u); }
        if (success) Assert.Equal(new byte[] { 7, 7, 7, 7 }, (await receive.WaitAsync(TimeSpan.FromSeconds(5), Token)).ToArray());
        else await Assert.ThrowsAnyAsync<OperationCanceledException>(() => receive.WaitAsync(TimeSpan.FromSeconds(5), Token));
        await owner.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.Equal(1, api.CancelCount);
        Assert.Equal(1, api.RetireCount);
    }

    [Fact]
    public async Task Shutdown_PendingReceiveAndHeldClose_SettlesActualTaskBeforeRelease()
    {
        await using var api = new ReportApi { HoldClose = true };
        var claim = new Claim();
        var owner = await OpenOwnerAsync("fake", api, Token, claim);
        var receive = owner.ReceiveAsync(false, Token);
        await api.Submitted.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        var shutdown = owner.ShutdownAsync();
        Assert.Same(shutdown, owner.ShutdownAsync());
        await api.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        api.Complete(995);
        try
        {
            await api.Closing.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.True(receive.IsCompleted);
            await Assert.ThrowsAnyAsync<Exception>(() => receive);
            Assert.False(shutdown.IsCompleted);
            Assert.Equal(0, claim.Releases);
        }
        finally { api.CloseGate.Set(); }
        await shutdown.WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.Equal(1, claim.Releases);
    }

    private sealed class Claim : IDisposable
    {
        internal int Releases;
        public void Dispose() => Interlocked.Increment(ref Releases);
    }

    private static Task<WindowsFidoHidNativeOwner> OpenOwnerAsync(string path, ReportApi api,
        CancellationToken cancellationToken, IDisposable? claim = null, Func<Task, Task>? beforeOpenContinuation = null)
    {
        var opening = WindowsFidoHidNativeOwner.OpenAsync(path, api, cancellationToken, claim, beforeOpenContinuation);
        api.Drain = async () => await (await opening).ShutdownAsync();
        return opening;
    }

    private static Task<WindowsFidoHidConnection> OpenConnectionAsync(string path, ReportApi api,
        CancellationToken cancellationToken)
    {
        var opening = WindowsFidoHidConnection.OpenAsync(path, api, cancellationToken);
        api.Drain = async () => await (await opening).DisposeAsync();
        return opening;
    }

    private sealed class ReportApi : IWindowsHidReportApi, IAsyncDisposable
    {
        private readonly Lock _completionSync = new();
        private bool _cleanup, _completed;
        internal Func<Task>? Drain;
        internal readonly ManualResetEventSlim SubmitGate = new(false);
        internal readonly ManualResetEventSlim OpenGate = new(false);
        internal readonly ManualResetEventSlim CloseGate = new(false);
        internal readonly TaskCompletionSource Submitted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Opening = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Closing = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly List<int> Threads = [];
        internal bool HoldSubmit, CompleteInSubmit, HoldClose;
        internal bool HoldOpen, FailOpen;
        internal bool ThrowSubmit;
        internal bool ReleaseProven = true;
        internal Thread? Worker;
        internal string? AmbientAtOpen;
        internal ushort Length = 5;
        internal uint Rejection;
        internal uint? Count;
        internal byte ReportId;
        internal uint CancelError;
        internal int RetireCount, CancelCount;
        internal WindowsHidOverlappedOperation? Operation;

        public WindowsHidReportMetadata Open(string devicePath)
        {
            Worker = Thread.CurrentThread;
            AmbientAtOpen = Ambient.Value;
            Threads.Add(Environment.CurrentManagedThreadId);
            Opening.SetResult();
            if (HoldOpen) OpenGate.Wait();
            if (FailOpen) throw new IOException("Injected open failure.");
            return new(0xf1d0, 1, Length, Length);
        }
        public WindowsHidSubmission Submit(WindowsHidOverlappedOperation operation)
        {
            Threads.Add(Environment.CurrentManagedThreadId);
            lock (_completionSync)
            {
                Operation = operation;
                _completed = false;
                if (CompleteInSubmit || _cleanup) Complete(0);
            }
            Submitted.SetResult();
            // These gates model held native calls; test finally blocks release before disposing them.
            if (HoldSubmit) SubmitGate.Wait();
            if (ThrowSubmit) throw new IOException("Unknown dispatch.");
            return new(Rejection);
        }
        internal void Complete(uint error)
        {
            lock (_completionSync)
            {
                var operation = Operation ?? throw new InvalidOperationException();
                if (_completed) return;
                _completed = true;
                if (operation.Direction == WindowsHidReportDirection.Input) operation.NativeReport.AsSpan(1).Fill(7);
                operation.NativeReport[0] = ReportId;
                operation.Complete(new(error, Count ?? (uint)operation.NativeReport.Length));
            }
        }
        public uint Cancel(WindowsHidOverlappedOperation operation)
        {
            Threads.Add(Environment.CurrentManagedThreadId);
            Interlocked.Increment(ref CancelCount);
            Cancelled.SetResult();
            return CancelError;
        }
        public void Retire(WindowsHidOverlappedOperation operation)
        {
            Threads.Add(Environment.CurrentManagedThreadId);
            lock (_completionSync) _completed = true;
            Interlocked.Increment(ref RetireCount);
        }
        public WindowsHidRelease CloseOnce()
        {
            Threads.Add(Environment.CurrentManagedThreadId);
            Closing.SetResult();
            if (HoldClose) CloseGate.Wait();
            return new(ReleaseProven, ReleaseProven ? 0u : 5u);
        }
        public async ValueTask DisposeAsync()
        {
            // The await-using scope starts before acquisition/admission. Release every fake hold,
            // including a submission racing cleanup, before bounded uncancelable worker drain.
            OpenGate.Set();
            SubmitGate.Set();
            CloseGate.Set();
            lock (_completionSync)
            {
                _cleanup = true;
                if (Operation is not null && !ThrowSubmit) Complete(0);
            }
            if (Drain is not null && !ThrowSubmit)
            {
                try { await Drain().WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None); }
                catch (Exception error) when (error is not TimeoutException)
                {
                    // Tests assert expected open/close diagnostics; joining below proves fake last use.
                }
            }
            // On timeout retain the gates with the worker rather than dispose resources it may use.
            if (Worker is not null) Assert.True(Worker.Join(TimeSpan.FromSeconds(5)));
            SubmitGate.Dispose();
            CloseGate.Dispose();
            OpenGate.Dispose();
        }
    }
}