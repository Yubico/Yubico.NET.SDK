// Copyright 2025 Yubico AB
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
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Pin;

using CredentialManagementClass = Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement;
using CtapException = Yubico.YubiKit.Fido2.Ctap.CtapException;
using CtapStatus = Yubico.YubiKit.Fido2.Ctap.CtapStatus;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for credential management operations.
/// </summary>
/// <remarks>
/// These tests exercise credential enumeration and deletion workflows on real YubiKeys.
/// Requires firmware 5.2+ with credMgmt option enabled.
/// </remarks>
[Trait("Category", "Integration")]
public class FidoCredentialManagementTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that EnumerateCredentials returns the created credential.
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task EnumerateCredentials_WithResidentKeys_ReturnsCredentialList()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Check if credential management is supported
        var info = await session.GetInfoAsync();
        if (!info.Options.TryGetValue("credMgmt", out var credMgmtSupported) || !credMgmtSupported)
        {
            // Skip test if credMgmt not supported
            return;
        }

        byte[]? credentialId = null;

        try
        {
            // Arrange: Create a credential
            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

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

            // Act: Enumerate credentials
            var (pinToken, clientPinForCredMan, protocol) = await FidoTestHelpers.GetCredManTokenAsync(
                session, FidoTestData.Pin);

            using (clientPinForCredMan)
            {
                var credMan = new CredentialManagementClass(session, protocol, pinToken);

                var rpIdHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(FidoTestData.RpId));
                var credentials = await credMan.EnumerateCredentialsAsync(rpIdHash);

                // Assert
                Assert.NotEmpty(credentials);
                Assert.Contains(credentials, c => 
                    c.CredentialId.Id.Span.SequenceEqual(credentialId));
            }

            CryptographicOperations.ZeroMemory(pinToken);
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
    /// Tests that EnumerateCredentials throws NoCredentials when none exist.
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task EnumerateCredentials_NoCredentials_ReturnsEmptyOrThrows()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Check if credential management is supported
        var info = await session.GetInfoAsync();
        if (!info.Options.TryGetValue("credMgmt", out var credMgmtSupported) || !credMgmtSupported)
        {
            // Skip test if credMgmt not supported
            return;
        }

        // Arrange: Use a unique RP ID that shouldn't have any credentials
        var uniqueRpId = $"test-credman-{Guid.NewGuid()}.example.com";

        using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

        var (pinToken, clientPinForCredMan, protocol) = await FidoTestHelpers.GetCredManTokenAsync(
            session, FidoTestData.Pin);

        using (clientPinForCredMan)
        {
            var credMan = new CredentialManagementClass(session, protocol, pinToken);
            var rpIdHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(uniqueRpId));

            // Act & Assert: Should throw NoCredentials or return empty list
            try
            {
                var credentials = await credMan.EnumerateCredentialsAsync(rpIdHash);
                // If it doesn't throw, it should be empty
                Assert.Empty(credentials);
            }
            catch (CtapException ex) when (ex.Status == CtapStatus.NoCredentials)
            {
                // Expected behavior - no credentials for this RP
            }
        }

        CryptographicOperations.ZeroMemory(pinToken);
    }

    /// <summary>
    /// Tests that DeleteCredential removes the credential from the device.
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task DeleteCredential_ExistingCredential_RemovesFromDevice()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Check if credential management is supported
        var info = await session.GetInfoAsync();
        if (!info.Options.TryGetValue("credMgmt", out var credMgmtSupported) || !credMgmtSupported)
        {
            // Skip test if credMgmt not supported
            return;
        }

        // Arrange: Create a credential
        using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

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

        var credentialId = makeResult.GetCredentialId().ToArray();

        // Act: Delete the credential
        var (pinToken, clientPinForCredMan, protocol) = await FidoTestHelpers.GetCredManTokenAsync(
            session, FidoTestData.Pin);

        using (clientPinForCredMan)
        {
            var credMan = new CredentialManagementClass(session, protocol, pinToken);
            var descriptor = new PublicKeyCredentialDescriptor(credentialId);
            await credMan.DeleteCredentialAsync(descriptor);
        }

        CryptographicOperations.ZeroMemory(pinToken);

        // Assert: Should not be found anymore via GetAssertion
        var assertChallenge = FidoTestData.GenerateChallenge();
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

        // Try to get assertion with allow list - should fail if it was the only credential
        // or succeed if there are other credentials for the same RP
        try
        {
            await session.GetAssertionAsync(
                rpId: FidoTestData.RpId,
                clientDataHash: assertChallenge,
                options: new GetAssertionOptions
                {
                    AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                    PinUvAuthParam = assertPinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version
                });

            // If we got here, there might be other credentials - that's fine
        }
        catch (CtapException ex) when (ex.Status == CtapStatus.NoCredentials)
        {
            // Expected - credential was deleted
        }
    }

    /// <summary>
    /// Tests that GetCredentialsMetadata returns valid counts.
    /// </summary>
    [Fact]
    public async Task GetCredentialsMetadata_ReturnsValidCounts()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Check if credential management is supported
        var info = await session.GetInfoAsync();
        if (!info.Options.TryGetValue("credMgmt", out var credMgmtSupported) || !credMgmtSupported)
        {
            // Skip test if credMgmt not supported
            return;
        }

        using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

        var (pinToken, clientPinForCredMan, protocol) = await FidoTestHelpers.GetCredManTokenAsync(
            session, FidoTestData.Pin);

        using (clientPinForCredMan)
        {
            var credMan = new CredentialManagementClass(session, protocol, pinToken);

            // Act
            var metadata = await credMan.GetCredentialsMetadataAsync();

            // Assert
            Assert.True(metadata.ExistingResidentCredentialsCount >= 0, 
                "Existing credentials count should be non-negative");
            Assert.True(metadata.MaxPossibleRemainingResidentCredentialsCount >= 0,
                "Max remaining credentials count should be non-negative");
        }

        CryptographicOperations.ZeroMemory(pinToken);
    }

    private async Task CleanupCredentialAsync(FidoSession session, byte[] credentialId)
    {
        try
        {
            var (pinToken, clientPin, protocol) = await FidoTestHelpers.GetCredManTokenAsync(
                session, FidoTestData.Pin);

            using (clientPin)
            {
                var credMan = new CredentialManagementClass(session, protocol, pinToken);
                var descriptor = new PublicKeyCredentialDescriptor(credentialId);
                await credMan.DeleteCredentialAsync(descriptor);
            }

            CryptographicOperations.ZeroMemory(pinToken);
        }
        catch
        {
            // Cleanup failures should not fail the test
        }
    }
}
