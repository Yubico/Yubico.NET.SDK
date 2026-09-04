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

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Reads <see cref="DeviceInfo"/> over an already-open connection by building the matching Core protocol.
/// </summary>
/// <remarks>
///     Borrows the supplied connection. It builds and disposes the matching protocol before returning, but
///     protocol disposal does not dispose the connection. The caller retains ownership and must dispose the
///     connection. Shared by discovery's serial-disambiguation read and the composite metadata read.
/// </remarks>
internal static class ProtocolDeviceInfo
{
    private static readonly ReadScope DefaultScope = new();

    // A finder owns one read scope, so its single-flight reads cannot be joined by a replacement manager.
    // Within that scope, generation and supersede token live in one immutable transport epoch captured under
    // a short lock. Transport activity either advances the default direct-call scope or terminally retires a
    // finder scope; a read admitted after retirement fails before worker admission or hardware open.
    internal sealed class ReadScope
    {
        private readonly Lock _gate = new();
        private readonly ConcurrentDictionary<ReadKey, SharedRead> _inFlightReads = new();
        private TransportEpoch _currentEpoch = new(generation: 0);
        private bool _retired;

        internal bool TryCaptureEpoch(out TransportEpoch epoch)
        {
            lock (_gate)
            {
                epoch = _currentEpoch;
                return !_retired;
            }
        }

        internal SharedRead GetOrAdd(ReadKey key, Func<ReadKey, SharedRead> create) =>
            _inFlightReads.GetOrAdd(key, create);

        internal void Remove(ReadKey key, SharedRead read) =>
            _ = _inFlightReads.TryRemove(KeyValuePair.Create(key, read));

        internal bool IsCurrent(TransportEpoch epoch)
        {
            lock (_gate)
                return !_retired && ReferenceEquals(_currentEpoch, epoch);
        }

        internal void Advance()
        {
            TransportEpoch retired;
            lock (_gate)
            {
                if (_retired)
                    return;

                retired = _currentEpoch;
                _currentEpoch = new TransportEpoch(retired.Generation + 1);
            }

            retired.MarkSuperseded();
        }

        internal void Retire()
        {
            TransportEpoch retired;
            lock (_gate)
            {
                if (_retired)
                    return;

                _retired = true;
                retired = _currentEpoch;
            }

            retired.MarkSuperseded();
        }

    }

    internal static ReadScope CreateScope() => new();

    /// <summary>
    ///     One immutable hotplug epoch: a generation and the cancellation source that fires when this
    ///     epoch is superseded. The retired CTS is only cancelled, never disposed, because detached
    ///     waiters may still be observing it (a cancelled timer-less CTS is plain collectible garbage).
    /// </summary>
    internal sealed class TransportEpoch(long generation)
    {
        private readonly CancellationTokenSource _superseded = new();

        public long Generation { get; } = generation;

        /// <summary>Fires when this epoch has been replaced by newer transport activity.</summary>
        public CancellationToken Superseded => _superseded.Token;

        public void MarkSuperseded() => _superseded.Cancel();
    }

    /// <summary>
    ///     Signals that hotplug activity was observed, detaching future device-info reads from any
    ///     in-flight read started against the previous physical topology, and failing superseded reads
    ///     that have not yet opened their interface (including reads still queued for worker admission,
    ///     whose wait is cancelled so they cannot accumulate behind hung workers). Called from the same
    ///     place <see cref="FindYubiKeys" /> invalidates its identity/metadata caches.
    /// </summary>
    internal static void NotifyTransportActivity(ReadScope? scope = null)
    {
        scope ??= DefaultScope;

        scope.Advance();
    }

    internal static void Retire(ReadScope scope) => scope.Retire();

