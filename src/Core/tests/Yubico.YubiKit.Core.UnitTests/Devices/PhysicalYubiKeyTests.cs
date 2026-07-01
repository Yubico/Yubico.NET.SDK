using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class PhysicalYubiKeyTests
{
    [Theory]
    [InlineData(ConnectionType.SmartCard, ConnectionType.SmartCard, true)]
    [InlineData(ConnectionType.SmartCard, ConnectionType.HidFido, false)]
    [InlineData(ConnectionType.HidFido | ConnectionType.SmartCard, ConnectionType.HidFido, true)]
    [InlineData(ConnectionType.HidFido | ConnectionType.SmartCard, ConnectionType.HidOtp, false)]
    [InlineData(ConnectionType.HidFido, ConnectionType.Hid, true)]
    [InlineData(ConnectionType.SmartCard, ConnectionType.Hid, false)]
    [InlineData(ConnectionType.SmartCard, ConnectionType.Unknown, false)]
    [InlineData(ConnectionType.SmartCard, ConnectionType.All, false)]
    [InlineData(ConnectionType.HidFido | ConnectionType.SmartCard, ConnectionType.HidFido | ConnectionType.SmartCard, false)]
    public void SupportsConnection_DefinedSemantics(ConnectionType available, ConnectionType requested, bool expected)
    {
        IYubiKey device = new FakePhysicalYubiKey(available);

        Assert.Equal(expected, device.SupportsConnection(requested));
    }

    [Theory]
    [InlineData(ConnectionType.SmartCard, ConnectionType.HidFido | ConnectionType.SmartCard, true)]
    [InlineData(ConnectionType.Hid, ConnectionType.HidOtp, true)]
    [InlineData(ConnectionType.Hid, ConnectionType.SmartCard, false)]
    [InlineData(ConnectionType.All, ConnectionType.SmartCard, true)]
    [InlineData(ConnectionType.All, ConnectionType.HidFido | ConnectionType.HidOtp, true)]
    [InlineData(ConnectionType.HidOtp, ConnectionType.SmartCard, false)]
    [InlineData(ConnectionType.Unknown, ConnectionType.SmartCard, false)]
    [InlineData(ConnectionType.SmartCard, ConnectionType.Unknown, false)]
    public void Matches_ComparesFilterAgainstCapabilitySet(ConnectionType filter, ConnectionType available, bool expected)
    {
        Assert.Equal(expected, filter.Matches(available));
    }

    [Fact]
    public async Task ConnectAsync_Default_SingleConnection_Resolves()
    {
        IYubiKey device = new FakePhysicalYubiKey(ConnectionType.SmartCard);

        await using var connection = await device.ConnectAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ConnectionType.SmartCard, connection.Type);
    }

    [Fact]
    public async Task ConnectAsync_Default_MultipleConnections_ThrowsInvalidOperation()
    {
        IYubiKey device = new FakePhysicalYubiKey(ConnectionType.SmartCard | ConnectionType.HidFido);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => device.ConnectAsync(TestContext.Current.CancellationToken));

        Assert.Contains("ambiguous", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConnectAsync_Default_NoConnections_ThrowsNotSupported()
    {
        IYubiKey device = new FakePhysicalYubiKey(ConnectionType.Unknown);

        await Assert.ThrowsAsync<NotSupportedException>(
            () => device.ConnectAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ConnectAsync_Typed_Supported_ReturnsConnection()
    {
        IYubiKey device = new FakePhysicalYubiKey(ConnectionType.HidFido);

        await using var connection =
            await device.ConnectAsync<IFidoHidConnection>(TestContext.Current.CancellationToken);

        Assert.Equal(ConnectionType.HidFido, connection.Type);
    }

    [Fact]
    public async Task ConnectAsync_Typed_Unsupported_ThrowsNotSupported()
    {
        IYubiKey device = new FakePhysicalYubiKey(ConnectionType.HidFido);

        await Assert.ThrowsAsync<NotSupportedException>(
            () => device.ConnectAsync<ISmartCardConnection>(TestContext.Current.CancellationToken));
    }

    private sealed class FakePhysicalYubiKey(ConnectionType available) : IYubiKey
    {
        public string DeviceId => "fake";
        public ConnectionType AvailableConnections { get; } = available;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            var requested =
                typeof(TConnection) == typeof(ISmartCardConnection) ? ConnectionType.SmartCard :
                typeof(TConnection) == typeof(IFidoHidConnection) ? ConnectionType.HidFido :
                typeof(TConnection) == typeof(IOtpHidConnection) ? ConnectionType.HidOtp :
                ConnectionType.Unknown;

            if (requested == ConnectionType.Unknown || !AvailableConnections.SupportsConnection(requested))
                throw new NotSupportedException($"Connection type {typeof(TConnection).Name} is not supported.");

            return Task.FromResult((TConnection)(IConnection)new FakeConnection(requested));
        }
    }

    private sealed class FakeConnection(ConnectionType type)
        : ISmartCardConnection, IFidoHidConnection, IOtpHidConnection
    {
        public ConnectionType Type => type;
        public Transport Transport => Transport.Usb;
        public int PacketSize => 64;
        public int FeatureReportSize => 8;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool SupportsExtendedApdu() => false;

        public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ReadOnlyMemory<byte>.Empty);

        public void Dispose() { }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}