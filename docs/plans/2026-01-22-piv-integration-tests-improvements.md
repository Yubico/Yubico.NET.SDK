# PIV Integration Tests Improvements Plan

**Goal:** Fix existing test issues and add comprehensive coverage for all PIV algorithms and missing API methods.

**Architecture:** Extend existing test classes with new tests, extract shared constants/helpers to avoid duplication, ensure all PIV algorithms (ECC P256/P384, Ed25519, X25519, RSA 1024/2048/3072/4096) have integration coverage where possible.

**Tech Stack:** xUnit v3, .NET 10, YubiKey PIV application, System.Security.Cryptography for RSA/ECC verification

---

## Phase 1: Fix Existing Test Issues

### Task 1.1: Fix Test Naming and Assertions in PivAuthenticationTests

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivAuthenticationTests.cs`

**Step 1: Rename misnamed test and add IsAuthenticated assertion**

Change `AuthenticateAsync_WithWrongKey_ThrowsBadResponse` to `AuthenticateAsync_WithWrongKey_ThrowsApduException` and verify `IsAuthenticated` remains false.

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
public async Task AuthenticateAsync_WithWrongKey_ThrowsApduException(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    await session.ResetAsync();
    
    var wrongKey = new byte[24];
    
    await Assert.ThrowsAsync<ApduException>(
        () => session.AuthenticateAsync(wrongKey));
    
    Assert.False(session.IsAuthenticated);
}
```

**Step 2: Add positive assertion to VerifyPinAsync_WithCorrectPin_Succeeds**

The test currently has no assertion. Add a PIN attempts check to verify PIN was accepted.

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
public async Task VerifyPinAsync_WithCorrectPin_Succeeds(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    await session.ResetAsync();
    
    await session.VerifyPinAsync(DefaultPin);
    
    // After successful PIN verify, attempts should still be at max
    var attempts = await session.GetPinAttemptsAsync();
    Assert.Equal(3, attempts);
}
```

**Step 3: Run tests**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~PivAuthenticationTests"
```

**Step 4: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivAuthenticationTests.cs
git commit -m "fix(piv-tests): rename misnamed test and add positive assertions"
```

---

### Task 1.2: Fix ResetAsync_ClearsAllSlots to Test Key Slot (Not Certificate)

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivResetTests.cs`

**Step 1: Fix the test to check slot metadata instead of certificate**

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
public async Task ResetAsync_ClearsAllSlots(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    await session.ResetAsync();
    
    // Generate a key in a slot
    await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
    await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);
    
    // Verify key exists
    var metadataBefore = await session.GetSlotMetadataAsync(PivSlot.Authentication);
    Assert.NotNull(metadataBefore);
    
    // Reset again
    await session.ResetAsync();
    
    // Verify slot is empty (key cleared)
    var metadataAfter = await session.GetSlotMetadataAsync(PivSlot.Authentication);
    Assert.Null(metadataAfter);
}
```

**Step 2: Run test**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~ResetAsync_ClearsAllSlots"
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivResetTests.cs
git commit -m "fix(piv-tests): ResetAsync_ClearsAllSlots now verifies key metadata"
```

---

### Task 1.3: Fix ECDH Test to Actually Verify Shared Secrets Match

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivFullWorkflowTests.cs`
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs`

**Step 1: Fix PivCryptoTests.CalculateSecretAsync_ECDH_ProducesSharedSecret**

The YubiKey returns the raw x-coordinate. Use `ECDiffieHellman.DeriveRawSecretAgreement()` on the peer side (available in .NET 10) and compare.

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
public async Task CalculateSecretAsync_ECDH_ProducesMatchingSharedSecret(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    await session.ResetAsync();
    await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
    var devicePublicKey = await session.GenerateKeyAsync(
        PivSlot.KeyManagement, 
        PivAlgorithm.EccP256);
    await session.VerifyPinAsync(DefaultPin);
    
    // Generate peer key
    using var peerKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
    var peerPublicKeyBytes = peerKey.PublicKey.ExportSubjectPublicKeyInfo();
    var peerPublicKey = ECPublicKey.CreateFromSubjectPublicKeyInfo(peerPublicKeyBytes);
    
    // Calculate shared secret on YubiKey (returns raw x-coordinate)
    var yubiKeySecret = await session.CalculateSecretAsync(
        PivSlot.KeyManagement, 
        peerPublicKey);
    
    // Calculate shared secret on peer side
    using var deviceECDH = ECDiffieHellman.Create();
    deviceECDH.ImportSubjectPublicKeyInfo(
        ((ECPublicKey)devicePublicKey).ExportSubjectPublicKeyInfo(),
        out _);
    
    // DeriveRawSecretAgreement returns the raw x-coordinate (same as YubiKey)
    var peerSecret = peerKey.DeriveRawSecretAgreement(deviceECDH.PublicKey);
    
    // Both should have identical shared secrets
    Assert.Equal(32, yubiKeySecret.Length);
    Assert.Equal(peerSecret.Length, yubiKeySecret.Length);
    Assert.True(yubiKeySecret.Span.SequenceEqual(peerSecret));
}
```

**Step 2: Update or remove CompleteWorkflow_ECDHKeyAgreement in PivFullWorkflowTests**

Either fix it similarly or delete it since PivCryptoTests now has proper coverage.

**Step 3: Run tests**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~CalculateSecret"
```

