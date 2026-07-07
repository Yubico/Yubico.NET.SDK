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

/// <summary>
///     Proves and guards the serialization contract of <see cref="FidoHidProtocol" />: CTAP HID allows
///     one transaction at a time per channel — a request is an init packet plus continuation packets,
///     and a foreign init packet transmitted mid-request aborts the transaction on a real device
///     (CTAP1_ERR_INVALID_SEQ / ERR_CHANNEL_BUSY). Concurrent operations on one protocol must therefore
///     never interleave packets on the wire, and lazy channel initialization must run exactly once.
/// </summary>
public class FidoHidProtocolConcurrencyTests
{
    private const byte VendorCommandA = 0x41;
    private const byte VendorCommandB = 0x42;
    private const byte PayloadTagA = 0xAA;
    private const byte PayloadTagB = 0xBB;

    private static readonly TimeSpan ObservationWindow = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan CompletionBound = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task SendVendorCommandAsync_ConcurrentOperations_DoNotInterleavePacketsOnTheWire()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new FakeCtapHidDevice { Responder = (_, payload) => payload };
        using var protocol = new FidoHidProtocol(fake);
        protocol.Configure(new FirmwareVersion(5, 8, 0));

        // Payloads larger than one init packet (57 bytes) force multi-packet requests and responses.
        var payloadA = CreatePayload(PayloadTagA, 100);
        var payloadB = CreatePayload(PayloadTagB, 100);

        // Operation A's init packet goes out and is held in flight — its continuation packet is still owed.
        fake.HoldSends();
        var operationA = protocol.SendVendorCommandAsync(VendorCommandA, payloadA, ct);
        Assert.True(await fake.WaitForSendsAsync(1, ObservationWindow, ct));

        var operationB = protocol.SendVendorCommandAsync(VendorCommandB, payloadB, ct);
        await fake.WaitForSendsAsync(2, ObservationWindow, ct);

        fake.ReleaseSends();
        var responseA = await operationA.WaitAsync(CompletionBound, ct);
        var responseB = await operationB.WaitAsync(CompletionBound, ct);

        // Both operations must see their own echoed payloads...
        Assert.Equal(payloadA.ToArray(), responseA.ToArray());
        Assert.Equal(payloadB.ToArray(), responseB.ToArray());

