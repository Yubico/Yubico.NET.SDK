# Management: Pass 2 classification
Classifier: one dedicated subagent, 2026-11-14. Source: master lines 1388–2339 at 52d34ca65. Policy: pass-2-review-policy.md (approved). Final per-card calls: `../pass-2-review-ledger.md`.
Sibling pins: py = yubikey-manager @ 4ca60f70 (`yubikit/`, `ykman/`); android = yubikit-android @ f4626856 (`management/src/main/java/com/yubico/yubikit/management/`); rust = yubikey-manager-rust-auto @ 90940e9b (`crates/yubikit/src/management.rs`). python-fido2 not used. .NET citations are at 52d34ca65 (cited paths clean in `git status`).

## Summary

| Disposition | Count | Cards |
| --- | --- | --- |
| A1 | 5 | MGMT-IMP-005, MGMT-IMP-006, MGMT-IMP-009 (local literal), MGMT-IMP-017 (labels), CORE-MGMT-X003 (narrow oracle) |
| A2 | 1 | MGMT-IMP-008 (public read floor; factory probe kept under A6) |
| A3 | 0 | none |
| A4 | 0 | none |
| A5 | 1 | MGMT-IMP-016 |
| A6 | 1 | MGMT-IMP-013 |
| Interview (gated) | 10 | High: MGMT-IMP-010. Medium: MGMT-IMP-003, MGMT-IMP-004, MGMT-IMP-007, MGMT-IMP-015, MGMT-XREF-001. Low: MGMT-IMP-001, MGMT-IMP-012, MGMT-IMP-014, MGMT-XREF-002 |
| T1 | 3 | MGMT-IMP-002, MGMT-IMP-011, Management qualification |
| T2 tag (not a disposition) | 13 | MGMT-IMP-001, -003, -004, -005, -007, -008, -009, -010, -012, -015, -016, MGMT-XREF-001, MGMT-XREF-002 |

Total 21 review-map cards: 8 auto-approved, 10 interview, 3 T1.

Four interview cards hit no gate and no auto rule: MGMT-IMP-004, -007, -012 and MGMT-XREF-001. The policy has no route for decision cards like these, so I queued them for voice. Reroute them if you prefer. MGMT-IMP-005, -009 and -017 are split by the master's own no-brainer wording; only the parts the master marks yes, or keep-and-document, are auto-approved.

## Index

| Card | Severity | Disposition | One-line rationale |
| --- | --- | --- | --- |
| MGMT-IMP-001 | Low | Interview (gated: G3; T2) | All three sibling libraries lack a field-specific NFC preflight; only the Python CLI checks 5.7 and NFC support. |
| MGMT-IMP-002 | No assigned defect severity | T1 | `17 01 00` encoding is established; device effect, recovery route and sibling split (Android explicit zero; Python and Rust omit) are unqualified. |
| MGMT-IMP-003 | Medium | Interview (gated: G3; T2) | Android and .NET silently narrow masks; Rust cannot express out-of-width values; Python expands. Unknown in-width bits need a decision. |
| MGMT-IMP-004 | Medium | Interview (no gate or auto rule; T2) | Independent finally is A5-shaped, but the module documents both zeroing and retention of the lock code, and they conflict when removal fails. |
| MGMT-IMP-005 | No assigned production-defect severity | A1 (T2 tag) | Test-only oracle for a held or failed backend write; production already zeroes after backend completion. |
| MGMT-IMP-006 | Low | A1 | Concrete-transport rows must pass the preferred connection and assert the session's `ConnectionType`. |
| MGMT-IMP-007 | Medium | Interview (no gate or auto rule; T2; T1 dependency) | Broad negative assertions confirmed; exact outcomes need a decision now and device status words later. |
| MGMT-IMP-008 | Low | A2 (probe kept under A6; T2 tag) | Python, Android and Rust all gate public device-info reads at 4.1; .NET has no gate. Factory probe stays best-effort. |
| MGMT-IMP-009 | Low | A1 (T2 tag) | Local `"5.0"` literal is ignored by the parser; the shared reject-or-ignore policy is T2. |
| MGMT-IMP-010 | High | Interview (gated: G1, G3; T2) | All four implementations gate reset by backend type; Android and .NET add a 5.6 firmware gate; none checks product family. Eligibility and lifecycle need a decision. |
| MGMT-IMP-011 | No assigned defect severity | T1 | Metadata-only oracles; biometric and FIPS operation tests need devices. |
| MGMT-IMP-012 | Low (if enabled unchanged) | Interview (no gate or auto rule; T2) | Siblings agree on P1 `0x11` but split on legacy payload byte order (Android big-endian; Python and Rust little-endian). |
| MGMT-IMP-013 | Low | A6 | SmartCard-only scope protects the reset menu and is already recorded in the module CLAUDE.md; keep and document. |
| MGMT-IMP-014 | Low | Interview (gated: G5) | `info --help` runs device info; the command-help and argument contract is a UX decision. |
| MGMT-IMP-015 | Medium | Interview (gated: G5; T2) | String prompts leave lock-code copies in memory; a clearable input path is a concrete safety gain. Owner cleanup alone is A5. |
| MGMT-IMP-016 | Low | A5 (T2 tag) | Unified CLI and OATH tool map cancellation to `ExitCode.UserCancelled`; token forwarding follows. |
| MGMT-IMP-017 | Medium (FIPS); Low (reboot, flags) | A1 + A6 | Label corrections are literal; the reboot flag is already documented as a request, so keep the member. |
| MGMT-XREF-001 (referral) | Medium | Interview (no gate or auto rule; Core-owned; T2) | FIPS bit mapping agrees across Python, Android, Rust and the unused .NET helper; width rule is open and Core-owned. |
| MGMT-XREF-002 (referral) | Low | Interview (gated: G5; Core-owned; T2) | Python, Android and Rust normalize the base form factor; .NET keeps the raw byte as documented design. |
| CORE-MGMT-X003 | Low | A1 | Bound oracle through public `CreateAsync` after failed init; reset-wire extension excluded (waits for MGMT-IMP-010). |
| Management qualification | No assigned defect severity | T1 | No live Management operation has run; the hardware matrix is in the T1 table. |

