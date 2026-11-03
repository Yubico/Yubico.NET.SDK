# Management independent review B — configuration model and write options

## Verdict

**PASS for the assigned documentation review. Required corrections: none. Optional changes: none recommended.**

The two edited files accurately distinguish replacement masks from additive changes, SDK validation
from device support, per-command authorization from persistent lock removal, and caller-owned secrets
from encoded copies. The timeout-zero explanations are corroborated by newly fetched official v1
documentation, without importing v1 range enforcement into v2. Restricted-NFC prose states the
documented power/NDEF/firmware conditions and does not promise a meaning for the unresolved false byte.
No additional wording changes are needed merely for editorial preference.

This verdict is not compilation, executable-token/API verification, hardware qualification, full
protocol conformance, or completion of deferred implementation work. Those dimensions remain separate.

## Review identity, instructions and scope

- Reviewer: fresh Sol 6.1 (`github-copilot/gpt-6.1-sol`), 2026-11-03; independent of the investigator
  and editor sessions. I checked the implementation and fetched original sources, rather than treating
  agreement with `investigation-b.md` or `edit-b.md` as evidence.
- Reviewed worktree: `/tmp/opencode/management-001-edit-b`; comparison baseline
  `da61d2e6d754b4c155cbb2579202e910986f4b3a`.
- Read root `CLAUDE.md`, worktree Management `CLAUDE.md` and README, primary campaign `workflow.md`,
  `records/style.md`, and run `plan.md`, `triage.md`, `investigation-b.md`, and `edit-b.md`.
  The root instruction file in the primary checkout and review worktree has identical SHA-256
  `2dc087582e4d3edf9376d257e76bfd3e8acfc549b27be721d44ed17a966d44e5`.
- Reviewed the available skill descriptions and loaded `workflow-verify`. Build/test/commit skills
  were not applicable: those operations were expressly excluded. No delegation occurred.
- Read both affected files completely, including unchanged declarations, setters, encoder, helper,
  cleanup paths, comments and headers. Inspected the complete source diff against the baseline.
  `git diff --stat da61d2e6` lists only these two files (100 insertions, 22 deletions).
- The only authored artifact is this report in the **primary** checkout. No source, test, API,
  implementation, investigator/editor report or workflow file was changed.

Reviewed final-file fingerprints:

| Worktree-relative path | SHA-256 |
| --- | --- |
| `src/Management/src/DeviceConfig.cs` | `688bf3ef8591d2a5b663e4e30f6c7d5bb517380996c51b19c3aec1d9ced4946c` |
| `src/Management/src/SetDeviceConfigOptions.cs` | `825976611b693a92f26d50f5d27c08b96f4999898444cf578e7c0f4943f0b4c5` |

Line references below identify those exact worktree files, not subsequently integrated primary files.

## Complete affected-unit coverage

