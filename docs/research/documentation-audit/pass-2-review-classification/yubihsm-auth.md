# YubiHSM Auth: Pass 2 classification
Classifier: one dedicated subagent, 2026-11-14. Source: master lines 5549–6259 at 52d34ca65. Policy: pass-2-review-policy.md (approved). Final per-card calls: `../pass-2-review-ledger.md`.
Sibling pins: yubikey-manager @4ca60f70 (yubikit/hsmauth.py, yubikit/core/__init__.py, ykman/_cli/hsmauth.py; unmodified for these paths); yubikit-android @f4626856 (AppId and Capability constants only; no HSM Auth session code); yubikey-manager-rust-auto @90940e9b (crates/yubikit/src/hsmauth.rs, crates/ykman-cli/src/hsmauth.rs). python-fido2 not used. Each sibling HEAD equals its pin. src/YubiHsm and src/Core are unchanged between 6ce355468 (master inspection commit) and 52d34ca65.

## Summary

Gates first, then A-rules. A card with a gate hit, or with no single rule covering all its parts, goes to the interview queue. Auto-eligible parts inside those cards are named so they can be cleared in the same sitting. G2 cannot be evaluated: no normative YubiHSM Auth spec was located (GAP-SPEC-1), so sibling precedent here is implementation evidence only. Short .NET paths are relative to `src/YubiHsm/` (`src/` library, `examples/HsmAuthTool/` sample, `tests/` tests) unless a full path is given. Sibling paths are relative to each sibling repo root.

| Disposition | Count | Cards |
| --- | --- | --- |
| A1 no-brainer | 1 | HSM-IMP-004 (part a; part b uses A5) |
| A2 sibling consensus | 0 | Android has no HSM Auth session code (`core/.../AppId.java:26`, `management/.../Capability.java:33–34` @f4626856 are constants only) |
| A3 Python plus one sibling | 0 | Eligible parts inside HSM-IMP-003 and HSM-IMP-007 stay in interview |
| A4 split siblings | 0 | None; HSM-IMP-008 is High, so A4 does not apply |
| A5 internal precedent | 1 | CORE-HSM-X002 |
| A6 keep and document | 0 | None as a whole card; CORE-HSM-X002 part (iii) is documentation only |
| Interview (gated) | 6 | HSM-IMP-002 (G1), HSM-IMP-005 (G1), HSM-IMP-008 (G1), HSM-IMP-001 (G3, G5), HSM-IMP-003 (G5), CORE-HSM-X001 (G5) |
| Interview (no gate, no single auto rule) | 4 | HSM-IMP-006, HSM-IMP-007, CORE-HSM-X003, CORE-HSM-X004 |
| T1 hardware qualification | 1 | Qualification card (no ID in master; labelled QUAL-HSM here) |
| T2 tag | 12 of 13 | Every classified card except the qualification card |
| Folded | 1 | CORE-HSM-X005 into HSM-IMP-008 |

## Index

| Card | Severity | Disposition | One-line rationale |
| --- | --- | --- | --- |
| HSM-IMP-001 | Medium | Interview (G3, G5) | Python and Rust also leave GET_CHALLENGE errors general, so precedent cannot settle it; lean keep and document. |
| HSM-IMP-002 | High | Interview (G1) | Owned key arrays survive ordinary failures; the A1 acquisition-scope part can be cleared in the same sitting. |
| HSM-IMP-003 | Medium | Interview (G5) | Silent skip and lossy decode are wrong; keeping unknown algorithms diverges from Python and Rust; the malformed-framing part is A3. |
| HSM-IMP-004 | Medium | Auto (A1 + A5) | Symmetric-only chooser (A1) and console presence prompt as the PIV tool wires it (A5); asymmetric dispatch is out of scope. |
| HSM-IMP-005 | High | Interview (G1) | Sample prints long-lived keys and takes visible secrets; product intent unsettled. |
| HSM-IMP-006 | Medium | Interview (no gate, no single rule) | Test-owned cleanup fits A1 and A5; oracle source and destructive hardware scope need a decision. |
| HSM-IMP-007 | Medium | Interview (no gate, no single rule) | Point validation for GET_PUBLIC_KEY and the HSM key is sibling-backed (A3); optional-field and API-shape policy is not. |
| HSM-IMP-008 | High | Interview (G1) | Strict 48-byte parse rejects a recorded 56-byte response; layout unknown; siblings truncate, which the card forbids. |
| CORE-HSM-X001 | Medium (risk) | Interview (G5, T2) | Reset reselects and keeps host state; OATH ends the session instead. Decide with the reset rule. |
| CORE-HSM-X002 | Low (risk) | Auto (A5) | Legacy zeroing becomes `ApduResponse.Clear()` as in PIV; partial SessionKeys cleanup added; lower copies stay with Core. |
| CORE-HSM-X003 | Medium (risk) | Interview (no gate, T2) | Whole-workflow lifetime is shared with CORE-B5-C006; no sibling precedent. |
| CORE-HSM-X004 | Low (risk) | Interview (no gate, T2) | Info label names a physical version; approve a new label; fallback stays Core-owned. |
| CORE-HSM-X005 | Folded | Folded into HSM-IMP-008 | Same response-grammar question; the master gives it no separate card body. |
| QUAL-HSM (no ID) | None assigned | T1 | Needs a device, a genuine HSM handshake, secure reset and platform lanes. |

