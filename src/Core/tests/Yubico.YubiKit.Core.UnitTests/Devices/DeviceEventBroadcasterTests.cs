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
/// White-box tests for <see cref="DeviceEventBroadcaster"/>.
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
        var broadcaster = new DeviceEventBroadcaster();
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
        var broadcaster = new DeviceEventBroadcaster();
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
        var broadcaster = new DeviceEventBroadcaster();

        broadcaster.Publish(Event());
    }

    // ---------- Subscription lifetime ----------

    [Fact]
    public void Publish_AfterUnsubscribe_DoesNotDeliverToUnsubscribed()
    {
        var broadcaster = new DeviceEventBroadcaster();
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
        var broadcaster = new DeviceEventBroadcaster();
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
        var broadcaster = new DeviceEventBroadcaster();
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
        var broadcaster = new DeviceEventBroadcaster();
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
        var broadcaster = new DeviceEventBroadcaster();
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

    // ---------- Exception propagation ----------

    [Fact]
    public void Publish_SubscriberThrows_PropagatesToPublisher()
    {
        var broadcaster = new DeviceEventBroadcaster();
        var throwing = new RecordingObserver<DeviceEvent>(_ => throw new InvalidOperationException("boom"));
        using var subscription = broadcaster.Subscribe(throwing);

        var ex = Assert.Throws<InvalidOperationException>(() => broadcaster.Publish(Event()));
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public void Publish_SubscriberThrows_LaterSubscribersDoNotReceiveThatEvent()
    {
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
        var broadcaster = new DeviceEventBroadcaster();
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
        var broadcaster = new DeviceEventBroadcaster();
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
        // Also the observable-grammar guarantee for serialised producers - which is what the
        // monitor's publish gate provides: a publish that BEGINS after completion delivers nothing,
        // so such a producer never sees OnNext after OnCompleted.
        //
        // The unsynchronised case (a publish that captured its snapshot before Complete ran) is
        // deliberately NOT defended against: doing so requires holding a lock across arbitrary
        // subscriber code, which would let a blocking subscriber wedge start/stop/dispose. See the
        // remarks on DeviceEventBroadcaster and src/Core/CLAUDE.md.
        var broadcaster = new DeviceEventBroadcaster();
        var observer = new RecordingObserver<DeviceEvent>();
        using var subscription = broadcaster.Subscribe(observer);

        broadcaster.Complete();
        broadcaster.Publish(Event());

        Assert.Empty(observer.Items);
        Assert.Equal(1, observer.CompletedCount);
    }

    [Fact]
    public void Subscribe_AfterComplete_CompletesImmediatelyAndDoesNotThrow()
    {
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
        var broadcaster = new DeviceEventBroadcaster();
        broadcaster.Complete();

        var subscription = broadcaster.Subscribe(new RecordingObserver<DeviceEvent>());

        subscription.Dispose();
        subscription.Dispose();
    }

    // ---------- Terminal-notification isolation ----------

    [Fact]
    public void Complete_WhenOneSubscriberThrows_StillNotifiesTheRest()
    {
        // Completion runs during disposal, exactly when a subscriber is likely tearing down its own
        // state. A throwing observer must not starve the others of their terminal signal - an async
        // consumer whose channel is never completed would hang - nor derail the caller's cleanup.
        var broadcaster = new DeviceEventBroadcaster();
        var throwing = new ThrowOnCompletedObserver();
        var later = new RecordingObserver<DeviceEvent>();
        using var s1 = broadcaster.Subscribe(throwing);
        using var s2 = broadcaster.Subscribe(later);

        broadcaster.Complete();

        Assert.True(later.IsCompleted);
    }

    // ---------- Observable grammar ----------

    [Fact]
    public async Task Complete_WhileASubscriberIsBlockedInOnNext_DoesNotWedge()
    {
        // Guards the documented lifecycle invariant: a blocking DeviceChanges subscriber must not be
        // able to wedge start/stop/dispose. This is the constraint that rules out serialising
        // OnNext against OnCompleted inside the broadcaster.
        var broadcaster = new DeviceEventBroadcaster();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();

        var blocking = new RecordingObserver<DeviceEvent>(_ =>
        {
            entered.Set();
            release.Wait(TimeSpan.FromSeconds(30));
        });
        using var subscription = broadcaster.Subscribe(blocking);

        var publish = Task.Run(() => broadcaster.Publish(Event()), TestContext.Current.CancellationToken);
        Assert.True(
            entered.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken),
            "subscriber never entered OnNext");

        // Must return while the subscriber is still blocked inside OnNext.
        broadcaster.Complete();

        release.Set();
        await publish.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Broadcaster_DoesNotExposeObserverSurface()
    {
        var broadcasterType = typeof(DeviceEventBroadcaster);

        Assert.False(typeof(IObserver<DeviceEvent>).IsAssignableFrom(broadcasterType));
        Assert.Null(broadcasterType.GetMethod(nameof(IObserver<DeviceEvent>.OnNext), [typeof(DeviceEvent)]));
        Assert.Null(broadcasterType.GetMethod(nameof(IObserver<DeviceEvent>.OnCompleted), Type.EmptyTypes));
    }

    // ---------- Subscription identity ----------

    [Fact]
    public void Unsubscribe_WithObserversThatCompareEqual_RemovesOnlyTheDisposedSubscription()
    {
        // Subscriptions are identity-based. Array.IndexOf would use EqualityComparer<T>.Default and
        // remove whichever instance compares equal first - and DeviceChanges is public API, so a
        // consumer may well subscribe a record type that overrides Equals.
        var broadcaster = new DeviceEventBroadcaster();
        var first = new EquatableObserver("same");
        var second = new EquatableObserver("same");
        Assert.Equal(first, second);

        using var firstSubscription = broadcaster.Subscribe(first);
        var secondSubscription = broadcaster.Subscribe(second);

        secondSubscription.Dispose();
        broadcaster.Publish(Event());

        Assert.Equal(1, first.Count);
        Assert.Equal(0, second.Count);
    }

    // ---------- Concurrency ----------

    [Fact]
    public void Publish_ConcurrentWithSubscribeAndUnsubscribe_DoesNotCorruptState()
    {
        var broadcaster = new DeviceEventBroadcaster();
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
        var broadcaster = new DeviceEventBroadcaster();
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

    private sealed class ThrowOnCompletedObserver : IObserver<DeviceEvent>
    {
        public void OnNext(DeviceEvent value)
        {
        }

        public void OnCompleted() => throw new ObjectDisposedException("SubscriberOwnedResource");

        public void OnError(Exception error)
        {
        }
    }

    /// <summary>Two instances with the same key compare equal but are distinct references.</summary>
    private sealed record EquatableObserver(string Key) : IObserver<DeviceEvent>
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public void OnNext(DeviceEvent value) => Interlocked.Increment(ref _count);

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }
    }
}
