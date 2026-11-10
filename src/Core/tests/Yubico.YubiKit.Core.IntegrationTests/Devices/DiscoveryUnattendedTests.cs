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

using Xunit.Abstractions;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

/// <summary>
/// Unattended discovery and monitoring checks against whatever keys are attached. Read-only: no applet
/// session is opened, so keys outside the allow list may be present.
/// </summary>
/// <remarks>
/// Run with <c>--filter "Category=Discovery&amp;Category!=RequiresUserPresence"</c>. The attended hotplug
/// ceremonies live in <see cref="DeviceEventHotPlugTests"/> and <see cref="HotPlugIdentityContractTests"/>.
/// </remarks>
[Trait(TestCategories.Category, TestCategories.RequiresHardware)]
[Trait(TestCategories.Category, TestCategories.Discovery)]
public class DiscoveryUnattendedTests(ITestOutputHelper output)
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task FindAllAsync_ReturnsAtLeastOneKey_AndScopedScansAreSubsets()
    {
        using var timeout = new CancellationTokenSource(Bound);
        var all = await TransientScanRetry.ScanAsync(() => YubiKeyManager.FindAllAsync(cancellationToken: timeout.Token));
        Assert.NotEmpty(all);

        var smartCard = await YubiKeyManager.FindAllAsync(ConnectionType.SmartCard, timeout.Token);
        var hid = await YubiKeyManager.FindAllAsync(ConnectionType.Hid, timeout.Token);
        Assert.All(smartCard, key => Assert.Equal(ConnectionType.SmartCard, key.AvailableConnections));
        Assert.All(hid, key => Assert.Equal(ConnectionType.Unknown, key.AvailableConnections & ConnectionType.SmartCard));
        Assert.Empty(await YubiKeyManager.FindAllAsync(ConnectionType.Unknown, timeout.Token));

        output.WriteLine($"all={all.Count} smartCard={smartCard.Count} hid={hid.Count}");
        foreach (var key in all)
            output.WriteLine($"  {key.DeviceId} serial={key.SerialNumber?.ToString() ?? "?"} {key.AvailableConnections}");
    }

    [Fact]
    public async Task FindFirstMethods_AgreeWithFindAll()
    {
        using var timeout = new CancellationTokenSource(Bound);
        var all = await TransientScanRetry.ScanAsync(() => YubiKeyManager.FindAllAsync(cancellationToken: timeout.Token));
        Assert.NotEmpty(all);

        var first = await YubiKeyManager.FindFirstAsync(cancellationToken: timeout.Token);
        Assert.Contains(all, key => key.DeviceId == first.DeviceId || key.SameDeviceAs(first) == DeviceCorrelation.Same);
        Assert.Null(await YubiKeyManager.FindFirstOrDefaultAsync(_ => false, cancellationToken: timeout.Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() => YubiKeyManager.FindFirstAsync(_ => false, cancellationToken: timeout.Token));
    }

    [Fact]
    public async Task CreateMonitorAsync_InitialReplayMatchesOneShotDiscovery()
    {
        using var timeout = new CancellationTokenSource(Bound);
        var oneShot = await TransientScanRetry.ScanAsync(() => YubiKeyManager.FindAllAsync(cancellationToken: timeout.Token));
        await using var monitor = await YubiKeyManager.CreateMonitorAsync(timeout.Token);

        var (replay, complete) = await ReadSnapshotAsync(monitor.WatchAsync(timeout.Token), timeout.Token);
        Assert.Equal(1, complete.Revision);
        Assert.Equal(oneShot.Count, replay.Count);
        foreach (var expected in oneShot)
            Assert.Contains(replay, actual => actual.SameDeviceAs(expected) == DeviceCorrelation.Same || actual.DeviceId == expected.DeviceId);
    }

    [Fact]
    public async Task WatchAsync_Facade_ReplaysThenSnapshotComplete_AndDisposesOnBreak()
    {
        using var timeout = new CancellationTokenSource(Bound);
        var replayed = 0;
        SnapshotComplete? complete = null;
        await foreach (var change in YubiKeyManager.WatchAsync(timeout.Token))
        {
            switch (change)
            {
                case DeviceAdded:
                    replayed++;
                    break;
                case SnapshotComplete marker:
                    complete = marker;
                    break;
                default:
                    Assert.Fail($"Unexpected change before SnapshotComplete: {change}");
                    break;
            }

            if (complete is not null)
                break;
        }

        Assert.NotNull(complete);
        Assert.True(replayed > 0, "Expected at least one attached key in the replay.");
        Assert.Equal(1, complete.Revision);

        // The enumeration-owned monitor is disposed on break; a second enumeration starts a fresh one.
        var (again, againComplete) = await ReadSnapshotAsync(YubiKeyManager.WatchAsync(timeout.Token), timeout.Token);
        Assert.Equal(replayed, again.Count);
        Assert.Equal(1, againComplete.Revision);
    }

    [Fact]
    public async Task WatchAsync_LateSubscriber_ReplaysAtCurrentRevision_AndCancellationIsSubscriberLocal()
    {
        using var timeout = new CancellationTokenSource(Bound);
        await using var monitor = await YubiKeyManager.CreateMonitorAsync(timeout.Token);

        using var early = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        await using var earlyEnumerator = monitor.WatchAsync(early.Token).GetAsyncEnumerator(early.Token);
        var earlySnapshot = await ReadSnapshotAsync(earlyEnumerator, timeout.Token);

        var (lateReplay, lateComplete) = await ReadSnapshotAsync(monitor.WatchAsync(timeout.Token), timeout.Token);
        Assert.Equal(earlySnapshot.Replay.Count, lateReplay.Count);
        Assert.True(lateComplete.Revision >= earlySnapshot.Complete.Revision);
        foreach (var device in earlySnapshot.Replay)
            Assert.Contains(lateReplay, candidate => ReferenceEquals(candidate, device));

        await early.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => earlyEnumerator.MoveNextAsync().AsTask());

        // The producer is unaffected by one subscriber's cancellation.
        var (afterCancel, _) = await ReadSnapshotAsync(monitor.WatchAsync(timeout.Token), timeout.Token);
        Assert.Equal(lateReplay.Count, afterCancel.Count);
    }

    [Fact]
    public async Task DisposeAsync_EndsLiveEnumerationsNormally_AndLaterEnumerationsAreEmpty()
    {
        using var timeout = new CancellationTokenSource(Bound);
        var monitor = await YubiKeyManager.CreateMonitorAsync(timeout.Token);

        await using var live = monitor.WatchAsync(timeout.Token).GetAsyncEnumerator(timeout.Token);
        _ = await ReadSnapshotAsync(live, timeout.Token);
        var pendingMove = live.MoveNextAsync().AsTask();

        await monitor.DisposeAsync();
        await monitor.DisposeAsync();

        // Already-published changes may drain; the stream then ends without faulting.
        var more = await pendingMove.WaitAsync(timeout.Token);
        while (more)
            more = await live.MoveNextAsync().AsTask().WaitAsync(timeout.Token);

        await using var afterDisposal = monitor.WatchAsync(timeout.Token).GetAsyncEnumerator(timeout.Token);
        Assert.False(await afterDisposal.MoveNextAsync());

        await using var recreated = await YubiKeyManager.CreateMonitorAsync(timeout.Token);
        var (replay, complete) = await ReadSnapshotAsync(recreated.WatchAsync(timeout.Token), timeout.Token);
        Assert.NotEmpty(replay);
        Assert.Equal(1, complete.Revision);
    }

    [Fact]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task IntervalRescan_WithoutHotplug_PublishesNoChanges()
    {
        // Longer than the five-second fallback so at least one interval scan runs. A no-change rescan must
        // not emit DeviceAdded/DeviceRemoved churn or a DeviceUpdated for metadata that did not change.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        await using var monitor = await YubiKeyManager.CreateMonitorAsync(timeout.Token);
        using var subscription = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        await using var enumerator = monitor.WatchAsync(subscription.Token).GetAsyncEnumerator(subscription.Token);
        _ = await ReadSnapshotAsync(enumerator, timeout.Token);

        var next = enumerator.MoveNextAsync().AsTask();
        var quiet = await Task.WhenAny(next, Task.Delay(TimeSpan.FromSeconds(12), timeout.Token));
        if (quiet == next && await next)
            Assert.Fail($"Unexpected change during a quiet interval rescan: {enumerator.Current}");

        // Retire the in-flight move before the enumerator is disposed.
        await subscription.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => next);
    }

    private static async Task<(IReadOnlyList<IYubiKey> Replay, SnapshotComplete Complete)> ReadSnapshotAsync(
        IAsyncEnumerable<DeviceChange> stream, CancellationToken cancellationToken)
    {
        await using var enumerator = stream.GetAsyncEnumerator(cancellationToken);
        return await ReadSnapshotAsync(enumerator, cancellationToken);
    }

    private static async Task<(IReadOnlyList<IYubiKey> Replay, SnapshotComplete Complete)> ReadSnapshotAsync(
        IAsyncEnumerator<DeviceChange> enumerator, CancellationToken cancellationToken)
    {
        var replay = new List<IYubiKey>();
        while (await enumerator.MoveNextAsync().AsTask().WaitAsync(cancellationToken))
        {
            if (enumerator.Current is SnapshotComplete complete)
                return (replay, complete);
            var added = Assert.IsType<DeviceAdded>(enumerator.Current);
            Assert.DoesNotContain(added.Device, replay);
            replay.Add(added.Device);
        }

        throw new InvalidOperationException("The subscription ended before SnapshotComplete.");
    }
}