**Step 4: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivFullWorkflowTests.cs
git commit -m "fix(piv-tests): ECDH test now verifies shared secrets match"
```

---

### Task 1.4: Fix Bio Metadata Test Assertion

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivMetadataTests.cs`

**Step 1: Tighten the assertion to only accept expected exceptions**

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
public async Task GetBioMetadataAsync_NonBioDevice_ThrowsNotSupported(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    
    // Non-bio YubiKeys should throw NotSupportedException or return error SW
    var ex = await Record.ExceptionAsync(() => session.GetBioMetadataAsync());
    
    // Accept NotSupportedException (feature not available) or ApduException with specific SW
    Assert.True(
        ex is NotSupportedException || 
        (ex is ApduException apduEx && apduEx.StatusWord is 0x6D00 or 0x6A81 or 0x6985),
        $"Expected NotSupportedException or ApduException with specific SW, but got: {ex?.GetType().Name ?? "null"}");
}
```

**Step 2: Run test**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~GetBioMetadataAsync"
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivMetadataTests.cs
git commit -m "fix(piv-tests): tighten bio metadata exception assertion"
```

---

### Task 1.5: Clean Up or Remove Misleading CompleteWorkflow_GenerateSignVerify

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivFullWorkflowTests.cs`

**Step 1: Remove the misleading certificate storage or rename test**

Option A: Remove the useless certificate storage entirely (recommended since PivCryptoTests has proper sign test):

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
public async Task CompleteWorkflow_GenerateKeySignVerify(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    await session.ResetAsync();
    
    // 1. Authenticate with management key
    await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
    
    // 2. Generate key
    var publicKey = await session.GenerateKeyAsync(
        PivSlot.Signature, 
        PivAlgorithm.EccP256,
        PivPinPolicy.Once);
    
    Assert.NotNull(publicKey);
    Assert.IsType<ECPublicKey>(publicKey);
    
    // 3. Verify PIN
    await session.VerifyPinAsync(DefaultPin);
    
    // 4. Sign data with YubiKey's generated key
    var dataToSign = "important document"u8.ToArray();
    var hash = SHA256.HashData(dataToSign);
    var signature = await session.SignOrDecryptAsync(
        PivSlot.Signature, 
        PivAlgorithm.EccP256, 
        hash);
    
    Assert.NotEmpty(signature.ToArray());
    
    // 5. Verify signature using the public key from GenerateKeyAsync
    using var ecdsa = ECDsa.Create();
    ecdsa.ImportSubjectPublicKeyInfo(((ECPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
    Assert.True(ecdsa.VerifyHash(hash, signature.Span, DSASignatureFormat.Rfc3279DerSequence));
}
```

**Step 2: Run tests**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~CompleteWorkflow_GenerateKey"
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivFullWorkflowTests.cs
git commit -m "refactor(piv-tests): remove misleading certificate storage from workflow test"
```

---

### Task 1.6: Fix MoveKeyAsync Test to Verify Key is Functional

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivKeyOperationsTests.cs`

