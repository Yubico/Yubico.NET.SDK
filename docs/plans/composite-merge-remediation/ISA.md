---
task: Composite-merge remediation — Phases 1 (RED harness) + 2 (core fixes)
branch: yubikit-composite-merge
phase: execute
date: 2026-07-28 (Phase 1), 2026-07-29 (Phase 2)
plan: docs/plans/composite-merge-remediation/PLAN.md
---

# ISA — Composite-Merge Remediation

## Evidence Ledger — Phase 1

Evidence rule applied (PLAN.md, Owner decisions item 3): **defect vectors** must fail against
pre-change code FOR THE PREDICTED REASON (asserted failure mode, recorded verbatim below);
**invariant/bound pins** may pass — they pin contracts and are never counted as fix evidence.
Zero production-code changes in this phase (`git status`: only test files + this ISA).

### Files delivered

| File | Content |
|---|---|
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/CompositeDeviceMergerVectorTests.cs` | 21 merger unit vectors (18 pins, 3 defect vectors) — new file rather than extending `FindYubiKeysPidMergeTests` because merger vectors are pure descriptor-level and need none of that file's FindYubiKeys-level fakes |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysFaultInjectionTests.cs` | 5 FindYubiKeys fault-injection pins via scripted constructor fakes (identity cache convergence/eviction/rename + deterministic self-contention reproduction) |
| `src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Devices/CompositeDiscoveryIntegrationTests.cs` | The 4 hard-`Assert.Single` tests generalized to 5 key-count-agnostic invariants |
| `docs/plans/composite-merge-remediation/ISA.md` | This ledger |

### 1. Merger defect vectors — RED, predicted reason confirmed

Run: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~CompositeDeviceMergerVectorTests"`
→ `total: 21, failed: 3, succeeded: 18, skipped: 0`. Verbatim failure lines:

**D1 — Cross-key transient shape B** (`Merge_Defect_CrossKeyShapeB_TwoTripleKeysDisjointHidNoCcidNoSerials_MustStayStandalone`):

> Cross-key transient shape B (premise 4b): the merger fused key A's FIDO and key B's OTP (both 0x0407, no CCID, no serials) into 1 composite(s): ykphysical:pid:0407=[fido-keyA|otp-keyB]. observed != expected must route to the serial path; null serials must stay standalone.

Predicted RED reason (hasSmartCard==false bypasses the bespoke triple guard) — CONFIRMED. This one
IS fixed by the tier-3 generalized guard (observed != expected → serial path).

**D2 — Pigeonhole deduction, 0x0407 pair, 5/6 serials** (`Merge_Defect_TwoTripleKeysFiveOfSixSerialsKnown_OrphanIsAttributedByPigeonhole`):

> Pigeonhole deduction (0x0407 pair, 5/6 serials): the null-serial OTP orphan uniquely fills key B's only missing slot but was left standalone; got 3 devices: ykphysical:111=[ccid-a|fido-a|otp-a]; ykphysical:222=[ccid-b|fido-b]; otp-b(HidOtp).

Predicted RED reason (current merger leaves the orphan standalone) — CONFIRMED.

**D3 — Pigeonhole deduction, 0x0403 pair, 3/4 serials** (`Merge_Defect_TwoDualKeysThreeOfFourSerialsKnown_OrphanIsAttributedByPigeonhole`):

> Pigeonhole deduction (0x0403 pair, 3/4 serials): the null-serial FIDO orphan uniquely fills key B's only missing slot but was left standalone; got 3 devices: ykphysical:111=[fido-a|otp-a]; otp-b(HidOtp); fido-b(HidFido).

Predicted RED reason — CONFIRMED.

### 2. Merger pins — declared as pins (all GREEN; never fix evidence)

