# G — CTAP ↔ WebAuthn boundary investigation

2026-11-03; Investigator G; source baseline independently checked as `5b09a9b6942ded3913b5c9340c3e126617e61a82`. Documentation investigation only. Own writes are this report, `boundary-map.md`, and `evidence/g/spec-quotes.md`; no source edits, builds, tests, hardware, commits or delegation. A–F remain the source/document editing owners. Proposed WebAuthn owners below require explicit coordinator assignment.

Read root and both module CLAUDE/README files, available skill descriptions, canonical workflow/style and this run's plan. Loaded security-guidelines for semantic secret classification; its generic PRD/firmware/touch examples are not protocol authority. Context7 supplied framework memory-lease guidance. Build/test/commit skills are inapplicable because these activities are prohibited.

## Outcome and provenance

The current WebAuthn documentation is substantially consistent with actual boundary source and correctly exposes existing implementation limitations. CTAP documentation still contradicts it on user-entity restrictions, descriptor omission, credProtect enforcement, minimum-PIN policy and UV/token sample guidance. An additional CTAP largeBlob statement contradicts the original 2.3 spec, not the chosen SDK route.

This is **not a complete WebAuthn re-audit**. It independently re-baselines relevant claims and source units. Original WA evidence is preserved: campaign `webauthn.md`, `records/runs/webauthn-001/triage.md`, investigation A (including leading review erratum), investigation B, and relevant investigation C responsibility/support/extension sections. Original WA baseline was `e353b49afa15d1d104fce70bbf1e1abc59ddb132`; completed documentation checkpoint is `9399b94bdc6fe2786903b1b6e58808537aa8c94e`. Reconfirmation is not a new bug count. WA Pass 2 has not started.

Primary-source excerpts, exact revision/status/actor/strength and access date are in [evidence/g/spec-quotes.md](evidence/g/spec-quotes.md). Initial research plus one focused ambiguity follow-up was used; remaining product/device questions are bounded gaps, not permission to implement.

## Candidates for coordinated triage

### G-001 — User entity restrictions: DOC; retain WA-XREF-001

`src/Ctap2/src/Credentials/GetAssertionResponse.cs:24–28,60–66` says user is discoverable-only and UV-only. Decode at 163–165 parses any supplied entity; `PublicKeyCredentialTypes.cs:349–385` permits id-only entities. WebAuthn `AuthenticationResponse.User:56–64`, `MatchedCredential.User:43–50` and authentication construction 219–220 correctly pass through the entity. S21/S23 §6.2.2 requires discoverable id, permits a server-side entity, and prohibits **identifying display fields**, not id, without UV. Owner C; no behavior edit. Reopen WA user/picker/RP-handoff docs if parsing/filtering changes. Preserve the original A-002 provenance.

### G-002 — Descriptor omission: DOC + existing IMP; retain WA-IMP-010

CTAP response remarks 25/40–42 overgeneralize omission to one matching credential or non-discoverable credentials. S20 §5.2 permits omission only for exactly one descriptor in allowList; original S21, errata S21 and S23 §6.2.2 mark descriptor required. A's leading erratum supersedes its old later-version attribution. CTAP parser is permissive and GetCredentialId returns empty; WebAuthn matcher 59–74 and response builder 216–217 still lose final selected ID. Probe singleton recovery (`CredentialFilter:130–138`) does not fix final construction. Owner C for CTAP docs, explicit proposed WA-client owner for future engineering. Do not guess first original multi-entry ID or redesign recovery here.

### G-003 — Direct CTAP PIN guidance emits mutually exclusive fields: DOC + source/example IMP candidate

CTAP README 116–158 says setting UV alone is rejected and combines `WithPinUvAuth` with `WithUserVerification(true)`; FidoTool MakeCredential 112–114 and GetAssertion 90–92 do the same. `Ctap2SessionRequestEncoding:59–64,98–103,154–157,181–184` encodes both fields and does not enforce the exclusion. S23 §6.1/§6.2 platform rule forbids uv **key**, even false, beside pinUvAuthParam. Conversely uv:true can request built-in UV without a PIN token on a capable authenticator. WebAuthn UvDecision/adapter correctly keeps uv absent with tokens. Owners C (options/encoding), F (guides/executable sample), B consulted (token explanation). Markdown/XML can be corrected; executable FidoTool changes and CTAP request validation are **deferred**. Do not force the WebAuthn policy into every raw CTAP caller.

