# Management investigation A — session, transports, and tests

## Scope, baseline, and evidence limits

- Investigator: GPT-6.1 Sol (`github-copilot/gpt-6.1-sol`), 2026-11-03.
- Source baseline verified with `git rev-parse HEAD`: `da61d2e6d754b4c155cbb2579202e910986f4b3a`.
- Assignment: seven session/interface/extension/backend source files and all fourteen tracked module test C# files. All 21 owned files were read completely, including helpers and assertions. `DeviceConfig.cs` and `SetDeviceConfigOptions.cs` were read as dependencies; B owns their dispositions/edits. C owns Markdown, package/support inventory, and examples.
- Read root `CLAUDE.md`, Management `README.md` and `CLAUDE.md`, `src/Management/tests/CLAUDE.md`, `docs/TESTING.md`, campaign workflow/style, and this run's plan. The requested root `tests/CLAUDE.md` does not exist; the module-local test instructions are the applicable file. Reviewed the available `.claude/skills/` inventory. No build/test/commit skill execution was applicable: this assignment prohibits those actions. The execution-plan skill was consulted; this report completes only investigation, not the plan's implementation/integration phases.
- Narrow dependencies were read after Core and Tests.Shared instructions (and Tests.Shared README). No Core-wide audit is claimed.
- Working tree already contained coordinator changes to campaign README/workflow, new Management records, and the preserved untracked research files and `mise.toml`. None were modified by this investigator. The only write is this report.
- Verification here is static source/assertion/citation review. No build, test, hardware operation, experiment, commit, or delegation was performed. A test's presence is not an executed result. All candidates below need coordinator triage and fresh independent review.

## Contract map established from source

1. The extension selects exactly one exposed interface, in order SmartCard → HID FIDO → HID OTP. An explicit valid preference wins; SCP without a preference forces SmartCard. Opening/initialization errors do not cause another interface to be tried (`IYubiKeyExtensions.cs:105–133`; Core `YubiKeyConnectionExtensions.cs:180–220,246–303`).
2. Direct `ManagementSession.CreateAsync` validates the supplied connection/options and borrows the connection. The extension opens a connection and calls `OwnConnection`; its returned session owns that hidden connection. A failed direct factory cleans up session/protocol state without disposing a borrowed connection; the extension helper disposes a connection it opened when creation fails (Core `ApplicationSession.cs:247–305,399–419,538–565`).
3. Core prepares the protocol and rejects SCP on HID before initialization I/O. FIDO channel initialization belongs to Core; Management reads the prepared channel's version. SmartCard SELECT and OTP status are explicit probes. A DeviceInfo read is attempted before optional SCP establishment; non-cancellation failure falls back to the probe version (`ManagementSession.cs:79–112,176–194`; Core `ApplicationSession.cs:323–397`). An SCP session therefore does not imply that every initialization metadata exchange was protected by SCP.
4. Effective `session.FirmwareVersion` is the override or resolved version. `_version`, passed to subsequent metadata reads, remains the discovered version. Overrides configure protocols and feature checks; they do not rewrite reported hardware metadata. Factory discovery/probes still run with an override.
5. Writes validate firmware (5.0.0), arguments, and encoded configuration, forward one backend write, then clear the encoded buffer in `finally`. Lock/unlock is per command, not a persistent session authentication state. The caller retains lock-code buffers.
6. Reset validates disposal, effective firmware (5.6.0; major-zero development versions bypass), then dispatches only to SmartCard. SmartCard sends INS `1F`, P1/P2 zero, without data. HID is rejected without reset I/O. Device rejection propagates through the APDU layer; no product-family check or reset-success session invalidation is present.
7. DeviceInfo reads delegate directly to Core, not the Management backend. Core reads pages individually and parses their TLVs. The guard protects each exchange, not an entire multi-page public read. Calls must be awaited/sequenced; overlapping exchanges are refused rather than queued. Disposal drains an admitted exchange, not an indivisible multi-exchange public operation.
8. SmartCard's admitted exchange uses `CancellationToken.None`; cancellation is checked before admission, not used to abort the exchange or roll back device mutation. HID also protects framing, but its Core protocols contain transport-specific cancellation signaling during device waits; do not generalize SmartCard behavior into a universal HID guarantee.

## Numbered candidates

### A-01 — Missing public session contracts and incomplete extension responsibilities

**Status:** candidate documentation change. **Priority:** high usefulness; no runtime change.

**Observed evidence:** `IManagementSession.cs:20–27` has no type/get-info/set-config XML. `ManagementSession.cs:30,67–77,115–125` has no type/factory/get-info/write XML; only reset inherits interface documentation. Extension class remarks (`IYubiKeyExtensions.cs:23–31`) are generic utility prose; get/write summaries omit their temporary connection/session lifetime. The create extension documents caller disposal but not explicitly the owned hidden connection, no fallback after failure, or option errors. Public API snapshot `src/Management/src/PublicAPI.Unshipped.txt:20–32,46–49` confirms these are public families, not inaccessible helpers.

**Desired vs observed:** a reader of API reference should understand how to create/reuse/dispose the session, what is read/changed, and which errors/conditions apply without reconstructing Core. Currently most of this is only in the README or not stated.

**Proposed correction:** document both public types; direct-factory borrowing, supported typed connections, direct preference as an assertion rather than transport selection, caller disposal on success and failure, initialized return, SCP/override ordering, single-session connection constraint, and sequencing. Give get-info/write contracts on the interface and inherit them on concrete implementations. Describe fresh device queries (not a session metadata cache), optional serial/model fields, partial config/null omission, 16-byte lock codes, firmware gate, reboot consequences, caller zeroing, no rollback, disposal/overlap/device-error families. Explain that one-shot extensions create/dispose an owned temporary session/connection; the session-creating extension transfers that lifetime to its caller. Do not promise universal model support or universal exception translation.

