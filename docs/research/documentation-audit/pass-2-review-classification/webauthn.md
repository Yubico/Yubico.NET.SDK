# WebAuthn: Pass 2 classification
Classifier: one dedicated subagent, 2026-11-14. Source: master lines 484–1387 at 52d34ca65. Policy: pass-2-review-policy.md (approved). Final per-card calls: `../pass-2-review-ledger.md`.
Sibling pins: python-fido2 @ 5bc9d3a; yubikit-android @ f4626856; yubikey-manager-rust-auto (Rust) @ 90940e9b. yubikey-manager @ 4ca60f70 not used for evidence in this file.

## Summary
| Disposition | Count | Cards |
| --- | --- | --- |
| A1 No-brainer | 3 | WA-IMP-008, WA-IMP-020, WA-IMP-021 |
| A2 Sibling consensus | 5 | WA-IMP-001, WA-IMP-004, WA-IMP-009, WA-IMP-018, WA-IMP-019 |
| A3 Python partial confluence | 0 | none (WA-IMP-001 batch-1 A3 is now A2, see Coverage check) |
| A4 Split siblings | 0 | none (WA-IMP-003 batch-1 A4 is withdrawn, see Interview queue) |
| A5 Internal precedent | 2 | WA-IMP-006, WA-IMP-007 |
| A6 Keep and document | 1 | WA-IMP-017 |
| Interview (gated, or unmatched) | 11 | High: WA-IMP-011, WA-IMP-012, WA-IMP-013. Medium: WA-IMP-002, WA-IMP-010, WA-IMP-014, WA-IMP-015, WA-IMP-016, whole-ceremony lifetime. Low: WA-IMP-003, WA-IMP-005 |
| T1 Hardware qualification | 1 | Ceremony/platform qualification |
| T2 candidate tag (overlaps rows above) | 8 | WA-IMP-002, 003, 006, 008, 009, 013, 016, whole-ceremony lifetime |

## Index
| Card | Severity | Disposition | One-line rationale |
| --- | --- | --- | --- |
| WA-IMP-001 | Medium | A2 | python-fido2, Android and Rust all forward enterprise attestation (python and Android only when GetInfo `ep` is set). Add vendor-facilitated mode 1; document Indirect as passthrough. |
| WA-IMP-002 | Medium | Interview (G5; T2 c) | Siblings split on optional-output failure (python fails the ceremony; Android and Rust omit). Sia keeps omission to avoid orphaned credentials. |
| WA-IMP-003 | Low | Interview (G2; T2 b) | L3 §5.1.3/§5.1.4 correct an out-of-range timeout to the nearest in-range value. Batch-1 "reject" conflicts with that. |
| WA-IMP-004 | Low | A2 (T1 lane) | python, Android and Rust all make UV mandatory under Discouraged when `alwaysUv` is set. Test guard only. |
| WA-IMP-005 | Low | Interview (G2, G3) | python-fido2 uses `json.dumps` (short escapes, ASCII-escaped non-ASCII). L3 §5.8.1.1 CCDToString requires four-digit escapes. |
| WA-IMP-006 | Medium | A5 | Ctap2's own decoders split copy and borrow, with the borrowed statement contract documented. Keep the borrowed contract and pin it with a test. |
| WA-IMP-007 | Low | A5 | CLAUDE.md ≤16-byte readonly-struct rule and Ctap2's `Guid` AAGUID give the carrier. Default is 16 zero bytes with reflexive equality. |
| WA-IMP-008 | Low | A1 (T2 d, test-only) | Mechanical test oracle fix: check retained owner storage after disposal. |
| WA-IMP-009 | Medium | A2 (T2 e) | python, Android and Rust negotiate the PIN/UV protocol from GetInfo. python and Rust gate built-in UV on `pinUvAuthToken`. |
| WA-IMP-010 | Medium | Interview (G2, G3) | Probe-level singleton recovery exists everywhere. Final empty or crashing ID is shared by python and Rust. Only Android recovers the original singleton. |
| WA-IMP-011 | High | Interview (G1, G2, G3) | L3 §10.1.4 says the PRF MUST be the verified one. python, Android and Rust apply no UV override. |
| WA-IMP-012 | High | Interview (G1) | python, Android and Rust all reject enforced-but-unsupported credProtect. .NET silently omits it. |
| WA-IMP-013 | High | Interview (G1; T2 d) | Residual PRF and blob plaintext and temporaries. Mechanical clears versus an owned result API. |
| WA-IMP-014 | Medium | Interview (G3) | python, Android and Rust all report `supported=false` on a missing key and never force resident-key creation. |
| WA-IMP-015 | Medium | Interview (G2, G3) | L3 §10.1.5 requires exactly one original `allowCredentials` entry for a write. All three siblings skip the check. |
| WA-IMP-016 | Medium | Interview (G2; T2 c) | L3 §10.1.5 note: a failed read keeps `largeBlob` present with no `blob`. python fails the assertion, Android omits, Rust matches the note. |
| WA-IMP-017 | no severity | A6 | Storage-only, token-required writes are an acceptable fail-closed contract. Direct route and pinless exception are feature additions. |
| WA-IMP-018 | Low | A2 | python, Android and Rust request minPinLength only when `Requested` is true. .NET requests on presence. |
| WA-IMP-019 | Low | A2 | python, Android and Rust accept empty generated handles. .NET rejects them in the constructor. TBS rule unchanged. |
| WA-IMP-020 | Low | A1 | Mechanical test fixture change from the card. Keep the extra-field case. |
| WA-IMP-021 | Low | A1 | Mechanical test assertion: returned credential ID must belong to the allow list. |
| Whole-ceremony lifetime | Medium (conditional) | Interview (unmatched; T2 a) | No gate or auto rule settles admission and shared disposal. Decide once with the OATH workflow owner. |
| Ceremony/platform qualification | no severity | T1 | Device, firmware, transport and OS evidence. Not a voice item. |

