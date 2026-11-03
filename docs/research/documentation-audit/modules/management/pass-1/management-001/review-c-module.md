# Management independent review C — module guides and ManagementTool

## Verdict and scope

**Required corrections: five. Not yet PASS. Optional corrections: none.**

Reviewer: fresh GPT-6.1 Sol (`github-copilot/gpt-6.1-sol`), 2026-11-03. Reviewed the C diff against
`da61d2e6d754b4c155cbb2579202e910986f4b3a` in `/tmp/opencode/management-001-edit-c`.
Only the 17 changed files under `src/Management` are review targets. C2 owns external shared files;
A/B-owned source and tests were read only as implementation/assertion evidence, not reviewed for acceptance.
This report is the only file written by this reviewer, in PRIMARY.

Read root `CLAUDE.md`, Management README/CLAUDE and test instructions, the PRIMARY plan, triage,
investigation C, edit C, campaign workflow and approved style. Read narrow Core and Tests.Shared guidance
for dependencies. Loaded `docs-module`, `workflow-verify` and `domain-secure-credential-prompt`.
No build, test, hardware operation, example execution, commit or delegation occurred. Compiled-snippet,
executable-token/directive and API-preservation checks belong to the coordinator and are **not claimed** here.

The coordinator supplied the secret-scope and reboot candidates during review. Both were independently
checked against executable source and the original configuration reference before accepting them below.
The unused/not-retained user-presence-prompt correction matches the current constructor/factory path.

## Required corrections

### C-MOD-R1 — dispose the SCP parameters created by the contributor example

- **Location:** `src/Management/CLAUDE.md:242–258`, complete “Low-Level: CreateManagementSessionAsync” block.
- **Problem:** `ScpKeyParameters = Scp03KeyParameters.Default` creates a disposable parameter object and
  owned key arrays, but no variable or disposal scope retains responsibility for them. Disposing the
  returned session does not dispose these borrowed initialization parameters. The block currently teaches
  connection/session cleanup while omitting another resource it creates.
- **Independent source evidence:** `Core/src/Protocols/SmartCard/Scp/Scp03KeyParameters.cs:45–57` makes
  `Default` a fresh `new(...)`, with `Dispose()` calling `Keys.Dispose()`;
  `StaticKeys.cs:52–61,83–99` clones three key arrays and zeroes them only on disposal.
  `ManagementSession.InitializeAsync` passes parameters to `ConfigureProtocolAsync` without taking
  ownership; `ApplicationSession.cs:359–397,480–565` establishes SCP and later disposes protocol/owned
  connection, not the input parameters. `SessionCreationOptions.cs:35–39` corroborates caller ownership.
- **Required edit:** introduce `using var scpParameters = Scp03KeyParameters.Default;` before session
  creation, assign that variable in options, and keep disposal order explicit (session ends before its
  outer parameter scope). State that the parameters are caller-owned and borrowed for initialization.
  Preserve the real factory and both awaited reads. Do not remove SCP from the example or substitute
  a fake/no-op parameter source to make the lifetime problem disappear.
- **Scope/limit:** this is a documentation-example ownership defect, not a finding of an SDK session-key
  leak. The demonstrated keys are public defaults; their sensitivity does not change the type's ownership
  contract or the need for a correct reusable example pattern. Related group: MGMT-DOC-006/011.

### C-MOD-R2 — distinguish Management command credentials from optional SCP keys

- **Location:** `src/Management/README.md:205–209`, complete User interaction opening paragraph.
- **Problem:** “The only secret it handles is the 16-byte configuration lock code” is too broad for a
  module whose factories accept optional SCP key parameters. It can also make “no management key” sound
  like no cryptographic key material is ever involved in initialization.
- **Independent source evidence:** `ManagementSession.cs:79–108` accepts and forwards `ScpKeyParameters`;
  `ApplicationSession.cs:382–388` uses them to establish SCP; `Scp03KeyParameters.cs:18–43` contains
  static ENC/MAC/DEK material. The ordinary configuration operation separately reads current/new lock
  memory at `ManagementSession.cs:133–145`. The constructor at `:47–49` uses the base overload with no
  prompt, so the adjacent not-retained/invoked/disposed claim is accurate and should remain.
- **Required edit:** narrow the claim to the command-level configuration credential, and explicitly
  distinguish optional caller-owned SCP initialization keys. For example, ordinary Management commands
  do not need a PIN/PIV management key; configuration protection uses the 16-byte lock code, while optional
  SCP uses separately supplied key parameters. Do not imply ownership transfer or prompt retention.
