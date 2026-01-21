---
active: false
iteration: 2
max_iterations: 10
completion_promise: "PIV_SESSION_PORT_COMPLETE"
started_at: "2026-01-18T14:40:09.637Z"
---

# PIV Integration Tests Fix (Ralph Loop)

**Goal:** Fix failing PIV integration tests and add missing test coverage for the PIV session port.

**Context:** PIV session was ported in `docs/plans/ralph-loop/2026-01-18-piv-session-port.md`. All integration tests currently fail with `NotSupportedException: Connection type ISmartCardConnection is not supported` because tests don't filter for SmartCard connection type.

**Reference Implementations:**
- **Python:** `../yubikey-manager` (straightforward, easy to read)
- **Java:** `yubikit-android` (verbose but complete)

**Completion Promise:** `PIV_TESTS_FIXED`

---

## Critical Knowledge: Version-Dependent Management Key

**Default management key type changed in firmware 5.7.0:**

| Firmware | Default Key Type | Default Key Value |
|----------|-----------------|-------------------|
| < 5.7.0 | Triple DES (0x03) | `010203040506070801020304050607080102030405060708` (24 bytes) |
| >= 5.7.0 | AES-192 (0x0A) | Check `../yubikey-manager` or `yubikit-android` for exact value |

**Implications:**
- Tests MUST use correct default key based on `state.FirmwareVersion`
- `ResetAsync()` resets to firmware default key type
- `AuthenticateAsync()` must use correct algorithm (3DES vs AES)

---

## Phase 1: Fix Existing Test Attributes

**User Story:** As a test runner, I need PIV tests to filter for SmartCard connection so tests can actually run.

**Files to modify:**
- `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivAuthenticationTests.cs`
- `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivKeyOperationsTests.cs`
- `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivResetTests.cs`
- `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivFullWorkflowTests.cs`

**Step 1: Study test infrastructure**
```bash
# Check attribute syntax in existing working tests
grep -rn "WithYubiKey" Yubico.YubiKit.Management/tests/ --include="*.cs" | head -5
grep -rn "ConnectionType" Yubico.YubiKit.Tests.Shared/ --include="*.cs" | head -10
```

**Step 2: Update all `[WithYubiKey]` attributes**

Change:
```csharp
[WithYubiKey]
```

To:
```csharp
[WithYubiKey(ConnectionType.SmartCardConnection)]
```

For attributes with MinFirmware, use:
```csharp
[WithYubiKey(ConnectionType.SmartCardConnection, MinFirmware = "5.3.0")]
```

**Step 3: Add version-aware management key helper**

Add to each test class (or create shared helper in `Yubico.YubiKit.Tests.Shared`):
```csharp
private static readonly byte[] DefaultTripleDesManagementKey = new byte[]
{
    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
};

// TODO: Look up exact value from ../yubikey-manager or yubikit-android
private static readonly byte[] DefaultAesManagementKey = new byte[24];

private static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
    version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultTripleDesManagementKey;
```

Update test methods to use `GetDefaultManagementKey(state.FirmwareVersion)` instead of hardcoded key.

**Step 4: Verify build**
```bash
dotnet build.cs build
```

**Step 5: Run tests to see new failures**
```bash
dotnet build.cs test --filter "FullyQualifiedName~Piv.IntegrationTests"
```

**Step 6: Commit**
```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/*.cs
git commit -m "fix(piv): add SmartCardConnection filter to integration tests"
```

→ Output `<promise>PHASE_1_DONE</promise>`

---

## Phase 2: Fix Subsequent Test Failures (Iterative)

**User Story:** As a developer, I need all existing PIV integration tests to pass.

**Loop Process:**
```
while (tests fail):
    1. Run: dotnet build.cs test --filter "FullyQualifiedName~Piv.IntegrationTests"
    2. Analyze failures (group by error type)
    3. Fix root cause
    4. Re-run tests
```

**Common issues and fixes:**

| Error Type | Likely Cause | Fix |
|------------|--------------|-----|
| `NotImplementedException` | Stub method | Implement the method |
| `ApduException` with SW | Wrong APDU encoding | Check PIV spec, compare with Python/Java |
| `TlvParseException` | Response parsing | Debug TLV structure, check tags |
| `InvalidOperationException` | State issue | Check auth/PIN state before operation |
| `CryptographicException` | Key/algo mismatch | Compare with reference impl byte-by-byte |
| `NullReferenceException` | Missing null check | Add defensive checks |
| Auth failure on 5.7+ | Wrong mgmt key type | Use AES key, check `ManagementKeyType` after reset |

**For each failure:**
1. Note test name and error
2. Check if implementation exists (vs stub)
3. Add logging to trace APDU exchange if needed
4. Compare with `../yubikey-manager` (Python) or `yubikit-android` (Java)
5. Fix and verify

**Step N: After each fix batch, verify build**
```bash
dotnet build.cs build
dotnet build.cs test --filter "FullyQualifiedName~Piv.IntegrationTests"
```

