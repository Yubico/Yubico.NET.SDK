// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.Windows;
using Yubico.YubiKit.Core.UnitTests.Devices;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Windows;

public abstract class WindowsRoutingDirectContract
{
    protected abstract bool Otp { get; }
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private IHidConnection Connect(WindowsRoutingApi api) => Otp
        ? new WindowsHidFeatureReportConnection("direct", api)
        : new WindowsHidIOReportConnection("direct", api);

    [Fact]
    public async Task DirectReports_DescriptorSizedPayloadAndNativeZeroFramingSurviveDispose()
    {
        var api = new WindowsRoutingApi { Length = 17 };
        var connection = Connect(api);
        byte[] received;
        try
        {
            Assert.Equal(16, connection.InputReportSize);
            Assert.Equal(16, connection.OutputReportSize);
            var payload = Enumerable.Repeat((byte)9, 16).ToArray();
            await Task.Run(() => connection.SetReport(payload), Token);
            Assert.Equal(new byte[] { 0 }.Concat(payload), api.Sent);
            received = await Task.Run(connection.GetReport, Token);
            Assert.Equal(Enumerable.Repeat((byte)7, 16), received);
            Assert.NotNull(api.NativeInput);
            Assert.All(api.NativeInput, value => Assert.Equal(0, value));
            Assert.Throws<ArgumentException>(() => connection.SetReport(new byte[15]));
        }
        finally { await connection.DisposeAsync(); }
        Assert.Equal(Enumerable.Repeat((byte)7, 16), received);
        Assert.Equal(1, api.Closes);
    }

