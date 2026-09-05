// Copyright 2025 Yubico AB
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

/// <summary>
/// Tests for <see cref="YubiKeyDeviceManager"/> - composition root for device management.
/// </summary>
public class YubiKeyDeviceManagerTests
{

    [Fact]
    public async Task FindAllAsync_FirstCall_PerformsScan()
    {
        // Arrange
        var (manager, findYubiKeys, repository) = CreateManager();
        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);

        // Act
        var devices = await manager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(devices);
        Assert.Equal(1, findYubiKeys.ScanCount);

        await manager.DisposeAsync();
    }

    [Fact]
    public async Task FindAllAsync_SubsequentCall_ReturnsCache()
    {
        // Arrange
        var (manager, findYubiKeys, repository) = CreateManager();
        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);

        // First call - triggers scan
        await manager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, findYubiKeys.ScanCount);

        // Change available devices (should not be seen)
        findYubiKeys.SetDevices([
            new FakeYubiKey("device-1", ConnectionType.SmartCard),
            new FakeYubiKey("device-2", ConnectionType.HidFido)
        ]);

        // Act - Second call
        var devices = await manager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert - Returns cached result, no new scan
        Assert.Single(devices);
        Assert.Equal(1, findYubiKeys.ScanCount);

        await manager.DisposeAsync();
    }

    [Fact]
    public async Task FindAllAsync_ForceRescan_AlwaysScans()
    {
        // Arrange
        var (manager, findYubiKeys, repository) = CreateManager();
        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);

        // First call
        await manager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, findYubiKeys.ScanCount);

        // Change available devices
        findYubiKeys.SetDevices([
            new FakeYubiKey("device-1", ConnectionType.SmartCard),
            new FakeYubiKey("device-2", ConnectionType.HidFido)
        ]);

        // Act - Force rescan
        var devices = await manager.FindAllAsync(forceRescan: true, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - Sees new devices
        Assert.Equal(2, devices.Count);
        Assert.Equal(2, findYubiKeys.ScanCount);

        await manager.DisposeAsync();
    }

    [Fact]
    public async Task FindAllAsync_WhileMonitoring_ReturnsCache()
    {
        // Arrange
        var (manager, findYubiKeys, repository) = CreateManager();
        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);

        // Populate cache with an initial scan before monitoring starts
        await manager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Start monitoring performs one startup rescan, then FindAllAsync returns cache.
        manager.StartMonitoring(TimeSpan.FromSeconds(10));

        await AsyncWait.WaitUntilAsync(() => findYubiKeys.ScanCount >= 2, "Monitoring startup rescan did not run");
        var scanCountAfterStart = findYubiKeys.ScanCount;

        // Act - Call FindAllAsync while monitoring
        var devices = await manager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert - Returns cache, no additional scan
        Assert.Single(devices);
        Assert.Equal(scanCountAfterStart, findYubiKeys.ScanCount);

        manager.StopMonitoring();
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task FindAllAsync_WithTypeFilter_ReturnsFilteredDevices()
    {
        // Arrange
        var (manager, findYubiKeys, repository) = CreateManager();
        findYubiKeys.SetDevices([
            new FakeYubiKey("device-1", ConnectionType.SmartCard),
            new FakeYubiKey("device-2", ConnectionType.HidFido),
            new FakeYubiKey("device-3", ConnectionType.SmartCard)
        ]);

        // Act
        var smartCardDevices = await manager.FindAllAsync(ConnectionType.SmartCard, cancellationToken: TestContext.Current.CancellationToken);
        var hidFidoDevices = await manager.FindAllAsync(ConnectionType.HidFido, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, smartCardDevices.Count);
        Assert.Single(hidFidoDevices);

        await manager.DisposeAsync();
    }



    [Fact]
    public async Task WatchAsync_EmitsEventsFromRepository()
    {
        // Arrange
        var (manager, findYubiKeys, repository) = CreateManager();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var watcher = await DeviceEventWatcher.StartAsync(
            manager.WatchAsync,
            () => repository.WatcherCount,
            cts.Token);

        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);

        // Act
        await manager.FindAllAsync(forceRescan: true, cancellationToken: cts.Token);

        // Assert
        var events = await watcher.DrainAsync(repository, cts.Token);
        Assert.Single(events);
        Assert.Equal(DeviceAction.Added, events[0].Action);

        await manager.DisposeAsync();
    }



    /// <summary>
    /// Ending one enumeration releases that watcher and nothing else: monitoring keeps running and
    /// every other watcher keeps receiving.
    /// </summary>
    /// <remarks>
    /// Ported here from <c>YubiKeyManagerStaticTests</c>, which asserted the same thing by calling
    /// the static <c>StartMonitoring</c> for real and so started actual HID and PC/SC listeners
    /// inside a unit test. On this seam the listeners are fakes, so the assertion is about watcher
    /// independence rather than about what hardware happens to be plugged in — which also lets it
    /// assert the part the static version had to leave out: that the surviving watcher still
    /// receives events afterwards.
    /// </remarks>
    [Fact]
    public async Task WatchAsync_EndingOneWatcher_LeavesMonitoringAndTheOtherWatcherRunning()
    {
        var (manager, findYubiKeys, repository) = CreateManager();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await using var survivor = await DeviceEventWatcher.StartAsync(
            manager.WatchAsync,
            () => repository.WatcherCount,
            cts.Token);

        using var doomedCts = new CancellationTokenSource();
        var doomed = await DeviceEventWatcher.StartAsync(
            manager.WatchAsync,
            () => repository.WatcherCount,
            doomedCts.Token);

        manager.StartMonitoring(TimeSpan.FromMilliseconds(50));
        Assert.True(manager.IsMonitoring);

        await doomedCts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => doomed.Completion);
        await doomed.DisposeAsync();

        await AsyncWait.WaitUntilAsync(
            () => repository.WatcherCount == 1,
            "the ended watcher did not release its subscription",
            TimeSpan.FromSeconds(10),
            cts.Token);

        // Monitoring was not disturbed and is still delivering to the watcher that stayed.
        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);
        await survivor.WaitForCountAsync(1, "monitoring stopped delivering after a watcher ended", cts.Token);

        Assert.True(manager.IsMonitoring);
        Assert.Equal(DeviceAction.Added, survivor.Events[0].Action);
        Assert.False(survivor.Completion.IsCompleted);

        manager.StopMonitoring();
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task IsMonitoring_InitiallyFalse()
    {
        // Arrange
        var (manager, _, _) = CreateManager();

        // Assert
        Assert.False(manager.IsMonitoring);

        await manager.DisposeAsync();
    }

    [Fact]
    public async Task StartMonitoring_SetsIsMonitoringTrue()
    {
        // Arrange
        var (manager, _, _) = CreateManager();

        // Act
        manager.StartMonitoring();

        // Assert
        Assert.True(manager.IsMonitoring);

        manager.StopMonitoring();
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task StopMonitoring_SetsIsMonitoringFalse()
    {
        // Arrange
        var (manager, _, _) = CreateManager();
        manager.StartMonitoring();

        // Act
        manager.StopMonitoring();

        // Assert
        Assert.False(manager.IsMonitoring);

        await manager.DisposeAsync();
    }

    [Fact]
    public async Task StartMonitoring_CustomInterval()
    {
        // Arrange
        var (manager, _, _) = CreateManager();

        // Act
        manager.StartMonitoring(TimeSpan.FromSeconds(10));

        // Assert
        Assert.True(manager.IsMonitoring);

        manager.StopMonitoring();
        await manager.DisposeAsync();
    }



    [Fact]
    public async Task DisposeAsync_StopsMonitoring()
    {
        // Arrange
        var (manager, _, _) = CreateManager();
        manager.StartMonitoring();
        Assert.True(manager.IsMonitoring);

        // Act
        await manager.DisposeAsync();

        // Assert
        Assert.False(manager.IsMonitoring);
    }

    /// <summary>
    /// The load-bearing half of the disposal contract, pinned at the instant it applies. A publication
    /// admitted before disposal outlives the monitor's bounded drain and is resumed at the point where
    /// the manager is about to dispose the repository — after every other teardown step it performs.
    /// The repository must still be intact there. If any teardown step empties it first, this snapshot
    /// diffs an attached device against an empty cache and reports it as newly Added, which is the
    /// event the contract says can never escape.
    /// </summary>
    /// <remarks>
    /// The publication is driven by an explicit <c>RescanAsync</c> whose task the test holds, so the
    /// hook can await the resumed <c>UpdateCache</c> to completion before disposal proceeds. Releasing
    /// it without awaiting would let the repository be disposed underneath the publication and turn any
    /// emission into a silently swallowed <see cref="ObjectDisposedException"/> — the race that made the
    /// post-return variant below unable to distinguish the two orderings.
    /// </remarks>
    [Fact]
    public async Task DisposeAsync_ResumingAParkedPublicationAtRepositoryTeardown_EmitsNothing()
    {
        var repository = new YubiKeyDeviceRepository();
        var device = new FakeYubiKey("device-1", ConnectionType.SmartCard);
        var findYubiKeys = new FakeFindYubiKeys([device]);
        var monitorService = new YubiKeyDeviceMonitorService(
            repository,
            findYubiKeys,
            static () => new FakeHidDeviceListener(),
            static () => new FakeSmartCardDeviceListener(),
            shutdownTimeout: TimeSpan.FromMilliseconds(250));
        var manager = new YubiKeyDeviceManager(repository, monitorService);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Populate the cache first, then start watching, so the steady state is "device-1 present and
        // already reported" and any further event can only come from the disposal window.
        await monitorService.RescanAsync(cts.Token);
        Assert.Single(repository.GetAll());
        await using var watcher = await DeviceEventWatcher.StartAsync(repository, cts.Token);

        var admitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        monitorService.PublishAdmittedForTest = async () =>
        {
            admitted.SetResult();
            await release.Task;
        };

        // Admitted while the service is live; then parked, holding the publication gate.
        var parkedPublication = monitorService.RescanAsync(cts.Token);
        await admitted.Task.WaitAsync(TimeSpan.FromSeconds(10), cts.Token);

        manager.RepositoryTeardownReachedForTest = async () =>
        {
            release.SetResult();
            await parkedPublication.WaitAsync(TimeSpan.FromSeconds(10), cts.Token);
        };

        // The parked publication holds the publication gate, so the monitor's bounded drain times out
        // and disposal walks on to repository teardown with the publication still in flight.
        await manager.DisposeAsync();

        // Ended normally at repository disposal, having received nothing: the resumed snapshot found
        // the cache exactly as it left it and had nothing to report.
        await watcher.Completion.WaitAsync(TimeSpan.FromSeconds(10), cts.Token);
        Assert.True(watcher.EndedNormally);
        Assert.Empty(watcher.Events);
    }

    /// <summary>
    /// The companion case: a publication that outlives the monitor's bounded drain and does not resume
    /// until <see cref="YubiKeyDeviceManager.DisposeAsync"/> has already returned. By then the
    /// repository is disposed, so the late snapshot is refused outright rather than published and
    /// merely unobserved.
    /// </summary>
    /// <remarks>
    /// This pins the post-return path only. It cannot distinguish disposing the repository from
    /// emptying and then disposing it, because both orderings finish before the publication resumes;
    /// the disposal window itself is pinned by
    /// <see cref="DisposeAsync_ResumingAParkedPublicationAtRepositoryTeardown_EmitsNothing"/> above.
    /// </remarks>
    [Fact]
    public async Task DisposeAsync_WithAPublicationResumingAfterDisposeReturned_EmitsNothing()
    {
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);
        var monitorService = new YubiKeyDeviceMonitorService(
            repository,
            findYubiKeys,
            static () => new FakeHidDeviceListener(),
            static () => new FakeSmartCardDeviceListener(),
            shutdownTimeout: TimeSpan.FromMilliseconds(250));
        var manager = new YubiKeyDeviceManager(repository, monitorService);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var watcher = await DeviceEventWatcher.StartAsync(repository, cts.Token);

        var admitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resumed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var parkFirst = 1;
        monitorService.PublishAdmittedForTest = async () =>
        {
            if (Interlocked.Exchange(ref parkFirst, 0) == 1)
            {
                admitted.SetResult();
                await release.Task;
                resumed.SetResult();
            }
        };

        manager.StartMonitoring(TimeSpan.FromHours(1));
        await admitted.Task.WaitAsync(TimeSpan.FromSeconds(10), cts.Token);

        // The parked publication holds the publication gate, so the bounded drain cannot complete;
        // DisposeAsync returns on its shutdown timeout with the publication still in flight.
        await manager.DisposeAsync();

        // Resuming it runs UpdateCache on the very next statement of the publication path.
        release.SetResult();
        await resumed.Task.WaitAsync(TimeSpan.FromSeconds(10), cts.Token);

        // Ended normally at repository disposal, having received nothing: the late snapshot was
        // refused outright rather than published and merely unobserved.
        await watcher.Completion.WaitAsync(TimeSpan.FromSeconds(10), cts.Token);
        Assert.True(watcher.EndedNormally);
        Assert.Empty(watcher.Events);
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var (manager, _, _) = CreateManager();

        // Act & Assert - No exception
        await manager.DisposeAsync();
        await manager.DisposeAsync();
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task FindAllAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var (manager, _, _) = CreateManager();
        await manager.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StartMonitoring_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var (manager, _, _) = CreateManager();
        await manager.DisposeAsync();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => manager.StartMonitoring());
    }

    [Fact]
    public async Task StopMonitoring_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var (manager, _, _) = CreateManager();
        await manager.DisposeAsync();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => manager.StopMonitoring());
    }



    [Fact]
    public async Task Create_ReturnsValidInstance()
    {
        // Act
        var manager = YubiKeyDeviceManager.Create();

        // Assert
        Assert.NotNull(manager);
        Assert.False(manager.IsMonitoring);

        await manager.DisposeAsync();
    }



    [Fact]
    public async Task FindAllAsync_ConcurrentCalls_AllComplete()
    {
        // Arrange
        var (manager, findYubiKeys, _) = CreateManager();
        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);
        const int concurrency = 50;
        var tasks = new List<Task<IReadOnlyList<IYubiKey>>>();

        // Act
        for (int i = 0; i < concurrency; i++)
        {
            tasks.Add(manager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All should complete successfully
        Assert.All(results, r => Assert.Single(r));

        await manager.DisposeAsync();
    }



    private static (YubiKeyDeviceManager Manager, FakeFindYubiKeys FindYubiKeys, YubiKeyDeviceRepository Repository)
        CreateManager()
    {
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);

        // Use fake listeners: real platform listeners would surface genuine hotplug events
        // during test runs and perturb exact ScanCount assertions.
        var monitorService = new YubiKeyDeviceMonitorService(
            repository,
            findYubiKeys,
            static () => new FakeHidDeviceListener(),
            static () => new FakeSmartCardDeviceListener());
        var manager = new YubiKeyDeviceManager(repository, monitorService);

        return (manager, findYubiKeys, repository);
    }

    /// <summary>
    /// Fake IFindYubiKeys for testing with scan counting. Counters use interlocked/volatile
    /// access because scans run on the monitor loop while tests read from the test thread.
    /// </summary>
    private sealed class FakeFindYubiKeys(IReadOnlyList<IYubiKey> initialDevices) : IFindYubiKeys
    {
        private readonly Lock _syncLock = new();
        private IReadOnlyList<IYubiKey> _devices = initialDevices;
        private int _scanCount;

        public int ScanCount => Volatile.Read(ref _scanCount);

        public void SetDevices(IReadOnlyList<IYubiKey> devices)
        {
            lock (_syncLock)
            {
                _devices = devices;
            }
        }

        public Task<IReadOnlyList<IYubiKey>> FindAllAsync(
            ConnectionType type,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _scanCount);

            IReadOnlyList<IYubiKey> devices;
            lock (_syncLock)
            {
                devices = _devices;
            }

            var filtered = type == ConnectionType.All
                ? devices
                : devices.Where(d => type.Matches(d.AvailableConnections)).ToList();
            return Task.FromResult<IReadOnlyList<IYubiKey>>(filtered);
        }
    }

    private sealed class FakeHidDeviceListener : HidDeviceListener
    {
        public override void Start() => Status = DeviceListenerStatus.Started;

        public override void Stop() => Status = DeviceListenerStatus.Stopped;
    }

    private sealed class FakeSmartCardDeviceListener : ISmartCardDeviceListener
    {
        public Action? DeviceEvent { get; set; }

        public DeviceListenerStatus Status { get; private set; } = DeviceListenerStatus.Stopped;

        public void Start() => Status = DeviceListenerStatus.Started;

        public void Stop() => Status = DeviceListenerStatus.Stopped;

        public void Dispose() => DeviceEvent = null;
    }

}