**Step N+1: Commit when a logical group of fixes is complete**
```bash
git add Yubico.YubiKit.Piv/src/*.cs
git commit -m "fix(piv): <describe fixes>"
```

→ Output `<promise>PHASE_2_DONE</promise>` when ALL existing PIV integration tests pass

---

## Phase 3: Create Missing Test Files

**User Story:** As a developer, I need complete test coverage for PIV functionality.

**Missing test files:**
- `PivCryptoTests.cs` - Sign/decrypt, ECDH
- `PivCertificateTests.cs` - Store/retrieve/delete certificates
- `PivMetadataTests.cs` - PIN/slot/management key metadata

### 3.1: Create PivCryptoTests.cs

**File:** `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs`

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

using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

public class PivCryptoTests
{
    private static readonly byte[] DefaultTripleDesManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };
    
    // TODO: Get exact default AES key from ../yubikey-manager or yubikit-android
    private static readonly byte[] DefaultAesManagementKey = new byte[24];
    
    private static readonly byte[] DefaultPin = "123456"u8.ToArray();

    private static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
        version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultTripleDesManagementKey;

    [Theory]
    [WithYubiKey(ConnectionType.SmartCardConnection)]
    public async Task SignOrDecryptAsync_EccP256Sign_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
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
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(((ECPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(ecdsa.VerifyHash(dataToSign, signature.Span));
    }

    [Theory]
    [WithYubiKey(ConnectionType.SmartCardConnection)]
    public async Task CalculateSecretAsync_ECDH_ProducesSharedSecret(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        var devicePublicKey = await session.GenerateKeyAsync(
            PivSlot.KeyManagement, 
            PivAlgorithm.EccP256);
        await session.VerifyPinAsync(DefaultPin);
        
        using var peerKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var peerPublicKeyBytes = peerKey.PublicKey.ExportSubjectPublicKeyInfo();
        var peerPublicKey = ECPublicKey.CreateFromSubjectPublicKeyInfo(peerPublicKeyBytes);
        
        var sharedSecret = await session.CalculateSecretAsync(
            PivSlot.KeyManagement, 
            peerPublicKey);
        
        Assert.Equal(32, sharedSecret.Length);
    }

    [Theory]
    [WithYubiKey(ConnectionType.SmartCardConnection, MinFirmware = "5.7.0")]
    public async Task SignOrDecryptAsync_Ed25519_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Signature, 
            PivAlgorithm.Ed25519,
            PivPinPolicy.Once);
        await session.VerifyPinAsync(DefaultPin);
        
        var dataToSign = "test data"u8.ToArray();
        
        var signature = await session.SignOrDecryptAsync(
            PivSlot.Signature, 
            PivAlgorithm.Ed25519, 
            dataToSign);
        
        Assert.NotEmpty(signature.ToArray());
        Assert.Equal(64, signature.Length);
    }
}
```

### 3.2: Create PivCertificateTests.cs

**File:** `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCertificateTests.cs`

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

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

public class PivCertificateTests
{
    private static readonly byte[] DefaultTripleDesManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };
    
    private static readonly byte[] DefaultAesManagementKey = new byte[24];

    private static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
        version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultTripleDesManagementKey;

    [Theory]
    [WithYubiKey(ConnectionType.SmartCardConnection)]
    public async Task StoreCertificateAsync_GetCertificateAsync_RoundTrip(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        var publicKey = await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);
        
        var cert = CreateSelfSignedCertificate((ECPublicKey)publicKey);
        
        await session.StoreCertificateAsync(PivSlot.Authentication, cert);
        var retrieved = await session.GetCertificateAsync(PivSlot.Authentication);
        
        Assert.NotNull(retrieved);
        Assert.Equal(cert.Thumbprint, retrieved.Thumbprint);
    }

    [Theory]
    [WithYubiKey(ConnectionType.SmartCardConnection)]
    public async Task GetCertificateAsync_EmptySlot_ReturnsNull(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var cert = await session.GetCertificateAsync(PivSlot.Authentication);
        
        Assert.Null(cert);
    }

    [Theory]
    [WithYubiKey(ConnectionType.SmartCardConnection)]
    public async Task DeleteCertificateAsync_IsIdempotent(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        
        await session.DeleteCertificateAsync(PivSlot.Authentication);
        await session.DeleteCertificateAsync(PivSlot.Authentication);
    }

    [Theory]
    [WithYubiKey(ConnectionType.SmartCardConnection)]
    public async Task GetObjectAsync_EmptyObject_ReturnsEmpty(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var data = await session.GetObjectAsync(PivDataObject.Chuid);
        
        Assert.True(data.IsEmpty);
    }

    private static X509Certificate2 CreateSelfSignedCertificate(ECPublicKey publicKey)
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(publicKey.ExportSubjectPublicKeyInfo(), out _);
        
        var request = new CertificateRequest(
            "CN=Test Certificate",
            ecdsa,
            HashAlgorithmName.SHA256);
        
        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddYears(1));
    }
}
```

### 3.3: Create PivMetadataTests.cs

**File:** `Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivMetadataTests.cs`

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
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

