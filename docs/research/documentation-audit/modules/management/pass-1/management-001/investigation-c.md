# Management investigation C — consumer journeys and external references

> **Later review erratum (2026-11-03):** C-10's statement that dispatch recognizes only `info`/`config`
> is incomplete: top-level `-h`, `--help` and `help` are recognized too. The actual deferred defect is
> ignored trailing arguments such as `info --help`. See C-MOD-R4 in [review-c-module.md](review-c-module.md)
> and corrected MGMT-IMP-014 in [triage.md](triage.md). The original investigation below is preserved.
>
> **Later path-check erratum:** C-05's relative-link diagnosis was wrong. From `src/Management/tests/`,
> `../../../docs/TESTING.md` resolves correctly to the repository document. The proposed extra `..`
> escaped the repository. Final validation restored the original valid link; original findings remain below.

## Scope, baseline, and evidence limits

Investigator: GPT-6.1 Sol (`github-copilot/gpt-6.1-sol`), 2026-11-03. Source baseline and observed
HEAD: `da61d2e6d754b4c155cbb2579202e910986f4b3a`. This is a read-only investigation except for this
report. No build, test, example execution, hardware operation, commit, or delegation was performed.
Candidate IDs below are local investigation IDs, not adjudicated `MGMT-DOC`/`MGMT-IMP` IDs.

Reconciliation checkpoint: read the coordinator's completed A/B triage after the steering update.
The mapping below preserves its authoritative IDs; overlaps are supporting evidence, not additional
bugs. C-only candidates remain for coordinator adjudication.

| C candidates | Existing accepted/deferred groups |
| --- | --- |
| C-01 | `MGMT-DOC-007`, `MGMT-IMP-010`; reset scope/recovery, not a second reset investigation. |
| C-02, C-04, C-05 | `MGMT-DOC-008`, `MGMT-IMP-004/006/007/009/011`, `MGMT-XREF-003`. |
| C-03, C-06, C-07, C-08, C-15, C-17 | Documentation dependencies of `MGMT-DOC-006/009`; C supplies current snippet and external-reference corrections. |
| C-13, C-19 | `MGMT-DOC-010`, `MGMT-XREF-001/002`; preserve the distinction between misleading output and wrong decoding. |
| C-14 | `MGMT-DOC-001/002/003/004/005`, `MGMT-IMP-001/002/003`; no new per-field policy authorized. |
| C-09–12, C-13 result/UI details, C-16, C-18 | C-only sample/shared-comment candidates for adjudication; assign new deferred IDs centrally, not here. |

Read root `CLAUDE.md`, reviewed `.claude/skills/`, read module README/CLAUDE, module
`src/Management/tests/CLAUDE.md`, campaign workflow/style, and the run plan. The requested root
`tests/CLAUDE.md` does not exist; the module-local test guide is the applicable file. Read Core
guidance for narrow source dependencies. Loaded the secure-credential-prompt skill for the lock-input
review; build/test/commit skills were not applicable because those actions were prohibited.

The checkout contains coordinator changes to campaign records and the pre-existing untracked files
listed in the plan. None were modified by C. The report's references to campaign instructions include
the coordinator's working copies; implementation evidence and module line numbers refer to baseline
HEAD. Generated `obj` files encountered during example enumeration were excluded from semantic scope.

Coverage is deliberately different from execution evidence. Reading an assertion establishes what a
test would check, not that it ran successfully here. A queued fake response does not establish hardware
support. This report does not certify device-family behavior or any platform.

## Outcome and recommended edit grouping

The public README already has a useful task-first structure, accurate current options syntax, and
good borrowed-memory cleanup guidance. Preserve that structure. The larger correction is the contributor
guide: it mixes current ownership rules with obsolete signatures, obsolete test-row semantics, an
incorrect restoration procedure, and an implementation sketch that no longer describes the reader.

Recommended documentation batches:

1. Reconcile reset applicability and lock-code recovery across the public guide, example guide, and
   sample XML. Keep SDK firmware checks distinct from actual device support.
2. Replace the contributor test instructions with the current placeholder/filter/helper contract and
   a genuinely usable restoration procedure. Repair shared test documentation at the same time.
3. Correct current Markdown/XML snippets, including whole-block lifetimes. Do not repair executable
   examples in this pass; qualify their limitations and preserve deferred implementation records.
4. Reconcile active architecture/migration references and the few shared CLI comments that depend on
   Management. Leave clearly historical evidence and original plan pseudocode unchanged.

## Findings

### C-01 — reset support and recovery are described as firmware-only when they are family-dependent

**Disposition:** candidate documentation correction, high reader impact; sample UI/support-predicate
changes are deferred implementation. Coordinate with A's reset API finding rather than count it twice.

**Observed:**

- `src/Management/README.md:24-28,116-134` gives a firmware/transport table and later says passing the
  check does not guarantee support, but never names the documented supported family. `:178` says only
  firmware below 5.6.0 has no reset fallback, which invites the inverse inference for ordinary 5.7 keys.
- `src/Management/examples/ManagementTool/README.md:17` correctly mentions Bio MPE; `:74` drops the
  family qualification. `Features/DeviceReset.cs:29-31,40-48,59-72` says firmware determines support,
  and lists OATH/OpenPGP erasure and all PIN/PUK defaults without the actual family context.
- `Cli/Menus/DeviceInfoMenu.cs:140-148` reports reset available solely from firmware. `ResetMenu.cs`
  uses the same predicate before confirmation and says all PINs/PUKs return to defaults at line 91.
  `LockCodeMenu.cs:54-56` promises factory reset as recovery on 5.6.0+ without a family qualification.
- Shared `src/Cli.Commands/src/Management/ManagementResetCommand.cs:22-31,45-64` repeats the firmware-only
  description and advertises both HID routes in `AppletTransports`, although reset only dispatches over
  SmartCard. An explicit global HID preference reaches the SDK's rejection, not a reset.

**Implementation evidence:** `ManagementSession.cs:164-173` checks effective firmware and dispatches
only `SmartCardBackend`. It does not check the product family. Unsupported-device APDU status is allowed
to propagate. `DeviceReset.IsResetSupported` only compares versions; it is not a capability probe.

**Primary evidence:** R2 and R3 below limit this operation to Bio Multi-protocol Edition; R4 corroborates
the SmartCard route, but is sibling implementation evidence, not the product specification. R5's precise
v1 command remarks exclude the PIV F9 attestation slot and describe removal of the shared PIN, not a
universal return to a default PIN. Do not promise erasure of factory material or applications absent on
the applicable device.

**Proposed documentation:** apply `MGMT-DOC-007`: explain that the SDK admits release firmware 5.6.0+
over SmartCard, while the documented combined reset is for Bio MPE's FIDO/PIV applications and shared
PIN. Name the failure difference (`NotSupportedException` for SDK checks, `ApduException` for device
rejection). Remove the lost-configuration-code recovery promise: the primary passages do not establish
Management configuration or lock-code erasure even on MPE. Keep those unknowns in `MGMT-IMP-010`, not
as an implied recovery procedure. State the destructive user-credential effects and retained F9 slot;
do not broaden them to every byte, every application, or every factory credential.

**Deferred implementation:** revisit the sample's misleading `IsResetSupported` name/boolean and UI
eligibility messages, and the shared reset command's permitted transports. These are executable tokens,
not prose edits. Device eligibility policy needs a product decision; a Bio form-factor alone does not
necessarily distinguish MPE from FIDO Edition. Re-review all named documentation when that policy changes.

### C-02 — configuration-test policy contradicts itself, and the recommended recovery code is unsafe

**Disposition:** candidate documentation correction, high priority. No new test or hardware operation.

`src/Management/CLAUDE.md:532-568` first says NEVER write configuration tests, then says the rule is
restore-what-you-changed, not don't-run-it. `src/Management/tests/CLAUDE.md:53-61` repeats the prohibition
and links to the contradictory safe pattern. `CLAUDE.md:22` means no automatic test reset, not absence
of the public reset operation; make that distinction explicit.

The supposedly safe block at `src/Management/CLAUDE.md:570-603` has three independent problems:

1. Both calls use the removed `reboot:` SDK parameter.
2. It retains `connection` and `mgmt` across a reboot and attempts restoration through that same old
   session in `finally`. Delay does not reconnect a session. The source sends the reboot TLV but does
   not replace the connection/session; the guide itself correctly warns elsewhere that reboot ends use
   of the old session.
3. It saves only USB/NFC masks, yet leaves the test payload unspecified. That is not restore-everything
   for tests modifying timeouts, flags, restriction, or lock state. Nor does `finally` guarantee a
   successful restore if the restore operation fails or the remaining interface is unusable.

`ManagementSessionCapabilityTests.cs:28-78` instead performs no-reboot persisted-config readback and
restores USB in `finally`; that is evidence for persisted values, not proof the newly disabled applet
is inaccessible. Its NFC test `:85-123` explicitly calls restoration best-effort and does not assert
the restored value. Do not cite it as proof of guaranteed hardware restoration. A owns the test audit.

