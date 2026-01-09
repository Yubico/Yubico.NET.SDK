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
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core;

public interface IDeviceRepository : IDisposable
{
    IObservable<DeviceEvent> DeviceChanges { get; }
    Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type, CancellationToken cancellationToken = default);
    void UpdateCache(IEnumerable<IYubiKey> discoveredDevices);
}

public class DeviceRepositoryCached(
    ILogger<DeviceRepositoryCached> logger,
    IFindYubiKeys findYubiKeys)
    : IDeviceRepository
{
    private readonly ConcurrentDictionary<string, IYubiKey> _deviceCache = new();
    private readonly Subject<DeviceEvent> _deviceChanges = new();
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    private readonly bool
        TEST_MONITORSERVICE_SKIP_MANUALSCAN = false; // For unit testing only, we should be able to set this to internal

    private bool _disposed;

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
        if (_hasData)
            return; // Double-check after acquiring lock

        try
        {
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

    private static bool DevicesAreEqual(IYubiKey device1, IYubiKey device2) =>
        device1.DeviceId == device2.DeviceId;

    #region IDeviceRepository Members

    // Public API methods with guaranteed data availability
    public async Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type = ConnectionType.All,
        CancellationToken cancellationToken = default)
    {
        await EnsureDataAvailable(cancellationToken).ConfigureAwait(false);
        return [.. _deviceCache.Values.Where(d => d.ConnectionType == type || type == ConnectionType.All)];
    }

    public IObservable<DeviceEvent> DeviceChanges => _deviceChanges.AsObservable();

    public void UpdateCache(IEnumerable<IYubiKey> discoveredDevices)
    {
        var currentIds = _deviceCache.Keys.ToHashSet();
        var newDeviceMap = new Dictionary<string, IYubiKey>();

        foreach (var device in discoveredDevices)
        {
            var deviceId = device.DeviceId;
            newDeviceMap[deviceId] = device;
        }

        var newIds = newDeviceMap.Keys.ToHashSet();
        var addedIds = newIds.Except(currentIds).ToList();
        var potentiallyUpdatedIds = newIds.Intersect(currentIds).ToList();
        var removedIds = currentIds.Except(newIds).ToList();

        // Handle added devices
        foreach (var deviceId in addedIds)
        {
            var device = newDeviceMap[deviceId];
            _deviceCache[deviceId] = device;
            _deviceChanges.OnNext(new DeviceEvent(DeviceAction.Added, device));
            logger.LogDebug("Added device: {DeviceId}", deviceId);
        }

        // Handle updated devices
        foreach (var deviceId in potentiallyUpdatedIds)
        {
            var newDevice = newDeviceMap[deviceId];
            if (!_deviceCache.TryGetValue(deviceId, out var existingDevice) ||
                DevicesAreEqual(existingDevice, newDevice))
                continue;

            _deviceCache[deviceId] = newDevice;
            _deviceChanges.OnNext(new DeviceEvent(DeviceAction.Updated, newDevice));
            logger.LogDebug("Updated device: {DeviceId}", deviceId);
        }

        // Handle removed devices
        foreach (var deviceId in removedIds)
            if (_deviceCache.TryRemove(deviceId, out var removedDevice))
            {
                _deviceChanges.OnNext(
                    new DeviceEvent(DeviceAction.Removed, null) { DeviceId = deviceId });
                logger.LogDebug("Removed device: {DeviceId}", deviceId);
            }

        _hasData = true; // Mark as initialized

        logger.LogDebug(
            "Device cache updated: {DeviceCount} devices, {AddedCount} added, {UpdatedCount} updated, {RemovedCount} removed",
            newDeviceMap.Count, addedIds.Count, potentiallyUpdatedIds.Count, removedIds.Count);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

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

        _disposed = true;
    }

    #endregion
}