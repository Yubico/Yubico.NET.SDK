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
using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Holds the current physical-device snapshot, its revision, and independent bounded subscriptions.
/// Subscribers receive initial membership, a completion marker, and subsequent ordered changes.
/// Only one caller may apply scans, sequentially; applying after completion or failure throws.
/// Call Complete only after the producer has stopped, or Fault to terminate monitoring unexpectedly.
/// </summary>
internal sealed class DeviceChangeFeed
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<DeviceChangeFeed>();
    private Dictionary<string, IYubiKey> _snapshot = [];
    private long _revision;
    private Subscription[] _subscribers = [];
    // One producer diffs outside the gate. Capture + registration and complete publication share this
    // gate so subscribers see a snapshot and its entire tail; channel writes never run consumer code.
    private readonly Lock _gate = new();
    private Exception? _fault;
    private bool _completed;

    /// <summary>Applies one complete observed scan; throws if monitoring has ended.</summary>
    internal void Apply(IReadOnlyList<IYubiKey> observed)
    {
        var revision = _revision + 1;
        var (next, changes) = SnapshotDiff.Apply(_snapshot, observed, revision);
        lock (_gate)
        {
            ThrowIfFaulted();
            if (_completed)
                throw new InvalidOperationException("Device monitoring has completed.");
            _snapshot = next;
            _revision = revision;
            foreach (var change in changes)
            {
                foreach (var subscriber in _subscribers)
                    subscriber.Deliver(change);
            }
        }

        Logger.LogDebug("Scan applied: {Total} devices, {Added} added, {Removed} removed, {Updated} updated",
            next.Count, changes.Count(c => c is DeviceAdded), changes.Count(c => c is DeviceRemoved),
            changes.Count(c => c is DeviceUpdated));
    }

    /// <summary>Replays membership then streams changes; cancellation, overflow, or failure ends this subscription.</summary>
    internal async IAsyncEnumerable<DeviceChange> Subscribe(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Dictionary<string, IYubiKey> snapshot;
        long revision;
        Subscription subscription;
        lock (_gate)
        {
            if (_completed)
                yield break;
            ThrowIfFaulted();
            cancellationToken.ThrowIfCancellationRequested();
            snapshot = _snapshot;
            revision = _revision;
            subscription = new Subscription(this);
            _subscribers = [.. _subscribers, subscription];
        }

        using (subscription)
        {
            foreach (var device in snapshot.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new DeviceAdded(revision, device);
            }
            cancellationToken.ThrowIfCancellationRequested();
            yield return new SnapshotComplete(revision);
            await foreach (var change in subscription.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                yield return change;
        }
    }

    /// <summary>Records the first terminal failure and faults subscriptions with its wrapped error.</summary>
    internal void Fault(Exception error)
    {
        lock (_gate)
        {
            if (_completed || _fault is not null)
                return;
            _fault = error;
            var failure = new InvalidOperationException("Device monitoring terminated unexpectedly.", error);
            foreach (var subscriber in _subscribers)
                subscriber.Complete(failure);
        }
    }

    /// <summary>
    /// Requests normal completion for remaining healthy subscriptions; later subscriptions end empty.
    /// </summary>
    /// <remarks>
    /// Call after the producer has stopped. Existing subscription faults, including overflow, retain
    /// their errors. Captured replay and buffered changes may still drain before the stream ends.
    /// </remarks>
    internal void Complete()
    {
        lock (_gate)
        {
            if (_completed)
                return;
            _completed = true;
            foreach (var subscriber in _subscribers)
                subscriber.Complete();
        }
    }

    private void ThrowIfFaulted()
    {
        if (_fault is { } error)
            throw new InvalidOperationException("Device monitoring terminated unexpectedly.", error);
    }

    private void Unsubscribe(Subscription subscription)
    {
        lock (_gate)
        {
            var index = Array.IndexOf(_subscribers, subscription);
            if (index >= 0)
                _subscribers = [.. _subscribers[..index], .. _subscribers[(index + 1)..]];
        }
    }

    private sealed class Subscription(DeviceChangeFeed owner) : IDisposable
    {
        private const int BufferCapacity = 256;
        private readonly Channel<DeviceChange> _channel = Channel.CreateBounded<DeviceChange>(
            new BoundedChannelOptions(BufferCapacity)
            {
                SingleReader = true,
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.Wait
            });
        private bool _terminated;

        internal ChannelReader<DeviceChange> Reader => _channel.Reader;

        internal void Deliver(DeviceChange change)
        {
            if (_terminated || _channel.Writer.TryWrite(change))
                return;
            Complete(new InvalidOperationException(
                $"The WatchAsync subscriber's {BufferCapacity}-change buffer was full and the " +
                "sequence was terminated to avoid silently dropping changes. " +
                "Clear the consumer's device projection and re-enumerate WatchAsync on the same monitor."));
        }

        internal void Complete(Exception? error = null)
        {
            _terminated = true;
            _ = _channel.Writer.TryComplete(error);
        }

        public void Dispose() => owner.Unsubscribe(this);
    }
}