**Proposed procedure:** state the canonical allow-list boundary, name the suite-wide blast radius,
choose a change that preserves a recovery transport, save every field being changed, and use `finally`
for the restoration attempt. For a rebooting change, dispose old scopes, rediscover the same authorized
device, open a fresh session, restore, and verify. Mark reconnection/selection policy as application-
dependent rather than publishing an apparently complete generic recipe with omitted recovery. A finite
3-second delay is an example pause, not an enumeration guarantee. Consider showing the narrower
no-reboot readback case separately. Link to `ReconfigurationDiscoveryInvariantTests` as an implementation
example, subject to A's full assessment; do not claim it was executed in this investigation.

### C-03 — current contributor and XML examples contain removed parameters and incomplete prerequisites

**Disposition:** candidate Markdown/XML correction; compile checks belong to the editor/validation stage.

- `src/Management/CLAUDE.md:300-309,538-550,570-603` calls SDK `SetDeviceConfigAsync(config, reboot: true)`.
  Both current overloads take `SetDeviceConfigOptions?`; see `ManagementSession.cs:122-125` and
  `IYubiKeyExtensions.cs:72-76`.
- `examples/ManagementTool/Features/DeviceInfoQuery.cs:36-49` uses
  `ManagementSession.CreateAsync(connection, ct)`. Argument two is `SessionCreationOptions?`, so use
  `cancellationToken: ct`. This XML-only correction is in scope.
- Root `CLAUDE.md:522-534` has a supposedly good integration example calling
  `ManagementSession.CreateAsync(device, scpKeyParams: scp03Keys)`: the factory takes a connection,
  not `IYubiKey`, and secure-channel policy is in options. Its literal test name in the following
  limitation example should not be carried forward without locating the actual test.
- `src/Management/CLAUDE.md:100-119` assigns nullable `state.SerialNumber` to `int`; the source state
  returns `int?`. Do not normalize missing serials to a magic value in a general usage sketch.
- `src/Management/CLAUDE.md:160-168,186-206` calls manual SmartCard code “equivalent” to the one-shot
  device extension. That is true only after explicitly restricting the example to SmartCard. On HID-
  only devices the one-shot extension can work and the manual SmartCard open cannot. The second block
  also uses implicit `reboot` and `lockCode`; declare them as typed application prerequisites or keep
  the alternatives separate and use consistent values.

Do not treat duplicated local names in before/after alternatives as one compilable program. Label the
alternatives and compile their current versions independently. The correction must preserve the useful
ownership explanation rather than erase manual-connection examples.

### C-04 — test-row, helper-signature, and cached-info explanations describe a previous harness

**Disposition:** candidate documentation correction across module/shared guides and shared XML.

- `src/Management/CLAUDE.md:58-93` says each matching allow-listed device runs once and gives three
  serials → three runs. Actual `WithYubiKeyAttribute.GetData:156-174` yields **one placeholder per
  attribute**. `YubiKeyTestState.cs:325-345` binds it to `candidates.FirstOrDefault()`. Multiple
  attributes create independent rows; they are not deduplicated into one row per physical device.
- The helper signature in `CLAUDE.md:365-382` reverses `configuration`/`scpKeyParams`, omits
  `preferredConnection`, and claims to query info at helper entry. The current async helper is
  `(action, configuration, scpKeyParams, preferredConnection, cancellationToken)` and passes
  `state.DeviceInfo` directly (`src/Tests.Shared/YubiKeyTestStateExtensions.cs:70-90`). It does not
  explicitly obtain fresh DeviceInfo for the callback. Session initialization may perform its own
  firmware read; that is not the callback's cached-value origin.
- The “pre-queried”/“initial query” language recurs at `CLAUDE.md:344-357,386-398,607-623` and module
  test guide `:39-51`. Explain the state cache and when a fresh session read is needed.
- `src/Tests.Shared/README.md:18-45` passes `Scp03KeyParameters.Default` positionally as argument two
  to the helper, where a `ProtocolConfiguration?` is expected. Name `scpKeyParams:`.
- Shared helper XML examples `YubiKeyTestStateExtensions.cs:57-69,103-115` use removed
  `[YubiKeyTheory]` and `YubiKeyTestDevice`; use current `[SkippableTheory]`, `[WithYubiKey]`, and
  `YubiKeyTestState`. The sync overload also lacks a `preferredConnection` parameter description.
- `src/Tests.Shared/CLAUDE.md:269-273` says each attribute produces a case per matching device,
  contradicting its own earlier placeholder sketch and the shared README's correct limitation at
  line 263. Correct the shared contributor limitation to one row bound to the first match. This is
  explicitly included in `MGMT-XREF-003`; do not change runtime binding policy.
- The advanced guide sample `src/Management/CLAUDE.md:508-530` infers PIV enabled over USB from
  `RequireUsb = true, Capability = Piv`. `FilterDevices:90-106` checks those independently;
  `HasCapability` accepts PIV enabled on **USB or NFC** (`YubiKeyTestState.cs:421-427`). The filter
  does not establish the assertion at line 527. Describe the narrower real predicate or explicitly
  require the USB bit in a custom filter/test. Do not claim infrastructure established it.

The guide should retain useful filtering examples, not claims that Management owns a uniquely “most
powerful” shared harness. All these modules use the same infrastructure. Do not replace tests wholesale;
A owns assertions and executable test findings.

### C-05 — test commands and links do not reach the advertised target

**Disposition:** candidate documentation correction.

`src/Management/tests/CLAUDE.md:7` links `../../../docs/TESTING.md`; from this directory that resolves to
`src/docs/TESTING.md`, not root `docs/TESTING.md`. Use `../../../../docs/TESTING.md`.

At lines 65-77, “all Management tests” and “integration tests only” omit `--integration --project
Management`. `scripts/toolchain/TestTargets.cs:11-21` runs unit tests by default and requires an explicit
project for integration. A fully-qualified-name filter does not include integration projects by itself.
The class filter `ManagementIntegrationTests` names no tracked Management class. Show separate scoped
unit and integration commands and a real class such as `ManagementSessionSimpleTests`. No command was
run. Keep runner instructions consistent with the mixed xUnit project files.

### C-06 — architecture and firmware sketches in the contributor guide are no longer accurate

**Disposition:** candidate documentation correction, source details coordinated with A/B.

- `src/Management/CLAUDE.md:703-715` invents `ReleaseCandidate`/`Development` qualifier values and
  `-rc1`/`-dev` displays. Core `VersionQualifier.cs:21-26,92` defines Alpha/Beta/Final and formats
  `version.type.iteration` (for example `5.7.2.beta.1`); `DeviceInfo.VersionName` uses bare firmware
  for Final. Use actual values and distinguish `FirmwareVersion` for comparisons from display text.
- `:719-736` presents a local `_version` comparison as feature gating. Actual operations call inherited
  `EnsureSupports`, which uses effective `ApplicationSession.FirmwareVersion`, including overrides and
  pre-release policy. An illustrative simplified gate must not silently erase that policy.
- `:763-796` says no protocol branching in public APIs and all methods delegate to `_backend`, despite
  correctly noting nearby that info reads use Core and reset is SmartCard-specific. The current source
  directly contradicts the absolute. Explain a write/mode backend with deliberate exceptions for
  shared reads and SmartCard-only reset. Include OTP in the opening transport description.
- `:817-839` calls removed `_backend.ReadConfigAsync` and uses a one-byte boolean-more-data sketch.
  Core `DeviceInfoReader.cs:61-108,116-129` actually reads counted remaining pages, rejects duplicate
  more-data tags, checks length, and bounds the P1 page index. Replace the obsolete pseudo-implementation
  with a current concise flow and a source link. Do not reproduce an older algorithm as current code.
- `:881` calls upper wire bits FormFactor enum flags. Core `FormFactor.cs` names only values 0–7;
  `DeviceInfo.cs:85-88` currently derives IsFips/IsSky and casts the raw byte without masking. Explain
  wire flag bits versus named enum values accurately, and hand off the raw-form-factor behavior to
  the deferred Core audit rather than silently documenting a normalization that does not happen.
- `:886-891` names old conceptual namespaces (`Core.YubiKey`, `Core.SmartCard`). Link current
  `Core.Abstractions`, `Core.Devices`, `Core.Sessions`, and `Core.Protocols.SmartCard.*` where useful.

The backend lifecycle test establishes non-disposability, not “zero branching,” feature support, or
the correctness of the retired page algorithm. Sibling architectural similarity is not protocol proof.

### C-07 — active shared examples use the removed ManagementSession.Transport property

**Disposition:** candidate cross-document correction.

`docs/TESTING.md:325-376` tells readers to assert `mgmt.Transport` in two code blocks and its best-
practices list. `docs/architecture/connection-ownership-and-contention.md:112-113` tells maintainers to
preserve that property. The current public property is inherited `ConnectionType`; migration already
documents this (`docs/migration/v1-to-v2.md:105`). Use `mgmt.ConnectionType` and describe the actual
selected interface, preserving the important distinction between filtering and pinning.

`docs/TESTING.md:306` says Management “tries” its order. Prefer “selects the first exposed supported
interface” so this is not mistaken for contention fallback; the later no-fallback section is correct.
Dependency: Core common contract and A's transport evidence; no API change is needed.