| Pin | Contract pinned |
|---|---|
| `Merge_SingleTripleKeyFullVisibilityNoSerials_MergesByPid_Pin` (0x0407, 0x0116) | Full-visibility single triple key merges by PID, no serial |
| `Merge_SingleDualInterfaceKeyFullVisibilityNoSerials_MergesByPid_Pin` (0x0403, 0x0405, 0x0406) | Full-visibility single dual-interface key merges by PID |
| `Merge_SingleInterfacePid_StandsAloneWithoutCompositeWrapper_Pin` (0x0401, 0x0402, 0x0404, 0x0120/SKY) | Single-interface PIDs never get a composite wrapper |
| `Merge_TwoSamePidTripleKeysAllSerialsKnown_GroupsBySerial_Pin` / `…DualKeys…` | Same-PID pairs with full serial evidence group per serial |
| `Merge_TwoTripleKeysBothMissingSameInterfaceTypeSerial_StaysConservativelySplit_Pin` | Deduction ambiguity (2 orphans of one type, 2 candidates) stays split — must SURVIVE Phase 2 |
| `Merge_ComplementaryPartialMasquerade_MisattributionIsRepresentableAndBounded_Pin` | **Epistemic bound** (PLAN.md): key A anchored {OTP,FIDO} + key B visible only as unread CCID merges as one key — misattribution representable, bounded, heals conditionally; unfixable by merge logic |
| `Merge_EpistemicBound_ComplementaryPartials_TwoDualKeysOneInterfaceEach_MergeIsRepresentable_Pin` | **Epistemic bound, shape A** (two 0x0403 keys, one complementary interface each, no serials, observed == expected): the cross-key merge is representable in this window and heals conditionally (first scan with complete visibility, serial, or topology evidence). **Disposition:** the Phase-1 PRD listed this shape as a defect vector; the audited PLAN's epistemic bound governs; reclassified defect→pin by orchestrator disposition (2026-07-28). Its initial RED run (verbatim: "the merger fused interfaces of two physical 0x0403 keys into 1 composite(s) … ykphysical:pid:0403=[fido-keyB\|otp-keyA]") is retained as *characterization* of the bound, not as fix evidence |
| `Merge_TwoSamePidTripleKeysNoSerialsFullVisibility_ConservativeSplit_Pin` / `…DualKeys…` | Serial-less same-PID pair: conservative split (macOS/Linux platform bound). Contains the commented Phase-3 topology-vector TODO (no skipped placeholder added, per repo rule) |
| `Merge_ReconfiguredKeyReenumeratedUnderNewPid_GroupsByCurrentPidTruth_Pin` | 0x0407→0x0403 reconfiguration: grouping follows current PID truth |
| `Merge_OneOfTwoKeysReconfigured_DifferentPidsNoSerialsNeeded…_Pin` | One-of-two reconfigured → PID counts drop to 1, trivially distinguishable |

### 3. FindYubiKeys fault-injection pins (all GREEN across 4 consecutive runs)

Run: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~FindYubiKeysFaultInjectionTests"`
→ `total: 5, failed: 0, succeeded: 5` (×4 runs).

| Pin | Contract pinned |
|---|---|
| `FindAllAsync_ScriptedIdentityFailureOrphans_SameInstanceHealsWhenRetrySucceeds_Pin` | Scripted read failure orphans on scan 1; same instance re-reads ONLY the failed interface on scan 2 and heals (cache hit proven by unchanged connect count on a cached interface) — Phase 0 finding 2 |
| `FindAllAsync_InterfaceDisappearance_EvictsIdentityCacheEntries_Pin` | Absent interfaces evict their identity-cache entries; replugged key with failing reads splits instead of reusing stale identity |
| `FindAllAsync_PcscReaderRenameBetweenScans_OldEntryMissesAndSuccessfulRereadHeals_Pin` | Reader-name suffix change = cache miss + one re-read; grouping stays complete |
| `FindAllAsync_PcscReaderRenameWithFailingReread_OrphansConservativelyWithoutStaleServe_Pin` | Rename + failing re-read: renamed CCID orphans conservatively; stale serial is NOT served across the rename |
| `FindAllAsync_SixIdentityReadsAgainstFourWorkerAdmission_TwoSkipWithoutConnecting` | See item-3 outcome below |

**Item-2 rename-cache finding (Phase 0 finding 4): PIN, not a defect.** The identity cache is keyed
by per-interface DeviceId; a rename is indistinguishable from new hardware, so the correct behaviors
are exactly what current code does: old entry self-evicts as absent, new name is re-read, and on a
failing re-read the interface orphans conservatively rather than being grouped from stale identity.
No stale serving, no leak, no misgrouping observed. Cost of a rename: one re-read.

### 4. Item-3 outcome — scan-1 self-contention REPRODUCED deterministically (no production seam required)

