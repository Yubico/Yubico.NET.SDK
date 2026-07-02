# Note to the v2 SDK team — previewSign test-shape drift pattern

**From:** Dennis (via Sia, after a v1-side previewSign integration session on 2026-04-28)
**Re:** branch `webauthn/phase-9.2-rust-port`, the inner-attestation-object fix in `0fbeb9c9`, and what it suggests about the broader test surface.

## TL;DR

Your fix `0fbeb9c9 fix(webauthn): decode CTAP-shaped inner attestation object in previewSign` is correct and well-engineered. But the root pattern it surfaced — test fixtures synthesizing wire bytes from the consumer layer's expected shape rather than from the device's actual emission shape — likely has more instances beyond the one you just fixed. Worth a focused audit before more hardware time gets spent re-discovering each one.

Also: thanks for `6ecbae3b` (the `-9` → `-65539` port from our Legacy `fe82b007`) and `3107bd5c` (CTAP key 6 not 8). v2 has both of v1's load-bearing previewSign discoveries already in. The bugs you're hitting now are different, and they're architectural.

## What we noticed

Your commit message on `0fbeb9c9` reads:

> "masked in the unit-test suite by `BuildAttestationObject` synthesizing a WebAuthn-shaped (text-keyed) attestation object — the test exercised a shape the device never emits."

That's the failure mode in one sentence. The unit tests synthesized the spec shape your adapter consumes (`{"fmt", "authData", "attStmt"}`), not the CTAP shape the YubiKey actually emits (`{1, 2, 3}`). Tests passed; hardware crashed at `WebAuthnAttestationObject.cs:79` with "major type 0 ... expected major type 3".

This is not a v2-specific issue per se — it's a generic test-fixture-quality pattern. v1 has it less severely because v1 has fewer transform points (single FIDO2 surface, customer talks CTAP-flavored types directly; no spec-shape adapter layer in between).

## Why v2 sees it more than v1

v2's two-layer architecture (WebAuthn → Adapters → Fido2) creates two transform points per wire artifact instead of one:

