---
task: "Remediate PR 528 concurrency and native resiliency audit findings"
slug: 20260723-pr528-concurrency-resiliency-remediation
project: Yubico.NET.SDK
branch: yubikit-concurrency-fixes
pull_request: 528
effort: E4
effort_source: explicit
phase: execute
progress: 93/93
mode: interactive
started: 2026-07-23
updated: 2026-07-28
---

## Problem

PR #528's audit identified seven correctness and resiliency defects across device discovery, session coordination, SCP gate ownership, monitor signaling and startup, and Linux native shutdown handling. The current behaviors can accumulate abandoned device-info reads, permit discovery/session races, serialize SCP traffic with the wrong gate, allocate unbounded coalescing hints, leak partially started monitor resources, reject the valid file descriptor `0`, and mishandle interrupted `eventfd` shutdown writes. These are concurrency defects whose happy paths can appear correct while failure paths violate resource and ownership invariants.

The remediation needs more than seven local edits. Each defect requires reproducible RED evidence, a minimal GREEN correction, retained regression coverage, cross-vendor review in bounded batches, and verification on both unit/resilience suites and authorized SmartCard-capable hardware.

## Vision

Discovery and sessions coordinate through explicit ownership rather than timing luck; repeated callers remain responsive without multiplying underlying work; every SCP exchange shares the intended serialization gate; monitor signaling is bounded and listener startup degrades independently to interval-only monitoring when needed; a monitor generation may do anything except publish stale truth, so lifecycle races are removed by gating publication rather than coordinating transitions; connections are disposed exactly once and a returning disposal call means teardown finished; and Linux shutdown remains correct across valid descriptor values and transient native errors. The final diff is small enough to reason about, strong enough to survive persistent failures, and backed by tests that would fail if any of these bugs returned.

## Out of Scope

- Public API redesign unrelated to the seven PR #528 findings.
- General performance tuning beyond the single-flight, lease, channel, and retry invariants named here.
- Changes to unrelated application modules or integration-test behavior.
- New dependency-injection layers, generalized concurrency frameworks, or speculative transport abstractions.
- Hardware tests requiring insertion, removal, or touch beyond the explicitly authorized Core, Management, and PIV runs.
- Commits, pushes, branch rebases, or PR updates without a separate explicit request.
- Production-code or test edits during this baseline-only OBSERVE task.

## Principles

- **Bound waiting and bound work separately.** A caller timeout is not proof that the shared underlying operation stopped.
- **Ownership precedes effects.** A session owns its interface before opening the physical connection; discovery owns its lease before selecting Management.
- **Concurrency invariants must be structural.** Correctness cannot depend on a favorable count/check timing window.
- **Shared serialization is never silently substituted.** Unsupported gate ownership fails explicitly rather than creating an independent lock.
- **Signals carry occurrence, not backlog.** Coalescing hints use bounded wake-up semantics when payloads are intentionally discarded.
- **Startup is a transaction.** Partial acquisition either commits completely or rolls back completely.
- **Native contracts include boundary values and transient errors.** File descriptor zero is valid; `EINTR` and `EAGAIN` receive explicit deterministic treatment.
- **Tests preserve causal evidence.** Every fix begins with a failing behavioral test and retains meaningful prior regressions.

## Constraints

- Use strict RED/GREEN TDD for each of the seven correctness bugs; capture a failing result before production implementation.
- Preserve meaningful existing tests as regressions; do not replace strong tests with implementation-detail assertions.
- Use the repository toolchain commands exactly; never invoke raw `dotnet build` or raw `dotnet test`.
- Execute three DevTeam batches in order: discovery; SCP plus monitor; Linux native.
- Each DevTeam batch pairs an OpenAI Engineer with an Anthropic Opus cross-vendor Reviewer, permits at most three iterations, and closes with no unresolved HIGH findings.
- A final cross-vendor Reviewer evaluates the whole diff after the three batches.
- The authorized hardware gate includes full PIV integration testing even though it resets or mutates PIV state and generates keys.
- Two allow-listed SmartCard-capable YubiKeys must remain connected for `PivMultiKeyContentionTests`; an environment skip is a failure, not an acceptable result.
- No commit or push occurs without explicit user authorization.
- The current task writes only this ISA and runs `dotnet toolchain.cs test`; implementation and all other gates belong to later phases.

## Goal

Remediate all seven PR #528 audit findings with structural concurrency and native-error invariants, one retained RED-first regression per bug, three bounded cross-vendor DevTeam batches, and a clean final whole-diff review. Completion requires every command and hardware gate in this ISA to pass with recorded evidence and no unresolved HIGH review finding.

## Criteria

- [x] ISC-1: `git status --short --branch` names `yubikit-concurrency-fixes`.
- [x] ISC-2: The persisted ISA exists at the requested repository path.
- [x] ISC-3: The ISA contains all twelve non-empty E4 sections.
- [x] ISC-4: Anti: Production or test files change before baseline completion.
- [x] ISC-5: Baseline `dotnet toolchain.cs test` exits zero before implementation edits.

- [x] ISC-6: Discovery single-flight regression demonstrates RED against pre-fix behavior.
- [x] ISC-7: One device-info read is in flight per stable interface/transport key.
- [x] ISC-8: A timed caller returns within its configured wait bound.
- [x] ISC-9: Anti: Caller cancellation cancels shared work needed by other callers.
- [x] ISC-10: Repeated timed callers do not increase underlying read concurrency.
- [x] ISC-11: Successful shared reads remain eligible for existing success caching.
- [x] ISC-12: Faulted shared reads permit a later independent retry.
- [x] ISC-13: Cancelled shared reads permit a later independent retry.
- [x] ISC-14: Completed shared operations release single-flight bookkeeping.

- [x] ISC-15: Registry lease regression demonstrates RED against pre-fix behavior.
- [x] ISC-16: Discovery leases are acquired atomically per stable interface.
- [x] ISC-17: Session leases are acquired atomically per stable interface.
- [x] ISC-18: Session ownership is established before physical connection creation.
- [x] ISC-19: Discovery skips nonblockingly while a session lease is active.
- [x] ISC-20: Anti: Sessions pass an active discovery lease on one interface.
- [x] ISC-21: Discovery and session ownership cannot overlap per interface.
- [x] ISC-22: Lease release occurs when physical connection creation fails.
- [x] ISC-23: Lease release occurs when Management selection fails.
- [x] ISC-24: Independent interfaces can make progress concurrently.

- [x] ISC-25: SCP unsupported-base regression demonstrates RED against pre-fix behavior.
- [x] ISC-26: Unsupported `ISmartCardProtocol` bases fail explicitly during SCP wrapping.
- [x] ISC-27: Anti: SCP wrapping silently allocates an independent exchange gate.
- [x] ISC-28: Supported SCP wrappers retain the base protocol's shared exchange gate.

- [x] ISC-29: Monitor signaling regression demonstrates RED against pre-fix behavior.
- [x] ISC-30: Monitor coalescing uses a bounded channel of capacity one.
- [x] ISC-31: Additional hints cannot grow queued signal count above one.
- [x] ISC-32: Anti: Monitor correctness depends on discarded hint payload values.
- [x] ISC-33: Existing maximum-coalesce timing behavior remains covered and passing.

- [x] ISC-34: Partial monitor startup regression demonstrates RED against pre-fix behavior. (Superseded by ISC-82..85 — see "Canonical alignment: graceful listener degradation"; no owning feature by design.)
- [x] ISC-35: Factory failure disposes every listener created earlier in that attempt.
- [x] ISC-36: Listener `Start` failure stops every listener started earlier.
- [x] ISC-37: Listener `Start` failure disposes every listener acquired in that attempt.
- [x] ISC-38: Failed startup disposes the attempt's channel resources. (Superseded — partial failure is no longer a failed attempt; no owning feature by design.)
- [x] ISC-39: Anti: Failed startup leaves monitor state marked as running. (Superseded — a partially-failed startup now intentionally keeps monitoring running; no owning feature by design.)
- [x] ISC-40: A clean startup retry succeeds after transactional rollback. (Superseded by ISC-82..85 and ISC-89; no owning feature by design.)

- [x] ISC-41: Linux file-descriptor regression demonstrates RED against pre-fix behavior.
- [x] ISC-42: Linux udev HID event source accepts file descriptor zero.
- [x] ISC-43: Anti: Linux udev HID event source accepts negative descriptors.
- [x] ISC-44: File-descriptor boundary behavior is verified through a native seam.

- [x] ISC-45: Linux eventfd regression demonstrates RED against pre-fix behavior.
- [x] ISC-46: Shutdown write retries after each `EINTR` result.
- [x] ISC-47: Shutdown write succeeds after `EINTR` followed by a full write.
- [x] ISC-48: Shutdown write treats `EAGAIN` as an already-signaled success.
- [x] ISC-49: Anti: Shutdown write suppresses non-`EINTR`/`EAGAIN` errors.
- [x] ISC-50: Native retry tests execute deterministically without Linux hardware.

- [x] ISC-51: Each production correction follows its captured RED test.
- [x] ISC-52: Each new regression test fails for the intended causal reason.
- [x] ISC-53: All seven RED regressions pass after their GREEN corrections.
- [x] ISC-54: Meaningful pre-existing regression tests remain enabled.
- [x] ISC-55: Anti: New tests self-skip because native services are unavailable.
- [x] ISC-56: Implementation notes map each audit finding to its regression test.
- [x] ISC-57: Relevant concurrency and native invariants are documented near tests.

- [x] ISC-58: Discovery DevTeam batch uses an OpenAI Engineer.
- [x] ISC-59: Discovery batch closes within three reviewer iterations.
- [x] ISC-60: Discovery batch has no unresolved HIGH review finding.
- [x] ISC-61: SCP-plus-monitor DevTeam batch uses an OpenAI Engineer.
- [x] ISC-62: SCP-plus-monitor batch closes within three reviewer iterations.
- [x] ISC-63: SCP-plus-monitor batch has no unresolved HIGH review finding.
- [x] ISC-64: Linux-native DevTeam batch uses an OpenAI Engineer.
- [x] ISC-65: Linux-native batch closes within three reviewer iterations.
- [x] ISC-66: Linux-native batch has no unresolved HIGH review finding.
- [x] ISC-67: Final Anthropic Opus review covers the complete remediation diff.
- [x] ISC-68: Final whole-diff review has no unresolved HIGH finding.

