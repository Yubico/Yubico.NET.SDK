# Yubico.YubiKit.Piv

> **Note:** This documentation is subject to change as the module evolves. Please check for updates regularly.

This module provides access to the PIV (Personal Identity Verification) application on YubiKeys, enabling smart card operations for authentication, digital signatures, encryption, and certificate management.

## Overview

PIV (Personal Identity Verification) is a US government standard (FIPS 201) for smart cards used for identity verification and access control. The YubiKey implements the PIV standard and provides:

- **Cryptographic Key Management**: Generate, import, and use RSA and EC keys
- **Certificate Operations**: Import, retrieve, and manage X.509 certificates
- **Cryptographic Operations**: Sign, decrypt, and perform key agreement
- **PIN/PUK Management**: Manage PIN (Personal Identification Number) and PUK (PIN Unblocking Key)
- **Authentication**: Management key authentication for administrative operations
- **Attestation**: Generate attestation statements for keys generated on-device

## Requirements

- **YubiKey Models**: YubiKey 4 series, YubiKey 5 series, YubiKey Bio series, Security Key series
- **PIV Application**: Available on all modern YubiKeys
- **Firmware**: Different features available on different firmware versions (see compatibility notes)

## Key Concepts

### Slots

PIV supports 24 slots for storing private keys and certificates:

**Standard PIV Slots:**
- **0x9A** (Authentication): Used for authentication to systems
- **0x9C** (Digital Signature): Used for digital signatures (requires PIN each time)
- **0x9D** (Key Management): Used for encryption/decryption
- **0x9E** (Card Authentication): Used for physical access control (no PIN required)

**Retired Key Slots:** 0x82-0x95 (20 retired key management slots)

**Attestation Slot:** 0xF9 (contains Yubico attestation certificate)

### Authentication Mechanisms

**PIN (Personal Identification Number):**
- Default: `123456`
- Required for cryptographic operations with most keys
- 6-8 digits/characters
- Retry limit: 3 attempts (then blocked)

**PUK (PIN Unblocking Key):**
- Default: `12345678`
- Used to unblock a blocked PIN and set a new PIN
- 6-8 digits/characters
- Retry limit: 3 attempts (then blocked permanently)

**Management Key:**
- Default: `010203040506070801020304050607080102030405060708` (24 bytes, 3DES)
- Required for administrative operations (generate/import keys, import certificates)
- Can be AES-128, AES-192, AES-256, or 3DES

### PIN and Touch Policies

When generating or importing keys, you can specify:

**PIN Policy:**
- `Default`: PIN required once per session
- `Never`: PIN never required (use with caution)
- `Once`: PIN required once per session (even if already verified)
- `Always`: PIN required for every operation

**Touch Policy:**
- `Default`: Touch not required
- `Never`: Touch never required
- `Always`: Physical touch required for every operation
- `Cached`: Touch required, but cached for 15 seconds

## Core API

### Creating a Session

```csharp
using Yubico.YubiKit.Piv;

// Create session from IYubiKey
await using var session = await yubiKey.CreatePivSessionAsync();

// Or directly from SmartCard connection
await using var session = await PivSession.CreateAsync(connection);
```

### Key Generation

```csharp
// Generate RSA 2048 key
var publicKey = await session.GenerateKeyAsync(
    PivSlot.Authentication,
    PivAlgorithm.Rsa2048,
    PivPinPolicy.Default,
    PivTouchPolicy.Default);

// Generate EC P-256 key
var publicKey = await session.GenerateKeyAsync(
    PivSlot.Signature,
    PivAlgorithm.EccP256,
    PivPinPolicy.Always,    // PIN required every time
    PivTouchPolicy.Cached); // Touch required (15s cache)
```

### Certificate Operations

```csharp
// Import certificate
await session.StoreCertificateAsync(PivSlot.Authentication, certificate);

// Retrieve certificate
var cert = await session.GetCertificateAsync(PivSlot.Authentication);

// Delete certificate
await session.DeleteCertificateAsync(PivSlot.Authentication);
```

