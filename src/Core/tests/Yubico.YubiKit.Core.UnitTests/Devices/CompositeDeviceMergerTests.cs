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

public class CompositeDeviceMergerTests
{
    private const ushort FullKeyPid = 0x0407; // OTP+FIDO+CCID
    private const ushort SkyPid = 0x0120;

    private static DeviceInterfaceDescriptor Usb(
        string id, ConnectionType connection, ushort? pid, int? serial = null, DeviceInfo? info = null) =>
        new(new FakeSlot(id, connection), connection, IsUsb: true, pid, serial, info);

    private static DeviceInterfaceDescriptor Nfc(string id) =>
        new(new FakeSlot(id, ConnectionType.SmartCard), ConnectionType.SmartCard, IsUsb: false, null, null, null);

    [Fact]
    public void Merge_FullKeySamePid_MergesByPidWithoutSerial()
    {
        var merged = CompositeDeviceMerger.Merge([
            Usb("pcsc:cc", ConnectionType.SmartCard, FullKeyPid),
            Usb("hid:fido", ConnectionType.HidFido, FullKeyPid),
            Usb("hid:otp", ConnectionType.HidOtp, FullKeyPid)
        ]);

        var composite = Assert.Single(merged);
        Assert.Equal(ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp,
            composite.AvailableConnections);
        Assert.Equal("ykphysical:pid:0407", composite.DeviceId);
        Assert.Equal(["hid:fido", "hid:otp", "pcsc:cc"], InterfaceIdsOf(composite));
    }
    [Fact]
    public void Merge_SkySingleFidoInterface_PublishesFlatOneSlotDeviceWithTransportId()
    {
        // SKY (Security Key): FIDO-HID only, no serial — passes through as one device, keyed by PID alone.
        var sky = new FakeSlot("hid:fido", ConnectionType.HidFido);
        var merged = CompositeDeviceMerger.Merge([
            new DeviceInterfaceDescriptor(sky, ConnectionType.HidFido, IsUsb: true, SkyPid, null, null)
        ]);

        var published = Assert.Single(merged);
        Assert.NotSame(sky, published);
        Assert.Equal(sky.InterfaceId, published.DeviceId);
        Assert.Equal(ConnectionType.HidFido, published.AvailableConnections);
        Assert.Equal("8:hid:fido", YubiKeyDevice.PhysicalIdentityKeyFor(published));
    }

    [Fact]
    public void Merge_PartialSeriallessSamePid_TwoHidNoCcid_StaysConservativelySplit()
    {
        // Phase-2 generalized guard (composite-merge remediation PLAN.md, verified premise 4b): two HID
        // interfaces of a full-triple PID with no CCID and no serials are byte-indistinguishable from the
        // disjoint interfaces of TWO same-model keys, so the merger keeps them conservatively split until
        // the observed set equals the PID's expected set (or serial/topology evidence arrives). This
        // replaces the pre-Phase-2 behavior ("Phase 37.5": merge any same-PID interfaces when the PID
        // count is 1), which could fuse two physical keys.
        var merged = CompositeDeviceMerger.Merge([
            Usb("hid:fido", ConnectionType.HidFido, FullKeyPid),
            Usb("hid:otp", ConnectionType.HidOtp, FullKeyPid)
        ]);

        Assert.Equal(2, merged.Count);
        Assert.All(merged, d => Assert.Single(InterfaceIdsOf(d)));
    }

    [Fact]
    public void Merge_DisjointPartialSamePidWithoutSerial_DoesNotMergeAcrossPossibleKeys()
    {
        var merged = CompositeDeviceMerger.Merge([
            Usb("pcsc:key-a", ConnectionType.SmartCard, FullKeyPid),
            Usb("hid-fido:key-b", ConnectionType.HidFido, FullKeyPid)
        ]);

        Assert.Equal(2, merged.Count);
        Assert.All(merged, d => Assert.Single(InterfaceIdsOf(d)));
    }

    [Fact]
    public void Merge_TwoSamePidKeysWithSerials_StayTwoDevices()
    {
        var merged = CompositeDeviceMerger.Merge([
            Usb("pcsc:101", ConnectionType.SmartCard, FullKeyPid, serial: 101),
            Usb("hid:otp:101", ConnectionType.HidOtp, FullKeyPid, serial: 101),
            Usb("pcsc:102", ConnectionType.SmartCard, FullKeyPid, serial: 102),
            Usb("hid:otp:102", ConnectionType.HidOtp, FullKeyPid, serial: 102)
        ]);

        Assert.Equal(2, merged.Count);
        Assert.All(merged, d => Assert.Equal(2, InterfaceIdsOf(d).Count));
        Assert.Contains(merged, d => d.DeviceId == "ykphysical:101");
        Assert.Contains(merged, d => d.DeviceId == "ykphysical:102");
    }

