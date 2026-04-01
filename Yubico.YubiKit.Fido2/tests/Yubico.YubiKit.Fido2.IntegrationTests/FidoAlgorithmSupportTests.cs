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

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for algorithm support in FIDO2 credentials.
/// </summary>
/// <remarks>
/// These tests verify credential creation with different cryptographic algorithms.
/// Algorithm support varies by YubiKey model and firmware version.
/// </remarks>
[Trait("Category", "Integration")]
public class FidoAlgorithmSupportTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that MakeCredential with ES256 (ECDSA P-256) succeeds.
    /// </summary>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task MakeCredential_ES256_ReturnsES256Credential()
    {
        await TestAlgorithmAsync(CoseAlgorithmIdentifier.ES256);
    }

    /// <summary>
    /// Tests that MakeCredential with EdDSA (Ed25519) succeeds on supported devices.
    /// </summary>
    /// <remarks>
    /// EdDSA support requires firmware 5.7.0 or later.
    /// </remarks>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task MakeCredential_EdDSA_ReturnsEdDSACredential()
    {
        await TestAlgorithmAsync(CoseAlgorithmIdentifier.EdDSA, skipIfUnsupported: true);
    }

    /// <summary>
    /// Tests that MakeCredential with ES384 (ECDSA P-384) succeeds on supported devices.
    /// </summary>
    /// <remarks>
    /// ES384 may not be supported on all devices.
    /// </remarks>
    [Fact]
    [Trait("RequiresUserPresence", "true")]
    public async Task MakeCredential_ES384_ReturnsES384Credential()
    {
        await TestAlgorithmAsync(CoseAlgorithmIdentifier.ES384, skipIfUnsupported: true);
    }

    private async Task TestAlgorithmAsync(
        CoseAlgorithmIdentifier algorithm,
        bool skipIfUnsupported = false)
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
        var device = devices.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(device, "No FIDO2 YubiKey found.");

        await using var connection = await device.ConnectAsync<IFidoHidConnection>();
        await using var session = await FidoSession.CreateAsync(connection);

        byte[]? credentialId = null;

        try
        {
            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

            // Check if algorithm is supported
            var info = await session.GetInfoAsync();
            var supported = info.Algorithms.Any(a => a.Algorithm == algorithm);

            if (!supported && skipIfUnsupported)
            {
                // Skip test if algorithm not supported
                return;
            }

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

            var pubKeyCredParams = new[] { new PublicKeyCredentialParameters(algorithm) };

            // Act
            var result = await session.MakeCredentialAsync(
                clientDataHash: challenge,
                rp: rp,
                user: user,
                pubKeyCredParams: pubKeyCredParams,
                options: new MakeCredentialOptions
                {
                    ResidentKey = true,
                    PinUvAuthParam = pinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version
                });

            credentialId = result.GetCredentialId().ToArray();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.AuthenticatorData.AttestedCredentialData);
            Assert.NotEmpty(credentialId);
            Assert.False(result.GetCredentialPublicKey().IsEmpty, 
                "Credential public key should not be empty");
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
