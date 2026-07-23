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
using System.Threading.Channels;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Service responsible for device discovery and monitoring lifecycle.
/// </summary>
/// <remarks>
/// This service owns the device listeners (HID and SmartCard) and coordinates
/// with <see cref="IYubiKeyDeviceRepository"/> to update the device cache.
/// Uses a single-reader channel to serialize listener ingress and debounce redundant scans.
/// </remarks>
internal sealed class YubiKeyDeviceMonitorService : IYubiKeyDeviceMonitorService
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<YubiKeyDeviceMonitorService>();

    /// <summary>
    /// Quiet period after the last listener hint before a coalesced repository rescan runs.
    /// </summary>
    internal static readonly TimeSpan ThrottleInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Maximum time listener hints may be coalesced before forcing a repository rescan.
    /// </summary>
    internal static readonly TimeSpan MaxCoalesceInterval = 5 * ThrottleInterval;

    /// <summary>
    /// Default maximum time <see cref="StopMonitoring"/> and <see cref="DisposeAsync"/>
    /// wait for the monitoring loop and in-flight rescans before abandoning them.
    /// </summary>
    internal static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(10);

    private readonly IYubiKeyDeviceRepository _repository;
    private readonly IFindYubiKeys _findYubiKeys;
    private readonly Lock _monitorLock = new();
    private readonly SemaphoreSlim _rescanGate = new(1, 1);
    private readonly TimeSpan _shutdownTimeout;

    // Device listeners for event-driven discovery
    private HidDeviceListener? _hidListener;
    private ISmartCardDeviceListener? _smartCardListener;

    // Channel-based event coalescing. Native listener callbacks may be concurrent,
    // so a single reader serializes hint ingress and debounce. All repository
    // rescans, including manual RescanAsync calls, are serialized through
    // _rescanGate before updating the repository cache.
    private DeviceMonitorSignal? _rescanSignal;

    // Monitoring lifecycle fields
    private CancellationTokenSource? _monitoringCts;
    private Task? _monitoringTask;

    private int _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="YubiKeyDeviceMonitorService"/> class.
    /// </summary>
    /// <param name="repository">The device repository to update on scans.</param>
    /// <param name="findYubiKeys">The device discovery service.</param>
    public YubiKeyDeviceMonitorService(
        IYubiKeyDeviceRepository repository,
        IFindYubiKeys findYubiKeys)
        : this(repository, findYubiKeys, HidDeviceListener.Create, () => new DesktopSmartCardDeviceListener())
    {
    }

    internal YubiKeyDeviceMonitorService(
        IYubiKeyDeviceRepository repository,
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

    /// <inheritdoc/>
    public bool IsMonitoring
    {
        get
        {
            lock (_monitorLock)
            {
                return _monitoringTask is not null && !_monitoringTask.IsCompleted;
            }
        }
    }

    /// <inheritdoc/>
    public async Task RescanAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await RescanCoreAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RescanCoreAsync(CancellationToken cancellationToken)
    {
        await _rescanGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Logger.LogDebug("Rescanning devices...");
            var devices = await _findYubiKeys.FindAllAsync(ConnectionType.All, cancellationToken)
                .ConfigureAwait(false);
            _repository.UpdateCache(devices);
        }
        finally
        {
            _rescanGate.Release();
        }
    }

    /// <inheritdoc/>
    public void StartMonitoring(TimeSpan interval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero, nameof(interval));
        ThrowIfDisposed();

        lock (_monitorLock)
        {
            ThrowIfDisposed();

            if (_monitoringTask is not null)
            {
                if (!_monitoringTask.IsCompleted)
                {
                    return; // Already monitoring, idempotent
                }

                // The monitoring loop only completes through StopMonitoring or
                // DisposeAsync, which also clear this field. A completed task here
                // means the loop terminated unexpectedly; tear down the stale
                // session state so monitoring can restart cleanly.
                Logger.LogWarning("Previous monitoring loop terminated unexpectedly; restarting device monitoring");
                TeardownListeners();
                _monitoringCts?.Dispose();
                _monitoringCts = null;
                _monitoringTask = null;
            }

            var rescanSignal = new DeviceMonitorSignal();

            HidDeviceListener? hidListener = null;
            ISmartCardDeviceListener? smartCardListener = null;
            var hidStartAttempted = false;
            var smartCardStartAttempted = false;
            CancellationTokenSource? monitoringCts = null;
            try
            {
                hidListener = HidListenerFactory();
                smartCardListener = SmartCardListenerFactory();

                // Capture this attempt's signal so a stale callback can never signal a later attempt.
                hidListener.DeviceEvent = hint => SignalHidEvent(rescanSignal, hint);
                smartCardListener.DeviceEvent = () => SignalSmartCardEvent(rescanSignal);

                hidStartAttempted = true;
                hidListener.Start();
                EnsureListenerStarted("HID", hidListener.Status);
                smartCardStartAttempted = true;
                smartCardListener.Start();
                EnsureListenerStarted("SmartCard", smartCardListener.Status);

                monitoringCts = new CancellationTokenSource();
                var monitoringTask = Task.Run(
                    () => MonitoringLoopAsync(interval, rescanSignal, monitoringCts.Token));

                _hidListener = hidListener;
                _smartCardListener = smartCardListener;
                _rescanSignal = rescanSignal;
                _monitoringCts = monitoringCts;
                _monitoringTask = monitoringTask;
            }
            catch
            {
                monitoringCts?.Cancel();
                rescanSignal.Complete();
                CleanupListeners(
                    hidListener,
                    hidStartAttempted,
                    smartCardListener,
                    smartCardStartAttempted);
                monitoringCts?.Dispose();
                throw;
            }

            Logger.LogInformation("Device monitoring started with interval {Interval}", interval);
        }
    }

    /// <inheritdoc/>
    public void StopMonitoring()
    {
        var (taskToAwait, ctsToDispose) = StopMonitoringCore(teardownWhenNotMonitoring: false);
        if (taskToAwait is null)
        {
            return;
        }

        // Wait for monitoring loop to complete (outside lock to avoid deadlock)
        var loopStopped = false;
        try
        {
            loopStopped = taskToAwait.Wait(_shutdownTimeout);
        }
        catch (AggregateException)
        {
            // The monitoring task faulted, which means it has completed.
            loopStopped = true;
        }

        if (!loopStopped)
        {
            // Abandon the stuck loop (e.g. a rescan blocked in native I/O) rather
            // than dispose the CTS out from under it while it may still run.
            Logger.LogWarning(
                "Device monitoring loop did not stop within {Timeout}; abandoning it",
                _shutdownTimeout);
            return;
        }

        ctsToDispose?.Dispose();

        Logger.LogInformation("Device monitoring stopped");
    }

    private (Task? TaskToAwait, CancellationTokenSource? CtsToDispose) StopMonitoringCore(bool teardownWhenNotMonitoring)
    {
        lock (_monitorLock)
        {
            if (_monitoringTask is null)
            {
                if (teardownWhenNotMonitoring)
                {
                    TeardownListeners();
                }

                return (null, null); // Not monitoring, idempotent
            }

            var taskToAwait = _monitoringTask;
            var ctsToDispose = _monitoringCts;

            // Signal cancellation
            _monitoringCts?.Cancel();
            _rescanSignal?.Complete();

            // Clear fields under lock
            _monitoringTask = null;
            _monitoringCts = null;

            // Teardown listeners under lock
            TeardownListeners();

            return (taskToAwait, ctsToDispose);
        }
    }

    /// <summary>
    /// Internal monitoring loop that processes listener events using a
    /// <see cref="ThrottleInterval"/> quiet period capped by
    /// <see cref="MaxCoalesceInterval"/>, plus interval fallback scans.
    /// </summary>
    private async Task MonitoringLoopAsync(
        TimeSpan interval,
        DeviceMonitorSignal signal,
        CancellationToken cancellationToken)
    {
        try
        {
            await RescanSafelyAsync("initial monitor startup", cancellationToken).ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                var trigger = await WaitForTriggerAsync(signal, interval, cancellationToken).ConfigureAwait(false);
                if (trigger == DeviceMonitorWaitResult.Timeout)
                {
                    await RescanSafelyAsync("interval fallback", cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (trigger == DeviceMonitorWaitResult.Completed)
                {
                    break;
                }

                _ = signal.TryConsume();

                if (!await WaitForDebounceQuietPeriodAsync(signal, cancellationToken).ConfigureAwait(false))
                {
                    break;
                }

                await RescanSafelyAsync("listener event", cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "monitoring loop terminated unexpectedly");
        }
    }

    private async Task RescanSafelyAsync(string reason, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogDebug("Starting device rescan after {Reason}", reason);
            await RescanCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Background device scan after {Reason} failed, continuing monitoring", reason);
        }
    }

    /// <summary>
    /// Waits for listener hints to stay quiet for <see cref="ThrottleInterval"/>,
    /// forcing a rescan once the coalescing round reaches
    /// <see cref="MaxCoalesceInterval"/>.
    /// </summary>
    private async Task<bool> WaitForDebounceQuietPeriodAsync(
        DeviceMonitorSignal signal,
        CancellationToken cancellationToken)
    {
        var coalesceStarted = Stopwatch.GetTimestamp();

        while (true)
        {
            var elapsed = Stopwatch.GetElapsedTime(coalesceStarted);
            if (elapsed >= MaxCoalesceInterval)
            {
                return true;
            }

            var remainingCoalesce = MaxCoalesceInterval - elapsed;
            var waitInterval = remainingCoalesce < ThrottleInterval
                ? remainingCoalesce
                : ThrottleInterval;

            var trigger = await WaitForTriggerAsync(signal, waitInterval, cancellationToken).ConfigureAwait(false);
            if (trigger == DeviceMonitorWaitResult.Timeout)
            {
                return true;
            }

            if (trigger == DeviceMonitorWaitResult.Completed)
            {
                return false;
            }

            _ = signal.TryConsume();
        }
    }

    private static async Task<DeviceMonitorWaitResult> WaitForTriggerAsync(
        DeviceMonitorSignal signal,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var timeoutTask = Task.Delay(timeout, timeoutCts.Token);
        var readTask = signal.WaitToReadAsync(readCts.Token).AsTask();
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

    private static void QueueRescan(DeviceMonitorSignal signal, string source)
    {
        try
        {
            if (!signal.TrySignal())
            {
                Logger.LogTrace("Ignored device rescan request from {Source}; monitoring is stopping", source);
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.LogTrace(ex, "Ignored device rescan request from {Source}", source);
        }
    }

    private static void SignalHidEvent(
        DeviceMonitorSignal signal,
        HidDeviceRescanHint hint)
    {
        Logger.LogTrace(
            "Received HID rescan hint: {Kind}, {PlatformDeviceId}, {DevicePath}",
            hint.ChangeKind,
            hint.PlatformDeviceId,
            hint.DevicePath);
        QueueRescan(signal, "HID");
    }

    private static void SignalSmartCardEvent(DeviceMonitorSignal signal)
    {
        QueueRescan(signal, "SmartCard");
    }

    private enum DeviceMonitorWaitResult
    {
        Signal,
        Timeout,
        Completed
    }

    /// <summary>
    /// Tears down device listeners.
    /// </summary>
    private void TeardownListeners()
    {
        var hidListener = _hidListener;
        var smartCardListener = _smartCardListener;
        _hidListener = null;
        _smartCardListener = null;

        _rescanSignal?.Complete();
        _rescanSignal = null;

        CleanupListeners(hidListener, hidListener is not null, smartCardListener, smartCardListener is not null);

        Logger.LogDebug("Device listeners torn down");
    }

    private static void CleanupListeners(
        HidDeviceListener? hidListener,
        bool hidStartAttempted,
        ISmartCardDeviceListener? smartCardListener,
        bool smartCardStartAttempted)
    {
        if (hidListener is not null)
            BestEffort(() => hidListener.DeviceEvent = null, "clear HID listener callback");
        if (smartCardListener is not null)
            BestEffort(() => smartCardListener.DeviceEvent = null, "clear SmartCard listener callback");

        if (hidStartAttempted && hidListener is not null)
            BestEffort(hidListener.Stop, "stop HID listener");
        if (smartCardStartAttempted && smartCardListener is not null)
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

    private static void EnsureListenerStarted(string listenerName, DeviceListenerStatus status)
    {
        if (status != DeviceListenerStatus.Started)
            throw new InvalidOperationException($"{listenerName} listener failed to start (status: {status}).");
    }

    private void ThrowIfDisposed()
    {
        if (_disposed == 1)
        {
            throw new ObjectDisposedException(nameof(YubiKeyDeviceMonitorService));
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        var (taskToAwait, ctsToDispose) = StopMonitoringCore(teardownWhenNotMonitoring: true);

        var loopStopped = true;
        if (taskToAwait is not null)
        {
            try
            {
                await taskToAwait.WaitAsync(_shutdownTimeout).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                loopStopped = false;
                Logger.LogWarning(
                    "Device monitoring loop did not stop within {Timeout} during dispose; abandoning it",
                    _shutdownTimeout);
            }
            catch
            {
                // Faulted or canceled means the loop has completed.
            }
        }

        if (loopStopped)
        {
            ctsToDispose?.Dispose();
        }

        // Drain any in-flight rescan before disposing the gate, bounded so a hung
        // discovery scan cannot hang disposal. On timeout the semaphore is
        // abandoned rather than disposed so the stuck rescan's Release() does not
        // hit a disposed handle.
        if (await _rescanGate.WaitAsync(_shutdownTimeout).ConfigureAwait(false))
        {
            _rescanGate.Release();
            _rescanGate.Dispose();
        }
        else
        {
            Logger.LogWarning(
                "In-flight device rescan did not finish within {Timeout} during dispose; abandoning rescan gate",
                _shutdownTimeout);
        }

        Logger.LogDebug("YubiKeyDeviceMonitorService disposed");
    }
}

/// <summary>
///     Capacity-one occurrence signal for monitor wake-ups. Listener payloads are logged at ingress and are
///     intentionally not queued because the consumer only needs to know that at least one rescan is pending.
/// </summary>
internal sealed class DeviceMonitorSignal
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