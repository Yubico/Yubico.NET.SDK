# Program design: incremental async boundaries

The single-key product and architecture gates were approved 2026-09-22.
First smart-card lifetime slice `db7a1bf6` was implemented and independently
reviewed PASS WITH NOTES. Its original pseudocode required owner-before-open,
one ordinary native job at a time, coalesced transaction-end/shutdown, checked
release or retained claim, and no early borrowed-input release. The shipped
connection owner implements those rules; subsequent built-in async begin
(ISC-51) and macOS routes are recorded in [current status](00-status.md).
This file defines **how to finish** a bounded increment, not another acceptance
registry. The [master](../../../2026-09-21-yubikit-async-boundaries-ISA.md#criteria)
retains all 72 IDs and historical D1–D31 decisions. The
[earlier program design](addenda/earlier-multi-key-program-design.md) retains
the deferred cross-key reservation proposal, not current implementation scope.

## Current scope: macOS evidence closure

The prior E1 listener callback-drain package finished: actual vendor-filtered
matching, selected removal and late matching-callback drain on pinned `.8`
supported ISC-33 at the bounded teardown grade. The orchestrator accepted it;
do not re-dispatch E1 or the inconclusive no-open removal experiment. Its
verification-only callback decorator did not add production hooks. Manager
close on physical removal reported a dead-port error, not positive per-device
close; run-loop exit, callback exit, unschedule and manager release order were
observed. Input Monitoring risk on other hosts remains an orchestrator-owned
product follow-up, not a reason to rescind the observed teardown evidence.

Current bounded work is **unattended, read-only Mac plus portable contracts**.
Implementation checkpoints are `8d13fd1e` (logging and FIDO response refactor)
and `8ebcd026` (synchronous drain, scan, registry and docs); no new native
package is implied. The Core-only source inventory classifies 205 outstanding sites:
134 native imports, 3 native exports, 24 blocking waits, 15 scheduling sites,
21 dispatch gaps, 6 callback registrations, 0 delegate conversions and
2 unmanaged callback addresses. Two shifted source-evidence line references
for HID scan and manager Shutdown were corrected without changing site IDs or
counts. The adapter test-link registry has **27 required
operations/49 profile links**, not 49 distinct tests (typed Mac FIDO 4, OTP 4,
portable PC/SC 5, direct input 4, direct feature 4, listener 2, portable PC/SC
synchronous begin/end/dispose and external default begin 4). The separate
public synchronous map pins 16 public type/method → owner/method entries to
named test links or explicit gaps: direct input/feature 8, smart-card 4,
listener 2, manager shutdown 1 and HID scan dispatch 1. Five negative cases
reject missing/duplicate/wrong mapping and arbitrary bare/stale evidence.
The HID scan row has three controlled tests via an internal enumeration delegate:
pre-cancel prevents submission, held enumeration returns a pending task and
late cancellation waits for resource completion, and failure preserves the
original exception without replay. Held-scan cleanup now releases the delegate
and awaits the actual scan before disposing its gates; targeted three passed
after this fix. Actual native platform enumeration was not tested by that seam.
Manager Shutdown remains an explicit bounded-monitor-stop
gap, not global native drain.
The 23/45 registry is historical; links and site classification are not
universal proof. Public docs for a changed route must state only its observed
cancellation, caller-return, borrowed-memory and drain/fault scope, and label
unverified external/platform behavior rather than generalizing a built-in path.
The [finite list](00-status.md#remaining-unattended-lanes-and-decisions)
is the dispatch order: contract registry/source classification, public waits,
diagnostics and exception docs, comparable measurements, then targeted route
verification/reconciliation. ISC-4/37/53/54/56/62 remain **open at full scope**.
Parent auditors handle disjoint code/test gaps; no engineer may infer a missing
test passes from this plan. Windows/Linux native and other hardware/operator
routes are deferred by user direction, not marked inapplicable. One attached
key is sufficient for a limited read-only observation but not universal closure.

| Work | Owner / finite outcome | Evidence and stop condition |
|---|---|---|
| Classification/registry | Orchestrator resolves remaining required operation/adapter rows and outstanding source sites with engineers' disjoint evidence; direct raw Mac rows are already registered. | Missing applicable rows stay pending ISC-4; runnable profiles must have nonzero tests. Preserve external-implementation and Windows/Linux gaps. |
| Public waits and documentation | Boundary/doc owner checks full public reachability, retained sync compatibility paths and cancellation/borrowed-memory/exception contracts. Built-in PC/SC withheld-native sync tests use a `LongRunning` caller-returned event, disposal snapshots and a finite 200 ms hold observation, not an all-races guarantee. External default async begin calls sync begin before task return and may block. | Link a named drain/fault test to each verified path; record uncovered paths explicitly. No Mac-only ISC-53/54 checkmark. |
| Diagnostics | Boundary owner inventories affected Core/applet logs and tests secret/response sentinels including externally supplied exceptions. Twelve diagnostics tests include real FIDO callback/failed cancel, OTP failed reset, PC/SC connection and protocol, and formatted/structured/exception positive controls. FIDO/OTP failure logs use exception type `FullName` only, excluding opaque external messages while preserving caller exceptions; PC/SC explicit structured command headers/length replace raw-object logs and public SELECT application-ID hex remains as requested. | First real sentinel exposed an opaque exception payload and was fixed; three protocols do not establish whole-Core/applet/external no-leak proof. ISC-56 stays pending. Never log credentials to create a probe. |
| Measurement | Measurement owner binds final source/binary/package and a comparable baseline/final fixture, including missing native-only/pending metrics. | Collect/review frozen finite samples and budget; incomparable historical `.3`/`.8` cohorts are descriptive only (ISC-62 pending). |
| Route verification | Selected-key read-only FIDO/OTP and smart-card begin/read/reopen plus existing managed/native tests. | Final-source published `.8` Native AOT `--probe` discovered one PID 0407 key (fallback identity), selected serial 31683481, and passed five production FIDO scenarios and three OTP info cycles without touch/replug. No fresh smart-card attempt: the last broad and isolated smart-card attempts both stopped at pre-open sharing contention. Record this blocker, not a fabricated smart-card pass or performance acceptance claim. |

The reviewed PC/SC change refuses subsequent commands on the same protocol
after interrupted plain chains or protected first transport/MAC/intermediate
failures once secure state advances. It preserves original exceptions, never
replays uncertain work and deduplicates wrapped plain SELECT continuation;
authenticated terminal application errors remain reusable. An independent
command-MAC card test held protected response continuation through caller
cancellation, then proved the next protected command's MAC: ISC-19 is accepted
at that single managed profile, not physical card grade. Three plain and one
wrapped SELECT cases were red then green; initial protected fixture failures
do not count as meaningful red evidence. Creating a new protocol on the borrowed
raw connection bypasses this latch; callers must reopen after uncertainty.
ISC-20 and ISC-37 remain open at their full scopes. Latest-source full Core
1,511 passed/3 skipped, PublicApi 22, Fido2 471, YubiOtp 180 and
resilience-fast 88 passed after restoring SELECT hex logging and adding the
HID discovery seam, before the implementation commits; no post-commit rerun
is inferred. Focused adapter registry 17, public-sync registry five,
PC/SC lifetime 30 and HID scan boundary three passed; targeted HID scan three
passed again after cleanup was corrected. Earlier targeted diagnostics 12,
PC/SC 23 and FIDO 40 are source-specific history. `FidoReceiveResponse` was split into
terminal, validation and copy helpers with the same control/error precedence
and buffer zeroing; 12 changed shipping methods were within cyclomatic
10/cognitive 20, not whole-project or untracked-test proof. Docs QA and
architecture validation passed. Independent review: PASS WITH NOTES after one
bounded test false-negative fix; cleanup now releases and awaits the held scan
before disposing gates, without an established production defect. The public
SELECT application-ID hex log remains.
Earlier Core 1,508/3, 1,492/3 and secure filter 159/2 are historical
results, not current runs.
Collected in the working tree before the implementation commits, the published
`.8` Native AOT `--probe` and `--otp-info` discovered one PID 0407
key using fallback identity and selected serial 31683481 (firmware 5.7.4).
Five production FIDO scenarios (including pending-read disposal/reopen and
pre-dispatch cancellation) and three typed OTP info cycles passed without touch
or replug. The first initialization was 229 ms, then 20 ms; do not treat these
as a comparable performance baseline or budget. The earlier two-attached-key
observation belongs to a different probe. The last broad smart-card integration
and isolated `--smartcard` failed sharing before open; no fresh smart-card probe
ran after that, so no final-source transaction/read is claimed. The isolated
read-only scenario has a 20-second watchdog; do not request operator work.

## Bounded slice finish line

1. Agree one observable route result, owner, bounded files/responsibilities,
   non-goals, applicable existing ISC IDs and required evidence grades.
2. Freeze named probes and exact focused commands before implementation; tests
   first for behavioral gaps. Reuse existing results only if the relevant
   source/package and risk are unchanged. A test with no matches is not a pass.
3. Require correct ownership, cancellation, release/quarantine, regression
   and exception behavior before declaring the **slice** done; missing native,
   hardware, platform or performance evidence blocks its respective claim.
4. Obtain one independent correctness review of the settled shape and review
   targeted fixes; default one implementation pass plus optional readability
   pass, at most two review/fix cycles before escalation. Do not generalize
   for future adapters or rerun the entire suite without a reason.
5. Reconcile results, exact provenance and limits in the existing master and
   status, not a new report. An investigation can finish at its pre-agreed
   limited evidence grade without checking a universal criterion. When editing
   public docs, assert only route-specific source/test evidence, state external
   synchronous fallback and unknown platform grades explicitly, and leave
   unsupported universal claims pending.

Focused existing commands (execute only when that route is affected):

```text
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~PcscConnectionLifetimeTests"
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~SmartCardConnectionFactoryTests"
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~PcscDiscoveryLifetimeTests"
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~BoundaryInventory"
dotnet toolchain.cs -- test --project PublicApi
dotnet toolchain.cs -- resilience --fast
dotnet toolchain.cs docs-qa
```

Use separate smart-card filters; the toolchain does not support `|`-joined
filters. `native-aot-contract-qa` is static project/anchor validation, not
native runtime execution. For device verification identify the actual key,
package, binary, host and command; do not substitute a build or empty discovery
for ISC-58/59. No tests or device probes are asserted to have run as part of
this documentation reconciliation.

## Decisions still requiring real evidence

- Public `IHidConnection` stays synchronous for direct report compatibility;
  its wholesale evolution (ISC-50) remains a separate consumer/visibility
  decision. Typed macOS FIDO/OTP use internal owned boundaries; external
  implementations retain their own documented synchronous fallback and
  lifetime responsibilities. Do not add a compatibility shim merely to
  conceal a proposed v2 surface change.
- Positive native release, callback quiescence and protocol recovery are
  separate; an irrecoverably hung call retains ownership. The direct raw
  caller is responsible for framing and safe sequencing. Checked macOS
  callback-context teardown does not prove permissionless device access.
- Original first-slice historical verification was 38 focused smart-card
  cases across three classes, Core 1,335 passed/3 skipped and PublicApi 22;
  these predate current source and cannot be used as current gates. The
  selected `.2` key passed read-only smart-card transaction cycles; later
  selected async acquisition/read/reopen passed after prior sharing
  contention. Historical `.3` FIDO touch, `.6` removal and `.7` candidate
  do not turn into `.8` hardware results. See the master verification index
  for unique artifact IDs, source revisions and accepted scoped criteria.
