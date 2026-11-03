# WA-B — WebAuthn extension investigation

## Result and verification boundary

Completed read-only semantic review of all **52 assigned tracked C# files** (40 extension source files and 12 test/support files), including every explanatory comment/XML passage and comment-free units. Reviewed the assigned public declarations against both API snapshots. Baseline: `e353b49afa15d1d104fce70bbf1e1abc59ddb132`, branch `yubikit-docs-audit`, inspected 2026-11-03.

The principal public-documentation problems are the undocumented PRF conformance exception, unfulfilled credential-protection enforcement promise, incomplete sensitive-output ownership contract, inaccessible comparer guidance, and missing extension input/result conditions. Internal/test comments also contain stale implementation history and unsupported specification quotations. Meaningful behavior changes below are **deferred engineering**, not proposed changes in this documentation cycle.

No repository files were written. No generation, build, test, hardware/device operation, commit, push, or recursive agent was performed. The four pre-existing untracked files were preserved. All behavior assessments below are source inspection, not new runtime verification. Specifications were read from primary W3C/FIDO/vendor HTML; tests corroborate implementation only. No sibling SDK was treated as conformance authority.

Required instructions read: root `CLAUDE.md`, `src/WebAuthn/CLAUDE.md`, external audit `style.md`, `runs/webauthn-001/status.md`, and `webauthn-plan.md`. `src/Ctap2/CLAUDE.md` was read before inspecting the narrow CTAP dependencies. The security-guidelines skill was loaded for sensitive-data handling; its generic examples were not substituted for protocol requirements.

## Findings

### WA-B-001 — PRF guidance must disclose the WebAuthn UV requirement and current exception

**Priority / disposition:** high; documentation correction now, coordinated UV-policy engineering deferred.

**Locations:** `src/WebAuthn/src/Extensions/Inputs/PrfInput.cs:30–37`; `Processing/Processors/PrfProcessor.cs:35–38,126–127`. Dependencies: `Client/WebAuthnClient.Registration.cs:103–114`, `Client/WebAuthnClient.Authentication.cs:93–102`, module README/CLAUDE and extension architecture guidance (WA-C).

**Evidence:** The public remarks say that the client does not override `UserVerification` and instruct the caller to choose `Required` for stable output. That describes current source: `PrfProcessor.Permissions` is `None`, and the ordinary options preference goes into UV selection. However, W3C L3 REC §10.1.4 [S1] says the hmac-secret-backed extension exposes **one** PRF per credential, which **MUST** be the verified variant, overriding `UserVerificationRequirement` if needed. CTAP's two variants [S4/S5/S6] do not authorize exposing both through WebAuthn PRF. The present remarks omit the normative mismatch and read like a supported standards design choice.

**Smallest supported correction:** retain the current-behavior warning and workaround, but explicitly call this a limitation: “The current client does not enforce WebAuthn's requirement to use the verified hmac-secret PRF. Request `UserVerification = Required` for PRF evaluation; otherwise the same credential and input can produce different outputs when actual UV changes. This workaround does not make the implementation generally conformant to the PRF requirement.” Avoid presenting explicit caller `Required` as a requirement imposed on the RP by WebAuthn.

**Deferred / re-review trigger:** maintainer coordinates the PRF-specific UV decision with siblings, then re-review both ceremonies, registration-time evaluation, `PrfInput`, processor remarks, PRF tests, README/CLAUDE, architecture and examples together. The pre-existing untracked `docs/research/audit-v2-closeout-review.md:94–164` corroborates a historical decision to coordinate before changing behavior and explicitly says this was **not acceptance of the conformance risk**. It is not current-code authority and was not modified. No communication or sibling response is asserted here.

### WA-B-002 — `EnforceCredentialProtectionPolicy` promises enforcement that is absent

**Priority / disposition:** high; truthful limitation now, client-policy engineering deferred.

**Locations:** `Inputs/CredProtectInput.cs:26–31`; `Processing/RegistrationExtensionSession.cs:60–67`; `Processing/Processors/CredProtectProcessor.cs:30–40`; `Codec/CredProtectCodec.cs:30–32`. Narrow dependency: `src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs:41–64,330–333` (field use search).

**Evidence:** XML promises “credential creation fails if the policy cannot be honored.” The WebAuthn factory simply omits credProtect if GetInfo does not advertise it, including when enforcement is true. When supported, it passes the boolean into the CTAP builder; that builder assigns `_credProtectEnforcePolicy` but never reads it. Only the integer policy is encoded. Output parsing does not compare the returned policy against an enforced request. CTAP §12.1 [S3] places this decision at the **platform/client**; the boolean is not a wire flag magically enforced by the authenticator. The spec uses SHOULD NOT create without the requested protection for policies 2/3 when enforcement is true; the SDK's own public promise is stronger and is not met even on the simple unsupported-device path.

**Smallest supported correction:** explicitly state that enforcement is not currently implemented and callers must not rely on this flag to prevent unprotected creation. Do not retain the unconditional failure promise or portray silently ignoring enforcement as intended policy. Caller checking the output can detect an unsatisfactory result but cannot undo credential creation; do not recommend that as equivalent enforcement.

**Deferred / trigger:** implement pre-creation enforcement and decide how to handle absent/changed policy outputs in a separately tested client-policy change. Re-review this XML, session support-filter comments, credProtect codec/processor/output guidance, CTAP builder XML, and any extension guide. No issue number invented.

### WA-B-003 — Sensitive extension outputs have no usable documented cleanup/lifetime contract

**Priority / disposition:** high; document the current limitation, defer ownership/API and cleanup engineering.

**Locations:** `Outputs/PrfOutput.cs:17–43`; `Outputs/LargeBlobOutput.cs:23–30`; `WebAuthnExtensionOutputs.cs:43–52`; `Codec/PrfCodec.cs:71–84`; `Processing/Processors/LargeBlobProcessor.cs:128–155`; `Inputs/PrfInput.cs:74–85`.

**Evidence:** PRF outputs are symmetric secret-derived bytes, not public metadata ([S1], [S4–S6]). The codec clears the decrypted aggregate but returns independently allocated output arrays behind `ReadOnlyMemory<byte>` in non-disposable records. No result or client disposal path clears those returned arrays. `plaintext[..OutputLength].ToArray()` and corresponding slices allocate an intermediate array *and* another returned array; clearing the original `plaintext` does not clear these range temporaries. LargeBlobStorage returns decrypted plaintext (`src/Ctap2/src/LargeBlobs/LargeBlobStorage.cs:171–190`), which the processor exposes without an owner/cleanup contract. Credential blob data may also contain caller-sensitive application payloads; its decoder allocates another array (`src/Ctap2/src/Extensions/CredBlob/CredBlobAssertionOutput.cs:41–48`). `PrfInput.ComputeSalt` retains an uncleared concatenation containing the supplied PRF input; sensitivity of that input is application-dependent. CTAP builder salt-copy cleanup is a narrow cross-module dependency, not an additional module-wide audit.

