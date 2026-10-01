# Status: YubiKit async boundaries

## Current checkpoint — 2026-09-27

### Authorized delivery — 2026-09-27

The user authorized committing and pushing this local increment and submitting
the native changes for review. Native pull request:
[677](https://github.com/Yubico/Yubico.NET.SDK/pull/677), branch
`feature/macos-hid-input-dev`, head `64d056ce`, targeting `develop`.
Independent review passed after wiring the assembled-package export checker
before upload; fresh native harness **23/23**, Python checker tests **5/5**, and
inspection of all **14** shared/static assets in `.8` passed locally.

The internal development package already exists and remains pinned in
`Directory.Packages.props`: **1.18.1-async.8**, producer `71a23cd0`, successful
[publish workflow](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/35955737172).
Subsequent native commits change tests, comments and workflow validation, not
the shipping native implementation. No replacement package is required. Hosted
execution of the new package-check step is not established by local validation.
The managed commit/push supersedes the historical no-authorization/uncommitted
notes below; hosted results must still be evaluated at the pushed head.

### Operator-assisted local verification — 2026-09-27

With only selected serial 31683481 attached, firmware 5.7.4, the existing published
native-compiled verifier passed the following modes. No source changes or rebuild
were made for this run; executable SHA-256:
`872735b23bebec8efb1df0b1b2a9cec7d7f11eea6100e8b9a097821859975d27`.

| Mode | Observed evidence |
|---|---|
| `--touch` | Exactly one Completed presence resolution; same-session GetInfo and reopen GetInfo passed. |
| `--expert-io` | Expected timeout at 6002 ms, followed by same-connection and reopened GetInfo. |
| `--expert-feature` | Three direct feature reads and three Management info reads passed. |
| `--removal` | Pending native input terminated after physical unplug, absence observed, native disposal completed, replug produced a fresh generation and successful GetInfo. |
| `--listener-remove` | Retry observed matching Removed callback for entry 4305440554; stop=1, callback exit=2, close returned=3, unscheduled=4, manager released=5, stop returned=6. Close result `0x10000003` is retained as an error result, not successful per-device close. Initial attempt timed out before the operator was ready; it is not counted as passing. |
| `--smartcard` | Three open/transaction/read/end/close cycles, two reopens, firmware 5.7.4. |
| `--probe` | Three normal cycles, two public sessions sequentially sharing one borrowed connection, pending-read disposal/reopen, and pre-admission cancellation/reopen all passed. |

Each invocation used `--serial 31683481` and the executable at
`verification/MacOSHidRouteVerification/bin/Release/net10.0/osx-arm64/publish/MacOSHidRouteVerification`.
The `--probe` summary's “touch/removal pending” describes that invocation only;
the separate successful modes above supply those observations. The prior two-key
direct-interface blocker and unanswered touch request are superseded for this
selected-host run. No credential creation/configuration writes were performed.

Full epic acceptance remains **19/72**: these are additional local evidence, not
complete all-applet outcome, cross-platform or native-metric acceptance. Hosted
delivery, full native consumer/lifecycle matrices, fresh-host permissions,
global instrumentation/budgets and active demo ownership remain open. Latest
implementation is still uncommitted; original local D1–D5 remains closed.

### Final local public-contract increment

The bounded local implementation and unattended verification are recorded in the
working tree. No commit or push was
authorized for this increment. Original D1–D5 remains closed; full epic acceptance
remains **19/72**. The following evidence supersedes the earlier first-probe/next-fix
notes below, not the deferred global criteria.

- Fixed idle FIDO protocol disposal: close admission and inspect the drain before
  requesting a terminal wake. Public sessions can now sequentially reuse the same
  borrowed macOS connection. Active disposal retains terminal-wake protection;
  reopen after uncertain recovery. Both disposal forms have managed wake/drain tests.
- Public Fido2 composition proves cancellation, exactly-once terminal prompt,
  overlap refusal while draining, same-session recovery and idle two-session reuse.
  Four new public raw smart-card journeys prove pre-admission cancellation/no dispatch,
  admitted-result completion despite cancellation, drain before sequential reuse,
  exact response-continuation bytes and refusal without replay after failure.
- Cross-applet review found the meaningful concurrency limit: guards reject
  overlapping **logical exchanges**, not whole multi-exchange public operations.
  Callers sequence those operations. Corrected the durable contract and obsolete
  Management hardware queuing expectation; a deterministic public Management
  two-page probe verifies refusal without a second dispatch. Hardware theories
  were compiled, not run against arbitrary attached keys.
- Native compatibility: the three supported old/current Core × 1.18.0/`.8`
  pairings passed crypto plus four actual smart-card imports (context establish,
  invalid-context cancel, nonexistent-reader connect, context release). Observed
  error codes were `0x80100003` and `0x80100009`. Wrong hashes, missing export and
  release-failure controls reject as intended. Binary hashes and limits are in
  `verification/NativeCompatibilityVerification/README.md`; this is not the full
  native matrix or every smart-card operation.
- Fresh native-compiled selected-key runs passed normal FIDO, automatic cancellation,
  OTP info, three smart-card transaction/read/end/close cycles, and both listener
  drain modes. The extended normal probe also passed **two public sessions on one
  borrowed physical connection**. Firmware was 5.7.4. Direct-interface I/O and
  feature reruns were **blocked**, because two attached keys could not be uniquely
  associated by those verification modes; no interface was guessed.
- Telegram availability request timed out; no touch/removal test was performed.
  Hosted checks still target `3e928106` and fail; no new push/settings authorization
  exists. Active demo ownership and global native metrics remain open.

Current boundary inventory: **206 classified/outstanding sites, 25 blocking waits**
(formerly 205/24). The explicit synchronous FIDO disposal drain is a reviewed
`sync-boundary` with named tests; it adds no asynchronous blocking entry point.
Latest suites: **Core 1,524 passed/3 skipped; Fido2 474; PublicApi 24; Management 87;
WebAuthn 221; SecurityDomain 45; YubiOtp 180; resilience 88**. The first full Core
run caught the new inventory site; it passed after classification, not suppression.
Shipping complexity checked both changed methods; tests/harness complexity was
reviewed manually. Review found and corrected test-race, hold-cleanup and verifier
handle-cleanup issues.

**Current measurements, not a matched before/after claim:** automatic cancellation
to resolution **99.967 ms**, terminal catch **100.037 ms**, verified reuse
**116.374 ms** (one run). Profile: two warmups, ten completed normal samples and one
completed idle sample. Medians: caller return **0.150 ms**, operation **19.902 ms**,
disposal **1.977 ms**, process allocation **37,416 bytes**. One-second idle observed
**1.918 ms** process CPU, **0 bytes** allocated, thread delta **0**. Native-only
duration and owner worker/pending counts are unavailable, not zero or waived.
LOCAL-only dataset: `artifacts/measurements/current-profile-20260925T192637011Z.json`,
SHA-256 `4ec5dce4bb4456529d673f07909e0244ba2e924a4b8a71ac75bcea0936311235`.
It records Core hash `38f43b1c4dc7e7efff69381554f190159b0c56f2ffa80ad28d1b6ead93e81417`,
HEAD `70e35389` plus working-tree/untracked hashes; source-to-binary identity is
not independently attested. An initial stale-runner mismatch was rejected before
hardware access; publishing the runner with the selected Core restored hash agreement.

Next work is limited to explicit blockers or new findings: authorized hosted delivery,
operator/fixture-dependent probes, active demo ownership, and original cross-platform/
full-metric acceptance. No further general Mac transport refactor is scheduled.

### Current lane board and epic slices

| Slice / lane | Current state / next boundary |
|---|---|
| S0 baseline / S0b shared foundation | Baseline and bounded foundation evidence exists; source classification and universal contracts are not closed. |
| S1 Windows FIDO | Windows native/hardware proof **user-deferred**; not accepted. |
| S2 macOS HID | **Local Mac milestone closed** at shipping `21498d24`: eight selected-key `.8` Native AOT route modes, including public pending-read completion and matching listener drain. Fresh-host Input Monitoring/normal-user permissions **user-deferred**; no permissionless-host claim or global S2 closure. |
| S3 Linux HID | Linux native/hardware proof **user-deferred**; not accepted. |
| S4 portable PC/SC lifecycle/discovery | **Local smart-card milestone closed**: after replug, selected-key async open/begin/read/end/dispose/reopen passed three cycles. Discovery/global PC/SC matrix still open. |
| S5 remaining boundaries | Portable diagnostics, discovery and public synchronous waits have bounded evidence (`8d13fd1e`/`8ebcd026`); global inventory, diagnostics and public-contract closure remain open. S5 still depends on deferred platform feedback. |
| S6 delivery and cross-platform evidence | `.8` published; clean packaged Native AOT consumers and fresh private-feed restore verified historically. **Separate delivery blocker at remote `3e928106`:** Build [36130687541](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/36130687541) stopped at seven local-only measurement links; the documentation fix in this checkpoint passes locally in a fresh checkout. Native AOT [36130687588](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/36130687588) failed private-package restore with `NU1301`/401; repository-owned workflow credential/permission repair is committed locally at `5680901f`, not hosted-verified or pushed. GitHub-managed Dependency Submission [36130685528](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/36130685528) failed the same restore and does not inherit this patch. Its authentication remains unresolved. No local runtime milestone is reopened by these delivery failures. |
| X exploration (after production milestone) | WinRT SmartCard/CryptoTokenKit exploration unstarted; no production acceptance claim. |

**Approved next phase, in progress:** prioritize delivery verification while
independently pursuing public-journey and cross-applet consistency. Compare
equivalent applet contracts first, then implement bounded cross-layer behavior,
consumer/native compatibility, measurement and active-claim work. Engineers own probes; no new
operator action or approval is requested. Fresh-checkout docs QA now passes
locally; Build, Native AOT and automatic Dependency Submission remain failed
at the old remote head, not verified green for the local implementation checkpoints. Follow the [finish
rules](03-program-design.md#next-bounded-milestone-portable-acceptance-and-build-reproducibility).
Reconcile the existing **205 outstanding classified Core sites**, **27 operation
rows/49 profile links** and **16 public-sync entries** into one gap-classified
map, without calling classification universal proof. ISC-1–4/6–8, ISC-53–56
and ISC-61–64 retain their full scope. Windows/Linux native, fresh-host
permissions, external implementations and unavailable global metrics remain
blocked/deferred. Keep local D1–D5 closed absent a demonstrated regression;
remote pushes and repository settings changes require separate authorization.

**New work item — public journey and cross-applet consistency (2026-09-25):**
the user requires transferable expectations across applets for creation, concurrency,
asynchronous return, cancellation, recovery, presence and disposal, with explicit
transport/domain differences. Recorded in the product commitment, architectural
comparison rule and [executable slice](03-program-design.md#public-journey-and-cross-applet-consistency).
Initial source comparison selected the missing public Fido2/macOS cancellation,
terminal prompt resolution and same-session recovery composition as the first
Engineer probe. Other comparison dimensions and applet/raw-tier rows remain
pending; a presence-focused scan is not a completed consistency review. Planning
adds no acceptance: **19/72 remains checked**. Hosted delivery remains separately blocked.

The first new public-journey probe now passes: one cancellation frame, overlap
refusal during drain, caller-token cancellation, one cancelled terminal prompt,
and same-session recovery on the existing channel. Fido2 full suite **472 passed**;
targeted new probe **1 passed** and Core cross-layer case **1 passed**. Managed
bridge evidence only. Independent source review found a separate ownership
mismatch: session disposal terminally wakes the borrowed macOS connection, while
the public base documentation promises reuse by another session. Next: reproduce
and fix that bounded ownership defect; native handle retention alone is not
connection reusability. Existing selected-key local acceptance is not reclassified
as universal borrowed-session proof. See program design for scope and evidence.

Committed workflow repair `5680901f` supplies `NuGetPackageSourceCredentials_Yubico_GH` using the
workflow token and package-read permission, excluding fork pull requests. Workflow
linting and the local Core build passed. Hosted package access is not proven by that
cached local build. GitHub's automatic dependency submission is a separate dynamic
workflow, so changing repository workflow environment variables does not fix it;
a supported credential mechanism or separately configured submission workflow and
managed-workflow setting change must be settled before calling all checks green.

**Portable checkpoint committed at `5fd0712f`; active-document contract corrections
are committed at `62388695`. The orchestrator accepted only ISC-50 after
independent review PASS with no findings (not global closure):** One managed `CrossLayerAcceptanceTests` case uses actual
Core FIDO protocol and macOS input connection with a controlled native bridge
fake. The 58-byte request takes two frames before one cancel; the exchange
guard (not a native-overlap error) refuses a second logical exchange while a
58-byte response continuation is withheld, then permits same-channel reuse
after draining. The prompt requests cancellation after `DeviceWaiting`: one
request, zero Core resolutions. The backend owns resolution; three existing
backend tests substitute the protocol, so full-layer presence outcome remains
unproved (ISC-55 open). Two PublicApi tests compile an external
`IHidConnection` and construct real internal FIDO/OTP raw adapters by
reflection only at construction; typed-interface sends verify zeroed SDK copy
and unchanged caller input, plus receive and disposal. No public-factory or
discovery end-to-end result follows. `SetReport` XML now says borrowed caller
data is valid through return and third-party implementations retaining it
must copy; no runtime test of every override. The alpha discovery-interface
rename under D15 was an intentional source break, not a compatibility alias
for `IHidConnection`. The orchestrator accepted ISC-50 for the reviewed retain
decision and these consumer tests, not for public-factory end-to-end access or
wholesale async interface evolution.

The native consumer verifier requires the canonical 36-export **package** set
in both 1.18.0 and `.8`, plus native big-number allocate/free smoke. Pre-owner
Core imported 28 of these, **not 36**. Current input-owner's five exports are
present only in `.8`; old 1.18.0 is **unsupported** for that consumer. There
is now a separate, locally compiled runtime consumer of the baseline
`65964966` Core binary (SHA-256
`928a0b6fcc75e6940d385621065aebed716957ef7d5988d27c5e251e6e2550c6`):
isolated processes against 1.18.0 and `.8` both passed P-256 and AES-128
CMAC known-answer vectors. Current Core plus `.8` passed too; each pairing
recorded 17 Core native resolver calls (8 EC, 4 BN, 5 CMAC), with known
native hashes checked. A wrong hash fails before resolver load. Independent
review: PASS WITH NOTES. This baseline is a **local build**, not an attested
published managed artifact. It exercises internal crypto success paths, not
the 11 SCard imports, five new input callback signatures/lifetimes, failure
paths, Windows/Linux or a public consumer journey; ISC-42 remains open. The
verifier README states the matrix limits. Registry 27 operations/49 links, 205 classified outstanding Core
sites and 16 public-sync entries are unchanged. Reported precommit
results: Core **1,516 passed/3 skipped**, PublicApi **24 passed**, Fido2
**471 passed**, YubiOtp **180 passed**; the final resilience-fast rerun passed
**88** (an earlier aggregate-command timeout was not a test failure). The
targeted cross-layer case passed **1** after the guard/prompt test-only refit.
Production runtime
behavior remains at `21498d24`; `5fd0712f` adds `SetReport` XML without changing
its members or runtime implementation. The bounded refinement and final independent review
passed; changed verification methods were manually assessed against the 10/20
limits (maximum reported cyclomatic complexity nine). The shipping-only command
does not cover these test/harness methods. **19/72 checked, 53 pending**;
ISC-16/17, ISC-42, ISC-55, ISC-62 and ISC-63 remain open at original scope.

The `--active-cancel` read-only hardware observation on selected serial
31683481 and `.8` recorded resolved=1, same-session GetInfo=1, reopen
GetInfo=1; cancel-to-resolution 91.274 ms, cancel-to-terminal-catch 91.399 ms,
cancel-to-reuse 111.377 ms, resolution-to-catch 0.126 ms. The subsequent
timing-helper refactor has three explicit-monotonic-tick self-test cases but
no repeated physical sample. This single public route is not native-only
duration, a fixed budget, a cross-package comparison or a full scenario
matrix; ISC-62 and ISC-55 remain open. Concurrent active-doc corrections are
not ISC-63 closure: demo/v2-slide was inspected read-only, not integrated;
still-active deck claims and ownership need resolution against the completed
boundary/evidence report, not a false merged-demo claim.

**Measurement provenance is LOCAL-only:** `artifacts/measurements/` is ignored;
the raw JSON collection files (including those in the separate baseline worktree)
are not in the repository. Code-formatted paths and recorded SHA-256 hashes below
identify historical local collections, not accessible links. A fresh checkout
cannot verify raw contents against those hashes without separate artifact transfer;
passing docs QA is not raw measurement reproducibility.

User-requested Mac and smart-card **local** close-out after replug is **CLOSED:
D1–D5 met** at committed shipping `21498d241f74d64055b10e5b0fb36a47d68acc3b`.
The predeclared [five local finish checks](03-program-design.md#user-approved-local-mac-and-smart-card-finish-decision)
cover selected-key routes, matched visible measurements, implemented-profile
correctness, relevant regression/review, and durable evidence/docs validation.
Fresh-host Input Monitoring was explicitly deferred by the user, not treated as
a failure or waiver of the original epic. No further local implementation or
operator action is requested; global criteria retain their original scope.

Implementation checkpoints: `8d13fd1e` (logging/FIDO), `8ebcd026`
(synchronous drain/scan/registry), `c417621b` and final public pending-read
completion fix `21498d24`. Measurement driver `d23cca94` pins the compared shipping
revisions and `.8` package; its commit does not change shipping code. Closure
documentation commit `3e928106` changes no shipping runtime. This is the current dispatch view; the
[product](01-product.md), [architecture](02-architecture.md) and
[finish rules](03-program-design.md) keep their separate responsibilities.

**19/72 checked, 53 pending.** The [master checklist](../../../2026-09-21-yubikit-async-boundaries-ISA.md#criteria)
owns acceptance and the [verification rows](../../../2026-09-21-yubikit-async-boundaries-ISA.md#verification)
own detailed proof and limits. ISC-5, ISC-19, ISC-31–33, ISC-38–39, ISC-41,
ISC-43–52 and ISC-60 are checked. ISC-50 alone was accepted in the portable
increment; all other unchecked criteria retain their original scope. This is bounded macOS and
portable managed evidence, **not** production epic closure. The orchestrator
owns acceptance; the Route/Native Engineers own their probes and implementation.

The current pin is private-published NativeShims `1.18.1-async.8`, produced by
native `71a23cd0269c968c1d9420eddb2e2fec71e0cc29` in
[workflow 35955737172](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/35955737172),
package SHA-256 `c85c56f7a41c6999b48b1a5fdcd82c56fbfb3e5ee6ff18ccc64a41144f760403`.
Seven clean packaged Native AOT consumers and fresh private-feed restore passed;
that is packaging, not Windows/Linux driver or hardware execution. The separate
`.9` release and verification-only native commits `541cfb09`/`760d0416`
did not produce this pin. Developers still require private-feed access.

On macOS 15.7.7 arm64 / .NET 10.0.12, selected serial 31683481 (firmware
5.7.4), published `.8` Native AOT, **all eight** modes passed at final shipping
source: `--probe` five FIDO normal/pending-dispose/pre-dispatch scenarios;
`--active-cancel` actual device-waiting automatic cancellation without operator
interaction; `--otp-info` three queries; `--expert-io` six-second timeout,
same-connection GetInfo and reopen; `--expert-feature` three read-only queries;
`--smartcard` three async open/begin/read/end/dispose/reopen cycles;
`--listener-drain` actual matching callback; `--listener-late-drain` actual
matching callback timeout/retention then restart. Probe source bytes did not
change on commit. The initial `c417621b` pending-dispose probe exposed Dispose
returning before the *public* read task terminated through the async cancellation-
registration Dispose wrapper. `21498d24` fixes this with shared shutdown waiting
and public task markers; four new tests include two red against the old code.
The probe assertion was not weakened. Historical pre-replug
`SCARD_E_SHARING_VIOLATION` occurred before open; replug resolved it for this
selected fixture, not every reader. External smart-card implementations retain
a synchronous transaction fallback; direct report compatibility paths remain
synchronous. No physical removal was requested for final verification.

ISC-33 is accepted **only** for observed macOS listener callback-context/device-
storage teardown. Real matching, operator-assisted selected-key removal (HID
entry 4305321102, matching `Removed` hint, manager open result 0), and matching-
callback late-drain probes observed callback exit before manager close, explicit
unschedule, release and Stop return. Selected removal ordering: stop request 1,
callback exit 2, close return 3, unschedule 4, release 5, Stop 6. Close returned
`0x10000003` on the dead port, **not** successful per-device close; Apple's
[IOHIDManager.c](https://github.com/apple-oss-distributions/IOKitUser/blob/main/hid.subproj/IOHIDManager.c)
shows manager unscheduling/closed state precedes the per-device close error.
The late-drain probe retained the timed-out generation, refused restart until
callback exit/cleanup, then restarted. Cross-vendor review: PASS WITH NOTES.
An earlier no-open removal probe ran more than ten minutes beyond its 180-second
watchdog without callback: inconclusive, not a gate or proof that open is required.
Vendor filtering does not establish permissionless Input Monitoring behavior on
other hosts. Permission/normal-user experience is a separate orchestrator follow-up,
not an ISC-33 teardown blocker. No repeat physical removal/Stop experiment is
needed for this checkpoint.

Final-source Core **1,515 passed/3 skipped**, PublicApi 22, Fido2 471,
YubiOtp 180, Management 86, Piv 209 and resilience-fast 88 passed. Three
changed shipping methods met cyclomatic 10/cognitive 20. Independent Engineer
close-out audit found D1–D4 evidenced and only stale D5 docs notes, now
reconciled; this is not a cross-vendor final review. Earlier focused adapter registry
**17**, public-sync registry **5**, PC/SC lifetime **30** and HID scan boundary
**3** passed; after the held-scan test cleanup fix, targeted HID scan boundary
**3** passed again. Earlier post-restoration targeted diagnostics 12, PC/SC 23 and
FIDO 40 are not replacements for the latest full Core result.
The first real sentinel failed on an opaque exception payload before the logging
fix. Twelve diagnostics tests include positive controls for formatted, structured
and exception text. Earlier independent review: PASS WITH NOTES after a bounded
test false-negative fix; held-scan cleanup now releases the delegate, awaits
the actual scan, then disposes gates. No production defect was established.
Public SELECT application-ID hex logging remains. Twelve then-changed shipping
methods met cyclomatic 10/cognitive 20 at that earlier checkpoint, not a
whole-project proof. Historical listener
checkpoint counts are in master Verification. These results do not establish
cross-platform native or all-scenario hardware execution. The current Core `BoundaryInventory` classifies
205 **outstanding** sites: 134 native imports, 3 native exports, 24 blocking
waits, 15 scheduling sites, 21 pre-task-return dispatch gaps, 6 callback
registrations, 0 delegate conversions and 2 unmanaged callback addresses.
The current test-only adapter registry requires **27 operations and 49 named
Fact/Theory profile links**, not 49 distinct tests: typed macOS FIDO 4, OTP 4,
portable PC/SC 5, macOS direct input 4, direct feature 4, listener 2 and
portable PC/SC synchronous begin/end/dispose plus external default begin 4.
Separately, the public synchronous reachability map pins **16 entries** to
public type/method, internal owner/method and test or explicit gap: direct
input/feature 8, smart-card 4, listener 2, manager shutdown 1, HID scan 1.
Five negative registry cases reject missing/duplicate/wrong mapping and bare or
stale evidence. The HID scan row now links three controlled tests using a narrow
internal enumeration delegate, not actual native platform enumeration: pre-cancel
submits nothing; a held call returns a pending task without caller blocking and
late cancellation waits for resource completion; a fault preserves the original
exception without replay. Manager Shutdown remains an explicit bounded-monitor-
stop gap, not proof of global native drain. These links do not execute tests or establish native drain or
full public/platform coverage. The earlier 23/45 is a historical checkpoint.
The 205-site source inventory had two freeform evidence line references updated
for shifted HID scan and manager Shutdown lines; site counts and identities did
not change.
Earlier, at the pre-expansion 13-row source checkpoint, targeted
inventory runs passed 28 tests (13 scanner, three registry, six responsiveness,
six diagnostics); the matching-probe checkpoint subsequently passed 32.
Neither tally means all sites or routes are verified.

### Measured comparison, not a performance acceptance claim

Local D2 compares baseline `65964966cc21f431e16793264c899c30a5b957a3`
to final shipping `21498d241f74d64055b10e5b0fb36a47d68acc3b` on the
same macOS 15.7.7 arm64 / .NET 10.0.12 host and selected serial 31683481,
with identical runner sources and NativeShims `1.18.1-async.8` (package SHA-256
`c85c56f7a41c6999b48b1a5fdcd82c56fbfb3e5ee6ff18ccc64a41144f760403`,
deployed native SHA-256 `65499e77151d483d8e04bd793c2d520b4f344478ffcacff91bff8e771ae9d8af`).
LOCAL-only before: `/Users/Dennis.Dyall/Code/y/worktrees/yubikit-async-baseline-65964966-async3/artifacts/measurements/comparison-v2-before-20260925T111722670Z.json`
(SHA-256 `75150b30488091623487b55b8c7d961c1a100fb9a211dd2b6388c230b15ac440`,
Core binary `928a0b6fcc75e6940d385621065aebed716957ef7d5988d27c5e251e6e2550c6`);
LOCAL-only after: `artifacts/measurements/comparison-v2-after-20260925T111741620Z.json`
(SHA-256 `948146da4cb5d12453698a7fd9497939609d4c6402ba5c966cd868a43bbd5488`,
Core binary `bb88e2dea542c97a94c597c893f62f56862a8ab7dc43567aa3fc8c3f7b46e43c`).
Each used two fresh-child warmups and ten completed lifecycle samples.
Medians/p95 (ms, before → after): invocation 23.49/26.72 → 4.69/6.11;
task terminal 23.79/27.81 → 26.48/28.71; lifecycle 70.74/81.75 →
74.31/78.23. **Median lifecycle increased about 5%; not all metrics got faster.**
Baseline no-input failed with PlatformApiException at invocation; after returned
pending and was censored after ten seconds, not a native cleanup proof. The
separate final-source pending-dispose hardware assertion passed. The LOCAL-only
final current profile `artifacts/measurements/current-profile-20260925T111800158Z.json`
(SHA-256 `ddabbd997198dc1641474a78afc1bcab78a098036ee5141f0872e51500693b45`)
has two completed warmups, ten normal and one idle, zero failed/censored:
median caller return 4.95045 ms, operation 26.4551 ms, disposal 3.4911 ms,
process allocation 62,252 bytes; idle 1,002.2526 ms, CPU 1.963 ms, zero
allocated bytes and zero thread delta. This is descriptive local evidence, not
population inference or an approved budget. Native-only duration and pending
ordinary count remain unavailable; global ISC-62 is open.

Earlier `.8` profiles and the pre-fix `c417621b` comparison remain historical
snapshots, not the final-source D2 comparison above. No numerical budget or
global ISC-62 performance acceptance is approved.

## Separate global epic lanes and decisions (not local Mac blockers)

- **ISC-4/1–3/6–8:** independently enumerate remaining required adapter/operation
  rows and transitive wrappers; resolve all 205 outstanding classified Core sites
  and broader shim/applet/build-configuration coverage. Direct raw macOS IO/feature
  and listener rows are already linked in the 27-row registry. That partial registry
  and ISC-5 unknown-import gate are not universal proof.
- **ISC-53/56:** finish the [public synchronous wait map](../../architecture/raw-access-tiers.md#retained-synchronous-compatibility-paths)
  with verified owner, drain/quarantine, fault and late-completion contracts
  across **all** public-reachable waits: the 16 mapped entries are only a
  bounded start, not a zero-gap certificate. Direct open/GET/SET/dispose,
  transactions, discovery, monitors, protocol forwarding, applet initialization
  and external implementations. Independently compare the source inventory;
  map each verified path to an actual withheld-native test or record the gap.
  The `LongRunning` caller-returned event and native-held PC/SC disposal
  snapshots use a finite 200 ms observation: they show held calls at sampled
  points, not every race. The custom smart-card default `BeginTransactionAsync`
  calls synchronous begin before task return and may block; do not generalize
  the built-in ISC-51 async proof. FIDO callback/failed-cancel and OTP
  failed-reset logs now emit only `ExceptionType` (`FullName`) metadata while
  excluding opaque external messages and preserving original caller exceptions.
  PC/SC logs explicit structured command headers/length, not raw objects;
  public SELECT application-ID hex remains logged,
  without logging secret response payloads.
  Extend sentinel proof beyond these three protocols to Core/applet and external
  mappings, including command, response, PIN, key, credential and external
  exception text. Neither
  universal criterion is checked from the current named-path probes.
- **ISC-54 and exception docs:** review public XML/examples for borrowed-input
  lifetime, cancellation, synchronous fallback, fault and raw-reuse semantics
  against the implemented route and existing tests. Document source-verified
  behavior separately from native-runtime and selected-device evidence;
  record gaps, not invented universal guarantees. Keep ISC-54 open until the
  full public-operation scope is reconciled.
- **Global ISC-62:** local matched before/after and current normal-profile data
  are recorded above. Local closure does not require a new arbitrary speed
  promise. Global ISC-62 still requires comparable frozen baseline/final data for the accepted
  source/binary/package and selected fixture, separate caller-return, native
  duration, recovery, active/pending workers, allocation and idle metrics,
  and approved budgets. Missing instrumentation remains unavailable; do not
  infer native metrics from managed elapsed time or compare different cohorts.
- **Platform and hardware lanes:** Windows overlapped HID/OTP access, Linux
  readiness/write isolation, Windows/Linux PC/SC lifecycle, cross-platform
  Native AOT execution and required hardware/reader matrix remain deferred pending user
  direction and suitable hosts (ISC-29–30/34–37/40/57–59). Complete protocol,
  prompting, public-contract, regression and cross-layer closure rows before P;
  exploration ISC-65–72 remains separate. Physical OTP mid-frame failure,
  borrowed-connection cross-session reuse and combined unplug/dispose uncertainty
  remain unproved; retain conservative quarantine. No hardware mutation is implied.
- **Authorized deferrals:** fresh-host Input Monitoring permission/normal-user
  experience is not a current local blocker. Windows/Linux native execution,
  extra NFC/other-reader fixtures and operator-present touch remain deferred,
  not passed or waived from global scope; do not request new operator work.

The reviewed PC/SC protocol change marks plain interrupted command/response
chains recovery-required. Once protected state has advanced, first transport
failure, response-MAC failure, interrupted protected continuation and rejected
intermediate fragment likewise refuse reuse on the same protocol, preserve the
original exception and do not replay. Wrapped plain SELECT continuation is
deduplicated through the secure hook. An authenticated terminal application
error remains reusable. The independent-card command-MAC test holds a protected
continuation through caller cancellation, completes it, then verifies the next
protected command's MAC; parent accepted **ISC-19 at this one managed profile**,
not as physical-cancellation proof. Three plain cases and the plain SELECT case
were red then green; initial protected fixture failures were not meaningful red
evidence. A new protocol over the same borrowed raw connection can bypass the
per-protocol latch: callers must reopen after uncertain recovery. ISC-20 remains
open; ISC-37 remains open because the global PC/SC monitor/discovery and custom
fallback lifecycle matrix is incomplete, despite five registered connection rows.

## Concise history and provenance

- Revised single-key product and architecture gates were approved 2026-09-22;
  incremental slices replaced whole-epic upfront specifications. The first
  smart-card lifetime slice is `db7a1bf6`; macOS direct IO `4f361504`, feature
  `db378ffc`, listener generation `69946394`, listener proof probes `5074ccb9`,
  OTP recovery/public contracts `efde3ef0` and registry `add0dc73` are separate
  checkpoints. The [first-slice design](03-program-design.md) preserves the
  ownership/finish rules; original pseudocode is historical, not a current
  dispatch instruction. The unrelated solution-file edit is outside this work.
- The `.3` FIDO touch/cancel proof used serial 31683481 on a second coordinated
  attempt; `.3`/`.4`/`.5` physical removal initially failed at close with BadArg.
  Native `.6` actual removal callback plus cancel acknowledgment/drain permitted
  guarded dead-device BadArg handling, then unplug → terminal → dispose → replug
  → getInfo passed (`/var/folders/gn/mh64zz5969j89_f5dffnvnb80000kt/T/opencode/yubikit-removal-async6.log`).
  Do not re-label this input-owner removal test as a `.8` run; the later `.8`
  listener removal probe is a *different* route. `.7` was local/unpublished;
  its stale private-feed blocker ended with `.8` publication. The `.8` bridge's
  23 native Release and pre-commit identical-runtime-source AddressSanitizer
  tests support ISC-44–46; proposed retained-buffer counters were never built.
  One-time `.8` export inspection checked 14 artifacts, not a continuous gate.
- Old PC/SC sharing contention resolved for the selected-key async acquisition
  test. An earlier OTP `0xE00002E2` opening failure had a possible but unproven
  Input Monitoring cause; later discovery timed out with a safely held claim,
  then an isolated late read released it (~1.3 s on serial 20260533). Three
  read-only feature GET cycles passed on that key and three on 31683481; a
  16-test controlled suite checks refusal before late release. These historical
  failures do not block the `.8` typed/direct normal paths or establish recovery
  from a permanently hung driver. The two-second identity budget was not raised.
- The `.3` same-host comparison retains LOCAL-only before dataset
  `/Users/Dennis.Dyall/Code/y/worktrees/yubikit-async-baseline-65964966-async3/artifacts/measurements/comparison-before-20260923T212640783Z.json`
  (SHA-256 `36852b85c6d0a2f7170da3b62c7625ecaffc02e0fcd7fbbbba1c586963b2fd01`)
  and LOCAL-only after dataset `artifacts/measurements/comparison-after-20260923T212725515Z.json`
  (SHA-256 `218016c00bac31fb1e364f5ec4b88a0e9754c7cb8b510043d7711a7f1d3fdb21`).
  Ten completed pairs showed invocation 47.881 → 28.538 ms, terminal
  48.467 → 47.953 ms, lifecycle 78.456 → 80.144 ms; AFTER maximum worsened
  (~151 vs 100 ms). These datasets lack native durations, allocations and idle
  metrics and cannot serve as a `.8` before/after. Historical no-input samples
  were censored, not proof of cleanup.

Do not promote managed tests, clean packages or one host's normal route to
universal acceptance. Global inventory (205 outstanding, including transitive/app
coverage), ISC-56 diagnostics, ISC-53 all-public synchronous waits including
external implementations, and ISC-62 full measurement/budget remain global work,
not an indefinitely open Mac implementation label. The parent has not accepted
additional criteria at that local close-out: **18/72 then, 19/72 now after ISC-50**. No further device configuration change,
touch or new physical topology probe is requested.