## Interview queue (sorted High, then Medium, then Low)

### WA-IMP-011: verified PRF variant not enforced
- Gate(s) and why: G1 (High). G2: L3 §10.1.4 says the PRF "MUST be the one used for when user verification is performed". python-fido2, Android and Rust apply no UV override to PRF. G3: all three siblings share the gap, so their behaviour is not precedent.
- Severity / certainty: High. Source confirmed; spec mismatch confirmed.
- The decision Dennis must make: When PRF is requested, should the SDK force the verified PRF (UV Required for evaluation, and never publish an unverified PRF result)?
- Options:
  - (A) Enforce. Resolve UV to Required for PRF evaluation in both ceremonies and at creation time. Consequence: PRF callers prompt for PIN or UV more often; spec-conformant. Sia's lean: A. The spec states a MUST, and sibling behaviour is a shared bug, not precedent.
  - (B) Keep the caller's UV preference and keep the documented limitation. Consequence: the same credential and input can yield different derived secrets across UV states.
  - (C) Fail closed unless the caller requests Required. Consequence: no silent unverified output, but callers must change code.
- Evidence: .NET `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs:118–120`; `src/WebAuthn/src/Extensions/Processing/Processors/PrfProcessor.cs:37–42, 130`. python-fido2 `fido2/ctap2/extensions.py:256–326 @5bc9d3a` (default permissions 0, `:58–66`). Android `fido/.../client/extensions/HmacSecretExtension.java:102–160 @f4626856` (no UV branch). Rust `crates/yubikit/src/webauthn/extensions/prf.rs @90940e9b` (no UV or permission logic).

### WA-IMP-012: credProtect enforcement silently omitted
- Gate(s) and why: G1 (High). G2 does not fire, because the siblings enforce and .NET is the outlier. The siblings are the evidence for option A.
- Severity / certainty: High. Source confirmed.
- The decision Dennis must make: When `enforceCredentialProtectionPolicy` is true and the authenticator lacks credProtect, should creation fail before makeCredential?
- Options:
  - (A) Fail before creation with NotSupported. python-fido2 (`fido2/ctap2/extensions.py:523–532 @5bc9d3a`), Android (`fido/.../extensions/CredProtectExtension.java:63–67 @f4626856`) and Rust (`crates/yubikit/src/webauthn/extensions/cred_protect.rs:149 @90940e9b`) all do this. Sia's lean: A. CTAP 2.3 §12.1 says the platform SHOULD NOT create a credential without the requested protection, and a created credential cannot be undone.
  - (B) Keep the omission and document it. Consequence: an unprotected credential is created silently.
  - (C) Create, then reject if the returned policy is weaker. Consequence: an orphaned credential. This detects the problem but does not prevent it.
- Evidence: .NET `src/WebAuthn/src/Extensions/Processing/RegistrationExtensionSession.cs:64`; `src/WebAuthn/src/Extensions/Inputs/CredProtectInput.cs:26–30`; `src/WebAuthn/src/Extensions/Processing/Processors/CredProtectProcessor.cs:25–28`. CTAP 2.3 §12.1 (quoted above).

### WA-IMP-013: residual sensitive results and temporaries
- Gate(s) and why: G1 (High). The siblings give no usable comparison (python bytes are immutable, Android and Rust do not zero), so G3 does not apply.
- Severity / certainty: High. Selected allocations confirmed; no exploit claim.
- The decision Dennis must make: Introduce owned disposable result types now (breaking, allowed in alpha), or clear temporaries now and defer the owner API to the T2 (d) secret-ownership decision?
- Options:
  - (A) Owned disposable PRF and blob result types now, plus finally-clears of temporaries. Consequence: a public API change that touches CTAP copies later.
  - (B) Mechanical clears now (ComputeSalt concatenation, range temporaries, failed allocations), and document residual copies. Owner API waits for T2 (d). Sia's lean: B, because no public API should change before the cross-applet ownership decision.
  - (C) Defer everything. Consequence: the High residual remains.
- Evidence: .NET `src/WebAuthn/src/Extensions/Codec/PrfCodec.cs:79–82, 57–58`; `src/WebAuthn/src/Extensions/Inputs/PrfInput.cs:99–101, 87–89`; `src/WebAuthn/src/Extensions/Processing/Processors/PrfProcessor.cs:174–175`. Module `src/WebAuthn/CLAUDE.md` lines 433–435 and 445–446.

### WA-IMP-002: ceremony error mapping and optional-output failure
- Gate(s) and why: G5. Sia's option keeps per-extension omission, which diverges from python-fido2's whole-ceremony failure. The concrete user benefit is avoiding a registration reported as failed while a credential now exists. Android and Rust also omit on output failure, but python is the primary FIDO precedent. Also T2 (c).
- Severity / certainty: Medium. Catch and support boundaries source-confirmed.
- The decision Dennis must make: After a credential is created, should an optional-output failure fail the whole registration (python) or omit that output and return the credential (current .NET, Android for large blob, Rust)?
- Options:
  - (A) Keep per-extension omission and document it. Also route GetInfo and token failures through the ceremony error mapper. Sia's lean: A. python maps GetInfo errors inside the same wrapper, and Rust maps GetInfo and token errors through `From<Ctap2Error>`.
  - (B) Whole-ceremony failure on output failure (python `fido2/client/__init__.py:905–913 @5bc9d3a`). Consequence: orphaned credentials and a failure the RP never sees.
  - (C) Omit the output and add a typed diagnostic to the response. Consequence: a new public field, allowed in alpha.