### C-08 — the physical-device-model transport example attempts two live Management sessions

**Disposition:** candidate Markdown example correction, high practical usefulness.

Read the complete block at `docs/architecture/physical-device-model.md:171-178`: it opens `mgmt` with an
`await using var`, then opens `otpMgmt` in the same scope before disposing `mgmt`. The example violates
its own next section's one-connection rule. On a grouped key the second open throws
`ConnectionInUseException`, not a successful transport override demonstration. Separate the alternatives
or put each in a bounded `await using (...)` block. Neither the line containing Management nor the line
containing the override can be validated independently of this implicit lifetime.

The earlier metadata block at `:141-147` is a valid composable fragment: it uses the one-shot extension
after the connection alternatives have ended. Keep this useful example.

### C-09 — ManagementTool documentation advertises HID operation that its active selector excludes

**Disposition:** candidate public/example qualification plus deferred executable sample defect.

The module README points to the tool without caveat. The tool README `:7,63-68` says all Management
operations across all three transports. All six menus and both local command handlers call static
`Cli/Prompts/DeviceSelector.SelectDeviceAsync`. That selector's `SupportedConnectionTypes` contains
only SmartCard (`:28-34,71-74`). Its local comment says Management requires SmartCard and that listing
all interfaces yields three physical rows. Both explanations are obsolete for current Management and
the grouped-device model. Its no-device message nevertheless claims all three transports (`:82`).

`ManagementDeviceSelector.cs` really lists all three but no active ManagementTool call site uses its
singleton. The SDK supports the three info/config routes; the executable's filter is the restriction.

**Pass 1:** repair the static selector's explanatory comment and document that the present tool selects
SmartCard-capable devices; reset itself remains SmartCard-only. Do not claim a behavior fix after a
comment change. Explain that disabling the last SmartCard-dependent app can make the current tool unable
to find the key even though SDK HID configuration remains possible.

**Pass 2:** decide whether to reuse the existing subclass or repair the static selector; add focused
no-hardware selection/routing evidence. No deletion of the unused subclass is authorized now. Re-review
README transport/requirements/feature sections and retry messages when selection changes.

### C-10 — the example guide misses usable CLI tasks and overstates its feature coverage

**Disposition:** candidate documentation correction; help-string behavior is deferred implementation.

`ManagementTool/Program.cs:16-43,73-83` has no-argument interactive mode and only two command tokens,
`info` and `config`, both read-only. The README documents only starting the menu. Add real command
examples using `dotnet run --project ... -- info` and `-- config`, with working-directory context.
R6 confirms application-argument forwarding. The command handlers still use device prompts, so do not
advertise a fully unattended or serial-selectable CLI.

`PrintUsage` advertises `command --help`, but dispatch looks only at `args[0]`; `info --help` runs the info
handler and can query a device. Do not repeat that help promise in prose. Fixing executable help strings
or argument parsing is deferred.

The project tree omits `Commands/` and `ManagementDeviceSelector.cs`. More importantly, “all Management
operations” is false: `Features/DeviceConfiguration.SetNfcRestrictedAsync` exists but Program's menu has
no NFC-restriction action. Document which tasks are actually exposed and distinguish reusable feature
helpers from menu reachability. SDK no-reset/legacy-mode gaps must not be hidden behind “all.”

### C-11 — “secure lock-code handling” overstates the sample's memory guarantees

**Disposition:** candidate documentation qualification, plus deferred sample security implementation.

`ManagementTool/README.md:58` and `LockCodePrompt.cs:20-24` imply secure handling. The successful callers
do clear returned byte arrays in `finally`, and confirmation uses `FixedTimeEquals`; preserve those good
patterns. But `PromptForLockCode:69-72`, `PromptForNewLockCode:107-110`, and `TryParseHex:153-163` create
immutable `string` input/confirmation/normalized copies. Array zeroing cannot clear those strings.
Spectre's `.Secret()` masks terminal input, not managed-memory storage (R7).

There is a second, bounded failure path: `PromptForNewLockCode` obtains the first byte array at line 100,
then performs the confirmation prompt before any enclosing cleanup for that first array. If the prompt
throws, the caller never receives the array and cannot zero it. Later ordinary mismatch/cancellation
paths do explicitly clear it. This is a source-derived defect, not an executed reproduction.

**Pass 1 wording:** the sample masks input and clears the mutable byte arrays it owns on the described
paths, but immutable prompt strings cannot be erased. Do not recommend it as a production secret-input
primitive. Keep caller cleanup requirements in XML. `README:59`'s “never logged or displayed” is supported
by the inspected ordinary paths for actual code contents, but does not imply absence from memory.

**Pass 2:** replace string-based input with a bounded secret-input design and close exceptional cleanup
paths, following repository credential guidance. Add failure-path evidence. Re-review all security
assurances, the XML cleanup sketch, and device-lock examples after implementation.

### C-12 — sample cancellation is swallowed or never forwarded

**Disposition:** deferred executable example defect; qualify explanatory claims where present.

`Program.cs:58-68` creates a console token but every menu callback ignores its `ct` and calls the menu
without the token. All feature wrappers (`DeviceConfiguration`, `DeviceInfoQuery`, `DeviceReset`) catch
`Exception`, including `OperationCanceledException`, and return ordinary failure results. The static
selector catches it as unavailable metadata. Thus a cancellation-token parameter is not proof the
tool delivers a coherent cancellation flow. No operation was run.

A narrow cross-module trace found the same catch-all metadata pattern in Cli.Shared, CTAP2, OpenPGP,
PIV, YubiHSM, and YubiOTP selectors/helpers. OATH's current selector explicitly excludes cancellation
from its catch and forwards prompt cancellation. Record the shared dependency rather than fix every
sibling during this audit. Core SDK cancellation remains A's topic; do not mislabel these sample bugs
as Management session behavior. Re-review sample lifecycle/cancellation prose after the later fix.

### C-13 — result and display explanations confuse requested reboot, device classification, and state

**Disposition:** candidate comment/XML corrections where truthful; executable labels/result semantics
are deferred implementation.

- `Features/Results/ConfigResult.cs:27-30,37-42` describes `RebootRequired` as whether the device needs
  a future reboot. The feature wrappers instead pass the **requested** `reboot` boolean after an
  already-sent write (`DeviceConfiguration.cs:76-80,115-119,149-153,183-187`). With `reboot:false`
  the result is false even when a change still needs a reboot; with true the reboot was requested,
  not merely required. Document the present request meaning without promising device observation;
  defer naming/semantic redesign and any UI changes.
- `DeviceInfoMenu.cs:63-69` and local `InfoCommand.cs:47` label `info.IsFips` as “FIPS Mode.”
  `DeviceInfo.cs:85-96` derives it from the manufacturing form-factor bit; per-application approval
  is separate `FipsApproved`. Correct relevant explanatory text and preserve an implementation item
  to relabel executable output. The shared `ManagementInfoCommand` already says “FIPS Device.”
  This distinction does not make the current FIPS capability/approval decoder reliable; see C-19
  and the accepted Core dependency.
- The tool's flags explanation at `DeviceFlagsMenu.cs:45-48` omits the CCID-only requirement for
  TouchEject. The public README correctly includes it; R1's device-flags table independently says
  “In pure CCID mode.” Add the condition in prose/XML now; changing UI strings is later work.
- Shared `ManagementConfigCommand.cs:12-15` promises “timeouts” while it displays only auto-eject;
  use the precise singular or retain an implementation item to add challenge-response display.

### C-14 — current guides need an actionable unlock recipe and per-field qualifications

**Disposition:** candidate usefulness correction, dependent on B's lock/NFC analysis.

The public guide teaches setting a random code, then says later calls need `CurrentLockCode`, but never
shows removing the lock. R1's Set Configuration Lock passage specifies all-zero replacement disables
it. Add a short caller-owned current-code fragment sending `NewLockCode = new byte[16]` and clearing
the actual secret in `finally`; distinguish authenticating one write from permanently removing the
configuration lock. The sample feature helpers already make that distinction.

The guide lists NFC restriction among configurable properties without explaining its task or 5.7+
device requirement. R2's `config nfc --restrict` documents temporary transport restriction cleared by
USB power, not a physical sensor touch. The SDK only gates the overall write at 5.0.0, so do not promise
that the SDK enforces every per-field firmware floor. Explain the existing limitation near the feature.

Likewise explain that capability masks are complete replacement sets for the selected transport, not
incremental enable lists. The `Piv | Oath` snippet intentionally disables other enabled USB apps.
`DeviceConfig.Validate` checks exact `None`, not whether a nonzero mask is supported by this device.
Consumers should intersect/inspect supported capabilities, not infer any arbitrary nonzero mask is safe.
B owns the precise wire/validation dispositions; these are their public task-flow dependencies.

### C-15 — absolute ownership language exceeds the grouped, process-local and finalizer bounds

**Disposition:** candidate targeted qualification; shared Core-wide audit remains deferred.