- [x] ISC-69: `dotnet toolchain.cs build` exits zero.
- [x] ISC-70: Final `dotnet toolchain.cs test` exits zero.
- [x] ISC-71: `dotnet toolchain.cs -- resilience --fast` exits zero.
- [x] ISC-72: Core integration smoke command exits zero.
- [x] ISC-73: Management integration smoke command exits zero.
- [x] ISC-74: Full PIV integration command exits zero.
- [x] ISC-75: Focused `PivMultiKeyContentionTests` execute with zero environment skips.
- [x] ISC-76: Formatting verification reports no whitespace or style changes in the branch; any aggregate analyzer failure is limited to the documented pre-existing baseline in untouched files.
- [x] ISC-77: `git diff --check` exits zero.
- [x] ISC-78: Final `git status --short --branch` shows only intended changes.
- [x] ISC-79: Discovery DevTeam Reviewer uses Anthropic Opus.
- [x] ISC-80: SCP-plus-monitor DevTeam Reviewer uses Anthropic Opus.
- [x] ISC-81: Linux-native DevTeam Reviewer uses Anthropic Opus.

- [x] ISC-82: SmartCard listener failure does not abort HID-backed monitoring.
- [x] ISC-83: HID listener failure does not abort SmartCard-backed monitoring.
- [x] ISC-84: Failure of both listeners falls back to interval-only monitoring.
- [x] ISC-85: Failed listeners are detached, stopped, and disposed without leaking callbacks or resources.

- [x] ISC-86: A superseded monitor generation cannot publish a device snapshot.
- [x] ISC-87: Publications never interleave, and a successor's snapshot lands after any in-flight predecessor's.
- [x] ISC-88: A blocking `DeviceChanges` subscriber cannot delay start, stop, or dispose.
- [x] ISC-89: Restart succeeds immediately after a stop that timed out on a hung scan.
- [x] ISC-90: Anti: any semaphore is disposed while a caller can still acquire it.
- [x] ISC-91: Inner connections are disposed exactly once and the ownership lease is released only after inner teardown completes.
- [x] ISC-92: Any registered-connection disposal call returning implies teardown finished, with the same outcome for every caller.
- [x] ISC-93: SCP wrapper construction is reachable only through `WithScpAsync`.

## Test Strategy

| isc | type | check | threshold | tool |
|---|---|---|---|---|
| ISC-1 | repository | active branch | exact branch name | `git status --short --branch` |
| ISC-2 | filesystem | ISA path | file exists | file read |
| ISC-3 | structure | E4 section headings and content | 12/12 populated | Markdown inspection |
| ISC-4 | anti-diff | pre-baseline changed paths | zero production/test paths | `git status --short` |
| ISC-5 | baseline | unit suite before edits | exit 0 | `dotnet toolchain.cs test` |
| ISC-6 | RED | single-flight regression | intended pre-fix failure | focused unit test log |
| ISC-7 | concurrency | active reads per key | maximum 1 | deterministic fake counter |
| ISC-8 | timing | caller wait | within configured bound plus test tolerance | focused unit test |
| ISC-9 | anti-concurrency | shared operation survives one waiter cancellation | other waiter completes | focused unit test |
| ISC-10 | stress | repeated timeout concurrency | maximum 1 underlying read | focused unit test |
| ISC-11 | cache | successful read reused as designed | existing cache assertion passes | unit suite |
| ISC-12 | recovery | retry after fault | next read starts and completes | focused unit test |
| ISC-13 | recovery | retry after cancellation | next read starts and completes | focused unit test |
| ISC-14 | lifecycle | in-flight entry after completion | entry absent | focused unit test |
| ISC-15 | RED | lease TOCTOU regression | intended pre-fix failure | deterministic interleaving test |
| ISC-16 | ownership | discovery lease acquisition | one owner per interface | focused unit test |
| ISC-17 | ownership | session lease acquisition | one owner per interface | focused unit test |
| ISC-18 | ordering | lease versus physical connect | lease observed first | fake factory trace |
| ISC-19 | nonblocking | discovery under session | immediate skip | focused unit test |
| ISC-20 | anti-race | session under discovery | cannot cross lease | controlled barrier test |
| ISC-21 | invariant | simultaneous ownership | zero overlap | stress/interleaving test |
| ISC-22 | cleanup | failed physical connect | lease reacquirable | focused unit test |
| ISC-23 | cleanup | failed Management SELECT | lease reacquirable | focused unit test |
| ISC-24 | concurrency | separate interfaces | both progress | controlled parallel test |
| ISC-25 | RED | unsupported SCP base regression | intended pre-fix failure | focused unit test |
| ISC-26 | contract | unsupported base | explicit exception | focused unit test |
| ISC-27 | anti-fallback | gate allocation | no independent fallback | constructor-path assertion |
| ISC-28 | serialization | supported wrapper gate identity | same gate instance | focused unit test |
| ISC-29 | RED | unbounded signaling regression | intended pre-fix failure | focused unit test |
| ISC-30 | capacity | signal channel | capacity 1 | focused unit test |
| ISC-31 | pressure | queued hints | maximum 1 | burst test |
| ISC-32 | anti-payload | discarded values | behavior value-independent | parameterized unit test |
| ISC-33 | regression | max coalesce | existing expected timing passes | existing unit tests |
| ISC-34 | RED | partial startup regression | intended pre-fix failure | fault-injection unit test |
| ISC-35 | rollback | factory failure cleanup | all earlier listeners disposed once | fake listener assertions |
| ISC-36 | rollback | start failure stop | all started listeners stopped once | fake listener assertions |
| ISC-37 | rollback | start failure disposal | all acquired listeners disposed once | fake listener assertions |
| ISC-38 | rollback | channel lifecycle | attempt resources disposed | lifecycle assertion |
| ISC-39 | anti-state | failed startup state | not running | service state assertion |
| ISC-40 | recovery | retry after rollback | subsequent start succeeds | two-attempt unit test |
| ISC-41 | RED | descriptor-zero regression | intended pre-fix failure | native-seam unit test |
| ISC-42 | boundary | descriptor zero | accepted | native-seam unit test |
| ISC-43 | anti-boundary | negative descriptor | rejected | parameterized unit test |
| ISC-44 | determinism | descriptor source | fake native seam used | test inspection |
| ISC-45 | RED | eventfd interruption regression | intended pre-fix failure | native-seam unit test |
| ISC-46 | retry | repeated `EINTR` | one retry per interruption | scripted native seam |
| ISC-47 | recovery | `EINTR`, then full write | success | scripted native seam |
| ISC-48 | transient | `EAGAIN` | accepted success | scripted native seam |
| ISC-49 | anti-suppression | other errno | surfaced failure | parameterized native-seam test |
| ISC-50 | determinism | environment dependency | no hardware/service requirement | unit test traits and execution |
| ISC-51 | TDD audit | edit ordering | RED evidence predates production change | logs plus diff timeline |
| ISC-52 | causality | RED failure messages | each names intended behavior | RED logs |
| ISC-53 | GREEN | seven regressions | 7/7 pass | focused tests |
| ISC-54 | regression | existing tests | no meaningful deletion/disablement | diff review |
| ISC-55 | anti-skip | native unit tests | zero environment skips | test output |
| ISC-56 | traceability | findings to tests | 7/7 mapped | implementation notes inspection |
| ISC-57 | documentation | invariant comments/docs | all non-obvious invariants covered | diff review |
| ISC-58 | DevTeam | discovery engineer vendor | OpenAI | batch transcript |
| ISC-59 | DevTeam | discovery iterations | at most 3 | batch transcript |
| ISC-60 | review | discovery HIGH findings | zero unresolved | reviewer verdict |
| ISC-61 | DevTeam | SCP/monitor engineer vendor | OpenAI | batch transcript |
| ISC-62 | DevTeam | SCP/monitor iterations | at most 3 | batch transcript |
| ISC-63 | review | SCP/monitor HIGH findings | zero unresolved | reviewer verdict |
| ISC-64 | DevTeam | Linux engineer vendor | OpenAI | batch transcript |
| ISC-65 | DevTeam | Linux iterations | at most 3 | batch transcript |
| ISC-66 | review | Linux HIGH findings | zero unresolved | reviewer verdict |
| ISC-67 | review | complete diff reviewer | Anthropic Opus | final review transcript |
| ISC-68 | review | final HIGH findings | zero unresolved | final reviewer verdict |
| ISC-69 | build | repository build | exit 0 | `dotnet toolchain.cs build` |
| ISC-70 | unit | full unit suite | exit 0 | `dotnet toolchain.cs test` |
| ISC-71 | resilience | fast resilience suite | exit 0 | `dotnet toolchain.cs -- resilience --fast` |
| ISC-72 | integration | Core smoke | exit 0 | `dotnet toolchain.cs -- test --integration --project Core --smoke` |
| ISC-73 | integration | Management smoke | exit 0 | `dotnet toolchain.cs -- test --integration --project Management --smoke` |
| ISC-74 | integration | full PIV suite | exit 0 | `dotnet toolchain.cs -- test --integration --project Piv` |
| ISC-75 | integration | multi-key contention | executed; zero environment skips | `dotnet toolchain.cs -- test --integration --project Piv --filter "FullyQualifiedName~PivMultiKeyContentionTests"` |
| ISC-76 | formatting | branch whitespace and style | clean; aggregate analyzer output limited to documented untouched-file baseline | `dotnet format whitespace/style --verify-no-changes` plus aggregate diagnostic review |
| ISC-77 | whitespace | patch whitespace | exit 0 | `git diff --check` |
| ISC-78 | repository | final changed paths | intended files only | `git status --short --branch` |
| ISC-79 | DevTeam | discovery reviewer vendor | Anthropic Opus | batch transcript |
| ISC-80 | DevTeam | SCP/monitor reviewer vendor | Anthropic Opus | batch transcript |
| ISC-81 | DevTeam | Linux reviewer vendor | Anthropic Opus | batch transcript |
| ISC-82 | degradation | SmartCard listener failure | HID-backed monitoring remains active | fault-injection unit tests |
| ISC-83 | degradation | HID listener failure | SmartCard-backed monitoring remains active | fault-injection unit tests |
| ISC-84 | fallback | both listeners fail | interval-only monitoring remains active | timed rescan unit test |
| ISC-85 | cleanup | failed listener | detached, stopped, and disposed | fake listener assertions |
| ISC-86 | admission | superseded generation publish | snapshot discarded | generation-swap unit tests |
| ISC-87 | exclusion | concurrent cross-generation publication | no interleaving; successor lands last | publish-gate seam test |
| ISC-88 | isolation | blocking subscriber | lifecycle unaffected; dispose drain bounded | blocking-subscriber unit test |
| ISC-89 | recovery | restart after stop timeout | new generation publishes | hung-scan unit test |
| ISC-90 | anti-disposal | semaphore disposal | zero `SemaphoreSlim.Dispose` calls | source inspection |
| ISC-91 | disposal | sync/async disposal race | one inner disposal; lease after teardown | per-wrapper race tests |
| ISC-92 | completion | losing disposal caller | returns only after winner's teardown, same outcome | shared-completion tests |
| ISC-93 | visibility | SCP wrapper construction | constructor internal; `WithScpAsync` sole path | compile surface + docs |

