// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class YubiKeyDeviceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void FlatSlots_ExposeCombinedConnectionsAndSortedInterfaceIds()
    {
        var device = FullKey(out var smartCard, out var fido, out var otp);

        Assert.Equal(
            ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp,
            device.AvailableConnections);
        Assert.Equal(
            new[] { smartCard.DeviceId, fido.DeviceId, otp.DeviceId }.Order(StringComparer.Ordinal),
            device.InterfaceIds);
    }
    [Fact]
    public void Constructor_WithNoSlots_RejectsUnopenablePublishedDevice()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new YubiKeyDevice("ykphysical:empty", null, null, null, deviceInfo: null));

        Assert.Contains("at least one connection slot", exception.Message, StringComparison.Ordinal);
        Assert.Null(exception.ParamName);
    }

    [Fact]
    public void Constructor_WithWrongSlotShape_RejectsMismatchedConnection()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new YubiKeyDevice(
                "ykphysical:wrong-slot",
                smartCard: null,
                hidFido: new RecordingSlot(ConnectionType.SmartCard | ConnectionType.HidFido),
                hidOtp: null,
                deviceInfo: null));

        Assert.Contains("HidFido slot", exception.Message, StringComparison.Ordinal);
        Assert.Equal("hidFido", exception.ParamName);
    }

    [Fact]
    public async Task SmartCardRouting_NormalDiscoveryAndRegistrySelectTheSameSlot()
    {
        var device = FullKey(out var smartCard, out var fido, out var otp);

        Assert.Equal(smartCard.DeviceId, DeviceConnectionRegistry.ResolveInterfaceId(device, ConnectionType.SmartCard));

        await using (var connection = await device.ConnectAsync<ISmartCardConnection>(Ct))
            Assert.Equal(ConnectionType.SmartCard, connection.Type);

        await using (var connection = await ((IDiscoveryConnectionProvider)device)
                         .ConnectForDiscoveryAsync(ConnectionType.SmartCard, Ct))
            Assert.Equal(ConnectionType.SmartCard, connection.Type);

        Assert.Equal(2, smartCard.ConnectCalls);
        Assert.Equal(0, fido.ConnectCalls);
        Assert.Equal(0, otp.ConnectCalls);
    }

    [Fact]
    public async Task FidoRouting_NormalDiscoveryAndRegistrySelectTheSameSlot()
    {
        var device = FullKey(out var smartCard, out var fido, out var otp);

        Assert.Equal(fido.DeviceId, DeviceConnectionRegistry.ResolveInterfaceId(device, ConnectionType.HidFido));

        await using (var connection = await device.ConnectAsync<IFidoHidConnection>(Ct))
            Assert.Equal(ConnectionType.HidFido, connection.Type);

        await using (var connection = await ((IDiscoveryConnectionProvider)device)
                         .ConnectForDiscoveryAsync(ConnectionType.HidFido, Ct))
            Assert.Equal(ConnectionType.HidFido, connection.Type);

        Assert.Equal(0, smartCard.ConnectCalls);
        Assert.Equal(2, fido.ConnectCalls);
        Assert.Equal(0, otp.ConnectCalls);
    }

    [Fact]
    public async Task OtpRouting_NormalDiscoveryAndRegistrySelectTheSameSlot()
    {
        var device = FullKey(out var smartCard, out var fido, out var otp);

        Assert.Equal(otp.DeviceId, DeviceConnectionRegistry.ResolveInterfaceId(device, ConnectionType.HidOtp));

        await using (var connection = await device.ConnectAsync<IOtpHidConnection>(Ct))
            Assert.Equal(ConnectionType.HidOtp, connection.Type);

        await using (var connection = await ((IDiscoveryConnectionProvider)device)
                         .ConnectForDiscoveryAsync(ConnectionType.HidOtp, Ct))
            Assert.Equal(ConnectionType.HidOtp, connection.Type);

        Assert.Equal(0, smartCard.ConnectCalls);
        Assert.Equal(0, fido.ConnectCalls);
        Assert.Equal(2, otp.ConnectCalls);
    }

    [Fact]
    public void HidGroupRouting_RegistrySelectsFidoBeforeOtp()
    {
        var device = FullKey(out _, out var fido, out _);

        Assert.Equal(fido.DeviceId, DeviceConnectionRegistry.ResolveInterfaceId(device, ConnectionType.Hid));
    }

    [Fact]
    public void PhysicalIdentityKeyFor_ThirdPartyImplementationFallsBackToDeviceId()
    {
        IYubiKey thirdParty = new ThirdPartyYubiKey("third-party:1");

        Assert.Equal("13:third-party:1", YubiKeyDevice.PhysicalIdentityKeyFor(thirdParty));
    }

    [Fact]
    public async Task ConnectAsync_UnsupportedGenericConnection_ThrowsNotSupported()
    {
        var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            FullKey(out _, out _, out _).ConnectAsync<IConnection>(Ct));

        Assert.Contains(nameof(IConnection), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConnectAsync_RequestedConnectionAbsent_ThrowsNotSupported()
    {
        var device = new YubiKeyDevice(
            "ykphysical:smart-card-only",
            new RecordingSlot(ConnectionType.SmartCard),
            hidFido: null,
            hidOtp: null,
            deviceInfo: null);

        var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            device.ConnectAsync<IOtpHidConnection>(Ct));

        Assert.Contains(nameof(ConnectionType.HidOtp), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ConnectionType.SmartCard), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConnectAsync_SlotCannotRawOpen_PropagatesOrdinaryNotSupported()
    {
        var expected = new NotSupportedException("The slot cannot open a raw connection.");
        var device = new YubiKeyDevice(
            "ykphysical:unsupported-raw",
            new UnsupportedRawSlot(expected),
            hidFido: null,
            hidOtp: null,
            deviceInfo: null);

        var actual = await Assert.ThrowsAsync<NotSupportedException>(() =>
            device.ConnectAsync<ISmartCardConnection>(Ct));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task DiscoveryConnectAsync_GenuineRawOpenNotSupported_PropagatesUnchanged()
    {
        var expected = new NotSupportedException("The native smart-card connection rejected this raw open.");
        var device = new YubiKeyDevice(
            "ykphysical:genuine-unsupported-raw",
            new UnsupportedRawSlot(expected),
            hidFido: null,
            hidOtp: null,
            deviceInfo: null);

        var actual = await Assert.ThrowsAsync<NotSupportedException>(() =>
            ((IDiscoveryConnectionProvider)device)
            .ConnectForDiscoveryAsync(ConnectionType.SmartCard, Ct));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task DefaultConnectAsync_MultiSlotDevice_ThrowsAmbiguous()
    {
        IYubiKey device = FullKey(out _, out _, out _);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => device.ConnectAsync(Ct));

        Assert.Contains("ambiguous", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeviceInfo_NullUpdate_DoesNotClearSuccessfulSnapshot()
    {
        var device = FullKey(out _, out _, out _);
        var metadata = default(DeviceInfo) with
        {
            FirmwareVersion = new FirmwareVersion(5, 7, 2),
            SerialNumber = 103
        };

        device.DeviceInfo = metadata;
        device.DeviceInfo = null;

        Assert.Equal(metadata, device.DeviceInfo);
    }

    private static YubiKeyDevice FullKey(
        out RecordingSlot smartCard,
        out RecordingSlot fido,
        out RecordingSlot otp)
    {
        smartCard = new RecordingSlot(ConnectionType.SmartCard);
        fido = new RecordingSlot(ConnectionType.HidFido);
        otp = new RecordingSlot(ConnectionType.HidOtp);
        return new YubiKeyDevice("ykphysical:103", smartCard, fido, otp, deviceInfo: null);
    }

    private sealed class RecordingSlot(ConnectionType connection) : IYubiKeyConnectionSlot
    {
        private readonly string _deviceId = $"member:{connection}:{Guid.NewGuid():N}";

        public int ConnectCalls { get; private set; }

        public string DeviceId => _deviceId;

        public ConnectionType AvailableConnections => connection;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            throw new InvalidOperationException("Published-device routing must use the raw slot connection path.");

        public Task<IConnection> OpenRawConnectionAsync(
            ConnectionType requested,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConnectCalls++;
            IConnection result = requested switch
            {
                ConnectionType.SmartCard when connection == ConnectionType.SmartCard => new FakeSmartCardConnection(),
                ConnectionType.HidFido when connection == ConnectionType.HidFido => new FakeFidoConnection(),
                ConnectionType.HidOtp when connection == ConnectionType.HidOtp => new FakeOtpConnection(),
                _ => throw new NotSupportedException()
            };
            return Task.FromResult(result);
        }
    }

    private sealed class ThirdPartyYubiKey(string deviceId) : IYubiKey
    {
        public string DeviceId => deviceId;

        public ConnectionType AvailableConnections => ConnectionType.SmartCard;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection => throw new NotSupportedException();
    }

    private sealed class UnsupportedRawSlot(NotSupportedException exception) : IYubiKeyConnectionSlot
    {
        public string DeviceId => "member:unsupported-raw";

        public ConnectionType AvailableConnections => ConnectionType.SmartCard;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            Task.FromException<TConnection>(new InvalidOperationException("Published routing must not use slot ConnectAsync."));

        public Task<IConnection> OpenRawConnectionAsync(
            ConnectionType connection,
            CancellationToken cancellationToken = default) =>
            Task.FromException<IConnection>(exception);
    }

    private sealed class FakeSmartCardConnection : ISmartCardConnection
    {
        public ConnectionType Type => ConnectionType.SmartCard;

        public Transport Transport => Transport.Usb;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default) => Task.FromResult(ReadOnlyMemory<byte>.Empty);

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool SupportsExtendedApdu() => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeFidoConnection : IFidoHidConnection
    {
        public ConnectionType Type => ConnectionType.HidFido;

        public int PacketSize => 64;

        public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ReadOnlyMemory<byte>.Empty);

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeOtpConnection : IOtpHidConnection
    {
        public ConnectionType Type => ConnectionType.HidOtp;

        public int FeatureReportSize => 8;

        public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ReadOnlyMemory<byte>.Empty);

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}