    /// <summary>
    ///     Opens a short-lived connection over the given interface and reads <see cref="DeviceInfo" />,
    ///     bounded by a hard wall-clock budget.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The budget bounds the caller's <em>wait</em>, not the work: an in-flight native call (e.g.
    ///         <c>SCardTransmit</c> against a card busy with a long applet operation such as RSA key
    ///         generation) cannot observe cancellation. On budget exhaustion the read is therefore
    ///         <em>abandoned, not aborted</em> — this method throws <see cref="TimeoutException" /> so the
    ///         scan can proceed, while the abandoned task keeps running in the background and disposes its
    ///         protocol through <see cref="ReadAsync" /> and its discovery-owned connection through
    ///         <c>ConnectAndReadAsync</c> when the native call eventually returns.
    ///     </para>
    ///     <para>
    ///         External cancellation via <paramref name="cancellationToken" /> likewise abandons only that
    ///         caller's wait (propagating <see cref="OperationCanceledException" />). The shared operation's
    ///         lifetime is independent of every waiter and remains the single in-flight read for its stable
    ///         interface/connection key until it completes.
    ///     </para>
    /// </remarks>
    /// <exception cref="TimeoutException">The budget elapsed before the read completed.</exception>
    public static async Task<DeviceInfo> ReadBoundedAsync(
        IYubiKey device,
        ConnectionType connection,
        TimeSpan budget,
        ILogger logger,
        CancellationToken cancellationToken,
        bool waitForWorkerSlot = false,
        ReadScope? scope = null)
    {
        var interfaceId = DeviceConnectionRegistry.ResolveInterfaceId(device, connection);
        var provider = device as IDiscoveryConnectionProvider;
        return await ReadBoundedCoreAsync(
                interfaceId,
                device.DeviceId,
                provider,
                connection,
                budget,
                logger,
                cancellationToken,
                waitForWorkerSlot,
                scope ?? DefaultScope)
            .ConfigureAwait(false);
    }

    public static async Task<DeviceInfo> ReadSlotBoundedAsync(
        IYubiKeyConnectionSlot slot,
        ConnectionType connection,
        TimeSpan budget,
        ILogger logger,
        CancellationToken cancellationToken,
        bool waitForWorkerSlot = false,
        ReadScope? scope = null) =>
        await ReadBoundedCoreAsync(
            slot.InterfaceId,
            slot.InterfaceId,
            slot as IDiscoveryConnectionProvider,
            connection,
            budget,
            logger,
            cancellationToken,
            waitForWorkerSlot,
            scope ?? DefaultScope).ConfigureAwait(false);