- Evidence: .NET `src/WebAuthn/src/Client/WebAuthnClient.Registration.cs:100` (GetInfo outside the mapper); `src/WebAuthn/src/Client/WebAuthnClient.Ceremony.cs:131–136`; `src/WebAuthn/src/Extensions/Processing/RegistrationExtensionSession.cs:118–127`. python `fido2/client/__init__.py:757, 1130–1139 @5bc9d3a`. Android `fido/.../client/extensions/LargeBlobExtension.java:146–150 @f4626856`. Rust `crates/yubikit/src/webauthn/client.rs:751–773 @90940e9b` (output preparation returns no error).

### WA-IMP-010: selected credential association lost on omitted descriptor
- Gate(s) and why: G3. python-fido2 (`fido2/client/__init__.py:198 @5bc9d3a`) indexes `credential["id"]` with no fallback, and Rust (`crates/yubikit/src/webauthn/client.rs:577–581 @90940e9b`) uses `unwrap_or_default()`. Only Android recovers the original singleton, via `Ctap2Session.java:1550–1566` and `PublicKeyCredential.java:197 @f4626856`, and throws for multi-entry lists. G2 also fires: L3 §5.1 says `rawId` returns the credential's identifier, so an empty ID does not meet it. Rust's multi-chunk probe also returns the first chunk entry when the response omits the credential (`client.rs:983–987`), which is a guess the card forbids.
- Severity / certainty: Medium. Both ID-loss sites confirmed in .NET (`CredentialMatcher.cs:60–61`, `WebAuthnClient.Authentication.cs:217`). The probe already recovers singletons (`CredentialFilter.cs:130–134`).
- The decision Dennis must make: On an omitted descriptor, recover only an original singleton allow-list entry and fail otherwise?
- Options:
  - (A) Android's behaviour. Recover only the original singleton. Otherwise fail with a typed malformed-response error. Sia's lean: A. It is the only sibling that matches the spec and never guesses.
  - (B) Keep the empty ID and document it. Consequence: the RP receives a response without a usable ID.
  - (C) Fail on any omission. Consequence: stricter, but rejects the unambiguous CTAP 2.0 singleton case.
- Evidence: .NET `src/WebAuthn/src/Client/Authentication/CredentialMatcher.cs:59–61`; `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs:216–217, 49–54`; `src/WebAuthn/src/Client/CredentialFilter.cs:130–134`. python `fido2/client/__init__.py:198, 599–603 @5bc9d3a`. Rust `client.rs:577–581, 983–987 @90940e9b`. Android `Ctap2Session.java:1550–1566`, `Ctap2Client.java:909–914 @f4626856`.

### WA-IMP-014: required largeBlob without a discoverable credential
- Gate(s) and why: G3. python (`fido2/ctap2/extensions.py:416–430 @5bc9d3a`), Android (`LargeBlobExtension.java:82–92 @f4626856`; `Ctap2Client.java:492–494` ties rk only to the selection) and Rust (`crates/yubikit/src/webauthn/extensions/large_blob.rs:142–148, 183 @90940e9b`) check only support and report false on a missing key. None forces rk.
- Severity / certainty: Medium. Effective-RK and key-output gaps source-confirmed.
- The decision Dennis must make: For `support: required` with a non-discoverable request, should the SDK reject before transmission, force resident-key creation, or keep reporting `supported=false`?
- Options:
  - (A) Reject before transmission (NotSupported) when Required and effective rk is false. Sia's lean: A. No side effect, and the caller learns the cause before a credential exists.
  - (B) Force rk true when Required. Consequence: silently changes the credential type. No sibling does this.
  - (C) Keep and document that Required does not guarantee a key. Consequence: an unusable credential persists.
- Evidence: .NET `src/WebAuthn/src/Extensions/Processing/Processors/LargeBlobProcessor.cs:52–57, 58–76`; `src/WebAuthn/src/Extensions/Processing/RegistrationExtensionSession.cs:67`. CTAP §12.3 rk rule as cited in the .NET doc (not re-verified in this pass).

### WA-IMP-015: blob write with multiple original allowed credentials
- Gate(s) and why: G2 and G3. WebAuthn L3 §10.1.5 says: "If write is present: If allowCredentials does not contain exactly one element: Return a DOMException whose name is NotSupportedError." python (`fido2/ctap2/extensions.py:432–458 @5bc9d3a`), Android (`LargeBlobExtension.java:112–122 @f4626856`) and Rust (`large_blob.rs:151–171 @90940e9b`) all accept multi-entry writes.
- Severity / certainty: Medium. Missing original-list check source-confirmed.
- The decision Dennis must make: Enforce the exactly-one rule before any side effect, or accept writes against the probe-selected credential and document the divergence?
- Options:
  - (A) Enforce exactly one original entry before backend, probe, token or write. Sia's lean: A. This is the spec's rule, and the check is mechanical.
  - (B) Accept multi-entry writes against the probe-selected credential and document the divergence. Consequence: a write may land on a credential the RP did not isolate.
- Evidence: .NET `src/WebAuthn/src/Extensions/Processing/Processors/LargeBlobProcessor.cs:81–84, 84–94`. python and Android and Rust as above.

