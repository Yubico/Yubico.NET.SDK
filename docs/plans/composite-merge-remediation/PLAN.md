# Composite-Merge Remediation Plan — Deterministic grouping + discovery guarantees

Author: Anthropic (Opus 4.8), synthesizing owner decisions, platform research, and v1-SDK archaeology.
Status: APPROVED by owner (2026-07-28). Cross-vendor audit: CONVERGED (2026-07-28, auditor
github-copilot/gpt-5.5 via opencode; Cato premium backend and codex CLI both unavailable).
Audit trail: cycle 1 round 1 found the complementary-partial epistemic bound and unsafe deduction
(both accepted - claims scoped, deduction gained type-count-closure); round 2 corrected "<=1 scan"
healing to conditional healing. Cycle 2 round 1 qualified the Windows topology-dependence and
convergence claims; round 2's single residual cell fix was applied verbatim as prescribed. All five
substantive findings were accepted rather than argued - the guarantee matrix is strictly weaker and
strictly more honest than the first draft.
Execution branch: new branch off `yubikit` after PR #528 (`yubikit-concurrency-fixes`) merges.
Repo: /Users/Dennis.Dyall/Code/y/Yubico.NET.SDK

## Problem

With two or more same-PID YubiKeys connected, discovery cannot tell them apart at the USB layer.
"Same-PID" is the precise term: the keys may differ in serial number and firmware version, but
those live inside the key and are readable only via a Management-application connection. At the
USB descriptor level - VID, PID, product string - same-model keys are byte-identical, and YubiKeys
deliberately expose no iSerialNumber descriptor. E.g. with two `0x0407` OTP+FIDO+CCID keys,
`FindYubiKeys.FindAllAsync` produces nondeterministic, incomplete composite grouping: interface
*count* is always right, but interfaces whose identity read failed that scan become orphaned
standalone devices instead of members of their physical key's composite. Observed on the two-key
rig (serials 103/125, fw 5.8.0): run-to-run groupings like `103={HidFido|HidOtp}`,
`125={HidOtp|SmartCard}` plus orphans. Reproduced identically at `e0516ba6` (pre-concurrency-work);
NOT a regression. Consequences: `SupportsConnection(SmartCard)` filters silently miss physically
present idle keys; `PivMultiKeyContentionTests` cannot find two SmartCard-capable keys; four
`CompositeDiscoveryIntegrationTests` fail (they also hard-assert `Assert.Single`, valid only on
single-key rigs).

**Root cause (verified against source): incompleteness, not cross-wiring.** A serial read cannot
return the wrong key's serial (PC/SC connects by reader name, HID by device path). With same-PID
keys, every USB interface takes the serial path (`FindYubiKeys.cs:91`); 6 concurrent reads race a
2s/attempt budget (`DiscoveryIdentityReader.cs:41`) under four-worker admission; failures are
nondeterministic; and the merger's deliberate conservatism — "null serial does not collapse"
(`CompositeDeviceMerger.cs:147-148`) — orphans every failure.

## Verified premises (source-checked)

1. **USB PID encodes the enabled interface set** (`ReaderNamePidParser.cs:56-64,98-105`).
   Reconfiguring transports via `ManagementSession.SetDeviceConfigAsync(reboot: true)` re-enumerates
   the key under a different PID and product string (0x0407→0x0403 when CCID disabled, →0x0406 when
   OTP disabled). PID and interface set can never disagree. Firmware forbids disabling all USB
   capabilities.
2. **Identity cache converges**: successful reads are sticky per interface DeviceId
   (`FindYubiKeys.cs:46`), evicted only when the interface disappears (`EvictAbsentIdentities`).
   Reconfiguration changes reader names and HID paths, so stale entries self-evict.
3. **Merger is pure**: `CompositeDeviceMerger.Merge` is static and side-effect-free — fully
   unit-vectorable. `FindYubiKeys` takes `IFindPcscDevices`/`IFindHidDevices`/`IYubiKeyFactory` via
   constructor — fully fake-able (precedent: `FindYubiKeysPidMergeTests.cs`).
