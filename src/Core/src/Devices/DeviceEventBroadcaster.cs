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

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Thread-safe multicast publisher of <see cref="DeviceEvent"/>s to <see cref="IObserver{T}"/>
/// subscribers.
/// </summary>
/// <remarks>
/// <para>
/// This replaces the <c>System.Reactive</c> <c>Subject&lt;DeviceEvent&gt;</c> that previously backed
/// <c>YubiKeyManager.DeviceChanges</c>, removing the SDK's only dependency-level Native AOT finding
/// (<c>IL3058</c>). It implements exactly the behaviour the device-event pipeline relies on and
/// nothing more: no schedulers, no replay, no error channel, no query operators. Consumers who want
/// those can add <c>System.Reactive</c> themselves — every Rx operator is an extension method on
/// <see cref="IObservable{T}"/> and composes with this type unchanged.
/// </para>
/// <para><strong>Write protection:</strong> this type deliberately does <em>not</em> implement
/// <see cref="IObserver{T}"/>. Publishing is done through <see cref="Publish"/> and
/// <see cref="Complete"/>, which are not reachable through any interface the type exposes, so a
/// consumer holding the <see cref="IObservable{T}"/> cannot cast back to an observer and inject
/// events. This is what Rx's <c>AsObservable()</c> wrapper was previously used for.</para>
/// <para><strong>Delivery:</strong> <see cref="Publish"/> delivers synchronously and inline on the
/// calling thread. The device monitor depends on this — its publish gate reasoning assumes a
/// subscriber runs (and therefore completes or throws) before <c>UpdateCache</c> returns.</para>
/// <para><strong>Subscriber exceptions propagate.</strong> An observer that throws from
/// <see cref="IObserver{T}.OnNext"/> aborts the publish loop, so later observers do not receive that
/// event and the exception surfaces to the publisher. This matches the previous
/// <c>Subject&lt;T&gt;</c> behaviour and is pinned by existing monitor-service tests. Whether
/// partial delivery is the right long-term contract is an open design question, deliberately left
/// unchanged here.</para>
/// <para><strong>Thread safety:</strong> all members are safe for concurrent use.
/// <see cref="Publish"/> is lock-free: it reads an immutable snapshot of the observer list, so
/// subscribing or unsubscribing from inside a handler cannot disturb an in-flight delivery.
/// Mutations take a short lock and swap in a new array (copy-on-write).</para>
/// <para><strong>Scope:</strong> this type is only responsible for multicast delivery — who is
/// subscribed and in what order they are notified. Buffering a slow consumer is a separate concern
/// handled by <see cref="DeviceEventStream"/>.</para>
/// </remarks>
internal sealed class DeviceEventBroadcaster : IObservable<DeviceEvent>, IDisposable
{
    private readonly Lock _gate = new();

    /// <summary>Immutable snapshot; only ever replaced, never mutated in place.</summary>
    private IObserver<DeviceEvent>[] _observers = [];

    /// <summary>Guarded by <see cref="_gate"/>; read without the lock only in <see cref="Subscribe"/>'s fast path.</summary>
    private bool _completed;

    /// <summary>
    /// Delivers an event to every current subscriber, synchronously and in subscription order.
    /// </summary>
    /// <param name="deviceEvent">The event to deliver.</param>
    /// <remarks>
    /// No-op once <see cref="Complete"/> has been called. Exceptions thrown by a subscriber are not
    /// caught — see the type-level remarks.
    /// </remarks>
    public void Publish(DeviceEvent deviceEvent)
    {
        // Lock-free: the array is immutable once published, so this snapshot is stable for the whole
        // loop even if someone subscribes or unsubscribes while we are delivering.
        foreach (var observer in Volatile.Read(ref _observers))
        {
            observer.OnNext(deviceEvent);
        }
    }

    /// <summary>
    /// Terminates the sequence, notifying every current subscriber via
    /// <see cref="IObserver{T}.OnCompleted"/>. Idempotent.
    /// </summary>
    /// <remarks>
    /// After this returns, subscribers are released and any later <see cref="Subscribe"/> call
    /// completes immediately.
    /// </remarks>
    public void Complete()
    {
        IObserver<DeviceEvent>[] snapshot;

        lock (_gate)
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            snapshot = _observers;
            _observers = [];
        }

        // Notify outside the lock: an observer is arbitrary user code and must never run while we
        // hold the gate, or a handler that subscribes/unsubscribes would deadlock.
        foreach (var observer in snapshot)
        {
            observer.OnCompleted();
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Subscribing after <see cref="Complete"/> delivers <see cref="IObserver{T}.OnCompleted"/>
    /// immediately and returns a no-op subscription, rather than throwing. A completed sequence
    /// completing its late subscribers is the conventional observable contract, and it keeps the
    /// subscribe/complete race benign: whether a caller lands just before or just after completion,
    /// it observes a clean terminal signal either way.
    /// </remarks>
    public IDisposable Subscribe(IObserver<DeviceEvent> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        lock (_gate)
        {
            if (!_completed)
            {
                _observers = [.. _observers, observer];
                return new Subscription(this, observer);
            }
        }

        // Outside the lock, for the same reason as Complete().
        observer.OnCompleted();

        return NoOpSubscription.Instance;
    }

    /// <summary>
    /// Terminates the sequence. Equivalent to <see cref="Complete"/>; provided so the broadcaster can
    /// participate in the owning type's disposal chain.
    /// </summary>
    public void Dispose() => Complete();

    private void Unsubscribe(IObserver<DeviceEvent> observer)
    {
        lock (_gate)
        {
            var index = Array.IndexOf(_observers, observer);
            if (index < 0)
            {
                return;
            }

            _observers = [.. _observers[..index], .. _observers[(index + 1)..]];
        }
    }

    /// <summary>Handle returned to a subscriber; unsubscribes at most once.</summary>
    private sealed class Subscription(DeviceEventBroadcaster owner, IObserver<DeviceEvent> observer)
        : IDisposable
    {
        private IObserver<DeviceEvent>? _observer = observer;

        public void Dispose()
        {
            // Exchange-to-null makes repeated Dispose calls safe and keeps a double-dispose from
            // removing a same-instance observer that resubscribed in the meantime.
            var target = Interlocked.Exchange(ref _observer, null);
            if (target is not null)
            {
                owner.Unsubscribe(target);
            }
        }
    }

    /// <summary>Returned to subscribers that arrived after completion; there is nothing to release.</summary>
    private sealed class NoOpSubscription : IDisposable
    {
        internal static readonly NoOpSubscription Instance = new();

        private NoOpSubscription()
        {
        }

        public void Dispose()
        {
            // Intentionally empty.
        }
    }
}