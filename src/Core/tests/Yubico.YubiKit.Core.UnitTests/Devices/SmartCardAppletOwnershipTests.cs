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
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
///     The CCID applet-ownership contract: a YubiKey's smart-card interface holds one applet, and selecting
///     a different one deselects the previous applet outright — the clobbered session then answers
///     SW=0x6D00, while the SELECT that caused it reports success. Measured on hardware; see
///     <c>docs/plans/session-contention/phase1-findings.md</c>.
/// </summary>
/// <remarks>
///     These drive the real <see cref="PcscProtocol" /> over wrapped connections instead of hand-rolling
///     SELECT bytes, so they pin that the seam recognizes what the protocol actually emits — including the
///     extended-APDU encoding used on USB.
/// </remarks>
public class SmartCardAppletOwnershipTests
{
    private static readonly byte[] SelectOk = [0x90, 0x00];

    /// <summary>SW=0x6A82, "file or application not found" — a SELECT the card refused to perform.</summary>
    private static readonly byte[] SelectNotFound = [0x6A, 0x82];

    /// <summary>
    ///     How many times the Dispose/SelectApplet race is replayed. The race window is a few instructions
    ///     wide, so a single attempt would not reliably expose it; see the test for why the passing
    ///     direction is nevertheless schedule-independent.
    /// </summary>
    private const int RaceAttempts = 3000;

    private static string NewInterfaceId() => $"test:{Guid.NewGuid():N}";

    /// <summary>
    ///     FIX EVIDENCE (hardware experiments 1 and 2). Two connections on one interface, the second
    ///     selecting a different applet: the SELECT must be refused before it reaches the card, naming both
    ///     applets and the interface.
    /// </summary>
    [Fact]
    public async Task SecondConnection_SelectingDifferentApplet_ThrowsAndTransmitsNothing()
    {
        var interfaceId = NewInterfaceId();
        await using var piv = await OpenAsync(interfaceId);
        await using var oath = await OpenAsync(interfaceId);

        await piv.SelectAsync(ApplicationIds.Piv);

        var conflict = await Assert.ThrowsAsync<SmartCardAppletConflictException>(
            () => oath.SelectAsync(ApplicationIds.Oath));

        Assert.Equal(interfaceId, conflict.InterfaceId);
        Assert.Equal(ApplicationIds.Piv, conflict.HeldApplicationId.ToArray());
        Assert.Equal(ApplicationIds.Oath, conflict.RequestedApplicationId.ToArray());
        Assert.Single(piv.Wire.TransmittedCommands); // the holder's own SELECT
        Assert.Empty(oath.Wire.TransmittedCommands); // refused before the wire — the holder is intact
    }

    /// <summary>
    ///     INVARIANT PIN, not fix evidence — this passed before the applet rule existed and must keep
    ///     passing. Same-applet nesting is safe on hardware (experiment 3: the first session's verified PIN
    ///     survives a second PIV SELECT), so the rule ref-counts rather than excluding.
    /// </summary>
    [Fact]
    public async Task SecondConnection_SelectingSameApplet_Succeeds()
    {
        var interfaceId = NewInterfaceId();
        await using var first = await OpenAsync(interfaceId);
        await using var second = await OpenAsync(interfaceId);

        await first.SelectAsync(ApplicationIds.Piv);
        await second.SelectAsync(ApplicationIds.Piv);

        Assert.Single(first.Wire.TransmittedCommands);
        Assert.Single(second.Wire.TransmittedCommands);
    }

    /// <summary>
    ///     INVARIANT PIN, not fix evidence. The rule must not over-reach: separate interfaces are separate
    ///     applet selections (experiment 4 — an unrelated interface is unaffected by a held CCID applet).
    /// </summary>
    [Fact]
    public async Task DifferentInterfaces_DifferentApplets_BothSucceed()
    {
        await using var piv = await OpenAsync(NewInterfaceId());
        await using var oath = await OpenAsync(NewInterfaceId());

        await piv.SelectAsync(ApplicationIds.Piv);
        await oath.SelectAsync(ApplicationIds.Oath);

        Assert.Single(piv.Wire.TransmittedCommands);
        Assert.Single(oath.Wire.TransmittedCommands);
    }

