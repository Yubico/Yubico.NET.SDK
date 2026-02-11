using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Interfaces;

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
/// <para><strong>Caching Behavior:</strong> By default, <see cref="FindAllAsync(CancellationToken)"/>
/// returns cached results after the first call. Use <c>forceRescan: true</c> to always perform a fresh
/// device scan, or call <see cref="ShutdownAsync"/> to clear the cache.</para>
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
/// <para><strong>Force Fresh Scan:</strong></para>
/// <code>
/// var devices = await YubiKeyManager.FindAllAsync(forceRescan: true);
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

    // Single manager that encapsulates all lifecycle state
    private static YubiKeyDeviceManager? _manager;
    private static readonly object _managerLock = new();

    /// <summary>
    /// Ensures the manager exists, creating it lazily if needed.
    /// </summary>
    private static YubiKeyDeviceManager EnsureManager()
    {
        var mgr = Volatile.Read(ref _manager);
        if (mgr is not null)
        {
            return mgr;
        }

        lock (_managerLock)
        {
            mgr = _manager;
            if (mgr is not null)
            {
                return mgr;
            }

            _manager = YubiKeyDeviceManager.Create();
            return _manager;
        }
    }

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
    public static void StartMonitoring() => StartMonitoring(YubiKeyDeviceManager.DefaultMonitoringInterval);

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
    public static void StartMonitoring(TimeSpan interval) => EnsureManager().StartMonitoring(interval);

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
        var mgr = Volatile.Read(ref _manager);
        mgr?.StopMonitoring();
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
            var mgr = Volatile.Read(ref _manager);
            return mgr?.IsMonitoring ?? false;
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
    public static IObservable<DeviceEvent> DeviceChanges => EnsureManager().DeviceChanges;

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

        YubiKeyDeviceManager? mgr;
        lock (_managerLock)
        {
            mgr = _manager;
            _manager = null;
        }

        if (mgr is not null)
        {
            await mgr.DisposeAsync().ConfigureAwait(false);
        }

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
    /// <para>This method returns cached results after the first call. Use the overload with
    /// <c>forceRescan: true</c> to always perform a fresh device scan.</para>
    /// <para>This method scans both SmartCard (PCSC) and HID transports.</para>
    /// <para><strong>Race Condition Note:</strong> Results may be stale if devices connect or
    /// disconnect during the scan. For real-time tracking, use <see cref="DeviceChanges"/>
    /// with <see cref="StartMonitoring()"/>.</para>
    /// </remarks>
    /// <seealso cref="FindAllAsync(ConnectionType, bool, CancellationToken)"/>
    /// <seealso cref="DeviceChanges"/>
    public static Task<IReadOnlyList<IYubiKey>> FindAllAsync(CancellationToken cancellationToken)
        => FindAllAsync(ConnectionType.All, forceRescan: false, cancellationToken);

    /// <summary>
    /// Finds all connected YubiKey devices, with options for connection type and rescan behavior.
    /// </summary>
    /// <param name="type">The connection type to filter by (SmartCard, HID, or All). Default is <see cref="ConnectionType.All"/>.</param>
    /// <param name="forceRescan">
    /// If <c>true</c>, always performs a fresh device scan.
    /// If <c>false</c> (default), returns cached results unless cache is empty.
    /// </param>
    /// <param name="cancellationToken">Cancellation token to cancel the scan.</param>
    /// <returns>A read-only list of discovered YubiKey devices matching the filter, or an empty list if none found.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the cancellation token is triggered.</exception>
    /// <exception cref="PlatformInteropException">Thrown when the platform API fails.</exception>
    /// <remarks>
    /// <para><strong>Caching:</strong> When <paramref name="forceRescan"/> is <c>false</c>:
    /// <list type="bullet">
    ///   <item>First call: Performs a fresh scan and caches results</item>
    ///   <item>Subsequent calls: Returns cached results</item>
    ///   <item>While monitoring: Returns cached results (monitoring keeps cache fresh)</item>
    /// </list>
    /// </para>
    /// <para><strong>Race Condition Note:</strong> Results may be stale if devices connect or
    /// disconnect during the scan. For real-time tracking, use <see cref="DeviceChanges"/>
    /// with <see cref="StartMonitoring()"/>.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple usage - returns cached results
    /// var devices = await YubiKeyManager.FindAllAsync();
    ///
    /// // Force a fresh scan
    /// var freshDevices = await YubiKeyManager.FindAllAsync(forceRescan: true);
    ///
    /// // Filter by connection type
    /// var smartCardDevices = await YubiKeyManager.FindAllAsync(ConnectionType.SmartCard);
    ///
    /// // Both options
    /// var freshSmartCard = await YubiKeyManager.FindAllAsync(
    ///     ConnectionType.SmartCard,
    ///     forceRescan: true);
    /// </code>
    /// </example>
    /// <seealso cref="FindAllAsync(CancellationToken)"/>
    /// <seealso cref="ConnectionType"/>
    /// <seealso cref="DeviceChanges"/>
    public static Task<IReadOnlyList<IYubiKey>> FindAllAsync(
        ConnectionType type = ConnectionType.All,
        bool forceRescan = false,
        CancellationToken cancellationToken = default)
        => EnsureManager().FindAllAsync(type, forceRescan, cancellationToken);
}