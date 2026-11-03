# Independent reviewer B — REQUIRED CHANGES

2026-11-03. Reviewed baseline `e353b49afa15d1d104fce70bbf1e1abc59ddb132` against the working-tree versions of the exact **40 C# files** in `edit-b.md` (32 extension source files, 8 test files).

**Verdict: REQUIRED CHANGES.** One remaining factual overclaim in a test summary needs a documentation-only qualification (B-R1). The substantive public-contract corrections are supported. No deferred implementation repair is requested.

## Review boundary and method

- Read root and WebAuthn instructions, the needed CTAP2 instructions, `style.md`, `triage.md`, the complete findings/citation sections of `investigation-b.md`, and `edit-b.md`. Consulted the sensitive-data guidelines without substituting their generic rules for primary protocol evidence.
- Independently read all 40 resulting files in full, their complete baseline diffs, and the affected sections in implementation context. Read dependency source from **the baseline**, including ceremony preparation/output construction, backend protocol selection/disposal, previewSign processing/decoders, the extension builder, credBlob decoding, and blob storage. Did not review C's in-flight Markdown or A/Core correction files. Required module instruction reads are not a review of C's edits.
- The A/C investigation or edit reports were not used as correctness evidence. B's investigation was a claim inventory to verify, not an authority for those claims.
- Re-fetched primary W3C, FIDO, and Yubico specifications. Direct FIDO retrieval returned HTTP 403; Exa successfully returned the primary documents, including their full relevant sections. This is specification inspection, not protocol execution.
- Independently compared comment/whitespace-stripped lexical content, preserving string/character literals, against `e353b49a` for all 40 files: **no differences**. Parsed all XML documentation fragments: **no XML parse errors**. `git diff --check e353b49a -- <exact 40 paths>` returned **0**.
- No repository edit, build, test, formatter, hardware operation, or delegation ran. The only authored file is this report. Static lexical/XML checks are not compilation, analyzer, runtime, or device verification.

## Required correction

### B-R1 — The two-credential integration summary overstates returned-ID verification

**Location:** `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs:210–214`, `TwoCredentialAllowList_SignsWithTheSelectedCredentialsEntry`.

The edited summary retains the claim that the signature verifies against the derived key of the credential **“that was actually returned.”** The fixture does not establish that association for both entries:

```csharp
var (returned, message) = response.CredentialId.Span.SequenceEqual(first.CredentialId.Span)
    ? (first, messageA)
    : (second, messageB);
Assert.True(returned.DerivedKey.VerifySignature(message, signature.Value.Span), ...);
```

At lines 253–257, every ID other than the first selects the second key. There is no assertion that the reported ID equals `second.CredentialId`, or even belongs to the original allow list. An empty or unrelated reported ID with a signature valid for the second fixture would satisfy these assertions. This is a source-visible verification limitation; it does not depend on executing the test or asserting any particular authenticator omission behavior.

**Required documentation change:** qualify the summary to describe the actual check, rather than asserting complete returned-ID binding. For example:

> Exercises two previewSign credentials, an allow list naming both, and one signing-parameter entry for each. The fixture verifies the signature with the first derived key when the reported ID matches the first credential, and otherwise with the second. It does not separately assert that the reported ID belongs to the allow list.

Keep the experimental ARKG/capability qualification. **Do not change the test assertions, fixture bytes, selection implementation, or empty-ID behavior in this documentation batch.** Stronger test coverage can remain engineering work. This is the remaining qualification in WA-B-016's test-description cleanup, not a request to revisit the unrelated A descriptor-version correction.

## Optional style suggestions

None required or proposed. The additions are direct and retain actionable input/result conditions; this review does not request a second preference-driven rewrite.

## Independently rechecked evidence

