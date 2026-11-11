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

namespace Yubico.YubiKit.Core.Devices;

/// <summary>Owns one shared discovery device-info read and its connection/read pipeline.</summary>
internal sealed class SharedDeviceInfoRead
{
    private readonly DiscoveryReadScope _scope;
    private readonly DiscoveryReadKey _key;
    private readonly string _deviceId;
    private readonly ConnectionType _connection;
    private readonly ILogger _logger;
    private readonly Lazy<Task<DeviceInfo>> _task;
    private readonly DiscoveryReadLeaseTracker _leaseTracker = new();
    private int _abandonedWaiterCount;

    public SharedDeviceInfoRead(
        DiscoveryReadScope scope,
        DiscoveryReadKey key,
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

    public void RecordAbandonment()
    {
        Interlocked.Increment(ref _abandonedWaiterCount);
        _leaseTracker.MarkAbandoned(_key.InterfaceId);
    }

    private Task<DeviceInfo> StartAndObserve(
        TransportEpoch epoch,
        IDiscoveryConnectionProvider? provider,
        bool waitForWorkerSlot)
    {
        var task = StartSharedRead(
            _scope,
            _key,
            epoch,
            provider,
            waitForWorkerSlot,
            _leaseTracker);
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

    private static async Task<DeviceInfo> StartSharedRead(
        DiscoveryReadScope scope,
        DiscoveryReadKey key,
        TransportEpoch epoch,
        IDiscoveryConnectionProvider? provider,
        bool waitForWorkerSlot,
        DiscoveryReadLeaseTracker leaseTracker)
    {
        DiscoveryReadAdmission? nativeAdmission = null;
        try
        {
            if (scope.ReadGatesFor(key.InterfaceId) is { } gates)
            {
                // Preserve nonblocking refusal for an already-held interface instead of waiting behind
                // its owner's PID gate. This is only a precheck; the worker still acquires atomically.
                if (!DeviceConnectionRegistry.IsDiscoveryAvailable(key.InterfaceId))
                    throw new DiscoveryReadSkippedException(key.InterfaceId, DiscoveryReadSkipCause.InterfaceLeaseHeld);
                nativeAdmission = await DiscoveryReadAdmission.AcquireAsync(gates, epoch.Superseded)
                    .ConfigureAwait(false);
            }

            return await StartAdmittedRead(scope, key, epoch, provider, waitForWorkerSlot, leaseTracker, nativeAdmission)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (epoch.Superseded.IsCancellationRequested)
        {
            throw key.Superseded();
        }
        finally
        {
            // After registration, only native teardown releases this admission. In particular a built-in
            // owner that cannot prove native release keeps the PID gate along with its quarantined lease.
            nativeAdmission?.DisposeUnlessTransferred();
        }
    }

    private static Task<DeviceInfo> StartAdmittedRead(
        DiscoveryReadScope scope,
        DiscoveryReadKey key,
        TransportEpoch epoch,
        IDiscoveryConnectionProvider? provider,
        bool waitForWorkerSlot,
        DiscoveryReadLeaseTracker leaseTracker,
        DiscoveryReadAdmission? nativeAdmission)
    {
        if (waitForWorkerSlot)
        {
            // Identity reads WAIT for a bounded worker slot instead of skipping: with N same-PID keys, a
            // scan legitimately issues more identity reads than there are workers, and skipping the
            // excess orphans their interfaces (the Phase-0 "aborted" scan-1 failures were exactly this
            // self-contention on the admission gate). The caller's wall-clock budget in DiscoveryDeviceInfoReader.ReadBoundedAsync
            // bounds the wait, and the admission bound itself is preserved: at most
            // MaximumConcurrentWorkers native reads run concurrently, so a hung native call still cannot
            // multiply workers.
            return StartQueuedSharedRead(scope, key, epoch, provider, leaseTracker, nativeAdmission);
        }

        if (!DiscoveryWorkerAdmission.TryAcquire(out var admission))
        {
            return System.Threading.Tasks.Task.FromException<DeviceInfo>(
                new DiscoveryReadSkippedException(key.InterfaceId, DiscoveryReadSkipCause.WorkerAdmissionSaturated));
        }

        try
        {
            // A bounded dedicated worker starts provider/native code only after this caller can install
            // WaitAsync. Saturation skips instead of queuing or allocating another worker (best-effort
            // metadata path). Supersession is validated inside the worker, immediately before the
            // hardware open (see ConnectAndReadAsync).
            return StartWorker(scope, key, epoch, provider, admission, leaseTracker, nativeAdmission);
        }
        catch
        {
            admission.Dispose();
            throw;
        }
    }

    private static async Task<DeviceInfo> StartQueuedSharedRead(
        DiscoveryReadScope scope,
        DiscoveryReadKey key,
        TransportEpoch epoch,
        IDiscoveryConnectionProvider? provider,
        DiscoveryReadLeaseTracker leaseTracker,
        DiscoveryReadAdmission? nativeAdmission)
    {
        // The queued wait is cancelled by transport activity so superseded reads cannot accumulate behind
        // hung workers: each hotplug event would otherwise enqueue another uncancellable waiter (its
        // timed-out caller has already abandoned it) that eventually opens an interface its evidence no
        // longer names. The token belongs to the SAME epoch as the key's generation (captured atomically
        // in DiscoveryDeviceInfoReader.ReadBoundedCoreAsync), so a read created against an already-retired epoch waits on a token
        // that is already cancelled - it can never queue indefinitely.
        IDisposable admission;
        try
        {
            admission = await DiscoveryWorkerAdmission.AcquireAsync(epoch.Superseded).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw key.Superseded();
        }

        try
        {
            return await StartWorker(scope, key, epoch, provider, admission, leaseTracker, nativeAdmission).ConfigureAwait(false);
        }
        catch
        {
            // Safe double-dispose when the worker's own `using` already released the slot: Admission
            // disposal is idempotent. This guards synchronous StartNew failures.
            admission.Dispose();
            throw;
        }
    }

    private static void ThrowIfSuperseded(DiscoveryReadScope scope, DiscoveryReadKey key, TransportEpoch epoch)
    {
        if (!scope.IsCurrent(epoch))
            throw key.Superseded();
    }

    private static Task<DeviceInfo> StartWorker(
        DiscoveryReadScope scope,
        DiscoveryReadKey key,
        TransportEpoch epoch,
        IDiscoveryConnectionProvider? provider,
        IDisposable admission,
        DiscoveryReadLeaseTracker leaseTracker,
        DiscoveryReadAdmission? nativeAdmission) =>
        System.Threading.Tasks.Task.Factory.StartNew(
                async () =>
                {
                    using (admission)
                    {
                        return await ConnectAndReadAsync(
                                scope,
                                key,
                                epoch,
                                provider,
                                leaseTracker,
                                nativeAdmission,
                                CancellationToken.None)
                            .ConfigureAwait(false);
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
                TaskScheduler.Default)
            .Unwrap();

    private static async Task<DeviceInfo> ConnectAndReadAsync(
        DiscoveryReadScope scope,
        DiscoveryReadKey key,
        TransportEpoch epoch,
        IDiscoveryConnectionProvider? provider,
        DiscoveryReadLeaseTracker leaseTracker,
        DiscoveryReadAdmission? nativeAdmission,
        CancellationToken cancellationToken)
    {
        var interfaceId = key.InterfaceId;
        if (provider is null)
            throw new DiscoveryReadSkippedException(interfaceId, DiscoveryReadSkipCause.NoDiscoveryProvider);

        var discoveryLease = DeviceConnectionRegistry.TryAcquireDiscovery(interfaceId, nativeAdmission);
        if (discoveryLease is null)
            throw new DiscoveryReadSkippedException(interfaceId, DiscoveryReadSkipCause.InterfaceLeaseHeld);
        nativeAdmission?.TransferToLease();
        leaseTracker.Register(discoveryLease);
        var registrationTransferred = false;
        try
        {
            // Final supersession check, as close to the hardware open as possible: a superseded read must not
            // START a new open against hardware its evidence no longer names.
            ThrowIfSuperseded(scope, key, epoch);

            // Discovery creates this connection, so discovery disposes it. A PC/SC slot takes the discovery
            // registration before native open. Resolve the selected member when the provider is the published
            // composite device so metadata reads follow the same route as direct slot reads.
            var pcscSlot = ResolvePcscSlot(provider, key.Connection);
            // Only built-in macOS HID owns its registry claim through native teardown. Windows and Linux
            // HID use the provider fallback below until their owned lifetimes land (slices S1 and S3 in
            // docs/plans/yubikit-async-boundaries/00-status.md; design in 02-architecture.md).
            var hidSlot = ResolveMacHidSlot(provider, key.Connection);
            registrationTransferred = pcscSlot is not null || hidSlot is not null;
            var conn = pcscSlot is not null
                ? await pcscSlot.OpenRegisteredConnectionAsync(discoveryLease, cancellationToken).ConfigureAwait(false)
                : hidSlot is not null
                    ? await hidSlot.OpenRegisteredConnectionAsync(discoveryLease, cancellationToken).ConfigureAwait(false)
                    : await provider.ConnectForDiscoveryAsync(key.Connection, cancellationToken).ConfigureAwait(false);
            await using (conn.ConfigureAwait(false))
                return await ProtocolDeviceInfo.ReadAsync(conn, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (!registrationTransferred)
                discoveryLease.Dispose();
        }
    }

    private static HidConnectionSlot? ResolveMacHidSlot(IDiscoveryConnectionProvider provider, ConnectionType connection)
    {
        if (connection is not (ConnectionType.HidFido or ConnectionType.HidOtp)) return null;
        if (provider is YubiKeyDevice published)
            return published.TryResolveSlot(connection, out var selected) ? AsBuiltInMacHidSlot(selected) : null;
        return AsBuiltInMacHidSlot(provider);
    }

    private static HidConnectionSlot? AsBuiltInMacHidSlot(object candidate) =>
        candidate is HidConnectionSlot hid && (hid.IsBuiltInMacFido || hid.IsBuiltInMacOtp) ? hid : null;

    private static PcscConnectionSlot? ResolvePcscSlot(
        IDiscoveryConnectionProvider provider,
        ConnectionType connection)
    {
        if (connection != ConnectionType.SmartCard)
            return null;
        if (provider is PcscConnectionSlot pcscSlot)
            return pcscSlot;
        return provider is YubiKeyDevice published &&
               published.TryResolveSlot(connection, out var slot) &&
               slot is PcscConnectionSlot selectedPcscSlot
            ? selectedPcscSlot
            : null;
    }

    private sealed class DiscoveryReadLeaseTracker
    {
        private readonly Lock _sync = new();
        private IDisposable? _lease;
        private Exception? _abandonment;

        public void Register(IDisposable lease)
        {
            Exception? abandonment;
            lock (_sync)
            {
                _lease = lease;
                abandonment = _abandonment;
            }

            if (abandonment is not null)
                DeviceConnectionRegistry.TryMarkUnrecovered(lease, abandonment);
        }

        public void MarkAbandoned(string interfaceId)
        {
            IDisposable? lease;
            var failure = new InvalidOperationException(
                $"A bounded discovery read for interface '{interfaceId}' was abandoned while native ownership remained active.");
            lock (_sync)
            {
                _abandonment ??= failure;
                lease = _lease;
            }

            if (lease is not null)
                DeviceConnectionRegistry.TryMarkUnrecovered(lease, failure);
        }
    }
}