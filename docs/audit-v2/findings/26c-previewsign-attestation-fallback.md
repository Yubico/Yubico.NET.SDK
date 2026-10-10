# 26c Missing previewSign attestation substitutes the authentication key (YESDK-1634)

| | |
| --- | --- |
| Audit severity | MED (one audit row, #26; sub-item c) |
| Our verdict | Confirmed |
| Our severity | HIGH. The failure is silent, and it stores the wrong public key and key handle. |
| Root cause | SDK only |
| Fix group | A |
| Evidence | unit test (no hardware) |
| Since the audit | Unchanged at current yubikit (`PreviewSignAdapter.cs`, `GeneratedSigningKey.cs`, `ExtensionPipeline.cs`). |

## What the audit says

The audit says the previewSign registration result can name the wrong key. When the signing-key attestation is missing, the adapter uses the ordinary authentication credential. Its key claim:

> falls back to the ordinary authentication credential’s ID and public key when the embedded signing-key attestation is missing.

previewSign is a draft Yubico WebAuthn (Web Authentication) extension. on the authenticator, separate from the credential key pair. The relying party (RP) can then ask that key to sign data. The "embedded signing-key attestation" is the attestation object for that second key pair.

## What is right

- The fallback works as described. When the unsigned output is missing, `PreviewSignAdapter.ParseRegistrationOutput` builds a `GeneratedSigningKey` from the authentication credential's ID and public key. It sets `AttestationObject` to null ([PreviewSignAdapter.cs:226-240](../../../src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs#L226-L240)).
  - Repro [WebAuthnExtensionAuditReproTests.cs:58-95](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnExtensionAuditReproTests.cs#L58-L95) fails with "No exception was thrown".
- The fallback is documented, so it is deliberate. The method remarks say: "If unsigned extension outputs are missing, the method builds the generated key from the response authenticator data's attested credential data." ([PreviewSignAdapter.cs:169-174](../../../src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs#L169-L174)). Commit `0ae08cf3` says it "Falls back to building GeneratedSigningKey from authData attested credential data if unsignedExtensionOutputs is missing (Swift fallback pattern)."
- The correct path decodes the inner attestation object through `PreviewSignCbor.DecodeUnsignedRegistrationOutput` ([PreviewSignCbor.cs:310-338](../../../src/Fido2/src/Extensions/PreviewSign/PreviewSignCbor.cs#L310-L338)). That path is not affected.

## What is wrong or imprecise

- The fallback is not a fallback. The draft requires the attestation object. It takes the key handle and public key from the inner attestation, not from the outer credential. So the result names the wrong key material.
- The code contradicts the software development kit's (SDK) own type documentation. `GeneratedSigningKey` says "The generated signing key is represented by an embedded attestation object whose attested credential data contains the signing key handle and public key." ([GeneratedSigningKey.cs:28-29](../../../src/WebAuthn/src/Extensions/PreviewSign/GeneratedSigningKey.cs#L28-L29)).
- The "Swift fallback pattern" cited in commit `0ae08cf3` does not match the Swift code at the pinned SHA. Swift throws when the attestation object is missing (see below). The stated precedent is not supported.
- Only `CborContentException` is caught around this call ([ExtensionPipeline.cs:256-262](../../../src/WebAuthn/src/Extensions/ExtensionPipeline.cs#L256-L262)). Any other exception, such as `WebAuthnClientError` with `InvalidState`, propagates and fails the registration. The fix depends on that.
- No previewSign hardware run is recorded in this audit, so we do not know whether any YubiKey omits the unsigned output.

## Why it matters

- Who is affected: RPs that request previewSign key generation at registration.
- Effect: the RP can store the authentication credential's ID and public key as its signing key. The call returns no error. Later signing uses the wrong key handle.
- The result looks valid unless the RP checks for a missing attestation object. The SDK's result carries `AttestationObject` as null, so a careful RP can detect it.
- Trigger: a registration where the authenticator's unsigned output is missing. The trigger is not known for YubiKeys. The code accepts it silently.
- Severity is HIGH because the failure is silent and the stored key is wrong.

## Specification

Yubico previewSign draft, version 4, §10.2.1 ([draft](https://yubicolabs.github.io/webauthn-sign-extension/4/)). This is a draft, not a ratified standard. The SDK's contract follows it. Its attestation objects are CBOR (Concise Binary Object Representation) data.

> Let origAttObj be unsignedExtOutputs["previewSign"][att-obj] parsed as a CBOR map. — §10.2.1, registration processing

> keyHandle: An ArrayBuffer containing a copy of innerAuthData.attestedCredentialData.credentialId. — §10.2.1, registration processing

> required ArrayBuffer attestationObject; — §10.2.1, AuthenticationExtensionsSignGeneratedKey

> att-obj => bstr .cbor attObj, ; Attestation object for signing key pair — §10.2.1, CDDL (Concise Data Definition Language)

> Note that unsigned extension output is only present in registration ceremonies. — §10.2.1

| Clause | Yubico previewSign draft v4 | Notes |
| --- | --- | --- |
| Key handle and public key source | §10.2.1, registration | From the inner attestation, not the outer credential |
| `attestationObject` member | §10.2.1, dictionary | Required |
| Attestation object key in unsigned output | §10.2.1, CDDL (`att-obj = 7`) | Integer key 7 |
| Unsigned output present | §10.2.1 | Registration only |

## Canonical Python reference

Python is correct here. It never substitutes the authentication credential. It requires key 7, parses the inner attestation, and takes the key handle and public key from the inner credential data.

```python
att_obj_bytes = response.unsigned_extension_outputs[
    PreviewSignExtension.NAME
][7]
```

([extensions.py:748-750](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L748-L750))

- If key 7 is missing, python-fido2 raises a `KeyError`. That is not a typed error, but it does not substitute a key.
- Commit `5bc9d3a` ("Fix previewSign att_obj formatting") changed this code to build the attestation object from the inner response. It did not add a fallback.

## Sibling SDKs (context only)

- yubikit-swift (8cd5583a): `PreviewSign.swift` throws `responseParseError` when the attestation object is missing ([PreviewSign.swift:144-159](https://github.com/Yubico/yubikit-swift/blob/8cd5583a489a2f7ab7a1c7669518e68b26b6e475/YubiKit/YubiKit/FIDO/CTAP/Extensions/PreviewSign.swift#L144-L159)). It does not fall back. This does not match the "Swift fallback pattern" in commit `0ae08cf3`.
- Legacy .NET SDK v1 (941874e9): returns null when the unsigned value is absent ([PreviewSignExtensionMethods.cs:42-46](https://github.com/Yubico/Yubico.NET.SDK/blob/941874e91a77616f5d7063f2952a0291c7e7c8f8/Yubico.YubiKey/src/Yubico/YubiKey/Fido2/PreviewSignExtensionMethods.cs#L42-L46)). It does not substitute a key.

## Reproduction

- Unit: [WebAuthnExtensionAuditReproTests.YESDK1634_PreviewSignMissingInnerAttestationDoesNotSubstituteCredentialKey](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnExtensionAuditReproTests.cs#L58-L95). Expects a `WebAuthnClientError` when the unsigned output is absent. Result today: fails ("No exception was thrown").
- Hardware: not applicable. No previewSign hardware run is recorded.

## Proposed fix

- Recommendation: in `PreviewSignAdapter.ParseRegistrationOutput`, delete the fallback at [PreviewSignAdapter.cs:226-240](../../../src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs#L226-L240). When previewSign output is present but the unsigned attestation is missing, throw `WebAuthnClientError` with `WebAuthnClientErrorCode.InvalidState`. The method already uses that code at lines 229-231. Update the remarks at [lines 169-174](../../../src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs#L169-L174).
- Options considered (group A):
  - Option A (recommended): throw `InvalidState`. The registration fails loudly, and the RP never gets a wrong key.
  - Option B: return no previewSign output (null). No exception, but the RP gets no key and no reason.
  - Option C: keep the fallback but clear the key handle. Still wrong, and still silent. Rejected.
- API impact (application programming interface): none for Option A. The record keeps its shape. Optional, separate follow-up: make `GeneratedSigningKey.AttestationObject` non-nullable ([WebAuthn PublicAPI.Unshipped.txt:236-237](../../../src/WebAuthn/src/PublicAPI.Unshipped.txt#L236-L237)). That is a breaking change for an unshipped member.
- Proving test:
  - `YESDK1634_PreviewSignMissingInnerAttestationDoesNotSubstituteCredentialKey` passes after the fix.
  - Add a test that the whole registration fails, not only the adapter. The exception must propagate through `ExtensionPipeline`.
  - Keep the valid-output path covered. Its decoding is in `PreviewSignCbor.DecodeUnsignedRegistrationOutput`.
- Depends on / interacts with: nothing in the audit. Independent of [#1](01-prf-not-translated-to-hmac-secret.md) and [26d](26d-previewsign-first-entry.md).
- Open questions for the maintainer: throw `InvalidState` (our recommendation, also the verification report's), or omit the result?

## Check it yourself

- Unit: `dotnet toolchain.cs -- test --project WebAuthn --filter "FullyQualifiedName~YESDK1634_PreviewSignMissingInner"`. Fails at the branch base.
- Spec: Yubico previewSign draft v4, §10.2.1 ([draft](https://yubicolabs.github.io/webauthn-sign-extension/4/)).
- Python: `fido2/ctap2/extensions.py`, lines 702-712 and 743-769. Commit `5bc9d3a` changed lines 689-693 and 748-769.
- Legacy and Swift: the two links in "Sibling SDKs".
