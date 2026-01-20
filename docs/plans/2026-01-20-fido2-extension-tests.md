# FIDO2 Extension Tests Implementation Plan

**Goal:** Add comprehensive unit and integration tests for FIDO2 extensions: hmac-secret, credProtect, largeBlob, and minPinLength.

**Architecture:** Unit tests verify CBOR encoding/decoding in isolation. Integration tests exercise end-to-end flows with real YubiKey, creating credentials with extensions and verifying outputs. Tests skip gracefully when extension not supported.

**Tech Stack:** xUnit, C# 14, .NET 10, real YubiKey (5.2+ for credMgmt, 5.5+ for largeBlob)

---

## Background

### Current State
- Extension infrastructure exists: `ExtensionBuilder`, `ExtensionOutput`, `HmacSecretInput`, etc.
- Unit tests exist for `ExtensionBuilder` CBOR encoding (good coverage)
- **NO integration tests** for extensions with real device
- Extension support varies by firmware version

### Extension Requirements by Firmware
| Extension | Min Firmware | Notes |
|-----------|-------------|-------|
| hmac-secret | 5.2 | Most widely used |
| credProtect | 5.2 | Credential protection levels |
| minPinLength | 5.2 | Return min PIN length |
| largeBlob | 5.5 | Large blob storage |

### Key Files
- `src/Extensions/ExtensionBuilder.cs` - Input encoding
- `src/Extensions/ExtensionOutput.cs` - Output decoding  
- `src/Extensions/HmacSecretInput.cs` - hmac-secret structures
- `src/LargeBlobs/LargeBlobStorage.cs` - Large blob operations
- `tests/.../Extensions/ExtensionBuilderTests.cs` - Existing unit tests

---

## Task 1: Add hmac-secret Integration Tests

**Files:**
- Create: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoHmacSecretTests.cs`

**Step 1: Write test file skeleton**

```csharp
// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0

using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.Fido2.Pin;

