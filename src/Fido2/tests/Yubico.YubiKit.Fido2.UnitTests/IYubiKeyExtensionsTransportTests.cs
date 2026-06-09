using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Fido2.UnitTests;

/// <summary>
///     Phase 36 mechanical migration of the FIDO2 transport selection off the removed scalar
///     <c>IYubiKey.ConnectionType</c>. Locks single-interface parity and the preference-free
///     dual-transport behavior (explicit selection is Phase 38).
/// </summary>
public class IYubiKeyExtensionsTransportTests
{
    [Fact]
    public async Task CreateFidoSessionAsync_SingleHidFido_RoutesToFidoTransport()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido);

        await Assert.ThrowsAsync<ConnectProbeException>(
            () => device.CreateFidoSessionAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(typeof(IFidoHidConnection), device.RequestedConnection);
    }

    [Fact]
    public async Task CreateFidoSessionAsync_SingleSmartCard_RoutesToSmartCardTransport()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.SmartCard);

        await Assert.ThrowsAsync<ConnectProbeException>(
            () => device.CreateFidoSessionAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(typeof(ISmartCardConnection), device.RequestedConnection);
    }

    [Fact]
    public async Task CreateFidoSessionAsync_BothFidoTransports_ThrowsWithoutPreference()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.SmartCard);

        var ex = await Assert.ThrowsAsync<NotSupportedException>(
            () => device.CreateFidoSessionAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("Phase 38", ex.Message, StringComparison.OrdinalIgnoreCase);
        // No transport was opened: the ambiguity is rejected rather than silently preferred.
        Assert.Null(device.RequestedConnection);
    }

    [Fact]
    public async Task CreateFidoSessionAsync_NoFidoCapableTransport_Throws()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidOtp);

        var ex = await Assert.ThrowsAsync<NotSupportedException>(
            () => device.CreateFidoSessionAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("FIDO-capable", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(device.RequestedConnection);
    }

    private sealed class SelectionProbeYubiKey(ConnectionType available) : IYubiKey
    {
        public string DeviceId => "probe";
        public ConnectionType AvailableConnections { get; } = available;
        public Type? RequestedConnection { get; private set; }

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            RequestedConnection = typeof(TConnection);
            throw new ConnectProbeException();
        }
    }

    private sealed class ConnectProbeException : Exception;
}
