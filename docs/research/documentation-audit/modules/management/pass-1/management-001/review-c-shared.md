# Management independent review C-shared

## Verdict and boundaries

**Required corrections; not yet PASS.** Three narrow corrections are listed below. The remaining
reviewed changes pass source/citation review within the stated scope. No optional taste rewrites are
requested.

Reviewer: GPT-6.1 Sol (`github-copilot/gpt-6.1-sol`), 2026-11-03, fresh independent review session.
Reviewed checkout: `/tmp/opencode/management-001-edit-c`; comparison baseline:
`da61d2e6d754b4c155cbb2579202e910986f4b3a`. Owned review scope is **all 17 C-changed paths outside
`src/Management/`**, including `Cli.Commands` (not merely the directory named `Cli.Shared`). C1 owns
the module changes. Read root instructions, applicable Core/Tests.Shared/PIV/YubiOTP instructions,
Tests.Shared and PIV README prerequisites, `docs-module`, house style, PRIMARY plan/triage/investigation-C/
edit-C, and campaign workflow/style. No local instruction files were found under Cli.Commands or
Tests.TestProject. Available skills were considered; no build/test/commit/delegation skill was actioned.

This session changed only this report in PRIMARY. **No source edits, builds, tests, snippet compilation,
hardware operations, commits, or delegation.** Source assertions were read, not executed. Coordinator
compile-only snippet checks, executable-token/directive/API checks and final integrated-hash checks
remain outstanding. A/B's final documentation is not implicitly reviewed by this report; current
runtime definitions in the C checkout supplied the narrow source evidence. Their runtime behavior must
remain unchanged in Pass 1.

## Required corrections

### CSH-R1 — reconcile the remaining generic Management session claim

- **Location:** `CLAUDE.md:168`, Architecture / Application sessions.
- **Status:** required, unresolved accepted `MGMT-DOC-011` dependency (investigation C external manifest).
- **Observed:** the root still describes sessions as generic over connection type and gives
  `ManagementSession<TConnection>` as its current example.
- **Independent evidence:** `src/Management/src/ManagementSession.cs:30` declares
  `public sealed class ManagementSession : ApplicationSession, IManagementSession`; its actual factory
  at `:67-77` takes `IConnection` and returns `Task<ManagementSession>`. The public device extension at
  `IYubiKeyExtensions.cs:101-103` also returns that non-generic type.
- **Correction:** narrowly describe the actual non-generic Management session over supported connection
  types. Do not redesign root architecture or the generic-type policy. Update the edit/reconciliation
  record to include the missed dependency. The coordinator independently steered this candidate during
  review; the source declaration above confirms it rather than treating steering as evidence.

### CSH-R2 — keep caller-owned SCP parameter disposal visible in the complete changed examples

- **Locations:** root `CLAUDE.md:525-536`, integration test example;
  `src/Tests.Shared/README.md:37-46`, second Quick Start test; and the analogous full Filtered Test
  at README `:161-175` (inline default at `:173`). The last is an unchanged adjacent example in this
  changed document, explicitly included by the coordinator's cross-document ownership steering.
- **Status:** required ownership/example correction under `MGMT-DOC-006/011`.
- **Observed:** all three complete examples create `Scp03KeyParameters.Default` inline and never dispose the
  returned caller-owned object. Correct session disposal does not dispose these parameters. This is not
  a compile failure and must not be hidden by compiling a stub or deleting the SCP task.
- **Independent evidence:** `Core/src/Protocols/SmartCard/Scp/Scp03KeyParameters.cs:45-58`: every `Default`
  access constructs a new object; its `Dispose()` disposes `Keys`. The base
  `ScpKeyParameters.cs:20-28` implements `IDisposable`. Management passes parameters to initialization
  (`ManagementSession.cs:67-107`); it does not own them. `ApplicationSession.cs:509-565` disposes protocol
  state and only an owned connection, not the supplied key-parameter object. The actual named integration
  test, `ManagementSessionSimpleTests.cs:193-209`, uses `using var scpKeyParams = Scp03KeyParameters.Default`
  before creating its connection/session. `StaticKeys.cs:52-61,83-99` confirms separate owned ENC/MAC/DEK
  arrays and deterministic zeroing by disposal. The filtered example's real project-specific
  `SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs:31-66`
  forwards the supplied parameter through options and disposes its sessions/connection, **not** that
  parameter. Core guidance also explicitly calls for SCP key disposal. This narrow dependency read is
  not a SecurityDomain module audit. The module reviewer's analogous finding is supporting reconciliation;
  the independently read construction/disposal paths determine this judgement.
