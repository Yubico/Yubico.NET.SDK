# Fresh final-boundary original-source checks

**Reviewer:** GPT-6.1 Sol, independent of original investigator G. **Access:** 2026-11-04.
These are independently retrieved original passages, not quotations accepted because investigators agreed.
Whitespace is normalized. No SDK/framework experiment, device exchange, build or test was executed.

Direct Python HTTP retrieval of FIDO originals returned 403. Exa retrieved the exact original URLs below,
including their publication headers and relevant tables. Initial 180,000-character-per-document retrieval
covered response/PIN clauses; a second 550,000-character retrieval of S23 covered its extensions/storage.
W3, V4 and A10 were retrieved directly by HTTP. An initial HTML-extraction attempt failed because bs4
was unavailable; the successful extraction used Python's standard HTMLParser. Neither failure is evidence
about a protocol. Context7 /dotnet/docs supplied memory owner/consumer/lease guidance; actual conversion
mechanics were checked in the original release-pinned runtime source. No moving specification replaced a
version-specific original.

## S20 / S21 / S21E / S23 — descriptor and user response

- **S20:** CTAP Proposed Standard, 30 January 2019:
  <https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html#authenticatorGetAssertion>.
  §5.2 response table, authenticator actor: credential is **Optional**, “May be omitted if the allowList
  has exactly one Credential.” This means the actual CTAP request, not number of matches or original
  multi-entry WebAuthn options before probing/narrowing.
- **S21:** original Proposed Standard, 15 June 2021:
  <https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-20210615.html#authenticatorGetAssertion>.
- **S21E:** errata Proposed Standard, 21 June 2022:
  <https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#authenticatorGetAssertion>.
- **S23:** Proposed Standard, 26 February 2026:
  <https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html>.
  All three later §6.2.2 tables explicitly mark credential **Required**. All say identifying
  name/DisplayName/icon **MUST NOT** be returned without authenticator UV, server-side user is
  **OPTIONAL**, and discoverable credentials require at least user id. The opaque id is not UV-only.
  The account-picker passage says the platform will **ONLY** return id to WebAuthn and discard other
  user details. This latter platform/WebAuthn actor boundary is separate from raw CTAP field truth;
  it does not settle whether the SDK's native selected-result DTO is a browser-equivalent boundary.

## S23 — UV, tokens, sequencing and envelopes

- §6.1/§6.2 option definitions, platform **MUST NOT**: include both the **uv option key** and
  pinUvAuthParam. Explicit false is still the key. uv cannot be included on an authenticator without
  built-in UV. §6.1 says up, if supplied, **MUST** be true, and platforms **SHOULD NOT** send it to
  CTAP2.0. Built-in verification and client-mediated PIN verification are distinct.
- §6.5.5.7.3: the UV permission-token subcommand **MUST be implemented by the authenticator** when
  uv and pinUvAuthToken are both true. Supported-but-unconfigured built-in UV returns NOT_ALLOWED.
  be/acfg additionally depend on uvBioEnroll/uvAcfg; UV_BLOCKED with configured clientPin permits
  platform **MAY** fallback. This is not mandatory PIN-first SDK orchestration or protocol negotiation.
- §6.5.5.7: legacy getPinToken grants default mc/ga; other permissions can only be acquired through
  permission-bearing 0x09/0x06. §6.5.5.7.1 rejects permissions and rpId in legacy 0x05; default mc/ga
  can be suppressed by noMcGaPermissionsWithClientPin. Thus a requested union is not a guarantee that
  any token returned after SDK legacy fallback carries it. §6.5.5.7.2 assigns requested defined
  permissions and supplied RP scope on successful permission-token issuance.
- §6 stateful commands permit failure after unrelated intervening operations or excessive delay.
  §6.3 client logic calls GetNextAssertion N−1 times before account selection; its NFC-specific timer
  qualifications must not be converted into an SDK sequence reservation or rollback guarantee.
- §6.1 response: largeBlobKey **0x05**, unsignedExtensionOutputs **0x06**.
  §6.2.2 response: largeBlobKey **0x07**, unsignedExtensionOutputs **0x08**.
  The response table expressly distinguishes unsigned envelope data from signed authData extensions.
- §6.4: encIdentifier 0x19 and encCredStoreState 0x1E are optional iv || ct, AES-128-CBC encryption
  of separate 128-bit values with HKDF-SHA256(32 zero salt, matching persistentPinUvAuthToken,
  L=16, context encIdentifier/encCredStoreState); IV regenerated on each GetInfo output. The standard
  field is IV16+ct16, not an arbitrary block-aligned helper input or proof of correct-token decryption.

## S23 — extensions/storage

- §12.1 platform **SHOULD NOT** create without implementing requested policy 2/3 when
  enforceCredentialProtectionPolicy is true. The wire input is a policy integer, not an enforcement
  boolean. The high-security note explicitly permits requested 2 → effective 3: stronger is not failure.