## Features

```yaml
- name: BaselineAndEvidence
  description: Persist this E4 ISA, prove the target branch, and capture a clean unit baseline before implementation.
  satisfies: [ISC-1, ISC-2, ISC-3, ISC-4, ISC-5]
  depends_on: []
  parallelizable: false

- name: DiscoverySingleFlight
  description: Bound caller waits and underlying ProtocolDeviceInfo reads independently with one operation per stable interface/transport key.
  satisfies: [ISC-6, ISC-7, ISC-8, ISC-9, ISC-10, ISC-11, ISC-12, ISC-13, ISC-14]
  depends_on: [BaselineAndEvidence]
  parallelizable: false

- name: AtomicDiscoverySessionLeases
  description: Replace registry count/check/recheck timing with atomic per-interface discovery and session leases acquired before side effects.
  satisfies: [ISC-15, ISC-16, ISC-17, ISC-18, ISC-19, ISC-20, ISC-21, ISC-22, ISC-23, ISC-24]
  depends_on: [BaselineAndEvidence]
  parallelizable: false

- name: ScpSharedGateContract
  description: Reject unsupported smart-card protocol bases and preserve shared-gate identity for supported SCP wrappers.
  satisfies: [ISC-25, ISC-26, ISC-27, ISC-28]
  depends_on: [BaselineAndEvidence]
  parallelizable: true

- name: BoundedMonitorSignaling
  description: Replace discarded unbounded hints with capacity-one signaling while preserving maximum coalescing behavior.
  satisfies: [ISC-29, ISC-30, ISC-31, ISC-32, ISC-33]
  depends_on: [BaselineAndEvidence]
  parallelizable: true

- name: GracefulMonitorStartup
  description: Start listeners independently, clean up each failed listener, and preserve interval-only monitoring when no listener is available.
  satisfies: [ISC-35, ISC-36, ISC-37, ISC-82, ISC-83, ISC-84, ISC-85]
  depends_on: [BaselineAndEvidence]
  parallelizable: true

- name: EpochGatedPublication
  description: Replace the monitor lifecycle state machine with an immutable generation whose snapshots are admitted only while current, and stop disposing gates entirely.
  satisfies: [ISC-86, ISC-87, ISC-88, ISC-89, ISC-90]
  depends_on: [BoundedMonitorSignaling, GracefulMonitorStartup]
  parallelizable: false

- name: OneShotConnectionDisposal
  description: Give registered-connection wrappers a single-winner disposal gate whose completion every caller observes, so the lease is released only after inner teardown.
  satisfies: [ISC-91, ISC-92]
  depends_on: [AtomicDiscoverySessionLeases]
  parallelizable: true

- name: ScpConstructionClosure
  description: Narrow the SCP wrapper constructor to internal so shared-gate ownership is a compile-time entry-point property rather than a runtime check on a public path.
  satisfies: [ISC-93]
  depends_on: [ScpSharedGateContract]
  parallelizable: true

- name: LinuxNativeBoundaries
  description: Accept fd zero, reject negative descriptors, retry eventfd writes on EINTR, and accept EAGAIN as already signaled.
  satisfies: [ISC-41, ISC-42, ISC-43, ISC-44, ISC-45, ISC-46, ISC-47, ISC-48, ISC-49, ISC-50]
  depends_on: [BaselineAndEvidence]
  parallelizable: true

- name: RegressionAndTraceability
  description: Preserve causal RED/GREEN evidence, existing regressions, deterministic native tests, and finding-to-test documentation.
  satisfies: [ISC-51, ISC-52, ISC-53, ISC-54, ISC-55, ISC-56, ISC-57]
  depends_on: [DiscoverySingleFlight, AtomicDiscoverySessionLeases, ScpSharedGateContract, BoundedMonitorSignaling, GracefulMonitorStartup, LinuxNativeBoundaries]
  parallelizable: false

- name: CrossVendorReview
  description: Run three bounded DevTeam batches and a final whole-diff Anthropic Opus review with no unresolved HIGH findings.
  satisfies: [ISC-58, ISC-59, ISC-60, ISC-61, ISC-62, ISC-63, ISC-64, ISC-65, ISC-66, ISC-67, ISC-68, ISC-79, ISC-80, ISC-81]
  depends_on: [RegressionAndTraceability]
  parallelizable: false

- name: FinalVerification
  description: Execute every required build, unit, resilience, hardware integration, formatting, whitespace, and repository-state gate.
  satisfies: [ISC-69, ISC-70, ISC-71, ISC-72, ISC-73, ISC-74, ISC-75, ISC-76, ISC-77, ISC-78]
  depends_on: [CrossVendorReview]
  parallelizable: false
```

## Decisions

