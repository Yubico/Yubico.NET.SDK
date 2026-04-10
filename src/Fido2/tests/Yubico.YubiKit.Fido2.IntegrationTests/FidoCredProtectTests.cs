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
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for FIDO2 credProtect extension.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Extension", "credProtect")]
public class FidoCredProtectTests
{
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("RequiresUserPresence", "true")]
    public async Task CredProtect_Level2_RequiresAllowListForDiscovery(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Extensions.Contains(ExtensionIdentifiers.CredProtect))
            {
                Skip.If(true, "YubiKey does not support credProtect extension");
                return;
            }

            byte[]? credentialId = null;

            try
            {
                using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.PinUtf8);

                var rp = FidoTestData.CreateRelyingParty();
                var user = FidoTestData.CreateUser();
                var makeChallenge = FidoTestData.GenerateChallenge();

                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                byte[] makePinToken;
                if (supportsPermissions)
                {
                    makePinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        FidoTestData.PinUtf8,
                        PinUvAuthTokenPermissions.MakeCredential,
                        FidoTestData.RpId);
                }
                else
                {
                    makePinToken = await clientPin.GetPinTokenAsync(FidoTestData.PinUtf8);
                }

                var makePinUvAuthParam = FidoTestHelpers.ComputeMakeCredentialAuthParam(
                    clientPin.Protocol, makePinToken, makeChallenge);

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

                if (makeResult.ExtensionOutputs.HasValue)
                {
                    var extOutput = ExtensionOutput.Decode(makeResult.ExtensionOutputs.Value);
                    if (extOutput.TryGetCredProtect(out var policy))
                    {
                        Assert.Equal(CredProtectPolicy.UserVerificationOptionalWithCredentialIdList, policy);
                    }
                }

                var assertChallenge = FidoTestData.GenerateChallenge();
                byte[] assertPinToken;
                if (supportsPermissions)
                {
                    assertPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        FidoTestData.PinUtf8,
                        PinUvAuthTokenPermissions.GetAssertion,
                        FidoTestData.RpId);
                }
                else
                {
                    assertPinToken = await clientPin.GetPinTokenAsync(FidoTestData.PinUtf8);
                }

                var assertPinUvAuthParam = FidoTestHelpers.ComputeGetAssertionAuthParam(
                    clientPin.Protocol, assertPinToken, assertChallenge);

                var discoverableResult = await session.GetAssertionAsync(
                    rpId: FidoTestData.RpId,
                    clientDataHash: assertChallenge,
                    options: new GetAssertionOptions
                    {
                        PinUvAuthParam = assertPinUvAuthParam,
                        PinUvAuthProtocol = clientPin.Protocol.Version
                    });

                var withAllowListResult = await session.GetAssertionAsync(
                    rpId: FidoTestData.RpId,
                    clientDataHash: assertChallenge,
                    options: new GetAssertionOptions
                    {
                        AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                        PinUvAuthParam = assertPinUvAuthParam,
                        PinUvAuthProtocol = clientPin.Protocol.Version
                    });

                Assert.NotNull(withAllowListResult);
                Assert.NotNull(withAllowListResult.AuthenticatorData);
            }
            finally
            {
                await FidoTestHelpers.DeleteAllCredentialsForRpAsync(session, FidoTestData.RpId, FidoTestData.PinUtf8);
            }
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("RequiresUserPresence", "true")]
    public async Task CredProtect_Level3_RequiresUserVerification(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Extensions.Contains(ExtensionIdentifiers.CredProtect))
            {
                Skip.If(true, "YubiKey does not support credProtect extension");
                return;
            }

            byte[]? credentialId = null;

            try
            {
                using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.PinUtf8);

                var rp = FidoTestData.CreateRelyingParty();
                var user = FidoTestData.CreateUser();
                var makeChallenge = FidoTestData.GenerateChallenge();

                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                byte[] makePinToken;
                if (supportsPermissions)
                {
                    makePinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        FidoTestData.PinUtf8,
                        PinUvAuthTokenPermissions.MakeCredential,
                        FidoTestData.RpId);
                }
                else
                {
                    makePinToken = await clientPin.GetPinTokenAsync(FidoTestData.PinUtf8);
                }

                var makePinUvAuthParam = FidoTestHelpers.ComputeMakeCredentialAuthParam(
                    clientPin.Protocol, makePinToken, makeChallenge);

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

                if (makeResult.ExtensionOutputs.HasValue)
                {
                    var extOutput = ExtensionOutput.Decode(makeResult.ExtensionOutputs.Value);
                    if (extOutput.TryGetCredProtect(out var policy))
                    {
                        Assert.Equal(CredProtectPolicy.UserVerificationRequired, policy);
                    }
                }

                var assertChallenge = FidoTestData.GenerateChallenge();
                byte[] assertPinToken;
                if (supportsPermissions)
                {
                    assertPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        FidoTestData.PinUtf8,
                        PinUvAuthTokenPermissions.GetAssertion,
                        FidoTestData.RpId);
                }
                else
                {
                    assertPinToken = await clientPin.GetPinTokenAsync(FidoTestData.PinUtf8);
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

                Assert.NotNull(assertionResult);
                Assert.NotNull(assertionResult.AuthenticatorData);

                var authData = assertionResult.AuthenticatorData;
                Assert.True(authData.UserVerified, "User Verified flag should be set");
            }
            finally
            {
                await FidoTestHelpers.DeleteAllCredentialsForRpAsync(session, FidoTestData.RpId, FidoTestData.PinUtf8);
            }
        });
}