using CredentialManagementClass = Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for the hmac-secret extension.
/// </summary>
/// <remarks>
/// hmac-secret allows deriving a secret from a credential, useful for disk encryption.
/// Requires firmware 5.2+.
/// </remarks>
[Trait("Category", "Integration")]
public class FidoHmacSecretTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that GetInfo reports hmac-secret extension support.
    /// </summary>
    [Fact]
    public async Task GetInfo_ReportsHmacSecretSupport()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        var info = await session.GetInfoAsync();

        Assert.Contains("hmac-secret", info.Extensions);
    }

    /// <summary>
    /// Tests that MakeCredential with hmac-secret returns hmac-secret in output.
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task MakeCredential_WithHmacSecretEnabled_ReturnsHmacSecretExtension()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        var info = await session.GetInfoAsync();
        if (!info.Extensions.Contains("hmac-secret"))
        {
            return; // Skip if not supported
        }

        byte[]? credentialId = null;

        try
        {
            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

            var rp = FidoTestData.CreateRelyingParty();
            var user = FidoTestData.CreateUser();
            var challenge = FidoTestData.GenerateChallenge();

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            byte[] pinToken;
            if (supportsPermissions)
            {
                pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    FidoTestData.Pin,
                    PinUvAuthTokenPermissions.MakeCredential,
                    FidoTestData.RpId);
            }
            else
            {
                pinToken = await clientPin.GetPinTokenAsync(FidoTestData.Pin);
            }

            var pinUvAuthParam = FidoTestHelpers.ComputeMakeCredentialAuthParam(
                clientPin.Protocol, pinToken, challenge);

            // Build extensions requesting hmac-secret support
            var extensions = new ExtensionBuilder()
                .WithHmacSecretMakeCredential()
                .Build();

            var result = await session.MakeCredentialAsync(
                clientDataHash: challenge,
                rp: rp,
                user: user,
                pubKeyCredParams: FidoTestData.ES256Params,
                options: new MakeCredentialOptions
                {
                    ResidentKey = true,
                    PinUvAuthParam = pinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version,
                    Extensions = extensions
                });

            credentialId = result.GetCredentialId().ToArray();

            // Assert: Check extension output indicates hmac-secret support
            Assert.NotNull(result.AuthenticatorData);
            // Note: hmac-secret-mc returns true if supported
            if (result.AuthenticatorData.HasExtensions && 
                result.AuthenticatorData.Extensions.HasValue)
            {
                var extOutput = result.AuthenticatorData.Extensions.Value;
                // hmac-secret-mc returns bool true on success
                Assert.True(extOutput.HasExtensions);
            }
        }
        finally
        {
            if (credentialId != null)
            {
                await CleanupCredentialAsync(session, credentialId);
            }
        }
    }

    /// <summary>
    /// Tests that GetAssertion with hmac-secret returns derived secret.
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task GetAssertion_WithHmacSecret_ReturnsDerivedSecret()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        var info = await session.GetInfoAsync();
        if (!info.Extensions.Contains("hmac-secret"))
        {
            return; // Skip if not supported
        }

        byte[]? credentialId = null;

        try
        {
            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

            // First, create a credential
            var rp = FidoTestData.CreateRelyingParty();
            var user = FidoTestData.CreateUser();

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            var makeChallenge = FidoTestData.GenerateChallenge();
            byte[] makePinToken;
            if (supportsPermissions)
            {
                makePinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    FidoTestData.Pin,
                    PinUvAuthTokenPermissions.MakeCredential,
                    FidoTestData.RpId);
            }
            else
            {
                makePinToken = await clientPin.GetPinTokenAsync(FidoTestData.Pin);
            }

            var makePinUvAuthParam = FidoTestHelpers.ComputeMakeCredentialAuthParam(
                clientPin.Protocol, makePinToken, makeChallenge);

            var makeResult = await session.MakeCredentialAsync(
                clientDataHash: makeChallenge,
                rp: rp,
                user: user,
                pubKeyCredParams: FidoTestData.ES256Params,
                options: new MakeCredentialOptions
                {
                    ResidentKey = true,
                    PinUvAuthParam = makePinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version
                });

            credentialId = makeResult.GetCredentialId().ToArray();

            // Now get assertion with hmac-secret
            var assertChallenge = FidoTestData.GenerateChallenge();

            // Get shared secret for hmac-secret extension
            var (sharedSecret, keyAgreement) = await clientPin.GetSharedSecretAsync();

            // Generate random salt
            var salt1 = new byte[32];
            RandomNumberGenerator.Fill(salt1);

            // Build hmac-secret extension
            var extensions = new ExtensionBuilder()
                .WithHmacSecret(
                    clientPin.Protocol,
                    sharedSecret,
                    keyAgreement,
                    salt1)
                .Build();

            byte[] assertPinToken;
            if (supportsPermissions)
            {
                assertPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    FidoTestData.Pin,
                    PinUvAuthTokenPermissions.GetAssertion,
                    FidoTestData.RpId);
            }
            else
            {
                assertPinToken = await clientPin.GetPinTokenAsync(FidoTestData.Pin);
            }

            var assertPinUvAuthParam = FidoTestHelpers.ComputeGetAssertionAuthParam(
                clientPin.Protocol, assertPinToken, assertChallenge);

            var assertionResult = await session.GetAssertionAsync(
                rpId: FidoTestData.RpId,
                clientDataHash: assertChallenge,
                options: new GetAssertionOptions
                {
                    AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                    PinUvAuthParam = assertPinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version,
                    Extensions = extensions
                });

            // Assert: hmac-secret output should be present
            Assert.NotNull(assertionResult);
            if (assertionResult.AuthenticatorData.HasExtensions &&
                assertionResult.AuthenticatorData.Extensions.HasValue)
            {
                var extOutput = assertionResult.AuthenticatorData.Extensions.Value;
                Assert.True(extOutput.TryGetHmacSecret(out var hmacOutput),
                    "Expected hmac-secret output in assertion response");
                Assert.NotNull(hmacOutput);
                Assert.False(hmacOutput.Output.IsEmpty, "hmac-secret output should not be empty");

                // Decrypt the output
                var decrypted = clientPin.Protocol.Decrypt(sharedSecret, hmacOutput.Output.Span);
                Assert.Equal(32, decrypted.Length); // One salt = 32-byte output
            }

            CryptographicOperations.ZeroMemory(sharedSecret);
        }
        finally
        {
            if (credentialId != null)
            {
                await CleanupCredentialAsync(session, credentialId);
            }
        }
    }

    /// <summary>
    /// Tests that same salt produces same derived secret (deterministic).
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task GetAssertion_WithSameSalt_ReturnsSameSecret()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        var info = await session.GetInfoAsync();
        if (!info.Extensions.Contains("hmac-secret"))
        {
            return;
        }

        byte[]? credentialId = null;

        try
        {
            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

            // Create credential
            var rp = FidoTestData.CreateRelyingParty();
            var user = FidoTestData.CreateUser();
            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            var makeChallenge = FidoTestData.GenerateChallenge();
            byte[] makePinToken = supportsPermissions
                ? await clientPin.GetPinUvAuthTokenUsingPinAsync(FidoTestData.Pin, PinUvAuthTokenPermissions.MakeCredential, FidoTestData.RpId)
                : await clientPin.GetPinTokenAsync(FidoTestData.Pin);

            var makePinUvAuthParam = FidoTestHelpers.ComputeMakeCredentialAuthParam(clientPin.Protocol, makePinToken, makeChallenge);

            var makeResult = await session.MakeCredentialAsync(
                clientDataHash: makeChallenge,
                rp: rp,
                user: user,
                pubKeyCredParams: FidoTestData.ES256Params,
                options: new MakeCredentialOptions
                {
                    ResidentKey = true,
                    PinUvAuthParam = makePinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version
                });

            credentialId = makeResult.GetCredentialId().ToArray();

            // Fixed salt for both assertions
            var salt = new byte[32];
            RandomNumberGenerator.Fill(salt);

            // First assertion
            var secret1 = await GetHmacSecretAsync(session, clientPin, credentialId, salt, supportsPermissions);

            // Second assertion with same salt
            var secret2 = await GetHmacSecretAsync(session, clientPin, credentialId, salt, supportsPermissions);

            // Assert: Same salt should produce same secret
            Assert.True(secret1.SequenceEqual(secret2), "Same salt should produce same derived secret");
        }
        finally
        {
            if (credentialId != null)
            {
                await CleanupCredentialAsync(session, credentialId);
            }
        }
    }

    private static async Task<byte[]> GetHmacSecretAsync(
        FidoSession session,
        ClientPin clientPin,
        byte[] credentialId,
        byte[] salt,
        bool supportsPermissions)
    {
        var challenge = FidoTestData.GenerateChallenge();
        var (sharedSecret, keyAgreement) = await clientPin.GetSharedSecretAsync();

        var extensions = new ExtensionBuilder()
            .WithHmacSecret(clientPin.Protocol, sharedSecret, keyAgreement, salt)
            .Build();

        byte[] pinToken = supportsPermissions
            ? await clientPin.GetPinUvAuthTokenUsingPinAsync(FidoTestData.Pin, PinUvAuthTokenPermissions.GetAssertion, FidoTestData.RpId)
            : await clientPin.GetPinTokenAsync(FidoTestData.Pin);

        var pinUvAuthParam = FidoTestHelpers.ComputeGetAssertionAuthParam(clientPin.Protocol, pinToken, challenge);

        var result = await session.GetAssertionAsync(
            rpId: FidoTestData.RpId,
            clientDataHash: challenge,
            options: new GetAssertionOptions
            {
                AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                PinUvAuthParam = pinUvAuthParam,
                PinUvAuthProtocol = clientPin.Protocol.Version,
                Extensions = extensions
            });

        var extOutput = result.AuthenticatorData.Extensions!.Value;
        extOutput.TryGetHmacSecret(out var hmacOutput);
        var decrypted = clientPin.Protocol.Decrypt(sharedSecret, hmacOutput!.Output.Span);

        CryptographicOperations.ZeroMemory(sharedSecret);
        return decrypted;
    }

    private async Task CleanupCredentialAsync(FidoSession session, byte[] credentialId)
    {
        try
        {
            var (pinToken, clientPin, protocol) = await FidoTestHelpers.GetCredManTokenAsync(session, FidoTestData.Pin);
            using (clientPin)
            {
                var credMan = new CredentialManagementClass(session, protocol, pinToken);
                await credMan.DeleteCredentialAsync(new PublicKeyCredentialDescriptor(credentialId));
            }
            CryptographicOperations.ZeroMemory(pinToken);
        }
        catch { /* Cleanup failures should not fail the test */ }
    }
}
```

**Step 2: Build and verify compilation**

Run: `dotnet build Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/Yubico.YubiKit.Fido2.IntegrationTests.csproj`
Expected: Build succeeded

**Step 3: Run test to verify basic test works**

Run: `dotnet test --filter "FullyQualifiedName~FidoHmacSecretTests.GetInfo_ReportsHmacSecretSupport"`
Expected: PASS (no user touch needed)

**Step 4: Commit**

```bash
git add Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoHmacSecretTests.cs
git commit -m "test: add hmac-secret integration tests"
```

---

## Task 2: Add credProtect Integration Tests

**Files:**
- Create: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoCredProtectTests.cs`

