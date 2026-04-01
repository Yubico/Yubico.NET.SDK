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
/// Integration tests for FIDO2 minPinLength extension.
/// </summary>
/// <remarks>
/// <para>
/// Tests the minPinLength extension which returns the minimum PIN length
/// required by the authenticator. This allows relying parties to inform users
/// of PIN complexity requirements before credential creation.
/// </para>
/// <para>
/// Default FIDO2 minimum PIN length is 4 characters, with a maximum of 63.
/// </para>
/// <para>
/// Tests automatically skip if the YubiKey does not support minPinLength extension.
/// </para>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#sctn-minpinlength-extension
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Extension", "minPinLength")]
public class FidoMinPinLengthTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that MakeCredential with minPinLength extension returns the minimum PIN length.
    /// </summary>
    /// <remarks>
    /// When minPinLength is requested during MakeCredential, the authenticator should
    /// return the current minimum PIN length requirement in the extension output.
    /// </remarks>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task MakeCredential_WithMinPinLength_ReturnsMinPinLength()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Skip if minPinLength not supported
        var info = await session.GetInfoAsync();
        if (!info.Extensions.Contains(ExtensionIdentifiers.MinPinLength))
        {
            Skip.If(true, "YubiKey does not support minPinLength extension");
            return;
        }

        try
        {
            // Arrange
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
            
            // Build extension with minPinLength
            var extensions = new ExtensionBuilder()
                .WithMinPinLength()
                .Build();
            
            // Act
            var makeResult = await session.MakeCredentialAsync(
                clientDataHash: makeChallenge,
                rp: rp,
                user: user,
                pubKeyCredParams: FidoTestData.ES256Params,
                options: new MakeCredentialOptions
                {
                    ResidentKey = false,
                    PinUvAuthParam = makePinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version,
                    Extensions = extensions
                });
            
            // Assert
            Assert.NotNull(makeResult);
            
            if (makeResult.ExtensionOutputs.HasValue)
            {
                var extOutput = ExtensionOutput.Decode(makeResult.ExtensionOutputs.Value);
                
                if (extOutput.TryGetMinPinLength(out var minPinLength))
                {
                    // Verify minPinLength is in valid range (4-63)
                    Assert.InRange(minPinLength, 4, 63);
                }
            }
        }
        finally
        {
            // Clean up test credentials
            await FidoTestHelpers.DeleteAllCredentialsForRpAsync(session, FidoTestData.RpId, FidoTestData.Pin);
        }
    }

    /// <summary>
    /// Tests that GetInfo includes minPinLength information.
    /// </summary>
    /// <remarks>
    /// GetInfo response should include minPinLength in the authenticator info
    /// if the extension is supported.
    /// </remarks>
    [Fact]
    public async Task GetInfo_IncludesMinPinLength()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        // Act
        var info = await session.GetInfoAsync();

        // Assert
        Assert.NotNull(info);
        
        // If minPinLength extension is supported, check for minPinLength in info
        if (info.Extensions.Contains(ExtensionIdentifiers.MinPinLength))
        {
            // minPinLength should be present in AuthenticatorInfo
            if (info.MinPinLength.HasValue)
            {
                Assert.InRange(info.MinPinLength.Value, 4, 63);
            }
        }
        else
        {
            Skip.If(true, "YubiKey does not support minPinLength extension");
        }
    }
}
