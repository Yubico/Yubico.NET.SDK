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
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Management.IntegrationTests;

/// <summary>
///     Read-only public discovery/monitor contracts requiring at least two attached allow-listed,
///     serial-bearing USB keys. No prompts, hotplug, configuration, authentication, or credential writes.
/// </summary>
/// <remarks>
///     An incomplete initial monitor snapshot fails immediately; it is never polled into readiness.
///     All live connections are sequential and outside every monitor lifetime. Real races and no-I/O
///     backend counts are covered by Core's deterministic tests, not inferred from hardware timing.
/// </remarks>
[Trait(TestCategories.Category, TestCategories.RequiresHardware)]
public class OwnedMonitorReadOnlyTests(ITestOutputHelper output)
{
    private readonly ScopedDiscoveryReadOnlyTests _scoped = new(output);
    private static readonly ConnectionType[] ConcreteTypes =
        [ConnectionType.SmartCard, ConnectionType.HidFido, ConnectionType.HidOtp];

    [Fact]
    public async Task CreateMonitorAsync_ReturnsBothFixturesReady_AndReplaysInitialSnapshot()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var reference = await ReferenceFixturesAsync(timeout.Token);
        await using var monitor = await YubiKeyManager.CreateMonitorAsync(timeout.Token);
        var snapshot = monitor.CurrentDevices;
        AssertFixturesReady(reference, snapshot);

