# YubiOTP: Pass 2 classification
Classifier: one dedicated subagent, 2026-11-14. Source: master lines 4744–5548 at 52d34ca65. Policy: pass-2-review-policy.md (approved). Final per-card calls: `../pass-2-review-ledger.md`.
Sibling pins: yubikey-manager @ 4ca60f70 (yubikit/yubiotp.py, ykman/_cli/otp.py, yubikit/management.py; clean); yubikit-android @ f4626856 (yubiotp/src/main/java/com/yubico/yubikit/yubiotp/*; untracked yubiotp/.classpath not used); yubikey-manager-rust-auto @ 90940e9b (crates/yubikit/src/yubiotp.rs, crates/ykman-cli/src/otp.rs; clean). python-fido2 not used (YubiOTP has no FIDO surface). .NET evidence at 52d34ca65 (worktree clean apart from the two policy drafts).

## Summary
| Disposition | Count | Cards |
| --- | --- | --- |
| A1 | 2 | OTP-IMP-001, OTP-IMP-009 |
| A2 | 0 | — (A2 corroboration holds for OTP-IMP-001; A1 is the first matching rule) |
| A3 | 0 | — (A3 corroboration holds for OTP-IMP-009; A1 is the first matching rule) |
| A4 | 0 | — (floor splits in OTP-IMP-006 are not auto: stricter floors break documented legacy use, E1) |
| A5 | 0 | — (A5-shaped sub-step under OTP-IMP-003 stays in the queue) |
| A6 | 1 | OTP-DEP-005 (docs only; target redirected to the active guide) |
| Interview (gated) | 7 | OTP-IMP-002 (G5), CORE-OTP-X004 (G5), OTP-IMP-004 (G5), OTP-IMP-005 (G3), OTP-IMP-007 (G5), OTP-IMP-008 (G5), OTP-IMP-010 (G5) |
| Interview (no gate, no rule fits) | 3 | OTP-IMP-003, OTP-IMP-006, OTP-IMP-011 |
| T1 | 1 | OTP-IMP-012 |
| T2 tag (cards also tagged) | 8 | OTP-IMP-003, OTP-IMP-004, OTP-IMP-005, OTP-IMP-006, OTP-IMP-007, OTP-IMP-010, OTP-IMP-011, CORE-OTP-X004 |

Notes:
- G2 is not triggered: no listed governing spec covers YubiOTP. The legacy personalization manual is cited as evidence only.
- G4 is suspended (AG-2).
- The policy has no path for a card that is ungated and matches no A-rule. The three "no rule" cards are queued. Dennis may instead approve a rule for them.

## Index
| Card | Severity | Disposition | One-line rationale |
| --- | --- | --- | --- |
| OTP-IMP-001 | Medium | A1 | HOTP divisor 16 and range; Python, Android and Rust all divide by 16. Firmware qualification is T1. |
| OTP-IMP-002 | Medium | Interview (G5) | Full 45-entry map and reset contract undecided; siblings forward raw bytes. |
| OTP-IMP-003 | Medium | Interview (no rule) | Reorder is A5-shaped (Oath precedent); disposed-setter and output-ownership choices remain. |
| CORE-OTP-X004 | Medium | Interview (G5) | Teardown failure after a valid HMAC: fault and clear, or a new diagnostic contract. |
| OTP-IMP-004 | Low | Interview (G5) | Keyboard-trigger bits reported as RequireTouch policy; no sibling prompts for SmartCard challenges. |
| OTP-IMP-005 | Medium | Interview (G3) | Rust and .NET share permissive short-status handling; sequence interval undecided. |
| OTP-IMP-006 | Medium | Interview (no rule) | Floors split 2–2 for HOTP, static and update; A4 would break documented legacy use (E1). |
| OTP-IMP-007 | Medium | Interview (G5) | Intent-bearing update vs full-mask replacement; sibling CLIs disagree on omitted codes. |
| OTP-IMP-008 | Low | Interview (G5) | Null disable and longest-prefix are capabilities no sibling has; undefined NDEF type. |
| OTP-IMP-009 | Medium | A1 | Translate with the existing layout constructor; Python ykman and Rust CLI do the same. Input policy unchanged. |
| OTP-IMP-010 | Low | Interview (G5) | OtpTool scope: small demonstrator or ykman-like; help-text and silent swap-code mismatches. |
| OTP-IMP-011 | Medium | Interview (no rule) | Destructive both-slot fixture; sequential cleanup; restoration needs authorization. |
| OTP-IMP-012 | none assigned | T1 | Challenge and verifier qualification; no defect severity. |
| OTP-DEP-005 | Low | A6 | Inventory resolved (device info via Management; no typed OTP FIPS query or legacy mutator). Docs only. |

## Interview queue (sorted High, then Medium, then Low)
No High cards in this section.

### OTP-IMP-002: Scan-map size and reset contract
- Gate(s): G5. Named better option: a typed full-map operation with a separate reset. Python and Rust forward raw bytes with no size check.
- Severity / certainty: Medium. Source confirmed. The 45-entry vendor map is cited from frozen evidence and was not re-verified. Reset behavior and the firmware gate are unverified.
- Decision: Accept a full 45-byte scan map, with reset as (A) a separate operation or (B) a sentinel in the same call?
- Options:
  - A. Separate `SetScanMap` (45 bytes) and `ResetScanMap` behind the accepted firmware gate. Two calls, no sentinel to misuse. **Sia's lean: A**, because reset is a different intent and the sentinel is unverified.
  - B. One call with a reset sentinel. Wire semantics must be verified on a dedicated key first.
  - C. Keep the 38-byte rejection and document it. No full map is possible, and the 38 limit is wrong against the vendor map.
- Evidence: .NET `src/YubiOtp/src/YubiOtpSession.cs:283–308` (38-byte check at 290–295, no feature gate); `src/YubiOtp/src/YubiOtpConstants.cs:33–35` (scan map reuses ScanCodesSize). Python `yubikit/yubiotp.py:870–877` @4ca60f70 (forwards bytes). Rust `crates/yubikit/src/yubiotp.rs:1273–1278` @90940e9b (forwards bytes). Android: no scan-map API; `yubiotp/.../YubiOtpSession.java:112` is a constant only @f4626856.

### OTP-IMP-003: Secret owner guards at acquisition
- Gate(s): none. No sibling shares the defect (Python builds immutable bytes and has no erasure path). No whole-card rule fits: the reorder is A5-shaped, but the disposed-setter and output-ownership choices are API decisions.
- Severity / certainty: Medium. Ordering confirmed in source. No proof of extraction.
- Decision: Approve the cleanup-scope reorder and disposed-setter rejection as one change?
- Options:
  - A. Reorder only: validate lengths and enter try/finally immediately after each secret allocation. Removes stranded copies on rejection; no API change. Internal precedent: `src/Oath/src/OathSession.cs:374–393`.
  - B. A, plus `ThrowIfDisposed()` in `SetExtFlag`, `SetTktFlag` and `SetCfgFlag`, matching `SetFixed`, `SetUid` and `SetKey`. Use-after-dispose becomes an exception instead of silent. **Sia's lean: B**, for consistency with the existing setters.
  - C. B, plus a zero-on-failure output contract for one-shot calls. Not needed: the root CLAUDE.md passthrough rule makes the caller own the output.
- Evidence: .NET `src/YubiOtp/src/SlotConfiguration.cs:150–184` (copies at 156–158, length throw at 160–167, no clear); `src/YubiOtp/src/YubiOtpSession.cs:536–549` (configBytes built before the try at 539, cleared only in finally at 547); `src/YubiOtp/src/SlotConfiguration.cs:209–265` (existing setters check disposal) vs `:267–301` (flag setters do not); `:350–371` (Dispose clears fixed, uid and key only). Python `yubikit/yubiotp.py:250–259` @4ca60f70 (immutable bytes). Internal precedent: `src/Oath/src/OathSession.cs:374–393`.

### CORE-OTP-X004: Retain one-shot output through teardown
- Gate(s): G5. Option B names a result-plus-diagnostic contract that the current one-shot API cannot express. No sibling has a hidden one-shot producer (Python N/A).
- Severity / certainty: Medium. Source confirmed. No native teardown failure was induced.
- Decision: If hidden-session teardown throws after a valid HMAC, should the one-shot call (A) fault and clear the result, or (B) return the result with a teardown diagnostic through a new public contract?
- Options:
  - A. Fault the outer task and clear the retained result before rethrowing. No caller sees a secret it cannot clear; no API change. **Sia's lean: A**, since the library is alpha with no consumers.
  - B. New result-plus-diagnostic type. Adds public surface for a rare fault.
  - C. Keep and document. A successful secret can be stranded on teardown failure; the XML remark already admits this.
- Evidence: .NET `src/YubiOtp/src/IYubiKeyExtensions.cs:88–99` (await using, then return), remarks at `:73–82`. Python `yubikit/yubiotp.py:901–928` @4ca60f70 (no hidden session; N/A).

### OTP-IMP-005: Malformed status and sequence policy
- Gate(s): G3. Rust (`crates/yubikit/src/yubiotp.rs:1407–1409, 1414–1419`) and .NET (`SmartCardBackend.cs:112–119`, `210–238`) both accept short or missing status bytes. Python (`yubikit/yubiotp.py:690–699`) and Rust publish the sequence before validating. Siblings therefore cannot settle these points.
- Severity / certainty: Medium. Source confirmed. Firmware boundaries 5.0.0, 5.4.3 and 5.5.0 are unverified on hardware.
- Decision: Keep the broad 5.0.0–<5.5.0 bypass as a documented limitation, or narrow it to the rule Python and Rust use?
- Options (every option rejects a short status; the choice is the sequence rule):
  - A. Narrow: accept an increment, or zero after a positive predecessor; bypass only for 5.0.0–<5.4.3. Rejects writes on 5.4.3–5.5.0 if that range needs the bypass; no device evidence here (E1 risk).
  - B. Keep the .NET bypass and document it; reject short status; publish state only after validation. **Sia's lean: B now**, then A after boundary qualification on hardware.
  - C. Keep current behavior. Silent success on short status remains. Not acceptable.
- Evidence: .NET `src/YubiOtp/src/Backend/SmartCardBackend.cs:112–119, 210–238`; `HidBackend.cs:67–71` (HID write does check length). Python `yubikit/yubiotp.py:685–705` @4ca60f70. Rust `crates/yubikit/src/yubiotp.rs:1395–1424, 1193–1197` @90940e9b (missing byte 3 becomes 0; touch level 0 for short status). Android `yubiotp/.../YubiOtpSession.java:228–231, 475–478` @f4626856 (no sequence check; ByteBuffer read throws on short status).

### OTP-IMP-006: Supported configuration matrix and firmware provenance
- Gate(s): none. No rule fits. A4 (split siblings, stricter for Medium) would pick the stricter floors. Those reject behavior the legacy reference documents: static passwords on 2.1 with up to 16 characters (`StaticPasswordSlotConfiguration.cs:33–34`) and zero-IMF HOTP on 2.1. E1 applies.
- Severity / certainty: Medium. Floors and probes confirmed in source. Exact edition support unverified.
- Sibling floors (2–2 splits):
  - HOTP: 2.1 (.NET, Android) vs 2.2 (Python, Rust).
  - Static password: 2.0 (.NET, Android via `SHORT_TICKET`) vs 2.2 (Python, Rust).
  - Update: 2.3 (.NET, Android) vs 2.2 (Python, Rust).
  - HMAC: 2.2 in all four (agreed).
- Other splits:
  - Alpha/beta sentinel (major 0): admitted by .NET and Android; Python and Rust compare it below their 2.2 floors and reject it.
  - Management probe: .NET on both transports; Python and Android on NFC only (Python then uses `mgmt_version[0] == 3`, .NET uses the OTP major); Rust none.
  - ConfigState provenance: .NET uses the raw status version; Python, Android and Rust use the effective version.
  - NFC 5.0.0–5.2.5 dummy status: Android only.
- Decision: Which admission model: (A) keep type floors and alpha admission, (B) align to the Python and Rust floors and reject alpha unless overridden, or (C) flag-level gating like Android's table, keeping the legacy 2.1 HOTP and 2.0 static floors?
- Options:
  - A. Unchanged. Cheapest; late device rejections remain.
  - B. Python and Rust floors. Rejects documented legacy 2.1 use and alpha devices.
  - C. Flag-level table. Most code, most accurate. **Sia's lean: C**, because flag gating rejects unsupported flags before I/O. Keep legacy floors where the legacy reference documents them, require 2.2 for nonzero IMF (IMP-001 E1), keep alpha admission and document it as optimistic.
- Evidence: .NET `src/YubiOtp/src/SlotConfiguration.cs:60–67`, `HotpSlotConfiguration.cs:67`, `HmacSha1SlotConfiguration.cs:51`, `StaticPasswordSlotConfiguration.cs:33–34`, `UpdateConfiguration.cs:36`, `ConfigState.cs:44–47`, `Backend/SmartCardBackend.cs:57–70`. Python `yubikit/yubiotp.py:384–385, 433–434, 478–479, 564–565, 723–741` @4ca60f70. Android `yubiotp/.../SlotConfiguration.java:69–70, 85–86, 106–108, 144–161`, `BaseSlotConfiguration.java:77–89`, `YubiOtpSession.java:179–189, 212–223`, `UpdateConfiguration.java:53–55` @f4626856. Rust `crates/yubikit/src/yubiotp.rs:867–875, 1354–1368` @90940e9b.

### OTP-IMP-007: Update and access-code intent
- Gate(s): G5. Named better option: an intent-bearing update (retain, replace, remove) instead of full-mask replacement with caller-supplied codes. Sibling CLIs disagree, so no clean A3 applies.
- Severity / certainty: Medium. Omission behavior confirmed in source. The 4.3.2–4.3.5 restriction is in Python, Android and Rust (A2-shaped) and absent from .NET. Device transition behavior is unverified.
- Decision: Keep the library's two-code shape with full-mask replacement, adding the 4.3.2 check, or add an intent-bearing update before alpha ends?
- Options:
  - A. Keep the shape. Add the 4.3.2 ≤ version < 4.3.6 check to `UpdateConfigurationAsync`. Sample CLIs keep the current code by default with an explicit delete flag (Python does this, Rust `settings` does too). **Sia's lean: A**, low risk, same library contract as all three siblings.
  - B. Add retain, replace and remove update operations. New public API; the omission trap disappears.
  - C. Keep and document. Omission still removes protection; the sample tools remain the trap.
- Evidence: .NET `src/YubiOtp/src/YubiOtpSession.cs:225–245` (no transition check), `UpdateConfiguration.cs:29–32, 145–152`, `examples/OtpTool/Commands/SettingsCommand.cs:69`, `ChalRespCommand.cs:79–81`, `StaticCommand.cs:83–84`. Python `yubikit/yubiotp.py:834–848`, `ykman/_cli/otp.py:586–587, 673–674, 915–917` @4ca60f70. Android `yubiotp/.../YubiOtpSession.java:400–406` @f4626856. Rust `crates/yubikit/src/yubiotp.rs:1235–1244`; `crates/ykman-cli/src/otp.rs:272–273, 1420–1426` (settings retains by default) and `:914, 1020, 1118, 1358` (programming commands send `--access-code` as the new code with no current code) @90940e9b. The Rust programming commands contradict the Rust settings default, so the CLI precedent is not clean.

### OTP-IMP-011: Test oracles and independent cleanup
- Gate(s): none. No rule fits: test work with destructive hardware effects, and restoration needs Dennis's authorization.
- Severity / certainty: Medium (state loss). Sequential cleanup confirmed in source. No test was run.
- Decision: Approve a destructive-fixture policy: (A) keep both-slot tests with independent cleanup per slot and recorded outcomes, (B) gate destructive physical cases behind an explicit dedicated-key switch, or (C) drop the both-slot protection test until a dedicated key exists?
- Options: A, B and C as above. **Sia's lean: A for the non-hardware oracles** (independent CRC, HOTP and HMAC vectors need no device), **and B for hardware cases**.
- Evidence: .NET `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/AuditV2/YubiOtpProtectSlot2Tests.cs:116–120` (a slot-two cleanup failure prevents slot-one cleanup), `:26` (original secrets not restorable), `:125–129`. Python: no audited test (N/A).

### OTP-IMP-004: Touch notification certainty
- Gate(s): G5. Option B names a weaker `PolicyMayRequire` notification that no sibling provides. Siblings never prompt for SmartCard challenge-response.
- Severity / certainty: Low. Heuristic confirmed. No bypass shown.
- Decision: For SmartCard challenges on a keyboard-triggered slot, stay silent and drop the PolicyRequires basis (A), emit a weaker PolicyMayRequire prompt (B), or use a supplied provisioning policy (C)?
- Options:
  - A. Stay silent. Matches all three siblings and removes a wrong basis. **Sia's lean: A** for alpha. Consequence: today's prompt disappears for keyboard-triggered slots on SmartCard, a user-visible change for Dennis to accept.
  - B. Weaker prompt wording. Still a heuristic.
  - C. New provisioning-policy input with stale-state invalidation rules. Larger API change.
- Evidence: .NET `src/YubiOtp/src/YubiOtpSession.cs:586–622` (heuristic at 608–621), `:624–633`; `src/YubiOtp/src/ConfigState.cs:104–110` (indicator is not RequireTouch). Python `yubikit/yubiotp.py:609–614` (docstring excludes challenge-response), `701–705` @4ca60f70. Rust `crates/yubikit/src/yubiotp.rs:1477` @90940e9b (no SmartCard cancel or keepalive). Android `yubiotp/.../ConfigurationState.java:61–78` @f4626856.

### OTP-IMP-008: NDEF input and disable contract
- Gate(s): G5. Null-disable and longest-prefix encoding are capabilities that Python, Android and Rust do not have. They map null to the default URI and take the first prefix match.
- Severity / certainty: Low. Source confirmed. On-tap effects unverified.
- Decision: Keep null as an all-zero disable with longest-prefix encoding, or align null with the siblings' default-URI behavior and add a separate disable call?
- Options:
  - A. Keep. **Sia's lean: A**, since these give capabilities the siblings lack. Also reject undefined `NdefType` values before building the payload (mechanical; stays with this card).
  - B. Align null to the default URI and add a separate disable operation. API change; no capability loss, but disable needs a new call.
  - C. Keep null and accept undefined types. Not acceptable.
- Evidence: .NET `src/YubiOtp/src/YubiOtpSession.cs:436–459` (null returns zeros at 441–445; type byte at 447; text branch for other values), `:461–475` (longest prefix), `:500–527` (text budget). Python `yubikit/yubiotp.py:274–292` @4ca60f70 (default URI at 276–277; first match at 278–284). Android `yubiotp/.../YubiOtpSession.java:424–432, 519–528` @f4626856. Rust `crates/yubikit/src/yubiotp.rs:643–672` @90940e9b.

### OTP-IMP-010: OtpTool command contract
- Gate(s): G5. The scope choice trades a small demonstrator against ykman-like behavior, and each option carries a different support cost.
- Severity / certainty: Low overall. Source confirmed. The silent code-omission items are handled under OTP-IMP-007 and T2.
- Decision: Scope the OtpTool as (A) a small documented demonstrator with mismatched options removed or labelled, (B) a ykman-compatible surface (base32 TOTP enrollment, stdin, OTP HID for USB calculation, retain-by-default codes), or (C) the current mixed state documented as a limitation?
- Options: A, B and C as above. **Sia's lean: A.** Fix the help text now: drop the stdin claim, change the layout default from `US` (no such enum member) to `ModHex`, and stop advertising `--access-code` for swap. Use OTP HID for USB calculate. Rename or drop `chalresp --totp`.
- Evidence: .NET `src/YubiOtp/examples/OtpTool/CliOptions.cs:99–102, 134, 302–304, 388–390, 446–450, 493, 512–518`; `Commands/CalculateCommand.cs:31–38`; `Commands/SwapCommand.cs:24, 30–32, 41`; `Commands/ChalRespCommand.cs:69–77`; `Program.cs:39–42` (cancel returns 0); `CliOptions.cs:171–181, 349–351` (Environment.Exit); `DeviceHelper.cs:11–26`; `src/Core/src/Transports/Hid/Keyboard/KeyboardLayout.cs:19–29`. Python `ykman/_cli/otp.py:619–675` (base32), `706–711` (USB uses OTP HID), `265–278` (swap) @4ca60f70. Rust `crates/ykman-cli/src/otp.rs:179–181` (modhex default) @90940e9b.

## Auto-approved

### OTP-IMP-001: Encode the requested HOTP counter
- Rule: A1 (master marks divisor and range as no-brainer). A2 also holds: Python, Android and Rust all divide by 16.
- Evidence pin (E2): Python `yubikit/yubiotp.py:454–460` @4ca60f70; Android `yubiotp/src/main/java/com/yubico/yubikit/yubiotp/HotpSlotConfiguration.java:87–92` @f4626856; Rust `crates/yubikit/src/yubiotp.rs:1003–1010` @90940e9b. .NET window: `src/YubiOtp/src/HotpSlotConfiguration.cs:55–62` @52d34ca65.
- Pseudocode for the fix:
```text
HotpSlotConfiguration(hmacKey, imf):
  if imf < 0 or imf > 0xFFFF0 or imf % 16 != 0:
      throw ArgumentException("Initial moving factor must be between 0 and 0xFFFF0, in steps of 16.", imf)
  SetHmacKey(hmacKey, (ushort)(imf / 16))   // SetHmacKey writes big-endian uid[4..6]
  _tktFlags |= OathHotp                      // unchanged
```
- Test to add: literal vectors through the public `GetConfig()` (no I/O), replacing the pins at `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/SlotConfigurationTests.cs:424–452`. Config bytes 20–21 must equal: imf 0 → `00 00`; 16 → `00 01`; 0x30000 → `30 00`; 0xFFFF0 → `FF FF`. Keep the existing invalid cases (`imf: 100`, `imf: -1`) and add 8 and 0x100000.
- Docs: reopen the HOTP XML (`HotpSlotConfiguration.cs:32–35, 45–48`), README and IMF help in the same commit, as the card requires.
- E1 escalation risk: stop if nonzero IMF needs a firmware gate (the IMP-006 decision; .NET floors HOTP at 2.1, Python and Rust at 2.2), or if device counter behavior must be confirmed before the test can land.
- Device qualification (keyboard output, counter sync) is T1 and not part of this approval.

### OTP-IMP-009: Translate standalone static-password text
- Rule: A1 (master marks translation as no-brainer). A3 also holds: Python (`ykman/_cli/otp.py:578, 585`) and Rust (`crates/ykman-cli/src/otp.rs:994–1002`) translate before the raw constructor. Android offers only the raw constructor and contradicts nothing.
- Scope: the input/output policy is not changed (immutable password text, printed generated secret). G5 is not hit: the argv string is immutable regardless, and `StaticPasswordSlotConfiguration.cs:71–79` already zeroes the translated intermediate.
- Evidence pin (E2): .NET BEFORE `src/YubiOtp/examples/OtpTool/Commands/StaticCommand.cs:56–59, 79–81` @52d34ca65; fix primitive `src/YubiOtp/src/StaticPasswordSlotConfiguration.cs:59–62, 149–179` @52d34ca65. Python `ykman/_cli/otp.py:548–549, 568–569, 578, 585` @4ca60f70. Rust `crates/ykman-cli/src/otp.rs:179–181, 538–548, 994–1002` @90940e9b. Android `yubiotp/.../StaticPasswordSlotConfiguration.java:30–44` @f4626856 (raw constructor).
- Pseudocode for the fix:
```text
layoutName = options.KeyboardLayout ?? "ModHex"
layout = Enum.TryParse<KeyboardLayout>(layoutName, ignoreCase) and IsDefined, else error   // before any secret is read
password = options.Password, or generated from the 16 ModHex letters when --generate
using var config = new StaticPasswordSlotConfiguration(password, layout)
  // translates, zeroes its intermediate, throws ArgumentOutOfRangeException on an untypeable character
// session and PutConfigurationAsync run only after the constructor succeeds
```
- Test to add: a public-constructor test, not a CLI test (examples have no test project; do not add a placeholder). `new StaticPasswordSlotConfiguration("a", KeyboardLayout.en_US).GetConfig()[0]` must equal `0x04` (USB HID usage for `a`; confirm against `HidCodeTranslator` for en_US). Add one uppercase vector taken from the translator table, and one layout-mismatch case, which is part of the translation contract.
- Help text: change the static `--keyboard-layout` default in `CliOptions.cs:493` to `ModHex`.
- E1 escalation risk: stop if a supported layout cannot type a ModHex letter (translator throws), since the generator would then need a per-layout alphabet and a public helper decision; stop if the change needs a public constructor signature change.
- Per-layout output on a host is T1 and not part of this approval.

### OTP-DEP-005: Legacy FIPS and mode surface inventory
- Rule: A6 (keep and document). Current behavior is acceptable: no typed OTP FIPS-mode query and no legacy mutator exists in the public surface. The card needs wording only.
- Redirect: the BEFORE edit targets `docs/migration/v1-to-v2-gaps.md:419`. `docs/migration/v1-to-v2.md:399–401` calls that file "a historical baseline, not a current API reference", and the master says historical surveys stay unchanged. The wording goes into the active migration guide instead. The active docs have no YubiOTP FIPS line; grep finds it only in the historical file.
- Evidence pin (E2): .NET absence: `src/YubiOtp/src` has no `QueryFipsMode`, `SetLegacyDeviceConfig` or public `SetMode` @52d34ca65; `src/Management/CLAUDE.md:835` (no current public legacy SetMode surface). Python `yubikit/management.py:695–749` @4ca60f70 (legacy mode precedent, not an OTP FIPS query).
- Pseudocode for the fix (documentation only):
```text
Replace the "unverified relocation" wording in the active migration guide with:
  "YubiOTP legacy/FIPS surfaces: device info is available through Management/Core.
   No typed OTP FIPS-mode query or legacy mode/config mutator exists at the inspected v2 baseline."
Do not edit docs/migration/v1-to-v2-gaps.md (historical).
```
- Test to add: none (documentation only). Verify with `git diff --stat` that the historical file is untouched.
- E1 escalation risk: stop if no active section fits (do not create a new migration document); stop if a typed legacy surface is requested (product decision, out of scope).

## T1 hardware qualification
| Card | What needs a device | Device/firmware/transport/OS |
| --- | --- | --- |
| OTP-IMP-012 | HMAC host vectors: 1–63 bytes with both pad choices and repeated bytes; empty, 63 and 64 boundaries; fixed 64-byte mode. AES ticket: independent decrypt, UID, CRC and counter checks. NFC read-back. HID and CCID separately. | Dedicated key with explicit authorization (slot 2 and protection effects). Firmware 2.2+ for HMAC; 3.0+ for touch-trigger and NDEF. USB OTP HID, USB CCID and NFC each separately. OS: Linux is the only recorded run (serial 103, metadata 5.8.0.beta.0, OTP 0.0.1; master evidence, not re-verified). Serial 9681620's two-key failure stays open. |
| OTP-IMP-001 (A1 follow-up) | Counter synchronization against a host verifier; keyboard output with nonzero IMF | Firmware 2.2+ for nonzero IMF; OTP HID; dedicated key |
| OTP-IMP-009 (A1 follow-up) | Keyboard output per layout (at least en_US, de_DE, sv_SE) via an authorized input capture | OTP HID keyboard output; layout named explicitly |
| OTP-IMP-002 | Full-map write and reset read-back | Firmware 3+ per vendor evidence (unverified here); dedicated key; the map changes both slots' keyboard output |
| OTP-IMP-005 | Behavior at 5.0.0, 5.4.3 and 5.5.0 boundaries | Keys at those firmware versions (none in our evidence) |
| OTP-IMP-004 | HID touch-wait versus SmartCard keyboard-trigger matrix | USB HID, USB CCID, NFC; touch-triggered slots |
| OTP-IMP-007 | 4.3.2–4.3.5 access-code transition (authorized probe only; never delete an unknown protected slot) | Firmware 4.3.2–4.3.5 key (none available) |
| OTP-IMP-008 | NFC on-tap output for URI, text and disable; read-back | NFC-capable key; dedicated slot |
| OTP-IMP-011 | Destructive both-slot fixture | Dedicated key; authorization |

## T2 cross-cutting candidates
| Theme | Card(s) here | Why it is cross-cutting |
| --- | --- | --- |
| Cached state published before validation or after failure | OTP-IMP-005 | Session-state policy after failed operations is shared with OATH, PIV and YubiHSM Auth (policy example). Python and Rust also publish early. |
| Response-length strictness versus forward compatibility | OTP-IMP-005 (SmartCard data: .NET prefix; Python and Android exact) | Root CLAUDE.md "Forward compatibility doctrine" governs every response parser. |
| Access-code omission and retention | OTP-IMP-007, OTP-IMP-010 | Any applet with protected writes. Sibling defaults differ by CLI. |
| Secret acquisition, cleanup scope and disposal | OTP-IMP-003, OTP-IMP-011 | Oath already uses try/finally zeroing; disposal ownership is shared with Core owners (B1/B5). |
| One-shot extension result ownership through teardown | CORE-OTP-X004 | Same `await using` helper shape as other applets' one-shot extensions (to confirm per applet). |
| User-presence certainty and cancellation | OTP-IMP-004 | `IUserPresencePrompt` is the cross-applet touch contract (root CLAUDE.md). |
| Firmware and feature admission, alpha/beta sentinel, override provenance | OTP-IMP-006 (and the OTP-IMP-001 E1 gate) | Core `FirmwareVersion`/`Feature` policy; master's Core B5 C007 says major-zero admission is host policy. |
| CLI sample contract: exit codes, parser exits, confirmation bypass | OTP-IMP-010 | Other example tools use the same `Environment.Exit` and confirmation patterns (to confirm). |
| Destructive hardware fixtures and restoration authorization | OTP-IMP-011 (OTP-IMP-012 physical lane is T1, not tagged) | Shared integration-harness policy. |

## Coverage check
- Review map IDs (14): OTP-IMP-001, -002, -003, -004, -005, -006, -007, -008, -009, -010, -011, -012, OTP-DEP-005, CORE-OTP-X004. Each appears exactly once in the Index.
- Not treated as cards:
  - OTP-DEP-001 to -004: coverage aliases of shared Core rows (X001, X002, X003, X009) and of cards already indexed.
  - CORE-OTP-X001, X002, X003, X005 to X009: shared-owner rows or aliases. X005 maps to OTP-IMP-003/010, X006 to IMP-004/005/006/012, X007 to IMP-009/010/012, X008 to DEP-005, X009 to IMP-011/012.
  - "YubiOTP shared dependencies" table (CORE-B1-C005/C008, CORE-B5-C004/C006, CORE-B5-C001/C007/C008, CORE-B5-C005, CORE-B6-C002, CORE-C-C008, CORE-B4-C001/C007/C008/C010, CORE-C-C001 to C004, CORE-A-C004): canonical Core owners, not YubiOTP cards.
  - Pass-1 OTP-C001 to C018 documentation acceptance, the withdrawn merged-flag wording request, and the excluded OATH, PIV, OpenPGP, HSM and Rust items.
- Observations outside the cards (not classified here):
  1. `src/YubiOtp/src/PublicAPI.Unshipped.txt:177` @52d34ca65 lists `ConfigFlagMasks.UpdateMask` as `HmacLt64 | ChalBtnTrig`. Source `src/YubiOtp/src/ConfigFlag.cs:57–59` is `PacingChar10 | PacingChar20`, which matches Python (`yubikit/yubiotp.py:176`) and Android. The API file is stale.
  2. Rust `crates/ykman-cli/src/otp.rs:914, 1020, 1118, 1358` pass `--access-code` as the new code with no current code, against the option's "Current access code" help. Report to the Rust owner (see OTP-IMP-007).
  3. Rust `crates/yubikit/src/yubiotp.rs:1414–1417` panics on a status shorter than 3 bytes when the predecessor is positive (`&status[..3]`). Recorded as evidence for OTP-IMP-005; not a .NET card.
  4. Android `calculateHmacSha1` throws on an empty challenge (`yubiotp/.../YubiOtpSession.java:454`), while Python, Rust and .NET pad with 0x00. Qualification note for OTP-IMP-012.
  5. Android's NFC 5.0.0–5.2.5 dummy status (`YubiOtpSession.java:212–223`) has no .NET or Rust counterpart. Recorded under OTP-IMP-006.
