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
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core;

public class DeviceRepositorySimple : IDeviceRepository
{
    private readonly Subject<DeviceEvent> _deviceChanges = new();

    #region IDeviceRepository Members

    public Task<IReadOnlyList<IYubiKey>> FindAllAsync(CancellationToken cancellationToken = default) =>
        FindYubiKeys.Create().FindAllAsync(cancellationToken);

    public void Update(IEnumerable<IYubiKey> discoveredDevices) => throw new NotImplementedException();

    public IObservable<DeviceEvent> DeviceChanges => _deviceChanges.AsObservable();

    public void Dispose() => _deviceChanges.Dispose();

    #endregion

    private async IAsyncEnumerable<DeviceEvent> MonitorAsync(
        TimeSpan? pollInterval = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var interval = pollInterval ?? TimeSpan.FromSeconds(1);
        var previousDeviceIds = new HashSet<string>();

        while (!cancellationToken.IsCancellationRequested)
        {
            var devices = await FindAllAsync(cancellationToken).ConfigureAwait(false);
            var currentDeviceIds = new HashSet<string>();

            foreach (var device in devices)
            {
                var id = device.DeviceId;
                currentDeviceIds.Add(id);

                if (previousDeviceIds.Contains(id))
                    continue;

                _deviceChanges.OnNext(new DeviceEvent(DeviceAction.Added, device));
                yield return new DeviceEvent(DeviceAction.Added, device);
            }

            foreach (var removedId in previousDeviceIds.Except(currentDeviceIds))
            {
                _deviceChanges.OnNext(new DeviceEvent(DeviceAction.Removed, null) { DeviceId = removedId });
                yield return new DeviceEvent(DeviceAction.Removed, null) { DeviceId = removedId };
            }

            previousDeviceIds = currentDeviceIds;
            await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
        }
    }
}