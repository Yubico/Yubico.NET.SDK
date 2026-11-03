# Management edit C — guides, examples and narrow shared references

## Preliminary-compilation correction checkpoint

2026-11-03: coordinator's preliminary compilation exposed nonexistent
`DeviceCapabilities.SecurityDomain` in shared README fence 5. Verified the enum, supported filter
implementation, SecurityDomain 5.3.0/SCP03 feature minimum and actual helper. Corrected only
`src/Tests.Shared/README.md`: concrete SmartCard/5.3.0 row, accurate `Scp03_AfterReset_ReturnsKeyInfo`
name, explicit dedicated-key/reset/default-SCP prerequisites, and a real fresh read/comparison in
Custom Filter fence 6 instead of an async callback without an await.
[correction-c-compilation.md](correction-c-compilation.md) preserves the failure/source evidence and
current hash. This editor did not compile or execute anything; final compilation and C2 follow-up are
pending. Separate CS1701 assembly-reference context remains tooling-agent/coordinator work.

## Shared-review correction checkpoint

2026-11-03: verified and corrected CSH-R1..R3 from
[review-c-shared.md](review-c-shared.md), only in root `CLAUDE.md` and `src/Tests.Shared/README.md`.
[correction-c-shared.md](correction-c-shared.md) preserves the original findings/evidence and records
fresh verification and corrected hashes. These changes remain pending focused independent re-review.
The root now describes non-generic ManagementSession and the real fakeable SCP initialization seam;
all three selected shared SCP examples retain their actual tasks with caller-owned parameter disposal.
The adjacent Filtered Test is now an edited compile target, with exact real helper/project prerequisites
below; it is no longer excluded as an unchanged dependency example.

## Module-review correction checkpoint

2026-11-03: verified and corrected C-MOD-R1..R5 from the independent
[module review](review-c-module.md), in the four named module Markdown files only. Focused re-review is
pending; no outside-module file was changed during this correction round while C2 review is active.
[correction-c-module.md](correction-c-module.md) preserves the original findings/evidence, fresh source
verification, exact edits and new hashes. The mapping and final snippet manifest below now describe the
corrected module text.

Erratum for the coordinator: investigation C-10 and triage IMP-014 overstate first-token dispatch by
omitting top-level `-h`/`--help`/`help`, which do print usage. Extra command arguments are ignored, so
`info --help` still runs the information command. Original investigation/triage records are not silently
rewritten here; the correction report requests a leading erratum in those records.

## Status and scope

Editor: GPT-6.1 Sol (`github-copilot/gpt-6.1-sol`), 2026-11-03. Edits are in
`/tmp/opencode/management-001-edit-c`, based on `da61d2e6d754b4c155cbb2579202e910986f4b3a`.
This report is deliberately written in PRIMARY. Coordinator integration, independent review and snippet
compilation remain pending. No build, test, hardware operation, commit, delegation or compile-source
creation was performed. `git diff --check` completed with no output in the editor checkout.

Read root/Management/test instructions, Core, Tests.Shared (including its README), PIV and YubiOTP
instructions before their scoped edits, plus the PRIMARY plan, triage, A/B/C investigations and approved
workflow/style. No local CLAUDE/AGENTS files exist under Cli.Commands, Cli.Shared or Tests.TestProject.
Loaded `docs-module` and `domain-secure-credential-prompt`. Build/test/commit skills were not actioned:
those operations are prohibited in this assignment. Used Context7 `/dotnet/docs` to reconfirm `dotnet run`
argument forwarding through `--`; the retained investigation citations remain the device-claim evidence.

All 34 changed checkout files are listed below. No A-owned source/session/test C#, B-owned models,
API snapshots, executable strings, project references or runtime configuration were intentionally changed.
C# edits are comments/XML only (some files also gain a final newline). Executable-token/directive
preservation is for coordinator verification, not claimed from a build. The contributor guide remains
roughly its original length, retaining transport/lifetime trade-offs, filtering, test safety, models,
firmware, initialization, backend/TLV rationale and debugging rather than replacing it with a short audit note.

## Exact change mapping

Paths are repository-relative. `C-*` provenance refers to investigation C; canonical IDs are the triage's
accepted documentation groups, not claims that deferred runtime work is implemented.