**Smallest supported correction:** public PRF/result guidance should identify derived secrets, prohibit logging, and state that results currently expose read-only views with **no SDK disposal/zeroing guarantee**. Warn that clearing a caller-created copy does not clear SDK-created original/intermediate arrays. For blob outputs, distinguish arbitrary data from potentially sensitive decrypted plaintext. Do not invent a promise that `WebAuthnClient.DisposeAsync` clears responses or prescribe `ZeroMemory(result.First.Span)` (it is read-only). Directly constructed records can wrap caller memory; distinguish that from SDK-produced results.

**Deferred / trigger:** explicit owned/disposable sensitive-result design, exception-path ownership transfer, eliminating unzeroed intermediate copies, and temporary salt/input cleanup belong to engineering. Re-review all PRF/blob inputs and outputs, response lifetimes, memory helpers, examples and dependent CTAP storage/builders when that lands. .NET ownership guidance [F1] supports distinguishing ownership from a read-only view; `ReadOnlyMemory` alone does not establish a cleanup contract. This is not a claim that the public signature/key in previewSign is itself secret.

### WA-B-004 — Public previewSign dictionary guidance names an inaccessible internal type

**Priority / disposition:** medium; documentation-only correction.

**Locations:** `PreviewSign/PreviewSignAuthenticationInput.cs:84–95`; `PreviewSign/ByteArrayKeyComparer.cs:24`; module CLAUDE example `src/WebAuthn/CLAUDE.md:333–338` (WA-C).

**Evidence:** Public factory remarks tell consumers to use `ByteArrayKeyComparer.Instance`, but the comparer class is `internal` and absent from exported API snapshots. The constructor rebuilds consumer-supplied dictionaries with its internal content comparer (`:53–80`), and processor lookup also falls back to byte-content comparison. An external consumer cannot compile the recommended dictionary construction.

**Smallest supported correction:** remove the internal-type instruction; explain that dictionary keys are raw credential-ID bytes and the input normalizes comparison by byte content. Show a normal dictionary passed to the public constructor/factory. Keep key buffers unchanged for the operation: normalization copies dictionary entries, not their backing byte arrays. No need to make the comparer public.

### WA-B-005 — Large-blob `Required` can succeed without blob capability for the created credential

**Priority / disposition:** high; expose limitation now, registration-policy fix deferred.

**Locations:** `Inputs/LargeBlobSupport.cs:27–30`; `Inputs/LargeBlobInput.cs:24–27,36–37`; `Processing/Processors/LargeBlobProcessor.cs:54–71,110–118`.

**Evidence:** `Required` is documented as “credential creation fails without it.” `ForRegistration` checks only authenticator capabilities. With a supporting device and effective `requireResidentKey = false`, it sets `_requestKey = false` and continues; output reports `Supported = false`. It also accepts a successful response without a key when `_requestKey` was true, even for `Required`. Thus the SDK promise is not met on source-visible paths. CTAP §12.3 [S7] requires `rk:true` when requesting the key. W3C §10.1.5 [S2] requires selecting a capable authenticator for `required`; it is not evidence that returning an unusable credential with false fulfills the SDK's own stronger enum promise.

**Smallest supported correction:** advise configuring a discoverable credential (prefer `ResidentKey = Required` when support is essential) and inspecting `LargeBlob.Supported`; explicitly say the current `Required` value does not guarantee failure for all cases where the created credential lacks blob support. Do not silently downgrade the enum's meaning without identifying a defect.

**Deferred / trigger:** decide and implement registration constraints/required-output handling; re-review this enum, input/output XML, factory rationale and largeBlob tests (especially nonresident preferred behavior, which is not the same case as required).

### WA-B-006 — Large-blob write lacks the WebAuthn single-allow-credential check

**Priority / disposition:** medium; limitation now, validation/policy engineering deferred.

**Locations:** `Inputs/LargeBlobInput.cs:42–43`; `Processing/Processors/LargeBlobProcessor.cs:74–87`; `Processing/AuthenticationExtensionSession.cs:45–59`; tests `Extensions/LargeBlob/LargeBlobProcessorTests.cs:138–174`.

**Evidence:** W3C §10.1.5 [S2], client authentication processing: when write is present, “If allowCredentials does not contain exactly one element,” return `NotSupportedError`. The factory receives no allow list and never checks its size. Session creation also does not check it. Unit tests deliberately build writes with `allowCredentials: null`. The final ceremony narrowing a multi-entry list to one descriptor does not satisfy this RP-input rule; a discoverable assertion can also enter with no allow list.

**Smallest supported correction:** document that callers should supply exactly one `AllowCredentials` entry for writes and that the current client does not enforce this WebAuthn input constraint. Do not cite the existing no-list tests as normative permission.

**Deferred / trigger:** separately implement validation at the layer that has the original request options. Re-review write input guidance, session/processor comments, tests, and examples when changed.

### WA-B-007 — Large-blob read failure/output behavior needs explicit qualification

**Priority / disposition:** medium; API-result guidance now, error/output semantics deferred.

**Locations:** `Outputs/LargeBlobOutput.cs:23–30`; `WebAuthnExtensionOutputs.cs:45–48`; `Processing/Processors/LargeBlobProcessor.cs:128–138`; `Processing/AuthenticationExtensionSession.cs:103–115`.

**Evidence:** W3C §10.1.5 [S2] initializes the read client output, then says an unsuccessful read leaves largeBlob present but blob absent. Current code returns no largeBlob output if no response key exists, and a `CtapException` thrown by `GetBlobAsync` is not caught by the session (which catches only CBOR/InvalidOperation exceptions). That exception can fail response construction after the assertion succeeded. An `InvalidOperationException` instead causes omission of the extension output. These are not uniformly “read failed => Blob null.”

**Additional source-predicted edge / gap:** storage returns `byte[]?`; `new LargeBlobAuthenticationOutput(Blob: blob)` implicitly converts that array to `ReadOnlyMemory<byte>?`. A null array may become a present, empty memory rather than null through the array-to-memory conversion. No build/test was allowed to verify this conversion path. Treat it as a bounded engineering verification item, not an established runtime finding or a basis for promising null semantics. Context7 returned the .NET 10 implicit conversion signature [F1], but not a focused null-to-nullable example.

**Smallest supported correction:** distinguish extension output absent, read data absent, valid empty blob, and failed read; state that storage errors can currently propagate after a successful assertion. Do not advertise one null value as proof of unsupported capability or missing stored data. Keep cancellation propagation intact.

**Deferred / trigger:** define error isolation/output-presence policy and verify nullable conversion with targeted tests, then re-review `Blob`, `Written`, output-container docs and processor/session comments. Do not redesign storage here.