| Unit / exact lines | Independent disposition and evidence |
| --- | --- |
| `DeviceConfig.cs:21–46`, type summary and complete example/remarks | PASS. Null omission matches every conditional encoder branch. The existing three-line example remains unchanged; surrounding prose now states the remaining-supported-USB and unlocked-configuration assumptions. The supported-mask responsibility and limited SDK checks are explicit, not a reachability guarantee. This is a composable fragment requiring an existing management session, not a complete program or executed configuration operation. |
| `DeviceConfig.cs:49–65`, constants/backing fields and comments | PASS, unchanged. Local tags, one-byte body-length bound and SDK timeout constants agree with the encoder. No normative ordering or universal transport-size claim was added. R1's NFC write-tag defect is not copied into code. |
| `DeviceConfig.cs:67–83`, USB/NFC replacement-mask XML and properties | PASS. R1 defines complete packed-bit replacement. Source `185–189` sends entire two-byte masks, with null omitted. USB `None` is rejected; NFC `None` is encoded. Documentation requests disabling NFC applications, not a temporary shipping restriction or unsupported-capability guarantee. |
| `DeviceConfig.cs:85–105`, complete auto-eject property | PASS. Inactivity, seconds, CCID-only and TouchEject conditions are supported by R1/R3. Zero-disable meaning is corroborated by R4. Source checks `>3600` at assignment, not during device I/O; zero is transmitted rather than omitted. The stated ceiling is explicitly an SDK limit, not a firmware maximum. Assigning the timeout does not set the flags. |
| `DeviceConfig.cs:107–123`, complete challenge-response property | PASS. Touch-wait context is corroborated by R3; R4 says zero resets the default, and R1 says the default is 15 seconds. Source checks `>60` at assignment and transmits zero. The public prose does not substitute v1's 255 ceiling for v2's 60 ceiling. |
| `DeviceConfig.cs:125–130`, device flags | PASS. Complete-byte replacement and null omission match source `197–198`; R1 describes the two flags and pure-CCID touch-eject condition, with replacement semantics additionally corroborated by R4. |
| `DeviceConfig.cs:132–146`, entire NFC restriction documentation/property | PASS. R2/R3 establish NFC-capable 5.7-and-later applicability, readable Yubico NDEF URL and three seconds of USB power as sufficient recovery. The summary describes a request, not guaranteed device acceptance. Source `203–204` establishes false sends zero; public prose does not say zero clears the restriction, requires reboot, queues restoration, or is ineffective. The missing field-specific SDK check is accurately disclosed. |
| `DeviceConfig.cs:148–164`, complete `Validate` contract/implementation | PASS. Exact USB-`None` rejection, null acceptance, absent width/device/firmware checks and separate setter checks all match source. It does not claim comprehensive policy, safety or verification. |
| `DeviceConfig.cs:166–228`, full `GetBytes` documentation/implementation/cleanup | PASS. Prefix and local order are exact. Reboot/current-code precede USB/NFC/timeouts/flags/new-code/restriction when present; no sorted-tag or protocol-mandated-order claim. Final result is separate owned memory. Encoded intermediate memory is cleared in the inner `finally`; each TLV is disposed in the outer `finally`. Narrow Core tracing confirms those TLVs own their copies and disposal zeroes them. |
| `DeviceConfig.cs:230–231`, helper and final newline | PASS, helper unchanged. Big-endian two-byte conversion matches its uses. The added final newline has no runtime/API effect. |
| `SetDeviceConfigOptions.cs:17–26`, type/complete ownership remarks | PASS. Options properties contain borrowed memory views, not private copies. Lifetime includes the one-shot extension's session-creation awaits. Caller cleanup after completion/fault/cancellation agrees with R7 and source; caller-retained options still reference their memory. No promise of clearing every transport-layer allocation is made. |
| `SetDeviceConfigOptions.cs:28–35`, reboot | PASS. False omits the TLV; true requests reboot contingent on accepted parsing/authorization (R1). Reboot is not factory reset. Dispose/rediscover guidance does not imply the old session becomes usable after reboot or promise a universal delay. |
| `SetDeviceConfigOptions.cs:37–45`, current lock code | PASS. R1 explicitly requires the unlock TLV in every protected command, not a prior separate command. This is neither session authentication nor persistent lock removal. Source enforces length 16 on present values and leaves correctness/requirement to the device. Null versus empty is accurately distinguished. |
| `SetDeviceConfigOptions.cs:47–56`, new lock code | PASS. R1 explicitly distinguishes sixteen zeros (disable) from any other 16-byte value (set/replace). Same-command current authorization is required when locked. Null omits the setting; empty is rejected by the session, not a removal representation. |
| Both files, headers/imports/declarations and residue/citation check | PASS within scope. Headers, imports, declarations, constants and executable bodies are unchanged on full-diff inspection. No TODO/specification URL or stale project-phase narrative was introduced. Compiler-generated record members need no invented documentation. |

### Supporting implementation checks

These are static source observations, not newly executed tests:

- Read complete `src/Management/src/ManagementSession.cs`. Lines `122–145` check disposal, the generic
  Set Config feature gate and both present lock-code lengths before encoding/writing. They do not
  check NFC capability or restriction-specific 5.7 firmware. Lines `147–160` clear the final encoded
  buffer in `finally` around the awaited write. No options field is retained by the session.