    /// <summary>
    ///     A connection that is the interface's only holder may switch its own applet: it can disturb
    ///     nothing but itself. YubiOtp's SmartCard init depends on this — it SELECTs Management to read a
    ///     version, then SELECTs OTP on the same connection.
    /// </summary>
    [Fact]
    public async Task SoleConnection_SwitchingItsOwnApplet_Succeeds()
    {
        await using var session = await OpenAsync(NewInterfaceId());

        await session.SelectAsync(ApplicationIds.Management);
        await session.SelectAsync(ApplicationIds.Otp);

        Assert.Equal(2, session.Wire.TransmittedCommands.Count);
    }

    /// <summary>
    ///     Releasing the holder frees the applet. Without this the rule would leak: one PIV connection would
    ///     poison the interface for the rest of the process lifetime.
    /// </summary>
    [Fact]
    public async Task DisposingTheHolder_FreesTheInterfaceForAnotherApplet()
    {
        var interfaceId = NewInterfaceId();
        var piv = await OpenAsync(interfaceId);
        await piv.SelectAsync(ApplicationIds.Piv);
        await piv.DisposeAsync();

        await using var oath = await OpenAsync(interfaceId);
        await oath.SelectAsync(ApplicationIds.Oath);

        Assert.Single(oath.Wire.TransmittedCommands);
    }

    /// <summary>
    ///     Ref-counted holders are not released by the first departure: while a second connection still
    ///     relies on PIV, a third must still be refused OATH.
    /// </summary>
    [Fact]
    public async Task DisposingOneOfTwoSameAppletHolders_KeepsTheAppletClaimed()
    {
        var interfaceId = NewInterfaceId();
        var first = await OpenAsync(interfaceId);
        await using var second = await OpenAsync(interfaceId);

        await first.SelectAsync(ApplicationIds.Piv);
        await second.SelectAsync(ApplicationIds.Piv);
        await first.DisposeAsync();

        await using var oath = await OpenAsync(interfaceId);

        _ = await Assert.ThrowsAsync<SmartCardAppletConflictException>(
            () => oath.SelectAsync(ApplicationIds.Oath));
        Assert.Empty(oath.Wire.TransmittedCommands);
    }

    /// <summary>
    ///     A sole holder may switch applets, but not while another connection depends on the current one —
    ///     the switch would deselect the applet that other connection is using.
    /// </summary>
    [Fact]
    public async Task HolderSwitchingApplet_WhileAnotherHolderShares_IsRefused()
    {
        var interfaceId = NewInterfaceId();
        await using var first = await OpenAsync(interfaceId);
        await using var second = await OpenAsync(interfaceId);

        await first.SelectAsync(ApplicationIds.Piv);
        await second.SelectAsync(ApplicationIds.Piv);

        _ = await Assert.ThrowsAsync<SmartCardAppletConflictException>(
            () => first.SelectAsync(ApplicationIds.Oath));
        Assert.Single(first.Wire.TransmittedCommands);
    }

    /// <summary>
    ///     The seam reads the AID off the wire, so it must recognize the short encoding too — NFC readers
    ///     and pre-4.0 keys do not get extended APDUs.
    /// </summary>
    [Fact]
    public async Task ConflictIsDetected_WhenTheSelectUsesShortApdus()
    {
        var interfaceId = NewInterfaceId();
        await using var piv = await OpenAsync(interfaceId, supportsExtendedApdu: false);
        await using var oath = await OpenAsync(interfaceId, supportsExtendedApdu: false);

        await piv.SelectAsync(ApplicationIds.Piv);

        _ = await Assert.ThrowsAsync<SmartCardAppletConflictException>(
            () => oath.SelectAsync(ApplicationIds.Oath));
        Assert.Empty(oath.Wire.TransmittedCommands);
    }

