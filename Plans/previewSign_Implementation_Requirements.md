# previewSign (CTAP v4 Draft) Extension — Implementation Requirements

**Source:** DRAFT Web Authentication sign extension v4 (published 2025-08-26)
**Status:** Authori tative spec for SDK implementation
**Length:** 1400 words

---

## 1. Purpose

The `previewSign` extension allows a Relying Party (web application) to use a WebAuthn credential not only for *authentication assertions* (signing a challenge), but also for *arbitrary data signing*. The signing key is separate from the authentication credential key pair but bound to the same authenticator device. A registration ceremony generates a new signing key pair and returns the public key; subsequent authentication ceremonies can sign arbitrary data (raw bytes) using the private key, without including authenticator data or client data. **Use case:** Generate verifiable credentials whose proofs are signed only by a WebAuthn device — decoupling the signing key from the authentication key.

---

## 2. Authoritative Names

- **Extension identifier:** `previewSign` (exact string)
- **No aliases mentioned.** Previous versions used `sign` (v3–v1); v4 renamed to `previewSign` in preparation for broader prototype availability.
- **CTAP error codes (when applicable):** `CTAP2_ERR_UNSUPPORTED_ALGORITHM`, `CTAP2_ERR_INVALID_OPTION`, `CTAP2_ERR_UP_REQUIRED`, `CTAP2_ERR_PUAT_REQUIRED`, `CTAP2_ERR_INVALID_CREDENTIAL`, `CTAP2_ERR_MISSING_PARAMETER`.

---

## 3. Extension Input Schema

### Registration Input
```csharp
// Client-side (TypeScript/WebIDL)
dictionary AuthenticationExtensionsSignGenerateKeyInputs {
    required sequence<COSEAlgorithmIdentifier> algorithms;  // Ordered by preference
};
```

| Field | Type | Required | Semantics |
|-------|------|----------|-----------|
| `algorithms` | `sequence<COSEAlgorithmIdentifier>` | **Yes** | Ordered list of acceptable signing algorithms (most to least preferred). Authenticator picks the first supported one. If none supported → registration fails with `CTAP2_ERR_UNSUPPORTED_ALGORITHM`. |

### Authentication Input
```csharp
dictionary AuthenticationExtensionsSignSignInputs {
    required BufferSource keyHandle;
    required BufferSource tbs;  // "to be signed"
    COSESignArgs additionalArgs;
};

typedef BufferSource COSESignArgs;  // CBOR-encoded COSE_Sign_Args
```

| Field | Type | Required | Semantics |
|-------|------|----------|-----------|
| `keyHandle` | `BufferSource` | **Yes** | Key handle from prior registration output (`generatedKey.keyHandle`). Authenticator uses this to re-derive the signing private key. |
| `tbs` | `BufferSource` | **Yes** | Raw data to be signed. **Unaltered** by authenticator—no clientDataJSON, no authenticator data wrapping. Depending on algorithm, RP may pre-hash. |
| `additionalArgs` | `COSESignArgs` (CBOR map) | Optional | Algorithm-specific signing arguments (per COSE two-party signing spec). MUST be CBOR-encoded `COSE_Sign_Args`. |

**Validation rules (client):**
- During registration: `generateKey` MUST be present; `signByCredential` MUST NOT be present.
- During authentication: `signByCredential` MUST be present; `generateKey` MUST NOT be present.
- `signByCredential` is a `record<string, AuthenticationExtensionsSignSignInputs>` mapping **base64url-encoded credential IDs** to sign inputs. Size MUST equal size of `allowCredentials`.
- `allowCredentials` MUST NOT be empty (signing requires knowing which key to use).

---

## 4. Extension Output Schema

### Registration Output
```csharp
dictionary AuthenticationExtensionsSignGeneratedKey {
    required ArrayBuffer keyHandle;
    required ArrayBuffer publicKey;  // COSE_Key format
    required COSEAlgorithmIdentifier algorithm;
    required ArrayBuffer attestationObject;
};

dictionary AuthenticationExtensionsSignOutputs {
    AuthenticationExtensionsSignGeneratedKey generatedKey;  // Omitted in auth
    ArrayBuffer signature;  // Omitted in registration
};
```

