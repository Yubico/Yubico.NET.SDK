# Windows upstream receive and readiness review

**Bounded intake complete; ready for an evidence-based integration plan, not an integration or platform-acceptance sign-off.**

This review receives the delivery substantively against the audited parent. It does not merge,
resolve conflicts, alter product code, reopen a wide audit, implement deferred findings, or authorize
platform execution. Astra owns coordination/execution; Sol owns investigation, editing and review.
The separate conflict-mapping owner supplies the nine-conflict decisions; this report does not
duplicate that line-by-line work. Only new `intake-review/*` records are owned here.

## 1. Pins, PRs and actual ancestry

| Identity | Exact value / interpretation |
| --- | --- |
| Audited parent / observed HEAD | `d5d01ef99f1478e4d49911194c17667b2450d2fd` |
| Incoming pin | `e322712d609aaf03b9bd1cf2964a99326728866a` |
| Merge base | `5746e72ed9a848d08b5782ddde025aa4a4b522ef` |
| Worktree / branch | `/home/rupicola/Code/y/yubikit-windows-audit-integration-20261113`, `yubikit-windows-audit-integration-20261113` |
| Incoming merge parents | `f99cf78fe90b257653aa0a3de60bbffe896b34fb`, `13fdf43019ce78f340a47c8484f97c12d3d18409` |
| Incoming merge tree | `6278bebdc13fba19726fb255c67e58155392f7a2` — identical to the PR774 head tree |

The completed [intake summary](../intake/summary.json) records **126 incoming change records**
(rename-aware, not 126 independent behavioral changes), 19 overlaps and an object-database preview
with nine conflict paths. Read-only ancestry checks confirm **36 commits** from merge base to incoming:
one `f99cf78fe` hotplug-assertion commit, 25 Windows commits through `bdac2f4f4`, nine consistency commits
through `13fdf4301`, and the merge commit. The Windows head is an ancestor of both consistency heads.

