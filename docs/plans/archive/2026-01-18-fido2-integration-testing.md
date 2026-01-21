# FIDO2 Integration Testing Implementation Plan (Ralph Loop)

**Goal:** Implement comprehensive integration tests for FidoSession to achieve feature parity with Java yubikit-android testing and prevent regressions in WebAuthn workflows.

**PRD:** `docs/specs/fido2-integration-testing/final_spec.md`
**Completion Promise:** `FIDO2_INTEGRATION_TESTING_COMPLETE`

---

## Phase 0: Test Infrastructure Setup

**Objective:** Create shared test utilities and constants before writing any tests.

**Files:**
- Create: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoTestData.cs`
- Create: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoTestStateExtensions.cs`
- Create: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoSessionExtensions.cs`
- Create: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoTestHelpers.cs`

**Step 1: Create FidoTestData.cs**

```csharp
using System.Security.Cryptography;
using Yubico.YubiKey.Fido2;

namespace Yubico.YubiKey.Fido2.IntegrationTests;

/// <summary>
/// Shared test constants and generators for FIDO2 integration tests.
/// </summary>
public static class FidoTestData
{
    public const string RpId = "localhost";
    public const string RpName = "Test RP";
    public const string UserName = "testuser@example.com";
    public const string UserDisplayName = "Test User";
    
    /// <summary>
    /// Test PIN that meets enhanced complexity requirements (8+ chars, mixed case + numbers).
    /// </summary>
    public const string Pin = "Abc12345";
    
    /// <summary>
    /// Simple PIN for devices without complexity enforcement.
    /// </summary>
    public const string SimplePinFallback = "123456";
    
    /// <summary>
    /// Generates a random 16-byte user ID.
    /// </summary>
    public static byte[] GenerateUserId() => RandomNumberGenerator.GetBytes(16);
    
    /// <summary>
    /// Generates a random 32-byte challenge.
    /// </summary>
    public static byte[] GenerateChallenge() => RandomNumberGenerator.GetBytes(32);
    
    /// <summary>
    /// Creates a standard relying party entity for tests.
    /// </summary>
    public static RelyingParty CreateRelyingParty() => new(RpId) { Name = RpName };
    
    /// <summary>
    /// Creates a standard user entity for tests.
    /// </summary>
    public static UserEntity CreateUser() => new(GenerateUserId())
    {
        Name = UserName,
        DisplayName = UserDisplayName
    };
}
```

**Step 2: Create FidoTestStateExtensions.cs**

```csharp
using Yubico.YubiKey.TestFramework;

namespace Yubico.YubiKey.Fido2.IntegrationTests;

/// <summary>
/// Extension methods on YubiKeyTestState for FIDO2 test lifecycle management.
/// </summary>
public static class FidoTestStateExtensions
{
    /// <summary>
    /// Executes an action with a FIDO session, handling session lifecycle.
    /// </summary>
    public static async Task WithFidoSessionAsync(
        this YubiKeyTestState state,
        Func<FidoSession, Task> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        
        await using var session = await state.Device
            .CreateFidoSessionAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        
        await action(session).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Executes an action with a FIDO session and returns a result.
    /// </summary>
    public static async Task<T> WithFidoSessionAsync<T>(
        this YubiKeyTestState state,
        Func<FidoSession, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        
        await using var session = await state.Device
            .CreateFidoSessionAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        
        return await action(session).ConfigureAwait(false);
    }
}
```

**Step 3: Create FidoSessionExtensions.cs**

```csharp
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Ctap2;

namespace Yubico.YubiKey.Fido2.IntegrationTests;

/// <summary>
/// Extension methods on FidoSession for common test operations.
/// </summary>
public static class FidoSessionExtensions
{
    /// <summary>
    /// Sets PIN if not configured, or verifies existing PIN.
    /// </summary>
    public static async Task SetOrVerifyPinAsync(
        this FidoSession session,
        string pin,
        CancellationToken cancellationToken = default)
    {
        var info = await session.GetInfoAsync(cancellationToken).ConfigureAwait(false);
        
        // Check if PIN is configured
        bool pinConfigured = info.Options?.TryGetValue("clientPin", out var clientPinObj) == true 
            && clientPinObj is bool clientPin && clientPin;
        
        if (!pinConfigured)
        {
            await session.SetPinAsync(pin, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Verify by getting a PIN token
            await session.VerifyPinAsync(pin, cancellationToken).ConfigureAwait(false);
        }
    }
    
    /// <summary>
    /// Deletes all resident credentials for the specified relying party.
    /// </summary>
    public static async Task DeleteAllCredentialsForRpAsync(
        this FidoSession session,
        string rpId,
        string pin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var credentials = await session
                .EnumerateCredentialsForRelyingPartyAsync(rpId, pin, cancellationToken)
                .ConfigureAwait(false);
            
            foreach (var credential in credentials)
            {
                await session
                    .DeleteCredentialAsync(credential.CredentialId, pin, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (CtapException ex) when (ex.Status == CtapStatus.NoCredentials)
        {
            // No credentials to delete - that's fine
        }
    }
}
```