## Interview queue (sorted High, then Medium, then Low)

### MGMT-IMP-010: Combined reset product and lifecycle contract
- Gate(s): G1 (High severity). G3: none of the four implementations checks product family. Python and Rust check only the SmartCard backend (`py:yubikit/management.py:756–764`; `rust:management.rs:1133–1138, 1305–1310`). Android and .NET add a 5.6 firmware gate (`android:ManagementSession.java:486–487`; `src/Management/src/ManagementSession.cs:207–216`). Sibling precedent does not settle eligibility. T2 link: OATH already ends the session after a confirmed RESET (`src/Oath/src/OathSession.cs:670–681`).
- Severity / certainty: High (destructive-operation policy risk). Family and lifecycle gaps confirmed; out-of-family erasure not observed.
- Decision: Which contract should the public reset have?
- Options:
  - (a) Family-gated to authoritative Bio MPE evidence over SmartCard; session ended after confirmed success. Consequence: needs a verified Bio MPE source, which the SDK does not have (`src/Management/CLAUDE.md:107–110`), so it blocks until one exists.
  - (b) Keep device-authoritative dispatch (5.6 and SmartCard); state the scope in docs (FIDO, PIV, shared PIN; F9 retained); end the session after confirmed success; report uncertain outcomes without claiming completion; revisit family gating when a verified source exists. Sia's lean: (b). No verified eligibility source exists, so (a) would encode a guess; (b) fixes lifecycle and scope without new claims.
  - (c) Remove the public reset and keep it internal. Consequence: drops a Bio-only operation from the public surface; re-adding it later is a public API change.
- Evidence: `src/Management/src/ManagementSession.cs:207–216`; `src/Management/examples/ManagementTool/Features/DeviceReset.cs:47–49`; `src/Cli.Commands/src/Management/ManagementResetCommand.cs:32–33, 56–57`; `py:yubikit/management.py:756–764` @4ca60f70; `android:ManagementSession.java:486–487` @f4626856; `rust:management.rs:1133–1138, 1305–1310` @90940e9b.

### MGMT-IMP-003: Request width and supported masks
- Gate(s): G3. Android shares the silent narrowing: `android:DeviceConfig.java:111–113` passes an `int` through `enabledCapabilities(…, int)` at 183–184. .NET narrows too (`src/Management/src/DeviceConfig.cs:185–189`). Python expands instead of truncating (`py:yubikit/management.py:260, 263`; `py:yubikit/core/__init__.py:264–266`). Rust cannot express out-of-width values (`rust:management.rs:112, 584–593`). Precedent is split and partly shares the defect. T2 link: Core width policy (A-C006/C016).
- Severity / certainty: Medium. Narrowing confirmed from source; hardware lockout not observed.
- Decision: Should out-of-width, negative and over-byte request values throw before encoding, with in-width unknown bits passed through as the caller sent them and documented?
- Options:
  - (a) Yes to both. Sia's lean. Consequence: throws before transmission; unknown in-width bits pass through; docs state that the caller owns supported-application checks, which the sibling libraries also leave to the caller.
  - (b) Yes to the throw, and also reject unknown in-width bits. Consequence: stricter; conflicts with the card's warning and the forward-compatibility doctrine (`CLAUDE.md:156–158`).
  - (c) Add device-aware checks against `UsbSupported` and `NfcSupported`. Consequence: needs an extra read; no sibling library does this (the Python CLI does, `py:ykman/_cli/config.py:270–282`).
