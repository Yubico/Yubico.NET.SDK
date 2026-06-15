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
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

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

    // Phase 38.5 (ISC-14): held SmartCard falls back to HID FIDO through the public entry point; the opened
    // fallback connection is disposed when session init fails after connect (no leak), and the surfaced
    // failure is the post-connect session-init failure, not the SCardException.
    [Fact]
    public async Task CreateManagementSessionAsync_SmartCardHeld_FallsBackToHidFidoAndDisposesOnInitFailure()
    {
        var hid = new FailingFidoConnection();
        var device = new FallbackProbeYubiKey(ConnectionType.SmartCard | ConnectionType.HidFido)
            .Throws(ConnectionType.SmartCard, HeldSmartCard())
            .Returns(ConnectionType.HidFido, hid);

        var ex = await Record.ExceptionAsync(() => device.CreateManagementSessionAsync(cancellationToken: Ct));

        Assert.NotNull(ex);
        Assert.IsNotType<SCardException>(ex);
        Assert.True(hid.Disposed, "the opened fallback HID connection must be disposed on session-init failure");
        Assert.Equal([ConnectionType.SmartCard, ConnectionType.HidFido], device.Attempts);
    }

    // Phase 38.5 (ISC-7/ISC-14): an explicit override never falls back — a held SmartCard override surfaces
    // the held SCardException and makes no HID attempt (the applet passes the single-element override list).
    [Fact]
    public async Task CreateManagementSessionAsync_OverrideSmartCardHeld_DoesNotFallBack()
    {
        var device = new FallbackProbeYubiKey(ConnectionType.SmartCard | ConnectionType.HidFido)
            .Throws(ConnectionType.SmartCard, HeldSmartCard())
            .Returns(ConnectionType.HidFido, new FailingFidoConnection());

        await Assert.ThrowsAsync<SCardException>(() =>
            device.CreateManagementSessionAsync(preferredConnection: ConnectionType.SmartCard, cancellationToken: Ct));

        Assert.Equal([ConnectionType.SmartCard], device.Attempts);
    }

    // SCARD_E_SHARING_VIOLATION (0x8010000B): SCardException stores it in HResult; the literal avoids needing
    // Core's internal ErrorCode constants from this test assembly.
    private static SCardException HeldSmartCard() => new("held by another process", 0x8010000BL);

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

    private sealed class FallbackProbeYubiKey(ConnectionType available) : IYubiKey
    {
        private readonly Dictionary<ConnectionType, Func<IConnection>> _behaviors = new();

        public string DeviceId => "fallback-probe";
        public ConnectionType AvailableConnections { get; } = available;
        public List<ConnectionType> Attempts { get; } = [];

        public FallbackProbeYubiKey Returns(ConnectionType transport, IConnection connection)
        {
            _behaviors[transport] = () => connection;
            return this;
        }

        public FallbackProbeYubiKey Throws(ConnectionType transport, Exception exception)
        {
            _behaviors[transport] = () => throw exception;
            return this;
        }

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            var transport = typeof(TConnection) == typeof(ISmartCardConnection) ? ConnectionType.SmartCard
                : typeof(TConnection) == typeof(IFidoHidConnection) ? ConnectionType.HidFido
                : ConnectionType.HidOtp;
            Attempts.Add(transport);
            return Task.FromResult((TConnection)_behaviors[transport]());
        }
    }

    // A HID FIDO connection valid enough to return from connect but that fails every protocol exchange, so
    // ManagementSession initialization cannot complete; records disposal to prove no leak on the fallback path.
    private sealed class FailingFidoConnection : IFidoHidConnection
    {
        public bool Disposed { get; private set; }
        public ConnectionType Type => ConnectionType.HidFido;
        public int PacketSize => 64;

        public void Dispose() => Disposed = true;

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }

        public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("session-init probe failure");

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("session-init probe failure");
    }

    private sealed class ConnectProbeException : Exception;
}