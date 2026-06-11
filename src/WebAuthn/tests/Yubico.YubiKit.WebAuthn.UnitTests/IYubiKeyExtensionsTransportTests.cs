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
using Yubico.YubiKit.WebAuthn.Client;

namespace Yubico.YubiKit.WebAuthn.UnitTests;

/// <summary>
///     Phase 38: WebAuthn forwards its <c>preferredConnection</c> override to the underlying FIDO2 session and
///     adds no independent transport logic. These tests prove the pass-through: WebAuthn sees the FIDO2 default
///     (HID FIDO first), honors an explicit override, and surfaces the FIDO2 validation errors unchanged.
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

    private static bool NeverPublicSuffix(string domain) => false;

    // Default both transports -> HID FIDO (the FIDO2 default), proving WebAuthn adds no transport logic.
    [Fact]
    public async Task CreateWebAuthnClientAsync_DefaultBothTransports_PicksHidFido()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.SmartCard);

        await Assert.ThrowsAsync<ConnectProbeException>(
            () => device.CreateWebAuthnClientAsync(Origin, NeverPublicSuffix, cancellationToken: Ct));

        Assert.Equal(typeof(IFidoHidConnection), device.RequestedConnection);
    }

    // Default fallback: first choice (HID FIDO) absent -> SmartCard (proves the forwarded HidFido->SmartCard order).
    [Fact]
    public async Task CreateWebAuthnClientAsync_DefaultSmartCardOnly_FallsBackToSmartCard()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.SmartCard);

        await Assert.ThrowsAsync<ConnectProbeException>(
            () => device.CreateWebAuthnClientAsync(Origin, NeverPublicSuffix, cancellationToken: Ct));

        Assert.Equal(typeof(ISmartCardConnection), device.RequestedConnection);
    }

    // Explicit override is forwarded and honored.
    [Fact]
    public async Task CreateWebAuthnClientAsync_OverrideSmartCard_OnBothTransports_UsesSmartCard()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.SmartCard);

        await Assert.ThrowsAsync<ConnectProbeException>(
            () => device.CreateWebAuthnClientAsync(
                Origin, NeverPublicSuffix, preferredConnection: ConnectionType.SmartCard, cancellationToken: Ct));

        Assert.Equal(typeof(ISmartCardConnection), device.RequestedConnection);
    }

    // Device-unsupported applet-valid override -> NotSupportedException, no connect attempt.
    [Fact]
    public async Task CreateWebAuthnClientAsync_OverrideSmartCard_NotOnDevice_ThrowsNotSupported()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido);

        await Assert.ThrowsAsync<NotSupportedException>(
            () => device.CreateWebAuthnClientAsync(
                Origin, NeverPublicSuffix, preferredConnection: ConnectionType.SmartCard, cancellationToken: Ct));

        Assert.Null(device.RequestedConnection);
    }

    // SCP does not change transport SELECTION (forwarded to FIDO2): scpKeyParams + default still routes to
    // HID FIDO. (Past selection, SCP over a non-SmartCard transport throws NotSupportedException in
    // ApplicationSession.InitializeCoreAsync — a pre-existing Core contract, not Phase 38 behavior.)
    [Fact]
    public async Task CreateWebAuthnClientAsync_ScpWithDefault_StillRoutesToHidFido()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.SmartCard);
        using var scp = Scp03KeyParameters.Default;

        await Assert.ThrowsAsync<ConnectProbeException>(
            () => device.CreateWebAuthnClientAsync(Origin, NeverPublicSuffix, scpKeyParams: scp, cancellationToken: Ct));

        Assert.Equal(typeof(IFidoHidConnection), device.RequestedConnection);
    }

    // Non-concrete override -> ArgumentException from the shared FIDO2 validation, no connect attempt.
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
                Origin, NeverPublicSuffix, preferredConnection: invalid, cancellationToken: Ct));

        Assert.Null(device.RequestedConnection);
    }

    // Applet-invalid concrete override (HID OTP) -> ArgumentException even when the device exposes it.
    [Fact]
    public async Task CreateWebAuthnClientAsync_OverrideHidOtp_AppletInvalid_ThrowsArgumentException()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.HidOtp);

        await Assert.ThrowsAsync<ArgumentException>(
            () => device.CreateWebAuthnClientAsync(
                Origin, NeverPublicSuffix, preferredConnection: ConnectionType.HidOtp, cancellationToken: Ct));

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
            () => device.CreateWebAuthnClientAsync(Origin, NeverPublicSuffix, cancellationToken: Ct));

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