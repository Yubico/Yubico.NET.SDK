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

// Synchronous version
using var session = new PivSession(yubiKeyDevice);

// With key collector for PIN/management key
session.KeyCollector = MyKeyCollectorDelegate;
```

### Key Generation

```csharp
// Generate RSA 2048 key
var publicKey = session.GenerateKeyPair(
    PivSlot.Authentication,
    PivAlgorithm.Rsa2048,
    PivPinPolicy.Default,
    PivTouchPolicy.Default);

// Generate EC P-256 key
var publicKey = session.GenerateKeyPair(
    PivSlot.Signing,
    PivAlgorithm.EccP256,
    PivPinPolicy.Always,    // PIN required every time
    PivTouchPolicy.Cached); // Touch required (15s cache)
```

### Certificate Operations

```csharp
// Import certificate
session.ImportCertificate(PivSlot.Authentication, certificate);

// Retrieve certificate
var cert = session.GetCertificate(PivSlot.Authentication);
```

### Cryptographic Operations

```csharp
// Sign data
byte[] dataToSign = ...;
byte[] signature = session.Sign(PivSlot.Authentication, dataToSign);

// Decrypt data (RSA)
byte[] encryptedData = ...;
byte[] decrypted = session.Decrypt(PivSlot.KeyManagement, encryptedData);

// Key agreement (ECDH)
byte[] sharedSecret = session.KeyAgree(
    PivSlot.KeyManagement,
    otherPartyPublicKey);
```

### PIN Management

```csharp
// Verify PIN
bool verified = session.TryVerifyPin(pin);

// Change PIN
session.ChangePin(currentPin, newPin);

// Reset PIN using PUK
session.ResetPin(puk, newPin);

// Change PUK
session.ChangePuk(currentPuk, newPuk);
```

### Management Key Operations

```csharp
// Authenticate management key (3DES)
bool authenticated = session.TryAuthenticateManagementKey(managementKey);

// Change management key
session.ChangeManagementKey(currentKey, newKey, PivTouchPolicy.Default);

// Set PIN-only mode (store management key on YubiKey)
session.SetPinOnlyMode(pin, managementKey, PivTouchPolicy.Default);
```

### Factory Reset

```csharp
// Reset PIV application to factory defaults
session.ResetApplication();
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
using var session = new PivSession(yubiKey);
session.KeyCollector = MyKeyCollector;

// Generate key pair
var publicKey = session.GenerateKeyPair(
    PivSlot.Authentication,
    PivAlgorithm.EccP256);

// Get certificate from CA
var cert = GetCertificateFromCA(publicKey);

// Import certificate
session.ImportCertificate(PivSlot.Authentication, cert);
```

### 2. Signing with PIN Protection

```csharp
using var session = new PivSession(yubiKey);
session.KeyCollector = MyKeyCollector;

// Generate key with PIN always required
var publicKey = session.GenerateKeyPair(
    PivSlot.Signing,
    PivAlgorithm.Rsa2048,
    PivPinPolicy.Always);

// Sign (will prompt for PIN via KeyCollector)
byte[] signature = session.Sign(PivSlot.Signing, dataToSign);
```

### 3. Secure Key Management Setup

```csharp
using var session = new PivSession(yubiKey);

// Change from default management key
var newManagementKey = GenerateRandomKey(24); // 24 bytes for 3DES
session.TryAuthenticateManagementKey(defaultManagementKey);
session.ChangeManagementKey(defaultManagementKey, newManagementKey);

// Change from default PIN
session.ChangePin("123456", "NewSecurePin");

// Change from default PUK
session.ChangePuk("12345678", "NewSecurePuk");
```

### 4. PIN-Only Mode (Convenience)

```csharp
using var session = new PivSession(yubiKey);

// Store management key on YubiKey, protected by PIN
session.TryAuthenticateManagementKey(managementKey);
session.SetPinOnlyMode(pin, managementKey);

// Now operations only require PIN, not management key
session.TryVerifyPin(pin);
session.GenerateKeyPair(PivSlot.Authentication, PivAlgorithm.EccP256);
// No need to authenticate management key separately
```

### 5. Attestation

```csharp
using var session = new PivSession(yubiKey);

// Generate key
var publicKey = session.GenerateKeyPair(
    PivSlot.Authentication,
    PivAlgorithm.EccP256);

// Get attestation certificate
var attestationCert = session.GetAttestationCertificate(PivSlot.Authentication);

// Verify key was generated on YubiKey hardware
bool validAttestation = VerifyAttestation(attestationCert);
```

## KeyCollector Pattern

The `KeyCollector` delegate is called when the SDK needs PIN, PUK, or management key:

```csharp
bool MyKeyCollector(KeyEntryData keyEntryData)
{
    if (keyEntryData.IsRetry)
    {
        Console.WriteLine($"Previous attempt failed. Retries remaining: {keyEntryData.RetriesRemaining}");
    }

    switch (keyEntryData.Request)
    {
        case KeyEntryRequest.Release:
            // Clean up any resources
            return true;

        case KeyEntryRequest.VerifyPivPin:
            Console.Write("Enter PIN: ");
            byte[] pin = GetPinFromUser();
            keyEntryData.SubmitValue(pin);
            return true;

        case KeyEntryRequest.AuthenticatePivManagementKey:
            byte[] mgmtKey = GetManagementKeyFromSecureStorage();
            keyEntryData.SubmitValue(mgmtKey);
            return true;

        default:
            return false; // Cancel operation
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
