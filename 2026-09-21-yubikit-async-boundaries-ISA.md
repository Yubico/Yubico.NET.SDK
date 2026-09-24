---
task: "Make YubiKit native async boundaries predictable and verifiable"
slug: 20260921-yubikit-async-boundaries
project: Yubico.YubiKit.NET and Yubico.NativeShims
effort: E4
effort_source: auto
phase: execute
progress: 16/72
mode: interactive
started: 2026-09-21T16:01:47Z
updated: 2026-09-24
---

# YubiKit async boundaries — PRD / ideal state artifact

**Status (2026-09-24):** Pinned private-published `1.18.1-async.8` is source-bound to native `71a23cd0`; packaged consumers and fresh private-feed restores passed. The committed expert IO/feature facades share typed connection owners. The macOS topology-listener slice, committed at `69946394`, retains each manager/callback generation until its run-loop thread finishes and native cleanup succeeds; independent review passed after the concurrent-stop fix. **ISC-5, ISC-31, ISC-32, ISC-38, ISC-39, ISC-41, ISC-43–49, ISC-51, ISC-52 and ISC-60 remain checked (16/72; 56 pending).** Six controlled listener-lifetime tests and four existing on-host listener integration tests do not observe actual arrival/removal callback drain; ISC-33 remains unchecked. ISC-53 (all public synchronous waits), ISC-56 (all migration diagnostics) and universal ISC-4 also remain open. No new touch, unplug or hardware-topology mutation is claimed. Windows/Linux native/hardware routes remain deferred pending user direction. The earlier `.8` current-profile dataset predates the facades and listener change; ISC-62 remains open. [Current status](docs/plans/yubikit-async-boundaries/00-status.md) records grades and limits; dated evidence below remains historical.

**Purpose:** Define the behavior, boundaries, staffing, migration slices, and evidence needed to make cross-platform asynchronous device operations reliable. It also defines later, separate evaluations of Windows WinRT SmartCard and Apple CryptoTokenKit.

**Current implementation:** worktree `/Users/Dennis.Dyall/Code/y/worktrees/yubikit-async-boundaries`, branch `yubikit-async-boundaries`. Listener-generation cleanup is committed at `69946394`, expert feature ownership at `db378ffc`, and expert input ownership at `4f361504`; OTP transport-fault recovery and public lifetime documentation at `efde3ef0`, and migrated-operation profiles and portable boundary controls at `add0dc73`. Native source `71a23cd0269c968c1d9420eddb2e2fec71e0cc29` produced private-published `.8` via workflow 35955737172. Native commits `541cfb09` and `760d0416` add verification and contract documentation only; they did not produce `.8`. The earlier `.7`, pre-slice baseline `a7f2cae8`, and demo baseline `99e082c4` remain historical.

**Post-replug verification:** the single selected-key PC/SC async transaction/read/reopen
integration test now passes (one test, no skips). During the same initialization, three
macOS OTP interfaces still returned `IOHIDDeviceOpen = 0xE00002E2`, Apple's
`kIOReturnNotPermitted`. Input Monitoring access is a plausible host cause, not established
as the cause for this test process. No settings changed. This upgrades S4's selected-key
hardware evidence only; other PC/SC platforms, OTP hardware, and full production closure
remain pending. See `docs/plans/yubikit-async-boundaries/00-status.md` for the active board.

**Later all-key visibility check:** after a terminal restart and another replug, three
physical keys were discovered, each advertising smart-card, FIDO, and OTP interfaces.
The typed-connect test that passed while only one smart-card device was visible did not
exercise OTP. On the all-key retry, an OTP identity read exceeded its two-second budget
while native ownership remained active; the registry retained that claim, and the test
failed on a subsequent open with `UnrecoveredConnectionException`. An isolated selected
OTP GET probe likewise stopped at discovery before submitting GET. Other OTP discovery
reads did execute, so the earlier access-denial result is historical, not the current
diagnosis. Selected-key FIDO still passed five nonmutating native-AOT scenarios on
serial 20260533. Native OTP completion, recovery, and safe claim release remain to be
established before treating this as an accepted OTP route.

**OTP late-completion follow-up:** in an isolated selected-key process, serial 20260533's
abandoned OTP discovery read released its claim after about 1.3 seconds; three subsequent
read-only feature GET/open/dispose cycles passed. Serial 31683481 completed three cycles
without a delayed claim. A deterministic test now proves that a timed-out OTP discovery
read rejects another connection until native GET returns and the original lease is
released (16 focused OTP tests pass). After the regression, Core 1,380 passed/3 skipped,
YubiOtp 180 passed and documentation checks passed. The two-second wall-clock budget
was not increased. Feature SET, touch, physical removal and permanently hung-native
recovery remain unverified; this evidence does not satisfy a whole-platform criterion.

**Current native dependency:** `Directory.Packages.props` pins private-published `Yubico.NativeShims` `1.18.1-async.8`, attested package SHA-256 `c85c56f7a41c6999b48b1a5fdcd82c56fbfb3e5ee6ff18ccc64a41144f760403`. [Workflow 35955737172](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/35955737172) at native source `71a23cd0269c968c1d9420eddb2e2fec71e0cc29` passed all seven packaged consumers, native AOT and private publish. Fresh private-feed restore passed in an empty cache after the repo source key `YubicoInternal` was renamed `Yubico_GH` to match the existing credential name at the same URL; no credentials changed. Parent normal `dotnet toolchain.cs restore` passed 43 projects. This does not prove Windows HID driver behavior. `.7`, `.3`, `.2` and 1.18.0 limitations below are historical.

**Reading guide:** Requirements and acceptance criteria precede design and delivery details. Features contains contracts, platform direction, the single-orchestrator workflow, the 11-slice graph, ownership, and dispatch/result packet rules. Decisions distinguishes adopted direction from choices that must be settled before affected slices. Verification separates current intake evidence from historical handoff claims. This document is the one master acceptance plan; the generated boundary manifest/coverage report is its executable evidence input, not a competing source of acceptance truth. Do not create parallel agent specification or report systems.

**Current workflow:** [Status](docs/plans/yubikit-async-boundaries/00-status.md) records approved product/architecture, the completed [first slice](docs/plans/yubikit-async-boundaries/03-program-design.md), and the next affected-route checkpoint. The user delegates ISA/planning progression to the orchestrator and Engineers: the orchestrator owns this master, evidence interpretation, criteria and proposed route sequence; Engineers implement bounded approved slices. D14 remains binding: material signatures, visibility, seams, native-platform changes, architecture or safety decisions are presented to the user for approval in an affected-route walkthrough before implementation. Delegation grants no self-approval authority. Non-blocking details are **deferred to implementation effort**. The [earlier architecture](docs/plans/yubikit-async-boundaries/addenda/earlier-multi-key-architecture.md), [earlier program design](docs/plans/yubikit-async-boundaries/addenda/earlier-multi-key-program-design.md) and [multi-key addendum](docs/plans/yubikit-async-boundaries/addenda/multi-key-capacity.md) retain broader historical research and are not the active checklist. This master retains all epic criteria and evidence.

## Problem

The SDK exposes asynchronous device operations, but a `Task`-returning signature does not prove that the initiating thread remains responsive. Current HID wrappers execute synchronous native report input/output before returning completed tasks. PC/SC uses per-call thread-pool offloading while transaction acquisition schedules work and then synchronously waits. Initialization, callbacks, cancellation, discovery, and teardown add boundaries with distinct lifetime requirements.

Existing ownership rules remain essential: `ExchangeGuard` refuses overlapping logical exchanges and drains admitted work; `DisposalGate` shares teardown completion; the device registry retains physical-interface ownership; user-presence notifications have paired request/resolution semantics. Modernization must preserve these guarantees and visible protocol flow rather than merely adding `await` or a universal command hierarchy.

The main engineering gap is traceability. No independently checked inventory connects native calls, callbacks, blocking waits, scheduling sites, and synchronous escapes to execution ownership, cancellation behavior, lifetime, tests, and platform evidence. Managed mocks and Native AOT link success do not prove native completion, callback quiescence, or hardware behavior.

### Current observations to revalidate during S0

| Area | Current source evidence | Planning observation |
|---|---|---|
| FIDO HID | `FidoHidConnection.cs:43,56` | Async methods call synchronous report methods. |
| FIDO initialization | `FidoHidProtocol.cs:50` | Configuration synchronously waits for initialization. |
| OTP HID | `OtpHidConnection.cs:44,57` | Async methods call synchronous report methods. |
| HID contract | `IHidConnection` | The lower-level contract is synchronous; ISC-50 must enforce the agreed surface after evolution. D15 permits breaking changes/internalization; D16 retains the distinct public raw connection operations. |
| Windows HID | `HidDDevice.cs:155,183,222` | Null overlapped pointers and `FILE_FLAG.NORMAL` are used; OTP zero-desired-access behavior must remain distinct. |
| macOS HID | `MacOSHidIOReportConnection.cs:100-113,216-219` | Input waits pump the caller run loop; removal callback is empty. |
| Linux HID | `LinuxHidIOReportConnection.cs:41,63,96` | Opens with `O_RDWR`; read and write are synchronous. |
| PC/SC | `UsbSmartCardConnection.cs:98,102` | Uses `Task.Run` followed by a synchronous wait for transaction acquisition. |
| Discovery | `ProtocolDeviceInfo.cs:399` | Uses an async `LongRunning` worker, which does not preserve dedicated-thread ownership after an incomplete await. |
| Existing PC/SC seam | `ISCardApi` | Covers discovery/status APIs only, not the complete connection/transmit/transaction lifecycle. |
| Existing API convention | `AsyncSurfaceConventionTests` | Enforces public session shape, not nonblocking method bodies. |
| Existing resilience scan | `RuntimeResilienceStaticScanTests` | Supplies useful source-gate patterns but is line-oriented, not semantic boundary analysis. |
| CI | `.github/workflows/build.yml`, `.github/workflows/native-aot.yml` | Main build is Ubuntu; recurring Native AOT is `macos-latest`/`osx-arm64` and performs no-hardware discovery only. |
| Documentation inventory leads | `docs/migration/v1-to-v2-gaps.md:356`, `docs/v2-highlights.md:38` | The first says a synchronous-wait grep is empty despite current Core findings; the second says “Async all the way down.” Both require full-context review before classification, not automatic deletion. |

The affected consumers are user-interface applications, services, command-line applications, raw-connection users, and transport maintainers. They need trustworthy completion and ownership contracts, not a promise that no operating-system thread ever blocks.

## Vision

A developer can await a YubiKey operation without learning operating-system scheduling quirks. Cancellation and disposal preserve protocol state and memory lifetime, failures state whether the connection remains usable, and maintainers can inspect one generated boundary coverage report showing every relevant boundary's classification and actual evidence level. Equivalent semantics across Core and all applets follow one developer's conventions; platform-required differences are explicit and justified.

## Out of scope

This migration does not add synchronous applet facades, permit overlapping application exchanges, replace the protocol architecture with actors, rewrite cryptography or discovery identity, change prompting policy, or make local encoding/parsing/computation asynchronous. It does not create an operation-specific command hierarchy or a generic universal native-operation hierarchy.

The project does not promise instantaneous cancellation of an in-flight APDU, rollback of device mutation, bounded disposal of an arbitrarily hung driver, or thread-free input/output on every platform. It does not mandate a helper process, direct USB ownership, `io_uring`, `epoll`, or `TimeProvider` changes outside boundaries that actually own delays or deadlines.

WinRT SmartCard and CryptoTokenKit remain exploration milestone X after production milestone P. A tested recommendation is in scope; changing a shipping default requires a later promotion scope. X staffing is deliberately deferred until P closes.

## Principles

- **Observable behavior outranks signature shape.** Fast completed tasks are valid; unpredictable hardware waits before return are not.
- **Native truth determines execution.** Use completion/readiness when verified; isolate genuinely blocking APIs in bounded owned execution.
- **Completion is an ownership promise.** When the public operation completes, the SDK no longer accesses borrowed inputs.
- **Cancellation is a protocol event, not a cleanup shortcut.** Caller intent, native completion, recovery, and safe reuse are distinct.
- **Safety and liveness are separate.** Retaining resources around a hung call may be required for safety without making it recoverable.
- **Use the lowest useful boundary.** Adapt blocking native calls first; batch only when measurements justify it.
- **Keep wire flow visible.** Shared scheduling/lifetime utilities stay mechanical and applet protocol code stays flat.
- **Evidence has levels.** Documented, implemented, managed-tested, native-runtime-tested, Native-AOT-tested, and hardware-tested are different claims.
- **Make omissions observable.** New native calls, wrappers, scheduling sites, and synchronous escapes fail inventory or registration gates until classified.
- **One plan, one integrator, one style.** The orchestrator decides inventory completeness and acceptance; engineers implement owned slices against frozen shared contracts.

## Constraints

1. Target the repository's .NET 10 / C# 14 SDK and supported desktop systems. S0 records exact operating-system floors, architectures, PC/SC versions, and package RIDs. Initial recurring targets are `win-x64`, `linux-x64`, and `osx-arm64`; this is not a claim that other declared RIDs are unsupported.
2. Preserve one-live-connection/one-live-session ownership and logical-exchange overlap refusal. Executor queueing never grants public concurrency.
3. Preserve APDU chaining, SCP state, FIDO keepalive/cancellation, OTP recovery, CRC validation, and exact user-presence request/resolution pairing. Cancellation cannot strand framing and silently permit reuse.
4. Preserve Native AOT compatibility with owned callbacks/buffers and runtime execution. Link success or empty discovery is insufficient.
5. Preserve sensitive-buffer zeroing and static `YubiKitLogging` conventions. Log identifiers, sizes, timings, states, and error codes only; never payloads or secrets.
6. SDK internals do not depend on an application dispatcher, synchronization context, or executor. Application callbacks remain caller-owned and obey existing prompt contracts.
7. NativeShims changes are allowed only after provenance and a compatible native base are verified. Record SDK revision, native source revision, package identity, RID, and artifact evidence even when both lineages share one upstream. Define ABI before consumption and serialize export/packaging ownership.
8. Breaking v2 public API changes and internalization are authorized under D15. Equivalent applet concepts must retain a consistent public grammar; Core types need a deliberate consumer use case to remain public. D16 explicitly retains raw sessions and raw connection operations as public supported use cases. Concrete signatures, visibility boundaries, and architecture still require presentation to the user before implementation under D14. Update public baselines/conventions and migration guidance together; there is no requirement to retain legacy signatures or build compatibility adapters solely to preserve the current alpha surface. Possible later exposure of other Core types is not a current extensibility commitment.
9. Implementation follows root/module `CLAUDE.md`, `docs/SDK-HOUSE-STYLE.md`, and repository wrappers. Use `dotnet toolchain.cs`, keep protocol flow flat, and read every affected module instruction.
10. This worktree planning document is planning state, not implementation evidence. The orchestrator alone edits its acceptance/evidence state. Engineers return evidence packets; summaries never authorize checkmarks.
11. One orchestrator owns planning, independent inventory/classification decisions, budgets, baseline/final measurement interpretation, file ownership, shared-contract decisions, dependency changes, integration, and master acceptance. Engineers implement runtime, test/harness/scanner/measurement tooling, and documentation in their assigned slices. One foundation engineer owns shared code; the orchestrator does not implement it.
12. No two engineers edit the same file. Shared runtime/protocol surfaces, factories, interfaces, public baselines, harness/registry, toolchain, packaging, and workflows have one serialized foundation/integration owner. Platform lanes receive exclusive globs, including native subdirectories. The macOS engineer owns its approved native bridge in a separate native worktree.

## Goal

Deliver cross-platform device input/output whose supported asynchronous entry points remain responsive during native waits while completion, cancellation, protocol recovery, and release obey explicit tested contracts. Prove completeness through independent semantic discovery, registered production-adapter profiles, native and Native AOT execution, hardware evidence, and cross-layer consistency review. Evaluate alternative smart-card APIs later without silently promoting them.

## Criteria

All criteria began unchecked; ISC-5, ISC-31, ISC-32, ISC-38, ISC-39, ISC-41, ISC-43–49, ISC-51, ISC-52 and ISC-60 are now verified below (16/72). **Production milestone P is ISC-1 through ISC-64; exploration milestone X is ISC-65 through ISC-72.** P may ship without X, but must never be reported as all 72 criteria complete. Universal criteria are parameterized by the explicit operation/adapter registry; a missing row fails verification. Slices may contribute evidence to the same criterion, but no early slice claims universal acceptance.

**Iteration 1 is a narrower delivery step, not a redefinition of P or X.** Establish
the single-key lifecycle first and map its required operation/platform rows in the
revised gates. Cross-key reservation, saturation, scale and isolation evidence remains
deferred, not passed or silently marked inapplicable. Do not check universal criteria
or declare full production closure from single-key evidence. Same-key overlap, callback,
recovery, removal-generation and monitor-context guarantees remain mandatory.

### Boundary inventory and enforcement — P

- [ ] ISC-1: Every discovered native entry point or callback-registration boundary in the shipping managed/shim scan set has a manifest disposition.
- [ ] ISC-2: Every discovered blocking-wait or work-scheduling site in shipping managed code has a manifest disposition.
- [ ] ISC-3: The manifest resolver reports zero entries referring to nonexistent managed symbols or native exports.
- [ ] ISC-4: Every required adapter-operation row has a registered applicable contract-test profile.
- [x] ISC-5: The inventory gate rejects a fixture containing an unclassified native entry point.
- [ ] ISC-6: The boundary checker rejects a fixture reaching a known blocking native operation through an unapproved direct or interface wrapper on an async caller path.
- [ ] ISC-7: Every inventory exception resolves to a current justification and named review owner.
- [ ] ISC-8: Every platform-capability record distinguishes documented, implemented, and verified evidence states.

### Observable operation contracts — P

- [ ] ISC-9: Every registered async entry probe returns its task before a deliberately withheld native operation is released.
- [ ] ISC-10: Cancellation winning before native dispatch results in zero native submissions for that operation.
- [ ] ISC-11: Instrumented borrowed input memory records zero SDK accesses after its public operation task reaches a terminal state.
- [ ] ISC-12: Cancellation of submitted native work does not release native operation resources before terminal native completion.
- [ ] ISC-13: Each accepted operation publishes exactly one terminal managed completion across prescribed completion/cancellation/removal races.
- [ ] ISC-14: A second overlapping logical exchange is refused under the existing admission contract.
- [ ] ISC-15: Exchange ownership is not released while recovery or response draining remains pending.
- [ ] ISC-16: A cancelled FIDO ceremony sends at most one `CTAPHID_CANCEL` at a protocol-permitted point after full request transmission.
- [ ] ISC-17: A recovered cancelled FIDO ceremony permits a subsequent correctly correlated exchange.
- [ ] ISC-18: A cancelled OTP operation reaches the documented recovered-or-unusable state.
- [ ] ISC-19: An admitted SCP-protected chained exchange preserves valid MAC/chaining state for the next exchange when reuse is reported safe.
- [ ] ISC-20: Anti: an operation with ambiguous device-side completion is never automatically replayed as recovery.

### Scheduling, buffering, and callback contracts — P

- [ ] ISC-21: The blocking executor never exceeds its configured active-worker limit under saturation.
- [ ] ISC-22: Pending blocking submissions never exceed the configured pending-work limit under saturation.
- [ ] ISC-23: Saturating discovery capacity leaves the configured interactive reservation available.
- [ ] ISC-24: The blocking-executor API and static gate reject asynchronous delegates as worker jobs.
- [ ] ISC-25: A capacity slot held by executing native work is released only when that call returns.
- [ ] ISC-26: Native completion/report callbacks execute no application callback or application continuation inline.
- [ ] ISC-27: Overflow of an SDK-owned inbound report buffer faults its connection instead of dropping reports.
- [ ] ISC-28: Completion from a retired connection generation cannot complete a replacement-generation operation.

### Platform-specific behavior — P

- [ ] ISC-29: Windows FIDO report input/output uses the verified overlapped handle/completion path.
- [ ] ISC-30: Windows cancellation tests observe terminal overlapped completion before reclaiming operation storage.
- [x] ISC-31: macOS HID input waits use a persistent native event-delivery owner rather than the caller's run loop.
- [x] ISC-32: Each macOS output/feature-report direction uses a verified callback API or explicitly classified bounded-executor fallback.
- [ ] ISC-33: macOS teardown reaches native quiescence acknowledgment before callback context/device storage is reclaimed.
- [ ] ISC-34: Linux HID input waits use nonblocking reads with readiness notification.
- [ ] ISC-35: Linux HID writes and feature-report ioctls that may block never execute on the shared read reactor.
- [ ] ISC-36: Linux readiness cancellation/removal wakes the registered wait without polling-delay dependence.
- [ ] ISC-37: Each registered PC/SC lifecycle operation uses its classified owner rather than an incidental caller-thread wait.
- [x] ISC-38: Anti: portable APDU cancellation does not depend on `SCardCancel` interrupting `SCardTransmit`.
- [x] ISC-39: Cancelling a PC/SC monitoring wait does not cancel or release an unrelated connection's native context.
- [ ] ISC-40: Windows OTP feature reports retain verified zero-desired-access keyboard-collection opening behavior.

### NativeShims and packaging — P

- [x] ISC-41: The integration record identifies the NativeShims source revision that produced the consumed package.
- [ ] ISC-42: ABI compatibility checks pass for the declared old/new NativeShims consumer matrix.
- [x] ISC-43: Every new asynchronous shim submission obeys its submission-versus-callback completion contract.
- [x] ISC-44: New shim operations retain borrowed native-pointer lifetimes through documented native completion.
- [x] ISC-45: A shim cancellation request does not itself destroy the operation or callback context.
- [x] ISC-46: Shim shutdown acknowledgment establishes that no future callback can access released context.
- [x] ISC-47: Release artifacts expose none of the test-only fault-injection exports.
- [x] ISC-48: A clean packaged consumer resolves the pinned NativeShims artifact on each required verification RID.

### API, compatibility, and interaction — P

- [x] ISC-49: Existing public async-surface convention tests pass for the shipping applet APIs.
- [ ] ISC-50: A reviewed `IHidConnection` evolution decision is enforced by its consumer compatibility test.
- [x] ISC-51: Transaction acquisition has an async path passing the withheld-native-completion responsiveness probe.
- [x] ISC-52: Production FIDO configuration does not synchronously wait for asynchronous channel initialization.
- [ ] ISC-53: Every retained synchronous wait reachable from public API is an explicit boundary with a verified drain contract.
- [ ] ISC-54: Public operation documentation states the applicable cancellation and borrowed-memory lifetime contract without contradicting implementation evidence.
- [ ] ISC-55: Existing user-presence pairing/outcome tests pass through migrated transports.
- [ ] ISC-56: Anti: migration diagnostics emit no raw command, response, PIN, key, or credential payload.

