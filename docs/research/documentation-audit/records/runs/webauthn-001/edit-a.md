# Editor A — WebAuthn cycle 001

**Independent review erratum:** the A-Q1 citation to CTAP2.1/2.3 below is superseded by `review-a.md` R1
and coordinator verification. Permitted singleton omission is CTAP2.0 PS §5.2; later 2.1/2.3 tables require
the descriptor. R1 and R2 are pending narrow correction/re-review, so this initial batch is not yet accepted.

2026-11-03; fresh GPT-6.1 Sol editor; baseline `e353b49afa15d1d104fce70bbf1e1abc59ddb132`, branch `yubikit-docs-audit`.

## Outcome and boundaries

Applied the accepted WA-A-001–022 C# documentation/comment portions, except WA-A-020's module Markdown handoff to editor C, plus the A-Q1 missing-ID limitation. Exactly 36 repository C# files changed. No executable tokens, declarations, API shapes, test assertions, test names, runtime messages, project metadata, or repository Markdown changed. All edits used patch. No builds, tests, formatter, hardware operations, commits, or delegation.

Read root/WebAuthn CLAUDE, approved style, canonical webauthn-plan, cycle status, triage, and investigation-a; read Core/CTAP2 CLAUDE before narrow dependency checks. Rechecked actual ceremony/catch/selection construction, public constructors and disposal, decoder/copy behavior, default Aaguid implementation, UV decisions, factory forwarding, and affected test assertions. Read every resulting changed comment/XML block in full, with surrounding declaration/implementation context. This is editorial/source verification, not runtime or hardware qualification.

The four original untracked files were not edited: `docs/research/audit-v2-closeout-review.md`, `docs/research/audit-v2-execution.md`, `docs/research/audit-v2-jira-traceability.md`, and `mise.toml`. A separate untracked `docs/research/webauthn-documentation-audit-checkpoint.md` appeared during the edit session; it is not editor A work and was left untouched.

## Finding-to-change disposition

