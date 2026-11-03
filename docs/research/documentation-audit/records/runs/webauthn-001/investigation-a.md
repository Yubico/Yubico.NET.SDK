# WA-A investigation — WebAuthn cycle 001

**Review erratum:** `review-a.md` R1 and coordinator primary-source verification supersede C2/Q1's
descriptor-omission version attribution below. CTAP2.0 PS 2019-01-30 §5.2 permits omission for one
allow-list descriptor; CTAP2.1/2.3 §6.2.2 mark the field required. The empty-ID source finding stands.
Retain this original investigation as history, but use the corrected attribution for edits and follow-ups.

## Scope and verification

Read-only investigation at `e353b49afa15d1d104fce70bbf1e1abc59ddb132`, branch `yubikit-docs-audit`, 2026-11-03. HEAD and status independently checked; tracked tree unchanged and the four baseline untracked files preserved. Read root and WebAuthn CLAUDE, approved style, cycle status, and canonical webauthn-plan. Read Core and CTAP2 CLAUDE before tracing those dependencies.

**Coverage:** all 34 assigned source C# files (4,371 lines counted by `wc -l`) and all 35 assigned test C# files (7,821 physical lines), including complete implementation surrounding comments and public contracts. Both API snapshots read; Shipped contains only `#nullable enable`; assigned declarations, operators, static factories, defaults and effective accessibility checked against Unshipped. No repository edits, generation, builds, tests, device operations, commits, or delegation. Findings are source/spec verification, not runtime verification. Auxiliary dependency reads are explicitly scoped below and are not whole-file audit coverage.

**22 findings:** 15 predominantly documentation/comment corrections (001, 002, 004, 007–011, 015–020, 022); 7 include deferred engineering or consequential contract questions (003, 005, 006, 012–014, 021). These are investigation classifications, not accepted edits. Some findings contain several locations sharing one root cause.

## Findings and minimal dispositions

### WA-A-001 — Authentication selection documentation obscures when work happens and what can be selected

**Locations:** `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs:26–45,100–116,140–154`; `Client/Authentication/MatchedCredential.cs:23–27,69–85`; `Client/Authentication/CredentialMatcher.cs:23–25`.

Public XML says `SelectAsync` completes authentication/packages the assertion and that the authenticator enumerates “all matching credentials.” Actual authentication gathers assertions **and prepares all extension outputs before returning**. Selection merely returns the prebuilt `AuthenticationResponse` through a lazy completed-task wrapper. An allow list is probed, narrowed to **one** descriptor, then asserted; it does not yield every held allowed credential for an application picker. Discoverable enumeration can yield several, subject to authenticator selection behavior. `CredentialMatcher` does not itself perform allow-list probing; `CredentialFilter` does.

**Correction:** explain zero/one/multiple matches; distinguish allow-list narrowing from discoverable enumeration; say assertions and extension side effects have completed when `GetAssertionAsync` returns, and `SelectAsync` retrieves the cached response without device I/O. Preserve idempotent factory semantics without presenting the internal slow-factory test as production behavior. Do not promise that cancelling a selection undoes authentication or extension writes. Describe SDK/server boundary: returned assertions still need RP verification. At Authentication.cs:159–161, replace the unsupported claim that the dummy descriptor makes the ceremony take “the same time” and show “the same prompt” with its actual rationale: still request UP rather than directly expose a silent-probe miss; exact timing/prompt equivalence is not established by code or fake tests.

**Evidence:** GetAssertion tests enumeration/idempotence; ceremony-frame tests allow-list narrowing; authentication builder code. **Dependencies:** README selection journey, CLAUDE:144–147, extension write timing (WA-B), both result types. Re-review if selection becomes genuinely deferred or allow-list policy changes.

### WA-A-002 — User information is not conditional on UV or restricted to discoverable credentials

**Locations:** `Client/Authentication/AuthenticationResponse.cs:49–54`; `MatchedCredential.cs:40–45`.

Both say user information is “Only present for discoverable credentials when user verification is performed.” WebAuthn passes the CTAP `User` entity through without that filter. CTAP 2.1/2.3 §6.2.2 distinguishes **user handle** from **identifying display fields**: discoverable credentials require at least `id`; name/displayName/icon MUST NOT be returned without UV; server-side credentials MAY also return a user entity. Integration `GetAssertion_DiscoverableCredential_ReturnsUserInfo` even requests discouraged UV and expects non-null User.