| Changed file | Candidates / accepted groups | Final change |
| --- | --- | --- |
| `CLAUDE.md` | C-03/15/18; DOC-006/011; CSH-R1/R2/R3 | Correct non-generic ManagementSession architecture dependency, actual Management/SCP integration task/reference, caller-owned default-parameter disposal and fakeable initialization versus hardware-interoperability boundary; rename illustrative large application models so they are not presented as Core DeviceInfo. No broad policy rewrite or comprehensive Core testing claim. |
| `src/Core/CLAUDE.md` | C-15; DOC-011 | Replace the two Management-relevant connection-finalizer absolutes with deterministic-disposal/best-effort bounds. |
| `src/Management/README.md` | C-01/03/09/13/14/15/19; DOC-001..007/010..012; C-MOD-R2/R3 | Complete replacement masks, supported-device responsibility, null omission, timeout zero/ranges, NFC restriction/USB-power recovery, lock removal/borrowed lifetime, Bio MPE reset/F9/no lock recovery, firmware support versus preflight, initialization/SCP/override and unused prompt, sequencing, ownership and current tool/metadata limits. Bound the starter session before later alternatives. R2 distinguishes configuration credentials from optional caller-owned SCP initialization keys; R3 conditions reconnection duties on actual reboot without automatic managed-session disposal claims. |
| `src/Management/CLAUDE.md` | C-02..08/14/15/18/19; DOC-001..011; C-MOD-R1 | Current row/cache/helper contracts; meaningful independent snippets; no-reboot save/write/read/restore/read example; reboot reconnection procedure; transport pins, metadata limits, actual qualifier/gating/initialization and reader/backend flow; retain useful rationale and explicit source-excerpt classification. Includes late prompt steering: unused, not retained/invoked/disposed. R1 adds an owned using scope for fresh default SCP parameters, enclosing the real session and both reads. |
| `src/Management/tests/CLAUDE.md` | C-02/04/05/19; DOC-008/010/011; C-MOD-R5 | Fix root-testing link, scoped runner commands/real class, row/cache/filter/pin contracts and restoration/secret-cleanup guidance; remove contradictory configuration-test prohibition. The --integration command is accurately labeled unit plus integration, not integration-only. |
| `src/Management/examples/ManagementTool/README.md` | C-01/09..14/18/19; DOC-001..004/007/010/012; C-MOD-R4 | Actual menu/command tasks, root-relative invocations, active SmartCard selector, unreachable NFC helper, help/secret-input/exceptional-cleanup/cancellation/result/display limits, replacement masks, timeout/flags, Bio MPE/F9/no recovery and current project tree. R4 documents real top-level help separately from ignored command-specific extra arguments and notes monitoring starts before dispatch. |
| `src/Management/examples/ManagementTool/Program.cs` | C-18; DOC-012 | Present-tense no-argument menu comment; dispatch/menu/help strings untouched. |
| `src/Management/examples/ManagementTool/Cli/Menus/CapabilitiesMenu.cs` | C-09/14; DOC-001/012 | Replacement-set and SmartCard-tool reachability explanation. |
| `src/Management/examples/ManagementTool/Cli/Menus/DeviceFlagsMenu.cs` | C-13; DOC-004/012 | Explain omitted CCID-only TouchEject condition without changing display strings. |
| `src/Management/examples/ManagementTool/Cli/Menus/DeviceInfoMenu.cs` | C-01/13/19; DOC-007/010/012 | Manufacturing indicator, wrong FIPS mapping and firmware-only reset display comments. |
| `src/Management/examples/ManagementTool/Cli/Menus/LockCodeMenu.cs` | C-01/11; DOC-002/007/012 | Remove unsupported reset-recovery implication from the explanatory comment; executable warning remains deferred. |
| `src/Management/examples/ManagementTool/Cli/Menus/ResetMenu.cs` | C-01; DOC-007/012 | Bio MPE/F9/firmware-only eligibility and display-erasure bounds. |
| `src/Management/examples/ManagementTool/Cli/Menus/TimeoutsMenu.cs` | C-13/14; DOC-004/012 | SDK ceilings versus firmware maxima and actual CCID timeout conditions. |
| `src/Management/examples/ManagementTool/Cli/Prompts/DeviceSelector.cs` | C-09/12/19; DOC-010/012 | Actual SmartCard filter and grouped physical-key explanation; correct selection summaries and flagged-form-factor formatting limit. |
| `src/Management/examples/ManagementTool/Cli/Prompts/LockCodePrompt.cs` | C-11; DOC-002/012 | Masking versus immutable-string erasure and exceptional cleanup; caller finally snippet now performs an actual authorized configuration write. |
| `src/Management/examples/ManagementTool/Features/DeviceConfiguration.cs` | C-11..14; DOC-001..004/012 | Borrowed secrets, full masks/flags, timeout zero/conditions/SDK ceilings, per-command code removal, encoded NFC request and documented recovery, wrapper catch-all/cancellation bounds. |
| `src/Management/examples/ManagementTool/Features/DeviceInfoQuery.cs` | C-03/12/19; DOC-010..012 | Correct named factory token in full XML example; metadata interpretation and failure-result/cancellation bounds. |
| `src/Management/examples/ManagementTool/Features/DeviceReset.cs` | C-01/12; DOC-007/012 | Bio MPE/F9 destruction, no lock-recovery promise, firmware-only predicate and actual wrapper/SmartCard prerequisites; retain complete query/reset/result XML task. |
| `src/Management/examples/ManagementTool/Features/Results/ConfigResult.cs` | C-13; DOC-012 | RebootRequired is supplied request flag, not observed reboot requirement/completion. |
| `src/Tests.Shared/README.md` | C-04/07/19; DOC-006/008/010/011; CSH-R2; preliminary-compilation fence 5/6 corrections | Name scpKeyParams, fix async examples to do real reads, describe SCP prerequisites, first-match rows/cache, criterion/pin/metadata limits and meaningful OTP-HID pin/read/assert example. Both the Quick Start SCP test and adjacent SecurityDomain Filtered Test own/dispose fresh default parameters around actual awaited helper tasks. Filtered Test uses real SmartCard/5.3.0 criteria rather than a nonexistent capability, has the accurate SCP03/key-info name, and identifies its actual project helper/import and reset/default-key prerequisites. Custom Filter reads fresh metadata and compares its serial with the cached snapshot. |
| `src/Tests.Shared/CLAUDE.md` | C-04/19; DOC-008/010/011 | First-match binding, cached callback, actual helper ordering, concrete pin/property, malformed-firmware/capability/FIPS/form-factor bounds. |
| `src/Tests.Shared/YubiKeyTestStateExtensions.cs` | C-03/04/07; DOC-008/011 | Current theory/state types, cached callback, correct synchronous pin parameter XML, precise pin rationale and actual borrowed-connection Management task instead of retired APDU API. |
| `src/Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs` | C-04/19, A-10; DOC-008/010/011 | Three-component XML criterion, first-match independent rows, meaningful distinct test methods, pin versus device filter and exact metadata limitations. |
| `docs/TESTING.md` | C-04/07; DOC-008/011 | Actual ConnectionType property and selection/cache semantics; replace empty Management row sketches with real metadata queries while preserving the explicit distinction from pinning. |
| `docs/architecture/physical-device-model.md` | C-08/15; DOC-006/011 | Separate bounded default/OTP Management lifetimes with actual reads; process/group/finalizer bounds. |
| `docs/architecture/connection-ownership-and-contention.md` | C-07/15; DOC-006/011 | Group/process contract, deterministic disposal versus best-effort native finalization and inherited ConnectionType instead of retired Transport. |
| `docs/migration/v1-to-v2.md` | C-03/14/19; DOC-001..004/006/007/010/011 | Scoped current Management qualifications alongside the existing current Device Info recipe; explicitly distinguish the historical gaps baseline. |
| `src/Piv/examples/PivTool/SDK_PAIN_POINTS.md` | C-17; DOC-006/011 | Correct asynchronous Management creation, actual query and bounded disposal before borrowing the same connection for PIV. |
| `src/Cli.Commands/src/Infrastructure/YkDeviceContext.cs` | C-16/19; DOC-010/011 | Best-effort closed one-shot snapshot, not always-first open session or SmartCard-only failure; metadata limits. |
| `src/Cli.Commands/src/Infrastructure/YkCommandBase.cs` | C-12/16; DOC-011/012 | Snapshot lifetime, visible informational failure message, default-selected transport and catch-all cancellation explanation. |
| `src/Cli.Commands/src/Management/ManagementConfigCommand.cs` | C-13/16; DOC-011/012 | Snapshot display and singular auto-eject timeout, not a fresh plural-timeout query. |
| `src/Cli.Commands/src/Management/ManagementResetCommand.cs` | C-01/16; DOC-007/011/012 | Bio MPE/F9/no lock recovery; firmware-only preflight and advertised-HID rejection explained without changing transports/output. |
| `src/Tests.TestProject/Controllers/DiTestController.cs` | C-16; DOC-011/012 | Actual one-shot disposal/token/no-device handling versus propagated query errors and legacy response-name string. |
| `src/YubiOtp/examples/OtpTool/DeviceHelper.cs` | C-15/16/19; DOC-010/011 | Discovery grouping versus nonexistent serial deduplication, SmartCard-first session and HID-capable best-effort Management metadata; form-factor display bounds. |

