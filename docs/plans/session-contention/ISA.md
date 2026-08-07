---
task: Session-vs-session contention on multi-interface YubiKey applets
branch: yubikit-session-contention
base: cb9ca41f (composite-merge HEAD; becomes yubikit via PR #543)
phase: phase-16-cross-vendor-review-G1-G2-discharged
date: 2026-07-30
last-updated: 2026-08-06
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
| ISC-1 | The three-line sequence cannot silently destroy session state. It either routes over a non-conflicting interface, or fails before wire I/O with an error naming the contended interface; the separate per-connection guard names the live session. | Hardware integration test + acquisition contract tests |
| ISC-2 | Every P1/P2 row of the edge-case register is covered by a passing test, or is a documented bound with a pinning test that asserts the bounded behaviour. | Register → test-ID mapping, mechanically checked |
| ISC-3 | The session-vs-session boundary is a **named concept** enforced in one place, not a test convention. | The convention comment is deleted and replaced by a contract test |
| ISC-4 | No regression: 5 hardware discovery invariants, `PivMultiKeyContentionTests` 3/3 including RSA-4096 liveness, resilience 69/69, full Core suite, and whitespace/style/error-severity analyzer formatting checks clean. | Standing gates, every phase |
| ISC-5 | No material performance regression in scan latency or session-open latency against a baseline captured before any change. | `merge-diag` before/after |
| ISC-6 | Management transport tests genuinely exercise the transport they name. | A HID-FIDO-pinned session rejects `ResetDeviceAsync` with `NotSupportedException`; the SmartCard backend emits reset APDU INS `0x1F` |
| ISC-7 | Every production behaviour change is backed by a test that failed **for its predicted reason** before the change. | RED output recorded verbatim in this ISA |
| ISC-8 | Identity contracts are explicit per API scope: repository-published `DeviceId` is stable for one uninterrupted presence, while fresh direct scans may derive different evidence-tier IDs. | Repository tier-flip/final-removal unit pins plus `FindAllAsync`/`DeviceChanges` documentation; **Windows topology-tier path hardware-confirmed in Phase 12** by an operator-coordinated insert/remove (4 actions → 4 events, zero phantom events, exact add/remove correlation). Windows resolves same-PID keys by the topology tier, so the serial↔PID flip needed a degraded-path run to reach hardware — **supplied on macOS in Phase 14** (7 actions → 7 events, zero phantoms, the real `ykphysical:pid:0407` → `ykphysical:103` flip observed on hardware). Linux was dropped by decision: it also lacks a Container ID, so it exercises the same degraded tiers |

## Non-goals

- Cross-process contention. The registry is in-process **by contract**; this effort does not change that.
- Logical-channel multiplexing. If two applet sessions genuinely need to coexist on one CCID
  interface, that is a separate design effort. Until someone demonstrates the need, a named loud
  error is the correct answer.

## Status and remaining work (as of 2026-08-06, `d0f672c3`)

**All eight ISCs pass, but the effort is NOT merge-ready.** A real production defect (**G5**) is open and
awaiting a fix-or-accept decision, and ISC pass status does not override it: the ISCs were written before
that defect was known, and none of them asks the question it fails. Read "all eight ISCs pass" as "the
criteria we set are met", not as "nothing is wrong". The edge-case register is 23 rows with zero open rows
and zero platform gaps.

**Updated after Phase 16.** The cross-vendor review of the production code (G1, G2) is now complete and
found two real defects, so the earlier claim that no remaining item was expected to change production code
did not survive contact with an opposite-family reviewer. One defect is fixed (Windows HID constructor
handle leak); one is filed and is now the blocking decision **G5**. Note also that all four prior Cato
audits were accidentally **same-vendor** and cannot be cited as cross-vendor evidence — see Phase 16.

Three production defects were found and fixed along the way, all of them real and none of them the
contention bug this effort set out to fix:

| Defect | Fix | Platform |
|---|---|---|
| FIDO HID opened with `kIOHIDOptionsTypeSeizeDevice`, so a second open failed `0xE00002C5` | `IOHIDDeviceOpen(handle, 0)`, matching Rust and python-fido2 | macOS |
| OTP HID feature connection opened the keyboard collection read/write | `DESIRED_ACCESS.NONE` (`6289c774`) | Windows |
| `YubiOtpSlotConfigTests` leaked OTP HID connections | disposal fix (`d82218bf`) | all |

### Merge gates — blocking

| # | Gate | Why it blocks |
|---|---|---|
| G1 | ~~Cross-vendor review of the Phase 11 Windows work~~ | **DISCHARGED, Phase 16.** `github-copilot/gpt-5.5`, verdict `concerns`. The access split is validated: `DESIRED_ACCESS.NONE` confirmed sufficient across every enumerated reachable feature-report call site, and `OpenIOConnection` correctly keeps read/write for FIDO. Found a real native handle leak on the failing-constructor path (fixed, 3 unit pins). The legacy-SDK parity claim is now known to be **false**, not merely unverifiable: v1 opens the feature connection with `GENERIC_WRITE` |
| G2 | ~~Record an explicit Phase 3-4 cross-vendor review verdict~~ | **DISCHARGED, Phase 16.** `github-copilot/gpt-5.5`, verdict `concerns`. Cleared: no TOCTOU, no sham guard, no memory/security violation, lease lifecycle sound. One real defect found and **filed rather than fixed** (session guard stranded by a derived-constructor failure on borrowed connections) — see G5 |
| G5 | ~~Fix or accept Finding 2 — `ConnectionSessionGuard` stranded when a derived session constructor throws~~ | **DISCHARGED 2026-08-06, fixed** (`8ef09522`). Binding moved out of the constructor into a new `ApplicationSession.Construct`, making the stranded state unrepresentable rather than cleaned up afterwards. Two cleanup designs were rejected as unsafe first — see Phase 17. All 8 factories converted; hardware gates re-run green |
| G3 | ~~Re-run Cato on this ISA~~ | **DISCHARGED 2026-08-06.** Ran with the corrected `--current-vendor anthropic`; auditor `openai/github-copilot/gpt-5.5` — the first genuinely cross-vendor audit of this document. Verdict improved `fail` → `concerns`. Two findings fixed (the "all eight ISCs pass" overclaim, and ISC-4's missing uncontended-host precondition); the third, a challenge to dropping Linux E1/E2, is recorded as **contested** rather than actioned because it overrides an operator decision. Commit `8297563d` |
| G4 | Review and merge consolidation | Final step |

### Deferred, with a recorded decision

| Item | Decision |
|---|---|
| Base reconciliation — **53 commits behind** `yubikit` (49 ours, 104 overlapping files) | **Parked deliberately**, but the topology is now measured rather than guessed — see "Base divergence" below. Needs an explicit decision before merge |
| Hoist `DeviceInfo` properties (serial first) onto `IYubiKey`, especially composite | New branch/issue. Compare against Rust `LocalYubiKeyDevice`. Out of scope here |
| 143 firmware-gated `[Theory]` tests fail instead of skipping on old firmware | Pre-existing, not this branch. Believed already resolved; needs one confirming run, then delete the entry |

### Base divergence, measured (2026-08-06)

The previously recorded "34 commits behind, 14 conflicts" was stale and the conflict count was never
verified. Measured against merge-base `46269ffd`:

| | Count |
|---|---|
| Commits on `origin/yubikit` not in this branch | **53** |
| Commits here not upstream | **49** |
| Files changed on both sides | **104** |

**The two branches worked on different axes of the same problem**, which is why they are largely
composable rather than competing:

| File | Ours | Upstream | Verdict |
|---|---|---|---|
| `DeviceConnectionRegistry.cs` | +74/-37 | **untouched** | clean — our contention work is uncontested |
| `ConnectionSessionGuard.cs`, `ConnectionInUseException.cs` | new files | absent | clean — introduced here |
| `AsyncExchangeGate.cs` | untouched | untouched | clean |
| `IApplicationSession.cs` | untouched | untouched | clean |
| `ApplicationSession.cs` | +106/-11 | +47/-15 | **the one genuinely contested file** |
| 8 applet session files | changed | changed | mechanical adaptation |

Ours is *connection* contention (exclusivity leases, one session per connection). Upstream's is
*protocol* ownership (`InitializeCoreAsync` → `InitializeProtocolAsync(IProtocol)`, plus a new
`DisposeAfterInitializationFailure()`). Different concerns in one file.

**An earlier reading of this divergence was wrong and is corrected here.** Upstream's parameterless
`protected ApplicationSession()` initially looked like upstream having *removed* the connection
parameter. It did not: the parameterless form is the **base** state at `46269ffd`, and *this branch*
added `(IConnection connection)`. Upstream never touched the constructor.

#### Which branch is more correct, on the contention axis

Upstream's `DeviceConnectionRegistry` documentation still reads:

> "Normal connections share session ownership; discovery takes a nonblocking exclusive lease."

That is verbatim the root-cause state this ISA opens with — discovery excluded from everything,
sessions excluded from nothing. A search of `origin/yubikit` for any session-contention concept
(`ConnectionInUse`, "one session at a time", "exclusive interface") returns **nothing**.

**The three-line PIV-PIN-destruction sequence still reproduces on `origin/yubikit`.** This branch is
the only place it is fixed, with measured hardware evidence (`SW=0x6D00`). On the contention axis this
branch is strictly more correct and strictly additive; on the protocol axis upstream is ahead. The
merge should take **both**, not choose.

#### Known merge adaptations

1. The 8 applet factories call `InitializeCoreAsync`; upstream renames and reshapes it to
   `InitializeProtocolAsync(IProtocol, ...)` returning `IProtocol`. Mechanical, but touches all 8.
2. Upstream's `DisposeAfterInitializationFailure()` addresses the same family as G5 — cleanup after a
   failed async factory — but only post-construction and with no guard, so it does **not** solve
   constructor-throw stranding. The merged result must end with **one** init-failure cleanup concept,
   not two competing ones.
3. Upstream `75a1a04b` ("remove internal working material before public v2 alpha") **deletes
   `docs/plans/**`** — 7 files, ~51k lines — because they leak local paths. This ISA lives there and
   does leak local paths. Whether it is scrubbed, archived, or dropped at merge is an open question,
   tracked as A2 Q4.

### Dropped, so they are not silently reopened

Linux E1/E2 (operator decision: Linux has no Container ID either, so Phase 14's macOS run exercises the
same degraded tiers) — **but this justification is contested.** The Phase 16 Cato auditor argued the tier
*model* being identical does not make the platform behaviour feeding it identical: udev path stability,
serial availability, permission failures, timing, and interface-removal sequencing all differ on Linux, and
this same ISA elsewhere insists PC/SC and HID behaviour diverge enough to require per-platform runs — which
undercuts using "no Container ID" alone as an equivalence argument. The rebuttal is reasonable and is
recorded rather than dismissed; the residual risk is a Linux-only phantom-event or identity-retention issue
that no current evidence would catch. Re-opening this is a live option for the operator · D3 removal-time exception type (the test does not assert on it) · the 2 transient
Fido2 failures (unreproducible across 3×29/29; recorded honestly as "observed-good, not proven") ·
the dirty worktree `agent-aa7ba443d8eec3e9e` (a different effort).

### Known unresolved, and left that way

The macOS OTP HID fault of Phase 13 was cleared by **restart, not replug**. Input Monitoring/TCC was
falsified as the cause (a persistent policy would survive a reboot), as were Wispr Flow, Karabiner,
orphaned test hosts, and re-enumeration. **No cause was identified, and no attribution is offered.**
The operational discriminator is the presence of `WARNING: Failed opening device`; never pipe
`ykman otp info` through `head`, which lets it fall back to CCID and exit 0 while HID is still broken.

---
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

Hardware evidence now spans macOS (firmware 5.8.0, two-key same-PID rig), Linux (firmware 5.4.3,
one- and two-key runs), and Windows 11 (firmware 5.8.0, two-key same-PID rig, serials 103 and 125).
Windows PC/SC sharing under contention (F3), Windows HID sharing (F2), and Windows typed-transport
connect are now verified on hardware — see Phase 11, which also found and fixed a real Windows OTP HID
open defect. The macOS physical HID FIDO double-open case is closed (Phase 9). Cross-process contention
is unchanged and out of scope. One residual Windows characterization, not a defect: the CCID-held
Management fallback routes through FIDO HID, which Windows admits only to an elevated process, so that
specific fallback requires Administrator on Windows (Phase 11).

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
| 3 | The interface lease belongs to the CONNECTION; CCID and OTP HID are exclusive, while FIDO HID remains shared | `DeviceConnectionRegistry.AcquireConnectionAsync(id, exclusive)`, `PcscYubiKey` (`true`) / `HidYubiKey` (`IOtpHidConnection` true, `IFidoHidConnection` false) |
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
1807 total / 0 failed · resilience 69/69 · formatting clean.

> **Correction (2026-08-06).** This line originally read "`dotnet format --verify-no-changes` 0 errors".
> That command does **not** exit 0 on this repository and could not have done so here: it reports
> `IL2026`/`IL3050` trim-AOT warnings from `src/Tests.TestProject/Program.cs`, a file added 2026-04-02 and
> therefore present when this phase ran. The formatting that was actually clean is the split, severity-
> scoped form used throughout this effort (`whitespace`, `style --severity error`,
> `analyzers --severity error`). See the consolidated note in Phase 6.

### Residual

Hardware validation of the three-line motivating sequence (ISC-1) still requires the rig. Everything above
is no-hardware evidence: the refusals are in-process facts, which is exactly why they are testable without a
YubiKey — but that the SDK now *routes* Management over HID does not by itself re-prove that the PIV session
survives. Phase 1 experiment 4 measured that; a post-change integration run should confirm it end to end.
---

## Status at handoff (2026-07-30, historical snapshot)

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

### Remaining at that handoff (subsequently reconciled below)

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
  not run. **(Phase 12: Windows topology-tier path confirmed by an operator-coordinated insert/remove — see below; the macOS/Linux serial/PID degraded path is still required.)**
- **~~Linux~~ and Windows.** Linux was closed on 2026-08-03 — see Phase 7 below. Windows PC/SC
  sharing semantics and HID open behaviour remain unverified.
- **Windows topology Tier 2**, carried from the composite-merge effort.
- **A `claude`-CLI entry for Fable** in `NAMED_MODEL_ALIASES` — its Copilot-only chain went dark when
  quota ran out, though the CLI transport worked.

### Register status at that handoff

This snapshot is superseded by the Phase 5 row-to-evidence reconciliation and the current
`edge-case-register.md`. It is retained only as history.

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
| Formatting (split, severity-scoped) [^fmt] | clean | clean | matches |
| Discovery invariants | **5/5** | 5/5 | **identical** |

[^fmt]: This row originally recorded `dotnet format --verify-no-changes` as `exit 0` on both platforms.
    Corrected 2026-08-06: the unqualified command exits **2** on this repository because of pre-existing
    `IL2026`/`IL3050` trim-AOT warnings in `src/Tests.TestProject/Program.cs` (added 2026-04-02, so present
    when this phase ran, and unrelated to this branch). The gate this effort actually uses, and which is
    clean, is the split severity-scoped form: `dotnet format whitespace --verify-no-changes`,
    `dotnet format style --verify-no-changes --severity error`, and
    `dotnet format analyzers --verify-no-changes --severity error`. There is no `format` target in
    `toolchain.cs`, so these are manual invocations.
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

The refusal message at that historical run named the contended `pcsc:` interface. It did not and could
not name the applet/session holder; the wording was generalized later for CCID and OTP HID:

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
2. ~~**YubiHsm and YubiOtp integration tests cannot run at all**~~ — `FileNotFoundException: Could not
   load file or assembly 'Xunit.SkippableFact'`. The original note said this was fixed only on
   `origin/yubikit` by `2e381cb1` and absent here because the branch is 34 commits behind.
   **RESOLVED ON THIS BRANCH and re-verified 2026-08-06.** Phase 8's repair added the direct package
   reference to both projects — `Xunit.SkippableFact` is present in
   `src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.IntegrationTests/*.csproj:14` and the YubiOtp equivalent, and
   the `Tests.Shared` reference remains `PrivateAssets=all` by design. Measured on macOS: YubiHsm
   integration **11/11 passed**, YubiOtp integration **10/10 passed**, no `FileNotFoundException`. Both
   projects also pass `test-infrastructure-qa`, which `build` and `test` depend on, so the defect cannot
   silently return. The "34 commits behind" clause no longer applies to this item.

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
- **DeviceId evidence-tier flip** — Windows topology-tier path confirmed on hardware in Phase 12; macOS/Linux serial/PID degraded path still needs an unplug run.
- **Windows** — unchanged. **(Closed in Phase 11.)**
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
suites run in parallel also produced transport contention. The runs were not valid module-result
counts because hardware suites must run sequentially against one physical key, but the contention
signal was retained and later helped confirm the separate-protocol OTP gate gap closed in Phase 5.

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

### Deferred strengthening — RSA-4096 cross-key liveness (non-blocking)

The 2/2 gate proves cross-key correctness and registry isolation through 10 parallel EccP256
signatures per key. It does not yet prove that a tens-of-seconds operation on one physical card
cannot delay work on another. When **two allow-listed firmware-5.7.0+ keys** are available, add:

`Rsa4096Keygen_OnOneKey_DoesNotDelayPivOperationsOnAnotherKey`

The test starts RSA-4096 generation on key A, waits 500 ms and asserts it is still running, then
requires a pre-provisioned PIN-gated EccP256 signature on key B to complete within four seconds.
It drains and validates key A's generation, then repeats with the key roles reversed. The existing
`FindAllAsync_WhileCardBusyWithRsa4096Keygen_CompletesWithoutWaitingForKeygen` supplies the same
long-operation and four-second-bound precedent, but covers discovery on one key rather than liveness
across two keys.

This is deliberate strengthening, not a merge criterion for the current ownership fix. The present
5.4.3 keys cannot run RSA-4096, and RSA-2048 may complete before overlap can be established.

---

## Phase 5 — Documentation and cross-vendor reconciliation (2026-08-04)

Phase 5 reconciles the public contract against the current code, exact standing tests, macOS/Linux
hardware evidence, and the confirmed cross-vendor review. The bulk of the phase is documentation,
but it also contains two production corrections found during reconciliation: OTP HID became exclusive,
and Management/YubiOTP reject unsupported connection types before the base session guard attaches.
Calling this phase documentation-only would hide behavior changes and violate ISC-7.

### Additional production corrections and authentic RED

The RED tests were reapplied to detached `d6d0cc3e` production code and executed through the repository
toolchain; these are reproduced failures, not inferred or invented provenance.

```text
dotnet toolchain.cs -- test --project Core --filter
  "FullyQualifiedName~ConnectAsync_SecondConnectionToHeldOtpHidInterface_IsRefusedBeforePhysicalOpen"

failed ConnectAsync_SecondConnectionToHeldOtpHidInterface_IsRefusedBeforePhysicalOpen
  Assert.Throws() Failure: No exception was thrown
  Expected: typeof(Yubico.YubiKit.Core.Devices.ConnectionInUseException)
```

This is the defect RED for OTP exclusivity. `ConnectAsync_OtpHidConnectionDisposed_InterfaceReopens`
is the lifecycle pin and is not misreported as defect evidence. The implementation makes only
`IOtpHidConnection` exclusive; the pre-existing FIDO double-open pin remains shared.

```text
dotnet toolchain.cs -- test --project Management --filter
  "FullyQualifiedName~CreateAsync_UnsupportedConnection_DoesNotLeaveSessionAttached"

failed ManagementSessionTests.CreateAsync_UnsupportedConnection_DoesNotLeaveSessionAttached
  ConnectionInUseException : This connection already has a live ManagementSession.
    at ConnectionSessionGuard.Attach
    at ProbeSession..ctor

dotnet toolchain.cs -- test --project YubiOtp --filter
  "FullyQualifiedName~CreateAsync_UnsupportedConnection_DoesNotLeaveSessionAttached"

failed YubiOtpSessionTests.CreateAsync_UnsupportedConnection_DoesNotLeaveSessionAttached
  ConnectionInUseException : This connection already has a live YubiOtpSession.
    at ConnectionSessionGuard.Attach
    at ProbeSession..ctor
```

These failures prove the rejected constructor had already attached a ghost holder. Moving supported-type
validation into the base-constructor argument prevents attachment rather than trying to clean up an object
whose constructor never completed.

Focused GREEN on the current tree: the OTP refusal test passed 1/1, the Management ghost-holder test
passed 1/1, and the YubiOTP ghost-holder test passed 1/1 using the same three commands above.

### Cross-vendor finding dispositions

1. **ISC-1 wording corrected.** Interface-scope acquisition knows the contended interface, not the
   applet or call site currently using it. `ConnectionInUseException` therefore names the interface.
   The lower per-connection `ConnectionSessionGuard` does know and name the live session. The previous
   phrase "naming the current holder" overclaimed what interface refusal can know; the criterion and
   consumer docs now state the two scopes separately.
2. **Connection leaks are contract-significant.** The interface lease belongs to the connection and is
   released only by deterministic connection disposal. Missing disposal can retain an exclusive CCID or
   OTP HID lease for the connection lifetime, potentially the process lifetime, and block later opens.
   Migration guidance now says: whoever creates the connection disposes it; direct
   `Session.CreateAsync(connection)` borrows it; `device.Create<App>SessionAsync()` owns its hidden
   connection; use `await using`; keep only one live session per connection and reuse sequentially.
3. **No finalizer backstop.** This is a deliberate ownership choice, not a dismissal of leak risk. A
   finalizer cannot provide the deterministic ordering required between native-handle teardown and lease
   release, and would make ownership mistakes appear to work nondeterministically. The public contract
   requires explicit disposal instead.
4. **Phase 7 and Phase 8 integration counts are not directly comparable coverage metrics.** They were
   produced under different available hardware and filters; Phase 8 also repaired dynamic-skip declarations
   and package references. `test-infrastructure-qa` proves declaration/package correctness. Named standing
   tests and hardware gates carry behavioral evidence. No exact cause beyond those confirmed differences is
   inferred from raw pass/skip totals.
5. **The HID OTP timeout and parallel-suite contention were not discarded.** Together they were evidence
   of the now-confirmed per-protocol gate gap: separate OTP protocol instances could interleave one logical
   multi-feature-report frame on a shared interface. OTP HID acquisition is now SDK-exclusive and pinned by
   `ConnectionOwnershipContractTests.ConnectAsync_SecondConnectionToHeldOtpHidInterface_IsRefusedBeforePhysicalOpen`
   and `ConnectAsync_OtpHidConnectionDisposed_InterfaceReopens`. FIDO HID remains shared.

### Discovery and identity contract

- Serial reads are conditional/on-demand; successful reads are cached by stable interface identity,
  while failed and null reads are retried. Discovery does not read every interface on every scan.
- One unparsed USB CCID marks PID correlation untrusted for all remaining USB interfaces in that scan.
  Topology still groups first; only successful equal serials group afterwards; null serials remain
  standalone.
- `AvailableConnections` is the union of observed interfaces, not proof that every capability is enabled
  over every interface or that concurrent semantic operations are safe.
- The repository retains the originally published object while physical interface identity and
  `AvailableConnections` remain unchanged. Its `DeviceId` is therefore stable for one uninterrupted
  published presence and correlates `Added` with final `Removed`, even across evidence-tier flips. A fresh
  direct scan object can still carry a different evidence-tier-derived ID.
- `FindAllAsync` and `DeviceChanges` now document the common one-key/one-object result, conservative split
  bounds, force-rescan/cache behavior, and no guarantees beyond
  `docs/architecture/device-discovery-guarantees.md`.

Retaining the originally published object has one consumer-visible tradeoff: an equivalent fresh object's
updated metadata/member instances are not substituted while identity and connection flags remain unchanged.
Consumers needing current configuration must query Management data explicitly rather than treating a
repository object refresh as a metadata refresh.

The repository tier-flip/final-removal behavior is pinned by deterministic repository tests. No physical
insert/remove or evidence-tier-flip run was performed on the current macOS rig, and none is claimed. **(A
physical insert/remove run was later performed on Windows hardware — see Phase 12. It confirmed the
no-phantom-event and add/remove-correlation contracts on hardware; the serial↔PID flip itself did not
arise there because Windows resolves same-PID keys by the higher-confidence topology tier, so that flip
path stays unit-pinned only.)**

### Ownership and transport contract

- CCID and OTP HID admit one live connection. FIDO HID remains shared.
- OTP exclusivity protects one logical OTP frame spanning multiple feature reports.
- Management may still try `SmartCard -> HidFido -> HidOtp` on its default path. A held OTP HID interface
  refuses that final acquisition; explicit overrides never fall back.
- Unsupported Management and YubiOTP `IConnection` types are validated before the base session guard
  attaches, pinned by `ManagementSessionTests.CreateAsync_UnsupportedConnection_DoesNotLeaveSessionAttached`
  and `YubiOtpSessionTests.CreateAsync_UnsupportedConnection_DoesNotLeaveSessionAttached`.
- SCP with a held CCID never downgrades to plaintext HID, pinned by
  `IYubiKeyExtensionsTransportTests.CreateManagementSessionAsync_ScpRequestedAndCcidHeld_DoesNotFallBackToPlaintextHid`.

### Edge register disposition

The register now has 21 in-scope rows and exact file/method evidence:

| Tier | Total | Covered | Bounded | Platform gap | Open |
|---|---:|---:|---:|---:|---:|
| P1 | 5 | 5 | 0 | 0 | 0 |
| P2 | 12 | 10 | 2 | 0 | 0 |
| P3 | 4 | 0 | 0 | 3 | 1 |
| **Total** | **21** | **15** | **2** | **3** | **1** |

The two bounded P2 rows are A4 (wire ownership is coordinated per interface; callers serialize
semantic configuration writes across CCID and OTP HID) and D1 (exclusive acquisitions do not wait;
the newcomer is refused immediately and succeeds after disposal). D3 remains open P3 for a
human-coordinated/fake hotplug follow-up. F1 is a platform gap because it specifically requires macOS
hardware; F2 and F3 require unavailable platform evidence. ISC-2 passes.

### Deferred and parked work

- Base reconciliation remains parked; this phase does not merge/rebase the branch.
- Windows PC/SC/HID/topology hardware validation remains deferred.
- RSA-4096 cross-key liveness strengthening is complete on firmware-5.8.0 serials 103 and 125; see the
  final hardware section below.
- Linux standing and two-key gates are complete as recorded in Phases 7-8.

### ISC status

| ISC | Status | Evidence |
|---|---|---|
| ISC-1 | pass | `PivSessionContentionTests` hardware path plus interface/session acquisition pins; wording corrected to the knowledge available at each scope |
| ISC-2 | pass | Register now **23 rows** (F4, F5 added after this phase): every P1/P2 covered or bounded with a pin; zero open rows, zero platform gaps |
| ISC-3 | pass | `DeviceConnectionRegistry` and `ConnectionSessionGuard` are named enforcement points; no wire-sniff convention |
| ISC-4 | pass (on an uncontended host — see below) | macOS discovery is now 5/5 after USB re-enumeration cleared a wedged host IOKit HID state (the earlier 2/5 was not an SDK defect); PIV session contention 5/5, multi-key 7/7 smoke, YubiOtp 10/10, Management green, build 0 errors, formatting clean. Unblocking OTP HID exposed and fixed a leaked-connection defect in `YubiOtpSlotConfigTests`. **Precondition, carried up from Phase 10/13 so the pass label is not overread:** the OTP-dependent gates require the OTP keyboard interface to be openable. The unresolved macOS host fault of Phase 13 can make them unreproducible on a wedged host until a restart clears it; that is a host condition, not an SDK regression, but it means "ISC-4 pass" asserts an uncontended rig |
| ISC-5 | pass | Same-rig Linux before/after delta shows no material scan/session-open regression |
| ISC-6 | pass | `ResetDeviceAsync_WithHidFidoPinnedSession_ThrowsNotSupportedException` passed on hardware with `Transport=HidFido`; `SmartCardBackend_DeviceResetAsync_SendsDeviceResetApdu` passed and pinned INS `0x1F` without resetting hardware |
| ISC-7 | pass | RED evidence is recorded for behavior changes, including reproduced OTP refusal and Management/YubiOTP ghost-holder failures; invariant pins are identified separately |
| ISC-8 | pass | Repository-published identity stability is unit-pinned, documented separately from fresh direct-scan IDs, and the `DeviceId`/serial contract is now written up in `docs/architecture/device-discovery-guarantees.md` (Phase 15). Hardware-confirmed on both paths: Phase 12 Windows topology tier (4 actions → 4 events) and Phase 14 macOS degraded path (7 actions → 7 events, real serial↔PID flip observed). No unit-pinned-only gap remains |

**Phase 5 status: complete.** Architecture Mermaid source and rendered artifacts are refreshed by the
repository render script; public XML docs and human/AI module guidance now expose discovery bounds,
connection ownership, session reuse, and the CCID/OTP-exclusive versus FIDO-shared split.

---

## Phase 6 — Final cross-vendor CodeAudit dispositions (2026-08-04)

The initial final CodeAudit used the router-selected **Anthropic** cross-vendor auditor. Its confirmed
HIGH and MEDIUM findings were addressed below; LOW findings remain deferred or accepted unless the
substantive fix naturally removed them. A follow-up review then found two residual lifecycle defects in
the initial async-disposal correction; their RED/GREEN remediation is recorded separately below.

### HIGH and MEDIUM findings

| Finding | Disposition | Fix and evidence |
|---|---|---|
| H1 — async session disposal skipped derived managed cleanup | **Superseded by follow-up below** | The initial fix invoked `Dispose(disposing: true)` after awaited teardown and covered the successful path, but a throwing `DisposeAsyncCore` still bypassed derived managed cleanup. The follow-up replaces this with guaranteed cleanup inside the shared one-shot lifecycle. |
| M2 — `ProtocolDeviceInfo` documented false connection ownership | **Fixed** | Remarks now state the actual borrowed-connection contract: only the protocol is disposed; the caller retains and must dispose the connection. |
| M3 — `FidoSession` hid `DisposeAsync` and forced synchronous teardown | **Fixed** | The hidden method is removed. `FidoSession.DisposeAsyncCore` now clears its backend and delegates to the base async path. RED observed synchronous `Dispose` instead of `DisposeAsync`; GREEN proves one async connection disposal, zero synchronous disposals, and idempotence. |
| M4 — PIV hardware assertion proved only a constant word | **Fixed and executed** | The assertion requires a non-empty quoted `pcsc:` member identity. `PivSessionContentionTests` passed 5/5 on the current macOS rig, including `ConnectAsync_SecondSmartCardConnection_WhilePivSessionOpen_IsRefused`. |
| M5 — Management SCP transport XML docs were incomplete | **Fixed** | `CreateManagementSessionAsync` now documents SmartCard-only SCP, `NotSupportedException` for explicit HID overrides, forced SmartCard selection without an override, and no plaintext HID fallback when CCID is held. Existing transport-routing tests remain green. |

### Focused verification

TDD RED was captured first for all new disposal tests. After the minimal fixes, focused class runs
passed **94/94** unit tests: Core 3, Oath 17, OpenPGP 5, SecurityDomain 26, Fido2 10, Piv 17, and
Management 16. Hardware integration tests were intentionally not run.

### Follow-up review — one-shot shared-completion disposal

The follow-up review confirmed two remaining findings:

1. **HIGH — failed async teardown skipped managed cleanup.** If `DisposeAsyncCore()` threw while
   disposing an owned connection, control never reached `Dispose(disposing: true)`. OATH/OpenPGP
   secret zeroing and SecurityDomain/FIDO terminal-state cleanup could therefore be skipped.
2. **MEDIUM — virtual cleanup was re-enterable.** Repeated, mixed, or concurrent `Dispose()` and
   `DisposeAsync()` calls could enter derived cleanup more than once. The base `_disposed` boolean
   prevented repeated base work only after virtual dispatch had already re-entered the override, and it
   did not provide a shared completion for concurrent callers.

The RED command was:

```text
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~ApplicationSessionDisposalTests"
```

All **5/5** tests failed for the predicted causal reasons: repeated sync, repeated async, and mixed
disposal observed cleanup count `2` instead of `1`; the concurrent sync loser returned before the
blocked async winner completed; and only the first caller observed the owned-connection async failure,
while later callers returned successfully and managed cleanup had not run on the failing path.

The minimal correction reuses Core's existing `DisposalGate`. Its lease parameter is now optional,
without changing registered-connection behavior. Each `ApplicationSession` owns one gate. A synchronous
winner invokes `Dispose(disposing: true)` once. An asynchronous winner awaits `DisposeAsyncCore()` and
runs `Dispose(disposing: true)` in `finally`, preserving the original async teardown exception when
managed cleanup succeeds. Every later or concurrent sync/async caller observes the same completion and
same exception without re-entering virtual cleanup. `ReleaseConnection` remains independently
idempotent as defense in depth, and `FidoSession` continues to override `DisposeAsyncCore` rather than
hiding `DisposeAsync`.

A final mechanical-hardening RED extended the same Core class from five to seven tests. The focused run
reported **2 failed, 5 passed**: synchronous owned-connection failure left `ThrowIfDisposed` non-terminal,
and a throwing protocol was disposed twice when async cleanup entered the managed `finally` path. The base
managed cleanup now publishes `_disposed` from `finally`, so a failed synchronous teardown is terminal,
and both sync/async paths capture and clear `Protocol` before invoking `Dispose()`. The first teardown
exception therefore remains the gate's shared outcome and protocol disposal is never retried.

The final reviewer RED extended the Core class from seven to eight tests. The focused run reported
**1 failed, 7 passed** because synchronous protocol failure prevented `ReleaseConnection`: the owned
connection disposal count remained `0`, and the live session guard would still refuse a successor.
`Dispose(bool)` now nests release in `finally` beneath terminal-state publication. When release succeeds,
the original protocol exception remains observable through the shared gate; the owned connection is
disposed once, the guard is detached, and a subsequent probe session can attach to the deliberately
reusable tracking fake.

The final async analogue RED extended the Core class from eight to nine tests. The focused run reported
**1 failed, 8 passed** because protocol failure skipped `ReleaseConnectionAsync`; outer managed cleanup
then disposed the owned connection synchronously (`Dispose == 1`, `DisposeAsync == 0`).
`DisposeAsyncCore` now awaits `ReleaseConnectionAsync` in `finally`. On successful release, the original
protocol exception remains the shared outcome, protocol disposal runs once, owned connection disposal is
asynchronous exactly once, outer managed cleanup observes the already-released state, terminal state is
published, and a subsequent probe session can attach.

Focused GREEN evidence totals **82/82** unique unit tests:

| Command | Result |
|---|---:|
| `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~ApplicationSessionDisposalTests"` | 9/9 |
| `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~DeviceConnectionRegistryTests"` | 11/11 |
| `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~ApplicationSessionScpTests"` | 3/3 |
| `dotnet toolchain.cs -- test --project Oath --filter "FullyQualifiedName~OathSessionTests"` | 17/17 |
| `dotnet toolchain.cs -- test --project OpenPgp --filter "FullyQualifiedName~OpenPgpSessionWireTests"` | 5/5 |
| `dotnet toolchain.cs -- test --project SecurityDomain --filter "FullyQualifiedName~SecurityDomainSessionTests"` | 26/26 |
| `dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~FidoSessionTests"` | 11/11 |

Core owns the throwing-cleanup contract and counter-based concurrency matrix. The existing OATH and
OpenPGP real-secret tests continue to prove their actual buffers are zeroed, so duplicate module-local
fault-injection scaffolding was not added. FIDO adds the applet-specific failure regression: an owned
connection's async exception is shared by repeated callers, async disposal is attempted once, and the
session is terminal afterward.

### LOW finding dispositions

- **L6 — registry asymmetry:** harmless and released today; deferred unless a future correctness case
  warrants changing it.
- **L7 — duplicate switch defaults:** minor cleanup only; deferred.
- **L8 — valid-type constructor throw:** currently unreachable; left documented as LOW.
- **L9 — broad `ConnectionInUseException` fallback on a fresh connection:** currently unreachable;
  deferred.
- **L10 — metadata freeze:** accepted and already documented as the repository-object contract.

### Completed full gates after disposal remediation

The formerly deferred full gates were subsequently completed on this working tree:

| Command | Result |
|---|---|
| `dotnet toolchain.cs build` | exit 0; 0 errors; 1 existing `CA2254` warning in `src/Cli.Shared/src/Logging/StaticLoggerExtensions.cs` |
| `dotnet toolchain.cs test` | 1841 passed, 0 failed, 3 skipped |
| `dotnet toolchain.cs -- resilience --fast` | 69/69 passed |
| `dotnet format whitespace --verify-no-changes` | exit 0; clean |
| `dotnet format style --verify-no-changes` | exit 0; clean |
| `dotnet format analyzers --verify-no-changes --severity error` | exit 0; clean |
| `git diff --check` | exit 0; clean |

**Phase 6 status: complete.** Follow-up HIGH/MEDIUM disposal findings are remediated, focused tests and
full gates are green with the one recorded pre-existing analyzer warning, and no LOW-only cleanup was
taken. No merge, rebase, commit, or push is part of this work.

---

## Historical pre-review macOS gate — RSA-4096 cross-key liveness (2026-08-04)

Two allow-listed firmware-5.8.0 devices were attached and selected by the test infrastructure:

- serial 125 — `HidFido, SmartCard`, `UsbAKeychain`
- serial 103 — `HidFido, SmartCard`, `UsbAKeychain`

The exact matching command was:

```text
dotnet toolchain.cs -- test --integration --project Piv --filter
  "FullyQualifiedName~Rsa4096Keygen_OnOneKey_DoesNotDelayPivOperationsOnAnotherKey"
```

Result: **1/1 passed in each of two matching runs**. The first test run took **1 minute 39 seconds**
(toolchain **1 minute 46 seconds**); the final run after adding the hardware trait took **2 minutes
20 seconds** (toolchain **2 minutes 26 seconds**). In each direction it established overlap after
500 ms, completed a PIN-gated EccP256 signature on the other key within the four-second bound, asserted
that RSA-4096 generation was still incomplete after the signature, then drained and validated the RSA
public key. It repeated with the serial roles reversed. At that point cleanup reset both dedicated PIV
applications sequentially; the independent-attempt remediation and current rerun are recorded below.

The standard passing-test console logger does not print `ITestOutputHelper`'s per-direction stopwatch
lines, so no finer timing is claimed than the mechanically observed four-second bounds and complete
test duration. The important liveness evidence is ordering, not an absolute speed claim: each responsive
operation completed while the other physical card's generation remained in flight.

### Independent-review remediation and current hardware gates

The follow-up review found that the prior headline hardware evidence predated OTP HID exclusivity and the
final disposal/lease-release changes. The current code was therefore exercised sequentially against
allow-listed serials 125 and 103, both firmware 5.8.0, without touch, insertion, removal, or user presence.

| Exact filter | Current result |
|---|---|
| `Management --filter "FullyQualifiedName~ResetDeviceAsync_WithHidFidoPinnedSession_ThrowsNotSupportedException"` | 1/1 passed; session asserted `Transport=HidFido`, reset rejected in 858 ms |
| `Piv --filter "FullyQualifiedName~PivSessionContentionTests"` | 5/5 passed in 6.21 s; includes revised M4 `pcsc:` identity assertion |
| `Core --filter "FullyQualifiedName~CompositeDiscoveryIntegrationTests"` | Superseded: was 2 passed / 3 failed in 3.63 s; now 5/5 after USB re-enumeration. See "OTP HID unblocked" below |
| `Piv --filter "FullyQualifiedName~PivMultiKeyContentionTests"` | 3/3 passed in 3m27s; RSA liveness 3m20s, complete toolchain 3m33s |

The discovery failures were not rerun until green. Both OTP keyboard interfaces failed
`IOHIDDeviceOpen` with `0xE00002E2`, so discovery conservatively returned two standalone `HidOtp` rows
plus `ykphysical:125` and `ykphysical:103` with `HidFido, SmartCard`. Conservation and consecutive-scan
stability passed; zero-orphans, completeness-per-PID, and typed-transport-connect failed for that exact
reason. Process inspection found no testhost/vstest runner. IORegistry showed Yubico HID clients from
WindowServer and Wispr Flow; that identifies live native clients but does not prove which client caused
the exclusive-open refusal. A fresh process running only
`ConnectAsync_TypedTransports_OnEveryReturnedDevice_Succeed` failed on its first OTP open, and independent
`ykman --device 103 otp info` and `ykman --device 125 otp info` processes both reported `Failed opening
device`. This rules out progressive connection retention from earlier tests in the class and reproduces
the unavailable native OTP interface outside the SDK process; it does not identify which external client
holds the interface. With user approval, Wispr Flow was then terminated completely and the five-test class
was rerun once; it remained 2 passed / 3 failed with the same `IOHIDDeviceOpen=0xE00002E2` result and
standalone OTP rows. Wispr Flow was reopened afterward. The failed independent `ykman` probes and unchanged
result without Wispr Flow rule out this branch's in-process lease registry and Wispr Flow as the holder;
WindowServer remained the observed native keyboard client. No assertion was weakened, and ISC-4 remains
blocked rather than treating the platform condition as a passing gate.

### OTP HID unblocked: ISC-4 now green, and the Wispr Flow line above is falsified

A later session with the operator physically present resolved this. The keys were unplugged and replugged
(a third, non-allow-listed production key was briefly attached and removed before any test ran). With no
SDK change, `ykman --device 103 otp info` and `ykman --device 125 otp info` both succeeded, and
`Core --filter "FullyQualifiedName~CompositeDiscoveryIntegrationTests"` passed **5/5 in 1.58 s**, including
`ConnectAsync_TypedTransports_OnEveryReturnedDevice_Succeed`. **ISC-4 is verified on macOS.**

The recorded hypothesis above is falsified, and the falsifying evidence is explicit: Wispr Flow was
**running** (PID 29856) during the green run, and had been **terminated** during the red runs. It cannot be
the holder; if anything the correlation is inverted. Supporting facts: `0xE00002E2` decodes to
`kIOReturnNotPermitted`; the SDK opens OTP HID with `options = 0`, i.e. non-seizing
(`MacOSHidFeatureReportConnection.cs:117`) — only FIDO seizes with `0x01`
(`MacOSHidIOReportConnection.cs:150`); no orphaned testhost process existed in either state; and an
independent non-SDK process (`ykman`) failed and later recovered in lockstep with the SDK. The only
variable that changed was USB re-enumeration.

Root cause at the time: **wedged host-side IOKit HID state for those two keyboard interfaces, cleared by
re-enumeration. Not an SDK defect.** Operator remedy: replug the key.

> **CORRECTION (2026-08-06, Phase 10).** The "cleared by re-enumeration" conclusion above is WRONG, and the
> statement that Wispr Flow was exonerated is unsupported. Later the same day, repeated physical replugs
> stopped clearing the condition, and IORegistry named a concrete holder. Do not rely on the paragraph
> above; see "Phase 10 — OTP HID holder identified" below. The SDK-is-not-at-fault part still holds and is
> now better evidenced, but the mechanism and the exoneration were both wrong.

### Defect this unmasked: leaked OTP HID connection in YubiOtpSlotConfigTests

Because OTP HID could not be opened at all, every OTP-HID-dependent test had been silently unexercised.
With the interface working, `YubiOtp` integration went **6 passed / 4 failed**: the first HidOtp test passed
in 698 ms and every later HidOtp open failed in `< 1 ms` with `ConnectionInUseException` on
`hid:4367418413:0006`.

This was a genuine test defect that this branch's OTP HID exclusivity exposed, not a product regression.
All four tests in `YubiOtpSlotConfigTests` created the connection themselves but bound it to a plain local:

```csharp
var connection = await state.Device.ConnectAsync<IOtpHidConnection>();   // test owns it
await using var session = await YubiOtpSession.CreateAsync(connection);  // session only borrows it
```

`Session.CreateAsync(connection)` borrows; only `IYubiKey.Create<App>SessionAsync()` owns. So each test
leaked the connection and held the exclusive OTP HID lease for the process lifetime — exactly the hazard
named in gotcha 2 of `src/Core/CLAUDE.md`. It was harmless while OTP HID was shared and became fatal when
this branch made it exclusive. Fix: `await using var connection = ...` in all four tests; no production
code and no assertion changed. The product contract is confirmed correct — it refused precisely what it
promises to refuse.

A repository-wide sweep for the same pattern found no other occurrence. The remaining matches are
deliberate: `ConnectionOwnershipContractTests` holds a first connection on purpose to pin exclusivity, and
`CompositeYubiKeyTests` asserts throws.

### Re-verification after the fix (macOS, serials 103 and 125, no touch)

| Exact command | Result |
|---|---|
| `test --integration --project Core --smoke --filter "…CompositeDiscoveryIntegrationTests"` | 5/5 in 1.58 s |
| `test --integration --project YubiOtp --smoke` | 10/10; all four HidOtp slot-config tests green |
| `test --integration --project Management --smoke` | All passed; newly reachable `Conn=HidOtp` variants green (`ManagementHidConcurrencyTests` 2 s, `GetDeviceInfo_AllTransports` 420 ms) |
| `test --integration --project Piv --smoke --filter "…PivSessionContentionTests\|…PivMultiKeyContentionTests"` | 7/7 |
| `build` | 0 errors |
| `dotnet format whitespace \| style \| analyzers --verify-no-changes --severity error` | Clean |

`Rsa4096Keygen_OnOneKey_DoesNotDelayPivOperationsOnAnotherKey` is `Slow`-tagged
(`PivMultiKeyContentionTests.cs:193`) and is therefore excluded by `--smoke`; its 3/3 evidence above stands
from the earlier unfiltered run.

---

## Phase 9 — F1 closed on macOS hardware, and it was a real defect (2026-08-06)

Register row F1 (macOS physical HID FIDO double-open) had been accepted as a platform gap for want of a
human-coordinated hardware run. With the operator present it was executed, and the documented contract
did not survive it.

### The defect

`DeviceConnectionRegistry` admits a second FIDO HID connection by design — "FIDO HID remains shared" — and
`ConnectionOwnershipContractTests.ConnectAsync_HidInterface_AllowsConcurrentConnections` pinned that. But
that pin uses fakes, so it only ever proved the in-process lease admits the second connection. On hardware
the second open failed:

```text
IOHIDDeviceOpen = 0xE00002C5   (kIOReturnExclusiveAccess)
  at MacOSHidIOReportConnection.SetupConnection() … MacOSHidIOReportConnection.cs:153
```

Cause was our own code: `MacOSHidIOReportConnection.cs:150` opened FIDO with `0x01`
(`kIOHIDOptionsTypeSeizeDevice`). The lease therefore admitted a connection the platform then rejected, and
the shared-FIDO contract was false on macOS while every fake-based test stayed green. The OTP feature-report
path in the same tree already opened with `0`, so FIDO was also inconsistent with its sibling.

### Canonical adjudication

Both canonical implementations open macOS HID non-seizing, so this was a C# deviation rather than a design
choice to defend:

| Source | macOS FIDO HID open | Evidence |
|---|---|---|
| Rust yubikit (`ykrust-auto` @ `9fe08d9a`) | non-seizing | `crates/yubikit/Cargo.toml:54-55` enables hidapi `macos-shared-device`, which calls `hid_darwin_set_open_exclusive(0)`; hidapi `mac/hid.c` maps `0` to `kIOHIDOptionsTypeNone` and defaults to seize "for backward compatibility" |
| python-fido2 | non-seizing | `fido2/hid/macos.py:292` — `iokit.IOHIDDeviceOpen(self.handle, 0)` |
| C# (before this fix) | **seizing** | `MacOSHidIOReportConnection.cs:150` — `IOHIDDeviceOpen(handle, 0x01)` |

Fix: open with `kIOHIDOptionsTypeNone`. One constant, canonical-aligned, uniform across platforms, and it
also stops the SDK locking other processes out of the key.

### Second finding: shared admission is not shared I/O (new row F4)

With the fix the second open succeeds, but the two-handle transaction test still failed — the FIRST
connection could not read its own response. A single-connection baseline
(`ConnectAsync_SingleFidoHidConnection_CompletesCtapHidInit`) passes, which rules out the probe itself. A
targeted diagnostic then established the mechanism: sending CTAPHID_INIT on handle one and reading on
handle two **passes**. Input reports are delivered to whichever handle's RunLoop runs; two handles do not
demultiplex.

So "FIDO HID is shared" is an admission guarantee only. That is all the Management-over-HID fallback needs,
but a caller driving CTAP on two handles concurrently can receive the peer's frames. Recorded as bounded row
F4 and corrected in `src/Core/CLAUDE.md`, `src/Core/README.md`, and
`docs/architecture/physical-device-model.md`. The misrouting pin passes *because* the behavior is wrong and
will fail if the transport ever demultiplexes.

### Incidental defect: Core integration tests could not use the allow list

`src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/appsettings.json` carried an **empty**
`AllowedSerialNumbers`, and its local `PreserveNewest` copy shadowed the canonical list that
`Tests.Shared` publishes with `CopyToOutputDirectory=Always`. Any `[WithYubiKey]` test in that project
therefore hit `AllowList`'s `Environment.Exit(-1)` and aborted the run — which is why the project's existing
tests enumerate through `YubiKeyManager` and bypass the authorization boundary. The empty file and its
csproj entry were removed so Core inherits the shared 11-serial list, verified in the build output. The
pre-existing decision to leave `CompositeDiscoveryIntegrationTests` un-gated is unchanged and still tracked.

### Evidence

| Command | Result |
|---|---|
| `test --integration --project Core --smoke --filter "…FidoHidSharingIntegrationTests"` | 3/3 |
| `test --integration --project Fido2 --smoke` | 29/29, three consecutive runs |
| `toolchain.cs test` (full unit) | 12/12 projects, 0 failed |
| `toolchain.cs -- resilience --fast` | passed |
| `toolchain.cs build` | 0 errors |

One caveat recorded rather than smoothed over: the first Fido2 integration run after the seize change
reported 27/29. Those two failures were not captured before the rerun and remain unidentified; three
consecutive 29/29 runs followed. Treat FIDO stability on macOS as observed-good, not proven.

Row F1 is now `covered`; F2's platform-divergence claim gains direct evidence (Linux shared FIDO with no
change; macOS refused until corrected). D3 remains the only open row.

The RSA cleanup now attempts both PIV resets independently. If the test body fails, that original
exception remains the thrown outcome; any cleanup failure is attached to its `Data` and written to test
output. If the body passes, one cleanup failure is thrown directly and two are aggregated. The current
3/3 hardware run exercised the success path and restored both PIV applications.

Diagnostic TDD for exclusive interfaces produced authentic RED on both CCID and OTP HID refusal tests:
the old message began `The SmartCard interface ...` and did not contain the required
`exclusive interface '<deviceId>'`. After generalizing the registry message and public XML docs, the
same focused Core command passed 2/2. ISC-6 added invariant coverage rather than a production fix: the
SmartCard reset-APDU unit pin passed 1/1 and the HID-FIDO-pinned hardware test passed 1/1.

Formatting remediation normalized the two previously failing files,
`WithYubiKeyAttribute.cs` and `YubiOtpSessionIntegrationTests.cs`, through the repository formatter.
Whole-workspace whitespace, style, and error-severity analyzer verification are clean:

```text
dotnet format whitespace --verify-no-changes
dotnet format style --verify-no-changes
dotnet format analyzers --verify-no-changes --severity error
```

The unqualified `dotnet format --verify-no-changes` no longer reports final-newline or style errors, but
still exits nonzero on two unchanged trim/AOT warnings (`IL2026`, `IL3050`) in
`src/Tests.TestProject/Program.cs:21`. They are unrelated to this task and were not suppressed or edited.
Final documentation QA validated 55 active files, and `git diff --check` exited clean.

---

## Phase 10 — D3 closed on hardware, and the OTP HID holder identified (2026-08-06)

### D3 closed: hotplug does not strand the exclusive CCID lease

D3 was the register's last `open` row. It was executed with the operator physically removing both
allow-listed keys mid-session.

The risk this effort owns is not that the operation fails when the card is pulled — obviously it does. It
is that the CCID lease could be **stranded**. The lease is released in the connection's disposal path with
no finalizer backstop, so if removal made disposal hang or throw before the release, the interface would
stay marked in-use for the process lifetime and every later open would be refused with
`ConnectionInUseException` — on a key the user had already plugged back in.

`PivHotplugContentionTests.PivSession_KeyRemovedMidSession_FailsBoundedAndDoesNotStrandTheCcidLease`
passed in 1m47s on serials 103/125. It establishes three things: the in-flight PIV call failed within a
bounded window rather than hanging, disposal completed with the card absent, and a subsequent connect
attempt reported something other than `ConnectionInUseException`. The test self-fails with an explicit
"this run proves nothing" message if no removal is observed, so a pass always corresponds to a real
unplug. A first attempt did exactly that and was discarded rather than recorded.

Not captured: the concrete exception type raised at the moment of removal. The test asserts only that a
bounded failure occurred, and the type is surfaced only on the failure path. Capturing it would need
another operator-coordinated run; the test was left exactly as verified rather than edited afterwards.

### Phase 10 correction: the OTP HID root cause recorded in Phase 9 was wrong

Phase 9 recorded that the OTP HID condition was wedged host IOKit state cleared by USB re-enumeration, and
that Wispr Flow had been falsified as a suspect. Both claims are now contradicted by direct evidence.

| Claim in Phase 9 | Later evidence |
|---|---|
| Re-enumeration clears it | Three successive physical replugs did NOT clear it; OTP stayed dead on both keys |
| No identified holder | IORegistry shows `IOUserClientCreator = "pid 29876, Wispr Flow"` holding an `IOHIDLibUserClient` directly on the OTP keyboard interface (`PrimaryUsagePage = 1`, `PrimaryUsage = 6`), alongside the normal `pid 402, WindowServer` event-service client |
| Wispr Flow exonerated | The exoneration rested on presence/absence correlation across two runs. That is weaker than a named IORegistry client, and is withdrawn |

Current state is deterministic rather than intermittent: CCID reads succeed on both keys while every OTP
open fails, `ykman` included. `Karabiner-DriverKit-VirtualHIDDevice` is also resident and is a second
plausible keyboard grabber, so attribution between the two is not yet settled.

What survives from Phase 9 is the part that was independently evidenced: **this is not an SDK defect.** No
testhost process exists when the condition holds, an independent non-SDK process fails identically, and the
SDK opens OTP non-seizing. The mechanism and the exoneration were wrong; the SDK verdict was not.

The decisive next step is to quit Wispr Flow, confirm its `IOHIDLibUserClient` disappears from IORegistry,
and re-probe. That was deliberately not done unilaterally because it is the operator's dictation tool.

### Consequence for ISC-4

The Phase 9 ISC-4 pass (discovery 5/5) is real but carries an unstated precondition: it requires the OTP
keyboard interface to be openable. While a keyboard grabber holds that interface, discovery returns
standalone `HidOtp` rows and the suite reverts to 2 passed / 3 failed. ISC-4 should be read as **passing on
an uncontended host**, not as unconditionally green. This is an environmental precondition, not a code
regression, and it is recorded here rather than left implicit in a green row.

### An operator hypothesis, tested and not reproduced

The operator reported a long-standing pattern that running `git commit` breaks the next integration run.
The mechanism exists on this machine and is worth recording: `~/.gnupg/scdaemon.conf` sets `disable-ccid`,
which routes scdaemon through **PC/SC** — the same channel the CCID tests use — and
`~/.gnupg/gpg-agent.conf` sets `enable-ssh-support`, making gpg-agent the SSH agent. In any repository with
an SSH remote or signed commits, a git operation can wake scdaemon and contend for the card.

It does not reproduce in this repository. Measured directly around a real commit:

| Probe | Before commit | After commit |
|---|---|---|
| CCID 103 / 125 | `PIV version 5.8.0` | `PIV version 5.8.0` |
| `scdaemon` process | absent | absent |

Consistent with `commit.gpgsign=false` locally and globally and an HTTPS remote, so nothing in this repo's
git path reaches gpg-agent. Recorded as a real hazard for differently configured repositories, and as not
the cause of the failures seen here.

### Phase 10 addendum — Wispr Flow exonerated by direct test; OTP cause is UNRESOLVED

The Phase 10 correction above named `pid 29876, Wispr Flow` as the holder of the OTP keyboard interface on
IORegistry evidence. That attribution is now also withdrawn. With the operator's agreement Wispr Flow was
quit completely; its processes are gone and no Wispr Flow `IOUserClientCreator` remains anywhere in
IORegistry. **Every OTP open still fails on both keys, `ykman` included.**

This is the second incorrect attribution in this investigation. Rather than name a third suspect, what is
established and what is excluded is recorded separately.

**Established:**

- The failure is `IOHIDDeviceOpen = 0xE00002E2` = `kIOReturnNotPermitted`. That is a PERMISSION-class
  status, not a busy/contended one. It is a materially different failure from the FIDO double-open result
  earlier in this phase, which was `0xE00002C5` = `kIOReturnExclusiveAccess`. Reasoning that treated the
  OTP condition as "someone holds the interface" was reasoning from the wrong error class.
- It is deterministic in the current state: CCID succeeds on both keys while every OTP open fails.
- It is not an SDK defect. An independent non-SDK process (`ykman`) fails identically, no testhost exists,
  and the SDK opens OTP non-seizing.

**Excluded by direct evidence:**

| Suspect | How excluded |
|---|---|
| This branch's in-process lease registry | `ykman` fails identically with no SDK process running |
| Orphaned test hosts | Process inspection shows only MSBuild node-reuse workers |
| Wispr Flow | Quit entirely; no processes and no IORegistry client remain; OTP still fails |
| Karabiner grabber daemons | Not running (`pgrep karabiner` empty); only the DriverKit extension is resident |
| USB re-enumeration as a cure | Three successive physical replugs failed to restore OTP |

**Not established:** the actual cause. The leading remaining hypothesis is a macOS Input Monitoring
(`kTCCServiceListenEvent`) denial against the process that runs the tests, which is consistent both with
`kIOReturnNotPermitted` and with the keyboard usage page of the OTP interface (`PrimaryUsagePage = 1`,
`PrimaryUsage = 6`). It is unconfirmed: the user TCC database holds no `kTCCServiceListenEvent` rows, the
system database needs root, and no TCC denial appears in the unified log. It also does not explain why OTP
worked earlier in the same session under the same process tree, which is an unexplained contradiction and
is recorded as such rather than smoothed over.

**Decisive tests, both requiring the operator:** run `sudo ykman --device 103 otp info` — success under
root implicates permissions and excludes a holder — or grant Input Monitoring to the terminal application
that runs the tests and re-probe.

**Impact:** OTP-dependent verification (discovery 5/5, YubiOtp 10/10) cannot be reproduced while this
holds. Those results stand as recorded from the window in which the OTP interface was openable, with the
precondition now stated explicitly. No SDK change is indicated.

### Phase 10 addendum 2 — probe methodology corrected, and the root test is not decisive

Two corrections to the investigation method above.

**The `ykman otp info` probe was misused.** `ykman` falls back to CCID when the OTP HID interface cannot be
opened, so the command can print `Slot 1: empty / Slot 2: empty` and exit successfully while the HID open
has failed. Intermediate probes in this investigation piped the output through `head -1`/`head -2`, which
truncated the slot lines and showed only warnings. The correct discriminator is the presence of
`WARNING: Failed opening device`, not the exit status or the slot output.

This does not change the earlier findings, and the corroboration is worth stating: the run recorded as
"OTP works" printed **no warnings at all**, and YubiOtp integration passed 10/10 including HidOtp-pinned
tests, which only pass over a real HID OTP connection. The transition from working to failing is therefore
a genuine state change on the host, not a measurement artifact.

**Running as root does not fix it, and does not exclude TCC.** `sudo ykman --device 103 otp info` produced
output identical to the unprivileged run — the same two `Failed opening device` warnings followed by the
CCID-served slot listing. Root therefore does not restore the HID OTP interface. This is NOT evidence
against the Input Monitoring hypothesis: macOS TCC is evaluated against the responsible application (the
terminal), and `sudo` inherits that context rather than bypassing it. The test is inconclusive for TCC
rather than negative.

The cause remains unresolved, with the exclusions listed in addendum 1 unchanged.

---

## Phase 11 — Windows verification, and a real Windows OTP HID defect fixed (2026-08-06)

The last two register rows, F2 and F3, were platform gaps solely because no Windows rig had run them.
This phase closes both on Windows 11 hardware (firmware 5.8.0, two same-PID keys, serials 103 and 125),
generalizes F4 from a macOS bound to a cross-platform one, and fixes a genuine Windows-only defect the
verification surfaced. All runs used the `dotnet toolchain.cs` runner.

### A prerequisite the platform imposes: FIDO HID needs elevation on Windows

The first F3 run was **non-elevated** and produced two failures, both `UnauthorizedAccessException` opening
the FIDO HID interface:

```
Access denied opening HID device '\\?\HID#VID_1050&PID_0407&MI_01#...'.
Windows denied access to the HID interface. ...
```

This is not an SDK defect. When a PIV session holds the CCID interface, `GetDeviceInfoAsync` /
`CreateManagementSessionAsync` fall back off SmartCard along the documented order `SmartCard → HidFido →
HidOtp`. FIDO HID uses INPUT/OUTPUT reports (`ReadFile`/`WriteFile`), and Windows restricts read/write on
the FIDO top-level collection to elevated processes. So on Windows this specific CCID-held Management
fallback requires Administrator. Re-run under an elevated shell, F3 passed **5/5**, including
`ConnectAsync_SecondSmartCardConnection_WhilePivSessionOpen_IsRefused`, whose message assertion confirms
the Windows PC/SC identity is surfaced as `pcsc:...` under contention exactly as on macOS/Linux. This is
recorded as a Windows characterization, not a code change: weakening or reordering the documented fallback
to dodge elevation was explicitly not done.

### F2 — Windows HID sharing, and F4 is cross-platform, not macOS-specific

`FidoHidSharingIntegrationTests` passed **3/3** elevated. The load-bearing result is that
`SendOnFirst_ReceiveOnSecond_RevealsReportMisrouting` **passed on Windows too**: a CTAPHID_INIT sent on the
first FIDO handle was read back on the second. The handoff hypothesis was that Windows might demultiplex
(making the test fail, a good outcome). It does not. Windows behaves like macOS — two FIDO handles are
admitted but input reports are not routed per handle. **F4 is therefore a cross-platform bound**, not a
macOS quirk: drive CTAP over one FIDO connection at a time on every platform. The test was left asserting
misrouting; it was not "fixed" to stay green.

### Defect: Windows OTP HID feature connection opened the keyboard collection read/write

`CompositeDiscoveryIntegrationTests` failed 1/5 on `ConnectAsync_TypedTransports_OnEveryReturnedDevice_Succeed`,
and this one **was** an SDK defect — reproducible even elevated:

```
Access denied opening HID device '\\?\HID#VID_1050&PID_0407&MI_00#...\KBD'. ...
   at HidDDevice.OpenHandleWithAccess(...)          # GENERIC_READ | GENERIC_WRITE
   at HidDDevice.OpenReadWriteHandle()
   at HidDDevice.OpenReportConnection()
   at HidDDevice.OpenFeatureConnection()
   at WindowsHidFeatureReportConnection..ctor(...)
   at HidYubiKey.CreateOtpConnection()
```

The YubiKey OTP interface is a **keyboard** top-level collection (`MI_00 ... \KBD`). The OTP protocol over
Windows uses only HID **feature reports** — `WindowsHidFeatureReportConnection` calls exclusively
`HidD_GetFeature`/`HidD_SetFeature`, which are IOCTLs and succeed on a **zero-access** handle. But
`HidDDevice.OpenFeatureConnection()` routed through `OpenReportConnection()` →
`OpenReadWriteHandle()`, opening with `GENERIC_READ | GENERIC_WRITE`. Windows refuses read/write on the
system keyboard collection even for an elevated process (anti-keylogger restriction), so OTP HID could not
be opened at all on Windows — which would make this branch's central "OTP HID is exclusive" contract
untestable and OTP-over-HID unusable on the platform.

**RED, for the predicted reason.** The failure was an access-denied on the read/write open of the keyboard
collection, while the constructor's metadata probe — which opens the *same* device path with
`DESIRED_ACCESS.NONE` (line 191) — had already succeeded. That is direct proof a zero-access handle opens on
that path and a read/write handle does not, isolating the desired-access flag as the cause rather than
sharing, elevation, or enumeration.

**Fix.** `src/Core/src/Native/Windows/HidD/HidDDevice.cs`: split the two report-open paths by the access
they actually need. `OpenIOConnection()` (FIDO input/output reports) keeps `GENERIC_READ | GENERIC_WRITE`;
`OpenFeatureConnection()` (OTP feature reports) now opens with `DESIRED_ACCESS.NONE`. Feature-report IOCTLs
need no read/write access, so this is sufficient and it sidesteps the keyboard restriction.

> **Claim RETRACTED — it is false (2026-08-06).** This paragraph originally ended: "This matches the legacy
> Yubico .NET SDK, which likewise opens the feature connection with zero desired access." The Phase 16 GPT-5.5
> reviewer could not verify it from this repository. A v1 checkout at `netsdk-ref/Yubico.NET.SDK` settles it —
> **against us**:
>
> ```csharp
> // v1 Yubico.Core/src/Yubico/PlatformInterop/Windows/HidD/HidDDevice.cs
> public HidDDevice(string devicePath)
>     _handle = OpenHandleWithAccess(DESIRED_ACCESS.NONE);            // line 38 — metadata probe
> public void OpenIOConnection()
>     _handle = OpenHandleWithAccess(GENERIC_READ | GENERIC_WRITE);   // line 51
> public void OpenFeatureConnection()
>     _handle = OpenHandleWithAccess(DESIRED_ACCESS.GENERIC_WRITE);   // line 56  <- NOT NONE
> ```
>
> v1 opens the feature connection with **`GENERIC_WRITE`**. Our `NONE` is a **divergence, not parity**. The
> `NONE` at v1 line 38 is the constructor metadata probe, filling exactly the same role as ours — the likely
> origin of the misreading, and the same overclaim shape the reviewer caught in the "probe proves sufficiency"
> comment.
>
> The fix is **not** being reverted on this evidence. `NONE` passed Windows hardware 10/10 including
> `CalculateHmacSha1`, which exercises `HidD_SetFeature`, and the review confirmed by enumeration that every
> reachable feature-report call site uses those IOCTLs and never `ReadFile`/`WriteFile`. Our open-then-dispose
> ordering is also safer than v1's dispose-then-open, which is left holding a disposed handle if the reopen
> fails. But the honest counterweight is that `GENERIC_WRITE` has years of field exposure across countless
> Windows configurations while `NONE` has one run, on one machine, on one firmware. Choosing between them is
> deferred to canonical Rust/Python and Windows HID research (Phase C, item C1).

### GREEN and standing gates after the fix (Windows, elevated, serials 103/125)

| Gate | Result |
|---|---|
| `toolchain.cs build` | 0 errors |
| `CompositeDiscoveryIntegrationTests` | 5/5 (was 4/5) |
| `PivSessionContentionTests` (F3) | 5/5 |
| `FidoHidSharingIntegrationTests` (F2) | 3/3 |
| `YubiOtp` integration `--smoke` | 10/10, incl. `CalculateHmacSha1_WithKnownKey...` over **HidOtp** — proves OTP feature reports work end-to-end on Windows now |
| `resilience --fast` | 69/69 |
| full Core unit suite | 740/740 (2 skipped) |
| `dotnet format whitespace \| analyzers --severity error` | clean |

`dotnet format style --verify-no-changes --severity error` still exits 2, but only on pre-existing native
P/Invoke naming (`IDE1006` on `kern_return_t`, `udev_device_get_parent`, `udev_device_get_syspath` in
`Native/MacOS` and `Native/Linux`) — not the changed file. The fix touches only `HidDDevice.cs`, which is
clean.

This satisfies ISC-7 for the fix (RED for the predicted reason, then GREEN) and leaves ISC-4's standing
gates green on a third platform. The register's last two platform gaps are closed.

## Phase 12 — E1/E2 DeviceId tier flip confirmed on Windows hardware (2026-08-06)

E1 (no phantom incumbent event on inserting a second same-PID key) and E2 (sibling-removal and
final-removal correlation) were "covered" by deterministic repository unit tests only
(`YubiKeyDeviceRepositoryCompositeTests`), with an explicit "no physical hotplug claim." This phase adds
direct hardware evidence via an operator-coordinated insert/remove of two same-PID firmware-5.8.0 keys on
Windows 11 (elevated), observing `YubiKeyManager.DeviceChanges` live.

### Method

A file-based monitoring harness (`experiment_e1e2_tierflip.cs`, run outside the repo tree) subscribed to
`YubiKeyManager.DeviceChanges` before `StartMonitoring(1s)` and logged every event with a UTC timestamp,
`DeviceAction`, `IYubiKey.DeviceId`, and `AvailableConnections`. The operator performed four physical
actions one at a time; each expected event was confirmed before advancing. The harness is an experiment
seam, not a committed test — hotplug correlation cannot run unattended in CI.

### Observed event stream — 4 actions, 4 events, zero phantoms

| # | Action | Event | DeviceId |
|---|---|---|---|
| 1 | Key A inserted (alone) | Added | `ykphysical:topology:255c4df9-…` |
| 2 | Key B inserted (E1) | Added | `ykphysical:topology:db4f2653-…` |
| 3 | Key A removed (E2) | Removed | `ykphysical:topology:255c4df9-…` |
| 4 | Key B removed (E2, final) | Removed | `ykphysical:topology:db4f2653-…` |

- **E1:** inserting B emitted exactly one `Added` for B; the incumbent A emitted **no** `Removed`/`Added`
  and kept its DeviceId. No phantom incumbent event.
- **E2:** removing A emitted exactly one `Removed` for A; the survivor B emitted **no** event and kept its
  DeviceId. The final removal of B emitted exactly one `Removed` whose DeviceId equalled B's earlier
  `Added` (event #2 ↔ #4, and #1 ↔ #3) — exact add/remove correlation.

### Finding: Windows resolves same-PID keys by the topology tier, so the serial↔PID flip does not arise

Both keys were resolved to `ykphysical:topology:<uuid>`, i.e. the **topology** evidence tier, not the
serial or PID tier. The E1/E2 unit tests *force* a serial↔PID flip because their synthetic
`KeyInterfaces` carry only serial+PID evidence; real Windows hardware exposes USB topology, a
higher-confidence tier that resolves each key **independently of its sibling**. Consequently each key's
DeviceId is sibling-independent and stable across the insert/remove of the other key — there is no tier
flip to absorb on this rig. The behavioral contracts E1/E2 exist to protect (one event per physical
action, no phantom incumbent/survivor event, correlated add/remove by stable DeviceId) hold exactly.

This is a strengthening of the identity guarantee, not a contradiction: the no-phantom-event outcome the
unit tests reach *via* diff-stability across a forced flip is reached on Windows *without* a flip at all.
The serial↔PID flip absorption path itself therefore remains **unit-pinned only** and is not claimed as
hardware-exercised. ISC-8's identity-stability contract is satisfied on hardware; the register's E1/E2 rows
and coverage summary are updated accordingly, with the topology-tier caveat recorded explicitly rather than
overclaiming that the flip path was walked.

### A Windows pass does not transfer — macOS/Linux runs still required

This is important and was flagged in review: E1/E2 is **platform-divergent by construction**, so a Windows
pass is not a cross-platform pass. `CompositeDeviceMerger` tier 1 is Windows **Container ID** topology
evidence, which `ProtocolDeviceInfo` documents as `null` "always on macOS and Linux"; the merger's own
comment states that absent topology "degrades to exactly the macOS/Linux semantics." Windows produced the
`ykphysical:topology:<uuid>` identities above from a code path the other two platforms lack, while
macOS/Linux fall through to the serial/PID tiers — precisely the tiers E1/E2's flip question is about.

So the Windows run confirms the topology branch and nothing more. The **macOS/Linux run is still required**
and arguably matters more: it exercises the degraded serial/PID path where a phantom incumbent add/remove is
most plausible, and `docs/architecture/device-discovery-guarantees.md:202-206` already records macOS/Linux
HID-to-HID topology as unimplemented and the Windows topology tier as validated only at seam level.
Sequencing: the macOS run must wait until the OTP fault is resolved, because that fault distorts the
discovered interface set. Do not substitute this Windows result for the macOS/Linux runs.

Caveat, same as the Phase 11 READ-FIRST banner: this is an operator-coordinated Windows-session
observation, not independently reviewed. It is lower-risk than the `HidDDevice` fix because Phase 12 changed
**no production code** — it only observed `DeviceChanges` during physical insert/remove — but the event
stream was not re-captured by a second party.
---

## Phase 13 — macOS OTP fault resolved by restart; Input Monitoring falsified (2026-08-06)

A machine restart cleared the macOS OTP HID fault. `ykman --device 103 otp info` and `--device 125` both
print the slot listing with **no** `WARNING: Failed opening device` line, which is the discriminator
established in addendum 2.

### The hypothesis this kills

**Input Monitoring (`kTCCServiceListenEvent`) is falsified.** TCC is persistent policy; a denial survives a
reboot. This did not. The leading hypothesis carried through addenda 1 and 2 is therefore wrong, and it is
the fourth attribution in this investigation to fail. What the restart establishes is a property, not a
culprit: the condition lived in **transient kernel or daemon HID state that survived USB re-enumeration but
not a restart**. That specific shape also explains the three earlier negative results — replug did not clear
it because re-enumeration does not reset that state, and quitting Wispr Flow did not clear it because the
state was not owned by that process.

The cause is still not identified, and no further attribution is offered. What is now known with evidence:

| Property | Evidence |
|---|---|
| Not an SDK defect | Independent `ykman` failed identically; no testhost present; SDK opens OTP non-seizing |
| Not a permissions/TCC policy | Cleared by restart with no configuration change |
| Not a user-space process holding the interface | Wispr Flow quit entirely, client gone from IORegistry, still failed |
| Not cured by re-enumeration | Three physical replugs failed |
| Cured by restart | This phase |

Operator remedy is therefore **restart**, not replug. Recorded as a known-recoverable host condition.

### Verification after the restart, and of the merged Windows work

This run also completes the macOS-side verification of the Phase 11 Windows commits, which had been pulled
but only built. All on serials 103/125, firmware 5.8.0.

| Gate | Result |
|---|---|
| `Core --filter "…CompositeDiscoveryIntegrationTests"` | **5/5**, no skipped interfaces, no `IOHIDDeviceOpen` errors |
| `YubiOtp --smoke` (unit + integration) | **10/10** integration, incl. all four HidOtp slot-config tests and `CalculateHmacSha1` over HidOtp |
| `Core --filter "…FidoHidSharingIntegrationTests"` | **3/3** — F1 admission pin, baseline, and the F4 misrouting pin all still hold |
| `toolchain.cs test` (full unit) | 12/12 projects, 0 failed |
| `toolchain.cs -- resilience --fast` | passed |
| `Piv --smoke` contention + multi-key | **7/7** |
| `toolchain.cs build` | 0 errors |

The Phase 11 `HidDDevice` change is Windows-only by file and shows no macOS regression across these gates.
That is now a measurement rather than the inference recorded at pull time. It does **not** discharge the
cross-vendor review of Phase 11, which remains a merge gate.

### ISC-4

ISC-4's precondition recorded in Phase 10 stands but is now actionable: discovery 5/5 requires an openable
OTP interface, and when that fails on macOS the remedy is a restart. Re-measured 5/5 in this phase.

This phase is numbered 13 because Phase 12 was taken concurrently by the Windows E1/E2 session; the two
were authored in parallel on different machines and both are retained.

---

## Phase 14 — E1/E2 confirmed on the macOS degraded path, exercising the real serial↔PID tier flip (2026-08-06)

Phase 12 confirmed E1/E2 on Windows but recorded an explicit caveat: on Windows both keys resolve through
the tier-1 **topology** (Container ID) path, so the serial↔PID flip the unit tests force was never
exercised. This phase closes that gap on macOS, where topology evidence does not exist and discovery
degrades to the serial/PID tiers.

### The tier flip actually occurred — this is what Windows could not test

macOS identity is evidence-dependent, and the run captured the transition directly:

| Rig state | Incumbent identity | Tier |
|---|---|---|
| Two same-PID keys present | `ykphysical:103` | serial (needed to disambiguate siblings) |
| One key present (103 alone) | `ykphysical:pid:0407` | PID (unique group, no serial evidence required) |

So inserting the second same-PID key forces the incumbent's evidence tier to flip from PID to serial. That
is the exact scenario `UpdateCache_SiblingSamePidKeyArrives_IncumbentEmitsNoRemovedOrAdded` pins
deterministically, and it had never been observed on hardware on any platform.

### Method

File-based harness (`e1e2_tierflip.cs`, run outside the repo tree) subscribing to
`YubiKeyManager.DeviceChanges` before `StartMonitoring(1s)`, logging UTC timestamp, `Action`, `DeviceId`
and `AvailableConnections`. Operator performed each physical action one at a time; every event was
confirmed before advancing. Experiment seam, not a committed test — hotplug correlation cannot run
unattended in CI.

### Observed stream — 7 actions, 7 events, zero phantoms

| # | Action | Event | DeviceId | Incumbent event? |
|---|---|---|---|---|
| 1 | Remove production key (setup) | Removed | `ykphysical:25555459` | none |
| 2 | Remove 125 | Removed | `ykphysical:125` | none for 103 |
| 3 | Insert 125 | Added | `ykphysical:125` | none for 103 |
| 4 | Remove 125 | Removed | `ykphysical:125` | none for 103 |
| 5 | Remove 103 (last key) | Removed | `ykphysical:103` | — |
| 6 | Insert 103 alone | Added | **`ykphysical:pid:0407`** | — |
| 7 | Insert 125 (2nd same-PID) | Added | `ykphysical:125` | **none for the incumbent** |

Event 5 confirms **E2 final-removal correlation**: the removal reported the same DeviceId previously
published, not a re-derived one. Events 4 and 7 confirm **E2 sibling removal** and **E1 sibling arrival**:
the incumbent stayed silent through both, and through the PID→serial tier flip at event 7.

### Retention contract demonstrated on hardware

After event 7 the live monitoring repository had never re-emitted the incumbent, so it still published
`ykphysical:pid:0407`. An independent fresh scan in a separate process at the same moment reported:

```text
FRESH SCAN: 2 device(s)
  ykphysical:103   [HidFido, HidOtp, SmartCard]
  ykphysical:125   [HidFido, HidOtp, SmartCard]
```

Fresh-scan identity and retained published identity therefore differ by design: the repository keeps the
originally published object across an evidence-only tier flip rather than churning consumers. That contract
was previously unit-pinned only; this is its first hardware demonstration.

### Caveat

Events 2 and 3 were an operator reseat of 125 performed to identify the keys by serial, with `ykman` run in
between, so external processes were opening interfaces during that window. They are treated as corroborating
only. Events 4-7 are the deliberate, uncontended sequence and carry the findings.

E1/E2 now have hardware confirmation on **both** the Windows topology tier and the macOS serial/PID
degraded tier. A Linux run remains nice-to-have rather than required: Linux shares the macOS property of
having no Container ID, so it exercises the same degraded tiers now covered here.

### Phase 14 follow-up — the identity contract is undocumented at the API surface

Phase 14's most consumer-relevant consequence is not the invariant it confirmed but the contract it exposed
as unwritten. One physical key was observed carrying `ykphysical:pid:0407` and `ykphysical:103` in the same
session, differing only by what else was plugged in, and the live repository simultaneously published a
different identity from an independent fresh scan. Both behaviours are correct and deliberate. Neither is
documented where an API consumer would look:

- `IYubiKey.DeviceId` (`src/Core/src/Abstractions/IYubiKey.cs:28`) carries **no XML documentation**.
- The per-platform tier model (Windows topology → serial → PID; macOS/Linux serial → PID) is described in
  `docs/architecture/device-discovery-guarantees.md` for merger maintainers, not at the API surface.
- `device-discovery-guarantees.md:41` promises a "stable interface `DeviceId`", which is the per-interface
  id and a different concept from the physical `ykphysical:*` id — a terminology collision worth resolving.

Tracked as a documentation work item in `HANDOFF.md`. It is not a defect in behaviour and does not gate the
hardware evidence, but it is a public-API gap on the exact property consumers are most likely to use as a
durable key, and it should be closed before merge consolidation.

---

## Phase 15 — Canonical adjudication of OTP HID exclusivity and the identity model (2026-08-06)

Two canonical questions, both bearing on contracts this branch asserts. Sources: Rust `ykrust-auto`
@ `9fe08d9a` (`/Users/Dennis.Dyall/Code/y/yubikey-manager-rust-auto`) and the Python `yubikit` /
`python-fido2` trees. No hardware involved.

### Q1 — Is OTP HID exclusivity canonical? **No. It is ours.**

| Source | Finding | Evidence |
|---|---|---|
| Rust | No in-process exclusivity of any kind | `HidOtpConnection::new` (`crates/yubikit/src/platform/hidapi.rs`) calls `api.open_path` and stores the handle. No mutex, no registry, no already-open check. The only "exclusive" token in that file is `CtapHidCommand::Lock` (0x04), which is CTAPHID channel locking for FIDO and unrelated |
| Rust (device layer) | No connection registry | `platform/device.rs` and `core.rs` contain no connection-ownership lock; the only `RwLock` is an override for firmware version (a test seam) |
| Python | No locking | `yubikit/core/otp.py` `OtpConnection` is a bare ABC declaring `receive`/`send`; the macOS backend opens with `IOHIDDeviceOpen(handle, 0)` and adds no exclusivity |

**This SDK's OTP-HID-exclusive rule is a deliberate strengthening beyond canonical, not parity with it.**
The recorded justification remains sound and is unaffected: one logical OTP exchange spans multiple feature
reports, so two protocol instances on one interface could interleave a single logical frame. Canonical does
not need a guard because neither implementation produces two concurrent in-process OTP handles — Rust's
ownership model makes it unnatural and Python's usage is single-connection — whereas this SDK exposes a
public `ConnectAsync` that any caller can invoke twice.

What must change is only the **provenance**, not the behaviour: no document should imply canonical mandates
OTP HID exclusivity. Where this SDK's exclusivity is described, it should be attributed to the interleaving
hazard and to this effort's own measurements. Contrast with F1, where the seizing FIDO open genuinely
contradicted canonical and the fix was to converge; here we intentionally diverge, and that is defensible
because our API surface admits a hazard canonical's does not.

Residual risk accepted: a caller that legitimately wants two OTP HID handles is refused by this SDK and
would not be by canonical. No such use case is known, and the register's D1 row already bounds the
behaviour (immediate refusal, success after disposal).

### Q2 — What is canonical's durable device identity? **The serial. Not a synthesized id, and not the firmware version.**

Canonical mints **no** `DeviceId` string. `list_devices` documents the model directly
(`platform/device.rs:694-695`):

> "When only one device is present per USB Product ID the merge is trivial; multiple devices sharing a PID
> are matched by firmware version and serial."

The same pair is the correlation key elsewhere: removal-wait logic compares
`d.info.serial == my_serial && d.info.version == my_version` (`device.rs` ~296, ~313, ~380), and
`merge_from` prefers the record carrying a serial, then the higher firmware version (~214-217).

Implications for the open documentation gap:

1. The planned guidance — *"use the serial, not the DeviceId, as a durable key"* — is **canonically
   supported**, not merely our opinion.
2. ~~Canonical pairs serial with firmware version.~~ **CORRECTED — see the correction below. Version is a
   tie-breaker, not a match key, and is excluded from this SDK's identity documentation.**
#### Correction to Q2 — firmware version is a tie-breaker, not a match key

The Q2 heading originally read "serial plus firmware version" on the strength of the `list_devices` doc
comment quoted above. The code does not support that reading, and the overstatement is corrected here
rather than left to propagate into the API documentation.

`merge_from` consults the version only **after serials have already matched**, to decide which metadata
record to retain:

```rust
// Prefer the info with a serial number, or with a higher firmware version.
if self.info.serial.is_none() && other.info.serial.is_some()
    || self.info.serial == other.info.serial && other.info.version > self.info.version
```

That is record selection, not identity. Three further reasons version is unsuitable as an identity
component, and why this SDK excludes it:

| Evidence | Implication |
|---|---|
| `src/YubiOtp/src/YubiOtpSession.cs` NEO workaround takes the higher of the Management and OTP versions | The version differs per applet on one physical key |
| Rust `management.rs:1462` — `version = Version(3, 0, 0); // Guess NEO` | Canonical sometimes guesses it |
| `CompositeDeviceMerger` contains no version logic | Grouping already works on three platforms without it |

The serial is already unique, and YubiKey 5 firmware cannot be updated in the field, so a version component
would add no uniqueness and no refresh signal — only a composite key that can vary by which interface
answered. The identity documentation states explicitly that firmware version is not part of identity.

3. This SDK's tiered `ykphysical:*` identity is an **SDK construct with no canonical counterpart**. That is
   legitimate — .NET consumers want a stable object key, which Rust's ownership model does not require —
   but it means its stability properties are ours alone to define and document. Phase 14 proved they are
   surprising (`ykphysical:pid:0407` → `ykphysical:103` on sibling insert), which is exactly why the
   documentation item is a merge-blocking concern rather than a nicety.

---

## Phase 16 — Cross-vendor review of the production code (2026-08-06)

This phase discharges merge gates **G1** and **G2**. It also invalidates a methodological assumption that
had held since Phase 5.

### The prior "cross-vendor" audits were same-vendor

`~/.claude/MEMORY/VERIFICATION/cato-findings.jsonl` records four Cato runs against this ISA on 2026-08-04.
Every one names the same auditor:

```
anthropic/google-vertex/claude-opus-5@default [premium]
```

That is the same vendor — and the same model — as the authoring harness. The cause is the recorded
invocation flag `--current-vendor openai`, which declares the *author* to be OpenAI and therefore routes the
audit to Anthropic. The ISA was authored by Claude, so the correct flag is `--current-vendor anthropic`.
Cato's own rule is "never silently run same-vendor review"; the flag defeated it.

**Consequence:** until this phase, no part of this effort had received a genuine opposite-family review. The
four prior verdicts remain useful — they found real problems — but they cannot be cited as cross-vendor
evidence. Gates G1 and G2 were therefore the first true outside look at the production code.

### Method

| Gate | Target | Reviewer | Verdict |
|---|---|---|---|
| G1 | `6289c774` — Windows `HidDDevice` feature-report access split | `github-copilot/gpt-5.5`, read-only `Reviewer` subagent | **concerns** |
| G2 | Phase 3-4 enforcement core (35 files, +771/-216 in `src/*/src/`) | `github-copilot/gpt-5.5`, read-only `Reviewer` subagent | **concerns** |

Codex CLI and the premium `openai/gpt-5.6-terra` tier were both unavailable (OpenAI account quota
exhausted), so both reviews ran through GitHub Copilot, which is a tier below the strongest available
OpenAI model. Reviewers were told the branch is 34 commits behind `yubikit` so base drift would not be
misreported, and were given the two documented non-goals.

### What the review cleared

These were checked and found sound — recorded because a clean result is evidence too:

- **No TOCTOU in the registry.** Check-and-claim is atomic under `InterfaceOwnership._sync`
  (`DeviceConnectionRegistry.cs:147-150`, `179-188`). The exclusive lease is taken before physical connect.
- **No sham guard.** `IsInterfaceInUse` is only a pre-skip optimization for discovery; the enforcement is
  the `TryAcquire*` claim, so the pre-check being advisory is not a hole.
- **No security or memory-rule violation** introduced by the diff. The changed enforcement files handle no
  PIN/PUK/key/SCP material; the `SequenceEqual` uses found in `DeviceInfo.cs` compare non-secret version
  bytes.
- **`DESIRED_ACCESS.NONE` is sufficient for every reachable feature-report path.** The reviewer enumerated
  the call sites (`WindowsHidFeatureReportConnection.GetReport`/`SetReport`, `HidYubiKey.CreateOtpConnection`
  and its discovery provider, `OtpHidConnection`, `OtpHidProtocol`, `ProtocolDeviceInfo`,
  `DeviceInfoReader.ReadOtpPageAsync`, `ManagementSession` OTP version, `Management.OtpBackend`,
  `YubiOtp.OtpHidBackend`) and confirmed all use `HidD_GetFeature`/`HidD_SetFeature`, never
  `ReadFile`/`WriteFile`.
- **`OpenIOConnection` correctly keeps `GENERIC_READ | GENERIC_WRITE`** — the FIDO path genuinely uses
  `ReadFile`/`WriteFile`. Access level is unrelated to the F4 demultiplexing problem.
- **Lease lifecycle is otherwise well covered**: connect/wrap failure disposes the lease, registered
  disposal releases it in a `finally`, and `DisposalGate` makes double dispose idempotent.

### Finding 1 — native handle leak on a failing constructor (REAL, FIXED)

`WindowsHidFeatureReportConnection` and `WindowsHidIOReportConnection` both did:

```csharp
_hidDDevice = new HidDDevice(path);   // opens a native handle
_hidDDevice.OpenFeatureConnection();  // throws -> constructor never completes
```

A constructor that acquires a resource and then throws leaves nothing for the caller to dispose: the object
never finishes construction, so no `using`, `finally`, or factory `catch` can reach it. The native handle
leaks for the process lifetime. This sits directly on the path `6289c774` modified.

**Fix.** Both types gained an internal seam taking an already-constructed `IHidDDevice`, with the report-open
wrapped so a failure disposes the device and rethrows.

**RED, for the predicted reason** (`catch` blocks temporarily removed, both tests present):

```
failed ... IOReportConnection_WhenIOOpenThrows_DisposesTheDevice (9ms)
  the device handle leaked: the failing constructor never disposed it
failed ... FeatureReportConnection_WhenFeatureOpenThrows_DisposesTheDevice (0ms)
  the device handle leaked: the failing constructor never disposed it
```

GREEN after restoring the fix: 3/3, including a success-path test asserting the device is **not** disposed,
so the leak fix cannot silently become a use-after-dispose. The device is faked, so these run on every
platform; **Windows hardware re-verification is still owed** and is queued for the Windows machine.

### Finding 2 — session guard stranded by a failing derived constructor (REAL, FILED NOT FIXED)

`ApplicationSession`'s base constructor calls `ConnectionSessionGuard.Attach(connection, this)`
(`ApplicationSession.cs:57`). Derived constructors then do work that can throw, and the factories construct
**outside** their `try`:

```csharp
var session = new ManagementSession(connection, scpKeyParams);  // outside the try
try { await session.InitializeAsync(...); return session; }
catch { await session.DisposeAsync(); throw; }
```

If the derived constructor throws after `base(...)` has attached, no session object exists to dispose, so
`Detach` never runs. Reachable throw sites confirmed by reading the code:
`PcscProtocol.cs:51` calls `_connection.SupportsExtendedApdu()` during construction — a method on the
**public** `ISmartCardConnection` interface, so caller-provided implementations may throw — and
`ManagementSession.cs:258` contains an explicit `?? throw new InvalidOperationException()`.

**Blast radius, scoped by reading both paths rather than assuming the worst:**

| Path | Outcome |
|---|---|
| Owned (`IYubiKey.Create<App>SessionAsync`) | **Safe.** `YubiKeyConnectionExtensions.cs:243` disposes the connection on any failure, releasing the interface lease; the `ConditionalWeakTable` entry dies with the connection |
| Borrowed (`Session.CreateAsync(callerConnection)`) | **Defective.** The caller keeps the connection alive, so the slot retains a dead half-constructed session and that connection is permanently refused with "This connection already has a live `<X>`Session" when no session exists |

Severity **warning, not critical**: it strands the per-connection guard, never the interface lease, and in
the most likely trigger (device removed mid-construction) the connection is already unusable, so poisoning
it costs little. It bites when the connection stays healthy — a caller-supplied `ISmartCardConnection` whose
`SupportsExtendedApdu()` throws transiently.

**Filed, not fixed.** The correct fix touches every session factory (Management, YubiOtp, Oath, Piv, Fido2,
OpenPgp, SecurityDomain, YubiHsm) and must skip detaching when the throw is `ConnectionInUseException` —
in that case another session legitimately holds the slot and clearing it would be a worse bug. That is a
change to the enforcement core deserving its own branch and a full gate re-run, not a late edit at a merge
gate. **This is now a merge-blocking decision item, not a silent deferral.**

### Finding 3 — two findings rejected, and the prompt was at fault

The reviewer reported that `ConnectionInUseException` fails to name both the contended interface *and* the
live session, at `DeviceConnectionRegistry.cs:179` and `ConnectionSessionGuard.cs:58`.

**Rejected.** Phase 6 already adjudicated exactly this: the registry cannot know which applet holds an
interface, so interface scope names the interface and the connection-scoped guard names the session. The
review prompt asserted the exception "is supposed to name the contended interface AND the live session
holding it", which overstates ISC-1 — the prompt, not the code, was wrong. Recorded because an audit finding
caused by a bad prompt is a failure mode worth naming: a reviewer will faithfully validate a false premise.

### Finding 4 — claims weakened (no code change)

- **Legacy-SDK parity is unverifiable here.** See the correction note in Phase 11. Retained as provenance,
  not evidence.
- **"The metadata probe proves a zero-access handle is sufficient" was an overclaim.** The probe exercises
  `CreateFile` + `HidD_GetPreparsedData`/`HidP_GetCaps` only, never `HidD_GetFeature`/`HidD_SetFeature`. The
  conclusion holds; the stated proof did not reach it. The comment in `HidDDevice.OpenFeatureConnection` now
  states the scope of its own evidence.

### Finding 5 — FIDO "admission is not concurrency" was missing from public docs (FIXED)

The caveat existed in `src/Core/CLAUDE.md` but a search of public XML documentation returned nothing, so no
SDK consumer could learn it. `IFidoHidConnection` now documents that the interface is shared, that admission
is not a concurrency guarantee, that two handles do not demultiplex, and that CTAP must be driven over one
connection at a time.

### Finding 6 — test gap accepted, not closed

Exclusive acquisition is pinned sequentially but never raced, so a regression separating "check count" from
"increment count" could pass the suite while admitting two exclusive holders. The atomicity was verified by
reading the lock, not by a racing test. Recorded as a known gap; a barrier-based parallel acquisition test
belongs with the Finding 2 branch, which touches the same code.

### Dispositions

| # | Finding | Severity | Disposition |
|---|---|---|---|
| 1 | Windows HID constructor handle leak | warning | **Fixed** + 3 unit pins; Windows hardware re-run owed |
| 2 | Session guard stranded on derived-ctor failure (borrowed only) | warning | **Filed** — merge-blocking decision, own branch |
| 3 | `ConnectionInUseException` naming | warning | **Rejected** — bad prompt premise; Phase 6 adjudicated |
| 4 | Parity + "probe proves" overclaims | info | **Claims weakened** |
| 5 | FIDO concurrency caveat absent from public docs | warning | **Fixed** in `IFidoHidConnection` |
| 6 | No raced test for exclusive acquisition | info | **Accepted gap**, deferred to the Finding 2 branch |

---

## Phase 17 — G5 fixed: binding moved out of the constructor (2026-08-06)

Discharges the last blocking gate. The plan was audited to a `pass` verdict by
`github-copilot/gpt-5.5` across three rounds before any code was written; the first design was
returned `fail` on a critical race, which is recorded below because the near-miss is the useful part.

### The fix

`ApplicationSession` bound itself in the base constructor. Derived constructors then do work that can
throw, and every factory constructed **outside** its `try`, so a constructor failing after the base had
bound left nothing able to unbind it — the object never finished construction, so no `using`, `finally`
or factory `catch` could reach it.

RED, for the predicted reason:

```
ConnectionInUseException : This connection already has a live FailingSession.
```

The connection was refused by a session that threw in its constructor and does not exist.

Binding now happens in `protected static ApplicationSession.Construct`, after the constructor returns:

```csharp
var session = create();                                  // may throw: nothing is bound yet
try { ConnectionSessionGuard.Attach(connection, session); }
catch { session.Dispose(); throw; }                      // holder untouched
```

### Two designs rejected as unsafe, for one shared reason

| Design | Why it fails |
|---|---|
| Compare the holder before/after construction, clear if changed | Loses a race: another session can bind between the peek and ours. Ours is then refused, the holder has changed, and the cleanup **evicts a live session** |
| Filter on `ConnectionInUseException`, clear otherwise | `EnsureSupportedConnection` runs as an *argument* to `base(...)`, so it throws **before** binding ever happened. The cleanup would clear a slot we never took |

**The shared reason:** any cleanup keyed on the *connection* cannot know whether *we* bound, because the
failure destroyed the only reference to our session. Binding where the session reference exists removes
the question rather than answering it. The first of these was caught by the Cato auditor; the second was
found while verifying the fallback and was **not** reported by the audit.

### Why binding cannot move later still

A one-line alternative existed: move binding into the shared `InitializeCoreAsync`, which all 8 sessions
call. It is wrong. Derived `InitializeAsync` implementations issue their applet SELECT **before** calling
it — `OathSession` calls `SelectAsync(ApplicationIds.Oath, ...)`, `ManagementSession` calls
`GetVersionAsync(...)` — so binding there would refuse the second session only after the first session's
state had already been destroyed.

It nearly passed review: **every existing test would have stayed green**, because they all assert
`ThrowsAsync` on the async factory, which would still throw — just too late to matter.

Verified as a precondition that no session constructor performs wire I/O: `SupportsExtendedApdu()` is
`smartCardDevice.Kind == PscsConnectionKind.Usb`, and `OtpHidProtocol`'s constructor only assigns fields.

### The omission guard

Binding is no longer automatic, so a future session type that skipped `Construct` would run unguarded —
fail-open, in the exact area this effort exists to protect. `InitializeCoreAsync` now verifies the session
is the registered holder and throws otherwise. It immediately caught two existing test doubles
constructing directly; both were routed through `Construct` as production does, so the pre-existing
refusal contracts still hold rather than being weakened.

### Gates

| Gate | Result |
|---|---|
| New unit pins | 5/5 — including a barrier-synchronised concurrent-construction race test and the omission guard |
| Full unit suite | 12/12 projects |
| Resilience | pass |
| Core integration (smoke) | **25/25**, all 5 discovery invariants |
| Piv integration (smoke) | **75/75**, including `GetDeviceInfoAsync_WhilePivSessionHasVerifiedPin_DoesNotClobberSessionState` and `SecondSession_OnOneLiveConnection_IsRefused` |
| YubiOtp integration (smoke) | **10/10** |
| Management integration (smoke) | **40/40** |
| Build / formatting / docs-qa | clean |

Hardware: serials 103 and 125, macOS. `--smoke` skips `RequiresUserPresence` and `Slow`, so the
removal-dependent `PivHotplugContentionTests` did not run — no operator was present to unplug. Its D3
evidence stands from Phase 10 and is unaffected by this change.

### Known merge adaptation

Upstream renames `InitializeCoreAsync` to `InitializeProtocolAsync(IProtocol, ...)`, so the omission guard
relocates at merge. Upstream also has `DisposeAfterInitializationFailure()`, which addresses the same
family but only post-construction and with no guard; the merged result should converge on **one**
init-failure cleanup concept rather than two.
