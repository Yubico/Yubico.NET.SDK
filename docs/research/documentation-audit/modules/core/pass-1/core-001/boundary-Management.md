# Management / Core boundary investigation

**Outcome:** the assigned current-source boundary investigation is complete, with explicit mechanism
handoffs to **B4 and B6**. All **331 original primary IDs** have dispositions: 204 read-clean and 127
candidate-context. The packet assigns zero shared primary IDs. All 55 candidate-file dispositions,
including eight context-only paths, are retained. This completes this investigation packet; fresh
triage, editing, independent review, compilation and platform/device qualification remain separate.
It completes neither the other eight boundary packets nor Core Pass 1.

Source remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; observed records-only HEAD was
`bb94f0884f63f260156227957d4cc112d984e300`. Every candidate source file matches that pin after
explicit UTF-8 BOM/newline normalization; all assigned raw/range hashes match the inventory.
The exact packet hash is `c688144c377edc718aa530296747c13b17695e52ee0140822fbf0612e4e964d8`.
The 47 paths containing selected units and the 55 disposition paths are different populations.

Only `boundaries/Management/*` and this report were written. Product code, assertions, API snapshots,
runtime strings, central coverage, inherited registries, shared indexes, original user work and frozen
records remain unchanged. No build, test, formatter, runtime experiment, native/device operation,
reset, CLI device command, subagent, commit, index or ref write ran. Python/Git performed static
record/source checks and two focused original-source captures. Runtime observations below come from
source/assertions or explicitly attributed earlier evidence, rather than a new execution.

## Records and reading scope

| Record | Purpose |
| --- | --- |
| [Unit dispositions](boundaries/Management/unit-dispositions.json) | Every assigned ID, full range and source/range hash; read context, candidates and pending independent review/validation |
| [File dispositions](boundaries/Management/file-dispositions.json) | Complete contextual reads, pinned Git identities, unchanged-path actions, all 55 candidate paths |
| [Candidates](boundaries/Management/candidates.json) | Three local concern groups, exact IDs/paths and reopening triggers; zero accepted Core allocations |
| [Inherited outcomes](boundaries/Management/inherited-dispositions.json) | All nine assigned provenance rows, their unchanged authoritative states and original definitions |
| [Shared dependencies](boundaries/Management/shared-dependencies.json) | Secondary routing to existing readers and future SHARED editor; zero new shared coverage claims |
| [Mechanism identities](boundaries/Management/mechanism-identities.json) | Exact secondary source hashes, inspected ranges and intersecting original IDs, without taking primary ownership |
| [Sources](boundaries/Management/sources.md) | Reused originals, current manufacturer passages, framework orientation, authority and bounded gaps |
| [Verification](boundaries/Management/verification.json) | Exact ID/hash/source reconciliation and explicit unexecuted dimensions |
| [Static receipt](boundaries/Management/verification-receipt.json) / [clean nominees](boundaries/Management/clean-nominees.json) | Separate record checker and four substantive later sampling nominees; neither is independent content acceptance |

Root/Core/Management README and CLAUDE instructions, nested test instructions, Tests.Shared guidance,
campaign workflow/style, approved plan/planning review, inventory/dispatch, registry/boundary seeds/routing,
source ownership and completed A/B1/B2/B3/C/B5 reports were read. Every assigned unit was read with its
enclosing implementation, examples, assertions and cleanup. Context-only files preserve the existing
inventory exclusion rather than receiving invented IDs. Narrow Core/shared reads establish the actual
call mechanisms below; those units retain their original primary owners. Existing applet-only findings
remain in the frozen Management run, rather than receiving a new whole-module audit here.

## 1. Select and create the actual Management session

`IYubiKeyExtensions.CreateManagementSessionAsync` reads SCP/preference values, calls
`YubiKeyConnectionExtensions.ResolveSessionTransport`, copies the supplied settings with
`WithPreferredConnectionType`, and calls `CreateSessionOverTransportAsync`. That helper opens exactly
one typed connection and awaits the direct factory. On success the extension calls `OwnConnection()`.
On factory failure the helper awaits hidden connection disposal and preserves the original creation
exception even if cleanup/logging also fails. Native release proof remains the connection owner's job.