    private static async Task<DeviceInfo> ReadBoundedCoreAsync(
        string interfaceId,
        string deviceId,
        IDiscoveryConnectionProvider? provider,
        ConnectionType connection,
        TimeSpan budget,
        ILogger logger,
        CancellationToken cancellationToken,
        bool waitForWorkerSlot,
        ReadScope scope)
    {
        // One atomic epoch capture: the key's generation and the supersede token used while queued are
        // guaranteed to belong to the same hotplug epoch, so a read created against an already-retired
        // epoch always holds a token that has been (or is about to be) cancelled.
        if (!scope.TryCaptureEpoch(out var epoch))
            throw Superseded(new ReadKey(interfaceId, connection, Generation: -1));

        var key = new ReadKey(interfaceId, connection, epoch.Generation);
        var sharedRead = scope.GetOrAdd(
            key,
            _ => new SharedRead(scope, key, epoch, deviceId, provider, connection, logger, waitForWorkerSlot));

        try
        {
            return await sharedRead.Task.WaitAsync(budget, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            sharedRead.RecordAbandonment();
            throw;
        }
        catch (OperationCanceledException)
        {
            sharedRead.RecordAbandonment();
            throw;
        }
    }

    internal readonly record struct ReadKey(string InterfaceId, ConnectionType Connection, long Generation);

    internal sealed class SharedRead
    {
        private readonly ReadScope _scope;
        private readonly ReadKey _key;
        private readonly string _deviceId;
        private readonly ConnectionType _connection;
        private readonly ILogger _logger;
        private readonly Lazy<Task<DeviceInfo>> _task;
        private int _abandonedWaiterCount;

        public SharedRead(
            ReadScope scope,
            ReadKey key,
            TransportEpoch epoch,
            string deviceId,
            IDiscoveryConnectionProvider? provider,
            ConnectionType connection,
            ILogger logger,
            bool waitForWorkerSlot)
        {
            _scope = scope;
            _key = key;
            _deviceId = deviceId;
            _connection = connection;
            _logger = logger;
            _task = new Lazy<Task<DeviceInfo>>(
                () => StartAndObserve(epoch, provider, waitForWorkerSlot),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public Task<DeviceInfo> Task => _task.Value;

        public void RecordAbandonment() => Interlocked.Increment(ref _abandonedWaiterCount);

        private Task<DeviceInfo> StartAndObserve(
            TransportEpoch epoch,
            IDiscoveryConnectionProvider? provider,
            bool waitForWorkerSlot)
        {
            var task = StartSharedRead(_scope, _key, epoch, provider, waitForWorkerSlot);
            _ = task.ContinueWith(
                Complete,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return task;
        }

        private void Complete(Task<DeviceInfo> task)
        {
            var exception = task.Exception?.GetBaseException();
            _scope.Remove(_key, this);

            var abandonedWaiterCount = Volatile.Read(ref _abandonedWaiterCount);
            if (abandonedWaiterCount > 0)
            {
                _logger.LogDebug(
                    exception,
                    "Abandoned discovery device-info read for {DeviceId} over {Connection} finished in the background (status: {Status}, abandoned waiters: {AbandonedWaiterCount}).",
                    _deviceId,
                    _connection,
                    task.Status,
                    abandonedWaiterCount);
            }
        }
    }

    private static Task<DeviceInfo> StartSharedRead(
        ReadScope scope,
        ReadKey key,
        TransportEpoch epoch,
        IDiscoveryConnectionProvider? provider,
        bool waitForWorkerSlot)
    {
        if (waitForWorkerSlot)
        {
            // Identity reads WAIT for a bounded worker slot instead of skipping: with N same-PID keys, a
            // scan legitimately issues more identity reads than there are workers, and skipping the
            // excess orphans their interfaces (the Phase-0 "aborted" scan-1 failures were exactly this
            // self-contention on the admission gate). The caller's wall-clock budget in ReadBoundedAsync
            // bounds the wait, and the admission bound itself is preserved: at most
            // MaximumConcurrentWorkers native reads run concurrently, so a hung native call still cannot
            // multiply workers.
            return StartQueuedSharedRead(scope, key, epoch, provider);
        }

        if (!DiscoveryWorkerAdmission.TryAcquire(out var admission))
        {
            return Task.FromException<DeviceInfo>(
                new DiscoveryReadSkippedException(key.InterfaceId, DiscoveryReadSkipCause.WorkerAdmissionSaturated));
        }

        try
        {
            // A bounded dedicated worker starts provider/native code only after this caller can install
            // WaitAsync. Saturation skips instead of queuing or allocating another worker (best-effort
            // metadata path). Supersession is validated inside the worker, immediately before the
            // hardware open (see ConnectAndReadAsync).
            return StartWorker(scope, key, epoch, provider, admission);
        }
        catch
        {
            admission.Dispose();
            throw;
        }
    }

    private static async Task<DeviceInfo> StartQueuedSharedRead(
        ReadScope scope,
        ReadKey key,
        TransportEpoch epoch,
        IDiscoveryConnectionProvider? provider)
    {
        // The queued wait is cancelled by transport activity so superseded reads cannot accumulate behind
        // hung workers: each hotplug event would otherwise enqueue another uncancellable waiter (its
        // timed-out caller has already abandoned it) that eventually opens an interface its evidence no
        // longer names. The token belongs to the SAME epoch as the key's generation (captured atomically
        // in ReadBoundedCoreAsync), so a read created against an already-retired epoch waits on a token
        // that is already cancelled - it can never queue indefinitely.
        IDisposable admission;
        try
        {
            admission = await DiscoveryWorkerAdmission.AcquireAsync(epoch.Superseded).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw Superseded(key);
        }

        try
        {
            return await StartWorker(scope, key, epoch, provider, admission).ConfigureAwait(false);
        }
        catch
        {
            // Safe double-dispose when the worker's own `using` already released the slot: Admission
            // disposal is idempotent. This guards synchronous StartNew failures.
            admission.Dispose();
            throw;
        }
    }

    private static void ThrowIfSuperseded(ReadScope scope, ReadKey key, TransportEpoch epoch)
    {
        if (!scope.IsCurrent(epoch))
            throw Superseded(key);
    }

    private static DiscoveryReadSkippedException Superseded(ReadKey key) =>
        new(key.InterfaceId, DiscoveryReadSkipCause.SupersededByTransportActivity);

    private static Task<DeviceInfo> StartWorker(
        ReadScope scope,
        ReadKey key,
        TransportEpoch epoch,
        IDiscoveryConnectionProvider? provider,
        IDisposable admission) =>
        Task.Factory.StartNew(
                async () =>
                {
                    using (admission)
                    {
                        return await ConnectAndReadAsync(scope, key, epoch, provider, CancellationToken.None)
                            .ConfigureAwait(false);
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
                TaskScheduler.Default)
            .Unwrap();

    private static async Task<DeviceInfo> ConnectAndReadAsync(
        ReadScope scope,
        ReadKey key,
        TransportEpoch epoch,
        IDiscoveryConnectionProvider? provider,
        CancellationToken cancellationToken)
    {
        var interfaceId = key.InterfaceId;
        if (provider is null)
            throw new DiscoveryReadSkippedException(interfaceId, DiscoveryReadSkipCause.NoDiscoveryProvider);

        using var discoveryLease = DeviceConnectionRegistry.TryAcquireDiscovery(interfaceId);
        if (discoveryLease is null)
            throw new DiscoveryReadSkippedException(interfaceId, DiscoveryReadSkipCause.InterfaceLeaseHeld);

        // Final supersession check, as close to the hardware open as possible: a superseded read must not
        // START a new open against hardware its evidence no longer names. An epoch flip landing after this
        // check is inherently racy with the open itself and is accepted as residual: the delivered
        // invariants are (a) a read holding an old epoch can never wait forever un-cancelled, and (b) it
        // cannot begin a hardware open once supersession is observable. A finder also stores cache writes in
        // the same captured EvidenceEpoch as this scope, so results completing after retirement cannot reach
        // the replacement epoch's caches.
        ThrowIfSuperseded(scope, key, epoch);

        // Discovery creates this connection, so discovery disposes it. Protocols are pure users of the
        // connection they are handed, so the protocol disposal inside ReadAsync does not release the handle.
        var conn = await provider.ConnectForDiscoveryAsync(key.Connection, cancellationToken).ConfigureAwait(false);
        await using (conn.ConfigureAwait(false))
            return await ReadAsync(conn, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<DeviceInfo> ReadAsync(IConnection connection, CancellationToken cancellationToken)
    {
        // Borrows: the caller owns this connection and disposes it (see the type remarks). Upstream
        // disposed it here on failure, which was correct only in a model where protocol disposal
        // cascaded to the connection. This branch removed that cascade, so disposing here would be a
        // premature dispose of a connection the caller still holds and will dispose itself.
        var protocol = ProtocolFactory.Create(connection);

        try
        {
            if (protocol is ISmartCardProtocol smartCard)
            {
                await smartCard.SelectAsync(ApplicationIds.Management, cancellationToken).ConfigureAwait(false);
            }
            else if (protocol is IFidoHidProtocol fidoHid)
            {
                await fidoHid.InitializeAsync(cancellationToken).ConfigureAwait(false);
            }

            return await DeviceInfoReader.ReadAsync(protocol, null, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            protocol.Dispose();
        }
    }
}

/// <summary>
///     Process-wide, nonqueueing admission for best-effort native discovery workers.
/// </summary>
internal static class DiscoveryWorkerAdmission
{
    internal const int MaximumConcurrentWorkers = 4;

    private static readonly SemaphoreSlim Slots =
        new(MaximumConcurrentWorkers, MaximumConcurrentWorkers);

    public static bool TryAcquire(out IDisposable admission)
    {
        if (!Slots.Wait(0))
        {
            admission = null!;
            return false;
        }

        admission = new Admission();
        return true;
    }

    /// <summary>
    ///     Waits (asynchronously, unbounded) for a worker slot. Used by identity reads, whose callers bound
    ///     their own wait via the read budget; the slot count still bounds concurrent native work.
    /// </summary>
    public static async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await Slots.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Admission();
    }

    private sealed class Admission : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                Slots.Release();
        }
    }
}