- Evidence: `src/Management/src/DeviceConfig.cs:162–163, 185–189`; `android:DeviceConfig.java:111–113, 183–184` @f4626856; `rust:management.rs:112, 584–593` @90940e9b; `py:yubikit/management.py:258–263` @4ca60f70.
- Note: the out-of-width throw is A4-shaped and can be split into its own commit if you want that part settled now.

### MGMT-IMP-004: Test restoration and independent secret cleanup
- Gate(s): none. No auto rule: the independent-finally part is A5-shaped (OATH precedent below), but the module CLAUDE.md states both independent zeroing and retention until removal (`src/Management/CLAUDE.md:561–562`), and they conflict when removal fails. T2 link: secret lifetime on failure.
- Severity / certainty: Medium. Ordering and default-owner facts confirmed; the SCP03 defaults are public test keys, not production secrets.
- Decision: When a lock-code removal or restoration fails, should the test keep its in-memory lock code until removal succeeds, or zero it anyway in the independent finally?
- Options:
  - (a) Keep it until removal succeeds, then zero it in the independent finally. Sia's lean: a dedicated key with a lost lock code has no documented recovery (`src/Management/CLAUDE.md:908–909`), so zeroing on failure can strand the key.
  - (b) Zero it immediately in the independent finally. Consequence: a failed removal can leave the key locked with an unknown code.
  - (c) Keep the current ordering and document the gap. Consequence: zeroing is skipped after a failed cleanup.
- Evidence: `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementLockCodeTests.cs:74–89, 130–141`; `ManagementSessionSimpleTests.cs:157–160, 184–187`; `src/Management/tests/Yubico.YubiKit.Management.UnitTests/IYubiKeyExtensionsTransportTests.cs:182–184`; `ManagementSessionAdvancedTests.cs:73–77`; OATH precedent `src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/OathSessionTests.cs:43–63` (finally zeroing).

### MGMT-IMP-007: Negative-test error provenance
- Gate(s): none. No auto rule: the exact expected outcomes need a decision and device evidence. T2 link: SCP setup and wire-status provenance (Core B6).
- Severity / certainty: Medium (test assurance; not a proven authentication bypass). Broad assertions confirmed; device status unqualified.
- Decision: Which assertions should the missing-lock-code and wrong-SCP-key negatives make now?
- Options:
  - (a) Assert the layer's exception type and preserved inner cause now; add the raw status word after a device run (T1). Sia's lean: stronger than `ThrowsAny` without inventing a status word.
  - (b) Wait for device-observed status words before changing the tests. Consequence: the weak oracle stays longer.
  - (c) Keep the broad assertion and document it. Consequence: the known weak oracle remains.
- Evidence: `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementLockCodeTests.cs:62–67`; `ManagementSessionSimpleTests.cs:235–241`; `py:ykman/_cli/config.py:230–233` (broad catch plus hint) @4ca60f70; `py:yubikit/management.py:560–564` (APDU passthrough).

### MGMT-IMP-015: Lock prompt secret lifetime
- Gate(s): G5. A clearable input path is a concrete safety improvement over the string-prompt precedent (`py:ykman/_cli/config.py:203–205` uses hidden strings @4ca60f70; the Spectre prompt returns strings). T2 link: secret input acquisition across example tools. The OATH tool already owns its password buffer (`src/Oath/examples/OathTool/OathToolApplication.cs:50–51, 95–107`).
- Severity / certainty: Medium (host-side sample retention). Strings and pre-cleanup failure confirmed; extraction not demonstrated.
- Decision: Should the sample read lock codes into clearable buffers, or keep Spectre string prompts and fix only cleanup ownership?
- Options:
  - (a) Clearable input and owned cleanup. Sia's lean: no immutable copies of the code; the cost is more sample code and our own hex, backspace and overflow rules.
  - (b) Keep string prompts and place the first parsed array under `try/finally` before confirmation. Consequence: smaller change; prompt strings stay in memory until collection.
  - (c) Keep as is and document the retention. Consequence: no change; documented limitation.
- Evidence: `src/Management/examples/ManagementTool/Cli/Prompts/LockCodePrompt.cs:74–78, 104–116, 155–160`; `src/Oath/examples/OathTool/OathToolApplication.cs:50–51, 95–107`; `py:ykman/_cli/config.py:203–205` @4ca60f70.
- Note: the owned-cleanup part is A5 and can be split out.

### MGMT-XREF-001: Core FIPS-tag decoding referral (Core-owned)
- Gate(s): none. No auto rule: the width rule is open, and the card needs the canonical owner to accept a bounded profile. T2 link: device-info decoding.
- Severity / certainty: Medium. Wrong integration and helper width confirmed; normative width gap retained.
- Decision: Should the FIDO2, PIV, OpenPGP, OATH and HSMAUTH bit mapping (shared by Python, Android, Rust and the unused .NET `FromFips` helper) be accepted now as the bounded profile, with the width rule set separately in Core (A-C004)?
- Options:
  - (a) Accept the mapping; accept 1–2 byte values and reject longer ones as malformed. Sia's lean: strict on malformed lengths (doctrine), and it reuses the existing helper.
  - (b) Accept the mapping; keep sibling-style unbounded widths. Consequence: tolerant of long fields; no fail-closed.
  - (c) Wait for normative or vendor width evidence. Consequence: wrong approval and filter results stay in place.