    /// <summary>
    ///     Non-SELECT traffic must be untouched by the rule, including commands that reuse INS 0xA4 for
    ///     their own purposes — OATH CALCULATE ALL does exactly that.
    /// </summary>
    [Fact]
    public async Task NonSelectTraffic_IsNotTreatedAsAnAppletClaim()
    {
        var interfaceId = NewInterfaceId();
        await using var oath = await OpenAsync(interfaceId);
        await using var other = await OpenAsync(interfaceId);

        await oath.SelectAsync(ApplicationIds.Oath);

        // OATH CALCULATE ALL: INS 0xA4, but P1 is 0x00, so it is not a SELECT-by-DF-name.
        other.Wire.EnqueueResponse(SelectOk);
        await other.Protocol.TransmitAndReceiveAsync(
            new ApduCommand(0x00, 0xA4, 0x00, 0x01, new byte[] { 0x74, 0x04 }),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(other.Wire.TransmittedCommands);
    }

    /// <summary>
    ///     A SELECT issued through a released lease must be rejected rather than claimed. A claim with no
    ///     remaining owner to release it would poison the interface against every other applet for the rest
    ///     of the process lifetime.
    /// </summary>
    [Fact]
    public async Task SelectThroughAReleasedLease_DoesNotClaimTheInterface()
    {
        var interfaceId = NewInterfaceId();
        var lease = await DeviceConnectionRegistry.AcquireSessionAsync(
            interfaceId, TestContext.Current.CancellationToken);
        lease.Dispose();

        Assert.Throws<ObjectDisposedException>(() => lease.SelectApplet(ApplicationIds.Piv));

        await using var oath = await OpenAsync(interfaceId);
        await oath.SelectAsync(ApplicationIds.Oath);

        Assert.Single(oath.Wire.TransmittedCommands);
    }

    /// <summary>
    ///     FIX EVIDENCE (HIGH 2 — transport fault). The claim is recorded before the SELECT reaches the
    ///     wire, which is what makes a refusal harmless. When the transmit throws, the card never changed
    ///     application, so the claim must be given back: otherwise the registry records OATH while the card
    ///     still has PIV, a second OATH lease is waved through, and its SELECT silently deselects the PIV
    ///     the first connection is still on — the original defect, recreated by the fix.
    /// </summary>
    [Fact]
    public async Task SwitchingApplets_WhenTheTransmitThrows_GivesThePreviousAppletBack()
    {
        var interfaceId = NewInterfaceId();
        await using var piv = await OpenAsync(interfaceId);
        await piv.SelectAsync(ApplicationIds.Piv);

        piv.Wire.OnTransmit = _ => throw new IOException("the reader went away mid-SELECT");
        _ = await Assert.ThrowsAsync<IOException>(() => piv.SelectAsync(ApplicationIds.Oath));
        piv.Wire.OnTransmit = null;

        await using var oath = await OpenAsync(interfaceId);
        var conflict = await Assert.ThrowsAsync<SmartCardAppletConflictException>(
            () => oath.SelectAsync(ApplicationIds.Oath));

        Assert.Equal(ApplicationIds.Piv, conflict.HeldApplicationId.ToArray());
        Assert.Empty(oath.Wire.TransmittedCommands);
    }

    /// <summary>
    ///     FIX EVIDENCE (HIGH 2 — cancellation). Same reconciliation, reached through the other way an
    ///     in-flight exchange ends without an answer.
    /// </summary>
    [Fact]
    public async Task SwitchingApplets_WhenTheTransmitIsCancelled_GivesThePreviousAppletBack()
    {
        var interfaceId = NewInterfaceId();
        await using var piv = await OpenAsync(interfaceId);
        await piv.SelectAsync(ApplicationIds.Piv);

        piv.Wire.OnTransmit = _ => throw new OperationCanceledException();
        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => piv.SelectAsync(ApplicationIds.Oath));
        piv.Wire.OnTransmit = null;

        await using var oath = await OpenAsync(interfaceId);

        _ = await Assert.ThrowsAsync<SmartCardAppletConflictException>(
            () => oath.SelectAsync(ApplicationIds.Oath));
    }