`src/Management/README.md:163-168` presents physical exclusion without the grouping/process boundary and
says “no finalizer backstop” for both sessions and caller-created connections. ManagementSession has no
finalizer, and deterministic disposal is required. But current built-in `UsbSmartCardConnection.cs:56-65`
has a finalizer requesting best-effort shutdown. Say explicitly that callers must dispose, sessions have
no finalizer recovery, and a connection's best-effort cleanup is not a lifetime guarantee. Do not claim
the implementation has no connection finalizer.

The same stale absolutes occur in the module contributor guide, root/Core guidance, and
`docs/architecture/{physical-device-model,connection-ownership-and-contention}.md`; Core README
`:310-316` has the better grouped/process-local and best-effort finalizer formulation. Reconcile the
Management-relevant shared statements without reopening discovery implementation. Its own guarantee
guide explains conservative ungrouped records. Avoid claiming all external handles/processes are
excluded by the SDK registry.

### C-16 — active shared prose still describes a SmartCard-only Management query or removed session type

**Disposition:** candidate XML/comment corrections; executable diagnostic text is deferred.

`src/Cli.Commands/src/Infrastructure/YkDeviceContext.cs:35-38` gives “SmartCard unavailable” as the
reason Management info is absent. `YkCommandBase.cs:63-74` repeats it and calls visible info output a
“debug warning.” The enrichment call is actually `device.GetDeviceInfoAsync()` with no explicit
preference; it can select either HID route. Describe metadata enrichment as best-effort and list an
unavailable/busy selected transport or a failed query, rather than absence of CCID alone.

`YkDeviceContext` says it always opens Management first; the selector may already query metadata and
the one-shot enrichment closes its own session before the command runs. Keep clear that `Info` is a
snapshot, not an open session. Shared info/config commands use it and do not refresh it in their body.

`src/Tests.TestProject/Controllers/DiTestController.cs:7-9` claims broad proper error handling, but only
handles no-device explicitly; connection/query failures propagate to the host. Its response and the
minimal endpoint in `Program.cs` say “ManagementSessionSimple,” a removed name. Correct the broad XML
claim now; changing returned strings is deferred example implementation. These endpoints correctly
use the one-shot extension with a named token and do not retain its connection.

### C-17 — present-tense sibling disposal example violates current API and lifetime contracts

**Disposition:** candidate active sibling Markdown correction.

`src/Piv/examples/PivTool/SDK_PAIN_POINTS.md:80-88` claims async-disposal consistency is fixed but its
resolution block calls removed `ManagementSession.Create(connection)` and keeps Management alive
while creating PIV on the same connection. Although the heading records development history, this is
an active, updated example document rather than an immutable campaign evidence record. Mark the old
code historical or show `await ManagementSession.CreateAsync` in a bounded scope, then create PIV after
Management disposal. The borrowed connection remains caller-owned. Do not merely add `Async` and leave
the simultaneous session acquisition.

### C-18 — development residue and unsupported absolutes should be replaced, not simply shortened

**Disposition:** candidate bounded editorial correction, subordinate to factual items.

The contributor guide's “most powerful,” “zero branching,” categorical single-operation anti-pattern,
and repeated lifecycle descriptions obscure the real decisions. A session for one operation is valid;
one-shot helpers are a convenience, not a correctness rule. Retain the useful task decision matrix and
explain trade-offs calmly. `ManagementTool/Program.cs:22`'s “existing behavior” is migration narration;
the comment can simply name interactive mode. This is the only explicit development-history residue
found in C-owned executable files; the module-wide scan also found “Phase 38” in A-owned transport
tests. No TODO/FIXME marker was found in the tracked module scan. This absence is not evidence that
implementation is complete. A owns the test comment correction.

### C-19 — consumer FIPS and flagged-form-factor examples depend on known wrong Core decoding

**Disposition:** apply `MGMT-DOC-010`; deferred `MGMT-XREF-001/002`, already investigated by A.
This is the consumer dependency record, not another Core audit or a new bug count.

The narrowly inspected `Core/src/Devices/DeviceInfo.cs:85-96,174-181` casts the raw form-factor byte
without normalization and runs FIPS tag values through application-bit decoding. The existing
`CapabilityMapper.FromFips` table at `:271-277` uses a different bit mapping. A's report and the
coordinator triage are the original evidence/decision for those defects.

The guide's FIPS filter examples, form-factor filtering examples, device-display helpers, and
`IsResetSupported` eligibility discussions must not invite security or family conclusions the metadata
cannot currently support. Add a precise limitation: current FIPS capability/approval decoding is a
known Core issue, so do not use those masks/derived filters as evidence of FIPS approval or enforcement;
flagged form-factor bytes may not equal the named base enum member, so equality-based form-factor
filters/display can miss those devices. `IsFips` is a distinct manufacturing indicator, not a substitute
for approval. Do not redefine the wrong mapping/raw enum cast as correct behavior or infer certification.

Affected C surfaces are `src/Management/README.md` metadata interpretation, contributor guide's filter/
model/gotcha sections, ManagementTool explanatory XML/README, shared test guidance, and the sibling
device-display examples inventoried below. After a separately scoped Core fix, re-review every derived
filter/example and remove only the limitation the new fixture/evidence actually resolves. No Core
runtime edit or new family research is selected here.

## Primary references and focused passage cache

All URLs below were accessed 2026-11-03. The product pages are live vendor documentation, not numbered
standards or immutable specification revisions. No publication revision was visible in the fetched
page; that limitation is recorded rather than inventing one. The excerpts here are the durable cache.
Claims are scoped to their named device/SDK layer; none establish new v2 runtime behavior.

| ID | Exact URL, title/status, section | Passage and supported conclusion |
| --- | --- | --- |
| R1 | https://developers.yubico.com/yubikey-manager/Config_Reference.html — **YubiKey 5 Series Configuration Reference Guide**, live unversioned developer reference; Concepts / Supported Capabilities, Configuration Lock, Device Flags; Writing Information / Set Configuration Lock, Unlock Configuration Lock, Reset | “Supported Capabilities are set during manufacturing” and “cannot be modified.” Lock is a “16 Byte value.” All-zero replacement disables it. Unlock TLV must be in the same command; it does not permanently remove protection. Device flags: “In pure CCID mode eject/inject the card with button.” Reset TLV forces reboot after other TLVs parse; it is **not** global factory reset. These support the capability/lock/touch-eject task explanations. |
| R1a | Same R1, Writing Information / Enabled NFC Capabilities | Page prints `TAG = 0x0c` and incorrectly repeats “Enabled USB.” It also uses 0x0c for reboot; the read table says NFC enabled 0x0e. Current C# uses 0x0e; R4 constants/encoder agree. This reference has an apparent typo and is not sufficient authority to change the encoder to 0x0c. B must adjudicate wire claims; preserve this contradiction. |
| R2 | https://docs.yubico.com/software/yubikey/tools/ykman/Config_Commands.html#ykman-config-reset-options — **YubiKey Manager (ykman) CLI Guide**, current live guide (2026 copyright); `ykman config reset`; also `#ykman-config-nfc-options` and `#ykman-config-set-lock-code-options` | Reset: “This command is only used with the YubiKey Bio Multi-protocol Edition”; wipes data/restores factory settings for its applications. NFC `--restrict`: “Disable NFC for transport. Re-enable by USB power. Available for YubiKeys with firmware version 5.7 and later.” Lock code protects application configuration, not OATH credential operations; introduced firmware 5.0. Vendor tool behavior/product conditions, not v2 validation promises. |
| R3 | https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-bio-specifics.html#resetting — **YubiKey Technical Manual / YubiKey Bio Series Specifics**, live unversioned hardware manual; Resetting, Device PIN, Device PUK | Reset deletes credentials, PIN and fingerprint templates. MPE uses a combined reset when PIV/FIDO are enabled; trying the MPE command on non-MPE Bio produces an error. “It is not possible to enable or disable protocols after a PIN is set.” Device PUK is blocked by default. These are product-family conditions, not a firmware-only availability predicate. |
| R4 | https://developers.yubico.com/yubikey-manager/API_Documentation/_modules/yubikit/management.html — rendered Python source, live version not exposed in fetched page; `ManagementSession.device_reset`, `_ManagementSmartCardBackend.device_reset`, tag constants / `DeviceConfig.get_bytes` | `device_reset` docstring: “only available for YubiKey Bio” with shared PIN; rejects a non-SmartCard backend; sends INS 0x1F. Encoder uses NFC enabled 0x0E, reboot 0x0C. Corroboration only: do not treat this implementation as a normative spec or assume its omissions are C# policy. |
| R5 | https://docs.yubico.com/yesdk/yubikey-api/Yubico.YubiKey.Management.Commands.DeviceResetCommand.html — v1 SDK API reference, live API page, Remarks | “only available on YubiKey Bio Multi-protocol Edition keys”; deletes FIDO credentials/fingerprints/shared PIN, PIV keys/certificates **except the F9 attestation slot**, and user PIV data. Supports avoiding an all-factory-material-erased promise; not a current v2 signature source. |
| R6 | Context7 `/dotnet/docs`, https://github.com/dotnet/docs/blob/main/docs/core/tools/dotnet-run.md, Arguments; live main-branch CLI documentation | Tokens following `--` are forwarded as application arguments. Supports `dotnet run --project ... -- info` without executing it. Context7 also returned `dotnet-package-add.md`: noun-first was introduced in .NET 10; this does not justify churn in otherwise valid module installation commands. |
| R7 | Context7 `/spectreconsole/spectre.console/0.49.1`, https://github.com/spectreconsole/spectre.console/blob/0.49.1/docs/input/prompts/text.md, Secrets / Masks | Secret prompts display masking characters (or hide input). The SDK sample itself uses `TextPrompt<string>` and normalized `string` copies; the docs do not promise erasure. Version 0.49.1 is explicitly referenced by ManagementTool's csproj HintPath; do not infer semantics from a different installed version. |

