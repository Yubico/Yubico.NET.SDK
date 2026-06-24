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

using System.Buffers.Binary;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;

namespace Yubico.YubiKit.Core.UnitTests.Protocols.Fido.Hid;

public class FidoHidProtocolTests
{
    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1, 1, true)]
    [InlineData(0x7F, 0x7F, true)]
    [InlineData(0, 0x80, true)]
    [InlineData(1, 0x81, true)]
    [InlineData(1, 0, false)]
    public void IsExpectedContinuationSequence_MasksSequenceToSevenBits(
        byte sequence,
        byte expectedSequence,
        bool expected)
    {
        Assert.Equal(expected, FidoHidProtocol.IsExpectedContinuationSequence(sequence, expectedSequence));
    }

    [Fact]
    public async Task SendVendorCommandAsync_ResponseWithWrongContinuationSequence_ThrowsInvalidOperationException()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);

        var responsePayload = Enumerable.Range(0, CtapConstants.InitDataSize + 1)
            .Select(i => (byte)i)
            .ToArray();
        connection.QueueResponsePackets(
            CreateInitPacket(0x01020304, CtapConstants.CtapVendorFirst, responsePayload),
            CreateContinuationPacket(0x01020304, sequence: 1, responsePayload.AsSpan(CtapConstants.InitDataSize)));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            protocol.SendVendorCommandAsync(CtapConstants.CtapVendorFirst, ReadOnlyMemory<byte>.Empty, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendVendorCommandAsync_ResponseWithWrongContinuationChannel_ThrowsInvalidOperationException()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);

        var responsePayload = Enumerable.Range(0, CtapConstants.InitDataSize + 1)
            .Select(i => (byte)i)
            .ToArray();
        connection.QueueResponsePackets(
            CreateInitPacket(0x01020304, CtapConstants.CtapVendorFirst, responsePayload),
            CreateContinuationPacket(0x05060708, sequence: 0, responsePayload.AsSpan(CtapConstants.InitDataSize)));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            protocol.SendVendorCommandAsync(CtapConstants.CtapVendorFirst, ReadOnlyMemory<byte>.Empty, TestContext.Current.CancellationToken));

        Assert.Contains("channel", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendVendorCommandAsync_ResponseWithWrongInitChannel_ThrowsInvalidOperationException()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);

        var responsePayload = new byte[] { 0xAA };
        connection.QueueResponsePackets(
            CreateInitPacket(0x05060708, CtapConstants.CtapVendorFirst, responsePayload));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            protocol.SendVendorCommandAsync(CtapConstants.CtapVendorFirst, ReadOnlyMemory<byte>.Empty, TestContext.Current.CancellationToken));

        Assert.Contains("channel", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendVendorCommandAsync_ResponseWithShortInitPacket_ThrowsInvalidOperationException()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);

        connection.QueueResponsePackets([0x01, 0x02, 0x03]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            protocol.SendVendorCommandAsync(CtapConstants.CtapVendorFirst, ReadOnlyMemory<byte>.Empty, TestContext.Current.CancellationToken));

        Assert.Contains("too short", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendVendorCommandAsync_ResponseWithInitPacketAsContinuation_ThrowsInvalidOperationException()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);

        var responsePayload = Enumerable.Range(0, CtapConstants.InitDataSize + 1)
            .Select(i => (byte)i)
            .ToArray();
        connection.QueueResponsePackets(
            CreateInitPacket(0x01020304, CtapConstants.CtapVendorFirst, responsePayload),
            CreateInitPacket(0x01020304, CtapConstants.CtapHidPing, responsePayload.AsSpan(CtapConstants.InitDataSize)));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            protocol.SendVendorCommandAsync(CtapConstants.CtapVendorFirst, ReadOnlyMemory<byte>.Empty, TestContext.Current.CancellationToken));

        Assert.Contains("continuation", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendVendorCommandAsync_ResponseWithExpectedContinuationSequence_Succeeds()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);

        var responsePayload = Enumerable.Range(0, CtapConstants.InitDataSize + 1)
            .Select(i => (byte)i)
            .ToArray();
        connection.QueueResponsePackets(
            CreateInitPacket(0x01020304, CtapConstants.CtapVendorFirst, responsePayload),
            CreateContinuationPacket(0x01020304, sequence: 0, responsePayload.AsSpan(CtapConstants.InitDataSize)));

        var response = await protocol.SendVendorCommandAsync(
            CtapConstants.CtapVendorFirst,
            ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken);

        Assert.Equal(responsePayload, response.ToArray());
    }

    private static byte[] CreateInitPacket(uint channelId, byte command, ReadOnlySpan<byte> payload)
    {
        var packet = new byte[CtapConstants.PacketSize];
        BinaryPrimitives.WriteUInt32BigEndian(packet, channelId);
        packet[4] = (byte)(command | CtapConstants.InitPacketMask);
        packet[5] = (byte)(payload.Length >> 8);
        packet[6] = (byte)payload.Length;
        payload[..Math.Min(payload.Length, CtapConstants.InitDataSize)]
            .CopyTo(packet.AsSpan(CtapConstants.InitHeaderSize));
        return packet;
    }

    private static byte[] CreateContinuationPacket(uint channelId, byte sequence, ReadOnlySpan<byte> payload)
    {
        var packet = new byte[CtapConstants.PacketSize];
        BinaryPrimitives.WriteUInt32BigEndian(packet, channelId);
        packet[4] = sequence;
        payload[..Math.Min(payload.Length, CtapConstants.ContinuationDataSize)]
            .CopyTo(packet.AsSpan(CtapConstants.ContinuationHeaderSize));
        return packet;
    }

    private sealed class FakeFidoHidConnection : IFidoHidConnection
    {
        private readonly Queue<byte[]> _responsePackets = new();
        private byte[]? _lastInitRequest;
        private bool _initResponseSent;

        public int PacketSize => CtapConstants.PacketSize;

        public ConnectionType Type => ConnectionType.HidFido;

        public void QueueResponsePackets(params byte[][] packets)
        {
            foreach (var packet in packets)
            {
                _responsePackets.Enqueue(packet);
            }
        }

        public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((packet.Span[4] & ~CtapConstants.InitPacketMask) == CtapConstants.CtapHidInit)
            {
                _lastInitRequest = packet.ToArray();
            }

            return Task.CompletedTask;
        }

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_initResponseSent)
            {
                _initResponseSent = true;
                return Task.FromResult<ReadOnlyMemory<byte>>(CreateInitResponse());
            }

            return Task.FromResult<ReadOnlyMemory<byte>>(_responsePackets.Dequeue());
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private byte[] CreateInitResponse()
        {
            if (_lastInitRequest is null)
                throw new InvalidOperationException("INIT request was not sent.");

            var payload = new byte[17];
            _lastInitRequest.AsSpan(CtapConstants.InitHeaderSize, CtapConstants.NonceSize).CopyTo(payload);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(8), 0x01020304);
            payload[12] = 2;
            payload[13] = 5;
            payload[14] = 8;
            payload[15] = 0;
            payload[16] = 0;
            return CreateInitPacket(CtapConstants.BroadcastChannelId, CtapConstants.CtapHidInit, payload);
        }
    }
}
