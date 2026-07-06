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
using System.Threading.Channels;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
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
    private static readonly TimeSpan ThrottleInterval = TimeSpan.FromMilliseconds(200);

    private readonly IYubiKeyDeviceRepository _repository;
    private readonly IFindYubiKeys _findYubiKeys;
    private readonly Lock _monitorLock = new();

    // Device listeners for event-driven discovery
    private HidDeviceListener? _hidListener;
    private ISmartCardDeviceListener? _smartCardListener;

    // Channel-based event coalescing. Native listener callbacks may be concurrent;
    // the single reader is the only place that performs repository rescans.
    private Channel<DeviceMonitorRescanRequest>? _rescanRequests;

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
        Func<ISmartCardDeviceListener> smartCardListenerFactory)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(findYubiKeys);
        ArgumentNullException.ThrowIfNull(hidListenerFactory);
        ArgumentNullException.ThrowIfNull(smartCardListenerFactory);

        _repository = repository;
        _findYubiKeys = findYubiKeys;
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
        Logger.LogDebug("Rescanning devices...");
        var devices = await _findYubiKeys.FindAllAsync(ConnectionType.All, cancellationToken)
            .ConfigureAwait(false);
        _repository.UpdateCache(devices);
    }

    /// <inheritdoc/>
    public void StartMonitoring(TimeSpan interval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero, nameof(interval));
        ThrowIfDisposed();

        lock (_monitorLock)
        {
            if (_monitoringTask is not null)
            {
                return; // Already monitoring, idempotent
            }

            _rescanRequests = Channel.CreateUnbounded<DeviceMonitorRescanRequest>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false
                });

            // Setup listeners BEFORE starting them
            SetupListeners();

            _monitoringCts = new CancellationTokenSource();
            _monitoringTask = Task.Run(() => MonitoringLoopAsync(interval, _rescanRequests.Reader, _monitoringCts.Token));

            Logger.LogInformation("Device monitoring started with interval {Interval}", interval);
        }
    }

    /// <inheritdoc/>
    public void StopMonitoring()
    {
        Task? taskToAwait;
        CancellationTokenSource? ctsToDispose;

        lock (_monitorLock)
        {
            if (_monitoringTask is null)
            {
                return; // Not monitoring, idempotent
            }

            taskToAwait = _monitoringTask;
            ctsToDispose = _monitoringCts;

            // Signal cancellation
            _monitoringCts?.Cancel();
            _rescanRequests?.Writer.TryComplete();

            // Clear fields under lock
            _monitoringTask = null;
            _monitoringCts = null;

            // Teardown listeners under lock
            TeardownListeners();
        }

        // Wait for monitoring loop to complete (outside lock to avoid deadlock)
        if (taskToAwait is not null)
        {
            try
            {
                taskToAwait.Wait(TimeSpan.FromSeconds(10));
            }
            catch (AggregateException)
            {
                // Ignore exceptions from the monitoring task - it's being stopped
            }
        }

        // Dispose the CancellationTokenSource
        ctsToDispose?.Dispose();

        Logger.LogInformation("Device monitoring stopped");
    }

    /// <summary>
    /// Internal monitoring loop that processes debounced listener events and interval fallback scans.
    /// </summary>
    private async Task MonitoringLoopAsync(
        TimeSpan interval,
        ChannelReader<DeviceMonitorRescanRequest> reader,
        CancellationToken cancellationToken)
    {
        try
        {
            await RescanSafelyAsync("initial monitor startup", cancellationToken).ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                var trigger = await WaitForTriggerAsync(reader, interval, cancellationToken).ConfigureAwait(false);
                if (trigger == DeviceMonitorWaitResult.Timeout)
                {
                    await RescanSafelyAsync("interval fallback", cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (trigger == DeviceMonitorWaitResult.Completed)
                {
                    break;
                }

                DrainQueuedRequests(reader);

                if (!await WaitForDebounceQuietPeriodAsync(reader, cancellationToken).ConfigureAwait(false))
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

    private static void DrainQueuedRequests(ChannelReader<DeviceMonitorRescanRequest> reader)
    {
        while (reader.TryRead(out _))
        {
        }
    }

    private static async Task<bool> WaitForDebounceQuietPeriodAsync(
        ChannelReader<DeviceMonitorRescanRequest> reader,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var trigger = await WaitForTriggerAsync(reader, ThrottleInterval, cancellationToken).ConfigureAwait(false);
            if (trigger == DeviceMonitorWaitResult.Timeout)
            {
                return true;
            }

            if (trigger == DeviceMonitorWaitResult.Completed)
            {
                return false;
            }

            DrainQueuedRequests(reader);
        }
    }

    private static async Task<DeviceMonitorWaitResult> WaitForTriggerAsync(
        ChannelReader<DeviceMonitorRescanRequest> reader,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var timeoutTask = Task.Delay(timeout, timeoutCts.Token);
        var readTask = reader.WaitToReadAsync(readCts.Token).AsTask();
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

    private void QueueRescan(DeviceMonitorRescanRequest request)
    {
        try
        {
            if (_rescanRequests is null || !_rescanRequests.Writer.TryWrite(request))
            {
                Logger.LogTrace("Ignored device rescan request from {Source}; monitoring is stopping", request.Source);
                return;
            }

            if (request.HidHint is not null)
            {
                Logger.LogTrace(
                    "Queued HID rescan hint: {Kind}, {PlatformDeviceId}, {DevicePath}",
                    request.HidHint.ChangeKind,
                    request.HidHint.PlatformDeviceId,
                    request.HidHint.DevicePath);
            }
        }
        catch (Exception ex)
        {
            Logger.LogTrace(ex, "Ignored device rescan request from {Source}", request.Source);
        }
    }

    private void SignalHidEvent(HidDeviceRescanHint hint)
    {
        QueueRescan(new DeviceMonitorRescanRequest("HID", hint));
    }

    private void SignalSmartCardEvent()
    {
        QueueRescan(new DeviceMonitorRescanRequest("SmartCard", null));
    }

    private readonly record struct DeviceMonitorRescanRequest(string Source, HidDeviceRescanHint? HidHint);

    private enum DeviceMonitorWaitResult
    {
        Signal,
        Timeout,
        Completed
    }

    /// <summary>
    /// Sets up device listeners for event-driven discovery.
    /// </summary>
    private void SetupListeners()
    {
        _hidListener = HidListenerFactory();
        _smartCardListener = SmartCardListenerFactory();

        // Wire event callbacks before starting listeners.
        _hidListener.DeviceEvent = SignalHidEvent;
        _smartCardListener.DeviceEvent = SignalSmartCardEvent;

        // Start listeners AFTER wiring callbacks (explicit lifecycle)
        _hidListener.Start();
        _smartCardListener.Start();

        Logger.LogDebug("Device listeners set up and started");
    }

    /// <summary>
    /// Tears down device listeners.
    /// </summary>
    private void TeardownListeners()
    {
        // Stop listeners first
        _hidListener?.Stop();
        _smartCardListener?.Stop();

        // Clear callbacks (prevent events during disposal)
        if (_hidListener is not null)
        {
            _hidListener.DeviceEvent = null;
        }
        if (_smartCardListener is not null)
        {
            _smartCardListener.DeviceEvent = null;
        }

        // Dispose listeners
        _hidListener?.Dispose();
        _hidListener = null;

        _smartCardListener?.Dispose();
        _smartCardListener = null;

        _rescanRequests?.Writer.TryComplete();
        _rescanRequests = null;

        Logger.LogDebug("Device listeners torn down");
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

        // 1. Signal cancellation
        _monitoringCts?.Cancel();
        _rescanRequests?.Writer.TryComplete();

        // 2. Wait for monitoring loop to exit (with timeout)
        if (_monitoringTask is not null)
        {
            try
            {
                await _monitoringTask.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }
            catch
            {
                // Ignore timeout/cancellation
            }
        }

        // 3. Stop listeners
        _hidListener?.Stop();
        _smartCardListener?.Stop();

        // 4. Clear callbacks (prevent events during disposal)
        if (_hidListener is not null)
        {
            _hidListener.DeviceEvent = null;
        }
        if (_smartCardListener is not null)
        {
            _smartCardListener.DeviceEvent = null;
        }

        // 5. Dispose listeners
        _hidListener?.Dispose();
        _smartCardListener?.Dispose();

        // 6. Complete channel resources
        _rescanRequests?.Writer.TryComplete();
        _rescanRequests = null;

        // 7. Dispose primitives
        _monitoringCts?.Dispose();

        Logger.LogDebug("YubiKeyDeviceMonitorService disposed");
    }
}