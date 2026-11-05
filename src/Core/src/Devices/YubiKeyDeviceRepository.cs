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
using System.Runtime.CompilerServices;
using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Pure cache repository for YubiKey devices with diff-based change detection.
/// </summary>
/// <remarks>
/// This class maintains a thread-safe cache of discovered devices and emits
/// <see cref="DeviceEvent"/>s when the cache is updated via <see cref="UpdateCache"/>.
/// It has no discovery capability - that responsibility belongs to
/// <see cref="YubiKeyMonitorWorker"/>.
/// The repository owns the <see cref="DeviceEventHub"/> that backs <see cref="WatchAsync"/>.
/// </remarks>
internal sealed class YubiKeyDeviceRepository : IDisposable
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<YubiKeyDeviceRepository>();

    // Published dictionaries are private and never mutated. A diff works on its own successor map.
    private Dictionary<string, IYubiKey> _deviceCache = [];
    private readonly DeviceEventHub _events = new();
    // Serialize diffs, but never make disposal/failure wait on device properties or logging in a diff.
    private readonly Lock _cacheLock = new();
    private readonly Lock _stateLock = new();
    private IReadOnlyList<IYubiKey> _snapshot = Array.AsReadOnly<IYubiKey>([]);
    private Exception? _failure;

    private volatile bool _hasData;
    private int _disposed;

    /// <summary>
    /// Async sequence of device change events (added/removed).
    /// </summary>
    /// <param name="cancellationToken">Cancels this enumeration only.</param>
    /// <remarks>
    /// Subscription is lazy, and each enumeration owns an independent bounded buffer. Cancellation and
    /// overflow terminate only the affected enumeration; disposal ends healthy active enumerations
    /// normally, while terminal monitoring failure faults them. Delivery is asynchronous, so no consumer can block or interrupt
    /// <see cref="UpdateCache"/>.
    /// </remarks>
    public IAsyncEnumerable<DeviceEvent> WatchAsync(CancellationToken cancellationToken = default) =>
        WatchCoreAsync(cancellationToken);

    private async IAsyncEnumerable<DeviceEvent> WatchCoreAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _disposed) == 1)
            yield break;

        await foreach (var change in _events.WatchAsync(cancellationToken).ConfigureAwait(false))
            yield return change;
    }

    /// <summary>
    /// Captures initial membership and registers its ordered delta subscription at one publication boundary.
    /// </summary>
    internal async IAsyncEnumerable<DeviceEvent> WatchWithSnapshotAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IYubiKey> snapshot;
        DeviceEventHub.Subscription subscription;
        lock (_stateLock)
        {
            if (_disposed == 1)
                yield break;
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfFailed();
            snapshot = _snapshot;
            subscription = _events.Subscribe();
        }

        using (subscription)
        {
            // Replay is outside both locks and does not consume any of the bounded delta capacity.
            foreach (var device in snapshot)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new DeviceEvent(DeviceAction.Added, device);
            }

            await foreach (var change in subscription.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                yield return change;
        }
    }

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
    /// rather than "never scanned". Monitor initialization accepts a successful empty publication.
    /// It returns to <see langword="false"/> on <see cref="Dispose"/>, which
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
        ThrowIfFailed();
        var snapshot = Volatile.Read(ref _snapshot);
        return type == ConnectionType.All
            ? snapshot
            : Array.AsReadOnly<IYubiKey>([.. snapshot.Where(d => type.Matches(d.AvailableConnections))]);
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
        lock (_cacheLock)
        {
            ThrowIfDisposed();
            ThrowIfFailed();
            UpdateCacheCore(devices);
        }
    }

    private void UpdateCacheCore(IEnumerable<IYubiKey> devices)
    {
        // Diffing is keyed by physical identity — the set of interface paths a key occupies — and never by
        // IYubiKey.DeviceId. DeviceId names the evidence tier that resolved the merge and may change while
        // the interface set remains unchanged; the repository retains the existing object across those
        // evidence changes so later removal remains correlated (see YubiKeyDevice.PhysicalIdentityKeyFor).
        var currentDeviceMap = Volatile.Read(ref _deviceCache);
        var currentKeys = currentDeviceMap.Keys.ToHashSet();
        var newDeviceMap = new Dictionary<string, IYubiKey>();
        var changes = new List<DeviceEvent>();

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
            var removedDevice = currentDeviceMap[identityKey];
            changes.Add(new DeviceEvent(DeviceAction.Removed, removedDevice));
            Logger.LogDebug("Device removed: {DeviceId}", removedDevice.DeviceId);
        }

        // Handle added devices
        foreach (var identityKey in addedKeys)
        {
            var device = newDeviceMap[identityKey];
            changes.Add(new DeviceEvent(DeviceAction.Added, device));
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
            var existing = currentDeviceMap[identityKey];
            if (UpdateRetainedDevice(existing, newDeviceMap[identityKey], changes))
                changedCount++;
            else
                newDeviceMap[identityKey] = existing;
        }

        // Readers see only complete publications, never the intermediate removals/additions above.
        // The private backing array escapes solely through its read-only wrapper.
        var snapshot = Array.AsReadOnly<IYubiKey>([.. newDeviceMap.Values]);
        lock (_stateLock)
        {
            ThrowIfDisposed();
            ThrowIfFailed();
            Volatile.Write(ref _deviceCache, newDeviceMap);
            Volatile.Write(ref _snapshot, snapshot);
            _hasData = true;
            // No device getters, logging providers, or consumer continuations execute in this boundary.
            // Snapshot capture + registration is wholly before or after this complete publication.
            foreach (var change in changes)
                _events.Publish(change);
        }

        Logger.LogDebug(
            "Cache updated: {Total} devices, {Added} added, {Removed} removed, {Changed} republished",
            newDeviceMap.Count,
            addedKeys.Count,
            removedKeys.Count,
            changedCount);
    }

    private static bool UpdateRetainedDevice(IYubiKey existing, IYubiKey updated, List<DeviceEvent> changes)
    {
        var serialContradiction = existing.SerialNumber is { } existingSerial &&
            updated.SerialNumber is { } updatedSerial && existingSerial != updatedSerial;
        if (existing.AvailableConnections != updated.AvailableConnections || serialContradiction)
        {
            changes.Add(new DeviceEvent(DeviceAction.Removed, existing));
            changes.Add(new DeviceEvent(DeviceAction.Added, updated));
            Logger.LogDebug(
                "Device republished: {OldDeviceId} -> {NewDeviceId}, connections {OldConnections} -> " +
                "{NewConnections}, serial {OldSerial} -> {NewSerial}",
                existing.DeviceId, updated.DeviceId, existing.AvailableConnections, updated.AvailableConnections,
                existing.SerialNumber, updated.SerialNumber);
            return true;
        }

        if (existing is YubiKeyDevice retained && updated is YubiKeyDevice latest && latest.DeviceInfo is { } metadata)
            retained.DeviceInfo = metadata;

        // Retain the originally published object so a later Removed event remains correlated.
        return false;
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) == 1)
        {
            throw new ObjectDisposedException(nameof(YubiKeyDeviceRepository));
        }
    }

    internal void Fail(Exception error)
    {
        lock (_stateLock)
        {
            if (_disposed == 1 || _failure is not null)
                return;
            Volatile.Write(ref _failure, error);
            _events.Complete(new InvalidOperationException("Device monitoring terminated unexpectedly.", error));
        }
    }

    private void ThrowIfFailed()
    {
        if (Volatile.Read(ref _failure) is { } failure)
            throw new InvalidOperationException("Device monitoring terminated unexpectedly.", failure);
    }

    /// <summary>
    /// Ends every active <see cref="WatchAsync"/> enumeration normally, clears the cache, and resets
    /// <see cref="HasData"/> so the discarded cache is no longer reported as valid. Idempotent.
    /// </summary>
    /// <remarks>
    /// Disposal never waits on the diff lock. It marks the repository disposed, completes event delivery,
    /// and discards membership under the short state lock. An abandoned diff can finish its internal work,
    /// but cannot restore a snapshot or cache validity, or publish through the completed hub.
    /// </remarks>
    public void Dispose()
    {
        lock (_stateLock)
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            _events.Complete();

            Volatile.Write(ref _deviceCache, []);
            Volatile.Write(ref _snapshot, Array.AsReadOnly<IYubiKey>([]));
            _hasData = false;
        }

        Logger.LogDebug("YubiKeyDeviceRepository disposed");

        GC.SuppressFinalize(this);
    }
}