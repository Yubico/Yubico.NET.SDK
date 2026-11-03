# WebAuthn documentation triage

2026-11-03; Astra; baseline `e353b49afa15d1d104fce70bbf1e1abc59ddb132`.

## Accepted documentation work

All 22 WA-A, 16 WA-B and 13 WA-C findings have supported documentation dispositions. These are **51 report findings, not 51 independent defects**: repeated selection, UV, naming, optionality and citation causes are reconciled across reports. Their exact file/symbol evidence and citation indexes remain in `investigation-a.md`, `investigation-b.md`, `investigation-c.md`. No runtime/API behavior change is authorized in this batch; project `Description` is documentation metadata and accepted.

- Editor A: WA-A-001–022 documentation/comment portions and Q1's truthful result limitation, only WA-A-owned C# files. WA-A-020 is handed to editor C. Preserve runtime tokens and test assertions.
- Editor B: WA-B-001–016 documentation/comment portions, only WA-B-owned C# files. Reconcile protocol-2-only context with A-021. Preserve runtime tokens and test assertions.
- Editor C: WA-C-001–013 plus dependent A/B limitations in three module Markdown files, project Description, `docs/architecture/webauthn-extension-processing.md`, `docs/v2-highlights.md`, `docs/v1-to-v2-comparison.md`, and the single transport phrase in `src/Management/examples/ManagementTool/README.md`. Also correct WA-S-001's named cancellation argument in `docs/usage/user-interaction.md` and accepted follow-ups in `sample-expansion.md`. Preserve historical migration entries and unrelated prose.
- Editors are fresh Sol sessions and run sequentially in the campaign tree. Independent review is fresh and read-only. No overlapping write ownership.

Sample expansion is complete: accept `WA-SAMPLE-EXPAND-001` (same root cause as WA-S-001) and
`WA-SAMPLE-EXPAND-002`. Explicitly extend editor C's ownership to the single WebAuthn XML example in
`src/Core/src/Credentials/UserPresenceIntent.cs`: name the third cancellation argument and use
`requestOptions` consistently with the guide. Read Core instructions first. This is a directly dependent
WebAuthn consumer example, not a Core audit or implementation change. Add that one external reference to
the coverage total (30 external files after expansion). No additional external snippet defect was found.

Editorial precision: explanations must identify actual behavior as a current limitation where a contract/spec is unmet, not redefine the intended standard. Keep useful conditions local; avoid repeating the entire limitation ledger throughout the guide. The README needs a coherent task flow, one original registration result, count-safe assertion handling, explicit RP-owned inputs and response handoff, and configuration alternatives that do not open overlapping sessions. Explain a small usable extension example and link deeper details. Do not introduce fabricated upload APIs or an RP server implementation.

Specific adjudications:

1. PRF UV: Astra independently fetched dated WebAuthn L3 REC §10.1.4 and confirmed the verified-PRF MUST/UV override. Explicitly disclose present non-enforcement and use Required for evaluation, without claiming this work fixes conformance.
2. credProtect: Astra independently fetched CTAP2.3 PS §12.1 and confirmed enforcement belongs to the platform and normative strength is SHOULD NOT. Source processor only forwards/parses; do not promise the flag prevents creation, and do not equate checking output after creation with enforcement.
3. Sensitive PRF memory: Astra read `PrfCodec.DecryptResults`; array ranges plus ToArray produce uncleared intermediates and returned arrays. State the missing SDK cleanup guarantee; do not invent writable result spans or say clearing a copy clears originals. No memory API redesign here.
4. Selection and errors: Astra traced authentication eager response construction and the catch boundary in `RunWithUvEscalationAsync`; token acquisition and output preparation sit outside its mapping/retry scope. Correct universal mapping and lazy-device-work claims.
5. Missing credential ID: disclose current empty-ID limitation. Never suggest guessing the selected ID from the first entry of a multi-entry allow list. An application-known ID is unambiguous only for an original single-entry request and still requires RP verification.
6. Aaguid default: Astra inspected null-backed default and nonreflexive Equals. Document as a defect/limitation with construction advice, not intentional equality semantics; defer code fix.
7. LargeBlob nullable conversion was initially unverified. The isolated `experiment_nullable_blob.cs`
   check now confirms that a null byte[] constructor argument produces a present empty ReadOnlyMemory<byte>.
   This is .NET conversion evidence plus the unchanged processor assignment, not a live storage test.
   Update the two B comments, module CLAUDE and checkpoint to the precise lost-distinction limitation.