### G-004 — Protocol negotiation and built-in UV: existing IMP only; retain WA-IMP-009

`WebAuthnBackend:285–292` constructs V2, used for token and extension key agreement. `ClientPin:343–352` falls back to legacy **command**, preserving supplied protocol; it does not choose V1. `UvDecision:107–116` selects built-in UV on uv alone, whereas S23 §6.5.5.7.3 guarantees the token subcommand only with uv AND pinUvAuthToken true. Current WA README requirements/CLAUDE/client XML correctly qualify these limitations. Direct CTAP's caller-selected V1/V2 is a legitimate layer difference, not a reason to claim universal facade compatibility. Proposed explicit WA-client/UV owner, B dependency. Reopen all protocol prerequisites and PRF context on negotiation/capability changes. Device support not newly verified.

### G-005 — PRF translation vs verified variant: DOC linkage + existing IMP; retain WA-IMP-011/013

The SHA-256("WebAuthn PRF" || 0x00 || input) mapping, encrypted CTAP hmac-secret salts and result decryption are correctly divided between layers. `PrfProcessor.Permissions` is None and ceremonies pass original UV preference; W3 §10.1.4 MUST select verified PRF and override preference. Correct CTAP two-variant behavior is not an exception authorizing two WebAuthn PRFs. WA public/architecture limitations are truthful; CTAP CLAUDE 224–231 should link the limitation and correct its unqualified §12.7 citation (2.1 is §12.5). `ExtensionBuilder:183–208` leaves plaintext saltData uncleared; `PrfCodec:79–82` creates unowned range/copy arrays while only clearing aggregate. Owners E/F and proposed WA-extension owner; no cleanup or UV fix now.

### G-006 — credProtect forwarding is not enforcement: DOC + existing IMP; retain WA-IMP-012

`ExtensionBuilder.WithCredProtect` XML promises failure for enforcePolicy; field assigned 64 but encoding 330–334 emits only policy integer. WA factory omits unsupported input and processor only forwards/parses. WA docs already disclose this. S23 §12.1 places conditional **SHOULD NOT** on platform for policies 2/3 with enforcement true. No wire boolean prevents creation. Owner E; proposed WA-extension owner for engineering. Output inspection after creation is not prevention; stronger device policy need not imply incorrect enforcement simply because unequal.

### G-007 — largeBlob route/support/cardinality/errors: DOC + existing IMP; retain WA-IMP-014–017/013

`ExtensionBuilder:108–112` says CTAP carries no largeBlob:{support}, then names later direct extension. S23 §12.4 explicitly has makeCredential support required/preferred, so qualify the claim as this builder's chosen route. CTAP CLAUDE's extension table also names `.WithLargeBlob()`, which does not exist in this builder (full-file symbol search); the implemented method is `WithLargeBlobKey()`. Both modules otherwise use largeBlobKey/storage and correct top-level key location. WA Required can return Supported=false for nonresident/missing-key registration; writes lack original allow-list count check. Narrowing to one CTAP descriptor does not satisfy W3's original-options check. SDK token-only write restriction is a disclosed legitimate support/policy limitation relative to conditional CTAP authentication, **not automatically a required feature addition**. Reads can throw after assertion; absent blob conversion distinction remains historical experiment plus source reconfirmation, not G execution. Owners E/F; proposed WA-extension owner. Reopen support/output/UV/selection/cleanup docs together after any change.

### G-008 — minPinLength reports, not sets policy: DOC + existing IMP; retain WA-IMP-018

CTAP CLAUDE extension table 198 says “Require minimum PIN length”; S23 §12.5 reports current policy in Unicode code points for authorized RP IDs. `MinPinLengthInput.Encode:49–54` emits Requested, but WA factory checks object presence and `MinPinLengthProcessor` emits true regardless. Current WA docs truthfully disclose false handling. Owner E (XML) / F (Markdown); proposed WA-extension owner for behavior. Use null to omit in current facade; do not make false ignorance a normative contract.

### G-009 — presence, cancellation, retries and failure provenance: DOC + existing IMP; retain WA-IMP-002/003