| Entry or condition | Actual choice and result |
| --- | --- |
| Default exposed interfaces | First exposed SmartCard → HidFido → HidOtp; capability masks do not select the transport |
| Concrete preferred type | Must be in the Management order and exposed; invalid group/combined/Unknown throws ArgumentException; absent valid type throws NotSupportedException |
| SCP with no preference | Requests SmartCard; absence fails rather than selecting plaintext HID |
| SCP with explicit HID | Selected HID can be opened; Prepare rejects SCP before protocol initialization I/O, then failure cleanup runs |
| Open/initialization failure | Original failure propagates; no other interface is tried |
| Direct `ManagementSession.CreateAsync(connection, options)` | Borrows supplied connection; preferred type asserts its existing Type, without reconnecting |
| Unsupported direct connection | Constructor's EnsureSupportedConnection rejects before base construction/attachment |

`YubiKeyDevice.ConnectAsync<T>` resolves a concrete slot, claims all known interface IDs through
`DeviceConnectionRegistry`, then opens that selected slot. Built-in PC/SC/Linux/macOS owners receive the
exact token before native acquisition. Other routes use registered wrappers. Exclusion is process-local
and bounded by overlapping known IDs; disjoint scoped views remain uncoordinated. The selection helper's
no-fallback policy is distinct from an explicitly configured PC/SC sharing fallback within one open.
Default PC/SC sharing is snapshotted when its factory is constructed; changing the process policy later
leaves existing factories/connections unchanged (B1's completed source scope).

### Initialization and failure trace

`ConstructInitializedAsync` validates preference, constructs the session and attaches
`ConnectionSessionGuard` before any wire work. `InitializeSessionAsync` checks holder/disposal and
publishes readiness only after the entire Management callback succeeds.

1. `PrepareProtocolAsync` constructs/registers the protocol as the session's disposal owner, rejects
   HID/SCP, checks cancellation and initializes the applet FIDO HID channel when selected.
2. SmartCard calls `SmartCardBackend.SelectApplicationAsync(ApplicationIds.Management)` and parses its
   returned version; OTP calls `OtpBackend.ReadStatusVersionAsync`; FIDO uses its prepared INIT firmware.
3. `ResolveFirmwareVersionAsync` calls `DeviceInfoReader.ReadAsync(protocol, null, token)`. Its parsed
   firmware wins. A non-cancellation failure logs and uses the transport fallback; cancellation propagates.
4. `ConfigureProtocolAsync` configures the effective version and optional SCP. Management then constructs
   its backend over the effective protocol. Only full callback completion sets IsInitialized.

The override leaves all preceding probes in place. Initial metadata precedes SCP establishment.
Creation failure invokes synchronous `DisposeAfterInitializationFailure`; protocol/guard cleanup runs,
while a directly borrowed connection remains caller-owned. That synchronous cleanup can wait for drain.
The convenience outer helper additionally disposes its own hidden connection asynchronously. B6's return
must settle detailed partial-SCP response/key/processor cleanup before any stronger cross-layer assurance.

## 2. Resources and authentication

| Resource/input/result | Actual owner and lifetime | Relevant limit/action |
| --- | --- | --- |
| Caller-opened IConnection | Caller; direct session borrows | Dispose session first, then connection; healthy sequential session reuse is supported |
| Hidden convenience connection | Session after OwnConnection; outer helper on creation failure | Dispose returned session with await using; no session finalizer supplies recovery |
| Protocol and optional SCP wrapper | ApplicationSession.Protocol | Prepare registers ownership before setup completes; backend only borrows |
| Management backend | Session retains mechanical protocol user | No IDisposable/resource ownership; no separate native or session guard |
| SessionCreationOptions | Factory reads values; extension makes preference copy | Object is not a lasting session cache; references retain their own ownership rules |
| ProtocolConfiguration / FirmwareVersionOverride | Passed into initialization/configuration | Effective policy is distinct from returned metadata; no deep service snapshot implied |
| ScpKeyParameters | Caller; borrowed for Management initialization | Keep alive through actual factory completion; dispose owned parameters and original source secrets separately |
| StaticKeys / Scp03 parameters | StaticKeys copies source arrays; Scp03 retains and disposes that Keys object | Default property creates a fresh disposable owner on every access; it is not a singleton |
| Scp11 parameter contents | References public/private key objects; copies certificate collection, retains certificate objects; Dispose reaches private key/certificates | C-C020/B6 own alias/profile semantics; no Management caller constructs SCP11 credentials in this packet |
| UserPresencePrompt | Management passes no prompt to its base constructor | Management retains/invokes/disposes none; FIDO/backend/read paths use None notifications |
| Current/NewLockCode | Caller-owned memory; operation encoder copies into TLVs/encoded payload | Keep source valid through awaited operation; caller clears sources in independent finally |
| Encoded config | DeviceConfig result transfers to session | Session clears it in finally after actual backend task completes, including failure/cancellation |
| DeviceInfo / DeviceInfoResult | Value result containing scalars plus memory-backed timeout | A-C003: copied record/result/list does not create independent timeout storage |
| Discovery snapshot / fixture snapshot | Core device stores atomic snapshot; Tests.Shared stores its separate initial live result | Neither is refreshed by Management writes or by a later live Management read |
| Native handles/read workers/physical claim | Selected connection/native owner, not Management backend | B1/B2/B3 release-proof distinctions apply; B4 Windows conclusions remain pending |

Inherited `IsAuthenticated` is set by successful protocol SCP configuration. A Management configuration
lock instead appears as `DeviceInfo.IsLocked`, with authorization supplied anew by CurrentLockCode on
each protected write. Management has no session IsLocked or unlock state. Successful lock authorization
neither removes the stored lock nor implies SCP authentication. SCP key kind/peer and management-authority
interpretations require B6's profile return; inherited IsAuthenticated alone supplies no host-management
authorization guarantee.

Disposal immediately makes readiness/authentication false. ApplicationSession shares one sync/async
teardown outcome, disposes/drains the protocol, detaches the guard and disposes only its owned connection.
Actual dispatch depends on the selected IDisposable/IAsyncDisposable implementation. This is managed
lifetime behavior; physical card state, device reset completion and cross-process isolation require
separate evidence. B1's requested UNPOWER policy is not proof of a physical card-state transition.

## 3. Operations through Core

| Operation | SmartCard | FIDO HID | OTP HID |
| --- | --- | --- | --- |
| Initialization fallback | Management SELECT and response-version parse | Prepared CTAPHID INIT version | ReadStatusAsync, including initial status initialization; protocol version preferred |
| GetDeviceInfoAsync | Core DeviceInfoReader → INS 1D, P1=page | Same reader → vendor C2, one page byte, None notification | Same reader → CmdYk4Capabilities (13), page 0 empty, later page byte; validates info CRC |
| SetDeviceConfigAsync | SmartCardBackend → INS 1C | FidoHidBackend → C3, None notification | OtpBackend → CmdYk4SetDeviceInfo (15) |
| ResetDeviceAsync | SmartCardBackend → INS 1F after disposal/feature check | Session rejects without reset exchange | Session rejects without reset exchange |
| Internal legacy SetMode | Backend INS 16 | Backend C0 | CmdDeviceConfig; no current public session method |

Every public read calls Core, rather than a backend read or session metadata cache. DeviceInfoReader
checks declared page length, duplicate pagination markers, remaining-page count and byte page-index
limit, accumulates non-pagination TLVs, then calls `DeviceInfo.CreateFromTlvs`. SmartCard default error
handling reaches PcscProtocol/ApduException; HID framing/device errors and metadata parse errors retain
their layer's error type. Ordinary metadata-probe errors during creation alone have the fallback catch.
OTP inbound info CRC belongs to the reader; outbound framing CRC belongs to Core OTP protocol. Backend
writes ignore returned payloads and rely on protocol outcome. B6 retains final APDU/status/response-owner
review; no general status-word immutability or all-error cleanup guarantee is inferred here.

Set checks disposal before feature/argument checks, validates present 16-byte codes, serializes and
awaits backend cleanup. Null config properties omit TLVs; non-null masks/flags replace complete fields.
Exact USB None is rejected. SDK timeout limits and numeric narrowing leave device-supported masks,
arbitrary cast widths and field-specific NFC support outside comprehensive validation. Those existing
applet findings remain deferred; this boundary work grants no new contract or implementation fix.

## 4. Metadata freshness and firmware provenance

| Value | Current source and meaning |
| --- | --- |
| Transport fallback | SELECT text/version, FIDO INIT, or OTP status; used only when initial metadata probe fails ordinarily |
| Management `_version` | Initial DeviceInfo's interpreted version, otherwise transport fallback; can already reflect non-final qualifier policy |
| Inherited session FirmwareVersion | Override ?? `_version`; used for protocol Configure and write/reset Feature checks |
| Later DeviceInfo.FirmwareVersion | Fresh reader passes `_version` as default; CreateFromTlvs validates tag05 but supplied default wins, except non-final qualifier firmware wins again |
| VersionName | Firmware for Final; qualifier display otherwise; not an extra capability probe |
| CachedDeviceInfo | Nullable atomic last-known Core snapshot, read without I/O; no Management write/live-read publication into it |
| Test-state DeviceInfo | Separate live one-shot result acquired during lazy fixture initialization, then retained for callbacks/filtering |

The live-read claim concerns I/O and current configuration fields. It should also explain the captured
firmware fallback/precedence (X002). Complete raw metadata/unknown TLVs are not published by DeviceInfo
(A-C016). Timeout memory aliases TLV/caller storage and content equality/hash can change when that
storage changes (A-C003). Reader cleanup must be coordinated with ownership redesign rather than adding
disposal mechanically. Replacing a device's snapshot preserves its earlier scalar value copy, but this
does not establish deep memory independence.

The actual metadata decoder still routes FIPS tags 14/15 to FromApp (A-C004 / MGMT-XREF-001). The distinct
FromFips helper has different mapping and a nonzero one-byte width problem. Form-factor high bits set
IsFips/IsSky and remain in the full enum cast (A-C005 / MGMT-XREF-002). IsFips is a manufacturing indicator.
Read-only and display tests comparing values produced by that same decoder cannot establish independent
FIPS mapping, application approval, normalized form factor or Bio MPE reset eligibility. Wider ordinary
application integers retain A-C006's truncation/sign-extension question. These are current limitations,
not intended future representation contracts.

Find creates a fresh scope-specific finder and retires its read scope in finally. Returned slots remain
openable; only in-scope transport/probe paths participate. A monitor instead owns listeners, its sequential
scan/publication loop and feed; subscriptions replay additions/SnapshotComplete then live diffs. The
creation token governs startup, while a returned monitor has its own lifetime. Retained objects keep
immutable connection sets; SnapshotDiff updates non-null changed metadata or replaces the object when
member/connection sets or contradictory known serial evidence demand it. Independent Find neither
refreshes a prior result nor forces a monitor publication.

The separate serial latch retains known serial when later metadata omits it. PID/grouping evidence is
configuration-sensitive: A-C017 records that the metadata cache lacks the identity cache's PID check;
A-C021 records successful serial-less identity caching despite retry prose. Missing listener hints and
absence observations can leave stale evidence. Reconfiguration callers should end old scopes, discover
again, match the intended key using available identity evidence and verify live metadata before further
mutation. Finding the first result, seeing a known PID or comparing an old object supplies no readiness
or perfect grouping guarantee. A-C011's accepted partial-evidence bound remains relevant to selection.

## 5. Configuration, reset, cancellation and recovery

A reboot TLV requests restart after the device accepts the complete configuration and required current
lock code. SetDeviceConfigAsync leaves managed session readiness intact; it neither updates discovery
metadata nor reconnects old handles. End old scopes and rediscover/reopen after the requested reboot.
Persisted readback without reboot establishes reported settings, separately from active interfaces.

Reset checks effective firmware 5.6 then backend kind and sends SmartCard INS 1F. It has no family check,
no Management configuration/lock-erasure verification and no successful-reset readiness invalidation.
Keep `management-001:MGMT-IMP-010` open for product/lifecycle decisions. The manufacturer's Bio MPE
description establishes combined FIDO/PIV/shared-PIN scope and F9 retention; its broad ALL applications
wording does not establish Management configuration-lock recovery. Apply the documented family scope
and obtain fresh lifecycle/device evidence before strengthening reset or post-reset reuse claims.

`ExchangeGuard` admits one logical exchange or refuses overlap, supplies an uncancellable constituent
token and closes/drains that admitted exchange on disposal. A multi-page read has one guard admission
per page, so another workflow can run between pages; post-exchange parsing and detached callback work
also lie outside that drain. Caller code must await the entire public workflow before disposal or the
next operation. Re-entrant disposal can self-wait; use non-reentrant enclosing scopes.

SmartCard cancellation before admission prevents exchange dispatch; an admitted chain drains without
rollback. FIDO passes the caller token separately into its keepalive loop, may send CANCEL after a
keepalive, and drains the terminal response. No arriving report can mean indefinite pending work. OTP
uses caller-token touch-wait handling and reset/abort recovery, latching failure if reset fails. These
are protocol behaviors, not native interruption or release proof. Management uses silent notification
handles. B5-C002/C003/C006/C008 retain malformed/cancel/workflow and oracle qualifications.

On Linux/macOS, typed native owners retain claims through actual drain/checked release, with transport-
specific cancellation and uncertain-release quarantine (completed B2/B3). An outer canceled wait or a
disposed subscription supplies no permission to clear input still borrowed by an underlying operation.
Windows/custom HID behavior and the future user-ready Windows change remain B4 dependencies.

## 6. Inherited outcomes, preserving original identities

| Run-qualified provenance | Current boundary disposition |
| --- | --- |
| `management-001:MGMT-XREF-001` and `audit-v2-20261022-20261031:deviceinfo-FIPS-residual` | Same nonadditive mapping group; A-C004 source concern remains. Current Management docs already qualify it; integrated nonzero tag vectors and authoritative tag width/mapping remain needed. |
| `management-001:MGMT-XREF-002` | A-C005 raw flagged enum remains. Exact shared filters/formatter and Management tests/display inherit it; normalized-plus-raw design remains undecided. |
| `management-001:MGMT-XREF-003` | Earlier row/cache prose stays documentation-resolved at its original checkpoint. Current fixture still binds one placeholder to first matching authorized state; separate live initialization versus Core CachedDeviceInfo is now rechecked. A-C013 and X001 reopen stale explanations; no new runtime row defect. |
| `management-001:MGMT-IMP-004` | Lock-code finally zeroing still follows fallible restoration; simple timeout restoration still skips failures/readback. Undisposed Default remains in AdvancedTests.ModernFeatures and transport-test ScpRequestedAndCcidHeld. SimpleTests' successful SCP case correctly owns parameters. Keep exact current paths; B6 key alias/cleanup conclusions pending. |
| `management-001:MGMT-IMP-008` | FeatureDeviceInfo (4.1) remains unused by factory/read enforcement; write/reset use effective version and major-zero allowance. Policy remains open; current docs accurately qualify support minimum versus preflight. |
| `management-001:MGMT-IMP-009` | MinFirmware="5.0" still fails three-component parsing and shared filter ignores it; existing comment explicitly warns. Executable correction/strict malformed-filter policy await separate scope; A-C014 is the shared dependency. |
| `management-001:MGMT-IMP-010` | SmartCard/firmware gate remains, with no family enforcement or reset-success invalidation. Manufacturer scope known; configuration/lock effects and post-reset physical lifetime remain open. |
| `security-domain-001:SD-DEP-002` | Management borrows parameters only for initialization; caller-owned contents/aliases retain C-C020/B6. Security Domain's separate retained-reset behavior is outside this packet. |

No authoritative registry state was modified. `fixes-wave-001:CORE-001`'s serial 9681620 failure remains
unexplained; avoiding an unrelated probe or passing managed ownership tests does not explain that old
two-key hardware result. Historical serial 125's two 6A88 INITIALIZE UPDATE responses remain before
EXTERNAL AUTHENTICATE/RESET, not executed resets. Frozen 5,366/8,325/114 artifact populations and four
original user files were left untouched; this packet ran no replacement whole-archive verification.

## 7. Exact test oracles and local candidates

The file ledger retains complete helper/assertion observations. In particular:

- Transport tests assert requested generic type or exact attempt list under synthetic failure. Method
  names containing FallsBack describe absent-interface selection, rather than retry after failure.
- Unit reset asserts a single protocol command with INS 1F; it leaves exact encoded APDU/status behavior
  to Core/B6. Inert HID fake throws on unexpected I/O, so expected NotSupportedException excludes that
  fake exchange path. Backend reflection checks IDisposable shape; source establishes actual ownership.
- Encoded-buffer clearing test observes the actual captured memory after successful fake write. Its
  uninitialized session helper supplies no construction/lifetime proof or failure/cancel clearing oracle.
- X003: unsupported-connection test's final ProbeSession directly invokes a constructor that never
  attaches. Its name overstates stale-guard detection. This observation is contextual outside the selected
  test-method range; it adds zero inventory IDs. Source rejection-before-attachment is independently visible.
- Production SmartCard overlap test holds page 0, verifies refusal/no extra wire, releases and asserts
  exact page 0/page 1 traffic plus reuse. Finally releases the gate; it does not explicitly await the whole
  reader task after every failed assertion. Preserve B5 workflow-drain qualification before stress runs.
- Scoped suite checks serial, firmware, form factor and USB/NFC supported/enabled masks; it omits lock,
  timeout, FIPS, qualifier/component and unknown raw fields. Its saved snapshot list shares memory-backed
  timeout storage, so final equality alone cannot prove deep snapshot independence (A-C003/X002).
- Owned-monitor suite requires two fixtures and checks initial replay, references, cancellation, disposal,
  recreation and no-match selection. It disposes monitors before explicit live reads; this supplies no
  attended removal/reinsert or concurrent sibling-native race proof.
- Readiness tests inject ScanView values and a throwing Connect implementation. They establish the real
  reference readiness loop's metadata-before-authorization behavior, without native discovery or Management I/O.
- Reconfiguration test requires completed scans, distinct DeviceIds and at most one serial-form holder;
  it does not require every transient state or contested branch. Fresh scopes and finally readback are
  present; broad retry catches and between-await deadlines supply no hard call-duration bound.
- Hardware FIPS/form/capability rows reuse decoded fixture metadata. AdvancedPiv additionally asserts
  USB PIV although the capability filter accepts either USB or NFC. Broad wrong-key/missing-lock exception
  assertions can pass on unrelated failure. Existing comments mostly qualify these limitations already.

Local **CORE-MGMT-X001** corrects stale discovery/fixture/equality explanations. **X002** adds current
returned-metadata provenance and A's ownership caveat. **X003** records the bounded test-oracle gap.
Their exact affected original IDs and unchanged paths are in candidates.json; contextual association
does not allege every line in a candidate file is defective. Existing FIPS/reset/cleanup/runtime-output
issues keep old identities. Runtime strings, tests and public API/behavior changes await separate owners.

## 8. Reopening map and explicit pending handoffs

| Dependency / owner | Exact affected surfaces | Reopening condition |
| --- | --- | --- |
| A-C003/C016; A and later Management editor | Core DeviceInfo/reader, IManagementSession.GetDeviceInfo XML, Management README/CLAUDE, DeviceInfoResult, TimeoutsMenu, scoped snapshot/equality commentary | Independent metadata storage/raw representation or cleanup decision; review equality/hash and caller/result lifetime together |
| A-C004/C005/C006; SHARED and Management editors | Core metadata XML; Tests.Shared filter guidance; Management README/CLAUDE/tests; InfoCommand/DeviceInfoMenu/OutputHelpers/DeviceSelector; docs/migration/v1-to-v2.md:329–346 | Mapping/width/normalized-raw representation changes plus independent fixtures; preserve manufacturing/approval distinctions |
| A-C013/C017/C021 / MGMT-XREF-003 | ReconfigurationDiscoveryInvariantTests.TryFindBySerialAsync; scoped AssertMetadataMatches; AdvancedTests cached-data comment; shared fixture instructions, discovery/physical-model guides | Cache validity, metadata origin, serial retention or row binding changes; shared editor owns shared files |
| B4 pinned Windows/support return, then later user-ready Windows delta | Management factory/lifetime/cancellation XML; README/CLAUDE constraints; typed factory/overlap/scoped/monitor/reconfiguration test descriptions; Core sharing/native/discovery guides | Reconcile Windows compatibility adapter disposal, native task completion/claim release, CCID/HID access/grouping/error/ABI and cancellation behavior against returned pin. Future work is explicitly unready until user notice. |
| B6 APDU/SCP return | SmartCardBackend SELECT/write/reset, Management factory SCP/borrowed-input XML, README initialization/security, Scp03 integration tests and shared helper parameter docs | Verify partial handshake cleanup/parameter aliases, auth actor roles, status/error mapping including response status storage, APDU formatting/chaining and response owner lifetime. Current caller route is known; full cross-layer proof remains pending. |
| MGMT-IMP-010; future product/implementation owner | ResetDeviceAsync interface/XML, DeviceReset helper, ResetMenu/DeviceInfoMenu qualifications, README reset, migration reset comparison | Agreed family/reset-success lifetime/configuration-lock contract and qualified device observations; no automatic OATH policy transfer |
| MGMT-IMP-004/009 and X003; future test owner | Exact unit/integration paths in ledger and test guidance | Authorized code/assertion/cleanup changes, followed by full-unit independent review and current execution |

The shared migration's current metadata/configuration qualifications were read and remain useful.
Shared examples and Core guides retain their original primary-reader/future-SHARED ownership; no shared
file is silently accepted wholesale or edited here. The standalone tool's actual selector is SmartCard-
only; unified CLI selection/live-read conclusions are reused from A's completed command-path review,
rather than a second whole Cli.Commands audit. The module/package page requires no speculative API rewrite.

## 9. Verification dimensions and next action

| Dimension | Current result / next owner |
| --- | --- |
| Assigned reading and static preservation | Complete exact IDs/hash/pin comparison; run-owned verification receipt |
| Documentation acceptance / independent review | Local candidates only; coordinator triage, fresh editor and fresh independent content/citation reviewer pending |
| Meaningful examples/API/compiler/analyzers | VALID-NOT-CHECKED here; coordinator compiles complete typed scopes in existing validation foundation |
| Existing managed regression assertions | Read, zero executions here; future selected Management/Core classes need actual nonzero counts and source/output identities |
| New bounded experiments | None run; coordinator may add initial-version/tag05/qualifier/override vectors and post-input-disposal timeout/hash vector with independent expected values |
| Native/platform | B1/B2/B3 completed static findings reused; B4/B6 detailed conclusions pending; no fresh native execution |
| Physical Management reads/configuration/reset | Zero operations here. Old management-001 hardware lane remained empty; inherited integration 4,442 units/197 overlapping reruns and Linux native-only checks are not new physical GetDeviceInfo success. Earlier B2 historical selected-key evidence retains its own source/rig scope. |
| Attended/PIV/Windows incoming | PIV physical deferral preserved; no touch/hotplug/reset request. Windows delta awaits explicit user readiness and exact baseline. |

Later verification should separate initial versus public reads, all three selected transports, borrowed
versus hidden-owned connections, creation/probe/SCP failure, pre-cancel versus admitted exchange, full
multi-page workflow versus exchange drain, same-key rediscovery and metadata/physical-state outcomes.
Keep future compiler scratch and logs under
`/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/BOUNDARY-Management` with propagated temp paths.
No runtime host was created or executed by this packet. Coordinator triage can proceed now for independent
Management observations; B4/B6 return reconciliation remains mandatory before claiming completed Windows
or secure-channel/APDU cross-layer proof.
