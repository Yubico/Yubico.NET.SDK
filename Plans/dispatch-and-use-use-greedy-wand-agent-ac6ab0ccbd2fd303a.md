# WebAuthn Client + previewSign Extension - Architectural Design

**Author**: Architect Agent (Opus)  
**Date**: 2026-04-22  
**Target**: Yubico.NET.SDK (FIDO2 module)  
**Frameworks**: .NET 10, C# 14

---

## Executive Summary

This document provides complete architectural specifications for two related features:

1. **WebAuthn Client Implementation** - Full browser-side WebAuthn logic (navigator.credentials.create/get equivalent) for .NET applications
2. **previewSign Extension** - CTAP v4 draft extension for arbitrary data signing with separate key pairs

Both designs integrate with the existing FIDO2 module architecture, reusing CBOR utilities, COSE key types, PIN/UV auth protocols, and the extension system.

---

## Meta-Analysis: Framing Correction (Lower → Higher Trust Mode)

**Original PRD Assumption**: "WebAuthn Client means browser integration"  
**Corrected Framing**: WebAuthn Client is the **protocol layer between RP and authenticator**, not the browser integration layer.

### What WebAuthn Client Actually Means

The orchestrator framed this as "browser-side logic" but marked "browser integration" as out of scope. This tension reveals a gap in understanding WebAuthn's architecture layers:

```
┌─────────────────────────────────┐
│  Browser (navigator.credentials)│  ← OUT OF SCOPE
├─────────────────────────────────┤
│  WebAuthn Client Layer          │  ← THIS DESIGN (what we're building)
│  - CollectedClientData          │
│  - Origin validation            │
│  - Challenge management         │
│  - PublicKeyCredential wrapper  │
│  - Timeout handling             │
├─────────────────────────────────┤
│  CTAP2 Protocol (IFidoSession)  │  ← ALREADY EXISTS
└─────────────────────────────────┘
```

**Key Insight**: In a .NET context, "origin" is NOT a browser origin. It's the **application identifier** (e.g., `app://my-desktop-app` or `https://example.com` for embedded webviews). The WebAuthn Client layer provides:

1. **Type-safe API** over raw CTAP (not byte arrays and CBOR maps)
2. **Challenge lifecycle** (generation, validation, replay protection)
3. **Client data JSON** (what the authenticator signs alongside RP data)
4. **Extension mapping** (WebAuthn extension names → CTAP extension encoding)
5. **Attestation verification** (cert chain validation, metadata service integration)

### Why This Matters for Implementation