- Evidence: `src/Core/src/Devices/DeviceInfo.cs:204–205, 290, 379–400` (`FipsMapping` at 379; `FromFips` at 388; one-byte check at 391; `ReadInt16BigEndian` at 393; `FromApp` at 403); `py:yubikit/management.py:81–93, 375–379` @4ca60f70; `android:DeviceInfo.java:203–204, 416–433` @f4626856; `rust:management.rs:135–148, 745–749` @90940e9b.

### MGMT-IMP-001: Field-specific NFC preflight
- Gate(s): G3. All three sibling libraries lack a field-specific preflight: `py:yubikit/management.py:679` (5.0 only), `android:ManagementSession.java:405–411` (config feature only), `rust:management.rs:972–975` (5.0 only). The only sibling preflight is the Python CLI (`py:ykman/_cli/config.py:502–503, 517`), which is CLI policy. T2 link: firmware or hardware preflight policy.
- Severity / certainty: Low. Missing preflight source-confirmed; enforcement choice unresolved. No security bypass demonstrated.
- Decision: Should `NfcRestricted = true` be checked in the SDK before it is sent?
- Options:
  - (a) Preflight firmware 5.7 for `true` only; leave NFC hardware to the device; hold `false` until MGMT-IMP-002 closes. Sia's lean: cheap and deterministic, and it avoids inferring hardware from an absent mask, which the card warns against.
  - (b) No preflight; send, and map the device's rejection to a clear error. Consequence: no new gate; the error text depends on firmware.
  - (c) Preflight both firmware and NFC hardware, using a DeviceInfo read first. Consequence: an extra read in a write path; hardware evidence is `NfcSupported`, not mask absence.
- Evidence: `src/Management/src/ManagementSession.cs:171–173`; `src/Management/src/DeviceConfig.cs:141–144`; siblings as cited above.

### MGMT-IMP-012: Dormant legacy mode wire choice
- Gate(s): none. No auto rule: A4 needs a stricter option, and the payload byte order is split. Sibling P1 agreement is 3 of 3 (`py:yubikit/management.py:515, 555`; `android:ManagementSession.java:87–89, 198`; `rust:management.rs:60–64, 1296`). T2 link: legacy NEO and YK4 support (the canonical OTP card covers legacy relocation).
- Severity / certainty: Low if enabled unchanged. Dormant; no public C# legacy mode API.
- Decision: What should happen to the internal SmartCard mode primitive?
- Options:
  - (a) Retire it. Sia's lean: no caller, no qualified legacy device, and the siblings disagree on payload byte order; re-add it later with a qualified device.
  - (b) Keep it dormant and align P1 to `0x11`. Consequence: a one-byte change matching three siblings; payload byte order and app selection stay undecided.
  - (c) Implement a public legacy mode API now. Consequence: needs NEO or YK4 qualification (T1), a byte-order decision (Android big-endian at `android:ManagementSession.java:466–471`; Python and Rust little-endian at `py:yubikit/management.py:752` and `rust:management.rs:1288–1289`), and app-selection rules.
- Evidence: `src/Management/src/Backend/SmartCardBackend.cs:60–68` (P1 = 0); sibling public `set_mode`: `py:yubikit/management.py:695`, `android:ManagementSession.java:436`, `rust:management.rs:1087`.

### MGMT-IMP-014: Command help and reachable features
- Gate(s): G5. Concrete UX defect: `info --help` runs device info because only `args[0]` is read. The Python CLI's Click declarations give per-command help and a reachable restrict option (`py:ykman/_cli/config.py:115–118, 157–174, 487–525` @4ca60f70).
- Severity / certainty: Low (CLI mismatch; optional feature reachability).
- Decision: Should the tool implement per-command help and reject unknown arguments before device discovery, or drop the false help promise and accept no trailing options?
- Options:
  - (a) Implement command help and reject unknown arguments before discovery. Sia's lean: help and bad arguments need no device I/O, and the usage text becomes true.
  - (b) Remove the promise and accept no trailing options. Consequence: smallest change; `info --bogus` still runs info unless unknown arguments are also rejected.
  - (c) Adopt a full argument-parser library. Consequence: a dependency and style change for a sample tool.
- Either way, the NFC restriction task stays unexposed until MGMT-IMP-001 and -002 close.
- Evidence: `src/Management/examples/ManagementTool/Program.cs:21–30`.

