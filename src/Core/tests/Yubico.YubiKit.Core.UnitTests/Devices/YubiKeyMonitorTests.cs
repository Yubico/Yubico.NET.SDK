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
using Yubico.YubiKit.Core.UnitTests.Infrastructure;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class YubiKeyMonitorTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task WatchAsync_IsLazy_AndEachEnumerationReplaysItsOwnSnapshotThenOrderedDeltas()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
        var first = new FakeYubiKey("first");
        var second = new FakeYubiKey("second");
        var third = new FakeYubiKey("third");
        await PublishAsync(fixture, [first]);
        var stream = monitor.WatchAsync(TestToken);
        await using var early = stream.GetAsyncEnumerator(TestToken);
        await PublishAsync(fixture, [second]);
        Assert.True(await early.MoveNextAsync());
        AssertChange<DeviceAdded>(early.Current, second);
        await PublishAsync(fixture, [third]);
        await using var late = stream.GetAsyncEnumerator(TestToken);
        Assert.True(await late.MoveNextAsync());
        AssertChange<DeviceAdded>(late.Current, third);
        await monitor.DisposeAsync();

        Assert.True(await early.MoveNextAsync());
        Assert.IsType<SnapshotComplete>(early.Current);
        Assert.True(await early.MoveNextAsync());
        AssertChange<DeviceRemoved>(early.Current, second);
        Assert.True(await early.MoveNextAsync());
        AssertChange<DeviceAdded>(early.Current, third);
        Assert.False(await early.MoveNextAsync());
        Assert.True(await late.MoveNextAsync());
        Assert.IsType<SnapshotComplete>(late.Current);
        Assert.False(await late.MoveNextAsync());
        Assert.Equal(4, fixture.Finder.ScanCount);
    }

    [Fact]
    public async Task WatchAsync_DuringPausedScan_ReplaysPreviousPublicationAndItsCompleteTail()
    {
        await using var fixture = new OwnedMonitorFixture();
        var first = new FakeYubiKey("first");
        var second = new FakeYubiKey("second");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([first]);
        var monitor = await fixture.CreateAsync(TestToken);
        var entered = Signal();
        var release = Signal();
        fixture.Finder.Scan = async ct =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(ct);
            return [second];
        };
        fixture.SmartCard.Raise();
        await fixture.AdvanceHotplugAsync();
        await entered.Task.WaitAsync(Bound, TestToken);
        await using var watcher = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await watcher.MoveNextAsync());
        AssertChange<DeviceAdded>(watcher.Current, first);
        release.TrySetResult();
        await fixture.WaitForScanAsync(2, TestToken);
        await monitor.DisposeAsync();
        Assert.True(await watcher.MoveNextAsync());
        Assert.IsType<SnapshotComplete>(watcher.Current);
        Assert.True(await watcher.MoveNextAsync());
        AssertChange<DeviceRemoved>(watcher.Current, first);
        Assert.True(await watcher.MoveNextAsync());
        AssertChange<DeviceAdded>(watcher.Current, second);
        Assert.False(await watcher.MoveNextAsync());
    }

    [Fact]
    public async Task WatchAsync_LargeInitialSnapshot_DoesNotUseTheBoundedDeltaBuffer()
    {
        await using var fixture = new OwnedMonitorFixture();
        IYubiKey[] devices = [.. Enumerable.Range(0, 306).Select(i => new FakeYubiKey($"initial-{i}"))];
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>(devices);
        var monitor = await fixture.CreateAsync(TestToken);
        await using var watcher = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await watcher.MoveNextAsync());
        var initial = new HashSet<IYubiKey> { Assert.IsType<DeviceAdded>(watcher.Current).Device };
        var added = new FakeYubiKey("delta");
        await PublishAsync(fixture, [.. devices, added]);
        await monitor.DisposeAsync();
        for (var i = 1; i < devices.Length; i++)
        {
            Assert.True(await watcher.MoveNextAsync());
            Assert.True(initial.Add(Assert.IsType<DeviceAdded>(watcher.Current).Device));
        }
        Assert.True(initial.SetEquals(devices));
        Assert.True(await watcher.MoveNextAsync());
        Assert.IsType<SnapshotComplete>(watcher.Current);
        Assert.True(await watcher.MoveNextAsync());
        AssertChange<DeviceAdded>(watcher.Current, added);
        Assert.False(await watcher.MoveNextAsync());
    }

    [Fact]
    public async Task WatchAsync_ChangedConnections_RemovesTheReplayedObjectBeforeAddingItsReplacement()
    {
        await using var fixture = new OwnedMonitorFixture();
        var first = new FakeYubiKey("same-interface", ConnectionType.SmartCard);
        var replacement = new FakeYubiKey("same-interface", ConnectionType.SmartCard | ConnectionType.HidFido);
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([first]);
        var monitor = await fixture.CreateAsync(TestToken);
        await using var watcher = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await watcher.MoveNextAsync());
        AssertChange<DeviceAdded>(watcher.Current, first);
        await PublishAsync(fixture, [replacement]);
        await monitor.DisposeAsync();
        Assert.True(await watcher.MoveNextAsync());
        Assert.IsType<SnapshotComplete>(watcher.Current);
        Assert.True(await watcher.MoveNextAsync());
        AssertChange<DeviceRemoved>(watcher.Current, first);
        Assert.True(await watcher.MoveNextAsync());
        AssertChange<DeviceAdded>(watcher.Current, replacement);
        Assert.False(await watcher.MoveNextAsync());
    }

    [Fact]
    public async Task WatchAsync_CancelingDuringInitialReplay_UnsubscribesOnlyThatEnumeration()
    {
        await using var fixture = new OwnedMonitorFixture();
        IYubiKey[] devices = [new FakeYubiKey("first"), new FakeYubiKey("second")];
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>(devices);
        var monitor = await fixture.CreateAsync(TestToken);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        await using var canceled = monitor.WatchAsync(cancellation.Token).GetAsyncEnumerator(cancellation.Token);
        await using var healthy = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await canceled.MoveNextAsync());
        Assert.True(await healthy.MoveNextAsync());
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled.MoveNextAsync().AsTask());
        var added = new FakeYubiKey("after-cancellation");
        await PublishAsync(fixture, [.. devices, added]);
        Assert.True(await healthy.MoveNextAsync());
        Assert.True(await healthy.MoveNextAsync());
        Assert.IsType<SnapshotComplete>(healthy.Current);
        Assert.True(await healthy.MoveNextAsync());
        AssertChange<DeviceAdded>(healthy.Current, added);
    }

    [Fact]
    public async Task WatchAsync_OverflowDuringReplay_IsIsolated_AndSameMonitorCanRebuildProjection()
    {
        await using var fixture = new OwnedMonitorFixture();
        var first = new FakeYubiKey("first");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([first, new FakeYubiKey("second")]);
        var monitor = await fixture.CreateAsync(TestToken);
        await using var stalled = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await stalled.MoveNextAsync());
        await using var healthy = await DeviceChangeWatcher.StartAsync(monitor.WatchAsync, TestToken);
        await healthy.WaitForCountAsync(2, "healthy initial replay missing", TestToken);
        for (var i = 0; i <= 128; i++)
        {
            await PublishAsync(fixture, [first, new FakeYubiKey($"replacement-{i}")]);
            await healthy.WaitForCountAsync(2 + (2 * (i + 1)), "healthy subscriber lost a delta", TestToken);
        }
        var overflow = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            while (await stalled.MoveNextAsync()) { }
        });
        Assert.Contains("clear", overflow.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("same monitor", overflow.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(healthy.Completion.IsCompleted);
        var snapshot = await DeviceChangeWatcher.ReadSnapshotAsync(monitor.WatchAsync, TestToken);
        Assert.Equal(2, snapshot.Count);
        Assert.Contains(first, snapshot);
        var added = new FakeYubiKey("after-recovery");
        await using var recovered = await DeviceChangeWatcher.StartAsync(monitor.WatchAsync, TestToken);
        await PublishAsync(fixture, [.. snapshot, added]);
        await recovered.WaitForCountAsync(3, "recovered subscription is not live", TestToken);
        AssertChange<DeviceAdded>(recovered.Events[2], added);
    }

    [Fact]
    public async Task WatchAsync_DisposalDrainsCapturedReplayAndPublishedTail_ButUnstartedEnumerationIsEmpty()
    {
        await using var fixture = new OwnedMonitorFixture();
        var first = new FakeYubiKey("first");
        var second = new FakeYubiKey("second");
        var added = new FakeYubiKey("added");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([first, second]);
        var monitor = await fixture.CreateAsync(TestToken);
        await using var started = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        await using var unstarted = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await started.MoveNextAsync());
        var projection = new HashSet<IYubiKey> { Assert.IsType<DeviceAdded>(started.Current).Device };
        // A paused consumer must not prevent the producer or sequential disposal from finishing.
        await PublishAsync(fixture, [second, added]);
        await monitor.DisposeAsync().AsTask().WaitAsync(Bound, TestToken);
        while (await started.MoveNextAsync())
            Apply(projection, started.Current);
        Assert.True(projection.SetEquals([second, added]));
        Assert.False(await unstarted.MoveNextAsync());
        await using var afterDisposal = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.False(await afterDisposal.MoveNextAsync());
    }

    [Fact]
    public async Task CreateAsync_WaitsForFirstLoopPublication_WithoutDuplicateInitialScan()
    {
        await using var fixture = new OwnedMonitorFixture();
        var entered = Signal();
        var release = Signal();
        var device = new FakeYubiKey("initial");
        fixture.Finder.Scan = async ct =>
        {
            Assert.True(fixture.Hid.Started && fixture.SmartCard.Started);
            entered.TrySetResult();
            await release.Task.WaitAsync(ct);
            return [device];
        };
        var creation = fixture.CreateAsync(TestToken);
        await entered.Task.WaitAsync(Bound, TestToken);
        Assert.False(creation.IsCompleted);
        release.TrySetResult();
        var monitor = await creation.WaitAsync(Bound, TestToken);
        Assert.Same(device, Assert.Single(await DeviceChangeWatcher.ReadSnapshotAsync(monitor.WatchAsync, TestToken)));
        Assert.Equal(1, fixture.Finder.ScanCount);
    }

    [Fact]
    public async Task CreateAsync_EmptyScan_IsReadyAndReplayPerformsNoDiscovery()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
        Assert.Empty(await DeviceChangeWatcher.ReadSnapshotAsync(monitor.WatchAsync, TestToken));
        Assert.Equal(1, fixture.Finder.ScanCount);
    }

    [Fact]
    public async Task CreateAsync_PreCanceled_DoesNotStartListenersOrScan()
    {
        await using var fixture = new OwnedMonitorFixture();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.CreateAsync(cancellation.Token));
        Assert.False(fixture.Hid.Started || fixture.SmartCard.Started);
        Assert.Equal(0, fixture.Finder.ScanCount);
    }

    [Fact]
    public async Task CreateAsync_InitialScanFailure_PropagatesAndCleansPartialLifetime()
    {
        await using var fixture = new OwnedMonitorFixture();
        var failure = new IOException("initial enumeration failed");
        var retired = false;
        fixture.Finder.OnRetire = () => retired = true;
        fixture.Finder.Scan = _ => throw failure;
        Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => fixture.CreateAsync(TestToken)));
        Assert.True(fixture.Hid.Disposed && fixture.SmartCard.Disposed);
        Assert.True(retired);
    }

    [Fact]
    public async Task CreateAsync_PublicationFailure_PropagatesAndCleansPartialLifetime()
    {
        await using var fixture = new OwnedMonitorFixture();
        var failure = new IOException("identity access failed during diff");
        var retired = false;
        fixture.Finder.OnRetire = () => retired = true;
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([new ThrowingIdentityKey(failure)]);
        Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => fixture.CreateAsync(TestToken)));
        Assert.True(fixture.Hid.Disposed && fixture.SmartCard.Disposed);
        Assert.True(retired);
    }

    [Fact]
    public async Task CreateAsync_CanceledDuringScan_CancelsScanAndCleansPartialLifetime()
    {
        await using var fixture = new OwnedMonitorFixture();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var entered = Signal();
        var canceled = Signal();
        fixture.Finder.Scan = async ct =>
        {
            entered.TrySetResult();
            try { await Signal().Task.WaitAsync(ct); }
            finally { canceled.TrySetResult(); }
            return [];
        };
        var creation = fixture.CreateAsync(cancellation.Token);
        await entered.Task.WaitAsync(Bound, TestToken);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => creation.WaitAsync(Bound, TestToken));
        Assert.True(canceled.Task.IsCompletedSuccessfully);
        Assert.True(fixture.Hid.Disposed && fixture.SmartCard.Disposed);
    }

    [Fact]
    public async Task CreateAsync_CreationTokenCanceledAfterReturn_DoesNotStopMonitoring()
    {
        await using var fixture = new OwnedMonitorFixture();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var monitor = await fixture.CreateAsync(cancellation.Token);
        await cancellation.CancelAsync();
        var device = new FakeYubiKey("after-creation");
        await PublishAsync(fixture, [device]);
        Assert.Same(device, Assert.Single(await DeviceChangeWatcher.ReadSnapshotAsync(monitor.WatchAsync, TestToken)));
    }

    [Fact]
    public async Task CreateAsync_HintDuringInitialScan_IsInvalidatedAndReconciled()
    {
        await using var fixture = new OwnedMonitorFixture();
        var entered = Signal();
        var release = Signal();
        fixture.Finder.Scan = async ct =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(ct);
            return [new FakeYubiKey("before-hint")];
        };
        var creation = fixture.CreateAsync(TestToken);
        await entered.Task.WaitAsync(Bound, TestToken);
        fixture.SmartCard.Raise();
        var next = new FakeYubiKey("after-hint");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([next]);
        release.TrySetResult();
        var monitor = await creation.WaitAsync(Bound, TestToken);
        await fixture.AdvanceHotplugAsync();
        await fixture.WaitForScanAsync(2, TestToken);
        Assert.Same(next, Assert.Single(await DeviceChangeWatcher.ReadSnapshotAsync(monitor.WatchAsync, TestToken)));
        Assert.Equal(1, fixture.Finder.ActivityCount);
        Assert.Equal(2, fixture.Finder.ScanCount);
    }

    public enum LaterScanFailure { ScanThrows, PublicationThrows, EnumerationTimeout }

    [Theory]
    [InlineData(LaterScanFailure.ScanThrows)]
    [InlineData(LaterScanFailure.PublicationThrows)]
    [InlineData(LaterScanFailure.EnumerationTimeout)]
    [Trait("Category", "RuntimeResilience")]
    public async Task BackgroundScanOrPublicationFailure_RetainsPublishedMembership_AndNextHintRetries(LaterScanFailure kind)
    {
        await using var fixture = new OwnedMonitorFixture();
        var first = new FakeYubiKey("first");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([first]);
        var monitor = await fixture.CreateAsync(TestToken);
        // EnumerationTimeout is what IMonitoredDiscovery raises instead of publishing a partial scan; the
        // loop must treat it exactly like any other later scan failure: keep the snapshot, log, retry.
        Exception failure = kind == LaterScanFailure.EnumerationTimeout
            ? new TimeoutException("HID interface enumeration exceeded its budget; the scan was not published.")
            : new IOException("transient scan or publication failure");
        fixture.Finder.Scan = kind == LaterScanFailure.PublicationThrows
            ? _ => Task.FromResult<IReadOnlyList<IYubiKey>>([new ThrowingIdentityKey(failure)])
            : _ => throw failure;
        fixture.SmartCard.Raise();
        await fixture.AdvanceHotplugAsync();
        // The next fallback timer is armed only after the failed scan has returned to the scheduler.
        await fixture.Clock.WaitForTimerAsync(fixture.Interval);
        Assert.Equal(2, fixture.Finder.ScanCount);
        Assert.Same(first, Assert.Single(await DeviceChangeWatcher.ReadSnapshotAsync(monitor.WatchAsync, TestToken)));
        var second = new FakeYubiKey("second");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([second]);
        fixture.SmartCard.Raise();
        await fixture.Clock.WaitForTimerAsync(RescanScheduler.QuietPeriod);
        fixture.Clock.Advance(RescanScheduler.QuietPeriod);
        // A failed attempt does not advance the publication revision.
        await fixture.WaitForScanAsync(2, TestToken);
        Assert.Same(second, Assert.Single(await DeviceChangeWatcher.ReadSnapshotAsync(monitor.WatchAsync, TestToken)));
        Assert.Equal(3, fixture.Finder.ScanCount);
    }

    [Fact]
    public async Task CreateAsync_OneListenerFails_StillInitializes()
    {
        var finder = new OwnedMonitorFixture.ScriptedFinder();
        var hid = new OwnedMonitorFixture.FakeDeviceChangeListener();
        await using var monitor = await YubiKeyMonitor.CreateAsync(finder,
            [(ConnectionType.Hid, () => hid), (ConnectionType.SmartCard, () => throw new IOException("unavailable"))],
            TimeSpan.FromHours(1), new ObservedTimeProvider(), TestToken);
        Assert.True(hid.Started);
        Assert.Empty(await DeviceChangeWatcher.ReadSnapshotAsync(monitor.WatchAsync, TestToken));
        Assert.Equal(1, finder.ScanCount);
    }

    [Fact]
    public async Task IntervalScan_UsesFakeClock_AndDoesNotInvalidateEvidence()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
        var device = new FakeYubiKey("fallback");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([device]);
        await fixture.Clock.WaitForTimerAsync(fixture.Interval);
        fixture.Clock.Advance(fixture.Interval - TimeSpan.FromTicks(1));
        Assert.Equal(1, fixture.Finder.ScanCount);
        fixture.Clock.Advance(TimeSpan.FromTicks(1));
        await fixture.WaitForScanAsync(2, TestToken);
        Assert.Same(device, Assert.Single(await DeviceChangeWatcher.ReadSnapshotAsync(monitor.WatchAsync, TestToken)));
        Assert.Equal(0, fixture.Finder.ActivityCount);
    }

    [Fact]
    public async Task HidHint_WithoutDiff_RescansAndInvalidatesButEmitsNoChange()
    {
        await using var fixture = new OwnedMonitorFixture();
        var device = new FakeYubiKey("retained");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([device]);
        var monitor = await fixture.CreateAsync(TestToken);
        await using var watcher = await DeviceChangeWatcher.StartAsync(monitor.WatchAsync, TestToken);
        await watcher.WaitForCountAsync(1, "missing replay", TestToken);
        fixture.Finder.OnActivity = transport => Assert.Equal(ConnectionType.Hid, transport);
        fixture.Finder.Scan = _ =>
        {
            Assert.Equal(1, fixture.Finder.ActivityCount);
            return Task.FromResult<IReadOnlyList<IYubiKey>>([device]);
        };
        fixture.Hid.Raise();
        await fixture.AdvanceHotplugAsync();
        await fixture.WaitForScanAsync(2, TestToken);
        await monitor.DisposeAsync();
        await watcher.Completion.WaitAsync(Bound, TestToken);
        AssertChange<DeviceAdded>(Assert.Single(watcher.Events), device);
    }

    [Fact]
    public async Task EventStorm_DuringBlockedScan_ProducesExactlyOneSequentialFollowUpScan()
    {
        await using var fixture = new OwnedMonitorFixture();
        var first = new FakeYubiKey("first");
        var second = new FakeYubiKey("second");
        var third = new FakeYubiKey("third");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([first]);
        var monitor = await fixture.CreateAsync(TestToken);
        await using var watcher = await DeviceChangeWatcher.StartAsync(monitor.WatchAsync, TestToken);
        await watcher.WaitForCountAsync(1, "missing initial replay", TestToken);
        var entered = Signal();
        var release = Signal();
        fixture.Finder.Scan = async ct =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(ct);
            return [second];
        };
        fixture.SmartCard.Raise();
        await fixture.AdvanceHotplugAsync();
        await entered.Task.WaitAsync(Bound, TestToken);
        await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            fixture.Hid.Raise();
            fixture.SmartCard.Raise();
        }, TestToken)));
        fixture.Clock.Advance(RescanScheduler.MaxCoalesce);
        Assert.Equal(2, fixture.Finder.ScanCount);
        Assert.Same(first, Assert.Single(await DeviceChangeWatcher.ReadSnapshotAsync(monitor.WatchAsync, TestToken)));
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([third]);
        release.TrySetResult();
        await fixture.AdvanceHotplugAsync();
        await fixture.WaitForScanAsync(3, TestToken);
        await monitor.DisposeAsync();
        await watcher.Completion.WaitAsync(Bound, TestToken);
        Assert.Equal(3, fixture.Finder.ScanCount);
        Assert.Collection(watcher.Events,
            c => AssertChange<DeviceAdded>(c, first),
            c => AssertChange<DeviceRemoved>(c, first),
            c => AssertChange<DeviceAdded>(c, second),
            c => AssertChange<DeviceRemoved>(c, second),
            c => AssertChange<DeviceAdded>(c, third));
    }

    [Fact]
    public async Task Monitors_AreIndependent_DisposingOneLeavesTheOtherLive()
    {
        await using var first = new OwnedMonitorFixture();
        await using var second = new OwnedMonitorFixture();
        var firstMonitor = await first.CreateAsync(TestToken);
        var secondMonitor = await second.CreateAsync(TestToken);
        await using var firstWatcher = await DeviceChangeWatcher.StartAsync(firstMonitor.WatchAsync, TestToken);
        await using var secondWatcher = await DeviceChangeWatcher.StartAsync(secondMonitor.WatchAsync, TestToken);
        await firstMonitor.DisposeAsync();
        await firstWatcher.Completion.WaitAsync(Bound, TestToken);
        var device = new FakeYubiKey("survivor");
        await PublishAsync(second, [device]);
        await secondWatcher.WaitForCountAsync(1, "other monitor stopped", TestToken);
        AssertChange<DeviceAdded>(Assert.Single(secondWatcher.Events), device);
        Assert.False(secondWatcher.Completion.IsCompleted);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task TerminalLoopFailure_FaultsActiveAndFutureWatchers()
    {
        await using var fixture = new OwnedMonitorFixture();
        var first = new FakeYubiKey("first");
        var second = new FakeYubiKey("second");
        var added = new FakeYubiKey("added");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([first, second]);
        var monitor = await fixture.CreateAsync(TestToken);
        await using var active = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await active.MoveNextAsync());
        var projection = new HashSet<IYubiKey> { Assert.IsType<DeviceAdded>(active.Current).Device };
        await PublishAsync(fixture, [second, added]);
        var failure = new IOException("invalidation failed");
        fixture.Finder.OnActivity = _ => throw failure;
        fixture.SmartCard.Raise();
        await fixture.AdvanceHotplugAsync();
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            while (await active.MoveNextAsync().AsTask().WaitAsync(Bound, TestToken))
                Apply(projection, active.Current);
        });
        Assert.Same(failure, actual.InnerException);
        Assert.True(projection.SetEquals([second, added]));
        await using var future = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        var futureError = await Assert.ThrowsAsync<InvalidOperationException>(() => future.MoveNextAsync().AsTask());
        Assert.Same(failure, futureError.InnerException);
        await monitor.DisposeAsync();
        await using var afterDisposal = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.False(await afterDisposal.MoveNextAsync());
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task DisposeAsync_CooperativeScansAcrossDistinctMonitors_DoNotAccumulateLoops()
    {
        for (var cycle = 0; cycle < 3; cycle++)
        {
            await using var fixture = new OwnedMonitorFixture();
            var monitor = await fixture.CreateAsync(TestToken);
            var entered = Signal();
            var canceled = Signal();
            fixture.Finder.Scan = async ct =>
            {
                entered.TrySetResult();
                try { await Signal().Task.WaitAsync(ct); }
                finally { canceled.TrySetResult(); }
                return [];
            };
            fixture.SmartCard.Raise();
            await fixture.AdvanceHotplugAsync();
            await entered.Task.WaitAsync(Bound, TestToken);
            await monitor.DisposeAsync().AsTask().WaitAsync(Bound, TestToken);
            Assert.True(canceled.Task.IsCompletedSuccessfully);
            Assert.Equal(2, fixture.Finder.ScanCount);
        }
    }

    [Fact]
    public async Task DisposeAsync_WhileSubscribersAreMidEnumeration_EndsThemNormally()
    {
        await using var fixture = new OwnedMonitorFixture();
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([new FakeYubiKey("first")]);
        var monitor = await fixture.CreateAsync(TestToken);

        // Two live subscribers parked in MoveNextAsync after their replay, as hosted consumers would be.
        await using var a = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        await using var b = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        foreach (var e in new[] { a, b })
        {
            Assert.True(await e.MoveNextAsync());
            Assert.IsType<DeviceAdded>(e.Current);
            Assert.True(await e.MoveNextAsync());
            Assert.IsType<SnapshotComplete>(e.Current);
        }
        var pendingA = a.MoveNextAsync().AsTask();
        var pendingB = b.MoveNextAsync().AsTask();
        Assert.False(pendingA.IsCompleted || pendingB.IsCompleted);

        // Disposal overlapping active enumerations is the documented, supported overlap.
        await monitor.DisposeAsync().AsTask().WaitAsync(Bound, TestToken);

        Assert.False(await pendingA.WaitAsync(Bound, TestToken));
        Assert.False(await pendingB.WaitAsync(Bound, TestToken));
        await using var late = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.False(await late.MoveNextAsync());
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task DisposeAsync_WaitsForScanExit_ThenRetiresAndCompletesFeed_AndIsIdempotent()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
        await using var watcher = await DeviceChangeWatcher.StartAsync(monitor.WatchAsync, TestToken);
        var entered = Signal();
        var canceled = Signal();
        var release = Signal();
        var exited = false;
        var retireCount = 0;
        var callback = fixture.Hid.Changed;
        Assert.NotNull(callback);
        fixture.Finder.Scan = async ct =>
        {
            entered.TrySetResult();
            try { await Signal().Task.WaitAsync(ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                Assert.True(fixture.Hid.Disposed && fixture.SmartCard.Disposed);
                canceled.TrySetResult();
                // Deliberately hold cooperative scan cleanup; disposal must await it, not abandon it.
                await release.Task.WaitAsync(TestToken);
            }
            exited = true;
            return [];
        };
        fixture.Finder.OnRetire = () =>
        {
            Assert.True(exited);
            Assert.True(fixture.Hid.Disposed && fixture.SmartCard.Disposed);
            retireCount++;
        };
        fixture.SmartCard.Raise();
        await fixture.AdvanceHotplugAsync();
        await entered.Task.WaitAsync(Bound, TestToken);
        var disposal = monitor.DisposeAsync().AsTask();
        try
        {
            await canceled.Task.WaitAsync(Bound, TestToken);
            Assert.False(disposal.IsCompleted);
            Assert.False(watcher.Completion.IsCompleted);
            Assert.Equal(0, retireCount);
        }
        finally
        {
            release.TrySetResult();
            await disposal.WaitAsync(Bound, CancellationToken.None);
        }
        await watcher.Completion.WaitAsync(Bound, TestToken);
        Assert.True(watcher.EndedNormally);
        await monitor.DisposeAsync();
        Assert.Equal(1, retireCount);
        Assert.Null(fixture.Hid.Changed);
        Assert.Null(fixture.SmartCard.Changed);
        callback();
        fixture.Clock.Advance(fixture.Interval * 2);
        Assert.Equal(2, fixture.Finder.ScanCount);
        Assert.Equal(1, fixture.Finder.ActivityCount);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task CreateAsync_CancellationRetainsAdmittedReadLeaseUntilNativeTeardown()
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestToken);
        var rig = new FindYubiKeysScopeTests.ScopeRig { PendingOpen = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var creation = YubiKeyMonitor.CreateAsync(rig.CreateFinder(ConnectionType.SmartCard),
            [(ConnectionType.Hid, () => new OwnedMonitorFixture.FakeDeviceChangeListener())],
            TimeSpan.FromHours(1), new ObservedTimeProvider(), cancellation.Token);
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
        }
        finally
        {
            await cancellation.CancelAsync();
            rig.PendingOpen.TrySetException(new InvalidOperationException("Test cleanup"));
            try
            {
                var monitor = await creation.WaitAsync(Bound, CancellationToken.None);
                await monitor.DisposeAsync();
            }
            catch (OperationCanceledException) when (creation.IsCanceled) { }
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(CancellationToken.None);
        }
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task DisposeAsync_RetiresQueuedBuiltInFinderReadsBeforeWorkerCapacityReturns()
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestToken);
        var rig = new FindYubiKeysScopeTests.ScopeRig { IncludeSecondKey = true };
        var finder = rig.CreateFinder(ConnectionType.SmartCard);
        using (await DiscoveryWorkerAdmissionCollection.SaturateAsync(TestToken))
        {
            await using var monitor = await YubiKeyMonitor.CreateAsync(finder,
                [(ConnectionType.Hid, () => new OwnedMonitorFixture.FakeDeviceChangeListener())],
                TimeSpan.FromHours(1), new ObservedTimeProvider(), TestToken);
            Assert.Equal(2, (await DeviceChangeWatcher.ReadSnapshotAsync(monitor.WatchAsync, TestToken)).Count);
            await monitor.DisposeAsync();
            await finder.FindAllAsync(ConnectionType.All, TestToken).WaitAsync(TimeSpan.FromSeconds(1), TestToken);
            Assert.Empty(rig.OpenCalls);
        }
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestToken);
        Assert.Empty(rig.OpenCalls);
    }

    private static async Task PublishAsync(OwnedMonitorFixture fixture, IReadOnlyList<IYubiKey> devices)
    {
        fixture.Finder.Scan = _ => Task.FromResult(devices);
        await fixture.TriggerAsync(TestToken);
    }

    private static void AssertChange<TChange>(DeviceChange change, IYubiKey device) where TChange : DeviceChange
    {
        Assert.IsType<TChange>(change);
        Assert.Same(device, change switch
        {
            DeviceAdded added => added.Device,
            DeviceRemoved removed => removed.Device,
            DeviceUpdated updated => updated.Device,
            _ => throw new InvalidOperationException("Expected a device change.")
        });
    }

    private static void Apply(HashSet<IYubiKey> projection, DeviceChange change) => Assert.True(change switch
    {
        DeviceAdded added => projection.Add(added.Device),
        DeviceRemoved removed => projection.Remove(removed.Device),
        _ => true
    });

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class ThrowingIdentityKey(Exception failure) : IYubiKey
    {
        public string DeviceId => throw failure;
        public ConnectionType AvailableConnections => ConnectionType.SmartCard;
        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection => throw new NotSupportedException();
    }
}