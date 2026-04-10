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
using Yubico.YubiKit.Core.Cryptography.Cose;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

using CredentialManagementClass = Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for algorithm support in FIDO2 credentials.
/// </summary>
[Trait("Category", "Integration")]
public class FidoAlgorithmSupportTests
{
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("RequiresUserPresence", "true")]
    public async Task MakeCredential_ES256_ReturnsES256Credential(YubiKeyTestState state) =>
        await TestAlgorithmAsync(state, CoseAlgorithmIdentifier.ES256);

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("RequiresUserPresence", "true")]
    public async Task MakeCredential_EdDSA_ReturnsEdDSACredential(YubiKeyTestState state) =>
        await TestAlgorithmAsync(state, CoseAlgorithmIdentifier.EdDSA, skipIfUnsupported: true);

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("RequiresUserPresence", "true")]
    public async Task MakeCredential_ES384_ReturnsES384Credential(YubiKeyTestState state) =>
        await TestAlgorithmAsync(state, CoseAlgorithmIdentifier.ES384, skipIfUnsupported: true);

    private static async Task TestAlgorithmAsync(
        YubiKeyTestState state,
        CoseAlgorithmIdentifier algorithm,
        bool skipIfUnsupported = false) =>
        await state.WithFidoSessionAsync(async session =>
        {
            byte[]? credentialId = null;

            try
            {
                using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.PinUtf8);

                var info = await session.GetInfoAsync();
                var supported = info.Algorithms.Any(a => a.Algorithm == algorithm);

                if (!supported && skipIfUnsupported)
                {
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
                        FidoTestData.PinUtf8,
                        PinUvAuthTokenPermissions.MakeCredential,
                        FidoTestData.RpId);
                }
                else
                {
                    pinToken = await clientPin.GetPinTokenAsync(FidoTestData.PinUtf8);
                }

                var pinUvAuthParam = FidoTestHelpers.ComputeMakeCredentialAuthParam(
                    clientPin.Protocol, pinToken, challenge);

                var pubKeyCredParams = new[] { new PublicKeyCredentialParameters(algorithm) };

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

                Assert.NotNull(result);
                Assert.NotNull(result.AuthenticatorData.AttestedCredentialData);
                Assert.NotEmpty(credentialId);
                Assert.False(result.GetCredentialPublicKey().IsEmpty,
                    "Credential public key should not be empty");
            }
            finally
            {
                if (credentialId is not null)
                {
                    await CleanupCredentialAsync(session, credentialId);
                }
            }
        });

    private static async Task CleanupCredentialAsync(FidoSession session, byte[] credentialId)
    {
        try
        {
            var (pinToken, clientPin, protocol) = await FidoTestHelpers.GetCredManTokenAsync(
                session, FidoTestData.PinUtf8);

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