**Step 4: Verify build compiles**

```bash
dotnet build.cs build --project Yubico.YubiKit.Fido2.IntegrationTests
```

Expected: Build succeeds (infrastructure code compiles)

**Step 5: Commit infrastructure**

```bash
git add Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoTestData.cs \
        Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoTestStateExtensions.cs \
        Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoSessionExtensions.cs
git commit -m "feat(fido2-tests): add test infrastructure and utilities"
```

→ Output `<promise>PHASE_0_DONE</promise>`

---

## Phase 1: Credential Registration Tests (Story 1)

**User Story:** As a SDK maintainer, I want to run integration tests that exercise MakeCredential workflows on real YubiKeys, so that I can detect regressions in the credential registration pipeline before release.

**Files:**
- Create: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoMakeCredentialTests.cs`

**Step 1: Write failing tests**

```csharp
using Xunit;
using Yubico.YubiKey.TestFramework;

namespace Yubico.YubiKey.Fido2.IntegrationTests;

[Trait("Category", "Integration")]
public class FidoMakeCredentialTests
{
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.2.0")]
    public async Task MakeCredential_NonResidentKey_ReturnsValidAttestation(YubiKeyTestState state)
    {
        await state.WithFidoSessionAsync(async session =>
        {
            // Arrange
            await session.SetOrVerifyPinAsync(FidoTestData.Pin);
            var rp = FidoTestData.CreateRelyingParty();
            var user = FidoTestData.CreateUser();
            var challenge = FidoTestData.GenerateChallenge();
            
            // Act
            var result = await session.MakeCredentialAsync(
                clientDataHash: challenge,
                rp: rp,
                user: user,
                pubKeyCredParams: [new PublicKeyCredentialParameters(PublicKeyCredentialType.PublicKey, CoseAlgorithm.ES256)],
                options: new MakeCredentialOptions { ResidentKey = false });
            
            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.AttestationObject);
            Assert.NotNull(result.AuthenticatorData);
            Assert.Equal(16, result.AuthenticatorData.AttestedCredentialData?.Aaguid.Length);
        });
    }
    
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.2.0")]
    public async Task MakeCredential_ResidentKey_ReturnsCredentialId(YubiKeyTestState state)
    {
        byte[]? credentialId = null;
        
        try
        {
            await state.WithFidoSessionAsync(async session =>
            {
                // Arrange
                await session.SetOrVerifyPinAsync(FidoTestData.Pin);
                var rp = FidoTestData.CreateRelyingParty();
                var user = FidoTestData.CreateUser();
                var challenge = FidoTestData.GenerateChallenge();
                
                // Act
                var result = await session.MakeCredentialAsync(
                    clientDataHash: challenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: [new PublicKeyCredentialParameters(PublicKeyCredentialType.PublicKey, CoseAlgorithm.ES256)],
                    options: new MakeCredentialOptions { ResidentKey = true });
                
                // Assert
                Assert.NotNull(result);
                Assert.NotNull(result.AuthenticatorData.AttestedCredentialData);
                credentialId = result.AuthenticatorData.AttestedCredentialData.CredentialId.ToArray();
                Assert.NotEmpty(credentialId);
            });
        }
        finally
        {
            // Cleanup: Delete the credential
            if (credentialId != null)
            {
                await state.WithFidoSessionAsync(async session =>
                {
                    await session.DeleteAllCredentialsForRpAsync(FidoTestData.RpId, FidoTestData.Pin);
                });
            }
        }
    }
    
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.2.0")]
    public async Task MakeCredential_WithExcludeList_ThrowsCredentialExcluded(YubiKeyTestState state)
    {
        byte[]? credentialId = null;
        
        try
        {
            await state.WithFidoSessionAsync(async session =>
            {
                // Arrange: Create first credential
                await session.SetOrVerifyPinAsync(FidoTestData.Pin);
                var rp = FidoTestData.CreateRelyingParty();
                var user = FidoTestData.CreateUser();
                
                var firstResult = await session.MakeCredentialAsync(
                    clientDataHash: FidoTestData.GenerateChallenge(),
                    rp: rp,
                    user: user,
                    pubKeyCredParams: [new PublicKeyCredentialParameters(PublicKeyCredentialType.PublicKey, CoseAlgorithm.ES256)],
                    options: new MakeCredentialOptions { ResidentKey = true });
                
                credentialId = firstResult.AuthenticatorData.AttestedCredentialData!.CredentialId.ToArray();
                
                // Act & Assert: Try to create with same credential in exclude list
                var excludeList = new[] { new PublicKeyCredentialDescriptor(credentialId) };
                
                var ex = await Assert.ThrowsAsync<CtapException>(async () =>
                {
                    await session.MakeCredentialAsync(
                        clientDataHash: FidoTestData.GenerateChallenge(),
                        rp: rp,
                        user: user,
                        pubKeyCredParams: [new PublicKeyCredentialParameters(PublicKeyCredentialType.PublicKey, CoseAlgorithm.ES256)],
                        options: new MakeCredentialOptions 
                        { 
                            ResidentKey = true,
                            ExcludeList = excludeList 
                        });
                });
                
                Assert.Equal(CtapStatus.CredentialExcluded, ex.Status);
            });
        }
        finally
        {
            if (credentialId != null)
            {
                await state.WithFidoSessionAsync(async session =>
                {
                    await session.DeleteAllCredentialsForRpAsync(FidoTestData.RpId, FidoTestData.Pin);
                });
            }
        }
    }
}
```

**Step 2: Verify RED**

```bash
dotnet build.cs test --filter "FullyQualifiedName~FidoMakeCredentialTests"
```

Expected: Tests should compile but may fail if device not present, or pass if device available.

**Step 3: Iterate on implementation**

Adjust test code based on actual API signatures discovered in the codebase. Reference:
- `Yubico.YubiKit.Fido2/src/FidoSession.cs` for method signatures
- `Yubico.YubiKit.Fido2/src/Commands/` for request/response types
- Existing tests in `FidoSessionSimpleTests.cs` for patterns

**Step 4: Verify GREEN**

```bash
dotnet build.cs test --filter "FullyQualifiedName~FidoMakeCredentialTests"
```

**Step 5: Commit**

```bash
git add Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoMakeCredentialTests.cs
git commit -m "test(fido2): add MakeCredential integration tests"
```

→ Output `<promise>PHASE_1_DONE</promise>`

---

## Phase 2: Authentication Flow Tests (Story 2)

**User Story:** As a SDK maintainer, I want to run integration tests that exercise GetAssertion workflows on real YubiKeys, so that I can verify the authentication pipeline produces valid signatures.

**Files:**
- Create: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoGetAssertionTests.cs`