- **DO** implement CollectedClientData JSON generation (it's part of the signature)
- **DO** implement challenge nonce tracking (prevents replay attacks)
- **DO** implement timeout handling (CTAP ops can wait for user touch indefinitely)
- **DO NOT** try to integrate with browser DOM (that's embedding, not client logic)
- **DO NOT** implement platform authenticator (YubiKey-only, hardware-backed)

The orchestrator correctly identified what's needed but used confusing terminology. This design corrects that framing.

---

# Plan 1: WebAuthn Client Implementation

## Problem Statement

The current FIDO2 module provides excellent **CTAP2 protocol access** (`IFidoSession`) but forces developers to:

1. Manually construct `clientDataHash` (SHA-256 of client data JSON)
2. Manually track challenges and prevent replay
3. Work with raw CBOR `ReadOnlyMemory<byte>` extension data
4. Handle timeout logic per-operation
5. Manually parse attestation objects for verification
6. Implement extension semantics (client input → authenticator input → client output)

**Goal**: Provide a **WebAuthn-compliant client library** that handles these concerns, presenting a type-safe, ergonomic .NET API while maintaining full control over the authenticator interaction.

**Success Criteria**:
- Registration flow produces `PublicKeyCredentialCreationResult` with attestation
- Authentication flow produces `PublicKeyCredentialAssertionResult` with signature
- Extensions work end-to-end (builder → CTAP → parsed output)
- Challenge replay is prevented
- Timeout cancellation works reliably
- Attestation verification supports common formats (packed, fido-u2f, none)

---

## Proposed Solution

### High-Level Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    WebAuthnClient                             │
│  - CreateCredentialAsync(options, origin, timeout)            │
│  - GetAssertionAsync(options, origin, timeout)                │
│  - Owns: IFidoSession, IChallengeStore, ITimeProvider         │
└──────────────────────────────────────────────────────────────┘
                            │
         ┌──────────────────┼──────────────────┐
         ▼                  ▼                  ▼
┌─────────────────┐  ┌─────────────┐  ┌────────────────────┐
│CollectedClientData│  │Extension    │  │PublicKeyCredential │
│    Generator      │  │  Processor  │  │     Result         │
└─────────────────┘  └─────────────┘  └────────────────────┘
                            │
                            ▼
                   ┌─────────────────┐
                   │  IFidoSession   │  (existing)
                   │  - MakeCredential│
                   │  - GetAssertion  │
                   └─────────────────┘
```

**Key Design Principle**: WebAuthnClient is a **thin orchestration layer** over IFidoSession. It does NOT replace CTAP—it adds WebAuthn semantics on top.

---

## Design Details

### Namespace Organization

```
Yubico.YubiKit.Fido2/
├── src/
│   ├── WebAuthn/                          # NEW - WebAuthn client layer
│   │   ├── WebAuthnClient.cs              # Main entry point
│   │   ├── IWebAuthnClient.cs             # Interface (for DI/testing)
│   │   ├── CollectedClientData.cs         # Client data JSON model
│   │   ├── PublicKeyCredentialCreationOptions.cs
│   │   ├── PublicKeyCredentialRequestOptions.cs
│   │   ├── PublicKeyCredential.cs         # Result wrappers
│   │   ├── AuthenticatorSelection.cs      # Criteria for auth selection
│   │   ├── ChallengeStore.cs              # In-memory challenge tracking
│   │   ├── OriginValidator.cs             # RP ID ↔ origin matching
│   │   ├── Attestation/                   # NEW - attestation verification
│   │   │   ├── IAttestationVerifier.cs
│   │   │   ├── PackedAttestationVerifier.cs
│   │   │   ├── FidoU2fAttestationVerifier.cs
│   │   │   ├── NoneAttestationVerifier.cs
│   │   │   └── AttestationResult.cs
│   │   └── Extensions/                    # WebAuthn extension mappers
│   │       ├── IWebAuthnExtensionProcessor.cs
│   │       ├── CredProtectExtensionProcessor.cs
│   │       ├── HmacSecretExtensionProcessor.cs
│   │       └── ... (one per extension)
│   │
│   ├── Credentials/                       # EXISTING - kept as-is
│   │   ├── MakeCredentialResponse.cs
│   │   ├── GetAssertionResponse.cs
│   │   └── ...
│   │
│   ├── Extensions/                        # EXISTING - kept as-is
│   │   ├── ExtensionBuilder.cs
│   │   ├── ExtensionOutput.cs
│   │   └── ...
│   │
│   └── FidoSession.cs                     # EXISTING - no changes
```

**Rationale**: Separate `WebAuthn/` namespace distinguishes WebAuthn client logic from CTAP protocol logic. Developers importing `Yubico.YubiKit.Fido2.WebAuthn` get the high-level API; those importing `Yubico.YubiKit.Fido2` get direct CTAP access.

---

### Public API Surface

#### 1. WebAuthnClient (Main Entry Point)

```csharp
namespace Yubico.YubiKit.Fido2.WebAuthn;

/// <summary>
/// WebAuthn client implementation for credential creation and assertion.
/// </summary>
/// <remarks>
/// <para>
/// Implements the WebAuthn Relying Party client logic per W3C WebAuthn Level 2 spec.
/// This class orchestrates the interaction between RP, client, and authenticator.
/// </para>
/// <para>
/// Thread-safe for concurrent operations on different credentials.
/// Challenge store is scoped to this instance.
/// </para>
/// </remarks>
public sealed class WebAuthnClient : IAsyncDisposable
{
    /// <summary>
    /// Creates a new WebAuthnClient wrapping an existing FIDO session.
    /// </summary>
    /// <param name="session">The FIDO session to use for authenticator operations.</param>
    /// <param name="challengeStore">Optional challenge store (defaults to in-memory).</param>
    /// <param name="timeProvider">Optional time provider (defaults to system time).</param>
    public WebAuthnClient(
        IFidoSession session,
        IChallengeStore? challengeStore = null,
        TimeProvider? timeProvider = null);
    
    /// <summary>
    /// Creates a new credential (registration/attestation ceremony).
    /// </summary>
    /// <param name="options">Credential creation options from RP.</param>
    /// <param name="origin">The origin of the calling application (e.g., "app://myapp").</param>
    /// <param name="crossOrigin">Whether this is a cross-origin operation.</param>
    /// <param name="timeout">Optional timeout (defaults to 60 seconds).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PublicKeyCredential with attestation.</returns>
    /// <exception cref="WebAuthnException">On WebAuthn-level errors.</exception>
    /// <exception cref="CtapException">On CTAP-level errors.</exception>
    public Task<PublicKeyCredentialCreationResult> CreateCredentialAsync(
        PublicKeyCredentialCreationOptions options,
        string origin,
        bool crossOrigin = false,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an assertion (authentication ceremony).
    /// </summary>
    /// <param name="options">Assertion request options from RP.</param>
    /// <param name="origin">The origin of the calling application.</param>
    /// <param name="crossOrigin">Whether this is a cross-origin operation.</param>
    /// <param name="timeout">Optional timeout (defaults to 60 seconds).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PublicKeyCredential with assertion.</returns>
    public Task<PublicKeyCredentialAssertionResult> GetAssertionAsync(
        PublicKeyCredentialRequestOptions options,
        string origin,
        bool crossOrigin = false,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Enumerates all matching credentials without performing assertion.
    /// </summary>
    /// <remarks>
    /// Useful for credential selection UI before calling GetAssertionAsync.
    /// Returns IAsyncEnumerable for pagination support when many credentials match.
    /// </remarks>
    public IAsyncEnumerable<PublicKeyCredentialDescriptor> EnumerateCredentialsAsync(
        PublicKeyCredentialRequestOptions options,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifies attestation for a credential creation result.
    /// </summary>
    /// <param name="result">The credential creation result.</param>
    /// <param name="options">The original creation options.</param>
    /// <param name="verifier">Optional custom attestation verifier.</param>
    /// <returns>Attestation verification result.</returns>
    public Task<AttestationResult> VerifyAttestationAsync(
        PublicKeyCredentialCreationResult result,
        PublicKeyCredentialCreationOptions options,
        IAttestationVerifier? verifier = null);
    
    public ValueTask DisposeAsync();
}
```

#### 2. PublicKeyCredential Types (Result Wrappers)

```csharp
/// <summary>
/// Result of a credential creation operation.
/// </summary>
public sealed record PublicKeyCredentialCreationResult
{
    /// <summary>Credential ID (base64url-encoded for WebAuthn compatibility).</summary>
    public required string Id { get; init; }
    
    /// <summary>Raw credential ID bytes.</summary>
    public required ReadOnlyMemory<byte> RawId { get; init; }
    
    /// <summary>Credential type (always "public-key").</summary>
    public required string Type { get; init; }
    
    /// <summary>Authenticator attestation response.</summary>
    public required AuthenticatorAttestationResponse Response { get; init; }
    
    /// <summary>Client extension results.</summary>
    public IReadOnlyDictionary<string, object?>? ClientExtensionResults { get; init; }
    
    /// <summary>Authenticator attachment (null for roaming authenticators like YubiKey).</summary>
    public string? AuthenticatorAttachment { get; init; }
}

/// <summary>
/// Authenticator attestation response.
/// </summary>
public sealed record AuthenticatorAttestationResponse
{
    /// <summary>Client data JSON bytes.</summary>
    public required ReadOnlyMemory<byte> ClientDataJson { get; init; }
    
    /// <summary>Attestation object CBOR bytes.</summary>
    public required ReadOnlyMemory<byte> AttestationObject { get; init; }
    
    /// <summary>Parsed attestation object (for convenience).</summary>
    public required MakeCredentialResponse ParsedAttestation { get; init; }
    
    /// <summary>Authenticator data bytes (extracted from attestation object).</summary>
    public ReadOnlyMemory<byte> AuthenticatorData => ParsedAttestation.AuthenticatorDataRaw;
    
    /// <summary>Public key algorithm (COSE algorithm identifier).</summary>
    public required int PublicKeyAlgorithm { get; init; }
    
    /// <summary>Public key bytes (COSE_Key CBOR encoding).</summary>
    public ReadOnlyMemory<byte> PublicKey => ParsedAttestation.GetCredentialPublicKey();
    
    /// <summary>Transports available for this authenticator.</summary>
    public IReadOnlyList<string>? Transports { get; init; }
}

/// <summary>
/// Result of an assertion operation.
/// </summary>
public sealed record PublicKeyCredentialAssertionResult
{
    public required string Id { get; init; }
    public required ReadOnlyMemory<byte> RawId { get; init; }
    public required string Type { get; init; }
    public required AuthenticatorAssertionResponse Response { get; init; }
    public IReadOnlyDictionary<string, object?>? ClientExtensionResults { get; init; }
    public string? AuthenticatorAttachment { get; init; }
}

/// <summary>
/// Authenticator assertion response.
/// </summary>
public sealed record AuthenticatorAssertionResponse
{
    public required ReadOnlyMemory<byte> ClientDataJson { get; init; }
    public required ReadOnlyMemory<byte> AuthenticatorData { get; init; }
    public required ReadOnlyMemory<byte> Signature { get; init; }
    public ReadOnlyMemory<byte>? UserHandle { get; init; }
}
```

#### 3. CollectedClientData

```csharp
/// <summary>
/// Represents the client data collected during a WebAuthn ceremony.
/// </summary>
/// <remarks>
/// Serialized to JSON and hashed (SHA-256) to produce clientDataHash for CTAP.
/// </remarks>
public sealed record CollectedClientData
{
    /// <summary>Type of operation ("webauthn.create" or "webauthn.get").</summary>
    public required string Type { get; init; }
    
    /// <summary>Base64url-encoded challenge from RP.</summary>
    public required string Challenge { get; init; }
    
    /// <summary>Origin of the calling application.</summary>
    public required string Origin { get; init; }
    
    /// <summary>Whether this is a cross-origin operation.</summary>
    public bool CrossOrigin { get; init; }
    
    /// <summary>Serializes to canonical JSON.</summary>
    public string ToJson();
    
    /// <summary>Computes SHA-256 hash of JSON (clientDataHash for CTAP).</summary>
    public byte[] ComputeHash();
    
    /// <summary>Parses from JSON bytes.</summary>
    public static CollectedClientData FromJson(ReadOnlySpan<byte> json);
}
```

#### 4. Options Types (Input from RP)

```csharp
/// <summary>
/// Options for credential creation (from RP).
/// </summary>
public sealed record PublicKeyCredentialCreationOptions
{
    /// <summary>Relying party information.</summary>
    public required PublicKeyCredentialRpEntity Rp { get; init; }
    
    /// <summary>User information.</summary>
    public required PublicKeyCredentialUserEntity User { get; init; }
    
    /// <summary>Challenge from RP (raw bytes, not base64url).</summary>
    public required ReadOnlyMemory<byte> Challenge { get; init; }
    
    /// <summary>Supported public key algorithms in preference order.</summary>
    public required IReadOnlyList<PublicKeyCredentialParameters> PubKeyCredParams { get; init; }
    
    /// <summary>Optional timeout in milliseconds.</summary>
    public uint? Timeout { get; init; }
    
    /// <summary>Credentials to exclude (prevent duplicate registration).</summary>
    public IReadOnlyList<PublicKeyCredentialDescriptor>? ExcludeCredentials { get; init; }
    
    /// <summary>Authenticator selection criteria.</summary>
    public AuthenticatorSelectionCriteria? AuthenticatorSelection { get; init; }
    
    /// <summary>Attestation preference ("none", "indirect", "direct", "enterprise").</summary>
    public string? Attestation { get; init; }
    
    /// <summary>WebAuthn extensions.</summary>
    public IReadOnlyDictionary<string, object?>? Extensions { get; init; }
}

/// <summary>
/// Options for assertion request (from RP).
/// </summary>
public sealed record PublicKeyCredentialRequestOptions
{
    public required ReadOnlyMemory<byte> Challenge { get; init; }
    public uint? Timeout { get; init; }
    public required string RpId { get; init; }
    public IReadOnlyList<PublicKeyCredentialDescriptor>? AllowCredentials { get; init; }
    public string? UserVerification { get; init; }
    public IReadOnlyDictionary<string, object?>? Extensions { get; init; }
}

/// <summary>
/// Authenticator selection criteria.
/// </summary>
public sealed record AuthenticatorSelectionCriteria
{
    /// <summary>Authenticator attachment ("platform", "cross-platform", or null).</summary>
    public string? AuthenticatorAttachment { get; init; }
    
    /// <summary>Resident key requirement ("discouraged", "preferred", "required").</summary>
    public string? ResidentKey { get; init; }
    
    /// <summary>User verification requirement ("required", "preferred", "discouraged").</summary>
    public string? UserVerification { get; init; }
}
```

#### 5. Challenge Management

```csharp
/// <summary>
/// Interface for storing and validating challenges.
/// </summary>
public interface IChallengeStore
{
    /// <summary>Stores a challenge with expiration.</summary>
    ValueTask StoreAsync(ReadOnlyMemory<byte> challenge, TimeSpan expiration);
    
    /// <summary>Validates and consumes a challenge (one-time use).</summary>
    ValueTask<bool> ValidateAndConsumeAsync(ReadOnlyMemory<byte> challenge);
    
    /// <summary>Generates a new cryptographically random challenge.</summary>
    byte[] GenerateChallenge(int length = 32);
}

/// <summary>
/// In-memory challenge store (default implementation).
/// </summary>
/// <remarks>
/// Thread-safe. Challenges expire after configured duration (default 5 minutes).
/// Used once and removed. Not suitable for multi-instance deployments (use distributed store).
/// </remarks>
public sealed class InMemoryChallengeStore : IChallengeStore
{
    public InMemoryChallengeStore(TimeSpan? defaultExpiration = null);
    // ... implementation
}
```

#### 6. Attestation Verification

```csharp
/// <summary>
/// Verifies attestation statements.
/// </summary>
public interface IAttestationVerifier
{
    /// <summary>Verifies attestation for a given format.</summary>
    Task<AttestationResult> VerifyAsync(
        string format,
        AttestationStatement statement,
        ReadOnlyMemory<byte> authenticatorData,
        ReadOnlyMemory<byte> clientDataHash);
}

/// <summary>
/// Result of attestation verification.
/// </summary>
public sealed record AttestationResult
{
    public required bool IsValid { get; init; }
    public required AttestationType Type { get; init; }
    public IReadOnlyList<string>? TrustPath { get; init; }
    public string? ErrorMessage { get; init; }
}

public enum AttestationType
{
    None,           // Self-attestation
    Basic,          // Vendor cert chain
    AttestationCA,  // Attestation CA
    ECDAA           // ECDAA signature
}
```

---

### Data Flow Diagrams

#### Create Credential Flow

```
┌─────────┐                     ┌────────────────┐                  ┌─────────────┐
│   RP    │                     │ WebAuthnClient │                  │IFidoSession │
└────┬────┘                     └───────┬────────┘                  └──────┬──────┘
     │                                  │                                   │
     │ 1. PublicKeyCredentialCreation   │                                   │
     │    Options (challenge, user, rp) │                                   │
     ├─────────────────────────────────>│                                   │
     │                                  │                                   │
     │                                  │ 2. Validate challenge not used    │
     │                                  │    Store challenge with expiry    │
     │                                  │                                   │
     │                                  │ 3. Build CollectedClientData      │
     │                                  │    {type: "webauthn.create",      │
     │                                  │     challenge: base64url(...),    │
     │                                  │     origin: "app://myapp"}        │
     │                                  │                                   │
     │                                  │ 4. clientDataHash = SHA256(JSON)  │
     │                                  │                                   │
     │                                  │ 5. Process extensions             │
     │                                  │    (WebAuthn → CTAP mapping)      │
     │                                  │                                   │
     │                                  │ 6. MakeCredentialAsync()          │
     │                                  ├──────────────────────────────────>│
     │                                  │                                   │
     │                                  │  [User touches YubiKey]           │
     │                                  │                                   │
     │                                  │ 7. MakeCredentialResponse         │
     │                                  │<──────────────────────────────────┤
     │                                  │                                   │
     │                                  │ 8. Parse extension outputs        │
     │                                  │    (CTAP → WebAuthn mapping)      │
     │                                  │                                   │
     │                                  │ 9. Build PublicKeyCredential      │
     │                                  │    result with attestation        │
     │                                  │                                   │
     │ 10. PublicKeyCredentialCreation  │                                   │
     │     Result (id, attestationObj)  │                                   │
     │<─────────────────────────────────┤                                   │
     │                                  │                                   │
     │ 11. (Optional) Verify attestation│                                   │
     ├─────────────────────────────────>│                                   │
     │                                  │                                   │
     │ 12. AttestationResult            │                                   │
     │<─────────────────────────────────┤                                   │
     │                                  │                                   │
```

#### Get Assertion Flow (Multiple Credentials)

```
┌─────────┐                     ┌────────────────┐                  ┌─────────────┐
│   RP    │                     │ WebAuthnClient │                  │IFidoSession │
└────┬────┘                     └───────┬────────┘                  └──────┬──────┘
     │                                  │                                   │
     │ 1. PublicKeyCredentialRequest    │                                   │
     │    Options (rpId, challenge)     │                                   │
     ├─────────────────────────────────>│                                   │
     │                                  │                                   │
     │                                  │ 2. Validate + store challenge     │
     │                                  │                                   │
     │                                  │ 3. Build CollectedClientData      │
     │                                  │    {type: "webauthn.get", ...}    │
     │                                  │                                   │
     │                                  │ 4. clientDataHash = SHA256(JSON)  │
     │                                  │                                   │
     │                                  │ 5. GetAssertionAsync()            │
     │                                  ├──────────────────────────────────>│
     │                                  │                                   │
     │                                  │  [User touches YubiKey]           │
     │                                  │                                   │
     │                                  │ 6. GetAssertionResponse           │
     │                                  │    (numberOfCredentials: 3)       │
     │                                  │<──────────────────────────────────┤
     │                                  │                                   │
     │                                  │ 7. If numberOfCredentials > 1:    │
     │                                  │    Loop GetNextAssertionAsync()   │
     │                                  ├──────────────────────────────────>│
     │                                  │<──────────────────────────────────┤
     │                                  │    (repeat for each credential)   │
     │                                  │                                   │
     │                                  │ 8. Select credential (policy):    │
     │                                  │    - First if allowList specified │
     │                                  │    - User selection if UI exists  │
     │                                  │    - Throw if ambiguous           │
     │                                  │                                   │
     │                                  │ 9. Build PublicKeyCredential      │
     │                                  │    result with assertion          │
     │                                  │                                   │
     │ 10. PublicKeyCredentialAssertion │                                   │
     │     Result (id, signature)       │                                   │
     │<─────────────────────────────────┤                                   │
     │                                  │                                   │
```

---

### Extension System Integration

**Problem**: WebAuthn extensions use different names/formats than CTAP extensions.

**Example**: `prf` (WebAuthn) vs `hmac-secret` (CTAP)

**Solution**: Extension processor pattern

```csharp
/// <summary>
/// Processes a WebAuthn extension bidirectionally (client ↔ authenticator).
/// </summary>
public interface IWebAuthnExtensionProcessor
{
    /// <summary>Extension identifier in WebAuthn namespace.</summary>
    string WebAuthnIdentifier { get; }
    
    /// <summary>Converts WebAuthn client input to CTAP authenticator input.</summary>
    void ProcessClientInput(
        object? clientInput,
        ExtensionBuilder ctapBuilder,
        WebAuthnContext context);
    
    /// <summary>Converts CTAP authenticator output to WebAuthn client output.</summary>
    object? ProcessAuthenticatorOutput(
        ExtensionOutput ctapOutput,
        WebAuthnContext context);
}

/// <summary>
/// Context passed to extension processors.
/// </summary>
public sealed class WebAuthnContext
{
    public required IFidoSession Session { get; init; }
    public required string RpId { get; init; }
    public required ReadOnlyMemory<byte> ClientDataHash { get; init; }
    public IPinUvAuthProtocol? PinUvAuthProtocol { get; init; }
    public ReadOnlyMemory<byte>? PinToken { get; init; }
}
```

**Example: PRF Extension Processor**

```csharp
public sealed class PrfExtensionProcessor : IWebAuthnExtensionProcessor
{
    public string WebAuthnIdentifier => "prf";
    
    public void ProcessClientInput(
        object? clientInput,
        ExtensionBuilder ctapBuilder,
        WebAuthnContext context)
    {
        if (clientInput is not PrfClientInput prfInput)
            throw new ArgumentException("Invalid prf input");
        
        // WebAuthn PRF uses base64url-encoded salts
        // CTAP hmac-secret uses raw bytes
        var salt1 = Convert.FromBase64String(prfInput.Eval.First);
        var salt2 = prfInput.Eval.Second is { } s2
            ? Convert.FromBase64String(s2)
            : null;
        
        // Map to CTAP extension
        if (context.PinUvAuthProtocol is not null)
        {
            ctapBuilder.WithHmacSecret(
                context.PinUvAuthProtocol,
                sharedSecret: context.PinToken!.Span,
                keyAgreement: ...,
                salt1: salt1,
                salt2: salt2 ?? ReadOnlySpan<byte>.Empty);
        }
    }
    
    public object? ProcessAuthenticatorOutput(
        ExtensionOutput ctapOutput,
        WebAuthnContext context)
    {
        if (!ctapOutput.TryGetHmacSecret(out var hmacOutput))
            return null;
        
        // CTAP returns encrypted outputs
        // Decrypt and return as WebAuthn PRF output
        var decrypted = context.PinUvAuthProtocol!.Decrypt(
            context.PinToken!.Span,
            hmacOutput.OutputEnc.Span);
        
        return new PrfClientOutput
        {
            Results = new()
            {
                First = Convert.ToBase64String(decrypted[..32]),
                Second = decrypted.Length > 32
                    ? Convert.ToBase64String(decrypted[32..])
                    : null
            }
        };
    }
}
```

---

### Timeout Handling Strategy

**Challenge**: CTAP operations wait for user interaction (touch) indefinitely. WebAuthn specifies timeout.

**Solution**: Use `CancellationTokenSource` with timeout, wrap CTAP exceptions.

```csharp
private async Task<MakeCredentialResponse> MakeCredentialWithTimeoutAsync(
    /* params */,
    TimeSpan timeout,
    CancellationToken cancellationToken)
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    cts.CancelAfter(timeout);
    
    try
    {
        return await _session.MakeCredentialAsync(
            clientDataHash, rp, user, pubKeyCredParams, options, cts.Token);
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
        // Timeout occurred (not user cancellation)
        throw new WebAuthnException(
            "Operation timed out waiting for user interaction",
            WebAuthnErrorCode.Timeout);
    }
}
```

---

### Origin Validation

**WebAuthn Spec**: RP ID must be a registrable domain suffix of origin.

**Examples**:
- RP ID: `example.com` ← Origin: `https://login.example.com` ✅
- RP ID: `example.com` ← Origin: `https://evil.com` ❌
- RP ID: `myapp` ← Origin: `app://myapp` ✅ (custom scheme, exact match)

**Implementation**:

```csharp
public static class OriginValidator
{
    /// <summary>
    /// Validates that RP ID matches origin per WebAuthn spec.
    /// </summary>
    public static bool IsValidOrigin(string rpId, string origin)
    {
        // Parse origin
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
            return false;
        
        // Custom schemes (app://, windows://) require exact match
        if (originUri.Scheme is not ("https" or "http"))
        {
            return string.Equals(rpId, originUri.Host, StringComparison.OrdinalIgnoreCase);
        }
        
        // HTTPS: RP ID must be suffix of origin host
        var originHost = originUri.Host;
        
        // Exact match
        if (string.Equals(rpId, originHost, StringComparison.OrdinalIgnoreCase))
            return true;
        
        // Suffix match (e.g., "example.com" suffix of "login.example.com")
        if (originHost.EndsWith($".{rpId}", StringComparison.OrdinalIgnoreCase))
            return true;
        
        return false;
    }
}
```

---

### Credential Selection Logic

**Scenario**: `GetAssertionAsync` returns `numberOfCredentials > 1`.

**Options**:
1. **RP specified allowList** → Return first (RP already filtered)
2. **No allowList, UI available** → Prompt user (via callback)
3. **No allowList, no UI** → Throw `WebAuthnException` with credential list

**Implementation**:

```csharp
public delegate Task<PublicKeyCredentialDescriptor?> CredentialSelectorDelegate(
    IReadOnlyList<PublicKeyCredentialDescriptor> credentials,
    CancellationToken cancellationToken);

// In WebAuthnClient constructor:
public WebAuthnClient(
    IFidoSession session,
    IChallengeStore? challengeStore = null,
    TimeProvider? timeProvider = null,
    CredentialSelectorDelegate? credentialSelector = null)
{
    _session = session;
    _challengeStore = challengeStore ?? new InMemoryChallengeStore();
    _timeProvider = timeProvider ?? TimeProvider.System;
    _credentialSelector = credentialSelector;
}

// In GetAssertionAsync:
if (numberOfCredentials > 1)
{
    var allAssertions = new List<GetAssertionResponse> { firstAssertion };
    
    for (int i = 1; i < numberOfCredentials; i++)
    {
        allAssertions.Add(await _session.GetNextAssertionAsync(cancellationToken));
    }
    
    if (options.AllowCredentials is { Count: > 0 })
    {
        // RP filtered, use first
        selectedAssertion = allAssertions[0];
    }
    else if (_credentialSelector is not null)
    {
        // User selection
        var descriptors = allAssertions
            .Select(a => a.Credential!)
            .ToList();
        
        var selected = await _credentialSelector(descriptors, cancellationToken);
        selectedAssertion = allAssertions
            .First(a => a.Credential?.Id.Span.SequenceEqual(selected.Id.Span) == true);
    }
    else
    {
        // No selection mechanism → fail with context
        throw new WebAuthnException(
            $"Multiple credentials ({numberOfCredentials}) matched but no selector provided",
            WebAuthnErrorCode.AmbiguousCredential,
            new { Credentials = allAssertions.Select(a => a.Credential).ToList() });
    }
}
```

---

## Critical Files to Create/Modify

### New Files

| Path | Purpose | Lines (est.) |
|------|---------|--------------|
| `src/Fido2/src/WebAuthn/WebAuthnClient.cs` | Main client implementation | ~500 |
| `src/Fido2/src/WebAuthn/IWebAuthnClient.cs` | Interface for DI | ~50 |
| `src/Fido2/src/WebAuthn/CollectedClientData.cs` | Client data model + JSON | ~150 |
| `src/Fido2/src/WebAuthn/PublicKeyCredential.cs` | Result types (all records) | ~200 |
| `src/Fido2/src/WebAuthn/PublicKeyCredentialCreationOptions.cs` | Options record | ~50 |
| `src/Fido2/src/WebAuthn/PublicKeyCredentialRequestOptions.cs` | Options record | ~40 |
| `src/Fido2/src/WebAuthn/AuthenticatorSelectionCriteria.cs` | Selection record | ~30 |
| `src/Fido2/src/WebAuthn/ChallengeStore.cs` | In-memory challenge store | ~100 |
| `src/Fido2/src/WebAuthn/IChallengeStore.cs` | Challenge store interface | ~30 |
| `src/Fido2/src/WebAuthn/OriginValidator.cs` | Origin validation static class | ~80 |
| `src/Fido2/src/WebAuthn/WebAuthnException.cs` | Custom exception type | ~60 |
| `src/Fido2/src/WebAuthn/Attestation/IAttestationVerifier.cs` | Verifier interface | ~40 |
| `src/Fido2/src/WebAuthn/Attestation/PackedAttestationVerifier.cs` | Packed format verifier | ~200 |
| `src/Fido2/src/WebAuthn/Attestation/FidoU2fAttestationVerifier.cs` | U2F format verifier | ~150 |
| `src/Fido2/src/WebAuthn/Attestation/NoneAttestationVerifier.cs` | None format verifier | ~50 |
| `src/Fido2/src/WebAuthn/Attestation/AttestationResult.cs` | Result record | ~40 |
| `src/Fido2/src/WebAuthn/Extensions/IWebAuthnExtensionProcessor.cs` | Processor interface | ~50 |
| `src/Fido2/src/WebAuthn/Extensions/PrfExtensionProcessor.cs` | PRF extension | ~150 |
| `src/Fido2/src/WebAuthn/Extensions/CredProtectExtensionProcessor.cs` | credProtect extension | ~80 |
| `src/Fido2/src/WebAuthn/Extensions/WebAuthnContext.cs` | Extension context | ~40 |

**Total new code**: ~2,100 LOC

### Modified Files

None. WebAuthn layer is additive.

---

## Test Strategy

### Unit Tests (No Hardware)

**Focus**: Logic, serialization, validation

```csharp
// Test file: src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/WebAuthn/CollectedClientDataTests.cs

[Fact]
public void CollectedClientData_ToJson_ProducesCanonicalFormat()
{
    var clientData = new CollectedClientData
    {
        Type = "webauthn.create",
        Challenge = "Y2hhbGxlbmdl", // base64url
        Origin = "app://myapp",
        CrossOrigin = false
    };
    
    var json = clientData.ToJson();
    
    Assert.Contains("\"type\":\"webauthn.create\"", json);
    Assert.Contains("\"challenge\":\"Y2hhbGxlbmdl\"", json);
    Assert.DoesNotContain("crossOrigin", json); // false is omitted
}

[Fact]
public void CollectedClientData_ComputeHash_ReturnsSha256()
{
    var clientData = new CollectedClientData { /* ... */ };
    var hash = clientData.ComputeHash();
    
    Assert.Equal(32, hash.Length);
}

[Fact]
public void OriginValidator_ValidatesHttpsOrigin()
{
    Assert.True(OriginValidator.IsValidOrigin("example.com", "https://example.com"));
    Assert.True(OriginValidator.IsValidOrigin("example.com", "https://login.example.com"));
    Assert.False(OriginValidator.IsValidOrigin("example.com", "https://evil.com"));
}

[Fact]
public void OriginValidator_ValidatesCustomScheme()
{
    Assert.True(OriginValidator.IsValidOrigin("myapp", "app://myapp"));
    Assert.False(OriginValidator.IsValidOrigin("myapp", "app://otherapp"));
}

[Fact]
public async Task ChallengeStore_GenerateChallenge_Returns32Bytes()
{
    var store = new InMemoryChallengeStore();
    var challenge = store.GenerateChallenge();
    
    Assert.Equal(32, challenge.Length);
}

[Fact]
public async Task ChallengeStore_ValidateAndConsume_RemovesChallenge()
{
    var store = new InMemoryChallengeStore();
    var challenge = store.GenerateChallenge();
    
    await store.StoreAsync(challenge, TimeSpan.FromMinutes(5));
    
    Assert.True(await store.ValidateAndConsumeAsync(challenge));
    Assert.False(await store.ValidateAndConsumeAsync(challenge)); // Second time fails
}

[Fact]
public async Task ChallengeStore_ExpiresAfterTimeout()
{
    var timeProvider = new FakeTimeProvider();
    var store = new InMemoryChallengeStore(timeProvider);
    var challenge = store.GenerateChallenge();
    
    await store.StoreAsync(challenge, TimeSpan.FromSeconds(10));
    
    timeProvider.Advance(TimeSpan.FromSeconds(11));
    
    Assert.False(await store.ValidateAndConsumeAsync(challenge));
}
```

### Integration Tests (Requires YubiKey)

**Focus**: End-to-end flows with real authenticator

```csharp
// Test file: src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/WebAuthn/WebAuthnClientTests.cs

[Fact]
public async Task CreateCredentialAsync_WithDiscoverableCredential_Succeeds()
{
    await using var session = await _yubiKey.CreateFidoSessionAsync();
    var client = new WebAuthnClient(session);
    
    var options = new PublicKeyCredentialCreationOptions
    {
        Rp = new PublicKeyCredentialRpEntity { Id = "example.com", Name = "Example" },
        User = new PublicKeyCredentialUserEntity
        {
            Id = RandomNumberGenerator.GetBytes(16),
            Name = "user@example.com",
            DisplayName = "Test User"
        },
        Challenge = RandomNumberGenerator.GetBytes(32),
        PubKeyCredParams =
        [
            new PublicKeyCredentialParameters { Type = "public-key", Alg = -7 } // ES256
        ],
        AuthenticatorSelection = new AuthenticatorSelectionCriteria
        {
            ResidentKey = "required",
            UserVerification = "discouraged" // No PIN for test
        }
    };
    
    var result = await client.CreateCredentialAsync(
        options,
        origin: "https://example.com",
        timeout: TimeSpan.FromSeconds(30));
    
    Assert.NotNull(result);
    Assert.NotEmpty(result.Id);
    Assert.Equal("public-key", result.Type);
    Assert.NotEmpty(result.Response.ClientDataJson);
    Assert.NotEmpty(result.Response.AttestationObject);
}

[Fact]
public async Task GetAssertionAsync_WithSingleCredential_Succeeds()
{
    // Arrange: Create credential first
    await using var session = await _yubiKey.CreateFidoSessionAsync();
    var client = new WebAuthnClient(session);
    
    var createResult = await CreateTestCredential(client); // Helper
    
    // Act: Authenticate
    var options = new PublicKeyCredentialRequestOptions
    {
        Challenge = RandomNumberGenerator.GetBytes(32),
        RpId = "example.com",
        AllowCredentials =
        [
            new PublicKeyCredentialDescriptor
            {
                Type = "public-key",
                Id = createResult.RawId
            }
        ]
    };
    
    var assertionResult = await client.GetAssertionAsync(
        options,
        origin: "https://example.com",
        timeout: TimeSpan.FromSeconds(30));
    
    // Assert
    Assert.NotNull(assertionResult);
    Assert.Equal(createResult.Id, assertionResult.Id);
    Assert.NotEmpty(assertionResult.Response.Signature);
}

[Fact]
public async Task GetAssertionAsync_WithMultipleCredentials_UsesSelector()
{
    // Create 3 credentials for same RP
    await using var session = await _yubiKey.CreateFidoSessionAsync();
    var client = new WebAuthnClient(
        session,
        credentialSelector: async (creds, ct) =>
        {
            // Select second credential
            return creds[1];
        });
    
    var cred1 = await CreateTestCredential(client, userId: [1]);
    var cred2 = await CreateTestCredential(client, userId: [2]);
    var cred3 = await CreateTestCredential(client, userId: [3]);
    
    var options = new PublicKeyCredentialRequestOptions
    {
        Challenge = RandomNumberGenerator.GetBytes(32),
        RpId = "example.com",
        // No allowCredentials → all 3 match
    };
    
    var result = await client.GetAssertionAsync(options, "https://example.com");
    
    Assert.Equal(cred2.Id, result.Id);
}

[Fact]
public async Task CreateCredentialAsync_Timeout_ThrowsWebAuthnException()
{
    await using var session = await _yubiKey.CreateFidoSessionAsync();
    var client = new WebAuthnClient(session);
    
    var options = /* ... */;
    
    // Do NOT touch YubiKey
    await Assert.ThrowsAsync<WebAuthnException>(async () =>
    {
        await client.CreateCredentialAsync(
            options,
            origin: "https://example.com",
            timeout: TimeSpan.FromSeconds(2)); // Short timeout
    });
}
```

### Test Coverage Goals

- **Unit tests**: 90%+ coverage for WebAuthn layer logic
- **Integration tests**: All happy paths + major error cases
- **Extension processors**: At least one integration test per processor
- **Attestation verifiers**: Test with real YubiKey attestation certs

---

## Security Considerations

### Sensitive Data Handling

1. **Challenges**: Zero after use
   ```csharp
   CryptographicOperations.ZeroMemory(challengeBytes);
   ```

2. **Client data hash**: Use `stackalloc` (≤512 bytes), zero after use
   ```csharp
   Span<byte> clientDataHash = stackalloc byte[32];
   SHA256.HashData(clientDataJson, clientDataHash);
   // Use clientDataHash
   CryptographicOperations.ZeroMemory(clientDataHash);
   ```

3. **PIN tokens**: Already handled by `IPinUvAuthProtocol` disposal

### Timing Attacks

- **Challenge comparison**: Use `CryptographicOperations.FixedTimeEquals`
- **RP ID hash verification**: Already uses fixed-time comparison in `AuthenticatorData.VerifyRpIdHash`

### Replay Protection

- **One-time challenge use**: `ValidateAndConsumeAsync` removes challenge
- **Challenge expiration**: Default 5 minutes (configurable)

### Input Validation

- **Origin format**: Must parse as valid URI
- **RP ID**: Must match origin per WebAuthn spec
- **Challenge length**: Minimum 16 bytes (recommend 32)
- **Credential ID**: Maximum length enforced (authenticator-specific)

### Attestation Verification

**Default behavior**: Accept all attestation formats (trust on first use model)

**Opt-in strict verification**:
```csharp
var strictVerifier = new StrictAttestationVerifier(trustedCertificates);
var result = await client.VerifyAttestationAsync(
    credentialResult,
    options,
    verifier: strictVerifier);

if (!result.IsValid)
{
    throw new SecurityException($"Attestation verification failed: {result.ErrorMessage}");
}
```

---

## Open Questions for Dennis

1. **Challenge Store Scope**: Should `WebAuthnClient` own challenge generation, or should RP pass pre-generated challenges? Current design assumes client-generated (more secure, prevents RP from reusing challenges).

2. **Attestation Verification Default**: Should attestation verification be automatic (with option to skip) or opt-in (current design)? Automatic is more secure but may break workflows expecting self-attestation.

3. **Credential Selector Callback**: Is a delegate-based selector sufficient, or should we provide a default UI prompt (platform-specific)? Current design uses delegate for flexibility.

4. **Origin for Desktop Apps**: Should we provide helpers for common patterns (e.g., `app://{assemblyName}` auto-generation)? Current design requires explicit origin.

5. **Extension Processor Registration**: Should extension processors be auto-discovered (reflection) or manually registered? Current design assumes manual registration for predictability.

6. **Timeout Defaults**: 60 seconds is generous (WebAuthn spec suggests 120-300 seconds). Should we align with spec or keep developer-friendly defaults?

7. **Multi-Instance Challenge Store**: Should we include a distributed challenge store implementation (Redis, SQL) or document the interface for implementers? Current design provides in-memory only.

8. **Backwards Compatibility**: Should `IFidoSession` gain convenience overloads that accept `CollectedClientData` directly, or keep WebAuthn layer separate? Current design keeps them separate for clean abstraction.

---

# Plan 2: previewSign Extension Implementation

## Problem Statement

CTAP v4 introduces `previewSign` extension for generating **separate signing key pairs** alongside WebAuthn credential key pairs. Use case: Verifiable credential signing keys bound to WebAuthn credential identity.

**Key difference from standard WebAuthn**:
- Standard: One key pair per credential (for authentication)
- previewSign: Two key pairs per credential (one for auth, one for signing arbitrary data)

**CTAP v4 spec summary** (from PRD):
- Registration: RP provides algorithm list, authenticator returns signing public key + attestation + key handle
- Authentication: RP provides key handle + data to sign, authenticator returns signature over raw data
- UP/UV policy: Fixed at creation time via `flags` parameter
- Error handling: Specific error codes for invalid algorithm, missing credential, etc.

**Goal**: Implement previewSign extension that integrates with WebAuthn Client extension system.

---

## Proposed Solution

### High-Level Architecture

```
┌────────────────────────────────────────────────────────────┐
│         WebAuthnClient (Plan 1)                            │
│  - CreateCredentialAsync (with previewSign extension)      │
│  - GetAssertionAsync (with previewSign extension)          │
└────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌────────────────────────────────────────────────────────────┐
│   PreviewSignExtensionProcessor (IWebAuthnExtensionProcessor)│
│  - WebAuthnIdentifier: "previewSign"                       │
│  - ProcessClientInput → CTAP encoding                      │
│  - ProcessAuthenticatorOutput → Client output              │
└────────────────────────────────────────────────────────────┘
                         │
         ┌───────────────┼───────────────┐
         ▼               ▼               ▼
┌─────────────┐  ┌──────────────┐  ┌─────────────────┐
│PreviewSign  │  │ KeyHandle    │  │ SigningRequest  │
│Input/Output │  │ (encoding)   │  │ (CBOR)          │
└─────────────┘  └──────────────┘  └─────────────────┘
```

**Integration Point**: Uses existing `ExtensionBuilder` (Plan 1) for CBOR encoding, extends it with previewSign-specific methods.

---

## Design Details

### Namespace Organization

```
Yubico.YubiKit.Fido2/
├── src/
│   ├── Extensions/                        # EXISTING
│   │   ├── ExtensionBuilder.cs            # MODIFY - add previewSign methods
│   │   ├── ExtensionOutput.cs             # MODIFY - add previewSign parsing
│   │   └── ExtensionIdentifiers.cs        # MODIFY - add constant
│   │
│   └── WebAuthn/                          # NEW (from Plan 1)
│       ├── Extensions/
│       │   ├── IWebAuthnExtensionProcessor.cs
│       │   └── PreviewSignExtensionProcessor.cs  # NEW
│       │
│       └── PreviewSign/                   # NEW - previewSign types
│           ├── PreviewSignInput.cs
│           ├── PreviewSignOutput.cs
│           ├── SigningKeyHandle.cs
│           ├── SigningRequest.cs
│           ├── PreviewSignFlags.cs
│           └── CoseSignArgs.cs
```

---

### Data Structures

#### 1. PreviewSignInput (WebAuthn Client Input)

```csharp
namespace Yubico.YubiKit.Fido2.WebAuthn.PreviewSign;

/// <summary>
/// Input for previewSign extension (WebAuthn layer).
/// </summary>
public abstract record PreviewSignInput
{
    /// <summary>
    /// Registration: Generate new signing key.
    /// </summary>
    public sealed record GenerateKey : PreviewSignInput
    {
        /// <summary>Supported signing algorithms in preference order (COSE algorithm IDs).</summary>
        public required IReadOnlyList<int> Algorithms { get; init; }
        
        /// <summary>User presence/verification policy for signing operations.</summary>
        public PreviewSignFlags Flags { get; init; } = PreviewSignFlags.None;
    }
    
    /// <summary>
    /// Authentication: Sign arbitrary data using existing signing key.
    /// </summary>
    public sealed record SignByCredential : PreviewSignInput
    {
        /// <summary>Key handle identifying the signing key.</summary>
        public required SigningKeyHandle KeyHandle { get; init; }
        
        /// <summary>Data to be signed (TBS = To Be Signed).</summary>
        public required ReadOnlyMemory<byte> DataToSign { get; init; }
        
        /// <summary>Optional COSE_Sign_Args for advanced signing.</summary>
        public CoseSignArgs? SignArgs { get; init; }
    }
}

/// <summary>
/// Flags controlling signing key behavior.
/// </summary>
[Flags]
public enum PreviewSignFlags : byte
{
    /// <summary>Unattended signing (no UP/UV required).</summary>
    None = 0b000,
    
    /// <summary>Require user presence for signing.</summary>
    RequireUserPresence = 0b001,
    
    /// <summary>Require user verification for signing.</summary>
    RequireUserVerification = 0b101  // Note: UV implies UP
}
```

#### 2. PreviewSignOutput (WebAuthn Client Output)

```csharp
/// <summary>
/// Output from previewSign extension (WebAuthn layer).
/// </summary>
public abstract record PreviewSignOutput
{
    /// <summary>
    /// Registration output: Generated signing key information.
    /// </summary>
    public sealed record GeneratedKey : PreviewSignOutput
    {
        /// <summary>Selected algorithm (COSE algorithm ID).</summary>
        public required int Algorithm { get; init; }
        
        /// <summary>UP/UV policy for this signing key.</summary>
        public required PreviewSignFlags Flags { get; init; }
        
        /// <summary>Key handle for future signing operations.</summary>
        public required SigningKeyHandle KeyHandle { get; init; }
        
        /// <summary>Signing public key (COSE_Key format).</summary>
        public required ReadOnlyMemory<byte> PublicKey { get; init; }
        
        /// <summary>Attestation object for signing key (if available).</summary>
        public ReadOnlyMemory<byte>? AttestationObject { get; init; }
    }
    
    /// <summary>
    /// Authentication output: Signature over data.
    /// </summary>
    public sealed record Signature : PreviewSignOutput
    {
        /// <summary>Signature bytes (format depends on algorithm).</summary>
        public required ReadOnlyMemory<byte> SignatureBytes { get; init; }
        
        /// <summary>Algorithm used (COSE algorithm ID).</summary>
        public required int Algorithm { get; init; }
    }
}
```

#### 3. SigningKeyHandle (Opaque Identifier)

```csharp
/// <summary>
/// Opaque handle identifying a signing key.
/// </summary>
/// <remarks>
/// Encoded by authenticator, decoded by authenticator. Client treats as opaque bytes.
/// </remarks>
public sealed record SigningKeyHandle
{
    /// <summary>Raw key handle bytes.</summary>
    public required ReadOnlyMemory<byte> RawHandle { get; init; }
    
    /// <summary>Creates handle from raw bytes.</summary>
    public static SigningKeyHandle FromBytes(ReadOnlyMemory<byte> bytes) =>
        new() { RawHandle = bytes };
    
    /// <summary>Creates handle from base64url string.</summary>
    public static SigningKeyHandle FromBase64Url(string base64Url) =>
        new() { RawHandle = Convert.FromBase64String(base64Url) };
    
    /// <summary>Encodes handle to base64url (for JSON transport).</summary>
    public string ToBase64Url() => Convert.ToBase64String(RawHandle.Span);
}
```

#### 4. CBOR Encoding Structures (CTAP Layer)

```csharp
/// <summary>
/// CTAP encoding for previewSign extension input.
/// </summary>
internal static class PreviewSignCtapEncoder
{
    /// <summary>
    /// Encodes GenerateKey input to CTAP CBOR.
    /// </summary>
    /// <remarks>
    /// Format: { alg: [+int], ?flags: uint }
    /// </remarks>
    public static void EncodeGenerateKey(
        CborWriter writer,
        IReadOnlyList<int> algorithms,
        PreviewSignFlags flags)
    {
        var mapSize = flags == PreviewSignFlags.None ? 1 : 2;
        writer.WriteStartMap(mapSize);
        
        // alg: array of COSE algorithm IDs
        writer.WriteTextString("alg");
        writer.WriteStartArray(algorithms.Count);
        foreach (var alg in algorithms)
        {
            writer.WriteInt32(alg);
        }
        writer.WriteEndArray();
        
        // flags: optional (omit if None)
        if (flags != PreviewSignFlags.None)
        {
            writer.WriteTextString("flags");
            writer.WriteUInt32((uint)flags);
        }
        
        writer.WriteEndMap();
    }
    
    /// <summary>
    /// Encodes SignByCredential input to CTAP CBOR.
    /// </summary>
    /// <remarks>
    /// Format: { kh: bstr, tbs: bstr, ?args: bstr .cbor COSE_Sign_Args }
    /// </remarks>
    public static void EncodeSignByCredential(
        CborWriter writer,
        SigningKeyHandle keyHandle,
        ReadOnlyMemory<byte> dataToSign,
        CoseSignArgs? args)
    {
        var mapSize = args is null ? 2 : 3;
        writer.WriteStartMap(mapSize);
        
        // kh: key handle
        writer.WriteTextString("kh");
        writer.WriteByteString(keyHandle.RawHandle.Span);
        
        // tbs: to-be-signed data
        writer.WriteTextString("tbs");
        writer.WriteByteString(dataToSign.Span);
        
        // args: optional COSE_Sign_Args
        if (args is not null)
        {
            writer.WriteTextString("args");
            args.Encode(writer);
        }
        
        writer.WriteEndMap();
    }
}

/// <summary>
/// CTAP decoding for previewSign extension output.
/// </summary>
internal static class PreviewSignCtapDecoder
{
    /// <summary>
    /// Decodes GeneratedKey output from CTAP CBOR.
    /// </summary>
    /// <remarks>
    /// Signed extension format: { alg: int, flags: uint }
    /// Unsigned extension format: { att-obj: bstr }
    /// </remarks>
    public static PreviewSignOutput.GeneratedKey DecodeGeneratedKey(
        ReadOnlyMemory<byte> signedData,
        ReadOnlyMemory<byte>? unsignedData)
    {
        var reader = new CborReader(signedData, CborConformanceMode.Lax);
        reader.ReadStartMap();
        
        int? alg = null;
        PreviewSignFlags? flags = null;
        
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadTextString();
            switch (key)
            {
                case "alg":
                    alg = reader.ReadInt32();
                    break;
                case "flags":
                    flags = (PreviewSignFlags)reader.ReadUInt32();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        reader.ReadEndMap();
        
        if (alg is null || flags is null)
            throw new InvalidOperationException("Missing required fields in previewSign output");
        
        // Parse unsigned extension data for attestation object
        ReadOnlyMemory<byte>? attObj = null;
        if (unsignedData.HasValue)
        {
            var unsignedReader = new CborReader(unsignedData.Value, CborConformanceMode.Lax);
            unsignedReader.ReadStartMap();
            
            while (unsignedReader.PeekState() != CborReaderState.EndMap)
            {
                var key = unsignedReader.ReadTextString();
                if (key == "att-obj")
                {
                    attObj = unsignedReader.ReadByteString();
                }
                else
                {
                    unsignedReader.SkipValue();
                }
            }
            unsignedReader.ReadEndMap();
        }
        
        // Extract key handle from attestation object
        // (Parse attestation → authData → attestedCredentialData → credentialId)
        var keyHandle = ExtractKeyHandleFromAttestation(attObj!.Value);
        var publicKey = ExtractPublicKeyFromAttestation(attObj!.Value);
        
        return new PreviewSignOutput.GeneratedKey
        {
            Algorithm = alg.Value,
            Flags = flags.Value,
            KeyHandle = SigningKeyHandle.FromBytes(keyHandle),
            PublicKey = publicKey,
            AttestationObject = attObj
        };
    }
    
    /// <summary>
    /// Decodes Signature output from CTAP CBOR.
    /// </summary>
    /// <remarks>
    /// Format: { sig: bstr }
    /// </remarks>
    public static PreviewSignOutput.Signature DecodeSignature(
        ReadOnlyMemory<byte> data,
        int algorithm)
    {
        var reader = new CborReader(data, CborConformanceMode.Lax);
        reader.ReadStartMap();
        
        byte[]? sig = null;
        
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadTextString();
            if (key == "sig")
            {
                sig = reader.ReadByteString();
            }
            else
            {
                reader.SkipValue();
            }
        }
        reader.ReadEndMap();
        
        if (sig is null)
            throw new InvalidOperationException("Missing signature in previewSign output");
        
        return new PreviewSignOutput.Signature
        {
            SignatureBytes = sig,
            Algorithm = algorithm
        };
    }
    
    private static ReadOnlyMemory<byte> ExtractKeyHandleFromAttestation(
        ReadOnlyMemory<byte> attestationObject)
    {
        // Parse CBOR attestation object
        var reader = new CborReader(attestationObject, CborConformanceMode.Lax);
        var response = MakeCredentialResponse.Decode(reader);
        
        return response.GetCredentialId();
    }
    
    private static ReadOnlyMemory<byte> ExtractPublicKeyFromAttestation(
        ReadOnlyMemory<byte> attestationObject)
    {
        var reader = new CborReader(attestationObject, CborConformanceMode.Lax);
        var response = MakeCredentialResponse.Decode(reader);
        
        return response.GetCredentialPublicKey();
    }
}
```

#### 5. COSE_Sign_Args (Advanced Signing)

```csharp
/// <summary>
/// COSE_Sign_Args for advanced signing scenarios.
/// </summary>
/// <remarks>
/// See COSE RFC 8152 for structure.
/// </remarks>
public sealed class CoseSignArgs
{
    /// <summary>Protected headers (CBOR-encoded).</summary>
    public ReadOnlyMemory<byte>? Protected { get; init; }
    
    /// <summary>Unprotected headers (CBOR map).</summary>
    public IReadOnlyDictionary<int, object?>? Unprotected { get; init; }
    
    /// <summary>Encodes to CBOR.</summary>
    internal void Encode(CborWriter writer)
    {
        writer.WriteStartArray(2);
        
        // Protected headers (empty if null)
        if (Protected.HasValue)
        {
            writer.WriteByteString(Protected.Value.Span);
        }
        else
        {
            writer.WriteByteString(ReadOnlySpan<byte>.Empty);
        }
        
        // Unprotected headers (empty map if null)
        if (Unprotected is not null)
        {
            writer.WriteStartMap(Unprotected.Count);
            foreach (var (key, value) in Unprotected)
            {
                writer.WriteInt32(key);
                WriteCborValue(writer, value);
            }
            writer.WriteEndMap();
        }
        else
        {
            writer.WriteStartMap(0);
            writer.WriteEndMap();
        }
        
        writer.WriteEndArray();
    }
    
    private static void WriteCborValue(CborWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNull();
                break;
            case int i:
                writer.WriteInt32(i);
                break;
            case byte[] b:
                writer.WriteByteString(b);
                break;
            case string s:
                writer.WriteTextString(s);
                break;
            default:
                throw new NotSupportedException($"Unsupported CBOR value type: {value.GetType()}");
        }
    }
}
```

---

### Integration with ExtensionBuilder

**Modify existing ExtensionBuilder** to add previewSign support:

```csharp
// In src/Fido2/src/Extensions/ExtensionBuilder.cs

public sealed class ExtensionBuilder
{
    // ... existing fields ...
    
    private PreviewSignInput? _previewSign;
    
    /// <summary>
    /// Adds previewSign extension for credential creation (generate signing key).
    /// </summary>
    public ExtensionBuilder WithPreviewSign(
        IReadOnlyList<int> algorithms,
        PreviewSignFlags flags = PreviewSignFlags.None)
    {
        _previewSign = new PreviewSignInput.GenerateKey
        {
            Algorithms = algorithms,
            Flags = flags
        };
        return this;
    }
    
    /// <summary>
    /// Adds previewSign extension for assertion (sign arbitrary data).
    /// </summary>
    public ExtensionBuilder WithPreviewSign(
        SigningKeyHandle keyHandle,
        ReadOnlyMemory<byte> dataToSign,
        CoseSignArgs? args = null)
    {
        _previewSign = new PreviewSignInput.SignByCredential
        {
            KeyHandle = keyHandle,
            DataToSign = dataToSign,
            SignArgs = args
        };
        return this;
    }
    
    // In Encode() method:
    public void Encode(CborWriter writer)
    {
        // ... existing extension encoding ...
        
        if (_previewSign is not null)
        {
            writer.WriteTextString(ExtensionIdentifiers.PreviewSign);
            
            switch (_previewSign)
            {
                case PreviewSignInput.GenerateKey gen:
                    PreviewSignCtapEncoder.EncodeGenerateKey(writer, gen.Algorithms, gen.Flags);
                    break;
                case PreviewSignInput.SignByCredential sign:
                    PreviewSignCtapEncoder.EncodeSignByCredential(
                        writer, sign.KeyHandle, sign.DataToSign, sign.SignArgs);
                    break;
            }
        }
        
        writer.WriteEndMap();
    }
}
```

**Modify ExtensionOutput** to parse previewSign:

```csharp
// In src/Fido2/src/Extensions/ExtensionOutput.cs

public sealed class ExtensionOutput
{
    // ... existing methods ...
    
    /// <summary>
    /// Attempts to get previewSign extension output.
    /// </summary>
    /// <param name="output">The parsed previewSign output.</param>
    /// <param name="unsignedExtensions">Unsigned extension data (for attestation object).</param>
    /// <returns>True if previewSign extension was present.</returns>
    public bool TryGetPreviewSign(
        out PreviewSignOutput? output,
        ReadOnlyMemory<byte>? unsignedExtensions = null)
    {
        output = null;
        
        if (!_extensions.TryGetValue(ExtensionIdentifiers.PreviewSign, out var data))
            return false;
        
        // Determine if this is GenerateKey or SignByCredential based on structure
        var reader = new CborReader(data, CborConformanceMode.Lax);
        reader.ReadStartMap();
        
        var firstKey = reader.PeekState() == CborReaderState.TextString
            ? reader.ReadTextString()
            : null;
        
        if (firstKey == "alg")
        {
            // GenerateKey output
            output = PreviewSignCtapDecoder.DecodeGeneratedKey(data, unsignedExtensions);
        }
        else if (firstKey == "sig")
        {
            // Signature output (need algorithm from context)
            // Note: Algorithm must be tracked separately
            throw new NotImplementedException("Signature decoding requires algorithm context");
        }
        
        return output is not null;
    }
}
```

**Add constant**:

```csharp
// In src/Fido2/src/Extensions/ExtensionIdentifiers.cs

public static class ExtensionIdentifiers
{
    // ... existing constants ...
    
    /// <summary>PreviewSign extension (CTAP v4 draft).</summary>
    public const string PreviewSign = "previewSign";
}
```

---

### WebAuthn Extension Processor

```csharp
// In src/Fido2/src/WebAuthn/Extensions/PreviewSignExtensionProcessor.cs

namespace Yubico.YubiKit.Fido2.WebAuthn.Extensions;

/// <summary>
/// Processes previewSign extension bidirectionally.
/// </summary>
public sealed class PreviewSignExtensionProcessor : IWebAuthnExtensionProcessor
{
    public string WebAuthnIdentifier => "previewSign";
    
    public void ProcessClientInput(
        object? clientInput,
        ExtensionBuilder ctapBuilder,
        WebAuthnContext context)
    {
        if (clientInput is not PreviewSignInput input)
            throw new ArgumentException("Invalid previewSign input type");
        
        switch (input)
        {
            case PreviewSignInput.GenerateKey gen:
                ctapBuilder.WithPreviewSign(gen.Algorithms, gen.Flags);
                break;
            
            case PreviewSignInput.SignByCredential sign:
                ctapBuilder.WithPreviewSign(sign.KeyHandle, sign.DataToSign, sign.SignArgs);
                break;
        }
    }
    
    public object? ProcessAuthenticatorOutput(
        ExtensionOutput ctapOutput,
        WebAuthnContext context)
    {
        // Note: Unsigned extensions come from attestation object
        // This is available in MakeCredentialResponse but not in context
        // Design decision: Pass unsignedExtensions via context or retrieve from response?
        
        if (ctapOutput.TryGetPreviewSign(out var output, context.UnsignedExtensions))
        {
            return output;
        }
        
        return null;
    }
}
```

**Context Update** (add unsigned extensions support):

```csharp
// In src/Fido2/src/WebAuthn/Extensions/WebAuthnContext.cs

public sealed class WebAuthnContext
{
    public required IFidoSession Session { get; init; }
    public required string RpId { get; init; }
    public required ReadOnlyMemory<byte> ClientDataHash { get; init; }
    public IPinUvAuthProtocol? PinUvAuthProtocol { get; init; }
    public ReadOnlyMemory<byte>? PinToken { get; init; }
    
    /// <summary>Unsigned extensions from attestation object (for previewSign).</summary>
    public ReadOnlyMemory<byte>? UnsignedExtensions { get; init; }
}
```

---

### UP/UV Policy Enforcement

**Question**: Who enforces UP/UV policy for signing operations?

**Answer**: **Authenticator enforces** (YubiKey firmware), not SDK.

**Rationale**:
- previewSign flags are **immutable** after key creation
- Authenticator returns error if policy not satisfied
- SDK **validates errors** but does not enforce policy

**Error Mapping**:

```csharp
// In src/Fido2/src/WebAuthn/PreviewSign/PreviewSignException.cs

public sealed class PreviewSignException : WebAuthnException
{
    public PreviewSignErrorCode Code { get; }
    
    public PreviewSignException(
        string message,
        PreviewSignErrorCode code,
        Exception? inner = null)
        : base(message, WebAuthnErrorCode.ExtensionError, inner)
    {
        Code = code;
    }
    
    internal static PreviewSignException FromCtapStatus(CtapStatus status) => status switch
    {
        CtapStatus.UnsupportedAlgorithm =>
            new PreviewSignException(
                "Requested signing algorithm not supported",
                PreviewSignErrorCode.UnsupportedAlgorithm),
        
        CtapStatus.InvalidOption =>
            new PreviewSignException(
                "Invalid previewSign flags or parameters",
                PreviewSignErrorCode.InvalidOption),
        
        CtapStatus.NoCredentials =>
            new PreviewSignException(
                "Signing key handle not found",
                PreviewSignErrorCode.InvalidCredential),
        
        CtapStatus.MissingParameter =>
            new PreviewSignException(
                "Required previewSign parameter missing",
                PreviewSignErrorCode.MissingParameter),
        
        CtapStatus.PinNotSet when /* UP required */ =>
            new PreviewSignException(
                "User presence required but not provided",
                PreviewSignErrorCode.UserPresenceRequired),
        
        CtapStatus.PinAuthInvalid when /* UV required */ =>
            new PreviewSignException(
                "User verification required but not provided",
                PreviewSignErrorCode.UserVerificationRequired),
        
        _ => new PreviewSignException(
            $"Unexpected CTAP error: {status}",
            PreviewSignErrorCode.Unknown)
    };
}

public enum PreviewSignErrorCode
{
    Unknown,
    UnsupportedAlgorithm,
    InvalidOption,
    InvalidCredential,
    MissingParameter,
    UserPresenceRequired,
    UserVerificationRequired
}
```

---

### Key Handle Format

**Design Decision**: Treat key handle as **opaque bytes** (no SDK parsing).

**Rationale**:
- Authenticator-specific encoding (implementation detail)
- YubiKey may use encrypted handles, other authenticators may differ
- SDK should not depend on internal format

**Storage Recommendations** (documentation):

```csharp
/// <summary>
/// Storing signing key handles for later use.
/// </summary>
/// <example>
/// <code>
/// // After registration
/// var createResult = await client.CreateCredentialAsync(options, origin);
/// 
/// if (createResult.ClientExtensionResults?["previewSign"] is PreviewSignOutput.GeneratedKey signingKey)
/// {
///     // Store key handle alongside credential ID
///     await database.StoreSigningKeyAsync(
///         credentialId: createResult.RawId,
///         signingKeyHandle: signingKey.KeyHandle.ToBase64Url(),
///         algorithm: signingKey.Algorithm,
///         publicKey: signingKey.PublicKey);
/// }
/// 
/// // Later, for signing
/// var keyHandle = SigningKeyHandle.FromBase64Url(storedHandle);
/// var signInput = new PreviewSignInput.SignByCredential
/// {
///     KeyHandle = keyHandle,
///     DataToSign = dataToSign
/// };
/// 
/// var assertionOptions = new PublicKeyCredentialRequestOptions
/// {
///     Challenge = challenge,
///     RpId = rpId,
///     Extensions = new Dictionary&lt;string, object?&gt;
///     {
///         ["previewSign"] = signInput
///     }
/// };
/// 
/// var result = await client.GetAssertionAsync(assertionOptions, origin);
/// var signature = result.ClientExtensionResults?["previewSign"] as PreviewSignOutput.Signature;
/// </code>
/// </example>
```

---

## Test Strategy

### Unit Tests

```csharp
[Fact]
public void PreviewSignCtapEncoder_EncodeGenerateKey_ProducesCorrectCbor()
{
    var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
    PreviewSignCtapEncoder.EncodeGenerateKey(
        writer,
        algorithms: [-7, -8],  // ES256, EdDSA
        flags: PreviewSignFlags.RequireUserPresence);
    
    var cbor = writer.Encode();
    
    // Verify structure: { alg: [-7, -8], flags: 1 }
    var reader = new CborReader(cbor, CborConformanceMode.Lax);
    reader.ReadStartMap();
    
    Assert.Equal("alg", reader.ReadTextString());
    Assert.Equal(2, reader.ReadStartArray());
    Assert.Equal(-7, reader.ReadInt32());
    Assert.Equal(-8, reader.ReadInt32());
    reader.ReadEndArray();
    
    Assert.Equal("flags", reader.ReadTextString());
    Assert.Equal(1u, reader.ReadUInt32());
    
    reader.ReadEndMap();
}

[Fact]
public void PreviewSignCtapEncoder_EncodeSignByCredential_IncludesKeyHandle()
{
    var keyHandle = SigningKeyHandle.FromBytes(new byte[] { 1, 2, 3, 4 });
    var dataToSign = new byte[] { 5, 6, 7, 8 };
    
    var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
    PreviewSignCtapEncoder.EncodeSignByCredential(
        writer, keyHandle, dataToSign, args: null);
    
    var cbor = writer.Encode();
    var reader = new CborReader(cbor, CborConformanceMode.Lax);
    
    reader.ReadStartMap();
    
    Assert.Equal("kh", reader.ReadTextString());
    Assert.Equal([1, 2, 3, 4], reader.ReadByteString());
    
    Assert.Equal("tbs", reader.ReadTextString());
    Assert.Equal([5, 6, 7, 8], reader.ReadByteString());
    
    reader.ReadEndMap();
}

[Fact]
public void PreviewSignFlags_RequireUserVerification_ImpliesUserPresence()
{
    var flags = PreviewSignFlags.RequireUserVerification;
    
    // 0b101 = UV flag
    Assert.Equal(0b101, (byte)flags);
    
    // Verify bit 0 (UP) is set
    Assert.True((flags & PreviewSignFlags.RequireUserPresence) != 0);
}
```

### Integration Tests (Mock Authenticator)

```csharp
[Fact]
public async Task PreviewSign_GenerateKey_ReturnsSigningKey()
{
    // Mock IFidoSession to return previewSign extension output
    var session = Substitute.For<IFidoSession>();
    
    // Build mock response with previewSign extension
    var mockAttestationObject = BuildMockAttestationWithPreviewSign(
        algorithm: -7,  // ES256
        flags: PreviewSignFlags.RequireUserPresence,
        keyHandle: [1, 2, 3, 4],
        publicKey: mockPublicKeyBytes);
    
    session.MakeCredentialAsync(
        Arg.Any<ReadOnlyMemory<byte>>(),
        Arg.Any<PublicKeyCredentialRpEntity>(),
        Arg.Any<PublicKeyCredentialUserEntity>(),
        Arg.Any<IReadOnlyList<PublicKeyCredentialParameters>>(),
        Arg.Any<MakeCredentialOptions>(),
        Arg.Any<CancellationToken>())
        .Returns(mockAttestationObject);
    
    var client = new WebAuthnClient(session);
    
    var options = new PublicKeyCredentialCreationOptions
    {
        // ... standard fields ...
        Extensions = new Dictionary<string, object?>
        {
            ["previewSign"] = new PreviewSignInput.GenerateKey
            {
                Algorithms = [-7],
                Flags = PreviewSignFlags.RequireUserPresence
            }
        }
    };
    
    var result = await client.CreateCredentialAsync(options, "https://example.com");
    
    Assert.NotNull(result.ClientExtensionResults);
    var previewSign = result.ClientExtensionResults["previewSign"] as PreviewSignOutput.GeneratedKey;
    
    Assert.NotNull(previewSign);
    Assert.Equal(-7, previewSign.Algorithm);
    Assert.Equal(PreviewSignFlags.RequireUserPresence, previewSign.Flags);
    Assert.NotEmpty(previewSign.KeyHandle.RawHandle);
    Assert.NotEmpty(previewSign.PublicKey);
}

[Fact]
public async Task PreviewSign_SignByCredential_ReturnsSignature()
{
    var session = Substitute.For<IFidoSession>();
    
    // Mock GetAssertion with previewSign signature output
    var mockSignatureOutput = BuildMockSignatureOutput(
        signature: mockSignatureBytes);
    
    session.GetAssertionAsync(/* ... */)
        .Returns(mockSignatureOutput);
    
    var client = new WebAuthnClient(session);
    
    var keyHandle = SigningKeyHandle.FromBytes([1, 2, 3, 4]);
    var dataToSign = "Hello, world!"u8.ToArray();
    
    var options = new PublicKeyCredentialRequestOptions
    {
        Challenge = RandomNumberGenerator.GetBytes(32),
        RpId = "example.com",
        Extensions = new Dictionary<string, object?>
        {
            ["previewSign"] = new PreviewSignInput.SignByCredential
            {
                KeyHandle = keyHandle,
                DataToSign = dataToSign
            }
        }
    };
    
    var result = await client.GetAssertionAsync(options, "https://example.com");
    
    var signature = result.ClientExtensionResults?["previewSign"] as PreviewSignOutput.Signature;
    
    Assert.NotNull(signature);
    Assert.NotEmpty(signature.SignatureBytes);
}
```

### Integration Tests (Real YubiKey) - **IF SUPPORTED**

**NOTE**: previewSign is CTAP v4 **draft**. As of 2026-04-22, no production YubiKey firmware supports it.

**Test strategy when firmware becomes available**:

```csharp
[Fact]
[Trait("RequiresYubiKeyFirmware", "6.0+")] // Hypothetical
public async Task PreviewSign_EndToEnd_RealYubiKey()
{
    await using var session = await _yubiKey.CreateFidoSessionAsync();
    var client = new WebAuthnClient(session);
    
    // 1. Create credential with signing key
    var createOptions = new PublicKeyCredentialCreationOptions
    {
        Rp = new PublicKeyCredentialRpEntity { Id = "example.com", Name = "Example" },
        User = new PublicKeyCredentialUserEntity
        {
            Id = RandomNumberGenerator.GetBytes(16),
            Name = "user@example.com",
            DisplayName = "Test User"
        },
        Challenge = RandomNumberGenerator.GetBytes(32),
        PubKeyCredParams =
        [
            new PublicKeyCredentialParameters { Type = "public-key", Alg = -7 } // ES256
        ],
        Extensions = new Dictionary<string, object?>
        {
            ["previewSign"] = new PreviewSignInput.GenerateKey
            {
                Algorithms = [-7],  // ES256
                Flags = PreviewSignFlags.RequireUserPresence
            }
        }
    };
    
    var createResult = await client.CreateCredentialAsync(
        createOptions,
        origin: "https://example.com",
        timeout: TimeSpan.FromSeconds(30));
    
    var signingKey = createResult.ClientExtensionResults?["previewSign"] as PreviewSignOutput.GeneratedKey;
    Assert.NotNull(signingKey);
    
    // 2. Sign arbitrary data
    var dataToSign = "Important document content"u8.ToArray();
    
    var signOptions = new PublicKeyCredentialRequestOptions
    {
        Challenge = RandomNumberGenerator.GetBytes(32),
        RpId = "example.com",
        AllowCredentials =
        [
            new PublicKeyCredentialDescriptor
            {
                Type = "public-key",
                Id = createResult.RawId
            }
        ],
        Extensions = new Dictionary<string, object?>
        {
            ["previewSign"] = new PreviewSignInput.SignByCredential
            {
                KeyHandle = signingKey.KeyHandle,
                DataToSign = dataToSign
            }
        }
    };
    
    var signResult = await client.GetAssertionAsync(
        signOptions,
        origin: "https://example.com",
        timeout: TimeSpan.FromSeconds(30));
    
    var signature = signResult.ClientExtensionResults?["previewSign"] as PreviewSignOutput.Signature;
    Assert.NotNull(signature);
    
    // 3. Verify signature using public key
    using var ecdsa = ECDsa.Create();
    ecdsa.ImportSubjectPublicKeyInfo(signingKey.PublicKey.Span, out _);
    
    var isValid = ecdsa.VerifyData(
        dataToSign,
        signature.SignatureBytes.Span,
        HashAlgorithmName.SHA256);
    
    Assert.True(isValid);
}
```

---

## Dependencies on Plan 1

**Critical Dependency**: previewSign implementation **requires** Plan 1 (WebAuthn Client) to be complete.

**Dependency Chain**:

1. Plan 1 implements `IWebAuthnExtensionProcessor` interface
2. Plan 1 implements `WebAuthnContext` for extension processing
3. Plan 1 implements extension result mapping (CTAP → WebAuthn)
4. previewSign extends `ExtensionBuilder` (Plan 1 provides base)
5. previewSign uses `WebAuthnClient.CreateCredentialAsync` and `GetAssertionAsync`

**Implementation Order**:

1. ✅ Implement Plan 1 WebAuthn Client (core + extension system)
2. ✅ Add previewSign CBOR encoding/decoding (standalone, testable)
3. ✅ Extend `ExtensionBuilder` with previewSign methods
4. ✅ Implement `PreviewSignExtensionProcessor`
5. ✅ Write unit tests (mock CTAP responses)
6. ⏳ Integration tests with real YubiKey (when firmware supports CTAP v4)

**Can previewSign be implemented standalone?** No. It requires WebAuthn extension processor infrastructure from Plan 1.

---

## Critical Files to Create/Modify

### New Files

| Path | Purpose | Lines (est.) |
|------|---------|--------------|
| `src/Fido2/src/WebAuthn/PreviewSign/PreviewSignInput.cs` | Input types (GenerateKey, SignByCredential) | ~80 |
| `src/Fido2/src/WebAuthn/PreviewSign/PreviewSignOutput.cs` | Output types (GeneratedKey, Signature) | ~70 |
| `src/Fido2/src/WebAuthn/PreviewSign/SigningKeyHandle.cs` | Key handle wrapper | ~40 |
| `src/Fido2/src/WebAuthn/PreviewSign/PreviewSignFlags.cs` | Flags enum | ~30 |
| `src/Fido2/src/WebAuthn/PreviewSign/CoseSignArgs.cs` | COSE_Sign_Args | ~60 |
| `src/Fido2/src/WebAuthn/PreviewSign/PreviewSignCtapEncoder.cs` | CBOR encoding (internal) | ~120 |
| `src/Fido2/src/WebAuthn/PreviewSign/PreviewSignCtapDecoder.cs` | CBOR decoding (internal) | ~150 |
| `src/Fido2/src/WebAuthn/PreviewSign/PreviewSignException.cs` | Exception + error mapping | ~80 |
| `src/Fido2/src/WebAuthn/Extensions/PreviewSignExtensionProcessor.cs` | Extension processor | ~100 |

**Total new code**: ~730 LOC

### Modified Files

| Path | Changes | Lines (est.) |
|------|---------|--------------|
| `src/Fido2/src/Extensions/ExtensionBuilder.cs` | Add `WithPreviewSign` overloads + encoding | +60 |
| `src/Fido2/src/Extensions/ExtensionOutput.cs` | Add `TryGetPreviewSign` method | +40 |
| `src/Fido2/src/Extensions/ExtensionIdentifiers.cs` | Add `PreviewSign` constant | +3 |
| `src/Fido2/src/WebAuthn/Extensions/WebAuthnContext.cs` | Add `UnsignedExtensions` property | +5 |

**Total modifications**: ~108 LOC

---

## Open Questions for Dennis

1. **Unsigned Extensions Handling**: The previewSign spec uses both signed (in authData) and unsigned (separate attestation object) extension outputs. How should we pass unsigned data to extension processors? Current design adds `UnsignedExtensions` to `WebAuthnContext`, but this requires WebAuthnClient to extract it from `MakeCredentialResponse`. Is this acceptable?

2. **Algorithm Tracking for Signature Output**: When decoding previewSign signature output, we need the algorithm to construct the result, but it's not in the CBOR output (only `sig`). Should we:
   - Track algorithm in `WebAuthnContext` (requires stateful processor)
   - Require caller to pass algorithm when parsing signature
   - Store algorithm in key handle (opaque to SDK, but could be documented pattern)

3. **Error Code Mapping**: previewSign spec defines error codes (`UNSUPPORTED_ALGORITHM`, `INVALID_CREDENTIAL`, etc.), but CTAP2 uses numeric status codes. Should we create previewSign-specific exception types or reuse `CtapException`? Current design uses `PreviewSignException` wrapping `CtapStatus`.

4. **Key Handle Storage Guidance**: Should SDK provide helper classes for storing signing keys (e.g., `SigningKeyRepository` interface), or just document the pattern? Current design documents only.

5. **COSE_Sign_Args Support**: The spec mentions `COSE_Sign_Args` for advanced signing, but provides minimal detail. Should we implement a full COSE signing API, or just pass through opaque bytes? Current design provides basic structure but not full COSE validation.

6. **YubiKey Firmware Support Timeline**: Do we have visibility into when YubiKey firmware will support previewSign? This affects testing strategy (can we use real hardware or only mocks?).

7. **Attestation Object Parsing**: previewSign returns signing key's attestation object in unsigned extension data. Should we parse this fully (into `MakeCredentialResponse`) or just extract key handle + public key? Current design parses fully for consistency.

8. **Extension Naming**: Spec uses "previewSign" (camelCase). Should SDK use same casing or follow .NET conventions (PascalCase for public APIs)? Current design uses `previewSign` for wire format, `PreviewSign*` for C# types.

---

## Summary

### Plan 1: WebAuthn Client

- **Scope**: Full WebAuthn client layer (CollectedClientData, challenge mgmt, timeout, attestation verification, extension mapping)
- **Files**: ~2,100 new LOC across 20 files
- **Dependencies**: None (builds on existing FIDO2 module)
- **Testing**: 90%+ unit test coverage, full integration test suite
- **Timeline**: 2-3 weeks for Engineer (implementation + tests)

### Plan 2: previewSign Extension

- **Scope**: CTAP v4 previewSign extension (signing key generation, arbitrary data signing)
- **Files**: ~730 new LOC + ~108 modified LOC
- **Dependencies**: Plan 1 must be complete (extension processor system)
- **Testing**: Unit tests + mock integration (real hardware TBD based on firmware support)
- **Timeline**: 1 week for Engineer (assuming Plan 1 complete)

### Critical Path

1. Implement Plan 1 (WebAuthn Client core)
2. Implement Plan 1 extension system
3. Implement Plan 2 previewSign (depends on step 2)
4. Integration testing (when YubiKey firmware supports CTAP v4)

### Risk Mitigation

- **Spec volatility**: previewSign is draft spec → may change. Design uses abstraction layers (CTAP encoder/decoder separate from public API).
- **Firmware availability**: Real hardware testing blocked on YubiKey firmware → comprehensive mock testing required.
- **Complexity**: Extension processor pattern is new → prototype with simpler extension (credProtect) first to validate design.

---

## Appendix: CBOR Wire Format Examples

### previewSign Registration (GenerateKey)

**Input** (CTAP extension):
```cbor
{
  "previewSign": {
    "alg": [-7, -8],  // ES256, EdDSA
    "flags": 1        // Require UP
  }
}
```

**Output** (signed extension in authData):
```cbor
{
  "previewSign": {
    "alg": -7,   // Selected algorithm
    "flags": 1   // Confirmed policy
  }
}
```

**Output** (unsigned extension, separate):
```cbor
{
  "previewSign": {
    "att-obj": h'...' // Full attestation object for signing key
  }
}
```

### previewSign Authentication (SignByCredential)

**Input** (CTAP extension):
```cbor
{
  "previewSign": {
    "kh": h'01020304',              // Key handle
    "tbs": h'48656c6c6f',           // "Hello" in hex
    "args": [h'', {}]               // Optional COSE_Sign_Args
  }
}
```

**Output** (signed extension in authData):
```cbor
{
  "previewSign": {
    "sig": h'3046022100...'  // Signature bytes
  }
}
```

---

**End of Design Document**

Dennis, these are complete architectural specifications ready for implementation. Key clarifications needed are marked in "Open Questions" sections. The designs are defensive against spec changes and firmware availability constraints, with clear abstraction boundaries.

Let me know which parts need deeper detail or adjustment based on your product strategy.