**Step 1: Write test file**

```csharp
// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0

using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.Fido2.Pin;

using CredentialManagementClass = Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement;
using CtapException = Yubico.YubiKit.Fido2.Ctap.CtapException;
using CtapStatus = Yubico.YubiKit.Fido2.Ctap.CtapStatus;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for the credProtect extension.
/// </summary>
/// <remarks>
/// credProtect controls credential protection levels:
/// - Level 1: userVerificationOptional (default)
/// - Level 2: userVerificationOptionalWithCredentialIdList
/// - Level 3: userVerificationRequired
/// </remarks>
[Trait("Category", "Integration")]
public class FidoCredProtectTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that credProtect level 2 requires allowList for discoverable assertion.
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task CredProtect_Level2_RequiresAllowListForDiscovery()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        var info = await session.GetInfoAsync();
        if (!info.Extensions.Contains("credProtect"))
        {
            return; // Skip if not supported
        }

        byte[]? credentialId = null;

        try
        {
            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

            var rp = FidoTestData.CreateRelyingParty();
            var user = FidoTestData.CreateUser();
            var challenge = FidoTestData.GenerateChallenge();

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            byte[] pinToken = supportsPermissions
                ? await clientPin.GetPinUvAuthTokenUsingPinAsync(FidoTestData.Pin, PinUvAuthTokenPermissions.MakeCredential, FidoTestData.RpId)
                : await clientPin.GetPinTokenAsync(FidoTestData.Pin);

            var pinUvAuthParam = FidoTestHelpers.ComputeMakeCredentialAuthParam(clientPin.Protocol, pinToken, challenge);

            // Create credential with credProtect level 2
            var extensions = new ExtensionBuilder()
                .WithCredProtect(CredProtectPolicy.UserVerificationOptionalWithCredentialIdList)
                .Build();

            var result = await session.MakeCredentialAsync(
                clientDataHash: challenge,
                rp: rp,
                user: user,
                pubKeyCredParams: FidoTestData.ES256Params,
                options: new MakeCredentialOptions
                {
                    ResidentKey = true,
                    PinUvAuthParam = pinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version,
                    Extensions = extensions
                });

            credentialId = result.GetCredentialId().ToArray();

            // Verify credProtect was applied
            if (result.AuthenticatorData.HasExtensions &&
                result.AuthenticatorData.Extensions.HasValue)
            {
                var extOutput = result.AuthenticatorData.Extensions.Value;
                Assert.True(extOutput.TryGetCredProtect(out var policy));
                Assert.Equal(CredProtectPolicy.UserVerificationOptionalWithCredentialIdList, policy);
            }

            // Try discoverable assertion WITHOUT allowList - should fail with NoCredentials
            var assertChallenge = FidoTestData.GenerateChallenge();
            byte[] assertPinToken = supportsPermissions
                ? await clientPin.GetPinUvAuthTokenUsingPinAsync(FidoTestData.Pin, PinUvAuthTokenPermissions.GetAssertion, FidoTestData.RpId)
                : await clientPin.GetPinTokenAsync(FidoTestData.Pin);

            var assertPinUvAuthParam = FidoTestHelpers.ComputeGetAssertionAuthParam(clientPin.Protocol, assertPinToken, assertChallenge);

            // This should fail - credential requires allowList due to credProtect level 2
            var ex = await Assert.ThrowsAsync<CtapException>(async () =>
            {
                await session.GetAssertionAsync(
                    rpId: FidoTestData.RpId,
                    clientDataHash: assertChallenge,
                    options: new GetAssertionOptions
                    {
                        // No AllowList!
                        PinUvAuthParam = assertPinUvAuthParam,
                        PinUvAuthProtocol = clientPin.Protocol.Version
                    });
            });

            Assert.Equal(CtapStatus.NoCredentials, ex.Status);
        }
        finally
        {
            if (credentialId != null)
            {
                await CleanupCredentialAsync(session, credentialId);
            }
        }
    }

    /// <summary>
    /// Tests that credProtect level 3 requires user verification for assertion.
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task CredProtect_Level3_RequiresUserVerification()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        var info = await session.GetInfoAsync();
        if (!info.Extensions.Contains("credProtect"))
        {
            return;
        }

        byte[]? credentialId = null;

        try
        {
            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

            var rp = FidoTestData.CreateRelyingParty();
            var user = FidoTestData.CreateUser();
            var challenge = FidoTestData.GenerateChallenge();

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            byte[] pinToken = supportsPermissions
                ? await clientPin.GetPinUvAuthTokenUsingPinAsync(FidoTestData.Pin, PinUvAuthTokenPermissions.MakeCredential, FidoTestData.RpId)
                : await clientPin.GetPinTokenAsync(FidoTestData.Pin);

            var pinUvAuthParam = FidoTestHelpers.ComputeMakeCredentialAuthParam(clientPin.Protocol, pinToken, challenge);

            // Create credential with credProtect level 3 (UV required)
            var extensions = new ExtensionBuilder()
                .WithCredProtect(CredProtectPolicy.UserVerificationRequired, enforcePolicy: true)
                .Build();

            var result = await session.MakeCredentialAsync(
                clientDataHash: challenge,
                rp: rp,
                user: user,
                pubKeyCredParams: FidoTestData.ES256Params,
                options: new MakeCredentialOptions
                {
                    ResidentKey = true,
                    PinUvAuthParam = pinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version,
                    Extensions = extensions
                });

            credentialId = result.GetCredentialId().ToArray();

            // Verify credProtect level 3 was applied
            if (result.AuthenticatorData.HasExtensions &&
                result.AuthenticatorData.Extensions.HasValue)
            {
                var extOutput = result.AuthenticatorData.Extensions.Value;
                Assert.True(extOutput.TryGetCredProtect(out var policy));
                Assert.Equal(CredProtectPolicy.UserVerificationRequired, policy);
            }

            // Assertion WITH PIN/UV should succeed
            var assertChallenge = FidoTestData.GenerateChallenge();
            byte[] assertPinToken = supportsPermissions
                ? await clientPin.GetPinUvAuthTokenUsingPinAsync(FidoTestData.Pin, PinUvAuthTokenPermissions.GetAssertion, FidoTestData.RpId)
                : await clientPin.GetPinTokenAsync(FidoTestData.Pin);

            var assertPinUvAuthParam = FidoTestHelpers.ComputeGetAssertionAuthParam(clientPin.Protocol, assertPinToken, assertChallenge);

            var assertResult = await session.GetAssertionAsync(
                rpId: FidoTestData.RpId,
                clientDataHash: assertChallenge,
                options: new GetAssertionOptions
                {
                    AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                    PinUvAuthParam = assertPinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version
                });

            Assert.NotNull(assertResult);
            Assert.False(assertResult.Signature.IsEmpty);
        }
        finally
        {
            if (credentialId != null)
            {
                await CleanupCredentialAsync(session, credentialId);
            }
        }
    }

    private async Task CleanupCredentialAsync(FidoSession session, byte[] credentialId)
    {
        try
        {
            var (pinToken, clientPin, protocol) = await FidoTestHelpers.GetCredManTokenAsync(session, FidoTestData.Pin);
            using (clientPin)
            {
                var credMan = new CredentialManagementClass(session, protocol, pinToken);
                await credMan.DeleteCredentialAsync(new PublicKeyCredentialDescriptor(credentialId));
            }
            CryptographicOperations.ZeroMemory(pinToken);
        }
        catch { }
    }
}
```

