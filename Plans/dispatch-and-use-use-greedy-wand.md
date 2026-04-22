# Implementation Plan: WebAuthn Client + previewSign Extension

## Context

We're implementing two complementary features for the Yubico.NET.SDK FIDO2 module:

1. **WebAuthn Client** - Full client-side WebAuthn protocol layer (W3C spec equivalent of `navigator.credentials.create/get`)
2. **previewSign Extension** - CTAP v4 draft extension for signing arbitrary data with credential-bound signing keys

### Why This Change?

**Current State:**
- The SDK provides low-level CTAP2 authenticator operations (`IFidoSession`)
- Missing: WebAuthn client-side logic (CollectedClientData, origin validation, PublicKeyCredential wrapping)
- Extensions exist but only at CTAP level, not WebAuthn level

**After This Change:**
- Complete WebAuthn client implementation for .NET applications (desktop, mobile, server)
- previewSign support enables verifiable credential signing use cases
- Clean separation: WebAuthn layer (client logic) → CTAP2 layer (authenticator protocol)

**Use Cases:**
- Desktop apps using YubiKey for WebAuthn authentication
- Server-side WebAuthn verification
- Native mobile apps with hardware authenticator support
- Verifiable credentials bound to WebAuthn credentials (previewSign)

### Reference Implementations

**yubikit-swift WebAuthn (76 files, ~8k LOC):**
- Actor-based `CTAP2.Session` with AsyncStream for status (processing, waitingForUser, finished)
- AsyncSequence for credential pagination
- Namespace organization: `WebAuthn.*`, `CTAP2.*`, `COSE.*`
- Custom CBOR encoder/decoder
- No explicit "WebAuthnClient" class - operations directly on session

**Current .NET State (47 files, ~11k LOC):**
- Complete CTAP2.1/2.3 protocol in `src/Fido2/`
- Excellent CBOR utilities (`System.Formats.Cbor`, `CtapRequestBuilder`)
- Extension system with fluent builder pattern
- PIN/UV auth protocols V1/V2
- **Gaps:** CollectedClientData, origin validation, PublicKeyCredential wrapper, attestation verification

## Architecture Overview

### Two-Part Implementation

**Part 1: WebAuthn Client (~2,100 LOC, 20 new files)**
```
src/Fido2/
├── WebAuthn/                          # NEW namespace
│   ├── IWebAuthnClient.cs             # Main client interface
│   ├── WebAuthnClient.cs              # Implementation
│   ├── CollectedClientData.cs         # Client data JSON generator
│   ├── PublicKeyCredential.cs         # Response wrapper
│   ├── IChallengeStore.cs             # Challenge lifecycle management
│   ├── InMemoryChallengeStore.cs      # Default implementation
│   ├── Attestation/
│   │   ├── IAttestationVerifier.cs
│   │   ├── AttestationVerifier.cs     # Cert chain + metadata service
│   │   └── PermissiveVerifier.cs      # Default (allows all)
│   └── Extensions/
│       ├── IExtensionProcessor.cs     # Bidirectional mapping
│       ├── ExtensionProcessor.cs      # Base implementation
│       └── CredProtectProcessor.cs    # Example
```

**Part 2: previewSign Extension (~730 new LOC + 108 modified LOC, 9 new files)**
```
src/Fido2/
├── Extensions/
│   ├── PreviewSign/                   # NEW
│   │   ├── PreviewSignInput.cs        # Client input (GenerateKey/SignByCredential)
│   │   ├── PreviewSignOutput.cs       # Client output (GeneratedKey/Signature)
│   │   ├── PreviewSignExtension.cs    # Encoder/decoder
│   │   ├── KeyHandle.cs               # Opaque handle wrapper
│   │   ├── SigningRequest.cs          # {kh, tbs, ?args}
│   │   └── PreviewSignProcessor.cs    # WebAuthn extension processor
```

### Key Design Decisions

#### 1. Origin Handling in Non-Browser Context

**Problem:** WebAuthn spec defines "origin" as browser origin (`https://example.com`). How does this apply to native .NET apps?

**Decision:**
- Use application identifier format: `app://bundle-id` (mobile), `app://assembly-name` (desktop)
- Provide `OriginHelper.GetApplicationOrigin()` for automatic detection
- RP ID validation: exact match for custom schemes, suffix match for HTTPS
- Allow explicit origin override for server-side scenarios

```csharp
// Default: auto-detect
var client = new WebAuthnClient(fidoSession);

// Custom origin (server-side verification)
var client = new WebAuthnClient(fidoSession, origin: "https://example.com");
```

#### 2. Async Operation Status Pattern

**Swift uses:** `AsyncStream<Status>` with `.processing`, `.waitingForUser(cancel)`, `.finished(Result)`

**.NET equivalent:**
```csharp
// Simple case: Task-based async (most operations)
Task<PublicKeyCredential> CreateCredentialAsync(...)

// Progress tracking: IProgress<T> (optional parameter)
await client.CreateCredentialAsync(
    options, 
    cancellationToken,
    progress: new Progress<OperationStatus>(status =>
    {
        if (status is OperationStatus.WaitingForUserPresence)
        {
            UI.ShowTouchPrompt();
        }
    }));
```

**Status enum:**
```csharp
public enum OperationStatus
{
    Preparing,
    WaitingForUserPresence,
    WaitingForUserVerification,
    Processing,
    Completed
}
```

