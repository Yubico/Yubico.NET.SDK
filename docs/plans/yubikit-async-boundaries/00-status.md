# Status: YubiKit async boundaries

## Current checkpoint — 2026-09-24 (published `.8` and bounded macOS evidence, not epic closure)

Owner: orchestrator for acceptance and sequencing; Route/Native Engineers for the
macOS implementation and native package. Independent review of the expert
feature slice approves ISC-32. **ISC-5, ISC-31, ISC-32, ISC-38, ISC-39,
ISC-41, ISC-43–49, ISC-51, ISC-52 and ISC-60 (16/72 checked, 56 pending)**
at their stated evidence grades. Windows/Linux native and hardware
routes and later backend explorations remain deferred pending user direction;
no criterion is waived.

Managed checkpoint HEAD `4f361504` includes the expert IO slice; the newer
expert-feature changes are uncommitted, not yet attributable to an SDK revision. `efde3ef0`
contains ordinary transport-fault recovery and public contract documentation;
`add0dc73` contains the operation registry and portable boundary controls.
The independent solution-file edit remains outside these commits.

`Directory.Packages.props` now pins private-published NativeShims
`1.18.1-async.8`, attested package SHA-256
`c85c56f7a41c6999b48b1a5fdcd82c56fbfb3e5ee6ff18ccc64a41144f760403`.
[Workflow 35955737172](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/35955737172)
completed successfully at native source `71a23cd0269c968c1d9420eddb2e2fec71e0cc29`:
four build jobs (Windows, Linux arm64, Linux x64, macOS), packaging, seven Native AOT packaged-consumer jobs and private
publication succeeded. Source-bound attestation and the package digest were audited.
Clean packaged consumers cover the seven configured RIDs, including required
`win-x64`, `linux-x64`, `osx-arm64`; this is **packaging** evidence, not device
I/O or callback execution on those systems. Fresh private-feed restore in an empty
cache passed after renaming `nuget.config` source key from `YubicoInternal` to
`Yubico_GH` for the existing credential name at the same URL. Normal parent
`dotnet toolchain.cs restore` passed 43 projects. The former 401 from
`YubicoInternal` and `.7` unpublished local-feed blocker are historical; no
credential changed or entered the repo. Developers need their own feed access.
The independently published `.9` (35956706576) is not pinned or counted.
Native verification-only commits `541cfb09` (export checker) and `760d0416`
(persistent-registration contract comment and test) are not pushed. Neither
changed runtime code, produced nor rebuilt pinned `.8`.

ISC-38 source audit found `SCardCancel` only on the monitoring listener path,
not portable APDU transmit cancellation; cancelled-transmit controlled tests keep
the native borrow until completion. This is a one-time audit, not a gate against
future `SCardCancel` callsites. ISC-39's `PcscContextIsolationTests` runs the
production monitor and connection with controlled native seams and distinct
actual context addresses: disposal cancels/releases monitor A while transmit is
held; connection B remains borrowed and is released after its own completion.
This is not operating-system native proof on Windows or Linux.

ISC-44–46 are accepted for the **five new macOS `Native_HidInput*` exports** only.
Source and Apple's callback-handler contract retain the report buffer, device
and context through callback return or cancellation acknowledgment plus accepted
delivery drain. Synthetic destroy-BUSY, close-attempt counters and callback-drain
tests passed: 23 native Release at package producer `71a23cd0` in continuous
integration; 23 AddressSanitizer tests passed locally on identical runtime source
before that commit, **not** rerun at its SHA. Actual `.6` unplug/replug after
acknowledgment/drain and `.8` normal device runs are complementary, version-specific
evidence; no new `.8` unplug was performed. Independent review accepted these as
an evidence-method substitution: the originally proposed native retained-buffer
counters were **not implemented**. ISC-43 is now checked under the explicit
`hidinput/owner.h` **persistent registration** contract at `760d0416` for the five new macOS
`Native_HidInput*` exports, not a per-report command/submission callback rule.
Create cannot call back; Start registers before activation, which can trigger
callbacks on another queue **before Start returns** (source-modeled possibility,
not a witnessed race). A single Start may deliver zero or many accepted reports,
once each in order; terminal occurs at most once after accepted reports drain.
Cancel requests shutdown without promising a terminal callback, and WaitShutdown
acknowledges and drains. Existing native lifecycle tests were extended to exercise
two reports after one Start and reject a repeated Start; 23 Release tests pass,
with no runtime change from package producer `71a23cd0`. This does **not**
pretend a per-operation callback-count fixture passed or waive a future async
operation's own contract.

ISC-47 used `Yubico.NativeShims/tests/check_package_exports.py` from verification-only
native commit `541cfb09` against the **actual** `.8` package digest above. LLVM
inspected all 14 shared/static artifacts across `linux-arm64`, `linux-x64`,
`osx-arm64`, `osx-x64`, `win-arm64`, `win-x64`, `win-x86`: all passed, 36 canonical
`Native_*` symbols each except macOS with 41, and no test-only helpers exported.
Five checker self-tests passed, including compiled Mach-O shared/static negative
controls. This is one-time artifact proof, **not** a continuous integration gate,
native behavior on those seven systems, or a rebuild at `541cfb09`.

ISC-60's six `ResponsivenessProbeTests` reject actual legacy synchronous macOS
feature-open and `OtpHidConnection` GET/SET waits via a caller/native thread-identity
gate. Positive built-in macOS FIDO open and OTP open/receive probes additionally
return before withheld native release. This managed sensitivity is scoped to
these entries, not every platform or every adapter.

ISC-51 is the built-in PC/SC async transaction acquisition, **not** the public
interface's synchronous default fallback for external implementers:
`PcscConnectionLifetimeTests.AsyncTransaction_BlockedNativeBegin_ReturnsPendingTaskWithoutBlockingCaller`
holds native begin, checks the returned task remains pending, releases it in
`finally` and checks one end. The existing async transaction path and managed
probe meet this literal criterion, not the whole PC/SC lifecycle/platform matrix.

