# G focused primary-source evidence

Accessed 2026-11-03. All listed original HTTP documents returned 200. Excerpts below normalize HTML whitespace; they are focused quotations, not complete cached specifications. Research used an initial retrieval and one focused follow-up for descriptor/user/version and misplaced search hits. Missing matches were not treated as absence of a requirement. No device or executable experiment was run.

## S20 — CTAP 2.0

Client to Authenticator Protocol, FIDO Proposed Standard, **30 January 2019**.
https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html

§5.2 response table, authenticator actor:

> credential (0x01) PublicKeyCredentialDescriptor Optional
> May be omitted if the allowList has exactly one Credential.

> User identifiable information (name, DisplayName, icon) MUST not be returned if user verification is not done by the authenticator.

> FIDO devices - device resident credentials: For device resident keys on FIDO devices, at least user "id" is mandatory.

The exception is about **request allow-list cardinality**, not merely one match or non-discoverability.

## S21 — CTAP 2.1 original and errata, separately verified

Original: FIDO Proposed Standard, **15 June 2021**.
https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-20210615.html

Errata: header identifies Proposed Standard, **21 June 2022**.
https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html

Both §6.2.2 response tables:

> credential (0x01) PublicKeyCredentialDescriptor Required
> User identifiable information (name, DisplayName, icon) MUST NOT be returned if user verification is not done by the authenticator.
> For server-side credentials on FIDO devices, this parameter is OPTIONAL
> For discoverable credentials on FIDO devices, at least user "id" is mandatory.

Both also describe the platform's account picker and say it will ONLY return id to the WebAuthn layer and discard other user details. That is a platform-to-WebAuthn privacy boundary, not a prohibition on the CTAP response containing display fields after UV.

Errata §6.4:

> pinUvAuthProtocols (0x06) ... List of supported PIN/UV auth protocols in order of decreasing authenticator preference. MUST NOT contain duplicate values nor be empty if present.

Errata hmac-secret section is **§12.5**, not §12.7. Original and errata are not silently interchangeable citations.

## S23 — CTAP 2.3

FIDO Proposed Standard, **26 February 2026**.
https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html

§6.2.2 repeats S21's required descriptor, display-field MUST NOT without UV, OPTIONAL server-side user entity and mandatory discoverable id. NumberOfCredentials is optional, defaults to one, and its requirement depends on multiple found credentials and authenticator display/UV/UP conditions; next-assertion responses omit it.

§6.1, platform actor:

> Platforms MUST NOT include both the "uv" option key and the pinUvAuthParam parameter in the same request.
> Platforms SHOULD NOT send "up" to a CTAP2.0 authenticator.

The backward-compatibility note says CTAP2.0 returns INVALID_OPTION if up is present. Later makeCredential accepts true, not false. The token carries verification; a PIN token is not paired with uv:true.

§6.5.5.7.3, #getPinUvAuthTokenUsingUvWithPermissions:

> This subCommand MUST be implemented if the authenticator returns both uv and pinUvAuthToken option IDs set to true in the authenticatorGetInfo response.
> The platform SHOULD request only the permissions absolutely necessary.
> If either the platform receives CTAP2_ERR_UV_BLOCKED or uvRetries is 0, and clientPin option ID is set to true, then the platform MAY fall back

uv alone is not a guaranteed permission-token capability. This does not mandate PIN-first SDK policy or a particular prompt-attempt limit.

§12.1, #sctn-credProtect-extension, **platform/client** actor:

> When enforceCredentialProtectionPolicy is true, and credentialProtectionPolicy’s value is either userVerificationOptionalWithCredentialIDList or userVerificationRequired, the platform SHOULD NOT create the credential in a way that does not implement the requested protection policy.

Not an authenticator wire boolean; do not strengthen SHOULD NOT to a quoted MUST or mistake output inspection for prevention.

§12.3, #sctn-largeBlobKey-extension:

> largeBlobKey in the extensions field.
> largeBlobs mapped to true in the options field.
> If the options field ... does not map rk to true, return CTAP2_ERR_INVALID_OPTION.

Key is top-level response 0x05 at makeCredential / 0x07 at getAssertion, not signed extension output. The platform-internal key is not suitable to be directly exposed to RPs.

§12.4, #sctn-largeBlob-extension:

> Authenticators MUST NOT support both extensions.
> Authenticator input for authenticatorMakeCredential ... support: "required" / "preferred"