- §12.2 RP **MUST** use credProtect level 3 with enforcement true for PII/sensitive credBlob.
  Supporting capacity is at least 32, with actual maxCredBlobLength advertised. §12.2.1 creation sends
  bytes, retrieval sends credBlob:true; retrieval with no stored blob returns an empty byte string.
- §12.3 support requires advertised largeBlobKey plus largeBlobs:true. Creation requires rk:true and
  returns key 0x05, assertion returns key 0x07. No signed extension output is provided. The key “is not
  suitable to be directly exposed to RPs.” It is a random 32-byte storage key; permitted internal
  derivation does not make it the credential private key or a predictable hmac-secret output.
- §12.4 direct largeBlob is a mutually exclusive alternative (**MUST NOT support both extensions**).
  Creation input explicitly has support required/preferred. Its outputs are **unsigned**;
  registration can return supported:true, read returns compressed blob/originalSize, and write returns
  written. Direct CTAP write requires a nonempty allowList, not the separate WebAuthn exact-one rule.
- §6.10.2 write authorization is conditional on configured UV or alwaysUv:true; the note expressly
  permits serialized-array writes without configured verification. Stateful fragments are not a
  transaction/rollback promise. Platform MUST NOT exceed advertised serialized-array size.
- §12.5 minPinLength returns the **current** minimum in Unicode code points during creation; RP
  authorization is checked against minPinLengthRPIDs, and unauthorized requests yield no output.
  §7.4/§6.11 setting policy is a different operation.
- §12.7 generates distinct CredRandomWithUV/CredRandomWithoutUV and chooses by actual response UV.
  Salts are encrypted/authenticated under the key-agreement shared secret. up:false is unsupported.
  §12.8 creation evaluation requires hmac-secret:true as well as hmac-secret-mc. S21/S21E hmac-secret
  remains §12.5; no section number was blindly carried across revisions.

## W3 — WebAuthn L3 Recommendation, 25 August 2026

<https://www.w3.org/TR/2026/REC-webauthn-3-20260825/>.

- §10.1.4 client **MUST** expose the verified-use hmac-secret PRF, overriding UserVerificationRequirement
  if needed: “This extension only exposes a single PRF per credential”. Salt transformation is
  SHA-256(UTF8Encode("WebAuthn PRF") || 0x00 || input). Correct hashing alone is not conformance.
- §10.1.5 authentication write: “If allowCredentials does not contain exactly one element: Return a
  DOMException whose name is ‘NotSupportedError’.” These are original client options, not the eventual
  singleton CTAP request. Successful assertion precedes storage; written reflects write outcome.
  For unsuccessful read, largeBlob remains present but blob member is absent.
- §9.4 **MUST NOT** return values for ignored extensions; it does not prescribe the SDK's precise
  CborContentException/InvalidOperationException catch/isolation policy.
- §13.4.3 challenges **MUST** have infeasible-to-guess entropy; **SHOULD** be at least 16 bytes.
  §7.1/§7.2 RP verification remains independent of local parsing and RP authorization callbacks.

## V4 / A10 — experimental signing/profile

**V4:** <https://yubicolabs.github.io/webauthn-sign-extension/4/>, DRAFT Web Authentication sign
extension, Version 4, Editor's Draft 26 August 2025; temporary draft, not CTAP4/adopted W3C extension.
§10.2.1 allows a REQUIRED zero-length keyHandle; present additionalArgs MUST encode a COSE_Sign_Args
CBOR map. TBS/prehashing is algorithm-specific. Chosen algorithm MAY differ from publicKey alg.
Client processing requires a nonempty original allow list with one signing entry per descriptor,
then chooses the matching entry. Unsigned att-obj is wrapped bstr CBOR; flags belong inside generated-key
attestation. Attestation parsing/repacking is not RP trust verification.

**A10:** <https://www.ietf.org/archive/id/draft-bradleylundberg-cfrg-arkg-10.html>, published 27 February
2026, intended Informational, Internet-Draft expires 31 August 2026; only work-in-progress authority.
§5.1's actual example contains **3:-65700**, direct BL=-1/KEM=-2 maps, kty=-65537 and derived -3:-9.
Its dkalg is optional int/tstr. §5.2 distinguishes seed -65700, signing -65539, verification ESP256=-9.
§2.3 ctx is at most 64 bytes. The SDK agrees on the seed marker/labels/example derived algorithm,
but kty=2, bstr wrapping and required integer dkalg differ. Expiry or partial agreement does not
establish full-profile interoperability, production qualification or a newly discovered seed-ID bug.

## F1 — original runtime memory conversion

<https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/ReadOnlyMemory.cs>.
Fresh retrieval confirms the T[]? constructor assigns default when array is null; implicit array
conversion returns **nonnullable** ReadOnlyMemory<T>; Length reads _length. Assignment to a nullable
memory then wraps the empty struct as present. Actual AuthenticatorInfo byte[]? locals are assigned
directly; actual facade blob constructor receives byte[]?. This independently supports COORD-001 and
WA-IMP-016's presence/length caveats. Prior executed nullable-blob evidence remains historical evidence,
not a fresh reviewer experiment. Read-only views impose no zeroing/ownership guarantee.
