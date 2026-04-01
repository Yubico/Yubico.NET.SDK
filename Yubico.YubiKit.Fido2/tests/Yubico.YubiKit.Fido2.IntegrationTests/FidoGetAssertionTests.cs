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
/// Integration tests for GetAssertion (authentication) operations.
/// </summary>
[Trait("Category", "Integration")]
public class FidoGetAssertionTests
{
    /// <summary>
    /// Tests that GetAssertion returns a valid signature after creating a credential.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("RequiresUserPresence", "true")]
    public async Task GetAssertion_AfterMakeCredential_ReturnsValidSignature(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            byte[]? credentialId = null;

            try
            {
                using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

                var rp = FidoTestData.CreateRelyingParty();
                var user = FidoTestData.CreateUser();

                var info = await session.GetInfoAsync();
                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                // Create credential
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

                // Act: Get assertion with allow list
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

                // Assert
                Assert.NotNull(assertionResult);
                Assert.NotNull(assertionResult.AuthenticatorData);
                Assert.False(assertionResult.Signature.IsEmpty, "Signature should not be empty");
            }
            finally
            {
                if (credentialId is not null)
                {
                    await CleanupCredentialAsync(session, credentialId);
                }
            }
        });

    /// <summary>
    /// Tests that GetAssertion with a resident key returns the user handle.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("RequiresUserPresence", "true")]
    public async Task GetAssertion_ResidentKey_ReturnsUserHandle(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            byte[]? credentialId = null;
            byte[]? expectedUserId = null;

            try
            {
                using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

                var rp = FidoTestData.CreateRelyingParty();
                expectedUserId = FidoTestData.GenerateUserId();
                var user = FidoTestData.CreateUser(expectedUserId);

                var info = await session.GetInfoAsync();
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

                // Act: Get assertion without allow list (discoverable credential)
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
                        PinUvAuthParam = assertPinUvAuthParam,
                        PinUvAuthProtocol = clientPin.Protocol.Version
                    });

                Assert.NotNull(assertionResult);
                var userHandle = assertionResult.GetUserHandle();
                Assert.False(userHandle.IsEmpty, "User handle should be present for discoverable credential");
                Assert.True(userHandle.Span.SequenceEqual(expectedUserId), "User handle should match expected user ID");
            }
            finally
            {
                if (credentialId is not null)
                {
                    await CleanupCredentialAsync(session, credentialId);
                }
            }
        });

    /// <summary>
    /// Tests that GetAssertion throws NoCredentials when no matching credential exists.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("RequiresUserPresence", "true")]
    public async Task GetAssertion_NoCredentials_ThrowsNoCredentials(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var uniqueRpId = $"test-{Guid.NewGuid()}.example.com";

            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.Pin);

            var info = await session.GetInfoAsync();
            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            var challenge = FidoTestData.GenerateChallenge();
            byte[] pinToken;
            if (supportsPermissions)
            {
                pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    FidoTestData.Pin,
                    PinUvAuthTokenPermissions.GetAssertion,
                    uniqueRpId);
            }
            else
            {
                pinToken = await clientPin.GetPinTokenAsync(FidoTestData.Pin);
            }

            var pinUvAuthParam = FidoTestHelpers.ComputeGetAssertionAuthParam(
                clientPin.Protocol, pinToken, challenge);

            var ex = await Assert.ThrowsAsync<CtapException>(async () =>
            {
                await session.GetAssertionAsync(
                    rpId: uniqueRpId,
                    clientDataHash: challenge,
                    options: new GetAssertionOptions
                    {
                        PinUvAuthParam = pinUvAuthParam,
                        PinUvAuthProtocol = clientPin.Protocol.Version
                    });
            });

            Assert.Equal(CtapStatus.NoCredentials, ex.Status);
        });

    private static async Task CleanupCredentialAsync(FidoSession session, byte[] credentialId)
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
