# Phase 9.8 — Attestation Typed-Variant Consolidation (deferred from Phase 9.7)

**Status:** Deferred follow-up tracker
**Filed:** 2026-04-24
**Predecessor:** `Plans/phase-9.7-soc-consolidation.md` (Group C)
**Reason for deferral:** Architectural conflict with Fido2 public API freeze

## Problem

`src/Fido2/src/Credentials/MakeCredentialResponse.cs:266` defines `public sealed class AttestationStatement` (flat/untyped — has `Format`, `Statement`, `RawCbor` properties). The class is exposed via `MakeCredentialResponse.AttestationStatement { get; }` at line 60 — part of Fido2's PUBLIC API.

`src/WebAuthn/src/Attestation/AttestationStatement.cs` defines an abstract base + typed variants (`PackedAttestationStatement`, `FidoU2FAttestationStatement`, `AppleAttestationStatement`, `TpmAttestationStatement`, `NoneAttestationStatement`) — same name, different shape.

Phase 9.7 attempted to promote the typed variants into `Yubico.YubiKit.Fido2.Credentials` namespace — collision: `CS0101: namespace already contains a definition for 'AttestationStatement'`.

Phase 9.7's hard constraint: **Fido2 public API surface is FROZEN.** Renaming the existing `AttestationStatement` class or changing the property type on `MakeCredentialResponse.AttestationStatement` would break Fido2's public API.

## Architectural options for Phase 9.8

### Option A — Replace existing flat class with typed hierarchy (BREAKING)
Remove `public sealed class AttestationStatement` (Fido2). Promote typed variants from WebAuthn into Fido2 with the same `AttestationStatement` name as the abstract base. Change `MakeCredentialResponse.AttestationStatement` property type from the flat class to the abstract record (or expose a new property with a different name and deprecate the old one).

- ✅ Clean architectural outcome
- ❌ Breaking change to Fido2 public API → requires explicit Fido2 maintainer sign-off
- ❌ Existing consumers of `MakeCredentialResponse.AttestationStatement.Statement` (raw CBOR) need migration to `attestationStatement switch { Packed => ..., FidoU2F => ..., ... }`

### Option B — Coexist (NOT consolidation)
Promote typed variants to Fido2 under a different name (e.g., `TypedAttestationStatement` abstract base; `*AttestationStatementVariant` derived). Keep existing `AttestationStatement` flat class. Add `MakeCredentialResponse.TypedAttestationStatement` property as new public API.

- ✅ Non-breaking; pure addition
- ❌ Two parallel models — violates the no-duplication rule that motivated Phase 9.7
- ⚠️ Effectively the same problem moved one level deeper

### Option C — Adapter pattern; keep flat in Fido2, typed in WebAuthn
Keep WebAuthn's typed `AttestationStatement` hierarchy. WebAuthn's `WebAuthnAttestationObject.Decode` consumes Fido2's flat `AttestationStatement`, switches on `Format`, and constructs the appropriate typed variant. Add a Fido2 helper for the envelope writer/decoder (C3, C4 from Phase 9.7) that does NOT touch the typed model.

- ✅ Non-breaking
- ❌ Decode logic for `packed`/`fido-u2f`/`apple`/`tpm`/`none` still lives in WebAuthn (the original violation #4)
- ⚠️ Partial consolidation only

### Option D — Defer indefinitely
Accept that the attestation-statement layer is one place where the typed-vs-flat tradeoff justifies the duplication, given the public-API freeze.

## Recommendation

**Option A is the right answer.** Fido2's flat `AttestationStatement.Statement` (raw CBOR) leaks the wire format to consumers; replacing it with typed variants is a real API improvement, not just consolidation. But it needs an explicit "we are breaking this" decision from Yubico maintainers and probably its own dedicated PR.

For the next iteration: file an issue against the Fido2 module asking for permission to deprecate-and-replace `MakeCredentialResponse.AttestationStatement`, and execute Option A in a follow-up branch off `yubikit-applets` once approved.

## Items still to address from Phase 9.7 Group C

- **C1** — Update Fido2's internal attestation decoder to return typed variants
- **C2** — Promote `AttestationFormat` enum + typed `*AttestationStatement` variants to Fido2 public API
- **C3** — Add Fido2 helper for writing the attestation envelope (text-keyed vs int-keyed)
- **C4** — Add Fido2 helper for decoding the envelope

Items C3 and C4 are independent of the type-naming conflict — they could land separately as pure helper additions without touching `MakeCredentialResponse.AttestationStatement`. Optional micro-progress before the bigger Option A negotiation.

## Reference

- Architect SoC re-audit: Sia conversation 2026-04-23
- Phase 9.7 PRD: `Plans/phase-9.7-soc-consolidation.md`
- Architectural rule: `~/.claude/projects/-Users-Dennis-Dyall-Code-y-Yubico-NET-SDK/memory/feedback_no_duplication_rule.md`
