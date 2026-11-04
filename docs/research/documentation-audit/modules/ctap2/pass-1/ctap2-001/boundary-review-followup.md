# Boundary correction re-review and bounded flags adjudication

**Date:** 2026-11-04. **Reviewer:** GPT-6.1 Sol, same independent final-boundary reviewer, not Astra/editor.
**PRIMARY:** `/home/rupicola/Code/y/Yubico.Yubikit.NET`.
**Unchanged baseline/HEAD:** `5b09a9b6942ded3913b5c9340c3e126617e61a82`.

**Verdict: BF-R1 and BF-R2 corrected and independently re-reviewed PASS; authorized BF-O1 PASS.**
**One remaining required documentation clarification: BF-R3, `src/WebAuthn/CLAUDE.md:544`.**
This is a bounded correction follow-up plus the coordinator's explicit Known Gotchas question, not a
whole-WebAuthn re-audit or a renewed implementation/security/hardware acceptance.

Read `correction-boundary.md` and the complete corrected architecture permissions, retry/key-agreement,
invariants and neighboring policy paragraphs (lines101–230); the complete codec registration XML/body
(lines83–165); the actual registration call/envelope/inner decoder chain. Re-read WebAuthn module
guidance, including its extension/privacy/ownership/error limitations and Known Gotchas. Previously
read root/CTAP guidance, canonical workflow/style and verification/security skill context remain
applicable. Existing independently retrieved S23 and P4/V4 authority from the initial review was
re-read; no new remote retrieval or SDK/library experiment is claimed.

Only this report and the dated current assessment in `boundary-final.md` were authored. No source
edits, builds, tests, hardware, formatting, commits, delegation, rendering or token/compiler operations.

## Complete-unit correction decisions

| Item | Actual corrected claim and independently checked chain | Disposition |
| --- | --- | --- |
| **BF-R1** | `docs/architecture/webauthn-extension-processing.md:107–109,185–196` now says the **request** combines ceremony/processor permissions, and locally says legacy fallback omits explicit permission/RP request, so a returned token proves no complete authorization union. `WebAuthnClient.Authentication.cs:110–119`, `WebAuthnClient.Registration.cs:117–127`, `WebAuthnBackend.cs:104–136`, `ClientPin.cs:375–459` confirm union/delegation/legacy branch. Original S23 §6.5.5.7/§6.5.5.7.1 supplies default mc/ga rather than arbitrary requested permissions. | **Corrected / PASS.** No token introspection, negotiation, administrative authorization or mandatory pinless-feature addition implied. WA-IMP-009/CTAP-IMP-010 remain open. |
| **BF-O1**, authorized | Architecture:118–122 now says fresh per-attempt key agreement/encrypted salts follow token reacquisition and are **not derived from token bytes**, then retains that key agreement can operate without PUAT. `RunWithUvEscalationAsync:129–136` acquires before invoking the attempt; authentication:137–156 and registration:137–155 create a fresh extension session and prepare it. `PrfProcessor:136–169` calls independent backend EncapsulateAsync; `WebAuthnBackend:70–79` uses protocol/ECDH, not token bytes. | **Accepted / PASS.** Phase ordering and independent cryptographic role preserved; no forced raw CTAP/WebAuthn parity or cleanup guarantee. |
| **BF-R2** | `src/WebAuthn/src/Extensions/Codec/PreviewSignCodec.cs:83–108` parameter now explicitly names **makeCredential key0x06**. Complete body:109–165 still reads signed algorithm, then `unsignedExtensionOutputs["previewSign"]`, delegates nested key7 bstr decoding and builds generated-key attestation. `PreviewSignProcessor:111–119` passes MakeCredentialResponse.UnsignedExtensionOutputs; `MakeCredentialResponse:91–102,213–228` parses outer0x06; `PreviewSignCbor:298–400` parses nested7/inner integer map. Original S23 make table0x06 versus get table0x08 and P4 att-obj7 confirm the distinction. | **Corrected / PASS.** Empty/missing/error isolation and parsing-not-trust qualifications preserved. No body, constant, envelope modeling, extraction/attestation verifier or API change. |

The complete architecture invariants retain selected-singleton requests, per-attempt processor use,
output-before-token-disposal and the unsupported/Required/blob/PRF distinctions. The corrected codec
retains lower-level decode errors and explicit client-error propagation; accepting the XML correction
does not fix flags interpretation, generated-key trust, algorithm propagation or result ownership.

## BF-R3 — remaining Known Gotchas wording requires narrow clarification