The current Core-only inventory assertion has **200 classified but outstanding
sites** after the feature changes: 130 native imports, 24 waits, 15 scheduling
sites, 21 pre-task-return dispatch gaps, six
callback registrations, two delegate conversions and two unmanaged callback
addresses. The separate registry lists **13 required operations**
and **26 profile links** across macOS FIDO, macOS OTP and portable PC/SC;
reflection checks owner/symbol, roles, test attributes and missing/duplicate
rows. The 26 links resolve to named runnable Fact/Theory tests. It covers only
the previously migrated typed routes, not expert raw IO/feature rows or every required
adapter in the master ISC-4 matrix.
After scoped formatting, the targeted `BoundaryInventory` run passed **28**:
13 scanner, three registry, six responsiveness and six diagnostics. The six
diagnostics cases cover a three-file logger inventory and real PC/SC/OTP
sentinel paths with full payloads and five-byte prefixes for framed payloads;
externally sourced exception messages and all other migration logs have not
been exhaustively proved safe. ISC-56 remains open. Scanner/registry coverage
is not universal.

**Committed expert macOS input slice (ISC-31, reviewed PASS):** typed
`MacOSFidoHidConnection` detaches a cancelled expected reader under lock. A
pending-read cancellation does not cancel native input or create a terminal;
late reports queue, and an old token cannot detach a new reader. Public expert
`MacOSHidIOReportConnection` is now a synchronous facade over the same persistent
native owner, not a caller `CFRunLoopRunInMode` pump, with no legacy callback
handle cleanup. Public `IHidConnection` shape and synchronous open/Get/Set/dispose
remain unchanged. `GetReport` still times out after six seconds with
`PlatformApiException` and can retry on the same connection. Expert output
accepts any report length for native validation, while typed FIDO still requires
64 bytes; input normalizes 64 bytes or 65 with zero report ID, and malformed
input terminates rather than promising identical legacy failure behavior.
Construction retains ordinary `PlatformApiException` wrapping and propagates
`UnrecoveredConnectionException` for unproven cleanup. A native SET failure now
retains `PlatformApiException` status rather than the typed route's
`InvalidOperationException`—an intentional public exception difference.
Five read-cancellation, eight IO-compatibility and three facade tests passed
(16); six old legacy tests were removed after meaningful cases moved into the
new coverage. Native-AOT `.8` `--expert-io` on serial 31683481 observed a
6,011 ms timeout followed by **same-connection** INIT/getInfo and
dispose/reopen/getInfo passing, without an operator. No new touch or unplug.
ISC-31 covers macOS HID **input waits**; macOS OTP feature GET/SET has no
input callback and is not the same route. ISC-33 still needs discovery-manager
callback quiescence; ISC-53 still covers all public synchronous waits. These
tests do not close ISC-4's registry gap or make all macOS async work complete.

**Current expert macOS feature slice (ISC-32, reviewed PASS WITH NOTES):** the
public synchronous `MacOSHidFeatureReportConnection` now delegates to the
typed macOS OTP owned worker: native open and descriptor metadata read, feature
GET/SET and checked shutdown run there. Typed FIDO output and expert IO SET
use the FIDO connection-owned worker; typed OTP GET/SET and expert feature
GET/SET use the OTP worker. These are classified blocking-worker fallbacks,
**not** claims that native callback GET/SET APIs are used or that public expert
methods are nonblocking. Each connection has one worker/one admitted operation;
no process-global capacity guarantee is established. Expert GET always returns
one owned eight-byte array: native lengths 0–8 remain zero-padded, lengths over
eight fail and zero the buffer. Typed OTP send still requires exactly eight
bytes; expert SET permits any length for native validation. Accepted calls
drain before checked close. Both expert IO and feature `GetReport` now return
their owned whole arrays via `MemoryMarshal.TryGetArray`, not an extra
uncleared copy; identity tests pin the ownership contract. Eleven feature and
nine IO compatibility tests passed after that fix. The public `IHidConnection`
shape remains unchanged; synchronous expert open/Get/Set/dispose still block.
The **parent worktree's** Native-AOT host with pinned `.8` ran
`--expert-feature --serial 31683481`: 3/3 eight-byte feature GETs and read-only
Management device info via feature SET/GET, with dispose/reopen and matching
serial, passed. Separate `--otp-info` typed queries passed 3/3 on the same key.
No new touch/unplug or other-platform native driver proof. ISC-33 still needs
listener/manager callback quiescence, and ISC-53 remains broader than these
documented expert waits.

At `.8`, selected-key macOS 15.7.7 arm64 / .NET 10.0.12 probes passed five
normal Native AOT scenarios, active cancellation and three read-only OTP info
queries on serial 31683481 (firmware 5.7.4). OTP partial-send/read faults now
attempt a safe abort once after an attempted write: successful abort permits
reuse, failed abort faults the protocol while preserving the original failure,
without replaying the original command. Thirty-five focused OTP protocol tests and one scripted Mac OTP
test passed; these do not prove physical mid-frame fault recovery. The actual
unplug → terminal → dispose → replug proof remains at `.6` on the same removal
source, **not** a repeated `.8` removal run. Combined unplug/dispose uncertainty
stays conservatively quarantined. The normal-use route is finite at this evidence
grade, not a universal platform/hardware completion.

Public XML contracts now describe borrowed memory/task lifetime, cancellation,
synchronous fallback and raw reuse caveats; no public API signature was changed
in this documentation increment. **Last full Core run: 1,451 passed/3 skipped**
after the expert-feature fix. PublicApi 22, YubiOtp 180 and resilience-fast 77
passed; Fido2 471 and 35 focused OTP protocol tests passed at earlier
checkpoints. `dotnet toolchain.cs complexity` passed 21 changed shipping methods at cyclomatic
≤10/cognitive ≤20; verification/harness/test methods were manually split but
excluded by the tool, so they are not tool-certified. The targeted inventory's
28 passes after earlier scoped formatting are a separate prior run, not a
claimed rerun from this docs update.

