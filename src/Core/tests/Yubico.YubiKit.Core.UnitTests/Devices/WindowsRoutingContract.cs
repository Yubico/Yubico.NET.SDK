// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.Windows;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public abstract class WindowsRoutingContract
{
    protected abstract bool Otp { get; }
    protected static CancellationToken Token => TestContext.Current.CancellationToken;

    private protected HidConnectionSlot Slot(Func<WindowsRoutingApi> factory) => new(
        new WindowsHidInterface(new HidDescriptorInfo
        {
            DevicePath = $"windows-routing-{Guid.NewGuid():N}",
            VendorId = 0x1050,
            ProductId = 0x407,
            UsagePage = Otp ? (ushort)1 : (ushort)0xf1d0,
            Usage = Otp ? (ushort)6 : (ushort)1
        }), windowsReportApiFactory: () => factory(), windowsFeatureReportApiFactory: () => factory());

    private YubiKeyDevice Device(HidConnectionSlot slot) => new($"windows-{Guid.NewGuid():N}",
        new SmartCardMember(), Otp ? null : slot, Otp ? slot : null, null);

    private async Task<IConnection> Open(YubiKeyDevice device, CancellationToken token) => Otp
        ? await device.ConnectAsync<IOtpHidConnection>(token)
        : await device.ConnectAsync<IFidoHidConnection>(token);

    private WindowsRoutingApi Api() => new() { Length = Otp ? (ushort)9 : (ushort)65 };

    [Fact]
    public async Task PublicOpen_HeldNativeOpenAndCloseRetainClaimAndReopenUsesFreshApi()
    {
        using var openGate = new ManualResetEventSlim();
        using var closeGate = new ManualResetEventSlim();
        var opening = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = Api();
        var second = Api();
        int acquisitions = 0;
        var slot = Slot(() => Interlocked.Increment(ref acquisitions) == 1 ? first : second);
        var device = Device(slot);
        first.Before = operation =>
        {
            if (operation == "open") { opening.SetResult(); openGate.Wait(Token); }
            if (operation == "close") { closing.SetResult(); closeGate.Wait(Token); }
        };
        Task<IConnection>? pending = null;
        Task<Task<IConnection>>? invocation = null;
        IConnection? connection = null;
        try
        {
            // Invocation itself must return while native Open is held.
            invocation = Task.Factory.StartNew(() => Open(device, Token), Token,
                TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            pending = await invocation.WaitAsync(TimeSpan.FromSeconds(5), Token);
            await opening.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.False(pending.IsCompleted);
            Assert.True(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
            Assert.All(device.InterfaceIds, id => Assert.True(DeviceConnectionRegistry.IsInUse(id)));
            await Assert.ThrowsAsync<ConnectionInUseException>(() => device.ConnectAsync<ISmartCardConnection>(Token));
            await Assert.ThrowsAsync<ConnectionInUseException>(() => Open(device, Token));
            openGate.Set();
            connection = await pending;
            Assert.IsType(Otp ? typeof(WindowsOtpHidConnection) : typeof(WindowsFidoHidConnection), connection);
            var closed = connection.DisposeAsync().AsTask();
            await closing.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.False(closed.IsCompleted);
            await Assert.ThrowsAsync<ConnectionInUseException>(() => Open(device, Token));
            closeGate.Set();
            await closed;
            await using var reopened = await Open(device, Token);
            Assert.Equal(2, acquisitions);
            Assert.Equal(1, first.Opens);
            Assert.Equal(1, second.Opens);
        }
        finally
        {
            openGate.Set(); closeGate.Set();
            // Drain original workers even if the test token cancels before releasing gates.
            if (pending is null && invocation is not null) pending = await invocation.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            if (connection is null && pending is not null) connection = await pending.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            if (connection is not null) await connection.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
    }

    [Fact]
    public async Task FactoryFailure_ReleasesTransferredClaimAndAllowsFreshAcquisition()
    {
        int calls = 0;
        var api = Api();
        var slot = Slot(() => ++calls == 1 ? throw new IOException("factory") : api);
        var device = Device(slot);
        await Assert.ThrowsAsync<IOException>(() => Open(device, Token));
        Assert.False(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
        await using var retry = await Open(device, Token);
        Assert.Equal(2, calls);
        Assert.Equal(1, api.Opens);
    }

    [Fact]
    public async Task PreCanceledRegisteredOpen_ReleasesExactClaimWithoutNativeOpen()
    {
        var api = Api();
        var slot = Slot(() => api);
        using var canceled = CancellationTokenSource.CreateLinkedTokenSource(Token);
        canceled.Cancel();
        var claim = new Claim();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => slot.OpenRegisteredConnectionAsync(claim, canceled.Token));
        Assert.Equal(1, claim.Releases);
        Assert.Equal(0, api.Opens);
    }

    [Fact]
    public async Task FactoryFailure_NoNativePathReleasesExactClaimOnceAndPreservesReleaseError()
    {
        var factoryError = new IOException("factory");
        var releaseError = new IOException("claim release");
        var claim = new Claim { Error = releaseError };
        var slot = Slot(() => throw factoryError);
        var failure = await Assert.ThrowsAsync<AggregateException>(() => slot.OpenRegisteredConnectionAsync(claim, Token));
        Assert.Equal(1, claim.Releases);
        Assert.Equal<Exception>([factoryError, releaseError], failure.InnerExceptions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DiscoveryFactoryFailure_ReleasesTransferredLeaseBeforeRetry(bool composite)
    {
        int calls = 0;
        var api = Api();
        var slot = Slot(() => ++calls == 1 ? throw new IOException("factory") : api);
        var device = Device(slot);
        var scope = new DiscoveryReadScope();
        try
        {
            var read = composite
                ? DiscoveryDeviceInfoReader.ReadBoundedAsync(device, slot.ConnectionType, TimeSpan.FromSeconds(5), NullLogger.Instance, Token, true, scope)
                : DiscoveryDeviceInfoReader.ReadSlotBoundedAsync(slot, slot.ConnectionType, TimeSpan.FromSeconds(5), NullLogger.Instance, Token, true, scope);
            await Assert.ThrowsAsync<IOException>(() => read);
            Assert.False(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
            await using var retry = await Open(device, Token);
            Assert.Equal(2, calls);
            Assert.Equal(1, api.Opens);
        }
        finally { scope.Retire(); }
    }

    [Fact]
    public async Task CreationCancellation_HeldOpenRetainsClaimUntilCheckedCloseAndAllowsRetry()
    {
        using var gate = new ManualResetEventSlim();
        using var closeGate = new ManualResetEventSlim();
        using var canceled = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var api = Api();
        var replacement = Api();
        int calls = 0;
        var slot = Slot(() => ++calls == 1 ? api : replacement);
        var device = Device(slot);
        api.Before = operation =>
        {
            if (operation == "open") { entered.SetResult(); gate.Wait(Token); }
            if (operation == "close") { closing.SetResult(); closeGate.Wait(Token); }
        };
        var open = Open(device, canceled.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            canceled.Cancel();
            Assert.False(open.IsCompleted);
            Assert.All(device.InterfaceIds, id => Assert.True(DeviceConnectionRegistry.IsInUse(id)));
            gate.Set();
            await closing.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.False(open.IsCompleted);
            await Assert.ThrowsAsync<ConnectionInUseException>(() => Open(device, Token));
            closeGate.Set();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => open);
            await using var retry = await Open(device, Token);
        }
        finally
        {
            gate.Set(); closeGate.Set();
            // A canceled caller still owns this observation until native cleanup finishes.
            try { await open.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None); }
            catch (OperationCanceledException) { }
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task DiscoveryTimeout_DirectAndPublishedRetainOriginalLeaseUntilLateCleanup(bool composite, bool cancel)
    {
        using var gate = new ManualResetEventSlim();
        using var closeGate = new ManualResetEventSlim();
        using var canceled = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var api = Api();
        api.Before = operation =>
        {
            if (operation == "open") { entered.SetResult(); gate.Wait(Token); }
            if (operation == "close") { closing.SetResult(); closeGate.Wait(Token); }
        };
        api.OpenError = new IOException("late open failure");
        var slot = Slot(() => api);
        var device = Device(slot);
        var scope = new DiscoveryReadScope();
        scope.CoordinateUsbRead(slot.InterfaceId, 0x407);
        Task? recovery = null;
        try
        {
            var read = composite
                ? DiscoveryDeviceInfoReader.ReadBoundedAsync(device, slot.ConnectionType, TimeSpan.FromMilliseconds(100), NullLogger.Instance, canceled.Token, true, scope)
                : DiscoveryDeviceInfoReader.ReadSlotBoundedAsync(slot, slot.ConnectionType, TimeSpan.FromMilliseconds(100), NullLogger.Instance, canceled.Token, true, scope);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            if (cancel)
            {
                canceled.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
            }
            else await Assert.ThrowsAsync<TimeoutException>(() => read);
            await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => Open(device, Token));
            recovery = DeviceConnectionRegistry.WaitForRecoveryForTest(slot.InterfaceId);
            scope.Retire();
            gate.Set();
            await closing.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.False(recovery.IsCompleted);
            Assert.All(DiscoveryReadAdmission.GatesFor(0x407), admission => Assert.Equal(0, admission.CurrentCount));
            await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => Open(device, Token));
            closeGate.Set();
            await recovery.WaitAsync(TimeSpan.FromSeconds(5), Token);
            Assert.Equal(1, api.Closes);
            Assert.False(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
        }
        finally
        {
            scope.Retire(); gate.Set(); closeGate.Set();
            // The discovery worker owns the late native call; join recovery before retiring its gate.
            if (recovery is not null) await recovery.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(CancellationToken.None);
        }
    }

    private sealed class Claim : IDisposable
    {
        internal int Releases;
        internal Exception? Error { get; init; }
        public void Dispose()
        {
            Interlocked.Increment(ref Releases);
            if (Error is not null) throw Error;
        }
    }

    private sealed class SmartCardMember : IYubiKeyConnectionSlot
    {
        public string InterfaceId { get; } = $"pcsc:windows-routing-{Guid.NewGuid():N}";
        public ConnectionType ConnectionType => ConnectionType.SmartCard;
    }

    [Fact]
    public async Task PublicRawSession_BorrowedIdleAndActiveReusePreservesOriginalOwner()
    {
        var api = Api();
        api.SessionReports = true;
        var slot = Slot(() => api);
        var device = Device(slot);
        await using var connection = await Open(device, Token);
        if (Otp)
        {
            var typed = Assert.IsAssignableFrom<IOtpHidConnection>(connection);
            await using (var idle = await RawOtpHidSession.CreateAsync(typed, Token))
                await Assert.ThrowsAsync<ConnectionInUseException>(() => RawOtpHidSession.CreateAsync(typed, Token));
            Assert.Equal(0, api.Gets);
            for (int i = 0; i < 2; i++)
            {
                api.Reports.Enqueue([0, 5, 8, 0, 0, 0, 0, 0]);
                for (int j = 0; j < 11; j++) api.Reports.Enqueue([0, 5, 8, 0, 1, 0, 0, 0]);
                api.Reports.Enqueue([0xAC, 0, 0, 0, 0, 0, 0, OtpConstants.ResponsePendingFlag]);
                api.Reports.Enqueue([0, 0, 0, 0, 0, 0, 0, OtpConstants.ResponsePendingFlag]);
                await using var active = await RawOtpHidSession.CreateAsync(typed, Token);
                Assert.Equal(0xAC, (await active.SendAndReceiveAsync(OtpConstants.CmdYk4Capabilities, ReadOnlyMemory<byte>.Empty, Token)).Span[0]);
            }
            Assert.Equal(22, api.Sends);
        }
        else
        {
            var typed = Assert.IsAssignableFrom<IFidoHidConnection>(connection);
            await using (var idle = await RawCtapHidSession.CreateAsync(typed, Token))
                await Assert.ThrowsAsync<ConnectionInUseException>(() => RawCtapHidSession.CreateAsync(typed, Token));
            Assert.Equal(0, api.Sends);
            for (int i = 0; i < 2; i++)
            {
                await using var active = await RawCtapHidSession.CreateAsync(typed, Token);
                Assert.Equal(0xAC, (await active.SendAndReceiveAsync(0x41, ReadOnlyMemory<byte>.Empty, Token)).Span[0]);
            }
            Assert.Equal(4, api.Sends);
        }
        Assert.Equal(0, api.Closes);
        Assert.Equal(1, api.Opens);
        await Assert.ThrowsAsync<ConnectionInUseException>(() => Open(device, Token));
    }

    [Fact]
    public Task UncertainClose_PublicAndDiscoveryKeepOriginalClaimsQuarantined() =>
        PcscIsolatedProbe.RunAsync($"windows-routing-{Otp}", GetType(),
            nameof(UncertainClose_PublicAndDiscoveryKeepOriginalClaimsQuarantined), async () =>
            {
                var api = Api();
                api.Release = new(false, 5);
                var slot = Slot(() => api);
                var device = Device(slot);
                var connection = await Open(device, Token);
                var failure = await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask());
                Assert.Same(failure, await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask()));
                await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => Open(device, Token));
                Assert.Equal(1, api.Closes);
                foreach (bool composite in new[] { false, true })
                {
                    var failedApi = Api();
                    failedApi.OpenError = new IOException("open");
                    failedApi.Release = new(false, 5);
                    var failedSlot = Slot(() => failedApi);
                    var failedDevice = Device(failedSlot);
                    var scope = new DiscoveryReadScope();
                    try
                    {
                        var read = composite
                            ? DiscoveryDeviceInfoReader.ReadBoundedAsync(failedDevice, failedSlot.ConnectionType, TimeSpan.FromSeconds(5), NullLogger.Instance, Token, true, scope)
                            : DiscoveryDeviceInfoReader.ReadSlotBoundedAsync(failedSlot, failedSlot.ConnectionType, TimeSpan.FromSeconds(5), NullLogger.Instance, Token, true, scope);
                        await Assert.ThrowsAnyAsync<Exception>(() => read);
                        await Assert.ThrowsAsync<UnrecoveredConnectionException>(() => Open(failedDevice, Token));
                        Assert.Throws<UnrecoveredConnectionException>(() => DeviceConnectionRegistry.TryAcquireDiscovery(failedSlot.InterfaceId));
                        Assert.Equal(1, failedApi.Closes);
                    }
                    finally { scope.Retire(); }
                }
            });
}