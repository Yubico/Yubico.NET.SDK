# OATH: Pass 2 classification
Classifier: one dedicated subagent, 2026-11-14. Source: master lines 93–483 at 52d34ca65. Policy: pass-2-review-policy.md (approved). Final per-card calls: `../pass-2-review-ledger.md`.
Sibling pins: yubikey-manager @ 4ca60f70 (`yubikit/oath.py` unmodified in the working tree); yubikit-android @ f4626856; yubikey-manager-rust-auto @ 90940e9b. python-fido2 not used (no FIDO path in OATH).

## Summary
| Disposition | Count | Cards |
| --- | --- | --- |
| A1 no-brainer | 0 | none; the master marks all eight "No-brainer: no" (CORE-OATH-X005 "No for the group") |
| A2 sibling consensus | 1 | OATH-DEC-001 |
| A3 Python precedent, partial confluence | 0 | none |
| A4 split siblings, stricter | 1 | OATH-DEC-002 |
| A5 internal precedent | 1 | CORE-OATH-X005 (three re-verified sites only) |
| A6 keep and document | 0 | none; the batch-1 A6 call for OATH-DEC-003 is withdrawn |
| Interview (gated) | 1 | OATH-DEC-003 (G2; no auto rule) |
| T1 qualification | 4 | OATH-DEC-004, OATH-DEC-006, OATH-DEC-007, secure-reset qualification |
| T2 candidate (also counted above) | 3 | OATH-DEC-001, OATH-DEC-007, CORE-OATH-X005 |

Total: eight cards. Three auto, one gated, four T1.

## Index
| Card | Severity | Disposition | One-line rationale |
| --- | --- | --- | --- |
| OATH-DEC-001 | Medium (conditional) | A2, T2 candidate | Python, Android and Rust all keep the session locked and the challenge on device-HMAC mismatch; .NET matches. Docs and a retry test. |
| OATH-DEC-002 | Low | A4 | Android's `setAccessKey` and Rust's `OathAccessKey` admit only 16 bytes; Python forwards any length. Adopt the strict 16-byte check. |
| OATH-DEC-003 | Low | Interview (G2; no auto rule) | Key URI text makes the HOTP counter REQUIRED; all four siblings default it to 0. Issuer precedence splits the siblings with no auto answer. |
| OATH-DEC-004 | No assigned severity | T1 | Padding threshold needs vendor, history or device provenance. No runtime change selected. |
| OATH-DEC-006 | No assigned severity | T1 | HOTP successive values, failure-time counter use and marker bytes need a device oracle. |
| OATH-DEC-007 | No assigned severity | T1, T2 candidate | The 5.6.3 OATH SCP gate needs firmware provenance. Gate kept as selected. |
| CORE-OATH-X005 | Medium (masking clause met) | A5, T2 candidate | Test CTS disposal, password-byte clearing, and cleanup that keeps the primary failure. Module precedents exist. Unverified sites excluded under E1. |
| Secure-reset qualification (no ID) | No assigned severity | T1 | Live SCP reset never reached; serial 125 returned 6A88 at INITIALIZE UPDATE. |

## Interview queue (sorted High, then Medium, then Low)

No High or Medium card is gated. One Low card is gated.

