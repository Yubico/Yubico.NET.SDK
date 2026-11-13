# Merge reconciliation preparation

**Recommendation only. No product merge, source edit, index update, build, test, native call, device operation, cryptographic exercise, or formatter run was performed.**

Inputs and preservation fingerprints are in [input-identities.md](input-identities.md). The machine-readable list of all 19 overlap records is [decision-map.json](decision-map.json). [proposed-ownership.md](proposed-ownership.md) assigns future resolution work, not permission to start it.

## Decision boundary

Use the incoming Windows lifetime implementation and internal adapter organization while retaining the audited branch's runtime corrections and truthful qualifications. Neither an incoming file nor a clean textual merge is sufficient reason to replace an audited runtime path. Retire descriptions of behavior that no longer exists; move still-valid qualifications to the actual new owner. Do not restore deleted classes to preserve their comments.

The recorded merge-tree preview has nine conflicted paths. The actual checkout inspected here has HEAD at the audited parent, an empty unmerged-index listing, and only the untracked RUN directory in status. Thus this document addresses the **object-database preview**, not an already applied merge. Do not manufacture an unmerged index to match the task description.

The 126 incoming change records intersect the 363-product audit map in 19 records (21 path spellings because two records are renames). All 19 are resolved below. A separate object-identity comparison checked 19,883 audited-side delta paths outside incoming path spellings: every one is identical in the preview to the audited parent. That is a static retention observation, not runtime equivalence or integration acceptance.

## Nine conflict resolutions

Paths below are repository-relative. `Core/` abbreviates `src/Core/` only in explanatory prose, never in the JSON map.

### 1. `CLAUDE.md`

Keep the audited file, including its corrected APDU pipeline, non-generic Management session description, logging accessibility/lifetime, struct ownership/boxing qualifications, source-backed examples and test-oracle limits. In the HID vocabulary bullet use incoming `IWindowsHidReportApi` and `IWindowsHidFeatureReportApi`, subordinate to their native lifetime owners. Keep the two adjacent audited APDU/application-session bullets rather than incoming's unchanged stale wording. No other instruction rewrite is needed.

### 2. `docs/architecture/raw-access-tiers.md`

Keep all audited Tier 0/1 explanations and examples: lazy raw HID initialization and inherited `0.0.0`; effective versus detected firmware; uncertain first-fragment APDU recovery; CBOR `0x10`/`0x90` cancellation normalization limit; no-response CTAPHID_CANCEL and original-response drain; OTP local deadlines versus native completion; SCP selection, contained ownership, exact GlobalPlatform editions and qualification gaps; response-owner clearing and whole-workflow sequencing.

At the Tier 2 conflict keep the general original-task/borrowed-memory paragraph, **remove its obsolete Windows compatibility assertion**, and add incoming's Windows FIDO overlapped and OTP blocking-worker paragraphs. The compatibility warning now applies only to custom interfaces. Preserve targeted CancelIoEx request versus terminal completion versus resource-last-use/retirement versus checked release, native success winning cancellation, and the known pre-dispatch BCL preparation-failure retention case. Public fault completion is not safe-reopen evidence.

Keep incoming's Windows direct-facade rows, exact descriptor payload SET rules, pre-open original-claim transfer and Windows terminal-wake shutdown distinction. Keep audited macOS direct IO fixed-input normalization and short successful feature GET padding qualifications. Do not add a macOS timeout to Linux or Windows. Keep custom fallback warnings and all existing monitor/discovery qualifications.

The preview still says Windows registry reconciliation is pending despite incoming's refreshed registry files. Incoming's BoundaryInventory README records 59 operations/104 profile links, 31 public reachability entries, and 245 classified source sites, with 229 unchanged full identities and 16 changed by renames/extractions. Receive these as incoming static inventory claims and reconcile final source against them; no scanner was run here. Describe 43 operations/76 links as the historical L6a checkpoint only; reflection/link counts are not native acceptance. Do not promote the incoming Windows historical results to acceptance of the merged source.

### 3. `src/Core/src/Devices/HidConnectionSlot.cs`

Keep incoming executable code and constructor seams: renamed macOS seams; Linux seams; fresh unopened Windows API factories per acquisition; Windows in `OwnsNativeLifetime`; Windows FIDO/OTP owner routing; and the explicit factory-construction/no-native failure release path. Preserve the existing registration/discovery transfer, wrong-transport rejection, raw pre-cancellation and no post-transfer early cancellation shortcut.

