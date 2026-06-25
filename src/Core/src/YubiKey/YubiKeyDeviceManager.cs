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
using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
/// Composition root that owns and coordinates the device repository and monitor service.
/// </summary>
/// <remarks>
/// This class provides the main entry point for device management. It:
/// <list type="bullet">
///   <item>Owns a <see cref="YubiKeyDeviceRepository"/> for caching device state</item>
///   <item>Owns a <see cref="YubiKeyDeviceMonitorService"/> for device discovery</item>
///   <item>Implements smart caching: first call scans, subsequent calls return cache</item>
///   <item>Supports forced rescans via <c>forceRescan</c> parameter</item>
/// </list>
/// </remarks>
internal sealed class YubiKeyDeviceManager : IAsyncDisposable
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<YubiKeyDeviceManager>();

    /// <summary>
    /// Default monitoring interval (5 seconds).
    /// </summary>
    internal static readonly TimeSpan DefaultMonitoringInterval = TimeSpan.FromSeconds(5);

    private readonly YubiKeyDeviceRepository _repository;
    private readonly YubiKeyDeviceMonitorService _monitorService;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    private int _disposed;

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

        _repository = repository;
        _monitorService = monitorService;
    }

    /// <summary>
    /// Gets an observable sequence of device events (arrivals and removals).
    /// </summary>
    public IObservable<DeviceEvent> DeviceChanges => _repository.DeviceChanges;

    /// <summary>
    /// Gets a value indicating whether device monitoring is currently active.
    /// </summary>
    public bool IsMonitoring => _monitorService.IsMonitoring;

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
            await _monitorService.RescanAsync(cancellationToken).ConfigureAwait(false);
            return _repository.GetAll(type);
        }

        // If monitoring is active and we have data, return cache
        if (IsMonitoring && _repository.HasData)
        {
            Logger.LogDebug("FindAllAsync: monitoring active with data, returning cache");
            return _repository.GetAll(type);
        }

        // If we have data, return cache
        if (_repository.HasData)
        {
            Logger.LogDebug("FindAllAsync: returning cached data");
            return _repository.GetAll(type);
        }

        // First-time initialization: need to scan
        await _initializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            if (_repository.HasData)
            {
                return _repository.GetAll(type);
            }

            Logger.LogDebug("FindAllAsync: first call, performing initial scan");
            await _monitorService.RescanAsync(cancellationToken).ConfigureAwait(false);
            return _repository.GetAll(type);
        }
        finally
        {
            _initializationLock.Release();
        }
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
        _monitorService.StartMonitoring(interval);
    }

    /// <summary>
    /// Stops monitoring for YubiKey device changes.
    /// </summary>
    public void StopMonitoring()
    {
        ThrowIfDisposed();
        _monitorService.StopMonitoring();
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
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        // 1. Stop monitoring (if active)
        _monitorService.StopMonitoring();

        // 2. Dispose monitor service
        await _monitorService.DisposeAsync().ConfigureAwait(false);

        // 3. Clear and dispose repository
        _repository.Clear();
        _repository.Dispose();

        // 4. Dispose synchronization primitives
        _initializationLock.Dispose();

        Logger.LogDebug("YubiKeyDeviceManager disposed");
    }

    /// <summary>
    /// Creates a new <see cref="YubiKeyDeviceManager"/> instance without requiring dependency injection.
    /// </summary>
    /// <returns>A new instance of <see cref="YubiKeyDeviceManager"/>.</returns>
    public static YubiKeyDeviceManager Create()
    {
        var repository = new YubiKeyDeviceRepository();
        var findYubiKeys = FindYubiKeys.Create();
        var monitorService = new YubiKeyDeviceMonitorService(repository, findYubiKeys);

        return new YubiKeyDeviceManager(repository, monitorService);
    }
}
