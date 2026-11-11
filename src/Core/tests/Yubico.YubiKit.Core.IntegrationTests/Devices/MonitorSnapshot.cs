// Copyright 2026 Yubico AB
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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

/// <summary>Collects one subscription's replay for hardware membership assertions.</summary>
internal static class MonitorSnapshot
{
    internal static async Task<IReadOnlyList<IYubiKey>> ReadAsync(YubiKeyMonitor monitor, CancellationToken cancellationToken)
    {
        var devices = new List<IYubiKey>();
        await foreach (var change in monitor.WatchAsync(cancellationToken))
        {
            if (change is SnapshotComplete)
                break;
            devices.Add(Assert.IsType<DeviceAdded>(change).Device);
        }
        return devices;
    }
}