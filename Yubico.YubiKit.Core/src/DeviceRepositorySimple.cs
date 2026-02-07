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

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core;

/// <summary>
/// Simple device repository that performs direct device scanning without caching.
/// </summary>
/// <remarks>
/// <para>
/// This implementation does not support composite device correlation. Use 
/// <see cref="DeviceRepositoryCached"/> via dependency injection for full functionality.
/// </para>
/// <para>
/// This class is provided for backwards compatibility and simple use cases where
/// device caching and correlation are not required.
/// </para>
/// </remarks>
[Obsolete("Use DeviceRepositoryCached via AddYubiKeyManagerCore() dependency injection for composite device support.")]
public class DeviceRepositorySimple : IDeviceRepository
{
    private readonly Subject<DeviceEvent> _deviceChanges = new();

    private async IAsyncEnumerable<DeviceEvent> MonitorAsync(
        TimeSpan? pollInterval = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var interval = pollInterval ?? TimeSpan.FromSeconds(1);
        var previousDeviceIds = new HashSet<string>();

        while (!cancellationToken.IsCancellationRequested)
        {
            var references = await FindYubiKeys.Create().FindAllAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var currentDeviceIds = new HashSet<string>();

            foreach (var reference in references)
            {
                var id = reference.DeviceId;
                currentDeviceIds.Add(id);

                if (previousDeviceIds.Contains(id))
                    continue;

                _deviceChanges.OnNext(new DeviceEvent(DeviceAction.Added, reference));
                yield return new DeviceEvent(DeviceAction.Added, reference);
            }

            foreach (var removedId in previousDeviceIds.Except(currentDeviceIds))
            {
                _deviceChanges.OnNext(new DeviceEvent(DeviceAction.Removed, (IYubiKeyReference?)null) { DeviceId = removedId });
                yield return new DeviceEvent(DeviceAction.Removed, (IYubiKeyReference?)null) { DeviceId = removedId };
            }

            previousDeviceIds = currentDeviceIds;
            await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
        }
    }

    #region IDeviceRepository Members

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">
    /// DeviceRepositorySimple does not support composite devices. 
    /// Use DeviceRepositoryCached via dependency injection.
    /// </exception>
    public Task<IReadOnlyList<IYubiKey>> FindAllAsync(ConnectionType type = ConnectionType.All,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            "DeviceRepositorySimple does not support composite devices. " +
            "Use AddYubiKeyManagerCore() dependency injection with DeviceRepositoryCached for composite device support.");

    /// <inheritdoc />
    public Task UpdateCacheAsync(IEnumerable<IYubiKeyReference> discoveredDevices, CancellationToken cancellationToken = default) => 
        throw new NotSupportedException("DeviceRepositorySimple does not support caching.");

    /// <inheritdoc />
    public IObservable<DeviceEvent> DeviceChanges => _deviceChanges.AsObservable();

    /// <inheritdoc />
    public void Dispose() => _deviceChanges.Dispose();

    #endregion
}