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
- **High-level WebAuthn API**: `IWebAuthnClient` abstracts credential registration and authentication
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
│   ├── IWebAuthnClient.cs
│   ├── WebAuthnClient.cs
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
├── Protocol/                     # Request/response types
│   ├── WebAuthnCredentialCreateOptions.cs
│   ├── WebAuthnCredentialRequestOptions.cs
│   ├── WebAuthnMakeCredentialResponse.cs
│   └── WebAuthnGetAssertionResponse.cs
└── Error/                        # Error handling
    ├── WebAuthnClientError.cs
    └── WebAuthnClientException.cs
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
// Create client
var backend = new FidoSessionWebAuthnBackend(fidoSession);
var client = new WebAuthnClient(backend);

// Registration
var createOptions = new WebAuthnCredentialCreateOptions
{
    Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
    User = new PublicKeyCredentialUserEntity { Id = userId, Name = "user@example.com" },
    Challenge = challenge,
    PubKeyCredParams = [new PublicKeyCredentialParameters { Alg = -7, Type = "public-key" }],
    Extensions = extensionInputs
};

var credential = await client.CreateCredentialAsync(createOptions);

// Authentication
var requestOptions = new WebAuthnCredentialRequestOptions
{
    Challenge = challenge,
    RpId = "example.com",
    AllowCredentials = [new PublicKeyCredentialDescriptor { Id = credentialId, Type = "public-key" }],
    Extensions = extensionInputs
};

var assertion = await client.GetAssertionAsync(requestOptions);
```

### Status Streaming

WebAuthn operations expose progress via `IAsyncEnumerable<WebAuthnStatus>`:

```csharp
await foreach (var status in client.CreateCredentialAsync(options))
{
    Console.WriteLine($"[{status.Stage}] {status.Message}");
    
    if (status.Stage == WebAuthnStage.WaitingForUserPresence)
    {
        Console.WriteLine("Touch your YubiKey...");
    }
}
```

**Status stages:**
- `Initializing` — Validating request
- `SelectingCredential` — Credential probe (auth only)
- `WaitingForUserPresence` — Awaiting touch
- `Complete` — Operation succeeded

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

**Key Pattern:**
```csharp
[Theory]
[WithYubiKey]
public async Task Registration_WithPreviewSign_ReturnsGeneratedSigningKey(YubiKeyTestState state)
{
    await using var fidoSession = await state.Device.CreateFidoSessionAsync();
    await WebAuthnTestHelpers.NormalizePinAsync(fidoSession, TestPin);
    
    var backend = new FidoSessionWebAuthnBackend(fidoSession);
    var client = new WebAuthnClient(backend);
    
    // Test logic...
}
```

**Running Tests:**
```bash
# All WebAuthn unit tests
dotnet toolchain.cs -- test --project WebAuthn

# Integration tests (no UP)
dotnet toolchain.cs -- test --integration --project WebAuthn --filter "Category!=RequiresUserPresence"

# Integration tests (with UP, user present)
dotnet toolchain.cs -- test --integration --project WebAuthn --filter "Category=RequiresUserPresence"

# Smoke tests only (skip Slow)
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