- 2026-07-23: Keep this effort at E4 because seven cross-cutting concurrency/native defects, hardware verification, and cross-vendor review require the full twelve-section artifact.
- 2026-07-23: Use 81 intentionally concrete ISCs, below the E4 soft floor of 128. The splitting test yields independently probeable invariants and gates at this count; adding rows to reach 128 would administratively inflate repeated mechanics rather than improve falsifiability.
- 2026-07-23: Treat caller wait bounds and underlying work bounds as separate design obligations. Cancelling or timing out a waiter must not automatically abandon or multiply shared device reads.
- 2026-07-23: Replace observational registry counts with ownership-bearing per-interface leases. Session ownership must precede physical connection creation because closing the final check/SELECT window requires ordering, not another recheck.
- 2026-07-23: Fail SCP wrapping for unsupported protocol bases rather than retaining compatibility through an independent gate; silent serialization divergence is more dangerous than explicit rejection.
- 2026-07-23: Use capacity-one monitor signaling because hint payloads are discarded and only the existence of pending work matters.
- 2026-07-23: Treat full PIV integration as authorized for this effort, including reset/mutation and generated keys; focused multi-key contention must execute on both connected allow-listed devices without environment skips.
- 2026-07-23: Run only `dotnet toolchain.cs test` in the current baseline task. Every production/test edit and remaining verification command is deferred to later phases.
- 2026-07-23: Batch 1 uses a process-wide single-flight table keyed by resolved interface `DeviceId` plus concrete `ConnectionType`. The shared operation runs with `CancellationToken.None`; each caller applies its own timeout/cancellation only to `WaitAsync`, and an exact key/value removal continuation retires completed work without removing a replacement operation.
- 2026-07-23: Batch 1 replaces connection counts with per-interface shared session ownership and nonblocking exclusive discovery ownership. Production devices expose an internal discovery-only connection path so discovery can hold its lease across physical connect, Management exchange, and disposal without reacquiring session ownership or expanding the public API.
- 2026-07-23: A permanently hung discovery operation deliberately retains both its single-flight entry and discovery lease. This prevents later sessions from crossing a native operation that could still issue Management SELECT; session callers can cancel their lease wait. The DevTeam Reviewer classified the resulting availability trade-off as MEDIUM, not a correctness failure.
- 2026-07-23: The initial DevTeam Batch 1 review returned `PASS WITH NOTES`, but the batch remained open after an independent cross-vendor iteration-2 review returned `NEEDS WORK`. The HIGH finding showed that public `IYubiKey.ConnectAsync` fallbacks under an already-held discovery lease could self-deadlock through transparent wrappers or composite members.
- 2026-07-23: Iteration 2 removes every public-connect fallback from discovery ownership. `ProtocolDeviceInfo` requires `IDiscoveryConnectionProvider` before taking a lease; `CompositeYubiKey` rejects members without that provider. Unsupported/custom devices now raise `DiscoveryReadSkippedException` without opening a connection or touching the wire.
- 2026-07-23: Idle ownership coordinators remain process-lifetime entries, bounded by unique interface IDs observed. Race-safe eviction would require retirement/version coordination around `GetOrAdd`; iteration 2 intentionally preserves the small space trade-off rather than adding a naïve remove/recreate race.
- 2026-07-23: Batch 2 rejects every `PcscProtocolScp` base that is not a concrete `PcscProtocol`. This preserves the public constructor while making shared-gate ownership a deterministic argument contract rather than silently substituting an independent gate.
- 2026-07-23: Monitor listener setup is an attempt-local transaction. Factories, callbacks, starts, cancellation source, and loop task are built in locals; service fields commit only after both listeners start. Rollback clears callbacks before best-effort stop/dispose, completes the attempt signal, preserves the initiating exception, and leaves no monitoring state to poison retry.
- 2026-07-23: `DeviceMonitorSignal` replaces the unbounded diagnostic request channel with a thin capacity-one bounded `bool` occurrence channel. HID diagnostics remain ingress logs; the consumer receives no payload or mirrored pending state. Each wake consumes at most one occurrence before debounce/deadline evaluation, preventing continuous producers from trapping the loop in a drain cycle.
- 2026-07-23: Batch 2 closed in two iterations after Anthropic `claude-opus-4-8` returned `PASS` with no HIGH or MEDIUM findings. ISC-62, ISC-63, and ISC-80 are satisfied.
- 2026-07-23: Batch 2 iteration 2 treats listener `Start()` return as provisional. Both HID and SmartCard status must equal `DeviceListenerStatus.Started`; `Error` or any other status raises a deterministic `InvalidOperationException` through the existing transactional rollback path.
- 2026-07-23: Batch 3 keeps Linux native testability local to `LinuxUdevHidEventSource`: one pure nonnegative-descriptor policy and one scripted eventfd-write helper. No broad libc abstraction or platform-dependent test path was introduced.
- 2026-07-23: Linux monitor descriptor `0` is valid; only negative descriptors fail validation. Shutdown eventfd writes succeed only after exactly eight bytes, retry every `EINTR`, accept `EAGAIN` as an already-pending wake, and report any other errno or incomplete nonnegative write exactly once before returning.
- 2026-07-23: Batch 3 closed in two iterations after the Anthropic Opus Reviewer returned `PASS` with no findings. Both iteration-1 defects are resolved: the udev descriptor ABI remains signed end to end, and persistent `EINTR` cannot make shutdown write retry indefinitely.
- 2026-07-23: Batch 3 iteration 1 Reviewer returned `NEEDS WORK`: HIGH because the C `int` return from `udev_monitor_get_fd` was incorrectly marshalled as `IntPtr`, allowing zero-extended native `-1` on 64-bit; MEDIUM because persistent `EINTR` could hot-loop synchronously forever during `Stop`.
- 2026-07-23: Batch 3 iteration 2 declares `udev_monitor_get_fd` as returning signed `int` and keeps descriptor validation int-only. No `IntPtr` compatibility helper remains.
- 2026-07-23: Shutdown eventfd writes have a private four-attempt bound. Because the fd is nonblocking, four total attempts tolerate three transient interruptions and a successful fourth write while bounding persistent-`EINTR` shutdown latency without sleeps or configurable policy.
- 2026-07-23: Final hardware verification exposed two upstream discovery boundaries outside the original seven local findings. Native provider work could begin synchronously before a caller installed `WaitAsync`, and macOS `SCardGetStatusChange` with timeout zero could still block behind a live PC/SC transaction. The remediation schedules admitted reads before provider invocation and bypasses status probing only for parseable integrated USB YubiKey reader names whose PID declares SmartCard support.
- 2026-07-23: Best-effort native discovery has four process-wide, nonqueueing worker admissions. Saturated protocol reads degrade to `DiscoveryReadSkippedException`; saturated PC/SC enumeration throws a transient `InvalidOperationException` rather than returning an authoritative empty snapshot. This bounds native workers while preserving repository state during saturation.
- 2026-07-23: Duplicate-PID identity reads run concurrently and retain `Task.WhenAll` input ordering, so independent two-second budgets consume one group wall-clock budget. The multi-key integration assertion identifies physical keys through SmartCard-capable rows because conservative macOS serial disambiguation may leave standalone OTP rows.
- 2026-07-23: Core plug/unplug integration tests were audited after operator feedback. All three human-intervention tests already carry both `RequiresUserPresence` and `Slow`; smoke excludes both traits. Composite discovery smoke separately documents and requires exactly one connected composite key, so the two-key contention gate ran first and Core smoke ran after one key was temporarily disconnected.
- 2026-07-23: The final complete-diff Anthropic Opus review returned `PASS WITH NOTES` and no HIGH findings. Its MEDIUM notes preserve two intentional availability choices: uncancelled sessions may wait behind a genuinely hung discovery lease, and direct PC/SC enumeration surfaces transient worker saturation instead of committing a false empty snapshot. LOW notes cover a rare synchronous scheduler-start failure and a self-correcting completed-entry teardown race.

## Changelog

- 2026-07-23 conjectured: bounding each ProtocolDeviceInfo caller by timeout or cancellation also bounded the underlying device read work. / refuted by: the audit traced abandoned waits into `FindYubiKeys` success-only caching, allowing later scans to start additional reads while prior operations remained active. / learned: responsiveness and work conservation are separate invariants; a bounded waiter can coexist with unbounded abandoned work. / criterion now: ISC-7 through ISC-14 require keyed single-flight work, bounded waits, non-destructive waiter cancellation, retry after non-success completion, and bookkeeping release.

- 2026-07-23 conjectured: registry count/check/recheck was sufficient to prevent discovery from selecting Management during an application session. / refuted by: the audit identified a TOCTOU interval after discovery's final check and before SELECT, while sessions register only after physical connection creation. / learned: observation cannot establish exclusion; ownership must be atomic and acquired before side effects. / criterion now: ISC-16 through ISC-24 require atomic per-interface leases, pre-connect session ownership, nonblocking discovery skip, session exclusion behind discovery, cleanup, and cross-interface progress.

- 2026-07-23 conjectured: creating a new `AsyncExchangeGate` was a safe fallback when `PcscProtocolScp` received an unsupported `ISmartCardProtocol` base. / refuted by: the audit showed the fallback serialized only the wrapper and not the underlying protocol's other exchanges. / learned: a lock with the right type but the wrong ownership domain breaks the serialization contract silently. / criterion now: ISC-26 through ISC-28 require explicit unsupported-base failure and shared-gate identity for supported bases.

- 2026-07-23 conjectured: an unbounded monitor channel was harmless because coalescing discarded hint payloads. / refuted by: the audit showed producers could enqueue unlimited discarded hints before the consumer drained them. / learned: payload irrelevance strengthens the case for a bounded occurrence signal; it does not make queue growth free. / criterion now: ISC-30 through ISC-33 require capacity-one signaling, bounded queued work, payload-independent behavior, and retained max-coalesce coverage.

- 2026-07-28 conjectured: monitor lifecycle races should be fixed by coordinating transitions — a `Stopping` state, a start-throws contract, a stop `TimeoutException`, and a drain-then-dispose ceremony for the shared rescan gate. / refuted by: each addition defended one transition and created the next reviewable edge, and two HIGH races survived anyway — a scan hung past the stop timeout could return and publish stale device truth, and a restart wedged behind the abandoned scan's hold on the shared gate. / learned: the invariant was never "transitions must be orderly"; it is "a generation may do anything except publish stale truth." Gating the single dangerous act is subtraction, and it made four concepts unnecessary; coordinating the transitions was addition that made them load-bearing. / criterion now: ISC-86 through ISC-90 require admission-checked publication, cross-generation mutual exclusion, subscriber isolation from lifecycle, restart after an abandoned stop, and no semaphore disposal at all.

- 2026-07-28 conjectured: a drain-then-dispose ceremony was needed so a hung rescan's `Release()` could not hit a disposed semaphore. / refuted by: `SemaphoreSlim.Dispose()` is only required when `AvailableWaitHandle` is used, and the monitor never touches it — the ceremony protected a disposal that was never needed. / learned: before coordinating access to a teardown, check whether the teardown is required at all; the safest disposal is the one that does not happen. / criterion now: ISC-90 asserts zero `SemaphoreSlim.Dispose` calls, making the use-after-dispose race unrepresentable rather than merely unlikely.

- 2026-07-28 conjectured: the registered-connection disposal defect was a double-dispose bug, fixable with an idempotence flag. / refuted by: writing the test exposed a second, worse face — a losing caller returned while the winner was still closing a PC/SC handle, inviting an immediate reopen of a dying handle. A bare idempotence flag would have made the counting correct and left the timing broken. / learned: "exactly once" and "returning means finished" are separate guarantees, and only the second one prevents the reopen. / criterion now: ISC-91 and ISC-92 require single-winner teardown, lease release strictly after inner completion, and a shared completion that every caller — sync or async — observes.

- 2026-07-28 conjectured: keeping the `PcscProtocolScp` constructor public was safe because it validates its base and throws on an unsupported one. / refuted by: the validation only fires after a caller has already found a path they should never have taken, and v2's pre-release status plus `InternalsVisibleTo` meant the path could simply be removed at no migration cost. / learned: a runtime check on a public entry point is a weaker form of an entry point that does not exist. / criterion now: ISC-93 requires `WithScpAsync` to be the sole construction path, with the validation retained as an internal-mistake guard rather than a public contract.

## Verification

- ISC-1 — branch evidence captured before the baseline on 2026-07-23:

  ```text
  $ git status --short --branch
  ## yubikit-concurrency-fixes...origin/yubikit-concurrency-fixes
  ?? .claude/worktrees/
  ?? .playwright-mcp/
  ```

