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

The WebAuthn module implements the W3C Web Authentication API (Level 2/3) on top of the FIDO2 CTAP protocol. It provides:
- **High-level WebAuthn API**: `WebAuthnClient` orchestrates credential registration and authentication
- **Extension Framework**: Pluggable CTAP v4 extensions (e.g., `previewSign`)
- **Backend Abstraction**: Transparently routes operations through `IFidoSession`
- **Status Streaming**: Async enumerable for operation progress (`IAsyncEnumerable<WebAuthnStatus>`)

**Key Dependencies:**
- **One-way dependency on Fido2**: WebAuthn builds on FIDO2/CTAP primitives but FIDO2 does NOT depend on WebAuthn
- **Core**: Shared types, logging, memory management

**Key Directories:**
```
src/
├── Client/                       # WebAuthn Client API
│   ├── WebAuthnClient.cs
│   ├── PublicSuffixChecker.cs
│   ├── FidoSessionWebAuthnBackend.cs
│   └── WebAuthnStatus.cs
├── Attestation/                  # Attestation object types
│   ├── WebAuthnAttestationObject.cs
│   └── WebAuthnAuthenticatorData.cs
├── Extensions/                   # CTAP v4 extension framework
│   ├── Adapters/
│   │   └── PreviewSignAdapter.cs  # WebAuthn-level adapter (translates to Fido2)
│   ├── PreviewSign/              # previewSign extension
│   │   ├── PreviewSignAuthenticationInput.cs
│   │   ├── PreviewSignRegistrationInput.cs
│   │   └── PreviewSignErrors.cs
│   └── IExtensionAdapter.cs
├── Client/Registration/          # Registration options and responses
│   ├── RegistrationOptions.cs
│   └── RegistrationResponse.cs
├── Client/Authentication/        # Authentication options and responses
│   ├── AuthenticationOptions.cs
│   ├── AuthenticationResponse.cs
│   └── MatchedCredential.cs
└── WebAuthnClientError.cs        # Error handling
```

## Logging

WebAuthn uses `YubiKitLogging` (NOT `LoggingFactory` — that does not exist). The canonical logger factory is at `src/Core/src/YubiKitLogging.cs:20`.

**WebAuthn currently has zero `ILogger` calls** — logging at protocol/extension boundaries is not implemented yet.

When adding logs:
```csharp
private static readonly ILogger Logger = YubiKitLogging.CreateLogger<PreviewSignAdapter>();

Logger.LogInformation("PreviewSign registration started for RP {RpId}", request.Rp.Id);
Logger.LogDebug("Selected algorithm: {Algorithm}", selectedAlgorithm);
Logger.LogError(ex, "PreviewSign authentication failed");
```

**Security:** NEVER log PINs, keys, `tbs` payloads, or credential private keys. Log lengths, algorithm IDs, and public metadata only.

## Critical Patterns

### WebAuthn Client Usage

```csharp
// Create client from an existing FIDO2 session.
// The PublicSuffixChecker should be backed by Public Suffix List data.
await using var client = new WebAuthnClient(
    fidoSession,
    origin,
    isPublicSuffix: domain => publicSuffixList.Contains(domain));

// Or create the FIDO2 session and WebAuthn client from a YubiKey device.
await using var clientFromDevice = await yubiKey.CreateWebAuthnClientAsync(
    origin,
    isPublicSuffix: domain => publicSuffixList.Contains(domain));

// Registration
var createOptions = new RegistrationOptions
{
    Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
    User = new PublicKeyCredentialUserEntity(userId, "user@example.com", "User"),
    Challenge = challenge,
    PubKeyCredParams = [CoseAlgorithm.Es256],
    Extensions = extensionInputs
};

var credential = await client.MakeCredentialAsync(createOptions, pin: null, useUv: false);

// Authentication
var requestOptions = new AuthenticationOptions
{
    Challenge = challenge,
    RpId = "example.com",
    AllowCredentials = [new PublicKeyCredentialDescriptor { Id = credentialId, Type = "public-key" }],
    Extensions = extensionInputs
};

var matches = await client.GetAssertionAsync(requestOptions, pin: null, useUv: false);
var assertion = await matches[0].SelectAsync();
```

### Status Streaming

WebAuthn operations expose progress via `IAsyncEnumerable<WebAuthnStatus>`:

```csharp
await foreach (var status in client.MakeCredentialStreamAsync(options))
{
    switch (status)
    {
        case WebAuthnStatusProcessing:
            Console.WriteLine("Processing WebAuthn ceremony...");
            break;
        case WebAuthnStatusRequestingPin requestingPin:
            await requestingPin.SubmitPin(pinBytes);
            break;
        case WebAuthnStatusRequestingUv requestingUv:
            await requestingUv.SetUseUv(false);
            break;
        case WebAuthnStatusFinished<RegistrationResponse> finished:
            Console.WriteLine($"Created credential {Convert.ToHexString(finished.Result.CredentialId.Span)}");
            break;
        case WebAuthnStatusFailed failed:
            throw failed.Error;
    }
}
```

**Status records:**
- `WebAuthnStatusProcessing` — ceremony work is in progress
- `WebAuthnStatusRequestingPin` — caller must supply PIN bytes or cancel
- `WebAuthnStatusRequestingUv` — caller must opt in/out of UV
- `WebAuthnStatusFinished<T>` — operation completed successfully
- `WebAuthnStatusFailed` — operation completed with a typed WebAuthn error

### RP ID Validation

`WebAuthnClient` validates RP IDs against `WebAuthnOrigin` before CTAP operations. Public suffixes such as `com` and `co.uk` must be rejected for suffix matches, so production callers must provide a `PublicSuffixChecker` backed by Public Suffix List data.

Cross-SDK alignment:
- Swift uses the same caller-supplied public-suffix checker pattern for `WebAuthn.Client`.
- Python `python-fido2` ships bundled PSL data and validates with `verify_rp_id`.
- Android accepts caller-supplied `effectiveDomain`; .NET intentionally keeps the safer explicit suffix-checker model.

### Extension Adapter Pattern

Extensions are implemented as `IExtensionAdapter<TInput, TOutput>`:

```csharp
public interface IExtensionAdapter<in TInput, out TOutput>
{
    string ExtensionIdentifier { get; }
    void EncodeInput(TInput input, IDictionary<string, object> extensionsMap);
    TOutput DecodeOutput(IDictionary<string, object> extensionsMap);
}
```

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
        new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>(ByteArrayKeyComparer.Instance)
        {
            [credentialId] = signingParams
        }));