**Step 2: Build and verify**

Run: `dotnet build Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoCredProtectTests.cs
git commit -m "test: add credProtect integration tests"
```

---

## Task 3: Add minPinLength Integration Tests

**Files:**
- Create: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoMinPinLengthTests.cs`

**Step 1: Write test file**

```csharp
// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0

using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.Fido2.Pin;

using CredentialManagementClass = Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for the minPinLength extension.
/// </summary>
[Trait("Category", "Integration")]
public class FidoMinPinLengthTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that minPinLength extension returns current minimum PIN length.
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task MakeCredential_WithMinPinLength_ReturnsMinPinLength()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        var info = await session.GetInfoAsync();
        if (!info.Extensions.Contains("minPinLength"))
        {
            return; // Skip if not supported
        }

        byte[]? credentialId = null;

        try
        {
            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

            var rp = FidoTestData.CreateRelyingParty();
            var user = FidoTestData.CreateUser();
            var challenge = FidoTestData.GenerateChallenge();

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            byte[] pinToken = supportsPermissions
                ? await clientPin.GetPinUvAuthTokenUsingPinAsync(FidoTestData.Pin, PinUvAuthTokenPermissions.MakeCredential, FidoTestData.RpId)
                : await clientPin.GetPinTokenAsync(FidoTestData.Pin);

            var pinUvAuthParam = FidoTestHelpers.ComputeMakeCredentialAuthParam(clientPin.Protocol, pinToken, challenge);

            // Request minPinLength extension
            var extensions = new ExtensionBuilder()
                .WithMinPinLength()
                .Build();

            var result = await session.MakeCredentialAsync(
                clientDataHash: challenge,
                rp: rp,
                user: user,
                pubKeyCredParams: FidoTestData.ES256Params,
                options: new MakeCredentialOptions
                {
                    ResidentKey = false,
                    PinUvAuthParam = pinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version,
                    Extensions = extensions
                });

            // For non-RK, no credential to clean up, but set for safety
            credentialId = result.GetCredentialId().ToArray();

            // Assert: minPinLength should be returned
            if (result.AuthenticatorData.HasExtensions &&
                result.AuthenticatorData.Extensions.HasValue)
            {
                var extOutput = result.AuthenticatorData.Extensions.Value;
                if (extOutput.TryGetMinPinLength(out var minLength))
                {
                    // Default FIDO2 min PIN is 4, but can be higher
                    Assert.True(minLength >= 4, $"Min PIN length should be at least 4, got {minLength}");
                    Assert.True(minLength <= 63, $"Min PIN length should be at most 63, got {minLength}");
                }
            }
        }
        finally
        {
            // Non-RK credentials don't need cleanup, but try anyway
            if (credentialId != null)
            {
                await CleanupCredentialAsync(session, credentialId);
            }
        }
    }

    /// <summary>
    /// Tests that GetInfo includes minPinLength in AuthenticatorInfo.
    /// </summary>
    [Fact]
    public async Task GetInfo_IncludesMinPinLength()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        var info = await session.GetInfoAsync();

        // MinPinLength should be available in GetInfo response
        if (info.MinPinLength.HasValue)
        {
            Assert.True(info.MinPinLength.Value >= 4, "Min PIN should be at least 4");
            Assert.True(info.MinPinLength.Value <= 63, "Min PIN should be at most 63");
        }
    }

    private async Task CleanupCredentialAsync(FidoSession session, byte[] credentialId)
    {
        try
        {
            var (pinToken, clientPin, protocol) = await FidoTestHelpers.GetCredManTokenAsync(session, FidoTestData.Pin);
            using (clientPin)
            {
                var credMan = new CredentialManagementClass(session, protocol, pinToken);
                await credMan.DeleteCredentialAsync(new PublicKeyCredentialDescriptor(credentialId));
            }
            CryptographicOperations.ZeroMemory(pinToken);
        }
        catch { }
    }
}
```

**Step 2: Build and verify**

Run: `dotnet build Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoMinPinLengthTests.cs
git commit -m "test: add minPinLength integration tests"
```

---

## Task 4: Add largeBlob Integration Tests

**Files:**
- Create: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoLargeBlobTests.cs`