- ISC-2 and ISC-3 — `docs/plans/concurrency-resiliency-remediation/ISA.md` exists; inspection found the twelve fixed-order headings from `Problem` through `Verification`, each populated.
- ISC-4 — the pre-baseline status contained only the two pre-existing untracked directories shown above; writing this ISA added `docs/plans/concurrency-resiliency-remediation/`. No production or test path changed before or during the baseline.
- ISC-5 — baseline executed after the initial ISA was persisted and before any production or test edit:

  ```text
  $ dotnet toolchain.cs test
  TEST SUMMARY
    ✓ Yubico.YubiKit.Cli.Commands.UnitTests
    ✓ Yubico.YubiKit.Cli.Shared.UnitTests
    ✓ Yubico.YubiKit.Core.UnitTests
    ✓ Yubico.YubiKit.Fido2.UnitTests
    ✓ Yubico.YubiKit.Management.UnitTests
    ✓ Yubico.YubiKit.Oath.UnitTests
    ✓ Yubico.YubiKit.OpenPgp.UnitTests
    ✓ Yubico.YubiKit.Piv.UnitTests
    ✓ Yubico.YubiKit.SecurityDomain.UnitTests
    ✓ Yubico.YubiKit.WebAuthn.UnitTests
    ✓ Yubico.YubiKit.YubiHsm.UnitTests
    ✓ Yubico.YubiKit.YubiOtp.UnitTests
  Passed: 12 | Failed: 0 | Skipped: 0 | Total: 12
  toolchain: test: Succeeded (1 m 20 s)
  ```

  The twelve projects contained 1,689 tests: 1,686 succeeded, 0 failed, and 3 were reported skipped inside Core (one Windows-only test and two explicitly hardware-dependent SCP tests). The toolchain's project-level summary above reports no skipped projects. Process exit code was `0`.

### DevTeam Batch 1 — discovery single-flight and atomic ownership

- ISC-6 through ISC-14 implementation mapping: `ProtocolDeviceInfo.cs` owns a process-wide single-flight operation per resolved interface and concrete `ConnectionType`; `FindYubiKeys.cs` retains its unchanged success-only identity and metadata caches. `DiscoverySingleFlightTests.cs` proves repeated timeout coalescing, waiter-cancellation independence, fault removal/retry, and underlying-cancellation removal/retry.
- Single-flight RED command: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~DiscoverySingleFlightTests"` (the required `--` separator avoids the .NET 10 script-host `--project`/`--file` collision). Result: 3 tests, 2 failed, 1 passed. `RepeatedTimeouts` expected one connect but observed four; `OneWaiterCancels` expected one connect but observed two.
- Single-flight GREEN: the same focused command first reported 3/3 passed; after adding the underlying-cancellation retry regression it reported 4/4 passed, 0 failed, 0 skipped.
- ISC-15 through ISC-24 implementation mapping: `DeviceConnectionRegistry.cs` coordinates shared session leases and exclusive discovery leases; `PcscYubiKey.cs` and `HidYubiKey.cs` acquire before physical creation and transfer ownership to idempotent registered wrappers; `IDiscoveryConnectionProvider.cs`, `CompositeYubiKey.cs`, and `ProtocolDeviceInfo.cs` provide the internal lease-aware discovery route.
- Atomic-ownership RED command: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~DeviceConnectionOwnershipTests"`. Result: 4 tests, 2 failed, 2 passed. `ConnectAsync_OwnsInterfaceBeforePhysicalConnectionCreation` showed discovery remained blocked behind a second factory call; `ConnectAsync_WaitsWhileDiscoveryOwnsInterface` reported that session physical creation crossed the active discovery read.
- Atomic-ownership GREEN: the same focused command reported 4/4 passed, 0 failed, 0 skipped. Retained `DeviceConnectionRegistryTests` reported 4/4 passed, and `DiscoveryIdentityReaderTests` reported 1/1 passed.
- Core unit gate: `dotnet toolchain.cs -- test --project Core` reported 616 total, 613 succeeded, 0 failed, 3 pre-existing platform/hardware skips; toolchain project summary `Passed: 1 | Failed: 0 | Skipped: 0 | Total: 1`; exit `0`.
- ISC-71: `dotnet toolchain.cs -- resilience --fast` reported 34/34 runtime-resilience tests passed, 0 failed, 0 skipped; exit `0`.
- Formatting: initial `dotnet format --verify-no-changes` identified final-newline diagnostics in changed and unrelated files. Targeted `dotnet format --include` cleaned every Batch 1 C# file. A second full verification reported only pre-existing unrelated final-newline/import diagnostics plus existing trimming warnings, so ISC-76 remains unchecked because the repository-wide command did not exit zero.
- ISC-77: `git diff --check` exited `0` with no output.
- ISC-58, ISC-59, ISC-60, ISC-79: CrossVendorRouter selected OpenAI Engineer and Anthropic `claude-opus-4-8` Reviewer. Iteration 1 verdict was `PASS WITH NOTES`; zero HIGH findings. Reviewer notes: MEDIUM retained coordinator entries; MEDIUM uncancelled sessions can wait indefinitely behind a permanently hung discovery lease; LOW unreachable composite fallback/self-deadlock risk, sync-over-async compatibility helper, and per-waiter cleanup-continuation allocation.

### DevTeam Batch 1 — iteration 2 reviewer remediation

- Reviewer status: independent cross-vendor verdict `NEEDS WORK`. ISC-59 and ISC-60 were reopened; Batch 1 is not marked PASS pending orchestrator review.
- HIGH RED: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~DiscoverySingleFlightTests"` reported 6 tests, 2 failed, 4 passed. Transparent wrapper expected `DiscoveryReadSkippedException` but received `TimeoutException`; composite member expected safe skip but received `InvalidOperationException` from the forbidden public connect. The wrapper test canceled its private escape token and awaited connect exit, leaving no permanently blocked test task.
- HIGH GREEN: the same command reported 6/6 passed, 0 failed, 0 skipped. `ProtocolDeviceInfo` now validates `IDiscoveryConnectionProvider` before lease acquisition, and `CompositeYubiKey` throws safe skip rather than invoking a member's public `ConnectAsync`. Deterministic reader fakes explicitly implement the internal provider.
- Exact pre-SELECT regression: `ConnectAsync_SessionStartingImmediatelyBeforeDiscoverySelect_CannotCrossOwnership` pauses after physical discovery connection creation at the entry to first transmit, before the wire counter increments. A session starts in that interval; factory count and registry state prove it remains a waiter until discovery releases. This regression passed on addition because iteration 1's coordinator already enforced the invariant; no RED is claimed.
- Coordinator cancellation/priority regression: `Coordinator_CanceledWaiterDecrementsCount_AndRemainingSessionHasPriority` holds discovery, cancels one of two waiting sessions, releases discovery, proves discovery cannot reacquire ahead of the remaining waiter, then proves cancellation bookkeeping permits a later discovery lease. This regression passed on addition; no RED is claimed.
- Focused GREEN: `DeviceConnectionOwnershipTests` 5/5; `DeviceConnectionRegistryTests` 4/4; `DiscoveryIdentityReaderTests` 1/1; all had 0 failures and 0 skips.
- Core GREEN: `dotnet toolchain.cs -- test --project Core` reported 619 total, 616 succeeded, 0 failed, 3 pre-existing platform/hardware skips; toolchain project summary `Passed: 1 | Failed: 0 | Skipped: 0 | Total: 1`; exit `0`.
- Resilience GREEN: `dotnet toolchain.cs -- resilience --fast` reported 34/34 passed, 0 failed, 0 skipped; exit `0`.
- Residual LOW: idle coordinator entries remain for the process lifetime, bounded by unique interface IDs observed. This is documented rather than "fixed" with unsafe naive eviction.
- Iteration 2 cross-vendor re-review: Anthropic Opus returned `PASS WITH NOTES`. The prior HIGH self-deadlock and MEDIUM race-coverage findings are resolved. The only remaining note is LOW process-lifetime retention of idle coordinator entries; the Reviewer explicitly classified it as non-blocking and preferred it over unsafe naive eviction.

### DevTeam Batch 2 — SCP and monitor resiliency

- Review status: iteration 1 returned `NEEDS WORK`; iteration 2 closed the batch with Anthropic Opus `PASS` and no HIGH or MEDIUM findings.
- SCP RED: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~PcscProtocolScpTests.Constructor_UnsupportedBaseProtocol"` reported 1 test, 1 failed: expected `ArgumentException`, but the unsupported `ISmartCardProtocol` constructed successfully.
- SCP GREEN: the same test reported 1/1 passed. The exception has parameter `baseProtocol` and names `PcscProtocol`; `PcscProtocolConcurrencyTests.ScpWrapper_SharesGateWithBaseProtocol` reported 1/1 passed, and the full `PcscProtocolScpTests` class reported 16/16 passed.
- Startup RED: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~YubiKeyDeviceMonitorServiceTests.StartMonitoring_"` reported 9 tests, 3 failed, 6 passed. SmartCard factory failure leaked the created HID listener; HID and SmartCard start failures left callbacks/listeners uncleared and undisposed.
- Startup GREEN: the same filter reported 9/9 passed. Tests cover factory-2 failure, HID `Start` failure, SmartCard `Start` failure, a throwing HID `Stop`, best-effort disposal of both listeners, initiating-exception preservation, `IsMonitoring == false`, stale callback isolation, and successful retry.
- Signaling RED was captured honestly in two steps. The first focused run failed compilation because the pending-signal seam did not exist. Minimal count instrumentation over the old unbounded channel then produced the behavioral RED: `EventStorm_DuringBlockedScan_KeepsExactlyOnePendingWakeUp` expected `1` but observed `128` queued requests while the initial scan was blocked.
- Signaling GREEN: the same focused test reported 1/1 passed using `DeviceMonitorSignal`. The full `YubiKeyDeviceMonitorServiceTests` class reported 27/27 passed, including existing burst, quiet-period, periodic fallback, and `MaxCoalesceInterval` regressions.
- Core GREEN: `dotnet toolchain.cs -- test --project Core` reported 624 total, 621 succeeded, 0 failed, 3 pre-existing platform/hardware skips; toolchain project summary `Passed: 1 | Failed: 0 | Skipped: 0 | Total: 1`; exit `0`.
- Resilience GREEN: `dotnet toolchain.cs -- resilience --fast` reported 35/35 passed, 0 failed, 0 skipped; exit `0`.
- Formatting: all Batch 2 C# files were targeted through `dotnet format --include`. Repository-wide `dotnet format --verify-no-changes` still reports only unrelated pre-existing final-newline/import diagnostics and existing trim warnings; ISC-76 remains unchecked.
- Whitespace: `git diff --check` exited `0` with no output after all Batch 2 code, tests, documentation, and ISA updates.

