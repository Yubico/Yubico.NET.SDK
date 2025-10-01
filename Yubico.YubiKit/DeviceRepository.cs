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
using Yubico.YubiKit.Core.Devices.SmartCard;
using Yubico.YubiKit.Device;

namespace Yubico.YubiKit;

public interface IDeviceRepository : IDisposable
{
    IObservable<YubiKeyDeviceEvent> DeviceChanges { get; }
    Task<IReadOnlyCollection<IYubiKey>> GetAllDevicesAsync(CancellationToken cancellationToken = default);
    void UpdateDeviceCache(IEnumerable<IYubiKey> discoveredDevices);
}

public class DeviceRepository(
    IYubiKeyFactory yubiKeyFactory,
    ILogger<DeviceRepository> logger,
    IPcscDeviceService pcscService)
    : IDeviceRepository
{
    private readonly Subject<YubiKeyDeviceEvent> _deviceChanges = new();
    private readonly ConcurrentDictionary<string, IYubiKey> _devices = new();
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    private readonly bool
        TEST_MONITORSERVICE_SKIP_MANUALSCAN = false; // For unit testing only, we should be able to set this to internal

    private bool _disposed;

    // Thread-safety for initialization
    private volatile bool _hasData;

    #region IDeviceRepository Members

    // Public API methods with guaranteed data availability
    public async Task<IReadOnlyCollection<IYubiKey>> GetAllDevicesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDataAvailable(cancellationToken).ConfigureAwait(false);
        return [.. _devices.Values];
    }

    public IObservable<YubiKeyDeviceEvent> DeviceChanges => _deviceChanges.AsObservable();

    public void UpdateDeviceCache(IEnumerable<IYubiKey> discoveredDevices)
    {
        var currentIds = _devices.Keys.ToHashSet();
        var newDeviceMap = new Dictionary<string, IYubiKey>();

        foreach (var device in discoveredDevices)
        {
            var deviceId = GetDeviceId(device);
            if (deviceId != null) newDeviceMap[deviceId] = device;
        }

        var newIds = newDeviceMap.Keys.ToHashSet();
        var addedIds = newIds.Except(currentIds);
        var removedIds = currentIds.Except(newIds);
        var potentiallyUpdatedIds = newIds.Intersect(currentIds);

        // Handle added devices
        foreach (var deviceId in addedIds)
        {
            var device = newDeviceMap[deviceId];
            _devices[deviceId] = device;
            _deviceChanges.OnNext(new YubiKeyDeviceEvent(YubiKeyDeviceAction.Added, device));
            logger.LogDebug("Added device: {DeviceId}", deviceId);
        }

        // Handle updated devices
        foreach (var deviceId in potentiallyUpdatedIds)
        {
            var newDevice = newDeviceMap[deviceId];
            if (_devices.TryGetValue(deviceId, out var existingDevice) && !DevicesAreEqual(existingDevice, newDevice))
            {
                _devices[deviceId] = newDevice;
                _deviceChanges.OnNext(new YubiKeyDeviceEvent(YubiKeyDeviceAction.Updated, newDevice));
                logger.LogDebug("Updated device: {DeviceId}", deviceId);
            }
        }

        // Handle removed devices
        foreach (var deviceId in removedIds)
            if (_devices.TryRemove(deviceId, out var removedDevice))
            {
                _deviceChanges.OnNext(
                    new YubiKeyDeviceEvent(YubiKeyDeviceAction.Removed, null) { DeviceId = deviceId });
                logger.LogDebug("Removed device: {DeviceId}", deviceId);
            }

        _hasData = true; // Mark as initialized

        logger.LogDebug(
            "Device cache updated: {DeviceCount} devices, {AddedCount} added, {UpdatedCount} updated, {RemovedCount} removed",
            newDeviceMap.Count, addedIds.Count(), potentiallyUpdatedIds.Count(), removedIds.Count());
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

    // Ensures cache has data - performs sync scan if needed
    private async Task EnsureDataAvailable(CancellationToken cancellationToken = default)
    {
        if (TEST_MONITORSERVICE_SKIP_MANUALSCAN)
        {
            await Task.Delay(500, cancellationToken);
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

            var devices = await pcscService.GetAllAsync(cancellationToken).ConfigureAwait(false);
            var yubiKeys = devices.Select(yubiKeyFactory.Create);
            UpdateDeviceCache(yubiKeys);

            logger.LogInformation("Synchronous scan completed, found {DeviceCount} devices", devices.Count());
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

    private static string? GetDeviceId(IYubiKey device) =>
        device switch
        {
            PcscYubiKey pcscDevice => GetPcscDeviceId(pcscDevice),
            _ => null
        };

    private static string? GetPcscDeviceId(PcscYubiKey pcscDevice) => $"pcsc:{pcscDevice.ReaderName}";

    private static bool IsSmartCardDevice(IYubiKey device) => device is PcscYubiKey;

    private static bool DevicesAreEqual(IYubiKey device1, IYubiKey device2) =>
        GetDeviceId(device1) == GetDeviceId(device2);
}