    [Fact]
    public void Merge_NfcReader_StandaloneNeverMergedWithUsb()
    {
        var merged = CompositeDeviceMerger.Merge([
            Usb("pcsc:usb", ConnectionType.SmartCard, FullKeyPid),
            Usb("hid:fido", ConnectionType.HidFido, FullKeyPid),
            Usb("hid:otp", ConnectionType.HidOtp, FullKeyPid),
            Nfc("pcsc:nfc")
        ]);

        Assert.Equal(2, merged.Count);
        var composite = Assert.Single(merged, d => InterfaceIdsOf(d).Count > 1);
        Assert.Equal(ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp, composite.AvailableConnections);
        Assert.Contains(merged, d => d.DeviceId == "pcsc:nfc" && InterfaceIdsOf(d).Count == 1);
    }

    [Fact]
    public void Merge_NullPidUsb_NotForceSerial_StandsAlone()
    {
        var single = new FakeSlot("pcsc:cc", ConnectionType.SmartCard);
        var merged = CompositeDeviceMerger.Merge([
            new DeviceInterfaceDescriptor(single, ConnectionType.SmartCard, IsUsb: true, null, null, null)
        ]);

        var published = Assert.Single(merged);
        Assert.NotSame(single, published);
        Assert.Equal(single.InterfaceId, published.DeviceId);
        Assert.Equal(ConnectionType.SmartCard, published.AvailableConnections);
    }

    [Fact]
    public void Merge_UnknownPid_TreatedAsNullAndStandsAlone()
    {
        var single = new FakeSlot("hid:weird", ConnectionType.HidFido);
        var merged = CompositeDeviceMerger.Merge([
            new DeviceInterfaceDescriptor(single, ConnectionType.HidFido, IsUsb: true, 0x9999, null, null)
        ]);

        var published = Assert.Single(merged);
        Assert.NotSame(single, published);
        Assert.Equal(single.InterfaceId, published.DeviceId);
        Assert.Equal(ConnectionType.HidFido, published.AvailableConnections);
    }

    [Fact]
    public void Merge_ForceSerial_MergesAllUsbBySerial_RejoiningUnparsedCcid()
    {
        // Reader-name drift: unparsed CCID (null PID) + HID sibling, both serial 103, PID correlation untrusted.
        var merged = CompositeDeviceMerger.Merge(
            [
                Usb("pcsc:cc", ConnectionType.SmartCard, null, serial: 103),
                Usb("hid:otp", ConnectionType.HidOtp, FullKeyPid, serial: 103)
            ],
            pidCorrelationUntrusted: true);

        var composite = Assert.Single(merged);
        Assert.Equal(ConnectionType.SmartCard | ConnectionType.HidOtp, composite.AvailableConnections);
        Assert.Equal("ykphysical:103", composite.DeviceId);
    }

    [Fact]
    public void Merge_SerialPath_CachesDiscoveryDeviceInfo()
    {
        var info = default(DeviceInfo) with { FirmwareVersion = new FirmwareVersion(5, 7, 2), SerialNumber = 103 };

        var merged = CompositeDeviceMerger.Merge([
            Usb("pcsc:103", ConnectionType.SmartCard, FullKeyPid, serial: 103, info: info),
            Usb("hid:otp:103", ConnectionType.HidOtp, FullKeyPid, serial: 103),
            // Second same-PID key forces the serial path.
            Usb("pcsc:104", ConnectionType.SmartCard, FullKeyPid, serial: 104),
            Usb("hid:otp:104", ConnectionType.HidOtp, FullKeyPid, serial: 104)
        ]);

        var composite = Assert.Single(merged, c => c.DeviceId == "ykphysical:103");
        var published = Assert.IsType<YubiKeyDevice>(composite);
        Assert.NotNull(published.DeviceInfo);
        Assert.Equal(103, published.DeviceInfo!.Value.SerialNumber);
    }
    private sealed class FakeSlot(string deviceId, ConnectionType connectionType) : IYubiKeyConnectionSlot
    {
        public string InterfaceId { get; } = deviceId;
        public ConnectionType ConnectionType { get; } = connectionType;
    }

    private static IReadOnlyList<string> InterfaceIdsOf(IYubiKey device) =>
        Assert.IsType<YubiKeyDevice>(device).InterfaceIds;
}