Therefore “CTAP itself carries no largeBlob:{support}” is false as an unqualified later-version statement, even when this SDK implements only largeBlobKey/storage.

§6.10.2, #largeBlobsRW:

> If the authenticator is protected by some form of user verification or the alwaysUv option ID is present and true
> Check if the pinUvAuthToken has the lbw permission
> This implies that a serialized large-blob array can be written without user verification if user verification is not configured.

Token requirement is conditional in CTAP; SDK writes are stricter. Serialized array transfers are stateful. Failure/cancellation does not establish transactional rollback.

§12.5, #sctn-minpinlength-extension:

> This extension returns the current minimum PIN length value.
> Boolean asking for minimum PIN length value in Unicode code points.
> not authorized, the authenticator ignores the extension and does not return any authenticator extension output.

§12.7, #sctn-hmac-secret-extension: two 32-byte CredRandom values (WithUV / WithoutUV), encrypted salts and results; platform MUST include the protocol parameter if not protocol 1. With up:false the authenticator returns UNSUPPORTED_OPTION. This is separate from the client's silent credential probe, which sends no hmac-secret input.

## W3 — WebAuthn Level 3

W3C Recommendation, **25 August 2026**.
https://www.w3.org/TR/2026/REC-webauthn-3-20260825/

§10.1.4, #prf-extension, client actor:

> This extension only exposes a single PRF per credential and, when implementing on top of hmac-secret, that PRF MUST be the one used for when user verification is performed. This overrides the UserVerificationRequirement if necessary.

§10.1.5, #sctn-large-blob-extension, client authentication processing:

> If allowCredentials does not contain exactly one element: Return a DOMException whose name is “NotSupportedError”.
> If the read is not successful, largeBlob will be present ... but the blob member will not be present.

This concerns original WebAuthn options, not the narrowed CTAP final request.

§9.4, #sctn-client-extension-processing:

> There MUST NOT be any values returned for ignored extensions.

No inspected §9.4 passage mandates this SDK's CborContentException/InvalidOperationException catch list.

§13.4.3, #sctn-cryptographic-challenges:

> the challenges MUST contain enough entropy to make guessing them infeasible. Challenges SHOULD therefore be at least 16 bytes long.

RP duties remain separate from local client orchestration and parsing (verification procedures §7.1/§7.2). Do not call locally generated demonstration hashes complete server registration/sign-in.

## W2 — retained UV preference reference

WebAuthn Level 2, W3C Recommendation, **8 April 2021**.
https://www.w3.org/TR/2021/REC-webauthn-2-20210408/#enumdef-userverificationrequirement

§5.8.6: Required means RP fails without UV flag; Preferred does not fail solely for absent UV; Discouraged expresses RP desire not to employ UV. This does not specify the SDK's PIN-first choice, token availability model or retry loop. Retain L2 where already cited, not an automatic L3 upgrade.

## V4 — previewSign

**DRAFT: Web Authentication sign extension, Version 4**, Yubico Editor’s Draft, **26 August 2025**.
https://yubicolabs.github.io/webauthn-sign-extension/4/#sctn-sign-extension

§10.2.1:

> This value is REQUIRED but MAY be a zero-length byte array.
> If present, this MUST contain a CBOR map encoding a COSE_Sign_Args object
> require-up: 0b001, require-uv: 0b101

Generated handle/public key come from inner signing-key attestation; RP requesting attestation SHOULD retrieve them after verifying it. TBS/prehashing is algorithm-specific. Vendor version 4 is not CTAP4 or a W3C adopted extension. The original WA-B report records contradictory output-description bit prose; input/enforcement meanings are the safe evidence, not that reversed sentence.

## F1 — framework ownership evidence via Context7

Context7 resolved `/dotnet/docs`, then queried Memory ownership/lease rules on 2026-11-03.
https://github.com/dotnet/docs/blob/main/docs/standard/memory-and-spans/memory-t-usage-guidelines.md

> Rule #7: If you have an IMemoryOwner<T> reference, you must at some point dispose of it or transfer its ownership (but not both).
> Rule #4: ... returns a Task, you must not use the Memory<T> instance after the Task transitions to a terminal state.

Read-only views constrain writes; they do not supply owned disposal or zeroing. Framework guidance does not prove SDK cleanup, null conversion execution, hardware behavior or cryptographic validity. WA-IMP-016's null-array conversion execution remains the **historical isolated experiment**, not a new G test.
