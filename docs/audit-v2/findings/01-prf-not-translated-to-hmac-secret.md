# #1 WebAuthn PRF is not translated to CTAP hmac-secret (YESDK-1609)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed, with a correction |
| Our severity | MED. Release blocker for 2.0 (roadmap workstream 2). |
| Root cause | SDK only |
| Fix group | C (real design decision: low-level interface shape, user verification (UV) policy, registration-time evaluation) |
| Evidence | unit test; hardware (5.7.4 test key, user present, registration step only) |
| Since the audit | Unchanged at current yubikit. No diff between `fdfcd6fd` and `df1ec06d` in the extension, credentials, or WebAuthn client and internal paths. `src/Fido2/src/FidoSession.cs` changed (user-presence labels only). A fix may touch it. |

## What the audit says

The audit says the software development kit (SDK) does not translate the WebAuthn (Web Authentication) pseudo-random function (PRF) extension into CTAP (Client to Authenticator Protocol) `hmac-secret`. Its key claim:

> it cannot invent a literal CTAP "prf" representation.

PRF is a WebAuthn extension that derives secrets from an authenticator. CTAP is the protocol between the platform and the authenticator. In the CTAP layer, PRF is carried by `hmac-secret`.

## What is right

- Registration sends the wrong key. `ExtensionPipeline` calls `PrfAdapter.ApplyToBuilderForRegistration`, which calls `WithPrf()` ([ExtensionPipeline.cs:74-79](../../../src/WebAuthn/src/Extensions/ExtensionPipeline.cs#L74-L79), [PrfAdapter.cs:30-34](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L30-L34)). The encoder writes `"prf": {}` ([ExtensionBuilder.cs:367-379](../../../src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs#L367-L379)).
  - Repro [Fido2ExtensionAuditReproTests.cs:34-43](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2ExtensionAuditReproTests.cs#L34-L43) fails: expected `hmac-secret`, actual `prf`.
- Authentication sends raw salts under `prf`. The encoder writes `first` and `second` as given ([ExtensionBuilder.cs:385-409](../../../src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs#L385-L409)). Nothing is hashed, encrypted, or authenticated.
  - Repro [WebAuthnExtensionAuditReproTests.cs:14-25](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnExtensionAuditReproTests.cs#L14-L25) fails: expected `hmac-secret`, actual `prf`.
- Output parsing looks for the literal `prf` key and expects an `eval` map. It never reads or decrypts `hmac-secret` ([PrfAdapter.cs:89-129](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L89-L129)).
- Hardware: PRF registration failed on the 5.7.4 test key. The authenticator ignored the `prf` entry and returned no `hmac-secret` output, although its GetInfo lists `hmac-secret`. The record reads `Prf.Enabled == null`. `Enabled` is a non-nullable `bool`, so the PRF result itself was null. The assertion step was not reached ([hardware-results.md](../evidence/hardware-results.md), Phase 4).
- The SDK already has the two pieces the fix needs:
  - `PrfInput.ComputeSalt` computes the spec salt ([PrfInput.cs:62-79](../../../src/Fido2/src/Extensions/Prf/PrfInput.cs#L62-L79)).
  - `ExtensionBuilder.WithHmacSecret(protocol, sharedSecret, keyAgreement, salt1, salt2)` encrypts and authenticates the salts ([ExtensionBuilder.cs:156-204](../../../src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs#L156-L204)).
  - The WebAuthn path uses neither.
- The audit's remediation list matches the spec: derive the salt, negotiate key agreement, encrypt and authenticate, request user verification (UV), decrypt the output.

## What is wrong or imprecise

- "Not translated" is partly imprecise. The helpers exist, but the WebAuthn path does not call them. Changing the key name alone would not make PRF work.
- `prf` is not a CTAP name. CTAP 2.1, 2.2 and 2.3 define no `"prf"` extension. The SDK's own docs say otherwise, and the code does not match them:
  - `ExtensionIdentifiers.Prf` calls `prf` the WebAuthn-level name that "maps to hmac-secret at the CTAP level" ([ExtensionIdentifiers.cs:93-97](../../../src/Fido2/src/Extensions/Shared/ExtensionIdentifiers.cs#L93-L97)).
  - `src/Fido2/CLAUDE.md` says PRF "serializes using hmac-secret wire format" ([CLAUDE.md:234](../../../src/Fido2/CLAUDE.md#L234)).
- No PRF result on registration. When PRF is requested, WebAuthn sets `enabled` to false if the authenticator reports no `hmac-secret` value. The SDK returns no PRF output at all. This is part of [#26b](26b-prf-enabled-ignores-value.md).
- Two existing tests look like PRF coverage but are not:
  - `FidoPrfTests.Prf_DeterministicOutputs_SameSaltProducesSameResult` ([FidoPrfTests.cs:124-230](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoPrfTests.cs#L124-L230)) makes an assertion with no PRF input and checks no output. Its own comment (lines 192-195) says evaluation is not tested.
  - `Prf_MakeCredential_IndicatesSupport` ([FidoPrfTests.cs:105-114](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoPrfTests.cs#L105-L114)) checks the output only if one is present.
- Related, not in the audit. `WithHmacSecretMakeCredential()` ([ExtensionBuilder.cs:214-218](../../../src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs#L214-L218), encoded at [325-329](../../../src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs#L325-L329)) sends `"hmac-secret-mc": true` without `"hmac-secret": true`. CTAP 2.3 §12.8 requires both, and says the `hmac-secret-mc` input is the same map as the getAssertion input.
  - Existing tests use this builder alone ([FidoHmacSecretTests.cs:92-94](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoHmacSecretTests.cs#L92-L94), [167-169](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoHmacSecretTests.cs#L167-L169), [286-288](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoHmacSecretTests.cs#L286-L288)).
  - [ExtensionBuilderTests.cs:155-172](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Extensions/Shared/ExtensionBuilderTests.cs#L155-L172) pins the wrong encoding.
  - We did not run this on the 5.8.0 test key. That key does advertise `hmac-secret-mc` ([hardware-results.md](../evidence/hardware-results.md), GetInfo).

## Why it matters

- Who is affected: any app that uses the WebAuthn `prf` extension through this SDK on a YubiKey that lists `hmac-secret`. The SDK's module table gives 5.2+ for this ([CLAUDE.md:182](../../../src/Fido2/CLAUDE.md#L182)).
- Effect today: no PRF result. The app gets no key material and no error that explains why. Apps that derive keys from PRF fail, or need their own fallback.
- It is a functional failure, not a key leak.
- Hazard for any fix: PRF output depends on UV. CTAP 2.3 §12.7 picks `CredRandomWithUV` or `CredRandomWithoutUV` from the uv bit in the response. The same salt gives a different secret with and without UV. WebAuthn Level 3 (L3) §10.1.4 says the PRF must use the UV variant.

## Specification

Quotes are checked against the spec text. SHA-256 is the Secure Hash Algorithm with a 256-bit output. CBOR (Concise Binary Object Representation) is the binary encoding that CTAP uses. Links point to the section anchors.

> Set hmac-secret to true in the authenticator extensions input. — WebAuthn L3 §10.1.4 ([prf-extension](https://www.w3.org/TR/webauthn-3/#prf-extension))

> Let salt1 be the value of SHA-256(UTF8Encode("WebAuthn PRF") || 0x00 || ev.first). — WebAuthn L3 §10.1.4

> that PRF MUST be the one used for when user verification is performed. This overrides the UserVerificationRequirement if necessary. — WebAuthn L3 §10.1.4

> The platform sends the authenticatorMakeCredential request with the following CBOR map entry in the "extensions" field to the authenticator: "hmac-secret": true — CTAP 2.3 §12.7 ([hmac-secret](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#sctn-hmac-secret-extension))

> If uv bit is set to 1 in the response, let CredRandom be CredRandomWithUV. — CTAP 2.3 §12.7

> If uv bit is set to 0 in the response, let CredRandom be CredRandomWithoutUV. — CTAP 2.3 §12.7

> This extension is only applicable for authenticatorMakeCredential, and the hmac-secret extension MUST also be present with the value of "hmac-secret" set to true. — CTAP 2.3 §12.8 ([hmac-secret-mc](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#sctn-hmac-secret-make-cred-extension))

> This extension input is the same as the hmac secret extension’s getAssertion input — CTAP 2.3 §12.8

The phrase "WebAuthn PRF" does not occur in WebAuthn Level 2 ([W3C Recommendation, 2021-04-08](https://www.w3.org/TR/2021/REC-webauthn-2-20210408/)). PRF is a Level 3 extension only.

CTAP 2.3 §6.5.8 also uses the word "PRF", for a personal identification number (PIN) protocol construction. It is unrelated to the WebAuthn extension.

Each CTAP edition in the table is a PS (Proposed Standard) document.

| Clause | CTAP 2.1 PS | CTAP 2.2 PS | CTAP 2.3 PS | Notes |
| --- | --- | --- | --- | --- |
| `hmac-secret: true` at registration | §12.5 | §12.7 | §12.7 | Present in all three |
| getAssertion input (keyAgreement, saltEnc, saltAuth) | §12.5 | §12.7 | §12.7 | Present in all three |
| uv bit selects CredRandomWithUV or WithoutUV | §12.5 | §12.7 | §12.7 | Present in all three |
| `hmac-secret-mc` | Not defined | §12.8 | §12.8 | 5.8.0 test key advertises it; 5.7.4 test key does not |
| `"prf"` key | Not defined | Not defined | Not defined | WebAuthn name only |

## Canonical Python reference

Python is correct on the wire format. It sends `hmac-secret: true` at registration and uses the hmac-secret wire format for PRF salts. It sends that input only when a PIN/UV protocol is negotiated ([extensions.py:284](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L284)). The CTAP registration text for this input does not mention a PIN/UV protocol, so that gate is a candidate for the divergence ledger.

```python
def _prf_salt(secret):
    return sha256(b"WebAuthn PRF\0" + secret)
```

([extensions.py:169-170](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L169-L170))

```python
inputs: dict[str, Any] = {HmacSecretExtension.NAME: True}
```

([extensions.py:285](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L285))

- Python reads `hmac-secret` as a boolean, with false as the default ([extensions.py:317](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L317)).
- Python sends `hmac-secret-mc` only when the authenticator lists it, using the getAssertion input map ([extensions.py:287-309](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L287-L309)).
- Python does not add a UV override for PRF. We found no PRF handling in `fido2/client/__init__.py`. So Python does not show the UV sentence in WebAuthn §10.1.4 either.
- yubikey-manager (4ca60f7) has no hmac-secret, PRF, credBlob or previewSign code.

## Sibling SDKs (context only)

- yubikit-swift (8cd5583a): `WebAuthnPRF.swift` derives the same salt ([WebAuthnPRF.swift:128-139](https://github.com/Yubico/yubikit-swift/blob/8cd5583a489a2f7ab7a1c7669518e68b26b6e475/YubiKit/YubiKit/FIDO/WebAuthn/Extensions/WebAuthnPRF.swift#L128-L139)) and evaluates it through the CTAP2 `HmacSecret` extension.
- yubikit-android (f462685): `HmacSecretExtension.java` maps prf to hmac-secret and hmac-secret-mc ([HmacSecretExtension.java:78-80](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/fido/src/main/java/com/yubico/yubikit/fido/client/extensions/HmacSecretExtension.java#L78-L80)).

## Reproduction

- Unit: [Fido2ExtensionAuditReproTests.YESDK1609_RegistrationPrfRequestsHmacSecret](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2ExtensionAuditReproTests.cs#L34-L43). Asserts that the registration key is `hmac-secret` with value true. Result today: fails (expected `hmac-secret`, actual `prf`).
- Unit: [WebAuthnExtensionAuditReproTests.YESDK1609_AuthenticationPrfUsesEncryptedHmacSecretNotLiteralPrf](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnExtensionAuditReproTests.cs#L14-L25). Asserts that the assertion request uses `hmac-secret`, not `prf`. Result today: fails (expected `hmac-secret`, actual `prf`).
- Hardware: [WebAuthnAuditHardwareTests.YESDK1609_PrfRoundTripProducesStable32ByteResult](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/AuditV2/WebAuthnAuditHardwareTests.cs#L23-L71). Run on the 5.7.4 test key with user presence and a PIN. Result: fails at registration, with `Prf?.Enabled` null. The assertion step was not reached. The test sets UV to Preferred to keep the required-UV defect (#2) out of the way.

## Proposed fix

- Recommendation:
  1. Registration: when PRF is requested and GetInfo lists `hmac-secret`, send `hmac-secret: true` and stop sending `prf`. Change [ExtensionPipeline.cs:74-79](../../../src/WebAuthn/src/Extensions/ExtensionPipeline.cs#L74-L79) and [PrfAdapter.cs:30-34](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L30-L34).
  2. Assertion: after credential selection ([#26a](26a-prf-credential-specific-inputs-ignored.md)), compute the salts with `PrfInput.ComputeSalt`. Get the authenticator's key agreement key, call `Encapsulate`, and build the request with the existing `ExtensionBuilder.WithHmacSecret(...)` overload. Follow the steps in [FidoHmacSecretTests.cs:203-210](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoHmacSecretTests.cs#L203-L210). Require UV, by token or built-in, so the response has the uv bit set. Change [PrfAdapter.cs:39-69](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L39-L69) and [ExtensionPipeline.cs:126-131](../../../src/WebAuthn/src/Extensions/ExtensionPipeline.cs#L126-L131).
  3. Output: parse `hmac-secret`. At registration it is a boolean. At assertion it is an encrypted byte string. Decrypt it with the PIN protocol (as [FidoHmacSecretTests.cs:231](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoHmacSecretTests.cs#L231) does). Split the result into two 32-byte values for `First` and `Second`. Change [PrfAdapter.cs:74-129](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L74-L129). Zero the shared secret, the salts, and the plaintext.
  4. Optional: at registration with `eval`, on an authenticator that lists `hmac-secret-mc`, also send `hmac-secret-mc` with the hmac-secret input map. See the open questions.
  5. Fix `WithHmacSecretMakeCredential()` so it also sends `hmac-secret: true` and uses the input map (see "What is wrong").
- Options considered (group C):
  - Option A: keep `WithPrf()` and change only the key to `hmac-secret: true`. Pros: small. Cons: no evaluation path, and the eval input still has no valid encoding.
  - Option B (recommended): remove `ExtensionBuilder.WithPrf()` and `WithPrf(PrfInput)`. Do the PRF translation in the WebAuthn layer, and keep `WithHmacSecret(...)` as the one CTAP encoder. Pros: one encoder, no invented encoding, and the wording in `CLAUDE.md` becomes true. Cons: breaking for direct callers of the two methods. Both are unshipped.
  - Option C: rebuild `WithPrf(PrfInput)` on top of hmac-secret, taking the PIN protocol and shared secret as inputs. Pros: keeps the name. Cons: a second encoder next to `WithHmacSecret`.
  - We recommend B, as the verification report does (decision 4).
- API impact (application programming interface): breaking for `ExtensionBuilder.WithPrf()` and `WithPrf(PrfInput)`, which are listed only in [PublicAPI.Unshipped.txt:464-465](../../../src/Fido2/src/PublicAPI.Unshipped.txt#L464-L465). The WebAuthn call shape does not change. `PrfInput`, `PrfInput.ComputeSalt` and `ExtensionIdentifiers.Prf` stay.
- Proving test:
  - The two repros above pass.
  - Replace `Build_WithPrf_EncodesCorrectly` ([ExtensionBuilderTests.cs:135-153](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Extensions/Shared/ExtensionBuilderTests.cs#L135-L153)), which pins `"prf"`.
  - Add a deterministic test from WebAuthn §16.17.1.2 (PIN protocol 2, one input). The spec gives the seed, the platform key agreement private key, the authenticator key agreement public key, and the CredRandom. We recomputed `salt1` and `output1` from those inputs, and they match the spec. If the implementation accepts an injected key agreement key, the test can check the request and the decrypted output. Otherwise, check the salt and hash-based message authentication code (HMAC) steps separately.
  - Hardware: `YESDK1609_PrfRoundTripProducesStable32ByteResult` must pass on the 5.7.4 test key. Run it on the 5.8.0 test key as well.
  - Rewrite `FidoPrfTests.Prf_DeterministicOutputs_SameSaltProducesSameResult` so that it passes a PRF input and compares two outputs. Make the check at lines 107-114 unconditional.
- Depends on / interacts with:
  - #2 (required UV): PRF needs UV, and the required-UV request is broken. Do not send `uv` and `pinUvAuthParam` together.
  - [#26a](26a-prf-credential-specific-inputs-ignored.md): credential selection for `evalByCredential`.
  - [#26b](26b-prf-enabled-ignores-value.md): registration output parsing. Ship the registration request change with 26b. Without the request, the authenticator returns no `hmac-secret` output (CTAP 2.3 §12.7).
  - `FidoSession.cs` changed in the yubikit range. Rebase with care.
- Open questions for the maintainer:
  1. Remove `WithPrf` (we recommend this), or keep a rebuilt overload?
  2. Support registration-time evaluation through `hmac-secret-mc` on CTAP 2.2+ authenticators? WebAuthn §10.1.4 leaves this to "a future extension". CTAP 2.2 now defines it. We recommend it once the assertion path works.
  3. For `userVerification` set to `preferred` or `discouraged`, should a PRF request force UV or be rejected?
  4. For an authenticator that has `hmac-secret` but no UV method, should a PRF request be rejected? The spec's UV rule suggests yes.

## Check it yourself

- Unit tests (about 30 seconds for all AuditV2 extension repros): `dotnet toolchain.cs -- test --filter "FullyQualifiedName~YESDK1609"`. Expect both tests to fail at the branch base.
- Hardware (needs the 5.7.4 test key, a PIN, a touch, and the device owner present; PIN setup can change the device PIN state): `dotnet toolchain.cs -- test --integration --project WebAuthn --filter "FullyQualifiedName~YESDK1609_PrfRoundTripProducesStable32ByteResult&Category=RequiresUserPresence"`.
- Spec: CTAP 2.3 §12.7 and §12.8. WebAuthn L3 §10.1.4 and §16.17.1.2.
- Python: `fido2/ctap2/extensions.py`, lines 169-170 and 280-326.
- Test vector check (recomputes `salt1` and `output1` from the spec's inputs):

  ```
  python3 -c 'import hashlib,hmac; s=bytes.fromhex("576562417574686e20505246207465737420766563746f727302"); salt=hashlib.sha256(b"WebAuthn PRF\x00"+s).hexdigest(); print(salt); print(hmac.new(bytes.fromhex("437e065e723a98b2f08f39d8baf7c53ecb3c363c5e5104bdaaf5d5ca2e028154"), bytes.fromhex(salt), hashlib.sha256).hexdigest())'
  ```

  Expected: `527413ebb48293772df30f031c5ac4650c7de14bf9498671ae163447b6a772b3`, then `3c33e07d202c3b029cc21f1722767021bf27d595933b3d2b6a1b9d5dddc77fae`.
