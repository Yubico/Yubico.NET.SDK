using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.YubiKey;

public interface IYubiKeyManager
{
    IObservable<DeviceEvent> DeviceChanges { get; }

    Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type = ConnectionType.All,
        CancellationToken cancellationToken = default);
}

public class YubiKeyManager(IDeviceRepository? deviceRepository) : IYubiKeyManager
{
    // Static API - Lazy singleton for DI-free usage
    private static readonly Lazy<DeviceRepository> _repository = new(
        DeviceRepository.Create,
        LazyThreadSafetyMode.ExecutionAndPublication);

    // Monitoring lifecycle fields
    private static CancellationTokenSource? _monitoringCts;
    private static Task? _monitoringTask;
    private static readonly object _monitorLock = new();
    
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
            catch (Exception)
            {
                // Log and continue - monitoring should be resilient
                // TODO: Add logging via YubiKitLogging.CreateLogger<YubiKeyManager>()
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

    // Instance API - for DI-based usage
    Task<IReadOnlyList<IYubiKey>> IYubiKeyManager.FindAllAsync(
        ConnectionType type,
        CancellationToken cancellationToken)
    {
        if (deviceRepository is null)
            return FindYubiKeys
                .Create()
                .FindAllAsync(type, cancellationToken);

        return deviceRepository.FindAllAsync(type, cancellationToken);
    }

    public IObservable<DeviceEvent> DeviceChanges
    {
        get
        {
            if (deviceRepository is null)
                throw new InvalidOperationException(
                    $"{nameof(DeviceChanges)} is not available when the {nameof(YubiKeyManager)} " +
                    "is created without a device repository.");

            return deviceRepository.DeviceChanges;
        }
    }
}