4. **Existing guard gap (pre-existing defect, found during this design)**:
   `CanMergeByPidWithoutSerial` (`CompositeDeviceMerger.cs:167`) protects only the full-triple
   PID and only the CCID+1-HID shape. Unguarded transient cross-key merges are representable today:
   (a) two 2-interface-PID keys (e.g. both 0x0403) each with one interface enumerated → merged
   cross-key; (b) two 0x0407 keys with one's FIDO + other's OTP enumerated, no CCID →
   `hasSmartCard==false` bypasses the guard → merged cross-key. Transient and self-healing, but a
   composite spanning two physical keys is representable.
5. **In-use interfaces are skipped by design** (`DiscoveryIdentityReader.cs:49-56`) — correct
   (discovery must not clobber sessions); the cache covers steady-state held sessions.

## Research record (2026-07-28; Perplexity sonar-pro + v1 archaeology)

**Windows — reader→USB topology: FULLY POSSIBLE, documented.**
`SCardGetReaderDeviceInstanceId` (WinSCard, Windows 8+) maps reader name → device instance ID →
`CM_Locate_DevNode` → `DEVPKEY_Device_ContainerId` via `CM_Get_DevNode_Property`. The Container ID
GUID matches across all interfaces of one composite USB device, including its HID interfaces.
Failure modes: API absent <Win8; `CR_NO_SUCH_DEVNODE` on stale IDs; treat any failure as
unknown-topology, never infer. (learn.microsoft.com winscard/nf-winscard-scardgetreaderdeviceinstanceida)

**macOS — reader→USB topology: NOT POSSIBLE (supported).** No stable way to map a PC/SC reader
name or TKSmartCardSlot to an IOUSBDevice/locationID. The " 02" suffix is a driver-local
enumeration artifact. `SCARD_ATTR_CHANNEL_ID`/`SCARD_ATTR_VENDOR_IFD_SERIAL_NO` unsupported or
meaningless there. YubiKeys expose no USB iSerialNumber descriptor.

**Linux — reader→USB topology: NO RELIABLE MAPPING.** pcsc-lite reader name format is
`name [interface] (serial) index slot` — serial comes from iSerialNumber, which YubiKeys lack.
`SCARD_ATTR_CHANNEL_ID` is not a documented bus/address contract. pcsc-lite provides no
reader→USB mapping service; only ordering heuristics exist (rejected as fragile).

**v1 SDK archaeology (develop branch, this repo).** v1's `YubiKeyDeviceListener.Update()` used a
three-tier cascade: path identity → `HasSameParentDevice` (`IDevice.ParentDeviceId`) → serial read
over the interface. `ParentDeviceId` was populated ONLY for HID: Windows `CmDevice.ContainerId`
(`PlatformInterop/Windows/Cfgmgr32/CmDevice.cs`), macOS IOKit LocationID (`MacOSHidDevice.cs:79`),
Linux udev 3-hop parent syspath (`LinuxHidDevice.cs:71,108`). `SmartCardDevice.ParentDeviceId` was
declared and NEVER assigned on any platform; `SCardGetReaderDeviceInstanceId` appears nowhere in
v1 history. v1's own doc comment: "macOS and Windows 7, for example, cannot provide parent
information for smart card devices." v1 therefore always attached CCID by serial read; serial-less
keys' CCID stayed a permanently separate device. Windows CCID topology in this plan is strictly
better than v1 ever shipped.

## Owner decisions (recorded)

1. End-of-effort deliverable: a clear **guarantees document** — what discovery can and cannot
   guarantee, per platform, every claim pinned by a test.
2. Assume Security Key series keys CAN be multi-interface (e.g. HID FIDO + CCID) and serial-less —
   pending confirmation from owner's management. Consequence: the serial-less multi-interface pair
   is in-scope hardware, and **A′-Windows topology is the only complete answer for it** — it is
   in-plan now, not a follow-up.
3. **Evidence rule (binding, all phases), three proof forms with distinct roles:**
   - **Defect-fix vectors: RED→GREEN.** RED must fail against pre-change code FOR THE PREDICTED
     REASON (asserted failure mode, not mere failure). A test that passes on first try against
     unfixed code is rejected as evidence of a fix.
   - **Invariant/pinning tests** (conservation, epistemic-bound windows, expected-conservative
     behavior): MAY pass against current code — they pin contracts, they are not evidence of a fix
     and are never counted as such in the ledger.
   - **Diagnostics delta** for probabilistic effects (read-scheduling tune): before/after failure
     tables, same rig, same scan count.
   The ISA carries an evidence ledger: change → RED failure text, pin declaration, or diagnostic
   delta.