R3's relevant passages were read from the original full hardware-manual page. R5 was initially found
via search and subsequently fetched from the original API-reference URL; its remarks independently
confirm the F9 exception and shared-PIN reset. R2/R3 establish the family qualification. None of these
passages establishes generic Management-lock recovery. R4 was read in full but no porting or code
comparison was performed.

## Module ownership inventory — all 55 tracked paths

Inventory command: `git ls-files src/Management`. Split: **A 21, B 2, C 32**. This includes all **42 C#
files**: A 21, B 2, C's executable examples 19. C owns five Markdown files and eight other support
files. Delegation below is assignment coverage, not a claim that C audited A/B's whole semantic scope.
Paths in the table are relative to `src/Management/`.

### C-owned files (32)

| File | Disposition / semantic units |
| --- | --- |
| `README.md` | Read complete; task flow/security/lifetime units; C-01, C-14, C-15; current options examples otherwise supported. |
| `PACKAGE_README.md` | Read complete; clean descriptive scope, alpha status consistent with root, transitive Core statement and documentation links. |
| `CLAUDE.md` | Read complete; C-02–06, C-15, C-18; clean present-day transport resolution/borrow-own descriptions subject to boundary qualifications. |
| `tests/CLAUDE.md` | Read complete; C-02, C-04, C-05; xUnit v3 unit/v2 integration distinction confirmed in project references. |
| `examples/ManagementTool/README.md` | Read complete; C-01, C-09–11; double confirmation confirmed through shared helper. |
| `examples/ManagementTool/Program.cs` | Read complete, all comments/dispatch/menu/help; C-10, C-12, C-18. Monitoring shutdown in `finally` is clean. |
| `examples/ManagementTool/Cli/Menus/CapabilitiesMenu.cs` | Read complete; selection/display/replacement-mask/USB-minimum/confirmation/cleanup; C-09, C-12 dependency. Known-capability mapping and ordinary lock-array `finally` supported. |
| `examples/ManagementTool/Cli/Menus/DeviceFlagsMenu.cs` | Read complete; C-09, C-12, C-13; ordinary cleanup supported. |
| `examples/ManagementTool/Cli/Menus/DeviceInfoMenu.cs` | Read complete; C-01, C-13; metadata sections match fields, unsupported optional sections are conditional. |
| `examples/ManagementTool/Cli/Menus/LockCodeMenu.cs` | Read complete, all three mutations; C-01, C-09, C-11 dependencies. All returned non-null secret arrays are cleared in caller `finally` on ordinary method paths. |
| `examples/ManagementTool/Cli/Menus/ResetMenu.cs` | Read complete; C-01, C-09, C-12; confirmation precedes destructive SDK call. |
| `examples/ManagementTool/Cli/Menus/TimeoutsMenu.cs` | Read complete; C-09, C-12; displayed ranges match DeviceConfig setters; reads CR timeout from byte memory. |
| `examples/ManagementTool/Cli/Output/OutputHelpers.cs` | Read complete; delegated formatting/confirmation and known-bit formatting clean as descriptions. Unknown bits are not displayed, so no complete future-capability display guarantee. |
| `examples/ManagementTool/Cli/Prompts/DeviceSelector.cs` | Read complete; C-09, C-12; query scopes rely correctly on one-shot disposal. |
| `examples/ManagementTool/Cli/Prompts/LockCodePrompt.cs` | Read complete, including XML code; C-11; empty/invalid/mismatch return contracts and zero-code helper otherwise supported. |
| `examples/ManagementTool/Cli/Prompts/ManagementDeviceSelector.cs` | Read complete; own descriptions match subclass settings; not currently used by active local callers (C-09). |
| `examples/ManagementTool/Commands/ConfigCommand.cs` | Read complete; read-only command summary matches behavior; C-09, C-10 dependencies. |
| `examples/ManagementTool/Commands/InfoCommand.cs` | Read complete; C-09, C-10, C-13; lifetime correct. |
| `examples/ManagementTool/Features/DeviceConfiguration.cs` | Read complete, every helper/XML block; C-12–14; parameter ranges match setters and ordinary caller-memory responsibility retained. No UI dependency. |
| `examples/ManagementTool/Features/DeviceInfoQuery.cs` | Read complete; C-03 XML factory call, C-12 catch-all; success/failure result description otherwise supported. |
| `examples/ManagementTool/Features/DeviceReset.cs` | Read complete; C-01, C-12; firmware-only predicate not proof of actual support. |
| `examples/ManagementTool/Features/Results/ConfigResult.cs` | Read complete; C-13 reboot meaning; success/error factories otherwise match comments. |
| `examples/ManagementTool/Features/Results/DeviceInfoResult.cs` | Read complete; clean nullable-data/success/error model and factory descriptions. |
| `examples/ManagementTool/Features/Results/ResetResult.cs` | Read complete; clean success/error result descriptions, not an independent hardware guarantee. |
| `examples/ManagementTool/ManagementTool.csproj` | Read complete; support exclusion from prose editing except existing comments. Non-packable, refs include Core/Management/Cli.Shared/Cli.Commands. Explicit old Spectre HintPath is an implementation/build portability concern, not a descriptive metadata correction; no build attempted. |
| `src/Yubico.YubiKit.Management.csproj` | Read complete; clean description/tags/dependency and AOT opt-in explanation. No PIV or other applet dependency. |
| `src/PublicAPI.Shipped.txt` | Read complete; nullable header only; intentional alpha baseline support file, no consumer prose needed. |
| `src/PublicAPI.Unshipped.txt` | Read complete; five type families and 47 non-header entries. Generated record equality/clone/hash/operator entries and C#14 extension projections are support artifacts, not 47 separate documentation tasks. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/Yubico.YubiKit.Management.IntegrationTests.csproj` | Read complete; support exclusion; xUnit v2/VSTest, shared refs, runner copied; descriptive comments match configuration. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/xunit.runner.json` | Read complete; support exclusion; parallel execution disabled, one thread. This is configuration, not evidence that two separately launched runners cannot overlap. |
| `tests/Yubico.YubiKit.Management.UnitTests/Yubico.YubiKit.Management.UnitTests.csproj` | Read complete; support exclusion; xUnit v3/MTP opt-in and references match guide. |
| `tests/Yubico.YubiKit.Management.UnitTests/xunit.runner.json` | Read complete; schema-only support exclusion, no explanatory prose. |

### A-owned files (21; traced only where needed for C claims)

| Paths | Ownership disposition |
| --- | --- |
| `src/IManagementSession.cs`; `src/ManagementSession.cs`; `src/IYubiKeyExtensions.cs` | A owns API/XML and session behavior; C read session/extensions and reset interface example dependencies. |
| `src/Backend/FidoHidBackend.cs`; `src/Backend/IManagementBackend.cs`; `src/Backend/OtpBackend.cs`; `src/Backend/SmartCardBackend.cs` | A owns all backend comments/wire/lifecycle; C read interface and used session dispatch for guide claims, not a duplicate backend audit. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/IntegrationTestBase.cs` | A-owned test support C#. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/ManagementHidConcurrencyTests.cs` | A owns current Transport/ConnectionType and assertion implications. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/ManagementLockCodeTests.cs` | A owns test safety/proof; B may use as lock evidence. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionAdvancedTests.cs` | A owns filter/test commentary; C's guide C-04 is related. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionCapabilityTests.cs` | A-owned; C read complete methods for C-02 proof limits. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionSimpleTests.cs` | A-owned; C names class for corrected command, not executed. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionTests.cs` | A-owned. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/ReconfigurationDiscoveryInvariantTests.cs` | A-owned reboot/recovery evidence and all assertion claims. |
| `tests/Yubico.YubiKit.Management.UnitTests/AuditV2/ManagementAuditReproTests.cs` | A-owned test explanations; B uses serialization evidence. |
| `tests/Yubico.YubiKit.Management.UnitTests/FirmwareVersionTests.cs` | A-owned. |
| `tests/Yubico.YubiKit.Management.UnitTests/IYubiKeyExtensionsTransportTests.cs` | A-owned; phase-residue handoff C-18. |
| `tests/Yubico.YubiKit.Management.UnitTests/ManagementBackendLifecycleTests.cs` | A-owned; its non-disposable claim must not be enlarged into guide architectural guarantees. |
| `tests/Yubico.YubiKit.Management.UnitTests/ManagementSessionExchangeOverlapTests.cs` | A-owned. |
| `tests/Yubico.YubiKit.Management.UnitTests/ManagementSessionTests.cs` | A-owned reset/zeroing/disposal evidence. |

