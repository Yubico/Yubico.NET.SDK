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
using Yubico.YubiKit.Core.Cryptography.Cose;
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
/// Integration tests for MakeCredential operations.
/// </summary>
/// <remarks>
/// These tests exercise credential creation workflows on real YubiKeys.
/// Tests requiring user presence (touch) are marked with the appropriate trait.
/// </remarks>
[Trait("Category", "Integration")]
public class FidoMakeCredentialTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that MakeCredential creates a non-resident credential successfully.
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task MakeCredential_NonResidentKey_ReturnsValidAttestation()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Arrange
        using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);
        
        var rp = FidoTestData.CreateRelyingParty();
        var user = FidoTestData.CreateUser();
        var challenge = FidoTestData.GenerateChallenge();
        
        // Get PIN token for credential creation
        var info = await session.GetInfoAsync();
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
        
        var options = new MakeCredentialOptions
        {
            ResidentKey = false,
            PinUvAuthParam = pinUvAuthParam,
            PinUvAuthProtocol = clientPin.Protocol.Version
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
        Assert.NotNull(result.AuthenticatorData);
        Assert.NotNull(result.Format);
        Assert.True(result.GetCredentialId().Length > 0, "Credential ID should not be empty");
        Assert.Equal(16, result.GetAaguid().ToByteArray().Length);
    }

    /// <summary>
    /// Tests that MakeCredential creates a resident (discoverable) credential.
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task MakeCredential_ResidentKey_ReturnsCredentialId()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        byte[]? credentialId = null;

        try
        {
            // Arrange
            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);
            
            var rp = FidoTestData.CreateRelyingParty();
            var user = FidoTestData.CreateUser();
            var challenge = FidoTestData.GenerateChallenge();
            
            var info = await session.GetInfoAsync();
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
            
            var options = new MakeCredentialOptions
            {
                ResidentKey = true,
                PinUvAuthParam = pinUvAuthParam,
                PinUvAuthProtocol = clientPin.Protocol.Version
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
            Assert.NotNull(result.AuthenticatorData.AttestedCredentialData);
            credentialId = result.GetCredentialId().ToArray();
            Assert.NotEmpty(credentialId);
        }
        finally
        {
            // Cleanup: Delete the credential
            if (credentialId != null)
            {
                await CleanupCredentialAsync(session, credentialId);
            }
        }
    }

    /// <summary>
    /// Tests that MakeCredential with exclude list throws CredentialExcluded when credential exists.
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task MakeCredential_WithExcludeList_ThrowsCredentialExcluded()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        byte[]? credentialId = null;

        try
        {
            // Arrange: Create first credential
            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);
            
            var rp = FidoTestData.CreateRelyingParty();
            var user = FidoTestData.CreateUser();
            
            var info = await session.GetInfoAsync();
            var supportsPermissions = info.Versions.Contains("FIDO_2_1") || 
                                       info.Versions.Contains("FIDO_2_1_PRE");
            
            // Create first credential
            var challenge1 = FidoTestData.GenerateChallenge();
            byte[] pinToken1;
            if (supportsPermissions)
            {
                pinToken1 = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    FidoTestData.Pin,
                    PinUvAuthTokenPermissions.MakeCredential,
                    FidoTestData.RpId);
            }
            else
            {
                pinToken1 = await clientPin.GetPinTokenAsync(FidoTestData.Pin);
            }
            
            var pinUvAuthParam1 = FidoTestHelpers.ComputeMakeCredentialAuthParam(
                clientPin.Protocol, pinToken1, challenge1);
            
            var firstResult = await session.MakeCredentialAsync(
                clientDataHash: challenge1,
                rp: rp,
                user: user,
                pubKeyCredParams: FidoTestData.ES256Params,
                options: new MakeCredentialOptions
                {
                    ResidentKey = true,
                    PinUvAuthParam = pinUvAuthParam1,
                    PinUvAuthProtocol = clientPin.Protocol.Version
                });

            credentialId = firstResult.GetCredentialId().ToArray();

            // Act & Assert: Try to create with same credential in exclude list
            var excludeList = new[] { new PublicKeyCredentialDescriptor(credentialId) };
            
            var challenge2 = FidoTestData.GenerateChallenge();
            byte[] pinToken2;
            if (supportsPermissions)
            {
                pinToken2 = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    FidoTestData.Pin,
                    PinUvAuthTokenPermissions.MakeCredential,
                    FidoTestData.RpId);
            }
            else
            {
                pinToken2 = await clientPin.GetPinTokenAsync(FidoTestData.Pin);
            }
            
            var pinUvAuthParam2 = FidoTestHelpers.ComputeMakeCredentialAuthParam(
                clientPin.Protocol, pinToken2, challenge2);

            var ex = await Assert.ThrowsAsync<CtapException>(async () =>
            {
                await session.MakeCredentialAsync(
                    clientDataHash: challenge2,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: FidoTestData.ES256Params,
                    options: new MakeCredentialOptions
                    {
                        ResidentKey = true,
                        ExcludeList = excludeList,
                        PinUvAuthParam = pinUvAuthParam2,
                        PinUvAuthProtocol = clientPin.Protocol.Version
                    });
            });

            Assert.Equal(CtapStatus.CredentialExcluded, ex.Status);
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