#### 3. Challenge Lifecycle Management

**Problem:** Challenges must be one-time-use and time-limited to prevent replay attacks.

**Decision:**
- `IChallengeStore` interface for validation
- In-memory default implementation (5-minute TTL, auto-cleanup)
- Distributed implementations can plug in (Redis, SQL, etc.)

```csharp
public interface IChallengeStore
{
    Task<byte[]> GenerateAndStoreAsync(string rpId, TimeSpan? ttl = null, CancellationToken ct = default);
    Task<bool> ValidateAndConsumeAsync(string rpId, byte[] challenge, CancellationToken ct = default);
}
```

**Critical:** `ValidateAndConsumeAsync` is atomic - challenge deleted after first validation.

#### 4. Extension System Integration

**Current:** CTAP-level extensions via `ExtensionBuilder` (fluent pattern)

**New:** WebAuthn-level extension processors for bidirectional mapping

```csharp
public interface IExtensionProcessor<TInput, TOutput>
{
    string Identifier { get; }
    
    // Client → Authenticator (before CTAP call)
    void ProcessInput(TInput input, ExtensionBuilder ctapBuilder, ProcessingContext context);
    
    // Authenticator → Client (after CTAP call)
    TOutput? ProcessOutput(ExtensionOutput ctapOutput, ProcessingContext context);
}
```

**Pattern:** Each WebAuthn extension gets a processor that knows how to map to/from CTAP.

Example:
- `credProtect` (WebAuthn) → `credProtect` (CTAP) - direct mapping
- `prf` (WebAuthn) → `hmac-secret` (CTAP) - encoding transformation

#### 5. Attestation Verification Strategy

**Problem:** Strict attestation verification requires metadata service, cert chain validation, revocation checks (complex).

**Decision:**
- Pluggable `IAttestationVerifier` interface
- Default: `PermissiveVerifier` (allows all, logs warning)
- Opt-in: `AttestationVerifier` (strict validation)

```csharp
// Default: permissive (suitable for most apps)
var client = new WebAuthnClient(fidoSession);

// Strict: requires valid attestation
var verifier = new AttestationVerifier(metadataService);
var client = new WebAuthnClient(fidoSession, attestationVerifier: verifier);
```

**Rationale:** Most apps don't need strict attestation. Power users can opt in.

#### 6. previewSign Key Handle Encoding

**Problem:** Key handle encoding is authenticator-specific (spec allows flexibility).

**Decision:**
- Treat key handles as **opaque byte strings** in SDK
- Don't parse or validate structure (that's authenticator's job)
- Provide `KeyHandle` wrapper for type safety
- Authenticator returns handle, client stores/passes it back unchanged

```csharp
public readonly record struct KeyHandle(ReadOnlyMemory<byte> Value);
```

**Anti-pattern:** Don't implement HMAC-based key handle generation in SDK - that's authenticator firmware concern.

## Detailed Implementation Plan

### Part 1: WebAuthn Client

#### Phase 1.1: Core Types (4 files, ~400 LOC)

**File:** `src/Fido2/WebAuthn/CollectedClientData.cs`
```csharp
public sealed class CollectedClientData
{
    public string Type { get; init; }           // "webauthn.create" or "webauthn.get"
    public string Challenge { get; init; }      // base64url
    public string Origin { get; init; }
    public bool? CrossOrigin { get; init; }
    public TokenBinding? TokenBinding { get; init; }
    
    public string ToJson();                     // Canonical JSON
    public byte[] GetHash();                    // SHA-256
}
```

**File:** `src/Fido2/WebAuthn/PublicKeyCredential.cs`
```csharp
public sealed class PublicKeyCredential
{
    public byte[] RawId { get; init; }
    public string Id { get; init; }              // base64url(RawId)
    public string Type { get; init; } = "public-key";
    public AuthenticatorResponse Response { get; init; }
    public AuthenticatorAttachment? AuthenticatorAttachment { get; init; }
    public Dictionary<string, object?>? ClientExtensionResults { get; init; }
}

public abstract class AuthenticatorResponse
{
    public byte[] ClientDataJSON { get; init; }
}

public sealed class AuthenticatorAttestationResponse : AuthenticatorResponse
{
    public byte[] AttestationObject { get; init; }
    public byte[]? TransportIds { get; init; }
}

public sealed class AuthenticatorAssertionResponse : AuthenticatorResponse
{
    public byte[] AuthenticatorData { get; init; }
    public byte[] Signature { get; init; }
    public byte[]? UserHandle { get; init; }
}
```

**File:** `src/Fido2/WebAuthn/IChallengeStore.cs`
```csharp
public interface IChallengeStore
{
    Task<byte[]> GenerateAndStoreAsync(string rpId, TimeSpan? ttl = null, CancellationToken ct = default);
    Task<bool> ValidateAndConsumeAsync(string rpId, byte[] challenge, CancellationToken ct = default);
}
```

**File:** `src/Fido2/WebAuthn/InMemoryChallengeStore.cs`
- `ConcurrentDictionary<(string RpId, string Challenge), DateTimeOffset Expiry>`
- Background cleanup task (every 60s, removes expired)
- Default TTL: 5 minutes

#### Phase 1.2: Extension System (5 files, ~600 LOC)

