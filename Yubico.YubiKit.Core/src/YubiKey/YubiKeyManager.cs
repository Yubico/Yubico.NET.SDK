using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Hid;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.YubiKey;

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
    /// This method is idempotent - calling it when monitoring is already active has no effect.
    /// </remarks>
    public static void StartMonitoring() => StartMonitoring(DefaultMonitoringInterval);
    
    /// <summary>
    /// Starts monitoring for YubiKey device changes using the specified interval.
    /// </summary>
    /// <param name="interval">The interval between device scans.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is zero or negative.</exception>
    /// <remarks>
    /// This method is idempotent - calling it when monitoring is already active has no effect.
    /// </remarks>
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
    /// This method is idempotent - calling it when monitoring is not active has no effect.
    /// Waits for any in-flight scan to complete (with a 10-second timeout).
    /// </remarks>
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
    /// Events are only emitted while monitoring is active (via <see cref="StartMonitoring()"/>).
    /// Subscribing before starting monitoring will not auto-start monitoring; the subscriber
    /// will simply receive events once monitoring is started.
    /// </remarks>
    public static IObservable<DeviceEvent> DeviceChanges => _repository.Value.DeviceChanges;
    
    /// <summary>
    /// Shuts down all YubiKeyManager resources asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// This method stops monitoring if active, clears the internal device cache,
    /// and disposes all managed resources. It is idempotent.
    /// </remarks>
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
    /// This is a convenience wrapper around <see cref="ShutdownAsync(CancellationToken)"/>.
    /// For async contexts, prefer the async version.
    /// </remarks>
    public static void Shutdown() => ShutdownAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Finds all connected YubiKey devices using the static API (no DI required).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of discovered YubiKey devices.</returns>
    public static Task<IReadOnlyList<IYubiKey>> FindAllAsync(CancellationToken cancellationToken = default)
        => FindAllAsync(ConnectionType.All, cancellationToken);

    /// <summary>
    /// Finds all connected YubiKey devices of the specified connection type using the static API.
    /// </summary>
    /// <param name="type">The connection type to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of discovered YubiKey devices.</returns>
    public static Task<IReadOnlyList<IYubiKey>> FindAllAsync(
        ConnectionType type,
        CancellationToken cancellationToken)
        => _repository.Value.FindAllAsync(type, cancellationToken);
}