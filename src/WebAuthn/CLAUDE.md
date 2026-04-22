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
│   ├── PreviewSign/              # CTAP v4 previewSign extension
│   │   ├── PreviewSignAdapter.cs
│   │   ├── PreviewSignCbor.cs
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

**WebAuthn currently has zero `ILogger` calls** — logging at protocol/extension boundaries is deferred to Phase 9.2 (auth/probe logging).

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
    Rp = new PublicKeyCredentialRpEntity { Id = "example.com", Name = "Example" },
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
// Registration
var adapter = new PreviewSignAdapter();
var input = new PreviewSignRegistrationInput { Algorithms = [-7, -257] };
var extensionsDict = new Dictionary<string, object>();
adapter.EncodeInput(input, extensionsDict);

// Send to FIDO2
var fidoOptions = new MakeCredentialOptions { Extensions = extensionsDict };

// Decode output
var output = adapter.DecodeOutput(response.UnsignedExtensionOutputs);
// output.GeneratedKey contains keyHandle, publicKey, algorithm, attestationObject
```

## CTAP v4 previewSign Extension

**Reference:** `Plans/previewSign_Implementation_Requirements.md` (authoritative spec for SDK implementation)

**Purpose:** Use a WebAuthn credential for arbitrary data signing, separate from authentication assertions.

**Wire Format:** CTAP v4 draft extension identifier `previewSign`.

**Key Features:**
- **Registration:** Generates a new signing key pair, returns public key + keyHandle
- **Authentication:** Signs arbitrary `tbs` (to-be-signed) bytes using keyHandle (NOT YET VALIDATED ON HARDWARE — see Phase 9.2 parity check)
- **Multi-credential probe:** CTAP v4 §10.2.1 step 7 iteration over `allowCredentials` (deferred to Phase 9.2)

**Current Status (as of Phase 9.1):**
- Registration path: ✅ **WORKING** on YubiKey 5.8.0-beta
- Authentication path: ⚠️ **DEFERRED** — throws `NotSupported` if `signByCredential.Count != 1` (awaiting Swift parity confirmation in Phase 9.2)

**Key Files:**
- `src/Extensions/PreviewSign/PreviewSignAdapter.cs` — Main adapter
- `src/Extensions/PreviewSign/PreviewSignCbor.cs` — CBOR key constants (split in Phase 9.1)
- `src/Extensions/PreviewSign/PreviewSignAuthenticationInput.cs:58` — Auth defer point
- `Plans/previewSign_Implementation_Requirements.md` — Full spec

## Security Boundary

**Sensitive Data Handling:**
- **PINs:** Never logged, zeroed via `CryptographicOperations.ZeroMemory` after use
- **Private keys:** Never exposed in public API; signing operations use FIDO2 CTAP commands internally
- **`tbs` payloads (previewSign auth):** Raw bytes signed by hardware; NOT logged; caller is responsible for semantic validation
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
- `WebAuthnTestHelpers.DeleteAllCredentialsForRpAsync(FidoSession, string rpId, CancellationToken)` — Cleanup helper (added in Phase 9.1)

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

1. **previewSign auth not hardware-validated yet** — Phase 9.2 will confirm Swift parity before shipping multi-credential probe
2. **Extension passthrough bug (fixed in commit `95abc0c5`)** — Extensions were silently dropped at backend; now wired correctly
3. **`flags` optional in previewSign registration output** — Matches Swift `PreviewSign.swift:132-176`; YubiKey 5.8.0-beta returns only key 3 (algorithm)
4. **No LoggingFactory** — Use `YubiKitLogging.CreateLogger<T>()` from Core
5. **Status stream must be consumed** — `IAsyncEnumerable` won't advance unless caller enumerates
6. **CBOR key constants split** — `PreviewSignCbor.Signature` and `PreviewSignCbor.ToBeSigned` were both `6` in the same scope; fixed in Phase 9.1 with nested static classes

## Future Work (Post-Phase 9)

See `Plans/yes-we-have-started-composed-horizon.md` for Phase 9 breakdown:
- **Phase 9.2:** Swift parity check → conditional auth/probe implementation
- **Phase 9.3:** Hardware verification with user-presence testing
- **Post-Phase-9:** Fido2 canonical extension coverage assessment
