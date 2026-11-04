# Investigator E — focused primary evidence

Access date for every URL below: **2026-11-03**. These are bounded excerpts inspected from originals, not a certification or an executed interoperability result. Ellipses omit unrelated text. Exact revisions are intentional; do not replace the URLs with a moving latest link.

## S21 — CTAP 2.1 Proposed Standard, 2021-06-15, errata 2022-06-21

https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html

- §12.1.1, client processing: “When enforceCredentialProtectionPolicy is true, and credentialProtectionPolicy’s value is either userVerificationOptionalWithCredentialIDList or userVerificationRequired, the platform SHOULD NOT create the credential in a way that does not implement the requested protection policy.” This is **platform SHOULD NOT**, not an authenticator-side enforcement bit.
- §12.1.1, authenticator processing: “If no credProtect extension was included in the request the authenticator SHOULD use the default value of 1 for compatibility with CTAP2.0 platforms.” Authenticators MAY establish stronger defaults; this does not establish what “most authenticators” do.
- §12.2: “Authenticator MUST support at least 32 bytes to be stored.” “If RPs want to put PII or sensitive information in this field, they MUST use the credProtect extension, setting the credentialProtectionPolicy as userVerificationRequired and enforceCredentialProtectionPolicy as true.” Actor: **RP MUST**, conditional on sensitive/PII content.
- §12.2.1: “If credBlob size is less than or equal to maxCredBlobLength, platform passes the information to the authenticator. Otherwise, platform ignores it.” If no value is stored, getAssertion returns “(empty) Byte String.” A missing extension is a separate observation.
- Heading verification: minPinLength is **§12.4**, hmac-secret is **§12.5**. This revision has no direct largeBlob §12.4 or hmac-secret-mc §12.8. The storage sections §6.10.2–§6.10.7 retain their numbers.

## S22 — CTAP 2.2 Proposed Standard, 2025-07-14

https://fidoalliance.org/specs/fido-v2.2-ps-20250714/fido-client-to-authenticator-protocol-v2.2-ps-20250714.html

- §12.8: “This extension is only applicable for authenticatorMakeCredential, and the hmac-secret extension MUST also be present with the value of \"hmac-secret\" set to true.” Authenticator MUST return MISSING_PARAMETER without hmac-secret. “This extension input is the same as the hmac secret extension’s getAssertion input.”
- §12.5 minPinLength: “This extension returns the current minimum PIN length value.” “This extension is only applicable during credential creation.” Unauthorized RP: “the authenticator ignores the extension and does not return any authenticator extension output.”
- The guessed 2025-02-28 2.2 HTML URL returned HTTP 404; one focused lookup resolved the original above. No statement relies on the failed URL.

## S23 — CTAP 2.3 Proposed Standard, 2026-02-26

https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html

Status: “This document was published by the FIDO Alliance as a Proposed Standard Specification.”

