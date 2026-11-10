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
using Yubico.YubiKit.Core.UnitTests.Infrastructure;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class SnapshotDiffTests
{
    [Fact]
    public void Apply_RemovalsAdditionsAndMetadataUpdates_AreGroupedInThatOrder()
    {
        var removed = new FakeYubiKey("removed");
        var replaced = new FakeYubiKey("replaced", ConnectionType.SmartCard);
        var retained = Published("old-tier", "retained", null);
        var current = SnapshotDiff.Apply([], [removed, replaced, retained], 1).Next;
        var replacement = new FakeYubiKey("replaced", ConnectionType.HidFido);
        var added = new FakeYubiKey("added");
        var metadata = default(DeviceInfo) with { SerialNumber = 103 };

        var (next, changes) = SnapshotDiff.Apply(current,
            [Published("new-tier", "retained", metadata), replacement, added], 2);

        Assert.Collection(changes,
            c => Assert.Same(removed, Assert.IsType<DeviceRemoved>(c).Device),
            c => Assert.Same(replaced, Assert.IsType<DeviceRemoved>(c).Device),
            c => Assert.Same(replacement, Assert.IsType<DeviceAdded>(c).Device),
            c => Assert.Same(added, Assert.IsType<DeviceAdded>(c).Device),
            c => Assert.Same(retained, Assert.IsType<DeviceUpdated>(c).Device));
        Assert.All(changes, c => Assert.Equal(2, c.Revision));
        Assert.Same(retained, next[retained.PhysicalIdentityKey]);
        Assert.Equal(metadata, retained.DeviceInfo);
        Assert.Equal(3, current.Count); // The input membership map was not changed.
        Assert.Same(replaced, current[YubiKeyDevice.PhysicalIdentityKeyFor(replaced)]);
    }

    [Theory]
    [InlineData(null, 103)]
    [InlineData(103, null)]
    [InlineData(103, 103)]
    public void Apply_UncontradictedSerialAndSameInterfaces_RetainsOriginallyPublishedObject(int? oldSerial, int? newSerial)
    {
        var first = Published("serial-tier", "a", oldSerial is { } serial ? default(DeviceInfo) with { SerialNumber = serial } : null);
        var current = SnapshotDiff.Apply([], [first], 1).Next;
        var latest = Published("pid-tier", "a", newSerial is { } updated ? default(DeviceInfo) with { SerialNumber = updated } : null);

        var (next, changes) = SnapshotDiff.Apply(current, [latest], 2);

        Assert.Same(first, Assert.Single(next).Value);
        Assert.DoesNotContain(changes, c => c is DeviceAdded or DeviceRemoved);
        Assert.Equal(latest.DeviceInfo ?? first.DeviceInfo, first.DeviceInfo);
        var removal = Assert.Single(SnapshotDiff.Apply(next, [], 3).Changes);
        Assert.Same(first, Assert.IsType<DeviceRemoved>(removal).Device);
    }

    [Fact]
    public void Apply_EqualMetadata_DoesNotEmitUpdate()
    {
        var metadata = default(DeviceInfo) with { SerialNumber = 103, IsLocked = true };
        var first = Published("first", "a", metadata);
        var current = SnapshotDiff.Apply([], [first], 1).Next;

        var (next, changes) = SnapshotDiff.Apply(current, [Published("next", "a", metadata with { })], 2);

        Assert.Empty(changes);
        Assert.Same(first, Assert.Single(next).Value);
    }

    [Fact]
    public void Apply_ChangedMetadata_UpdatesRetainedObjectAndEmitsUpdate()
    {
        var metadata = default(DeviceInfo) with { SerialNumber = 103 };
        var first = Published("first", "a", metadata);
        var current = SnapshotDiff.Apply([], [first], 1).Next;
        var changed = metadata with { IsLocked = true };

        var (next, changes) = SnapshotDiff.Apply(current, [Published("next", "a", changed)], 2);

        Assert.Same(first, Assert.IsType<DeviceUpdated>(Assert.Single(changes)).Device);
        Assert.Same(first, Assert.Single(next).Value);
        Assert.Equal(changed, first.DeviceInfo);
    }

    [Fact]
    public void Apply_MissingMetadata_PreservesLastSuccessfulMetadataWithoutUpdate()
    {
        var metadata = default(DeviceInfo) with { SerialNumber = 103, IsLocked = true };
        var first = Published("first", "a", metadata);
        var current = SnapshotDiff.Apply([], [first], 1).Next;

        var (next, changes) = SnapshotDiff.Apply(current, [Published("next", "a", null)], 2);

        Assert.Empty(changes);
        Assert.Same(first, Assert.Single(next).Value);
        Assert.Equal(metadata, first.DeviceInfo);
    }

    [Fact]
    public void Apply_DifferentKnownSerials_RepublishesWithoutMutatingPredecessor()
    {
        var oldMetadata = default(DeviceInfo) with { SerialNumber = 103 };
        var first = Published("first", "a", oldMetadata);
        var current = SnapshotDiff.Apply([], [first], 1).Next;
        var latest = Published("latest", "a", default(DeviceInfo) with { SerialNumber = 125 });

        var (next, changes) = SnapshotDiff.Apply(current, [latest], 2);

        Assert.Collection(changes,
            c => Assert.Same(first, Assert.IsType<DeviceRemoved>(c).Device),
            c => Assert.Same(latest, Assert.IsType<DeviceAdded>(c).Device));
        Assert.Same(latest, Assert.Single(next).Value);
        Assert.Equal(oldMetadata, first.DeviceInfo);
    }

    [Fact]
    public void Apply_SerialContradiction_DoesNotTrustCustomCorrelationOverride()
    {
        var first = new CorrelationOverridingYubiKey(103);
        var latest = new CorrelationOverridingYubiKey(125);
        var current = SnapshotDiff.Apply([], [first], 1).Next;

        var (next, changes) = SnapshotDiff.Apply(current, [latest], 2);

        Assert.Collection(changes,
            c => Assert.Same(first, Assert.IsType<DeviceRemoved>(c).Device),
            c => Assert.Same(latest, Assert.IsType<DeviceAdded>(c).Device));
        Assert.Same(latest, Assert.Single(next).Value);
    }

    [Fact]
    public void Apply_SameDeviceIdDifferentInterfaceSet_Republishes()
    {
        var first = Published("same-pid", "a", null);
        var latest = Published("same-pid", "b", null);
        var current = SnapshotDiff.Apply([], [first], 1).Next;

        var (next, changes) = SnapshotDiff.Apply(current, [latest], 2);

        Assert.Collection(changes,
            c => Assert.Same(first, Assert.IsType<DeviceRemoved>(c).Device),
            c => Assert.Same(latest, Assert.IsType<DeviceAdded>(c).Device));
        Assert.Same(latest, Assert.Single(next).Value);
    }

    [Theory]
    [InlineData(103)]
    [InlineData(125)]
    public void Apply_ConnectionSetChangedWithSameInterfaceSet_RepublishesOnce(int newSerial)
    {
        var first = new YubiKeyDevice("fido", null, new FakeSlot("a", ConnectionType.HidFido), null,
            default(DeviceInfo) with { SerialNumber = 103 });
        var latest = new YubiKeyDevice("otp", null, null, new FakeSlot("a", ConnectionType.HidOtp),
            default(DeviceInfo) with { SerialNumber = newSerial });
        var current = SnapshotDiff.Apply([], [first], 1).Next;

        var (next, changes) = SnapshotDiff.Apply(current, [latest], 2);

        Assert.Collection(changes,
            c => Assert.Same(first, Assert.IsType<DeviceRemoved>(c).Device),
            c => Assert.Same(latest, Assert.IsType<DeviceAdded>(c).Device));
        Assert.Same(latest, Assert.Single(next).Value);
    }

    [Fact]
    public void Apply_SiblingArrivalAndRemovalWithTierFlip_RetainsIncumbentAndCorrelatedRemoval()
    {
        var first = Assert.Single(CompositeDeviceMerger.Merge(Interfaces("a", null)));
        var current = SnapshotDiff.Apply([], [first], 1).Next;
        var (both, arrivals) = SnapshotDiff.Apply(current,
            CompositeDeviceMerger.Merge([.. Interfaces("a", 103), .. Interfaces("b", 125)]), 2);
        var sibling = Assert.IsType<DeviceAdded>(Assert.Single(arrivals)).Device;
        Assert.Equal("ykphysical:125", sibling.DeviceId);
        var (alone, removals) = SnapshotDiff.Apply(both, CompositeDeviceMerger.Merge(Interfaces("a", null)), 3);
        Assert.Same(sibling, Assert.IsType<DeviceRemoved>(Assert.Single(removals)).Device);
        Assert.Same(first, Assert.Single(alone).Value);
        Assert.Same(first, Assert.IsType<DeviceRemoved>(Assert.Single(SnapshotDiff.Apply(alone, [], 4).Changes)).Device);
    }

    [Fact]
    public void Apply_ReinsertionAfterObservedAbsence_PublishesNewObject()
    {
        var first = Published("first", "a", default(DeviceInfo) with { IsLocked = true });
        var current = SnapshotDiff.Apply([], [first], 1).Next;
        var empty = SnapshotDiff.Apply(current, [], 2).Next;
        var latest = Published("first", "a", null);
        var (next, changes) = SnapshotDiff.Apply(empty, [latest], 3);
        Assert.Same(latest, Assert.IsType<DeviceAdded>(Assert.Single(changes)).Device);
        Assert.NotSame(first, Assert.Single(next).Value);
        Assert.Null(latest.DeviceInfo);
    }

    private static YubiKeyDevice Published(string id, string tag, DeviceInfo? metadata) =>
        new(id, new FakeSlot($"pcsc:{tag}", ConnectionType.SmartCard),
            new FakeSlot($"hid:{tag}", ConnectionType.HidFido), null, metadata);

    private static DeviceInterfaceDescriptor[] Interfaces(string tag, int? serial) =>
    [
        new(new FakeSlot($"pcsc:{tag}", ConnectionType.SmartCard), ConnectionType.SmartCard, true, 0x0407, serial, null),
        new(new FakeSlot($"hid-fido:{tag}", ConnectionType.HidFido), ConnectionType.HidFido, true, 0x0407, serial, null),
        new(new FakeSlot($"hid-otp:{tag}", ConnectionType.HidOtp), ConnectionType.HidOtp, true, 0x0407, serial, null)
    ];

    private sealed class FakeSlot(string id, ConnectionType type) : IYubiKeyConnectionSlot
    {
        public string InterfaceId => id;
        public ConnectionType ConnectionType => type;
    }

    private sealed class CorrelationOverridingYubiKey(int serial) : IYubiKey
    {
        public string DeviceId => "same-interface";
        public ConnectionType AvailableConnections => ConnectionType.SmartCard;
        public int? SerialNumber => serial;
        public DeviceCorrelation SameDeviceAs(IYubiKey other) => DeviceCorrelation.Same;
        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection => throw new NotSupportedException();
    }
}