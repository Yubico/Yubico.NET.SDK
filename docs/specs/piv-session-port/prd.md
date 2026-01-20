# PRD: Port PivSession from Java yubikit-android to C# Yubico.YubiKit.Piv

**Status:** Final (Post-Validation)  
**Author:** Porting Orchestrator  
**Date:** 2026-01-18  
**Source:** `/home/dyallo/Code/y/yubikit-android/piv/src/main/java/com/yubico/yubikit/piv/`  
**Target:** `/home/dyallo/Code/y/Yubico.NET.SDK-2/Yubico.YubiKit.Piv/src/`

---

## Validation Summary

| Validator | Result | Key Findings |
|-----------|--------|--------------|
| Technical | ✅ PASS | Reuse Core key types; SCP automatic via InitializeCoreAsync |
| Security | ✅ PASS+WARN | Add temp PIN zeroing; document timing attack mitigations |
| DX | ⚠️ CONDITIONAL | Fixed: param order, PIN as bytes, async gzip |
| UX | ⚠️ CONDITIONAL | Fixed: cancellation, empty states, error messages |

**All critical findings addressed in this revision.**

---

## Executive Summary

Port the PIV (Personal Identity Verification) application support from the Java YubiKit Android SDK to the C# Yubico.YubiKit SDK. This includes the main `PivSession` class with 34 public methods, 11 supporting types (enums/records), and comprehensive test coverage.

The PIV application enables RSA and ECC cryptographic operations using private keys stored on the YubiKey, supporting NIST SP 800-73 compliant smart card functionality.

---

## Source Inventory

| Java File | Lines | C# Target | Purpose |
|-----------|-------|-----------|---------|
| `PivSession.java` | 1369 | `PivSession*.cs` (partial classes) | Main session class |
| `Slot.java` | 80 | `PivSlot.cs` | PIV slot enum |
| `KeyType.java` | 100 | `PivAlgorithm.cs` | Key algorithm enum |
| `PinPolicy.java` | 50 | `PivPinPolicy.cs` | PIN policy enum |
| `TouchPolicy.java` | 40 | `PivTouchPolicy.cs` | Touch policy enum |
| `ManagementKeyType.java` | 45 | `PivManagementKeyType.cs` | Management key types |
| `ObjectId.java` | 75 | `PivDataObject.cs` | Data object ID constants |
| `PinMetadata.java` | 30 | `PivPinMetadata.cs` | PIN/PUK metadata |
| `SlotMetadata.java` | 40 | `PivSlotMetadata.cs` | Slot metadata |
| `ManagementKeyMetadata.java` | 30 | `PivManagementKeyMetadata.cs` | Management key metadata |
| `BioMetadata.java` | 30 | `PivBioMetadata.cs` | Biometric metadata |
| `GzipUtils.java` | 40 | (inline in session) | Certificate compression |

**Total:** ~1929 lines of Java → estimated ~2500 lines C# (with async patterns)

---

## User Stories

### US-1: Initialize PIV Session

**As a** developer  
**I want to** create a PIV session with a YubiKey  
**So that** I can perform PIV operations

**Acceptance Criteria:**
- [ ] Create session from `IYubiKey` via `CreatePivSessionAsync()` extension method
- [ ] Create session from existing `ISmartCardConnection` via static `CreateAsync()` factory
- [ ] Session selects PIV AID and retrieves version on initialization
- [ ] Support optional SCP key parameters for secure channel
- [ ] Support optional firmware version override (for test mocking)
- [ ] Return `IPivSession` interface for testability

**Technical Notes:**
```csharp
// Extension method pattern
public static Task<PivSession> CreatePivSessionAsync(
    this IYubiKey yubiKey,
    ScpKeyParameters? scpKeyParams = null,
    ProtocolConfiguration? configuration = null,
    CancellationToken cancellationToken = default);

// Factory pattern
public static Task<PivSession> CreateAsync(
    ISmartCardConnection connection,
    ProtocolConfiguration? configuration = null,
    ScpKeyParameters? scpKeyParams = null,
    FirmwareVersion? firmwareVersion = null,
    CancellationToken cancellationToken = default);
```

---

### US-2: Authenticate with Management Key

**As a** developer  
**I want to** authenticate with the PIV management key  
**So that** I can perform privileged operations (key generation, import, certificate storage)

