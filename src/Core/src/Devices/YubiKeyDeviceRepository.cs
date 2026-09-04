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
using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Pure cache repository for YubiKey devices with diff-based change detection.
/// </summary>
/// <remarks>
/// This class maintains a thread-safe cache of discovered devices and emits
/// <see cref="DeviceEvent"/>s when the cache is updated via <see cref="UpdateCache"/>.
/// It has no discovery capability - that responsibility belongs to
/// <see cref="YubiKeyDeviceMonitorService"/>.
/// The repository owns the <see cref="DeviceEventHub"/> that backs <see cref="WatchAsync"/>.
/// </remarks>
internal sealed class YubiKeyDeviceRepository : IDisposable
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<YubiKeyDeviceRepository>();

    private readonly ConcurrentDictionary<string, IYubiKey> _deviceCache = new();
    private readonly DeviceEventHub _events = new();

    private volatile bool _hasData;
    private int _disposed;

    /// <summary>
    /// Async sequence of device change events (added/removed).
    /// </summary>
    /// <param name="cancellationToken">Cancels this enumeration only.</param>
    /// <remarks>
    /// Subscription is lazy, and each enumeration owns an independent bounded buffer. Cancellation and
    /// overflow terminate only the affected enumeration; repository disposal ends every active
    /// enumeration normally. Delivery is asynchronous, so no consumer can block or interrupt
    /// <see cref="UpdateCache"/>.
    /// </remarks>
    public IAsyncEnumerable<DeviceEvent> WatchAsync(CancellationToken cancellationToken = default) =>
        _events.WatchAsync(cancellationToken);

    /// <summary>
    /// Number of live <see cref="WatchAsync"/> subscriptions. Diagnostic observability for the
    /// lazy-subscription contract.
    /// </summary>
    internal int WatcherCount => _events.WatcherCount;

    /// <summary>
    /// Indicates whether the cache holds the result of a completed scan.
    /// </summary>
    /// <remarks>
    /// This is a cache-validity flag, not a count. A scan that legitimately found no devices sets it,
    /// so <see langword="true"/> with an empty <see cref="GetAll"/> means "scanned, nothing attached"
    /// rather than "never scanned" - which is exactly the distinction
    /// <see cref="YubiKeyDeviceManager.FindAllAsync"/> needs to avoid rescanning on every call when no
    /// key is plugged in. It returns to <see langword="false"/> on <see cref="Dispose"/>, which
    /// discards the cache it describes.
    /// </remarks>
    public bool HasData => _hasData;

    /// <summary>
    /// Gets all cached devices, optionally filtered by connection type.
    /// </summary>
    /// <param name="type">The connection type to filter by, or <see cref="ConnectionType.All"/> for all devices. <see cref="ConnectionType.Hid"/> includes HID FIDO and HID OTP devices.</param>
    /// <returns>A read-only list of cached devices matching the filter.</returns>
    /// <remarks>
    /// This is a synchronous operation that returns only cached data.
    /// It does not trigger device discovery.
    /// </remarks>
    public IReadOnlyList<IYubiKey> GetAll(ConnectionType type = ConnectionType.All)
    {
        ThrowIfDisposed();

        return [.. _deviceCache.Values.Where(d => type.Matches(d.AvailableConnections))];
    }

    /// <summary>
    /// Updates the cache with a new set of discovered devices.
    /// </summary>
    /// <param name="devices">The complete set of currently connected devices.</param>
    /// <remarks>
    /// This method performs a diff between the current cache and the new set,
    /// emitting <see cref="DeviceEvent"/>s for added and removed devices.
    /// Publication is asynchronous fan-out into per-watcher buffers, so this method never runs
    /// consumer code and cannot be blocked or interrupted by a watcher.
    /// </remarks>
    public void UpdateCache(IEnumerable<IYubiKey> devices)
    {
        ThrowIfDisposed();

        // Diffing is keyed by PHYSICAL identity — the set of interface paths a key occupies — and never by
        // IYubiKey.DeviceId. A composite's DeviceId names the evidence tier that resolved the merge, so it
        // flips when the surrounding evidence changes even though the key never moved (see
        // CompositeYubiKey.PhysicalIdentityKeyFor). Diffing on it reported an unmoved key as Removed+Added.
        var currentKeys = _deviceCache.Keys.ToHashSet();
        var newDeviceMap = new Dictionary<string, IYubiKey>();

        foreach (var device in devices)
        {
            newDeviceMap[CompositeYubiKey.PhysicalIdentityKeyFor(device)] = device;
        }

        var newKeys = newDeviceMap.Keys.ToHashSet();
        var addedKeys = newKeys.Except(currentKeys).ToList();
        var removedKeys = currentKeys.Except(newKeys).ToList();

        // Handle removed devices first (include the removed device object in event)
        foreach (var identityKey in removedKeys)
        {
            if (_deviceCache.TryRemove(identityKey, out var removedDevice))
            {
                _events.Publish(new DeviceEvent(DeviceAction.Removed, removedDevice));
                Logger.LogDebug("Device removed: {DeviceId}", removedDevice.DeviceId);
            }
        }

        // Handle added devices
        foreach (var identityKey in addedKeys)
        {
            var device = newDeviceMap[identityKey];
            _deviceCache[identityKey] = device;
            _events.Publish(new DeviceEvent(DeviceAction.Added, device));
            Logger.LogDebug("Device added: {DeviceId}", device.DeviceId);
        }

        // Update existing devices in cache. A physical device's available connections can change while it
        // stays present. DeviceAction has only Added/Removed, so model a capability change as Removed+Added
        // rather than overwriting silently (ISC-17). An interface appearing or disappearing changes the
        // interface set itself, so it is already reported as Removed+Added by the two loops above; this loop
        // covers the same interface set reporting different connections.
        var changedCount = 0;
        foreach (var identityKey in newKeys.Intersect(currentKeys))
        {
            var updated = newDeviceMap[identityKey];
            if (_deviceCache.TryGetValue(identityKey, out var existing) &&
                existing.AvailableConnections != updated.AvailableConnections)
            {
                _deviceCache[identityKey] = updated;
                _events.Publish(new DeviceEvent(DeviceAction.Removed, existing));
                _events.Publish(new DeviceEvent(DeviceAction.Added, updated));
                changedCount++;
                Logger.LogDebug(
                    "Device connections changed: {DeviceId} ({Old} -> {New})",
                    updated.DeviceId,
                    existing.AvailableConnections,
                    updated.AvailableConnections);
            }

            // Otherwise retain the object whose DeviceId was published in Added, so a later Removed event
            // remains correlated for this uninterrupted physical-presence lifetime.
        }

        _hasData = true;

        Logger.LogDebug(
            "Cache updated: {Total} devices, {Added} added, {Removed} removed, {Changed} connection-changed",
            newDeviceMap.Count,
            addedKeys.Count,
            removedKeys.Count,
            changedCount);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed == 1)
        {
            throw new ObjectDisposedException(nameof(YubiKeyDeviceRepository));
        }
    }

    /// <summary>
    /// Ends every active <see cref="WatchAsync"/> enumeration normally, clears the cache, and resets
    /// <see cref="HasData"/> so the discarded cache is no longer reported as valid. Idempotent.
    /// </summary>
    /// <remarks>
    /// The disposed flag is set before the cache is emptied, so a concurrent <see cref="UpdateCache"/>
    /// either completes in full beforehand or is rejected outright. There is no instant at which the
    /// repository is live and emptied, which is what stops a late publication from diffing an attached
    /// device against an empty cache and reporting it as newly added.
    /// </remarks>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        _events.Complete();

        _deviceCache.Clear();
        _hasData = false;

        Logger.LogDebug("YubiKeyDeviceRepository disposed");

        GC.SuppressFinalize(this);
    }
}