### WA-B-008 — Large-blob capability and token comments overgeneralize CTAP requirements

**Priority / disposition:** medium; documentation correction; broader support deferred.

**Locations:** `Inputs/LargeBlobInput.cs:24–31`; `Processing/Processors/LargeBlobProcessor.cs:25–29,44–46,92–94`.

**Evidence:** The implementation supports **the largeBlobKey + authenticatorLargeBlobs route**, requiring advertised `largeBlobKey` plus `largeBlobs:true`. CTAP 2.2/2.3 §12.4 [S8] defines an alternative direct `largeBlob` route and says authenticators MUST NOT support both extensions. A device supporting only that alternative is treated as unsupported. Public docs describe the chosen route but do not explicitly identify this support boundary.

The comment “CTAP §6.10.2: writing the array needs a token” is unconditional. The primary text [S7] checks authentication and `lbw` only **if the authenticator is protected by some form of UV or `alwaysUv` is true**; it explicitly notes the unconfigured-device exception. The SDK's `LargeBlobStorage` nevertheless requires a configured protocol/token for every write (`:140–150`). The processor requests `LargeBlobWrite` permissions, and current client authentication does aggregate them (`WebAuthnClient.Authentication.cs:93–95`): the module CLAUDE's “not yet aggregated” statement is stale (WA-C).

**Smallest supported correction:** label the supported route and say the **SDK currently requires** a PIN/UV token for writes, rather than ascribing an unconditional requirement to CTAP. Preserve that this can require UV despite `Discouraged` when verification is configured. Do not claim support for direct §12.4 or infer token availability on a key with no PIN/UV configured.

**Deferred / trigger:** direct extension support or pinless writing is a capability/behavior change; re-review support checks, input remarks, permissions/UV documentation, storage docs and tests when added. No firmware support claim established from a test name.

### WA-B-009 — Extension optionality/result meanings are largely undocumented at public entry points

**Priority / disposition:** medium; focused public guidance additions.

**Locations:** `WebAuthnExtensionInputs.cs:19–53`; `WebAuthnExtensionOutputs.cs:19–52`; `Inputs/PrfInput.cs:42–67`; `Outputs/PrfOutput.cs:17–43`; `PreviewSign/PreviewSignAuthenticationInput.cs:20–42`; `PreviewSign/PreviewSignRegistrationInput.cs:22–26,50–52`.

**Evidence:** Inputs mostly name extensions and outputs mostly say “Gets …”; nullable properties do not explain why they can be missing. Source factories filter by GetInfo support; request containers with all filtered processors still yield an output record whose properties are null, while null input containers yield null extension results. Malformed CBOR/incorrect-type outputs can be omitted. LargeBlob and PRF have explicit false results in some processed cases rather than null. PreviewSign unsupported by GetInfo is also **silently omitted**, not a typed unsupported error (both factories); `hasPreviewSign` elsewhere only affects CTAP error mapping/retry and does not change this support-filter behavior.

**Smallest supported correction:** add shared remarks that extension inputs are requests, not guarantees; callers must inspect results. Explain null containers versus null individual results and distinguish `Prf.Enabled == false`, `LargeBlob.Supported == false`, `Written == false`, absent evaluation results, and empty credBlob bytes. Refer to specific errors where known; do not promise all extension errors are ignored. W3C §9 and §9.4 [S9] support optionality/no results for ignored extensions, not every implementation-specific error policy.

Add actionable PRF shape guidance: `First`/`Second` represent the default `eval` inputs, `Second` alone is not evaluated, a credential-specific entry wins over the default, nonempty maps require an allow list, keys are unpadded base64url credential IDs, registration rejects `EvalByCredential` when the PRF processor is active, and registration evaluation needs advertised `hmac-secret-mc`. These are present in code but not discoverable from the current terse property docs. Keep UV exception WA-B-001 close to this guidance.

For previewSign, explain the nonempty allow list and one signing-parameter mapping **per allow-list entry**, including multi-credential lists, rather than leaving the caller to discover factory exceptions. Explain registration flags are derived from `UserVerification`: Required => UP+UV, all others => UP. `PreviewSignFlags` is public but no public request setter accepts it. `GenerateKey` should describe effective policy, not imply a consumer-controlled/default flag persists through the ceremony.

**Dependencies:** WA-C owns README/CLAUDE and should reconcile the stale single-credential and typed-error claims there. Add a compact complete extension example only if coordinated with WA-C; this investigator proposes no new broad guide/file.

### WA-B-010 — Minimum-PIN-length input's `Requested` property is ignored

**Priority / disposition:** medium; limitation now; local input-handling fix deferred for triage.

**Locations:** `WebAuthnExtensionInputs.cs:24,32`; `Processing/RegistrationExtensionSession.cs:63`; `Processing/Processors/MinPinLengthProcessor.cs:29–30`; `Codec/MinPinLengthCodec.cs:28–30`. Dependency: `src/Ctap2/src/Extensions/MinPinLength/MinPinLengthInput.cs:35–54`.

**Evidence:** The reused public input has `Requested { get; init; } = true`, with documentation saying true requests the value. WebAuthn checks only whether the object is nonnull, constructs a processor without passing it, and unconditionally emits `minPinLength:true`. Supplying `new MinPinLengthInput { Requested = false }` does not suppress the request.

**Smallest supported correction:** document at the WebAuthn `MinPinLength` parameter that presence currently requests the extension regardless of `Requested`; use null to omit it. Label this an input-handling limitation, not a normative interpretation of false. Also explain the returned value is a minimum in Unicode code points and is omitted unless the RP ID is authorized in the authenticator's minimum-PIN-length RP list [S10]. It is not a command to set/enforce a new PIN length.

**Deferred / trigger:** decide whether to honor false or replace the reused carrier; re-review the input parameter, processor/codec, CTAP input and any usage examples. This is a narrow possible local fix, but no implementation authorization was assumed by this investigator.

### WA-B-011 — §9.4 does not contain the claimed malformed-output rule/quotation

**Priority / disposition:** medium; citation/rationale correction, no exception-policy change implied.

**Locations:** `Processing/RegistrationExtensionSession.cs:96–122`; `Processing/AuthenticationExtensionSession.cs:88–114`; `Codec/PrfCodec.cs:50–54`; tests `Extensions/ExtensionSessionTests.cs:209,245–247`; `Extensions/Prf/PrfProcessorTests.cs:222`; architecture prose dependent on these claims (WA-C).

**Evidence:** The catch comments quote WebAuthn L3 §9.4 as “ignore any extension output it ... cannot process.” The complete dated REC §9.4 [S9] does **not** contain that sentence or a rule about swallowing malformed extension outputs; it describes client input/output dictionaries, “There MUST NOT be any values returned for ignored extensions,” and required per-extension translations. §9 permits optional/ignored extensions, and §9.3 recommends ignoring invalid **client inputs**. RP verification elsewhere permits the **RP** to ignore outputs; that is not this client's parsing rule. Do not conflate actors or turn general optionality into a universal malformed-output mandate.