- **Correction:** introduce `using var scpKeyParams = Scp03KeyParameters.Default` before the owned session
  or helper call, and pass that local through the current options/named helper argument. Preserve the
  awaited real metadata query, assertion, cancellation handling and session scope. Keep the existing
  dedicated SmartCard/default-SCP-key execution prerequisite. These particular default bytes are public:
  this finding is about truthful ownership and deterministic cleanup, **not an allegation of exposed
  non-public key material**. No production lifecycle change is requested.

### CSH-R3 — the touched SCP limitation example still asserts an unsupported impossibility

- **Location:** root `CLAUDE.md:550-563`, When You Can't Write Meaningful Tests / Example (complete
  XML/comment teaching block, not only the renamed integration-test token).
- **Status:** required narrow factual correction; no broader testing-policy rewrite requested.
- **Observed:** after updating the integration-test reference, the block still says actual SCP
  initialization cannot be unit tested because `ScpState.Scp03InitAsync` is static, and the body says to
  test only routing. Static dispatch alone is not the current source's testing boundary.
- **Independent evidence:** `Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreScpAuditReproTests.cs:123-138`
  constructs a real `FakeApduProcessor`, queues an INITIALIZE UPDATE response, calls that exact static
  `ScpState.Scp03InitAsync`, and asserts `BadResponseException` for response lengths 0 and 28. The actual
  `ScpInitializer.cs:113-125` calls the same method through an APDU-processor argument. Thus initialization
  behavior already has a fakeable wire seam. The test read establishes malformed-response coverage, not
  a successful full handshake or a newly passing test run.
- **Correction:** preserve the surrounding policy, but make this small example distinguish fakeable
  initialization/protocol behavior from real-device interoperability that requires integration testing;
  alternatively use an explicitly illustrative limitation that does not make this false claim about
  current Core. Retain the verified current Management integration-test reference if relevant. Do not
  claim comprehensive unit coverage from the one test or open a Core refactor.

## Independent evidence and consequential citation checks

Source paths below are relative to the reviewed checkout. Runtime definitions used here are unchanged
in the C diff. Each conclusion is bounded to the actual statement being reviewed.