**Acceptance Criteria:**
- [ ] Authenticate using 3DES key (24 bytes, default)
- [ ] Authenticate using AES-128 key (16 bytes, requires 5.4+)
- [ ] Authenticate using AES-192 key (24 bytes, requires 5.4+)
- [ ] Authenticate using AES-256 key (32 bytes, requires 5.4+)
- [ ] Validate key length matches expected for key type
- [ ] Throw `BadResponseException` if challenge-response fails
- [ ] Zero management key memory after use
- [ ] Track authentication state in session

**Default Management Key:**
```
01 02 03 04 05 06 07 08  01 02 03 04 05 06 07 08  01 02 03 04 05 06 07 08
```

**Technical Notes:**
- Uses mutual authentication: device sends witness, client decrypts and sends challenge, device responds
- Management key type is cached after first retrieval

---

### US-3: Verify PIN

**As a** developer  
**I want to** verify the user's PIN  
**So that** I can perform operations requiring PIN authentication

**Acceptance Criteria:**
- [ ] Verify PIN (UTF-8 encoded, max 8 bytes)
- [ ] Pad PIN to 8 bytes with 0xFF
- [ ] Throw `InvalidPinException` with retry count on failure
- [ ] Track current/max PIN attempts internally
- [ ] Zero PIN memory after use
- [ ] Support empty PIN verification to check attempts remaining (metadata fallback)

**Default PIN:** `123456`

**Error Mapping:**
- SW 0x63Cx → x retries remaining (version ≥1.0.4)
- SW 0x63xx → xx retries remaining (version <1.0.4)
- SW 0x6983 → blocked (0 retries)

---

### US-4: Generate Key Pair

**As a** developer  
**I want to** generate a key pair on the YubiKey  
**So that** the private key never leaves the device

**Acceptance Criteria:**
- [ ] Generate RSA 1024-bit key (not on FIPS)
- [ ] Generate RSA 2048-bit key
- [ ] Generate RSA 3072-bit key (requires 5.7+)
- [ ] Generate RSA 4096-bit key (requires 5.7+)
- [ ] Generate ECC P-256 key
- [ ] Generate ECC P-384 key (requires 4.0+)
- [ ] Generate Ed25519 key (requires 5.7+)
- [ ] Generate X25519 key (requires 5.7+)
- [ ] Set PIN policy (Default, Never, Once, Always, MatchOnce, MatchAlways)
- [ ] Set Touch policy (Default, Never, Always, Cached)
- [ ] Return `PivPublicKey` containing the public key data
- [ ] Require management key authentication
- [ ] Validate version supports key type and policies
- [ ] Block RSA generation on YubiKey 4.2.6-4.3.4 (ROCA vulnerability)

**Slots:**
| Slot | Value | Purpose |
|------|-------|---------|
| Authentication | 0x9A | General authentication |
| Signature | 0x9C | Digital signatures (PIN each use per spec) |
| Key Management | 0x9D | Key agreement/encryption |
| Card Auth | 0x9E | Card authentication |
| Retired 1-20 | 0x82-0x95 | Historical key management |
| Attestation | 0xF9 | Attestation key (read-only) |

---

### US-5: Import Private Key

**As a** developer  
**I want to** import an existing private key  
**So that** I can use external keys with the YubiKey

**Acceptance Criteria:**
- [ ] Import RSA private key (CRT components: p, q, dmp1, dmq1, iqmp)
- [ ] Import ECC private key (secret scalar)
- [ ] Import Ed25519/X25519 private key
- [ ] Set PIN and Touch policies
- [ ] Return the detected `PivAlgorithm`
- [ ] Require management key authentication
- [ ] Zero private key memory after import

---

### US-6: Sign or Decrypt

**As a** developer  
**I want to** perform raw sign/decrypt operations  
**So that** I can use the private key for cryptographic operations