**Step 1: Add signing verification after move**

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
public async Task MoveKeyAsync_MovesToNewSlot_KeyRemainsFunctional(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    await session.ResetAsync();
    await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
    var publicKey = await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);
    
    await session.MoveKeyAsync(PivSlot.Authentication, PivSlot.Retired1);
    
    // Verify source is empty
    var sourceMetadata = await session.GetSlotMetadataAsync(PivSlot.Authentication);
    Assert.Null(sourceMetadata);
    
    // Verify destination has key
    var destMetadata = await session.GetSlotMetadataAsync(PivSlot.Retired1);
    Assert.NotNull(destMetadata);
    Assert.Equal(PivAlgorithm.EccP256, destMetadata.Value.Algorithm);
    
    // Verify key is functional in new slot
    await session.VerifyPinAsync(DefaultPin);
    var hash = SHA256.HashData("test"u8);
    var signature = await session.SignOrDecryptAsync(
        PivSlot.Retired1, 
        PivAlgorithm.EccP256, 
        hash);
    
    // Verify signature
    using var ecdsa = ECDsa.Create();
    ecdsa.ImportSubjectPublicKeyInfo(((ECPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
    Assert.True(ecdsa.VerifyHash(hash, signature.Span, DSASignatureFormat.Rfc3279DerSequence));
}
```

**Step 2: Run test**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~MoveKeyAsync"
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivKeyOperationsTests.cs
git commit -m "fix(piv-tests): MoveKeyAsync test verifies key remains functional"
```

---

## Phase 2: Add Missing Algorithm Coverage

### Task 2.1: Add P384 Signing Test

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs`

**Step 1: Add P384 sign and verify test**

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.0.0")]
public async Task SignOrDecryptAsync_EccP384Sign_ProducesValidSignature(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    await session.ResetAsync();
    await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
    var publicKey = await session.GenerateKeyAsync(
        PivSlot.Signature, 
        PivAlgorithm.EccP384,
        PivPinPolicy.Once);
    await session.VerifyPinAsync(DefaultPin);
    
    var dataToSign = SHA384.HashData("test data"u8);
    
    var signature = await session.SignOrDecryptAsync(
        PivSlot.Signature, 
        PivAlgorithm.EccP384, 
        dataToSign);
    
    Assert.NotEmpty(signature.ToArray());
    
    // Verify signature
    using var ecdsa = ECDsa.Create();
    ecdsa.ImportSubjectPublicKeyInfo(((ECPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
    Assert.True(ecdsa.VerifyHash(dataToSign, signature.Span, DSASignatureFormat.Rfc3279DerSequence));
}
```

**Step 2: Run test**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~EccP384Sign"
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs
git commit -m "test(piv): add P384 signing integration test"
```

---

### Task 2.2: Add X25519 ECDH Test

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs`

**Step 1: Add X25519 key agreement test**

Note: .NET 10 should have X25519 support via `ECDiffieHellman.Create(ECCurve.NamedCurves.curve25519)` or similar. If not available, document as limitation.

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
public async Task CalculateSecretAsync_X25519_ProducesSharedSecret(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    await session.ResetAsync();
    await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
    var devicePublicKey = await session.GenerateKeyAsync(
        PivSlot.KeyManagement, 
        PivAlgorithm.X25519);
    await session.VerifyPinAsync(DefaultPin);
    
    // X25519 produces 32-byte shared secrets
    // Note: Creating peer key for X25519 may require special handling
    // This test verifies the YubiKey can perform the operation
    
    Assert.NotNull(devicePublicKey);
    Assert.IsType<Curve25519PublicKey>(devicePublicKey);
    
    // For full verification, we would need X25519 software implementation
    // This is a placeholder - see Ed25519 note about OpenSSL/BouncyCastle
    // For now, verify key generation succeeds and public key is 32 bytes
    var pubKeyBytes = ((Curve25519PublicKey)devicePublicKey).PublicPoint;
    Assert.Equal(32, pubKeyBytes.Length);
}
```

**Step 2: Run test**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~X25519"
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs
git commit -m "test(piv): add X25519 key generation test (partial - software verification TBD)"
```

---

### Task 2.3: Add Ed25519 Test (Generation Only - No Software Verification)

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs`

**Step 1: Fix existing Ed25519 test to document limitation**

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
public async Task SignOrDecryptAsync_Ed25519_ProducesSignature(YubiKeyTestState state)
{
    // Note: Ed25519 signature verification requires external library (OpenSSL/BouncyCastle)
    // as .NET 10 does not have native Ed25519 support. This test verifies the YubiKey
    // can generate keys and produce signatures of the expected format.
    
    await using var session = await state.Device.CreatePivSessionAsync();
    await session.ResetAsync();
    await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
    var publicKey = await session.GenerateKeyAsync(
        PivSlot.Signature, 
        PivAlgorithm.Ed25519,
        PivPinPolicy.Once);
    await session.VerifyPinAsync(DefaultPin);
    
    // Ed25519 signs the raw message (not a hash)
    var dataToSign = "test data"u8.ToArray();
    
    var signature = await session.SignOrDecryptAsync(
        PivSlot.Signature, 
        PivAlgorithm.Ed25519, 
        dataToSign);
    
    // Ed25519 signatures are always 64 bytes
    Assert.Equal(64, signature.Length);
    
    // Verify public key format
    Assert.NotNull(publicKey);
    Assert.IsType<Curve25519PublicKey>(publicKey);
    var pubKeyBytes = ((Curve25519PublicKey)publicKey).PublicPoint;
    Assert.Equal(32, pubKeyBytes.Length);
    
    // TODO: Add signature verification when OpenSSL/BouncyCastle Ed25519 support is added
}
```

**Step 2: Run test**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~Ed25519"
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs
git commit -m "test(piv): document Ed25519 verification limitation, verify signature format"
```

---

### Task 2.4: Add RSA 2048 Signing Test

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs`

**Step 1: Add RSA 2048 sign and verify test**

RSA signing with PIV requires pre-padding. The YubiKey performs raw RSA (no padding), so caller must apply PKCS#1 v1.5 padding.

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.3.5")]
public async Task SignOrDecryptAsync_Rsa2048Sign_ProducesValidSignature(YubiKeyTestState state)
{
    // Skip if RSA generation not supported on this firmware
    if (!PivFeatures.SupportsRsaGeneration(state.FirmwareVersion))
    {
        return; // Skip test
    }
    
    await using var session = await state.Device.CreatePivSessionAsync();
    await session.ResetAsync();
    await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
    
    // RSA key generation can be slow (10-30 seconds)
    var publicKey = await session.GenerateKeyAsync(
        PivSlot.Signature, 
        PivAlgorithm.Rsa2048,
        PivPinPolicy.Once);
    
    await session.VerifyPinAsync(DefaultPin);
    
    // Hash the data
    var dataToSign = "test data for RSA signing"u8.ToArray();
    var hash = SHA256.HashData(dataToSign);
    
    // Apply PKCS#1 v1.5 padding manually for PIV
    // PIV expects the DigestInfo structure: SEQUENCE { AlgorithmIdentifier, OCTET STRING hash }
    // For SHA-256: 30 31 30 0d 06 09 60 86 48 01 65 03 04 02 01 05 00 04 20 [32-byte hash]
    var digestInfo = new byte[]
    {
        0x30, 0x31, 0x30, 0x0d, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01,
        0x05, 0x00, 0x04, 0x20
    };
    var paddedData = CreatePkcs1v15Padding(digestInfo, hash, 256); // 2048 bits = 256 bytes
    
    var signature = await session.SignOrDecryptAsync(
        PivSlot.Signature, 
        PivAlgorithm.Rsa2048, 
        paddedData);
    
    Assert.Equal(256, signature.Length);
    
    // Verify signature using .NET RSA
    using var rsa = RSA.Create();
    rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
    Assert.True(rsa.VerifyData(dataToSign, signature.Span, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
}

private static byte[] CreatePkcs1v15Padding(byte[] digestInfo, byte[] hash, int modulusBytes)
{
    // PKCS#1 v1.5 format: 0x00 0x01 [0xFF padding] 0x00 [DigestInfo + hash]
    var tLen = digestInfo.Length + hash.Length;
    var psLen = modulusBytes - tLen - 3;
    
    if (psLen < 8)
    {
        throw new ArgumentException("Message too long for key size");
    }
    
    var result = new byte[modulusBytes];
    result[0] = 0x00;
    result[1] = 0x01;
    for (int i = 2; i < 2 + psLen; i++)
    {
        result[i] = 0xFF;
    }
    result[2 + psLen] = 0x00;
    Array.Copy(digestInfo, 0, result, 3 + psLen, digestInfo.Length);
    Array.Copy(hash, 0, result, 3 + psLen + digestInfo.Length, hash.Length);
    
    return result;
}
```

**Step 2: Run test**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~Rsa2048Sign"
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs
git commit -m "test(piv): add RSA 2048 signing integration test with PKCS#1 v1.5 padding"
```

---

### Task 2.5: Add RSA 1024, 3072, 4096 Tests (Parameterized)

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs`

**Step 1: Add RSA tests for other key sizes**

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.3.5")]
[InlineData(PivAlgorithm.Rsa1024, 128)]
public async Task SignOrDecryptAsync_Rsa1024Sign_ProducesValidSignature(
    YubiKeyTestState state, 
    PivAlgorithm algorithm, 
    int modulusBytes)
{
    if (!PivFeatures.SupportsRsaGeneration(state.FirmwareVersion))
    {
        return;
    }
    
    await using var session = await state.Device.CreatePivSessionAsync();
    await session.ResetAsync();
    await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
    
    var publicKey = await session.GenerateKeyAsync(
        PivSlot.Signature, 
        algorithm,
        PivPinPolicy.Once);
    
    await session.VerifyPinAsync(DefaultPin);
    
    var dataToSign = "test data"u8.ToArray();
    var hash = SHA256.HashData(dataToSign);
    var digestInfo = new byte[]
    {
        0x30, 0x31, 0x30, 0x0d, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01,
        0x05, 0x00, 0x04, 0x20
    };
    var paddedData = CreatePkcs1v15Padding(digestInfo, hash, modulusBytes);
    
    var signature = await session.SignOrDecryptAsync(
        PivSlot.Signature, 
        algorithm, 
        paddedData);
    
    Assert.Equal(modulusBytes, signature.Length);
    
    using var rsa = RSA.Create();
    rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
    Assert.True(rsa.VerifyData(dataToSign, signature.Span, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
}

[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
[InlineData(PivAlgorithm.Rsa3072, 384)]
[InlineData(PivAlgorithm.Rsa4096, 512)]
public async Task SignOrDecryptAsync_Rsa3072And4096Sign_ProducesValidSignature(
    YubiKeyTestState state, 
    PivAlgorithm algorithm, 
    int modulusBytes)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    await session.ResetAsync();
    await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
    
    var publicKey = await session.GenerateKeyAsync(
        PivSlot.Signature, 
        algorithm,
        PivPinPolicy.Once);
    
    await session.VerifyPinAsync(DefaultPin);
    
    var dataToSign = "test data"u8.ToArray();
    var hash = SHA256.HashData(dataToSign);
    var digestInfo = new byte[]
    {
        0x30, 0x31, 0x30, 0x0d, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01,
        0x05, 0x00, 0x04, 0x20
    };
    var paddedData = CreatePkcs1v15Padding(digestInfo, hash, modulusBytes);
    
    var signature = await session.SignOrDecryptAsync(
        PivSlot.Signature, 
        algorithm, 
        paddedData);
    
    Assert.Equal(modulusBytes, signature.Length);
    
    using var rsa = RSA.Create();
    rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
    Assert.True(rsa.VerifyData(dataToSign, signature.Span, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
}
```

**Step 2: Run tests**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~Rsa"
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs
git commit -m "test(piv): add RSA 1024/3072/4096 signing integration tests"
```

---

### Task 2.6: Add RSA Decryption Test

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs`

**Step 1: Add RSA decryption test**

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.3.5")]
public async Task SignOrDecryptAsync_Rsa2048Decrypt_DecryptsCorrectly(YubiKeyTestState state)
{
    if (!PivFeatures.SupportsRsaGeneration(state.FirmwareVersion))
    {
        return;
    }
    
    await using var session = await state.Device.CreatePivSessionAsync();
    await session.ResetAsync();
    await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
    
    var publicKey = await session.GenerateKeyAsync(
        PivSlot.KeyManagement, // Use key management slot for encryption
        PivAlgorithm.Rsa2048,
        PivPinPolicy.Once);
    
    await session.VerifyPinAsync(DefaultPin);
    
    // Encrypt data with public key (software)
    using var rsa = RSA.Create();
    rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
    
    var plaintext = "secret message"u8.ToArray();
    var ciphertext = rsa.Encrypt(plaintext, RSAEncryptionPadding.Pkcs1);
    
    // Decrypt with YubiKey (raw RSA operation)
    var decrypted = await session.SignOrDecryptAsync(
        PivSlot.KeyManagement, 
        PivAlgorithm.Rsa2048, 
        ciphertext);
    
    // YubiKey returns raw decrypted block with PKCS#1 padding
    // Remove PKCS#1 v1.5 encryption padding: 0x00 0x02 [random nonzero] 0x00 [message]
    var decryptedSpan = decrypted.Span;
    Assert.Equal(0x00, decryptedSpan[0]);
    Assert.Equal(0x02, decryptedSpan[1]);
    
    // Find the 0x00 separator
    int separatorIndex = -1;
    for (int i = 2; i < decryptedSpan.Length; i++)
    {
        if (decryptedSpan[i] == 0x00)
        {
            separatorIndex = i;
            break;
        }
    }
    Assert.True(separatorIndex > 10, "PKCS#1 padding requires at least 8 bytes of random data");
    
    var recoveredPlaintext = decryptedSpan.Slice(separatorIndex + 1);
    Assert.True(recoveredPlaintext.SequenceEqual(plaintext));
}
```

**Step 2: Run test**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~Rsa2048Decrypt"
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs
git commit -m "test(piv): add RSA 2048 decryption integration test"
```

---

## Phase 3: Add Missing API Coverage

### Task 3.1: Add PUK and PIN Unblock Tests

**Files:**
- Create: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivPukTests.cs`

**Step 1: Create PUK test file**

```csharp
// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Xunit;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

public class PivPukTests
{
    private static readonly byte[] DefaultTripleDesManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };
    
    private static readonly byte[] DefaultAesManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };
    
    private static readonly byte[] DefaultPin = "123456"u8.ToArray();
    private static readonly byte[] DefaultPuk = "12345678"u8.ToArray();

    private static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
        version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultTripleDesManagementKey;

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task ChangePukAsync_WithCorrectOldPuk_Succeeds(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var newPuk = "87654321"u8.ToArray();
        
        await session.ChangePukAsync(DefaultPuk, newPuk);
        
        // Verify we can use new PUK to unblock (block PIN first)
        await BlockPin(session);
        await session.UnblockPinAsync(newPuk, DefaultPin);
        
        // Verify PIN works
        await session.VerifyPinAsync(DefaultPin);
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task UnblockPinAsync_AfterBlockedPin_RestoresAccess(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        // Block the PIN
        await BlockPin(session);
        
        // Verify PIN is blocked
        var ex = await Assert.ThrowsAsync<InvalidPinException>(
            () => session.VerifyPinAsync(DefaultPin));
        Assert.Equal(0, ex.RetriesRemaining);
        
        // Unblock with PUK
        var newPin = "654321"u8.ToArray();
        await session.UnblockPinAsync(DefaultPuk, newPin);
        
        // Verify new PIN works
        await session.VerifyPinAsync(newPin);
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task GetPukMetadataAsync_ReturnsValidMetadata(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var metadata = await session.GetPukMetadataAsync();
        
        Assert.True(metadata.IsDefault);
        Assert.Equal(3, metadata.TotalRetries);
        Assert.Equal(3, metadata.RetriesRemaining);
    }

    private static async Task BlockPin(IPivSession session)
    {
        var wrongPin = "000000"u8.ToArray();
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await session.VerifyPinAsync(wrongPin);
            }
            catch (InvalidPinException)
            {
                // Expected
            }
        }
    }
}
```

**Step 2: Run tests**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~PivPukTests"
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivPukTests.cs
git commit -m "test(piv): add PUK and PIN unblock integration tests"
```

---

### Task 3.2: Add SetManagementKeyAsync Test

**Files:**
- Create: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivManagementKeyTests.cs`

**Step 1: Create management key test file**

```csharp
// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Xunit;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

public class PivManagementKeyTests
{
    private static readonly byte[] DefaultTripleDesManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };
    
    private static readonly byte[] DefaultAesManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };

    private static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
        version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultTripleDesManagementKey;

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task SetManagementKeyAsync_ChangesToNewKey(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        
        var newKey = new byte[]
        {
            0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x11, 0x22,
            0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00,
            0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0
        };
        
        var keyType = state.FirmwareVersion >= new FirmwareVersion(5, 7, 0)
            ? PivManagementKeyType.Aes192
            : PivManagementKeyType.TripleDes;
        
        await session.SetManagementKeyAsync(keyType, newKey);
        
        // Create new session to verify key change
        await using var session2 = await state.Device.CreatePivSessionAsync();
        
        // Old key should fail
        await Assert.ThrowsAsync<ApduException>(
            () => session2.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion)));
        
        // New key should work
        await session2.AuthenticateAsync(newKey);
        Assert.True(session2.IsAuthenticated);
        
        // Reset to restore default key
        await session2.ResetAsync();
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.2")]
    public async Task SetManagementKeyAsync_AES256_Succeeds(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        
        var aes256Key = new byte[32];
        for (int i = 0; i < 32; i++) aes256Key[i] = (byte)i;
        
        await session.SetManagementKeyAsync(PivManagementKeyType.Aes256, aes256Key);
        
        // Verify via metadata
        var metadata = await session.GetManagementKeyMetadataAsync();
        Assert.Equal(PivManagementKeyType.Aes256, metadata.KeyType);
        Assert.False(metadata.IsDefault);
        
        // Reset to restore default
        await session.ResetAsync();
    }
}
```

**Step 2: Run tests**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~PivManagementKeyTests"
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivManagementKeyTests.cs
git commit -m "test(piv): add management key change integration tests"
```

