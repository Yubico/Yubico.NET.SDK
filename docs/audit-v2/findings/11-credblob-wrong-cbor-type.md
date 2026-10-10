# #11 credBlob retrieval uses the wrong CBOR type (YESDK-1619)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed |
| Our severity | MED |
| Root cause | SDK only |
| Fix group | B (small interface decision: name and shape of the get-side input and output) |
| Evidence | unit test; hardware (5.7.4 test key, user present) |
| Since the audit | Unchanged at current yubikit for the builder, encoder, pipeline and adapter files. In this branch, `FidoCredBlobTests.cs` changed: the blob assertions are now unconditional. |

## What the audit says

The audit says the get-side credBlob request is non-conforming. credBlob is a CTAP (Client to Authenticator Protocol) extension that stores a small blob with a credential. CTAP is the protocol between the platform and the authenticator. The software development kit (SDK) sends an empty CBOR (Concise Binary Object Representation) byte string, `h''`. The spec requires a boolean. Its key claim:

> The SDK’s request is nonconforming, but a YubiKey tolerates it.

## What is right

- The get-side request is wrong, as the audit describes. `ExtensionBuilder.WithCredBlob` always writes a byte string ([ExtensionBuilder.cs:75-79](../../../src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs#L75-L79), encoded at [307-311](../../../src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs#L307-L311)). The integration test passes an empty memory block, which encodes as `h''` ([FidoCredBlobTests.cs:129-132](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoCredBlobTests.cs#L129-L132)). The request encoder copies that value unchanged under key 4 ([FidoSessionRequestEncoding.cs:80-83](../../../src/Fido2/src/Cbor/FidoSessionRequestEncoding.cs#L80-L83)).
  - Repro [Fido2ExtensionAuditReproTests.cs:10-30](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2ExtensionAuditReproTests.cs#L10-L30) fails with "next CBOR data item is of major type '2'".
- WebAuthn (Web Authentication) callers cannot ask for the blob. `AuthenticationExtensionInputs` has no member for it ([WebAuthnExtensionInputs.cs:38-47](../../../src/WebAuthn/src/Extensions/WebAuthnExtensionInputs.cs#L38-L47)).
  - Repro [WebAuthnExtensionAuditReproTests.cs:97-104](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnExtensionAuditReproTests.cs#L97-L104) fails: the `GetCredBlob` property does not exist.
- The output side can already read a blob. The pipeline parses credBlob on every assertion ([ExtensionPipeline.cs:292-305](../../../src/WebAuthn/src/Extensions/ExtensionPipeline.cs#L292-L305)).
- Hardware: the store-and-retrieve test passed on the 5.7.4 test key with user presence. The test now asserts the output unconditionally ([FidoCredBlobTests.cs:147-152](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoCredBlobTests.cs#L147-L152)). The key accepted `"credBlob": h''` and returned the stored blob ([hardware-results.md](../evidence/hardware-results.md), Phase 4).
- The creation side is correct. `WithCredBlob(blob)` writes a byte string at makeCredential, as the spec requires. The unit test for the empty creation case, `WithCredBlob_EmptyBlob_EncodesEmptyByteString` ([ExtensionBuilderTests.cs:304-321](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Extensions/Shared/ExtensionBuilderTests.cs#L304-L321)), stays valid.

## What is wrong or imprecise

- The wire value is wrong, but the 5.7.4 test key accepts it, so the hardware run shows no failure. "A YubiKey tolerates it" is confirmed for the 5.7.4 test key only. We did not run the 5.8.0 test key for credBlob.
- Before this branch, the integration test checked the output only when one was present. A dropped extension would have passed. The unconditional assertions close that gap, but they have not yet been run with the boolean request.
- The WebAuthn Level 3 spec does not define credBlob. It lists credBlob only as a WebDriver capability and defers to the CTAP definition. The client-side `getCredBlob` input comes from CTAP, so the WebAuthn-facing names should follow CTAP.
- Section numbers: the verification report cites §12.2. That is the credBlob section. The input definitions sit under the §12.2.1 heading in CTAP 2.3, so cite §12.2.1 for the input.

## Why it matters

- Who is affected: apps that need the blob back at assertion time. Today they must use the low-level builder with the wrong value. WebAuthn callers cannot request the blob at all.
- Effect on YubiKeys: none seen. The 5.7.4 test key accepts `h''`.
- Effect on other authenticators: an authenticator that checks the CBOR type strictly may reject the request or return no blob. We did not test this.

## Specification

> get() : A boolean value to indicate that this extension is requested by the Relying Party. — CTAP 2.3 §12.2.1 ([getCredBlob](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#dom-authenticationextensionsclientinputs-getcredblob))

> partial dictionary AuthenticationExtensionsClientInputs { boolean getCredBlob; }; — CTAP 2.3 §12.2.1

> The platform sends the authenticatorGetAssertion request with the following CBOR map entry in the "extensions" field to the authenticator: "credBlob":true — CTAP 2.3 §12.2.1

> "credBlob": Byte String containing the credBlob value — CTAP 2.3 §12.2.1 (authenticatorMakeCredential input)

Each CTAP edition in the table is a PS (Proposed Standard) document. In all three editions, the input definitions sit under the §12.2.1 "Feature detection" heading.

| Clause | CTAP 2.1 PS | CTAP 2.2 PS | CTAP 2.3 PS | Notes |
| --- | --- | --- | --- | --- |
| makeCredential input: byte string | §12.2.1 | §12.2.1 | §12.2.1 | Present in all three |
| getAssertion input: `"credBlob": true` | §12.2.1 | §12.2.1 | §12.2.1 | Present in all three |
| Client input `getCredBlob` (boolean) | §12.2.1 | §12.2.1 | §12.2.1 | The "get()" text is present in all three |
| WebAuthn Level 3 | Not defined | Not defined | Not defined | Only a WebDriver capability mention |

The CTAP 2.1 document is the 2021 Proposed Standard with the 2022 errata.

## Canonical Python reference

Python is correct here. It sends a non-empty blob that fits maxCredBlobLength at creation, and boolean true for retrieval, only when the caller sets `getCredBlob` to true. Its creation path omits an empty blob. The client rule in CTAP 2.3 §12.2.1 passes any blob whose size is at most maxCredBlobLength, and an empty blob qualifies. That creation-side gap is a candidate for the divergence ledger.

```python
def get_assertion(self, ctap, options, pin_protocol):
    inputs = options.extensions or {}
    if self.is_supported(ctap) and inputs.get("getCredBlob") is True:
        return AuthenticationExtensionProcessor(inputs={self.NAME: True})
```

([extensions.py:495-498](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L495-L498))

Python has no divergence on this item.

## Sibling SDKs (context only)

- yubikit-swift (8cd5583a): the credBlob test requests retrieval with `getCredBlob: true` ([CredBlobTests.swift:58](https://github.com/Yubico/yubikit-swift/blob/8cd5583a489a2f7ab7a1c7669518e68b26b6e475/FullStackTests/Tests/WebAuthn/Extensions/CredBlobTests.swift#L58)).
- yubikit-android (f462685): `CredBlobExtension.java` reads `getCredBlob` as a boolean ([CredBlobExtension.java:80](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/fido/src/main/java/com/yubico/yubikit/fido/client/extensions/CredBlobExtension.java#L80)).

## Reproduction

- Unit: [Fido2ExtensionAuditReproTests.YESDK1619_GetAssertionCredBlobUsesBooleanTrue](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2ExtensionAuditReproTests.cs#L10-L30). Asserts that the getAssertion `credBlob` value is boolean true. Result today: fails ("next CBOR data item is of major type '2'").
- Unit: [WebAuthnExtensionAuditReproTests.YESDK1619_WebAuthnOffersGetCredBlobClientInput](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnExtensionAuditReproTests.cs#L97-L104). Asserts that `AuthenticationExtensionInputs` has a `GetCredBlob` property. Result today: fails (property absent).
- Hardware: [FidoCredBlobTests.CredBlob_StoreAndRetrieve_ReturnsStoredData](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoCredBlobTests.cs#L34-L168). Run on the 5.7.4 test key with user presence, with the request as it stood (`h''`). Result: passed, and the blob came back. Not yet run with the boolean request.

## Proposed fix

- Recommendation:
  1. Add `ExtensionBuilder.WithGetCredBlob()`, which writes `"credBlob": true`. Keep `WithCredBlob(ReadOnlyMemory<byte>)` for makeCredential only.
  2. Add an optional `GetCredBlob` parameter to WebAuthn `AuthenticationExtensionInputs` ([WebAuthnExtensionInputs.cs:44-47](../../../src/WebAuthn/src/Extensions/WebAuthnExtensionInputs.cs#L44-L47)). Call `WithGetCredBlob()` from `ExtensionPipeline.BuildAuthenticationExtensionsCbor` ([ExtensionPipeline.cs:105-146](../../../src/WebAuthn/src/Extensions/ExtensionPipeline.cs#L105-L146)).
  3. Keep the existing output parsing ([ExtensionPipeline.cs:292-305](../../../src/WebAuthn/src/Extensions/ExtensionPipeline.cs#L292-L305), [CredBlobAdapter.cs:51-61](../../../src/WebAuthn/src/Extensions/Adapters/CredBlobAdapter.cs#L51-L61)). Expose the result to WebAuthn callers (see open questions).
  4. Change [FidoCredBlobTests.cs:131](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoCredBlobTests.cs#L131) to `WithGetCredBlob()`. Keep the unconditional assertions. Re-run on the 5.7.4 test key.
- Options considered (group B):
  - Option A: keep `WithCredBlob(Empty)` for retrieval and document it. Pros: no change. Cons: the wrong value stays on the wire.
  - Option B (recommended): add `WithGetCredBlob()` and `GetCredBlob`, and keep `WithCredBlob(bytes)` for creation. Pros: additive, and one method per CTAP operation. Cons: two methods to learn.
  - Option C: make `WithCredBlob` accept either bytes or a get flag. Pros: one method. Cons: mixes two operations, which the audit calls out. Breaking.
- API impact (application programming interface): additive. A new builder method and a new optional input member. The output choice (see open questions) may add one more member. The unshipped `AuthenticationExtensionInputs` constructor changes ([WebAuthn PublicAPI.Unshipped.txt:145](../../../src/WebAuthn/src/PublicAPI.Unshipped.txt#L145)).
- Proving test:
  - Change `YESDK1619_GetAssertionCredBlobUsesBooleanTrue` to call `WithGetCredBlob()`, as the review correction in the verification report requires.
  - Keep `WithCredBlob_EmptyBlob_EncodesEmptyByteString` as the creation-side control. It must still pass.
  - `YESDK1619_WebAuthnOffersGetCredBlobClientInput` passes.
  - Hardware: `CredBlob_StoreAndRetrieve_ReturnsStoredData` passes on the 5.7.4 test key with the boolean request.
- Depends on / interacts with: no dependency in the audit. The change touches the same builder and input record as [#1](01-prf-not-translated-to-hmac-secret.md) and [#26a](26a-prf-credential-specific-inputs-ignored.md). Keep it in its own commit.
- Open questions for the maintainer:
  1. For the WebAuthn output, keep the name `CredBlob` for the assertion result, or add a `GetCredBlob` member? We recommend an additive `GetCredBlob` member, so create and get outputs stay separate.
  2. Keep the input as a bool, or accept an options type? A bool matches CTAP.

## Check it yourself

- Unit: `dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~YESDK1619"`, and the same with `--project WebAuthn`. Both fail at the branch base.
- Hardware (5.7.4 test key, user presence): `dotnet toolchain.cs -- test --integration --project Fido2 --filter "FullyQualifiedName~CredBlob_StoreAndRetrieve_ReturnsStoredData&Category=RequiresUserPresence"`.
- Spec: CTAP 2.3 §12.2 and §12.2.1 ([credBlob](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#sctn-credBlob-extension)).
- Python: `fido2/ctap2/extensions.py`, lines 487-498.
