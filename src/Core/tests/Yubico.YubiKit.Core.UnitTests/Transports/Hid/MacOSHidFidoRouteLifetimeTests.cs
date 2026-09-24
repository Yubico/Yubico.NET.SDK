using Yubico.YubiKit.Core.Transports.Hid.MacOS;
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Sessions;
using System.Buffers.Binary;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

public class MacOSHidFidoRouteLifetimeTests
{
    [Fact]
    public async Task RegisteredMacSlotClaimsBeforeOpenAndReleasesAfterTeardown()
    {
        var bridge = new ControlledBridge();
        var descriptor = new HidDescriptorInfo { VendorId = 0x1050, ProductId = 0x0407,
            UsagePage = 0xF1D0, Usage = 1 };
        var slot = new HidConnectionSlot(new MacOSHidInterface(13579, descriptor), bridge);
        var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([slot.InterfaceId], TestContext.Current.CancellationToken);
        var opening = slot.OpenRegisteredConnectionAsync(claim, TestContext.Current.CancellationToken);
        await bridge.OpenEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
        bridge.ReleaseOpen.SetResult();
        var connection = await opening.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await connection.DisposeAsync();
        Assert.False(DeviceConnectionRegistry.IsInUse(slot.InterfaceId));
    }

    [Fact]
    public async Task ApplicationSessionCreationAwaitsWireInitWithoutBlockingCaller()
    {
        var bridge = new ControlledBridge { HoldSend = true, RespondToInit = true }; bridge.ReleaseOpen.SetResult();
        await using var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        var creating = InitializingSession.CreateAsync(connection, TestContext.Current.CancellationToken);
        await bridge.SendEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.False(creating.IsCompleted);
        bridge.ReleaseSend.SetResult();
        await using var session = await creating.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(session.IsInitialized);
        Assert.Equal(1, bridge.SendCount);
    }

