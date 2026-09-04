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

using System.Diagnostics;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.UnitTests.Infrastructure;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
/// White-box tests for <see cref="DeviceEventHub"/> — the single delivery path behind
/// <c>YubiKeyManager.WatchAsync</c>.
/// </summary>
/// <remarks>
/// These pin the primitive: independent per-watcher buffers, lazy subscription, overflow and
/// cancellation isolation, normal completion, and a publisher that no consumer can stall.
/// Consumer-facing usage is demonstrated by the repository, manager, and integration tests.
/// </remarks>
public class DeviceEventHubTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(30);

    private static DeviceEvent Added(string deviceId) => new(DeviceAction.Added, new FakeYubiKey(deviceId));

    // ---------- Fan-out ----------

    [Fact]
    public async Task WatchAsync_YieldsPublishedEventsInOrder()
    {
        var hub = new DeviceEventHub();
        using var cts = new CancellationTokenSource(Bound);
        await using var watcher = await DeviceEventWatcher.StartAsync(hub, cts.Token);

        hub.Publish(Added("a"));
        hub.Publish(Added("b"));
        hub.Publish(Added("c"));

        await watcher.WaitForCountAsync(3, "watcher did not receive all three events", cts.Token);
        Assert.Equal(["a", "b", "c"], watcher.Events.Select(e => e.Device.DeviceId));
    }

    /// <summary>ISC-5: concurrent enumerations are independent and see the same ordered sequence.</summary>
    [Fact]
    public async Task WatchAsync_TwoConcurrentWatchers_EachReceiveTheSameOrderedSequence()
    {
        var hub = new DeviceEventHub();
        using var cts = new CancellationTokenSource(Bound);
        await using var first = await DeviceEventWatcher.StartAsync(hub, cts.Token);
        await using var second = await DeviceEventWatcher.StartAsync(hub, cts.Token);

        Assert.Equal(2, hub.WatcherCount);

        hub.Publish(Added("a"));
        hub.Publish(Added("b"));
        hub.Publish(Added("c"));

        await first.WaitForCountAsync(3, "first watcher did not receive every event", cts.Token);
        await second.WaitForCountAsync(3, "second watcher did not receive every event", cts.Token);

        Assert.Equal(["a", "b", "c"], first.Events.Select(e => e.Device.DeviceId));
        Assert.Equal(["a", "b", "c"], second.Events.Select(e => e.Device.DeviceId));
    }

    [Fact]
    public void Publish_WithNoWatchers_DoesNotThrow() => new DeviceEventHub().Publish(Added("a"));

    // ---------- Lazy subscription (ISC-9) ----------

    [Fact]
    public void WatchAsync_WithoutEnumerating_CreatesNoSubscription()
    {
        var hub = new DeviceEventHub();

        _ = hub.WatchAsync(CancellationToken.None);
        _ = hub.WatchAsync(CancellationToken.None);

        // Subscription happens on the first MoveNextAsync, so an un-enumerated sequence must leave
        // nothing behind for Publish to write into.
        Assert.Equal(0, hub.WatcherCount);
    }

    [Fact]
    public async Task WatchAsync_WhenEnumerationStops_ReleasesItsSubscription()
    {
        var hub = new DeviceEventHub();
        using var cts = new CancellationTokenSource(Bound);

        var consumer = Task.Run(
            async () =>
            {
                await foreach (var _ in hub.WatchAsync(cts.Token))
                {
                    break;
                }
            },
            cts.Token);

        await AsyncWait.WaitUntilAsync(() => hub.WatcherCount == 1, "watcher did not subscribe", Bound, cts.Token);
        hub.Publish(Added("a"));
        await consumer;

        await AsyncWait.WaitUntilAsync(
            () => hub.WatcherCount == 0,
            "watcher did not release its subscription after enumeration stopped",
            Bound,
            cts.Token);
    }

    // ---------- Cancellation isolation (ISC-6) ----------

    [Fact]
    public async Task WatchAsync_WhenCancelled_ThrowsOperationCanceled()
    {
        var hub = new DeviceEventHub();
        using var cts = new CancellationTokenSource();
        var watcher = await DeviceEventWatcher.StartAsync(hub, cts.Token);

        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => watcher.Completion);
        await watcher.DisposeAsync();
    }

    [Fact]
    public async Task WatchAsync_CancellingOneWatcher_DoesNotCompleteOrFaultAnother()
    {
        var hub = new DeviceEventHub();
        using var survivorCts = new CancellationTokenSource(Bound);
        using var doomedCts = new CancellationTokenSource();

        await using var survivor = await DeviceEventWatcher.StartAsync(hub, survivorCts.Token);
        var doomed = await DeviceEventWatcher.StartAsync(hub, doomedCts.Token);

        await doomedCts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => doomed.Completion);
        await doomed.DisposeAsync();

        await AsyncWait.WaitUntilAsync(
            () => hub.WatcherCount == 1,
            "the cancelled watcher did not release its subscription",
            Bound,
            survivorCts.Token);

        hub.Publish(Added("a"));
        await survivor.WaitForCountAsync(1, "surviving watcher stopped receiving events", survivorCts.Token);

        Assert.False(survivor.Completion.IsCompleted);
    }

    // ---------- Overflow isolation (ISC-7) ----------

    [Fact]
    public async Task WatchAsync_WhenWatcherFallsTooFarBehind_FaultsRatherThanDroppingSilently()
    {
        var hub = new DeviceEventHub();
        using var cts = new CancellationTokenSource(Bound);
        using var stall = new SemaphoreSlim(0, 1);

        var consumer = Task.Run(
            async () =>
            {
                var stalled = false;
                await foreach (var _ in hub.WatchAsync(cts.Token))
                {
                    if (!stalled)
                    {
                        stalled = true;
                        await stall.WaitAsync(cts.Token);
                    }
                }
            },
            cts.Token);

        await AsyncWait.WaitUntilAsync(() => hub.WatcherCount == 1, "watcher did not subscribe", Bound, cts.Token);

        for (var i = 0; i < DeviceEventHub.WatcherBufferCapacity + 50; i++)
        {
            hub.Publish(Added($"device-{i}"));
        }

        stall.Release();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => consumer);
        Assert.Contains("FindAllAsync", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Publish_WhenOneWatcherOverflows_LeavesOtherWatchersAndThePublisherUnaffected()
    {
        var hub = new DeviceEventHub();
        using var cts = new CancellationTokenSource(Bound);
        using var stall = new SemaphoreSlim(0, 1);

        await using var healthy = await DeviceEventWatcher.StartAsync(hub, cts.Token);

        var overflowing = Task.Run(
            async () =>
            {
                var stalled = false;
                await foreach (var _ in hub.WatchAsync(cts.Token))
                {
                    if (!stalled)
                    {
                        stalled = true;
                        await stall.WaitAsync(cts.Token);
                    }
                }
            },
            cts.Token);

        await AsyncWait.WaitUntilAsync(() => hub.WatcherCount == 2, "watcher did not subscribe", Bound, cts.Token);

        // Paced in batches so only the watcher that is genuinely not draining fills its buffer. An
        // unpaced burst of this size overflows any consumer, which would prove nothing about isolation.
        const int total = DeviceEventHub.WatcherBufferCapacity + 50;
        var publishTime = Stopwatch.StartNew();
        for (var i = 0; i < total; i++)
        {
            hub.Publish(Added($"device-{i}"));
            if (i % 32 == 31)
            {
                await healthy.WaitForCountAsync(i + 1, "healthy watcher fell behind", cts.Token);
            }
        }

        publishTime.Stop();

        stall.Release();
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => overflowing);

        // The publisher never waited on the stalled consumer, and the healthy watcher lost nothing.
        Assert.True(
            publishTime.Elapsed < TimeSpan.FromSeconds(10),
            $"publishing blocked on a stalled watcher for {publishTime.Elapsed}");
        await healthy.WaitForCountAsync(total, "healthy watcher did not receive every event", cts.Token);
        Assert.False(healthy.Completion.IsCompleted);
    }

    /// <summary>
    /// Inverts the old broadcaster contract. A subscriber that threw used to propagate to the
    /// publisher and cut off every later subscriber; now the exception stays inside the consumer's
    /// own enumeration and everyone else — including the publisher — carries on.
    /// </summary>
    [Fact]
    public async Task Publish_WhenOneWatcherThrowsFromItsLoopBody_LeavesOtherWatchersAndThePublisherUnaffected()
    {
        var hub = new DeviceEventHub();
        using var cts = new CancellationTokenSource(Bound);

        await using var healthy = await DeviceEventWatcher.StartAsync(hub, cts.Token);

        var throwing = Task.Run(
            async () =>
            {
                await foreach (var _ in hub.WatchAsync(cts.Token))
                {
                    throw new InvalidOperationException("consumer bug");
                }
            },
            cts.Token);

        await AsyncWait.WaitUntilAsync(() => hub.WatcherCount == 2, "watcher did not subscribe", Bound, cts.Token);

        // Publish is non-throwing even though one watcher is about to blow up on this event.
        hub.Publish(Added("a"));

        var consumerFault = await Assert.ThrowsAsync<InvalidOperationException>(() => throwing);
        Assert.Equal("consumer bug", consumerFault.Message);

        await AsyncWait.WaitUntilAsync(
            () => hub.WatcherCount == 1,
            "the throwing watcher did not release its subscription",
            Bound,
            cts.Token);

        hub.Publish(Added("b"));
        await healthy.WaitForCountAsync(2, "the healthy watcher stopped receiving events", cts.Token);

        Assert.Equal(["a", "b"], healthy.Events.Select(e => e.Device.DeviceId));
        Assert.False(healthy.Completion.IsCompleted);
    }

    // ---------- Completion (ISC-8) ----------

    [Fact]
    public async Task Complete_EndsActiveWatchersNormally()
    {
        var hub = new DeviceEventHub();
        using var cts = new CancellationTokenSource(Bound);
        await using var first = await DeviceEventWatcher.StartAsync(hub, cts.Token);
        await using var second = await DeviceEventWatcher.StartAsync(hub, cts.Token);

        hub.Publish(Added("a"));
        hub.Complete();

        await first.Completion.WaitAsync(Bound, cts.Token);
        await second.Completion.WaitAsync(Bound, cts.Token);

        // Normal completion: not faulted, not cancelled.
        Assert.True(first.EndedNormally);
        Assert.True(second.EndedNormally);
        Assert.Equal(1, first.Count);
        Assert.Equal(1, second.Count);
    }

    [Fact]
    public async Task WatchAsync_AfterComplete_EndsImmediately()
    {
        var hub = new DeviceEventHub();
        hub.Complete();

        var count = 0;
        await foreach (var _ in hub.WatchAsync(TestContext.Current.CancellationToken))
        {
            count++;
        }

        Assert.Equal(0, count);
        Assert.Equal(0, hub.WatcherCount);
    }

    [Fact]
    public async Task Complete_IsIdempotent()
    {
        var hub = new DeviceEventHub();
        using var cts = new CancellationTokenSource(Bound);
        await using var watcher = await DeviceEventWatcher.StartAsync(hub, cts.Token);

        hub.Complete();
        hub.Complete();
        hub.Complete();

        await watcher.Completion.WaitAsync(Bound, cts.Token);
        Assert.True(watcher.EndedNormally);
    }

    [Fact]
    public async Task Publish_AfterComplete_IsANoOp()
    {
        var hub = new DeviceEventHub();
        using var cts = new CancellationTokenSource(Bound);
        await using var watcher = await DeviceEventWatcher.StartAsync(hub, cts.Token);

        hub.Complete();
        hub.Publish(Added("a"));

        await watcher.Completion.WaitAsync(Bound, cts.Token);
        Assert.Empty(watcher.Events);
    }

    // ---------- Concurrency ----------

    [Fact]
    public async Task Publish_ConcurrentWithSubscribeAndUnsubscribe_DoesNotCorruptState()
    {
        var hub = new DeviceEventHub();
        using var cts = new CancellationTokenSource(Bound);
        await using var stable = await DeviceEventWatcher.StartAsync(hub, cts.Token);

        await Parallel.ForAsync(
            0,
            100,
            cts.Token,
            async (i, token) =>
            {
                if (i % 2 == 0)
                {
                    hub.Publish(Added($"device-{i}"));
                }
                else
                {
                    // Subscribe and leave straight away, so subscription churn overlaps delivery.
                    using var churnCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                    var enumerator = hub.WatchAsync(churnCts.Token).GetAsyncEnumerator(churnCts.Token);
                    var move = enumerator.MoveNextAsync();
                    await churnCts.CancelAsync();

                    try
                    {
                        _ = await move;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected: this churner left before an event arrived.
                    }

                    await enumerator.DisposeAsync();
                }
            });

        await stable.WaitForCountAsync(50, "the stable watcher missed events during churn", cts.Token);
        await AsyncWait.WaitUntilAsync(
            () => hub.WatcherCount == 1,
            "churned watchers did not all unsubscribe",
            Bound,
            cts.Token);
        Assert.Equal(50, stable.Count);
    }

    /// <summary>
    /// A subscription racing <see cref="DeviceEventHub.Complete"/> must land on exactly one side of it:
    /// either it is in the completed snapshot, or it completes immediately. Neither may hang.
    /// </summary>
    [Fact]
    public async Task Complete_ConcurrentWithSubscribe_EveryWatcherEndsNormally()
    {
        var hub = new DeviceEventHub();
        using var cts = new CancellationTokenSource(Bound);

        var consumers = Enumerable.Range(0, 32)
            .Select(i => Task.Run(
                async () =>
                {
                    if (i == 16)
                    {
                        hub.Complete();
                    }

                    await foreach (var _ in hub.WatchAsync(cts.Token))
                    {
                        // Drain until the hub completes the sequence.
                    }
                },
                cts.Token))
            .ToArray();

        hub.Complete();

        // Task.WhenAll rethrows any fault or cancellation, so completing normally is the assertion.
        await Task.WhenAll(consumers).WaitAsync(Bound, cts.Token);
        Assert.Equal(0, hub.WatcherCount);
    }
}