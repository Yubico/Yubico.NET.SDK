# WebAuthn editor B

2026-11-03. Baseline `e353b49afa15d1d104fce70bbf1e1abc59ddb132`, branch `yubikit-docs-audit`.

## Outcome and boundaries

Applied all accepted WA-B-001–016 documentation/comment portions in B-owned C# files. No runtime expressions, API declarations, exception messages, test assertions, or fixture bytes changed. A's existing modifications and all pre-existing untracked files were preserved. No repository Markdown was edited; this external handoff report is the requested exception.

Read root and WebAuthn instructions, `style.md`, `triage.md`, and the complete `investigation-b.md`. Used its adjudicated source/specification evidence; a focused Context7 `/dotnet/docs` memory-ownership query corroborated that a read-only view is not an owned/disposable cleanup contract. No fresh protocol conformance or hardware verification is claimed.

## Finding mappings

Paths below use the exact manifest that follows. All implementation gaps remain deferred under triage's engineering ledger.

| Finding | Applied documentation/comment changes |
| --- | --- |
| WA-B-001 | `Inputs/PrfInput.cs` and `Processing/Processors/PrfProcessor.cs` explicitly disclose missing verified-PRF/UV enforcement, distinguish the WebAuthn requirement from caller Required mitigation, and avoid claiming general conformance. PRF processor/vector test summaries do not imply enforcement coverage. |
| WA-B-002 | `Inputs/CredProtectInput.cs`, `Outputs/CredProtectOutput.cs`, codec, processor, registration session, and session-test summary disclose missing client enforcement. Checking output after creation is not presented as prevention. |
| WA-B-003 | PRF input/value/result, largeBlob input/result/processor, codec, and aggregate output guidance distinguish borrowed caller inputs, directly constructed results, and SDK-produced arrays. Derived secrets and potentially sensitive plaintext must not be logged. SDK/result/client disposal does not promise result cleanup; copied-buffer cleanup does not clear originals or intermediates. No clearing of read-only spans is prescribed. |
| WA-B-004 | Public previewSign input/factory guidance uses an ordinary dictionary example, describes content comparison, and preserves caller backing-buffer lifetime. It no longer references the inaccessible comparer. |
| WA-B-005 | largeBlob support enum, input/output, processor/session context, and tests distinguish Required intent from incomplete registration enforcement, retain the discoverable-credential prerequisite, and instruct inspection of Supported. |
| WA-B-006 | largeBlob Write, authentication session/processor, and write-test comments identify the exactly-one-original-allow-entry constraint and missing client validation. Final selection is not presented as satisfying that input rule. |
| WA-B-007 | largeBlob output, aggregate output, session/processor, and storage-test comments distinguish omission, read data, empty data, write false, and propagated errors. Nullable array-to-memory conversion remains explicitly unverified; no runtime outcome is asserted. Cancellation is not described as false or swallowed. |
| WA-B-008 | largeBlob input/processor and tests name the supported largeBlobKey/storage-array route, exclude the alternative direct extension, and distinguish the SDK's unconditional write-token requirement from CTAP's conditional rule, including the alwaysUv qualification. |
| WA-B-009 | Aggregate input/output remarks explain optional requests/results, null containers versus null individual results, explicit false values, and incomplete error isolation. PRF properties explain eval defaults, credential-map precedence/validation, registration restrictions, and hmac-secret-mc availability. previewSign input/flags guidance explains allow-list mappings, multi-credential selection, and UV-derived registration policy without a public flag setter. |
| WA-B-010 | WebAuthn MinPinLength parameter, codec, processor, and registration-session comments label ignored Requested=false as an input-handling limitation, explain null omission, and identify Unicode code points and RP authorization without implying policy-setting. |
| WA-B-011 | Both sessions, PRF codec, and relevant session/PRF tests remove the fabricated §9.4 malformed-output mandate/quotation. The actual two-category catch behavior is identified as SDK isolation policy, not a normative rule. |
| WA-B-012 | previewSign codec exception/return guidance distinguishes absent outputs, delegated invalid-operation/CBOR failures, and explicit InvalidState propagation. Audit repro commentary names RegistrationExtensionSession and both caught categories, preserving the no-outer-key-substitution rationale. |
| WA-B-013 | GeneratedSigningKey, signing parameters, constructor-test summaries, and integration fixture comment preserve draft-permitted empty handles while identifying the current constructor limitation. Nonempty TBS is described as local SDK validation, not a generic draft length requirement. |
| WA-B-014 | Signing-parameter/output/generated-key guidance describes COSE_Sign_Args CBOR maps, algorithm-specific TBS/prehashing, pass-through versus semantic validation, selected/public-key algorithm distinctions, and RP-owned attestation verification. ARKG conveniences and integration bridge remain explicitly experimental and separate from generic previewSign. |
| WA-B-015 | Aggregate input docs identify the Yubico WebAuthn sign-extension draft version 4, not CTAP4. PRF and minPinLength section ranges associate versions correctly. Substantive PRF/vector references identify the existing dated L3 Recommendation. credProps reports the effective requireResidentKey parameter without the incomplete quotation. |
| WA-B-016 | Corrected full-content hashing, unused stored Flags versus separately derived ceremony byte, stale permissions/phase/fix markers, inner-attestation fixture explanation, concrete CTAP2 dependency names, and unsubstantiated historical hardware-verification narration. Retained useful wire-layout and algorithm rationale. |
| WA-A-021 dependency | `Processing/ExtensionContext.cs`, `Processing/Processors/PrfProcessor.cs`, and PRF codec-test summary identify protocol-2 hardcoding and absence of protocol-1 negotiation. Codec protocol-1 vectors are not treated as client interoperability evidence. |