The pre-expert-facade `.8` [current profile](../../../artifacts/measurements/current-profile-20260924T051049865Z.json)
(SHA-256 `9bbd8837afab35e1143385bc6e383a9baf330a5c5ce20de5892b2090b7ba1f65`)
records schema 2, two completed warmups, ten completed normal samples, and one
completed idle sample (.NET 10.0.12). Normal medians: caller return 5.00675 ms,
operation completion 24.84525 ms, disposal 2.90555 ms, allocated 60004 bytes;
one idle sample recorded 1.945 ms CPU and zero idle allocated bytes over ~1 s.
These observations predate both expert facades and are **not** a `.8` before/after comparison or
comparable to the historical `.3` pair. Native-only duration and pending ordinary
count are null with instrumentation-unavailable reasons; ISC-62 stays open.

Finite next work: parent integrates the uncommitted expert-feature slice and
records its actual SDK revision later. Study macOS listener/manager callback
quiescence separately; ISC-32 does not close ISC-33 or universal ISC-53.
ISC-56 still needs coverage beyond the three inventoried files and sentinel
paths, and the registry needs expert raw IO/feature rows for universal ISC-4.
`docs/architecture/raw-access-tiers.md`
and `src/Core/README.md` now describe the expert boundary; their earlier
snapshots are historical. Physical `.8` removal awaits an operator. Prioritize
normal-use correctness over hot-plug storm tuning; do not infer Windows/Linux
native results from packaged consumers. Parent owns subsequent commits; no
staging, new report or device mutation is authorized by this doc update.

### Previous `.7` checkpoint (superseded for current-state claims)

At that earlier checkpoint, source `89420aa6` contained the transport increment, `e2093a07` the failed
OTP reset/reuse correction, and `cb8dda26` the Core semantic inventory gate. Native
removal cleanup is committed separately as `71a23cd0`. These commits were not pushed;
the local `.7` package is still not a published dependency.

| Evidence grade | Observed result and limit |
|---|---|
| Real macOS FIDO key | On serial 31683481, firmware 5.7.4, `.3` active cancellation selected `DeviceWaiting`, cancelled, resolved presence once, then same-session getInfo and reopen passed. First touch attempt expired because operator coordination missed the window; the second passed (`/var/folders/gn/mh64zz5969j89_f5dffnvnb80000kt/T/opencode/yubikit-touch.log`). This is not evidence of every cancellation timing. |
| Physical removal and native fix | `.3`, `.4` and `.5` removal/dispose attempts failed at close with `BadArg`. Native `.6` uses the real Apple `service_terminated` removal callback plus cancellation acknowledgment and drain; its guarded removal-only close interpretation permits `BadArg` **only** after that evidence. Actual unplug → terminal → dispose → replug → fresh-generation getInfo passed (`/var/folders/gn/mh64zz5969j89_f5dffnvnb80000kt/T/opencode/yubikit-removal-async6.log`). Native Release and AddressSanitizer tests passed 23 each; cross-vendor review PASS WITH NOTES. Candidate edge unplug/dispose race remains conservatively quarantined rather than optimized into unsafe release. Do not generalize this guard to ordinary close errors. |
| `.7` local candidate | `1.18.1-async.7` retains `.6` behavior but is silent by default (no diagnostic standard-error output). Local unsigned/unpublished package SHA-256 `68c3ca219a5581fdce7c11f98c0bc7b83d38a0f0f191278480ae48d49fc41b83`; built **before** local native commit `71a23cd0` (not pushed), from the same-behavior source. Its producer metadata remains `f8c974` plus dirty changes; `71a23cd0` is a source checkpoint, **not** the package-producing commit or a post-commit rebuild. Native-AOT normal five scenarios, active cancellation and three read-only OTP info queries (feature SET/GET and dispose/reopen) passed on serial 31683481. No new `.7` operator unplug run: removal hardware proof is `.6` on the same removal code, not a `.7` hardware result. `.6` live diagnostics and `.7` configuration-only diagnostics are distinct. |
| OTP recovery | Production failed-reset latch is centralized in `ResetStateAsync`: subsequent protocol exchanges, status reads and Configure are rejected after failure, without replacing the primary cancellation/timeout. Twenty-eight focused tests and one real-protocol scripted `MacOSOtpRecoveryTests` pass (successful abort and cyclic-redundancy-checked reuse). Native physical mid-frame failure was not exercised; the latch is per protocol instance, not global to a borrowed raw connection. `RawOtpHidSession` recommends reopening after reset failure; creating another session over the same connection could bypass the latch. |
| Smart card | Selected-key asynchronous transaction/read/reopen passed once after earlier sharing contention. This does not verify remaining PC/SC platforms, response-time matrix or other readers. |

S0b Core-only inventory checkpoint (scanner Engineer; bounded parent review complete):
`src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/` contains the semantic
scanner and exact-source-site baseline. Thirteen targeted tests pass, including a cross-file
source-order regression; **198** sites (130 native
imports, 18 waits, 17 scheduling, 21 pre-task-return dispatch gaps, six callback
registrations, four delegate conversions, two unmanaged callback addresses) remain
documented/outstanding, **not** verified safe. The unknown native import fixture and
exact manifest gate establish ISC-5; stale and invalid-review rows also fail. Only
Core shipping source in the current .NET 10 preprocessor configuration is scanned;
unknown conditional symbols fail. No native exports, applets, all build configurations,
or whole-program transitive call graph are covered. ISC-6 stays unchecked: interface
dispatch is flagged for review, not demonstrated to reach every blocking implementation.
ISC-1–4/7–8 remain open (7/8 are only partially supported). Two low-priority deferred
review note: callback forwarder/nontransitive helper native-graph coverage; it cannot
be counted as a completed universal gate. Source-order dependence is fixed by
two-pass collection, not deferred.

