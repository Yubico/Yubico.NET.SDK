# Audit Gate 2 — WebAuthn Phases 7-8 (previewSign)

**Branch:** webauthn/phase-8-previewsign-wire
**Audit date:** 2026-04-22
**Scope:** Phase 7 + Phase 8 changes only (compare to gate-1-fixup baseline)
**Auditor model:** Claude (general-purpose; tier unknown — peer-skepticism mode)

## Summary

- Critical: 3
- High: 4
- Medium: 5
- Low/Info: 4
- **Block-ship?** **YES** — three Critical findings represent fundamental wire-format and decoder bugs that will not interoperate with any real authenticator.

## Spec conformance results

| Requirement | Status | Notes |
|---|---|---|
| Extension identifier `"previewSign"` exact spelling | PASS | Consistent across all 14 occurrences |
| Registration input `{3: alg-array, 4: flags-byte}` | PASS | Hex `A2 03 82 26 28 04 01` matches spec |
| Authentication input `{2: kh, 6: tbs, 7?: args}` (single map per spec §10.2.1 step 9) | **FAIL** | C# encodes credential-keyed outer map; spec says single map for chosen credential |
| Registration output (signed) `{3: alg, 4: flags, 6: sig}` decoder | PARTIAL | Decoder defined but always returns null (line 178-179); fallback path is dead |
| Registration output (unsigned) `{7: att-obj}` decoder reads from `unsignedExtensionOutputs` | **FAIL** | Reads from `authData.extensions["previewSign"]` instead of top-level `unsignedExtensionOutputs` map |
| Authentication output `{6: sig}` | PASS | Reads from authData extensions correctly |
| Flag byte semantics: only `0b000`, `0b001`, `0b101` valid | PASS | `IsValid()` enforces; tested for invalid patterns 0b011, 0b100, 0b110, 0b111 |
| `additionalArgs` wrapped as bstr when present, omitted when null | PASS | Verified in encoder + tests |
| Validation timing: client-side before CTAP roundtrip | PASS | `Build*ExtensionsCbor` called before backend invocation |
| Verified attestation supersedes loose values (spec §4) | N/A | Adapter prefers unsigned form, but unsigned path is broken (see Critical) |
| Empty allowList throws on authentication | PASS | `BuildAuthenticationCbor` throws `InvalidRequest` |
| signByCredential coverage check (every allowCredentials id present) | PASS | Iterates allowCredentials with `ByteArrayKeyComparer` |
| 6 CTAP error codes mapped | PASS (defined) / **FAIL (unused)** | `PreviewSignErrors.MapCtapError` defined but never called from any pipeline path |
| Algorithms: array of negative ints | PASS | `Esp256SplitArkgPlaceholder` (-65539) round-trips via `WriteInt32` |

## Findings

### CRITICAL

#### C-1. Authentication input wire format is wrong — credential-keyed outer map is not the spec
- **Severity:** Critical
- **Category:** spec-conformance
- **File:** `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignCbor.cs:92-126`
- **Description:** `EncodeAuthenticationInput` writes a CBOR map keyed by credential-id whose values are the per-credential `{2: kh, 6: tbs, 7?: args}` maps. The spec (§10.2.1, "Authenticator extension input" CDDL plus client extension processing step 9) and the Swift reference (`yubikit-swift/.../CTAP/Extensions/PreviewSign.swift:193-208` and `Backend+Extensions.swift:216-227`) require the SDK to encode ONLY the chosen credential's parameters as a single, flat map `{2: kh, 6: tbs, 7?: args}`. Selection happens client-side after probing the authenticator with `up=false`. The C# wire format will be rejected by any compliant authenticator with `CTAP2_ERR_MISSING_PARAMETER` (no top-level `kh` key found).
- **Evidence:** Spec line 4998-5001:
  > 9. Set the `previewSign` authenticator extension input to a CBOR map with the entries: `kh`: `signInputs.keyHandle` … `tbs`: `signInputs.tbs` … `args`: `signInputs.additionalArgs`

  Swift `Backend+Extensions.swift:216-227` only invokes `ps.getAssertion.input(keyHandle: params.keyHandle, …)` for `selectedCredentialId`.
  C# encoder writes `WriteStartMap(input.SignByCredential.Count)` and iterates ALL entries.