### DevTeam Batch 2 — iteration 2 reviewer remediation

- Reviewer status: Anthropic `claude-opus-4-8` re-reviewed the scoped Batch 2 diff and returned `PASS`; ISC-62, ISC-63, and ISC-80 are checked.
- Status-validation RED: `StartMonitoring_HidReturnsError_RollsBackAndAllowsRetry` and `StartMonitoring_SmartCardReturnsError_RollsBackAndAllowsRetry` each ran separately and failed 1/1 because no exception was thrown after `Start()` returned with `Status == Error`.
- Status-validation GREEN: both focused commands reported 1/1 passed. Deterministic messages are `HID listener failed to start (status: Error).` and `SmartCard listener failed to start (status: Error).`; each test proves callbacks cleared, attempted listeners stopped, acquired listeners disposed, `IsMonitoring == false`, and a subsequent start succeeds.
- Observable blocked-scan regression: `EventStorm_DuringBlockedScan_ProducesExactlyOneFollowUpScan` now releases the blocked initial scan, waits for exactly one follow-up scan, then proves the count remains `2` through a quiet period. It passed both before and after iteration-2 production changes; no RED is claimed.
- Sustained-producer regression: the producer now emits 64-hint bursts separated only by `Task.Yield`; `SustainedHintStorm_RescanRunsWithinMaxCoalesceInterval` remained bounded and passed before and after the fix. No RED is claimed. Production nevertheless removes the theoretically unbounded drain loop: each wake consumes one capacity-one signal and immediately re-enters debounce, where elapsed max-coalesce time is checked.
- Simplification: removed `PendingRescanSignalCount`, the mirrored `_pending` bit, and `DeviceMonitorSignal`'s lock. The signal wrapper now delegates only `TryWrite`, `WaitToReadAsync`, one `TryRead`, and completion to a capacity-one channel.
- Focused GREEN: all `YubiKeyDeviceMonitorServiceTests` reported 29/29 passed; relevant `PcscProtocolScpTests` remained 16/16 passed.
- Core GREEN: `dotnet toolchain.cs -- test --project Core` reported 626 total, 623 succeeded, 0 failed, 3 pre-existing platform/hardware skips; toolchain project summary `Passed: 1 | Failed: 0 | Skipped: 0 | Total: 1`; exit `0`.
- Resilience GREEN: `dotnet toolchain.cs -- resilience --fast` reported 35/35 passed, 0 failed, 0 skipped; exit `0`.
- Iteration 2 cross-vendor re-review: Anthropic Opus confirmed that status validation reaches transactional rollback, the signal has no mirrored state, one-signal consumption cannot starve deadline evaluation, regressions assert observable scan behavior, and SCP gate ownership remains correct. Verdict: `PASS`, with zero HIGH and zero MEDIUM findings. Two informational LOW notes recorded the bounded busy loop during a continuous producer storm and the intentional factory/callback wiring order; neither was classified as a defect.

### DevTeam Batch 3 — Linux descriptor and shutdown resiliency

- Review status: iteration 1 returned `NEEDS WORK`; iteration 2 closed the batch with Anthropic Opus `PASS` and no findings.
- Preparatory seam extraction preserved the buggy behavior while making it cross-platform testable: existing `LinuxHidDeviceListenerFaultInjectionTests` remained 14/14 GREEN. `IsValidFileDescriptor` retained the zero rejection, and `WriteShutdownSignal` retained the one-shot write policy until behavioral RED was captured.
- Discarded pre-RED: the first new-test invocation failed before test execution because a tuple-list `Add` method group did not match `Action<int, int>`. The test callback was corrected to a lambda without changing production behavior; this run is not counted as RED evidence.
- Accepted RED: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~LinuxUdevHidEventSourceTests"` executed 10 tests and reported 5 failed, 5 passed. Descriptor `0` expected `true` but was `false`; `EINTR`-then-success expected 2 writes but observed 1; repeated `EINTR` expected 4 writes but observed 1; zero-byte and four-byte writes expected one diagnostic but observed none.
- GREEN: the same focused command reported 10/10 passed, 0 failed, 0 skipped. Scripted delegates verify fd `0` and positive descriptors accepted, negative rejected, immediate errno capture, one retry per `EINTR`, exact eight-byte success, `EAGAIN` acceptance, one-shot reporting for nonretryable errno, and one-shot reporting for zero/short writes. The tests call no native library and run on every platform.
- Listener-policy regression: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~LinuxHidDeviceListenerFaultInjectionTests"` reported 14/14 passed, 0 failed, 0 skipped.
- Core GREEN: `dotnet toolchain.cs -- test --project Core` reported 636 total, 633 succeeded, 0 failed, 3 pre-existing platform/hardware skips; toolchain project summary `Passed: 1 | Failed: 0 | Skipped: 0 | Total: 1`; exit `0`.
- Resilience GREEN: `dotnet toolchain.cs -- resilience --fast` reported 45/45 passed, 0 failed, 0 skipped; exit `0`.
- Scope: production changed only `LinuxHidEventSource.cs`; tests added only `LinuxUdevHidEventSourceTests.cs`. No libc interop types, other platform listeners, app modules, or integration tests changed.
- Formatting: targeted `dotnet format` and targeted `dotnet format --verify-no-changes --include` both succeeded for the two Batch 3 C# files. Repository-wide verification still reports only the previously recorded unrelated final-newline/import diagnostics and trim warnings, so ISC-76 remains unchecked.
- Whitespace: `git diff --check` exited `0` with no output after the Batch 3 implementation, tests, and ISA update.

### DevTeam Batch 3 — iteration 2 reviewer remediation

- Reviewer status: iteration 1 verdict `NEEDS WORK`; iteration 2 verdict `PASS`. ISC-65, ISC-66, and ISC-81 are satisfied.
- Accepted RED: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~LinuxUdevHidEventSourceTests"` executed 12 tests and reported 2 failed, 10 passed. The zero-extended native `-1` regression expected rejection but the `IntPtr` policy returned `true`; the persistent-`EINTR` regression exhausted its four scripted failures and caught an unexpected fifth write, proving the loop was unbounded.
- ABI GREEN: `Udev.NativeMethods.udev_monitor_get_fd` now matches the documented C signature with an `int` return. `Initialize` carries that signed `int` directly into validation and `PollFd.fd`; `IsValidFileDescriptor(int)` accepts zero through `int.MaxValue` and rejects `-1` through `int.MinValue`. A compile-time method-group regression fixes the managed ABI contract to `Func<LinuxUdevMonitorSafeHandle, int>` without invoking libudev.
- EINTR GREEN: `MaxShutdownWriteAttempts` is a private constant of `4`. Three transient `EINTR` results may still recover through an exact eight-byte fourth write; four persistent `EINTR` results produce exactly four writes, four immediate errno reads, one diagnostic callback, and return. `EAGAIN`, exact writes, incomplete writes, and other errno paths retain their prior assertions.
- Focused GREEN: the same event-source command reported 15/15 passed, 0 failed, 0 skipped after expanding signed-int boundaries and adding the ABI and persistent-interruption regressions.
- Listener-policy regression: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~LinuxHidDeviceListenerFaultInjectionTests"` reported 14/14 passed, 0 failed, 0 skipped.
- Core GREEN: `dotnet toolchain.cs -- test --project Core` reported 641 total, 638 succeeded, 0 failed, 3 pre-existing platform/hardware skips; toolchain project summary `Passed: 1 | Failed: 0 | Skipped: 0 | Total: 1`; exit `0`.
- Resilience GREEN: `dotnet toolchain.cs -- resilience --fast` reported 50/50 passed, 0 failed, 0 skipped; exit `0`.
- Formatting: targeted `dotnet format` and `dotnet format --verify-no-changes --include` succeeded for `Udev.Interop.cs`, `LinuxHidEventSource.cs`, and `LinuxUdevHidEventSourceTests.cs`.
- Whitespace: `git diff --check` exited `0` with no output after iteration-2 code, tests, and ISA evidence.
- Independent orchestrator verification repeated `dotnet toolchain.cs -- test --project Core` and reported 641 total, 638 succeeded, 0 failed, 3 pre-existing platform/hardware skips; `dotnet toolchain.cs -- resilience --fast` reported 50/50 passed, 0 failed, 0 skipped; the following `git diff --check` exited `0`.
- Iteration 2 cross-vendor re-review: the existing Anthropic Opus Reviewer confirmed the native `int` ABI, nonnegative descriptor policy, four-attempt `EINTR` bound, `EAGAIN` handling, incomplete-write diagnostics, and deterministic regression coverage. Both prior findings are resolved. Verdict: `PASS`, with no findings.

### Final hardware-discovered discovery remediation

- Synchronous-start RED: a provider that blocks before returning its `Task` delayed a nominal 50 ms caller budget to 807 ms. GREEN schedules the admitted shared operation before provider/native invocation; `DiscoverySingleFlightTests` now report 8/8 passed.
- PC/SC stage timing localized the RSA-4096 stall: `SCardGetStatusChange(context, 0, ...)` consumed 49.866 seconds behind the in-flight transaction. Recognized integrated SmartCard-capable USB YubiKey readers now bypass that status probe; generic, NFC, malformed, and OTP-only names retain status/ATR validation. `FindPcscDevicesTests` report 4/4 passed.
- Group-budget RED: two independent blocked duplicate-PID identity reads accumulated 4.042 seconds sequentially. GREEN uses concurrent ordered identity resolution; `FindYubiKeysPidMergeTests` report 3/3 passed and the live identity group completed in approximately 2.004 seconds.
- Worker-bound RED proved unique hung interface IDs could exceed a process-wide resource budget. GREEN admits exactly four native discovery workers and skips excess best-effort reads without queueing. Saturation separately propagates PC/SC enumeration failure; lower-level and repository-boundary regressions prove no native enumeration begins, no empty snapshot commits, and no false removal emits.
- Follow-up cross-vendor review required three iterations: first HIGH/MEDIUM findings requested global worker bounds and a strict CCID predicate; the next MEDIUM prevented saturation from masquerading as no devices; final verdict `PASS` with no findings.

