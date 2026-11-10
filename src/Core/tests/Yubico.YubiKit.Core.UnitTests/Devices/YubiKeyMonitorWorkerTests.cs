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
/// Tests for <see cref="YubiKeyMonitorWorker"/>.
/// </summary>
public class YubiKeyMonitorWorkerTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task CreateAsync_PublishesInitialRepositoryMembership()
    {
        // Arrange
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([
            new FakeYubiKey("device-1", ConnectionType.SmartCard),
            new FakeYubiKey("device-2", ConnectionType.HidFido)
        ]);
        var worker = new YubiKeyMonitorWorker(repository, findYubiKeys,
            () => new FakeHidDeviceListener(), () => new FakeSmartCardDeviceListener());

        // Act
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));

        // Assert
        var devices = repository.GetAll();
        Assert.Equal(2, devices.Count);
        Assert.True(repository.HasData);
    }

    [Fact]
    public async Task ListenerScan_ChangedDiscovery_UpdatesRepositoryMembership()
    {
        var (worker, repository, findYubiKeys, _, smartCard) = CreateWorker();
        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.SmartCard)]);
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
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
        var (worker, repository, findYubiKeys, _, smartCard) = CreateWorker();
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
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
        var (worker, repository, findYubiKeys, _, _) = CreateWorker();
        findYubiKeys.SetDevices([new FakeYubiKey("device-1", ConnectionType.HidFido)]);

        // Act
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
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
        var (worker, repository, findYubiKeys, hidListener, _) = CreateWorker();
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
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
        var (worker, repository, findYubiKeys, hidListener, _) = CreateWorker();
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
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
        var (worker, repository, findYubiKeys, hidListener, smartCardListener) = CreateWorker();
        findYubiKeys.ScanDelay = TimeSpan.FromMilliseconds(80);

        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
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
        var (worker, repository, findYubiKeys, hidListener, _) = CreateWorker();
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        await findYubiKeys.WaitForCompletedScanAsync(1, TestContext.Current.CancellationToken);

        // Act - a rapid burst must coalesce into few rescans, not one rescan per hint.
        for (var i = 0; i < 32; i++)
        {
            hidListener.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Added, $"hid-{i}"));
        }

        await findYubiKeys.WaitForCompletedScanAsync(2, TestContext.Current.CancellationToken);

        // Allow any (incorrect) trailing per-hint rescans to surface before asserting the bound.
        await Task.Delay(YubiKeyMonitorWorker.MaxCoalesceInterval + YubiKeyMonitorWorker.ThrottleInterval,
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
        var (worker, repository, findYubiKeys, hidListener, smartCardListener) = CreateWorker();
        findYubiKeys.HangIgnoringCancellation = true;
        var creation = YubiKeyMonitor.CreateAsync(repository, worker,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        var loop = worker.MonitoringCompletionForTest;
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
            await Task.Delay(YubiKeyMonitorWorker.ThrottleInterval + TimeSpan.FromMilliseconds(100),
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
        var (worker, repository, findYubiKeys, hidListener, _) = CreateWorker();
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
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
            var deadline = DateTimeOffset.UtcNow + YubiKeyMonitorWorker.MaxCoalesceInterval + TimeSpan.FromSeconds(2);
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
    public async Task CreateAsync_SmartCardFactoryThrows_StartsWithHidOnly()
    {
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var hid = new FakeHidDeviceListener();
        var worker = new YubiKeyMonitorWorker(
            repository,
            findYubiKeys,
            () => hid,
            () => throw new InvalidOperationException("Expected SmartCard factory failure."));

        // The SmartCard listener is unavailable, but HID starts, so monitoring
        // runs (degraded). Startup must not throw for an unavailable transport.
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
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
        var worker = new YubiKeyMonitorWorker(
            repository,
            findYubiKeys,
            () => hid,
            () => smartCard);

        // HID cannot start, but SmartCard starts, so monitoring runs (degraded).
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
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
        var worker = new YubiKeyMonitorWorker(
            repository,
            findYubiKeys,
            () => hid,
            () => smartCard);

        // HID reports Error (does not throw); it is cleaned up while SmartCard
        // keeps monitoring alive.
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
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
        var worker = new YubiKeyMonitorWorker(
            repository,
            findYubiKeys,
            () => hid,
            () => smartCard);

        // SmartCard reports Error (the common no-PC/SC case); it is cleaned up
        // while HID keeps monitoring alive.
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
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
        var worker = new YubiKeyMonitorWorker(
            repository,
            findYubiKeys,
            () => hid,
            () => failedSmartCard);

        // SmartCard Start throws; HID starts, so monitoring runs (degraded).
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
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
            YubiKeyMonitorWorker.ThrottleInterval + TimeSpan.FromMilliseconds(100),
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
        var worker = new YubiKeyMonitorWorker(
            repository,
            findYubiKeys,
            () => hid,
            () => smartCard);

        // Neither listener can start, but monitoring still runs on the interval
        // rescan alone. Startup must not throw.
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
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
    public async Task DisposeAsync_StopsMonitoring()
    {
        // Arrange
        var (worker, repository, _, _, _) = CreateWorker();
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        var loop = worker.MonitoringCompletionForTest;

        // Act
        await monitor.DisposeAsync();

        // Assert
        await loop.WaitAsync(Bound, TestContext.Current.CancellationToken);
        Assert.Throws<ObjectDisposedException>(() => monitor.CurrentDevices);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task DisposeAsync_RescanHungIgnoringCancellation_CompletesWithinBoundedTime()
    {
        var (worker, repository, findYubiKeys, _, smartCard) = CreateWorker(shutdownTimeout: TimeSpan.FromMilliseconds(250));
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        var loop = worker.MonitoringCompletionForTest;
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
    public async Task CreateAsync_ObjectDisposedFromInitialPublication_StillSurfaces()
    {
        // Arrange - the late-publication guard absorbs ObjectDisposedException only while the
        // monitor itself is disposing. A repository disposed out from under a live monitor throws
        // the same exception type and is a composition bug, so it must keep surfacing through the
        // normal scan failure path rather than being misattributed to shutdown.
        var (worker, repository, findYubiKeys, _, _) = CreateWorker();
        findYubiKeys.SetDevices([new FakeYubiKey("device-a", ConnectionType.SmartCard)]);

        repository.Dispose();

        // Act + Assert - the monitor is not disposed, so the exception propagates.
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => YubiKeyMonitor.CreateAsync(repository, worker,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1)));
    }

    /// <summary>
    ///     Listener events from either transport must notify the finder before the triggered rescan. The
    ///     finder treats the transport as diagnostic context and globally invalidates identity and metadata
    ///     caches; <c>FindYubiKeysFaultInjectionTests</c> pins that eviction behavior.
    /// </summary>
    [Fact]
    public async Task ListenerEvents_InvalidateEvidenceBeforeSignalingDiscovery()
    {
        var (worker, repository, findYubiKeys, hidListener, smartCardListener) = CreateWorker();

        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));

        hidListener.Raise(new HidDeviceRescanHint(HidDeviceChangeKind.Removed));
        Assert.Equal([ConnectionType.Hid], findYubiKeys.TransportActivity);

        smartCardListener.Raise();
        Assert.Equal([ConnectionType.Hid, ConnectionType.SmartCard], findYubiKeys.TransportActivity);

        await findYubiKeys.WaitForCompletedScanAsync(2, TestContext.Current.CancellationToken);
        Assert.Equal([ConnectionType.Hid, ConnectionType.SmartCard], findYubiKeys.ActivityAtLastScan);
    }

    /// <summary>
    /// Fake IMonitoredDiscovery for testing.
    /// </summary>
    private static (
        YubiKeyMonitorWorker Worker,
        YubiKeyDeviceRepository Repository,
        FakeFindYubiKeys FindYubiKeys,
        FakeHidDeviceListener HidListener,
        FakeSmartCardDeviceListener SmartCardListener) CreateWorker(TimeSpan? shutdownTimeout = null)
    {
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = new FakeFindYubiKeys([]);
        var hidListener = new FakeHidDeviceListener();
        var smartCardListener = new FakeSmartCardDeviceListener();
        var worker = new YubiKeyMonitorWorker(
            repository,
            findYubiKeys,
            () => hidListener,
            () => smartCardListener,
            shutdownTimeout);
        findYubiKeys.ObserveLoop(worker);

        return (worker, repository, findYubiKeys, hidListener, smartCardListener);
    }

    private sealed class FakeHidDeviceListener : HidDeviceListener
    {
        public int StartCount { get; private set; }

        public int StopCount { get; private set; }

        public int DisposeCount { get; private set; }

        public bool ThrowOnStart { get; init; }

        public DeviceListenerStatus StartStatus { get; init; } = DeviceListenerStatus.Started;

        public override void Start()
        {
            StartCount++;
            if (ThrowOnStart)
                throw new InvalidOperationException("Expected HID start failure.");

            Status = StartStatus;
        }

        public override void Stop()
        {
            StopCount++;
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

    private sealed class FakeFindYubiKeys(IReadOnlyList<IYubiKey> initialDevices) : IMonitoredDiscovery
    {
        private readonly Lock _syncLock = new();
        private readonly TaskCompletionSource _hangReleased = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private IReadOnlyList<IYubiKey> _devices = initialDevices;
        private int _activeScans;
        private int _maxConcurrentScans;
        private int _scanCount;
        private int _completedScans;

        public int CompletedScans => Volatile.Read(ref _completedScans);
        public void ObserveLoop(YubiKeyMonitorWorker worker) =>
            worker.MonitoringWaitReachedForTest = () => Volatile.Write(ref _completedScans, ScanCount);
        public Task WaitForCompletedScanAsync(int count, CancellationToken cancellationToken) => AsyncWait.WaitUntilAsync(
            () => CompletedScans >= count, $"Loop did not complete scan/publication {count}", Bound, cancellationToken);

        public List<ConnectionType> TransportActivity { get; } = [];
        public IReadOnlyList<ConnectionType> ActivityAtLastScan { get; private set; } = [];

        public void InvalidateEvidence(ConnectionType transport)
        {
            lock (_syncLock)
            {
                TransportActivity.Add(transport);
            }
        }

        public TimeSpan ScanDelay { get; set; }

        public void Retire() { }

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

        public async Task<IReadOnlyList<IYubiKey>> FindAllAsync(
            CancellationToken cancellationToken)
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

                if (ScanDelay > TimeSpan.Zero)
                {
                    await Task.Delay(ScanDelay, cancellationToken).ConfigureAwait(false);
                }

                IReadOnlyList<IYubiKey> devices;
                lock (_syncLock)
                {
                    devices = _devices;
                }

                return devices;
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