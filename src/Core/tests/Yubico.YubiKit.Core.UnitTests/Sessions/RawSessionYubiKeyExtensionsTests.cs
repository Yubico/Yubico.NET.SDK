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

using System.Buffers.Binary;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
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
            yubiKey.CreateRawSmartCardSessionAsync(
                scp,
                new FirmwareVersion(5, 7, 2),
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(1, connection.DisposeAsyncCount);
    }

    [Fact]
    public async Task SmartCardDisposeAsync_DuringAdmittedExchange_DrainsBeforeDisposingHiddenConnection()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var connection = new MultiTransportConnection(ConnectionType.SmartCard);
        connection.Enqueue(new byte[] { 0x90, 0x00 });
        connection.HoldNextOperation();
        var yubiKey = new StubYubiKey(connection);
        RawSmartCardSession session = await yubiKey.CreateRawSmartCardSessionAsync(
            cancellationToken: cancellationToken);
        Task<ApduResponse> exchange = session.TransmitAndReceiveAsync(
            new ApduCommand(0x00, 0x01, 0x00, 0x00),
            cancellationToken: cancellationToken);
        await connection.OperationStarted.Task.WaitAsync(cancellationToken);

        Task asyncDisposal = session.DisposeAsync().AsTask();

        try
        {
            Assert.False(asyncDisposal.IsCompleted);
            Assert.Equal(0, connection.DisposeAsyncCount + connection.DisposeCount);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => session.TransmitAndReceiveAsync(
                new ApduCommand(0x00, 0x02, 0x00, 0x00),
                cancellationToken: cancellationToken));
        }
        finally
        {
            connection.ReleaseOperation();
        }

        Assert.True((await exchange).IsOK());
        await asyncDisposal.WaitAsync(cancellationToken);
        Assert.Equal(1, connection.DisposeAsyncCount + connection.DisposeCount);
    }

    [Fact]
    public async Task FidoDisposeAsync_DuringAdmittedExchange_DrainsBeforeDisposingHiddenConnection()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var connection = new MultiTransportConnection(ConnectionType.HidFido);
        connection.Enqueue(CreateFidoInitPacket(0x01020304, 0x42, [0xAB]));
        connection.HoldNextOperation();
        var yubiKey = new StubYubiKey(connection);
        RawFidoHidSession session = await yubiKey.CreateRawFidoHidSessionAsync(cancellationToken);
        Task<ReadOnlyMemory<byte>> exchange = session.SendAndReceiveAsync(
            0x42,
            new byte[] { 0x10 },
            cancellationToken);
        await connection.OperationStarted.Task.WaitAsync(cancellationToken);

        Task disposal = session.DisposeAsync().AsTask();

        try
        {
            Assert.False(disposal.IsCompleted);
            Assert.Equal(0, connection.DisposeAsyncCount);
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                session.SendAndReceiveAsync(0x42, ReadOnlyMemory<byte>.Empty, cancellationToken));
        }
        finally
        {
            connection.ReleaseOperation();
        }

        Assert.Equal(new byte[] { 0xAB }, (await exchange).ToArray());
        await disposal;
        Assert.Equal(1, connection.DisposeAsyncCount);
    }

    [Fact]
    public async Task OtpDisposeAsync_DuringAdmittedExchange_DrainsBeforeDisposingHiddenConnection()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var connection = new MultiTransportConnection(ConnectionType.HidOtp);
        connection.Enqueue(OtpStatus(versionMajor: 5));
        connection.Enqueue(OtpStatus(programmingSequence: 1));
        for (int i = 0; i < 10; i++)
            connection.Enqueue(OtpStatus(programmingSequence: 1));
        connection.Enqueue(OtpStatus(programmingSequence: 2));
        connection.HoldNextOperation();
        var yubiKey = new StubYubiKey(connection);
        RawOtpHidSession session = await yubiKey.CreateRawOtpHidSessionAsync(cancellationToken);
        Task<ReadOnlyMemory<byte>> exchange = session.SendAndReceiveAsync(
            0x13,
            new byte[] { 0x10 },
            cancellationToken);
        await connection.OperationStarted.Task.WaitAsync(cancellationToken);

        Task disposal = session.DisposeAsync().AsTask();

        try
        {
            Assert.False(disposal.IsCompleted);
            Assert.Equal(0, connection.DisposeAsyncCount);
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                session.SendAndReceiveAsync(0x13, ReadOnlyMemory<byte>.Empty, cancellationToken));
        }
        finally
        {
            connection.ReleaseOperation();
        }

        Assert.Equal(6, (await exchange).Length);
        await disposal;
        Assert.Equal(1, connection.DisposeAsyncCount);
    }

    private static byte[] CreateFidoInitPacket(uint channelId, byte command, ReadOnlySpan<byte> payload)
    {
        var packet = new byte[64];
        BinaryPrimitives.WriteUInt32BigEndian(packet, channelId);
        packet[4] = (byte)(command | 0x80);
        packet[5] = (byte)(payload.Length >> 8);
        packet[6] = (byte)payload.Length;
        payload[..Math.Min(payload.Length, 57)].CopyTo(packet.AsSpan(7));
        return packet;
    }

    private static byte[] OtpStatus(byte versionMajor = 0, byte programmingSequence = 0) =>
        [0x00, versionMajor, 0x04, 0x03, programmingSequence, 0x00, 0x00, 0x00];

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
        private readonly Queue<ReadOnlyMemory<byte>> _responses = new();
        private TaskCompletionSource? _operationHold;
        private byte[]? _fidoInitRequest;

        public int DisposeAsyncCount { get; private set; }
        public int DisposeCount { get; private set; }
        public Transport Transport => Transport.Usb;
        public int PacketSize => 64;
        public int FeatureReportSize => 8;

        public TaskCompletionSource OperationStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            await WaitIfHeldAsync(cancellationToken);
            return _responses.Dequeue();
        }

        public async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            byte[] snapshot = data.ToArray();
            if (Type == ConnectionType.HidFido && (snapshot[4] & 0x7F) == 0x06)
                _fidoInitRequest = snapshot;
            await WaitIfHeldAsync(cancellationToken);
        }

        public async Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            await WaitIfHeldAsync(cancellationToken);
            if (_fidoInitRequest is not null)
            {
                byte[] payload = new byte[17];
                _fidoInitRequest.AsSpan(7, 8).CopyTo(payload);
                BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(8), 0x01020304);
                payload[12] = 2;
                payload[13] = 5;
                payload[14] = 8;
                _fidoInitRequest = null;
                return CreateFidoInitPacket(uint.MaxValue, 0x06, payload);
            }

            return _responses.Dequeue();
        }

        public void Enqueue(ReadOnlyMemory<byte> response) => _responses.Enqueue(response);

        public void HoldNextOperation() =>
            _operationHold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseOperation() => _operationHold?.TrySetResult();

        private async Task WaitIfHeldAsync(CancellationToken cancellationToken)
        {
            TaskCompletionSource? hold = _operationHold;
            if (hold is null)
                return;

            OperationStarted.TrySetResult();
            await hold.Task.WaitAsync(cancellationToken);
            _operationHold = null;
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool SupportsExtendedApdu() => true;
        public void Dispose()
        {
            DisposeCount++;
        }

        public ValueTask DisposeAsync()
        {
            DisposeAsyncCount++;
            return ValueTask.CompletedTask;
        }
    }
}