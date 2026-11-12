# Compiler WA addendum — new unread whole-unit proposals

**No content verdicts or sample credit.** Source pin `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Read each whole unit and relevant enclosing context.

## `core-001:unit:9b2b20dfbae1bf78`
`src/WebAuthn/CLAUDE.md:530–539`; original rank 6; Lost original supported-slot replacement.

````markdown
## Peer Module Pointers

**Related Modules:**
- **Yubico.YubiKit.Ctap2** — Lower-level CTAP protocol; WebAuthn builds on top of `ICtap2Session`
- **Yubico.YubiKit.Core** — Shared types, logging (`YubiKitLogging`), memory utilities

**Integration:**
- WebAuthn → Ctap2 (depends on)
- Ctap2 does not depend on WebAuthn


````

## `core-001:unit:761f1552c9ab4aaf`
`src/WebAuthn/README.md:470–477`; original rank 7; New authorized full original WebAuthn narrative quota.

````markdown
## Related

- [../Core/README.md](../Core/README.md) - device discovery, connections, and the credential contracts.
- [../Ctap2/README.md](../Ctap2/README.md) - the CTAP2 operations layer this package builds on, and [FidoTool](../Ctap2/examples/FidoTool/), the interactive sample; this package has none of its own.
- [user-interaction.md](../../docs/usage/user-interaction.md) and [device-discovery.md](../../docs/usage/device-discovery.md).
- The [WebAuthn Level 3](https://www.w3.org/TR/webauthn-3/) specification and the [Public Suffix List](https://publicsuffix.org/).
- [Developer guide](../../docs/DEV-GUIDE.md): building, testing, and contributing.


````

## `core-001:unit:6afae0cb3a2a58c7`
`src/WebAuthn/README.md:329–355`; original rank 8; New authorized full original WebAuthn narrative quota.

````markdown
### Request protection and blob storage

`CredProtect` forwards the requested policy and reports the returned policy, but
`EnforceCredentialProtectionPolicy` is **not currently enforced** by this client. Do not rely on that
flag to prevent unprotected creation. Checking the result can detect an unacceptable policy only
after a credential has been created; it is not equivalent to pre-creation enforcement. CTAP2.3 §12.1
places enforcement at the platform and says it SHOULD NOT create without the requested protection
for policies 2/3 when enforcement is requested.

Large-blob support uses `largeBlobKey` plus `authenticatorLargeBlobs`, not the alternative direct CTAP
`largeBlob` extension. Request a discoverable credential (`ResidentKey = Required` when essential)
and inspect registration's `LargeBlob.Supported`. The current `LargeBlobSupport.Required` does not
guarantee failure in every case where the created credential lacks blob support.

For writes, supply exactly **one entry in the original** `AllowCredentials` list; the client currently
does not enforce this WebAuthn input constraint. The SDK requires a PIN/UV token for writes and adds
`LargeBlobWrite` to the ceremony permissions, even though CTAP has an unconfigured-authenticator
exception. Storage work runs after the assertion, before `GetAssertionAsync` returns. Read/storage
errors can therefore propagate after a successful assertion; an absent result or nullable `Blob` alone
does not reliably distinguish missing data, unsupported capability, and failure. Valid empty data is
a separate state. Decrypted blob and credential-blob outputs may contain sensitive application data
and share the lack of an SDK-owned cleanup contract described above.

`MinPinLength` reports the existing policy in Unicode code points only when the RP is authorized on
the authenticator; it does not set a PIN policy. A current input-handling limitation means any nonnull
input requests it, even with `Requested = false`; use null to omit it.


````

## `core-001:unit:9dc7ab5953064c3f`
`src/WebAuthn/CLAUDE.md:323–370`; original rank 9; New authorized full original WebAuthn narrative quota.

````markdown
### Extension Processing

Extensions follow the processor model described in
[docs/architecture/webauthn-extension-processing.md](../../docs/architecture/webauthn-extension-processing.md)
(a per-attempt processor design analogous to other YubiKit clients, not a cross-SDK parity claim):

- `Extensions/Codec/*` are pure static CBOR encoders/decoders for one extension each. No ceremony state.
- `Extensions/Processing/Processors/*` are single-use, one sealed class per extension. They implement
  `IRegistrationExtensionProcessor` / `IAuthenticationExtensionProcessor`: `Permissions` (folded into the
  PIN/UV token request), `PrepareAsync` (authenticator-dependent setup), `PrepareInputs(builder, [selected,] token)` and
  `PrepareOutputsAsync(response, token, outputs, ct)`.
- `RegistrationExtensionSession` / `AuthenticationExtensionSession` are created per ceremony attempt from the
  typed inputs and an `ExtensionContext` (GetInfo, PIN/UV protocol, backend), plus creation/effective-RK
  or allow-list context. Most unsupported requests, including previewSign, are omitted. credProps is
  client-derived; largeBlob has separate support checks. Inspect optional outputs. Per-processor
  `CborContentException` and `InvalidOperationException` suppress that output as SDK isolation policy,
  not a WebAuthn §9.4 mandate. Other exceptions, including explicit previewSign errors, can propagate.
- `Client/CredentialFilter` is the shared `up:false` probe for the exclude-list pre-flight and allow-list
  selection; with an allow list the real getAssertion carries exactly one descriptor.
  Exclude-list pre-flight also runs without a PIN/UV token: probes omit PIN-auth fields and the final
  makeCredential carries only the matching descriptor (or an empty list), retaining required user
  presence. With a token, the probe authenticates a zero client-data hash; registration re-mints
  afterward before the final command.

Adding an extension: a codec, a processor, a line in each session's factory, a property on the typed
input/output records, unit tests for the codec and for the processor against a mocked backend, and a
hardware test if a test key advertises it.

**previewSign Example:**
```csharp
// Registration: request a generated signing key.
var registrationExtensions = new RegistrationExtensionInputs(
    PreviewSign: PreviewSignRegistrationInput.GenerateKey(CoseAlgorithm.Es256));

// Authentication: provide keyHandle, algorithm-specific tbs, and optional raw additionalArgs.
var signingParams = new PreviewSignSigningParams(
    keyHandle: generatedKey.KeyHandle,
    tbs: toBeSigned,
    additionalArgs: algorithmSpecificAdditionalArgs);

var authenticationExtensions = new AuthenticationExtensionInputs(
    PreviewSign: PreviewSignAuthenticationInput.CreateSignByCredential(
        new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>
        {
            [credentialId] = signingParams
        }));
```


````

## `core-001:unit:13efa480557b5601`
`src/WebAuthn/CLAUDE.md:429–461`; original rank 10; New authorized full original WebAuthn narrative quota.

````markdown
## Security Boundary

**Sensitive Data Handling:**
- **PINs:** Never logged, zeroed via `CryptographicOperations.ZeroMemory` after use
- **Private keys:** Authenticator-generated credential/signing private keys are not returned; signing uses CTAP2 commands
- **`tbs` and `additionalArgs` payloads (previewSign auth):** Algorithm-specific bytes; NOT logged; caller is responsible for semantic validation
- **Credential IDs:** Nonsecret protocol identifiers that can expose account identity/correlation; apply logging/redaction/access policy

**Memory Management:**
- Follow root `CLAUDE.md` memory hierarchy (Span > Memory > ArrayPool > Array)
- Zero sensitive buffers with `CryptographicOperations.ZeroMemory`
- Use `ArrayPool<byte>.Shared` for >512 byte temp buffers

**Error Handling:**
- The main attempt maps selected CTAP failures (and previewSign-specific statuses), but GetInfo and
  token acquisition are outside that mapping catch. Output preparation is outside the retry scope so
  an output failure cannot cause repeated credential creation. Lower-level exceptions can escape.
- Caller cancellation propagates `OperationCanceledException`. A resulting timeout cancellation maps
  to `NotAllowed` only when the caller token was not cancelled; success can win the race. The timer
  starts after validation/RP callback, is cooperative, and uses the framework-supported delay range.
- Exclusion maps to `InvalidState`; main authentication NoCredentials/InvalidCredential returns no
  matches. Do not promise universal `WebAuthnClientError` normalization or rollback of side effects.

**Other public boundaries:** Default None attestation removes non-self statements while retaining the
packed/zero-AAGUID/no-certificate self-attestation exception and leaving the returned AAGUID unchanged.
Indirect has no anonymization service; Enterprise does not send the CTAP enterprise-attestation request.
Client-data escaping is manual and has a known exact-CCDToString control-escape gap. Attestation Decode
borrows outer CBOR/statement slices: keep caller input unchanged; parsing is not verification.
`default(Aaguid)` has empty-span and nonreflexive-equality defects; advise explicit construction, not
intentional default semantics. Final descriptor omission can expose an empty ID: CTAP2.0 §5.2 permits
singleton omission, whereas 2.1/2.3 §6.2.2 require it. Never recover an ID by guessing the first of a
multi-entry allow list; an original singleton is unambiguous but still needs RP verification.


````