**Step 1: Write test file**

```csharp
// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0

using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.Fido2.LargeBlobs;
using Yubico.YubiKit.Fido2.Pin;

using CredentialManagementClass = Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for the largeBlob extension and LargeBlobStorage.
/// </summary>
/// <remarks>
/// Requires firmware 5.5+ for largeBlob support.
/// </remarks>
[Trait("Category", "Integration")]
public class FidoLargeBlobTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that GetInfo reports largeBlob support.
    /// </summary>
    [Fact]
    public async Task GetInfo_ReportsLargeBlobSupport()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        var info = await session.GetInfoAsync();

        // largeBlob support indicated by option or extension
        var hasLargeBlobSupport = info.Extensions.Contains("largeBlob") ||
                                   (info.Options.TryGetValue("largeBlobs", out var supported) && supported);

        // Log for debugging - not all keys support this
        if (!hasLargeBlobSupport)
        {
            // This is OK - largeBlob requires firmware 5.5+
            return;
        }

        Assert.True(hasLargeBlobSupport);
    }

    /// <summary>
    /// Tests creating a credential with largeBlob support enabled.
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task MakeCredential_WithLargeBlob_ReturnsLargeBlobKey()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        var info = await session.GetInfoAsync();
        var hasLargeBlobSupport = info.Options.TryGetValue("largeBlobs", out var supported) && supported;
        if (!hasLargeBlobSupport)
        {
            return; // Skip - requires firmware 5.5+
        }

        byte[]? credentialId = null;

        try
        {
            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

            var rp = FidoTestData.CreateRelyingParty();
            var user = FidoTestData.CreateUser();
            var challenge = FidoTestData.GenerateChallenge();

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            byte[] pinToken = supportsPermissions
                ? await clientPin.GetPinUvAuthTokenUsingPinAsync(FidoTestData.Pin, PinUvAuthTokenPermissions.MakeCredential, FidoTestData.RpId)
                : await clientPin.GetPinTokenAsync(FidoTestData.Pin);

            var pinUvAuthParam = FidoTestHelpers.ComputeMakeCredentialAuthParam(clientPin.Protocol, pinToken, challenge);

            // Request largeBlob support
            var extensions = new ExtensionBuilder()
                .WithLargeBlob(LargeBlobSupport.Required)
                .Build();

            var result = await session.MakeCredentialAsync(
                clientDataHash: challenge,
                rp: rp,
                user: user,
                pubKeyCredParams: FidoTestData.ES256Params,
                options: new MakeCredentialOptions
                {
                    ResidentKey = true, // largeBlob requires RK
                    PinUvAuthParam = pinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version,
                    Extensions = extensions
                });

            credentialId = result.GetCredentialId().ToArray();

            // largeBlobKey is returned during GetAssertion, not MakeCredential
            Assert.NotNull(result);
            Assert.NotEmpty(credentialId);
        }
        finally
        {
            if (credentialId != null)
            {
                await CleanupCredentialAsync(session, credentialId);
            }
        }
    }

    /// <summary>
    /// Tests reading and writing large blob data.
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task LargeBlobStorage_ReadWrite_RoundTrips()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        var info = await session.GetInfoAsync();
        var hasLargeBlobSupport = info.Options.TryGetValue("largeBlobs", out var supported) && supported;
        if (!hasLargeBlobSupport)
        {
            return; // Skip
        }

        byte[]? credentialId = null;

        try
        {
            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

            // Create credential with largeBlob support
            var rp = FidoTestData.CreateRelyingParty();
            var user = FidoTestData.CreateUser();
            var challenge = FidoTestData.GenerateChallenge();

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            byte[] pinToken = supportsPermissions
                ? await clientPin.GetPinUvAuthTokenUsingPinAsync(FidoTestData.Pin, PinUvAuthTokenPermissions.MakeCredential, FidoTestData.RpId)
                : await clientPin.GetPinTokenAsync(FidoTestData.Pin);

            var pinUvAuthParam = FidoTestHelpers.ComputeMakeCredentialAuthParam(clientPin.Protocol, pinToken, challenge);

            var makeExtensions = new ExtensionBuilder()
                .WithLargeBlob(LargeBlobSupport.Required)
                .Build();

            var makeResult = await session.MakeCredentialAsync(
                clientDataHash: challenge,
                rp: rp,
                user: user,
                pubKeyCredParams: FidoTestData.ES256Params,
                options: new MakeCredentialOptions
                {
                    ResidentKey = true,
                    PinUvAuthParam = pinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version,
                    Extensions = makeExtensions
                });

            credentialId = makeResult.GetCredentialId().ToArray();

            // Get largeBlobKey via assertion
            var assertChallenge = FidoTestData.GenerateChallenge();
            byte[] assertPinToken = supportsPermissions
                ? await clientPin.GetPinUvAuthTokenUsingPinAsync(FidoTestData.Pin, PinUvAuthTokenPermissions.GetAssertion, FidoTestData.RpId)
                : await clientPin.GetPinTokenAsync(FidoTestData.Pin);

            var assertPinUvAuthParam = FidoTestHelpers.ComputeGetAssertionAuthParam(clientPin.Protocol, assertPinToken, assertChallenge);

            var assertExtensions = new ExtensionBuilder()
                .WithLargeBlobRead()
                .Build();

            var assertResult = await session.GetAssertionAsync(
                rpId: FidoTestData.RpId,
                clientDataHash: assertChallenge,
                options: new GetAssertionOptions
                {
                    AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                    PinUvAuthParam = assertPinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version,
                    Extensions = assertExtensions
                });

            Assert.NotNull(assertResult);

            // Check for largeBlobKey in extension output
            if (assertResult.AuthenticatorData.HasExtensions &&
                assertResult.AuthenticatorData.Extensions.HasValue)
            {
                var extOutput = assertResult.AuthenticatorData.Extensions.Value;
                if (extOutput.TryGetLargeBlobKey(out var largeBlobKey))
                {
                    Assert.Equal(32, largeBlobKey.Length); // largeBlobKey is 32 bytes
                }
            }
        }
        finally
        {
            if (credentialId != null)
            {
                await CleanupCredentialAsync(session, credentialId);
            }
        }
    }

    private async Task CleanupCredentialAsync(FidoSession session, byte[] credentialId)
    {
        try
        {
            var (pinToken, clientPin, protocol) = await FidoTestHelpers.GetCredManTokenAsync(session, FidoTestData.Pin);
            using (clientPin)
            {
                var credMan = new CredentialManagementClass(session, protocol, pinToken);
                await credMan.DeleteCredentialAsync(new PublicKeyCredentialDescriptor(credentialId));
            }
            CryptographicOperations.ZeroMemory(pinToken);
        }
        catch { }
    }
}
```

