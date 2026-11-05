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

using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Asynchronous fan-out of <see cref="DeviceEvent"/>s to <c>await foreach</c> consumers: the single
/// delivery path behind <see cref="YubiKeyMonitor.WatchAsync"/>.
/// </summary>
/// <remarks>
/// <para><strong>The publisher is never handed to a consumer.</strong> <see cref="Publish"/> only writes
/// into per-watcher bounded buffers and returns; no consumer code runs on the publishing thread, and no
/// consumer can block, slow, or fault a publication. The repository can therefore enqueue a complete
/// diff under its short state lock without a slow watcher wedging device monitoring.</para>
/// <para><strong>Every watcher is independent.</strong> Each enumeration owns a
/// <see cref="WatcherBufferCapacity"/>-event buffer. Overflow, cancellation, and abandonment terminate
/// only the watcher they happen to; the publisher and every other watcher continue unaffected.</para>
/// <para><strong>Subscription is lazy.</strong> <see cref="WatchAsync"/> is an async iterator, so nothing
/// is subscribed until the first <c>MoveNextAsync</c>. Events raised between calling it and entering the
/// loop are not observed.</para>
/// <para><strong>Overflow faults instead of dropping.</strong> Device events are deltas, so a consumer
/// that falls behind must rebuild its view rather than silently carry on with a hole in it. Owned
/// monitor watchers clear their projection and re-enumerate the same monitor's replay stream.</para>
/// <para><strong>Thread safety:</strong> all members are safe for concurrent use. <see cref="Publish"/>
/// is lock-free: it reads an immutable snapshot of the watcher list, so subscribing or unsubscribing
/// cannot disturb an in-flight publication. Mutations take a short lock and swap in a new array
/// (copy-on-write).</para>
/// <para><see cref="Complete"/> is idempotent, ends every current watcher (normally or with a supplied
/// terminal monitoring error), and makes later enumerations observe the same outcome. Ordering across
/// concurrent <see cref="Publish"/> calls is the caller's
/// responsibility; the repository enqueues each complete diff under its state lock, and the monitor's
/// sole worker loop awaits each <see cref="YubiKeyDeviceRepository.UpdateCache"/> before scanning again.</para>
/// </remarks>
internal sealed class DeviceEventHub
{
    /// <summary>Per-watcher buffer depth.</summary>
    internal const int WatcherBufferCapacity = 256;

    private readonly Lock _gate = new();

    /// <summary>Immutable snapshot; only ever replaced, never mutated in place.</summary>
    private Subscription[] _watchers = [];

    /// <summary>Guarded by <see cref="_gate"/>.</summary>
    private bool _completed;

    private Exception? _completionError;

    /// <summary>
    /// Number of registered watchers. Diagnostic observability for the lazy-subscription and
    /// unsubscribe-on-exit contracts.
    /// </summary>
    /// <remarks>
    /// Registered, not live: a watcher terminated by overflow stays in the array until its
    /// enumeration is disposed, and is counted until then. That is deliberate — an abandoned
    /// enumeration is a leak, and a count that quietly excluded it would hide exactly the leak this
    /// number exists to expose.
    /// </remarks>
    internal int WatcherCount => Volatile.Read(ref _watchers).Length;

    /// <summary>
    /// Hands <paramref name="deviceEvent"/> to every current watcher's buffer and returns.
    /// </summary>
    /// <param name="deviceEvent">The event to deliver.</param>
    /// <remarks>
    /// Non-blocking and non-throwing. A watcher whose buffer is full is terminated with an overflow
    /// fault; the publisher does not wait for it and no other watcher is affected. No-op once
    /// <see cref="Complete"/> has been called, because completion empties the watcher array and
    /// <see cref="WatchAsync"/> never refills it.
    /// </remarks>
    public void Publish(DeviceEvent deviceEvent)
    {
        // The immutable snapshot remains stable if subscriptions change during delivery.
        foreach (var watcher in Volatile.Read(ref _watchers))
        {
            watcher.Deliver(deviceEvent);
        }
    }

    /// <summary>
    /// Ends the sequence for every current watcher, optionally with a terminal monitoring error. Idempotent.
    /// </summary>
    /// <remarks>
    /// After this returns, watchers are released and any later <see cref="WatchAsync"/> enumeration ends
    /// immediately without yielding anything, or faults with the supplied error.
    /// </remarks>
    public void Complete(Exception? error = null)
    {
        Subscription[] snapshot;

        lock (_gate)
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            _completionError = error;
            snapshot = _watchers;
            _watchers = [];
        }

