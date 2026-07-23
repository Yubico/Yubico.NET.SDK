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

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
/// Tests for <see cref="YubiKeyDeviceMonitorService"/>.
/// </summary>
public class YubiKeyDeviceMonitorServiceTests
{

    [Fact]
    public async Task RescanAsync_UpdatesRepository()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([
            new FakeYubiKey("device-1", ConnectionType.SmartCard),
            new FakeYubiKey("device-2", ConnectionType.HidFido)
        ]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        // Act
        await service.RescanAsync(TestContext.Current.CancellationToken);

        // Assert
        var devices = repository.GetAll();
        Assert.Equal(2, devices.Count);
        Assert.True(repository.HasData);

        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    public async Task RescanAsync_CalledMultipleTimes_UpdatesCorrectly()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        // Act - First scan
        await service.RescanAsync(TestContext.Current.CancellationToken);
        Assert.Single(repository.GetAll());

        // Change what FindYubiKeys returns
        findYubiKeys.SetDevices([
            new FakeYubiKey("device-2", ConnectionType.HidFido),
            new FakeYubiKey("device-3", ConnectionType.HidOtp)
        ]);

        // Act - Second scan
        await service.RescanAsync(TestContext.Current.CancellationToken);

        // Assert
        var devices = repository.GetAll();
        Assert.Equal(2, devices.Count);
        Assert.Contains(devices, d => d.DeviceId == "device-2");
        Assert.Contains(devices, d => d.DeviceId == "device-3");
        Assert.DoesNotContain(devices, d => d.DeviceId == "device-1");

        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    public async Task RescanAsync_EmitsEvents()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        var events = new List<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events.Add);

        // Act
        await service.RescanAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(events);
        Assert.Equal(DeviceAction.Added, events[0].Action);
        Assert.Equal("device-1", events[0].Device.DeviceId);

