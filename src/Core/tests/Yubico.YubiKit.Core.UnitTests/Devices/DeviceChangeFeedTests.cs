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

public class DeviceChangeFeedTests
{
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Subscribe_EmptySnapshot_EmitsCompletionAtCurrentRevision()
    {
        var feed = new DeviceChangeFeed();
        feed.Apply([]);
        feed.Apply([]);
        await using var subscriber = feed.Subscribe(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await subscriber.MoveNextAsync());
        Assert.Equal(new SnapshotComplete(2), subscriber.Current);
        feed.Complete();
        Assert.False(await subscriber.MoveNextAsync());
    }

    [Fact]
    public async Task Subscribe_LazyReplayAndRegistration_AreAtomicWithLaterDeltas()
    {
        var feed = new DeviceChangeFeed();
        var first = new FakeYubiKey("first");
        var second = new FakeYubiKey("second");
        var third = new FakeYubiKey("third");
        feed.Apply([first]);
        var stream = feed.Subscribe(TestToken);
        await using var early = stream.GetAsyncEnumerator(TestToken);
        feed.Apply([second]);
        Assert.True(await early.MoveNextAsync());
        Assert.Equal(new DeviceAdded(2, second), early.Current);
        feed.Apply([third]); // Happens while replay is suspended, before SnapshotComplete.
        await using var late = stream.GetAsyncEnumerator(TestToken);
        Assert.True(await late.MoveNextAsync());
        Assert.Equal(new DeviceAdded(3, third), late.Current);
        feed.Complete();
        Assert.True(await early.MoveNextAsync());
        Assert.Equal(new SnapshotComplete(2), early.Current);
        Assert.True(await early.MoveNextAsync());
        Assert.Equal(new DeviceRemoved(3, second), early.Current);
        Assert.True(await early.MoveNextAsync());
        Assert.Equal(new DeviceAdded(3, third), early.Current);
        Assert.False(await early.MoveNextAsync());
        Assert.True(await late.MoveNextAsync());
        Assert.Equal(new SnapshotComplete(3), late.Current);
        Assert.False(await late.MoveNextAsync());
    }

    [Fact]
    public async Task Subscribe_RacingPublication_ReceivesEitherWholeSnapshotBoundary()
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var feed = new DeviceChangeFeed();
            var first = new FakeYubiKey("first");
            var second = new FakeYubiKey("second");
            feed.Apply([first]);
            await using var subscriber = feed.Subscribe(TestToken).GetAsyncEnumerator(TestToken);
            var publication = Task.Run(() => feed.Apply([second]), TestToken);
            Assert.True(await subscriber.MoveNextAsync());
            var changes = new List<DeviceChange> { subscriber.Current };
            await publication.WaitAsync(TimeSpan.FromSeconds(10), TestToken);
            feed.Complete();
            while (await subscriber.MoveNextAsync())
                changes.Add(subscriber.Current);