**Dependencies/re-review:** interface, concrete session, all three extensions, README requirements/common operations/security/constraints, module contributor lifecycle sections, package API reference. Coordinate config specifics with B, reset with A-02, concurrency with A-03, and read-firmware enforcement with A-09.

### A-02 — Reset documentation needs product applicability and a narrower destruction statement

**Status:** candidate documentation correction plus consequential unknown. **Priority:** high (destructive operation/recovery advice).

**Observed evidence:** `IManagementSession.cs:30–46` says whole-device factory settings and erasure of credentials, keys, and configuration, conditioned mainly on firmware/transport. README `118–134` adds that firmware does not guarantee implementation, but still does not identify the supported product family. README `178` and module CLAUDE `877` suggest newer firmware provides a reset recovery path for a lost configuration lock. Source sends a bare device reset after only firmware/backend checks (`ManagementSession.cs:164–174`; `SmartCardBackend.cs:74–84`). The unit test verifies INS `1F` only, not hardware erasure (`ManagementSessionTests.cs:103–113`).

**Primary evidence:** R2 identifies this operation specifically as YubiKey Bio Multi-protocol Edition (MPE), resetting shared PIN and FIDO/PIV applications. R3 lists FIDO credentials/fingerprints/shared PIN and PIV keys/certificates/data, explicitly excepting the F9 attestation slot. R2's reset APDU is `00 1F 00 00`, with no payload. Neither focused passage establishes clearing the Management configuration lock or all device configuration. These are product/API documents, not a versioned ISO/FIDO normative rule.

**Desired vs observed:** consumers should know what supported hardware this destructive operation is for, rather than discovering it through a device rejection on an ordinary 5 Series key. A broad warning is appropriate, but must not promise erasure of every key/configuration value or imply lost-lock recovery without evidence.

**Proposed correction:** identify Bio MPE applicability and distinguish this operation from per-application reset on other keys. Retain the actual SDK firmware/transport gates, development-version allowance, override effect, and `ApduException` possibility. Explain application/shared-PIN destruction with appropriate attestation qualification or avoid claiming exhaustive erasure. Remove the implied general firmware-5.6+ configuration-lock recovery route unless separately verified.

**Deferred behavior:** do not add a family check, change feature thresholds, reset session state, or remap device errors in Pass 1. Whether successful reset invalidates/requires reopening the v2 session, and whether it clears Management configuration/lock on supported products, remain **consequential unknowns**. Initial official reset research plus focused Bio MPE/APDU/command-reference follow-up established family and application scope, not those two details. Coordinator should qualify/remove unsupported claims, not invent the missing guarantee.

**Dependencies/re-review:** interface reset XML and inherited concrete XML, README feature table/reset/security/constraints, module CLAUDE reset/gotchas, ManagementTool reset guidance (C), shared reset comparisons/migration. Reopen after any family gate, reset teardown policy, confirmed device erasure matrix, or firmware-threshold decision.

### A-03 — Expose sequencing and cancellation boundaries without promising public-operation atomicity

**Status:** candidate documentation change. **Priority:** medium/high.

**Evidence:** owned `ManagementSessionExchangeOverlapTests.cs:21–25,35–70` correctly limits its proof to a second call starting during an admitted page exchange. `ManagementHidConcurrencyTests.cs:24–37,59–89` correctly admits nondeterministic overlap and permits success/refusal. Core `DeviceInfoReader.cs:61–108` calls one page at a time; `ExchangeGuard.cs:49–62,111–127` rejects concurrent admission and substitutes `None`. `ApplicationSession.cs:42–44` explicitly lacks a public-operation lifetime admission lease. Public interface token prose only says “cancel the operation”; extension token prose is equally generic. README `140–141` already gives the useful SmartCard drain/no-rollback qualification but does not explain page boundaries or overlap refusal locally.

**Desired vs observed:** the reader must await each public operation and understand that cancel/dispose is not transaction rollback or guaranteed immediate interruption. Existing tests are more precise than API XML.

**Proposed correction:** add concise sequencing guidance to session remarks and document possible immediate `InvalidOperationException` for overlapping exchanges. Say a metadata read may use multiple exchanges; do not describe the whole operation as atomic or guaranteed drained on concurrent disposal. Keep cancellation transport-scoped: SmartCard pre-admission cancellation/drain and no rollback after configuration transmission. Link detailed Core lifetime/transport guidance rather than asserting HID cancellation is identical.

**Dependencies/re-review:** A-01 surfaces, README constraints/user interaction, `docs/architecture/connection-ownership-and-contention.md`, raw-access/lifetime guidance. Existing overlap and HID test explanations are clean and should not be weakened. Reopen for any operation-wide guard, cancellation signaling, or disposal policy change.

### A-04 — Test commentary falsely claims automatic execution on every matching device and fresh helper metadata

**Status:** candidate documentation correction; cross-module descriptive dependency. **Priority:** medium.

**Evidence:** integration `ManagementSessionTests.cs:27–31,129–147` claims ALL matching devices / once per device / both “all devices” sets. `ManagementSessionAdvancedTests.cs:24–45,418–428` claims nonexistent `[YubiKeyTheory]` and automatic all-device/per-transport execution. Actual `WithYubiKeyAttribute.GetData` yields exactly one placeholder per attribute (`Tests.Shared/.../WithYubiKeyAttribute.cs:156–174`); `YubiKeyTestState.BindToRealDevice` uses `candidates.FirstOrDefault()` (`315–346`). Multiple attributes bind independently, potentially to the same or different keys. `WithManagementAsync` passes cached `state.DeviceInfo`, not a new query at helper entry (`YubiKeyTestStateExtensions.cs:80–90`). Module CLAUDE `65–94,365–398,607–622` contains the same inaccurate explanations; C owns those files. Shared README `263` already states one placeholder per attribute; shared CLAUDE `273` contradicts it.

**Desired vs observed:** comments should describe actual row binding and cached discovery information. “All devices/transports” is not a verified coverage matrix.