### MGMT-XREF-002: Core flagged form-factor referral (Core-owned)
- Gate(s): G5. Three siblings normalize the base form factor: `py:yubikit/management.py:181–185` (`code &= 0xF`), `android:FormFactor.java:48–53` (`value &= 0xf`), `rust:management.rs:274–285` (`code & 0x0F`). The .NET enum keeps the raw byte and documents it (`src/Core/src/Devices/FormFactor.cs:19–20`). T2 link: device-info representation.
- Severity / certainty: Low. Flagged-key filter and label mismatch confirmed.
- Decision: Should the base form factor be normalized with the raw byte kept beside it, or should the raw enum stay and filters and labels be fixed around it?
- Options:
  - (a) Normalize the base and keep the raw byte. Sia's lean: matches all three siblings and the doctrine ("preserve complete raw response bytes", `CLAUDE.md:156–158`); it changes what `FormFactor` means, which alpha permits.
  - (b) Keep the raw enum and add explicit base-comparison helpers. Consequence: no meaning change; diverges from all three siblings.
  - (c) Keep as is and document. Consequence: flagged keys keep missing base filters.
- Evidence: `src/Core/src/Devices/DeviceInfo.cs:194–197, 57`; `src/Core/src/Devices/FormFactor.cs:19–20`; `src/Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs:86–88`; `py:yubikit/management.py:345–348` @4ca60f70.

## Auto-approved

### MGMT-IMP-005: Encoded-buffer failure oracle
- Rule: A1. The card marks it "Yes for selected test scope". Test-only; no production change; no sibling comparator.
- Evidence pin (E2): no sibling comparator (.NET test card). Production precedent: `src/Management/src/ManagementSession.cs:194–202` (finally after backend completion). Current test and fake: `src/Management/tests/Yubico.YubiKit.Management.UnitTests/ManagementSessionTests.cs:87–106` and `:218–223`.
- Pseudocode:
```text
HoldingBackend : IManagementBackend
  WriteConfigAsync(config): Captured = config (borrowed view); await a gate; then fail or observe cancellation
test:
  task = session.SetDeviceConfigAsync(config, { CurrentLockCode = lockCode })
  assert Captured holds non-zero bytes while held
  release the gate with the failure (or cancel)
  await task; catch the expected failure
  assert Captured is all zero after the task terminated
  assert the session left lockCode unchanged; the test clears lockCode in its own finally
```
- Test to add: `SetDeviceConfigAsync_BackendFailure_ZeroesEncodingOnlyAfterTaskTermination`; a cancellation variant; a NewLockCode variant.
- T2 tag: secret zeroing timing (drain before zero). If the T2 decision changes that timing, this test follows it.
- E1: stop if the test needs production exposure of the encoding, or if zeroing happens before the task terminates.

### MGMT-IMP-006: Actual operation transport coverage
- Rule: A1. The card marks it "Yes for tests whose accepted purpose is concrete-interface coverage".
- Evidence pin (E2): no sibling (`py:yubikit/management.py:599–618` dispatches on the supplied connection; its CLI fallback is not transplanted). .NET: one-shot selection `src/Management/src/IYubiKeyExtensions.cs:144`, documented order `src/Management/CLAUDE.md:336–338`; the correct pattern in the same suite is `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionAdvancedTests.cs:418–431`.
- Pseudocode:
```text
OTP row:
  mgmt = state.Device.CreateManagementSessionAsync({ PreferredConnectionType = HidOtp })
  assert mgmt.ConnectionType == HidOtp        // before the live read
  info = mgmt.GetDeviceInfoAsync(); assert info.SerialNumber == state.SerialNumber
CCID-only row: same, with SmartCard
HID FIDO reset rejection (SimpleTests:107–117) and AllTransports (AdvancedTests:418–437): already pinned; keep
```
- Test to add: none beyond the corrected integration rows. Do not duplicate pinned rows.
- E1: stop if a concrete row cannot bind its interface; skip it, do not fall back. Never infer one physical key across independently bound rows (`src/Management/CLAUDE.md:73–77`).

### MGMT-IMP-008: Legacy read and initialization policy (A2 public floor; A6 probe)
- Rule: A2 for the public read floor: py, android and rust all gate public device-info reads at 4.1. A6 for the factory probe: keep the best-effort initial read, which falls back to the transport version, and document it.
- Evidence pin (E2): `py:yubikit/management.py:642–645` @4ca60f70 (`require_version(self.version, (4, 1, 0))`); `android:ManagementSession.java:72–73, 361–362` @f4626856 (`FEATURE_DEVICE_INFO` 4.1.0; `require`); `rust:management.rs:1052–1058` @90940e9b (`< Version(4, 1, 0)` returns NotSupported). .NET, unchanged: `src/Management/src/ManagementSession.cs:39–40, 154–161` (no gate) and `:218–236` (probe).
- Pseudocode:
```text
GetDeviceInfoAsync(ct):
    ThrowIfDisposed()
    EnsureSupports(FeatureDeviceInfo)        // 4.1.0 on effective firmware; NotSupportedException before any I/O
    protocol = Protocol ?? throw InvalidOperationException
    return DeviceInfoReader.ReadAsync(protocol, _version, ct)
ResolveFirmwareVersionAsync (ManagementSession.cs:218–236): unchanged
```
- Test to add: in `ManagementSessionTests.cs`, firmware 4.0.9 through `CreateSessionForBackend` with `RecordingSmartCardProtocol`: expect NotSupportedException and zero recorded commands. Supported 4.1.0 reaches the read. Override and development firmware follow the effective-version path. The initialization probe test is unchanged.
- T2 tag: device-info read gate and probe (Core Feature and reader owner). The change stays in `ManagementSession.GetDeviceInfoAsync`; the shared reader and factory probe are not touched.
- E1: stop if any real or fixture 4.0.x device returns a valid DeviceInfo (the gate would remove a working read), or if the gate would move into the shared reader or the factory probe.

