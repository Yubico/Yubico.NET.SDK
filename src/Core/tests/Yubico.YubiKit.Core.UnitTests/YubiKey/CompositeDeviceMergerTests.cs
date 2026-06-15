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

using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.UnitTests.CoreYubiKey;

public class CompositeDeviceMergerTests
{
    private const ushort FullKeyPid = 0x0407; // OTP+FIDO+CCID
    private const ushort SkyPid = 0x0120;

    private static DeviceInterfaceDescriptor Usb(
        string id, ConnectionType connection, ushort? pid, int? serial = null, DeviceInfo? info = null) =>
        new(new FakeYubiKey(id, connection), connection, IsUsb: true, pid, serial, info);

    private static DeviceInterfaceDescriptor Nfc(string id) =>
        new(new FakeYubiKey(id, ConnectionType.SmartCard), ConnectionType.SmartCard, IsUsb: false, null, null, null);

    [Fact]
    public void Merge_FullKeySamePid_MergesByPidWithoutSerial()
    {
        var merged = CompositeDeviceMerger.Merge([
            Usb("pcsc:cc", ConnectionType.SmartCard, FullKeyPid),
            Usb("hid:fido", ConnectionType.HidFido, FullKeyPid),
            Usb("hid:otp", ConnectionType.HidOtp, FullKeyPid)
        ]);

        var composite = Assert.IsType<CompositeYubiKey>(Assert.Single(merged));
        Assert.Equal(ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp,
            composite.AvailableConnections);
        Assert.Equal("ykphysical:pid:0407", composite.DeviceId);
    }

    [Fact]
    public void Merge_SkySingleFidoInterface_PassesThroughAsOneDevice()
    {
        // SKY (Security Key): FIDO-HID only, no serial — passes through as one device, keyed by PID alone.
        var sky = new FakeYubiKey("hid:fido", ConnectionType.HidFido);
        var merged = CompositeDeviceMerger.Merge([
            new DeviceInterfaceDescriptor(sky, ConnectionType.HidFido, IsUsb: true, SkyPid, null, null)
        ]);

        Assert.Same(sky, Assert.Single(merged));
    }

    [Fact]
    public void Merge_SeriallessMultiInterfaceSamePid_MergesByPid()
    {
        // The Phase 37.5 fix: a serial-less key exposing several interfaces of one PID merges by PID.
        var merged = CompositeDeviceMerger.Merge([
            Usb("hid:fido", ConnectionType.HidFido, FullKeyPid),
            Usb("hid:otp", ConnectionType.HidOtp, FullKeyPid)
        ]);

        var composite = Assert.IsType<CompositeYubiKey>(Assert.Single(merged));
        Assert.Equal(ConnectionType.HidFido | ConnectionType.HidOtp, composite.AvailableConnections);
    }

    [Fact]
    public void Merge_DisjointPartialSamePidWithoutSerial_DoesNotMergeAcrossPossibleKeys()
    {
        var merged = CompositeDeviceMerger.Merge([
            Usb("pcsc:key-a", ConnectionType.SmartCard, FullKeyPid),
            Usb("hid-fido:key-b", ConnectionType.HidFido, FullKeyPid)
        ]);

        Assert.Equal(2, merged.Count);
        Assert.DoesNotContain(merged, d => d is CompositeYubiKey);
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
        Assert.All(merged, d => Assert.IsType<CompositeYubiKey>(d));
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
        var composite = Assert.Single(merged.OfType<CompositeYubiKey>());
        Assert.Equal(ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp, composite.AvailableConnections);
        Assert.Contains(merged, d => d.DeviceId == "pcsc:nfc" && d is not CompositeYubiKey);
    }

    [Fact]
    public void Merge_NullPidUsb_NotForceSerial_StandsAlone()
    {
        var single = new FakeYubiKey("pcsc:cc", ConnectionType.SmartCard);
        var merged = CompositeDeviceMerger.Merge([
            new DeviceInterfaceDescriptor(single, ConnectionType.SmartCard, IsUsb: true, null, null, null)
        ]);

        Assert.Same(single, Assert.Single(merged));
    }

    [Fact]
    public void Merge_UnknownPid_TreatedAsNullAndStandsAlone()
    {
        var single = new FakeYubiKey("hid:weird", ConnectionType.HidFido);
        var merged = CompositeDeviceMerger.Merge([
            new DeviceInterfaceDescriptor(single, ConnectionType.HidFido, IsUsb: true, 0x9999, null, null)
        ]);

        Assert.Same(single, Assert.Single(merged));
    }

    [Fact]
    public void Merge_ForceSerial_MergesAllUsbBySerial_RejoiningUnparsedCcid()
    {
        // Reader-name drift: unparsed CCID (null PID) + HID sibling, both with serial 103, forceSerial=true.
        var merged = CompositeDeviceMerger.Merge(
            [
                Usb("pcsc:cc", ConnectionType.SmartCard, null, serial: 103),
                Usb("hid:otp", ConnectionType.HidOtp, FullKeyPid, serial: 103)
            ],
            forceSerialMerge: true);

        var composite = Assert.IsType<CompositeYubiKey>(Assert.Single(merged));
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

        var composite = merged.OfType<CompositeYubiKey>().Single(c => c.DeviceId == "ykphysical:103");
        Assert.NotNull(composite.DeviceInfo);
        Assert.Equal(103, composite.DeviceInfo!.Value.SerialNumber);
    }

    private sealed class FakeYubiKey(string deviceId, ConnectionType connectionType) : IYubiKey
    {
        public string DeviceId { get; } = deviceId;
        public ConnectionType AvailableConnections { get; } = connectionType;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
            => throw new NotSupportedException("FakeYubiKey does not support connections.");
    }
}