- **Scope:** factual contract qualification, not a new secret-input feature. MGMT-DOC-006/011.

### C-MOD-R3 — qualify the reboot effect by whether the requested reboot occurs

- **Location:** `src/Management/README.md:244–246`, Constraints reboot bullet.
- **Problem:** “SetDeviceConfigAsync with Reboot = true terminates every session” states an unconditional
  outcome from a request flag. A validation, authorization or device failure can prevent the reboot.
  Nor does this source mark all managed session objects disposed when encoding that flag.
- **Independent source/reference evidence:** `DeviceConfig.GetBytes:125–126` adds the reboot TLV;
  `ManagementSession.SetDeviceConfigAsync:127–159` validates, encodes, sends and clears the request with
  no global session-state transition. Original reference E1, Writing Information / Reset, says the TLV
  forces reboot **if all other TLVs have been correctly parsed**, and a missing/wrong configuration lock
  produces an error and does not reboot.
- **Required edit:** describe `Reboot = true` as a request; **if the requested reboot occurs**, existing
  sessions/connections must no longer be reused. Retain the dispose → rediscover intended key → fresh
  scopes procedure and capability-change blast-radius warning. Do not promise that managed session
  objects are automatically invalidated/disposed.
- **Scope:** align the guide with the request/result distinction already correctly explained by
  `ConfigResult` and the tool guide. MGMT-DOC-002/006.

### C-MOD-R4 — document real top-level help separately from ignored command-specific help

- **Location:** `src/Management/examples/ManagementTool/README.md:78–80`.
- **Problem:** “Program recognizes only info and config as the first command token” and unqualified
  “help flags are not parsed” contradict the actual dispatcher. The warning about `info --help` itself
  is correct and useful.
- **Independent source evidence:** `Program.cs:34–42` switches on the first token and recognizes
  `info`, `config`, **`-h`, `--help`, and `help`**. Only the first token is used; command handlers get no
  remaining arguments. `InfoCommand.ExecuteAsync` selects a device, opens an owned session and queries
  metadata; thus `info --help` really can perform the read task instead of showing command help.
- **Required edit:** state that the data commands are `info`/`config`, top-level `-h`/`--help`/`help`
  print usage, and extra command arguments are ignored. Preserve the command-specific-help warning and
  menu route for writes. No executable parsing/help-string changes are requested in Pass 1.
- **Limit:** top-level usage dispatch is not a claim of zero device activity: monitoring starts before
  dispatch. MGMT-DOC-012; MGMT-IMP-014 remains deferred.

### C-MOD-R5 — do not label a unit-plus-integration command “integration tests only”

- **Location:** `src/Management/tests/CLAUDE.md:81–82`.
- **Problem:** `dotnet toolchain.cs -- test --integration --project Management` includes Management unit
  projects as well as integration projects. The updated command reaches the hardware tests, but its label
  still misstates the selected suite.
- **Independent source evidence:** `scripts/toolchain/Context.cs:44–50` concatenates unit and integration
  project lists when `--integration` is present; `TestTargets.cs:21–24` selects from that combined list;
  `Discovery.cs:113–118` applies the project matcher rather than filtering to integration projects.
- **Required edit:** simplest correction: label it “Run unit and integration tests (requires YubiKey).”
  If integration-only execution is desired instead, use and source-check a specific integration-project
  selector. No runner command was executed by this reviewer.
- **Scope:** task usability/command description, MGMT-DOC-008/011. The separately filtered real-class
  command and unit-only command are not implicated by this finding.

## Accepted semantic review results

Apart from the required corrections above, the following changed full units are supported by the
inspected source/assertions and independently fetched references. These are documentation conclusions,
not evidence that deferred behavior was fixed or any test/device operation passed.