### MGMT-IMP-009: Malformed firmware test filter (local literal)
- Rule: A1 for the local literal (the card marks "Yes for local criterion"). The shared reject-or-ignore policy is not auto-approved (T2).
- Evidence pin (E2): no sibling (declarative .NET fixture). .NET: `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionTests.cs:138`; shared parse-and-ignore `src/Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs:79–83`; current behaviour documented at `src/Management/CLAUDE.md:52–53`.
- Pseudocode:
```text
[WithYubiKey(MinFirmware = "5.0.0")]        // was "5.0"; the three-component parser ignores it
[WithYubiKey(FormFactor = FormFactor.UsbAKeychain)]
```
- Test to add: a filter-path check with an older and a modern candidate through `YubiKeyTestInfrastructure.FilterDevices`: the row binds only devices at 5.0.0 or later, and the form-factor row binds independently. No validation-only parser test. If the candidate states need hardware, rely on the integration row and record the binding in the PR body.
- T2 tag: shared malformed-value policy (Core A-C014 and Tests.Shared).
- E1: stop if no authorized device at 5.0.0 or later exists (do not loosen the attribute to recover coverage), or if the T2 decision makes malformed strings fail (then this literal is redundant).

### MGMT-IMP-013: Standalone tool selection policy (keep and document)
- Rule: A6. The SmartCard-only scope is intentional, protects the reset menu, and is already recorded in the module CLAUDE.md. The card needs documentation only.
- Evidence pin (E2): none (product scope). .NET: `src/Management/examples/ManagementTool/Cli/Prompts/DeviceSelector.cs:13–16`; `src/Management/CLAUDE.md:38–41`; the unused `ManagementDeviceSelector` has no references under `src`.
- Pseudocode:
```text
examples/ManagementTool/README.md: add one sentence:
  "This tool selects SmartCard (CCID) devices only. HID-only keys are not offered, because the factory-reset menu requires SmartCard."
```
- Test to add: none (documentation only). Do not add a validation-only test.
- E1: stop if anyone proposes widening the selector; that reopens reset eligibility (MGMT-IMP-010).

### MGMT-IMP-016: Tool cancellation propagation (A5)
- Rule: A5. Cancellation is an A5 category, and the SDK's unified CLI and OATH tool already map `OperationCanceledException` to `ExitCode.UserCancelled` (5).
- Evidence pin (E2, .NET internal precedent): `src/Oath/examples/OathTool/OathToolApplication.cs:40–43`; `src/Cli.Commands/src/Infrastructure/YkCommandBase.cs:78–80`; `src/Cli.Commands/src/OpenPgp/OpenPgpResetCommand.cs:34`; `src/Cli.Commands/src/Otp/OtpCommands.cs:333`; `src/Cli.Commands/src/Infrastructure/ExitCode.cs:25`. Exception-filter precedent: `src/Oath/examples/OathTool/Commands/AccessCommand.cs:62`; `src/Cli.Shared/src/Device/DeviceSelector.cs:197`. Not followed: `src/YubiOtp/examples/OtpTool/Program.cs:39–41` (exit 0) and `src/Ctap2/examples/FidoTool/Program.cs:77–79` (exit 1). Current defect: `src/Management/examples/ManagementTool/Program.cs:50–57` (menu callbacks discard `ct`); `src/Management/examples/ManagementTool/Features/DeviceConfiguration.cs:84–87` (generic catch).
- Pseudocode:
```text
RunInteractiveAsync(ct):
  menu callbacks pass ct to the selector, session creation and each awaited write
SetCapabilitiesAsync(..., ct):
  catch (Exception ex) when (ex is not OperationCanceledException) -> ConfigResult.Failed(...)
RunInteractiveAsync:
  catch (OperationCanceledException) -> "Operation cancelled."; return ExitCode.UserCancelled
text: a cancelled write may already have applied; never say "rolled back"
```
- Test to add: none in-tree (no test project covers `examples/ManagementTool`). Record a scripted run with a pre-cancelled token (no device): zero selector or session calls. Do not add a validation-only test.
- T2 tag: cancellation exit contract and drain. This card follows the majority pattern for the Management tool only; YubiOtp and FIDO alignment is a separate T2 follow-up.
- E1: stop if whole-task drain (Core B5-C006) needs awaiting beyond the menu await, or if `ExitCode.UserCancelled` conflicts with a documented unified-CLI contract.

