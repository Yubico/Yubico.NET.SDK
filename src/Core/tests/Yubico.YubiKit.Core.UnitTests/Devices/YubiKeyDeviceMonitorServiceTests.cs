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

        var events = new RecordingObserver<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events);

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
        await AsyncWait.WaitUntilAsync(() => repository.HasData, "Initial monitoring rescan did not update repository");
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
        await AsyncWait.WaitUntilAsync(() => findYubiKeys.ScanCount >= 1, "Initial monitoring rescan did not run");

        var events = new RecordingObserver<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events);
        var scanCount = findYubiKeys.ScanCount;

        // Act
        hidListener.Raise(new HidDeviceRescanHint(
            HidDeviceChangeKind.Added,
            PlatformDeviceId: "diagnostic-only",
            DevicePath: "/dev/hidraw999"));

        // Assert
        await AsyncWait.WaitUntilAsync(() => findYubiKeys.ScanCount > scanCount, "HID hint did not trigger rescan");
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
        await AsyncWait.WaitUntilAsync(() => findYubiKeys.ScanCount >= 1, "Initial monitoring rescan did not run");

        findYubiKeys.SetDevices([new FakeYubiKey("device-2", ConnectionType.HidOtp)]);

        // Act - unknown removals must still fall back to a repository rescan.
        hidListener.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Removed));

        // Assert
        await AsyncWait.WaitUntilAsync(
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
        await AsyncWait.WaitUntilAsync(
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
        await AsyncWait.WaitUntilAsync(() => findYubiKeys.ScanCount >= 1, "Listener events did not trigger a rescan");
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
        await AsyncWait.WaitUntilAsync(
            () => findYubiKeys.ScanCount >= 1 && findYubiKeys.ActiveScans == 0,
            "Initial monitoring rescan did not complete");

        findYubiKeys.ResetCounters();

        // Act - a rapid burst must coalesce into few rescans, not one rescan per hint.
        for (var i = 0; i < 32; i++)
        {
            hidListener.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Added, $"hid-{i}"));
        }

        await AsyncWait.WaitUntilAsync(() => findYubiKeys.ScanCount >= 1, "Hint burst did not trigger a rescan");

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
        await AsyncWait.WaitUntilAsync(() => findYubiKeys.ActiveScans == 1, "Initial blocked scan did not start");

        for (var i = 0; i < 128; i++)
        {
            if ((i & 1) == 0)
                hidListener.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Added, $"storm-{i}"));
            else
                smartCardListener.Raise();
        }

        findYubiKeys.ReleaseHungScans();
        await AsyncWait.WaitUntilAsync(
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
        await AsyncWait.WaitUntilAsync(
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
    public async Task StartMonitoring_SmartCardFactoryThrows_StartsWithHidOnly()
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
        service.StartMonitoring(TimeSpan.FromSeconds(30));

        Assert.True(service.IsMonitoring);
        Assert.Equal(1, hid.StartCount);
        Assert.NotNull(hid.DeviceEvent);
        Assert.Equal(0, hid.StopCount);
        Assert.Equal(0, hid.DisposeCount);

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    public async Task StartMonitoring_HidStartThrows_StartsWithSmartCardOnly()
    {
        var repository = new YubiKeyDeviceRepository();
        var hid = new FakeHidDeviceListener { ThrowOnStart = true };
        var smartCard = new FakeSmartCardDeviceListener();
        var service = new YubiKeyDeviceMonitorService(
            repository,
            new FakeFindYubiKeys([]),
            () => hid,
            () => smartCard);

        // HID cannot start, but SmartCard starts, so monitoring runs (degraded).
        service.StartMonitoring(TimeSpan.FromSeconds(30));

        Assert.True(service.IsMonitoring);
        // The failed HID listener is cleaned up individually.
        Assert.Null(hid.DeviceEvent);
        Assert.Equal(1, hid.StopCount);
        Assert.Equal(1, hid.DisposeCount);
        // SmartCard remains the active listener and is not torn down.
        Assert.NotNull(smartCard.DeviceEvent);
        Assert.Equal(1, smartCard.StartCount);
        Assert.Equal(0, smartCard.StopCount);
        Assert.Equal(0, smartCard.DisposeCount);

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    public async Task StartMonitoring_HidReturnsError_StartsWithSmartCardOnly()
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
        service.StartMonitoring(TimeSpan.FromSeconds(30));

        Assert.True(service.IsMonitoring);
        Assert.Null(hid.DeviceEvent);
        Assert.Equal(1, hid.StartCount);
        Assert.Equal(1, hid.StopCount);
        Assert.Equal(1, hid.DisposeCount);
        Assert.NotNull(smartCard.DeviceEvent);
        Assert.Equal(1, smartCard.StartCount);
        Assert.Equal(0, smartCard.StopCount);
        Assert.Equal(0, smartCard.DisposeCount);

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    public async Task StartMonitoring_SmartCardReturnsError_StartsWithHidOnly()
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
        service.StartMonitoring(TimeSpan.FromSeconds(30));

        Assert.True(service.IsMonitoring);
        Assert.NotNull(hid.DeviceEvent);
        Assert.Equal(1, hid.StartCount);
        Assert.Equal(0, hid.StopCount);
        Assert.Equal(0, hid.DisposeCount);
        Assert.Null(smartCard.DeviceEvent);
        Assert.Equal(1, smartCard.StartCount);
        Assert.Equal(1, smartCard.StopCount);
        Assert.Equal(1, smartCard.DisposeCount);

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    public async Task StartMonitoring_SmartCardStartThrows_StartsWithHidAndDetachesFailedListener()
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
        service.StartMonitoring(TimeSpan.FromSeconds(30));

        Assert.True(service.IsMonitoring);
        Assert.NotNull(hid.DeviceEvent);
        Assert.Equal(1, hid.StartCount);
        // The failed SmartCard listener is cleaned up and detached from its signal.
        Assert.Null(failedSmartCard.DeviceEvent);
        Assert.Equal(1, failedSmartCard.StopCount);
        Assert.Equal(1, failedSmartCard.DisposeCount);

        await AsyncWait.WaitUntilAsync(
            () => findYubiKeys.ScanCount >= 1 && findYubiKeys.ActiveScans == 0,
            "Initial scan did not finish");
        var scanCountBeforeStaleCallback = findYubiKeys.ScanCount;

        // A callback from the detached, failed listener can no longer trigger a rescan.
        failedSmartCard.Raise();
        await Task.Delay(
            YubiKeyDeviceMonitorService.ThrottleInterval + TimeSpan.FromMilliseconds(100),
            TestContext.Current.CancellationToken);
        Assert.Equal(scanCountBeforeStaleCallback, findYubiKeys.ScanCount);

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    public async Task StartMonitoring_BothListenersFail_StartsIntervalOnlyMonitoring()
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
        service.StartMonitoring(TimeSpan.FromMilliseconds(200));

        Assert.True(service.IsMonitoring);
        Assert.Null(hid.DeviceEvent);
        Assert.Null(smartCard.DeviceEvent);
        Assert.Equal(1, hid.DisposeCount);
        Assert.Equal(1, smartCard.DisposeCount);

        // With no listeners to signal, only the 200ms interval fallback can drive
        // rescans. Require at least two scans so we prove the interval loop keeps
        // running on its own, not merely the one-shot startup rescan.
        await AsyncWait.WaitUntilAsync(() => findYubiKeys.ScanCount >= 2, "Interval fallback rescan did not run without listeners");

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
        await AsyncWait.WaitUntilAsync(() => findYubiKeys.ActiveScans == 1, "Initial rescan never started");

        // Act - disposal must abandon the stuck loop and rescan gate instead of hanging
        await service.DisposeAsync().AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Cleanup - unblock the stuck scan; the abandoned (undisposed) gate accepts its Release
        findYubiKeys.ReleaseHungScans();
        await AsyncWait.WaitUntilAsync(() => findYubiKeys.ActiveScans == 0, "Hung rescan never completed after release");

        repository.Dispose();
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task RescanAsync_SupersededByLifecycleSwap_DiscardsStaleSnapshot()
    {
        // Arrange - a manual rescan hangs in discovery, capturing the pre-start
        // monitor generation.
        var (service, repository, findYubiKeys, _, _) = CreateService(shutdownTimeout: TimeSpan.FromMilliseconds(250));
        findYubiKeys.HangIgnoringCancellation = true;

        var staleRescan = service.RescanAsync(TestContext.Current.CancellationToken);
        await AsyncWait.WaitUntilAsync(() => findYubiKeys.ActiveScans >= 1, "Manual rescan never started");

        // Act - start/stop swaps the monitor generation; the manual rescan's
        // captured generation is now superseded.
        service.StartMonitoring(TimeSpan.FromHours(1));
        service.StopMonitoring();

        var events = new RecordingObserver<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events);

        findYubiKeys.SetDevices([new FakeYubiKey("stale-device", ConnectionType.SmartCard)]);
        findYubiKeys.ReleaseHungScans();

        // The superseded rescan completes silently without publishing.
        await staleRescan;
        await AsyncWait.WaitUntilAsync(() => findYubiKeys.ActiveScans == 0, "Hung scans never unwound");

        // Assert - the stale snapshot was discarded, not published as device truth.
        Assert.Empty(events);
        Assert.Empty(repository.GetAll());

        await service.DisposeAsync();
        repository.Dispose();
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
        await AsyncWait.WaitUntilAsync(() => findYubiKeys.ActiveScans == 1, "Initial scan never started");

        service.StopMonitoring();
        Assert.False(service.IsMonitoring);

        // Act - restart must succeed immediately with a fresh generation even though
        // the abandoned scan still holds its dead generation's scan gate.
        findYubiKeys.HangIgnoringCancellation = false;
        findYubiKeys.SetDevices([new FakeYubiKey("device-b", ConnectionType.SmartCard)]);
        service.StartMonitoring(TimeSpan.FromHours(1));
        Assert.True(service.IsMonitoring);

        await AsyncWait.WaitUntilAsync(
            () => repository.GetAll().Any(d => d.DeviceId == "device-b"),
            "Restarted monitoring did not publish with a new generation");

        // The hung scan from the abandoned generation later returns - its snapshot
        // must fail admission and emit no device events.
        var events = new RecordingObserver<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events);
        findYubiKeys.SetDevices([new FakeYubiKey("device-c", ConnectionType.SmartCard)]);
        findYubiKeys.ReleaseHungScans();
        await AsyncWait.WaitUntilAsync(() => findYubiKeys.ActiveScans == 0, "Abandoned scan never unwound");
        await Task.Delay(200, TestContext.Current.CancellationToken);

        Assert.Empty(events);
        Assert.DoesNotContain(repository.GetAll(), d => d.DeviceId == "device-c");
        Assert.Contains(repository.GetAll(), d => d.DeviceId == "device-b");

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task CrossGenerationPublications_SerializeAndSuccessorSnapshotLandsLast()
    {
        // Arrange - a device-event subscriber blocks the first publication inside
        // UpdateCache, holding the publication gate across a generation swap.
        var (service, repository, findYubiKeys, _, _) = CreateService(shutdownTimeout: TimeSpan.FromMilliseconds(250));
        findYubiKeys.SetDevices([new FakeYubiKey("device-a", ConnectionType.SmartCard)]);

        using var subscriberEntered = new ManualResetEventSlim();
        using var subscriberRelease = new ManualResetEventSlim();
        var blockOnce = 1;
        var events = new List<DeviceEvent>();
        var activeEmissions = 0;
        var maxConcurrentEmissions = 0;
        using var subscription = repository.DeviceChanges.Subscribe(new RecordingObserver<DeviceEvent>(deviceEvent =>
        {
            lock (events)
            {
                events.Add(deviceEvent);
                activeEmissions++;
                maxConcurrentEmissions = Math.Max(maxConcurrentEmissions, activeEmissions);
            }

            try
            {
                if (Interlocked.Exchange(ref blockOnce, 0) == 1)
                {
                    subscriberEntered.Set();
                    subscriberRelease.Wait(TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                lock (events)
                {
                    activeEmissions--;
                }
            }
        }));

        service.StartMonitoring(TimeSpan.FromHours(1));
        Assert.True(
            subscriberEntered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken),
            "First publication never reached the subscriber");

        // Act - restart while the old-generation publication is held inside UpdateCache.
        service.StopMonitoring();
        findYubiKeys.SetDevices([new FakeYubiKey("device-b", ConnectionType.SmartCard)]);
        service.StartMonitoring(TimeSpan.FromHours(1));

        // The successor generation scans immediately (its scan is not serialized
        // behind the dead generation) but must not enter UpdateCache while the
        // old publication holds the publication gate.
        await AsyncWait.WaitUntilAsync(() => findYubiKeys.ScanCount >= 2, "Successor generation never scanned");
        await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.Contains(repository.GetAll(), d => d.DeviceId == "device-a");
        Assert.DoesNotContain(repository.GetAll(), d => d.DeviceId == "device-b");

        // Release the old publication: it completes first, then the successor's
        // snapshot lands last. Publications never interleave.
        subscriberRelease.Set();
        await AsyncWait.WaitUntilAsync(
            () => repository.GetAll().Any(d => d.DeviceId == "device-b"),
            "Successor snapshot was never published");

        Assert.DoesNotContain(repository.GetAll(), d => d.DeviceId == "device-a");
        lock (events)
        {
            Assert.Equal(1, maxConcurrentEmissions);
            Assert.Equal(3, events.Count);
            Assert.Equal((DeviceAction.Added, "device-a"), (events[0].Action, events[0].Device.DeviceId));
            Assert.Equal((DeviceAction.Removed, "device-a"), (events[1].Action, events[1].Device.DeviceId));
            Assert.Equal((DeviceAction.Added, "device-b"), (events[2].Action, events[2].Device.DeviceId));
        }

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
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

        var events = new RecordingObserver<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events);

        var staleRescan = service.RescanAsync(TestContext.Current.CancellationToken);
        await held.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Act - swap generations while the publication is parked between gate
        // acquisition and admission. StartMonitoring takes only the state lock,
        // so it must not block on the held publication gate.
        findYubiKeys.SetDevices([new FakeYubiKey("device-b", ConnectionType.SmartCard)]);
        service.StartMonitoring(TimeSpan.FromHours(1));
        Assert.True(service.IsMonitoring);

        release.SetResult();
        await staleRescan;

        // Assert - admission failed for the parked snapshot; it was discarded and
        // the successor generation published.
        await AsyncWait.WaitUntilAsync(
            () => repository.GetAll().Any(d => d.DeviceId == "device-b"),
            "Successor generation did not publish");
        Assert.DoesNotContain(repository.GetAll(), d => d.DeviceId == "stale-device");
        Assert.DoesNotContain(events, e => e.Device.DeviceId == "stale-device");

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task BlockedSubscriber_DoesNotBlockLifecycle_DisposeDrainBounded()
    {
        // Arrange - a subscriber blocks a publication inside UpdateCache, holding
        // the publication path. Lifecycle operations must stay bounded regardless.
        var (service, repository, findYubiKeys, _, _) = CreateService(shutdownTimeout: TimeSpan.FromMilliseconds(250));
        findYubiKeys.SetDevices([new FakeYubiKey("device-a", ConnectionType.SmartCard)]);

        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var blockOnce = 1;
        using var subscription = repository.DeviceChanges.Subscribe(new RecordingObserver<DeviceEvent>(_ =>
        {
            if (Interlocked.Exchange(ref blockOnce, 0) == 1)
            {
                entered.Set();
                release.Wait(TestContext.Current.CancellationToken);
            }
        }));

        service.StartMonitoring(TimeSpan.FromHours(1));
        Assert.True(
            entered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken),
            "Publication never reached the subscriber");

        // Act & Assert - stop, restart, and dispose all complete bounded while the
        // publication is still stuck inside UpdateCache.
        service.StopMonitoring();
        Assert.False(service.IsMonitoring);

        service.StartMonitoring(TimeSpan.FromHours(1));
        Assert.True(service.IsMonitoring);

        await service.DisposeAsync().AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Cleanup - unblock the stuck publication; it unwinds against never-disposed gates.
        release.Set();
        repository.Dispose();
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
            await AsyncWait.WaitUntilAsync(() => findYubiKeys.ActiveScans == 1, $"Cycle {cycle}: scan never started");

            service.StopMonitoring();

            Assert.False(service.IsMonitoring);
            await AsyncWait.WaitUntilAsync(() => findYubiKeys.ActiveScans == 0, $"Cycle {cycle}: cancelled scan never unwound");
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
        findYubiKeys.HangIgnoringCancellation = true;
        service.StartMonitoring(TimeSpan.FromHours(1));
        await AsyncWait.WaitUntilAsync(() => findYubiKeys.ActiveScans == 1, "Initial scan never started");

        service.StopMonitoring();

        var events = new RecordingObserver<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events);

        // Act - the slow scan completes after the stop timeout. Its snapshot must
        // be suppressed, not published.
        findYubiKeys.SetDevices([new FakeYubiKey("stale-device", ConnectionType.SmartCard)]);
        findYubiKeys.ReleaseHungScans();
        await AsyncWait.WaitUntilAsync(() => findYubiKeys.ActiveScans == 0, "Slow scan never completed");
        await Task.Delay(200, TestContext.Current.CancellationToken);

        Assert.Empty(events);
        Assert.Empty(repository.GetAll());

        // The stop timeout is not terminal: a subsequent start publishes normally.
        findYubiKeys.HangIgnoringCancellation = false;
        findYubiKeys.SetDevices([new FakeYubiKey("device-b", ConnectionType.SmartCard)]);
        service.StartMonitoring(TimeSpan.FromHours(1));
        await AsyncWait.WaitUntilAsync(
            () => repository.GetAll().Any(d => d.DeviceId == "device-b"),
            "Restart after an abandoned stop did not publish");
        Assert.DoesNotContain(events, e => e.Device.DeviceId == "stale-device");

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task DisposeAsync_PublicationInFlight_BoundedDrain_LatePublicationCompletesCleanly()
    {
        // Arrange - an admitted publication is held inside UpdateCache when
        // disposal begins.
        var (service, repository, findYubiKeys, _, _) = CreateService(shutdownTimeout: TimeSpan.FromMilliseconds(250));
        findYubiKeys.SetDevices([new FakeYubiKey("device-a", ConnectionType.SmartCard)]);

        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var blockOnce = 1;
        using var subscription = repository.DeviceChanges.Subscribe(new RecordingObserver<DeviceEvent>(_ =>
        {
            if (Interlocked.Exchange(ref blockOnce, 0) == 1)
            {
                entered.Set();
                release.Wait(TestContext.Current.CancellationToken);
            }
        }));

        // Run the rescan off the test thread: the fake completes synchronously, so
        // the publication (and the blocking subscriber) would otherwise run inline
        // on the test thread and deadlock against entered/release.
        var rescanToken = TestContext.Current.CancellationToken;
        var rescan = Task.Run(() => service.RescanAsync(rescanToken), rescanToken);
        Assert.True(
            entered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken),
            "Publication never reached the subscriber");

        // Act - dispose must bounded-drain the in-flight publication and abandon it
        // on timeout instead of hanging or disposing gates out from under it.
        await service.DisposeAsync().AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // The admitted publication may complete after dispose (documented contract)
        // and must complete cleanly - no disposed-semaphore faults.
        release.Set();
        await rescan;

        Assert.Contains(repository.GetAll(), d => d.DeviceId == "device-a");
        repository.Dispose();
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task DisposeAsync_LatePublication_AfterRepositoryDisposed_IsDiscardedNotThrown()
    {
        // Arrange - reproduces the manager's shutdown order: the monitor's bounded
        // drain times out on a publication blocked in a subscriber, DisposeAsync
        // returns, and the manager then disposes the repository. The blocked
        // publication resumes afterwards against a disposed repository.
        var (service, repository, findYubiKeys, _, _) = CreateService(shutdownTimeout: TimeSpan.FromMilliseconds(250));

        // Two devices, so UpdateCache emits twice. Blocking on the FIRST emission
        // leaves a second OnNext still to come after the repository is disposed -
        // that is the actual window, not the initial ThrowIfDisposed which has
        // already passed by the time a subscriber can block.
        findYubiKeys.SetDevices(
        [
            new FakeYubiKey("device-a", ConnectionType.SmartCard),
            new FakeYubiKey("device-b", ConnectionType.SmartCard)
        ]);

        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var blockOnce = 1;
        using var subscription = repository.DeviceChanges.Subscribe(new RecordingObserver<DeviceEvent>(_ =>
        {
            if (Interlocked.Exchange(ref blockOnce, 0) == 1)
            {
                entered.Set();
                release.Wait(TestContext.Current.CancellationToken);
            }
        }));

        var rescanToken = TestContext.Current.CancellationToken;
        var rescan = Task.Run(() => service.RescanAsync(rescanToken), rescanToken);
        Assert.True(
            entered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken),
            "Publication never reached the subscriber");

        await service.DisposeAsync().AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // The manager disposes the repository immediately after the monitor.
        repository.Dispose();

        // Act - release the blocked publication into the disposed repository.
        release.Set();

        // Assert - it is discarded, not thrown. The type contract promises no device
        // event escapes a disposed manager; UpdateCache and the subject both throw
        // once disposed, so the publish path must absorb that rather than surface it.
        await rescan.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task Publish_SubscriberThrowsObjectDisposed_WhileNotDisposing_StillSurfaces()
    {
        // Arrange - UpdateCache invokes subscribers synchronously, so a subscriber
        // touching its own disposed state throws the same exception type the shutdown
        // race produces. Outside monitor disposal that is a subscriber bug and must
        // keep surfacing, not be absorbed by the late-publication guard.
        var (service, repository, findYubiKeys, _, _) = CreateService();
        findYubiKeys.SetDevices([new FakeYubiKey("device-a", ConnectionType.SmartCard)]);

        using var subscription = repository.DeviceChanges.Subscribe(
            new RecordingObserver<DeviceEvent>(_ => throw new ObjectDisposedException("SubscriberOwnedResource")));

        // Act + Assert - the monitor is not disposed, so the exception propagates.
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => service.RescanAsync(TestContext.Current.CancellationToken));

        await service.DisposeAsync();
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
    public async Task ListenerEvents_NotifyTheFinderOfTransportActivity_PerTransport()
    {
        var (service, repository, findYubiKeys, hidListener, smartCardListener) = CreateService();

        service.StartMonitoring(TimeSpan.FromHours(1));

        hidListener.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Removed));
        Assert.Equal([ConnectionType.Hid], findYubiKeys.TransportActivity);

        smartCardListener.Raise();
        Assert.Equal([ConnectionType.Hid, ConnectionType.SmartCard], findYubiKeys.TransportActivity);

        service.StopMonitoring();
        await service.DisposeAsync();
        repository.Dispose();
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

        public List<ConnectionType> TransportActivity { get; } = [];

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