**File:** `src/Fido2/WebAuthn/Extensions/IExtensionProcessor.cs`
```csharp
public interface IExtensionProcessor<TInput, TOutput>
{
    string Identifier { get; }
    void ProcessInput(TInput input, ExtensionBuilder ctapBuilder, ProcessingContext context);
    TOutput? ProcessOutput(ExtensionOutput ctapOutput, ProcessingContext context);
}

public sealed class ProcessingContext
{
    public ReadOnlyMemory<byte> ClientDataHash { get; init; }
    public string RpId { get; init; }
    public IPinUvAuthProtocol? PinUvAuthProtocol { get; init; }
    public byte[]? PinUvAuthToken { get; init; }
}
```

**File:** `src/Fido2/WebAuthn/Extensions/ExtensionProcessor.cs`
- Base class with common patterns
- Registry: `Dictionary<string, IExtensionProcessor>`

**File:** `src/Fido2/WebAuthn/Extensions/CredProtectProcessor.cs`
- Example processor (direct mapping)

**File:** `src/Fido2/WebAuthn/Extensions/PrfProcessor.cs`
- Complex processor (prf → hmac-secret mapping)
- Base64url encoding/decoding

**File:** `src/Fido2/WebAuthn/Extensions/ExtensionOutput.cs`
- Wrapper for CTAP extension outputs
- Merge signed + unsigned extensions

#### Phase 1.3: Client Implementation (2 files, ~800 LOC)

**File:** `src/Fido2/WebAuthn/IWebAuthnClient.cs`
```csharp
public interface IWebAuthnClient : IAsyncDisposable
{
    Task<PublicKeyCredential> CreateCredentialAsync(
        PublicKeyCredentialCreationOptions options,
        CancellationToken cancellationToken = default,
        IProgress<OperationStatus>? progress = null);
    
    Task<PublicKeyCredential> GetAssertionAsync(
        PublicKeyCredentialRequestOptions options,
        CancellationToken cancellationToken = default,
        IProgress<OperationStatus>? progress = null);
}
```

**File:** `src/Fido2/WebAuthn/WebAuthnClient.cs`

Main workflows:

**CreateCredential:**
1. Generate challenge (if not provided) via `IChallengeStore`
2. Build `CollectedClientData` with type="webauthn.create"
3. Compute `clientDataHash = SHA256(clientDataJSON)`
4. Process WebAuthn extensions → CTAP extensions
5. Call `IFidoSession.MakeCredentialAsync(...)`
6. Process CTAP extension outputs → WebAuthn outputs
7. Verify attestation (via `IAttestationVerifier`)
8. Validate challenge (via `IChallengeStore.ValidateAndConsumeAsync`)
9. Return `PublicKeyCredential` with `AuthenticatorAttestationResponse`

**GetAssertion:**
1. Validate challenge (if provided) via `IChallengeStore`
2. Build `CollectedClientData` with type="webauthn.get"
3. Compute `clientDataHash`
4. Process extensions
5. Call `IFidoSession.GetAssertionAsync(...)`
6. If `numberOfCredentials > 1`, handle selection:
   - If `allowCredentials` specified: pick first match
   - If discoverable credential: invoke `credentialSelector` delegate
7. Process extension outputs
8. Return `PublicKeyCredential` with `AuthenticatorAssertionResponse`

**Constructor:**
```csharp
public WebAuthnClient(
    IFidoSession fidoSession,
    string? origin = null,
    IChallengeStore? challengeStore = null,
    IAttestationVerifier? attestationVerifier = null,
    Func<IReadOnlyList<CredentialDescriptor>, Task<int>>? credentialSelector = null)
```

#### Phase 1.4: Attestation Verification (3 files, ~300 LOC)

**File:** `src/Fido2/WebAuthn/Attestation/IAttestationVerifier.cs`
```csharp
public interface IAttestationVerifier
{
    Task<AttestationVerificationResult> VerifyAsync(
        AttestationObject attestationObject,
        byte[] clientDataHash,
        CancellationToken ct = default);
}

public sealed class AttestationVerificationResult
{
    public bool IsValid { get; init; }
    public AttestationType Type { get; init; }  // Basic, Self, AttCA, None
    public X509Certificate2? Certificate { get; init; }
    public string? ErrorMessage { get; init; }
}
```

**File:** `src/Fido2/WebAuthn/Attestation/PermissiveVerifier.cs`
- Always returns `IsValid = true`
- Logs warning once per session

**File:** `src/Fido2/WebAuthn/Attestation/AttestationVerifier.cs`
- Format handlers: `packed`, `fido-u2f`, `android-safetynet`, `apple`, `none`
- Certificate chain validation
- Metadata service integration (optional)
- **Note:** Initial implementation focuses on `packed` and `none`; other formats can be added incrementally

#### Phase 1.5: Supporting Types (6 files, ~200 LOC)

**File:** `src/Fido2/WebAuthn/OriginHelper.cs`
```csharp
public static class OriginHelper
{
    public static string GetApplicationOrigin();  // Auto-detect
    public static bool ValidateRpId(string origin, string rpId);
}
```

**File:** `src/Fido2/WebAuthn/PublicKeyCredentialCreationOptions.cs`
- Properties: `rp`, `user`, `challenge`, `pubKeyCredParams`, `timeout`, `excludeCredentials`, `authenticatorSelection`, `attestation`, `extensions`

