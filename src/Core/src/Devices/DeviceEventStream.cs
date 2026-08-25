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
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Adapts a push-based <see cref="IObservable{T}"/> of <see cref="DeviceEvent"/>s into a pull-based
/// <see cref="IAsyncEnumerable{T}"/> that consumers can <c>await foreach</c> over.
/// </summary>
/// <remarks>
/// <para>
/// Kept separate from <see cref="DeviceEventBroadcaster"/> on purpose. The broadcaster's job is
/// multicast delivery — who is subscribed, and in what order they are notified. This type's job is
/// buffering policy — how deep a slow consumer may fall behind and what happens when it does. Those
/// change for different reasons, so they are different types.
/// </para>
/// <para>
/// Deliberately concrete rather than a generic <c>IObservable&lt;T&gt;</c> adapter: the SDK has
/// exactly one event stream and is not expected to grow another, so a generic version would be
/// speculative surface with a single call site.
/// </para>
/// </remarks>
internal static class DeviceEventStream
{
    /// <summary>
    /// Per-consumer buffer depth.
    /// </summary>
    /// <remarks>
    /// Device events are driven by physical insertion and removal, so a realistic burst is a handful
    /// of events. 256 is far beyond any human-generated burst: reaching it means the consumer has
    /// stopped draining, which is reported rather than silently absorbed.
    /// </remarks>
    internal const int BufferCapacity = 256;

    private static readonly ILogger Logger = YubiKitLogging.CreateLogger(nameof(DeviceEventStream));

    /// <summary>
    /// Streams <paramref name="source"/> as an async sequence, with an independent buffer per call.
    /// </summary>
    /// <param name="source">The observable to adapt.</param>
    /// <param name="cancellationToken">Stops the stream. Cancelling is the normal way to stop watching.</param>
    /// <returns>A sequence that ends when <paramref name="cancellationToken"/> fires or the source completes.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown from the enumeration when the consumer falls more than <see cref="BufferCapacity"/>
    /// events behind.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <strong>The subscription starts on first enumeration, not when this method is called.</strong>
    /// This is an async iterator, so nothing is subscribed until the first <c>MoveNextAsync</c>. Begin
    /// the <c>await foreach</c> before performing an action that is expected to produce an event,
    /// otherwise events raised in the gap are not observed.
    /// </para>
    /// <para>
    /// <strong>Overflow faults instead of dropping.</strong> A <see cref="DeviceEvent"/> is a delta,
    /// not a snapshot: consumers fold Added/Removed into their own view of what is connected.
    /// Silently discarding one would permanently desynchronise that view — a removal for a device
    /// never seen added, or a device pinned in the list forever. So an overflow ends the stream with
    /// an exception, and the consumer should re-enumerate and resynchronise via
    /// <c>YubiKeyManager.FindAllAsync</c>. Only the offending stream is affected; other subscribers
    /// continue to receive events.
    /// </para>
    /// <para>The stream ends normally, without an exception, if the source completes.</para>
    /// </remarks>
    internal static async IAsyncEnumerable<DeviceEvent> From(
        IObservable<DeviceEvent> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var channel = Channel.CreateBounded<DeviceEvent>(
            new BoundedChannelOptions(BufferCapacity)
            {
                SingleReader = true,
                SingleWriter = false,

                // Keep reader continuations off the publishing thread; otherwise a consumer would run
                // inline inside Publish and could stall the monitor - the very hazard this API avoids.
                AllowSynchronousContinuations = false,

                // With Wait, TryWrite reports failure instead of blocking, which is what lets the
                // publisher stay non-blocking while still detecting overflow.
                FullMode = BoundedChannelFullMode.Wait
            });

        using var subscription = source.Subscribe(new ChannelObserver(channel.Writer));

        await foreach (var deviceEvent in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return deviceEvent;
        }
    }

    /// <summary>Bridges the observer contract onto one consumer's channel.</summary>
    private sealed class ChannelObserver(ChannelWriter<DeviceEvent> writer) : IObserver<DeviceEvent>
    {
        public void OnNext(DeviceEvent value)
        {
            if (writer.TryWrite(value))
            {
                return;
            }

            // Terminating a consumer's stream is a unilateral, hard-to-reproduce action, so it is
            // logged as well as surfaced - otherwise "my app stopped seeing insertions" leaves no trace.
            Logger.LogWarning(
                "Device event stream overflowed its {Capacity}-event buffer; terminating that watcher. " +
                "The consumer is not draining events fast enough.",
                BufferCapacity);

            // Deliberately does not throw: that would propagate into Publish and deny the event to
            // other subscribers. Instead only this consumer's stream is faulted.
            _ = writer.TryComplete(new InvalidOperationException(
                $"The device event stream fell more than {BufferCapacity} events behind and was " +
                "terminated to avoid silently dropping events. Re-enumerate and resynchronise the " +
                "device list via YubiKeyManager.FindAllAsync."));
        }

        public void OnCompleted() => _ = writer.TryComplete();

        public void OnError(Exception error) => _ = writer.TryComplete(error);
    }
}