| Evidence | Independently read source / assertions | Result and limit |
| --- | --- | --- |
| E1: one row / first match | `Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs:168-185`; `YubiKeyTestState.cs:152-195,315-346` | One `yield return` per attribute, criteria carried in a placeholder; execution uses `candidates.FirstOrDefault()`, then caches the chosen metadata. Multiple attributes are independent, not all-device enumeration. No execution-order stability or physical-key deduplication is promised. |
| E2: filter semantics | `YubiKeyTestInfrastructure.cs:72-125`; `YubiKeyTestState.cs:241-251,412-427`; `Core/Devices/FirmwareVersion.cs:145-158` | Invalid/non-three-component firmware parses as null and is ignored by filtering. USB availability and enabled capability are independent predicates. Enabled capability accepts USB **or** NFC; exact form-factor equality is used. This is an observed limitation, not endorsement of permissive malformed criteria. |
| E3: cached helper and actual API | `YubiKeyTestStateExtensions.cs:72-93,123-142,166-175`; `Core/Sessions/ApplicationSession.cs:73-74`; `Management/src/IYubiKeyExtensions.cs:46-53,101-144` | Async helper order is action/configuration/scpKeyParams/preferredConnection/cancellationToken; callback is passed `state.DeviceInfo`. The actual opened interface is inherited `ConnectionType => Connection.Type`. A device filter is not a session pin. One-shot metadata uses an owned, disposed session. |
| E4: choose once, no fallback | `Core/Devices/YubiKeyConnectionExtensions.cs:180-220,246-303`; Management extension `:105-133` | Exactly one concrete type is selected/opened; group/composite preferences rejected. SCP without a preference selects SmartCard. Construction errors dispose the newly opened connection and propagate, not retry another route. Default-order examples here assume no SCP policy unless they explicitly supply it. |
| E5: process/group scope | `Core/Devices/DeviceConnectionRegistry.cs:52-54,89-120,161-213`; `YubiKeyDevice.cs:44-71,128-179` | Static process-local registry over the known interface-ID scope; sorted/deduplicated claims and rollback; published-device connect claims every known slot before native open. This cannot exclude other processes or arbitrary independently opened raw-factory handles; conservative single-slot records do not magically acquire unknown siblings. |
| E6: borrowed sessions/disposal | `Core/Sessions/ConnectionSessionGuard.cs:44-65,91-100`; `ApplicationSession.cs:227-269,305,441-565` | One live attached session per connection; direct factory borrows, convenience session owns only after explicit transfer. Sequential bounded examples are valid. No session finalizer is defined. Failure/drain/reopen qualifications remain in architecture prose; disposal is not proof of reusable state after a partial exchange. |
| E7: actual connection finalizer | `Core/Transports/SmartCard/UsbSmartCardConnection.cs:56-65,155-180`; `Core/Devices/RegisteredConnections.cs:28-107`; macOS typed HID connection finalizers | PC/SC finalizer requests shutdown and suppresses cleanup errors; explicit disposal awaits/reports it. Registered wrappers have no compensating lease finalizer. “Some built-in connections” plus “best effort, not timely teardown/lease-release guarantee” is accurate. No all-platform finalizer recovery claim is justified. |
| E8: current metadata defects | `Core/Devices/DeviceInfo.cs:85-96,174-182,269-302`; `FormFactor.cs`; shared filters in E2 | FIPS tags currently route through `FromApp`, despite the distinct local FIPS bit table; raw flagged form-factor byte is cast unmasked. E.g. 0x81 differs from the base USB-A value. Narrow consumer warnings correctly identify defects rather than intended decoding, and do not infer certification. |
| E9: migration write/read/reset | `ManagementSession.cs:32-39,99-119,122-173`; `DeviceConfig.cs:63-115,118-150`; `Core/Sessions/ApplicationSession.cs:422-428` | Read's declared 4.1 feature is not checked; write/reset use effective firmware. Null omissions, full masks/flags, exact USB None rejection, per-command lock fields, zero replacement and encoded false restriction are accurately separated from field-specific validation/recovery unknowns. |
| E10: CLI snapshot/results | Complete `YkCommandBase`, `YkDeviceContext`, `ManagementConfigCommand`, `ManagementResetCommand`; `Cli.Shared/src/Device/DeviceSelectorBase.cs` metadata selection flow; `Cli.Shared/src/Output/ConfirmationPrompts.cs:41-56` | Enrichment is awaited before command execution; one-shot extension has already disposed its session. Config displays cached auto-eject (not both timeout fields). Reset checks snapshot firmware, advertises all three interfaces, then SDK rejects HID reset; confirmation uses boolean plus typed word unless forced. Catch-all metadata cancellation remains deferred. |
| E11: sibling/current host examples | Complete `PivTool/SDK_PAIN_POINTS.md` changed disposal task; `PivSession.cs` factory; complete `DiTestController.Get`; complete `OtpTool/DeviceHelper` | PIV's actual factory is async over a SmartCard connection. Controller forwards token, explicitly handles absence, propagates query errors and keeps legacy string unchanged. OTP helper filters physical-key discovery results, queries metadata before creating OTP, and uses SmartCard-first SDK selection. No serial-dedup algorithm exists there. |

### Original references re-fetched, not inherited citation assurances

Access date: **2026-11-03**. These are live vendor pages, not fixed numbered standards. This review read
the actual relevant passages. Product applicability is separate from the SDK's current enforcement.

