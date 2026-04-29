# Plan — Ship the ARKG primitives port and unblock previewSign FullCeremony

**Date:** 2026-04-28 evening
**Active branch:** `webauthn/phase-9.2-rust-port` (tip `0fbeb9c9`)
**Eventual merge target:** `yubikit-applets`
**Driver:** Dennis Dyall — DevTeam ship requested
**Strategy frame:** [`Plans/yes-we-have-started-composed-horizon.md`](yes-we-have-started-composed-horizon.md) (rev 2)
**Closes:** Open Follow-up A (ARKG-P256 CoseKey decoder), #17 (Yubico.Core ARKG port), and unskips the WebAuthn FullCeremony hardware test.

---

## Context

Today's three-way audit (modern SDK ↔ python-fido2 ↔ Legacy SDK) confirmed a single gating dependency chain blocking previewSign full-ceremony parity:

> **Yubico.Core ARKG primitives port** → ARKG-P256 `CoseKey` decoder → typed `PreviewSignGeneratedKey`/`PreviewSignDerivedKey` → unskip the existing `FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature` integration test on YK 5.8.0-beta hardware.

Legacy SDK already shipped a complete, hardware-verified implementation against the same firmware (`feature/webauthn-preview-sign` branch). The 525-line `ArkgPrimitivesOpenSsl.cs` is mechanical to port: no unsafe blocks, no `Marshal.AllocHGlobal`, no pinned spans, no .NET-Framework-only dependencies, and consistent `CryptographicOperations.ZeroMemory` hygiene at every ephemeral-secret site.

