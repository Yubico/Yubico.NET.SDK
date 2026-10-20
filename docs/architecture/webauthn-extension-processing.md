# WebAuthn Extension Processing

This document defines how the WebAuthn client turns relying-party extension inputs into CTAP2
extension requests and turns authenticator extension outputs into client extension results:

> Extension inputs are prepared **per ceremony attempt**, after the authenticator is known, after the
> PIN/UV token has been obtained, and (for assertions) after one credential has been selected.
> Extension outputs are prepared while the token is still live. Each extension owns its own state.

It is the .NET counterpart of python-fido2's `Ctap2Extension` / `ExtensionProcessor` model and
yubikit-android's `Extension` / `RegistrationProcessor` / `AuthenticationProcessor` model.

## Vocabulary

The SDK uses three names for three layers. Type names follow the layer they belong to.

| Term | Means | Where it lives |
| --- | --- | --- |
| **FIDO** | The umbrella (WebAuthn + CTAP) and the HID transport (CTAPHID framing, `FidoHidProtocol`) | `Yubico.YubiKit.Core` |
| **CTAP2** | The authenticator protocol: commands, CBOR codecs, `ExtensionBuilder`, PIN/UV protocols, `ClientPin`, `LargeBlobStorage` | `Yubico.YubiKit.Fido2` (`FidoSession` is the CTAP2 session) |
| **WebAuthn** | The client: ceremonies, RP-facing options and responses, extension *processing* | `Yubico.YubiKit.WebAuthn` |

A CTAP2 codec encodes one extension's wire shape and knows nothing about ceremonies. A WebAuthn
processor decides *what* to send and *when*, and may call the authenticator (key agreement, large-blob
reads and writes) to do so.

## Where code belongs

> `Yubico.YubiKit.Fido2` (CTAP2) contains only what has a representation on the CTAP wire or in the
> CTAP specification. `Yubico.YubiKit.WebAuthn` contains everything that exists because a *client*
> sits between a relying party and an authenticator.

Test for any piece of code: *would a non-WebAuthn CTAP client (ykman, a CLI, a test harness) need this
exactly as written?* Yes: Fido2. No: WebAuthn. This is the split python-fido2 draws between
`fido2/ctap2` and `fido2/client`, and yubikit-android between `fido/ctap` and `fido/client`.

| Concern | Layer | Why |
| --- | --- | --- |
| `FidoSession` commands, CBOR request/response codecs, `CtapStatus`, `AuthenticatorInfo` | Fido2 | CTAP §6 verbatim |
| Validation of CTAP invariants on request construction (`up=false` at makeCredential, duplicate `pubKeyCredParams`) | Fido2; WebAuthn adds a typed error on top | the MUST is on whoever builds the CTAP message |
| `ExtensionBuilder` and the per-extension wire codecs (`hmac-secret` §12.5 map, `credProtect`, `credBlob`, `largeBlobKey`, `minPinLength`, `previewSign` CBOR) | Fido2 | wire shapes |
| hmac-secret salt encryption and authentication (`WithHmacSecret(protocol, sharedSecret, keyAgreement, salt1, salt2)`) | Fido2 | CTAP §12.5 defines it; a raw CTAP client needs it |
| `ClientPin` key agreement and token minting, PIN/UV protocols, `LargeBlobStorage` (§6.10 array, compression, entry encryption) | Fido2 | CTAP §6.5, §6.10 |
| WebAuthn PRF: salt derivation `SHA-256("WebAuthn PRF" ‖ 0x00 ‖ input)`, `evalByCredential`, `enabled`/`results` | WebAuthn (`PrfProcessor`) | WebAuthn L3 §10.1.4; CTAP has no "prf" |
| `largeBlob: {support}`, `read`/`write` client inputs, `supported`/`blob`/`written` outputs, "store after the assertion with largeBlobKey" | WebAuthn (`LargeBlobProcessor`) | WebAuthn L3 §10.1.5; CTAP has only `largeBlobKey` + `authenticatorLargeBlobs` |
| `credProps` | WebAuthn | client-derived, L3 §10.1.3 |
| previewSign `signByCredential[selected]`, inner attestation handling, flags from the UV preference | WebAuthn (`PreviewSignProcessor`) | the draft's "client extension processing" |
| Credential selection, exclude pre-flight, dummy descriptor | WebAuthn (`CredentialFilter`) | python-fido2 keeps `_filter_creds` in `client/` |
| UV decision, permissions union, PIN prompting, token lifetime, PUAT retry | WebAuthn | client policy |
| Options and response records, client data, RP ID validation, origin | WebAuthn | L3 §5 |

Consequences:

- `IWebAuthnBackend` is the only door from WebAuthn into Fido2. Processors never hold an
  `IFidoSession`; the backend exposes what they need (PIN/UV protocol, key agreement, large-blob storage).
- `FidoSession` stays a complete, standalone CTAP2 client. The WebAuthn package consumes it and never
  adds WebAuthn-flavoured parameters to it.
