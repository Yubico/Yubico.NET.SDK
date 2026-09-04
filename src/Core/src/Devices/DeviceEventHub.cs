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

using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Asynchronous fan-out of <see cref="DeviceEvent"/>s to <c>await foreach</c> consumers: the single
/// delivery path behind <see cref="YubiKeyManager.WatchAsync"/>.
/// </summary>
/// <remarks>
/// <para><strong>The publisher is never handed to a consumer.</strong> <see cref="Publish"/> only writes
/// into per-watcher bounded buffers and returns; no consumer code runs on the publishing thread, and no
/// consumer can block, slow, or fault a publication. That is what lets the device monitor hold its
/// publication gate across <see cref="YubiKeyDeviceRepository.UpdateCache"/> without a slow watcher
/// wedging device monitoring.</para>
/// <para><strong>Every watcher is independent.</strong> Each enumeration owns a
/// <see cref="WatcherBufferCapacity"/>-event buffer. Overflow, cancellation, and abandonment terminate
/// only the watcher they happen to; the publisher and every other watcher continue unaffected.</para>
/// <para><strong>Subscription is lazy.</strong> <see cref="WatchAsync"/> is an async iterator, so nothing
/// is subscribed until the first <c>MoveNextAsync</c>. Events raised between calling it and entering the
/// loop are not observed.</para>
/// <para><strong>Overflow faults instead of dropping.</strong> Device events are deltas, so a consumer
/// that falls behind must resynchronize via <c>YubiKeyManager.FindAllAsync</c> rather than silently
/// carry on with a hole in its view.</para>
/// <para><strong>Thread safety:</strong> all members are safe for concurrent use. <see cref="Publish"/>
/// is lock-free: it reads an immutable snapshot of the watcher list, so subscribing or unsubscribing
/// cannot disturb an in-flight publication. Mutations take a short lock and swap in a new array
/// (copy-on-write).</para>
/// <para><see cref="Complete"/> is idempotent, ends every current watcher normally, and makes any later
/// enumeration end immediately. Ordering across concurrent <see cref="Publish"/> calls is the caller's
/// responsibility; the monitor's publication gate provides it.</para>
/// </remarks>
internal sealed class DeviceEventHub
{
    /// <summary>Per-watcher buffer depth.</summary>
    internal const int WatcherBufferCapacity = 256;

    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<DeviceEventHub>();

    private readonly Lock _gate = new();

    /// <summary>Immutable snapshot; only ever replaced, never mutated in place.</summary>
    private Watcher[] _watchers = [];

    /// <summary>Guarded by <see cref="_gate"/>.</summary>
    private bool _completed;

    /// <summary>
    /// Number of live subscriptions. Diagnostic observability for the lazy-subscription and
    /// unsubscribe-on-exit contracts.
    /// </summary>
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
    /// Ends the sequence for every current watcher normally — not faulted, not cancelled. Idempotent.
    /// </summary>
    /// <remarks>
    /// After this returns, watchers are released and any later <see cref="WatchAsync"/> enumeration ends
    /// immediately without yielding anything.
    /// </remarks>
    public void Complete()
    {
        Watcher[] snapshot;

        lock (_gate)
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            snapshot = _watchers;
            _watchers = [];
        }

        foreach (var watcher in snapshot)
        {
            watcher.Complete();
        }
    }

    /// <summary>
    /// Streams device events as an async sequence, with an independent buffer per enumeration.
    /// </summary>
    /// <param name="cancellationToken">Cancels this enumeration only.</param>
    /// <returns>A sequence that ends normally when the hub completes.</returns>
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
        var watcher = Subscribe();
        try
        {
            await foreach (var deviceEvent in watcher.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return deviceEvent;
            }
        }
        finally
        {
            // Runs on enumerator disposal, however the loop ended: normal completion, break,
            // cancellation, overflow, or an exception thrown by the consumer's own loop body.
            Unsubscribe(watcher);
        }
    }

    private Watcher Subscribe()
    {
        var watcher = new Watcher();

        lock (_gate)
        {
            if (!_completed)
            {
                _watchers = [.. _watchers, watcher];
                return watcher;
            }
        }

        // Subscribing after completion yields an already-finished buffer, so the enumeration ends
        // immediately instead of waiting for events that can never arrive.
        watcher.Complete();
        return watcher;
    }

    private void Unsubscribe(Watcher watcher)
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
    private sealed class Watcher
    {
        private readonly Channel<DeviceEvent> _channel = Channel.CreateBounded<DeviceEvent>(
            new BoundedChannelOptions(WatcherBufferCapacity)
            {
                SingleReader = true,
                SingleWriter = false,

                // Keep reader continuations off the publishing thread so a consumer cannot stall the
                // monitor while it holds the repository publication gate.
                AllowSynchronousContinuations = false,

                // TryWrite reports a full buffer without blocking the publisher.
                FullMode = BoundedChannelFullMode.Wait
            });

        public ChannelReader<DeviceEvent> Reader => _channel.Reader;

        public void Deliver(DeviceEvent deviceEvent)
        {
            if (_channel.Writer.TryWrite(deviceEvent))
            {
                return;
            }

            // Completing distinguishes a full live buffer from one that is already finished.
            var faulted = _channel.Writer.TryComplete(new InvalidOperationException(
                $"The device event watcher's {WatcherBufferCapacity}-event buffer was full and the " +
                "sequence was terminated to avoid silently dropping events. Re-enumerate and " +
                "resynchronize the device list via YubiKeyManager.FindAllAsync."));

            // TryWrite also returns false after normal completion. Log only when this call actually
            // terminates a live watcher as an overflow.
            if (faulted)
            {
                Logger.LogWarning(
                    "A device event watcher overflowed its {Capacity}-event buffer and was terminated. " +
                    "That consumer is not draining events fast enough; other watchers are unaffected.",
                    WatcherBufferCapacity);
            }
        }

        public void Complete() => _ = _channel.Writer.TryComplete();
    }
}