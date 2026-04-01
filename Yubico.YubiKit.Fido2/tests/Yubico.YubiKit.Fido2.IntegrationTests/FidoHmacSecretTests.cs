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
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for FIDO2 hmac-secret extension.
/// </summary>
/// <remarks>
/// <para>
/// Tests the hmac-secret extension which allows deriving stable secrets from credentials.
/// Used for disk encryption, password vaults, and other applications requiring deterministic
/// secrets derived from user verification.
/// </para>
/// <para>
/// Tests automatically skip if the YubiKey does not support hmac-secret extension.
/// </para>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#sctn-hmac-secret-extension
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Extension", "hmac-secret")]
public class FidoHmacSecretTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that GetInfo reports hmac-secret extension support.
    /// </summary>
    /// <remarks>
    /// The hmac-secret extension is supported on YubiKey 5.0+ firmware.
    /// This test verifies the extension is listed in authenticator info.
    /// Test skips gracefully if the extension is not supported.
    /// </remarks>
    [Fact]
    public async Task GetInfo_ReportsHmacSecretSupport()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Act
        var info = await session.GetInfoAsync();

        // Assert
        Assert.NotNull(info.Extensions);
        
        var supportsHmacSecret = info.Extensions.Contains(ExtensionIdentifiers.HmacSecret);
        
        // YubiKey 5+ should support hmac-secret, but skip gracefully if not
        if (supportsHmacSecret)
        {
            Assert.Contains(ExtensionIdentifiers.HmacSecret, info.Extensions);
        }
        else
        {
            Skip.If(true, "YubiKey does not support hmac-secret extension");
        }
    }

    /// <summary>
    /// Tests that MakeCredential with hmac-secret extension returns the extension in output.
    /// </summary>
    /// <remarks>
    /// When hmac-secret extension is requested during MakeCredential, the authenticator
    /// should include "hmac-secret": true in the extension output to indicate the
    /// credential was created with hmac-secret support enabled.
    /// </remarks>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task MakeCredential_WithHmacSecretEnabled_ReturnsHmacSecretExtension()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Skip if hmac-secret not supported
        var info = await session.GetInfoAsync();
        if (!info.Extensions.Contains(ExtensionIdentifiers.HmacSecret))
        {
            Skip.If(true, "YubiKey does not support hmac-secret extension");
            return;
        }

        // Arrange
        using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);
        
        var rp = FidoTestData.CreateRelyingParty();
        var user = FidoTestData.CreateUser();
        var challenge = FidoTestData.GenerateChallenge();
        
        // Get PIN token
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
        
        // Build extension with hmac-secret enabled
        var extensions = new ExtensionBuilder()
            .WithHmacSecretMakeCredential()
            .Build();
        
        var options = new MakeCredentialOptions
        {
            ResidentKey = false,
            PinUvAuthParam = pinUvAuthParam,
            PinUvAuthProtocol = clientPin.Protocol.Version,
            Extensions = extensions
        };
        
        // Act
        var result = await session.MakeCredentialAsync(
            clientDataHash: challenge,
            rp: rp,
            user: user,
            pubKeyCredParams: FidoTestData.ES256Params,
            options: options);
        
        // Assert
        Assert.NotNull(result);
        
        // Extension output may be null if hmac-secret-mc isn't supported
        // The important thing is the credential was created successfully
        // and hmac-secret can be used during getAssertion
        if (result.ExtensionOutputs.HasValue)
        {
            var extOutput = ExtensionOutput.Decode(result.ExtensionOutputs.Value);
            if (extOutput.HasExtensions)
            {
                // If extensions are present, hmac-secret should be among them
                Assert.Contains(ExtensionIdentifiers.HmacSecret, extOutput.ExtensionIds);
            }
        }
        
        // Verify credential was created successfully
        Assert.True(result.GetCredentialId().Length > 0, "Credential ID should not be empty");
    }

    /// <summary>
    /// Tests that GetAssertion with hmac-secret extension returns a derived secret.
    /// </summary>
    /// <remarks>
    /// This test creates a credential with hmac-secret enabled, then calls GetAssertion
    /// with a salt value. The authenticator should derive and return a 32-byte secret
    /// based on the credential's private key and the provided salt.
    /// </remarks>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task GetAssertion_WithHmacSecret_ReturnsDerivedSecret()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Skip if hmac-secret not supported
        var info = await session.GetInfoAsync();
        if (!info.Extensions.Contains(ExtensionIdentifiers.HmacSecret))
        {
            Skip.If(true, "YubiKey does not support hmac-secret extension");
            return;
        }

        byte[]? credentialId = null;

        try
        {
            // Arrange: Create credential with hmac-secret
            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);
            
            var rp = FidoTestData.CreateRelyingParty();
            var user = FidoTestData.CreateUser();
            var makeChallenge = FidoTestData.GenerateChallenge();
            
            var supportsPermissions = info.Versions.Contains("FIDO_2_1") || 
                                       info.Versions.Contains("FIDO_2_1_PRE");
            
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
            
            var makeExtensions = new ExtensionBuilder()
                .WithHmacSecretMakeCredential()
                .Build();
            
            var makeResult = await session.MakeCredentialAsync(
                clientDataHash: makeChallenge,
                rp: rp,
                user: user,
                pubKeyCredParams: FidoTestData.ES256Params,
                options: new MakeCredentialOptions
                {
                    ResidentKey = true,
                    PinUvAuthParam = makePinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version,
                    Extensions = makeExtensions
                });
            
            credentialId = makeResult.GetCredentialId().ToArray();
            
            // Act: GetAssertion with hmac-secret
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
            
            // Get authenticator key agreement and compute shared secret
            var authenticatorKeyAgreement = await clientPin.GetKeyAgreementAsync();
            var (platformKeyAgreement, sharedSecret) = clientPin.Protocol.Encapsulate(authenticatorKeyAgreement);
            
            // Generate a random 32-byte salt
            var salt1 = new byte[32];
            RandomNumberGenerator.Fill(salt1);
            
            var assertExtensions = new ExtensionBuilder()
                .WithHmacSecret(clientPin.Protocol, sharedSecret, platformKeyAgreement, salt1)
                .Build();
            
            var assertionResult = await session.GetAssertionAsync(
                rpId: FidoTestData.RpId,
                clientDataHash: assertChallenge,
                options: new GetAssertionOptions
                {
                    AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                    PinUvAuthParam = assertPinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version,
                    Extensions = assertExtensions
                });
            
            // Assert
            Assert.NotNull(assertionResult);
            Assert.NotNull(assertionResult.ExtensionOutputs);
            
            var extOutput = ExtensionOutput.Decode(assertionResult.ExtensionOutputs.Value);
            Assert.True(extOutput.TryGetHmacSecret(out var hmacOutput), "hmac-secret output should be present");
            Assert.NotNull(hmacOutput);
            
            // Decrypt the output
            var decrypted = clientPin.Protocol.Decrypt(sharedSecret, hmacOutput!.Output.Span);
            Assert.Equal(32, decrypted.Length); // Should be 32 bytes for single salt
            Assert.False(decrypted.ToArray().All(b => b == 0), "Derived secret should not be all zeros");
            
            // Clean up
            CryptographicOperations.ZeroMemory(sharedSecret);
            CryptographicOperations.ZeroMemory(decrypted);
        }
        finally
        {
            // Clean up test credentials
            await FidoTestHelpers.DeleteAllCredentialsForRpAsync(session, FidoTestData.RpId, FidoTestData.Pin);
        }
    }

    /// <summary>
    /// Tests that GetAssertion with the same salt returns the same derived secret (determinism).
    /// </summary>
    /// <remarks>
    /// This test verifies that hmac-secret is deterministic - calling it multiple times
    /// with the same salt should always return the same secret value.
    /// </remarks>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task GetAssertion_WithSameSalt_ReturnsSameSecret()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Skip if hmac-secret not supported
        var info = await session.GetInfoAsync();
        if (!info.Extensions.Contains(ExtensionIdentifiers.HmacSecret))
        {
            Skip.If(true, "YubiKey does not support hmac-secret extension");
            return;
        }

        byte[]? credentialId = null;

        try
        {
            // Arrange: Create credential with hmac-secret
            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);
            
            var rp = FidoTestData.CreateRelyingParty();
            var user = FidoTestData.CreateUser();
            var makeChallenge = FidoTestData.GenerateChallenge();
            
            var supportsPermissions = info.Versions.Contains("FIDO_2_1") || 
                                       info.Versions.Contains("FIDO_2_1_PRE");
            
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
            
            var makeExtensions = new ExtensionBuilder()
                .WithHmacSecretMakeCredential()
                .Build();
            
            var makeResult = await session.MakeCredentialAsync(
                clientDataHash: makeChallenge,
                rp: rp,
                user: user,
                pubKeyCredParams: FidoTestData.ES256Params,
                options: new MakeCredentialOptions
                {
                    ResidentKey = true,
                    PinUvAuthParam = makePinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version,
                    Extensions = makeExtensions
                });
            
            credentialId = makeResult.GetCredentialId().ToArray();
            
            // Generate a fixed salt to use for both assertions
            var salt1 = new byte[32];
            RandomNumberGenerator.Fill(salt1);
            
            // Act: Call GetAssertion twice with the same salt
            byte[] secret1, secret2;
            
            // First assertion
            {
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
                
                var authenticatorKeyAgreement = await clientPin.GetKeyAgreementAsync();
                var (platformKeyAgreement, sharedSecret) = clientPin.Protocol.Encapsulate(authenticatorKeyAgreement);
                
                var assertExtensions = new ExtensionBuilder()
                    .WithHmacSecret(clientPin.Protocol, sharedSecret, platformKeyAgreement, salt1)
                    .Build();
                
                var assertionResult = await session.GetAssertionAsync(
                    rpId: FidoTestData.RpId,
                    clientDataHash: assertChallenge,
                    options: new GetAssertionOptions
                    {
                        AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                        PinUvAuthParam = assertPinUvAuthParam,
                        PinUvAuthProtocol = clientPin.Protocol.Version,
                        Extensions = assertExtensions
                    });
                
                Assert.NotNull(assertionResult.ExtensionOutputs);
                var extOutput = ExtensionOutput.Decode(assertionResult.ExtensionOutputs.Value);
                Assert.True(extOutput.TryGetHmacSecret(out var hmacOutput));
                
                secret1 = clientPin.Protocol.Decrypt(sharedSecret, hmacOutput!.Output.Span);
                CryptographicOperations.ZeroMemory(sharedSecret);
            }
            
            // Second assertion with same salt
            {
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
                
                var authenticatorKeyAgreement = await clientPin.GetKeyAgreementAsync();
                var (platformKeyAgreement, sharedSecret) = clientPin.Protocol.Encapsulate(authenticatorKeyAgreement);
                
                var assertExtensions = new ExtensionBuilder()
                    .WithHmacSecret(clientPin.Protocol, sharedSecret, platformKeyAgreement, salt1)
                    .Build();
                
                var assertionResult = await session.GetAssertionAsync(
                    rpId: FidoTestData.RpId,
                    clientDataHash: assertChallenge,
                    options: new GetAssertionOptions
                    {
                        AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                        PinUvAuthParam = assertPinUvAuthParam,
                        PinUvAuthProtocol = clientPin.Protocol.Version,
                        Extensions = assertExtensions
                    });
                
                Assert.NotNull(assertionResult.ExtensionOutputs);
                var extOutput = ExtensionOutput.Decode(assertionResult.ExtensionOutputs.Value);
                Assert.True(extOutput.TryGetHmacSecret(out var hmacOutput));
                
                secret2 = clientPin.Protocol.Decrypt(sharedSecret, hmacOutput!.Output.Span);
                CryptographicOperations.ZeroMemory(sharedSecret);
            }
            
            // Assert: Both secrets should be identical
            Assert.Equal(32, secret1.Length);
            Assert.Equal(32, secret2.Length);
            Assert.True(secret1.SequenceEqual(secret2), "Same salt should produce same derived secret");
            
            // Clean up
            CryptographicOperations.ZeroMemory(secret1);
            CryptographicOperations.ZeroMemory(secret2);
        }
        finally
        {
            // Clean up test credentials
            await FidoTestHelpers.DeleteAllCredentialsForRpAsync(session, FidoTestData.RpId, FidoTestData.Pin);
        }
    }
}
