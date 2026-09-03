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
///     Stage D' device identity contract (docs/architecture/device-identity.md): the public
///     <see cref="IYubiKey.SerialNumber" /> nullability/lifetime clauses and the tri-state
///     <see cref="IYubiKey.SameDeviceAs" /> correlation truth table.
/// </summary>
public class DeviceIdentityContractTests
{
    // ---------------------------------------------------------------------------------------------
    // R2: SerialNumber contract on the published production device
    // ---------------------------------------------------------------------------------------------

    // CONTRACT PIN (R2): null until a metadata read has succeeded.
    [Fact]
    public void SerialNumber_BeforeAnyMetadataRead_IsNull()
    {
        IYubiKey device = Published(deviceInfo: null);

        Assert.Null(device.SerialNumber);
    }

    // CONTRACT PIN (R2): serial delivered with discovery metadata is exposed without a session.
    [Fact]
    public void SerialNumber_MetadataWithSerialArrives_IsExposed()
    {
        var device = Published(deviceInfo: null);

        device.DeviceInfo = WithSerial(103);

        Assert.Equal(103, ((IYubiKey)device).SerialNumber);
    }

    // CONTRACT PIN (R2): once non-null, the serial never reverts to null - even when a later
    // successful metadata read carries no serial (serial-less report, mid-reconfiguration read).
    [Fact]
    public void SerialNumber_LaterMetadataWithoutSerial_DoesNotRegressToNull()
    {
        var device = Published(deviceInfo: WithSerial(103));

        var serialLess = default(DeviceInfo) with { FirmwareVersion = new FirmwareVersion(5, 7, 2) };
        device.DeviceInfo = serialLess;

        Assert.Equal(103, ((IYubiKey)device).SerialNumber);
        Assert.Equal(serialLess, device.DeviceInfo); // the metadata snapshot itself does update
    }

    // CONTRACT PIN (R2): a null DeviceInfo update (transient read failure) changes nothing.
    [Fact]
    public void SerialNumber_TransientReadFailure_DoesNotRegress()
    {
        var device = Published(deviceInfo: WithSerial(103));

        device.DeviceInfo = null;

        Assert.Equal(103, ((IYubiKey)device).SerialNumber);
    }

    // CONTRACT PIN (R2): third-party IYubiKey implementations inherit a null default; the addition
    // is additive and non-breaking.
    [Fact]
    public void SerialNumber_ThirdPartyImplementation_DefaultsToNull()
    {
        // BareThirdPartyYubiKey declares only the abstract members; SerialNumber and SameDeviceAs
        // come entirely from the interface defaults, so this compiles against pre-existing code.
        IYubiKey device = new BareThirdPartyYubiKey("third:party");

        Assert.Null(device.SerialNumber);
        Assert.Equal(DeviceCorrelation.Same, device.SameDeviceAs(device));
    }

    // ---------------------------------------------------------------------------------------------
    // R2: SerialNumber lifetime clauses through the repository
    // ---------------------------------------------------------------------------------------------

    // CONTRACT PIN (R2): the serial may transition null -> non-null after publication without any
    // device event; the retained published object is updated in place.
    [Fact]
    public void UpdateCache_LateSerialArrival_PopulatesRetainedObjectWithoutEvents()
    {
        using var repository = new YubiKeyDeviceRepository();
        var firstScan = Published(deviceInfo: null);
        repository.UpdateCache([firstScan]);

        var events = new List<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events.Add);

        repository.UpdateCache([Published(deviceInfo: WithSerial(103))]);