| Units | Review result and evidence |
| --- | --- |
| Module entry point and capability task | Bounded starter session ends before alternatives. Query/support guard/full replacement write/reboot scope remain real useful tasks. The rescan is explicitly not identity/readiness proof. `DeviceConfig.GetBytes` omits nulls, writes whole masks and only rejects exact USB None through `Validate`; E1 independently specifies replacement bits. |
| Lock procedure and complete examples | Current-code authorization is per command; all-zero **sixteen-byte** replacement removes protection, not an empty memory value. Setting and removal examples use actual current options APIs and independent `finally` zeroing; setting first persists the code to an external secret store. Source lengths/encoding/encoded-buffer finally and E1 agree. No lock-recovery promise remains in prose; executable UI remains deferred. |
| Timeouts/flags | 3600/60 are assignment-time SDK ceilings, not firmware maxima. E1 supports inactivity, default 15 seconds and pure-CCID touch eject; E4 independently specifies zero/no-auto-eject and zero/default-challenge semantics. The guide does not copy ykman's convenience behavior that implicitly sets TouchEject into the SDK setter. |
| Restricted NFC | NFC-capable 5.7+, readable NDEF URL and three seconds USB power including charger are supported by E2/E3. False is accurately described as an encoded zero without inventing firmware recovery or reboot/touch prerequisites. Generic 5.0 write check is not field-specific support enforcement. |
| Combined reset and complete reset XML task | Bio MPE applicability, FIDO/PIV/shared PIN destruction and retained F9 match E3/E5/E6; firmware-only helper is not support discovery. SmartCard dispatch/5.6 generic gate are executable source facts. Family gate, post-reset lifecycle and lock recovery are not claimed implemented. Query → result guard → real reset → result handling is retained. |
| Initialization/effective firmware/user prompt | Actual factory signatures and source excerpt match. Protocol preparation precedes transport probe; metadata resolution precedes optional SCP; override does not skip probes; non-cancellation probe failures fall back. `FeatureDeviceInfo` is not enforced on reads, while write/reset use effective feature gates. Major-zero bypass differs from display qualifiers. Management does not pass UserPresencePrompt to its base constructor. |
| Lifetimes/transport selection | Direct factories borrow connections, assertion options do not reconnect, and failed direct creation still leaves connection disposal with the caller. Device extensions own hidden connections; one-shot/helper alternatives have non-overlapping scopes. Registry exclusion is process-local and across proven known groups; session guard differs from physical connection lease. Best-effort native finalization is not deterministic cleanup. Sources: extensions, ApplicationSession, DeviceConnectionRegistry, ConnectionSessionGuard and UsbSmartCardConnection. R1 is the remaining SCP-input ownership correction. |
| Contributor backend/reader/TLV explanations | Backends implement writes/internal mode; shared reader bypasses backend and reset branches to SmartCard. DeviceInfoReader implements counted pagination, duplicate more-data rejection, exact declared length, inbound OTP CRC and page bound. Local write order is not promoted to a normative ordering rule; vendor NFC write-tag contradiction is preserved. No obsolete backend-read implementation/no-op replacement is presented as current code. |
| Test rows/cache/filter/pin examples | Attribute yields one placeholder, state binds first matching authorized device, callback receives cached state metadata, and operation transport needs explicit concrete pin/assertion. Multiple rows are not an all-device matrix. USB availability and enabled PIV over USB-or-NFC are separate predicates. Full useful read/assert snippets retained; metadata tests are not biometric-operation/FIPS-approval/reset-eligibility evidence. |
| Restoration and test proof | New no-reboot timeout example saves exactly its changed field, checks restorable SDK range/unlocked prerequisite, changes the value, reads it, attempts restoration in finally and reads restored value. Prose correctly limits this to persistence, notes restoration/failure-provenance limits and requires fresh scopes after actual reboot. Existing broad exception tests and cleanup/restoration gaps are not presented as security or hardware proof. Reconfiguration assertions concern completed scans only. |
| ManagementTool menus/selector/help/results/UI | Active static filter really selects SmartCard-capable records; unused alternate selector is not a behavior fix. All changed menu/feature comments were read with whole methods and complete XML blocks. NFC helper is unreachable from menu/dispatch. ConfigResult carries request flag, not reboot observation. FIPS mode/reset/recovery/transport display strings remain misleading executable limitations, explicitly qualified rather than silently rewritten. R4 corrects the remaining prose overstatement. |
| Tool secrets/cancellation | TextPrompt<string>/normalized string copies survive byte-array zeroing; confirmation prompt can throw before first array enters cleanup. Returned caller-owned arrays have finally cleanup in inspected menu paths; fixed-time comparison does not imply erasure of strings. Menu callbacks drop console token and feature/metadata catches include cancellation. Prose correctly leaves these as sample limitations, not SDK behavior fixes. Lock-prompt XML now performs a real authorized write and retains finally. |