**Proposed correction:** describe one row per attribute bound to a matching authorized device; explicitly distinguish filtering, pinning, and independent row identity. Describe helper `DeviceInfo` as the test state's cached metadata. Replace obsolete theory/type names in descriptive material. Do not rename tests or change row generation in Pass 1.

**Dependencies/re-review:** integration test comments listed above, module/test Markdown (C), shared helper XML examples and Tests.Shared CLAUDE known limitation, canonical `docs/TESTING.md` (already largely correct on this point). Reopen for harness row/binding/cache changes. No framework inference is needed: the repository's own `yield return` and `FirstOrDefault` establish the result.

### A-05 — Device filters are presented as actual transport evidence where tests do not pin/verify it

**Status:** candidate comment correction; deferred-pass-2 test coverage. **Priority:** medium.

**Evidence:** `ManagementSessionSimpleTests.cs:75–78` says Management over HID requires FIDO, yet OTP HID is supported. Its direct FIDO connection test (`81–89`) really opens FIDO; however one-shot tests `96–129` only assert `state.ConnectionType` then call the unpinned extension. On composite hardware these may run SmartCard. `ManagementSessionAdvancedTests.cs:396–414` likewise asserts the filter, not `mgmt.ConnectionType`; default SmartCard selection makes this work on ordinary composites but does not turn the attribute into a pin. Its final three-row test (`425–445`) does pin and assert each session: clean transport evidence, but rows need not share one physical device (A-04). Simple config tests and some capability/lock tests filter SmartCard while helper selection remains default.

**Desired vs observed:** explanatory text should distinguish a device exposing an interface from the operation actually exercising it.

**Proposed correction:** remove FIDO-only HID claim; describe the unpinned one-shot tests as reading a key that exposes the named interface using Management default selection. Preserve existing pin-and-assert explanations where accurate.

**Deferred behavior:** add explicit options/helper preference and assert actual connection type in future genuinely transport-specific tests. Changing calls/assertions/test names is out of this pass. Do not claim present HID coverage solely from test names or attribute rows.

**Dependencies/re-review:** simple/advanced test summaries, module/test transport examples (C), Tests.Shared helper pinning docs, any validation report claiming per-interface coverage. Reopen when pins/assertions or harness selection change.

### A-06 — Several test explanations claim more than the assertions establish

**Status:** candidate documentation corrections plus deferred assertion/coverage decisions. **Priority:** medium.

**Evidence and corrections:**

- Advanced `65–80` claims SCP03/SCP11 demonstration but supplies only `Scp03KeyParameters.Default`; document SCP03 only. Advanced `149–185` calls flag checks FIPS compliance / FIPS 140-2 assurance. These assertions compare reported PIV capability/approval bits; they do not validate cryptography, approved operation behavior, or a certification target. Describe only reported flags/filtering, avoiding an unverified FIPS edition claim.
- Advanced `231–239` says extended capabilities and biometric support where assertions only check nonzero USB flags / firmware. Advanced `365–393` calls this validation “across all properties” although assertions compare serial, firmware, and form factor, with local USB/NFC variables not asserted. Narrow prose to those checks.
- Simple `211–235` says incorrect SCP authentication fails but accepts **any** exception during session creation. Lock `61–65` similarly treats any exception as lock enforcement. Explain rejection is expected but the current broad assertion does not isolate its cause; use a precise error/status assertion in Pass 2. Connection, firmware discovery, and unrelated failures could satisfy these tests.
- Simple `133–157` claims changing **and restoring** AutoEjectTimeout are verified; only the changed value is read/asserted. Restoration is sent, not read/asserted, and not protected by `finally`. See A-07.
- Unit `ManagementSessionTests.cs:130–140` says disposal is “the only thing left” that can reject the calls. Its helper skips initialization; without the disposal guard, get-info would also see missing protocol. The exception-type assertion validly pins `ObjectDisposedException`, not elimination of every other failure. State that directly.
- Advanced performance test `343–363` asserts a one-read <1s threshold on the selected rig; it is not a general SDK latency guarantee. Preserve as a test threshold, not product performance documentation.
- `ReconfigurationDiscoveryInvariantTests.cs:29` says disabling an application reboots the key. In this test the explicitly requested `Reboot = true` causes reboot (`155–157`); other tests write with default false. Clarify actor/option. Its 37,083-scan historical validation paragraph (`39–44`) is a provenance claim not newly verified here; preserve only with a durable original evidence reference, or replace with the present test's timing-independent purpose. Assertions establish pairwise IDs and at most one serial-form holder in completed scans, not guaranteed observation of every transitional state.

**Desired:** comments describe exactly what the checked values prove. **Deferred:** stronger assertions, altered timeout budgets, new feature tests, and exact exception tests require Pass 2; no broad contract should be invented to justify the existing tests.

**Dependencies/re-review:** listed test comments, module CLAUDE FIPS/Bio examples and test claims, any coverage/hardware report (C). Metadata flag correctness additionally depends on A-13/A-14. Reopen when assertion scope or original historical evidence changes.

### A-07 — Configuration-restoration and secret-cleanup guarantees exceed actual failure paths

**Status:** candidate explanatory correction; deferred-pass-2 test reliability/security work. **Priority:** high for lock-code cleanup.

**Evidence:** simple config tests (`ManagementSessionSimpleTests.cs:139–183`) restore only after successful read/assertion, not in `finally`; neither verifies restored value. Lock tests (`ManagementLockCodeTests.cs:72–86,126–135`) place lock-code zeroing **after** awaited restoration (and, in the first, read/assert), so any restoration failure bypasses zeroing. The second does not read/assert the restored timeout or lock state. Their class summary (`22–25`) says original state is restored, which is an attempted teardown rather than an unconditional guarantee. Capability class summary (`22–25`) says “always restore” even though NFC teardown only sends a best-effort write and does not verify its effect (`118–123`). USB-capability test does attempt restore in `finally` and reads/asserts the result (`67–76`): stronger, but hardware failure can still prevent success.