### WA-IMP-016: failed or absent largeBlob read output
- Gate(s) and why: G2. The L3 §10.1.5 note says: "If the read is not successful, largeBlob will be present in AuthenticationExtensionsClientOutputs but the blob member will not be present." python fails the assertion (`fido2/ctap2/extensions.py:443–463 @5bc9d3a`). Android omits the whole largeBlob output (`LargeBlobExtension.java:127–150 @f4626856`). Rust matches the note (`client.rs:888–891 @90940e9b`). Also T2 (c).
- Severity / certainty: Medium. Assignment confirmed; the isolated .NET conversion from null array to present empty memory is historical evidence.
- The decision Dennis must make: On a failed read, should the output be present with no blob, should the assertion fail, or should the output be omitted?
- Options:
  - (A) Spec behaviour: present extension, blob absent, and keep "absent" distinct from "present empty" (null maps to absent, non-null empty maps to present empty). Sia's lean: A. The spec describes it, and a read has no side effect.
  - (B) Typed failure, as python does. Consequence: a successful assertion is lost on a read error.
  - (C) Omit, as Android does. Consequence: indistinguishable from "not requested", which is misleading.
- Evidence: .NET `src/WebAuthn/src/Extensions/Processing/Processors/LargeBlobProcessor.cs:143–150` (conversion at `:146–149`, doc at `:145–147`).

### Whole-ceremony lifetime and shared disposal (linked CORE-B5-C006; CORE-WA-X002)
- Gate(s) and why: no gate fires. No sibling settles admission: Rust serializes through `&mut self` (`crates/yubikit/src/webauthn/client.rs:190–191 @90940e9b`), and python and Android are synchronous. No auto rule matches, so this goes to interview. T2 (a).
- Severity / certainty: Medium only if concurrent disposal is admitted. Current order confirmed; no runtime race run here.
- The decision Dennis must make: Keep caller-serialized, fully awaited ceremonies with concurrent disposal documented as unsupported, or add whole-ceremony admission and a shared disposal task? Decide once with the OATH workflow owner.
- Options:
  - (A) Document as unsupported and keep the bool guard. Consequence: unsafe concurrent disposal stays a caller obligation, which module CLAUDE.md already states (lines 183–189).
  - (B) Admission gate plus shared disposal across probe, token, GetNext and output continuations. Consequence: a new lifecycle mechanism shared with OATH.
  - (C) Gate admission only. Consequence: partial protection.
  - Sia's lean: A until the T2 (a) session decides.
- Evidence: .NET `src/WebAuthn/src/Client/WebAuthnClient.cs:105–109`; `src/WebAuthn/src/Client/WebAuthnClient.Ceremony.cs:120–147`. Module `src/WebAuthn/CLAUDE.md` lines 183–189.

### WA-IMP-003: client timeout range and success race
- Gate(s) and why: G2. L3 §5.1.3 and §5.1.4 say: "check if its value lies within a reasonable range as defined by the client and if not, correct it to the closest value lying within that range." Batch-1's stricter reject (A4) conflicts with that, so the batch-1 disposition is withdrawn and this card moves to interview. python-fido2 applies no correction (`fido2/client/__init__.py:1119–1124, 1157–1162 @5bc9d3a`). Also T2 (b).
- Severity / certainty: Low. Cooperative mechanism source-confirmed.
- The decision Dennis must make: Which client range applies to `Timeout`, and does the 300 s recommended default apply when `Timeout` is null?
- Options:
  - (A) Clamp only the upper bound to 600000 ms (L3 §15.1 recommended maximum). Leave short values alone, and leave null as no timer. Sia's lean: A. Clamping only shortens an oversized window; clamping short values would widen it.
  - (B) Clamp both ends to 300000–600000 ms and apply the 300 s default. Consequence: short RP timeouts are lengthened, which widens the ceremony window.
  - (C) Keep current behaviour and correct the docs. Consequence: smallest change, still different from §5.1.3.
- Keep unchanged: success-can-win (python agrees; Android and Rust ignore client timeouts). Non-positive values are already rejected (`WebAuthnClient.Validation.cs:94–100, 138–144`).
- Evidence: .NET `src/WebAuthn/src/Client/CeremonyDeadline.cs:23–26, 37–41`; `src/WebAuthn/src/Client/WebAuthnClient.Validation.cs:94–100, 138–144`. python `fido2/client/__init__.py:1119–1124 @5bc9d3a`. Android `fido/.../client/Ctap2Client.java:80–81 @f4626856`. Rust `crates/yubikit/src/webauthn/types.rs:421–423 @90940e9b` (field not used by `client.rs`). Spec §5.1.3, §5.1.4, §15.1.

### WA-IMP-005: client-data control escapes
- Gate(s) and why: G2 and G3. L3 §5.8.1.1 defines CCDToString: code points U+0020–U+0021, U+0023–U+005B and U+005D–U+10FFFF are written as UTF-8. U+0022 becomes `\"`, U+005C becomes `\\`, and everything else becomes `\u` plus four lowercase hex digits. python-fido2 (`fido2/webauthn.py:406–415 @5bc9d3a`, `json.dumps`) uses short escapes for `\b \f \n \r \t` and ASCII-escapes non-ASCII. .NET shares the short escapes (`WebAuthnClientData.cs:163–177`) and escapes U+007F–U+009F, which CCDToString leaves raw. Rust writes no escaping at all (`crates/yubikit/src/webauthn/types.rs:519–527 @90940e9b`). Android takes app-supplied JSON (`ClientDataProvider.java:55 @f4626856`). Master said "Yes" for no-brainer; the spec gate overrides that.
- Severity / certainty: Low. Exact-byte interoperability gap; no injection shown.
- The decision Dennis must make: Match CCDToString byte for byte, or document the builder as JSON-compatible but not exact?
- Options:
  - (A) Exact CCDToString: four lowercase hex digits for all C0 controls, raw U+007F–U+009F, raw non-ASCII. Sia's lean: A. The spec is explicit and the change is cheap. Byte-exact vectors then pass, and the divergence from python-fido2 (which is also non-conformant) is no reason to stay wrong.
  - (B) Keep short escapes and document as JSON-compatible. Consequence: exact-byte vectors fail and the spec is not met. RPs that hash the bytes are unaffected either way.