        await using var watcher = monitor.WatchAsync(timeout.Token).GetAsyncEnumerator(timeout.Token);
        await AssertReplayAsync(watcher, snapshot, timeout.Token);
        for (var i = 0; i < 100; i++)
        {
            var current = monitor.CurrentDevices;
            Assert.Equal(snapshot, current);
            foreach (var key in snapshot)
                Assert.Contains(current, candidate => ReferenceEquals(candidate, key));
        }
        output.WriteLine($"Initialized snapshot and initial Added replay cover {_scoped.Authorized(snapshot).Select(key => key.SerialNumber).Distinct().Count()} authorized keys.");
    }

    [Fact]
    public async Task WatchAsync_CancellationIsSubscriberLocal_DisposalCompletesStreams_AndRecreationIsIndependent()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var reference = await ReferenceFixturesAsync(timeout.Token);
        using var creation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        await using var monitor = await YubiKeyManager.CreateMonitorAsync(creation.Token);
        var snapshot = monitor.CurrentDevices;
        AssertFixturesReady(reference, snapshot);
        await creation.CancelAsync(); // Factory token no longer owns this initialized lifetime.
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        await using var canceled = monitor.WatchAsync(cancellation.Token).GetAsyncEnumerator(cancellation.Token);
        await using var healthy = monitor.WatchAsync(timeout.Token).GetAsyncEnumerator(timeout.Token);
        Assert.True(await canceled.MoveNextAsync());
        Assert.True(await healthy.MoveNextAsync());
        Assert.Equal(DeviceAction.Added, healthy.Current.Action);
        var replay = new List<IYubiKey> { healthy.Current.Device };
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled.MoveNextAsync().AsTask());
        for (var i = 1; i < snapshot.Count; i++)
        {
            Assert.True(await healthy.MoveNextAsync().AsTask().WaitAsync(timeout.Token));
            Assert.Equal(DeviceAction.Added, healthy.Current.Action);
            replay.Add(healthy.Current.Device);
        }
        Assert.Equal(snapshot.OrderBy(key => key.DeviceId), replay.OrderBy(key => key.DeviceId));
        AssertFixturesReady(reference, monitor.CurrentDevices);

        await monitor.DisposeAsync();
        await monitor.DisposeAsync();
        while (await healthy.MoveNextAsync().AsTask().WaitAsync(timeout.Token))
        {
            // Already-published deltas may drain; do not require disposal to erase queued history.
        }
        Assert.Throws<ObjectDisposedException>(() => monitor.CurrentDevices);
        AssertFixturesReady(reference, snapshot); // Historical membership survives disposal.
        await using var afterDisposal = monitor.WatchAsync(timeout.Token).GetAsyncEnumerator(timeout.Token);
        Assert.False(await afterDisposal.MoveNextAsync());

        await using var recreated = await YubiKeyManager.CreateMonitorAsync(timeout.Token);
        AssertFixturesReady(reference, recreated.CurrentDevices);
        foreach (var previous in _scoped.Authorized(snapshot))
        {
            var next = recreated.CurrentDevices.First(key => key.SerialNumber == previous.SerialNumber);
            Assert.NotSame(previous, next);
            Assert.Equal(DeviceCorrelation.Same, previous.SameDeviceAs(next));
        }
        await using var newWatcher = recreated.WatchAsync(timeout.Token).GetAsyncEnumerator(timeout.Token);
        await AssertReplayAsync(newWatcher, recreated.CurrentDevices, timeout.Token);
        output.WriteLine("Subscriber cancellation, normal disposal completion, and a fresh initialized monitor passed for both fixtures.");
    }

    [Fact]
    public async Task FindConveniences_SelectEachFixtureWithCombinedInterfaces_AndReportNoMatchWithoutRemoval()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var reference = await ReferenceFixturesAsync(timeout.Token);
        foreach (var fixture in reference.GroupBy(key => key.SerialNumber))
        {
            var serial = fixture.Key;
            var selected = await YubiKeyManager.FindFirstAsync(predicate: key => key.SerialNumber == serial,
                interfaces: ConnectionType.SmartCard | ConnectionType.HidFido, cancellationToken: timeout.Token);
            var optional = await YubiKeyManager.FindFirstOrDefaultAsync(predicate: key => key.SerialNumber == serial,
                interfaces: ConnectionType.SmartCard | ConnectionType.HidFido, cancellationToken: timeout.Token);
            Assert.NotNull(optional);
            Assert.Equal(serial, selected.SerialNumber);
            Assert.Equal(serial, optional.SerialNumber);
            Assert.NotSame(selected, optional);
            Assert.Equal(DeviceCorrelation.Same, selected.SameDeviceAs(optional));
            Assert.Equal(ConnectionType.Unknown, selected.AvailableConnections & ConnectionType.HidOtp);
            Assert.Equal(ConnectionType.Unknown, optional.AvailableConnections & ConnectionType.HidOtp);
        }

        Assert.Null(await YubiKeyManager.FindFirstOrDefaultAsync(predicate: _ => false, cancellationToken: timeout.Token));
        var noMatch = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            YubiKeyManager.FindFirstAsync(predicate: _ => false, cancellationToken: timeout.Token));
        Assert.Equal("No matching YubiKey was found.", noMatch.Message);
        output.WriteLine("Independent convenience searches selected both authorized serials; null/throw no-match contracts passed without hotplug.");
    }

    [Fact]
    public async Task CurrentDevices_CachedMetadataMatchesSequentialLiveReads_AfterMonitorDisposal()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var reference = await ReferenceFixturesAsync(timeout.Token);
        IReadOnlyList<IYubiKey> snapshot;
        await using (var monitor = await YubiKeyManager.CreateMonitorAsync(timeout.Token))
        {
            snapshot = monitor.CurrentDevices;
            AssertFixturesReady(reference, snapshot);
        }

        // No monitor or scan may probe disjoint sibling interfaces during these live reads.
        foreach (var view in _scoped.Authorized(snapshot))
        {
            var cached = Assert.IsType<DeviceInfo>(view.CachedDeviceInfo);
            foreach (var type in ConcreteTypes.Where(view.SupportsConnection))
                await _scoped.AssertLiveReadAsync(view, type, cached, timeout.Token);
        }
    }

    private async Task<IReadOnlyList<IYubiKey>> ReferenceFixturesAsync(CancellationToken token)
    {
        var reference = await _scoped.ReferenceScanAsync(token);
        var fixtures = ScopedDiscoveryReadOnlyTests.ConnectionsBySerial(reference);
        Assert.True(fixtures.Count >= 2, "This suite requires two attached authorized serial-bearing USB keys; fewer usable fixtures is an unmet hardware prerequisite.");
        var index = 0;
        foreach (var connections in fixtures.Values)
            output.WriteLine($"Authorized fixture {++index}: supported observed interfaces {connections}.");
        return reference;
    }

    private void AssertFixturesReady(IReadOnlyList<IYubiKey> reference, IReadOnlyList<IYubiKey> snapshot)
    {
        Assert.All(snapshot, key => Assert.NotNull(key.CachedDeviceInfo));
        var authorized = _scoped.Authorized(snapshot);
        Assert.Equal(ScopedDiscoveryReadOnlyTests.ConnectionsBySerial(reference).OrderBy(pair => pair.Key),
            ScopedDiscoveryReadOnlyTests.ConnectionsBySerial(authorized).OrderBy(pair => pair.Key));
        foreach (var view in authorized)
        {
            var baseline = reference.First(key => key.SerialNumber == view.SerialNumber);
            ScopedDiscoveryReadOnlyTests.AssertMetadataMatches(Assert.IsType<DeviceInfo>(baseline.CachedDeviceInfo),
                Assert.IsType<DeviceInfo>(view.CachedDeviceInfo));
        }
    }

    private static async Task AssertReplayAsync(IAsyncEnumerator<DeviceEvent> watcher, IReadOnlyList<IYubiKey> snapshot, CancellationToken token)
    {
        var replay = new HashSet<IYubiKey>();
        for (var i = 0; i < snapshot.Count; i++)
        {
            Assert.True(await watcher.MoveNextAsync().AsTask().WaitAsync(token));
            Assert.Equal(DeviceAction.Added, watcher.Current.Action);
            Assert.Contains(watcher.Current.Device, snapshot);
            Assert.True(replay.Add(watcher.Current.Device), "Initial replay duplicated a device.");
        }
        Assert.True(replay.SetEquals(snapshot));
    }
}