| ID / original URL | Title, status and focused passage | Reviewed conclusion |
| --- | --- | --- |
| P1 — <https://docs.yubico.com/yesdk/yubikey-api/Yubico.YubiKey.Management.Commands.DeviceResetCommand.html> | Live **v1 API reference**, Remarks: “only available on YubiKey Bio Multi-protocol Edition keys”; deletes FIDO credentials/fingerprints/shared PIN, PIV keys/certificates “except the F9 attestation slot,” user PIV data; restores PUK/management-key defaults. | Supports narrow shared-reset XML and migration family/F9 statements. Not authority for v2 signatures, general Management lock/configuration erasure, or full factory-byte erasure. |
| P2 — <https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-bio-specifics.html#resetting> | Live **YubiKey Technical Manual**, Resetting. Original HTML anchor re-read directly after the full-page extraction cut off before it. If both PIV/FIDO are enabled, MPE individual native resets are replaced by one combined reset; trying the MPE command on non-MPE Bio produces an error. Reset deletes credentials, PIN and fingerprint templates. | Confirms product-family distinction and shared state. Firmware alone/form-factor display alone is not eligibility. No Management lost-code recovery promise follows. No transfer of an individual FIDO reset's timing or ResetBlocked rules. |
| P3 — <https://docs.yubico.com/software/yubikey/tools/ykman/Config_Commands.html#ykman-config-reset-options> | Live **ykman CLI Guide**, config reset: “only used with the YubiKey Bio Multi-protocol Edition.” NFC restrict: re-enable by USB power, firmware 5.7+. | Corroborates product scope and NFC task. Tool descriptions do not establish C# SDK preflight or HID reset support. |
| P4 — <https://developers.yubico.com/yubikey-manager/Config_Reference.html> | Live **YubiKey 5 Series Configuration Reference Guide**, Enabled Capabilities / Set and Unlock Configuration Lock: complete packed replacement bits; unlock must be in same command; sixteen zero bytes disables lock. Form-factor table separates 0x01/0x41/0x81. | Supports active migration mask/lock statements and raw-form-factor caveat. Its conflicting NFC write-tag 0x0c/reboot wording was observed; not used to change or certify encoding. |
| P5 — <https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-firmware-5.7.html#restricted-nfc> | Live **Technical Manual**, Restricted NFC: NFC keys with 5.7+; USB charger or computer port “for 3 seconds” is sufficient to disable restriction. | Supports migration's concrete recovery route, not a promise that a false TLV restores NFC. No host communication, touch or PIN is required by that passage. |
| P6 — <https://developers.yubico.com/yubikey-manager/API_Documentation/_modules/yubikit/management.html> | Rendered **yubikey-manager 5.9.0** source as fetched now: `_from_fips` maps bit 0 to FIDO2, bit 1 to PIV, etc.; form-factor parser masks 0x0f; reset uses SmartCard backend. | Sibling corroboration for the local decoder discrepancy and reset route only; not a normative protocol specification or certification evidence. The version is visible in this fresh fetch; no assumption about the earlier report's fetch is needed. |
| P7 — Context7 `/dotnet/docs`: <https://github.com/dotnet/docs/blob/main/docs/csharp/language-reference/statements/using.md> and <https://github.com/dotnet/docs/blob/main/docs/standard/garbage-collection/implementing-disposeasync.md> | Current main-branch framework docs: a using declaration disposes at enclosing-scope end; explicit await-using blocks await cleanup at each block end; reverse declaration order for nested lifetimes. | Confirms whole-block lifetime reasoning and deterministic-disposal recommendation. Framework documentation, not device interoperability evidence. |

## Complete changed-unit and snippet review

The unit descriptions below cover **whole affected sections/examples**, including relevant unchanged
statements in those units and implicit session/connection scopes. They do not certify every unrelated
pre-existing claim in the larger shared documents.