| Captured PR | Last head | Captured delivery state |
| --- | --- | --- |
| [771](https://github.com/Yubico/Yubico.NET.SDK/pull/771), base `yubikit`, head `yubikit-windows-async` | `bdac2f4f4224fa0ccf92006971af8659736aa2b1` | `MERGED`, `isDraft=false`, merged `2026-11-13T10:01:16Z`; 12 captured check runs, all `COMPLETED/SUCCESS` |
| [774](https://github.com/Yubico/Yubico.NET.SDK/pull/774), base `yubikit-windows-async`, head `yubikit-windows-consistency` | `13fdf43019ce78f340a47c8484f97c12d3d18409` | `MERGED`, `isDraft=false`, merged `2026-11-13T10:01:18Z`; 13 captured check runs, all `COMPLETED/SUCCESS` |

Both raw PR captures report **the same mergeCommit `e322712d6`**. The actual object has the two
parents above and subject `Merge pull request #774 from Yubico/yubikit-windows-consistency`.
Do not invent a separate PR771 merge object, infer a squash, or treat the two PRs as independent
deliveries to apply twice. Receive the pinned ancestry once. Raw captures were made at
`2026-11-13T10:17:57.786541+00:00`; no fresh GitHub query was made by this reviewer.

PR bodies retain historical `draft`, `PENDING` human review and `No automatic merge` prose.
That text is not the current PR state. Conversely, merged metadata is not evidence that omitted
platform gates passed, that the human draft review was submitted, or that a `COMMENTED` Copilot
review was approval. The check rollups include duplicated push/PR workflow runs: 12 and 13 are
reported check runs, not unique acceptance criteria. Hosted Native AOT publication is not execution
of the separate sealed Windows route verifier or physical-device scenarios.

### Evidence revisions must remain distinct

- **`bdac2f4f4`:** original Windows owner/routing implementation and diagnostic-tooling fixes;
  Windows checkpoint results and product snapshot
  `52ecb539bbb03151a1e8aa66eaabf1cfe3633f1ae9e1cb3b3b2e2189d66b7c8a` belong here.
- **`0f10775cecfb61f74d20851417746a5816863bb7`:** consistency source with the earlier injected-pool
  test correction. Windows console evidence uses snapshot
  `d677fb5c7a0e602a0a18511de01c35a511e46c7f094a9a8e2ec2f48de4db0c2c`.
  Later redirected records identify the same commit but snapshot
  `fb70a23b9afc526da733c59631bdc0faca0cedfbefbf416f707527a1957a75ed`; retain that separately.
  Reported Mac execution also names this commit, not the final head.
- **`13fdf4301`:** after `030b2a448` restored the original PIV authentication implementation and
  removed the injected pool/20 dependent cases, this final commit added XML documentation to five
  PC/SC/input-bridge files. PR774 reports a fresh full build, 4,306 unit executions / 0 failed /
  5 skipped / 0 warnings, 289 PIV cases, docs/complexity and final hosted results on this head.
  Those local software results are PR-summary evidence here, not locally retained raw reports read
  by this reviewer. Earlier 4,326/309 results belong to `0f10775ce`, not this head.
- **`e322712d6`:** merge metadata changes the commit identity but has the same tree as `13fdf4301`.
  It does not rerun tests. The future audited-plus-upstream integration will have different inputs
  again. Neither tree equivalence nor documentation-only shipping changes constitute execution
  at that future source identity.

## 2. Functional delivery and callers

The source was read with `git show` at the incoming pin, not inferred solely from PR summaries.
Root/Core instructions and affected Ctap2, YubiOtp and Piv module instructions were read.

### Windows FIDO and OTP lifetime changes

`src/Core/src/Transports/Hid/Windows/` gains separate typed connections and rooted
`WindowsFidoHidNativeOwner` / `WindowsOtpHidNativeOwner` authorities. Each owns one original,
execution-context-suppressed worker and one ordinary operation slot, without a backlog or
replacement worker. Open, operation completion, cancellation intent and proven release are distinct.

- **FIDO:** `IWindowsHidReportApi` / `WindowsHidReportApi`,
  `WindowsHidOverlappedOperation`, `Native/Windows/Kernel32/WindowsHidSafeHandle.cs` and Kernel32
  imports implement overlapped read/write using read/write access, report-ID-zero framing,
  `ThreadPoolBoundHandle` binding/allocation/free and exact-request `CancelIoEx`.
  The owner joins Submit return with callback resource-last-use publication before retirement,
  staging clearing and actual operation-task completion. Immediate success and pending acceptance
  both require a real callback; neither cancellation nor error 1168 proves completion. Native
  success wins cancellation intent. Typed packets remain 64 bytes; full native count and zero report
  ID are checked. Successful receive storage escapes independently to the caller.
- **OTP:** `IWindowsHidFeatureReportApi` / `WindowsHidFeatureReportApi` preserves zero desired
  access and synchronous HidD feature GET/SET on its own worker, not a fictional overlapped feature
  API. Typed payloads are eight bytes / native descriptor length nine. Cancellation prevents
  dispatch; after dispatch it cannot detach a blocked call, and success may win late cancellation.
  Staging clears after native return, before task completion. HidD success exposes **no actual
  transfer count**. A permanently held native feature call can keep shutdown pending indefinitely.
- Both owners receive the original registry/discovery claim **before native acquisition**; checked
  cleanup is required even for failed/cancelled opens. Unknown release retains/quarantines the
  rooted owner and original claim. An allocator-only FIDO preparation failure proves non-dispatch,
  clears report bytes and faults operation/shared shutdown, but retains uncertain BCL allocation,
  pin, binding and handle: null pointer is not release proof. No timeout or retry is substituted.
- Opening-token authority ends at success publication, not when the factory continuation resumes.
  Synchronous disposal observes the shared drain and rejects worker self-wait. Finalization requests
  shutdown only. Callback resource-last-use is not a claim that the native callback stack returned.

`HidConnectionSlot` now selects the Windows typed owners as well as macOS/Linux owners. Per-open
Windows factories must create fresh, unopened, stateful APIs. Factory failure before any owner/native
acquisition is an explicit no-native claim-release path; a transferred pre-cancelled open must still
reach its owner for claim cleanup. `YubiKeyDevice`, `SharedDeviceInfoRead` and existing registry code
are caller dependencies even where not changed by this incoming delta: preserve grouped known-member
claims, late discovery cleanup, replacement evidence, and no transport fallback/replay.

`WindowsHidIOReportConnection` / `WindowsHidFeatureReportConnection` become synchronous expert
facades over the same owners. Constructor/Get/Set/Dispose intentionally block; DisposeAsync observes
actual drain. Direct reports use descriptor-sized payloads, with exact descriptor-size SET, not the
typed FIDO64/OTP8 contract. Returned arrays belong to the caller. Custom `IHidInterface` providers
still use synchronous compatibility adapters; built-in Windows guarantees must not be generalized
to them. Applet callers still sequence whole workflows: one raw-call/one-exchange admission is not
a join for every public multi-exchange operation, callback or borrowed input lifetime.

New byte-return HidD imports correctly distinguish native one-byte BOOLEAN from Kernel32 BOOL on
the new report path. **The old Bool-return HidD declarations remain in the file**, and Cfgmgr32
Guid/PULONG declarations are outside this delivery. This is not a complete Windows ABI repair.

### Cross-platform consistency and PIV test rollback

- macOS: `IMacOSHidReportApi` / `MacOSHidReportApi`, `IMacOSHidInputBridge` /
  `MacOSHidInputBridge`, and extracted `MacOSFidoHidNativeOwner` / `MacOSOtpHidNativeOwner` separate
  mechanical report/callback glue from lifetime authority. Preserve borrowed callback memory,
  bounded copying, terminal notification versus acknowledgment, Cancel → WaitShutdown → Destroy,
  retained callback roots on failed destroy, checked device release and original claim ownership.
  This extraction is not permissionless-host or native producer qualification.
- Linux: independent native/readiness/registration/event seam types move into matching files;
  `LinuxHidEventSource` becomes `LinuxUdevHidEventSource`. Preserve reactor true quiescence,
  original-worker reference release and checked fd close; splitting files is not new kernel evidence.
- PC/SC: internal `UsbSmartCardConnection` → `PcscConnection` and
  `PcscConnectionNativeState` → `PcscConnectionNativeOwner`; native API implementations move to
  `NativeSCardApi.cs` / `NativeSCardConnectionApi.cs`. USB/NFC support, exclusive default, explicit
  sharing opt-ins, no ordinary queue, begin/end/transmit drain and disconnect-plus-context-release
  proof remain. Logger category changes to `PcscConnection`; disposed diagnostics intentionally
  retain `"UsbSmartCardConnection"`. This matters to diagnostics consumers without being a public
  signature change.
- PIV: PR774, not redundant closed PR773, delivers the test correction. Final incoming
  `AuthenticationProtocol.cs` is byte-identical to merge-base blob
  `526c5da214546c1655ab8f36e2f7e95831cfdbf0`. The net incoming delta removes invalid late pooled-command
  observations from `PivPinOnlyModeSessionTests`, retains independent owned-response clearing and
  recovery assertions, and documents the withdrawn coverage. **No direct runtime assertion of
  pre-return pooled-command clearing remains in that fixture.** Do not resurrect the temporary pool
  seam, infer a production zeroing fix, or copy the superseded 20-case increment into acceptance.
- Public connection/raw/applet interfaces, public named parameters, `PscsConnectionKind`,
  `DesktopSmartCardDeviceListener` and `IO` spelling are retained. New owner/seam identities are
  internal/friend-visible. Core adds verifier friendship; the standalone Windows project is
  nonpackable net10.0/C#14, unsafe/AOT-compatible, with a Core project reference and linked evidence
  helpers. It is not compiled by assuming the ordinary solution build covers every verifier.

## 3. Active tests, registries and documentation surfaces

| Surface | Integration validation/review purpose |
| --- | --- |
| Core `Transports/Hid/Windows/*Tests.cs` | Owner, preparation, result/count/framing, SafeHandle, direct facade and independent report/checked-close holds; actual production owners with controlled native seams |
| Core `Devices/WindowsFidoHidRoutingTests`, `WindowsOtpHidRoutingTests`, shared `WindowsRoutingContract` | Public open, fresh API per reopen, failed/pre-cancelled open, discovery timeout/direct/composite ownership, idle/active raw-session reuse, quarantined unknown release |
| Ctap2 `Ctap2SessionWindowsCrossLayerTests` | Three public production-route managed journeys: held initialization/borrowed reuse; keepalive cancellation/correlated reuse; owned pending-exchange disposal and delayed claim release |
| YubiOtp `YubiOtpSessionWindowsCrossLayerTests` | Four cases: held initial status/borrowed reuse; touch-wait cancellation/reset/recovery; terminal presence owned by applet CRC validation (two theory cases) |
| Core Mac lifetime/facade/cancellation/recovery/route tests; PC/SC connection/lifetime/context/sharing/factory tests | Changed extraction names and real owner bindings, not renamed tests alone |
| Core `BoundaryInventory` scanner, manifests, adapter/public-sync registries, diagnostics and negative controls | Incoming 245 classified sites, 59 operations / 104 unique method links, 31 public-sync entries; links do not recursively execute profiles or confer native verification |
| `DeviceEventHotPlugTests`, new `DeviceEventLifecycleTests` | Event-ordered object-identity assertion permits transient fragments/equal-valued new objects but rejects re-adding a removed object; three new cases are hardware-free despite living in the integration assembly |
| `scripts/toolchain/BuildTargets.cs`, shared `WindowsHidVerification/*`, `verification/WindowsHidRouteVerification/*` | Portable guard self-tests and separately opt-in Windows JIT/AOT source/artifact-bound kernel-pipe lane |
| `.github/workflows/build.yml`, `PcscIsolatedProbe` | Ubuntu runs portable verifier guards; failure-only toolchain/probe/runtime diagnostic retention and numeric child exit; no demonstrated runtime repair of the intermittent Linux finalizer failure |

Incoming consistency mapping retains 229 source identities and maps 16 one-to-one (245 → 245),
with source families 58 → 61. This is the incoming source's inventory, **not a prescribed count for
the combined audited tree**. Run the exact-site gate after integration and review real deltas; do
not blindly overwrite audited manifests, promote `outstanding` to `verified`, or add overlapping
profile/case counts together. The 104 linked methods' historical 145 executed cases overlap units.

Active bounded documentation targets include root/Core instructions and Core README,
`docs/SDK-HOUSE-STYLE.md`, `docs/architecture/raw-access-tiers.md`, existing async status/architecture/ISA,
the two incoming research handoffs, the BoundaryInventory README and PIV verification instructions.
Also inspect **unchanged caller-facing** `docs/architecture/connection-ownership-and-contention.md`
and relevant HID/public lifetime XML where the frozen audit's Windows/custom limitation split lived.
Any necessary updates belong in new integration/product records under the approved later plan,
never by rewriting frozen Core001 findings to pretend they anticipated this owner migration.

The incoming Windows research record is correctly scoped to `bdac2f4f4`. The Mac handoff still
says “Planned, not executed”; status/architecture/house-style and ISA contain older paused/unexecuted
statements. PR774 has later source-specific Windows/Mac evidence. Reconcile this dated layering in
the integration documentation delta rather than accepting either “all still unrun” or “all closed”.
**ISA remains 19/72.** Merged PR state does not advance its checkboxes.

## 4. Received execution evidence and its limits

Only specifically named local retained jobs/bundles from the PRs were inspected. No controller,
Windows SDK, VM or device command was invoked; no broad home-directory search was performed.
Hashes and retrieval scope are in [evidence-identities.json](evidence-identities.json).

### Locally inspected Windows reports

| Source / job | Observed retained result |
| --- | --- |
| `bdac2f4f4`, `d49f259548214758af46d5385cc3422a` | Authoritative report, native exit 0; Core units 2210 / 0 failed / 3 skipped / 0 warnings; `sourceAfter` and target bind the requested product snapshot, not the report's pre-sync runner-source field |
| `0f10775ce`, `5e570107f0b84bb399086613a80886bc` | Authoritative report, native exit 0; Core units 2210 / 0 / 3 / 0; target/sourceAfter bind `d677…` |
| `0f10775ce`, `2a7c41d0d0c0402ebf812a46f42b76e2` | Authoritative report, native exit 1; Core device selection 28 / **1 failed** / 0 / 0. Raw log points to first `ConnectAsync` at `FidoHidOwnershipIntegrationTests.cs:58`, through `WindowsFidoHidNativeOwner.OpenAsync`, before the second-open assertion; reported 17 s with a 15 s token |
| `0f10775ce`, `91b16adfa0414214801feaf48f1b2a2a` | Authoritative attended console report, native exit 1; **18 executions, 17 passed / 1 failed**, zero skips/warnings. Raw method log confirms FullCeremony and discoverable GetAssertion passes; `YESDK1617_TimeoutWithoutTouchReturnsNotAllowedAndSessionStaysUsable` failed because no exception was thrown |
| `0f10775ce`, `daf18a7e92a74110b251016d50d75485` | Native exit 0; explicit SmartCard WebAuthn MakeCredential 1 / 0 / 0 / 0; nested actor standard token, RDP protocol 2, serial 103; sourceAfter `fb70…` |
| `0f10775ce`, `464d5c1e1ecd45f78b7b35332c79de77` | Native exit 0; CTAP2 redirected selection 5 / 0 / **80 skipped** / 0, same later snapshot and standard-token RDP/SmartCard actor; not 85 accepted cases |

For hardware jobs the outer session-0 runner identity is **not** the SDK actor; nested hardware and
sdkSession fields identify elevated active console/direct USB for the Core failure and attended
ceremony, versus standard-token RDP/SmartCard for redirected jobs. These are SDK CTAP-over-SmartCard
results, **not `webauthn.dll` API-channel attestation**. No private payloads or attestation material
were extracted into this record.

The PR771 body/checkpoint reports 59 selected device cases at `bdac2f4f4`; this reviewer did not
individually reread every constituent report, so that total remains a received summary, not a newly
derived raw-report total. PR774's original five-job hardware selection reports 40 executions,
39 passed / 1 failed; the failing Core constituent was checked directly above. Later isolated
source comparisons and a test-only timing diagnostic reportedly passed without reproducing the
first-open failure. They neither explain it nor establish a fix. Likewise a later focused
WithoutTouch pass is separate PR-summary evidence; it does not turn the original aggregate into
18/18. Keep both original failures visible.

### Locally inspected unsealed JIT bundles

Both `pr771-console-003/unsealed-jit-79c73f7ed683458a97a70a25eed850c4` and
`pr774-console-008/unsealed-jit-5c3c37cf35fc4248b5d484841db41f2b` are retained locally under
`~/.local/share/yubikit-work-evidence/`. The latter contains its timestamped run subdirectory.
Diagnostic receipts identify `bdac2f4f4` and `0f10775ce`, respectively, PublishAot=false,
publication/verifier/diagnostic exits 0 and AotAttempted=false. Their recorded source status includes
the runner's untracked `.windows-sdk-lease.json`; do not call it a literally empty porcelain capture.

For **each bundle separately**, this intake rechecked 11 evidence JSON documents: ten scenario
documents totaling 44 true checks plus 21 true summary checks, all PASSED, matching the same
207-entry published-file map. All 207 collected file hashes and exact membership still match;
there are no extra/missing published files. This is a read-only retained-artifact check, not a new
verifier run, independently reconstructed build attestation, or a combined seal. The production
contract requires exact scenario/check names, not count-only acceptance.

The scenarios cover pending read/GC, output, early callback before Submit return, held cancel
completion, success-versus-cancel intent, actual ERROR_NOT_FOUND, rejected read/write, 100 pipe
close/reopen lifetimes and ordinary-metadata rejection. These are **Windows x64 .NET 10.0.15 kernel/BCL
byte-mode pipe results, not HID-driver/device races**. Raw immediate-success versus ERROR_IO_PENDING
is normalized; immediate-success attribution remains unattested. Native BCL allocator-failure
unwind, hung-native recovery and physical report-driver cases are not established. Windows Native
AOT/combined sealed publication remains blocked by **Platform linker not found** in the received
execution window; hosted generic AOT publication does not close it.

### Mac evidence unavailable locally: explicitly PR-summary-only

`~/.local/share/yubikit-work-evidence/pr774-macos-001/` is absent on this Linux host. The PR says raw
logs, publish manifest, baseline comparison and nine toolchain runs are retained **on the Mac**.
No remote retrieval or execution was attempted. Receive these as reported results, not independently
verified raw Mac proof:

- Clean `0f10775ce`, Apple M1 native arm64/macOS 15.7.7, .NET SDK 10.0.401, authorized serial 125;
  controlled-seam counts 94 Mac / 11 resilience / 4 CTAP / 129 inventory, zero skips/warnings.
- Production-route self-contained **JIT**, NativeShims 1.18.1: FIDO `--probe` and PC/SC `--smartcard`
  reportedly pass (three cycles each, pending-read disposal/reopen and borrowed reuse for FIDO).
  Bounded integration selections 3 FIDO / 2 PCSC / 3 CTAP HID / 1 explicit CTAP SmartCard pass.
  Timing self-test is managed bookkeeping, not native evidence. Deployed arm64 dylib reportedly
  matches the package; the **loaded image path was not observed**.
- OTP `--otp-get` / `--otp-info` blocked by `IOHIDDeviceOpen=0xE00002E2`
  (`kIOReturnNotPermitted`); same launcher/parent-source comparison reportedly blocks too. Preserve
  permission attribution as the reported investigation, not a fresh local diagnosis or approval to
  change Input Monitoring. `--expert-feature` also has permission/two-key conditions;
  `--expert-io` requires exactly one attached key and was topology-blocked with two.
- FIDO/PCSC passes do not close OTP/expert modes, listener/touch/removal, AOT, other launchers or
  later source revisions. A macOS first-open pass does not close the Windows first-open finding.

## 5. Reuse, requalification and frozen finding continuity

Portable managed tests and their source/profile mappings are reusable **test assets and historical
evidence**, not passes for the new integration. Existing exact artifact/report identities remain
useful baselines. Rerun the affected assets after merge resolution; retain before/after source,
command, nonzero executed counts, skips, warning totals and complete RESULT/run directories.
Native/hardware claims need separately source-bound qualification for changed owner/factory/ABI
paths; source equality may narrow review scope but cannot relabel a historical run as a new one.

The [published external Core handoff](../../../core.md) records frozen Core001 commit
`bff374eb533f9ec03deecc04561aa286c050efa6` and source pin `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
Its frozen documents intentionally retain pre-publication/pre-receive wording. Use the existing
[deferred handoff](../../../modules/core/pass-1/core-001/deferred-findings.md) and original IDs/facets;
do not amend them, recount candidates as accepted defects, or create a new implementation backlog.

| Existing reference | Bounded receive/reopen implication, not a closure claim |
| --- | --- |
| `core-001:CORE-B4-C001`, `CORE-B1-C005`, `CORE-B1-C007` | The new built-in Windows owner/claim/drain route triggers the documented reopening condition. Update current Windows-versus-custom ownership/raw-tier guidance after reconciliation; preserve custom-provider unknown-release limits and whole-workflow sequencing |
| `core-001:CORE-B4-C007`, `CORE-B5-C005` | New native staging clearing and exact FIDO count checks change the old Windows-copy facet. Review production array last-use/clear assertions and caller ownership; they do not prove erasure of every protocol/APDU/report copy or exceptional path |
| `core-001:CORE-B4-C003` | New byte-return report imports address that narrow path; retained old HidD Bool declarations, Cfgmgr32 Guid/PULONG and architecture/JIT/AOT cells remain separate. No all-ABI-issues-resolved claim |
| `core-001:CORE-B4-C002`, `CORE-B3-C005` | Listener unregister/partial-start/callback-root findings are not fixed by report owners, new hotplug assertion tests or a quiet listener test passing. Preserve successful native Started/unregister/drain proof gaps |
| `core-001:CORE-B4-C004`, `CORE-B4-C008`, `CORE-B3-C008` | Access conditions, source/package/producer/runtime identity and evidence-specific claims require bounded new qualifications; elevated Windows console results do not prove standard-token HID or permissionless Mac access |
| `core-001:CORE-B1-C006`, `CORE-B1-C009`, `CORE-B1-C010`, `CORE-B3-C006` | Rename/extraction does not resolve transaction error propagation, post-open logging cleanup or ambient-context policy. Preserve native release/diagnostic distinctions and original finding scopes |
| `core-001:CORE-B2-C003`, `CORE-B2-C004`, `CORE-B2-C005` | Linux file split/event-source rename must retain audited libudev/string/runtime-prerequisite and failure-path oracle qualifications; it is not their runtime resolution |
| `core-001:CORE-B5-C006`, `CORE-B6-C015` | Public applet cross-layer passes remain bounded to exercised journeys. Exchange drain is not a whole-workflow lifetime/disposal join |
| `CORE-PIV-X006` / `PIVIMP-006`, `PIVIMP-005`, handoff protected qualifications | PIV pre-try caller cleanup and historical tool framing remain unchanged; test-pool rollback is not their repair. PIV physical touch/hotplug and touch-dependent cancellation/recovery remain deferred |

Do not reopen resolved OATH outcomes, retracted HSM claims or unrelated crypto/profile/Rust/producer
gaps merely to expand this integration. Historical serial 9681620 two-key failure and serial 125
pre-authentication `6A88` results remain unexplained/phase-bound as recorded. Windows receive readiness
does not convert the former unready handoff into all-gates-closed acceptance. Private keys,
attestation/certificate material and preserved private archives remain protected and out of this packet.

## 6. Source-derived validation commands for the later integration plan

**Nothing below was executed by this intake.** These commands are read from incoming toolchain,
project/workflow and handoff contracts, with exact project/class filters for the affected assets.
They are validation recommendations for separately authorized execution, not authorization to run
native/device/crypto operations. Preserve old outputs before using cleanup/publish targets; toolchain
retains only five runs. Read the RESULT block, native exit and skips, not just runner green lines.

### Linux-available software lane

```bash
python3 scripts/docs/test-docs-patches.py
dotnet toolchain.cs build
dotnet toolchain.cs test
dotnet toolchain.cs -- docs-qa
dotnet toolchain.cs -- docs-architecture
dotnet toolchain.cs -- native-aot-contract-qa
dotnet toolchain.cs -- resilience --fast
dotnet toolchain.cs complexity
dotnet toolchain.cs windows-hid-verification-build
dotnet toolchain.cs windows-hid-verification-evidence-test
```

`windows-hid-verification-evidence-test` depends on the standalone build target; listing both makes
that compilation scope explicit, not two independent passes. It runs 21 child/coordinator guard
cases plus 19 publication guard cases portably, **without Windows APIs**. Normal build/test/default
do not depend on opt-in Windows native targets. Native AOT contract QA validates references/anchors,
not native AOT execution. Do not copy the workflow's package publication or host-remediation steps
into this local preparation task.

Focused diagnostic selections, followed by the ordinary full unit gate above:

```bash
dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~Windows"
dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~BoundaryInventory"
dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~MacOS"
dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~Linux"
dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~Pcsc|FullyQualifiedName~SmartCardSharing|FullyQualifiedName~SmartCardConnectionFactory"
dotnet toolchain.cs -- test --project Ctap2.UnitTests --filter "FullyQualifiedName~Ctap2SessionWindowsCrossLayerTests|FullyQualifiedName~Ctap2SessionHidCrossLayerTests"
dotnet toolchain.cs -- test --project YubiOtp.UnitTests --filter "FullyQualifiedName~YubiOtpSessionWindowsCrossLayerTests"
dotnet toolchain.cs -- test --project Piv --filter "FullyQualifiedName~PivPinOnlyModeSessionTests"
dotnet toolchain.cs -- test --integration --project Core.IntegrationTests --filter "FullyQualifiedName~DeviceEventLifecycleTests"
```

The final command selects only the three **hardware-free** assertion cases; it must not become a
whole integration-assembly run. Registry validation must be paired with actual linked behavioral
execution and applet selections. New integrated counts are to be observed, not forced to historical
4,306, 4,442, 245 or 104 values. Fresh compilation/clean strategy and exact historical formatter holds
belong to Astra's execution plan; this intake neither formats nor promises blanket formatting-clean.

### Windows lane — separate runner authorization/prerequisites

```text
dotnet toolchain.cs -- test --project Core.UnitTests
dotnet toolchain.cs windows-hid-verification-nohardware
```

The **single** `windows-hid-verification-nohardware` target restores/publishes isolated fresh
self-contained win-x64 JIT and AOT trees, seals source/resolved-assets/artifact maps, validates
stability before/after execution, and runs both native pipe hosts. Do not join runner target names
with `+`. It requires Windows/.NET 10/native compiler-linker prerequisites and must fail if missing;
this review authorizes no installation, VM reconfiguration, runner control or device lease.
`windows-hid-verification-publish` publishes only; it is not execution. Standalone `--native jit`
output is not a substitute for the combined gate. The host on Linux exits 2 without native checks.

A later authorized physical requalification should select the source-derived narrow
`FidoHidOwnershipIntegrationTests`, `PcscLifetimeIntegrationTests`, explicit CTAP2 HID/SmartCard and
OTP routes, retain the first-open failure as an investigation input, and keep listener Started,
direct synchronous facades, removal/touch/two-key/driver races separate. Do not rerun broad ceremony,
PIN-normalizing or slot-mutating classes by default; `--smoke` removes only Slow/RequiresUserPresence,
not every mutation/PIN operation. Exact historical commands are in the named retained reports;
they are provenance, not permission to replay them.

### Mac lane — source-derived handoff, separate execution

On the later authorized final checkout, record commit/dirty/untracked inputs, `sw_vers`, `uname -m`,
`dotnet --info`, launcher/permissions, native package/deployed/loaded binding and fixture scope.
Use the incoming `adapter-consistency-macos-handoff.md` commands, including:

```bash
dotnet toolchain.cs build
dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~MacOS"
dotnet toolchain.cs -- resilience --fast --filter "FullyQualifiedName~MacOS"
dotnet toolchain.cs -- test --project Ctap2.UnitTests --filter "FullyQualifiedName~Ctap2SessionHidCrossLayerTests"
dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~BoundaryInventory"
dotnet publish verification/MacOSHidRouteVerification/MacOSHidRouteVerification.csproj \
  -c Release -r "$RID" --self-contained -p:PublishAot=false -o "$ROUTE_PUBLISH"
"$ROUTE_PUBLISH/MacOSHidRouteVerification" --timing-selftest
```

`RID` is the actual osx-arm64/osx-x64 process/native-asset architecture and `ROUTE_PUBLISH` a fresh
absolute output path. The Mac project defaults to AOT; the explicit false is necessary for this JIT
lane. There is no Mac verifier toolchain target. The separate experimental input verifier requires
an external native producer and must not substitute for production packaged-bridge testing.

After explicit fixture authorization and permission/topology checks, `--probe`, `--smartcard`,
`--otp-get`, `--otp-info`, `--expert-io`, `--expert-feature` each use `--serial "$SERIAL"`, one mode at
a time, requiring phase/PASS output, native exit zero and `child exit=0`. OTP/expert blockers remain
blocked until those prerequisites actually change and a source-bound run succeeds; do not silently
change Input Monitoring, remove a bystander key, elevate, or request touch/topology actions here.
The handoff's four narrow integration commands select expected 3/2/3/1 FIDO/PCSC/CTAP HID/SmartCard
methods; actual counts, firmware skips and effective AllowedSerialNumbers must still be checked.

## 7. Planning exit and stop boundary

Astra can now write the bounded integration plan using the fixed ancestry, this evidence split,
the other Sol owner's conflict map, the preserved audited qualifications and separately scoped
software/platform validation lanes. The plan should include a focused active-documentation receive
delta, source/manifest/API and caller-contract checks, fresh Sol review and exact final evidence
identity. It must not infer Windows readiness or waive Mac OTP/AOT/first-open/physical gaps because
the delivery PRs merged.

This intake performed read-only Git/source/report/hash inspection and wrote only new records.
No build, test, formatter, native/device/crypto execution, Git index/ref/merge/commit/push operation
or subagent was run. Old Core001, prior integrations and private/user material remain untouched.
**Stop here: preparation report complete; integration and its validation remain unexecuted.**
