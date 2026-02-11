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
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core;

public interface IDeviceRepository : IDisposable
{
    IObservable<DeviceEvent> DeviceChanges { get; }
    Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type, CancellationToken cancellationToken = default);
    void UpdateCache(IEnumerable<IYubiKey> discoveredDevices);
}

public class DeviceRepository(
    ILogger<DeviceRepository> logger,
    IFindYubiKeys findYubiKeys)
    : IDeviceRepository
{
    private readonly ConcurrentDictionary<string, IYubiKey> _deviceCache = new();
    private readonly Subject<DeviceEvent> _deviceChanges = new();
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    private readonly bool
        TEST_MONITORSERVICE_SKIP_MANUALSCAN = false; // For unit testing only, we should be able to set this to internal

    private int _disposed;

    // Thread-safety for initialization
    private volatile bool _hasData;

    // Ensures cache has data - performs sync scan if needed
    private async Task EnsureDataAvailable(CancellationToken cancellationToken = default)
    {
        if (TEST_MONITORSERVICE_SKIP_MANUALSCAN)
        {
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (_hasData)
            return; // Fast path - data already available

        await _initializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_hasData)
                return; // Double-check after acquiring lock

            logger.LogInformation("Cache empty, performing synchronous device scan...");
            var yubiKeys = await findYubiKeys.FindAllAsync(ConnectionType.All, cancellationToken)
                .ConfigureAwait(false);
            UpdateCache(yubiKeys);

            logger.LogInformation("Synchronous scan completed, found {DeviceCount} devices", yubiKeys.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Synchronous device scan failed");
            // Even if sync scan fails, mark as initialized to prevent repeated attempts
            _hasData = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    #region IDeviceRepository Members

    // Public API methods with guaranteed data availability
    public async Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type = ConnectionType.All,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await EnsureDataAvailable(cancellationToken).ConfigureAwait(false);
        return [.. _deviceCache.Values.Where(d => d.ConnectionType == type || type == ConnectionType.All)];
    }

    public IObservable<DeviceEvent> DeviceChanges => _deviceChanges.AsObservable();

    public void UpdateCache(IEnumerable<IYubiKey> discoveredDevices)
    {
        ThrowIfDisposed();
        
        var currentIds = _deviceCache.Keys.ToHashSet();
        var newDeviceMap = new Dictionary<string, IYubiKey>();

        foreach (var device in discoveredDevices)
        {
            var deviceId = device.DeviceId;
            newDeviceMap[deviceId] = device;
        }

        var newIds = newDeviceMap.Keys.ToHashSet();
        var addedIds = newIds.Except(currentIds).ToList();
        var removedIds = currentIds.Except(newIds).ToList();

        // Handle added devices
        foreach (var deviceId in addedIds)
        {
            var device = newDeviceMap[deviceId];
            _deviceCache[deviceId] = device;
            _deviceChanges.OnNext(new DeviceEvent(DeviceAction.Added, device));
            logger.LogDebug("Added device: {DeviceId}", deviceId);
        }

        // Handle removed devices - include the removed device object
        foreach (var deviceId in removedIds)
        {
            if (_deviceCache.TryRemove(deviceId, out var removedDevice))
            {
                _deviceChanges.OnNext(new DeviceEvent(DeviceAction.Removed, removedDevice));
                logger.LogDebug("Removed device: {DeviceId}", deviceId);
            }
        }

        // Update existing devices in cache (refresh connection info)
        foreach (var deviceId in newIds.Intersect(currentIds))
        {
            _deviceCache[deviceId] = newDeviceMap[deviceId];
        }

        _hasData = true; // Mark as initialized

        logger.LogDebug(
            "Device cache updated: {DeviceCount} devices, {AddedCount} added, {RemovedCount} removed",
            newDeviceMap.Count, addedIds.Count, removedIds.Count);
    }

    /// <summary>
    /// Clears the device cache. Used during shutdown.
    /// </summary>
    public void ClearCache()
    {
        _deviceCache.Clear();
        _hasData = false;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed == 1)
        {
            throw new ObjectDisposedException(nameof(DeviceRepository));
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        try
        {
            _initializationLock?.Dispose();
            _deviceChanges?.OnCompleted();
            _deviceChanges?.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Ignore if already disposed
        }

        GC.SuppressFinalize(this);
    }

    #endregion

    /// <summary>
    /// Creates a new <see cref="DeviceRepository"/> instance without requiring dependency injection.
    /// </summary>
    /// <returns>A new instance of <see cref="DeviceRepository"/>.</returns>
    public static DeviceRepository Create() =>
        new(
            YubiKitLogging.CreateLogger<DeviceRepository>(),
            FindYubiKeys.Create());
}