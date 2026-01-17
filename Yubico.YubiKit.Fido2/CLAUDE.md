# CLAUDE.md - FIDO2 Module

This file provides Claude-specific guidance for working with the FIDO2 module. **Read the root [CLAUDE.md](../CLAUDE.md) first** for general repository patterns.

## Documentation Maintenance

> **Important:** This documentation is subject to change. When working on this module:
> - **Notable changes** to APIs, patterns, or behavior should be documented here
> - **New features** (e.g., new CTAP commands, extensions) should include usage examples
> - **Breaking changes** require updates with migration guidance

## Module Context

The FIDO2 module implements CTAP 2.1/2.3 (Client to Authenticator Protocol) for YubiKey authenticators. This module supports passkey creation, authentication, and advanced FIDO2 features like credential management, biometric enrollment, and WebAuthn extensions.

**Transports Supported:**
- FIDO HID protocol over USB (primary transport)
- SmartCard (CCID) via NFC only (USB CCID is NOT supported for FIDO2)

> **Important:** FIDO2 over USB uses the HID FIDO interface, NOT the CCID/SmartCard interface. 
> The SmartCard transport only works over NFC. Attempting to create a FidoSession with a USB CCID 
> connection will throw `NotSupportedException`.

## Architecture Overview

```
Yubico.YubiKit.Fido2/
├── src/
│   ├── FidoSession.cs              # Main session class (entry point)
│   ├── IFidoSession.cs             # Session interface
│   ├── AuthenticatorInfo.cs        # GetInfo response parsing
│   ├── DependencyInjection.cs      # DI registration
│   ├── IYubiKeyExtensions.cs       # Extension methods for IYubiKey
│   ├── Backend/                    # Transport backends
│   │   ├── IFidoBackend.cs
│   │   ├── FidoHidBackend.cs       # HID transport
│   │   └── SmartCardFidoBackend.cs # SmartCard/CCID transport
│   ├── Cbor/                       # CBOR serialization
│   │   ├── CtapRequestBuilder.cs   # Fluent CBOR request builder
│   │   └── CtapResponseParser.cs   # CBOR response parsing utilities
│   ├── Config/                     # authenticatorConfig (FW 5.4+)
│   │   ├── ConfigSubCommand.cs
│   │   └── AuthenticatorConfig.cs
│   ├── CredentialManagement/       # Credential enumeration/management
│   │   ├── CredManagementSubCommand.cs
│   │   ├── CredentialManagement.cs
│   │   └── CredentialManagementModels.cs
│   ├── Credentials/                # MakeCredential/GetAssertion types
│   │   ├── AuthenticatorData.cs
│   │   ├── AttestedCredentialData.cs
│   │   ├── CredentialOptions.cs
│   │   ├── MakeCredentialResponse.cs
│   │   ├── GetAssertionResponse.cs
│   │   └── PublicKeyCredentialTypes.cs
│   ├── Crypto/                     # YK 5.7/5.8 encrypted metadata
│   │   └── EncryptedMetadataDecryptor.cs
│   ├── Ctap/                       # CTAP protocol types
│   │   ├── CtapCommand.cs
│   │   ├── CtapStatus.cs
│   │   └── CtapException.cs
│   ├── Extensions/                 # WebAuthn extensions
│   │   ├── ExtensionIdentifiers.cs
│   │   ├── ExtensionBuilder.cs     # Fluent extension builder
│   │   ├── ExtensionOutput.cs
│   │   ├── CredProtectPolicy.cs
│   │   ├── HmacSecretInput.cs
│   │   ├── CredBlobExtension.cs
│   │   ├── LargeBlobExtension.cs
│   │   ├── MinPinLengthExtension.cs
│   │   └── PrfExtension.cs
│   ├── BioEnrollment/              # Fingerprint enrollment (FW 5.2+)
│   │   ├── BioEnrollmentSubCommand.cs
│   │   ├── BioEnrollmentModels.cs
│   │   └── FingerprintBioEnrollment.cs
│   ├── LargeBlobs/                 # Large blob storage
│   │   ├── LargeBlobData.cs        # Entry/Array types
│   │   └── LargeBlobStorage.cs     # Storage API
│   └── Pin/                        # PIN/UV authentication
│       ├── IPinUvAuthProtocol.cs
│       ├── PinUvAuthProtocolV1.cs
│       ├── PinUvAuthProtocolV2.cs
│       ├── ClientPin.cs
│       ├── ClientPinSubCommand.cs
│       └── PinUvAuthTokenPermissions.cs
└── tests/
    ├── Yubico.YubiKit.Fido2.UnitTests/
    └── Yubico.YubiKit.Fido2.IntegrationTests/
```