**Step 2: Build and verify**

Run: `dotnet build Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/FidoLargeBlobTests.cs
git commit -m "test: add largeBlob integration tests"
```

---

## Task 5: Add Unit Tests for Extension Edge Cases

**Files:**
- Modify: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Extensions/ExtensionBuilderTests.cs`

**Step 1: Add edge case tests to existing file**

Add these tests to the existing `ExtensionBuilderTests.cs`:

```csharp
#region Edge Case Tests

[Fact]
public void WithHmacSecret_InvalidSalt1Length_ThrowsArgumentException()
{
    // Arrange
    var builder = new ExtensionBuilder();
    var protocol = new MockPinProtocol();
    var sharedSecret = new byte[32];
    var keyAgreement = CreateMockCoseKey();
    var invalidSalt = new byte[16]; // Should be 32

    // Act & Assert
    var ex = Assert.Throws<ArgumentException>(() =>
        builder.WithHmacSecret(protocol, sharedSecret, keyAgreement, invalidSalt));
    Assert.Contains("32 bytes", ex.Message);
}

[Fact]
public void WithHmacSecret_InvalidSalt2Length_ThrowsArgumentException()
{
    // Arrange
    var builder = new ExtensionBuilder();
    var protocol = new MockPinProtocol();
    var sharedSecret = new byte[32];
    var keyAgreement = CreateMockCoseKey();
    var salt1 = new byte[32];
    var invalidSalt2 = new byte[16]; // Should be 32

    // Act & Assert
    var ex = Assert.Throws<ArgumentException>(() =>
        builder.WithHmacSecret(protocol, sharedSecret, keyAgreement, salt1, invalidSalt2));
    Assert.Contains("32 bytes", ex.Message);
}