| Field | Type | Present | Semantics |
|-------|------|---------|-----------|
| `generatedKey.keyHandle` | `ArrayBuffer` | **Reg only** | Auxiliary handle for the private key. May be zero-length if authenticator stores key internally. RP should prefer extracting from `attestationObject.attestedCredentialData.credentialId` after verifying attestation. |
| `generatedKey.publicKey` | `ArrayBuffer` | **Reg only** | COSE_Key-encoded signing public key. RP should prefer extracting from `attestationObject` after verifying. |
| `generatedKey.algorithm` | `COSEAlgorithmIdentifier` | **Reg only** | Algorithm chosen from input list. May differ from `alg (3)` in `publicKey` if using split signing algorithms (COSE two-party signing spec). |
| `generatedKey.attestationObject` | `ArrayBuffer` | **Reg only** | Attestation object for the signing key pair. Same structure as credential attestation but `previewSign` extension output contains `flags` (UP/UV policy) instead of `alg`/`sig`. |
| `signature` | `ArrayBuffer` | **Auth only** | Raw signature over `tbs` input (no wrapped data). MUST be present in authentication output. |

---

## 5. CBOR Encoding

### Authenticator Extension Input (CDDL + Map Keys)
```cddl
; Integer aliases (left = symbolic, right = CBOR int key)
kh = 2
alg = 3
flags = 4
tbs = 6
args = 7

$$extensionInput //= (
  previewSign: {
    ; Registration input
    alg        => [ + COSEAlgorithmIdentifier ],
    ? flags    => &(unattended: 0b000,
                    require-up: 0b001,
                    require-uv: 0b101) .default 0b001,
    //
    ; Authentication input
    kh         => bstr,
    tbs        => bstr,
    ? args     => bstr .cbor COSE_Sign_Args,
  },
)
```

| Key | Type | CBOR Int | Presence | Value |
|-----|------|----------|----------|-------|
| `alg` | CBOR array of ints | **3** | Registration only | List of COSE algorithm IDs, in preference order. |
| `flags` | CBOR uint | **4** | Registration optional | User presence / verification flags: `0b000` (none), `0b001` (UP required, default), `0b101` (UP+UV required). |
| `kh` | CBOR bstr | **2** | Authentication only | Signing key handle (byte string). |
| `tbs` | CBOR bstr | **6** | Authentication only | Data to be signed. |
| `args` | CBOR bstr (contains CBOR map) | **7** | Authentication optional | COSE_Sign_Args encoded as CBOR byte string (nesting limit safety). |

**Ordering:** No strict order required, but map keys MUST be unique.

### Authenticator Extension Output (CDDL + Map Keys)
```cddl
alg = 3
flags = 4
sig = 6

$$extensionOutput //= (
  previewSign: {
    ; Registration output
    alg     => COSEAlgorithmIdentifier,
    //
    ; Authentication output
    sig     => bstr,
    //
    ; Attestation (in nested attestObject)
    flags   => &(unattended: 0b000,
                 require-up: 0b001,
                 require-uv: 0b101)
  },
)
```

| Key | Type | CBOR Int | Ceremony | Value |
|-----|------|----------|----------|-------|
| `alg` | CBOR int | **3** | Registration | Chosen COSE algorithm ID. |
| `sig` | CBOR bstr | **6** | Authentication | Raw signature bytes over `tbs`. |
| `flags` | CBOR uint | **4** | Attestation only | Copy of input `flags`, appears only in nested attestation object's `previewSign` extension output. |

### Unsigned Extension Output (Registration only)
```cddl
att-obj = 7

$$unsignedExtensionOutput //= (
  previewSign: {
    att-obj => bstr .cbor attObj,  ; Attestation object for signing key
  },
)
```

| Key | Type | Value |
|-----|------|-------|
| `att-obj` | CBOR bstr containing CBOR map | Complete attestation object (fmt, authData, attStmt) for the signing public key. |

---

## 6. Algorithms and Key Types

- **Supported algorithms:** Any `COSEAlgorithmIdentifier` the authenticator supports for signing.
  - Common: `ES256` (-7), `EdDSA` (-8), `ES384` (-35), `ES512` (-36), `RS256` (-257), etc.
  - No restriction to EC2 only; RSA, EdDSA all permissible.
  - **Two-party signing algorithms** (I-D.cose-2p-algs) allowed if `additionalArgs` provided.
- **Key types:** COSE_Key format (RFC 9052). Public key returned in COSE_Key encoding (includes algorithm, coordinates, etc.).
- **Relation to credential creation:**
  - Signing key pair is **independent** of credential authentication key pair.
  - Both can use different algorithms; RP specifies signing algorithms separately.
  - Each credential can have at most one associated signing key pair.

---

## 7. Flow Details

### Registration Ceremony (Key Generation)
1. **RP sends:** `create()` with `extensions.previewSign.generateKey.algorithms = [alg1, alg2, ...]` and optionally `userVerification = "required"` (sets UV flag).
2. **Client processing:**
   - Validates `generateKey` present, `signByCredential` absent.
   - Sets authenticator input: CBOR map with `alg` (array) and optional `flags` (default `0b001` = UP required).