        foreach (var watcher in snapshot)
        {
            watcher.Complete(error);
        }
    }

    /// <summary>
    /// Streams device events as an async sequence, with an independent buffer per enumeration.
    /// </summary>
    /// <param name="cancellationToken">Cancels this enumeration only.</param>
    /// <returns>A sequence that ends normally when the hub completes.</returns>
    /// <remarks>
    /// The caller must dispose the enumerator — <c>await foreach</c> and <c>await using</c> do this
    /// automatically. Unsubscription happens on disposal, so an abandoned enumeration keeps its buffer
    /// registered with the hub for the lifetime of the process.
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown from enumeration when <paramref name="cancellationToken"/> is canceled.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown from enumeration when an event arrives while this watcher's
    /// <see cref="WatcherBufferCapacity"/>-event buffer is full.
    /// </exception>
    public IAsyncEnumerable<DeviceEvent> WatchAsync(CancellationToken cancellationToken = default) =>
        Iterate(cancellationToken);

    private async IAsyncEnumerable<DeviceEvent> Iterate(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var watcher = Subscribe();
        await foreach (var deviceEvent in watcher.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return deviceEvent;
        }
    }

    // Called directly under the repository's state lock for atomic snapshot capture + registration.
    // Merely obtaining an independent WatchAsync iterator here would defer registration until too late.
    internal Subscription Subscribe()
    {
        var watcher = new Subscription(this);

        lock (_gate)
        {
            if (!_completed)
            {
                _watchers = [.. _watchers, watcher];
                return watcher;
            }

            // Preserve terminal monitoring failure for enumerations that start after loop death.
            watcher.Complete(_completionError);
        }

        // Subscribing after completion yields an already-finished buffer, so the enumeration ends
        // immediately instead of waiting for events that can never arrive.
        return watcher;
    }

    private void Unsubscribe(Subscription watcher)
    {
        lock (_gate)
        {
            // Reference identity: each enumeration owns exactly one watcher instance.
            var index = Array.FindIndex(_watchers, w => ReferenceEquals(w, watcher));
            if (index < 0)
            {
                return;
            }

            _watchers = [.. _watchers[..index], .. _watchers[(index + 1)..]];
        }
    }

    /// <summary>One enumeration's private bounded buffer.</summary>
    internal sealed class Subscription(DeviceEventHub owner) : IDisposable
    {
        private readonly Channel<DeviceEvent> _channel = Channel.CreateBounded<DeviceEvent>(
            new BoundedChannelOptions(WatcherBufferCapacity)
            {
                SingleReader = true,
                SingleWriter = false,

                // Keep reader continuations off the publishing thread so a consumer cannot stall the
                // repository while it holds its short state lock to enqueue a complete diff.
                AllowSynchronousContinuations = false,

                // TryWrite reports a full buffer without blocking the publisher.
                FullMode = BoundedChannelFullMode.Wait
            });

        /// <summary>
        /// Set once the channel can never accept another event, so <see cref="Deliver"/> can bail out
        /// before doing any work. Volatile rather than locked: a stale <c>false</c> only costs one
        /// wasted <c>TryWrite</c>, and the publisher must not take a lock.
        /// </summary>
        private volatile bool _terminated;

        public ChannelReader<DeviceEvent> Reader => _channel.Reader;

        public void Deliver(DeviceEvent deviceEvent)
        {
            // A terminated watcher stays in the publisher's snapshot until its enumerator is disposed,
            // and an abandoned enumerator never disposes. Without this check every later Publish would
            // allocate and format a fresh overflow exception for a buffer that can never accept it —
            // once per event, forever, inside the repository's short state-lock publication boundary.
            if (_terminated)
            {
                return;
            }

            if (_channel.Writer.TryWrite(deviceEvent))
            {
                return;
            }

            _terminated = true;

            // Completing distinguishes a full live buffer from one that is already finished.
            _ = _channel.Writer.TryComplete(new InvalidOperationException(
                $"The device event watcher's {WatcherBufferCapacity}-event buffer was full and the " +
                "sequence was terminated to avoid silently dropping events. " +
                "Clear the consumer's device projection and re-enumerate WatchAsync on the same monitor."));

            // Do not invoke logging providers here: repository publication holds the short state lock.
            // The enumeration's terminal exception carries the fault and recovery instructions.
        }

        public void Complete(Exception? error = null)
        {
            _terminated = true;
            _ = _channel.Writer.TryComplete(error);
        }

        public void Dispose() => owner.Unsubscribe(this);
    }
}