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
    public async Task WatchAsync_IsLazy_AndEachEnumerationReplaysItsOwnSnapshotThenOrderedDeltas()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
        var first = new FakeYubiKey("first");
        var second = new FakeYubiKey("second");
        var third = new FakeYubiKey("third");
        fixture.Repository.UpdateCache([first]);
        var stream = monitor.WatchAsync(TestToken);
        await using var early = stream.GetAsyncEnumerator(TestToken);
        Assert.Equal(0, fixture.Repository.WatcherCount);

        fixture.Repository.UpdateCache([second]);
        var initial = early.MoveNextAsync().AsTask();
        fixture.Repository.UpdateCache([third]);
        await using var late = stream.GetAsyncEnumerator(TestToken);
        var lateInitial = late.MoveNextAsync().AsTask();
        await monitor.DisposeAsync();

        Assert.True(await initial.WaitAsync(Bound, TestToken));
        AssertChange(early.Current, DeviceAction.Added, second);
        Assert.True(await early.MoveNextAsync());
        AssertChange(early.Current, DeviceAction.Removed, second);
        Assert.True(await early.MoveNextAsync());
        AssertChange(early.Current, DeviceAction.Added, third);
        Assert.False(await early.MoveNextAsync());
        Assert.True(await lateInitial.WaitAsync(Bound, TestToken));
        AssertChange(late.Current, DeviceAction.Added, third);
        Assert.False(await late.MoveNextAsync());
        Assert.Equal(1, fixture.Finder.ScanCount);
    }

    [Fact]
    public async Task WatchAsync_DuringParkedDiff_CapturesPreviousPublicationAndItsCompleteTail()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
        using var release = new ManualResetEventSlim();
        var removed = new BlockingIdentityKey();
        var added = new FakeYubiKey("added");
        fixture.Repository.UpdateCache([removed]);
        var diffReached = Signal();
        removed.OnIdRead = () =>
        {
            diffReached.TrySetResult();
            Assert.True(release.Wait(Bound, TestToken));
        };
        var publication = Task.Run(() => fixture.Repository.UpdateCache([added]), TestToken);
        try
        {
            await diffReached.Task.WaitAsync(Bound, TestToken);
            await using var watcher = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
            var initial = watcher.MoveNextAsync();
            // Snapshot capture must not wait for a diff parked in a device getter/logger.
            Assert.True(initial.IsCompletedSuccessfully);
            Assert.True(await initial);
            AssertChange(watcher.Current, DeviceAction.Added, removed);
            Assert.Equal(1, fixture.Repository.WatcherCount);
            Assert.Same(removed, Assert.Single(monitor.CurrentDevices));
            release.Set();
            await publication.WaitAsync(Bound, TestToken);
            await monitor.DisposeAsync();

            Assert.True(await watcher.MoveNextAsync());
            AssertChange(watcher.Current, DeviceAction.Removed, removed);
            Assert.True(await watcher.MoveNextAsync());
            AssertChange(watcher.Current, DeviceAction.Added, added);
            Assert.False(await watcher.MoveNextAsync());
        }
        finally
        {
            release.Set();
            // The diff still owns the fake's gate even if an assertion or runner cancellation fires.
            await publication.WaitAsync(Bound, CancellationToken.None);
        }
    }

    [Fact]
    public async Task WatchAsync_LargeInitialSnapshot_DoesNotUseTheBoundedDeltaBuffer()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
        IYubiKey[] devices = [.. Enumerable.Range(0, DeviceEventHub.WatcherBufferCapacity + 50)
            .Select(i => new FakeYubiKey($"initial-{i}"))];
        fixture.Repository.UpdateCache(devices);
        await using var watcher = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await watcher.MoveNextAsync());
        var initial = new HashSet<IYubiKey> { watcher.Current.Device };
        Assert.Equal(DeviceAction.Added, watcher.Current.Action);
        // A later publication queues a delta while the consumer is still replaying initial membership.
        var added = new FakeYubiKey("delta");
        fixture.Repository.UpdateCache([.. devices, added]);
        await monitor.DisposeAsync();

        for (var i = 1; i < devices.Length; i++)
        {
            Assert.True(await watcher.MoveNextAsync());
            Assert.Equal(DeviceAction.Added, watcher.Current.Action);
            Assert.True(initial.Add(watcher.Current.Device), "duplicate initial device");
        }
        Assert.True(initial.SetEquals(devices));
        Assert.True(await watcher.MoveNextAsync());
        AssertChange(watcher.Current, DeviceAction.Added, added);
        Assert.False(await watcher.MoveNextAsync());
    }

    [Fact]
    public async Task WatchAsync_ChangedConnections_RemovesTheReplayedObjectBeforeAddingItsReplacement()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
        var first = new FakeYubiKey("same-interface", ConnectionType.SmartCard);
        var replacement = new FakeYubiKey("same-interface", ConnectionType.SmartCard | ConnectionType.HidFido);
        fixture.Repository.UpdateCache([first]);
        await using var watcher = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await watcher.MoveNextAsync());
        AssertChange(watcher.Current, DeviceAction.Added, first);
        fixture.Repository.UpdateCache([replacement]);
        Assert.Same(replacement, Assert.Single(monitor.CurrentDevices));
        await monitor.DisposeAsync();

        Assert.True(await watcher.MoveNextAsync());
        AssertChange(watcher.Current, DeviceAction.Removed, first);
        Assert.True(await watcher.MoveNextAsync());
        AssertChange(watcher.Current, DeviceAction.Added, replacement);
        Assert.False(await watcher.MoveNextAsync());
    }

    [Fact]
    public async Task WatchAsync_CancelingDuringInitialReplay_UnsubscribesOnlyThatEnumeration()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
        fixture.Repository.UpdateCache([new FakeYubiKey("first"), new FakeYubiKey("second")]);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        await using var canceled = monitor.WatchAsync(cancellation.Token).GetAsyncEnumerator(cancellation.Token);
        await using var healthy = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await canceled.MoveNextAsync());
        Assert.True(await healthy.MoveNextAsync());
        Assert.Equal(2, fixture.Repository.WatcherCount);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled.MoveNextAsync().AsTask());
        Assert.Equal(1, fixture.Repository.WatcherCount);
        var added = new FakeYubiKey("after-cancellation");
        IReadOnlyList<IYubiKey> next = [.. monitor.CurrentDevices, added];
        fixture.Finder.Scan = _ => Task.FromResult(next);
        fixture.SmartCard.Raise();
        await fixture.WaitForScanAsync(2, TestToken);
        Assert.True(await healthy.MoveNextAsync());
        Assert.True(await healthy.MoveNextAsync());
        AssertChange(healthy.Current, DeviceAction.Added, added);
    }

    [Fact]
    public async Task WatchAsync_OverflowDuringReplay_IsIsolated_AndSameMonitorCanRebuildProjection()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
        var first = new FakeYubiKey("first");
        fixture.Repository.UpdateCache([first, new FakeYubiKey("second")]);
        var stream = monitor.WatchAsync(TestToken);
        await using var stalled = stream.GetAsyncEnumerator(TestToken);
        Assert.True(await stalled.MoveNextAsync());
        await using var healthy = await DeviceEventWatcher.StartAsync(
            monitor.WatchAsync, () => fixture.Repository.WatcherCount, TestToken);
        await healthy.WaitForCountAsync(2, "healthy initial replay missing", TestToken);

        var projection = new HashSet<IYubiKey> { stalled.Current.Device };
        for (var i = 0; i <= DeviceEventHub.WatcherBufferCapacity / 2; i++)
        {
            fixture.Repository.UpdateCache([first, new FakeYubiKey($"replacement-{i}")]);
            await healthy.WaitForCountAsync(2 + (2 * (i + 1)), "healthy subscriber lost a delta", TestToken);
        }

        var overflow = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            while (await stalled.MoveNextAsync())
                Apply(projection, stalled.Current);
        });
        Assert.Contains("clear", overflow.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("same monitor", overflow.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FindAllAsync", overflow.Message, StringComparison.Ordinal);
        Assert.Equal(1, fixture.Repository.WatcherCount);
        Assert.False(healthy.Completion.IsCompleted);

        // Recovery deliberately discards stale state and reuses the same monitor, not one-shot discovery.
        projection.Clear();
        await using var recovered = stream.GetAsyncEnumerator(TestToken);
        Assert.True(await recovered.MoveNextAsync());
        Apply(projection, recovered.Current);
        var expected = monitor.CurrentDevices;
        var afterRecovery = new FakeYubiKey("after-recovery");
        fixture.Repository.UpdateCache([.. expected, afterRecovery]);
        await monitor.DisposeAsync();
        while (await recovered.MoveNextAsync())
            Apply(projection, recovered.Current);
        Assert.True(projection.SetEquals([.. expected, afterRecovery]));
        await healthy.Completion.WaitAsync(Bound, TestToken);
        Assert.Equal(1, fixture.Finder.ScanCount);
    }

    [Fact]
    public async Task WatchAsync_DisposalDrainsCapturedReplayAndPublishedTail_ButUnstartedEnumerationIsEmpty()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
        var first = new FakeYubiKey("first");
        var second = new FakeYubiKey("second");
        var added = new FakeYubiKey("added");
        fixture.Repository.UpdateCache([first, second]);
        var stream = monitor.WatchAsync(TestToken);
        await using var started = stream.GetAsyncEnumerator(TestToken);
        await using var unstarted = stream.GetAsyncEnumerator(TestToken);
        Assert.True(await started.MoveNextAsync());
        var projection = new HashSet<IYubiKey> { started.Current.Device };
        fixture.Repository.UpdateCache([second, added]);
        await monitor.DisposeAsync();

        while (await started.MoveNextAsync())
            Apply(projection, started.Current);
        Assert.True(projection.SetEquals([second, added]));
        Assert.False(await unstarted.MoveNextAsync());
        await using var afterDisposal = stream.GetAsyncEnumerator(TestToken);
        Assert.False(await afterDisposal.MoveNextAsync());
        Assert.Equal(0, fixture.Repository.WatcherCount);
    }

    [Fact]
    public async Task WatchAsync_PausedReplay_DoesNotHoldThePublicationOrDisposalBoundary()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
        var first = new FakeYubiKey("first");
        var second = new FakeYubiKey("second");
        var added = new FakeYubiKey("added");
        fixture.Repository.UpdateCache([first, second]);
        await using var watcher = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await watcher.MoveNextAsync());

        // A different thread must be able to publish/dispose while the consumer holds its first item.
        await Task.Run(() => fixture.Repository.UpdateCache([second, added]), TestToken).WaitAsync(Bound, TestToken);
        await Task.Run(async () => await monitor.DisposeAsync(), TestToken).WaitAsync(Bound, TestToken);
        var projection = new HashSet<IYubiKey> { watcher.Current.Device };
        while (await watcher.MoveNextAsync())
            Apply(projection, watcher.Current);
        Assert.True(projection.SetEquals([second, added]));
    }

    [Fact]
    public async Task WatchAsync_StartedDuringMonitorTeardown_DoesNotReplayTheStillRetainedRepository()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
        fixture.Repository.UpdateCache([new FakeYubiKey("historical")]);
        var entered = Signal();
        var release = Signal();
        monitor.RepositoryTeardownReachedForTest = async () =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(TestToken);
        };
        var disposal = monitor.DisposeAsync().AsTask();
        try
        {
            await entered.Task.WaitAsync(Bound, TestToken);
            Assert.Single(fixture.Repository.GetAll());
            await using var watcher = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
            Assert.False(await watcher.MoveNextAsync());
            Assert.Equal(0, fixture.Repository.WatcherCount);
        }
        finally
        {
            release.TrySetResult();
            await disposal.WaitAsync(Bound, TestToken);
        }
    }

    [Fact]
    public async Task WatchAsync_TerminalFailure_DrainsCapturedHistoryButFutureEnumerationDoesNotReplay()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
        var first = new FakeYubiKey("first");
        var second = new FakeYubiKey("second");
        var added = new FakeYubiKey("added");
        fixture.Repository.UpdateCache([first, second]);
        await using var active = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await active.MoveNextAsync());
        var projection = new HashSet<IYubiKey> { active.Current.Device };
        fixture.Repository.UpdateCache([second, added]);
        var failure = new IOException("terminal failure");
        fixture.Repository.Fail(failure);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            while (await active.MoveNextAsync())
                Apply(projection, active.Current);
        });
        Assert.Same(failure, actual.InnerException);
        Assert.True(projection.SetEquals([second, added]));
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => monitor.CurrentDevices).InnerException);
        await using var future = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        var futureError = await Assert.ThrowsAsync<InvalidOperationException>(() => future.MoveNextAsync().AsTask());
        Assert.Same(failure, futureError.InnerException);
        await monitor.DisposeAsync();
        await using var afterDisposal = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.False(await afterDisposal.MoveNextAsync());
    }

    private static void AssertChange(DeviceEvent change, DeviceAction action, IYubiKey device)
    {
        Assert.Equal(action, change.Action);
        Assert.Same(device, change.Device);
    }

    private static void Apply(HashSet<IYubiKey> projection, DeviceEvent change)
    {
        Assert.True(change.Action == DeviceAction.Added
            ? projection.Add(change.Device)
            : projection.Remove(change.Device), "duplicate arrival or unpaired removal");
    }

    private sealed class BlockingIdentityKey : IYubiKey
    {
        public Action? OnIdRead { get; set; }
        public string DeviceId
        {
            get
            {
                OnIdRead?.Invoke();
                return "removed";
            }
        }
        public ConnectionType AvailableConnections => ConnectionType.SmartCard;
        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection => throw new NotSupportedException();
    }

    [Fact]
    public async Task CreateAsync_WaitsForFirstLoopPublication_WithoutDuplicateInitialScan()
    {
        await using var fixture = new OwnedMonitorFixture();
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
        fixture.Worker.PublishAdmittedForTest = async () =>
        {
            publicationEntered.TrySetResult();
            await releasePublication.Task.WaitAsync(TestToken);
        };
        fixture.WaitReached = () => waiting.TrySetResult();

        var creation = fixture.CreateAsync(TestToken);
        try
        {
            await scanEntered.Task.WaitAsync(Bound, TestToken);
            Assert.False(creation.IsCompleted);
            releaseScan.TrySetResult();
            await publicationEntered.Task.WaitAsync(Bound, TestToken);
            Assert.False(creation.IsCompleted);
            releasePublication.TrySetResult();

            var monitor = await creation.WaitAsync(Bound, TestToken);
            await waiting.Task.WaitAsync(Bound, TestToken);
            Assert.Same(device, Assert.Single(monitor.CurrentDevices));
            Assert.Null(((IYubiKey)device).CachedDeviceInfo);
            Assert.Equal(1, fixture.Finder.ScanCount);
        }
        finally
        {
            releaseScan.TrySetResult();
            releasePublication.TrySetResult();
        }
    }

    [Fact]
    public async Task CreateAsync_EmptyScan_IsReadyAndCachedReadsPerformNoDiscovery()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);

        Assert.True(fixture.Repository.HasData);
        for (var i = 0; i < 100; i++)
            Assert.Empty(monitor.CurrentDevices);
        Assert.Equal(1, fixture.Finder.ScanCount);
    }

    [Fact]
    public async Task CurrentDevices_PopulatedSnapshot_PerformsNoBackendEnumerationProbesOrOpens()
    {
        var rig = new FindYubiKeysScopeTests.ScopeRig { IncludeSecondKey = true };
        var repository = new YubiKeyDeviceRepository();
        var worker = new YubiKeyMonitorWorker(
            repository, rig.CreateFinder(ConnectionType.All),
            () => new OwnedMonitorFixture.FakeHidListener(), () => new OwnedMonitorFixture.FakeSmartCardListener(), TimeSpan.FromMilliseconds(50));
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker, TestToken, TimeSpan.FromHours(1));
        var snapshot = monitor.CurrentDevices;
        Assert.Equal(2, snapshot.Count);
        Assert.All(snapshot, key => Assert.NotNull(key.CachedDeviceInfo));
        var before = (rig.Pcsc.Calls, rig.Hid.Calls, rig.CreatedSlots.Count, rig.TopologyCalls.Count,
            rig.OpenCalls.Values.Sum(), rig.Connections.Count);
        Assert.True(before.Item1 > 0 && before.Item2 > 0 && before.Item5 > 0);

        // Poison the actual finder backends after initialization. This is a call-count proof, not
        // a timing assertion about reads finishing before a background scan happens to run.
        rig.Pcsc.PendingEnumeration = Task.FromResult<IReadOnlyList<IPcscDevice>>([]);
        rig.Hid.Devices = [];
        rig.OpenConnection = _ => throw new InvalidOperationException("Cached access must not open hardware.");
        for (var i = 0; i < 100; i++)
        {
            Assert.Same(snapshot, monitor.CurrentDevices);
            Assert.All(monitor.CurrentDevices, key => Assert.NotNull(key.CachedDeviceInfo));
        }

        Assert.Equal(before, (rig.Pcsc.Calls, rig.Hid.Calls, rig.CreatedSlots.Count, rig.TopologyCalls.Count,
            rig.OpenCalls.Values.Sum(), rig.Connections.Count));
    }

    [Fact]
    public async Task CreateAsync_PreCanceled_CleansOwnershipWithoutStartingListenersOrScanning()
    {
        await using var fixture = new OwnedMonitorFixture();
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
        await using var fixture = new OwnedMonitorFixture();
        var failure = new IOException("initial enumeration failed");
        fixture.Finder.Scan = _ => throw failure;

        var actual = await Assert.ThrowsAsync<IOException>(() => fixture.CreateAsync(TestToken));

        Assert.Same(failure, actual);
        Assert.True(fixture.Hid.Disposed && fixture.SmartCard.Disposed);
        Assert.True(fixture.Loop.IsCompletedSuccessfully);
        Assert.False(fixture.Repository.HasData);
        Assert.Throws<ObjectDisposedException>(() => fixture.Repository.GetAll());
    }

    [Fact]
    public async Task CreateAsync_PublicationFailure_DoesNotReturnAnInitializedEmptyMonitor()
    {
        await using var fixture = new OwnedMonitorFixture();
        var failure = new IOException("publication failed");
        fixture.Worker.PublishAdmittedForTest = () => throw failure;

        Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => fixture.CreateAsync(TestToken)));
        Assert.True(fixture.Hid.Disposed && fixture.SmartCard.Disposed);
        Assert.False(fixture.Repository.HasData);
    }

    [Fact]
    public async Task CreateAsync_CanceledDuringNativeScan_CleansUpAndRejectsLatePublication()
    {
        await using var fixture = new OwnedMonitorFixture();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var entered = Signal();
        var release = Signal();
        fixture.Finder.Scan = async _ =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(TestToken); // Simulates native work ignoring lifetime cancellation.
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
            await fixture.Loop.WaitAsync(Bound, TestToken);
            Assert.False(fixture.Repository.HasData);
        }
        finally
        {
            release.TrySetResult();
            // The real loop still owns the fake after bounded factory cancellation.
            await fixture.Loop.WaitAsync(Bound, CancellationToken.None);
        }
    }

    [Fact]
    public async Task CreateAsync_CreationTokenCanceledAfterReturn_DoesNotStopMonitoring()
    {
        await using var fixture = new OwnedMonitorFixture();
        var updated = Signal();
        fixture.WaitReached = () =>
        {
            if (fixture.Finder.ScanCount == 2)
                updated.TrySetResult();
        };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var monitor = await fixture.CreateAsync(cancellation.Token);
        await using var watcher = await DeviceEventWatcher.StartAsync(
            monitor.WatchAsync, () => fixture.Repository.WatcherCount, TestToken);
        await cancellation.CancelAsync();

        var device = new FakeYubiKey("after-creation");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([device]);
        fixture.SmartCard.Raise();
        await watcher.WaitForCountAsync(1, "creation cancellation stopped the monitor", TestToken);
        await updated.Task.WaitAsync(Bound, TestToken);

        Assert.Same(device, Assert.Single(monitor.CurrentDevices));
    }

    [Fact]
    public async Task CreateAsync_HintDuringInitialScan_IsInvalidatedAndReconciled()
    {
        await using var fixture = new OwnedMonitorFixture();
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
            var monitor = await creation.WaitAsync(Bound, TestToken);
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
        await using var fixture = new OwnedMonitorFixture();
        var first = new FakeYubiKey("first");
        var source = new List<IYubiKey> { first };
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>(source);
        var monitor = await fixture.CreateAsync(TestToken);
        var snapshot = monitor.CurrentDevices;
        source.Clear();

        Assert.Same(first, Assert.Single(snapshot));
        if (snapshot is IList<IYubiKey> mutable)
            Assert.Throws<NotSupportedException>(() => mutable.Clear());

        var second = new FakeYubiKey("second");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([second]);
        fixture.SmartCard.Raise();
        await fixture.WaitForScanAsync(2, TestToken);
        Assert.Same(first, Assert.Single(snapshot));
        Assert.Same(second, Assert.Single(monitor.CurrentDevices));
        await monitor.DisposeAsync();
        Assert.Same(first, Assert.Single(snapshot));
        Assert.Throws<ObjectDisposedException>(() => monitor.CurrentDevices);
        await using var afterDisposal = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        Assert.False(await afterDisposal.MoveNextAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "RuntimeResilience")]
    public async Task BackgroundScanOrPublicationFailure_RetainsPublishedMembership_AndNextHintRetries(bool publicationFailure)
    {
        await using var fixture = new OwnedMonitorFixture();
        var first = new FakeYubiKey("first");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([first]);
        var afterFailure = Signal();
        var afterRetry = Signal();
        var waits = 0;
        fixture.WaitReached = () =>
        {
            var count = Interlocked.Increment(ref waits);
            if (count == 2)
                afterFailure.TrySetResult();
            if (count == 3)
                afterRetry.TrySetResult();
        };
        var monitor = await fixture.CreateAsync(TestToken);
        await using var watcher = await DeviceEventWatcher.StartAsync(
            monitor.WatchAsync, () => fixture.Repository.WatcherCount, TestToken);
        await watcher.WaitForCountAsync(1, "initial membership was not replayed", TestToken);
        if (publicationFailure)
        {
            fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([]);
            fixture.Worker.PublishAdmittedForTest = () => throw new IOException("transient publication failure");
        }
        else
            fixture.Finder.Scan = _ => throw new IOException("transient scan failure");
        fixture.SmartCard.Raise();
        await afterFailure.Task.WaitAsync(Bound, TestToken);

        Assert.Same(first, Assert.Single(monitor.CurrentDevices));
        AssertChange(Assert.Single(watcher.Events), DeviceAction.Added, first);
        var second = new FakeYubiKey("second");
        fixture.Worker.PublishAdmittedForTest = null;
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([second]);
        fixture.SmartCard.Raise();
        await watcher.WaitForCountAsync(3, "monitor did not recover from a failed scan", TestToken);
        await afterRetry.Task.WaitAsync(Bound, TestToken);
        Assert.Same(second, Assert.Single(monitor.CurrentDevices));
        Assert.Equal(3, fixture.Finder.ScanCount);
        Assert.Collection(watcher.Events,
            change => AssertChange(change, DeviceAction.Added, first),
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
        await using var first = new OwnedMonitorFixture();
        await using var second = new OwnedMonitorFixture();
        var firstMonitor = await first.CreateAsync(TestToken);
        var secondMonitor = await second.CreateAsync(TestToken);
        await using var firstWatcher = await DeviceEventWatcher.StartAsync(
            firstMonitor.WatchAsync, () => first.Repository.WatcherCount, TestToken);
        await using var secondWatcher = await DeviceEventWatcher.StartAsync(
            secondMonitor.WatchAsync, () => second.Repository.WatcherCount, TestToken);

        await firstMonitor.DisposeAsync();
        await firstWatcher.Completion.WaitAsync(Bound, TestToken);
        var device = new FakeYubiKey("survivor");
        second.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([device]);
        second.SmartCard.Raise();
        await second.WaitForScanAsync(2, TestToken);
        await secondWatcher.WaitForCountAsync(1, "disposing the other monitor stopped delivery", TestToken);

        Assert.Same(device, Assert.Single(secondMonitor.CurrentDevices));
        Assert.False(secondWatcher.Completion.IsCompleted);
    }

    [Fact]
    public async Task DisposeAsync_AdmittedPublicationOutlivesBoundedDrain_CannotRepopulateOrEmit()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
        await using var watcher = await DeviceEventWatcher.StartAsync(
            monitor.WatchAsync, () => fixture.Repository.WatcherCount, TestToken);
        var admitted = Signal();
        var release = Signal();
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([new FakeYubiKey("late")]);
        fixture.Worker.PublishAdmittedForTest = async () =>
        {
            admitted.TrySetResult();
            await release.Task.WaitAsync(TestToken);
        };
        fixture.SmartCard.Raise();
        try
        {
            await admitted.Task.WaitAsync(Bound, TestToken);
            await monitor.DisposeAsync();
            await monitor.DisposeAsync();
            release.TrySetResult();
            await fixture.Loop.WaitAsync(Bound, TestToken);
            await watcher.Completion.WaitAsync(Bound, TestToken);
            Assert.Empty(watcher.Events);
            Assert.False(fixture.Repository.HasData);
            Assert.Throws<ObjectDisposedException>(() => fixture.Repository.GetAll());
        }
        finally
        {
            release.TrySetResult();
            // Release the admitted publication before draining its actual owner.
            await monitor.DisposeAsync();
            await fixture.Loop.WaitAsync(Bound, CancellationToken.None);
        }
    }

    [Fact]
    public async Task DisposeAsync_ConcurrentCallsShareCompletion_UntilRepositoryTeardownFinishes()
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
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
        var repository = new YubiKeyDeviceRepository();
        var worker = new YubiKeyMonitorWorker(
            repository, rig.CreateFinder(ConnectionType.SmartCard),
            () => new OwnedMonitorFixture.FakeHidListener(), () => new OwnedMonitorFixture.FakeSmartCardListener(), TimeSpan.FromMilliseconds(50));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var creation = YubiKeyMonitor.CreateAsync(repository, worker, cancellation.Token, TimeSpan.FromHours(1));
        var loop = worker.MonitoringCompletionForTest;
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
            await cancellation.CancelAsync();
            rig.PendingOpen.TrySetException(new InvalidOperationException("Test cleanup"));
            try
            {
                var monitor = await creation.WaitAsync(Bound, CancellationToken.None);
                await monitor.DisposeAsync();
            }
            catch (OperationCanceledException) when (creation.IsCanceled)
            {
                // The factory owns and cleans a canceled initialization, even on assertion failure.
            }
            // Admitted work owns these fakes until teardown, including when runner cancellation fires.
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(CancellationToken.None);
            await loop.WaitAsync(Bound, CancellationToken.None);
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
        var repository = new YubiKeyDeviceRepository();
        var hid = new OwnedMonitorFixture.FakeHidListener();
        var smartCard = new OwnedMonitorFixture.FakeSmartCardListener();
        var worker = new YubiKeyMonitorWorker(
            repository, finder, () => hid, () => smartCard, TimeSpan.FromMilliseconds(50));
        try
        {
            for (var i = 0; i < DiscoveryWorkerAdmission.MaximumConcurrentWorkers; i++)
            {
                Assert.True(DiscoveryWorkerAdmission.TryAcquire(out var admission));
                admissions.Add(admission);
            }

            await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker, TestToken, TimeSpan.FromHours(1));
            Assert.Equal(2, monitor.CurrentDevices.Count);
            var hidCallback = hid.DeviceEvent;
            var pcscCallback = smartCard.DeviceEvent;
            Assert.NotNull(hidCallback);
            Assert.NotNull(pcscCallback);
            await monitor.DisposeAsync();
            // Captured callbacks must not renew the built-in finder's finally retired read scope.
            hidCallback(new HidDeviceRescanHint(HidDeviceChangeKind.Removed));
            pcscCallback();

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
        await using var fixture = new OwnedMonitorFixture();
        var reachedWait = Signal();
        var failure = new IOException("terminal loop failure");
        fixture.WaitReached = () => reachedWait.TrySetResult();
        var monitor = await fixture.CreateAsync(TestToken);
        await reachedWait.Task.WaitAsync(Bound, TestToken);
        await using var watcher = await DeviceEventWatcher.StartAsync(
            monitor.WatchAsync, () => fixture.Repository.WatcherCount, TestToken);
        fixture.WaitReached = () => throw failure;
        fixture.SmartCard.Raise();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => watcher.Completion.WaitAsync(Bound, TestToken));
        Assert.Same(failure, actual.InnerException);
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => monitor.CurrentDevices).InnerException);
        await using var future = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        var futureFailure = await Assert.ThrowsAsync<InvalidOperationException>(() => future.MoveNextAsync().AsTask());
        Assert.Same(failure, futureFailure.InnerException);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task TerminalLoopFailure_ThrowingLogger_CannotPreventFailurePublicationOrDisposal()
    {
        await using var fixture = new OwnedMonitorFixture();
        var reachedWait = Signal();
        var loggingEntered = Signal();
        using var releaseLogging = new ManualResetEventSlim();
        var failure = new IOException("terminal loop failure");
        var logger = new ThrowingTerminalLogger(error =>
        {
            Assert.Same(failure, error);
            loggingEntered.TrySetResult();
            Assert.True(releaseLogging.Wait(Bound, TestToken));
        });
        fixture.Worker.TerminalFailureLogForTest = error => logger.LogError(error, "terminal report");
        fixture.WaitReached = () => reachedWait.TrySetResult();
        var monitor = await fixture.CreateAsync(TestToken);
        await reachedWait.Task.WaitAsync(Bound, TestToken);
        await using var active = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
        var pending = active.MoveNextAsync().AsTask();
        fixture.WaitReached = () => throw failure;
        fixture.SmartCard.Raise();
        try
        {
            await loggingEntered.Task.WaitAsync(Bound, TestToken);
            // Failure must already be visible while the provider is blocked, before it throws.
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => monitor.CurrentDevices).InnerException);
            var activeError = await Assert.ThrowsAsync<InvalidOperationException>(() => pending.WaitAsync(Bound, TestToken));
            Assert.Same(failure, activeError.InnerException);
            await using var future = monitor.WatchAsync(TestToken).GetAsyncEnumerator(TestToken);
            var futureError = await Assert.ThrowsAsync<InvalidOperationException>(() => future.MoveNextAsync().AsTask());
            Assert.Same(failure, futureError.InnerException);
            // A blocked provider must not hold a lifecycle lock or prevent bounded disposal.
            await Task.Run(async () => await monitor.DisposeAsync(), TestToken).WaitAsync(Bound, TestToken);
            Assert.True(fixture.Hid.Disposed && fixture.SmartCard.Disposed);
            Assert.Throws<ObjectDisposedException>(() => monitor.CurrentDevices);
        }
        finally
        {
            releaseLogging.Set();
            // The provider has left its gate; drain the outstanding move before enumerator disposal,
            // including on an assertion failure or runner cancellation.
            await monitor.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None);
            await fixture.Loop.WaitAsync(Bound, CancellationToken.None);
            try { await pending.WaitAsync(Bound, CancellationToken.None); }
            catch (InvalidOperationException) { }
        }
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task ListenerScans_WaitForPublication_AndPublishOrderedDeltas()
    {
        await using var fixture = new OwnedMonitorFixture();
        var first = new FakeYubiKey("first");
        var second = new FakeYubiKey("second");
        var third = new FakeYubiKey("third");
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([first]);
        var monitor = await fixture.CreateAsync(TestToken);
        await using var watcher = await DeviceEventWatcher.StartAsync(fixture.Repository, TestToken);
        var admitted = Signal();
        var release = Signal();
        fixture.Worker.PublishAdmittedForTest = () =>
        {
            admitted.TrySetResult();
            return release.Task;
        };
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([second]);
        fixture.SmartCard.Raise();
        try
        {
            await admitted.Task.WaitAsync(Bound, TestToken);
            fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([third]);
            fixture.SmartCard.Raise();
            await Task.Delay(YubiKeyMonitorWorker.ThrottleInterval * 2, TestToken);
            Assert.Equal(2, fixture.Finder.ScanCount);
            Assert.Same(first, Assert.Single(monitor.CurrentDevices));
            release.TrySetResult();
            await fixture.WaitForScanAsync(3, TestToken);
            Assert.Same(third, Assert.Single(monitor.CurrentDevices));
            await monitor.DisposeAsync();
            await watcher.Completion.WaitAsync(Bound, TestToken);
            Assert.Collection(watcher.Events,
                change => AssertChange(change, DeviceAction.Removed, first),
                change => AssertChange(change, DeviceAction.Added, second),
                change => AssertChange(change, DeviceAction.Removed, second),
                change => AssertChange(change, DeviceAction.Added, third));
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task DisposeAsync_LoopParkedInsideRepositoryDiff_RejectsLateCommit()
    {
        await using var fixture = new OwnedMonitorFixture();
        var removed = new BlockingIdentityKey();
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([removed]);
        var monitor = await fixture.CreateAsync(TestToken);
        await using var watcher = await DeviceEventWatcher.StartAsync(fixture.Repository, TestToken);
        using var release = new ManualResetEventSlim();
        var entered = Signal();
        removed.OnIdRead = () =>
        {
            entered.TrySetResult();
            Assert.True(release.Wait(Bound, TestToken));
        };
        fixture.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([new FakeYubiKey("late")]);
        fixture.SmartCard.Raise();
        try
        {
            await entered.Task.WaitAsync(Bound, TestToken);
            await monitor.DisposeAsync().AsTask().WaitAsync(Bound, TestToken);
            Assert.False(fixture.Loop.IsCompleted);
            release.Set();
            await fixture.Loop.WaitAsync(Bound, TestToken);
            await watcher.Completion.WaitAsync(Bound, TestToken);
            Assert.Empty(watcher.Events);
            Assert.False(fixture.Repository.HasData);
            Assert.Throws<ObjectDisposedException>(() => fixture.Repository.GetAll());
        }
        finally
        {
            release.Set();
            await monitor.DisposeAsync();
            // The diff owns the fake getter until the real loop exits, not just finder return.
            await fixture.Loop.WaitAsync(Bound, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "RuntimeResilience")]
    public async Task DisposeAsync_AbandonedScanCannotAffectANewMonitor(bool lateFailure)
    {
        await using var old = new OwnedMonitorFixture();
        var oldMonitor = await old.CreateAsync(TestToken);
        var entered = Signal();
        var release = Signal();
        old.Finder.Scan = async _ =>
        {
            entered.TrySetResult();
            await release.Task; // Simulates native discovery ignoring lifetime cancellation.
            if (lateFailure)
                throw new IOException("abandoned discovery failed");
            return [new FakeYubiKey("stale")];
        };
        await using var oldWatcher = await DeviceEventWatcher.StartAsync(old.Repository, TestToken);
        old.SmartCard.Raise();
        try
        {
            await entered.Task.WaitAsync(Bound, TestToken);
            await oldMonitor.DisposeAsync().AsTask().WaitAsync(Bound, TestToken);
            Assert.False(old.Loop.IsCompleted);

            await using var next = new OwnedMonitorFixture();
            var retained = new FakeYubiKey("successor");
            next.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([retained]);
            var nextMonitor = await next.CreateAsync(TestToken);
            await using var nextWatcher = await DeviceEventWatcher.StartAsync(next.Repository, TestToken);
            release.TrySetResult();
            await old.Loop.WaitAsync(Bound, TestToken);
            Assert.False(old.Repository.HasData);
            Assert.Throws<ObjectDisposedException>(() => old.Repository.GetAll());
            Assert.Same(retained, Assert.Single(nextMonitor.CurrentDevices));
            var added = new FakeYubiKey("still-live");
            next.Finder.Scan = _ => Task.FromResult<IReadOnlyList<IYubiKey>>([retained, added]);
            next.SmartCard.Raise();
            await next.WaitForScanAsync(2, TestToken);
            await nextMonitor.DisposeAsync();
            await nextWatcher.Completion.WaitAsync(Bound, TestToken);
            AssertChange(Assert.Single(nextWatcher.Events), DeviceAction.Added, added);
            await oldWatcher.Completion.WaitAsync(Bound, TestToken);
            Assert.Empty(oldWatcher.Events);
        }
        finally
        {
            release.TrySetResult();
            await oldMonitor.DisposeAsync();
            // Abandoned work retains its fakes until the loop has handled the late result/error.
            await old.Loop.WaitAsync(Bound, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "RuntimeResilience")]
    public async Task DisposeAsync_OrdersAdmittedListenerActivity_AndIgnoresCapturedLateCallback(bool hid)
    {
        await using var fixture = new OwnedMonitorFixture();
        var monitor = await fixture.CreateAsync(TestToken);
        await fixture.WaitForScanAsync(1, TestToken);
        var hidCallback = fixture.Hid.DeviceEvent;
        var pcscCallback = fixture.SmartCard.DeviceEvent;
        Assert.NotNull(hidCallback);
        Assert.NotNull(pcscCallback);
        void Callback()
        {
            if (hid)
                hidCallback(new HidDeviceRescanHint(HidDeviceChangeKind.Removed));
            else
                pcscCallback();
        }

        using var release = new ManualResetEventSlim();
        var entered = Signal();
        fixture.Finder.OnActivity = _ =>
        {
            entered.TrySetResult();
            Assert.True(release.Wait(Bound, TestToken));
        };
        var callback = Task.Run(Callback, TestToken);
        Task? disposal = null;
        try
        {
            await entered.Task.WaitAsync(Bound, TestToken);
            var disposalStarted = Signal();
            disposal = Task.Run(async () =>
            {
                disposalStarted.TrySetResult();
                await monitor.DisposeAsync();
            }, TestToken);
            await disposalStarted.Task.WaitAsync(Bound, TestToken);
            // Closing public access precedes waiting for already-admitted ingress to retire.
            await AsyncWait.WaitUntilAsync(() =>
            {
                try { _ = monitor.CurrentDevices; return false; }
                catch (ObjectDisposedException) { return true; }
            }, "Disposal did not close public access", Bound, TestToken);
            Assert.False(disposal.IsCompleted);
            release.Set();
            await callback.WaitAsync(Bound, TestToken);
            await disposal.WaitAsync(Bound, TestToken);
            await fixture.Loop.WaitAsync(Bound, TestToken);
            Assert.Null(fixture.Hid.DeviceEvent);
            Assert.Null(fixture.SmartCard.DeviceEvent);
            var scans = fixture.Finder.ScanCount;
            Assert.Equal(1, fixture.Finder.ActivityCount);
            Callback();
            Assert.Equal(1, fixture.Finder.ActivityCount);
            Assert.Equal(scans, fixture.Finder.ScanCount);
        }
        finally
        {
            release.Set();
            // Ingress and disposal must leave their gates before fixture teardown, even if canceled.
            await callback.WaitAsync(Bound, CancellationToken.None);
            if (disposal is not null)
                await disposal.WaitAsync(Bound, CancellationToken.None);
        }
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
                try { await Task.Delay(Timeout.InfiniteTimeSpan, ct); }
                finally { canceled.TrySetResult(); }
                return [];
            };
            fixture.SmartCard.Raise();
            await entered.Task.WaitAsync(Bound, TestToken);
            await monitor.DisposeAsync().AsTask().WaitAsync(Bound, TestToken);
            await fixture.Loop.WaitAsync(Bound, TestToken);
            Assert.True(canceled.Task.IsCompletedSuccessfully);
            Assert.Equal(2, fixture.Finder.ScanCount);
        }
    }

    private sealed class ThrowingTerminalLogger(Action<Exception?> beforeThrow) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            beforeThrow(exception);
            throw new IOException("logging provider failed");
        }
    }

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

}