    [Fact]
    public async Task KeepaliveCancellationSendsOneCancelDrainsAndReusesTheSameChannel()
    {
        using var abandoned = new CancellationTokenSource();
        var bridge = new ControlledBridge { RespondToInit = true, RespondToVendor = true,
            CancelAfterKeepalive = abandoned }; bridge.ReleaseOpen.SetResult();
        await using var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        using var protocol = new FidoHidProtocol(connection);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => protocol.SendVendorCommandAsync(
            0x41, ReadOnlyMemory<byte>.Empty, abandoned.Token));
        Assert.Equal(1, bridge.CancelCount);
        var response = await protocol.SendVendorCommandAsync(0x41, ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken);
        Assert.Equal((byte)0xAC, response.Span[0]);
        Assert.Equal(1, bridge.CancelCount);
        Assert.Equal(2, bridge.VendorCount);
        Assert.All(bridge.SentPackets.Skip(1), packet =>
            Assert.Equal(0x01020304u, BinaryPrimitives.ReadUInt32BigEndian(packet)));
    }

    private sealed class InitializingSession(IFidoHidConnection connection) : ApplicationSession(connection)
    {
        internal static async Task<InitializingSession> CreateAsync(IFidoHidConnection connection, CancellationToken token)
        {
            var session = Construct(connection, () => new InitializingSession(connection));
            try
            {
                await session.InitializeProtocolAsync(new FidoHidProtocol(connection),
                    new FirmwareVersion(5, 8, 0), cancellationToken: token);
                return session;
            }
            catch { session.DisposeAfterInitializationFailure(); throw; }
        }
    }

    [Fact]
    public async Task OpenDoesNotBlockCallerAndReceivesReportsWithoutPumping()
    {
        var bridge = new ControlledBridge();
        var opening = MacOSFidoHidConnection.OpenAsync(1, bridge, CancellationToken.None);
        await bridge.OpenEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.False(opening.IsCompleted);
        bridge.ReleaseOpen.SetResult();
        await using var connection = await opening.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var receiving = connection.ReceiveAsync(TestContext.Current.CancellationToken);
        bridge.Report?.Invoke(new byte[64]);
        Assert.Equal(64, (await receiving.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Length);
    }

    [Fact]
    public async Task ReceiveAndSendOverlapIsRefusedWithoutASecondSubmission()
    {
        var bridge = new ControlledBridge(); bridge.ReleaseOpen.SetResult();
        await using var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        var read = connection.ReceiveAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => connection.SendAsync(new byte[64], TestContext.Current.CancellationToken));
        Assert.Equal(0, bridge.SendCount);
        bridge.Report?.Invoke(new byte[64]);
        await read.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        bridge.HoldSend = true;
        var send = connection.SendAsync(new byte[64], TestContext.Current.CancellationToken);
        await bridge.SendEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => connection.ReceiveAsync(TestContext.Current.CancellationToken));
        bridge.ReleaseSend.SetResult();
        await send.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DisposalWakesReceiveAndWaitsForCancelAcknowledgmentBeforeRelease()
    {
        var bridge = new ControlledBridge { HoldAck = true }; bridge.ReleaseOpen.SetResult();
        var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        var read = connection.ReceiveAsync(TestContext.Current.CancellationToken);
        var disposal = connection.DisposeAsync().AsTask();
        await Assert.ThrowsAnyAsync<Exception>(() => read);
        await bridge.AckEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.False(disposal.IsCompleted);
        Assert.Equal(0, bridge.ReleaseCount);
        bridge.ReleaseAck.SetResult();
        await disposal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(1, bridge.ReleaseCount);
    }

    [Fact]
    public async Task ManagedOverflowTerminatesPendingAndFutureReads()
    {
        var bridge = new ControlledBridge(); bridge.ReleaseOpen.SetResult();
        await using var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        for (var i = 0; i < 161; i++) bridge.Report?.Invoke(new byte[64]);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.ReceiveAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task NativeTerminalWakesPendingReceiveAndLateOldReportCannotCrossOwners(int reason)
    {
        var first = new ControlledBridge(); first.ReleaseOpen.SetResult();
        await using var oldConnection = await MacOSFidoHidConnection.OpenAsync(1, first, TestContext.Current.CancellationToken);
        var oldReport = first.Report;
        var pending = oldConnection.ReceiveAsync(TestContext.Current.CancellationToken);
        first.Terminal?.Invoke(reason);
        await Assert.ThrowsAsync<InvalidOperationException>(() => pending);

        var second = new ControlledBridge(); second.ReleaseOpen.SetResult();
        await using var replacement = await MacOSFidoHidConnection.OpenAsync(1, second, TestContext.Current.CancellationToken);
        oldReport?.Invoke(new byte[64]);
        var receiving = replacement.ReceiveAsync(TestContext.Current.CancellationToken);
        Assert.False(receiving.IsCompleted);
        second.Report?.Invoke(new byte[64]);
        Assert.Equal(64, (await receiving.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Length);
    }

    [Fact]
    public async Task CloseFaultDoesNotReleasePhysicalClaimOrRetry()
    {
        await PcscIsolatedProbe.RunAsync("hid-close-fault", typeof(MacOSHidFidoRouteLifetimeTests),
            nameof(CloseFaultDoesNotReleasePhysicalClaimOrRetry), async () =>
            {
                var bridge = new ControlledBridge { CloseResult = 6 }; bridge.ReleaseOpen.SetResult();
                const string interfaceId = "hid:close-fault-test";
                var claim = await DeviceConnectionRegistry.AcquireConnectionAsync([interfaceId], TestContext.Current.CancellationToken);
                var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken, claim);
                await Assert.ThrowsAsync<Yubico.YubiKit.Core.Devices.UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask());
                await Assert.ThrowsAsync<Yubico.YubiKit.Core.Devices.UnrecoveredConnectionException>(() => connection.DisposeAsync().AsTask());
                Assert.Equal(1, bridge.DestroyCount);
                Assert.Equal(0, bridge.ReleaseCount);
                Assert.True(DeviceConnectionRegistry.IsInUse(interfaceId));
                Assert.Throws<UnrecoveredConnectionException>(() => DeviceConnectionRegistry.TryAcquireDiscovery(interfaceId));
            });
    }

    [Fact]
    public async Task FailedOpenBeforeNativeOwnershipReleasesDeviceAndClaim()
    {
        var bridge = new ControlledBridge { ThrowOnOpen = true }; bridge.ReleaseOpen.SetResult();
        var claim = new RecordingClaim();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken, claim));
        Assert.Equal(1, bridge.ReleaseCount);
        Assert.True(claim.Released);
        Assert.Equal(0, bridge.CloseUnstartedCount);
    }

    [Fact]
    public async Task ExclusiveAccessOpenRequiresCheckedCloseBeforeClaimRelease()
    {
        var bridge = new ControlledBridge { ExclusiveOpen = true };
        bridge.ReleaseOpen.SetResult();
        var claim = new RecordingClaim();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken, claim));

        Assert.Equal(1, bridge.CloseUnstartedCount);
        Assert.Equal(1, bridge.ReleaseCount);
        Assert.True(claim.Released);
    }

    [Fact]
    public async Task CancelledBeforeWorkerDispatchMakesNoNativeCall()
    {
        var bridge = new ControlledBridge();
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            MacOSFidoHidConnection.OpenAsync(1, bridge, cancelled.Token));
        Assert.False(bridge.OpenEntered.Task.IsCompleted);
    }

    [Fact]
    public async Task HeldOutputDelaysSynchronousDisposalAndRetainsBorrowUntilNativeReturn()
    {
        var bridge = new ControlledBridge { HoldSend = true }; bridge.ReleaseOpen.SetResult();
        var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        var input = new byte[64]; input[0] = 42;
        var send = connection.SendAsync(input, TestContext.Current.CancellationToken);
        await bridge.SendEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        input[0] = 0;
        var disposal = Task.Run(connection.Dispose, TestContext.Current.CancellationToken);
        Assert.False(disposal.IsCompleted);
        Assert.Equal(0, bridge.ReleaseCount);
        bridge.ReleaseSend.SetResult();
        await send.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await disposal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal((byte)42, bridge.ObservedOutput);
        Assert.All(bridge.LastReport ?? [], b => Assert.Equal((byte)0, b));
        Assert.Equal(1, bridge.ReleaseCount);
    }

    [Fact]
    public async Task ShutdownBetweenSendAdmissionAndWorkerPublicationDrainsAcceptedOutput()
    {
        using var entered = new ManualResetEventSlim();
        using var publish = new ManualResetEventSlim();
        var bridge = new ControlledBridge(); bridge.ReleaseOpen.SetResult();
        var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken,
            beforeSendPublication: () => { entered.Set(); publish.Wait(); });
        var sending = Task.Run(() => connection.SendAsync(new byte[64], TestContext.Current.CancellationToken),
            TestContext.Current.CancellationToken);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            var disposing = Task.Run(async () => await connection.DisposeAsync(), TestContext.Current.CancellationToken);
            Assert.Equal(0, bridge.ReleaseCount);
            publish.Set();
            await sending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            await disposing.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Equal(1, bridge.SendCount);
            Assert.Equal(1, bridge.ReleaseCount);
        }
        finally { publish.Set(); }
    }

    [Fact]
    public void NativeCallbackStateContainsReportAndTerminalExceptions()
    {
        var faults = 0;
        var state = new NativeHidInputBridge.CallbackState(
            _ => throw new InvalidOperationException("report callback failed"),
            _ => { faults++; throw new InvalidOperationException("terminal callback failed"); });
        state.Deliver(new byte[64]);
        state.Terminal(1);
        Assert.Equal(2, faults);
    }

    [Fact]
    public async Task DroppedWrapperRetainsCallbackStateUntilNativeAcknowledgment()
    {
        var bridge = new ControlledBridge { HoldAck = true }; bridge.ReleaseOpen.SetResult();
        var weak = await DropWrapper(bridge);
        for (var attempt = 0; attempt < 10 && !bridge.AckEntered.Task.IsCompleted; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
        await bridge.AckEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.False(weak.IsAlive);
        Assert.Equal(0, bridge.ReleaseCount);
        bridge.Report?.Invoke(new byte[64]); // callback state must still be safe before native quiescence
        bridge.ReleaseAck.SetResult();
        for (var attempt = 0; attempt < 20 && bridge.ReleaseCount == 0; attempt++)
            await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.Equal(1, bridge.ReleaseCount);
    }

    private static async Task<WeakReference> DropWrapper(ControlledBridge bridge)
    {
        var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        return new WeakReference(connection);
    }

    private sealed class RecordingClaim : IDisposable
    {
        public bool Released;
        public void Dispose() => Released = true;
    }

    private sealed class ControlledBridge : IHidInputBridge
    {
        public readonly TaskCompletionSource OpenEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource ReleaseOpen = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource SendEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource ReleaseSend = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource AckEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource ReleaseAck = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool HoldSend;
        public bool RespondToInit;
        public bool RespondToVendor;
        public CancellationTokenSource? CancelAfterKeepalive;
        public int CancelCount;
        public int VendorCount;
        public readonly List<byte[]> SentPackets = [];
        public bool ThrowOnOpen;
        public bool ExclusiveOpen;
        public int CloseUnstartedCount;
        public bool HoldAck;
        public int CloseResult;
        public int ReleaseCount;
        public int DestroyCount;
        public int SendCount;
        public byte ObservedOutput;
        public byte[]? LastReport;
        public Action<ReadOnlyMemory<byte>>? Report;
        public Action<int>? Terminal;

        public nint CreateDevice(long entryId) { OpenEntered.SetResult(); ReleaseOpen.Task.GetAwaiter().GetResult(); return 1; }
        public void OpenDevice(nint device) { if (ThrowOnOpen) throw new InvalidOperationException("open failed"); }
        public int OpenDeviceResult(nint device)
        {
            if (ThrowOnOpen) return -1;
            return ExclusiveOpen ? unchecked((int)0xE00002C5) : 0;
        }
        public int InputSize(nint device) => 64;
        public void ReleaseDevice(nint device) { Interlocked.Increment(ref ReleaseCount); }
        public bool CloseUnstarted(nint device) { CloseUnstartedCount++; return true; }
        public nint CreateInput(nint device, int size, Action<ReadOnlyMemory<byte>> report, Action<int> terminal)
        { Report = report; Terminal = terminal; return 2; }
        public int Start(nint owner) => 0;
        public void Cancel(nint owner) { }
        public int WaitShutdown(nint owner)
        { AckEntered.TrySetResult(); if (HoldAck) ReleaseAck.Task.GetAwaiter().GetResult(); return 0; }
        public int Destroy(nint owner) { Interlocked.Increment(ref DestroyCount); return CloseResult; }
        public void SetReport(nint device, byte[] report)
        {
            Interlocked.Increment(ref SendCount); SendEntered.TrySetResult();
            if (HoldSend) ReleaseSend.Task.GetAwaiter().GetResult();
            ObservedOutput = report[0];
            LastReport = report;
            var snapshot = report.ToArray();
            lock (SentPackets) SentPackets.Add(snapshot);
            if (RespondToInit && report[4] == 0x86)
            {
                var response = new byte[64];
                BinaryPrimitives.WriteUInt32BigEndian(response, uint.MaxValue);
                response[4] = 0x86;
                response[6] = 17;
                report.AsSpan(7, 8).CopyTo(response.AsSpan(7));
                BinaryPrimitives.WriteUInt32BigEndian(response.AsSpan(15), 0x01020304);
                response[19] = 2; response[20] = 5; response[21] = 8;
                Report?.Invoke(response);
            }
            else if (RespondToVendor && report[4] == 0xC1)
            {
                VendorCount++;
                if (VendorCount == 1)
                {
                    Report?.Invoke(CreateResponse(0xBB, 2));
                    CancelAfterKeepalive?.Cancel();
                }
                else Report?.Invoke(CreateResponse(0x41, 0xAC));
            }
            else if (RespondToVendor && report[4] == 0x91)
            {
                CancelCount++;
                Report?.Invoke(CreateResponse(0x41, 0x2D));
            }
        }

        private static byte[] CreateResponse(byte command, byte payload)
        {
            var packet = new byte[64];
            BinaryPrimitives.WriteUInt32BigEndian(packet, 0x01020304);
            packet[4] = (byte)(0x80 | command);
            packet[6] = 1;
            packet[7] = payload;
            return packet;
        }
    }
}