- Read complete `src/Management/src/IYubiKeyExtensions.cs`. Lines `72–82` await session creation
  before consuming options, supporting the full one-shot-operation lifetime qualification.
- Read complete Core `Tlv.cs` and `TlvHelper.cs` as narrow ownership evidence. `Tlv.cs:42–50,92–135`
  copies the value into owned encoded memory; `81–89` zeroes it on disposal.
  `TlvHelper.cs:112–137` returns a new concatenated buffer without disposing the source TLVs.
  Thus `DeviceConfig.GetBytes`'s cleanup claim is about its own intermediate encoded buffers, not
  a proof of all lower transport copies or all memory in the process.
- Read complete Core `DeviceCapabilities.cs` and `DeviceFlags.cs` to check enum meanings/widths and
  the CCID-only qualification. `Validate` still does not reject arbitrary nonzero out-of-wire-width
  enum values or compare requested capabilities against actual device support. The public limitations
  at `DeviceConfig.cs:40–44,67–83,148–155` preserve that distinction.

## Independently fetched original references

All originals below were fetched anew on **2026-11-03**, using Exa's page-fetch tool for R1–R6 and
Context7 resolve/query for R7. I inspected the actual relevant passages, not search highlights or
the investigator's quotations. Focused quotations are retained here so this report does not depend
on temporary tool-output files. Section titles identify the passages when a stable anchor is absent.

### R1 — vendor device-configuration reference

URL: <https://developers.yubico.com/yubikey-manager/Config_Reference.html>

Title/status: **YubiKey 5 Series Configuration Reference Guide**, live official developer reference;
no fixed numbered revision identified. Actor/layer: vendor device command format and behavior,
not SDK validation policy or an RFC-style conformance standard. Applicability introduction says
“YubiKey 5 Series and above, and the Security Key by Yubico”; that does not make every field supported
on every product meeting a generic firmware threshold.

Independently checked passages:

- **List of Capabilities:** “Each Application is defined as a single bit and is enabled if the bit is
  set and disabled if not.” “The Enabled Capabilities are always masked with the Supported
  Capabilities.” **Enabled USB Capabilities** under Writing Information says to supply “the complete
  list of packed bits.” These support replacement semantics, not additive enablement or SDK checking.
- **Auto-Eject Timeout:** “period of inactivity (in seconds)” and “only meaningful if the Device Flag,
  MODE_FLAG_EJECT, is set.” **Device Flags** describes `MODE_FLAG_EJECT` as “In pure CCID mode
  eject/inject the card with button.”
- **Challenge-Response Timeout:** “The default is 15 seconds.” Writing sections specify two-byte
  big-endian auto-eject and one-byte challenge-response values. They do not establish 3600/60 firmware
  maxima or the special meaning of zero; R4 supplies official sibling corroboration for zero.
- **Set Configuration Lock:** “If the payload is the Byte 00 repeated 16 times, the Configuration
  Lock is disabled”; otherwise “the value is stored as the Configuration Lock.”
- **Unlock Configuration Lock:** correct 16-byte payload; may be anywhere in the same list, but
  “can not be part of a separate command.” “All interface commands requiring the Configuration Lock
  must contain an Unlock Configuration Lock TLV.” This resolves the per-command/session distinction.
- **Writing Information / Reset:** one-byte total length followed by TLVs; reboot occurs “if all the
  other TLVs have been correctly parsed.” “missing/wrong Configuration Lock will produce an error
  and not reboot.” This is configuration-triggered reboot, not factory reset.

Citation defect independently confirmed: **Enabled NFC Capabilities** in the write section prints
`TAG = 0x0c` and incorrectly repeats “Enabled USB Capabilities.” The read table gives enabled NFC
`0x0e`, while Reset uses `0x0c`. R6 corroborates the local `0x0e` encoder, but does not repair this
vendor reference or establish complete firmware conformance. The reference does not define tag `0x17`.
Neither this typo nor the absence of `0x17` is concealed by the review verdict.