Resolve the comment to: `Custom interfaces open synchronously before Task.FromResult. Their adapters also call report I/O inline; a Task return type adds neither native cancellation nor release proof.` Do not leave Windows in that warning or route it back through compatibility adapters. Keep runtime messages and public names untouched.

### 4–5. Deleted Windows report-access files

Accept incoming deletion of both:

- `src/Core/src/Native/Windows/HidD/IWindowsHidReportAccess.cs`
- `src/Core/src/Native/Windows/HidD/WindowsHidReportAccess.cs`

The preview retains ours verbatim for these modify/delete conflicts; leaving them would retain dead code, not preserve an active contract. Map qualifications to `Transports/Hid/Windows/IWindowsHidReportApi.cs`, `IWindowsHidFeatureReportApi.cs`, the two concrete APIs, the two owners, and direct facades:

- Native descriptor lengths include report ID; public facade payload sizes exclude it. Typed FIDO64 and OTP8 remain separate contracts.
- Successful receive payloads are independent caller-owned storage; report-ID staging and protocol/app output have separate clearing owners.
- FIDO needs read/write desired access and overlapped submission; OTP opens with zero desired access for feature calls, with no FIDO fallback. ACL, collection, driver, token and existing sharing policy still govern success. Metadata acquisition is not feature-I/O or permission proof; elevation is not a universal prerequisite.
- FILE_SHARE.ALL does not override another handle's share restrictions or acquire a grouped SDK claim.
- Retire the old normal-handle/null-OVERLAPPED, inline typed Windows I/O, unchecked-close, constructor metadata-fallback and staging-left-to-GC descriptions. They describe the removed implementation, not defects to reintroduce into the new one.
- Preserve the incoming access-denied runtime diagnostic strings; qualify explanatory prose instead of changing runtime help.

### 6. `src/Core/src/Transports/Hid/Linux/LinuxUdevHidEventSource.cs`

Use incoming's implementation-only file, retaining the audited `WaitForEvent` comment that distinguishes the listener's scoped poll leases from the report reactor's registration-wide references. Do not retain the old combined-file declarations inside it. Otherwise they duplicate incoming `LinuxHidPollOutcome.cs`, `LinuxHidUdevEvent.cs`, and `ILinuxHidEventSource.cs`.

Move the audited interface qualifications into **`ILinuxHidEventSource.cs`**, which merges cleanly but contains the old pre-audit wording: fake loop-policy/caller-stop tests do not establish native poll completion or a shutdown deadline; null receive can mean no event or failed reception and requests rescan; SignalShutdown is only an attempted wake, not acknowledgment, drain or release. Preserve the existing four-attempt EINTR signal policy, EAGAIN handling, handle leases, event ordering, null rescan behavior and listener runtime unchanged. This is a documentation migration, not a shutdown bug fix.

### 7. `src/Core/src/Transports/Hid/MacOS/IMacOSHidReportApi.cs`

Keep incoming's renamed interface and signatures; retain audited ownership/status-handling, seam-not-drain-proof, and persistent-registration pin/root qualifications. Replace the deleted Windows seam name with the two live Windows report API seams, or omit the analogy. Keep the default OpenDeviceResult behavior and checked-close distinction.

Remove the old `IOKitDeviceLifetime` implementation from this interface file. The actual implementation is incoming **`MacOSHidReportApi.cs`**. Port there the audited summary (native forwarding plus small managed compositions/status handling, not a one-for-one pass-through) and its non-seizing comment (process-local admission versus OS permission, without unqualified Rust/Python parity claims). Keep incoming implementation and new type name. Close, release of CF references, callback quiescence and physical-claim release are separate events.

Keep incoming `IMacOSHidInputBridge`, `MacOSHidInputBridge`, `MacOSFidoHidNativeOwner` and `MacOSOtpHidNativeOwner` composition. The input bridge owns native callback mechanism/state, not a second scheduler or physical claim. Do not duplicate old nested owners or restore `IIOKitDeviceLifetime`.

### 8. `src/Core/src/Transports/SmartCard/PcscNativeResources.cs`