### Delivery evidence — P

- [ ] ISC-57: Required managed boundary-contract tests execute with nonzero counts on Windows, Linux, and macOS CI.
- [ ] ISC-58: A Native AOT host executes required native completion/callback paths on `win-x64`, `linux-x64`, and `osx-arm64`.
- [ ] ISC-59: Every required S0 hardware scenario has a passing result for the accepted revision pair.
- [x] ISC-60: A deliberately restored synchronous HID wait causes the responsiveness gate to fail.
- [ ] ISC-61: Affected applet/protocol regressions pass after each adapter migration and after each required consistency refit.
- [ ] ISC-62: Baseline/final evidence records caller-return latency, native duration, recovery duration, worker and pending counts, allocation, and idle activity for agreed scenarios.
- [ ] ISC-63: Existing docs/examples and separately inventoried still-active demo-branch async claims match completed evidence; absence of `docs/demo` on the integration base is recorded rather than treated as a merged demo.
- [ ] ISC-64: The production closure report contains no required row marked unclassified, outstanding, or evidence-pending, and its mandatory consistency row records zero unresolved migration-induced cross-layer drift. Pre-existing unrelated style drift may be excluded only when identified in an S0 baseline row with rationale and no missing required behavioral or native evidence.

### Future smart-card API exploration — X

- [ ] ISC-65: An isolated Windows WinRT SmartCard prototype records a raw APDU exchange result.
- [ ] ISC-66: The WinRT prototype produces an evidence-backed session/transaction/error/cancellation compatibility report.
- [ ] ISC-67: The WinRT prototype records deployment and Native AOT feasibility for proposed hosts.
- [ ] ISC-68: An isolated CryptoTokenKit prototype records a raw APDU exchange result.
- [ ] ISC-69: The CryptoTokenKit prototype produces an evidence-backed session/exclusivity/SCP compatibility report.
- [ ] ISC-70: The CryptoTokenKit prototype records entitlement, host-process, and Native AOT feasibility.
- [ ] ISC-71: A comparison decision records promote/defer/reject recommendations for both backends using existing contracts.
- [ ] ISC-72: Anti: completing exploration does not change the default backend without a separate promotion scope.

## Test strategy

### Evidence layers and semantics

1. **Managed deterministic contracts:** controlled seams beneath production adapters; barriers/completion sources, not sleeps. Watchdogs protect the test process but are not responsiveness assertions.
2. **Native harness:** actual P/Invoke, calling convention, overlapped/callback completion, pointer and buffer lifetime, cancellation, and quiescence. A no-hardware harness does not claim driver coverage.
3. **Native AOT execution:** publish and run required paths per RID. Link-only and empty-discovery runs do not satisfy ISC-58.
4. **Hardware acceptance:** representative FIDO, OTP, and PC/SC USB/NFC scenarios on recorded operating system, driver, reader, and firmware configurations. Operator-assisted and unattended results remain distinct. Unavailable evidence is mandatory to record and remains pending; every required pending row blocks production closure, while it does not block independent engineering whose prerequisites are met.
5. **Regression and consistency:** preserve exchange, disposal, ownership, prompting, and byte-level tests. For changed code and callers/contracts affected by the migration diff, compare Core/protocol/native and relevant Management, Piv, Fido2, WebAuthn, Oath, YubiOtp, OpenPgp, SecurityDomain, and YubiHsm siblings for naming/type placement, visible wire flow, cancellation/exception/lifetime ownership, `ConfigureAwait` convention, exact prompt pairing, memory/zeroing/logging, tests, and docs. This is a scoped review, not a blanket whole-repository style sweep.

The entry registry includes opening/factories, initialization, typed raw operations, one representative applet operation per adapter route, transaction acquisition, retained synchronous lifecycle routes, and `DisposeAsync`, including synchronous prefixes before the first incomplete await. Fast paths may complete immediately.

The responsiveness probe runs on a dedicated context-bearing thread, withholds native completion, and requires invocation return before release. Cleanup always releases controlled fakes. Irrecoverably hung native work is tested in a disposable process. Borrowed-memory tests instrument access after terminal completion; native lifetime tests use retained buffers and counters rather than crash-as-assertion.

### Criterion probes

Every row is proposed work, not an existing passing test. Parameterized profiles cannot skip an unregistered adapter.

| ISC | Probe and required result |
|---|---|
| ISC-1 | Semantic native/callback discovery: zero undispositioned sites. |
| ISC-2 | Wait/scheduling discovery: zero undispositioned sites. |
| ISC-3 | Symbol/export resolution: zero stale records. |
| ISC-4 | Registry coverage: zero missing required profiles. |
| ISC-5 | Unknown native import/export fixture makes the gate fail. |
| ISC-6 | Direct and interface wrapper fixtures reaching known blocking leaves make the gate fail. |
| ISC-7 | Exception validation: every exception has a current reason and owner. |
| ISC-8 | Capability schema keeps documented, implemented, and verified states separate. |
| ISC-9 | Withheld native completion: invocation returns before release. |
| ISC-10 | Predispatch cancellation: native submission count is zero. |
| ISC-11 | Borrowed-memory instrumentation: no accesses after public terminal state. |
| ISC-12 | Submitted-work cancellation: no premature resource release. |
| ISC-13 | Deterministic races: exactly one managed terminal completion. |
| ISC-14 | Existing overlap suites observe the current refusal contract. |
| ISC-15 | Held recovery/drain retains exchange ownership. |
| ISC-16 | Repeated FIDO cancellation yields at most one legal cancel frame. |
| ISC-17 | Cancel, drain, next command preserves channel correlation. |
| ISC-18 | OTP cancellation reaches recorded recovered-or-unusable disposition. |
| ISC-19 | Cancelled SCP chain validates next exchange whenever reuse is reported safe. |
| ISC-20 | Ambiguous completion never increases mutation submission count above one. |
| ISC-21 | Executor saturation never exceeds configured active count. |
| ISC-22 | Executor saturation never exceeds configured pending count. |
| ISC-23 | Discovery saturation leaves interactive reservation usable. |
| ISC-24 | Compilation/static fixture rejects async worker delegates. |
| ISC-25 | Cancelled caller does not release capacity before native return. |
| ISC-26 | Native callback stack invokes no application code/continuation inline. |
| ISC-27 | Receiver overflow faults the connection. |
| ISC-28 | Retired-generation completion cannot complete replacement work. |
| ISC-29 | Windows trace and hardware evidence show overlapped FIDO reports. |
| ISC-30 | `CancelIoEx` harness observes completion before free. |
| ISC-31 | macOS input harness observes no caller-run-loop pumping. |
| ISC-32 | Per-direction macOS profiles identify verified callback or bounded fallback ownership. |
| ISC-33 | macOS shutdown harness observes quiescence before free. |
| ISC-34 | Linux harness and hidraw device evidence show readiness-driven nonblocking reads. |
| ISC-35 | Stalled Linux output cannot stall input-reactor progress. |
| ISC-36 | Cancellation/removal explicit wake terminates readiness wait. |
| ISC-37 | PC/SC lifecycle profiles withhold every native call and observe its classified owner. |
| ISC-38 | Policy audit/test proves no portable transmit-abort reliance on `SCardCancel`. |
| ISC-39 | Monitor cancellation leaves independent connection context active. |
| ISC-40 | Windows OTP hardware exchange succeeds through zero-desired-access opening. |
| ISC-41 | Release job/package attestation resolves the currently consumed package to immutable source (not the historical 1.18.0 artifact). |
| ISC-42 | Declared SDK/shim compatibility combinations pass. |
| ISC-43 | For the five new macOS input-bridge exports, source and synthetic lifecycle tests enforce one persistent registration per owner (not one submission/callback per report); callbacks may start before `Start` returns, accepted reports deliver once in order, terminal occurs at most once, and shutdown acknowledges and drains. Future async callback operations still require their own contract. |
| ISC-44 | Source/Apple callback-lifetime audit, synthetic destroy-BUSY and drain/close-attempt tests, pre-commit identical-runtime-source AddressSanitizer tests, and version-specific real removal evidence substitute for the proposed but unimplemented retained-buffer counters on the new macOS bridge. |
| ISC-45 | Cancellation without completion does not destroy operation/context. |
| ISC-46 | Native instrumentation records no callback-context access after quiescence acknowledgment. |
| ISC-47 | Release export inspection finds zero test-only exports. |
| ISC-48 | Clean packaged consumer loads the pinned artifact per required RID. |
| ISC-49 | Existing public async-surface convention suite passes for shipping applet APIs. |
| ISC-50 | Declared HID consumer compatibility fixture enforces reviewed choice. |
| ISC-51 | Withheld transaction acquisition returns its task before release. |
| ISC-52 | FIDO initialization regression has no synchronous async wait. |
| ISC-53 | Sync-boundary registry has zero unclassified public escapes; drains pass. |
| ISC-54 | Documentation/API policy review finds zero contradiction between public contracts and evidence. |
| ISC-55 | Existing presence request/resolution tests pass on migrated routes. |
| ISC-56 | Sentinel diagnostics contain zero sensitive payload bytes. |
| ISC-57 | CI artifacts show successful nonzero managed counts on all three systems. |
| ISC-58 | Native AOT artifacts show actual required path execution on all three RIDs. |
| ISC-59 | Frozen hardware matrix has zero missing/failing required cells. |
| ISC-60 | Preserved red sensitivity fixture fails when synchronous HID wait is restored. |
| ISC-61 | Focused applet/protocol suites pass after migration and consistency refits. |
| ISC-62 | Baseline/final report contains all metrics with revision/environment provenance. |
| ISC-63 | Claim scan covers current docs/examples and separately recorded active demo claims. |
| ISC-64 | Closure matrix has zero unfinished required rows; its manual consistency row has zero unresolved migration-induced drift, with any unrelated S0 style baseline separate. |
| ISC-65 | Isolated WinRT raw APDU result is reproducible. |
| ISC-66 | WinRT parity matrix has evidence/result for every comparison. |
| ISC-67 | WinRT host/deployment/Native AOT result is captured. |
| ISC-68 | Isolated CryptoTokenKit raw APDU result is reproducible. |
| ISC-69 | CryptoTokenKit parity matrix has evidence/result for every comparison. |
| ISC-70 | CryptoTokenKit entitlement/host/Native AOT result is captured. |
| ISC-71 | Reviewed recommendations cite the comparison evidence. |
| ISC-72 | Shipping factory/package review finds no unscoped backend promotion. |

For the five macOS `Native_HidInput*` bridge exports, the accepted ISC-44–46
evidence method is source/Apple callback-contract audit, synthetic destroy-BUSY,
close-attempt and callback-drain observations under native Release and AddressSanitizer,
and the version-specific real `.6` removal after acknowledgment and drain. Native
retained-buffer counters proposed for ISC-44 were **not** implemented; do not report
them as observed. For ISC-43 the bridge is a persistent registration, not a
per-report command: Create never calls back, Start registers before activation
and may produce callbacks on another queue before returning; zero or many reports
may follow one Start, each accepted report delivers once in order, terminal is at
most once, and WaitShutdown acknowledges cancellation and drains accepted callbacks.
This scoped contract does not waive callback contracts for future operations or
establish cross-platform/native driver acceptance. ISC-47's one-time packaged-export inspection
is not yet a continuous integration gate; ISC-38's source audit is not a future-callsite
scanner. ISC-60's managed sensitivity gate covers the identified legacy macOS open
and OTP report synchronous waits, not every adapter.

Exploration incompatibility is a valid research result, not production readiness. Production behavior requires passing evidence; an explanation of failure is not a pass.

### Existing and proposed verification entry points

```sh
dotnet toolchain.cs build
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~PcscConnectionLifetimeTests"
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~SmartCardConnectionFactoryTests"
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~PcscDiscoveryLifetimeTests"
dotnet toolchain.cs -- test --project PublicApi
dotnet toolchain.cs -- resilience --fast
dotnet toolchain.cs native-aot-contract-qa
```

The three current smart-card boundary classes are intentionally separate and must be run as separate invocations because the toolchain does not support a `|`-joined filter. Future boundary suites may use another reviewed naming convention, but every gate must reject zero matching tests. `native-aot-contract-qa` already exists at `toolchain.cs:216` with its build dependency at `toolchain.cs:241`; it validates static project/opt-in/reference/anchor contracts and is not native runtime evidence for ISC-58. Native runtime commands are defined only after reading the verified package-producing base's instructions.

### S0 budgets and baseline freeze

The list below preserves the broader epic's evidence requirements. For iteration 1,
freeze the selected key's scenarios and relevant per-key limits/baseline; multi-key
pool sizes, spare-cleanup guarantees and simultaneous-device budgets are deferred
under D20 and the addendum. Do not make the entire old reservation framework a
prerequisite for a first working single-key path.

- Freeze required scenarios for FIDO HID request/response and touch cancellation; OTP exchange/touch abandonment; PC/SC USB and supported NFC; cross-process contention; removal during input/output; and disposal during an admitted exchange.
- Record exact systems, architecture, PC/SC/reader drivers, firmware, SDK/native revisions, package, harness revision, and applicability. A missing fixture is pending, not passed.
- Freeze numeric active/pending/report-buffer limits and discovery/interactive capacity separation before implementation comparisons. Test permanent-block exhaustion. P1 provides separation; S4 proves discovery migration and isolation.
- The broader epic additionally requires reserved bounded cleanup/control progress. For the
  deferred multi-key iteration, its affected capacity checkpoint must specify reservation
  topology, supported simultaneous stalls and caller-visible exhaustion without abandoning
  a stalled call's slot or spawning unbounded replacements. Freeze measured values before
  that production change; this obligation is not a prerequisite for the current single-key slice.
- Staff one measurement engineer in S0 to implement only the baseline harness/tooling against the untouched baseline runtime. The orchestrator designs scenarios and metrics, interprets results, and freezes numeric budgets before any production change; the orchestrator does not modify runtime code. This measurement subtask is a sequential predecessor to S0b, which may reuse the harness.
- Freeze performance regression budgets before changes. Baseline measurements happen in S0, not after migration. Keep caller-return, native, recovery, scheduling, allocation, and idle costs separate. Missing required baseline evidence blocks the affected before/after comparison; it is never presumed available.
- Responsiveness is primarily event ordering, not an arbitrary latency threshold. Record distributions as supporting evidence.

## Features

### Single-key first iteration — D20 scope precedence

The immediate target is one physical key's open → initialize → operate → cancel/
recover → dispose → safe-reopen lifecycle. Preserve public applet and both raw-access
tiers, native/borrowed-memory lifetime, overlap refusal, same-key discovery ownership,
prompt pairing, and removal/replug generation isolation. The existing desktop platform
direction and explicit macOS IOKit expansion remain; one key does not imply a new
platform restriction or a process-wide one-device lock.

Defer shared cross-key worker/queue sizing, priority/fairness, origin-separated cleanup
credits, spare cleanup progress for other keys, multi-key stress and simultaneous-device
performance claims. The full prior program design remains a reviewed reference in
the addendum. The reduced architecture must choose only the execution/lifetime
mechanisms it needs, not the entire old design with every count changed to one.

The primitives and 11-slice graph below remain the broader epic design. Under D23,
approve the first-slice pseudocode, then establish concrete details through implementation
and verification; no whole-effort program/slice specification is required. The old
global-capacity portions are not automatically part of iteration 1. Nothing in
this reduction permits releasing unresolved native resources, hiding unbounded work
for the selected key, or downgrading native/hardware evidence into managed-test claims.

### Target boundary

```text
Public session / factory / typed raw operation: Task / Task<T>
  -> synchronous local validation, encoding, parsing, cryptography
  -> asynchronous protocol orchestration
       admission, framing, chaining, SCP, prompts, cancellation, recovery
  -> platform transport owner
       native completion/readiness/callbacks OR bounded synchronous-native executor

NativeShims: ABI/platform adaptation and difficult callback-lifetime bridges only
```

Keep `ExchangeGuard`, `DisposalGate`, `ConnectionSessionGuard`, and `DeviceConnectionRegistry` as distinct starting points. Audit their interactions; do not wrap them in a competing state machine. Await remains where `using`, `finally`, exception timing, zeroing, or ownership requires it.

The concern registry is about observable ownership and flow, not merely whether an API
uses `async`/`await`:

| Concern | Required distinction and owner |
|---|---|
| Execution scheduling | The platform transport owner chooses verified native completion/readiness/callback delivery or the classified blocking owner. This says where native work waits, not who owns the protocol. |
| Control flow and ownership | Existing applet/protocol code and guards continue to sequence the full admitted exchange: framing, chaining, prompts, protocol cancellation/recovery and final disposition. Lifecycle end/shutdown remains with the connection owner; public raw consumers retain their documented framing, exclusion and recovery responsibilities. |
| Cancellation intent | Cancellation winning before submission prevents that submission. After submission it records intent and uses only the route's permitted cancellation point/capability; it is not native completion or permission to release resources. |
| Recovery and drain | The protocol owner drains or reaches a terminal disposition before deciding reusable versus faulted. Recovery is not proof that native resources or the physical claim were released, and an uncertain mutation is never replayed. |
| Concurrency and admission | Competing logical exchanges remain refused, with no ordinary same-key backlog. Independently owned native completion, readiness, callback, monitor and coalesced lifecycle-control progress may overlap only as specified; that does not grant public-operation concurrency. |
| Resource lifetime | Borrowed input ends at public operation completion only after native last use. Operation resources live through native terminal completion; callback contexts live through quiescence; the physical claim lives through positive release evidence or remains quarantined. |

“Control” is overloaded and does not name one queue or execution class. A lifecycle
control intent is the coalesced transaction-end or connection-shutdown request; a native
transport control operation is a feature-report ioctl or equivalent platform call; a wire
control action is a protocol frame such as `CTAPHID_CANCEL`. Caller cancellation may cause
a permitted control action without establishing native abort, terminal completion,
protocol recovery, safe reuse or physical release.

### Minimal primitives

**P1 — bounded blocking native executor.** Foundation-owned, uses owned blocking-worker threads, accepts synchronous jobs only, bounds active and pending work, supports cancellable admission, completes callers asynchronously, and retains capacity until native return. It provides documented discovery/interactive separation and reserved cleanup/control progress for actual consumers: macOS fallback directions, Linux output/control, and PC/SC. Completion/quiescence observation cannot require fresh executor admission; later blocking cleanup may require the bounded reserve and cannot touch an unresolved native handle. It does not run applet callbacks, retry protocol work, or serialize public overlap. S0 freezes numbers including the stalled-cleanup failure budget; S0b implements and tests bounds, predispatch cancellation, sync-only jobs, and capacity retention; S4 verifies discovery use/isolation.

**P2 — owned native operation.** Platform-local where needed, owning real request state, handles, buffers, callback context, cancellation registration, and exactly-once cleanup. Windows and Apple implementations share contract tests, not inheritance. P2 is not mandatory shared foundation.

**P3 — bounded report receiver.** Use only on a platform with a persistent producer, particularly macOS. Deliver an owned input report to a pending reader or bounded FIFO, copying or transferring ownership before a native callback buffer can be reused. Fault on overflow and complete pending readers on terminal removal/error. Returned response data must remain immutable and valid for the existing public lifetime; never return a view of a native buffer that is about to be reused. Do not require a background receiver where one outstanding native read suffices.

**P4 — exchange-owned cancellation/recovery.** Keeps admitted, in-flight, recovering/completing, reusable/faulted state local to the protocol owner. Caller cancellation never impersonates native completion.

**P5 — testable SDK time.** Use `TimeProvider` only for changed SDK-owned delays, backoff, and deadlines. Do not sweep unrelated timing code.

**P6 — inventory and contract registry.** Test/build-time only. Stable boundary IDs identify discovered operations independently of the stable ISC acceptance IDs. Boundary IDs connect semantic discovery, symbols/exports, capability states, profiles, and evidence; ISC IDs state acceptance properties over those registered rows.

### Inventory and executable contracts

The orchestrator independently judges completeness; engineers produce scanner and manifest fragments. Scan shipping managed code semantically and relevant native headers/exports with appropriate native tooling. Cover imports, dynamic lookup, callbacks, open/init, close/dispose, monitoring, discovery, scheduling, blocking waits, and application-callback boundaries. Record excluded projects and reasons.

The S0b semantic scanner resolves actual `Task` blocking symbols such as `Wait`, `Result`, and `GetAwaiter().GetResult()` rather than treating every property named `Result` as suspicious. It recognizes known blocking native leaves and direct/interface wrappers present in this codebase. It includes unknown-import and wrapper negative fixtures and fails when source coverage is empty. Relevant unresolved dynamic/indirect calls become findings. Do not downgrade to regular-expression line scanning or claim whole-program proof. Black-box responsiveness tests and source dependency boundaries remain required.

The generated manifest/coverage report distinguishes `verified`, `implemented/evidence pending`, `intentional synchronous boundary`, `outstanding`, and `not applicable with reason`. It records a stable boundary ID, operation, platform, route, execution owner, admission/lifetime/cancellation/reuse rules, profile, and separate documented/implemented/verified evidence. Unknown sites, stale symbols, absent profiles, and zero-count tests fail gates. Baseline findings acknowledge current defects but are not passed rows. A pre-existing unrelated style carveout must cite its S0 baseline row and cannot exempt required behavioral, native-runtime, Native AOT, package, or hardware evidence.

Minimum manifest schema, illustrative rather than an existing file format:

```yaml
id: PCSC.Transmit.Windows
managed_symbol: UsbSmartCardConnection.TransmitAndReceiveAsync
native_entry: Native_SCardTransmit
platform: windows
operation: transmit
execution: bounded-blocking
admission_owner: PcscProtocol.ExchangeGuard
native_lifetime_owner: PcscNativeOperation
cancellation_before_dispatch: reject-without-submission
cancellation_after_dispatch: drain
reuse_after_failure: protocol-classified
borrowed_input_release: public-operation-terminal
native_buffer_release: native-call-return
shutdown_acknowledgment: native-call-return
contract_profile: PcscTransmit
evidence:
  documented: [platform-source-reference]
  implemented: null
  verified: []
status: discovered
exception: null
```

