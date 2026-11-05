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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.Infrastructure;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class YubiKeyMonitorTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CreateAsync_WaitsForFirstLoopPublication_WithoutDuplicateInitialScan()
    {
        var fixture = new MonitorFixture();
        var scanEntered = Signal();
        var releaseScan = Signal();
        var publicationEntered = Signal();
        var releasePublication = Signal();
        var waiting = Signal();
        var device = new FakeYubiKey("initial");
        fixture.Finder.Scan = async ct =>
        {
            Assert.True(fixture.Hid.Started && fixture.SmartCard.Started);
            scanEntered.TrySetResult();
            await releaseScan.Task.WaitAsync(ct);
            return [device];
        };
        fixture.Service.PublishAdmittedForTest = async () =>
        {
            publicationEntered.TrySetResult();
            await releasePublication.Task.WaitAsync(TestToken);
        };
        fixture.Service.MonitoringWaitReachedForTest = () => waiting.TrySetResult();

        var creation = fixture.CreateAsync(TestToken);
        try
        {
            await scanEntered.Task.WaitAsync(Bound, TestToken);
            Assert.False(creation.IsCompleted);
            releaseScan.TrySetResult();
            await publicationEntered.Task.WaitAsync(Bound, TestToken);
            Assert.False(creation.IsCompleted);
            releasePublication.TrySetResult();

            await using var monitor = await creation.WaitAsync(Bound, TestToken);
            await waiting.Task.WaitAsync(Bound, TestToken);
            Assert.Same(device, Assert.Single(monitor.CurrentDevices));
            Assert.Null(((IYubiKey)device).CachedDeviceInfo);
            Assert.Equal(1, fixture.Finder.ScanCount);
        }
        finally
        {
            releaseScan.TrySetResult();
            releasePublication.TrySetResult();
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task CreateAsync_EmptyScan_IsReadyAndCachedReadsPerformNoDiscovery()
    {
        await using var fixture = new MonitorFixture();
        await using var monitor = await fixture.CreateAsync(TestToken);

        Assert.True(fixture.Repository.HasData);
        for (var i = 0; i < 100; i++)
            Assert.Empty(monitor.CurrentDevices);
        Assert.Equal(1, fixture.Finder.ScanCount);
    }

    [Fact]
    public async Task CreateAsync_PreCanceled_CleansOwnershipWithoutStartingListenersOrScanning()
    {
        await using var fixture = new MonitorFixture();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.CreateAsync(cancellation.Token));

        Assert.False(fixture.Hid.Started || fixture.SmartCard.Started);
        Assert.Equal(0, fixture.Finder.ScanCount);
        Assert.Throws<ObjectDisposedException>(() => fixture.Repository.GetAll());
    }

    [Fact]
    public async Task CreateAsync_InitialScanFailure_PropagatesAndCleansPartialLifetime()
    {
        await using var fixture = new MonitorFixture();
        var failure = new IOException("initial enumeration failed");
        fixture.Finder.Scan = _ => throw failure;

        var actual = await Assert.ThrowsAsync<IOException>(() => fixture.CreateAsync(TestToken));

        Assert.Same(failure, actual);
        Assert.True(fixture.Hid.Disposed && fixture.SmartCard.Disposed);
        Assert.False(fixture.Service.IsMonitoring);
        Assert.False(fixture.Repository.HasData);
        Assert.Throws<ObjectDisposedException>(() => fixture.Repository.GetAll());
    }

    [Fact]
    public async Task CreateAsync_PublicationFailure_DoesNotReturnAnInitializedEmptyMonitor()
    {
        await using var fixture = new MonitorFixture();
        var failure = new IOException("publication failed");
        fixture.Service.PublishAdmittedForTest = () => throw failure;

        Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => fixture.CreateAsync(TestToken)));
        Assert.True(fixture.Hid.Disposed && fixture.SmartCard.Disposed);
        Assert.False(fixture.Repository.HasData);
    }

    [Fact]
    public async Task CreateAsync_CanceledDuringNativeScan_CleansUpAndRejectsLatePublication()
    {
        await using var fixture = new MonitorFixture();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var entered = Signal();
        var release = Signal();
        var finished = Signal();
        fixture.Finder.Scan = async _ =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(TestToken); // Simulates native work ignoring lifetime cancellation.
            finished.TrySetResult();
            return [new FakeYubiKey("late")];
        };
        var creation = fixture.CreateAsync(cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(Bound, TestToken);
            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => creation.WaitAsync(Bound, TestToken));
            Assert.True(fixture.Hid.Disposed && fixture.SmartCard.Disposed);
            Assert.Throws<ObjectDisposedException>(() => fixture.Repository.GetAll());
            release.TrySetResult();
            await finished.Task.WaitAsync(Bound, TestToken);
            Assert.False(fixture.Repository.HasData);
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Fact]
    public async Task CreateAsync_CreationTokenCanceledAfterReturn_DoesNotStopMonitoring()
    {
        await using var fixture = new MonitorFixture();
        var updated = Signal();
        fixture.Service.MonitoringWaitReachedForTest = () =>
        {
            if (fixture.Finder.ScanCount == 2)
                updated.TrySetResult();
        };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        await using var monitor = await fixture.CreateAsync(cancellation.Token);
        await using var watcher = await DeviceEventWatcher.StartAsync(
            monitor.WatchAsync, () => fixture.Repository.WatcherCount, TestToken);
        await cancellation.CancelAsync();

        var device = new FakeYubiKey("after-creation");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([device]);
        fixture.SmartCard.Raise();
        await watcher.WaitForCountAsync(1, "creation cancellation stopped the monitor", TestToken);
        await updated.Task.WaitAsync(Bound, TestToken);

        Assert.True(fixture.Service.IsMonitoring);
        Assert.Same(device, Assert.Single(monitor.CurrentDevices));
    }

    [Fact]
    public async Task CreateAsync_HintDuringInitialScan_IsInvalidatedAndReconciled()
    {
        await using var fixture = new MonitorFixture();
        var entered = Signal();
        var release = Signal();
        var first = new FakeYubiKey("before-hint");
        var next = new FakeYubiKey("after-hint");
        fixture.Finder.Scan = async ct =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(ct);
            return [first];
        };
        var creation = fixture.CreateAsync(TestToken);
        try
        {
            await entered.Task.WaitAsync(Bound, TestToken);
            fixture.SmartCard.Raise();
            fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([next]);
            release.TrySetResult();
            await using var monitor = await creation.WaitAsync(Bound, TestToken);
            await AsyncWait.WaitUntilAsync(
                () => monitor.CurrentDevices.Count == 1 && ReferenceEquals(monitor.CurrentDevices[0], next),
                "startup hint was lost", Bound, TestToken);
            Assert.Equal(1, fixture.Finder.ActivityCount);
            Assert.Equal(2, fixture.Finder.ScanCount);
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Fact]
    public async Task CurrentDevices_IsReadOnlyHistoricalMembership_NotTheFindersList()
    {
        await using var fixture = new MonitorFixture();
        var first = new FakeYubiKey("first");
        var source = new List<IYubiKey> { first };
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>(source);
        await using var monitor = await fixture.CreateAsync(TestToken);
        var snapshot = monitor.CurrentDevices;
        source.Clear();

        Assert.Same(first, Assert.Single(snapshot));
        if (snapshot is IList<IYubiKey> mutable)
            Assert.Throws<NotSupportedException>(() => mutable.Clear());

        var second = new FakeYubiKey("second");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([second]);
        await fixture.Service.RescanAsync(TestToken);
        Assert.Same(first, Assert.Single(snapshot));
        Assert.Same(second, Assert.Single(monitor.CurrentDevices));
        await monitor.DisposeAsync();
        Assert.Same(first, Assert.Single(snapshot));
        Assert.Throws<ObjectDisposedException>(() => monitor.CurrentDevices);
        await using var afterDisposal = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.False(await afterDisposal.MoveNextAsync());
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task BackgroundScanFailure_RetainsPublishedMembership_AndNextHintRetries()
    {
        await using var fixture = new MonitorFixture();
        var first = new FakeYubiKey("first");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([first]);
        var afterFailure = Signal();
        var afterRetry = Signal();
        var waits = 0;
        fixture.Service.MonitoringWaitReachedForTest = () =>
        {
            var count = Interlocked.Increment(ref waits);
            if (count == 2)
                afterFailure.TrySetResult();
            if (count == 3)
                afterRetry.TrySetResult();
        };
        await using var monitor = await fixture.CreateAsync(TestToken);
        await using var watcher = await DeviceEventWatcher.StartAsync(
            monitor.WatchAsync, () => fixture.Repository.WatcherCount, TestToken);
        fixture.Finder.Scan = _ => throw new IOException("transient scan failure");
        fixture.SmartCard.Raise();
        await afterFailure.Task.WaitAsync(Bound, TestToken);

        Assert.Same(first, Assert.Single(monitor.CurrentDevices));
        Assert.Empty(watcher.Events);
        var second = new FakeYubiKey("second");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([second]);
        fixture.SmartCard.Raise();
        await watcher.WaitForCountAsync(2, "monitor did not recover from a failed scan", TestToken);
        await afterRetry.Task.WaitAsync(Bound, TestToken);
        Assert.Same(second, Assert.Single(monitor.CurrentDevices));
        Assert.Equal(3, fixture.Finder.ScanCount);
        Assert.Collection(watcher.Events,
            change =>
            {
                Assert.Equal(DeviceAction.Removed, change.Action);
                Assert.Same(first, change.Device);
            },
            change =>
            {
                Assert.Equal(DeviceAction.Added, change.Action);
                Assert.Same(second, change.Device);
            });
    }

    [Fact]
    public async Task Monitors_AreIndependent_DisposingOneLeavesTheOtherLive()
    {
        await using var first = new MonitorFixture();
        await using var second = new MonitorFixture();
        await using var firstMonitor = await first.CreateAsync(TestToken);
        await using var secondMonitor = await second.CreateAsync(TestToken);
        await using var firstWatcher = await DeviceEventWatcher.StartAsync(
            firstMonitor.WatchAsync, () => first.Repository.WatcherCount, TestToken);
        await using var secondWatcher = await DeviceEventWatcher.StartAsync(
            secondMonitor.WatchAsync, () => second.Repository.WatcherCount, TestToken);

        await firstMonitor.DisposeAsync();
        await firstWatcher.Completion.WaitAsync(Bound, TestToken);
        var device = new FakeYubiKey("survivor");
        second.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([device]);
        await second.Service.RescanAsync(TestToken);
        await secondWatcher.WaitForCountAsync(1, "disposing the other monitor stopped delivery", TestToken);

        Assert.True(second.Service.IsMonitoring);
        Assert.Same(device, Assert.Single(secondMonitor.CurrentDevices));
        Assert.False(secondWatcher.Completion.IsCompleted);
    }

    [Fact]
    public async Task DisposeAsync_AdmittedPublicationOutlivesBoundedDrain_CannotRepopulateOrEmit()
    {
        await using var fixture = new MonitorFixture();
        await using var monitor = await fixture.CreateAsync(TestToken);
        await using var watcher = await DeviceEventWatcher.StartAsync(
            monitor.WatchAsync, () => fixture.Repository.WatcherCount, TestToken);
        var admitted = Signal();
        var release = Signal();
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([new FakeYubiKey("late")]);
        fixture.Service.PublishAdmittedForTest = async () =>
        {
            admitted.TrySetResult();
            await release.Task.WaitAsync(TestToken);
        };
        var publication = fixture.Service.RescanAsync(TestToken);
        try
        {
            await admitted.Task.WaitAsync(Bound, TestToken);
            await monitor.DisposeAsync();
            await monitor.DisposeAsync();
            release.TrySetResult();
            await publication.WaitAsync(Bound, TestToken);
            await watcher.Completion.WaitAsync(Bound, TestToken);
            Assert.Empty(watcher.Events);
            Assert.False(fixture.Repository.HasData);
            Assert.Throws<ObjectDisposedException>(() => fixture.Repository.GetAll());
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Fact]
    public async Task DisposeAsync_ConcurrentCallsShareCompletion_UntilRepositoryTeardownFinishes()
    {
        await using var fixture = new MonitorFixture();
        await using var monitor = await fixture.CreateAsync(TestToken);
        var teardown = Signal();
        var release = Signal();
        monitor.RepositoryTeardownReachedForTest = async () =>
        {
            teardown.TrySetResult();
            await release.Task.WaitAsync(TestToken);
        };
        var first = monitor.DisposeAsync().AsTask();
        try
        {
            await teardown.Task.WaitAsync(Bound, TestToken);
            var second = monitor.DisposeAsync().AsTask();
            Assert.False(first.IsCompleted);
            Assert.False(second.IsCompleted);
            Assert.Throws<ObjectDisposedException>(() => monitor.CurrentDevices);
            release.TrySetResult();
            await Task.WhenAll(first, second).WaitAsync(Bound, TestToken);
            Assert.False(fixture.Repository.HasData);
            Assert.Throws<ObjectDisposedException>(() => fixture.Repository.GetAll());
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task CreateAsync_CancellationRetainsAdmittedReadLeaseUntilNativeTeardown()
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestToken);
        var rig = new FindYubiKeysScopeTests.ScopeRig { PendingOpen = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        using var repository = new YubiKeyDeviceRepository();
        await using var service = new YubiKeyDeviceMonitorService(
            repository, rig.CreateFinder(ConnectionType.SmartCard),
            () => new FakeHidListener(), () => new FakeSmartCardListener(), TimeSpan.FromMilliseconds(50));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var creation = YubiKeyMonitor.CreateAsync(repository, service, cancellation.Token, TimeSpan.FromHours(1));
        try
        {
            await rig.OpenEntered.Task.WaitAsync(Bound, TestToken);
            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => creation.WaitAsync(Bound, TestToken));

            var id = rig.InterfaceId(FindYubiKeysScopeTests.ScopeRig.Reader);
            Assert.Throws<UnrecoveredConnectionException>(() => DeviceConnectionRegistry.TryAcquireDiscovery(id));
            var recovery = DeviceConnectionRegistry.WaitForRecoveryForTest(id);
            Assert.False(recovery.IsCompleted);
            rig.PendingOpen.SetResult(rig.CreateIdentityConnection(111));
            await recovery.WaitAsync(Bound, TestToken);
            using var next = DeviceConnectionRegistry.TryAcquireDiscovery(id);
            Assert.NotNull(next);
            Assert.All(rig.Connections, connection => Assert.Equal(1, connection.DisposeCount));
            Assert.False(repository.HasData);
        }
        finally
        {
            rig.PendingOpen.TrySetException(new InvalidOperationException("Test cleanup"));
            // Admitted work owns these fakes until teardown, including when runner cancellation fires.
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(CancellationToken.None);
        }
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task DisposeAsync_RetiresQueuedBuiltInFinderReadsBeforeWorkerCapacityReturns()
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestToken);
        var admissions = new List<IDisposable>();
        var rig = new FindYubiKeysScopeTests.ScopeRig { IncludeSecondKey = true };
        var finder = rig.CreateFinder(ConnectionType.SmartCard);
        using var repository = new YubiKeyDeviceRepository();
        await using var service = new YubiKeyDeviceMonitorService(
            repository, finder, () => new FakeHidListener(), () => new FakeSmartCardListener(), TimeSpan.FromMilliseconds(50));
        try
        {
            for (var i = 0; i < DiscoveryWorkerAdmission.MaximumConcurrentWorkers; i++)
            {
                Assert.True(DiscoveryWorkerAdmission.TryAcquire(out var admission));
                admissions.Add(admission);
            }

            await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service, TestToken, TimeSpan.FromHours(1));
            Assert.Equal(2, monitor.CurrentDevices.Count);
            await monitor.DisposeAsync();

            // A live scope would join its queued reads and wait the 2s read budget. A retired scope
            // cannot reopen or wait on them, even though worker admission is still deliberately full.
            await finder.FindAllAsync(ConnectionType.All, TestToken).WaitAsync(TimeSpan.FromSeconds(1), TestToken);
            Assert.Empty(rig.OpenCalls);
        }
        finally
        {
            foreach (var admission in admissions)
                admission.Dispose();
        }

        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestToken);
        Assert.Empty(rig.OpenCalls);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task TerminalLoopFailure_FaultsActiveAndFutureWatchers_AndRejectsSnapshotAccess()
    {
        await using var fixture = new MonitorFixture();
        var reachedWait = Signal();
        var failure = new IOException("terminal loop failure");
        fixture.Service.MonitoringWaitReachedForTest = () => reachedWait.TrySetResult();
        await using var monitor = await fixture.CreateAsync(TestToken);
        await reachedWait.Task.WaitAsync(Bound, TestToken);
        await using var watcher = await DeviceEventWatcher.StartAsync(
            monitor.WatchAsync, () => fixture.Repository.WatcherCount, TestToken);
        fixture.Service.MonitoringWaitReachedForTest = () => throw failure;
        fixture.SmartCard.Raise();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => watcher.Completion.WaitAsync(Bound, TestToken));
        Assert.Same(failure, actual.InnerException);
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => monitor.CurrentDevices).InnerException);
        await using var future = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        var futureFailure = await Assert.ThrowsAsync<InvalidOperationException>(() => future.MoveNextAsync().AsTask());
        Assert.Same(failure, futureFailure.InnerException);
    }

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class MonitorFixture : IAsyncDisposable
    {
        public YubiKeyDeviceRepository Repository { get; } = new();
        public ScriptedFinder Finder { get; } = new();
        public FakeHidListener Hid { get; } = new();
        public FakeSmartCardListener SmartCard { get; } = new();
        public YubiKeyDeviceMonitorService Service { get; }

        public MonitorFixture() => Service = new(
            Repository, Finder, () => Hid, () => SmartCard, TimeSpan.FromMilliseconds(50));

        public Task<YubiKeyMonitor> CreateAsync(CancellationToken ct) =>
            YubiKeyMonitor.CreateAsync(Repository, Service, ct, TimeSpan.FromHours(1));

        public async ValueTask DisposeAsync()
        {
            await Service.DisposeAsync();
            Repository.Dispose();
        }
    }

    private sealed class ScriptedFinder : IFindYubiKeys
    {
        private int _scanCount;
        private int _activityCount;
        public Func<CancellationToken, Task<IReadOnlyList<IYubiKey>>> Scan { get; set; } =
            _ => Task.FromResult<IReadOnlyList<IYubiKey>>([]);
        public int ScanCount => Volatile.Read(ref _scanCount);
        public int ActivityCount => Volatile.Read(ref _activityCount);
        public Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _scanCount);
            return Scan(cancellationToken);
        }
        public void NotifyTransportActivity(ConnectionType transport) => Interlocked.Increment(ref _activityCount);
    }

    private sealed class FakeHidListener : HidDeviceListener
    {
        public bool Started => Status == DeviceListenerStatus.Started;
        public bool Disposed { get; private set; }
        public override void Start() => Status = DeviceListenerStatus.Started;
        public override void Stop() => Status = DeviceListenerStatus.Stopped;
        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class FakeSmartCardListener : ISmartCardDeviceListener
    {
        public Action? DeviceEvent { get; set; }
        public DeviceListenerStatus Status { get; private set; }
        public bool Started => Status == DeviceListenerStatus.Started;
        public bool Disposed { get; private set; }
        public void Start() => Status = DeviceListenerStatus.Started;
        public void Stop() => Status = DeviceListenerStatus.Stopped;
        public void Raise() => DeviceEvent?.Invoke();
        public void Dispose() => Disposed = true;
    }
}