```

## previewSign Extension

**Purpose:** Use a WebAuthn credential for arbitrary data signing, separate from authentication assertions.

**Wire Format:** previewSign extension identifier `previewSign`.

**Key Features:**
- **Registration:** Generates a new signing key pair, returns public key + keyHandle
- **Authentication:** Signs algorithm-specific `tbs` (to-be-signed) bytes using keyHandle and optional raw `additionalArgs`
- **Multi-credential probe:** Single-credential authentication is implemented; multi-credential probe-selection is not implemented yet
- **Algorithm agility:** `tbs` and `additionalArgs` are algorithm-specific values and are passed through unchanged

**Current Status:**
- Registration path: working on previewSign-capable YubiKeys
- Authentication path: working for single-credential scope; throws `NotSupported` if `signByCredential.Count != 1`
- Full ceremony: `FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature` verifies registration, ARKG derivation, signing, and offline signature verification with user presence
- ARKG helpers: **WARNING -- EXPERIMENTAL --** public experimental conveniences; not the generic previewSign API contract; not ready for production use and must not be treated as production cryptographic guidance

**Key Files:**
- `src/Extensions/Adapters/PreviewSignAdapter.cs` — WebAuthn-level adapter (translates to Fido2)
- `../../Fido2/src/Extensions/PreviewSign/` — Canonical Fido2 types and encoder
- `src/Extensions/PreviewSign/PreviewSignAuthenticationInput.cs` — WebAuthn authentication input
- `Plans/previewSign_Implementation_Requirements.md` — Full spec

**Architectural Note:** The CBOR encoding logic lives in the Fido2 layer (`Yubico.YubiKit.Fido2.Extensions.PreviewSignCbor`), ensuring a single canonical encoder shared by both Fido2 and WebAuthn. The WebAuthn adapter translates WebAuthn-level types to Fido2 types and delegates encoding to the Fido2 layer. Generic signing params carry raw `additionalArgs`; ARKG-specific helpers can be converted to raw bytes with `PreviewSignCbor.EncodeAdditionalArgs(...)` before being passed to WebAuthn. **WARNING -- EXPERIMENTAL --** Those ARKG helpers are not ready for production use and must not be treated as production cryptographic guidance.

## Security Boundary

**Sensitive Data Handling:**
- **PINs:** Never logged, zeroed via `CryptographicOperations.ZeroMemory` after use
- **Private keys:** Never exposed in public API; signing operations use FIDO2 CTAP commands internally
- **`tbs` and `additionalArgs` payloads (previewSign auth):** Algorithm-specific bytes; NOT logged; caller is responsible for semantic validation
- **Credential IDs:** Public identifiers — safe to log

**Memory Management:**
- Follow root `CLAUDE.md` memory hierarchy (Span > Memory > ArrayPool > Array)
- Zero sensitive buffers with `CryptographicOperations.ZeroMemory`
- Use `ArrayPool<byte>.Shared` for >512 byte temp buffers

**Error Handling:**
- Map CTAP errors to `WebAuthnClientError` enums via `PreviewSignErrors.MapCtapError` (or equivalent per extension)
- Never expose raw CTAP status codes to high-level API consumers

## Test Infrastructure

### Integration Tests

**Location:** `tests/Yubico.YubiKit.WebAuthn.IntegrationTests/`

**Test Helpers:**
- `WebAuthnTestHelpers.NormalizePinAsync(FidoSession, ReadOnlyMemory<byte>, CancellationToken)` — Sets/verifies PIN, more defensive than Fido2's helper (handles `ForcePinChange`, skips on mismatch)
- `WebAuthnTestHelpers.DeleteAllCredentialsForRpAsync(FidoSession, string rpId, CancellationToken)` — Cleanup helper

**Traits:**
- `[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]` — Tests requiring touch
- `[Trait(TestCategories.Category, TestCategories.Slow)]` — RSA 3072/4096 keygen or >5s tests (skipped by `--smoke`)

**Coordination lanes:**

| Lane | Examples | Agent-runnable? | Rule |
|------|----------|-----------------|------|
| Unit/fake-backend | WebAuthn client, origin, extension adapter, status-stream unit tests | Yes | Run through `dotnet toolchain.cs test --project WebAuthn` |
| Integration smoke without UP | factory/session checks that do not ask for touch | Yes | Use `--smoke` or `Category!=RequiresUserPresence` |
| User Presence | registration/authentication ceremonies, previewSign hardware checks | No by default | Mark with `Category=RequiresUserPresence`; run only with a human present |
| User Verification / PIN | PIN normalization, UV-required/preferred flows | No by default | Requires explicit human approval and known PIN/device state |
| Reset/destructive cleanup | reset or broad persistent credential deletion | No | Human-approved destructive run only |

Agents must not run WebAuthn User Presence, UV/PIN, reset, insert/remove, or destructive hardware checks unless a human explicitly approves the exact command and is physically present for the interaction.

**Key Pattern:**
```csharp
[Theory]
[WithYubiKey]
public async Task Registration_WithPreviewSign_ReturnsGeneratedSigningKey(YubiKeyTestState state)
{
    await using var fidoSession = await state.Device.CreateFidoSessionAsync();
    await WebAuthnTestHelpers.NormalizePinAsync(fidoSession, TestPin);

    WebAuthnOrigin.TryParse(TestOriginUrl, out var origin);
    await using var client = new WebAuthnClient(
        fidoSession,
        origin!,
        isPublicSuffix: domain => domain is "com" or "org" or "net" or "co.uk");
    
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
- **Yubico.YubiKit.Fido2** — Lower-level CTAP protocol; WebAuthn builds on top of `IFidoSession`
- **Yubico.YubiKit.Core** — Shared types, logging (`YubiKitLogging`), memory utilities

**Integration:**
- WebAuthn → Fido2 (depends on)
- Fido2 ← WebAuthn (NO dependency back)

## Known Gotchas

1. **previewSign auth is single-credential only for now** — multi-credential probe-selection is not implemented yet
2. **Extension passthrough bug (fixed in commit `95abc0c5`)** — Extensions were silently dropped at backend; now wired correctly
3. **`flags` optional in previewSign registration output** — Some authenticators return only key 3 (algorithm)
4. **No LoggingFactory** — Use `YubiKitLogging.CreateLogger<T>()` from Core
5. **Status stream must be consumed** — `IAsyncEnumerable` won't advance unless caller enumerates
6. **CBOR key constants are context-specific** — key `7` means authentication `additionalArgs` in GetAssertion input and attestation object in MakeCredential unsigned output; keep parsing paths context-specific