Package/restore boundary: `Directory.Packages.props` pins `.7` locally. Fresh restore
against the private feed is **blocked** until `.7` is pushed with package-read access;
cached local restore requires explicit `RestoreConfigFile` selecting the local package
source. The last private **published** version is `.3`, not `.7`. Do not substitute a
cached artifact for fresh-feed proof. The next release step is promotion through the
already approved private workflow **on a future explicitly authorized push**, not a
push/publication now; bind rebuilt producer metadata to source before claiming ISC-41/48.
No hardware mutation or new Windows work was performed for this checkpoint. Scoped
format touched meaningful changes only; existing xUnit1051 analyzer warnings remain,
so no zero-warning build claim. Final full Core run: **1,400 passed/3 skipped**, including
the thirteenth inventory regression. Other recorded managed runs:
PublicApi 22, Fido2 471, YubiOtp 180, resilience-fast 77. These are SDK runs
on .NET 10.0.12 after another agent updated the global SDK to 10.0.401; the paired
comparison below used .NET 10.0.0. No later operator step is required for this bounded
checkpoint; remaining physical failure/race evidence is deferred, not silently passed.

### Measured comparison, not a performance acceptance claim

Ten completed before and ten after lifecycle samples used the same .NET 10.0.0 host,
serial 31683481 and pinned `.3` native package. The BEFORE transport source is
`65964966`, AFTER `89420aa6`; these results do **not** measure the subsequently installed
`.7` package. Dataset and SHA-256 pairs:

- BEFORE: `/Users/Dennis.Dyall/Code/y/worktrees/yubikit-async-baseline-65964966-async3/artifacts/measurements/comparison-before-20260923T212640783Z.json` — `36852b85c6d0a2f7170da3b62c7625ecaffc02e0fcd7fbbbba1c586963b2fd01`.
- AFTER: `artifacts/measurements/comparison-after-20260923T212725515Z.json` — `218016c00bac31fb1e364f5ec4b88a0e9754c7cb8b510043d7711a7f1d3fdb21`.

The persistent comparison runner has six passing self-tests. It is pinned to the
published `.3` package and historical source revisions; with the present `.7` checkout
its `--measure` preflight rejects the mismatch. The recorded medians (before → after, milliseconds) are invocation return
47.881 → 28.538, task terminal 48.467 → 47.953, lifecycle completion
78.456 → 80.144. AFTER's lifecycle maximum is worse (about 151 versus 100 ms).
One prior BEFORE block had 8/10 completed, one failed and one unattempted; it is
preserved, not counted in the completed pair. The paired no-input samples are not completed lifecycle samples: BEFORE failed
at invocation start, AFTER timed out after invocation return; neither proves native
cleanup. The before binary was preserved, then rebuilt with a different hash; the
dataset records the input at measurement time, not a claim that the rebuilt hash
equals the original. No previously approved numerical budgets; native durations,
allocations and idle activity were not measured. **ISC-62 remains open.**

### Bounded macOS finish line and remaining evidence

Prioritize truthful failure and safe reuse over hot-plug storm optimization. The
Route Engineer implemented the failed-abort per-protocol latch; the Native Engineer
retains conservative root/quarantine for the unplug/dispose edge. Finite correctness
review returned PASS WITH NOTES; stop this bounded route/probe/review iteration here,
not an endless cleanup loop. The scripted success and `.6` unplug pass do not prove
physical mid-frame failed abort, combined-race release, or cross-session isolation
on one borrowed raw connection. Do not silently expand to Windows, mutate device
configuration, relax quarantine or promote any route-level result to epic closure.

Finite follow-up evidence for one selected macOS key, deferred rather than required
for this checkpoint: Route Engineer owns physical mid-frame OTP outcome and any
borrowed-connection cross-session contract decision; Native Engineer owns further
unplug/dispose native outcome while retaining conservative root/quarantine. Non-goals:
hardware failure injection, credential/configuration writes, Windows/Linux migration,
hot-plug storm tuning and universal criterion closure. Applicable open criteria include
ISC-13, ISC-18, ISC-28, ISC-33 and ISC-59. Scripted probes (1) failed abort rejects
reuse on the same protocol and (2) successful abort permits cyclic-redundancy-checked
reuse are complete; (3) removal versus dispose retains an unresolved generation when
release is unproven and (4) selected-key mid-frame cancellation requires physical
operator coordination and remains pending. For a future changed route, use the focused
Core `MacOSOtpRecoveryTests` and `MacOSOtpRouteTests` filters with
`dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~MacOSOtpRecoveryTests"`
and the corresponding `FullyQualifiedName~MacOSOtpRouteTests` filter; use native Release
and AddressSanitizer tests for native outcomes and a selected-key read-only host probe
only when the fixture is available. Stop after finite probes and one correctness review
with targeted fixes, or record a blocker. This bounded implementation milestone is
complete at scripted/selected normal-use grade, **not** physical failed-abort or full
platform acceptance.

## Historical checkpoints (superseded for current-state claims)

The dated entries below preserve observations at their original package pins and
test counts. Statements there that touch, removal, OTP feature SET or a comparable
BEFORE dataset are still pending describe **those earlier checkpoints**, not the
current evidence above. They are not a current test tally.

### Earlier verified checkpoint — 2026-09-23

The private-feed `1.18.1-async.3` workflow finished successfully across the configured
build and native ahead-of-time jobs. An arm64 macOS production-host publish and live run
on serial 31683481 (firmware 5.7.4) passed five read-only scenarios: three
open/initialize/getInfo/dispose cycles, pending receive → dispose → reopen, and
predispatch cancellation → reopen. The macOS FIDO open-failure cleanup now distinguishes
an unopened device from an exclusive-access open; 18 focused tests passed. This does
not cover physical removal, touch, or a valid before/after performance comparison.

Smart-card awaitable transaction acquisition is additive and passes eight focused
managed tests; its selected-key integration test encountered PC/SC sharing contention,
so native/hardware responsiveness evidence remains pending. macOS OTP feature GET/SET
has a connection-owned worker and 15 passing focused tests. Its direct device open
encountered IOKit access failure (`0xE00002E2`), so native OTP recovery is unproven.
After the last OTP changes: Core 1,378 passed/3 skipped, PublicApi 22, YubiOtp 180,
resilience 77, and documentation validation passed. No whole-platform acceptance
criterion was newly checked. Keep the independent platform routes and final closure
in the schedule below; these three increments are code checkpoints, not epic closure.