        // ...and all of B's packets must land after all of A's packets on the wire.
        var order = fake.WireTags;
        var lastA = order.FindLastIndex(tag => tag is VendorCommandA or PayloadTagA);
        var firstB = order.FindIndex(tag => tag is VendorCommandB or PayloadTagB);
        Assert.True(firstB >= 0);
        Assert.True(
            firstB > lastA,
            $"Operation B's packet interleaved operation A's CTAP HID transaction (wire tags: {string.Join(", ", order.Select(t => $"0x{t:X2}"))}). " +
            "A real device would abort the transaction with ERR_INVALID_SEQ or ERR_CHANNEL_BUSY.");
    }

    [Fact]
    public async Task SendVendorCommandAsync_ConcurrentFirstUse_InitializesChannelExactlyOnce()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new FakeCtapHidDevice { Responder = (_, payload) => payload };
        using var protocol = new FidoHidProtocol(fake);

        var payloadA = CreatePayload(PayloadTagA, 4);
        var payloadB = CreatePayload(PayloadTagB, 4);

        // Channel not yet initialized: both operations race the lazy CTAPHID_INIT handshake.
        fake.HoldSends();
        var operationA = Task.Run(() => protocol.SendVendorCommandAsync(VendorCommandA, payloadA, ct), ct);
        Assert.True(await fake.WaitForSendsAsync(1, ObservationWindow, ct));

        var operationB = Task.Run(() => protocol.SendVendorCommandAsync(VendorCommandB, payloadB, ct), ct);
        await fake.WaitForSendsAsync(2, ObservationWindow, ct);

        fake.ReleaseSends();
        var responseA = await operationA.WaitAsync(CompletionBound, ct);
        var responseB = await operationB.WaitAsync(CompletionBound, ct);

        // Exactly one CTAPHID_INIT exchange may reach the device; a second INIT mid-flight reassigns
        // the channel and cross-delivers nonces.
        Assert.Equal(1, fake.InitExchangeCount);
        Assert.Equal(payloadA.ToArray(), responseA.ToArray());
        Assert.Equal(payloadB.ToArray(), responseB.ToArray());
    }

    [Fact]
    public async Task Configure_RacingFirstUseOperation_InitializesChannelExactlyOnce()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new FakeCtapHidDevice { Responder = (_, payload) => payload };
        using var protocol = new FidoHidProtocol(fake);

        var payloadA = CreatePayload(PayloadTagA, 4);

        // Operation A enters the gate first and starts the lazy CTAPHID_INIT handshake.
        fake.HoldSends();
        var operationA = Task.Run(() => protocol.SendVendorCommandAsync(VendorCommandA, payloadA, ct), ct);
        Assert.True(await fake.WaitForSendsAsync(1, ObservationWindow, ct));

        // Configure() is sync-over-async; pre-fix it initialized the channel outside the gate and
        // raced a second CTAPHID_INIT onto the wire mid-transaction.
        var configure = Task.Run(() => protocol.Configure(new FirmwareVersion(5, 8, 0)), ct);

        // Operation A is parked inside SendAsync with _channelId still unset, so pre-fix Configure
        // unconditionally starts its own ungated INIT — its packet is recorded as a second held send
        // within milliseconds. Post-fix Configure blocks on the gate and nothing reaches the wire.
        var secondInitObserved = await fake.WaitForSendsAsync(2, TimeSpan.FromSeconds(1), ct);
        Assert.False(
            secondInitObserved,
            "Configure raced a second ungated CTAPHID_INIT onto the wire while a first-use operation held the gate mid-INIT.");

        fake.ReleaseSends();
        var responseA = await operationA.WaitAsync(CompletionBound, ct);
        await configure.WaitAsync(CompletionBound, ct);

        Assert.Equal(1, fake.InitExchangeCount);
        Assert.True(protocol.IsChannelInitialized);
        Assert.Equal(payloadA.ToArray(), responseA.ToArray());
    }

    private static ReadOnlyMemory<byte> CreatePayload(byte tag, int length)
    {
        var payload = new byte[length];
        Array.Fill(payload, tag);
        return payload;
    }

    /// <summary>
    ///     A minimal CTAP HID device model: assembles requests from init + continuation packets, echoes
    ///     responses through <see cref="Responder" /> with proper CTAP HID framing, and answers
    ///     CTAPHID_INIT with a nonce echo and a fixed channel. Records the first distinguishing byte of
    ///     every sent packet (command byte for init packets, first payload byte for continuations) so
    ///     tests can assert wire order, and can hold sends in flight.
    /// </summary>
    private sealed class FakeCtapHidDevice : IFidoHidConnection
    {
        private const uint AssignedChannelId = 0x00000001;

        private readonly SemaphoreSlim _responseAvailable = new(0);
        private readonly Queue<byte[]> _responsePackets = new();
        private readonly SemaphoreSlim _sendArrivals = new(0);
        private readonly List<byte> _wireTags = [];
        private readonly object _lock = new();

        private byte[] _requestBuffer = [];
        private int _requestBytesReceived;
        private byte _requestCommand;
        private volatile TaskCompletionSource? _hold;

        public required Func<byte, byte[], byte[]> Responder { get; init; }

        public int InitExchangeCount { get; private set; }

        public List<byte> WireTags
        {
            get
            {
                lock (_lock)
                    return [.. _wireTags];
            }
        }

        public ConnectionType Type => ConnectionType.HidFido;

        public int PacketSize => CtapConstants.PacketSize;

        public void HoldSends() =>
            _hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseSends()
        {
            var hold = _hold;
            _hold = null;
            hold?.SetResult();
        }

        public async Task<bool> WaitForSendsAsync(int count, TimeSpan timeout, CancellationToken ct)
        {
            while (true)
            {
                lock (_lock)
                {
                    if (_wireTags.Count >= count)
                        return true;
                }

                if (!await _sendArrivals.WaitAsync(timeout, ct).ConfigureAwait(false))
                    return false;
            }
        }

        public async Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default)
        {
            var span = packet.Span;
            var isInitPacket = (span[4] & CtapConstants.InitPacketMask) != 0;

            lock (_lock)
            {
                _wireTags.Add(isInitPacket
                    ? (byte)(span[4] & ~CtapConstants.InitPacketMask)
                    : span[CtapConstants.ContinuationHeaderSize]);

                if (isInitPacket)
                {
                    _requestCommand = (byte)(span[4] & ~CtapConstants.InitPacketMask);
                    var length = (span[5] << 8) | span[6];
                    _requestBuffer = new byte[length];
                    var initData = Math.Min(length, CtapConstants.InitDataSize);
                    span.Slice(CtapConstants.InitHeaderSize, initData).CopyTo(_requestBuffer);
                    _requestBytesReceived = initData;
                }
                else
                {
                    var chunk = Math.Min(
                        _requestBuffer.Length - _requestBytesReceived,
                        CtapConstants.ContinuationDataSize);
                    span.Slice(CtapConstants.ContinuationHeaderSize, chunk)
                        .CopyTo(_requestBuffer.AsSpan(_requestBytesReceived));
                    _requestBytesReceived += chunk;
                }

                if (_requestBytesReceived >= _requestBuffer.Length)
                    ProcessCompleteRequest();
            }

            _sendArrivals.Release();

            var hold = _hold;
            if (hold is not null)
                await hold.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            await _responseAvailable.WaitAsync(cancellationToken).ConfigureAwait(false);
            lock (_lock)
                return _responsePackets.Dequeue();
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private void ProcessCompleteRequest()
        {
            byte[] responsePayload;
            if (_requestCommand == CtapConstants.CtapHidInit)
            {
                InitExchangeCount++;
                responsePayload = new byte[17];
                _requestBuffer.AsSpan(0, CtapConstants.NonceSize).CopyTo(responsePayload);
                BinaryPrimitives.WriteUInt32BigEndian(responsePayload.AsSpan(8), AssignedChannelId);
                responsePayload[12] = 2;
                responsePayload[13] = 5;
                responsePayload[14] = 8;
                responsePayload[15] = 0;
            }
            else
            {
                responsePayload = Responder(_requestCommand, _requestBuffer);
            }

            EnqueueResponse(_requestCommand, responsePayload);
        }

        private void EnqueueResponse(byte command, byte[] payload)
        {
            var initPacket = new byte[CtapConstants.PacketSize];
            var channel = command == CtapConstants.CtapHidInit ? CtapConstants.BroadcastChannelId : AssignedChannelId;
            BinaryPrimitives.WriteUInt32BigEndian(initPacket, channel);
            initPacket[4] = (byte)(command | CtapConstants.InitPacketMask);
            initPacket[5] = (byte)(payload.Length >> 8);
            initPacket[6] = (byte)(payload.Length & 0xFF);
            var initData = Math.Min(payload.Length, CtapConstants.InitDataSize);
            payload.AsSpan(0, initData).CopyTo(initPacket.AsSpan(CtapConstants.InitHeaderSize));
            _responsePackets.Enqueue(initPacket);
            _responseAvailable.Release();

            var offset = initData;
            byte sequence = 0;
            while (offset < payload.Length)
            {
                var continuationPacket = new byte[CtapConstants.PacketSize];
                BinaryPrimitives.WriteUInt32BigEndian(continuationPacket, channel);
                continuationPacket[4] = sequence;
                var chunk = Math.Min(payload.Length - offset, CtapConstants.ContinuationDataSize);
                payload.AsSpan(offset, chunk).CopyTo(continuationPacket.AsSpan(CtapConstants.ContinuationHeaderSize));
                _responsePackets.Enqueue(continuationPacket);
                _responseAvailable.Release();
                offset += chunk;
                sequence++;
            }
        }
    }
}