### B-owned files (2)

`src/DeviceConfig.cs` and `src/SetDeviceConfigOptions.cs`: B owns full XML/wire/validation/lock semantics.
C read DeviceConfig for README and sample range/serialization claims and used options signatures from
the public snapshot/session source. C-14 is an affected-documentation dependency, not an independent
second configuration-model audit.

### API/support reconciliation

The five effective public type families are `DeviceConfig`, `SetDeviceConfigOptions`,
`IManagementSession`, `ManagementSession`, and static `IYubiKeyExtensions`. Their meaningful operations
are config properties/Validate, config-operation policy and borrowed lock memory, read/set/reset, direct
session creation, and three device extensions. Inherited session lifecycle/ConnectionType/FirmwareVersion
come from Core. Internal backends and `DeviceConfig.GetBytes` are not new consumer APIs; example-public
classes belong to a non-packable executable. Generated record members and duplicate compiler projections
of C#14 extension members remain API-preservation checks, not invitations to author boilerplate prose.

## Clean semantic units available for sampling

These are affirmative inspected units, not whole-file clean stamps that erase nearby findings:

1. **C-CLEAN-01:** Package README scope/alpha/transitive dependency/link section (`PACKAGE_README.md:1-25`).
2. **C-CLEAN-02:** Public README query-capability fragment and supported/enabled distinction (`README.md:63-72`),
   qualified by optional older-firmware metadata behavior owned by A/Core.
3. **C-CLEAN-03:** Public README random lock-code mutable-buffer `try/finally` (`README.md:97-111`): actual caller bytes
   are cleared; secret-store persistence is explicitly required before writing.
4. **C-CLEAN-04:** Public README effective-version/HID reset rejection distinction (`README.md:129-134`); missing family
   context is C-01, not a defect in the stated SDK rejection/propagation distinction.
5. **C-CLEAN-05:** Backend borrow/own versus one-shot session lifetime explanation (`CLAUDE.md:313-338`) apart from the
   global grouping/finalizer bounds; actual extension uses `OwnConnection`.
6. **C-CLEAN-06:** Capabilities menu ordinary successful/error/confirmation-cancel cleanup region (`:113-163`).
7. **C-CLEAN-07:** Lock-code menu separate set/change/remove caller cleanup regions (`:99-137,146-196,205-242`).
8. **C-CLEAN-08:** Output helper forwarding and double confirmation (`OutputHelpers.cs:30-77`, shared
   `ConfirmationPrompts.cs:41-56`): boolean confirmation then exact word comparison, not a touch ceremony.
9. **C-CLEAN-09:** DeviceInfoResult and ResetResult documented factories/properties (complete files).
10. **C-CLEAN-10:** Timeout helper XML numeric ranges (`DeviceConfiguration.cs:88-104`) versus DeviceConfig setter
    limits; this does not independently assert all devices implement every timeout.
11. **C-CLEAN-11:** Standalone `ManagementDeviceSelector` description/settings (complete file), distinct from actual
    unused routing status.
12. **C-CLEAN-12:** Library description/tags/Core dependency/AOT opt-in (`Yubico.YubiKit.Management.csproj:8-17`).

Sample across narrative, XML, and internal comments; at least three units are available without relying
on a single category. No clean claim above asserts a hardware test result.

## Complete snippet classification — module Markdown and C-owned XML

Classes: **complete** means a usable program/command after stated installation/working-directory
prerequisites; **fragment** means typed values/imports/lifetime from the surrounding task are required;
**sketch** means application/test/internal scaffolding is intentionally omitted; **historical** means
an explicitly contrasting old/bad alternative, not a current copy-and-run recipe. A correctness finding
can apply to any class. No snippet was compiled or executed by C.

### All module Markdown fenced blocks (49)

| File and baseline lines | Classification / result |
| --- | --- |
| `README.md:32-35` | Complete installation commands inside an existing consumer project; feed not queried by C. |
| `README.md:41-55` | Complete top-level read example with explicit imports/device/session. Treat it as its own lifetime, not a prefix retaining `session` across later independent tasks. |
| `README.md:65-70` | Fragment: imports + device; one-shot query. |
| `README.md:76-87` | Fragment: imports + device; scoped rebooting session then forced discovery. Delay is illustrative, not readiness proof; C-02/C-14 context. |
| `README.md:97-111` | Fragment: imports + device; lock-store application responsibility; clean mutable-buffer cleanup. |
| `README.md:121-127` | Fragment: imports + device + explicit destructive authorization; SDK transport selection correct, family context C-01. |
| `README.md:185-187` | Complete command from repository root; starts executable/hardware-facing UI, not run here. |
| `PACKAGE_README.md:15-18` | Complete installation commands in consumer-project context. |
| `CLAUDE.md:39-61` | Test sketch with empty body; filtering supported, per-matching-device comments C-04. |
| `CLAUDE.md:67-78` | Contrasting current test sketches, not historical APIs; row count claims C-04. |
| `CLAUDE.md:84-94` | Test sketch with multiple data attributes; one row per attribute, not union/dedup C-04. |
| `CLAUDE.md:100-119` | Test-context fragment; nullable serial C-03. |
| `CLAUDE.md:127-144` | Internal source sketch explaining extension syntax; method body matches one-shot flow, supporting imports required. |
| `CLAUDE.md:160-168` | Two alternative fragments, not one compilable block (duplicate local); implicit device/token, SmartCard equivalence C-03. |
| `CLAUDE.md:186-206` | Two alternative mutation fragments; implicit lockCode/reboot/token and SmartCard limitation C-03. |
| `CLAUDE.md:224-240` | Fragment: device/customProtocolConfig/token/SCP imports and key-parameter lifetime; no standalone construction story. |
| `CLAUDE.md:273-281` | Before/after alternatives; both APIs current. Historical anti-pattern is an ergonomic choice, not invalid API use (C-18). |
| `CLAUDE.md:285-296` | Before/after alternatives with duplicate locals; use current versions independently; recommend async disposal. |
| `CLAUDE.md:300-309` | Before/after alternatives; both use removed reboot parameter, including recommended version (C-03). |
| `CLAUDE.md:344-357` | Three alternative test fragments; helper cached-origin and preferred pattern C-04. |
| `CLAUDE.md:365-382` | Internal helper-signature sketch, not compilable empty async implementation; incorrect current signature/origin C-04. |
| `CLAUDE.md:386-398` | Test sketch; callback cache explanation C-04, serial equality only. |
| `CLAUDE.md:404-416` | Test sketch; read + assertions, no broad hardware qualification implied. |
| `CLAUDE.md:420-433` | Test sketch; non-null qualifier assertion does not establish 5.7-only functionality; qualifier is required/default-populated in Core. Do not present it as a meaningful modern-feature demonstration. |
| `CLAUDE.md:437-453` | Test sketch; form factor/firmware assertions, not a biometric ceremony. |
| `CLAUDE.md:457-474` | Test sketch; checks enabled PIV on either transport, matching actual HasCapability meaning. |
| `CLAUDE.md:478-504` | Two test sketches; cached filters + fresh capability-bit assertions, not certification. |
| `CLAUDE.md:508-530` | Test sketch; unsupported USB-enabled inference C-04. |
| `CLAUDE.md:538-550` | Explicit negative/historical anti-pattern; obsolete SDK signature still should be recognizable/current if retained (C-02/C-03). |
| `CLAUDE.md:570-603` | Application-dependent destructive restoration sketch falsely offered as safe; C-02/C-03. |
| `CLAUDE.md:611-623` | Test fragment; cache origin C-04; fresh-read equality is all the assertion proves. |
| `CLAUDE.md:629-639` | Test fragment; sequential calls share session; clean reuse illustration. |
| `CLAUDE.md:645-659` | Model-definition sketch plus usage fragment, deliberately elides required fields; not a second actual type declaration to compile with SDK. |
| `CLAUDE.md:663-677` | Config mutation fragment: session/token supplied; ranges/TouchEject relationship need task conditions, options syntax current. |
| `CLAUDE.md:681-697` | Self-contained in-method capability-flag fragment after imports; checks/add/remove semantics correct. |
| `CLAUDE.md:703-715` | Firmware-display fragment; invented qualifier names/format C-06. |
| `CLAUDE.md:719-736` | Internal feature sketch; policy mismatch C-06. |
| `CLAUDE.md:740-752` | Current internal factory source sketch; requires class/base/private initializer context; not public consumer code. |
| `CLAUDE.md:767-777` | Internal backend-interface sketch; shape matches interface, surrounding zero-branching prose does not. |
| `CLAUDE.md:801-806` | Explicit historical before-code; private protocol fields are not current APIs. |
| `CLAUDE.md:809-811` | Internal after-code fragment; valid write delegation only. |
| `CLAUDE.md:817-839` | Obsolete internal read sketch C-06; not a valid historical contrast as currently labeled. |
| `CLAUDE.md:845-856` | Logging/session fragment with implicit borrowed connection; logger-factory ownership/import/package prerequisites need stating. |
| `CLAUDE.md:860-873` | Discovery/query fragment with implicit token/imports; named first arg then ordered positional token is not itself a stale Management overload. |
| `tests/CLAUDE.md:20-33` | Test sketch; independent filter criteria, empty body. |
| `tests/CLAUDE.md:39-51` | Test sketch; callback state-cache origin C-04. |
| `tests/CLAUDE.md:65-77` | Complete intended commands, incorrect project/integration selection C-05. |
| `ManagementTool/README.md:21-47` | Non-executable directory-tree illustration; incomplete current structure C-10. |
| `ManagementTool/README.md:51-54` | Complete repository-root run command; no hardware execution here. |