- **Recommended fix:** Refactor to send only the chosen credential's params. Either (a) add a credential-selection step to `BuildAuthenticationCbor` (probing `up=false` first, like Swift does in `Client+GetAssertion.swift:128-148`), or (b) split the API: keep `signByCredential` as the application-facing input dictionary, but have the pipeline pick one and call a new `EncodeChosenSigningParams(PreviewSignSigningParams)` that emits the flat map. Test 7 (`PreviewSign_Authentication_RoutesCorrectSigningParams_ToBackend`) currently asserts the wrong wire format and must be rewritten.

#### C-2. Unsigned registration output decoder reads from wrong CBOR location
- **Severity:** Critical
- **Category:** spec-conformance
- **File:** `src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs:179-196` and `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignCbor.cs:205-318`
- **Description:** Per spec §10.2.1 step 5 (registration), the chosen `algorithm` is read from `authData.extensions["previewSign"][alg]` while the attestation object is read from the top-level CTAP2 `unsignedExtensionOutputs["previewSign"][att-obj]` (a SEPARATE field on the MakeCredential response, not embedded in authData). The C# adapter reads everything from `authData.ParsedExtensions["previewSign"]` and expects the `att-obj` (key 7) to live there. There is no plumbing for `unsignedExtensionOutputs` anywhere in the WebAuthn module (verified via grep: zero references). With a real authenticator response, `DecodeUnsignedRegistrationOutput` will throw `InvalidState` on every successful registration because key 7 is never in the embedded extension map.
- **Evidence:** Spec line 4965-4967:
  > Let unsignedExtOutputs denote the unsigned extension outputs. Set … `attestationObject` … `unsignedExtOutputs["previewSign"][att-obj]`

  Swift `PreviewSign.swift:146-149` reads `response.unsignedExtensionOutputs?[…]` (a top-level field on `MakeCredential.Response`).
  Grep `unsignedExtensionOutputs|UnsignedExtensions` over `src/WebAuthn/src/` returns zero matches.
- **Recommended fix:** Add `UnsignedExtensionOutputs` (CBOR map) to whatever response DTO carries `MakeCredential.Response` data into the WebAuthn pipeline, plumb it through `ParseRegistrationOutputs`, and rewrite `PreviewSignAdapter.ParseRegistrationOutput` to take both the authData extensions (for `alg`) and the unsigned outputs (for `att-obj`). Until plumbed through, mark `DecodeUnsignedRegistrationOutput` as TODO/unused and have the parser return a `GeneratedSigningKey` populated from authData extensions (alg, flags) and the attested credential data (keyHandle from credentialId, publicKey from credentialPublicKey) — matching Swift fallback at `PreviewSign.swift:170-176`.

#### C-3. `DecodeSignedRegistrationOutput` always returns null — registration output never populated for signed form
- **Severity:** Critical
- **Category:** spec-conformance / dead code
- **File:** `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignCbor.cs:143-188`
- **Description:** The "signed registration output" decoder reads the alg/sig/flags from the CBOR map, but at line 175-179 unconditionally returns `null` with comment "less trusted per spec §4". This means the fallback at `PreviewSignAdapter.cs:193` (`output ??= PreviewSignCbor.DecodeSignedRegistrationOutput(rawCbor)`) is unreachable: if `DecodeUnsignedRegistrationOutput` throws (which it currently always does for real responses — see C-2), no fallback runs and the entire registration output is lost as a thrown exception. If `DecodeUnsignedRegistrationOutput` returns successfully, the `??=` short-circuits. Either way `DecodeSignedRegistrationOutput` never produces a value.
- **Evidence:**
  ```csharp
  // Line 175-179
  // For signed output, we don't have the full GeneratedSigningKey structure
  // This variant is less trusted per spec §4
  // Return null to indicate we should prefer the unsigned att-obj variant
  return null;
  ```
- **Recommended fix:** Either delete the method (and remove the dead `??=` fallback in the adapter), or implement it properly to return a `PreviewSignRegistrationOutput` populated from the signed form's alg/flags plus the authData attested credential data for keyHandle/publicKey. Note: per spec, the signed form is the COMMON case (lives in `authData.extensions["previewSign"]`). The "unsigned" form is the SEPARATE attestation-object delivery via top-level `unsignedExtensionOutputs`. The current naming and fallback ordering invert the spec's relationship.

