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
