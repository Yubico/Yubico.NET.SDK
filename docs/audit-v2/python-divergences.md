# Python divergences

This file lists places where the canonical Python implementation (`python-fido2` or `yubikey-manager`) appears to share an issue we found in the .NET SDK, or deviates from the spec in a way we should not copy.

Nothing here has been reported upstream. Each entry is a candidate to look at later. The linked finding page holds the evidence, the spec quote and the pinned Python link.

| # | Finding | Python project | What Python does | Spec or contract | Deliberate? |
| --- | --- | --- | --- | --- | --- |
| 1 | [#4](findings/04-getassertion-unsigned-extension-outputs.md) | python-fido2 | Decodes unsigned extension outputs (0x06) on `AttestationResponse` only; `AssertionResponse` has no field for key 0x08. | CTAP 2.2 / 2.3 §6.2 getAssertion response, key 0x08. | No sign: commit `01d5ac5` adds the field to attestation only, without explanation. |
| 2 | [#5](findings/05-largeblob-supported-misreported.md) | python-fido2 | Sends `largeBlobKey: true` without checking that the request has `rk: true`. | CTAP §12.3: without `rk` true the authenticator returns `CTAP2_ERR_INVALID_OPTION`. | Not investigated. |
| 3 | [#6](findings/06-attestation-none-forwards-statement.md) | python-fido2 | Does not replace the attestation statement when the RP asks for `none`. | WebAuthn attestation conveyance `none`. | No evidence; blame points to a refactor commit (`f47f935`). |
| 4 | [#13](findings/13-largeblob-private-entry-format.md) | python-fido2 | Format is correct. Decoding skips an entry that fails to decompress or has the wrong size, where the spec says return an error. On write, passes malformed entries through. | CTAP §6.10.4 (read), §6.10.2 (write: only conforming entries). | No comment in `blob.py` explains it. |
| 5 | [#22](findings/22-piv-pin-state-shared-across-processes.md) | yubikey-manager (`ykman` CLI) | Opens the card exclusively, but falls back to shared access, where the same cross-process exposure exists. Powering the card off on close limits the window afterwards. | Not a spec violation; a design choice about isolation. | Exclusive-first is deliberate (`20d94d72`); the fallback is too. |
| 6 | [25b](findings/25b-deviceinfo-pagination-loop.md) | yubikey-manager | On OTP and FIDO transports, the device-info page loop has no bound when the device keeps signalling "more data". On smart card it stops at page 256 with an exception. | No spec bound; robustness against a malicious or faulty device. | Not discussed (`13f8a21c`). |
| 7 | [25e](findings/25e-tlv-long-form-length.md) | yubikey-manager | Accepts the reserved initial length octet `0xFF` (as the SDK does). | X.690 / ISO/IEC 8825-1 §8.1.3.5: "the value 11111111 shall not be used". | Not investigated. |
| 8 | [25g](findings/25g-nonpositive-webauthn-batch-size.md) | python-fido2 | Handles a zero `maxCredentialCountInList`, but not a negative one. | CTAP GetInfo: the value is a positive count. | No discussion (`f87a358`). |
| 9 | [25h/26q](findings/25h-26q-openpgp-kdf-count.md) | yubikey-manager | Hashes fewer bytes than one full salt + PIN when the card's KDF count is short, as the SDK does. | RFC 9580 / RFC 4880 §3.7.1.3: always hash the full salt and passphrase at least once. | The byte-count meaning is deliberate; the short-count rule is not discussed (`4bea80dc`). |
| 10 | [26a](findings/26a-prf-credential-specific-inputs-ignored.md) | python-fido2 | At registration, ignores `prf.evalByCredential` unless `hmac-secret-mc` is used, instead of rejecting it. | WebAuthn L3 PRF extension: registration with `evalByCredential` present must be rejected. | Not investigated. |
| 11 | [26d](findings/26d-previewsign-first-entry.md) | python-fido2 | Checks that each allowed credential has a previewSign entry, but accepts extra entries. | previewSign draft v4 §10.2.1: map and allow list must have equal sizes. | Not investigated. |
| 12 | [#11](findings/11-credblob-wrong-cbor-type.md) | python-fido2 | Omits an empty credBlob at creation, where the client rule passes it on. Minor. | CTAP 2.3 §12.2.1. | Not investigated. |
| 13 | [26m](findings/26m-securitydomain-empty-allowlist.md) | yubikey-manager | `store_allowlist` sends an empty list without a guard, which GlobalPlatform defines as "accept all". | GlobalPlatform Amendment F allowlist semantics. Contract issue, not a protocol violation. | The docstring documents the semantics; no guard. |
| 14 | [26p](findings/26p-yubiotp-hmac-challenge-padding.md) | yubikey-manager | Same challenge padding rule as the SDK, so a short challenge and its padded form collide. An empty challenge is padded with `0x00` (the SDK uses `0x01`). | Yubico HMAC-SHA1 challenge-response documentation. | Not investigated. |
| 15 | [#28](findings/28-oath-period-zero-breaks-batch.md) | yubikey-manager | A credential with period 0 makes code calculation divide by zero. | RFC 6238 (X > 0); YKOATH name format. | No evidence (`677b91cd`, `1cb1006d`). |
| 16 | [N1](findings/N1-duplicate-pubkeycredparams.md) | python-fido2 | Forwards duplicate `pubKeyCredParams` entries unchanged. | CTAP 2.1–2.3 §6.1 `pubKeyCredParams`: "MUST NOT include duplicate entries". | Not investigated. |
| 17 | [N2](findings/N2-scp-init-slicing-and-bare-9000.md) | yubikey-manager | Verifies the response MAC only when the response has data; a bare status word is returned unauthenticated. | GlobalPlatform Amendment D §6.2.5 R-MAC. | Same in Android and Swift; not investigated. |

Related, but not divergences:

- [#1](findings/01-prf-not-translated-to-hmac-secret.md): python-fido2 only sends `hmac-secret: true` at registration when a PIN/UV protocol is negotiated. The CTAP registration text doesn't require that. It is a narrower behaviour, not a violation.
- [#7](findings/07-preferred-resident-key-not-requested.md): python-fido2 handles `residentKey: preferred` correctly, deliberately (`24e5e77`). The SDK should match it.
- [26k](findings/26k-oath-device-id-ignored.md): Python checks the device identity on its object-based path; its raw-ID methods make no identity promise, so they aren't a divergence.

Abbreviations: CTAP is Client to Authenticator Protocol; RP is relying party; PRF is pseudo-random function; KDF is key derivation function; R-MAC is response message authentication code; rk is resident key.
