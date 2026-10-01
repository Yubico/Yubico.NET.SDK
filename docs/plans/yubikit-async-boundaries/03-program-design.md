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

## User-approved local Mac and smart-card finish decision

After the user's replug, close out **local macOS and smart-card** work by the
following definition of done; parent checks evidence autonomously, without
asking for further operator action. This is a local phase decision, **not** a
waiver of the original 72-criterion epic or retrospective acceptance of an
unapproved performance budget. Parent accepted this local phase on actual
results at shipping `21498d241f74d64055b10e5b0fb36a47d68acc3b`;
**D1–D5 complete at 18/72; current global checklist 19/72 after ISC-50**. Local D1–D5 are distinct from the
master's historical D1–D31 program decisions.

| Local check | Evidence required before parent can mark local closure | State |
|---|---|---|
| D1 shipping route | Selected-key read-only typed/direct Mac and PC/SC, published `.8` on actual macOS runtime; reconcile existing listener proof without new removal. | **Complete:** at final shipping source, eight Native AOT modes passed: FIDO `--probe` five scenarios with public pending-task completion, automatic device-waiting `--active-cancel`, typed `--otp-info` three, direct `--expert-io` timeout/same-connection/reopen, direct `--expert-feature` three, `--smartcard` three async transaction/read/reopen cycles, and actual matching `--listener-drain`/`--listener-late-drain`. Selected serial 31683481, firmware 5.7.4. Pre-replug sharing failure is history, not a current blocker. |
| D2 observable comparison | Same-package/runtime before/after and current normal profile, source/binary/fixture and limitations; no invented speed promise. | **Complete locally:** `65964966` → `21498d24`, identical runner, `.8`/.NET 10.0.12, two warmups/ten completed lifecycle cycles per side, current profile two warmups/ten normal/one idle. [Datasets, hashes and actual medians/p95](00-status.md#measured-comparison-not-a-performance-acceptance-claim) include a roughly 5% median lifecycle increase. Native-only duration, pending worker metrics and budget remain global ISC-62. |
| D3 correctness | Implemented operation profiles checked for caller return, cancellation, resource ownership/drain, recovery and fault contracts; no open local correctness bug. | **Complete for bounded local scope:** initial pending-dispose probe exposed a real public-task completion bug, not waived; `21498d24` added shared shutdown waiting/public task markers and four tests (two red on old code), then the unchanged hardware probe passed. Independent Engineer audit found no remaining local blocker. The 27 operations/49 links and 16 synchronous entries do not establish exhaustive global coverage. Public `YubiOtp` Configure factories await `ReadStatus` before Configure; the initialization guard avoids input/output at that stage. |
| D4 verification | Relevant Core, PublicApi, Piv, Management, Fido2, YubiOtp and resilience regressions, changed-shipping-method complexity 10/20 and bounded independent review. | **Complete:** Core 1,515 passed/3 skipped; PublicApi 22, Piv 209, Management 86, Fido2 471, YubiOtp 180, resilience-fast 88 passed; three changed shipping methods met cyclomatic 10/cognitive 20. Independent Engineer close-out audit found D1–D4 evidenced with only stale-doc notes for D5; no cross-vendor final review is claimed. |
| D5 handoff | Existing status and master release evidence, explicit global gaps/deferrals, no stale ISA current claims; final docs QA after code work. | **Complete:** these three existing documents reconcile the final source, results, local closure and unchanged global checklist; final docs QA validated 72 active docs and 10 package readmes. No additional epic criterion checked. |

Fresh-host Input Monitoring permission/normal-user testing is **user-authorized
deferred**, not a local blocker. Windows/Linux native, extra NFC/other readers
and operator-present touch are also deferred, not declared passing. Global
ISC-4 inventory (205 outstanding, including transitive/app coverage), ISC-56
all-SDK diagnostics, ISC-53 all-public sync waits including external owners,
and ISC-62 full native-only metrics/budgets remain open at original scope. The
master now reads **19/72** solely because of the later reviewed ISC-50
consumer decision; local acceptance itself did not change its checklist.
Current shipping source pointers for audit: `src/Core/src/Transports/Hid/MacOS/`
(`MacOSHidInterface.cs`, typed/direct connections and listener),
`src/Core/src/Transports/SmartCard/`, `src/Core/src/Protocols/SmartCard/Apdu/`
and `src/Core/src/Devices/PcscConnectionSlot.cs`; use current paths rather than
historical renamed HID interface references.

## Public journey and cross-applet consistency

User-approved refinement, 2026-09-25: make transferable public expectations the
organizing goal of the next portable slice. The orchestrator owns comparison and
acceptance; Engineers own bounded tests and fixes. Begin this work while hosted
delivery is blocked. No push or repository-settings authorization is implied.

1. Compare representative public creation → operation → cancellation/failure →
   recovery → disposal journeys across Management, Piv, Fido2, WebAuthn, Oath,
   YubiOtp, OpenPgp, SecurityDomain and YubiHsm, plus the two raw-access tiers.
   Record naming/options, concurrency scope and overlap outcome, asynchronous
   invocation return, cancellation completion, prompt pairing, borrowed-memory and
   connection ownership, and same-session reuse versus reopen requirements.
2. Reconcile each row with existing evidence before adding checks. Record public
   entry point, promise, owner, source/test reference, evidence grade, justified
   difference or gap, and next action. Distinguish documentation mismatch,
   behavioral defect, proof gap and unrelated pre-existing drift. An unreviewed
   row stays pending. Extend the existing evidence map rather than create a report.
3. Implement named missing public-journey probes over production backend/protocol/
   migrated transport with a controlled native boundary. Use deterministic holds
   for pending completion and overlap assertions. Test prompt outcome, safe reuse
   or explicit refusal, and cleanup—not only cancellation-token forwarding.
4. Address demonstrated defects and misleading promises in bounded slices; retain
   transport-specific constraints. Review fit and rerun affected regressions. A
   material public-contract redesign still requires the established scope decision.
5. Define agreed measurement fields before instrumentation, collect the selected
   evidence, then reconcile durable docs and still-active demo claims. Missing
   native observations and deferred platforms remain pending at original scope.

Finish: every selected row has linked evidence or an explicit remaining gap,
justified differences are documented, and no known mismatch remains in the
selected journey. This advances criteria 37, 42, 49, 53–56 and 61–64 only where
their own evidence obligations are satisfied; no new checklist or acceptance
checkmark is created by planning. Local Mac/smart-card D1–D5 remains closed.

### First selected public journey

Read-only source comparison found composed presence tests in other applet routes,
but the existing Fido2 backend outcome tests replace the protocol, while Core's
`CrossLayerAcceptanceTests` omit the backend that owns terminal resolution. This
is a proof gap, not an established runtime defect. The first Engineer slice is a
public `FidoSession` over real backend, protocol and macOS connection with a
controlled input bridge, in Fido2 unit tests. Prove one device-waiting prompt,
one cancellation frame, drain before terminal completion, exactly one cancelled
resolution, same-session reuse and caller-owned connection lifetime. Include
overlap rejection while draining if reachable through the same fixture.

Reuse existing framing and response conventions; keep the fixture local. No
hardware or shipping refactor is required merely to add this proof. Escalate any
unexpected Core ownership defect before broadening the slice. Verification:
`dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~FidoSessionHidCrossLayerTests"`,
then the affected Fido2 suite and existing Core cross-layer case. This single route
cannot close all presence outcomes or cross-applet consistency.

Checkpoint, 2026-09-25: the new `FidoSessionHidCrossLayerTests` probe passes
through public `MakeCredentialAsync`, real backend/protocol/macOS connection and
a controlled bridge. It verifies overlap refusal during drain, caller-token
cancellation, exactly one cancelled resolution, same-session recovery without
reinitialization, and native release only on caller disposal. Targeted probe:
1 passed; full Fido2 suite: 472 passed; Core cross-layer case: 1 passed. These
are managed tests, not a real credential creation or new native/hardware proof.

**Defect investigation (subsequently resolved below):** same-session recovery before disposal was
proved; borrowed-connection reuse after session disposal was not. Source review
traced `FidoHidProtocol.DisposeAsync` through `RequestTerminalWake` to macOS
connection terminal state, contradicting `ApplicationSession.Connection`'s
documented reuse by another session. The test deliberately claims only native
release ownership. Reproduce sequential public sessions over the borrowed
connection and repair the ownership mismatch with pending-operation disposal
regressions before claiming that public journey consistent. Do not normalize
the mismatch by weakening the documented retained composition contract.

### Operator evidence update — 2026-09-27

The user attended touch/unplug/replug verification. With one selected key attached,
the previous direct-interface blockers cleared; touch success, physical pending-read
removal/reopen, matched listener removal on retry, smart-card cycles and normal/
borrowed-session reuse passed. The latest status checkpoint records the executable
hash and individual outcomes. This supersedes the operator/fixture limitations in
the historical matrix below for the tested host only. Listener close returned
`0x10000003`; ordered cleanup passed without claiming successful per-device close.
No new implementation or criterion acceptance follows merely from these runs.

### Final local acceptance matrix — 2026-09-25

This is the finite matrix for this increment, not an assertion that every applet
operation has independent native proof. Comparison covered Management, Piv, Fido2,
WebAuthn, Oath, YubiOtp, OpenPgp, SecurityDomain, YubiHsm and both raw tiers.

| Selected contract | Evidence / resolution | Remaining limit |
|---|---|---|
| Equivalent creation/options and asynchronous public shape | Existing `AppletSessionShapeTests`, `AsyncSurfaceConventionTests`, `PublicReturnContractTests`; PublicApi 24 pass | Signature checks do not prove invocation responsiveness; WebAuthn remains a higher-level facade and raw tiers have different responsibilities. |
| Borrowed idle session disposal and sequential composition | Conditional wake fix; `FidoHidProtocolTests` sync/async probes; `FidoSessionHidCrossLayerTests`; real selected-key two-session native-compiled probe | Active disposal may terminate the connection; no recoverable active-abort promise. |
| Overlap / concurrency | Public Fido2 drain probe, `RawSmartCardNativeBoundaryTests`, `ManagementSessionExchangeOverlapTests` | Guard is per logical exchange; multi-exchange operations are not atomic or queued. Caller sequencing is required. |
| Cancellation and recovery | Public Fido2 cancellation/presence/reuse; four public raw smart-card native-seam journeys and existing secure-chain tests | Smart-card cancellation after admission can return a successful result; FIDO drains cancellation; OTP requires successful reset. No blanket reuse promise. |
| Presence ownership | Full public Fido2 composition and selected-device automatic cancellation; existing applet-specific notification tests reused | Touch-success not newly performed; no claim that all outcomes on all applet transports ran. Management/SecurityDomain have no equivalent touch ceremony. |
| Smart-card native binding compatibility | Historical Core × old/new native, current hashed Core × new native: four context/error imports plus existing crypto vectors | Success connect/transmit/transaction on every pairing and other operating systems remain unproved. Current Core × old input-owner native is unsupported. |
| Public measurements | Ten current normal samples, idle observation and one cancellation/reuse interval; provenance and numbers in status | Native-only durations, owner counts and global budgets remain pending. Current profile is not the historical matched comparison. |
| Truthful durable claims and delivery | Ownership/cancellation/exchange granularity corrected; obsolete Management queuing test replaced; fresh docs validation | Demo branch ownership unresolved; hosted fixes not pushed; direct-interface reruns blocked by two-key association, Telegram touch request unanswered. |

The ownership defect is implemented and verified: idle disposal no longer sends
the terminal wake. Active disposal preserves the previous terminal behavior and
public docs explicitly describe reopening after uncertainty. Additional changes
were limited to discovered contract/test defects and native verifier cleanup.
Current inventory is 206/25 total/blocking-wait sites, with the explicit synchronous
FIDO disposal classified and backed by drain tests. No criterion scope or count
was changed. See the status checkpoint for commands/results and residual blockers.

Final local commands (run from this worktree):

```sh
dotnet toolchain.cs -- test --project Core
dotnet toolchain.cs -- test --project Fido2
dotnet toolchain.cs -- test --project PublicApi
dotnet toolchain.cs -- test --project Management.UnitTests
dotnet toolchain.cs -- test --project WebAuthn
dotnet toolchain.cs -- test --project SecurityDomain
dotnet toolchain.cs -- test --project YubiOtp
dotnet toolchain.cs -- resilience --fast
dotnet toolchain.cs complexity
dotnet toolchain.cs docs-qa
git diff --check
```

The standalone verifier README owns exact compatibility commands and hash choices.
The native-compiled route host ran `--probe`, `--active-cancel`, `--otp-info`,
`--smartcard`, `--listener-drain` and `--listener-late-drain`; device-specific modes
used `--serial 31683481`. `--expert-io` and `--expert-feature` returned explicit
blocked results. No touch or removal action was inferred from silence.

## Next bounded milestone: portable acceptance and build reproducibility

Under the existing orchestration authorization, prioritize fresh-checkout docs validation and reproducible
branch delivery before portable acceptance coverage, without another unbounded
Mac refactor. Build [36130687541](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/36130687541)
stopped at docs QA on seven links into ignored local measurement collections;
these planning docs now record code-formatted LOCAL-only paths and hashes instead.
Native AOT [36130687588](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/36130687588)
and Dependency Submission [36130685528](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/36130685528)
failed private-package restore with `NU1301`/401. Repository-owned workflow
authentication was committed locally at `5680901f` and linted, but is not pushed
or hosted-verified. The GitHub-managed dynamic
dependency workflow does not inherit those changes and remains unresolved.
No runtime defect is established. Fresh-checkout docs QA passed locally after
link repair committed in `70e35389`; hosted checks remain unverified for the local
checkpoints (recorded remote failures are at `3e928106`). Hosted
verification is required before closing delivery; independent acceptance-map
work may proceed. Prior clean `.8` package consumption is not a substitute for
green branch checks. A real
runtime regression found during CI investigation is evaluated on its own evidence,
not assumed from the failed checks.

The user authorized the behavior/compatibility/measurement/claims phase using
Engineers without further operator action. The public-journey refinement above
governs local sequencing; hosted delivery remains a separate closure obligation.
The finite deliverables are:

1. Delivery owner records reproducible branch restore/build and actual green
   Build, Native AOT and Dependency Submission checks at the relevant head.
   GitHub's managed dynamic dependency workflow does not inherit repository
   workflow credentials; its private-feed authentication is a distinct blocker.
2. Behavior: orchestrator and boundary owner reconcile one coherent gap-classified list
   of required operation/adapter and source paths from the existing 205
   outstanding Core sites, 27 required rows/49 named profile links and 16
   public-sync entries, with named runnable cross-layer profiles or explicit
   gaps. The new Core FIDO + macOS bridge-fake profile checks two request frames,
   single cancel, a distinct exchange-guard refusal while a 58-byte response
   continuation is withheld, and drain before same-channel reuse. Cancellation
   starts inside the device-waiting prompt callback: one request and zero Core
   resolutions because backend owns resolution. Three existing backend mapping
   tests substitute the protocol. Full-layer presence
   outcome, external paths and ISC-55 remain pending. Add bounded diagnostics
   with positive/negative controls rather than infer ISC-56 from three protocols.
3. Compatibility: parent accepted ISC-50 after independent review PASS with
   no findings following a bounded refit. Two tests compile a public custom
   `IHidConnection` and exercising real typed raw adapters constructed by
   reflection (not a public factory or discovery route). The `SetReport` XML describes caller ownership until return and
   third-party copy responsibility; it does not prove external runtime behavior.
   D15's alpha discovery-interface rename was an intentional source break, not
   a report-interface alias. Native export and big-number allocate/free smoke
   for 1.18.0/`.8` covers the canonical **36 package exports**; old Core
   imported 28 of them. Five input-owner exports are `.8` only, so old 1.18.0
   is unsupported for current input-owner Core. A separate locally compiled
   consumer runs the baseline `65964966` Core binary against 1.18.0 and `.8`
   in isolated processes and current Core against `.8`: all three pass P-256
   and AES-128 CMAC known-answer vectors with 17 Core native resolver calls
   each (8 EC, 4 BN, 5 CMAC). Core/native digests are checked and a bad hash
   fails before resolver load. Independent review: PASS WITH NOTES. The old
   Core binary is a local build, not an attested published artifact; the
   11 SCard imports, five input-owner callback signatures/lifetimes, failure
   paths, Windows/Linux and public consumer journeys remain untested by this
   runtime consumer. ISC-42 remains pending. Preserve the verifier's README limits.
4. Measurement, then claims: one read-only selected-key active-cancel run gives
   public-path ordering and timings, not native-only durations, distribution or
   approved budget. Three explicit-monotonic-tick timing-helper self-tests
   passed after that physical sample; hardware was not rerun after the helper
   refactor. Do not compare `.7` with `.8` as a matched cohort. Global ISC-62
   still needs its original native counts/scenarios.
   Active documentation corrections distinguish raw tiers and Core/backend
   presence ownership; inspect still-active demo/v2-slide claims separately
   before ISC-63 closure. No demo branch is merged by inspection.

The bounded portable probes, consumer fixture and timing verifier are committed
at `5fd0712f`; ISC-50 alone is parent accepted after independent review PASS.
`SetReport` XML is the only Core source change in that commit, not a change to
the selected-key runtime implementation at `21498d24`. Other criteria remain pending; the
delivery exit is still blocked by remote checks and automatic submission
authentication. Exit requires recorded deliverables and an honest gap list,
**not** a checkmark increase from partial probes. Reported precommit
results: full Core 1,516 passed/3 skipped; PublicApi 24, Fido2 471 and
YubiOtp 180 passed. The targeted cross-layer test passed one after its
guard/prompt assertion refit, and the final resilience-fast rerun passed 88;
an earlier aggregate-command timeout was not a test failure. Method complexity 10/20 remains manual-review pending (observed
maximum cyclomatic complexity nine), with zero shipping-tool or test/harness
coverage claimed. Fresh-host permissions, Windows/Linux native
and hardware, external implementations and unavailable global native metrics
remain explicit blockers/deferred evidence for original production acceptance.
The original S5-after-platform-feedback and S6-after-S5 dependencies still
govern global closure. Remote pushes and repository settings changes require
their own authorization. Preserve the closed local milestone unless a demonstrated
runtime regression justifies reopening it; no operator action is part of this scope.

## Last bounded increment and remaining global work

The prior E1 listener callback-drain package finished: actual vendor-filtered
matching, selected removal and late matching-callback drain on pinned `.8`
supported ISC-33 at the bounded teardown grade. The orchestrator accepted it;
do not re-dispatch E1 or the inconclusive no-open removal experiment. Its
verification-only callback decorator did not add production hooks. Manager
close on physical removal reported a dead-port error, not positive per-device
close; run-loop exit, callback exit, unschedule and manager release order were
observed. Input Monitoring risk on other hosts remains an orchestrator-owned
product follow-up, not a reason to rescind the observed teardown evidence.

The last bounded **unattended, read-only Mac plus portable contracts** increment
is complete at implementation checkpoints `8d13fd1e` (logging and FIDO response
refactor), `8ebcd026` (synchronous drain, scan, registry and docs), `c417621b`
and final public pending-read completion fix `21498d24`; this does not complete
the wider cross-platform epic or imply a new native package. The Core-only
source inventory classifies 205 outstanding sites:
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
The [finite list](00-status.md#separate-global-epic-lanes-and-decisions-not-local-mac-blockers)
records wider global work, not a prerequisite list for the local finish decision.
ISC-4/37/53/54/56/62 remain **open at full scope**.
Parent auditors handle disjoint code/test gaps; no engineer may infer a missing
test passes from this plan. Windows/Linux native and other hardware/operator
routes are deferred by user direction, not marked inapplicable. One attached
key is sufficient for a limited read-only observation but not universal closure.

| Work | Owner / finite outcome | Evidence and stop condition |
|---|---|---|
| Classification/registry | Orchestrator resolves remaining required operation/adapter rows and outstanding source sites with engineers' disjoint evidence; direct raw Mac rows are already registered. | Missing applicable rows stay pending ISC-4; runnable profiles must have nonzero tests. Preserve external-implementation and Windows/Linux gaps. |
| Public waits and documentation | Boundary/doc owner checks full public reachability, retained sync compatibility paths and cancellation/borrowed-memory/exception contracts. Built-in PC/SC withheld-native sync tests use a `LongRunning` caller-returned event, disposal snapshots and a finite 200 ms hold observation, not an all-races guarantee. External default async begin calls sync begin before task return and may block. | Link a named drain/fault test to each verified path; record uncovered paths explicitly. No Mac-only ISC-53/54 checkmark. |
| Diagnostics | Boundary owner inventories affected Core/applet logs and tests secret/response sentinels including externally supplied exceptions. Twelve diagnostics tests include real FIDO callback/failed cancel, OTP failed reset, PC/SC connection and protocol, and formatted/structured/exception positive controls. FIDO/OTP failure logs use exception type `FullName` only, excluding opaque external messages while preserving caller exceptions; PC/SC explicit structured command headers/length replace raw-object logs and public SELECT application-ID hex remains as requested. | First real sentinel exposed an opaque exception payload and was fixed; three protocols do not establish whole-Core/applet/external no-leak proof. ISC-56 stays pending. Never log credentials to create a probe. |
| Measurement | Local D2 binds matched same-package/runtime before/after and current normal-profile; global ISC-62 still needs native-only/pending metrics and approved budget. | Final-source matched data recorded in status; incomparable historical `.3` and pre-fix `c417621b` cohorts remain history. |
| Route verification | Selected-key read-only FIDO/OTP, direct IO/feature, listener and smart-card begin/read/reopen plus existing managed/native tests. | Final-source eight-mode `.8` Native AOT route run passed on serial 31683481 (firmware 5.7.4); earlier sharing and pre-fix public completion failure are resolved for this fixture. D1 accepted locally, not universally. |

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
ISC-20 and ISC-37 remain open at their full scopes. At the earlier
`8d13fd1e`/`8ebcd026` checkpoint Core 1,511 passed/3 skipped, PublicApi 22,
Fido2 471, YubiOtp 180 and resilience-fast 88 passed before those commits;
these are not final-source counts. Focused adapter registry 17, public-sync registry five,
PC/SC lifetime 30 and HID scan boundary three passed; targeted HID scan three
passed again after cleanup was corrected. Earlier targeted diagnostics 12,
PC/SC 23 and FIDO 40 are source-specific history. `FidoReceiveResponse` was split into
terminal, validation and copy helpers with the same control/error precedence
and buffer zeroing; 12 changed shipping methods were within cyclomatic
10/cognitive 20 at that earlier checkpoint, not whole-project or untracked-test proof.
Earlier docs QA and architecture validation passed. Earlier independent review: PASS WITH NOTES after one
bounded test false-negative fix; cleanup now releases and awaits the held scan
before disposing gates, without an established production defect. The public
SELECT application-ID hex log remains.
Earlier Core 1,508/3, 1,492/3 and secure filter 159/2 are historical
results, not current runs. The final-source Core 1,515/3 skipped and six other
module/resilience counts are in the local D4 row and [master evidence](../../../2026-09-21-yubikit-async-boundaries-ISA.md#local-close-out-evidence-2026-09-25-d1d5-only).
Historically, collected in the working tree before the implementation commits, the published
`.8` Native AOT `--probe` and `--otp-info` discovered one PID 0407
key using fallback identity and selected serial 31683481 (firmware 5.7.4).
Five production FIDO scenarios (including pending-read disposal/reopen and
pre-dispatch cancellation) and three typed OTP info cycles passed without touch
or replug. The first initialization was 229 ms, then 20 ms; do not treat these
as a comparable performance baseline or budget. The earlier two-attached-key
observation belongs to a different probe. The earlier broad smart-card integration
and isolated `--smartcard` failed sharing before open; after replug, fresh-source
`c417621b` PC/SC passed three read-only cycles on the same serial/firmware.
The isolated read-only scenario has a 20-second watchdog. Final-source
`21498d24` ran the smart-card route again successfully, along with seven other
modes; neither that result nor the original contention asks for more operator
work.

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
  ISC-50 accepts this retained public member set and the borrowed-through-return
  `SetReport` contract, enforced by actual adapter consumer tests. Wholesale
  async evolution is future scope, not an open ISC-50 decision. Typed macOS
  FIDO/OTP use internal owned boundaries; external
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
