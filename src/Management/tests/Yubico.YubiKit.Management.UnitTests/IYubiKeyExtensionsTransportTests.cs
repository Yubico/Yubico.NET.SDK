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
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Management.UnitTests;

/// <summary>
///     Phase 38: Management transport selection — app-specific smart default
///     (SmartCard, then HID FIDO, then HID OTP) plus explicit caller override, validated over a fake probe.
/// </summary>
public class IYubiKeyExtensionsTransportTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // (a) default top-choice: all transports present -> SmartCard.
    [Fact]
    public async Task CreateManagementSessionAsync_DefaultAllTransports_PicksSmartCard()
    {
        var device = new SelectionProbeYubiKey(
            ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp);

        await Assert.ThrowsAsync<ConnectProbeException>(() => device.CreateManagementSessionAsync(cancellationToken: Ct));

        Assert.Equal(typeof(ISmartCardConnection), device.RequestedConnection);
    }

    // (b) default fallback: first choice (SmartCard) absent -> HID FIDO (second choice).
    [Fact]
    public async Task CreateManagementSessionAsync_DefaultNoSmartCard_FallsBackToHidFido()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.HidOtp);

        await Assert.ThrowsAsync<ConnectProbeException>(() => device.CreateManagementSessionAsync(cancellationToken: Ct));

        Assert.Equal(typeof(IFidoHidConnection), device.RequestedConnection);
    }

    // (b) default fallback: only the third choice (HID OTP) remains.
    [Fact]
    public async Task CreateManagementSessionAsync_DefaultOnlyHidOtp_FallsBackToHidOtp()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidOtp);

        await Assert.ThrowsAsync<ConnectProbeException>(() => device.CreateManagementSessionAsync(cancellationToken: Ct));

        Assert.Equal(typeof(IOtpHidConnection), device.RequestedConnection);
    }

    // (c) explicit non-default override on a multi-transport device beats the default order.
    [Fact]
    public async Task CreateManagementSessionAsync_OverrideHidOtp_OnAllTransports_UsesHidOtp()
    {
        var device = new SelectionProbeYubiKey(
            ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp);

        await Assert.ThrowsAsync<ConnectProbeException>(
            () => device.CreateManagementSessionAsync(preferredConnection: ConnectionType.HidOtp, cancellationToken: Ct));

        Assert.Equal(typeof(IOtpHidConnection), device.RequestedConnection);
    }

    // (d) device-unsupported applet-valid override -> NotSupportedException, no connect attempt.
    [Fact]
    public async Task CreateManagementSessionAsync_OverrideSmartCard_NotOnDevice_ThrowsNotSupported()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidOtp);

        await Assert.ThrowsAsync<NotSupportedException>(
            () => device.CreateManagementSessionAsync(preferredConnection: ConnectionType.SmartCard, cancellationToken: Ct));

        Assert.Null(device.RequestedConnection);
    }

    // (e) non-concrete override values -> ArgumentException, no connect attempt.
    [Theory]
    [InlineData(ConnectionType.Hid)]
    [InlineData(ConnectionType.All)]
    [InlineData(ConnectionType.Unknown)]
    [InlineData(ConnectionType.SmartCard | ConnectionType.HidFido)]
    public async Task CreateManagementSessionAsync_NonConcreteOverride_ThrowsArgumentException(ConnectionType invalid)
    {
        var device = new SelectionProbeYubiKey(
            ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp);

        await Assert.ThrowsAsync<ArgumentException>(
            () => device.CreateManagementSessionAsync(preferredConnection: invalid, cancellationToken: Ct));

        Assert.Null(device.RequestedConnection);
    }

    // No usable transport -> NotSupportedException, no connect attempt.
    [Fact]
    public async Task CreateManagementSessionAsync_NoUsableTransport_ThrowsNotSupported()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.Unknown);

        await Assert.ThrowsAsync<NotSupportedException>(() => device.CreateManagementSessionAsync(cancellationToken: Ct));

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