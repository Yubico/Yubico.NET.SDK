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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class DiscoverySingleFlightTests
{
    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task ReadBoundedAsync_SynchronousProviderBlock_StillInstallsCallerTimeoutPromptly()
    {
        using var device = new SynchronouslyBlockingConnectYubiKey();
        using var watchdogCancellation = new CancellationTokenSource();
        var watchdog = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(750), watchdogCancellation.Token);
                device.ReleaseSynchronousBlock();
            }
            catch (OperationCanceledException) when (watchdogCancellation.IsCancellationRequested)
            {
            }
        }, TestContext.Current.CancellationToken);

        var stopwatch = Stopwatch.StartNew();
        Exception? exception;
        TimeSpan elapsed;
        try
        {
            exception = await Record.ExceptionAsync(() => ProtocolDeviceInfo.ReadBoundedAsync(
                device,
                ConnectionType.SmartCard,
                TimeSpan.FromMilliseconds(50),
                NullLogger.Instance,
                TestContext.Current.CancellationToken));
            elapsed = stopwatch.Elapsed;
        }
        finally
        {
            stopwatch.Stop();
            device.ReleaseSynchronousBlock();
            watchdogCancellation.Cancel();
            await watchdog;
            await device.ConnectReturned.Task.WaitAsync(
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken);
            device.FailConnect();
            await device.ConnectFinished.Task.WaitAsync(
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken);
        }
        Assert.IsType<TimeoutException>(exception);
        Assert.True(
            elapsed < TimeSpan.FromMilliseconds(300),
            $"Caller timeout was installed only after {elapsed}; synchronous provider work escaped the budget.");
        Assert.Equal(1, device.ConnectCalls);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task ReadBoundedAsync_ManyUniqueBlockedProviders_AdmitsAtMostFourWorkersWithoutQueueing()
    {
        const int maximumWorkers = 4;
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);

        var devices = Enumerable.Range(0, maximumWorkers + 1)
            .Select(_ => new SynchronouslyBlockingConnectYubiKey())
            .ToArray();

        Exception?[] exceptions;
        try
        {
            var reads = devices.Select(device => Record.ExceptionAsync(() =>
                ProtocolDeviceInfo.ReadBoundedAsync(
                    device,
                    ConnectionType.SmartCard,
                    TimeSpan.FromMilliseconds(500),
                    NullLogger.Instance,
                    TestContext.Current.CancellationToken)).AsTask());
            exceptions = await Task.WhenAll(reads);
        }
        finally
        {
            foreach (var device in devices)
                device.ReleaseSynchronousBlock();

            foreach (var device in devices.Where(device => device.ConnectCalls > 0))
            {
                await device.ConnectReturned.Task.WaitAsync(
                    TimeSpan.FromSeconds(5),
                    TestContext.Current.CancellationToken);
                device.FailConnect();
                await device.ConnectFinished.Task.WaitAsync(
                    TimeSpan.FromSeconds(5),
                    TestContext.Current.CancellationToken);
            }

            foreach (var device in devices)
                device.Dispose();

            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(maximumWorkers, devices.Sum(device => device.ConnectCalls));
        Assert.Equal(maximumWorkers, exceptions.Count(exception => exception is TimeoutException));
        Assert.Single(exceptions, exception => exception is DiscoveryReadSkippedException);
    }

    [Fact]
    public async Task ReadBoundedAsync_TransparentWrapperWithoutDiscoveryProvider_SkipsWithoutPublicConnect()
    {
        var factory = new CountingConnectionFactory();
        var slot = new DeviceConnectionSlot(
            new PcscDevice { ReaderName = $"wrapped-reader-{Guid.NewGuid():N}", Atr = null },
            factory);
        var inner = new YubiKeyDevice(
            slot.InterfaceId, slot, hidFido: null, hidOtp: null, deviceInfo: null);
        using var wrapper = new TransparentYubiKey(inner);

        Exception? exception = null;
        try
        {
            _ = await ProtocolDeviceInfo.ReadBoundedAsync(
                wrapper,
                ConnectionType.SmartCard,
                TimeSpan.FromMilliseconds(50),
                NullLogger.Instance,
                TestContext.Current.CancellationToken);
        }
        catch (Exception e)
        {
            exception = e;
        }
        finally
        {
            wrapper.CancelBlockedConnect();
            if (wrapper.ConnectCalls > 0)
                await wrapper.ConnectExited.Task.WaitAsync(TestContext.Current.CancellationToken);
        }

        Assert.IsType<DiscoveryReadSkippedException>(exception);
        Assert.Equal(0, wrapper.ConnectCalls);
        Assert.Equal(0, factory.CreateCalls);
    }

    [Fact]
    public async Task ReadBoundedAsync_PublishedSlotWithoutRawOpen_ReportsNoDiscoveryProvider()
    {
        var smartCard = new UnsupportedDiscoveryYubiKey(ConnectionType.SmartCard);
        var hid = new UnsupportedDiscoveryYubiKey(ConnectionType.HidFido);
        var composite = new YubiKeyDevice(
            $"composite:{Guid.NewGuid():N}", smartCard, hid, hidOtp: null, deviceInfo: null);

        var exception = await Assert.ThrowsAsync<DiscoveryReadSkippedException>(() =>
            ProtocolDeviceInfo.ReadBoundedAsync(
                composite,
                ConnectionType.SmartCard,
                TimeSpan.FromSeconds(1),
                NullLogger.Instance,
                TestContext.Current.CancellationToken));

        Assert.Equal(DiscoveryReadSkipCause.NoDiscoveryProvider, exception.Cause);
        Assert.Contains(smartCard.InterfaceId, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReadBoundedAsync_RepeatedTimeouts_StartOneUnderlyingReadPerInterfaceAndConnectionType()
    {
        var device = new ControllableConnectYubiKey(cancelConnectWithCaller: false);

        try
        {
            var reads = Enumerable.Range(0, 4).Select(_ => Assert.ThrowsAsync<TimeoutException>(() =>
                ProtocolDeviceInfo.ReadBoundedAsync(
                    device,
                    ConnectionType.SmartCard,
                    TimeSpan.FromMilliseconds(50),
                    NullLogger.Instance,
                    CancellationToken.None)));

            await Task.WhenAll(reads);
            Assert.Equal(1, device.ConnectCalls);
        }
        finally
        {
            device.FailConnect();
            await device.ConnectFinished.Task.WaitAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task ReadBoundedAsync_ManyTimedOutWaiters_RetainOneSharedCompletionObserver()
    {
        const int waiterCount = 16;
        var device = new ControllableConnectYubiKey(cancelConnectWithCaller: false);
        var firstCallerLogger = new CompletionRecordingLogger();
        var laterWaiterLogger = new CompletionRecordingLogger();

        try
        {
            var firstRead = Record.ExceptionAsync(() => ProtocolDeviceInfo.ReadBoundedAsync(
                device,
                ConnectionType.SmartCard,
                TimeSpan.FromMilliseconds(100),
                firstCallerLogger,
                CancellationToken.None)).AsTask();
            await device.ConnectStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            var laterReads = Enumerable.Range(1, waiterCount - 1).Select(_ =>
                Record.ExceptionAsync(() => ProtocolDeviceInfo.ReadBoundedAsync(
                    device,
                    ConnectionType.SmartCard,
                    TimeSpan.FromMilliseconds(100),
                    laterWaiterLogger,
                    CancellationToken.None)).AsTask());
            var exceptions = await Task.WhenAll([firstRead, .. laterReads]);

            Assert.All(exceptions, exception => Assert.IsType<TimeoutException>(exception));
            Assert.Equal(1, device.ConnectCalls);
        }
        finally
        {
            device.FailConnect();
            await device.ConnectFinished.Task.WaitAsync(TestContext.Current.CancellationToken);
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        }

        await WaitForStableCompletionLogCountAsync(firstCallerLogger, laterWaiterLogger);

        var completion = Assert.Single(firstCallerLogger.CompletionMessages);
        Assert.Contains(device.DeviceId, completion, StringComparison.Ordinal);
        Assert.Contains(nameof(ConnectionType.SmartCard), completion, StringComparison.Ordinal);
        Assert.Empty(laterWaiterLogger.CompletionMessages);
    }

    [Fact]
    public async Task ReadBoundedAsync_OneWaiterCancels_DoesNotCancelSharedUnderlyingRead()
    {
        var device = new ControllableConnectYubiKey(cancelConnectWithCaller: true);
        using var firstWaiter = new CancellationTokenSource();

        try
        {
            var firstRead = ProtocolDeviceInfo.ReadBoundedAsync(
                device,
                ConnectionType.SmartCard,
                TimeSpan.FromSeconds(5),
                NullLogger.Instance,
                firstWaiter.Token);
            await device.ConnectStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            firstWaiter.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => firstRead);

            await Assert.ThrowsAsync<TimeoutException>(() => ProtocolDeviceInfo.ReadBoundedAsync(
                device,
                ConnectionType.SmartCard,
                TimeSpan.FromMilliseconds(50),
                NullLogger.Instance,
                TestContext.Current.CancellationToken));

            Assert.Equal(1, device.ConnectCalls);
        }
        finally
        {
            device.FailConnect();
            await device.ConnectFinished.Task.WaitAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task ReadBoundedAsync_TransportActivityBetweenReads_DetachesFromInFlightRead()
    {
        // Single-flight reads are keyed by reusable interface identifiers. A timed-out native read that
        // survives a removal + same-slot replacement must NOT be joined by the replacement's scan — that
        // would hand the departed key's device info to its successor. NotifyTransportActivity bumps the
        // key generation, so the post-activity read starts its own attempt instead of awaiting the old
        // one; here that attempt is refused (the abandoned read still holds the interface's discovery
        // lease) and is retried on a later scan — never resolved from pre-activity hardware's result.
        var device = new ControllableConnectYubiKey(cancelConnectWithCaller: false);

        try
        {
            await Assert.ThrowsAsync<TimeoutException>(() => ProtocolDeviceInfo.ReadBoundedAsync(
                device,
                ConnectionType.SmartCard,
                TimeSpan.FromMilliseconds(50),
                NullLogger.Instance,
                CancellationToken.None));
            Assert.Equal(1, device.ConnectCalls);

            ProtocolDeviceInfo.NotifyTransportActivity();

            // Pre-fix this joined the in-flight read (and would have consumed its DeviceInfo); post-fix
            // it is a new attempt that observes the still-held discovery lease and skips.
            var exception = await Assert.ThrowsAsync<DiscoveryReadSkippedException>(() =>
                ProtocolDeviceInfo.ReadBoundedAsync(
                    device,
                    ConnectionType.SmartCard,
                    TimeSpan.FromMilliseconds(50),
                    NullLogger.Instance,
                    CancellationToken.None));

            Assert.Equal(DiscoveryReadSkipCause.InterfaceLeaseHeld, exception.Cause);
            Assert.Equal(1, device.ConnectCalls);
        }
        finally
        {
            device.FailConnect();
            await device.ConnectFinished.Task.WaitAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task ReadBoundedAsync_TransportActivityWhileQueuedForAdmission_FailsFastWithoutConnecting()
    {
        // Queued identity reads wait (uncancelled by their caller's timeout) for a worker slot. With hung
        // native reads holding all slots, every hotplug event used to be able to enqueue another orphaned
        // waiter that would eventually open an interface its evidence no longer named. Transport activity
        // must cancel superseded queued waits promptly - not leave them accumulating behind hung workers.
        // The generation and the supersede token are captured as ONE immutable epoch, so no interleaving
        // of epoch reads and activity can produce a waiter holding an old generation with a token that
        // never fires; the double notification below leaves the queued read's epoch two generations stale
        // and it must still fail through its own epoch's token.
        const int maximumWorkers = 4;
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);

        var hungDevices = Enumerable.Range(0, maximumWorkers)
            .Select(_ => new ControllableConnectYubiKey(cancelConnectWithCaller: false))
            .ToArray();
        var queuedDevice = new ControllableConnectYubiKey(cancelConnectWithCaller: false);

        try
        {
            // Saturate all workers with hung native reads.
            var hungReads = hungDevices.Select(device => Record.ExceptionAsync(() =>
                ProtocolDeviceInfo.ReadBoundedAsync(
                    device,
                    ConnectionType.SmartCard,
                    TimeSpan.FromMilliseconds(100),
                    NullLogger.Instance,
                    CancellationToken.None)).AsTask()).ToArray();
            foreach (var device in hungDevices)
                await device.ConnectStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
            Assert.All(await Task.WhenAll(hungReads), exception => Assert.IsType<TimeoutException>(exception));

            // Queue a read behind them with a budget far larger than this test's runtime, then supersede it.
            var queuedRead = ProtocolDeviceInfo.ReadBoundedAsync(
                queuedDevice,
                ConnectionType.SmartCard,
                TimeSpan.FromSeconds(30),
                NullLogger.Instance,
                CancellationToken.None,
                waitForWorkerSlot: true);

            ProtocolDeviceInfo.NotifyTransportActivity();
            ProtocolDeviceInfo.NotifyTransportActivity();

            var exception = await Assert.ThrowsAsync<DiscoveryReadSkippedException>(() =>
                queuedRead.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.Equal(DiscoveryReadSkipCause.SupersededByTransportActivity, exception.Cause);
            Assert.Equal(0, queuedDevice.ConnectCalls);
        }
        finally
        {
            foreach (var device in hungDevices)
                device.FailConnect();
            foreach (var device in hungDevices)
                await device.ConnectFinished.Task.WaitAsync(TestContext.Current.CancellationToken);
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        }

        // Even after the hung workers free their slots, the superseded read must never reach hardware.
        Assert.Equal(0, queuedDevice.ConnectCalls);
    }

    [Fact]
    public async Task ReadBoundedAsync_FaultedRead_IsRemovedSoLaterCallRetries()
    {
        var device = new FaultingConnectYubiKey();

        await Assert.ThrowsAsync<InvalidOperationException>(() => ReadAsync(device));
        await Assert.ThrowsAsync<InvalidOperationException>(() => ReadAsync(device));

        Assert.Equal(2, device.ConnectCalls);
    }

    [Fact]
    public async Task ReadBoundedAsync_UnderlyingReadCancels_IsRemovedSoLaterCallRetries()
    {
        var device = new CancelThenFaultConnectYubiKey();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ReadAsync(device));
        await Assert.ThrowsAsync<InvalidOperationException>(() => ReadAsync(device));

        Assert.Equal(2, device.ConnectCalls);
    }

    private static Task<DeviceInfo> ReadAsync(IYubiKey device) =>
        ProtocolDeviceInfo.ReadBoundedAsync(
            device,
            ConnectionType.SmartCard,
            TimeSpan.FromSeconds(1),
            NullLogger.Instance,
            TestContext.Current.CancellationToken);

    private static async Task WaitForStableCompletionLogCountAsync(params CompletionRecordingLogger[] loggers)
    {
        var previousCount = -1;
        var stableSamples = 0;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        while (stableSamples < 5)
        {
            var currentCount = loggers.Sum(logger => logger.CompletionMessages.Count);
            stableSamples = currentCount > 0 && currentCount == previousCount
                ? stableSamples + 1
                : 0;
            previousCount = currentCount;
            await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token);
        }
    }

    private sealed class CompletionRecordingLogger : ILogger
    {
        public ConcurrentQueue<string> CompletionMessages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            if (message.Contains("finished in the background", StringComparison.Ordinal))
                CompletionMessages.Enqueue(message);
        }
    }

    private sealed class ControllableConnectYubiKey(bool cancelConnectWithCaller) : IYubiKey, IDiscoveryConnectionProvider
    {
        private readonly TaskCompletionSource<IConnection> _connection =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _connectCalls;

        public TaskCompletionSource ConnectStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ConnectFinished { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ConnectCalls => Volatile.Read(ref _connectCalls);

        public string DeviceId { get; } = $"test:single-flight:{Guid.NewGuid():N}";

        public ConnectionType AvailableConnections => ConnectionType.SmartCard;

        public async Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            _ = Interlocked.Increment(ref _connectCalls);
            ConnectStarted.TrySetResult();

            try
            {
                if (cancelConnectWithCaller)
                    await _connection.Task.WaitAsync(cancellationToken);
                else
                    await _connection.Task;

                throw new InvalidOperationException("The test connection should remain incomplete.");
            }
            finally
            {
                ConnectFinished.TrySetResult();
            }
        }

        Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
            ConnectionType connection,
            CancellationToken cancellationToken) =>
            ConnectAsync<IConnection>(cancellationToken);

        public void FailConnect() =>
            _connection.TrySetException(new InvalidOperationException("Expected cleanup failure."));
    }

    private sealed class SynchronouslyBlockingConnectYubiKey : IYubiKey, IDiscoveryConnectionProvider, IDisposable
    {
        private readonly ManualResetEventSlim _releaseSynchronousBlock = new(false);
        private readonly TaskCompletionSource<IConnection> _connection =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _connectCalls;

        public TaskCompletionSource ConnectReturned { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ConnectFinished { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ConnectCalls => Volatile.Read(ref _connectCalls);

        public string DeviceId { get; } = $"test:synchronously-blocking-single-flight:{Guid.NewGuid():N}";

        public ConnectionType AvailableConnections => ConnectionType.SmartCard;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            Task.FromException<TConnection>(new InvalidOperationException("Public connect must not be used by discovery."));

        async Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
            ConnectionType connection,
            CancellationToken cancellationToken)
        {
            _ = Interlocked.Increment(ref _connectCalls);
            _releaseSynchronousBlock.Wait();
            ConnectReturned.TrySetResult();
            try
            {
                return await _connection.Task;
            }
            finally
            {
                ConnectFinished.TrySetResult();
            }
        }

        public void ReleaseSynchronousBlock() => _releaseSynchronousBlock.Set();

        public void FailConnect() =>
            _connection.TrySetException(new InvalidOperationException("Expected cleanup failure."));

        public void Dispose() => _releaseSynchronousBlock.Dispose();
    }

    private sealed class FaultingConnectYubiKey : IYubiKey, IDiscoveryConnectionProvider
    {
        private int _connectCalls;

        public int ConnectCalls => Volatile.Read(ref _connectCalls);

        public string DeviceId { get; } = $"test:faulting-single-flight:{Guid.NewGuid():N}";

        public ConnectionType AvailableConnections => ConnectionType.SmartCard;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            _ = Interlocked.Increment(ref _connectCalls);
            return Task.FromException<TConnection>(new InvalidOperationException("Expected connect failure."));
        }

        Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
            ConnectionType connection,
            CancellationToken cancellationToken) =>
            ConnectAsync<IConnection>(cancellationToken);
    }

    private sealed class CancelThenFaultConnectYubiKey : IYubiKey, IDiscoveryConnectionProvider
    {
        private int _connectCalls;

        public int ConnectCalls => Volatile.Read(ref _connectCalls);

        public string DeviceId { get; } = $"test:canceling-single-flight:{Guid.NewGuid():N}";

        public ConnectionType AvailableConnections => ConnectionType.SmartCard;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            Interlocked.Increment(ref _connectCalls) == 1
                ? Task.FromCanceled<TConnection>(new CancellationToken(canceled: true))
                : Task.FromException<TConnection>(new InvalidOperationException("Expected retry failure."));

        Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
            ConnectionType connection,
            CancellationToken cancellationToken) =>
            ConnectAsync<IConnection>(cancellationToken);
    }

    private sealed class TransparentYubiKey(IYubiKey inner) : IYubiKey, IDisposable
    {
        private readonly CancellationTokenSource _escape = new();
        private int _connectCalls;

        public TaskCompletionSource ConnectExited { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ConnectCalls => Volatile.Read(ref _connectCalls);

        public string DeviceId => inner.DeviceId;

        public ConnectionType AvailableConnections => inner.AvailableConnections;

        public async Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            _ = Interlocked.Increment(ref _connectCalls);
            try
            {
                return await inner.ConnectAsync<TConnection>(_escape.Token);
            }
            finally
            {
                ConnectExited.TrySetResult();
            }
        }

        public void CancelBlockedConnect() => _escape.Cancel();

        public void Dispose() => _escape.Dispose();
    }

    private sealed class UnsupportedDiscoveryYubiKey(ConnectionType availableConnections) : IYubiKeyConnectionSlot
    {
        public string InterfaceId { get; } = $"unsupported:{availableConnections}:{Guid.NewGuid():N}";

        public ConnectionType ConnectionType => availableConnections;
    }

    private sealed class CountingConnectionFactory : ISmartCardConnectionFactory
    {
        private int _createCalls;

        public int CreateCalls => Volatile.Read(ref _createCalls);

        public Task<ISmartCardConnection> CreateAsync(
            IPcscDevice smartCardDevice,
            CancellationToken cancellationToken = default)
        {
            _ = Interlocked.Increment(ref _createCalls);
            return Task.FromException<ISmartCardConnection>(new InvalidOperationException("Physical connect must not run."));
        }
    }
}