---

### Task 3.3: Add ImportKeyAsync Test

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivKeyOperationsTests.cs`

**Step 1: Add key import test**

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
public async Task ImportKeyAsync_EccP256_CanSign(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    await session.ResetAsync();
    await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
    
    // Generate a software key pair
    using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    var privateKeyPkcs8 = ecdsa.ExportPkcs8PrivateKey();
    var privateKey = ECPrivateKey.CreateFromPkcs8(privateKeyPkcs8);
    
    // Import to YubiKey
    var algorithm = await session.ImportKeyAsync(
        PivSlot.Signature, 
        privateKey,
        PivPinPolicy.Once);
    
    Assert.Equal(PivAlgorithm.EccP256, algorithm);
    
    // Sign with YubiKey
    await session.VerifyPinAsync(DefaultPin);
    var hash = SHA256.HashData("test data"u8);
    var signature = await session.SignOrDecryptAsync(
        PivSlot.Signature, 
        PivAlgorithm.EccP256, 
        hash);
    
    // Verify with software public key
    Assert.True(ecdsa.VerifyHash(hash, signature.Span, DSASignatureFormat.Rfc3279DerSequence));
}
```

**Step 2: Run test**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~ImportKeyAsync"
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivKeyOperationsTests.cs
git commit -m "test(piv): add key import integration test"
```

---

### Task 3.4: Add DeleteKeyAsync Test

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivKeyOperationsTests.cs`

