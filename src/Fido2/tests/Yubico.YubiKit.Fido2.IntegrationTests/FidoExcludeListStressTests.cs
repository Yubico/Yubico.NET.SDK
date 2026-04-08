// Copyright 2026 Yubico AB
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
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

using CredentialManagementClass = Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement;
using CtapException = Yubico.YubiKit.Fido2.Ctap.CtapException;
using CtapStatus = Yubico.YubiKit.Fido2.Ctap.CtapStatus;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Stress tests for large FIDO2 exclude lists. These tests create many credentials
/// and verify that the exclude list mechanism works at scale.
/// </summary>
[Trait("Category", "Integration")]
[Trait(TestCategories.Category, TestCategories.Slow)]
public class FidoExcludeListStressTests
{
    /// <summary>
    /// The number of credentials to create for the stress test.
    /// 17 is chosen to exceed typical batching thresholds in CTAP2 implementations.
    /// </summary>
    private const int CredentialCount = 17;

    /// <summary>
    /// Creates 17 resident credentials for the same RP, builds an exclude list
    /// containing all of them, then attempts to create another credential for the
    /// same RP and user. The authenticator should reject the request with
    /// <see cref="CtapStatus.CredentialExcluded"/> because the same user already
    /// has a credential on that RP in the exclude list.
    /// </summary>
    /// <remarks>
    /// This test is marked as Slow because creating 17 credentials requires
    /// multiple user touches and takes significant time.
    /// </remarks>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("RequiresUserPresence", "true")]
    public async Task MakeCredential_WithLargeExcludeList_RejectsExcludedCredential(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var createdCredentialIds = new List<byte[]>();

            try
            {
                using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

                var rp = FidoTestData.CreateRelyingParty();
                var info = await session.GetInfoAsync();
                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                // Create CredentialCount resident credentials, each with a unique user
                for (var i = 0; i < CredentialCount; i++)
                {
                    var user = FidoTestData.CreateUser();
                    var challenge = FidoTestData.GenerateChallenge();

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

                    var result = await session.MakeCredentialAsync(
                        clientDataHash: challenge,
                        rp: rp,
                        user: user,
                        pubKeyCredParams: FidoTestData.ES256Params,
                        options: new MakeCredentialOptions
                        {
                            ResidentKey = true,
                            PinUvAuthParam = pinUvAuthParam,
                            PinUvAuthProtocol = clientPin.Protocol.Version
                        });

                    createdCredentialIds.Add(result.GetCredentialId().ToArray());
                }

                Assert.Equal(CredentialCount, createdCredentialIds.Count);

                // Build the exclude list from all created credentials
                var excludeList = createdCredentialIds
                    .Select(id => new PublicKeyCredentialDescriptor(id))
                    .ToList();

                // Attempt to create a new credential for the same RP using one of the
                // existing user IDs. The exclude list contains a credential for this user,
                // so the authenticator should reject it.
                var existingUserId = FidoTestData.CreateUser();
                var finalChallenge = FidoTestData.GenerateChallenge();

                byte[] finalPinToken;
                if (supportsPermissions)
                {
                    finalPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        FidoTestData.Pin,
                        PinUvAuthTokenPermissions.MakeCredential,
                        FidoTestData.RpId);
                }
                else
                {
                    finalPinToken = await clientPin.GetPinTokenAsync(FidoTestData.Pin);
                }

                var finalPinUvAuthParam = FidoTestHelpers.ComputeMakeCredentialAuthParam(
                    clientPin.Protocol, finalPinToken, finalChallenge);

                var ex = await Assert.ThrowsAsync<CtapException>(async () =>
                {
                    await session.MakeCredentialAsync(
                        clientDataHash: finalChallenge,
                        rp: rp,
                        user: existingUserId,
                        pubKeyCredParams: FidoTestData.ES256Params,
                        options: new MakeCredentialOptions
                        {
                            ResidentKey = true,
                            ExcludeList = excludeList,
                            PinUvAuthParam = finalPinUvAuthParam,
                            PinUvAuthProtocol = clientPin.Protocol.Version
                        });
                });

                Assert.Equal(CtapStatus.CredentialExcluded, ex.Status);
            }
            finally
            {
                // Clean up all created credentials
                await CleanupAllCredentialsAsync(session, createdCredentialIds);
            }
        });

    private static async Task CleanupAllCredentialsAsync(
        FidoSession session,
        List<byte[]> credentialIds)
    {
        try
        {
            var (pinToken, clientPin, protocol) = await FidoTestHelpers.GetCredManTokenAsync(
                session, FidoTestData.Pin);

            using (clientPin)
            {
                var credMan = new CredentialManagementClass(session, protocol, pinToken);

                foreach (var credentialId in credentialIds)
                {
                    try
                    {
                        var descriptor = new PublicKeyCredentialDescriptor(credentialId);
                        await credMan.DeleteCredentialAsync(descriptor);
                    }
                    catch
                    {
                        // Best-effort cleanup; continue with remaining credentials
                    }
                }
            }

            CryptographicOperations.ZeroMemory(pinToken);
        }
        catch
        {
            // Cleanup failures should not fail the test
        }
    }
}
