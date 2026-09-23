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

using System.Runtime.CompilerServices;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.Devices;
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Transports.SmartCard;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class PcscConnectionLifetimeTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AsyncTransaction_ExternalSyncOnlyImplementation_UsesDefaultFallback()
    {
        ISmartCardConnection connection = new SyncOnlyTransactionConnection();
        using var scope = await connection.BeginTransactionAsync(Ct);
        Assert.Equal(1, ((SyncOnlyTransactionConnection)connection).BeginCalls);
    }

    [Fact]
    public async Task AsyncTransaction_BlockedNativeBegin_ReturnsPendingTaskWithoutBlockingCaller()
    {
        var api = new ControlledSCardConnectionApi { HoldBegin = true };
        await using var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        Task<IDisposable>? begin = null;
        try
        {
            begin = connection.BeginTransactionAsync(Ct);
            await api.BeginEntered.Task.WaitAsync(Ct);
            Assert.False(begin.IsCompleted);
        }
        finally
        {
            api.ReleaseBegin.Set();
            if (begin is not null)
            {
                using var scope = await begin;
            }
        }
        Assert.Equal(1, api.EndTransactionCalls);
    }

    [Fact]
    public async Task AsyncTransaction_PreCanceledToken_DoesNotBeginNativeTransaction()
    {
        var api = new ControlledSCardConnectionApi();
        await using var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => connection.BeginTransactionAsync(cancellation.Token));
        Assert.Equal(0, api.BeginTransactionCalls);
    }

    [Fact]
    public async Task AsyncTransaction_ShutdownDuringNativeBegin_EndsLateSuccessBeforeRelease()
    {
        var api = new ControlledSCardConnectionApi { HoldBegin = true };
        var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        try
        {
            var begin = connection.BeginTransactionAsync(Ct);
            await api.BeginEntered.Task.WaitAsync(Ct);
            var shutdown = connection.DisposeAsync().AsTask();
            Assert.False(shutdown.IsCompleted);
            api.ReleaseBegin.Set();
            var scope = await begin;
            await shutdown;
            await ((IAsyncDisposable)scope).DisposeAsync();
            Assert.Equal(["establish", "connect", "begin-enter", "begin-exit", "end", "disconnect", "release-context"], api.Events);
        }
        finally
        {
            api.ReleaseBegin.Set();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task AsyncTransaction_OverlappingBeginIsRefusedWithoutSecondNativeCall()
    {
        var api = new ControlledSCardConnectionApi { HoldBegin = true };
        await using var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        var begin = connection.BeginTransactionAsync(Ct);
        try
        {
            await api.BeginEntered.Task.WaitAsync(Ct);
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.BeginTransactionAsync(Ct));
            Assert.Equal(1, api.BeginTransactionCalls);
        }
        finally
        {
            api.ReleaseBegin.Set();
            using var scope = await begin;
        }
    }

    [Fact]
    public async Task AsyncTransaction_CancellationAfterNativeDispatch_DoesNotAbandonSuccessfulBegin()
    {
        var api = new ControlledSCardConnectionApi { HoldBegin = true };
        await using var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        using var cancellation = new CancellationTokenSource();
        var begin = connection.BeginTransactionAsync(cancellation.Token);
        try
        {
            await api.BeginEntered.Task.WaitAsync(Ct);
            cancellation.Cancel();
            Assert.False(begin.IsCompleted);
            api.ReleaseBegin.Set();
            IDisposable scope = await begin;
            await ((IAsyncDisposable)scope).DisposeAsync();
            Assert.Equal(1, api.EndTransactionCalls);
        }
        finally
        {
            api.ReleaseBegin.Set();
        }
    }

    [Fact]
    public async Task AsyncTransaction_AsyncScopeEndAwaitsNativeEndAndSyncBeginStillWorks()
    {
        var api = new ControlledSCardConnectionApi();
        var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        try
        {
            IDisposable scope = await connection.BeginTransactionAsync(Ct);
            var transmit = connection.TransmitAndReceiveAsync(new byte[] { 0x00 }, Ct);
            await api.FirstTransmitEntered.Task.WaitAsync(Ct);
            var end = ((IAsyncDisposable)scope).DisposeAsync().AsTask();
            Assert.False(end.IsCompleted);
            Assert.Equal(0, api.EndTransactionCalls);
            api.ReleaseTransmit.Set();
            _ = await transmit;
            await end;
            scope.Dispose();
            Assert.Equal(1, api.EndTransactionCalls);
            using (connection.BeginTransaction(Ct)) { }
            Assert.Equal(2, api.EndTransactionCalls);
        }
        finally
        {
            api.ReleaseTransmit.Set();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task AsyncTransaction_FailedAsyncEnd_RefusesFurtherWorkAndShutdownReportsFailure()
    {
        var api = new ControlledSCardConnectionApi
        {
            EndTransactionResult = ErrorCode.SCARD_E_NOT_TRANSACTED
        };
        var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        IDisposable scope = await connection.BeginTransactionAsync(Ct);

        await ((IAsyncDisposable)scope).DisposeAsync();
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => connection.TransmitAndReceiveAsync(new byte[] { 0x00 }, Ct));
        _ = await Assert.ThrowsAsync<SCardException>(() => connection.DisposeAsync().AsTask());
        Assert.Equal(1, api.EndTransactionCalls);
        Assert.Equal(1, api.DisconnectCalls);
        Assert.Equal(1, api.ReleaseContextCalls);
    }

    [Fact]
    public async Task BuiltInPcscConnection_OverlappingRawTransmits_RefusesSecondWithoutNativeSubmission()
    {
        var api = new ControlledSCardConnectionApi();
        var device = PcscTestDevices.Create(api);
        ISmartCardConnection? connection = null;
        Task<ReadOnlyMemory<byte>>? firstTransmit = null;

        try
        {
            connection = await device.ConnectAsync<ISmartCardConnection>(Ct);
            firstTransmit = connection.TransmitAndReceiveAsync(new byte[] { 0x00 }, Ct);
            await api.FirstTransmitEntered.Task.WaitAsync(Ct);

            _ = await Assert.ThrowsAsync<InvalidOperationException>(
                () => connection.TransmitAndReceiveAsync(new byte[] { 0x01 }, Ct));

            Assert.Equal(1, api.TransmitCalls);
        }
        finally
        {
            api.ReleaseTransmit.Set();
            if (firstTransmit is not null)
                _ = await firstTransmit;
            if (connection is not null)
                await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task BuiltInPcscConnection_DisposeDuringTransmit_WaitsThenReleasesOnLifetimeOwner()
    {
        var api = new ControlledSCardConnectionApi();
        var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        var transmit = connection.TransmitAndReceiveAsync(new byte[] { 0x00 }, Ct);

        try
        {
            await api.FirstTransmitEntered.Task.WaitAsync(Ct);
            var dispose = connection.DisposeAsync().AsTask();

            Assert.False(dispose.IsCompleted);
            Assert.Equal(0, api.DisconnectCalls);

            api.ReleaseTransmit.Set();
            _ = await transmit;
            await dispose;

            Assert.Equal(1, api.DisconnectCalls);
            Assert.Equal(1, api.ReleaseContextCalls);
            Assert.Single(api.NativeThreadIds.Distinct());
            Assert.Equal(["establish", "connect", "transmit-enter", "transmit-exit", "disconnect", "release-context"],
                api.Events);
        }
        finally
        {
            api.ReleaseTransmit.Set();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task BuiltInPcscConnection_CancellationAfterDispatch_DoesNotAbandonNativeBorrow()
    {
        var api = new ControlledSCardConnectionApi();
        await using var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        using var cancellation = new CancellationTokenSource();
        var transmit = connection.TransmitAndReceiveAsync(new byte[] { 0x00 }, cancellation.Token);

        try
        {
            await api.FirstTransmitEntered.Task.WaitAsync(Ct);
            cancellation.Cancel();

            Assert.False(transmit.IsCompleted);

            api.ReleaseTransmit.Set();
            ReadOnlyMemory<byte> response = await transmit;
            Assert.Equal(new byte[] { 0x90, 0x00 }, response.ToArray());
        }
        finally
        {
            api.ReleaseTransmit.Set();
        }
    }

    [Fact]
    public async Task BuiltInPcscConnection_CancellationBeforeDispatch_SubmitsNoNativeCall()
    {
        var api = new ControlledSCardConnectionApi();
        await using var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => connection.TransmitAndReceiveAsync(new byte[] { 0x00 }, cancellation.Token));

        Assert.Equal(0, api.TransmitCalls);
    }

    [Fact]
    public async Task BuiltInPcscConnection_TransactionEndAndShutdownDuringTransmit_AreOrderedAndCoalesced()
    {
        var api = new ControlledSCardConnectionApi();
        var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        var transaction = connection.BeginTransaction(Ct);
        var transmit = connection.TransmitAndReceiveAsync(new byte[] { 0x00 }, Ct);

        try
        {
            await api.FirstTransmitEntered.Task.WaitAsync(Ct);
            var end = Task.Run(transaction.Dispose, Ct);
            var dispose = connection.DisposeAsync().AsTask();

            Assert.False(end.IsCompleted);
            Assert.False(dispose.IsCompleted);

            api.ReleaseTransmit.Set();
            _ = await transmit;
            await end;
            await dispose;

            Assert.Equal(1, api.EndTransactionCalls);
            Assert.Equal(1, api.DisconnectCalls);
            Assert.Equal(
                ["establish", "connect", "begin", "transmit-enter", "transmit-exit", "end", "disconnect", "release-context"],
                api.Events);
        }
        finally
        {
            api.ReleaseTransmit.Set();
            transaction.Dispose();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task BuiltInPcscConnection_PartialOpenFailure_ReleasesContextAndPhysicalClaim()
    {
        var api = new ControlledSCardConnectionApi { FailNextConnect = true };
        var device = PcscTestDevices.Create(api);

        _ = await Assert.ThrowsAsync<SCardException>(() => device.ConnectAsync<ISmartCardConnection>(Ct));

        Assert.Equal(1, api.ReleaseContextCalls);
        await using var connection = await device.ConnectAsync<ISmartCardConnection>(Ct);
        Assert.Equal(2, api.ConnectCalls);
    }

    [Fact]
    public async Task BuiltInPcscConnection_EndFailure_StillReleasesNativeResourcesAndPhysicalClaim()
    {
        var api = new ControlledSCardConnectionApi
        {
            EndTransactionResult = ErrorCode.SCARD_E_NOT_TRANSACTED
        };
        var device = PcscTestDevices.Create(api);
        var connection = await device.ConnectAsync<ISmartCardConnection>(Ct);
        _ = connection.BeginTransaction(Ct);

        _ = await Assert.ThrowsAsync<SCardException>(() => connection.DisposeAsync().AsTask());

        Assert.Equal(1, api.EndTransactionCalls);
        Assert.Equal(1, api.DisconnectCalls);
        Assert.Equal(1, api.ReleaseContextCalls);
        await using var reopened = await device.ConnectAsync<ISmartCardConnection>(Ct);
    }

    [Fact]
    public async Task BuiltInPcscConnection_EndedTransaction_AllowsTransmitAndSecondTransaction()
    {
        var api = new ControlledSCardConnectionApi();
        api.ReleaseTransmit.Set();
        await using var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);

        var first = connection.BeginTransaction(Ct);
        first.Dispose();
        first.Dispose();

        _ = await connection.TransmitAndReceiveAsync(new byte[] { 0x00 }, Ct);

        var second = connection.BeginTransaction(Ct);
        first.Dispose();
        Assert.Equal(1, api.EndTransactionCalls);
        second.Dispose();

        Assert.Equal(2, api.EndTransactionCalls);
        Assert.Equal(2, api.BeginTransactionCalls);
    }

    [Fact]
    public async Task BuiltInPcscConnection_ShutdownDuringHeldBegin_EndsLateSuccessfulTransactionBeforeCleanup()
    {
        var api = new ControlledSCardConnectionApi { HoldBegin = true };
        var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        var begin = Task.Run(() => connection.BeginTransaction(Ct), Ct);

        try
        {
            await api.BeginEntered.Task.WaitAsync(Ct);
            var dispose = connection.DisposeAsync().AsTask();
            Assert.False(dispose.IsCompleted);

            api.ReleaseBegin.Set();
            var scope = await begin;
            await dispose;

            Assert.Equal(1, api.EndTransactionCalls);
            Assert.Equal(
                ["establish", "connect", "begin-enter", "begin-exit", "end", "disconnect", "release-context"],
                api.Events);

            var repeatedScopeDispose = Task.Run(scope.Dispose, Ct);
            await repeatedScopeDispose.WaitAsync(Ct);
        }
        finally
        {
            api.ReleaseBegin.Set();
        }
    }

    [Fact]
    public async Task BuiltInPcscConnection_ConcurrentDisposalOfSameScope_SharesOneEndCompletion()
    {
        var api = new ControlledSCardConnectionApi();
        var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        var transaction = connection.BeginTransaction(Ct);
        var transmit = connection.TransmitAndReceiveAsync(new byte[] { 0x00 }, Ct);

        try
        {
            await api.FirstTransmitEntered.Task.WaitAsync(Ct);
            var firstDispose = Task.Run(transaction.Dispose, Ct);
            var secondDispose = Task.Run(transaction.Dispose, Ct);
            Assert.False(firstDispose.IsCompleted);
            Assert.False(secondDispose.IsCompleted);

            api.ReleaseTransmit.Set();
            _ = await transmit;
            await Task.WhenAll(firstDispose, secondDispose);

            Assert.Equal(1, api.EndTransactionCalls);
        }
        finally
        {
            api.ReleaseTransmit.Set();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task BuiltInPcscConnection_ConcurrentBeginWhileFirstIsHeld_IsRefusedBeforeSecondSubmission()
    {
        var api = new ControlledSCardConnectionApi { HoldBegin = true };
        await using var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        var firstBegin = Task.Run(() => connection.BeginTransaction(Ct), Ct);

        try
        {
            await api.BeginEntered.Task.WaitAsync(Ct);
            _ = Assert.Throws<InvalidOperationException>(() => connection.BeginTransaction(Ct));
            Assert.Equal(1, api.BeginTransactionCalls);

            api.ReleaseBegin.Set();
            var scope = await firstBegin;
            scope.Dispose();
        }
        finally
        {
            api.ReleaseBegin.Set();
        }
    }

    [Fact]
    public async Task BuiltInPcscConnection_FailedScopeEnd_RefusesOrdinaryWorkButStillShutsDown()
    {
        var api = new ControlledSCardConnectionApi
        {
            EndTransactionResult = ErrorCode.SCARD_E_NOT_TRANSACTED
        };
        api.ReleaseTransmit.Set();
        var connection = await PcscTestDevices.Create(api).ConnectAsync<ISmartCardConnection>(Ct);
        var transaction = connection.BeginTransaction(Ct);

        transaction.Dispose();
        _ = await Assert.ThrowsAnyAsync<InvalidOperationException>(
            () => connection.TransmitAndReceiveAsync(new byte[] { 0x00 }, Ct));
        _ = await Assert.ThrowsAsync<SCardException>(() => connection.DisposeAsync().AsTask());

        Assert.Equal(1, api.DisconnectCalls);
        Assert.Equal(1, api.ReleaseContextCalls);
    }

    [Fact]
    public Task BuiltInPcscConnection_DroppedLiveWrapper_FinalizerRequestsCheckedShutdown() =>
        PcscIsolatedProbe.RunAsync(
            "finalizer-live-wrapper",
            typeof(PcscConnectionLifetimeTests),
            nameof(BuiltInPcscConnection_DroppedLiveWrapper_FinalizerRequestsCheckedShutdown),
            RunDroppedLiveWrapperFinalizerProbeAsync);

    [Fact]
    public Task BuiltInPcscConnection_DroppedUninitializedWrapper_ReleasesOwnerWithoutNativeCalls() =>
        PcscIsolatedProbe.RunAsync(
            "finalizer-uninitialized-wrapper",
            typeof(PcscConnectionLifetimeTests),
            nameof(BuiltInPcscConnection_DroppedUninitializedWrapper_ReleasesOwnerWithoutNativeCalls),
            RunDroppedUninitializedWrapperFinalizerProbeAsync);

    private static async Task RunDroppedLiveWrapperFinalizerProbeAsync()
    {
        var api = new ControlledSCardConnectionApi();
        WeakReference wrapper = OpenAndDropDirectConnection(api);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        await api.ContextReleased.Task.WaitAsync(Ct);

        Assert.False(wrapper.IsAlive);
        Assert.Equal(1, api.DisconnectCalls);
        Assert.Equal(1, api.ReleaseContextCalls);
    }

    private static async Task RunDroppedUninitializedWrapperFinalizerProbeAsync()
    {
        var api = new ControlledSCardConnectionApi();
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        WeakReference wrapper = CreateAndDropUninitializedConnection(api, released);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        await released.Task.WaitAsync(Ct);

        Assert.False(wrapper.IsAlive);
        Assert.Empty(api.Events);
    }

    [Fact]
    public async Task BuiltInPcscConnection_WorkerStartFailureFaultsOpenAndRepeatedDisposalWithoutNativeCalls()
    {
        var expected = new InvalidOperationException("worker start failed");
        var releasedCalls = 0;
        var connection = new UsbSmartCardConnection(
            new PcscDevice { ReaderName = $"start-failure-{Guid.NewGuid():N}", Atr = null },
            api: new ControlledSCardConnectionApi(),
            released: () => Interlocked.Increment(ref releasedCalls),
            startWorker: _ => throw expected);

        var openFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => connection.InitializeAsync(Ct).AsTask());
        var firstDisposeFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => connection.DisposeAsync().AsTask());
        var secondDisposeFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => connection.DisposeAsync().AsTask());

        Assert.Same(expected, openFailure);
        Assert.Same(firstDisposeFailure, secondDisposeFailure);
        Assert.Equal(1, releasedCalls);
    }

    [Fact]
    public async Task BuiltInPcscConnection_ReleaseObserverFailureIsSharedByRepeatedDisposal()
    {
        var expected = new InvalidOperationException("release observer failed");
        var api = new ControlledSCardConnectionApi();
        var connection = new UsbSmartCardConnection(
            new PcscDevice { ReaderName = $"observer-failure-{Guid.NewGuid():N}", Atr = null },
            api: api,
            released: () => throw expected);
        await connection.InitializeAsync(Ct);

        var first = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.DisposeAsync().AsTask());
        var second = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.DisposeAsync().AsTask());

        Assert.Same(expected, first);
        Assert.Same(first, second);
        Assert.Equal(1, api.DisconnectCalls);
        Assert.Equal(1, api.ReleaseContextCalls);
    }

    [Fact]
    public async Task BuiltInPcscConnection_TerminalWorkerFailureIsSharedByRepeatedDisposal()
    {
        var expected = new InvalidOperationException("terminal worker failure");
        var releasedCalls = 0;
        var api = new ControlledSCardConnectionApi();
        var connection = new UsbSmartCardConnection(
            new PcscDevice { ReaderName = $"terminal-worker-failure-{Guid.NewGuid():N}", Atr = null },
            api: api,
            released: () => Interlocked.Increment(ref releasedCalls),
            workerLoopStarting: () => throw expected);

        var openFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => connection.InitializeAsync(Ct).AsTask());
        var firstDisposeFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => connection.DisposeAsync().AsTask());
        var secondDisposeFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => connection.DisposeAsync().AsTask());

        Assert.Same(expected, openFailure);
        Assert.Same(firstDisposeFailure, secondDisposeFailure);
        Assert.Equal(1, releasedCalls);
        Assert.Empty(api.Events);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference OpenAndDropDirectConnection(ControlledSCardConnectionApi api)
    {
        var connection = new UsbSmartCardConnection(
            new PcscDevice { ReaderName = $"direct-drop-{Guid.NewGuid():N}", Atr = null },
            api: api);
        connection.InitializeAsync(Ct).AsTask().GetAwaiter().GetResult();
        return new WeakReference(connection);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndDropUninitializedConnection(
        ControlledSCardConnectionApi api,
        TaskCompletionSource released)
    {
        var connection = new UsbSmartCardConnection(
            new PcscDevice { ReaderName = $"uninitialized-drop-{Guid.NewGuid():N}", Atr = null },
            api: api,
            released: released.SetResult);
        return new WeakReference(connection);
    }

    private sealed class SyncOnlyTransactionConnection : ISmartCardConnection
    {
        public int BeginCalls { get; private set; }
        public ConnectionType Type => ConnectionType.SmartCard;
        public Transport Transport => Transport.Usb;
        public bool SupportsExtendedApdu() => false;
        public IDisposable BeginTransaction(CancellationToken cancellationToken = default)
        {
            BeginCalls++;
            return new CancellationTokenSource();
        }

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
