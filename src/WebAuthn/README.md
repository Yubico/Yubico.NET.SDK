# Yubico.YubiKit.WebAuthn

This package provides client-side WebAuthn registration and authentication over `Yubico.YubiKit.Ctap2`.
`WebAuthnClient` builds client data, orchestrates CTAP2 commands and typed extensions, and acquires PIN/UV
tokens when needed. The application supplies a supported origin and a required `RpIdValidator` that owns
RP validity (including syntax) and authorization for that origin. The SDK's RP wire checks require only
nonempty text that can be encoded faithfully as UTF-8. The relying party (RP) must verify each response
before treating an account as registered or signed in.

WebAuthn owns client ceremonies and origin parsing; `Yubico.YubiKit.Ctap2` owns direct
authenticator operations. FIDO2 remains the authenticator application identity, and FIDO HID remains
the transport name.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Requirements

- .NET 10. On Linux, install PC/SC and the udev rules described in [linux-setup.md](../../docs/linux-setup.md).
- YubiKey 5 series, Security Key series, or YubiKey Bio series with firmware 5.0.0 or later. This module adds no
  firmware gates of its own; [../Ctap2/README.md](../Ctap2/README.md) lists SDK firmware thresholds, not
  a qualified per-model support matrix. Revision/extension advertisement, configured state and permission
  requirements still apply independently of firmware metadata.
- Transports: HID FIDO over USB by default, or SmartCard where the connected authenticator exposes the
  FIDO AID. NFC requires a current PC/SC connection reporting `Transport.Nfc`; USB SmartCard uses the
  CTAP2 SDK's 5.8.0+ gate. NFC capability metadata alone does not establish the current transport.
- The supported origin representation is HTTPS, or HTTP on `localhost` / `*.localhost`; this parser is not
  a complete browser Secure Contexts evaluation. Native/opaque/custom-scheme origins are not supported.
- PIN/UV token acquisition and PRF key agreement currently use PIN/UV protocol 2, without protocol-1
  negotiation. This restriction applies to those operations, not every PINless ceremony. Built-in UV
  token acquisition also depends on the authenticator's token capabilities.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.WebAuthn --prerelease
```

`Yubico.YubiKit.Core` is installed transitively, as is `Yubico.YubiKit.Ctap2`.

## Getting started

In production, obtain creation/request options from your RP through your application's protocol. The RP
generates a fresh, unpredictable challenge for each ceremony, retains it for verification, and supplies
the RP ID, algorithm and UV policies, and credential allow/exclude lists. For registration, it also
supplies a stable, opaque, account-specific user handle (nonempty, at most 64 bytes, without personal
information). The client neither creates an RP account nor validates challenge freshness on the server.

The following code block is **one local hardware demo**, not an RP integration. It uses local challenges
and a new demo account handle, creates a persistent discoverable credential, and retrieves an assertion
for it. Use a dedicated test key with the illustrated PIN already configured, or replace `demoPin` with
your own securely acquired UTF-8 PIN. The demo does not set a PIN, verify server responses, or delete the
credential. Configure any prompt alternatives below **before** the factory call; do not open another
client while this one is alive.

```csharp
using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.WebAuthn;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Preferences;

IYubiKey device = await YubiKeyManager.FindFirstAsync();
if (!WebAuthnOrigin.TryParse("https://example.com", out var origin))
    throw new InvalidOperationException("The origin is not supported.");

// A complete, deliberately narrow allowlist: this origin may use exactly this known-valid RP ID.
var clientOptions = new WebAuthnClientOptions
{
    RpIdValidator = (requestOrigin, rpId) =>
        requestOrigin.StringValue == "https://example.com" && rpId == "example.com"
};
await using var client = await device.CreateWebAuthnClientAsync(origin, clientOptions);