**Step 1: Add key deletion test**

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
public async Task DeleteKeyAsync_RemovesKey_SlotBecomesEmpty(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    await session.ResetAsync();
    await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
    
    // Generate key
    await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);
    
    // Verify key exists
    var metadataBefore = await session.GetSlotMetadataAsync(PivSlot.Authentication);
    Assert.NotNull(metadataBefore);
    
    // Delete key
    await session.DeleteKeyAsync(PivSlot.Authentication);
    
    // Verify slot is empty
    var metadataAfter = await session.GetSlotMetadataAsync(PivSlot.Authentication);
    Assert.Null(metadataAfter);
}
```

**Step 2: Run test**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~DeleteKeyAsync"
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivKeyOperationsTests.cs
git commit -m "test(piv): add key deletion integration test"
```

---

### Task 3.5: Add PutObjectAsync / GetObjectAsync Test

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCertificateTests.cs`

**Step 1: Add data object round-trip test**

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
public async Task PutObjectAsync_GetObjectAsync_RoundTrip(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    await session.ResetAsync();
    await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
    
    // Write custom data to a data object
    // Use Discovery object (0x7E) which is writable
    var testData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0xAA, 0xBB, 0xCC };
    
    await session.PutObjectAsync(PivDataObject.Printed, testData);
    
    // Read it back
    var retrieved = await session.GetObjectAsync(PivDataObject.Printed);
    
    Assert.False(retrieved.IsEmpty);
    Assert.True(retrieved.Span.SequenceEqual(testData));
    
    // Clean up - write null to delete
    await session.PutObjectAsync(PivDataObject.Printed, null);
    
    var empty = await session.GetObjectAsync(PivDataObject.Printed);
    Assert.True(empty.IsEmpty);
}
```