    /// <summary>
    ///     FIX EVIDENCE (HIGH 2 — error status word). A SELECT that answers with a non-success SW did not
    ///     change the card's current application (ISO 7816-4: a checking error leaves the previous
    ///     selection in place), so the registry must agree with the card and keep the previous claim.
    /// </summary>
    [Fact]
    public async Task SwitchingApplets_WhenTheCardRefusesTheSelect_GivesThePreviousAppletBack()
    {
        var interfaceId = NewInterfaceId();
        await using var piv = await OpenAsync(interfaceId);
        await piv.SelectAsync(ApplicationIds.Piv);

        _ = await Assert.ThrowsAsync<ApduException>(() => piv.SelectAsync(ApplicationIds.Oath, SelectNotFound));

        await using var oath = await OpenAsync(interfaceId);
        var conflict = await Assert.ThrowsAsync<SmartCardAppletConflictException>(
            () => oath.SelectAsync(ApplicationIds.Oath));

        Assert.Equal(ApplicationIds.Piv, conflict.HeldApplicationId.ToArray());
        Assert.Empty(oath.Wire.TransmittedCommands);
    }

    /// <summary>
    ///     The mirror of the above, and the reason reconciliation cannot simply drop the claim: a failed
    ///     first SELECT leaves the interface with nothing selected by anyone, so the next lease must be
    ///     free to take any applet. Reverting has to restore the previous state, not a fixed one.
    /// </summary>
    [Fact]
    public async Task FirstSelectOnAnInterface_WhenItFails_LeavesTheInterfaceFree()
    {
        var interfaceId = NewInterfaceId();
        await using var piv = await OpenAsync(interfaceId);

        _ = await Assert.ThrowsAsync<ApduException>(() => piv.SelectAsync(ApplicationIds.Piv, SelectNotFound));

        await using var oath = await OpenAsync(interfaceId);
        await oath.SelectAsync(ApplicationIds.Oath);

        Assert.Single(oath.Wire.TransmittedCommands);
    }

    /// <summary>
    ///     A failed SELECT must not roll the interface back past a selection someone else has since made
    ///     real on the card. While this lease's switch to OATH was in flight another lease joined OATH and
    ///     its SELECT succeeded, so the card is on OATH; reverting to PIV here would let a later PIV lease
    ///     through as a same-applet join and clobber the OATH holder — the mirror-image of HIGH 2.
    /// </summary>
    [Fact]
    public async Task FailedSwitch_DoesNotRevertPastAnAppletAnotherLeaseHasSinceTaken()
    {
        var interfaceId = NewInterfaceId();
        await using var switcher = await OpenAsync(interfaceId);
        await using var joiner = await OpenAsync(interfaceId);
        await switcher.SelectAsync(ApplicationIds.Piv);

        // While the switch to OATH is on the wire, another lease joins OATH and its SELECT succeeds.
        switcher.Wire.OnTransmit = async _ =>
        {
            switcher.Wire.OnTransmit = null;
            await joiner.SelectAsync(ApplicationIds.Oath);
            throw new IOException("the switcher's own SELECT never came back");
        };

        _ = await Assert.ThrowsAsync<IOException>(() => switcher.SelectAsync(ApplicationIds.Oath));

        await using var latecomer = await OpenAsync(interfaceId);
        var conflict = await Assert.ThrowsAsync<SmartCardAppletConflictException>(
            () => latecomer.SelectAsync(ApplicationIds.Piv));

        Assert.Equal(ApplicationIds.Oath, conflict.HeldApplicationId.ToArray());
    }

    /// <summary>
    ///     INVARIANT PIN, not fix evidence. Reconciling a failed SELECT is a state mutation that happens
    ///     after an await, so it can land on a lease that was disposed while its SELECT was on the wire.
    ///     Disposal has already handed the applet back by then, and the reconcile must not undo that a
    ///     second time or hand back a claim nobody is left to release.
    /// </summary>
    [Fact]
    public async Task FailedSelect_OnALeaseDisposedWhileInFlight_LeavesTheInterfaceFree()
    {
        var interfaceId = NewInterfaceId();
        var piv = await OpenAsync(interfaceId);
        await piv.SelectAsync(ApplicationIds.Piv);

        piv.Wire.OnTransmit = async _ =>
        {
            await piv.DisposeAsync();
            throw new IOException("the connection was closed under the in-flight SELECT");
        };
        _ = await Assert.ThrowsAsync<IOException>(() => piv.SelectAsync(ApplicationIds.Oath));

        await using var oath = await OpenAsync(interfaceId);
        await oath.SelectAsync(ApplicationIds.Oath);

        Assert.Single(oath.Wire.TransmittedCommands);
    }

