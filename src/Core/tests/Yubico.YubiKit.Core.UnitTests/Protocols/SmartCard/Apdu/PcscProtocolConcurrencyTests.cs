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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu;

/// <summary>
///     Proves and guards the serialization contract of <see cref="PcscProtocol" />: two concurrent
///     operations on one protocol must never interleave APDUs on the shared connection. A smart card is a
///     stateful sequential peer — a foreign command transmitted in the middle of another operation's
///     chained-response exchange (SW1=0x61 → SEND REMAINING) destroys the pending response data; the same
///     holds for command chaining and SCP MAC chaining.
/// </summary>
public class PcscProtocolConcurrencyTests
{
    private const byte InsOperationA = 0xA1;
    private const byte InsOperationB = 0xB2;
    private const byte InsSendRemaining = 0xC0;

    private static readonly TimeSpan ObservationWindow = TimeSpan.FromMilliseconds(300);

    [Fact]
    public async Task TransmitAndReceiveAsync_ConcurrentOperations_DoNotInterleaveApdusOnTheWire()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new HoldingFakeConnection
        {
            Responder = ins => ins switch
            {
                // Operation A: two-part chained response (61 02 = more data available).
                InsOperationA => new byte[] { 0xDE, 0xAD, 0x61, 0x02 },
                InsSendRemaining => new byte[] { 0xBE, 0xEF, 0x90, 0x00 },
                // Operation B: single-part response.
                InsOperationB => new byte[] { 0xCA, 0xFE, 0x90, 0x00 },
                _ => throw new InvalidOperationException($"Unexpected INS 0x{ins:X2}")
            }
        };
        var protocol = new PcscProtocol(fake);

        // Start operation A and hold its first transmit in flight — mid-exchange, with SEND REMAINING
        // still owed to the card.
        fake.HoldTransmits();
        var operationA = protocol.TransmitAndReceiveAsync(new ApduCommand { Ins = InsOperationA }, true, ct);
        Assert.True(await fake.WaitForArrivalsAsync(1, ObservationWindow, ct));

        // Start operation B while A's exchange is open.
        var operationB = protocol.TransmitAndReceiveAsync(new ApduCommand { Ins = InsOperationB }, true, ct);
        await fake.WaitForArrivalsAsync(2, ObservationWindow, ct);

        // Let everything flow and both operations complete.
        fake.ReleaseTransmits();
        var responseA = await operationA.WaitAsync(TimeSpan.FromSeconds(5), ct);
        var responseB = await operationB.WaitAsync(TimeSpan.FromSeconds(5), ct);