**File:** `src/Fido2/WebAuthn/PublicKeyCredentialRequestOptions.cs`
- Properties: `challenge`, `timeout`, `rpId`, `allowCredentials`, `userVerification`, `extensions`

**File:** `src/Fido2/WebAuthn/AuthenticatorSelectionCriteria.cs`
- Properties: `authenticatorAttachment`, `residentKey`, `userVerification`

**File:** `src/Fido2/WebAuthn/OperationStatus.cs`
- Enum for progress reporting

**File:** `src/Fido2/WebAuthn/WebAuthnException.cs`
- Custom exception for WebAuthn errors

---

### Part 2: previewSign Extension

**Dependencies:** Requires Part 1's extension processor system.

#### Phase 2.1: Core Types (4 files, ~350 LOC)

**File:** `src/Fido2/Extensions/PreviewSign/PreviewSignInput.cs`
```csharp
public abstract record PreviewSignInput
{
    public sealed record GenerateKey(IReadOnlyList<CoseAlgorithmIdentifier> Algorithms) : PreviewSignInput;
    
    public sealed record SignByCredential(
        IReadOnlyDictionary<string, SigningRequest> ByCredential) : PreviewSignInput;
}
```

**File:** `src/Fido2/Extensions/PreviewSign/PreviewSignOutput.cs`
```csharp
public abstract record PreviewSignOutput
{
    public sealed record GeneratedKey(
        KeyHandle Handle,
        ReadOnlyMemory<byte> PublicKey,
        CoseAlgorithmIdentifier Algorithm,
        ReadOnlyMemory<byte> AttestationObject) : PreviewSignOutput;
    
    public sealed record Signature(
        ReadOnlyMemory<byte> SignatureBytes,
        CoseAlgorithmIdentifier Algorithm) : PreviewSignOutput;
}
```

**File:** `src/Fido2/Extensions/PreviewSign/KeyHandle.cs`
```csharp
public readonly record struct KeyHandle(ReadOnlyMemory<byte> Value)
{
    public string ToBase64Url() => Base64Url.Encode(Value.Span);
    public static KeyHandle FromBase64Url(string encoded);
}
```

**File:** `src/Fido2/Extensions/PreviewSign/SigningRequest.cs`
```csharp
public sealed record SigningRequest(
    KeyHandle Handle,
    ReadOnlyMemory<byte> ToBeSigned,
    ReadOnlyMemory<byte>? AdditionalArgs = null);
```

#### Phase 2.2: CBOR Encoding (2 files, ~280 LOC)

**File:** `src/Fido2/Extensions/PreviewSign/PreviewSignExtension.cs`

**Registration Input (CBOR):**
```csharp
// Map keys
const int KeyAlg = 3;
const int KeyFlags = 4;

writer.WriteStartMap(2);
writer.WriteInt32(KeyAlg);
writer.WriteStartArray(input.Algorithms.Count);
foreach (var alg in input.Algorithms)
{
    writer.WriteInt32((int)alg);
}
writer.WriteEndArray();
// Optional: flags (default 0b001 = require UP)
writer.WriteEndMap();
```

**Authentication Input (CBOR):**
```csharp
// Map keys
const int KeyKh = 2;
const int KeyTbs = 6;
const int KeyArgs = 7;

writer.WriteStartMap(2 or 3);  // 3 if args present
writer.WriteInt32(KeyKh);
writer.WriteByteString(request.Handle.Value.Span);
writer.WriteInt32(KeyTbs);
writer.WriteByteString(request.ToBeSigned.Span);
if (request.AdditionalArgs.HasValue)
{
    writer.WriteInt32(KeyArgs);
    writer.WriteByteString(request.AdditionalArgs.Value.Span);
}
writer.WriteEndMap();
```

**Output Parsing:**
- Registration: `{alg: int, flags?: uint}` + unsigned `{att-obj: bstr}`
- Authentication: `{sig: bstr}`