### HIGH

#### H-1. `PreviewSignErrors.MapCtapError` is dead code — CTAP errors never get typed mapping
- **Severity:** High
- **Category:** spec-conformance
- **File:** `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignErrors.cs:31`
- **Description:** The mapper for the 6 spec-defined CTAP error codes (UnsupportedAlgorithm, InvalidOption, UpRequired, PuvathRequired, InvalidCredential, MissingParameter) is implemented but never invoked from `WebAuthnClient`, `ExtensionPipeline`, or the adapter. A `CtapException` returned from the backend will surface to the caller untyped instead of as `WebAuthnClientError(NotSupported)`, `…(NotAllowed)`, etc. Spec parity requires these to be mapped at the previewSign call boundary.
- **Evidence:** `grep -rn "PreviewSignErrors\|MapCtapError" src/WebAuthn/src` returns only the definition site.
- **Recommended fix:** Wrap the backend MakeCredential / GetAssertion call sites in `WebAuthnClient` (or in the adapter) with try/catch on `CtapException`, calling `PreviewSignErrors.MapCtapError(ex)` when `inputs.PreviewSign is not null`. Add tests that simulate each CTAP status and assert the mapped `WebAuthnClientErrorCode`.

#### H-2. Manual canonical sort in `ExtensionPipeline` uses ordinal string compare, not CTAP2 canonical (length-then-lex)
- **Severity:** High
- **Category:** spec-conformance / CBOR parity
- **File:** `src/WebAuthn/src/Extensions/ExtensionPipeline.cs:138-164` (registration) and `:259-285` (authentication)
- **Description:** The merge logic uses `string.CompareOrdinal(key, "previewSign") > 0` to decide insertion position. CTAP2 canonical CBOR ordering is **length-ascending first, then lexicographic** — not pure ordinal/lex. Comparing real extension keys: ordinal places `"largeBlob"` < `"largeBlobKey"` < `"minPinLength"` < `"prf"` < `"previewSign"` (P-uppercase < p), but CTAP2 canonical places `"prf"` first (length 3), then `"credBlob"` (8), then `"largeBlob"` (9), then ties at length 11 sorted lex (`credProtect`, `hmac-secret`, `previewSign`), then ties at length 12 (`largeBlobKey`, `minPinLength`). Because the writer is in `Ctap2Canonical` mode it will throw on `Encode()` if keys are written out of canonical order — meaning the merge path will throw `InvalidOperationException` whenever standard extensions and previewSign are combined (e.g., `prf + previewSign`, `credBlob + previewSign`).
- **Evidence:** Line 145 comment is wrong: "previewSign comes after prf but before others alphabetically" — `prf` is length 3 so it always comes first regardless of mode; `previewSign` (length 11) comes after `largeBlob` (length 9) by length, and `string.CompareOrdinal("largeBlob", "previewSign")` is negative (won't trigger insertion before previewSign), so the code happens to work for that specific pair — but for `largeBlobKey` (length 12), `CompareOrdinal("largeBlobKey", "previewSign") < 0` (l < p) so previewSign is written AFTER largeBlobKey — but canonical order requires previewSign (len 11) BEFORE largeBlobKey (len 12). The Ctap2Canonical writer will throw.
- **Recommended fix:** Either (a) accumulate all entries into a `SortedDictionary<(int len, string key), ReadOnlyMemory<byte>>` keyed by (length, ordinal) before writing, or (b) write the standard CBOR + previewSign into separate buffers, decode them as `(string, value)` pairs into one list, sort using CTAP2 canonical key comparer, then write in order. Add a test that combines previewSign with credProtect AND largeBlobKey AND minPinLength to exercise the multi-length sort.

#### H-3. `BuildAuthenticationCbor` cannot validate signByCredential if the input was constructed with a different equality comparer
- **Severity:** High
- **Category:** spec-conformance / API contract
- **File:** `src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs:140-152`
- **Description:** The adapter constructs a fresh `HashSet<ReadOnlyMemory<byte>>` from `input.SignByCredential.Keys` using `ByteArrayKeyComparer.Instance`, then iterates `allowCredentials` calling `Contains(allowedCred.Id)`. This works only if the caller used `ByteArrayKeyComparer` when building the dictionary (otherwise reference equality on `ReadOnlyMemory<byte>` is meaningless and the keys are still byte-distinct memory regions). The adapter relies on the dictionary's `Keys` enumeration order/identity rather than its lookup. This is correct as written, but the public `PreviewSignAuthenticationInput` constructor does NOT enforce that callers supply the right comparer — a caller who builds `Dictionary<ReadOnlyMemory<byte>, …>()` (default comparer) will silently pass validation in some cases and fail in others depending on whether `ReadOnlyMemory<byte>` happens to wrap the same array. Public API note in `PreviewSignAuthenticationInput.cs:67-70` ("Use `ByteArrayKeyComparer.Instance` when constructing the dictionary") is documentation, not enforcement.
- **Recommended fix:** In the constructor of `PreviewSignAuthenticationInput`, defensively rebuild the dictionary with `ByteArrayKeyComparer.Instance` if the supplied dictionary is not already using it. Alternatively, change the public type to accept `IReadOnlyCollection<KeyValuePair<ReadOnlyMemory<byte>, PreviewSignSigningParams>>` and build the comparer-correct dictionary internally.

#### H-4. `ByteArrayKeyComparer.GetHashCode` produces signed Int32 from raw bytes — collisions OK, but distribution is poor for short or all-zero credential IDs
- **Severity:** High
- **Category:** correctness
- **File:** `src/WebAuthn/src/Extensions/PreviewSign/ByteArrayKeyComparer.cs:51-71`
- **Description:** The hash uses only the first 4 bytes (or fewer). Real WebAuthn credential IDs are typically 16–64 bytes of high-entropy randomness, so distribution is acceptable in practice, but: (a) using only 4 bytes is wasteful given the full bytes are available; (b) for short keys (≤3 bytes), the loop `hash = (hash << 8) | span[i]` is fine but the distribution is small; (c) `BinaryPrimitives.ReadInt32LittleEndian` on an unverified arbitrary span (could be malicious test input) is fine — no security issue. The bigger concern is consistency: a future change to credential IDs that happen to share a 4-byte prefix (e.g., a vendor prefix) would degrade the hash to O(n) per lookup. Since the only consumer is the previewSign validation loop (single-pass), perf impact is bounded.
- **Recommended fix:** Use `HashCode.AddBytes(ReadOnlySpan<byte>)` (.NET 8+ instance method via `var hc = new HashCode(); hc.AddBytes(span); return hc.ToHashCode();`) for full-content hashing. Drop the 4-byte shortcut.

### MEDIUM

#### M-1. Spec says `flags` MUST NOT be present during authentication ceremonies, but C# encoder doesn't enforce on input boundary
- **Severity:** Medium
- **Category:** spec-conformance / defense-in-depth
- **File:** `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAuthenticationInput.cs`
- **Description:** Spec CDDL (line 5085-5095) and prose (Authenticator extension input section) state `flags` is registration-only — MUST NOT be sent during authentication. The C# auth input has no `Flags` field, so the encoder cannot send it — defense-in-depth is fine. However, no test asserts that the encoded auth CBOR contains keys *only* in `{2, 6, 7}`. Adding such a test prevents future regressions from accidentally introducing a flags key.
- **Recommended fix:** Add a unit test `AuthenticationInput_ContainsOnlyAllowedKeys_2_6_7` that decodes the CBOR and asserts the inner-map key set ⊆ {2, 6, 7}.

#### M-2. Conflict-resolution policy in `BuildRegistrationCbor` diverges from Swift (silent promotion in Swift, throw in C#)
- **Severity:** Medium
- **Category:** spec-conformance / parity
- **File:** `src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs:73-92`
- **Description:** Swift `Backend+Extensions.swift:85` unconditionally derives `flags = userVerification == .required ? 0b101 : 0b001` — the user has no input over `flags` at all, the spec's UV preference rule is the single source of truth (spec line 4953-4954 says exactly this: "The CDDL value `0b101` if `pkOptions.authenticatorSelection.userVerification` is set to `required`, otherwise the CDDL value `0b001`"). The C# adapter introduces a user-controllable `flags` field on the input record and then throws `InvalidRequest` on conflict. **Per the spec**, the user is NOT supposed to be able to specify `Unattended` (0b000) at the WebAuthn-client layer at all — that's a CTAP-level CDDL value the client never emits per the spec processing rules. Allowing it as a public API surface and then validating against UV preference is non-conformant API design.
- **Evidence:** Spec line 4953-4954 (registration extension processing step 4):
  > `flags`: The CDDL value `0b101` if `pkOptions.authenticatorSelection.userVerification` is set to `required`, otherwise the CDDL value `0b001`.
- **Recommended fix:** Remove the `Flags` field from `PreviewSignRegistrationInput` (or mark it `internal`/obsolete with a doc note). Derive `flags` purely from `RegistrationOptions.UserVerification`. If a future use case demands explicit `Unattended` support, that's a CTAP-level extension a separate API surface should expose, not the WebAuthn-spec client extension.

#### M-3. `DecodeUnsignedRegistrationOutput` doesn't catch `WebAuthnClientError` — recursive throw will surface as plain exception, not malformed-CBOR `InvalidState`
- **Severity:** Medium
- **Category:** error handling
- **File:** `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignCbor.cs:311-317`
- **Description:** The catch block matches `CborContentException or InvalidOperationException`. But `WebAuthnAttestationObject.Decode` and `CoseKey.Decode` throw their own typed exceptions (likely not in this catch list). If `attestationObject` is malformed, the exception escapes uncaught, producing an uncategorized failure. The pipeline-level catch in `ExtensionPipeline.cs:405-409` only catches `CborContentException`, so the same problem propagates upward.
- **Recommended fix:** Either widen the catch to include `WebAuthnClientError` (rethrow as-is) and any other expected attestation-decode exception types, or catch them and re-wrap as `WebAuthnClientError(InvalidState, "previewSign nested attestation malformed", ex)`.

#### M-4. `PreviewSignSigningParams.AdditionalArgs` is not validated as CBOR
- **Severity:** Medium
- **Category:** spec-conformance / input validation
- **File:** `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignSigningParams.cs:64-86`
- **Description:** Spec §10.2.1 says `additionalArgs` "MUST contain a CBOR map encoding a COSE_Sign_Args object". The constructor accepts arbitrary `ReadOnlyMemory<byte>?` without verifying the bytes parse as CBOR. Garbage input will be wrapped as a CBOR bstr and sent to the authenticator, which will reject with `CTAP2_ERR_INVALID_CREDENTIAL` per spec §8 authenticator-side validation. Catching this client-side would be cheaper and more diagnosable.
- **Recommended fix:** In the constructor, when `additionalArgs.HasValue`, do a `new CborReader(additionalArgs.Value, Ctap2Canonical).PeekState()` and verify it parses (at least one map). Throw `InvalidRequest` on parse failure with message "previewSign additionalArgs must be valid CBOR-encoded COSE_Sign_Args".

#### M-5. Registration output `GeneratedSigningKey.AttestationObject` is the raw `WebAuthnAttestationObject`, not the re-encoded WebAuthn-shape CBOR bytes
- **Severity:** Medium
- **Category:** spec-conformance / parity
- **File:** `src/WebAuthn/src/Extensions/PreviewSign/GeneratedSigningKey.cs:54-59`
- **Description:** Spec §10.2.1 step 5 (registration) says the client output `attestationObject` is constructed by re-encoding the CTAP2 integer-keyed attestation map (1=fmt, 2=authData, 3=attStmt) into the WebAuthn string-keyed form ("fmt", "authData", "attStmt"). Swift `PreviewSign.swift:161-169, 175` does this re-encoding explicitly and returns `Data` (raw bytes ready for RP transmission). The C# field type `WebAuthnAttestationObject` is a parsed object, leaving the re-encoding burden on the caller. This is a minor spec divergence: the WebAuthn API contract is "provide the bytes ready to ship to the RP," not "provide a parsed view." Either ship raw bytes or guarantee deterministic re-serialization.
- **Recommended fix:** Add `ReadOnlyMemory<byte> AttestationObjectBytes` alongside the parsed `AttestationObject` (caller can trust the bytes match the spec's WebAuthn-form re-encoding). Reuse the original received bytes when possible; only re-encode if the source format is CTAP-form (integer keys).

### LOW / INFO

#### L-1. `DecodeSignedRegistrationOutput` reads keys but discards them — wasted work
- **Severity:** Low
- **Category:** dead code
- **File:** `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignCbor.cs:154-179`
- **Description:** The decoder loops through all map keys reading `algorithm`, `signature`, `flags` into local variables that are immediately discarded when the method returns null. If the goal is "always prefer unsigned form" (per the comment), this whole method is dead code today. See C-3 for the broader fix.

#### L-2. Const `KeyHandle = 2` and `Signature = 6` collide with `ToBeSigned = 6` and `KeyHandle` reuse — confusing constants
- **Severity:** Low
- **Category:** code clarity
- **File:** `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignCbor.cs:42-49`
- **Description:** Constants `Signature = 6` and `ToBeSigned = 6` share the same integer value (legitimate per spec — same key reused across input vs. output), and `KeyHandle = 2` vs. `AttestationObject = 7` vs. `AdditionalArgs = 7` similarly overlap. While correct per the spec's CDDL, this masks two distinct meanings under one name. Future maintainers will struggle.
- **Recommended fix:** Split into two nested static classes, e.g. `RegistrationKeys.{Algorithm, Flags, AttestationObject}` and `AuthenticationKeys.{KeyHandle, ToBeSigned, AdditionalArgs, Signature}` — make the dual usage explicit.

#### L-3. Test 7 (`PreviewSign_Authentication_RoutesCorrectSigningParams_ToBackend`) asserts the incorrect wire format
- **Severity:** Low (the underlying bug is C-1, but the test enshrines the bug)
- **Category:** test quality
- **File:** `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/PreviewSign/PreviewSignAdapterTests.cs:217-288`
- **Description:** The test asserts BOTH credentials' params are present in the encoded CBOR, justified by the comment "authenticator filters down". This contradicts the spec (§10.2.1 step 7-9) which requires the CLIENT to select and send only the chosen credential's params. After C-1 is fixed, this test must be rewritten to expect a single flat map for the chosen credential and verify the selection logic (probe `up=false`, pick available, send only that one).

#### L-4. Comment in `ExtensionPipeline.cs:145, 266` is wrong about CTAP2 canonical ordering
- **Severity:** Low
- **Category:** documentation
- **File:** `src/WebAuthn/src/Extensions/ExtensionPipeline.cs:145, 266`
- **Description:** Comment "Canonical sort: previewSign comes after prf but before others alphabetically" misstates CTAP2 canonical rules (length-first). See H-2 for the underlying bug; this comment misled the implementation.

## CBOR parity verdict

**PARTIAL PASS / FAIL.** Registration input encoding (`A2 03 82 26 28 04 01`) is byte-correct against the spec CDDL. Authentication output decoder is correct. Authentication INPUT encoding fails parity with both spec and Swift reference (C-1) — the C# wire format is structurally different and will not be accepted by any spec-conformant authenticator. Registration output decoder reads from the wrong CBOR location (C-2) — would also fail any real interop test.

## Phase 8 specific concerns

### 1. Flag conflict resolution
**FAIL (per spec).** The throw path (explicit `Unattended` + `UV=Required`) and the silent promotion path (default flags + `UV=Required` → promoted to RUV) are both reachable and tested. However, the entire conflict scenario only exists because C# exposes `Flags` as a public input field — which the spec says the WebAuthn client should NOT do. Per spec processing rule (line 4953-4954), `flags` is derived solely from `userVerification`. See M-2.

### 2. CBOR merging strategy
**FAIL.** The strategy of "parse standard map, re-emit with previewSign in sorted order" is conceptually sound, but the manual sort uses pure ordinal compare instead of CTAP2-canonical (length-then-lex) ordering. Combining previewSign with `largeBlobKey` or `minPinLength` (length 12) will produce out-of-order key writes and cause `Ctap2Canonical` mode to throw at `Encode()`. See H-2. Additionally, no test exercises a multi-extension merge that would catch the ordering bug.

### 3. Test 7 routing semantics
**FAIL.** The test enshrines the C-1 wire-format bug. Per spec and Swift parity, the SDK is responsible for selecting the chosen credential and sending only its params (not all credentials). After C-1 is fixed, this test needs a complete rewrite — likely as an integration test with a credential-selection probe step.

## Closing notes

Phase 7 (data layer: enums, records, CBOR encode/decode helpers) is structurally close to spec, with the major exceptions being:
1. The "unsigned" decoder reads from the wrong CBOR location (the embedded extension instead of the top-level `unsignedExtensionOutputs`).
2. The "signed" decoder is dead.
3. The auth input encoder produces a credential-keyed outer map that the spec does not define.

Phase 8 (wire-up via `PreviewSignAdapter` and `ExtensionPipeline`) inherits all Phase 7 issues and adds a CBOR canonical-ordering bug in the merge logic.

The pattern across these findings: the implementation appears to have been guided by a high-level README of the spec rather than the CDDL grammar plus the Swift reference's exact wire encoding. The Swift code in `WebAuthnPreviewSign.swift` + `PreviewSign.swift` + `Backend+Extensions.swift` together define exactly what bytes go on the wire and where they come from in the response — those three files, plus the spec's "Client extension processing" steps and CDDL section, are the byte-level source of truth.

**Recommendation:** Block ship. Convert C-1, C-2, C-3 into a single follow-up phase ("Phase 8.5 — wire-format alignment with CTAP v4 spec") that pins the spec's exact byte encoding, then rebuild Test 7 plus add an integration test that round-trips a fixture from the Swift reference's `MockWebAuthnBackend`. H-1 (error mapping wire-up) and H-2 (canonical sort) should ride along in the same fix-up.

## Resolutions

| ID | Status | Commit | Notes |
|----|--------|--------|-------|
| C-1 | ✓ Fixed | f1425044 | Auth wire format now flat single-credential map; multi-credential probe deferred to Phase 9 |
| C-2 | ✓ Fixed | 3364ed1d + 0ae08cf3 | unsignedExtensionOutputs plumbed Fido2 (prep) → WebAuthn (wire) |
| C-3 | ✓ Fixed | 6fd4acca | Removed dead DecodeSignedRegistrationOutput (always returned null) |
| H-1 | ✓ Fixed | 297ca139 | PreviewSignErrors.MapCtapError wired at MakeCredential backend boundary |
| H-2 | ✓ Fixed | 3bfb0b02 | CTAP2 canonical (length-then-lex) sort with Ctap2CanonicalKeyComparer |
| H-3 | ✓ Fixed | 04ef8beb | Defensive ByteArrayKeyComparer normalization in constructor |
| H-4 | ✓ Fixed | 8c1b9efe | Full-content HashCode.AddBytes() |
| M-1 | ✓ Fixed | 4a13e723 | Defense-in-depth auth CBOR key test (keys ⊆ {2,6,7}) |
| M-2 | ✓ Fixed | 5ca187bb | Flags derived from UV only (spec line 4962); removed user-controllable field |
| M-3 | ✓ Fixed | 950468c3 | Nested attestation errors wrapped as WebAuthnClientError |
| M-4 | ✓ Fixed | a2d1b626 | AdditionalArgs CBOR validation in constructor |
| M-5 | ⊘ Deferred | — | Re-encode AttestationObject as raw bytes — broader API decision; Phase 9 |
| L-1 | ✓ Fixed | (with C-3) | Dead-code removed |
| L-2 | ⊘ Deferred | — | CBOR key constants split into Reg/Auth nested classes — non-blocking refactor |
| L-3 | ✓ Fixed | (with C-1) | Test 7 rewritten to verify flat map encoding |
| L-4 | ✓ Fixed | (with H-2) | Comment fixed to reflect length-then-lex canonical ordering |

**Re-audit verdict:** 0 Critical, 0 High, 0 Medium blocking issues remaining. All spec-conformance bugs fixed.

**Multi-credential probe-selection (Phase 9):** Single-credential authentication is enforced by construction (BuildAuthenticationCbor throws NotSupported if signByCredential.Count > 1). The probe-selection step per spec §10.2.1 step 7 (CTAP up=false probe to determine available credential) is documented as a Phase 9 enhancement. The public API (PreviewSignAuthenticationInput.SignByCredential) accepts a dictionary but runtime validation restricts to single-credential until probe logic is implemented.

**Block-ship status:** CLEARED for previewSign correctness. Wire format aligns with spec CDDL and Swift reference byte-level encoding.
