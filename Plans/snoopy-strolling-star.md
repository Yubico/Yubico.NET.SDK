# Plan — previewSign 0x7F: legacy ↔ modern parser diff to pinpoint wrong-value bug

**Date:** 2026-04-29 (afternoon resume; supersedes the wire-chatter plan that was parked at Dennis's request)
**Branch:** `webauthn/phase-9.2-rust-port` at `43a466b4` (2 commits unpushed)
**PR:** [#466](https://github.com/Yubico/Yubico.NET.SDK/pull/466) → `yubikit-applets`
**Scope:** Find why GetAssertion still returns 0x7F after wire-shape converged byte-for-byte with python-fido2's working call.

---

## Context

Six hardware iterations earlier today proved the modern SDK's GetAssertion CBOR is **byte-identical** to python-fido2's working call (same map keys 2/6/7, same canonical order, same 81-byte slot-7 value, same flags, same omission of `up`). The Explore agent confirmed wire encoding parity at the source level. Yet the firmware still rejects with 0x7F.

Dennis's afternoon update made the path clear:
- **Legacy SDK** (`/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK-Legacy`) has a working `FullCeremony_RegisterDeriveSignVerify_RoundTrip` integration test plus the supporting parser/encoder code on the same JVM-style C# we're porting from. **Same language, same semantic model — much shorter diff than python ↔ modern.**
- NativeShims 1.16 ships `Native_EC_POINT_*` (free/get_affine/is_on_curve/mul/new/set_affine). **Modern SDK already uses these in `src/Core/src/Cryptography/ArkgPrimitivesOpenSsl.cs:572-633`** — so the new shims are not the missing piece. (Legacy doesn't use them at all.)

### What the diff so far has ruled out

| Hypothesis | Status |
|---|---|
| Wire shape `{2:kh, 6:tbs, 7:args}` differs | ❌ Ruled out — encoders byte-identical |
| Inner `COSE_Sign_Args {3:-65539, -1:kh, -2:ctx}` differs | ❌ Ruled out — encoders byte-identical |
| Legacy uses Native_EC_POINT_*, modern doesn't | ❌ Ruled out — modern already uses all 6 |
| ARKG algorithm divergence (modern vs python KAT) | ❌ Ruled out — morning cross-check matched algorithmically |

### What's left as the live suspect

**Modern populates `PreviewSignGeneratedKey.KeyHandle` (slot-2 wire value) and/or `BlindingPublicKey`/`KemPublicKey` with wrong values** extracted from the MakeCredential previewSign unsigned-extension-output bytes. Specifically:

- The previewSign att-obj is nested CBOR shaped `{1:fmt, 2:authData, 3:attStmt}` with **its own credentialId inside its authData**. That inner credentialId is the *device key handle* the firmware expects in slot 2 at sign time. **Not** the outer MakeCredential credentialId (which is a separate FIDO2 credential).
- If modern pulls outer credentialId into `KeyHandle`, the GA wire shape looks right but the firmware can't find a matching previewSign signing key → 0x7F.
- The morning's hypothesis (`pkBl`/`pkKem` extracted from wrong COSE seed key) is the same class of bug — wrong byte slice from the inner attestation object.

---

## Recommended Approach

A **focused 2-step parser-output diff with no hardware needed**, then a one-line fix.

### Step 1 — Write a one-shot diagnostic unit test

**File:** new test class in `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Extensions/PreviewSignParityTests.cs`

The test feeds the **captured MC response previewSign payload** (extracted from `/tmp/hid-modern-sdk.log` RECV idx 164) into both:

- Modern's `PreviewSignCbor.TryExtractGeneratedKey` (or whichever extracts the generated key from registration output)
- A small in-test helper that walks the inner att-obj manually and emits the *expected* `(KeyHandle, BlindingPublicKey, KemPublicKey)` tuple

The expected values come from the legacy SDK's parser run on the same bytes. Use the legacy `PreviewSignExtension.cs:DecodeGeneratedKey` (lines 184-282) as the oracle — copy its decode logic into the test (or reference-add the legacy assembly for one test). Assert each field byte-for-byte.

**Wire payload bytes:** decode the CBOR slice from `/tmp/hid-modern-sdk.log` RECV idx 164 (modern MC response, 1315 bytes). Field of interest: top-level CTAP key 6 (unsigned_extension_outputs) → "previewSign" entry.

**Why this works without hardware:** the bug lives in pure parsing logic against fixed bytes. The captured payload is the ground truth.

### Step 2 — Pinpoint and fix

The diff will reveal exactly one of:

| Differ | Fix |
|---|---|
| `KeyHandle` differs | Modern pulls from wrong credentialId source (likely outer instead of inner authData). One-field swap in the extractor. |
| `BlindingPublicKey` (`pkBl`) differs | Modern decodes the wrong COSE key field. Likely fix in `CoseArkgP256SeedKey.cs` (which slice maps to `BlPublicKey`). |
| `KemPublicKey` (`pkKem`) differs | Same class as above for the KEM half. |
| All three match | Bug is downstream — re-pivot to checking the GA assertion-output `auth_data.flags` byte and `up`/`uv` invariants. Lower probability given how exhaustively iteration-6 already converged. |

**Total LOC for the fix is expected to be 1-5 lines.** The test created in Step 1 stays as a permanent regression guard.

### Step 3 — Confirm on hardware

Re-run the FullCeremony integration test in worktree B (HID instrumentation still in place). Expect green. Then:
- Unskip FullCeremony tests in main checkout (Fido2 + WebAuthn)
- Revert HID instrumentation in worktree B (debug-only)
- Fold test correctness fixes back into main
- Push `425386bd` + `43a466b4` + this fix to PR #466

---

## Critical Files to Read/Touch

| File | Role |
|---|---|
| `src/Fido2/src/Extensions/PreviewSignExtension.cs:518-end` | Modern's `DecodeGeneratedKey`/inner att-obj decoder — extractor lives here |
| `src/Fido2/src/Cose/CoseArkgP256SeedKey.cs` | Modern's COSE seed-key parser — `BlPublicKey` and `KemPublicKey` extraction |
| `src/Fido2/src/Extensions/PreviewSignGeneratedKey.cs:71` | Modern ctor — receives `KeyHandle` from extractor |
| `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK-Legacy/Yubico.YubiKey/src/Yubico/YubiKey/Fido2/PreviewSignExtension.cs:184-282` | Legacy `DecodeGeneratedKey` — the oracle |
| `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK-Legacy/Yubico.YubiKey/src/Yubico/YubiKey/Fido2/PreviewSignGeneratedKey.cs:90-95` | Legacy ctor — receives `keyHandle`, `blindingPublicKey`, `kemPublicKey` |
| `/tmp/hid-modern-sdk.log` (RECV idx 164) | Captured MC response with previewSign payload — diagnostic input |
| **NEW:** `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Extensions/PreviewSignParityTests.cs` | Diagnostic + permanent regression guard |

## Out of scope (intentionally)

- **Wire-chatter cleanup** (the parked plan: GetInfo/ClientPin caching, 24→7 wire commands). Still a valid future PR; orthogonal to 0x7F.
- **Pushing existing commits to PR #466** — happens after the previewSign green, not as part of this fix.
- **Multi-credential probe selection** (CTAP v4 §10.2.1 step 7) — Phase 10.
- **NativeShims integration** — not needed; modern already uses all 6 EC_POINT_* shims.

---

## Verification

### Unit-level (no hardware)
```bash
dotnet toolchain.cs build
dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~PreviewSignParityTests"
```
Expect: new test fails *before* fix (showing exact byte delta), passes *after* fix. All 377 existing Fido2 tests stay green.

### Hardware-level (worktree B)
```bash
cd .claude/worktrees/agent-aa7ba443d8eec3e9e
# Apply same parser fix to worktree B's tree
# Temporarily unskip FullCeremony test (line 255)
dotnet toolchain.cs -- test --integration --project Fido2 \
  --filter "FullyQualifiedName~FullCeremony_RegisterDeriveSignVerify"
# Touch when prompted (~2 touches)
# Verify offline signature verifies against derived public key
```
Expect: GetAssertion returns 0x00 (success) instead of 0x7F. `derived.VerifySignature(message, signature)` returns true.

### Negative-path tests
The 3 existing negative integration tests (`UnsupportedAlgorithm`, `InvalidFlags`, `MissingArgs`) must remain green — the fix shouldn't change rejection paths.

---

## ISC (mutated for this thread)

The four pre-existing ISC tasks were written for the wire-chatter plan and no longer match. After plan approval they should be:

| # | Old (chatter) | New (previewSign) |
|---|---|---|
| 4 | GetInfo cached after first call per session | Diagnostic test exposes byte-delta in extracted KeyHandle/Bl/Kem |
| 2 | PinUvAuth shared-secret derived once per session | Modern extractor pulls correct field from inner att-obj |
| 3 | Wire-command count drops from 24 to ≤10 | FullCeremony_RegisterDeriveSignVerify returns assertions[0] (no 0x7F) |
| 1 | All 477 unit tests stay green | All 477 unit tests stay green + new parity test passes (was: 477) |