- Fido2 carries no WebAuthn vocabulary. The `"prf"` identifier and `PrfInput`/`PrfOutput`, the
  `signByCredential` dictionary, and the `largeBlob: {support, read, write}` client inputs are WebAuthn
  types; Fido2 exposes their CTAP counterparts (`hmac-secret`, `WithPreviewSign(PreviewSignSigningParams)`,
  `largeBlobKey` + `LargeBlobStorage`). Nothing added to Fido2 says "WebAuthn"; nothing in WebAuthn
  re-encodes CBOR Fido2 already encodes.

## Why inputs cannot be built up front

Several extensions need facts that only exist part-way through a ceremony:

| Extension | Needs at input time | Needs at output time |
| --- | --- | --- |
| `hmac-secret` (WebAuthn PRF) | the negotiated PIN/UV protocol and a fresh shared secret to encrypt the salts; for `evalByCredential`, the **selected credential** | the same shared secret to decrypt the result |
| `hmac-secret-mc` | as above, at makeCredential | as above |
| `previewSign` (authentication) | the **selected credential** to pick `signByCredential[selected]` | — |
| `largeBlob` (WebAuthn read/write) | the `largeBlobKey` from the assertion | the PIN/UV token with `LargeBlobWrite` permission for `authenticatorLargeBlobs` |
| `credProps` | the effective `requireResidentKey` the client resolved against GetInfo | — |

A design that encodes every extension input before the first authenticator call cannot express any
row of this table: it has no shared secret, no selected credential, no `largeBlobKey` and no resolved
`requireResidentKey` yet. That is why extension handling is split into per-attempt processors with
separate input and output phases.

## Types

```
Yubico.YubiKit.WebAuthn.Extensions
├── Codec/                      pure, static, per extension: CBOR in/out only (no ceremony state)
└── Processing/
    ├── ExtensionContext        record: AuthenticatorInfo Info, IPinUvAuthProtocol? Protocol, IWebAuthnBackend Backend
    ├── IRegistrationExtensionProcessor
    │     string Name
    │     PinUvAuthTokenPermissions Permissions
    │     void PrepareInputs(ExtensionBuilder builder, PinUvAuthTokenSession? token)
    │     ValueTask PrepareOutputsAsync(MakeCredentialResponse response, WebAuthnAuthenticatorData authData,
    │                                   PinUvAuthTokenSession? token, RegistrationExtensionOutputsBuilder outputs, CancellationToken ct)
    ├── IAuthenticationExtensionProcessor
    │     string Name
    │     PinUvAuthTokenPermissions Permissions
    │     void PrepareInputs(ExtensionBuilder builder, PublicKeyCredentialDescriptor? selected, PinUvAuthTokenSession? token)
    │     ValueTask PrepareOutputsAsync(GetAssertionResponse response, WebAuthnAuthenticatorData authData,
    │                                   PinUvAuthTokenSession? token, AuthenticationExtensionOutputsBuilder outputs, CancellationToken ct)
    ├── RegistrationExtensionSession     one per ceremony attempt; owns the processors for the requested inputs
    ├── AuthenticationExtensionSession   same, for assertions
    └── Processors/                      one sealed class per extension (CredProtect, CredBlob, MinPinLength,
                                         CredProps, LargeBlob, Prf, PreviewSign)
Yubico.YubiKit.WebAuthn.Client
└── CredentialFilter            the up=false probe shared by the exclude-list pre-flight and allow-list selection
```

Rules:

- A **session** is created from the RP's typed inputs and an `ExtensionContext`. It instantiates one
  processor per requested extension that the authenticator advertises; unsupported extensions are
  dropped (WebAuthn L3 §9.4) except where the public API already promises a typed error (previewSign →
  `WebAuthnClientError(NotSupported)`).
- `Permissions` is the union of the processors' permissions and is folded into the PIN/UV token request
  (`UvDecisionLogic.Decide(..., requestedPermissions: ceremony | session.Permissions)`), as
  python-fido2 (`permissions |= ext.permissions`) and yubikit-android (`permissions |= processor.getPermissions()`) do.
- `PrepareInputs` writes into the CTAP2 `ExtensionBuilder`; it never encodes CBOR itself.
- `PrepareOutputsAsync` receives the CTAP response and its authenticator data decoded once by the
  session, and fills the typed output record through a small internal builder. Outputs are typed
  records, not dictionaries. A `CborContentException` from one extension's output drops that
  extension's result only (WebAuthn L3 §9.4 tolerance for malformed authenticator output).
- Processors are **single-use**. A PUAT retry creates a fresh session, so encrypted salts and key
  agreements are re-derived against the new token.
- `ExtensionContext.Protocol` is available without a token: CTAP 2.0 `hmac-secret` works without a PUAT.

## Ceremony flow

### Registration (`WebAuthnClient.MakeCredentialAsync`)

