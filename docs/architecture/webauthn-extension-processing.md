# WebAuthn Extension Processing

This document defines how the WebAuthn client turns relying-party extension inputs into CTAP2
extension requests and turns authenticator extension outputs into client extension results:

> Extension inputs are prepared **per ceremony attempt**, after the authenticator is known, after the
> PIN/UV decision and any required acquisition, and (for allow-list assertions) after a credential
> has been selected. Outputs are prepared before any acquired token is disposed. Each extension
> owns its own per-attempt state.

The processors and sessions are internal implementation components, not a public plugin interface.
The model separates client policy from wire encoding; it does not establish cross-SDK parity or full
WebAuthn conformance. See the [consumer guide](../../src/WebAuthn/README.md#use-extensions) for request
and result usage.

## Vocabulary

The SDK uses three names for three layers. Type names follow the layer they belong to.

| Term | Means | Where it lives |
| --- | --- | --- |
| **FIDO** | The umbrella (WebAuthn + CTAP) and the HID transport (CTAPHID framing, `FidoHidProtocol`) | `Yubico.YubiKit.Core` |
| **CTAP2** | The authenticator protocol: commands, CBOR codecs, `ExtensionBuilder`, PIN/UV protocols, `ClientPin`, `LargeBlobStorage` | `Yubico.YubiKit.Ctap2` (`Ctap2Session` / `ICtap2Session`) |
| **WebAuthn** | The client: ceremonies, RP-facing options and responses, extension *processing* | `Yubico.YubiKit.WebAuthn` |

A CTAP2 codec encodes one extension's wire shape and knows nothing about ceremonies. A WebAuthn
processor decides *what* to send and *when*, and may call the authenticator (key agreement, large-blob
reads and writes) to do so.

## Where code belongs

> `Yubico.YubiKit.Ctap2` contains what has a representation on the CTAP wire or in the
> CTAP specification. `Yubico.YubiKit.WebAuthn` contains everything that exists because a *client*
> sits between a relying party and an authenticator.

Test for any piece of code: *would a non-WebAuthn CTAP client (ykman, a CLI, a test harness) need this
exactly as written?* Yes: Ctap2. No: WebAuthn. A similar split appears in python-fido2 between
`fido2/ctap2` and `fido2/client`, and yubikit-android between `fido/ctap` and `fido/client`.

| Concern | Layer | Why |
| --- | --- | --- |
| `Ctap2Session` commands, CBOR request/response codecs, `CtapStatus`, `AuthenticatorInfo` | Ctap2 | CTAP2 authenticator operations |
| Validation of CTAP invariants on request construction (`up=false` at makeCredential, duplicate `pubKeyCredParams`) | Ctap2; WebAuthn adds selected typed errors | requirements apply to the actor building the CTAP message |
| `ExtensionBuilder` and the per-extension wire codecs (`hmac-secret`, `credProtect`, `credBlob`, `largeBlobKey`, `minPinLength`, `previewSign` CBOR) | Ctap2 | wire shapes; hmac-secret is CTAP2.1 §12.5, CTAP2.2/2.3 §12.7 |
| hmac-secret salt encryption and authentication (`WithHmacSecret(protocol, sharedSecret, keyAgreement, salt1, salt2)`) | Ctap2 | the same version-specific hmac-secret sections define it; raw clients need it |
| `ClientPin` key agreement and token minting, PIN/UV protocols, `LargeBlobStorage` (§6.10 array, compression, entry encryption) | Ctap2 | CTAP2.3 §6.5, §6.10 |
| WebAuthn PRF: salt derivation `SHA-256("WebAuthn PRF" ‖ 0x00 ‖ input)`, `evalByCredential`, `enabled`/`results` | WebAuthn (`PrfProcessor`) | WebAuthn L3 §10.1.4; CTAP has no "prf" |
| `largeBlob: {support}`, `read`/`write` client inputs, `supported`/`blob`/`written` outputs, "store after the assertion with largeBlobKey" | WebAuthn (`LargeBlobProcessor`) | WebAuthn L3 §10.1.5; this implementation uses `largeBlobKey` + `authenticatorLargeBlobs`, not CTAP's alternative direct extension |
| `credProps` | WebAuthn | client-derived, L3 §10.1.3 |
| previewSign `signByCredential[selected]`, inner attestation handling, flags from the UV preference | WebAuthn (`PreviewSignProcessor`) | the draft's "client extension processing" |
| Credential selection, exclude pre-flight, dummy descriptor | WebAuthn (`CredentialFilter`) | python-fido2 keeps `_filter_creds` in `client/` |
| UV decision, permissions union, PIN prompting, token lifetime, PUAT retry | WebAuthn | client policy |
| Options and response records, client data, RP ID validation, origin | WebAuthn | L3 §5 |

Consequences:

- `IWebAuthnBackend` is the ceremony I/O boundary into Ctap2. Processors never hold an
  `ICtap2Session`; the backend exposes what they need (PIN/UV protocol, key agreement, large-blob storage).
- `Ctap2Session` stays a standalone CTAP2 client. The WebAuthn package consumes it and never
  adds WebAuthn-flavoured parameters to it.
- Client-only vocabulary stays in WebAuthn. The `"prf"` identifier and PRF inputs/results, the
  `signByCredential` dictionary, and the `largeBlob: {support, read, write}` client inputs are WebAuthn
  types; Ctap2 exposes their CTAP counterparts (`hmac-secret`, `WithPreviewSign(PreviewSignSigningParams)`,
  `largeBlobKey` + `LargeBlobStorage`). WebAuthn delegates existing CTAP wire encoding rather than
  maintaining a duplicate encoder.

## Why inputs cannot be built up front

Several extensions need facts that only exist part-way through a ceremony:

| Extension | Needs at input time | Needs at output time |
| --- | --- | --- |
| `hmac-secret` (WebAuthn PRF) | a PIN/UV protocol and a fresh shared secret to encrypt the salts; for `evalByCredential`, the **selected credential** | the same shared secret to decrypt the result |
| `hmac-secret-mc` | as above, at makeCredential | as above |
| `previewSign` (authentication) | the **selected credential** to pick `signByCredential[selected]` | — |
| `largeBlob` (WebAuthn read/write) | capability checks and a request for the response's `largeBlobKey` | that returned key, plus the token with `LargeBlobWrite` permission for SDK writes |
| `credProps` | the effective `requireResidentKey` the client resolved against GetInfo | — |

A design that completes all extension work before the first authenticator call does not yet have
the shared secret, selected credential, response `largeBlobKey` or resolved `requireResidentKey`.
That is why extension handling is split into per-attempt processors with
separate input and output phases.

## Types

```
Yubico.YubiKit.WebAuthn.Extensions
├── Codec/                      pure, static, per extension: CBOR in/out only (no ceremony state)
└── Processing/
    ├── ExtensionContext        record: AuthenticatorInfo Info, IPinUvAuthProtocol? Protocol, IWebAuthnBackend Backend
    ├── IRegistrationExtensionProcessor   Name, Permissions, PrepareAsync, PrepareInputs, PrepareOutputsAsync
    ├── IAuthenticationExtensionProcessor same phases, with selected-credential context for inputs
    ├── RegistrationExtensionSession     one per ceremony attempt; owns the processors for the requested inputs
    ├── AuthenticationExtensionSession   same, for assertions
    └── Processors/                      one sealed class per extension (CredProtect, CredBlob, MinPinLength,
                                         CredProps, LargeBlob, Prf, PreviewSign)
Yubico.YubiKit.WebAuthn.Client
└── CredentialFilter            the up=false probe shared by the exclude-list pre-flight and allow-list selection
```

Rules:

- A **session** is created from typed inputs and an `ExtensionContext`, plus registration options and
  effective RK, or authentication's original allow list. Most processors are gated on advertised
  support, including previewSign; an unsupported previewSign request is omitted, not guaranteed to
  throw. credProps needs no advertisement, and largeBlob has separate support checks.
- `Permissions` is the union of the processors' permissions and is folded into the PIN/UV token request
  (`UvDecisionLogic.Decide(..., requestedPermissions: ceremony | session.Permissions)`). LargeBlob
  writes contribute `LargeBlobWrite`; PRF contributes no additional permissions.
- `PrepareAsync` performs authenticator-dependent setup, such as key agreement, before inputs are built.
- `PrepareInputs` writes into the CTAP2 `ExtensionBuilder`; it never encodes CBOR itself.
- `PrepareOutputsAsync` receives the CTAP response and its authenticator data already decoded by the
  ceremony, and fills the typed output record through a small internal builder. Outputs are typed
  records, not dictionaries. Per-processor `CborContentException` and `InvalidOperationException`
  omit that processor's result. This is SDK exception-isolation policy, not a rule quoted from
  WebAuthn §9.4. Other exceptions, including CTAP storage failures and explicit previewSign errors,
  can propagate after the main authenticator operation succeeded.
- Processors are **single-use**. A PUAT retry creates a fresh extension session; fresh per-attempt key
  agreement and encrypted salts are prepared after token reacquisition, not derived from the token bytes.
- `ExtensionContext.Protocol` is available without a token; hmac-secret key agreement need not have
  a PUAT. The current backend nevertheless uses protocol 2, without protocol-1 negotiation, for this
  exchange and PIN/UV token acquisition. Do not infer universal CTAP2.0 compatibility from token absence.

[WebAuthn L3 REC §9 and §9.4](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#sctn-extensions)
describe extension optionality and client input/output transformation, including no output for ignored
extensions. They do not mandate this SDK's exact catch policy. A null input container produces a null
output container; a requested container can produce a record with every individual property null.
Explicit false enablement/support/write results remain different states.

## Ceremony flow

### Registration (`WebAuthnClient.MakeCredentialAsync`)

1. Validate options; build client data; `GetInfo` (cached).
2. Resolve `requireResidentKey` against GetInfo.
3. Create the extension session with inputs, creation options, effective RK and context.
4. Decide UV using `MakeCredential | session.Permissions`; acquire a token if required/available.
5. Run `PrepareAsync`, then exclude-list pre-flight through `CredentialFilter` with `up:false`.
   A match is carried into the final makeCredential exclude list, not rejected directly from the
   silent probe. Without a token, probes omit PIN-auth fields. With one, probes authenticate a zero
   client-data hash; registration re-mints the token afterward for the final command.
6. `session.PrepareInputs` → extensions CBOR → `authenticatorMakeCredential`, with the matched
   descriptor or empty exclude list and normal required-UP policy. Exclusion maps to `InvalidState`.
7. `session.PrepareOutputsAsync(response, token)` → `RegistrationExtensionOutputs`.
8. Dispose the token.

### Authentication (`WebAuthnClient.GetAssertionAsync`)

1. Validate options; build client data; `GetInfo` (cached).
2. Create the extension session with inputs, original allow list and context.
3. Decide UV using `GetAssertion | session.Permissions`; acquire a token if required/available, then
   run `PrepareAsync`.
4. **Selection.** If `allowCredentials` is non-empty, `CredentialFilter.SelectAsync` probes it in
   chunks of `maxCredentialCountInList` with `up: false` and returns the first credential the
   authenticator recognises, or null. This can take multiple round trips; probes request no user
   presence. A chunk's internal credential choice follows the authenticator response, not a promise
   of deterministic ordering among all matching descriptors.
5. `session.PrepareInputs(builder, selected, token)` → extensions CBOR.
6. `authenticatorGetAssertion` with `allowList = [selected]`. If nothing matched, a **dummy descriptor**
   (`id = 0x00`, the first entry's type) is sent so user presence is still requested; the
   main `NO_CREDENTIALS`/`INVALID_CREDENTIAL` responses produce an empty match list. The dummy request
   avoids returning the silent miss directly; it does not prove equal timings/prompts or that absence
   cannot be inferred. With no allow list, `GetNextAssertion` can enumerate discoverable assertions.
7. For every returned assertion, `session.PrepareOutputsAsync(response, token)` runs **eagerly** while
   the token is live; the results are captured in `MatchedCredential`, whose `SelectAsync` performs
   no further authenticator work.
8. Dispose the token.

### `CredentialFilter`

Final descriptor omission currently loses the selected ID during response construction. CTAP2.0
§5.2 permits omission for a singleton allow list; CTAP2.1/2.3 §6.2.2 require the descriptor. Probe
singleton recovery does not repair that final-response gap. Only an original single-entry request
gives the application an unambiguous requested ID; guessing the first of multiple entries is unsafe.
RP verification remains mandatory.

The shared probe implementation:

- drops descriptors longer than `maxCredentialIdLength` (CTAP 2.1-2.3 §6.4);
- refuses a non-positive `maxCredentialCountInList` (§6.4 "MUST be greater than zero");
- probes chunks with `up:false` and, when there is a token, `pinUvAuthParam` over a 32-byte zero client-data hash; without a token, PIN-auth fields are omitted;
- on `CTAP2_ERR_REQUEST_TOO_LARGE` with a chunk larger than one, shrinks the chunk and retries;
- on `CTAP2_ERR_NO_CREDENTIALS` advances to the next chunk; any other status propagates.

## Flow invariants

- No extension CBOR is produced before `GetInfo` has been read.
- Most unadvertised extensions produce no input/output; client-derived credProps and largeBlob's
  separate support policy must not be forced into that rule.
- The ceremony's token request combines its own permission with the processors' requested permissions.
  Legacy PIN-token command fallback omits the explicit permission/RP request; a returned token does not
  prove that every requested operation is authorized.
- With an allow list, the real `getAssertion` carries exactly one descriptor.
- A `selected`-dependent input (`evalByCredential`, `signByCredential`) always names the selected credential.
- A PUAT retry never reuses a processor instance.
- Output preparation never outlives the token.

## Current policy and lifetime boundaries

Extension-session disposal releases per-attempt shared secrets; the ceremony owns and disposes its
token separately. Neither clears returned response buffers. PRF outputs are derived secrets exposed
through read-only result memory, with no SDK cleanup guarantee for returned or intermediate arrays.
Decrypted large/credential blobs can also be sensitive.
Clearing an application copy does not clear the SDK's original allocations.

The PRF salt mapping belongs in WebAuthn, but correct domain separation alone does not establish
conformance. [L3 REC §10.1.4](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#prf-extension)
requires the verified-use hmac-secret PRF and a UV override when necessary. The current processor
preserves caller UV instead. Required is recommended for evaluation, not a complete conformance or
cleanup remedy.

credProtect currently forwards/parses policy without implementing enforcement. CTAP2.3 §12.1 assigns
the platform a SHOULD NOT for creating without requested policies 2/3 when enforcement is true;
checking a returned policy after creation is not equivalent to prevention. LargeBlob Required also
does not guarantee rejection for every unusable created credential. The original-singleton-allow-list
write check is missing, and SDK writes always require a token even though CTAP has an
unconfigured-authenticator exception. Storage errors can propagate after successful assertion;
nullable Blob is not a unique absence/support/error discriminator. When storage returns a null byte
array, the current conversion produces a present value of length zero. Nullable presence and length
therefore do not distinguish missing data from a stored empty blob.

The previewSign processor follows the [vendor draft v4 §10.2.1](https://yubicolabs.github.io/webauthn-sign-extension/4/#sctn-sign-extension),
including multi-credential map coverage and selected-credential lookup. Its generated key is separate
from the credential key; inner attestation parsing is not trust verification. Explicit missing-output
client errors can propagate, while decoder CBOR/invalid-operation failures may omit results. The draft's
permitted empty handles cannot currently pass the signing-params constructor. MinPinLength also has
an input-handling limitation: presence requests it regardless of Requested=false.

GetInfo/token acquisition and extension output preparation are not all inside the main command's
mapping catch. Outputs deliberately run outside UV escalation so a failed read/parse cannot trigger
another credential creation. Keep these boundaries explicit when revisiting error policy. The
ceremony timer signals cooperative cancellation; successful device completion can win the race.

## Non-goals

- The direct CTAP2.2/2.3 §12.4 `largeBlob` extension, mutually exclusive with `largeBlobKey`.
  This implementation supports the latter route; do not infer universal device support.
- Credential selection UI. `SelectAsync` on `MatchedCredential` is the caller's choice among already
  computed assertions; it does not drive authenticator selection.