### MGMT-IMP-017: Request results and executable labels (labels A1; reboot flag A6)
- Rule: A1 for the three literal label corrections (the card marks labels no-brainer yes). A6 for the reboot flag: keep the member, because its XML doc already says it is the requested flag, not an observed requirement.
- Evidence pin (E2): no sibling for labels. Reboot context only: `py:ykman/_cli/config.py:293–298` @4ca60f70 (interface-change request; not adopted). .NET: `src/Management/examples/ManagementTool/Features/Results/ConfigResult.cs:40–43`; `Commands/InfoCommand.cs:47`; `Cli/Menus/DeviceInfoMenu.cs:65, 69`; `Cli/Menus/DeviceFlagsMenu.cs:45–48`.
- Pseudocode:
```text
InfoCommand.cs:47         WriteKeyValue("FIPS manufacturing indicator", info.IsFips ? "Yes" : "No")
DeviceInfoMenu.cs:65, 69  same label
DeviceFlagsMenu.cs:48     "Touch Eject: CCID-only mode (OTP, U2F and FIDO2 disabled); card ejected by default, touch to insert"
ConfigResult.Succeeded(rebootRequired): unchanged; XML doc keeps "requested, not observed"
```
- Test to add: none in-tree for the example. Show the rendered labels in the PR body. No validation-only test.
- E1: stop if a grep of `src/Cli.Commands` (and elsewhere) finds another consumer of the "FIPS Mode" text, or if the reboot flag is read as observed state anywhere.

### CORE-MGMT-X003: Bound factory oracle (narrow part only)
- Rule: A1 for the narrow bound-oracle part (the card marks "Yes for a genuinely bound behavioral oracle"). The reset-wire extension depends on MGMT-IMP-010 and Core B6, so it is excluded from this approval.
- Evidence pin (E2): no sibling (the Core guard is .NET-only). .NET: `src/Management/tests/Yubico.YubiKit.Management.UnitTests/ManagementSessionTests.cs:33–45` (current direct-constructor probe), `:48–57` (borrowed-connection test), `:333–358` (`FailingSmartCardConnection`). Core: `src/Core/src/Sessions/ApplicationSession.cs:233–249` (`Construct` attaches after create and disposes on attach failure), `:253–271` (`ConstructInitializedAsync`), `:409–411` (`DisposeAfterInitializationFailure`), `:491–518` and `:574` (sync `Dispose(bool)` releases the guard via `Detach`); `src/Core/src/Sessions/ConnectionSessionGuard.cs:50–56`; `src/Core/src/Devices/ConnectionInUseException.cs:57` (derives from `InvalidOperationException`).
- Pseudocode:
```text
connection = new FailingSmartCardConnection()
await Assert.ThrowsAsync<InvalidOperationException>(() => ManagementSession.CreateAsync(connection, ct))   // first attempt fails in init
await Assert.ThrowsAsync<InvalidOperationException>(() => ManagementSession.CreateAsync(connection, ct))   // second attempt reaches init, not a stale guard
assert connection.DisposeCount == 0
```
Use exact-type `ThrowsAsync`. `ThrowsAny` would also accept `ConnectionInUseException`, which derives from `InvalidOperationException`.
- Test to add: `CreateAsync_FailedInitialization_ReleasesGuardForSameConnection`.
- E1: stop if the second attempt needs a seam beyond the existing fakes. Stop too if the guard is still attached after failure: that is a Core defect, not an oracle fix. Current source releases it (`ApplicationSession.cs:491–518, 574`).

## T1 hardware qualification

| Card | What needs a device | Device/firmware/transport/OS |
| --- | --- | --- |
| MGMT-IMP-002 | Starting restriction; true, false and null matrix; NFC application access; NDEF URL retention; USB-power recovery duration; reboot request | NFC-capable YubiKey on firmware 5.7+ (versioned definition or authorized matrix); CCID and NFC transports; host OS recorded per run |
| MGMT-IMP-002 (dependency) | Settle the recovery route: `rust:management.rs:564` says "requires physical touch to enable"; `src/Management/src/DeviceConfig.cs:137` and `py:ykman/_cli/config.py:523–524` say USB power | Same key set |
| MGMT-IMP-011 | Applet-operation outcomes for Bio, FIPS and flagged keys; expected metadata at the owning module | USB-A Bio and FIPS-capable keys, firmware per row; USB or NFC named per row |
| MGMT-IMP-007 (dependency) | Exact status words for a missing lock code and wrong SCP03 keys | Dedicated SmartCard key with a configuration lock and SCP03 provisioning; firmware 5.3+ |
| MGMT-IMP-004 (dependency) | Restoration readback and failed-recovery path with a lock code | Dedicated SmartCard key, firmware 5.0+; allow-listed only |
| MGMT-IMP-010 (dependency) | Reset eligibility and outcome: Bio MPE reset, F9 retention, post-reset session state | USB-A Bio MPE key on firmware 5.6+, SmartCard only; authorized reset lane |
| MGMT-IMP-012 (only if legacy support is selected) | Legacy mode payload and byte order | NEO or YubiKey 4 (3.x–4.x) over OTP or CCID as applicable |
| Management qualification | Sequential read-only metadata; write and readback; attended touch and hotplug; native release; SCP and profile conformity; Windows HID first-open scope | Allow-listed serials per row; pinned transports; Windows token, driver and architecture for HID lanes. Serial 9681620's two-key failure and serial 125's `6A88` outcomes stay unexplained and are not reset evidence |

