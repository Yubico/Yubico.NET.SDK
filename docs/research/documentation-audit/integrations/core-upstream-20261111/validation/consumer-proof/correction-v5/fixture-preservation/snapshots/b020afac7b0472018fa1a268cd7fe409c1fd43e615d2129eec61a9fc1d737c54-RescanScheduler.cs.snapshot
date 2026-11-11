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

/// <summary>Schedules initial, fallback, and coalesced hotplug rescans using the supplied clock.</summary>
internal sealed class RescanScheduler
{
    /// <summary>Quiet period after the last listener hint before a coalesced rescan runs.</summary>
    internal static readonly TimeSpan QuietPeriod = TimeSpan.FromMilliseconds(200);

    /// <summary>Maximum time listener hints may be coalesced before forcing a rescan.</summary>
    internal static readonly TimeSpan MaxCoalesce = 5 * QuietPeriod;

    private readonly ChannelReader<ConnectionType> _signals;
    private readonly TimeSpan _interval;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates a scheduler; the caller must enumerate it as the channel's sole reader.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="interval"/> is not positive.</exception>
    internal RescanScheduler(ChannelReader<ConnectionType> signals, TimeSpan interval, TimeProvider timeProvider)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);
        _signals = signals;
        _interval = interval;
        _timeProvider = timeProvider;
    }

    /// <summary>Yields scan triggers sequentially until the channel completes. Dispose the enumeration when finished.</summary>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled.</exception>
    internal async IAsyncEnumerable<RescanTrigger> RunAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return RescanTrigger.Initial;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var trigger = await ReadAsync(_interval, cancellationToken).ConfigureAwait(false);
            if (trigger is { Reason: RescanReason.Hotplug } hotplug)
                trigger = await CoalesceAsync(hotplug.Transports, cancellationToken).ConfigureAwait(false);
            if (trigger is null)
                yield break;

            cancellationToken.ThrowIfCancellationRequested();
            yield return trigger.Value;
        }
    }

    private async ValueTask<RescanTrigger?> CoalesceAsync(ConnectionType transports, CancellationToken cancellationToken)
    {
        var started = _timeProvider.GetTimestamp();
        while (true)
        {
            var remaining = MaxCoalesce - _timeProvider.GetElapsedTime(started);
            if (remaining <= TimeSpan.Zero)
                break;
            var timeout = remaining < QuietPeriod ? remaining : QuietPeriod;
            var next = await ReadAsync(timeout, cancellationToken).ConfigureAwait(false);
            if (next is null)
                return null;
            if (next.Value.Reason == RescanReason.Interval)
                break;
            transports |= next.Value.Transports;
        }
        return RescanTrigger.Hotplug(transports);
    }

    private async ValueTask<RescanTrigger?> ReadAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        // CancelAfter has no TimeProvider overload. A provider-owned timer gives the one linked
        // source the same deadline without a second source or a competing asynchronous wait.
        // The timer state is always the non-null source passed below.
        await using var timer = _timeProvider.CreateTimer(static state => ((CancellationTokenSource)state!).Cancel(),
            cts, timeout, Timeout.InfiniteTimeSpan);
        try
        {
            return RescanTrigger.Hotplug(await _signals.ReadAsync(cts.Token).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return RescanTrigger.Interval;
        }
        catch (ChannelClosedException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }
    }
}

/// <summary>The cause and affected transports for one scheduled rescan.</summary>
internal readonly record struct RescanTrigger(RescanReason Reason, ConnectionType Transports)
{
    internal static RescanTrigger Initial => new(RescanReason.Initial, ConnectionType.Unknown);
    internal static RescanTrigger Interval => new(RescanReason.Interval, ConnectionType.Unknown);
    internal static RescanTrigger Hotplug(ConnectionType transports) => new(RescanReason.Hotplug, transports);
}

/// <summary>Identifies the event that scheduled a rescan.</summary>
internal enum RescanReason
{
    Initial,
    Interval,
    Hotplug
}