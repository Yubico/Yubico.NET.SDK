# Handoff — previewSign 0x7F root cause FIXED; signature-verify mismatch is the next thread

**Date:** 2026-04-29 (afternoon session, second pass — supersedes the earlier `args[-1]-value-suspect` handoff)
**Branch (main repo):** `webauthn/phase-9.2-rust-port` at tip `43a466b4` (uncommitted edits in working tree)
**Branch (worktree B):** `worktree-agent-aa7ba443d8eec3e9e` at tip `6988dc1d` — **locked, uncommitted edits**
**HEAD ↔ origin:** main repo is **2 commits ahead** of origin — `425386bd` + `43a466b4` not pushed
**PR:** [Yubico/Yubico.NET.SDK#466](https://github.com/Yubico/Yubico.NET.SDK/pull/466) — OPEN, target `yubikit-applets`
**Strategy frame:** [`Plans/yes-we-have-started-composed-horizon.md`](yes-we-have-started-composed-horizon.md)
**Current focused plan:** [`Plans/snoopy-strolling-star.md`](snoopy-strolling-star.md) — *previewSign root-cause fix* (was: wire-chatter cleanup, parked)

---

## Headline finding

🎯 **Root cause #1 found and fixed.** `CoseArkgP256SeedKey.Decode` had `pkBl` and `pkKem` **swapped** at CBOR keys `-1` and `-2`. Spec contract per draft-bradleylundberg-cfrg-arkg-10, python-fido2 (`cose.py:428-433`), and the legacy SDK (`PreviewSignExtension.cs:317-323`):

| CBOR key | What lives there |
|---|---|
| **-1** | `pkBl` (blinding public key) |
| **-2** | `pkKem` (KEM public key) |

Modern had it inverted (`-1=KEM, -2=BL`). The morning's "byte-identical wire" check missed it because the wire IS identical — only the *interpretation* of the two nested COSE keys was flipped, so modern computed the offline ARKG handle against the wrong KEM, and the firmware couldn't decapsulate → CTAP2_ERR_OTHER (0x7F).

**Hardware result after the fix:** firmware accepts the GetAssertion request and returns a signature. **0x7F is gone.** ✅

**New issue surfaced (much smaller scope):** the offline `derivedKey.VerifySignature(message, signature)` returns `false` at line 417. Likely cause is one of:
1. **Test code semantics** — modern test passes the *already-hashed* `message` (line 337: `message = SHA256.HashData(messageRaw)`) to `VerifySignature`, which then hashes again internally via `ECDsa.VerifyData(..., SHA256)` → double-hash. Legacy test passes the **raw** message to its `VerifySignature`. Single-line test fix candidate: pass `messageRaw` instead of `message` to `derivedKey.VerifySignature`.
2. **ARKG derived-public-key calculation** divergence somewhere in `ArkgPrimitivesOpenSsl.BlBlindPublicKey` despite the swap fix. Less likely given firmware accepted the keyHandle (KEM half is correct end-to-end).
3. Signature DER ↔ IEEE conversion edge case in `PreviewSignDerivedKey.ConvertDerToIeee`.

---

## Critical next steps (read first)

1. **Confirm the test-vs-SDK hashing semantic:** read `src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoPreviewSignTests.cs:336-337` and compare with `src/Fido2/src/Extensions/PreviewSignDerivedKey.cs:108-141` (which uses `VerifyData`, not `VerifyHash`). Likely test just needs `messageRaw` passed instead of `message`.
2. **If that fix doesn't resolve it:** spot-check ARKG primitive output. Build a unit test that runs modern `ArkgP256.DerivePublicKey` against the **same fixed inputs** as python-fido2's `derive_public_key` (same ikm, same ctx, same pkBl/pkKem) and assert byte-equal output. If they differ, the ARKG primitive itself has a second bug.
3. **Push** `425386bd` + `43a466b4` + the new fix commit(s) to PR #466 once FullCeremony is green.

---

## What was done this session (afternoon, second pass)

### Diagnostic chain
1. Resumed from morning handoff (`args[-1] value suspect`)
2. Dennis flagged: legacy SDK has working FullCeremony tests + NativeShims 1.16 ships new EC_POINT_* exports (`/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK-Legacy/...`)
3. Ruled out NativeShims gap — modern `ArkgPrimitivesOpenSsl.cs:572-633` already P/Invokes all 6 EC_POINT_* shims
4. Read both legacy and modern `CoseArkgP256SeedKey` / `PreviewSignExtension` parsers side-by-side
5. Cross-checked python-fido2 `cose.py:428-433`
6. Confirmed: legacy + python both say `-1 = pkBl, -2 = pkKem`. Modern says `-1 = KEM, -2 = BL`. Inverted.

### TDD fix
1. Added new spec-contract regression test `Decode_pkBlAtMinus1_pkKemAtMinus2_PerSpec` to `CoseArkgP256SeedKeyTests.cs` — uses distinguishable byte patterns (BL=0xAA, KEM=0xBB) at CBOR keys `-1`/`-2` and asserts decoder routes them to `BlPublicKey`/`KemPublicKey`
2. Confirmed RED: `Expected: 170 (0xAA), Actual: 187 (0xBB)` — bug empirically proved
3. Applied fix in `src/Fido2/src/Cose/CoseArkgP256SeedKey.cs`:
   - `Decode` (lines 77-83): swap which CBOR key feeds `blKeyCbor` vs `kemKeyCbor`
   - `Encode` (lines 158-162): swap which key writes BL vs KEM
   - Docstring (lines 31-35): correct the wire-format description
4. Updated existing test `Decode_WithValidArkgSeedKey_ReturnsCorrectVariant` which had the inverted contract baked into the producer — flipped `kemNested`/`blNested` to match the spec-correct wire layout
5. Result: 4/4 `CoseArkgP256SeedKeyTests` green, full Fido2 + WebAuthn unit suites green

### Hardware verification
1. Synced fix into worktree B (`cp` of corrected `CoseArkgP256SeedKey.cs`)
2. Lifted the `Skip.If(true, "previewSign GA returns 0x7F...")` guard at `FidoPreviewSignTests.cs:259`
3. Built worktree B clean (0 errors)
4. Ran FullCeremony with hardware touches:
   - Step A (MakeCredential + previewSign): ✅ succeeded
   - Step B (offline derive): ✅ produced derivedKey with 65-byte SEC1 publicKey + non-empty arkgKeyHandle
   - Step C (GetAssertion + previewSign signing): ✅ **firmware accepted; signature returned (no 0x7F!)**
   - Step D (offline VerifySignature): ❌ returned `false` at line 417

### Did not achieve
- A green FullCeremony test end-to-end. **One step remains** (verify-signature mismatch). Root cause likely test-code hashing semantic; investigation deferred to the next agent or next session.

---

## Current State

### Uncommitted changes — main checkout
- `src/Fido2/src/Cose/CoseArkgP256SeedKey.cs` — **the fix** (swap -1/-2)
- `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Cose/CoseArkgP256SeedKeyTests.cs` — new spec-contract test + corrected existing test
- `Plans/handoff.md` — this file
- `Plans/snoopy-strolling-star.md` — focused fix plan (parked wire-chatter plan was overwritten)

### Uncommitted changes — worktree B
- `src/Fido2/src/Cose/CoseArkgP256SeedKey.cs` — **the fix** (synced from main)
- `src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoPreviewSignTests.cs` — Skip lifted at line 259

### Build & test status

| Check | Status | Where |
|---|---|---|
| `dotnet toolchain.cs build` | **0 errors** | Main + worktree B |
| Fido2 unit tests | **377/377 PASS** | Main checkout |
| WebAuthn unit tests | **100/100 PASS** | Main checkout |
| New `Decode_pkBlAtMinus1_pkKemAtMinus2_PerSpec` | **PASS** (after fix) | Main checkout |
| `FullCeremony_RegisterDeriveSignVerify_RoundTrip` | ⚠️ **Reaches Step D, fails on `VerifySignature`** | Worktree B (was: 0x7F at Step C) |
| Negative-path tests (3) | ✅ All pass | (unchanged from morning) |

---

## Readiness Assessment

| Need | Status | Notes |
|---|---|---|
| WebAuthn data model + ClientData/AttestationObject/AuthenticatorData | ✅ Working | Phases 1-2 |
| `WebAuthnClient.MakeCredentialAsync` + `GetAssertionAsync` (standard FIDO2) | ✅ Working | Hardware-verified |
| Extension framework | ✅ Working | All inputs/outputs in Fido2 |
| `previewSign` registration + seed key extraction | ✅ Working | Hardware-verified |
| ARKG-P256 cryptographic primitives | ✅ Working | 3 KAT + cross-verified |
| **CoseArkgP256SeedKey CBOR mapping (-1=Bl, -2=Kem)** | ✅ **FIXED THIS SESSION** | Spec parity test added |
| `previewSign` GetAssertion accepted by firmware | ✅ **FIXED THIS SESSION** | No more 0x7F |
| **Offline VerifySignature on derived-key signature** | ⚠️ **OPEN — likely test hashing semantic** | See "Critical next steps" |
| Negative-path tests | ✅ Working | 3/3 PASS |

**Overall:** 🟡 **Beta** — One step from end-to-end green. Single-line test fix is the most likely remaining work.

---

## Quick Start for New Agent

```bash
# 1. Confirm state
cd /Users/Dennis.Dyall/Code/y/Yubico.NET.SDK
git status --short  # expect: 4 modified/new files (handoff, plan, fix, tests)
git log -1 --oneline  # expect: 43a466b4 fix(webauthn): wire PreviewSignErrors.MapCtapError...

# 2. Read the fix and the test
cat src/Fido2/src/Cose/CoseArkgP256SeedKey.cs | sed -n '74,90p'
cat src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Cose/CoseArkgP256SeedKeyTests.cs | sed -n '113,170p'

# 3. Confirm green
dotnet toolchain.cs -- test --project Fido2     # 377/377
dotnet toolchain.cs -- test --project WebAuthn  # 100/100

# 4. Investigate the verify-signature mismatch
#    Most likely fix is in worktree B's test code — pass `messageRaw` instead of `message`
#    to derivedKey.VerifySignature at FidoPreviewSignTests.cs:416
#    (modern's VerifySignature uses ECDsa.VerifyData, which hashes internally)

# 5. Hardware verify (worktree B)
cd .claude/worktrees/agent-aa7ba443d8eec3e9e
# Apply the test fix here
dotnet toolchain.cs -- test --integration --project Fido2 \
  --filter "FullyQualifiedName~FullCeremony_RegisterDeriveSignVerify"
# Touch when prompted (~2 touches)
```

---

## Files Touched This Session

| File | Change |
|---|---|
| `src/Fido2/src/Cose/CoseArkgP256SeedKey.cs` | **The fix** — swap CBOR -1/-2 in Decode + Encode + docstring |
| `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Cose/CoseArkgP256SeedKeyTests.cs` | New spec-contract test + corrected existing test contract |
| `.claude/worktrees/.../src/Fido2/src/Cose/CoseArkgP256SeedKey.cs` | Synced fix into worktree B |
| `.claude/worktrees/.../src/Fido2/tests/.../FidoPreviewSignTests.cs:259` | Skip lifted |
| `Plans/snoopy-strolling-star.md` | New focused plan (wire-chatter plan parked, overwritten) |
| `Plans/handoff.md` | This file |

## Lessons captured this session

1. **"Byte-identical wire" can mask interpretation bugs.** The morning hard-converged the wire format to match python's, but never proved that python's *parser* extracted the same labeled values from those bytes. The CBOR was right; the names attached to the two nested keys were swapped. Future cross-checks should compare *labeled* outputs, not just raw bytes.
2. **Existing test that bakes in the bug as a contract is invisible to CI.** The original `Decode_WithValidArkgSeedKey_ReturnsCorrectVariant` wrote `kemNested` at -1 and asserted `KemPublicKey == kemNested`. Both producer and consumer were wrong consistently; CI green proved nothing. **Add spec-contract tests** (with distinguishable byte patterns and a third-party oracle) when porting from a reference implementation, not round-trip tests.
3. **TDD with a red-failing test is fast for diagnostic confirmation.** A 30-line spec-contract test took ~10 minutes to write and produced an unambiguous Expected/Actual diff that pinpointed the swap. Far better than reading code in isolation.
4. **One bug at a time.** Fixing the seed-key swap unblocked the firmware reject; the verify-signature mismatch became visible only because the firmware now responds. If we'd tried to "fix everything in one go," we'd have conflated the two issues.
5. **Worktree B remains a useful staging area.** Synced fixes via `cp`, ran hardware tests there, kept main checkout clean of the HID instrumentation. Pattern works.
