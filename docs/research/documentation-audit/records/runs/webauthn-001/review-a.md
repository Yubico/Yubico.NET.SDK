# Independent reviewer A — WebAuthn cycle 001

2026-11-03; fresh independent GPT-6.1 Sol reviewer; baseline `e353b49afa15d1d104fce70bbf1e1abc59ddb132`.

## Verdict: REQUIRED CHANGES

Reviewed all **36 files** in the edit-a manifest against the baseline diff and complete affected declarations/methods, including surrounding implementations and assertions. The accepted A dispositions are substantially implemented, but two documentation/evidence corrections remain: the version attribution of permitted descriptor omission, and a residual no-information-leak assurance in a test summary. Neither requires implementation changes.

Root and WebAuthn instructions, approved style, triage, investigation-a and edit-a were read. Core/CTAP2 instructions were read before narrow dependency tracing. B-owned extension dependencies were read using `git show e353b49a:<path>`, not their in-flight working-tree changes. No repository files changed; no build, tests, formatter, hardware operation, or delegation. This report is the only authored file.

This is a semantic/source/specification review, not acceptance of edit-a's token comparison or XML parse as proof of correctness. No compiler cref-resolution or runtime qualification is claimed.

## Required changes

### R1 — Correct the specification-version attribution for A-Q1

**Affected new prose:**

- `src/WebAuthn/src/Client/Authentication/AuthenticationResponse.cs:27–31` describes permitted omission without naming the applicable CTAP version.
- `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs:49–53` likewise says “where CTAP permits it.”
- The supporting record is explicitly wrong: `investigation-a.md:204,221` attributes this permission to CTAP2.1/2.3 §6.2.2, including a supposed special algorithm note, and `edit-a.md:110` repeats that attribution as the exact primary follow-up.

**Independent primary evidence:**

