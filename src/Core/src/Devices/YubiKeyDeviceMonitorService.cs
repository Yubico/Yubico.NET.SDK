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
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Service responsible for device discovery and monitoring lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// This service owns the device listeners (HID and SmartCard) and coordinates
/// with <see cref="IYubiKeyDeviceRepository"/> to update the device cache.
/// Uses a single-reader channel to serialize listener ingress and debounce redundant scans.
/// </para>
/// <para>
/// Lifecycle races are handled with epoch-gated publication: each monitoring
/// epoch is a <see cref="MonitorGeneration"/> captured once by the loop, manual
/// rescans, and listener callbacks. Every publication acquires the shared,
/// never-disposed <see cref="_publishGate"/> and is admitted only if its
/// generation is still current and the service is not disposed; superseded
/// snapshots are discarded. Lifecycle operations swap <see cref="_current"/>
/// under <see cref="_publishLock"/>; <see cref="StartMonitoring"/> and
/// <see cref="StopMonitoring"/> never touch the publication gate at all, and
/// <see cref="DisposeAsync"/> only attempts a bounded drain of it. A stalled
/// publication therefore cannot block start or stop, and can delay dispose by
/// no more than the shutdown timeout. An abandoned generation is unreachable
/// garbage that can no longer publish stale truth.
/// A publication already admitted when disposal times out its bounded drain may
/// complete after <see cref="DisposeAsync"/> returns. If the manager has
/// disposed the repository by then, the publication is discarded rather than
/// emitted, so no device event escapes a disposed manager. This is a documented
/// contract, not an accident.
/// </para>
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
    /// wait for the monitoring loop and in-flight publications before abandoning them.
    /// </summary>
    internal static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(10);

    private readonly IYubiKeyDeviceRepository _repository;
    private readonly IFindYubiKeys _findYubiKeys;
    private readonly Lock _monitorLock = new();

    // Shared, never-disposed publication gate held across admission + UpdateCache.
    // All publications, across all generations, are mutually exclusive and never
    // interleave; a successor's snapshot is serialized strictly after any
    // in-flight predecessor's.
    private readonly SemaphoreSlim _publishGate = new(1, 1);

    // Tiny state lock guarding only the publication admission check and _current
    // swaps at start/stop/dispose. Lifecycle operations take ONLY this lock,
    // never _publishGate (except the bounded dispose drain), so a stalled
    // device-event subscriber can never block start/stop/dispose.
    private readonly Lock _publishLock = new();

    private readonly TimeSpan _shutdownTimeout;

    // Device listeners for event-driven discovery
    private HidDeviceListener? _hidListener;
    private ISmartCardDeviceListener? _smartCardListener;

    // The current monitor generation. The loop, manual rescans, and listener
    // callbacks capture this reference ONCE; a generation's identity is that
    // reference, so a torn gate/generation pairing is impossible by
    // construction. Null only after disposal.
    private volatile MonitorGeneration? _current;

    // Monitoring lifecycle fields
    private Task? _monitoringTask;

    private volatile int _disposed;

    /// <summary>
    /// Test seam: invoked after <see cref="_publishGate"/> is acquired and before
    /// the admission check, so tests can pin admission atomicity deterministically.
    /// Never set in production.
    /// </summary>
    internal Func<Task>? PublishGateAcquiredForTest;

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
        _current = new MonitorGeneration();
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

        // Capture the generation ONCE at entry. If a lifecycle swap happens while
        // this rescan is in flight, its snapshot fails admission and is discarded.
        var generation = _current;
        if (generation is null)
        {
            throw new ObjectDisposedException(nameof(YubiKeyDeviceMonitorService));
        }

        await RescanCoreAsync(generation, cancellationToken).ConfigureAwait(false);
    }

    private async Task RescanCoreAsync(MonitorGeneration generation, CancellationToken cancellationToken)
    {
        // The per-generation scan gate serializes whole scan+publish sequences
        // within one generation, so same-generation snapshot ordering cannot
        // invert. It is never disposed; a hung scan holding it wedges nothing
        // outside its own dead generation.
        await generation.ScanGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Logger.LogDebug("Rescanning devices...");
            var devices = await _findYubiKeys.FindAllAsync(ConnectionType.All, cancellationToken)
                .ConfigureAwait(false);
            await PublishSnapshotAsync(generation, devices, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            generation.ScanGate.Release();
        }
    }

    /// <summary>
    /// Publishes a device snapshot under the shared publication gate. Admission is
    /// the linearization point: a snapshot publishes iff its generation is current
    /// and the service undisposed at admission; superseded snapshots are discarded.
    /// A publication admitted while current may complete after a concurrent swap,
    /// but the successor's first publication is serialized strictly after it, so
    /// newer truth always lands last.
    /// </summary>
    private async Task PublishSnapshotAsync(
        MonitorGeneration generation,
        IReadOnlyList<IYubiKey> devices,
        CancellationToken cancellationToken)
    {
        await _publishGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var hook = PublishGateAcquiredForTest;
            if (hook is not null)
            {
                await hook().ConfigureAwait(false);
            }

            lock (_publishLock)
            {
                if (_disposed == 1 || !ReferenceEquals(generation, _current))
                {
                    Logger.LogDebug(
                        "Discarding device snapshot from superseded monitor generation {GenerationId}",
                        generation.Id);
                    return;
                }
            }

            try
            {
                _repository.UpdateCache(devices);
            }
            catch (ObjectDisposedException) when (_disposed == 1)
            {
                // The shutdown race the type-level contract describes: this publication
                // was admitted before disposal, outlived DisposeAsync's bounded drain,
                // and resumed after the manager disposed the repository. Discarding it
                // here is what makes "the repository silences any later emission" true -
                // UpdateCache and the underlying subject both throw once disposed.
                // Nothing is lost: the repository is being torn down.
                //
                // The _disposed guard matters: UpdateCache invokes DeviceChanges
                // subscribers synchronously, so a subscriber touching its own disposed
                // state throws the same exception type. Outside monitor disposal that is
                // a subscriber bug, and it must keep surfacing through the normal scan
                // failure path rather than being misattributed to shutdown.
                Logger.LogDebug(
                    "Repository disposed while publishing from monitor generation {GenerationId}; discarding late snapshot",
                    generation.Id);
            }
        }
        finally
        {
            _publishGate.Release();
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
                var deadGeneration = _current;
                deadGeneration?.Cts.Cancel();
                deadGeneration?.Signal.Complete();

                _monitoringTask = null;
            }

            var generation = new MonitorGeneration();

            // Listeners are best-effort accelerators. A transport whose listener
            // cannot start (for example, no PC/SC service on the host) simply
            // contributes no event hints; it is never fatal to monitoring. Device
            // truth always comes from the interval fallback rescan and the
            // repository diff, so a transport whose listener is unavailable is
            // still detected at interval granularity. This mirrors canonical
            // yubikit (Rust/Python), where a transport that fails to enumerate is
            // skipped and discovery continues with the others.
            var hidListener = TryStartHidListener(generation.Signal);
            var smartCardListener = TryStartSmartCardListener(generation.Signal);

            // Swap the current generation BEFORE starting the loop so the loop's
            // initial rescan passes admission. The predecessor (if any) was
            // already retired; anything it still does fails admission.
            lock (_publishLock)
            {
                _current = generation;
            }

            try
            {
                var monitoringTask = Task.Run(
                    () => MonitoringLoopAsync(interval, generation, generation.Cts.Token));

                _hidListener = hidListener;
                _smartCardListener = smartCardListener;
                _monitoringTask = monitoringTask;
            }
            catch
            {
                // Scheduling the loop is not expected to fail, but keep the path
                // leak-free: tear down every listener that did start. The failed
                // generation stays current (its gates are never disposed), so
                // manual rescans keep working and a later start swaps it out.
                generation.Cts.Cancel();
                generation.Signal.Complete();
                CleanupListeners(hidListener, smartCardListener);
                throw;
            }

            if (hidListener is null && smartCardListener is null)
            {
                Logger.LogWarning(
                    "No device-change listener could start; monitoring will rely on the {Interval} interval rescan only.",
                    interval);
            }

            Logger.LogInformation(
                "Device monitoring started with interval {Interval} (generation {GenerationId})",
                interval,
                generation.Id);
        }
    }

    /// <summary>
    /// Starts the HID device-change listener on a best-effort basis. Returns the
    /// started listener, or <see langword="null"/> if it could not start; in that
    /// case HID changes are still detected by the interval fallback rescan.
    /// </summary>
    private HidDeviceListener? TryStartHidListener(DeviceMonitorSignal rescanSignal)
    {
        HidDeviceListener? listener = null;
        try
        {
            listener = HidListenerFactory();
            // Capture this attempt's signal so a stale callback can never signal a later attempt.
            listener.DeviceEvent = hint => SignalHidEvent(rescanSignal, hint);
            listener.Start();
            if (listener.Status == DeviceListenerStatus.Started)
            {
                return listener;
            }

            Logger.LogWarning(
                "HID device-change listener did not start (status: {Status}); HID changes will be detected on the interval rescan",
                listener.Status);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                ex,
                "HID device-change listener failed to start; HID changes will be detected on the interval rescan");
        }

        CleanupListeners(listener, smartCardListener: null);
        return null;
    }

    /// <summary>
    /// Starts the SmartCard device-change listener on a best-effort basis. Returns
    /// the started listener, or <see langword="null"/> if it could not start; in
    /// that case SmartCard changes are still detected by the interval fallback
    /// rescan. A missing or stopped PC/SC service is the common non-fatal cause.
    /// </summary>
    private ISmartCardDeviceListener? TryStartSmartCardListener(DeviceMonitorSignal rescanSignal)
    {
        ISmartCardDeviceListener? listener = null;
        try
        {
            listener = SmartCardListenerFactory();
            listener.DeviceEvent = () => SignalSmartCardEvent(rescanSignal);
            listener.Start();
            if (listener.Status == DeviceListenerStatus.Started)
            {
                return listener;
            }

            Logger.LogWarning(
                "SmartCard device-change listener did not start (status: {Status}); SmartCard changes will be detected on the interval rescan",
                listener.Status);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                ex,
                "SmartCard device-change listener failed to start; SmartCard changes will be detected on the interval rescan");
        }

        CleanupListeners(hidListener: null, listener);
        return null;
    }

    /// <inheritdoc/>
    public void StopMonitoring()
    {
        var taskToAwait = StopMonitoringCore(disposing: false);
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
            // Abandon the stuck loop (e.g. a rescan blocked in native I/O). Its
            // generation is already retired, so it can no longer publish.
            Logger.LogWarning(
                "Device monitoring loop did not stop within {Timeout}; abandoning it",
                _shutdownTimeout);
            return;
        }

        Logger.LogInformation("Device monitoring stopped");
    }

    private Task? StopMonitoringCore(bool disposing)
    {
        lock (_monitorLock)
        {
            if (_monitoringTask is null)
            {
                if (disposing)
                {
                    // Retire the idle generation: any in-flight manual rescan on it
                    // fails admission from here on.
                    lock (_publishLock)
                    {
                        _current = null;
                    }

                    TeardownListeners();
                }

                return null; // Not monitoring, idempotent
            }

            var taskToAwait = _monitoringTask;
            var generation = _current;

            // Signal cancellation and retire the generation. Anything it still
            // does - including a scan hung in native I/O that returns much later -
            // fails admission and can never publish stale truth.
            generation?.Cts.Cancel();
            generation?.Signal.Complete();

            lock (_publishLock)
            {
                // A fresh generation keeps manual rescans working after a plain
                // stop; dispose clears it so admission has no current generation.
                _current = disposing ? null : new MonitorGeneration();
            }

            _monitoringTask = null;

            // Teardown listeners under lock
            TeardownListeners();

            return taskToAwait;
        }
    }

    /// <summary>
    /// Internal monitoring loop that processes listener events using a
    /// <see cref="ThrottleInterval"/> quiet period capped by
    /// <see cref="MaxCoalesceInterval"/>, plus interval fallback scans.
    /// The loop captures its generation once and never re-reads
    /// <see cref="_current"/>; once superseded, its publications fail admission.
    /// </summary>
    private async Task MonitoringLoopAsync(
        TimeSpan interval,
        MonitorGeneration generation,
        CancellationToken cancellationToken)
    {
        var signal = generation.Signal;
        try
        {
            await RescanSafelyAsync("initial monitor startup", generation, cancellationToken).ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                var trigger = await WaitForTriggerAsync(signal, interval, cancellationToken).ConfigureAwait(false);
                if (trigger == DeviceMonitorWaitResult.Timeout)
                {
                    await RescanSafelyAsync("interval fallback", generation, cancellationToken).ConfigureAwait(false);
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

                await RescanSafelyAsync("listener event", generation, cancellationToken).ConfigureAwait(false);
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

    private async Task RescanSafelyAsync(string reason, MonitorGeneration generation, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogDebug("Starting device rescan after {Reason}", reason);
            await RescanCoreAsync(generation, cancellationToken).ConfigureAwait(false);
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
    /// Tears down device listeners. Signal completion is handled where the
    /// owning generation is retired, not here.
    /// </summary>
    private void TeardownListeners()
    {
        var hidListener = _hidListener;
        var smartCardListener = _smartCardListener;
        _hidListener = null;
        _smartCardListener = null;

        CleanupListeners(hidListener, smartCardListener);

        Logger.LogDebug("Device listeners torn down");
    }

    private static void CleanupListeners(
        HidDeviceListener? hidListener,
        ISmartCardDeviceListener? smartCardListener)
    {
        if (hidListener is not null)
            BestEffort(() => hidListener.DeviceEvent = null, "clear HID listener callback");
        if (smartCardListener is not null)
            BestEffort(() => smartCardListener.DeviceEvent = null, "clear SmartCard listener callback");

        if (hidListener is not null)
            BestEffort(hidListener.Stop, "stop HID listener");
        if (smartCardListener is not null)
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

        var taskToAwait = StopMonitoringCore(disposing: true);

        if (taskToAwait is not null)
        {
            try
            {
                await taskToAwait.WaitAsync(_shutdownTimeout).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                Logger.LogWarning(
                    "Device monitoring loop did not stop within {Timeout} during dispose; abandoning it",
                    _shutdownTimeout);
            }
            catch
            {
                // Faulted or canceled means the loop has completed.
            }
        }

        // Bounded drain of any in-flight publication - the gate itself is the
        // in-flight indicator. The gate is never disposed: on timeout the
        // publication is abandoned and, having been admitted while current, may
        // complete and emit device events after disposal (documented contract).
        // The manager disposes the repository afterwards, which silences any
        // later emission.
        if (await _publishGate.WaitAsync(_shutdownTimeout).ConfigureAwait(false))
        {
            _publishGate.Release();
        }
        else
        {
            Logger.LogWarning(
                "In-flight device publication did not finish within {Timeout} during dispose; abandoning it",
                _shutdownTimeout);
        }

        Logger.LogDebug("YubiKeyDeviceMonitorService disposed");
    }

    /// <summary>
    /// One monitoring epoch. The loop, manual rescans, and listener callbacks all
    /// capture this reference once; a generation's identity is that reference, so
    /// a torn gate/generation pairing is impossible by construction. Gates are
    /// never disposed - an abandoned generation is unreachable garbage that can
    /// wedge nothing outside itself.
    /// </summary>
    private sealed class MonitorGeneration
    {
        private static long _nextId;

        /// <summary>Monotonic identifier, used for logging only.</summary>
        public long Id { get; } = Interlocked.Increment(ref _nextId);

        /// <summary>Serializes scan+publish sequences within this generation. Never disposed.</summary>
        public SemaphoreSlim ScanGate { get; } = new(1, 1);

        /// <summary>Capacity-one wake-up signal for this generation's loop.</summary>
        public DeviceMonitorSignal Signal { get; } = new();

        /// <summary>
        ///     Cancellation source for this generation's loop. Never disposed, like the gates:
        ///     an abandoned generation is unreachable garbage. It holds no timer (nothing calls
        ///     <c>CancelAfter</c>) and every linked source derived from it is disposed by its own
        ///     <c>using</c>, so disposal would reclaim nothing while reintroducing the question
        ///     "is it safe to dispose this yet?" that the epoch model exists to eliminate.
        /// </summary>
        public CancellationTokenSource Cts { get; } = new();
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