- Evidence: .NET `src/WebAuthn/src/Client/WebAuthnClientData.cs:163–182, 77–79`. Spec §5.8.1.1 "Serialization".

## Auto-approved

### WA-IMP-008: retained shared-secret owner test
- Rule: A1. Card is "No-brainer: yes". No sibling equivalent exists, and the change is a literal test replacement.
- Evidence pin (E2): .NET `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnBackendTests.cs:78–82` (BEFORE). Owner under test `src/WebAuthn/src/Client/PinUvSharedSecret.cs:39–54`.
- Pseudocode for the fix:
```text
retained = secret.SharedSecret
using (secret):
    assert any nonzero byte in retained.Span
    secret.Dispose()
    assert ObjectDisposedException on secret.SharedSecret
    assert every byte of retained.Span == 0
```
- Test to add: the card's replacement. It fails if `ZeroMemory` is removed from `PinUvSharedSecret.Dispose`.
- E1 escalation risk: none known.

### WA-IMP-020: minimal flags-only inner signing attestation
- Rule: A1. Card is "No-brainer: yes". Test-only fixture change.
- Evidence pin (E2): .NET `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/PreviewSign/PreviewSignCodecTests.cs:240–246` (BEFORE). No sibling needed.
- Pseudocode for the fix:
```text
extensionWriter.WriteStartMap(1); WriteInt32(4); WriteInt32(flags); WriteEndMap()
keep the existing algorithm-plus-flags fixture as a separate extra-field case
```
- Test to add: decode and repack asserts key/handle, flags and the outer selected algorithm. The extra-field case stays.
- E1 escalation risk: none known.

### WA-IMP-021: returned credential ID membership before key selection
- Rule: A1. Card is "No-brainer: yes". Test-only assertion change.
- Evidence pin (E2): .NET `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs:254–256` (BEFORE). No sibling needed.
- Pseudocode for the fix:
```text
isFirst = id equals first.CredentialId; isSecond = id equals second.CredentialId
Assert.True(isFirst || isSecond)
(returned, message) = isFirst ? (first, messageA) : (second, messageB)
```
- Test to add: the card's replacement. The hardware run stays approval-gated (PIN, UP, credential setup).
- E1 escalation risk: none known.

### WA-IMP-001: enterprise forwarding and Indirect passthrough
- Rule: A2. python-fido2 forwards `enterpriseAttestation` = 1 (no RP list configured) or 2 (RP in list), only when `ep` is set (`fido2/client/__init__.py:759–769`). Android forwards the caller's value only when `ep` is set (`Ctap2Client.java:540–546`). Rust forwards 1 unconditionally (`client.rs:295–298, 371–372`). All three forward. Indirect is passthrough in python and Android, so documenting it is consensus. Spec §5.4.7 allows enterprise attestation when "the user agent or authenticator configuration permits it"; vendor-facilitated mode leaves that to the authenticator. Batch-1 A3 becomes A2 on the Rust evidence; disposition unchanged.
- Evidence pin (E2): python-fido2 `fido2/client/__init__.py:759–769 @5bc9d3a`. Android `fido/.../client/Ctap2Client.java:194, 540–546 @f4626856`. Rust `crates/yubikit/src/webauthn/client.rs:295–298, 371–372 @90940e9b`. .NET anchors: `src/WebAuthn/src/Client/WebAuthnClient.Registration.cs:205–207` (BEFORE), `src/WebAuthn/src/Client/IWebAuthnBackend.cs:121` (request record), `src/WebAuthn/src/Client/WebAuthnBackend.cs:149–161` (options mapping), `src/Ctap2/src/Credentials/MakeCredentialOptions.cs:113` (`EnterpriseAttestation` already exists), `src/Ctap2/src/Cbor/Ctap2SessionRequestEncoding.cs:74–76` (already encodes key 10).
- Pseudocode for the fix:
```text
BuildMakeCredentialRequest:
  enterprise = Attestation == Enterprise && info.Options["ep"] == true ? 1 : null
  request.EnterpriseAttestation = enterprise          // new field on BackendMakeCredentialRequest
WebAuthnBackend.MakeCredentialAsync:
  options.EnterpriseAttestation = request.EnterpriseAttestation   // existing CTAP field
Indirect, None, Direct: unchanged. Document Indirect as passthrough (no anonymization).
```
- Test to add: fake GetInfo with `ep` true and false, crossed with Enterprise and Indirect. Assert captured `EnterpriseAttestation` is 1 or null, and that key 10 is encoded only when set.
- E1 escalation risk: stop if platform-managed mode (2) or an RP allow-list becomes necessary (a new public configuration API). Also stop if the enterprise statement needs format changes.