Names in this example are proposals. Match established repository vocabulary during implementation. Keep a caller-facing deadline, a native timeout, and a teardown watchdog as three separately named and measured concepts; never collapse them into an ambiguous `timeout` field.

Required coverage matrix axes:

| Axis | Required enumeration |
|---|---|
| Platform × transport | Windows HID, macOS HID, Linux HID, Windows smart card, macOS smart card, Linux smart card |
| Report/operation direction | HID: FIDO input, FIDO output, OTP feature GET and OTP feature SET as separate rows. Smart card: PC/SC USB and supported PC/SC NFC lifecycle operations where applicable. |
| Lifecycle | Open/connect, initialize, send/receive/transmit, transaction begin/end where applicable, cancellation/recovery, removal, dispose, monitor start/stop, discovery probe |
| Entry route | Applet/convenience path, typed raw connection path, retained synchronous expert/lifecycle path |
| Evidence | Documented, implemented, managed-tested, native-runtime-tested, Native-AOT-runtime-tested, hardware-tested |

The six platform/transport combinations are coverage keys, not claims of identical native
semantics. The detailed table in Gate 2 A3 is the authoritative architecture view of
their classified execution and current evidence; it does not create criteria. ISC-4,
ISC-8–20, ISC-26–40, ISC-44–46, ISC-54 and ISC-57–64 remain the stable mappings, stay
parameterized by applicable rows, and remain unchecked until their existing obligations
are satisfied.

S0b freezes the shared foundation only after a withheld native seam has been exercised through an actual available-host production adapter, preserving the red sensitivity fixture. This is managed proof, not native or hardware proof. S0b requires no hardware success and cannot pass universal criteria early.

### Shared safety and protocol rules

- Predispatch cancellation that wins the defined admission point prevents submission. After dispatch, resources live until observed native completion; mutations with ambiguous completion are never replayed.
- Public task termination releases borrowed inputs. `WaitAsync`/`WhenAny` abandonment is insufficient when underlying work still uses caller memory.
- Detached discovery owns all memory, handles, slots, exception observation, and generation lifetime; stale epochs cannot republish results.
- `DisposeAsync` returns without unbounded caller-thread waiting; neither sync nor async disposal frees live native storage merely to meet a deadline.
- Finalizers and cancellation callbacks never impersonate completion. Unresolved native work quarantines ownership.
- Quarantined operation state, handles, buffers and callback contexts remain strongly reachable with native references retained after callers drop their connection. Late native completion/quiescence permits safe cleanup and lease release; removal/replug alone does not. Nonterminating native work may retain ownership for process lifetime, with an observable unrecovered state distinct from ordinary in-use refusal. `DisposalGate`'s current lease release on fault must not free an interface whose native work remains unresolved.
- Retained synchronous disposal cannot wait on an owned worker/event thread needed for its completion or run reentrantly inside the exchange it drains. Each affected-route checkpoint traces/tests its wait graph, relevant execution-resource pressure and absence of application-context dependency; dedicated native workers alone do not establish managed-continuation independence from the shared pool.
- Native callbacks/reactors/workers invoke no application callback inline and use asynchronous managed continuations.
- FIDO sends `CTAPHID_CANCEL` only after full request transmission, at most once, while retaining/draining the exchange. Do not cancel the read needed for protocol recovery merely because the caller cancelled.
- Gate 2 approves retaining the current FIDO between-read/keepalive cancellation point. It does not require concurrent send/read support from public raw-connection implementers. A later pending-read control-send design would need explicit public concurrency semantics, consumer tests, documentation updates, and renewed architecture approval; ISC-16's count/ordering rule is not evidence that such concurrency already exists. Register the no-further-keepalive case: the exchange remains admitted until native read termination/removal supplies a terminal disposition, not merely caller cancellation.
- OTP feature report polling/recovery remains protocol-owned; input readiness is not a replacement. Report success only after CRC validation.
- Prompt callbacks dispatch and return rather than wait for human action. Preserve exact request/resolution pairing, exception precedence, and resolution with `CancellationToken.None`.
- SCP/chaining owners retain cryptographic state through recovery and zero sensitive data at the established lifetime boundary.

### Platform implementation direction

**Windows FIDO:** open report handles with `FILE_FLAG_OVERLAPPED` and retain `GENERIC_READ | GENERIC_WRITE` access. Own overlapped state and preserve report-ID normalization: Windows buffer lengths include the report ID while existing SDK payload lengths do not. Validate full expected packet length; partial or malformed reports are not successful packets. Cover reject/failure/immediate/pending completion and `CancelIoEx` races without double completion. Cancellation requests do not free state before terminal completion.

**macOS HID:** expand the existing IOKit usage explicitly. First inspect the current `IIOKitDeviceLifetime` seam and investigate callback viability separately for input, output, and feature reports before introducing another seam. Input uses a persistent event-delivery owner through `IOHIDDeviceSetDispatchQueue` / `IOHIDDeviceActivate` / `IOHIDDeviceCancel` / `IOHIDDeviceSetCancelHandler` across the current .NET 10 upstream-supported macOS versions, never caller-run-loop pumping. Do not add an owned-run-loop fallback solely to preserve an older binary target; any technical fallback needs explicit evidence and approval. Evaluate the exact `IOHIDDeviceSetReportWithCallback` and `IOHIDDeviceGetReportWithCallback` APIs; these report callbacks are distinct from similarly named value APIs and each direction requires verified contract/implementation evidence. Each unverified output/feature direction uses a classified P1 blocking fallback without redesigning the shared contract. Register before activation; copy or transfer reports before callback-buffer reuse under P3, keep returned response memory valid, fault pending readers on removal, and observe both callback quiescence and outstanding report-operation completion before release. The architecture permits a minimal C/Objective-C bridge; its concrete contract and native base still require approval/provenance before the macOS engineer implements it. The unrelated static-linking branch is not the base. Changing native device-matching/discovery shape remains outside this effort.

**Linux HID:** use `O_NONBLOCK` reads with `poll`/`eventfd` readiness and explicit cancellation/registration/shutdown wake-up. Handle `EAGAIN`, `EINTR`, errors, removal, deregistration, and descriptor reuse by generation. Preserve existing numbered and unnumbered report framing in both directions. Potentially blocking writes/ioctls use P1 and never the read reactor. Adopt `epoll` only with measured need.

**PC/SC:** classify establish/connect/reconnect/transmit/begin/end/disconnect/release/status/discovery independently. Use P1 for blocking lifecycle work; do not synchronously wait on caller threads or rely on `SCardCancel` for portable APDU abort. Add async transaction acquisition/release under the prior API decision. Isolate monitoring contexts. Preserve APDU/chaining/SCP drains and SafeHandle lifetime.

**Windows OTP:** preserve zero-desired-access keyboard collection opening. Verify GET and SET feature directions independently; use overlapped control only when proven and P1 otherwise. Select a backend before state-changing work and never fallback-replay ambiguous mutations.

### Native code/package lineage

NativeShims 1.18.0 was the earlier selected signed upstream dependency; its package identity,
signatures, artifact contents and historical consumer checks are recorded below. Current
development instead consumes the explicitly approved local `1.18.1-async.2` preview for
the macOS input bridge. Exact original signed-package producer binding remains pending.
Tag `1.18.0` (`cb5275e…`) is the aligned preview source base, not signed-package-producing
proof or release authority. The unrelated `nativeshims-static-core` branch remains read-only
and supplies no accepted premise.

NativeShims may normalize platform ABI, bridge Apple block/callback ownership, and expose testable lifetime semantics. It does not own applet admission, CTAP cancellation policy, SCP, prompts, or a second scheduler.

#### Required contract for any new asynchronous native ABI

The following is a semantic template, not a prescribed export name/signature:

| Event | Required ABI rule |
|---|---|
| Submission rejected | Returns a defined error; no later callback for that submission; no retained caller-owned storage. |
| Submission accepted | Exactly one terminal operation callback, unless an explicitly documented shutdown completion mechanism accounts for it; the managed owner handles that mechanism exactly once. |
| Immediate completion | Explicitly permitted or forbidden by the ABI; if permitted, the callback may run before submission returns, so managed state must already be rooted. |
| Cancellation requested | Does not imply completion, successful cancellation, device rollback, or permission to destroy the operation. |
| Buffer use | Specify each buffer's owner, size, read/write access, pinning requirement, and exact last-use event. The current synchronous `fixed`-span wrapper cannot simply be reused for a callback operation. |
| Terminal completion | Supplies a stable error domain/code, valid byte count, and ownership disposition; preserve success racing cancellation rather than inventing an aborted result. |
| Shutdown | Supplies a quiescence acknowledgment before callback context can be freed; account for pending operations and reports separately from event callbacks. |
| Exceptions | No managed exception crosses a native callback boundary and no native language exception escapes the C ABI. |
| Version/capability mismatch | Deterministic initialization failure or a preselected documented fallback; no mid-mutation replay. |

Prefer opaque handles and explicit lengths/error codes over exposing platform-specific struct layouts. Ensure operation-handle creation and callback reentrancy rules are compatible: callbacks must not depend on an out handle that has not yet been returned. Export/header conformance tests and native runtime tests verify the same rules consumed by managed owners.

For every coordinated native slice record SDK SHA, native SHA, package identity, RID, and harness revision. Conditional cross-checkout delivery lands/tests native code and produces an identifiable package before SDK consumption. Test package artifacts, old/new compatibility, release exports, clean loading, Native AOT execution, and callback lifetime. Update `Directory.Packages.props` only after compatible package/RID contents are verified. Rollback chooses a known compatible pair before opening a device; it never switches backend mid-exchange.

### Orchestration and ownership

The parent orchestrator owns this master acceptance plan, interpretation of the generated manifest/coverage report, inventory acceptance, measurement design and interpretation, budgets, API and shared-contract decisions, ownership map, dependency changes, one-at-a-time integration, consistency review, and all checkmarks. It does not author runtime or measurement tooling. A measurement engineer implements narrowly scoped S0 baseline harness/tooling against the untouched runtime before production changes. A foundation engineer owns shared contract/runtime/scanner/harness work and later serves as serialized integration owner for shared files.

Platform engineers own disjoint platform adapter/native globs and their tests, measurements, and manifest fragments. One macOS engineer owns the corresponding native bridge after base approval. Shared protocol files, factories/interfaces, public baselines, common harness/manifest generation, toolchain, package declarations, and workflows are never concurrently edited. Cross-checkout export/package files likewise have one owner. Criteria remain accountable to the named platform lane even when a shared-file change is delivered by the foundation owner as a serialized subtask.

The concrete shared-file map is mandatory:

- `src/Core/src/Protocols/Fido/Hid/FidoHidProtocol.cs` remains with the foundation engineer throughout production work. S1 owns the cancellation/reuse behavior and evidence but requests changes to this file through the orchestrator as serialized foundation subtasks.
- `src/Core/src/Devices/ProtocolDeviceInfo.cs` belongs to the foundation engineer through S0b. After initialization callers and the shared contract stabilize, the orchestrator records an explicit tested ownership transfer to the S4 engineer. Other lanes request any later change through the orchestrator; they do not edit it.
- `src/Core/src/Transports/Hid/MacOS/MacOSHidIOReportConnection.cs` may receive only minimal withheld-seam instrumentation from the foundation engineer during S0b. At the foundation freeze, its tests and ownership are explicitly handed to S2; after that handoff the foundation engineer never edits it in parallel. S2 first inspects the existing `IIOKitDeviceLifetime` seam before proposing a new one.
- Any lane's changes to other shared FIDO, PC/SC, executor, factory, interface, public-baseline, harness, toolchain, or workflow files are delivered as serialized foundation/integration subtasks. The requesting lane retains criterion and result-packet accountability.

After S0b, platform worktrees start from the same accepted foundation revision. A shared-contract change pauses affected lanes; the single shared owner edits/tests it, the orchestrator freezes and distributes a new base, and affected lanes sync or are re-dispatched. Unrelated lanes may continue. The orchestrator integrates one result at a time, rechecks it against the accepted base, and returns conflicts to the owning engineer rather than silently resolving them. A serialized tooling engineer grows cross-platform CI and native runtime execution with each platform slice; S6 closes and reconciles evidence rather than introducing those workflows for the first time.

Every dispatch packet contains task/slice ID, ISC obligations, exact source/base and shared-contract revisions, owned and forbidden files, canonical examples/module instructions, required tests with expected nonzero counts, evidence obligation, and stop conditions. Every result packet contains exact files plus patch or authorized commit references, test commands/counts/results, manifest deltas and capability distinctions, measurement environment/inputs/raw artifacts, pending fixtures, shared-contract requests, and drift deviations. Commits, pull requests, pushes, and worktree publication require explicit user authorization; future stack descriptions do not grant it.

### Cross-layer consistency gate

For each slice and the final integrated tree, use unmodified siblings as comparators for changed code and for callers/contracts affected by the migration diff. Relevant comparators may come from Core/protocol/native and Management, Piv, Fido2, WebAuthn, Oath, YubiOtp, OpenPgp, SecurityDomain, and YubiHsm; this is not a blanket sweep requiring unrelated modules to change. Review naming/type placement, visible wire flow/no command hierarchy, cancellation/exception/lifetime ownership, `ConfigureAwait` conventions, prompt pairing, memory/zeroing/logging, tests, and docs. Equivalent semantics share conventions; platform-required differences are justified, not forced into inheritance.

“Migration-induced” means present in changed code or in a caller/contract affected by the migration diff. The orchestrator records findings and the engineer refits the slice, then reruns affected contracts and regressions. Existing `AsyncSurfaceConventionTests` provide the automated ISC-49 shape component; no new whole-applet analyzer is required. Public contract review maps to ISC-54, claim reconciliation to ISC-63, regressions after refits to ISC-61, and all additional manual style consistency pass/fail acceptance resides in the mandatory ISC-64 closure row. Existing unrelated drift is excluded only through a cited S0 baseline row and never substitutes for required behavior/native evidence.

### Work slices and dependency graph

The graph has 11 slices. Within S0, orchestrator design precedes a measurement engineer's tooling-only run against the untouched baseline; interpreted baseline results and numeric budgets freeze before S0b production changes. S0 defers executable contract gates to S0b. S0 and S0b may contribute to the same ISC without passing it universally. S1–S4 are engineering-ready after S0b; they become evidence-complete only with their applicable native/hardware matrix. Windows native tests require a Windows host, but S1 may develop managed seams independently; unavailable native/hardware evidence remains pending and blocks closure, not independent lanes. S5 waits for all four lanes. X staffing remains deferred.

```yaml
- name: S0_BoundaryBaselineAndContracts
  description: Orchestrator pins sources, verifies native provenance or records it pending, inventories/classifies boundaries, designs metrics, assigns files, and records the HID API decision. A measurement engineer then builds/runs tooling only against the untouched baseline; the orchestrator interprets results and freezes support, hardware, numeric budgets, and baseline evidence before production changes. The transaction choice may defer until before S4. Executable gates are S0b work.
  satisfies: [ISC-1, ISC-2, ISC-3, ISC-4, ISC-7, ISC-8, ISC-41, ISC-50, ISC-62]
  depends_on: []
  parallelizable: false

- name: S0b_SharedFoundationAndExecutableContracts
  description: Foundation engineer implements the genuinely async HID contract/factory/FIDO init plumbing selected by ISC-50, semantic scanner and registry, production-adapter withheld seam, executable profiles, and minimal bounded synchronous-native executor.
  satisfies: [ISC-1, ISC-2, ISC-3, ISC-4, ISC-5, ISC-6, ISC-7, ISC-8, ISC-9, ISC-10, ISC-21, ISC-22, ISC-24, ISC-25, ISC-50, ISC-52, ISC-60]
  depends_on: [S0_BoundaryBaselineAndContracts]
  parallelizable: false

- name: S1_WindowsFidoVerticalSlice
  description: Windows FIDO open/init/send/receive/cancel/remove/dispose through owned overlapped input/output. Managed seam work may proceed without hardware; Windows-host native tests and hardware evidence remain required for closure. Shared FIDO protocol edits are serialized foundation subtasks requested by S1.
  satisfies: [ISC-9, ISC-10, ISC-11, ISC-12, ISC-13, ISC-14, ISC-15, ISC-16, ISC-17, ISC-20, ISC-26, ISC-28, ISC-29, ISC-30, ISC-52, ISC-55, ISC-61]
  depends_on: [S0b_SharedFoundationAndExecutableContracts]
  parallelizable: true

- name: S2_MacOSHidVerticalSlice
  description: Per-direction callback investigation, persistent input delivery, classified output/feature callback or executor paths, and native bridge/quiescence only on an approved base.
  satisfies: [ISC-9, ISC-10, ISC-11, ISC-12, ISC-13, ISC-26, ISC-27, ISC-28, ISC-31, ISC-32, ISC-33, ISC-42, ISC-43, ISC-44, ISC-45, ISC-46, ISC-47, ISC-55, ISC-61]
  depends_on: [S0b_SharedFoundationAndExecutableContracts]
  parallelizable: true

- name: S3_LinuxHidVerticalSlice
  description: Readiness-driven input, explicit wake/lifetime handling, and P1-isolated blocking output/control operations.
  satisfies: [ISC-9, ISC-10, ISC-11, ISC-12, ISC-13, ISC-21, ISC-22, ISC-24, ISC-25, ISC-26, ISC-27, ISC-28, ISC-34, ISC-35, ISC-36, ISC-55, ISC-61]
  depends_on: [S0b_SharedFoundationAndExecutableContracts]
  parallelizable: true

- name: S4_PcscLifecycleAndDiscoveryIsolation
  description: Complete PC/SC lifecycle adaptation, the transaction decision made before this slice, SCP draining, discovery migration after explicit ProtocolDeviceInfo ownership transfer, and verified interactive/discovery capacity isolation. Shared changes are serialized foundation subtasks requested by S4.
  satisfies: [ISC-9, ISC-10, ISC-11, ISC-12, ISC-13, ISC-14, ISC-15, ISC-19, ISC-20, ISC-21, ISC-22, ISC-23, ISC-24, ISC-25, ISC-37, ISC-38, ISC-39, ISC-51, ISC-53, ISC-61]
  depends_on: [S0b_SharedFoundationAndExecutableContracts]
  parallelizable: true

- name: S5_OtpAndRemainingBoundaryClosure
  description: Windows OTP feature path, shared OTP recovery, and remaining raw/factory/monitor/disposal boundaries after all platform contract feedback is integrated.
  satisfies: [ISC-9, ISC-10, ISC-11, ISC-12, ISC-13, ISC-14, ISC-15, ISC-18, ISC-20, ISC-28, ISC-40, ISC-49, ISC-53, ISC-54, ISC-55, ISC-56, ISC-61]
  depends_on: [S1_WindowsFidoVerticalSlice, S2_MacOSHidVerticalSlice, S3_LinuxHidVerticalSlice, S4_PcscLifecycleAndDiscoveryIsolation]
  parallelizable: false

- name: S6_CrossPlatformPackagingAndEvidence
  description: Reconcile and close operating-system/native/Native-AOT gates grown during prior slices, then complete package consumers, hardware matrix, final measurements, consistency refits, claims scan, and production closure. S6 is not the first workflow or native-runtime implementation.
  satisfies: [ISC-1, ISC-2, ISC-3, ISC-4, ISC-7, ISC-8, ISC-41, ISC-42, ISC-43, ISC-44, ISC-45, ISC-46, ISC-47, ISC-48, ISC-49, ISC-54, ISC-57, ISC-58, ISC-59, ISC-60, ISC-61, ISC-62, ISC-63, ISC-64]
  depends_on: [S5_OtpAndRemainingBoundaryClosure]
  parallelizable: false

- name: X1_WinRtSmartCardExploration
  description: Isolated raw-APDU and host/Native-AOT feasibility prototype against production contracts; staffing begins only after S6.
  satisfies: [ISC-65, ISC-66, ISC-67, ISC-72]
  depends_on: [S6_CrossPlatformPackagingAndEvidence]
  parallelizable: true

- name: X2_CryptoTokenKitExploration
  description: Isolated raw-APDU and session/SCP/host/Native-AOT prototype against production contracts; staffing begins only after S6.
  satisfies: [ISC-68, ISC-69, ISC-70, ISC-72]
  depends_on: [S6_CrossPlatformPackagingAndEvidence]
  parallelizable: true

- name: X3_BackendPromotionDecision
  description: Compare both prototypes with PC/SC, record evidence-backed recommendations, and define any later promotion ISA.
  satisfies: [ISC-71, ISC-72]
  depends_on: [X1_WinRtSmartCardExploration, X2_CryptoTokenKitExploration]
  parallelizable: false
```

### Slice anchors and exits

| Slice | Exclusive/serialized anchors | Exit evidence, not universal completion |
|---|---|---|
| S0 | Master acceptance plan, generated manifest inputs, read-only runtime/release provenance, measurement harness/tooling | Independent nonempty inventory; frozen matrices/numeric budgets/baseline before changes; ownership map; HID API decision; transaction decision due before S4; provenance result or explicit pending stop. |
| S0b | Shared HID interface/factory/FIDO init, shared executor, scanner/manifest generator/harness; minimal macOS withheld instrumentation only | Red sensitivity retained; production adapter managed proof; negative fixtures; bounds/cancellation/sync-only/capacity tests; tested macOS handoff; frozen shared revision. |
| S1 | Windows Kernel32/HidD/HID adapter and owned tests; shared FIDO requests through foundation | Managed seams plus Windows-host overlapped report races and cancel-then-reuse; unavailable native/Native-AOT/hardware evidence remains pending. |
| S2 | macOS IOKit/HID adapter plus approved native macOS bridge checkout | Per-direction decision; input/removal behavior; callback/report lifetime and quiescence evidence. |
| S3 | Linux Libc/HID adapter and owned tests | Efficient readiness, explicit wake, descriptor generation, blocked-output isolation. |
| S4 | PC/SC connection/interops/discovery/listener after explicit `ProtocolDeviceInfo.cs` handoff; shared requests through foundation | Lifecycle responsiveness, prior transaction decision implementation, actual discovery/capacity isolation for ISC-23, chained/SCP integrity. |
| S5 | OTP protocol/Windows feature path and remaining factory/raw/monitor/disposal sites | OTP recovery, classified sync escapes, interaction/API evidence, all four lane feedback integrated. |
| S6 | Serialized final toolchain/workflow/package/public-baseline/docs integration and master acceptance document | Reconciled earlier CI/native paths, package/native/Native-AOT/hardware proof, final measurements, claims and consistency closure. |

### Future smart-card exploration

