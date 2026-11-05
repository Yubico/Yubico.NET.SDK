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
        using var repository = new YubiKeyDeviceRepository();
        var stable = Published("stable", "hid:stable", ConnectionType.HidFido, 303);
        repository.UpdateCache([.. CompositeDeviceMerger.Merge([.. KeyInterfaces("a", null), .. KeyInterfaces("b", null)]), stable]);
        var initial = repository.GetAll();
        Assert.Equal(7, initial.Count);
        Assert.All(initial.Where(device => !ReferenceEquals(device, stable)), device =>
        {
            Assert.Single(Assert.IsType<YubiKeyDevice>(device).InterfaceIds);
            Assert.Null(device.CachedDeviceInfo);
        });

        repository.UpdateCache([.. CompositeDeviceMerger.Merge([.. KeyInterfaces("a", 103), .. KeyInterfaces("b", 125)]),
            Published("stable-new-evidence", "hid:stable", ConnectionType.HidFido, 303)]);
        var current = repository.GetAll();
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
        using var repository = new YubiKeyDeviceRepository();
        var previous = Published("old-evidence", "hid:a", ConnectionType.HidFido, initialSerial);
        var scan = Published("new-evidence", "hid:a", ConnectionType.HidFido, updatedSerial);
        repository.UpdateCache([previous]);
        var initial = repository.GetAll();
        repository.UpdateCache([scan]);
        var retained = repository.GetAll();

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
        using var repository = new YubiKeyDeviceRepository();
        var previous = Published("old", "hid:a", ConnectionType.HidFido, 103);
        var successor = Published("new", "hid:a", ConnectionType.HidOtp, 103);
        repository.UpdateCache([previous]);
        var initial = repository.GetAll();
        repository.UpdateCache([successor]);
        var current = repository.GetAll();

        Assert.Equal(YubiKeyDevice.PhysicalIdentityKeyFor(previous), YubiKeyDevice.PhysicalIdentityKeyFor(successor));
        Assert.NotEqual(previous.AvailableConnections, successor.AvailableConnections);
        Assert.Same(successor, Assert.Single(current));
        CompositeDiscoveryIntegrationTests.AssertMetadataReadyAndStableGroupsRetained(initial, current);
    }

    [Fact]
    public void MetadataExpectation_DifferentKnownSerialOnStableSets_AllowsRepositoryReplacement()
    {
        using var repository = new YubiKeyDeviceRepository();
        var previous = Published("old", "hid:a", ConnectionType.HidFido, 103);
        var successor = Published("new", "hid:a", ConnectionType.HidFido, 125);
        repository.UpdateCache([previous]);
        var initial = repository.GetAll();
        repository.UpdateCache([successor]);
        var current = repository.GetAll();

        Assert.Equal(YubiKeyDevice.PhysicalIdentityKeyFor(previous), YubiKeyDevice.PhysicalIdentityKeyFor(successor));
        Assert.Equal(previous.AvailableConnections, successor.AvailableConnections);
        Assert.Same(successor, Assert.Single(current));
        Assert.Equal(103, previous.SerialNumber);
        CompositeDiscoveryIntegrationTests.AssertMetadataReadyAndStableGroupsRetained(initial, current);
    }

    [Fact]
    public void MetadataExpectation_DifferentMembersWithSameCounts_RejectsInterfaceLoss()
    {
        using var repository = new YubiKeyDeviceRepository();
        repository.UpdateCache([Published("old", "hid:a", ConnectionType.HidFido, 103)]);
        var initial = repository.GetAll();
        repository.UpdateCache([Published("new", "hid:b", ConnectionType.HidFido, 103)]);

        Assert.ThrowsAny<XunitException>(() =>
            CompositeDiscoveryIntegrationTests.AssertMetadataReadyAndStableGroupsRetained(initial, repository.GetAll()));
    }

    [Fact]
    public void MetadataExpectation_UnchangedGroupWithoutMetadata_StillFailsReadiness()
    {
        using var repository = new YubiKeyDeviceRepository();
        var unknown = new YubiKeyDevice("unknown", smartCard: null,
            new Slot("hid:a", ConnectionType.HidFido), hidOtp: null, deviceInfo: null);
        repository.UpdateCache([unknown]);
        var initial = repository.GetAll();
        repository.UpdateCache([unknown]);

        Assert.Throws<AllException>(() =>
            CompositeDiscoveryIntegrationTests.AssertMetadataReadyAndStableGroupsRetained(initial, repository.GetAll()));
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