### WA-IMP-004: discouraged-UV test applicability
- Rule: A2. python `fido2/client/__init__.py:657, 664 @5bc9d3a` (`alwaysUv` and `makeCredUvNotRqd` decide UV for makeCredential under Discouraged). Android `Ctap2Client.java:732 @f4626856` (`alwaysUv`). Rust `crates/yubikit/src/webauthn/client.rs:1120 @90940e9b` (`alwaysUv` with a configured UV method). All three make UV mandatory under Discouraged. The .NET test asserts the opposite unconditionally.
- Evidence pin (E2): above, plus .NET `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/WebAuthnClientTests.cs:254–256` (BEFORE, method at line 233).
- Pseudocode for the fix:
```text
info = GetInfo()
mandatoryUv = info.Options["alwaysUv"] == true
           || (UvConfigured(info) && info.Options["makeCredUvNotRqd"] != true)
if mandatoryUv: return NotApplicable for the no-UV assertion (mandatory lane runs separately)
else: Assert.False(response.AuthenticatorData.UserVerified, ...)
```
- Test to add: fake-GetInfo unit cases for the predicate. The device lane is T1 (see below).
- E1 escalation risk: stop if the mandatory-UV lane needs a PIN change or reset that has not been approved.

### WA-IMP-006: attestation borrowed-view contract
- Rule: A5. Ctap2 already splits this way: `AuthenticatorData` copies (`src/Ctap2/src/Credentials/AuthenticatorData.cs:158`), `AttestedCredentialData` copies (`:110, 116`), and `AttestationStatement.RawCbor` borrows and documents the caller-owned input (`src/Ctap2/src/Credentials/AttestationStatement.cs:24`). WebAuthn `Decode` mirrors that split and documents it (`src/WebAuthn/src/Attestation/WebAuthnAttestationObject.cs:72–75`). The client never calls `Decode` internally, only through the public API, so caller-owned input applies.
- Evidence pin (E2): .NET internal precedent `src/Ctap2/src/Credentials/AttestationStatement.cs:24` and `src/Ctap2/src/Credentials/AuthenticatorData.cs:158`. Card BEFORE at `src/WebAuthn/src/Attestation/WebAuthnAttestationObject.cs:103–104`.
- Pseudocode (no behaviour change; the documented contract stays):
```text
Decode(ReadOnlyMemory<byte> cbor):
  RawCbor = cbor                      // caller memory, documented contract
  attStmt views borrow cbor           // same as Ctap2 AttestationStatement
  authData copied                     // same as Ctap2 AuthenticatorData
Encode(): re-encodes fmt, attStmt, authData; never returns RawCbor
```
- Test to add: assert `RawCbor` is reference-equal to the input span, and that `Encode()` output is the modeled re-encoding after input mutation. This pins the documented contract.
- E1 escalation risk: stop if any SDK-owned buffer that gets zeroed or reused is ever passed to `Decode`, or if a returned object must outlive a buffer the SDK owns. Then the snapshot option is needed.

### WA-IMP-007: Aaguid default-zero semantics
- Rule: A5. CLAUDE.md type-selection rule: an immutable value of ≤16 bytes is a `readonly struct` with inline storage. Ctap2 precedent: `AttestedCredentialData.Aaguid` is a `Guid` (`src/Ctap2/src/Credentials/AttestedCredentialData.cs:51`), whose default is 16 zero bytes with reflexive equality.
- Evidence pin (E2): .NET internal precedent `src/Ctap2/src/Credentials/AttestedCredentialData.cs:51` and root `CLAUDE.md` lines 175–190. Card BEFORE at `src/WebAuthn/src/Cose/Aaguid.cs:65, 93–94`.
- Pseudocode for the fix:
```text
readonly struct Aaguid : IEquatable<Aaguid>
    inline 16-byte storage (no nullable field)
    AsSpan() => 16 bytes always              // default(Aaguid) => 16 zeros
    Value => Guid from bytes (all zeros => Guid.Empty)
    Equals, ==, GetHashCode over the 16 bytes  // reflexive for default
    constructors unchanged (16-byte check; Guid to big-endian)
```
- Test to add: `default(Aaguid).AsSpan().Length == 16` and all zero. `default == new Aaguid(Guid.Empty)`. Reflexive equality. Dictionary lookup and boxed equality.
- E1 escalation risk: stop if the change alters big-endian conversion (owned by CTAP-IMP-025) or needs a public signature change beyond the struct's own members.