WinRT evaluation proves raw APDU fidelity, limits/chaining, transaction mapping, cancellation completion, errors, removal/reopen, deployment hosts, projection/COM behavior, Native AOT, and PC/SC contention compatibility. CryptoTokenKit evaluation proves session/exclusivity, authentication/SCP retention, identity, invalidation/removal, APDU fidelity, errors, entitlement/sandbox/signing, CLI/service feasibility, bridge lifetime, and Native AOT. Their output is a per-contract and host matrix, measurements, gaps, and promote/defer/reject recommendation. No prototype changes the shipping factory.

## Decisions

**Current package selection (2026-09-24):** `.8` is the private-published dependency bound to native source `71a23cd0` by workflow 35955737172. Fresh authenticated private-feed restores passed. `.7` was an earlier unsigned local candidate; `.3` and its earlier private-feed 403 remain historical checkpoints, not current blockers. Developers must configure their own private-feed credentials; `nuget.config` carries no credentials.

**Historical published `.3` package consumption:** run
[35888947280](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/35888947280)
completed successfully, including all seven platform consumer checks and private-feed
publication of `1.18.1-async.3`. The user requested updating the reference on completion;
At that checkpoint `Directory.Packages.props` selected `.3` and `NuGet.Config` mapped NativeShims to
the private feed. Local private-feed access returned 403; the exact uploaded workflow
artifact was downloaded, restored from an isolated temporary source, and passed all 17
focused macOS lifetime tests. Package SHA-256:
`b6df35457da06409f5bfd6643dd7dbc9da8b0e7e8404bb5fa99fcdde076f4a3c`.
Private-feed credentials remain a local setup requirement. The current pin is `.8`. No new hardware acceptance
or universal criterion is inferred from these package checks.

**Historical dependency override (D31):** the user explicitly approved moving forward with
NativeShims **`1.18.1-async.2`**. That earlier pin restored with a temporary local feed
and explicit `RestoreConfigFile`; it is an unsigned/unpublished preview, not a released
stable dependency. The current pin is the private-published `.8` above. The
earlier 1.18.0 override and its successful restore and 17 focused controlled-seam tests
remain historical evidence only: the cached 1.18.0 macOS arm64 library lacked
`Native_HidInput*` exports and could not run the persistent-input native route. Earlier
`.2` selected-key hardware proof applies to the preview; a later selected-key pending-read
probe followed the latest restore, without closing the route. Another agent owns
DllImport/LibraryImport migration and root policy files; this decision does not alter that work.

### Adopted direction — 2026-09-21

- **D1:** Keep async public operations and protocol orchestration, synchronous local computation, native async input/output where verified, and bounded blocking adaptation elsewhere.
- **D2:** Treat independent inventory, a shared contract harness, vertical adapters, and per-platform evidence as the migration method. The requested output is a developer handoff implementing that process, not an immediate code change; a completed plan or merged slice is not production closure.
- **D3:** NativeShims is within engineering scope because the team owns its code/package lineage. Use it for ABI and callback bridges, not applet/protocol policy.
- **D4:** Preserve runtime overlap and ownership guarantees. Executor queueing is an implementation detail and never relaxes public exchange admission.
- **D5:** Preserve public borrowed-input completion semantics. Do not use detached cancellation as a fast-timeout shortcut for ordinary session operations.
- **D6:** Keep production milestone P separate from exploration milestone X. WinRT and CryptoTokenKit produce recommendations; changing defaults is a later promotion decision.
- **D7:** Use the 12-section E4 ISA form and 72 stable criteria. Parameterized adapter/operation rows avoid duplicating prose criteria while keeping every matrix row and its evidence mandatory.
- **D8:** The external `IsaFormat.md` referenced during the original handoff was unavailable at the inspected skill path. The historical artifact followed the available scaffold/completeness workflow and canonical 12-section example.

### Delivery refinement — 2026-09-21

- **D9:** One parent orchestrator owns planning, independent inventory acceptance, measurement design/interpretation, budgets, shared decisions, integration, consistency review, and evidence/checkmarks. A measurement engineer implements S0 baseline tooling; implementation engineers own all runtime/scanner/harness/test/docs changes; one foundation engineer owns shared code.
- **D10:** Simplify dependencies to S0 → S0b → parallel S1/S2/S3/S4 → S5 → S6 → parallel X1/X2 → X3. S0b owns shared executable gates and P1 implementation; only S4 can satisfy actual discovery-isolation ISC-23. S0/S0b cannot claim universal acceptance.
- **D11:** NativeShims is a code/package lineage in the same upstream, but tag 1.16.1 is only a candidate release reference. Verify release/package provenance and a compatible base before native edits; do not adopt the unrelated static-linking branch.
- **D12:** Use one master acceptance document plus a generated boundary manifest/coverage report and disjoint engineer fragments, not duplicated sources of truth or a whole-program theorem framework. Semantic known-leaf/wrapper checks and negative fixtures remain mandatory.
- **D13:** Apply scoped per-slice and final consistency review to changed code and migration-affected callers/contracts. Existing convention tests contribute API shape, while all additional style pass/fail closure is recorded under ISC-64. Unrelated pre-existing drift requires an S0 baseline row and never exempts behavioral/native evidence.
- **D14 — intelligence-only authorization:** The user requested S0 investigation and explicitly wants to know before decisions on new code, architecture, or seams. Existing-source inspection, artifact inspection, existing tests, and recording findings are authorized. Present proposed interface changes, seam changes, scanner/harness code, worker policy, and platform architecture before implementation. The proposed S0 measurement engineer cannot begin writing a harness under this authorization. The user also requested the latest `yubikit` base; a fresh fetch confirmed the worktree and `origin/yubikit` still match `a7f2cae8c32ad6e0ada55e404f85442f6a266f6c`.
- **D15 — approved breaking-change and visibility policy:** The user permits any breaking change in v2, provided the public applet APIs remain consistent. Types not needed at the intentional public API boundary may become internal. More Core classes might be exposed as the SDK matures; do not promise that expansion or retain public machinery in anticipation of it. This settles compatibility tolerance, not the exact retained surface. Preserve semantic differences between applets where meaningful; normalize equivalent creation, cancellation, disposal, ownership, options, naming, and memory contracts. Consumer-surface tests must enforce the newly agreed boundary, including intentional removals, rather than require old external transport implementations to keep compiling. D14 still applies to new code, seam, and architecture decisions. This v2 permission does not settle compatibility for the independently consumed NativeShims package.

### Public raw-access decision — 2026-09-22

- **D16 — retain both public raw-access tiers:** The user confirmed that raw sessions and raw connection operations remain publicly supported. Retain the guarded logical-exchange surface of `RawSmartCardSession`, `RawFidoHidSession`, and `RawOtpHidSession`, as well as the expert operation contracts represented by `ISmartCardConnection`, `IFidoHidConnection`, and `IOtpHidConnection`. Their public base/lifetime contracts and caller-owned connection/session composition remain intentional consumer capabilities, including external implementations of the public connection contracts. D15 permits consistent signature changes, but not silently removing either access tier. Lower-level platform/report interfaces such as `IHidConnection` are distinct internalization candidates; this decision does not require native handles, schedulers, callback machinery, or test seams to be public. Preserve the documented distinction between session-managed exchange safety and raw-connection caller responsibilities. Exact signatures, transactions, and internal seams remain subject to D14.

- **D17 — Gate 1 approved, 2026-09-22:** The user explicitly approved the product gate: responsiveness is an ordering guarantee across every required scenario; ownership/cancellation/data-lifetime contracts and both public raw-access tiers are preserved; consistent breaking v2 changes/internalization are permitted; initial recurring verification covers Windows x64, Linux x64, and macOS arm64 without withdrawing other packaged targets. Production and later backend exploration remain separate. Exact hardware/system matrices and numeric budgets remain explicit prerequisites before affected measurement/production work. Proceed to architecture review, not implementation; Gate 2, Gate 3, and Gate 4 still require approval.
- **D18 — Gate 2 approved, 2026-09-22:** The user approved A1–A5, including the explicit expansion of IOKit dispatch-lifecycle and report-callback usage, with evidence-gated per-direction fallbacks. Keep public typed raw operations and raw sessions; move lower-level report machinery behind an internal asynchronous boundary. Use platform completion/readiness plus owned bounded synchronous workers, discovery/interactive separation and finite cleanup/control reservations. Preserve distinct guards, require rooted quarantine and positive native release evidence, and verify the synchronous-disposal wait graph. Keep FIDO cancellation between reads; no new duplex obligation is imposed on external raw implementations. Test below production adapters and keep semantic inventory tooling outside shipping code. Exact signatures, internalization closure, seam contracts, budgets, native exports and slice sequence still require Gates 3 and 4. The expanded IOKit scope does not include changing device-matching/discovery shape.

- **D19 — initial fixtures selected, 2026-09-22:** The user requested the three currently connected keys for the initial capacity/measurement proposal. Read-only operating-system enumeration confirmed three Yubico `1050:0407` composite devices, recorded as K1–K3 in Gate 3. Bus versions/locations are descriptor observations, not verified firmware or applet behavior. This selects the initial local fixture count, not numerical worker/queue defaults, a supported-device-count promise, the whole hardware matrix, or authorization for destructive tests. The initial capacity table in Gate 3 remains unmeasured calibration subject to review and baseline freeze.

- **D20 — single-key first iteration requested, 2026-09-22:** The user asked to focus the first sync/async/cancellation/recovery iteration on one YubiKey to minimize scope, while preserving the existing planning and findings. Defer cross-key scheduling, shared-capacity reservations, multi-device saturation and scaling work to the addendum; preserve same-key races, ownership, recovery and native lifetime. D19's simultaneous three-key target no longer drives this iteration; those devices remain potential sequential fixtures. Reopen the affected product/architecture gates, retain the unapproved earlier program design as reference, and do not treat this scope request as approval of a replacement implementation topology. D15/D16 public-access and compatibility policies remain settled. All 72 epic criteria retain their identities and completion state.

- **D21 — revised Gate 1 approved, 2026-09-22:** The user approved one selected key at a time as the first iteration: open, initialize, operate, cancel/recover, dispose and safely reopen. Preserve same-key races, ownership, public applet/raw access and the existing desktop-platform direction. Defer cross-key scheduling, shared capacity, fairness, healthy-key progress under another key's failure, and simultaneous-device measurements. No artificial global one-key restriction is authorized. Proceed to review the smaller architecture; its per-connection worker proposal is not yet approved. Earlier architecture and program-design material remain available for later iterations.

- **D22 — revised Gate 2 approved, 2026-09-22:** The user approved connection-local lifetime ownership with one lazy worker for blocking native calls, no ordinary backlog, and bounded coalesced scope-end/shutdown intents. Retain native completion/readiness, the explicit macOS IOKit expansion, positive release evidence, same-key discovery quarantine and public raw/app session access. Shared pools and cross-key quotas remain deferred.
- **D23 — incremental delivery requested, 2026-09-22:** The user explicitly said not to waterfall-spec the entire effort: conventions, rules and verification are sufficient to proceed with the first iteration after reviewing pseudocode. Replace the remaining whole-effort Gate 3/Gate 4 prerequisite with a first-slice walkthrough, then implementation/tests/review. Label non-blocking choices **deferred to implementation effort** and resolve them live. This does not defer correctness, authorize an unreviewed material architectural/public-contract change, or turn missing native/hardware evidence into success. The proposed initial smart-card slice awaits pseudocode acceptance; implementation has not begun.

- **D24 — first implementation slice approved, 2026-09-22:** The user accepted the smart-card lifetime pseudocode and authorized implementation. Use the existing native package to prove the single-key open/operation/cancellation/cleanup model through production-adapter tests, preserving raw/app access and the existing ownership rules. Keep synchronous transaction paths lifetime-safe on the same owner while the new async public transaction surface remains deferred to implementation effort. Run focused regressions, independent code review and a style/consistency check. Identify the selected fixture before device operations; do not count planning approval or managed tests as native/hardware acceptance.

- **D25 — delegated incremental orchestration, 2026-09-23:** The user delegates ISA and planning progression to the orchestrator and implementation of bounded approved slices to Engineers. The orchestrator owns master criteria, evidence interpretation and proposed sequencing. This does not grant self-approval: D14 remains binding, and material signatures, visibility, seams, native-platform changes, architecture or safety decisions must be presented to the user for approval in the affected-route walkthrough before implementation. Whole-epic reapproval is not required, archived multi-key work is not reactivated, and all 72 criteria remain unchecked until their evidence obligations are actually satisfied. The orchestrator must track its own and delegated work as it progresses in this master, status and affected plans. At each meaningful checkpoint record scope/owner, outcome (`planned`, `in progress`, `implemented`, `verified`, `blocked` or `deferred`), concrete evidence/command and limits, and the next decision; distinguish review from managed implementation, native-runtime and hardware verification. Do not create a separate report artifact or invent completed evidence. Each recommendation must state **document section → relevant rule in plain language → current evidence/gap → recommendation and approval needed**, not cite references alone.

- **D26 — bounded definition of done requested, 2026-09-23:** The user requires a finite slice finish line to prevent endless refactoring and overengineering. Before an Engineer starts, agree the observable outcome, selected route, owner, bounded responsibilities/files and non-goals, applicable existing criterion IDs, finite acceptance probes and exact relevant commands, and required evidence grades. Slice closure is not completion of all 72 criteria. The operating defaults below govern orchestration; they add no epic gate or criterion and approve no route.

- **D27 — close documentation review and schedule work, 2026-09-23:** The user waived the pending independent reviewer for the transport/concern documentation clarification and requested scheduling upcoming work. That documentation checkpoint is closed on its recorded document validation and orchestrator consistency check. This waiver does not retroactively claim an independent review ran, change implementation-slice review defaults, or approve an unreviewed production/native design. The orchestrator will schedule bounded prerequisite/design work, reconcile its results into the existing plans, and present the material decisions before dispatching dependent implementation.

- **D28 — NativeShims 1.18.0 and modern macOS direction, 2026-09-23:** The user selected NativeShims 1.18.0 for v2 and rejected paying implementation cost solely to preserve older macOS APIs when doing so would compromise smart-card or HID support. `README.md:16-20`, `src/Core/README.md:12-17` and `Directory.Build.props:35` establish the v2 .NET 10 target; the [upstream .NET 10 support matrix](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md), fetched on this date, lists macOS 14, 15 and 26 on arm64/x64. That upstream matrix is not YubiKit hardware verification and does not promise every future macOS 14+ release indefinitely. Native artifact minimum macOS 12, .NET runtime support from macOS 14, and API availability from macOS 10.15 are distinct facts. The approved macOS HID direction remains persistent modern IOKit dispatch lifecycle across the current upstream-supported systems; do not add an old-API fallback solely to preserve a macOS 12 binary target. This dependency decision does not implement HID migration, make blocking PC/SC asynchronous, add abort semantics, or advance CryptoTokenKit/WinRT beyond later exploration. Native changes still require an approved bridge contract and compatible source base, and must not regress macOS smart-card or HID behavior.

- **D29 — ratchet the bounded prerequisite increment, 2026-09-23:** In response to the concrete next-step proposal, the user said “They wont. Lets ratchet forward.” The orchestrator dispatched only non-production macOS input-owner experiments on a detached 1.18.0-tag worktree and opt-in baseline tooling in the benchmark project, plus a synthetic-backed native AOT verification host. The finite outcome is compiled and executed synthetic ownership/quiescence probes and hardware-free tooling checks, **not** a real-device baseline or the previously proposed full 2b exit. The orchestrator reviewed the architectural native contract and targeted correctness fixes; no independent code reviewer ran (D27 waived a documentation review only). Material public interfaces, shipping native exports/package, Core route migration and hardware operations were not authorized. D14/D26 still govern any next production slice.

- **D30 — coherent production milestone, 2026-09-23:** The user directs the orchestrator to continue within the approved product/platform principles through larger coherent changes rather than stop for each helper, test, fixture choice or routine packaging decision. D14/D25's earlier micro-walkthrough requirements yield to this newer delegation for in-scope internal implementation; retain the public raw-access and safety invariants. Escalate genuine material scope changes, unresolvable blockers and destructive operations, not each implementation detail. This is not blanket permission for credential writes, releases or git commits. The current review point is a **full built-in macOS FIDO** open/initialize/send/receive/cancel-recovery/concurrency/shutdown/reopen production route with evidence; the user may request refactoring at that review point. Continue the rest of the epic afterward; no criterion is waived or checked by this decision. D26's finite evidence/stop rule applies to coherent milestones, without automatically imposing an independent review round the user has not requested.
- **D31 — preview dependency and epic continuation, 2026-09-23:** After a temporary 1.18.0 override, the user said “set it to 1.18.1-async-2 then whatever it was lets roll forward Epic.” The exact package version is `1.18.1-async.2`; the pin is in `Directory.Packages.props` and requires a local feed because the preview is unsigned and unpublished. Preserve the earlier stable-attempt evidence without attributing its 17 managed seam tests to native exports. Continue the approved epic under D30; the macOS physical-key pending-read dispose/reopen probe has since passed (Verification below), while bounded Windows FIDO production-route design proceeds independently. Windows overlapped implementation is a proposal, not runtime/hardware proof. No new criterion is checked by this decision.

The following is the historical `.2`/`.3` dependency schedule; the active next bounded
slice is at the top of `docs/plans/yubikit-async-boundaries/00-status.md`. N1 and D29's
bounded tooling/harness increment are recorded. At this earlier D30 checkpoint the
macOS built-in FIDO production milestone was **in progress**, not accepted:
the selected-key packaged native-AOT public route, read-only lifecycle and pending-raw-receive
dispose/reopen have executed with the earlier local `.2` preview since the initial empty-host
check. The then-current `.3` pin restored locally via a workflow artifact, not the private feed;
its selected-key route was unverified at that checkpoint. Later `.3` touch, `.6` actual removal
and `.7` normal-use proofs supersede these particular gaps only on tested paths. Bounded
Windows FIDO overlapped production-route design was proposed read-only in parallel;
Linux readiness and remaining macOS report directions can proceed independently when scoped.
Then complete OTP/macOS report paths, smart-card async transaction/context proof and the
cross-platform production matrix; WinRT/CryptoTokenKit exploration follows separately.
Do not treat these selected macOS probes as route closure or Windows/Linux evidence.

Selected aligned source option from N1: local annotated tag `1.18.0` resolves to
`cb5275ea8c151b7ad0bc9465ff2f9fa24785d3b3`, a descendant of candidate `29d38c6`.
At that immutable revision, `Yubico.NativeShims/build-macOS.sh:23,35` and the macOS
triplets set deployment target 12.0; `.github/workflows/build-nativeshims.yml:259–295`
invokes and checks those inputs. This verifies committed source/build configuration only.
The consumed 1.18.0 package is verified separately below, but its metadata supplies no
source commit, so exact source-producing binding remains pending. Use this tag as the
recommended aligned base for a proposed bridge contract, not blanket approval for native
edits. The old 1.16.1 producer hunt is historical and no longer blocks the selected
dependency; do not resume it without new relevance.

**D29 prerequisite evidence (historical non-production increment).** The Native Engineer used a new
detached worktree `/Users/Dennis.Dyall/Code/y/worktrees/nativeshims-macos-input-harness`
from the immutable 1.18.0 tag `cb5275ea8c151b7ad0bc9465ff2f9fa24785d3b3`, not the
unrelated native branch. Only `Yubico.NativeShims/hidinput/{owner.h,owner.c,internal.h,
backend_synthetic.c,backend_iohid.c,tests.c,CMakeLists.txt,.gitignore}` is new: an
experimental standalone dylib, with no shipping CMake/export/package changes. The opaque
create/start/cancel/wait/destroy contract copies into bounded reports, isolates owners,
refuses self-wait, waits for cancellation acknowledgment **and** accepted-delivery drain,
and refuses destroy while busy. Checked close runs synchronously on the destroy caller,
not the event queue; `HIDINPUT_CLOSE_FAULT` retains roots without blind close retry.
Production close would need a blocking owner. The native cancellation handler posts an
acknowledgment after its return on the same serial queue. The actual IOKit backend compiled
and linked, but was not exercised with a device. Synthetic libdispatch execution passed
eight C tests in Debug, Release and AddressSanitizer configurations; the final Release
build explicitly used `-DCMAKE_OSX_DEPLOYMENT_TARGET=12.0` and its binary minos was
inspected as 12 (earlier host-default-15 configuration was corrected). Reproduce the
final Release result in that detached worktree:

```text
cmake -S Yubico.NativeShims/hidinput -B Yubico.NativeShims/hidinput/build-release -DCMAKE_BUILD_TYPE=Release -DCMAKE_OSX_DEPLOYMENT_TARGET=12.0
cmake --build Yubico.NativeShims/hidinput/build-release
ctest --test-dir Yubico.NativeShims/hidinput/build-release --output-on-failure
```

