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

using Microsoft.Extensions.Logging;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Thread-safe multicast publisher of <see cref="DeviceEvent"/>s to <see cref="IObserver{T}"/>
/// subscribers.
/// </summary>
/// <remarks>
/// <para><see cref="Publish"/> delivers synchronously on the calling thread and in subscription
/// order. An exception from <see cref="IObserver{T}.OnNext"/> propagates to the publisher and stops
/// delivery of that event to later observers.</para>
/// <para><strong>Thread safety:</strong> all members are safe for concurrent use.
/// <see cref="Publish"/> is lock-free: it reads an immutable snapshot of the observer list, so
/// subscribing or unsubscribing from inside a handler cannot disturb an in-flight delivery.
/// Mutations take a short lock and swap in a new array (copy-on-write).</para>
/// <para><see cref="Complete"/> is idempotent. It notifies every current observer, isolates and logs
/// exceptions from <see cref="IObserver{T}.OnCompleted"/>, and immediately completes later
/// subscribers.</para>
/// <para>Concurrent <see cref="Publish"/> and <see cref="Complete"/> calls are state-safe: the
/// broadcaster does not corrupt its subscription list or deadlock itself. Producers that require
/// strict observer grammar must still serialize those calls, because a publication already using
/// its observer snapshot can finish concurrently with completion.</para>
/// <para>This type provides multicast delivery only. Per-consumer buffering is handled by
/// <see cref="DeviceEventStream"/>.</para>
/// </remarks>
internal sealed class DeviceEventBroadcaster : IObservable<DeviceEvent>, IDisposable
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<DeviceEventBroadcaster>();

    private readonly Lock _gate = new();

    /// <summary>Immutable snapshot; only ever replaced, never mutated in place.</summary>
    private IObserver<DeviceEvent>[] _observers = [];

    /// <summary>Guarded by <see cref="_gate"/>; read without the lock only in <see cref="Subscribe"/>'s fast path.</summary>
    private volatile bool _completed;

    /// <summary>
    /// Delivers an event to every current subscriber, synchronously and in subscription order.
    /// </summary>
    /// <param name="deviceEvent">The event to deliver.</param>
    /// <remarks>
    /// No-op once <see cref="Complete"/> has been called. Subscriber exceptions propagate to the
    /// caller and stop delivery to later subscribers. The device monitor relies on this method
    /// completing synchronously before a repository update returns.
    /// </remarks>
    public void Publish(DeviceEvent deviceEvent)
    {
        if (_completed)
        {
            return;
        }

        // The immutable snapshot remains stable if subscriptions change during delivery.
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

        // Never run arbitrary observer code while holding the subscription gate.
        foreach (var observer in snapshot)
        {
            try
            {
                observer.OnCompleted();
            }
#pragma warning disable CA1031 // Terminal cleanup must not be derailed by observer callbacks.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                Logger.LogWarning(ex, "A device event subscriber threw from OnCompleted during shutdown.");
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Subscribing after <see cref="Complete"/> delivers <see cref="IObserver{T}.OnCompleted"/>
    /// immediately and returns a no-op subscription.
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

        // Never run arbitrary observer code while holding the subscription gate.
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
            // Each subscription removes its exact observer instance.
            var index = Array.FindIndex(_observers, o => ReferenceEquals(o, observer));
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