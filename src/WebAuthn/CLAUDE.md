# CLAUDE.md - WebAuthn Module

This file provides module-specific guidance for working in **Yubico.YubiKit.WebAuthn**.
For overall repo conventions, see the repository root [CLAUDE.md](../../CLAUDE.md).

## Documentation Maintenance

> **Important:** This documentation is subject to change. When working on this module:
> - **Notable changes** to APIs, patterns, or behavior should be documented in both CLAUDE.md and README.md
> - **New features** (e.g., new extensions, credential types) should include usage examples
> - **Breaking changes** require updates to both files with migration guidance
> - **Test infrastructure changes** should be reflected in the test pattern sections below

## Module Context

The WebAuthn module provides client-side Web Authentication ceremonies over CTAP2. Level 3 informs its
contracts, but current limitations below preclude a blanket conformance claim. It provides:
- **High-level WebAuthn API**: `WebAuthnClient` orchestrates credential registration and authentication
- **Extension Processing**: Internal per-attempt processors translate typed WebAuthn inputs/results to CTAP2 extensions, including the experimental Yubico sign-extension draft v4 (`previewSign`)
- **Backend Abstraction**: `IWebAuthnBackend` (internal) routes operations through `ICtap2Session`
- **Credential Prompting**: Optional `ICredentialPrompt` supplies a PIN on demand; the SDK owns the retry loop

**Key Dependencies:**
- **One-way dependency on Ctap2**: WebAuthn builds on CTAP2 primitives; Ctap2 does not depend on WebAuthn
- **Core**: Shared types, logging, memory management, `ICredentialPrompt`, `IUserPresencePrompt`

**Key Directories:**
```
src/
├── Client/                       # WebAuthn Client API
│   ├── WebAuthnClient.cs                 # ctors, disposal, shared state
│   ├── WebAuthnClient.Registration.cs    # MakeCredential ceremony
│   ├── WebAuthnClient.Authentication.cs  # GetAssertion ceremony
│   ├── WebAuthnClient.Ceremony.cs        # shared attempts, probes, UV escalation
│   ├── WebAuthnClient.PinUvAuth.cs       # PIN/UV token acquisition and prompting
│   ├── WebAuthnClient.Validation.cs      # option validation and CTAP error mapping
│   ├── WebAuthnClientOptions.cs
│   ├── RpIdValidator.cs          # public application-authorization delegate
│   ├── WebAuthnBackend.cs        # internal
│   ├── IWebAuthnBackend.cs       # internal
│   ├── PinUvAuthTokenSession.cs  # internal
│   ├── PinUvSharedSecret.cs      # internal shared-secret owner
│   ├── CeremonyDeadline.cs       # cooperative cancellation timer
│   ├── CredentialFilter.cs       # exclude/allow-list probes
│   ├── WebAuthnClientData.cs
│   ├── WebAuthnOrigin.cs
│   ├── UserVerification/         # UV/PIN decision logic
│   │   └── UvDecision.cs
│   ├── Registration/             # Registration options and responses
│   │   ├── PublicKeyCredentialCreationOptions.cs
│   │   └── RegistrationResponse.cs
│   └── Authentication/           # Authentication options and responses
│       ├── PublicKeyCredentialRequestOptions.cs
│       ├── AuthenticationResponse.cs
│       ├── CredentialMatcher.cs
│       └── MatchedCredential.cs
├── Attestation/                  # Attestation object types
│   └── WebAuthnAttestationObject.cs
├── Extensions/                   # WebAuthn client extension processing
│   ├── Codec/                    # per-extension translation, delegates wire CBOR to Ctap2
│   ├── Inputs/  Outputs/
│   ├── PreviewSign/              # previewSign extension types
│   └── Processing/               # sessions, interfaces, output builders, Processors/
├── Preferences/                  # AttestationPreference, ResidentKeyPreference,
│                                 # UserVerificationPreference
├── WebAuthnAuthenticatorData.cs
└── WebAuthnClientError.cs        # Error handling
```

## Logging