    [Theory]
    [InlineData("set")]
    [InlineData("get")]
    public async Task DirectCall_HeldNativeLastUseBlocksCallerAndSharedDisposal(string direction)
    {
        var gate = new ManualResetEventSlim();
        var closeGate = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var api = new WindowsRoutingApi { Length = 17 };
        var connection = Connect(api);
        api.Before = operation =>
        {
            if (operation == (Otp ? direction : "last-use")) { entered.TrySetResult(); gate.Wait(Token); }
            if (operation == "close") { closing.TrySetResult(); closeGate.Wait(Token); }
        };
        Task? call = null;
        try
        {
            call = Task.Run(() => { if (direction == "set") connection.SetReport(new byte[16]); else connection.GetReport(); }, Token);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.False(call.IsCompleted);
            var first = connection.DisposeAsync().AsTask();
            var second = connection.DisposeAsync().AsTask();
            Assert.Same(first, second);
            Assert.False(first.IsCompleted);
            Assert.Equal(0, api.Closes);
            gate.Set();
            await call;
            await closing.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.False(first.IsCompleted);
            closeGate.Set();
            await first;
        }
        finally
        {
            gate.Set(); closeGate.Set();
            // Keep barriers alive until the original worker and synchronous caller finish.
            try
            {
                if (call is not null) await call.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            }
            finally
            {
                await connection.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
                // A timeout does not prove the caller drained; retain its gates if it is still live.
                if (call is null || call.IsCompleted)
                {
                    gate.Dispose();
                    closeGate.Dispose();
                }
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SyncDispose_HeldReportAndCheckedClose_DrainsBeforeReturningSharedOutcome(bool startDuringClose)
    {
        var reportGate = new ManualResetEventSlim();
        var closeGate = new ManualResetEventSlim();
        var returned = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var invoking = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var events = new System.Collections.Concurrent.ConcurrentQueue<string>();
        // Proven release with a diagnostic makes sharing the final shutdown outcome observable.
        var api = new WindowsRoutingApi { Length = 17, Release = new(true, 5) };
        var connection = Connect(api);
        api.Before = operation =>
        {
            if (operation == (Otp ? "set" : "last-use"))
            {
                entered.TrySetResult();
                reportGate.Wait(Token);
                events.Enqueue("report-last-use-exit");
            }
            if (operation == "close")
            {
                closing.TrySetResult();
                closeGate.Wait(Token);
                events.Enqueue("checked-close-exit");
            }
        };
        Task? report = null;
        Task<Exception?>? dispose = null;
        try
        {
            report = Task.Factory.StartNew(() => connection.SetReport(new byte[16]),
                Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            if (startDuringClose)
            {
                _ = connection.DisposeAsync();
                reportGate.Set();
                await closing.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            }
            dispose = Task.Factory.StartNew(() =>
            {
                Assert.False(Thread.CurrentThread.IsThreadPoolThread);
                invoking.TrySetResult();
                var error = Record.Exception(connection.Dispose);
                events.Enqueue("sync-dispose-return");
                returned.Set();
                return error;
            }, Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            await invoking.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            // Finite nonreturn observations, not universal scheduling or latency proofs.
            Assert.False(returned.Wait(TimeSpan.FromMilliseconds(200), Token),
                startDuringClose ? "Dispose returned while checked close was held." : "Dispose returned while native report last use was held.");
            var first = connection.DisposeAsync().AsTask();
            Assert.Same(first, connection.DisposeAsync().AsTask());
            Assert.False(first.IsCompleted);
            if (!startDuringClose)
            {
                Assert.Equal(0, api.Closes);
                reportGate.Set();
                await closing.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
                Assert.False(returned.Wait(TimeSpan.FromMilliseconds(200), Token),
                    "Dispose returned while checked close was held.");
            }
            await report.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.False(first.IsCompleted);
            closeGate.Set();
            var syncError = await dispose.WaitAsync(TimeSpan.FromSeconds(5), Token);
            var asyncError = await Record.ExceptionAsync(() => first.WaitAsync(TimeSpan.FromSeconds(5), Token));
            if (Otp) Assert.Equal(5, Assert.IsType<System.ComponentModel.Win32Exception>(syncError).NativeErrorCode);
            else Assert.Equal("Close failed with Windows error 5.", Assert.IsType<IOException>(syncError).Message);
            Assert.Same(asyncError, syncError);
            Assert.Equal(["report-last-use-exit", "checked-close-exit", "sync-dispose-return"], events);
            Assert.Equal(1, api.Closes);
        }
        finally
        {
            reportGate.Set();
            closeGate.Set();
            // Drain every admitted caller and the owner despite runner cancellation or assertion failure.
            // Observe the expected close diagnostic without allowing it to skip another caller's drain.
            var shutdown = connection.DisposeAsync().AsTask();
            var drain = Task.WhenAll(report ?? Task.CompletedTask, dispose ?? Task.CompletedTask, shutdown);
            _ = await Record.ExceptionAsync(() => drain.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None));
            // A watchdog expiry is not release proof; retain gates if any worker can still access them.
            if (drain.IsCompleted)
            {
                reportGate.Dispose();
                closeGate.Dispose();
                returned.Dispose();
            }
            Assert.True(drain.IsCompleted, "Admitted callers and native owner did not drain after both gates were released.");
        }
    }

    [Fact]
    public async Task SyncDispose_FromNativeWorkerRejectsSelfWait()
    {
        var api = new WindowsRoutingApi { Length = 17 };
        await using var connection = Connect(api);
        api.Before = operation => { if (operation == "set") Assert.Throws<InvalidOperationException>(connection.Dispose); };
        await Task.Run(() => connection.SetReport(new byte[16]), Token);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Constructor_HeldOpenOrFailedOpenCleanupBlocksOnIndependentOwner(bool fail)
    {
        using var gate = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int caller = 0, worker = 0;
        var api = new WindowsRoutingApi { Length = 17, OpenError = fail ? new IOException("open") : null };
        api.Before = operation =>
        {
            if (operation != (fail ? "close" : "open")) return;
            worker = Environment.CurrentManagedThreadId;
            entered.SetResult();
            gate.Wait(Token);
        };
        var constructing = Task.Run(() => { caller = Environment.CurrentManagedThreadId; return Connect(api); }, Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.NotEqual(caller, worker);
            Assert.False(constructing.IsCompleted);
        }
        finally
        {
            gate.Set();
            // Constructor failure has no facade to dispose; its original owner must finish cleanup here.
            if (fail) await Assert.ThrowsAsync<IOException>(() => constructing.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None));
            else await (await constructing.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None)).DisposeAsync();
        }
        Assert.Equal(1, api.Closes);
    }
}