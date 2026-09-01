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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.UnitTests.Infrastructure;

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

        var events = new RecordingObserver<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events);

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

        var events = new RecordingObserver<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events);

        repository.UpdateCache([new FakeYubiKey("ykphysical:103", ConnectionType.SmartCard | ConnectionType.HidOtp)]);

        Assert.Empty(events);
    }

    [Fact]
    public void UpdateCache_OnePhysicalDevice_EmitsSingleAddedNotPerInterface()
    {
        using var repository = new YubiKeyDeviceRepository();
        var events = new RecordingObserver<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events);

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

        var events = new RecordingObserver<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events);

        repository.UpdateCache([second]);

        Assert.Equal(2, events.Count);
        Assert.Equal(DeviceAction.Removed, events[0].Action);
        Assert.Same(first, events[0].Device);
        Assert.Equal(DeviceAction.Added, events[1].Action);
        Assert.Same(second, events[1].Device);
    }

    [Fact]
    public void Merge_SurvivingKeyAfterSiblingRemoval_FlipsDeviceIdTierButNotMemberIds()
    {
        // Documents the tier flip this file's diff-stability tests exist to absorb. Two same-PID keys are
        // resolved by serial evidence; with only one key left the PID tier resolves it instead. The
        // composite DeviceId therefore encodes WHICH EVIDENCE resolved the key, and changes when the
        // evidence changes even though the physical key never moved. The member interface ids do not.
        var both = CompositeDeviceMerger.Merge([.. KeyInterfaces("a", 103), .. KeyInterfaces("b", 125)]);
        var survivorWhileSiblingPresent = Assert.IsType<CompositeYubiKey>(
            Assert.Single(both, d => d.DeviceId == "ykphysical:103"));

        var alone = CompositeDeviceMerger.Merge(KeyInterfaces("a", null));
        var survivorAlone = Assert.IsType<CompositeYubiKey>(Assert.Single(alone));

        Assert.NotEqual(survivorWhileSiblingPresent.DeviceId, survivorAlone.DeviceId);
        Assert.Equal("ykphysical:pid:0407", survivorAlone.DeviceId);
        Assert.Equal(survivorWhileSiblingPresent.MemberDeviceIds, survivorAlone.MemberDeviceIds);
    }

    [Fact]
    public void UpdateCache_SiblingSamePidKeyRemoved_SurvivorEmitsNoRemovedOrAdded()
    {
        using var repository = new YubiKeyDeviceRepository();
        repository.UpdateCache(CompositeDeviceMerger.Merge([.. KeyInterfaces("a", 103), .. KeyInterfaces("b", 125)]));

        var events = new RecordingObserver<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events);

        // Key B unplugged. Key A did not move — same three interfaces, same interface paths — but its
        // composite DeviceId flips from the serial tier to the PID tier.
        repository.UpdateCache(CompositeDeviceMerger.Merge(KeyInterfaces("a", null)));

        var evt = Assert.Single(events);
        Assert.Equal(DeviceAction.Removed, evt.Action);
        Assert.Equal("ykphysical:125", evt.Device.DeviceId);
    }

    [Fact]
    public void UpdateCache_SiblingSamePidKeyArrives_IncumbentEmitsNoRemovedOrAdded()
    {
        using var repository = new YubiKeyDeviceRepository();
        repository.UpdateCache(CompositeDeviceMerger.Merge(KeyInterfaces("a", null)));

        var events = new RecordingObserver<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events);

        // Key B plugged in. Key A did not move, but its DeviceId flips back from the PID tier to the
        // serial tier.
        repository.UpdateCache(CompositeDeviceMerger.Merge([.. KeyInterfaces("a", 103), .. KeyInterfaces("b", 125)]));

        var evt = Assert.Single(events);
        Assert.Equal(DeviceAction.Added, evt.Action);
        Assert.Equal("ykphysical:125", evt.Device.DeviceId);
    }

    [Fact]
    public void UpdateCache_TierFlipThenFinalRemoval_RemovalUsesPreviouslyAddedDeviceId()
    {
        using var repository = new YubiKeyDeviceRepository();
        var events = new RecordingObserver<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events);

        repository.UpdateCache(CompositeDeviceMerger.Merge([.. KeyInterfaces("a", 103), .. KeyInterfaces("b", 125)]));
        repository.UpdateCache(CompositeDeviceMerger.Merge(KeyInterfaces("a", null)));
        repository.UpdateCache([]);

        var added = Assert.Single(events, e =>
            e.Action == DeviceAction.Added && e.Device.DeviceId == "ykphysical:103");
        var removed = Assert.Single(events, e =>
            e.Action == DeviceAction.Removed &&
            e.Device is CompositeYubiKey composite &&
            composite.MemberDeviceIds.Contains("pcsc:a"));

        Assert.Equal(added.Device.DeviceId, removed.Device.DeviceId);
    }

    // INVARIANT PIN (not fix evidence): a genuinely removed physical key still emits Removed.
    [Fact]
    public void UpdateCache_CompositeKeyUnplugged_EmitsRemoved()
    {
        using var repository = new YubiKeyDeviceRepository();
        var key = Composite("ykphysical:pid:0407", "pcsc:a", "hid-fido:a");
        repository.UpdateCache([key]);

        var events = new RecordingObserver<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events);

        repository.UpdateCache([]);

        var evt = Assert.Single(events);
        Assert.Equal(DeviceAction.Removed, evt.Action);
        Assert.Same(key, evt.Device);
    }

    // INVARIANT PIN (not fix evidence): a genuinely added physical key still emits Added.
    [Fact]
    public void UpdateCache_CompositeKeyPluggedIn_EmitsAdded()
    {
        using var repository = new YubiKeyDeviceRepository();
        repository.UpdateCache([]);

        var events = new RecordingObserver<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events);

        var key = Composite("ykphysical:pid:0407", "pcsc:a", "hid-fido:a");
        repository.UpdateCache([key]);

        var evt = Assert.Single(events);
        Assert.Equal(DeviceAction.Added, evt.Action);
        Assert.Same(key, evt.Device);
    }

    private const ushort FullKeyPid = 0x0407; // OTP + FIDO + CCID

    /// <summary>The three USB interfaces of one full-triple physical key, tagged so ids are per-key.</summary>
    private static DeviceInterfaceDescriptor[] KeyInterfaces(string tag, int? serial) =>
    [
        Usb($"pcsc:{tag}", ConnectionType.SmartCard, serial),
        Usb($"hid-fido:{tag}", ConnectionType.HidFido, serial),
        Usb($"hid-otp:{tag}", ConnectionType.HidOtp, serial)
    ];

    private static DeviceInterfaceDescriptor Usb(string id, ConnectionType connection, int? serial) =>
        new(new FakeYubiKey(id, connection), connection, IsUsb: true, FullKeyPid, serial, null);

    private static CompositeYubiKey Composite(string deviceId, string smartCardId, string hidFidoId) =>
        new(
            deviceId,
            [
                new FakeYubiKey(smartCardId, ConnectionType.SmartCard),
                new FakeYubiKey(hidFidoId, ConnectionType.HidFido)
            ],
            null);
}