8. Hardware-test capability assumptions and zeroing assertions get accurate descriptions, not changed tests. No hardware qualification claim is added.
9. Existing specification levels remain: e.g. fix the cited L2 UV section without automatically upgrading it. previewSign is vendor draft v4, not CTAP4. Remove the unsupported §9.4 malformed-output quotation; describe SDK exception isolation as policy.

## Deferred engineering ledger

## Independent review corrections — accepted

Reviewer A (`review-a.md`) requires R1/R2. Astra fetched CTAP2.0 PS 2019-01-30 response table and
CTAP2.3 PS 2026-02-26 response table directly: 2.0 marks credential optional with singleton allow-list
omission; 2.3 marks it required. The original A-Q1 attribution to 2.1/2.3 was incorrect. Its source-backed
empty-ID limitation remains, but valid omission must be qualified as CTAP2.0 §5.2, not a later-version
permission. Correct the existing `CredentialFilter.cs` citation too. R2's no-disclosure guarantee exceeds
the fake test's assertions and is accepted for factual weakening.

After B finishes, editor C is explicitly assigned these narrow A corrections in addition to C's own
prose: `Client/Authentication/AuthenticationResponse.cs`, `Client/WebAuthnClient.Authentication.cs`,
`Client/CredentialFilter.cs`, and `tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientCeremonyFrameTests.cs`
under WebAuthn. No executable tokens change. Reviewer A re-reviews the corrected scope afterward.

B review requires one correction (`review-b.md`, B-R1). Astra inspected the actual test: any returned ID
other than the first selects the second verification key, without asserting membership. Accept the
summary qualification and explicitly assign editor C the summary of `TwoCredentialAllowList_SignsWithTheSelectedCredentialsEntry`
in WebAuthn IntegrationTests/PreviewSignTests.cs. Preserve all executable tokens. Stronger test membership
coverage is deferred as WA-IMP-021; reviewer B rechecks the corrected summary afterward.

### Deferred item register

Owner is the future WebAuthn implementation pass unless a narrower owner is named. Every item reopens the listed public/internal documentation and relevant tests after a behavior change. None is silently resolved by a prose edit.

