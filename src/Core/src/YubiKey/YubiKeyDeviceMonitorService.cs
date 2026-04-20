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

using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
/// Uses Rx-based event coalescing via Throttle to minimize redundant scans.
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

    // Rx-based event coalescing
    private Subject<Unit>? _rescanTrigger;
    private IDisposable? _throttleSubscription;

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

            // Setup Rx subject for event coalescing
            _rescanTrigger = new Subject<Unit>();
            
            // Setup listeners BEFORE starting them
            SetupListeners();
            
            _monitoringCts = new CancellationTokenSource();
            _monitoringTask = Task.Run(() => MonitoringLoopAsync(_monitoringCts.Token));

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
    /// Internal monitoring loop that processes throttled device events.
    /// </summary>
    private async Task MonitoringLoopAsync(CancellationToken cancellationToken)
    {
        if (_rescanTrigger is null)
        {
            return;
        }

        try
        {
            // Subscribe to throttled events - this coalesces rapid device events
            await _rescanTrigger
                .Throttle(ThrottleInterval)
                .ForEachAsync(async _ =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    try
                    {
                        await RescanAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected during shutdown
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Background device scan failed, continuing monitoring");
                    }
                }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown
        }
    }

    /// <summary>
    /// Sets up device listeners for event-driven discovery.
    /// </summary>
    private void SetupListeners()
    {
        _hidListener = HidDeviceListener.Create();
        _smartCardListener = new DesktopSmartCardDeviceListener();

        // Wire event callbacks to signal Rx subject
        _hidListener.DeviceEvent = SignalEvent;
        _smartCardListener.DeviceEvent = SignalEvent;

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

        // Dispose Rx resources
        _throttleSubscription?.Dispose();
        _throttleSubscription = null;

        _rescanTrigger?.Dispose();
        _rescanTrigger = null;

        Logger.LogDebug("Device listeners torn down");
    }

    /// <summary>
    /// Signals the Rx subject when a device event occurs.
    /// </summary>
    private void SignalEvent()
    {
        try
        {
            _rescanTrigger?.OnNext(Unit.Default);
        }
        catch (ObjectDisposedException)
        {
            // Subject was disposed, ignore
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

        // 6. Dispose Rx resources
        _throttleSubscription?.Dispose();
        _rescanTrigger?.Dispose();

        // 7. Dispose primitives
        _monitoringCts?.Dispose();

        Logger.LogDebug("YubiKeyDeviceMonitorService disposed");
    }
}
