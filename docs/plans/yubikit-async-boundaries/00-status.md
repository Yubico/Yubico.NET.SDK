# Status: YubiKit async boundaries

## Current checkpoint — 2026-09-25

### Current lane board

| Lane | Current state / next boundary |
|---|---|
| macOS typed/direct HID and listener | **Local milestone closed** at shipping `21498d24`: all eight Native AOT selected-key route modes passed, including automatic active cancellation, public pending-read completion, direct IO/feature and actual matching listener callback/drain. No fresh removal or permissionless-host claim. |
| Smart-card lifetime and recovery | **Local milestone closed**: after replug, final-source selected-key PC/SC async open/begin/read/end/dispose/reopen passed three cycles. Historical pre-open sharing contention is resolved for this fixture. |
| Portable diagnostics, discovery and synchronous wait maps | Last finite increment completed in `8d13fd1e`/`8ebcd026`, with bounded tests and explicit gaps; full inventory, diagnostics and public-wait closure remain open. |
| Native package | `.8` published and clean packaged Native AOT consumers verified; this is not Windows/Linux driver or hardware proof. |
| Windows/Linux and backend exploration | Windows/Linux native/hardware lanes deferred; WinRT SmartCard and CryptoTokenKit exploration not started (after production milestone). |

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
revisions and `.8` package; its commit does not change shipping code. This is the current dispatch view; the
[product](01-product.md), [architecture](02-architecture.md) and
[finish rules](03-program-design.md) keep their separate responsibilities.

**18/72 checked, 54 pending.** The [master checklist](../../../2026-09-21-yubikit-async-boundaries-ISA.md#criteria)
owns acceptance and the [verification rows](../../../2026-09-21-yubikit-async-boundaries-ISA.md#verification)
own detailed proof and limits. ISC-5, ISC-19, ISC-31–33, ISC-38–39, ISC-41,
ISC-43–49, ISC-51–52 and ISC-60 are checked. No additional criterion was checked
in this increment. This is bounded macOS and
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
Before: `/Users/Dennis.Dyall/Code/y/worktrees/yubikit-async-baseline-65964966-async3/artifacts/measurements/comparison-v2-before-20260925T111722670Z.json`
(SHA-256 `75150b30488091623487b55b8c7d961c1a100fb9a211dd2b6388c230b15ac440`,
Core binary `928a0b6fcc75e6940d385621065aebed716957ef7d5988d27c5e251e6e2550c6`);
[after](../../../artifacts/measurements/comparison-v2-after-20260925T111741620Z.json)
(SHA-256 `948146da4cb5d12453698a7fd9497939609d4c6402ba5c966cd868a43bbd5488`,
Core binary `bb88e2dea542c97a94c597c893f62f56862a8ab7dc43567aa3fc8c3f7b46e43c`).
Each used two fresh-child warmups and ten completed lifecycle samples.
Medians/p95 (ms, before → after): invocation 23.49/26.72 → 4.69/6.11;
task terminal 23.79/27.81 → 26.48/28.71; lifecycle 70.74/81.75 →
74.31/78.23. **Median lifecycle increased about 5%; not all metrics got faster.**
Baseline no-input failed with PlatformApiException at invocation; after returned
pending and was censored after ten seconds, not a native cleanup proof. The
separate final-source pending-dispose hardware assertion passed. The
[final current profile](../../../artifacts/measurements/current-profile-20260925T111800158Z.json)
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
- The `.3` same-host comparison retains before dataset
  `/Users/Dennis.Dyall/Code/y/worktrees/yubikit-async-baseline-65964966-async3/artifacts/measurements/comparison-before-20260923T212640783Z.json`
  (SHA-256 `36852b85c6d0a2f7170da3b62c7625ecaffc02e0fcd7fbbbba1c586963b2fd01`)
  and [after dataset](../../../artifacts/measurements/comparison-after-20260923T212725515Z.json)
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
additional criteria: **18/72 unchanged**. No further device configuration change,
touch or new physical topology probe is requested.
