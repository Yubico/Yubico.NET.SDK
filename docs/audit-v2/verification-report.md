# Audit v2.0 pre-release: verification and remediation plan

| | |
| --- | --- |
| Source audit | [original-audit.md](original-audit.md) (internal security review) |
| Audited commits | `55e02064`, `65e36386` |
| Verification base | `yubikit` at `fdfcd6fd` (10 commits after `65e36386`) |
| Branch / worktree | `audit/v2-verification` at `worktrees/yubikit-audit-v2-verification` |
| Date | 2026-09-25 |
| Method | Eight verification agents working in parallel, each in its own worktree. They traced code at HEAD, quoted the specs, compared against reference implementations (python-fido2, libfido2, yubikey-manager, yubikit-android/swift, legacy .NET SDK v1) and wrote failing repro tests. The orchestrator then ran every hardware repro one at a time. |
| Review | Cross-vendor review of this document by four reviewers (GPT on FIDO/WebAuthn, applets and CI; Claude on largeBlob and PIV/Core). Their corrections are applied inline as "Review correction" bullets or direct edits. The #22 conclusions were then re-tested on hardware (poll-during-session attack, fast sequential processes). |
| Devices | YubiKey 5C NFC fw 5.7.4 (5.7.4 test key), YubiKey 5 NFC Enhanced PIN fw 5.8.0-alpha.2 (5.8.0 test key). Both are allow-listed test devices. |
| Scope | Every finding rated MED or higher, plus #27 (LOW/MED) and #28 (MED/LOW). That covers #1–#14, #16, #17, #19, #22–#28. The LOW-only findings #15, #18, #20 and #21 and the "items to consider" were not verified. #20 got incidental coverage under 25e. |
| Spec editions | CTAP 2.3 PS (2026-02-26), WebAuthn Level 3, RFC 9580, RFC 8032, RFC 5869, RFC 6238, NIST SP 800-73-5, FIPS 186-5, GlobalPlatform SCP03 (Amd D) and SCP11 (Amd F v1.3), OpenPGP card 3.4.1, YKOATH, Yubico PIV extensions, the Yubico previewSign draft v4 |

## Verdict counts

Each #25 and #26 sub-item is counted separately. That gives 48 audit items, plus 4 new findings.