- §6.4, encIdentifier 0x19: “AES-128-CBC encryption of (128-bit device identifier) using HKDF-SHA-256(salt = 32 zero bytes, IKM = persistentPinUvAuthToken, L = 16, info = \"encIdentifier\"). The encryption iv must be regenerated for each output of getInfo.”
- §6.4, encCredStoreState 0x1E: corresponding **128-bit credential store state**, HKDF info “encCredStoreState”; IV regenerated for each GetInfo output. Both fields are **Optional** GetInfo members, not per-credential credential-management outputs.
- §6.4 maxCredBlobLength: “If present, this value MUST be at least 32 bytes.” Minimum capacity, not fixed maximum. minPINLength is current enforced minimum in Unicode code points; default preconfigured minimum is **at least** four. The separate PIN UTF-8 byte constraint is not the same measurement.
- §6.10.2: maxFragmentLength = maxMsgSize minus 64, default 960. Required read response config 0x01 “Contains the requested substring of the serialized large-blob array.” Platform SHOULD repeat after full fragment; stops on short/empty. Platform MUST verify trailing SHA-256 and, if corrupt, discard and act as initial array.
- §6.10.2 write authentication message: `32×0xff || h'0c00' || uint32LittleEndian(offset) || SHA-256(contents of set byte string)`; length appears iff set and offset zero. Authenticator checks token/`lbw` **if protected by UV or alwaysUv true**. Note: array “can be written without user verification if user verification is not configured.” SDK policy can be stricter, but must be identified as SDK policy.
- §6.10.2: “If the length of the pending serialized large-blob array is equal to expectedLength: Verify that the final 16 bytes ... [match] ... Commit ...” Double-buffering not universal: compliant single-buffer implementation permitted; “even with double-buffering ... ‘torn write’ ... unusable config.” Thus neither an intermediate success nor a failed call proves transactional rollback.
- §6.10.2: “Platforms SHOULD take care to preserve existing entries ... where space permits.” Platform MUST NOT attempt write exceeding reported maxSerializedLargeBlobArray.
- §6.10.3: conforming map MUST include required elements, MAY include unknown elements. AEAD_AES_256_GCM, 12-byte nonce, ciphertext with appended tag, compressed plaintext, AAD `"blob" || uint64LittleEndian(origSize)`.
- §6.10.4: decryption failure → skip element; decompression failure/length mismatch → error. Note: “Platforms SHOULD limit the maximum permitted value of origSize and that maximum SHOULD be at least 1MiB.” This is a **SHOULD note**, not mandated universal maximum.
- §12.3 largeBlobKey: “Since this value is a random key, an authenticator MAY derive it as needed from other key material, rather than storing the value itself.” It is not necessarily derived from a credential private key. Feature detection requires both `largeBlobKey` extension and `largeBlobs:true` option. makeCredential requires `rk:true`.
- §12.3: largeBlobKey is response key **0x05** for MakeCredential and **0x07** for GetAssertion, “not in the extensions field of the authenticator data.” Authenticator extension output: “None.”
- §12.4 direct largeBlob: alternative to authenticatorLargeBlobs + largeBlobKey. “Authenticators MUST NOT support both extensions.” Registration input **does** contain `{support:"required"/"preferred"}`; output `{supported:true}` in unsigned outputs. Assertion read returns **compressed blob + originalSize** in unsigned outputs; write returns written. Do not conflate this with WebAuthn's decompressed result or an invented largeBlobKey member.
- §12.7 hmac-secret: authenticator generates **two random 32-byte** values, CredRandomWithUV and CredRandomWithoutUV. Output is HMAC-SHA-256 of selected CredRandom and salt; selection depends on response UV bit. The secret is credential-scoped, not derived from PIN/biometrics. `up:false` returns UNSUPPORTED_OPTION. Input decrypted salts must be 32 or 64 bytes. §12.8 has the companion extension requirement quoted under S22.

## P4 — Yubico Labs previewSign version 4

https://yubicolabs.github.io/webauthn-sign-extension/4/#sctn-sign-extension

Title: **DRAFT: Web Authentication sign extension: Signing arbitrary data using the Web Authentication API. Version 4**. Header: **Editor’s Draft, 26 August 2025**. Abstract: “This is a draft in a temporary location; the extension is intended to be upstreamed to WebAuthn after some more prototyping.” It is not “CTAP version 4.”

- §10.2.1: signing key associated with credential but **different from credential key pair**; data signed unaltered, excluding authenticator/client data. `tbs` “may or may not need to be pre-hashed by the Relying Party” depending on algorithm.
- `additionalArgs`, if present, “MUST contain a CBOR map encoding a COSE_Sign_Args object.” CDDL `args => bstr .cbor COSE_Sign_Args`; key 7 wraps CBOR, not direct nested map. SDK byte passthrough is not validation of this requirement.
- Generated key handle “REQUIRED but MAY be a zero-length byte array”; authenticator-specific handle “MAY be empty if the authenticator can store equivalent information internally.”
- Registration input flags: 0 unattended, 1 UP, 5 UP+UV; omitted defaults to 1. Flags fixed at creation. Authenticator authentication processing checks UP/UV bits against stored policy. The ARKG ticket is not a generic substitute for ceremony PIN/UV authorization.
- Signed outer registration output contains **alg only**; flags output is “Present only in the attestation object embedded within the att-obj output during registration ceremonies.” Output parser accepting missing outer flags does not need a beta-firmware exception.
- Signing request requires nonempty allowCredentialDescriptorList; if empty, authenticator returns INVALID_OPTION. Missing algorithm-required args returns MISSING_PARAMETER. Invalid registration flags returns INVALID_OPTION. Unsupported algorithm returns UNSUPPORTED_ALGORITHM.
- Draft has an internal typo in the output description of require-up (reverses UP/UV), while input description and authenticator bit checks agree. Use the unambiguous input/CDDL and bit checks; do not silently promote the contradictory sentence to normative proof.

## V58 — vendor implementation/support bounds

https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-firmware-5.8.html

The table lists hmac-secret-mc, encIdentifier, encCredStoreState, previewSign (Preview only), and ARKG for its named 5.8 families. Signing section: “currently delivered as a preview option with YubiKey Firmware 5.8 and is likely to change before the final specifications are agreed.” This confirms bounded 5.8 support, not earliest support on every family/firmware.

https://developers.yubico.com/Passkeys/Passkey_concepts/Security_key_capabilities/Signing_Extension_Preview.html

“Early Access Only. The previewSign extension, its algorithm, and the algorithm ID (-65539) are not final and will change before general availability. Do not use this in production.” “previewSign requires firmware 5.8+. Check for the \"previewSign\" extension ... and stop if it is missing.” Offline derivation and verification are RP/server concerns. No retrieved primary page establishes the module CLAUDE's universal “no YubiKey supports [direct largeBlob]” claim or hmac-secret-mc 5.4 earliest support.

## A10 — ARKG Internet-Draft -10, 2026-02-27

https://datatracker.ietf.org/doc/html/draft-bradleylundberg-cfrg-arkg-10

Status: Internet-Draft, work in progress, stated expiration **2026-08-31**. No RFC/conformance claim follows from citing it; no claim it is the latest draft at access time.

- §2.3 subordinate party derivation: `ctx` octet string at most 64, may be empty; abort if length exceeds 64; `ctx' = I2OSP(LEN(ctx),1) || ctx`. The 64 limit is explicit algorithm policy, **not** the representable maximum of a one-byte length prefix.
- §2.3 may invoke same public seed any number of times with different ikm/ctx. This is not an absolute promise of unique independent output for every distinct context or a proof the SDK implementation conforms to all of -10.

## L — .NET library research and limits

Context7 resolved `/dotnet/docs` and `/websites/learn_microsoft_en-us_dotnet_api`; two focused queries (CBOR raw-value preservation and ECDsa.VerifyData signature format) returned no matching documentation. Focused official API fallback:

- https://learn.microsoft.com/en-us/dotnet/api/system.formats.cbor.cborreader.readencodedvalue?view=net-10.0 — “Reads the next CBOR data item, returning a ReadOnlyMemory view of the encoded value.” “For indefinite length encodings this includes the break byte.” This is a view, unlike E's source's decode/re-encode implementation.
- https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.ecdsa.verifydata?view=net-10.0 — verifies signature for provided data with specified hash algorithm; overload accepts DSASignatureFormat. No runtime experiments were performed. DER finding below is based on the SDK's explicit ASN.1 conversion and omitted exhaustion checks, not assumed runtime behavior.

## Research stop conditions

Initial originals + one focused follow-up settled preview v4 URL/status, 2.2 URL, and 2.1 numbering. Unsettled: earliest support for encIdentifier/PPUAT and hmac-secret-mc on all YubiKey families; exact provenance behind beta-specific 0x7F/ECDH-reset claims; full ARKG -09 versus -10 equivalence; all direct-largeBlob hardware support. Mark unknown, do not expand into hardware experiments or a whole Core/COSE/WebAuthn audit.