| Finding | Applied correction |
| --- | --- |
| A-001 | Public authentication/result/selection explanations distinguish narrowed allow lists from discoverable enumeration, zero/one/multiple matches, already-built responses and completed extension side effects. SelectAsync retrieves a cached response without device I/O; cancellation does not undo work. Dummy-descriptor rationale no longer promises timing/prompt equivalence. Matcher no longer claims to perform probing. |
| A-002 | User entities are authenticator-supplied, not conditional on UV or restricted to discoverable credentials. Handles and verified RP account binding are distinguished from optional display data. |
| A-003 | Enterprise is explicitly not forwarded as a CTAP enterprise-attestation request; Indirect has no anonymization service. Creation option, preference enum, and registration implementation comment agree. None's self-attestation exception is retained. |
| A-004 | Error examples match InvalidState exclusion, NotAllowed PIN rejection/client timeout, Constraint authenticator timeout, and application-RP rejection as Security. Cancelled is not presented as the ceremonies' caller-token result. No enum values changed. |
| A-005 | Both public methods and internal mapping comments identify incomplete normalization. Discovery/token/output/transport failures can escape; output work stays outside retry. The main-command test comment now describes its specific assertion rather than a universal mapping guarantee. |
| A-006 | Both Timeout options, deadline/helper comments, and hardware-test XML describe cooperative cancellation, delayed transport response, success races, post-validation/RP-callback start, no default/clamp, nonpositive rejection, and framework-supported delay restriction. Authentication says allow-list selection. Keep-alive cadence remains SHOULD, and test latency slack is an assumption. |
| A-007 | Both Challenge properties name RP generation/freshness duties and distinguish sufficient entropy from the recommended 16-byte length; SDK nonempty validation is explicit. |
| A-008 | Preferred UV comment keeps WebAuthn L2 and corrects §5.4.2 to §5.8.6. SDK enabled-method/availability decisions are distinguished from the specification preference. |
| A-009 | Hardware-test explanation states modern-device prerequisites and missing guards. SDK omission of makeCredential up is compatibility policy, not a CTAP2.1/2.3 prohibition of up:true; required presence is not universally a separate touch. Assertions/runtime messages remain unchanged. |
| A-010 | Removed stale Required-UV failures, corrected absent-uv and three-token/cached-probe explanations, reversed two mislabeled InlineData comments, repaired both dangling crefs to existing invalid-input tests, removed completed-refactor/reflection narration, and qualified the internal slow-factory test. |
| A-011 | Removed obsolete backend implementation-phase remarks, retaining the raw processor-built CBOR passthrough explanation. |
| A-012 | Client-data docs describe manual escaping and always-emitted crossOrigin; public builder validation boundary and exact CCDToString control-escape limitation are explicit. The contradictory topOrigin test is labeled a low-level builder case, not a valid ceremony example. |
| A-013 | Public class/constructors explain successful ownership transfer, retained caller-owned prompt, response-buffer disposal boundary, and RP server verification. Attestation Decode/RawCbor describe borrowed input/slices versus copied authenticator data; Create retains components and allocates raw CBOR; Encode rebuilds modeled fields. Authenticator-data Decode describes independent storage and parsing-versus-verification. |
| A-014 | Default Aaguid's empty span, Guid.Empty projection, and nonreflexive equality are explicitly a known limitation. Constructor guidance is provided; implementation and equality operators remain unchanged. Internal storage rationale names public rather than secret data. |
| A-015 | Backend protocol creation comment describes a stateless object and per-operation ClientPin key agreement, not deferred asynchronous initialization. |
| A-016 | Attestation encoding comment describes writer-controlled CTAP canonical encoded-key order, not plain text lexicographic sorting. |
| A-017 | Stress-test docs identify a new random final user ID and RP-scoped exclusion, metadata-based capacity, model/firmware variance, connection ownership, explicit operator-approved destructive reset, and RP cleanup. Removed unpinned sibling line references and unsupported shared-slot/protocol-contamination theories. |
| A-018 | Encapsulation test docs identify derived-secret/access-guard evidence and the missing retained-view zeroing assertion. TokenBufferAssert describes its actual any-nonzero check rather than exact sentinel preservation. Assertions and test names remain unchanged. |
| A-019 | Factory XML states automatic SmartCard selection with SCP, explicit HID/SCP rejection, firmware/FIDO-AID qualifications, NFC distinction, and no retry on connection-opening failure. |
| A-020 | No A repository edit: all module Markdown portions remain editor C's exclusive assignment. Dependent C# corrections from A-001/004/005/008/009/019/021/022 are included here. |
| A-021 | Public class and concrete backend qualify protocol-2-only PIN/UV/key-agreement support without claiming all ceremonies need it. UV decision notes the missing pinUvAuthToken capability check. Internal key-agreement and test citations distinguish §6.5.5.4 exchange from protocol-2 §6.5.7 derivation; token remint references token state and command checks rather than acquisition alone. |
| A-022 | Origin docs state HTTPS/HTTP localhost and .localhost support without full Secure Contexts/browser conformance; helper does not resolve names or accept loopback IPs. Transport anchor and contact-smart-card/hybrid meanings corrected. |
| A-Q1 | GetAssertionAsync, MatchedCredential.Id, AuthenticationResponse.CredentialId, matcher and response-construction comments disclose lost final-descriptor recovery. Only an original single-entry allow list provides an unambiguous application-known requested ID; no text advises taking the first of several allowed IDs. RP verification remains required. |

## Exact changed paths and finding mapping

Paths are repository-relative; `A-` IDs refer to investigation-a and the disposition table above.