`FindAllAsync_SixIdentityReadsAgainstFourWorkerAdmission_TwoSkipWithoutConnecting` (GREEN pin):
on a two-key same-PID 0x0407 rig, one scan issues 6 identity reads against the process-wide
FOUR-worker `DiscoveryWorkerAdmission`. The admission is nonblocking (`TryAcquire` → skip, not
queue): with the four admitted connects held open, the remaining two reads deterministically throw
`DiscoveryReadSkippedException` from `ProtocolDeviceInfo.StartSharedRead` (ProtocolDeviceInfo.cs:153-154)
**without ever connecting** (asserted: exactly 4 connects, 6-way conservative split).

Root-cause identification for Phase 0 findings 1 & 5:

- The "aborted: interface gained a live connection" log line in `DiscoveryIdentityReader` is emitted
  for EVERY `DiscoveryReadSkippedException`, but the exception has (at least) three distinct causes:
  (a) worker-admission saturation, (b) discovery/session lease contention, (c) device without
  `IDiscoveryConnectionProvider`. The Phase-0 shared-mode scan-1 signature (exactly 2 aborts on a
  6-interface rig) matches cause (a) — **admission saturation, i.e. discovery self-contention on its
  own worker gate** — not an actual live connection. The log wording misattributes the cause.
- Corroborating incidental evidence: during harness development, the identity-cache pins on a
  6-interface rig with *instantly-succeeding* fakes nondeterministically orphaned up to two
  interfaces via the same mechanism (observed: a 4-device scan-1 result where 2 was expected).
  The pins were moved to a 4-interface (2×0x0405) rig for determinism; the saturation pin covers the
  6-read case deterministically.
- The plan's alternate hypothesis — an identity read arriving while a prior ABANDONED read still
  holds the per-interface discovery lease — is **not independently reachable between discovery
  reads**: `ProtocolDeviceInfo` single-flights per (interfaceId, ConnectionType), so a later read of
  the same interface JOINS the in-flight read (observing `TimeoutException`, not a skip), and
  distinct interfaces hold distinct leases. Lease-busy skips therefore require a *session* holding
  the interface, which is the documented in-use path, not self-contention.