4. macOS/Linux HID↔HID topology (LocationID / udev parent): DEFERRED follow-up; only valuable for
   serial-less multi-HID hardware (moot for FIDO+CCID-shaped keys, which have one HID interface).

## Scenario analyses (design must hold under all)

**Transport reconfiguration (OTP or CCID disabled via Management):** PID re-enumerates with the new
interface set, so one-key-reconfigured rigs become trivially distinguishable (PID counts drop to 1;
no serial reads needed). Both-keys-reconfigured-identically reproduces the same-PID problem in a
different PID class — all logic must be PID-generic. During the ~3s reboot window enumeration is
partial: the generalized guard (below) keeps partial observations conservative; deduction only
attributes enumerated orphans, never synthesizes from expectation. Capability changes *within* a
transport (PIV off, CCID kept) don't change the PID or interface set — no effect. NFC-only changes
don't affect USB.

**Two serial-less multi-interface keys:** Windows — grouped 100% by Container ID. macOS/Linux —
CCID unattributable (no serial, no topology): permanent conservative split, two devices per key.
This is a platform bound, documented in the guarantees doc, pinned by an expected-conservative
unit vector.

**Both keys' interfaces in use from plug-in:** reads skipped by design; heals on first idle scan
via cache. Documented as eventual.

## Solution design

**Evidence hierarchy in the merger (deterministic order):**
1. **Topology key** (Windows Container ID; strongest, when available)
2. **Serial evidence** (existing path)
3. **PID-unique AND observed == ExpectedConnectionsForPid** (generalized guard — replaces the
   bespoke triple-shape check). This FIXES the previously unguarded 0x0407 two-HID-no-CCID shape
   (observed != expected → serial path). It does NOT — and cannot — fix the complementary-partial
   shape (see Epistemic bound below).
4. **Pigeonhole deduction** (new, PID-generic, ~30 lines): orphans whose connection types exactly
   fill the missing slots of exactly ONE incomplete same-PID composite are attributed there, with a
   type-count-closure precondition: for every interface type in the PID's expected set, the count
   of visible same-PID interfaces of that type must not exceed the number of candidate keys. Any
   ambiguity (two candidates, or counts exceeding candidates) stays split.
5. **Conservative standalone** (unchanged last resort)

**Epistemic bound (applies to tiers 3 and 4, on macOS/Linux without topology evidence):** when two
same-PID keys are only PARTIALLY visible with complementary interface sets — e.g. two 0x0403 keys
where only key A's OTP and key B's FIDO enumerate, or key A anchored as {OTP,FIDO} while key B is
visible only as an unread CCID — the descriptors are indistinguishable from one fully-visible key.
No merge logic can resolve this; only serial or topology evidence can, and in these windows neither
exists for the invisible/unread interfaces. The canonical Rust reference model shares this bound
(it merges by PID). Rejected alternatives, with proportionality rationale: (a) always requiring
serial evidence for every multi-interface merge — imposes 1-2s first-scan latency on every
single-key user (the overwhelmingly common case) to defend a transient window on multi-identical-
key rigs; (b) post-merge dual-interface serial verification — still best-effort, converts but does
not close the window, doubles metadata reads. Instead the bound is DOCUMENTED, bounded, and pinned:
a cross-key attribution can exist only while visibility is partial, and persists until the first
subsequent scan with complete same-PID visibility, serial evidence, or topology evidence — which
may be more than one scan if an interface remains absent, busy, or identity-unreadable. Healing is
conditional on evidence, not on time; the interval fallback guarantees scans keep happening, not
that any given scan heals. The pinning vectors assert exactly this conditional healing. Per-interface connections are path-bound, so the harm is composite-level metadata/
filter truth, never a connection silently landing on a different physical interface than the one
it names.

