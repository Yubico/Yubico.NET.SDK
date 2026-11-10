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

using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Owns continuous discovery and the latest published membership snapshot of connected YubiKeys.
/// </summary>
/// <remarks>
/// Each instance has independent discovery evidence, listeners, and subscriptions. Dispose the monitor
/// when its owner no longer needs tracking; this does not dispose caller-owned sessions or connections.
/// Membership snapshots are read-only, but their device objects can receive later last-known metadata.
/// Creation awaits the worker's first successful publication. Each monitor has one initialize-run-dispose
/// lifetime; recovery after terminal failure requires creating a new monitor, not restarting this one.
/// </remarks>
public sealed class YubiKeyMonitor : IAsyncDisposable
{
    private static readonly TimeSpan DefaultMonitoringInterval = TimeSpan.FromSeconds(5);

    private readonly DisposalGate _disposal = new();
    private readonly DeviceChangeFeed _feed;
    private readonly YubiKeyMonitorWorker _worker;

    internal Func<Task>? RepositoryTeardownReachedForTest;

    private YubiKeyMonitor(DeviceChangeFeed feed, YubiKeyMonitorWorker worker)
    {
        _feed = feed;
        _worker = worker;
    }

    /// <summary>
    /// Replays current devices, emits SnapshotComplete, then streams ordered changes with an independent bounded delta buffer.
    /// </summary>
    /// <param name="cancellationToken">Cancels only this subscription, not the monitor.</param>
    /// <returns>A lazy stream that completes on disposal and faults on terminal monitoring failure.</returns>
    /// <remarks>
    /// On the first MoveNextAsync, membership capture and delta registration share one short publication
    /// boundary. Replay runs outside that boundary and does not occupy the bounded delta buffer. Changes
    /// arriving during replay can overflow that buffer; clear the consumer's projection and re-enumerate
    /// this same monitor to recover. Cancellation and overflow affect only their own subscription.
    /// Replay and events already captured before disposal may still drain. Enumerating after disposal ends empty.
    /// </remarks>
    /// <exception cref="OperationCanceledException">The subscription is canceled.</exception>
    /// <exception cref="InvalidOperationException">The subscriber overflowed or monitoring failed terminally.</exception>
    public IAsyncEnumerable<DeviceChange> WatchAsync(CancellationToken cancellationToken = default) =>
        _feed.Subscribe(cancellationToken);

    internal static Task<YubiKeyMonitor> CreateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var feed = new DeviceChangeFeed();
        var worker = new YubiKeyMonitorWorker(
            feed, discovery: FindYubiKeys.Create(),
            [(ConnectionType.Hid, HidDeviceListener.Create),
                (ConnectionType.SmartCard, DesktopSmartCardDeviceListener.Create)]);
        return CreateAsync(feed, worker, cancellationToken, DefaultMonitoringInterval);
    }

    internal static async Task<YubiKeyMonitor> CreateAsync(
        DeviceChangeFeed feed,
        YubiKeyMonitorWorker worker,
        CancellationToken cancellationToken,
        TimeSpan interval)
    {
        var monitor = new YubiKeyMonitor(feed, worker);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await worker.InitializeAsync(interval, cancellationToken).ConfigureAwait(false);
            return monitor;
        }
        catch
        {
            await monitor.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Stops tracking and completes subscriptions. Idempotent with shared completion.
    /// </summary>
    /// <remarks>
    /// Public access closes first, then the worker stops listeners, drains its sole loop with a ten-second
    /// budget, and retires discovery. The budget does not bound native listener cleanup or total disposal time.
    /// The feed is completed in a finally block, rejecting late membership/change commits even if the
    /// loop is abandoned. Already-open native reads may outlive this call and retain their admission and
    /// lease until actual teardown. Changes committed before feed completion may still drain.
    /// Device descriptors and caller-owned connections remain usable independently of this monitor.
    /// </remarks>
    public ValueTask DisposeAsync() => _disposal.DisposeAsync(DisposeCoreAsync);

    private async ValueTask DisposeCoreAsync()
    {
        try
        {
            await _worker.DisposeAsync().ConfigureAwait(false);
            var hook = RepositoryTeardownReachedForTest;
            if (hook is not null)
                await hook().ConfigureAwait(false);
        }
        finally
        {
            _feed.Complete();
        }
    }
}