**Exact current path/claim:** `src/WebAuthn/CLAUDE.md:540–546`, item3 at544:

> **`flags` optional in previewSign registration output** — Some authenticators return only key 3 (algorithm)

**Adjudication: required factual clarification, not optional style.** This suggests flags normally
belong in the outer registration map and algorithm-only output is a particular authenticator variant.
It conflicts with the corrected CTAP contract and lacks device-variant evidence. The original P4
vendor draft §10.2.1 says flags are:

> Present only in the attestation object embedded within the att-obj output during registration ceremonies.

The draft's outer signed registration output carries chosen algorithm key3; unsigned previewSign
key7 wraps generated-key attestation with the signing policy inside. Actual
`src/Ctap2/src/Extensions/PreviewSign/PreviewSignCbor.cs:230–266` documents and implements outer
key4 **parser tolerance**. Absence of that outer key is not beta-firmware evidence or an assertion
about the inner flags. `PreviewSignCodec.cs:121–126` accepts/defaults the returned nullable outer
flags local; that does not establish a standard outer-flags or device-qualification contract.

**Required prose-only replacement**, confined to Known Gotchas item3:

> **previewSign policy flags belong inside the generated-key attestation** — The draft's signed
> outer registration output carries algorithm key3. The CTAP parser tolerates an outer flags key4;
> its absence is not evidence of a beta/device variant or of the inner policy.

Equivalent concise wording is acceptable if those layer/tolerance/no-device-proof distinctions remain.
Do not add inner-policy enforcement, remove parser tolerance, strip native fields, claim hardware
observation or change codec/XML/body beyond separately authorized prose. This follows existing
CTAP-DOC-015/017 and CTAP-IMP-038/WA-IMP-006 dependencies, not a new engineering defect count.
Coordinator should explicitly assign this previously unchanged WebAuthn guidance path and re-review
the complete corrected item with its surrounding previewSign status/architecture paragraphs. No other
WebAuthn path is requested by this bounded adjudication.

**Current uncorrected guidance SHA256:**
`b058a8b1089549410867550550d24ab7c4540b76a3c47aaa08af995c6b56fe2a`.
The initial final-boundary read included this guide but did not flag that sentence; the coordinator's
bounded question prompted this additional adjudication. Do not retroactively claim it was already
accepted as accurate or that original G had found it.

## Fresh snapshot/preservation checks

Read-only Python recomputed all28 key claim/source hashes from the initial boundary-final table:
**only the two explicitly corrected paths differ; other26 match**, including WebAuthn CLAUDE,
CTAP credential decoder, authentication ceremony and PRF/backend dependencies represented in that table.
This is byte identity for those28 paths, not a fresh review of every source file or all campaign gates.
The codec's baseline comparison, removing only whole-line `//` comments and blanks, leaves the same
noncomment line sequence. Formal token/directive/API/compiler/format checks remain coordinator-owned.

| Accepted corrected PRIMARY path | New exact SHA256 | Prior reviewed SHA256 |
| --- | --- | --- |
| `docs/architecture/webauthn-extension-processing.md` | `56ce5bf608fa1452402100b3d6e51af6b8a3b8277d0f3ada1e8b4b342bffe09c` | `6b183a1724e6d7da3720f300d5d5c91124851cda08a4e3f12cdedf79973a7178` |
| `src/WebAuthn/src/Extensions/Codec/PreviewSignCodec.cs` | `2ed3b03efcb3e82fd58980b91518abe0638b064207e7ccae025e920aa2a30fb7` | `a394b1cbbc30979835d01da6cf4088d920a8b780a4eaf2605770189118200732` |

Original map remains SHA256
`097c23d16d9f72f16a0a05db730b5bee4a5e0fda6a6d872fe59e2e668c713c36`; original boundary-review is
not rewritten. `boundary-final.md` now records these accepted current dispositions/hashes and BF-R3
with a dated follow-up. WA-XREF-001 documentation satisfaction remains verified; all original WA
engineering IDs, DEC-002, COORD-001 and experimental/product/ownership limits remain unchanged.

Coordinator reports full repository gates at3998 tests/zero warnings; this reviewer did not run or
independently certify those gates. Later token/format/example checks and any image freshness/remote
checkpoint remain coordinator work. No whole-WebAuthn re-audit, implementation completion, hardware
qualification, generated-image PASS or Pass2 authorization follows. **Next: BF-R3 prose clarification
and focused re-review; BF-R1/BF-R2/BF-O1 need no further correction at these hashes.**