### R2 — official technical manual, Restricted NFC

URL: <https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-firmware-5.7.html#restricted-nfc>

Title/status: **YubiKey Technical Manual**, 5.7/5.6 firmware-specific page, live and including later
5.7.4 material; no immutable manual revision identified. Actor/layer: product behavior and activation,
not a byte-level SET CONFIG definition.

The **Restricted NFC** section says the mode is enabled by default on new NFC keys with “5.7 and
later firmware.” It says “the only permitted action via the NFC connection is reading the URL
configured by Yubico on the NDEF tag set by Yubico.” For recovery: “plug the key into USB power such
as a USB charger or computer USB port for 3 seconds. This action is sufficient to disable Restricted
NFC mode.” It also says the restriction can be re-enabled using `ykman config nfc`.

This directly supports power rather than touch/authenticated use, and avoids the overbroad claim
that the device permits no NFC response at all. It does not define the effect or timing of a zero
payload for tag `0x17`.

### R3 — official ykman CLI guide

URL: <https://docs.yubico.com/software/yubikey/tools/ykman/Config_Commands.html>

Title/status: **CONFIG Commands — YubiKey Manager (ykman) CLI Guide**, live official guide; a fixed
tool release was not identified in this page's retrieved prose. Actor/layer: CLI user/tool behavior,
corroborating product conditions, not authority to import extra CLI policy into v2.

- **ykman config nfc**, `--restrict`: “Re-enable by USB power. Available for YubiKeys with firmware
  version 5.7 and later.”
- **ykman config usb**, `--autoeject-timeout`: “Implies --touch-eject”; `--touch-eject` is CCID only.
  That implication belongs to the CLI. This SDK's timeout setter does not set `DeviceFlags`.
- `--chalresp-timeout`: timeout “when waiting for touch response to the challenge-response from the
  OTP application.” This corroborates the touch-wait qualification in the edited property summary.

### R4 — official v1 .NET API, timeout/mask corroboration

URL: <https://docs.yubico.com/yesdk/yubikey-api/Yubico.YubiKey.IYubiKeyDevice.html>

Title/status: **Interface IYubiKeyDevice**, generated official reference for `Yubico.YubiKey.dll`,
the **v1** surface; no fixed package version identified. Actor/layer: v1 callers. This is corroborating
documentation, not authority for v2 enforcement, v2 cache updates, automatic reboot, or session policy.

- **SetAutoEjectTimeout(int):** “A value of `0` means that the timeout is disabled (the smart card will
  not be ejected automatically).” The same member documents the wider ushort range for v1.
- **SetChallengeResponseTimeout(int):** “The value must be in the range 0-255, where 0 resets the value
  to its default.” Combined with R1's 15-second default, this corroborates `DeviceConfig.cs:109`.
- **SetDeviceFlags(DeviceFlags):** “A set flag means that the setting is enabled. Otherwise, the
  capability is disabled.” **SetEnabledNfcCapabilities** similarly says set bits enable, otherwise
  disable. Neither changes the local SDK limits or supplies a firmware maximum.
- **SetIsNfcRestricted(bool):** “Set this value to true to restrict NFC, false to allow NFC.” I checked
  this apparently stronger sibling claim too. It describes that v1 API, not the precise firmware
  handling of our `17 01 00` request; it does not resolve R6's different emission behavior. The edited
  v2 prose correctly does not inherit the promise or the v1 immediate cached-property update.

These original zero passages match the editor's factual clauses. I have not independently measured
defaults or acceptance ranges on any device, and do not transfer the v1 methods' lock/cache/reboot
postconditions to v2.

### R5 — official v1 .NET command reference

URL: <https://docs.yubico.com/yesdk/yubikey-api/Yubico.YubiKey.Management.Commands.SetDeviceInfoBaseCommand.html>

Title/status: **Class SetDeviceInfoBaseCommand**, generated official v1 reference for
`Yubico.YubiKey.dll`; no fixed package version identified. Actor/layer: sibling host command API,
not independent firmware specification.

