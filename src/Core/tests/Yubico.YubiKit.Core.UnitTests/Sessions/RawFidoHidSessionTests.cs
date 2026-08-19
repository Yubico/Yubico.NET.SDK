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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Sessions;

namespace Yubico.YubiKit.Core.UnitTests.Sessions;

public class RawFidoHidSessionTests
{
    [Fact]
    public async Task SendAndReceiveAsync_ProcessesKeepAliveAndContinuationPackets()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var connection = new ScriptedFidoConnection();
        byte[] expected = Enumerable.Range(0, 60).Select(value => (byte)value).ToArray();
        connection.QueueResponse(
            CreateInitPacket(0x01020304, 0x3B, new byte[] { 0x01 }),
            CreateInitPacket(0x01020304, 0x42, expected),
            CreateContinuationPacket(0x01020304, 0, expected.AsSpan(57)));

        await using var raw = await RawFidoHidSession.CreateAsync(connection, cancellationToken);
        Assert.Empty(connection.SentPackets);

        ReadOnlyMemory<byte> response = await raw.SendAndReceiveAsync(
            0x42,
            new byte[] { 0xAA, 0xBB },
            cancellationToken);

        Assert.Equal(expected, response.ToArray());
        Assert.Equal(2, connection.SentPackets.Count);
        Assert.Equal(0x42, connection.SentPackets[1][4] & 0x7F);

        await raw.DisposeAsync();
        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public async Task SendAndReceiveAsync_OverlappingOperationThrowsThenSequentialCallSucceeds()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var connection = new ScriptedFidoConnection();
        connection.QueueResponse(
            CreateInitPacket(0x01020304, 0x42, new byte[] { 0x01 }),
            CreateInitPacket(0x01020304, 0x43, new byte[] { 0x02 }));
        await using var raw = await RawFidoHidSession.CreateAsync(connection, cancellationToken);
        connection.HoldNextSend();

        Task<ReadOnlyMemory<byte>> first = raw.SendAndReceiveAsync(0x42, ReadOnlyMemory<byte>.Empty, cancellationToken);
        await connection.SendStarted.Task.WaitAsync(cancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => raw.SendAndReceiveAsync(0x43, ReadOnlyMemory<byte>.Empty, cancellationToken));

        connection.ReleaseSend();
        Assert.Equal(new byte[] { 0x01 }, (await first).ToArray());
        Assert.Equal(
            new byte[] { 0x02 },
            (await raw.SendAndReceiveAsync(0x43, ReadOnlyMemory<byte>.Empty, cancellationToken)).ToArray());
    }

    private static byte[] CreateInitPacket(uint channelId, byte command, ReadOnlySpan<byte> payload)
    {
        var packet = new byte[64];
        BinaryPrimitives.WriteUInt32BigEndian(packet, channelId);
        packet[4] = (byte)(command | 0x80);
        packet[5] = (byte)(payload.Length >> 8);
        packet[6] = (byte)payload.Length;
        payload[..Math.Min(payload.Length, 57)].CopyTo(packet.AsSpan(7));
        return packet;
    }

    private static byte[] CreateContinuationPacket(uint channelId, byte sequence, ReadOnlySpan<byte> payload)
    {
        var packet = new byte[64];
        BinaryPrimitives.WriteUInt32BigEndian(packet, channelId);
        packet[4] = sequence;
        payload[..Math.Min(payload.Length, 59)].CopyTo(packet.AsSpan(5));
        return packet;
    }

    private sealed class ScriptedFidoConnection : IFidoHidConnection
    {
        private readonly Queue<ReadOnlyMemory<byte>> _responses = new();
        private byte[]? _initRequest;
        private TaskCompletionSource? _sendHold;

        public List<byte[]> SentPackets { get; } = [];
        public int DisposeCount { get; private set; }
        public int PacketSize => 64;
        public ConnectionType Type => ConnectionType.HidFido;
        public TaskCompletionSource SendStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void QueueResponse(params byte[][] packets)
        {
            foreach (byte[] packet in packets)
                _responses.Enqueue(packet);
        }

        public async Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] copy = packet.ToArray();
            SentPackets.Add(copy);
            if ((copy[4] & 0x7F) == 0x06)
                _initRequest = copy;
            TaskCompletionSource? hold = _sendHold;
            if (hold is not null)
            {
                SendStarted.TrySetResult();
                await hold.Task.WaitAsync(cancellationToken);
                _sendHold = null;
            }
        }

        public void HoldNextSend() =>
            _sendHold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseSend() => _sendHold?.TrySetResult();

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_initRequest is not null)
            {
                byte[] payload = new byte[17];
                _initRequest.AsSpan(7, 8).CopyTo(payload);
                BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(8), 0x01020304);
                payload[12] = 2;
                payload[13] = 5;
                payload[14] = 8;
                _initRequest = null;
                return Task.FromResult<ReadOnlyMemory<byte>>(CreateInitPacket(uint.MaxValue, 0x06, payload));
            }

            return Task.FromResult(_responses.Dequeue());
        }

        public void Dispose() => DisposeCount++;
        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }
}