| Path | Findings |
| --- | --- |
| `src/WebAuthn/src/Attestation/WebAuthnAttestationObject.cs` | A-013, A-016 |
| `src/WebAuthn/src/Client/Authentication/AuthenticationResponse.cs` | A-001, A-002, A-Q1 |
| `src/WebAuthn/src/Client/Authentication/CredentialMatcher.cs` | A-001, A-Q1 |
| `src/WebAuthn/src/Client/Authentication/MatchedCredential.cs` | A-001, A-002, A-Q1 |
| `src/WebAuthn/src/Client/Authentication/PublicKeyCredentialRequestOptions.cs` | A-006, A-007 |
| `src/WebAuthn/src/Client/CeremonyDeadline.cs` | A-006 |
| `src/WebAuthn/src/Client/IWebAuthnBackend.cs` | A-011, A-021 citation context |
| `src/WebAuthn/src/Client/PinUvSharedSecret.cs` | A-021 citation context |
| `src/WebAuthn/src/Client/Registration/PublicKeyCredentialCreationOptions.cs` | A-003, A-006, A-007 |
| `src/WebAuthn/src/Client/UserVerification/UvDecision.cs` | A-008, A-021 |
| `src/WebAuthn/src/Client/WebAuthnBackend.cs` | A-015, A-021 |
| `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs` | A-001, A-005, A-006, A-Q1 |
| `src/WebAuthn/src/Client/WebAuthnClient.Ceremony.cs` | A-005, A-021 citation context |
| `src/WebAuthn/src/Client/WebAuthnClient.PinUvAuth.cs` | A-005 |
| `src/WebAuthn/src/Client/WebAuthnClient.Registration.cs` | A-003, A-005, A-006 |
| `src/WebAuthn/src/Client/WebAuthnClient.Validation.cs` | A-004, A-005, A-006 |
| `src/WebAuthn/src/Client/WebAuthnClient.cs` | A-013, A-021 |
| `src/WebAuthn/src/Client/WebAuthnClientData.cs` | A-012 |
| `src/WebAuthn/src/Client/WebAuthnOrigin.cs` | A-022 |
| `src/WebAuthn/src/Cose/Aaguid.cs` | A-014 |
| `src/WebAuthn/src/IYubiKeyExtensions.cs` | A-019 |
| `src/WebAuthn/src/Preferences/AttestationPreference.cs` | A-003 |
| `src/WebAuthn/src/WebAuthnAuthenticatorData.cs` | A-013 |
| `src/WebAuthn/src/WebAuthnClientError.cs` | A-004 |
| `src/WebAuthn/src/WebAuthnTransport.cs` | A-022 |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/AuditV2/WebAuthnAuditHardwareReproTests.cs` | A-006, A-010 |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/AuditV2/WebAuthnAuditHardwareTests.cs` | A-010 |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/WebAuthnClientTests.cs` | A-009 |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/WebAuthnExcludeListStressTests.cs` | A-017 |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/UserVerification/UvDecisionLogicTests.cs` | A-010 |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnBackendTests.cs` | A-018, A-021 citation context |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientCeremonyFrameTests.cs` | A-010 |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientDataTests.cs` | A-012 |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientGetAssertionTests.cs` | A-010 |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientMakeCredentialTests.cs` | A-005, A-010 |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/TestSupport/TokenBufferAssert.cs` | A-018 |

## Evidence and verification

- Used investigation-a's source/citation index C1–C12 as the accepted evidence; retained existing specification levels rather than upgrading every reference. In particular Preferred retains WebAuthn L2 §5.8.6; None/self attestation uses L3 semantics; CTAP2.1/2.3 makeCredential permits up:true but not up:false.
- Narrow source follow-up: CTAP2 `AuthenticatorData.Parse` copies input/fields; `GetAssertionResponse.Decode` leaves credential null when omitted; `ClientPin` permissions fallback retains the supplied protocol and UV-token path lacks capability checks; CTAP2 factory selects SmartCard when SCP is supplied without an explicit preference; Core HID receive returns actual success racing cancellation. No dependency files edited.
- Context7 independently returned .NET 10 `CancellationTokenSource(TimeSpan)` supported-delay/ArgumentOutOfRangeException documentation. No numeric delay maximum was invented in public XML.
- Focused primary recheck of CTAP2.1 PS 2021 with 2022 errata confirmed §6.5.2 token state, §6.5.3.2 maintenance, and the token acquisition distinction at §6.5.5.7. Primary URL: `https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html` (accessed 2026-11-03).
- `git diff --check`: exit 0, no findings.
- Read-only Python comparison against HEAD: executable tokens unchanged in all 36 C# files, preserving string/character literals while ignoring comments and external whitespace. No non-C# tracked edits.
- Read-only XML structural parse: all 254 XML comment blocks in the 36 changed files parse. This is not compiler cref resolution or documentation generation; repaired crefs were checked against existing method declarations.
- No build/test/format/documentation CLI gate, hardware validation, or cryptographic/server verification is claimed. Final independent review and campaign gates remain the coordinator's work.

## Unresolved implementation and handoffs

No new unresolved documentation question was introduced. Accepted prose does not resolve any engineering item. The triage ledger remains authoritative:

| Deferred item | Required next implementation/evidence step and documentation re-review |
| --- | --- |
| WA-IMP-001 / A-003 | Decide enterprise forwarding/authorization or explicit rejection and any indirect anonymization policy; reopen preference/option/backend/client/guide text. CTAP2.3 §6.1 enterpriseAttestation and WebAuthn L3 §5.1.3/§5.4.7 are the primary policy references. |
| WA-IMP-002 / A-005 | Normalize failures without putting post-creation output processing inside retry. Reopen public method/error and extension-output guidance. |
| WA-IMP-003 / A-006 | Decide oversized timeout admission and completed-success race policy. Reopen both Timeout options, deadline/method docs, and hardware assumptions; no hard-deadline guarantee added here. |
| WA-IMP-004 / A-009 | Test owner must add actual makeCredUvNotRqd/alwaysUv applicability guards before claiming portable no-UV hardware assertions. Existing runtime assertion strings remain untouched under the no-token-change rule. |
| WA-IMP-005 / A-012 | Implement/decide exact CCDToString control and surrogate behavior against dated WebAuthn L3 REC §5.8.1.1; reopen builder XML/tests and conformance claims. |
| WA-IMP-006 / A-013 | Decide snapshot semantics for attestation Decode; reopen RawCbor/Decode/Create/Encode and extension attestation consumers after ownership changes. |
| WA-IMP-007 / A-014 | Fix default Aaguid representation/reflexive equality and add behavioral evidence separately; reopen public default/equality/span docs. Current prose explicitly calls it a limitation. |
| WA-IMP-008 / A-018 | Test owner must retain the actual shared-secret memory view before disposal and inspect it afterwards to establish zeroing; a pre-disposal copy/access guard cannot do so. Test name remains untouched. |
| WA-IMP-009 / A-021 | Implement protocol negotiation and built-in-UV token capability checks; reopen client prerequisites, backend/key-agreement, UV and extension guidance. CTAP2.3 §6.5.5.7, §6.5.6 and §6.5.7 distinguish token support and protocol versions. |
| WA-IMP-010 / A-Q1 | Carry the selected descriptor into final response construction when CTAP permits omission, then add fake-response evidence. Reopen both ID properties, GetAssertionAsync/selection guidance and credential-specific extensions. CTAP2.1/2.3 §6.2.2 is the exact primary omission follow-up. Never recover by guessing the first ID of an original multi-entry list. |
| WA-XREF-001 / A-002 | Future CTAP2 documentation cycle must correct `GetAssertionResponse.User`'s UV/discoverable-only restriction using CTAP2.3 §6.2.2; no cross-module edit here. |

Editor C still owns all A-020 module Markdown and reconciliation of the public guide's selection, protocol-2, timeout/error, ownership/server, origin, attestation and factory limitations. Editor B still owns extension source and its PRF/credProtect/largeBlob/previewSign limitations. The unverified nullable largeBlob conversion hypothesis is not asserted as a runtime outcome in this batch.