        var retained = Assert.Single(repository.GetAll());
        Assert.Same(firstScan, retained);
        Assert.Equal(103, retained.SerialNumber);
        Assert.Empty(events);
    }

    // CONTRACT PIN (R2): a republished object never inherits identity from its predecessor object.
    // A connection-set change delivers a NEW object whose serial is whatever discovery established
    // for it - null here, because the replacement scan object carries no metadata.
    [Fact]
    public void UpdateCache_ConnectionSetChangeRepublication_NewObjectDoesNotInheritSerial()
    {
        using var repository = new YubiKeyDeviceRepository();
        var withSerial = new YubiKeyDevice(
            "ykphysical:103",
            new FakeSlot("pcsc:a", ConnectionType.SmartCard),
            new FakeSlot("hid-fido:a", ConnectionType.HidFido),
            hidOtp: null,
            WithSerial(103));
        repository.UpdateCache([withSerial]);

        var events = new List<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events.Add);

        // Same interface set, different connection set, no metadata on the fresh scan object.
        var republished = new YubiKeyDevice(
            "ykphysical:pid:0405",
            new FakeSlot("pcsc:a", ConnectionType.SmartCard),
            hidFido: null,
            new FakeSlot("hid-fido:a", ConnectionType.HidOtp),
            deviceInfo: null);
        repository.UpdateCache([republished]);

        Assert.Equal(2, events.Count);
        var added = Assert.Single(repository.GetAll());
        Assert.Same(republished, added);
        Assert.Null(added.SerialNumber);
    }

    // CONTRACT PIN (R2): the object delivered with a removal event retains its last-known serial.
    [Fact]
    public void UpdateCache_RemovalEvent_ObjectRetainsLastKnownSerial()
    {
        using var repository = new YubiKeyDeviceRepository();
        var firstScan = Published(deviceInfo: null);
        repository.UpdateCache([firstScan]);
        repository.UpdateCache([Published(deviceInfo: WithSerial(103))]); // late serial arrival

        var events = new List<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events.Add);

        repository.UpdateCache([]);

        var removal = Assert.Single(events);
        Assert.Equal(DeviceAction.Removed, removal.Action);
        Assert.Same(firstScan, removal.Device);
        Assert.Equal(103, removal.Device.SerialNumber);
    }

    // ---------------------------------------------------------------------------------------------
    // R3: SameDeviceAs tri-state truth table
    // ---------------------------------------------------------------------------------------------

    // CONTRACT PIN (R3): the same object is always Same, even when its serial is unknown.
    [Fact]
    public void SameDeviceAs_SameReferenceWithUnknownSerial_Same()
    {
        IYubiKey device = Published(deviceInfo: null);

        Assert.Equal(DeviceCorrelation.Same, device.SameDeviceAs(device));
    }

    // CONTRACT PIN (R3): known, equal serials are Same across distinct objects.
    [Fact]
    public void SameDeviceAs_EqualKnownSerials_Same()
    {
        IYubiKey first = Published(deviceInfo: WithSerial(103));
        IYubiKey second = Published(deviceInfo: WithSerial(103));

        Assert.NotSame(first, second);
        Assert.Equal(DeviceCorrelation.Same, first.SameDeviceAs(second));
        Assert.Equal(DeviceCorrelation.Same, second.SameDeviceAs(first));
    }

    // CONTRACT PIN (R3): known, unequal serials are Different.
    [Fact]
    public void SameDeviceAs_UnequalKnownSerials_Different()
    {
        IYubiKey first = Published(deviceInfo: WithSerial(103));
        IYubiKey second = Published(deviceInfo: WithSerial(125));

        Assert.Equal(DeviceCorrelation.Different, first.SameDeviceAs(second));
        Assert.Equal(DeviceCorrelation.Different, second.SameDeviceAs(first));
    }

    // CONTRACT PIN (R3): an unknown serial on EITHER side yields Unknown - never a guess.
    [Fact]
    public void SameDeviceAs_OwnSerialUnknown_Unknown()
    {
        IYubiKey unknown = Published(deviceInfo: null);
        IYubiKey known = Published(deviceInfo: WithSerial(103));

        Assert.Equal(DeviceCorrelation.Unknown, unknown.SameDeviceAs(known));
    }

    [Fact]
    public void SameDeviceAs_OtherSerialUnknown_Unknown()
    {
        IYubiKey known = Published(deviceInfo: WithSerial(103));
        IYubiKey unknown = Published(deviceInfo: null);

        Assert.Equal(DeviceCorrelation.Unknown, known.SameDeviceAs(unknown));
    }

    [Fact]
    public void SameDeviceAs_BothSerialsUnknown_Unknown()
    {
        IYubiKey first = Published(deviceInfo: null);
        IYubiKey second = Published(deviceInfo: null);

        Assert.Equal(DeviceCorrelation.Unknown, first.SameDeviceAs(second));
    }

    // CONTRACT PIN (R3): the default interface implementation gives third-party devices the same
    // truth table, and it interoperates with production devices.
    [Fact]
    public void SameDeviceAs_ThirdPartyDefaultImplementation_UsesSerialContract()
    {
        IYubiKey thirdPartyWithSerial = new MinimalThirdPartyYubiKey("third:a", serialNumber: 103);
        IYubiKey thirdPartySerialLess = new MinimalThirdPartyYubiKey("third:b");
        IYubiKey production = Published(deviceInfo: WithSerial(103));

        Assert.Equal(DeviceCorrelation.Same, thirdPartyWithSerial.SameDeviceAs(production));
        Assert.Equal(DeviceCorrelation.Same, production.SameDeviceAs(thirdPartyWithSerial));
        Assert.Equal(DeviceCorrelation.Unknown, thirdPartySerialLess.SameDeviceAs(production));
        Assert.Equal(
            DeviceCorrelation.Same,
            thirdPartySerialLess.SameDeviceAs(thirdPartySerialLess));
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private static DeviceInfo WithSerial(int serial) =>
        default(DeviceInfo) with
        {
            FirmwareVersion = new FirmwareVersion(5, 7, 2),
            SerialNumber = serial
        };

    private static YubiKeyDevice Published(DeviceInfo? deviceInfo) =>
        new(
            "ykphysical:pid:0407",
            new FakeSlot("pcsc:a", ConnectionType.SmartCard),
            new FakeSlot("hid-fido:a", ConnectionType.HidFido),
            hidOtp: null,
            deviceInfo);

    private sealed class FakeSlot(string interfaceId, ConnectionType connectionType) : IYubiKeyConnectionSlot
    {
        public string InterfaceId { get; } = interfaceId;
        public ConnectionType ConnectionType { get; } = connectionType;
    }

    /// <summary>
    ///     An external implementation declaring only the abstract members - the compile-time proof
    ///     that the stage D' additions are non-breaking for existing third-party implementers.
    /// </summary>
    private sealed class BareThirdPartyYubiKey(string deviceId) : IYubiKey
    {
        public string DeviceId { get; } = deviceId;
        public ConnectionType AvailableConnections => ConnectionType.SmartCard;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            throw new NotSupportedException();
    }

    /// <summary>
    ///     A deliberately minimal external implementation: only the abstract members, plus an optional
    ///     serial override to exercise the default <see cref="IYubiKey.SameDeviceAs" /> semantics.
    /// </summary>
    private sealed class MinimalThirdPartyYubiKey(string deviceId, int? serialNumber = null) : IYubiKey
    {
        public string DeviceId { get; } = deviceId;
        public ConnectionType AvailableConnections => ConnectionType.SmartCard;
        public int? SerialNumber { get; } = serialNumber;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            throw new NotSupportedException();
    }
}