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

/// <summary>
/// Repository for discovering and caching YubiKey devices.
/// </summary>
public interface IDeviceRepository : IDisposable
{
    /// <summary>
    /// Observable stream of device events (added, removed, updated).
    /// </summary>
    /// <remarks>
    /// Requires background services to be running. Call <c>host.StartAsync()</c> before accessing.
    /// </remarks>
    IObservable<DeviceEvent> DeviceChanges { get; }

    /// <summary>
    /// Finds all connected YubiKey devices, returning composite devices that aggregate all transports.
    /// </summary>
    /// <param name="type">Connection type filter. Use <see cref="ConnectionType.All"/> for all devices.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of composite YubiKey devices.</returns>
    Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the cache with newly discovered transport references.
    /// </summary>
    /// <param name="discoveredDevices">Transport references discovered by device scanning.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// This method is called by background services. It correlates transport references into composites.
    /// </remarks>
    Task UpdateCacheAsync(IEnumerable<IYubiKeyReference> discoveredDevices, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cached device repository that correlates transport references into composite YubiKey devices.
/// </summary>
/// <remarks>
/// <para>
/// This repository caches <see cref="IYubiKey"/> composites rather than raw transport references.
/// When <see cref="FindAllAsync"/> is called, transport references are correlated using the
/// <see cref="ICompositeYubiKeyFactory"/> to group multiple transports for the same physical device.
/// </para>
/// <para>
/// The identity reader delegate is provided by the Management module at DI registration time,
/// enabling identity reading without creating a circular dependency.
/// </para>
/// </remarks>
public class DeviceRepositoryCached(
    ILogger<DeviceRepositoryCached> logger,
    IFindYubiKeys findYubiKeys,
    ICompositeYubiKeyFactory compositeFactory,
    Func<IYubiKeyReference, CancellationToken, Task<IDeviceIdentity?>> identityReader)
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
        try
        {
            if (_hasData)
                return; // Double-check after acquiring lock

            logger.LogInformation("Cache empty, performing synchronous device scan...");
            var references = await findYubiKeys.FindAllAsync(ConnectionType.All, cancellationToken)
                .ConfigureAwait(false);
            await UpdateCacheInternalAsync(references, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Synchronous scan completed, found {DeviceCount} devices", _deviceCache.Count);
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

    /// <summary>
    /// Creates an identity reader that handles null returns from the delegate.
    /// </summary>
    private Func<IYubiKeyReference, CancellationToken, Task<IDeviceIdentity>> CreateSafeIdentityReader()
    {
        return async (reference, ct) =>
        {
            var identity = await identityReader(reference, ct).ConfigureAwait(false);
            // If identity is null, return a minimal identity for uncorrelatable devices
            return identity ?? new MinimalDeviceIdentity(reference.DeviceId);
        };
    }

    private static bool DevicesAreEqual(IYubiKey device1, IYubiKey device2) =>
        device1.DeviceId == device2.DeviceId;

    #region IDeviceRepository Members

    /// <inheritdoc />
    public async Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type = ConnectionType.All,
        CancellationToken cancellationToken = default)
    {
        await EnsureDataAvailable(cancellationToken).ConfigureAwait(false);

        if (type == ConnectionType.All)
        {
            return [.. _deviceCache.Values];
        }

        // Filter to composites that support the requested connection type
        return [.. _deviceCache.Values.Where(d => d.SupportsConnection(type))];
    }

    /// <inheritdoc />
    public IObservable<DeviceEvent> DeviceChanges
    {
        get
        {
            if (!DeviceMonitorService.IsStarted)
            {
                throw new InvalidOperationException(
                    "DeviceChanges requires background services to be running. " +
                    "Call host.StartAsync() before accessing DeviceChanges. " +
                    "Alternatively, use FindAllAsync() which works without background services.");
            }

            return _deviceChanges.AsObservable();
        }
    }

    /// <inheritdoc />
    public Task UpdateCacheAsync(IEnumerable<IYubiKeyReference> discoveredDevices, CancellationToken cancellationToken = default) =>
        UpdateCacheInternalAsync(discoveredDevices.ToList(), cancellationToken);

    private async Task UpdateCacheInternalAsync(IEnumerable<IYubiKeyReference> discoveredDevices, CancellationToken cancellationToken)
    {
        var referenceList = discoveredDevices.ToList();
        if (referenceList.Count == 0)
        {
            // Handle removal of all devices
            var emptyRemovedIds = _deviceCache.Keys.ToList();
            foreach (var deviceId in emptyRemovedIds)
            {
                if (_deviceCache.TryRemove(deviceId, out _))
                {
                    _deviceChanges.OnNext(new DeviceEvent(DeviceAction.Removed, (IYubiKey?)null) { DeviceId = deviceId });
                    logger.LogDebug("Removed device: {DeviceId}", deviceId);
                }
            }
            _hasData = true;
            return;
        }

        // Correlate transport references into composite devices
        IReadOnlyList<IYubiKey> composites;
        try
        {
            composites = await compositeFactory.CreateCompositesAsync(
                referenceList,
                CreateSafeIdentityReader(),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to correlate devices into composites");
            _hasData = true;
            return;
        }

        var currentIds = _deviceCache.Keys.ToHashSet();
        var newDeviceMap = composites.ToDictionary(c => c.DeviceId);

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
            if (_deviceCache.TryRemove(deviceId, out _))
            {
                _deviceChanges.OnNext(
                    new DeviceEvent(DeviceAction.Removed, (IYubiKey?)null) { DeviceId = deviceId });
                logger.LogDebug("Removed device: {DeviceId}", deviceId);
            }

        _hasData = true; // Mark as initialized

        logger.LogDebug(
            "Device cache updated: {DeviceCount} devices, {AddedCount} added, {UpdatedCount} updated, {RemovedCount} removed",
            newDeviceMap.Count, addedIds.Count, potentiallyUpdatedIds.Count, removedIds.Count);
    }

    /// <inheritdoc />
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