CTAP README 70/165–170 overstates “touch and nothing else”, all ClientPin commands silent, and cancellation always reported after drain. Built-in UV token acquisition requires user interaction; getAssertion up:false is permitted separately; success may win cooperative cancellation. WA guide correctly distinguishes UP/UV, callbacks and cancellation. CTAP backends resolve known CTAP status before ownership transfer; callback error can occur after successful device side effect. Main WA command mapping does not include GetInfo, token acquisition or output construction; output errors must not enlarge retry scope. Prompt retries only PinInvalid with a fresh secret; terminal blocked states stop; caller PIN is not blindly retried; SDK MaxPromptAttempts is not device retry count. Owners A/B/F; proposed WA-client owner if mapping/deadline policy changes. No rollback, exact gesture/timing or universal normalized-error guarantee.

### G-010 — requirements conflate standards/firmware/capability: DOC + bounded evidence gap

CTAP README first sentence broadly identifies the FIDO application as CTAP2.1/2.3 and requirements say “NFC always”; WA README inherits NFC phrase and lists firmware 5.0+ while explicitly qualifying V2 token operations. Revision advertisement, optional extension advertisement, enabled configuration, transport FIDO AID, firmware minimum and authorization are different checks. CTAP CLAUDE itself more carefully conditions NFC on current PC/SC Transport.Nfc and FIDO AID. Bio firmware minimum does not prove fingerprint hardware; hmac-secret-mc advertised support is not established by inconsistent 5.4+ feature table vs builder “5.8 and later”. Owners A/F (capabilities/guides), E consulted (extension qualification), proposed explicit WA-doc owner for inherited requirements sentence. Exact SKU/firmware qualification remains for A/E/F primary product evidence; G does not invent a replacement broad support matrix.

### G-011 — selected-response display retention: unresolved actor boundary, proposed needs-decision

S21/S23 §6.2.2 says platform account picker forwards ONLY id to WebAuthn and discards other user details. SDK exposes full user entities on matches **and** final AuthenticationResponse without stripping. Its custom native client intentionally exposes picker data and has no WebAuthn-response JSON serializer; the README handoff tells application to send the handle, not display fields. Therefore no forced DTO redesign or new security-defect count is justified by this bounded investigation. Proposed explicit WA-client owner must decide/document whether AuthenticationResponse is a native application result or a browser-equivalent RP-facing response boundary before claiming exact privacy/conformance behavior. Owner C only for raw CTAP field truth. Pass-2 trigger: serializer, selected-response privacy policy or conformance claim. Full browser privacy/actor audit not undertaken.

## Inspected path/semantic-unit manifest

All paths repository-relative. `full` means the file was read completely as a **boundary dependency**, not a claim of complete owning-module audit. Explicit ranges are partial and exclude the rest.