Keep incoming `_connectionApi`, constructor parameter `connectionApi`, all forwarding and `PcscConnectionNativeOwner` cref. At disconnect use the audited comment, followed by incoming `_connectionApi.Disconnect(cardHandle, SCARD_DISPOSITION.UNPOWER_CARD)`. Keep both checked disconnect and checked context release before release proof. UNPOWER is a requested native disposition, not an observed power cycle or PIN clearing. No call sequence, sharing default or retry policy changes.

### 9. `src/Piv/README.md`

Keep the complete audited guide. In Maintainer verification combine its qualified `--project Yubico.YubiKit.Piv` selectors and archive-host explanation with incoming's focused `PivPinOnlyModeSessionTests` command and owned-response-only oracle qualification; use the qualified selector on that new focused command too. Retain all audited physical/firmware/transport/OS limitations and F9 preservation. Do not weaken the existing examples, lifetime cleanup or firmware-provenance statements.

The incoming test change intentionally removes unsafe observations of command-pool memory after return. Accept that change unchanged; there is no replacement pool-injection seam or pre-return pool-clearing runtime oracle. Production authentication remains unchanged. The parent's evidence owner must attach the **byte-original human rollback instruction** to its receipt; this plan neither reconstructs nor paraphrases it as an authoritative quote. Do not reintroduce the rejected seam under integration authority.

## Ten clean overlap records: explicit acceptance and qualifications

