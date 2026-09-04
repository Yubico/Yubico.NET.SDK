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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.UnitTests.Infrastructure;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
/// Tests for <see cref="DeviceEventStream.From"/> — the async-sequence surface that lets
/// consumers observe device events without referencing <c>System.Reactive</c>.
/// </summary>
public class DeviceEventStreamTests
{
    /// <summary>
    /// Subscription bookkeeping happens on a background task; give it a generous bound because a
    /// miss here is a hang, not a slow assert.
    /// </summary>
    private static readonly TimeSpan SubscriptionTimeout = TimeSpan.FromSeconds(10);

    private static DeviceEvent Added(string deviceId) =>
        new(DeviceAction.Added, new FakeYubiKey(deviceId));

    [Fact]
    public async Task WatchAsync_YieldsPublishedEventsInOrder()
    {
        var broadcaster = new DeviceEventBroadcaster();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var collected = new List<string>();
        var consumer = Task.Run(async () =>
        {
            await foreach (var e in DeviceEventStream.From(broadcaster, cts.Token))
            {
                collected.Add(e.Device.DeviceId);
                if (collected.Count == 3)
                {
                    break;
                }
            }
        }, cts.Token);

        await WaitForSubscriberAsync(broadcaster, cts.Token);

        broadcaster.Publish(Added("a"));
        broadcaster.Publish(Added("b"));
        broadcaster.Publish(Added("c"));

        await consumer;

        Assert.Equal(["a", "b", "c"], collected);
    }