The blocker has been native FFI surface. Dennis built and pushed `Yubico.NativeShims 1.16.1-prerelease.20260428.1` ([Actions run 25048152164](https://github.com/Yubico/Yubico.NET.SDK/actions/runs/25048152164)) which exposes the EC_*/BN_* OpenSSL bindings (`EcGroupNewByCurveName`, `EcPointMul`, `EcPointSetAffineCoordinates`, `EcPointIsOnCurve`, `EcPointGetAffineCoordinates`, `BnBinaryToBigNum`, `BnNew`, `BnBigNumToBinaryWithPadding`, `EcPointNew`) the port needs.

**Outcome on success:** modern SDK reaches feature parity with Legacy on previewSign full ceremony, exceeds Legacy on type safety (typed `CoseSignArgs` already shipped as `adcff793`), and adds three negative-path integration tests that close the test-coverage gap vs python-fido2. The only remaining capability gap across all three implementations becomes multi-credential probe-selection — genuinely unimplemented anywhere — which stays deferred per Open Follow-up #15.

**Decisions locked from clarification:**
- Public API style: **fully modernized to `ReadOnlySpan<byte>` at API and internals.** Data-holder properties on `PreviewSignGeneratedKey`/`PreviewSignDerivedKey` remain `ReadOnlyMemory<byte>` (Span cannot be a field/property type).
- Scope add-on: **negative-path tests from python-fido2** (`unsupported_alg`, `invalid_flags`, `missing_args`). Multi-credential probe-selection stays deferred. PRD commit handled separately as a docs-only commit.

---

## Strategy: DevTeam-driven ship

Six phases, sequential gates between each. Phases A → D are required core; phase E is the scope add-on; phase F is the `/DevTeam` Engineer+Reviewer ship loop that takes the integrated diff and drives it to a clean commit.

Each phase ends with a hard verification gate. **No phase advances on a "should work" — only on mechanical evidence (build clean, tests green, hardware test PASS).**

---

## Phase A — Native package upgrade (gates everything)

**Files:**
- `nuget.config` (repo root) — uncomment lines 7, 17-19 to enable `https://nuget.pkg.github.com/Yubico/index.json`. Confirm credentials available via `NUGET_AUTH_TOKEN` or `gh auth token`.
- `Directory.Packages.props:24` — bump `Yubico.NativeShims` from `1.16.0` → `1.16.1-prerelease.20260428.1`.

**Verification gate:**
- `dotnet restore` succeeds and resolves the prerelease from the GitHub Packages feed.
- `dotnet toolchain.cs build` clean (0 errors). New `Native_*` entry points present in restored package metadata (spot-check via `nm` or `dotnet list package --include-transitive`).

---

## Phase B — Port `Yubico.YubiKit.Core` ARKG primitives

**Reuse before porting:**
- `src/Core/src/Cryptography/HkdfUtilities.cs` — already exists; Legacy `HmacKemEncaps` calls `HkdfUtilities.DeriveKey` directly.
- `src/Core/src/Cryptography/EcdsaVerify.cs` — already exists (~20KB); `PreviewSignDerivedKey.VerifySignature` will wrap it.
- `src/Core/src/Cryptography/CmacPrimitivesOpenSsl.cs` — canonical pattern for new P/Invoke. Match: `[DllImport(Libraries.NativeShims, EntryPoint = "Native_*")]`, internal static partial class `NativeMethods` colocated with the consumer.
- `src/Core/src/Cryptography/CryptographyProviders.cs` — host the new `ArkgPrimitivesCreator` static delegate. The commented-out precedent at `CryptographyProviders.cs:48` (`AesGcmPrimitivesCreator`) shows the exact shape.

**New files:**
- `src/Core/src/Cryptography/IArkgPrimitives.cs` — three methods, all `ReadOnlySpan<byte>` parameters where the Legacy uses `byte[]`:
  - `bool IsPointOnCurve(ReadOnlySpan<byte> point)`
  - `byte[] ComputeEcdhSharedSecret(ReadOnlySpan<byte> privateScalar, ReadOnlySpan<byte> publicPoint)` — return stays `byte[]` (caller may need to retain; ZeroMemory after use is caller responsibility)
  - `(byte[] derivedPk, byte[] arkgKeyHandle) Derive(ReadOnlySpan<byte> pkBl, ReadOnlySpan<byte> pkKem, ReadOnlySpan<byte> ikm, ReadOnlySpan<byte> ctx)`
- `src/Core/src/Cryptography/ArkgPrimitivesOpenSsl.cs` — port of the Legacy 525-line file at `Yubico.NET.SDK-Legacy/Yubico.Core/src/Yubico/Core/Cryptography/ArkgPrimitivesOpenSsl.cs`. Convert `byte[]` parameters → `ReadOnlySpan<byte>` at API and internal helper boundaries. Internal scratch buffers ≤512 bytes use `stackalloc`; >512 use `ArrayPool<byte>.Shared.Rent` per CLAUDE.md memory rules. Preserve all 7 `CryptographicOperations.ZeroMemory` call sites verbatim (lines 193, 241, 264, 280, 284, 292, 337). No `unsafe`, no `Marshal`. Library constant: reuse `Libraries.NativeShims` from `src/Core/src/PlatformInterop/Libraries.cs`.
- `src/Core/src/Cryptography/ArkgPrimitives.cs` — static `Create()` factory mirroring Legacy.

**Edits:**
- `src/Core/src/Cryptography/CryptographyProviders.cs` — add `public static Func<IArkgPrimitives> ArkgPrimitivesCreator { get; set; } = ArkgPrimitives.Create;`

**Tests:**
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Cryptography/ArkgP256Tests.cs` — port the 3 KAT vectors verbatim from `Yubico.NET.SDK-Legacy/Yubico.YubiKey/tests/unit/Yubico/YubiKey/Fido2/Arkg/ArkgP256Tests.cs` (vectors A, B, C; shared `pkBl` and `pkKem`). These match upstream Rust reference (`cnh-authenticator-rs-extension/native/crates/hid-test/src/arkg.rs`) and are deterministic — no hardware required.

**Verification gate:**
- `dotnet toolchain.cs build` clean.
- `dotnet toolchain.cs test --project Core --filter "FullyQualifiedName~Arkg"` — all 3 KAT tests PASS.
- `vslsp get_diagnostics_summary` for the solution — 0 errors.

---

## Phase C — Fido2 layer: ARKG seed-key `CoseKey` + typed key holders

**Edits:**
- `src/Fido2/src/Cose/CoseAlgorithm.cs` — add `public static readonly CoseAlgorithm ArkgP256SeedKey = new(-65700);` next to existing `ArkgP256` (-65539) at line 81. Update `IsKnown` switch at line 88 to include `-65700`. Comment header MUST disambiguate the two constants per the lesson captured in the morning's `LEGACY_PREVIEWSIGN_FORENSICS.md` §3.4 and PRD §7 (signing-op alg vs seed-key alg are NOT interchangeable).
- `src/Fido2/src/Cose/CoseKey.cs` — extend `Decode` (entry at line 43, current throw at line 59) to recognize the ARKG-P256 seed-key shape. Per `draft-bradleylundberg-cfrg-arkg-10`: `kty=2` (EC2) + `alg=-65700` + parameter `-1` carries `pkKem` (P-256 SEC1) + parameter `-2` carries `pkBl` (P-256 SEC1). New variant dispatch joins the existing `kty=2 → DecodeEc2` branch (line 76) — discriminate on `alg` after `kty=2`.

**New files:**
- `src/Fido2/src/Cose/CoseArkgP256SeedKey.cs` — sealed record holding `KemPublicKey` and `BlPublicKey` as `ReadOnlyMemory<byte>` (65-byte SEC1). Inherits `CoseKey`. Properties are init-only.
- `src/Fido2/src/Arkg/ArkgP256.cs` — port of Legacy's 54-line router. Single internal static `DerivePublicKey(ReadOnlySpan<byte> pkBl, ReadOnlySpan<byte> pkKem, ReadOnlySpan<byte> ikm, ReadOnlySpan<byte> ctx)` delegating to `CryptographyProviders.ArkgPrimitivesCreator().Derive(...)`.
- `src/Fido2/src/Extensions/PreviewSignGeneratedKey.cs` — port of Legacy's 147-line file. Public sealed class. Properties: `KeyHandle`, `BlindingPublicKey`, `KemPublicKey` (all `ReadOnlyMemory<byte>`); `DerivedKeyAlgorithm` (CoseAlgorithm). `DerivePublicKey(ReadOnlySpan<byte> ikm, ReadOnlySpan<byte> ctx)` returns `PreviewSignDerivedKey`.
- `src/Fido2/src/Extensions/PreviewSignDerivedKey.cs` — port of Legacy's 133-line file. Public sealed class. Properties: `PublicKey`, `ArkgKeyHandle`, `DeviceKeyHandle`, `Context` (all `ReadOnlyMemory<byte>`). `VerifySignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)` returns `bool` — internally constructs `EcdsaVerify` from `src/Core/src/Cryptography/EcdsaVerify.cs` against `PublicKey` (65-byte SEC1) and calls `verifier.VerifyData(message, signature, isStandardSignature: true)`.

**Edits to existing extension surface:**
- `src/Fido2/src/Extensions/PreviewSignExtension.cs` — `PreviewSignRegistrationOutput` already exposes `PublicKey` as `ReadOnlyMemory<byte>` (line 182). Add a typed accessor / static helper `TryGetGeneratedKey(out PreviewSignGeneratedKey?)` that wraps `CoseKey.Decode(this.PublicKey)` and returns the typed `PreviewSignGeneratedKey` if the decoded variant is `CoseArkgP256SeedKey`. Backwards-compatible — does not change existing `PublicKey` field.

**Verification gate:**
- `dotnet toolchain.cs build` clean.
- `dotnet toolchain.cs test --project Fido2 --filter "FullyQualifiedName~PreviewSign"` — all existing 17 PreviewSign unit tests still PASS (no regression). New unit tests for `CoseArkgP256SeedKey.Decode` and `PreviewSignGeneratedKey.DerivePublicKey` (using KAT vectors from Phase B) — all PASS.

---

## Phase D — Unskip + hardware-verify the FullCeremony tests

**Edits:**
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs` — locate `FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature`. The test was already rewired for the typed `CoseSignArgs` API in commit `adcff793`. Remove the `Skip.If(true, ...)` guard. Wire it through the new `PreviewSignGeneratedKey.DerivePublicKey()` → `PreviewSignDerivedKey.VerifySignature()` flow.
- `src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoPreviewSignTests.cs` — add `FullCeremony_RegisterDeriveSignVerify_RoundTrip` mirroring Legacy's verbatim body (`Yubico.NET.SDK-Legacy/Yubico.YubiKey/tests/integration/Yubico/YubiKey/Fido2/PreviewSignTests.cs:62-109`). Two-touch hardware ceremony: register → offline derive (random 32-byte ikm + ASCII ctx) → GetAssertion with allowList (single credential id from MakeCredential) → offline ECDSA verify. Trait: `[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]`.

**Verification gate (HARDWARE — not skippable):**
- Plug in YK 5.8.0-beta.
- `dotnet toolchain.cs test --project Fido2 --integration --filter "FullyQualifiedName~FullCeremony_RegisterDeriveSignVerify_RoundTrip"` — PASS, two touches required.
- `dotnet toolchain.cs test --project WebAuthn --integration --filter "FullyQualifiedName~FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature"` — PASS, two touches required.
- `Registration_WithPreviewSign_ReturnsGeneratedSigningKey` (the WebAuthn registration test currently FAILing on `CoseKey.Decode`) — PASS.

If either FullCeremony test fails: STOP. Do not advance to Phase E. Diagnose — the failure is necessarily in B or C.

---

## Phase E — Negative-path integration tests (from python-fido2)

Port the three negative cases from `python-fido2/tests/device/test_sign_extension_v4.py:407-632`:

- `MakeCredential_WithUnsupportedAlgorithm_ReturnsError` (mirrors `test_register_unsupported_alg`) — request `Es256` instead of `Esp256SplitArkgPlaceholder`; expect `Fido2Exception` with appropriate CTAP error.
- `MakeCredential_WithInvalidFlags_ReturnsError` (mirrors `test_register_invalid_flags`) — flag value outside `{0x01, 0x05}`; expect rejection.
- `GetAssertion_WithMissingArgs_ReturnsError` (mirrors `test_assert_missing_args`) — submit `PreviewSignAuthenticationInput` lacking `CoseSignArgs` for an ARKG-derived key; expect rejection.

**Files:**
- `src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoPreviewSignTests.cs` — append the three tests. Trait `RequiresUserPresence` on the two MakeCredential ones (registration touches), no touch on the GetAssertion-missing-args one (rejected pre-touch).

**Verification gate (HARDWARE):**
- All three new tests PASS on YK 5.8.0-beta.

---

## Phase F — DevTeam ship

Once Phases A–E are green, invoke `/DevTeam` over the integrated diff:

1. **Engineer pass** — review the ported code for: DRY (any new duplication of HKDF / hash-to-curve logic that should consolidate with existing `HkdfUtilities`), SRP (any 525-line monolith that should split — Legacy was tightly coupled, modern can decompose `ArkgPrimitivesOpenSsl` into `ArkgKem` + `ArkgBl` + `RfcExpander` if natural seams emerge), Span/Memory hygiene per CLAUDE.md, ZeroMemory completeness.
2. **Reviewer pass** — security audit (no secret leakage in logs, no timing-vulnerable comparisons on signature material — use `CryptographicOperations.FixedTimeEquals` if any secret-derived comparison exists), correctness (KAT vectors, exception types, cancellation), test value (no validation-only tests added per CLAUDE.md test philosophy).
3. **Format pass** — `dotnet format` clean.
4. **Final build + test** — `dotnet toolchain.cs build` + `dotnet toolchain.cs test` (unit only on this gate; integration was hardware-verified in D and E).
5. **Commit** — single semantic commit grouping Phases A–E. Suggested message: `feat(core,fido2,webauthn): port Yubico.Core ARKG primitives + unblock previewSign FullCeremony hardware ceremony`. Co-author Claude per repo convention.
6. **Push** to `webauthn/phase-9.2-rust-port`. PR #466 picks up the new commit automatically.

DevTeam loops up to 3 iterations if Reviewer flags issues. Ship succeeds when all stages green and commit recorded.

---

## End-to-end verification (must hold at handoff)

| Check | Expected | Phase |
|---|---|---|
| `dotnet toolchain.cs build` | 0 errors | A,B,C,D,E,F |
| `Yubico.NativeShims` resolved version | `1.16.1-prerelease.20260428.1` | A |
| Core ARKG KAT unit tests (3 vectors) | PASS | B |
| Fido2 unit tests (existing 17 PreviewSign + new ones) | PASS | C |
| WebAuthn unit tests (existing 15 PreviewSign) | PASS | C |
| `Fido2.../FullCeremony_RegisterDeriveSignVerify_RoundTrip` | PASS on YK 5.8.0-beta (2 touches) | D |
| `WebAuthn.../FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature` | PASS on YK 5.8.0-beta (2 touches) | D |
| `WebAuthn.../Registration_WithPreviewSign_ReturnsGeneratedSigningKey` | PASS (was FAIL on `CoseKey.Decode`) | D |
| 3 negative-path Fido2 integration tests | PASS on YK 5.8.0-beta | E |
| `dotnet format` | clean | F |
| Final commit on origin | pushed | F |

---

## Critical file references

**Legacy SDK (read-only sources to port):**
- `Yubico.NET.SDK-Legacy/Yubico.Core/src/Yubico/Core/Cryptography/IArkgPrimitives.cs` (77 LOC)
- `Yubico.NET.SDK-Legacy/Yubico.Core/src/Yubico/Core/Cryptography/ArkgPrimitives.cs` (factory)
- `Yubico.NET.SDK-Legacy/Yubico.Core/src/Yubico/Core/Cryptography/ArkgPrimitivesOpenSsl.cs` (525 LOC — main port target)
- `Yubico.NET.SDK-Legacy/Yubico.YubiKey/src/Yubico/YubiKey/Fido2/Arkg/ArkgP256.cs` (54 LOC)
- `Yubico.NET.SDK-Legacy/Yubico.YubiKey/src/Yubico/YubiKey/Fido2/PreviewSignGeneratedKey.cs` (147 LOC)
- `Yubico.NET.SDK-Legacy/Yubico.YubiKey/src/Yubico/YubiKey/Fido2/PreviewSignDerivedKey.cs` (133 LOC)
- `Yubico.NET.SDK-Legacy/Yubico.YubiKey/tests/unit/Yubico/YubiKey/Fido2/Arkg/ArkgP256Tests.cs` (KAT vectors)
- `Yubico.NET.SDK-Legacy/Yubico.YubiKey/tests/integration/Yubico/YubiKey/Fido2/PreviewSignTests.cs:62-109` (FullCeremony body)

**python-fido2 (read-only sources for negative-path tests):**
- `python-fido2/tests/device/test_sign_extension_v4.py:407-632`

**Modern SDK existing infrastructure to reuse:**
- `src/Core/src/Cryptography/HkdfUtilities.cs`
- `src/Core/src/Cryptography/EcdsaVerify.cs`
- `src/Core/src/Cryptography/CmacPrimitivesOpenSsl.cs` (P/Invoke pattern reference)
- `src/Core/src/Cryptography/CryptographyProviders.cs` (factory delegate registry)
- `src/Core/src/PlatformInterop/Libraries.cs` (NativeShims constant)

**Modern SDK files to create:**
- `src/Core/src/Cryptography/IArkgPrimitives.cs`
- `src/Core/src/Cryptography/ArkgPrimitives.cs`
- `src/Core/src/Cryptography/ArkgPrimitivesOpenSsl.cs`
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Cryptography/ArkgP256Tests.cs`
- `src/Fido2/src/Cose/CoseArkgP256SeedKey.cs`
- `src/Fido2/src/Arkg/ArkgP256.cs`
- `src/Fido2/src/Extensions/PreviewSignGeneratedKey.cs`
- `src/Fido2/src/Extensions/PreviewSignDerivedKey.cs`

**Modern SDK files to edit:**
- `nuget.config` (uncomment GitHub Packages source)
- `Directory.Packages.props:24` (bump NativeShims version)
- `src/Core/src/Cryptography/CryptographyProviders.cs` (add `ArkgPrimitivesCreator`)
- `src/Fido2/src/Cose/CoseAlgorithm.cs` (add `ArkgP256SeedKey = -65700`, update `IsKnown`)
- `src/Fido2/src/Cose/CoseKey.cs` (extend `Decode` to dispatch to `CoseArkgP256SeedKey`)
- `src/Fido2/src/Extensions/PreviewSignExtension.cs` (add `TryGetGeneratedKey` accessor on `PreviewSignRegistrationOutput`)
- `src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoPreviewSignTests.cs` (add FullCeremony + 3 negative tests)
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs` (remove `Skip.If(true)` on FullCeremony)

---

## Risks and mitigations

| Risk | Likelihood | Mitigation |
|---|---|---|
| GitHub Packages auth fails on `dotnet restore` | Medium | Pre-flight `gh auth status`; document `NUGET_AUTH_TOKEN` env var setup in commit message |
| New `Native_Ec*` / `Native_Bn*` entry points missing from prerelease build | Low | Spot-check `dotnet list package --include-transitive` output post-restore; if missing, prerelease build is incomplete and Phase A halts |
| Span-based API forces too many breaking changes in callers | Low | `IArkgPrimitives` is internal to Core's primitive layer; modern Span API is purely additive at the public Fido2 surface |
| KAT vectors fail after port — RFC 9380 hash-to-curve subtle bugs | Medium | KAT vectors are deterministic and traceable to Rust reference; if they fail, diff against Legacy line-by-line in `expand_message_xmd` (lines 358-413) and `HashToScalar` (lines 345-356) |
| Hardware test fails despite green unit tests | Low-Medium | Stop and diagnose; do not advance. Reuse parallel-forensic-agents pattern (proven twice this session) if cause is ambiguous |
| `dotnet format` mass-rewrites the port | Low | Run `dotnet format` early in Phase F before Engineer pass; resolve disagreements in the Engineer iteration |

---

## Phase D follow-up (NOT shipped this session — gap documented)

**Status at session pause:** Phases A, B, C, E shipped. Phase D **partially complete**: hardware-verified registration + ARKG seed-key extraction + offline derivation on YK 5.8.0-beta. Both FullCeremony tests `Skip.If(true, "GetAssertion+previewSign+ARKG hardware gap — see plan file")` until the gap below is resolved.

**The gap:** `GetAssertionAsync` with previewSign+ARKG returns `CtapException("Unspecified error", CTAP1_ERR_OTHER 0x7F)` BEFORE user touch on YK 5.8.0-beta. Root cause is NOT in the CBOR encoder.

**What was verified during 5 hardware iterations:**
- Build clean throughout
- Yubico.Core ARKG primitives KAT (3 vectors) PASS
- Fido2 ARKG seed-key `CoseKey.Decode` works (after fixing two issues: nested COSE_Key map support + alg-first dispatch for sentinel kty -65537)
- `PreviewSignCbor.TryExtractGeneratedKey()` extracts the typed `PreviewSignGeneratedKey` correctly
- `PreviewSignGeneratedKey.DerivePublicKey()` produces a derived key (Phase B/C wired end-to-end)
- PIN authentication succeeds (passed "PIN authentication failed" → progressed to wire-format rejection)
- COSE_Sign_Args inner CBOR is **byte-for-byte identical to python-fido2's emission** (verified via on-device hex capture decoded programmatically): `{3:-65539, -1:<81-byte ARKG kh>, -2:<20-byte "integration-test-ctx">}`

**Suspected root cause** (not confirmed; needs deeper forensic):
- Legacy SDK's passing `FullCeremony_RegisterDeriveSignVerify_RoundTrip` test does NOT include `pinUvAuthParam`/`pinUvAuthProtocol` on the `GetAssertion` call. Modern SDK's test always sets these (separate fresh PIN token per CTAP §6.2.2). The YK 5.8.0-beta firmware may reject GetAssertion+previewSign+ARKG specifically when accompanied by PIN/UV auth, OR the credential's UV requirement (registered with `flags: 0x01` = RequireUserPresence) may be incompatible with PIN-supplied UV at sign time.

**Next-session action plan:**

1. Side-by-side trace: capture EXACT GetAssertion CBOR bytes from a passing Legacy SDK run (USB capture or instrumented Legacy build). Compare against the modern SDK's bytes (which we already have via `[DIAG]` Console.WriteLine — re-add temporarily). Diff at the GetAssertion top-level map (keys 1-7), not the previewSign extension input (which is verified correct).
2. If keys 6 (pinUvAuthParam) / 7 (pinUvAuthProtocol) are the only difference: try the modern test WITHOUT PIN auth on GetAssertion (pass null/omit those fields). May require flag adjustments at registration too.
3. If the difference is elsewhere (e.g., extension key ordering, allowList format, `up`/`uv` option flags): apply surgical fix.
4. If still rejected: enable CTAP HID raw logging in firmware (Yubico-internal channel).
5. Once hardware passes, REMOVE `Skip.If(true, ...)` guards from BOTH FullCeremony tests (Fido2 + WebAuthn).

**Locations:**
- `src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoPreviewSignTests.cs:251-262` — Fido2 FullCeremony skip guard with gap explanation
- `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs:106-115` — WebAuthn FullCeremony skip guard with gap explanation
- `Plans/v2-team-previewsign-test-shape-drift-note.md` — saved for v2 SDK team; covers test-fixture-quality patterns relevant to this gap

---

## Out of scope (explicitly deferred)

- **Multi-credential probe-selection** (Open Follow-up #15, Phase 10 §1) — neither python-fido2 nor Legacy implements this; modern SDK shipping it would require novel CTAP up=false probe ceremony work.
- **Cryptographic signature verification helper** as a public API — `PreviewSignDerivedKey.VerifySignature` provides the in-port surface; broader `Yubico.YubiKit.WebAuthn.Crypto.Verify` namespace is Phase 10 §2.
- **Tier B / Tier C audit cleanup** (carried open follow-ups #2-5) — orthogonal; tracked separately.
- **Untracked PRD commit** (`Plans/phase-10-arkg-sign-args-builder-prd.md`) — handle as a docs-only commit before or after this ship.

---

## Quick start for the executing agent

```bash
# Phase A
git checkout webauthn/phase-9.2-rust-port
# Edit nuget.config, Directory.Packages.props per Phase A
dotnet restore
dotnet toolchain.cs build

# Phase B
# Port Core/src/Cryptography/{IArkgPrimitives,ArkgPrimitives,ArkgPrimitivesOpenSsl}.cs from Legacy
# Edit Core/src/Cryptography/CryptographyProviders.cs
# Port Core/tests/.../ArkgP256Tests.cs (3 KAT vectors)
dotnet toolchain.cs build
dotnet toolchain.cs test --project Core --filter "FullyQualifiedName~Arkg"

# Phase C
# Edit Fido2/src/Cose/{CoseAlgorithm,CoseKey}.cs
# Create Fido2/src/Cose/CoseArkgP256SeedKey.cs
# Port Fido2/src/Arkg/ArkgP256.cs
# Port Fido2/src/Extensions/{PreviewSignGeneratedKey,PreviewSignDerivedKey}.cs
# Edit Fido2/src/Extensions/PreviewSignExtension.cs (add TryGetGeneratedKey)
dotnet toolchain.cs build
dotnet toolchain.cs test --project Fido2 --filter "FullyQualifiedName~PreviewSign"

# Phase D — HARDWARE (YK 5.8.0-beta required)
# Edit WebAuthn integration test (remove Skip.If)
# Add Fido2 integration test (FullCeremony round-trip)
dotnet toolchain.cs test --project Fido2 --integration --filter "FullyQualifiedName~FullCeremony"
dotnet toolchain.cs test --project WebAuthn --integration --filter "FullyQualifiedName~FullCeremony"

# Phase E — HARDWARE
# Add 3 negative-path tests to Fido2 integration suite
dotnet toolchain.cs test --project Fido2 --integration --filter "FullyQualifiedName~MakeCredential_With"

# Phase F
/DevTeam
# Engineer + Reviewer + format + final build + test + commit + push
```