**Correctness invariant (all platforms): no cross-key composite under complete same-PID interface
visibility — 100%, pinned by vectors; on Windows this holds under partial visibility too whenever
topology evidence is readable, degrading to the macOS bound when it is not. Under partial visibility (hotplug staggering, mid-reboot
enumeration, an interface failing to enumerate), a cross-key composite may exist and persists
until the first scan with complete visibility, serial evidence, or topology evidence; this
conditional-healing window is itself pinned by expected-behavior vectors, and on Windows the
topology tier closes it entirely.** Completeness is eventual: ~99% scan-1 on macOS/Linux,
converging to 100% via the identity cache PROVIDED each interface is eventually idle and
identity-readable (an interface held in-use forever, or persistently unreadable, stays
conservatively split — consistent with the in-use guarantee row); 100% scan-1 on Windows with
topology evidence, including serial-less hardware.

**Supporting changes:** read-scheduling tune per Phase-0 findings (serialize identity reads
per candidate key or exempt them from the four-worker admission — diagnostics-proved); one in-scan
retry for transiently-failed reads (RED-proved via fault injection).

**A′-Windows interop:** `SCardGetReaderDeviceInstanceIdW` P/Invoke in
`Native/Desktop/SCard`; Container-ID read for both the reader's devnode and HID device instances
via existing Cfgmgr32 interop (v1 `CmDevice` as blueprint). All behavior behind a scripted native
seam (the `LinuxUdevHidEventSourceTests` pattern) so the logic is fully unit-tested keyless on any
OS. Topology evidence is optional input to the merger: absent on mac/Linux, and absent on Windows
when the topology read fails (stale devnode mid-hotplug, `CR_NO_SUCH_DEVNODE`, API unavailable).
**Topology failure degrades Windows to exactly the macOS/Linux semantics** — the same evidence
hierarchy, the same conservative fallbacks, the same epistemic bound — and never guesses. Windows
guarantee rows therefore read "with topology evidence (the normal case)"; the degraded path is
pinned by seam vectors scripting each failure mode.

## Phases

| Phase | Content | Evidence | Hardware |
|---|---|---|---|
| 0 | Diagnostics: 20 scans, debug logs, tabulate identity-read failure reasons | The diagnostic baseline itself | current rig |
| 1 | Keyless RED harness: generalize the 4 CompositeDiscovery tests to invariants (conservation, zero-orphans-idle, completeness-per-PID, stability, key-count-agnostic); merger unit vectors for every PID class, null serials, partial enumerations, both cross-key transient shapes, the complementary-partial epistemic-bound scenarios (pinned as expected bounded behavior), reconfig transitions, serial-less pair; FindYubiKeys fault-injection via constructor fakes | Defect vectors RED for predicted reasons; invariant/bound pins declared as pins | none (vectors), rig (integration RED) |
| 2 | B core: generalized guard, pigeonhole deduction, scheduling tune, in-scan retry | RED→GREEN per vector; tune by diagnostics delta | none + rig |
| 3 | A′-Windows: WinSCard+Cfgmgr32 interop behind seams; topology tier in merger | Seam tests first; serial-less vector RED against B-only merger → GREEN with topology | none (seams) |
| 4 | Hardware verification Tier 1 (current rig): suite RED→GREEN, PivMultiKeyContentionTests un-skips, 20-scan stability, busy-key, in-use, full reconfig matrix (authorized on dedicated test keys). Tier 2 (lab, gates release not development): Windows+Linux single-key smoke, serial-less pair if exists, 0x011x key, NFC reader | recorded runs | tiered |
| 5 | Guarantees doc `docs/architecture/device-discovery-guarantees.md` + links from physical-device-model.md, Core README/CLAUDE.md, Tests.Shared/README.md; ISA with evidence ledger | every guarantee row cites its pinning test; unpinned claims reworded weaker | none |

Final gates: full build; full unit suite; `resilience --fast` (discovery loops touched);
`dotnet format`; `git diff --check`; cross-vendor DevTeam review (opposite-vendor reviewer);
Cato convergence rules apply (max 2 rounds/artifact, disposition-scoped round 2).

## Guarantee matrix (target state; each cell pinned by a named test in Phase 5)

