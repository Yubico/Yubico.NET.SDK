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
    private static DeviceInterfaceDescriptor Usb(string id, ConnectionType connection, int? serial, DeviceInfo? info = null) =>
        new(new FakeYubiKey(id, connection), connection, IsUsb: true, serial, info);

    private static DeviceInterfaceDescriptor Nfc(string id, int? serial) =>
        new(new FakeYubiKey(id, ConnectionType.SmartCard), ConnectionType.SmartCard, IsUsb: false, serial, null);

    [Fact]
    public void Merge_ThreeInterfacesSameSerial_ProducesOneCompositeWithUnionedConnections()
    {
        var merged = CompositeDeviceMerger.Merge([
            Usb("hid:fido", ConnectionType.HidFido, 103),
            Usb("pcsc:cc", ConnectionType.SmartCard, 103),
            Usb("hid:otp", ConnectionType.HidOtp, 103)
        ]);

        var device = Assert.Single(merged);
        var composite = Assert.IsType<CompositeYubiKey>(device);
        Assert.Equal(ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp,
            composite.AvailableConnections);
        Assert.Equal("ykphysical:103", composite.DeviceId);
    }

    [Fact]
    public void Merge_TwoKeysDifferentSerials_StayTwoDevicesEachPairingOwnInterfaces()
    {
        var merged = CompositeDeviceMerger.Merge([
            Usb("hid:otp:101", ConnectionType.HidOtp, 101),
            Usb("pcsc:101", ConnectionType.SmartCard, 101),
            Usb("hid:otp:102", ConnectionType.HidOtp, 102),
            Usb("pcsc:102", ConnectionType.SmartCard, 102)
        ]);

        Assert.Equal(2, merged.Count);
        Assert.All(merged, d => Assert.IsType<CompositeYubiKey>(d));
        Assert.Contains(merged, d => d.DeviceId == "ykphysical:101"
            && d.AvailableConnections == (ConnectionType.SmartCard | ConnectionType.HidOtp));
        Assert.Contains(merged, d => d.DeviceId == "ykphysical:102"
            && d.AvailableConnections == (ConnectionType.SmartCard | ConnectionType.HidOtp));
    }

    [Fact]
    public void Merge_UsbInterfacesWithoutSerial_DoNotCollapse()
    {
        var merged = CompositeDeviceMerger.Merge([
            Usb("pcsc:cc", ConnectionType.SmartCard, serial: null),
            Usb("hid:otp", ConnectionType.HidOtp, serial: null)
        ]);

        Assert.Equal(2, merged.Count);
        Assert.DoesNotContain(merged, d => d is CompositeYubiKey);
        Assert.Contains(merged, d => d.DeviceId == "pcsc:cc");
        Assert.Contains(merged, d => d.DeviceId == "hid:otp");
    }

    [Fact]
    public void Merge_SeriallessSingleInterface_SkyStyle_PassesThroughAsOneDevice()
    {
        // SKY-series (Security Key) devices report no serial number and typically expose only the FIDO HID
        // interface. A single serial-less interface needs no merge and passes through as one device.
        var sky = new FakeYubiKey("hid:fido", ConnectionType.HidFido);
        var merged = CompositeDeviceMerger.Merge([
            new DeviceInterfaceDescriptor(sky, ConnectionType.HidFido, IsUsb: true, null, null)
        ]);

        var device = Assert.Single(merged);
        Assert.Same(sky, device);
        Assert.IsNotType<CompositeYubiKey>(device);
    }

    [Fact]
    public void Merge_SeriallessMultiInterface_DoesNotMerge_ConservativeNoCollapse()
    {
        // A serial-less key exposing more than one interface cannot be merged on serial evidence, so each
        // interface stands alone (conservative no-collapse). This is the known limitation for serial-less
        // multi-interface keys; merging them would require PID/topology evidence (deferred).
        var merged = CompositeDeviceMerger.Merge([
            Usb("hid:fido", ConnectionType.HidFido, serial: null),
            Usb("hid:otp", ConnectionType.HidOtp, serial: null)
        ]);

        Assert.Equal(2, merged.Count);
        Assert.DoesNotContain(merged, d => d is CompositeYubiKey);
    }

    [Fact]
    public void Merge_NfcReader_NeverMergedWithUsbEvenOnSharedSerial()
    {
        var merged = CompositeDeviceMerger.Merge([
            Usb("pcsc:usb", ConnectionType.SmartCard, 103),
            Usb("hid:fido", ConnectionType.HidFido, 103),
            Nfc("pcsc:nfc", 103)
        ]);

        Assert.Equal(2, merged.Count);
        var composite = Assert.Single(merged.OfType<CompositeYubiKey>());
        Assert.Equal(ConnectionType.SmartCard | ConnectionType.HidFido, composite.AvailableConnections);
        Assert.Contains(merged, d => d.DeviceId == "pcsc:nfc" && d is not CompositeYubiKey);
    }

    [Fact]
    public void Merge_SingleUsbInterfaceWithSerial_PassesThroughWithoutWrapper()
    {
        var single = new FakeYubiKey("pcsc:cc", ConnectionType.SmartCard);
        var merged = CompositeDeviceMerger.Merge([
            new DeviceInterfaceDescriptor(single, ConnectionType.SmartCard, IsUsb: true, 103, null)
        ]);

        var device = Assert.Single(merged);
        Assert.Same(single, device);
    }

    [Fact]
    public void Merge_CompositeFilteringOverMergedSet_MatchesByCapability()
    {
        var merged = CompositeDeviceMerger.Merge([
            Usb("pcsc:cc", ConnectionType.SmartCard, 103),
            Usb("hid:otp", ConnectionType.HidOtp, 103)
        ]);
        var available = Assert.Single(merged).AvailableConnections;

        Assert.True(ConnectionType.SmartCard.Matches(available));
        Assert.True(ConnectionType.HidOtp.Matches(available));
        Assert.True(ConnectionType.Hid.Matches(available));
        Assert.True(ConnectionType.All.Matches(available));
        Assert.False(ConnectionType.HidFido.Matches(available));
    }

    [Fact]
    public void Merge_Composite_CachesDiscoveryDeviceInfo()
    {
        var info = default(DeviceInfo) with
        {
            FirmwareVersion = new FirmwareVersion(5, 7, 2),
            SerialNumber = 103
        };

        var merged = CompositeDeviceMerger.Merge([
            Usb("pcsc:cc", ConnectionType.SmartCard, 103, info),
            Usb("hid:otp", ConnectionType.HidOtp, 103)
        ]);

        var composite = Assert.IsType<CompositeYubiKey>(Assert.Single(merged));
        Assert.NotNull(composite.DeviceInfo);
        Assert.Equal(103, composite.DeviceInfo!.Value.SerialNumber);
        Assert.Equal(new FirmwareVersion(5, 7, 2), composite.FirmwareVersion);
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