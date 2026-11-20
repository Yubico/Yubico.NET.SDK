# CTAP2: Pass 2 classification
Classifier: one dedicated subagent, 2026-11-14. Source: master lines 2340–3930 at 52d34ca65. Policy: pass-2-review-policy.md (approved). Final per-card calls: `../pass-2-review-ledger.md`.
Sibling pins: python-fido2 @ 5bc9d3a1c; yubikit-android @ f46268563; yubikey-manager-rust-auto @ 90940e9bb. yubikey-manager @ 4ca60f706 was not read in this pass; the master's citations to it are carried unverified.

## Summary
| Disposition | Count | Cards |
| --- | --- | --- |
| A1 | 5 | CTAP-IMP-005, 024, 025, 035, 045 |
| A2 | 3 | CTAP-IMP-011, 014, 039 |
| A3 | 3 | CTAP-IMP-013, 031 (A4 for empty and oversize), 033 |
| A4 | 1 | CTAP-IMP-023 |
| A5 | 2 (historical) | CTAP-IMP-018, 027 (027 proposal superseded by AG-26) |
| A6 | 1 | CTAP-IMP-044 |
| Interview (gated) | 33 | G1: 009, 012, 029, 048. G2: 006, 007, 036. G3: 001, 008, 010, 019, 030, 040, 043. G5: 016, 047. No matching rule (no gate): 002, 003, 004, 015, 020, 021, 026, 028, 032, 034, 037, 038, 041, 042, 046, X020, DEC-002 |
| T1 | 2 | CTAP-IMP-022, CTAP-DEC-003 |
| T2-tag count | 24 | See T2 table; tags are on the Index rows |

Notes:
- The 17 "no matching rule" cards went to interview because the policy has no auto rule for them. Any of them can move to auto with one approval.
- The master's review map (lines 2364–2371) is lane-level and has no per-card severity column. Severity and no-brainer values come from each card body.
- Spec basis: CTAP 2.1 errata (2022-06-21), the master's pin, fetched and searched in this pass. CTAP 2.2 and 2.3 were not checked. Cards that depend on a 2.2 or 2.3 key say so.
- The master's "P0: not installed or inspected" disposition was re-evaluated against python-fido2 @ 5bc9d3a1c, which this pass read directly.

