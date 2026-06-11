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

using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Fido2.UnitTests;

/// <summary>
///     Phase 38: FIDO2 transport selection — app-specific smart default (HID FIDO first, then SmartCard)
///     plus explicit caller override, validated over a fake probe that records the requested connection type.
/// </summary>
public class IYubiKeyExtensionsTransportTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // (a) default top-choice: both transports present -> HID FIDO.
    [Fact]
    public async Task CreateFidoSessionAsync_DefaultBothTransports_PicksHidFido()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.SmartCard);

        await Assert.ThrowsAsync<ConnectProbeException>(() => device.CreateFidoSessionAsync(cancellationToken: Ct));

        Assert.Equal(typeof(IFidoHidConnection), device.RequestedConnection);
    }

    // (b) default fallback: first choice (HID FIDO) absent -> SmartCard.
    [Fact]
    public async Task CreateFidoSessionAsync_DefaultSmartCardOnly_FallsBackToSmartCard()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.SmartCard);

        await Assert.ThrowsAsync<ConnectProbeException>(() => device.CreateFidoSessionAsync(cancellationToken: Ct));

        Assert.Equal(typeof(ISmartCardConnection), device.RequestedConnection);
    }

    [Fact]
    public async Task CreateFidoSessionAsync_SingleHidFido_RoutesToFidoTransport()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido);

        await Assert.ThrowsAsync<ConnectProbeException>(() => device.CreateFidoSessionAsync(cancellationToken: Ct));

        Assert.Equal(typeof(IFidoHidConnection), device.RequestedConnection);
    }

    // (c) explicit non-default override on a multi-transport device beats the default order.
    [Fact]
    public async Task CreateFidoSessionAsync_OverrideSmartCard_OnBothTransports_UsesSmartCard()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.SmartCard);

        await Assert.ThrowsAsync<ConnectProbeException>(
            () => device.CreateFidoSessionAsync(preferredConnection: ConnectionType.SmartCard, cancellationToken: Ct));

        Assert.Equal(typeof(ISmartCardConnection), device.RequestedConnection);
    }

    // (d) device-unsupported but applet-valid override -> NotSupportedException, no connect attempt.
    [Fact]
    public async Task CreateFidoSessionAsync_OverrideSmartCard_NotOnDevice_ThrowsNotSupported()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido);

        await Assert.ThrowsAsync<NotSupportedException>(
            () => device.CreateFidoSessionAsync(preferredConnection: ConnectionType.SmartCard, cancellationToken: Ct));

        Assert.Null(device.RequestedConnection);
    }

    // (e) non-concrete override values -> ArgumentException, no connect attempt.
    [Theory]
    [InlineData(ConnectionType.Hid)]
    [InlineData(ConnectionType.All)]
    [InlineData(ConnectionType.Unknown)]
    [InlineData(ConnectionType.HidFido | ConnectionType.SmartCard)]
    public async Task CreateFidoSessionAsync_NonConcreteOverride_ThrowsArgumentException(ConnectionType invalid)
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.SmartCard);

        await Assert.ThrowsAsync<ArgumentException>(
            () => device.CreateFidoSessionAsync(preferredConnection: invalid, cancellationToken: Ct));

        Assert.Null(device.RequestedConnection);
    }

    // (f) applet-invalid concrete override (HID OTP) -> ArgumentException even when the device exposes it.
    [Fact]
    public async Task CreateFidoSessionAsync_OverrideHidOtp_AppletInvalid_ThrowsArgumentException()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.HidOtp);

        await Assert.ThrowsAsync<ArgumentException>(
            () => device.CreateFidoSessionAsync(preferredConnection: ConnectionType.HidOtp, cancellationToken: Ct));

        Assert.Null(device.RequestedConnection);
    }

    // (h) no FIDO-capable transport -> NotSupportedException, no connect attempt.
    [Theory]
    [InlineData(ConnectionType.HidOtp)]
    [InlineData(ConnectionType.Unknown)]
    public async Task CreateFidoSessionAsync_NoFidoCapableTransport_ThrowsNotSupported(ConnectionType available)
    {
        var device = new SelectionProbeYubiKey(available);

        await Assert.ThrowsAsync<NotSupportedException>(() => device.CreateFidoSessionAsync(cancellationToken: Ct));

        Assert.Null(device.RequestedConnection);
    }

    // (i) SCP does not change transport SELECTION: scpKeyParams + default still routes to HID FIDO (deferred
    // "SCP implies SmartCard" behavior is NOT implemented); SmartCard is reached only via explicit override.
    // (Past selection, SCP over a non-SmartCard transport throws NotSupportedException in
    // ApplicationSession.InitializeCoreAsync — a pre-existing Core contract, not Phase 38 behavior.)
    [Fact]
    public async Task CreateFidoSessionAsync_ScpWithDefault_StillRoutesToHidFido()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.SmartCard);
        using var scp = Scp03KeyParameters.Default;

        await Assert.ThrowsAsync<ConnectProbeException>(
            () => device.CreateFidoSessionAsync(scpKeyParams: scp, cancellationToken: Ct));

        Assert.Equal(typeof(IFidoHidConnection), device.RequestedConnection);
    }

    // Phase 38.5 (ISC-10/ISC-14): the FIDO2 "no FIDO-capable connection" remap stays scoped to the
    // ResolveSessionTransports call only. When ResolveSessionTransports succeeds but the HID FIDO connect
    // fails with a non-held error, that error must surface unchanged — NOT be masked as the generic
    // NotSupportedException — proving the remap did not widen around ConnectSessionTransportAsync.
    [Fact]
    public async Task CreateFidoSessionAsync_ConnectFailsNonHeld_SurfacesErrorNotGenericRemap()
    {
        var device = new ThrowingProbeYubiKey(
            ConnectionType.HidFido | ConnectionType.SmartCard,
            new InvalidOperationException("transport boom"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => device.CreateFidoSessionAsync(cancellationToken: Ct));

        Assert.Equal("transport boom", ex.Message);
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

    // Resolves a candidate list successfully, then throws a caller-supplied exception from the connect itself.
    private sealed class ThrowingProbeYubiKey(ConnectionType available, Exception connectException) : IYubiKey
    {
        public string DeviceId => "throwing-probe";
        public ConnectionType AvailableConnections { get; } = available;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            throw connectException;
    }

    private sealed class ConnectProbeException : Exception;
}