## Key Patterns

### Session Creation

```csharp
// Recommended: Use extension method
await using var fidoSession = await yubiKey.CreateFidoSessionAsync();

// Or via DI
var factory = serviceProvider.GetRequiredService<FidoSessionFactoryDelegate>();
await using var fidoSession = await factory(connection, configuration: null);

// Or direct creation
await using var fidoSession = await FidoSession.CreateAsync(connection);
```

### GetInfo (No User Presence Required)

```csharp
// Get authenticator capabilities
var info = await fidoSession.GetInfoAsync();

// Check versions
bool ctap2_1 = info.Versions.Contains("FIDO_2_1");

// Check supported extensions
bool hasHmacSecret = info.Extensions?.Contains("hmac-secret") ?? false;

// Check options
bool hasRk = info.Options?.TryGetValue("rk", out var rk) == true && rk;
```

### PIN/UV Auth Protocols

```csharp
// Protocol V2 (recommended, CTAP 2.1)
using var protocol = new PinUvAuthProtocolV2();
await protocol.InitializeAsync(session);

// Get PIN token with permissions
var clientPin = new ClientPin(session, protocol);
var pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
    pin,
    PinUvAuthTokenPermissions.MakeCredential);

// Authenticate command
var authParam = protocol.Authenticate(pinToken, messageHash);
```

### Extensions (WebAuthn)

The SDK uses the **ExtensionBuilder** fluent pattern for all WebAuthn/CTAP extensions. Extensions are NOT implemented as separate classes that you instantiate—instead, you compose them via the builder.

#### Available Extensions

| Extension | Builder Method | Description | YubiKey FW |
|-----------|---------------|-------------|------------|
| credProtect | `.WithCredProtect()` | Credential protection level | 5.2+ |
| hmac-secret | `.WithHmacSecret()` | CTAP2 symmetric secret derivation | 5.2+ |
| hmac-secret-mc | `.WithHmacSecretMc()` | hmac-secret during MakeCredential | 5.4+ |
| credBlob | `.WithCredBlob()` | Per-credential blob storage (32-64 bytes) | 5.5+ |
| largeBlob | `.WithLargeBlobKey()` | Large blob storage key | 5.5+ |
| minPinLength | `.WithMinPinLength()` | Require minimum PIN length | 5.4+ |
| prf | `.WithPrf()` | WebAuthn PRF extension (wraps hmac-secret) | 5.2+ |

#### Example Usage

```csharp
// Build extensions for MakeCredential
var extensions = ExtensionBuilder.Create()
    .WithCredProtect(CredProtectPolicy.UserVerificationRequired)
    .WithCredBlob(blobData)
    .Build();

var options = new MakeCredentialOptions
{
    Extensions = extensions,
    ResidentKey = true
};

// For GetAssertion with PRF
var prfExtensions = ExtensionBuilder.Create()
    .WithPrf(salt1, salt2)  // WebAuthn PRF extension
    .Build();
```

#### hmac-secret vs PRF Extension

These extensions serve similar purposes but operate at different protocol levels:

| Aspect | hmac-secret (CTAP2) | PRF (WebAuthn) |
|--------|---------------------|----------------|
| Protocol | CTAP2 native | WebAuthn wrapper |
| Salt encoding | Raw bytes | Base64url encoded |
| Output | Raw HMAC bytes | eval/evalByCredential |
| Use case | Direct authenticator access | Browser/WebAuthn API |

**When to use which:**
- Use **hmac-secret** when building CTAP2-level tools or when you need direct authenticator interaction
- Use **PRF** when implementing WebAuthn APIs or browser-compatible flows

```csharp
// CTAP2 hmac-secret (direct)
var hmacExtensions = ExtensionBuilder.Create()
    .WithHmacSecret(salt1, salt2)
    .Build();

// WebAuthn PRF (browser-compatible)
var prfExtensions = ExtensionBuilder.Create()
    .WithPrf(salt1, salt2)
    .Build();
```

Internally, PRF extension serializes using hmac-secret wire format but presents the WebAuthn PRF input/output model.

### Credential Management

