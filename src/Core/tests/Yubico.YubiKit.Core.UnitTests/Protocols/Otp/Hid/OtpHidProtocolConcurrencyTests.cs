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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Transports.Hid;

namespace Yubico.YubiKit.Core.UnitTests.Protocols.Otp.Hid;

/// <summary>
///     Proves and guards the serialization contract of <see cref="OtpHidProtocol" />: an OTP slot
///     command is a 70-byte frame written as ten sequenced 8-byte feature reports followed by a polled
///     multi-report read — the device assembles the frame by sequence number, so a foreign report
///     written mid-frame corrupts the frame (bad CRC at best, a mixed frame accepted at worst).
///     Concurrent operations on one protocol must never interleave feature reports on the wire.
/// </summary>
public class OtpHidProtocolConcurrencyTests
{
    private const byte SlotCommandA = 0x31;
    private const byte SlotCommandB = 0x32;
    private const byte PayloadTagA = 0xAA;
    private const byte PayloadTagB = 0xBB;

    private static readonly TimeSpan ObservationWindow = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan CompletionBound = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task SendAndReceiveAsync_ConcurrentOperations_DoNotInterleaveReportsOnTheWire()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new FakeOtpHidDevice(new FirmwareVersion(5, 8, 0))
        {
            Responder = (slot, _) => CreateResponse(slot switch
            {
                SlotCommandA => (byte)0xA0,
                SlotCommandB => (byte)0xB0,
                _ => (byte)0x00
            })
        };
        using var protocol = new OtpHidProtocol(fake);
        protocol.Configure(new FirmwareVersion(5, 8, 0));

        var payloadA = CreatePayload(PayloadTagA);
        var payloadB = CreatePayload(PayloadTagB);

        // Operation A's first frame report goes out and is held in flight — nine more reports of its
        // frame are still owed to the device.
        fake.HoldSends();
        var operationA = protocol.SendAndReceiveAsync(SlotCommandA, payloadA, ct);
        Assert.True(await fake.WaitForSendsAsync(1, ObservationWindow, ct));

        var operationB = protocol.SendAndReceiveAsync(SlotCommandB, payloadB, ct);
        await fake.WaitForSendsAsync(2, ObservationWindow, ct);

        fake.ReleaseSends();
        var responseA = await operationA.WaitAsync(CompletionBound, ct);
        var responseB = await operationB.WaitAsync(CompletionBound, ct);

        // Both operations must see their own responses...
        Assert.Equal(CreateResponse(0xA0), responseA.ToArray());
        Assert.Equal(CreateResponse(0xB0), responseB.ToArray());

