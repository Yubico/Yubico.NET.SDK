# Phase 9.4 — Fido2 Canonical Extension Coverage Polish

**Created:** 2026-04-23
**Status:** Tracker — deferred (non-blocking for the WebAuthn Phase 9 PR)
**Owner:** TBD
**Predecessor:** Phase 9 WebAuthn port (Phases 1–9.3)
**Source:** Post-Phase-9 Fido2 canonical extension coverage assessment (2026-04-23, single Explore agent run per `Plans/yes-we-have-started-composed-horizon.md` Post-Phase-9 section)

## Context

The post-Phase-9 Fido2 coverage assessment surveyed `src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/` and `src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/` against the canonical CTAP 2.1+/v4 extension list. **Verdict: 4 minor gaps, all unit-test coverage polish — no functional defects.** Integration tests for every implemented extension exist and exercise the end-to-end round-trip on real hardware. Unlike the WebAuthn module bug (extensions silently dropped at backend, fixed in `95abc0c5`), no equivalent latent functional gap was found in Fido2.

The horizon doc Post-Phase-9 decision rule was: "If gaps are trivial (≤ 5 missing tests), file as a 9.4 sub-phase before squash-merging." Since these gaps are **non-functional polish** (the integration round-trip already covers each), they do not warrant blocking the WebAuthn Phase 9 PR. Filed here as a tracker; can be picked up independently.

## Coverage matrix (frozen at 2026-04-23 — re-survey when scheduling 9.4)

| Extension | Registration | Authentication | Round-trip | Negative-case | Notes |
|---|---|---|---|---|---|
| credProtect | ✅ | ✅ | ✅ | ❌ | `FidoCredProtectTests.cs:36,151`; `ExtensionBuilderTests.cs:41` |
| credBlob | ✅ | ✅ | ✅ | ❌ | `FidoCredBlobTests.cs:38,177`; `ExtensionBuilderTests.cs:60`; `ExtensionTypesTests.cs:209,224,240` |
| minPinLength | ✅ | ✅ | ✅ | ❌ | `FidoMinPinLengthTests.cs:36,110`; `ExtensionBuilderTests.cs:119`; `ExtensionTypesTests.cs:259,273` |
| largeBlob | ✅ | ✅ | ✅ | ❌ | `FidoLargeBlobTests.cs:39,142`; `ExtensionBuilderTests.cs:79,99`; `ExtensionTypesTests.cs:107,124,140,156,173,183` |
| largeBlobKey | ✅ | ✅ | ✅ | ❌ | `FidoLargeBlobTests.cs:83,182`; `ExtensionTypesTests.cs:535` (output decode only); **no builder encode test** |
| hmac-secret | ✅ | ✅ | ✅ | ❌ | `FidoHmacSecretTests.cs:36,58,129`; `ExtensionTypesTests.cs:54,89`; **`ExtensionBuilderTests` missing explicit test** |
| hmac-secret-mc | ✅ | n/a | ✅ | ❌ | `FidoHmacSecretTests.cs:94,169,288`; `ExtensionBuilderTests.cs:157` (builder); **no unit decode test** |
| prf | ✅ | ✅ | ✅ | ❌ | `FidoPrfTests.cs:39,127`; `ExtensionBuilderTests.cs:137`; `ExtensionTypesTests.cs:291,307` |
| credProps | ❌ | ❌ | ❌ | ❌ | Not implemented in Fido2 (out of scope here) |
| previewSign | ❌ | ❌ | ❌ | ❌ | WebAuthn-level extension (per `src/WebAuthn/CLAUDE.md`); not part of Fido2 surface |

## Gaps and proposed tests

| # | Gap | Proposed test name | Description |
|---|-----|-------|---|
| 1 | `largeBlobKey` builder encode test | `Build_WithLargeBlobKey_EncodesCorrectly` | `ExtensionBuilder.WithLargeBlobKey()` produces `"largeBlobKey": true` in the CBOR extensions map |
| 2 | `hmac-secret-mc` output decode test | `HmacSecretMcOutput_DecodesCorrectly` | Decoding the `hmac-secret-mc` response output during registration (no-hardware unit test) |
| 3 | Negative case — unsupported extension | `MakeCredential_WithUnsupportedExtension_YieldsEmptyOutputMap` | Requesting an extension the firmware doesn't support is silently dropped (no exception) |
| 4 | Negative case — malformed input boundary | `ExtensionBuilder_WithInvalidCredBlobSize_ThrowsOrSilentlyIgnores` | Boundary conditions (credBlob > 64 bytes, minPinLength out of range) — confirm validation behavior |

## Effort estimate

- 4 unit tests, ~30 lines each → ~120 lines total + minor builder/decoder helper exercises
- Estimated single-engineer ship time: ~1-2 hours including audit
- No new public API surface; no behavior changes; pure test coverage addition

## Unblocking criteria

- Bandwidth on a contributor — these are independent, parallelizable, low-risk
- Or: a future bug-hunt cycle that wants to harden the unit-test surface

## Out of scope for this tracker

- `credProps` extension implementation (extension itself is not in the codebase)
- `previewSign` extension at the Fido2 layer (it lives at the WebAuthn layer per architectural decision)
- Any behavior changes to existing extension code paths

## Closes

When all 4 tests are written and merged, `Plans/yes-we-have-started-composed-horizon.md` Post-Phase-9 section can be marked Done and this tracker deleted.
