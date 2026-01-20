---
active: false
iteration: 2
max_iterations: 10
completion_promise: "PIV_SESSION_PORT_COMPLETE"
started_at: "2026-01-18T14:40:09.637Z"
---

# PivSession Port Implementation Plan (Ralph Loop)

**Goal:** Port PIV (Personal Identity Verification) application support from Java yubikit-android to C# Yubico.YubiKit.Piv, enabling RSA/ECC cryptographic operations using YubiKey-stored private keys.

**PRD:** `docs/specs/piv-session-port/prd.md`  
**Completion Promise:** `PIV_SESSION_PORT_COMPLETE`

---

## Phase 1: Foundation Types

**User Story:** As a developer, I want PIV type definitions so that I can work with strongly-typed PIV concepts.

**Files:**
- Create: `Yubico.YubiKit.Piv/src/PivSlot.cs`
- Create: `Yubico.YubiKit.Piv/src/PivAlgorithm.cs`
- Create: `Yubico.YubiKit.Piv/src/PivPinPolicy.cs`
- Create: `Yubico.YubiKit.Piv/src/PivTouchPolicy.cs`
- Create: `Yubico.YubiKit.Piv/src/PivManagementKeyType.cs`
- Create: `Yubico.YubiKit.Piv/src/PivDataObject.cs`
- Create: `Yubico.YubiKit.Piv/src/PivMetadata.cs`
- Create: `Yubico.YubiKit.Piv/src/PivFeatures.cs`
- Create: `Yubico.YubiKit.Piv/src/IPivSession.cs`
- Test: `Yubico.YubiKit.Piv/tests/UnitTests/PivTypesTests.cs`

**Step 1: Write failing tests**
```csharp
// PivTypesTests.cs
namespace Yubico.YubiKit.Piv.UnitTests;

public class PivTypesTests
{
    [Theory]
    [InlineData(PivSlot.Authentication, 0x9A)]
    [InlineData(PivSlot.Signature, 0x9C)]
    [InlineData(PivSlot.KeyManagement, 0x9D)]
    [InlineData(PivSlot.CardAuthentication, 0x9E)]
    [InlineData(PivSlot.Attestation, 0xF9)]
    public void PivSlot_HasCorrectValue(PivSlot slot, byte expected)
    {
        Assert.Equal(expected, (byte)slot);
    }

    [Theory]
    [InlineData(PivAlgorithm.Rsa1024, 0x06)]
    [InlineData(PivAlgorithm.Rsa2048, 0x07)]
    [InlineData(PivAlgorithm.EccP256, 0x11)]
    [InlineData(PivAlgorithm.EccP384, 0x14)]
    [InlineData(PivAlgorithm.Ed25519, 0xE0)]
    [InlineData(PivAlgorithm.X25519, 0xE1)]
    public void PivAlgorithm_HasCorrectValue(PivAlgorithm algo, byte expected)
    {
        Assert.Equal(expected, (byte)algo);
    }

    [Theory]
    [InlineData(PivManagementKeyType.TripleDes, 0x03)]
    [InlineData(PivManagementKeyType.Aes128, 0x08)]
    [InlineData(PivManagementKeyType.Aes192, 0x0A)]
    [InlineData(PivManagementKeyType.Aes256, 0x0C)]
    public void PivManagementKeyType_HasCorrectValue(PivManagementKeyType type, byte expected)
    {
        Assert.Equal(expected, (byte)type);
    }

    [Fact]
    public void PivFeatures_P384_RequiresVersion4()
    {
        var feature = PivFeatures.P384;
        Assert.Equal(new FirmwareVersion(4, 0, 0), feature.Version);
    }

    [Fact]
    public void PivFeatures_SupportsRsaGeneration_FalseFor426()
    {
        Assert.False(PivFeatures.SupportsRsaGeneration(new FirmwareVersion(4, 2, 6)));
        Assert.False(PivFeatures.SupportsRsaGeneration(new FirmwareVersion(4, 3, 0)));
        Assert.True(PivFeatures.SupportsRsaGeneration(new FirmwareVersion(4, 3, 5)));
        Assert.True(PivFeatures.SupportsRsaGeneration(new FirmwareVersion(5, 0, 0)));
    }
}
```