## Exact edited C# manifest

32 extension source files:

- `src/WebAuthn/src/Extensions/Codec/CredProtectCodec.cs`
- `src/WebAuthn/src/Extensions/Codec/MinPinLengthCodec.cs`
- `src/WebAuthn/src/Extensions/Codec/PreviewSignCodec.cs`
- `src/WebAuthn/src/Extensions/Codec/PrfCodec.cs`
- `src/WebAuthn/src/Extensions/Inputs/CredProtectInput.cs`
- `src/WebAuthn/src/Extensions/Inputs/LargeBlobInput.cs`
- `src/WebAuthn/src/Extensions/Inputs/LargeBlobSupport.cs`
- `src/WebAuthn/src/Extensions/Inputs/PrfInput.cs`
- `src/WebAuthn/src/Extensions/Inputs/PrfInputValues.cs`
- `src/WebAuthn/src/Extensions/Outputs/CredProtectOutput.cs`
- `src/WebAuthn/src/Extensions/Outputs/LargeBlobOutput.cs`
- `src/WebAuthn/src/Extensions/Outputs/PrfOutput.cs`
- `src/WebAuthn/src/Extensions/PreviewSign/ByteArrayKeyComparer.cs`
- `src/WebAuthn/src/Extensions/PreviewSign/GeneratedSigningKey.cs`
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAuthenticationInput.cs`
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAuthenticationOutput.cs`
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignFlags.cs`
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignRegistrationInput.cs`
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignRegistrationOutput.cs`
- `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignSigningParams.cs`
- `src/WebAuthn/src/Extensions/Processing/AuthenticationExtensionSession.cs`
- `src/WebAuthn/src/Extensions/Processing/ExtensionContext.cs`
- `src/WebAuthn/src/Extensions/Processing/IAuthenticationExtensionProcessor.cs`
- `src/WebAuthn/src/Extensions/Processing/IRegistrationExtensionProcessor.cs`
- `src/WebAuthn/src/Extensions/Processing/Processors/CredPropsProcessor.cs`
- `src/WebAuthn/src/Extensions/Processing/Processors/CredProtectProcessor.cs`
- `src/WebAuthn/src/Extensions/Processing/Processors/LargeBlobProcessor.cs`
- `src/WebAuthn/src/Extensions/Processing/Processors/MinPinLengthProcessor.cs`
- `src/WebAuthn/src/Extensions/Processing/Processors/PrfProcessor.cs`
- `src/WebAuthn/src/Extensions/Processing/RegistrationExtensionSession.cs`
- `src/WebAuthn/src/Extensions/WebAuthnExtensionInputs.cs`
- `src/WebAuthn/src/Extensions/WebAuthnExtensionOutputs.cs`

8 test files:

- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs`
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnExtensionAuditReproTests.cs`
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/ExtensionSessionTests.cs`
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/LargeBlob/LargeBlobProcessorTests.cs`
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/PreviewSign/PreviewSignCodecTests.cs`
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/PreviewSign/PreviewSignSigningParamsTests.cs`
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/Prf/PrfCodecTests.cs`
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/Prf/PrfProcessorTests.cs`

The remaining B-owned files required no edit: CredBlobCodec, CredPropsInput/Output, PreviewSignErrors, both output builders, CredBlobProcessor, PreviewSignProcessor, PreviewSignProcessorTests, PrfInputTests, WebAuthnLargeBlobAuditReproTests, and TestExtensionContext. Their clean dispositions remain those in the investigation; no artificial edits were added.

## Checks and reread

- Re-read complete resulting public XML sections, codec/session/processor comment sections, and affected test comment sections, including adjacent qualifiers and unchanged explanatory context. Corrected the second-default-PRF-input wording and a session-summary indentation error during this reread.
- Static comparison of all 40 edited B-owned C# files against HEAD found identical non-comment, non-whitespace content, preserving literals. XML documentation fragments parsed successfully using Python's XML parser. This is a lexical/XML check, not compiler or analyzer validation.
- Scoped `git diff --check` reported no whitespace errors.
- A scoped source search found no remaining `CTAP v4`, `2.1-2.3 §12.7`, fabricated `ignore any extension output` quotation, obsolete `Fido2` dependency comment, `overridden by adapter`, first-four-byte hash claim, or negotiated-protocol claim under extension source.
- No build, test, formatter, hardware operation, commit, push, or delegation ran. No claim is made that examples compile, tests pass, firmware is qualified, or deferred implementation gaps are fixed.

## New questions / handoff

No new blocking question. The nullable largeBlob conversion remains unverified (WA-IMP-016), the minimal inner previewSign flags-only fixture remains missing (WA-IMP-020), and all other engineering gaps retain the owners and triggers in triage. No outcome for either gap was inferred from prose or fixture agreement.

Editor C still owns reconciliation of module Markdown and external guides: particularly PRF UV/cleanup, credProtect enforcement, largeBlob restrictions, previewSign optionality/multi-credential examples, and protocol-2-only prerequisites. This batch did not edit those surfaces.