### OATH-DEC-003: URI extensions and wider HOTP counters
- **Gate(s) and why:** G2. The Google Key URI format the SDK cites (`src/Oath/src/CredentialData.cs:98–99`) makes the HOTP counter REQUIRED. All four siblings default a missing counter to 0, so precedent contradicts that text. If Dennis rules the Key URI format not governing, G2 drops, but the issuer case still gates. Siblings split on issuer precedence: query wins in .NET, Python and the Rust CLI; label wins in Android. A4 needs a stricter option among sibling behaviours, and neither precedence is stricter. A reject-on-conflict rule is no sibling's behaviour, so it is a G5 divergence.
- **Severity / certainty:** Low. Parser behaviour is source-confirmed. Intended extensions are unresolved.
- **Decision:** Keep the documented URI extensions (missing HOTP counter means 0; query issuer beats label issuer) as SDK policy, or follow the Key URI text strictly?
- **Options:**
  1. **Keep and document (Sia's lean).** Matches Python, the Rust CLI and the existing .NET pins (`src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/CredentialDataTests.cs:234`, `:270`). Android stays a documented divergence. `src/Oath/CLAUDE.md` already says to preserve these policies. Lean because every sibling already defaults the counter, and the issuer conflict is only a SHOULD in the Key URI text. Root CLAUDE.md keeps missing required fields strict, but that doctrine is written for device responses, so it does not settle URI input.
  2. **Strict Key URI.** Reject HOTP URIs without a counter and reject conflicting issuers. Breaks URIs all four siblings accept, adds a new failure mode, and has no sibling precedent.
  3. **Mixed.** Keep the default-zero counter and reject conflicting issuers. Gives one credential ID across SDKs, which is a concrete benefit. Diverges from all four siblings on the issuer rule.
- **Evidence:** .NET `src/Oath/src/CredentialData.cs:179–184` (query issuer), `:201–204` (digits), `:217–222` (counter), `:272–288` (range checks). Python `yubikit/oath.py:118–146` @4ca60f70. Rust CLI `crates/ykman-cli/src/oath.rs:956–959`, `:978–981` (digits, no range check at parse), `:993–994` @90940e9b. Android `oath/src/main/java/com/yubico/yubikit/oath/CredentialData.java:39`, `:124–130`, `:306–312` @f4626856. Spec: Key URI format at github.com/google/google-authenticator/wiki/Key-Uri-Format (issuer "should be equal"; digits "6 or 8"; counter "REQUIRED if type is hotp").
- Once Dennis picks option 1, 2 or 3, the Int32 counter and 6–8 digit sub-decisions follow A4 and need no separate question. Digits: .NET and Android accept 6–8; Python and the Rust URI parser accept any value. Counter: .NET and Android use signed 32-bit; Python and Rust use unsigned 32-bit. The stricter choice in each case is what the code already does.
- Batch-1 A6 call for this card is withdrawn on the evidence above.

## Auto-approved

### OATH-DEC-001: state after failed device authentication
- **Rule:** A2, T2 candidate. Python, Android and Rust all keep the session locked and the challenge when the device answer does not match. None marks the session terminal. .NET already behaves this way, so the change is docs and a test, not behaviour.
  - G2 not hit: the YKOATH VALIDATE text defines the exchange, not host state after a mismatch (developers.yubico.com/OATH/YKOATH_Protocol.html).
  - G3 not hit: no shared unsafe unlock. Terminality is a policy choice.
  - G5 not hit: no concrete user benefit over the siblings. A retry reuses the same device challenge, so it sends the same host response (`OathSession.cs:714`) and adds nothing for an observer. The master records no bypass.
  - T2: the cross-applet question (OATH, PIV, YubiHSM Auth) is decided first. If T2 picks another policy, re-route this card before the engineer starts.
  - Correction to batch-1: "a retry can't succeed" overstates it. `ValidateAsync` generates a fresh host challenge on every call (`OathSession.cs:715`), and the YKOATH VALIDATE text says the same. A retry is a new exchange. Only the device's earlier answer is suspect.
- **Evidence (E2):** .NET `src/Oath/src/OathSession.cs:714–715`, `:745–750`. Python `yubikit/oath.py:333–351` @4ca60f70 (raise at 346–349, challenge cleared at 350). Android `oath/src/main/java/com/yubico/yubikit/oath/OathSession.java:270–298` @f4626856. Rust `crates/yubikit/src/oath.rs:699–719` @90940e9b.
- **Pseudocode:**
```text
ValidateAsync(key)                          // behaviour unchanged
  send VALIDATE(HMAC(key, _challenge), fresh host challenge)
  if device answer != HMAC(key, host challenge):
      throw BadResponseException            // IsLocked stays true; _challenge kept
  IsLocked = false; clear _challenge

XML on ValidateAsync (src/Oath/src/IOathSession.cs:290) and the unlock guide:
  "A device-authentication mismatch leaves the session locked. Retry with the same key
   (a new host challenge is sent), or create a fresh session."
```
- **Test to add:** extend the mismatch leg of `src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/OathResponseTests.cs:110–152`. Today it asserts one mismatch and `IsLocked`. Add: a protected call (LIST) still reports Locked; a second `ValidateAsync` with a correct device answer succeeds and clears `IsLocked`; the second host challenge differs from the first. Fake pattern: `OathResponseTests.cs:114–126`.
- **E1:** Stop if T2 selects a terminal or recovery policy, because the change then becomes a behaviour change. Stop if the retry needs a new SELECT.

### OATH-DEC-002: raw access-key contract
- **Rule:** A4. Siblings split: Android's `setAccessKey` and Rust's `OathAccessKey` admit only 16 bytes; Python forwards any length. Severity is Low, so the stricter, fail-closed choice applies. Root CLAUDE.md's forward-compatibility doctrine also keeps security-critical protocol data strict. G2 checked: the YKOATH SET CODE text defines the key as "16 bytes of that" PBKDF2 output, and the adopted behaviour matches it. The lenient Python behaviour is not adopted. G3 and G5 not hit.
- **Evidence (E2):** Android `oath/src/main/java/com/yubico/yubikit/oath/OathSession.java:109`, `:328–331` @f4626856. Rust `crates/yubikit/src/oath.rs:122–133`, `:699`, `:722` @90940e9b (`validate` and `set_key` take `&OathAccessKey`). Python (contrast) `yubikit/oath.py:333–351`, `:353–376` @4ca60f70. .NET `src/Oath/src/OathSession.cs:703–704`, `:786–787`, `:798–801`.
- **Pseudocode:**
```text
const int AccessKeyLength = 16              // YKOATH SET CODE: 16 bytes of PBKDF2 output
ValidateAsync(key):
  if key.Length != AccessKeyLength: throw ArgumentException("Access key must be 16 bytes.", nameof(key))
  ...                                        // unchanged; no APDU before the check
SetKeyAsync(key):
  if key.Length != AccessKeyLength: throw ArgumentException(same)
  ...                                        // TAG_KEY stays [0x21][key]
DeriveKey: unchanged (already 16 bytes)
XML on IOathSession.cs:290 and :310: "key is the 16-byte access key from DeriveKey"
```
- **Test to add:** wire-level, not validation-only. For lengths 0, 15, 17 and 32, assert that the fake connection records no command. For a 16-byte key, assert the exact SET CODE TAG_KEY bytes `73 11 21 <16 bytes>`. Assert the caller's buffer is unchanged.
- **E1:** Stop if a caller, example or test sends a non-16-byte raw key that must keep working. None found: `src/Oath/examples/OathTool/Commands/AccessCommand.cs:97–99` and `src/Oath/examples/OathTool/Cli/SessionHelper.cs:109–111` use DeriveKey; `src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/OathProtocolResponseTests.cs:132–133` and `:149–151` use 16 bytes; `src/Oath/README.md:181–197` uses DeriveKey. Stop if a documented device profile accepts another length. Public signatures do not change (`src/Oath/src/PublicAPI.Unshipped.txt:63`, `:65`, `:100`, `:102`).

### CORE-OATH-X005: inherited test resource and restoration work (verified sites only)
- **Rule:** A5. Zeroing, disposal and cancellation in .NET-only test code, with no sibling equivalent. The module and Core already have the pattern. Auto covers the three sites re-verified in this pass: (1) per-test CTS disposal, (2) UTF-8 password bytes cleared, (3) cleanup that keeps the primary failure and uses the password actually active. Every other site is E1.
- **Severity:** Medium. The masking clause is met in source: the PasswordChange `finally` replaces the first error (`src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/OathPasswordChangeTests.cs:122–136`). Public test passwords keep it from High.
- **Internal precedent (A5):**
  - CTS: `src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/OathLifecycleTimeTests.cs:38` (`using var timeout = new CancellationTokenSource(...)`).
  - Password bytes: `src/Oath/README.md:181–197` and `src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/OathAuthenticationTests.cs:277–278` (cleared in `finally`).
  - Primary failure: `src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/TestExtensions/OathTestStateExtensions.cs:72–88` (`primaryFailure`; `catch when (primaryFailure is not null)`) and `src/Core/src/Sessions/ApplicationSession.cs:405–425` (`DisposeAfterInitializationFailure`).
- **Evidence (worktree, 52d34ca65):** `OathPasswordChangeTests.cs:37–38` (`NewToken` returns `.Token` of an undisposed CTS), `:54–65` (`Encoding.UTF8.GetBytes(password)` at `:56` is never cleared), `:122–136`.
- **Pseudocode:**
```text
// (1) the test owns the CTS
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

// (2) the test owns and clears the password bytes
byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
byte[]? key = null;
try { key = session.DeriveKey(passwordBytes); await action(key); }
finally { if (key is not null) ZeroMemory(key); ZeroMemory(passwordBytes); }

// (3) cleanup keeps the primary failure and uses the active password
Exception? primary = null; bool rotated = false;
try { ...; SetKey(newPassword); rotated = true; ... }
catch (Exception e) { primary = e; throw; }
finally
{
    try { cleanup using (rotated ? newPassword : originalPassword), then UnsetKey }
    catch when (primary is not null) { }    // primary wins; same as the precedent
}
```
- **Test to add:** no new unit test is practical for the integration helper without moving it. Verification is a clean build of the integration project plus review. Hardware runs of `OathPasswordChangeTests` need an authorized lane and are not part of this classification.
- **E1:** Stop and escalate if (a) a fix needs sites outside the three verified ones: AuditV2 raw PUT copies, DELETE status and readback, fake snapshot and response disposal, drain and independent readback. The master holds those as frozen per-site evidence, so re-verify first. (b) A change needs a new device operation or edits `Tests.Shared`. (c) A test uses a real credential, which raises severity above Medium.

## T1 hardware qualification
| Card | What needs a device | Device/firmware/transport/OS |
| --- | --- | --- |
| OATH-DEC-004 (padding provenance) | Nothing for the provenance step (vendor, history or device statement). A device only if a later change needs wire vectors for short, equal and long secrets. | None recorded. A vector run would be SmartCard; firmware not fixed. |
| OATH-DEC-006 (HOTP operation and markers) | HOTP credential with a known seed and counter; independent counter oracle; full vs truncated CALCULATE; failures before and after admission; counter use on failure; HOTP+touch marker bytes with a person at the key. | SmartCard only (`src/Oath/CLAUDE.md`). Firmware, state and OS not recorded. |
| OATH-DEC-007 (SCP gate provenance) | Compatible SCP-capable devices on both sides of the gate (OATH 5.6.3; Core SCP03 5.3.0; SCP11 5.7.2). Supported matrix and historical rationale (shared history commit `1b87964ba`, `docs/research/documentation-audit/modules/core/pass-1/core-001/boundaries/Oath/analysis.md:80`). | SmartCard; firmware bands below and above 5.6.3 and 5.7.2. Authorization needed, because SCP changes device key state. |
| Secure-reset qualification | Authorized SCP-capable key. Bounded INITIALIZE UPDATE checks first; then EXTERNAL AUTHENTICATE and RESET (erases OATH data, so explicit authorization); then terminal admission, teardown and fresh reopen with readback. Optional separate CORE-001 two-key discovery reproduction. | Serial 125: INITIALIZE UPDATE returned 6A88 twice, before EXTERNAL AUTHENTICATE and RESET. Firmware and transport for that run are not recorded on the OATH card. PIV record `docs/research/documentation-audit/modules/piv/pass-1/piv-001/hardware/final-125.json` (utc 2026-11-05) lists device_version 5.8.0; not confirmed for the OATH run. Serial 9681620: old Linux hidraw two-key run, unexplained (`docs/research/documentation-audit/modules/core/pass-1/core-001/investigation-B2.md:118`). |

OATH-DEC-004 is T1 by the no-severity rule; its provenance step needs no device.

## T2 cross-cutting candidates
| Theme | Card(s) here | Why it is cross-cutting |
| --- | --- | --- |
| Session state after failed device authentication | OATH-DEC-001 (A2, provisional) | Named in the policy's T2 list. PIV and YubiHSM Auth have the same question; not inspected in this pass. Decide once before OATH's docs land. |
| SCP firmware gate provenance and naming | OATH-DEC-007 | The same SCP03/SCP11 concept is gated in Core (`src/Core/src/Protocols/SmartCard/Scp/ScpInitializer.cs:34–35`, `:66`, `:76`) and SecurityDomain (`src/SecurityDomain/src/SecurityDomainSession.cs:41`, `:43`). OATH's 5.6.3 check runs before Core's SCP11 check (`OathSession.cs:120–136`), so SCP11 parameters get a message that names SCP03. |
| Test-owned secret zeroing, CTS disposal, cleanup that keeps the primary failure | CORE-OATH-X005 | The policy names secret zeroing, disposal and cancellation as T2 examples. Module and Core precedents already agree. Other applets' integration tests were not checked in this pass. |
| Core-linked rows (shared; not OATH cards; not decided here) | None of the eight. Master rows: CORE-B5-C001, CORE-B5-C006, CORE-C-C001–C004, CORE-B6-C015, CORE-B6-C002/C007 with CORE-B5-C005, original CORE-001 | Owned by core-001 and used by every applet. Decide in the Core/T2 track, not per applet. |

## Coverage check
Review-map cards (eight): OATH-DEC-001, OATH-DEC-002, OATH-DEC-003, OATH-DEC-004, OATH-DEC-006, OATH-DEC-007, CORE-OATH-X005, and the unnumbered "Secure-reset qualification". Each appears once in the Index. Eight of eight dispositioned.

Deliberate exclusions, not treated as cards:
- IMP-001 to IMP-014: resolved in scope, per the master's coverage ledger.
- DEC-005: resolved (Int64 timestamp and exclusive-end policy).
- DOC-001: resolved (Linux PC/SC reference).
- DOC-002: migration YAML; shared documentation hygiene outside OATH product scope.
- CORE-OATH-X001, X002, X003, X004, X006 and original CORE-001: linked Core rows, listed under T2.
- Windows successor protected OATH dispositions: unchanged; no card.

Pin corrections: the master's DEC-007 BEFORE pin says `OathSession.cs:36`. At 52d34ca65 the `FeatureScp03` declaration is at line 37; line 36 is `FeatureRenameCredential`. The master names 6ce3554 as its baseline. All other .NET citations in this section were re-checked at 52d34ca65 and match within a line.