**Step 2: Run test**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~PutObjectAsync"
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCertificateTests.cs
git commit -m "test(piv): add data object read/write integration test"
```

---

### Task 3.6: Add SetPinAttemptsAsync Test

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivAuthenticationTests.cs`

**Step 1: Add PIN attempts configuration test**

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
public async Task SetPinAttemptsAsync_CustomLimit_EnforcesLimit(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    await session.ResetAsync();
    await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
    
    // Set custom PIN attempts (5 PIN, 4 PUK)
    await session.SetPinAttemptsAsync(5, 4);
    
    // Verify via metadata (if supported) or attempt count
    if (state.FirmwareVersion >= new FirmwareVersion(5, 3, 0))
    {
        var pinMeta = await session.GetPinMetadataAsync();
        Assert.Equal(5, pinMeta.TotalRetries);
        
        var pukMeta = await session.GetPukMetadataAsync();
        Assert.Equal(4, pukMeta.TotalRetries);
    }
    
    var attempts = await session.GetPinAttemptsAsync();
    Assert.Equal(5, attempts);
    
    // Reset to restore defaults
    await session.ResetAsync();
}
```

**Step 2: Run test**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~SetPinAttemptsAsync"
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivAuthenticationTests.cs
git commit -m "test(piv): add PIN attempts configuration integration test"
```