| ID | Primary evidence and actor/version | Result of recheck |
| --- | --- | --- |
| P1 | [WebAuthn L3 Recommendation, 25 August 2026, §10.1.4](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#prf-extension) | The **client** exposes only the verified hmac-secret PRF, overriding the UV requirement if necessary. Salt formula, `evalByCredential` validation/precedence, creation restriction, enabled/results distinctions, and creation-evaluation availability match the cited text. The new SDK limitation/Required mitigation wording does not pretend to implement that requirement. |
| P2 | [Same REC, §10.1.5](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#sctn-large-blob-extension) | **Client** write input requires exactly one original allowed credential. Required registration rejects/ignores incapable candidates, not the previously fabricated exact NotSupported quotation. Failed reads leave the extension present with no blob under the standard; SDK omission/error differences are now disclosed rather than equated with that standard. `supported` describes the created credential. |
| P3 | [Same REC, §9.4](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#sctn-client-extension-processing) | Re-read the complete section: it defines client processing/translation and forbids outputs for ignored extensions. It does **not** contain the old malformed-output quotation or prescribe the SDK's two-category exception catch. The new SDK-policy attribution is correct. |
| P4 | Same REC, §10.1.3, credential-properties client processing | `rk` reports the effective `requireResidentKey` **parameter** used in the authenticator invocation. The corrected processor wording preserves that distinction; it is not an independent hardware introspection result. |
| P5 | [Same REC, §16.17.1.2](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#test-vectors-extensions-prf-ctap) | Inspected the one-/two-input protocol-2 and one-input protocol-1 vectors against both PRF fixtures. Their source values/provenance are supported. These informative examples do not establish client protocol-1 negotiation or UV enforcement. No cryptographic test was run. |
| P6 | [CTAP 2.3 Proposed Standard, 26 February 2026, §12.1](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#sctn-credProtect-extension) | Enforcement is a **platform** decision: with enforcement true and policies 2/3, the platform **SHOULD NOT** create without implementing the requested protection. The wire input is the integer policy. The authenticator can return a stronger actual policy. The edited XML does not upgrade this to a universal normative MUST, promise SDK prevention, or equate checking an output with prevention. |
| P7 | [CTAP 2.1 PS, 15 June 2021](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-20210615.html), [2.2 PS, 14 July 2025](https://fidoalliance.org/specs/fido-v2.2-ps-20250714/fido-client-to-authenticator-protocol-v2.2-ps-20250714.html), and [2.3 PS](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html) | Confirmed 2.1 `minPinLength` §12.4 / `hmac-secret` §12.5 versus 2.2–2.3 §12.5 / §12.7. 2.2–2.3 `hmac-secret-mc` §12.8 uses the assertion processing and also requires `hmac-secret:true`. Inspected current 2.3 UV-dependent CredRandom selection and 32-byte HMAC outputs. No CTAP protocol-version upgrade is implied. |
| P8 | CTAP 2.3 PS §12.3, §12.4, and complete §6.10.2 | `largeBlobKey` plus `largeBlobs:true` detects the implemented route; makeCredential requests need `rk:true`; response keys are top-level 0x05/0x07. The alternative direct extension is mutually exclusive. Write token/lbw checks are conditional on configured verification **or alwaysUv enabled**. The SDK's unconditional storage-token requirement is correctly labeled SDK behavior. |
| P9 | CTAP 2.3 PS §12.5 and §12.2.1 | minPinLength reports Unicode code points and is omitted when the RP is unauthorized; it does not set policy. `getCredBlob` translates to authenticator `credBlob:true`, with stored or empty bytes. The docs distinguish empty bytes from absent client results. |
| P10 | [Yubico Web Authentication sign-extension **Editor's Draft version 4**, 26 August 2025, §10.2.1](https://yubicolabs.github.io/webauthn-sign-extension/4/#sctn-sign-extension) | Independently inspected input/output and client/authenticator processing: nonempty allow list and corresponding signing map, flags 0b101 for Required otherwise 0b001, required but potentially empty handle, algorithm-dependent TBS/prehashing, optional CBOR COSE_Sign_Args map, and possible signing/public-key algorithm distinction. Generated key/handle come from inner attestation, outer extension has algorithm, inner extension has flags. RP attestation responsibilities are not transferred to the client decoder. This is not CTAP4 or a W3C-standardized previewSign claim. |
| P11 | Same vendor draft, flags input and signing-enforcement steps | Confirmed UP/UV meaning from consistent input/enforcement text. The output-description sentence for require-up contradicts those steps; the edited enum correctly does not inherit that reversed sentence. |
| F1 | Context7 `/dotnet/docs`, [Memory usage/ownership guidelines](https://github.com/dotnet/docs/blob/main/docs/standard/memory-and-spans/memory-t-usage-guidelines.md), fresh focused query | Ownership/lifetime and read-only consumption are separate concepts. This corroborates the terminology only. The actual absence of output cleanup is established from SDK allocation/disposal source, not a generic .NET promise. No nullable-array conversion outcome was inferred. |

### Source evidence supporting the new limitations

- `RegistrationExtensionSession.Create:58–71` and `AuthenticationExtensionSession.Create:51–61`: null-container distinction, support filtering including previewSign, client-only credProps, separate blob factory rules, and active-processor validation.
- Both session `PrepareOutputsAsync` loops catch only `CborContentException` / `InvalidOperationException`. No universal catch, rollback, or translation promise was added. The currently registered processors assign their result after their relevant parse/storage work; the omission comments fit these implementations.
- `PrfProcessor:60–107,129–169,176–249`: input-map validation, no additional permissions/UV override, creation feature gating, default versus credential-map selection, shared secret and salt handling, and result parsing. `PrfCodec:65–89` clears aggregate plaintext but not the array-range/ToArray intermediates or output arrays. `PrfInput.ComputeSalt:90–101` leaves its concatenation uncleared. Baseline CTAP builder `BuildHmacSecretInput:183–207` also leaves its copied salt data uncleared.
- `PreviewSignAuthenticationInput:53–91`: content-comparer normalization copies entries, not backing buffers. Its example uses public types only. Baseline `PreviewSignProcessor:50–108` validates the allow-list mapping and uses the selected entry; it does not impose the obsolete single-credential restriction.
- `PreviewSignCodec:113–165,184–195` and baseline CTAP `PreviewSignCbor:230–261,310–394,410–437`: signed-output absence, explicit InvalidState branches, delegated missing-key/CBOR failures, inner key extraction, and signature output. No outer-key substitution or attestation verification occurs.
- `LargeBlobProcessor:58–93,127–167`: incomplete Required enforcement, missing original-list cardinality validation, read-error propagation, write false for the two caught categories, no-key omission, and no cancellation-to-false catch. Baseline storage `WriteLargeBlobArrayAsync:140–168` requires a protocol; `GetBlobAsync:179–189` returns decrypted `byte[]?` with no result cleanup owner.
- Baseline WebAuthn registration/authentication code aggregates extension permissions, obtains any required token before preparation, and builds outputs before returning results. Output preparation is outside UV escalation/error mapping. Baseline `WebAuthnBackend` hardcodes `PinUvAuthProtocolV2` and disposes protocol/session, not already returned PRF/blob arrays.

## Accepted-disposition accounting

All **WA-B-001–016** accepted documentation dispositions are accounted for. No item was silently treated as fixed behavior. B-R1 is the one incomplete qualification in the final test commentary.

| Finding | Review disposition |
| --- | --- |
| WA-B-001 | Applied and supported: verified-PRF requirement, absent enforcement, caller Required mitigation, and no general-conformance claim (P1; PRF input/processor/tests). |
| WA-B-002 | Applied and supported: enforcement flag is not implemented; returned actual policy/check-after-creation is not prevention (P6; input/output/codec/processor/session). |
| WA-B-003 | Applied and supported: borrowed inputs versus SDK-produced/directly constructed outputs; sensitive PRF/plaintext; no disposal/zeroing guarantee; copying cannot clear originals/intermediates (F1 and allocation/disposal paths). |
| WA-B-004 | Applied and supported: public ordinary-dictionary example, content comparison, caller backing-buffer lifetime; inaccessible comparer instruction removed. |
| WA-B-005 | Applied and supported: Required intent retained with explicit incomplete enforcement, discoverable prerequisite and Supported inspection (P2/P8). |
| WA-B-006 | Applied and supported: exactly one **original** allow entry for writes; no suggestion that final narrowing satisfies it (P2). |
| WA-B-007 | Applied and supported within the allowed evidence boundary: absent output/read data/empty bytes/write false/errors are not conflated; cancellation preserved; nullable conversion remains explicitly unverified. |
| WA-B-008 | Applied and supported: chosen blob route versus direct extension, SDK token requirement versus conditional CTAP rule including alwaysUv (P8). |
| WA-B-009 | Applied and supported: requests/results optionality, null container versus null fields and false values, PRF input shape/availability, previewSign mappings/multi-entry use/derived flags. Public responsibilities are locally actionable. |
| WA-B-010 | Applied and supported: nonnull minPinLength input ignores Requested=false in the supported path; null omits; reporting/units/RP authorization distinguished from setting policy. General unsupported-request filtering remains stated in the surrounding input guidance. |
| WA-B-011 | Applied and supported: fabricated §9.4 quotation/rule removed; actual two-category catch is labeled SDK policy (P3). |
| WA-B-012 | Applied and supported: absent versus malformed/delegated output and explicit InvalidState propagation distinguished; security rationale retained. |
| WA-B-013 | Applied and supported: draft-permitted empty handles retained, constructor rejection disclosed; local nonempty TBS validation not presented as a generic draft mandate (P10). |
| WA-B-014 | Applied and supported: COSE_Sign_Args map, algorithm-dependent TBS/prehashing, no semantic-validation promise, algorithm-role distinction, RP attestation responsibility, and experimental ARKG boundary (P10). |
| WA-B-015 | Applied and supported: vendor draft v4 identity, dated existing L3 REC, correct CTAP section/version pairs, effective credProps parameter (P1/P4/P7/P10). |
| WA-B-016 | Hashing, stored versus derived flags, stale markers, fixture shape, concrete dependency names, and removal of historical hardware-verification narration are correct. **B-R1 remains** in the two-credential test summary's retained returned-ID claim. Minimal inner flags-only fixture is honestly left deferred. |
| WA-A-021 dependency | Reconciled in B-owned context/processor/vector commentary: protocol 2 hardcoded; codec protocol-1 vectors are not client interoperability evidence. No A/C artifact needed to establish this. |

### Exact missing/unverified claims and deferred limits

- **Missing qualification:** B-R1's summary lacks the test's reported-ID membership/second-key fallback limitation. This is required prose correction only.
- **Unverified, not an asserted defect outcome:** null `byte[]?` to `ReadOnlyMemory<byte>?` conversion in the blob result assignment (WA-IMP-016). Neither a null nor a present-empty outcome was executed or adopted by this review. The edited warning is appropriately bounded.
- **Missing test evidence, not a documentation blocker:** minimal inner previewSign flags-only fixture (WA-IMP-020). Primary draft text and parser source support the revised explanation; the current nonminimal fixture is not treated as conformance proof.
- PRF UV enforcement, credProtect enforcement, result ownership/cleanup, full blob Required handling, original write-list validation, direct/pinless blob support, minPinLength false handling, empty-handle round-trip support, and protocol negotiation remain the established deferred engineering items. The documentation pass does not resolve them, and this review does not demand their implementation.
- No new claim of build/test success, firmware qualification, successful hardware operation, production ARKG readiness, or complete protocol conformance is supported or made.

## Per-file review coverage

Each row covers the **entire resulting file and baseline diff**, not just keyword hits. The shortened paths in the source table are relative to `src/WebAuthn/src/Extensions/`; test paths are relative to `src/WebAuthn/tests/`.

### Extension source — 32/32

| File | Evidence / result |
| --- | --- |
| `Codec/CredProtectCodec.cs` | Policy-only encoding and unused-enforcement source verified; WA-B-002 supported. |
| `Codec/MinPinLengthCodec.cs` | Unconditional true in active path and delegated parse checked; WA-B-010 supported. |
| `Codec/PreviewSignCodec.cs` | Full translation, both parse paths, explicit versus delegated failures, inner-key extraction and absence of verification checked against P10; WA-B-012/014/016 supported. |
| `Codec/PrfCodec.cs` | Enabled handling, decryption failure categories, output-length policy, returned/intermediate allocations and aggregate clearing inspected; P1/P7 and WA-B-003/011/015 supported. |
| `Inputs/CredProtectInput.cs` | Caller warning no longer promises prevention; P6 and actual filtering/unused flag agree. |
| `Inputs/LargeBlobInput.cs` | Supported route, effective rk, Required limitation, conditional CTAP versus SDK token requirement, original write list and borrowed memory checked; P2/P8. |
| `Inputs/LargeBlobSupport.cs` | Required meaning qualified without redefining unsupported success as intended behavior. |
| `Inputs/PrfInput.cs` | Formula, UV exception, default/map precedence, registration restriction, memory and temporary ownership checked; P1/P7. |
| `Inputs/PrfInputValues.cs` | First/optional Second shape and borrowed-memory/cleanup cross-reference checked. |
| `Outputs/CredProtectOutput.cs` | Actual policy/stronger policy allowed; post-creation inspection not prevention. |
| `Outputs/LargeBlobOutput.cs` | Supported versus missing; read absence/empty/error limitations; write catch/cancellation; sensitive plaintext owner distinction checked. Nullable edge remains unverified, not misreported. |
| `Outputs/PrfOutput.cs` | Derived-secret and cleanup limitations verified; First/Second/Enabled/creation Results retain their distinct meanings. |
| `PreviewSign/ByteArrayKeyComparer.cs` | Full-span content equality/hash verified against implementation. |
| `PreviewSign/GeneratedSigningKey.cs` | Inner key/handle, empty-handle permission/constructor gap, algorithm roles and RP trust boundary checked against P10. |
| `PreviewSign/PreviewSignAuthenticationInput.cs` | Constructor normalization, shallow memory retention, allow mappings and public example checked with processor and P10. |
| `PreviewSign/PreviewSignAuthenticationOutput.cs` | Unchanged algorithm-specific signature and verification responsibility checked; no private-key/verification guarantee added. |
| `PreviewSign/PreviewSignFlags.cs` | Enum values, effective UV-derived input and absence of public independent setter verified; P10/P11. |
| `PreviewSign/PreviewSignRegistrationInput.cs` | Stored Flags versus separate encoder byte, algorithm preference and public factory policy checked. |
| `PreviewSign/PreviewSignRegistrationOutput.cs` | Public key/handle/decoded attestation, not private key; RP verification condition accurately retained. |
| `PreviewSign/PreviewSignSigningParams.cs` | Borrowed memory, null-versus-empty args, CBOR-map semantics, algorithm TBS rules and local validation checked; P10. |
| `Processing/AuthenticationExtensionSession.cs` | Complete filtering, permission union, preparation, output catch and disposal loops checked; no universal error suppression promise. |
| `Processing/ExtensionContext.cs` | Support/option semantics and baseline backend hardcoded protocol 2 verified. |
| `Processing/IAuthenticationExtensionProcessor.cs` | Optional token, preparation order, per-credential input and output/token lifetime checked against ceremony implementation. |
| `Processing/IRegistrationExtensionProcessor.cs` | Per-attempt lifetime, optional token and preparation/output contract checked against ceremony implementation. |
| `Processing/Processors/CredPropsProcessor.cs` | Effective parameter reporting, no wire input verified against P4 and caller. |
| `Processing/Processors/CredProtectProcessor.cs` | Forward/parse only, no enforcement/comparison verified against codec/builder/session. |
| `Processing/Processors/LargeBlobProcessor.cs` | Complete factories, permissions, both input/output overloads, catches and absent key paths checked against storage and P2/P8. |
| `Processing/Processors/MinPinLengthProcessor.cs` | Presence-triggered true path, units/RP authorization/reporting checked against codec and P7/P9. |
| `Processing/Processors/PrfProcessor.cs` | Complete validation, key agreement, salts, credential selection, creation evaluation, parsing and secret disposal checked; P1/P7 plus backend protocol source. |
| `Processing/RegistrationExtensionSession.cs` | Complete filtering/exception isolation/output-container/disposal paths checked; current limitations preserved. |
| `WebAuthnExtensionInputs.cs` | Aggregate requests, unsupported omission, active minPinLength limitation, draft identity, getCredBlob translation checked against factories and P9/P10. |
| `WebAuthnExtensionOutputs.cs` | Container/field/false/empty distinctions, incomplete error isolation and sensitive result ownership checked against factories/builders/processors. |

### Test files — 8/8

| File | Evidence / result |
| --- | --- |
| `Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs` | Full registration, ARKG bridge, single-/two-credential assertions and helpers read; capability/experimental and empty-handle qualifications correct. **B-R1 required** for returned-ID verification overclaim. No hardware run. |
| `Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnExtensionAuditReproTests.cs` | Full mock setup/assertions read; missing unsigned previewSign case explicitly propagates InvalidState; no outer-key substitution; current session/catch categories accurately named. |
| `Yubico.YubiKit.WebAuthn.UnitTests/Extensions/ExtensionSessionTests.cs` | Complete fixtures/assertions read; wrong-type isolation, unadvertised credProtect/no enforcement coverage, largeBlob route and getCredBlob permission comment fit actual fixtures. |
| `Yubico.YubiKit.WebAuthn.UnitTests/Extensions/LargeBlob/LargeBlobProcessorTests.cs` | Preferred-versus-Required paths, storage mock success/failure, absent key and invalid combinations checked. Null allow list labeled a gap, not normative permission; read/cancellation not claimed tested. |
| `Yubico.YubiKit.WebAuthn.UnitTests/Extensions/PreviewSign/PreviewSignCodecTests.cs` | Full request/output fixtures and helpers read; integer-keyed inner shape and extra inner algorithm honestly identified; minimal flags-only gap remains deferred. |
| `Yubico.YubiKit.WebAuthn.UnitTests/Extensions/PreviewSign/PreviewSignSigningParamsTests.cs` | Full constructor tests read; arbitrary argument bytes labeled pass-through rather than valid COSE map; local empty constraints correctly distinguished from draft. |
| `Yubico.YubiKit.WebAuthn.UnitTests/Extensions/Prf/PrfCodecTests.cs` | All fixture bytes and assertions read against P5; protocol-1 codec coverage not portrayed as backend negotiation or UV policy coverage. |
| `Yubico.YubiKit.WebAuthn.UnitTests/Extensions/Prf/PrfProcessorTests.cs` | Complete validation/selection/enabled/creation-evaluation/error fixtures read; salts/shared-secret vector provenance supported by P5; no verified-PRF enforcement coverage claimed. |

## Handoff

Make the B-R1 prose-only qualification in `PreviewSignTests.cs`, retain executable-token identity, and re-review that summary against its selection/assertion block. All other B-edited C# documentation passes this independent review within the explicit static/specification boundary above. C's reconciliation and the campaign's final build/test gates remain separate work.
