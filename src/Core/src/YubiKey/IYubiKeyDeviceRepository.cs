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

using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
/// Pure cache repository for YubiKey devices.
/// </summary>
/// <remarks>
/// This interface defines a cache-only repository with no discovery dependency.
/// Device discovery is handled by <see cref="IYubiKeyDeviceMonitorService"/>.
/// The repository only maintains state and emits change events.
/// </remarks>
internal interface IYubiKeyDeviceRepository : IDisposable
{
    /// <summary>
    /// Observable stream of device change events (added/removed).
    /// </summary>
    IObservable<DeviceEvent> DeviceChanges { get; }

    /// <summary>
    /// Indicates whether the cache contains any data.
    /// </summary>
    bool HasData { get; }

    /// <summary>
    /// Gets all cached devices, optionally filtered by connection type.
    /// </summary>
    /// <param name="type">The connection type to filter by, or <see cref="ConnectionType.All"/> for all devices.</param>
    /// <returns>A read-only list of cached devices matching the filter.</returns>
    /// <remarks>
    /// This is a synchronous operation that returns only cached data.
    /// It does not trigger device discovery.
    /// </remarks>
    IReadOnlyList<IYubiKey> GetAll(ConnectionType type = ConnectionType.All);

    /// <summary>
    /// Updates the cache with a new set of discovered devices.
    /// </summary>
    /// <param name="devices">The complete set of currently connected devices.</param>
    /// <remarks>
    /// This method performs a diff between the current cache and the new set,
    /// emitting <see cref="DeviceEvent"/>s for added and removed devices.
    /// </remarks>
    void UpdateCache(IEnumerable<IYubiKey> devices);

    /// <summary>
    /// Clears all devices from the cache.
    /// </summary>
    /// <remarks>
    /// Does not emit removal events. Use during shutdown only.
    /// </remarks>
    void Clear();
}
