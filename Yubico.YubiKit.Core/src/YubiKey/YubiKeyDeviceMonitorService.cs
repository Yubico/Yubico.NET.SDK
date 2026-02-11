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
using Yubico.YubiKit.Core.Hid;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
/// Service responsible for device discovery and monitoring lifecycle.
/// </summary>
/// <remarks>
/// This service owns the device listeners (HID and SmartCard) and coordinates
/// with <see cref="IYubiKeyDeviceRepository"/> to update the device cache.
/// The monitoring loop uses event-driven acceleration with coalescing to
/// minimize redundant scans.
/// </remarks>
internal sealed class YubiKeyDeviceMonitorService : IYubiKeyDeviceMonitorService
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<YubiKeyDeviceMonitorService>();

    private readonly IYubiKeyDeviceRepository _repository;
    private readonly IFindYubiKeys _findYubiKeys;
    private readonly object _monitorLock = new();

    // Device listeners for event-driven discovery
    private HidDeviceListener? _hidListener;
    private ISmartCardDeviceListener? _smartCardListener;

    // Event semaphore for coalescing rapid device events
    private SemaphoreSlim? _eventSemaphore;

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
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(findYubiKeys);

        _repository = repository;
        _findYubiKeys = findYubiKeys;
    }

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

            // Perform initial scan synchronously before starting background loop
            // This ensures HasData is true before returning, preventing race conditions
            // where FindAllAsync() might trigger a parallel scan
            var devices = _findYubiKeys.FindAllAsync(ConnectionType.All, CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            _repository.UpdateCache(devices);

            SetupListeners();
            _monitoringCts = new CancellationTokenSource();
            _monitoringTask = Task.Run(() => MonitoringLoopAsync(interval, _monitoringCts.Token));

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
    /// Internal monitoring loop that scans for devices at the specified interval,
    /// with event-driven acceleration.
    /// </summary>
    private async Task MonitoringLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        // Skip initial scan - StartMonitoring already performed it before starting this loop
        var skipNextScan = true;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Trigger a device rescan (unless skipping)
                if (!skipNextScan)
                {
                    await RescanAsync(cancellationToken).ConfigureAwait(false);
                }
                skipNextScan = false;

                // Wait for EITHER interval OR device event
                var eventTask = _eventSemaphore?.WaitAsync(cancellationToken) ?? Task.CompletedTask;

                // var intervalTask = Task.Delay(interval, cancellationToken);
                // var completed = await Task.WhenAny(eventTask, intervalTask).ConfigureAwait(false);
                var completed = await Task.WhenAny(eventTask).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (completed == eventTask)
                {
                    // Device event received - add coalescing delay
                    await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken)
                        .ConfigureAwait(false);

                    // Drain any additional signals (coalescing)
                    while (_eventSemaphore is not null && _eventSemaphore.CurrentCount > 0)
                    {
                        await _eventSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected during shutdown, exit gracefully
                break;
            }
            catch (ObjectDisposedException)
            {
                // CTS or semaphore was disposed during monitoring, exit gracefully
                Logger.LogDebug("Monitoring loop exiting: resource was disposed");
                break;
            }
            catch (Exception ex)
            {
                // Log and continue - monitoring should be resilient
                Logger.LogWarning(ex, "Background device scan failed, continuing monitoring");
            }
        }
    }

    /// <summary>
    /// Sets up device listeners for event-driven discovery.
    /// </summary>
    private void SetupListeners()
    {
        _hidListener = HidDeviceListener.Create();
        _smartCardListener = new DesktopSmartCardDeviceListener();
        _eventSemaphore = new SemaphoreSlim(0, 1);

        // Wire event callbacks to signal semaphore
        _hidListener.DeviceEvent = SignalEvent;
        _smartCardListener.DeviceEvent = SignalEvent;

        Logger.LogDebug("Device listeners set up");
    }

    /// <summary>
    /// Tears down device listeners.
    /// </summary>
    private void TeardownListeners()
    {
        // Clear callbacks FIRST (prevent events during disposal)
        if (_hidListener is not null)
        {
            _hidListener.DeviceEvent = null;
        }
        if (_smartCardListener is not null)
        {
            _smartCardListener.DeviceEvent = null;
        }

        // NOW dispose listeners
        _hidListener?.Dispose();
        _hidListener = null;

        _smartCardListener?.Dispose();
        _smartCardListener = null;

        _eventSemaphore?.Dispose();
        _eventSemaphore = null;

        Logger.LogDebug("Device listeners torn down");
    }

    /// <summary>
    /// Signals the event semaphore when a device event occurs.
    /// </summary>
    private void SignalEvent()
    {
        // Capture reference under lock to avoid race with disposal
        SemaphoreSlim? semaphore;
        lock (_monitorLock)
        {
            semaphore = _eventSemaphore;
        }

        if (semaphore is null)
        {
            return;
        }

        try
        {
            // Only release if current count is 0 (avoid building up releases)
            if (semaphore.CurrentCount == 0)
            {
                semaphore.Release();
            }
        }
        catch (ObjectDisposedException)
        {
            // Semaphore was disposed, ignore
        }
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

        // 3. Clear callbacks (prevent events during disposal)
        if (_hidListener is not null)
        {
            _hidListener.DeviceEvent = null;
        }
        if (_smartCardListener is not null)
        {
            _smartCardListener.DeviceEvent = null;
        }

        // 4. Dispose listeners
        _hidListener?.Dispose();
        _smartCardListener?.Dispose();

        // 5. Dispose primitives
        _eventSemaphore?.Dispose();
        _monitoringCts?.Dispose();

        Logger.LogDebug("YubiKeyDeviceMonitorService disposed");
    }
}