// Local demo inputs only. Production options and user handles come from the RP.
var registrationOptions = new PublicKeyCredentialCreationOptions
{
    Challenge = RandomNumberGenerator.GetBytes(32),
    Rp = new PublicKeyCredentialRpEntity("example.com", "Example Corp"),
    User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "Example User"),
    PubKeyCredParams = [CoseAlgorithm.Es256],
    ResidentKey = ResidentKeyPreference.Required,
    UserVerification = UserVerificationPreference.Required,
    Extensions = new RegistrationExtensionInputs(CredProps: new CredPropsInput())
};
byte[] demoPin = Encoding.UTF8.GetBytes("11234567"); // Dedicated demo key's existing PIN; never embed a production PIN.
try
{
    var registration = await client.MakeCredentialAsync(registrationOptions, pinBytes: demoPin);

    // Keep the original result: do not register again just to obtain metadata.
    string[] transportNames = [.. registration.Transports.Select(transport => transport.Value)];
    bool? residentKey = registration.ClientExtensionResults?.CredProps?.ResidentKey;
    Console.WriteLine($"Created a credential; discoverable: {residentKey}.");

    var requestOptions = new PublicKeyCredentialRequestOptions
    {
        Challenge = RandomNumberGenerator.GetBytes(32), // A separate challenge for this demo assertion.
        RpId = "example.com",
        AllowCredentials = [new PublicKeyCredentialDescriptor(registration.CredentialId)],
        UserVerification = UserVerificationPreference.Required
    };
    var matches = await client.GetAssertionAsync(requestOptions, pinBytes: demoPin);
    if (matches.Count == 0)
    {
        Console.WriteLine("No matching credential was returned.");
        return;
    }

    // This single-entry allow-list demo needs no account picker.
    var assertion = await matches[0].SelectAsync();
    Console.WriteLine("Retrieved an assertion; RP verification is still required.");
}
finally
{
    CryptographicOperations.ZeroMemory(demoPin);
}
```

### Complete registration and sign-in with your RP

For registration, send `registration.CredentialId`, the **exact** `registration.ClientData.JsonBytes`,
`registration.RawAttestationObject`, transport names, and requested client extension results to your
RP using its response format. The RP verifies the response and stores the verified credential record,
including the public key and account binding, with the transport hints. An unchecked local credential
ID alone is not a registered account credential.

For authentication, send `assertion.CredentialId`, the exact `assertion.ClientData.JsonBytes`,
`assertion.RawAuthenticatorData`, `assertion.Signature`, the user handle from `assertion.User` when
provided, and requested client extension results. The RP verifies these against its stored credential
and pending challenge before completing sign-in. These variable names refer to the results inside the
demo's `try` block; place your application's handoff there at each step. The SDK does not provide an
upload client or a WebAuthn-response JSON serializer. Preserve byte values using the encoding your RP
expects; do not regenerate client data from its parsed properties.

The [server-check checklist](#client-checks-do-not-replace-rp-server-checks) applies independently of
the local `RpIdValidator`. Decoding a response is not signature or attestation verification.

For a capability-only task, use a separate CTAP2 session's `GetInfoAsync` instead of starting a
WebAuthn ceremony. Dispose the WebAuthn client before opening that session on the same physical key.

### Registration transport hints

`RegistrationResponse.Transports` is a non-null, ordinal-sorted, unique snapshot of the authenticator's
advertised GetInfo transport names. It is empty when that metadata is unavailable. The client reuses
the ceremony's cached GetInfo response: it does not make an extra query or infer names from the active
connection. Unknown and future names are preserved by the string-backed `WebAuthnTransport` type.

Store these hints alongside the credential ID for the relying party. They describe advertised
authenticator support, not which transports this host can currently reach, and do not switch the
session's connection. For example, an authenticator may advertise `nfc` while registration uses USB
on a host with no NFC reader.

The demo reads `transportNames` from the original registration. Use
`WebAuthnTransport.FromValue("future-link")` to represent an unknown name without normalization.

### Choose an authentication response

With a nonempty allow list, `GetAssertionAsync` probes in device-sized batches and narrows the real
request to one recognized descriptor; it does not enumerate every allowed credential on the key.
With no allow list, it can enumerate discoverable assertions for the RP, subject to authenticator
selection behavior. Handle zero matches before indexing. For one match, retrieve its response with
`SelectAsync`; for several, show your application's account picker and select the chosen match.
User display fields may be absent, especially without UV; do not use a display name for authorization.

Assertions and extension outputs are computed **before** `GetAssertionAsync` returns. `SelectAsync`
retrieves a cached response, is idempotent and supports cancellation while waiting; it performs no
further device I/O. Cancelling a picker does not undo an assertion or an extension write.

The current client can expose an empty ID if the final response omits its credential descriptor.
CTAP2.0 §5.2 permits omission for a singleton allow list; CTAP2.1/2.3 §6.2.2 require the descriptor.
This is a client recovery limitation, not a usable empty credential ID. Only an **original single-entry**
request gives the application an unambiguous requested ID to retain for its RP protocol. Never guess
from the first entry of a multi-entry list, and always require RP verification and account binding.

`RegistrationExtensionOutputs` and `AuthenticationExtensionOutputs` are non-positional records. Their init
properties are optional and default to null; construct them with named object initializers.

### Let the client ask for the PIN

As an alternative to borrowed PIN bytes, set `WebAuthnClientOptions.CredentialPrompt` and leave
`pinBytes` null in ceremony calls. The client asks only when a ceremony needs a PIN and owns the retry
loop. `ReadPinAsync` below is an application-supplied input routine, which `ConsoleCredentialReader`
can back; it must honor cancellation and return only the UTF-8 secret bytes.

```csharp
internal sealed class PinPrompt(Func<CredentialPromptContext, CancellationToken, Task<byte[]>> readPin) : ICredentialPrompt
{
    public async ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(
        CredentialPromptContext context, CancellationToken cancellationToken)
    {
        byte[] pin = await readPin(context, cancellationToken);
        try { return DisposableArrayPoolBuffer.CreateFromSpan(pin); }   // exactly sized; ownership transfers
        finally { CryptographicOperations.ZeroMemory(pin); }
    }
}
```

```csharp
// Alternative configuration: insert before the demo's single CreateWebAuthnClientAsync call.
clientOptions = clientOptions with { CredentialPrompt = new PinPrompt(ReadPinAsync) };
```

## User interaction

Both ceremonies use required user presence for the final command; a USB YubiKey normally asks for a
touch. Presence does not universally mean a fresh button gesture across NFC/contact transports or
built-in verification. The client omits the makeCredential `up` option for compatibility: CTAP2.0
rejects it, whereas later versions allow `up:true` but not `up:false`.

Notifications come from the CTAP2 session. HID reports the live CTAPHID wait signal; SmartCard reports
the effective required-UP policy before the APDU. Neither notification is proof that a gesture occurred
or that the RP authorized an operation. Pass your `IUserPresencePrompt` in `sessionOptions` when opening
the one client:

```csharp
sealed class TouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) =>
        new(Console.Out.WriteLineAsync($"Touch your YubiKey for {context.Application} {context.Scope}."));
}
```

```csharp
// Alternative factory call: replace the demo's call; do not append another live client.
await using var client = await device.CreateWebAuthnClientAsync(
    origin, clientOptions, sessionOptions: new SessionCreationOptions { UserPresencePrompt = new TouchPrompt() });