After the YubiKeys were replugged, the selected smart-card async transaction/read/reopen
integration test passed once on an authorized USB key (previously blocked by a PC/SC
sharing violation). The same test-run initialization found three OTP keyboard interfaces,
but every `IOHIDDeviceOpen` failed with `0xE00002E2` (`kIOReturnNotPermitted`), so replugging
did not unblock macOS OTP hardware proof. Apple's Input Monitoring permission is a
possible host-level explanation, not a verified cause for this process; no device or
privacy setting was changed. This single-key smart-card result does not verify the
remaining PC/SC platforms or the full transaction-responsiveness matrix.

After the terminal restart and another replug, the native-AOT verification host found
three physical keys (serials 20260533, 31683481 and 125), each advertising OTP, FIDO,
and smart-card interfaces. The general typed-connect integration test first passed
while only the 20260533 smart-card interface was visible, so that run did not exercise
OTP. With all three visible, its retry failed for a different reason: an OTP discovery
identity read on one interface exceeded its two-second budget while native ownership
was active. The registry marked that claim unrecovered, and a subsequent typed open
correctly refused it. A direct selected-key OTP GET probe in an isolated process also
encountered the unrecovered claim during discovery, before any direct GET was submitted.
Other OTP discovery reads did execute on the device, so the earlier blanket access
denial is not the present failure. OTP feature GET/recovery hardware acceptance remains
pending; do not release the claim or extend the budget without establishing the native
completion and ownership outcome. The selected 20260533 FIDO route still passed five
read-only native-AOT scenarios after the replug.

The follow-up isolated probe established the late outcome without changing the discovery
budget or releasing an unproven claim: serial 20260533's timed-out OTP read released its
claim about 1.3 seconds later, after which three read-only feature GET/open/dispose
cycles passed. Serial 31683481 passed three such cycles without a delayed claim. A
controlled regression now holds native GET beyond the caller's budget, verifies new
connections are refused while ownership is unresolved, then releases GET and waits for
claim recovery before connecting again (16 OTP focused tests pass). Core 1,380 passed,
3 skipped; YubiOtp 180 and documentation validation passed. This is successful feature
GET and eventual release evidence, not a demonstration of touch, OTP feature SET on
hardware, or recovery after a permanently hung driver. The 2-second identity budget
remains unchanged; a timeout still temporarily quarantines that interface for safety.

## Historical `.3` package checkpoint (superseded by local `.7` candidate)

The user authorized restoring `1.18.1-async.2` and building the native additions for the
private Yubico feed. Local restore succeeded. Native-only commit
`f8c974f785d96dfc654606b573c6840968bb8220` was pushed on `feature/macos-hid-input-dev`.
[Workflow run 35888947280](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/35888947280)
published `1.18.1-async.3` successfully; workflow 35888947280 completed SUCCESS with
macOS/Linux/Windows builds, AOT consumers and private publish.
`Directory.Packages.props` now selects `.3`; `NuGet.Config` maps NativeShims to the
private Yubico feed. Direct local restore returned 403 with the available credentials.
The exact workflow package artifact was instead downloaded and restored via an isolated
temporary source; all 17 focused macOS lifetime tests passed against that package.
Its SHA-256 is `b6df35457da06409f5bfd6643dd7dbc9da8b0e7e8404bb5fa99fcdde076f4a3c`.
Fresh private-feed restores require package-read credentials; this artifact-based check
does not claim private-feed authentication succeeded. Native tests passed 11/11 and
static package validation passed before dispatch.
The managed milestones below are not committed; `db7a1bf6` is the recorded first
smart-card slice commit only. Earlier `.2` hardware evidence remains version-specific;
no selected-key `.3` FIDO or OTP device run is verified.

- Gate 1 — Product: APPROVED 2026-09-22 for the revised single-key-first scope.
- Gate 2 — Architecture: APPROVED 2026-09-22 for the revised single-key design.
- First smart-card lifetime slice: implemented and verified 2026-09-22; code review PASS WITH NOTES.
- Built-in macOS FIDO production code: selected-key normal and pending-raw-receive dispose/reopen paths passed with the local `.2` preview; touch, physical removal and interrupted shutdown remain unverified, so the route is not accepted.
- S4 smart-card async transaction: additive public `ISmartCardConnection.BeginTransactionAsync`; built-in awaitable worker and synchronous fallback for external implementations documented. Eight focused managed tests, pre-OTP Core 1,363 and PublicApi 22 passed. Selected PC/SC hardware integration test failed with a sharing violation; no hardware proof or S4 closure.
- S2 macOS OTP GET/SET: worker feature-report candidate implemented; eight focused unit tests passed, Core 1,371 passed/3 skipped, PublicApi 22, YubiOtp 180 and resilience-fast 77 passed. No selected-key actual OTP hardware or touch probe; not S5 or universal criterion acceptance.
- Master acceptance: 2/72 verified (ISC-49 and ISC-52); 70 pending.
- Whole-effort program/slice specification: deferred to implementation effort at the user's request.

## Authority and approvals

Historical direction (master D31): the user explicitly approved local `Yubico.NativeShims`
**`1.18.1-async.2`**, since superseded by the published `.3` pin above. Parent restore
with `.2` passed with a temporary local feed and explicit `RestoreConfigFile`; this unsigned,
unpublished preview was not a stable release. The earlier 1.18.0 override remains a
historical attempt: restore and 17 focused managed lifetime tests passed after the
readability cleanup, but its cached macOS arm64 library had no `Native_HidInput*` exports.
Those tests prove only the controlled seam at 1.18.0. The selected-key normal and later
pending-read shutdown probes used `.2`; the latter followed the latest restore.
Another agent owns the interop migration and root policy files.

The [master acceptance plan](../../../2026-09-21-yubikit-async-boundaries-ISA.md)
owns the 72 stable acceptance criteria, detailed requirements, evidence, and decisions.
This file records approved direction, slice checkpoints and implementation progress.
Gate documents are review views of the master plan, not independent specifications or
replacement acceptance registries. Reconcile an approved change into the master plan
before dispatching affected work.