| Owned path | Complete units and result |
| --- | --- |
| `CLAUDE.md` | Read the entire root guide. Reviewed complete class-choice and size-guideline fenced alternatives: names now explicitly illustrative, not Core's `readonly record struct DeviceInfo`. Kept their teaching/sketch classification, not a standalone nullable-clean compile promise. Complete SCP integration test performs discovery/create/read/assert/async disposal with xUnit-v3 tokens and current signatures; **CSH-R2** parameter ownership remains. Full adjacent limitation/XML teaching block has the right existing integration-test name but **CSH-R3** factual testing-boundary error. Architecture dependency **CSH-R1** remains. No policy redesign requested. |
| `docs/TESTING.md` | Entire Multi-Transport Test Infrastructure section, all five fences, all three first-fence methods, default helper callback, concrete three-row pin/assert/read method, old negative DeviceId example and new FIDO pin task. **PASS** on E1–E4. Old block is deliberately historical/fragile. New methods do real metadata I/O/assertions, not no-op stubs. Requested `state.ConnectionType` versus full availability is accurate. These are xUnit-v2 Tests.Shared project members, not v3 token recipes. |
| `docs/architecture/connection-ownership-and-contention.md` | Entire opening contract, grouping bound, ownership/disposal, transport section and associated whole example scopes read, with full document for context. **PASS** on E3–E7. Deliberate pre-enforcement PIV/Management overlap remains historical bad evidence, not a supported current recipe. Sequential borrowed PIV/OATH scopes remain distinct. Current `ConnectionType` replaces retired session `Transport`. Wider protocol/platform statements are unchanged and not newly qualified by this review. |
| `docs/architecture/physical-device-model.md` | Whole applet selection table/options semantics and full default/OTP Management fence; both reads are now bounded and sequential. Read metadata and earlier typed-connect alternatives in context; they do not supply a still-live implicit lease. Entire ownership/finalizer/grouping subsection reviewed. **PASS** on E3–E7. Actual OTP-HID exposure is an execution prerequisite for the explicit second alternative. Current contract is process/known-group bounded, not global exclusion. |
| `docs/migration/v1-to-v2.md` | Full Device Info section: V1 historical fence, current V2 discovery/session/read/display fence, all migration notes and new four-bullet qualifications, plus following applet-creation context. **PASS** on E8–E9/P1–P6. Current V2 fragment needs a supplied token and ordinary method scope ending the session before a subsequent applet task. V1 and historical gaps remain historical; no instruction to concatenate alternatives into one overlapping scope. No source-encoding/certification promise added. |
| `src/Cli.Commands/src/Infrastructure/YkCommandBase.cs` | Complete command lifecycle, selection/enrichment catch, context handoff and finally read. **PASS** on E3/E10. Comments accurately name visible informational output and swallowed cancellation; no new token-forwarding claim. No source strings changed in this diff. |
| `src/Cli.Commands/src/Infrastructure/YkDeviceContext.cs` | Entire class/context XML, Info/selection/preference/options/banner lifetime read. **PASS** on E8/E10. Snapshot not retained session; absent SmartCard not itself failure; family/certification not inferred from known flawed metadata. XML indentation is not a factual finding. |
| `src/Cli.Commands/src/Management/ManagementConfigCommand.cs` | Entire command/output body versus summary. **PASS** on E10. Correctly calls it context snapshot and singular auto-eject; body also displays NFC-restricted/reset-blocked details without introducing a fresh query. |
| `src/Cli.Commands/src/Management/ManagementResetCommand.cs` | Entire settings/transport/preflight/confirmation/create/reset/result unit. **PASS** on E9–E10/P1–P3. MPE, retained F9, firmware-only check, SmartCard-only operation, advertised HID rejection and absent lock-recovery proof all accurate. Misleading erasure/success executable strings are intentionally unchanged and remain IMP-010/017 dependencies, not fixed by XML. |
| `src/Core/CLAUDE.md` | Read entire guide; reviewed full Session Base Class ownership paragraphs and full Known Gotchas ownership item, not only finalizer replacement tokens. **PASS** on E5–E7. Deterministic owner disposal, convenience ownership transfer and sequential reuse remain explicit. No claim that all connections recover through finalization; no broad Core instruction rewrite. |
| `src/Piv/examples/PivTool/SDK_PAIN_POINTS.md` | Read full development-notes document; full Issue/Resolution/code unit for async disposal. **PASS** on E6/E11/P7. Exact `await ManagementSession.CreateAsync(connection)`; real metadata read/output; bounded Management scope ends before actual `await PivSession.CreateAsync(connection)`. Connection is explicitly caller-owned and must outlive/dispose after both. Original Issue is historical context, not a new current API claim. Other PIV tasks remain outside this correction. |
| `src/Tests.Shared/CLAUDE.md` | Read full guide, discovery/execution binding unit, whole Known Limitations list and helper/allow-list sketches for context. **PASS** on E1–E4/E8. Correct first-match independent rows, cached callback, actual helper ordering and pin property; malformed criteria/capability/FIPS/raw-form-factor bounds are narrow defect qualifications. Unchanged schematic pseudo-implementations are not standalone compilable new SDK definitions. |
| `src/Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs` | Entire type and all XML, both complete code elements: distinct test methods with real reads/assertions; custom filter declaration kept in its own containing-class context. All changed property summaries/GetData binding explanation checked against E1/E2/E8. **PASS**. Exact three-component criterion fixed only in documentation; current source filter behavior not altered. Serial-range “ProductionKeysOnly” example does not bypass the separate dedicated-key allow list. |
| `src/Tests.Shared/README.md` | Read full README; complete Quick Start class/second SCP test, new filter caveats, helper explanation plus full OTP pin/read/assert fence and following actual OATH borrowing helper, final first-match limitation. **PASS except CSH-R2**. SCP prerequisites are explicitly stated; firmware alone not support. Methods remain meaningful; no task deleted to satisfy compilation. Unchanged JSON serials are illustrative authorization configuration, not permission to add a personal key. Related ownership steering also brings the entire unchanged Filtered Test and actual SecurityDomain test-project helper into R2's narrow disposal correction; this does not establish its reset/hardware behavior or advertise it as a Tests.Shared-shipped helper. Unchanged recorder remains a sketch, not a newly compiled Management example. |
| `src/Tests.Shared/YubiKeyTestStateExtensions.cs` | Entire implementation and three full XML code elements reviewed: current theory/state read/serial test; intentionally synchronous cached-property callback; helper-owned SmartCard connection with one borrowed Management session/read/assert. **PASS** on E1/E3/E6. Borrowed session is disposed inside callback before connection-owner helper exits. Both overload parameter sets match actual arguments and callback freshness is accurately qualified. |
| `src/Tests.TestProject/Controllers/DiTestController.cs` | Entire controller/Get/no-device/query/response unit. **PASS** on E3/E11. Caller token forwarded; temporary one-shot session already disposed before building response. No implied exception handling beyond no-device. Legacy response string remains unchanged/deferred; no ASP.NET-wide error/cancellation guarantee inferred. |
| `src/YubiOtp/examples/OtpTool/DeviceHelper.cs` | Entire selector/helper including default session creation, retry-filter, metadata catch, parallel per-discovery-row queries and form-factor display. **PASS** on E3/E8/E11. New remarks correct nonexistent serial-dedup claim; no merging/runtime change. Best-effort metadata can use either HID route when SmartCard absent, actual OTP session uses its separate SmartCard/OTP order. Raw flagged display is not family proof. Existing generic cancellation catch remains deferred. |

