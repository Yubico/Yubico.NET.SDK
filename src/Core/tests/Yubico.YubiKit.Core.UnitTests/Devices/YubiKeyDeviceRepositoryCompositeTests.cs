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

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
///     Repository semantics for composite (physical-identity) devices: events are keyed by physical
///     identity, and a change in a present device's available connections emits Removed+Added (ISC-16/17).
/// </summary>
public class YubiKeyDeviceRepositoryCompositeTests
{
    [Fact]
    public void UpdateCache_SamePhysicalIdConnectionsChanged_EmitsRemovedThenAdded()
    {
        using var repository = new YubiKeyDeviceRepository();
        repository.UpdateCache([new FakeYubiKey("ykphysical:103", ConnectionType.SmartCard)]);

        var events = new List<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events.Add);

        // Same physical device id, but a HID interface appeared (capabilities changed).
        repository.UpdateCache([
            new FakeYubiKey("ykphysical:103", ConnectionType.SmartCard | ConnectionType.HidFido)
        ]);

        Assert.Equal(2, events.Count);
        Assert.Equal(DeviceAction.Removed, events[0].Action);
        Assert.Equal(ConnectionType.SmartCard, events[0].Device.AvailableConnections);
        Assert.Equal(DeviceAction.Added, events[1].Action);
        Assert.Equal(ConnectionType.SmartCard | ConnectionType.HidFido, events[1].Device.AvailableConnections);
    }

    [Fact]
    public void UpdateCache_SamePhysicalIdUnchangedConnections_EmitsNoEvent()
    {
        using var repository = new YubiKeyDeviceRepository();
        repository.UpdateCache([new FakeYubiKey("ykphysical:103", ConnectionType.SmartCard | ConnectionType.HidOtp)]);

        var events = new List<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events.Add);

        repository.UpdateCache([new FakeYubiKey("ykphysical:103", ConnectionType.SmartCard | ConnectionType.HidOtp)]);

        Assert.Empty(events);
    }

    [Fact]
    public void UpdateCache_OnePhysicalDevice_EmitsSingleAddedNotPerInterface()
    {
        using var repository = new YubiKeyDeviceRepository();
        var events = new List<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events.Add);

        // A merged composite device is a single cache entry keyed by physical identity.
        repository.UpdateCache([
            new FakeYubiKey("ykphysical:103",
                ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp)
        ]);

        var evt = Assert.Single(events);
        Assert.Equal(DeviceAction.Added, evt.Action);
        Assert.Equal("ykphysical:103", evt.Device.DeviceId);
    }

    [Fact]
    public void UpdateCache_SamePidCompositeDifferentMemberIds_EmitsRemovedThenAdded()
    {
        using var repository = new YubiKeyDeviceRepository();
        var first = Composite("ykphysical:pid:0407", "pcsc:key-a", "hid:key-a");
        var second = Composite("ykphysical:pid:0407", "pcsc:key-b", "hid:key-b");
        repository.UpdateCache([first]);

        var events = new List<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events.Add);

        repository.UpdateCache([second]);

        Assert.Equal(2, events.Count);
        Assert.Equal(DeviceAction.Removed, events[0].Action);
        Assert.Same(first, events[0].Device);
        Assert.Equal(DeviceAction.Added, events[1].Action);
        Assert.Same(second, events[1].Device);
    }

    private static CompositeYubiKey Composite(string deviceId, string smartCardId, string hidFidoId) =>
        new(
            deviceId,
            [
                new FakeYubiKey(smartCardId, ConnectionType.SmartCard),
                new FakeYubiKey(hidFidoId, ConnectionType.HidFido)
            ],
            null);

    private sealed class FakeYubiKey(string deviceId, ConnectionType connectionType) : IYubiKey
    {
        public string DeviceId { get; } = deviceId;
        public ConnectionType AvailableConnections { get; } = connectionType;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
            => throw new NotSupportedException();
    }
}