The user approved the earlier broader Gates 1 and 2 on 2026-09-22, then requested a
single-key first iteration while retaining the multi-key planning. Reopen the affected
gates rather than treating the earlier approvals as authority for a different scope.
Gate 3 was positively received with this scope correction, not unconditionally approved.
The user approved revised Gates 1 and 2, then explicitly requested incremental delivery
instead of waterfall specification of the whole effort. Review first-slice pseudocode,
then implement with tests, independent review and a consistency check. Non-blocking
details are **deferred to implementation effort** and handled live. Do not impose a
separate whole-epic Gate 3/Gate 4 approval before that slice. Material public-contract,
architecture or safety changes still require user approval before implementation.

Under D30's newer delegation, the orchestrator owns master evidence and sequencing and
may direct Engineers through coherent in-scope implementation milestones, including
internal seams, routine packaging, fixture selection and tests. D14/D25's historical
micro-walkthrough requirements do not halt this approved macOS direction. Preserve public
raw-access and safety contracts; escalate material scope change, unresolvable blocker or
destructive operation. No blanket authorization for credential writes, releases or commits.

### Working agreement

After each meaningful checkpoint, update the master, this status and the affected plan
while work progresses. Record scope and owner (orchestrator or named Engineer), outcome
(`planned`, `in progress`, `implemented`, `verified`, `blocked` or `deferred`), concrete
evidence or command plus its limits, and the next decision. Distinguish document/design
review, managed implementation verification, native-runtime verification and hardware
verification. Do not create a separate report artifact or infer completed evidence.

When recommending a next move, use: **document section → relevant rule stated plainly →
current evidence or gap → recommendation and approval needed**. References alone are not
enough; spell out the rule that governs the recommendation.

For each implementation slice, apply master D26's finish line before dispatch: agree one
observable route outcome, bounded files/responsibilities and non-goals, applicable existing
criterion IDs, finite probes/commands and required evidence grades. Close only when agreed
behavior passes without correctness/safety/regression blockers, appropriate correctness
review of the settled shape and targeted fixes are complete, and evidence/limits/next
action are recorded. Stop at a coherent milestone rather than each tiny helper;
defer taste-driven redesign and speculative abstractions. Missing required native, hardware,
platform or performance evidence means **blocked**, not silently managed-only complete.

Historical `.2` checkpoint — owners: Route/Native Engineers (macOS FIDO/OTP), Smart-card Engineer
(async transaction), Measurement Engineer (baseline tooling), orchestrator (integration/evidence).
**Selected normal and pending-read shutdown paths verified on key 31683481 (firmware 5.7.4); route not accepted.**
Final `.2` preview-package runs: Core 1,355 passed/3 skipped, PublicApi 22, Fido2 471,
Management 86, resilience-fast 77; after a small report-queue allocation fix, focused
macOS FIDO route tests passed 17 (full suites precede that final allocation-only edit).
Native 11 and synthetic AOT 5 passed as supplied. The earlier packaged-host `--list`
attempt found zero keys; a later explicit-serial native-AOT production-host probe with
real Core/Fido2 and `.2` passed three read-only open/init/getInfo/dispose/reopen cycles.
The same fixture passed one `PcscLifetimeIntegrationTests` read-only test (three
connections, two transactions and reads per connection). Benchmark-tooling artifacts
record three completed lifecycle samples and one censored no-input sample: invocation
returned in 79.9447 ms, but the watchdog ended the child before shutdown was requested.
That is **not** evidence of shutdown failure or completion. No touch/unplug/removal,
BEFORE dataset, comparison or performance budget exists. Parent correctness review/fixes
are not an independent review. A later real-Core/Fido2 native-AOT probe with the local
`.2` preview passed three read-only cycles and pending raw receive → `DisposeAsync` →
terminal read → completed dispose → reopen/getInfo. This exercised real IOKit callback
acknowledgment in normal shutdown, not physical removal, touch or every interruption race.
Seventeen focused post-craftsmanship managed tests passed with the preview. The bounded
craftsmanship cleanup (named native codes,
per-owner capacity, format quarantine helper, comment trimming) finished with review
PASS WITH NOTES and 17 focused managed tests at the historical 1.18.0 pin; these older
tests remain separate from the later preview result.
**ISC-49/52 verified; 70 criteria pending**, including
ISC-31/33/58. Artifact names and evidence limits are in master Verification; exact
host/integration shell invocations for the earlier user run were not retained. The later
publish and explicit-serial probe commands are recorded there.

Small deferred list: cross-key scheduling remains deferred by D20 because it is outside
the single-key iteration; wholesale `IHidConnection` migration remains deferred because it
needs a separate approved compatibility walkthrough. macOS GET/SET worker code now exists,
but callback capability and actual per-direction native/hardware evidence remain pending.
Add an item only with a reason and remove or promote it when its scope is selected.

### Historical work schedule (not the current dispatch order)

This is dependency order, not calendar estimates or approval of production changes.
The orchestrator owns acceptance and integration; Engineers receive bounded assignments.

N1/D1/B1 preparation and D29 synthetic checks preceded D30's production dispatch. Older
1.18.0 checks and D29 no-production statements remain historical, not the current state.
Preserve the staged planning edits; distinguish the executed selected-key normal path
from synthetic/AOT publish-only evidence and untested interruptions. Record progress
at milestone checkpoints, not every test ping.

