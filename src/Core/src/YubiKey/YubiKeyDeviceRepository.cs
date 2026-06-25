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

using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
/// Pure cache repository for YubiKey devices with diff-based change detection.
/// </summary>
/// <remarks>
/// This class maintains a thread-safe cache of discovered devices and emits
/// <see cref="DeviceEvent"/>s when the cache is updated via <see cref="UpdateCache"/>.
/// It has no discovery capability - that responsibility belongs to the monitor service.
/// </remarks>
internal sealed class YubiKeyDeviceRepository : IYubiKeyDeviceRepository
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<YubiKeyDeviceRepository>();

    private readonly ConcurrentDictionary<string, IYubiKey> _deviceCache = new();
    private readonly Subject<DeviceEvent> _deviceChanges = new();

    private volatile bool _hasData;
    private int _disposed;

    /// <inheritdoc/>
    public IObservable<DeviceEvent> DeviceChanges => _deviceChanges.AsObservable();

    /// <inheritdoc/>
    public bool HasData => _hasData;

    /// <inheritdoc/>
    public IReadOnlyList<IYubiKey> GetAll(ConnectionType type = ConnectionType.All)
    {
        ThrowIfDisposed();

        return type == ConnectionType.All
            ? [.. _deviceCache.Values]
            : [.. _deviceCache.Values.Where(d => d.ConnectionType == type)];
    }

    /// <inheritdoc/>
    public void UpdateCache(IEnumerable<IYubiKey> devices)
    {
        ThrowIfDisposed();

        var currentIds = _deviceCache.Keys.ToHashSet();
        var newDeviceMap = new Dictionary<string, IYubiKey>();

        foreach (var device in devices)
        {
            newDeviceMap[device.DeviceId] = device;
        }

        var newIds = newDeviceMap.Keys.ToHashSet();
        var addedIds = newIds.Except(currentIds).ToList();
        var removedIds = currentIds.Except(newIds).ToList();

        // Handle removed devices first (include the removed device object in event)
        foreach (var deviceId in removedIds)
        {
            if (_deviceCache.TryRemove(deviceId, out var removedDevice))
            {
                _deviceChanges.OnNext(new DeviceEvent(DeviceAction.Removed, removedDevice));
                Logger.LogDebug("Device removed: {DeviceId}", deviceId);
            }
        }

        // Handle added devices
        foreach (var deviceId in addedIds)
        {
            var device = newDeviceMap[deviceId];
            _deviceCache[deviceId] = device;
            _deviceChanges.OnNext(new DeviceEvent(DeviceAction.Added, device));
            Logger.LogDebug("Device added: {DeviceId}", deviceId);
        }

        // Update existing devices in cache (refresh connection info)
        foreach (var deviceId in newIds.Intersect(currentIds))
        {
            _deviceCache[deviceId] = newDeviceMap[deviceId];
        }

        _hasData = true;

        Logger.LogDebug(
            "Cache updated: {Total} devices, {Added} added, {Removed} removed",
            newDeviceMap.Count,
            addedIds.Count,
            removedIds.Count);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        ThrowIfDisposed();

        _deviceCache.Clear();
        _hasData = false;

        Logger.LogDebug("Cache cleared");
    }

    private void ThrowIfDisposed()
    {
        if (_disposed == 1)
        {
            throw new ObjectDisposedException(nameof(YubiKeyDeviceRepository));
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        try
        {
            _deviceChanges.OnCompleted();
            _deviceChanges.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed, ignore
        }

        _deviceCache.Clear();

        Logger.LogDebug("YubiKeyDeviceRepository disposed");

        GC.SuppressFinalize(this);
    }
}