Complete changed Markdown files were read, including all fences; all five module XML `<code>` examples
were read as complete blocks. The manual SmartCard and one-shot alternatives are independent scopes,
not a combined program. Contributor factory/backend declarations are explicitly source excerpts/sketches,
not compilable consumer tasks. No no-op stub or truncation was accepted as meaningful task validation.

## Independent original-reference evidence

Accessed **2026-11-03** using Exa fetches of the exact official URLs below (E2 was independently searched
and then fetched from the original page). Live vendor pages expose no immutable publication revision in
the fetched content; none is invented here. Excerpts are the durable focused evidence. Vendor product/API
documentation is not a claim of full protocol conformance or a current-v2 signature authority.

| ID / original source | Actual passage and application |
| --- | --- |
| E1 — [YubiKey 5 Series Configuration Reference Guide](https://developers.yubico.com/yubikey-manager/Config_Reference.html), Concepts; Writing Information / Enabled USB Capabilities, Set/Unlock Configuration Lock, Reset | Enabled bits are supplied as the “complete list of packed bits.” Lock payload is 16 bytes; byte 00 repeated 16 times disables it. Unlock must be in the same interface command; every protected command must contain it. Reset TLV forces reboot “if all the other TLVs have been correctly parsed”; missing/wrong lock produces an error and no reboot. Auto-eject is inactivity, meaningful with EJECT; device flags specify “In pure CCID mode.” Default challenge timeout is 15 seconds. The contradictory NFC write 0x0C row is present alongside reboot 0x0C/read NFC 0x0E; it is not grounds to change the encoder. |
| E2 — [YubiKey Technical Manual / 5.7–5.6 Firmware Specifics, Restricted NFC](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-firmware-5.7.html#restricted-nfc) | “the only permitted action via the NFC connection is reading the URL configured by Yubico on the NDEF tag”; plug into USB power “such as a USB charger or computer USB port for 3 seconds”; “This action is sufficient to disable Restricted NFC mode.” Mode applies to NFC 5.7+ and can be re-enabled using ykman. Supports the recovery/NDEF qualification, not false-byte semantics. |
| E3 — [ykman CONFIG Commands](https://docs.yubico.com/software/yubikey/tools/ykman/Config_Commands.html), config nfc/reset/set-lock-code | `--restrict`: “Re-enable by USB power,” firmware 5.7+. Reset “only used with the YubiKey Bio Multi-protocol Edition.” Configuration lock protects configuration, not adding/removing OATH credentials. Vendor-tool help does not establish v2 host-side validation. |
| E4 — [official v1 SDK user manual / FIDO U2F commands](https://docs.yubico.com/yesdk/users-manual/application-u2f/u2f-commands.html), Get device info / Table 1; Set device info / Input | Tag 06: “if 0, no auto-eject”; tag 07: “if 0, default.” Optional set fields are left as-is if not supplied; changing an existing lock requires current and new code. This is official legacy wire/data documentation, not the v2 API/range contract; v2 setter ceilings come from current executable source. |
| E5 — [YubiKey Technical Manual / Bio Series Specifics](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-bio-specifics.html#resetting), Resetting | Reset deletes credentials, PIN and fingerprint templates. With both PIV/FIDO enabled on Bio MPE, native resets are replaced by a single reset for both; using the MPE command on non-MPE Bio produces an error. Supports family distinction, not firmware-only eligibility or configuration-lock recovery. |
| E6 — [official v1 DeviceResetCommand API](https://docs.yubico.com/yesdk/yubikey-api/Yubico.YubiKey.Management.Commands.DeviceResetCommand.html), Remarks | “only available on YubiKey Bio Multi-protocol Edition keys”; deletes FIDO2 credentials/fingerprints/shared PIN, PIV keys/certificates “except the F9 attestation slot,” user PIV data; restores PIV PUK/management-key defaults. Supports narrower destructive scope; does not document generic Management-lock recovery. Not a current-v2 factory signature source. |
| E7 — Context7 `/dotnet/docs`, [dotnet run Arguments](https://github.com/dotnet/docs/blob/main/docs/core/tools/dotnet-run.md) | Tokens after literal `--` are forwarded as application arguments. Independently resolved library ID and queried current documentation; supports the three root-relative tool invocations without executing them. |

No cited source here settles what a false tag-0x17 request does across power/reboot boundaries, generic
Management configuration/lock erasure after combined reset, or any device/platform qualification. Those
remain the triage's deferred evidence gaps. Source assertions were inspected, not executed.

## Reviewed-file manifest

SHA-256 of exact reviewed working-tree bytes, computed after full semantic reading. `R*` denotes a
required correction above; `PASS` is semantic documentation acceptance only, subject to coordinator gates.
All paths below are relative to the repository root.

| Changed path | SHA-256 | Disposition |
| --- | --- | --- |
| `src/Management/CLAUDE.md` | `29c8049368051eab08f579be2131c1a3f7544cfd1e36e85b28bb67b7e1c778ee` | R1; remaining changed units accepted |
| `src/Management/README.md` | `b3033f873263deeb04e4b9e73d983684dc406e6ec7ad815b7c0bc2d2deb28c12` | R2/R3; remaining changed units accepted |
| `src/Management/examples/ManagementTool/Cli/Menus/CapabilitiesMenu.cs` | `065b1e178554acb300bcae5ee870015b23030f7e8e3a6258238f2cc12b8ec07b` | PASS |
| `src/Management/examples/ManagementTool/Cli/Menus/DeviceFlagsMenu.cs` | `36b58d173fe16387c109d8a9b4f6657e8942a4b43feefda4ac860248e51bf2ec` | PASS |
| `src/Management/examples/ManagementTool/Cli/Menus/DeviceInfoMenu.cs` | `032dde7346ebaddc3f5872cb59a92b19304aaa9d5f43b85c9b4ad4dc4c2b7d7a` | PASS |
| `src/Management/examples/ManagementTool/Cli/Menus/LockCodeMenu.cs` | `d01e6b1b3c284c1e81c5f0b6a656995bef89da53346349e6354955b83fa041d7` | PASS |
| `src/Management/examples/ManagementTool/Cli/Menus/ResetMenu.cs` | `f014f36c32727ad0945a3c66570a5d3c6170c8b4e4bde1c3e63294831304c3be` | PASS |
| `src/Management/examples/ManagementTool/Cli/Menus/TimeoutsMenu.cs` | `c7ffc4c8b074c91c467bc8f25aab68906a7be7564269462869f9887a47ce108e` | PASS |
| `src/Management/examples/ManagementTool/Cli/Prompts/DeviceSelector.cs` | `dad8bfdac220b1a231107f3df1dee4a48ca39565538aa58d3b994ee5a03233b2` | PASS |
| `src/Management/examples/ManagementTool/Cli/Prompts/LockCodePrompt.cs` | `5b1ed2ac13617d14fc87d9e2af6d7815c99d861a18dd2e17818c12dea699a9bc` | PASS |
| `src/Management/examples/ManagementTool/Features/DeviceConfiguration.cs` | `a3256fb34e1342a936e1a3720f6c9d359465d3219fac8152ecc86013348d630a` | PASS |
| `src/Management/examples/ManagementTool/Features/DeviceInfoQuery.cs` | `f580637352b8c322ccdf4d685300eaddde3f699d84b92fdfcb28f6194474caef` | PASS |
| `src/Management/examples/ManagementTool/Features/DeviceReset.cs` | `5f166b482cbd013d98bf64c47849a65283b093fa78975d8e9a8798fdb38b4902` | PASS |
| `src/Management/examples/ManagementTool/Features/Results/ConfigResult.cs` | `a613d0aa8c4192e0c5b6ad966cc52651fde64feb530dc6b66286db5b7e1d20d2` | PASS |
| `src/Management/examples/ManagementTool/Program.cs` | `ca49ad5bc8f7eebf82978692da18c6ec857e4278ec0c188bb5bff38b5f712586` | PASS |
| `src/Management/examples/ManagementTool/README.md` | `35b8725c97f779a449cb3968e15614ac182d1da4135c1b31dcd3a29a906c803a` | R4; remaining changed units accepted |
| `src/Management/tests/CLAUDE.md` | `7f40bdc2a6b5704acb6ff22fe4850aa530ceb52998b4719c5c4a5a955ecc702b` | R5; remaining changed units accepted |

Fresh read-only check: `git diff --check da61d2e6 -- src/Management` in the C worktree exited 0 with no
output. This checks diff whitespace only; it is not a build, scoped formatter, token comparison, compiled
snippet or hardware result. Corrections change the reviewed hashes and need focused re-review before
claiming PASS. All existing MGMT-IMP and MGMT-XREF deferred work remains deferred.