**SetLockCode(ReadOnlySpan)** states: “A 16-byte lock code. A value of all zeros (16 bytes) will clear
the code.” **ApplyLockCode** describes temporarily applying the code. These corroborate R1; they do
not replace R1's stronger same-command requirement or imply a session-wide unlock in this SDK.
The reference's `ResetAfterConfig` is explicitly reboot after successful configuration, not factory reset.

### R6 — official published sibling encoder

URL: <https://developers.yubico.com/yubikey-manager/API_Documentation/_modules/yubikit/management.html>

Title/status: **yubikit.management — yubikey-manager 5.9.0 documentation**, explicitly versioned
published Python source. Actor/layer: sibling host encoder, corroboration only, not a normative
firmware definition or evidence of this SDK's hardware behavior.

I checked `TAG_NFC_ENABLED = 0x0E`, `TAG_REBOOT = 0x0C`, `TAG_NFC_RESTRICTED = 0x17` and the complete
`DeviceConfig.get_bytes` unit. Its local order matches the described order. Restriction emission is
conditional on truthiness: `if self.nfc_restricted: ... Tlv(TAG_NFC_RESTRICTED, b"\1")`.
It omits false, whereas our source explicitly sends zero. This confirms the evidence gap rather
than proving either that zero restores NFC or that our encoder is wrong. The edited public property
carefully stops at the certain encoded value and documented USB-power recovery route.

### R7 — Microsoft memory-lifetime guidance through Context7

URL: <https://github.com/dotnet/docs/blob/main/docs/standard/memory-and-spans/memory-t-usage-guidelines.md>

Title/status: **Memory<T> and Span<T> usage guidelines**, current Microsoft guidance on `main`, not a
pinned runtime build. Context7 library independently resolved as `/dotnet/docs`. Actor/layer: managed
library/application memory consumers, usage guidance rather than device behavior.

**Rule 4** says a Task-returning method must not use the memory after its Task reaches a terminal
state; “terminal state” includes completed, faulted and canceled. This supports the caller cleanup
boundary in options remarks. Actual copying, retention and zeroing claims were separately checked
against the SDK source above, not inferred merely from recommended framework practice.

## Required corrections

None in the assigned files at the recorded fingerprints. In particular, I found no public guarantee
of comprehensive validation, unsupported firmware maximum, all-clear verification claim, false-byte
restoration contract or full device-support policy requiring correction.

## Optional feedback

None recommended. The direct, calm and precise voice preserves useful local qualifications without
turning public XML into an audit transcript. Do not remove the SDK-limit, support-check or false-byte
distinctions merely to shorten the prose.

## Verification and limits

- Fresh `git diff --check da61d2e6` in the review worktree exited **0**, with no output.
- Full-diff manual inspection found only XML-documentation edits plus the final newline in
  `DeviceConfig.cs`; no changed executable body, import, directive, constant, signature or example
  statement was visible. This is not a mechanical C# token/API proof. The coordinator owns that gate.
- No build, test, example compilation, XML/cref compiler check, formatter, hardware operation, commit
  or delegation was run. No passing test or compilation result is claimed. The single unchanged XML
  fragment still requires the coordinator's compile-only validation with an explicitly typed session.
- Core was read narrowly for ownership/enum evidence, not audited as a module. Other editors' files,
  test assertions, guides and platform transport internals are not approved by this two-file verdict.
- MGMT-IMP-001 (field-specific support gates), MGMT-IMP-002 (false restriction semantics/timing),
  MGMT-IMP-003 (mask width/device support policy) and MGMT-IMP-005 (failure/cancellation test evidence)
  remain deferred. No implementation deficiency was converted into a promised permanent contract.
- Exact firmware maxima, all-device zero/default behavior, NFC false behavior and reboot timing were
  not hardware-qualified. Original vendor/sibling passages have the authority and version limits
  stated above. Reopen this XML if those contracts or the relevant implementation change.