**Correction:** entity is returned when provided by the authenticator; its handle identifies the account, and display fields may be absent, especially without UV. Do not tell callers to use display names for account authorization. **Citation C2. Dependencies:** result/picker prose, CTAP2 `GetAssertionResponse.User` repeats the same inaccurate sentence (cross-module follow-up, not this cycle's edit).

### WA-A-003 — Enterprise attestation enum promises a request the client never sends

**Locations:** `Preferences/AttestationPreference.cs:41–44`; creation options `Attestation`; `WebAuthnClient.Registration.cs:183–194,212–222`; `IWebAuthnBackend.cs:122–168`; `WebAuthnBackend.cs:147–178`.

`Enterprise` says “Request enterprise attestation (requires authenticator and RP support).” No enterprise-attestation parameter exists in the backend request or is set on `MakeCredentialOptions`. All non-None preferences pass returned statements through. CTAP2 has `MakeCredentialOptions.EnterpriseAttestation` and encoder key 0x0A; WebAuthn does not populate it. CTAP §6.1 says absent parameter leaves privacy characteristics unaffected. This is not a request merely because the returned statement is unmodified.

**Correction now:** explicitly disclose that this client currently passes through returned attestation for Enterprise but does not request CTAP enterprise attestation; do not promote it as working enterprise support. None keeps the L3 self-attestation exception; Indirect currently passes through and performs no anonymization service. **Deferred:** decide implement/validate enterprise request and authorization policy versus unsupported rejection in a separate pass. **C1/C3. Dependencies:** enum, creation option, README attestation guidance, module capability/conformance claims; reopen after any policy/encoding change.

### WA-A-004 — Error enum examples contradict the actual error vocabulary

**Locations:** `WebAuthnClientError.cs:32–55`; `WebAuthnClient.Validation.cs:163–181`; `WebAuthnClient.Ceremony.cs:218–230`.

Credential exclusion maps to **InvalidState**, not the NotAllowed example. PIN auth invalid maps to **NotAllowed**, not the Security example. Caller cancellation propagates `OperationCanceledException`, never this client's `Cancelled` enum member. Ceremony timeout is NotAllowed while device-originated timeout statuses map to Constraint; the single “timeout” example does not explain the distinction.

**Correction:** use actual code-specific examples; document Cancelled as an available code not emitted for caller-token cancellation by these ceremonies, without removing/renumbering it. Distinguish client deadline from authenticator status. **Evidence:** mapping and audit timeout tests. **Dependencies:** README error table and cancellation examples, public method exceptions, CLAUDE error policy.

### WA-A-005 — “Raw CTAP codes never escape” is a false whole-ceremony guarantee

**Locations:** `WebAuthnClient.Validation.cs:157–161`; `WebAuthnClient.PinUvAuth.cs:255–257`; `WebAuthnClient.Ceremony.cs:108–116`; `WebAuthnClient.Registration.cs:44`; CLAUDE:380–382; test `WebAuthnClientMakeCredentialTests.cs:567`.

The mapping catch surrounds `attempt`, **not** initial GetInfo or `auth.AcquireAsync`. Token acquisition catches only four PIN rejection statuses. A GetInfo `CtapException`, or token acquisition failure such as UnsupportedOption, PinPolicyViolation or UvBlocked, can escape unchanged. Extension output preparation is deliberately outside that catch to prevent repeated credential creation, and may propagate additional failures. InnerException also deliberately preserves CTAP details even on mapped errors.

**Correction now:** describe which failures are mapped and state that lower-level capability/PIN/UV/transport failures may propagate; do not promise every failure is `WebAuthnClientError`. Replace the test's universal statement with its actual main-command assertion. **Deferred:** centralize mapping without enlarging retry scope, preserving cancellation and original causes. **Dependencies:** all public ceremony exception prose, error guidance, extension output errors (WA-B), module error policy. Re-review after mapping fix; do not move output work into retry to satisfy prose.

### WA-A-006 — Timeout is a cooperative cancellation signal, not a guaranteed terminal failure

**Locations:** both public options' `Timeout`; `CeremonyDeadline`; `WebAuthnClient.Validation.cs:148–155`; audit hardware timeout XML.

Documentation correctly warns PC/SC drains in-flight exchange, but still says the call “fails with NotAllowed.” The client converts only a resulting `OperationCanceledException`. Core `FidoHidProtocol.ReceiveResponse:302–310` explicitly returns a success racing cancellation; there is no final deadline check in WebAuthn. PC/SC may also finish the last command after the signal. Therefore an elapsed deadline can still yield a successful result. Timeout does not start until after local validation/RP callback. A positive TimeSpan is not the whole input contract: `new CancellationTokenSource(value)` can throw `ArgumentOutOfRangeException` for values beyond the framework's supported delay. The request option also mistakenly refers to authentication's **exclude-list** preflight rather than allow-list selection.

**Correction:** signal/cancel/drain behavior and conditional conversion to NotAllowed; successful completion may win a cancellation race, important because a credential or extension write may already exist. State validation/callback exclusion and the framework-supported delay restriction. Fix allow-list copy/paste. Keep no-default/no-clamping distinction: L3 §15.1 recommends 300–600 s/default 300 s, not the SDK's default.

**Deferred:** decide whether to normalize oversized positive timeouts into InvalidRequest; do not silently introduce hard-deadline semantics that discard completed credential creation. **C4/C5. Dependencies:** timeout tests/comments, both methods, README cancellation, extension side effects. Hardware XML's “at least every 100ms” must preserve CTAP's **SHOULD** and not imply guaranteed scheduling or SDK response bound.

### WA-A-007 — Challenge XML upgrades SHOULD to MUST and omits the responsible actor

**Location:** request options `Challenge:27–33`; counterpart creation `Challenge` is too bare for the same reader responsibility.

“Must be at least 16 bytes of random data” is neither enforced by SDK (only nonempty) nor the precise standard. L3 §13.4.3: challenges MUST have enough entropy to make guessing infeasible and SHOULD therefore be at least 16 bytes. RP generates/stores/checks freshness; SDK does not produce a challenge or verify it server-side.

**Correction:** unpredictable RP-generated challenge; recommend at least 16 bytes, explain SDK checks nonempty, use the same local guidance for registration and authentication. **C6. Dependencies:** examples' application-supplied challenge and server duties.

### WA-A-008 — UV “Preferred” citation points to RP entity, not UV

**Locations:** `Client/UserVerification/UvDecision.cs:146–149`; CLAUDE:237.

WebAuthn **L2 §5.4.2 is PublicKeyCredentialRpEntity**, not Preferred UV. L2 §5.8.6 defines Preferred as preferring UV if possible without failure solely for missing UV. The enabled/configured-option gates and PIN-first selection are SDK/CTAP decisions, not a direct quotation of §5.4.2. Also Preferred can proceed without UV if a PIN is configured but unobtainable (`Decide:117–130`), which CLAUDE's token table omits.

**Correction:** retain L2 target and point to §5.8.6/enum anchor; describe actual method availability as an SDK decision. Do not mechanically replace it with L3. **C7. Dependencies:** CLAUDE UV table, UserVerificationPreference, PIN prompt guidance.

### WA-A-009 — Absolute statements that CTAP forbids any makeCredential `up` key are wrong

**Locations:** integration `WebAuthnClientTests.cs:225–227`; CLAUDE:468–472. The same test:214–240 also unconditionally promises discouraged UV must produce no UV on a PIN-set key.

CTAP 2.1 errata and 2.3 §6.1 allow `up:true`; they prohibit `up:false`, and warn CTAP2.0 rejects any `up`. This SDK appropriately omits it. User presence is not always a separate physical “touch”: NFC establishes presence through the field gesture, and built-in verification can satisfy UP. Discouraged does not override alwaysUv or the older makeCredential UV policy; code explicitly handles these in UvDecision. The unqualified hardware-test expectation has no `makeCredUvNotRqd`/alwaysUv guard.

**Correction:** SDK omits `up` for compatibility, required-UP policy remains; qualify “touch” to actual transport/authenticator interaction. Describe test's intended modern authenticator prerequisites rather than asserting all devices must skip UV. **Deferred test behavior:** capability gating needs separate implementation/test-owner decision; wording alone cannot make the unguarded hardware assertion portable. **C3. Dependencies:** UP prose, UV table, test prerequisites, module Gotchas.

### WA-A-010 — Stale test explanations and dangling XML after completed changes

**Locations:**

- `IntegrationTests/AuditV2/WebAuthnAuditHardwareReproTests.cs:82–83,108–109` and `WebAuthnAuditHardwareTests.cs:40,57`: Required UV “currently fails”/“Today SDK sends both” is stale. UvDecision always leaves UvOption null for token decisions and backend preserves absence; the Required UV hardware test itself expects success.
- `UnitTests/Client/UserVerification/UvDecisionLogicTests.cs:259,304–305`: Required supposedly pins `uv:true`; the actual assertion at 320 is Null. InlineData:328–329 comments have their cases reversed: `(uv:false, clientPin:false, pinAvailable:true)` is no configured PIN; `(false,true,false)` is configured but unobtainable.
- `WebAuthnClientMakeCredentialTests.cs:485–486`: says retry reruns preflight and holds **four** token buffers; implementation caches probe and assertion expects **three**.
- `WebAuthnClientCeremonyFrameTests.cs:33`: “before it is refactored” is completed-development narration; partial ceremony frame exists.
- `WebAuthnClientMakeCredentialTests.cs:512` and `WebAuthnClientGetAssertionTests.cs:383`: cref names `*WhenExtensionBuildFails_NeverComputesPinUvAuthParam` no longer exist. Their replacement invalid-extension-input tests are present later in each file.
- `WebAuthnClientGetAssertionTests.cs:515`: “Use reflection or create via internal constructor pattern” while directly using constructor; remove narration.

**Correction:** present behavior and test intent; remove obsolete defect isolation and scaffolding; repair crefs to existing tests, preserving useful control-test rationale. **Dependencies:** C3 and source behavior above; no claim hardware was rerun.

### WA-A-011 — Backend XML contains obsolete project phases

**Location:** `Client/IWebAuthnBackend.cs:91,99,150`.

“Implemented in Phase 4” and “opaque passthrough for Phase 3” describe old implementation stages. All methods are implemented, and extension processors now build the map. **Correction:** remove project phase remarks; preserve raw CBOR passthrough explanation. No TODO quota, and no removal of legitimate PIN key-agreement/protocol phases. **Dependencies:** none beyond backend methods.

### WA-A-012 — Client-data escaping documentation names code that is not used; exact L3 serialization has a narrow gap

**Locations:** `Client/WebAuthnClientData.cs:30–31,94–98,141–182`; client-data tests:104–125.

Claim “String escaping uses System.Text.Json.JsonEncodedText” is false: a manual loop handles escapes. The key order and always-present crossOrigin agree with L3 §5.8.1.1; the two remarks still say crossOrigin is conditional. However CCDToString requires control code points encoded as `\\u` plus four lowercase hex digits, whereas loop uses short `\\b`, `\\f`, `\\n`, `\\r`, `\\t`. Ordinary JSON parsers accept these but the standard's restricted serialization algorithm differs. Public Create accepts arbitrary nonblank type and arbitrary topOrigin text, so this is not limited to impossible inputs. The topOrigin test intentionally produces a contradictory crossOrigin:false/topOrigin value; it does not demonstrate a valid ceremony (client methods reject that combination).

**Correction now:** say manual JSON escaping and correct unconditional crossOrigin description; explain Create is a low-level builder and ceremony option validation is separate. Do not label short escapes exact CCDToString conformance; retain exact-order requirement with L3 §5.8.1.1 citation. Add an honest limitation if full serialization conformance is claimed externally. **Deferred:** exact CCDToString implementation/control/unpaired-surrogate contract; no replacement serializer during documentation pass. **C8. Dependencies:** client-data XML/tests, cross-origin examples, conformance claims. Re-review after serializer fix.

### WA-A-013 — Decoder ownership and decode-versus-verify boundaries are missing from public XML

**Locations:** `Attestation/WebAuthnAttestationObject.cs:46–49,61–66,115–143`; `WebAuthnAuthenticatorData.cs:74–85`; `Client/WebAuthnClient.cs:21–35,45–54,84–92`.

Attestation Decode retains the **caller's original** CBOR as RawCbor and takes attStmt slices from it; other parsed authData is copied. Caller mutation can make raw/parsed representations disagree. WebAuthnAuthenticatorData's CTAP parser copies raw/authenticator subfields; it does not retain caller's raw buffer. Encode rebuilds the known 3-field map rather than returning RawCbor, so it need not reproduce unknown fields/order/noncanonical input exactly. Neither decode validates attestation/signature trust or origin/challenge/account policy. Public client owns supplied session on successful construction, not on rejected construction; prompt is retained/used but is not disposed by WebAuthn.

**Correction:** explicit borrowed versus independently materialized byte storage and keep-input-unchanged lifetime; decode is parsing, not cryptographic/RP verification; Encode creates a fresh representation of modeled components. Move source-file split explanation out of public class remarks in favor of session ownership, disposal, and server responsibilities. Clarify constructor ownership transfer on success and prompt service ownership.

**Deferred:** decide consistent snapshot semantics for public decoder (not silently clone as docs edit). **Dependencies:** README lifecycle, generated XML, result consumers, WA-B GeneratedSigningKey attestation use. Re-review after ownership changes.

### WA-A-014 — Default Aaguid violates the advertised 16-byte and equality expectations

**Location:** `Cose/Aaguid.cs:29–31,59–63,71–88`; API snapshot exposes parameterless value construction.

`default(Aaguid).AsSpan()` is empty, while XML says 16 bytes. Value is Guid.Empty, but Equals returns false whenever either backing array is null: default does not equal itself or a constructed 16-zero-byte Aaguid. This is a genuine value-type defect, not an intended contract inferred from implementation. Tests cover initialized values only. Internal “safe per CLAUDE.md exception” is an unnecessary policy citation; local reason is that data is public, not sensitive.

**Correction now:** qualify AsSpan for uninitialized default and document constructors as valid 16-byte instances; keep defect in deferred ledger rather than imply normal equality is guaranteed for default. **Deferred:** choose zero-value semantics and reflexive equality, add behavioral regression tests separately. **Dependencies:** Aaguid public XML, registration Aaguid consumption, equality/default tests; reopen prose when fixed. No runtime test performed.

### WA-A-015 — Backend protocol initialization comment describes a nonexistent async initialization step

**Location:** `Client/WebAuthnBackend.cs:284–290`.

The method constructs a stateless PinUvAuthProtocolV2. ClientPin performs getKeyAgreement and encapsulation per operation; there is no deferred async protocol initialization. CTAP2 CLAUDE:149–153 explicitly documents this. **Correction:** remove misleading two-line explanation or explain lazy object construction only. Preserve backend ownership/disposal. **Dependencies:** backend/ClientPin explanation, C9.

### WA-A-016 — Attestation encoding comment confuses text order with encoded-key canonical order

**Location:** `Attestation/WebAuthnAttestationObject.cs:161`.

“CBOR text string keys are sorted lexicographically” is incomplete/incorrect for these keys: CTAP canonical key order sorts lengths before lexical order for same major type. Thus `fmt`, `attStmt`, `authData`, not the source write order `authData`, `attStmt`, `fmt`. CborWriter canonical mode handles encoded ordering, but comment should not suggest ordinary string lexicographic sorting is the rule. **Correction:** say writer emits CTAP canonical encoded-key order, or omit redundant key-order parenthesis. **C10. Dependencies:** fixture encoding explanations; do not change bytes as a prose correction.

### WA-A-017 — Exclude-list stress-test comments misidentify the account and overstate capacity rationale

**Location:** `IntegrationTests/WebAuthnExcludeListStressTests.cs:31–57,61–66,87–91`.

The final request creates a **new random user ID**, not “same RP and user.” Exclusion is because the authenticator recognizes a listed credential for that RP, not a same-user comparison. Capacity text bundles fingerprint enrollments/FIDO configuration as if all consume the same discoverable credential slots; no local evidence establishes that. Comments also assert fresh sessions avoid PIN/UV protocol state contamination, while current ClientPin protocols are stateless and each token request does key agreement; use fresh-session rationale grounded in connection/session ownership rather than an unverified old failure explanation.

**Correction:** exact different-user request/excluded credential relationship; say available discoverable capacity may be insufficient and use reported metadata, qualify firmware/model-dependent “~25” and avoid unsupported shared-pool theory. Retain operator reset requirement, human approval, destructive-cleanup warning, and legitimate session ownership rationale. Sibling Android line numbers without pinned revision are corroboration only, not current requirements. **Dependencies:** test contributor instructions and destructive-run guidance. No hardware reset/cleanup attempted.

### WA-A-018 — Tests claim more zeroing evidence than their assertions establish

**Locations:** `UnitTests/Client/WebAuthnBackendTests.cs:48–52,75–81`; `TestSupport/TokenBufferAssert.cs:66–76`.

Encapsulate test claims the owner is zeroed, but checks only that a **pre-disposal copy remains nonzero** and property access throws after disposal; no retained view is inspected for zeroing. Production PinUvSharedSecret.Dispose does clear its array, so this is a false test-evidence claim, not proof production is wrong. `NotZeroed` says it checks the sentinel pattern but actually checks merely “any nonzero byte.”

**Correction now:** accurately describe what current assertions prove; retain real ownership reasoning. **Deferred test improvement:** inspect a pre-disposal shared-memory view after disposal if owner-zeroing regression protection is wanted; choose exact sentinel check versus accurate weaker helper description. Do not rewrite tests just to match prose. **Dependencies:** zeroing verification summaries; no test execution inferred.

### WA-A-019 — Factory XML incorrectly requires explicit SmartCard selection for SCP

**Location:** `IYubiKeyExtensions.cs:40–46`.

Current CTAP factory chooses SmartCard automatically when SCP parameters are present and there is no explicit transport override (`Ctap2/IYubiKeyExtensions.cs:128–135`). WebAuthn forwards options unchanged. Its own `IYubiKeyExtensionsTransportTests:125–139` asserts this route. Explicit HID+SCP remains unsupported. **Correction:** ordinary default HID then SmartCard, **except SCP defaults to SmartCard**; explicit SmartCard override is allowed but not required. State firmware/FIDO-AID qualifications via CTAP2, and no fallback after connection failure. **Dependencies:** README factory and module factory examples, session option reference.

### WA-A-020 — Independently verified stale module guidance (WA-C prose owner)

**Locations and evidence:**

- CLAUDE:18,23,54–59,359–365,456–457 describe general “CTAP v4”/Fido2 dependency and old Adapter/ExtensionPipeline/ExcludeListPreflight paths. Actual dependency namespace/module is Ctap2; previewSign **draft v4** is not a general CTAP protocol version; implementation is Codec/Processing/CredentialFilter. Distinguish FIDO identity from assembly/module names.
- CLAUDE:243–245 “Known gap: extension-driven permissions are not aggregated” is stale: registration:105–106/authentication:94–95 OR session Permissions before UV decision; AuthenticationExtensionSession:63–65 aggregates and LargeBlobProcessor:92–94 supplies LargeBlobWrite. Remove obsolete gap, retain any *actual* extension limitations WA-B establishes.
- CLAUDE:350,355,461 says previewSign multi-credential probe-selection unimplemented/count !=1 rejected. PreviewSignProcessor validates count equals allowCredentials count, searches by selected ID, and the client probes/selects one from a multi-entry list. This claim is stale; WA-B owns complete draft interpretation.
- CLAUDE:390 claims NormalizePinAsync signature accepts PIN and cancellation token. Actual helper takes only Ctap2Session; KnownTestPin is fixed. Do not present an overload that does not exist.
- CLAUDE:144–147 laziness should be reconciled with WA-A-001: lazy cached wrapper, not deferred device/output work.
- CLAUDE:237,380–382,468–472 additionally depend on 004/005/008/009.

**Correction:** present architecture and current boundaries, not previous adapters/phases. Preserve genuine gaps (e.g. PRF UV behavior, delegated RP policy) until WA-B/WA-C reconcile. This finding is evidence for WA-C, not permission for WA-A edits.

### WA-A-021 — Backend hard-codes PIN/UV protocol 2; broad CTAP compatibility needs qualification

**Locations:** `WebAuthnBackend.cs:32,284–290`; `Client/WebAuthnClient.cs:26–27`; construction prerequisites/module purpose.

Backend always creates protocol 2; it never consults `AuthenticatorInfo.PinUvAuthProtocols`. All ClientPin token/key-agreement work uses it. CTAP2's helper falls back from permissions token to getPinToken when `pinUvAuthToken` is absent, but **does not change protocol version**. A protocol-1-only authenticator therefore is not made compatible by that fallback. Non-PIN/no-key-agreement ceremonies may still work; do not say every operation requires protocol 2. Unit double `TestPinUvAuthProtocol` reports version2 but 16-byte tags (real protocol2 uses32); it is an intentionally non-cryptographic fake, not conformance evidence.

**Correction now:** qualify PIN/UV and relevant extension operation prerequisites: current backend uses protocol2, rather than broadly guaranteeing all CTAP2.0/2.1 authenticators. **Deferred:** protocol negotiation and built-in UV support checks; `UvDecision` also selects built-in UV based on `uv` without checking `pinUvAuthToken`, while §6.5.5.7 requires both for permission-token UV. No device claim is established. **C9. Dependencies:** README prerequisites/UV flows, backend comments, PRF key agreement (WA-B); re-review after negotiation fix.

### WA-A-022 — Transport/origin short descriptions imply broader or narrower standards guarantees

**Locations:** `WebAuthnTransport.Hybrid:48–51`, `SmartCard:43–46`; `WebAuthnOrigin:19–26,69–73,131–137`; transport enum href at21.

Hybrid is not definitionally “QR code + BLE”; L3 defines combined data/proximity mechanisms, QR/BLE is one implementation. Smart-card means contact ISO7816 hint, not necessarily every SDK SmartCard connection (USB CCID/NFC route). Origin accepts HTTPS or HTTP `localhost`/**subdomains ending .localhost**, not only HTTP localhost as class remarks say. It performs URI parsing and a restricted scheme/host policy, not full browser Secure Contexts evaluation; the latter also includes ancestry and other potentially trustworthy origins. `IsLoopback` checks names, not IP loopback/resolution. The transport link `#enum-transport` should use the actual L3 `#enumdef-authenticatortransport` anchor (verified in fetched document).

**Correction:** exact supported host/scheme rules, no full secure-context/browser-conformance claim; generic hybrid definition with examples; distinguish standard hint from SDK selected transport. **C11/C12. Dependencies:** origin README/RP callback qualifications and transport metadata guidance. Keep native/opaque origins unsupported. No policy expansion proposed.

## Citation/claim index

All network references accessed 2026-11-03. These are the versions actually fetched, not automatic upgrades. Primary pages returned HTTP 200. Code evidence is baseline above. Moving source links below use dated equivalent when fetched. Sections are explicitly named so the evidence remains actionable when numeric headings differ.

| ID | Version/status and exact section | Actor/conditions; supporting excerpt/paraphrase | Disposition/dependencies |
| --- | --- | --- | --- |
| C1 | [WebAuthn L3, W3C Recommendation 25 Aug 2026](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#sctn-createCredential), §5.1.3, and §5.4.7 AttestationConveyancePreference | Client: None keeps packed/no-x5c/zero-AAGUID self attestation; otherwise fmt none/empty attStmt; Indirect MAY anonymize; Direct/Enterprise convey unchanged. No instruction here to zero returned AAGUID. | Registration's None/self branch accurate; avoid L2 AAGUID rules. Enterprise request gap 003. |
| C2 | [CTAP2.3 Proposed Standard 26 Feb 2026](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#sctn-getAssert-authnr-alg), §6.2.2 response table; also [CTAP2.1 PS 15 Jun 2021, errata 21 Jun 2022](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#sctn-getAssert-authnr-alg) | Authenticator: identifying user fields “MUST NOT be returned if user verification is not done”; discoverable at least id mandatory; server-side User optional. Credential is marked Required in table, with special omission note in algorithm for single allowList. | 002. Single-allow-list omission is accepted by CredentialFilter, but final matcher/response builder lose ID if omitted: see deferred Q1. |
| C3 | Same CTAP2.1/2.3, §6.1 authenticatorMakeCredential parameter/options table and §6.1.2 algorithm | Platform MUST NOT include uv together with pinUvAuthParam; up default true, MAY send up:true to2.1, false invalid; 2.0 rejects any up. Absent enterpriseAttestation leaves privacy characteristics unaffected. makeCredUvNotRqd permits **non-discoverable** unverified creation. | 003/009/010. Correct no uv+token code; distinguish preference from mandatory authenticator policy. |
| C4 | [WebAuthn L3](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#sctn-createCredential) §5.1.3 and §5.1.4; §15.1 Recommended Range for Ceremony Timeouts | Client cancels issued authenticator requests when lifetimeTimer expires and throws NotAllowedError. Recommended range300000–600000ms; default300000ms. SDK applies no default/clamp and preserves completed results; not complete normative timer implementation. | 006; options/Deadline and audit tests. |
| C5 | CTAP2.3 §11.2.9.1.5 CTAPHID_CANCEL and §11.2.9.1.7 CTAPHID_KEEPALIVE; .NET10 [CancellationTokenSource docs](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource?view=net-10.0) via Context7 | Authenticator MUST cancel an outstanding cancellable request and replies KEEPALIVE_CANCEL; no reply to cancel itself. Keepalive “SHOULD be sent at least every100ms,” not a guaranteed maximum. .NET constructor rejects delay beyond supported maximum. Core returns actual success racing cancel. | 006; timeout transport caveat and hardware-test bound are assumptions, not universal proof. |
| C6 | WebAuthn L3 §13.4.3 Cryptographic Challenges | RP challenges MUST contain enough entropy to make guessing infeasible; “SHOULD therefore be at least16 bytes long.” | 007; SDK rejects empty only; server verifies independently. |
| C7 | [WebAuthn L2 REC 8 Apr 2021](https://www.w3.org/TR/2021/REC-webauthn-2-20210408/#enumdef-userverificationrequirement), §5.8.6; compare §5.4.2 RP entity | RP Preferred prefers UV if possible, will not fail merely because response lacks UV. §5.4.2 is unrelated to UV. | 008; retain cited level rather than silently modernize. |
| C8 | WebAuthn L3 §5.8.1 CollectedClientData and §5.8.1.1 Serialization | Client serializes type/challenge/origin/crossOrigin in fixed order; missing crossOrigin serialized false; append topOrigin if present. CCDToString escapes quote/backslash and uses lowercase four-digit Unicode escapes for remaining controls. Verifiers must tolerate unknown/reordered fields (§5.8.1 note), not a permission for client serializer to ignore its algorithm. | 012. TopOrigin is set only when crossOrigin true; SDK ceremony validates that but public builder does not. |
| C9 | CTAP2.3 §6.5.5.4 getKeyAgreement, §6.5.5.7 Obtain token, §6.5.6 ProtocolOne, §6.5.7 ProtocolTwo | Protocol2 is stateless; encapsulation derives separate HMAC/AES keys. getPinUvAuthTokenUsingUvWithPermissions requires pinUvAuthToken **and** uv advertised true. PIN permissions tokens require clientPin and token capability; legacy fallback separate. | 015/021. Current internal key-agreement XML cites §6.5.6 alone while concrete backend uses protocol2; editors should cite protocol-neutral exchange and protocol2 derivation where appropriate. |
| C10 | CTAP2.3 §8.1 Message Encoding (CTAP2 canonical CBOR); [.NET CborConformanceMode](https://learn.microsoft.com/en-us/dotnet/api/system.formats.cbor.cborconformancemode?view=net-10.0) | Encoder: map keys sorted by major type, then length, then bytewise lexical order; not plain decoded-string lexical ordering. Microsoft Ctap2Canonical enforces sorted-by-encoding canonical output. | 016; attestation structure itself correctly references existing L2 §6.5/attestation topic; no need to change its level. |
| C11 | WebAuthn L3 §5.8.4 [AuthenticatorTransport](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#enumdef-authenticatortransport); §5.2.1 getTransports; CTAP2.3 [§6.4 GetInfo](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#authenticatorGetInfo) | Client transport names unique/lexicographically ordered; unknowns tolerated. Authenticator hints not host reachability. GetInfo count limit must be>0; maxCredentialIdLength positive if present; platforms tolerate unknown transport values. Hybrid combined data/proximity; smart-card contact ISO7816. | RegistrationTransport current snapshot behavior accurate, 022 fixes definitions/link. CredentialFilter bounded chunks/current limits correct local policy. |
| C12 | WebAuthn L3 §4 [RP ID](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#rp-id); [Secure Contexts CR Draft 10 Nov 2023](https://www.w3.org/TR/2023/CRD-secure-contexts-20231110/), §3.1/§5.2 | Non-Web binding specifications MAY differ, but RP syntax MUST be domain string or URI. Secure Contexts potentially-trustworthy origins includes loopback IPs and qualified localhost names subject to resolution policy; full secure context is more than HTTPS syntax. | Delegated RP validity boundary is accurately qualified, do not reinsert SDK PSL/IDNA policy; origin's narrower surface needs 022 precision. |
| C13 | [RFC4648, Standards Track Oct2006](https://www.rfc-editor.org/rfc/rfc4648#section-5), §5; WebAuthn base64url definition | URL-safe alphabet; padding omission is the referring protocol's rule, not intrinsically every RFC4648 encoding. | Base64Url encoding matches WebAuthn; optional internal clarification “WebAuthn unpadded base64url using RFC4648§5 alphabet.” Its decoder is permissive, not a strict canonical validator. |

Context7 used for .NET APIs (library resolution and separate CBOR/CTS queries). CTS returned relevant .NET10 constructor/cancel-after documentation. CBOR query was unhelpful (one no-match and one irrelevant result), so its primary Microsoft API reference was fetched directly once; no runtime behavior was guessed from an unrelated result. No library setup/build/test CLI was executed.

## Deferred questions / bounded follow-up completed

1. **Q1: missing final assertion credential descriptor.** Initial source shows CredentialFilter correctly recovers single-descriptor success (`CredentialFilter:130–134`); bounded follow-up CTAP2 GetAssertionResponse decoder confirms descriptor can remain null and no lower layer fills it. `CredentialMatcher:60,73` and `BuildAuthenticationResponseAsync:199–200` then publish empty IDs despite known selected allow-list descriptor. This is a genuine implementation gap on allowed omission responses, not a new supported empty-ID contract. Minimal docs disposition: disclose that some authenticator responses omitting descriptor may yield an empty ID, retain selected requested ID application-side where applicable, do not claim every response always identifies a credential. **Deferred fix:** carry selected descriptor through final response construction; reopen MatchedCredential.Id, AuthenticationResponse.CredentialId, selection guide and per-credential extension handling. Current fakes always supply credential and miss this case. Citation C2.
2. **Enterprise intent (003):** bounded trace reached CTAP2 option and encoder; no WebAuthn forwarding/policy exists. No more research needed before triage decides future behavior.
3. **Error normalization (005):** bounded trace identifies exact catch boundaries, including outputs deliberately outside retry. Scope is not merely cosmetic. Proposed docs must acknowledge gap now, not normalize raw exceptions as ideal contract.
4. **Timeout input/return (006):** bounded Core/framework trace establishes cooperative signal and success race. Desired final admission/timeout policy remains triage; hardware claims remain unexecuted.
5. **Aaguid default (014) and serialization (012):** deterministic source counterexamples; do not need device experiments. Keep behavioral fixes deferred, publicly qualify affected guarantees.
6. **Protocol2 compatibility (021):** bounded ClientPin trace proves permission fallback does not negotiate version. Do not infer platform or authenticator support from fake protocol. No hardware qualification authorized.

## Coverage manifest

All paths below are under `src/WebAuthn/`. “Read” means the entire assigned semantic file was read, not grep-only; copyright boilerplate is not counted as an explanatory unit. “Clean” means no required comment/XML correction identified in that file, not complete protocol conformance or executed tests. IDs are local findings above, Q1 is the deferred omission bug. Every assigned source and test is enumerated.

### Source (34)

| File relative to `src/WebAuthn/` | Disposition |
| --- | --- |
| src/Attestation/WebAuthnAttestationObject.cs | Read; 013,016 |
| src/Client/Authentication/AuthenticationResponse.cs | Read; 001,002,Q1 |
| src/Client/Authentication/CredentialMatcher.cs | Read; 001,Q1 |
| src/Client/Authentication/MatchedCredential.cs | Read; 001,002,Q1 |
| src/Client/Authentication/PublicKeyCredentialRequestOptions.cs | Read; 006,007 |
| src/Client/CeremonyDeadline.cs | Read; 006 |
| src/Client/CredentialFilter.cs | Read; clean behavior explanation; C11/Q1 context; first **chunk** selection follows authenticator result, avoid promising deterministic individual-RP ordering within multi-entry chunk |
| src/Client/IWebAuthnBackend.cs | Read; 011, C9 citation qualification |
| src/Client/PinUvAuthTokenSession.cs | Read; clean ownership/zeroing and backend-owned protocol explanation |
| src/Client/PinUvSharedSecret.cs | Read; clean ownership; C9 protocol-specific section qualification |
| src/Client/Registration/PublicKeyCredentialCreationOptions.cs | Read; 003,006,007 |
| src/Client/Registration/RegistrationResponse.cs | Read; clean returned-field descriptions; 013 contextual ownership improvements |
| src/Client/RpIdValidator.cs | Read; clean delegated validity/authorization/server boundary, C12 |
| src/Client/UserVerification/UvDecision.cs | Read; 008,021 |
| src/Client/WebAuthnBackend.cs | Read; 015,021; 019 dependency confirmation |
| src/Client/WebAuthnClient.Authentication.cs | Read; 001,005,006,Q1 |
| src/Client/WebAuthnClient.Ceremony.cs | Read; 005; remint citation should point to token state/command checks rather than only token acquisition §6.5.5.7 |
| src/Client/WebAuthnClient.PinUvAuth.cs | Read; 005; prompt retry/late-buffer disposal explanations otherwise accurate |
| src/Client/WebAuthnClient.Registration.cs | Read; 003,005; C1 None/self behavior confirmed |
| src/Client/WebAuthnClient.Validation.cs | Read; 004–006; validation/RP callback order accurate |
| src/Client/WebAuthnClient.cs | Read; 013,021; source-file split belongs internal rather than consumer remarks |
| src/Client/WebAuthnClientData.cs | Read; 012 |
| src/Client/WebAuthnClientOptions.cs | Read; clean values/defaults/guards; retention/disposal context013 |
| src/Client/WebAuthnOrigin.cs | Read; 022 |
| src/Cose/Aaguid.cs | Read; 014 |
| src/IYubiKeyExtensions.cs | Read; 019 |
| src/Preferences/AttestationPreference.cs | Read; 003 |
| src/Preferences/ResidentKeyPreference.cs | Read; clean preference versus required distinction |
| src/Preferences/UserVerificationPreference.cs | Read; clean concise preference definitions; enrich only with verified exception qualifications from008/009 |
| src/Util/Base64Url.cs | Read; clean current unpadded encoding behavior, C13 optional citation clarification; not strict canonical decoder |
| src/Util/SensitiveMemory.cs | Read; clean null/empty/in-place behavior |
| src/WebAuthnAuthenticatorData.cs | Read; 013; raw extension map is parsing not trust verification |
| src/WebAuthnClientError.cs | Read; 004 |
| src/WebAuthnTransport.cs | Read; 022 |

### Tests (35)

Prefix `I/` = `tests/Yubico.YubiKit.WebAuthn.IntegrationTests/`; `U/` = `tests/Yubico.YubiKit.WebAuthn.UnitTests/`.

| File | Disposition |
| --- | --- |
| I/AuditV2/WebAuthnAuditHardwareReproTests.cs | Read; 006,010 |
| I/AuditV2/WebAuthnAuditHardwareTests.cs | Read; 010; PRF UV/largeBlob explanations shared evidence for WA-B, no device verification |
| I/WebAuthnClientFactoryTests.cs | Read; clean comments; SmartCard failure skip correctly scoped |
| I/WebAuthnClientTests.cs | Read; 002/009 evidence; ownership disposal comments otherwise accurate |
| I/WebAuthnExcludeListStressTests.cs | Read; 017 |
| I/WebAuthnTestHelpers.cs | Read; clean local comment about fixed reused test PIN; actual signature disproves CLAUDE020; no security audit/cleanup execution |
| U/Attestation/AttestationObjectTests.cs | Read; clean fixture comments; synthetic statements are parsing fixtures, not cryptographic verification |
| U/AuditV2/WebAuthnAuditReproTests.cs | Read; clean present test explanations; C1/C4/C8/C11 evidence |
| U/Client/CredentialFilterTests.cs | Read; clean intended chunk/probe tests; Q1 coverage gap |
| U/Client/CredentialPromptRetryTests.cs | Read; clean rejected-PIN/late-owner/cancellation explanations |
| U/Client/CtapStatusMappingTests.cs | Read; clean mapper grouping; 004 evidence, not entire ceremony coverage |
| U/Client/PinUvAuthTokenSessionTests.cs | Read; clean single-owner adoption/disposal rationale |
| U/Client/RegistrationTransportTests.cs | Read; clean snapshot/default/cache/unknown-name tests |
| U/Client/RpAuthorizationTests.cs | Read; clean callback/wire-boundary explanation |
| U/Client/UserPresenceIntentTests.cs | Read; clean fake SmartCard request-flow explanation |
| U/Client/UserVerification/UvDecisionLogicTests.cs | Read; 010 |
| U/Client/WebAuthnBackendTests.cs | Read; 018 |
| U/Client/WebAuthnClientCeremonyFrameTests.cs | Read; 010; C9 remint citation qualification; fake assertions do not prove equal timings |
| U/Client/WebAuthnClientConstructionTests.cs | Read; clean ownership/admission tests |
| U/Client/WebAuthnClientDataTests.cs | Read; 012 contradictory low-level-builder example needs explicit purpose, not valid ceremony claim |
| U/Client/WebAuthnClientGetAssertionTests.cs | Read; 010; 001 evidence |
| U/Client/WebAuthnClientMakeCredentialTests.cs | Read; 005,010 |
| U/Client/WebAuthnClientOptionsTests.cs | Read; clean initialization/default/guard description |
| U/Client/WebAuthnClientPinRetryTests.cs | Read; clean no-retry explanation for PinAuthInvalid |
| U/Client/WebAuthnOriginTests.cs | Read; clean existing cases; not full secure-context proof |
| U/Client/WebAuthnSurfaceTests.cs | Read; clean effective public/internal boundary explanation |
| U/Cose/AaguidTests.cs | Read; clean initialized-value tests; default semantic gap014 |
| U/Cose/CoseAlgorithmTests.cs | Read; clean imported CTAP value tests; `Placeholder` is actual experimental constant name, not unfinished test scaffolding |
| U/Cose/CoseKeyTests.cs | Read; clean decoding fixture descriptions; no crypto validity claim |
| U/Cose/Fixtures.cs | Read; clean synthetic programmatic encoding/regeneration explanation; RSA zeros explicitly identified |
| U/IYubiKeyExtensionsTransportTests.cs | Read; clean explicit SCP routing;019 evidence |
| U/TestSupport/MockCtap2Responses.cs | Read; clean fixture encoding explanations; minor optional precision: Guid default byte layout is mixed-endian, not simply all little-endian; Q1 fakes always supply credential |
| U/TestSupport/TestPinUvAuthProtocol.cs | Read; clean noncrypto auth-tag sentinel rationale; NotImplementedException members are deliberately unused test-double operations, not shipping unfinished work;021 caveat |
| U/TestSupport/TokenBufferAssert.cs | Read;018 |
| U/WebAuthnAuthenticatorDataTests.cs | Read; clean authData/flags/extension parsing fixture comments |

## Narrow dependency index and handoff

- **Instructions:** root/WebAuthn/Core/CTAP2 CLAUDE fully read. Core guidance unrelated long lines were not treated as this audit's claims. No nested WebAuthn instructions in coordinator inventory.
- **API snapshots:** WebAuthn Shipped1 line and Unshipped544 lines read; internal backend/token/decision/helpers correctly absent from external API. Public default Aaguid/transport construction is visible, not hidden by private explicit constructors. Snapshot content itself owned by WA-C.
- **CTAP2 source:** `IYubiKeyExtensions.cs:75–191` transport/SCP/connection ownership; `Pin/ClientPin.cs:285–494` token adoption, permission fallback, UV/key agreement; `Credentials/GetAssertionResponse.cs` and `AuthenticatorData.cs` fully read for passthrough/copies; `Cbor/AaguidConverter.cs` fully read for16-byte conversion; `PinUvAuthProtocolV2.cs:1–170` stateless encapsulation/32-byte tag evidence; encoder enterprise/exclude Count predicates found narrowly. No claim these partially read dependencies are complete audits.
- **Core source:** `Protocols/Fido/Hid/FidoHidProtocol.cs:260–409` terminal drain/cancel/success-race evidence. No broad transport/platform audit.
- **WA-B overlap:** complete RegistrationExtensionSession, AuthenticationExtensionSession, PreviewSignProcessor read as direct ceremony dependencies; LargeBlobProcessor:1–145 read for permission aggregation and read-before-return only. Extension ownership remains WA-B. Reconcile 001/003/005/006/012/013/021 and stale module020 with WA-B report.
- **WA-C handoff:** present-task prose must reconcile factory019, selection001, User002, enterprise003, errors004/005, timeout006, challenge007, UV008/009, compatibility021, origin/transport022; module stale claims020 independently disproved by source. Do not let missing implementation disposition survive only in this private report.
- **No assertions of hardware success, full browser/WebAuthn conformance, cross-platform behavior, or passing tests.** Initial investigation plus bounded targeted follow-ups completed. Meaningful implementation choices remain deferred as stated; no further device research proposed.