        // Both operations must see their own, correctly assembled responses...
        Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, responseA.Data.ToArray());
        Assert.Equal(new byte[] { 0xCA, 0xFE }, responseB.Data.ToArray());

        // ...and operation B's APDU must not have hit the wire inside operation A's exchange.
        var order = fake.WireOrder;
        var firstA = order.IndexOf(InsOperationA);
        var sendRemaining = order.IndexOf(InsSendRemaining);
        var firstB = order.IndexOf(InsOperationB);
        Assert.False(
            firstA < firstB && firstB < sendRemaining,
            $"Operation B's APDU interleaved operation A's chained-response exchange (wire order: {string.Join(", ", order.Select(i => $"0x{i:X2}"))}). " +
            "A real card would have discarded A's pending response data.");
    }

    /// <summary>
    ///     The SCP wrapper bypasses the base protocol's public methods and drives the same connection
    ///     through its own processor chain — it must therefore share the base protocol's gate, or gated
    ///     plain traffic and SCP traffic interleave on the wire.
    /// </summary>
    [Fact]
    public async Task ScpWrapper_SharesGateWithBaseProtocol_PlainAndScpTrafficDoNotInterleave()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new HoldingFakeConnection
        {
            Responder = ins => ins switch
            {
                InsOperationA => new byte[] { 0xDE, 0xAD, 0x61, 0x02 },
                InsSendRemaining => new byte[] { 0xBE, 0xEF, 0x90, 0x00 },
                InsOperationB => new byte[] { 0xCA, 0xFE, 0x90, 0x00 },
                _ => throw new InvalidOperationException($"Unexpected INS 0x{ins:X2}")
            }
        };
        var baseProtocol = new PcscProtocol(fake);
        var scpProtocol = new PcscProtocolScp(
            baseProtocol,
            new ApduTransmitter(fake, new ApduFormatterExtended(SmartCardMaxApduSizes.Yk43)),
            data => data.ToArray());

        fake.HoldTransmits();
        var plainOperation = baseProtocol.TransmitAndReceiveAsync(new ApduCommand { Ins = InsOperationA }, true, ct);
        Assert.True(await fake.WaitForArrivalsAsync(1, ObservationWindow, ct));

        var scpOperation = scpProtocol.TransmitAndReceiveAsync(new ApduCommand { Ins = InsOperationB }, true, ct);
        await fake.WaitForArrivalsAsync(2, ObservationWindow, ct);

        fake.ReleaseTransmits();
        await plainOperation.WaitAsync(TimeSpan.FromSeconds(5), ct);
        await scpOperation.WaitAsync(TimeSpan.FromSeconds(5), ct);

        var order = fake.WireOrder;
        var firstA = order.IndexOf(InsOperationA);
        var sendRemaining = order.IndexOf(InsSendRemaining);
        var firstB = order.IndexOf(InsOperationB);
        Assert.True(firstB >= 0);
        Assert.False(
            firstA < firstB && firstB < sendRemaining,
            $"SCP traffic interleaved the base protocol's chained-response exchange (wire order: {string.Join(", ", order.Select(i => $"0x{i:X2}"))}).");
    }

    /// <summary>A failed exchange must release the gate — the protocol must not wedge.</summary>
    [Fact]
    public async Task TransmitAndReceiveAsync_ExchangeThrows_GateIsReleasedForNextOperation()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new HoldingFakeConnection
        {
            Responder = ins => ins switch
            {
                InsOperationA => throw new InvalidOperationException("Simulated transport failure."),
                InsOperationB => new byte[] { 0x90, 0x00 },
                _ => throw new InvalidOperationException($"Unexpected INS 0x{ins:X2}")
            }
        };
        var protocol = new PcscProtocol(fake);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => protocol.TransmitAndReceiveAsync(new ApduCommand { Ins = InsOperationA }, true, ct));

        // A wedged gate would hang here; the bounded wait turns that into a failure.
        var response = await protocol
            .TransmitAndReceiveAsync(new ApduCommand { Ins = InsOperationB }, true, ct)
            .WaitAsync(TimeSpan.FromSeconds(2), ct);
        Assert.True(response.IsOK());
    }

    /// <summary>
    ///     Cancellation must gate entry only: a token canceled after an exchange has entered the gate
    ///     must not abort the exchange between its constituent transmits. Aborting mid-exchange would
    ///     release the gate while the card still owes chained-response data, poisoning the next operation.
    /// </summary>
    [Fact]
    public async Task TransmitAndReceiveAsync_CancelledMidExchange_ExchangeRunsToCompletionWithoutInterleaving()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new HoldingFakeConnection
        {
            Responder = ins => ins switch
            {
                InsOperationA => new byte[] { 0xDE, 0xAD, 0x61, 0x02 },
                InsSendRemaining => new byte[] { 0xBE, 0xEF, 0x90, 0x00 },
                InsOperationB => new byte[] { 0xCA, 0xFE, 0x90, 0x00 },
                _ => throw new InvalidOperationException($"Unexpected INS 0x{ins:X2}")
            }
        };
        var protocol = new PcscProtocol(fake);

        // Operation A enters the gate and is held mid-exchange (SEND REMAINING still owed).
        using var operationACancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        fake.HoldTransmits();
        var operationA = protocol.TransmitAndReceiveAsync(
            new ApduCommand { Ins = InsOperationA }, true, operationACancellation.Token);
        Assert.True(await fake.WaitForArrivalsAsync(1, ObservationWindow, ct));

        // Cancel A's token while its exchange is in flight, with a second operation queued behind it.
        await operationACancellation.CancelAsync();
        var operationB = protocol.TransmitAndReceiveAsync(new ApduCommand { Ins = InsOperationB }, true, ct);

        fake.ReleaseTransmits();

        // A must complete its full exchange (not observe the cancellation) with intact data...
        var responseA = await operationA.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, responseA.Data.ToArray());
        await operationB.WaitAsync(TimeSpan.FromSeconds(5), ct);

        // ...and the wire must show A's exchange atomic: SEND REMAINING before B's first APDU.
        Assert.Equal(new byte[] { InsOperationA, InsSendRemaining, InsOperationB }, fake.WireOrder);
    }

    /// <summary>
    ///     The flip side of the entry-only cancellation contract: a caller canceled while still waiting
    ///     for its turn must throw and never reach the wire, and the gate must stay usable.
    /// </summary>
    [Fact]
    public async Task TransmitAndReceiveAsync_CancelledWhileWaitingForGate_ThrowsWithoutTransmitting()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new HoldingFakeConnection
        {
            Responder = ins => ins switch
            {
                InsOperationA => new byte[] { 0xDE, 0xAD, 0x61, 0x02 },
                InsSendRemaining => new byte[] { 0xBE, 0xEF, 0x90, 0x00 },
                InsOperationB => new byte[] { 0xCA, 0xFE, 0x90, 0x00 },
                _ => throw new InvalidOperationException($"Unexpected INS 0x{ins:X2}")
            }
        };
        var protocol = new PcscProtocol(fake);

        fake.HoldTransmits();
        var operationA = protocol.TransmitAndReceiveAsync(new ApduCommand { Ins = InsOperationA }, true, ct);
        Assert.True(await fake.WaitForArrivalsAsync(1, ObservationWindow, ct));

        // Operation B queues behind A's held exchange, then is canceled while waiting.
        using var operationBCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var operationB = protocol.TransmitAndReceiveAsync(
            new ApduCommand { Ins = InsOperationB }, true, operationBCancellation.Token);
        await operationBCancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operationB);

        fake.ReleaseTransmits();
        await operationA.WaitAsync(TimeSpan.FromSeconds(5), ct);

        // B never touched the wire, and the gate is free for the next operation.
        Assert.DoesNotContain(InsOperationB, fake.WireOrder);
        var nextOperation = await protocol
            .TransmitAndReceiveAsync(new ApduCommand { Ins = InsOperationB }, true, ct)
            .WaitAsync(TimeSpan.FromSeconds(2), ct);
        Assert.True(nextOperation.IsOK());
    }

    /// <summary>
    ///     Records the INS of every transmitted APDU in wire order and can hold transmits in flight so the
    ///     test controls when responses flow. Responses are routed by INS, modeling a well-behaved
    ///     sequential card.
    /// </summary>
    private sealed class HoldingFakeConnection : ISmartCardConnection
    {
        private readonly SemaphoreSlim _arrivals = new(0);
        private readonly List<byte> _wireOrder = [];
        private volatile TaskCompletionSource? _hold;

        public required Func<byte, ReadOnlyMemory<byte>> Responder { get; init; }

        public List<byte> WireOrder
        {
            get
            {
                lock (_wireOrder)
                    return [.. _wireOrder];
            }
        }

        public ConnectionType Type => ConnectionType.SmartCard;

        public Transport Transport => Transport.Usb;

        public void HoldTransmits() =>
            _hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseTransmits()
        {
            var hold = _hold;
            _hold = null;
            hold?.SetResult();
        }

        /// <summary>Waits until at least <paramref name="count" /> transmits have arrived on the wire.</summary>
        public async Task<bool> WaitForArrivalsAsync(int count, TimeSpan timeout, CancellationToken ct)
        {
            while (true)
            {
                lock (_wireOrder)
                {
                    if (_wireOrder.Count >= count)
                        return true;
                }

                if (!await _arrivals.WaitAsync(timeout, ct).ConfigureAwait(false))
                    return false;
            }
        }

        public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default)
        {
            var ins = command.Span[1];
            lock (_wireOrder)
                _wireOrder.Add(ins);

            _arrivals.Release();

            var hold = _hold;
            if (hold is not null)
                await hold.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

            return Responder(ins);
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool SupportsExtendedApdu() => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}