## Interview queue (sorted High, then Medium, then Low)

### HSM-IMP-002: Own secrets from first acquisition through failure
- Gate(s): G1 (High). Part A1 (move owned acquisition under cleanup; the card's own no-brainer label) can be cleared in the same sitting.
- Severity / certainty: High. Source-confirmed: owned arrays are created before their clearing scope, so ordinary bad input or a failed prompt leaks them.
- The decision Dennis must make: Should the four owned-array sites (S1–S4 below) move into one failure-safe clearing scope now, with input and output policy left to HSM-IMP-005?
- Options:
  - A. Approve S1–S4. **Sia's lean:** it fixes confirmed leaks with no success-path change, and `CalculateSessionKeys.cs:55–105` already uses this shape.
  - B. Approve S1 only (management rotation). S2–S4 stay until HSM-IMP-005 settles the CLI shape.
  - C. Hold until HSM-IMP-005. Consequence: leaks remain longer, and the rewrite may happen twice.
- Sites: S1 `ChangeManagementKey.cs:25–26`; S2 `Cli/Commands/CredentialsCommand.cs:154–155` (second parse can throw before the inner `try` at 164); S3 `CredentialsCommand.cs:216–221` (wrong-width private key returns without clearing); S4 `HsmAuthExamples/AddSymmetricCredential.cs:50–60` (parse and prompts before the `try` at 62).
- Evidence: .NET as above (`src/YubiHsm/examples/HsmAuthTool/...`); internal precedent `HsmAuthExamples/CalculateSessionKeys.cs:55–105`. Sibling, reference only: `crates/yubikit/src/hsmauth.rs:599–609` @90940e9b zeroizes derived key arrays after the call on both success and error paths. Python `yubikit/hsmauth.py:125–135` @4ca60f70 returns immutable bytes and is not an erasure template.

### HSM-IMP-005: Diagnostic exposure versus safe-default tool
- Gate(s): G1 (High).
- Severity / certainty: High. Source-confirmed: `HsmAuthExamples/CalculateSessionKeys.cs:90–94` prints S-ENC, S-MAC and S-RMAC; `AddSymmetricCredential.cs:43–46` prints generated K-ENC and K-MAC; the CLI takes key and password values as arguments (`Cli/Commands/CredentialsCommand.cs:104–106`, `:207`). Product intent is unsettled.
- The decision Dennis must make: Should the HsmAuthTool example be safe by default (no key output or secret arguments unless a diagnostic mode is enabled)?
- Options:
  - A. Keep it diagnostic; add an explicit opt-in reveal and clearer warnings. Consequence: matches the production CLI, which prints generated K-ENC and K-MAC (`src/Cli.Commands/src/HsmAuth/HsmAuthCommands.cs:322–323`).
  - B. Safe default for the example: hidden prompts for secrets, no key output by default, diagnostics behind a separate flag. **Sia's lean, for the example only,** because applications copy examples. Leave `Cli.Commands` unchanged. Dropping generated-key output needs a protected recovery path first; `CredentialsCommand.cs:19–21` already says provisioning is separate.
  - C. Hold until the provisioning and recovery owner is named. Consequence: generated keys stay printed meanwhile.
- Evidence: .NET as above; Python `ykman/_cli/hsmauth.py:165–194` @4ca60f70 (passwords hidden at 169 and 181; symmetric key prompt visible at 190–194).

### HSM-IMP-008: Decode verified response variants, not arbitrary prefixes
- Gate(s): G1 (High). Both siblings truncate, which the card forbids: Python `yubikit/hsmauth.py:210–220` @4ca60f70 slices with no length check, and Rust `crates/yubikit/src/hsmauth.rs:282–293` @90940e9b accepts 48 or more bytes and keeps the first 48. Precedent is not authority here.
- Severity / certainty: High. A confirmed failure on a recorded development key: a supplied symmetric cryptogram yields a 56-byte response that strict parsing rejects (`src/YubiHsm/src/SessionKeys.cs:97–108`, called at `HsmAuthSession.cs:923`). The 56-byte layout and production applicability are unknown; the raw bytes were not kept (master).
- The decision Dennis must make: Authorize the recorded-device capture (T1) to establish the 56-byte layout, and keep strict 48-byte parsing until it is in?
- Options:
  - A. Authorize the capture under destructive applet authorization: transient owned storage; persist comparison booleans and offsets, not key bytes. Keep strict parsing meanwhile. **Sia's lean:** only device evidence separates the key prefix from the suffix.
  - B. Skip the capture; keep strict parsing and document the failure as a known limitation. Consequence: supplied-cryptogram calculation stays unusable on that device class.
  - C. Adopt truncation now, as both siblings do. Not recommended: the suffix meaning is unknown, and the card rejects silent widening.
- Evidence: .NET `SessionKeys.cs:28–32, 97–108`; `HsmAuthSession.cs:923`. Python `yubikit/hsmauth.py:210–220` @4ca60f70. Rust `crates/yubikit/src/hsmauth.rs:282–293` @90940e9b. Vendor C (eight-byte symmetric cryptogram, strict 48) comes from the master only and is not re-pinned here.

### HSM-IMP-001: GET_CHALLENGE retry contract
- Gate(s): G3. Python `get_challenge` (`yubikit/hsmauth.py:695–718` @4ca60f70) and Rust `get_challenge` (`crates/yubikit/src/hsmauth.rs:914–934` @90940e9b, plain `?` at 931) both leave retry statuses on the general error path. The inconsistency the card describes is shared, so precedent cannot settle it. G5 applies to the translation option, which diverges from both siblings.
- Severity / certainty: Medium. Source-confirmed inconsistency: GET_CHALLENGE uses plain `SendAsync` (`src/YubiHsm/src/HsmAuthSession.cs:612–617`), while CALCULATE and PUT use the retry checker. Unobserved: whether the applet returns 63Cx on GET_CHALLENGE with a wrong password. Consequences differ: credential exhaustion deletes the credential; management exhaustion blocks management.
- The decision Dennis must make: Keep GET_CHALLENGE on general APDU errors and document it, or translate 63Cx through the typed retry path?
- Options:
  - A. Keep and document. **Sia's lean:** both siblings do this, the XML docs already say so (`IHsmAuthSession.cs:264–273`), and no device evidence yet shows a counter change on this path.
  - B. Translate through the retry checker. Consequence: one public exception type changes on this path. Justify only after an authorized, non-exhausting observation.
  - C. Defer to the T1 wrong-password observation. Consequence: no code change now.
- Evidence: .NET `HsmAuthSession.cs:588–626`; `HsmAuthRetryException.cs:33–37`. Python `yubikit/hsmauth.py:619–626` @4ca60f70 (CALCULATE translation). Rust `crates/yubikit/src/hsmauth.rs:855–856` @90940e9b (CALCULATE translation).

### HSM-IMP-003: LIST must distinguish malformed records from future metadata
- Gate(s): G5. Keeping unknown algorithm and touch values (`src/YubiHsm/src/HsmAuthSession.cs:278–285`) diverges from Python (`yubikit/hsmauth.py:256`, strict enum) and Rust (`crates/yubikit/src/hsmauth.rs:487–489`, rejects unknown algorithms). Keeping them gives a concrete benefit: a future algorithm does not fail the whole LIST. Part 003a (reject short recognized records and invalid UTF-8 labels) is A3-eligible: Rust rejects at 482–486 and 494–495; Python decodes strictly at `yubikit/hsmauth.py:259`.
- Severity / certainty: Medium. Source-confirmed: short records are skipped silently (`HsmAuthSession.cs:274–276`); invalid label bytes are replaced (`:288`).
- The decision Dennis must make: Confirm the LIST contract: keep unknown algorithm and touch values in results, and reject malformed framing and invalid UTF-8 labels with a parse error?
- Options:
  - A. Approve both. **Sia's lean:** repo doctrine (`CLAUDE.md:156–158`) requires open carriers for extensible identifiers and strict rejection of malformed encodings, and Python and Rust agree on rejecting malformed framing.
  - B. Also reject unknown algorithm values, as Python and Rust do. Consequence: one future algorithm fails LIST for every caller, and the doctrine is contradicted.
  - C. Keep unknown values and add a raw record carrier. Consequence: a larger public-shape change; route through the T2 carrier decision.
- Evidence: .NET `HsmAuthSession.cs:255–294`; `IHsmAuthSession.cs:36–39`. Rust `crates/yubikit/src/hsmauth.rs:472–504` @90940e9b. Python `yubikit/hsmauth.py:250–263` @4ca60f70.

### HSM-IMP-006: Meaningful oracles and independent test cleanup
- Gate(s): None. No single auto rule covers the card: CTS disposal and fixture secret clearing fit A1 and A5, while oracle sources, negative-profile field choices and destructive hardware scope need a decision. Profile work depends on HSM-IMP-007 and -008.
- Severity / certainty: Medium. Evidence gaps, not proof the crypto is wrong. `Pbkdf2DerivationTests.cs:25–37` recomputes through the same .NET primitive; the asymmetric dummy flow (`HsmAuthAsymmetricTests.cs:128–158`) asserts a general `ApduException` with nonzero SW only.
- The decision Dennis must make: Approve the test-owned cleanup now (CTS disposal, fixture secret clearing), and defer oracle work and destructive hardware cleanup until after HSM-IMP-007/008 and the T1 session?
- Options:
  - A. Split: cleanup now; oracles and hardware later. **Sia's lean:** cleanup needs no product decision, and the oracle question depends on profile evidence.
  - B. Also add independent PBKDF2 vectors now, computed outside .NET (for example Python `hashlib.pbkdf2_hmac`) with the command recorded. Consequence: Dennis picks the vector source first.
  - C. Defer the whole card. Consequence: test-owned secrets stay uncleared on failure paths.
- Evidence: .NET `HsmAuthSessionTests.cs:18–20, 199–209`; `HsmAuthAsymmetricTests.cs:19–20, 128–158`; `Pbkdf2DerivationTests.cs:25–37`; `TestExtensions/HsmAuthTestStateExtensions.cs:43–67`. Internal: `src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/OathSessionTests.cs:31` (same helper), `OathLifecycleTimeTests.cs:38` (`using var` precedent), `src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Device/DeviceSelectorTests.cs:278` (`TestContext.Current.CancellationToken`; root `CLAUDE.md:121`).

### HSM-IMP-007: Profile validation must name its actor
- Gate(s): None strictly. G2 cannot be evaluated: no normative applet spec located (GAP-SPEC-1). No single auto rule covers the card. Part 007a (validate P-256 points) is A3-eligible: Python (`yubikit/hsmauth.py:452` @4ca60f70) and Rust (`crates/yubikit/src/hsmauth.rs:681` @90940e9b) both validate GET_PUBLIC_KEY output, and both take a typed P-256 key for the HSM static key (Python `yubikit/hsmauth.py:659, 674`; Rust `crates/yubikit/src/hsmauth.rs:882–890`).
- Severity / certainty: Medium. Source-confirmed partial validation: GET_PUBLIC_KEY checks only length (`src/YubiHsm/src/HsmAuthSession.cs:732–734`); asymmetric CALCULATE forwards the static key and cryptogram unchecked (`:553–558`). No peer-trust bypass demonstrated (master).
- The decision Dennis must make: Validate GET_PUBLIC_KEY output and the HSM static key as P-256 points before I/O, as Python and Rust do, while keeping the raw-bytes API?
- Options:
  - A. Yes. **Sia's lean:** sibling-backed, and an internal helper exists. The shape check (`src/Core/src/Cryptography/AsnUtilities.cs:60–75, 102–122`) is reachable because YubiHsm has friend access (`src/Core/src/Yubico.YubiKit.Core.csproj:50`). That check covers shape only; curve membership needs a .NET key import, as `AsnPublicKeyDecoder.cs:124–135` does.
  - B. Keep width-only and document that point validation belongs to the integration layer, as `IHsmAuthSession.cs:335–339` says today. Consequence: diverges from both siblings.
  - C. Typed key input, as both siblings have. Consequence: a breaking public change, and a larger one.
- Not asked now: present-but-empty optional values (Python omits them at `yubikit/hsmauth.py:606–610`; Rust sends `Some(&[])` as a TLV at `hsmauth.rs:835–850`; C# sends an empty tag 78 at `HsmAuthSession.cs:440–441`), cryptogram and challenge shape, and the password gate. These wait for the HSM-IMP-008 evidence.
- Evidence: .NET `HsmAuthSession.cs:529–575, 577–585, 716–737`. Python `yubikit/hsmauth.py:440–452, 655–693` @4ca60f70. Rust `crates/yubikit/src/hsmauth.rs:673–682, 882–912, 936–958` @90940e9b. Core as above.

### CORE-HSM-X001: Reset and authorization state are not channel recovery
- Gate(s): G5 for option A. Python `reset` (`yubikit/hsmauth.py:245–248` @4ca60f70) and Rust `reset` (`crates/yubikit/src/hsmauth.rs:464–469` @90940e9b) send RESET and keep no terminal state. T2 theme 1.
- Severity / certainty: Medium (risk). Source-confirmed: after RESET the session reselects and keeps SCP host state and readiness flags (`src/YubiHsm/src/HsmAuthSession.cs:512–525`; `IHsmAuthSession.cs:198–206`). No demonstrated breakage.
- The decision Dennis must make: Should a successful HSM `ResetAsync` end the session, as OATH does, or keep the reselect and document the limitation?
- Options:
  - A. End the session; callers reopen. **Sia's lean,** if the T2 session adopts the OATH rule. Consequence: one behaviour change; the reset docs already tell callers to reopen.
  - B. Explicit reinitialization: define retained options, run a new handshake, and publish readiness only after success. Consequence: a larger change.
  - C. Keep and document. Consequence: protected calls after reset remain the caller's risk.
- Evidence: .NET as above. OATH `src/Oath/src/OathSession.cs:670–682` (session ends; logs reopen) and `src/Oath/src/IOathSession.cs:232–239`.

### CORE-HSM-X003: Lease or serialize the complete public workflow
- Gate(s): No gate in the strict sense. Siblings are synchronous, so there is no sibling lifetime precedent. The decision is shared with CORE-B5-C006 (T2 theme 3); no single auto rule.
- Severity / certainty: Medium (lifecycle risk). Source-confirmed phase boundaries: LIST policy lookup (`src/YubiHsm/src/HsmAuthSession.cs:859–893`), CALCULATE (`:425–426`, `:447–453`), and callbacks inside `src/Core/src/Sessions/ApplicationSession.cs:139–205`.
- The decision Dennis must make: For HSM CALCULATE, admit the whole workflow (LIST, request, CALCULATE, parse, resolution, cleanup) as one guarded unit, or keep per-exchange guards plus documented caller serialization?
- Options:
  - A. Whole-operation admission. Consequence: overlap and disposal rules must be defined, and CORE-B5-C006 extends them to other applets.
  - B. Keep per-exchange guards and document. **Sia's lean for now:** `IHsmAuthSession.cs:24–27` already asks callers to await whole tasks.
  - C. Defer to the T2 session with CORE-B5-C006. Consequence: no change until then.
- Evidence: .NET as above; `ApplicationSession.cs:139–156, 175–205`; `IHsmAuthSession.cs:24–27`.

### CORE-HSM-X004: Effective version is policy, not a physical reading
- Gate(s): None. No single auto rule: the display label needs agreement, and fallback, major-zero and override semantics are Core-owned (T2 theme 6).
- Severity / certainty: Low (risk). Source-confirmed: `examples/HsmAuthTool/Cli/Commands/InfoCommand.cs:30–32` prints `session.FirmwareVersion` as "YubiHSM Auth version", while its own comment says that value is session feature policy.
- The decision Dennis must make: Approve the label "Effective session feature policy" for the Info output, with fallback and override behaviour unchanged?
- Options:
  - A. Approve the label only. **Sia's lean:** no code-path change.
  - B. Also expose the raw SELECT version and its source as a separate carrier. Consequence: an API change; defer.
  - C. Keep the current label. Consequence: the output keeps presenting policy as a physical version.
- Evidence: .NET `InfoCommand.cs:30–32`; `src/YubiHsm/src/Backend/HsmAuthBackend.cs:27–33, 44–57`; `HsmAuthSession.cs:158–159`. Python `yubikit/hsmauth.py:226–238` @4ca60f70 (SELECT, then patch at 232) and `yubikit/core/__init__.py:246–247` @4ca60f70 (patches only exact 0.0.1).

## Auto-approved

Dennis can veto either card before the engineer starts it.

### HSM-IMP-004: Honest calculation dispatch and presence notification
- Rule: A1 for part (a), the card's own no-brainer label (symmetric-only chooser). A5 for part (b), a console presence prompt at session creation, with internal precedent in the PIV tool and the CLI session options. Part (c), asymmetric dispatch, is not approved here: it needs HSM-IMP-007 and -008 first. If the T2 sample-output decision (theme 7) goes the other way, drop part (b).
- Evidence pin (E2): .NET `examples/HsmAuthTool/HsmAuthExamples/CalculateSessionKeys.cs:17–18, 26–29`; `examples/HsmAuthTool/Cli/Menus/SessionKeyMenu.cs:21–25`; signature `src/YubiHsm/src/IYubiKeyExtensions.cs:40–41`. Internal precedent (A5): `src/Piv/examples/PivTool/Cli/Output/OutputHelpers.cs:26–29`; `src/Cli.Commands/src/Infrastructure/YkDeviceContext.cs:48–52`. Sibling: neither `ykman/_cli/hsmauth.py` @4ca60f70 nor `crates/ykman-cli/src/hsmauth.rs` @90940e9b has a calculation command, so there is no sibling chooser precedent.
- Pseudocode for the fix:
```text
(a) CalculateSessionKeys.RunAsync, replacing lines 26-29:
    var symmetric = credentials
        .Where(c => c.Algorithm == HsmAuthAlgorithm.Aes128YubicoAuthentication)
        .ToList();
    if (symmetric.Count == 0) { info "No symmetric credentials available."; return; }
    var labels = symmetric.OrderBy(c => c.Label, StringComparer.OrdinalIgnoreCase)
        .Select(c => c.Label).ToList();

(b) SessionKeyMenu.RunAsync, replacing lines 23-24:
    await using var session = await selection.Device.CreateHsmAuthSessionAsync(
        options: new SessionCreationOptions { UserPresencePrompt = ConsoleUserPresencePrompt.Instance },
        cancellationToken: cancellationToken);
    // usings: Yubico.YubiKit.Cli.Shared.Output (ConsoleUserPresencePrompt), Yubico.YubiKit.Core.Sessions
```
- Test to add: if the filter is extracted to a pure function, a behaviour test over AES-only, EC-only, mixed and empty lists. The examples have no test project; do not add validation-only tests. Part (b) has no automated test; an attended touch-required run belongs to T1.
- E1 escalation risk: stop if `ConsoleUserPresencePrompt` cannot be referenced from the example (`HsmAuthTool.csproj:22` already references Cli.Shared, so this should hold). Stop if a presence request is not resolved on cancellation (check `ApplicationSession.cs:175–205`). Stop if asymmetric dispatch enters this change.

### CORE-HSM-X002: Supported final-owner clearing and independent copies
- Rule: A5. Internal precedent: PIV clears its final response with `response.Clear()` in a `finally`. Part (i), the owner-API substitution, is also the card's own no-brainer label. Part (ii), partial-construction cleanup, follows the same finally-clear pattern. Part (iii), error-path buffers, is A6: keep and document.
- Evidence pin (E2): .NET internal precedent `src/Piv/src/Cryptography/PrivateKeyOperations.cs:145–153` (`response.Clear()` at 152); owner API `src/Core/src/Protocols/SmartCard/Apdu/ApduResponse.cs:25, 104`; no payload on error `src/Core/src/Protocols/SmartCard/Apdu/ApduException.cs:108–129`. Sibling reference only: `crates/yubikit/src/hsmauth.rs:599–609` @90940e9b (Rust zeroizes derived key arrays on both paths).
- Scope: (i) replace `ZeroApduResponse` with `response.Clear()`; (ii) on partial `SessionKeys` construction, clear the arrays already created; (iii) keep the error-path buffers that sit before the success `try`, and say so in a comment. Out of scope: formatter, transport, fragment, decrypted and TLV staging copies, which stay with the Core owners (master CORE-C-C005/C006/C007/C020, CORE-B6-C002/C006/C007).
- Pseudocode for the fix:
```text
HsmAuthSession.CalculateSessionKeysAsync, finally (HsmAuthSession.cs:936-939):
    response.Clear();                  // replaces ZeroApduResponse(response)
Delete internal static ZeroApduResponse (HsmAuthSession.cs:953-960).

SessionKeys.Parse (SessionKeys.cs:104-108):
    create sEnc, sMac, sRmac in order;
    if a later step throws, clear the arrays already created, then rethrow.
    success path unchanged.
```
- Test to add: rewrite `SessionKeysTests.ZeroApduResponse_ClearsOwnedRawResponseStorage` (`SessionKeysTests.cs:104–116`) to call `response.Clear()`, keeping the same witness (every raw byte zero). No test for partial allocation: it needs allocation fault injection, and a fake test would break the test-value rule in the root `CLAUDE.md`.
- E1 escalation risk: stop if the final response array is pooled or still read by the transport after the call (`Clear` would zero a live buffer). Stop if `SessionKeys` gains a public surface. Stop if any CALCULATE error response is found to carry data; `ApduException` keeps only SW and header, so this is not expected.

## T1 hardware qualification

| Card | What needs a device | Device/firmware/transport/OS |
| --- | --- | --- |
| HSM-IMP-008 | Recorded-device CALCULATE layout: exact request, three key regions, two candidate tails, post-call retries; destructive applet authorization | Development YubiKey with a symmetric cryptogram; firmware provenance unresolved (master cites 0.0.1 and 5.8.0); USB SmartCard |
| HSM-IMP-001 | Authorized, non-exhausting GET_CHALLENGE wrong-password observation, plus recovery | YubiKey 5.7.1+ (password transmitted) and 5.6.0–5.7.0 (password omitted, for contrast); USB SmartCard |
| HSM-IMP-004 (part b), CORE-HSM-X003 | Attended touch-required CALCULATE: request and resolution on success, failure and cancel; token, request, device and terminal times | YubiKey 5.4.3+ with a touch-required credential; USB SmartCard |
| CORE-HSM-X001 | Secure reset: authenticated SCP setup, RESET, unprotected re-SELECT, next protected command, fresh recovery | YubiKey with SCP keys (5.4.3+); USB SmartCard and NFC |
| HSM-IMP-006 | Destructive final reset and independent readback on a dedicated device; restore the factory management key | Dedicated test YubiKey; any firmware; USB SmartCard |
| CORE-HSM-X004 | Feature-table check on older and production firmware | Firmware matrix including 5.4.3, 5.6.0, 5.7.1 and 5.8.0; USB SmartCard |
| QUAL-HSM (no ID) | Genuine HSM handshake: real HSM challenge and EPK-SD, cryptogram, peer verification; HSM firmware, connector and library revision; trusted static-key provenance | YubiHSM 2 with connector; YubiKey 5.6.0+ |
| QUAL-HSM (no ID) | Platform lanes: NFC; JIT versus Native AOT artifacts; Core listener and loader questions keep their existing owners | NFC reader; Windows, macOS and Linux |
| QUAL-HSM (no ID) | Documentary only, no device: response and profile authority (GAP-SPEC-1) | None |

## T2 cross-cutting candidates

| Theme | Card(s) here | Why it is cross-cutting |
| --- | --- | --- |
| 1. Session state after reset or failed authentication | CORE-HSM-X001; HSM-IMP-001 (retry consequence) | OATH `ResetAsync` ends the session (`src/Oath/src/OathSession.cs:670–682`); HSM keeps it through reselect. The master lists this as a T2 example. |
| 2. Retry-exception translation scope | HSM-IMP-001 | Typed retry exceptions exist in PIV (`src/Piv/src/InvalidPinException.cs:25`) and OpenPGP (`src/OpenPgp/src/OpenPgpInvalidPinException.cs:32, 57`, which maps 6983 to zero). HSM's own docs check every credential-password and management-key operation except GET_CHALLENGE (`src/YubiHsm/src/HsmAuthRetryException.cs:24–37`). |
| 3. Operation lifetime, cancellation and disposal | CORE-HSM-X003; HSM-IMP-004 (prompt lifetime); HSM-IMP-006 (CTS ownership) | Shared owner CORE-B5-C006 (master). The `NewToken` helper also appears in OATH integration tests (`OathSessionTests.cs:31`, `OathHashAlgorithmTests.cs:27`, `OathPasswordChangeTests.cs:38`). |
| 4. Secret and key-material zeroing scope | HSM-IMP-002; CORE-HSM-X002; HSM-IMP-006 (fixture secrets); HSM-IMP-008 (owned result) | Named T2 example. PIV clears its final response with `response.Clear()` (`PrivateKeyOperations.cs:152`). Lower copies belong to Core owners. |
| 5. Forward-compatible response carriers versus strict parsing | HSM-IMP-003; HSM-IMP-008 | Repo doctrine (`CLAUDE.md:156–158`) governs every parser of extensible identifiers or response suffixes. |
| 6. Version and feature-policy provenance and overrides | CORE-HSM-X004; CORE-HSM-X001 (reset keeps policy) | Core version policy (master CORE-B5-C001, A-C015). Python patches only exact 0.0.1 (`yubikit/core/__init__.py:246–247` @4ca60f70); C# has its own override rule. |
| 7. Sample and CLI secret output and presence wiring | HSM-IMP-005; HSM-IMP-004 (part b) | Production CLI prints generated K-ENC and K-MAC (`src/Cli.Commands/src/HsmAuth/HsmAuthCommands.cs:322–323`); OTP tools print generated keys (`src/Cli.Commands/src/Otp/OtpCommands.cs:396`). CLI and PIV wire the console presence prompt (`YkDeviceContext.cs:48–52`; `PivTool/Cli/Output/OutputHelpers.cs:26–29`). |
| 8. EC public-point admission (candidate; other applets not surveyed) | HSM-IMP-007 | Core shape helper and decoder (`src/Core/src/Cryptography/AsnUtilities.cs`, `AsnPublicKeyDecoder.cs:114–135`) apply to any EC point input. |

## Coverage check

Review-map IDs. Each appears exactly once in the Index: HSM-IMP-001, HSM-IMP-002, HSM-IMP-003, HSM-IMP-004, HSM-IMP-005, HSM-IMP-006, HSM-IMP-007, HSM-IMP-008, CORE-HSM-X001, CORE-HSM-X002, CORE-HSM-X003, CORE-HSM-X004, CORE-HSM-X005 (folded into HSM-IMP-008; the master gives it no separate body), and the qualification card (no ID; labelled QUAL-HSM here). That is 14 rows.

Coverage-ledger exclusions, deliberately not treated as cards:
- HSM-DEP-001, HSM-DEP-002, HSM-DEP-003: dependency questions, not cards (master).
- CORE-HSM-X006 and CORE-HSM-X007: accepted prose. Their open facets sit in HSM-IMP-001, -003 and -007, and in HSM-IMP-002, -004, -005 and -006.
- Retracted `core-001:CORE-HSM-X007#retry-message-observation`: not reopened. `HsmAuthSession.cs:995–996` still reads "Invalid credential password", as the master states.
- C001–C012: accepted documentation origins, not open IDs. The rejected Pass-1 test-guide link claim is excluded.
- Eleven non-HSM routed provenance keys: owned by OATH, SD, OTP, OpenPGP and PIV.

Observed outside the card set (not classified; for awareness): C# pads byte credential passwords shorter than 16 bytes (`src/YubiHsm/src/HsmAuthSession.cs:193–222`). Python and Rust require exactly 16 bytes for byte input (`yubikit/hsmauth.py:131–134` @4ca60f70; `crates/yubikit/src/hsmauth.rs:165–172` @90940e9b) and pad only string input. The master does not list this difference.