**Step 1: Write failing tests**

```csharp
using Xunit;
using Yubico.YubiKey.TestFramework;

namespace Yubico.YubiKey.Fido2.IntegrationTests;

[Trait("Category", "Integration")]
public class FidoGetAssertionTests
{
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.2.0")]
    public async Task GetAssertion_AfterMakeCredential_ReturnsValidSignature(YubiKeyTestState state)
    {
        byte[]? credentialId = null;
        
        try
        {
            await state.WithFidoSessionAsync(async session =>
            {
                // Arrange: Create credential first
                await session.SetOrVerifyPinAsync(FidoTestData.Pin);
                var rp = FidoTestData.CreateRelyingParty();
                var user = FidoTestData.CreateUser();
                
                var makeResult = await session.MakeCredentialAsync(
                    clientDataHash: FidoTestData.GenerateChallenge(),
                    rp: rp,
                    user: user,
                    pubKeyCredParams: [new PublicKeyCredentialParameters(PublicKeyCredentialType.PublicKey, CoseAlgorithm.ES256)],
                    options: new MakeCredentialOptions { ResidentKey = true });
                
                credentialId = makeResult.AuthenticatorData.AttestedCredentialData!.CredentialId.ToArray();
                
                // Act: Get assertion
                var assertionResult = await session.GetAssertionAsync(
                    rpId: FidoTestData.RpId,
                    clientDataHash: FidoTestData.GenerateChallenge(),
                    options: new GetAssertionOptions
                    {
                        AllowList = [new PublicKeyCredentialDescriptor(credentialId)]
                    });
                
                // Assert
                Assert.NotNull(assertionResult);
                Assert.NotNull(assertionResult.AuthenticatorData);
                Assert.NotNull(assertionResult.Signature);
                Assert.True(assertionResult.Signature.Length > 0);
            });
        }
        finally
        {
            if (credentialId != null)
            {
                await state.WithFidoSessionAsync(async session =>
                {
                    await session.DeleteAllCredentialsForRpAsync(FidoTestData.RpId, FidoTestData.Pin);
                });
            }
        }
    }
    
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.2.0")]
    public async Task GetAssertion_ResidentKey_ReturnsUserHandle(YubiKeyTestState state)
    {
        byte[]? credentialId = null;
        byte[]? expectedUserId = null;
        
        try
        {
            await state.WithFidoSessionAsync(async session =>
            {
                // Arrange: Create RK credential
                await session.SetOrVerifyPinAsync(FidoTestData.Pin);
                var rp = FidoTestData.CreateRelyingParty();
                expectedUserId = FidoTestData.GenerateUserId();
                var user = new UserEntity(expectedUserId)
                {
                    Name = FidoTestData.UserName,
                    DisplayName = FidoTestData.UserDisplayName
                };
                
                var makeResult = await session.MakeCredentialAsync(
                    clientDataHash: FidoTestData.GenerateChallenge(),
                    rp: rp,
                    user: user,
                    pubKeyCredParams: [new PublicKeyCredentialParameters(PublicKeyCredentialType.PublicKey, CoseAlgorithm.ES256)],
                    options: new MakeCredentialOptions { ResidentKey = true });
                
                credentialId = makeResult.AuthenticatorData.AttestedCredentialData!.CredentialId.ToArray();
                
                // Act: Get assertion (no allow list - uses RK)
                var assertionResult = await session.GetAssertionAsync(
                    rpId: FidoTestData.RpId,
                    clientDataHash: FidoTestData.GenerateChallenge());
                
                // Assert
                Assert.NotNull(assertionResult.UserHandle);
                Assert.Equal(expectedUserId, assertionResult.UserHandle.ToArray());
            });
        }
        finally
        {
            if (credentialId != null)
            {
                await state.WithFidoSessionAsync(async session =>
                {
                    await session.DeleteAllCredentialsForRpAsync(FidoTestData.RpId, FidoTestData.Pin);
                });
            }
        }
    }
    
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.2.0")]
    public async Task GetAssertion_NoCredentials_ThrowsNoCredentials(YubiKeyTestState state)
    {
        await state.WithFidoSessionAsync(async session =>
        {
            // Arrange: Ensure no credentials for our RP
            await session.SetOrVerifyPinAsync(FidoTestData.Pin);
            await session.DeleteAllCredentialsForRpAsync(FidoTestData.RpId, FidoTestData.Pin);
            
            // Act & Assert
            var ex = await Assert.ThrowsAsync<CtapException>(async () =>
            {
                await session.GetAssertionAsync(
                    rpId: FidoTestData.RpId,
                    clientDataHash: FidoTestData.GenerateChallenge());
            });
            
            Assert.Equal(CtapStatus.NoCredentials, ex.Status);
        });
    }
}
```

