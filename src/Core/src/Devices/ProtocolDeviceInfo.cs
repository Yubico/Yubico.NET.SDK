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
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Reads <see cref="DeviceInfo"/> over an already-open connection by building the matching Core protocol.
/// </summary>
/// <remarks>
///     Takes ownership of the supplied connection: it builds a protocol over the connection and disposes the
///     protocol (which disposes the connection) before returning. The caller must not dispose the connection
///     separately. Shared by discovery's serial-disambiguation read and the composite metadata read.
/// </remarks>
internal static class ProtocolDeviceInfo
{
    private static readonly ConcurrentDictionary<ReadKey, SharedRead> InFlightReads = new();

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
    ///         protocol/connection through the normal <see cref="ReadAsync" /> control flow when the native
    ///         call eventually returns.
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
        bool waitForWorkerSlot = false)
    {
        var key = new ReadKey(DeviceConnectionRegistry.ResolveInterfaceId(device, connection), connection);
        var sharedRead = InFlightReads.GetOrAdd(
            key,
            _ => new SharedRead(key, device, connection, logger, waitForWorkerSlot));

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

    private readonly record struct ReadKey(string InterfaceId, ConnectionType Connection);

    private sealed class SharedRead
    {
        private readonly ReadKey _key;
        private readonly string _deviceId;
        private readonly ConnectionType _connection;
        private readonly ILogger _logger;
        private readonly Lazy<Task<DeviceInfo>> _task;
        private int _abandonedWaiterCount;

        public SharedRead(
            ReadKey key,
            IYubiKey device,
            ConnectionType connection,
            ILogger logger,
            bool waitForWorkerSlot)
        {
            _key = key;
            _deviceId = device.DeviceId;
            _connection = connection;
            _logger = logger;
            _task = new Lazy<Task<DeviceInfo>>(
                () => StartAndObserve(device, waitForWorkerSlot),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public Task<DeviceInfo> Task => _task.Value;

        public void RecordAbandonment() => Interlocked.Increment(ref _abandonedWaiterCount);

        private Task<DeviceInfo> StartAndObserve(IYubiKey device, bool waitForWorkerSlot)
        {
            var task = StartSharedRead(device, _connection, waitForWorkerSlot);
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
            _ = InFlightReads.TryRemove(KeyValuePair.Create(_key, this));

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

    private static Task<DeviceInfo> StartSharedRead(IYubiKey device, ConnectionType connection, bool waitForWorkerSlot)
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
            return StartQueuedSharedRead(device, connection);
        }

        var interfaceId = DeviceConnectionRegistry.ResolveInterfaceId(device, connection);
        if (!DiscoveryWorkerAdmission.TryAcquire(out var admission))
        {
            return Task.FromException<DeviceInfo>(
                new DiscoveryReadSkippedException(interfaceId, DiscoveryReadSkipCause.WorkerAdmissionSaturated));
        }

        try
        {
            // A bounded dedicated worker starts provider/native code only after this caller can install
            // WaitAsync. Saturation skips instead of queuing or allocating another worker (best-effort
            // metadata path).
            return StartWorker(device, connection, admission);
        }
        catch
        {
            admission.Dispose();
            throw;
        }
    }

    private static async Task<DeviceInfo> StartQueuedSharedRead(IYubiKey device, ConnectionType connection)
    {
        var admission = await DiscoveryWorkerAdmission.AcquireAsync().ConfigureAwait(false);
        try
        {
            return await StartWorker(device, connection, admission).ConfigureAwait(false);
        }
        catch
        {
            // Safe double-dispose when the worker's own `using` already released the slot: Admission
            // disposal is idempotent. This guards only synchronous StartNew failures.
            admission.Dispose();
            throw;
        }
    }

    private static Task<DeviceInfo> StartWorker(IYubiKey device, ConnectionType connection, IDisposable admission) =>
        Task.Factory.StartNew(
                async () =>
                {
                    using (admission)
                    {
                        return await ConnectAndReadAsync(device, connection, CancellationToken.None)
                            .ConfigureAwait(false);
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
                TaskScheduler.Default)
            .Unwrap();

    private static async Task<DeviceInfo> ConnectAndReadAsync(
        IYubiKey device,
        ConnectionType connection,
        CancellationToken cancellationToken)
    {
        var interfaceId = DeviceConnectionRegistry.ResolveInterfaceId(device, connection);
        if (device is not IDiscoveryConnectionProvider provider)
            throw new DiscoveryReadSkippedException(interfaceId, DiscoveryReadSkipCause.NoDiscoveryProvider);

        using var discoveryLease = DeviceConnectionRegistry.TryAcquireDiscovery(interfaceId);
        if (discoveryLease is null)
            throw new DiscoveryReadSkippedException(interfaceId, DiscoveryReadSkipCause.InterfaceLeaseHeld);

        var conn = await provider.ConnectForDiscoveryAsync(connection, cancellationToken).ConfigureAwait(false);
        return await ReadAsync(conn, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<DeviceInfo> ReadAsync(IConnection connection, CancellationToken cancellationToken)
    {
        switch (connection)
        {
            case ISmartCardConnection smartCard:
                {
                    var protocol = PcscProtocolFactory<ISmartCardConnection>.Create().Create(smartCard);
                    try
                    {
                        await protocol.SelectAsync(ApplicationIds.Management, cancellationToken).ConfigureAwait(false);
                        return await DeviceInfoReader.ReadAsync(protocol, null, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        protocol.Dispose();
                    }
                }
            case IFidoHidConnection fido:
                {
                    var protocol = FidoProtocolFactory.Create().Create(fido);
                    try
                    {
                        // Initializes the HID channel; the application id is unused for HID.
                        await protocol.SelectAsync(ApplicationIds.Management, cancellationToken).ConfigureAwait(false);
                        return await DeviceInfoReader.ReadAsync(protocol, null, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        protocol.Dispose();
                    }
                }
            case IOtpHidConnection otp:
                {
                    var protocol = OtpProtocolFactory.Create().Create(otp);
                    try
                    {
                        return await DeviceInfoReader.ReadAsync(protocol, null, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        protocol.Dispose();
                    }
                }
            default:
                throw new NotSupportedException(
                    $"Connection type {connection.GetType().Name} is not supported for reading device info.");
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