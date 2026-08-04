---
task: Session-vs-session contention on multi-interface YubiKey applets
branch: yubikit-session-contention
base: cb9ca41f (composite-merge HEAD; becomes yubikit via PR #543)
phase: investigate
date: 2026-07-30
---

# ISA — Session Contention

## The problem in three lines

```csharp
await using var piv = await key.CreatePivSessionAsync();  // CCID handle #1, SELECT PIV
await piv.VerifyPinAsync(pin);                            // security state established
var info = await key.GetDeviceInfoAsync();                // CCID handle #2, SELECT Management
                                                          //   -> PIV deselected, PIN destroyed
await piv.SignAsync(...);                                 // expected failure
```

Ordinary public API, default settings, one process, no exotic conditions. `GetDeviceInfoAsync`
is a plain read whose default transport order puts SmartCard first, so it takes CCID and issues
`SELECT Management`. On the card's basic logical channel that deselects PIV and destroys the
verified-PIN state.

## Why it exists

`DeviceConnectionRegistry` excludes **discovery** from everything and excludes **sessions** from
nothing. Session leases are ref-counted (N sessions coexist); the discovery lease is exclusive.
Two in-process sessions on one interface get two `SCardConnect` handles and two protocol
instances, so the per-protocol `AsyncExchangeGate` does not serialize them.

The registry was built for the single-interface, PIV-shaped world where discovery is the only
other actor. Multi-interface applets — Management (`[SmartCard, HidFido, HidOtp]`), YubiOTP,
FIDO2 — arrived afterwards. The registry's docs are explicit about the **process** boundary and
silent about the **session-vs-session** boundary, which is exactly where the hazard lives.

**Discovery is the existence proof.** Discovery reads also issue `SELECT Management`; the
exclusive discovery lease exists precisely because someone hit this. The fix never generalized
to sessions.

## Ideal State Criteria

| ISC | Criterion | Verified by |
|---|---|---|
| ISC-1 | The three-line sequence cannot silently destroy session state. It either succeeds, or fails loudly with an error naming the current holder. | Hardware integration test |
| ISC-2 | Every P1/P2 row of the edge-case register is covered by a passing test, or is a documented bound with a pinning test that asserts the bounded behaviour. | Register → test-ID mapping, mechanically checked |
| ISC-3 | The session-vs-session boundary is a **named concept** enforced in one place, not a test convention. | The convention comment is deleted and replaced by a contract test |
| ISC-4 | No regression: 5 hardware discovery invariants, `PivMultiKeyContentionTests` 2/2, resilience 69/69, full Core suite, `dotnet format` clean. | Standing gates, every phase |
| ISC-5 | No material performance regression in scan latency or session-open latency against a baseline captured before any change. | `merge-diag` before/after |
| ISC-6 | Management transport tests genuinely exercise the transport they name. | A HID-pinned session must throw `NotSupportedException` from `ResetDeviceAsync`; SmartCard must not |
| ISC-7 | Every production behaviour change is backed by a test that failed **for its predicted reason** before the change. | RED output recorded verbatim in this ISA |
| ISC-8 | `DeviceId` has one stated contract, and no component contradicts it. | `YubiKeyDeviceRepository` and `FindYubiKeys` agree; contract documented |

## Non-goals

- Cross-process contention. The registry is in-process **by contract**; this effort does not change that.
- Logical-channel multiplexing. If two applet sessions genuinely need to coexist on one CCID
  interface, that is a separate design effort. Until someone demonstrates the need, a named loud
  error is the correct answer.
- Reworking `AsyncExchangeGate`, `DisposalGate`, single-flight reads, or worker admission. These
  are different layers and are load-bearing for reasons unrelated to this problem.
- The composite merge algorithm. Landed separately in PR #543.

## Abort criteria

Stop and re-plan rather than pushing through if any of these hold:

1. The fix requires a **public API redesign** rather than a behavioural correction.
2. The register exceeds roughly **30 P1/P2 rows** — the problem is bigger than scoped and needs
   splitting.
3. **Three DevTeam loops** close on one phase without convergence.
4. A hardware experiment shows the hazard is a **device/firmware** behaviour the SDK cannot
   correct, in which case the deliverable becomes documentation plus a loud failure, not a fix.

## Evidence rules

Inherited from the composite-merge effort and binding here:

- Defect fixes require **RED for the predicted reason**. A test that merely fails is not evidence;
  it must fail in the way the hypothesis states.
- Invariant pins may pass before the change and **never** count as fix evidence.
- Probabilistic or timing-sensitive changes require a diagnostics delta, not a single green run.
- Concurrency assertions run **at least 10 iterations**. A single intermittent red is a finding to
  investigate, never a rerun-until-green.

## Shipping decision

Behaviour change, no compatibility switch. The package is `2.0.0-preview.X`, which is the correct
window to fix a contract that silently destroys authentication state. The pinned test asserting
that two session leases succeed is **reclassified** — it proves lease *acquisition* is
ref-counted, not that concurrent applet sessions are *supported*. Two independent reviews reached
that reading separately.

## Residual — not covered by this effort

All hardware evidence here is **macOS**, on a two-key same-PID rig (serials 103 and 125, both
firmware 5.8.0, PID 0x0407).

- **Linux** — udev and PC/SC paths unverified.
- **Windows** — PC/SC sharing semantics, and the platform-divergent HID open behaviour
  (macOS seizes HID FIDO IO reports; Windows and Linux share) unverified.

These require dedicated hardware on those platforms and are tracked as followups, alongside the
existing Windows topology Tier 2 gap from the composite-merge effort. Coverage claims in this
document are macOS-scoped unless stated otherwise.

---

## Phase 0 — Harness unblocked (2026-07-30)

### Defect: transport-specific tests were not using their transport

`[WithYubiKey(ConnectionType = X)]` is a device **filter**, not a transport pin — a composite key
exposing SmartCard satisfies a HID request — and `WithManagementAsync` had no way to pin one. Two
integration tests therefore ran entirely over SmartCard while claiming to exercise HID, and passed.

**RED, for the predicted reason** (transport asserted, no pin):

```
Expected: HidFido   Actual: SmartCard
Expected: HidOtp    Actual: SmartCard
```

GREEN with `preferredConnection` threaded through: `ManagementHidConcurrencyTests` 2/2 over its
named transports, `GetDeviceInfo_AllTransports_ReturnsConsistentData` 3/3 over three genuinely
distinct transports.

This satisfies **ISC-6** and is a prerequisite for every Phase 1 Management experiment — without
it those experiments would have silently measured SmartCard three times.

`ManagementSession.Transport` was added as the instrument. It is derived from the connection the
constructor already switches on, not threaded down from transport resolution, so no new plumbing
exists. It reports what was actually opened rather than what was requested.

### Performance baseline (ISC-5)

Captured at `7f39d85f`, rig `ykphysical:103` + `ykphysical:125`, 20 iterations, macOS.
Phase 3 must be compared against this before any performance claim.

| Measurement | min | p50 | p95 | mean |
|---|---|---|---|---|
| Discovery scan (fresh finder) | 154.9 ms | 186.8 ms | 427.8 ms | 212.9 ms |
| Session open — SmartCard | 9.5 ms | 10.8 ms | 24.2 ms | 12.5 ms |
| Session open — HidFido | 19.8 ms | 20.4 ms | 24.1 ms | 21.1 ms |
| Session open — HidOtp | 222.0 ms | 253.2 ms | 397.4 ms | 273.4 ms |

Harness: `/var/folders/.../opencode/perf-baseline/` (`dotnet run -c Release -- 20 <label>`).

Incidental observation, not a goal of this effort: opening a Management session over HID OTP costs
roughly **25x** SmartCard and **13x** HID FIDO. Recorded because the transport fallback chain can
land a caller on HID OTP without the caller knowing, which makes that cost invisible at the call
site.

---

## Phase 3 — IN PROGRESS, blocked on a design decision (2026-07-30)

The CCID applet-ownership rule is implemented and green (Core 731, 0 failed; resilience 69/69;
format clean), but **cross-vendor review has not passed** and the code must not be merged as-is.

Implemented: same applet ref-counts; different applet while another holder exists throws
`SmartCardAppletConflictException`; a sole holder may switch its own applet (required by
`YubiOtpSession`, which legitimately SELECTs Management then OTP on one connection). Enforcement
sniffs the SELECT off the wire in `RegisteredSmartCardConnection` and claims before transmitting,
so a conflicting SELECT never reaches the card.

Review loop 1 (`gpt-5.6-terra`) → NEEDS WORK, 2 HIGH. Both addressed structurally: lease lifecycle
state moved entirely under the interface lock (the `_disposed` field was deleted rather than
re-guarded, so the bug class is not representable), and the pre-transmit claim is reconciled on
throw, cancellation, and non-success status word.

Review loop 2 → NEEDS WORK. Assessment of its findings:

| Finding | Reviewer | My assessment |
|---|---|---|
| Concurrent SELECTs on ONE lease overwrite the single `UnconfirmedSelect` slot | HIGH | **Over-rated.** Requires driving one connection from multiple threads, which `src/Core/CLAUDE.md` explicitly forbids: `PcscProtocol._exchangeGate` serializes exchanges, and there is one connection per session. Out of contract — but worth a guard or a documented precondition. |
| Phantom claim across TWO leases with in-flight SELECTs | HIGH | **Real**, but the impact is conservative: a stale applet name with zero holders, which the next claim overwrites, or a false refusal. Availability, not corruption. MEDIUM by impact. |
| Reconciliation assumes a thrown/cancelled transmit means the card did not act | UNRESOLVED | **Correct, and it is the real problem.** See below. |

### The open design question

A cancellation or transport fault can occur **after** the command reached the card and changed its
selection. The code cannot distinguish that from "never arrived", so abandoning the claim is an
optimistic guess that can leave the registry naming an applet the card does not have.

The asymmetry that matters: if the registry **under**-reports what is selected, a conflicting SELECT
gets through and a victim session is silently destroyed — the original defect. If it **over**-reports,
callers get false refusals — annoying, safe. So the safe direction under uncertainty is pessimistic,
and the current code is optimistic.

Candidate resolutions, none yet chosen:

1. **Keep the claim on indeterminate outcomes** (pessimistic). Simple, but a failed session's claim
   could block others until its lease is disposed.
2. **A third `Unknown` applet state** — next SELECT is always permitted and re-establishes truth, but
   no ref-count join is allowed while unknown. Honest, but it adds a concept, which Simplify resists.
3. **Re-read the selection from the card** to reconcile. Costs an APDU on a failure path and may
   itself fail.

This is a genuine design fork, not an implementation defect, so it goes back to planning rather than
to a third implementation loop. Note the abort criteria: "three DevTeam loops on one phase without
convergence" — we are at two.

Work is committed as WIP on the branch and must not be merged until this is settled.

---

## Phase 3 — DECISION: the enforcement layer was wrong (2026-07-30)

### Canonical evidence

**Rust** (`crates/yubikit/src`, authoritative for protocol behaviour). All six applet sessions take
the connection **by value**:

```rust
pub fn new(connection: C) -> Result<Self, (PivError, C)>
```

Verified across `piv`, `oath`, `management`, `openpgp`, `hsmauth`, `securitydomain`. **Zero** take
`&mut`. The session owns the connection, so a second session on it is a compile error. The error arm
returns the connection, making ownership transfer explicit and recoverable.

**Python** (`packages/yubikit/yubikit/core/__init__.py`, base `Session`):

```python
def __init__(self, connection):
    existing = getattr(connection, "_session", None)
    if existing is not None:
        existing.close()
    setattr(connection, "_session", self)
```

One session per connection, enforced at construction. A second session does not corrupt the first —
it deterministically **closes** it.

Both enforce at **session-to-connection binding, before any wire operation**. Neither sniffs
SELECTs. Neither reconciles anything. Neither can have the indeterminate-outcome problem, because
the second session never reaches the wire.

### Why our approach could not converge

Enforcing at transmit time requires a registry that mirrors the **card's** selection state, and card
state after an indeterminate transmit is unknowable in principle — the Two Generals problem. All
three candidates in the previous section were mitigations of unknowability, not solutions. The
unresolved HIGH was not the last bug; it was a proof that the layer was wrong.

Moving enforcement to acquisition changes what the registry **means**: from *"what applet is the card
in"* (unknowable) to *"which in-process holder has leased this interface"* (a pure in-process fact,
always knowable). Card state becomes irrelevant rather than untracked, because every session begins
with its own SELECT — a new holder re-establishes ground truth regardless of what a crashed
predecessor left behind. Candidate 2's `Unknown` state becomes the implicit default *between* leases,
so the concept evaporates instead of being added.

