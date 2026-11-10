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
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using Yubico.YubiKit.Core.Transports;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Owns listeners and one sequential discovery/publication loop, borrowing the owner's change feed.
/// </summary>
/// <remarks>
/// Initialized once before public exposure, then retired by <see cref="YubiKeyMonitor"/>. Initial,
/// listener-triggered, and fallback scans all await full publication inside the same loop; there is no
/// manual rescan, restart, or replacement monitor generation. Listener teardown and the managed-loop
/// drain precede final discovery retirement; discovery serializes evidence invalidation internally.
/// The owner completes the feed after the worker's one bounded managed-loop drain.
/// </remarks>
internal sealed class YubiKeyMonitorWorker : IAsyncDisposable
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<YubiKeyMonitorWorker>();

    /// <summary>Quiet period after the last listener hint before a coalesced rescan runs.</summary>
    internal static readonly TimeSpan ThrottleInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>Maximum time listener hints may be coalesced before forcing a rescan.</summary>
    internal static readonly TimeSpan MaxCoalesceInterval = 5 * ThrottleInterval;

    /// <summary>Bounds the managed loop drain, not native listener teardown or total disposal time.</summary>
    private static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(10);

    private readonly DeviceChangeFeed _feed;
    private readonly IMonitoredDiscovery _discovery;
    private readonly TimeSpan _shutdownTimeout;
    private readonly IReadOnlyList<(ConnectionType Transport, Func<IDeviceChangeListener> Create)> _listeners;
    private readonly TaskCompletionSource<Exception?> _initialScan = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Not disposed: an abandoned loop may still register with its token. This timer-less source and
    // its registrations remain owned by that work; linked wait sources are disposed by the loop itself.
    private readonly CancellationTokenSource _lifetime = new();

    private HotplugSignals? _hotplugSignals;
    private Task? _monitoringTask;
    private volatile int _disposed;

    /// <summary>Test-only pause after the lifetime precheck and before feed publication.</summary>
    internal Func<Task>? PublishAdmittedForTest;

    /// <summary>Test-only observation/fault injection at the loop's wait boundary.</summary>
    internal Action? MonitoringWaitReachedForTest;

    /// <summary>Test-only replacement of terminal logging, without changing the process-wide logger.</summary>
    internal Action<Exception>? TerminalFailureLogForTest;

    /// <summary>Read-only observation of the one loop task, retained through disposal.</summary>
    internal Task MonitoringCompletionForTest => _monitoringTask ?? Task.CompletedTask;

    /// <summary>Starts the sole lifetime and awaits its first successful feed publication.</summary>
    internal async Task InitializeAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero, nameof(interval));
        Debug.Assert(_monitoringTask is null, "Owned construction initializes once, before public exposure.");

        _hotplugSignals = new HotplugSignals(_listeners, Logger);
        _monitoringTask = Task.Run(() => MonitoringLoopAsync(interval, _lifetime.Token));

        Logger.LogInformation("Device monitoring started with interval {Interval}", interval);

        // Creation cancellation only cancels this wait. The owner disposes partial initialization;
        // the loop's lifetime is independent of the caller's creation token after successful return.
        var failure = await _initialScan.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (failure is not null)
            ExceptionDispatchInfo.Throw(failure);
        cancellationToken.ThrowIfCancellationRequested();
    }

    internal YubiKeyMonitorWorker(
        DeviceChangeFeed feed,
        IMonitoredDiscovery discovery,
        IReadOnlyList<(ConnectionType Transport, Func<IDeviceChangeListener> Create)> listeners,
        TimeSpan? shutdownTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(feed);
        ArgumentNullException.ThrowIfNull(discovery);
        ArgumentNullException.ThrowIfNull(listeners);

        _feed = feed;
        _discovery = discovery;
        _shutdownTimeout = shutdownTimeout ?? DefaultShutdownTimeout;
        _listeners = listeners;
    }

    private async Task ScanAndPublishAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Logger.LogDebug("Rescanning devices...");
        var devices = await _discovery.FindAllAsync(cancellationToken).ConfigureAwait(false);

        // A finder may ignore cancellation. Reject its late result before entering snapshot diffing.
        if (_disposed == 1)
            return;
        cancellationToken.ThrowIfCancellationRequested();

        var admittedHook = PublishAdmittedForTest;
        if (admittedHook is not null)
            await admittedHook().ConfigureAwait(false);

        try
        {
            // Only the loop calls this helper and awaits the complete publication before scanning again.
            // Do not lock across the diff: getters/loggers can block. If this outlives the bounded drain,
            // the owner's feed completion and its final gate check reject the late commit.
            _feed.Apply(devices);
        }
        catch (ObjectDisposedException) when (_disposed == 1)
        {
            // Existing collaborators or hooks may throw during retirement; live failures must surface.
        }
    }

    private async Task MonitoringLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        try
        {
            await ScanAndPublishAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            _ = _initialScan.TrySetResult(null);

            while (!cancellationToken.IsCancellationRequested)
            {
                MonitoringWaitReachedForTest?.Invoke();
                var trigger = await WaitForTriggerAsync(interval, cancellationToken).ConfigureAwait(false);
                if (trigger == DeviceMonitorWaitResult.Timeout)
                {
                    await RescanSafelyAsync("interval fallback", cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (trigger == DeviceMonitorWaitResult.Completed)
                    break;

                ConsumeSignal();
                if (!await WaitForDebounceQuietPeriodAsync(cancellationToken).ConfigureAwait(false))
                    break;

                await RescanSafelyAsync("listener event", cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("The device monitoring signal completed unexpectedly.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _ = _initialScan.TrySetCanceled(cancellationToken);
        }
        catch (Exception ex)
        {
            FailLoop(ex);
        }
    }

    private void FailLoop(Exception error)
    {
        // A result rather than a faulted TCS avoids an unobserved exception if initialization's
        // wait was canceled while a native scan continued. The factory rethrows the original error.
        _ = _initialScan.TrySetResult(error);
        if (_disposed == 1)
            return;

        // Failure uses the feed's short publication boundary, never the diff lock.
        _feed.Fault(error);

        // Publish the causal failure first: diagnostic providers may block or throw.
        try
        {
            if (TerminalFailureLogForTest is { } log)
                log(error);
            else
                Logger.LogError(error, "monitoring loop terminated unexpectedly");
        }
        catch (Exception)
        {
            // Reporting is best-effort; the original failure is already observable in the feed.
        }
    }

    private async Task RescanSafelyAsync(string reason, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogDebug("Starting device rescan after {Reason}", reason);
            await ScanAndPublishAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown.
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Background device scan after {Reason} failed, continuing monitoring", reason);
        }
    }

    private async Task<bool> WaitForDebounceQuietPeriodAsync(CancellationToken cancellationToken)
    {
        var coalesceStarted = Stopwatch.GetTimestamp();
        while (true)
        {
            var elapsed = Stopwatch.GetElapsedTime(coalesceStarted);
            if (elapsed >= MaxCoalesceInterval)
                return true;

            var remainingCoalesce = MaxCoalesceInterval - elapsed;
            var waitInterval = remainingCoalesce < ThrottleInterval
                ? remainingCoalesce
                : ThrottleInterval;
            var trigger = await WaitForTriggerAsync(waitInterval, cancellationToken).ConfigureAwait(false);
            if (trigger == DeviceMonitorWaitResult.Timeout)
                return true;
            if (trigger == DeviceMonitorWaitResult.Completed)
                return false;

            ConsumeSignal();
        }
    }

    private async Task<DeviceMonitorWaitResult> WaitForTriggerAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var timeoutTask = Task.Delay(timeout, timeoutCts.Token);
        var readTask = Signals.WaitToReadAsync(readCts.Token).AsTask();
        var completedTask = await Task.WhenAny(timeoutTask, readTask).ConfigureAwait(false);

        if (completedTask == timeoutTask)
        {
            readCts.Cancel();
            await ObserveCancellationAsync(readTask).ConfigureAwait(false);
            await timeoutTask.ConfigureAwait(false);
            return DeviceMonitorWaitResult.Timeout;
        }

        timeoutCts.Cancel();
        await ObserveCancellationAsync(timeoutTask).ConfigureAwait(false);
        return await readTask.ConfigureAwait(false)
            ? DeviceMonitorWaitResult.Signal
            : DeviceMonitorWaitResult.Completed;
    }

    private static async Task ObserveCancellationAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected for the loser of a timeout/channel-read race.
        }
    }

    private ChannelReader<ConnectionType> Signals =>
        (_hotplugSignals ?? throw new InvalidOperationException("Monitoring is not initialized.")).Signals;

    private void ConsumeSignal()
    {
        if (!Signals.TryRead(out var transport))
            return;

        if (_disposed == 1)
            return;

        // Invalidate at signal consumption; slices 5/6 finalize placement with scheduling.
        _discovery.InvalidateEvidence(transport);
    }

    private enum DeviceMonitorWaitResult
    {
        Signal,
        Timeout,
        Completed
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        _lifetime.Cancel();
        _hotplugSignals?.Dispose();

        if (_monitoringTask is { } loop)
        {
            try
            {
                // Every publication is inside this one task: no separate publication drain exists.
                await loop.WaitAsync(_shutdownTimeout).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                Logger.LogWarning(
                    "Device monitoring loop did not stop within {Timeout} during dispose; abandoning it",
                    _shutdownTimeout);
            }
        }

        _discovery.Retire();
        Logger.LogDebug("YubiKeyMonitorWorker disposed");
    }

}