    [Fact]
    public async Task WatchAsync_WhenBroadcasterCompletes_StreamEndsWithoutError()
    {
        var broadcaster = new DeviceEventBroadcaster();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var count = 0;
        var consumer = Task.Run(async () =>
        {
            await foreach (var _ in DeviceEventStream.From(broadcaster, cts.Token))
            {
                count++;
            }
        }, cts.Token);

        await WaitForSubscriberAsync(broadcaster, cts.Token);
        broadcaster.Publish(Added("a"));
        broadcaster.Complete();

        // Completing must end the loop normally, not by throwing.
        await consumer;
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task WatchAsync_AfterComplete_EndsImmediately()
    {
        var broadcaster = new DeviceEventBroadcaster();
        broadcaster.Complete();

        var count = 0;
        await foreach (var _ in DeviceEventStream.From(broadcaster, TestContext.Current.CancellationToken))
        {
            count++;
        }

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task WatchAsync_WhenCancelled_ThrowsOperationCanceled()
    {
        var broadcaster = new DeviceEventBroadcaster();
        using var cts = new CancellationTokenSource();

        var consumer = Task.Run(async () =>
        {
            await foreach (var _ in DeviceEventStream.From(broadcaster, cts.Token))
            {
                // no-op
            }
        }, TestContext.Current.CancellationToken);

        await WaitForSubscriberAsync(broadcaster, TestContext.Current.CancellationToken);
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consumer);
    }

    [Fact]
    public async Task WatchAsync_WhenEnumerationStops_UnsubscribesFromBroadcaster()
    {
        var broadcaster = new DeviceEventBroadcaster();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var consumer = Task.Run(async () =>
        {
            await foreach (var _ in DeviceEventStream.From(broadcaster, cts.Token))
            {
                break;
            }
        }, cts.Token);

        await WaitForSubscriberAsync(broadcaster, cts.Token);
        broadcaster.Publish(Added("a"));
        await consumer;

        // Breaking out disposes the enumerator, which must release the underlying subscription.
        await AsyncWait.WaitUntilAsync(
            () => CountSubscribers(broadcaster) == 0,
            "watcher did not unsubscribe after enumeration stopped",
            SubscriptionTimeout,
            cts.Token);
    }

    [Fact]
    public async Task WatchAsync_MultipleConsumers_EachReceiveEveryEvent()
    {
        var broadcaster = new DeviceEventBroadcaster();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        async Task<List<string>> ConsumeTwoAsync()
        {
            var seen = new List<string>();
            await foreach (var e in DeviceEventStream.From(broadcaster, cts.Token))
            {
                seen.Add(e.Device.DeviceId);
                if (seen.Count == 2)
                {
                    break;
                }
            }

            return seen;
        }

        var first = Task.Run(ConsumeTwoAsync, cts.Token);
        var second = Task.Run(ConsumeTwoAsync, cts.Token);

        await AsyncWait.WaitUntilAsync(
            () => CountSubscribers(broadcaster) == 2,
            "both watchers did not subscribe",
            SubscriptionTimeout,
            cts.Token);

        broadcaster.Publish(Added("a"));
        broadcaster.Publish(Added("b"));

        Assert.Equal(["a", "b"], await first);
        Assert.Equal(["a", "b"], await second);
    }

    [Fact]
    public async Task WatchAsync_WhenConsumerFallsTooFarBehind_FaultsRatherThanDroppingSilently()
    {
        var broadcaster = new DeviceEventBroadcaster();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var gate = new SemaphoreSlim(0, 1);
        var consumer = Task.Run(async () =>
        {
            var stalled = false;
            await foreach (var _ in DeviceEventStream.From(broadcaster, cts.Token))
            {
                // Stall once, letting the buffer fill behind us, then drain freely so the
                // enumeration can reach the fault the overflow queued up.
                if (!stalled)
                {
                    stalled = true;
                    await gate.WaitAsync(cts.Token);
                }
            }
        }, cts.Token);

        await WaitForSubscriberAsync(broadcaster, cts.Token);

        // One to get consumed, then far more than the buffer can hold.
        for (var i = 0; i < DeviceEventStream.BufferCapacity + 50; i++)
        {
            broadcaster.Publish(Added($"device-{i}"));
        }

        gate.Release();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => consumer);
        Assert.Contains("FindAllAsync", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WatchAsync_SlowConsumerOverflow_DoesNotDisturbObserverSubscribers()
    {
        // The overflow path must fault only the offending stream. An observer subscribed alongside
        // it has to keep receiving everything.
        var broadcaster = new DeviceEventBroadcaster();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var healthy = new RecordingObserver<DeviceEvent>();
        using var healthySubscription = broadcaster.Subscribe(healthy);

        var gate = new SemaphoreSlim(0, 1);
        var consumer = Task.Run(async () =>
        {
            var stalled = false;
            await foreach (var _ in DeviceEventStream.From(broadcaster, cts.Token))
            {
                if (!stalled)
                {
                    stalled = true;
                    await gate.WaitAsync(cts.Token);
                }
            }
        }, cts.Token);

        await AsyncWait.WaitUntilAsync(
            () => CountSubscribers(broadcaster) == 2,
            "watcher did not subscribe",
            SubscriptionTimeout,
            cts.Token);

        const int total = DeviceEventStream.BufferCapacity + 50;
        for (var i = 0; i < total; i++)
        {
            broadcaster.Publish(Added($"device-{i}"));
        }

        gate.Release();
        await Assert.ThrowsAsync<InvalidOperationException>(() => consumer);

        Assert.Equal(total, healthy.Count);
    }

    // ---------- helpers ----------

    /// <summary>
    /// Subscription happens on a background task, so publishing immediately would race it.
    /// </summary>
    private static Task WaitForSubscriberAsync(DeviceEventBroadcaster broadcaster, CancellationToken token) =>
        AsyncWait.WaitUntilAsync(
            () => CountSubscribers(broadcaster) > 0,
            "watcher did not subscribe",
            SubscriptionTimeout,
            token);

    /// <summary>
    /// Probes the live subscriber count by publishing to a counting observer is not possible without
    /// side effects, so reflect over the private observer array instead. Keeps the production type
    /// free of test-only surface.
    /// </summary>
    private static int CountSubscribers(DeviceEventBroadcaster broadcaster)
    {
        var field = typeof(DeviceEventBroadcaster)
            .GetField("_observers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return ((IObserver<DeviceEvent>[])field!.GetValue(broadcaster)!).Length;
    }
}