| Guarantee | Windows | macOS | Linux |
|---|---|---|---|
| No cross-key composite under complete same-PID visibility | 100% | 100% | 100% |
| No cross-key composite during partial-visibility transients | 100% with topology evidence (normal case); degrades to macOS semantics on topology-read failure, pinned | bounded: possible while partial visibility persists; heals on first scan with complete visibility or evidence; pinned (epistemic bound) | same as macOS |
| Identical serial-bearing keys: complete grouping | 100% scan-1 with topology evidence; on topology-read failure, same as macOS/Linux | ~99% scan-1; converges to 100% provided each interface is eventually idle and identity-readable, otherwise conservative split | same as macOS |
| Serial-less multi-interface pair | 100% with topology evidence; conservative split on topology-read failure | not groupable — permanent split (platform bound) | same as macOS |
| Reconfiguration (one key) | 100% (PID split) | 100% | 100% |
| Transients (reboot/hotplug) | correct, possibly partial, self-healing | correct-or-bounded per epistemic bound, self-healing | same as macOS |
| In-use-since-plug-in interfaces | attributed once the interface first becomes idle AND a subsequent discovery scan successfully reads its identity (cache retains it thereafter) | same | same |

## Phase 0 RESULTS (2026-07-28, executed; harness at merge-diag/ alongside this plan)

Rig: three same-PID 0x0407 keys at start - different serials (103, 125, 25555459) and firmware
(5.8.0.beta.0, 5.8.0.alpha.2, production), but byte-identical USB descriptors, which is all the
merger can see pre-read. The third (owner's production key, deliberately unplugged mid-run, will
not recur; only read-only discovery DeviceInfo reads ever touched it) left after scan 11, giving
both 3-key and 2-key data. 20 scans x 2 modes, debug logs captured
(diag-fresh.log, diag-shared.log).

1. **Failure reasons (fresh instance per scan, no caches): 68/71 = "aborted: interface gained a
   live connection" (`DiscoveryReadSkippedException`), 1/71 budget timeout, 2/71 transient retry
   failures.** Discovery contends with ITSELF: abandoned background reads and metadata reads still
   hold exclusive per-interface leases when the next scan's identity read arrives. NOT PC/SC
   sharing violations, NOT budget pressure. HID interfaces bear nearly all failures; CCID
   attribution almost always succeeds on the 2-key rig — inverting the draft's emphasis.
2. **Convergence works: with one FindYubiKeys instance (production shape), scan 1 was partial
   (2 aborts) and scans 2-20 were 19/19 PERFECT complete groupings at 1-19ms.** The user-visible
   defect is therefore concentrated in scan-1 / fresh-process / one-shot FindAllAsync conditions -
   exactly what the failing integration tests exercise (one scan per fresh process).
3. Three identical keys (9 concurrent reads) assemble far worse than two: scans 1-11 produced 7-8
   devices with heavy orphaning; at most one composite assembled per scan.
4. pcsc reader-name suffixes are unstable as the reader set changes ("", " 01", " 02" all observed
   for the same physical keys across scans) - pcsc DeviceIds are not stable identifiers when keys
   come and go; identity-cache eviction behavior under renames needs a pinning test.
5. Scan-1 self-contention interleaving (2 aborts even in shared mode, fresh process) is NOT yet
   pinned; hypothesis: identity-read tail overlapping the same scan's metadata phase on the same
   interface. To be pinned deterministically by Phase 1 fault injection before Phase 2 touches it.

**Phase 2 refinement (diagnostics-driven):** the "read scheduling tune" is now precise - eliminate
discovery SELF-contention: (a) when an identity read finds the interface leased by discovery's own
prior abandoned read, JOIN/await it (single-flight semantics) instead of aborting to null;
(b) ensure the metadata phase cannot overlap identity leases on the same interface within a scan;
(c) one targeted same-scan retry for self-contention aborts (the colliding lease usually releases
within milliseconds). Proof: diagnostics delta (fresh-mode failure table before/after) plus RED
fault-injection vectors for (a) and (b).

## Out of scope

Per-transport scan isolation when PC/SC enumeration throws (tracked in polling-migration ISA);
macOS/Linux HID↔HID topology (deferred, pending Security-Key hardware confirmation); NFC reader
grouping (stands alone by design); cross-process contention.

## Open items

1. Owner's management: do multi-interface serial-less Security Key configurations exist in the
   field? (Affects deferred follow-up priority and one guarantees-doc row's wording; plan holds
   either way.)
2. Lab availability for Tier 2 hardware.