`DOC-*` above abbreviates `MGMT-DOC-*`. All twelve accepted groups have C-scope dependencies addressed;
this does not replace A/B's owned API/model/test edits. C-18 phase/builder comments in A-owned tests remain A's work.

## Final snippets: extraction rules and typed prerequisites

This manifest describes the **final edited files**, not baseline snippets. Block ordinals count all opening
Markdown fences in a file (including bash/text/JSON). XML ordinals count `<code>` elements in a file.
Headings/first tokens below are stable locators if lines move. Extract each alternative independently;
do not concatenate snippets and thereby reintroduce duplicate locals or overlapping live sessions.
Compile-only validation must not execute a discovery, configuration write, reset or test.

Classification: **complete** = complete program/command or complete test-member example with ordinary
project scaffolding; **fragment** = composable statements/member with the typed prerequisites listed;
**sketch** = deliberately incomplete internal/application-dependent structure, not runnable consumer code;
**historical** = old code retained for explanation, not current compile target. No no-op stand-in is
authorized to make a task compile; retain the real calls, awaited tasks and disposal boundaries.

Common prerequisite sets (expand these imports/types in extraction, without fake SDK implementations):

- **M**: net10.0/C#14, nullable enabled, System/Threading/Tasks/Linq implicit imports; references Core and
  Management; imports `Yubico.YubiKit.Core.Abstractions`, `.Devices`, `.Sessions`,
  `.Transports.SmartCard`, `.Protocols.SmartCard.Apdu`, `.Protocols.SmartCard.Scp`, and
  `Yubico.YubiKit.Management`. Add `.Transports.Hid` where a typed HID connection appears.
