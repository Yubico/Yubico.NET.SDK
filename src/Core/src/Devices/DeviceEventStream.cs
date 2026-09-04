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
/// Each enumeration subscribes lazily and owns an independent bounded buffer. Source completion
/// ends the sequence, cancellation throws <see cref="OperationCanceledException"/>, and overflow
/// faults only the affected sequence.
/// </remarks>
internal static class DeviceEventStream
{
    /// <summary>
    /// Per-consumer buffer depth.
    /// </summary>
    internal const int BufferCapacity = 256;

    private static readonly ILogger Logger = YubiKitLogging.CreateLogger(nameof(DeviceEventStream));

    /// <summary>
    /// Streams <paramref name="source"/> as an async sequence, with an independent buffer per call.
    /// </summary>
    /// <param name="source">The observable to adapt.</param>
    /// <param name="cancellationToken">Cancels enumeration.</param>
    /// <returns>A sequence that ends normally when the source completes.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown from enumeration when <paramref name="cancellationToken"/> is canceled.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown from enumeration when a new event arrives while the consumer's
    /// <see cref="BufferCapacity"/>-event buffer is full.
    /// </exception>
    /// <remarks>
    /// <para><strong>The subscription starts on first enumeration, not when this method is called.</strong>
    /// This is an async iterator, so nothing is subscribed until the first <c>MoveNextAsync</c>. Begin
    /// the <c>await foreach</c> before performing an action that is expected to produce an event,
    /// otherwise events raised in the gap are not observed.
    /// </para>
    /// <para><strong>Overflow faults instead of dropping.</strong> Device events are deltas, so the
    /// consumer must re-enumerate and resynchronize via <c>YubiKeyManager.FindAllAsync</c> after an
    /// overflow. Other streams and subscribers continue receiving events.</para>
    /// <para>The stream ends normally, without an exception, if the source completes.</para>
    /// </remarks>
    internal static IAsyncEnumerable<DeviceEvent> From(
        IObservable<DeviceEvent> source,
        CancellationToken cancellationToken = default)
    {
        // Validate before returning the lazy iterator.
        ArgumentNullException.ThrowIfNull(source);

        return Iterate(source, cancellationToken);
    }

    private static async IAsyncEnumerable<DeviceEvent> Iterate(
        IObservable<DeviceEvent> source,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<DeviceEvent>(
            new BoundedChannelOptions(BufferCapacity)
            {
                SingleReader = true,
                SingleWriter = false,

                // Keep reader continuations off the publishing thread so a consumer cannot stall
                // the monitor while it holds the repository publication gate.
                AllowSynchronousContinuations = false,

                // TryWrite reports a full buffer without blocking the publisher.
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

            // Completing distinguishes a full live channel from a channel already completed.
            var faulted = writer.TryComplete(new InvalidOperationException(
                $"The device event stream's {BufferCapacity}-event buffer was full and the stream was " +
                "terminated to avoid silently dropping events. Re-enumerate and resynchronize the " +
                "device list via YubiKeyManager.FindAllAsync."));

            // TryWrite also returns false after normal completion. Log only when this call actually
            // terminates a live stream as an overflow.
            if (faulted)
            {
                Logger.LogWarning(
                    "Device event stream overflowed its {Capacity}-event buffer; terminating that watcher. " +
                    "The consumer is not draining events fast enough.",
                    BufferCapacity);
            }
        }

        public void OnCompleted() => _ = writer.TryComplete();

        public void OnError(Exception error) => _ = writer.TryComplete(error);
    }
}