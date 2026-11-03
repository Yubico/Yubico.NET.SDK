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
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Management.UnitTests;

/// <summary>
///     Management transport selection — app-specific smart default
///     (SmartCard, then HID FIDO, then HID OTP) plus explicit caller override, validated over a fake probe.
/// </summary>
/// <remarks>
///     The fake connection failures do not establish device <see cref="ApduException" /> handling.
/// </remarks>
public class IYubiKeyExtensionsTransportTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // With all transports exposed, default selection chooses SmartCard.
    [Fact]
    public async Task CreateManagementSessionAsync_DefaultAllTransports_PicksSmartCard()
    {
        var device = new SelectionProbeYubiKey(
            ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp);

        await Assert.ThrowsAsync<ConnectProbeException>(() => device.CreateManagementSessionAsync(cancellationToken: Ct));

        Assert.Equal(typeof(ISmartCardConnection), device.RequestedConnection);
    }

    // With SmartCard absent, selection chooses HID FIDO without attempting SmartCard.
    [Fact]
    public async Task CreateManagementSessionAsync_DefaultNoSmartCard_FallsBackToHidFido()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.HidOtp);

        await Assert.ThrowsAsync<ConnectProbeException>(() => device.CreateManagementSessionAsync(cancellationToken: Ct));

        Assert.Equal(typeof(IFidoHidConnection), device.RequestedConnection);
    }

    // With only HID OTP exposed, selection chooses it directly.
    [Fact]
    public async Task CreateManagementSessionAsync_DefaultOnlyHidOtp_FallsBackToHidOtp()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidOtp);

        await Assert.ThrowsAsync<ConnectProbeException>(() => device.CreateManagementSessionAsync(cancellationToken: Ct));

        Assert.Equal(typeof(IOtpHidConnection), device.RequestedConnection);
    }

    // An explicit non-default override on a multi-transport device beats the default order.
    [Fact]
    public async Task CreateManagementSessionAsync_OverrideHidOtp_OnAllTransports_UsesHidOtp()
    {
        var device = new SelectionProbeYubiKey(
            ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp);

        await Assert.ThrowsAsync<ConnectProbeException>(
            () => device.CreateManagementSessionAsync(
                new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp }, Ct));

        Assert.Equal(typeof(IOtpHidConnection), device.RequestedConnection);
    }

    // A supported preference absent from this device is rejected without a connection attempt.
    [Fact]
    public async Task CreateManagementSessionAsync_OverrideSmartCard_NotOnDevice_ThrowsNotSupported()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidOtp);

        await Assert.ThrowsAsync<NotSupportedException>(
            () => device.CreateManagementSessionAsync(
                new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard }, Ct));

        Assert.Null(device.RequestedConnection);
    }

    // A non-concrete preference is rejected without a connection attempt.
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
            () => device.CreateManagementSessionAsync(
                new SessionCreationOptions { PreferredConnectionType = invalid }, Ct));

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

    // A held default SmartCard transport propagates unchanged and no alternate interface is opened.
    [Fact]
    public async Task CreateManagementSessionAsync_SmartCardHeld_PropagatesWithoutHidFallback()
    {
        var hid = new FailingFidoConnection();
        var device = new FallbackProbeYubiKey(ConnectionType.SmartCard | ConnectionType.HidFido)
            .Throws(ConnectionType.SmartCard, HeldSmartCard())
            .Returns(ConnectionType.HidFido, hid);

        var ex = await Assert.ThrowsAsync<SCardException>(
            () => device.CreateManagementSessionAsync(cancellationToken: Ct));

        Assert.Equal(unchecked((int)0x8010000B), ex.HResult);
        Assert.False(hid.Disposed);
        Assert.Equal([ConnectionType.SmartCard], device.Attempts);
    }

    // A held explicit SmartCard override surfaces unchanged and makes no HID attempt.
    [Fact]
    public async Task CreateManagementSessionAsync_OverrideSmartCardHeld_DoesNotFallBack()
    {
        var device = new FallbackProbeYubiKey(ConnectionType.SmartCard | ConnectionType.HidFido)
            .Throws(ConnectionType.SmartCard, HeldSmartCard())
            .Returns(ConnectionType.HidFido, new FailingFidoConnection());

        await Assert.ThrowsAsync<SCardException>(() =>
            device.CreateManagementSessionAsync(
                new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard }, Ct));

        Assert.Equal([ConnectionType.SmartCard], device.Attempts);
    }

    // In-process ownership refusal propagates from the single selected transport without trying another member.
    [Fact]
    public async Task CreateManagementSessionAsync_CcidHeldInProcess_PropagatesWithoutFallback()
    {
        var hid = new FailingFidoConnection();
        var device = new FallbackProbeYubiKey(ConnectionType.SmartCard | ConnectionType.HidFido)
            .Throws(ConnectionType.SmartCard, new ConnectionInUseException("pcsc:test-reader is in use."))
            .Returns(ConnectionType.HidFido, hid);

        _ = await Assert.ThrowsAsync<ConnectionInUseException>(
            () => device.CreateManagementSessionAsync(cancellationToken: Ct));

        Assert.Equal([ConnectionType.SmartCard], device.Attempts);
    }

    [Fact]
    public async Task CreateManagementSessionAsync_ScpRequestedAndCcidHeld_DoesNotFallBackToPlaintextHid()
    {
        var device = new FallbackProbeYubiKey(ConnectionType.SmartCard | ConnectionType.HidFido)
            .Throws(ConnectionType.SmartCard, new ConnectionInUseException("pcsc:test-reader is in use."))
            .Returns(ConnectionType.HidFido, new FailingFidoConnection());

        _ = await Assert.ThrowsAsync<ConnectionInUseException>(() =>
            device.CreateManagementSessionAsync(
                new SessionCreationOptions { ScpKeyParameters = Scp03KeyParameters.Default }, Ct));

        Assert.Equal([ConnectionType.SmartCard], device.Attempts);
    }

    // In-process refusal on an explicit preference propagates; no other transport is attempted.
    [Fact]
    public async Task CreateManagementSessionAsync_CcidHeldInProcess_ExplicitOverrideDoesNotFallBack()
    {
        var device = new FallbackProbeYubiKey(ConnectionType.SmartCard | ConnectionType.HidFido)
            .Throws(ConnectionType.SmartCard, new ConnectionInUseException("pcsc:test-reader is in use."))
            .Returns(ConnectionType.HidFido, new FailingFidoConnection());

        _ = await Assert.ThrowsAsync<ConnectionInUseException>(() =>
            device.CreateManagementSessionAsync(
                new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard }, Ct));

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

    // A HID FIDO connection valid enough to return from connect but that fails every protocol exchange.
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