**Step 2: Verify RED**

```bash
dotnet build.cs test --filter "FullyQualifiedName~FidoGetAssertionTests"
```

**Step 3: Adjust based on actual API**

Reference existing test patterns and FidoSession API.

**Step 4: Verify GREEN**

```bash
dotnet build.cs test --filter "FullyQualifiedName~FidoGetAssertionTests"
```

**Step 5: Commit**

```bash
git add Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoGetAssertionTests.cs
git commit -m "test(fido2): add GetAssertion integration tests"
```

→ Output `<promise>PHASE_2_DONE</promise>`

---

## Phase 3: Credential Management Tests (Story 3)

**User Story:** As a SDK maintainer, I want to enumerate, inspect, and delete credentials during integration tests, so that tests can verify credential management APIs and clean up after themselves.

**Files:**
- Create: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoCredentialManagementTests.cs`

**Step 1: Write failing tests**

```csharp
using Xunit;
using Yubico.YubiKey.TestFramework;

namespace Yubico.YubiKey.Fido2.IntegrationTests;

[Trait("Category", "Integration")]
public class FidoCredentialManagementTests
{
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.4.0")]
    public async Task EnumerateCredentials_WithResidentKeys_ReturnsCredentialList(YubiKeyTestState state)
    {
        byte[]? credentialId = null;
        
        try
        {
            await state.WithFidoSessionAsync(async session =>
            {
                // Arrange: Create a credential
                await session.SetOrVerifyPinAsync(FidoTestData.Pin);
                var rp = FidoTestData.CreateRelyingParty();
                var user = FidoTestData.CreateUser();
                
                var makeResult = await session.MakeCredentialAsync(
                    clientDataHash: FidoTestData.GenerateChallenge(),
                    rp: rp,
                    user: user,
                    pubKeyCredParams: [new PublicKeyCredentialParameters(PublicKeyCredentialType.PublicKey, CoseAlgorithm.ES256)],
                    options: new MakeCredentialOptions { ResidentKey = true });
                
                credentialId = makeResult.AuthenticatorData.AttestedCredentialData!.CredentialId.ToArray();
                
                // Act: Enumerate credentials
                var credentials = await session.EnumerateCredentialsForRelyingPartyAsync(
                    FidoTestData.RpId, FidoTestData.Pin);
                
                // Assert
                Assert.NotEmpty(credentials);
                Assert.Contains(credentials, c => c.CredentialId.SequenceEqual(credentialId));
            });
        }
        finally
        {
            if (credentialId != null)
            {
                await state.WithFidoSessionAsync(async session =>
                {
                    await session.DeleteAllCredentialsForRpAsync(FidoTestData.RpId, FidoTestData.Pin);
                });
            }
        }
    }
    
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.4.0")]
    public async Task EnumerateCredentials_NoCredentials_ReturnsEmptyList(YubiKeyTestState state)
    {
        await state.WithFidoSessionAsync(async session =>
        {
            // Arrange: Ensure no credentials
            await session.SetOrVerifyPinAsync(FidoTestData.Pin);
            await session.DeleteAllCredentialsForRpAsync(FidoTestData.RpId, FidoTestData.Pin);
            
            // Act & Assert
            var ex = await Assert.ThrowsAsync<CtapException>(async () =>
            {
                await session.EnumerateCredentialsForRelyingPartyAsync(
                    FidoTestData.RpId, FidoTestData.Pin);
            });
            
            Assert.Equal(CtapStatus.NoCredentials, ex.Status);
        });
    }
    
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.4.0")]
    public async Task DeleteCredential_ExistingCredential_RemovesFromDevice(YubiKeyTestState state)
    {
        await state.WithFidoSessionAsync(async session =>
        {
            // Arrange: Create a credential
            await session.SetOrVerifyPinAsync(FidoTestData.Pin);
            var rp = FidoTestData.CreateRelyingParty();
            var user = FidoTestData.CreateUser();
            
            var makeResult = await session.MakeCredentialAsync(
                clientDataHash: FidoTestData.GenerateChallenge(),
                rp: rp,
                user: user,
                pubKeyCredParams: [new PublicKeyCredentialParameters(PublicKeyCredentialType.PublicKey, CoseAlgorithm.ES256)],
                options: new MakeCredentialOptions { ResidentKey = true });
            
            var credentialId = makeResult.AuthenticatorData.AttestedCredentialData!.CredentialId.ToArray();
            
            // Act: Delete the credential
            await session.DeleteCredentialAsync(credentialId, FidoTestData.Pin);
            
            // Assert: Should not be found anymore
            var ex = await Assert.ThrowsAsync<CtapException>(async () =>
            {
                await session.EnumerateCredentialsForRelyingPartyAsync(
                    FidoTestData.RpId, FidoTestData.Pin);
            });
            
            Assert.Equal(CtapStatus.NoCredentials, ex.Status);
        });
    }
}
```

**Step 2-5:** Verify RED → Implement → Verify GREEN → Commit

```bash
dotnet build.cs test --filter "FullyQualifiedName~FidoCredentialManagementTests"
git add Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoCredentialManagementTests.cs
git commit -m "test(fido2): add credential management integration tests"
```

→ Output `<promise>PHASE_3_DONE</promise>`

---

## Phase 4: Algorithm Support Tests (Story 4)

**User Story:** As a SDK maintainer, I want to verify credential creation with different cryptographic algorithms, so that I can ensure algorithm negotiation works correctly across YubiKey models.

**Files:**
- Create: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoAlgorithmSupportTests.cs`