- Instructions/narratives full: `CLAUDE.md`; `src/Ctap2/CLAUDE.md`, `src/Ctap2/README.md`; `src/WebAuthn/CLAUDE.md`, `src/WebAuthn/README.md`; `docs/SDK-HOUSE-STYLE.md`; `docs/research/documentation-audit/workflow.md`, `records/style.md`, `webauthn.md`, `modules/ctap2/pass-1/ctap2-001/plan.md`; `docs/architecture/webauthn-extension-processing.md`; `docs/usage/user-interaction.md` (boundary callback/WA example; other applets not audited). `docs/v2-highlights.md:1–65` (WebAuthn section).
- Historical records: `docs/research/documentation-audit/records/runs/webauthn-001/triage.md` full; `investigation-a.md:1–120` including erratum/A-002 and error/policy findings; `investigation-b.md:1–292` including citation and dependency index; `investigation-c.md` focused returned search excerpts for WA-C-002/004/005/008/010/012 and C1–C11/C14 citations (not full coverage).
- CTAP full: `src/Ctap2/src/Credentials/GetAssertionResponse.cs`, `PublicKeyCredentialTypes.cs`, `MakeCredentialOptions.cs`, `GetAssertionOptions.cs`; `src/Ctap2/src/Cbor/Ctap2SessionRequestEncoding.cs`; `src/Ctap2/src/Backend/HidBackend.cs`, `SmartCardBackend.cs`; `src/Ctap2/src/Extensions/MinPinLength/MinPinLengthInput.cs`; `src/Ctap2/examples/FidoTool/FidoExamples/MakeCredential.cs`, `GetAssertion.cs` (complete helper flows, not complete FidoTool journey).
- CTAP partial: `src/Ctap2/src/Ctap2Session.cs:208–408` (make/get/next/raw exchange); `src/Ctap2/src/Pin/ClientPin.cs:1–500` (retry/status/PIN/token/key agreement); `src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs:24–418` (fields/inputs/wire emission); `src/Ctap2/src/LargeBlobs/LargeBlobStorage.cs:115–204` (array write/read and decrypted blob result).
- WA client full: under `src/WebAuthn/src/Client/`: `WebAuthnClient.cs`, `WebAuthnBackend.cs`, `WebAuthnClient.Authentication.cs`, `WebAuthnClient.Ceremony.cs`, `WebAuthnClient.PinUvAuth.cs`, `UserVerification/UvDecision.cs`, `CredentialFilter.cs`, `PinUvAuthTokenSession.cs`, `PinUvSharedSecret.cs`, `Authentication/AuthenticationResponse.cs`, `Authentication/MatchedCredential.cs`, `Authentication/CredentialMatcher.cs`. Partial: `WebAuthnClient.Registration.cs:94–278`; `WebAuthnClient.Validation.cs:110–191`.
- WA extensions full under `src/WebAuthn/src/Extensions/`: `Processing/RegistrationExtensionSession.cs`, `Processing/AuthenticationExtensionSession.cs`; `Processing/Processors/PrfProcessor.cs`, `LargeBlobProcessor.cs`, `CredProtectProcessor.cs`, `MinPinLengthProcessor.cs`, `PreviewSignProcessor.cs`; `Codec/PrfCodec.cs`, `PreviewSignCodec.cs`; `Inputs/PrfInput.cs`, `CredProtectInput.cs`, `LargeBlobInput.cs`; `Outputs/PrfOutput.cs`; `PreviewSign/PreviewSignSigningParams.cs`.

## Meaningful clean boundary units

Clean means no additional factual boundary-doc correction found in the named unit; it is not implementation conformance, certification or executed verification.

| ID | Exact unit | Basis / limitation |
| --- | --- | --- |
| G-CLEAN-01 | WA README “Complete registration and sign-in with your RP”, lines 129–146 | Exact-byte handoff and independent RP verification; no invented upload/verification API. |
| G-CLEAN-02 | WA MatchedCredential.SelectAsync XML/body, lines 72–95 | Cached eager response, idempotent lazy wrapper, cancellation of wait not device work. |
| G-CLEAN-03 | WA WebAuthnBackend.MakeCredential/GetAssertion auth-param copies, lines 163–194/226–252 | Copies are intentionally cleared without destroying caller's reused auth param. Not end-to-end response cleanup. |
| G-CLEAN-04 | WA PinUvAuthTokenSession.Token/Dispose, lines 45–86 | Adopts token array, views borrow owner, zeroes on dispose; does not own protocol. No retained-view test run. |
| G-CLEAN-05 | WA PrfInput and PrfOutput public limitation paragraphs | Correct verified-variant/secret-result caveats and caller memory lifetime. Existing IMPs remain open. |
| G-CLEAN-06 | WA extension sessions output catches and Dispose | Exact catch categories, output omission, separate shared-secret cleanup; no universal ignore rule. |
| G-CLEAN-07 | CTAP ExtensionBuilder.WithPreviewSign signing overload, lines 266–280 + WA PreviewSignProcessor 50–108 | One CTAP input vs original-map validation and selected-key lookup are legitimate layer responsibilities. |
| G-CLEAN-08 | WA README registration transport hints, lines 151–164 + Registration.cs 258–263 | Advertised unique/sorted snapshot not active transport; no extra GetInfo request for snapshot. ClientPin may separately query info for its own token decision. |

## Gaps and next handoff

No builds/tests or live UP/UV/PIN/retry/cancellation/storage/signing paths executed. Hardware/OS/firmware/SKU reachability and timing remain unqualified. No full Core transport, cryptographic primitive or test-assertion audit; A–F own those. Nullable conversion uses prior experiment provenance; exact original support/product facts need A/E/F adjudication. Custom facade selected-user privacy boundary remains needs-decision. The final independent boundary review must re-read both modules after edits, especially shared G-003/G-006/G-007 claims, and preserve all deferred WA IDs and side-effect/ownership limitations.