        // ...and all of B's frame reports must land after all of A's on the wire.
        var order = fake.WireTags;
        var lastA = order.FindLastIndex(tag => tag == PayloadTagA);
        var firstB = order.FindIndex(tag => tag == PayloadTagB);
        Assert.True(firstB >= 0);
        Assert.True(
            firstB > lastA,
            $"Operation B's feature report interleaved operation A's 10-report frame (wire tags: {string.Join(", ", order.Select(t => $"0x{t:X2}"))}). " +
            "A real device would assemble a corrupted frame.");
    }

    /// <summary>
    ///     Deadlock guard for the fix's design, not an interleave proof: on NEO (firmware 3.x) the lazy
    ///     initialization path itself issues a slot command (scan-map refresh) from inside the first
    ///     public operation. A naive gate that wraps the public method AND is re-entered by the
    ///     initialization path would deadlock here; the serialized protocol must still complete
    ///     first-use on NEO within the bound.
    /// </summary>
    [Fact]
    public async Task SendAndReceiveAsync_FirstUseOnNeoFirmware_CompletesInitializationQuirkWithoutDeadlock()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new FakeOtpHidDevice(new FirmwareVersion(3, 4, 5))
        {
            Responder = (_, _) => CreateResponse(0xC0)
        };
        using var protocol = new OtpHidProtocol(fake);

        var response = await Task.Run(
                () => protocol.SendAndReceiveAsync(SlotCommandA, CreatePayload(PayloadTagA), ct), ct)
            .WaitAsync(CompletionBound, ct);

        Assert.Equal(CreateResponse(0xC0), response.ToArray());
        Assert.Equal(new FirmwareVersion(3, 4, 5), protocol.FirmwareVersion);
    }

    private static ReadOnlyMemory<byte> CreatePayload(byte tag)
    {
        var payload = new byte[OtpConstants.SlotDataSize];
        Array.Fill(payload, tag);
        return payload;
    }

    private static byte[] CreateResponse(byte tag)
    {
        var response = new byte[OtpConstants.FeatureReportDataSize];
        Array.Fill(response, tag);
        return response;
    }

    /// <summary>
    ///     A minimal OTP HID device model: returns idle status reports (version + programming sequence,
    ///     ready to write), assembles 70-byte frames from sequenced write reports, and serves the
    ///     <see cref="Responder" /> payload as a pending-data report chain. Records the first byte of
    ///     every written report so tests can assert wire order, and can hold writes in flight.
    /// </summary>
    private sealed class FakeOtpHidDevice(FirmwareVersion version) : IOtpHidConnection
    {
        private readonly byte[] _frameBuffer = new byte[OtpConstants.FrameSize];
        private readonly Queue<byte[]> _pendingReads = new();
        private readonly SemaphoreSlim _sendArrivals = new(0);
        private readonly List<byte> _wireTags = [];
        private readonly object _lock = new();
        private volatile TaskCompletionSource? _hold;

        public required Func<byte, byte[], byte[]> Responder { get; init; }

        public List<byte> WireTags
        {
            get
            {
                lock (_lock)
                    return [.. _wireTags];
            }
        }

        public ConnectionType Type => ConnectionType.HidOtp;

        public int FeatureReportSize => OtpConstants.FeatureReportSize;

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

        public async Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default)
        {
            var span = report.Span;
            var statusByte = span[OtpConstants.FeatureReportDataSize];

            lock (_lock)
            {
                _wireTags.Add(span[0]);

                if (statusByte == OtpConstants.DummyReportWrite)
                {
                    // Reset/abort: clear any unread response state.
                    _pendingReads.Clear();
                }
                else if ((statusByte & OtpConstants.SlotWriteFlag) != 0)
                {
                    var sequence = statusByte & OtpConstants.SequenceMask;
                    var offset = sequence * OtpConstants.FeatureReportDataSize;
                    var chunk = Math.Min(OtpConstants.FeatureReportDataSize, OtpConstants.FrameSize - offset);
                    span[..chunk].CopyTo(_frameBuffer.AsSpan(offset));

                    if (offset + chunk >= OtpConstants.FrameSize)
                        ProcessCompleteFrame();
                }
            }

            _sendArrivals.Release();

            var hold = _hold;
            if (hold is not null)
                await hold.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (_pendingReads.Count > 0)
                    return Task.FromResult<ReadOnlyMemory<byte>>(_pendingReads.Dequeue());
            }

            // Idle status: version in bytes 1-3, programming sequence at offset 4, flags clear
            // (ready to write, no response pending).
            var status = new byte[OtpConstants.FeatureReportSize];
            status[1] = version.Major;
            status[2] = version.Minor;
            status[3] = version.Patch;
            return Task.FromResult<ReadOnlyMemory<byte>>(status);
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private void ProcessCompleteFrame()
        {
            var slot = _frameBuffer[OtpConstants.SlotDataSize];
            var payload = _frameBuffer.AsSpan(0, OtpConstants.SlotDataSize).ToArray();
            var responsePayload = Responder(slot, payload);

            // Serve the response as one pending-data report (7 payload bytes, ResponsePendingFlag,
            // sequence 0); the following idle status report ends the chain.
            var dataReport = new byte[OtpConstants.FeatureReportSize];
            responsePayload.AsSpan(0, OtpConstants.FeatureReportDataSize).CopyTo(dataReport);
            dataReport[OtpConstants.FeatureReportDataSize] = OtpConstants.ResponsePendingFlag;
            _pendingReads.Enqueue(dataReport);
        }
    }
}