| Verdict | Count |
| --- | --- |
| Confirmed | 30 |
| Confirmed, with a correction to the audit's claim | 15 |
| Partial | 1 (#17) |
| By design, but undocumented | 1 (26j) |
| Not reproduced | 1 (26o) |

No finding was already fixed at HEAD. #17 was mostly fixed in `65e36386`, and the audit says so itself.

Test run on the consolidated worktree: 83 unit repro tests. 76 fail as intended because they assert the spec-correct behaviour. 5 are passing controls or counterexamples, and 2 are skipped child-process bodies. Hardware without touch: #10, #22, #24 and #28 all reproduced on real devices. With the user present, #1, #2, #7 and #13 also reproduced on hardware, and #11 was confirmed as tolerated by the YubiKey.

## Summary table

"Ours" is our severity rating. The Effort column estimates the size of the fix: S is up to about 1 day, M is a few days, L is a week or more, or a format or API redesign.

| # | Jira | Finding | Audit | Verdict | Ours | Evidence | Effort | API impact |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 1609 | WebAuthn PRF is not translated to hmac-secret | MED | Confirmed, corrected | MED | unit + **hardware** | L | low-level `WithPrf` breaking |
| 2 | 1610 | Required UV sends both `uv` and `pinUvAuthParam` | MED | Confirmed, corrected | **HIGH** | unit + **hardware (0x2C on both keys)** | S | none |
| 3 | 1611 | Direct CTAP `largeBlob` write lacks `originalSize` | MED | Confirmed, corrected | LOW (YubiKey) | unit; GetInfo | M | additive/breaking |
| 4 | 1612 | getAssertion 0x08 unsigned outputs dropped | MED | Confirmed, corrected | LOW (YubiKey) | unit | S | additive |
| 5 | 1613 | `largeBlob.supported` false despite key | MED | Confirmed | MED | unit | S | none |
| 6 | 1614 | attestation=none forwards real statement | MED | Confirmed, corrected | MED | unit | S | none |
| 7 | 1615 | `residentKey=preferred` sends `rk=false` | MED | Confirmed, corrected | MED | unit + **hardware** | S | none |
| 8 | 1616 | `topOrigin` sent with `crossOrigin=false` | MED | Confirmed | MED | unit | S | behavioural |
| 9 | 1617 | Ceremony timeout ignored | MED | Confirmed | MED | unit | S | none |
| 10 | 1618 | makeCredential `up=false` serialized | MED | Confirmed | MED | unit + **hardware (0x2C)** | S | behavioural |
| 11 | 1619 | credBlob get sends `h''`, should send `true` | MED | Confirmed | MED | unit; hardware: YubiKey tolerates it | S | additive |
| 12 | 1620 | Fragment length 1024, not maxMsgSize−64 | MED | Confirmed | MED (benign on YubiKey) | unit; GetInfo | S | additive |
| 13 | 1621 | Private, non-CTAP large-blob entry format | MED | Confirmed | **HIGH** | unit + **hardware (both directions)** | L | breaking (pre-release) |
| 14 | 1622 | Corrupt checksum throws instead of resetting | MED | Confirmed | MED | unit | S | behavioural |
| 16 | 1624 | Large-blob plaintext left uncleared | MED | Confirmed, corrected | LOW-MED | code inspection | S | none |
| 17 | 1625 | Private-key intermediate copies | MED | Partial | LOW | existing tests; runtime source | S | none |
| 19 | 1627 | Docs workflow runs unpinned installer with write token | MED | Confirmed, corrected | MED | static | S | CI only |
| 22 | 1630 | PIV PIN state reusable by another process | MED | Confirmed | MED | **hardware, both devices, cross-process** | M | additive (opt-in) |
| 23 | 1631 | Short SCP response kills the process | MED | Confirmed | MED | unit (child process: exit 134) | S | none |
| 24 | 1632 | Ed25519 message truncated/padded to 32 bytes | MED/HIGH | Confirmed | **HIGH** | unit + **hardware (verification fails)** | S | none |
| 25a | 1633 | CLI OTP `--length` unchecked before `stackalloc` | MED | Confirmed | LOW | unit (child process) | S | none |
| 25b | 1633 | DeviceInfo pagination never terminates | MED | Confirmed | LOW-MED | unit | S | none |
| 25c | 1633 | `GetInt32` wide range returns a constant | MED | Confirmed | LOW | unit | S | none |
| 25d | 1633 | HKDF counter wraps at 8129–8160 bytes | MED | Confirmed, corrected | INFO-LOW | unit | S | none |
| 25e | 1633/1628 | TLV 5+ byte length wraps; truncated header throws IOOR | MED/LOW | Confirmed, corrected | LOW | unit | S | none |
| 25f | 1633 | `EncodeDictionary` underallocates | MED | Confirmed | LOW | unit | S | none |
| 25g | 1633 | Non-positive `maxCredentialCountInList` stalls preflight | MED | Confirmed, corrected | LOW-MED | unit | S | none |
| 25h | 1633 | OpenPGP KDF count sign-wrap | MED | Confirmed | MED | unit | S | behavioural |
| 25i | 1633 | OATH period 0 aborts batch | MED | Confirmed | MED | unit (see #28) | S | none |
| 26a | 1634 | PRF `evalByCredential` ignored or fails | MED | Confirmed | MED | unit | (with #1) | none |
| 26b | 1634 | PRF `enabled` ignores value | MED | Confirmed, corrected | MED | unit | (with #1) | none |
| 26c | 1634 | previewSign falls back to auth key | MED | Confirmed | **HIGH** | unit | S | none |
| 26d | 1634 | Low-level previewSign picks `.First()` | MED | Confirmed | MED | unit | S | behavioural |
| 26e | 1634 | PIV set-retries resets PIN/PUK silently | MED | Confirmed | **HIGH** (doc) | spec + code | S | doc / optional breaking |
| 26f | 1634 | PIV EC right-pads digests; RSA truncates oversize | MED | Confirmed | MED | unit | S | behavioural |
| 26g | 1634 | Redirected credential input skips policy | MED | Confirmed | LOW-MED | unit | S | behavioural |
| 26h | 1634 | Confirmation strands first credential | MED | Confirmed | LOW | unit | S | none |
| 26i | 1634 | OpenPGP import targets template slot, not `keyRef` | MED | Confirmed | MED | unit (APDU) | S | behavioural |
| 26j | 1634 | OpenPGP attestation overwrites cert | MED | By design | MED (doc) | vendor doc | S | doc |
| 26k | 1634 | OATH ops ignore `DeviceId` | MED | Confirmed | MED | unit | S | behavioural |
| 26l | 1634 | OTP update drops `AllowUpdate`/`ProtectSlot2` | MED | Confirmed, corrected | MED/LOW | unit | S | behavioural |
| 26m | 1634 | SD empty allowlist removes restriction | MED | Confirmed, corrected | **HIGH** (misuse) | unit + GP spec | S | behavioural |
| 26n | 1634 | DeviceConfig builder reuse mutates snapshot | MED | Confirmed | MED | unit | S | none |
| 26o | 1634 | OATH retry sends zero seed | MED | **Not reproduced** | none | passing counterexample | – | – |
| 26p | 1634 | HMAC-SHA1 challenge padding ambiguity | MED | Confirmed, corrected | LOW-MED | unit | S | behavioural |
| 26q | 1634 | OpenPGP KDF short count skips PIN | MED | Confirmed | MED | unit | S | behavioural |
| 27 | 1635 | Mismatched PKCS#8 public/private accepted | LOW/MED | Confirmed | MED | unit | S | behavioural |
| 28 | 1636 | OATH period=0 breaks `CalculateAllAsync` | MED/LOW | Confirmed | MED | unit + **hardware (DivideByZero)** | S | none |
| N1 | – | Duplicate `pubKeyCredParams` accepted | – | Confirmed | LOW | unit | S | behavioural |
| N2 | – | SCP init unchecked slicing; bare 9000 skips R-MAC | – | Observation | LOW / design question | code | S | – |
| N3 | – | CLI: confirmation unusable with redirected stdin; static password programmed as UTF-8 bytes, not scan codes | – | Confirmed | LOW | scratch + code | S | none |
| N4 | – | Lockless npm installs; monthly synthesis co-locates OAuth + write token | – | Observation | LOW-MED | static | S | CI only |

The #25 and #26 sub-item letters are ours. The audit lists each of #25 and #26 as a single row.

## Key corrections to the audit

- **#22 is confirmed and slightly broader than the audit says.**
  - An attacker holding a shared handle can sign while the victim's session is still open, not only after the victim disposes. The same happens in-process.
  - On macOS, sequential separate processes did not reproduce, and the reason is unknown. That was not tested on other platforms, so it is not a mitigation.
  - The audit's `CoreCompatSwitches.OpenSmartCardHandlesExclusively` already exists. It only blocks an attacker that connected first, and then the victim cannot connect at all.
- **#26o (OATH retry zero seed) does not reproduce.** The retry helper derives a fresh key from the provider's password. It zeroes only that derived key, and it retries the operation, not the validation.
- **25d:** the RFC 5869 limit L ≤ 255·HashLen is already enforced (`HkdfUtilities.cs:45-48`). The real defect is that a one-byte counter wraps for valid lengths 8129–8160. No caller can reach that range.
- **25e:** the audit's example `84 FF FF FF FF` is already rejected. Lengths of 5 or more bytes, such as `85 01 00 00 00 01`, wrap and are accepted.
- **#3 and #4 cannot be reached on current YubiKeys.** The `{read, write, originalSize}` shape belongs to the direct CTAP §12.4 `largeBlob` extension, which neither device advertises. YubiKeys use `largeBlobKey` (§12.3) with authenticatorLargeBlobs. makeCredential already decodes its 0x06 unsigned outputs.
- **#12 does not fail on YubiKeys.** They advertise maxMsgSize 1536, so maxFragmentLength is 1472, and 1024 fits. It fails on authenticators that omit maxMsgSize (default 960) or advertise less than 1088.
- **#6:** WebAuthn L3 no longer zeroes the AAGUID for `none`, according to the L3 change log. Only the statement has to be replaced.
- **#7:** the WebAuthn builder emits no `rk` entry at all. `WebAuthnBackend.cs:124-125` then turns the missing entry into `false`, and the encoder serializes `"rk": false`. The same backend coercion produces `"uv": false` (#2).
- **#2:** the backend is now `WebAuthnBackend`. Getting a token with built-in UV is valid on CTAP 2.1+. The only defect is that `uv` and `pinUvAuthParam` appear in the same request.
- **#1:** the audit implies PRF is simply not translated. In fact a `PrfInput.ComputeSalt` helper and a low-level `WithHmacSecret(...)` encoder both exist, but the WebAuthn path uses neither. Instead it sends a CTAP `"prf"` key that no spec defines. This also affects 26a and 26b.
- **#2 is worse than the audit says.** It is a functional break, not just non-conformance. YubiKeys reject `uv:true` together with `pinUvAuthParam` (0x2C), so WebAuthn registration with `UserVerification=Required` and a PIN always fails.
- **#13 is worse than the audit says.** On a key that already holds a spec-format large-blob entry, the SDK cannot write at all.
- **#10 is not a user-presence bypass.** The YubiKey rejects the request with CTAP2_ERR_INVALID_OPTION (0x2C) before asking for touch. The SDK's own WebAuthn registration never sets `up=false`. getAssertion `up=false`, which exclude-list preflight uses, is valid and must stay.
- **#17 is mostly fixed at HEAD.** What remains is an abandoned `D` array inside .NET's macOS ECC import (`EccSecurityTransforms.macOS.cs`). PIV and Security Domain imports do not call `ECPrivateKey.CreateFromValue`. The suggested `CreateFromParameters` workaround only helps callers who already have the correct public point.
- **#19:** the installer does check the binary's SHA-256, but against a manifest fetched from the same origin. `2.1.199` selects the final version, not the bootstrap binary. The real least-privilege gap: both generator jobs inherit `contents: write` and `pull-requests: write`, and their checkout persists the token in `.git/config` before the installer runs.
- **26j is device behaviour.** Yubico's attestation documentation says the cardholder certificate slot is overwritten. The SDK's gap is that it doesn't warn.
- **26m:** the GlobalPlatform spec itself defines an empty or missing allowlist as "accept all certificates signed by the CA-KLOC". An empty list cannot mean deny-all on the device.
- **26p:** yubikey-manager uses the same trailing-byte padding rule. The encoding collision is real, but whether the device output collides depends on the slot's challenge mode.

## Remediation roadmap

Workstreams in suggested order. **B** means we recommend treating it as a release blocker for 2.0 GA. Blockers are: anything we rate HIGH, anything that crashes the process, anything that produces wrong cryptographic output, and any spec MUST violation on the default WebAuthn path.

| WS | Scope | Findings | Blocker | Rationale |
| --- | --- | --- | --- | --- |
| 1 | WebAuthn/CTAP request shaping | #2, #7, #6, #8, #9, #10, 25g, N1 | B: #2, #6, #7, #8 | MUST violations on the default ceremony path. One shared fix: keep absent options absent in `WebAuthnBackend`, and pass `AuthenticatorInfo` into request construction. |
| 2 | PRF / hmac-secret and extensions | #1, 26a, 26b, #11, 26c, 26d | B: #1, 26c | PRF is currently non-functional against any spec-conforming authenticator. 26c can bind an application to the wrong key. |
| 3 | largeBlob rewrite | #13, then #12, #14, #16, #5, #3/#4 | B: #13, #5 | The format is not interoperable (data unreadable by any other client), and it is simplest to break before 2.0 ships. The rest of the list falls out of the same rewrite. |
| 4 | PIV correctness | #24, 26f, 26e, #22 | B: #24, 26e (doc), 26f | Wrong signatures, silently altered inputs, and silent credential reset. #22 needs a policy decision (see below); ship the documentation now, with opt-in hardening. |
| 5 | Untrusted-input hardening | #23, #28/25i, 25b, 25e/#20, 25f, 25c, 25d, 25a, N2 | B: #23, #28 | #23 kills the process from a single on-path byte. #28 lets one bad credential break the whole batch. The rest are cheap bounds checks. |
| 6 | Key material | #27, #17 | – | Integrity check on import (Q = D·G, n = P·Q). #17 is residual and runtime-side: file upstream. |
| 7 | Applet API footguns | 26i, 26k, 26l, 26m, 26n, 26q/25h, 26g, 26h, 26p, 26j | B: 26m, 26i | 26m can silently weaken SCP11 trust. 26i writes to two slots when the caller asked for one. The others are small behavioural fixes plus docs. |
| 8 | CI supply chain | #19, N4 | – (fix promptly; CI, not shipped code) | Read-only generator jobs, `persist-credentials: false`, and a pinned, committed digest for the Claude binary. Patch sketch: `docs/audit-v2/evidence/scripts/yesdk1627-docs-update.patch`. |

Dependencies:
- WS1 before WS2, for assertion-time PRF evaluation only. That depends on correct UV and token handling, because PRF results require the UV-backed secret. Registration-time `hmac-secret: true` feature detection (#1, 26b) has no such dependency and can ship first.
- In WS3, #13 comes first.
- #24 and 26f touch the same function (`PrepareDataForCrypto`), so fix them together.
- #28 and 25i share a root cause.
- 26q and 25h share a fix.

## Decisions needed

1. **#22 PIN-state isolation.**
   - (a) Document only.
   - (b) Document, plus an opt-in session isolation policy that holds the card exclusively (or in a transaction) from VERIFY until the last private-key operation, then releases with `SCARD_RESET_CARD`.
   - (c) Change the default to exclusive, or to reset on dispose.

   Recommend **(b)**, after hardware tests on macOS, Windows and Linux. Resetting on dispose without exclusivity is not enough: hardware shows the attack works during the victim's session. Making exclusive plus reset the default would break coexistence with gpg-agent, browsers and OS smart-card services.
2. **#13 compatibility.** Either ship no reader for the old private format, or add a migration shim. Recommend **no shim**: this is pre-release, and no external consumers are known.
3. **#10.** Either reject `UserPresence=false` on makeCredential, or silently omit it. Recommend **reject**. The #10 repro tests need updating to match; see the review correction there.
4. **#1 low-level API.** Remove `ExtensionBuilder.WithPrf`, which encodes a key no spec defines, or re-implement it on top of hmac-secret. Recommend **remove it**, and keep PRF at the WebAuthn layer and `WithHmacSecret` at the CTAP layer.
5. **#1 and 26a, multiple allowed credentials with `evalByCredential`.** Either probe first to find which credential will be used, or reject the request. Recommend **reject now, probe later**. Never silently use the first one.
6. **#24 maximum Ed25519 message length.** It has not been established per firmware. Measure it on hardware and document it, and reject messages above it. Recommend **measure**.
7. **26e retry limits.** Either document the reset, or also require an explicit acknowledgement parameter. Recommend **document now**, and consider the acknowledgement parameter as a breaking 2.0 change.
8. **26f short EC digest.** Either left-pad it, or reject any length that isn't the curve's canonical length. Recommend **left-pad**, which matches FIPS 186-5 integer semantics and yubikit-android. Reject oversized RSA input in either case.
9. **26m deny-all.** Either reject an empty list in `StoreAllowListAsync` (keeping `ClearAllowListAsync` explicit), or treat empty as meaning clear. Recommend **reject**, and document that deny-all means removing the trust anchor.
10. **#28 and 25i, a malformed credential in a batch.** Either omit it with a diagnostic, or return it with a null code. Recommend **omit and log**.
11. **25h and 26q, KDF counts.** Decide the maximum supported count, and what to do about cards already provisioned with short counts. Recommend **always hashing at least one complete salt-plus-PIN input, even when the encoded count is shorter**, as RFC 9580 §3.7.1.3 requires, and documenting that this changes the derived value on such cards. Whether to also reject or migrate short-count KDF objects is a separate decision.
12. **25g.** Either reject a non-positive `maxCredentialCountInList` when decoding GetInfo, or clamp it to 1. Recommend **reject in the decoder, and guard in preflight as well**.
13. **#17 residual.** Either file upstream with dotnet/runtime, or document and accept it. Recommend **file upstream**.
14. **#19 digest ownership.** Decide who reviews and approves updates to the Claude binary digest, and whether the monthly synthesis workflows get the same job split.
15. **N2, bare 9000 without R-MAC.** GP §6.2.5 says an R-MAC is required even with no data, but ykman, Android and Swift all skip it. Decide whether to be strict. Recommend a **separate discussion**; out of scope here.

# Per-finding detail

### #1 WebAuthn PRF is not translated to CTAP hmac-secret (YESDK-1609)
- Verdict / our severity: CONFIRMED-WITH-CORRECTION / MED.
- Location at HEAD: `src/WebAuthn/src/Extensions/ExtensionPipeline.cs:74-79,126-131,230-242,321-333`; `src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs:30-69,74-129`; `src/Fido2/src/Extensions/Prf/PrfInput.cs:62-79`; `src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs:140-204,214-218,230-250,367-409`; `src/Fido2/src/Cbor/FidoSessionRequestEncoding.cs:75-83`.
- Spec: “Set hmac-secret to true in the authenticator extensions input.”; “Let salt1 be the value of SHA-256(UTF8Encode("WebAuthn PRF") || 0x00 || ev.first).”; “Send an hmac-secret extension to the authenticator using the values of salt1 and, if set, salt2 as the parameters of the same name in that process. Decrypt the extension result and set results to the PRF result(s), if any.” — WebAuthn L3 §10.1.4.
- Reference implementations: python-fido2 hashes inputs, negotiates/encrypts/authenticates salts, decrypts output; libfido2 emits hmac-secret inputs.
- Evidence: Registration emits `prf:{}` instead of `hmac-secret:true`; authentication sends raw salts as `prf.eval`, without required transformation or decryption. PRF result parsing looks for `prf.eval`, and registration eval is discarded.
- Repro: `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2ExtensionAuditReproTests.cs::YESDK1609_RegistrationPrfRequestsHmacSecret` — failed as expected; WebAuthn repro `WebAuthnExtensionAuditReproTests::YESDK1609_AuthenticationPrfUsesEncryptedHmacSecretNotLiteralPrf` — failed as expected.
- Hardware: **confirmed** on 5.7.4 (user present). `WebAuthnAuditHardwareTests::YESDK1609_PrfRoundTripProducesStable32ByteResult` (UV=Preferred, to isolate it from #2) fails at registration with `ClientExtensionResults.Prf.Enabled == null`. The authenticator ignores the non-spec `prf` CTAP extension and returns no `hmac-secret` output, although GetInfo advertises `hmac-secret`. The assertion stage was not reached.
- Remediation: Feature-detect hmac-secret; hash PRF inputs, select credential-specific input, encrypt/authenticate salts, request UV, decrypt output, and return results with secret buffers zeroed.
- API impact: WebAuthn call shape can remain; direct `ExtensionBuilder.WithPrf` likely needs a breaking session-aware redesign.
- Depends on / open decisions: Related to #26a/#26b and PRF UV policy; decide direct API deprecation and registration-time eval support.

### #2 Required UV sends both uv and pinUvAuthParam (YESDK-1610)
- Verdict / our severity: CONFIRMED-WITH-CORRECTION / **HIGH** (audit MED). Hardware shows this is a functional failure, not only non-conformance: WebAuthn registration with `UserVerification=Required` and a PIN fails on every PIN-only YubiKey.
- Location at HEAD: `src/WebAuthn/src/Client/UserVerification/UvDecision.cs:93-111`; `src/WebAuthn/src/Client/WebAuthnClient.Registration.cs:298-335`; `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs:216-255`; `src/WebAuthn/src/Client/WebAuthnBackend.cs:124-139,180-199`; `src/Fido2/src/Cbor/FidoSessionRequestEncoding.cs:46-55,85-95,100-156`.
- Spec: “Platforms MUST NOT include both the "uv" option key and the pinUvAuthParam parameter in the same request.” — CTAP 2.3 §6.1. “Platforms MUST NOT include both "uv" and pinUvAuthParam parameters in same request.” — §6.2.
- Reference implementations: python-fido2 emits `uv` only for internal UV without a token, and pin_auth only when a token is used.
- Evidence: Required+PIN and built-in UV paths both produce `UseToken=true,UvOption=true`; backend/encoder serialize both. makeCredential also converts absent `uv` to false, while getAssertion preserves absence.
- Repro: `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs::YESDK1610_RegistrationTokenDoesNotAlsoRequestUv` and `::YESDK1610_AssertionTokenDoesNotAlsoRequestUv` — failed as expected; absent-UV registration control failed, assertion control passed.
- Hardware: **confirmed**. `WebAuthnAuditHardwareReproTests::YESDK1610_RequiredUvRegistrationWithPinSucceeds` fails today with `WebAuthnClientError: Invalid option` (CTAP2_ERR_INVALID_OPTION) within 1 s, before any touch. Independently, python-fido2 sending makeCredential with `{"uv": true}` plus `pinUvAuthParam` gets `INVALID_OPTION (0x2c)` on both 5.7.4 and 5.8.0-alpha.2.
- Remediation: Set `UvOption=null` whenever a token is used; preserve nullable absence in `WebAuthnBackend.MakeCredentialAsync` as assertion already does.
- API impact: None; internal transport behavior.
- Depends on / open decisions: No open decision recorded; exclude-list preflight uses token plus `up=false`, not `uv`.

### #3 Direct CTAP largeBlob write input shape (YESDK-1611)
- Verdict / our severity: CONFIRMED-WITH-CORRECTION / LOW for currently deployed YubiKeys, MED where direct `largeBlob` is advertised.
- Location at HEAD: `src/Fido2/src/Extensions/LargeBlob/LargeBlobAssertionInput.cs:28-81`; `src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs:93-110`.
- Spec: “This extension is an alternative to the to authenticatorLargeBlobs command and the largeBlobKey extension for authenticators that can accept the full contents of a largeBlob in an authenticatorGetAssertion message. Authenticators MUST NOT support both extensions.” — CTAP 2.3 §12.4. “largeblob-inputs = { ? read : true ? write : bstr ? originalSize : uint }” — §12.4.
- Reference implementations: python-fido2 and libfido2 implement WebAuthn largeBlob through largeBlobKey and authenticatorLargeBlobs; neither establishes direct-extension support on YubiKeys.
- Evidence: `WithLargeBlobWrite(data)` emits only `write`, without DEFLATE compression or `originalSize`; read plus write can also encode a prohibited combination.
- Repro: `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs::YESDK1611_DirectWriteCarriesOriginalSize` — failed as expected (map count 1, expected 2).
- Hardware: HW-RESULTS GetInfo found neither YubiKey advertises direct CTAP `largeBlob`; finding is not reachable on those devices.
- Remediation: If supporting direct §12.4, compress with DEFLATE, encode original size, reject read/write mixtures, and gate on advertised support; otherwise remove or clearly mark the surface unsupported.
- Review correction: the repro `YESDK1611_DirectWriteCarriesOriginalSize` currently expects `write` to equal the raw blob. CTAP §12.4 says "blob data is compressed in the CTAP version and the uncompressed size is stored with it", so a spec-compliant fix still fails it. Before fixing, change it to assert that `write` raw-inflates back to the blob and that `originalSize == Blob.Length`, and add a test that rejects read and write together.
- API impact: Potentially additive `OriginalSize` and validated builder contract; invalid combinations may become rejected.
- Depends on / open decisions: Related to #4; decide whether to support direct §12.4 or focus API on YubiKey largeBlobKey/storage.

### #4 Unsigned assertion extension outputs (YESDK-1612)
- Verdict / our severity: CONFIRMED-WITH-CORRECTION / MED for direct-largeBlob devices, LOW for YubiKeys using largeBlobKey.
- Location at HEAD: `src/Fido2/src/Credentials/GetAssertionResponse.cs:87-111,130-197`; makeCredential already handles key 0x06 at `src/Fido2/src/Credentials/MakeCredentialResponse.cs:79-106,188-204`.
- Spec: “unsignedExtensionOutputs (0x08) CBOR map of extension identifier → unsigned extension output values ... A map, keyed by extension identifiers, to unsigned outputs of extensions, if any.” — CTAP 2.3 §6.2 getAssertion response.
- Reference implementations: python-fido2 models top-level unsigned extension outputs on both attestation and assertion responses.
- Evidence: GetAssertionResponse extracts signed authenticator-data outputs only and skips key 8, so direct-largeBlob `written` output cannot reach callers. makeCredential key 6 is already decoded.
- Repro: `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs::YESDK1612_GetAssertionPreservesUnsignedLargeBlobOutput` — failed as expected; no `UnsignedExtensionOutputs` property.
- Hardware: HW-RESULTS says neither tested YubiKey advertises direct CTAP `largeBlob`; no hardware repro is applicable to those devices.
- Remediation: Decode key 8 as a map of encoded values, preserving the response-specific key number; expose it and pass it to consumers.
- Review correction: CTAP §6.2 says "Clients MUST treat an empty map the same as an omitted field". Decode an empty key-8 map as absent.
- API impact: Additive.
- Depends on / open decisions: Depends on #3; decide whether to expose raw values only or also a typed direct-largeBlob result.

### #5 WebAuthn supported result drops largeBlobKey (YESDK-1613)
- Verdict / our severity: CONFIRMED / MED; misreports a successfully enabled feature.
- Location at HEAD: `src/Fido2/src/Credentials/MakeCredentialResponse.cs:185-187`; `src/WebAuthn/src/Client/WebAuthnClient.Registration.cs:359-364`; `src/WebAuthn/src/Extensions/ExtensionPipeline.cs:216-227`; `src/WebAuthn/src/Extensions/Adapters/LargeBlobAdapter.cs:28-56`.
- Spec: “Set the value of largeBlobKey (0x05) in the authenticatorMakeCredential response structure (i.e., not in the extensions field of the authenticator data) to the value of the generated largeBlobKey.” — CTAP 2.3 §12.3.
- Reference implementations: python-fido2 returns supported when `response.large_blob_key is not None`, and checks GetInfo `largeBlobKey` plus `largeBlobs` option.
- Evidence: Fido2 decodes response key 5, but registration pipeline does not pass it to the adapter. Adapter searches the signed authenticator-data extension map for `largeBlobKey` instead.
- Repro: `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnLargeBlobAuditReproTests.cs::YESDK1613_LargeBlobKeyFromCtapResponseReportsSupported` — failed as expected; pipeline returned `Supported=false` despite a 32-byte key.
- Hardware: HW-RESULTS marks #5 optional; no hardware result recorded.
- Remediation: Pass `ctapResponse.LargeBlobKey` or its presence through the registration pipeline; consult unsigned outputs only for the alternative direct extension.
- Review correction: passing the key through is not enough. The adapter always sends `largeBlobKey: true`. CTAP §12.3 says "If the options field of the authenticatorMakeCredential request does not map rk to true, return CTAP2_ERR_INVALID_OPTION", so because of #7, `residentKey=preferred|discouraged` plus `largeBlob` fails the whole registration. Send `largeBlobKey` only when `rk=true` and GetInfo advertises both the `largeBlobKey` extension and the `largeBlobs` option. Otherwise return `supported=false` (WebAuthn §10.1.5: "true if, and only if, the created credential supports storing large blobs"). The repro calls `ParseRegistrationOutputs(..., null, ...)`, which has no parameter for the key, so it will need a signature change and a test edit.
- API impact: None if pipeline remains internal.
- Depends on / open decisions: Related to #4 only if supporting direct largeBlob; use actual credential key presence when advertised options and response disagree.

### #6 Attestation None forwards authenticator statement (YESDK-1614)
- Verdict / our severity: CONFIRMED-WITH-CORRECTION / MED; L3 does not require zeroing AAGUID for none attestation.
- Location at HEAD: `src/WebAuthn/src/Client/WebAuthnClient.Registration.cs:339-380`; default None at `src/WebAuthn/src/Client/Registration/RegistrationOptions.cs:64`.
- Spec: “If the authenticator generates an attestation statement that is not a self attestation, the client will replace it with a None attestation statement.” — WebAuthn L3 §5.4.7. “Set the value of credentialCreationData.attestationObjectResult.fmt to "none", and set the value of credentialCreationData.attestationObjectResult.attStmt to be an empty CBOR map.” — §5.1.3.
- Reference implementations: python-fido2 forwards raw authenticator attestation without checking preference; Windows forwards preference to the external platform API.
- Evidence: Builder copies authenticator `AttestationStatement` unchanged and never reads `options.Attestation`; returned object can remain packed when preference is None.
- Repro: `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs::YESDK1614_NoneAttestationRemovesAuthenticatorStatement` — failed as expected (actual fmt `packed`).
- Hardware: No hardware repro recorded; response transformation is locally testable.
- Remediation: For None, emit fmt `none` and an empty CBOR map for non-self statements; retain credential ID and AAGUID, and handle the L3 self-attestation exception.
- API impact: None; response values change to honor the preference.
- Depends on / open decisions: Other attestation preferences need separate policy review; direct/enterprise/indirect pass-through remains undecided.

### #7 Preferred resident key not requested (YESDK-1615)
- Verdict / our severity: CONFIRMED-WITH-CORRECTION / MED; builder omits `rk`, then backend coerces absence to false.
- Location at HEAD: `src/WebAuthn/src/Client/WebAuthnClient.Registration.cs:84-85,282-335`; `src/WebAuthn/src/Client/WebAuthnBackend.cs:124`; `src/Fido2/src/Cbor/FidoSessionRequestEncoding.cs:100-118`.
- Spec: “If the authenticator is capable of client-side credential storage modality ... Let requireResidentKey be true.” — WebAuthn L3 §5.1.3, preferred `residentKey` branch.
- Reference implementations: python-fido2 sets `rk=true` for Preferred when authenticator info advertises `rk`; Windows passes preference to the platform API.
- Evidence: Builder requests `rk=true` only for Required and ignores GetInfo `rk`; backend maps absence to false and encoder emits it. The unit repros show capable-device preferred registration does not request discoverability.
- Repro: `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs::YESDK1615_PreferredResidentKeyOnCapableAuthenticatorRequestsDiscoverable` and `::YESDK1615_BackendPreservesAbsentResidentKeyOption` — both failed as expected.
- Hardware: **confirmed** on 5.7.4 (rk-capable, user present). `YESDK1615_PreferredResidentKeyCanBeDiscoveredWithoutAllowList` (changed to UV=Preferred to isolate it from #2) registers with `residentKey=Preferred`, then runs getAssertion without an allowList. The result is `[]`: the credential is not discoverable.
- Remediation: Pass GetInfo capability into request building; send `rk=true` for Required or Preferred when supported, and preserve absence in backend.
- Review correction: for `Required` on an authenticator without discoverable-credential support, WebAuthn skips that authenticator. Fail the ceremony; never fall back to `rk=false`. The quote above joins separate clauses with an ellipsis, so read it as an excerpt of WebAuthn L3 §6.3.2 / §5.4.6.
- API impact: None; new discoverable credentials consume authenticator storage.
- Depends on / open decisions: Related to credential storage limits and credProps; omit `rk` for unknown capability (recommended).

### #8 topOrigin with crossOrigin false (YESDK-1616)
- Verdict / our severity: CONFIRMED / MED.
- Location at HEAD: `src/WebAuthn/src/Client/WebAuthnClient.Registration.cs:77-83`; `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs:79-86`; `src/WebAuthn/src/Client/WebAuthnClientData.cs:116-125`.
- Spec: “It is set only if the call was made from context that is not same-origin with its ancestors, i.e. if crossOrigin is true.” — WebAuthn L3 §5.8.1 CollectedClientData.topOrigin.
- Reference implementations: python-fido2 serializes crossOrigin but has no topOrigin field in its ClientData constructor; its payment top_origin is a distinct extension.
- Evidence: Non-null `TopOrigin` alone controls inclusion in clientData, including when `CrossOrigin` is false; JSON and its hash are returned/sent.
- Repro: `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs::YESDK1616_SameOriginRegistrationOmitsTopOriginFromClientData` and `::YESDK1616_SameOriginAssertionOmitsTopOriginFromClientData` — both failed as expected.
- Hardware: No hardware repro recorded; clientData behavior is locally testable.
- Remediation: Reject contradictory `CrossOrigin != true` plus non-null `TopOrigin`, or omit topOrigin consistently; recommendation is to reject explicit inconsistency.
- Review correction: the repro tests expect success with `topOrigin` omitted, but the remediation says reject. Pick one before fixing. We recommend rejecting at options validation (`ArgumentException`: topOrigin requires crossOrigin=true), then change both tests to expect the rejection.
- API impact: Behavioral change, no signature change.
- Depends on / open decisions: Decide rejection versus omission; related to #9 only in deadline placement around validation.

### #9 Ceremony timeout ignored (YESDK-1617)
- Verdict / our severity: CONFIRMED / MED.
- Location at HEAD: `src/WebAuthn/src/Client/Registration/RegistrationOptions.cs:66-69`; `src/WebAuthn/src/Client/Authentication/AuthenticationOptions.cs:59-66`; `src/WebAuthn/src/Client/WebAuthnClient.Registration.cs:49-126`; `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs:52-118`.
- Spec: “If pkOptions.timeout is present, check if its value lies within a reasonable range as defined by the client and if not, correct it to the closest value lying within that range. Set a timer lifetimeTimer to this adjusted value.” — WebAuthn L3 §5.1.3 and §5.1.4.
- Reference implementations: python-fido2 starts a timer for make/get and cancels it on completion; libfido2 device timeout is not the same ceremony policy.
- Evidence: No reads of either `options.Timeout` in the client; caller cancellation is forwarded, but no automatic ceremony deadline exists. Repro confirms a test guard, not the requested 50 ms timeout, cancels.
- Repro: `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs::YESDK1617_RegistrationTimeoutCancelsPendingAuthenticator` and `::YESDK1617_AuthenticationTimeoutCancelsPendingAuthenticator` — both failed as expected.
- Hardware: No hardware repro recorded; cancellation/deadline behavior is locally testable.
- Remediation: Start a linked bounded timeout at ceremony entry, pass its token through backend and prompts, and map deadline expiry to NotAllowed while preserving caller cancellation.
- API impact: Behavioral change, no signature change.
- Depends on / open decisions: Decide null timeout semantics and whether deferred `MatchedCredential.SelectAsync` is within the deadline; document null=no-timeout divergence if retained.

### #10 makeCredential accepts forbidden options.up=false (YESDK-1618)
- Verdict / our severity: CONFIRMED / MED; invalid low-level request, not a UP bypass.
- Location at HEAD: `src/Fido2/src/Credentials/MakeCredentialOptions.cs:51-57`; `src/Fido2/src/Cbor/FidoSessionRequestEncoding.cs:23-49,100-124`; `src/Fido2/src/FidoSession.cs:229-279`.
- Spec: “Platforms MAY send the "up" option key to CTAP2.1 authenticators, and its value MUST be true if present. The value false will cause a CTAP2_ERR_INVALID_OPTION response regardless of authenticator version.” — CTAP 2.3 §6.1.
- Reference implementations: python-fido2 raw API forwards caller options; high-level registration and libfido2 makeCredential encoder do not send `up`.
- Evidence: Encoder emits literal `"up": false` when `UserPresence=false`; WebAuthn registration does not set it. getAssertion `up=false` is intentionally used for preflight and is distinct.
- Repro: `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2RequestAuditReproTests.cs::YESDK1618_MakeCredentialDoesNotEncodeFalseUserPresence` — failed as expected; `::YESDK1618_GetAssertionStillEncodesFalseUserPresence` — passed control.
- Hardware: HW-RESULTS confirms `YESDK1618_MakeCredentialFalseUserPresenceReturnsInvalidOption` passed; device returned CTAP2_ERR_INVALID_OPTION (0x2C) before touch.
- Remediation: Reject explicit false at public session boundary (recommended), or omit it with clear documentation; do not change getAssertion encoding. Correct XML docs and existing unit expectation.
- Review correction: if we reject (decision 3), change `YESDK1618_MakeCredentialDoesNotEncodeFalseUserPresence` to expect rejection at the `FidoSession.MakeCredentialAsync` boundary, before any transmission. Also update the hardware test, which today expects the device's 0x2C. Keep the passing getAssertion `up=false` control test unchanged.
- API impact: Behavioral breaking for callers explicitly sending makeCredential `UserPresence=false`; no signature change.
- Depends on / open decisions: Decide reject versus omit; separately decide whether to omit explicit true for CTAP2.0 devices.

### #11 credBlob assertion retrieval uses wrong CBOR type (YESDK-1619)
- Verdict / our severity: CONFIRMED / MED.
- Location at HEAD: `src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs:43,75-79,307-311`; `src/Fido2/src/Cbor/FidoSessionRequestEncoding.cs:80-83`; `src/WebAuthn/src/Extensions/WebAuthnExtensionInputs.cs:38-47`; `src/WebAuthn/src/Extensions/ExtensionPipeline.cs:117-146,292-305`.
- Spec: “get() : A boolean value to indicate that this extension is requested by the Relying Party.”; “partial dictionary AuthenticationExtensionsClientInputs { boolean getCredBlob; };”; “authenticatorGetAssertion authenticator extension input ... \"credBlob\":true” — CTAP 2.3 §12.2.
- Reference implementations: python-fido2 and libfido2 send credBlob bytes at creation and boolean true for assertion retrieval.
- Evidence: `WithCredBlob` always writes a byte string, including empty `h''`, and request encoder forwards it under key 4. WebAuthn authentication inputs have no GetCredBlob property; existing hardware assertion conditionally checks output and can pass without retrieval.
- Repro: `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2ExtensionAuditReproTests.cs::YESDK1619_GetAssertionCredBlobUsesBooleanTrue` — failed as expected; `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnExtensionAuditReproTests.cs::YESDK1619_WebAuthnOffersGetCredBlobClientInput` — failed as expected.
- Hardware: `FidoCredBlobTests.CredBlob_StoreAndRetrieve_ReturnsStoredData`, tightened to assert unconditionally, **passes** on 5.7.4. The YubiKey accepts `"credBlob": h''` and returns the blob, which confirms the audit's "a YubiKey tolerates it". The request is still non-conformant and may fail on other authenticators.
- Remediation: Add `WithGetCredBlob()` emitting boolean true, retain `WithCredBlob(blob)` for registration, expose and wire WebAuthn `GetCredBlob`, and require the expected response in integration test.
- Review correction: `YESDK1619_GetAssertionCredBlobUsesBooleanTrue` calls `WithCredBlob(Empty)`. Once `WithGetCredBlob()` exists, change the repro to call it, and keep a separate creation-side byte-string control. The quoted input definitions are in CTAP 2.3 §12.2.1, not §12.2, and are a condensed excerpt.
- API impact: Additive.
- Depends on / open decisions: None; decide whether WebAuthn output should add/rename a `GetCredBlob` member rather than conflate create/get outputs.

### #12 Incorrect max fragment length (YESDK-1620)
- Verdict / our severity: CONFIRMED / MED (audit severity).
- Location at HEAD: `src/Fido2/src/LargeBlobs/LargeBlobStorage.cs:74-103,271-315,341-350`; `src/Fido2/src/FidoSession.cs:155-157`.
- Spec or contract: “A per-authenticator constant, maxFragmentLength, is here defined as the value of maxMsgSize (from the authenticatorGetInfo response) minus 64. ... If no maxMsgSize is given in the authenticatorGetInfo response) then it defaults to 1024, leaving maxFragmentLength to default to 960.” “If the value of get is greater than maxFragmentLength, return CTAP1_ERR_INVALID_LENGTH.” — CTAP 2.3 §6.10.2.
- Reference implementations: `python-fido2/fido2/ctap2/blob.py:100-110,122-129` derives maxMsgSize minus 64; `libfido2/src/largeblob.c:440-458` obtains the chunk length before reading.
- Evidence: Storage defaults reads and writes to 1024 bytes without retaining GetInfo maxMsgSize; the initial read exceeds the required 960-byte default when maxMsgSize is absent.
- Hardware: HW-RESULTS GetInfo on both tested YubiKeys reports maxMsgSize=1536, so their limit is 1472; SDK's 1024 does not fail on these devices. The defect can fail when maxMsgSize is absent or below 1088.
- Repro: `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs::YESDK1620_DefaultReadFragmentIs960Bytes` — FAILED as expected: expected 960, actual 1024.
- Remediation: derive read/write fragments from session GetInfo; default to 960 only when maxMsgSize is absent, and bound any explicit override by the device limit.
- Review correction: wording: when maxMsgSize is absent, maxMsgSize defaults to 1024, which makes maxFragmentLength default to 960 (§6.10.2).
- API impact: potentially additive GetInfo/session or storage-constructor plumbing; retain explicit overrides only within limits.
- Depends on / open decisions: #13 needs safe fragment sizing; recommend querying through existing GetInfoAsync and caching the calculated limit per storage instance.

### #13 Incompatible per-credential array entries (YESDK-1621)
- Verdict / our severity: CONFIRMED / HIGH interoperability and data availability (audit severity MED).
- Location at HEAD: `src/Fido2/src/LargeBlobs/LargeBlobData.cs:20-30,57-85,95-175,206-269`; `src/Fido2/src/LargeBlobs/LargeBlobStorage.cs:186-216`.
- Spec or contract: “The elements of the large-blob array MUST conform to the following large-blob map structure.” “ciphertext (0x01) Byte String Required ... authentication tag at the end”; “nonce (0x02) Byte String Required ... exactly 12 bytes long”; “origSize (0x03) Unsigned Integer Required ... length, in bytes, of the uncompressed data.” “Plaintext: the compressed opaque large-blob data. Associated data: The value 0x626c6f62 (\"blob\") || uint64LittleEndian(origSize).” — CTAP 2.3 §6.10.3. “Let plaintext equal origData after compression with DEFLATE.” — CTAP 2.3 §6.10.5. “Platforms MUST ensure that the large-blob array ... is a CBOR array where all entries conform to the large-blob map structure defined below.” — CTAP 2.3 §6.10.2.
- Reference implementations: `python-fido2/fido2/ctap2/blob.py:44-79,117-145,176-214` uses raw DEFLATE, spec AAD and map fields 1–3; `libfido2/src/largeblob.c:50-82,118-146,277-352,358-393` corroborates.
- Evidence: SDK stores private CBOR plaintext without DEFLATE or spec AAD, then stores each entry as a byte string instead of a map. Its parser rejects conforming maps, and spec decoders reject SDK entries; the array SHA-256 trailer is correctly constructed.
- Repro: `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs::YESDK1621_ReadsPythonFido2CompatibleArray` and `::YESDK1621_SdkWrittenArrayIsSpecDecodable` — both FAILED as expected on CBOR major-type mismatch.
- Hardware: **confirmed in both directions** (user present).
  - On 5.7.4, `YESDK1621_SdkWriteIsReadablePerSpec` fails inside `SetBlobAsync`: "next CBOR data item is of major type '5'". The existing array already held a spec-conformant map entry from another client (python-fido2 `read_blob_array()` shows 1 `dict` entry). So **the SDK cannot write large blobs on a device that holds spec-format data.**
  - On 5.8.0 test key, python-fido2 sees the 2 entries left by earlier SDK runs as raw `bytes`, not spec maps, so no spec client can decrypt them.
- Remediation: encode/decode spec map fields 1–3, raw DEFLATE, AAD and authenticated decompression/size validation; preserve valid unrelated entries and skip malformed maps.
- Review correction: spec details the rewrite must cover.
  - **Read side:** after a successful AEAD decrypt, CTAP §6.10.4 says "If decompression fails, return an error" and "If the length ... is not equal to origSize, return an error". python-fido2 instead skips the entry (`blob.py:186-189`); this is a documented deviation, not a reference. Check `origSize` against a cap before decrypting (per the §6.10.4 note the cap SHOULD be at least 1 MiB), and stop inflating at `origSize+1`.
  - **Write side:** §6.10.2 requires the written array to hold only conforming entries, canonically encoded, and not larger than `maxSerializedLargeBlobArray` (4096 on the tested keys). Drop malformed entries on write. Keep untouched conforming entries as raw bytes, because §6.10.3 lets maps "include unknown elements".
- API impact: potentially breaking if public `EncryptedData` changes shape; select migration policy for pre-release.
- Depends on / open decisions: #12 safe fragments, #14 corrupt-array fallback, #16 plaintext cleanup. Recommend no compatibility shim absent deployed external data; define a decompression maximum (CTAP §6.10.4 recommends at least 1 MiB).

### #14 Bad array checksum response (YESDK-1622)
- Verdict / our severity: CONFIRMED / MED (audit severity).
- Location at HEAD: `src/Fido2/src/LargeBlobs/LargeBlobData.cs:206-224`; `src/Fido2/src/LargeBlobs/LargeBlobStorage.cs:113-123`.
- Spec or contract: “Once complete, the platform MUST confirm that the embedded SHA-256 hash is correct ... If not, the configuration is corrupt and the platform MUST discard it and act as if the initial serialized large-blob array was received.” — CTAP 2.3 §6.10.2. Initial bytes: “h’8076be8b528d0075f7aae98d6fa57a6d3c'” — CTAP 2.3 §6.10.
- Reference implementations: `python-fido2/fido2/ctap2/blob.py:117-134` returns an empty array on mismatch; `libfido2/src/largeblob.c:413-463` substitutes a zero-length CBOR array.
- Evidence: The SDK detects checksum corruption but throws through the storage read path instead of recovering to the initial array; this finding is distinct from malformed CBOR with a valid checksum.
- Repro: `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs::YESDK1622_CorruptChecksumActsAsInitialArray` — FAILED as expected with `ArgumentException: Large blob array hash verification failed`.
- Hardware: No hardware repro required; unit repro exercises checksum recovery.
- Remediation: on checksum mismatch during storage read, discard the corrupt contents and return the initial empty array; keep transport errors and malformed-CBOR policy distinct.
- Review correction: `SetBlobAsync` and `DeleteBlobAsync` both read through `ReadLargeBlobArrayAsync`. Put the fallback there, so a write after a corrupt read replaces the array with the initial array plus the new entry, which matches python-fido2 `put_blob`. Add a repro for the write path. A non-empty read shorter than 17 bytes currently throws "Data too short" (`LargeBlobData.cs:208-211`). That also fails the hash check and must take the same fallback.
- API impact: none; behavioral recovery.
- Depends on / open decisions: #13 array representation. Implement fallback at storage-read level and retain strict `Deserialize` behavior for direct callers.

### #16 Temporary plaintext retention (YESDK-1624)
- Verdict / our severity: CONFIRMED-WITH-CORRECTION / LOW-MED, conditional local heap-dump exposure, not remote disclosure (audit severity MED).
- Location at HEAD: `src/Fido2/src/LargeBlobs/LargeBlobData.cs:74-85,102-133,136-175`; `src/Fido2/src/LargeBlobs/LargeBlobStorage.cs:199-216,240-265`.
- Spec or contract: “✅ ALWAYS zero sensitive data: `CryptographicOperations.ZeroMemory()`”; “✅ Classify byte data by semantic meaning, not by direction. ... YubiKey-returned authentication material, token material, decrypted plaintext, or secret-derived output does.” — repository `CLAUDE.md:58-60`. This is repository memory hygiene, not a CTAP wire requirement.
- Reference implementations: `libfido2/src/largeblob.c:118-153` frees plaintext through blob cleanup; `python-fido2/fido2/ctap2/blob.py:72-79,176-211` uses immutable bytes without comparable zeroing, so is not a .NET zeroing model.
- Evidence: Decrypted plaintext is not zeroed after parsing; encryption plaintext and discarded match results also remain. Parser internals may make additional copies; `plaintext.ToArray()` and `reader.ReadByteString()` definitely allocate separately.
- Repro: `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs::YESDK1624_DecryptedTemporaryPlaintextIsCleared` — FAILED as expected: 6/6 bytes of temporary plaintext remain nonzero; it does not observe GC-discarded copies.
- Hardware: No hardware repro required; this is managed-memory ownership behavior.
- Remediation: zero owned temporary decrypt/encrypt buffers in `finally` on success and failure; reduce copies, clear owned parser copies and discarded results, and preserve only explicitly returned caller-owned data.
- Review correction: the probe `YESDK1624_DecryptedTemporaryPlaintextIsCleared` hands a test-owned buffer to the private `ParseDecryptedBlob(ReadOnlySpan<byte>)`. The buffer that actually needs zeroing belongs to `TryDecrypt` (`LargeBlobData.cs:77`), so a correct fix still fails the probe, and #13 removes the probed method anyway. Treat the evidence as **code inspection**. Re-target the probe at the decrypt/inflate buffer owned by the #13 rewrite.
- API impact: none; document caller responsibility to zero returned plaintext.
- Depends on / open decisions: #13 format rewrite should incorporate cleanup; retain explicitly returned caller-owned result and clarify its ownership contract.

### #17 Private-key intermediate copies (YESDK-1625)
- Verdict / our severity: PARTIAL — most SDK copies were fixed at commit 65e36386; LOW for remaining managed base class library copy, requiring local-memory disclosure (audit severity MED).
- Location at HEAD: `src/Core/src/Cryptography/AsnPrivateKeyDecoder.cs:347-374`; `RSAParametersExtensions.cs:60-100`; `RSAPrivateKey.cs:114-131`; `ECPrivateKey.cs:120-132,174-183,223-247`.
- Spec or contract: “A new disposable private-key object that owns its decoded key material.” — `AsnPrivateKeyDecoder.cs:37-39`; “ALWAYS zero sensitive data: `CryptographicOperations.ZeroMemory()`” — root `CLAUDE.md:58-65`. No protocol specification governs host heap hygiene.
- Reference implementations: .NET runtime v10.0.0 `EccSecurityTransforms.macOS.cs::ExtractPublicKeyFromPrivateKey` overwrites the `ECParameters` holding original D without clearing it; `EccKeyFormatHelper.cs::FromECPrivateKey` allocates D via `ToArray()`. This residual belongs to the runtime.
- Evidence: SDK RSA/PKCS#8 and other described private arrays are cleared; the remaining macOS path is D-only `ECDsa.Create(parameters)` at `ECPrivateKey.cs:232`. No PIV or SecurityDomain production private-key import path invokes `ECPrivateKey.CreateFromValue`.
- Repro: `PrivateKeyDecodingZeroingTests` — 12 passed; no deterministic test can observe the inaccessible runtime-owned array, so the residual is based on runtime source inspection.
- Hardware: No hardware repro; issue is runtime-managed memory behavior.
- Remediation: replace D-only `ECDsa.Create(parameters)` with a vetted derivation path whose managed intermediates are known to clear; optionally report the macOS runtime copy upstream. No SDK change needed for already-fixed paths.
- API impact: none if factory internals change.
- Depends on / open decisions: #27, ensuring supplied public Q is not trusted as a workaround. Recommend investigate upstream before choosing a native derivation or documenting the runtime-local residual.

### #19 Write-capable documentation workflows execute unverified remote installers (YESDK-1627)
- Verdict / our severity: CONFIRMED-WITH-CORRECTION / MED (audit severity); native installer checks a digest from a remotely fetched manifest, but it is not independently pinned.
- Location at HEAD: `.github/workflows/docs-update.yml:3-19,25-35,78-92,138-144,146-156,188-202,242-267,280-316`.
- Spec or contract: “It's good security practice to set the default permission for the `GITHUB_TOKEN` to read access only for repository contents. The permissions can then be increased, as required, for individual jobs within the workflow file.” “Pinning an action to a full-length commit SHA is currently the only way to use an action as an immutable release.” — GitHub Secure use reference, https://docs.github.com/en/actions/reference/security/secure-use.
- Reference implementations: Not applicable (workflow, not device protocol); pinned `actions/checkout` documents that its auth token is persisted in local git config by default, and `persist-credentials: false` opts out.
- Evidence: the `migration` and `architecture` jobs inherit the workflow-level request for `contents: write` and `pull-requests: write` (a `workflow_call` caller can narrow this; `push` and `workflow_dispatch` runs get it in full), although only `create-pr` needs write access; checkout credentials persist. Installer verifies its executable against a manifest from the same origin, then runs it; artifact jobs are split, permissions are not.
- Repro: Static workflow/source trace — no unit or hardware repro; no workflow execution.
- Hardware: No hardware repro; CI configuration finding.
- Remediation: fetch a fixed native release and verify against a reviewed, committed SHA-256 before execution; disable CLI updates. Set generators to read-only, elevate writes only in `create-pr`, and use `persist-credentials: false` (or a read-only fetch token); retain artifact split and review artifacts as untrusted.
- API impact: none; CI only.
- Depends on / open decisions: adjacent lockless Mermaid installs and monthly synthesis workflows merit separate follow-up. Decide who approves pinned digest updates and whether to apply the permission split to monthly workflows; recommend human approval of digest changes and PR merges.

### #22 PIV cross-connection PIN status (YESDK-1630)
- Verdict / our severity: CONFIRMED / MED. It needs a local process (or code in the same process) that holds a shared handle while the victim is verified, plus a PIN=ONCE key; with TOUCH=CACHED it only works during the touch cache (audit severity MED).
- Location at HEAD: `src/Core/src/Transports/SmartCard/UsbSmartCardConnection.cs:125-138,198-224,301-303`; `src/Core/src/Native/Desktop/SCard/SCardCardHandle.cs:35-38`; `src/Core/src/CoreCompatSwitches.cs:21-27`; `src/Piv/src/PivSession.cs:386-390`.
- Spec or contract: “A security status indicator is said to be an application security status indicator if it is set to FALSE when the currently selected application changes from one application to another.” “The security status indicators associated with the PIV Card Application PIN ... are application security status indicators” — NIST SP 800-73-5 Part 2 §2.4.2. “Once the correct PIN has been provided, multiple private key operations may be performed without additional cardholder consent.” — YubiKey technical manual, PIV slot 9A.
- Reference implementations: Legacy .NET SDK v1 defaults to `RESET_CARD` on disconnect; `yubikey-manager/ykman/pcsc/__init__.py:130-149` tries exclusive then shared. Their behavior does not establish a safe v2 default.
- Evidence: v2 opens shared handles and disconnects with `LEAVE_CARD`. PIN state belongs to the card and applet, not to the SDK handle, and it survives a fresh SELECT of the PIV AID. Hardware results on macOS:
  - An attacker holding a shared handle can sign without the PIN **while the victim's session is still open**. A polling attacker succeeded about 1 s after the victim verified, 14 s before the victim disposed.
  - It also works after the victim disposes and exits.
  - In-process, across fully disposed SDK sessions and PC/SC contexts, it also reproduces.
  - Sequential separate processes, down to about 0.6 s apart with a prebuilt binary, do **not** reproduce: the next process gets 6982.
  - Why process exit clears the state while in-process dispose does not is **unknown**. It was observed on macOS only. Windows WinSCard and Linux pcsc-lite were not tested, so do not rely on "sequential is safe".
- Repro: `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/AuditV2/PivAuditHardwareReproTests.cs::YESDK1630_PinOnceStateSurvivesConnectionDisposal` — inverted to assert correct behavior; FAILS today: “Unverified handle produced a signature without PIN (verifies against slot key: True)”.
- Hardware: `docs/audit-v2/evidence/scripts/Yesdk1630CrossProcess/app.cs` (modes `setup`, `hold`, `poll`, `verify`, `verify-wait`, `sign`). The cross-process attack is confirmed on 5.7.4 and 5.8.0-alpha.2, and the poll-during-session variant on 5.7.4. With `OpenSmartCardHandlesExclusively=true`, a **pre-connected** attacker makes the victim's connect fail with a sharing violation, so the victim never verifies. That is the only ordering tested. It shows exclusive mode blocks a pre-connected attacker. It does not show isolation after the victim releases the card with `LEAVE_CARD`.
- Remediation: resetting on dispose **alone is insufficient**, because the attacker can act during the victim's session.
  - An effective isolation mode has to do both of the following:
    - Keep other handles out between VERIFY and the last private-key operation, either with an exclusive handle or by holding `SCardBeginTransaction` for that whole span.
    - Release with `SCARD_RESET_CARD`, the legacy v1 default, so the PIN state does not outlive the session.
  - Offer this as an opt-in session policy that warns it disrupts other smart-card users, and surface acquisition failure explicitly.
  - Separately, document that the PIN=ONCE state is card-wide.
  - Test on macOS (CryptoTokenKit-based PC/SC), Windows and Linux. Whether macOS honours `RESET_CARD`, and whether another handle's transaction can delay it, is not established.
  - A victim process that crashes never issues the disposition.
- API impact: additive per-connection/session isolation or disposition policy; changing defaults would be breaking.
- Depends on / open decisions: decision 1. Also decide whether the policy is exclusive plus reset, or a transaction plus reset (a transaction lets other readers of the card coexist between operations).

### #23 Short SCP response crashes the host process (YESDK-1631)
- Verdict / our severity: CONFIRMED / MED. An on-path responder can terminate the host before MAC verification.
- Location at HEAD: `src/Core/src/Protocols/SmartCard/Scp/ScpState.cs:192,198-202`; caller `ScpProcessor.cs:121-123`.
- Spec or contract: “The R-MAC is made of the first 8 bytes (in S8 mode) or full 16 bytes (in S16 mode) of the CMAC computed on the message made of the MAC chaining value, the response data field (if present) and the status bytes.” — GlobalPlatform Card Spec v2.3 Amd D (SCP03) v1.1.2 §6.2.5. A protected response must therefore contain at least 8 R-MAC bytes.
- Reference implementations: ykman rejects short R-MAC via failed comparison; legacy .NET v1 explicitly throws `SecureChannelException` for responses shorter than 8 bytes.
- Evidence: data lengths 1–5 cause negative `stackalloc` and process exit 134; lengths 6–7 throw a range exception that is mislabeled `NotSupportedException`.
- Repro test: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreScpAuditReproTests.cs::YESDK1631_ShortResponseDataBelowRmac_DoesNotTerminateProcess`, `YESDK1631_SixOrSevenByteResponse_ThrowsBadResponse(6|7)` — failed as expected (SIGABRT 134; misleading exception type).
- Hardware: not needed.
- Remediation: reject `data.Length < MacLength` with `BadResponseException` before arithmetic; narrow the crypto-provider catch.
- Review correction: the parent test only asserts the child's exit code, so it could pass without the child running at all (filter mismatch or zero tests). Also assert that the child's output reports exactly one test passed. The exit-134 expectation is specific to Unix.
- API impact: none; internal exception behavior becomes a protocol error rather than process termination or `NotSupportedException`.
- Open decisions: handle error-status responses with data consistently; separately assess the empty-data `9000` R-MAC gap listed under new findings.

### #24 Ed25519 signs altered message (YESDK-1632)
- Verdict / our severity: CONFIRMED / HIGH for silently incorrect signatures and input collisions, subject to supported message length.
- Location at HEAD: `src/Piv/src/PivSession.cs:486-528,821`; `src/Piv/src/Cryptography/PivCryptographicOperations.cs:77-110,394-447`; `IPivSession.cs:247-266,275-300`.
- Spec or contract: “The inputs to the signing procedure is the private key, a 32-octet string, and a message M of arbitrary size.” — RFC 8032 §5.1.6. SDK contract: “EdDSA: Sign message directly” — `IPivSession.cs:255`.
- Reference implementations: yubikey-manager and yubikit-android pass Ed25519 messages unchanged.
- Evidence: `PrepareDataForCrypto` truncates messages over 32 bytes and right-pads shorter ones before TLV `0x81`; both 10- and 64-byte APDU repros failed.
- Repro test: `src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/AuditV2/PivAuditReproTests.cs::YESDK1632_Ed25519ChallengeContainsEntireMessage(10|64)` — failed as expected; actual APDU contains altered data.
- Hardware: CONFIRMED per authoritative `HW-RESULTS.md`: `YESDK1632_Ed25519SignsUnmodifiedShortAndLongMessages` produced a 10-byte-message signature that failed OpenSSL PureEd25519 verification against the slot public key.
- Remediation: pass the original message unchanged and explicitly reject lengths beyond a documented device/transport maximum; continue zeroing owned copies.
- API impact: no signature change; accepted messages will produce correct signatures instead of signatures over altered inputs.
- Open decisions: establish exact firmware message-size limits; RFC’s arbitrary-size message statement does not establish an APDU limit.

### #25 Numeric-boundary issues (YESDK-1633)

#### 25a `--length` unchecked before `stackalloc`
- Verdict / our severity: CONFIRMED / LOW; local CLI self-denial-of-service in excluded internal tooling.
- Location at HEAD: `src/Cli.Commands/src/Otp/OtpCommands.cs:203,480-481`; option documentation at `:92-94` says "Length of generated password (1-38)."
- Spec or contract: CLI option contract “(1-38)”; 38 is the YubiKey static-password maximum.
- Reference implementations: ykman rejects values above the static-password limit.
- Evidence: negative or very large lengths can terminate the process through stack exhaustion; zero gives an empty password; values above 38 are accepted.
- Repro test: `src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/AuditV2/CliOtpAuditReproTests.cs::YESDK1633_GenerateStaticPasswordOutOfDocumentedRange_Throws(0|39)`, `YESDK1633_GenerateStaticPasswordNegativeLength_DoesNotTerminateProcess` — failed as expected (no exception; child exited 134).
- Hardware: none.
- Remediation: enforce length 1–38 in `GenerateStaticPassword`; optionally validate at the CLI settings boundary.
- API impact: none; internal tooling.
- Open decisions: whether to add friendly Spectre validation in addition to method guards.

#### 25b DeviceInfoReader page counter wraps
- Verdict / our severity: CONFIRMED / LOW–MED; malformed or faulty devices can hang discovery until cancellation.
- Location at HEAD: `src/Core/src/Devices/DeviceInfoReader.cs:61,88,101`.
- Spec or contract: page index is a single P1 byte, so at most 256 distinct page values; Core loop guidance requires loops to exit or back off on failure.
- Reference implementations: Python’s integer does not wrap (SmartCard eventually fails encoding page 256); Swift traps on overflow; v1 .NET also wraps. None has an explicit page bound.
- Evidence: a fake returning “more pages” indefinitely causes requests for pages 0–255 and then page 0 again.
- Repro test: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs::YESDK1633_DeviceInfoAlwaysMoreData_StopsWithBadResponseWithinPageSpace` — failed as expected after the fake’s 600-request guard.
- Hardware: none.
- Remediation: throw `BadResponseException` before incrementing past `byte.MaxValue` while more pages remain.
- API impact: none.
- Open decisions: use the protocol maximum of 256 pages or a smaller cap; report recommends 256.

#### 25c `RandomNumberGeneratorExt.GetInt32` returns a constant for wide ranges
- Verdict / our severity: CONFIRMED / LOW; public API, but internal use is limited to byte-sized ranges.
- Location at HEAD: `src/Core/src/Cryptography/RandomNumberGeneratorExt.cs:61-67`.
- Spec or contract: XML contract promises a random signed 32-bit integer in `[fromInclusive, toExclusive)`; source says adopted from `RandomNumberGenerator.GetInt32`.
- Reference implementations: .NET’s `RandomNumberGenerator.GetInt32` uses a `do/while`, unlike this port.
- Evidence: for ranges at least `int.MaxValue`, the `while (result > range)` body never runs; the result is constant and consumes no random bytes.
- Repro test: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs::YESDK1633_GetInt32WideRange_ConsumesRandomness` — failed as expected for all three ranges.
- Hardware: none.
- Remediation: preferably remove the wrapper and use BCL APIs; alternatively fix the loop and malformed exception message.
- API impact: removal affects only unshipped API; loop fix has none.
- Open decisions: remove or fix; report recommends removal.

#### 25d HKDF counter overflow at maximum valid output
- Verdict / our severity: CONFIRMED-WITH-CORRECTION / INFO–LOW. The claimed missing RFC maximum check is already present; valid output lengths 8129–8160 still fail.
- Location at HEAD: `src/Core/src/Cryptography/HkdfUtilities.cs:45-48,69,85`.
- Spec or contract: “L length of output keying material in octets (<= 255*HashLen) [...] N = ceil(L/HashLen) [...] (where the constant concatenated to the end of each T(n) is a single octet.)” — RFC 5869 §2.3.
- Reference implementations: BCL `HKDF.DeriveKey` provides a directly comparable implementation.
- Evidence: byte counter wraps after 255; at valid maximum length the block offset becomes -32 and `AsSpan(-32)` throws. Current callers request only about 32 bytes.
- Repro test: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs::YESDK1633_HkdfMaximumRfcLength_MatchesBclHkdf(8129|8160)` — failed as expected; above-maximum guard passed.
- Hardware: none.
- Remediation: replace with BCL HKDF or use an integer loop counter.
- API impact: none; internal.
- Open decisions: replacement versus minimal loop fix; report recommends BCL HKDF.

#### 25e (+LOW #20) BER-TLV long-form length decoding
- Verdict / our severity: CONFIRMED-WITH-CORRECTION / LOW. The audit’s `84 FF FF FF FF` example is rejected; 5+ byte overflow is accepted, and #20 truncated lengths throw the wrong exception type.
- Location at HEAD: `src/Core/src/Utilities/Tlv.cs:232-244`.
- Spec or contract: “b) bits 7 to 1 shall encode the number of subsequent octets in the length octets [...] c) the value 11111111₂ shall not be used. [...] shall be the encoding of an unsigned binary integer equal to the number of octets in the contents octets” — ITU-T X.690 §8.1.3.5.
- Reference implementations: ykman uses arbitrary-precision integers and maps truncation to `ValueError`; Android has a similar integer-overflow pattern.
- Evidence: `85 01 00 00 00 01` overflows the accumulator to 1 and is silently accepted. `5A 81` and `5A 82 01` cause `IndexOutOfRangeException` rather than the parser’s `ArgumentException` family.
- Repro test: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs::YESDK1633_TlvFiveByteLengthOverflow_Rejected`, `YESDK1628_TlvTruncatedLongFormLength_ThrowsParseError` — failed as expected; guard for negative decoded length passed.
- Hardware: none.
- Remediation: validate length-octet count and remaining buffer before reading; accumulate unsigned/wide and reject lengths beyond `int.MaxValue` or available data.
- API impact: none; malformed-input exception becomes `ArgumentException` rather than an unhandled index error or silent acceptance.
- Open decisions: permit up to 3 or 4 length octets; report recommends 4.

#### 25f `TlvHelper.EncodeDictionary` underallocates
- Verdict / our severity: CONFIRMED / LOW; public API, internal callers currently use small values.
- Location at HEAD: `src/Core/src/Utilities/TlvHelper.cs:238,250`.
- Spec or contract: encoder contract is to encode all dictionary TLVs; no separate normative quote recorded.
- Reference implementations: `TlvHelper.EncodeList` sizes from each TLV’s `TotalLength`.
- Evidence: estimate assumes one-byte tag and length; array-pool bucket boundaries expose underallocation for a 0x7F49 tag with 126 bytes and a 0x53 tag with 254 bytes.
- Repro test: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs::YESDK1633_EncodeDictionaryLongTagOrLength_EncodesAllBytes` — failed as expected for both vectors (“Destination is too short”).
- Hardware: none.
- Remediation: build TLVs and sum `TotalLength`, or use a safe worst-case estimate.
- API impact: none.
- Open decisions: exact sizing versus conservative capacity; exact sizing matches `EncodeList`.

#### 25g Nonpositive WebAuthn batch size
- Verdict / our severity: CONFIRMED-WITH-CORRECTION / LOW for conforming hardware, MED for malicious or faulty GetInfo data.
- Location at HEAD: `src/Fido2/src/AuthenticatorInfo.cs:365-367`; `src/WebAuthn/src/Client/WebAuthnClient.Registration.cs:248-255`; `src/WebAuthn/src/Internal/ExcludeListPreflight.cs:94-110,125-143`.
- Spec or contract: “maxCredentialCountInList (0x07) Unsigned Integer Optional ... Maximum number of credentials supported in credentialID list at a time by the authenticator. MUST be greater than zero if present.” — CTAP 2.3 §6.4.
- Reference implementations: Python defaults zero to 1; Android uses null-coalescing only, so zero can also produce an empty chunk.
- Evidence: zero or negative decoded values yield nonpositive chunk sizes; zero can repeat without progress. Preflight runs only for a nonempty exclude list and available token.
- Repro test: `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs::YESDK1634_NonpositiveGetInfoListLimitStillProbesAllCredentials(0|-1)` — failed as expected with an empty probe.
- Hardware: none.
- Remediation: reject malformed nonpositive GetInfo values at decode, and retain a progress guard in preflight.
- API impact: malformed device responses fail earlier; no public API signature change.
- Open decisions: reject at decoder (recommended) or clamp in preflight.

#### 25h OpenPGP KDF count truncation
- Verdict / our severity: CONFIRMED / MED; malicious or invalid card-supplied count; no PIN entropy is added to the derived value for short counts.
- Location at HEAD: `src/OpenPgp/src/Kdf.cs:214-257,319-329`.
- Spec or contract: “The input is truncated to the octet count, except if the octet count is less than the initial size of the salt plus passphrase. That is, at least one copy of the full salt plus passphrase will be provided as input to each hash context regardless of the octet count.” — RFC 9580 §3.7.1.3.
- Reference implementations: yubikey-manager reads the count unsigned, avoiding signed wrap, but shares the short-count behavior.
- Evidence: unsigned `0x80000000` becomes negative `int`; counts 8 and 9 hash only the salt or salt plus one PIN byte.
- Repro test: `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/AuditV2/OpenPgpAuditReproTests.cs::YESDK1633_UnsignedKdfCountDoesNotWrapNegative`, `YESDK1633_ShortKdfCountStillHashesCompletePin(8|9)` — failed as expected.
- Hardware: none.
- Remediation: decode with checked unsigned bounds and process at least one complete salt-plus-passphrase block.
- API impact: potentially rejects extreme card values; changes derived output for short counts.
- Open decisions: maximum resource bound and compatibility/migration handling for existing cards provisioned with short counts.

#### 25i Invalid OATH periods abort batch calculation (YESDK-1636)
- Verdict / our severity: CONFIRMED / MED; same zero-period root cause is independently reported in #28.
- Location at HEAD: `src/Oath/src/Credential.cs:135-153`; `src/Oath/src/OathSession.cs:520-580`.
- Spec or contract: “More specifically, T = (Current Unix time - T0) / X, where the default floor function is used in the computation.” — RFC 6238 §4.2. Google Key URI Format says “The `period` parameter defines a period that a TOTP code will be valid for, in seconds. The default value is 30.”
- Reference implementations: yubikey-manager also parses zero and raises `ZeroDivisionError` during calculation.
- Evidence: a `0/demo` period in a truncated batch causes `DivideByZeroException`; a later normal credential is not returned.
- Repro test: `src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/AuditV2/OathAuditReproTests.cs::YESDK1636_MalformedPeriodDoesNotAbortNormalBatchEntry` — failed as expected.
- Hardware: confirmed separately under #28.
- Remediation: validate positive periods on import/creation and at untrusted response boundaries; skip/report malformed entries without aborting valid batch results.
- API impact: none if malformed-response conventions are retained.
- Open decisions: omit malformed entries with diagnostics (recommended) or expose them with no code.

### #26 API footguns (YESDK-1634)
- Audit row: #26 was reported as one MED row; the following are its distinct sub-items and do not imply separate original audit rows.

#### 26e PIV retry reset changes PIN and PUK
- Verdict / our severity: CONFIRMED / HIGH where callers assume the old PIN remains; changing retry limits resets both secrets.
- Location at HEAD: `src/Piv/src/IPivSession.cs:158-164`; `PivSession.cs:426-430`; `Metadata/PivMetadataProtocol.cs:433-472`.
- Spec or contract: “Both PIN and PUK will be reset to default values when this is executed.” — Yubico PIV extensions, `SET PIN RETRIES`, command section.
- Reference implementations: yubikey-manager’s public docstring warns that both PIN and PUK reset.
- Evidence: public method contract says only “Set PIN and PUK retry limits”; implementation sends the command without exposing the reset warning, though internal remarks know the side effect.
- Repro test: no fake test; reset of device-held credentials is not meaningfully simulated by a recorder.
- Hardware: not run; controlled metadata/default-PIN check required to prove the reset.
- Remediation: document prominently on interface and facade; consider an explicitly acknowledged API or carefully specified follow-up credential-change flow.
- API impact: documentation-only if warning; acknowledgment/rename would be breaking.
- Open decisions: recommended public warning and explicit credential-rotation guidance; do not imply multiple commands are atomic.

#### 26f `PrepareDataForCrypto` alters cryptographic inputs
- Verdict / our severity: CONFIRMED / MED for EC padding and oversized RSA truncation. The Ed25519 part is tracked and rated under #24.
- Location at HEAD: `src/Piv/src/Cryptography/PivCryptographicOperations.cs:394-448`.
- Spec or contract: “2. Derive the integer e from H as follows: a. If len(n) ≥ hashlen, set E = H. Otherwise, set E equal to the leftmost ⌈log2(n)⌉ bits of H.” — FIPS 186-5 §6.4.1. SDK: “EdDSA: Sign message directly” — `IPivSession.cs:255`.
- Reference implementations: Android left-pads short EC/RSA inputs, rejects oversized RSA, and passes Ed25519 through; legacy .NET v1 prepends zero for short EC digests.
- Evidence: short EC input is copied at offset zero (right-padded); oversized RSA drops leading bytes; Ed25519 is truncated/padded to 32 bytes. FIPS leftmost truncation for long byte-aligned EC digests is not the defect.
- Repro test: `src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/AuditV2/PivAuditReproTests.cs::YESDK1634_ShortEcDigestIsLeftPaddedNotRightPadded`, `YESDK1634_OversizedRsaBlockIsRejectedRatherThanSilentlyTruncated` — failed as expected. Ed25519 hardware evidence is #24.
- Hardware: #24 confirms altered Ed25519 signature; no separate EC/RSA run reported.
- Remediation: left-pad a short EC digest (this preserves its integer value, matching FIPS 186-5 integer semantics; FIPS does not endorse arbitrarily short hashes), or reject non-canonical lengths; reject oversized RSA; handle Ed25519 unchanged as specified in #24.
- API impact: previously accepted invalid inputs will be rejected or correctly encoded; no signature change.
- Open decisions: accept short EC digests or require exact lengths; report recommends explicit policy and rejecting oversized RSA.

#### 26a Credential-specific PRF inputs ignored
- Verdict / our severity: CONFIRMED / MED.
- Location at HEAD: `src/Fido2/src/Extensions/Prf/PrfInput.cs:53-60`; `src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs:44-68`; `src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs:385-409`.
- Spec or contract: “If evalByCredential is present and contains an entry whose key is the base64url encoding of the credential ID that will be returned, let ev be the value of that entry. If ev is null and eval is present, then let ev be the value of eval.” — WebAuthn L3 §10.1.4.
- Reference implementations: python-fido2 validates credential IDs, selects matching per-credential input, then falls back to global `eval`.
- Evidence: adapter never selects by credential; a credential-specific override can encode identically to global input, and map-only input creates an incomplete CBOR map. The underlying fictional `prf` wire encoding is also covered by #1.
- Repro test: `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2ExtensionAuditReproTests.cs::YESDK1634_CredentialSpecificPrfOverridesGlobalInput`, `YESDK1634_OnlyCredentialSpecificPrfEncodesWithoutFailure`; WebAuthn sibling `WebAuthnExtensionAuditReproTests.cs::YESDK1634_PrFOnlyCredentialMapIsSelectedForMatchingAllowCredential` — failed as expected.
- Hardware: no direct test; PRF round trip pending separately.
- Remediation: select credential-specific inputs after credential selection, validate IDs against allowCredentials, and encrypt selected salts using the corrected hmac-secret path.
- API impact: none if selection remains in WebAuthn layer; direct low-level PRF API redesign is tracked under #1.
- Open decisions: preselect/probe credentials or explicitly reject unsupported multi-credential requests; do not silently choose the first.

#### 26b PRF enabled ignores returned value
- Verdict / our severity: CONFIRMED-WITH-CORRECTION / MED; adapter checks fictional `prf`, and its presence-only handling is wrong even for that representation.
- Location at HEAD: `src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs:74-84`.
- Spec or contract: “Set enabled to the value of hmac-secret in the authenticator extensions output. If not present, set enabled to false.” — WebAuthn L3 §10.1.4.
- Reference implementations: python-fido2 reads the `hmac-secret` boolean, defaulting to false.
- Evidence: `prf:false` returns `Enabled:true`; valid `hmac-secret:false` is ignored.
- Repro test: `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnExtensionAuditReproTests.cs::YESDK1634_PrFRegistrationFalseOutputIsNotEnabled` — failed as expected.
- Hardware: none.
- Remediation: parse CTAP `hmac-secret` boolean strictly and report false when absent; handle optional decrypted `hmac-secret-mc` result under #1.
- API impact: none.
- Open decisions: malformed non-boolean output should be rejected or reported disabled, never enabled.

#### 26c Missing previewSign attestation substitutes authentication key
- Verdict / our severity: CONFIRMED / HIGH; an application may persist the wrong public key/handle as a separate signing key.
- Location at HEAD: `src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs:175-241`; `GeneratedSigningKey.cs:20-52`.
- Spec or contract: “This authenticator registration extension and authentication extension allows a Relying Party to sign arbitrary data using an asymmetric key pair associated with a credential but different from the credential key pair.” — Yubico draft sign extension v4 §10.2.1. Required output: “required ArrayBuffer attestationObject;” — same section.
- Reference implementations: python-fido2 requires and parses the inner attestation; legacy .NET returns null when unsigned output is absent and never substitutes the authentication credential.
- Evidence: absent unsigned output falls back to the outer authentication credential’s ID/public key with null attestation.
- Repro test: `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnExtensionAuditReproTests.cs::YESDK1634_PreviewSignMissingInnerAttestationDoesNotSubstituteCredentialKey` — failed as expected; no exception was thrown.
- Hardware: none.
- Remediation: remove fallback and reject incomplete requested output or omit result; never substitute keys.
- API impact: none if rejected; making `AttestationObject` nonnullable could be a breaking cleanup.
- Open decisions: throw `InvalidState` (recommended) or omit incomplete extension result.

#### 26d FIDO previewSign chooses first credential parameters
- Verdict / our severity: CONFIRMED / MED; direct Fido2 API only. WebAuthn rejects multiple entries and mismatched single credentials.
- Location at HEAD: `src/Fido2/src/Extensions/PreviewSign/PreviewSignAuthenticationInput.cs:17-49`; `ExtensionBuilder.cs:420-430`; `FidoSessionRequestEncoding.cs:75-83`.
- Spec or contract: “If the authenticator contains any credentials whose credential IDs appear as keys, the client selects one of them and sends the corresponding value to the authenticator as inputs to the signing algorithm.” — Yubico draft sign extension v4 §10.2.1. SDK: “Maps credential IDs to their corresponding signing parameters.” — `PreviewSignAuthenticationInput.cs:20-24`.
- Reference implementations: python-fido2 validates IDs against allow list and indexes the selected credential’s entry.
- Evidence: low-level builder serializes `.First().Value` without matching the allow list; map insertion order silently changes the chosen signing parameters.
- Repro test: `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2ExtensionAuditReproTests.cs::YESDK1634_MultiplePreviewSignCredentialsCannotSilentlySelectFirst` — failed as expected; no rejection.
- Hardware: none.
- Remediation: bind parameters to selected credential ID or reject ambiguous multi-entry input.
- API impact: selection context may require an additive argument; rejection changes behavior for ambiguous callers.
- Open decisions: implement selection or impose a safe single-credential restriction; restriction recommended as interim behavior.

#### 26g Credential reader accepts redirected input that violates policy
- Verdict / our severity: CONFIRMED / LOW–MED; automation can pass invalid PINs to the device and consume a retry.
- Location at HEAD: `src/Core/src/Credentials/ConsoleCredentialReader.cs:75-77,125-145`; policy `CredentialReaderOptions.cs:44-63,87-94`.
- Spec or contract: `ForPin()` XML: “Creates options configured for PIN input (6-8 numeric digits).”
- Reference implementations: none recorded.
- Evidence: noninteractive path passes input directly to conversion; invalid characters and 3-/9-character PINs are returned as credential buffers.
- Repro test: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreCredentialReaderAuditReproTests.cs::YESDK1634_NonInteractivePinViolatingPolicy_IsRejected("abcdef"|"123"|"123456789")` — failed as expected for all three cases.
- Hardware: none.
- Remediation: enforce configured length, character filter, and control-character policy on redirected input; reject invalid lines. Route confirmation through the same interactive/noninteractive branch.
- API impact: behavioral; invalid redirected input returns null.
- Open decisions: reject or filter; rejection is recommended. Confirmation path also fails with redirected stdin because `Console.KeyAvailable` throws.

#### 26h Credential reader confirmation strands first buffer
- Verdict / our severity: CONFIRMED / LOW.
- Location at HEAD: `src/Core/src/Credentials/ConsoleCredentialReader.cs:87-100`.
- Spec or contract: class remarks: “Returns IMemoryOwner<byte> that zeros memory on disposal [...] Clears intermediate buffers on all code paths”.
- Reference implementations: none recorded.
- Evidence: cancellation after the first read leaves its pooled buffer uncleared while confirmation prompt/second read runs without `try/finally`.
- Repro test: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreCredentialReaderAuditReproTests.cs::YESDK1634_ConfirmationCancelled_FirstCredentialIsCleared` — failed as expected; pooled bytes still contained `123456`.
- Hardware: none.
- Remediation: dispose the first buffer on every exceptional/cancellation path and similarly guard the second.
- API impact: none.
- Open decisions: none.

#### 26i OpenPGP import target mismatch
- Verdict / our severity: CONFIRMED / MED.
- Location at HEAD: `src/OpenPgp/src/IOpenPgpSession.cs:212-225`; `OpenPgpSession.Keys.cs:48-92`; `PrivateKeyTemplate.cs:35-45,97-108`.
- Spec or contract: “The DOs in the Extended Header list start with a Control Reference Template (CRT) of the referenced key. Digital signature: B6 00 (Tag, Length) Confidentiality: B8 00 Authentication: A4 00” — OpenPGP card 3.4.1 §4.4.3.12. API promises “The target key slot” for `keyRef`.
- Reference implementations: yubikey-manager constructs the private-key template from the same requested reference used for algorithm attributes.
- Evidence: attributes target SIG (`C1`) while a mismatched AUT template serializes CRT `A4 00` into the import command.
- Repro test: `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/AuditV2/OpenPgpAuditReproTests.cs::YESDK1634_ImportDoesNotTargetTemplateSlotInsteadOfRequestedSlot` — failed as expected; expected `B6 00`, actual `A4 00`.
- Hardware: none.
- Remediation: reject mismatched references before any attribute mutation, or derive CRT from `keyRef`; rejection recommended.
- API impact: behavioral for mismatched arguments; later removal of redundant template reference would be breaking.
- Open decisions: reject or override; reject is recommended to prevent cross-slot operations.

#### 26j OpenPGP attestation replaces cardholder certificate
- Verdict / our severity: BY-DESIGN, but undocumented / MED documentation footgun.
- Location at HEAD: `src/OpenPgp/src/IOpenPgpSession.cs:241-254`; `OpenPgpSession.Keys.cs:142-175`.
- Spec or contract: “When an attestation statement is generated, it is placed in the OpenPGP cardholder certificate slot for the attested key. ... Any data currently stored in the cardholder certificate slot is overwritten.” — Yubico OpenPGP Attestation, Implementation.
- Reference implementations: yubikey-manager documents the slot write, issues `GET_ATTESTATION`, then reads the certificate.
- Evidence: device overwrites the cardholder certificate slot before SDK reads it; restoring prior certificate is not automatic and may require admin authorization.
- Repro test: none; recorded responses cannot reproduce device-side certificate mutation.
- Hardware: none run; a populated key could lose user data.
- Remediation: add explicit destructive side-effect warning to `AttestKeyAsync` and usage documentation.
- API impact: none.
- Open decisions: documentation first; optional backup/preflight must not promise atomic restoration.

#### 26k OATH credential device identity ignored
- Verdict / our severity: CONFIRMED / MED.
- Location at HEAD: `src/Oath/src/Credential.cs:23-27,169-188`; `OathSession.cs:333-343,346-377,385-405,437-472`.
- Spec or contract: “Credentials are uniquely identified by the combination of DeviceId and Id.” — SDK `Credential` XML contract.
- Reference implementations: python-fido2 checks `device_id` for calculate-code, but raw-ID rename/delete cannot check a foreign handle.
- Evidence: delete, rename, calculate and calculate-code transmit supplied ID on the current session without comparing credential and session device IDs.
- Repro test: `src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/AuditV2/OathAuditReproTests.cs::YESDK1634_ForeignCredentialCannotTargetLocalDevice` — all four variants failed as expected; recorder returned success for same-named local credential.
- Hardware: none.
- Remediation: compare `credential.DeviceId` with session `DeviceId` before constructing or transmitting each command; throw `ArgumentException`.
- API impact: rejects previously accepted cross-device handles.
- Open decisions: exception type; report recommends consistent `ArgumentException`.

#### 26l OTP update security flags
- Verdict / our severity: CONFIRMED-WITH-CORRECTION / MED for AllowUpdate, LOW for ProtectSlot2.
- Location at HEAD: `src/YubiOtp/src/SlotConfiguration.cs:48,65-72,119-126,155-158`; `ExtendedFlag.cs:37-46`; `TicketFlag.cs:38-46`; `UpdateConfiguration.cs:133-140`.
- Spec or contract: “Only flags within the defined update masks are written to the device.” — `UpdateConfiguration.cs:19-29`. `AllowUpdate` promises to permit future update operations on the slot.
- Reference implementations: yubikey-manager includes `EXTFLAG.ALLOW_UPDATE` in update mask and raises `ValueError` for `protect_slot2` during update.
- Evidence: both `AllowUpdate(true)` and `AllowUpdate(false)` emit byte 45 = 4; `ProtectSlot2()` silently clears bit 0x40.
- Repro test: `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/AuditV2/YubiOtpAuditReproTests.cs::YESDK1634_UpdateDoesNotSilentlyDiscardAllowUpdate`, `YESDK1634_UpdateRejectsUnsupportedProtectSlot2` — failed as expected.
- Hardware: none.
- Remediation: include `ExtendedFlag.AllowUpdate` in mask; reject `ProtectSlot2` for update configuration.
- API impact: unsupported call now throws; otherwise behavior corrected.
- Open decisions: reject in update builder or base; explicit update-builder rejection recommended.

#### 26m Security Domain empty allowlist
- Verdict / our severity: CONFIRMED-WITH-CORRECTION / HIGH for callers treating empty as deny-all; device semantics do not represent deny-all with an empty list.
- Location at HEAD: `src/SecurityDomain/src/SecurityDomainSession.cs:741-788`.
- Spec or contract: “If an allowlist with one or more Certificate Serial Number entries exists in the SD for the CA-KLOC’s public key, the SD also verifies that the certificate is contained in an allowlist. Else the SD accepts all certificates signed by the CA-KLOC.” Also: “special care shall be taken never to empty/remove the allowlist ... because no restrictions apply ... once an allowlist is removed.” — GlobalPlatform Card Specification Amendment F SCP11 v1.3 §3.3.
- Reference implementations: yubikey-manager also serializes an empty tag `70` without a guard.
- Evidence: `StoreAllowListAsync(key, [])` serializes zero-length tag `70`; `ClearAllowListAsync` uses the same method.
- Repro test: `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/AuditV2/SecurityDomainAuditReproTests.cs::YESDK1634_EmptyAllowListDoesNotIssueRestrictionRemovingStoreData` — failed as expected; STORE DATA sent without rejection.
- Hardware: fake cannot establish device authorization outcome; specification establishes semantics.
- Remediation: reject empty lists before I/O; keep explicit clear operation with its own wire implementation; document that no allowlist means unrestricted.
- API impact: empty input now throws.
- Open decisions: deny-all policy should use disabling/removing CA trust/key, not empty allowlist.

#### 26n Management builder reuse mutates built configuration
- Verdict / our severity: CONFIRMED / MED.
- Location at HEAD: `src/Management/src/DeviceConfig.cs:148-161,202-215`; `ManagementSession.cs:173`.
- Spec or contract: `Build()` rejects zero USB capabilities; `EnabledCapabilities` is exposed as `IReadOnlyDictionary` (suggesting snapshot semantics, not an explicit immutability promise).
- Reference implementations: no equivalent mutable-builder/read-only-view pattern recorded.
- Evidence: `ReadOnlyDictionary` wraps the builder’s live dictionary; later `.WithCapabilities(Usb, 0)` mutates a previously built, validated configuration and serialization does not revalidate.
- Repro test: `src/Management/tests/Yubico.YubiKit.Management.UnitTests/AuditV2/ManagementAuditReproTests.cs::YESDK1634_BuiltConfigurationIsSnapshotOfBuilder` — failed; expected USB 32, actual 0.
- Hardware: none.
- Remediation: copy the dictionary in `Build` before wrapping; optionally validate direct `DeviceConfig` instances at serialization.
- API impact: none for builder callers; direct-object validation may reject prior inputs.
- Open decisions: builder-only or session-boundary validation; both recommended for invariants.

#### 26o OATH authentication retry zero seed (not reproduced)
- Verdict / our severity: NOT-REPRODUCED at HEAD; no severity for the alleged all-zero retry.
- Location at HEAD: `src/Oath/src/OathSession.cs:757-825`.
- Spec or contract: method contract says it validates an already-derived access key and unconditionally zeroes it; retry contract supplies a borrowed password, not a key to resend.
- Reference implementations: yubikey-manager derives a PBKDF2 key from passphrase and uses the key for challenge HMAC; no corresponding .NET retry helper.
- Evidence: retry invokes provider once, derives a new key, and zeroes that derived key after validation; it retries after success, not by validating a zeroed key. Caller password remains unchanged.
- Repro test: `src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/AuditV2/OathAuditReproTests.cs::YESDK1634_RetryDoesNotZeroCallerPassword` — PASSED; the password bytes were unchanged and only one VALIDATE was sent on the failure path. This test does not cover the success-path retry. The not-reproduced verdict rests on code reading of `OathSession.cs:767-779`: it derives a fresh key, validates, zeroes that key, then retries the operation.
- Hardware: none.
- Remediation: none for this alleged finding.
- API impact: none.
- Open decisions: none.

#### 26p YubiOTP HMAC challenge length ambiguity
- Verdict / our severity: CONFIRMED-WITH-CORRECTION / LOW–MED; wire collision is established, device-output collision depends on slot configuration.
- Location at HEAD: `src/YubiOtp/src/YubiOtpSession.cs:351-390,426-444`.
- Spec or contract: “The size of the challenge sent to the YubiKey with `UseChallenge()` must align with the slot's configuration. ... If the slot has been configured with `UseSmallChallenge()`, a challenge smaller than 64 bytes is acceptable.” — Yubico OTP challenge-response manual. “An HMAC-SHA1 challenge is 64 bytes by default. The YubiKey also supports a short challenge mode ... less than 64 bytes.” — programming manual.
- Reference implementations: yubikey-manager uses the same padding rule; `0x41` and `0x41` followed by 63 zeroes serialize identically.
- Evidence: `PadHmacChallenge` produces identical 64-byte payloads for these different input lengths; invocation has no slot-mode metadata.
- Repro test: `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/AuditV2/YubiOtpAuditReproTests.cs::YESDK1634_DistinctChallengeLengthsDoNotHaveSameRequestBytes` — failed; request collections equal.
- Hardware: none; digest identity depends on mode/firmware.
- Remediation: expose explicit mode or document uniform input-width restriction; reject ambiguous lengths if strict injectivity is required.
- API impact: additive mode overload and potentially breaking validation.
- Open decisions: preserve mode-less method with documented limitation and add explicit mode API (recommended).

#### 26q OpenPGP KDF short count excludes PIN
- Verdict / our severity: CONFIRMED / MED.
- Location at HEAD: `src/OpenPgp/src/Kdf.cs:214-257,319-329`.
- Spec or contract: “at least one copy of the full salt plus passphrase will be provided as input to each hash context regardless of the octet count.” — RFC 9580 §3.7.1.3.
- Reference implementations: yubikey-manager shares the same short-count behavior.
- Evidence: count 8 hashes only the 8-byte salt; count 9 hashes the salt and first PIN byte, not the complete passphrase.
- Repro test: `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/AuditV2/OpenPgpAuditReproTests.cs::YESDK1633_ShortKdfCountStillHashesCompletePin(8|9)` — failed as expected; digest differs from SHA256(salt || PIN).
- Hardware: none.
- Remediation: always hash at least one complete salt-plus-passphrase block.
- API impact: derived output changes for invalid/small counts; migration risk for cards provisioned with such counts.
- Open decisions: follow RFC with migration warning (recommended) or preserve legacy hashes.

### #27 Mismatched public/private key components (YESDK-1635)
- Verdict / our severity: CONFIRMED / MED; trust-boundary integrity, not direct extraction of imported key.
- Location at HEAD: `src/Core/src/Cryptography/AsnPrivateKeyDecoder.cs:223-285,328-374`; `ECPrivateKey.cs:120-132`; `RSAPrivateKey.cs:114-131`.
- Spec or contract: “publicKey contains the elliptic curve public key associated with the private key in question.” and “Given the private key and the parameters, the public key can always be recomputed” — RFC 5915 §3.
- Reference implementations: .NET runtime behavior for inconsistent EC Q is documented as inconsistent; Android and yubikey-manager import private values without checking supplied public Q in the cited paths.
- Evidence: EC decoder checks point format but not Q = D·G; RSA decoder normalizes fields without validating `n=P·Q` or exponent/CRT relationships. Consumers can receive a wrapper reporting unrelated public parameters.
- Repro test: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreAuditReproTests.cs::YESDK1635_EcPkcs8RejectsPublicPointFromDifferentPrivateScalar`, `YESDK1635_RsaPkcs8RejectsModulusUnrelatedToPrivatePrimes` — both failed as expected; no exception.
- Hardware: none; keys generated locally.
- Remediation: validate EC public point against D-derived Q and validate RSA mathematical/CRT relationships before producing wrappers; clear temporary private buffers.
- Review correction: checking Q = D·G portably means deriving Q from D, which is the D-only import path on macOS that #17 flags (`ECPrivateKey.cs:232`). Today PKCS#8 import avoids it. So either do an explicit scalar multiplication, or accept that the #17 residual extends to every EC PKCS#8 import. Also, whether `ImportParameters(D+Q)` validates consistency differs by platform (OpenSSL does; CNG and macOS are not established).
- API impact: malformed keys previously accepted will throw `CryptographicException`.
- Open decisions: validate all RSA CRT relationships or minimum checks; all checks recommended.

### #28 Invalid OATH period zero aborts batch calculation (YESDK-1636)
- Verdict / our severity: CONFIRMED / MED.
- Location at HEAD: `src/Oath/src/Credential.cs:135-153`; `src/Oath/src/OathSession.cs:520-580`.
- Spec or contract: “More specifically, T = (Current Unix time - T0) / X, where the default floor function is used in the computation.” — RFC 6238 §4.2. Zero is not a defined time step.
- Reference implementations: yubikey-manager has the same zero-period division failure; that does not establish correct behavior.
- Evidence: zero period parsed from the device’s TOTP credential causes `CalculateCodeAsync` to divide by zero, so the valid credential later in the batch is not returned.
- Repro test: `src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/AuditV2/OathAuditReproTests.cs::YESDK1636_MalformedPeriodDoesNotAbortNormalBatchEntry` — failed as expected with `DivideByZeroException`.
- Hardware: CONFIRMED by authoritative `HW-RESULTS.md` on 2026-09-25: `YESDK1636_RawPutZeroPeriodDoesNotAbortNormalBatch`; corrected test type byte from `0x11` (HOTP) to `0x21` (TOTP/SHA1); batch threw `DivideByZeroException` and did not return the valid credential. Raw DELETE cleanup succeeded.
- Remediation: validate positive periods at import/creation and untrusted response boundaries; skip/report malformed entries while preserving valid batch results.
- API impact: none if malformed-response conventions are retained.
- Open decisions: omit malformed credential with diagnostics (recommended) or expose it without a code.

## New findings (not in the original audit)

### Duplicate `pubKeyCredParams` entries accepted
- Verdict / our severity: CONFIRMED / LOW; invalid platform request that an authenticator may tolerate.
- Location at HEAD: `src/Fido2/src/FidoSession.cs:250-255`; `src/Fido2/src/Cbor/FidoSessionRequestEncoding.cs:30-34,162-174`.
- Spec or contract: “The array is ordered from most preferred to least preferred and MUST NOT include duplicate entries.” — CTAP 2.3 §6.1.
- Reference implementations: python-fido2 forwards caller list without deduplication; libfido2’s single configured type avoids this particular duplicate case.
- Evidence: repeating the same `PublicKeyCredentialParameters(ES256)` produces two entries in CBOR key 4.
- Repro test: consolidated rename `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2RequestAuditReproTests.cs::NEW_MakeCredentialDoesNotEncodeDuplicateCredentialParameters` — failed as expected; actual count 2.
- Hardware: none; CBOR encoding suffices.
- Remediation: reject duplicate `(Type, Algorithm)` entries at `MakeCredentialAsync`, preserving preference order for valid entries.
- API impact: behavioral for duplicate-list callers; no signature change.
- Open decisions: reject (recommended) or stable deduplication.

### SCP adjacent findings
- Verdict / our severity: observations beyond #23; unchecked initialization slicing can produce range exceptions, and bare `9000` with empty data skips R-MAC verification. The empty-response case is a design question, not classified here as a separate confirmed defect.
- Location at HEAD: `ScpState.Scp03InitAsync` (`src/Core/src/Protocols/SmartCard/Scp/ScpState.Scp03.cs:47-51`), `Scp11InitAsync` (`ScpState.Scp11.cs:117-118`), `ScpState.Decrypt` (`ScpState.cs:121-139`), and `ScpProcessor.cs:121`.
- Spec or contract: SCP03 INITIALIZE UPDATE fields are sliced without length validation; SCP11 accesses first two TLVs without count validation. For successful protected response, §6.2.5 requires the R-MAC over chaining value, response data if present, and status bytes.
- Reference implementations: ykman accepts an empty decrypted plaintext block; ykman, Android, and Swift also skip R-MAC verification for empty data with `9000`.
- Evidence: malformed SCP03/SCP11 initialization responses produce `ArgumentOutOfRangeException`/index errors rather than `BadResponseException`; decrypt padding scan omits index 0. Empty `9000` response bypasses verification in the SDK and cited peers.
- Repro test: no separate repro path/result recorded for these observations; #23 tests exercise only short response R-MAC handling.
- Hardware: none.
- Remediation: add initialization length/TLV-count guards and validate ciphertext block shape/padding; decide separately whether empty-data success must carry and verify R-MAC.
- API impact: internal malformed-response exception behavior.
- Open decisions: whether to require R-MAC on bare empty-data `9000` despite matching reference behavior; verify firmware before changing.

### CLI and confirmation-mode adjacent findings
- Verdict / our severity: CONFIRMED (2 defects) / LOW. Credential confirmation cannot be used with redirected stdin, and the CLI programs the wrong static password (it passes the password's UTF-8 bytes where HID scan codes are expected). Both are in internal CLI tooling.
- Location at HEAD: `src/Cli.Commands/src/Otp/OtpCommands.cs:483,494`; `src/Core/src/Credentials/ConsoleCredentialReader.cs:81-117`.
- Spec or contract: credential-reader confirmation API should support its configured interaction mode; no protocol clause recorded. `scanCodes` naming implies scan codes, while current value is UTF-8 password bytes.
- Reference implementations: none recorded.
- Evidence: real redirected stdin makes `Console.KeyAvailable` throw `InvalidOperationException`; `OtpStaticCommand` passes `Encoding.UTF8.GetBytes(password)` as `scanCodes` (`OtpCommands.cs:483,493,513`), even though `StaticPasswordSlotConfiguration(string password, KeyboardLayout)` (`StaticPasswordSlotConfiguration.cs:53`) already translates characters into HID scan codes. So the slot types different characters from the password the user supplied.
- Repro test: no named repro for OTP scan-code question; redirected-input exception reproduced with scratch program `echo 123456 | dotnet run app.cs`.
- Hardware: none.
- Remediation: route confirmation input through the non-interactive path; build static-password slots with the `(string, KeyboardLayout)` constructor and honour the CLI keyboard-layout option.
- API impact: none (CLI behaviour fix).
- Open decisions: none.

### Lockless npm installs and monthly synthesis credential co-location
- Verdict / our severity: adjacent workflow supply-chain findings; no severity assigned in report.
- Location at HEAD: `.github/workflows/docs-update.yml:161-166`; `.github/workflows/architecture-docs-monthly-synthesis.yml:8-10,17-55`; `.github/workflows/migration-docs-monthly-synthesis.yml:10-12,19-57`.
- Spec or contract: OpenSSF guidance: “A ‘pinned dependency’ is a dependency that is explicitly set to a specific hash instead of allowing a mutable version or range of versions.” Its token-permissions guidance recommends setting required writes at job level.
- Reference implementations: none; guidance-based workflow review.
- Evidence: global `@mermaid-js/mermaid-cli@11.16.0` installs have no npm lockfile to pin transitive dependencies. Monthly synthesis jobs place OAuth secret and repository write token in the same job; architecture monthly installs before invoking Claude.
- Repro test: none; static inspection only.
- Hardware: not applicable.
- Remediation: use lockfile-backed `npm ci` or otherwise pin dependency content; split read-only generation from minimal write/apply job.
- API impact: none; CI only.
- Open decisions: align both monthly synthesis jobs with the read-only generator/write-only patch workflow.

# How to reproduce

Run everything from the worktree root, `worktrees/yubikit-audit-v2-verification`. Judge each run by the per-project `total:` and `failed:` lines, not by the closing summary line, which counts projects.

Unit repros (no hardware; every failure is an intended repro):

```bash
dotnet toolchain.cs -- test --filter "FullyQualifiedName~AuditV2&Category!=RequiresHardware&Category!=RequiresUserPresence"
```

Hardware repros that need no touch. They reset the PIV and OATH applications and delete OATH credentials on allow-listed devices:

```bash
dotnet toolchain.cs -- test --integration --project Piv.IntegrationTests --filter "FullyQualifiedName~AuditV2"      # #22 (in-process), #24
dotnet toolchain.cs -- test --integration --project Oath --filter "FullyQualifiedName~AuditV2"                     # #28
dotnet toolchain.cs -- test --integration --project Fido2 --filter "FullyQualifiedName~YESDK1618_MakeCredentialFalseUserPresenceReturnsInvalidOption"  # #10
```

#22 cross-process repro. Each command runs as its own OS process:

```bash
cd docs/audit-v2/evidence/scripts/Yesdk1630CrossProcess
dotnet run app.cs -- <serial> setup                       # reset PIV; P-256 in 9A, PIN=ONCE, TOUCH=NEVER
dotnet run app.cs -- <serial> hold &                      # "attacker": opens PIV, no PIN, waits
dotnet run app.cs -- <serial> verify                      # "victim": verifies PIN, disposes, exits
touch "$TMPDIR/yesdk1630-<serial>.go"; wait               # attacker signs -> succeeds today
EXCL=1 dotnet run app.cs -- <serial> verify               # variant: victim uses the exclusive switch -> victim cannot connect; attacker is rejected
```

# User-present verification (done)

Run with the maintainer present on 2026-09-25. Details are in the per-finding Hardware bullets.

- [x] #11 passes, but only because the YubiKey tolerates `h''`.
- [x] #2 reproduced. New test `YESDK1610_RequiredUvRegistrationWithPinSucceeds`, plus an independent python-fido2 check.
- [x] #7 reproduced (UV=Preferred).
- [x] #1 reproduced at registration (UV=Preferred).
- [x] #13 reproduced in both directions.
- [ ] Not run: #5 on hardware (the unit repro suffices), and the python-to-SDK read interop (superseded by the #13 observation). No FIDO reset was needed.
- Commands: `dotnet toolchain.cs -- test --integration --project WebAuthn.IntegrationTests --filter "FullyQualifiedName~AuditV2"` and `dotnet toolchain.cs -- test --integration --project Fido2.IntegrationTests --filter "FullyQualifiedName~YESDK1621|FullyQualifiedName~CredBlob_StoreAndRetrieve"`. Both need touch. The macOS HID "RunLoop completed but no report was queued" flake appeared once; re-run if you see it.

# Abbreviations

- AAD: additional authenticated data (AES-GCM)
- AAGUID: authenticator attestation GUID
- APDU: application protocol data unit (smart-card command/response)
- BCL: .NET base class library
- CBOR: Concise Binary Object Representation
- CTAP: Client to Authenticator Protocol
- CA-KLOC: GlobalPlatform off-card CA key used for SCP11 certificate checks
- GP: GlobalPlatform
- IOOR: IndexOutOfRangeException
- KDF: key derivation function
- PRF: pseudo-random function (WebAuthn extension, backed by CTAP hmac-secret)
- R-MAC: response message authentication code (SCP)
- RP: relying party
- SCP: Secure Channel Protocol (SCP03/SCP11)
- SD: Security Domain
- TLV: tag-length-value encoding
- UP / UV: user presence / user verification
- WS: workstream