**Desired vs observed:** teardown attempts should be described truthfully; sensitive local buffers should be cleared even when restoring the key fails.

**Proposed correction:** use “attempts to restore in finally” / “sends a restoration write” as appropriate, explicitly distinguish the two timeout tests' unprotected restoration. Do not promise that cleanup code guarantees recovery after a failed device write. Do not lower intended security requirements to make current cleanup appear adequate.

**Deferred behavior:** nested `try/finally` for zeroing irrespective of hardware cleanup; robust restoration and readback after assertions fail; preserve original failures while reporting restoration failures; test backend-write failure/cancellation zeroing (owned unit test only checks successful write). The session production write already clears its encoded buffer on failure via `finally`; this is not a production zeroing defect.

**Dependencies/re-review:** lock/simple/capability comments, module test-safety guidance (C), security examples/options XML (B), eventual device-state cleanup validation. Reopen after teardown or secret lifetime changes. No hardware remediation is authorized in this run.

### A-08 — Restricted NFC test remarks invent a touch requirement and unsupported restoration rationale

**Status:** candidate documentation correction; deferred behavior/verification unknown. **Priority:** medium.

**Evidence:** `ManagementSessionCapabilityTests.cs:85–90` says restricted NFC prevents all NFC response until the device is “physically touched via USB,” and says clearing the flag requires reboot, citing unspecified Java behavior. R4 instead says the NDEF URL remains readable and **USB power for three seconds** is sufficient; a charger is explicitly sufficient, so neither touch nor USB data communication is required. The test only checks `IsNfcRestricted` after a true write and sends the original bool back in `finally`; it does not test NFC communication, touch, activation, reboot, or restoration effectiveness.

**Proposed correction:** explain the actual USB-power activation condition and NDEF exception. Describe this test as writing/readback of reported restriction state, with attempted teardown. Remove uncited Java equivalence and unsupported “queued for next reboot” explanation (`118–120`). B should reconcile `DeviceConfig.NfcRestricted` guidance; C should reconcile user guidance.

**Deferred/unknown:** whether `NfcRestricted = false` is supported directly, ignored, or requires some other lifecycle action is not settled by R4 or this test. R5's pinned Python encoder emits the NFC TLV only for true; that corroborates a difference, not device intent. Initial primary manual research plus focused encoder/reference inspection did not establish false-wire semantics. Do not advertise false as a reliable immediate-unrestrict operation, nor recast a possible encoder/firmware issue as intended SDK policy.

**Dependencies/re-review:** capability-test remarks/teardown comments, DeviceConfig XML (B), README/ManagementTool NFC guidance (C), shared metadata property docs. Reopen after direct-false firmware qualification and exact restoration evidence.

### A-09 — Read-info firmware gate exists as a declaration but is not enforced

**Status:** deferred-pass-2 behavior candidate; documentation qualification. **Priority:** medium.

**Evidence:** `ManagementSession.cs:32–33` defines 4.1.0 `FeatureDeviceInfo`, but `GetDeviceInfoAsync:115–120` never calls `EnsureSupports`; factory also performs an unconditional DeviceInfo probe and can fall back. Write/reset explicitly enforce their gates. README `14,24–28,171` broadly associates supported minimums and firmware-gated `NotSupportedException`. R5 `read_device_info` explicitly requires `(4,1,0)`, corroboration only, not proof v2 must copy the sibling.

**Desired vs observed:** if 4.1.0 is a supported-minimum policy, public docs should state it as such; they must not promise a host-side `NotSupportedException` for this path while implementation can instead send a request and propagate device/parse failure.

**Proposed correction:** distinguish supported firmware minimum from SDK enforcement. In new XML, do not claim get-info implements the same firmware preflight as writes/reset.

**Deferred behavior:** decide whether to enforce the gate and how to handle development versions/overrides and initialization probes; add regression tests before changing behavior. Do not remove supported-minimum requirements merely because an unsupported call might reach firmware.

**Dependencies/re-review:** A-01 XML, README requirements/feature table/errors, contributor initialization explanation, tests on legacy firmware. Reopen when the enforcement/creation contract is decided.

### A-10 — Two-component minimum firmware silently disables a test's requested filter

**Status:** deferred-pass-2 test/harness candidate; candidate comment qualification. **Priority:** medium.

**Evidence:** `ManagementSessionTests.cs:134` uses `MinFirmware = "5.0"`, while comments `139–141` promise ≥5.0 matching. Core `FirmwareVersion.FromString:145–158` accepts exactly three components, and Tests.Shared `YubiKeyTestInfrastructure.FilterDevices:79–84` ignores an unparseable minimum. Thus the first row is unfiltered by firmware; the assertion at `147` can fail on an otherwise matching old non-USB-A key. The test was not run; this is a statically traced possible assertion failure. Similar two-component examples exist in shared helper/attribute XML (A-04/C dependency).

**Proposed correction:** do not repeat the promised filter behavior as current coverage. Executable attribute correction to `5.0.0` and whether the harness rejects malformed criteria belong to Pass 2; Markdown/XML sample corrections are permitted in Pass 1.

**Dependencies/re-review:** this test's summary/comments, module filtering examples, Tests.Shared attribute/helper XML and filter validation contract. Reopen after strict criterion validation or parsing/input correction.

### A-11 — OTP backend summary incorrectly attributes inbound CRC validation to the backend

**Status:** candidate internal-comment correction. **Priority:** low/medium.

**Evidence:** `Backend/OtpBackend.cs:20–23` says it encodes slot commands “with CRC validation.” Its methods simply delegate write/mode/status. Core `OtpHidProtocol.cs:532–542` generates an outbound frame CRC; inbound validation is command-specific, as its class summary correctly states (`28–31`). Device-info CRC is checked by Core `DeviceInfoReader.cs:181–199`, a path that bypasses the backend.