### All C-owned XML code blocks (5)

| File / lines | Classification / disposition |
| --- | --- |
| `Features/DeviceConfiguration.cs:46-56` | Fragment with `IManagementSession session` and `CancellationToken ct`; calls the **sample wrapper**, whose `reboot:` parameter still exists. Do not replace this legitimate named argument merely because SDK overloads changed. |
| `Features/DeviceConfiguration.cs:207-221` | Application-dependent secret acquisition sketch (`GetCurrentCode`, `GetNewCode`, session/ct/imports); mutable arrays cleared in finally; helper methods must be supplied, no implementation inferred. |
| `Features/DeviceInfoQuery.cs:36-49` | Fragment with device/token/connection imports; stale positional factory token C-03. |
| `Features/DeviceReset.cs:74-99` | Fragment requiring an already-open supported SmartCard session and explicit destructive authorization; missing family/transport context C-01; does not itself solicit confirmation. |
| `Cli/Prompts/LockCodePrompt.cs:25-41` | Fragment illustrating returned-byte ownership and cancellation return; correct finally for a returned buffer, not proof that prompt-internal strings/failure paths are erased (C-11). |

A/B-owned XML blocks remain their ownership: notably DeviceConfig's mutation fragment and reset interface
example. Do not omit them from final compiled-example inventory merely because they are not repeated here.

## External coverage manifest

Search was not limited to the word Management. Tracked Markdown/C#/YAML/project searches included
`ManagementSession`, `CreateManagementSession`, `GetDeviceInfoAsync`, `SetDeviceConfig`,
`ResetDeviceAsync`, `UsbEnabled`, `NfcRestricted`, configuration-lock, enabled-capability, touch-eject,
device-configuration and firmware-override terminology. Full blocks containing discovered calls were
read, including implicit `mgmt` lifetimes and generic test callbacks. Generic PIV/HSM management-key
and CTAP credential-management hits were separated from device Management.

### Active current-reference documentation

| Paths / semantic units read | Disposition |
| --- | --- |
| Root `README.md` complete; Management package row and discovery/session task context | Clean Management scope/link. Its current getting-started block is Core discovery, not a Management example. |
| Root `CLAUDE.md` complete | C-03 factory example and C-15 finalizer boundary; generic modern session statement also should not call current ManagementSession generic. Root architecture says `ManagementSession<TConnection>` although current type is non-generic. |
| `docs/migration/v1-to-v2.md` Management contract rows `:105-116,125,140-141`, complete Device Info v1/v2 blocks `:250-283`, session-creation context | Current options/ConnectionType/Task/removal mappings supported; V1 block historical, V2 block composable with supplied token. No live session overlap in its Management block. |
| `docs/migration/v1-to-v2-map.yml` Management/package/device-info/serial recipe entries | Current mappings supported at semantic level; chained `Create...(...).Get...(...)` field is conceptual recipe text, not awaitable C# to copy literally. |
| `docs/TESTING.md:257-378` complete multi-transport section/blocks; authorization policy context | C-07 removed Transport and ambiguous “tries”; useful filter-versus-pin distinction preserved. Classified blocks: empty row declarations = sketches; default/pinned helper bodies = test fragments; DeviceId parsing old block = historical negative example. |
| `docs/architecture/physical-device-model.md` complete | C-08 overlapping session block; C-15 boundary/finalizer wording. Metadata fragment supported; discovery and typed-connection alternatives use bounded scopes. “Mode changes” at :149 describes module ownership, not a public set-mode method; do not turn it into an API claim. |
| `docs/architecture/connection-ownership-and-contention.md` complete | C-07 property, C-15 finalizer. Initial PIV-then-Management conflict is explicitly historical before-enforcement evidence, not a current successful recipe; sequential PIV/OATH block has correct scopes. |
| `docs/architecture/applet-public-api.md` complete | Clean current common options/effective version/borrowed bytes/alpha snapshots; no Management-specific correction. No consumer executable blocks. |
| `docs/architecture/sdk-architecture-diagrams.md` Management L1/L2/L4 sections and full relevant diagram/teaching blocks; `sdk-architecture-map.yml:47-82` | Core dependency and three-transport order supported. “a SmartCard impl and a Hid impl” at :172-173 compresses Management's two distinct HID backends; qualify if editing that explanation. Graphs are illustrative non-executable material, not APIs. |
| `docs/architecture/device-identity.md` Management/effective-firmware/cache portions; `device-discovery-guarantees.md` reconfiguration and version portions | Clean separation between metadata and identity, and documented bounds. Named historical 37,000-scan result is not newly rerun or inherited as this audit's evidence. Core-wide assertions remain deferred. |
| `docs/NATIVE-AOT.md` complete; `docs/v2-highlights.md` common session/package/AOT sections | Management AOT claim correctly limited to recorded macOS device-information evidence. No new device or platform verification. Unrelated native-library deployment wording in highlights is outside the Management claim; canonical AOT document governs. |
| `docs/LOGGING.md` complete | Management category supported by actual session type. No Management invocation in its current code blocks. Broader logging-sensitive-payload assurances require separate Core audit; do not certify them through this review. |
| `scripts/alpha/RELEASE_NOTES.md` complete | Management package row and alpha scope consistent. General feed/version/restore policy not independently network-qualified. |
| `TOOLCHAIN.md` Management project-list references; test target source | Inventory/command context only; no Management behavior example. |
| `src/Core/README.md` complete; `src/Core/CLAUDE.md` complete | Core metadata/borrowed direct factory contract supports Management journeys. Raw-session block relies on a **previously obtained** `firmwareVersion`; do not open Management while raw session is live to fill it. Core finalizer absolutes need C-15 reconciliation; broader Core changes deferred. |
| `src/Tests.Shared/README.md` complete | C-04 wrong positional SCP helper argument; basic/read/custom-filter Management examples are test fragments, not evidence that every matching key runs. `WithOathAsync` is a helper sketch, not a Management mutation. |
| `src/Tests.Shared/CLAUDE.md` complete | C-04 one-case-per-matching-device limitation contradicts actual placeholder binding and README; correct narrowly under `MGMT-XREF-003`. Existing common helper implementation sketch forwards `state.DeviceInfo`, consistent with the C-04 correction. Generic cache/allow-list pseudo-implementations are internal explanatory sketches, not new consumer APIs. |
| `src/Piv/examples/PivTool/README.md` device-info API-list context | Current `ManagementSession.CreateAsync` API mention supported; no complete Management code block. |
| `src/Piv/examples/PivTool/SDK_PAIN_POINTS.md` complete | Active development-notes document, not immutable evidence; C-17 current resolution block needs correction. Other PIV examples excluded from Management semantics. |
| `src/SecurityDomain/README.md:133-168` complete constraints/security/related context | Management as firmware source supported. Add the practical responsibility to close Management before opening Security Domain if expanding this task; no overlapping session example currently exists. |
| `src/OpenPgp/README.md:212`; `src/YubiOtp/README.md:190` related links | Clean references to enabling the corresponding application; no Management executable block. |
| `.claude/skills/domain-api-design-standards/SKILL.md:135` module-file table | Support/tooling instruction hit, not consumer sample; path uses obsolete `Yubico.YubiKit.Management/src/...` layout. Cross-module instruction-maintenance dependency; no skill edited in this run. |

### Current source examples, shared CLI, and explanatory comments outside Management

