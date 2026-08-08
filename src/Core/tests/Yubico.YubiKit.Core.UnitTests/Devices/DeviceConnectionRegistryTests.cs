// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class DeviceConnectionRegistryTests
{
    private static string NewId() => $"test:{Guid.NewGuid():N}";

    /// <summary>
    ///     A NON-exclusive (HID) interface ref-counts its connection leases: two both succeed, the count is
    ///     shared, and dispose is idempotent.
    /// </summary>
    /// <remarks>
    ///     This test used to cover every interface, and that was the bug in the pin, not just in the code.
    ///     Acquisition succeeding and coexistence being safe are different claims, and conflating them is what
    ///     let the CCID contention defect go unnoticed: on a SmartCard interface a second connection's session
    ///     issues its own applet SELECT, which deselects the first applet and leaves the earlier session
    ///     answering SW=0x6D00 (measured, docs/architecture/connection-ownership-and-contention.md). CCID is now
    ///     exclusive — see <see cref="AcquireConnection_ExclusiveInterface_SecondAcquisitionIsRefused" />.
    ///     HID has no applet-selection state, so ref-counting is correct there and is retained deliberately:
    ///     Management over HID must stay available while a PIV session holds CCID.
    /// </remarks>
    [Fact]
    public async Task AcquireConnection_NonExclusiveInterface_RefCounts_AndDisposeIsIdempotent()
    {
        var id = NewId();
        Assert.False(DeviceConnectionRegistry.IsInUse(id));

        var first = await DeviceConnectionRegistry.AcquireConnectionAsync(id, exclusive: false, TestContext.Current.CancellationToken);
        var second = await DeviceConnectionRegistry.AcquireConnectionAsync(id, exclusive: false, TestContext.Current.CancellationToken);
        Assert.True(DeviceConnectionRegistry.IsInUse(id));

        first.Dispose();
        Assert.True(DeviceConnectionRegistry.IsInUse(id));

        first.Dispose(); // double-dispose must not steal the remaining count
        Assert.True(DeviceConnectionRegistry.IsInUse(id));

        second.Dispose();
        Assert.False(DeviceConnectionRegistry.IsInUse(id));
    }

    /// <summary>
    ///     An exclusive (CCID) interface admits one live connection and refuses the second immediately —
    ///     refuse, never queue: waiting for an unbounded session to finish is worse than a clear error, and
    ///     the caller usually has another transport. Releasing the holder makes the interface available again.
    /// </summary>
    [Fact]
    public async Task AcquireConnection_ExclusiveInterface_SecondAcquisitionIsRefused()
    {
        var id = NewId();

        var held = await DeviceConnectionRegistry.AcquireConnectionAsync(
            id, exclusive: true, TestContext.Current.CancellationToken);

        var refusal = await Assert.ThrowsAsync<ConnectionInUseException>(async () =>
            await DeviceConnectionRegistry.AcquireConnectionAsync(
                id, exclusive: true, TestContext.Current.CancellationToken));
        Assert.Contains(id, refusal.Message, StringComparison.Ordinal);

        held.Dispose();

        using var next = await DeviceConnectionRegistry.AcquireConnectionAsync(
            id, exclusive: true, TestContext.Current.CancellationToken);
        Assert.True(DeviceConnectionRegistry.IsInUse(id));
    }

    /// <summary>
    ///     An identity read against an interface this process holds a live connection to must be skipped
    ///     entirely (no connection opened — a discovery SELECT would clobber the session's applet state).
    /// </summary>
    [Fact]
    public async Task IdentityRead_DeviceInUse_SkipsWithoutConnecting()
    {
        var device = new RecordingYubiKey(NewId(), ConnectionType.SmartCard);
        using var registration = await DeviceConnectionRegistry.AcquireConnectionAsync(
            device.DeviceId, exclusive: true, TestContext.Current.CancellationToken);

        var info = await DiscoveryIdentityReader.TryReadAsync(
            device, ConnectionType.SmartCard, NullLogger.Instance, TestContext.Current.CancellationToken);

        Assert.Null(info);
        Assert.Equal(0, device.ConnectCalls);
    }

    /// <summary>
    ///     The metadata read must skip only the in-use member interface of a composite and still attempt
    ///     the remaining free transports (independent USB interfaces are safe to read).
    /// </summary>
    [Fact]
    public async Task MetadataRead_CompositeWithInUseSmartCardMember_SkipsItButTriesOtpTransport()
    {
        var smartCardMember = new RecordingYubiKey(NewId(), ConnectionType.SmartCard);
        var otpMember = new RecordingYubiKey(NewId(), ConnectionType.HidOtp);
        var composite = new CompositeYubiKey(NewId(), [smartCardMember, otpMember], deviceInfo: null);
        using var registration = await DeviceConnectionRegistry.AcquireConnectionAsync(
            smartCardMember.DeviceId, exclusive: true, TestContext.Current.CancellationToken);

        var info = await CompositeMetadataReader.TryReadAsync(
            composite, TimeSpan.FromSeconds(5), NullLogger.Instance, TestContext.Current.CancellationToken);

        Assert.Null(info); // OTP member's connect fails by design; result degrades to null
        Assert.Equal(0, smartCardMember.ConnectCalls);
        Assert.Equal(1, otpMember.ConnectCalls);
    }

    [Fact]
    public async Task RegisteredSmartCardConnection_Dispose_ReleasesRegistration_EvenWhenInnerThrows()
    {
        var id = NewId();
        var throwingInner = new FakeSmartCardConnection { ThrowOnDispose = true };
        var lease = await DeviceConnectionRegistry.AcquireConnectionAsync(id, exclusive: false, TestContext.Current.CancellationToken);
        var wrapped = new RegisteredSmartCardConnection(throwingInner, lease);
        Assert.True(DeviceConnectionRegistry.IsInUse(id));

        Assert.Throws<InvalidOperationException>(wrapped.Dispose);
        Assert.False(DeviceConnectionRegistry.IsInUse(id));

        var asyncId = NewId();
        var inner = new FakeSmartCardConnection();
        var asyncLease = await DeviceConnectionRegistry.AcquireConnectionAsync(
            asyncId, exclusive: false, TestContext.Current.CancellationToken);
        var asyncWrapped = new RegisteredSmartCardConnection(inner, asyncLease);
        Assert.True(DeviceConnectionRegistry.IsInUse(asyncId));

        await asyncWrapped.DisposeAsync();
        Assert.False(DeviceConnectionRegistry.IsInUse(asyncId));
        Assert.True(inner.Disposed);
    }

    // Blocking waits below are deliberate: "a losing caller must not return early" can only be observed by
    // blocking, and sync-over-async disposal is precisely what these tests pin.
#pragma warning disable xUnit1031

    // ---------------------------------------------------------------------------------------------------
    // One-shot disposal with shared completion (DisposalGate).
    //
    // Invariants under test:
    //   I1  the inner connection is disposed exactly once, however many callers race;
    //   I2  the registry lease is released exactly once;
    //   I3  the lease is never released before inner teardown completes;
    //   I4  a losing caller (sync or async) does not return before the winner's teardown finishes;
    //   I5  a synchronous loser blocking on an asynchronous winner does not deadlock;
    //   I6  teardown failure releases the lease anyway, and every caller observes the same exception.
    // ---------------------------------------------------------------------------------------------------

    /// <summary>I1, I2, I3, I4 — SmartCard wrapper.</summary>
    [Fact]
    public async Task RegisteredSmartCardConnection_SyncDisposeRacingAsyncDispose_DisposesInnerOnce()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var inner = new FakeSmartCardConnection { DisposeGate = gate };
        var lease = new CountingLease();
        var wrapped = new RegisteredSmartCardConnection(inner, lease);

        var asyncDispose = Task.Run(async () => await wrapped.DisposeAsync(), TestContext.Current.CancellationToken);
        Assert.True(inner.DisposeEntered.Wait(EnterTimeout, TestContext.Current.CancellationToken));

        var syncDispose = Task.Run(wrapped.Dispose, TestContext.Current.CancellationToken);

        Assert.False(syncDispose.Wait(LoserProbe, TestContext.Current.CancellationToken)); // I4: the loser must not return while teardown runs
        Assert.Equal(0, lease.ReleaseCount); // I3: lease still held while inner teardown is in flight

        gate.SetResult();
        await asyncDispose;
        await syncDispose;

        Assert.Equal(1, inner.DisposeCount); // I1
        Assert.Equal(1, lease.ReleaseCount); // I2
    }

    /// <summary>I1, I2, I3, I4 — FIDO HID wrapper.</summary>
    [Fact]
    public async Task RegisteredFidoHidConnection_SyncDisposeRacingAsyncDispose_DisposesInnerOnce()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var inner = new FakeFidoHidConnection { DisposeGate = gate };
        var lease = new CountingLease();
        var wrapped = new RegisteredFidoHidConnection(inner, lease);

        var asyncDispose = Task.Run(async () => await wrapped.DisposeAsync(), TestContext.Current.CancellationToken);
        Assert.True(inner.DisposeEntered.Wait(EnterTimeout, TestContext.Current.CancellationToken));

        var syncDispose = Task.Run(wrapped.Dispose, TestContext.Current.CancellationToken);

        Assert.False(syncDispose.Wait(LoserProbe, TestContext.Current.CancellationToken));
        Assert.Equal(0, lease.ReleaseCount);

        gate.SetResult();
        await asyncDispose;
        await syncDispose;

        Assert.Equal(1, inner.DisposeCount);
        Assert.Equal(1, lease.ReleaseCount);
    }

    /// <summary>I1, I2, I3, I4 — OTP HID wrapper.</summary>
    [Fact]
    public async Task RegisteredOtpHidConnection_SyncDisposeRacingAsyncDispose_DisposesInnerOnce()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var inner = new FakeOtpHidConnection { DisposeGate = gate };
        var lease = new CountingLease();
        var wrapped = new RegisteredOtpHidConnection(inner, lease);

        var asyncDispose = Task.Run(async () => await wrapped.DisposeAsync(), TestContext.Current.CancellationToken);
        Assert.True(inner.DisposeEntered.Wait(EnterTimeout, TestContext.Current.CancellationToken));

        var syncDispose = Task.Run(wrapped.Dispose, TestContext.Current.CancellationToken);

        Assert.False(syncDispose.Wait(LoserProbe, TestContext.Current.CancellationToken));
        Assert.Equal(0, lease.ReleaseCount);

        gate.SetResult();
        await asyncDispose;
        await syncDispose;

        Assert.Equal(1, inner.DisposeCount);
        Assert.Equal(1, lease.ReleaseCount);
    }

    /// <summary>
    ///     I4, I5 — a synchronous <c>Dispose</c> loser blocks on an asynchronous winner's completion without
    ///     deadlocking, and observes a fully finished teardown when it returns. This is the case that makes an
    ///     early-returning caller unable to reopen a PC/SC handle that is still being torn down.
    /// </summary>
    [Fact]
    public async Task RegisteredSmartCardConnection_SyncDisposeLoser_WaitsForAsyncWinnerTeardown()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var inner = new FakeSmartCardConnection { DisposeGate = gate };
        var lease = new CountingLease();
        var wrapped = new RegisteredSmartCardConnection(inner, lease);

        var asyncDispose = Task.Run(async () => await wrapped.DisposeAsync(), TestContext.Current.CancellationToken);
        Assert.True(inner.DisposeEntered.Wait(EnterTimeout, TestContext.Current.CancellationToken));

        var syncLoserSawTeardownComplete = false;
        var syncLoserSawLeaseReleased = false;
        var syncDispose = Task.Run(
            () =>
            {
                wrapped.Dispose();
                syncLoserSawTeardownComplete = inner.TeardownCompleted;
                syncLoserSawLeaseReleased = lease.ReleaseCount == 1;
            },
            TestContext.Current.CancellationToken);

        Assert.False(syncDispose.Wait(LoserProbe, TestContext.Current.CancellationToken));
        Assert.False(inner.TeardownCompleted);

        gate.SetResult();
        Assert.True(syncDispose.Wait(DeadlockTimeout, TestContext.Current.CancellationToken)); // I5: no deadlock on the sync-over-async wait
        await syncDispose;
        await asyncDispose;

        Assert.True(syncLoserSawTeardownComplete);
        Assert.True(syncLoserSawLeaseReleased);
        Assert.Equal(1, inner.DisposeCount);
        Assert.Equal(1, lease.ReleaseCount);
    }

    /// <summary>I1, I2 — repeated sequential disposal is a no-op after the first.</summary>
    [Fact]
    public async Task RegisteredSmartCardConnection_RepeatedDispose_DisposesInnerOnce()
    {
        var inner = new FakeSmartCardConnection();
        var lease = new CountingLease();
        var wrapped = new RegisteredSmartCardConnection(inner, lease);

        wrapped.Dispose();
        wrapped.Dispose();
        await wrapped.DisposeAsync();

        Assert.Equal(1, inner.DisposeCount);
        Assert.Equal(1, lease.ReleaseCount);
    }

    /// <summary>I6 — teardown failure still releases the lease, and every caller sees the same exception.</summary>
    [Fact]
    public async Task RegisteredSmartCardConnection_InnerDisposeThrows_ReleasesLeaseAndSharesException()
    {
        var inner = new FakeSmartCardConnection { ThrowOnDispose = true };
        var lease = new CountingLease();
        var wrapped = new RegisteredSmartCardConnection(inner, lease);

        var winner = Assert.Throws<InvalidOperationException>(wrapped.Dispose);
        var syncLoser = Assert.Throws<InvalidOperationException>(wrapped.Dispose);
        var asyncLoser = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await wrapped.DisposeAsync());

        Assert.Same(winner, syncLoser);
        Assert.Same(winner, asyncLoser);
        Assert.Equal(1, inner.DisposeCount);
        Assert.Equal(1, lease.ReleaseCount);
    }

