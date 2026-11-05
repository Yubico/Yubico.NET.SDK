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
using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Temporary adapter retaining the legacy static manager's cached discovery and lifecycle APIs.
/// </summary>
/// <remarks>
/// Composition and teardown belong solely to <see cref="YubiKeyMonitor"/>. This adapter is removed
/// with the compile-consistent migration of the static API and its consumers.
/// </remarks>
internal sealed class YubiKeyDeviceManager : IAsyncDisposable
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<YubiKeyDeviceManager>();

    /// <summary>
    /// Default monitoring interval (5 seconds).
    /// </summary>
    internal static readonly TimeSpan DefaultMonitoringInterval = YubiKeyMonitor.DefaultMonitoringInterval;

    private readonly YubiKeyMonitor _monitor;
    private YubiKeyDeviceRepository Repository => _monitor.Repository;
    private YubiKeyDeviceMonitorService MonitorService => _monitor.Service;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    private int _disposed;

    /// <summary>
    /// Test seam: invoked by <see cref="DisposeAsync"/> immediately before the repository is disposed,
    /// after every other teardown step has run. Never set in production.
    /// </summary>
    /// <remarks>
    /// This is the instant the disposal contract turns on. Any repository teardown step the manager
    /// performs before disposing it happens before this hook, so a test that resumes a publication here
    /// observes whatever intermediate repository state that step left behind. The contract is that
    /// there is no such state: a publication resuming at this point must not be able to emit a device
    /// event. Pinning it needs a hook because the window lives entirely inside <see cref="DisposeAsync"/>
    /// and closes before it returns.
    /// </remarks>
    internal Func<Task>? RepositoryTeardownReachedForTest
    {
        get => _monitor.RepositoryTeardownReachedForTest;
        set => _monitor.RepositoryTeardownReachedForTest = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YubiKeyDeviceManager"/> class.
    /// </summary>
    /// <param name="repository">The device repository.</param>
    /// <param name="monitorService">The monitor service.</param>
    internal YubiKeyDeviceManager(
        YubiKeyDeviceRepository repository,
        YubiKeyDeviceMonitorService monitorService)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(monitorService);

        _monitor = new YubiKeyMonitor(repository, monitorService);
    }

    private YubiKeyDeviceManager(YubiKeyMonitor monitor) => _monitor = monitor;

    /// <summary>
    /// Gets an async sequence of device events (arrivals and removals).
    /// </summary>
    /// <param name="cancellationToken">Stops this enumeration.</param>
    public IAsyncEnumerable<DeviceEvent> WatchAsync(CancellationToken cancellationToken = default) =>
        _monitor.WatchAsync(cancellationToken);

    /// <summary>
    /// Gets a value indicating whether device monitoring is currently active.
    /// </summary>
    public bool IsMonitoring => MonitorService.IsMonitoring;

    /// <summary>
    /// Finds all connected YubiKey devices.
    /// </summary>
    /// <param name="type">The connection type to filter by.</param>
    /// <param name="forceRescan">
    /// If <c>true</c>, always performs a fresh device scan.
    /// If <c>false</c>, returns cached results unless cache is empty.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of discovered YubiKey devices.</returns>
    public async Task<IReadOnlyList<IYubiKey>> FindAllAsync(
        ConnectionType type = ConnectionType.All,
        bool forceRescan = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (forceRescan)
        {
            Logger.LogDebug("FindAllAsync: forceRescan=true, performing scan");
            await MonitorService.RescanAsync(cancellationToken).ConfigureAwait(false);
            return Repository.GetAll(type);
        }

        // If monitoring is active and we have data, return cache
        if (IsMonitoring && Repository.HasData)
        {
            Logger.LogDebug("FindAllAsync: monitoring active with data, returning cache");
            return Repository.GetAll(type);
        }

        // If we have data, return cache
        if (Repository.HasData)
        {
            Logger.LogDebug("FindAllAsync: returning cached data");
            return Repository.GetAll(type);
        }

        // First-time initialization: need to scan
        await _initializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            if (Repository.HasData)
            {
                return Repository.GetAll(type);
            }

            Logger.LogDebug("FindAllAsync: first call, performing initial scan");
            await MonitorService.RescanAsync(cancellationToken).ConfigureAwait(false);
            return Repository.GetAll(type);
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    /// <inheritdoc cref="YubiKeyManager.FindFirstAsync(Func{IYubiKey, bool}?, CancellationToken)" />
    public async Task<IYubiKey> FindFirstAsync(
        Func<IYubiKey, bool>? predicate = null,
        CancellationToken cancellationToken = default) =>
        await FindFirstOrDefaultAsync(predicate, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No matching YubiKey was found.");

    /// <inheritdoc cref="YubiKeyManager.FindFirstOrDefaultAsync(Func{IYubiKey, bool}?, CancellationToken)" />
    public async Task<IYubiKey?> FindFirstOrDefaultAsync(
        Func<IYubiKey, bool>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var devices = await FindAllAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return predicate is null
            ? devices.FirstOrDefault()
            : devices.FirstOrDefault(predicate);
    }

    /// <summary>
    /// Starts monitoring for YubiKey device changes using the default interval.
    /// </summary>
    public void StartMonitoring() => StartMonitoring(DefaultMonitoringInterval);

    /// <summary>
    /// Starts monitoring for YubiKey device changes using the specified interval.
    /// </summary>
    /// <param name="interval">The polling interval between scans when no events occur.</param>
    public void StartMonitoring(TimeSpan interval)
    {
        ThrowIfDisposed();
        MonitorService.StartMonitoring(interval);
    }

    /// <summary>
    /// Stops monitoring for YubiKey device changes.
    /// </summary>
    public void StopMonitoring()
    {
        ThrowIfDisposed();
        MonitorService.StopMonitoring();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed == 1)
        {
            throw new ObjectDisposedException(nameof(YubiKeyDeviceManager));
        }
    }

    /// <summary>
    /// Disposes all resources with correct ordering.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref _disposed, 1);
        // Do not dispose the initialization gate: an abandoned caller can still release it.
        return _monitor.DisposeAsync();
    }

    /// <summary>
    /// Creates a new <see cref="YubiKeyDeviceManager"/> instance without requiring dependency injection.
    /// </summary>
    /// <returns>A new instance of <see cref="YubiKeyDeviceManager"/>.</returns>
    public static YubiKeyDeviceManager Create() => new(YubiKeyMonitor.CreateUninitialized());
}