| Assignment | Owner | Outcome and finite exit |
|---|---|---|
| N1 — native prerequisite card | Native Engineer | Historical 1.18.0 signed-package checks remain recorded; current `.2` preview uses the tagged 1.18.0 source base plus local edits. Its exact local source hash manifest must be recovered for clean-machine replay; no release or signed-package producer binding is claimed. |
| macOS FIDO production | Route/Native Engineers | Code implemented; selected-key read-only packaged native-AOT normal path and pending-read shutdown/reopen passed. Touch, physical removal and interrupted callback quiescence pending; no route acceptance. |
| S2 macOS OTP GET/SET | Route Engineer | Worker feature reports implemented and managed-tested (8 focused; Core 1,371/3 skipped, PublicApi 22, YubiOtp 180, resilience 77). Selected-key OTP device/touch and recovery remain unverified; not S5 closure. |
| S4 smart-card transaction | Smart-card Engineer | Additive async interface and built-in worker/fallback implemented; 8 focused tests and pre-OTP Core 1,363/PublicApi 22 passed. Selected PC/SC integration failed sharing violation; hardware proof blocked. |
| B1 — baseline tooling | Measurement Engineer | Implemented; 8 self-tests, dry-run and 12-benchmark listing; later three selected-key lifecycle samples completed and one no-input sample censored before shutdown request. No BEFORE dataset/budgets. |
| D29 input-owner experiment | Native Engineer + verification Engineer | Completed at synthetic grade (8 C tests in three configurations, 3 executed synthetic AOT probes); real IOKit proof remains part of active production milestone. |

Schedule the actual work as follows:

| Order | Work / epic contribution | Owner | Starts after | Finish line |
|---|---|---|---|---|
| 1 — macOS selected paths verified; route pending | Built-in macOS FIDO lifecycle | Route/Native Engineers + orchestrator | Normal and pending-read dispose/reopen probes passed | Touch, physical removal and interrupted callback shutdown; comparable before/after data and producer checks. No full route acceptance yet. |
| 2 — next production increment, design read-only now | Windows FIDO overlapped read/write, terminal completion and `CancelIoEx` races | Architect/orchestrator for design; route Engineer after scope selection | Independent of macOS hardware | Finite production-route probes and Windows-host runtime/driver/hardware evidence; macOS managed seams cannot prove Windows behavior. |
| 3 — independent lane | Linux HID nonblocking readiness and explicit wake | Platform Engineer when scoped | Does not need Windows to finish first | Native/hardware proof of read readiness and output isolation on Linux; not inferred from macOS or Windows. |
| 4 — remaining reports and protocol routes | macOS GET/SET candidate and OTP recovery, including Windows zero-access feature reports | Route/Native Engineers | macOS worker code and managed tests passed | Selected-key GET/SET/touch and per-direction native evidence; Windows and recovery remain pending. |
| 5 — smart-card continuation | Async transaction acquisition, context isolation and lifecycle proof | Smart-card Engineer | Additive async path and managed tests passed | Resolve selected PC/SC sharing violation, then hardware and cross-platform lifecycle/context proof. |
| 6 — production closure | Inventory, native/package producer and consumer checks, before/after measurements, platform matrix and consistency | Orchestrator + Engineers | Relevant route evidence, not a strict serial dependency between platform lanes | Close ISC-1–64 only with every required row verified; current count stays 2/72. |
| 7 — later exploration | WinRT/CryptoTokenKit prototypes and recommendation | Orchestrator + Engineers | Separate from production milestone | ISC-65–72; no default backend promotion without a new scope. |

NativeShims 1.18.0 was the selected signed upstream base; current development selects
private-published `1.18.1-async.3` for five new macOS exports. The earlier `.2` local preview's tagged
source base is recorded in the master; the cited dirty-file hash manifest was absent on
later inspection and must be recovered for a reproducibility claim. No original 1.18.0
package-producer binding is asserted. The older 1.16.1 binary targeted minimum 14;
1.18.0 macOS artifacts target minimum 12;
the project targets .NET 10, whose upstream support matrix currently lists macOS 14, 15
and 26 on arm64/x64; the relevant dispatch APIs are available from 10.15. These are distinct
facts, not a floor-compatibility fix, new YubiKit hardware claim or perpetual future-support promise. D28 selects
modern dispatch APIs without an old-API fallback solely for the binary minimum. Package
metadata still does not bind the original signed 1.18.0 to a producer commit. D30 permits
routine native bridge work within this approved direction, not releases or scope expansion.
The package declaration now reads `1.18.1-async.3`; local restore via the identical workflow
artifact passed, whereas direct private-feed access returned 403. The `.2` pending-read probe
followed its earlier local-feed restore. Replaying that historical preview
requires tagged source plus dirty-file hashes, native macOS package scripts/inputs, a
temporary local feed listed with normal feeds in a NuGet configuration, and explicit
`RestoreConfigFile` for restore/build/publish; a temporary path alone cannot reproduce it.

Shared-file boundary: baseline tooling stays in the benchmark project; the Native Engineer
owns only the approved native worktree/harness; the Route Engineer owns the approved Core
slice; the orchestrator alone updates master/status. No shared-file editing is parallelized.
Windows/Linux route evidence requires suitable hosts. Smart-card
Windows/Linux proof, asynchronous transaction acquisition, remaining protocol boundaries,
and production/exploration closure remain visible in the master rather than disappearing
behind this macOS schedule.

## Product review

- [Gate 1 product](01-product.md): approved for one selected key at a time on 2026-09-22.
- Already approved: breaking v2 changes are permitted if equivalent public applet
  contracts remain consistent; unnecessary public implementation types may become
  internal (master decision D15).
- Already approved: raw sessions and raw connection operations remain public,
  including consumer connection implementations and caller-owned connection reuse
  (master decision D16).
- Non-blocking exact signatures, helper/seam layout and measurement details are deferred
  to implementation effort. The approved behavior, public tiers and safety rules remain binding.

## Architecture review

- [Revised Gate 2 architecture](02-architecture.md): approved after independent
  review. It selects a connection-lifetime owner, one lazy worker for blocking native
  calls, no ordinary-operation backlog, and coalesced transaction-end/shutdown intents.
  Native completion/readiness and explicit macOS IOKit expansion remain.
- The complete [earlier architecture](addenda/earlier-multi-key-architecture.md) is
  archived. Shared pools, credits and cross-key progress guarantees stay deferred.
- Retain the current between-read FIDO cancellation sequence; the proposal imposes no
  new simultaneous-send/read obligation on external raw-connection implementations.
- Native lifetime, safe release, strong rooting, late-completion handling, visible
  unrecovered state and synchronous-disposal wait-graph requirements still apply to
  one key. Spare-worker guarantees for other keys and shared reservation topology do not.

## First-slice pseudocode