## T2 cross-cutting candidates

| Theme | Card(s) here | Why it is cross-cutting |
| --- | --- | --- |
| Firmware or hardware preflight versus device-authoritative route | MGMT-IMP-001, MGMT-IMP-008 | Every settings write and optional feature uses `EnsureSupports`-style gates or leaves the decision to the device. 008's public gate is auto-approved (A2); this theme can still veto it. |
| Destructive reset eligibility and terminal lifecycle | MGMT-IMP-010 | OATH already ends the session after a confirmed RESET (`src/Oath/src/OathSession.cs:670–681`). Reset eligibility is a firmware and transport question for any applet with reset. |
| Secret lifetime after failure or cancellation | MGMT-IMP-004, MGMT-IMP-005, MGMT-IMP-015 | Drain-before-zero (Core B5-C006), owner cleanup (OATH `OathToolApplication.cs:50–51, 95–107`) and fixture zeroing (`OathSessionTests.cs:43–63`) appear in several applets and tools. |
| Cancellation exit contract and drain | MGMT-IMP-016 | Patterns differ: `ExitCode.UserCancelled` in the OATH tool and in unified-CLI commands (`YkCommandBase`, OpenPgp, Otp); exit 0 in the YubiOtp example; exit 1 in the FIDO example. A5 settles only the Management tool. |
| SCP setup and wire-status provenance | MGMT-IMP-007 | Setup failures and status words are Core B6 canonical; SecurityDomain's SD-DEP-002 overlaps. |
| Device-info decoding, width and representation | MGMT-IMP-003, MGMT-XREF-001, MGMT-XREF-002 | `DeviceInfo` decoding is Core-owned and feeds every applet's metadata filters and display. Core width policy is A-C006/C016. |
| Shared malformed firmware attribute policy | MGMT-IMP-009 | `FilterDevices` silently ignores an unparseable `MinFirmware` (`YubiKeyTestInfrastructure.cs:79–83`); every `[WithYubiKey(MinFirmware = …)]` row inherits it. |
| Legacy NEO and YK4 mode support | MGMT-IMP-012 | The Management dormant primitive and OTP legacy relocation (canonical OTP card) need one decision. |

Thirteen cards carry at least one T2 tag. None of the eight themes has a decision yet.

## Coverage check

Review-map card IDs (21): MGMT-IMP-001 through MGMT-IMP-017 (17 rows), MGMT-XREF-001 (referral), MGMT-XREF-002 (referral), CORE-MGMT-X003 and Management qualification. Each appears exactly once in the Index (21 rows). No other card heading exists in the section. The shared-dependency table, the package-resolution note and the coverage ledger are not cards.

Coverage-ledger exclusions, deliberately not treated as cards:
- MGMT-XREF-003: documentation resolved; reopen only after binding or cache changes.
- CORE-MGMT-X001: accepted fixture documentation; future cache, equality and lookup facets stay with Core A. Not in the review map.
- CORE-MGMT-X002: documentation accepted; storage, raw-representation and version facets stay with Core A-C003/C016. Not in the review map.
- SD-DEP-002: canonical Security Domain referral, not a Management card.
- The six inherited MGMT IMP and XREF keys and the frozen Core boundary references: accounted for in the ledger; no added card.
- MGMT-DOC-001 through -012 and B-08 (TLV cleanup and order): accepted documentation and clean scope.
- B-06 timeout-range expansion: not selected; the 3600 and 60 SDK limits stay.
- Spectre HintPath (`src/Management/examples/ManagementTool/ManagementTool.csproj:25–28`): unnumbered support observation; not a defect and not a card. Track it only if you choose to.
- Obsolete API-design-skill module path: instruction-maintenance referral, excluded from applet engineering.
- Windows B4-C001 and B4-C007 mechanisms: superseded at the named built-in scope; open facets live in the Windows successor.
- Historical validation failures (serial 9681620; serial 125 `6A88`): preserved in the T1 table, not relabelled.
