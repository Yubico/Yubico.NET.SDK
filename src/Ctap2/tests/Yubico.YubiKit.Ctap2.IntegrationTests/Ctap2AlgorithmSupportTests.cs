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
using Yubico.YubiKit.Core.Cryptography.Cose;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

using CredentialManagementClass = Yubico.YubiKit.Ctap2.CredentialManagement.CredentialManagement;

namespace Yubico.YubiKit.Ctap2.IntegrationTests;

/// <summary>
/// Integration tests for algorithm support in CTAP2 credentials.
/// </summary>
[Trait("Category", "Integration")]
public class Ctap2AlgorithmSupportTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_ES256_ReturnsES256Credential(YubiKeyTestState state) =>
        await TestAlgorithmAsync(state, CoseAlgorithmIdentifier.ES256);

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_EdDSA_ReturnsEdDSACredential(YubiKeyTestState state) =>
        await TestAlgorithmAsync(state, CoseAlgorithmIdentifier.EdDSA, skipIfUnsupported: true);

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_ES384_ReturnsES384Credential(YubiKeyTestState state) =>
        await TestAlgorithmAsync(state, CoseAlgorithmIdentifier.ES384, skipIfUnsupported: true);

    private static async Task TestAlgorithmAsync(
        YubiKeyTestState state,
        CoseAlgorithmIdentifier algorithm,
        bool skipIfUnsupported = false) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            byte[]? credentialId = null;

            try
            {
                using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

                var info = await session.GetInfoAsync();
                var supported = info.Algorithms.Any(a => a.Algorithm == algorithm);

                if (!supported && skipIfUnsupported)
                {
                    return;
                }

                var rp = Ctap2TestData.CreateRelyingParty();
                var user = Ctap2TestData.CreateUser();
                var challenge = Ctap2TestData.GenerateChallenge();

                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                byte[] pinToken;
                if (supportsPermissions)
                {
                    pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        Ctap2TestData.PinUtf8,
                        PinUvAuthTokenPermissions.MakeCredential,
                        Ctap2TestData.RpId);
                }
                else
                {
                    pinToken = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
                }

                var pinUvAuthParam = Ctap2TestHelpers.ComputeMakeCredentialAuthParam(
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

    private static async Task CleanupCredentialAsync(Ctap2Session session, byte[] credentialId)
    {
        try
        {
            var (pinToken, clientPin, protocol) = await Ctap2TestHelpers.GetCredManTokenAsync(
                session, Ctap2TestData.PinUtf8);

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