```

User verification is separate from presence. `UserVerification` expresses a preference; authenticator
policy and extension permissions also affect the SDK's PIN/UV-token decision:

| Preference | Behavior |
|---|---|
| `Required` | Requires a token; fails with `NotAllowed` when no available PIN or built-in UV method can satisfy it. Lower-level acquisition failures can also propagate. |
| `Preferred` (default) | Tries configured, available verification; can proceed without a token if no method can be supplied. |
| `Discouraged` | Normally avoids a token. Configured verification can still be required by `alwaysUv`, older makeCredential policy, or privileged extension permissions such as `LargeBlobWrite`. |

Extension permissions are combined with ceremony permissions before this decision. The client can
escalate once to Required after `PUAT_REQUIRED`. A configured PIN is not an obtainable PIN unless the
caller supplies it or a prompt can acquire it. PRF has a separate UV limitation described below.

Registration checks nonempty `ExcludeCredentials` lists in device-sized batches, even when verification is
discouraged and no PIN token is used. The final registration retains required user presence before reporting that a
credential is excluded.

### Cancellation and failures

Pass the ceremony's third argument by name when omitting the PIN:
`client.GetAssertionAsync(requestOptions, cancellationToken: cancellationToken)`.
`Timeout` on either options record has no SDK default or clamping and must be positive and within the
framework-supported delay range. It starts after local validation and the RP callback, and signals
cooperative cancellation, not a hard completion deadline. HID can send `CTAPHID_CANCEL` during a
keep-alive and drain a terminal response; admitted SmartCard exchanges finish before returning.
A completed success can win the cancellation race. Cancellation does not roll back credential creation
or extension writes.

An empty match list is normal. Excluded registration credentials map to `InvalidState`; unavailable
required verification and an observed ceremony timeout map to `NotAllowed`. Caller-token cancellation
propagates `OperationCanceledException`, not the client's `Cancelled` error code. Authenticator timeout
statuses instead map to `Constraint`. Capability reads, token acquisition, transport/setup, and extension
output processing can propagate lower-level exceptions: not every failure is `WebAuthnClientError`.
A failure does not trigger a retry on another transport. See
[`WebAuthnClientError`](src/WebAuthnClientError.cs) for the error vocabulary.

## Use extensions

Set `Extensions` on the creation or request options before starting a ceremony. The demo requests
`new RegistrationExtensionInputs(CredProps: new CredPropsInput())` and reads
`registration.ClientExtensionResults?.CredProps?.ResidentKey`. `credProps` is client-derived: it reports
the effective discoverable-credential setting used for registration, not a second authenticator query.
A nullable property means unknown when absent; do not turn absence into `false` without deciding that
policy in your application.

| Task | Registration input | Authentication input |
| --- | --- | --- |
| Inspect discoverable-credential selection | `CredProps` | — |
| Request credential protection | `CredProtect` | — |
| Store/retrieve a small credential blob | `CredBlob` | `GetCredBlob` |
| Read the authorized minimum PIN-length policy | `MinPinLength` | — |
| Enable/use large-blob storage | `LargeBlob` support | `LargeBlob` read or write |
| Enable/evaluate a credential-derived PRF | `Prf` (evaluation needs `hmac-secret-mc`) | `Prf` evaluation |
| Generate/use a separate signing key (experimental) | `PreviewSign` | `PreviewSign` |

Inputs are requests, not guarantees of device support. With no input container, the result container is
null; with a container, individual results can still be null because support or output was absent, or
parsing was suppressed. This differs from explicit results such as `Prf.Enabled == false`,
`LargeBlob.Supported == false`, or `Written == false`. An empty credential blob is also distinct from
no extension result. Not every extension error is ignored. Consult the
[typed inputs](src/Extensions/WebAuthnExtensionInputs.cs),
[outputs](src/Extensions/WebAuthnExtensionOutputs.cs), and the
[processing architecture](../../docs/architecture/webauthn-extension-processing.md) for phase and
error details. The processors are internal implementation components, not a public plugin API.

### Evaluate PRF inputs deliberately

The WebAuthn PRF maps application input to domain-separated `hmac-secret` salts. `First` and optional
`Second` supply the default evaluation inputs; `Second` alone is not evaluated. Authentication can
override these per credential with `EvalByCredential`, whose keys are unpadded base64url credential
IDs and whose nonempty map requires an allow list. Registration does not accept that map when the PRF
processor runs; registration-time evaluation additionally needs advertised `hmac-secret-mc` support.
Check enablement and evaluation results separately.

**Current limitation:** [WebAuthn L3 §10.1.4](https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#prf-extension)
requires the hmac-secret-backed PRF to use the verified variant, overriding the UV preference if needed.
This client does not enforce that override. Request `UserVerification = UserVerificationPreference.Required`
for evaluation; otherwise actual UV changes can change the output for the same credential and input.
Selecting Required alone does **not** establish full PRF conformance or solve result cleanup.

PRF outputs are derived secrets: never log them. SDK-produced results expose read-only memory without
a disposal/zeroing guarantee for returned arrays or intermediate copies; disposing the client does not
clear them. Clearing a copy you made does not clear the SDK's originals. Treat PRF inputs according to
their application sensitivity as well. This lifetime limitation matters when evaluating suitability
for secret-handling applications; there is no writable-result-span API to use for guaranteed cleanup.

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

### Evaluate experimental signing

`previewSign` implements the [Yubico WebAuthn sign-extension draft v4](https://yubicolabs.github.io/webauthn-sign-extension/4/#sctn-sign-extension),
not a CTAP version or an adopted W3C signing standard. Registration generates a **separate signing
key**, not a way to export or repurpose the WebAuthn credential's private key. Authentication requires
a nonempty allow list and one signing-parameter map entry per allowed credential; multi-entry lists
are supported, and the probe-selected credential chooses the corresponding parameters. A normal
public dictionary is sufficient; the input normalizes credential-ID comparison by byte content.

Unsupported previewSign requests are omitted, so inspect the optional result before treating signing
as performed. Some explicit output errors propagate while CBOR/invalid-operation parsing failures
can omit the result. Supply the selected algorithm's TBS representation (including prehashing if
required), and encode present `additionalArgs` as its CBOR `COSE_Sign_Args` map; pass-through does not
validate algorithm semantics. Verify generated-key attestation under your RP policy before trusting
it. The selected signing algorithm can differ from the public key's algorithm identifier. The draft
permits empty generated handles, but the current signing-params constructor rejects them. ARKG helper
conveniences are experimental and not production cryptographic guidance or the generic signing contract.

## RP authorization belongs to the application

`RpIdValidator(WebAuthnOrigin origin, string rpId)` is synchronous and required. It is called once
for each registration and authentication, before any ceremony device I/O (including capability reads,
credential probes, and PIN/UV acquisition). The callback owns **RP validity, including domain/URI
syntax, and caller/RP authorization**. Return `true` only when the application considers the RP ID
valid under its chosen binding model and authorizes that exact origin/RP pair. Returning `false`
or throwing fails with `WebAuthnClientErrorCode.Security`;
exceptions are retained as `InnerException`. Cancellation exceptions thrown by the callback propagate
as cancellation, but also block the operation. The callback runs before the ceremony timeout starts:
keep it prompt, local, and thread-safe if clients share it; do not block on network lookups.

The SDK's RP wire checks reject only null/empty RP IDs and invalid UTF-16 that cannot be encoded
faithfully as UTF-8, using `InvalidRequest` without invoking the callback. **Wire-valid is not
RP-valid**: all other RP text reaches the callback unchanged, including whitespace or malformed
domain/URI strings. The SDK does not parse domains/URIs, perform IDNA conversion, or impose any
public-suffix or caller/RP-binding rule before or after callback approval. It sends the approved RP
ID unchanged (no case folding or normalization). CTAP encodes it as a CBOR text string; authenticator
message/credential limits remain in the CTAP2 layer. Passing the callback is an application decision,
not a blanket claim of browser-WebAuthn conformance.

### Explicit origin/RP allowlist

```csharp
var allowedPairs = new HashSet<(string Origin, string RpId)>
{
    ("https://login.example.com", "example.com"),
    ("https://login.example.com", "partner.test")
};
var allowlistOptions = new WebAuthnClientOptions
{
    RpIdValidator = (requestOrigin, rpId) => allowedPairs.Contains((requestOrigin.StringValue, rpId))
};
```

Load trusted configuration before constructing the client; do not mutate the set during ceremonies.
This is a complete allowlist: only the listed known-valid origin/RP pairs pass; every other string
is rejected. Validate trusted configuration entries against the application's binding model before
including them. An unrelated RP ID such as `partner.test` requires an intentional
application binding decision and a corresponding RP-server origin policy. It is not automatic browser
related-origin validation, and an allowlist is not a blanket claim of browser-WebAuthn conformance.

### Application-service validator

Bind your existing application validator directly; no SDK policy service is needed. The service must
validate **both RP validity (including domain/URI syntax) and caller/RP binding**, using the applicable
[WebAuthn RP ID definition](https://www.w3.org/TR/webauthn-3/#rp-id) or established native binding model:

```csharp
// rpValidationService is your application's service. Its synchronous method returns bool
// and takes (WebAuthnOrigin origin, string rpId), validating RP syntax AND caller/RP binding.
// It uses trusted, already-loaded policy; the SDK does not validate domain/URI syntax for it.
var serviceOptions = new WebAuthnClientOptions
{
    RpIdValidator = rpValidationService.IsRpValidForOrigin
};
```

For browser-compatible use, the application must implement RP syntax validity and browser RP/origin
binding rules, including public-suffix handling, through its validator. Other native binding models are permitted by
[WebAuthn Level 3, RP ID](https://www.w3.org/TR/webauthn-3/#rp-id), whose syntax requirement remains
domain strings or URIs. The delegation also follows python-fido2's
[`ClientDataCollector` / `DefaultClientDataCollector`](https://github.com/Yubico/python-fido2/blob/9d5991d6785e20bd487c8b6c996df6ce2226642a/fido2/client/__init__.py):
client-data collection delegates RP validation to application-supplied logic. This SDK does not
implement a policy framework or fetch a Public Suffix List or related-origin resources.

**Origin representation limit:** `WebAuthnOrigin` remains a serialized `https://host[:port]` origin,
or `http` for `localhost` / `*.localhost`, stripping path/query/fragment. Arbitrary native-app,
opaque, or custom-scheme origins are not representable. Authorizing a URI RP ID does not extend
origin support or establish a new native binding specification.