| ID | Evidence / unresolved behavior | Documentation re-review trigger and scope |
| --- | --- | --- |
| WA-IMP-001 | A-003: Enterprise does not send CTAP enterprise-attestation request; Indirect has no anonymization service | Enterprise/attestation policy or forwarding; enum, creation option, guide, backend |
| WA-IMP-002 | A-005/B-007/B-012/C-D2: incomplete ceremony/extension error normalization and unsupported-previewSign policy | Error/isolation policy changes; public methods/errors, codecs/sessions, selection/blob/previewSign guidance |
| WA-IMP-003 | A-006: cooperative timeout can race success; oversized delay uses framework exception | Timeout input/return policy; both options, deadline, cancellation guide, hardware-test assumptions |
| WA-IMP-004 | A-009: discouraged-UV hardware assertion lacks capability guards | Test owner adds device capability applicability; test comments and UV guidance |
| WA-IMP-005 | A-012: manual escaping differs from exact CCDToString for controls; low-level builder validation boundary | Serializer/control/surrogate policy; client-data XML/tests and conformance statements |
| WA-IMP-006 | A-013: borrowed attestation CBOR can diverge from parsed data if mutated | Decoder snapshot/ownership change; Decode/RawCbor/Encode and generated-key attestation guidance |
| WA-IMP-007 | A-014: default Aaguid empty span and nonreflexive equality | Zero/default semantics fix; Aaguid docs and equality/default regression evidence |
| WA-IMP-008 | A-018: test checks disposal guard, not actual shared-memory zeroing | Test owner adds retained-view assertion; test documentation and verification claims |
| WA-IMP-009 | A-021: protocol2 hardcoded, no protocol1 negotiation; built-in-UV token capability checks | Backend negotiation/UV support; prerequisites, UV table, protocol/context/PRF docs |
| WA-IMP-010 | A-Q1: allowed final credential-descriptor omission publishes empty ID | Descriptor recovery; matcher/result/selection XML, guide, credential-specific extensions |
| WA-IMP-011 | B-001/C-D1: PRF does not enforce verified variant as WebAuthn requires | Coordinated PRF UV policy; both ceremonies, input/processor/results, README/CLAUDE/architecture, relevant CTAP guidance |
| WA-IMP-012 | B-002: credProtect enforcement flag ignored, including unsupported devices | Client enforcement and CTAP builder field use; input/output/processor/session XML and guide |
| WA-IMP-013 | B-003: sensitive result ownership/zeroing absent, PRF intermediate copies/input salt temporary uncleared | Ownership API/cleanup pass with narrow CTAP dependencies; all PRF/blob/result/client lifetime docs and examples |
| WA-IMP-014 | B-005: largeBlob Required can produce unsupported credential | Required registration policy; largeBlob enum/input/output, effective RK guidance, tests |
| WA-IMP-015 | B-006: largeBlob writes do not validate exactly one original allowed credential | Request validation; write input, session/processor/tests and guide |
| WA-IMP-016 | B-007: null array becomes present empty memory; isolated .NET check confirmed conversion | Blob result policy fix; Blob optionality/error documentation |
| WA-IMP-017 | B-008: direct largeBlob route/pinless writes unsupported by current SDK path | Feature support decision, not automatically a required implementation; capability/permissions/storage/UV guidance |
| WA-IMP-018 | B-010: MinPinLength Requested=false ignored when object present | Input handling decision; aggregate parameter, processor/codec and CTAP reused input |
| WA-IMP-019 | B-013: valid empty generated previewSign handles rejected by signing params | Constructor/round-trip policy; generated key/params/exception docs and tests |
| WA-IMP-020 | B-016: previewSign inner fixture includes non-required algorithm field | Test owner adds minimal flags-only inner shape; fixture commentary and decoder evidence |
| WA-IMP-021 | B-R1: previewSign two-credential test does not assert returned-ID membership before selecting verification key | Test owner strengthens ID association assertions; test summary and returned-credential evidence |
| WA-XREF-001 | A-002: CTAP2 GetAssertionResponse.User repeats inaccurate UV/discoverable restriction | Future CTAP2 documentation cycle; User entity/handle/display fields |

Core discovery/SCP and OATH follow-ups remain deferred separately. DOC-002 migration YAML parse failure is pre-existing and no map edit is proposed.

## Coverage and next gates

All 74 source + 47 test files have complete semantic dispositions in A/B reports; C reconciles 44 public types/542 snapshot entries and all 3 Markdown + 7 support files. C covers 25 active external-reference files plus 4 supplemental migration files; historical/generated exclusions are explicit. This establishes inventory/investigation coverage, not completed review or runtime verification.

Next: sequential fresh editors, fresh independent Sol review/correction loop, Astra stratified clean sample, warning-free final full build/unit tests, documentation/architecture/complexity checks, scoped formatting and executable-token diff check. Compile the guide's example shape if practical without hardware. Integrate on `yubikit-docs-audit` and push reviewed work to its existing remote; do not merge the whole campaign to another branch here.
