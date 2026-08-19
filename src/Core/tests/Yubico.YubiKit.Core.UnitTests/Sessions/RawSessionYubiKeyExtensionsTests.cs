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
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Sessions;

public class RawSessionYubiKeyExtensionsTests
{
    [Fact]
    public async Task CreateRawSmartCardSessionAsync_DisposeSession_DisposesHiddenConnection()
    {
        var connection = new MultiTransportConnection(ConnectionType.SmartCard);
        var yubiKey = new StubYubiKey(connection);

        RawSmartCardSession session = await yubiKey.CreateRawSmartCardSessionAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await session.DisposeAsync();

        Assert.Equal(1, connection.DisposeAsyncCount);
        Assert.Equal([typeof(ISmartCardConnection)], yubiKey.RequestedConnections);
    }

    [Fact]
    public async Task CreateRawFidoHidSessionAsync_DisposeSession_DisposesHiddenConnection()
    {
        var connection = new MultiTransportConnection(ConnectionType.HidFido);
        var yubiKey = new StubYubiKey(connection);

        RawFidoHidSession session = await yubiKey.CreateRawFidoHidSessionAsync(TestContext.Current.CancellationToken);
        await session.DisposeAsync();

        Assert.Equal(1, connection.DisposeAsyncCount);
        Assert.Equal([typeof(IFidoHidConnection)], yubiKey.RequestedConnections);
    }

    [Fact]
    public async Task CreateRawOtpHidSessionAsync_DisposeSession_DisposesHiddenConnection()
    {
        var connection = new MultiTransportConnection(ConnectionType.HidOtp);
        var yubiKey = new StubYubiKey(connection);

        RawOtpHidSession session = await yubiKey.CreateRawOtpHidSessionAsync(TestContext.Current.CancellationToken);
        await session.DisposeAsync();

        Assert.Equal(1, connection.DisposeAsyncCount);
        Assert.Equal([typeof(IOtpHidConnection)], yubiKey.RequestedConnections);
    }

    [Fact]
    public async Task CreateRawSmartCardSessionAsync_WhenScpInitializationFails_DisposesHiddenConnection()
    {
        var connection = new MultiTransportConnection(ConnectionType.SmartCard);
        var yubiKey = new StubYubiKey(connection);
        using var scp = Scp03KeyParameters.Default;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            yubiKey.CreateRawSmartCardSessionAsync(scp, TestContext.Current.CancellationToken));

        Assert.Equal(1, connection.DisposeAsyncCount);
    }

    private sealed class StubYubiKey(IConnection connection) : IYubiKey
    {
        public string DeviceId => "raw-session-test";
        public ConnectionType AvailableConnections => connection.Type;
        public List<Type> RequestedConnections { get; } = [];

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestedConnections.Add(typeof(TConnection));
            return Task.FromResult((TConnection)connection);
        }
    }

    private sealed class MultiTransportConnection(ConnectionType type)
        : ISmartCardConnection, IFidoHidConnection, IOtpHidConnection
    {
        public ConnectionType Type { get; } = type;
        public int DisposeAsyncCount { get; private set; }
        public Transport Transport => Transport.Usb;
        public int PacketSize => 64;
        public int FeatureReportSize => 8;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("No scripted response.");

        public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("No scripted response.");

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool SupportsExtendedApdu() => true;
        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
            DisposeAsyncCount++;
            return ValueTask.CompletedTask;
        }
    }
}