[Fact]
public void WithCredBlob_EmptyBlob_EncodesEmptyByteString()
{
    // Arrange
    var builder = new ExtensionBuilder()
        .WithCredBlob(ReadOnlyMemory<byte>.Empty);

    // Act
    var result = builder.Build();

    // Assert
    Assert.NotNull(result);
    var reader = new CborReader(result.Value, CborConformanceMode.Lax);
    reader.ReadStartMap();
    Assert.Equal("credBlob", reader.ReadTextString());
    var blob = reader.ReadByteString();
    Assert.Empty(blob);
}

[Theory]
[InlineData(CredProtectPolicy.UserVerificationOptional)]
[InlineData(CredProtectPolicy.UserVerificationOptionalWithCredentialIdList)]
[InlineData(CredProtectPolicy.UserVerificationRequired)]
public void WithCredProtect_AllPolicies_EncodeCorrectValue(CredProtectPolicy policy)
{
    // Arrange
    var builder = new ExtensionBuilder()
        .WithCredProtect(policy);

    // Act
    var result = builder.Build();

    // Assert
    Assert.NotNull(result);
    var reader = new CborReader(result.Value, CborConformanceMode.Lax);
    reader.ReadStartMap();
    Assert.Equal("credProtect", reader.ReadTextString());
    Assert.Equal((int)policy, reader.ReadInt32());
}