One close-failure race first reproduced a red test (release attempted despite failed
close), then passed after the targeted fix; no independent reviewer ran. The main
worktree's `verification/MacOSHidInputVerification/{Program.cs,
MacOSHidInputVerification.csproj}` executes three watchdog-isolated synthetic-backed
macOS arm64 Native AOT probes (borrow/root/drain, late-owner isolation, overflow) via
unmanaged Cdecl callbacks and a rooted `GCHandle`. The final minos-12 experiment dylib
was republished and **executed**, reporting `MacOS native input AOT: 3 passed (synthetic
backend)`:

```text
dotnet publish verification/MacOSHidInputVerification/MacOSHidInputVerification.csproj -c Release -r osx-arm64 --self-contained -p:PublishAot=true -p:HidInputNativeLibrary=/Users/Dennis.Dyall/Code/y/worktrees/nativeshims-macos-input-harness/Yubico.NativeShims/hidinput/build-release/libhidinput_experiment.dylib
verification/MacOSHidInputVerification/bin/Release/net10.0/osx-arm64/publish/MacOSHidInputVerification
```

These prove synthetic C queue/managed callback lifetimes, not actual IOKit device callback
quiescence, removal behavior, or HID hardware. The experimental ABI has no managed
terminal-error/removal read path; production initialization, control and callback terminal
integration still require the D1 walkthrough. ISC-10/11/26/27/28 gain **partial**
synthetic evidence only; ISC-31/33/58 remained unchecked at this checkpoint. Later
ISC-49/52 closure is recorded in the evidence record below.

The Measurement Engineer added opt-in `--async-boundary-self-test` (8 passed),
`--async-boundary-baseline --dry-run` (no device), and preserved `--list flat` (12 existing
benchmarks) under `benchmarks/Yubico.YubiKit.PerformanceBenchmarks/`; existing session
signatures were updated to `SessionCreationOptions` to compile with the current API.
Only two hardware modes are implemented but **not executed**: read-only lifecycle/getInfo/
dispose/reopen and raw no-input receive without sending a request. The previously proposed
keepalive cancel/recovery and held-exchange overlap remain deferred until faithful fixture
support. Per-sample child watchdogs with flushed progress distinguish blocked synchronous
prefix, returned pending task, synchronous/task fault and censored unknown cleanup. Dataset
metadata records source commit plus shipping diff hash, tooling hashes, executing
NativeShims 1.18.0, package/deployed-native digests and architecture/runtime/fixture
serial; it compares the cached archive's native **asset** to the deployed binary, not the
whole package to that binary. A passing comparison does not prove which DLL loaded.
Firmware is null with “not read” reason; native durations/counts are null when unavailable.
At that tooling-only checkpoint there was no hardware dataset, numeric budget or regression comparison. For a comparable
before/after baseline use `db7a1bf6` old transport source with the **same** local preview
package `1.18.1-async.2` on both sides; record package hash, shipping diff and tooling
revision. The earlier 1.18.0 baseline proposal is superseded, not measured evidence.

**D30 macOS production review point — historical pre-fixture snapshot.** The Route Engineer has
implemented internal `MacOSFidoHidConnection`/`IHidInputBridge`, terminal wake,
slot/registry/discovery claim transfer, and awaitable initialization through
`ApplicationSession`, retaining existing public raw contracts. The native engineer
integrated the opaque owner into a local preview package with five new macOS exports,
at-most-once terminal callback, report type/ID validation and checked close-failure
retention. Both macOS architectures' shared/static builds expose 41 exports; native
11 tests and synthetic native AOT 5 probes passed as reported by the engineers. Local
package `1.18.1-async.2` is reproducible from the tagged 1.18.0 base and the dirty-file
hash manifest in detached-worktree `docs/local-provenance.json`, not a signed release.
Main-worktree restore passed using
`RestoreConfigFile=/var/folders/gn/mh64zz5969j89_f5dffnvnb80000kt/T/opencode/yubikit-async-native.nuget.config dotnet toolchain.cs -- restore --project Core`.
Final preview-package regression runs reported Core 1,355 passed/3 skipped, PublicApi 22,
Fido2 471, Management 86 and resilience-fast 77. After a small report-queue change that
avoids allocating a discarded overflow copy, the focused
`MacOSHidFidoRouteLifetimeTests` filter passed 17; full suite results precede that
allocation-only follow-up. The packaged-native AOT `MacOSHidRouteVerification` host
published with real Core/Fido2 and `.2`, but `--list` discovered zero devices and exited 2;
system USB enumeration also found no YubiKeys. `--probe --serial 0` rejected an invalid
fixture. At that time no actual IOKit input/cancel acknowledgment, device lifecycle, firmware or
performance evidence existed. Parent correctness review/fixes occurred, but no independent
code review is claimed. Earlier D29 no-shipping statements describe that historical
increment only. The later selected-key results are recorded below; ISC-49/52 are complete,
while other mapped native/route criteria remain pending.

### Bounded slice definition of done

Before dispatch, record these five closure conditions:

1. **Bounded outcome:** one observable outcome on one selected route, with owner,
   allowed responsibilities/files, explicit non-goals and applicable stable criterion IDs.
2. **Finite evidence:** named acceptance probes, exact relevant regression/build commands
   and required managed, native-runtime, hardware, platform and performance grades as
   applicable. Reuse existing passes until meaningful changes or new risks invalidate them.
3. **Behavioral closure:** every agreed behavioral check passes; no correctness, safety or
   regression blocker remains; the change fits scope and surrounding conventions without
   speculative shared abstractions or generalization solely for unimplemented routes.
4. **Bounded review:** one independent correctness review on the settled shape, followed
   only by targeted review of fixes. The existing two-pass Craftsman budget remains the
   upper bound. The ordinary default is one implementation pass plus at most one optional,
   bounded readability cleanup and no more than two review/fix cycles. If blockers remain,
   the orchestrator presents them to the user rather than waiving them or declaring success.
5. **Honest handoff:** update this master, status and affected plan with commands/results,
   evidence grades and limits, deferred items with reasons, and the next action or decision.

**Stop when all five are met.** Defer unsolicited enhancements and redesign for taste.
Unavailable required evidence makes the route **blocked**; it is never silently downgraded
to managed-only acceptance. A pre-agreed limited investigation or managed-only milestone
may finish on its own stated evidence grade. A newly discovered safety issue may require
more work, but must be made explicit and re-scoped; materially new scope requires user
approval. Do not spawn self-perpetuating review rounds or rerun a whole suite without a
meaningful change or newly identified risk.

### Choices to settle before affected work

| Decision | Default/proposal | Owner and deadline |
|---|---|---|
| Support floors/RIDs | Preserve current products; recurring first matrix is Windows x64, Linux x64, macOS arm64 | Orchestrator, S0 |
| Native provenance/base | Validate release job/package provenance and compatible native revision; candidate tag is not proof | Orchestrator/native maintainer, S0 before native edits |
| Public surface and `IHidConnection` evolution | D15/D16 remain binding; exact changes deferred to implementation effort and reviewed when the affected route is reached | Orchestrator and route engineer |
| Transaction API | Exact new async signature deferred to implementation effort; existing begin/end must remain lifetime-safe in the first slice | Smart-card slice owner |
| First-iteration execution scope | Connection-local owner approved under D22; exact helper layout deferred to implementation effort; global P1 reservations stay in the addendum | Orchestrator and engineer |
| macOS directions | Investigate callbacks per direction; use classified P1 fallback where callback cannot be verified | macOS owner, S2 |
| Windows OTP directions | Prove GET and SET independently; P1 fallback when required | Windows/OTP owner, S5 |
| Linux readiness scale | `poll`/`eventfd` first; `epoll` only with measurements | Linux owner, S3 |
| Hardware and performance budgets | One selected key at a time (D20); retain K1–K3 as potential sequential fixtures. Verify firmware/capabilities and required platform/reader rows; freeze relevant budgets before production changes | Orchestrator, reduced-scope S0 |
| Irrecoverable native hang | Retain safety/ownership; do not promise bounded completion | API/Core owners, S0 contract |

### Rejected shortcuts and deferred optimizations — 2026-09-21

- `ValueTask`, `ConfigureAwait(false)`, or wrapping every public operation in `Task.Run` does not establish the required execution/lifetime contract.
- `SCardCancel` is not assumed to abort portable APDU transmission.
- Caller cancellation never frees borrowed/pinned memory while native work continues or triggers ambiguous mutation replay.
- Successful Native AOT linking, no-hardware discovery, candidate source package declarations, and agent summaries are not runtime/package/hardware evidence.
- No unbounded thread replacement, speculative all-primitives foundation, generic native hierarchy, whole-program theorem, unrelated `TimeProvider` sweep, or mandatory shared P2.
- Do not force platform differences into inheritance or hide wire flow in operation-specific commands.

## Changelog

- **2026-09-24** | **conjectured:** The unsigned local `.7` dependency still blocked producer and private-feed evidence. **refuted by:** workflow 35955737172 published pinned `.8` from native revision `71a23cd0` with seven packaged consumers; fresh authenticated Core and native-consumer restores passed. **learned:** keep historical `.7` limitations, but report current source-bound packaging separately from device/hardware and release-export inspection. **criterion now:** ISC-41 and ISC-48 checked; ISC-47 and platform-wide ISC-58 remain open.
- **2026-09-24** | **conjectured:** A synchronous fallback on the public transaction interface precluded any responsive transaction acquisition. **refuted by:** the built-in PC/SC connection's controlled native begin returns a pending task before release; the fallback applies to external implementations only. **learned:** distinguish the built-in production path from the external synchronous compatibility contract. **criterion now:** ISC-51 checked; ISC-37 and ISC-53 remain open.
- **2026-09-21** | **conjectured:** An async-looking layer stack establishes that callers are never blocked by device I/O. **refuted by:** inspected FIDO/OTP wrappers call synchronous report I/O before returning completed tasks, and macOS input pumps a run loop in `GetReport`. **learned:** signature conventions need an independent behavior probe below the production adapter. **criterion now:** ISC-9 and ISC-60.
- **2026-09-21** | **conjectured:** A nonblocking HID descriptor and writable readiness eliminate Linux blocking transport work. **refuted by:** read readiness and output/control execution have different blocking behavior. **learned:** input readiness and output/control execution need distinct owners. **criterion now:** ISC-34 and ISC-35.
- **2026-09-21** | **conjectured:** Cancelling an API wait equals native completion and permits immediate cleanup. **refuted by:** overlapped cancellation requires terminal completion before storage reuse, and PC/SC has no portable transmit-abort promise. **learned:** caller intent, native completion, protocol recovery, and resource release are separate boundaries. **criterion now:** ISC-11, ISC-12, ISC-15, ISC-25, and ISC-45.
- **2026-09-21** | **conjectured:** NativeShims must be a separate repository and the available static-linking checkout is a suitable implementation base. **refuted by:** the checkout shares `https://github.com/Yubico/Yubico.NET.SDK.git`, is unrelated in-flight work, and tag 1.16.1 is only a candidate release reference rather than package-producing proof. **learned:** track code/package lineages and immutable revision/package pairs; validate release provenance before native edits. **criterion now:** ISC-41, ISC-42, and ISC-48.

## Verification

### Current `.8` checkpoint — 2026-09-24

Owner: orchestrator for acceptance; Native/Route Engineers for source and probes. SDK HEAD `db378ffc` contains the expert feature slice; listener-generation cleanup remains uncommitted. `.8` native producer source is `71a23cd0269c968c1d9420eddb2e2fec71e0cc29`. Later unpushed native commits `541cfb09` (export checker) and `760d0416` (persistent-registration contract comment and test) do not change the `.8` runtime/package and are not producer revisions. No full cross-platform acceptance, actual listener arrival/removal callback-drain proof or `.8` physical-unplug run is claimed. [Status](docs/plans/yubikit-async-boundaries/00-status.md) has the earlier bounded measurement; the older `.7` table below is historical.

| Criterion | Audited evidence | Grade / limit |
|---|---|---|
| ISC-41 | Pinned `Directory.Packages.props:23` selects `1.18.1-async.8`. [Native workflow 35955737172](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/35955737172) has head SHA `71a23cd0269c968c1d9420eddb2e2fec71e0cc29`, package/artifact source-bound attestation and SHA-256 `c85c56f7a41c6999b48b1a5fdcd82c56fbfb3e5ee6ff18ccc64a41144f760403` (also captured in `artifacts/measurements/current-profile-20260924T051049865Z.json:48-56`). | Published package provenance; `.7`'s pre-commit build and `.9`'s separate workflow are not this pin. |
| ISC-48 | The same workflow's seven Native AOT packaged-consumer jobs succeeded (`linux-x64`, `linux-arm64`, `win-x86`, `win-x64`, `win-arm64`, `osx-x64`, `osx-arm64`), with packaging and private publish successful. Fresh private-feed restore passed in an empty cache after `nuget.config:8,11` source key was renamed `Yubico_GH` to match the existing credential name at the unchanged URL; no secrets were altered. Normal parent `dotnet toolchain.cs restore` passed 43 projects. | Clean packaged resolution for each required RID, **not** Windows HID driver or hardware/device-I/O coverage for ISC-58. Developers need their own private-feed credentials. |
| ISC-51 | `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/PcscConnectionLifetimeTests.cs:38-59` invokes the built-in connection's `BeginTransactionAsync` with native begin withheld; invocation returns a pending task before release, and cleanup releases begin and ends once. | Managed withheld-completion behavior for built-in acquisition; the interface's synchronous fallback for external implementations does not satisfy this probe. Not full PC/SC platform evidence. |
| ISC-38 | Shipping Core source audit locates `SCardCancel` only in the monitoring listener path (`DesktopSmartCardDeviceListener.cs` through `ISCardApi`/interop), not in portable transmit cancellation. Controlled cancelled-transmit tests retain the borrowed native input until transmit returns. | Scoped source and fake-native borrow proof; this one-time audit is not a scanner that would prevent a future new cancel callsite. It does not claim that `SCardCancel` interrupts native `SCardTransmit`. |
| ISC-39 | `PcscContextIsolationTests.ListenerDisposal_CancelsOnlyMonitorContext_WhileConnectionTransmits` runs the production monitoring listener and smart-card connection against controlled native seams with **actual distinct context handle addresses**. While transmit is held, listener disposal cancels/releases only monitor context A; context B remains borrowed and is disconnected/released only after its own transmit completes. | Production route with fake native contexts, not Windows or Linux operating-system/native driver proof. |
| ISC-43 | `Yubico.NativeShims/hidinput/owner.h:32-63` at unpushed comment/test-only native commit `760d0416` specifies one persistent subscription per owner, not a submission for every report or Start. Create has no callbacks; Start registers handlers before activation. Activation **can** deliver on another queue before Start returns (source-modeled possibility, not an observed race). One Start can yield zero or many ordered, once-delivered accepted reports; terminal is at most once after drain; clean Cancel need not emit terminal, and WaitShutdown acknowledges and drains. Existing lifecycle tests extended for two reports from one Start and Start-once rejection; the native suite remains 23 Release tests green without changing runtime code from producer `71a23cd0`. | Independent review accepts the explicit persistent-registration callback contract for the five new `Native_HidInput*` exports. No per-report/per-operation callback-count fixture is claimed. Future new asynchronous callback operations must define and pass their own submission/registration contract; this does not waive ISC-9 or universal platform checks. |
| ISC-44–46 | For the five new macOS `Native_HidInput*` exports, source audit plus Apple's callback-handler lifetime contract show input report buffer/device/context ownership through callback return or cancellation acknowledgment and accepted-delivery drain. Native synthetic Release tests (23 at producer `71a23cd0` in continuous integration) cover destroy-BUSY, close-attempt counters, acknowledgment/drain and release; 23 AddressSanitizer tests passed locally against identical runtime source **before** that commit, not rerun with the committed SHA. Actual `.6` unplug → terminal → dispose → replug passed after acknowledgment/drain on the removal source carried into `.8`; `.8` normal hardware paths passed without new operator unplug. | Independent review accepts this **substitute** for the planned native retained-buffer counters, which were not implemented. Synthetic tests and source/Apple contract plus version-specific device evidence are distinct grades. Only this new bridge is checked. |
| ISC-47 | `Yubico.NativeShims/tests/check_package_exports.py` (verification-only native commit `541cfb09`, not pushed or wired to continuous integration) used LLVM symbol inspection of the **actual** `.8` package SHA-256 `c85c56f7a41c6999b48b1a5fdcd82c56fbfb3e5ee6ff18ccc64a41144f760403`: all 14 shared/static artifacts across `linux-arm64`, `linux-x64`, `osx-arm64`, `osx-x64`, `win-arm64`, `win-x64`, `win-x86` passed, with 36 canonical `Native_*` symbols except 41 on each macOS artifact and zero declared test-only helpers. Five negative/positive checker tests, including compiled Mach-O shared and static negative controls, passed. | One-time actual release-artifact export proof; not a continuous-integration regression gate or runtime execution on other operating systems. `541cfb09` did not rebuild `.8`. |
| ISC-60 | Six `BoundaryInventory/ResponsivenessProbeTests` exercise a dedicated context-bearing caller with withheld native completion. Actual legacy synchronous macOS feature open and `OtpHidConnection` GET/SET task-returning entrypoints fail the caller/native thread-identity gate; positive built-in macOS FIDO open and OTP open/receive probes additionally return before native release. | Managed sensitivity against these known synchronous HID escapes, not universal entry/profile, hardware or Windows/Linux proof. |
| ISC-31 | `MacOSFidoHidConnection` retains the persistent native input owner for typed FIDO; the public expert `MacOSHidIOReportConnection` now delegates to that owner instead of scheduling `CFRunLoopRunInMode` on the caller. Pending typed reads detach their expected reader under lock on cancellation, without cancelling native input or publishing a terminal result; late reports queue, and a stale cancellation token cannot detach a replacement reader. Five read-cancellation, eight IO-compatibility and three facade tests pass; six old legacy tests were removed after moving meaningful coverage. On serial 31683481 the `.8` native-AOT `--expert-io` probe observed a 6,011 ms timeout, then same-connection INIT/getInfo and dispose/reopen/getInfo passed without operator interaction. Independent cross-vendor review: PASS. | All current macOS HID **input wait** routes use the event-delivery owner; macOS OTP feature reports are feature GET/SET, not input callbacks. Public synchronous open/get/set/dispose remain explicitly blocking. ISC-33 still needs discovery-manager callback quiescence; ISC-53 still covers other synchronous public waits. This does not establish universal ISC-4 (13 registered migrated-route operations exclude expert raw) or Windows/Linux hardware execution. |
| ISC-32 | Typed macOS FIDO output and public expert IO SET use the connection-owned FIDO worker; typed OTP feature GET/SET and the public expert `MacOSHidFeatureReportConnection` use the connection-owned OTP worker, not native callback report APIs. The feature facade now opens/reads descriptor metadata on that worker and forwards GET/SET and checked shutdown through it. It preserves expert synchronous caller blocking; one owned worker admits one operation at a time, without a process-global capacity bound. Expert GET owns an eight-byte array: short native replies (0–8) stay zero-padded, lengths greater than eight fault and zero the buffer. Typed OTP send remains exactly eight bytes; expert SET admits arbitrary length for native validation. Accepted native calls drain before checked close. Eleven feature-compatibility and nine IO-compatibility tests passed after the whole-array ownership fix (both expert GetReport facades return the owned array through `MemoryMarshal.TryGetArray` without a second uncleared copy; identity is tested). Independent review: PASS WITH NOTES. Native-AOT host built from this worktree using pinned `.8` passed `--expert-feature --serial 31683481` (3/3 eight-byte feature GETs plus read-only Management info via feature SET/GET and dispose/reopen, serial matched) and separately `--otp-info` (3/3 typed queries) on the same key. | Explicit connection-owned worker fallback for all identified macOS output/feature directions, not a claim that callback GET/SET APIs are asynchronous. Public expert open/Get/Set/dispose intentionally block; capacity is local to the connection, not proven process-global. ISC-33 still includes listener/manager callback quiescence; ISC-53 covers remaining public synchronous waits. No physical touch/unplug or other-platform native execution. |

**ISC-33 partial listener evidence, not acceptance:** `MacOSHidDeviceListener` now roots each manager/run-loop/callback generation through the listener thread's `finally` cleanup, unscheduling after `Run` returns before releasing mode, loop, manager and root. Stop wakes and waits outside the lock; concurrent Stop/Dispose share a monotonic deadline. Timeout retains the running generation without repeated eight-second waits; Start refuses a fresh generation until prior cleanup succeeds. Callback self-stop requests shutdown without joining itself; callback and logger exceptions are contained. Six `MacOSHidListenerLifetimeTests` (runtime-resilience cases) pass, including held callback, concurrent Stop/Dispose, cleanup failure and restart gating. The four existing `HidDeviceListenerIntegrationTests` passed on macOS/.NET 10.0.12 for Start, no-change, Dispose and platform type, **without** an actual arrival/removal callback. This still uses the existing IOHIDManager-owned run loop with 100 ms poll, not a new dispatch bridge; real native callback quiescence and other teardown paths remain unverified, so ISC-33 is unchecked. No operator hardware topology change was performed.

Other current bounded evidence: the Core-only registry has 13 required migrated-route operations with 26 valid named runnable Fact/Theory profile links; expert public raw IO and feature routes are not yet added operation rows. The targeted `BoundaryInventory` run passed **28** tests at the prior checkpoint: 13 scanner, three registry, six responsiveness, six diagnostics. Diagnostics inventory logging in three files and exercise real PC/SC and OTP sentinel paths, including full payloads and five-byte prefixes to catch report-fragment leaks; externally supplied exception messages and other migration logs are not comprehensively covered, so ISC-56 stays open. The registry covers macOS typed FIDO/OTP and portable PC/SC, not universal ISC-4. The current Core inventory assertion lists **198 outstanding sites**: 130 native imports, 24 waits, 15 scheduling, 21 pre-task-return dispatch gaps, six callback registrations, zero delegate conversions and two unmanaged callback addresses; these are documented, not cleared. The 200-site breakdown belongs to the earlier feature slice.

The **pre-expert-facade** `.8` current profile schema 2 completed two warmups, ten normal fresh-child samples and one idle sample on macOS 15.7.7 arm64 / .NET 10.0.12; [dataset](artifacts/measurements/current-profile-20260924T051049865Z.json) SHA-256 `9bbd8837afab35e1143385bc6e383a9baf330a5c5ce20de5892b2090b7ba1f65`. Normal medians: caller return 5.00675 ms, operation completion 24.84525 ms, disposal 2.90555 ms, allocated 60,004 bytes; idle CPU 1.945 ms and idle allocations zero over 1,001.7302 ms. Native-only duration and pending ordinary count are null with instrumentation-unavailable reasons; this dataset does not measure either expert facade, the historical `.3` pair is not comparable, and ISC-62 remains unchecked. OTP partial-send/read faults attempt abort once after attempted write, without replay of the original command; successful abort permits reuse and failed abort faults the protocol preserving the original failure. 35 protocol tests and one scripted Mac OTP test passed at the earlier checkpoint; physical mid-frame failure remains untested.

**Last full Core run:** 1,457 passed/3 skipped after the shared-stop fix. PublicApi 22 and resilience-fast 83 passed (up from 77 with six listener cases); YubiOtp 180 passed at the prior feature checkpoint, while Fido2 471 and the 35 focused OTP protocol tests passed earlier. After the whole-array fix, targeted feature and IO compatibility runs passed 11 and nine respectively. `dotnet toolchain.cs complexity` passed 33 changed shipping methods at cyclomatic ≤10/cognitive ≤20; manually split verification/harness/test methods were excluded and are not tool-certified. The prior targeted 28-test inventory run is recorded above, not represented as a current rerun. No new touch or unplug at `.8`; physical removal remains `.6`-only. Windows/Linux native runtime and hardware tests are deferred pending user direction; packaged consumer jobs do not prove them.

### First smart-card lifetime slice — 2026-09-22