1. **Decode** at Fido2 layer (CTAP integer keys → typed C# struct)
2. **Re-shape** at WebAuthn adapter layer (typed C# struct → spec-shape WebAuthn object)

Each transform is a place where the assumed shape can drift from reality. The `previewSign` extension is especially exposed because `unsignedExtensionOutputs` carries a nested attestation object that's CTAP-shaped on the wire but the WebAuthn surface treats as spec-shaped. Easy to miss.

The good news: the fix you shipped (typed `InnerAttestationObject(string Fmt, ReadOnlyMemory<byte> AuthData, ReadOnlyMemory<byte> AttStmtRawCbor)` in Fido2 layer + spec-shape rebuild in WebAuthn adapter) is the right architectural discipline. Codifying it as a rule helps the rest.

## Other test-shape-drift suspects (audit candidates)

`grep -rn "Build.*Cbor\|BuildAttestation\|MockFido2" src/{Fido2,WebAuthn}/tests --include="*.cs"` surfaced these (non-exhaustive):

- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/TestSupport/MockFido2Responses.cs:146` — `BuildMakeCredentialResponseCbor(authData, format)` synthesizes the response wire bytes from caller intent
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/ExtensionPipelineTests.cs:41+` — `BuildRegistrationExtensionsCbor(...)` family

Each of these is a candidate for the same trap: the helper builds bytes that look like what the device ought to emit, but the canonical reference is the actual byte sequence captured from a real YubiKey 5.8.0-beta, not a hand-crafted approximation. If the device's output shape diverges from what the helper synthesizes, the unit test stays green and a hardware integration test catches it months later (or never, if the integration test is skipped).

## Suggested approach

Two complementary moves, ordered by ROI:

### 1. Capture device truth as test fixtures (highest leverage)

Plug in a YubiKey 5.8.0-beta and capture the actual CBOR bytes returned by the firmware for the previewSign code paths:

- A `MakeCredential` response with previewSign-generate-key extension (the full response, including `unsignedExtensionOutputs[6][previewSign]`)
- A `GetAssertion` response with previewSign-by-credential extension (the full response, including `authData.extensions["previewSign"][6]` for the signature)
- Both for FIPS and non-FIPS device variants if relevant

Drop these as `byte[]` constants in test fixtures. Replace any synthesized-from-caller-intent test helpers with these captured fixtures. Synthesizers can stay for cases where you genuinely need to test malformed/edge-case input — but for happy-path shape verification, the device's output IS the spec.

This also doubles as future-proofing: when YubiKey firmware changes the wire format (it has and will), re-running the capture against new firmware shows the diff immediately.

### 2. Codify the layering rule

Your `0fbeb9c9` commit body already states the correct discipline:

> "Per the layering rule, canonical CBOR decode lives in Fido2."
> "WebAuthn owns the wrap to the spec shape; no CBOR is decoded here."

Worth promoting that to a written rule in `CLAUDE.md` or contributor docs — explicitly: *"WebAuthn adapters never decode raw CTAP CBOR; they accept typed structs from Fido2. Fido2 never produces spec-shape WebAuthn objects; it produces typed CTAP-shape structs."*

If that rule is enforced in code review, the inner-attestation-object class of bug becomes mechanically catchable: any `cbor.Decode(...)` call inside `src/WebAuthn/` that takes raw bytes from a Fido2 response is suspicious by construction.

### 3. Continue the typed-wrapper push (already in motion)

Your `adcff793 feat(fido2,webauthn): typed CoseSignArgs builder` is the model. Quoting your own commit message: *"Makes the -9 vs -65539 algorithm bug class unrepresentable at the type level."* That's the durable defense.

Replicate the pattern for:

- The parsed `InnerAttestationObject` (you started this in `0fbeb9c9`; finish the surface)
- The `unsignedExtensionOutputs` map (currently a generic dictionary, easy to fat-finger keys)
- The COSE seed-key + derived-key distinction (your `CoseAlgorithm.cs:59-99` doc warnings are good but type-level enforcement would be better)

## What v1 learned that's relevant

(Documented in `Plans/codeaudit-previewsign-port.md`, `Plans/python-fido2-reference-findings.md`, and the memory entries listed at the end of this note.)

- For load-bearing constants and DSTs, **cite TWO independent reference impls** in the code comments. When N references agree and ours differs, the outlier is the bug. Saved us hours on the `-65539` discovery; would have saved months if it had been there from the start.
- A **changed error code after a fix means the wall moved, not regression**. The previewSign full-ceremony test went from generic "command failed" to specific "option/extension invalid" after the alg fix landed, which exposed the missing AllowCredential precondition. Each fix earns the right to see the next bug.
- `vslsp find_usages` is authoritative for who-calls-X questions in C# — except for runtime int→enum casts. Roslyn won't see `(MyEnum)reader.ReadInt32()` as a use of `MyEnum.SomeMember` even though it produces that value at runtime. Worth knowing when assessing dead-code candidates.

## Pointers if useful

In our v1 branch (`feature/webauthn-preview-sign`, commit `69b365bd`):

- `Yubico.YubiKey/src/Yubico/YubiKey/Fido2/PreviewSignExtension.cs:144-147, 249-282` — the on-the-wire layout your `0fbeb9c9` commit cited as ground truth for the inner attestation object
- `Yubico.YubiKey/src/Yubico/YubiKey/Fido2/GetAssertionParameters.cs:475-493` — the corrected `EncodeArkgSignArgs` (ours uses the constant directly; yours wraps it in a typed builder, which is better)
- `Plans/bugreport-v2-previewsign-ctap-key.md` (untracked but in branch) — original draft of the v2 bug report, with both the CTAP key 6 vs 8 finding AND the alg-constant finding appended

## What we're NOT suggesting

- **Don't refactor away your two-layer architecture.** The WebAuthn-over-Fido2 design is the right call for v2's broader goals; the impedance is the cost of a more powerful surface. The fix is discipline + types + better fixtures, not collapsing the layers.
- **Don't bulk-replace all synthesizers with captured fixtures overnight** — start with the previewSign surface (where you've already paid for the lessons), then expand opportunistically.

---

If anything here is wrong about your codebase or your direction, please push back — this is read-only archaeology against the head of `webauthn/phase-9.2-rust-port` at `0fbeb9c9` and may already be out of date by the time you read it.

— Dennis (via Sia), 2026-04-28