**Phase 2 guidance derived:** (a) exempt or serialize identity reads with respect to the four-worker
admission (the plan's scheduling tune) — the saturation pin above becomes the RED→GREEN vector by
changing its assertion from `TotalConnectCalls == 4` to `== 6`; (b) give
`DiscoveryReadSkippedException` a cause discriminator so the log stops misattributing admission
saturation to "gained a live connection". No new production seam is required to pin (a); (b) is a
diagnostics improvement, not a seam.

### 5. Integration invariants — compiled, run on the attached rig

Run: `dotnet toolchain.cs -- test --integration --project Core --filter "FullyQualifiedName~CompositeDiscoveryIntegrationTests"`
→ `Total tests: 5, Passed: 2, Failed: 3`.

⚠️ Rig deviation: the PRD said 2-key rig (103/125), but the attached rig had a THIRD 0x0407 key —
serial 25555459, the Phase-0 production key, plus reader "…OTP+FIDO+CCID 02". The invariants are
key-count-agnostic by design and handled 3 keys correctly (expected key count derived as 3 from the
raw USB oracle). Results below are 3-key data.

| Invariant | Result | Verbatim evidence |
|---|---|---|
| Conservation (every enumerated interface exactly once) | **PASSED** | — (consistent with the plan: "interface count is always right") |
| Zero orphans when idle | **RED** (predicted) | `Orphaned interfaces: expected 3 physical device(s) from the enumerated USB interface set but discovery returned 8. Devices: ykphysical:25555459(HidOtp, SmartCard); pcsc:Yubico YubiKey OTP+FIDO+CCID(SmartCard); hid:4360895504:0001(HidFido); hid:4360893090:0006(HidOtp); hid:4360893089:0001(HidFido); hid:4360893154:0006(HidOtp); pcsc:Yubico YubiKey OTP+FIDO+CCID 01(SmartCard); hid:4360893152:0001(HidFido)` |
| Completeness per ExpectedConnectionsForPid | **RED** (predicted) | `Incomplete grouping: expected interface-set multiset [HidFido, HidOtp, SmartCard, HidFido, HidOtp, SmartCard, HidFido, HidOtp, SmartCard] but discovery returned [HidFido, HidFido, HidFido, HidOtp, HidOtp, HidOtp, SmartCard, SmartCard, SmartCard]. Devices: pcsc:Yubico YubiKey OTP+FIDO+CCID(SmartCard); hid:4360895504:0001(HidFido); hid:4360893090:0006(HidOtp); hid:4360893089:0001(HidFido); hid:4360893154:0006(HidOtp); pcsc:Yubico YubiKey OTP+FIDO+CCID 01(SmartCard); pcsc:Yubico YubiKey OTP+FIDO+CCID 02(SmartCard); hid:4360893152:0001(HidFido); hid:4360895506:0006(HidOtp)` |
| Stability across two consecutive scans on one manager | **RED** (predicted) | `Grouping unstable: scan 1 returned 8 device(s) [ykphysical:25555459(HidFido, SmartCard); pcsc:Yubico YubiKey OTP+FIDO+CCID(SmartCard); hid:4360893090:0006(HidOtp); hid:4360893089:0001(HidFido); hid:4360893154:0006(HidOtp); pcsc:Yubico YubiKey OTP+FIDO+CCID 01(SmartCard); hid:4360893152:0001(HidFido); hid:4360895506:0006(HidOtp)] but scan 2 returned 5 [ykphysical:25555459(HidFido, SmartCard); ykphysical:103(HidFido, HidOtp, SmartCard); hid:4360893090:0006(HidOtp); ykphysical:125(HidFido, SmartCard); hid:4360895506:0006(HidOtp)].` — note scan 2 visibly HEALING toward complete grouping (Phase 0 finding 2, cache convergence) |
| Typed connects on every returned device (single-key-rig compatibility) | **PASSED** | — |

The three RED invariants fail exactly where Phase 0 predicted (fresh-process single-scan orphaning);
they are the Phase-2/Phase-4 RED→GREEN acceptance vectors on hardware.

### 6. Suite health and gates

- Full Core unit suite: `total: 694, failed: 3, succeeded: 688, skipped: 3` — the ONLY failures are
  the three intended defect vectors D1–D3; the 3 skips are pre-existing.
- `dotnet toolchain.cs -- build --project Core`: Succeeded, 0 warnings, 0 errors.
- `dotnet format --verify-no-changes`: exit 0 (only the pre-existing IL2026/IL3050 warnings in
  Tests.TestProject).
- `git status`: only the two new unit-test files, the modified integration-test file, and this ISA.
  `src/Core/src/**` untouched.

### Deviations / flags

1. **Shape-A tier-3 tension — RESOLVED by orchestrator disposition (2026-07-28).** Phase 1 flagged
   that the PRD's shape-A defect vector (assert standalone) contradicted the plan's tier-3 guard
   (observed == expected → merge) and Epistemic bound (same shape cited as unresolvable). The
   audited PLAN governs: shape A was reclassified defect→pin
   (`Merge_EpistemicBound_ComplementaryPartials_TwoDualKeysOneInterfaceEach_MergeIsRepresentable_Pin`,
   see pins table). Phase 2 needs no guard change for this shape; the pin must SURVIVE Phase 2.
2. **Rig had 3 keys, not 2** (25555459 attached alongside 103/125). Invariants are count-agnostic;
   captured results are 3-key. Note: discovery reads and the typed-connect invariant opened
   read-only connections on all attached keys, including 25555459 — same read-only surface Phase 0
   used, but worth the owner's awareness.
3. **Identity-cache pins use a 2×0x0405 rig** (4 reads) instead of 2×0x0407 (6 reads) for
   determinism, because 6 concurrent reads nondeterministically trip the 4-worker admission skip —
   itself the finding recorded in item 4. The 6-read case is pinned deterministically by the
   saturation vector.
4. **Self-contention hypothesis refined**: the plan's "abandoned-read lease" interleaving is masked
   by single-flight joining and is not independently representable; the reproducible mechanism is
   worker-admission saturation (item 4). No production seam needed for Phase 2's RED vectors.
## Evidence Ledger — Phase 2 (2026-07-29)

Four scoped changes, each proved per the evidence rule. Production diff confined to
`src/Core/src/Devices/`: `CompositeDeviceMerger.cs` (changes 1–2), `ProtocolDeviceInfo.cs` +
`DiscoveryIdentityReader.cs` + `DiscoveryReadSkippedException.cs` (change 3). Test diffs:
`FindYubiKeysFaultInjectionTests.cs` (admission vectors + two deduction-updated pins),
`CompositeDeviceMergerTests.cs` (one superseded legacy pin).

### Change 1 — Generalized tier-3 guard

`CanMergeByPidWithoutSerial` now merges a PID-unique group without serial evidence ONLY when the
observed connection set exactly equals `ExpectedConnectionsForPid(pid)`; the bespoke triple-shape
check is gone. Partial observations fall to the serial/deduction path.

- **RED** (Phase-1 D1, verbatim): `Cross-key transient shape B (premise 4b): the merger fused key
  A's FIDO and key B's OTP (both 0x0407, no CCID, no serials) into 1 composite(s):
  ykphysical:pid:0407=[fido-keyA|otp-keyB]. observed != expected must route to the serial path;
  null serials must stay standalone.`
- **GREEN**: merger vector class `total: 21, failed: 0, succeeded: 21`.
- **Survival**: shape-A epistemic-bound pin (observed == expected still merges), all PID-class
  pins, reconfiguration pins — all green in the same run.
- **Superseded legacy pin (flagged)**: pre-existing
  `CompositeDeviceMergerTests.Merge_SeriallessMultiInterfaceSamePid_MergesByPid` pinned exactly the
  unguarded premise-4(b) shape (two HID, no CCID, PID-merge) — the defect the plan's guard fixes
  ("This FIXES the previously unguarded 0x0407 two-HID-no-CCID shape"). Updated in place to
  `Merge_PartialSeriallessSamePid_TwoHidNoCcid_StaysConservativelySplit` with a comment recording
  the supersession.

### Change 2 — Pigeonhole deduction with type-count closure

New `MergeSamePidBySerialWithDeduction` (merger stays pure/static): within one same-PID group,
serial evidence anchors keys; a null-serial orphan is attributed only when exactly ONE anchored key
is missing the orphan's connection type AND, for every type in the PID's expected set, the visible
same-PID interface count does not exceed the anchored-candidate count. Ambiguity stays standalone.
Serial disambiguation now runs per PID class (a physical key has exactly one PID at a time), which
also removes the previous cross-PID same-serial grouping — flagged as a deliberate refinement.

- **RED** (Phase-1 D2, verbatim): `Pigeonhole deduction (0x0407 pair, 5/6 serials): the null-serial
  OTP orphan uniquely fills key B's only missing slot but was left standalone; got 3 devices:
  ykphysical:111=[ccid-a|fido-a|otp-a]; ykphysical:222=[ccid-b|fido-b]; otp-b(HidOtp).`
- **RED** (Phase-1 D3, verbatim): `Pigeonhole deduction (0x0403 pair, 3/4 serials): the null-serial
  FIDO orphan uniquely fills key B's only missing slot but was left standalone; got 3 devices:
  ykphysical:111=[fido-a|otp-a]; otp-b(HidOtp); fido-b(HidFido).`
- **GREEN**: same 21/21 run.
- **Survival**: deduction-ambiguity pin (2 orphans / 2 candidates stays split — closure catches
  it), serial-less pair pins (no anchors → no deduction), epistemic masquerade pin — all green.
- **Updated pins (deduction changes their scenario by design)**: two FindYubiKeys pins had frozen
  pre-deduction "orphan conservatively" outcomes that the unique-candidate deduction now heals in
  scan 1: `…ScriptedIdentityFailureOrphans_SameInstanceHeals…` →
  `…ScriptedIdentityFailure_DeducedIntoAnchoredKey_AndRereadOnNextScan_Pin` (cache contract —
  failure not cached, re-read next scan, cached interface untouched — still asserted), and
  `…RenameWithFailingReread_OrphansConservatively…` →
  `…RenameWithFailingReread_RereadsAndDeducesWithoutStaleServe_Pin` (no-stale-serve still proven
  via the mandatory re-read connect count; attribution now reached via deduction over current-scan
  evidence). Both updated pins declared here as pins.

### Change 3 — Identity reads wait for worker admission + skip-cause discriminator

`ProtocolDeviceInfo.ReadBoundedAsync` gained `waitForWorkerSlot` (default false):
`DiscoveryIdentityReader` passes true, so identity reads await `DiscoveryWorkerAdmission.AcquireAsync`
(new; async semaphore wait) instead of nonblocking-skipping; the caller's existing 2s budget bounds
the wait. The metadata path is UNCHANGED (nonblocking `TryAcquire`, skip on saturation), and the
admission bound itself is preserved — at most four concurrent native reads, hung calls cannot
multiply workers. `DiscoveryReadSkippedException` now carries `DiscoveryReadSkipCause`
(`NoDiscoveryProvider` / `InterfaceLeaseHeld` / `WorkerAdmissionSaturated`), the message includes
it, and `DiscoveryIdentityReader` logs `skipped ({Cause})` — the misattributing "aborted: interface
gained a live connection" wording is gone. Phase-1 hooks (a) and (b): RESOLVED.

- **RED** (captured against stashed pre-change production, verbatim):
  - `Identity reads must WAIT for a worker slot, not skip: only 4 of 6 interfaces ever reached a
    connect (admission saturation skipped the rest).`
    (`FindAllAsync_SixIdentityReadsAgainstFourWorkerAdmission_WaitForSlotsInsteadOfSkipping`)
  - `The excess identity reads must wait for a slot (not skip): they never connected after workers
    freed.` (`FindAllAsync_SaturatedWorkersBeyondBudget_IdentityDegradesToNull_BoundPreserved`)
- **GREEN**: fault-injection class `total: 6, failed: 0, succeeded: 6` — three consecutive runs.
- **Bound preserved** (new vector, same run): with all four workers hung, exactly 4 connects at
  scan end (never 5+), waiting reads degrade to null on budget exhaustion (conservative six-way
  split, no stall), and connect after the workers free (waiting, never skipped).
- **Diagnostics delta (binding proof)** — Phase-0 harness rebuilt against the fixed Core,
  `dotnet run -c Release -- fresh 20` (fresh instance per scan, no caches, live rig):

  | Failure reason (identity reads) | Before (Phase 0, diag-fresh.log) | After (diag-fresh-after.log) |
  |---|---|---|
  | "aborted"/skipped (admission self-contention) | 68 | 0 |
  | Budget timeout | 2 | 0 |
  | Transient failure (retry path) | 1 | 0 |
  | Total reads degraded to serial-unknown | 69 | **0** |
  | Distinct groupings across 20 scans | multiple, heavy orphaning | **1** (both keys complete, 20/20) |
  | Scan latency | — | 187–1135 ms |

  Caveat: the before-log includes an 11-scan 3-key window plus a 2-key window (both failing); the
  after-run is the 2-key rig (103/125; the Phase-0 production key was detached before this run).
  The after-state is zero failures and one stable grouping across all 20 fresh-process scans.

### Change 4 — In-scan retry: NOT NEEDED (superseded by admission wait)

Disposition recorded per the "no machinery without a RED" rule. Post-change-3 failure classes:
(1) admission contention — eliminated (reads wait; diagnostics show 0 remaining aborts);
(2) transient PC/SC failures — already covered by DiscoveryIdentityReader's existing 3-attempt
in-scan retry loop (150ms/300ms backoff); (3) budget timeout (busy card) — deliberately NOT
retried per existing design (retrying extends the stall); (4) `InterfaceLeaseHeld` (live session)
— retry is pointless by design (the cache covers steady-state sessions). No failure class remains
that a same-scan retry would fix, and the 20-scan diagnostics delta shows nothing left to retry.
No RED vector exists → no code added. The plan's item (b) (metadata phase overlapping identity
leases within a scan) is structurally impossible — the phases are sequentially awaited in
`FindAllAsync` — and item (a)'s JOIN semantics already exist via ProtocolDeviceInfo single-flight.

### Phase-2 verification summary

| Gate | Result |
|---|---|
| Merger vectors | `total: 21, failed: 0` (all Phase-1 REDs → GREEN; all pins survive) |
| Fault-injection vectors | `total: 6, failed: 0` × 3 consecutive runs |
| Full Core unit suite | `total: 695, failed: 0, succeeded: 692, skipped: 3` (pre-existing platform skips; zero intended REDs remain) |
| `resilience --fast` | Succeeded (RuntimeResilience category green) |
| Rig integration (2-key, fresh manager per test) | `Total tests: 5, Passed: 5` × 2 runs — zero-orphans, completeness, and stability flipped RED→GREEN on hardware |
| `dotnet format --verify-no-changes` | exit 0 (only pre-existing IL2026/IL3050 in Tests.TestProject) |

Phase-1 hooks: (a) admission wait — RESOLVED (change 3); (b) skip-cause discriminator — RESOLVED
(change 3); shape-A disposition — closed in Phase 1 (pin, survives change 1 by construction).