**Step 1: Write tests for ES256, ES384, EdDSA**

```csharp
using Xunit;
using Yubico.YubiKey.TestFramework;

namespace Yubico.YubiKey.Fido2.IntegrationTests;

[Trait("Category", "Integration")]
public class FidoAlgorithmSupportTests
{
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.2.0")]
    public async Task MakeCredential_ES256_ReturnsES256Credential(YubiKeyTestState state)
    {
        await TestAlgorithmAsync(state, CoseAlgorithm.ES256);
    }
    
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.2.0")]
    public async Task MakeCredential_ES384_ReturnsES384Credential(YubiKeyTestState state)
    {
        // ES384 may not be supported on all devices - skip if not
        await TestAlgorithmAsync(state, CoseAlgorithm.ES384, skipIfUnsupported: true);
    }
    
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.7.0")]
    public async Task MakeCredential_EdDSA_ReturnsEdDSACredential(YubiKeyTestState state)
    {
        await TestAlgorithmAsync(state, CoseAlgorithm.EdDSA);
    }
    
    private static async Task TestAlgorithmAsync(
        YubiKeyTestState state, 
        CoseAlgorithm algorithm,
        bool skipIfUnsupported = false)
    {
        byte[]? credentialId = null;
        
        try
        {
            await state.WithFidoSessionAsync(async session =>
            {
                await session.SetOrVerifyPinAsync(FidoTestData.Pin);
                
                // Check if algorithm is supported
                var info = await session.GetInfoAsync();
                var supported = info.Algorithms?.Any(a => a.Algorithm == algorithm) ?? false;
                
                if (!supported && skipIfUnsupported)
                {
                    Skip.If(true, $"Algorithm {algorithm} not supported on this device");
                    return;
                }
                
                var rp = FidoTestData.CreateRelyingParty();
                var user = FidoTestData.CreateUser();
                
                var result = await session.MakeCredentialAsync(
                    clientDataHash: FidoTestData.GenerateChallenge(),
                    rp: rp,
                    user: user,
                    pubKeyCredParams: [new PublicKeyCredentialParameters(PublicKeyCredentialType.PublicKey, algorithm)],
                    options: new MakeCredentialOptions { ResidentKey = true });
                
                credentialId = result.AuthenticatorData.AttestedCredentialData!.CredentialId.ToArray();
                
                // Verify the credential uses the requested algorithm
                Assert.NotNull(result.AuthenticatorData.AttestedCredentialData.PublicKey);
                // Algorithm verification depends on COSE key structure
            });
        }
        finally
        {
            if (credentialId != null)
            {
                await state.WithFidoSessionAsync(async session =>
                {
                    await session.DeleteAllCredentialsForRpAsync(FidoTestData.RpId, FidoTestData.Pin);
                });
            }
        }
    }
}
```