Implemented and committed at `db7a1bf6` over `a7f2cae8`. No NativeShims source/package
or applet protocol bytes changed. Key source: `PcscConnectionNativeState.cs` owns native
execution, partial opening, transaction generations/end intents, rooted native state
and checked cleanup; `ISCardConnectionApi.cs` supplies the native test seam. Factory,
registry and both discovery entry paths retain unproven claims. The public
`UnrecoveredConnectionException` documents that state. Synchronous transaction surface
is retained and now uses the same owner; an async public acquisition remains deferred.

| Command / evidence | Observed result and limit |
|---|---|
| Historical pre-split invocation: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~CoreAsyncBoundaryTests"` | 34 passed, zero failed/skipped at the original slice verification point. The class was subsequently split; this command is retained only as historical evidence and is no longer an active gate. |
| `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~UnrecoveredRegistration_NewAndWaiting"` | One passed; fresh typed quarantine failures for current and already-waiting callers. |
| `dotnet toolchain.cs -- test --project Core` | 1,334 total: 1,331 passed, zero failed, three existing platform/hardware-dependent skips. |
| `dotnet toolchain.cs -- test --project PublicApi` | 22 passed, zero failed/skipped. |
| `dotnet toolchain.cs -- resilience --fast` | 77 passed, zero failed/skipped; overlaps the Core suite. |
| `dotnet toolchain.cs -- build --project Core.IntegrationTests` | Successful compile, zero warnings/errors. |
| `dotnet toolchain.cs -- test --integration --project Core --smoke --filter "FullyQualifiedName~PcscLifetimeIntegrationTests"` | One hardware test passed. The unit project had no matching tests, not an additional hardware skip/pass. |
| `dotnet toolchain.cs native-aot-contract-qa` | Ten shipping library opt-ins/references/anchors validated; static evidence only. |
| `dotnet publish verification/NativeAotVerification/Yubico.YubiKit.NativeAotVerification.csproj -c Release -r osx-arm64 --self-contained -p:PublishAot=true` | Native publish succeeded with no warnings reported. This is the repository-documented independent publish path, not a toolchain target. |
| Run `./Yubico.YubiKit.NativeAotVerification` from its `bin/Release/net10.0/osx-arm64/publish` directory | Process exited successfully; discovered three YubiKeys. Default discovery mode does not assert which native adapter paths executed and does not satisfy the full ISC-58 harness. |
| `dotnet toolchain.cs docs-qa`; `git diff --check` | Passed. |

**Test sensitivity and review.** The initial overlapping-transmit test failed against
the old behavior (expected InvalidOperationException; no exception), then passed after
implementation. A later engineer iteration initially failed compilation; that is not
behavioral red evidence. A separate negative control restored only the old published-
device discovery routing defect and ran
`dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~PublishedDevicePcscDiscovery_UnprovenDisconnectRetainsOriginalClaimAndNativeRoot"`:
one test failed with expected UnrecoveredConnectionException/no exception. Restoring
the fixed source passed the same test. The restored file's before/after SHA-256 was
`800135e97662517412ffd8144e79e583453eadb0540137d5a5328a53c8ef6828`.

Independent Anthropic review of the OpenAI engineer's code required three rounds and
finished **PASS WITH NOTES**, with no remaining high/medium findings. It covered source
fit as well as correctness. Fixes included transaction reuse, begin/shutdown races,
both discovery lease routes, pre-acquisition failures, worker startup/exit, explicit
native-state rooting, custom factories and isolated failure probes. Full review packets
were generated with a temporary index under the approved temporary directory; the
user's real staged index was not altered.

**Permanent-failure probes.** Child-process tests cover failed disconnect, failed
context release, published-device variants, late discovery proof, and a custom factory
delegating to a built-in partial open. They use fake SafeHandle subclasses rather than
passing fabricated pointers to real native finalizers, force collection, and assert
retained state/claims. Each child has a watchdog, explicit `PROBE-COMPLETE:<case>` marker
and exactly one passing child test. Logs are `yubikit-pcsc-*-probe.log` in the
system temporary directory; these are local artifacts, not durable release evidence.

**Selected hardware.** The user allowed any attached key. Read-only identification
reported K1 as YubiKey 5C NFC, firmware **5.7.4**; the existing allowlist already includes
it. The new integration test selected that authorized USB-C fixture and asserted stable
serial/firmware while performing **three connection/disposal cycles and two sequential
transaction/device-info reads per connection**. No reset, configuration write, credential
operation, touch ceremony or unplug was performed. Host: macOS 15.7.7, arm64, .NET 10.0.0;
then-existing NativeShims 1.16.1. This hardware result predates the refactor and 1.18.0
upgrade and is not reusable as 1.18.0 hardware evidence. Serial numbers remain out of this planning record.

During that hardware run, discovery initialization skipped the non-selected 5.4.3
fixture after an unresolved HID discovery read. The selected smart-card scenario passed;
this observation is retained for follow-up rather than described as full multi-device
or HID acceptance. The separate native-compiled discovery smoke later found all three
keys, but did not identify per-adapter execution or resolve the earlier observation.

**Remaining limits / deferred to implementation effort.** Non-success native release
codes remain conservatively unrecovered until their platform semantics are established;
process restart can be required. Low notes from the original review concern the internal
test starter's throw-before-start contract, failed-end diagnostic specificity and a
child-process kill diagnostic race. The subsequent structural refactor centralized slot
selection and registration-owning smart-card opening before commit. Windows/Linux native
execution, interruption/touch/removal hardware scenarios,
clean packaged consumers, performance budgets and whole-epic inventory remain pending.
No universal epic criterion is checked from this local slice evidence.

### Craftsman refactor evidence — 2026-09-22

This follow-up is a structural review of the first single-key smart-card slice, not a
claim that the broader platform, inventory, scheduling, performance, or packaging work
is complete. The built-in `SmartCardConnectionFactory` is sealed; external customization
continues through `ISmartCardConnectionFactory`. The slot selects the internal
`IRegisteredSmartCardConnectionFactory` capability directly, so both public connection
opening and direct/published discovery use one registration-owning entry point without
making registration ownership part of a shared slot interface.

`PcscNativeResources` now contains only reader/API references, context/card/protocol
state, and synchronous open/transmit/transaction/release operations. It has no lock,
task, worker, observer, registration, or self-root. `PcscConnectionNativeState` retains
those lifetime and scheduling responsibilities, zeroes release proof before open, and
marks proof only after checked disconnect and context release.

The active focused gates are separate invocations for
`PcscConnectionLifetimeTests`, `SmartCardConnectionFactoryTests`, and
`PcscDiscoveryLifetimeTests`. They now contain 17, 13, and 8 cases respectively: **38
total**, preserving the original 34 behaviors and adding successful external-factory
dispose/reopen ownership plus canonical smart-card logging coverage. Finalizer assertions
run inside watchdog-bounded child processes because two full-suite attempts previously
stalled in synchronous
`GC.WaitForPendingFinalizers`; isolation bounds the suite, but does not prove which other
finalizer or scheduling interaction caused those stalls. Child success still requires
the finalizer assertions, one passing filtered test, and an explicit completion marker.

Current managed verification after the refactor: Core build compiled all three matching
projects with zero warnings/errors; the full Core run reported 1,338 total, 1,335 passed
and three existing skips; PublicApi reported 22 passed; and the fast resilience gate
reported 77 passed. No native runtime or hardware scenario was rerun for this structural
follow-up, so the earlier separately recorded native/hardware evidence is not upgraded.

Applicable first-slice evidence covers one connection's open, held transmit,
predispatch/post-dispatch cancellation, overlap refusal, transaction end/shutdown,
partial-open cleanup, unrecovered quarantine, late release proof, factory ownership,
finalization, and safe reopen through controlled native seams. It does not establish
all-platform native runtime behavior, complete lifecycle inventory, asynchronous public
transaction acquisition, cross-key capacity/fairness, removal/touch recovery, package
provenance, or whole-epic closure. All 72 criteria were unchecked at that first-slice
checkpoint; ISC-49/52 were subsequently verified independently.

### Historical first-iteration checkpoint and design review

The user approved and committed the first smart-card lifetime iteration at `db7a1bf6`
before extending implementation to HID. Smart-card logging now uses `YubiKitLogging` exclusively;
the sealed built-in factory exposes a parameterless constructor and `CreateDefault()`.
Custom factories continue to implement `ISmartCardConnectionFactory`.

Before implementing the next slice, reassess HID designs against the established lessons:
create the lifetime owner before native open; keep one lifetime authority distinct from
its native-resources helper; admit no ordinary backlog; treat public task termination as
the borrowed-memory release boundary; and release physical ownership only after checked
cleanup, otherwise quarantine it. Apply those rules by route without pre-extracting a
generic scheduler: native completion, readiness and callback ownership remain platform-
specific choices.

At that checkpoint no HID route or exact new interface was approved. `IHidConnection` joins
`IHidDevice`/`IFindHidDevices`, both FIDO/OTP wrappers and six platform implementations;
wholesale conversion or internalization is broader than one route. The proposal is a
route-local internal async seam while leaving the common/public surface unchanged until
a separate compatible migration walkthrough approves otherwise.

The then-proposed next action was a macOS persistent-input native-owner prerequisite and
bounded design checkpoint, not production migration. Approved A3 direction still requires
persistent event delivery, accepted-operation drain, event-quiescence acknowledgment,
bounded copied reports, overflow faulting and late-callback generation isolation. Moving
the current per-call run-loop pump to a worker is not sufficient. An owned persistent run
loop remains a fallback only with explicit rationale. Dispatch cancellation-handler
ownership requires the planned native block bridge, and the report-buffer bound remains
pending checkpoint selection. The input harness proposal covers
register/activate/input/removal/cancel acknowledgment, late callbacks/generation and
Native AOT execution; GET/SET callback timeout behavior remains a separate per-direction,
per-version runtime experiment and may use the approved blocking fallback meanwhile.

D28 resolves the product-direction fork: use modern dispatch APIs across the current .NET
10 upstream-supported macOS versions rather than an old-API fallback solely for a macOS 12
binary minimum. NativeShims 1.18.0 is selected and consumed; exact producer binding remains
unknown. Tag `cb5275e…` is the recommended aligned source base for the proposed bridge, but
the concrete contract/base still required approval at that checkpoint. The dependency
change alone did not authorize HID implementation, native bridge edit or hardware operation;
D30 and later evidence supersede that proposed next step.

Applicable stable mappings are ISC-9–15, ISC-20, ISC-26–28, ISC-31–33, ISC-50,
ISC-52–56 and ISC-60–63, with ISC-41–48/ISC-58 only if affected. A Windows FIDO
alternative additionally maps ISC-16–17 and ISC-29–30, with ISC-50/ISC-52 retained.
ISC-40 applies only if a separate Windows OTP scope is authorized. This proposal checks none.
Scheduling overhead and end-to-end performance remain unmeasured.

### Initial intake evidence — 2026-09-21

- Orchestrator fetched `origin/yubikit`; worktree branch is `yubikit-async-boundaries`; `HEAD` is `a7f2cae8c32ad6e0ada55e404f85442f6a266f6c`.
- `git diff --exit-code 99e082c46018ecdde63c3c47980c14e313b70192 HEAD -- src/Core src/PublicApi Directory.Packages.props toolchain.cs .github/workflows/build.yml .github/workflows/native-aot.yml` exited 0. Entire Core, including tests, and the `src/PublicApi` test project are unchanged in that scope. Applet public surfaces were outside that command and require S0 revalidation; the Fido2 `PreviewSignGeneratedKey` extension and its public baseline changed since the demo baseline.
- Current source was re-read at the line anchors listed in Problem. These observations classify the starting point only; they do not prove replacements.
- Main build workflow is Ubuntu. Native AOT workflow is `macos-latest`/`osx-arm64` and performs no-hardware discovery only.
- `Directory.Packages.props:23` references NativeShims 1.16.1.
- Native checkout `/Users/Dennis.Dyall/Code/y/worktrees/nativeshims-static-core` is clean at `411e5a9bd8fda49cccfb5a3423e41bd1ba3062de` on unrelated `feature/nativeshims-static-core`, with the same upstream remote. It is not an approved implementation base.
- Local tag 1.16.1 resolves to candidate release reference `9dffc9d5ad752ac9203c1ca47f343294957e8924`; `git show --stat --oneline 1.16.1` points to a consumer lockfile repin. Its inspected nuspec has placeholder version `1.0.0` and declares seven RIDs; its CMake input builds shared PC/SC/OpenSSL wrappers. Release-job provenance, consumed artifact inspection, support floors, and package-producing revision remain unverified; ISC-41 is pending.
- `docs/demo` is absent on the integration base. S6 must scan existing docs/examples and separately record any still-active demo-branch claims rather than assume demo content merged.
- Constraint store `/Users/Dennis.Dyall/.local/share/steward/constraints/records` contains only `.gitkeep`; no verified constraint record is available.
- As of the initial intake pass, no SDK/native builds, runtime tests, responsiveness experiments, benchmarks, native runtime execution, or device access had been performed. Subsequent existing-test execution and artifact inspection are recorded under S0 intelligence below; no implementation criterion is complete.
- Planning review completed with an architect, an engineer editing the plan, and a separate reviewer. Review restored explicit native lifetime rules and clarified shared-file handoffs; the revised plan passed review for starting S0. This is planning acceptance only.
- Document checks confirmed 12 ordered sections, 72 unique unchecked criteria with one probe mapping each, complete criterion coverage in the 11-slice dependency graph, and all 14 historical source references. The working copy is at the worktree root because the repository's `Plans/` ignore rule also matches new `docs/plans/` files on this case-insensitive filesystem. The original handoff remains untouched.

### S0 intelligence evidence — 2026-09-21

This is an independently searched, manually inspected **starting inventory at `a7f2cae8`**, not the semantic scanner or a closed acceptance manifest. The orchestrator owns these classifications; engineers supplied read-only evidence on native provenance and existing tests. No runtime, test, build-script, interface, or seam source was changed during that pass. The initial-intake statements above describe the earlier point in time; the tests and package inspection below happened subsequently.

#### Revision, scope, and source census

- `git fetch origin yubikit` followed by `git rev-parse HEAD origin/yubikit` returned `a7f2cae8c32ad6e0ada55e404f85442f6a266f6c` for both. There were no incoming commits to integrate.
- Applet delta review used `git diff 99e082c46018ecdde63c3c47980c14e313b70192 HEAD -- src/Management/src src/Piv/src src/Fido2/src src/WebAuthn/src src/Oath/src src/YubiOtp/src src/OpenPgp/src src/SecurityDomain/src src/YubiHsm/src`. Only `src/Fido2/src/Extensions/PreviewSign/PreviewSignGeneratedKey.cs` and its `PublicAPI.Unshipped.txt` differ: the demo-only `FromArkgSeedKey` static factory is absent on the integration base. The class itself remains. All other source in these nine roots matches; test/example differences outside these roots are not included in this assertion. This closes source-delta revalidation, not a whole-applet behavior audit.
- Primary shipping scan scope: `src/{Core,Management,Piv,Fido2,WebAuthn,Oath,YubiOtp,OpenPgp,SecurityDomain,YubiHsm}/src/**/*.cs`. The nine applets were searched independently of Core. Test projects, example executables, command-line tooling, benchmarks, verification hosts, generated `obj`/`bin`, and native source from the other lineage are separate evidence scopes, not silently included as shipping-library imports. Public raw connections and Core console credential helpers remain in scope.
- An initial wildcard search selected no sources. It was discarded; the census below used ten explicit source roots and produced nonzero results. This is a concrete reason the future scanner must assert its input set.
- Textual import discovery found **125 `LibraryImport`/`DllImport` declarations in 13 files**, all in Core. This counts declarations, including overloads, not distinct exports or runtime-reachable calls. Conditional compilation, runtime-owned native calls, wrappers, dynamic lookups, and callback lifetimes still need semantic/native analysis. These numbers do not satisfy ISC-1 through ISC-6.

Paths in this census are relative to `src/Core/src/`:

| Source file | Import declarations | Initial disposition |
|---|---:|---|
| `Cryptography/ArkgPrimitivesOpenSsl.cs` | 12 | Local cryptographic native work; classify memory/lifetime separately from hardware waits. |
| `Cryptography/CmacPrimitivesOpenSsl.cs` | 5 | Local cryptographic native work; same distinction. |
| `Native/Desktop/SCard/SCard.Interop.cs` | 11 | Synchronous NativeShims connection, transaction, monitoring, and release boundaries. |
| `Native/Windows/Cfgmgr32/Cfgmgr32.Interop.cs` | 12 | Discovery/property queries and notification registration/unregistration. |
| `Native/Windows/HidD/HidD.Interop.cs` | 8 | Capabilities/metadata and synchronous feature reports. |
| `Native/Windows/Kernel32/Kernel32.Interop.cs` | 3 | `CreateFileW`, `WriteFile`, `ReadFile`; current declarations/callers use synchronous report completion. |
| `Native/Windows/WinSCard/WinSCard.Interop.cs` | 1 | Windows-specific smart-card discovery/device-property boundary. |
| `Native/MacOS/IOKitFramework/IOKitHid.Interop.cs` | 18 | Manager/device lifecycle, report operations, run-loop scheduling, callback registration. |
| `Native/MacOS/IOKitFramework/IOKitFramework.Interop.cs` | 4 | Registry/service discovery and release. |
| `Native/MacOS/CoreFoundation/CoreFoundation.Interop.cs` | 11 | Run-loop execution/control and native object handling. |
| `Native/Linux/Udev/Udev.Interop.cs` | 21 | Enumeration, monitoring, native objects and cleanup. |
| `Native/Linux/Libc/Libc.Interop.cs` | 10 | Open/close/ioctl/read/write, poll/eventfd/fcntl, including overloads. |
| `Native/NativeMethods.cs` | 9 | Platform library loading, symbol lookup and unloading; do not omit dynamic entry resolution. |

Reproduce the declaration census without introducing a scanner:

```sh
rg --count-matches '\[(LibraryImport|DllImport)\(' src/Core/src src/Management/src src/Piv/src src/Fido2/src src/WebAuthn/src src/Oath/src src/YubiOtp/src src/OpenPgp/src src/SecurityDomain/src src/YubiHsm/src --glob '*.cs'
```

The same explicit roots yielded 24 textual candidates across 17 Core files for the invocation-shaped pattern below. This excludes method-group references: `DesktopSmartCardDeviceListener.cs:59` additionally passes `Thread.Sleep` into its injected backoff delegate and must be classified. This is **not** a count of blocking defects: `ProtocolDeviceInfo.cs:484` is a nonblocking `SemaphoreSlim.Wait(0)` admission attempt. Separate searches found three listener `Thread.Join` sites and asynchronous delays/admission waits. No matching direct import or scheduling/wait expression was found in the nine applet source roots for these patterns; that does not prove their call paths nonblocking.

```sh
rg --count-matches 'Task\.Run\(|Task\.Factory\.StartNew\(|\.GetAwaiter\(\)\.GetResult\(|\.Wait\(|\.Result\b|Thread\.Sleep\(|new Thread\(' src/Core/src src/Management/src src/Piv/src src/Fido2/src src/WebAuthn/src src/Oath/src src/YubiOtp/src src/OpenPgp/src src/SecurityDomain/src src/YubiHsm/src --glob '*.cs'
```

#### Boundary families and current execution owners

Paths below are relative to `src/Core/src/`. Smart-card dispositions are `a7f2cae8`
baseline observations superseded where the first slice changed them. Reinspection at
`db7a1bf6` confirms that synchronous HID opening and the macOS per-call run-loop pump
remain live defects. Other rows remain baseline inventory, not unreviewed current-tree
claims or approved replacement designs.