### Client checks do not replace RP-server checks

The client callback authorizes use of a credential scope locally. The RP server must independently
validate the challenge and ceremony type, expected signed client-data origin (and cross-origin context),
RP ID hash, signature/attestation policy, UP/UV requirements, and credential/account binding. Approval
does not change the origin in client data or make an assertion acceptable to the server.

## Constraints

- After successful construction, the client owns the `ICtap2Session` in both paths: the one `CreateWebAuthnClientAsync` opens and one handed to the `WebAuthnClient` constructor. Use `await using` on the client and do not dispose that session yourself. Rejected construction does not transfer a caller-supplied session. Prompts remain caller-owned; keep them alive until the client ends.
- A physical YubiKey admits one live connection across all of its interfaces, and one session per connection; a second session throws `ConnectionInUseException` until the first is disposed.
- Transport is fixed when the CTAP2 session is created and is never renegotiated after a failure. Override it with `SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard }`. Supplying SCP parameters without a transport override selects SmartCard automatically; explicit HID plus SCP is unsupported. Firmware/FIDO-AID qualifications still apply.
- RP validity and origin/RP authorization are mandatory application policy through `WebAuthnClientOptions.RpIdValidator`; there is no built-in domain/URI syntax gate, domain-suffix policy, or enterprise bypass.
- The client asks for nothing but a PIN, supplied as `pinBytes` or by the prompt. A ceremony that needs one and has neither fails with `NotAllowed` rather than prompting.
- `ResidentKeyPreference.Required` consumes persistent credential slots. This facade exposes no credential administration or deletion; use the CTAP2 credential-management APIs separately, after disposing the client. Firmware gates live in CTAP2.
- Default attestation is `None`: non-self statements become `fmt: none` with an empty statement; packed self attestation with zero AAGUID and no certificate is retained, and this policy does not zero the returned AAGUID. Other preferences pass through returned statements. `Indirect` performs no anonymization service; `Enterprise` does not currently send a CTAP enterprise-attestation request.
- Client-data construction is not a blanket claim of exact browser serialization conformance: the low-level builder currently uses short JSON escapes for some controls rather than the exact CCDToString form. See [`WebAuthnClientData`](src/Client/WebAuthnClientData.cs). For a manually created AAGUID, use an explicit constructor; `default(Aaguid)` has known empty-span/nonreflexive-equality defects, not normal 16-byte value semantics.