### Decision

**One lease per CCID interface**, acquired at session construction, released on dispose. Conflicting
acquisition throws. Every acquisition is followed by the session's own SELECT (already true today).
Internal convenience APIs route around a held lease where a safe transport exists.

Three sub-decisions:

1. **Refuse the newcomer; do not copy Python's close-the-predecessor.** Python's choice is safe there
   because the caller explicitly passed the same connection twice. In C# the newcomer is an
   *invisible internal call* (`GetDeviceInfoAsync` opens its own connection), so closing the
   predecessor would trade silent corruption for silent revocation, and under concurrency would turn
   a design error into a race. Refusal also puts the exception on the call that *would have caused*
   the damage rather than on the innocent operation three lines later. This matches .NET convention —
   `FileShare` violations throw at open.
2. **Forbid same-applet nesting**, despite Phase 1 measuring it hardware-safe. Hardware-safe is not
   software-safe: two PIV sessions on one interface share security state, so one verifying a PIN
   silently elevates the other. Forbidding now and widening later is non-breaking; shipping nesting
   and withdrawing it later is not.
3. **`GetDeviceInfoAsync` must prefer a non-conflicting transport** when CCID is leased, and throw
   only when no route exists. Phase 1 experiment 4 proved both HID transports work while PIV holds
   CCID. The SDK should not be the thing that throws in its own motivating case.