| Family / source anchors | Current behavior | Outstanding evidence or classification |
|---|---|---|
| Opening: `Devices/HidConnectionSlot.cs:48-70` | `OpenRawConnectionAsync` calls synchronous `ConnectToIOReports`/`ConnectToFeatureReports` while constructing the result passed to `Task.FromResult`. Predispatch cancellation is checked. | Factory responsiveness includes native constructors/open, not just report methods. |
| Report adapters: `Protocols/Fido/Hid/FidoHidConnection.cs:32-60`; `Protocols/Otp/Hid/OtpHidConnection.cs:32-62` | Copy/zero send buffers around synchronous report calls; receive is synchronous before returning a task. | Existing protocol fakes bypass these wrappers. Borrowed-memory terminal probes are absent. |
| Windows reports: `Native/Windows/HidD/HidDDevice.cs:65-109,155-183,214-222` | FIDO uses read/write access and synchronous `ReadFile`/`WriteFile`; OTP uses zero desired access and feature reports. | Open, report, removal and release each need disposition; no owned overlapped completion path currently exists. |
| macOS reports: `Transports/Hid/MacOS/MacOSHidIOReportConnection.cs:90-145,161-175,178-219` | Caller run loop waits up to six seconds; output calls `IOHIDDeviceSetReport`; report/removal callbacks are registered, removal callback is empty, queue is unbounded. | Persistent event ownership, bounded buffer policy and callback quiescence are unresolved implementation choices. |
| Linux reports: `Transports/Hid/Linux/LinuxHidIOReportConnection.cs:37-114`; `LinuxHidFeatureReportConnection.cs` | `O_RDWR` open, synchronous read/write and feature ioctls. | Existing eventfd/poll declarations and listener code are not a report-read reactor. |
| FIDO configuration: `Protocols/Fido/Hid/FidoHidProtocol.cs:48-67,115-121`; `Devices/ProtocolDeviceInfo.cs:444-467` | `Configure` synchronously waits for initialization; explicit async initialization and lazy initialization already exist elsewhere. | Public/factory callers must be traced before choosing initialization changes. No new configuration API is selected. |
| Smart-card lifecycle: `Transports/SmartCard/UsbSmartCardConnection.cs:54-75,86-114,116-155,165-185,198-287` | Connect and transmit use per-call `Task.Run`; begin uses `Task.Run` plus synchronous wait; end is synchronous; async dispose offloads sync dispose. Failed initialization cleanup also calls native handle disposal. | Full connection lifecycle is outside current `ISCardApi` test coverage. Public transaction acquisition/release shape needs a decision. |
| Native release: `Native/Desktop/SCard/SCardContext.cs:36`; `SCardCardHandle.cs:37-38` | SafeHandle release invokes native context release/disconnect directly. | Explicit dispose and finalizer paths belong to the inventory; offloading transmit alone does not classify release. |
| Discovery enumeration: `Transports/Hid/FindHidDevices.cs:33`; `Transports/SmartCard/FindPcscDevices.cs:35-62` | HID uses shared-pool `Task.Run` with the caller token and no shared admission slot. Public PC/SC enumeration throws `InvalidOperationException` when its shared discovery admission is saturated. After admission it starts a synchronous `LongRunning` job with `CancellationToken.None` and awaits it unconditionally; caller cancellation is checked only before admission. | These observable policies differ from queued identity reads. Capture their compatibility implications before changing admission/cancellation. Do not label all `LongRunning` jobs asynchronous-delegate defects. |
| Discovery metadata: `Devices/ProtocolDeviceInfo.cs:225,349-411,475-511` | Four process-wide admitted workers; an async `LongRunning` delegate retains admission through its awaited operation, but not dedicated-thread execution. Identity reads may wait asynchronously for capacity; caller budget and epoch retirement are distinct. | Pending capacity and interactive reservation are not the proposed bounded-executor contract. Existing four-worker limit is a baseline fact, not the new default. |
| Discovery cleanup: `Transports/SmartCard/FindPcscDevices.cs:74-91,135-138` | List-reader failure/empty returns occur before the explicit context-disposal `try/finally`; SafeHandle finalization is then the remaining cleanup path. | Record as a current lifecycle coverage gap to reproduce before any fix; no cleanup change made. |
| Listener start/stop: `Transports/Hid/Windows/WindowsHidDeviceListener.cs:98-102,129-177`; macOS listener `:114,151`; Linux listener `:97,134`; smart-card listener `:99,185` | Windows native notification callback; owned macOS/Linux/PCSC listener threads; synchronous joins on stop; Windows unregistration on stop/finalization. | Review callback/drain ownership per platform, including return codes and retained context. Existing listener threads are not new report-completion owners. |
| Callback delivery: `Transports/Hid/HidDeviceListener.cs:113`; `DesktopSmartCardDeviceListener.cs:439` | Listener callbacks invoke delegates synchronously. Manager wiring uses them to signal a rescan; this differs from the asynchronous public device event stream. | Distinguish internal signal handlers from externally supplied delegates before claiming no application code can run on native/worker stacks. |
| Monitoring: `Devices/YubiKeyDeviceMonitorService.cs:360,464-491,692-694,843-863` | Pool-launched async monitor, explicit synchronous stop wait, time-bounded asynchronous shutdown/publication waits and generation retirement. | Retained synchronous API versus asynchronous teardown must be classified by reachable route; abandoning a generation is not native completion. |
| Disposal forwarding: `Protocols/Fido/Hid/FidoHidConnection.cs:71-74`; OTP wrapper `:73-76`; Windows report adapters `:79-82` | Async disposal directly invokes sync disposal. Linux/macOS transport disposal uses `Task.Run`, but wrappers can bypass that asynchronous route by calling their sync `Dispose`. | This is a separate caller-blocking path even after report methods change. |
| Explicit synchronous lifetime: `Utilities/ExchangeGuard.cs:95`; `Devices/DisposalGate.cs:32-40`; `Devices/YubiKeyManager.cs:257` | Synchronous drain/disposal/shutdown facades. `DisposalGate.DisposeAsync` invokes the selected teardown inline until it yields. | Preserve documented sync boundaries; inspect async delegate prefixes rather than treating the gate itself as offloading. |
| Teardown fault and lease release: `Devices/DisposalGate.cs:53-70` | The gate releases its lease in `finally` after teardown completes or faults; it does not independently establish native quiescence. | Gate 2 proposes explicit release evidence before physical reuse if a new async native operation remains unresolved. This is a source-level design constraint, not a reproduced current native-lifetime failure. |
| Explicit console input: `Credentials/ConsoleCredentialReader.cs:71-81,125-130,147-161` | Synchronous credential-reader API, redirected `ReadLine`, interactive ten-millisecond sleep polling. | Classify as console input rather than changing prompting policy during transport migration. |
| Dynamic loading: `Native/{Windows,MacOS,Linux}/*UnmanagedDynamicLibrary.cs`; `Native/NativeMethods.cs:48-96` | Native load/lookup/unload plus `Marshal.GetDelegateForFunctionPointer`; runtime/SafeHandle-owned native calls also exist outside handwritten imports. | Textual declaration counts do not establish complete native coverage or symbol reachability. |

Baseline timing/resource constants additionally found: two-second identity-attempt budget (`DiscoveryIdentityReader.cs:41`), three-second metadata budget (`FindYubiKeys.cs:85`), five-second monitor interval (`YubiKeyDeviceManager.cs:39`), ten-second shutdown bound (`YubiKeyDeviceMonitorService.cs:72`), and 256 device-event slots per watcher (`DeviceEventHub.cs:51`). The event-watch buffer is not a HID-report buffer. No new worker, queue, latency, or operating-system support budget has been chosen.

#### Existing test seams and route coverage

| Existing asset | What it can already exercise | What it cannot establish |
|---|---|---|
| `src/Core/src/Native/Windows/HidD/IHidDDevice.cs:17-31` and `WindowsHidConnectionOpenFailureTests` | Synchronous open/report/dispose interface; current tests cover constructor-failure cleanup. | Owned asynchronous request state, delayed completion, cancellation races or completion-before-free. |
| `src/Core/src/Transports/Hid/MacOS/IIOKitDeviceLifetime.cs:22-95` and `MacOSHidConnectionLifetimeTests` | Device creation/open/release and callback registration; failure/finalization/disposal tests. | Report operations and run-loop scheduling are deliberately excluded (`:36-38`); it cannot withhold native report completion or acknowledge quiescence. |
| Linux `ILinuxHidEventSource` | Listener discovery/event injection. | Report open/read/write/ioctl operations; those currently call native methods directly. |
| `src/Core/src/Transports/SmartCard/ISCardApi.cs:19-32` | Establish context, list readers, status-change waits and cancel for discovery/listener tests. | Connect/reconnect/transmit/begin/end/disconnect/release through `UsbSmartCardConnection`. Its current unit tests mainly check transport/extended-packet mapping. |
| `ExchangeGuardTests`, protocol concurrency tests, `RawSessionYubiKeyExtensionsTests`, `ApplicationSessionDisposalTests` | Managed admission, shared completion, logical drain and reuse. | Platform native completion, handle/pointer lifetime, driver cancellation. |
| `FidoHidProtocolTests`, `OtpHidProtocolTests`, `UserPresenceNotificationTests` | Packet-level cancel/reset/reuse, zeroing, and prompt pairing/exception precedence through fakes. | The migrated production-adapter route or instrumented borrowed-memory accesses after public completion. |
| `AsyncSurfaceConventionTests`, `RuntimeResilienceStaticScanTests`, `crap.cs` | Reflection conventions, bounded source heuristics/negative fixtures, and Roslyn syntax-tree complexity extraction respectively. | None supplies semantic known-native-leaf/direct/interface-wrapper reachability. Their existence does not settle scanner design. |

Applet routes were traced through each module's `src/IYubiKeyExtensions.cs`; the order below is selection precedence, not permission to retry another transport after an uncertain operation:

| Module | Current routes | Source anchor |
|---|---|---|
| Management | SmartCard, then FIDO HID, then OTP HID; explicit selection and secure-channel rules | `src/Management/src/IYubiKeyExtensions.cs:101-144` |
| Piv | SmartCard | `src/Piv/src/IYubiKeyExtensions.cs:38-63` |
| Fido2 | FIDO HID, then SmartCard; explicit selection and secure-channel rules | `src/Fido2/src/IYubiKeyExtensions.cs:124-190` |
| WebAuthn | Delegates to Fido2, no separate transport implementation | `src/WebAuthn/src/IYubiKeyExtensions.cs:40-79` |
| Oath | SmartCard | `src/Oath/src/IYubiKeyExtensions.cs:39-63` |
| YubiOtp | SmartCard, then OTP HID; explicit selection and secure-channel rules | `src/YubiOtp/src/IYubiKeyExtensions.cs:102-145` |
| OpenPgp | SmartCard | `src/OpenPgp/src/IYubiKeyExtensions.cs:37-62` |
| SecurityDomain | SmartCard | `src/SecurityDomain/src/IYubiKeyExtensions.cs:45-70` |
| YubiHsm Auth | SmartCard | `src/YubiHsm/src/IYubiKeyExtensions.cs:41-66` |

`RawSmartCardSession`, `RawFidoHidSession`, `RawOtpHidSession`, and typed raw connections add distinct entry routes. `UsbSmartCardConnection.Transport` maps both USB and NFC (`:290-295`), so the class name must not exclude NFC from coverage. Existing multi-transport selection tests in Management/Fido2/WebAuthn/YubiOtp and applet wire/presence tests are useful regressions, not native boundary evidence. The public compatibility scope also includes `IHidDevice.ConnectToIOReports`/`ConnectToFeatureReports`, not only `IHidConnection`: see `src/Core/src/PublicAPI.Unshipped.txt:632-639,683`.

#### Earlier signed NativeShims 1.18.0 dependency evidence — 2026-09-23

The Dependency Engineer first changed only `Directory.Packages.props:23` from 1.16.1 to
1.18.0; no lockfile changed. This evidence predates the local `.2` preview pin:

| Evidence | Result and limitation |
|---|---|
| Package identity/signature | SHA-512 `7bf7866ead06877547c4b5056c1ba84358cac1e01ee7ae85acdd65d1105dac54782ef3d3f014c14a920871b1d259efcdbbc5fb16063d00878b71fdc018a8c43d`. Author and NuGet repository signatures verified; NU3028 reported an incomplete certificate-revocation check, so do not overstate revocation evidence. |
| Restore | `dotnet toolchain.cs restore` passed; generated assets contain 1.18.0 and no 1.16.1. |
| Managed regressions | `dotnet toolchain.cs -- test --project Core` reported 1,335 passed/3 skipped; `dotnet toolchain.cs -- test --project PublicApi` reported 22 passed. Focused results: Core `CryptographyProviderExtensionTests` 11 and `ArkgP256Tests` 3; Fido2 `PreviewSignGeneratedKeyTests` 3. These prove managed/local native-crypto paths, not device PC/SC or HID behavior. |
| Native AOT publish | `dotnet publish verification/NativeAotVerification/Yubico.YubiKit.NativeAotVerification.csproj -c Release -r osx-arm64 --self-contained -p:PublishAot=true` passed. The static shim linked, PCSC.framework remained a dependency, and no shared shim was published. The output was not executed and no hardware was used. |
| Packaged native shape | macOS x64/arm64 artifacts declare minimum macOS 12 with SDK 14.5. Both retain all 11 `Native_SCard` exports; `pcsc.c`, `native_abi.h`, macOS export lists and PCSC.framework/libSystem dependencies are unchanged from 1.16.1. New static-runtime archives are present across all packaged native platforms. Header/export inspection is not driver/runtime verification. |
| Deployed artifact check | The deployed arm64 shim's partial digest `96feaab5…` matches the 1.18.0 cache and differs from old `db45ce49…`; these are deliberately recorded as partial identifiers, not fabricated full hashes. |
| Source binding | Tag `1.18.0` at `cb5275ea8c151b7ad0bc9465ff2f9fa24785d3b3` is inspected aligned source evidence, but package metadata contains no producer commit. Exact package-producing binding remains pending. Any bridge native edit still requires explicit contract/base approval. |

The modern macOS HID path remains direct managed IOKit; NativeShims 1.18.0 adds no HID
exports or dispatch bridge and does not make blocking PC/SC calls asynchronous or abortable. Windows,
Linux and Intel macOS runtime behavior, device PC/SC/HID behavior and all hardware scenarios
remain unverified for 1.18.0. Historical macOS hardware evidence predates both the smart-card
refactor and this upgrade and is not a 1.18.0 hardware pass.

#### Historical NativeShims 1.16.1 provenance and capability evidence

