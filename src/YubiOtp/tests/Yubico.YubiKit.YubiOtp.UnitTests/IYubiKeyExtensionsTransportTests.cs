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

using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.YubiOtp.UnitTests;

/// <summary>
///     Phase 38: YubiOTP transport selection — app-specific smart default (SmartCard, then HID OTP) plus
///     explicit caller override, validated over a fake probe that records the requested connection type.
/// </summary>
public class IYubiKeyExtensionsTransportTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // (a) default top-choice: both transports present -> SmartCard.
    [Fact]
    public async Task CreateYubiOtpSessionAsync_DefaultBothTransports_PicksSmartCard()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.SmartCard | ConnectionType.HidOtp);

        await Assert.ThrowsAsync<ConnectProbeException>(() => device.CreateYubiOtpSessionAsync(cancellationToken: Ct));

        Assert.Equal(typeof(ISmartCardConnection), device.RequestedConnection);
    }

    // (b) default fallback: first choice (SmartCard) absent -> HID OTP.
    [Fact]
    public async Task CreateYubiOtpSessionAsync_DefaultHidOtpOnly_FallsBackToHidOtp()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidOtp);

        await Assert.ThrowsAsync<ConnectProbeException>(() => device.CreateYubiOtpSessionAsync(cancellationToken: Ct));

        Assert.Equal(typeof(IOtpHidConnection), device.RequestedConnection);
    }

    // (c) explicit non-default override on a multi-transport device beats the default order.
    [Fact]
    public async Task CreateYubiOtpSessionAsync_OverrideHidOtp_OnBothTransports_UsesHidOtp()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.SmartCard | ConnectionType.HidOtp);

        await Assert.ThrowsAsync<ConnectProbeException>(
            () => device.CreateYubiOtpSessionAsync(preferredConnection: ConnectionType.HidOtp, cancellationToken: Ct));

        Assert.Equal(typeof(IOtpHidConnection), device.RequestedConnection);
    }

    // (d) device-unsupported applet-valid override -> NotSupportedException, no connect attempt.
    [Fact]
    public async Task CreateYubiOtpSessionAsync_OverrideSmartCard_NotOnDevice_ThrowsNotSupported()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidOtp);

        await Assert.ThrowsAsync<NotSupportedException>(
            () => device.CreateYubiOtpSessionAsync(preferredConnection: ConnectionType.SmartCard, cancellationToken: Ct));

        Assert.Null(device.RequestedConnection);
    }

    // (e) non-concrete override values -> ArgumentException, no connect attempt.
    [Theory]
    [InlineData(ConnectionType.Hid)]
    [InlineData(ConnectionType.All)]
    [InlineData(ConnectionType.Unknown)]
    [InlineData(ConnectionType.SmartCard | ConnectionType.HidOtp)]
    public async Task CreateYubiOtpSessionAsync_NonConcreteOverride_ThrowsArgumentException(ConnectionType invalid)
    {
        var device = new SelectionProbeYubiKey(ConnectionType.SmartCard | ConnectionType.HidOtp);

        await Assert.ThrowsAsync<ArgumentException>(
            () => device.CreateYubiOtpSessionAsync(preferredConnection: invalid, cancellationToken: Ct));

        Assert.Null(device.RequestedConnection);
    }

    // (f) applet-invalid concrete override (HID FIDO) -> ArgumentException even when the device exposes it.
    [Fact]
    public async Task CreateYubiOtpSessionAsync_OverrideHidFido_AppletInvalid_ThrowsArgumentException()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.SmartCard | ConnectionType.HidFido);

        await Assert.ThrowsAsync<ArgumentException>(
            () => device.CreateYubiOtpSessionAsync(preferredConnection: ConnectionType.HidFido, cancellationToken: Ct));

        Assert.Null(device.RequestedConnection);
    }

    // No usable transport (only HID FIDO, which YubiOTP cannot use) -> NotSupportedException, no connect attempt.
    [Fact]
    public async Task CreateYubiOtpSessionAsync_NoUsableTransport_ThrowsNotSupported()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido);

        await Assert.ThrowsAsync<NotSupportedException>(() => device.CreateYubiOtpSessionAsync(cancellationToken: Ct));

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