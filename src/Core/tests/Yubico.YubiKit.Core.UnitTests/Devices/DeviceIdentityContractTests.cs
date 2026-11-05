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

/// <summary>
///     Verifies the public <see cref="IYubiKey.SerialNumber" /> lifetime rules and the tri-state
///     <see cref="IYubiKey.SameDeviceAs" /> truth table.
/// </summary>
public class DeviceIdentityContractTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(30);

    // ---------------------------------------------------------------------------------------------
    // SerialNumber behavior on a published production device
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SerialNumber_BeforeAnyMetadataRead_IsNull()
    {
        IYubiKey device = Published(deviceInfo: null);

        Assert.Null(device.SerialNumber);
    }

    [Fact]
    public void SerialNumber_MetadataWithSerialArrives_IsExposed()
    {
        var device = Published(deviceInfo: null);

        device.DeviceInfo = WithSerial(103);

        Assert.Equal(103, ((IYubiKey)device).SerialNumber);
    }

    // A later metadata snapshot without a serial must not erase an established identity.
    [Fact]
    public void SerialNumber_LaterMetadataWithoutSerial_DoesNotRegressToNull()
    {
        var device = Published(deviceInfo: WithSerial(103));

        var serialLess = default(DeviceInfo) with { FirmwareVersion = new FirmwareVersion(5, 7, 2) };
        device.DeviceInfo = serialLess;

        Assert.Equal(103, ((IYubiKey)device).SerialNumber);
        Assert.Equal(serialLess, device.DeviceInfo); // the metadata snapshot itself does update
    }

    [Fact]
    public void DeviceInfo_NullAssignment_DoesNotChangeMetadataOrSerial()
    {
        var original = WithSerial(103);
        var device = Published(deviceInfo: original);

        device.DeviceInfo = null;

        Assert.Equal(original, device.DeviceInfo);
        Assert.Equal(103, ((IYubiKey)device).SerialNumber);
    }

    // Existing third-party implementations inherit the default interface behavior.
    [Fact]
    public void SerialNumber_ThirdPartyImplementation_DefaultsToNull()
    {
        IYubiKey device = new BareThirdPartyYubiKey("third:party");

        Assert.Null(device.SerialNumber);
        Assert.Equal(DeviceCorrelation.Same, device.SameDeviceAs(device));
    }

    // ---------------------------------------------------------------------------------------------
    // SerialNumber lifetime through the repository
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task UpdateCache_LateSerialArrival_PopulatesRetainedObjectWithoutEvents()
    {
        using var repository = new YubiKeyDeviceRepository();
        using var cts = new CancellationTokenSource(Bound);
        var firstScan = Published(deviceInfo: null);
        repository.UpdateCache([firstScan]);

        await using var watcher = await DeviceEventWatcher.StartAsync(repository, cts.Token);

        repository.UpdateCache([Published(deviceInfo: WithSerial(103))]);

        // Snapshot the cache before draining: the drain sentinel is itself a cache entry.
        var cached = repository.GetAll();
        var events = await watcher.DrainAsync(repository, cts.Token);

        var retained = Assert.Single(cached);
        Assert.Same(firstScan, retained);
        Assert.Equal(103, retained.SerialNumber);
        Assert.Empty(events);
    }

    // The replacement has only the metadata established for that scan.
    [Fact]
    public async Task UpdateCache_ConnectionSetChangeRepublication_NewObjectDoesNotInheritSerial()
    {
        using var repository = new YubiKeyDeviceRepository();
        using var cts = new CancellationTokenSource(Bound);
        var withSerial = new YubiKeyDevice(
            "ykphysical:103",
            new FakeSlot("pcsc:a", ConnectionType.SmartCard),
            new FakeSlot("hid-fido:a", ConnectionType.HidFido),
            hidOtp: null,
            WithSerial(103));
        repository.UpdateCache([withSerial]);

        await using var watcher = await DeviceEventWatcher.StartAsync(repository, cts.Token);

        // Same interface set, different connection set, no metadata on the fresh scan object.
        var republished = new YubiKeyDevice(
            "ykphysical:pid:0405",
            new FakeSlot("pcsc:a", ConnectionType.SmartCard),
            hidFido: null,
            new FakeSlot("hid-fido:a", ConnectionType.HidOtp),
            deviceInfo: null);
        repository.UpdateCache([republished]);

        var cached = repository.GetAll();
        var events = await watcher.DrainAsync(repository, cts.Token);

        Assert.Equal(2, events.Count);
        var added = Assert.Single(cached);
        Assert.Same(republished, added);
        Assert.Null(added.SerialNumber);
    }

    [Fact]
    public async Task UpdateCache_RemovalEvent_ObjectRetainsLastKnownSerial()
    {
        using var repository = new YubiKeyDeviceRepository();
        using var cts = new CancellationTokenSource(Bound);
        var firstScan = Published(deviceInfo: null);
        repository.UpdateCache([firstScan]);
        repository.UpdateCache([Published(deviceInfo: WithSerial(103))]); // late serial arrival

        await using var watcher = await DeviceEventWatcher.StartAsync(repository, cts.Token);

        repository.UpdateCache([]);

        var events = await watcher.DrainAsync(repository, cts.Token);

        var removal = Assert.Single(events);
        Assert.Equal(DeviceAction.Removed, removal.Action);
        Assert.Same(firstScan, removal.Device);
        Assert.Equal(103, removal.Device.SerialNumber);
    }

    // ---------------------------------------------------------------------------------------------
    // SameDeviceAs tri-state truth table
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SameDeviceAs_SameReferenceWithUnknownSerial_Same()
    {
        IYubiKey device = Published(deviceInfo: null);

        Assert.Equal(DeviceCorrelation.Same, device.SameDeviceAs(device));
    }

    [Fact]
    public void SameDeviceAs_EqualKnownSerials_Same()
    {
        IYubiKey first = Published(deviceInfo: WithSerial(103));
        IYubiKey second = Published(deviceInfo: WithSerial(103));

        Assert.NotSame(first, second);
        Assert.Equal(DeviceCorrelation.Same, first.SameDeviceAs(second));
        Assert.Equal(DeviceCorrelation.Same, second.SameDeviceAs(first));
    }

    [Fact]
    public void SameDeviceAs_UnequalKnownSerials_Different()
    {
        IYubiKey first = Published(deviceInfo: WithSerial(103));
        IYubiKey second = Published(deviceInfo: WithSerial(125));

        Assert.Equal(DeviceCorrelation.Different, first.SameDeviceAs(second));
        Assert.Equal(DeviceCorrelation.Different, second.SameDeviceAs(first));
    }

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
    ///     An external implementation declaring only the abstract interface members.
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