| Path | Recommendation |
|---|---|
| `docs/SDK-HOUSE-STYLE.md` | Keep incoming native-adapter role/layout section and audited illustrative-example, named-token, readiness/provenance and workflow-guard qualifications. Mechanical API resource handling remains subordinate to owner scheduling. |
| `src/Core/CLAUDE.md` | Keep both sets of changes. Correct the clean-merged concurrency bullet still saying Windows/custom disposal has compatibility semantics: built-in Windows now requires route-specific native release proof; only custom HID retains wrapper semantics. Keep incremental grouped admission, not atomic preflight; budgets are caller waits, not native release. |
| `src/Core/README.md` | Keep incoming owner/navigation/Windows sections and audited monitor cleanup/logger caveats, firmware and memory ownership, transaction-end oracle, raw/SCP examples and sharing qualifications. Keep logger category's approved internal rename and the explicitly preserved legacy disposed-object string. Historical Windows checkpoint is not final merged-source qualification. |
| `src/Core/src/Native/Windows/HidD/HidD.Interop.cs` | Keep incoming byte-return/uint-length entry points and audited residual ABI warning. Make that warning precise: remaining old Bool-return imports remain uncorrected; new GetHid*/SetHid*/FreeHid* declarations already use byte. HIDP_CAPS remains signed short storage; new API reads reinterpret ushort. Do not claim every import or consumer was fixed, and do not implement remaining ABI fixes here. |
| `src/Core/src/Transports/Hid/MacOS/MacOSHidFeatureReportConnection.cs` | Keep incoming seam names and audited expert GET8/padded-short versus typed exact8, supported SET length, worker and close-drain remarks. |
| `src/Core/src/Transports/Hid/MacOS/MacOSHidIOReportConnection.cs` | Keep incoming bridge names and audited descriptor-metadata versus normalized input64/zero-prefixed65, terminal other lengths, supported SET lengths and six-second reader-detach remarks. |
| `src/Core/src/Transports/SmartCard/SmartCardConnectionFactory.cs` | Keep incoming `PcscConnection` and `connectionApi:` calls with all audited sharing/coexistence wording and complete scoped examples. Exclusive default, no implicit shared retry, factory policy snapshot and UNPOWER request stay unchanged. |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/PcscConnectionLifetimeTests.cs` | Keep incoming construction names and all audited XML qualification. The scope-end test holds transmit, not native EndTransaction; failed end closes admission and later shutdown reports error independently of native-release proof. Do not rename tests or strengthen assertions. |
| `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Transports/SmartCard/SmartCardSharingTests.cs` | Keep incoming types and approved `NativeState_` → `NativeOwner_` test rename; retain audited policy/request-only summaries and all assertion values. No physical PIN/power/process-isolation claim. |
| `src/Piv/CLAUDE.md` | Keep audited guide plus incoming owned-response capture/no pool-runtime-oracle paragraph. Keep direct/hidden ownership, full-workflow lifetime, detected/effective firmware distinction, F9 and physical deferrals. |

## Clean extraction and cross-layer hazards beyond those overlaps

1. **Linux splits.** Keep incoming `ILinuxHidNativeApi.cs` (including `LinuxIoResult`), `ILinuxHidReadRegistration.cs`, `ILinuxHidReadinessNative.cs`, `LinuxHidPollOutcome.cs` and `LinuxHidUdevEvent.cs`; keep corresponding old containers stripped of moved declarations. Registration Activation is not Quiescence; RequestRemoval is not drain; ReleaseReference runs after real quiescence on the original owner worker; checked close is separate. The extracted registration/native/readiness comments already retain these qualifications. Do not duplicate them to satisfy old source-path maps. The event-record HID hint is diagnostic, not physical identity attestation.
2. **macOS extraction.** No audited-side runtime delta exists in the old FIDO/OTP combined files relative to the natural base. Incoming extraction changes names/layout and constants' ownership; normalized source comparison shows the moved worker/admission/callback/close flows. This supports retaining incoming organization, not native-equivalence certification. Keep callback buffer/root ownership and output-copy drain; no native or hardware macOS run is claimed.
3. **PC/SC moves.** Keep `PcscConnection.cs`, `PcscConnectionNativeOwner.cs`, `NativeSCardApi.cs` and `NativeSCardConnectionApi.cs`; do not restore `UsbSmartCardConnection.cs` or duplicate extracted implementations in the interface files. Preserve worker confinement, admission retirement before public completion, original claim quarantine, held disconnect/context-release ordering and transaction end dispositions. Preserve the literal `ObjectDisposedException("UsbSmartCardConnection")`: it is intentionally retained runtime compatibility, not a missed source rename. `PscsConnectionKind` and `DesktopSmartCardDeviceListener` are unchanged public names.
4. **Active unchanged architecture map:** `docs/architecture/connection-ownership-and-contention.md:119–123,218–222` still describes the removed Windows compatibility lifetime/no-wake path. Replace only those obsolete Windows clauses with owner-directed completion/retirement/checked release and terminal-wake shutdown; retain audited custom-disposal, workflow-drain, grouping and no-safe-reuse qualifications. Its OTP platform note must not imply universal permission sufficiency or unconditional keyboard denial. No PIV physical-release claim follows.
5. **Active unchanged source comments:** `src/Core/src/Devices/RegisteredConnections.cs:27` and `SharedDeviceInfoRead.cs:270` list only macOS/Linux. Add Windows to the built-in owner-transfer description, preserving wrapper fallback policy and exact pre-open discovery transfer. Their executable selection already follows `OwnsNativeLifetime`; no runtime edit is required.
6. **Active unchanged verification example:** `verification/LinuxHidRouteVerification/BlockingReportConnection.cs:11` calls the blocking adapter supported for Windows/custom providers. Narrow that explanation to custom providers/test contrast; built-in Windows now bypasses it. Keep diagnostic behavior untouched.
7. **Active API/examples search:** no additional live old-type references were found outside the named conflicts, Core guide's intentional legacy diagnostic, and BoundaryInventory's explicit predecessor mappings. Preserve those historical mapping entries and their source identities. Public `ISmartCardConnection`/`IHidInterface`/`IHidConnection` signatures, public named parameters, CLI/runtime help and assertions stay unchanged except incoming's approved deltas. Do not rewrite v1 migration mentions of `Transport.UsbSmartCard` or `FeatureFido2UsbSmartCard`: they are not the deleted class.
8. **Registries are active, archives are frozen.** Receive incoming BoundaryInventory scanner, manifests, operation profiles and test links together, then reconcile final source locations/counts. Old names in explicit migration/evidence descriptions are not active binding errors. Do not run codemapper, regenerate frozen source/example inventories or edit historical host injections to chase new type names. New example validation, if later authorized, needs a new RUN-owned context.

## Minimal future validation, not run here

Start only after a parent-approved resolution set and source-edit authority. First check exact parents, an approved changed-path allowlist, no unresolved markers, one definition per extracted type, all crefs/call sites using live names, original PIV rollback instruction receipt, and all frozen on-disk fingerprints. Check non-overlap audited runtime files remain unchanged against ours, not merely against incoming. Compile/test commands must use the repository build/test skills; do not broaden to hardware automatically.

| Lane | Existing tests / required assertion boundary |
|---|---|
| Windows FIDO managed | `WindowsFidoHidNativeOwnerTests`, `WindowsFidoPreparationFailureTests`, `WindowsHidReportApiTests`, `WindowsHidSafeHandleTests`: immediate and pending acceptance require callback completion; callback-before-submit-return retains storage/task; cancel/not-found is not completion; retire once after both barriers; pre-dispatch cancellation submits none; known allocator failure faults actual task but retains unproven resources/claim; held close retains claim. |
| Windows OTP/direct/routing managed | `WindowsOtpHidNativeOwnerTests`, `WindowsOtpLifetimeTests`, `WindowsHidFeatureReportApiTests`, `WindowsFidoDirectFacadeTests`, `WindowsOtpDirectFacadeTests`, `WindowsFidoHidRoutingTests`, `WindowsOtpHidRoutingTests`: original claim through held/failed open, operation and close; one ordinary call/no backlog; owned send copy and post-last-use clearing; separate successful receive storage; no invented HidD transfer count; direct descriptor size versus typed64/8; factory failure with no acquisition releases exactly once. |
| macOS extraction regression | `MacOSHidConnectionLifetimeTests`, `MacOSHidFidoReadCancellationTests`, `MacOSHidFidoRouteLifetimeTests`, `MacOSOtpRouteTests`, `MacOSOtpRecoveryTests`, both facade and compatibility test classes: borrowed idle reuse, reader detach, actual public task versus disposal completion, held output, callback acknowledgment/destruction before CF release, unknown close retention, input64 normalization and expert-short OTP behavior. Fake seams do not qualify macOS native execution. |
| Linux split regression | `LinuxHidRoutingTests`, `LinuxDirectFacadeAcceptanceTests`, reactor/report/lifetime and Linux listener resilience tests: same fd0/ABI and framing, exact registration identity/generation, real quiescence before reference release/CloseOnce, EINTR/EIO release diagnostics versus unknown quarantine, listener null rescan and bounded signal-attempt policy. Preserve existing independent-PID admission/evidence retirement tests. |
| PC/SC regression | `PcscConnectionTests`, `PcscConnectionLifetimeTests`, `PcscContextIsolationTests`, `SmartCardConnectionFactoryTests`, `SmartCardSharingTests`, `PcscDiscoveryLifetimeTests`: exclusive/no retry and opt-ins; no native calls after pre-dispatch cancellation; held borrow blocks teardown; disconnect and context release held independently; end failure closes admission and is reported later; isolated quarantine/finalizer probes retain their existing process boundaries. |
| Cross-layer managed | `Ctap2SessionWindowsCrossLayerTests` and `YubiOtpSessionWindowsCrossLayerTests`, plus existing macOS/Linux companions: actual slot/device/owner composition with fake native seams, borrowed idle sequential sessions, held initialization returning pending, CTAPHID cancellation and correlated reuse, OTP abort before recovery and applet CRC-owned terminal presence, owned-session claim retained through close. No new wire values, prompt operation names or recovery policy. |
| API / inventory / PIV managed | Core `RawAccessPublicApiTests`; PublicApi `HidConnectionCompatibilityTests`, factory/session/async and return contracts; BoundaryInventory source and profile-link tests; PIV `PivPinOnlyModeSessionTests`. Public signatures/named arguments stay stable; inventory identity migration is not global attestation. PIV asserts response clearing, recovery candidates and authentication/witness outcomes only, not pooled command clearing after return. |

For eventual managed integration acceptance, build the affected solution with zero warnings/errors and run focused Core, PublicApi, Ctap2, YubiOtp and Piv unit lanes through the toolchain. If runtime-loop/lifecycle source changes are actually made, include the required fast resilience lane. Verify selected tests really executed using RESULT blocks, rather than treating zero tests or skipped platform probes as success. Do not execute archived audit hosts as part of broad project substring matching.

Windows real completion/HID-driver/JIT/AOT, sealed publication and permissions, macOS native extraction acceptance, and any attended device ceremonies remain separately authorized lanes. Receive the other evidence owner's exact producer/source/version/device/firmware/transport/OS qualifications; never transfer a historical checkpoint result to this merged tree. No PIV physical PIN isolation, power cycle, separate-process access or release acceptance is made here. Broader ABI, crypto, protocol-normalization, logger-cleanup and listener fixes remain deferred rather than implemented under merge reconciliation.