### Compilation handoff: preserve useful tasks and real lifetimes

- Root SCP test: compile as an xUnit **v3** member with Core/Management references, `Xunit`, Core Devices/
  Sessions/Scp and Management imports; retain real discovery/read/assert, add parameter disposal per R2.
  Execution requires an authorized dedicated SmartCard key configured with the chosen SCP03 keys.
- Tests.Shared examples/XML: actual xUnit **v2** + SkippableFact + Tests.Shared/Core/Management references,
  relevant Devices/Management imports. Wrap members and custom-filter declarations in real containing
  test classes; duplicate example filter names belong to separate extraction scopes. Do not replace
  real reads with completed tasks or remove assertions. The synchronous callback is intentionally a
  cached-metadata task, not a promise of fresh I/O.
- The adjacent README Filtered Test additionally requires the actual SecurityDomain project-specific
  test-helper namespace/reference and dedicated default-SCP03/reset prerequisites. Add its own using-scoped
  parameter local too; retain its real reset-before-use helper call and key-information query/assertion,
  rather than replacing the task with a stub or silently dropping SCP. No reset is authorized here.
- Physical-device-model fragment: typed `IYubiKey device`, ordinary async method, actual `SessionCreationOptions`
  and Management imports. Keep both bounded await-using bodies and both reads intact.
- PIV resolution: caller-supplied idle `ISmartCardConnection connection`, actual Management and PIV
  references/imports; do not manufacture a still-live session as its prerequisite. Keep Management's
  block before PIV's declaration. Host owns the connection.
- Migration V2 fragment: typed `CancellationToken cancellationToken`; keep discovery/create/read/display
  and method-end disposal. V1/historical negative examples, root illustrative models and unchanged
  internal skeletons are not current consumer compile targets.

No no-op stub, omitted useful operation, or invented SDK member is accepted as compilation scaffolding.
Compilation can establish syntax/signatures, not hardware support, credentials, reset safety or successful
restoration. Coordinator should compile corrected final blocks, not these pre-correction hashes.

## Reviewed file hashes