            DeviceChange[] expected = changes[0].Revision == 1
                ? [new DeviceAdded(1, first), new SnapshotComplete(1), new DeviceRemoved(2, first), new DeviceAdded(2, second)]
                : [new DeviceAdded(2, second), new SnapshotComplete(2)];
            Assert.Equal(expected, changes);
        }
    }

    [Fact]
    public async Task Subscribe_LargeReplay_DoesNotUseDeltaBuffer()
    {
        var feed = new DeviceChangeFeed();
        IYubiKey[] devices = [.. Enumerable.Range(0, 300).Select(i => new FakeYubiKey($"key-{i}"))];
        feed.Apply(devices);
        await using var subscriber = feed.Subscribe(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await subscriber.MoveNextAsync());
        var added = new FakeYubiKey("delta");
        feed.Apply([.. devices, added]);
        feed.Complete();
        Assert.Equal(new DeviceAdded(1, devices[0]), subscriber.Current);
        for (var i = 1; i < devices.Length; i++)
        {
            Assert.True(await subscriber.MoveNextAsync());
            Assert.Equal(new DeviceAdded(1, devices[i]), subscriber.Current);
        }
        Assert.True(await subscriber.MoveNextAsync());
        Assert.Equal(new SnapshotComplete(1), subscriber.Current);
        Assert.True(await subscriber.MoveNextAsync());
        Assert.Equal(new DeviceAdded(2, added), subscriber.Current);
        Assert.False(await subscriber.MoveNextAsync());
    }

    [Fact]
    public async Task Apply_Overflow_FaultsOnlySlowSubscriberAndAllowsReplayRecovery()
    {
        var feed = new DeviceChangeFeed();
        await using var slow = feed.Subscribe(TestToken).GetAsyncEnumerator(TestToken);
        await using var healthy = feed.Subscribe(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await slow.MoveNextAsync());
        Assert.True(await healthy.MoveNextAsync());
        var device = new FakeYubiKey("key");
        for (var i = 0; i < 257; i++)
        {
            feed.Apply(i % 2 == 0 ? [device] : []);
            Assert.True(await healthy.MoveNextAsync());
            Assert.Equal(i + 1, healthy.Current.Revision);
        }
        var overflow = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            while (await slow.MoveNextAsync()) { }
        });
        Assert.Contains("WatchAsync", overflow.Message, StringComparison.Ordinal);
        await using var recovered = feed.Subscribe(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await recovered.MoveNextAsync());
        Assert.Equal(new DeviceAdded(257, device), recovered.Current);
        feed.Complete();
        Assert.False(await healthy.MoveNextAsync());
    }

    [Fact]
    public async Task Complete_EndsCurrentAndLaterSubscriptionsNormally_AndRejectsApply()
    {
        var feed = new DeviceChangeFeed();
        await using var current = feed.Subscribe(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await current.MoveNextAsync());
        var pending = current.MoveNextAsync().AsTask();
        feed.Complete();
        feed.Complete();
        Assert.False(await pending.WaitAsync(TimeSpan.FromSeconds(10), TestToken));
        await using var later = feed.Subscribe(TestToken).GetAsyncEnumerator(TestToken);
        Assert.False(await later.MoveNextAsync());
        Assert.Throws<InvalidOperationException>(() => feed.Apply([]));
    }

    [Fact]
    public async Task Fault_EndsCurrentAndLaterSubscriptionsWithFirstFailure_AndRejectsApply()
    {
        var feed = new DeviceChangeFeed();
        await using var current = feed.Subscribe(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await current.MoveNextAsync());
        var failure = new IOException("scan loop failed");
        feed.Fault(failure);
        feed.Fault(new IOException("second failure"));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => current.MoveNextAsync().AsTask());
        Assert.Same(failure, error.InnerException);
        await using var later = feed.Subscribe(TestToken).GetAsyncEnumerator(TestToken);
        error = await Assert.ThrowsAsync<InvalidOperationException>(() => later.MoveNextAsync().AsTask());
        Assert.Same(failure, error.InnerException);
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => feed.Apply([])).InnerException);
        feed.Complete(); // Does not overwrite the error already delivered to current subscribers.
    }

    [Fact]
    public async Task Subscribe_CancellationDuringReplay_DoesNotAffectOtherSubscribers()
    {
        var feed = new DeviceChangeFeed();
        var first = new FakeYubiKey("first");
        var second = new FakeYubiKey("second");
        feed.Apply([first, second]);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        await using var canceled = feed.Subscribe(cancellation.Token).GetAsyncEnumerator(cancellation.Token);
        await using var healthy = feed.Subscribe(TestToken).GetAsyncEnumerator(TestToken);
        Assert.True(await canceled.MoveNextAsync());
        Assert.True(await healthy.MoveNextAsync());
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled.MoveNextAsync().AsTask());
        feed.Apply([]);
        feed.Complete();
        Assert.True(await healthy.MoveNextAsync());
        Assert.Equal(new DeviceAdded(1, second), healthy.Current);
        Assert.True(await healthy.MoveNextAsync());
        Assert.Equal(new SnapshotComplete(1), healthy.Current);
        Assert.True(await healthy.MoveNextAsync());
        Assert.Equal(new DeviceRemoved(2, first), healthy.Current);
        Assert.True(await healthy.MoveNextAsync());
        Assert.Equal(new DeviceRemoved(2, second), healthy.Current);
        Assert.False(await healthy.MoveNextAsync());
    }
}