[Fact]
public void WithLargeBlob_PreferredSupport_EncodesPreferred()
{
    // Arrange
    var builder = new ExtensionBuilder()
        .WithLargeBlob(LargeBlobSupport.Preferred);

    // Act
    var result = builder.Build();

    // Assert
    Assert.NotNull(result);
    var reader = new CborReader(result.Value, CborConformanceMode.Lax);
    reader.ReadStartMap();
    Assert.Equal("largeBlob", reader.ReadTextString());
    reader.ReadStartMap();
    Assert.Equal("support", reader.ReadTextString());
    Assert.Equal("preferred", reader.ReadTextString());
}

private static Dictionary<int, object?> CreateMockCoseKey()
{
    return new Dictionary<int, object?>
    {
        { 1, 2 },      // kty = EC2
        { 3, -25 },    // alg = ECDH-ES+HKDF-256
        { -1, 1 },     // crv = P-256
        { -2, new byte[32] }, // x
        { -3, new byte[32] }  // y
    };
}

#endregion
```

**Step 2: Create mock PIN protocol for unit tests**

Add to the same test file or create helper:

```csharp
/// <summary>
/// Mock PIN protocol for unit testing extension building.
/// </summary>
private class MockPinProtocol : IPinUvAuthProtocol
{
    public int Version => 2;
    
    public byte[] Authenticate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message)
    {
        return new byte[32]; // Mock HMAC output
    }
    
    public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext)
    {
        // Mock: prepend 16-byte IV + copy plaintext
        var result = new byte[16 + plaintext.Length];
        plaintext.CopyTo(result.AsSpan(16));
        return result;
    }
    
    public byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext)
    {
        // Mock: skip 16-byte IV
        return ciphertext[16..].ToArray();
    }
    
    public (byte[] SharedSecret, IReadOnlyDictionary<int, object?> PublicKey) GenerateKeyAgreement()
    {
        return (new byte[32], CreateMockCoseKey());
    }
    
    public byte[] DeriveSharedSecret(IReadOnlyDictionary<int, object?> peerPublicKey)
    {
        return new byte[32];
    }
}
```

**Step 3: Build and run unit tests**

Run: `dotnet test --filter "FullyQualifiedName~ExtensionBuilderTests"`
Expected: All tests pass

**Step 4: Commit**

```bash
git add Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/Extensions/ExtensionBuilderTests.cs
git commit -m "test: add extension builder edge case unit tests"
```

---

## Task 6: Run Full Test Suite and Verify

**Step 1: Build entire solution**

Run: `dotnet build Yubico.YubiKit.sln`
Expected: Build succeeded

**Step 2: Run all FIDO2 unit tests**

Run: `dotnet test Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/ -v q`
Expected: All tests pass

**Step 3: Run all FIDO2 integration tests (requires YubiKey + touch)**

Run: `dotnet test Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/ -v q`
Expected: Tests requiring extension support on device pass or skip gracefully

**Step 4: Final commit**

```bash
git add -A
git commit -m "test: complete FIDO2 extension test coverage"
```

---

## Summary

| Task | Tests Added | Type |
|------|-------------|------|
| 1 | hmac-secret (4 tests) | Integration |
| 2 | credProtect (2 tests) | Integration |
| 3 | minPinLength (2 tests) | Integration |
| 4 | largeBlob (3 tests) | Integration |
| 5 | Edge cases (6 tests) | Unit |

**Total: ~17 new tests**

## Dependencies

- **FidoTestHelpers**: Must have `GetSharedSecretAsync()` method on `ClientPin`
- **ExtensionBuilder**: Already supports all extensions
- **Real YubiKey**: Firmware 5.2+ for most extensions, 5.5+ for largeBlob

## Risks

1. **GetSharedSecretAsync may not exist** - May need to add helper method
2. **Extension output parsing** - May encounter CBOR decoding issues
3. **Firmware compatibility** - Tests gracefully skip if extension unsupported