### What this deletes

Dropping applet-awareness from the lease removes, not adds: SELECT sniffing and APDU parsing, the
applet-keyed registry state, the claim/reconcile lifecycle, the guarded restore, the
sole-holder-may-switch rule (YubiOTP's Management→OTP is one lease — no rule needed), and the
`Unknown` state discussion. `SmartCardAppletConflictException` survives in simplified form.

Commit `00a9e26f` is superseded. Most of its 17 tests express requirements that survive and retarget
to acquisition time with *less* setup, because there is no wire to fake. The `DeviceConnectionRegistry`
plumbing survives. What dies is the sniffing decorator and the reconciliation logic — exactly the
parts review could not pass.

### The usage pattern the pivot requires (and what canonical does)

Forbidding two concurrent sessions is only acceptable if using two applets stays ergonomic. The
canonical answer is **sequential ownership transfer with connection reuse**, not reconnect.

Rust gives every applet session `pub fn into_connection(self) -> C` — verified on all six
(`piv`, `oath`, `management`, `openpgp`, `hsmauth`, `securitydomain`):

```rust
let piv  = PivSession::new(conn)?;
// ... use piv ...
let conn = piv.into_connection();    // hand the connection back
let oath = OathSession::new(conn)?;  // give it to the next applet
```

One connection, successive applets, no re-enumeration and no reconnect. Ownership moves; the
physical handle never closes.

Python reaches the same outcome differently: constructing the next session on the same connection
closes the previous one automatically, so the sequence is implicit.

**C# already has the pieces, and already needed them.**

1. Every session's `CreateAsync` accepts a caller-owned `ISmartCardConnection`
   (`PivSession.cs:124`), so the caller *can* own the connection.
2. But disposing a session disposes the connection (`PcscProtocol.cs:93`), which prevents reuse —
   this is the gap.
3. The SDK **already solved it, privately**: `src/Tests.Shared/SharedSmartCardConnection.cs` is a
   non-owning wrapper that forwards everything and ignores `Dispose`, added because integration
   helpers needed several sessions over one physical connection. The pattern exists, is proven, and
   is visible only to the test assembly.

That third point is the finding. The SDK hit this exact need, built the answer, and left it in test
infrastructure. The pivot should promote it: give callers a supported way to run successive applet
sessions over one connection, so "one session at a time per interface" costs an explicit handoff
rather than a reconnect.

Deferred to the implementation phase: whether that surface is a non-owning connection wrapper, an
ownership flag on `CreateAsync`, or a scoped multi-applet helper. All three are additive and none
changes the enforcement rule.

---

## Phase 4 — Enforcement moved to acquisition (2026-07-30)

Implements the Phase 3 DECISION. Enforcement is at binding time in three places, none of which looks at the
wire, so the Two Generals problem that killed `00a9e26f` cannot arise: nothing here mirrors card state.

### What changed

| # | Change | Where |
|---|---|---|
| 1 | Protocols no longer dispose their connection | `PcscProtocol`, `FidoHidProtocol`, `OtpHidProtocol` |
| 2 | Discovery disposes the connection it created | `ProtocolDeviceInfo.ConnectAndReadAsync` |
| 3 | The interface lease belongs to the CONNECTION, and CCID is exclusive | `DeviceConnectionRegistry.AcquireConnectionAsync(id, exclusive)`, `PcscYubiKey` (`true`) / `HidYubiKey` (`false`) |
| 4 | One live session per connection | `ConnectionSessionGuard`, attached in the `ApplicationSession` constructor |
| 5 | An in-process refusal counts as a held transport | `YubiKeyConnectionExtensions.IsHeldTransportError` |
| 6 | Convenience entry points own the connection they open | `ApplicationSession.OwnConnection()`, called at 8 `Create<App>SessionAsync` sites |
| 7 | A failed `CreateAsync` releases its claim | 8 session factories |

`SmartCardAppletConflictException` did not come back. One exception type, `ConnectionInUseException`, covers
both refusals — they are the same fact at two scopes.

### Deleted

`SharedSmartCardConnection` and its 4 usages (non-owning is now the default), the false `src/Core/CLAUDE.md`
gotcha "PcscProtocol disposes its underlying connection", 5 duplicated `_connection` fields (subsumed by
`ApplicationSession.Connection`), and — from the reverted design — SELECT sniffing, applet-keyed registry
state, and the claim/reconcile lifecycle. Net: no new subsystem, one new 85-line guard, one exception type.

### RED, each for its predicted reason

Recorded verbatim before the corresponding change. Full set: `dotnet toolchain.cs -- test --project Core
--filter "FullyQualifiedName~ConnectionOwnershipContractTests"` at the pre-change tree.

```
failed ConnectionOwnershipContractTests.ConnectAsync_SecondConnectionToHeldCcidInterface_IsRefused
  Assert.Throws() Failure: No exception was thrown
  Expected: typeof(Yubico.YubiKit.Core.Devices.ConnectionInUseException)

failed ConnectionOwnershipContractTests.Session_SecondLiveSessionOnOneConnection_IsRefused
  Assert.Throws() Failure: No exception was thrown
  Expected: typeof(Yubico.YubiKit.Core.Devices.ConnectionInUseException)

failed ConnectionOwnershipContractTests.Session_Dispose_DoesNotDisposeACallerCreatedConnection
  Assert.Equal() Failure: Values differ
  Expected: 0
  Actual:   1

failed ConnectionOwnershipContractTests.PcscProtocol_Dispose_DoesNotDisposeTheConnection
  Assert.Equal() Failure: Values differ
  Expected: 0
  Actual:   1

failed ConnectionOwnershipContractTests.SuccessiveSessions_OverOneConnection_BothReachTheWire
  System.ObjectDisposedException : Cannot access a disposed object.
  Object name: 'RecordingSmartCardConnection'.
    at PcscProtocol.SelectAsync(...)
    at ProbeSession.CreateAsync(...)          <- the SECOND session

failed ConnectionOwnershipContractTests.Session_AfterFirstDisposed_SecondSessionOnSameConnectionSucceeds
  System.ObjectDisposedException : Cannot access a disposed object.   (same cause)

failed IYubiKeyExtensionsTransportTests.CreateManagementSessionAsync_CcidHeldInProcess_FallsBackToHidFido
  Assert.Equal() Failure: Collections differ
                        ↓ (pos 1)
  Expected: [SmartCard, HidFido]
  Actual:   [SmartCard]

failed OathSessionTests.CreateAsync_InitializationFails_LeavesTheConnectionUsableByTheNextSession
  ConnectionInUseException : This connection already has a live OathSession. ...
    at ConnectionSessionGuard.Attach   <- ghost holder left by the failed first attempt
```

The last one is a defect introduced by change 4 and caught by its own pin before it shipped: the guard is
attached in the constructor, so a factory that throws after construction must dispose what it built.

### Invariant pins (passed before AND after — not fix evidence)

`ConnectAsync_AfterFirstConnectionDisposed_SecondSucceeds` ·
`ConnectAsync_HidInterface_AllowsConcurrentConnections` ·
`ConnectAsync_CcidHeld_SameKeysHidInterfaceStillConnects` ·
`ConnectAsync_DifferentInterfaces_CreatePhysicalConnectionsConcurrently` ·
`CreateOathSessionAsync_DisposingSession_DisposesTheConnectionItOpened` ·
`CreateManagementSessionAsync_CcidHeldInProcess_ExplicitOverrideDoesNotFallBack` ·
`CreateManagementSessionAsync_CcidHeldInProcess_NoOtherTransport_Throws` ·
`IdentityRead_DeviceInUse_SkipsWithoutConnecting` ·
`MetadataRead_CompositeWithInUseSmartCardMember_SkipsItButTriesOtpTransport` ·
`Coordinator_CanceledWaiterDecrementsCount_AndRemainingConnectionHasPriority`

### Tests retargeted, and why

Four tests asserted the ownership bug as if it were the contract. None was bulk-edited to pass; each was
re-pointed at the requirement it was actually reaching for.

| Test | Was | Now |
|---|---|---|
| `PcscProtocolTests.Dispose_Twice_DisposesConnectionOnce` | disposal reaches the connection | `Dispose_Twice_IsIdempotent_AndDoesNotDisposeConnection` — the idempotency claim survives, the ownership claim does not |
| `PcscProtocolScpTests.Dispose_DisposesBaseProtocol` | proved base disposal by watching the CONNECTION die | `Dispose_DisposesBaseProtocol_ButNotTheConnection` — asserts the base protocol directly |
| `PcscProtocolScpTests.Dispose_Twice_DisposesBaseProtocolAndScpProcessorOnce` | connection disposed once | `..._DisposesScpProcessorOnce_AndNeverTheConnection` |
| `DeviceConnectionRegistryTests.AcquireSession_RefCountsPerDeviceId_AndDisposeIsIdempotent` | ref-counting for every interface | split: `AcquireConnection_NonExclusiveInterface_RefCounts_AndDisposeIsIdempotent` (HID, still a real requirement) and `AcquireConnection_ExclusiveInterface_SecondAcquisitionIsRefused` (CCID) |

The fourth is the one Phase 3 flagged for reclassification. The pin was wrong, not just the code: it read
"acquisition is ref-counted" as "coexistence is supported", and that conflation is what let the defect through.

### Not done, deliberately

- **`into_connection`-style ownership transfer as public API.** Not needed: with non-owning disposal the
  handoff is just "dispose session A, construct session B on the same connection." Adding a method would
  add a concept for a lifetime C# already expresses.
- **Same-applet nesting.** Phase 1 measured it hardware-safe and Phase 3 chose to forbid it anyway — two
  PIV sessions on one interface share security state, so one verifying a PIN silently elevates the other.
  Widening later is non-breaking; withdrawing later is not.
- **Cross-process contention.** Unchanged and still out of scope; it surfaces as a PC/SC sharing violation.
- **A finalizer backstop for the ownership change.** It would hide the mistake it exists to catch.

### Verification

Build 0 errors · Core 724 total / 0 failed / 3 pre-existing skips (baseline 714, +10 new) · full unit suite
1807 total / 0 failed · resilience 69/69 · `dotnet format --verify-no-changes` 0 errors.

### Residual

Hardware validation of the three-line motivating sequence (ISC-1) still requires the rig. Everything above
is no-hardware evidence: the refusals are in-process facts, which is exactly why they are testable without a
YubiKey — but that the SDK now *routes* Management over HID does not by itself re-prove that the PIV session
survives. Phase 1 experiment 4 measured that; a post-change integration run should confirm it end to end.
---

## Status at handoff (2026-07-30)

### Complete

| Phase | Outcome |
|---|---|
| 0 Harness | `preferredConnection` pin + `ManagementSession.Transport`; two false-positive tests fixed (RED: "Expected HidFido, Actual SmartCard") |
| 1 Investigation | 4 hardware experiments, predictions recorded first; findings in `phase1-findings.md` |
| 2 Fable | Declared two of its own prior recommendations dead; canonical comparison settled the layer question |
| 3 Fix | Wire-sniffer reverted (`b0ce52a0`); ownership + CCID exclusivity (`d463e83a`) |
| 4 DeviceId | Phantom events fixed (`1e0560af`) |

**ISC-1 achieved on hardware.** The three-line sequence completes; it was `SW=0x6D00`. Cross-applet
is refused with the victim session intact.

Gates at `1e0560af`: build 0 errors/0 warnings · Core 729 total, 0 failed, 3 pre-existing skips ·
resilience 69/69 · full suite 12/12 · format 0 errors · hardware: discovery 5/5, PIV two-key 2/2.

### Remaining

- **Phase 5 — documentation.** Guarantees-doc sufficiency (the `ce07f721` regression: serial
  conditionality, `pidCorrelationUntrusted`, the flags-union caveat), consumer surface on
  `FindAllAsync`, `docs/` index, staleness + L4 SVG re-render, register row→test-ID mapping, and the
  **ownership contract with its migration note** — the breaking change is currently recorded only in
  a commit message.
- **Phase 6 — Fable `/CodeAudit`**, scoped to `Devices/`, `Transports/`, applet transport resolution.

### Deferred, with reasons

- **Cross-vendor review of Phases 3–4.** Both were engineered and self-verified but not reviewed.
  Terra reviewed the *superseded* wire-sniffer, not this design. **Do not merge without it.**
- **Hardware confirmation of the DeviceId tier flip.** The mechanism is pinned by a merger-level test
  showing the id flips while interface paths stay byte-identical, and the fix is pinned by a
  repository test that went RED for the predicted reason. Physical confirmation needs either
  unplugging a key or reconfiguring one to a different PID via the reconfiguration harness — planned,
  not run.
- **~~Linux~~ and Windows.** Linux was closed on 2026-08-03 — see Phase 7 below. Windows PC/SC
  sharing semantics and HID open behaviour remain unverified.
- **Windows topology Tier 2**, carried from the composite-merge effort.
- **A `claude`-CLI entry for Fable** in `NAMED_MODEL_ALIASES` — its Copilot-only chain went dark when
  quota ran out, though the CLI transport worked.

### Register status

Resolved: A1, A2, A3, A5, B4, B5, C1, C2 (partial), E1, E2. Unresolved: A4, B1–B3, D1–D3, F1.
F2/F3 remain platform gaps.

---

## Phase 7 — Linux hardware confirmation (2026-08-03)

Different machine, different OS, different firmware, different key, one interface class. Every
earlier hardware result in this document is macOS / firmware 5.8.0 / two keys (103, 125); none of
it transfers by argument, because the two things this effort actually depends on — PC/SC sharing
semantics and HID open behaviour — are exactly the things that diverge per platform.

Rig: Linux, `pcscd` 1.7.5, `70-yubikey.rules` present, single YubiKey 5A, **serial 9681620,
firmware 5.4.3**, `UsbAKeychain`, PID `0x0407`, all three transports enabled.

### Standing gates (ISC-4), reproduced on Linux

| Gate | Linux | macOS reference | |
|---|---|---|---|
| Build | 0 errors | 0 errors / 0 warnings | matches |
| Core unit | 729 total, 0 failed, 3 skips | 729 / 0 / 3 | **identical** |
| Full unit suite | 1815 total, 0 failed, 12/12 projects | 1807 total, 0 failed | +8 (Phase 4 → `1e0560af` delta) |
| Resilience | 69/69 | 69/69 | **identical** |
| `dotnet format --verify-no-changes` | exit 0 | 0 errors | matches |
| Discovery invariants | **5/5** | 5/5 | **identical** |
| Core integration (whole suite, smoke) | 22/22 | — | new |
| Management integration (smoke) | 38 passed, 13 skipped, 0 failed | — | new; skips are FW ≥5.7.0 gates and multi-key |
| PIV two-key contention | **not run** | 2/2 | needs a second key |

The Core unit and resilience numbers landing on the exact macOS figures is the useful signal:
the ownership model, `ConnectionSessionGuard`, and the CCID exclusivity contract tests are
platform-independent in fact, not just in argument.

### ISC-1 on Linux — the motivating sequence, end to end

The Phase 4 Residual asked for exactly this: *"a post-change integration run should confirm it end
to end."* Run against the live card, PIV `VERIFY` attempted only because PIN metadata reported
`IsDefault=true` (no guessing, no burned retries), nothing else written to the device.

```
PASS  PIV session live, GetSerialNumber -> 9681620
      PIN metadata: IsDefault=True retries=3/3
PASS  VerifyPin succeeded — verified-PIN state established on the PIV applet
PASS  Management routed over HidFido, NOT SmartCard — CCID lease respected
PASS  PIV still answering, GetSerialNumber -> 9681620   (pre-fix this was SW=0x6D00)
PASS  PIN retry counter intact at 3 — verified state was not torn down
PASS  Refused loudly with ConnectionInUseException
```

All four lines of the motivating sequence behave as the Phase 3 DECISION predicted. The third line
is the load-bearing one: `ManagementSession.Transport` reported `HidFido`, proving the Phase 4
change-6 fallback picks a non-conflicting transport on Linux rather than taking CCID. Phase 1
experiment 4 measured that HID works while PIV holds CCID; this confirms the SDK now *chooses* it.

The refusal message is worth quoting, because ISC-1 requires failing "loudly with an error naming
the current holder":

> The SmartCard interface 'pcsc:Yubico YubiKey OTP+FIDO+CCID 00 00' already has a live connection in
> this process. A YubiKey's CCID interface holds one selected application at a time, so a second
> connection would deselect the first holder's application and destroy its security state. Dispose
> the existing connection first, or run both applications as successive sessions over that one
> connection.

Firmware 5.4.3 is a second new axis: the ownership model does not depend on 5.8.0-era behaviour.

Harness: `/tmp/opencode/isc1-probe/` (`dotnet run -c Release`), ephemeral — see the gap below.

### ISC-1 is now pinned in CI (`aadd89f1`)

The probe above was promoted to `PivSessionContentionTests` — five tests covering victim survival,
the Management non-SmartCard fallback asserted on `ManagementSession.Transport`, the loud refusal,
successive sessions over one caller-owned connection, and the per-connection guard. `ConnectionInUseException`
previously appeared in **zero** integration tests, so the headline criterion of the whole effort was
un-pinned; it no longer is.

**RED for the predicted reason**, captured at `b0ce52a0` in a detached worktree on this rig:

```
GetDeviceInfoAsync_WhilePivSessionHasVerifiedPin_DoesNotClobberSessionState
  ApduException : Sign/decrypt operation failed for slot 0x9A:
  Instruction code not supported or invalid (SW=0x6D00)

CreateManagementSessionAsync_WhilePivHoldsCcid_OpensOverANonSmartCardTransport
  Assert.NotEqual() Failure: Values are equal
  Expected: Not SmartCard
  Actual:       SmartCard

SuccessiveSessions_OverOneCallerOwnedConnection_BothReachTheCard
  System.ObjectDisposedException : Cannot access a disposed object.
  Object name: 'UsbSmartCardConnection'
    at ManagementSession.CreateAsync          <- the SECOND session
```

The remaining two require `ConnectionInUseException`, which does not exist pre-change and therefore
cannot be compiled against that tree. That absence is their evidence.

### Eight existing tests asserted the OLD contract (`aadd89f1`)

Running the module integration suites on hardware — for the first time since the ownership change,
because the macOS rig only ever ran discovery and two-key contention — surfaced eight tests holding
two live sessions on one CCID interface. This is the same class Phase 4 already handled for four unit
tests ("Tests retargeted, and why"); the job was simply unfinished, and no macOS run could have
revealed it.

| Test file | Count | Was |
|---|---|---|
| `PivManagementKeyTests` | 3 | second PIV session opened while the first was still live |
| `OathPasswordChangeTests` | 2 | up to four overlapping sessions in one test |
| `OathSessionTests` | 2 | locked/unlocked observation sessions nested inside the setup session |
| `OathHashAlgorithmTests` | 1 | locked session nested inside the setup session |

Each was re-pointed at the requirement it was reaching for — *"the new credential authenticates on a
**fresh** session"* — which is what a consumer actually writes. None was bulk-edited to pass. This is
a **finding about branch readiness**, not about the design: the breaking change's test debt was
larger than Phase 4 recorded.

### Pre-existing defects this rig exposed (NOT caused by this branch; fixed in Phase 8)

Neither is in scope here; both are recorded because a 5.8.0-only macOS rig could never surface them.

1. **143 firmware-gated integration tests fail instead of skipping on older firmware.** `[WithYubiKey(MinFirmware=…)]`
   raises `Xunit.SkipException` from `YubiKeyTestState.BindToRealDevice()`, which only becomes a skip
   under `[SkippableTheory]`. 143 such tests use plain `[Theory]`; only 12 use `[SkippableTheory]`.
   On fw 5.4.3 this reports 16 Piv, 18 SecurityDomain, 6 Oath and 2 OpenPgp failures that are really
   skips. This branch never touched that path.
2. **YubiHsm and YubiOtp integration tests cannot run at all** — `FileNotFoundException: Could not
   load file or assembly 'Xunit.SkippableFact'`. Already fixed on `origin/yubikit` by `2e381cb1`;
   absent here only because the branch is 34 commits behind, which was parked deliberately.

### Integration results, Linux / fw 5.4.3 / two keys (9681620, 20260533)

| Suite | Result | Notes |
|---|---|---|
| Core | 22 / 22 | includes discovery invariants 5/5 |
| Management | 38 passed, 13 skipped, 0 failed | skips are FW ≥5.7.0 gates |
| Piv | 67 passed, 8 "failed" | all 8 are pre-existing defect 1 |
| Oath | 15 passed, 3 "failed" | all 3 are defect 1 (serial 103 absent) |
| OpenPgp | 45 passed, 1 "failed" | defect 1 |
| SecurityDomain | 16 passed, 9 "failed" | defect 1 |
| YubiHsm / YubiOtp | blocked | defect 2 |

### Still open after this run

- **PIV two-key contention** — `PivMultiKeyContentionTests` still skipped; it needs both keys to
  expose SmartCard simultaneously and was not re-run after the second key was added.
- **DeviceId evidence-tier flip** — unchanged; needs unplug or PID reconfiguration.
- **Windows** — unchanged.
- **Cross-vendor review of Phases 3–4** — still the blocking item before merge.

---

## Phase 8 — Hardware-test harness repair and Linux performance delta (2026-08-04)

### Dynamic skips are now a mechanically enforced contract

The Phase 7 count understated the defect. There were 143 firmware-gated cases using plain
`[Theory]`, but firmware is only one way a filter can miss. The actual invariant is broader:
**every** `[WithYubiKey]` test can have no matching device (firmware, form factor, capability,
transport, custom filter, or simply no hardware). The exact inventory was 260 methods in 58 files
using `[Theory]`, versus 61 already using `[SkippableTheory]`.

The repair:

- All 260 declarations now use `[SkippableTheory]`: **321 skippable, 0 plain**.
- YubiHsm and YubiOtp integration projects directly reference `Xunit.SkippableFact`; the reference
  on `Tests.Shared` is intentionally `PrivateAssets=all` and cannot supply the runtime assembly.
- Active documentation and XML examples now teach `[SkippableTheory]`, including the canonical
  `WithYubiKeyAttribute` docs and `docs/TESTING.md`.
- `dotnet toolchain.cs test-infrastructure-qa` scans integration declarations and project package
  references. Both `build` and `test` depend on it, so the defect cannot silently return.

**RED:** the new guard reported exactly `260` declaration violations and `2` missing package
references. **GREEN:** 0 violations; a fw 5.7-only PIV test on this 5.4.3 rig reports `1 skipped,
0 failed` with process exit 0.

### One additional false transport test

`YubiOtpSessionIntegrationTests.CalculateHmacSha1_WithKnownKey_ReturnsExpectedResponse` requested
`ConnectionType.HidOtp` through `[WithYubiKey]` but opened `CreateYubiOtpSessionAsync()` with the
default transport order, so it ran over SmartCard while claiming HID coverage — the same test-harness
mistake Phase 0 found in Management. RED was `SW=0x6985` through `SmartCardBackend`; pinning
`preferredConnection: state.ConnectionType` is GREEN over HidOtp.

### Linux hardware matrix after the repair

One allow-listed key was available for this pass: serial 9681620, fw 5.4.3. The second connected
device identified itself as **24070033**, the personal SSH key removed from the allow list in
`8af1207f`; it was correctly filtered and no test operated on it. Consequently the two-key gate
remains unverified in this pass.

| Suite | Result |
|---|---|
| Core | 22 passed, 0 failed |
| Management | 38 passed, 13 skipped, 0 failed |
| Piv | 65 passed, 10 skipped, 0 failed |
| Oath | 15 passed, 3 skipped, 0 failed |
| OpenPgp | 45 passed, 1 skipped, 0 failed |
| SecurityDomain | 16 passed, 9 skipped, 0 failed |
| YubiHsm | 6 passed, 5 skipped, 0 failed |
| YubiOtp | 10 passed, 0 failed |
| Fido2 | 24 passed, 5 skipped, 0 failed |
| WebAuthn | 1 skipped, 0 failed |

Management's HID OTP concurrency test timed out once in the first whole-suite pass, then passed 10
focused runs (50 concurrent-call iterations) and the final sequential whole-suite run. Two module
suites run in parallel also produced transport contention; that evidence was discarded — hardware
suites must run sequentially against one physical key.

### ISC-5 — before/after on the same Linux rig

The macOS baseline at `7f39d85f` cannot serve as a before/after comparison on Linux. The original
pre-change tree (`b0ce52a0`) and the current branch were therefore measured on the same machine,
same connected devices, same serial-9681620 target, 20 iterations each. Fresh `FindYubiKeys` per
discovery scan; Management session creation pinned per transport; native/JIT warmup before timing.

| Measurement | Pre-change min / p50 / p95 / mean (ms) | Current min / p50 / p95 / mean (ms) | Delta at p50 / p95 / mean |
|---|---:|---:|---:|
| Discovery scan | 116.5 / 127.1 / 137.4 / 126.9 | 115.9 / 127.7 / 139.0 / 128.9 | +0.5% / +1.2% / +1.6% |
| Session open — SmartCard | 28.0 / 38.5 / 42.3 / 38.1 | 30.3 / 34.9 / 40.9 / 35.4 | **-9.4% / -3.3% / -7.1%** |
| Session open — HidFido | 61.4 / 62.0 / 62.5 / 62.0 | 61.2 / 62.0 / 62.3 / 61.9 | 0.0% / -0.3% / -0.2% |
| Session open — HidOtp | 12.7 / 14.5 / 22.3 / 165.0 | 12.0 / 14.0 / 21.3 / 164.4 | -3.4% / -4.5% / -0.4% |

Both HidOtp runs contain one approximately 3-second outlier, which inflates the mean equally while
leaving p95 near 22 ms. That is a baseline transport characteristic, not a branch delta. **ISC-5
passes: no material performance regression.** Discovery moves by at most 1.6%; session-open p50,
p95, and mean are equal or faster on every transport.

Harnesses and raw logs are ephemeral under `/tmp/opencode/perf-{before,current}`; all statistics and
methodology needed to reproduce them are captured above.

### Final two-key gate — 2/2 on Linux

After replacing the filtered personal key with allow-listed test key 20260533, the infrastructure
discovered both intended devices with all required interfaces:

- 9681620 — YubiKey 5A, UsbAKeychain, firmware 5.4.3
- 20260533 — YubiKey 5C, UsbCKeychain, firmware 5.4.3

`PivMultiKeyContentionTests` passed **2/2**:

1. `FindAllAsync_WithOpenSessionOnOneKey_IdentifiesOtherKeysAndPreservesSession` — an authenticated,
   PIN-verified PIV session survived a cold multi-device discovery scan while the free key remained
   fully identifiable.
2. `ConcurrentPivSessions_OnTwoKeys_OperateIndependently` — two sessions on distinct physical keys
   each completed 10 PIN-gated EccP256 signatures in parallel without cross-wiring registry entries,
   connections, or exchange gates.

This reproduces the macOS 2/2 gate on Linux with different keys and older firmware. The Phase 8
single-key limitation is closed.
