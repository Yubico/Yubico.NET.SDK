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
        CancellationToken cancellationToken)
    {
        var key = new ReadKey(DeviceConnectionRegistry.ResolveInterfaceId(device, connection), connection);
        var sharedRead = InFlightReads.GetOrAdd(
            key,
            _ => new SharedRead(key, device, connection, logger));

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
            ILogger logger)
        {
            _key = key;
            _deviceId = device.DeviceId;
            _connection = connection;
            _logger = logger;
            _task = new Lazy<Task<DeviceInfo>>(
                () => StartAndObserve(device),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public Task<DeviceInfo> Task => _task.Value;

        public void RecordAbandonment() => Interlocked.Increment(ref _abandonedWaiterCount);

        private Task<DeviceInfo> StartAndObserve(IYubiKey device)
        {
            var task = StartSharedRead(device, _connection);
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

    private static Task<DeviceInfo> StartSharedRead(IYubiKey device, ConnectionType connection)
    {
        var interfaceId = DeviceConnectionRegistry.ResolveInterfaceId(device, connection);
        if (!DiscoveryWorkerAdmission.TryAcquire(out var admission))
            return Task.FromException<DeviceInfo>(new DiscoveryReadSkippedException(interfaceId));

        try
        {
            // A bounded dedicated worker starts provider/native code only after this caller can install
            // WaitAsync. Saturation skips instead of queuing or allocating another worker.
            return Task.Factory.StartNew(
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
        }
        catch
        {
            admission.Dispose();
            throw;
        }
    }

    private static async Task<DeviceInfo> ConnectAndReadAsync(
        IYubiKey device,
        ConnectionType connection,
        CancellationToken cancellationToken)
    {
        var interfaceId = DeviceConnectionRegistry.ResolveInterfaceId(device, connection);
        if (device is not IDiscoveryConnectionProvider provider)
            throw new DiscoveryReadSkippedException(interfaceId);

        using var discoveryLease = DeviceConnectionRegistry.TryAcquireDiscovery(interfaceId);
        if (discoveryLease is null)
            throw new DiscoveryReadSkippedException(interfaceId);

        var conn = await provider.ConnectForDiscoveryAsync(connection, cancellationToken).ConfigureAwait(false);
        return await ReadAsync(conn, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<DeviceInfo> ReadAsync(IConnection connection, CancellationToken cancellationToken)
    {
        var protocol = YubiKeyProtocol.Create(connection);
        try
        {
            if (protocol is YubiKeyProtocol.SmartCard smartCard)
            {
                await smartCard.Protocol.SelectAsync(ApplicationIds.Management, cancellationToken).ConfigureAwait(false);
            }
            else if (protocol is YubiKeyProtocol.FidoHid fidoHid)
            {
                await fidoHid.Protocol.InitializeAsync(cancellationToken).ConfigureAwait(false);
            }

            return await DeviceInfoReader.ReadAsync(protocol.Inner, null, cancellationToken).ConfigureAwait(false);
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