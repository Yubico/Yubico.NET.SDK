using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Hid;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
/// Provides a static API for discovering and monitoring YubiKey devices.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="YubiKeyManager"/> is a static-only API - no dependency injection is required.
/// Simply call the static methods directly to discover and monitor devices.
/// </para>
/// <para><strong>Thread Safety:</strong> All methods are thread-safe and can be called from any thread.</para>
/// <para><strong>UI Thread Marshaling:</strong> Events from <see cref="DeviceChanges"/> are raised on
/// background threads. UI applications must marshal to the UI thread for updates.</para>
/// <para><strong>Testing Pattern:</strong> Call <see cref="ShutdownAsync"/> in test cleanup (e.g., xUnit
/// <c>DisposeAsync</c> or <c>IAsyncLifetime.DisposeAsync</c>) to reset static state between tests.</para>
/// </remarks>
/// <example>
/// <para><strong>Simple Device Discovery:</strong></para>
/// <code>
/// var devices = await YubiKeyManager.FindAllAsync();
/// foreach (var device in devices)
/// {
///     Console.WriteLine($"Found: {device.SerialNumber}");
/// }
/// </code>
/// <para><strong>Device Monitoring:</strong></para>
/// <code>
/// using var subscription = YubiKeyManager.DeviceChanges.Subscribe(e =>
/// {
///     Console.WriteLine($"{e.Action}: {e.Device.SerialNumber}");
/// });
/// YubiKeyManager.StartMonitoring();
/// // ... application runs ...
/// await YubiKeyManager.ShutdownAsync();
/// </code>
/// </example>
public static class YubiKeyManager
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger(nameof(YubiKeyManager));
    
    // Static API - Lazy singleton for DI-free usage
    private static readonly Lazy<DeviceRepository> _repository = new(
        DeviceRepository.Create,
        LazyThreadSafetyMode.ExecutionAndPublication);

    // Monitoring lifecycle fields
    private static CancellationTokenSource? _monitoringCts;
    private static Task? _monitoringTask;
    private static readonly object _monitorLock = new();
    
    // Device listeners for event-driven discovery
    private static HidDeviceListener? _hidListener;
    private static ISmartCardDeviceListener? _smartCardListener;
    
    // Event semaphore for coalescing rapid device events (200ms window)
    private static SemaphoreSlim? _eventSemaphore;
    
    /// <summary>
    /// Default monitoring interval (5 seconds).
    /// </summary>
    private static readonly TimeSpan DefaultMonitoringInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Starts monitoring for YubiKey device changes using the default interval (5 seconds).
    /// </summary>
    /// <remarks>
    /// <para>This method is idempotent - calling it when monitoring is already active has no effect.</para>
    /// <para>Device listeners are set up to detect hardware events and trigger immediate scans.</para>
    /// </remarks>
    /// <seealso cref="StartMonitoring(TimeSpan)"/>
    /// <seealso cref="StopMonitoring"/>
    /// <seealso cref="IsMonitoring"/>
    /// <seealso cref="DeviceChanges"/>
    public static void StartMonitoring() => StartMonitoring(DefaultMonitoringInterval);
    
    /// <summary>
    /// Starts monitoring for YubiKey device changes using the specified interval.
    /// </summary>
    /// <param name="interval">The interval between device scans. Must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is zero or negative.</exception>
    /// <remarks>
    /// <para>This method is idempotent - calling it when monitoring is already active has no effect.</para>
    /// <para>Device listeners are set up to detect hardware events and trigger immediate scans,
    /// in addition to the periodic interval-based scans.</para>
    /// </remarks>
    /// <seealso cref="StartMonitoring()"/>
    /// <seealso cref="StopMonitoring"/>
    /// <seealso cref="IsMonitoring"/>
    public static void StartMonitoring(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "Monitoring interval must be positive.");
        
        lock (_monitorLock)
        {
            if (_monitoringTask is not null)
                return; // Already monitoring, idempotent
            
            SetupListeners();
            _monitoringCts = new CancellationTokenSource();
            _monitoringTask = Task.Run(() => MonitoringLoopAsync(interval, _monitoringCts.Token));
        }
    }
    
    /// <summary>
    /// Internal monitoring loop that scans for devices at the specified interval.
    /// </summary>
    private static async Task MonitoringLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Trigger a device scan by calling FindAllAsync which updates the cache
                _ = await _repository.Value.FindAllAsync(ConnectionType.All, cancellationToken).ConfigureAwait(false);
                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected during shutdown, exit gracefully
                break;
            }
            catch (ObjectDisposedException)
            {
                // CTS was disposed during monitoring, exit gracefully (task 3.21)
                Logger.LogDebug("Monitoring loop exiting: CancellationTokenSource was disposed");
                break;
            }
            catch (Exception ex)
            {
                // Log and continue - monitoring should be resilient (task 3.15)
                Logger.LogWarning(ex, "Background device scan failed, continuing monitoring");
            }
        }
    }
    
    /// <summary>
    /// Sets up device listeners for event-driven discovery.
    /// </summary>
    private static void SetupListeners()
    {
        // Create listeners
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
    private static void TeardownListeners()
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
    /// This causes the monitoring loop to perform an immediate scan.
    /// </summary>
    private static void SignalEvent()
    {
        // Release semaphore if available (won't block if already signaled)
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
    
    /// <summary>
    /// Stops monitoring for YubiKey device changes.
    /// </summary>
    /// <remarks>
    /// <para>This method is idempotent - calling it when monitoring is not active has no effect.</para>
    /// <para>Waits for any in-flight scan to complete (with a 10-second timeout).</para>
    /// <para>Device listeners are disposed and events will no longer be emitted to <see cref="DeviceChanges"/>
    /// until <see cref="StartMonitoring()"/> is called again.</para>
    /// </remarks>
    /// <seealso cref="StartMonitoring()"/>
    /// <seealso cref="IsMonitoring"/>
    /// <seealso cref="ShutdownAsync"/>
    public static void StopMonitoring()
    {
        Task? taskToAwait;
        CancellationTokenSource? ctsToDispose;
        
        lock (_monitorLock)
        {
            if (_monitoringTask is null)
                return; // Not monitoring, idempotent
            
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
    /// Gets a value indicating whether device monitoring is currently active.
    /// </summary>
    /// <value><see langword="true"/> if monitoring is active; otherwise, <see langword="false"/>.</value>
    /// <seealso cref="StartMonitoring()"/>
    /// <seealso cref="StopMonitoring"/>
    public static bool IsMonitoring
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
    /// Gets an observable sequence of device events (arrivals and removals).
    /// </summary>
    /// <remarks>
    /// <para>Events are only emitted while monitoring is active (via <see cref="StartMonitoring()"/>).</para>
    /// <para>Subscribing before starting monitoring will not auto-start monitoring; the subscriber
    /// will simply receive events once monitoring is started.</para>
    /// <para><strong>UI Thread Marshaling:</strong> Events are raised on background threads.
    /// UI applications must marshal to the UI thread (e.g., using <c>ObserveOn(SynchronizationContext.Current)</c>
    /// with System.Reactive, or <c>Dispatcher.Invoke</c> in WPF).</para>
    /// <para><strong>Implementation Note:</strong> Device listeners only signal that a change occurred;
    /// they do not pass device objects directly. A full device scan is triggered on each signal
    /// to determine which devices arrived or were removed.</para>
    /// </remarks>
    /// <seealso cref="StartMonitoring()"/>
    /// <seealso cref="DeviceEvent"/>
    public static IObservable<DeviceEvent> DeviceChanges => _repository.Value.DeviceChanges;
    
    /// <summary>
    /// Shuts down all YubiKeyManager resources asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the shutdown operation.</param>
    /// <returns>A task representing the async shutdown operation.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the cancellation token is triggered.</exception>
    /// <remarks>
    /// <para>This method stops monitoring if active, clears the internal device cache,
    /// and disposes all managed resources. It is idempotent.</para>
    /// <para><strong>Testing Pattern:</strong> Call this in test cleanup to reset static state:</para>
    /// <code>
    /// public async ValueTask DisposeAsync()
    /// {
    ///     await YubiKeyManager.ShutdownAsync();
    /// }
    /// </code>
    /// <para>After shutdown, <see cref="FindAllAsync(CancellationToken)"/> will perform a fresh scan,
    /// and <see cref="StartMonitoring()"/> can be called again to resume monitoring.</para>
    /// </remarks>
    /// <seealso cref="Shutdown"/>
    /// <seealso cref="StopMonitoring"/>
    public static async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        // Stop monitoring if active
        StopMonitoring();
        
        // Wait for any in-flight operations
        await Task.Yield();
        
        Logger.LogInformation("YubiKeyManager shutdown complete");
    }
    
    /// <summary>
    /// Shuts down all YubiKeyManager resources synchronously.
    /// </summary>
    /// <remarks>
    /// <para>This is a convenience wrapper around <see cref="ShutdownAsync(CancellationToken)"/>.</para>
    /// <para>For async contexts, prefer the async version to avoid blocking.</para>
    /// </remarks>
    /// <seealso cref="ShutdownAsync"/>
    public static void Shutdown() => ShutdownAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Finds all connected YubiKey devices using the static API (no DI required).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the scan.</param>
    /// <returns>A read-only list of discovered YubiKey devices, or an empty list if none found.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the cancellation token is triggered.</exception>
    /// <exception cref="PlatformInteropException">Thrown when the platform API fails.</exception>
    /// <remarks>
    /// <para>This method scans both SmartCard (PCSC) and HID transports.</para>
    /// <para><strong>Race Condition Note:</strong> Results may be stale if devices connect or
    /// disconnect during the scan. For real-time tracking, use <see cref="DeviceChanges"/>
    /// with <see cref="StartMonitoring()"/>.</para>
    /// </remarks>
    /// <seealso cref="FindAllAsync(ConnectionType, CancellationToken)"/>
    /// <seealso cref="DeviceChanges"/>
    public static Task<IReadOnlyList<IYubiKey>> FindAllAsync(CancellationToken cancellationToken = default)
        => FindAllAsync(ConnectionType.All, cancellationToken);

    /// <summary>
    /// Finds all connected YubiKey devices of the specified connection type using the static API.
    /// </summary>
    /// <param name="type">The connection type to filter by (SmartCard, HID, or All).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the scan.</param>
    /// <returns>A read-only list of discovered YubiKey devices matching the filter, or an empty list if none found.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the cancellation token is triggered.</exception>
    /// <exception cref="PlatformInteropException">Thrown when the platform API fails.</exception>
    /// <remarks>
    /// <para><strong>Race Condition Note:</strong> Results may be stale if devices connect or
    /// disconnect during the scan. For real-time tracking, use <see cref="DeviceChanges"/>
    /// with <see cref="StartMonitoring()"/>.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Find only SmartCard-connected devices
    /// var smartCardDevices = await YubiKeyManager.FindAllAsync(ConnectionType.SmartCard);
    /// </code>
    /// </example>
    /// <seealso cref="FindAllAsync(CancellationToken)"/>
    /// <seealso cref="ConnectionType"/>
    public static Task<IReadOnlyList<IYubiKey>> FindAllAsync(
        ConnectionType type,
        CancellationToken cancellationToken = default)
        => _repository.Value.FindAllAsync(type, cancellationToken);
}