Use Core's `YubiKitLogging.CreateLogger<T>()` when adding logging; there is no `LoggingFactory`.

The WebAuthn source currently contains no logger calls. This does not guarantee that the underlying
CTAP2/Core layers or application logging are silent, and it is not a sensitive-data logging audit.

When adding logs:
```csharp
private static readonly ILogger Logger = YubiKitLogging.CreateLogger<PreviewSignProcessor>();

Logger.LogDebug("Selected algorithm: {Algorithm}", selectedAlgorithm);
```

**Security:** Never log PINs, derived secrets, keys, or sensitive `tbs`/argument payloads. Prefer lengths
and algorithm IDs. Public protocol identifiers can still identify or correlate accounts; apply
redaction/access policy rather than asserting that credential IDs are safe to log.

## Critical Patterns

### WebAuthn Client Usage

The [README demo](README.md#getting-started) is the composable consumer example. The following is an
implementation-oriented fragment: `ctap2Session`, `origin`, prompts and ceremony options are supplied
by the application. Production challenges, account user handles, policies and credential lists come
from the RP; responses still need an application handoff and independent RP verification.

```csharp
// Alternative 1: transfer an existing CTAP2 session on successful construction.
// The application owns RP validity and authorization of each origin/RP pair.
// WebAuthnClientOptions and its RpIdValidator are required. CredentialPrompt remains optional.
await using var client = new WebAuthnClient(
    ctap2Session,
    origin,
    new WebAuthnClientOptions
    {
        RpIdValidator = (requestOrigin, rpId) =>
            requestOrigin.StringValue == "https://example.com" && rpId == "example.com",
        CredentialPrompt = myCredentialPrompt,
        MaxPromptAttempts = 3
    });

// Registration
var createOptions = new PublicKeyCredentialCreationOptions
{
    Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
    User = new PublicKeyCredentialUserEntity(userId, "user@example.com", "User"),
    Challenge = registrationChallenge,
    PubKeyCredParams = [CoseAlgorithm.Es256],
    Extensions = registrationExtensions
};

// Pass pinBytes to supply a PIN directly, or leave it null to use the configured prompt.
var credential = await client.MakeCredentialAsync(createOptions, pinBytes: null);

// Authentication
var requestOptions = new PublicKeyCredentialRequestOptions
{
    Challenge = authenticationChallenge,
    RpId = "example.com",
    AllowCredentials = [new PublicKeyCredentialDescriptor(credential.CredentialId)],
    Extensions = authenticationExtensions
};

var matches = await client.GetAssertionAsync(requestOptions, pinBytes: null);
if (matches.Count == 0)
    return; // Normal no-match path; no response to submit.
var assertion = await matches[0].SelectAsync();
```

Alternatively, replace the constructor above with the device factory; do not append another live
client/session for the same key:

```csharp
await using var client = await yubiKey.CreateWebAuthnClientAsync(
    origin,
    clientOptions: new WebAuthnClientOptions
    {
        RpIdValidator = rpValidationService.IsRpValidForOrigin,
        CredentialPrompt = myCredentialPrompt
    },
    sessionOptions: new SessionCreationOptions
    {
        PreferredConnectionType = ConnectionType.SmartCard,
        ScpKeyParameters = scpKeyParameters
    });
```

Use `matches.Count` to distinguish zero, one, and multiple matches. `MatchedCredential` deliberately has no
`RequiresSelection` property; selection is a property of the returned list. A nonempty allow list is
probed and narrowed to one descriptor, not exhaustively enumerated; an absent allow list can yield multiple
discoverable assertions. Device work and extension outputs finish before the public call returns.
`SelectAsync` retrieves the cached response without device I/O; its internal lazy task wrapper is
idempotent and cancellation-aware while waiting, not lazy authenticator work. Registration and
authentication extension outputs are non-positional records with optional init properties that default to null.

The device factory takes two independent configuration carriers, and mixing them up is the easy mistake:
required `clientOptions` (`WebAuthnClientOptions`) is forwarded to the created `WebAuthnClient`, while optional
`sessionOptions` (`SessionCreationOptions`) is forwarded unchanged to `CreateCtap2SessionAsync`.
The origin and client options are required; `RpIdValidator` is required within the client options,
while `CredentialPrompt` remains optional. The earlier standalone public-suffix checker and overload ending
in `scpKeyParams`, `configuration`, and `cancellationToken` were removed during the coordinated 2.0 breaking window.

### Registration transport metadata

`MakeCredentialCoreAsync` already obtains `AuthenticatorInfo` through `GetCachedInfoAsync`. Pass that
same info into response construction and snapshot its string transport list as `WebAuthnTransport`
values, deduplicated and sorted with `StringComparer.Ordinal`, in independently owned read-only storage.
Do not query GetInfo again, infer transports from the live connection, or introduce a CTAP2 dependency
on WebAuthn. CTAP2's transport model stays a string list, preserving unknown names.

`RegistrationResponse.Transports` is non-nullable and defaults to `[]`; absent GetInfo metadata yields
an empty response list. These are advertised authenticator hints for the relying party, not a host
reachability check or session transport-selection policy. The public client/fake-session regression
tests in `Client/RegistrationTransportTests.cs` cover cached-info reuse, all known and future names,
missing metadata, ordinal deduplication, and snapshot independence.

Migration: replace `WebAuthnTransport.Unknown(value)` with `WebAuthnTransport.FromValue(value)` for both
known and unknown strings; constants and exact string equality are unchanged. Replace response null
checks/initializers with empty-list handling. Descriptor input optional semantics are unchanged.

Reference precedent: [Android cached-info forwarding](https://github.com/Yubico/yubikit-android/blob/76b89d35142d9a4937b6b72e5c840e057a280620/fido/src/main/java/com/yubico/yubikit/fido/client/Ctap2Client.java#L240-L244).
The [WebAuthn response contract](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#dom-authenticatorattestationresponse-gettransports)
requires unique, lexicographically ordered names, empty when unavailable; [CTAP GetInfo transports](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#getinfo-transports)
requires tolerance of unknown values.

### Credential Prompting

A PIN reaches the client one of two ways: the caller passes `pinBytes`, or the client was
constructed with `WebAuthnClientOptions.CredentialPrompt` (an `ICredentialPrompt` from
`Yubico.YubiKit.Core.Credentials`) and asks for one when the ceremony needs it. There is no
other secret-input callback; presence notifications use the separate session prompt.

```csharp
public interface ICredentialPrompt
{
    ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(
        CredentialPromptContext context,
        CancellationToken cancellationToken);
}
```

Contract:

- Returning `null` means **the user declined**, and nothing else. It is not a channel for
  "cancelled" or "failed" — cancellation throws, and failures throw.
- `RequestSecretAsync` returns its `ValueTask` promptly, never blocks the calling thread while
  obtaining the secret, and honors the supplied cancellation token. `WaitAsync` bounds only the
  SDK's wait after the task has been returned; it cannot bound synchronous work inside the call.
- The returned `Memory<byte>` must be **exactly** the secret's length. Use
  `DisposableArrayPoolBuffer.CreateFromSpan`, whose `Memory` already slices exactly; a
  pool-sized buffer with trailing slack will be read as part of the secret.
- The prompt owns a returned buffer until its task completes successfully. Ownership then transfers
  to the SDK. A null, faulted, or cancelled result transfers no ownership.
- **The SDK owns the retry loop.** On a rejected PIN it zeroes the secret, never resubmits it, and
  calls the prompt again with a fresh `CredentialPromptContext` carrying `IsRetry = true` and the
  refreshed `RetriesRemaining`. Implementations must not retry internally.
- `WebAuthnClientOptions.MaxPromptAttempts` (default 3) limits SDK prompt attempts and is rejected
  at options construction if it is not positive. The authenticator separately enforces and reports
  its own retry state.
- WebAuthn additionally zeroes and disposes late-returned PIN buffers after cancellation when the
  prompt task eventually completes. There is no Core-wide tracking guarantee for a prompt that
  never completes; implementations must honor cancellation.

`CredentialPromptContext` carries `Kind`, `Scope` (the RP ID), `RetriesRemaining`, `IsRetry`,
`MinLengthCodePoints`, `MaxLengthBytes`, and `RequiresConfirmation`. CTAP reports its minimum in
Unicode code points while its encoded PIN limit is expressed in UTF-8 bytes.

### User Verification Decision

`UvDecisionLogic.Decide` (`src/Client/UserVerification/UvDecision.cs`) answers two questions in
order, and keeping them separate is the point: **whether** the ceremony needs a PIN/UV auth token,
and only then **which** method supplies it.

"Configured" throughout means the option is advertised **and** enabled. A YubiKey with no PIN set
still advertises `clientPin: false`, so testing for the key's presence rather than its value reads
as "verification is available" on a key that has none.

| Preference | Needs a token? |
|---|---|
| `Required` | Always. Throws `NotAllowed` if nothing can satisfy it. |
| `Preferred` | Tries configured verification when a method is obtainable; can continue without a token when it is not. |
| `Discouraged` | No — unless verification is configured AND one of: `alwaysUv` is enabled; this is a makeCredential and `makeCredUvNotRqd` is not enabled (pre-CTAP-2.1 keys); or the request needs a permission beyond makeCredential/getAssertion (e.g. `LargeBlobWrite`). |

WebAuthn L2 [§5.8.6](https://www.w3.org/TR/2021/REC-webauthn-2-20210408/#enumdef-userverificationrequirement)
defines preference intent. The enabled-option checks, PIN-first method choice and availability fallback
are SDK policy, not a quotation of that section. Configured `clientPin` without PIN bytes/prompt is not
an obtainable method. The backend currently hard-codes PIN/UV protocol 2 for token acquisition and PRF
key agreement; legacy command fallback does not negotiate protocol 1. PINless operations that need
neither may still work. Built-in UV selection currently lacks a full token-capability check, so do not
promise that advertised `uv` alone guarantees token acquisition.

Backstop: if the client's model of the authenticator is wrong and the key answers
`CTAP2_ERR_PUAT_REQUIRED` (0x36), `ShouldRetryWithRequiredUv` escalates to `Required` and retries the
ceremony once.

Both ceremonies union extension permissions before the decision; largeBlob writes contribute
`LargeBlobWrite`. The SDK requires a token for those writes, even though CTAP permits an
unconfigured-authenticator exception. PRF contributes no extra permission and currently preserves the
caller UV preference: this fails to enforce WebAuthn L3 §10.1.4's verified-PRF override. Recommend
Required for evaluation without calling it a full conformance or cleanup fix.

### User-presence notification

WebAuthn ceremonies remain plain awaitable methods with no progress stream. A CTAP2 session created
with `SessionCreationOptions.UserPresencePrompt` now provides touch notifications underneath
`MakeCredentialAsync` and `GetAssertionAsync`: HID reports the authoritative CTAPHID wait signal,
while SmartCard reports the effective required-UP policy before its APDU. Device-factory callers pass
the prompt in the `SessionCreationOptions` already forwarded to CTAP2; callers supplying an existing
`ICtap2Session` must configure the prompt when that session is created.

The prompt is informational, not a user-verification input. To abandon a ceremony, cancel its token.
A PIN still comes only from `pinBytes` or `WebAuthnClientOptions.CredentialPrompt`.

### RP ID Validation

`WebAuthnClient.Validation.cs` checks only nonempty, faithfully UTF-8 encodable RP text,
then invokes the required synchronous `Client/RpIdValidator.cs` delegate
with the configured origin and unchanged RP ID. Both ceremonies do this once before any backend
call, including GetInfo, PIN/UV, extensions, and credential probing. Approval is not overridden by
SDK domain/URI syntax, IDNA, domain-suffix, or Public Suffix List policy. The callback owns RP validity
(including syntax) AND caller/RP authorization. False and non-cancellation exceptions map to
`Security` (exception retained); callback cancellation propagates and blocks device I/O. Null/empty
RP IDs and invalid UTF-16 map to `InvalidRequest` without invoking the callback. The callback executes before
the deadline starts and must be prompt/local; shared callbacks must handle concurrency.

**Approved boundary:** callback owns RP validity. Do not add a domain/URI parser, IDNA converter,
PSL check, or hidden browser syntax policy. Every RP ID passing the wire checks reaches the callback
unchanged, including `x:credential`, `http:credential`, IDNs, and malformed domain strings. A permissive
callback does not establish browser conformance. Exact-RP and origin/RP allowlists are complete policies
when their trusted entries are validated; an application-service validator must validate both RP syntax
and caller/RP binding under the applicable specification. CTAP message/credential limits remain in CTAP2.

**YESDK-1623 / audit 15 migration:** `PublicSuffixChecker`, `EnterpriseRpIds`, and
`WebAuthnOrigin.IsRpIdValid` are removed. Applications must provide RP validity and their complete binding policy
through `WebAuthnClientOptions.RpIdValidator`, including browser public-suffix/related-origin rules
if applicable. Carry existing PIN prompt/attempt settings forward; do not just invert the old checker.
Callbacks from an earlier development build that checked only authorization must also own RP syntax
validity; the SDK's former independent syntax gate was removed.
Exact-RP, origin/RP allowlist, and existing application-service examples are in [README.md](README.md#rp-authorization-belongs-to-the-application).

WebAuthn L3 [RP ID](https://www.w3.org/TR/webauthn-3/#rp-id) permits other native binding models but
still requires domain-string or URI RP syntax. python-fido2's pinned
[`ClientDataCollector`](https://github.com/Yubico/python-fido2/blob/9d5991d6785e20bd487c8b6c996df6ce2226642a/fido2/client/__init__.py)
delegates authorization to application logic. No SDK policy framework or network lookups are added.
`WebAuthnOrigin.TryParse` is unchanged: HTTPS or HTTP localhost / *.localhost only, serialized as
scheme://host[:port], stripping path/query/fragment. Native/opaque/custom-scheme origins remain
unrepresentable; accepting a URI RP ID does not expand this origin boundary.

The callback is a client-side authorization boundary, not RP-server verification. The configured
origin still enters signed client data; servers independently check origin, challenge/type, RP ID hash,
signature/attestation policy, UP/UV, cross-origin context, and credential/account binding.

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

## previewSign Extension

**Purpose:** Generate a separate signing key associated with a WebAuthn credential, then use its handle
for algorithm-specific signing during authentication. This is the experimental
[Yubico WebAuthn sign-extension draft v4, §10.2.1](https://yubicolabs.github.io/webauthn-sign-extension/4/#sctn-sign-extension),
not CTAP v4 or an adopted W3C signing standard.

**Wire Format:** previewSign extension identifier `previewSign`.

**Key Features:**
- **Registration:** Generates a new signing key pair, returns public key + keyHandle
- **Authentication:** Signs algorithm-specific `tbs` (to-be-signed) bytes using keyHandle and optional raw `additionalArgs`
- **Multi-credential probe:** A nonempty allow list needs one signing-parameter map entry per descriptor; the probe-selected credential chooses its parameters. The public input normalizes dictionary comparison by byte content; keep key buffers unchanged during the operation.
- **Algorithm agility:** `tbs` and `additionalArgs` are algorithm-specific values and are passed through unchanged

**Current Status:**
- Both paths are capability-gated. Unsupported requests are omitted, not guaranteed to throw `NotSupported`; callers must check optional outputs.
- Registration flags derive from the ceremony UV preference: Required selects UP+UV, other preferences select UP. The public flags enum is not a request setter.
- `FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature` exercises registration, ARKG derivation, signing and offline verification when run on suitable hardware. Do not infer device/firmware qualification from the test's existence.
- Present `additionalArgs` must be an algorithm-appropriate CBOR `COSE_Sign_Args` map; TBS/prehashing is algorithm-specific. Raw pass-through does not validate semantics. Selected signing `Algorithm` may differ from the public key's `alg`.
- Generated inner attestation is parsed/repacked, not trust-verified. The RP must verify it when required by its policy. Empty generated handles are permitted by the draft but rejected by the current signing-params constructor.
- ARKG helpers: **WARNING -- EXPERIMENTAL --** public experimental conveniences; not the generic previewSign API contract; not ready for production use and must not be treated as production cryptographic guidance

**Key Files:**
- `src/Extensions/Processing/Processors/PreviewSignProcessor.cs` — per-attempt orchestration
- `src/Extensions/Codec/PreviewSignCodec.cs` — WebAuthn/CTAP2 translation
- `../Ctap2/src/Extensions/PreviewSign/` — canonical CTAP2 types and encoder
- `src/Extensions/PreviewSign/PreviewSignAuthenticationInput.cs` — WebAuthn authentication input
- [Versioned vendor draft](https://yubicolabs.github.io/webauthn-sign-extension/4/#sctn-sign-extension) — input/output and actor requirements

**Architectural Note:** `Yubico.YubiKit.Ctap2.Extensions.PreviewSignCbor` owns wire CBOR encoding.
WebAuthn's processor/codec translates client types and delegates rather than duplicating that encoder.
Generic signing params carry raw `additionalArgs`; ARKG helpers can be converted with
`PreviewSignCbor.EncodeAdditionalArgs(...)`. Those helpers remain experimental, not production guidance.

### Extension policy and result-lifetime limitations

Keep these conditions attached to public guidance when changing extension flow:

- PRF does not enforce the verified-variant UV override. Its SDK-produced read-only results and
  intermediate copies have no disposal/zeroing guarantee; clearing a caller copy or disposing the
  client does not clear them. PRF results are derived secrets, not public metadata.
- credProtect enforcement is not implemented, including unsupported-device handling. CTAP2.3 §12.1
  assigns the platform a SHOULD NOT for creating without requested policies 2/3 when enforcement is
  true; forwarding the integer or inspecting output after creation is not enforcement.
- LargeBlob uses largeBlobKey/storage-array operations, not the alternative direct extension.
  Required support can still yield an unsupported created credential; effective RK and response-key
  validation need an implementation fix. Writes require exactly one original allowed credential under
  WebAuthn, but the client currently does not check this. Reads can fail response construction after a
  successful assertion. Do not equate nullable Blob with one proven absence/support/error state; the
  nullable-array conversion turns missing data into a present empty memory value; nullable presence and
  length do not distinguish it from a stored empty blob.
  Decrypted blob outputs lack an owned cleanup contract.
- A nonnull minPinLength input requests the extension even if Requested is false; null omits it.
  The value reports existing policy only for authorized RP IDs, not a policy-setting operation.
- Null input containers yield null result containers; requested containers can yield records with null
  individual outputs. Explicit false enablement/support/write results have their own meanings.

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

## Test Infrastructure

### Integration Tests

**Location:** `tests/Yubico.YubiKit.WebAuthn.IntegrationTests/`

**Test Helpers:**
- `WebAuthnTestHelpers.NormalizePinAsync(Ctap2Session)` — Uses fixed `KnownTestPin`, sets/verifies it and handles ForcePinChange; does not accept a caller PIN or cancellation token. Requires explicit approval for that device state.
- `WebAuthnTestHelpers.DeleteAllCredentialsForRpAsync(Ctap2Session, string rpId, CancellationToken)` — Cleanup helper

**Traits:**
- `[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]` — Tests requiring touch
- `[Trait(TestCategories.Category, TestCategories.Slow)]` — RSA 3072/4096 keygen or >5s tests (skipped by `--smoke`)

**Coordination lanes:**

| Lane | Examples | Agent-runnable? | Rule |
|------|----------|-----------------|------|
| Unit/fake-backend | WebAuthn client, origin, extension processor unit tests | Yes | Run through `dotnet toolchain.cs -- test --project WebAuthn` |
| Integration smoke without UP | factory/session checks that do not ask for touch | Yes | Use `--smoke` or `Category!=RequiresUserPresence` |
| User Presence | registration/authentication ceremonies, previewSign hardware checks | No by default | Mark with `Category=RequiresUserPresence`; run only with a human present |
| User Verification / PIN | PIN normalization, UV-required/preferred flows | No by default | Requires explicit human approval and known PIN/device state |
| Reset/destructive cleanup | reset or broad persistent credential deletion | No | Human-approved destructive run only |

Agents must not run WebAuthn User Presence, UV/PIN, reset, insert/remove, or destructive hardware checks unless a human explicitly approves the exact command and is physically present for the interaction.

**Key Pattern:**
```csharp
[SkippableTheory]
[WithYubiKey]
[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
public async Task Registration_WithPreviewSign_ReturnsGeneratedSigningKey(YubiKeyTestState state)
{
    // Test setup mutates PIN state; run only with the approval described above.
    await using (var setupSession = await state.Device.CreateCtap2SessionAsync())
    {
        await WebAuthnTestHelpers.NormalizePinAsync(setupSession);
    }

    if (!WebAuthnOrigin.TryParse(WebAuthnTestHelpers.TestOriginUrl, out var origin))
        throw new InvalidOperationException("Unsupported test origin.");
    await using var client = await state.Device.CreateWebAuthnClientAsync(
        origin,
        new WebAuthnClientOptions
        {
            RpIdValidator = (requestOrigin, rpId) =>
                requestOrigin.StringValue == WebAuthnTestHelpers.TestOriginUrl && rpId == WebAuthnTestHelpers.TestRpId
        });

    // Test logic...
}
```

**Running Tests:**
```bash
# All WebAuthn unit tests
dotnet toolchain.cs -- test --project WebAuthn

# Integration tests (no UP)
dotnet toolchain.cs -- test --integration --project WebAuthn --filter "Category!=RequiresUserPresence"

# Integration tests (with UP, human-coordinated only)
dotnet toolchain.cs -- test --integration --project WebAuthn --filter "Category=RequiresUserPresence"

# Smoke tests only (skip Slow and RequiresUserPresence)
dotnet toolchain.cs -- test --integration --project WebAuthn --smoke
```

## Peer Module Pointers

**Related Modules:**
- **Yubico.YubiKit.Ctap2** — Lower-level CTAP protocol; WebAuthn builds on top of `ICtap2Session`
- **Yubico.YubiKit.Core** — Shared types, logging (`YubiKitLogging`), memory utilities

**Integration:**
- WebAuthn → Ctap2 (depends on)
- Ctap2 does not depend on WebAuthn

## Known Gotchas

1. **previewSign maps cover the allow list** — require one parameter entry per allowed descriptor and select by the recognized credential, including multi-entry requests
2. **Extension inputs must reach the backend** — Assert on encoded extension maps because a missing processor call can silently omit an extension
3. **`flags` optional in previewSign registration output** — Some authenticators return only key 3 (algorithm)
4. **No LoggingFactory** — Use `YubiKitLogging.CreateLogger<T>()` from Core
5. **CBOR key constants are context-specific** — key `7` means authentication `additionalArgs` in GetAssertion input and attestation object in MakeCredential unsigned output; keep parsing paths context-specific
6. **The client never asks for anything except a PIN** — input comes from `pinBytes` or
   `WebAuthnClientOptions.CredentialPrompt`; abandonment comes from the cancellation token.
7. **User presence and user verification are distinct** — final commands retain required-UP policy.
   Presence need not mean a fresh physical button touch on every transport. The SDK omits makeCredential
   `up` for CTAP2.0 compatibility; CTAP2.1/2.3 allow true, not false. Silent exclude/allow-list probes
   use `up:false` in `Client/CredentialFilter.cs`. Notifications prove neither a gesture nor authorization.
8. **Touch notification comes from the CTAP2 session** — configure
   `SessionCreationOptions.UserPresencePrompt`; `WebAuthnClientOptions` does not duplicate it.
