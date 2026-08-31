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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;

namespace Yubico.YubiKit.Core.UnitTests.Sessions;

/// <summary>
///     A session constructor that fails must not leave its connection permanently refusing new sessions, and
///     a session that is legitimately refused must never disturb the session that already holds the
///     connection. The second property matters more than the first: evicting a live holder would be worse
///     than the stranding defect these tests exist to prevent.
/// </summary>
/// <remarks>
///     Binding happens in <c>ApplicationSession.Construct</c> rather than in the constructor precisely so the
///     failing-constructor case cannot strand a claim: when construction throws, nothing has been bound yet.
///     Found by cross-vendor review (G5, ISA Phase 16 Finding 2).
/// </remarks>
public class SessionConstructionGuardTests
{
    [Fact]
    public void FailingConstructor_LeavesTheConnectionUsable()
    {
        var connection = new StubConnection();

        _ = Assert.Throws<InvalidOperationException>(() => FailingSession.Create(connection));

        // The connection outlives the failed session, so a later session must still be able to bind.
        using var later = OkSession.Create(connection);
        Assert.NotNull(later);
    }

    [Fact]
    public void SecondSession_IsRefused_AndTheFirstKeepsItsClaim()
    {
        var connection = new StubConnection();
        using var first = OkSession.Create(connection);

        _ = Assert.Throws<ConnectionInUseException>(() => OkSession.Create(connection));

        // The refusal must not have evicted the incumbent: disposing it is what frees the connection.
        first.Dispose();
        using var third = OkSession.Create(connection);
        Assert.NotNull(third);
    }

    [Fact]
    public void RefusedSession_IsDisposed_AndTheHolderIsUntouched()
    {
        var connection = new StubConnection();
        using var holder = OkSession.Create(connection);

        _ = Assert.Throws<ConnectionInUseException>(() => TrackingSession.Create(connection));

        // The session we built before being refused must not leak; disposing it must not detach the holder.
        Assert.Equal(1, TrackingSession.LastDisposeCount);
        _ = Assert.Throws<ConnectionInUseException>(() => OkSession.Create(connection));
    }

    [Fact]
    public async Task ConcurrentConstruction_AdmitsExactlyOne_AndTheLoserLeavesTheWinnerIntact()
    {
        var connection = new StubConnection();
        using var barrier = new Barrier(2);
        var sessions = new OkSession?[2];
        var failures = new Exception?[2];

        var racers = Enumerable.Range(0, 2).Select(i => Task.Run(() =>
        {
            barrier.SignalAndWait();
            try
            {
                sessions[i] = OkSession.Create(connection);
            }
            catch (Exception ex)
            {
                failures[i] = ex;
            }
        }));

        await Task.WhenAll(racers);

        Assert.Single(sessions, s => s is not null);
        var loser = Assert.Single(failures, f => f is not null);
        _ = Assert.IsType<ConnectionInUseException>(loser);

        // The winner still holds the connection: the loser's failure path must not have cleared the slot.
        _ = Assert.Throws<ConnectionInUseException>(() => OkSession.Create(connection));

        foreach (var session in sessions)
            session?.Dispose();
    }

    [Fact]
    public async Task SessionThatBypassedConstruct_FailsLoudlyOnInitialize()
    {
        var connection = new StubConnection();

        // Constructed directly, so it never bound. Silently running unguarded is the failure mode this
        // check exists to prevent, so initialization must refuse rather than proceed.
        using var unbound = new OkSession(connection);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => unbound.InitializeForTestAsync());

        Assert.Contains("Construct", ex.Message, StringComparison.Ordinal);
    }

    private sealed class OkSession : ApplicationSession
    {
        internal OkSession(IConnection connection)
            : base(connection)
        {
        }

        public static OkSession Create(IConnection connection)
            => Construct(connection, () => new OkSession(connection));

        public Task InitializeForTestAsync()
            => InitializeProtocolAsync(new StubProtocol(), new FirmwareVersion());
    }

    private sealed class FailingSession : ApplicationSession
    {
        private FailingSession(IConnection connection)
            : base(connection)
            => throw new InvalidOperationException("simulated failure after base construction");

        public static FailingSession Create(IConnection connection)
            => Construct(connection, () => new FailingSession(connection));
    }

    private sealed class TrackingSession : ApplicationSession
    {
        private int _disposeCount;

        private TrackingSession(IConnection connection)
            : base(connection)
        {
        }

        public static int LastDisposeCount { get; private set; }

        public static TrackingSession Create(IConnection connection)
            => Construct(connection, () => new TrackingSession(connection));

        protected override void Dispose(bool disposing)
        {
            LastDisposeCount = Interlocked.Increment(ref _disposeCount);
            base.Dispose(disposing);
        }
    }

    private sealed class StubConnection : IConnection
    {
        public ConnectionType Type => ConnectionType.SmartCard;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class StubProtocol : IProtocol
    {
        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null)
        {
        }

        public void Dispose()
        {
        }
    }
}