- **T**: M plus the actual Tests.Shared assembly/project, xUnit v2 and SkippableFact dependencies;
  imports `Xunit`, `Yubico.YubiKit.Tests.Shared` and `.Infrastructure`. Wrap test members in a distinct
  class. `state` means `YubiKeyTestState state`; live execution requires an allow-listed dedicated key
  matching the attribute. An unfiltered row does not establish a transport pin.
- **D**: `IYubiKey yubiKey` (or `device` where spelled that way), `CancellationToken cancellationToken`
  (or `ct`), no other live connection to that physical key. For compilation these can be typed method
  parameters, not hardware-producing initializers. For execution the actual exposed transport/firmware,
  supported masks and authorization must match the prose.
- **S**: `IManagementSession session` or concrete `ManagementSession mgmt`, already initialized and
  caller-owned/borrowed for this fragment; `CancellationToken ct`/`cancellationToken` as spelled. The host
  must await and dispose owned resources; a fragment must not fabricate another live session to supply S.
- **E**: M plus the actual ManagementTool example project/types; imports its `.Features`, `.Features.Results`
  and `.Cli.Prompts` namespaces as needed; `System.Security.Cryptography` for zeroing snippets.

### Module Markdown (43 final fences)

`src/Management/PACKAGE_README.md` has one unchanged bash fence (Installation): **complete command**,
requires .NET CLI and access to the alpha feed. No C# in this package page.

`src/Management/README.md`:

| Block / locator | Class | Exact prerequisites / extraction notes |
| --- | --- | --- |
| 1 Installation, bash | complete | .NET CLI/alpha-feed access; not a compile target. |
| 2 Getting started, `using System.Security.Cryptography` | complete | M plus Console; async top-level program. Keep the bounded await-using body; discovery is part of the task, never executed by compilation. |
| 3 Query capabilities, `var info` | fragment | M + D with `IYubiKey device`; independent method scope. |
| 4 Enable or disable, `var info` | fragment | M + D (`device`); live write assumes unlocked configuration/5.0+ and supported PIV/OATH, which code checks. Keep query, support guard, write, disposal, delay and rescan together. |
| 5 Lock, `await using var session` | fragment | M + D (`device`), Cryptography; unlocked 5.0+ key. Secret-store persistence before the write remains caller policy; generated code is owned/zeroed here. |
| 6 Lock removal, `try` | fragment | M + D (`device`), Cryptography and caller-owned `byte[] currentLockCode` exactly 16 bytes, loaded before entering the fragment and valid until completion. Keep finally. |
| 7 Factory reset, `await using var session` | fragment | M + D (`device`); caller already confirmed Bio MPE, SmartCard exposure and destruction. Preserve actual reset and disposal; do not infer eligibility from metadata. |
| 8 Example, bash | complete | Repository-root cwd, .NET10/project prerequisites; interactive command, not compile execution. |

