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
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
/// Internal lifecycle context that encapsulates all disposable/resettable state for YubiKeyManager.
/// </summary>
/// <remarks>
/// This class is responsible for managing the lifecycle of device discovery and monitoring.
/// All mutable state is encapsulated here, allowing YubiKeyManager.ShutdownAsync to fully
/// reset by disposing this context and creating a new one on next use.
/// </remarks>
internal sealed class YubiKeyManagerContext : IAsyncDisposable
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger(nameof(YubiKeyManagerContext));

    /// <summary>
    /// Default monitoring interval (5 seconds).
    /// </summary>
    internal static readonly TimeSpan DefaultMonitoringInterval = TimeSpan.FromSeconds(5);

    private readonly DeviceRepository _repository;
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
    /// Initializes a new instance of the <see cref="YubiKeyManagerContext"/> class.
    /// </summary>
    public YubiKeyManagerContext()
    {
        _repository = DeviceRepository.Create();
    }

    /// <summary>
    /// Gets an observable sequence of device events (arrivals and removals).
    /// </summary>
    public IObservable<DeviceEvent> DeviceChanges => _repository.DeviceChanges;

    /// <summary>
    /// Gets a value indicating whether device monitoring is currently active.
    /// </summary>
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

    /// <summary>
    /// Finds all connected YubiKey devices.
    /// </summary>
    public Task<IReadOnlyList<IYubiKey>> FindAllAsync(
        ConnectionType type,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _repository.FindAllAsync(type, cancellationToken);
    }

    /// <summary>
    /// Starts monitoring for YubiKey device changes using the specified interval.
    /// </summary>
    public void StartMonitoring(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), interval,
                "Monitoring interval must be positive.");
        }

        ThrowIfDisposed();

        lock (_monitorLock)
        {
            if (_monitoringTask is not null)
            {
                return; // Already monitoring, idempotent
            }

            SetupListeners();
            _monitoringCts = new CancellationTokenSource();
            _monitoringTask = Task.Run(() => MonitoringLoopAsync(interval, _monitoringCts.Token));
        }
    }

    /// <summary>
    /// Stops monitoring for YubiKey device changes.
    /// </summary>
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
    }

    /// <summary>
    /// Internal monitoring loop that scans for devices at the specified interval,
    /// with event-driven acceleration.
    /// </summary>
    private async Task MonitoringLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Trigger a device scan
                _ = await _repository.FindAllAsync(ConnectionType.All, cancellationToken)
                    .ConfigureAwait(false);

                // Wait for EITHER interval OR device event
                var delayTask = Task.Delay(interval, cancellationToken);
                var eventTask = _eventSemaphore?.WaitAsync(cancellationToken) ?? Task.Delay(Timeout.Infinite, cancellationToken);

                var completed = await Task.WhenAny(delayTask, eventTask).ConfigureAwait(false);

                if (completed == eventTask)
                {
                    // Device event received - add coalescing delay
                    await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken).ConfigureAwait(false);

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
    }

    /// <summary>
    /// Tears down device listeners.
    /// </summary>
    private void TeardownListeners()
    {
        _hidListener?.Dispose();
        _hidListener = null;

        _smartCardListener?.Dispose();
        _smartCardListener = null;

        _eventSemaphore?.Dispose();
        _eventSemaphore = null;
    }

    /// <summary>
    /// Signals the event semaphore when a device event occurs.
    /// </summary>
    private void SignalEvent()
    {
        if (_eventSemaphore is not null)
        {
            try
            {
                // Only release if current count is 0 (avoid building up releases)
                if (_eventSemaphore.CurrentCount == 0)
                {
                    _eventSemaphore.Release();
                }
            }
            catch (ObjectDisposedException)
            {
                // Semaphore was disposed, ignore
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed == 1)
        {
            throw new ObjectDisposedException(nameof(YubiKeyManagerContext));
        }
    }

    /// <summary>
    /// Disposes all resources with correct ordering.
    /// </summary>
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

        // 3. Dispose listeners (no more callbacks after this)
        _hidListener?.Dispose();
        _smartCardListener?.Dispose();

        // 4. Dispose repository (completes Subject)
        _repository.ClearCache();
        _repository.Dispose();

        // 5. Dispose primitives
        _eventSemaphore?.Dispose();
        _monitoringCts?.Dispose();

        Logger.LogDebug("YubiKeyManagerContext disposed");
    }
}
