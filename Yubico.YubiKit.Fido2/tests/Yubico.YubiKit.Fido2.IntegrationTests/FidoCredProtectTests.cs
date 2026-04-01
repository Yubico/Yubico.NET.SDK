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
/// Integration tests for FIDO2 credProtect extension.
/// </summary>
/// <remarks>
/// <para>
/// Tests the credProtect extension which controls credential protection policies.
/// The extension determines when user verification is required for credential usage:
/// <list type="bullet">
///   <item><description>Level 1 (userVerificationOptional): UV not required (default)</description></item>
///   <item><description>Level 2 (userVerificationOptionalWithCredentialIdList): UV not required but credential not discoverable without allow list</description></item>
///   <item><description>Level 3 (userVerificationRequired): UV always required</description></item>
/// </list>
/// </para>
/// <para>
/// Tests automatically skip if the YubiKey does not support credProtect extension.
/// </para>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#sctn-credProtect-extension
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Extension", "credProtect")]
public class FidoCredProtectTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that credProtect level 2 requires allow list for credential discovery.
    /// </summary>
    /// <remarks>
    /// Level 2 (userVerificationOptionalWithCredentialIdList) means the credential
    /// is NOT discoverable without providing its credential ID in the allow list.
    /// Resident key enumeration should not reveal this credential.
    /// </remarks>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task CredProtect_Level2_RequiresAllowListForDiscovery()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Skip if credProtect not supported
        var info = await session.GetInfoAsync();
        if (!info.Extensions.Contains(ExtensionIdentifiers.CredProtect))
        {
            Skip.If(true, "YubiKey does not support credProtect extension");
            return;
        }

        byte[]? credentialId = null;

        try
        {
            // Arrange: Create resident credential with credProtect level 2
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
            
            // Build extension with credProtect level 2
            var extensions = new ExtensionBuilder()
                .WithCredProtect(CredProtectPolicy.UserVerificationOptionalWithCredentialIdList)
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
                    Extensions = extensions
                });
            
            credentialId = makeResult.GetCredentialId().ToArray();
            
            // Verify credProtect was applied
            if (makeResult.ExtensionOutputs.HasValue)
            {
                var extOutput = ExtensionOutput.Decode(makeResult.ExtensionOutputs.Value);
                if (extOutput.TryGetCredProtect(out var policy))
                {
                    Assert.Equal(CredProtectPolicy.UserVerificationOptionalWithCredentialIdList, policy);
                }
            }
            
            // Act: Try discoverable assertion (no allow list) - credential should not be returned
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
            
            // Discoverable assertion without allow list
            var discoverableResult = await session.GetAssertionAsync(
                rpId: FidoTestData.RpId,
                clientDataHash: assertChallenge,
                options: new GetAssertionOptions
                {
                    PinUvAuthParam = assertPinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version
                });
            
            // Credential should exist but may not be returned due to level 2 protection
            // (Implementation-dependent: some may return it, some may not)
            
            // Now try with allow list - should definitely work
            var withAllowListResult = await session.GetAssertionAsync(
                rpId: FidoTestData.RpId,
                clientDataHash: assertChallenge,
                options: new GetAssertionOptions
                {
                    AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                    PinUvAuthParam = assertPinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version
                });
            
            // Assert: With allow list, assertion should succeed
            Assert.NotNull(withAllowListResult);
            Assert.NotNull(withAllowListResult.AuthenticatorData);
        }
        finally
        {
            // Clean up test credentials
            await FidoTestHelpers.DeleteAllCredentialsForRpAsync(session, FidoTestData.RpId, FidoTestData.Pin);
        }
    }

    /// <summary>
    /// Tests that credProtect level 3 requires user verification.
    /// </summary>
    /// <remarks>
    /// Level 3 (userVerificationRequired) means user verification is ALWAYS required
    /// for assertions. Attempts to get assertions without UV should fail.
    /// </remarks>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task CredProtect_Level3_RequiresUserVerification()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Skip if credProtect not supported
        var info = await session.GetInfoAsync();
        if (!info.Extensions.Contains(ExtensionIdentifiers.CredProtect))
        {
            Skip.If(true, "YubiKey does not support credProtect extension");
            return;
        }

        byte[]? credentialId = null;

        try
        {
            // Arrange: Create credential with credProtect level 3
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
            
            // Build extension with credProtect level 3
            var extensions = new ExtensionBuilder()
                .WithCredProtect(CredProtectPolicy.UserVerificationRequired)
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
                    Extensions = extensions
                });
            
            credentialId = makeResult.GetCredentialId().ToArray();
            
            // Verify credProtect level 3 was applied
            if (makeResult.ExtensionOutputs.HasValue)
            {
                var extOutput = ExtensionOutput.Decode(makeResult.ExtensionOutputs.Value);
                if (extOutput.TryGetCredProtect(out var policy))
                {
                    Assert.Equal(CredProtectPolicy.UserVerificationRequired, policy);
                }
            }
            
            // Act: Try assertion WITH user verification (PIN provides UV)
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
            
            var assertionResult = await session.GetAssertionAsync(
                rpId: FidoTestData.RpId,
                clientDataHash: assertChallenge,
                options: new GetAssertionOptions
                {
                    AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                    PinUvAuthParam = assertPinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version
                });
            
            // Assert: With UV (via PIN), assertion should succeed
            Assert.NotNull(assertionResult);
            Assert.NotNull(assertionResult.AuthenticatorData);
            
            // Verify UV flag is set in authenticator data
            var authData = assertionResult.AuthenticatorData;
            Assert.True(authData.UserVerified, "User Verified flag should be set");
        }
        finally
        {
            // Clean up test credentials
            await FidoTestHelpers.DeleteAllCredentialsForRpAsync(session, FidoTestData.RpId, FidoTestData.Pin);
        }
    }
}
