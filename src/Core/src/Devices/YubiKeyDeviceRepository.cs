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
/// It has no discovery capability - that responsibility belongs to the monitor service.
/// </remarks>
internal sealed class YubiKeyDeviceRepository : IYubiKeyDeviceRepository
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<YubiKeyDeviceRepository>();

    private readonly ConcurrentDictionary<string, IYubiKey> _deviceCache = new();
    private readonly DeviceEventBroadcaster _deviceChanges = new();

    private volatile bool _hasData;
    private int _disposed;

    /// <inheritdoc/>
    /// <remarks>
    /// Events are delivered synchronously in subscription order. Subscriber exceptions propagate
    /// and stop delivery to later subscribers.
    /// </remarks>
    public IObservable<DeviceEvent> DeviceChanges => _deviceChanges;

    /// <summary>
    /// Async-sequence view of <see cref="DeviceChanges"/>, for <c>await foreach</c> consumers.
    /// </summary>
    /// <param name="cancellationToken">Cancels enumeration.</param>
    /// <remarks>
    /// Subscription is lazy. Each enumeration has an independent bounded buffer; cancellation and
    /// overflow fault the enumeration, while repository disposal completes it normally.
    /// </remarks>
    public IAsyncEnumerable<DeviceEvent> WatchAsync(CancellationToken cancellationToken = default) =>
        DeviceEventStream.From(_deviceChanges, cancellationToken);

    /// <inheritdoc/>
    public bool HasData => _hasData;

    /// <inheritdoc/>
    public IReadOnlyList<IYubiKey> GetAll(ConnectionType type = ConnectionType.All)
    {
        ThrowIfDisposed();

        return [.. _deviceCache.Values.Where(d => type.Matches(d.AvailableConnections))];
    }
    /// <inheritdoc/>
    public void UpdateCache(IEnumerable<IYubiKey> devices)
    {
        ThrowIfDisposed();

        // Diffing is keyed by physical identity — the set of interface paths a key occupies — and never by
        // IYubiKey.DeviceId. DeviceId names the evidence tier that resolved the merge and may change while
        // the interface set remains unchanged; the repository retains the existing object across those
        // evidence changes so later removal remains correlated (see YubiKeyDevice.PhysicalIdentityKeyFor).
        var currentKeys = _deviceCache.Keys.ToHashSet();
        var newDeviceMap = new Dictionary<string, IYubiKey>();

        foreach (var device in devices)
        {
            newDeviceMap[YubiKeyDevice.PhysicalIdentityKeyFor(device)] = device;
        }

        var newKeys = newDeviceMap.Keys.ToHashSet();
        var addedKeys = newKeys.Except(currentKeys).ToList();
        var removedKeys = currentKeys.Except(newKeys).ToList();

        // Handle removed devices first (include the removed device object in event)
        foreach (var identityKey in removedKeys)
        {
            if (_deviceCache.TryRemove(identityKey, out var removedDevice))
            {
                _deviceChanges.Publish(new DeviceEvent(DeviceAction.Removed, removedDevice));
                Logger.LogDebug("Device removed: {DeviceId}", removedDevice.DeviceId);
            }
        }

        // Handle added devices
        foreach (var identityKey in addedKeys)
        {
            var device = newDeviceMap[identityKey];
            _deviceCache[identityKey] = device;
            _deviceChanges.Publish(new DeviceEvent(DeviceAction.Added, device));
            Logger.LogDebug("Device added: {DeviceId}", device.DeviceId);
        }

        // Update existing devices in cache. A physical device's available connections can change while it
        // stays present, and a different known serial on the same interface set proves a substitution.
        // DeviceAction has only Added/Removed, so model either change as Removed+Added rather than overwriting
        // silently. An interface appearing or disappearing changes the interface set itself, so it is already
        // reported as Removed+Added by the two loops above.
        var changedCount = 0;
        foreach (var identityKey in newKeys.Intersect(currentKeys))
        {
            var updated = newDeviceMap[identityKey];
            if (!_deviceCache.TryGetValue(identityKey, out var existing))
                continue;

            var serialContradiction = existing.SerialNumber is { } existingSerial &&
                updated.SerialNumber is { } updatedSerial &&
                existingSerial != updatedSerial;
            if (existing.AvailableConnections != updated.AvailableConnections || serialContradiction)
            {
                _deviceCache[identityKey] = updated;
                _deviceChanges.Publish(new DeviceEvent(DeviceAction.Removed, existing));
                _deviceChanges.Publish(new DeviceEvent(DeviceAction.Added, updated));
                changedCount++;
                Logger.LogDebug(
                    "Device republished: {OldDeviceId} -> {NewDeviceId}, connections {OldConnections} -> " +
                    "{NewConnections}, serial {OldSerial} -> {NewSerial}",
                    existing.DeviceId,
                    updated.DeviceId,
                    existing.AvailableConnections,
                    updated.AvailableConnections,
                    existing.SerialNumber,
                    updated.SerialNumber);
            }
            else if (existing is YubiKeyDevice retained &&
                updated is YubiKeyDevice latest &&
                latest.DeviceInfo is { } metadata)
            {
                retained.DeviceInfo = metadata;
            }

            // Otherwise retain the object whose DeviceId was published in Added, so a later Removed event
            // remains correlated for this uninterrupted physical-presence lifetime.
        }

        _hasData = true;

        Logger.LogDebug(
            "Cache updated: {Total} devices, {Added} added, {Removed} removed, {Changed} republished",
            newDeviceMap.Count,
            addedKeys.Count,
            removedKeys.Count,
            changedCount);
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

        _deviceChanges.Complete();

        _deviceCache.Clear();

        Logger.LogDebug("YubiKeyDeviceRepository disposed");

        GC.SuppressFinalize(this);
    }
}