**Step 2: Verify RED**
```bash
dotnet build.cs test --filter "FullyQualifiedName~PivTypesTests"
```
Expected: Compilation failure (types don't exist)

**Step 3: Implement**
Create all enum and record types per PRD §Types to Implement:
- `PivSlot` enum with all 26 slots (Auth, Sign, KeyMgmt, CardAuth, Retired1-20, Attestation)
- `PivAlgorithm` enum with RSA 1024/2048/3072/4096, ECC P-256/P-384, Ed25519, X25519
- `PivPinPolicy` enum (Default, Never, Once, Always, MatchOnce, MatchAlways)
- `PivTouchPolicy` enum (Default, Never, Always, Cached)
- `PivManagementKeyType` enum (TripleDes, Aes128, Aes192, Aes256)
- `PivDataObject` static class with all object ID constants
- `PivPinMetadata`, `PivManagementKeyMetadata`, `PivSlotMetadata`, `PivBioMetadata` records
- `PivFeatures` static class with all Feature instances
- `IPivSession` interface per PRD Interface Definition

**Step 4: Verify GREEN**
```bash
dotnet build.cs test --filter "FullyQualifiedName~PivTypesTests"
```

**Step 5: Commit**
```bash
git add Yubico.YubiKit.Piv/src/Piv*.cs Yubico.YubiKit.Piv/src/IPivSession.cs \
        Yubico.YubiKit.Piv/tests/UnitTests/PivTypesTests.cs
git commit -m "feat(piv): add PIV type definitions and IPivSession interface"
```

→ Output `<promise>PHASE_1_DONE</promise>`

---

## Phase 2: Session Core

**User Story:** As a developer, I want to create a PIV session with a YubiKey so that I can perform PIV operations.

**Files:**
- Create: `Yubico.YubiKit.Piv/src/PivSession.cs`
- Create: `Yubico.YubiKit.Piv/src/IYubiKeyExtensions.cs`
- Test: `Yubico.YubiKit.Piv/tests/UnitTests/PivSessionTests.cs`
- Test: `Yubico.YubiKit.Piv/tests/IntegrationTests/PivSessionIntegrationTests.cs`

**Step 1: Write failing tests**
```csharp
// PivSessionTests.cs (unit tests with mocks)
public class PivSessionTests
{
    [Fact]
    public async Task CreateAsync_SelectsPivApplication()
    {
        var mockProtocol = new Mock<ISmartCardProtocol>();
        mockProtocol.Setup(p => p.SelectAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SelectResponse(/* success */));
        mockProtocol.Setup(p => p.TransmitAsync(It.IsAny<ApduCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApduResponse(new byte[] { 5, 8, 0, 0x90, 0x00 })); // Version 5.8.0
        
        var session = await PivSession.CreateAsync(mockProtocol.Object);
        
        Assert.NotNull(session);
        Assert.Equal(new FirmwareVersion(5, 8, 0), session.FirmwareVersion);
    }

    [Fact]
    public async Task Dispose_ClosesProtocol()
    {
        var mockProtocol = new Mock<ISmartCardProtocol>();
        // ... setup
        
        var session = await PivSession.CreateAsync(mockProtocol.Object);
        session.Dispose();
        
        mockProtocol.Verify(p => p.Dispose(), Times.Once);
    }
}

// PivSessionIntegrationTests.cs (requires YubiKey)
public class PivSessionIntegrationTests
{
    [Theory]
    [WithYubiKey]
    public async Task CreateAsync_WithYubiKey_ReturnsInitializedSession(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        
        Assert.True(session.IsInitialized);
        Assert.True(session.FirmwareVersion >= new FirmwareVersion(4, 0, 0));
    }

    [Theory]
    [WithYubiKey(MinimumFirmware = "5.0.0")]
    public async Task GetSerialNumberAsync_ReturnsValidSerial(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        
        var serial = await session.GetSerialNumberAsync();
        
        Assert.True(serial > 0);
    }
}
```

**Step 2: Verify RED**
```bash
dotnet build.cs test --filter "FullyQualifiedName~PivSession"
```
Expected: Compilation failure (PivSession doesn't exist)

**Step 3: Implement**
Create `PivSession.cs`:
- Inherit from `ApplicationSession`
- Implement `IAsyncDisposable`
- Static `CreateAsync()` factory that:
  1. Selects PIV AID (A0 00 00 03 08)
  2. Sends GET VERSION (INS 0xFD)
  3. Calls `InitializeCoreAsync()` with version
- Implement `GetSerialNumberAsync()` (INS 0xF8, requires 5.0+)
- Cache `ManagementKeyType` on init (via metadata or default to TDES)

Create `IYubiKeyExtensions.cs`:
- `CreatePivSessionAsync()` extension method on `IYubiKey`

**Step 4: Verify GREEN**
```bash
dotnet build.cs test --filter "FullyQualifiedName~PivSession"
```

**Step 5: Commit**
```bash
git add Yubico.YubiKit.Piv/src/PivSession.cs Yubico.YubiKit.Piv/src/IYubiKeyExtensions.cs \
        Yubico.YubiKit.Piv/tests/
git commit -m "feat(piv): add PivSession core with CreateAsync factory"
```

→ Output `<promise>PHASE_2_DONE</promise>`

---

## Phase 3: Authentication

**User Story:** As a developer, I want to authenticate with management key and verify PIN so that I can perform privileged operations.

**Files:**
- Create: `Yubico.YubiKit.Piv/src/PivSession.Authentication.cs`
- Create: `Yubico.YubiKit.Piv/src/InvalidPinException.cs`
- Test: `Yubico.YubiKit.Piv/tests/IntegrationTests/PivAuthenticationTests.cs`

**Step 1: Write failing tests**
```csharp
public class PivAuthenticationTests
{
    private static readonly byte[] DefaultManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };
    private static readonly byte[] DefaultPin = "123456"u8.ToArray();

    [Theory]
    [WithYubiKey]
    public async Task AuthenticateAsync_WithDefaultKey_Succeeds(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync(); // Ensure default state
        
        await session.AuthenticateAsync(DefaultManagementKey);
        
        Assert.True(session.IsAuthenticated);
    }

    [Theory]
    [WithYubiKey]
    public async Task AuthenticateAsync_WithWrongKey_ThrowsBadResponse(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var wrongKey = new byte[24];
        
        await Assert.ThrowsAsync<BadResponseException>(
            () => session.AuthenticateAsync(wrongKey));
    }

    [Theory]
    [WithYubiKey]
    public async Task VerifyPinAsync_WithCorrectPin_Succeeds(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        
        await session.VerifyPinAsync(DefaultPin);
        
        // No exception means success
    }

    [Theory]
    [WithYubiKey]
    public async Task VerifyPinAsync_WithWrongPin_ThrowsInvalidPinException(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var wrongPin = "000000"u8.ToArray();
        
        var ex = await Assert.ThrowsAsync<InvalidPinException>(
            () => session.VerifyPinAsync(wrongPin));
        
        Assert.True(ex.RetriesRemaining >= 0);
        Assert.True(ex.RetriesRemaining < 3); // One attempt used
    }

    [Theory]
    [WithYubiKey]
    public async Task GetPinAttemptsAsync_ReturnsCorrectCount(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var attempts = await session.GetPinAttemptsAsync();
        
        Assert.Equal(3, attempts); // Default after reset
    }
}
```

**Step 2: Verify RED**
```bash
dotnet build.cs test --filter "FullyQualifiedName~PivAuthenticationTests"
```

**Step 3: Implement**
Create `PivSession.Authentication.cs` partial class:
- `AuthenticateAsync()`: Mutual auth with 3DES/AES per PRD
  - Send empty witness request (TAG 0x80)
  - Decrypt witness, encrypt challenge
  - Verify response
  - Zero key after use!
- `VerifyPinAsync()`: INS 0x20, P2=0x80
  - Pad to 8 bytes with 0xFF
  - Parse SW 0x63Cx for retry count
  - Zero PIN after use!
- `GetPinAttemptsAsync()`: Use metadata (5.3+) or empty verify fallback
- `ChangePinAsync()`, `ChangePukAsync()`, `UnblockPinAsync()`: INS 0x24/0x2C
- `SetPinAttemptsAsync()`: INS 0xFA

Create `InvalidPinException.cs`:
- Property `int RetriesRemaining`
- Actionable message per PRD error handling

**Security:** Ensure ALL sensitive data zeroed in finally blocks!

**Step 4: Verify GREEN**
```bash
dotnet build.cs test --filter "FullyQualifiedName~PivAuthenticationTests"
```

**Step 5: Commit**
```bash
git add Yubico.YubiKit.Piv/src/PivSession.Authentication.cs \
        Yubico.YubiKit.Piv/src/InvalidPinException.cs \
        Yubico.YubiKit.Piv/tests/
git commit -m "feat(piv): add management key and PIN authentication"
```

→ Output `<promise>PHASE_3_DONE</promise>`

---

## Phase 4: Key Operations

**User Story:** As a developer, I want to generate and import private keys so that I can use YubiKey for cryptographic operations.

**Files:**
- Create: `Yubico.YubiKit.Piv/src/PivSession.KeyPairs.cs`
- Test: `Yubico.YubiKit.Piv/tests/IntegrationTests/PivKeyOperationsTests.cs`

**Step 1: Write failing tests**
```csharp
public class PivKeyOperationsTests
{
    [Theory]
    [WithYubiKey]
    public async Task GenerateKeyAsync_EccP256_ReturnsPublicKey(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(DefaultManagementKey);
        
        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Authentication, 
            PivAlgorithm.EccP256);
        
        Assert.NotNull(publicKey);
        Assert.IsType<ECPublicKey>(publicKey);
    }

    [Theory]
    [WithYubiKey(MinimumFirmware = "5.7.0")]
    public async Task GenerateKeyAsync_Ed25519_ReturnsPublicKey(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(DefaultManagementKey);
        
        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Signature, 
            PivAlgorithm.Ed25519);
        
        Assert.NotNull(publicKey);
        Assert.IsType<Curve25519PublicKey>(publicKey);
    }

    [Theory]
    [WithYubiKey(MinimumFirmware = "4.3.0")]
    public async Task AttestKeyAsync_GeneratedKey_ReturnsCertificate(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(DefaultManagementKey);
        await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);
        
        var attestation = await session.AttestKeyAsync(PivSlot.Authentication);
        
        Assert.NotNull(attestation);
        Assert.Contains("Yubico", attestation.Issuer);
    }

    [Theory]
    [WithYubiKey(MinimumFirmware = "5.7.0")]
    public async Task MoveKeyAsync_MovesToNewSlot(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(DefaultManagementKey);
        await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);
        
        await session.MoveKeyAsync(PivSlot.Authentication, PivSlot.Retired1);
        
        var sourceMetadata = await session.GetSlotMetadataAsync(PivSlot.Authentication);
        var destMetadata = await session.GetSlotMetadataAsync(PivSlot.Retired1);
        
        Assert.Null(sourceMetadata); // Source now empty
        Assert.NotNull(destMetadata); // Dest has key
    }
}
```

**Step 2: Verify RED**
```bash
dotnet build.cs test --filter "FullyQualifiedName~PivKeyOperationsTests"
```

**Step 3: Implement**
Create `PivSession.KeyPairs.cs` partial class:
- `GenerateKeyAsync()`: INS 0x47
  - Check version support (ROCA, P384, Cv25519, RSA3072/4096)
  - Build TLV with algorithm + policies
  - Parse response TAG 0x7F49 for public key
  - Return appropriate `IPublicKey` (RSAPublicKey, ECPublicKey, Curve25519PublicKey)
- `ImportKeyAsync()`: INS 0xFE
  - Extract key components based on algorithm
  - Zero private key after import!
- `MoveKeyAsync()`: INS 0xF6 (requires 5.7+)
- `DeleteKeyAsync()`: INS 0xF6 with dest=0xFF (requires 5.7+)
- `AttestKeyAsync()`: INS 0xF9 (requires 4.3+)

**Step 4: Verify GREEN**
```bash
dotnet build.cs test --filter "FullyQualifiedName~PivKeyOperationsTests"
```

**Step 5: Commit**
```bash
git add Yubico.YubiKit.Piv/src/PivSession.KeyPairs.cs \
        Yubico.YubiKit.Piv/tests/
git commit -m "feat(piv): add key generation, import, move, and attestation"
```

→ Output `<promise>PHASE_4_DONE</promise>`

---

## Phase 5: Cryptographic Operations

**User Story:** As a developer, I want to sign/decrypt data and perform ECDH so that I can use YubiKey for real cryptographic work.

**Files:**
- Create: `Yubico.YubiKit.Piv/src/PivSession.Crypto.cs`
- Test: `Yubico.YubiKit.Piv/tests/IntegrationTests/PivCryptoTests.cs`

**Step 1: Write failing tests**
```csharp
public class PivCryptoTests
{
    [Theory]
    [WithYubiKey]
    public async Task SignOrDecryptAsync_EccP256Sign_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(DefaultManagementKey);
        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Signature, 
            PivAlgorithm.EccP256,
            PivPinPolicy.Once);
        await session.VerifyPinAsync(DefaultPin);
        
        var dataToSign = SHA256.HashData("test data"u8);
        
        var signature = await session.SignOrDecryptAsync(
            PivSlot.Signature, 
            PivAlgorithm.EccP256, 
            dataToSign);
        
        Assert.NotEmpty(signature.ToArray());
        // Verify signature with public key
        var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(((ECPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(ecdsa.VerifyHash(dataToSign, signature.Span));
    }

    [Theory]
    [WithYubiKey]
    public async Task CalculateSecretAsync_ECDH_ProducesSharedSecret(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(DefaultManagementKey);
        var devicePublicKey = await session.GenerateKeyAsync(
            PivSlot.KeyManagement, 
            PivAlgorithm.EccP256);
        await session.VerifyPinAsync(DefaultPin);
        
        // Generate ephemeral key pair for peer
        using var peerKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var peerPublicKey = ECPublicKey.FromECDiffieHellman(peerKey);
        
        var sharedSecret = await session.CalculateSecretAsync(
            PivSlot.KeyManagement, 
            peerPublicKey);
        
        Assert.Equal(32, sharedSecret.Length); // P-256 x-coordinate is 32 bytes
    }
}
```

**Step 2: Verify RED**
```bash
dotnet build.cs test --filter "FullyQualifiedName~PivCryptoTests"
```

**Step 3: Implement**
Create `PivSession.Crypto.cs` partial class:
- `SignOrDecryptAsync()`: INS 0x87
  - TAG 0x82 (response) + TAG 0x81 (challenge) for sign/decrypt
  - Pad/truncate payload per key size
  - Handle PIN policy (may need PIN before each use)
- `CalculateSecretAsync()`: INS 0x87
  - TAG 0x82 (response) + TAG 0x85 (exponentiation)
  - Encode peer public key appropriately

**Step 4: Verify GREEN**
```bash
dotnet build.cs test --filter "FullyQualifiedName~PivCryptoTests"
```

**Step 5: Commit**
```bash
git add Yubico.YubiKit.Piv/src/PivSession.Crypto.cs \
        Yubico.YubiKit.Piv/tests/
git commit -m "feat(piv): add sign/decrypt and ECDH operations"
```

→ Output `<promise>PHASE_5_DONE</promise>`

---

## Phase 6: Certificates & Data Objects

**User Story:** As a developer, I want to store and retrieve X.509 certificates and PIV data objects.

**Files:**
- Create: `Yubico.YubiKit.Piv/src/PivSession.Certificates.cs`
- Create: `Yubico.YubiKit.Piv/src/PivSession.DataObjects.cs`
- Test: `Yubico.YubiKit.Piv/tests/IntegrationTests/PivCertificateTests.cs`

**Step 1: Write failing tests**
```csharp
public class PivCertificateTests
{
    [Theory]
    [WithYubiKey]
    public async Task StoreCertificateAsync_GetCertificateAsync_RoundTrip(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(DefaultManagementKey);
        await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);
        
        // Create self-signed cert
        var cert = CreateSelfSignedCertificate();
        
        await session.StoreCertificateAsync(PivSlot.Authentication, cert);
        var retrieved = await session.GetCertificateAsync(PivSlot.Authentication);
        
        Assert.NotNull(retrieved);
        Assert.Equal(cert.Thumbprint, retrieved.Thumbprint);
    }

    [Theory]
    [WithYubiKey]
    public async Task GetCertificateAsync_EmptySlot_ReturnsNull(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var cert = await session.GetCertificateAsync(PivSlot.Authentication);
        
        Assert.Null(cert);
    }

    [Theory]
    [WithYubiKey]
    public async Task DeleteCertificateAsync_IsIdempotent(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(DefaultManagementKey);
        
        // Delete twice should not throw
        await session.DeleteCertificateAsync(PivSlot.Authentication);
        await session.DeleteCertificateAsync(PivSlot.Authentication);
    }

    [Theory]
    [WithYubiKey]
    public async Task GetObjectAsync_EmptyObject_ReturnsEmpty(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var data = await session.GetObjectAsync(PivDataObject.Chuid);
        
        Assert.True(data.IsEmpty);
    }
}
```

**Step 2: Verify RED**
```bash
dotnet build.cs test --filter "FullyQualifiedName~PivCertificateTests"
```

**Step 3: Implement**
Create `PivSession.Certificates.cs`:
- `GetCertificateAsync()`: INS 0xCB (GET DATA)
  - Parse TAG 0x70 (cert) + TAG 0x71 (info)
  - Decompress if info byte = 0x01
  - Return null if not found (per PRD empty state handling)
- `StoreCertificateAsync()`: INS 0xDB (PUT DATA)
  - Compress with gzip if requested or cert > 1856 bytes
  - Build TLV: TAG 0x70 + TAG 0x71 + TAG 0xFE
- `DeleteCertificateAsync()`: PUT DATA with null (idempotent)

Create `PivSession.DataObjects.cs`:
- `GetObjectAsync()`: Generic GET DATA
- `PutObjectAsync()`: Generic PUT DATA

**Step 4: Verify GREEN**
```bash
dotnet build.cs test --filter "FullyQualifiedName~PivCertificateTests"
```

**Step 5: Commit**
```bash
git add Yubico.YubiKit.Piv/src/PivSession.Certificates.cs \
        Yubico.YubiKit.Piv/src/PivSession.DataObjects.cs \
        Yubico.YubiKit.Piv/tests/
git commit -m "feat(piv): add certificate and data object operations"
```

→ Output `<promise>PHASE_6_DONE</promise>`

---

## Phase 7: Metadata & Bio

**User Story:** As a developer, I want to retrieve metadata about slots and use biometric verification on supported devices.

**Files:**
- Create: `Yubico.YubiKit.Piv/src/PivSession.Metadata.cs`
- Create: `Yubico.YubiKit.Piv/src/PivSession.Bio.cs`
- Test: `Yubico.YubiKit.Piv/tests/IntegrationTests/PivMetadataTests.cs`

**Step 1: Write failing tests**
```csharp
public class PivMetadataTests
{
    [Theory]
    [WithYubiKey(MinimumFirmware = "5.3.0")]
    public async Task GetPinMetadataAsync_ReturnsValidMetadata(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var metadata = await session.GetPinMetadataAsync();
        
        Assert.True(metadata.IsDefault); // Default PIN after reset
        Assert.Equal(3, metadata.TotalRetries);
        Assert.Equal(3, metadata.RetriesRemaining);
    }

    [Theory]
    [WithYubiKey(MinimumFirmware = "5.3.0")]
    public async Task GetSlotMetadataAsync_EmptySlot_ReturnsNull(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var metadata = await session.GetSlotMetadataAsync(PivSlot.Authentication);
        
        Assert.Null(metadata);
    }

    [Theory]
    [WithYubiKey(MinimumFirmware = "5.3.0")]
    public async Task GetSlotMetadataAsync_WithKey_ReturnsMetadata(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(DefaultManagementKey);
        await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);
        
        var metadata = await session.GetSlotMetadataAsync(PivSlot.Authentication);
        
        Assert.NotNull(metadata);
        Assert.Equal(PivAlgorithm.EccP256, metadata.Value.Algorithm);
        Assert.True(metadata.Value.IsGenerated);
    }

    [Theory]
    [WithYubiKey]
    public async Task GetBioMetadataAsync_NonBioDevice_ThrowsNotSupported(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        
        // Most test devices won't be Bio
        await Assert.ThrowsAsync<NotSupportedException>(
            () => session.GetBioMetadataAsync());
    }
}
```

**Step 2: Verify RED**
```bash
dotnet build.cs test --filter "FullyQualifiedName~PivMetadataTests"
```

**Step 3: Implement**
Create `PivSession.Metadata.cs`:
- `GetPinMetadataAsync()`: INS 0xF7, P2=0x80
- `GetPukMetadataAsync()`: INS 0xF7, P2=0x81
- `GetManagementKeyMetadataAsync()`: INS 0xF7, P2=0x9B
- `GetSlotMetadataAsync()`: INS 0xF7, P2=slot
  - Return null if SW=0x6A82 (per PRD empty state)
- `SetManagementKeyAsync()`: INS 0xFF

Create `PivSession.Bio.cs`:
- `GetBioMetadataAsync()`: INS 0xF7, P2=0x96
  - Throw `NotSupportedException` if SW=0x6A82 or SW=0x6D00
- `VerifyUvAsync()`: INS 0x20, P2=0x96
  - Zero returned temp PIN reminder in docs!
- `VerifyTemporaryPinAsync()`: INS 0x20, P2=0x96, TAG 0x01

**Step 4: Verify GREEN**
```bash
dotnet build.cs test --filter "FullyQualifiedName~PivMetadataTests"
```

**Step 5: Commit**
```bash
git add Yubico.YubiKit.Piv/src/PivSession.Metadata.cs \
        Yubico.YubiKit.Piv/src/PivSession.Bio.cs \
        Yubico.YubiKit.Piv/tests/
git commit -m "feat(piv): add metadata retrieval and biometric support"
```

→ Output `<promise>PHASE_7_DONE</promise>`

---

## Phase 8: Reset & Final Integration

**User Story:** As a developer, I want to reset the PIV application and have all operations work together correctly.

**Files:**
- Update: `Yubico.YubiKit.Piv/src/PivSession.cs` (add ResetAsync)
- Create: `Yubico.YubiKit.Piv/tests/IntegrationTests/PivResetTests.cs`
- Create: `Yubico.YubiKit.Piv/tests/IntegrationTests/PivFullWorkflowTests.cs`

**Step 1: Write failing tests**
```csharp
public class PivResetTests
{
    [Theory]
    [WithYubiKey]
    public async Task ResetAsync_RestoresToDefaults(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        
        await session.ResetAsync();
        
        // Verify default state
        var pinMeta = await session.GetPinMetadataAsync();
        Assert.True(pinMeta.IsDefault);
        Assert.Equal(PivManagementKeyType.TripleDes, session.ManagementKeyType);
    }
}

public class PivFullWorkflowTests
{
    [Theory]
    [WithYubiKey]
    public async Task CompleteWorkflow_GenerateSignVerify(YubiKeyTestState state)
    {
        await using var session = await state.YubiKey.CreatePivSessionAsync();
        await session.ResetAsync();
        
        // 1. Authenticate
        await session.AuthenticateAsync(DefaultManagementKey);
        
        // 2. Generate key
        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Signature, 
            PivAlgorithm.EccP256,
            PivPinPolicy.Once);
        
        // 3. Store certificate
        var cert = CreateCertificate(publicKey);
        await session.StoreCertificateAsync(PivSlot.Signature, cert);
        
        // 4. Verify PIN
        await session.VerifyPinAsync(DefaultPin);
        
        // 5. Sign data
        var hash = SHA256.HashData("important document"u8);
        var signature = await session.SignOrDecryptAsync(
            PivSlot.Signature, 
            PivAlgorithm.EccP256, 
            hash);
        
        // 6. Verify signature with retrieved cert
        var storedCert = await session.GetCertificateAsync(PivSlot.Signature);
        using var ecdsa = storedCert!.GetECDsaPublicKey()!;
        Assert.True(ecdsa.VerifyHash(hash, signature.Span));
    }
}
```

**Step 2: Verify RED**
```bash
dotnet build.cs test --filter "FullyQualifiedName~PivResetTests|FullyQualifiedName~PivFullWorkflowTests"
```

**Step 3: Implement**
Add `ResetAsync()` to `PivSession.cs`:
- Check bio not configured (throw if it is)
- Block PIN by verifying with wrong PIN until locked
- Block PUK by unblocking with wrong PUK until locked
- Send INS 0xFB reset
- Refresh management key type

**Step 4: Verify GREEN**
```bash
dotnet build.cs test --filter "FullyQualifiedName~PivResetTests|FullyQualifiedName~PivFullWorkflowTests"
```

**Step 5: Commit**
```bash
git add Yubico.YubiKit.Piv/
git commit -m "feat(piv): add reset and complete integration tests"
```

→ Output `<promise>PHASE_8_DONE</promise>`

---

## Phase 9: Security Verification

**Required Checks (from security_audit.md):**

- [ ] All PIN/PUK/management key buffers zeroed after use
- [ ] Temporary PIN from bio zeroed after use (documented)
- [ ] No secrets in log statements
- [ ] Input validation on all public methods
- [ ] Constant-time operations where applicable

**Verification:**
```bash
# Check for ZeroMemory usage (should find multiple)
grep -rn "ZeroMemory" Yubico.YubiKit.Piv/src/

# Check for logging of sensitive data (should return nothing suspicious)
grep -rn "Log.*[Pp]in\|Log.*[Kk]ey\|Log.*[Pp]uk" Yubico.YubiKit.Piv/src/

# Check all try/finally for sensitive operations
grep -A5 "finally" Yubico.YubiKit.Piv/src/*.cs | grep -i "zero"
```

If any security issues found, fix and re-verify.

→ Output `<promise>PHASE_9_DONE</promise>`

---

## Phase 10: Documentation

**Files:**
- Update: `Yubico.YubiKit.Piv/CLAUDE.md`
- Update: `Yubico.YubiKit.Piv/README.md`

**Step 1: Update CLAUDE.md**
Update migration status to reflect completed implementation.

**Step 2: Create/Update README.md**
Add usage examples:
```csharp
// Basic signing workflow
await using var session = await yubiKey.CreatePivSessionAsync();
await session.AuthenticateAsync(managementKey);
var publicKey = await session.GenerateKeyAsync(PivSlot.Signature, PivAlgorithm.EccP256);
await session.VerifyPinAsync(pin);
var signature = await session.SignOrDecryptAsync(PivSlot.Signature, PivAlgorithm.EccP256, hash);
```

**Step 3: Commit**
```bash
git add Yubico.YubiKit.Piv/CLAUDE.md Yubico.YubiKit.Piv/README.md
git commit -m "docs(piv): update documentation with usage examples"
```

→ Output `<promise>PHASE_10_DONE</promise>`

---

## Verification Requirements (MUST PASS BEFORE COMPLETION)

1. **Build:** `dotnet build.cs build` (must exit 0)
2. **All Tests:** `dotnet build.cs test` (all tests must pass)
3. **No Regressions:** Existing tests in other modules still pass
4. **Security Checks:** All Phase 9 verifications pass
5. **Documentation:** CLAUDE.md and README.md updated

**Verification commands:**
```bash
# Full build
dotnet build.cs build

# All tests
dotnet build.cs test

# PIV-specific tests
dotnet build.cs test --filter "FullyQualifiedName~Yubico.YubiKit.Piv"
```

Only after ALL pass, output `<promise>PIV_SESSION_PORT_COMPLETE</promise>`.
If any fail, fix and re-verify.

---

## Handoff

```bash
# Start Ralph Loop autonomous execution
bun .claude/skills/agent-ralph-loop/ralph-loop.ts \
  --prompt-file ./docs/plans/ralph-loop/2026-01-18-piv-session-port.md \
  --completion-promise "PIV_SESSION_PORT_COMPLETE" \
  --max-iterations 50 \
  --learn \
  --model claude-sonnet-4
```

**Notes:**
- Using 50 iterations due to 10 phases
- Test device is YubiKey 5.8.0-alpha - all features available
- Get firmware version via ManagementSession (FIDO reports 0.0.1 incorrectly)
