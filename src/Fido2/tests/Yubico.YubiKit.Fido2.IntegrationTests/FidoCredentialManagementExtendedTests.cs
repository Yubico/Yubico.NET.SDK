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

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Extended integration tests for FIDO2 credential management operations.
/// Tests user info updates and multiple users per RP enumeration.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "CredentialManagement")]
public class FidoCredentialManagementExtendedTests
{
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("RequiresUserPresence", "true")]
    public async Task UpdateUserInformation_ChangesDisplayName(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Options.TryGetValue("credMgmt", out var credMgmtSupported) || !credMgmtSupported)
            {
                Skip.If(true, "YubiKey does not support credential management (credMgmt option)");
                return;
            }

            // UpdateUserInformation requires CTAP 2.1
            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");
            if (!supportsPermissions)
            {
                Skip.If(true, "UpdateUserInformation requires CTAP 2.1");
                return;
            }

            byte[]? credentialId = null;

            try
            {
                using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

                var rp = FidoTestData.CreateRelyingParty();
                var userId = FidoTestData.GenerateUserId();
                var user = FidoTestData.CreateUser(userId);
                var makeChallenge = FidoTestData.GenerateChallenge();

                var makePinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    FidoTestData.Pin,
                    PinUvAuthTokenPermissions.MakeCredential,
                    FidoTestData.RpId);

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

                // Update the user display name
                var (pinToken, clientPinForCredMan, protocol) = await FidoTestHelpers.GetCredManTokenAsync(
                    session, FidoTestData.Pin);

                using (clientPinForCredMan)
                {
                    var credMan = new CredentialManagementClass(session, protocol, pinToken);

                    var updatedUser = new PublicKeyCredentialUserEntity(
                        userId,
                        "updated-user@example.com",
                        "Updated Display Name");

                    var descriptor = new PublicKeyCredentialDescriptor(credentialId);
                    await credMan.UpdateUserInformationAsync(descriptor, updatedUser);

                    // Verify the update by enumerating credentials
                    var rpIdHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(FidoTestData.RpId));
                    var credentials = await credMan.EnumerateCredentialsAsync(rpIdHash);

                    var updated = credentials.FirstOrDefault(c =>
                        c.CredentialId.Id.Span.SequenceEqual(credentialId));

                    Assert.NotNull(updated);
                    Assert.Equal("Updated Display Name", updated.User.DisplayName);
                }

                CryptographicOperations.ZeroMemory(pinToken);
            }
            finally
            {
                await FidoTestHelpers.DeleteAllCredentialsForRpAsync(session, FidoTestData.RpId, FidoTestData.Pin);
            }
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("RequiresUserPresence", "true")]
    public async Task EnumerateCredentials_MultipleUsersPerRp_ReturnsAll(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Options.TryGetValue("credMgmt", out var credMgmtSupported) || !credMgmtSupported)
            {
                Skip.If(true, "YubiKey does not support credential management (credMgmt option)");
                return;
            }

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            var credentialIds = new List<byte[]>();

            try
            {
                using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

                var rp = FidoTestData.CreateRelyingParty();

                // Create two credentials for the same RP with different users
                for (var i = 0; i < 2; i++)
                {
                    var userId = FidoTestData.GenerateUserId();
                    var user = new PublicKeyCredentialUserEntity(
                        userId,
                        $"user{i}@example.com",
                        $"Test User {i}");

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

                    credentialIds.Add(makeResult.GetCredentialId().ToArray());
                }

                // Enumerate credentials for the RP
                var (pinToken, clientPinForCredMan, protocol) = await FidoTestHelpers.GetCredManTokenAsync(
                    session, FidoTestData.Pin);

                using (clientPinForCredMan)
                {
                    var credMan = new CredentialManagementClass(session, protocol, pinToken);

                    var rpIdHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(FidoTestData.RpId));
                    var credentials = await credMan.EnumerateCredentialsAsync(rpIdHash);

                    Assert.True(credentials.Count >= 2,
                        $"Expected at least 2 credentials for the RP, found {credentials.Count}");

                    // Verify both our credential IDs are in the enumeration
                    foreach (var credId in credentialIds)
                    {
                        Assert.Contains(credentials, c =>
                            c.CredentialId.Id.Span.SequenceEqual(credId));
                    }
                }

                CryptographicOperations.ZeroMemory(pinToken);
            }
            finally
            {
                await FidoTestHelpers.DeleteAllCredentialsForRpAsync(session, FidoTestData.RpId, FidoTestData.Pin);
            }
        });
}
