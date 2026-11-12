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
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.WebAuthn.Client;

namespace Yubico.YubiKit.WebAuthn.UnitTests;

/// <summary>
///     WebAuthn forwards its session creation options to the underlying CTAP2 session and adds no independent
///     transport selection in the inspected factory. These tests check selected connection types and
///     pre-connect validation errors. The fake throws at Connect, before native open or CTAP initialization.
/// </summary>
public class IYubiKeyExtensionsTransportTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static WebAuthnOrigin Origin
    {
        get
        {
            Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
            return origin;
        }
    }

    private static WebAuthnClientOptions ClientOptions => new() { RpIdValidator = (_, rpId) => rpId == "example.com" };

    [Fact]
    public async Task CreateWebAuthnClientAsync_NullClientOptions_ThrowsBeforeConnection()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => device.CreateWebAuthnClientAsync(Origin, clientOptions: null!, cancellationToken: Ct));

        Assert.Null(device.RequestedConnection);
    }

    [Fact]
    public async Task CreateWebAuthnClientAsync_UnconfiguredRpIdValidator_ThrowsBeforeConnection()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido);
        WebAuthnClientOptions clientOptions = Activator.CreateInstance<WebAuthnClientOptions>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => device.CreateWebAuthnClientAsync(Origin, clientOptions, cancellationToken: Ct));

        Assert.Contains("RpIdValidator must be configured", exception.Message, StringComparison.Ordinal);
        Assert.Null(device.RequestedConnection);
    }

    // With both transports exposed, this route requests HID FIDO, matching the CTAP2 default.
    [Fact]
    public async Task CreateWebAuthnClientAsync_DefaultBothTransports_PicksHidFido()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.SmartCard);

        await Assert.ThrowsAsync<ConnectProbeException>(
            () => device.CreateWebAuthnClientAsync(Origin, ClientOptions, cancellationToken: Ct));

        Assert.Equal(typeof(IFidoHidConnection), device.RequestedConnection);
    }

    // With HID FIDO absent, selection requests SmartCard; this is not retry after an opening failure.
    [Fact]
    public async Task CreateWebAuthnClientAsync_DefaultSmartCardOnly_FallsBackToSmartCard()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.SmartCard);

        await Assert.ThrowsAsync<ConnectProbeException>(
            () => device.CreateWebAuthnClientAsync(Origin, ClientOptions, cancellationToken: Ct));

        Assert.Equal(typeof(ISmartCardConnection), device.RequestedConnection);
    }

    // Explicit override is forwarded and honored.
    [Fact]
    public async Task CreateWebAuthnClientAsync_OverrideSmartCard_OnBothTransports_UsesSmartCard()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.SmartCard);

        await Assert.ThrowsAsync<ConnectProbeException>(
            () => device.CreateWebAuthnClientAsync(
                Origin,
                ClientOptions,
                sessionOptions: new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard },
                cancellationToken: Ct));

        Assert.Equal(typeof(ISmartCardConnection), device.RequestedConnection);
    }

    // Device-unsupported applet-valid override -> NotSupportedException, no connect attempt.
    [Fact]
    public async Task CreateWebAuthnClientAsync_OverrideSmartCard_NotOnDevice_ThrowsNotSupported()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido);

        await Assert.ThrowsAsync<NotSupportedException>(
            () => device.CreateWebAuthnClientAsync(
                Origin,
                ClientOptions,
                sessionOptions: new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard },
                cancellationToken: Ct));

        Assert.Null(device.RequestedConnection);
    }

    // SCP implies SmartCard when no explicit transport override is supplied because SCP is SmartCard-only.
    [Fact]
    public async Task CreateWebAuthnClientAsync_ScpWithDefault_RoutesToSmartCard()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.SmartCard);
        using var scp = Scp03KeyParameters.Default;

        await Assert.ThrowsAsync<ConnectProbeException>(
            () => device.CreateWebAuthnClientAsync(
                Origin,
                ClientOptions,
                sessionOptions: new SessionCreationOptions { ScpKeyParameters = scp },
                cancellationToken: Ct));

        Assert.Equal(typeof(ISmartCardConnection), device.RequestedConnection);
    }

    // Non-concrete override -> ArgumentException from the shared CTAP2 validation, no connect attempt.
    [Theory]
    [InlineData(ConnectionType.Hid)]
    [InlineData(ConnectionType.All)]
    [InlineData(ConnectionType.Unknown)]
    [InlineData(ConnectionType.HidFido | ConnectionType.SmartCard)]
    public async Task CreateWebAuthnClientAsync_NonConcreteOverride_ThrowsArgumentException(ConnectionType invalid)
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.SmartCard);

        await Assert.ThrowsAsync<ArgumentException>(
            () => device.CreateWebAuthnClientAsync(
                Origin,
                ClientOptions,
                sessionOptions: new SessionCreationOptions { PreferredConnectionType = invalid },
                cancellationToken: Ct));

        Assert.Null(device.RequestedConnection);
    }

    // Applet-invalid concrete override (HID OTP) -> ArgumentException even when the device exposes it.
    [Fact]
    public async Task CreateWebAuthnClientAsync_OverrideHidOtp_AppletInvalid_ThrowsArgumentException()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.HidOtp);

        await Assert.ThrowsAsync<ArgumentException>(
            () => device.CreateWebAuthnClientAsync(
                Origin,
                ClientOptions,
                sessionOptions: new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp },
                cancellationToken: Ct));

        Assert.Null(device.RequestedConnection);
    }

    // No FIDO-capable transport -> NotSupportedException, no connect attempt.
    [Theory]
    [InlineData(ConnectionType.HidOtp)]
    [InlineData(ConnectionType.Unknown)]
    public async Task CreateWebAuthnClientAsync_NoFidoCapableTransport_ThrowsNotSupported(ConnectionType available)
    {
        var device = new SelectionProbeYubiKey(available);

        await Assert.ThrowsAsync<NotSupportedException>(
            () => device.CreateWebAuthnClientAsync(Origin, ClientOptions, cancellationToken: Ct));

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