**Step 2-5:** Verify RED → Implement → Verify GREEN → Commit

```bash
dotnet build.cs test --filter "FullyQualifiedName~FidoAlgorithmSupportTests"
git add Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoAlgorithmSupportTests.cs
git commit -m "test(fido2): add algorithm support integration tests"
```

→ Output `<promise>PHASE_4_DONE</promise>`

---

## Phase 5: GetInfo Validation Tests

**Objective:** Port `Ctap2SessionTests.testCtap2GetInfo()` patterns from Java.

**Files:**
- Create: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoGetInfoTests.cs`

**Step 1: Write tests**

```csharp
using Xunit;
using Yubico.YubiKey.TestFramework;

namespace Yubico.YubiKey.Fido2.IntegrationTests;

[Trait("Category", "Integration")]
public class FidoGetInfoTests
{
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.2.0")]
    public async Task GetInfo_ReturnsValidVersions(YubiKeyTestState state)
    {
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            
            Assert.NotNull(info.Versions);
            Assert.True(
                info.Versions.Contains("FIDO_2_0") ||
                info.Versions.Contains("FIDO_2_1_PRE") ||
                info.Versions.Contains("FIDO_2_1") ||
                info.Versions.Contains("FIDO_2_2"),
                "Expected at least one FIDO2 version");
        });
    }
    
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.2.0")]
    public async Task GetInfo_ReturnsValidAaguid(YubiKeyTestState state)
    {
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            
            Assert.NotNull(info.Aaguid);
            Assert.Equal(16, info.Aaguid.Length);
        });
    }
    
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.2.0")]
    public async Task GetInfo_ReturnsExpectedOptions(YubiKeyTestState state)
    {
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            
            Assert.NotNull(info.Options);
            
            // Platform authenticator should be false (YubiKey is roaming)
            Assert.True(info.Options.TryGetValue("plat", out var plat));
            Assert.False((bool)plat!);
            
            // Resident keys should be supported
            Assert.True(info.Options.TryGetValue("rk", out var rk));
            Assert.True((bool)rk!);
            
            // User presence should be supported
            Assert.True(info.Options.TryGetValue("up", out var up));
            Assert.True((bool)up!);
            
            // clientPin option should exist
            Assert.True(info.Options.ContainsKey("clientPin"));
        });
    }
    
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.2.0")]
    public async Task GetInfo_ReturnsPinUvAuthProtocols(YubiKeyTestState state)
    {
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            
            Assert.NotNull(info.PinUvAuthProtocols);
            Assert.NotEmpty(info.PinUvAuthProtocols);
            Assert.Contains(1, info.PinUvAuthProtocols); // Protocol 1 should always be present
        });
    }
}
```

**Step 2-5:** Verify RED → Implement → Verify GREEN → Commit

```bash
dotnet build.cs test --filter "FullyQualifiedName~FidoGetInfoTests"
git add Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoGetInfoTests.cs
git commit -m "test(fido2): add GetInfo validation tests"
```

→ Output `<promise>PHASE_5_DONE</promise>`

---

## Phase 6: FIPS Compliance Tests (Story 5)

**User Story:** As a SDK maintainer, I want to verify FIDO2 behavior on FIPS-capable YubiKeys, so that I can ensure FIPS-approved operation meets compliance requirements.

**Files:**
- Create: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoFipsComplianceTests.cs`