3. **Authenticator processing:**
   - Iterates `alg` array; picks first supported algorithm.
   - If none supported → error `CTAP2_ERR_UNSUPPORTED_ALGORITHM`.
   - Generates key pair (deterministically seeded from auxIkm + per-credential secret + flags).
   - Encodes key handle `kh` (authenticator-specific; example: HMAC-SHA-256(macKey, khParams || "previewSign" || rpIdHash)).
   - Returns `authData.extensions["previewSign"][alg]` = chosen algorithm.
   - Returns unsigned extension output: `att-obj` = attestation object for signing public key.
4. **Client extraction:**
   - Parses unsigned outputs; retrieves attestation object.
   - Sets client output `generatedKey`: {keyHandle, publicKey, algorithm, attestationObject}.

### Authentication Ceremony (Signing)
1. **RP sends:** `get()` with `extensions.previewSign.signByCredential = { base64url(credId1): {keyHandle, tbs, additionalArgs}, ... }`.
2. **Client processing:**
   - Validates `signByCredential` present, `generateKey` absent.
   - Validates `allowCredentials` not empty and size matches `signByCredential` keys.
   - Determines which credentials are available on authenticator.
   - Picks one; sends corresponding sign inputs to authenticator.
3. **Authenticator processing:**
   - Decodes `kh` to extract chosenAlg, signFlags, auxIkm.
   - Validates integrity of `kh` (HMAC check in example encoding).
   - If `args` present, decodes as COSE_Sign_Args; validates `args[alg]` matches chosenAlg.
   - Checks UP/UV flags against current `authData.flags`; fails if required but not set.
   - Re-derives key pair deterministically (same seeds as registration).
   - Signs `tbs` (raw, unaltered) with private key and optional `args`.
   - Returns `authData.extensions["previewSign"][sig]` = signature bytes.
4. **Client extraction:**
   - Sets client output: `signature` = signature bytes.

### Differences from Standard getAssertion
- **No clientDataJSON wrapping:** Signature is over `tbs` only, not over challenge or origin.
- **No authenticator data in signature:** Standard assertion includes authenticator data in what's signed; signing extension does not.
- **Raw byte input:** `tbs` passed unaltered; RP responsible for hashing if needed.
- **Repeated signing:** Same credential can be used to sign multiple different messages (not one-time assertion per challenge).

---

## 8. Validation Rules

### Client-Side (Registration)
- `generateKey` MUST be present; MUST contain non-empty `algorithms` array.
- `signByCredential` MUST NOT be present.
- All algorithm identifiers MUST be valid COSE integers.

### Client-Side (Authentication)
- `signByCredential` MUST be present; MUST be a map.
- `generateKey` MUST NOT be present.
- `allowCredentials` MUST NOT be empty.
- Size of `signByCredential` keys (base64url-encoded) MUST equal size of `allowCredentials`.
- Each `allowCredentials[i].id` MUST have a corresponding entry in `signByCredential` (keyed by base64url of ID).
- Each entry's `keyHandle` and `tbs` MUST be present (non-empty buffers).
- If `additionalArgs` present, MUST be valid CBOR-encoded COSE_Sign_Args.

### Authenticator-Side (Registration)
- At least one algorithm from input `alg` array MUST be supported; else return `CTAP2_ERR_UNSUPPORTED_ALGORITHM`.
- `flags` (if present) MUST be one of `0b000`, `0b001`, `0b101`; else return `CTAP2_ERR_INVALID_OPTION`.
- Key handle `kh` encoding SHOULD include integrity check (HMAC).

### Authenticator-Side (Authentication)
- `kh` and `tbs` MUST be present; else return `CTAP2_ERR_INVALID_OPTION`.
- `kh` MUST decode successfully and pass integrity check; else return `CTAP2_ERR_INVALID_CREDENTIAL` (in example encoding).
- If `args` present, MUST be valid COSE_Sign_Args; `args[alg]` MUST match extracted chosenAlg, else return `CTAP2_ERR_INVALID_CREDENTIAL`.
- If `args` absent but algorithm requires additional arguments → return `CTAP2_ERR_MISSING_PARAMETER`.
- UP flag in signFlags: If set, MUST be set in `authData.flags` → else return `CTAP2_ERR_UP_REQUIRED`.
- UV flag in signFlags: If set, MUST be set in `authData.flags` → else return `CTAP2_ERR_PUAT_REQUIRED`.

