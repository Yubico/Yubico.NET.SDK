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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Devices.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

public class YubiKeyDeviceRepository : BackgroundService, IYubiKeyDeviceRepository
{
    private readonly IDeviceChannel _deviceChannel;
    private readonly IYubiKeyFactory _yubiKeyFactory;
    private readonly ILogger<YubiKeyDeviceRepository> _logger;
    private readonly ConcurrentDictionary<string, IYubiKey> _devices = new();
    private readonly Subject<YubiKeyDeviceEvent> _deviceChanges = new();

    // Thread-safety for initialization
    private volatile bool _hasData = false;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    public YubiKeyDeviceRepository(IDeviceChannel deviceChannel, IYubiKeyFactory yubiKeyFactory, ILogger<YubiKeyDeviceRepository> logger)
    {
        _deviceChannel = deviceChannel;
        _yubiKeyFactory = yubiKeyFactory;
        _logger = logger;
    }

    // Public API methods with guaranteed data availability
    public async Task<IReadOnlyCollection<IYubiKey>> GetAllDevicesAsync()
    {
        await EnsureDataAvailable();
        return _devices.Values.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyCollection<IYubiKey>> GetSmartCardDevicesAsync()
    {
        await EnsureDataAvailable();
        return _devices.Values
            .Where(IsSmartCardDevice)
            .ToList()
            .AsReadOnly();
    }

    public async Task<IYubiKey?> GetDeviceByIdAsync(string deviceId)
    {
        await EnsureDataAvailable();
        _devices.TryGetValue(deviceId, out var device);
        return device;
    }

    public IObservable<YubiKeyDeviceEvent> DeviceChanges => _deviceChanges.AsObservable();

    // Ensures cache has data - performs sync scan if needed
    private async Task EnsureDataAvailable()
    {
        if (_hasData) return; // Fast path - data already available

        await _initializationLock.WaitAsync();
        try
        {
            if (_hasData) return; // Double-check after acquiring lock

            _logger.LogInformation("Cache empty, performing synchronous device scan...");

            // Perform ONE synchronous scan to populate cache immediately
            var devices = await PcscYubiKey.GetAllAsync(_yubiKeyFactory);
            UpdateDeviceCache(devices);

            _logger.LogInformation("Synchronous scan completed, found {DeviceCount} devices", devices.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Synchronous device scan failed");
            // Even if sync scan fails, mark as initialized to prevent repeated attempts
            _hasData = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    // Background service - consumes from channel for ongoing updates
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var devices in _deviceChannel.ConsumeAsync(stoppingToken))
            {
                UpdateDeviceCache(devices);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Device repository background service was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming device updates");
        }
    }

    private void UpdateDeviceCache(IEnumerable<IYubiKey> discoveredDevices)
    {
        var deviceList = discoveredDevices.ToList();

        // Determine what changed
        var currentIds = _devices.Keys.ToHashSet();
        var newDeviceMap = new Dictionary<string, IYubiKey>();

        foreach (var device in deviceList)
        {
            var deviceId = GetDeviceId(device);
            if (deviceId != null)
            {
                newDeviceMap[deviceId] = device;
            }
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
            _logger.LogDebug("Added device: {DeviceId}", deviceId);
        }

        // Handle updated devices
        foreach (var deviceId in potentiallyUpdatedIds)
        {
            var newDevice = newDeviceMap[deviceId];
            if (_devices.TryGetValue(deviceId, out var existingDevice) && !DevicesAreEqual(existingDevice, newDevice))
            {
                _devices[deviceId] = newDevice;
                _deviceChanges.OnNext(new YubiKeyDeviceEvent(YubiKeyDeviceAction.Updated, newDevice));
                _logger.LogDebug("Updated device: {DeviceId}", deviceId);
            }
        }

        // Handle removed devices
        foreach (var deviceId in removedIds)
        {
            if (_devices.TryRemove(deviceId, out var removedDevice))
            {
                _deviceChanges.OnNext(new YubiKeyDeviceEvent(YubiKeyDeviceAction.Removed, null) { DeviceId = deviceId });
                _logger.LogDebug("Removed device: {DeviceId}", deviceId);
            }
        }

        _hasData = true; // Mark as initialized

        _logger.LogDebug("Device cache updated: {DeviceCount} devices, {AddedCount} added, {UpdatedCount} updated, {RemovedCount} removed",
            deviceList.Count, addedIds.Count(), potentiallyUpdatedIds.Count(), removedIds.Count());
    }

    private static string? GetDeviceId(IYubiKey device)
    {
        return device switch
        {
            PcscYubiKey pcscDevice => GetPcscDeviceId(pcscDevice),
            _ => null
        };
    }

    private static string? GetPcscDeviceId(PcscYubiKey pcscDevice)
    {
        try
        {
            var deviceField = typeof(PcscYubiKey).GetField("_pcscDevice",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (deviceField?.GetValue(pcscDevice) is PcscDevice pcscDev)
            {
                return $"pcsc:{pcscDev.ReaderName}";
            }
        }
        catch (Exception)
        {
        }

        return null;
    }

    private static bool IsSmartCardDevice(IYubiKey device)
    {
        return device is PcscYubiKey;
    }

    private static bool DevicesAreEqual(IYubiKey device1, IYubiKey device2)
    {
        return GetDeviceId(device1) == GetDeviceId(device2);
    }

    public override void Dispose()
    {
        _initializationLock?.Dispose();
        _deviceChanges.OnCompleted();
        _deviceChanges.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}