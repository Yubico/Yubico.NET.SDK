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

using Xunit.Sdk;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.IntegrationTests.Devices;

/// <summary>Hardware-free checks of the monitor hardware test's actual retention expectation.</summary>
public class CompositeDiscoveryRetentionTests
{
    [Fact]
    public void MetadataExpectation_LaterSerialEvidenceRegroupsStandaloneMembers_RetainsOnlyUnchangedGroups()
    {
        var stable = Published("stable", "hid:stable", ConnectionType.HidFido, 303);
        var first = SnapshotDiff.Apply([], [.. CompositeDeviceMerger.Merge([.. KeyInterfaces("a", null), .. KeyInterfaces("b", null)]), stable], 1).Next;
        var initial = first.Values.ToList();
        Assert.Equal(7, initial.Count);
        Assert.All(initial.Where(device => !ReferenceEquals(device, stable)), device =>
        {
            Assert.Single(Assert.IsType<YubiKeyDevice>(device).InterfaceIds);
            Assert.Null(device.CachedDeviceInfo);
        });

        var next = SnapshotDiff.Apply(first, [.. CompositeDeviceMerger.Merge([.. KeyInterfaces("a", 103), .. KeyInterfaces("b", 125)]),
            Published("stable-new-evidence", "hid:stable", ConnectionType.HidFido, 303)], 2).Next;
        var current = next.Values.ToList();
        Assert.Equal(3, current.Count);
        Assert.All(current.Where(device => !ReferenceEquals(device, stable)), device =>
        {
            Assert.Equal(3, Assert.IsType<YubiKeyDevice>(device).InterfaceIds.Count);
            Assert.DoesNotContain(initial, previous => ReferenceEquals(previous, device));
        });
        Assert.Contains(current, device => ReferenceEquals(device, stable));

        CompositeDiscoveryIntegrationTests.AssertMetadataReadyAndStableGroupsRetained(initial, current);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, 103)]
    [InlineData(103, null)]
    [InlineData(103, 103)]
    public void MetadataExpectation_UnchangedMembersAndConnections_RejectsWrongReplacement(int? initialSerial, int? updatedSerial)
    {
        var previous = Published("old-evidence", "hid:a", ConnectionType.HidFido, initialSerial);
        var scan = Published("new-evidence", "hid:a", ConnectionType.HidFido, updatedSerial);
        var first = SnapshotDiff.Apply([], [previous], 1).Next;
        var initial = first.Values.ToList();
        var retained = SnapshotDiff.Apply(first, [scan], 2).Next.Values.ToList();

        Assert.NotEqual(previous.DeviceId, scan.DeviceId);
        Assert.Equal(YubiKeyDevice.PhysicalIdentityKeyFor(previous), YubiKeyDevice.PhysicalIdentityKeyFor(scan));
        Assert.Same(previous, Assert.Single(retained));
        CompositeDiscoveryIntegrationTests.AssertMetadataReadyAndStableGroupsRetained(initial, retained);

        // A fresh object with identical members/connection flags is not a legitimate regrouping.
        Assert.ThrowsAny<XunitException>(() =>
            CompositeDiscoveryIntegrationTests.AssertMetadataReadyAndStableGroupsRetained(initial, [scan]));
    }

    [Fact]
    public void MetadataExpectation_SameMembersWithChangedConnections_AllowsRepositoryReplacement()
    {
        var previous = Published("old", "hid:a", ConnectionType.HidFido, 103);
        var successor = Published("new", "hid:a", ConnectionType.HidOtp, 103);
        var first = SnapshotDiff.Apply([], [previous], 1).Next;
        var initial = first.Values.ToList();
        var current = SnapshotDiff.Apply(first, [successor], 2).Next.Values.ToList();

        Assert.Equal(YubiKeyDevice.PhysicalIdentityKeyFor(previous), YubiKeyDevice.PhysicalIdentityKeyFor(successor));
        Assert.NotEqual(previous.AvailableConnections, successor.AvailableConnections);
        Assert.Same(successor, Assert.Single(current));
        CompositeDiscoveryIntegrationTests.AssertMetadataReadyAndStableGroupsRetained(initial, current);
    }

    [Fact]
    public void MetadataExpectation_DifferentKnownSerialOnStableSets_AllowsRepositoryReplacement()
    {
        var previous = Published("old", "hid:a", ConnectionType.HidFido, 103);
        var successor = Published("new", "hid:a", ConnectionType.HidFido, 125);
        var first = SnapshotDiff.Apply([], [previous], 1).Next;
        var initial = first.Values.ToList();
        var current = SnapshotDiff.Apply(first, [successor], 2).Next.Values.ToList();

        Assert.Equal(YubiKeyDevice.PhysicalIdentityKeyFor(previous), YubiKeyDevice.PhysicalIdentityKeyFor(successor));
        Assert.Equal(previous.AvailableConnections, successor.AvailableConnections);
        Assert.Same(successor, Assert.Single(current));
        Assert.Equal(103, previous.SerialNumber);
        CompositeDiscoveryIntegrationTests.AssertMetadataReadyAndStableGroupsRetained(initial, current);
    }

    [Fact]
    public void MetadataExpectation_DifferentMembersWithSameCounts_RejectsInterfaceLoss()
    {
        var first = SnapshotDiff.Apply([], [Published("old", "hid:a", ConnectionType.HidFido, 103)], 1).Next;
        var initial = first.Values.ToList();
        var current = SnapshotDiff.Apply(first, [Published("new", "hid:b", ConnectionType.HidFido, 103)], 2).Next.Values.ToList();

        Assert.ThrowsAny<XunitException>(() =>
            CompositeDiscoveryIntegrationTests.AssertMetadataReadyAndStableGroupsRetained(initial, current));
    }

    [Fact]
    public void MetadataExpectation_UnchangedGroupWithoutMetadata_StillFailsReadiness()
    {
        var unknown = new YubiKeyDevice("unknown", smartCard: null,
            new Slot("hid:a", ConnectionType.HidFido), hidOtp: null, deviceInfo: null);
        var first = SnapshotDiff.Apply([], [unknown], 1).Next;
        var initial = first.Values.ToList();
        var current = SnapshotDiff.Apply(first, [unknown], 2).Next.Values.ToList();

        Assert.Throws<AllException>(() =>
            CompositeDiscoveryIntegrationTests.AssertMetadataReadyAndStableGroupsRetained(initial, current));
    }

    private static DeviceInterfaceDescriptor[] KeyInterfaces(string tag, int? serial) =>
    [
        Usb($"pcsc:{tag}", ConnectionType.SmartCard, serial),
        Usb($"hid-fido:{tag}", ConnectionType.HidFido, serial),
        Usb($"hid-otp:{tag}", ConnectionType.HidOtp, serial)
    ];

    private static DeviceInterfaceDescriptor Usb(string id, ConnectionType type, int? serial) =>
        new(new Slot(id, type), type, IsUsb: true, Pid: 0x0407, serial,
            serial is not null ? Metadata(serial) : null);

    private static DeviceInfo Metadata(int? serial) =>
        DeviceInfo.CreateFromTlvs([], new FirmwareVersion(5, 8, 0)) with { SerialNumber = serial };

    private static YubiKeyDevice Published(string evidenceId, string memberId, ConnectionType type, int? serial) =>
        new(evidenceId,
            type == ConnectionType.SmartCard ? new Slot(memberId, type) : null,
            type == ConnectionType.HidFido ? new Slot(memberId, type) : null,
            type == ConnectionType.HidOtp ? new Slot(memberId, type) : null,
            Metadata(serial));

    private sealed class Slot(string id, ConnectionType type) : IYubiKeyConnectionSlot
    {
        public string InterfaceId => id;
        public ConnectionType ConnectionType => type;
    }
}