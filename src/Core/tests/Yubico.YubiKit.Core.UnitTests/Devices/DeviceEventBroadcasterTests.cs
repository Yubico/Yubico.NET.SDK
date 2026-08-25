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
/// White-box tests for <see cref="DeviceEventBroadcaster"/>, the multicast primitive that replaced
/// the <c>System.Reactive</c> <c>Subject&lt;DeviceEvent&gt;</c>.
/// </summary>
/// <remarks>
/// These test the primitive itself and are deliberately not written as usage examples — consumer-
/// facing usage is demonstrated by the repository, manager, and integration tests.
/// </remarks>
public class DeviceEventBroadcasterTests
{
    private static DeviceEvent Event(string deviceId = "device-1") =>
        new(DeviceAction.Added, new StubYubiKey(deviceId));

    // ---------- Multicast delivery ----------

    [Fact]
    public void Publish_WithMultipleSubscribers_DeliversToAll()
    {
        using var broadcaster = new DeviceEventBroadcaster();
        var first = new RecordingObserver<DeviceEvent>();
        var second = new RecordingObserver<DeviceEvent>();
        using var s1 = broadcaster.Subscribe(first);
        using var s2 = broadcaster.Subscribe(second);

        broadcaster.Publish(Event());

        Assert.Single(first.Items);
        Assert.Single(second.Items);
    }

    [Fact]
    public void Publish_DeliversEventsInOrder()
    {
        using var broadcaster = new DeviceEventBroadcaster();
        var observer = new RecordingObserver<DeviceEvent>();
        using var subscription = broadcaster.Subscribe(observer);

        broadcaster.Publish(Event("a"));
        broadcaster.Publish(Event("b"));
        broadcaster.Publish(Event("c"));

        Assert.Equal(["a", "b", "c"], observer.Items.Select(e => e.Device.DeviceId));
    }

    [Fact]
    public void Publish_WithNoSubscribers_DoesNotThrow()
    {
        using var broadcaster = new DeviceEventBroadcaster();

        broadcaster.Publish(Event());
    }

    // ---------- Subscription lifetime ----------

    [Fact]
    public void Publish_AfterUnsubscribe_DoesNotDeliverToUnsubscribed()
    {
        using var broadcaster = new DeviceEventBroadcaster();
        var staying = new RecordingObserver<DeviceEvent>();
        var leaving = new RecordingObserver<DeviceEvent>();
        using var s1 = broadcaster.Subscribe(staying);
        var s2 = broadcaster.Subscribe(leaving);

        s2.Dispose();
        broadcaster.Publish(Event());

        Assert.Single(staying.Items);
        Assert.Empty(leaving.Items);
    }

    [Fact]
    public void Subscription_Dispose_IsIdempotent()
    {
        using var broadcaster = new DeviceEventBroadcaster();
        var observer = new RecordingObserver<DeviceEvent>();
        var subscription = broadcaster.Subscribe(observer);

        subscription.Dispose();
        subscription.Dispose();
        subscription.Dispose();

        broadcaster.Publish(Event());
        Assert.Empty(observer.Items);
    }

    [Fact]
    public void Subscribe_SameObserverTwice_ReceivesEventTwice()
    {
        using var broadcaster = new DeviceEventBroadcaster();
        var observer = new RecordingObserver<DeviceEvent>();
        using var s1 = broadcaster.Subscribe(observer);
        using var s2 = broadcaster.Subscribe(observer);

        broadcaster.Publish(Event());

        Assert.Equal(2, observer.Count);
    }