1. [CTAP 2.0 Proposed Standard, 30 Jan 2019, §5.2 authenticatorGetAssertion](https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html#authenticatorGetAssertion): the response table marks `credential (0x01)` **Optional** and says, “May be omitted if the allowList has exactly one Credential.” This is an authenticator response optimization, not a permission for the WebAuthn client to publish an unusable empty identifier.
2. [CTAP 2.1 PS with 21 Jun 2022 errata, §6.2.2](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#sctn-getAssert-authnr-alg) and [CTAP 2.3 PS, 26 Feb 2026, §6.2.2](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#sctn-getAssert-authnr-alg) mark that member **Required**. Their response algorithm/table does not contain the claimed single-entry omission exemption. I checked the primary 2.3 HTML as well as extracted specification text; this is not an inference from a missing search result.

**Source evidence confirms the limitation, independently of the incorrect citation:** baseline CTAP2 `Credentials/GetAssertionResponse.cs:134,149–150,183–198` permits a null descriptor and does not recover an ID. WebAuthn `CredentialMatcher.cs:59–61,72–74` and `WebAuthnClient.Authentication.cs:215–216` then publish empty IDs. `CredentialFilter.cs:130–134` recovers the probe's singleton descriptor, but that recovery is not carried through final response construction. Its existing §6.2.2 citation is also not a valid attribution to the current 2.1/2.3 rule.

**Required disposition:** retain the honest empty-ID limitation and the original-single-entry-only application guidance. Correct the evidence/handoff attribution to **CTAP2.0 §5.2**, and make the version distinction local in the new public prose characterizing omission as permitted. Do not claim 2.1/2.3 permits the optimization. The current generic “where CTAP permits it” is not intrinsically false across all CTAP versions, but it does not repair the explicitly incorrect evidence record or communicate the relevant compatibility boundary. Do not remove the limitation, add ID guessing, or implement recovery in this documentation batch. The pre-existing `CredentialFilter` citation can receive the same narrow documentation correction under the coordinator's ownership decision; no broader filter audit is requested.

### R2 — Remove the remaining unconditional no-disclosure assurance from the dummy-descriptor test summary

**Location:** `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientCeremonyFrameTests.cs:380–381`:

> “…the relying party cannot learn \"credential absent\" without a gesture…”

This survives in the complete affected class although A-001 correctly weakens the corresponding implementation comments at `WebAuthnClient.Authentication.cs:157–160,176–177` to the actual rationale and explicitly disclaims equal timing/prompts.

**Exact assertion evidence:** `GetAssertion_AllowList_NothingMatches_SendsDummyDescriptorAndReturnsEmpty`, lines 383–399, configures the fake backend to throw `NoCredentials` for every request and asserts an empty result, absent explicit options, and the dummy descriptor/type. It does not obtain or observe a gesture, measure timing, model authenticator UP policy, or prove that credential absence cannot be inferred. The implementation at Authentication.cs:163–171 sends the dummy request; it does not establish that broader information-flow guarantee.

**Required disposition:** describe what the test checks: after a silent-probe miss, the client sends the final dummy-descriptor request with the normal required-UP policy instead of returning the probe miss directly, and returns an empty match list for `NoCredentials`. Remove the unconditional “cannot learn” guarantee. Keep test name, assertions, literals and executable tokens unchanged. This completes A-001's already accepted distinction between rationale and proven timing/prompt/security properties.

## Accepted dispositions and semantic evidence

All source references below are current working-tree line numbers unless identified as baseline dependencies. Line numbers may move when R1/R2 are corrected.

| Disposition | Review result and exact evidence |
| --- | --- |
| A-001 selection | Accepted apart from R2. Authentication.cs:121–134 awaits response/output construction for every match before returning; line 130 wraps the built response in `Task.FromResult`. MatchedCredential.cs:52,67–69,92–94 uses one lazy task with a cancellation-aware wait. Matcher.cs:45–78 gathers final assertions; Authentication.cs:163–171 narrows an allow list. New public prose correctly distinguishes no match, one selected allowed credential, and possible discoverable enumeration, with no deferred writes or RP verification. |
| A-002 user data | Accepted. Authentication.cs:218–231 passes authenticator User through. CTAP2.3 §6.2.2 response table makes server-side User optional, discoverable User.id mandatory, and forbids identifying display fields without UV. The new XML does not promise UV or account authorization from a display name. Deferred CTAP2 User XML correction remains a separate module follow-up. |
| A-003 attestation | Accepted. Registration.cs:197–208 and Backend.cs:149–179 do not forward enterpriseAttestation. Registration.cs:232–237 replaces non-self statements only for None; lines 277–279 define the packed/zero-AAGUID/no-certificate exception. The enum and creation option disclose the enterprise request and indirect anonymization limits without promising unsupported behavior. Dated L3 §5.1.3 supports retaining the AAGUID and the self-attestation exception. |
| A-004 errors | Accepted. Validation.cs:165–183 maps PIN rejection to NotAllowed and authenticator timeout statuses to Constraint. Ceremony.cs:220–227 maps exclusion to InvalidState. Validation.cs:43–52 maps application RP rejection to Security. Public wrappers catch only timeout-origin cancellation; Cancelled is not their caller-token result. |
| A-005 normalization | Accepted. Ceremony.cs:129–145 acquires a token before the mapping catch. Authentication.cs:107 and Registration.cs:100 obtain GetInfo outside it; both prepare outputs after the attempt returns. PinUvAuth.cs:100–103,174–193 maps only selected rejection statuses. Public methods now say lower-level failures can propagate and do not imply rollback after credential creation. |
| A-006 deadline | Accepted. Deadline.cs:34–46 creates the timer after validation/callback in each public method; Expired at line 59 excludes caller cancellation. Wrappers translate a resulting OCE only. Baseline Core FidoHidProtocol.cs:302–310 preserves a real success racing cancellation and lines 334–378 send cancellation during keep-alive processing. Options retain no-default/no-clamp, PC/SC draining, supported-framework-delay, and side-effect qualifications. Hardware XML preserves SHOULD and labels two-second latency slack an assumption, not a guarantee. |
| A-007 challenge | Accepted. Both options name the RP actor and distinguish sufficient entropy from recommended length. Validation.cs:58–63,115–120 only rejects empty challenges. Dated L3 §13.4.3 says MUST have enough entropy and SHOULD be at least 16 bytes. No SDK challenge generation or RP verification is implied. |
| A-008 UV citation | Accepted. UvDecision.cs:148–151 cites L2 §5.8.6, not RP entity §5.4.2, and describes the SDK's enabled-method/availability policy separately. Lines 119–132 permit Preferred to proceed without an obtainable method. The specification level was not silently upgraded. |
| A-009 presence/test applicability | Accepted. Test XML identifies missing makeCredUvNotRqd/alwaysUv guards and the non-discoverable/HID context. Its options helper at WebAuthnClientTests.cs:33–52 supplies Discouraged and non-discoverable defaults. CTAP2.3 §6.1 permits up:true and prohibits up:false; SDK omission is compatibility policy. Unchanged runtime assertion text is not treated as an editorial promise or portability proof. |
| A-010 stale tests/crefs | Accepted. Required-UV commentary now matches the absent-uv token path; InlineData labels match the parameter order. MakeCredentialTests.cs:485–508 checks three buffers, consistent with cached pre-flight at Registration.cs:162–171. Replacement cref targets exist at GetAssertionTests.cs:416 and MakeCredentialTests.cs:545. The slow-factory test is explicitly an internal waiting-semantics case, not production deferred device work. |
| A-011 backend phases | Accepted. IWebAuthnBackend.cs:89–99 removes completed phase history; line 149 retains the useful processor-built raw-map passthrough contract. |
| A-012 serialization | Accepted. ClientData.cs:114–139 always emits crossOrigin in the documented key order; lines 148–190 manually escape strings, including short escapes. Create's new remarks disclose the low-level validation and exact-CCDToString limitation rather than presenting ordinary JSON acceptance as exact conformance. The contradictory topOrigin fixture is explicitly labeled non-ceremony input. |
| A-013 ownership/trust | Accepted. AttestationObject.cs:99–104,119–123 borrows original outer CBOR/statement slices while materializing authData. Create retains supplied objects at 140–145; Encode rebuilds three modeled fields at 157–195. Baseline CTAP2 AuthenticatorData.cs:134,138,153–164 copies raw/subfields. Client.cs:70–75 validates constructor inputs before adopting the session, and DisposeAsync delegates to the backend without clearing response buffers. Prompt and RP-server verification responsibilities remain explicit. |
| A-014 Aaguid | Accepted. Aaguid.cs:65,77–79,93–94 deterministically establishes default's empty span, Guid.Empty projection and nonreflexive equality. XML calls this a known limitation and recommends explicit construction; it does not rebrand the defect as intended value semantics. No engineering change requested here. |
| A-015 backend initialization | Accepted. Backend.cs:285–292 constructs the protocol object only. Baseline ClientPin.cs:354–359,429–434,467–478 performs per-operation getKeyAgreement/encapsulation. No asynchronous initialization is claimed. |
| A-016 CBOR order | Accepted. AttestationObject.cs:177 selects Ctap2Canonical. The revised comment no longer equates decoded-string lexical order with canonical encoded-key order. CTAP2.3 encoding rules sort major type, then length, then bytewise order; for these text keys that yields fmt, attStmt, authData. |
| A-017 stress comments | Accepted. StressTests.cs:94–134 reads capacity metadata and skips insufficient capacity; lines 145 and 175 use different random user IDs; lines 203–215 perform RP-scoped cleanup. New prose preserves destructive operator/reset and session ownership context without the unsupported fingerprint/config shared-pool theory. |
| A-018 zeroing evidence | Accepted. BackendTests.cs:76–82 checks nonzero pre-disposal bytes and an access guard, not a retained view after disposal. The new comment says so. TokenBufferAssert.cs:57–62 checks every byte for zero; lines 73–75 only checks that some nonzero byte remains. Updated descriptions match that distinction. |
| A-019 factory | Accepted. WebAuthn factory.cs:71–81 forwards session options and cleans failed client construction. Baseline CTAP2 factory.cs:128–154 chooses SmartCard automatically for SCP without an override, otherwise selects one transport; resolution/failure paths do not implement connection-opening fallback. Firmware/FIDO-AID and NFC distinctions remain attached to the transport guidance. |
| A-020 Markdown | Intentionally pending editor C, not a blocker for A. No demand to edit C-owned prose during this review. Module architecture, helper signature, UV/error/selection, protocol-version and extension limitations still require the coordinated Markdown reconciliation before whole-cycle acceptance. |
| A-021 protocol support | Accepted. Backend.cs:33,285–292 always uses V2. Baseline ClientPin.cs:343–352 falls back to a legacy command without replacing its protocol; lines 472–474 transmit that supplied version. UvDecision.cs:107–116 does not gate on pinUvAuthToken. CTAP2.3 §6.5.5.7 requires both token and uv capability for the UV token subcommand. Public compatibility wording preserves possible PINless/no-key-agreement operation and does not claim protocol negotiation. |
| A-022 origin/transport | Accepted. Origin.cs:84–112,135–138 accepts HTTPS or HTTP localhost/.localhost only, without DNS/ancestry evaluation. Transport.cs uses the actual enum anchor and distinguishes a contact smart-card hint from selected SDK transport; hybrid is not defined as necessarily QR/BLE. |
| A-Q1 | Runtime limitation and safe application guidance accepted; **specification attribution requires R1**. Neither an original multi-entry list's first ID nor display names are proposed as credential/account recovery. |

## Independent citation checks

Accessed primary specifications on 2026-11-03, not just investigation-a's claim index:

- [WebAuthn L3 REC, 25 Aug 2026](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/): §5.1.3 None/self-attestation and unaltered Direct/Enterprise conveyance; §5.8.1.1 fixed serialization order, always-written crossOrigin, CCDToString's lowercase four-digit control escapes; §13.4.3 challenge entropy/recommended length; §15.1 recommended 300000–600000 ms/default 300000 ms; AuthenticatorTransport's contact ISO7816 and combined data/proximity definitions. Both revised transport and existing attestation enum anchors exist. Actor distinctions in the edits are preserved: client serialization/conveyance, RP challenge/verification, authenticator transport hints.
- [WebAuthn L2 REC, 8 Apr 2021](https://www.w3.org/TR/2021/REC-webauthn-2-20210408/#enumdef-userverificationrequirement): §5.8.6 Preferred explicitly prefers UV if possible but does not fail solely because the response lacks UV. The new citation targets the correct level/section.
- CTAP2.3 PS (26 Feb 2026), cited above: §6.1 options/enterprise parameter, §6.2.2 User and required credential descriptor, §6.5.5.7 UV-token capability requirements, §6.5.7 protocol-two derivation/tag rules, canonical CBOR sorting rules, §11.2.9.1.7 keep-alive **SHOULD** at least every 100ms. This does not turn the SDK's cooperative timer into full WebAuthn lifetime-timer conformance.
- CTAP2.1 errata (21 Jun 2022), cited above: §6.5.2 token state and §6.5.3.2 maintenance, plus command checks, support the revised bounded remint explanation; §6.5.5.7 remains the acquisition reference, not a blanket statement that every silent probe consumes all permissions.
- Context7 was used for .NET CTS and CBOR documentation. It returned the framework-supported delay restriction/ArgumentOutOfRangeException and encoded-key sorting for Ctap2Canonical. The public text deliberately does not invent a numeric delay maximum. CBOR search also returned a preview-version API entry; I did not treat that as .NET10 runtime execution or use irrelevant search results as semantic proof.

The direct Python CTAP fetch was HTTP 403; the primary page was subsequently retrieved through the read-only fetch tools, including full 2.3 HTML. No unresolved conclusion is based on that initial fetch failure.

## Optional improvements (not blockers)

1. `MatchedCredential.Id` could link to `AuthenticationResponse.CredentialId` so the result's full RP-verification/empty-ID guidance is one click away. The necessary facts already appear locally and on GetAssertionAsync; this is not required duplication.
2. `WebAuthnClientData.Type` and Create's `type` parameter retain the conventional two operation names, while the new Create remarks correctly say any nonblank type is accepted. Adding “normally” to those short descriptions could make the low-level distinction even more immediate; there is no contradictory validation promise in the revised remarks.

## File coverage (36/36)

Every listed file's baseline diff and complete changed comment/XML block were read with its enclosing declaration/method and relevant implementation/assertions. All 25 changed source files were read in full. Large test files were reviewed by complete affected methods and the referenced helpers; the class-level ceremony-frame summary was checked against its complete class. This is not a claim to re-audit all unchanged tests. `S/` = `src/WebAuthn/src/`; `I/` = `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/`; `U/` = `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/`.

| # | File | Result |
| --- | --- | --- |
| 1 | S/Attestation/WebAuthnAttestationObject.cs | Accepted; borrowed memory, modeled re-encoding and trust boundary checked |
| 2 | S/Client/Authentication/AuthenticationResponse.cs | R1 version precision; behavior/User/signature guidance otherwise accepted |
| 3 | S/Client/Authentication/CredentialMatcher.cs | Accepted; Q1 actual empty-ID path checked |
| 4 | S/Client/Authentication/MatchedCredential.cs | Accepted; cached factory/wait and limitation checked |
| 5 | S/Client/Authentication/PublicKeyCredentialRequestOptions.cs | Accepted; challenge, allow-list timeout and cancellation qualifiers |
| 6 | S/Client/CeremonyDeadline.cs | Accepted; signal/start/Expired semantics |
| 7 | S/Client/IWebAuthnBackend.cs | Accepted; raw CBOR and key-agreement citation context |
| 8 | S/Client/PinUvSharedSecret.cs | Accepted; concrete protocol/caller-disposal context |
| 9 | S/Client/Registration/PublicKeyCredentialCreationOptions.cs | Accepted; challenge/attestation/timer |
| 10 | S/Client/UserVerification/UvDecision.cs | Accepted; L2 citation and missing-capability disclosure |
| 11 | S/Client/WebAuthnBackend.cs | Accepted; stateless V2 object/no negotiation |
| 12 | S/Client/WebAuthnClient.Authentication.cs | R1 version precision; eager outputs/narrowing/error/timer otherwise accepted |
| 13 | S/Client/WebAuthnClient.Ceremony.cs | Accepted; remint citation and catch/retry boundary |
| 14 | S/Client/WebAuthnClient.PinUvAuth.cs | Accepted; bounded PIN-rejection mapping |
| 15 | S/Client/WebAuthnClient.Registration.cs | Accepted; side effects/attestation/error/timer |
| 16 | S/Client/WebAuthnClient.Validation.cs | Accepted; mapping and timeout limitations |
| 17 | S/Client/WebAuthnClient.cs | Accepted; construction ownership/prompt/results/server/V2 |
| 18 | S/Client/WebAuthnClientData.cs | Accepted; manual serialization and builder limitation |
| 19 | S/Client/WebAuthnOrigin.cs | Accepted; restricted scheme/host policy |
| 20 | S/Cose/Aaguid.cs | Accepted; default/equality defect disclosed |
| 21 | S/IYubiKeyExtensions.cs | Accepted; options forwarding/transport/SCP |
| 22 | S/Preferences/AttestationPreference.cs | Accepted; None/Indirect/Enterprise qualifications |
| 23 | S/WebAuthnAuthenticatorData.cs | Accepted; independent storage/parse-not-verify |
| 24 | S/WebAuthnClientError.cs | Accepted; actual vocabulary/cancellation |
| 25 | S/WebAuthnTransport.cs | Accepted; exact enum anchor/hint definitions |
| 26 | I/AuditV2/WebAuthnAuditHardwareReproTests.cs | Accepted; entire file read, timer assumptions and present UV path |
| 27 | I/AuditV2/WebAuthnAuditHardwareTests.cs | Accepted; complete affected PRF round-trip method, known-PIN context |
| 28 | I/WebAuthnClientTests.cs | Accepted; affected complete method plus options helper, capability limits |
| 29 | I/WebAuthnExcludeListStressTests.cs | Accepted; entire file read, actual IDs/capacity/session/cleanup |
| 30 | U/Client/UserVerification/UvDecisionLogicTests.cs | Accepted; complete affected Preferred/Required cases and parameter labels |
| 31 | U/Client/WebAuthnBackendTests.cs | Accepted; complete affected ECDH/protocol tests and class ownership context |
| 32 | U/Client/WebAuthnClientCeremonyFrameTests.cs | R2; entire class read, summary otherwise matches implemented frame |
| 33 | U/Client/WebAuthnClientDataTests.cs | Accepted; entire file read, low-level topOrigin fixture |
| 34 | U/Client/WebAuthnClientGetAssertionTests.cs | Accepted; complete affected control/invalid-input/slow-factory methods and helpers |
| 35 | U/Client/WebAuthnClientMakeCredentialTests.cs | Accepted; complete affected retry/control/invalid-input/mapping methods |
| 36 | U/TestSupport/TokenBufferAssert.cs | Accepted; entire file read, actual assertion strength |

## Remaining boundaries

No additional unresolved documentation question beyond R1/R2 was identified in this batch. The deferred engineering ledger remains open by design: no request here to implement enterprise attestation, normalize every exception, impose a hard deadline, fix Aaguid/default equality, change serialization/decoder ownership, negotiate protocols, recover IDs, improve zeroing assertions, or change hardware applicability. These are not silently resolved by accepting their truthful documentation limits.

A-020 and cross-cutting module Markdown remain editor C's work. B's extension contracts and their ultimate review are not accepted or rejected based on an in-flight diff. After narrow R1/R2 correction, re-review the changed wording/evidence; campaign build/test/documentation gates remain the coordinator's later responsibility.