### Cryptographic Operations

```csharp
// Sign data (or decrypt for RSA)
var hash = SHA256.HashData("data to sign"u8);
var signature = await session.SignOrDecryptAsync(
    PivSlot.Authentication, 
    PivAlgorithm.EccP256,
    hash);

// Key agreement (ECDH)
var peerPublicKey = new ECPublicKey(peerKeyBytes);
var sharedSecret = await session.CalculateSecretAsync(
    PivSlot.KeyManagement,
    peerPublicKey);
```

### PIN Management

```csharp
// Verify PIN
await session.VerifyPinAsync(pin);

// Change PIN
await session.ChangePinAsync(currentPin, newPin);

// Unblock PIN using PUK
await session.UnblockPinAsync(puk, newPin);

// Change PUK
await session.ChangePukAsync(currentPuk, newPuk);

// Get PIN retry count
var attempts = await session.GetPinAttemptsAsync();
```

### Management Key Operations

```csharp
// Authenticate with management key (3DES/AES)
await session.AuthenticateAsync(managementKey);

// Set new management key
await session.SetManagementKeyAsync(
    newKey, 
    PivManagementKeyType.Aes256,
    PivTouchPolicy.Default);
```

### Factory Reset

```csharp
// Reset PIV application to factory defaults
await session.ResetAsync();
// After reset:
// - PIN: 123456
// - PUK: 12345678
// - Management Key: default 3DES key
// - All keys and certificates deleted
```

## Project Structure

```
Yubico.YubiKit.Piv/
├── src/
│   └── Yubico.YubiKit.Piv.csproj
└── tests/
    ├── Yubico.YubiKit.Piv.IntegrationTests/
    │   ├── PlaceholderTests.cs
    │   └── Yubico.YubiKit.Piv.IntegrationTests.csproj
    └── Yubico.YubiKit.Piv.UnitTests/
        ├── PlaceholderTests.cs
        └── Yubico.YubiKit.Piv.UnitTests.csproj
```

**Note:** This module is currently being migrated from the legacy codebase. The structure above shows the target organization.

## Common Use Cases

### 1. Basic Authentication Setup

```csharp
await using var session = await yubiKey.CreatePivSessionAsync();

// Reset to ensure clean state
await session.ResetAsync();

// Authenticate with management key
await session.AuthenticateAsync(defaultManagementKey);

// Generate key pair
var publicKey = await session.GenerateKeyAsync(
    PivSlot.Authentication,
    PivAlgorithm.EccP256);

// Get certificate from CA
var cert = GetCertificateFromCA(publicKey);

// Import certificate
await session.StoreCertificateAsync(PivSlot.Authentication, cert);
```

### 2. Signing with PIN Protection

```csharp
await using var session = await yubiKey.CreatePivSessionAsync();

// Authenticate and generate key with PIN always required
await session.AuthenticateAsync(managementKey);
var publicKey = await session.GenerateKeyAsync(
    PivSlot.Signature,
    PivAlgorithm.Rsa2048,
    PivPinPolicy.Always);

// Verify PIN
await session.VerifyPinAsync(pin);

// Sign (may prompt for PIN again if policy is Always)
var hash = SHA256.HashData(dataToSign);
var signature = await session.SignOrDecryptAsync(
    PivSlot.Signature, 
    PivAlgorithm.Rsa2048,
    hash);
```

### 3. Secure Key Management Setup

```csharp
await using var session = await yubiKey.CreatePivSessionAsync();

// Change from default management key
var newManagementKey = GenerateRandomKey(32); // 32 bytes for AES-256
await session.AuthenticateAsync(defaultManagementKey);
await session.SetManagementKeyAsync(
    newManagementKey, 
    PivManagementKeyType.Aes256);

// Change from default PIN
await session.ChangePinAsync("123456"u8.ToArray(), newPin);

// Change from default PUK
await session.ChangePukAsync("12345678"u8.ToArray(), newPuk);
```