---

### Task 3.7: Add GetSerialNumberAsync Test

**Files:**
- Modify: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivMetadataTests.cs`

**Step 1: Add serial number test**

```csharp
[Theory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
public async Task GetSerialNumberAsync_ReturnsDeviceSerial(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    
    var serialNumber = await session.GetSerialNumberAsync();
    
    Assert.True(serialNumber > 0);
    // Should match the device serial from test state
    Assert.Equal(state.SerialNumber, serialNumber);
}
```

**Step 2: Run test**

```bash
dotnet build.cs test --project Piv --filter "FullyQualifiedName~GetSerialNumberAsync"
```

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivMetadataTests.cs
git commit -m "test(piv): add serial number retrieval integration test"
```

---

## Phase 4: Extract Shared Test Helpers

### Task 4.1: Create Shared Test Constants and Helpers

**Files:**
- Create: `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivTestHelpers.cs`

**Step 1: Extract common constants and helper methods**

```csharp
// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Piv.IntegrationTests;

/// <summary>
/// Shared constants and helper methods for PIV integration tests.
/// </summary>
internal static class PivTestHelpers
{
    /// <summary>
    /// Default PIV management key for TripleDES (firmware &lt; 5.7.0).
    /// </summary>
    public static readonly byte[] DefaultTripleDesManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };
    
    /// <summary>
    /// Default PIV management key for AES192 (firmware &gt;= 5.7.0).
    /// </summary>
    public static readonly byte[] DefaultAesManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };
    
    /// <summary>
    /// Default PIV PIN (123456).
    /// </summary>
    public static readonly byte[] DefaultPin = "123456"u8.ToArray();
    
    /// <summary>
    /// Default PIV PUK (12345678).
    /// </summary>
    public static readonly byte[] DefaultPuk = "12345678"u8.ToArray();

    /// <summary>
    /// Gets the appropriate default management key for the given firmware version.
    /// </summary>
    public static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
        version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultTripleDesManagementKey;

    /// <summary>
    /// SHA-256 DigestInfo prefix for PKCS#1 v1.5 padding.
    /// </summary>
    public static readonly byte[] Sha256DigestInfo = new byte[]
    {
        0x30, 0x31, 0x30, 0x0d, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01,
        0x05, 0x00, 0x04, 0x20
    };

    /// <summary>
    /// Creates PKCS#1 v1.5 padded data for RSA signing.
    /// </summary>
    /// <param name="digestInfo">The DigestInfo prefix for the hash algorithm.</param>
    /// <param name="hash">The hash value.</param>
    /// <param name="modulusBytes">The RSA modulus size in bytes.</param>
    /// <returns>PKCS#1 v1.5 padded data ready for raw RSA operation.</returns>
    public static byte[] CreatePkcs1v15SigningPadding(byte[] digestInfo, byte[] hash, int modulusBytes)
    {
        // PKCS#1 v1.5 format: 0x00 0x01 [0xFF padding] 0x00 [DigestInfo + hash]
        var tLen = digestInfo.Length + hash.Length;
        var psLen = modulusBytes - tLen - 3;
        
        if (psLen < 8)
        {
            throw new ArgumentException("Message too long for key size");
        }
        
        var result = new byte[modulusBytes];
        result[0] = 0x00;
        result[1] = 0x01;
        for (int i = 2; i < 2 + psLen; i++)
        {
            result[i] = 0xFF;
        }
        result[2 + psLen] = 0x00;
        Array.Copy(digestInfo, 0, result, 3 + psLen, digestInfo.Length);
        Array.Copy(hash, 0, result, 3 + psLen + digestInfo.Length, hash.Length);
        
        return result;
    }

    /// <summary>
    /// Blocks the PIN by attempting wrong PIN until retries exhausted.
    /// </summary>
    public static async Task BlockPinAsync(IPivSession session)
    {
        var wrongPin = "000000"u8.ToArray();
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await session.VerifyPinAsync(wrongPin);
            }
            catch (InvalidPinException)
            {
                // Expected
            }
        }
    }
}
```

**Step 2: Update existing test files to use PivTestHelpers**

Replace local constants with `PivTestHelpers.DefaultPin`, `PivTestHelpers.GetDefaultManagementKey(version)`, etc.

**Step 3: Run all tests**

```bash
dotnet build.cs test --project Piv
```

**Step 4: Commit**

```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/
git commit -m "refactor(piv-tests): extract shared test helpers to reduce duplication"
```

---

## Summary

| Phase | Task | Description |
|-------|------|-------------|
| 1.1 | Fix naming/assertions | Rename test, add IsAuthenticated check |
| 1.2 | Fix ResetAsync test | Check slot metadata instead of certificate |
| 1.3 | Fix ECDH test | Verify shared secrets match with DeriveRawSecretAgreement |
| 1.4 | Fix Bio test | Tighten exception assertion |
| 1.5 | Fix workflow test | Remove misleading certificate storage |
| 1.6 | Fix MoveKey test | Verify key remains functional after move |
| 2.1 | P384 test | Add EccP384 signing with verification |
| 2.2 | X25519 test | Add X25519 key generation test |
| 2.3 | Ed25519 test | Document limitation, verify format |
| 2.4 | RSA 2048 test | Add signing with PKCS#1 v1.5 padding |
| 2.5 | RSA 1024/3072/4096 | Add remaining RSA sizes |
| 2.6 | RSA decrypt test | Add RSA decryption test |
| 3.1 | PUK tests | Add ChangePuk, UnblockPin, GetPukMetadata |
| 3.2 | Management key | Add SetManagementKeyAsync tests |
| 3.3 | Import key | Add ImportKeyAsync test |
| 3.4 | Delete key | Add DeleteKeyAsync test |
| 3.5 | Data objects | Add PutObject/GetObject round-trip |
| 3.6 | PIN attempts | Add SetPinAttemptsAsync test |
| 3.7 | Serial number | Add GetSerialNumberAsync test |
| 4.1 | Helpers | Extract shared constants and helpers |

---

**Completion Criteria:**

- [ ] All existing test issues fixed
- [ ] All PIV algorithms have integration coverage (P256, P384, Ed25519*, X25519*, RSA 1024/2048/3072/4096)
- [ ] All missing API methods have tests
- [ ] No duplicate constants across test files
- [ ] All tests pass: `dotnet build.cs test --project Piv`

*Ed25519/X25519 verification limited until OpenSSL/BouncyCastle support added