**Proposed correction:** describe delegation to the OTP HID protocol/slot commands, without claiming backend inbound validation. Preserve the useful 8-byte feature-report context but attribute framing to Core. `IManagementBackend.WriteConfigAsync` parameter documentation (`32`) should clarify the **one-byte total length followed by TLVs**, not suggest bare TLVs; R1 and DeviceConfig serialization support that distinction.

**Dependencies/re-review:** backend XML, module CLAUDE backend/TLV explanation (C). No protocol behavior change proposed; reopen if response validation is moved.

### A-12 — Internal/test explanations contain phase history, unsupported absolutes, and placeholder residue

**Status:** candidate documentation cleanup; preserve actual unfinished test gap. **Priority:** low.

**Evidence:** transport-test class XML says “Phase 38” (`IYubiKeyExtensionsTransportTests.cs:27–30`); numbered `(a)/(b)` development comments and “would be a lie” (`186–187`) obscure the simple no-fallback contract. Unit session disposal summary (`130–135`) narrates the former eight-session implementation history. `ManagementAuditReproTests.cs:20–22` says a “built” config cannot change “when its source changes”; current test derives a new immutable record and asserts both values (`28–35`), not mutation of a source/builder. Audit trait/issue IDs remain useful traceability; the pinned wire-order citation (`71`) is valid (R5). Integration `ManagementSessionTests.cs:111` is “Bio-specific tests would go here,” but its body only asserts a Bio form factor.

**Proposed correction:** explain present regression invariants naturally; retain issue provenance where useful without stale builder/phase framing. Remove the placeholder comment from the executable test file while recording the absence of biometric-operation coverage durably. Do not remove the test, rename symbols/traits, or pretend such coverage exists.

**Dependencies/re-review:** listed comments and contributor descriptions of these tests (C). Search of owned files found no TODO/FIXME/HACK/NotImplemented markers beyond this placeholder/history material. No zero-TODO completion claim is appropriate.

### A-13 — Narrow Core dependency: FIPS flags are decoded using application masks

**Status:** deferred-pass-2 cross-module behavior candidate; no Core edit. **Priority:** high for reliance on FIPS flags; primary wire-spec gap remains.

**Evidence:** Core `DeviceInfo.cs:95–96` routes tags `14`/`15` to `ReadCapabilities`, whose `174–181` uses `CapabilityMapper.FromApp`. The same file contains unused `FromFips` and a distinct mapping (`271–290`): FIPS `01 → Fido2`, `02 → Piv`, `04 → OpenPgp`, `08 → Oath`, `10 → HsmAuth`. Direct application bits are different (`DeviceCapabilities.cs:25–55`). For example, a one-byte FIPS PIV indicator `02` becomes `U2f`, so Management's PIV FIPS filters can skip the very device intended. R5's pinned manufacturer-maintained implementation uses `_from_fips` for both fields and the same distinct mapping; it is corroborating implementation evidence, not normative certification proof.

**Desired vs observed:** public `FipsCapabilities`/`FipsApproved` and PIV filtering are intended as application sets, but this path does not apply its own FIPS mapping. In addition, `FromFips` itself reads Int16 for nonzero one-byte data; simply switching the call would need width review/tests, not a blind fix.

**Proposed documentation handling:** narrow FIPS test claims per A-06 and record this limitation in handoff. Do not document the wrong bit interpretation as the correct contract. Coordinator should decide an appropriately scoped consumer qualification where metadata is used for security decisions, without turning an unvalidated implementation candidate into a sweeping product warning.

**Missing evidence/deferred action:** acquire authoritative firmware-tag specification/fixtures and add focused decode/filter tests in the separately selected Core/Pass-2 work. Existing local mapping plus R5 is strong defect evidence, but no live FIPS fixture or normative tag-format document was obtained in bounded research.

**Dependencies/re-review:** Management FIPS test comments and Markdown examples, Core metadata XML/CapabilityMapper, Tests.Shared FIPS filters, any FIPS claims in shared examples. Reopen after mapping/width fixes and affected flag/filter tests; certification remains a separate dimension.

### A-14 — Narrow Core dependency: flagged form-factor byte is not normalized

**Status:** deferred-pass-2 cross-module behavior candidate; documentation must not canonize defect. **Priority:** medium.

**Evidence:** Core `DeviceInfo.cs:85–88` extracts `IsFips`/`IsSky`, then casts the full raw byte to `FormFactor`, whose named values are only `0–7` (`FormFactor.cs:17–57`). R1's manufacturer table explicitly encodes USB-A keychain as `01`, Security Key as `41`, FIPS as `81` for FW 5.4+. Thus a FIPS USB-A keychain becomes unnamed enum `0x81`, not `UsbAKeychain`; exact form-factor filtering (`Tests.Shared/.../YubiKeyTestInfrastructure.cs:86–88`) may skip it. The module CLAUDE gotcha (`881`) calls these flags part of the enum, but that is not its declared public shape. R5 normalizes with `code &= 0xF` before constructing its form-factor enum.

**Desired vs observed:** the API separately exposes product flags and physical form factor; current parsing can retain the flags in both. Do not redefine the enum contract in public prose to excuse the decoder.

**Proposed documentation handling:** correct descriptions that call the enum itself a flags enum; retain the raw-byte/decoded-property distinction in contributor guidance and handoff. Defer parser normalization and exact form-factor tests; C coordinates related guide qualifications.

**Dependencies/re-review:** Core metadata/form-factor contract, module form-factor tests/filter docs, ManagementTool display/filtering, Tests.Shared form-factor filter. Reopen after parser/forward-compatibility policy and flagged-wire fixture tests.

## Per-file disposition (complete reads)

Paths below are relative to `src/Management/`. A mixed file is not wholly “clean”; the clean units table identifies precisely what was accepted without a candidate.