**Smallest supported correction:** explain this SDK's actual isolation choice: its session catches `CborContentException` and `InvalidOperationException` per processor so those failures omit that processor's output. Cite §9 for general optionality if useful, but remove the fabricated quotation and do not claim §9.4 mandates this exact catch policy. PRF wrong-length output omission can remain as current behavior without sibling/spec-conformance shorthand. Specific previewSign `WebAuthnClientError` paths remain exceptions (WA-B-012).

**Framework evidence:** Context7 confirms the CBOR API uses incorrect-major-type/invalid-CBOR exceptions [F2], but returned .NET 11 preview pages for parts of this request. Do not infer new .NET 11 depth limits for this net10 module. Existing source catch behavior is sufficient for the proposed prose correction.

### WA-B-012 — previewSign codec exception XML does not match delegated decoder behavior

**Priority / disposition:** medium; documentation correction; unified error semantics deferred if desired.

**Locations:** `Codec/PreviewSignCodec.cs:88–101,112–150,166–182`; `Processing/RegistrationExtensionSession.cs:118`; `Processing/AuthenticationExtensionSession.cs:110`; test `AuditV2/WebAuthnExtensionAuditReproTests.cs:87–90`.

**Evidence:** Codec XML says malformed present output throws `WebAuthnClientError(InvalidState)`. Most malformed fields are decoded directly by the CTAP decoder and throw `InvalidOperationException`/CBOR exceptions; the sessions suppress the first two documented exception categories, yielding absent output. Missing entire unsigned previewSign output and decoded inner authData without attested data explicitly throw `WebAuthnClientError`; missing key 7 within an existing unsigned map throws `InvalidOperationException` in `PreviewSignCbor.DecodeUnsignedRegistrationOutput`, so the session drops that extension instead. Authentication missing signature key similarly throws `InvalidOperationException`, not the promised typed error. The test summary names removed `ExtensionPipeline` and falsely says it “only swallows CborContentException.”

**Smallest supported correction:** describe actual direct-codec exceptions and distinguish absent output, malformed decoder failures, and explicit `InvalidState` paths; update the test comment to refer to the current session and explain that `WebAuthnClientError` propagates. Do not claim all malformed output causes typed failure, or all malformed output is safely dropped.

**Deferred / trigger:** a consistent strict-versus-ignore policy and error translation is behavioral engineering. Re-review codec XML, both session summaries, output optionality guidance and regression comments when settled. The original security rationale—never substitute the outer credential key for a missing signing key—remains useful and correct [S11].

### WA-B-013 — previewSign accepts an empty generated handle but cannot sign with it

**Priority / disposition:** medium; precise limitation now, validation change deferred.

**Locations:** `PreviewSign/GeneratedSigningKey.cs:32–35`; `PreviewSign/PreviewSignSigningParams.cs:56–77`; `Extensions/PreviewSign/PreviewSignSigningParamsTests.cs:63–80`.

**Evidence:** The output correctly says the signing key handle can be empty, and vendor draft v4 §10.2.1 [S11] explicitly permits it (required member, MAY be zero-length; authenticator can store equivalent information internally). The signing constructor unconditionally rejects an empty handle, so an otherwise valid generated key cannot be round-tripped. Required `tbs` also has a local nonempty restriction; the generic draft shape is a required BufferSource/bstr without that length constraint. Do not claim every zero-length TBS is valid for every algorithm, but distinguish SDK validation from the generic draft.

**Smallest supported correction:** preserve the accurate output qualification and document that the current signing-params constructor does not support empty key handles even though the draft permits them. Preserve documented local validation on TBS, explaining that algorithms impose additional requirements. Do not erase the draft permission or label the entire draft feature unsupported based on this edge.

**Deferred / trigger:** revisit constructor restrictions and targeted round-trip tests, then re-review both public types, exceptions and examples.

### WA-B-014 — previewSign argument/attestation guidance omits useful protocol responsibilities

**Priority / disposition:** medium; documentation additions only.

**Locations:** `PreviewSign/PreviewSignSigningParams.cs:20–31,40–55`; `PreviewSign/GeneratedSigningKey.cs:36–48`; `PreviewSign/PreviewSignRegistrationOutput.cs:20–28`; `PreviewSign/PreviewSignAuthenticationOutput.cs:20–28`.

**Evidence:** Raw `additionalArgs` pass-through is correctly implemented and null-vs-empty encoding distinction is useful. But “algorithm-specific bytes” alone omits the draft requirement that present args contain a **CBOR map encoding a COSE_Sign_Args object** [S11]; empty bytes are faithfully encoded but are not automatically valid draft arguments. `tbs` may need prehashing depending on the selected algorithm. The codec decodes/repackages the generated attestation; it does not verify its signature/trust. Draft guidance tells RPs requesting attestation to verify it and retrieve the key/handle from it. The selected signing `Algorithm` may differ from the public key's `alg` for split signing/verification algorithms.

**Smallest supported correction:** state these caller/RP responsibilities locally: construct args according to the selected algorithm (omit when not needed); supply the algorithm's required TBS representation; distinguish transport pass-through from semantic validation; and verify generated-key attestation before trusting it where attestation policy is required. Document the possible algorithm-ID distinction. Retain the experimental qualification on ARKG conveniences and avoid suggesting that generic previewSign itself mandates ARKG or a 32-byte digest.

No algorithm/cryptography implementation change proposed. The v4 draft has an internally inconsistent prose sentence for `require-up` in its output description (it reverses UP/UV), while input definitions and enforcement steps consistently use UP bit 1 and UV bit 4. Cite the consistent input/enforcement text for `PreviewSignFlags`; do not copy the contradictory sentence or silently alter the enum.

### WA-B-015 — Correct specification identities and section ranges, without upgrading the target

**Priority / disposition:** low/medium; citation correction.

**Locations:** `WebAuthnExtensionInputs.cs:28,43` (“CTAP v4 draft”); `Inputs/PrfInput.cs:30,33–34`; `Codec/PrfCodec.cs:23–24`; `Processing/Processors/CredPropsProcessor.cs:24–25`; `Processing/Processors/MinPinLengthProcessor.cs:23`; PRF test summaries.

**Evidence:** previewSign **draft version 4** [S11] is a Yubico WebAuthn sign-extension Editor's Draft, not CTAP protocol version 4. “CTAP 2.1–2.3 §12.7” is incorrect for CTAP 2.1: its hmac-secret section is §12.5; CTAP 2.2 and 2.3 use §12.7 [S4/S5/S6]. The minPinLength shorthand §12.4/§12.5 is accurate as a pair but should associate versions explicitly (2.1 §12.4, 2.2–2.3 §12.5). The credProps short quote leaves out “parameter”; its substance is correct [S12]. A moving GitHub WebAuthn URL should not hide which revision supports the UV claim.

