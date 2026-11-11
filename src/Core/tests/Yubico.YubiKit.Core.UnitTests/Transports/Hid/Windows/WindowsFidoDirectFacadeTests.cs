// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.Windows;
using Yubico.YubiKit.Core.UnitTests.Devices;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Windows;

public class WindowsFidoDirectFacadeTests : WindowsRoutingDirectContract
{
    protected override bool Otp => false;
}

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
        using var gate = new ManualResetEventSlim();
        using var closeGate = new ManualResetEventSlim();
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
            if (call is not null) await call.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            await connection.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
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