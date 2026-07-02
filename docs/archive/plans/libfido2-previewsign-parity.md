# libfido2 previewSign Parity Report

**Date:** 2026-04-22
**Investigated:** libfido2 release 1.17.0 (2026-04-15)
**Verdict:** NONE

## Findings

**Code paths:** No registration or authentication support. Zero matches for `previewSign`, `preview_sign`, `previewsign`, or `preview-sign` across the entire codebase (v1.17.0).

**Supported extensions (confirmed via cbor.c):** libfido2 currently recognizes and implements exactly 7 extension masks:
- `FIDO_EXT_CRED_BLOB` (credBlob)
- `FIDO_EXT_HMAC_SECRET` (hmac-secret)
- `FIDO_EXT_HMAC_SECRET_MC` (hmac-secret on multi-credential)
- `FIDO_EXT_CRED_PROTECT` (credentialProtectionPolicy)
- `FIDO_EXT_LARGEBLOB_KEY` (largeBlob)
- `FIDO_EXT_MINPINLEN` (minPinLength)
- `FIDO_EXT_PAYMENT` (payment)

PreviewSign is absent from this list.

**Hardware tests:** No references to previewSign, preview_sign, or related CTAP v4 features in test harnesses (`regress/`, `examples/`, `fuzz/`, tools). The assertion test (`examples/assert.c`, `regress/assert.c`) exercises only standard HMAC-SECRET and credBlob extensions.

**CHANGELOG/release notes:** v1.17.0 (2026-04-15) claims "Added CTAP 2.3 support" but the commit log and API additions list 45+ new functions covering PIN/UV tokens, payment extension, large blob, and credential manager APIs — **no previewSign reference**. Previous releases (1.16.0, 1.15.0) also show no previewSign.

**Issues / PRs:** Zero results for `previewSign OR preview_sign` in GitHub issue tracker. No recent discussions in PRs about CTAP v4 authentication extensions or multi-credential probing workflows.

**Documentation:** None found. No man pages, examples, or API docs mention previewSign or preview_sign.

## Citations
- [libfido2 v1.17.0 NEWS](https://raw.githubusercontent.com/Yubico/libfido2/1.17.0/NEWS) — CTAP 2.3 announced, no previewSign listed
- [libfido2 v1.17.0 cbor.c](https://raw.githubusercontent.com/Yubico/libfido2/1.17.0/src/cbor.c) — Extension encoding shows only CRED_BLOB, HMAC_SECRET, CRED_PROTECT, LARGEBLOB_KEY, MINPINLEN, PAYMENT; no previewSign
- [libfido2 GitHub repo](https://github.com/Yubico/libfido2) — No code matches for previewSign variants
- [libfido2 v1.17.0 assert.c](https://raw.githubusercontent.com/Yubico/libfido2/1.17.0/src/assert.c) — Only CTAP_CMD_CBOR path, standard extensions only

## Recommendation for Phase 9.2 verdict step

**This report supports DEFER judgment for the previewSign auth+probe parity question.** libfido2 is mature CTAP 2.1/2.3 reference implementation, yet shows zero implementation of previewSign. This strongly suggests either (a) previewSign is a future/draft extension not yet in CTAP stable specs, or (b) it's a proprietary YubiKey extension not standardized. Before committing C# parity work, validate whether previewSign is public CTAP v4 or internal YubiKey protocol and confirm its firmware availability matrix.