public class PivMetadataTests
{
    private static readonly byte[] DefaultTripleDesManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };
    
    private static readonly byte[] DefaultAesManagementKey = new byte[24];

    private static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
        version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultTripleDesManagementKey;

    [Theory]
    [WithYubiKey(ConnectionType.SmartCardConnection, MinFirmware = "5.3.0")]
    public async Task GetPinMetadataAsync_ReturnsValidMetadata(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var metadata = await session.GetPinMetadataAsync();
        
        Assert.True(metadata.IsDefault);
        Assert.Equal(3, metadata.TotalRetries);
        Assert.Equal(3, metadata.RetriesRemaining);
    }

    [Theory]
    [WithYubiKey(ConnectionType.SmartCardConnection, MinFirmware = "5.3.0")]
    public async Task GetSlotMetadataAsync_EmptySlot_ReturnsNull(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var metadata = await session.GetSlotMetadataAsync(PivSlot.Authentication);
        
        Assert.Null(metadata);
    }

    [Theory]
    [WithYubiKey(ConnectionType.SmartCardConnection, MinFirmware = "5.3.0")]
    public async Task GetSlotMetadataAsync_WithKey_ReturnsMetadata(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);
        
        var metadata = await session.GetSlotMetadataAsync(PivSlot.Authentication);
        
        Assert.NotNull(metadata);
        Assert.Equal(PivAlgorithm.EccP256, metadata.Value.Algorithm);
        Assert.True(metadata.Value.IsGenerated);
    }

    [Theory]
    [WithYubiKey(ConnectionType.SmartCardConnection, MinFirmware = "5.3.0")]
    public async Task GetManagementKeyMetadataAsync_ReturnsValidMetadata(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var metadata = await session.GetManagementKeyMetadataAsync();
        
        Assert.True(metadata.IsDefault);
        // Key type depends on firmware version
        if (state.FirmwareVersion >= new FirmwareVersion(5, 7, 0))
        {
            Assert.Equal(PivManagementKeyType.Aes192, metadata.KeyType);
        }
        else
        {
            Assert.Equal(PivManagementKeyType.TripleDes, metadata.KeyType);
        }
    }

    [Theory]
    [WithYubiKey(ConnectionType.SmartCardConnection)]
    public async Task GetBioMetadataAsync_NonBioDevice_ThrowsOrReturnsError(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        
        var ex = await Record.ExceptionAsync(() => session.GetBioMetadataAsync());
        
        Assert.True(ex is NotSupportedException || ex is ApduException);
    }
}
```

**Step: Verify build and tests**
```bash
dotnet build.cs build
dotnet build.cs test --filter "FullyQualifiedName~Piv.IntegrationTests"
```

**Step: Commit**
```bash
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCertificateTests.cs
git add Yubico.YubiKit.Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivMetadataTests.cs
git commit -m "test(piv): add crypto, certificate, and metadata integration tests"
```

→ Output `<promise>PHASE_3_DONE</promise>`

---

## Phase 4: Fix New Test Failures (Iterative)

Same process as Phase 2 - iterate until all new tests pass.

**Loop:**
```bash
dotnet build.cs test --filter "FullyQualifiedName~Piv.IntegrationTests"
# Analyze failures, fix, repeat
```

**Commit after fixes:**
```bash
git add Yubico.YubiKit.Piv/src/*.cs
git commit -m "fix(piv): <describe fixes for new tests>"
```

→ Output `<promise>PHASE_4_DONE</promise>` when all PIV integration tests pass

---

## Verification Requirements (MUST PASS BEFORE COMPLETION)

1. **Build:** `dotnet build.cs build` (must exit 0)
2. **PIV Unit Tests:** `dotnet build.cs test --filter "FullyQualifiedName~Piv.UnitTests"` (all pass)
3. **PIV Integration Tests:** `dotnet build.cs test --filter "FullyQualifiedName~Piv.IntegrationTests"` (all pass)
4. **No Regressions:** `dotnet build.cs test` (full suite passes)

**Final verification:**
```bash
dotnet build.cs build && dotnet build.cs test
```

Only after ALL pass, output `<promise>PIV_TESTS_FIXED</promise>`.
If any fail, fix and re-verify.

---

## On Failure

- If build fails: fix compilation errors, re-run build
- If tests fail: analyze error, fix root cause, re-run ALL PIV tests
- If management key auth fails on 5.7+: check `ManagementKeyType`, use AES key
- If stuck: compare with `../yubikey-manager` (Python) or `yubikit-android` (Java)
- Do NOT output completion until all green

---

## Handoff

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts \
  --prompt-file ./docs/plans/ralph-loop/2026-01-21-piv-test-fixes.md \
  --completion-promise "PIV_TESTS_FIXED" \
  --max-iterations 30 \
  --learn \
  --model claude-sonnet-4
```

**Notes:**
- Using 30 iterations due to iterative fix phases
- Test device is YubiKey 5.8.0 (uses AES-192 default management key)
- Reference Python impl at `../yubikey-manager` for quick lookups