    [Fact]
    public void Subscribe_NullObserver_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new DeviceEventBroadcaster().Subscribe(null!));

    // ---------- Snapshot semantics during delivery ----------

    [Fact]
    public void Subscribe_DuringPublish_DoesNotReceiveInFlightEvent()
    {
        using var broadcaster = new DeviceEventBroadcaster();
        var lateObserver = new RecordingObserver<DeviceEvent>();
        IDisposable? lateSubscription = null;

        var reentrant = new RecordingObserver<DeviceEvent>(_ =>
            lateSubscription ??= broadcaster.Subscribe(lateObserver));
        using var subscription = broadcaster.Subscribe(reentrant);

        broadcaster.Publish(Event());

        // The in-flight delivery iterates an immutable snapshot taken before the new subscription.
        Assert.Empty(lateObserver.Items);

        broadcaster.Publish(Event());
        Assert.Single(lateObserver.Items);

        lateSubscription?.Dispose();
    }

    [Fact]
    public void Unsubscribe_DuringPublish_DoesNotDisturbInFlightDelivery()
    {
        using var broadcaster = new DeviceEventBroadcaster();
        var second = new RecordingObserver<DeviceEvent>();
        IDisposable? secondSubscription = null;

        var first = new RecordingObserver<DeviceEvent>(_ => secondSubscription!.Dispose());
        using var firstSubscription = broadcaster.Subscribe(first);
        secondSubscription = broadcaster.Subscribe(second);

        broadcaster.Publish(Event());

        // Already in the snapshot, so it still receives this event; it is gone from the next one.
        Assert.Single(second.Items);

        broadcaster.Publish(Event());
        Assert.Single(second.Items);
    }

    // ---------- Exception propagation (matches previous Subject<T> behaviour) ----------

    [Fact]
    public void Publish_SubscriberThrows_PropagatesToPublisher()
    {
        using var broadcaster = new DeviceEventBroadcaster();
        var throwing = new RecordingObserver<DeviceEvent>(_ => throw new InvalidOperationException("boom"));
        using var subscription = broadcaster.Subscribe(throwing);

        var ex = Assert.Throws<InvalidOperationException>(() => broadcaster.Publish(Event()));
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public void Publish_SubscriberThrows_LaterSubscribersDoNotReceiveThatEvent()
    {
        // Pins the inherited Subject<T> partial-delivery contract. Recorded as a deferred design
        // question; changing it is deliberately out of scope for the Rx removal.
        using var broadcaster = new DeviceEventBroadcaster();
        var later = new RecordingObserver<DeviceEvent>();
        var throwing = new RecordingObserver<DeviceEvent>(_ => throw new InvalidOperationException("boom"));
        using var s1 = broadcaster.Subscribe(throwing);
        using var s2 = broadcaster.Subscribe(later);

        Assert.Throws<InvalidOperationException>(() => broadcaster.Publish(Event()));

        Assert.Empty(later.Items);
    }

    // ---------- Completion ----------

    [Fact]
    public void Complete_NotifiesAllSubscribers()
    {
        using var broadcaster = new DeviceEventBroadcaster();
        var first = new RecordingObserver<DeviceEvent>();
        var second = new RecordingObserver<DeviceEvent>();
        using var s1 = broadcaster.Subscribe(first);
        using var s2 = broadcaster.Subscribe(second);

        broadcaster.Complete();

        Assert.True(first.IsCompleted);
        Assert.True(second.IsCompleted);
    }

    [Fact]
    public void Complete_IsIdempotent()
    {
        using var broadcaster = new DeviceEventBroadcaster();
        var observer = new RecordingObserver<DeviceEvent>();
        using var subscription = broadcaster.Subscribe(observer);

        broadcaster.Complete();
        broadcaster.Complete();
        broadcaster.Complete();

        Assert.Equal(1, observer.CompletedCount);
    }

    [Fact]
    public void Publish_AfterComplete_IsNoOp()
    {
        using var broadcaster = new DeviceEventBroadcaster();
        var observer = new RecordingObserver<DeviceEvent>();
        using var subscription = broadcaster.Subscribe(observer);

        broadcaster.Complete();
        broadcaster.Publish(Event());

        Assert.Empty(observer.Items);
    }

    [Fact]
    public void Subscribe_AfterComplete_CompletesImmediatelyAndDoesNotThrow()
    {
        // Decision pin: a completed sequence completes late subscribers rather than throwing
        // ObjectDisposedException (which is what the Rx Subject did). This behaviour was previously
        // unspecified and untested.
        using var broadcaster = new DeviceEventBroadcaster();
        broadcaster.Complete();

        var observer = new RecordingObserver<DeviceEvent>();
        using var subscription = broadcaster.Subscribe(observer);

        Assert.True(observer.IsCompleted);
        Assert.Empty(observer.Items);
    }

    [Fact]
    public void Subscribe_AfterComplete_ReturnsDisposableSubscription()
    {
        using var broadcaster = new DeviceEventBroadcaster();
        broadcaster.Complete();

        var subscription = broadcaster.Subscribe(new RecordingObserver<DeviceEvent>());

        subscription.Dispose();
        subscription.Dispose();
    }

    // ---------- Disposal ----------

    [Fact]
    public void Dispose_CompletesSubscribers()
    {
        var broadcaster = new DeviceEventBroadcaster();
        var observer = new RecordingObserver<DeviceEvent>();
        using var subscription = broadcaster.Subscribe(observer);

        broadcaster.Dispose();

        Assert.True(observer.IsCompleted);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var broadcaster = new DeviceEventBroadcaster();
        var observer = new RecordingObserver<DeviceEvent>();
        using var subscription = broadcaster.Subscribe(observer);

        broadcaster.Dispose();
        broadcaster.Dispose();

        Assert.Equal(1, observer.CompletedCount);
    }

    // ---------- Concurrency ----------

    [Fact]
    public void Publish_ConcurrentWithSubscribeAndUnsubscribe_DoesNotCorruptState()
    {
        using var broadcaster = new DeviceEventBroadcaster();
        var stable = new RecordingObserver<DeviceEvent>();
        using var stableSubscription = broadcaster.Subscribe(stable);

        Parallel.For(0, 100, i =>
        {
            if (i % 2 == 0)
            {
                broadcaster.Publish(Event());
            }
            else
            {
                var churn = broadcaster.Subscribe(new RecordingObserver<DeviceEvent>());
                churn.Dispose();
            }
        });

        // The stable observer must have seen every publish, and no more than that.
        Assert.Equal(50, stable.Count);
    }

    [Fact]
    public void Complete_ConcurrentWithSubscribe_EveryObserverEndsCompleted()
    {
        using var broadcaster = new DeviceEventBroadcaster();
        var observers = new List<RecordingObserver<DeviceEvent>>();

        Parallel.For(0, 100, i =>
        {
            var observer = new RecordingObserver<DeviceEvent>();
            lock (observers)
            {
                observers.Add(observer);
            }

            _ = broadcaster.Subscribe(observer);

            if (i == 50)
            {
                broadcaster.Complete();
            }
        });

        broadcaster.Complete();

        // Whether a subscription landed before or after completion, it must observe exactly one
        // terminal signal - never zero, never two.
        Assert.All(observers, o => Assert.Equal(1, o.CompletedCount));
    }
}