1. Validate options; build client data; `GetInfo` (cached).
2. Resolve `requireResidentKey` against GetInfo.
3. `session = RegistrationExtensionSession.Create(options.Extensions, context)`.
4. Decide UV; obtain the token with `MakeCredential | session.Permissions`.
5. Exclude-list pre-flight through `CredentialFilter` (probe with `up: false`; a match raises
   `CredentialExcluded` before any touch).
6. `session.PrepareInputs(builder, token)` → extensions CBOR → `authenticatorMakeCredential`.
7. `session.PrepareOutputsAsync(response, token)` → `RegistrationExtensionOutputs`.
8. Dispose the token.

### Authentication (`WebAuthnClient.GetAssertionAsync`)

1. Validate options; build client data; `GetInfo` (cached).
2. `session = AuthenticationExtensionSession.Create(options.Extensions, context)`.
3. Decide UV; obtain the token with `GetAssertion | session.Permissions`.
4. **Selection.** If `allowCredentials` is non-empty, `CredentialFilter.SelectAsync` probes it in
   chunks of `maxCredentialCountInList` with `up: false` and returns the first credential the
   authenticator recognises, or null. This always runs when an allow list is present; it costs one
   extra round trip and no touch.
5. `session.PrepareInputs(builder, selected, token)` → extensions CBOR.
6. `authenticatorGetAssertion` with `allowList = [selected]`. If nothing matched, a **dummy descriptor**
   (`id = 0x00`, the first entry's type) is sent so user presence is still requested; the
   authenticator answers `CTAP2_ERR_NO_CREDENTIALS`, mapped to `InvalidState`. This keeps "does this
   credential exist?" unobservable without a touch, as in python-fido2 and yubikit-android.
   With no allow list the existing `GetNextAssertion` enumeration runs unchanged.
7. For every returned assertion, `session.PrepareOutputsAsync(response, token)` runs **eagerly** while
   the token is live; the results are captured in `MatchedCredential`, whose `SelectAsync` performs
   no further authenticator work.
8. Dispose the token.

### `CredentialFilter`

The one implementation of python-fido2's `_filter_creds` / yubikit-android's `Utils.filterCreds`:

- drops descriptors longer than `maxCredentialIdLength` (CTAP 2.1-2.3 §6.4);
- refuses a non-positive `maxCredentialCountInList` (§6.4 "MUST be greater than zero");
- probes chunks with `up: false` and the ceremony's `pinUvAuthParam` over a 32-byte zero client-data hash;
- on `CTAP2_ERR_REQUEST_TOO_LARGE` with a chunk larger than one, shrinks the chunk and retries;
- on `CTAP2_ERR_NO_CREDENTIALS` advances to the next chunk; any other status propagates.

## Invariants (tests pin these)

- No extension CBOR is produced before `GetInfo` has been read.
- An extension the authenticator does not advertise produces no CTAP input and no output.
- The token requested for a ceremony carries every processor's permissions.
- With an allow list, the real `getAssertion` carries exactly one descriptor.
- A `selected`-dependent input (`evalByCredential`, `signByCredential`) always names the selected credential.
- A PUAT retry never reuses a processor instance.
- Output preparation never outlives the token.

## Sibling mapping

| Concept | This SDK | python-fido2 (`fido2/ctap2/extensions.py`, `fido2/client/__init__.py`) | yubikit-android (`fido/client/extensions`, `Ctap2Client`) |
| --- | --- | --- | --- |
| Processor | `I{Registration,Authentication}ExtensionProcessor` | `RegistrationExtensionProcessor` / `AuthenticationExtensionProcessor` | `Extension.RegistrationProcessor` / `AuthenticationProcessor` |
| Prepare inputs | `PrepareInputs(builder, [selected,] token)` | `prepare_inputs([selected,] pin_token)` | `prepareInput([selected,] pinToken)` |
| Prepare outputs | `PrepareOutputsAsync(response, token, outputs, ct)` | `prepare_outputs(response, pin_token)` | `prepareOutput(data, pinToken)` |
| Permissions | `Permissions` union into `UvDecision` | `permissions \|= ext.permissions` | `permissions \|= processor.getPermissions()` |
| Selection | `CredentialFilter.SelectAsync` | `_filter_creds` | `Utils.filterCreds` |
| Dummy descriptor | yes | yes (`id=b"\0"`) | yes |
| Output timing | eager per assertion | lazy in `get_response(index)` holding the token | eager |

Known divergence: python-fido2 prepares outputs lazily and keeps the PIN token alive in the selection
object; this SDK prepares them eagerly so the token's lifetime ends when the public call returns.

## Non-goals

- The direct CTAP 2.2-2.3 §12.4 `largeBlob` getAssertion extension. §12.4 makes it mutually exclusive with `largeBlobKey`, which is what YubiKeys implement.
- Credential selection UI. `SelectAsync` on `MatchedCredential` is the caller's choice among already
  computed assertions; it does not drive authenticator selection.