### 4. Complete Signing Workflow

```csharp
await using var session = await yubiKey.CreatePivSessionAsync();
await session.ResetAsync(); // Start fresh

// 1. Authenticate and generate key
await session.AuthenticateAsync(defaultManagementKey);
var publicKey = await session.GenerateKeyAsync(
    PivSlot.Signature, 
    PivAlgorithm.EccP256,
    PivPinPolicy.Once);

// 2. Store certificate
var cert = CreateCertificate(publicKey);
await session.StoreCertificateAsync(PivSlot.Signature, cert);

// 3. Verify PIN
await session.VerifyPinAsync(pin);

// 4. Sign document
var hash = SHA256.HashData(document);
var signature = await session.SignOrDecryptAsync(
    PivSlot.Signature, 
    PivAlgorithm.EccP256, 
    hash);
```

### 5. Attestation

```csharp
await using var session = await yubiKey.CreatePivSessionAsync();

// Authenticate and generate key
await session.AuthenticateAsync(managementKey);
var publicKey = await session.GenerateKeyAsync(
    PivSlot.Authentication,
    PivAlgorithm.EccP256);

// Get attestation certificate
var attestationCert = await session.AttestKeyAsync(PivSlot.Authentication);

// Verify key was generated on YubiKey hardware
bool validAttestation = VerifyAttestation(attestationCert);
```

### 6. ECDH Key Agreement

```csharp
await using var session = await yubiKey.CreatePivSessionAsync();

// Generate key for key agreement
await session.AuthenticateAsync(managementKey);
var devicePublicKey = await session.GenerateKeyAsync(
    PivSlot.KeyManagement,
    PivAlgorithm.EccP256);

// Perform key agreement with peer's public key
await session.VerifyPinAsync(pin);
var sharedSecret = await session.CalculateSecretAsync(
    PivSlot.KeyManagement,
    peerPublicKey);

// Use shared secret for symmetric encryption
```

## KeyCollector Pattern

**Note:** This implementation uses direct async methods instead of the KeyCollector pattern. Credentials are passed directly to methods:

```csharp
await using var session = await yubiKey.CreatePivSessionAsync();

// Authenticate with management key
await session.AuthenticateAsync(managementKey);

// Verify PIN
await session.VerifyPinAsync(pin);

// Operations now proceed with authenticated state
var publicKey = await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);
```

For more complex scenarios, wrap these calls in your own credential management:

```csharp
async Task<PivSession> CreateAuthenticatedSession(IYubiKey yubiKey)
{
    var session = await yubiKey.CreatePivSessionAsync();
    
    try
    {
        var managementKey = await GetMgmtKeyFromSecureStorage();
        await session.AuthenticateAsync(managementKey);
        return session;
    }
    catch
    {
        await session.DisposeAsync();
        throw;
    }
}
```

## Security Best Practices

1. **Change Default Credentials**: Always change PIN, PUK, and management key from defaults
2. **Use PIN Policies Appropriately**: 
   - Use `Always` for signing keys
   - Use `Default` or `Once` for authentication keys
3. **Protect Management Key**: Store management key securely (or use PIN-only mode)
4. **Use Touch Policies for Sensitive Operations**: Add physical presence requirement
5. **Monitor Retry Counters**: Track failed attempts to detect attacks
6. **Use Attestation**: Verify keys were generated on-device for high-security applications

## Testing Guidance

See [CLAUDE.md](CLAUDE.md) for detailed test infrastructure information.

## References

- **PIV Standard**: NIST FIPS 201 and SP 800-73
- **YubiKey PIV Documentation**: https://developers.yubico.com/PIV/
- **PIV Tool**: https://developers.yubico.com/yubico-piv-tool/