## Index
| Card | Severity | Disposition | One-line rationale |
| --- | --- | --- | --- |
| CTAP-IMP-001 | Medium | Interview (G3) | Silent AAGUID and Versions defaults are shared by python-fido2 and Rust; absent encrypted fields already match siblings. T2. |
| CTAP-IMP-002 | Medium | Interview (no rule) | No sibling gates USB SmartCard by firmware; sentinel policy is product-owned. T2 candidate: firmware gating. |
| CTAP-IMP-003 | Medium | Interview (no rule) | Device maxMsgSize send check is A3-settled; schema depth and raw-route responsibility have no sibling precedent. |
| CTAP-IMP-004 | Medium | Interview (no rule) | Builder reuse after Build is an API choice; clearing follows the module's finally pattern. T2. |
| CTAP-IMP-005 | Medium | A1 | No-brainer yes. Clear owned V1 and V2 buffers on exceptional exits, as Android V2 and the PIV PIN protocol do. |
| CTAP-IMP-006 | Medium | Interview (G2) | CTAP 2.1 §6.5.1 sets a 4-code-point minimum, NFC, and 63 bytes. Rust counts bytes and Android counts UTF-16 units. T2. |
| CTAP-IMP-007 | Low | Interview (G2, G3) | §6.5.4 requires the peer key as getPublicKey specifies; all three siblings skip the kty, alg and crv checks. |
| CTAP-IMP-008 | Medium | Interview (G3) | Absent-count fail-closed is settled by siblings; signed and negative counts are accepted by all three. T2. |
| CTAP-IMP-009 | High | Interview (G1) | High severity. Length validation after decrypt is settled by python-fido2 and Rust; Dennis reviews High. T2. |
| CTAP-IMP-010 | Medium | Interview (G3) | All three siblings silently drop permissions and RP on the legacy command; the overestimation risk is shared. T2. |
| CTAP-IMP-011 | Medium | A2 | No-brainer yes. Spec §6.1.1 and §6.2.1 say platforms MUST NOT send uv with pinUvAuthParam; all clients omit it. |
| CTAP-IMP-012 | High | Interview (G1) | High severity. Standard subcommand numbers and transcript match siblings; no physical enrollment success exists. T2. |
| CTAP-IMP-013 | Medium | A3 | Python-fido2 precedent; Android and Rust agree. Decode templateInfos; InvalidOption means empty; other statuses propagate. T2. |
| CTAP-IMP-014 | Medium | A2 | Begin and next results split with the begin template ID carried; required fields per operation; completion at remaining 0. |
| CTAP-IMP-015 | Medium | Interview (no rule) | Spec status names 0x0B, 0x0D and 0x0E are settled. The unverified maxTemplateCount vendor field needs a profile decision. |
| CTAP-IMP-016 | Low | Interview (G5) | Read alwaysUv before toggling is a safety gain no sibling has. Explicit-false representation splits siblings. T2. |
| CTAP-IMP-018 | Low | A5 (T2) | Reject use after Dispose with the ThrowIfDisposed pattern already used in Ctap2Session and Oath. Borrowed owners untouched. |
| CTAP-IMP-019 | Medium | Interview (G3) | Hash-width and sequence guards are missing in every sibling; missing totals are handled four different ways. T2. |
| CTAP-IMP-020 | Medium | Interview (no rule) | Default normalizePin in read-only test lanes is a test-policy decision with no sibling precedent. T2. |
| CTAP-IMP-021 | Medium | Interview (no rule) | Restoration-failure policy is undecided. Local clearing follows the A1 pattern of CTAP-IMP-035. T2. |
| CTAP-IMP-022 | none assigned | T1 | Test-oracle qualification. Integration oracles need a device; fake crypto and CBOR vectors do not. T2. |
| CTAP-IMP-023 | Medium | A4 (T2) | Siblings split. Stricter option: map only 6D00, 6A82 and 6E00 to not-supported (Android); reject empty responses (Rust). |
| CTAP-IMP-024 | Medium | A1 | No-brainer yes. Finally-clear named admin temporaries; Android changePin does this; borrowed token untouched. |
| CTAP-IMP-025 | Low | A1 | No-brainer yes. AAGUID conversion must be unconditional; siblings treat AAGUID as raw RFC-order bytes. |
| CTAP-IMP-026 | Low | Interview (no rule) | Siblings split on name optionality (python optional, Android required). Overlaps the DEC-002 privacy decision. |
| CTAP-IMP-027 | Medium | A5 (T2; superseded) | Original proposal: make response owners disposable. [AG-26](../pass-2-review-decisions.md#ag-26-ctap-imp-027-ownership) applies X3/R1 instead. |
| CTAP-IMP-028 | Medium | Interview (no rule) | Unsigned count width is settled. ED-suffix and AT conditionals are a request-aware decision. |
| CTAP-IMP-029 | High | Interview (G1) | High severity. Only python-fido2 implements ARKG and it assumes P-256 without reading crv. Experimental draft. T2. |
| CTAP-IMP-030 | Medium | Interview (G3) | No sibling exposes GetAssertion key 0x08. The 2.1 errata text searched here runs GetAssertion response keys to 0x07 and has no key 0x08 or unsigned-output field; check CTAP 2.2 first. |
| CTAP-IMP-031 | Low | A3 (A4 for empty and oversize) | Device-advertised credBlob capacity is shared by python-fido2 and Android; Rust errors on oversize; empty rejected. |
| CTAP-IMP-032 | Medium | Interview (no rule) | Registration output should be supported, per python-fido2 and Rust. Removing the synthetic convenience shape is a choice. |
| CTAP-IMP-033 | Low | A3 | Ceremony-specific hmac-secret outputs in python-fido2 and Android: registration bool, assertion bytes. |
| CTAP-IMP-034 | Medium | Interview (no rule) | Preserving encoded values is A1-shaped; duplicate and malformed key policy is undecided. T2. |
| CTAP-IMP-035 | Medium | A1 | No-brainer yes. Finally-clear untransferred temporaries in the extension builder and membership probe. |
| CTAP-IMP-036 | Medium | Interview (G2, G3) | Rust errors on hash mismatch, but CTAP 2.1 §6.10 says a corrupt array acts as initial. All three loop without bound. T2. |
| CTAP-IMP-037 | Medium | Interview (no rule) | uint64 origSize is A3-settled. The 1 MiB asymmetric cap is policy; §6.10.4 says cap at least 1 MiB; siblings do not cap. |
| CTAP-IMP-038 | Medium | Interview (no rule) | Siblings split (python and Android throw, Rust omits). Throwing would break the Try-style public contract. |
| CTAP-IMP-039 | Medium | A2 | Python-fido2, Rust and Android take the derived algorithm from device output. No sibling uses the seed marker. |
| CTAP-IMP-040 | Medium | Interview (G3) | additionalArgs and TBS passed raw by python-fido2 and Rust; construction policy is shared. Experimental. |
| CTAP-IMP-041 | Medium | Interview (no rule) | No sibling DER parser for this converter. The raw-message versus prehash contract is undecided. Experimental. |
| CTAP-IMP-042 | Medium | Interview (no rule) | Remove the env-var key export or keep a human-controlled evidence channel. No sibling precedent. T2. |
| CTAP-IMP-043 | Medium | Interview (G3) | Python-fido2 and Rust both accept IV-only encrypted GetInfo fields. Strict wrapper versus generic CBC is undecided. |
| CTAP-IMP-044 | none assigned | A6 | The protocol-demo boundary is already documented in the FidoTool README and MakeCredential.cs. Keep and document. |
| CTAP-IMP-045 | Low | A1 | No-brainer yes. Missing flag values and unknown verbs fail; help runs before device selection; unknown sensor kind is not renamed. |
| CTAP-IMP-046 | Medium | Interview (no rule) | Presence-based bioEnroll support is settled by spec and siblings. Selector and non-interactive target policy is a product choice. |
| CTAP-IMP-047 | Medium | Interview (G5, no rule) | Console handler lifetime after CTS disposal is a concrete gain. The cancellation result contract is undecided. T2. |
| CTAP-IMP-048 | High | Interview (G1) | High severity. Reset does not bind the confirmed key across reinsertion; Rust keeps the same device object. T2. |
| CTAP-DEC-002 | none assigned | Interview (no rule) | Native selected-user disclosure is an actor and privacy decision. No severity assigned. |
| CTAP-DEC-003 | none assigned | T1 | Product, firmware and profile qualification needs physical keys. |
| CORE-CTAP-X020 | Medium | Interview (no rule) | Clearing is A5-shaped. Exception and logging reporting is Core-owned and unselected. T2. |

## Interview queue (sorted High, then Medium, then Low)

### CTAP-IMP-009: Token response shape and crypto evidence
- Gate(s): G1 (High severity). Length validation after decrypt is settled by python-fido2 and Rust (A3-shaped), but High severity needs Dennis.
- Severity / certainty: High, potential impact. Source-confirmed shape gap; no bypass shown.
- Decision: Should the SDK validate the decrypted PIN/UV token length (16 or 32 for V1, 32 for V2) before returning it, and clear rejected plaintext?
- Options:
  - (a) Validate, and clear rejected plaintext before throwing. Sia's lean: yes. python-fido2 and Rust both reject wrong lengths; Android returns the plaintext unchecked.
  - (b) Keep current behaviour and document it. Malformed authentication material then reaches callers.
- Evidence: .NET `src/Ctap2/src/Pin/ClientPin.cs:349–350, 449–450, 526–527`. python-fido2 `fido2/ctap2/pin.py:146–149, 206–209, 323–327 @5bc9d3a1c`. Rust `crates/yubikit/src/ctap2/client_pin.rs:390–396, 429–435 @90940e9bb` (rejected Vec not zeroed). Android `fido/.../ctap/ClientPin.java:189, 248 @f46268563` (no check).

### CTAP-IMP-012: Standard biometric request wire
- Gate(s): G1 (High severity). Siblings agree on the standard numbering and transcript, but High severity applies and no physical enrollment success exists.
- Severity / certainty: High, source-confirmed wrong operation number. Physical success not established. Spec numbering not re-verified in this pass.
- Decision: Adopt the standard bio subcommand numbers (1 to 7), the modality-prefixed authenticated transcript and timeout in parameter key 3 now, leaving preview routing separate?
- Options:
  - (a) Fix the standard path now. Sia's lean: yes. All three siblings use 1 to 7 and key 3; python-fido2 authenticates modality || subCommand || params.
  - (b) Hold until a device enrollment test runs. Delays a deterministic fix.
  - (c) Keep current. Rejected: EnrollBegin = 0x02 sends the wrong operation.
- Evidence: .NET `src/Ctap2/src/BioEnrollment/BioEnrollmentSubCommand.cs:37`. python-fido2 `fido2/ctap2/bio.py:165–171, 205–221, 223–228 @5bc9d3a1c`. Android `fido/.../ctap/FingerprintBioEnrollment.java:42–48, 61–63 @f46268563`. Rust `crates/yubikit/src/ctap2/bio_enrollment.rs:29–42, 65–70 @90940e9bb`.

### CTAP-IMP-029: ARKG-P256 seed key validation and representation
- Gate(s): G1 (High severity). Only python-fido2 implements ARKG among the siblings; it fixes P-256 and does not read the crv label.
- Severity / certainty: High, unqualified key material as crypto input. Experimental draft; the master records draft-10's stated expiry as 2026-08-31.
- Decision: Which ARKG profile target does the SDK pin, and should non-conforming seed keys be rejected now?
- Options:
  - (a) Pin the current carrier. Validate nested curve 1, 32-byte coordinates and on-curve points before derivation; keep the experimental label. Sia's lean: yes. Blocks unqualified input without choosing draft conformance.
  - (b) Remove the ARKG convenience until a profile is pinned. Safest, but removes public surface (alpha permits it).
  - (c) Keep with the experimental warning only. Rejected: accepts unvalidated curve and points.
- Evidence: .NET `src/Ctap2/src/Cose/CoseArkgP256SeedKey.cs:101–105`; `src/Ctap2/src/PublicAPI.Unshipped.txt:146–149`. python-fido2 `fido2/cose.py:389–391, 394–464 @5bc9d3a1c` (fixed P-256; placeholder -65700 at 419). Note: `fido2/arkg.py:51–52` cites draft-08 while `cose.py:390` cites draft-10. Android and Rust: no ARKG implementation.

### CTAP-IMP-048: Destructive reset target continuity
- Gate(s): G1 (High severity). Destructive action. Siblings keep the same device object across reinsertion; the .NET flow re-selects independently.
- Severity / certainty: High, another key's credentials or PIN could be reset. Source-confirmed; no live reset run.
- Decision: How should reset bind to the device confirmed in preflight across reinsertion, and what happens when serial is absent?
- Options:
  - (a) Retain the selected device object through reinsertion. When serial is absent, verify identity with a GetInfo fingerprint and document that fingerprint equality is not unique identity. Sia's lean: yes, matching ykman-cli's retained `dev`.
  - (b) Refuse reset for keys without a serial. Stronger; excludes some devices.
  - (c) Keep independent selection. Rejected.
- Evidence: .NET `src/Ctap2/examples/FidoTool/Cli/Menus/ResetMenu.cs:98–100`. Rust `crates/ykman-cli/src/fido.rs:648, 684–702 @90940e9bb` (same `dev` reinserted). python yubikey-manager `ykman/_cli/fido.py:225–350 @4ca60f706` (master citation; not re-read this pass).

### CTAP-IMP-001: GetInfo validity and absent encrypted fields
- Gate(s): G3. python-fido2 and Rust default a missing AAGUID to zeros; Android returns null. The silent default is the defect the card targets, so precedent does not decide it.
- Severity / certainty: Medium. Source-confirmed; no device exploit established.
- Decision: Should GetInfo reject a missing or wrong-length AAGUID and missing Versions, or keep the silent defaults?
- Options:
  - (a) Reject. Sia's lean: yes. AAGUID and Versions identify and describe the device; a zero AAGUID misidentifies it. Absent encrypted fields stay absent (A2-settled); unknown fields are preserved.
  - (b) Keep the defaults and document them. Matches python-fido2 and Rust, but reports an identity the device did not send.
- Evidence: .NET `src/Ctap2/src/AuthenticatorInfo.cs:528–530, 556`. python-fido2 `fido2/ctap2/base.py:87–89, 111, 116 @5bc9d3a1c` (Versions required; AAGUID defaults to zero), `fido2/webauthn.py:64–67`, `fido2/utils.py:183–198`. Rust `crates/yubikit/src/ctap2/mod.rs:526–529, 587 @90940e9bb`. Android `fido/.../ctap/Ctap2Session.java:763, 809 @f46268563` (null when absent).

### CTAP-IMP-002: Initialization version and firmware policy
- Gate(s): none. No sibling gates USB SmartCard by firmware, so there is no precedent; sentinel handling is a product rule. T2 candidate.
- Severity / certainty: Medium. Source-confirmed policy gap; no product test.
- Decision: For the USB SmartCard firmware gate, what replaces the optimistic treatment of a 0.x sentinel?
- Options:
  - (a) Use Management-observed firmware when GetInfo reports a 0.x sentinel. Fail closed when neither source knows firmware. Sia's lean: yes. CLAUDE.md says sentinel firmware is not verified support.
  - (b) Keep the optimistic policy and document it.
  - (c) Fail closed on any sentinel. Blocks devices whose real firmware is 5.8.0 or later.
- Evidence: .NET `src/Ctap2/src/Ctap2Session.cs:160–165`; `src/Ctap2/CLAUDE.md:44–46`. python-fido2 `fido2/ctap2/base.py:100 @5bc9d3a1c` (int, default 0; no gate). Android `Ctap2Session.java:786 @f46268563` (nullable). Rust `mod.rs:576 @90940e9bb` (Option).

### CTAP-IMP-003: Typed request limits versus raw responsibility
- Gate(s): none. Enforcing device maxMsgSize on every send matches python-fido2 and Android (A3-settled). Depth and schema checks have no sibling precedent.
- Severity / certainty: Medium. Source-confirmed absence.
- Decision: Should typed and raw CBOR sends enforce device maxMsgSize, with schema depth left to callers?
- Options:
  - (a) Enforce maxMsgSize on every send from cached GetInfo. Document depth and schema as caller responsibility. Sia's lean: yes.
  - (b) Also enforce depth and schema in the typed layer. No sibling precedent; more code.
  - (c) Keep current. Rejected: typed callers can build requests the device refuses.
- Evidence: .NET `src/Ctap2/src/Cbor/Ctap2SessionRequestEncoding.cs:79`. python-fido2 `fido2/ctap2/base.py:283–284 @5bc9d3a1c`. Android `fido/.../ctap/Ctap2Session.java:203–207 @f46268563`. Rust `crates/yubikit/src/ctap2/session.rs:85–97 @90940e9bb` (no check).

### CTAP-IMP-004: Builder clones and raw buffer lifetime
- Gate(s): none. No sibling builder object exists to compare. Reuse-after-Build is an API choice.
- Severity / certainty: Medium. Source-confirmed; no end-to-end exploit.
- Decision: May a request builder be reused after Build(), and when are WithBytes clones cleared?
- Options:
  - (a) Single-use builder. Clear owned clones after the awaited send and on failure; reuse throws. Sia's lean: yes, matching the module's own finally pattern.
  - (b) Reusable builder with clearing on Dispose. Needs a disposable builder and callers that dispose it.
  - (c) Keep current. Clones persist until garbage collection.
- Evidence: .NET `src/Ctap2/src/Cbor/CtapRequestBuilder.cs:91–93, 104`. Internal precedent `src/Oath/src/OathSession.cs:392, 450` (finally ZeroMemory). Siblings: no builder object (python-fido2 dict; Rust `build_args_map`).

### CTAP-IMP-006: PIN units and input enforcement
- Gate(s): G2. CTAP 2.1 §6.5.1 requires platform NFC normalization, a 63-byte UTF-8 maximum, no trailing NUL, and a 4-code-point minimum (authenticator side). Rust counts bytes and Android counts UTF-16 units for the minimum, so that precedent contradicts the spec. No sibling normalizes.
- Severity / certainty: Medium. Source-confirmed.
- Decision: Adopt §6.5.1 for new PINs (4 code points, 63 bytes, NFC, no trailing NUL), and validate the current PIN only for encodability?
- Options:
  - (a) Spec rules for the new PIN; current PIN checked only for encodability. Sia's lean: yes. Avoids locking out existing PINs; the device decides verification.
  - (b) Same rules applied to the current PIN. Rejects legacy PINs client-side. Not recommended.
  - (c) Keep the byte minimum. Contradicts §6.5.1.
- Evidence: .NET `src/Ctap2/src/Pin/ClientPin.cs:573–576`. Spec §6.5.1. python-fido2 `fido2/ctap2/pin.py:51–60 @5bc9d3a1c` (code-point minimum; no 63-byte cap; no NFC). Rust `crates/yubikit/src/ctap2/client_pin.rs:47–59 @90940e9bb` (byte minimum; no 63-byte cap). Android `fido/.../ctap/ClientPin.java:52–54, 399–417 @f46268563` (UTF-16 units; 63-byte cap).

### CTAP-IMP-008: Retry absence, ranges and UV fields
- Gate(s): G3 for range handling. Absent-count fail-closed is settled: python-fido2, Rust and Android all error on a missing required count, and the UV response reads only key 0x05 in all three. Signed and negative counts are accepted by all three.
- Severity / certainty: Medium. Source-confirmed.
- Decision: Reject negative retry counts and fail on a missing required count?
- Options:
  - (a) Error on an absent count; reject negatives; read only the UV count key. Sia's lean: yes.
  - (b) Keep zero defaults and signed reads. A missing count can look like exhaustion.
- Evidence: .NET `src/Ctap2/src/Pin/ClientPin.cs:614–615, 645`. python-fido2 `fido2/ctap2/pin.py:368–389 @5bc9d3a1c`. Rust `crates/yubikit/src/ctap2/client_pin.rs:240–285 @90940e9bb` (`as u32` at 257, 283 wraps negatives). Android `fido/.../ctap/ClientPin.java:263–288 @f46268563` (Integer, no range check).

### CTAP-IMP-010: Token capability, permission, RP and persistence policy
- Gate(s): G3. All three siblings silently drop requested permissions and RP when pinUvAuthToken is absent. The card's overestimation risk is shared, so precedent does not decide it. The FidoTool README documents the same drop.
- Severity / certainty: Medium. Source-confirmed.
- Decision: On the legacy path, reject requested permissions or RP, or report them as not applied?
- Options:
  - (a) Reject requested permissions or RP on the legacy path unless the caller opts into fallback. Sia's lean: yes. Callers cannot overestimate authorization.
  - (b) Keep the silent fallback and expose a flag that scope was not applied. Less breaking; callers must check.
  - (c) Keep and document. Matches all siblings and the README; keeps the risk the card names.
- Evidence: .NET `src/Ctap2/src/Pin/ClientPin.cs:410–415`. python-fido2 `fido2/ctap2/pin.py:307–313 @5bc9d3a1c`. Rust `crates/yubikit/src/ctap2/client_pin.rs:362–371 @90940e9bb`. Android `fido/.../ctap/ClientPin.java:168–182 @f46268563`. FidoTool README `src/Ctap2/examples/FidoTool/README.md:91–93`.

### CTAP-IMP-015: Biometric status names and vendor fields
- Gate(s): none as a whole. Status names 0x0B, 0x0D and 0x0E are settled by the spec and by python-fido2 and Android. The maxTemplateCount vendor field and the 0x0C name need a profile decision.
- Severity / certainty: Medium. Source-confirmed standard mismatch; vendor provenance unresolved.
- Decision: Keep maxTemplateCount only in raw data, and leave 0x0C unnamed?
- Options:
  - (a) Adopt the spec names. Keep maxTemplateCount only in raw data. Leave 0x0C unnamed. Sia's lean: yes. python-fido2 names 0x0C "database full"; the spec calls it unused, so that name is not adopted.
  - (b) Keep the vendor field with a vendor-defined doc. Needs a vendor source the repo does not hold.
- Evidence: .NET `src/Ctap2/src/BioEnrollment/BioEnrollmentModels.cs:99, 105, 111`. Spec §6.7 status table and sensor-info keys (0x02, 0x03, 0x08). Android `fido/.../ctap/FingerprintBioEnrollment.java:75–79 @f46268563`. python-fido2 `fido2/ctap2/bio.py:180–195 @5bc9d3a1c`.

### CTAP-IMP-019: Administrative counts, hashes and sequences
- Gate(s): G3. No sibling guards the begin and next sequence, and none checks RP-hash width (Rust not verified on width this pass). Totals are handled four ways: python-fido2 RP path fails, python-fido2 credential path defaults to one, Rust returns empty, and Android throws.
- Severity / certainty: Medium. Source-confirmed gaps.
- Decision: On missing, zero or negative totals and non-32-byte RP hashes, should enumeration fail? Who owns the begin and next sequence?
- Options:
  - (a) Fail with InvalidResponse on missing or negative totals and non-32-byte hashes. Document caller serialization. Sia's lean: yes; matches the python-fido2 RP path and Android.
  - (b) Keep one-entry or empty defaults. Silent partial inventory.
  - (c) Whole-sequence admission. Larger change; defer to the shared workflow decision.
- Evidence: .NET `src/Ctap2/src/CredentialManagement/CredentialManagement.cs:149–153`. python-fido2 `fido2/ctap2/credman.py:161–176, 204–222 @5bc9d3a1c`. Rust `crates/yubikit/src/ctap2/credential_management.rs:195–213, 216–237 @90940e9bb`. Android `fido/.../ctap/CredentialManagement.java:144–160, 173–190 @f46268563`.

### CTAP-IMP-020: Read-only tests with mutating setup
- Gate(s): none. Test-lane default is a policy decision; no sibling test-helper precedent was found.
- Severity / certainty: Medium. Source-confirmed helper default.
- Decision: Should WithCtap2SessionAsync default normalizePin to false, so PIN-mutating lanes opt in?
- Options:
  - (a) Default false, with explicit mutating overloads. Sia's lean: yes. A read-only test cannot change PIN state by accident.
  - (b) Keep default true and add a read-only overload. Smaller change; unmigrated call sites keep the risk.
- Evidence: .NET `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/TestExtensions/Ctap2TestStateExtensions.cs:75, 172–175, 203, 229–260`.

### CTAP-IMP-021: Test secret cleanup and state restoration
- Gate(s): none. Local owned-array clearing follows the CTAP-IMP-035 pattern (A1-shaped). The restoration-failure policy is a decision.
- Severity / certainty: Medium. Source-confirmed representative sites.
- Decision: On failed normalization or teardown, should restoration failure be reported as its own outcome, with only authorized restoration attempted?
- Options:
  - (a) Finally-clear temporaries now; report restoration failure separately; no automatic destructive restoration. Sia's lean: yes.
  - (b) Keep teardown as is. Masks primary failures and leaves token copies live.
- Evidence: .NET `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/TestExtensions/Ctap2TestStateExtensions.cs:75–80, 132 (master citation), 203, 260`.

### CTAP-IMP-028: Structural versus contextual credential decoding
- Gate(s): none as a whole. Unsigned count width is settled by python-fido2 and Rust. ED-suffix and AT conditionals belong to request-aware validation.
- Severity / certainty: Medium. Source-confirmed.
- Decision: Should standalone decoders check the ED suffix as one map and AT presence, with request-aware checks in one place?
- Options:
  - (a) Structural checks in decoders: ED as one map, count as uint32, AT present when credential data is claimed. Request-aware conditionals stay in the caller. Sia's lean: yes.
  - (b) One contextual validator that receives the request. Larger API change.
- Evidence: .NET `src/Ctap2/src/Credentials/GetAssertionResponse.cs:200–202`. python-fido2 `fido2/ctap2/base.py:184–187 @5bc9d3a1c`. Rust `crates/yubikit/src/ctap2/types.rs:164, 189–203 @90940e9bb`.

### CTAP-IMP-030: Unsigned outputs and complete raw envelope
- Gate(s): G3. No sibling exposes GetAssertion key 0x08. The 2.1 errata text searched here runs GetAssertion response keys to 0x07 (largeBlobKey) and has no key 0x08 or unsigned-output field, so the spec basis is unverified until CTAP 2.2 is checked.
- Severity / certainty: Medium. Source-confirmed omission; spec status unverified.
- Decision: Expose GetAssertion unsigned extension outputs now, or defer until CTAP 2.2 is checked?
- Options:
  - (a) Defer. Check CTAP 2.2 for key 0x08 first. Sia's lean: yes.
  - (b) Expose key 0x08 as raw unsigned outputs now. Spec basis unverified.
  - (c) Keep skipping. Valid outputs stay unavailable.
- Evidence: .NET `src/Ctap2/src/Credentials/GetAssertionResponse.cs:209–211`. python-fido2 `fido2/ctap2/base.py:181–187 @5bc9d3a1c` (no field). Rust `crates/yubikit/src/ctap2/types.rs:155–210 @90940e9bb` (no field). Android `fido/.../ctap/Ctap2Session.java:1416–1502 @f46268563` (no field).

### CTAP-IMP-032: Legacy largeBlob convenience shape
- Gate(s): none as a whole. Registration meaning (supported, not written) is A3-settled. Removing the synthetic signed-key convenience is a choice.
- Severity / certainty: Medium. Source-confirmed output meaning.
- Decision: Replace the registration boolean with Supported, and remove the synthetic convenience output?
- Options:
  - (a) Supported for registration; remove the synthetic convenience output; keep the largeBlobKey envelope separate. Sia's lean: yes, as python-fido2 and Rust report it.
  - (b) Keep both shapes as separate typed outputs. More surface, same confusion risk.
- Evidence: .NET `src/Ctap2/src/Extensions/Shared/ExtensionOutput.cs:178` (TryGetLargeBlob; Written set from the registration boolean, body at 190–194 per master). python-fido2 `fido2/ctap2/extensions.py:386–391, 419–427 @5bc9d3a1c`. Rust `crates/yubikit/src/webauthn/extensions/large_blob.rs:45–49, 181–185 @90940e9bb`.

### CTAP-IMP-034: Preserve valid raw extension values
- Gate(s): none. Preserving encoded values is A1-shaped. Duplicate-key and malformed handling is undecided.
- Severity / certainty: Medium. Source-confirmed; siblings keep decoded values.
- Decision: Preserve each complete encoded value in the reader overload, and what happens on a duplicate key?
- Options:
  - (a) Preserve encoded values; reject duplicate keys. Sia's lean: yes. Confirm the strict reader's duplicate behaviour before implementing.
  - (b) Preserve values; keep last-wins. Silent loss of the earlier value.
- Evidence: .NET `src/Ctap2/src/Extensions/Shared/ExtensionOutput.cs:288–291`. Internal precedent `src/Ctap2/src/LargeBlobs/LargeBlobData.cs:269` (unknown encoded values retained). Siblings: python-fido2 dict; Rust `Value`; Android decoded map.

### CTAP-IMP-036: Shared blob sequences and malformed fragments
- Gate(s): G2 and G3. Rust errors on a hash mismatch, but CTAP 2.1 §6.10 says a corrupt array is handled as the initial array, so that precedent contradicts the spec. All three read in unbounded loops.
- Severity / certainty: Medium. Source-confirmed.
- Decision: Keep the spec's act-as-initial on a hash mismatch? How is the fragment loop bounded?
- Options:
  - (a) Keep act-as-initial on mismatch. Throw on wrong-typed fragments. Cap iterations from maximum size over fragment length, with a documented ceiling. Sia's lean: yes.
  - (b) Throw on mismatch (Rust style). Contradicts §6.10.
  - (c) Keep unbounded loops. A misbehaving device can stall the caller.
- Evidence: .NET `src/Ctap2/src/LargeBlobs/LargeBlobStorage.cs:494–498 (ParseReadResponse; master cite 496–498), 445–447 (LE transcript)`. python-fido2 `fido2/ctap2/blob.py:117–134 @5bc9d3a1c`. Rust `crates/yubikit/src/ctap2/large_blobs.rs:89–135 @90940e9bb`. Android `fido/.../client/extensions/LargeBlobs.java:88–110 @f46268563`. Spec §6.10 (initial array value) and §6.10.2 (read and reset rules).

### CTAP-IMP-037: Blob uint ranges, preservation and asymmetric cap
- Gate(s): none as a whole. uint64 origSize is A3-settled (python-fido2 and Rust). The asymmetric read cap is a policy choice.
- Severity / certainty: Medium. Source-confirmed; no all-client interop failure shown.
- Decision: Apply a symmetric cap of at least 1 MiB to writes and reads, or raise the read cap to the device's advertised limit?
- Options:
  - (a) Full uint64 origSize. Symmetric 1 MiB cap enforced on writes too. Sia's lean: yes. Own writes stay readable; matches §6.10.4's SHOULD (cap at least 1 MiB).
  - (b) Read cap set from the advertised maxLargeBlob (GetInfo key 0x0B). Depends on trusting the advertised limit.
  - (c) Keep the asymmetry. Rejected: writes the SDK cannot read back.
- Evidence: .NET `src/Ctap2/src/LargeBlobs/LargeBlobData.cs:35, 56, 239–240, 273–277`. Spec §6.10.4 note on origSize. python-fido2 `fido2/ctap2/blob.py:54–55 @5bc9d3a1c` (uint64 AAD; no cap). Rust `crates/yubikit/src/ctap2/large_blobs.rs:277–289, 339–347 @90940e9bb` (u64; no cap). Android `fido/.../client/extensions/LargeBlobs.java:321–328 @f46268563` (Integer origSize; no cap).

### CTAP-IMP-038: previewSign extraction contract and inner flags
- Gate(s): none. A4 would make malformed output throw, which conflicts with the Try-style public contract.
- Severity / certainty: Medium. Source-confirmed; forged attestation not shown.
- Decision: On malformed present previewSign output, return null (Try contract) or throw?
- Options:
  - (a) Keep Try semantics and return null, with a distinct diagnostic. Sia's lean: yes. Keeps the public contract; matches Rust's warn-and-omit behaviour.
  - (b) Throw on malformed present output (python-fido2 and Android style). Fail closed; changes the contract.
- Evidence: .NET `src/Ctap2/src/Extensions/PreviewSign/PreviewSignCbor.cs:486–488`; `PreviewSignRegistrationOutput.cs:90`. python-fido2 `fido2/ctap2/extensions.py:743–753 @5bc9d3a1c` (no catch). Android `fido/.../client/extensions/SignExtension.java:233–267 @f46268563` (throws). Rust `crates/yubikit/src/webauthn/extensions/sign.rs:421–437 @90940e9bb` (warns and omits).

### CTAP-IMP-040: previewSign construction and operation policy
- Gate(s): G3. python-fido2 and Rust pass additionalArgs and TBS as raw bytes with no map or emptiness check. The shared gap means precedent does not decide it. Experimental.
- Severity / certainty: Medium. Source-confirmed restrictions and gaps.
- Decision: Validate additionalArgs as a single CBOR map (COSE_Sign_Args), and reject empty key handle or TBS at construction?
- Options:
  - (a) Validate the map and reject empties at construction. Sia's lean: yes. Low blast radius for an experimental surface.
  - (b) Keep raw bytes. Matches siblings; sends non-map bytes to the device.
- Evidence: .NET `src/Ctap2/src/Extensions/PreviewSign/PreviewSignSigningParams.cs:52, 57`. python-fido2 `fido2/ctap2/extensions.py:789–800 @5bc9d3a1c` (raw passthrough). Rust `crates/yubikit/src/webauthn/extensions/sign.rs:175–190 @90940e9bb` (raw additional args).

### CTAP-IMP-041: Experimental signature DER and raw-message contract
- Gate(s): none. No sibling DER parser for this converter; python-fido2 delegates to cryptography (not line-pinned). Provisional risk.
- Severity / certainty: Medium (provisional). Source-confirmed parser limits; no exploit claim.
- Decision: Strict DER (exhaustion, positive integers, bounds) with a documented raw-message P-256 and SHA-256 limit, or remove the convenience verifier?
- Options:
  - (a) Strict DER and a documented raw-message limit. Sia's lean: yes.
  - (b) Remove the experimental convenience verifier. Smaller surface.
- Evidence: .NET `src/Ctap2/src/Extensions/PreviewSign/PreviewSignDerivedKey.cs:145, 156–162`. python-fido2 verification path via `cryptography` (not line-pinned in this pass).

### CTAP-IMP-042: Optional immutable largeBlobKey export
- Gate(s): none. Removal or a human-controlled evidence channel is a policy choice. No sibling export precedent.
- Severity / certainty: Medium (confidentiality if enabled with real secrets). No export executed.
- Decision: Remove the environment-variable export, or keep a bounded, human-controlled evidence channel?
- Options:
  - (a) Remove the export. Sia's lean: yes. Immutable string copies cannot be cleared, and the existing comment already warns of this.
  - (b) Keep behind explicit opt-in with file-permission checks and independent cleanup. More code for a test-only path.
- Evidence: .NET `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/AuditV2/Ctap2LargeBlobAuditReproTests.cs:108–114`.

### CTAP-IMP-043: Encrypted GetInfo wrapper versus generic CBC helper
- Gate(s): G3. python-fido2 returns empty plaintext for an IV-only field, and Rust's helper accepts exactly 16 bytes. The shared acceptance is the card's target.
- Severity / certainty: Medium. Source-confirmed; plaintext is unauthenticated.
- Decision: Use a strict wrapper for GetInfo encrypted fields (IV plus ciphertext profile), keeping the generic CBC helper separate?
- Options:
  - (a) Strict wrapper for GetInfo fields; generic helper kept separate; plaintext treated as opaque and unauthenticated. Sia's lean: yes.
  - (b) Keep the generic helper for both. Accepts IV-only as metadata.
- Evidence: .NET `src/Ctap2/src/Crypto/EncryptedMetadataDecryptor.cs:128–132`. python-fido2 `fido2/ctap2/base.py:119–136 @5bc9d3a1c`. Rust `crates/yubikit/src/ctap2/mod.rs:620–641 @90940e9bb` (rejects under 16 bytes only).

### CTAP-IMP-046: Current selector and bio capability classification
- Gate(s): none as a whole. Presence-based bioEnroll support is settled by the spec and by python-fido2, Android and Rust. Selector and non-interactive target policy is a product choice.
- Severity / certainty: Medium. Source-confirmed.
- Decision: Adopt presence-based bio support (value means enrolled), and require an explicit device target for non-interactive use?
- Options:
  - (a) Presence means supported; value means enrolled; non-interactive use needs an explicit target. Sia's lean: yes.
  - (b) Keep auto-first selection. Can pick a SmartCard candidate with no FIDO AID.
- Evidence: .NET `src/Ctap2/examples/FidoTool/FidoExamples/BioEnrollment.cs:378–379`; `src/Ctap2/examples/FidoTool/Cli/Prompts/DeviceSelector.cs:24–25`. python-fido2 `fido2/ctap2/bio.py:66–75 @5bc9d3a1c`. Android `fido/.../ctap/Ctap2Session.java:170–179 @f46268563`. Rust `crates/yubikit/src/ctap2/bio_enrollment.rs:65–70, 93 @90940e9bb`. Spec §6.4 bioEnroll option table.

### CTAP-IMP-047: Remaining error, cancellation and console lifetime
- Gate(s): G5 for console handler lifetime. A disposed CTS currently leaves a Console handler attached for later commands in the process. No rule covers the cancellation result contract.
- Severity / certainty: Medium. Source-confirmed remaining facets.
- Decision: Detach the Ctrl+C registration when the console CTS is disposed, and surface failed IsSupported reads as errors rather than unsupported?
- Options:
  - (a) Detach on dispose; surface failed reads as errors. Sia's lean: yes. Removes stale callbacks and false "unsupported" results.
  - (b) Keep current. Handler persists for process lifetime.
- Evidence: .NET `src/Cli.Shared/src/Cli/CommandHelper.cs:27–36`; `src/Ctap2/examples/FidoTool/FidoExamples/BioEnrollment.cs:378–379`. Rust `crates/ykman-cli/src/fido.rs:737–748, 1328 @90940e9bb` (cancel module; no per-command detach shown).

### CORE-CTAP-X020: Cleanup when terminal notification logging throws
- Gate(s): none. The finally-clear of the untransferred view is A5-shaped. The primary, callback and logger exception policy is Core-owned and unselected.
- Severity / certainty: Medium. Source-confirmed conditional ordering; not reproduced.
- Decision: When a terminal callback or logger throws during a failed CTAP exchange, which exception surfaces? Clearing runs either way.
- Options:
  - (a) Always clear first (finally). Keep the primary CTAP exception; attach callback and logger failures as suppressed. Sia's lean: yes. Clearing never depends on logging.
  - (b) Let the callback failure surface. Can hide the primary CTAP status.
- Evidence: .NET `src/Ctap2/src/Backend/HidBackend.cs:114–117`; `src/Ctap2/src/Backend/SmartCardBackend.cs:146`. No sibling has a terminal-notification callback (python-fido2 `on_keepalive` is non-terminal; Rust `on_keepalive` is a plain callback).

### CTAP-IMP-007: Security-critical peer COSE labels
- Gate(s): G2 and G3. §6.5.4 says the peer key is parsed as getPublicKey specifies (kty 2, alg -25, crv 1, 32-byte x and y). All three siblings read only -2 and -3, so the precedent contradicts the spec and shares the omission.
- Severity / certainty: Low. Bounded impact: acceptance of a schema-invalid key-agreement response. No exploit shown.
- Decision: Require kty, alg and crv and 32-byte coordinates on the peer key, and how should extra labels be treated?
- Options:
  - (a) Require the three labels and 32-byte coordinates; tolerate unknown extra labels. Sia's lean: yes. Keeps security-critical fields strict and follows the forward-compat doctrine. This is a deliberate relaxation of getPublicKey's "no other optional parameters" wording.
  - (b) Fully strict, rejecting extra labels too. Spec-literal; less forward-compatible.
  - (c) Keep current. Rejected.
- Evidence: .NET `src/Ctap2/src/Pin/PinUvAuthHelpers.cs:63–66` (comment: labels not yet validated). python-fido2 `fido2/ctap2/pin.py:123–124 @5bc9d3a1c`. Android `fido/.../ctap/PinUvAuthProtocolV1.java:91–94 @f46268563`. Rust `crates/yubikit/src/ctap2/pin_protocol.rs:67–74 @90940e9bb`. Spec §6.5.4 (ecdh and getPublicKey).

### CTAP-IMP-016: Configuration expressiveness and capability gates
- Gate(s): G5. Reading alwaysUv before a toggle is a safety gain that no sibling has. The explicit-false representation also splits siblings (python-fido2 and Android send it; Rust omits it), so it is a decision.
- Severity / certainty: Low (Medium if unsupported policy operations are assumed accepted). Source-confirmed restrictions.
- Decision: Adopt nullable request fields (omit minimum length, send explicit false, empty RP list) and read alwaysUv before toggling?
- Options:
  - (a) Nullable fields in python-fido2 and Android style, plus read-before-toggle with a re-read after an uncertain outcome. Sia's lean: yes.
  - (b) Nullable fields only; keep the blind toggle.
- Evidence: .NET `src/Ctap2/src/Config/AuthenticatorConfig.cs:170–174`; `src/Ctap2/src/PublicAPI.Unshipped.txt:133` (public signature). python-fido2 `fido2/ctap2/config.py:104–109, 111–139 @5bc9d3a1c`. Android `fido/.../ctap/Config.java:141–144, 161–170 @f46268563`. Rust `crates/yubikit/src/ctap2/config.rs:148–151, 157–180 @90940e9bb`. Spec §6.11.4 (absent minimum keeps current; empty RP list allowed).

### CTAP-IMP-026: Request/response user representation
- Gate(s): none. Siblings split on name optionality: python-fido2 treats names as optional; Android requires name and displayName. The CTAP privacy policy overlaps DEC-002.
- Severity / certainty: Low. Source-confirmed representation gap.
- Decision: Accept absent names on responses and require non-empty id on requests, leaving WebAuthn handle policy to the client?
- Options:
  - (a) Accept absent name and displayName on responses; require non-empty id on requests. Sia's lean: yes. Matches python-fido2 and CTAP's optional names.
  - (b) Mirror WebAuthn exactly. Rejects valid CTAP responses.
- Evidence: .NET `src/Ctap2/src/Credentials/PublicKeyCredentialTypes.cs:323–325, 331–332, 350`. python-fido2 `fido2/webauthn.py:494–500 @5bc9d3a1c`. Android `fido/src/main/java/com/yubico/yubikit/fido/webauthn/PublicKeyCredentialUserEntity.java:58–61 @f46268563`.

### CTAP-DEC-002: Canonical native selected-user disclosure decision
- Gate(s): none. Actor and privacy intent decision; no severity assigned.
- Severity / certainty: none assigned. Actor trace complete; contract undecided.
- Decision: For native GetAssertion results, is the User entity raw application picker data (RP disclosure owned by the application) or browser-equivalent output with an enforced outgoing field set?
- Options:
  - (a) Raw application picker data, with the application owning disclosure, documented. Sia's lean: yes. No sibling SDK enforces an outgoing field set.
  - (b) Browser-equivalent output with an enforced contract. New public contract; overlaps the WebAuthn referral.
- Evidence: .NET `src/Ctap2/src/Credentials/GetAssertionResponse.cs:197–198`. python-fido2 `fido2/ctap2/base.py:184 @5bc9d3a1c` (raw user dict). WebAuthn referral is owned by the WebAuthn section.

## Auto-approved

### CTAP-IMP-005: Local PIN crypto exceptional cleanup
- Rule: A1 (no-brainer yes). Clearing owned buffers on exceptional exits is mechanical.
- Evidence pin (E2): .NET internal precedent `src/Piv/src/Authentication/PinOnlyProtocol.cs:93–95, 148–150` (finally ZeroMemory). Android `fido/.../ctap/PinUvAuthProtocolV2.java:70–76, 91–95 @f46268563` (finally Arrays.fill on derived and encryption keys). Site: `src/Ctap2/src/Pin/PinUvAuthProtocolV1.cs:174–179` (BEFORE).
- Pseudocode:
```text
V1/V2 Decrypt:
  out = new buffer; try { transform into out; return out } catch { ZeroMemory(out); throw }
V1 Authenticate:
  mac = full HMAC (32 bytes); try { return copy of mac[0..16] } finally { ZeroMemory(mac) }
Encapsulate: z = raw ECDH; try { derive } finally { ZeroMemory(z) }; guard z immediately after derivation
No provider-wide erasure; no new owner type; successful outputs unchanged
```
- Test to add: decrypt with invalid block length leaves the sentinel plaintext buffer zeroed; V1 MAC temporary cleared; successful outputs match fixed vectors.
- E1 escalation risk: stop if clearing would change the returned plaintext's ownership or touch provider-internal copies.

### CTAP-IMP-011: Exclude uv when token auth is sent
- Rule: A2 (python-fido2, Rust and Android agree). Clients omit uv when pinUvAuthParam is sent. CTAP 2.1 §6.1.1 and §6.2.1 say platforms MUST NOT include both.
- Evidence pin (E2): .NET sample `src/Ctap2/examples/FidoTool/FidoExamples/MakeCredential.cs:121–124` ("Known sample defect" comment; uv set after pinUvAuthParam). python-fido2 `fido2/client/__init__.py:844, 1000 @5bc9d3a1c` (uv only for internal UV). Rust `crates/yubikit/src/webauthn/client.rs:337, 520 @90940e9bb`. Android `fido/.../client/Ctap2Client.java:510–511, 655 @f46268563`.
- Pseudocode:
```text
MakeCredential sample:
  if (pinUvAuthParam used) { options.WithPinUvAuth(authParam, protocol.Version); }   // no uv key at all
  else if (built-in UV) { options.WithUserVerification(true); }
Typed encoder unchanged: no rejection, no normalization; document "uv MUST NOT accompany pinUvAuthParam"
```
- Test to add: MakeCredential with pinUvAuthParam encodes no "uv" key (including uv:false); built-in UV encodes uv true only.
- E1 escalation risk: stop if the encoder itself must reject, since that changes the typed API rather than the sample.

### CTAP-IMP-013: Biometric inventory schema and unsupported errors
- Rule: A3 (python-fido2 precedent; Android and Rust agree). Array decode; InvalidOption means empty; other statuses propagate. Optional names follow spec §6.7 and Android and Rust. python-fido2 requires names (`bio.py:318`); that is not adopted.
- Evidence pin (E2): .NET `src/Ctap2/src/BioEnrollment/FingerprintBioEnrollment.cs:228–231`. python-fido2 `fido2/ctap2/bio.py:310–326 @5bc9d3a1c`. Android `fido/.../ctap/FingerprintBioEnrollment.java:405–430 @f46268563` (ERR_INVALID_OPTION at 427). Rust `crates/ykman-cli/src/fido.rs:1282–1284 @90940e9bb` and `crates/yubikit/src/ctap2/bio_enrollment.rs:240–262 @90940e9bb`.
- Pseudocode:
```text
EnumerateEnrollmentsAsync:
  try { return ParseTemplates(send(ENUMERATE)[0x07]) }   // id required; name optional; malformed entry → error
  catch CtapException ex when ex.Status == InvalidOption → return []
  // InvalidCommand and other statuses rethrown unchanged
```
- Test to add: several templates; name absent; InvalidOption returns []; InvalidCommand surfaces as CtapException(InvalidCommand); malformed entry is an error.
- E1 escalation risk: stop if callers depend on empty results for unsupported devices beyond the FidoTool sample.

### CTAP-IMP-014: Enrollment completion and identity
- Rule: A2 (python-fido2, Android and Rust agree). Begin and next results differ; begin's template ID is carried; required fields per operation; completion only at remaining 0.
- Evidence pin (E2): .NET `src/Ctap2/src/BioEnrollment/BioEnrollmentModels.cs:273–275` (defaults). python-fido2 `fido2/ctap2/bio.py:230–261, 263–295, 140–143 @5bc9d3a1c`. Android `fido/.../ctap/FingerprintBioEnrollment.java:203–216, 292–331 @f46268563`. Rust `crates/yubikit/src/ctap2/bio_enrollment.rs:178–222 @90940e9bb`.
- Pseudocode:
```text
BeginResult { TemplateId (required), LastSampleStatus (required), RemainingSamples (required) }
NextResult  { LastSampleStatus (required), RemainingSamples (required) }   // ID comes from begin
Enroll loop: id = begin.TemplateId; loop { next = capture(id); if status != Good → CaptureError; if remaining == 0 → return id }
Sensor info: FingerprintKind and MaxCaptureSamplesRequiredForEnroll required
```
- Test to add: next response without template ID succeeds using begin's ID; missing status or remaining → InvalidResponse; remaining 0 completes; non-zero continues.
- E1 escalation risk: stop if the public result type change reaches beyond FingerprintBioEnrollment, or if a caller needs the next result to carry an ID.

### CTAP-IMP-018: Disposed CredentialManagement helper
- Rule: A5 (.NET-only lifetime). Follow the module's existing ThrowIfDisposed pattern.
- Evidence pin (E2): internal precedent `src/Ctap2/src/Ctap2Session.cs:243, 324, 450` (ThrowIfDisposed) and `src/Oath/src/OathSession.cs:204`. Site: `src/Ctap2/src/CredentialManagement/CredentialManagement.cs:293–294` (BEFORE).
- Pseudocode:
```text
CredentialManagement public members: ThrowIfDisposed() first → ObjectDisposedException
Dispose(): if already disposed return; _disposed = true
  // does not dispose session or protocol; does not clear the borrowed token
```
- Test to add: each public operation after Dispose throws ObjectDisposedException; Dispose twice is safe; the borrowed session remains usable.
- E1 escalation risk: stop if a caller needs a non-owning helper that keeps working after Dispose.

### CTAP-IMP-023: SmartCard SELECT and empty-response provenance
- Rule: A4 (siblings split; Medium, so the stricter fail-closed option). Android maps only three no-AID statuses to not-available. Rust keeps other provenance and rejects empty responses.
- Evidence pin (E2): .NET site `src/Ctap2/src/Backend/SmartCardBackend.cs:61–65`. Android `core/src/main/java/com/yubico/yubikit/core/smartcard/SmartCardProtocol.java:173–188 @f46268563` (6D00, 6A82, 6E00 → ApplicationNotAvailable; others → IOException). Rust `crates/yubikit/src/ctap.rs:255–262 @90940e9bb` (select error kept as Connection) and `crates/yubikit/src/ctap2/session.rs:99–101 @90940e9bb` (empty → InvalidResponse).
- Pseudocode:
```text
SelectApplicationAsync:
  catch ApduException ex when ex.Sw in {0x6D00, 0x6A82, 0x6E00} → throw NotSupportedException("FIDO2 AID not available", ex)
  catch ApduException ex → rethrow with ex preserved (not NotSupported)
Empty CTAP response (length 0) → throw InvalidResponse("empty CTAP response"); no fabricated CtapStatus
```
- Test to add: 6A82 maps to NotSupported with inner ex; 6F00 is not NotSupported and keeps the inner ex; empty response maps to InvalidResponse.
- E1 escalation risk: stop if the public exception type must change (keep NotSupportedException), or if the probe-path NotSupported behaviour needs changing.

### CTAP-IMP-024: Administrative owned request temporaries
- Rule: A1 (no-brainer yes). Named owned-buffer finally clearing; borrowed token untouched.
- Evidence pin (E2): Android `fido/.../ctap/ClientPin.java:343–396 @f46268563` (changePin finally clears pinHash, pinHashEnc, newPinEnc, pinUvAuthParam) and `ClientPin.java:193–202` (getPinToken). Rust `crates/yubikit/src/ctap2/client_pin.rs:320, 359 @90940e9bb` (Zeroizing hash). Internal precedent `src/Oath/src/OathSession.cs:275–288`. Site: `src/Ctap2/src/CredentialManagement/CredentialManagement.cs:431–436` (BEFORE).
- Pseudocode:
```text
BuildUpdateUserPayload / config / bio helpers:
  owned = [subCommandParams, messageToAuth, pinUvAuthParam, payload, requestCopy]
  try { build, authenticate, send } finally { ZeroMemory each non-null owned buffer }
Never clear _pinUvAuthToken, the protocol, or caller-owned arrays
```
- Test to add: owned buffers observed through actual arrays are zero after success and after Authenticate, Encode and send failures; the borrowed token is unchanged.
- E1 escalation risk: stop if owned-buffer ownership must move out of the helper, or if the change touches the borrowed token.

### CTAP-IMP-025: AAGUID endian portability
- Rule: A1 (no-brainer yes). The host-dependent branch is removed; siblings treat AAGUID as raw RFC-order bytes.
- Evidence pin (E2): python-fido2 `fido2/webauthn.py:64–67, 87 @5bc9d3a1c` (raw 16 bytes). Android `fido/.../ctap/Ctap2Session.java:763 @f46268563` (raw byte[]). Rust `crates/yubikit/src/ctap2/mod.rs:339–356 @90940e9bb` (raw [u8;16]). Site: `src/Ctap2/src/Cbor/AaguidConverter.cs:77–82` (BEFORE).
- Pseudocode:
```text
ToBigEndianBytes(Guid g):
  b = g.ToByteArray()
  reverse b[0..4], b[4..6], b[6..8]        // unconditional; no IsLittleEndian branch
  return b
FromBigEndianBytes(b): copy b, apply the same reversal, return new Guid(copy)
```
- Test to add: fixed AAGUID text and byte vector round-trip in both directions, with no host branch in the test.
- E1 escalation risk: stop if `Guid.ToByteArray` is host-dependent on a target runtime; then build the Guid from the 16 RFC bytes explicitly.

### CTAP-IMP-027: Credential and extension result owners

**Historical proposal, superseded by [AG-26](../pass-2-review-decisions.md#ag-26-ctap-imp-027-ownership).**
The proposal and evidence below are retained for context, not as implementation instructions.

- Rule: A5 (.NET-only lifetime). Follow the StoredCredentialInfo and CredentialEnumerationResult pattern in this module.
- Evidence pin (E2): internal precedent `src/Ctap2/src/CredentialManagement/CredentialEnumerationResult.cs:63` (_disposed) and `src/Ctap2/src/CredentialManagement/StoredCredentialInfo.cs:210` (owned clone cleared). Site: `src/Ctap2/src/Credentials/MakeCredentialResponse.cs:187–189` (BEFORE).
- Pseudocode:
```text
MakeCredentialResponse, GetAssertionResponse: sealed, IDisposable
  Decode: copy authData and extension-output bytes into owned arrays (secret-bearing views only)
  Dispose(): ZeroMemory owned copies; idempotent
Borrowed input unchanged; public property shapes unchanged
```
- Test to add: owned arrays cleared on Dispose; decode of fixtures returns the same values as before; borrowed input unchanged.
- E1 escalation risk: stop if the change needs owner types beyond these two responses, or changes RawData retention.

### CTAP-IMP-031: credBlob size and empty-input contract
- Rule: A3 (python-fido2 and Android use the device-advertised capacity; Rust errors on oversize). Empty and oversize rejected at MakeCredential (A4 stricter option).
- Evidence pin (E2): .NET `src/Ctap2/src/Extensions/CredBlob/CredBlobInput.cs:64–67` (BEFORE, fixed 1 to 32). python-fido2 `fido2/ctap2/extensions.py:491–492 @5bc9d3a1c`. Android `fido/.../client/extensions/CredBlobExtension.java:59–61 @f46268563`. Rust `crates/yubikit/src/webauthn/extensions/cred_blob.rs:104–105 @90940e9bb`.
- Pseudocode:
```text
At MakeCredential, with cached GetInfo:
  if blob is empty → throw ArgumentException("credBlob must not be empty")
  max = info.MaxCredBlobLength              // advertised; not a fixed 32
  if blob.Length > max → throw ArgumentException
DTO: remove the fixed 1..32 bound; raw builder unchanged
```
- Test to add: empty rejected; length equal to max accepted; max+1 rejected; advertised max other than 32 (for example 64) accepted at 64.
- E1 escalation risk: stop if the check needs a fresh GetInfo or changes the builder API; stop if credProtect enforcement is requested (out of scope).

### CTAP-IMP-033: Ceremony-specific hmac-secret accessors
- Rule: A3 (python-fido2 and Android route by ceremony).
- Evidence pin (E2): .NET `src/Ctap2/src/Extensions/Shared/ExtensionOutput.cs:155–165` (TryGetHmacSecret, one byte-string decode). python-fido2 `fido2/ctap2/extensions.py:317–318` (registration bool) and `:367` (assertion bytes) @5bc9d3a1c. Android `fido/.../client/extensions/HmacSecretExtension.java:148` (registration Boolean) and `:205` (assertion byte[]) @f46268563.
- Pseudocode:
```text
Registration output: TryGetHmacSecretRegistration(out bool enabled) reads "hmac-secret" as bool
Assertion output:    TryGetHmacSecretAssertion(out byte[] output) reads "hmac-secret" as byte string
Wrong shape → false (Try contract kept)
Existing TryGetHmacSecret: keep for assertion only, documented, or route by ceremony
```
- Test to add: registration bool true and false; assertion bytes; registration with a bool does not throw; wrong shape returns false.
- E1 escalation risk: stop if removing a public accessor breaks an in-repo sample beyond FidoTool.

### CTAP-IMP-035: Storage, salt and metadata temporary cleanup
- Rule: A1 (no-brainer yes; local clearing only).
- Evidence pin (E2): Android `fido/.../ctap/ClientPin.java:320–326 @f46268563` (setPin finally clears pinBytes, shared secret, pinEnc). Rust `crates/yubikit/src/ctap2/client_pin.rs:164, 320–324 @90940e9bb` (Zeroizing pad and hash). Internal precedent `src/Oath/src/OathSession.cs:275–288`. Site: `src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs:226–230` (BEFORE, no finally).
- Pseudocode:
```text
BuildHmacSecretInput:
  saltData = ...
  try { saltEnc = Encrypt(saltData); saltAuth = Authenticate(saltEnc); return dto }
  finally { if (!transferred) ZeroMemory(saltData, saltEnc) }
Membership probe: ZeroMemory(probePlaintext) after compare (not transferred)
Compression backing and metadata failure output: finally ZeroMemory
Caller-owned inputs never cleared
```
- Test to add: observed arrays cleared after Encrypt, Authenticate and DTO-construction failure, and after the membership probe; the success path returns a usable DTO.
- E1 escalation risk: stop if successful outputs' ownership or transfer must change (the card separates transfer decisions).

### CTAP-IMP-039: Propagate derived algorithm, not seed marker
- Rule: A2 (python-fido2, Rust and Android agree). The generated key's algorithm is the device-reported previewSign output key 3, the derived key algorithm. No sibling uses the seed marker.
- Evidence pin (E2): .NET `src/Ctap2/src/Extensions/PreviewSign/PreviewSignRegistrationOutput.cs:98–102` (passes arkgKey.Algorithm, the seed marker). Documented disagreement: `src/Ctap2/src/Extensions/PreviewSign/PreviewSignCbor.cs:470` and `PreviewSignGeneratedKey.cs:67`. python-fido2 `fido2/ctap2/extensions.py:762–766 @5bc9d3a1c` (algorithm = data.get(3)). Rust `crates/yubikit/src/webauthn/extensions/sign.rs:223–226 @90940e9bb`. Android `fido/.../client/extensions/SignExtension.java:279 @f46268563`.
- Pseudocode:
```text
TryGetGeneratedKey():
  alg = previewSign output key 3 (device-reported), same source as PreviewSignCbor extraction
  return new PreviewSignGeneratedKey(handle, blPub, kemPub, alg)   // never -65700
Dispatch on alg: out of scope; no change
```
- Test to add: one fixture where both accessors return the same algorithm; the seed marker never appears as the generated key's algorithm.
- E1 escalation risk: stop if the device-reported algorithm differs from the seed's DerivedKeyAlgorithm on a real fixture.

### CTAP-IMP-044: Optional RP-capable protocol demonstration
- Rule: A6 (keep and document). The demo is labelled a protocol demonstration; the RP-capable enhancement is optional and not approved.
- Evidence pin (E2): `src/Ctap2/examples/FidoTool/FidoExamples/MakeCredential.cs:26, 85–86`; `src/Ctap2/README.md:85`; `src/Ctap2/examples/FidoTool/README.md:183–186`.
- Pseudocode: none. Docs only: keep the protocol-demo boundary text; no code change.
- Test to add: none (docs-only).
- E1 escalation risk: stop if the work starts adding RP verification, a serializer, or account handles derived from RP input.

### CTAP-IMP-045: FidoTool help, parser and truthful display
- Rule: A1 (no-brainer yes for truthful help and parser errors). The UI-policy facet is limited to not inventing a sensor-kind name.
- Evidence pin (E2): .NET site `src/Ctap2/examples/FidoTool/Program.cs:678–684` (ParseOption returns args[i+1] without a bounds or flag check). Rust `crates/ykman-cli/src/fido.rs:18, 39 @90940e9bb` (clap Subcommand: unknown verbs are parse errors).
- Pseudocode:
```text
ParseOption(args, flag):
  i = index of flag; if none → null
  if i+1 >= args.Length or args[i+1] starts with "--" → throw UsageError("missing value for " + flag)
  return args[i+1]
Unknown subcommand → usage to stderr; non-zero exit
help → dispatch before device selection; list only implemented subcommands (set-pin and new-pin listed only if implemented)
Unknown sensor kind → display "unknown (0x..)"; never a named kind
```
- Test to add: missing flag value gives a usage error; unknown verb exits non-zero; help runs without a device; unknown sensor kind string.
- E1 escalation risk: stop if help must describe features the sample does not implement (the card says not to add absent features).

## T1 hardware qualification
| Card | What needs a device | Device/firmware/transport/OS |
| --- | --- | --- |
| CTAP-DEC-003 | Exact SKU, firmware and prototype support; standard versus preview bio minima and vendor field definitions; reset product window (5.5.2, 5.5.3); earliest hmac-secret-mc, PPUAT and encrypted-field support; ARKG vendor profile; preview beta 0x7F and ECDH provenance; unsigned-output introduction history. | Each SKU and firmware line named in the card; HID FIDO over USB and SmartCard over NFC or USB 5.8 or later. Record OS and driver per run. Reset only with an independent before and after oracle. |
| CTAP-IMP-022 | Integration oracles that need real delete and re-read, feature exercise and cap isolation (credBlob, largeBlobs, bio). Fake crypto and CBOR vectors need no device. | YubiKey at a pinned CTAP 2.1 firmware with the features under test; HID FIDO over USB and SmartCard transport; Windows and macOS runs recorded separately. |

Linked device steps (not separate T1 cards): CTAP-IMP-012 (physical enrollment) and CTAP-IMP-048 (reset) need a device when their interview outcomes are approved.

## T2 cross-cutting candidates
| Theme | Card(s) here | Why it is cross-cutting |
| --- | --- | --- |
| Secret buffer and owned-copy lifetime | CTAP-IMP-004, 021, 027, CORE-CTAP-X020 | Same owner-API question as Core report and APDU copies (CORE-B5-C005, B6-C002) and PIV and OATH clearing. CTAP-IMP-005, 024 and 035 are local A1 cases. |
| Cancellation and terminal notification | CTAP-IMP-012, 047, CORE-CTAP-X020 | Timeout and cancellation are a named policy example; Core owns notification and terminal resolution. |
| Error mapping (unsupported, malformed, transport) | CTAP-IMP-013, 016, 023, 047 | Same unsupported-versus-malformed split in OATH, PIV and OpenPGP SELECT and unsupported paths. |
| PIN/UV token and retry state after failed authentication | CTAP-IMP-008, 009, 010 | Named policy example (session state after failed authentication); OATH-DEC-001 has the same HMAC-mismatch shape. |
| Forward compatibility of GetInfo and response preservation | CTAP-IMP-001, 034 | Root CLAUDE.md forward-compat doctrine applies to every applet's GetInfo and response parsing. |
| Firmware and transport gating; sentinel metadata | CTAP-IMP-002 | Management and PIV firmware gates; Core SCP firmware path (CORE-CTAP-X017). |
| Destructive target continuity | CTAP-IMP-048 | Security Domain reset (SD-IMP-001) and PIV and OATH reset paths use the same confirm-then-act question. |
| Integration test defaults, oracles and secret exports | CTAP-IMP-020, 021, 022, 042 | Shared integration-test policy: PIN normalization defaults, fake versus real oracles, secret export channels. |
| PIN input admission (units, NFC, length) | CTAP-IMP-006 | CORE-CTAP-X003 reader units; OpenPGP keeps its own profile; PIV PIN input. |
| Whole-operation admission and multi-exchange workflows | CTAP-IMP-019, 036 | Core A/B1 and B5-C006 workflow admission; same caller-serialization question. |
| Disposal of borrowed-resource helpers | CTAP-IMP-018, 027 | Core workflow disposal convention; borrowed session, protocol and token ownership. |
| Experimental draft profiles | CTAP-IMP-029 (tagged); also 038, 039, 040, 041 (cross-layer, untagged) | WebAuthn previewSign client layer and Core ARKG math (C018, C019). One decision on draft expiry and profile pinning. |

## Coverage check
- Review map: the CTAP2 review map (master lines 2364–2371) is lane-level, with no per-card severity or no-brainer column. Per-card values come from each card body, which I read in full.
- Cards in the section, 50 in total: CTAP-IMP-001 to 016 (16 cards), CTAP-IMP-018 to 048 (31 cards), CTAP-DEC-002, CTAP-DEC-003 and CORE-CTAP-X020. Each appears once in the Index: 001, 002, 003, 004, 005, 006, 007, 008, 009, 010, 011, 012, 013, 014, 015, 016, 018, 019, 020, 021, 022, 023, 024, 025, 026, 027, 028, 029, 030, 031, 032, 033, 034, 035, 036, 037, 038, 039, 040, 041, 042, 043, 044, 045, 046, 047, 048, DEC-002, DEC-003, X020 (50 rows). Master totals agree: 47 IMP, 2 DEC and 1 X020.
- Coverage-ledger exclusions, not treated as cards:
  - CTAP-IMP-017: rejected runtime inference (writer sorts keys; no wire defect).
  - CTAP-DEC-001: resolved into CTAP-IMP-025.
  - CTAP-XREF-001 and WA-XREF-001: documentation resolved.
  - CTAP-XREF-002 and the WA-IMP referrals: live in the WebAuthn section.
  - CTAP-XREF-003 and CORE-CTAP-X003: mapped to CTAP-IMP-006.
  - D-14 (withdrawn), F-10 (rejected), historical makeCredential up=false and raced-success (already corrected), F project hash (resolved), CTAP-DOC-001 to 018 (closed at handoff), ARKG seed marker and BE/BS citation (resolved evidence corrections).
  - CORE-CTAP-X001 to X019 and X021 to X023: occurrence groups mapped to existing cards, not additional cards.
- Sources not used: the constraints store contains only a README with no CTAP2 records, so nothing was consulted. yubikey-manager @ 4ca60f706 was not read; master citations to it are unverified in this pass.
- Verification done: .NET citations spot-checked against `src/Ctap2` at 52d34ca65 (module unchanged since master pin 6ce355468). Sibling pins checked clean (no unstaged diffs). No build, test, source edit, commit, push or hardware action.
