# #4 Unsigned getAssertion extension outputs are not decoded (YESDK-1612)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed, with a correction |
| Our severity | LOW for the test keys, because the direct extension that produces this output is not advertised. MED on an authenticator that advertises it. |
| Root cause | SDK and Python share it |
| Fix group | A (no-brainer) |
| Evidence | unit test; GetInfo of both test keys (recorded in the hardware results) |
| Since the audit | Unchanged at current yubikit (`df1ec06d`). No diff in `src/Fido2/src/` between `fdfcd6fd` and `df1ec06d` except `FidoSession.cs`, which does not touch this code. |

## What the audit says

The audit says the getAssertion decoder skips the `unsignedExtensionOutputs` map (key 0x08) of the Client to Authenticator Protocol (CTAP) response. A direct largeBlob result, such as `written`, can therefore never reach the caller.

> GetAssertionResponse.Decode() has cases only for keys 0x01–0x07.

> Therefore, the SDK can send a largeBlob operation but can never return its defined result to the caller.

## What is right

- The decoder skips key 0x08. Its switch covers keys 1 to 7. The `default` branch reads and discards any other key ([GetAssertionResponse.cs#L146-L177](../../../src/Fido2/src/Credentials/GetAssertionResponse.cs#L146-L177)).
- The response has no unsigned-outputs property. Its only extension map is the signed one from the authenticator data ([GetAssertionResponse.cs#L87-L90](../../../src/Fido2/src/Credentials/GetAssertionResponse.cs#L87-L90)).
- The makeCredential side already decodes key 0x06 into `UnsignedExtensionOutputs` ([MakeCredentialResponse.cs#L78-L87](../../../src/Fido2/src/Credentials/MakeCredentialResponse.cs#L78-L87), [#L188-L204](../../../src/Fido2/src/Credentials/MakeCredentialResponse.cs#L188-L204)). The gap is getAssertion only.
- The repro fails as intended. `YESDK1612_GetAssertionPreservesUnsignedLargeBlobOutput` decodes a synthetic response with key 0x08 and finds no `UnsignedExtensionOutputs` property ([Fido2LargeBlobAuditReproTests.cs#L35-L68](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs#L35-L68)).
- The audit's example shape matches CTAP 2.3 §12.4. A `written` member under `largeBlob` is the output that the spec defines for a direct write.

## What is wrong or imprecise

- The audit's claim holds only for the direct extension. The `largeBlobKey` path returns its key in response key 0x07 on getAssertion, and the SDK decodes that key ([GetAssertionResponse.cs#L172-L174](../../../src/Fido2/src/Credentials/GetAssertionResponse.cs#L172-L174)).
- Not reachable on the test keys. Neither advertises the direct extension (see #3), so no largeBlob output under key 0x08 is expected from either. We rate it LOW for these keys.
- The audit omits the empty-map rule. CTAP 2.3 §6.2 says: "Clients MUST treat an empty map the same as an omitted field." The new decoder must apply it. The same rule applies to key 0x06. Today `MakeCredentialResponse` returns an empty dictionary for an empty key 0x06 map, not null ([MakeCredentialResponse.cs#L188-L190](../../../src/Fido2/src/Credentials/MakeCredentialResponse.cs#L188-L190)). The only code that reads it, [PreviewSignCbor.cs#L478](../../../src/Fido2/src/Extensions/PreviewSign/PreviewSignCbor.cs#L478), treats empty and null the same way, so the difference is cosmetic today.
- Correction to our earlier notes. They said python-fido2 models top-level unsigned extension outputs "on both attestation and assertion responses". At the pinned commit, only `AttestationResponse` has that field ([base.py#L148-L166](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/base.py#L148-L166), field at line 165). `AssertionResponse` has no such field ([base.py#L169-L187](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/base.py#L169-L187)). Its decoder reads only declared fields ([utils.py#L292-L297](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/utils.py#L292-L297)), so key 0x08 is ignored. This changes the Python verdict below.

## Why it matters

- Who: an app that uses the low-level FIDO2 API with the direct largeBlob read or write (`ExtensionBuilder.WithLargeBlobRead` or `WithLargeBlobWrite`).
- Preconditions: the authenticator must support the direct extension. No test key does.
- Effect: the app cannot tell whether a direct write was stored, and cannot read the blob. Both results come back only in key 0x08, and the SDK drops them.

## Specification

> A map, keyed by extension identifiers, to unsigned outputs of extensions, if any.

> Clients MUST treat an empty map the same as an omitted field.

Source: CTAP 2.3 PS, §6.2 (authenticatorGetAssertion response, key 0x08). The same two sentences appear for the makeCredential key 0x06 in §6.1.

> Add an element to the unsigned extension outputs for this extension that conforms to largeblob-outputs, below, and which contains a written member equal to the value of the written variable.

Source: CTAP 2.3 PS, §12.4, authenticatorGetAssertion extension processing (write path).

| Clause | CTAP 2.1 | CTAP 2.2 | CTAP 2.3 | Notes |
| --- | --- | --- | --- | --- |
| getAssertion response key 0x08, `unsignedExtensionOutputs` | No | Yes | Yes | |
| makeCredential response key 0x06, `unsignedExtensionOutputs` | No | Yes | Yes | The SDK already decodes this key |
| "Clients MUST treat an empty map the same as an omitted field" | No | Yes | Yes | Applies to keys 0x06 and 0x08 |
| §12.4 largeBlob outputs (`written`, `blob`, `originalSize`) | No | Yes | Yes | Defined only for the direct extension |

## Canonical Python reference

Python has the same gap for getAssertion, so it is a shared gap and a candidate for the divergence ledger. python-fido2 models key 0x06 on `AttestationResponse` only. `AssertionResponse` has no unsigned-outputs field, so key 0x08 is silently dropped.

[utils.py#L292-L297](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/utils.py#L292-L297) (decoder reads declared fields only):

```python
    def _parse_from_dict(cls: type[Self], data: Mapping[_T, Any]) -> Self:
        kwargs = {}
        hints = get_type_hints(cls)
        for f in fields(cls):
            key = cls._get_field_key(f)
            value = data.get(key)
```

Is the omission deliberate? The field was added in one commit (`01d5ac5`, 2025-05-06, "Add unsigned_extension_outputs to response"). That commit adds it to `AttestationResponse` only and gives no reason. We found no evidence that the omission is deliberate.

## Sibling SDKs (context only)

Not checked for key 0x08.

## Reproduction

- Unit: [Fido2LargeBlobAuditReproTests.YESDK1612_GetAssertionPreservesUnsignedLargeBlobOutput](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2LargeBlobAuditReproTests.cs#L35-L68). Today it fails: the response has no `UnsignedExtensionOutputs` property.
- Add for the fix: an empty key 0x08 map must produce a null `UnsignedExtensionOutputs`. This test is not in the repo yet.
- Hardware: not applicable. Neither test key advertises the direct extension ([hardware-results.md](../evidence/hardware-results.md)).

## Proposed fix

- Recommendation: add `UnsignedExtensionOutputs` (type `IReadOnlyDictionary<string, ReadOnlyMemory<byte>>?`) to `GetAssertionResponse`. Decode key 0x08 the same way key 0x06 is decoded in `MakeCredentialResponse` ([MakeCredentialResponse.cs#L188-L204](../../../src/Fido2/src/Credentials/MakeCredentialResponse.cs#L188-L204)). Add `case 8:` to the switch in `GetAssertionResponse.Decode` ([GetAssertionResponse.cs#L146-L177](../../../src/Fido2/src/Credentials/GetAssertionResponse.cs#L146-L177)). Return null for an empty map. Update the class docs ([GetAssertionResponse.cs#L19-L33](../../../src/Fido2/src/Credentials/GetAssertionResponse.cs#L19-L33)) to list key 0x08.
- Apply the same empty-as-null rule to key 0x06 ([MakeCredentialResponse.cs#L188-L204](../../../src/Fido2/src/Credentials/MakeCredentialResponse.cs#L188-L204)).
- Options considered:
  1. Raw map, with the same shape as key 0x06 (recommended now). Pros: smallest change; matches the existing type. Cons: callers parse the largeBlob CBOR themselves.
  2. Typed direct-largeBlob result (`written`, `blob`, `originalSize`). Pros: easier to use. Cons: only useful once #3 chooses option 1. Defer.
- API impact: additive for key 0x08. Behavioral for key 0x06: an empty map changes from an empty dictionary to null. Callers that read the property without a null check would fail. Our only reader handles null ([PreviewSignCbor.cs#L478](../../../src/Fido2/src/Extensions/PreviewSign/PreviewSignCbor.cs#L478)).
- Proving test: `YESDK1612_GetAssertionPreservesUnsignedLargeBlobOutput` (existing; it should pass after the fix). Add `YESDK1612_EmptyUnsignedOutputsAreAbsent` for keys 0x06 and 0x08.
- Depends on or interacts with: #3 (only the direct extension produces a largeBlob output under key 0x08).
- Open questions: expose raw values only (recommended), or also a typed result? Accept the null change for key 0x06?

## Check it yourself

- `dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~YESDK1612"` (fails today).
- CTAP 2.3 PS §6.2: search for `unsignedExtensionOutputs (0x08)` and for "Clients MUST treat an empty map". Section 6.1 has the same rule for key 0x06.
- python-fido2 at `5bc9d3a1c8c34a3c4ca408366e630b620db47faa`: read `fido2/ctap2/base.py` lines 148-187 and `fido2/utils.py` lines 292-297.