- [First-slice pseudocode](03-program-design.md) proposes the existing smart-card
  open/request/close lifetime for one key, with necessary transaction-release coordination
  and a small set of controlled lifecycle tests. The user approved it on 2026-09-22.
- The full [earlier program design](addenda/earlier-multi-key-program-design.md) is archived.
  Its review and findings remain useful; it is not the current implementation checklist.
- [Multi-key addendum](addenda/multi-key-capacity.md) records deferred scheduling,
  capacity, cross-key isolation, and measurement work. Do not implement it during the
  first iteration merely because the earlier review passed.
- The user allowed any attached key for the read-only check. The test selected the
  authorized 5.7.4 USB-C key (K1), verified real metadata, and passed three connection
  cycles with two transaction/read cycles each. Other keys were not acceptance fixtures.
- Review corrected pre-open lease/reservation plumbing, internal smart-card visibility,
  native-release versus cleanup-error results, finite cleanup saturation, finalizer roots,
  and discovery quarantine waking connection waiters without releasing unsafe ownership.
- Reuse relevant findings without inheriting shared pools, credits or unnecessary types.
  Resolve routine details against failing tests and surrounding conventions during the slice.

## Incremental delivery

Retain public applet/raw access, nonblocking native execution, teardown release evidence,
and tests beneath production adapters. Re-slice around one key's complete lifecycle
instead of building the earlier shared-capacity foundation first.

The first slice is implemented with 38 passing boundary tests and historical resilience
77 passed. At NativeShims 1.18.0, Core reran with 1,335 passed/3 existing skipped,
PublicApi reran with 22 passed, focused crypto/PreviewSign tests passed, and macOS arm64
Native AOT publish passed without execution. The selected-key hardware and earlier native
discovery smoke predate the structural refactor and dependency upgrade; they are not 1.18.0
hardware/runtime evidence.
Independent cross-vendor review finished PASS WITH NOTES after three rounds. The
broader S0/S0b/S1–S6/X1–X3 graph remains reference; do not dispatch it unchanged or finish
its detailed specification before starting the approved slice. Subsequent details are
deferred to implementation effort with evidence mapped to the master as work lands.
Known limits: unproven native close errors retain ownership until process exit; Windows/
Linux and interruption/touch hardware scenarios remain unverified. The hardware run's
discovery initialization skipped the non-selected 5.4.3 key on an unresolved HID read;
recorded for follow-up, not silently treated as multi-key success. The original review
finished PASS WITH NOTES; the later structural refactor centralized slot selection and
registration-owning smart-card opening. Remaining low-priority notes concern diagnostic
detail, the internal thread-start test seam and child-kill diagnostics.
The built-in `SmartCardConnectionFactory` is sealed with a parameterless constructor and
`CreateDefault()`; custom factories use `ISmartCardConnectionFactory`. Smart-card logging
uses the single static `YubiKitLogging` source rather than a factory-specific logger.
The S0 intelligence pass has source/artifact findings and existing-test results;
S0 is not complete; subsequent atomic ISC-49/52 evidence brings the master to 2/72.

## Historical handoff notes (read the current checkpoint first)

- Worktree: `/Users/Dennis.Dyall/Code/y/worktrees/yubikit-async-boundaries`.
- Branch: `yubikit-async-boundaries`; smart-card commit
  `db7a1bf64b7c5b4915565eca56273ce2bda15e57` plus uncommitted macOS FIDO route,
  benchmark tooling, async transaction, macOS OTP and `.3` pin; selected-key FIDO evidence
  consumed `1.18.1-async.2`, not the current `.3` declaration. Its fetched base was
  `a7f2cae8c32ad6e0ada55e404f85442f6a266f6c`, matching `origin/yubikit` at the time.
  Refresh before further work and revalidate affected evidence if the base changes.
- One architect/orchestrator owns planning, inventory acceptance, measurements,
  integration, and master acceptance. Engineers own implementation; shared-file
  ownership and handoffs are explicit. Final cross-applet/Core consistency is mandatory.
- Follow D30 for in-scope internal execution; escalate material scope changes, unresolvable
  blockers and destructive actions, not every helper, test or routine package decision.
- Initial iteration scope is one selected physical key at a time; the prior simultaneous
  three-key target is deferred. D28 selects modern macOS APIs within the current .NET 10
  upstream-supported matrix; it is not a YubiKit hardware claim or perpetual future-floor
  promise. Verified firmware, other platform/reader fixtures, frozen numerical budgets,
  and exact 1.18.0 package-producing provenance remain open. Preserve evidence grades;
  managed test passes and binary inspection do not establish hardware behavior.
- The first smart-card lifetime iteration and its planning/evidence documents were
  committed together at `db7a1bf6`.
- The smart-card lifetime lessons are reconciled; its later async transaction increment is managed-tested but the selected PC/SC integration failed sharing violation. The macOS FIDO production milestone has `.2` selected-key normal and pending-read shutdown/reopen results but remains underway. D30's bounded fit pass retained the nested owner; named return codes, per-owner capacity, format quarantine and comment trimming are complete, review PASS WITH NOTES; 17 focused post-cleanup managed tests ran at the historical 1.18.0 pin and 17 later focused tests passed with the preview. Touch/removal and interrupted shutdown remain outstanding. MacOS OTP GET/SET has managed tests only. No shared slot interface or scheduler.
- Read the current scope/architecture, first-slice pseudocode and master decisions;
  resume the pending slice checkpoint under the user's incremental workflow. Do not
  restart whole-effort gates or repeat settled D15/D16 choices.
- Before replacing a retained broader gate document with its reduced-scope successor,
  archive the complete earlier version and update the addendum's reference links.
- Revised Gate 2's independent review passed after clarifying bounded transaction-release
  intent, per-route abort limits, borrowed-buffer ownership and synchronous reentrancy.
  This is proposal review only; no native/hardware or implementation evidence was added.
- D29's synthetic increment and D30's empty-host attempt are historical; the later
  selected-key read-only probe passed. Touch/removal, before/after and release evidence
  remain pending; unrelated platform work need not wait. No whole-epic gate is auto-passed.
