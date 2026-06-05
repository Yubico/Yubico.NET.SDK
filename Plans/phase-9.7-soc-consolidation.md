# Phase 9.7 — WebAuthn ↔ Fido2 Separation-of-Concerns Consolidation

**Branch:** `webauthn/phase-9.2-rust-port` (this PR — #466)
**Authorization:** Dennis 2026-04-23 — "Ship Option B" (all violations) via `/DevTeam Ship`
**Architect verdict:** 11+ duplications across WebAuthn that under the strict no-duplication rule must consolidate to Fido2

## Constraints (NON-NEGOTIABLE — read first)

1. **Fido2 PUBLIC API SURFACE IS FROZEN.** No breaking changes to existing public types or member signatures in `src/Fido2/src/`. **Additions are fine.** If a refactor would require changing a public Fido2 method/property/type, instead add a NEW public type/method alongside, or change visibility on a NEW type only.
2. **Fido2 internals and privates ARE allowed to change.** `internal`, `private`, `protected internal` members can be renamed, moved, deleted, restructured.
3. **Fido CLI MUST continue to work.** Look in `src/` for any CLI/example projects that consume `Yubico.YubiKit.Fido2` and verify they still build + their tests pass.
4. **WebAuthn API changes are ENCOURAGED.** WebAuthn is preview-stage and secondary to Fido2. Breaking its public surface is fine if it removes duplication.
5. **All existing tests must continue to pass.** Baseline: 10/10 projects pass; Fido2 357/0; WebAuthn 90/0.
6. **No #region.** No nullable `!` suppressions without justification. Follow root `CLAUDE.md` modern-C# rules and memory hierarchy.

## Architectural Rule

**Zero duplicated code or behavior in `src/WebAuthn/`.** If both projects need a behavior, the canonical implementation lives in `src/Fido2/` and WebAuthn consumes it. WebAuthn contains only behavior that is genuinely W3C-spec-specific with no Fido2 analog.

## Violations to Fix (19 items)

### Group A — Extension Input/Output type shadowing

- [ ] **A1 (was #6) — `CredBlobInput` duplicate.**
  - WebAuthn: `src/WebAuthn/src/Extensions/Inputs/CredBlobInput.cs:25` (`record class CredBlobInput(ReadOnlyMemory<byte> Blob)` + `Validate()`)
  - Fido2: `src/Fido2/src/Extensions/CredBlobExtension.cs:39` (`class CredBlobInput { ReadOnlyMemory<byte> Blob; Encode(CborWriter); }`)
  - **Action:** Add 1-32 byte length validation to Fido2's `CredBlobInput` (this also closes Phase 9.6's `WithCredBlob` validation gap — see `Plans/phase-9.6-credblob-validation.md`). Delete WebAuthn's `CredBlobInput.cs`. Update WebAuthn adapter (`src/WebAuthn/src/Extensions/Adapters/CredBlobAdapter.cs`) and any WebAuthn public API to accept `Yubico.YubiKit.Fido2.Extensions.CredBlobInput`.

- [ ] **A2 (was #7) — `CredBlobOutput` / `CredBlobAssertionOutput` duplicate.**
  - WebAuthn: `src/WebAuthn/src/Extensions/Outputs/CredBlobOutput.cs:21,27` + decoder in `Adapters/CredBlobAdapter.cs:39-84`
  - Fido2: `src/Fido2/src/Extensions/CredBlobExtension.cs:80-138` (`CredBlobMakeCredentialOutput.Decode(CborReader)` + `CredBlobAssertionOutput.Decode(CborReader)`)
  - **Action:** Delete WebAuthn output types. Update `CredBlobAdapter` to delegate to Fido2's `Decode` methods.

- [ ] **A3 (was #8) — `MinPinLengthInput` / `MinPinLengthOutput` duplicate.**
  - WebAuthn: `src/WebAuthn/src/Extensions/Inputs/MinPinLengthInput.cs`, `Outputs/MinPinLengthOutput.cs`, decoder in `Adapters/MinPinLengthAdapter.cs:37-49`
  - Fido2: `src/Fido2/src/Extensions/MinPinLengthExtension.cs:35-100` (with `Decode(CborReader)`)
  - **Action:** Delete WebAuthn input/output types. Update adapter to delegate.

- [ ] **A4 (was #9) — `LargeBlobInput` + `LargeBlobSupport` enum duplicate.**
  - WebAuthn: `src/WebAuthn/src/Extensions/Inputs/LargeBlobInput.cs:20-37`
  - Fido2: `src/Fido2/src/Extensions/LargeBlobExtension.cs:32-92` (same enum, same shape)
  - **Action:** Delete WebAuthn copy. Adapter consumes Fido2 type. Preserve any WebAuthn-spec-only validation (e.g., `Required` rejection in `LargeBlobAdapter.cs:30-42`).

- [ ] **A5 (was #10) — `PrfInput` + `PrfEvaluation` (salts) + `EvalByCredential` model duplicate.**
  - WebAuthn: `src/WebAuthn/src/Extensions/Inputs/PrfInput.cs:22-47`
  - Fido2: `src/Fido2/src/Extensions/PrfExtension.cs:35-98` (`PrfInput` + `PrfInputValues`)
  - **Action:** Delete WebAuthn `PrfInput` types. Adapter (`PrfAdapter`) translates W3C `evalByCredential` filter logic → Fido2 `PrfInput`. The W3C-shaped allow-list filter stays in WebAuthn (legitimate adapter logic).

- [ ] **A6 (was #11) — `PrfAdapter.ParseAuthenticationOutput` CBOR decoder.**
  - WebAuthn: `src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs:113-184`
  - Fido2: `src/Fido2/src/Extensions/PrfExtension.cs:106-163` has `PrfOutput.FromHmacSecretOutput` but no CBOR-map decoder
  - **Action:** Add `PrfOutput.Decode(CborReader)` to Fido2's `PrfExtension.cs` (decoding `eval/first/second` map). Adapter calls Fido2's decoder.

### Group B — Dual identity types

- [ ] **B1 (was #13) — `WebAuthnCredentialDescriptor` duplicate.**
  - WebAuthn: `src/WebAuthn/src/WebAuthnCredentialDescriptor.cs:28-36`
  - Fido2: `src/Fido2/src/Credentials/PublicKeyCredentialTypes.cs:32-161` (`PublicKeyCredentialDescriptor` — superset with CBOR encode/decode)
  - **Action:** Delete WebAuthn type. Update `WebAuthnClient` API and `WebAuthnCredentialRequestOptions.AllowCredentials` (and similar) to use `Fido2.Credentials.PublicKeyCredentialDescriptor`.

- [ ] **B2 (was #14) — `WebAuthnRelyingParty` duplicate.**
  - WebAuthn: `src/WebAuthn/src/WebAuthnRelyingParty.cs:25-36`
  - Fido2: `src/Fido2/src/Credentials/PublicKeyCredentialTypes.cs:175-258` (`PublicKeyCredentialRpEntity` superset)
  - **Action:** Delete WebAuthn type. Update `WebAuthnCredentialCreateOptions.Rp` shape.

- [ ] **B3 (was #15) — `WebAuthnUser` duplicate.**
  - WebAuthn: `src/WebAuthn/src/WebAuthnUser.cs:25-41`
  - Fido2: `src/Fido2/src/Credentials/PublicKeyCredentialTypes.cs:272-419` (`PublicKeyCredentialUserEntity` superset)
  - **Action:** Delete WebAuthn type. Update `WebAuthnCredentialCreateOptions.User` shape and any `MatchedCredential.User` mapping.

### Group C — Attestation envelope + decoders

- [ ] **C1 (was #4) — Attestation statement decoders for `packed`/`fido-u2f`/`apple`/`none`.**
  - WebAuthn: `src/WebAuthn/src/Attestation/AttestationStatement.cs:91-147,170-208,229-263,280-284`
  - Fido2: `src/Fido2/src/Credentials/MakeCredentialResponse.cs:266-387` (single `AttestationStatement` decoder)
  - **Action:** Promote typed attestation-statement variants (Packed / FidoU2F / Apple / Tpm / None) to Fido2 (or expose Fido2's existing decoder via internal+InternalsVisibleTo if cleaner). WebAuthn's `WebAuthnAttestationObject.Decode` consumes Fido2's typed variants.

- [ ] **C2 (was #5) — Typed attestation statement record hierarchy + format identifier.**
  - WebAuthn: `src/WebAuthn/src/Attestation/AttestationStatement.cs:23-300`, `AttestationFormat.cs:23-66`
  - **Action:** Move the typed variant hierarchy to `src/Fido2/src/Credentials/` (new file: `AttestationStatementVariants.cs` or fold into existing). Mark as `public` in Fido2 (this is an *addition* to Fido2's public API, not a change to existing). WebAuthn re-exports or aliases.

- [ ] **C3 (was #18) — `WebAuthnAttestationObject.EncodeAttestationObject` envelope writer.**
  - WebAuthn: `src/WebAuthn/src/Attestation/WebAuthnAttestationObject.cs:152-176`
  - **Action:** Add helper to Fido2 (e.g., `AttestationEnvelopeWriter.Write(CborWriter, ReadOnlyMemory<byte> authData, AttestationStatement attStmt, string fmt, bool textKeyed)`). WebAuthn calls with `textKeyed: true`. Fido2 internal users (if any) can call with `textKeyed: false`.

- [ ] **C4 (was #19) — `WebAuthnAttestationObject.Decode` envelope decoder.**
  - WebAuthn: `src/WebAuthn/src/Attestation/WebAuthnAttestationObject.cs:66-113`
  - Fido2: `src/Fido2/src/Credentials/MakeCredentialResponse.cs:148-227` already parses the same fields with int keys
  - **Action:** Add Fido2 helper that decodes the envelope with configurable text/int keys, shared by both layers.

### Group D — COSE consolidation

- [ ] **D1 (was #1) — Parallel COSE encoders.**
  - WebAuthn: `src/WebAuthn/src/Cose/CoseKey.cs:121-237` (typed `Encode()`)
  - Fido2: `src/Fido2/src/Cbor/CoseKeyWriter.cs:30` (internal dict-based)
  - **Action:** Make Fido2's `CoseKeyWriter` the single canonical encoder. Either (a) move WebAuthn's typed `CoseKey` records into Fido2.Cose and have them call `CoseKeyWriter` internally, or (b) keep typed model in Fido2.Cose and delete `CoseKeyWriter` if the typed model fully replaces it.

- [ ] **D2 (was #2) — Typed COSE key model.**
  - WebAuthn: `src/WebAuthn/src/Cose/CoseKey.cs:27-254` (EC2/OKP/RSA/Other discriminated records)
  - **Action:** Move the typed model to `src/Fido2/src/Cose/` (or `Cbor/`). Mark `public` in Fido2 (addition). WebAuthn re-exports under its current namespace if RP-facing API stability matters; otherwise WebAuthn callers update their using-statements.

### Group E — AAGUID byte-order helper

- [ ] **E1 (was #3) — Big-endian↔mixed-endian Guid conversion.**
  - WebAuthn: `src/WebAuthn/src/Cose/Aaguid.cs:53-71` (forward) and `:85-105` (reverse)
  - Fido2: `src/Fido2/src/Credentials/AttestedCredentialData.cs:116-138` (`ParseAaguid` reverse only)
  - **Action:** Add internal helper `Fido2.Cbor.AaguidConverter` (or extension method) with both `ToBigEndian(Guid) → byte[16]` and `FromBigEndian(ReadOnlySpan<byte>) → Guid`. Replace duplicated logic in `AttestedCredentialData.ParseAaguid` and WebAuthn's `Aaguid` constructors. WebAuthn's `Aaguid` struct can keep its public surface (RP-facing typed wrapper) but internals call Fido2.

### Group F — PreviewSign decoder symmetry

- [ ] **F1 (was #21) — PreviewSign output CBOR decoders.**
  - WebAuthn: `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignCbor.cs:66-194` (`DecodeUnsignedRegistrationOutput`, `DecodeAuthenticationOutput`)
  - Fido2: `src/Fido2/src/Extensions/PreviewSignExtension.cs` has `EncodeRegistrationInput` / `EncodeAuthenticationInput` but no decoder
  - **Action:** Add `PreviewSignCbor.DecodeRegistrationOutput(CborReader)` and `DecodeAuthenticationOutput(CborReader)` to Fido2's `PreviewSignExtension.cs`. Return typed Fido2 records (e.g., `PreviewSignRegistrationOutput`, `PreviewSignAuthenticationOutput`).

- [ ] **F2 (was #22) — `PreviewSignAdapter.ParseRegistrationOutput` CBOR map reading.**
  - WebAuthn: `src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs:188-253` (lines 197-219 read `alg`/`flags`)
  - **Action:** Adapter calls Fido2's new `PreviewSignCbor.DecodeRegistrationOutput`. Translates Fido2 typed output → WebAuthn `GeneratedSigningKey` / `PreviewSignAuthenticationOutput`. No CBOR reading at WebAuthn layer.

### Group G — Cleanup

- [ ] **G1 (was #12) — Duplicate `ByteArrayKeyComparer`.**
  - Public: `src/WebAuthn/src/Extensions/PreviewSign/ByteArrayKeyComparer.cs:26-61`
  - Private: `src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs:189-202`
  - **Action:** Delete the private copy in `PrfAdapter`. Reference the public singleton. Better: move the comparer to a shared utility location in Fido2 (e.g., `Fido2.Cbor.ByteArrayComparer`) since the use-case (CBOR map keys) is generic; WebAuthn re-uses.

- [ ] **G2 (was #41) — Dead `using` alias.**
  - WebAuthn: `src/WebAuthn/src/Client/FidoSessionWebAuthnBackend.cs:19` (`using Fido2AttestationStatement = ...` — zero usages)
  - **Action:** Delete the line.

## Acceptance Criteria

1. ✅ All 19 violations addressed (or, for any deferred, reason explicitly recorded in this doc).
2. ✅ `dotnet toolchain.cs build` returns 0 errors.
3. ✅ `dotnet toolchain.cs test` returns all 10 projects passing. WebAuthn ≥ 86 tests (some may be removed/merged from 90 baseline; net-new count ≥ original-minus-removed). Fido2 ≥ 357 tests.
4. ✅ Fido2's existing public API surface unchanged (only additions allowed). Verify by scanning `git diff src/Fido2/src/` for any modified or deleted public types/members; if found, justify or undo.
5. ✅ Fido CLI (look for `src/Fido2/cli/` or similar) still builds + tests pass.
6. ✅ Code follows root `CLAUDE.md` rules: file-scoped namespaces, `is null` patterns, switch expressions, collection expressions, no `#region`, no unjustified `!`, modern Span/Memory APIs.
7. ✅ All deletions and moves preserve git-traceable history where possible (consider `git mv` for file moves).

## Completion Record (2026-04-24)

**Shipped: 12/19 violations (Groups A, B, D, E, F, G — complete).**

### ✅ Done
- **Group A (6/6)** — A1 CredBlobInput, A2 CredBlobOutput, A3 MinPinLength, A4 LargeBlob, A5 PrfInput, A6 PrfAdapter decoder. Phase 9.6 (`WithCredBlob` 32-byte validation) absorbed into A1 and is now also closed.
- **Group B (3/3)** — B1 WebAuthnCredentialDescriptor → PublicKeyCredentialDescriptor, B2 WebAuthnRelyingParty → PublicKeyCredentialRpEntity, B3 WebAuthnUser → PublicKeyCredentialUserEntity.
- **Group D (2/2)** — D1+D2 COSE typed model promoted to `Yubico.YubiKit.Fido2.Cose.CoseKey` + `CoseAlgorithm` (new public API additions). WebAuthn deletes its `Cose/CoseKey.cs`.
- **Group E (1/1)** — E1 AAGUID converter shared helper at `Yubico.YubiKit.Fido2.Cbor.AaguidConverter` (internal); replaces duplicated big-endian↔mixed-endian logic in both layers.
- **Group F (2/2)** — F1+F2 PreviewSign decoders moved into `Yubico.YubiKit.Fido2.Extensions.PreviewSignCbor` alongside the encoders. WebAuthn adapter no longer reads CBOR.
- **Group G (2/2)** — G1 deleted private `ByteArrayComparer` in `PrfAdapter`, G2 deleted dead `using Fido2AttestationStatement = ...` alias.

### ⏳ Deferred to Phase 9.8
- **Group C (0/4)** — C1, C2, C3, C4 attestation envelope + typed variants. Blocker: Fido2's existing `public sealed class AttestationStatement` (consumed via `MakeCredentialResponse.AttestationStatement` property) collides with the typed-variant promotion. Replacing it would break Fido2's public API surface, which Dennis explicitly froze. Filed as `Plans/phase-9.8-attestation-typed-variants.md` with architectural options (Option A: breaking-change replacement is the right answer but needs explicit Fido2 maintainer sign-off).

### Constraint compliance
- ✅ Fido2 public API: only additions (CoseKey, CoseAlgorithm, AaguidConverter, decoders); zero breaking changes to existing types. CredBlobInput.Blob property body change is source-compatible.
- ✅ All 10 projects passing tests (Fido2 357/0, WebAuthn 90/0).
- ✅ Build clean (0 errors).
- ✅ No sed/awk on source code (after engineer's first-pass mistake was reverted).

### Notes from execution
- Engineer's first-pass attempt used `awk '!seen[$0]++'` and `sed` patterns to do cross-file type renames; mangled ~10 Client files. Recovery: `git restore` + redo with Edit tool only. Lesson: **never use batch text manipulation on source files; use the Edit tool one file at a time, build after each file.** Captured as a learning-frame.
- Engineer's second pass (after lesson) was clean. Group C blocker was architectural, not procedural — engineer handled it correctly by stopping at a clean checkpoint.
- Sia (orchestrator) handled the Group C orphan-file cleanup directly (delete 2 files, fix 2 integration test instantiations) rather than re-dispatching, since it was a sub-5-minute fix.

## Out of Scope

- Specializations (rows 16, 20, 23-27, 37, 38, 40 in architect's report) — these are genuinely WebAuthn-only spec concerns with no Fido2 analog.
- Adapters (rows 17, 28-36, 39 in architect's report) — these are legitimate translation/delegation layers, not duplications.
- Phase 10 (ARKG, multi-credential probe, sig-verify) — separate tracker `Plans/phase-10-previewsign-auth.md`.

## Reference

- Architect re-audit: see Sia conversation 2026-04-23
- Architectural rule: `~/.claude/projects/-Users-Dennis-Dyall-Code-y-Yubico-NET-SDK/memory/feedback_no_duplication_rule.md`
- Original architect verdict (now superseded): "Layering clean with 2 minor cohesion observations" — was wrong under the strict rule