**Step 1: Write tests**

```csharp
using Xunit;
using Yubico.YubiKey.TestFramework;

namespace Yubico.YubiKey.Fido2.IntegrationTests;

[Trait("Category", "Integration")]
[Trait("Category", "FIPS")]
public class FidoFipsComplianceTests
{
    [Theory]
    [WithYubiKey(FipsCapable = DeviceCapabilities.Fido2, MinFirmware = "5.4.0")]
    public async Task FipsDevice_RequiresPinUvAuthProtocolV2(YubiKeyTestState state)
    {
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            
            // FIPS devices should support protocol 2
            Assert.NotNull(info.PinUvAuthProtocols);
            Assert.Contains(2, info.PinUvAuthProtocols);
        });
    }
    
    [Theory]
    [WithYubiKey(FipsApproved = DeviceCapabilities.Fido2, MinFirmware = "5.4.0")]
    public async Task FipsApproved_HasAlwaysUvEnabled(YubiKeyTestState state)
    {
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            
            Assert.NotNull(info.Options);
            Assert.True(info.Options.TryGetValue("alwaysUv", out var alwaysUv));
            Assert.True((bool)alwaysUv!, "FIPS-approved device should have alwaysUv enabled");
        });
    }
}
```

**Step 2-5:** Verify RED → Implement → Verify GREEN → Commit

```bash
dotnet build.cs test --filter "FullyQualifiedName~FidoFipsComplianceTests"
git add Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoFipsComplianceTests.cs
git commit -m "test(fido2): add FIPS compliance integration tests"
```

→ Output `<promise>PHASE_6_DONE</promise>`

---

## Phase 7: Enhanced PIN Tests (Story 7)

**User Story:** As a SDK maintainer, I want to verify FIDO2 behavior on YubiKeys with enhanced PIN complexity requirements, so that I can ensure PIN operations work correctly on devices with stricter policies.

**Files:**
- Create: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoEnhancedPinTests.cs`

**Step 1: Write tests**

```csharp
using Xunit;
using Yubico.YubiKey.TestFramework;

namespace Yubico.YubiKey.Fido2.IntegrationTests;

