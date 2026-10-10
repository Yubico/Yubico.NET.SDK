# #5 WebAuthn largeBlob supported result ignores largeBlobKey (YESDK-1613)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed |
| Our severity | MED. A successfully enabled feature is reported as unsupported. The same code also has a failure mode the audit does not mention: a largeBlob registration that does not send `rk: true` is expected to fail. Today that is every `residentKey` value except `required`. That is not run on hardware. |
| Root cause | SDK only. python-fido2 has a related request issue (see the Python section). |
| Fix group | A (no-brainer) |
| Evidence | unit test (static trace). Hardware not run (optional). |
| Since the audit | Unchanged at current yubikit (`df1ec06d`). No diff in `src/WebAuthn/src/` or in `src/Fido2/src/Credentials/` between `fdfcd6fd` and `df1ec06d`. |

## What the audit says

The audit says the SDK requests the CTAP `largeBlobKey` and decodes the key, but the WebAuthn `largeBlob.supported` result never sees that key.

> The SDK requests CTAP `largeBlobKey` and correctly decodes the returned key, but does not pass that key to the code that constructs the WebAuthn `largeBlob.supported` result

## What is right

- The SDK decodes Client to Authenticator Protocol (CTAP) response key 0x05 into `LargeBlobKey` ([MakeCredentialResponse.cs#L185-L187](../../../src/Fido2/src/Credentials/MakeCredentialResponse.cs#L185-L187); property at [#L68-L71](../../../src/Fido2/src/Credentials/MakeCredentialResponse.cs#L68-L71)).
- The registration call does not pass that key on. It passes the authenticator data and the unsigned outputs, but not `LargeBlobKey` ([WebAuthnClient.Registration.cs#L360-L364](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L360-L364)).
- The adapter looks for `largeBlobKey` in the signed authenticator-data extension map ([LargeBlobAdapter.cs#L45-L57](../../../src/WebAuthn/src/Extensions/Adapters/LargeBlobAdapter.cs#L45-L57), lookup at line 49). CTAP 2.3 §12.3 places the key in response key 0x05, "i.e., not in the extensions field of the authenticator data". The lookup looks in the wrong place.
- The repro fails as intended. `YESDK1613_LargeBlobKeyFromCtapResponseReportsSupported` expects `Supported` to be true for a response with a 32-byte key, and gets false ([WebAuthnLargeBlobAuditReproTests.cs#L16-L52](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnLargeBlobAuditReproTests.cs#L16-L52)).

## What is wrong or imprecise

- Passing the key through is not enough. The adapter always requests `largeBlobKey: true` ([LargeBlobAdapter.cs#L38-L39](../../../src/WebAuthn/src/Extensions/Adapters/LargeBlobAdapter.cs#L38-L39)). The registration builder sets `rk` only for `residentKey: required` ([WebAuthnClient.Registration.cs#L293-L295](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L293-L295)). CTAP §12.3 says that an authenticator must return CTAP2_ERR_INVALID_OPTION unless the options map `rk` to true. So a largeBlob registration without `rk: true` should fail on a conforming authenticator. Today that includes `residentKey` preferred and discouraged. Review found this. We have not run it on hardware.
- For `support` preferred, WebAuthn does not require an error. WebAuthn L3 §10.1.5 sets `supported` to false when the credential cannot store large blobs. The correct result is `supported: false`, not a failed registration.
- GetInfo is not checked. CTAP §12.3 says platforms detect support from GetInfo, using both the `largeBlobKey` extension and the `largeBlobs` option. The SDK requests the key without checking either.
- `support: required` throws `NotSupported` ([LargeBlobAdapter.cs#L31-L36](../../../src/WebAuthn/src/Extensions/Adapters/LargeBlobAdapter.cs#L31-L36)). The audit does not mention this. It belongs in the same change.

## Why it matters

- Who: any app that registers a WebAuthn credential with the `largeBlob` input.
- Preconditions for the misreport: an authenticator that stores large blobs. Preconditions for the failure: a `largeBlob` input on a request without `rk: true`. Today the registration builder sends `rk` only for `residentKey: required`, so every other value is affected.
- Effects: an app sees `supported: false` on a credential that can store blobs, and may skip the feature. Without `rk: true`, the registration itself is expected to fail. Today that includes `residentKey` preferred and discouraged. This is based on the CTAP rule and the code path, and has not been run on a device.

## Specification

> Set the value of largeBlobKey (0x05) in the authenticatorMakeCredential response structure (i.e., not in the extensions field of the authenticator data) to the value of the generated largeBlobKey.

Source: CTAP 2.3 PS, §12.3 Large Blob Key (largeBlobKey), authenticatorMakeCredential processing.

> If the options field of the authenticatorMakeCredential request does not map rk to true, return CTAP2_ERR_INVALID_OPTION.

Source: CTAP 2.3 PS, §12.3, authenticatorMakeCredential processing.

> Platforms can detect support for this extension by checking for all of the following in the authenticatorGetInfo response: largeBlobKey in the extensions field. largeBlobs mapped to true in the options field.

Source: CTAP 2.3 PS, §12.3.

> If an authenticator is selected and the selected authenticator supports large blobs, set supported to true, and false otherwise.

Source: WebAuthn Level 3 (L3), §10.1.5, registration client extension processing. [WebAuthn L3 §10.1.5](https://www.w3.org/TR/webauthn-3/#sctn-large-blob-extension).

> true if, and only if, the created credential supports storing large blobs. Only present in registration outputs.

Source: WebAuthn L3 §10.1.5, client extension output.

> Roaming authenticators that use [FIDO-CTAP] as their cross-platform transport protocol only support this Large Blob extension for discoverable credentials, and might return an error unless authenticatorSelection.residentKey is set to preferred or required.

Source: WebAuthn L3 §10.1.5, note. The source wraps this sentence across lines; the words are unchanged.

> If the authenticatorMakeCredential operation for the new credential does not map rk to true in the options map, return an error. (Large blobs are only applicable for discoverable credentials.)

Source: CTAP 2.3 PS, §6.10.5. This is the platform-side rule for the same case.

| Clause | CTAP 2.1 | CTAP 2.2 | CTAP 2.3 | Notes |
| --- | --- | --- | --- | --- |
| §12.3 response key 0x05 | Yes | Yes | Yes | |
| §12.3 `rk` must be true, or CTAP2_ERR_INVALID_OPTION | Yes | Yes | Yes | |
| §12.3 detection from GetInfo | Yes | Yes | Yes | |
| WebAuthn L3 §10.1.5 registration `supported` | n/a | n/a | n/a | WebAuthn L3, not CTAP |

## Canonical Python reference

python-fido2 gets the result right. It sets `supported` from whether the response carries the key ([extensions.py#L420-L428](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L420-L428)).

```python
                def prepare_inputs(self, pin_token):
                    return {LargeBlobExtension.NAME: True}

                def prepare_outputs(self, response, pin_token):
                    return {
                        "largeBlob": AuthenticatorExtensionsLargeBlobOutputs(
                            supported=response.large_blob_key is not None
                        )
                    }
```

Python has the same request problem, so it is a candidate for the divergence ledger. It sends `largeBlobKey: true` without checking `rk` ([extensions.py#L420-L421](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L420-L421)). It checks GetInfo only when `support` is `required` ([extensions.py#L416-L417](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L416-L417)). Its client sets `rk` only for a required resident key, or for a preferred one on a capable authenticator ([client/__init__.py#L828-L842](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L828-L842)). So a largeBlob request with other `residentKey` values can reach the authenticator without `rk`. This is not run. We did not check the git history for intent.

## Sibling SDKs (context only)

yubikit-android sets `supported` from the presence of the key ([LargeBlobExtension.java#L92](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/fido/src/main/java/com/yubico/yubikit/fido/client/extensions/LargeBlobExtension.java#L92)). That matches the fix below.

## Reproduction

- Unit: [WebAuthnLargeBlobAuditReproTests.YESDK1613_LargeBlobKeyFromCtapResponseReportsSupported](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnLargeBlobAuditReproTests.cs#L16-L52). It decodes a makeCredential response with key 0x05 and expects `Supported` to be true. Today it fails: `Supported` is false. The call has no parameter for the key, so the test changes with the signature.
- Hardware: not run (optional in the plan). The request-without-`rk` failure is not run on hardware either.

## Proposed fix

- Recommendation (option B below):
  1. Pass the decoded key into the registration pipeline. `ExtensionPipeline.ParseRegistrationOutputs` takes the key, or a presence flag, from `ctapResponse.LargeBlobKey` ([WebAuthnClient.Registration.cs#L360-L364](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L360-L364), [ExtensionPipeline.cs#L156](../../../src/WebAuthn/src/Extensions/ExtensionPipeline.cs#L156)).
  2. `LargeBlobAdapter.ParseRegistrationOutput` returns `supported` from key presence. Remove the signed-map lookup ([LargeBlobAdapter.cs#L45-L57](../../../src/WebAuthn/src/Extensions/Adapters/LargeBlobAdapter.cs#L45-L57)).
  3. `LargeBlobAdapter.ApplyToBuilder` requests `largeBlobKey` only when the request has `rk: true` and GetInfo advertises both `largeBlobKey` and `largeBlobs`. Otherwise it omits the key ([LargeBlobAdapter.cs#L38-L39](../../../src/WebAuthn/src/Extensions/Adapters/LargeBlobAdapter.cs#L38-L39), [WebAuthnClient.Registration.cs#L293-L295](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L293-L295)). Step 3 needs AuthenticatorInfo in request construction. That plumbing is part of #7.
  4. Keep `support: required` rejected, or implement enforcement in the same change ([LargeBlobAdapter.cs#L31-L36](../../../src/WebAuthn/src/Extensions/Adapters/LargeBlobAdapter.cs#L31-L36)).
- Options considered:
  - A. Presence only (steps 1 and 2). Pros: smallest change. Cons: still sends `largeBlobKey` on requests without `rk: true`, which a conforming authenticator rejects.
  - B. Presence plus gating (steps 1 to 3). Recommended. Pros: matches CTAP §12.3 and WebAuthn L3 §10.1.5. Cons: depends on the GetInfo plumbing from #7.
  - C. Read `largeBlob.supported` from the direct-extension output instead. Pros: works for direct-only devices. Cons: no YubiKey uses it; depends on #3 and #4.
- API impact: none. The pipeline is internal, so its signature changes but its public surface does not.
- Proving test: `YESDK1613_LargeBlobKeyFromCtapResponseReportsSupported`, updated for the new signature. Test that requests without `rk: true` omit `largeBlobKey`. Also test that `residentKey: preferred` on a capable authenticator produces `rk: true`, requests `largeBlobKey`, and reports support when the response contains the key.
- Depends on or interacts with: #7 (the `rk` builder and GetInfo plumbing).
- Open questions: when the response and GetInfo disagree, trust the response (recommended). Should `support: required` throw until enforcement exists?

## Check it yourself

- `dotnet toolchain.cs -- test --project WebAuthn --filter "FullyQualifiedName~YESDK1613"` (fails today).
- WebAuthn L3 §10.1.5 (`largeBlob`), linked above. CTAP 2.3 PS §12.3 (`largeBlobKey`).
- Read `src/WebAuthn/src/Extensions/Adapters/LargeBlobAdapter.cs` lines 28-57, and `src/WebAuthn/src/Client/WebAuthnClient.Registration.cs` lines 293-295 and 360-364.