`src/Management/CLAUDE.md`:

| Block / locator | Class | Exact prerequisites / extraction notes |
| --- | --- | --- |
| 1 Attribute System, `ReadMetadata_ForMatchingKey` | complete test member | T; cached filters do not pin transport. |
| 2 Multi-Device, `FirstMatchingKey_ReturnsSameSerial` | complete test member | T; one actual read, not per-key enumeration. |
| 3 Multiple Attribute, `MultiFormFactor_Test` | complete test member | T; three independently bound rows, Core flagged-form-factor limitation applies. |
| 4 Device Context, `int? serial` | fragment | T + `YubiKeyTestState state`; synchronous metadata inspection. Preserve nullable serial. |
| 5 Modern Extension Syntax, `public static class IYubiKeyExtensions` | fragment (declaration) | M; C#14 static extension container, isolated namespace to avoid shadowing the actual SDK class. It calls actual `CreateManagementSessionAsync` and performs an actual read; compile independently of consumers so a duplicate extension is not imported globally. |
| 6 High-Level Get, `var deviceInfo` | fragment | M + D (`yubiKey`, `cancellationToken`). |
| 7 High-Level Get manual, `await using var connection` | fragment | M + D; SmartCard exposed. Keep connection/session/read lifetimes together. |
| 8 High-Level Set one-shot, `var config` | fragment | M + D; unlocked 5.0+ key supporting USB PIV; full mask disables other USB apps. |
| 9 High-Level Set manual, `var config` | fragment | M + D; same write prerequisites plus SmartCard; independent alternative to block 8. |
| 10 Low-Level Create, `using var scpParameters` followed by `await using var mgmtSession` | fragment | M + D + `ProtocolConfiguration customProtocolConfig`; SmartCard key configured for the supplied defaults. The real `Scp03KeyParameters.Default` factory returns a fresh caller-owned object; retain its using declaration, assign that variable in options, retain both awaited reads, then dispose the session before the enclosing parameters at scope end. Session initialization borrows parameters, not their disposal responsibility. |
| 11 Useful lifetime, `await using var mgmt` | fragment | M + D; unlocked 5.0+ key; keep read/mask guard/write and scope termination. |
| 12 Testing Considerations, `await state.WithManagementAsync` | fragment | T + `YubiKeyTestState state`; helper ends before following one-shot read. |
| 13 Helper usage, `MyTest` | complete test member | T; cached/fresh metadata comparison. |
| 14 Read-only tests, `GetDeviceInfo_ReturnsValidData` | complete test member | T; authorized key with readable positive serial. |
| 15 Firmware tests, `DeviceInfo_Firmware57Plus_HasVersionDisplay` | complete test member | T; 5.7+ filter, actual version-display/read task, not generic feature support. |
| 16 Form-factor tests, `BioFormFactor_MetadataMatchesCache` | complete test member | T; exact enum filter with documented flagged-value limitation; not a biometric operation/reset-eligibility test. |
| 17 Capability tests, `PivCapability_EnabledDevices_Accessible` | complete test member | T; USB-or-NFC metadata check, not an actual PIV transaction. |
| 18 Multi-Criteria, `AdvancedFiltering_ModernUsbPiv_Works` | complete test member | T; independent USB availability and USB-or-NFC enabled PIV checks. |
| 19 Restoration, `AutoEject_PersistsAndRestorationIsAttempted` | complete test member | T; dedicated SmartCard 5.0+ key, unlocked, original auto-eject within 0–3600 (checked before writing). Keep real change/read/finally restore/read. No reboot, no claimed ejection proof. |
| 20 Device Info Caching, `await state.WithManagementAsync` | fragment | T + `state`; snapshot use plus actual fresh query/comparison. |
| 21 Session Reuse, `await state.WithManagementAsync` | fragment | T + `state`; retain all three awaited reads and helper disposal. |
| 22 Core-owned snapshot, `var info1` | fragment | T + `ManagementSession mgmt`; sequential reads and serial assertion. |
| 23 Partial configuration, `var config` | fragment | M + S (`IManagementSession session`, `CancellationToken cancellationToken`); unlocked 5.0+ hardware supporting PIV/OATH, full CCID-only mask intentional. |
| 24 Capability Flags, `var capabilities` | fragment | M; pure flag arithmetic, no device/session. |
| 25 Qualifier System, `var info` | fragment | M + `ManagementSession mgmt`; actual read and typed firmware/qualifier/display locals. |
| 26 Session Initialization, `public static Task<ManagementSession> CreateAsync` | sketch (source excerpt) | Exact containing ManagementSession private constructor/InitializeAsync and inherited ConstructInitializedAsync context. Not a public replacement factory or consumer fragment; inspect the actual source rather than invent no-op scaffolding. |
| 27 Internal Structure, `private IManagementBackend _backend` | sketch (separate source scopes) | Session field and internal interface declaration, intentionally different containing scopes. No consumer task or standalone compile claim; actual implementation is A-owned. |
| 28 Logging, `using var loggerFactory` | fragment | M + `Microsoft.Extensions.Logging` / `.AddConsole` package, `Yubico.YubiKit.Core`, caller-owned `ISmartCardConnection connection`. Configure before constructing session; close session before factory disposal; host owns connection. |
| 29 Enumeration, `var devices` | fragment | M + `CancellationToken cancellationToken`; implicit System.Linq/Console. No previous live session. Keep loop one-shot reads and optional serial selection together. |