| Paths / complete relevant semantic blocks | Disposition / snippet class |
| --- | --- |
| `src/Cli.Commands/src/Management/{ManagementInfoCommand,ManagementConfigCommand,ManagementResetCommand}.cs` complete | Executable command examples. Info/config read a snapshot, lifetime supported; reset family/transport C-01 and timeout-summary C-13. |
| `src/Cli.Commands/src/Infrastructure/{YkCommandBase,YkDeviceContext}.cs` complete | C-16 enrichment comments; default Management query is a one-shot operation completed before applet command. Executable flow, not a retained session example. |
| `src/Cli.Shared/src/Device/DeviceSelectorBase.cs` complete | One-shot metadata helper and parallel per-device query blocks; ownership supported; catch-all cancellation shared dependency C-12. Its transport-filter cref names a different overload than the actual filter call; optional XML precision follow-up, not a Management behavior claim. |
| `src/Cli.Shared/src/Output/ConfirmationPrompts.cs` complete | Actual double-confirmation behavior confirmed. Generic dangerous-operation UI says “cannot be undone” even for reversible config changes; executable shared wording deferred, not a policy about actual irreversible data loss. |
| `src/Ctap2/examples/FidoTool/Cli/Prompts/DeviceSelector.cs` complete | Executable metadata query helper/whole selection flow, named cancellation syntax current, no retained Management lease; cancellation catch C-12. |
| `src/Oath/examples/OathTool/Cli/Prompts/DeviceSelector.cs` complete | Executable query + selection flow, current named token, non-cancellation catch and prompt cancellation present; clean Management-lifetime unit. |
| `src/OpenPgp/examples/OpenPgpTool/Cli/Prompts/{DeviceSelector,OpenPgpDeviceSelector}.cs` complete | Executable default Management metadata query before OpenPGP; no unsupported claim that the Management query itself requires CCID. Catch-all C-12 dependency. |
| `src/Piv/examples/PivTool/Cli/Commands/DeviceHelper.cs` complete; `CliRunner.cs:114-155` complete HandleDeviceInfo body with surrounding dispatch; `Cli/Menus/DeviceInfoMenu.cs` complete; `Cli/Prompts/DeviceSelector.cs` complete | Device-info one-shot read completes before PIV session; explicit SmartCard selector creates/disposes both borrowed connection and session. Clean lifetime/API usage; catch-all metadata errors C-12 dependency. PIV credential handlers outside these blocks excluded. |
| `src/Piv/examples/PivTool/Cli/Commands/JsonOutput.cs` DeviceInfo capability serialization region | DeviceInfo data formatting only, no session API or mutation; forward-format handling outside selected scope. |
| `src/YubiHsm/examples/HsmAuthTool/Cli/Prompts/DeviceSelector.cs` complete | Current one-shot metadata query, no held Management scope; catch-all C-12 dependency. |
| `src/YubiOtp/examples/OtpTool/DeviceHelper.cs` complete | Current metadata-query lifetime supported; XML :50-54 still says this selector deduplicates per-transport rows by serial although it only filters current grouped IYubiKeys. Candidate cross-module comment correction; no dedup runtime change authorized. Catch-all C-12 dependency. |
| `src/Tests.Shared/YubiKeyTestStateExtensions.cs` complete | C-04 two stale XML test sketches; actual async/sync helper implementations and callback origin traced. |
| `src/Tests.Shared/{YubiKeyTestState.cs,Infrastructure/WithYubiKeyAttribute.cs,Infrastructure/YubiKeyTestInfrastructure.cs}` placeholder binding/filter units | Narrow evidence dependencies, not full shared-infrastructure audit. One row/first matching device and USB-or-NFC capability predicate traced. |
| `src/Tests.TestProject/{Program.cs,Controllers/DiTestController.cs}` complete Management endpoint bodies | Application-host-dependent examples, current named token and one-shot lifetime supported; C-16 XML and deferred returned diagnostic strings. ASP.NET-wide behavior not audited. |
| `benchmarks/Yubico.YubiKit.PerformanceBenchmarks/Program.cs:144-249` complete Management benchmark classes with base fixture context | Executable benchmark fragments within real host; concrete transport preferences and bounded scopes current. They measure connect/select/create/read units, not a claim of cancellation or comprehensive performance. Not run. |
| `verification/MacOSHidRouteVerification/{AcceptanceScenarios.cs:196-217,ExpertFeatureScenario.cs:29-47,SmartCardScenario.cs:53-85}` complete Management operation loops/blocks and ownership context | Executable verification scenarios; explicit route/serial assertions and borrowed-session scopes supported. Historical PASS output in records is separate evidence; no host was run. |
| `verification/NativeAotVerification/Program.cs` rooted `typeof(ManagementSession)` entry | Link-root support exclusion: naming a type does not exercise a Management operation. |
| `src/PublicApi/tests/.../{FactoryShapeTests,AppletSessionShapeTests,AsyncSurfaceConventionTests}.cs` Management names/surface registries | Cross-module convention-test support inventory, A/common-contract dependency. Not consumer samples or proof of hardware behavior. |
| `src/Piv/tests/.../{PivSessionContentionTests,PivMultiKeyContentionTests}.cs` Management conflict/sequential-use references | A/Core evidence dependency, not a second test-body audit by C. Historical bad sequence must stay labeled historical; no execution claimed. |

### Historical, generated, and out-of-scope dispositions

- **Immutable campaign history:** all `docs/research/documentation-audit/records/**`, including OATH/
  WebAuthn investigations, edits/reviews, serialized pre-edit content, checkpoint source copies, hashes,
  and test result JSON. Search hits are provenance, not current instructions to repair. Do not modify.
- **Historical source snapshots:** `docs/migration/v1-to-v2-gaps.md` explicitly identifies its baseline
  and says it is point-in-time. Its Management Builder/legacy-mode/field-gate observations must not be
  rewritten into current guide text. Current public limitations go in the active migration guide and
  module README, with B/A evidence. Preserve the snapshot.
- **Historical AOT evidence:** `docs/research/native-aot-readiness{,-data}.md` identify their assessment
  date and canonical current `docs/NATIVE-AOT.md`. Old DI/interop snippets there are historical, not
  present-day Management examples. No revalidation of their old hardware output performed.
- **Original implementation plans:** `docs/plans/{single-connection-single-session,raw-access-tiers,
  2026-09-04-applet-public-api-consolidation}.md` and async-boundary earlier addendum retain original
  pseudocode/proposals. Their local snippets are internal design sketches, not current public recipes.
  Current architecture/migration is audited above. Do not “fix” historical proposed APIs into evidence
  that they shipped. The consolidation plan's “Ready for implementation” header is stale lifecycle
  metadata relative to the active architecture; optional status annotation belongs to plan ownership.
- **Active async program:** `docs/plans/yubikit-async-boundaries/03-program-design.md` current-status,
  operation-evidence, and Management overlap references read narrowly. Recorded D1–D5 scope is bounded
  historical macOS evidence, not all-platform Management qualification. Earlier multi-key proposal is
  explicitly deferred history.
- **Inventory/troubleshooting history:** `docs/docs-inventory-report.md` labels itself a historical
  snapshot; `docs/troubleshooting/testdiscovery-findings.md` preserves dated sessions and forbids deleting
  old entries. Old discovery descriptions are not authority over the current placeholder implementation.
- **Generated diagrams:** `docs/architecture/images/L{1-assembly-deps,4-connection}.svg` are generated
  presentation outputs; source diagram/graph inputs were assessed. Do not edit generated SVGs by hand.
- **Generated build output:** all example `obj`/`bin` files are untracked/generated and excluded. Their
  presence does not establish that baseline examples compile now.
- **Generic terminology false positives:** PIV management-key authentication, YubiHSM management-key
  operations, CTAP credential management, and general memory-management documents are not this module's
  control surface. Sibling full applet audits and Core-wide protocol/discovery review remain deferred.

## Handoff dependencies and verification still needed

| Candidate group | Dependencies / later re-review trigger |
| --- | --- |
| C-01 reset and lost-code recovery | A firmware/transport/error findings; primary family references; any future support-predicate change must reopen public README/table, sample XML/readme/UI, lock warning, shared reset command. |
| C-02/C-04/C-05 test guidance | A assertion/cleanup/test-row findings; actual helper and placeholder binding; editor must not present persisted readback as transport/application effect or turn best-effort finally into guaranteed restoration. |
| C-03/C-06/C-07/C-08 current snippets | Compile-only validation with explicit typed prerequisites and current references; separate alternatives and scopes. Backend/internal sketches are source-review checks, not consumer compile programs. |
| C-09/C-10 sample reachability | Deferred selector/menu/help behavior fix, focused no-hardware evidence, no hardware change in Pass 1. Comment/prose edits must continue to disclose present limits. |
| C-11/C-12 sample security/cancellation | Deferred executable input/cleanup/forwarding fixes; shared CLI and sibling catch patterns are cross-module dependencies. Re-review assurances only after failure-path evidence exists. |
| C-13 result/display semantics | Product decision on reboot result naming/meaning; display classification versus FIPS approval; defer executable string changes. |
| C-14 config task completeness | B's lock zero-value, supported masks, NFC 5.7+ and encoding evidence; A overall feature gates. No field gate or validation change authorized. |
| C-15 ownership qualifications | Core grouping/process/finalizer source; do not infer a process-global registry controls other processes or arbitrary direct raw-factory opens. |
| C-16/C-17 shared samples | Current one-shot and borrowed-session contracts; do not repair examples with renamed APIs while preserving simultaneous leases. |
| C-18 editorial cleanup | Preserve useful rationale and conditions; optimization for comprehension, not a guide-length target. |
| C-19 known Core metadata defects | `MGMT-DOC-010`, `MGMT-XREF-001/002`; apply narrow consumer limitations now, do not claim decoded FIPS masks prove approval or flagged form-factor equality is normalized. Re-review after authoritative decoding fixtures and implementation changes. |

Before documentation closeout, the editor/reviewer must independently recheck consequential product
passages, compile meaningful current consumer snippets, and reconcile A/B findings against this external
manifest. This investigation deliberately supplies no build/test/hardware success claim. Executable
sample defects remain deferred even when their comments can be made truthful now.