### Final verification gates

- `dotnet toolchain.cs build`: final post-format run exited `0` with 0 warnings and 0 errors.
- `dotnet toolchain.cs test`: all 12 projects passed. Final current-code Core run reported 651 total, 648 succeeded, 0 failed, and 3 expected platform/hardware skips.
- `dotnet toolchain.cs -- resilience --fast`: final current-code run reported 57/57 passed, 0 failed, 0 skipped.
- `dotnet toolchain.cs -- test --integration --project Management --smoke`: unit and integration projects passed; 39 integration tests passed and 12 capability/form-factor cases skipped.
- `dotnet toolchain.cs -- test --integration --project Piv`: final current-code run reported 65/65 unit tests and 75/75 integration tests passed; 0 failed and 0 skipped. The unchanged RSA-4096 discovery-contention test and both strengthened multi-key tests passed.
- `dotnet toolchain.cs -- test --integration --project Piv --filter "FullyQualifiedName~PivMultiKeyContentionTests"`: both integration tests executed and passed on two allow-listed SmartCard-capable keys; no integration test skipped. The unit project reported only the expected no-filter-match project skip.
- `dotnet toolchain.cs -- test --integration --project Core --smoke`: after satisfying the class's documented single-composite-key precondition, 21/21 integration tests passed; Core unit tests also passed. The runner excluded `Slow` and `RequiresUserPresence`, so no plug/unplug test executed.
- `dotnet format` corrected the 23 repository-baseline diagnostics: 22 final-newline findings and one import-order finding. The final `dotnet format --verify-no-changes` run exited `0`; it emitted only the existing IL2026 and IL3050 trim-analysis warnings from `Tests.TestProject`, which are not formatting failures. ISC-76 is satisfied.
- `git diff --check`: exit `0` with no output. Final status contains only the intended remediation paths and ISA, plus the pre-existing untracked `.claude/worktrees/` and `.playwright-mcp/` directories.
- Final same-vendor complete-diff review initially found two MEDIUM gaps. GREEN moved completion/removal ownership into one `SharedRead` observer regardless of waiter count and strengthened the multi-key test to require key B's `ykphysical:{serial}` identity during the active contention scan. Re-review verdict: `PASS`, no findings.
- Final cross-vendor DevTeam routing selected `anthropic/claude-opus-4-8`. A mandatory 321,181-byte inline packet contained the complete diff and full changed-file contents for a tool-disabled review. Verdict: `PASS WITH NOTES`; no HIGH findings. ISC-67 and ISC-68 are satisfied.

### Post-push CI remediation

- GitHub `build-and-test` (headless Linux, no PC/SC) failed 10 Core unit tests after the transactional startup change. Root cause: the new strict startup (`EnsureListenerStarted`) throws when a listener cannot reach `Started`, and the CI runner has no PC/SC service, so the SmartCard listener returns `Error`. The base commit had no status validation and therefore never threw.
- Decision (owner): keep the strict all-or-nothing throw as reviewed; do not weaken production validation.
- Fix, tests only: four `YubiKeyDeviceMonitorServiceTests` lifecycle tests that accidentally used the real-listener constructor now use the existing fake-listener `CreateService()` seam; six `YubiKeyManagerStaticTests` monitoring tests (which exercise the seamless static manager) are runtime-gated via `StartMonitoringOrSkip`, which skips only when the environment reports `listener failed to start`. Where PC/SC is available (local dev, hardware runners) all six still run and fully assert behavior.
- Local verification after the fix: full `dotnet toolchain.cs test` 12/12 projects passed (Core 648 succeeded, 3 expected skips, 0 failed); `dotnet toolchain.cs -- resilience --fast` 57/57; `dotnet format --verify-no-changes` exit `0`; `git diff --check` clean. The no-PC/SC skip path can only be confirmed by GitHub CI after push.
- That fix landed as `4f326c9e` and CI confirmed green (Core `total: 651, skipped: 9, failed: 0`). It was then superseded by the canonical-alignment change below.

### Canonical alignment: graceful listener degradation

- Superseding decision (owner): after reviewing the canonical implementations, adopt graceful degradation instead of the strict all-or-nothing throw. Canonical evidence gathered read-only from `/Users/Dennis.Dyall/Code/y/yubikey-manager-rust-auto`:
  - Rust (authoritative) `crates/yubikit/src/platform/device.rs:719,736,745`: each transport enumerates behind `if let Ok(...)`, so a failing transport (e.g. PC/SC `Context::establish` error at `platform/pcsc.rs:80`) is swallowed and discovery continues with the others. The only hard error is a compile-time feature gate (`device.rs:703`), never a runtime service outage.
  - Python `packages/yubikit/yubikit/device.py:158`: `list_readers()` catches `OSError` and returns `[]`. "Monitoring" in ykman (`ykman/scripting.py:146`) is a `while True: scan_devices(); sleep(1)` poll, so there is no long-lived listener that can fail hard.