SHA-256 of **every changed owned path** at this review checkpoint. Source changes after these hashes
require targeted re-review; a final integrated hash is not implied by this report.

| Path | SHA-256 |
| --- | --- |
| `CLAUDE.md` | `c48191a8f7b8db60400684f9275855c2fbd0456a45fa9b1b5bd7cfcea6e23d7c` |
| `docs/TESTING.md` | `ccaacaaee2a4c7df77f2ecad10753e4d3bb01dd1cb71dcf2a28e0a4d9a557668` |
| `docs/architecture/connection-ownership-and-contention.md` | `7dde7422dfeffe2d1464d8a52513340f1f5f6ea7681b581ebca0214a7463cebb` |
| `docs/architecture/physical-device-model.md` | `1eb4efa5450d69829d0f7165f5d02b413e5e539e8275611b5b64983af5e43cd9` |
| `docs/migration/v1-to-v2.md` | `d624e3ddbc94d6e629b8585e13b6c6dcc1c2e3374d4243f4012eb15272a22c2a` |
| `src/Cli.Commands/src/Infrastructure/YkCommandBase.cs` | `d3b2ffd3171d65ddb9708e68d7a9f33011180600f39d9dc70a36419c30b73f71` |
| `src/Cli.Commands/src/Infrastructure/YkDeviceContext.cs` | `25b0e1885b1caefe8e6089e9dfa3204f97a1bda038d6a8b57f2b2f177c9e173c` |
| `src/Cli.Commands/src/Management/ManagementConfigCommand.cs` | `d65ea5db448fb89dae24343810c746c3fb6ef4cffba08ec7ef0095ba9a11a8ba` |
| `src/Cli.Commands/src/Management/ManagementResetCommand.cs` | `56fc900e8096a6579335993d35c790110f9eb868e85145265d6b2405aac96242` |
| `src/Core/CLAUDE.md` | `c1093643f6b7cadd5bfa58cf6c6ab65a5d1851a5a8360ee4916264888da35a97` |
| `src/Piv/examples/PivTool/SDK_PAIN_POINTS.md` | `0eef2e65430d38b9b0e6d9c05aca7eb409d6033c25a4e38d4ced13f260c601ab` |
| `src/Tests.Shared/CLAUDE.md` | `5d9303462a236dcfbc4cf3a71afca4e3d84d3aeecc9ca81b703e284de39e3f35` |
| `src/Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs` | `2e626d332c33f7fd139c08351b8fc7d41cc53591968080571e7e0e9913546809` |
| `src/Tests.Shared/README.md` | `d114dce80a9c8194e4f01765454cbf59aaf9559590302e5b0c18447addf1c9df` |
| `src/Tests.Shared/YubiKeyTestStateExtensions.cs` | `7988924ef82bad118595c65782af67e87672950b2e0fe000c784a4b4ab3b31f2` |
| `src/Tests.TestProject/Controllers/DiTestController.cs` | `0a7f864d8185ad178e3c7433e12801f21093668488485084336f68ce323a288f` |
| `src/YubiOtp/examples/OtpTool/DeviceHelper.cs` | `667f143a14cdc8635bffa9c7cc402ea2b6e6b25f3170e16b52859fee909ba0dd` |

## Preserved scope and final handoff

Historical `docs/migration/v1-to-v2-gaps.md`, original plans and immutable campaign records are not
changed by this diff and were not treated as current API recipes. Root model renaming and narrow
Core/shared corrections are consequential ambiguity fixes, not broad instruction-policy churn.
Comment indentation/final newlines do not warrant optional rewrites. No other consequential unintended
scope expansion was found in the owned diff.

Preserve `MGMT-IMP-001..017` / `MGMT-XREF-001..003`: truthful comments do not fix reset eligibility,
unknown lock erasure/lifecycle, NFC-false recovery, flawed FIPS/form-factor decoding, transport coverage,
criteria handling, sample cancellation, secrets input or misleading executable strings. No certification,
all-platform exclusion/finalizer guarantee, hardware-reset result or newly passing test is claimed.

Next: apply the three narrow required corrections, re-review their full blocks and hashes, compile
meaningful final snippets with coordinator-authorized tooling, then reconcile C1/A/B and executable-token/
API preservation on the integrated tree. This report is **review complete / corrections required**, not
documentation closeout.