`src/Management/tests/CLAUDE.md`: blocks 1 (Advanced Device Filtering, `MyTest`) and 2 (Test Helper,
`MyTest`) are **complete test members** under T, compiled in distinct classes because names repeat.
Block 3 (Running Tests) is a **complete command** fence requiring repository-root cwd, toolchain and,
for integration only, authorized dedicated hardware; do not run it in snippet compilation.

Its `--integration --project Management` line includes both Management unit and integration projects;
the corrected label reflects that selection. The unit-only and real-class-filtered commands are unchanged.

`src/Management/examples/ManagementTool/README.md`: block 1 Project Structure is a **sketch** directory
map (not C#); block 2 Usage is **complete commands** with repository-root cwd and actual tool project.
The following prose now distinguishes the `info`/`config` data commands from top-level help tokens;
extra arguments such as `info --help` are ignored, not a command-specific help route.

### Module XML (five final code elements)

| File / XML ordinal / first statement | Class | Exact prerequisites |
| --- | --- | --- |
| `.../Cli/Prompts/LockCodePrompt.cs` #1, `byte[]? lockCode` | fragment | E + S (`IManagementSession session`, `CancellationToken ct`), async Task method allowing early `return`; actual prompt and current-code-authorized challenge timeout write. Keep finally and secret-input caveats. |
| `.../Features/DeviceConfiguration.cs` #1, `var result = await DeviceConfiguration.SetCapabilitiesAsync` | fragment | E + S; `Transport` is `Yubico.YubiKit.Core.Transport`; unlocked key supporting USB PIV/FIDO2; retain requested reboot, do not reuse old session afterwards. |
| same file #2, `byte[]? current` | sketch (application-dependent acquisition, real write) | E + S + Cryptography; exact application functions/delegates `Func<byte[]?> GetCurrentCode` and `Func<byte[]> GetNewCode`, yielding caller-owned 16-byte buffers (current nullable only for an unlocked key). Keep acquisition inside try, actual awaited helper and both zeroing paths. Compile may supply typed delegates as method parameters, not no-op secret-input implementations. |
| `.../Features/DeviceInfoQuery.cs` #1, `await using var connection` | fragment | E + D (`IYubiKey device`, `CancellationToken ct`); actual SmartCard connection/session and named factory token; keep query/result-display task and both disposals. |
| `.../Features/DeviceReset.cs` #1, `var infoResult` | fragment | E + S, async Task method permitting early return; caller confirmed Bio MPE/SmartCard and authorized destruction. Keep query, result guard, reset and both result branches. Host owns/disposes session. |

The `...` prefix in this XML table is exactly `src/Management/examples/ManagementTool`.

### Shared/current reference snippets touched or directly depended on

| File / stable block locator | Class | Exact prerequisites / extraction scope |
| --- | --- | --- |
| root `CLAUDE.md`, Integration Tests, `CreateSession_WithSCP03_AuthenticatesAndCommunicates` | complete test member | M + xUnit **v3** (`Xunit.TestContext.Current.CancellationToken`), actual Core/Management references, SmartCard/default-SCP03-configured authorized dedicated key if executed. Retain discovery, `using var scpKeyParams = Scp03KeyParameters.Default`, owned session with those borrowed parameters, real query/assertion and token propagation. The session ends before the parameters; do not inline the factory or drop SCP. |
| root `CLAUDE.md`, Use class / Size Guidelines `DeviceMetadataExample` blocks | sketch | Illustrative local models, not SDK DeviceInfo definitions; partial constructors/properties and teaching alternatives are not a Management compile task. Compile alternatives separately only if separately completing that general example. |
| root `CLAUDE.md`, When You Can't Write Meaningful Tests / Example comment, `ScpInitializer` shell | sketch (explanatory XML/comment) | Explicitly illustrative class shell, not the actual Core implementation and not a substitute for its real tests. Text identifies the actual fake APDU-processor malformed-response test versus real-device interoperability. No standalone executable task/compile validation claim; do not compile the empty shell as evidence of SCP behavior. |
| `docs/TESTING.md`, ConnectionType Filtering (three methods) | complete test members | T; retain all three real reads and independent attributes. These illustrate eligible-device rows, not pinning. |
| same, Default Management Selection | complete test member | T; actual cached callback plus fresh read. |
| same, Transport-Specific Testing | complete test member | T; all concrete rows, helper pin, actual ConnectionType assertion and read; do not remove lifetime or task. |
| same, Migration from Old Patterns, Old | historical | Retired DeviceId-parsing approach, no current compilation claim. |
| same, Migration from Old Patterns, New | complete test member | T; actual HID FIDO pin/assert/read. |
| `docs/architecture/physical-device-model.md`, Applet Transport Selection fence | fragment | M + D (`IYubiKey device`); actual key supports HID OTP for second example. Keep both bounded scopes and their reads; no simultaneous sessions. |
| same, Read-Only Metadata Ownership | fragment | M + D (`device`); useful unchanged one-shot metadata/typed extraction fragment. |
| `docs/architecture/connection-ownership-and-contention.md`, Why ownership is enforced | historical | Deliberate pre-enforcement conflict example, not a valid current parallel ownership recipe. |
| `docs/migration/v1-to-v2.md`, Device Info / V1 | historical | V1 types/package; preserved. |
| same, Device Info / V2 | fragment | M + `CancellationToken cancellationToken`; actual discovery/session/query/display plus disposal at method end. Current scoped prose added, code unchanged. |
| `src/Piv/examples/PivTool/SDK_PAIN_POINTS.md`, Inconsistent Async Disposal / Resolution | fragment | M + PIV reference/import `Yubico.YubiKit.Piv`, caller-owned idle `ISmartCardConnection connection`; retain bounded Management query/disposal before actual PIV factory await. Caller disposes connection after both session lifetimes. |
| `src/Tests.Shared/YubiKeyTestStateExtensions.cs`, XML #1 | complete test member | T; current theory/state/actual read and comparison. |
| same, XML #2 | complete test member | T; current theory/state and synchronous cached-property callback, no fresh read claim. |
| same, XML #3 | complete test member | T; concrete SmartCard device filter, real helper-owned connection and borrowed Management scope/read/assert. |
| `src/Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs`, XML #1 | complete test members + filter declaration | T; two distinct real read methods and nested/custom `ProductionKeysOnly : IYubiKeyFilter`. Each attribute binds independently. Keep the three-component criterion and all bodies. |
| same, XML #2 | complete test member + filter declaration | T; independent class scope from #1 to avoid duplicate custom-filter types. Current real read, no empty method. |
| `src/Tests.Shared/README.md`, #1 Quick Start | complete class | T; includes actual namespace imports, two real reads and named SCP parameter. Second test retains `using var scpKeyParams = Scp03KeyParameters.Default` around the awaited real helper; helper session/connection end before parameter disposal. Default SCP03 execution prerequisites apply. |
| same, #2 JSON and #3 text flow | complete config / sketch | Example serials are illustrative, not permission to authorize a real non-test device; text is conceptual flow, not C#. |
| same, #4 Basic Test | complete test member | T; actual read/cached comparisons. |
| same, #5 Filtered Test, `using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions` / `Scp03_AfterReset_ReturnsKeyInfo` | fragment (test-project-specific member, edited current example) | **Current compile target**. T plus the actual helper references specified immediately below. Hoist the using directive to compilation-unit scope and wrap the complete method in a test class. Retain the `MinFirmware = "5.3.0", ConnectionType = ConnectionType.SmartCard` row, caller-owned `using var scpKeyParams = Scp03KeyParameters.Default`, actual awaited reset-before-use helper, key-information read/NotEmpty assertion and named parameter. No SecurityDomain capability enum exists and none is assumed. The real helper explicitly opens SmartCard. Do not stub it or omit reset/SCP to compile. |
| same, #6 Custom Filter, `ProductionKeysOnly` / `RunsOnlyOnProductionKeyRange` | complete members | T; custom IYubiKeyFilter declaration and real async helper callback, wrapped as class members. Keep actual `await mgmt.GetDeviceInfoAsync()`, fresh serial-range assertion and cached/fresh serial comparison. No no-op await or cached-only async callback. A serial-range name does not override the dedicated-device allow list. |
| same, #7 Common Helper Pattern (OTP HID) | complete test member | T; actual pin/ConnectionType/read/assert example. |
| same, #8 Common Helper Pattern (WithOathAsync declaration) | fragment | T + actual Oath reference/import `Yubico.YubiKit.Oath`; place extension declaration in static class; no reset, actual SmartCard session/callback/disposal; unchanged. |
| same, #9 Byte-Level Recorder | sketch | Deliberate `SomeSession` placeholder and `expectedInstruction` application choice; no fake no-op session authorized. Unchanged outside Management extraction. |
| same, #10 Running Tests | complete commands | Repository-root/toolchain prerequisites; no execution in compilation. |
| `src/Tests.Shared/CLAUDE.md`, existing fenced examples | sketches except WithOathAsync member fragment | Unmodified illustrative attribute/cache/allow-list/filter/provider skeletons are not complete source definitions; existing Oath helper uses the same typed prerequisites as README #8. Only related prose was changed. Do not stub bodies to make the skeletons appear to validate Management. |

**Filtered Test actual reference prerequisites:** use the project/assembly from
`src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/Yubico.YubiKit.SecurityDomain.IntegrationTests.csproj`,
whose public `TestStateExtensions` type is in
`Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions`. Alternatively, link the exact existing
`src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs`
as source, with real project references to `src/SecurityDomain/src/Yubico.YubiKit.SecurityDomain.csproj`,
`src/Core/src/Yubico.YubiKit.Core.csproj` and `src/Tests.Shared/Yubico.YubiKit.Tests.Shared.csproj`.
Use the repository's xUnit v2 and `Xunit.SkippableFact` dependencies, net10.0/C#14, and T imports including
Core.Devices/Scp, Tests.Shared/Infrastructure and Xunit. This is the real helper, not a shipping
Tests.Shared extension. Its signature is `(bool resetBeforeUse, Func<SecurityDomainSession, Task> action,
ProtocolConfiguration? configuration = null, ScpKeyParameters? scpKeyParams = null,
CancellationToken cancellationToken = default)`. It owns its connection/sessions, not the input keys.
Execution would require an authorized dedicated SmartCard key matching the 5.3.0/concrete SmartCard row,
actually supporting SecurityDomain/SCP03 with state suitable for the helper's reset and subsequent
authentication using SCP03 defaults. These filters do not prove application support or key/reset state. Compilation
must not execute the test or claim reset/key/device interoperability. These are narrow helper/source
dependencies, not a SecurityDomain/Core module audit.

Other unmodified root/Core/PIV/architecture Markdown examples are outside this narrow dependency edit,
not implicitly certified by this report. Historical original plans, immutable records and
`docs/migration/v1-to-v2-gaps.md` are preserved without edits; do not extract their retired API sketches
as current examples. No C-owned executable file contains an additional code element beyond the five
module XML examples enumerated here.

## Deferred and integration handoff

- Preserve every triage `MGMT-IMP-*` and `MGMT-XREF-*` item. No false-NFC recovery decision, mask/gate
  policy, timeout range expansion, reset-family/lifecycle behavior, Core decoding change, test cleanup,
  row policy or new assertion was implemented. The guide's test examples are documentation, not changed tests.
- In particular, IMP-013..017 still cover the active SmartCard selector, ignored command-specific extra arguments/unreachable NFC
  menu, immutable prompt strings/exceptional cleanup, dropped/swallowed cancellation and misleading
  reboot/FIPS/reset/legacy-response strings. Correct prose is not a behavior fix.
- Shared CLI reset still advertises HID and has broad erasure text; ManagementTool still displays its
  misleading firmware-only reset/recovery/FIPS/help messages. Comments and guides now qualify these.
  Coordinator should retain the exact deferred dependencies rather than silently updating those tokens.
- Core FIPS mapping and flagged-form-factor issues remain XREF-001/002; first-match/cache explanation
  corrections address XREF-003 documentation only. No certification/device qualification is claimed.
- Package metadata/support files and `PACKAGE_README.md` were inspected and remain unchanged. The old
  Spectre HintPath and unrelated API-design skill path remain unselected support observations, as triaged.
- Coordinator must integrate this diff, compare executable tokens/directives/API shapes, extract final
  meaningful snippets with the prerequisites above, run its authorized verification and assign a fresh
  independent reviewer. Recheck actual final integrated locators after A/B integration; their source/XML
  snippets are separately owned and are not classified as C edits here.
