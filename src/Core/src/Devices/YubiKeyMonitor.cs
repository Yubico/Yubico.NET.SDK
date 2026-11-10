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
using System.Runtime.ExceptionServices;
using Yubico.YubiKit.Core.Transports;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Tracks connected YubiKeys continuously and streams their changes to any number of subscribers.
/// </summary>
/// <remarks>
/// <para>Create one monitor per application through <see cref="YubiKeyManager.CreateMonitorAsync"/> and share
/// it; every monitor runs its own OS listeners and discovery scans. Creation completes after the first scan
/// has been published, so the first <see cref="WatchAsync"/> replay is never empty because a scan is pending.</para>
/// <para>A monitor has one lifetime: it runs from creation until <see cref="DisposeAsync"/> and cannot be
/// paused or restarted. To resume tracking after disposal, create a new monitor. Disposal does not dispose
/// caller-owned sessions or connections.</para>
/// <para>Concurrent <see cref="WatchAsync"/> enumerations are safe while the monitor runs. <see cref="DisposeAsync"/>
/// must not overlap other members.</para>
/// </remarks>
public sealed class YubiKeyMonitor : IAsyncDisposable
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<YubiKeyMonitor>();

    /// <summary>Fallback rescan interval when no listener reports a change.</summary>
    internal static readonly TimeSpan DefaultRescanInterval = TimeSpan.FromSeconds(5);

    private readonly DeviceChangeFeed _feed = new();
    private readonly IMonitoredDiscovery _discovery;
    private readonly HotplugSignals _signals;
    private readonly RescanScheduler _scheduler;
    private readonly CancellationTokenSource _lifetime = new();

    // A result rather than a faulted task: creation may have stopped awaiting before the first scan failed.
    private readonly TaskCompletionSource<Exception?> _firstPublication = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private Task? _run;
    private Task? _disposal;

    private YubiKeyMonitor(
        IMonitoredDiscovery discovery,
        IReadOnlyList<(ConnectionType Transport, Func<IDeviceChangeListener> Create)> listeners,
        TimeSpan interval,
        TimeProvider timeProvider)
    {
        _discovery = discovery;
        _signals = new HotplugSignals(listeners, Logger);
        _scheduler = new RescanScheduler(_signals.Signals, interval, timeProvider);
    }

    /// <summary>
    /// Replays the connected devices as <see cref="DeviceAdded"/>, emits <see cref="SnapshotComplete"/>, then
    /// streams every later change in order.
    /// </summary>
    /// <param name="cancellationToken">Ends this enumeration only; the monitor keeps running.</param>
    /// <returns>A lazy stream. Nothing is subscribed until the first <c>MoveNextAsync</c>.</returns>
    /// <remarks>
    /// Each enumeration has its own bounded buffer. Keep consuming it: a subscriber that falls more than
    /// 256 changes behind is ended with <see cref="InvalidOperationException"/> instead of silently losing
    /// changes; clear the projection and enumerate again to recover. Changes published before disposal may
    /// still be delivered; an enumeration started after disposal ends immediately.
    /// </remarks>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    /// <exception cref="InvalidOperationException">This subscriber overflowed, or monitoring failed terminally.</exception>
    public IAsyncEnumerable<DeviceChange> WatchAsync(CancellationToken cancellationToken = default) =>
        _feed.Subscribe(cancellationToken);

    internal static Task<YubiKeyMonitor> CreateAsync(CancellationToken cancellationToken) =>
        CreateAsync(
            FindYubiKeys.Create(),
            [(ConnectionType.Hid, HidDeviceListener.Create), (ConnectionType.SmartCard, DesktopSmartCardDeviceListener.Create)],
            DefaultRescanInterval,
            TimeProvider.System,
            cancellationToken);

    /// <summary>Starts listeners and the scan loop, then awaits the first published scan.</summary>
    /// <remarks>
    /// <paramref name="cancellationToken"/> bounds only this wait. A first-scan failure is terminal and
    /// rethrown; either outcome disposes the partially created monitor before returning.
    /// </remarks>
    internal static async Task<YubiKeyMonitor> CreateAsync(
        IMonitoredDiscovery discovery,
        IReadOnlyList<(ConnectionType Transport, Func<IDeviceChangeListener> Create)> listeners,
        TimeSpan interval,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(discovery);
        ArgumentNullException.ThrowIfNull(listeners);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero, nameof(interval));
        cancellationToken.ThrowIfCancellationRequested();

        var monitor = new YubiKeyMonitor(discovery, listeners, interval, timeProvider);
        try
        {
            monitor._run = Task.Run(() => monitor.RunAsync(monitor._lifetime.Token), CancellationToken.None);
            Logger.LogInformation("Device monitoring started with interval {Interval}", interval);

            var failure = await monitor._firstPublication.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            if (failure is not null)
                ExceptionDispatchInfo.Throw(failure);
            return monitor;
        }
        catch
        {
            await monitor.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var trigger in _scheduler.RunAsync(cancellationToken).ConfigureAwait(false))
            {
                if (trigger.Reason == RescanReason.Hotplug)
                    _discovery.InvalidateEvidence(trigger.Transports);

                if (trigger.Reason == RescanReason.Initial)
                {
                    // The first scan must succeed for creation to return; later failures only log and retry.
                    await ScanAndPublishAsync(cancellationToken).ConfigureAwait(false);
                    _ = _firstPublication.TrySetResult(null);
                    continue;
                }

                try
                {
                    await ScanAndPublishAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    Logger.LogWarning(ex, "Device scan after {Reason} failed; retrying on the next trigger", trigger.Reason);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _ = _firstPublication.TrySetCanceled(cancellationToken);
        }
        catch (Exception ex)
        {
            _ = _firstPublication.TrySetResult(ex);
            _feed.Fault(ex);
            try
            {
                Logger.LogError(ex, "Device monitoring terminated unexpectedly");
            }
            catch (Exception)
            {
                // Logging is best-effort; the failure is already observable through WatchAsync.
            }
        }
    }

    private async Task ScanAndPublishAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("Scanning devices");
        var devices = await _discovery.FindAllAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        _feed.Apply(devices);
    }

    /// <summary>
    /// Stops tracking and ends every <see cref="WatchAsync"/> enumeration normally. Idempotent.
    /// </summary>
    /// <remarks>
    /// Returns after the scan loop has stopped, which waits for an in-flight scan to finish within its
    /// discovery budgets. Not safe to call concurrently with other members.
    /// </remarks>
    public ValueTask DisposeAsync() => new(_disposal ??= DisposeCoreAsync());

    private async Task DisposeCoreAsync()
    {
        // Each step removes the only source of concurrency the next step would otherwise have to guard against:
        // stopped listeners cannot signal, a stopped loop cannot scan or publish, a retired discovery
        // cannot open reads, and a completed feed accepts no subscribers.
        _signals.Dispose();
        _lifetime.Cancel();
        if (_run is { } run)
            await run.ConfigureAwait(false);
        _lifetime.Dispose();
        _discovery.Retire();
        _feed.Complete();
        Logger.LogDebug("Device monitoring stopped");
    }
}