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

using System.Reflection;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.Infrastructure;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
/// Tests for <see cref="YubiKeyDeviceMonitorService"/>.
/// </summary>
public class YubiKeyDeviceMonitorServiceTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Parks the service's first admitted publication on the publication path and returns a task that
    /// signals when it is parked, plus the release callback.
    /// </summary>
    /// <remarks>
    /// Device events are delivered asynchronously into per-watcher buffers, so a consumer can no
    /// longer hold a publication in flight the way a blocking observer used to. Lifecycle tests that
    /// need an in-flight publication use this seam instead.
    /// </remarks>
    private static (Task Parked, Action Release) ParkFirstPublication(YubiKeyDeviceMonitorService service)
    {
        var parked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var parkFirst = 1;

        service.PublishAdmittedForTest = () =>
        {
            if (Interlocked.Exchange(ref parkFirst, 0) != 1)
            {
                return Task.CompletedTask;
            }

            parked.SetResult();
            return release.Task;
        };

        return (parked.Task, () => release.TrySetResult());
    }

    [Fact]
    public async Task CreateAsync_PublishesInitialRepositoryMembership()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([
            new FakeYubiKey("device-1", ConnectionType.SmartCard),
            new FakeYubiKey("device-2", ConnectionType.HidFido)
        ]);
        var service = new YubiKeyDeviceMonitorService(repository, findYubiKeys,
            () => new FakeHidDeviceListener(), () => new FakeSmartCardDeviceListener());

        // Act
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));

        // Assert
        var devices = repository.GetAll();
        Assert.Equal(2, devices.Count);
        Assert.True(repository.HasData);

    }

    [Fact]
    public async Task ListenerScan_ChangedDiscovery_UpdatesRepositoryMembership()
    {
        var (service, repository, findYubiKeys, _, smartCard) = CreateService();
        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        Assert.Single(repository.GetAll());

        // Change what FindYubiKeys returns
        findYubiKeys.SetDevices([
            new FakeYubiKey("device-2", ConnectionType.HidFido),
            new FakeYubiKey("device-3", ConnectionType.HidOtp)
        ]);

        smartCard.Raise();
        await findYubiKeys.WaitForCompletedScanAsync(2, TestContext.Current.CancellationToken);

        // Assert
        var devices = repository.GetAll();
        Assert.Equal(2, devices.Count);
        Assert.Contains(devices, d => d.DeviceId == "device-2");
        Assert.Contains(devices, d => d.DeviceId == "device-3");
        Assert.DoesNotContain(devices, d => d.DeviceId == "device-1");

    }

    [Fact]
    public async Task ListenerScan_EmitsRepositoryDiffEvents()
    {
        var (service, repository, findYubiKeys, _, smartCard) = CreateService();
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        await using var watcher = await DeviceEventWatcher.StartAsync(repository, TestContext.Current.CancellationToken);
        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);
        smartCard.Raise();
        await findYubiKeys.WaitForCompletedScanAsync(2, TestContext.Current.CancellationToken);
        await monitor.DisposeAsync();
        await watcher.Completion.WaitAsync(Bound, TestContext.Current.CancellationToken);
        var events = watcher.Events;
        Assert.Single(events);
        Assert.Equal(DeviceAction.Added, events[0].Action);
        Assert.Equal("device-1", events[0].Device.DeviceId);

    }



    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task CreateAsync_PerformsOneInitialLoopScan()
    {
        // Arrange
        var (service, repository, findYubiKeys, _, _) = CreateService();
        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.HidFido)]);

        // Act
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));

        // Assert
        Assert.True(repository.HasData);
        Assert.Single(repository.GetAll());
        Assert.Equal(1, findYubiKeys.ScanCount);

    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task HidListenerHint_DoesNotEmitPublicDeviceChangeWithoutRepositoryDiff()
    {
        // Arrange
        var (service, repository, findYubiKeys, hidListener, _) = CreateService();
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        await using var watcher = await DeviceEventWatcher.StartAsync(repository, TestContext.Current.CancellationToken);

        // Act
        hidListener.Raise(new HidDeviceRescanHint(
            HidDeviceChangeKind.Added,
            PlatformDeviceId: "diagnostic-only",
            DevicePath: "/dev/hidraw999"));

        // Assert
        await findYubiKeys.WaitForCompletedScanAsync(2, TestContext.Current.CancellationToken);
        Assert.Empty(repository.GetAll());

        await monitor.DisposeAsync();
        await watcher.Completion.WaitAsync(Bound, TestContext.Current.CancellationToken);
        Assert.Empty(watcher.Events);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task HidUnknownRemoval_TriggersRepositoryRescan()
    {
        // Arrange
        var (service, repository, findYubiKeys, hidListener, _) = CreateService();
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));

        findYubiKeys.SetDevices([new FakeYubiKey("device-2", ConnectionType.HidOtp)]);

        // Act - unknown removals must still fall back to a repository rescan.
        hidListener.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Removed));

        // Assert
        await findYubiKeys.WaitForCompletedScanAsync(2, TestContext.Current.CancellationToken);
        Assert.Equal("device-2", Assert.Single(monitor.CurrentDevices).DeviceId);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task ConcurrentListenerEvents_DoNotRunConcurrentRescans()
    {
        // Arrange
        var (service, repository, findYubiKeys, hidListener, smartCardListener) = CreateService();
        findYubiKeys.ScanDelay = TimeSpan.FromMilliseconds(80);

        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        await findYubiKeys.WaitForCompletedScanAsync(1, TestContext.Current.CancellationToken);

        // Act
        using var start = new ManualResetEventSlim();
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
        await findYubiKeys.WaitForCompletedScanAsync(2, TestContext.Current.CancellationToken);
        await monitor.DisposeAsync();
        Assert.Equal(1, findYubiKeys.MaxConcurrentScans);

    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task HintBurst_CoalescesIntoBoundedRescans()
    {
        // Arrange
        var (service, repository, findYubiKeys, hidListener, _) = CreateService();
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        await findYubiKeys.WaitForCompletedScanAsync(1, TestContext.Current.CancellationToken);

        // Act - a rapid burst must coalesce into few rescans, not one rescan per hint.
        for (var i = 0; i < 32; i++)
        {
            hidListener.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Added, $"hid-{i}"));
        }

        await findYubiKeys.WaitForCompletedScanAsync(2, TestContext.Current.CancellationToken);

        // Allow any (incorrect) trailing per-hint rescans to surface before asserting the bound.
        await Task.Delay(YubiKeyDeviceMonitorService.MaxCoalesceInterval + YubiKeyDeviceMonitorService.ThrottleInterval,
            TestContext.Current.CancellationToken);

        // Assert - the whole burst lands within the debounce window: at most the coalesced
        // rescan plus one straggler, never anything approaching one rescan per hint.
        await monitor.DisposeAsync();
        Assert.InRange(findYubiKeys.ScanCount, 2, 3);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task EventStorm_DuringBlockedScan_ProducesExactlyOneFollowUpScan()
    {
        var (service, repository, findYubiKeys, hidListener, smartCardListener) = CreateService();
        findYubiKeys.HangIgnoringCancellation = true;
        var creation = YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        var loop = service.MonitoringCompletionForTest;
        try
        {
            await AsyncWait.WaitUntilAsync(() => findYubiKeys.ActiveScans == 1, "Initial blocked scan did not start",
                cancellationToken: TestContext.Current.CancellationToken);
            Assert.False(creation.IsCompleted);
            for (var i = 0; i < 128; i++)
            {
                if ((i & 1) == 0)
                    hidListener.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Added, $"storm-{i}"));
                else
                    smartCardListener.Raise();
            }
            findYubiKeys.ReleaseHungScans();
            var monitor = await creation.WaitAsync(Bound, TestContext.Current.CancellationToken);
            await findYubiKeys.WaitForCompletedScanAsync(2, TestContext.Current.CancellationToken);
            await Task.Delay(YubiKeyDeviceMonitorService.ThrottleInterval + TimeSpan.FromMilliseconds(100),
                TestContext.Current.CancellationToken);
            await monitor.DisposeAsync();
            Assert.Equal(2, findYubiKeys.ScanCount);
            Assert.Equal(1, findYubiKeys.MaxConcurrentScans);
        }
        finally
        {
            findYubiKeys.ReleaseHungScans();
            // Creation and its loop own these fakes, including if the test runner has canceled.
            var monitor = await creation.WaitAsync(Bound, CancellationToken.None);
            await monitor.DisposeAsync();
            await loop.WaitAsync(Bound, CancellationToken.None);
        }
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task SustainedHintStorm_RescanRunsWithinMaxCoalesceInterval()
    {
        // Arrange
        var (service, repository, findYubiKeys, hidListener, _) = CreateService();
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        await findYubiKeys.WaitForCompletedScanAsync(1, TestContext.Current.CancellationToken);

        // Act - hints arriving faster than the quiet period re-arm the debounce forever;
        // the max coalesce cap must force a rescan while the storm is still running.
        using var stormDone = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
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
            while (findYubiKeys.CompletedScans < 2)
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
            // The producer still owns the listener after runner cancellation.
            await storm.WaitAsync(Bound, CancellationToken.None);
        }
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
    public async Task CreateAsync_SmartCardFactoryThrows_StartsWithHidOnly()
    {
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var hid = new FakeHidDeviceListener();
        var service = new YubiKeyDeviceMonitorService(
            repository,
            findYubiKeys,
            () => hid,
            () => throw new InvalidOperationException("Expected SmartCard factory failure."));

        // The SmartCard listener is unavailable, but HID starts, so monitoring
        // runs (degraded). Startup must not throw for an unavailable transport.
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        Assert.True(repository.HasData);
        Assert.Equal(1, hid.StartCount);
        Assert.NotNull(hid.DeviceEvent);
        Assert.Equal(0, hid.StopCount);
        Assert.Equal(0, hid.DisposeCount);

        await AssertListenerProgressAsync(monitor, findYubiKeys,
            () => hid.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Added)));
    }

    [Fact]
    public async Task CreateAsync_HidStartThrows_StartsWithSmartCardOnly()
    {
        var repository = new YubiKeyDeviceRepository();
        var hid = new FakeHidDeviceListener { ThrowOnStart = true };
        var smartCard = new FakeSmartCardDeviceListener();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(
            repository,
            findYubiKeys,
            () => hid,
            () => smartCard);

        // HID cannot start, but SmartCard starts, so monitoring runs (degraded).
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        Assert.True(repository.HasData);
        // The failed HID listener is cleaned up individually.
        Assert.Null(hid.DeviceEvent);
        Assert.Equal(1, hid.StopCount);
        Assert.Equal(1, hid.DisposeCount);
        // SmartCard remains the active listener and is not torn down.
        Assert.NotNull(smartCard.DeviceEvent);
        Assert.Equal(1, smartCard.StartCount);
        Assert.Equal(0, smartCard.StopCount);
        Assert.Equal(0, smartCard.DisposeCount);

        await AssertListenerProgressAsync(monitor, findYubiKeys, smartCard.Raise);
    }

    [Fact]
    public async Task CreateAsync_HidReturnsError_StartsWithSmartCardOnly()
    {
        var repository = new YubiKeyDeviceRepository();
        var hid = new FakeHidDeviceListener { StartStatus = DeviceListenerStatus.Error };
        var smartCard = new FakeSmartCardDeviceListener();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(
            repository,
            findYubiKeys,
            () => hid,
            () => smartCard);

        // HID reports Error (does not throw); it is cleaned up while SmartCard
        // keeps monitoring alive.
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        Assert.True(repository.HasData);
        Assert.Null(hid.DeviceEvent);
        Assert.Equal(1, hid.StartCount);
        Assert.Equal(1, hid.StopCount);
        Assert.Equal(1, hid.DisposeCount);
        Assert.NotNull(smartCard.DeviceEvent);
        Assert.Equal(1, smartCard.StartCount);
        Assert.Equal(0, smartCard.StopCount);
        Assert.Equal(0, smartCard.DisposeCount);

        await AssertListenerProgressAsync(monitor, findYubiKeys, smartCard.Raise);
    }

    [Fact]
    public async Task CreateAsync_SmartCardReturnsError_StartsWithHidOnly()
    {
        var repository = new YubiKeyDeviceRepository();
        var hid = new FakeHidDeviceListener();
        var smartCard = new FakeSmartCardDeviceListener { StartStatus = DeviceListenerStatus.Error };
        var findYubiKeys = new FakeFindYubiKeys([]);
        var service = new YubiKeyDeviceMonitorService(
            repository,
            findYubiKeys,
            () => hid,
            () => smartCard);

        // SmartCard reports Error (the common no-PC/SC case); it is cleaned up
        // while HID keeps monitoring alive.
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        Assert.True(repository.HasData);
        Assert.NotNull(hid.DeviceEvent);
        Assert.Equal(1, hid.StartCount);
        Assert.Equal(0, hid.StopCount);
        Assert.Equal(0, hid.DisposeCount);
        Assert.Null(smartCard.DeviceEvent);
        Assert.Equal(1, smartCard.StartCount);
        Assert.Equal(1, smartCard.StopCount);
        Assert.Equal(1, smartCard.DisposeCount);

        await AssertListenerProgressAsync(monitor, findYubiKeys,
            () => hid.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Added)));
    }

    [Fact]
    public async Task CreateAsync_SmartCardStartThrows_StartsWithHidAndDetachesFailedListener()
    {
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var hid = new FakeHidDeviceListener();
        var failedSmartCard = new FakeSmartCardDeviceListener { ThrowOnStart = true };
        var service = new YubiKeyDeviceMonitorService(
            repository,
            findYubiKeys,
            () => hid,
            () => failedSmartCard);

        // SmartCard Start throws; HID starts, so monitoring runs (degraded).
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        Assert.True(repository.HasData);
        Assert.NotNull(hid.DeviceEvent);
        Assert.Equal(1, hid.StartCount);
        // The failed SmartCard listener is cleaned up and detached from its signal.
        Assert.Null(failedSmartCard.DeviceEvent);
        Assert.Equal(1, failedSmartCard.StopCount);
        Assert.Equal(1, failedSmartCard.DisposeCount);

        var scanCountBeforeStaleCallback = findYubiKeys.ScanCount;

        // A callback from the detached, failed listener can no longer trigger a rescan.
        failedSmartCard.Raise();
        await Task.Delay(
            YubiKeyDeviceMonitorService.ThrottleInterval + TimeSpan.FromMilliseconds(100),
            TestContext.Current.CancellationToken);
        Assert.Equal(scanCountBeforeStaleCallback, findYubiKeys.ScanCount);

        await AssertListenerProgressAsync(monitor, findYubiKeys,
            () => hid.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Added)));
    }

    [Fact]
    public async Task CreateAsync_BothListenersFail_StartsIntervalOnlyMonitoring()
    {
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var hid = new FakeHidDeviceListener { StartStatus = DeviceListenerStatus.Error };
        var smartCard = new FakeSmartCardDeviceListener { StartStatus = DeviceListenerStatus.Error };
        var service = new YubiKeyDeviceMonitorService(
            repository,
            findYubiKeys,
            () => hid,
            () => smartCard);

        // Neither listener can start, but monitoring still runs on the interval
        // rescan alone. Startup must not throw.
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromMilliseconds(200));
        Assert.True(repository.HasData);
        Assert.Null(hid.DeviceEvent);
        Assert.Null(smartCard.DeviceEvent);
        Assert.Equal(1, hid.DisposeCount);
        Assert.Equal(1, smartCard.DisposeCount);

        // With no listeners to signal, only the 200ms interval fallback can drive
        // rescans. Require at least two scans so we prove the interval loop keeps
        // running on its own, not merely the one-shot startup rescan.
        await AssertListenerProgressAsync(monitor, findYubiKeys, () => { });
        Assert.True(findYubiKeys.ScanCount >= 2);
    }

    private static async Task AssertListenerProgressAsync(YubiKeyMonitor monitor, FakeFindYubiKeys finder, Action hint)
    {
        var device = new FakeYubiKey("after-listener-failure");
        finder.SetDevices([device]);
        hint();
        await AsyncWait.WaitUntilAsync(() => monitor.CurrentDevices.Count == 1 && ReferenceEquals(monitor.CurrentDevices[0], device),
            "The surviving listener/interval did not publish changed discovery", Bound, TestContext.Current.CancellationToken);
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
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        var loop = service.MonitoringCompletionForTest;

        // Act
        await monitor.DisposeAsync();

        // Assert
        await loop.WaitAsync(Bound, TestContext.Current.CancellationToken);
        Assert.Throws<ObjectDisposedException>(() => monitor.CurrentDevices);
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
        var (service, repository, findYubiKeys, _, smartCard) = CreateService(shutdownTimeout: TimeSpan.FromMilliseconds(250));
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        var loop = service.MonitoringCompletionForTest;
        findYubiKeys.HangIgnoringCancellation = true;
        smartCard.Raise();
        try
        {
            await AsyncWait.WaitUntilAsync(() => findYubiKeys.ActiveScans == 1, "Listener scan never started",
                cancellationToken: TestContext.Current.CancellationToken);
            await monitor.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.False(loop.IsCompleted);
        }
        finally
        {
            findYubiKeys.ReleaseHungScans();
            await monitor.DisposeAsync();
            // Finder return is not enough: drain the loop's subsequent publication/cancellation path.
            await loop.WaitAsync(Bound, CancellationToken.None);
        }
        Assert.Equal(0, findYubiKeys.ActiveScans);
        Assert.False(repository.HasData);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task RescanAsync_SupersededByLifecycleSwap_DiscardsStaleSnapshot()
    {
        // Arrange - a manual rescan hangs in discovery, capturing the pre-start
        // monitor generation.
        var (service, repository, findYubiKeys, _, _) = CreateService(shutdownTimeout: TimeSpan.FromMilliseconds(250));
        using var cts = new CancellationTokenSource(Bound);
        findYubiKeys.HangIgnoringCancellation = true;

        var staleRescan = service.RescanAsync(TestContext.Current.CancellationToken);
        Task loop = Task.CompletedTask;
        try
        {
            await AsyncWait.WaitUntilAsync(() => findYubiKeys.ActiveScans >= 1, "Manual rescan never started");

            // Act - start/stop swaps the monitor generation; the manual rescan's
            // captured generation is now superseded.
            service.StartMonitoring(TimeSpan.FromHours(1));
            loop = service.MonitoringCompletionForTest;
            service.StopMonitoring();

            await using var watcher = await DeviceEventWatcher.StartAsync(repository, cts.Token);

            findYubiKeys.SetDevices([new FakeYubiKey("stale-device", ConnectionType.SmartCard)]);
            findYubiKeys.ReleaseHungScans();

            // The superseded rescan completes silently without publishing.
            await staleRescan;
            await loop.WaitAsync(Bound, TestContext.Current.CancellationToken);

            // Assert - the stale snapshot was discarded, not published as device truth.
            Assert.Empty(repository.GetAll());
            Assert.Empty(await watcher.DrainAsync(repository, cts.Token));

            await service.DisposeAsync();
            repository.Dispose();
        }
        finally
        {
            findYubiKeys.ReleaseHungScans();
            await service.DisposeAsync();
            // Both the manual producer and the retired real loop still own their fake finder.
            await Task.WhenAll(staleRescan, loop).WaitAsync(Bound, CancellationToken.None);
            repository.Dispose();
        }
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task StopMonitoring_TimesOutOnHungScan_RestartPublishesWithNewGeneration()
    {
        // Arrange - the initial scan hangs ignoring cancellation, so StopMonitoring
        // times out and abandons the generation.
        var (service, repository, findYubiKeys, _, _) = CreateService(shutdownTimeout: TimeSpan.FromMilliseconds(250));
        findYubiKeys.HangIgnoringCancellation = true;
        service.StartMonitoring(TimeSpan.FromHours(1));
        var oldLoop = service.MonitoringCompletionForTest;
        Task nextLoop = Task.CompletedTask;
        try
        {
            await AsyncWait.WaitUntilAsync(() => findYubiKeys.ActiveScans == 1, "Initial scan never started");

            service.StopMonitoring();
            Assert.False(service.IsMonitoring);

            // Act - restart must succeed immediately with a fresh generation even though
            // the abandoned scan still holds its dead generation's scan gate.
            findYubiKeys.HangIgnoringCancellation = false;
            findYubiKeys.SetDevices([new FakeYubiKey("device-b", ConnectionType.SmartCard)]);
            service.StartMonitoring(TimeSpan.FromHours(1));
            nextLoop = service.MonitoringCompletionForTest;
            Assert.True(service.IsMonitoring);

            await AsyncWait.WaitUntilAsync(
                () => repository.GetAll().Any(d => d.DeviceId == "device-b"),
                "Restarted monitoring did not publish with a new generation");

            // The hung scan from the abandoned generation later returns - its snapshot
            // must fail admission and emit no device events.
            using var cts = new CancellationTokenSource(Bound);
            await using var watcher = await DeviceEventWatcher.StartAsync(repository, cts.Token);
            findYubiKeys.SetDevices([new FakeYubiKey("device-c", ConnectionType.SmartCard)]);
            findYubiKeys.ReleaseHungScans();
            await oldLoop.WaitAsync(Bound, TestContext.Current.CancellationToken);

            Assert.DoesNotContain(repository.GetAll(), d => d.DeviceId == "device-c");
            Assert.Contains(repository.GetAll(), d => d.DeviceId == "device-b");

            service.StopMonitoring();
            Assert.Empty(await watcher.DrainAsync(repository, cts.Token));

            await service.DisposeAsync();
            repository.Dispose();
        }
        finally
        {
            findYubiKeys.ReleaseHungScans();
            await service.DisposeAsync();
            // Drain both actual loops before releasing the fake repository/finder lifetime.
            await Task.WhenAll(oldLoop, nextLoop).WaitAsync(Bound, CancellationToken.None);
            repository.Dispose();
        }
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task CrossGenerationPublications_SerializeAndSuccessorSnapshotLandsLast()
    {
        // Arrange - the first publication is parked after admission, holding the publication gate
        // across a generation swap. (Device events are delivered asynchronously now, so a consumer
        // can no longer hold a publication in flight; the seam is what replaces it.)
        var (service, repository, findYubiKeys, _, _) = CreateService(shutdownTimeout: TimeSpan.FromMilliseconds(250));
        using var cts = new CancellationTokenSource(Bound);
        findYubiKeys.SetDevices([new FakeYubiKey("device-a", ConnectionType.SmartCard)]);

        await using var watcher = await DeviceEventWatcher.StartAsync(repository, cts.Token);

        var admitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var parkFirst = 1;
        var activePublications = 0;
        var maxConcurrentPublications = 0;
        service.PublishAdmittedForTest = async () =>
        {
            var active = Interlocked.Increment(ref activePublications);
            InterlockedMax(ref maxConcurrentPublications, active);

            try
            {
                if (Interlocked.Exchange(ref parkFirst, 0) == 1)
                {
                    admitted.SetResult();
                    await release.Task;
                }
            }
            finally
            {
                _ = Interlocked.Decrement(ref activePublications);
            }
        };

        service.StartMonitoring(TimeSpan.FromHours(1));
        var oldLoop = service.MonitoringCompletionForTest;
        Task nextLoop = Task.CompletedTask;
        try
        {
            await admitted.Task.WaitAsync(TimeSpan.FromSeconds(5), cts.Token);

            // Act - restart while the old-generation publication holds the gate.
            service.StopMonitoring();
            findYubiKeys.SetDevices([new FakeYubiKey("device-b", ConnectionType.SmartCard)]);
            service.StartMonitoring(TimeSpan.FromHours(1));
            nextLoop = service.MonitoringCompletionForTest;

            // The successor generation scans immediately (its scan is not serialized
            // behind the dead generation) but must not enter UpdateCache while the
            // old publication holds the publication gate.
            await AsyncWait.WaitUntilAsync(() => findYubiKeys.ScanCount >= 2, "Successor generation never scanned");
            await Task.Delay(100, cts.Token);
            Assert.Empty(repository.GetAll());

            // Release the old publication: it publishes first, then the successor's
            // snapshot lands last. Publications never interleave.
            release.SetResult();
            await AsyncWait.WaitUntilAsync(
                () => repository.GetAll().Any(d => d.DeviceId == "device-b"),
                "Successor snapshot was never published");

            Assert.DoesNotContain(repository.GetAll(), d => d.DeviceId == "device-a");

            await watcher.WaitForCountAsync(3, "the full cross-generation sequence never arrived", cts.Token);
            var events = watcher.Events;
            Assert.Equal(1, Volatile.Read(ref maxConcurrentPublications));
            Assert.Equal(3, events.Count);
            Assert.Equal((DeviceAction.Added, "device-a"), (events[0].Action, events[0].Device.DeviceId));
            Assert.Equal((DeviceAction.Removed, "device-a"), (events[1].Action, events[1].Device.DeviceId));
            Assert.Equal((DeviceAction.Added, "device-b"), (events[2].Action, events[2].Device.DeviceId));

            service.StopMonitoring();
            await service.DisposeAsync();
            repository.Dispose();
        }
        finally
        {
            release.TrySetResult();
            await service.DisposeAsync();
            // A released publication seam is not itself proof that either producer has exited.
            await Task.WhenAll(oldLoop, nextLoop).WaitAsync(Bound, CancellationToken.None);
            repository.Dispose();
        }
    }

    private static void InterlockedMax(ref int target, int candidate)
    {
        while (true)
        {
            var current = Volatile.Read(ref target);
            if (candidate <= current || Interlocked.CompareExchange(ref target, candidate, current) == current)
            {
                return;
            }
        }
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task PublicationHeldBetweenGateAndAdmission_SwappedGeneration_IsDiscarded()
    {
        // Arrange - the internal test seam parks a publication after it acquires
        // the publication gate but before its admission check.
        var (service, repository, findYubiKeys, _, _) = CreateService();
        findYubiKeys.SetDevices([new FakeYubiKey("stale-device", ConnectionType.SmartCard)]);

        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstPublication = 1;
        service.PublishGateAcquiredForTest = () =>
        {
            if (Interlocked.Exchange(ref firstPublication, 0) == 1)
            {
                held.SetResult();
                return release.Task;
            }

            return Task.CompletedTask;
        };

        using var cts = new CancellationTokenSource(Bound);
        await using var watcher = await DeviceEventWatcher.StartAsync(repository, cts.Token);

        var staleRescan = service.RescanAsync(TestContext.Current.CancellationToken);
        Task loop = Task.CompletedTask;
        try
        {
            await held.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            // Act - swap generations while the publication is parked between gate
            // acquisition and admission. StartMonitoring takes only the state lock,
            // so it must not block on the held publication gate.
            findYubiKeys.SetDevices([new FakeYubiKey("device-b", ConnectionType.SmartCard)]);
            service.StartMonitoring(TimeSpan.FromHours(1));
            loop = service.MonitoringCompletionForTest;
            Assert.True(service.IsMonitoring);

            release.SetResult();
            await staleRescan;

            // Assert - admission failed for the parked snapshot; it was discarded and
            // the successor generation published.
            await AsyncWait.WaitUntilAsync(
                () => repository.GetAll().Any(d => d.DeviceId == "device-b"),
                "Successor generation did not publish");
            Assert.DoesNotContain(repository.GetAll(), d => d.DeviceId == "stale-device");

            service.StopMonitoring();
            Assert.DoesNotContain(
                await watcher.DrainAsync(repository, cts.Token),
                e => e.Device.DeviceId == "stale-device");

            await service.DisposeAsync();
            repository.Dispose();
        }
        finally
        {
            release.TrySetResult();
            await service.DisposeAsync();
            // Drain the manual scan and the real loop before ending the fake lifetime.
            await Task.WhenAll(staleRescan, loop).WaitAsync(Bound, CancellationToken.None);
            repository.Dispose();
        }
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task BlockedPublication_DoesNotBlockLifecycle_DisposeDrainBounded()
    {
        // Arrange - a publication is parked mid-flight, holding the publication path.
        // Lifecycle operations must stay bounded regardless.
        var (service, repository, findYubiKeys, _, _) = CreateService(shutdownTimeout: TimeSpan.FromMilliseconds(250));
        findYubiKeys.SetDevices([new FakeYubiKey("device-a", ConnectionType.SmartCard)]);

        var (parked, release) = ParkFirstPublication(service);

        service.StartMonitoring(TimeSpan.FromHours(1));
        var oldLoop = service.MonitoringCompletionForTest;
        Task nextLoop = Task.CompletedTask;
        try
        {
            await parked.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            // Act & Assert - stop, restart, and dispose all complete bounded while the
            // publication is still stuck on the publication path.
            service.StopMonitoring();
            Assert.False(service.IsMonitoring);

            service.StartMonitoring(TimeSpan.FromHours(1));
            nextLoop = service.MonitoringCompletionForTest;
            Assert.True(service.IsMonitoring);

            await service.DisposeAsync().AsTask()
                .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            // Cleanup - unblock the stuck publication; it unwinds against never-disposed gates.
            release();
        }
        finally
        {
            release();
            await service.DisposeAsync();
            // Both loops can outlive the bounded drain while a publication is parked.
            await Task.WhenAll(oldLoop, nextLoop).WaitAsync(Bound, CancellationToken.None);
            repository.Dispose();
        }
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task StopMonitoring_CancellationHonoringScan_UnblocksPromptly_NoLoopAccumulation()
    {
        // Arrange - discovery blocks until cancelled, honoring the loop token.
        // Lifecycle cancellation must propagate into discovery, and stopped
        // generations must not accumulate blocked scans.
        var (service, repository, findYubiKeys, _, _) = CreateService();
        findYubiKeys.HangUntilCancelled = true;

        for (var cycle = 0; cycle < 3; cycle++)
        {
            service.StartMonitoring(TimeSpan.FromHours(1));
            var loop = service.MonitoringCompletionForTest;
            try
            {
                await AsyncWait.WaitUntilAsync(() => findYubiKeys.ActiveScans == 1, $"Cycle {cycle}: scan never started");

                service.StopMonitoring();

                Assert.False(service.IsMonitoring);
                await loop.WaitAsync(Bound, TestContext.Current.CancellationToken);
                Assert.Equal(0, findYubiKeys.ActiveScans);
            }
            finally
            {
                service.StopMonitoring();
                // Cancellation must unwind the complete producer before the next legacy restart.
                await loop.WaitAsync(Bound, CancellationToken.None);
            }
        }

        // One scan per generation - no blocked loops accumulated across cycles.
        Assert.Equal(3, findYubiKeys.ScanCount);

        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task SlowScan_OutlivingStopTimeout_CannotPublish_AndRestartRecovers()
    {
        // Arrange - a slow scan exceeds the shutdown bound; StopMonitoring abandons it.
        var (service, repository, findYubiKeys, _, _) = CreateService(shutdownTimeout: TimeSpan.FromMilliseconds(250));
        using var cts = new CancellationTokenSource(Bound);
        findYubiKeys.HangIgnoringCancellation = true;
        service.StartMonitoring(TimeSpan.FromHours(1));
        var oldLoop = service.MonitoringCompletionForTest;
        Task nextLoop = Task.CompletedTask;
        try
        {
            await AsyncWait.WaitUntilAsync(() => findYubiKeys.ActiveScans == 1, "Initial scan never started");

            service.StopMonitoring();

            await using var watcher = await DeviceEventWatcher.StartAsync(repository, cts.Token);

            // Act - the slow scan completes after the stop timeout. Its snapshot must
            // be suppressed, not published.
            findYubiKeys.SetDevices([new FakeYubiKey("stale-device", ConnectionType.SmartCard)]);
            findYubiKeys.ReleaseHungScans();
            await oldLoop.WaitAsync(Bound, TestContext.Current.CancellationToken);

            Assert.Empty(repository.GetAll());
            Assert.Empty(await watcher.DrainAsync(repository, cts.Token));
            var afterDrain = watcher.Count;

            // The stop timeout is not terminal: a subsequent start publishes normally.
            findYubiKeys.HangIgnoringCancellation = false;
            findYubiKeys.SetDevices([new FakeYubiKey("device-b", ConnectionType.SmartCard)]);
            service.StartMonitoring(TimeSpan.FromHours(1));
            nextLoop = service.MonitoringCompletionForTest;
            await AsyncWait.WaitUntilAsync(
                () => repository.GetAll().Any(d => d.DeviceId == "device-b"),
                "Restart after an abandoned stop did not publish");

            service.StopMonitoring();
            Assert.DoesNotContain(
                await watcher.DrainAsync(repository, cts.Token),
                e => e.Device.DeviceId == "stale-device");
            Assert.True(watcher.Count > afterDrain, "the restarted generation published nothing");

            await service.DisposeAsync();
            repository.Dispose();
        }
        finally
        {
            findYubiKeys.ReleaseHungScans();
            await service.DisposeAsync();
            // The late result must have passed publication admission before asserting no stale event.
            await Task.WhenAll(oldLoop, nextLoop).WaitAsync(Bound, CancellationToken.None);
            repository.Dispose();
        }
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task DisposeAsync_PublicationInFlight_BoundedDrain_LatePublicationCompletesCleanly()
    {
        // Arrange - an admitted publication is parked on the publication path when
        // disposal begins.
        var (service, repository, findYubiKeys, _, _) = CreateService(shutdownTimeout: TimeSpan.FromMilliseconds(250));
        findYubiKeys.SetDevices([new FakeYubiKey("device-a", ConnectionType.SmartCard)]);

        var (parked, release) = ParkFirstPublication(service);

        // Run the rescan off the test thread: the fake completes synchronously, so the
        // parked publication would otherwise block the test thread itself.
        var rescanToken = TestContext.Current.CancellationToken;
        var rescan = Task.Run(() => service.RescanAsync(rescanToken), rescanToken);
        try
        {
            await parked.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            // Act - dispose must bounded-drain the in-flight publication and abandon it
            // on timeout instead of hanging or disposing gates out from under it.
            await service.DisposeAsync().AsTask()
                .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            // The admitted publication may complete after dispose (documented contract)
            // and must complete cleanly - no disposed-semaphore faults.
            release();
            await rescan;

            Assert.Contains(repository.GetAll(), d => d.DeviceId == "device-a");
            repository.Dispose();
        }
        finally
        {
            release();
            await service.DisposeAsync();
            // The manual producer still owns the admitted publication until it finishes unwinding.
            await rescan.WaitAsync(Bound, CancellationToken.None);
            repository.Dispose();
        }
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task DisposeAsync_LatePublication_AfterRepositoryDisposed_IsDiscardedNotThrown()
    {
        // Arrange - reproduces the manager's shutdown order: the monitor's bounded
        // drain times out on a parked publication, DisposeAsync returns, and the
        // manager then disposes the repository. The parked publication resumes
        // afterwards against a disposed repository.
        var (service, repository, findYubiKeys, _, _) = CreateService(shutdownTimeout: TimeSpan.FromMilliseconds(250));
        findYubiKeys.SetDevices([new FakeYubiKey("device-a", ConnectionType.SmartCard)]);

        // Parking after admission but before UpdateCache is the actual window: a
        // publication already past the admission check is the only one that can still
        // reach a repository the manager has since disposed.
        var (parked, release) = ParkFirstPublication(service);

        var rescanToken = TestContext.Current.CancellationToken;
        var rescan = Task.Run(() => service.RescanAsync(rescanToken), rescanToken);
        try
        {
            await parked.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            await service.DisposeAsync().AsTask()
                .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            // The manager disposes the repository immediately after the monitor.
            repository.Dispose();

            // Act - release the parked publication into the disposed repository.
            release();

            // Assert - it is discarded, not thrown. The type contract promises no device
            // event escapes a disposed manager; UpdateCache throws once disposed, so the
            // publish path must absorb that rather than surface it.
            await rescan.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
        finally
        {
            release();
            await service.DisposeAsync();
            // Always release and drain the manual producer, even on runner cancellation.
            await rescan.WaitAsync(Bound, CancellationToken.None);
            repository.Dispose();
        }
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task CreateAsync_ObjectDisposedFromInitialPublication_StillSurfaces()
    {
        // Arrange - the late-publication guard absorbs ObjectDisposedException only while the
        // monitor itself is disposing. A repository disposed out from under a live monitor throws
        // the same exception type and is a composition bug, so it must keep surfacing through the
        // normal scan failure path rather than being misattributed to shutdown.
        var (service, repository, findYubiKeys, _, _) = CreateService();
        findYubiKeys.SetDevices([new FakeYubiKey("device-a", ConnectionType.SmartCard)]);

        repository.Dispose();

        // Act + Assert - the monitor is not disposed, so the exception propagates.
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1)));
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
    ///     A second <c>StartMonitoring</c> with a <em>different</em> interval is ignored, not applied and not
    ///     rejected. The same-interval idempotence test cannot see this: it passes whether the argument is
    ///     honoured or discarded, so it constrains nothing about the interesting case.
    /// </summary>
    /// <remarks>
    ///     Pinned by generation identity. Applying a new interval would require retiring the running
    ///     generation and installing a successor, so an unchanged generation is proof the call was a no-op
    ///     rather than a silent restart.
    /// </remarks>
    [Fact]
    public async Task StartMonitoring_WhileRunningWithDifferentInterval_IsIgnoredNotApplied()
    {
        var (service, repository, _, hidListener, _) = CreateService();

        service.StartMonitoring(TimeSpan.FromHours(1));
        var generation = CurrentGenerationOf(service);
        var startCountAfterFirst = hidListener.StartCount;

        service.StartMonitoring(TimeSpan.FromMilliseconds(5));

        Assert.Same(generation, CurrentGenerationOf(service));
        Assert.Equal(startCountAfterFirst, hidListener.StartCount);
        Assert.True(service.IsMonitoring);

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    /// <summary>
    ///     Listener events from either transport must notify the finder before the triggered rescan. The
    ///     finder treats the transport as diagnostic context and globally invalidates identity and metadata
    ///     caches; <c>FindYubiKeysFaultInjectionTests</c> pins that eviction behavior.
    /// </summary>
    [Fact]
    public async Task ListenerEvents_NotifyTheFinderOfTransportActivity_EvictingEvidenceGlobally()
    {
        var (service, repository, findYubiKeys, hidListener, smartCardListener) = CreateService();

        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, service,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));

        hidListener.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Removed));
        Assert.Equal([ConnectionType.Hid], findYubiKeys.TransportActivity);

        smartCardListener.Raise();
        Assert.Equal([ConnectionType.Hid, ConnectionType.SmartCard], findYubiKeys.TransportActivity);

        await findYubiKeys.WaitForCompletedScanAsync(2, TestContext.Current.CancellationToken);
        Assert.Equal([ConnectionType.Hid, ConnectionType.SmartCard], findYubiKeys.ActivityAtLastScan);
    }

    /// <summary>
    ///     Restart after an unexpected loop death must retire the dead generation completely — cancelled,
    ///     signalled, and no longer reachable through <c>_current</c>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Cancelling alone is not retirement. Admission in <c>PublishSnapshotAsync</c> compares against
    ///         <c>_current</c>, and <c>RescanAsync</c> waits on the caller's token rather than the
    ///         generation's, so a dead generation still sitting in <c>_current</c> can publish stale truth.
    ///         The restart branch used to leave it there across listener teardown and startup.
    ///     </para>
    ///     <para>
    ///         The HID listener's <c>Start</c> hook is the observation point: it runs after retirement and
    ///         before the successor is installed, which is exactly the interval that used to be unguarded.
    ///     </para>
    /// </remarks>
    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task RestartAfterLoopDeath_RetiresTheDeadGenerationFromCurrent()
    {
        var (service, repository, _, hidListener, _) = CreateService();

        service.StartMonitoring(TimeSpan.FromHours(1));
        var deadGeneration = CurrentGenerationOf(service);
        var deadLoop = service.MonitoringCompletionForTest;
        Assert.NotNull(deadGeneration);

        // MonitoringLoopAsync swallows every exception, so no in-process failure can leave a completed
        // task behind. Substituting one is the only way to reach the restart branch at all — which is
        // itself why the branch had zero coverage.
        typeof(YubiKeyDeviceMonitorService)
            .GetField("_monitoringTask", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(service, Task.CompletedTask);

        object? currentDuringListenerStartup = null;
        hidListener.OnStart = () => currentDuringListenerStartup = CurrentGenerationOf(service);

        service.StartMonitoring(TimeSpan.FromHours(1));

        Assert.NotNull(currentDuringListenerStartup);
        Assert.NotSame(deadGeneration, currentDuringListenerStartup);
        Assert.NotSame(deadGeneration, CurrentGenerationOf(service));
        Assert.True(service.IsMonitoring);

        service.StopMonitoring();
        await service.DisposeAsync();
        // Reflection replaces only the slot, not the old producer; retirement must drain that task too.
        await deadLoop.WaitAsync(Bound, TestContext.Current.CancellationToken);
        repository.Dispose();
    }

    private static object? CurrentGenerationOf(YubiKeyDeviceMonitorService service) =>
        typeof(YubiKeyDeviceMonitorService)
            .GetField("_current", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(service);

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
        findYubiKeys.ObserveLoop(service);

        return (service, repository, findYubiKeys, hidListener, smartCardListener);
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

        /// <summary>
        ///     Runs inside <c>StartMonitoring</c>, after generation retirement and before the successor is
        ///     installed. The only seam that can observe that interval from outside.
        /// </summary>
        public Action? OnStart { get; set; }

        public override void Start()
        {
            StartCount++;
            CapturedDeviceEvent = DeviceEvent;
            OnStart?.Invoke();
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
        private int _completedScans;

        public int CompletedScans => Volatile.Read(ref _completedScans);
        public void ObserveLoop(YubiKeyDeviceMonitorService service) =>
            service.MonitoringWaitReachedForTest = () => Volatile.Write(ref _completedScans, ScanCount);
        public Task WaitForCompletedScanAsync(int count, CancellationToken cancellationToken) => AsyncWait.WaitUntilAsync(
            () => CompletedScans >= count, $"Loop did not complete scan/publication {count}", Bound, cancellationToken);

        public List<ConnectionType> TransportActivity { get; } = [];
        public IReadOnlyList<ConnectionType> ActivityAtLastScan { get; private set; } = [];

        public void NotifyTransportActivity(ConnectionType transport)
        {
            lock (_syncLock)
            {
                TransportActivity.Add(transport);
            }
        }

        public TimeSpan ScanDelay { get; set; }

        /// <summary>
        /// When set, scans block until <see cref="ReleaseHungScans"/> is called,
        /// ignoring the caller's cancellation token. Models a discovery backend
        /// stuck in native I/O.
        /// </summary>
        public bool HangIgnoringCancellation { get; set; }

        /// <summary>
        /// When set, scans block until the caller's cancellation token fires and
        /// then throw <see cref="OperationCanceledException"/>. Models a
        /// cancellation-honoring discovery backend, asserting that lifecycle
        /// cancellation propagates into discovery.
        /// </summary>
        public bool HangUntilCancelled { get; set; }

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

        public async Task<IReadOnlyList<IYubiKey>> FindAllAsync(
            ConnectionType type,
            CancellationToken cancellationToken = default)
        {
            lock (_syncLock)
                ActivityAtLastScan = [.. TransportActivity];
            Interlocked.Increment(ref _scanCount);
            var activeScans = Interlocked.Increment(ref _activeScans);
            UpdateMaxConcurrentScans(activeScans);

            try
            {
                if (HangIgnoringCancellation)
                {
                    await _hangReleased.Task.ConfigureAwait(false);
                }

                if (HangUntilCancelled)
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
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

}