#pragma warning restore xUnit1031

    private static readonly TimeSpan EnterTimeout = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan DeadlockTimeout = TimeSpan.FromSeconds(5);

    /// <summary>How long a losing caller is observed for; it must still be blocked when this elapses.</summary>
    private static readonly TimeSpan LoserProbe = TimeSpan.FromMilliseconds(250);

    private sealed class CountingLease : IDisposable
    {
        private int _releaseCount;

        public int ReleaseCount => Volatile.Read(ref _releaseCount);

        public void Dispose() => Interlocked.Increment(ref _releaseCount);
    }

    private sealed class RecordingYubiKey(string deviceId, ConnectionType available) : IYubiKey, IDiscoveryConnectionProvider
    {
        public int ConnectCalls { get; private set; }

        public string DeviceId => deviceId;

        public ConnectionType AvailableConnections => available;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            ConnectCalls++;
            throw new InvalidOperationException("Test connect refused by design.");
        }

        Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
            ConnectionType connection,
            CancellationToken cancellationToken)
        {
            ConnectCalls++;
            throw new InvalidOperationException("Test discovery connect refused by design.");
        }
    }

    /// <summary>
    ///     Records how many times disposal ran, and can be held inside teardown via <see cref="DisposeGate" />
    ///     so a test can observe the window in which the winner's teardown is still in flight.
    /// </summary>
    private abstract class FakeDisposableConnection
    {
        private int _disposeCount;
        private int _teardownCompleted;

        public ManualResetEventSlim DisposeEntered { get; } = new();

        /// <summary>
        ///     When set, the FIRST teardown blocks on this until the test releases it. Only the first, so that a
        ///     second caller reaching the inner connection at all is immediately visible as a defect rather than
        ///     being hidden behind the same wait.
        /// </summary>
        public TaskCompletionSource? DisposeGate { get; init; }

        public bool ThrowOnDispose { get; init; }

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public bool Disposed => DisposeCount > 0;

        public bool TeardownCompleted => Volatile.Read(ref _teardownCompleted) > 0;

        public void Dispose()
        {
            var ordinal = Interlocked.Increment(ref _disposeCount);
            DisposeEntered.Set();
            if (ordinal == 1)
                DisposeGate?.Task.GetAwaiter().GetResult();

            Finish();
        }

        public async ValueTask DisposeAsync()
        {
            var ordinal = Interlocked.Increment(ref _disposeCount);
            DisposeEntered.Set();
            if (ordinal == 1 && DisposeGate is not null)
                await DisposeGate.Task.ConfigureAwait(false);

            Finish();
        }

        private void Finish()
        {
            _ = Interlocked.Exchange(ref _teardownCompleted, 1);
            if (ThrowOnDispose)
                throw new InvalidOperationException("Inner dispose failure.");
        }
    }

    private sealed class FakeSmartCardConnection : FakeDisposableConnection, ISmartCardConnection
    {
        public int TransmitCalls { get; private set; }

        public ConnectionType Type => ConnectionType.SmartCard;

        public Transport Transport => Transport.Usb;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default)
        {
            TransmitCalls++;
            return Task.FromResult(ReadOnlyMemory<byte>.Empty);
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool SupportsExtendedApdu() => true;
    }

    private sealed class FakeFidoHidConnection : FakeDisposableConnection, IFidoHidConnection
    {
        public ConnectionType Type => ConnectionType.HidFido;

        public int PacketSize => 64;

        public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ReadOnlyMemory<byte>.Empty);
    }

    private sealed class FakeOtpHidConnection : FakeDisposableConnection, IOtpHidConnection
    {
        public ConnectionType Type => ConnectionType.HidOtp;

        public int FeatureReportSize => 8;

        public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ReadOnlyMemory<byte>.Empty);
    }
}