**File:** `src/Fido2/Extensions/PreviewSign/CoseSignArgs.cs`
- Helper for encoding COSE_Sign_Args (if needed)
- Placeholder for now (spec doesn't fully define structure)

#### Phase 2.3: Extension Processor (2 files, ~100 LOC)

**File:** `src/Fido2/Extensions/PreviewSign/PreviewSignProcessor.cs`
```csharp
public sealed class PreviewSignProcessor : IExtensionProcessor<PreviewSignInput, PreviewSignOutput>
{
    public string Identifier => "previewSign";
    
    public void ProcessInput(PreviewSignInput input, ExtensionBuilder ctapBuilder, ProcessingContext context)
    {
        // Encode to CBOR and add to ctapBuilder
        // This requires extending ExtensionBuilder with .WithPreviewSign()
    }
    
    public PreviewSignOutput? ProcessOutput(ExtensionOutput ctapOutput, ProcessingContext context)
    {
        // Parse CBOR response
        // Registration: extract public key from unsigned extensions
        // Authentication: extract signature
    }
}
```

**File:** `src/Fido2/Extensions/ExtensionBuilder.cs` (modify)
- Add `.WithPreviewSign(PreviewSignInput input)` method (~20 LOC)

#### Phase 2.4: Error Handling (1 file, ~30 LOC)

**File:** `src/Fido2/Extensions/PreviewSign/PreviewSignException.cs`
```csharp
public sealed class PreviewSignException : CtapException
{
    public PreviewSignError ErrorCode { get; }
}

public enum PreviewSignError
{
    UnsupportedAlgorithm,
    InvalidOption,
    InvalidCredential,
    MissingParameter,
    UserPresenceRequired,
    UserVerificationRequired
}
```

Map CTAP status codes to domain-specific errors.

---

## Critical Files Summary

### New Files (29 total)

**Part 1 - WebAuthn Client (20 files):**
- `src/Fido2/WebAuthn/IWebAuthnClient.cs`
- `src/Fido2/WebAuthn/WebAuthnClient.cs`
- `src/Fido2/WebAuthn/CollectedClientData.cs`
- `src/Fido2/WebAuthn/PublicKeyCredential.cs`
- `src/Fido2/WebAuthn/PublicKeyCredentialCreationOptions.cs`
- `src/Fido2/WebAuthn/PublicKeyCredentialRequestOptions.cs`
- `src/Fido2/WebAuthn/AuthenticatorSelectionCriteria.cs`
- `src/Fido2/WebAuthn/OperationStatus.cs`
- `src/Fido2/WebAuthn/WebAuthnException.cs`
- `src/Fido2/WebAuthn/OriginHelper.cs`
- `src/Fido2/WebAuthn/IChallengeStore.cs`
- `src/Fido2/WebAuthn/InMemoryChallengeStore.cs`
- `src/Fido2/WebAuthn/Extensions/IExtensionProcessor.cs`
- `src/Fido2/WebAuthn/Extensions/ExtensionProcessor.cs`
- `src/Fido2/WebAuthn/Extensions/ExtensionOutput.cs`
- `src/Fido2/WebAuthn/Extensions/CredProtectProcessor.cs`
- `src/Fido2/WebAuthn/Extensions/PrfProcessor.cs`
- `src/Fido2/WebAuthn/Attestation/IAttestationVerifier.cs`
- `src/Fido2/WebAuthn/Attestation/PermissiveVerifier.cs`
- `src/Fido2/WebAuthn/Attestation/AttestationVerifier.cs`

**Part 2 - previewSign (9 files):**
- `src/Fido2/Extensions/PreviewSign/PreviewSignInput.cs`
- `src/Fido2/Extensions/PreviewSign/PreviewSignOutput.cs`
- `src/Fido2/Extensions/PreviewSign/KeyHandle.cs`
- `src/Fido2/Extensions/PreviewSign/SigningRequest.cs`
- `src/Fido2/Extensions/PreviewSign/PreviewSignExtension.cs`
- `src/Fido2/Extensions/PreviewSign/PreviewSignProcessor.cs`
- `src/Fido2/Extensions/PreviewSign/PreviewSignException.cs`
- `src/Fido2/Extensions/PreviewSign/CoseSignArgs.cs`
- `src/Fido2/Extensions/ExtensionIdentifiers.cs` (modify to add "previewSign")

### Modified Files (1)

- `src/Fido2/Extensions/ExtensionBuilder.cs` - Add `.WithPreviewSign()` method

---

## Test Strategy

### Part 1: WebAuthn Client

#### Unit Tests (~2,000 LOC)

**CollectedClientData:**
- JSON generation matches spec (canonical form)
- Base64url encoding
- Hash computation
- Type validation

**PublicKeyCredential:**
- Serialization/deserialization
- Id = base64url(rawId) consistency

**IChallengeStore:**
- In-memory implementation:
  - Generate and store
  - Validate and consume (one-time-use)
  - Expiration (TTL)
  - Concurrent access safety
- Mock implementation for tests

**ExtensionProcessor:**
- Each processor:
  - Input mapping (WebAuthn → CTAP)
  - Output mapping (CTAP → WebAuthn)
  - Error handling
- Registry operations

**WebAuthnClient:**
- Mock `IFidoSession` with `NSubstitute`
- Verify CBOR request construction
- Test workflows:
  - CreateCredential: challenge generation, client data, extension processing
  - GetAssertion: challenge validation, multiple credentials
- Error scenarios:
  - Excluded credential
  - No matching credentials
  - Timeout
  - Invalid challenge

**AttestationVerifier:**
- Permissive verifier (always succeeds)
- Format-specific verification:
  - `packed` - signature validation
  - `none` - no attestation
  - Future: `fido-u2f`, `apple`

**OriginHelper:**
- Application origin detection
- RP ID validation (exact match, suffix match)
- Edge cases (port numbers, subdomains)

#### Integration Tests (~500 LOC)

Requires physical YubiKey:

```csharp
[Fact]
[Trait("RequiresUserPresence", "true")]
public async Task CreateCredential_RealYubiKey_Success()
{
    await using var fidoSession = await yubiKey.CreateFidoSessionAsync();
    var client = new WebAuthnClient(fidoSession, origin: "app://test");
    
    var options = new PublicKeyCredentialCreationOptions
    {
        Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
        User = new PublicKeyCredentialUserEntity([1,2,3], "alice", "Alice"),
        PubKeyCredParams = [new(-7)],  // ES256
    };
    
    var credential = await client.CreateCredentialAsync(options);
    
    Assert.NotNull(credential.RawId);
    Assert.Equal("public-key", credential.Type);
}
```

**Test scenarios:**
- Create credential (ES256, EdDSA, RS256)
- Get assertion (single credential, multiple credentials)
- Extensions (credProtect, prf, largeBlobKey)
- PIN/UV authentication
- Resident keys
- Exclude list (credential exists)
- User selection (multiple discoverable credentials)

**Exclusions:**
- Timeout tests (flaky, hard to control timing)
- User cancellation (requires manual intervention)

### Part 2: previewSign Extension

#### Unit Tests (~400 LOC)

**PreviewSignExtension (CBOR):**
- Registration input encoding:
  - Single algorithm
  - Multiple algorithms
  - Flags parameter (0b000, 0b001, 0b101)
- Authentication input encoding:
  - Key handle + TBS
  - With additional args
- Output parsing:
  - Registration: alg, flags, unsigned att-obj
  - Authentication: signature

**PreviewSignProcessor:**
- Input processing (call to ExtensionBuilder)
- Output processing (parse CBOR)
- Error mapping (CTAP → PreviewSignException)

**KeyHandle:**
- Base64url encoding/decoding
- Equality comparison

#### Integration Tests (~200 LOC)

**Mock Authenticator:**
- Mock `IFidoSession` to return previewSign responses
- Test full flow:
  - Registration: generate key, receive public key + att-obj
  - Authentication: sign arbitrary data, receive signature

**Real YubiKey (if firmware supports):**
- Check `AuthenticatorInfo.Extensions` for "previewSign"
- If supported:
  - Create credential with `previewSign.generateKey`
  - Sign data with `previewSign.signByCredential`
  - Verify signature with public key

**Note:** YubiKey firmware support timeline unknown - may need to wait for firmware update or use simulator.

---

## Security Considerations

### Sensitive Data Handling

**Challenge Storage:**
- Challenges are sensitive (prevent replay attacks)
- In-memory store: use `ConcurrentDictionary` with `DateTimeOffset` expiry
- Zero challenge bytes after consumption: `CryptographicOperations.ZeroMemory(challenge)`

**ClientDataHash:**
- Derived value (hash of client data JSON) - not sensitive
- OK to keep in memory

**Attestation Signatures:**
- Public data (part of attestation object)
- No special handling needed

**previewSign Signing Keys:**
- Key handles are opaque authenticator data - treat as potentially sensitive
- Zero `ToBeSigned` data after use (caller's responsibility)
- Signatures are public output - no zeroing needed

### Timing Attacks

**Challenge Comparison:**
- Use `CryptographicOperations.FixedTimeEquals(challenge1, challenge2)`
- Never use `SequenceEqual` (timing-vulnerable)

**Key Handle Validation:**
- Authenticator handles validation
- SDK doesn't parse/compare key handles (opaque bytes)

### Validation Boundaries

**What SDK MUST validate:**
- Challenge format (32 bytes)
- Challenge one-time-use
- RP ID format
- Origin format
- clientDataHash length (32 bytes)
- Extension identifier strings
- CBOR structure validity

**What SDK TRUSTS from authenticator:**
- Signature validity (authenticator signs with private key)
- Credential existence
- User presence/verification
- Key handle integrity

**What SDK TRUSTS from caller:**
- RP entity data (name, icon)
- User entity data (id, name, displayName)
- Credential parameters (algorithms)
- Extension inputs (well-formed)

---

## Open Questions for Dennis

### Part 1: WebAuthn Client

1. **Challenge Store Scope:**
   - Should `IChallengeStore.GenerateAndStoreAsync` accept RP-provided challenges, or always generate fresh?
   - Proposal: Support both (if `challenge` parameter is null, generate; else, store provided challenge)

2. **Attestation Verification Default:**
   - Should attestation verification be automatic (with permissive default), or opt-in (explicit `VerifyAttestationAsync` call)?
   - Proposal: Automatic with permissive default (match browser behavior)

3. **Credential Selector UI:**
   - For multiple discoverable credentials, provide platform-specific selector helper, or just delegate?
   - Proposal: Delegate only (UI is platform-specific)

4. **Origin Helpers:**
   - Should `OriginHelper.GetApplicationOrigin()` use `Assembly.GetEntryAssembly()?.GetName().Name` or read from config?
   - Proposal: Assembly name as default, with override in `WebAuthnClient` constructor

5. **Extension Processor Registration:**
   - Should extension processors be globally registered (static registry) or per-client instance?
   - Proposal: Per-client instance (allows custom processors)

6. **Timeout Handling:**
   - Should `WebAuthnClient` enforce timeout from `PublicKeyCredentialCreationOptions.Timeout`, or let caller handle via `CancellationToken`?
   - Proposal: Caller-provided `CancellationToken` (more flexible)

7. **Progress Reporting:**
   - Is `IProgress<OperationStatus>` the right pattern, or use callbacks/events?
   - Proposal: `IProgress<T>` (idiomatic .NET)

8. **Namespace:**
   - Confirm `Yubico.YubiKit.Fido2.WebAuthn` namespace?
   - Alternative: `Yubico.YubiKit.WebAuthn` (top-level)

### Part 2: previewSign Extension

1. **Unsigned Extensions Handling:**
   - How to expose unsigned extension outputs (like previewSign att-obj)?
   - Proposal: `PublicKeyCredential.UnsignedExtensionResults` property (separate from `ClientExtensionResults`)

2. **Key Handle Encoding:**
   - Should SDK provide example key handle encoder (HMAC-based), or leave entirely to authenticator?
   - Proposal: No SDK implementation (authenticator-specific)

3. **COSE_Sign_Args:**
   - Spec says "optional" but doesn't define structure yet. Implement as opaque `byte[]` or wait for spec clarity?
   - Proposal: Opaque `ReadOnlyMemory<byte>?` until spec stabilizes

4. **Algorithm Tracking:**
   - Should `PreviewSignOutput.Signature` include the algorithm used, or assume caller tracks it?
   - Proposal: Include `CoseAlgorithmIdentifier Algorithm` property (less error-prone)

5. **Error Mapping:**
   - Map all CTAP errors to `PreviewSignException`, or let generic `CtapException` propagate?
   - Proposal: Map known previewSign errors, let others propagate

6. **YubiKey Support:**
   - Do we know which YubiKey firmware versions will support previewSign?
   - Action: Check with firmware team before implementation

7. **Multi-Credential Signing:**
   - `SignByCredential` allows signing for multiple credentials in one call. Should SDK support this, or restrict to single credential?
   - Proposal: Support full spec (multiple credentials)

8. **Attestation Object Storage:**
   - Should `GeneratedKey` output include full attestation object, or just signature?
   - Proposal: Include full attestation object (caller may need cert chain)

---

## Verification Steps

### After Part 1 Implementation

**Build:**
```bash
dotnet build src/Fido2/Yubico.YubiKit.Fido2.csproj --configuration Debug
```

**Unit Tests:**
```bash
dotnet toolchain.cs test --filter "FullyQualifiedName~WebAuthn&RequiresUserPresence!=true"
```

**Integration Tests (requires YubiKey):**
```bash
dotnet toolchain.cs test --filter "FullyQualifiedName~WebAuthn.Integration"
```

**Code Coverage:**
```bash
dotnet toolchain.cs coverage --filter "FullyQualifiedName~WebAuthn"
```
Target: ≥90% line coverage for WebAuthn namespace.

**Manual Verification:**
1. Create test app: `examples/WebAuthnDemo/`
2. Create credential with ES256
3. Get assertion with created credential
4. Verify signature manually
5. Test extensions: credProtect, prf, largeBlobKey
6. Test error scenarios: excluded credential, no credentials, timeout

**Documentation:**
```bash
dotnet msbuild src/Fido2/Yubico.YubiKit.Fido2.csproj /t:DocFXBuild
```
Verify XML docs for all public APIs.

### After Part 2 Implementation

**Build:**
```bash
dotnet build src/Fido2/Yubico.YubiKit.Fido2.csproj
```

**Unit Tests:**
```bash
dotnet toolchain.cs test --filter "FullyQualifiedName~PreviewSign"
```

**CBOR Wire Format Verification:**
- Capture CBOR bytes from `ExtensionBuilder.WithPreviewSign().Build()`
- Decode with online CBOR tool (http://cbor.me)
- Verify structure matches spec:
  - Registration: `{3: [-7, -8], 4: 1}`
  - Authentication: `{2: h'...kh...', 6: h'...tbs...'}`

**Integration Test (mock):**
1. Mock `IFidoSession` to return previewSign output
2. Call `WebAuthnClient.CreateCredentialAsync` with `previewSign.generateKey`
3. Verify `GeneratedKey` output contains public key, key handle, attestation object
4. Call `WebAuthnClient.GetAssertionAsync` with `previewSign.signByCredential`
5. Verify `Signature` output contains signature bytes

**Integration Test (real YubiKey, if supported):**
1. Check `AuthenticatorInfo.Extensions` for "previewSign"
2. If present:
   - Create credential with ES256 + previewSign
   - Extract signing public key
   - Sign test data: `"Hello, World!"`
   - Verify signature with public key using `ECDsa.VerifyData()`

**Interoperability:**
- If reference implementation exists (browser, another SDK):
  - Create signing key in browser
  - Sign data in .NET SDK with same credential
  - Verify signature matches

---

## Dependencies and Constraints

### Required Before Implementation

**Part 1:**
- None (builds on existing FIDO2 module)

**Part 2:**
- Part 1 must be complete (needs extension processor system)
- Confirm YubiKey firmware support timeline

### External Dependencies

**NuGet Packages (already in project):**
- `System.Formats.Cbor` - CBOR encoding/decoding
- `System.Security.Cryptography` - SHA-256, ECDSA, certificate validation

**New Dependencies (none):**
- All required libraries already present

### Compatibility

**Target Frameworks:**
- .NET 10 (primary)
- Check if backport to .NET 8 needed (unlikely for new features)

**YubiKey Firmware:**
- Part 1: All FIDO2-capable YubiKeys (5.x+)
- Part 2: TBD (depends on previewSign firmware support)

---

## Risks and Mitigations

### Risk: Origin Validation in Non-Browser Context

**Problem:** WebAuthn spec assumes browser origin (`https://example.com`). Desktop apps don't have standard origin format.

**Mitigation:**
- Use application identifier convention: `app://assembly-name`
- Document clearly in XML docs
- Provide `OriginHelper.GetApplicationOrigin()` for auto-detection
- Allow explicit override in `WebAuthnClient` constructor

### Risk: Challenge Replay Attacks

**Problem:** If challenges aren't one-time-use, attacker can replay authentication.

**Mitigation:**
- `IChallengeStore.ValidateAndConsumeAsync` is atomic (delete on first use)
- In-memory implementation uses `ConcurrentDictionary` with thread-safe removal
- Distributed implementations must use atomic operations (e.g., Redis `GETDEL`)

### Risk: Timing Attacks on Challenge Comparison

**Problem:** Byte-by-byte comparison leaks timing information.

**Mitigation:**
- Always use `CryptographicOperations.FixedTimeEquals`
- Document in code comments and security guidelines

### Risk: Attestation Verification Complexity

**Problem:** Full attestation verification requires metadata service, cert chains, revocation checks.

**Mitigation:**
- Default to permissive verifier (matches browser behavior)
- Provide strict verifier as opt-in
- Document trade-offs clearly

### Risk: previewSign Spec Draft Status

**Problem:** CTAP v4 draft may change before final spec.

**Mitigation:**
- Namespace as `Extensions.PreviewSign` (indicates experimental)
- Use version-specific CBOR map keys (allow future versioning)
- Document draft status in XML docs
- Mark as `[Experimental]` attribute if available in .NET 10

### Risk: YubiKey Firmware Support Unknown

**Problem:** Don't know which firmware versions will support previewSign.

**Mitigation:**
- Check `AuthenticatorInfo.Extensions` before use
- Throw descriptive exception if not supported
- Coordinate with firmware team for timeline

---

## Success Criteria

### Part 1: WebAuthn Client

✅ **Functionality:**
- Create credential (ES256, EdDSA, RS256)
- Get assertion (single credential)
- Get assertion (multiple credentials with selection)
- Extensions (credProtect, prf, largeBlobKey)
- Challenge lifecycle management
- Origin validation
- Attestation verification (permissive + strict)

✅ **Quality:**
- ≥90% line coverage
- All unit tests pass
- Integration tests pass with real YubiKey
- XML documentation for all public APIs
- No build warnings
- `dotnet format` clean

✅ **Security:**
- Fixed-time challenge comparison
- Sensitive data zeroed after use
- No PIN/key logging
- Challenge one-time-use enforced

✅ **Usability:**
- Idiomatic C# APIs
- Clear error messages
- Progress reporting available
- Example app demonstrating usage

### Part 2: previewSign Extension

✅ **Functionality:**
- Registration: generate signing key
- Authentication: sign arbitrary data
- CBOR encoding matches spec
- Error mapping
- Multi-credential signing

✅ **Quality:**
- ≥85% line coverage (lower due to hardware dependency)
- Unit tests pass
- Mock integration tests pass
- Real YubiKey tests pass (if firmware supported)
- XML documentation complete

✅ **Compliance:**
- CBOR wire format matches draft spec
- Extension identifier "previewSign"
- All CBOR map keys correct
- Error codes match spec

---

## Timeline Estimate

### Part 1: WebAuthn Client
- **Phase 1.1:** Core Types - 2 days
- **Phase 1.2:** Extension System - 3 days
- **Phase 1.3:** Client Implementation - 4 days
- **Phase 1.4:** Attestation Verification - 2 days
- **Phase 1.5:** Supporting Types - 1 day
- **Testing:** 3 days (unit + integration)
- **Documentation:** 1 day
- **Total:** ~16 days (3 weeks with buffer)

### Part 2: previewSign Extension
- **Phase 2.1:** Core Types - 1 day
- **Phase 2.2:** CBOR Encoding - 2 days
- **Phase 2.3:** Extension Processor - 1 day
- **Phase 2.4:** Error Handling - 0.5 days
- **Testing:** 2 days
- **Documentation:** 0.5 days
- **Total:** ~7 days (1.5 weeks)

**Grand Total:** ~23 days (~4.5 weeks)

**Note:** Timeline assumes:
- Single engineer working full-time
- No major spec changes during implementation
- YubiKey firmware support available for testing Part 2
- Standard review/iteration cycle (1-2 days per phase)

---

## Next Steps

1. **Dennis: Review and answer open questions** (8 for Part 1, 8 for Part 2)
2. **Dennis: Confirm timeline and priorities** (implement both parts together, or Part 1 first?)
3. **Engineer: Create feature branch** (`feature/webauthn-client-previewsign`)
4. **Engineer: Implement Part 1 Phase 1.1** (core types)
5. **Code Review: After each phase**
6. **QA: Integration test after Part 1 complete**
7. **Engineer: Implement Part 2** (after Part 1 merged or in parallel)
8. **Final Review: Both parts together**
9. **Documentation: Update root CLAUDE.md and FIDO2 CLAUDE.md with new APIs**
10. **Release: Increment minor version (1.X.0 → 1.Y.0)**

---

## References

**Specifications:**
- [WebAuthn Level 3](https://www.w3.org/TR/webauthn-3/) - W3C Recommendation
- [CTAP 2.1](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html) - FIDO Alliance
- `docs/research/DRAFT Web Authentication sign extension Signing arbitrary data using the Web Authentication API. Version 4.md` - previewSign spec

**Reference Implementations:**
- yubikit-swift: `/Users/Dennis.Dyall/Code/y/yubikit-swift/YubiKit/YubiKit/FIDO/`
- Current .NET FIDO2: `src/Fido2/`

**Design Documents:**
- Architect agent output (see agent ac6ab0ccbd2fd303a for full details)

---

**Plan Status:** Ready for Review
**Next Action:** Dennis to answer open questions, then hand off to Engineer for implementation.
