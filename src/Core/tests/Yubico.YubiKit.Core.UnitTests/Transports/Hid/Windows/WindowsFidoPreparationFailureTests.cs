// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Reflection;
using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.Hid.Windows;
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Windows;

public sealed class WindowsFidoPreparationFailureTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public Task Send_MarkedPreparationFailure_FaultsAndRetainsResources() =>
        PcscIsolatedProbe.RunAsync("windows-fido-preparation-send", typeof(WindowsFidoPreparationFailureTests),
            nameof(Send_MarkedPreparationFailure_FaultsAndRetainsResources), () => VerifyFailureAsync(false));

    [Fact]
    public Task Receive_MarkedPreparationFailure_FaultsAndRetainsResources() =>
        PcscIsolatedProbe.RunAsync("windows-fido-preparation-receive", typeof(WindowsFidoPreparationFailureTests),
            nameof(Receive_MarkedPreparationFailure_FaultsAndRetainsResources), () => VerifyFailureAsync(true));

    private static async Task VerifyFailureAsync(bool receive)
    {
        // This fake proves SDK classification only, not a reproduction of BCL allocation failure/OOM.
        using var gate = new ManualResetEventSlim(false);
        var api = new PreparationApi(gate);
        var claim = new Claim();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var connection = await WindowsFidoHidConnection.OpenAsync("fake", api, Token, claim);
        var owner = Assert.IsType<WindowsFidoHidNativeOwner>(typeof(WindowsFidoHidConnection)
            .GetField("_owner", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(connection));
        byte[] source = new byte[64];
        source.AsSpan().Fill(9);
        Task operation = receive ? connection.ReceiveAsync(cancellation.Token) : connection.SendAsync(source, cancellation.Token);
        Task disposal;
        try
        {
            await api.Submitted.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            cancellation.Cancel();
            disposal = connection.DisposeAsync().AsTask();
            Assert.Same(owner.ShutdownAsync(), owner.ShutdownAsync());
            Assert.False(operation.IsCompleted);
            Assert.False(disposal.IsCompleted);
        }
        finally
        {
            gate.Set();
            // The fake gate must outlive its original worker, including on assertion failure.
            Assert.True(Assert.IsType<Thread>(api.Worker).Join(TimeSpan.FromSeconds(5)));
        }

        Assert.True(operation.IsFaulted);
        Assert.Same(api.Error, await Assert.ThrowsAsync<IOException>(() => operation));
        Assert.All(Assert.IsType<WindowsHidOverlappedOperation>(api.Operation).NativeReport, b => Assert.Equal(0, b));
        Assert.All(source, b => Assert.Equal(9, b));
        Assert.Throws<ObjectDisposedException>(() => { _ = owner.ReceiveAsync(false, Token); });
        var failure = await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => disposal.WaitAsync(TimeSpan.FromSeconds(5), Token));
        Assert.Same(api.Error, failure.InnerException);
        Assert.Same(failure, await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask()));
        Assert.Equal(0, api.CancelCount);
        Assert.Equal(0, api.RetireCount);
        Assert.Equal(0, api.CloseCount);
        Assert.Equal(0, claim.Releases);
        var root = Assert.IsType<GCHandle>(typeof(WindowsFidoHidNativeOwner)
            .GetField("_root", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner));
        Assert.True(root.IsAllocated);
        Assert.Same(owner, root.Target);
        Assert.Same(api, typeof(WindowsFidoHidNativeOwner).GetField("_reportApi", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner));
        Assert.NotNull(typeof(WindowsFidoHidNativeOwner).GetField("_request", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner));
        owner.RequestShutdown(); // Retired signal must not hide or replace the shared failure.
    }

    private sealed class Claim : IDisposable
    {
        internal int Releases;
        public void Dispose() => Releases++;
    }

    private sealed class PreparationApi(ManualResetEventSlim gate) : IWindowsHidReportApi
    {
        internal readonly IOException Error = new("Injected preparation failure.");
        internal readonly TaskCompletionSource Submitted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal Thread? Worker;
        internal WindowsHidOverlappedOperation? Operation;
        internal int CancelCount, RetireCount, CloseCount;

        public WindowsHidReportMetadata Open(string devicePath)
        {
            Worker = Thread.CurrentThread;
            return new(0xf1d0, 1, 65, 65);
        }

        public WindowsHidSubmission Submit(WindowsHidOverlappedOperation operation)
        {
            Operation = operation;
            // Seed failed input staging so zeroing assertions are meaningful without native access.
            if (operation.Direction == WindowsHidReportDirection.Input) operation.NativeReport.AsSpan().Fill(7);
            Submitted.SetResult();
            gate.Wait();
            operation.PreparationFailedBeforeNativeDispatch = true;
            throw Error;
        }

        public uint Cancel(WindowsHidOverlappedOperation operation) { CancelCount++; return 0; }
        public void Retire(WindowsHidOverlappedOperation operation) => RetireCount++;
        public WindowsHidRelease CloseOnce() { CloseCount++; return new(true, 0); }
    }
}