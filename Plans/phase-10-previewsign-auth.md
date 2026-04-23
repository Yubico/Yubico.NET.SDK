# Phase 10 — previewSign Authentication Follow-Ups

**Created:** 2026-04-23
**Status:** Tracker — not yet scheduled
**Owner:** TBD (Phase 10 lead)
**Predecessor:** Phase 9.2 (path 2A) — single-credential previewSign authentication, Rust-validated wire format

## What ships in Phase 9.2 (path 2A)

- `previewSign` **registration** — fully shipped, hardware-proven on YubiKey 5.8.0-beta (Phases 7+8+Gate-2-fixup); also hardware-proven in `cnh-authenticator-rs-extension/hid-test` and `yubikit-android` instrumented tests.
- `previewSign` **single-credential authentication** — wire-format ported from `cnh-authenticator-rs-extension` (`get_assertion.rs:290-323`); deterministic byte-level unit test asserts equivalence against the Rust encoder; integration test re-enabled and gated on user presence; signature returned by hardware (Step 8 verification).

## What defers to Phase 10

### 1. Multi-credential probe-selection (CTAP §10.2.1 step 7)

The current adapter throws `NotSupported` when `signByCredential.Count != 1` (entry point: `src/WebAuthn/src/Extensions/PreviewSign/PreviewSignAdapter.cs:141-149`, method `BuildAuthenticationCbor`). The CTAP spec describes an iterative `up=false` probe across `allowCredentials` to select the matching key — this is not implemented anywhere in our parity evidence base.

**Unblocking criteria (any one suffices):**
- A hardware-proven multi-credential probe in any upstream SDK: yubikit-swift, libfido2, yubikit-android, or `cnh-authenticator-rs-extension`. Today: zero of four. Even Rust's encoder comment at `get_assertion.rs:294` admits multi-credential selection is statemachine-deferred.
- A formal Yubico statement that the YubiKey firmware supports the probe and a recommended client-side iteration pattern.
- An RP-side use case that would consume the probe (until then it is speculative API surface).

**Suspected technical scope (when unblocked):**
- Loop over `allowCredentials`, sending one `up=false` `GetAssertion` per credential with the corresponding `signByCredential` entry, gather candidate matches, then issue the final `up=true` `GetAssertion` against the selected credential.
- The current `Count != 1` throw is the natural entry point — replace with the probe loop. No public-API break expected.
- Reuse the Phase 9.2-validated wire-format encoder for each per-credential probe call.

### 2. Cryptographic signature verification

Phase 9.2 Step 8 asserts only that a signature is **returned** (non-null, non-empty). It does not verify the signature against the registered public key.

**Unblocking criteria:** Phase 9.3 hardware verification expansion, or the post-9 Fido2 canonical extension assessment.

**Suspected technical scope:** P-256 ECDSA verify using the public key returned by registration; surface a verification helper on the `PreviewSignAdapter` (or as a static utility) that callers can use to validate `tbs` → signature for the ARKG-signed shape.

### 3. ARKG `additional_args` first-class support

The current code path treats `additional_args` (CBOR key 7) as an opaque byte string. The Rust hid-test demonstrates the ARKG-specific `COSE_Sign_Args` shape: `{3: alg, -1: arkg_kh, -2: ctx}` (per `native/crates/hid-test/src/main.rs:272-277` and `arkg::encode_arkg_sign_args`).

**Unblocking criteria:** RP-side demand for ARKG, or a Yubico statement about ARKG production support.

**Suspected technical scope:** A first-class ARKG sign-args builder in the C# port that mirrors `arkg::encode_arkg_sign_args` rather than requiring callers to hand-encode the inner CBOR map.

## Reference snapshot (frozen at Phase 9.2 ship time)

| Reference | Path / version | Hardware-proven auth? |
|---|---|---|
| yubikit-swift | release/1.3.0 | No (untested) |
| libfido2 | v1.17.0 | N/A (no previewSign code) |
| yubikit-android | v3.1.0 | No (registration only) |
| cnh-authenticator-rs-extension | commit `c83cbce` (2026-04-09) | **Yes (single-credential only)** |
| Yubico.NET.SDK | webauthn/phase-9.2-rust-port | **Yes (single-credential, after Step 8)** |

Phase 10 should re-survey these references at scheduling time — they evolve.