**Acceptance Criteria:**
- [ ] Sign/decrypt with RSA keys (PKCS#1 v1.5 or raw)
- [ ] Sign with ECC keys (returns DER-encoded signature)
- [ ] Sign with Ed25519 keys
- [ ] Left-pad payload for RSA if shorter than key size
- [ ] Truncate payload for ECC if longer than curve order
- [ ] Require PIN verification based on slot's PIN policy
- [ ] Throw descriptive error if key not present in slot

**Note:** Hashing and padding should be performed by caller. This is a raw operation.

---

### US-7: Calculate Shared Secret (ECDH)

**As a** developer  
**I want to** perform ECDH key agreement  
**So that** I can establish shared secrets with peer public keys

**Acceptance Criteria:**
- [ ] ECDH with P-256 private key and peer public key
- [ ] ECDH with P-384 private key and peer public key
- [ ] ECDH with X25519 private key and peer public key
- [ ] Return raw shared secret (x-coordinate for EC, full output for X25519)
- [ ] Validate peer key curve matches private key curve

---

### US-8: Certificate Management

**As a** developer  
**I want to** store and retrieve X.509 certificates  
**So that** I can associate certificates with key slots

**Acceptance Criteria:**
- [ ] Store certificate in slot (uncompressed)
- [ ] Store certificate with gzip compression (for large certs)
- [ ] Retrieve certificate from slot
- [ ] Detect and decompress gzip-compressed certificates
- [ ] Delete certificate from slot
- [ ] Parse certificate info byte (0x00=uncompressed, 0x01=gzip)
- [ ] Require management key for storage/deletion

**Technical Notes:**
- Certificates stored as TLV: TAG 0x70 (cert), TAG 0x71 (info), TAG 0xFE (LRC)
- Standard PIV limit: 1856 bytes; YubiKey extends to 3052 bytes

---

### US-9: Key Attestation

**As a** developer  
**I want to** attest that a key was generated on the YubiKey  
**So that** I can prove key provenance

**Acceptance Criteria:**
- [ ] Generate attestation certificate for a slot (requires 4.3+)
- [ ] Return X509Certificate2 signed by attestation key (0xF9)
- [ ] Throw descriptive error if key not generated on device
- [ ] Retrieve attestation certificate from 0xF9 slot

---

### US-10: PIN/PUK Management

**As a** developer  
**I want to** manage PIN and PUK  
**So that** users can change credentials and recover from lockout

**Acceptance Criteria:**
- [ ] Change PIN (old PIN + new PIN)
- [ ] Change PUK (old PUK + new PUK)
- [ ] Unblock PIN using PUK (PUK + new PIN)
- [ ] Set PIN/PUK retry counts (requires mgmt key + PIN verified)
- [ ] Get PIN attempts remaining
- [ ] Zero all PIN/PUK memory after operations

**Default PUK:** `12345678`

---

### US-11: Metadata Retrieval

**As a** developer  
**I want to** retrieve metadata about slots and credentials  
**So that** I can inspect YubiKey configuration

**Acceptance Criteria:**
- [ ] Get PIN metadata: isDefault, totalRetries, retriesRemaining (requires 5.3+)
- [ ] Get PUK metadata: isDefault, totalRetries, retriesRemaining (requires 5.3+)
- [ ] Get management key metadata: keyType, isDefault, touchPolicy (requires 5.3+)
- [ ] Get slot metadata: algorithm, pinPolicy, touchPolicy, isGenerated, publicKey (requires 5.3+)
- [ ] Fallback to attempt-counting for PIN attempts on older devices

---

### US-12: Biometric Authentication (YubiKey Bio)

**As a** developer  
**I want to** use biometric verification  
**So that** users can authenticate with fingerprint

**Acceptance Criteria:**
- [ ] Get bio metadata: isConfigured, retriesRemaining, hasTemporaryPin
- [ ] Verify UV (biometric): returns temporary PIN if requested
- [ ] Verify temporary PIN (16 bytes)
- [ ] Throw `UnsupportedOperationException` if bio not available
- [ ] Throw `InvalidPinException` on fingerprint mismatch

---

### US-13: Move and Delete Keys

**As a** developer  
**I want to** move keys between slots or delete them  
**So that** I can reorganize key storage

**Acceptance Criteria:**
- [ ] Move key from source to destination slot (requires 5.7+)
- [ ] Delete key from slot (requires 5.7+)
- [ ] Prevent moving attestation key (0xF9)
- [ ] Require management key authentication

---

### US-14: Data Object Operations

**As a** developer  
**I want to** read and write arbitrary PIV data objects  
**So that** I can manage CHUID, CCC, and other PIV objects

**Acceptance Criteria:**
- [ ] Read data object by ID (returns raw bytes)
- [ ] Write data object by ID
- [ ] Support standard PIV objects (CHUID, CCC, etc.)
- [ ] Support YubiKey extensions (PIVMAN data)

**Object IDs:**
| Name | ID | Purpose |
|------|-----|---------|
| CAPABILITY | 0x5FC107 | Card capability container |
| CHUID | 0x5FC102 | Card holder unique ID |
| AUTHENTICATION | 0x5FC105 | Cert for 9A |
| SIGNATURE | 0x5FC10A | Cert for 9C |
| KEY_MANAGEMENT | 0x5FC10B | Cert for 9D |
| CARD_AUTH | 0x5FC101 | Cert for 9E |
| DISCOVERY | 0x7E | Discovery object |
| RETIRED1-20 | 0x5FC10D-0x5FC120 | Retired certs |
| ATTESTATION | 0x5FFF01 | Attestation cert |

---

### US-15: Application Reset

**As a** developer  
**I want to** reset the PIV application to factory defaults  
**So that** I can start with a clean state

**Acceptance Criteria:**
- [ ] Block PIN by intentionally failing until locked
- [ ] Block PUK by intentionally failing until locked
- [ ] Send reset command (only works when both blocked)
- [ ] Verify bio not configured before reset (throw if configured)
- [ ] Reset internal attempt counters
- [ ] Refresh management key type after reset

---

### US-16: Serial Number

**As a** developer  
**I want to** retrieve the YubiKey serial number  
**So that** I can identify the device

**Acceptance Criteria:**
- [ ] Get serial number as int (requires 5.0+)
- [ ] Require SERIAL_API_VISIBLE flag on OTP slot

---

## Types to Implement

### Enums

#### PivSlot
```csharp
public enum PivSlot : byte
{
    Authentication = 0x9A,
    Signature = 0x9C,
    KeyManagement = 0x9D,
    CardAuthentication = 0x9E,
    Retired1 = 0x82,
    // ... Retired2-20
    Retired20 = 0x95,
    Attestation = 0xF9
}
```

#### PivAlgorithm
```csharp
public enum PivAlgorithm : byte
{
    Rsa1024 = 0x06,
    Rsa2048 = 0x07,
    Rsa3072 = 0x05,
    Rsa4096 = 0x16,
    EccP256 = 0x11,
    EccP384 = 0x14,
    Ed25519 = 0xE0,
    X25519 = 0xE1
}
```

#### PivPinPolicy
```csharp
public enum PivPinPolicy : byte
{
    Default = 0x00,
    Never = 0x01,
    Once = 0x02,
    Always = 0x03,
    MatchOnce = 0x04,
    MatchAlways = 0x05
}
```

#### PivTouchPolicy
```csharp
public enum PivTouchPolicy : byte
{
    Default = 0x00,
    Never = 0x01,
    Always = 0x02,
    Cached = 0x03
}
```

#### PivManagementKeyType
```csharp
public enum PivManagementKeyType : byte
{
    TripleDes = 0x03,  // 24 bytes, 8-byte challenge
    Aes128 = 0x08,     // 16 bytes, 16-byte challenge
    Aes192 = 0x0A,     // 24 bytes, 16-byte challenge
    Aes256 = 0x0C      // 32 bytes, 16-byte challenge
}
```

### Records

#### PivPinMetadata
```csharp
public readonly record struct PivPinMetadata(
    bool IsDefault,
    int TotalRetries,
    int RetriesRemaining);
```

#### PivManagementKeyMetadata
```csharp
public readonly record struct PivManagementKeyMetadata(
    PivManagementKeyType KeyType,
    bool IsDefault,
    PivTouchPolicy TouchPolicy);
```

#### PivSlotMetadata
```csharp
public readonly record struct PivSlotMetadata(
    PivAlgorithm Algorithm,
    PivPinPolicy PinPolicy,
    PivTouchPolicy TouchPolicy,
    bool IsGenerated,
    ReadOnlyMemory<byte> PublicKey);
```

#### PivBioMetadata
```csharp
public readonly record struct PivBioMetadata(
    bool IsConfigured,
    int RetriesRemaining,
    bool HasTemporaryPin);
```

### Constants

#### PivDataObject
```csharp
public static class PivDataObject
{
    public const int Capability = 0x5FC107;
    public const int Chuid = 0x5FC102;
    public const int Authentication = 0x5FC105;
    // ... etc
}
```

---

## Feature Gates

```csharp
public static class PivFeatures
{
    public static Feature P384 { get; } = new("P-384 Curve", 4, 0, 0);
    public static Feature UsagePolicy { get; } = new("PIN/Touch Policy", 4, 0, 0);
    public static Feature TouchCached { get; } = new("Cached Touch", 4, 3, 0);
    public static Feature Attestation { get; } = new("Attestation", 4, 3, 0);
    public static Feature Serial { get; } = new("Serial Number", 5, 0, 0);
    public static Feature Metadata { get; } = new("Metadata", 5, 3, 0);
    public static Feature AesKey { get; } = new("AES Management Key", 5, 4, 0);
    public static Feature MoveKey { get; } = new("Move/Delete Key", 5, 7, 0);
    public static Feature Cv25519 { get; } = new("Curve25519", 5, 7, 0);
    public static Feature Rsa3072Rsa4096 { get; } = new("RSA 3072/4096", 5, 7, 0);
    
    // Special: RSA generation broken on 4.2.6-4.3.4 (ROCA)
    public static bool SupportsRsaGeneration(FirmwareVersion version) =>
        version < new FirmwareVersion(4, 2, 6) || version >= new FirmwareVersion(4, 3, 5);
}
```

---

## Interface Definition

**Note:** Per DX validation, PIN/PUK parameters use `ReadOnlyMemory<byte>` (not `char`) to enable secure zeroing.

```csharp
public interface IPivSession : IApplicationSession, IAsyncDisposable
{
    // Session
    Task<int> GetSerialNumberAsync(CancellationToken cancellationToken = default);
    Task ResetAsync(CancellationToken cancellationToken = default);
    
    // Authentication
    /// <summary>Authenticate with management key. Key is NOT zeroed by this method - caller must zero.</summary>
    Task AuthenticateAsync(ReadOnlyMemory<byte> managementKey, CancellationToken cancellationToken = default);
    
    /// <summary>Verify PIN. Throws InvalidPinException with RetriesRemaining on failure.</summary>
    /// <exception cref="InvalidPinException">PIN incorrect. Check RetriesRemaining property.</exception>
    /// <exception cref="OperationCanceledException">Cancellation requested. Session state unchanged.</exception>
    Task VerifyPinAsync(ReadOnlyMemory<byte> pin, CancellationToken cancellationToken = default);
    
    /// <summary>Verify biometrics. Returns temporary PIN if requested, null otherwise.</summary>
    /// <returns>16-byte temporary PIN if requestTemporaryPin=true; null otherwise</returns>
    /// <exception cref="NotSupportedException">Bio not available on this YubiKey</exception>
    Task<ReadOnlyMemory<byte>?> VerifyUvAsync(bool requestTemporaryPin = false, bool checkOnly = false, CancellationToken cancellationToken = default);
    
    Task VerifyTemporaryPinAsync(ReadOnlyMemory<byte> temporaryPin, CancellationToken cancellationToken = default);
    
    // PIN/PUK (all throw InvalidPinException on auth failure)
    Task ChangePinAsync(ReadOnlyMemory<byte> oldPin, ReadOnlyMemory<byte> newPin, CancellationToken cancellationToken = default);
    Task ChangePukAsync(ReadOnlyMemory<byte> oldPuk, ReadOnlyMemory<byte> newPuk, CancellationToken cancellationToken = default);
    Task UnblockPinAsync(ReadOnlyMemory<byte> puk, ReadOnlyMemory<byte> newPin, CancellationToken cancellationToken = default);
    Task SetPinAttemptsAsync(int pinAttempts, int pukAttempts, CancellationToken cancellationToken = default);
    Task<int> GetPinAttemptsAsync(CancellationToken cancellationToken = default);
    
    // Keys - use Core's IPublicKey/IPrivateKey types per technical validation
    /// <summary>Generate key pair. RSA 4096 may take 30+ seconds.</summary>
    /// <returns>Public key (RSAPublicKey, ECPublicKey, or Curve25519PublicKey based on algorithm)</returns>
    Task<IPublicKey> GenerateKeyAsync(PivSlot slot, PivAlgorithm algorithm, PivPinPolicy pinPolicy = PivPinPolicy.Default, PivTouchPolicy touchPolicy = PivTouchPolicy.Default, CancellationToken cancellationToken = default);
    
    Task<PivAlgorithm> ImportKeyAsync(PivSlot slot, IPrivateKey privateKey, PivPinPolicy pinPolicy = PivPinPolicy.Default, PivTouchPolicy touchPolicy = PivTouchPolicy.Default, CancellationToken cancellationToken = default);
    Task MoveKeyAsync(PivSlot sourceSlot, PivSlot destinationSlot, CancellationToken cancellationToken = default);
    Task DeleteKeyAsync(PivSlot slot, CancellationToken cancellationToken = default);
    Task<X509Certificate2> AttestKeyAsync(PivSlot slot, CancellationToken cancellationToken = default);
    
    // Crypto
    Task<ReadOnlyMemory<byte>> SignOrDecryptAsync(PivSlot slot, PivAlgorithm algorithm, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
    Task<ReadOnlyMemory<byte>> CalculateSecretAsync(PivSlot slot, IPublicKey peerPublicKey, CancellationToken cancellationToken = default);
    
    // Certificates
    /// <summary>Get certificate from slot.</summary>
    /// <returns>Certificate or null if slot is empty</returns>
    Task<X509Certificate2?> GetCertificateAsync(PivSlot slot, CancellationToken cancellationToken = default);
    
    /// <summary>Store certificate. Large certs (>1856 bytes) auto-compress unless compress=false.</summary>
    Task StoreCertificateAsync(PivSlot slot, X509Certificate2 certificate, bool compress = false, CancellationToken cancellationToken = default);
    
    /// <summary>Delete certificate. No-op if slot already empty (idempotent).</summary>
    Task DeleteCertificateAsync(PivSlot slot, CancellationToken cancellationToken = default);
    
    // Metadata (5.3+)
    Task<PivPinMetadata> GetPinMetadataAsync(CancellationToken cancellationToken = default);
    Task<PivPinMetadata> GetPukMetadataAsync(CancellationToken cancellationToken = default);
    Task<PivManagementKeyMetadata> GetManagementKeyMetadataAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Get slot metadata.</summary>
    /// <returns>Metadata or null if slot is empty</returns>
    Task<PivSlotMetadata?> GetSlotMetadataAsync(PivSlot slot, CancellationToken cancellationToken = default);
    
    Task<PivBioMetadata> GetBioMetadataAsync(CancellationToken cancellationToken = default);
    
    // Data Objects
    /// <summary>Read data object.</summary>
    /// <returns>Object data or empty if not found</returns>
    Task<ReadOnlyMemory<byte>> GetObjectAsync(int objectId, CancellationToken cancellationToken = default);
    
    /// <summary>Write data object. Pass null to delete.</summary>
    Task PutObjectAsync(int objectId, ReadOnlyMemory<byte>? data, CancellationToken cancellationToken = default);
    
    // Management Key
    Task SetManagementKeyAsync(PivManagementKeyType keyType, ReadOnlyMemory<byte> newKey, bool requireTouch = false, CancellationToken cancellationToken = default);
    PivManagementKeyType ManagementKeyType { get; }
}
```

### Cancellation Behavior

All async methods support cancellation with consistent behavior:

| State When Cancelled | Behavior |
|---------------------|----------|
| Before APDU sent | Throws `OperationCanceledException`, no state change |
| During APDU | Waits for APDU completion, then throws (atomic) |
| After APDU | Completes normally (too late to cancel) |

**Important:** Cancellation does NOT leave session in inconsistent state. Operations are atomic at the APDU level.

### Empty State Handling

| Method | Empty Slot Behavior |
|--------|---------------------|
| `GetCertificateAsync` | Returns `null` |
| `GetSlotMetadataAsync` | Returns `null` |
| `GetObjectAsync` | Returns `ReadOnlyMemory<byte>.Empty` |
| `DeleteCertificateAsync` | No-op (idempotent) |
| `DeleteKeyAsync` | Throws `InvalidOperationException` |

### Default Policy Behavior

`PivPinPolicy.Default` and `PivTouchPolicy.Default` resolve to slot-specific defaults:

| Slot | Default PIN Policy | Default Touch Policy |
|------|-------------------|---------------------|
| 0x9A (Auth) | Once | Never |
| 0x9C (Sign) | Always | Never |
| 0x9D (KeyMgmt) | Once | Never |
| 0x9E (CardAuth) | Never | Never |
| Retired | Once | Never |

---

## Security Requirements

### SR-1: Sensitive Data Zeroing
All PIN, PUK, and management key data MUST be zeroed after use:
```csharp
finally
{
    CryptographicOperations.ZeroMemory(pinBuffer);
}
```

### SR-2: Temporary PIN Zeroing (from Security Audit)
Bio temporary PINs returned from `VerifyUvAsync` MUST be zeroed by caller after use:
```csharp
var tempPin = await session.VerifyUvAsync(requestTemporaryPin: true);
try
{
    await session.VerifyTemporaryPinAsync(tempPin!.Value);
}
finally
{
    if (tempPin.HasValue)
        CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(tempPin.Value).Span);
}
```

### SR-3: No Sensitive Data Logging
NEVER log PIN values, PUK values, management keys, or private key material:
```csharp
// ✅ OK
_logger.LogDebug("PIN verified successfully");

// ❌ NEVER
_logger.LogDebug("PIN value: {Pin}", pin);
```

### SR-4: Memory Types for Secrets
Use stack-allocated or pooled memory for sensitive data:
```csharp
Span<byte> pinBytes = stackalloc byte[8];
// OR
byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(24);
try { ... }
finally 
{
    CryptographicOperations.ZeroMemory(rentedBuffer.AsSpan(0, 24));
    ArrayPool<byte>.Shared.Return(rentedBuffer);
}
```

### SR-5: Validate Input Lengths
Always validate that provided keys/PINs match expected lengths before use.

### SR-6: Timing Attack Mitigations (from Security Audit)
PIN verification should use constant-time comparison where possible. Error handling should not reveal timing information about which character failed.

---

## Error Handling

### Exception Mapping

| Condition | Exception Type | Message Pattern |
|-----------|---------------|-----------------|
| Wrong PIN/PUK | `InvalidPinException` | "PIN verification failed. {n} attempts remaining before lockout." |
| PIN/PUK blocked | `InvalidPinException` | "PIN is blocked. Use PUK to unblock or reset the application." |
| Challenge-response mismatch | `BadResponseException` | "Management key authentication failed. Verify the key is correct." |
| Feature not supported | `NotSupportedException` | "{Feature} requires YubiKey firmware {version}+. Current: {current}." |
| Invalid slot/algorithm combo | `ArgumentException` | "Cannot generate {algorithm} key in slot {slot}." |
| Key not present in slot | `InvalidOperationException` | "No key present in slot {slot}. Generate or import a key first." |
| Bio not available | `NotSupportedException` | "Biometric verification not supported by this YubiKey." |
| Slot already occupied (move) | `InvalidOperationException` | "Destination slot {slot} already contains a key. Delete it first." |
| Cancellation | `OperationCanceledException` | Standard .NET message, session state unchanged |

### Exception Hierarchy

```
Exception
├── InvalidPinException (has RetriesRemaining property)
├── BadResponseException  
├── NotSupportedException (standard .NET)
├── ArgumentException (standard .NET)
├── InvalidOperationException (standard .NET)
├── OperationCanceledException (standard .NET)
└── ApduException (low-level, wrap before throwing)
```

**Pattern:** Higher-level methods wrap `ApduException` with actionable messages. Only protocol/debug code should catch `ApduException` directly.

### APDU Status Word Handling

| SW | Meaning | Action |
|----|---------|--------|
| 0x9000 | Success | Return data |
| 0x63Cx | x retries remaining | Throw `InvalidPinException(x)` |
| 0x6983 | Authentication blocked | Throw `InvalidPinException(0)` |
| 0x6982 | Security status not satisfied | Throw `InvalidOperationException("Authentication required...")` |
| 0x6A82 | File/application not found | Return null/empty (for queries) or throw (for writes) |
| 0x6985 | Conditions not satisfied | Throw with specific context |

---

## Implementation Plan

### Phase 1: Foundation (Week 1)
- [ ] Create enum types (PivSlot, PivAlgorithm, PivPinPolicy, PivTouchPolicy, PivManagementKeyType)
- [ ] Create record types (metadata structs)
- [ ] Create PivDataObject constants class
- [ ] Create PivFeatures static class
- [ ] Create IPivSession interface

### Phase 2: Session Core (Week 2)
- [ ] Create PivSession.cs with CreateAsync factory
- [ ] Implement initialization (SELECT, GET VERSION)
- [ ] Implement Dispose pattern
- [ ] Implement GetSerialNumberAsync

### Phase 3: Authentication (Week 3)
- [ ] Create PivSession.Authentication.cs partial
- [ ] Implement AuthenticateAsync (3DES + AES)
- [ ] Implement VerifyPinAsync
- [ ] Implement PIN/PUK change methods
- [ ] Implement UnblockPinAsync
- [ ] Implement GetPinAttemptsAsync

### Phase 4: Key Operations (Week 4)
- [ ] Create PivSession.KeyPairs.cs partial
- [ ] Implement GenerateKeyAsync
- [ ] Implement ImportKeyAsync
- [ ] Implement MoveKeyAsync / DeleteKeyAsync
- [ ] Implement AttestKeyAsync

### Phase 5: Cryptographic Operations (Week 5)
- [ ] Create PivSession.Crypto.cs partial
- [ ] Implement SignOrDecryptAsync
- [ ] Implement CalculateSecretAsync

### Phase 6: Certificates & Data (Week 6)
- [ ] Create PivSession.Certificates.cs partial
- [ ] Implement certificate CRUD with compression
- [ ] Create PivSession.DataObjects.cs partial
- [ ] Implement generic object read/write

### Phase 7: Metadata & Bio (Week 7)
- [ ] Create PivSession.Metadata.cs partial
- [ ] Implement all metadata methods
- [ ] Create PivSession.Bio.cs partial
- [ ] Implement bio verification methods

### Phase 8: Testing & Polish (Week 8)
- [ ] Unit tests for all types
- [ ] Integration tests for all operations
- [ ] Update module CLAUDE.md
- [ ] Create README.md with usage examples

---

## Appendix A: APDU Instructions

| Name | INS | Purpose |
|------|-----|---------|
| VERIFY | 0x20 | Verify PIN |
| CHANGE_REFERENCE | 0x24 | Change PIN/PUK |
| RESET_RETRY | 0x2C | Unblock PIN with PUK |
| GENERATE_ASYMMETRIC | 0x47 | Generate key pair |
| AUTHENTICATE | 0x87 | Mgmt key auth / crypto ops |
| GET_DATA | 0xCB | Read data object |
| PUT_DATA | 0xDB | Write data object |
| MOVE_KEY | 0xF6 | Move/delete key |
| GET_METADATA | 0xF7 | Get metadata |
| GET_SERIAL | 0xF8 | Get serial number |
| ATTEST | 0xF9 | Attest key |
| SET_PIN_RETRIES | 0xFA | Set retry counts |
| RESET | 0xFB | Factory reset |
| GET_VERSION | 0xFD | Get PIV version |
| IMPORT_KEY | 0xFE | Import private key |
| SET_MGMKEY | 0xFF | Set management key |

---

## Appendix B: TLV Tags

| Tag | Purpose |
|-----|---------|
| 0x80 | Auth witness / Gen algorithm |
| 0x81 | Auth challenge |
| 0x82 | Auth response |
| 0x85 | Auth exponentiation (ECDH) |
| 0x53 | Data object content |
| 0x5C | Object ID |
| 0x70 | Certificate |
| 0x71 | Certificate info |
| 0x7C | Dynamic authentication |
| 0x7F49 | Public key from generation |
| 0xAA | PIN policy |
| 0xAB | Touch policy |
| 0xFE | LRC (error detection) |
| 0x01 | Metadata: algorithm |
| 0x02 | Metadata: policy |
| 0x03 | Metadata: origin |
| 0x04 | Metadata: public key |
| 0x05 | Metadata: is default |
| 0x06 | Metadata: retries |
| 0x07 | Metadata: bio configured |
| 0x08 | Metadata: temporary PIN |

---

## Appendix C: Test Device Notes

- **Device:** YubiKey 5.8.0-alpha
- **Quirk:** FIDO `AuthenticatorData.FirmwareVersion` reports 0.0.1 (incorrect)
- **Workaround:** Use `ManagementSession.GetDeviceInfoAsync()` for accurate firmware version
- **Enabled Capabilities:** USB-A with OTP, PIV, NFC
- **Note:** Enhanced PIN has stricter requirements (alphanumeric, min length, etc.)