## Security notes

- A PIN passed as `pinBytes` is borrowed; zero it with `CryptographicOperations.ZeroMemory` in a `finally`.
- A PIN returned from `ICredentialPrompt` transfers ownership to the SDK, which zeroes and disposes it, including a buffer returned late after cancellation. Return memory sized exactly to the secret, not a raw pooled buffer.
- The SDK owns the retry loop: it zeroes a rejected PIN, never resubmits it, and re-prompts with `IsRetry` set and `RetriesRemaining` refreshed, up to `MaxPromptAttempts` (default 3). Do not retry inside a prompt. Returning `null` means the user declined, and nothing else; report cancellation by throwing.
- Never log PINs, PRF results, or sensitive payloads. Credential IDs, RP IDs, and AAGUIDs are not private keys, but credential/account-associated identifiers can be privacy-sensitive. Apply your application's redaction, access, and retention policy rather than treating public protocol material as unconditionally safe to log.

## Related

- [../Core/README.md](../Core/README.md) - device discovery, connections, and the credential contracts.
- [../Ctap2/README.md](../Ctap2/README.md) - the CTAP2 operations layer this package builds on, and [FidoTool](../Ctap2/examples/FidoTool/), the interactive sample; this package has none of its own.
- [user-interaction.md](../../docs/usage/user-interaction.md) and [device-discovery.md](../../docs/usage/device-discovery.md).
- The [WebAuthn Level 3](https://www.w3.org/TR/webauthn-3/) specification and the [Public Suffix List](https://publicsuffix.org/).
- [Developer guide](../../docs/DEV-GUIDE.md): building, testing, and contributing.

## Migration from earlier v2 development builds

This is a breaking authorization-policy change (YESDK-1623, audit 15); there was no shipped v1
WebAuthn package. `PublicSuffixChecker`, `WebAuthnClientOptions.EnterpriseRpIds`, and
`WebAuthnOrigin.IsRpIdValid` were removed, not deprecated. Replace them with one required callback:

- `RegistrationResponse.Transports` is now non-nullable and defaults to an empty list rather than null.
  Use `registration.Transports.Count == 0` for unavailable metadata; remove null initializers and
  null-based checks. Descriptor input transports retain their existing optional semantics.
- Replace `WebAuthnTransport.Unknown(value)` with `WebAuthnTransport.FromValue(value)`. The factory
  accepts both known and arbitrary future names without normalization; the known constants are unchanged.

```csharp
// Before
new WebAuthnClient(session, origin, new WebAuthnClientOptions
{
    PublicSuffixChecker = isPublicSuffix,
    EnterpriseRpIds = enterpriseRpIds
});

// After
new WebAuthnClient(
    ctap2Session,
    origin,
    new WebAuthnClientOptions
    {
        RpIdValidator = (requestOrigin, rpId) =>
            requestOrigin.StringValue == "https://example.com" && rpId == "example.com"
    });
```

Choose and implement RP syntax validity and the complete binding policy, rather than merely inverting the old suffix checker.
If retaining earlier browser-style behavior or enterprise overrides, move both decisions into your
application validator and test them together; scope overrides to trusted origin/RP pairs. Both constructor
and device factory require `new WebAuthnClientOptions { RpIdValidator = ... }`; carry forward optional
`CredentialPrompt` and `MaxPromptAttempts` settings. Authorization failures now use `Security` rather
than the old domain policy's `InvalidRequest`; null/empty RP IDs and invalid UTF-16 still use
`InvalidRequest`. A callback that previously checked only authorization must now also own RP validity.
The SDK no longer provides an independent domain/URI syntax gate or IDNA checks.

The operations dependency is now `Yubico.YubiKit.Ctap2`: migrate session types to `Ctap2Session` /
`ICtap2Session` and device factories to `CreateCtap2SessionAsync`. The public `WebAuthnClient`
constructor parameter is named `ctap2Session`; update named arguments accordingly. `CtapCommand`,
`CtapStatus`, and `CtapException` live directly in `Yubico.YubiKit.Ctap2`. FIDO2 application/PIN names,
FIDO HID connection names, and protocol strings are unchanged.