- New production contract (`YubiKeyDeviceMonitorService`): listeners are best-effort, independent accelerators. Each is started on its own; a throw or post-`Start()` status other than `Started` is logged, that listener is individually stopped/disposed, and startup continues. Monitoring always starts — worst case with zero listeners it runs on the interval fallback rescan alone, because device truth is the full `FindAllAsync` + repository diff, not listener hints. `StartMonitoring` no longer throws for listener unavailability (only invalid interval / disposed service). Individually failed listeners are still cleaned up so no partial resources leak, and detached callbacks cannot signal a later run.
- Superseded criteria: ISC-34..40 described strict transactional all-or-nothing startup (partial failure aborts, throws, and leaves the service not monitoring). Their intent is now reframed:
  - ISC-35/36/37 (clean up listeners on failure) remain satisfied, but per-listener rather than whole-attempt.
  - ISC-38 (dispose the attempt's channel resources on failure) no longer applies, because partial failure is no longer a failed attempt.
  - ISC-39 (Anti: failed startup leaves monitor marked running) is replaced: a partially-failed startup now *intentionally* leaves monitoring running with the available listeners.
  - ISC-34/40 (RED regression + clean retry) are replaced by the degradation regressions below.
- New criteria (all verified by tests in `YubiKeyDeviceMonitorServiceTests`):
  - ISC-82: A SmartCard listener that reports `Error` or throws does not abort startup; HID keeps monitoring active (`StartMonitoring_SmartCardReturnsError_StartsWithHidOnly`, `_SmartCardFactoryThrows_StartsWithHidOnly`, `_SmartCardStartThrows_StartsWithHidAndDetachesFailedListener`).
  - ISC-83: A HID listener that reports `Error` or throws does not abort startup; SmartCard keeps monitoring active (`StartMonitoring_HidReturnsError_StartsWithSmartCardOnly`, `_HidStartThrows_StartsWithSmartCardOnly`).
  - ISC-84: When both listeners fail, monitoring still starts on the interval fallback alone and does not throw (`StartMonitoring_BothListenersFail_StartsIntervalOnlyMonitoring`).
  - ISC-85: A failed/detached listener is individually stopped and disposed (no leak) and its callback can no longer trigger a rescan.
- Test-gating reverted: the `StartMonitoringOrSkip` runtime skip added in `4f326c9e` is removed; the six `YubiKeyManagerStaticTests` now call `StartMonitoring` directly and positively assert graceful degradation in headless CI (no PC/SC → HID/interval keeps `IsMonitoring == true`). The four `YubiKeyDeviceMonitorServiceTests` lifecycle tests retain the deterministic fake `CreateService()` seam.
- Docs: the `src/Core/CLAUDE.md` monitor startup contract was rewritten from "transactional" to "best-effort graceful degradation."
- Verification (local): `dotnet toolchain.cs build` 0 errors; full `dotnet toolchain.cs test` 12/12 (Core 648 succeeded, 3 expected skips, 0 failed, 0 gated skips); `dotnet toolchain.cs -- resilience --fast` 57/57; `git diff --check` clean. Cross-vendor review performed via Copilot CLI (gpt-5.5) on the diff.
- Formatting (corrected): `dotnet format whitespace --verify-no-changes` and `dotnet format style --verify-no-changes` both exit `0` (all changed files are format-clean). The aggregate `dotnet format --verify-no-changes` exits `2`, but solely because the analyzers pass surfaces two pre-existing, non-auto-fixable `IL2026`/`IL3050` trim/AOT warnings in `src/Tests.TestProject/Program.cs` — a file untouched by this work. No changed file appears in any format diagnostic.
- Correction to the earlier ISC-76 note: the phase-1 claim that `dotnet format --verify-no-changes` "exited 0" was a measurement error (`$?` captured the exit of a piped `tail`, not `dotnet format`). The accurate status is the two-line result above: whitespace+style are clean; the aggregate command's non-zero exit is a pre-existing analyzer-warning baseline unrelated to this PR, so ISC-76 is met for formatting proper and blocked only by that untouched-file baseline.
- Cross-vendor review (Copilot CLI, gpt-5.5) on the graceful-degradation diff: verdict `PASS WITH NOTES`, no HIGH findings. Two MEDIUM notes were addressed:
  - MEDIUM 1 (test strength): `StartMonitoring_BothListenersFail_...` now waits for `ScanCount >= 2` with a 200ms interval, proving the interval fallback keeps driving rescans with zero listeners rather than only the one-shot startup rescan.
  - MEDIUM 2 (doc accuracy / scan-layer isolation): confirmed via `FindPcscDevices` that the common no-PC/SC cases (missing native lib, `SCardEstablishContext` failure, no readers) return empty, so `FindAllAsync` still enumerates HID and the interval diff detects it. A PC/SC enumeration *exception* (worker saturation or `SCardGetStatusChange` error) instead aborts that one scan — intentionally, because this PR's earlier remediation requires that a failed PC/SC probe never be committed as a false-empty snapshot (which would emit spurious removals). The `CLAUDE.md` claim was corrected to state this precisely. Full per-transport scan-layer isolation (enumerate HID even when PC/SC enumeration throws, without reintroducing false removals) is deliberately deferred to the polling-migration ISA, since it changes `FindAllAsync`/repository-diff semantics rather than listener startup.

### Epoch-gated publication, one-shot disposal, and SCP closure

- Frame change (owner-approved, cross-vendor audited): the three DevTeam findings remaining after the graceful-degradation work — MEDIUM registered-connection double disposal, HIGH stop-timeout restart overlap, HIGH rescan/dispose gate race — were originally planned as added machinery. That plan was rejected as the bulldozer pattern applied to planning, and replaced by the subtraction frame recorded in this Changelog. The replacement plan passed a cross-vendor audit (OpenAI gpt-5.6-sol, round 2, zero findings) before implementation began.
- ISC-86 through ISC-90 implementation mapping: `YubiKeyDeviceMonitorService.cs` now holds one immutable `MonitorGeneration { Id, ScanGate, Signal, Cts }` in a single volatile `_current`. `PublishSnapshotAsync` acquires the never-disposed `_publishGate`, checks admission (`ReferenceEquals(gen, _current) && !_disposed`) under the small `_publishLock`, and calls `UpdateCache` while holding the gate. Lifecycle swaps `_current` under `_publishLock` only. The drain-then-dispose block was deleted; no `SemaphoreSlim.Dispose` call remains in the file.
- Epoch RED: seven new tests were run against the pre-change implementation; four failed on the predicted defects. `RescanAsync_SupersededByLifecycleSwap_DiscardsStaleSnapshot` published a stale device; `SlowScan_OutlivingStopTimeout_CannotPublish_AndRestartRecovers` emitted a device event after an abandoned stop; `StopMonitoring_TimesOutOnHungScan_RestartPublishesWithNewGeneration` never published from the successor; `CrossGenerationPublications_SerializeAndSuccessorSnapshotLandsLast` never let the successor scan at all.
- Epoch GREEN: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~YubiKeyDeviceMonitorServiceTests"` reported 38/38 passed. ISC-86 is proved by the four admission tests, ISC-87 by the cross-generation ordering test (max concurrent emissions == 1), ISC-88 by the blocking-subscriber test plus the bounded dispose drain, ISC-89 by the restart-after-timeout test, and ISC-90 structurally — grep confirms only CTS disposals remain, each guarded by loop-observed-stopped.
- ISC-91 and ISC-92 implementation mapping: `DisposalGate.cs` gives the first caller the claim via one `Interlocked.CompareExchange` on the completion `Task` field; it disposes the inner connection and releases the lease in a `finally`, and every other caller observes that same task. Deadlock is avoided structurally: a sync winner's teardown completes inline before the task is returned, the single `await` is `ConfigureAwait(false)`, the claim uses `RunContinuationsAsynchronously`, and losers block on a plain task via `GetAwaiter().GetResult()` (which also unwraps to the original exception instance).
- Disposal RED: six new tests, all failing against the pre-change wrappers. The sync/async race tests for all three wrappers failed on `Assert.False(syncDispose.Wait(250ms))` — the loser returned immediately; the shared-completion test showed the loser returning mid-teardown; repeated disposal counted three inner disposals; the failure-propagation test showed each caller re-running teardown and observing a *different* exception instance.
- Disposal GREEN: the focused class reported 10/10. A methodological note worth keeping: the first draft of the fake blocked on the control gate for *every* disposal call, which hid the early-return defect entirely — both callers blocked in the fake, so the assertion passed against broken code. Blocking only the winner's teardown exposed it. A test that fails only for the reason you already knew about is weaker than it looks.
- ISC-93: `PcscProtocolScp`'s constructor is `internal`; the concrete-base validation and shared-gate assignment are byte-for-byte unchanged. `InternalsVisibleTo` kept all 21 direct-construction test sites compiling untouched, so `Constructor_UnsupportedBaseProtocol` (16/16 class) and `ScpWrapper_SharesGateWithBaseProtocol` (5/5 class) remain as written. Migration guidance was added to `v1-to-v2-map.yml` (`scp-protocol-construction`), `v1-to-v2.md`, and `v1-to-v2-changelog.md`.
- Subtractions landed alongside, each verified dead before removal: `DeviceConnectionRegistry.Register` (sync-over-async, no production caller); both discovery caches made non-nullable after tracing that every write is behind a successful-read guard, correcting a comment that falsely claimed null recorded a failed read; the two `CleanupListeners` booleans, which at all four call sites duplicated the null check they were ANDed against; `ProtocolDeviceInfo.ActiveCompletionObserverCount` and its interlocked bookkeeping, whose only observer was its own assertions — single-flight is pinned by the connect-count assertion and the one-completion-path property by the completion-log assertions in the same test. A non-generic `AsyncExchangeGate.RunExclusiveAsync` overload replaced three faked return values, and three private gate-held helpers gained an `UnderGate` suffix.
- Doc corrections: `docs/architecture/event-driven-device-discovery.md` claimed a `Channel<DeviceMonitorRescanRequest>` with queue draining. Neither exists — the implementation uses a capacity-one `Channel<bool>` with `FullMode = DropWrite`, consumed one occurrence per wake-up. The doc now describes that and the generation model. `src/Core/CLAUDE.md` and `src/Core/README.md` record the epoch model and the disposal gate.
- Hardware policy recorded (owner decision): allow-listed serials are dedicated test keys, so state mutation, PIV reset, and key generation on them are authorized without per-run approval — the allow list's `Environment.Exit(-1)` hard-fail is the boundary and no second config gate was added. User Presence, UV, touch, and insert/remove timing remain human-coordinated, because the gate is presence and timing rather than destruction. Written into `docs/TESTING.md` (new Hardware Authorization section) and `src/Tests.Shared/README.md`.

### Hardware verification and final gates (epoch-model work)

- Hardware: two allow-listed test keys connected throughout. `MonitorService_Enabled_Tests` 2/2;
  `ManagementHidConcurrencyTests` 2/2 (HidFido and HidOtp); `PivConnectionConcurrencyTests` 1/1;
  `PivDiscoveryContentionTests` 2/2, including the RSA-4096 keygen contention case that runs
  ~3 minutes and proves `FindAllAsync` does not wait for a busy card. Zero skips in all four.
- Blocked gate, pre-existing, NOT caused by this work: `PivMultiKeyContentionTests` skips both cases
  (`GetSmartCardStatesOrSkip(2)` finds one SmartCard-capable authorized key, not two), and four
  `CompositeDiscoveryIntegrationTests` fail. Both were reproduced identically at `e0516ba6`, before
  any epoch-model commit, so neither is a regression. Root cause is composite merging with two
  identical keys: PC/SC exposes both readers (`Yubico YubiKey OTP+FIDO+CCID` and `... 02`) and both
  serials are read, but interfaces are misgrouped nondeterministically across runs — one observed
  grouping was `ykphysical:103 = HidFido|HidOtp`, `ykphysical:125 = HidOtp|SmartCard`, with two
  orphaned single-interface devices. The interface count is right, the attribution is not. The four
  `CompositeDiscoveryIntegrationTests` additionally hard-assert `Assert.Single`, so they can only pass
  with exactly one key connected. This is a discovery/merge defect, out of scope for a concurrency
  plan, and is left for the polling-migration follow-up rather than fixed opportunistically here.
- Final gates: full solution build 0 errors (1 pre-existing CS7022 in the NuGet-generated
  `Tests.TestProject` entry point); full unit suite 12/12 projects with Core at 668 total, 665
  succeeded, 3 platform skips, 0 failed; `resilience --fast` 67/67; `git diff --check` clean.
- ISC-76 finally met for formatting proper: `dotnet format --verify-no-changes` now reports **zero
  errors**. The previously recorded FINALNEWLINE diagnostic in `src/Tests.Shared/Infrastructure/
  TestCategories.cs` was traced to this branch (the file was clean at the merge base and picked up a
  trailing newline in `e0516ba6`) and fixed. The command's exit code is still non-zero, but now solely
  because of the two pre-existing IL2026/IL3050 trim warnings in the untouched `Tests.TestProject`.
- Simplification review of the changed set produced three accepted items: primary constructors
  restored on the registered-connection wrappers (about sixty lines of the diff had been mechanical
  `inner` -> `_inner` renaming that hid the one real change); `MonitorGeneration.Cts` no longer
  disposed, which deleted the tuple return from `StopMonitoringCore`, the `loopStopped` local, and
  three comments that existed only to answer "is it safe to dispose this yet?"; and `PcscProtocolScp`
  sealed, since an internal constructor already made external derivation impossible.
- Cross-vendor review (OpenAI reviewer; author Anthropic) took three rounds and earned its keep:
  - Round 1 `NEEDS WORK`, warning: the documented contract claimed the manager's repository disposal
    "silences any later emission". It does not — `UpdateCache` calls `ThrowIfDisposed` and the subject
    throws once disposed. A genuine claim-vs-delivery gap. Fixed at the publish site rather than by
    adding a manager/repository synchronization boundary, which would have re-added exactly the kind
    of coordination this plan removed.
  - Round 2 `NEEDS WORK`, warning: the resulting catch was too broad. `UpdateCache` invokes subscribers
    synchronously, so a subscriber touching its own disposed state throws the same type and would have
    been silently misattributed to shutdown. Narrowed to `when (_disposed == 1)`.
  - Round 3 `PASS`, zero findings.
- Test-writing lesson recorded twice in this work, in different forms: the first draft of the
  late-publication test used one device and passed against the unfixed code, because a stalled
  publication is already past `ThrowIfDisposed` and the throw comes from a *later* `OnNext` in the same
  `UpdateCache` call. Two devices were needed to make it RED. Earlier, the first draft of the disposal
  fake blocked on every call and hid the early-return defect the same way. A test that fails only for
  the reason you already knew about is weaker than it looks — and one that passes on the first try
  against unfixed code is worthless, not reassuring.