        await service.DisposeAsync();
        repository.Dispose();
    }



    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task StartMonitoring_PerformsInitialRescan()
    {
        // Arrange
        var (service, repository, findYubiKeys, _, _) = CreateService();
        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.HidFido)]);

        // Act
        service.StartMonitoring(TimeSpan.FromSeconds(10));

        // Assert
        await WaitUntilAsync(() => repository.HasData, "Initial monitoring rescan did not update repository");
        Assert.Single(repository.GetAll());
        Assert.Equal(1, findYubiKeys.ScanCount);

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task HidListenerHint_DoesNotEmitPublicDeviceChangeWithoutRepositoryDiff()
    {
        // Arrange
        var (service, repository, findYubiKeys, hidListener, _) = CreateService();
        service.StartMonitoring(TimeSpan.FromSeconds(10));
        await WaitUntilAsync(() => findYubiKeys.ScanCount >= 1, "Initial monitoring rescan did not run");

        var events = new List<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events.Add);
        var scanCount = findYubiKeys.ScanCount;

        // Act
        hidListener.Raise(new HidDeviceRescanHint(
            HidDeviceChangeKind.Added,
            PlatformDeviceId: "diagnostic-only",
            DevicePath: "/dev/hidraw999"));

        // Assert
        await WaitUntilAsync(() => findYubiKeys.ScanCount > scanCount, "HID hint did not trigger rescan");
        Assert.Empty(events);
        Assert.Empty(repository.GetAll());

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task HidUnknownRemoval_TriggersRepositoryRescan()
    {
        // Arrange
        var (service, repository, findYubiKeys, hidListener, _) = CreateService();
        service.StartMonitoring(TimeSpan.FromSeconds(10));
        await WaitUntilAsync(() => findYubiKeys.ScanCount >= 1, "Initial monitoring rescan did not run");

        findYubiKeys.SetDevices([new FakeYubiKey("device-2", ConnectionType.HidOtp)]);

        // Act - unknown removals must still fall back to a repository rescan.
        hidListener.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Removed));

        // Assert
        await WaitUntilAsync(
            () => repository.GetAll().Any(device => device.DeviceId == "device-2"),
            "Unknown HID removal hint did not trigger repository rescan");

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task ConcurrentListenerEvents_DoNotRunConcurrentRescans()
    {
        // Arrange
        var (service, repository, findYubiKeys, hidListener, smartCardListener) = CreateService();
        findYubiKeys.ScanDelay = TimeSpan.FromMilliseconds(80);

        service.StartMonitoring(TimeSpan.FromSeconds(10));
        await WaitUntilAsync(
            () => findYubiKeys.ScanCount >= 1 && findYubiKeys.ActiveScans == 0,
            "Initial monitoring rescan did not complete");

        findYubiKeys.ResetCounters();

        // Act
        var start = new ManualResetEventSlim();
        var tasks = Enumerable.Range(0, 32)
            .Select(i => Task.Run(() =>
            {
                start.Wait(TestContext.Current.CancellationToken);
                if (i % 2 == 0)
                {
                    hidListener.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Added, $"hid-{i}"));
                }
                else
                {
                    smartCardListener.Raise();
                }
            }, TestContext.Current.CancellationToken))
            .ToArray();

        start.Set();
        await Task.WhenAll(tasks);

        // Assert
        await WaitUntilAsync(() => findYubiKeys.ScanCount >= 1, "Listener events did not trigger a rescan");
        Assert.Equal(1, findYubiKeys.MaxConcurrentScans);

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
        start.Dispose();
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task HintBurst_CoalescesIntoBoundedRescans()
    {
        // Arrange
        var (service, repository, findYubiKeys, hidListener, _) = CreateService();
        service.StartMonitoring(TimeSpan.FromSeconds(30));
        await WaitUntilAsync(
            () => findYubiKeys.ScanCount >= 1 && findYubiKeys.ActiveScans == 0,
            "Initial monitoring rescan did not complete");

        findYubiKeys.ResetCounters();

        // Act - a rapid burst must coalesce into few rescans, not one rescan per hint.
        for (var i = 0; i < 32; i++)
        {
            hidListener.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Added, $"hid-{i}"));
        }

        await WaitUntilAsync(() => findYubiKeys.ScanCount >= 1, "Hint burst did not trigger a rescan");

        // Allow any (incorrect) trailing per-hint rescans to surface before asserting the bound.
        await Task.Delay(YubiKeyDeviceMonitorService.MaxCoalesceInterval + YubiKeyDeviceMonitorService.ThrottleInterval,
            TestContext.Current.CancellationToken);

        // Assert - the whole burst lands within the debounce window: at most the coalesced
        // rescan plus one straggler, never anything approaching one rescan per hint.
        Assert.InRange(findYubiKeys.ScanCount, 1, 2);

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task EventStorm_DuringBlockedScan_ProducesExactlyOneFollowUpScan()
    {
        var (service, repository, findYubiKeys, hidListener, smartCardListener) = CreateService();
        findYubiKeys.HangIgnoringCancellation = true;
        service.StartMonitoring(TimeSpan.FromHours(1));
        await WaitUntilAsync(() => findYubiKeys.ActiveScans == 1, "Initial blocked scan did not start");

        for (var i = 0; i < 128; i++)
        {
            if ((i & 1) == 0)
                hidListener.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Added, $"storm-{i}"));
            else
                smartCardListener.Raise();
        }

        findYubiKeys.ReleaseHungScans();
        await WaitUntilAsync(
            () => findYubiKeys.ScanCount == 2 && findYubiKeys.ActiveScans == 0,
            "Exactly one follow-up scan did not finish");
        await Task.Delay(
            YubiKeyDeviceMonitorService.ThrottleInterval + TimeSpan.FromMilliseconds(100),
            TestContext.Current.CancellationToken);
        Assert.Equal(2, findYubiKeys.ScanCount);

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task SustainedHintStorm_RescanRunsWithinMaxCoalesceInterval()
    {
        // Arrange
        var (service, repository, findYubiKeys, hidListener, _) = CreateService();
        service.StartMonitoring(TimeSpan.FromSeconds(30));
        await WaitUntilAsync(
            () => findYubiKeys.ScanCount >= 1 && findYubiKeys.ActiveScans == 0,
            "Initial monitoring rescan did not complete");

        findYubiKeys.ResetCounters();

        // Act - hints arriving faster than the quiet period re-arm the debounce forever;
        // the max coalesce cap must force a rescan while the storm is still running.
        using var stormDone = new CancellationTokenSource();
        var storm = Task.Run(async () =>
        {
            var i = 0;
            while (!stormDone.IsCancellationRequested)
            {
                for (var burst = 0; burst < 64; burst++)
                {
                    hidListener.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Added, $"storm-{i++}"));
                }

                await Task.Yield();
            }
        }, TestContext.Current.CancellationToken);

        try
        {
            // Assert - without the cap this deadline is unreachable: the quiet period re-arms
            // on every 20 ms hint until the storm ends.
            var deadline = DateTimeOffset.UtcNow + YubiKeyDeviceMonitorService.MaxCoalesceInterval + TimeSpan.FromSeconds(2);
            while (findYubiKeys.ScanCount < 1)
            {
                Assert.True(
                    DateTimeOffset.UtcNow < deadline,
                    "Sustained hint storm starved the rescan beyond the max coalesce interval");
                await Task.Delay(TimeSpan.FromMilliseconds(20), TestContext.Current.CancellationToken);
            }
        }
        finally
        {
            stormDone.Cancel();
            await storm;
        }

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    public void IsMonitoring_InitiallyFalse()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        // Act & Assert
        Assert.False(service.IsMonitoring);

        repository.Dispose();
    }

    [Fact]
    public void StartMonitoring_SetsIsMonitoringTrue()
    {
        // Arrange
        var (service, repository, _, _, _) = CreateService();

        // Act
        service.StartMonitoring(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(service.IsMonitoring);

        // Cleanup
        service.StopMonitoring();
        repository.Dispose();
    }

    [Fact]
    public void StopMonitoring_SetsIsMonitoringFalse()
    {
        // Arrange
        var (service, repository, _, _, _) = CreateService();
        service.StartMonitoring(TimeSpan.FromSeconds(5));

        // Act
        service.StopMonitoring();

        // Assert
        Assert.False(service.IsMonitoring);

        repository.Dispose();
    }

    [Fact]
    public void StartMonitoring_Idempotent()
    {
        // Arrange
        var (service, repository, _, _, _) = CreateService();

        // Act - Call multiple times
        service.StartMonitoring(TimeSpan.FromSeconds(5));
        service.StartMonitoring(TimeSpan.FromSeconds(5));
        service.StartMonitoring(TimeSpan.FromSeconds(5));

        // Assert - Still monitoring, no exception
        Assert.True(service.IsMonitoring);

        // Cleanup
        service.StopMonitoring();
        repository.Dispose();
    }

    [Fact]
    public void StopMonitoring_Idempotent()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        // Act - Call multiple times without starting
        service.StopMonitoring();
        service.StopMonitoring();
        service.StopMonitoring();

        // Assert - No exception
        Assert.False(service.IsMonitoring);

        repository.Dispose();
    }

    [Fact]
    public async Task StartMonitoring_SmartCardFactoryThrows_RollsBackHidAndAllowsRetry()
    {
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var failedHid = new FakeHidDeviceListener();
        var retryHid = new FakeHidDeviceListener();
        var retrySmartCard = new FakeSmartCardDeviceListener();
        var hidAttempts = new Queue<FakeHidDeviceListener>([failedHid, retryHid]);
        var smartCardFactoryCalls = 0;
        var service = new YubiKeyDeviceMonitorService(
            repository,
            findYubiKeys,
            () => hidAttempts.Dequeue(),
            () => ++smartCardFactoryCalls == 1
                ? throw new InvalidOperationException("Expected SmartCard factory failure.")
                : retrySmartCard);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.StartMonitoring(TimeSpan.FromSeconds(30)));

        Assert.Equal("Expected SmartCard factory failure.", exception.Message);
        Assert.False(service.IsMonitoring);
        Assert.Null(failedHid.DeviceEvent);
        Assert.Equal(0, failedHid.StopCount);
        Assert.Equal(1, failedHid.DisposeCount);

        service.StartMonitoring(TimeSpan.FromSeconds(30));
        Assert.True(service.IsMonitoring);
        Assert.Equal(1, retryHid.StartCount);
        Assert.Equal(1, retrySmartCard.StartCount);

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    public async Task StartMonitoring_HidStartThrows_RollsBackBothAcquiredListeners()
    {
        var repository = new YubiKeyDeviceRepository();
        var hid = new FakeHidDeviceListener { ThrowOnStart = true };
        var smartCard = new FakeSmartCardDeviceListener();
        var service = new YubiKeyDeviceMonitorService(
            repository,
            new FakeFindYubiKeys([]),
            () => hid,
            () => smartCard);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.StartMonitoring(TimeSpan.FromSeconds(30)));

        Assert.Equal("Expected HID start failure.", exception.Message);
        Assert.False(service.IsMonitoring);
        Assert.Null(hid.DeviceEvent);
        Assert.Null(smartCard.DeviceEvent);
        Assert.Equal(1, hid.StopCount);
        Assert.Equal(0, smartCard.StopCount);
        Assert.Equal(1, hid.DisposeCount);
        Assert.Equal(1, smartCard.DisposeCount);

        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    public async Task StartMonitoring_HidReturnsError_RollsBackAndAllowsRetry()
    {
        var repository = new YubiKeyDeviceRepository();
        var failedHid = new FakeHidDeviceListener { StartStatus = DeviceListenerStatus.Error };
        var failedSmartCard = new FakeSmartCardDeviceListener();
        var retryHid = new FakeHidDeviceListener();
        var retrySmartCard = new FakeSmartCardDeviceListener();
        var hidAttempts = new Queue<FakeHidDeviceListener>([failedHid, retryHid]);
        var smartCardAttempts = new Queue<FakeSmartCardDeviceListener>([failedSmartCard, retrySmartCard]);
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(
            repository,
            findYubiKeys,
            () => hidAttempts.Dequeue(),
            () => smartCardAttempts.Dequeue());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.StartMonitoring(TimeSpan.FromSeconds(30)));

        Assert.Equal("HID listener failed to start (status: Error).", exception.Message);
        Assert.False(service.IsMonitoring);
        Assert.Null(failedHid.DeviceEvent);
        Assert.Null(failedSmartCard.DeviceEvent);
        Assert.Equal(1, failedHid.StopCount);
        Assert.Equal(0, failedSmartCard.StopCount);
        Assert.Equal(1, failedHid.DisposeCount);
        Assert.Equal(1, failedSmartCard.DisposeCount);

        service.StartMonitoring(TimeSpan.FromSeconds(30));
        Assert.True(service.IsMonitoring);
        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    public async Task StartMonitoring_SmartCardReturnsError_RollsBackAndAllowsRetry()
    {
        var repository = new YubiKeyDeviceRepository();
        var failedHid = new FakeHidDeviceListener();
        var failedSmartCard = new FakeSmartCardDeviceListener { StartStatus = DeviceListenerStatus.Error };
        var retryHid = new FakeHidDeviceListener();
        var retrySmartCard = new FakeSmartCardDeviceListener();
        var hidAttempts = new Queue<FakeHidDeviceListener>([failedHid, retryHid]);
        var smartCardAttempts = new Queue<FakeSmartCardDeviceListener>([failedSmartCard, retrySmartCard]);
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(
            repository,
            findYubiKeys,
            () => hidAttempts.Dequeue(),
            () => smartCardAttempts.Dequeue());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.StartMonitoring(TimeSpan.FromSeconds(30)));

        Assert.Equal("SmartCard listener failed to start (status: Error).", exception.Message);
        Assert.False(service.IsMonitoring);
        Assert.Null(failedHid.DeviceEvent);
        Assert.Null(failedSmartCard.DeviceEvent);
        Assert.Equal(1, failedHid.StopCount);
        Assert.Equal(1, failedSmartCard.StopCount);
        Assert.Equal(1, failedHid.DisposeCount);
        Assert.Equal(1, failedSmartCard.DisposeCount);

        service.StartMonitoring(TimeSpan.FromSeconds(30));
        Assert.True(service.IsMonitoring);
        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    public async Task StartMonitoring_SmartCardStartThrows_RollbackPreservesInitiatingFailureAndAllowsRetry()
    {
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var failedHid = new FakeHidDeviceListener { ThrowOnStop = true };
        var failedSmartCard = new FakeSmartCardDeviceListener { ThrowOnStart = true };
        var retryHid = new FakeHidDeviceListener();
        var retrySmartCard = new FakeSmartCardDeviceListener();
        var hidAttempts = new Queue<FakeHidDeviceListener>([failedHid, retryHid]);
        var smartCardAttempts = new Queue<FakeSmartCardDeviceListener>([failedSmartCard, retrySmartCard]);
        var service = new YubiKeyDeviceMonitorService(
            repository,
            findYubiKeys,
            () => hidAttempts.Dequeue(),
            () => smartCardAttempts.Dequeue());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.StartMonitoring(TimeSpan.FromSeconds(30)));

        Assert.Equal("Expected SmartCard start failure.", exception.Message);
        Assert.False(service.IsMonitoring);
        Assert.Null(failedHid.DeviceEvent);
        Assert.Null(failedSmartCard.DeviceEvent);
        Assert.Equal(1, failedHid.StopCount);
        Assert.Equal(1, failedSmartCard.StopCount);
        Assert.Equal(1, failedHid.DisposeCount);
        Assert.Equal(1, failedSmartCard.DisposeCount);
        var staleCallback = Assert.IsType<Action<HidDeviceRescanHint>>(failedHid.CapturedDeviceEvent);

        service.StartMonitoring(TimeSpan.FromSeconds(30));
        Assert.True(service.IsMonitoring);
        await WaitUntilAsync(
            () => findYubiKeys.ScanCount == 1 && findYubiKeys.ActiveScans == 0,
            "Retry initial scan did not finish");
        var scanCountBeforeStaleCallback = findYubiKeys.ScanCount;
        staleCallback(new HidDeviceRescanHint(HidDeviceChangeKind.Added, "stale-attempt"));
        await Task.Delay(
            YubiKeyDeviceMonitorService.ThrottleInterval + TimeSpan.FromMilliseconds(100),
            TestContext.Current.CancellationToken);
        Assert.Equal(scanCountBeforeStaleCallback, findYubiKeys.ScanCount);

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    public void StartMonitoring_ZeroInterval_Throws()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => service.StartMonitoring(TimeSpan.Zero));

        repository.Dispose();
    }

    [Fact]
    public void StartMonitoring_NegativeInterval_Throws()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => service.StartMonitoring(TimeSpan.FromSeconds(-1)));

        repository.Dispose();
    }



    [Fact]
    public async Task DisposeAsync_StopsMonitoring()
    {
        // Arrange
        var (service, repository, _, _, _) = CreateService();
        service.StartMonitoring(TimeSpan.FromSeconds(5));

        // Act
        await service.DisposeAsync();

        // Assert
        Assert.False(service.IsMonitoring);

        repository.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        // Act & Assert - No exception
        await service.DisposeAsync();
        await service.DisposeAsync();
        await service.DisposeAsync();

        repository.Dispose();
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task DisposeAsync_RescanHungIgnoringCancellation_CompletesWithinBoundedTime()
    {
        // Arrange - discovery scan hangs and ignores cancellation, holding the rescan gate
        var (service, repository, findYubiKeys, _, _) = CreateService(shutdownTimeout: TimeSpan.FromMilliseconds(250));
        findYubiKeys.HangIgnoringCancellation = true;

        service.StartMonitoring(TimeSpan.FromHours(1));
        await WaitUntilAsync(() => findYubiKeys.ActiveScans == 1, "Initial rescan never started");

        // Act - disposal must abandon the stuck loop and rescan gate instead of hanging
        await service.DisposeAsync().AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Cleanup - unblock the stuck scan; the abandoned (undisposed) gate accepts its Release
        findYubiKeys.ReleaseHungScans();
        await WaitUntilAsync(() => findYubiKeys.ActiveScans == 0, "Hung rescan never completed after release");

        repository.Dispose();
    }

    [Fact]
    public async Task RescanAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);
        await service.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => service.RescanAsync(TestContext.Current.CancellationToken));

        repository.Dispose();
    }

    [Fact]
    public async Task StartMonitoring_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys);
        await service.DisposeAsync();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => service.StartMonitoring(TimeSpan.FromSeconds(5)));

        repository.Dispose();
    }



    [Fact]
    public void Constructor_NullRepository_Throws()
    {
        // Arrange
        var findYubiKeys = new FakeFindYubiKeys([]);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new YubiKeyDeviceMonitorService(null!, findYubiKeys));
    }

    [Fact]
    public void Constructor_NullFindYubiKeys_Throws()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new YubiKeyDeviceMonitorService(repository, null!));

        repository.Dispose();
    }



    /// <summary>
    /// Fake IFindYubiKeys for testing.
    /// </summary>
    private static (
        YubiKeyDeviceMonitorService Service,
        YubiKeyDeviceRepository Repository,
        FakeFindYubiKeys FindYubiKeys,
        FakeHidDeviceListener HidListener,
        FakeSmartCardDeviceListener SmartCardListener) CreateService(TimeSpan? shutdownTimeout = null)
    {
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var hidListener = new FakeHidDeviceListener();
        var smartCardListener = new FakeSmartCardDeviceListener();
        var service = new YubiKeyDeviceMonitorService(
            repository,
            findYubiKeys,
            () => hidListener,
            () => smartCardListener,
            shutdownTimeout);

        return (service, repository, findYubiKeys, hidListener, smartCardListener);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string failureMessage)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException(failureMessage);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20), TestContext.Current.CancellationToken);
        }
    }

    private sealed class FakeHidDeviceListener : HidDeviceListener
    {
        public int StartCount { get; private set; }

        public int StopCount { get; private set; }

        public int DisposeCount { get; private set; }

        public bool ThrowOnStart { get; init; }

        public bool ThrowOnStop { get; init; }

        public DeviceListenerStatus StartStatus { get; init; } = DeviceListenerStatus.Started;

        public Action<HidDeviceRescanHint>? CapturedDeviceEvent { get; private set; }

        public override void Start()
        {
            StartCount++;
            CapturedDeviceEvent = DeviceEvent;
            if (ThrowOnStart)
                throw new InvalidOperationException("Expected HID start failure.");

            Status = StartStatus;
        }

        public override void Stop()
        {
            StopCount++;
            if (ThrowOnStop)
                throw new InvalidOperationException("Expected HID stop failure.");

            Status = DeviceListenerStatus.Stopped;
        }

        public void Raise(HidDeviceRescanHint hint) => OnDeviceEvent(hint);

        protected override void Dispose(bool disposing)
        {
            DisposeCount++;
            base.Dispose(disposing);
        }
    }

    private sealed class FakeSmartCardDeviceListener : ISmartCardDeviceListener
    {
        public Action? DeviceEvent { get; set; }

        public DeviceListenerStatus Status { get; private set; } = DeviceListenerStatus.Stopped;

        public int StartCount { get; private set; }

        public int StopCount { get; private set; }

        public int DisposeCount { get; private set; }

        public bool ThrowOnStart { get; init; }

        public DeviceListenerStatus StartStatus { get; init; } = DeviceListenerStatus.Started;

        public void Start()
        {
            StartCount++;
            if (ThrowOnStart)
                throw new InvalidOperationException("Expected SmartCard start failure.");

            Status = StartStatus;
        }

        public void Stop()
        {
            StopCount++;
            Status = DeviceListenerStatus.Stopped;
        }

        public void Raise() => DeviceEvent?.Invoke();

        public void Dispose()
        {
            DisposeCount++;
            DeviceEvent = null;
        }
    }

    private sealed class FakeFindYubiKeys(IReadOnlyList<IYubiKey> initialDevices) : IFindYubiKeys
    {
        private readonly Lock _syncLock = new();
        private readonly TaskCompletionSource _hangReleased = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private IReadOnlyList<IYubiKey> _devices = initialDevices;
        private int _activeScans;
        private int _maxConcurrentScans;
        private int _scanCount;

        public TimeSpan ScanDelay { get; set; }

        /// <summary>
        /// When set, scans block until <see cref="ReleaseHungScans"/> is called,
        /// ignoring the caller's cancellation token. Models a discovery backend
        /// stuck in native I/O.
        /// </summary>
        public bool HangIgnoringCancellation { get; set; }

        public int ScanCount => Volatile.Read(ref _scanCount);

        public int ActiveScans => Volatile.Read(ref _activeScans);

        public int MaxConcurrentScans => Volatile.Read(ref _maxConcurrentScans);

        public void SetDevices(IReadOnlyList<IYubiKey> devices)
        {
            lock (_syncLock)
            {
                _devices = devices;
            }
        }

        public void ReleaseHungScans() => _hangReleased.TrySetResult();

        public void ResetCounters()
        {
            Volatile.Write(ref _scanCount, 0);
            Volatile.Write(ref _activeScans, 0);
            Volatile.Write(ref _maxConcurrentScans, 0);
        }

        public async Task<IReadOnlyList<IYubiKey>> FindAllAsync(
            ConnectionType type,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _scanCount);
            var activeScans = Interlocked.Increment(ref _activeScans);
            UpdateMaxConcurrentScans(activeScans);

            try
            {
                if (HangIgnoringCancellation)
                {
                    await _hangReleased.Task.ConfigureAwait(false);
                }

                if (ScanDelay > TimeSpan.Zero)
                {
                    await Task.Delay(ScanDelay, cancellationToken).ConfigureAwait(false);
                }

                IReadOnlyList<IYubiKey> devices;
                lock (_syncLock)
                {
                    devices = _devices;
                }

                return type == ConnectionType.All
                    ? devices
                    : devices.Where(d => type.Matches(d.AvailableConnections)).ToList();
            }
            finally
            {
                Interlocked.Decrement(ref _activeScans);
            }
        }

        private void UpdateMaxConcurrentScans(int activeScans)
        {
            while (true)
            {
                var current = Volatile.Read(ref _maxConcurrentScans);
                if (activeScans <= current)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref _maxConcurrentScans, activeScans, current) == current)
                {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Minimal fake IYubiKey implementation for testing.
    /// </summary>
    private sealed class FakeYubiKey(string deviceId, ConnectionType connectionType) : IYubiKey
    {
        public string DeviceId { get; } = deviceId;
        public ConnectionType AvailableConnections { get; } = connectionType;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
            => throw new NotSupportedException("FakeYubiKey does not support connections.");
    }

}