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
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Owns listeners and one sequential discovery/publication loop, borrowing the owner's repository.
/// </summary>
internal sealed class YubiKeyMonitorWorker : IAsyncDisposable
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<YubiKeyMonitorWorker>();

    /// <summary>Quiet period after the last listener hint before a coalesced rescan runs.</summary>
    internal static readonly TimeSpan ThrottleInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>Maximum time listener hints may be coalesced before forcing a rescan.</summary>
    internal static readonly TimeSpan MaxCoalesceInterval = 5 * ThrottleInterval;

    /// <summary>Bounds the managed loop drain, not native listener teardown or total disposal time.</summary>
    private static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(10);

    private readonly YubiKeyDeviceRepository _repository;
    private readonly IFindYubiKeys _findYubiKeys;
    private readonly Lock _ingressLock = new();
    private readonly TimeSpan _shutdownTimeout;
    private readonly DeviceMonitorSignal _signal = new();
    private readonly TaskCompletionSource<Exception?> _initialScan = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Not disposed: an abandoned loop may still register with its token. This timer-less source and
    // its registrations remain owned by that work; linked wait sources are disposed by the loop itself.
    private readonly CancellationTokenSource _lifetime = new();

    private HidDeviceListener? _hidListener;
    private ISmartCardDeviceListener? _smartCardListener;
    private Task? _monitoringTask;
    private volatile int _disposed;

    /// <summary>Test-only pause after the lifetime precheck and before repository publication.</summary>
    internal Func<Task>? PublishAdmittedForTest;

    /// <summary>Test-only observation/fault injection at the loop's wait boundary.</summary>
    internal Action? MonitoringWaitReachedForTest;

    /// <summary>Test-only replacement of terminal logging, without changing the process-wide logger.</summary>
    internal Action<Exception>? TerminalFailureLogForTest;

    /// <summary>Read-only observation of the one loop task, retained through disposal.</summary>
    internal Task MonitoringCompletionForTest => _monitoringTask ?? Task.CompletedTask;

    internal async Task InitializeAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero, nameof(interval));
        Debug.Assert(_monitoringTask is null, "Owned construction initializes once, before public exposure.");

        // Listeners are optional latency accelerators; discovery/repository diffs supply device truth.
        // Store each started listener immediately so the owner can clean partial initialization.
        _hidListener = TryStartHidListener();
        _smartCardListener = TryStartSmartCardListener();
        _monitoringTask = Task.Run(() => MonitoringLoopAsync(interval, _lifetime.Token));

        if (_hidListener is null && _smartCardListener is null)
        {
            Logger.LogWarning(
                "No device-change listener could start; monitoring will rely on the {Interval} interval rescan only.",
                interval);
        }

        Logger.LogInformation("Device monitoring started with interval {Interval}", interval);

        // Creation cancellation only cancels this wait. The owner disposes partial initialization;
        // the loop's lifetime is independent of the caller's creation token after successful return.
        var failure = await _initialScan.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (failure is not null)
            ExceptionDispatchInfo.Throw(failure);
        cancellationToken.ThrowIfCancellationRequested();
    }

    internal YubiKeyMonitorWorker(
        YubiKeyDeviceRepository repository,
        IFindYubiKeys findYubiKeys,
        Func<HidDeviceListener> hidListenerFactory,
        Func<ISmartCardDeviceListener> smartCardListenerFactory,
        TimeSpan? shutdownTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(findYubiKeys);
        ArgumentNullException.ThrowIfNull(hidListenerFactory);
        ArgumentNullException.ThrowIfNull(smartCardListenerFactory);

        _repository = repository;
        _findYubiKeys = findYubiKeys;
        _shutdownTimeout = shutdownTimeout ?? DefaultShutdownTimeout;
        HidListenerFactory = hidListenerFactory;
        SmartCardListenerFactory = smartCardListenerFactory;
    }

    private Func<HidDeviceListener> HidListenerFactory { get; }
    private Func<ISmartCardDeviceListener> SmartCardListenerFactory { get; }

    private async Task ScanAndPublishAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Logger.LogDebug("Rescanning devices...");
        var devices = await _findYubiKeys.FindAllAsync(ConnectionType.All, cancellationToken).ConfigureAwait(false);

        // A finder may ignore cancellation. Reject its late result before entering repository diffing.
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
            // the owner's repository disposal and its final state-lock check reject the late commit.
            _repository.UpdateCache(devices);
        }
        catch (ObjectDisposedException) when (_disposed == 1)
        {
            // Expected only during retirement. A disposed repository in a live worker must surface.
        }
    }

    private HidDeviceListener? TryStartHidListener()
    {
        HidDeviceListener? listener = null;
        try
        {
            listener = HidListenerFactory();
            listener.DeviceEvent = SignalHidEvent;
            listener.Start();
            if (listener.Status == DeviceListenerStatus.Started)
                return listener;

            Logger.LogWarning(
                "HID device-change listener did not start (status: {Status}); HID changes will be detected on the interval rescan",
                listener.Status);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                ex,
                "HID device-change listener failed to start; HID changes will be detected on the interval rescan");
        }

        CleanupListeners(listener, smartCardListener: null);
        return null;
    }

    private ISmartCardDeviceListener? TryStartSmartCardListener()
    {
        ISmartCardDeviceListener? listener = null;
        try
        {
            listener = SmartCardListenerFactory();
            listener.DeviceEvent = () => SignalTransportEvent(ConnectionType.SmartCard);
            listener.Start();
            if (listener.Status == DeviceListenerStatus.Started)
                return listener;

            Logger.LogWarning(
                "SmartCard device-change listener did not start (status: {Status}); SmartCard changes will be detected on the interval rescan",
                listener.Status);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                ex,
                "SmartCard device-change listener failed to start; SmartCard changes will be detected on the interval rescan");
        }

        CleanupListeners(hidListener: null, listener);
        return null;
    }

    private async Task MonitoringLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        try
        {
            await ScanAndPublishAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            _initialScan.TrySetResult(null);

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

                _ = _signal.TryConsume();
                if (!await WaitForDebounceQuietPeriodAsync(cancellationToken).ConfigureAwait(false))
                    break;

                await RescanSafelyAsync("listener event", cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("The device monitoring signal completed unexpectedly.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _initialScan.TrySetCanceled(cancellationToken);
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
        _initialScan.TrySetResult(error);
        if (_disposed == 1)
            return;

        // Failure uses the repository's short state boundary, never the diff or ingress lock.
        _repository.Fail(error);

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
            // Reporting is best-effort; the original failure is already observable in the repository.
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

            _ = _signal.TryConsume();
        }
    }

    private async Task<DeviceMonitorWaitResult> WaitForTriggerAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var timeoutTask = Task.Delay(timeout, timeoutCts.Token);
        var readTask = _signal.WaitToReadAsync(readCts.Token).AsTask();
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

    private void SignalHidEvent(HidDeviceRescanHint hint)
    {
        Logger.LogTrace(
            "Received HID rescan hint: {Kind}, {PlatformDeviceId}, {DevicePath}",
            hint.ChangeKind,
            hint.PlatformDeviceId,
            hint.DevicePath);
        SignalTransportEvent(ConnectionType.Hid);
    }

    private void SignalTransportEvent(ConnectionType transport)
    {
        lock (_ingressLock)
        {
            if (_disposed == 1)
                return;

            // Activity globally invalidates identity/metadata evidence before signaling discovery.
            // Ordered with final retirement so even a captured late callback cannot renew read scopes.
            _findYubiKeys.NotifyTransportActivity(transport);
            _ = _signal.TrySignal();
        }
    }

    private enum DeviceMonitorWaitResult
    {
        Signal,
        Timeout,
        Completed
    }

    private void TeardownListeners()
    {
        var hidListener = _hidListener;
        var smartCardListener = _smartCardListener;
        _hidListener = null;
        _smartCardListener = null;
        CleanupListeners(hidListener, smartCardListener);
        Logger.LogDebug("Device listeners torn down");
    }

    private static void CleanupListeners(
        HidDeviceListener? hidListener,
        ISmartCardDeviceListener? smartCardListener)
    {
        if (hidListener is not null)
            BestEffort(() => hidListener.DeviceEvent = null, "clear HID listener callback");
        if (smartCardListener is not null)
            BestEffort(() => smartCardListener.DeviceEvent = null, "clear SmartCard listener callback");

        if (hidListener is not null)
            BestEffort(hidListener.Stop, "stop HID listener");
        if (smartCardListener is not null)
            BestEffort(smartCardListener.Stop, "stop SmartCard listener");

        if (hidListener is not null)
            BestEffort(hidListener.Dispose, "dispose HID listener");
        if (smartCardListener is not null)
            BestEffort(smartCardListener.Dispose, "dispose SmartCard listener");
    }

    private static void BestEffort(Action cleanup, string operation)
    {
        try
        {
            cleanup();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to {CleanupOperation} during monitor teardown", operation);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        lock (_ingressLock)
        {
            // Lock order: ingress -> finder evidence -> read-scope gate. Retire releases the scope
            // gate before cancelling its internal admission waits; those callbacks do not wait for
            // this loop or re-enter ingress. Already-open reads retain their native admission/lease.
            if (_findYubiKeys is FindYubiKeys finder)
                finder.RetireReadScope();
        }

        // Worker cancellation callbacks and native listener cleanup run outside the ingress lock.
        _lifetime.Cancel();
        _signal.Complete();
        TeardownListeners();

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

        Logger.LogDebug("YubiKeyMonitorWorker disposed");
    }

    /// <summary>
    ///     Capacity-one occurrence signal for monitor wake-ups. Listener payloads are logged at ingress and are
    ///     intentionally not queued because the consumer only needs to know that at least one rescan is pending.
    /// </summary>
    private sealed class DeviceMonitorSignal
    {
        private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
            new BoundedChannelOptions(1)
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.DropWrite
            });
        public bool TrySignal() => _channel.Writer.TryWrite(true);

        public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken) =>
            _channel.Reader.WaitToReadAsync(cancellationToken);

        public bool TryConsume() => _channel.Reader.TryRead(out _);

        public void Complete() => _channel.Writer.TryComplete();
    }
}