**Smallest supported correction:** identify vendor draft v4 accurately; use named anchors plus version-specific section numbers; pin substantive L3 claims to the already applicable 2026-08-25 Recommendation rather than automatically changing a standards target. The two dated CTAP documents plus 2.2 were checked to establish the existing range, not to mandate implementing every optional 2.3 extension.

### WA-B-016 — Stale local explanations and development markers

**Priority / disposition:** low; remove/rewrite documentation residue, preserve useful algorithm/fixture rationale.

**Exact candidates and smallest corrections:**

- `PreviewSign/ByteArrayKeyComparer.cs:20–22` says the hash uses only the first four bytes; `:47–57` hashes the full span with `HashCode.AddBytes`. Replace with full-content hashing (method remarks are already accurate).
- `PreviewSign/PreviewSignRegistrationInput.cs:35–41,63,70`: says an internal Flags value is “overridden by adapter.” The codec derives a separate local flags byte from the ceremony preference; it never overrides this property. Describe the effective derivation; avoid implying the unused internal property changes or consumers can select `Unattended`.
- `Extensions/ExtensionSessionTests.cs:292`: “today no processor needs any (R3 wires them)” is stale: `LargeBlobProcessor.Permissions` returns `LargeBlobWrite`. State that this fixture only requests getCredBlob and thus contributes no extra permissions; remove R3 narration.
- `Extensions/PreviewSign/PreviewSignCodecTests.cs:27–29`: “per Phase 8 spec” is an opaque project-phase marker. Name previewSign draft v4 §10.2.1 and actual input/output translation.
- Same file `:92–93`: remove “per Fix #6b”; retain the signed algorithm/unsigned inner-attestation distinction.
- Same file `:233`: “flags AND algorithm (both required per decoder)” is misleading. The fixture is building the **inner** generated-key attestation; the draft says its extension contains flags, while the **outer** registration extension contains alg. The current inner parser does not require both fields. Explain fixture shape accurately and preferably make a later engineering test cover the minimal actual inner flags-only shape; do not make test-fixture agreement the conformance argument.
- `Extensions/Prf/PrfProcessorTests.cs:245,317`: remove opaque “26a/26b” headings but keep input-validation and creation-evaluation group descriptions and real `Audit` traits.
- `Extensions/LargeBlob/LargeBlobProcessorTests.cs:57`: remove “(#5)” from a registration heading; keep registration/authentication grouping.
- `IntegrationTests/PreviewSignTests.cs:105–106`: “Verified end-to-end on YK 5.8.0 after switching …” is development history without a linked measurement artifact. Prefer present test prerequisites and assertions. Do not convert it to a new verification claim; no device work was done here.
- Same file `:121–123`: retain the meaningful experimental request-vs-output algorithm distinction, remove “is the bug class the … bridge makes explicit” narration; `:210–214` remove “26d on hardware,” retaining the useful two-credential behavioral description.
- Source codec comments repeatedly say “Fido2 decoder/encoder” while actual namespace/package is Ctap2. Use CTAP2 where identifying the concrete dependency, preserving FIDO2 where it names the protocol/application family rather than obsolete package naming.

No genuine TODO/FIXME, placeholder implementation or future-promise marker was found in the assigned source after semantic reading plus a candidate search. `CoseAlgorithm.Esp256SplitArkgPlaceholder` in an integration request is a real algorithm identifier symbol, not automatically a TODO to delete. Legitimate registration/authentication phases, key-agreement steps and explanatory byte-layout comments should be retained.

## Primary citation and claim index

All URLs below were accessed 2026-11-03; primary HTML returned 200. Findings apply to source revision above. A guessed CTAP 2.2 February URL returned 404 during discovery; one bounded follow-up found and read the actual July publication. No claim is based on the 404 page.

| ID | Specification, status, exact section / URL | Actor, qualification, supporting passage | Claims / dependent documentation |
| --- | --- | --- | --- |
| S1 | **Web Authentication Level 3**, W3C Recommendation **25 August 2026**, §10.1.4, https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#prf-extension | WebAuthn client implementing PRF on hmac-secret: “that PRF MUST be the one used for when user verification is performed. This overrides the UserVerificationRequirement if necessary.” Salt is SHA-256(UTF8Encode("WebAuthn PRF") || 0x00 || input). Registration evalByCredential unsupported; authentication map keys validated and selected entry wins over eval. Creation results may be unavailable. | WA-B-001/003/009/015; public PRF XML, processor/test comments; WA-C PRF descriptions. Salt translation and availability qualifications are substantively correct; UV exception needs explicit limitation. |
| S2 | Same W3C REC, §10.1.5, https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#sctn-large-blob-extension | WebAuthn client: read/write cannot be registration inputs; support cannot be authentication input; read+write cannot coexist. Write: “If allowCredentials does not contain exactly one element” => NotSupportedError. Failed read leaves largeBlob present but blob absent. Required registration ignores incapable candidate authenticators. | WA-B-005/006/007; largeBlob inputs/outputs/processor/tests. Current quoted phrase “required and … does not support … return NotSupportedError” is a native single-device adaptation, **not an exact quotation of this REC**; remove quotation marks or explain adaptation. |
| S3 | **CTAP 2.3**, FIDO Proposed Standard **26 February 2026**, §12.1, https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#sctn-credProtect-extension | Platform handles enforcement: for enforcement true and policies 2/3 it “SHOULD NOT create the credential in a way that does not implement the requested protection policy.” Authenticator returns the policy actually set; stricter device policy is possible. Only the policy integer is authenticator input. | WA-B-002; CredProtectInput/output/processor/codec; CTAP builder dependency. Do not treat equality to requested policy as the only possible satisfactory stronger policy without a policy decision. |
| S4 | **CTAP 2.1**, FIDO Proposed Standard **15 June 2021**, §12.5, https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-20210615.html#sctn-hmac-secret-extension | Authenticator generates CredRandomWithUV/WithoutUV, selects by actual response UV bit; salts and outputs are encrypted using negotiated protocol, results HMAC-SHA-256 (32 bytes). | WA-B-001/003/015; version-range correction. Original 2.1 PS inspected; no silent substitution of a later errata revision. |
| S5 | **CTAP 2.2**, FIDO Proposed Standard **14 July 2025**, §12.7 and §12.8, https://fidoalliance.org/specs/fido-v2.2-ps-20250714/fido-client-to-authenticator-protocol-v2.2-ps-20250714.html#sctn-hmac-secret-extension and #sctn-hmac-secret-make-cred-extension | CTAP platform/authenticator: hmac-secret-mc uses the getAssertion input/output processing at makeCredential and “hmac-secret extension MUST also be present … true.” | WA-B-009/015; creation-time evaluation source calls CTAP helper that sets both. Correct, not unfinished future work merely because W3C says “a future extension.” |
| S6 | **CTAP 2.3**, same PS as S3, §12.7/§12.8, https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#sctn-hmac-secret-extension and #sctn-hmac-secret-make-cred-extension | Same relevant encrypted salt/result and creation-time extension rules as S5. Explicitly selects CredRandom by UV flag; output1 and optional output2 are HMAC-SHA-256. | WA-B-001/003/009/015; PRF codec/decryption and processor notes. |
| S7 | CTAP 2.3 PS §12.3, §6.10.1/.2, same base URL as S3, anchors #sctn-largeBlobKey-extension, #largeBlobsFeatureDetection, #largeBlobsRW | Support requires largeBlobKey advertisement AND largeBlobs:true; rk must be true at makeCredential. Key returned in top-level 0x05 (registration), 0x07 (assertion), not authData extensions. Write authentication/lbw required if protected by UV or alwaysUv true; no such check for unconfigured authenticator. | WA-B-005/007/008; largeBlob route/input/output rationale and tests. Existing key-location rationale is correct. |
| S8 | CTAP 2.2 and 2.3 PS §12.4, S5/S3 base URLs, #sctn-largeBlob-extension | Alternative direct largeBlob route: “Authenticators MUST NOT support both extensions.” Can carry compressed blob in getAssertion and unsigned outputs. | WA-B-008; advertise selected route, not every possible CTAP largeBlob capability. No automatic optional-feature implementation requirement inferred. |
| S9 | W3C L3 REC §9, §9.3, §9.4, S1 base URL, #sctn-extensions, #sctn-extending-request-parameters, #sctn-client-extension-processing | All extensions OPTIONAL; RP must handle ignored ones. §9.4: “There MUST NOT be any values returned for ignored extensions.” No “ignore any extension output … cannot process” quote/rule in §9.4. Invalid-client-input recommendation and RP-output-ignore permission are different actors/inputs. | WA-B-009/011/012; both extension sessions, test summaries, architecture doc. Exact §9.4 anchor and complete section inspected; §9.3 anchor name is a navigation label to verify when editing (section text inspected). |
| S10 | CTAP 2.1 PS §12.4; CTAP 2.2/2.3 PS §12.5, S4/S5/S3 base URLs, #sctn-minpinlength-extension | Authenticator returns current minimum PIN length in Unicode code points only if RP ID is authorized; otherwise ignores extension/returns no output. This reports a policy, it does not set one. | WA-B-009/010/015; WebAuthn input/output docs and processor summary. |
| S11 | **DRAFT: Web Authentication sign extension, version 4**, Yubico Editor's Draft **26 August 2025**, §10.2.1, https://yubicolabs.github.io/webauthn-sign-extension/4/#sctn-sign-extension | Client flags Required=>0b101 else 0b001; map must match nonempty allowCredentials; choose applicable credential and matching params. Generated key from inner attestation, handle MAY be empty. Args MUST be a CBOR COSE_Sign_Args map; TBS/prehashing depends on algorithm; selected algorithm may differ from publicKey.alg. Authenticator outer reg output contains alg; inner signing-key attestation extension contains flags; unsigned key 7 contains CTAP-shaped attestation. | WA-B-004/009/012/013/014/015/016; all previewSign public XML, codec/processor/tests, WA-C guide. Retain draft/experimental boundary; no claim of W3C-standardized previewSign. Input/enforcement text resolves UP/UV meaning despite a contradictory output-description sentence, recorded above. |
| S12 | W3C L3 REC §10.1.3, S1 base URL, credential-properties extension section | Client sets rk to effective `requireResidentKey` parameter used in authenticatorMakeCredential. Missing rk means unknown; this implementation always produces a known bool when processor runs. | CredPropsInput/Output/processor and tests are substantively correct. Preserve nullable type's unknown meaning; do not promise only false means unsupported. |
| S13 | W3C L3 REC §16.17.1.2, https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#test-vectors-extensions-prf-ctap | Informative test vectors, not independent normative requirements. Read the one/two-input protocol-2 and one-input protocol-1 sharedSecret/saltEnc/outputEnc/output values. Assigned fixture hex matches the published values inspected. | `PrfCodecTests`, `PrfProcessorTests`; retain useful vector provenance, but “re-derived independently” is historical provenance not new execution verified by this investigation. |
| S14 | CTAP 2.3 PS §12.2.1, S3 base URL, #credBlobFeatureDetection | Client getCredBlob bool becomes authenticator `credBlob:true`; output stored bytes or empty byte string. Feature detection also describes maxCredBlobLength/dependent credProtect; makeCredential input exceeds advertised max => platform ignores it. | CredBlob codec/processor/input/output descriptions reviewed. Current source only filters on extension advertisement and reused input limits blob to 1–32 bytes. No new claim of arbitrary advertised-size support; broader capability validation is not established by tests. |

### Framework/library evidence

- **F1 — .NET memory ownership**, Context7 `/dotnet/docs`, query on buffer ownership/read-only memory; returned https://github.com/dotnet/docs/blob/main/docs/standard/memory-and-spans/memory-t-usage-guidelines.md. Relevant supporting passage: owner manages lifetime/destruction; a read-only memory consumer is not by itself an explicit disposable owner. Separate Context7 query returned .NET 10 `ReadOnlyMemory<T>` implicit array conversion: https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1?view=net-10.0. No claim that nullable conversion was executed. WA-B-003/004/007.
- **F2 — System.Formats.Cbor**, Context7 `/websites/learn_microsoft_en-us_dotnet`, focused ReadInt32 / incorrect-major-type exception query plus one bounded version-targeted follow-up. Returned ReadInt32 API and incorrect-major-type `InvalidOperationException` / invalid-CBOR `CborContentException` documentation, partly through net-11.0-pp pages. Treat this as corroborating API-family behavior only; not net10 execution evidence, and no new .NET 11 behavior adopted. WA-B-011/012.

## Dependency / re-review map

| Change or deferred decision | Source/public documentation to reopen | Other owned material |
| --- | --- | --- |
| PRF UV policy | PrfInput, PrfProcessor, both ceremony UV calls, registration-time results | WA-A UV preference/result/error contracts; WA-C README/CLAUDE and extension architecture; historical coordination decision remains historical |
| Sensitive-output ownership | PRF result records/codec, largeBlob output/processor, response containers, input-buffer lifetimes | WA-A response/client disposal; CTAP decoded credBlob, salt builder and storage owners; WA-C memory examples |
| credProtect enforcement | CredProtectInput, support filtering, codec/processor/output | CTAP builder ignored enforcement field; WA-C support/limitations and any security promises |
| largeBlob required/write/read policy | input/support/output types, processor, authentication session; permissions remain aggregated | WA-A original allow-list/UV/error contracts; WA-C stale “permissions not aggregated”; CTAP storage token requirement/alternate route |
| previewSign errors, empty handles, args/attestation | all previewSign contract types and codec/session exception docs | WA-A ceremony error mapping/attestation ownership; WA-C obsolete single-credential limitation, inaccessible-comparer example, typed-unsupported claim |
| minPinLength Requested | WebAuthn input parameter, factory/codec/processor | CTAP reused input; WA-C extension usage |
| Citation cleanup / unfinished-work residue | named exact comments in WA-B-011/015/016 | WA-C architecture/CLAUDE package names and draft identity; keep historical reports untouched |

Narrow dependencies read: CTAP module instructions; `Extensions/Shared/ExtensionBuilder.cs:35–274` plus exact enforcement-field uses/encoding lines; `Extensions/PreviewSign/PreviewSignCbor.cs:220–438`; `Extensions/CredBlob/CredBlobInput.cs`, `CredBlobAssertionOutput.cs`; `Extensions/MinPinLength/MinPinLengthInput.cs`, `MinPinLengthOutput.cs`; `LargeBlobs/LargeBlobStorage.cs:1–230`; WebAuthn ceremony permission/preparation/output paths; `Client/WebAuthnBackend.cs:1–210`; `Util/SensitiveMemory.cs`; module README extension/lifetime references and module CLAUDE (dependency only); historical untracked closeout lines 94–164. Partial dependency reads are deliberately not claims of complete cross-module/comment coverage.

## Assigned file coverage manifest

Every path below was read in full, including explanatory comments without keyword matches. Paths are repository-relative. “Clean” means no additional documentation finding beyond the listed shared findings, **not** comprehensive protocol/security conformance or executed tests.

### Extension source — 40 files

| Path | Semantic disposition |
| --- | --- |
| `src/WebAuthn/src/Extensions/Codec/CredBlobCodec.cs` | Clean translation/delegated parser explanation; shares result ownership/optionality WA-B-003/009. |
| `src/WebAuthn/src/Extensions/Codec/CredProtectCodec.cs` | Parses returned policy; enforcement limitation WA-B-002 belongs above the wire codec. |
| `src/WebAuthn/src/Extensions/Codec/MinPinLengthCodec.cs` | Presence always emits true; WA-B-010. |
| `src/WebAuthn/src/Extensions/Codec/PreviewSignCodec.cs` | Correct inner-key extraction/repacking rationale; WA-B-012/015/016 for exceptions, terminology and flags explanation. |
| `src/WebAuthn/src/Extensions/Codec/PrfCodec.cs` | Salt/result translation correct on inspected paths; WA-B-003/011/015. |
| `src/WebAuthn/src/Extensions/Inputs/CredPropsInput.cs` | Clean: parameterless presence requests client-side properties. |
| `src/WebAuthn/src/Extensions/Inputs/CredProtectInput.cs` | WA-B-002. |
| `src/WebAuthn/src/Extensions/Inputs/LargeBlobInput.cs` | WA-B-005/006/007/008/009. |
| `src/WebAuthn/src/Extensions/Inputs/LargeBlobSupport.cs` | WA-B-005. |
| `src/WebAuthn/src/Extensions/Inputs/PrfInput.cs` | WA-B-001/003/009/015; keep salt formula. |
| `src/WebAuthn/src/Extensions/Inputs/PrfInputValues.cs` | Structurally accurate first/optional-second XML; shares input lifetime and eval shape guidance WA-B-003/009. |
| `src/WebAuthn/src/Extensions/Outputs/CredPropsOutput.cs` | Clean nullable bool contract; supplied processor always knows the effective flag. |
| `src/WebAuthn/src/Extensions/Outputs/CredProtectOutput.cs` | Correct actual-policy meaning; enforcement caveat WA-B-002/009. |
| `src/WebAuthn/src/Extensions/Outputs/LargeBlobOutput.cs` | WA-B-003/005/007/009. |
| `src/WebAuthn/src/Extensions/Outputs/PrfOutput.cs` | Enabled/results creation distinction useful and correct; WA-B-003/009. |
| `src/WebAuthn/src/Extensions/PreviewSign/ByteArrayKeyComparer.cs` | WA-B-004/016; internal, not exported extension point. |
| `src/WebAuthn/src/Extensions/PreviewSign/GeneratedSigningKey.cs` | Correct inner-attestation and empty-handle statements; WA-B-013/014. |
| `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAuthenticationInput.cs` | WA-B-004/009; constructor copies entries with content comparer, not backing buffers. |
| `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAuthenticationOutput.cs` | Raw algorithm-specific signature statement clean; shares WA-B-014 algorithm responsibility. |
| `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignErrors.cs` | Actual status-to-SDK mappings accurately described; do not infer equality with draft DOMException mappings (e.g. draft ConstraintError versus SDK NotAllowed). No additional finding. |
| `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignFlags.cs` | Bit values and meanings consistent with draft inputs/enforcement; useful note needed that no public flag input setter exists (WA-B-009). |
| `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignRegistrationInput.cs` | WA-B-009/016; no public Flags setter in snapshot. |
| `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignRegistrationOutput.cs` | Generated material is handle/public key, not private key; add task/trust context WA-B-014, otherwise clean. |
| `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignSigningParams.cs` | Correct raw pass-through/null-vs-empty distinction; WA-B-003/013/014. |
| `src/WebAuthn/src/Extensions/Processing/AuthenticationExtensionOutputsBuilder.cs` | Clean internal mutable sink and property mapping; no invented public extension point. |
| `src/WebAuthn/src/Extensions/Processing/AuthenticationExtensionSession.cs` | Correct permission union/preparation/disposal; WA-B-007/009/011/012 for output policy and qualifications. |
| `src/WebAuthn/src/Extensions/Processing/ExtensionContext.cs` | Capability advertisement/option value checks accurately described. “Negotiated protocol” / CTAP2.0 availability depends on concrete backend which is V2-only; do not use this comment as proof of V1 interoperability (WA-A dependency). |
| `src/WebAuthn/src/Extensions/Processing/IAuthenticationExtensionProcessor.cs` | Clean internal per-attempt lifecycle, selected credential, nullable token and follow-up-call contracts; token “exists” means available if ceremony acquired one, not guaranteed nonnull. |
| `src/WebAuthn/src/Extensions/Processing/IRegistrationExtensionProcessor.cs` | Same clean per-attempt lifecycle; useful retry/shared-secret rationale retained. |
| `src/WebAuthn/src/Extensions/Processing/Processors/CredBlobProcessor.cs` | Correct registration-vs-getCredBlob behavior and section; shares WA-B-009 output guidance. |
| `src/WebAuthn/src/Extensions/Processing/Processors/CredPropsProcessor.cs` | Clean client-only behavior, S12 confirms effective requireResidentKey rationale; minor precise quotation WA-B-015. |
| `src/WebAuthn/src/Extensions/Processing/Processors/CredProtectProcessor.cs` | Actual source accurately delegates; missing policy enforcement is WA-B-002. |
| `src/WebAuthn/src/Extensions/Processing/Processors/LargeBlobProcessor.cs` | WA-B-005/006/007/008/011; correct top-level-key explanation retained. |
| `src/WebAuthn/src/Extensions/Processing/Processors/MinPinLengthProcessor.cs` | WA-B-010/015. |
| `src/WebAuthn/src/Extensions/Processing/Processors/PreviewSignProcessor.cs` | Multi-credential map validation/selected-entry handling matches inspected draft rules; shares WA-B-009 useful public guidance and WA-B-015 identity. |
| `src/WebAuthn/src/Extensions/Processing/Processors/PrfProcessor.cs` | Correct hash/encrypted salt/key-agreement/selection structure; WA-B-001/003/009/015; creation-time extension is real current processing, not a leftover future phase. |
| `src/WebAuthn/src/Extensions/Processing/RegistrationExtensionOutputsBuilder.cs` | Clean internal mutable sink/property mapping. |
| `src/WebAuthn/src/Extensions/Processing/RegistrationExtensionSession.cs` | Correct permission union/per-attempt dispose; WA-B-002/009/011/012; broad support-filter summary should acknowledge processed false results/client-only/special-error paths. |
| `src/WebAuthn/src/Extensions/WebAuthnExtensionInputs.cs` | WA-B-009/010/015; CTAP input reuse checked against declarations. |
| `src/WebAuthn/src/Extensions/WebAuthnExtensionOutputs.cs` | WA-B-003/009; optional init properties/default null match actual API snapshot. |

### Assigned tests/support — 12 files

| Path | Semantic disposition |
| --- | --- |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/ExtensionSessionTests.cs` | All fixture/byte-order/optional-output rationale read. WA-B-011/016; canonical string-key ordering example and UP/ED byte-layout comments are useful, not phase residue. |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/LargeBlob/LargeBlobProcessorTests.cs` | WA-B-005/006/007/008/016. Keep key-location and storage-failure behavior assertions distinct from WebAuthn compliance; no-list write fixture is not normative permission. |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/PreviewSign/PreviewSignCodecTests.cs` | WA-B-012/014/016; retain integer-keyed inner vs text-keyed WebAuthn attestation rationale and raw signature fixture qualification. |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/PreviewSign/PreviewSignProcessorTests.cs` | Clean selected-entry, no-match and map/allow-list validation explanations against draft; sibling references are corroboration only. |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/PreviewSign/PreviewSignSigningParamsTests.cs` | Raw args comment accurate; empty-handle test exposes WA-B-013 limitation, not proof of draft constraint. Some tests are validation-only; test-suite redesign/removal is out of this documentation assignment. |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/Prf/PrfCodecTests.cs` | Vector values inspected against S13, crypto shape comments useful. WA-B-003/011/015; historical independent-derivation assertion not newly rerun. |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/Prf/PrfInputTests.cs` | No non-boilerplate explanatory comments; full file read. Deterministic/different-input assertions do not independently prove spec hash formula; no comment correction needed. |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/Prf/PrfProcessorTests.cs` | Full vector/protocol/selection/validation/creation-time commentary read; WA-B-011/015/016. Support and validation assertions are scoped to processor-active supporting devices. |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnExtensionAuditReproTests.cs` | Full file read; WA-B-012 stale adapter/pipeline catch explanation. getCredBlob wire/output claims correct under S14; keep useful no-outer-key-substitution rationale. |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnLargeBlobAuditReproTests.cs` | Clean top-level key 0x05/discoverable prerequisite rationale under S7. No independent proof of all required/support cases. |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/TestSupport/TestExtensionContext.cs` | Full file read; accurate mocked advertised-support context description. Not a real device capability guarantee. |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs` | Full file read; WA-B-014/016. Preserve experimental ARKG bridge warning and actual reflection-constructor rationale; no new hardware/firmware claim. |

### Public API snapshot reconciliation

Read `src/WebAuthn/src/PublicAPI.Shipped.txt` (nullable directive only) and all relevant `PublicAPI.Unshipped.txt` extension entries, including the later PRF/largeBlob appendices (`:140–317,381–437,458–497,511–544`). Compared every assigned public type/member declaration, constructors, properties, enum values, factories and `ComputeSalt`; compiler-generated record equality/clone/deconstruct members were inventoried but do not require artificial XML findings.

No assigned explicit public declaration was found absent from these snapshots, and no internal processor/codec/builder/comparer was found falsely exported. The public flags enum is present but no public Flags property exists; the comparer is absent as expected. Output containers are non-positional and nullable properties default null; the nested output result records remain positional. Snapshot/source agreement is source evidence, **not an API-analyzer or compilation pass**. WA-C retains file ownership and final whole-surface reconciliation.

## Remaining gaps and bounded follow-up outcomes

1. **No runtime/device evidence added.** Crypto vectors and source paths were read, not executed. Nothing here certifies firmware/platform support, successful PIN policy, or comprehensive WebAuthn/CTAP conformance.
2. **PRF UV policy:** consequential question is already a coordination decision, not an invitation to investigate siblings indefinitely. Current source and exact primary clause establish the disclosure need; maintainers must settle implementation intent.
3. **LargeBlob null-array conversion:** one focused Context7 follow-up established the conversion API but did not provide a null-to-nullable example. Leave runtime result-presence edge explicitly unverified pending engineering; do not hide the independently established read-error/output issues.
4. **CBOR framework references:** one version-targeted follow-up still returned preview-version documentation in part; do not adopt unverified version-specific behavior. Existing catch source and delegated decoder source establish documentation inconsistencies without executing a malformed fixture.
5. **PreviewSign draft inconsistency:** do not “repair” enum semantics from the contradictory output paragraph. Input definitions, CDDL and algorithm checks support existing values. Any upstream vendor clarification is a separate maintainer action.
6. **Cross-module issues are bounded dependencies:** ignored CTAP enforcement storage, token-required storage policy and sensitive temporaries are named only because they affect WebAuthn promises. No independent CTAP/Core audit, issue creation or repository fix was started.
7. **Clean-unit sampling suggestions:** independently sample `CredPropsOutput` + `CredPropsProcessor` against S12, the two output builders against actual records, `PreviewSignProcessorTests` against S11 selection rules, and `WebAuthnLargeBlobAuditReproTests` against S7 top-level-key behavior. These span clean public XML/internal rationale/test comments rather than only changed findings.