| Owned file | Disposition and semantic scope |
| --- | --- |
| `src/IManagementSession.cs` (64 lines) | A-01/A-02/A-03; read-info/write missing XML; reset public contract/example fully inspected. |
| `src/ManagementSession.cs` (206) | A-01/A-02/A-03/A-09; complete creation/probes/version resolution/operations/backend dispatch; disposal-order rationale clean. |
| `src/IYubiKeyExtensions.cs` (146) | A-01/A-03; all three public extensions, class remarks, transport/ownership comments; selection implementation and own-connection rationale clean. |
| `src/Backend/IManagementBackend.cs` (43) | A-11 payload precision; ownership remarks and legacy-operation summary clean. |
| `src/Backend/SmartCardBackend.cs` (87) | Clean present backend prose; SELECT/write/reset implementation checked against R1/R2. Legacy `SetModeAsync` is not reachable through the current public Management API; no public legacy support should be inferred. Its P1=0 differs from R5's legacy P1=0x11: dormant behavior discrepancy for future investigation, not a doc fix or additional public contract. |
| `src/Backend/FidoHidBackend.cs` (54) | Clean present XML/internal command comments; complete write/mode methods and silent notification forwarding inspected. Vendor bytes include the HID init bit; R1 uses the same wire-byte notation. |
| `src/Backend/OtpBackend.cs` (56) | A-11; complete status/version and write/mode delegation inspected. No backend resource ownership. |
| `tests/Yubico.YubiKit.Management.UnitTests/ManagementSessionTests.cs` (367) | A-01/A-06/A-07/A-12; all tests, reflection/uninitialized helpers, protocols/connections fully read; no execution. |
| `tests/Yubico.YubiKit.Management.UnitTests/ManagementBackendLifecycleTests.cs` (87) | Clean lifecycle comments; tests pin non-IDisposable shape and absence of redundant backend FIDO initialization, not hardware ownership teardown. |
| `tests/Yubico.YubiKit.Management.UnitTests/ManagementSessionExchangeOverlapTests.cs` (186) | Clean class/test/helper explanations; asserts admitted-page refusal, undisturbed two-page response and later reuse. |
| `tests/Yubico.YubiKit.Management.UnitTests/IYubiKeyExtensionsTransportTests.cs` (274) | A-12 wording/history; transport/no-fallback assertions otherwise match comments. Fake-selected connection failures do not establish native OS behavior. |
| `tests/Yubico.YubiKit.Management.UnitTests/FirmwareVersionTests.cs` (600) | Clean explanatory comments for comparison/identity/null/string/bytes/operators/static values; Core-owned type still included in module coverage. Hash inequality is an assertion for one pair, not a general distinct-values hash guarantee; no runtime collision failure demonstrated. |
| `tests/Yubico.YubiKit.Management.UnitTests/AuditV2/ManagementAuditReproTests.cs` (103) | A-12 immutable-record explanation; pinned wire-order reference verified R5; all omission/range/serialization assertions inspected, B owns model contracts. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/IntegrationTestBase.cs` (42) | Clean monitor start/shutdown comments; no test derives from it in the owned files, so do not claim the entire suite uses this lifecycle. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionTests.cs` (149) | A-04/A-06/A-10/A-12; all device/filter/form-factor/placeholder comments checked; “Management requires 4.1.0+” is supported minimum, not proof of enforcement. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionSimpleTests.cs` (302) | A-05/A-06/A-07; every creation/info/config/SCP/AuthorizedDevices loop read. Explicit direct SmartCard/FIDO paths and default-extension tests distinguished. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionAdvancedTests.cs` (447) | A-04/A-05/A-06; all 17 test bodies/remarks read; final pinned transport test and serial consistency tests are useful evidence within actual row binding. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionCapabilityTests.cs` (127) | A-07/A-08; USB persistence vs active interface behavior, NFC flag vs actual wireless behavior distinguished. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/ManagementLockCodeTests.cs` (139) | A-06/A-07; both lock lifecycle methods fully inspected including cleanup failures. Zero-code clear comment agrees with R1. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/ManagementHidConcurrencyTests.cs` (119) | Clean sequencing/overlap bounds, genuine transport pins/assertions and narrow refusal filter; static review only, no hardware result. |
| `tests/Yubico.YubiKit.Management.IntegrationTests/ReconfigurationDiscoveryInvariantTests.cs` (240) | A-06 qualifications; invariant/helper comments otherwise align with assertions and bounded retry intent. Deadline checked between awaits is not a hard end-to-end wall-clock guarantee. Historical macOS evidence not independently revalidated. |

## Clean semantic units available for independent sampling

“Clean” means the explanation matches the static evidence in the stated unit; it does not mean tests ran or the broader feature is qualified. These units are intentionally narrower than files with mixed findings.

| Unit | Location | Evidence/bound |
| --- | --- | --- |
| A-C01 | `IManagementBackend.cs:21–26` | Backend borrows protocol, has no disposable state; session/creator lifetime chain traced in Core. |
| A-C02 | `SmartCardBackend.cs:21–24,34–55` | SmartCard APDU backend description; SELECT and INS 1C write match R1. |
| A-C03 | `FidoHidBackend.cs:20–30` | FIDO Management vendor-command description; C0/C3 notation consistent with wire init bit and R1/R5. |
| A-C04 | `IYubiKeyExtensions.cs:127–130` | OwnConnection rationale: only convenience factory-created connection is transferred. |
| A-C05 | `IYubiKeyExtensions.cs:138–144` | Exactly one default/override, no connect/init fallback; tested fake attempts agree. |
| A-C06 | `ManagementSession.cs:127–129` | Disposal checked before firmware gate; all three operations guard disposal. |
| A-C07 | `ManagementBackendLifecycleTests.cs:23–39` | Non-disposable invariant and prepared FIDO channel; inert initializer would throw if called. |
| A-C08 | `ManagementSessionExchangeOverlapTests.cs:21–25,43–69` | Correct exchange-vs-public-operation distinction, wire count and subsequent reuse assertions. |
| A-C09 | `ManagementHidConcurrencyTests.cs:24–37` | Nondeterministic hardware overlap need not refuse; deterministic unit references clearly distinguished. |
| A-C10 | `ManagementHidConcurrencyTests.cs:59–105` | Completion/refusal union, one success, identity fields and later reuse exactly asserted. |
| A-C11 | `ManagementHidConcurrencyTests.cs:108–111` | Filter-vs-pin distinction and actual `ConnectionType` assertion. |
| A-C12 | `ManagementSessionTests.cs` (unit) `53–68` | Failed direct creation does not dispose borrowed connection; actual counter assertions. Cancellation test is pre-cancelled, not proof of a mid-initialization interruption. |
| A-C13 | `ManagementSessionTests.cs` (unit) `173–191,296–319` | Real constructor/disposal versus skipped initialization accurately identified by helper prose. |
| A-C14 | `ManagementAuditReproTests.cs:55–57,71–101` | NFC-only omission bytes and all illustrated TLV field/order assertions; pinned R5 matches emitted sequence for chosen true restriction/current-lock input. |
| A-C15 | `FirmwareVersionTests.cs:43–74,568–598` | Integer-overload development-version allowances and operator relationship comments match assertions/source. Do not extend “always” to all possible FirmwareVersion development-to-development comparisons. |
| A-C16 | `IntegrationTestBase.cs:19–29` | Base starts monitor; static `YubiKeyManager` API in use. No claim every test inherits it. |
| A-C17 | `ManagementSessionSimpleTests.cs:31–44,187–208` | Explicit SmartCard factory/session serial verification; default-key SCP03 case requires appropriate configured keys and only demonstrates selected operation communication if executed. |
| A-C18 | `ManagementLockCodeTests.cs:47–48,74–79` | All-zero replacement clears config lock, with current code in same command; R1 supports actor/operation. Cleanup success not guaranteed. |
| A-C19 | `ReconfigurationDiscoveryInvariantTests.cs:112–135` | Exactly two per-completed-scan identity invariants, not physical grouping completeness. |
| A-C20 | `ReconfigurationDiscoveryInvariantTests.cs:159–172,215–218` | Retry intent and between-attempt deadline; comments do not establish cancellation of a hung awaited operation. |

## Dependency reads and boundaries

Complete narrow units read: Core `Sessions/ApplicationSession.cs`, `SessionCreationOptions.cs`; `Devices/{DeviceInfoReader,DeviceInfo,FirmwareVersion,Feature,YubiKeyConnectionExtensions,DeviceCapabilities,FormFactor}.cs`; `Utilities/ExchangeGuard.cs`; `Protocols/SmartCard/Apdu/PcscProtocol.cs`; `Protocols/{Fido/Hid/FidoHidProtocol,Otp/Hid/OtpHidProtocol}.cs`. Scope was the call paths used by Management. No native implementation, SCP cryptographic handshake implementation, whole discovery merger, or Core test suite was audited.

Tests.Shared complete reads: instructions/README, `YubiKeyTestStateExtensions.cs`, `Infrastructure/{WithYubiKeyAttribute,FilterCriteria}.cs`. Focused units only: `YubiKeyTestState.cs:315–346,407–427` (binding and capability predicates), `Infrastructure/YubiKeyTestInfrastructure.cs:72–125` (filtering). These partial dependency reads are not whole-file coverage claims. Module public snapshot was inspected to reconcile public families; generated record and C# extension snapshot entries do not need independent prose.

Configuration dependencies `DeviceConfig.cs` / `SetDeviceConfigOptions.cs` were read completely for session validation/serialization/ownership, but remain B's inventory. README/CLAUDE/shared documentation are impact-map inputs; C owns their full documentation disposition. The old Config Reference contains an apparent NFC-write-tag typo (0C versus the encoder/read field's 0E); it must not be used to change the encoder. B/C adjudicate that citation issue. No historical archive was read wholesale.

## Sources and focused citation cache

All URLs below were accessed **2026-11-03**. Manufacturer pages are primary product/reference descriptions, but unversioned web pages are not immutable protocol standards. Exact status/section and actor are recorded to avoid silently upgrading their authority. Quotes are deliberately focused; local source line citations refer to the verified baseline.

### R1 — YubiKey 5 Series Configuration Reference Guide

- URL: <https://developers.yubico.com/yubikey-manager/Config_Reference.html>.
- Status/version: unversioned living manufacturer reference as served on access date; opening scope is YubiKey 5 Series and above / Security Key, with read-format compatibility with YubiKey 4. No specific revision/date exposed. Sections: **Reading Information → Device Information**; **Writing Information**; **Set Configuration Lock**; **Unlock Configuration Lock**; **Reset**; **Concepts → Form Factor**.
- Actor/layer: host encodes interface-specific Management command; device applies lock/capability policy. Product protocol description, not a FIDO application command requirement.
- Focused quotes: “CCID … instruction 0x1d”; “OTP … SLOT 0x13”; “FIDO … HID command 0xc2 … with INIT set.” Write equivalents are “0x1c”, “SLOT 0x15”, “0xc3”. “one Byte total length followed by a list of TAG-LENGTH-VALUE (TLV) triplets”.
- Lock: “If the payload is the Byte 00 repeated 16 times, the Configuration Lock is disabled”; “All interface commands requiring the Configuration Lock must contain an Unlock Configuration Lock TLV.” This supports per-command unlock and zero-code clearing, not session authentication.
- Reboot TLV: “forces the device to reboot if all the other TLVs have been correctly parsed.” It is not the whole-device reset APDU.
- Form-factor table: keychain USB-A Standard `0x01`, Security Key FW5.4+ `0x41`, FIPS FW5.4+ `0x81`.
- Limitation: the page's Enabled NFC write section says tag 0C and repeats USB prose, contradicting its read table and current manufacturer encoder. Do not treat that isolated row as authoritative against the implementation. It does not specify current NFC-restricted false-write behavior or prove config-lock recovery by device reset.

### R2 — YubiKey Bio Multi-protocol Edition considerations and quirks

- URL/sections: <https://docs.yubico.com/yesdk/users-manual/sdk-programming-guide/bio-mpe.html#resetting-a-yubikey-bio-mpe>, and **DeviceReset APDUs** on that page.
- Status/version: living official legacy .NET SDK user manual, no fixed version/revision displayed; current access-date description. Hardware-specific evidence, not v2 API syntax authority.
- Actor/layer: Bio MPE hardware shared-PIN reset; host sends Management APDU. Declarative product support condition.
- Quotes: “For all YubiKeys except for the YubiKey Bio MPE, factory resets are done strictly by application.” Device-wide reset “resets both PIV and FIDO applications at the same time.” APDU table is CLA `00`, INS `1F`, P1/P2 `00`, Lc/Data/Le absent; success `9000`, failure `6f00`.
- Important distinction: the page's `ResetBlocked` note concerns **individual FIDO reset**, not a requirement to block PINs before the Management whole-device command. Do not transfer that condition across operations. No such block prerequisite was established for Management reset.

### R3 — DeviceResetCommand reference

- URL/section: <https://docs.yubico.com/yesdk/yubikey-api/Yubico.YubiKey.Management.Commands.DeviceResetCommand.html>, **Remarks**.
- Status/version: living official legacy SDK API reference for `Yubico.YubiKey.dll`; no immutable version on fetched page. Corroborates hardware effects, not v2 exception behavior.
- Actor/layer: device application reset. Quotes: “only available on YubiKey Bio Multi-protocol Edition keys”; deletes FIDO2 credentials/fingerprints, removes shared PIN, deletes PIV keys/certificates “(except the F9 attestation slot)”. The passage does not assert erasure of the Management lock/configuration.

### R4 — YubiKey Technical Manual, 5.7–5.6 Firmware Specifics

- URL/section: <https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-firmware-5.7.html#restricted-nfc>, **Restricted NFC**.
- Status/version: living official Technical Manual, copyright 2026, section explicitly applies NFC restriction to firmware **5.7 and later**. No immutable document revision displayed.
- Actor/layer: device shipping protection and end-user activation, not an SDK touch/PIN ceremony.
- Quotes: “the only permitted action via the NFC connection is reading the URL … on the NDEF tag”; “plug the key into USB power such as a USB charger or computer USB port for 3 seconds. This action is sufficient to disable Restricted NFC mode.”
- Does not specify tag-17 false-write semantics; do not derive them from this consumer activation procedure.

### R5 — Manufacturer-maintained Python sibling at the exact cited revision

- URL: <https://raw.githubusercontent.com/Yubico/yubikey-manager/4ca60f7/yubikit/management.py>.
- Status/version: exact abbreviated commit `4ca60f7` named in owned wire-order test, fetched successfully; implementation evidence, **not normative protocol authority**. Sections/symbols: `DeviceConfig.get_bytes`, `_ManagementSmartCardBackend`, `ManagementSession.read_device_info`, `ManagementSession.device_reset`, `CAPABILITY._from_fips`, `FORM_FACTOR.from_code`.
- Encoder order: reboot → unlock → USB → NFC → auto-eject → challenge timeout → flags → new lock → NFC restriction. Includes `TAG_NFC_ENABLED = 0x0E`. Exact fixture in owned test matches this order for its provided fields; no claim that TLV order is device-mandated.
- Focused excerpts: `if self.nfc_restricted: ... Tlv(TAG_NFC_RESTRICTED, b"\1")`; `code &= 0xF`; FIPS PIV `if fips & (1 << 1): c |= CAPABILITY.PIV`; read gate `require_version(self.version, (4, 1, 0))`; reset doc: shared global PIN and associated applications, CCID-only backend.
- Initial official generated source page also read: <https://developers.yubico.com/yubikey-manager/API_Documentation/_modules/yubikit/management.html>. It is unversioned and differs in pagination from pinned R5; conclusions about the wire-order test use pinned R5, not a silently substituted current source revision.

### R6 — .NET hash/equality contract (Context7 framework check; no failure claim)

- Context7 resolved `/dotnet/docs`, queried `Object.GetHashCode` / `HashCode.Combine` collision contract. Returned official documentation: <https://github.com/dotnet/docs/blob/main/docs/framework/debug-trace-profile/moduloobjecthashcode-mda.md> (**Cause**) and <https://github.com/dotnet/docs/blob/main/docs/fundamentals/code-analysis/quality-rules/ca2218.md> (**Rule description**).
- Status/version: living Microsoft documentation, general hash/equality principle; first page describes a .NET Framework diagnostic aid, not .NET 10 hash implementation. Actor: implementer/caller of hashing/equality.
- Quote: “two objects may have the same hash code even if their respective fields have different values.” Equal same-type objects must return the same hash. This does **not** establish that the specific two FirmwareVersion inputs collide on .NET 10; no such failure is claimed or new finding counted.

## Handoff summary

Coverage: **21/21 owned C# files fully read** (7 source, 6 unit-test files, 8 integration-test files), plus scoped dependencies and 20 explicitly bounded clean semantic units. Fourteen A-* candidates are distinct investigation buckets, not fourteen proven independent runtime bugs. A-01–A-12 primarily drive documentation corrections/qualifications, with explicit deferred test/behavior portions; A-13/A-14 are narrowly traced Core cross-module candidates. Deduplicate overlaps against B/C rather than adding counts.

Highest-impact decisions: reset family/erasure/recovery qualification (A-02); truthful cleanup claims and future secret cleanup (A-07); public session/lifetime/sequence contracts (A-01/A-03); and avoiding FIPS assurance based on unqualified flags (A-06/A-13). Consequential unknowns remaining after bounded research: Management configuration-lock erasure/reset teardown policy, NFC false-write semantics, authoritative current FIPS-tag wire widths/mapping, and original historical macOS scan-log provenance. None authorize a source fix, test assertion change, or live device operation in Pass 1.