---

## 9. Security Considerations

- **User Verification Policy:** Set once at registration (via `flags`). Signing operations MUST enforce the chosen policy (UP only, UP+UV, or unattended).
- **Attestation:** Supported; attests signing public key separately from credential. Attestation object embeds `flags` so RP can verify UP/UV requirement.
- **Scope Limiting:** RP MUST NOT use empty `allowCredentials` (enforced by spec). Prevents anonymous "sign any credential" attacks.
- **Replay Prevention:** Raw `tbs` is not tied to a nonce or timestamp by the extension. RP responsible for including anti-replay material (timestamp, nonce) in `tbs` if needed.
- **Signature Counter:** Signing extension output does NOT include `signCount`; authenticator data's `signCount` is not tied to signing operations (set to 0 in attestation object). No implicit replay protection via counters.
- **AAGUID:** Included in attestation object for signing key; RP can identify authenticator make/model.
- **Sensitive Material:** Private key never leaves authenticator; RP never sees it. Attestation can be verified offline to confirm public key came from trusted authenticator.

---

## 10. Differences from Standard WebAuthn Assertion

| Aspect | Standard Assertion | previewSign |
|--------|-------------------|-------------|
| **Signed data** | clientDataJSON + authenticator data | Raw `tbs` only |
| **Challenge** | One-time nonce per ceremony | Arbitrary data (RP-supplied) |
| **Signature count** | Incremented, returned in authData | Not used (auth data signCount = 0) |
| **Repeated use** | One assertion per ceremony | Same credential signs multiple messages |
| **Key pair** | Credential key pair | Separate signing key pair |
| **Coupling** | Signed data coupled to origin, RP ID, challenge | Signed data unrelated to origin or challenge |

---

## 11. Cross-References & Parity Notes

- **COSE Algorithm Spec:** RFC 9052 / RFC 9053. All COSE algorithm identifiers defined there are valid.
- **COSE Two-Party Signing:** I-D.cose-2p-algs (draft). Defines `COSE_Sign_Args` structure for algorithms requiring split signing. SDK must support passing `additionalArgs` through to authenticator.
- **Key Handle Encoding:** Section 10.2.1.1 provides **example** implementation (HMAC-SHA-256 integrity check). Authenticators MAY use different encoding; SDK must accept any valid kh from prior registration.
- **Parity with Java/Python/Swift:** All SDKs MUST:
  - Accept CBOR-encoded `previewSign` extension outputs from authenticator.
  - Encode client inputs (algorithms, keyHandle, tbs, additionalArgs) correctly as CBOR for authenticator.
  - Handle base64url-encoded credential ID mapping in `signByCredential`.
  - Validate flags consistency (UP/UV requirements enforced at auth time).

---

## 12. Open Questions / Implementation Decisions

1. **Pre-hashing responsibility:** Does RP pre-hash `tbs` or pass raw data? **Decision:** Spec says "depending on the signing algorithm, this may or may not need to be pre-hashed." SDK should document which algorithms expect pre-hashed input and provide helper functions if needed.

2. **Key handle storage:** If RP requests attestation, should kh be stored alongside attestation object or separately? **Decision:** Spec recommends extracting credential ID from attestation object after verification; kh from `generatedKey.keyHandle` is for offline RP use.

3. **Algorithm selection logic:** If authenticator doesn't support any algorithm in the list, should client retry with different RP or fail immediately? **Decision:** Fail immediately with `NotSupportedError` (per spec step 4, registration).

4. **Signature format:** Are signatures returned as raw bytes or DER-encoded? **Decision:** Raw bytes (COSE convention). SDK must not wrap or DER-encode.

5. **Multiple signing keys per credential:** Can RP request multiple signing key pairs for one auth credential? **Decision:** No. Each credential = at most one signing key pair. Create new credentials for additional keys.

6. **Handling of args nesting:** Why is `args` wrapped in a byte string instead of a direct CBOR map? **Answer:** CBOR has a max 4-level nesting limit; unwrapped map could exceed this if it contains nested arrays/maps. Wrapping as byte string counts as one level.

---

## Summary

The `previewSign` extension decouples signing from authentication by providing a separate, reusable signing key pair bound to a WebAuthn credential. Registration creates the signing key and returns its public key; authentication ceremonies sign arbitrary data without the ceremony-level metadata that wraps assertion signatures. The extension is algorithm-agnostic and supports attestation. Client validation enforces non-empty allowCredentials; authenticator validation enforces UP/UV policies at signing time. SDK implementation must handle CBOR encoding/decoding, base64url credential ID mapping, and attestation object parsing.

