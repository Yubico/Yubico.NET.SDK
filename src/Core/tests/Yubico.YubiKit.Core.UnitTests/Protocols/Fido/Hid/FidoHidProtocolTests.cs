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
using System.Runtime.InteropServices;
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
    public async Task InitializeAsync_PerformsCtapHidInit()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);

        await protocol.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.True(protocol.IsChannelInitialized);
        Assert.Equal(1, connection.InitRequestCount);
    }

    [Fact]
    public async Task InitializeAsync_PopulatesFirmwareVersionFromCtapHidInit()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);

        await protocol.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new FirmwareVersion(5, 8, 0), protocol.FirmwareVersion);
    }

    [Fact]
    public async Task InitializeAsync_WhenAlreadyInitialized_DoesNotSendSecondInit()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);

        await protocol.InitializeAsync(TestContext.Current.CancellationToken);
        await protocol.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, connection.InitRequestCount);
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
    public async Task SendVendorCommandAsync_ResponseWithWrongCommand_ThrowsInvalidOperationException()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);
        connection.QueueResponsePackets(
            CreateInitPacket(0x01020304, CtapConstants.CtapHidPing, [0xAA]));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            protocol.SendVendorCommandAsync(
                CtapConstants.CtapVendorFirst,
                ReadOnlyMemory<byte>.Empty,
                TestContext.Current.CancellationToken));

        Assert.Contains("command", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(CtapConstants.CtapYubikeyDeviceConfig)]
    [InlineData(CtapConstants.CtapReadConfig)]
    [InlineData(CtapConstants.CtapWriteConfig)]
    [InlineData(CtapConstants.CtapHidPing)]
    public async Task SendVendorCommandAsync_MatchingResponse_NormalizesInitBitOnBothCommands(byte command)
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);
        connection.QueueResponsePackets(CreateInitPacket(0x01020304, command, [0xAA]));

        ReadOnlyMemory<byte> response = await protocol.SendVendorCommandAsync(
            command,
            ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken);

        Assert.Equal(new byte[] { 0xAA }, response.ToArray());
    }

    [Fact]
    public async Task SendVendorCommandAsync_CtapHidError_ThrowsProtocolFailureWithErrorCode()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);
        connection.QueueResponsePackets(
            CreateInitPacket(0x01020304, CtapConstants.CtapHidError, [0x06]));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            protocol.SendVendorCommandAsync(
                CtapConstants.CtapVendorFirst,
                ReadOnlyMemory<byte>.Empty,
                TestContext.Current.CancellationToken));

        Assert.Contains("CTAP HID error", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0x06", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendVendorCommandAsync_KeepAliveThenMatchingResponse_Succeeds()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);
        connection.QueueResponsePackets(
            CreateInitPacket(0x01020304, CtapConstants.CtapHidKeepAlive, [0x01]),
            CreateInitPacket(0x01020304, CtapConstants.CtapVendorFirst, [0xAA]));

        ReadOnlyMemory<byte> response = await protocol.SendVendorCommandAsync(
            CtapConstants.CtapVendorFirst,
            ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken);

        Assert.Equal(new byte[] { 0xAA }, response.ToArray());
    }

    [Fact]
    public async Task SendVendorCommandAsync_ResponseWithShortInitPacket_ThrowsInvalidOperationException()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);

        connection.QueueResponsePackets([0x01, 0x02, 0x03]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            protocol.SendVendorCommandAsync(CtapConstants.CtapVendorFirst, ReadOnlyMemory<byte>.Empty, TestContext.Current.CancellationToken));

        Assert.Contains("exactly", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendVendorCommandAsync_ResponseWithPartialInitPacket_ThrowsInvalidOperationException()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);

        var packet = CreateInitPacket(0x01020304, CtapConstants.CtapVendorFirst, [0xAA]);
        connection.QueueResponsePackets(packet[..CtapConstants.InitHeaderSize]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            protocol.SendVendorCommandAsync(CtapConstants.CtapVendorFirst, ReadOnlyMemory<byte>.Empty, TestContext.Current.CancellationToken));

        Assert.Contains("exactly", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendVendorCommandAsync_ResponseWithPartialContinuationPacket_ThrowsInvalidOperationException()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);

        var responsePayload = Enumerable.Range(0, CtapConstants.InitDataSize + 1)
            .Select(i => (byte)i)
            .ToArray();
        var continuation = CreateContinuationPacket(0x01020304, sequence: 0, responsePayload.AsSpan(CtapConstants.InitDataSize));
        connection.QueueResponsePackets(
            CreateInitPacket(0x01020304, CtapConstants.CtapVendorFirst, responsePayload),
            continuation[..CtapConstants.ContinuationHeaderSize]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            protocol.SendVendorCommandAsync(CtapConstants.CtapVendorFirst, ReadOnlyMemory<byte>.Empty, TestContext.Current.CancellationToken));

        Assert.Contains("exactly", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendVendorCommandAsync_ResponseWithWrongChannelKeepAlive_ThrowsInvalidOperationException()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);

        connection.QueueResponsePackets(
            CreateInitPacket(0x05060708, CtapConstants.CtapHidKeepAlive, [0x01]),
            CreateInitPacket(0x01020304, CtapConstants.CtapVendorFirst, [0xAA]));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            protocol.SendVendorCommandAsync(CtapConstants.CtapVendorFirst, ReadOnlyMemory<byte>.Empty, TestContext.Current.CancellationToken));

        Assert.Contains("channel", ex.Message, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public async Task SendVendorCommandAsync_ResponseContinuationInInitPositionMatchingKeepAliveCommand_ThrowsInvalidOperationException()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);

        connection.QueueResponsePackets(
            CreateContinuationPacket(0x01020304, CtapConstants.CtapHidKeepAlive, [0xAA]),
            CreateInitPacket(0x01020304, CtapConstants.CtapVendorFirst, [0xBB]));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => protocol.SendVendorCommandAsync(
            CtapConstants.CtapVendorFirst,
            ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken));

        Assert.Contains("init", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendVendorCommandAsync_AfterSuccessfulSends_ZerosSdkOwnedPacketsButNotCallerPayload()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);
        await protocol.InitializeAsync(TestContext.Current.CancellationToken);
        connection.ClearCapturedPackets();
        connection.QueueResponsePackets(
            CreateInitPacket(0x01020304, CtapConstants.CtapVendorFirst, [0xAA]));
        byte[] callerPayload = Enumerable.Range(1, CtapConstants.InitDataSize + 2)
            .Select(value => (byte)value)
            .ToArray();
        byte[] expectedCallerPayload = callerPayload.ToArray();

        _ = await protocol.SendVendorCommandAsync(
            CtapConstants.CtapVendorFirst,
            callerPayload,
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedCallerPayload, callerPayload);
        Assert.Equal(2, connection.SentPacketSnapshots.Count);
        Assert.Equal(
            expectedCallerPayload[..CtapConstants.InitDataSize],
            connection.SentPacketSnapshots[0].AsSpan(
                CtapConstants.InitHeaderSize,
                CtapConstants.InitDataSize).ToArray());
        Assert.Equal(
            expectedCallerPayload[CtapConstants.InitDataSize..],
            connection.SentPacketSnapshots[1].AsSpan(
                CtapConstants.ContinuationHeaderSize,
                expectedCallerPayload.Length - CtapConstants.InitDataSize).ToArray());
        Assert.All(connection.RetainedSentPackets, packet =>
            Assert.All(packet.ToArray(), value => Assert.Equal(0, value)));
    }

    [Fact]
    public async Task SendVendorCommandAsync_WhenSendThrows_ZerosSdkOwnedPacketButNotCallerPayload()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);
        await protocol.InitializeAsync(TestContext.Current.CancellationToken);
        connection.ClearCapturedPackets();
        connection.ThrowOnNextSend = true;
        byte[] callerPayload = [0x11, 0x22, 0x33];

        await Assert.ThrowsAsync<IOException>(() => protocol.SendVendorCommandAsync(
            CtapConstants.CtapVendorFirst,
            callerPayload,
            TestContext.Current.CancellationToken));

        Assert.Equal(new byte[] { 0x11, 0x22, 0x33 }, callerPayload);
        ReadOnlyMemory<byte> retained = Assert.Single(connection.RetainedSentPackets);
        Assert.All(retained.ToArray(), value => Assert.Equal(0, value));
        Assert.Contains((byte)0x11, Assert.Single(connection.SentPacketSnapshots));
    }

    [Fact]
    public async Task SendVendorCommandAsync_WhenContinuationValidationFails_ZerosPartialResponseBuffer()
    {
        var allocatedBuffers = new List<byte[]>();
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection, responseBufferFactory: length =>
        {
            var buffer = new byte[length];
            allocatedBuffers.Add(buffer);
            return buffer;
        });
        byte[] responsePayload = Enumerable.Range(1, CtapConstants.InitDataSize + 1)
            .Select(value => (byte)value)
            .ToArray();
        connection.QueueResponsePackets(
            CreateInitPacket(0x01020304, CtapConstants.CtapVendorFirst, responsePayload),
            CreateContinuationPacket(0x01020304, sequence: 1, responsePayload.AsSpan(CtapConstants.InitDataSize)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => protocol.SendVendorCommandAsync(
            CtapConstants.CtapVendorFirst,
            ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken));

        byte[] partialResponseBuffer = allocatedBuffers[^1];
        Assert.Equal(responsePayload.Length, partialResponseBuffer.Length);
        Assert.All(partialResponseBuffer, value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task SendVendorCommandAsync_WhenResponseSucceeds_TransfersUnclearedResponseBuffer()
    {
        var allocatedBuffers = new List<byte[]>();
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection, responseBufferFactory: length =>
        {
            var buffer = new byte[length];
            allocatedBuffers.Add(buffer);
            return buffer;
        });
        byte[] responsePayload = [0xAA, 0xBB, 0xCC];
        connection.QueueResponsePackets(
            CreateInitPacket(0x01020304, CtapConstants.CtapVendorFirst, responsePayload));

        ReadOnlyMemory<byte> response = await protocol.SendVendorCommandAsync(
            CtapConstants.CtapVendorFirst,
            ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken);

        Assert.True(MemoryMarshal.TryGetArray(response, out ArraySegment<byte> responseSegment));
        Assert.Same(allocatedBuffers[^1], responseSegment.Array);
        Assert.Equal(responsePayload, response.ToArray());
    }

    /// <summary>
    /// Abandoning a ceremony while the authenticator is asking for a touch must tell the
    /// authenticator, or it keeps that request pending on a channel nobody is servicing and the
    /// key stays unusable until it is physically re-plugged. The cancel is sent once, not on
    /// every keep-alive frame that follows it, and the channel is left immediately reusable.
    /// </summary>
    /// <remarks>
    /// The caller is abandoned mid-exchange rather than up front: <see cref="ExchangeGuard"/>
    /// rejects an already-cancelled token before the exchange is admitted, so a pre-cancelled
    /// token would never reach the keep-alive loop this covers.
    /// </remarks>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [Trait("Category", "RuntimeResilience")]
    public async Task SendVendorCommandAsync_AbandonedDuringKeepAlive_SendsExactlyOneCancelAndLeavesChannelUsable(
        int keepAliveCount)
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);
        using var cts = new CancellationTokenSource();

        // The authenticator reports it is waiting for a touch, then answers the cancel with
        // CTAP2_ERR_KEEPALIVE_CANCEL (0x2D).
        connection.QueueResponsePackets([
            .. Enumerable.Range(0, keepAliveCount).Select(_ =>
                CreateInitPacket(0x01020304, CtapConstants.CtapHidKeepAlive, [KeepAliveUpNeeded])),
            CreateInitPacket(0x01020304, CtapConstants.CtapVendorFirst, [0x2D])
        ]);

        // Abandon the moment the authenticator says it is waiting for a touch.
        connection.OnResponseDequeued = packet =>
        {
            if (IsCommand(packet, CtapConstants.CtapHidKeepAlive))
                cts.Cancel();
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            protocol.SendVendorCommandAsync(
                CtapConstants.CtapVendorFirst,
                ReadOnlyMemory<byte>.Empty,
                cts.Token));

        Assert.Equal(
            1,
            connection.SentPacketSnapshots.Count(packet => IsCommand(packet, CtapConstants.CtapHidCancel)));

        // The symptom the fix exists to prevent: before it, the abandoned user-presence request
        // left the channel busy and the next exchange failed until the key was re-plugged.
        connection.OnResponseDequeued = null;
        connection.QueueResponsePackets(
            CreateInitPacket(0x01020304, CtapConstants.CtapVendorFirst, [0xAA]));

        var afterCancel = await protocol.SendVendorCommandAsync(
            CtapConstants.CtapVendorFirst,
            ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken);

        Assert.Equal(0xAA, afterCancel.Span[0]);
    }

    /// <summary>
    /// A touch landing at the same moment the caller abandons makes the authenticator's answer a
    /// real multi-packet response rather than the one-byte cancel acknowledgement. Every
    /// continuation frame must still be drained before cancellation is surfaced, or the next
    /// exchange reads the leftovers as its own response.
    /// </summary>
    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task SendVendorCommandAsync_AbandonedWhenAnswerIsMultiPacket_DrainsEveryFrame()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);
        using var cts = new CancellationTokenSource();

        var racedPayload = new byte[CtapConstants.InitDataSize + 20];
        racedPayload.AsSpan().Fill(0x5A);

        connection.QueueResponsePackets(
            CreateInitPacket(0x01020304, CtapConstants.CtapHidKeepAlive, [KeepAliveUpNeeded]),
            CreateInitPacket(0x01020304, CtapConstants.CtapVendorFirst, racedPayload),
            CreateContinuationPacket(0x01020304, sequence: 0, racedPayload.AsSpan(CtapConstants.InitDataSize)));

        connection.OnResponseDequeued = packet =>
        {
            if (IsCommand(packet, CtapConstants.CtapHidKeepAlive))
                cts.Cancel();
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            protocol.SendVendorCommandAsync(
                CtapConstants.CtapVendorFirst,
                ReadOnlyMemory<byte>.Empty,
                cts.Token));

        // The continuation frame was consumed, so the next exchange sees its own response rather
        // than the tail of the abandoned one.
        connection.OnResponseDequeued = null;
        connection.QueueResponsePackets(
            CreateInitPacket(0x01020304, CtapConstants.CtapVendorFirst, [0xAA]));

        var afterCancel = await protocol.SendVendorCommandAsync(
            CtapConstants.CtapVendorFirst,
            ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, afterCancel.Length);
        Assert.Equal(0xAA, afterCancel.Span[0]);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task SendVendorCommandAsync_NotAbandoned_DoesNotSendCtapHidCancel()
    {
        var connection = new FakeFidoHidConnection();
        var protocol = new FidoHidProtocol(connection);

        connection.QueueResponsePackets(
            CreateInitPacket(0x01020304, CtapConstants.CtapHidKeepAlive, [KeepAliveUpNeeded]),
            CreateInitPacket(0x01020304, CtapConstants.CtapVendorFirst, [0xAA]));

        var response = await protocol.SendVendorCommandAsync(
            CtapConstants.CtapVendorFirst,
            ReadOnlyMemory<byte>.Empty,
            TestContext.Current.CancellationToken);

        Assert.Equal(0xAA, response.Span[0]);
        Assert.DoesNotContain(
            connection.SentPacketSnapshots,
            packet => IsCommand(packet, CtapConstants.CtapHidCancel));
    }

    /// <summary>CTAP HID keep-alive status byte meaning a touch is awaited.</summary>
    private const byte KeepAliveUpNeeded = 0x02;

    private static bool IsCommand(byte[] packet, byte command) =>
        (packet[4] & ~CtapConstants.InitPacketMask) == command;

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

        public int InitRequestCount { get; private set; }
        public bool ThrowOnNextSend { get; set; }

        /// <summary>
        /// Invoked as each queued packet is handed to the protocol, letting a test act at a
        /// precise point mid-exchange — notably abandoning the caller while a keep-alive is
        /// being processed.
        /// </summary>
        public Action<byte[]>? OnResponseDequeued { get; set; }

        public List<ReadOnlyMemory<byte>> RetainedSentPackets { get; } = [];
        public List<byte[]> SentPacketSnapshots { get; } = [];

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
            RetainedSentPackets.Add(packet);
            byte[] snapshot = packet.ToArray();
            SentPacketSnapshots.Add(snapshot);
            if ((snapshot[4] & ~CtapConstants.InitPacketMask) == CtapConstants.CtapHidInit)
            {
                InitRequestCount++;
                _lastInitRequest = snapshot;
            }

            if (ThrowOnNextSend)
            {
                ThrowOnNextSend = false;
                throw new IOException("Scripted send failure.");
            }

            return Task.CompletedTask;
        }

        public void ClearCapturedPackets()
        {
            RetainedSentPackets.Clear();
            SentPacketSnapshots.Clear();
        }

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_initResponseSent)
            {
                _initResponseSent = true;
                return Task.FromResult<ReadOnlyMemory<byte>>(CreateInitResponse());
            }

            var packet = _responsePackets.Dequeue();
            OnResponseDequeued?.Invoke(packet);
            return Task.FromResult<ReadOnlyMemory<byte>>(packet);
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