    /// <summary>
    ///     FIX EVIDENCE (HIGH 1). <c>Dispose</c> and <c>SelectApplet</c> race on one lease. The
    ///     released-lease guard has to be evaluated inside the lock that protects the applet state it
    ///     guards; read outside it, a whole <c>Dispose</c> — flag, lock, release, unlock — fits in the
    ///     window before the claim lands, and the interface is left holding an applet for a lease that no
    ///     longer exists to release it, poisoning it against every other applet for the process lifetime.
    /// </summary>
    /// <remarks>
    ///     The window is a few instructions wide, so reproducing the defect needs repetition — this test is
    ///     probabilistic in the RED direction. It is not flaky in the GREEN direction: once the guard is
    ///     read under the lock, the two operations are totally ordered, and both orders (claim then
    ///     release, or release then <see cref="ObjectDisposedException" />) end with the interface free. No
    ///     schedule exists that fails it.
    /// </remarks>
    [Fact]
    public async Task SelectRacingDisposeOnOneLease_NeverLeavesAnOwnerlessClaim()
    {
        for (var attempt = 0; attempt < RaceAttempts; attempt++)
        {
            var interfaceId = NewInterfaceId();
            var lease = await DeviceConnectionRegistry.AcquireSessionAsync(
                interfaceId, TestContext.Current.CancellationToken);

            using var start = new Barrier(2);
            await Task.WhenAll(
                Task.Run(
                    () =>
                    {
                        start.SignalAndWait();
                        try
                        {
                            lease.SelectApplet(ApplicationIds.Piv);
                        }
                        catch (ObjectDisposedException)
                        {
                            // Lost the race to Dispose. That is a correct outcome; the claim never happened.
                        }
                    },
                    TestContext.Current.CancellationToken),
                Task.Run(
                    () =>
                    {
                        start.SignalAndWait();
                        lease.Dispose();
                    },
                    TestContext.Current.CancellationToken));

            // Whichever order won, no live lease holds anything, so the interface must be free.
            var next = await DeviceConnectionRegistry.AcquireSessionAsync(
                interfaceId, TestContext.Current.CancellationToken);
            next.SelectApplet(ApplicationIds.Oath);
            next.Dispose();
        }
    }

    private static async Task<AppletSession> OpenAsync(string interfaceId, bool supportsExtendedApdu = true)
    {
        var wire = new FakeSmartCardConnection { SupportsExtendedApduValue = supportsExtendedApdu };
        var lease = await DeviceConnectionRegistry.AcquireSessionAsync(
            interfaceId, TestContext.Current.CancellationToken);
        var connection = new RegisteredSmartCardConnection(wire, lease);

        return new AppletSession(wire, PcscProtocolFactory<ISmartCardConnection>.Create().Create(connection));
    }

    /// <summary>One open connection on an interface, plus the fake wire behind it.</summary>
    private sealed class AppletSession(FakeSmartCardConnection wire, ISmartCardProtocol protocol) : IAsyncDisposable
    {
        public FakeSmartCardConnection Wire => wire;

        public ISmartCardProtocol Protocol => protocol;

        public Task SelectAsync(ReadOnlyMemory<byte> applicationId) => SelectAsync(applicationId, SelectOk);

        /// <summary>Selects <paramref name="applicationId" /> with <paramref name="response" /> on the wire.</summary>
        public Task SelectAsync(ReadOnlyMemory<byte> applicationId, byte[] response)
        {
            wire.EnqueueResponse(response);
            return protocol.SelectAsync(applicationId, TestContext.Current.CancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            protocol.Dispose(); // disposes the registered connection, releasing the lease
            return ValueTask.CompletedTask;
        }
    }
}