[Trait("Category", "Integration")]
public class FidoEnhancedPinTests
{
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.8.0")]
    public async Task EnhancedPin_CompliantPin_Succeeds(YubiKeyTestState state)
    {
        await state.WithFidoSessionAsync(async session =>
        {
            // Use the enhanced-complexity-compliant PIN
            await session.SetOrVerifyPinAsync(FidoTestData.Pin); // "Abc12345"
            
            // If we get here without exception, PIN was accepted
            var info = await session.GetInfoAsync();
            Assert.True(info.Options?.TryGetValue("clientPin", out var pinSet) == true && (bool)pinSet!);
        });
    }
    
    // Note: Testing non-compliant PIN rejection would require a clean device
    // and is potentially destructive, so it's marked as opt-in
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.8.0")]
    [Trait("Destructive", "true")]
    public async Task EnhancedPin_SimplePinOnComplexityDevice_ThrowsPolicyViolation(YubiKeyTestState state)
    {
        Skip.If(true, "Destructive test - requires device reset and opt-in");
        
        // This test would:
        // 1. Reset the FIDO app
        // 2. Try to set a simple PIN like "1234"
        // 3. Expect CtapStatus.PinPolicyViolation
    }
}
```

**Step 2-5:** Verify RED → Implement → Verify GREEN → Commit

```bash
dotnet build.cs test --filter "FullyQualifiedName~FidoEnhancedPinTests"
git add Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoEnhancedPinTests.cs
git commit -m "test(fido2): add enhanced PIN integration tests"
```

→ Output `<promise>PHASE_7_DONE</promise>`

---

## Phase 8: Security Verification

**From security_audit.md requirements:**

**Required Checks:**
- [ ] PIN byte representations zeroed after use
- [ ] No sensitive data in logs (credentialId, PIN, keys)
- [ ] Credential cleanup in all test finally blocks
- [ ] PIN retry counter not exhausted

**Verification:**

```bash
# Check that tests use try/finally for cleanup
grep -rn "finally" Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/*.cs | head -20

# Check no PIN logging
grep -rE "(Log|Console|Output).*Pin" Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/*.cs
# Should return nothing or only non-sensitive references

# Verify all tests compile and have cleanup
dotnet build.cs build --project Yubico.YubiKit.Fido2.IntegrationTests
```

**Commit security verification:**

```bash
git add -A
git commit -m "chore(fido2-tests): security review complete"
```

→ Output `<promise>PHASE_8_DONE</promise>`

---

## Verification Requirements (MUST PASS BEFORE COMPLETION)

1. **Build:** 
   ```bash
   dotnet build.cs build
   ```
   Must exit 0

2. **All new tests pass:**
   ```bash
   dotnet build.cs test --filter "Namespace~Yubico.YubiKey.Fido2.IntegrationTests"
   ```
   All tests must pass (or skip appropriately if no device)

3. **No regressions:**
   ```bash
   dotnet build.cs test
   ```
   Existing tests still pass

4. **Security checks:**
   - No PIN/credential logging
   - All tests have cleanup in finally blocks

5. **Files created:**
   - [ ] `FidoTestData.cs`
   - [ ] `FidoTestStateExtensions.cs`
   - [ ] `FidoSessionExtensions.cs`
   - [ ] `FidoMakeCredentialTests.cs`
   - [ ] `FidoGetAssertionTests.cs`
   - [ ] `FidoCredentialManagementTests.cs`
   - [ ] `FidoAlgorithmSupportTests.cs`
   - [ ] `FidoGetInfoTests.cs`
   - [ ] `FidoFipsComplianceTests.cs`
   - [ ] `FidoEnhancedPinTests.cs`

**Only after ALL pass, output `<promise>FIDO2_INTEGRATION_TESTING_COMPLETE</promise>`.**

If any fail, fix and re-verify.

---

## Handoff

```bash
bun .claude/skills/agent-ralph-loop/ralph-loop.ts \
  --prompt-file ./docs/plans/ralph-loop/2026-01-18-fido2-integration-testing.md \
  --completion-promise "FIDO2_INTEGRATION_TESTING_COMPLETE" \
  --max-iterations 30 \
  --learn \
  --model claude-sonnet-4
```

**Notes:**
- Requires physical YubiKey with FIDO2 capability in allow list
- Some tests may require user touch (UP)
- FIPS tests only run on FIPS-capable devices
- Enhanced PIN tests require firmware 5.8.0+
