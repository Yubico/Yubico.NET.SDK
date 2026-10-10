# #3 Direct CTAP largeBlob write has an invalid shape (YESDK-1611)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed, with a correction |
| Our severity | LOW for the test keys, which do not advertise the direct extension. MED on an authenticator that does. |
| Root cause | SDK only |
| Fix group | C (implement the direct extension properly, or remove the surface) |
| Evidence | unit test; GetInfo of both test keys (recorded in the hardware results) |
| Since the audit | Unchanged at current yubikit (`df1ec06d`). No diff in `src/Fido2/src/` between `fdfcd6fd` and `df1ec06d` except `FidoSession.cs`, which does not touch this code. |

## What the audit says

The audit says the direct Client to Authenticator Protocol (CTAP) `largeBlob` input for getAssertion has the wrong shape. The public model has no `originalSize` field, so a write cannot match CTAP 2.3 §12.4.

> The public model has Read and Write; there is no OriginalSize property.

## What is right

- The write request has the wrong shape. `LargeBlobAssertionInput.Encode` writes a Concise Binary Object Representation (CBOR) map that contains `write` alone, with no `originalSize` ([LargeBlobAssertionInput.cs#L52-L82](../../../src/Fido2/src/Extensions/LargeBlob/LargeBlobAssertionInput.cs#L52-L82)). CTAP 2.3 §12.4 accepts `write` only together with `originalSize`.
- The class has no `OriginalSize` property ([LargeBlobAssertionInput.cs#L37-L46](../../../src/Fido2/src/Extensions/LargeBlob/LargeBlobAssertionInput.cs#L37-L46)).
- `ExtensionBuilder.WithLargeBlobWrite` passes the data through without DEFLATE compression. CTAP 2.3 §12.4 says blob data is compressed ([ExtensionBuilder.cs#L107-L111](../../../src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs#L107-L111)).
- The class is public, with `init` setters. `Encode` accepts `Read = true` and `Write` together, which §12.4 forbids. The builder never sets both ([ExtensionBuilder.cs#L96-L111](../../../src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs#L96-L111)).
- The repro fails as intended. It expects a two-entry map and gets one ([Fido2LargeBlobAuditReproTests.cs#L18-L31](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs#L18-L31)).
- Neither test key advertises the direct extension. The recorded GetInfo extension lists contain no `largeBlob` entry ([hardware-results.md](../evidence/hardware-results.md), GetInfo section).

## What is wrong or imprecise

- The audit cites CTAP 2.3 only. CTAP 2.2 has the same §12.4. CTAP 2.1 has no direct `largeBlob` extension. See the version table.
- The finding is not reachable on the test keys. They use `largeBlobKey` (§12.3) and `authenticatorLargeBlobs` (§6.10). We rate it LOW for these keys.
- The repro is wrong for a compliant fix. It asserts that `write` equals the raw blob. §12.4 says blob data is compressed, so a compliant `write` is DEFLATE output. Review found this. Before any fix, change the test to assert that `write` inflates back to the blob and that `originalSize` equals `Blob.Length`. Add a test that rejects read and write together.
- The XML docs on `LargeBlobAssertionInput` describe the `largeBlobKey` flow. `Read` says the authenticator returns the largeBlobKey ([LargeBlobAssertionInput.cs#L30-L36](../../../src/Fido2/src/Extensions/LargeBlob/LargeBlobAssertionInput.cs#L30-L36)). `Write` says the authenticator returns the key that the client uses to encrypt ([#L39-L45](../../../src/Fido2/src/Extensions/LargeBlob/LargeBlobAssertionInput.cs#L39-L45)). Under §12.4, the authenticator returns the blob, or a `written` flag, in the unsigned extension outputs.
- A read-only request is valid as written. `{read: true}` matches §12.4. The result comes back in unsigned outputs, which the SDK does not decode (see #4).

## Why it matters

- Who can trigger it: an app that calls `ExtensionBuilder.WithLargeBlobWrite` or `WithLargeBlobRead` on the low-level FIDO2 API. The WebAuthn layer does not use them. Authentication-time largeBlob is not implemented there ([ExtensionPipeline.cs#L117-L123](../../../src/WebAuthn/src/Extensions/ExtensionPipeline.cs#L117-L123)).
- Preconditions: the authenticator must support the direct extension. Neither test key does, so we have no device evidence for the failure.
- Expected effect: a CTAP 2.2 or 2.3 authenticator that implements §12.4 must return CTAP2_ERR_INVALID_CBOR for this shape. We have not tested that on a device.
- Nothing under `src/` outside the tests references the direct builder. The current risk is a misleading public API, not data loss.

## Specification

> This extension is an alternative to the to authenticatorLargeBlobs command and the largeBlobKey extension for authenticators that can accept the full contents of a largeBlob in an authenticatorGetAssertion message. Authenticators MUST NOT support both extensions.

Source: CTAP 2.3 PS, §12.4 Large Blob (largeBlob). [CTAP 2.3 PS](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html).

> The major difference is that blob data is compressed in the CTAP version and the uncompressed size is stored with it.

Source: CTAP 2.3 PS, §12.4.

> If the input contains the read member and neither of write nor originalSize members, or contains the write and originalSize members but not the read member, then continue. Otherwise return CTAP2_ERR_INVALID_CBOR.

Source: CTAP 2.3 PS, §12.4, authenticatorGetAssertion extension processing.

Concise Data Definition Language (CDDL) input definition, CTAP 2.3 PS, §12.4:

```
largeblob-inputs = {
 ? read : true
 ? write : bstr
 ? originalSize : uint
}
```

| Clause | CTAP 2.1 | CTAP 2.2 | CTAP 2.3 | Notes |
| --- | --- | --- | --- | --- |
| §12.4 direct `largeBlob` extension (`largeblob-inputs`) | No | Yes | Yes | In CTAP 2.1, §12.4 is Minimum PIN Length |
| "Authenticators MUST NOT support both extensions" | No | Yes | Yes | |
| §12.3 `largeBlobKey` extension | Yes | Yes | Yes | The mechanism YubiKeys use |

## Canonical Python reference

Python does not implement the direct §12.4 extension. Its largeBlob support uses `largeBlobKey` and `authenticatorLargeBlobs` only. Python has no equivalent.

[extensions.py#L401-L406](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L401-L406)

```python
    NAME = "largeBlobKey"

    def is_supported(self, ctap):
        return self.NAME in ctap.info.extensions and ctap.info.options.get(
            "largeBlobs", False
        )
```

## Sibling SDKs (context only)

The yubikit-android WebAuthn largeBlob code reads the registration `largeBlobKey` output ([LargeBlobExtension.java#L92](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/fido/src/main/java/com/yubico/yubikit/fido/client/extensions/LargeBlobExtension.java#L92)). We did not check whether any sibling SDK implements the direct §12.4 input.

## Reproduction

- Unit: [Fido2LargeBlobAuditReproTests.YESDK1611_DirectWriteCarriesOriginalSize](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs#L18-L31). It asserts a two-entry map with `write` and `originalSize`. Today it fails: the map has one entry where two are expected.
- Also affected by any fix: [LargeBlobAssertionInputTests.LargeBlobAssertionInput_EncodesWriteCorrectly](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Extensions/LargeBlob/LargeBlobAssertionInputTests.cs#L35-L47). It asserts the current write-only encoding, so it changes under any option below.
- Hardware: not applicable. Neither test key advertises the direct extension ([hardware-results.md](../evidence/hardware-results.md)).

## Proposed fix

- Recommendation: option 2. Remove the direct-extension surface from the 2.0 public API (`ExtensionBuilder.WithLargeBlobRead`, `ExtensionBuilder.WithLargeBlobWrite`, and the public `LargeBlobAssertionInput`), or make it internal. No SDK code path uses it, no test device advertises it, and no hardware test can prove its shape.
- Options considered:
  1. Implement §12.4 properly. Compress `write` with DEFLATE, send `originalSize`, reject read and write together, and gate on GetInfo advertising the extension. Read `written` and `blob` from the unsigned outputs (this needs #4). Pros: spec-complete. Cons: no device to test on; more code.
  2. Remove the surface, or make it internal (recommended). Pros: removes a misleading API; small change. Cons: breaking for any caller of the unreleased API; the encoder must be rebuilt if §12.4 is needed later.
  3. Keep the methods, but throw `NotSupported` until GetInfo advertises the extension. Pros: keeps the API shape. Cons: the shape stays unverified; a half-built path remains.
- API impact: option 1 is additive (`OriginalSize`) with stricter validation. Option 2 is breaking: it removes three public members. Option 3 is behavioral.
- Proving test: option 1: the corrected `YESDK1611_DirectWriteCarriesOriginalSize`, plus a test that rejects read and write together. Option 2: no behavioral test applies; the build and an API review prove the removal.
- Depends on or interacts with: #4 (needed to read the direct outputs under option 1).
- Open question for the maintainer: keep the direct extension for a future device (option 1), or remove it for 2.0 (option 2)? We recommend option 2.

## Check it yourself

- `dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~YESDK1611"` (fails today).
- CTAP 2.3 PS §12.4: search for `largeblob-inputs` and for "Authenticators MUST NOT support both extensions". Both strings also appear in CTAP 2.2 §12.4. Neither appears in CTAP 2.1 PS.
- [hardware-results.md](../evidence/hardware-results.md): the GetInfo lines list no `largeBlob` extension for either test key.
