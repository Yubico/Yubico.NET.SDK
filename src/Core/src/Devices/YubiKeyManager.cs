using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

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
/// using System;
/// using Yubico.YubiKit.Core;
/// using Yubico.YubiKit.Core.Devices;
///
/// var devices = await YubiKeyManager.FindAllAsync();
/// foreach (var device in devices)
/// {
///     Console.WriteLine($"Found: {device.DeviceId} ({device.AvailableConnections})");
/// }
/// </code>
/// <para><strong>Force Fresh Scan:</strong></para>
/// <code>
/// var devices = await YubiKeyManager.FindAllAsync(forceRescan: true);
/// </code>
/// <para><strong>Device Monitoring:</strong></para>
/// <code>
/// YubiKeyManager.StartMonitoring();
///
/// await foreach (var e in YubiKeyManager.WatchAsync())
/// {
///     Console.WriteLine($"{e.Action}: {e.Device.DeviceId} ({e.Device.AvailableConnections})");
/// }
/// </code>
/// <para>
/// <see cref="DeviceChanges"/> exposes the same events as an <see cref="IObservable{T}"/> for
/// consumers who prefer that model. Note the SDK itself has no reactive dependency: the
/// <c>Subscribe(Action&lt;T&gt;)</c> overload and operators such as <c>Where</c> or <c>ObserveOn</c>
/// come from the <c>System.Reactive</c> package, which a consumer must reference explicitly. Without
/// it, <see cref="IObservable{T}"/> offers only <c>Subscribe(IObserver&lt;DeviceEvent&gt;)</c>.
/// </para>
/// </example>
public static class YubiKeyManager
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger(nameof(YubiKeyManager));

    // Single manager that encapsulates all lifecycle state
    private static YubiKeyDeviceManager? _manager;
    private static readonly Lock _managerLock = new();

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
    /// <para>Observers are called synchronously in subscription order. An exception from
    /// <see cref="IObserver{T}.OnNext"/> propagates to the publisher and prevents later observers from
    /// receiving that event. SDK shutdown completes all current subscriptions.</para>
    /// <para>Concurrent publication and completion are state-safe, but strict observer grammar still
    /// requires the producer to serialize them. The SDK's monitor publication gate provides that
    /// serialization during ordinary operation. During bounded shutdown, a publication already
    /// using an observer snapshot may finish after completion; repository disposal discards a late
    /// publication only when delivery has not begun.</para>
    /// <para><strong>UI Thread Marshaling:</strong> Events are raised on background threads.
    /// UI applications must marshal to the UI thread (e.g., using <c>ObserveOn(SynchronizationContext.Current)</c>
    /// with System.Reactive, or <c>Dispatcher.Invoke</c> in WPF).</para>
    /// <para><strong>Implementation Note:</strong> Device listeners only signal that a change occurred;
    /// they do not pass device objects directly. A full device scan is triggered on each signal
    /// to determine which devices arrived or were removed.</para>
    /// <para><strong>Physical-device semantics:</strong> A composite key normally appears as one event,
    /// but ambiguous evidence can conservatively publish one physical key as multiple devices. For an
    /// uninterrupted presence with unchanged interface and connection sets and no contradictory known
    /// serial, the repository retains the object originally published in <see cref="DeviceAction.Added"/>
    /// so its <see cref="IYubiKey.DeviceId"/> correlates with the eventual
    /// <see cref="DeviceAction.Removed"/> event.</para>
    /// </remarks>
    /// <seealso cref="StartMonitoring()"/>
    /// <seealso cref="DeviceEvent"/>
    public static IObservable<DeviceEvent> DeviceChanges => EnsureManager().DeviceChanges;

    /// <summary>
    /// Gets an async sequence of device events (arrivals and removals), for consumers that prefer
    /// <c>await foreach</c> over subscribing an observer.
    /// </summary>
    /// <param name="cancellationToken">Cancels enumeration.</param>
    /// <returns>A sequence that ends normally when the SDK shuts down.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown from enumeration when <paramref name="cancellationToken"/> is canceled.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown from enumeration when a new event arrives while the consumer's 256-event buffer is
    /// full. Re-enumerate and resynchronize via <see cref="FindAllAsync(CancellationToken)"/>.
    /// </exception>
    /// <remarks>
    /// <para>Events only flow while monitoring is active (see <see cref="StartMonitoring()"/>).</para>
    /// <para><strong>Subscription starts on first enumeration, not when this method is called.</strong>
    /// Begin the <c>await foreach</c> before performing an action expected to produce an event —
    /// events raised between calling this method and entering the loop are not observed. In practice
    /// this means iterating directly, as in the example below, rather than storing the sequence and
    /// enumerating it later.</para>
    /// <para>Each enumeration gets an independent bounded buffer, so concurrent watchers do not
    /// interfere and a slow consumer cannot stall device monitoring. Overflow faults only the
    /// affected enumeration.</para>
    /// </remarks>
    /// <example>
    /// <para><strong>Wait for the next YubiKey to be inserted, with a timeout:</strong></para>
    /// <code>
    /// using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    /// YubiKeyManager.StartMonitoring();
    ///
    /// try
    /// {
    ///     await foreach (var e in YubiKeyManager.WatchAsync(cts.Token))
    ///     {
    ///         if (e.Action == DeviceAction.Added)
    ///         {
    ///             Console.WriteLine($"Inserted: {e.Device.DeviceId}");
    ///             break;
    ///         }
    ///     }
    /// }
    /// catch (OperationCanceledException) when (cts.IsCancellationRequested)
    /// {
    ///     Console.WriteLine("Timed out waiting for a YubiKey.");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="DeviceChanges"/>
    /// <seealso cref="StartMonitoring()"/>
    public static IAsyncEnumerable<DeviceEvent> WatchAsync(CancellationToken cancellationToken = default) =>
        EnsureManager().WatchAsync(cancellationToken);

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
    /// <para>Results normally contain one <see cref="IYubiKey"/> per physical key, but ambiguous discovery
    /// evidence can conservatively split one key into multiple results. See the device-discovery guarantees
    /// documentation for the exact platform bounds.</para>
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
    /// <param name="type">The connection type to filter by. <see cref="ConnectionType.Hid"/> includes both HID FIDO and HID OTP devices. Default is <see cref="ConnectionType.All"/>.</param>
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
    /// <para><strong>Identity:</strong> For an uninterrupted presence with unchanged physical interfaces and
    /// <see cref="IYubiKey.AvailableConnections"/>, the cached object retains the <see cref="IYubiKey.DeviceId"/>
    /// that was originally published unless a fresh known serial proves that different hardware now occupies
    /// the same interfaces. A connection-set change or proven substitution is republished as removal followed
    /// by addition. A newly
    /// constructed object from a fresh scan can have a different evidence-tier-derived ID before repository
    /// reconciliation; do not use independently obtained scan objects as durable physical-identity records.</para>
    /// <para><strong>Physical-device bounds:</strong> One result per physical key is the common case, not an
    /// unconditional promise. When topology, serial, and PID evidence cannot safely correlate interfaces,
    /// discovery publishes conservative splits rather than risk merging different keys.</para>
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