The inspected artifact was the [GitHub release package](https://github.com/Yubico/Yubico.NET.SDK/releases/download/1.16.1/Yubico.NativeShims.1.16.1.nupkg), retained locally at `/var/folders/gn/mh64zz5969j89_f5dffnvnb80000kt/T/opencode/nativeshims-1.16.1/Yubico.NativeShims.1.16.1.nupkg`; extracted binaries are under its sibling `extracted/runtimes/`. Temporary paths are investigation artifacts, not durable release storage.

| Evidence | Result and limitation |
|---|---|
| Package identity | SHA-256 `b89b539ff3953276cdc1bf3ec9f83d266888e32faad97125c85a447b9283aee7`, matching GitHub release metadata. `dotnet nuget verify <package> --all` exited 0 with Yubico AB author signature. |
| Content identity | `dotnet nuget verify` was run against the retained local GitHub-release package; it reported content hash `odGky7RVjFwP+UfHIpk/XpREauuNhp3JjYbDXnDBg4ufnYE8CL+PLuN40IBhUwPCoxB1lhyxx0rt/CDuI3w4OA==`, matching the tagged consumer lockfile. This value came from local artifact verification, not registration metadata. NuGet.org package bytes were not independently downloaded and compared; its registration metadata was queried. Exact producer provenance remains a separate unresolved claim. |
| Strongest producer candidate | [Run 25131796239](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/25131796239), successful workflow dispatch, head `29d38c6fe55dcf0a6810b79b664b1b73a66f38e4`, started `2026-04-29T20:21:07Z`, finished `20:38:19Z`. NuGet registration gives publication at `20:51:31.473Z`; later `9dffc9d5...` repins the consumer. Source/workflow files match between candidate producer and tag. |
| Missing binding | Dispatch version input unavailable; build logs return 410 and workflow package artifacts expired. Release attestation associates the digest with the tag, not the producing build. The default `gh attestation verify` attempt returned 404 for the requested provenance predicate. This is not verified build provenance; ISC-41 stays pending. |
| Packaged platforms | All seven declared binaries present: Windows x86/x64/arm64, Linux x64/arm64, macOS x64/arm64. Header/export inspection is not successful runtime loading on those systems. |
| Existing native surface | Export tables inspected with `nm`/`objdump` show the canonical 36 `Native_*` names: 11 synchronous PC/SC and 25 cryptographic wrappers. No async operation, callback submission, HID bridge, or quiescence export in this release. No separate automated cross-artifact export-count assertion was run. |
| macOS artifact floor | Both binaries declare minimum macOS 14.0; arm64 load command shows `LC_BUILD_VERSION`, `minos 14.0`, `sdk 14.5`. Links PCSC.framework and libSystem. This is an artifact requirement, not an approved product-support policy. |
| Linux artifact requirements | Both binaries depend on `libpcsclite.so.1`; highest observed GNU C Library symbol requirement 2.17. Candidate workflow targets 2.23 and contains older-distribution load tests, while native README says 2.28. These three facts need reconciliation, not an inferred support promise. |
| Windows requirements | Headers and static-runtime checks exist, but header subsystem versions do not prove the supported Windows floor. No Windows runtime test was run in this intelligence pass. |
| Reproducibility inputs | Candidate source uses CMake/vcpkg, Zig 0.15.2 for Linux, platform scripts, and shared-library output. Scripts use unpinned vcpkg state; exact dependency revisions cannot be reconstructed from repository inputs alone. |
| Existing native checks | Candidate source contains `Yubico.NativeShims/tests/expected_symbols.txt`, `check_exports.sh`, and `check_exports.ps1`; historical workflow metadata shows successful build/package jobs. No new old/new-consumer matrix or callback/lifetime harness is present or verified. |

Immutable source anchors for the investigation: [producer workflow](https://github.com/Yubico/Yubico.NET.SDK/blob/29d38c6fe55dcf0a6810b79b664b1b73a66f38e4/.github/workflows/build-nativeshims.yml), [native CMake input](https://github.com/Yubico/Yubico.NET.SDK/blob/29d38c6fe55dcf0a6810b79b664b1b73a66f38e4/Yubico.NativeShims/CMakeLists.txt), [canonical exports](https://github.com/Yubico/Yubico.NET.SDK/blob/29d38c6fe55dcf0a6810b79b664b1b73a66f38e4/Yubico.NativeShims/tests/expected_symbols.txt), [synchronous PC/SC wrappers](https://github.com/Yubico/Yubico.NET.SDK/blob/29d38c6fe55dcf0a6810b79b664b1b73a66f38e4/Yubico.NativeShims/pcsc.c), and [NuGet registration](https://api.nuget.org/v3/registration5-semver1/yubico.nativeshims/1.16.1.json).

#### Existing-test baseline and measurement limitations

Local host: macOS 15.7.7 build `24G720`, arm64; .NET SDK 10.0.100 and runtime 10.0.0. Test commands ran from the unchanged worktree revision in Release configuration through `toolchain.cs`:

| Exact command | Observed result | Evidence level |
|---|---|---|
| `dotnet toolchain.cs -- test --project Core` | 1,299 total; 1,296 succeeded; zero failed; three skipped; test-run duration 20.425 seconds. | Current managed baseline only. Skips: two SCP11 hardware tests and Windows notification registration. |
| `dotnet toolchain.cs -- test --project PublicApi` | 22 total/succeeded; zero failed/skipped. | Existing public shape conventions, not async body responsiveness. |
| `dotnet toolchain.cs -- resilience --fast` | 77 total/succeeded; zero failed/skipped. | Existing managed resilience gate; overlaps Core's suite, not 77 additional unique tests. |
| `dotnet toolchain.cs native-aot-contract-qa` | Ten supported libraries opt in and are referenced/anchored; two internal tooling projects excluded. | Static contract validation only; no native executable was published or run. |

An earlier Core invocation with a `|`-joined list of `FullyQualifiedName` filters matched zero tests and failed as intended by the zero-match guard. `toolchain.cs:1042-1108` translates `&` compounds, not that OR expression. The successful unfiltered Core run above replaced it; future dispatch packets must use supported filters or separate invocations. This is a tooling observation, not a request to change the runner.

The existing BenchmarkDotNet project (`benchmarks/**/Program.cs:25-49,68-88,94-240`) measures discovery and Management opening/session/device-info operations across transports and configures memory/threading diagnostics. It expects a YubiKey 5.8 fixture. It does not provide caller-return/native/recovery decomposition, bounded pending/active counters, cancellation/removal/transaction scenarios, or idle-work accounting. It was not run. The named test durations above are test-run timing, **not** an ISC-62 performance baseline. No new measurement harness was written, no numeric regression budget was frozen, and no hardware/reader fixture or remote Windows/Linux host was confirmed.

Source-only documentation leads from that pass: `docs/migration/v1-to-v2-gaps.md:356` claims the synchronous-wait search is empty, contradicted by `UsbSmartCardConnection.cs:102`; `docs/v2-highlights.md:38-46` contains an “Async all the way down” claim requiring qualification. The older Native AOT evidence in that pass cannot fill new boundary-test rows; the later 1.18.0 publish-only evidence is recorded separately above.

#### Decisions to bring to the user

These were the S0 questions. Later decisions supersede them where stated; unresolved
route-specific evidence still blocks only the affected work.

1. **Compatibility and raw-access scope answered:** D15 permits consistent breaking v2 changes and internalization; D16 retains both public raw sessions and raw connection operations, including public connection implementations and composition. No legacy-signature compatibility path is mandatory. Exact signatures/internalization closure remain to be presented, and the separate transaction-scope decision is due before S4.
2. **Baseline measurement scope:** which device scale, platforms, reader/firmware fixtures and application scenarios are required? Agree these before proposing harness code or numeric budgets; current defaults are observations, not selected budgets.
3. **Test-seam design:** present alternatives for the missing Windows/macOS/Linux report and full PC/SC lifecycle coverage, including whether to extend existing seams or introduce narrower ones. Do not silently extend `ISCardApi` or `IIOKitDeviceLifetime`.
4. **Support/provenance policy resolved in part by D28:** v2 follows modern APIs within the current .NET 10 upstream support matrix rather than preserving an old API solely for a macOS 12 binary target. Exact 1.18.0 package-producer binding remains unknown; bridge edits still require an approved contract and compatible base.
5. **Scanner and platform implementation:** present the bounded semantic-scanner approach, worker admission/queue policy, and each native completion bridge before engineers write them. The proposed graph does not authorize these detailed decisions.

No acceptance checkbox is changed by this pass: the source census is not semantic enforcement; current managed tests are not migrated native contract tests; package inspection is not producer proof or all-platform runtime loading.

#### Public-boundary map after D15 and D16

The source still matches `origin/yubikit` at `a7f2cae8` after another fetch. Read-only architecture review traced the following groups through the Core public baseline, applet factory declarations, and `docs/architecture/raw-access-tiers.md`. This is a bounded dependency map for the async migration, not permission for an unrelated whole-Core visibility sweep.

| Boundary group | Treatment and decision status | Evidence and dependency implications |
|---|---|---|
| Intentional consumer surface | Retain device discovery/identity, applet sessions and interfaces, options, credentials/presence services, and domain/key/value/error types needed by those contracts. Preserve meaningful semantic differences, including WebAuthn's facade role. | `IYubiKey`, `YubiKeyManager`, `IApplicationSession`, `SessionCreationOptions`; `docs/architecture/applet-public-api.md:3-34`. Public return/parameter/base types constrain visibility transitively. Do not internalize a value or exception merely because it resides in Core or a native namespace. |
| Raw logical sessions | **Approved public scope (D16):** retain `RawSmartCardSession`, `RawFidoHidSession`, and `RawOtpHidSession` as the deliberate lower-level operation surface. | `docs/architecture/raw-access-tiers.md:19-34,136-157` distinguishes framed/guarded logical exchanges from unguarded raw connections. Keeping raw sessions does not require every native transport seam to be public. |
| Platform/discovery/report machinery | Candidate internalization families: `IHidConnection`/`IHidDevice`, their finders/descriptors/listeners, and smart-card reader/find/factory/listener plumbing. Review platform helpers and protocol processors for the same consumer-use test. Exact member closure remains to be checked before changes. | `src/Core/src/PublicAPI.Unshipped.txt:582-641,666-710` exposes these families. `FidoHidConnection` and `OtpHidConnection` are already internal wrappers, so lower-level `IHidConnection` visibility is distinct from public `IFidoHidConnection`/`IOtpHidConnection`. `SmartCardConnectionFactory.CreateAsync(IPcscDevice, ...)` and `IHidDevice` report-opening signatures mean related types must move together. |
| Expert raw connections / custom transports | **Approved public scope (D16):** retain raw connection operations and the public contracts consumers implement/use. Exact member shapes remain design work. | Current docs explicitly support tier 2. `ISmartCardConnection`, `IFidoHidConnection`, `IOtpHidConnection`, device connection acquisition, and connection-taking session factories form a connected public contract. Breaking signatures are permitted; removing this use case is not. Externally derived session hooks require a separate member-level review, not automatic retention merely because raw operations remain public. |

Specific constraints on a consistent change:

- `IConnection` itself currently exposes only connection type and disposal (`src/Core/src/Abstractions/IConnection.cs:26-28`). It is already distinct from packet/transmit methods on the typed interfaces. D16 retains these public consumer capabilities, including sequential cross-applet connection reuse; internalizing platform machinery must not remove them. No replacement factory signature is selected here.
- Public/protected references must change together. `ApplicationSession` is the public base of applet sessions; its `Connection` property and connection-taking constructors are protected (`src/Core/src/Sessions/ApplicationSession.cs:38,59,81-99`), so they participate in the external inheritance surface. Keep public contract dependencies accessible and review protected hooks explicitly when changing factory or connection signatures.
- Five single-transport applet factories currently take `ISmartCardConnection`; Management, Fido2, and YubiOtp take `IConnection` for multiple supported routes. This difference reflects capabilities and is not, by itself, style drift. Decide any change using the same capability/ownership rules across all applets; do not replace useful type constraints solely to make signatures textually identical.
- Core already grants internal access to its eight direct applet assemblies (`src/Core/src/Yubico.YubiKit.Core.csproj:29-48`); WebAuthn delegates through Fido2. Cross-assembly implementation use therefore does not automatically justify public visibility. Friend access does not permit a public signature to expose an internal type.
- If a public contract is removed or narrowed, the owning engineer must update all affected applet/raw factories, public/protected signatures, public baselines, examples, and convention tests in the same integrated slice. Keep applet testing/composition interfaces public where intentional; internal test seams do not need to become product extensibility points.

D15's compatibility/visibility policy and D16's retention of both public raw-access tiers are adopted. Exact implementation and internalization candidates remain proposals; no visibility, factory, transaction, native package, or test-seam implementation has changed. A fresh fetch on 2026-09-22 confirmed both the worktree and `origin/yubikit` still at `a7f2cae8c32ad6e0ada55e404f85442f6a266f6c`.

### Historical handoff evidence

- The original handoff recorded demo baseline `99e082c46018ecdde63c3c47980c14e313b70192`, a clean status before writing its external artifact, NativeShims package declaration 1.16.1, source observations, and structural validation of 12 sections, 72 unchecked criteria, criterion mappings, 10 original slices, anti-criteria, and balanced fences.
- Those claims describe handoff preparation, not the current implementation or acceptance. The current plan supersedes its workflow/graph while retaining the baseline as historical reference and all stable ISC IDs unchecked.
- The following references were inspected during the original handoff, not re-fetched in this refinement. They remain historical planning references rather than current passing evidence; pin moving sources and verify them against supported products during the relevant slice.

| Ref | Historical source | Supports / limitation |
|---|---|---|
| API-1 | https://learn.microsoft.com/en-us/windows/win32/api/winscard/nf-winscard-scardtransmit | WinSCard transmit has no overlapped/completion parameter. |
| API-2 | https://learn.microsoft.com/en-us/windows/win32/api/winscard/nf-winscard-scardcancel | Context-scoped cancellation, especially status waits; not portable APDU-abort evidence. |
| API-3 | https://pcsclite.apdu.fr/api/group__API.html | `SCardCancel` describes cancellation of blocking `SCardGetStatusChange`. |
| API-4 | https://learn.microsoft.com/en-us/windows-hardware/drivers/hid/obtaining-hid-reports | HID input through `ReadFile`; overlapped operation is supported. |
| API-5 | https://learn.microsoft.com/en-us/windows/win32/api/ioapiset/nf-ioapiset-cancelioex | Cancellation request is distinct from terminal completion and storage reuse. |
| API-6 | https://learn.microsoft.com/en-us/windows/win32/api/ioapiset/nf-ioapiset-deviceiocontrol | Overlapped `DeviceIoControl` mechanism; not proof for every HID feature/driver direction. |
| API-7 | https://github.com/libusb/hidapi/blob/master/windows/hid.c | Practical GET_FEATURE precedent; inspected implementation waits synchronously and uses a different SET path. |
| API-8 | https://github.com/apple-oss-distributions/IOKitUser/blob/main/hid.subproj/IOHIDDevice.h | Dispatch lifecycle, input/report callbacks, availability, and release requirements. |
| API-9 | https://github.com/apple-oss-distributions/IOKitUser/blob/main/hid.subproj/IOHIDDevice.c | Implementation evidence for report callbacks; verify supported deployed systems. |
| API-10 | https://docs.kernel.org/hid/hidraw.html | Nonblocking input, report formats, feature ioctls, and transport distinctions. |
| API-11 | https://github.com/torvalds/linux/blob/master/drivers/hid/hidraw.c | Readiness/write-path and descriptor behavior; moving source must be pinned. |
| API-12 | https://learn.microsoft.com/en-us/uwp/api/windows.devices.smartcards.smartcardconnection.transmitasync | WinRT asynchronous APDU alternative; compatibility remains untested. |
| API-13 | https://developer.apple.com/documentation/cryptotokenkit/tksmartcard | CryptoTokenKit session model and lifetime. |
| API-14 | https://developer.apple.com/documentation/cryptotokenkit/tksmartcard/transmit(_:reply:) | Callback/async APDU API; original handoff inspected Apple documentation JSON when rendering was unavailable. |

### Evidence record

Append accepted evidence to this master acceptance document under the stable ISC ID, with generated boundary manifest/coverage artifacts linked by stable boundary ID. Include probe identity, exact command/result or artifact, SDK SHA, native SHA/package, system/RID, driver/firmware when relevant, and evidence level. Record failures and unavailable fixtures without checking criteria. Every required pending item blocks closure. Preserve IDs when refining interpretation.

**Historical `.7` limited evidence — 2026-09-24 (superseded by the `.8` checkpoint above for package and count, not for its recorded outcomes).** Owner: orchestrator for evidence; Route/Native Engineers for runtime; scanner Engineer for Core inventory. The accepted source checkpoint for the comparison's AFTER transport is `89420aa6` plus its recorded working-tree diff, not a commit of all present changes. These rows added only the bounded ISC-5 fixture checkmark at that time; ISC-1–4/6–8 and route-wide criteria remained open (3/72 at that checkpoint).

| Applicable criteria | Observed grade and remaining condition |
|---|---|
| ISC-1–3, ISC-5–8 | Core-only `src/Core/tests/Yubico.YubiKit.Core.UnitTests/BoundaryInventory/{BoundaryScanner.cs,BoundaryManifest.cs,BoundaryInventoryTests.cs,core-boundaries.v1.json,README.md}` scans `src/Core/src/**/*.cs`, excluding generated files. Thirteen targeted tests pass, including the cross-file source-order regression; 198 exact sites remain **documented/outstanding**: 130 native imports, 18 waits, 17 scheduling, 21 pre-task-return dispatch gaps, six callback registrations, four delegate conversions and two unmanaged callback addresses. The reviewed unknown-import fixture and exact manifest gate reject an unclassified native entry (ISC-5); stale and invalid-review fixtures also fail. This is Core source at the current .NET 10 preprocessor configuration only, not native exports, all applets, other configurations or a whole-program call graph. Source-order dependence was fixed by two-pass collection; callback forwarder/nontransitive helper-to-native graph coverage remains a deferred limitation. The last full Core run (1,399 passed/3 skipped) preceded the thirteenth inventory test; do not infer a new full-suite count until rerun. ISC-1–4/7–8 remain open; the direct/interface negative fixture does **not** prove ISC-6's reachability through arbitrary interface implementations. |
| ISC-16–18, ISC-31–33, ISC-59 | On macOS 15.7.7 arm64, key 31683481 (firmware 5.7.4), `.3` active `DeviceWaiting` cancellation resolved presence once, recovered same session and reopened on the successful second touch attempt; first attempt timed out due to missed operator window (`/var/folders/gn/mh64zz5969j89_f5dffnvnb80000kt/T/opencode/yubikit-touch.log`). `.3`, `.4`, `.5` actual unplug/dispose failed close with `BadArg`; `.6` actual unplug → terminal → dispose → replug → getInfo passed (`/var/folders/gn/mh64zz5969j89_f5dffnvnb80000kt/T/opencode/yubikit-removal-async6.log`). `.6` distinguishes Apple removal `service_terminated` plus cancellation acknowledgment/drain before interpreting removal-only `BadArg`, not ordinary close errors; native Release and AddressSanitizer tests passed 23 each, review PASS WITH NOTES. `.7` retained that logic, is silent by default, and passed five normal native-AOT scenarios and active cancellation; it was **not** operator-unplugged. OTP failed-reset latch in `OtpHidProtocol.ResetStateAsync` rejects later exchanges, status reads and Configure on that protocol instance while preserving the primary cancellation/timeout; 28 focused tests and one real-protocol scripted recovery test pass. Physical mid-frame failure is unverified and another session borrowing the same raw connection can bypass the per-protocol latch; `RawOtpHidSession` recommends reopening. No all-device/platform claim. Current SDK host runs used .NET 10.0.12 after another agent updated global .NET SDK 10.0.401; the separate paired `.3` comparison used .NET 10.0.0. |
| ISC-37, ISC-51, ISC-58 | Selected smart-card async transaction/read/reopen passed once after previous sharing contention; not the PC/SC platform/reader matrix. `.7` also passed three read-only OTP info queries (feature SET/GET and dispose/reopen), not a write or native mid-frame abort. Native-AOT execution on one macOS fixture does not close the required cross-platform ISC-58 rows. |
| ISC-41–48 | `Directory.Packages.props` pins local unsigned/unpublished `1.18.1-async.7`, SHA-256 `68c3ca219a5581fdce7c11f98c0bc7b83d38a0f0f191278480ae48d49fc41b83`; native fix is now **locally committed** at `71a23cd0`, not pushed. This package was built before that commit: its producer metadata remains `f8c974` plus dirty changes, so `71a23cd0` is a same-behavior source checkpoint, **not** a rebuilt package producer identity. Fresh private restore and publication remain pending. Published `.3` and its workflow remain separate historical evidence. |
| ISC-62 | [Comparison datasets](docs/plans/yubikit-async-boundaries/00-status.md#measured-comparison-not-a-performance-acceptance-claim) and the reproducibility commands below give 10 completed fresh-child lifecycle samples per side on the same host/key and `.3` package, BEFORE source `65964966`, AFTER `89420aa6`. Invocation medians 47.881 → 28.538 ms, task terminal 48.467 → 47.953 ms, lifecycle 78.456 → 80.144 ms; AFTER max lifecycle ~151 vs ~100 ms. These medians are not a performance pass: no `.7` comparison, approved budget, native durations, allocation or idle activity. No-input samples did not complete both lifecycle phases; the earlier BEFORE block had 8/10 completed, one failed and one unattempted, and is not counted in the paired dataset. Descriptive only; ISC-62 remains open. |

**Reproduce the `.3` comparison, not `.7`:** runner `verification/MacOSHidBoundaryComparison/Program.cs` freezes two fresh-child warmups, ten fresh-child lifecycle samples and one no-input child per checkout with a 10-second per-child watchdog. The older pair is `comparison-before-20260923T212640783Z.json` (SHA-256 `36852b85c6d0a2f7170da3b62c7625ecaffc02e0fcd7fbbbba1c586963b2fd01`) under `/Users/Dennis.Dyall/Code/y/worktrees/yubikit-async-baseline-65964966-async3/artifacts/measurements/` and `artifacts/measurements/comparison-after-20260923T212725515Z.json` (SHA-256 `218016c00bac31fb1e364f5ec4b88a0e9754c7cb8b510043d7711a7f1d3fdb21`) in this worktree. Those are **version-1 datasets**; the newer runner emits `comparison-v2-*` and enforces a fixed `.3` package SHA, so do not claim it generated the old pair. Its exact hardware-free validation entry points, from the worktree containing the runner, are `dotnet run --project verification/MacOSHidBoundaryComparison/MacOSHidBoundaryComparison.csproj -- --self-test` and `dotnet run --project verification/MacOSHidBoundaryComparison/MacOSHidBoundaryComparison.csproj -- --compare <before.json> <after.json>`. To collect **new** v2 data, build each pinned checkout with `.3` via an explicit local feed and its own `RestoreConfigFile`, set `YUBIKIT_BOUNDARY_SOURCE_ROOT` to that checkout and `YUBIKIT_BOUNDARY_RUNNER_ROOT` to this worktree, run `dotnet run --project verification/MacOSHidBoundaryComparison/MacOSHidBoundaryComparison.csproj -- --check` before hardware, then `--measure before`/`--measure after` on the respective checkouts. With the current `.7` pin or untracked source this runner deliberately rejects measurement; preserve/reconcile the checkout first. The datasets record package/native asset and source/runner hashes at measurement time; the before binary was later rebuilt with a different hash, not interchangeable proof. Performance acceptance remains pending due to tails and missing metrics; no arbitrary 15–30% target is adopted.

All verification entries below this paragraph are dated historical checkpoints. In particular, their “current” package, absent-touch/removal/comparison and pre-replug sharing statements describe only those earlier runs; they do not supersede the current table above.

**Accepted atomic evidence — 2026-09-23, uncommitted source over `db7a1bf6`, local
NativeShims `1.18.1-async.2` (SHA-256 `514f3804201c26447273a48fef89fa31f1153eadf12ab9cbd6ea2c5266c60d1f`):**

| Criterion | Exact executed command / result and source | Grade / limit |
|---|---|---|
| ISC-49 | `dotnet toolchain.cs -- test --project PublicApi` — 22 passed. `src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/AsyncSurfaceConventionTests.cs:20-55` tests every registered shipping applet session/extension operation for Task return, Async suffix and final defaulted cancellation token (documented synchronous allowlist). | Public managed API shape only, not native responsiveness. |
| ISC-52 | `dotnet toolchain.cs -- test --project Core` — 1,355 passed/3 skipped before the subsequent report-queue allocation fix; `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~MacOSHidFidoRouteLifetimeTests"` — 17 passed after it. `FidoHidProtocol.cs:49-71` has synchronous local-only `Configure` and separately awaited `InitializeAsync`; `ApplicationSession.cs:292-294` awaits FIDO channel initialization. `FidoHidProtocolConcurrencyTests.Configure_DoesNotStartWireInitialization` and `MacOSHidFidoRouteLifetimeTests` held-init session test cover zero-wire configure and pending native initialization. | Managed production configuration/initialization contract, not actual device INIT completion. |

**Earlier empty-host route attempt — not accepted.** `verification/MacOSHidRouteVerification`
was Native AOT published with real Core/Fido2 and the `.2` package: the host statically
links 41 exports with no native shared-shim dependency. Executing its binary with `--list`
returned `discovered=0`, exit 2; `system_profiler SPUSBDataType` showed zero YubiKeys.
`--probe --serial 0` rejected an invalid serial; no valid explicit-serial probe ran. Thus
that invocation performed no physical open/init/send/receive/cancel/removal/reopen, native driver callback or
cancel acknowledgment, firmware, factory-fallback recovery, manual touch/unplug, hardware
baseline or before/after performance. Neither publishing nor empty discovery
checks ISC-31/33/58 or route completion. The subsequent selected-key checkpoint below
supersedes the empty-host availability assessment, not its historical result.

```yaml
isc: ISC-30
boundary_id: Windows.Fido.ReportRead
probe: WindowsCancellationHarness.CancelThenNativeCompletion
status: pending
sdk_sha: null
native_shims_sha: null
native_shims_package: null
os_build: null
rid: win-x64
device_and_driver: null
evidence_level: native-runtime
command: null
result: null
artifact: null
```

**Current selected-key checkpoint — 2026-09-23.** The following host and integration
PASS results are supplied from the prior user run; their exact shell invocations and
console logs are not retained here. Do not treat them as newly rerun verification.
On macOS 15.7.7 arm64 / .NET 10.0.0,
the attached selected key (serial 31683481, firmware 5.7.4) exercised the production
`verification/MacOSHidRouteVerification` native-AOT host with real Core/Fido2 and the
local `.2` shim. The explicit-serial read-only probe returned PASS for three
open → initialize → getInfo → dispose → reopen cycles. This proves a normal-path
public-route device execution, not removal, touch, pending-read shutdown or all driver
callback races. The read-only
`PcscLifetimeIntegrationTests.PcscLifetime_SequentialTransactionsAndConnectionReopens_ReadStableDeviceInfo`
integration filter passed **one** test on the same fixture: three connections, each
with two transactions and device-info reads. This result is separate from the earlier
smart-card test at 1.16.1; it does not prove Windows/Linux smart-card behavior.

Benchmark-tooling artifacts
[`lifecycle`](artifacts/measurements/async-boundary-20260923T153527498Z-911932a49f5d4a47a85b420e233e0410.json)
and [`no-input-return`](artifacts/measurements/async-boundary-20260923T153539866Z-85978c78e9af4e80bf6b11e101a4d184.json)
record three completed lifecycle samples and one **censored** no-input sample (watchdog
invocation exit 1; child exit code unrecorded; `phase=invocation_returned`, invocation
return 79.9447 ms). The no-input
sample never recorded that shutdown was requested: it says nothing about whether
dispose would complete, and is not a shutdown bug. The tooling records firmware as
unread, independently of the fixture firmware identified above; `shippingDiffSha256`
is unavailable because untracked source files exist. There is **no BEFORE dataset** or
agreed numeric budget: neither latency improvement nor ISC-62 closure follows. No
touch/unplug/removal scenario ran at this earlier checkpoint. Native-AOT device execution
on this one macOS route does not close ISC-31/33/58 across required rows; ISC-49/52 remain
the only checkmarks.

**Later selected-key pending-read checkpoint — 2026-09-23, supplied result (not rerun here).**
With local `.2` and the explicit temporary feed, the following publish succeeded:
`RestoreConfigFile=/var/folders/gn/mh64zz5969j89_f5dffnvnb80000kt/T/opencode/yubikit-async-native.nuget.config dotnet publish verification/MacOSHidRouteVerification/MacOSHidRouteVerification.csproj -c Release -r osx-arm64 --self-contained -p:PublishAot=true`.
`./verification/MacOSHidRouteVerification/bin/Release/net10.0/osx-arm64/publish/MacOSHidRouteVerification --probe --serial 31683481`
passed three cycles **and** pending raw receive → `DisposeAsync` → read terminal → dispose
complete → reopen/getInfo on the selected key (firmware 5.7.4). This exercises the real
IOKit callback acknowledgment in normal shutdown while a read is pending, not physical
removal, touch or every interruption race. The separate 17 focused post-craftsmanship
managed tests passed with the preview; the previous 1.18.0-pin tests remain historical.
Do not check universal ISC-33 or all-platform ISC-58 from this one path. No comparable
BEFORE dataset or performance improvement is established. The later `.3` publication
and artifact-backed managed tests above do not retroactively turn this `.2` hardware
checkpoint into `.3` hardware evidence or establish local private-feed consumption.

**Historical `.3` bounded milestones — supplied results, not rerun by this document edit.**

| Lane | Outcome and evidence grade | Still required |
|---|---|---|
| S4 smart-card transaction | Additive public `BeginTransactionAsync` on `ISmartCardConnection`; built-in path awaits its owned worker, external implementations retain a synchronous fallback. Eight focused managed tests; pre-OTP Core 1,363 passed and PublicApi 22 passed. The selected PC/SC hardware integration test failed with a sharing violation. Implemented/managed-tested, **not hardware-verified**. | Resolve sharing contention, rerun selected-key transaction probe; prove remaining PC/SC lifecycle/context isolation and required platform rows before S4 acceptance. |
| S2 macOS OTP feature GET/SET | Worker-owned GET and SET candidate implemented. Eight focused unit tests passed; after this change Core 1,371 passed/3 skipped, PublicApi 22 passed, YubiOtp 180 passed, resilience-fast 77 passed. Implemented/managed-tested, **not selected-key OTP hardware-verified**. | Actual GET/SET, touch and recovery on the selected key, plus native/packaged route evidence; do not promote this partial S2 contribution to S5 or universal ISC-18/32/59/61. |
| S5 OTP and remaining boundaries | Not complete; macOS GET/SET contributes only one platform's candidate implementation. | Windows zero-access feature reports, cross-platform OTP recovery and remaining raw/factory/monitor/disposal boundaries still need their own evidence. |

At this earlier checkpoint, the `.3` workflow and artifact-backed restore established packaging/build evidence, not a
fresh private-feed restore or selected-key `.3` route run. No physical removal or touch
occurred in those cited live probes; no comparison existed yet at that checkpoint.
The parent handles verification and any requested commit later; `db7a1bf6` is the
recorded first smart-card slice commit. Later `89420aa6` commits the transport checkpoint,
not the current follow-up working tree or the local `.7` native producer.

**Bounded fit pass (D30; not a new architecture gate).** Parent owns evidence and
doc reconciliation; the separate Code Engineer completed named return codes, per-owner report
capacity, format quarantine helper and trimmed comments on the macOS FIDO route. The
bounded review returned PASS WITH NOTES; 17 focused managed tests passed after cleanup
at the historical 1.18.0 pin. This is not a preview native-runtime or hardware result. Keep
the connection/native event owner and existing public raw contracts, with no shared
slot interface or scheduler. Value 2 / cost 1: local clarity of return and capacity;
value 1 / cost 1: comments/docs that distinguish proven normal flow from pending
shutdown. Keeping the current shape unchanged would leave ambiguous local names and
stale claims; introducing shared machinery would enlarge a one-route iteration and
is deferred. The cross-vendor Fable two-turn consultation retained the overall nested
owner shape and withdrew the shared slot interface. The later preview/pending-read
hardware result is recorded above; this fit pass supplies no new criterion checkmark.

For a clean-machine replay, obtain the tagged native source plus the exact dirty-file
hash manifest (the cited `docs/local-provenance.json` was not present in the detached
worktree on this inspection; recover and verify it before claiming reproducibility),
build the local preview with that native checkout's macOS build/package
scripts and inputs, and expose the resulting `.2` package through a **temporary local
NuGet feed** listed in a temporary NuGet configuration alongside the normal feeds.
At that historical `.2` preview checkpoint, use the same configuration explicitly as `RestoreConfigFile` for restore/build/publish;
record resulting package and deployed asset digests. A temporary configuration path
by itself is not a portable recipe. The then-current `.2` declaration had restored with
that configuration; this does not prove clean-machine replay. Before promoting shipping artifacts, establish actual
source/package binding, compatible clean restores on required systems, native runtime
and hardware matrix evidence; this unsigned local build is not promotion authority.
Continue independent platform work while those gates remain pending.