```csharp
var credMgmt = new CredentialManagement(session, protocol, pinToken);

// Get credential metadata
var metadata = await credMgmt.GetCredentialsMetadataAsync();

// Enumerate RPs
var rps = await credMgmt.EnumerateRelyingPartiesAsync();

// Enumerate credentials for an RP
var creds = await credMgmt.EnumerateCredentialsAsync(rpIdHash);

// Delete credential
await credMgmt.DeleteCredentialAsync(credentialId);
```

## CBOR Encoding Pattern

Use `CtapRequestBuilder` for all CTAP commands:

```csharp
var request = CtapRequestBuilder.Create(CtapCommand.ClientPin)
    .WithParameter(1, pinUvAuthProtocol)
    .WithParameter(2, subCommand)
    .WithParameter(3, keyAgreement, k => k.Encode(k))
    .WithParameter(4, pinHashEnc)
    .Build();

var response = await session.SendCborRequestAsync(request);
```

Key rules:
- Parameters are **integer keys** (not strings) for CTAP
- Use **Ctap2Canonical** conformance mode (sorted integer keys)
- Build request returns command byte + CBOR payload

## Security Requirements

### Memory Handling

```csharp
// ✅ Zero sensitive data
CryptographicOperations.ZeroMemory(pinBytes);

// ✅ Use ArrayPool for crypto buffers
var buffer = ArrayPool<byte>.Shared.Rent(32);
try
{
    // use buffer
}
finally
{
    CryptographicOperations.ZeroMemory(buffer.AsSpan(0, 32));
    ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
}

// ❌ NEVER log PIN, keys, or sensitive CTAP data
```

### Protocol Disposal

```csharp
using var protocol = new PinUvAuthProtocolV2();
// Protocol disposes ECDH key pair on dispose
```

## Test Patterns

### Unit Tests (No Hardware)

```csharp
[Fact]
public void AuthenticatorInfo_Decode_ParsesVersions()
{
    // Arrange - raw CBOR bytes
    var cborData = new byte[] { ... };
    
    // Act
    var info = AuthenticatorInfo.Decode(cborData);
    
    // Assert
    Assert.Contains("FIDO_2_0", info.Versions);
}
```

### Hardware Integration Tests

Tests requiring user presence must be marked and excluded:

```csharp
// Test that can run without user interaction
[Fact]
public async Task GetInfoAsync_Returns_AAGUID()
{
    await using var session = await device.CreateFidoSessionAsync();
    var info = await session.GetInfoAsync();
    Assert.Equal(16, info.Aaguid.Length);
}

// Test requiring user touch - MUST be excluded from automated runs
[Fact]
[Trait("RequiresUserPresence", "true")]
public async Task MakeCredentialAsync_CreatesPasskey()
{
    // This requires user touch
}
```

Run tests excluding user presence:
```bash
dotnet test --filter "RequiresUserPresence!=true"
```

## Feature Flags

The module defines firmware feature flags:

```csharp
public static readonly Feature FeatureFido2 = new("FIDO2", 5, 0, 0);
public static readonly Feature FeatureBioEnrollment = new("Bio Enrollment", 5, 2, 0);
public static readonly Feature FeatureCredentialManagement = new("Credential Management", 5, 2, 0);
public static readonly Feature FeatureHmacSecretMc = new("hmac-secret-mc", 5, 4, 0);
public static readonly Feature FeatureAuthenticatorConfig = new("Authenticator Config", 5, 4, 0);
public static readonly Feature FeatureCredBlob = new("credBlob", 5, 5, 0);
public static readonly Feature FeatureEncIdentifier = new("Encrypted Identifier", 5, 7, 0);
```

Check firmware support before using features:
```csharp
if (!FidoSession.FeatureBioEnrollment.IsSupported(firmwareVersion))
{
    throw new NotSupportedException("Bio enrollment requires firmware 5.2+");
}
```

## Common Pitfalls

1. **CBOR Key Order**: CTAP2 requires canonical CBOR with sorted integer keys. Always use `CborConformanceMode.Ctap2Canonical`.

2. **PIN Padding**: Client PIN commands require specific padding (64 bytes, PKCS#7 style) before encryption.

3. **Protocol Reuse**: PIN/UV auth protocols maintain state (shared secret). Create once per session.

4. **User Presence**: Many operations (MakeCredential, GetAssertion, Reset) require user touch. Tests must account for this.

5. **Extension Order**: Extensions map keys must be in canonical order when encoding.

## References

- [CTAP 2.1 Specification](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html)
- [WebAuthn Level 2](https://www.w3.org/TR/webauthn-2/)
- [COSE Key Registry](https://www.iana.org/assignments/cose/cose.xhtml)
