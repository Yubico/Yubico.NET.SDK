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
| **CTAP2** | The authenticator protocol: commands, CBOR codecs, `ExtensionBuilder`, PIN/UV protocols, `ClientPin`, `LargeBlobStorage` | `Yubico.YubiKit.Fido2` (today's `FidoSession` is the CTAP2 session) |
| **WebAuthn** | The client: ceremonies, RP-facing options and responses, extension *processing* | `Yubico.YubiKit.WebAuthn` |

A CTAP2 codec encodes one extension's wire shape and knows nothing about ceremonies. A WebAuthn
processor decides *what* to send and *when*, and may call the authenticator (key agreement, large-blob
reads and writes) to do so.

## Why inputs cannot be built up front

Several extensions need facts that only exist part-way through a ceremony:

| Extension | Needs at input time | Needs at output time |
| --- | --- | --- |
| `hmac-secret` (WebAuthn PRF) | the negotiated PIN/UV protocol and a fresh shared secret to encrypt the salts; for `evalByCredential`, the **selected credential** | the same shared secret to decrypt the result |
| `hmac-secret-mc` | as above, at makeCredential | as above |
| `previewSign` (authentication) | the **selected credential** to pick `signByCredential[selected]` | — |
| `largeBlob` (WebAuthn read/write) | the `largeBlobKey` from the assertion | the PIN/UV token with `LargeBlobWrite` permission for `authenticatorLargeBlobs` |
| `credProps` | the effective `requireResidentKey` the client resolved against GetInfo | — |

A pipeline that encodes everything before the first device call (the pre-port `ExtensionPipeline`)
cannot express any row of this table. Audit findings #1, 26a, 26b, 26d and the largeBlob group
(#12-#16) are all instances of that one limitation.

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
    │     ValueTask PrepareOutputsAsync(MakeCredentialResponse response, PinUvAuthTokenSession? token,
    │                                   RegistrationExtensionOutputs.Builder outputs, CancellationToken ct)
    ├── IAuthenticationExtensionProcessor
    │     string Name
    │     PinUvAuthTokenPermissions Permissions
    │     void PrepareInputs(ExtensionBuilder builder, PublicKeyCredentialDescriptor? selected, PinUvAuthTokenSession? token)
    │     ValueTask PrepareOutputsAsync(GetAssertionResponse response, PinUvAuthTokenSession? token,
    │                                   AuthenticationExtensionOutputs.Builder outputs, CancellationToken ct)
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
- `PrepareOutputsAsync` fills the typed output record through a small internal builder. Outputs are
  typed records, not dictionaries.
- Processors are **single-use**. A PUAT retry creates a fresh session, so encrypted salts and key
  agreements are re-derived against the new token.
- `ExtensionContext.Protocol` is available without a token: CTAP 2.0 `hmac-secret` works without a PUAT.

## Ceremony flow

### Registration (`WebAuthnClient.MakeCredentialAsync`)

1. Validate options; build client data; `GetInfo` (cached).
2. Resolve `requireResidentKey` against GetInfo (#7).
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
   authenticator recognises, or null. This always runs when an allow list is present (sibling parity;
   one extra round trip without a touch).
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
- refuses a non-positive `maxCredentialCountInList` (§6.4 "MUST be greater than zero"; 25g);
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

- The direct CTAP 2.2-2.3 §12.4 `largeBlob` getAssertion extension (removed in #3; no YubiKey supports it).
- Credential selection UI. `SelectAsync` on `MatchedCredential` is the caller's choice among already
  computed assertions; it does not drive authenticator selection.