### WA-IMP-009: PIN/UV protocol negotiation and built-in UV preflight
- Rule: A2 (T2 e candidate). All three siblings negotiate from GetInfo. python `fido2/client/__init__.py:771–777 @5bc9d3a` with `fido2/ctap2/pin.py:221` (`PROTOCOLS = [V2, V1]`). Android `Ctap2Client.java:187, 806–815 @f4626856` (takes the device's listed order, not V2 first). Rust `crates/yubikit/src/webauthn/client.rs:829–832, 1034–1037 @90940e9b` (V2 first). Built-in UV is gated on `pinUvAuthToken` in python (`fido2/client/__init__.py:683`), Android (`Ctap2Client.java:756`) and Rust (`client.rs:1045–1046`). The existing `PinUvAuthProtocolV1` exists in .NET (`src/Ctap2/src/Pin/PinUvAuthProtocolV1.cs`), so no new protocol code is needed.
- Evidence pin (E2): above. Card BEFORE at `src/WebAuthn/src/Client/WebAuthnBackend.cs:287–289`. Module `CLAUDE.md` lines 275–278.
- Pseudocode for the fix:
```text
protocol = info.PinUvAuthProtocols.Contains(2) ? V2
         : info.PinUvAuthProtocols.Contains(1) ? V1 : null
if protocol is null and the ceremony needs PIN, PRF or token: NotSupported (before any PIN prompt)
backend.Protocol = protocol              // cached per backend; used by token, PRF agreement, legacy fallback
builtInUvToken = info.Options["uv"] && info.Options["pinUvAuthToken"]
if !builtInUvToken: use the existing UvDecision path (uv option or PIN); never a token
```
- Test to add: fake GetInfo for V1 only, V2 only, both and neither, crossed with PIN and built-in UV, with `pinUvAuthToken` true and false. PRF key agreement with V1.
- E1 escalation risk: stop if V1 needs changes to `PinUvAuthProtocolV1` beyond selection, or if a device advertises neither protocol.

### WA-IMP-018: minPinLength honors Requested
- Rule: A2. python `fido2/ctap2/extensions.py:550 @5bc9d3a` (requests only when `is True`). Rust `crates/yubikit/src/webauthn/extensions/min_pin_length.rs:70 @90940e9b` (no request unless `Some(true)`). Android `fido/.../extensions/MinPinLengthExtension.java:61–66 @f4626856` (sends the caller's boolean, true only when requested). Consensus: a false or absent request does not ask for the value.
- Evidence pin (E2): above. .NET BEFORE `src/WebAuthn/src/Extensions/Processing/RegistrationExtensionSession.cs:66`; doc `src/WebAuthn/src/Extensions/Processing/Processors/MinPinLengthProcessor.cs:24–27`; input doc `src/WebAuthn/src/Extensions/WebAuthnExtensionInputs.cs:29–34`. `MinPinLengthInput.Requested` exists in `src/Ctap2/src/Extensions/MinPinLength/MinPinLengthInput.cs:37–43`.
- Pseudocode for the fix:
```text
if inputs.MinPinLength is { Requested: true } && context.Supports(MinPinLength)
    processors.Add(new MinPinLengthProcessor())
// null or Requested false: no processor, no extension entry. Carrier type unchanged.
```
- Test to add: outgoing extension map for null, Requested false and Requested true, crossed with supported and unsupported. An unauthorized RP keeps the output absent.
- E1 escalation risk: stop if the carrier type must change (presence-only API). That is a public API decision.

### WA-IMP-019: previewSign empty generated handle
- Rule: A2. python passes `key_handle` through with no emptiness check (`fido2/ctap2/extensions.py:773–810 @5bc9d3a`, line 796). Android accepts any `keyHandle` (`fido/.../extensions/SignExtension.java:103–121, 258 @f4626856`). Rust passes `key_handle` bytes through (`crates/yubikit/src/webauthn/extensions/sign.rs:84, 113, 181 @90940e9b`). Consensus: an empty handle is not rejected. Draft v4 permits an empty generated handle (per the .NET doc, not re-read here).
- Evidence pin (E2): above. .NET BEFORE `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignSigningParams.cs:78–83`.
- Pseudocode for the fix:
```text
ctor(keyHandle, tbs, additionalArgs):
    // remove: if keyHandle.Length == 0 throw
    keep: tbs non-empty rule (unchanged, flagged; no sibling enforces it either)
    keep: additionalArgs null versus present
    doc: "empty generated handle permitted by draft v4"
```
- Test to add: round-trip an empty and a non-empty handle through the codec and backend request. Empty `tbs` is still rejected. Null and empty `additionalArgs` stay distinct.
- E1 escalation risk: stop if the change needs algorithm-specific `tbs` or `additionalArgs` validation, or ARKG helper changes. Those are experimental (CTAP-IMP-029 and -039 owners).

### WA-IMP-017: optional direct largeBlob and pinless storage writes
- Rule: A6. The card's default is retention: storage-only, token-required writes. That is a fail-closed contract, and the direct route and the CTAP §6.10.2 pinless exception are feature additions with no assigned defect. Siblings differ: python requires the token for writes (`fido2/ctap2/extensions.py:456–458, 467–470 @5bc9d3a`), while Android attempts a write even without a token (`LargeBlobExtension.java:112–122, 166–167 @f4626856`). Both are the device's call, so no defect is assigned.
- Evidence pin: .NET `src/WebAuthn/src/Extensions/Processing/Processors/LargeBlobProcessor.cs:49–50, 98–104`. Module `CLAUDE.md` lines 439–446.
- Pseudocode (documentation only):
```text
LargeBlobProcessor remarks and README:
  "storage route only (largeBlobKey); writes require a LargeBlobWrite token;
   CTAP pinless exception not implemented; direct largeBlob extension not implemented"
```
- Test to add: none required.
- E1 escalation risk: stop if Dennis wants the direct route or pinless writes. That is a new card with hardware qualification.

## T1 hardware qualification
| Card | What needs a device | Device/firmware/transport/OS |
| --- | --- | --- |
| Ceremony/platform qualification | Human-coordinated registration and authentication with UP and UV, PIN prompt, retries, decline and cancel. Allow-list and discoverable selection. PRF, credProtect, blob and previewSign flows. Timeout, cancel, removal. | Approved serials per master (9681620 two-key discovery failure; 125 pre-auth and reset `6A88`; F9 material deferred). HID, SmartCard and NFC where available. Windows successor, macOS arm64 JIT (reported, not run), AOT. No PIN normalization on a real user's key. |
| WA-IMP-004 (lane) | Mandatory-UV case on an `alwaysUv` device. No-UV assertion on a non-`alwaysUv` device. | Firmware with `alwaysUv`, approved PIN state, UP touch. |
| WA-IMP-009 (lane) | V1-only and V2-only `pinUvAuthProtocols`, and a device with `pinUvAuthToken` false. | Older firmware if available; otherwise fake GetInfo only. |
| WA-IMP-010 (lane) | CTAP 2.0 authenticator that omits the descriptor for a singleton allow list. | Firmware-dependent; unknown which device, if any. |
| WA-IMP-018 (lane, optional) | minPinLength with an RP in the minimum-PIN RP list. | Firmware with minPinLength; RP list set by an authorized operator. |
| WA-IMP-019, 020, 021 (lane) | previewSign draft v4 generated key and signature. Experimental. | Firmware with previewSign; approval for PIN and UP. Assertion existence is not firmware qualification. |

## T2 cross-cutting candidates
| Theme | Card(s) here | Why it is cross-cutting |
| --- | --- | --- |
| a. Whole-ceremony lifetime, admission and shared disposal | Whole-ceremony lifetime (CORE-B5-C006 and CORE-WA-X002) | Same disposal-before-continuation gap in every multi-step workflow. OATH shared workflow owner (master, linked; not re-read here) shares it. Decide once. |
| b. Timeout and cancel semantics (range, correction, success after expiry, drain) | WA-IMP-003 (CORE-WA-X005 to Core B5-C002, B5-C003, B4-C001, B6-C003) | The same cooperative timer and native-recovery question appears in other ceremonies. |
| c. Error mapping and optional-output failure contract | WA-IMP-002, WA-IMP-016 | Same policy question: stage-aware mapping, and whether an optional output failure fails the operation or is omitted. |
| d. Ownership, zeroing and test oracles for retained secrets | WA-IMP-013, WA-IMP-006, WA-IMP-008 (test-only) (CTAP-IMP-004, -027, -035; Core B6-C002, B6-C006, B6-C007, B5-C005) | Same producer-copy and owner-visibility problem. Decide owner types once. The CTAP and Core items are canonical elsewhere. |
| e. PIN/UV protocol negotiation and PIN reader profile | WA-IMP-009 (CTAP-IMP-006, CTAP-XREF-003; Core C-C001 to C-C004) | Same protocol selection in Ctap2 `ClientPin`, plus reader and profile rules. |
| Referrals (not new T2 decisions) | CTAP-DEC-002 (native selected-user disclosure); CORE-WA-X004 (notification outcome to CTAP-X020); CTAP-IMP-029 and -039 (previewSign draft profile) | Owned by the CTAP section. Cited here only. |

## Coverage check
- Review map (master lines 526–550) has 23 rows. Each appears once in the Index: WA-IMP-001 through WA-IMP-021, "Whole-ceremony lifetime", and "Ceremony/platform qualification". Confirmed.
- Deliberately not treated as cards:
  - WA-XREF-001 and CTAP-XREF-001: excluded, resolved documentation.
  - CORE-WA-X001 to CORE-WA-X012: closure referrals. Each maps to an Index row or a T2 theme.
  - CTAP-DEC-002, CTAP-IMP-026, G-011: CTAP canonical decision, referred once.
  - CTAP-IMP-004, -006, -025, -027, -029, -030, -035, -039 and CTAP-XREF-003: canonical CTAP dependencies.
  - `audit-v2-20261022-20261031:#4` and YESDK-1612: folded into WA-IMP-017 via CTAP-IMP-030.
  - Core logging FULL-032 and SAMPLE-WA-002: excluded, retracted original claim.
  - Windows B4-C001 and B4-C007: partial supersession, not a WebAuthn card.
  - Original CORE-001 and protected OATH and PIV facets: linked qualification only.
  - DOC-002 migration YAML failure: outside WebAuthn scope.
  - Python P0 to P2 labels in the master: context only. This pass inspected python-fido2 directly.
- Changes from the master and batch 1, each with evidence:
  - WA-IMP-001: the master says no enterprise field exists in non-test source. That is wrong for Ctap2 (`MakeCredentialOptions.cs:113`, `Ctap2SessionRequestEncoding.cs:74–76`). Scope is the WebAuthn request and backend only. Batch-1 A3 is now A2 (Rust evidence). Disposition unchanged.
  - WA-IMP-003: batch-1 A4 ("stricter range check") is withdrawn. L3 §5.1.3 and §5.1.4 say to correct, not reject. Moved to interview.
  - WA-IMP-005, WA-IMP-010, WA-IMP-015, WA-IMP-016: the master's "no-brainer" or clean disposition is overridden by spec gates G2 or G3.
  - WA-IMP-009: python-fido2 negotiation exists. The master's P1 label did not inspect it.
- Spec pins (fetched 2026-11-14, text searched, not saved): WebAuthn L3 REC 2026-08-25 (`https://www.w3.org/TR/2026/REC-webauthn-3-20260825/`) §5.1, §5.1.3, §5.1.4, §5.4.7, §5.8.1.1, §10.1.4, §10.1.5, §15.1. CTAP 2.3-ps-20260226 §12.1. CTAP §12.3 and §6.2.2 are cited from the .NET docs and not re-verified.
- Abbreviations: UV is user verification; UP is user presence; PIN is personal identification number; RK is resident key; RP is relying party; PRF is pseudo-random function; TBS is to-be-signed data; AAGUID is authenticator attestation GUID; ARKG is a previewSign key-derivation scheme; CTAP is Client to Authenticator Protocol; G, A, T, E gates and rules are defined in pass-2-review-policy.md.
