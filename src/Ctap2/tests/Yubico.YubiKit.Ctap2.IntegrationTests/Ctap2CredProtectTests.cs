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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Ctap2.IntegrationTests;

/// <summary>
/// Integration tests for CTAP2 credProtect extension.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Extension", "credProtect")]
public class Ctap2CredProtectTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task CredProtect_Level2_RequiresAllowListForDiscovery(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
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
                using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

                var rp = Ctap2TestData.CreateRelyingParty();
                var user = Ctap2TestData.CreateUser();
                var makeChallenge = Ctap2TestData.GenerateChallenge();

                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                byte[] makePinToken;
                if (supportsPermissions)
                {
                    makePinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        Ctap2TestData.PinUtf8,
                        PinUvAuthTokenPermissions.MakeCredential,
                        Ctap2TestData.RpId);
                }
                else
                {
                    makePinToken = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
                }

                var makePinUvAuthParam = Ctap2TestHelpers.ComputeMakeCredentialAuthParam(
                    clientPin.Protocol, makePinToken, makeChallenge);

                var extensions = new ExtensionBuilder()
                    .WithCredProtect(CredProtectPolicy.UserVerificationOptionalWithCredentialIdList)
                    .Build();

                var makeResult = await session.MakeCredentialAsync(
                    clientDataHash: makeChallenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: Ctap2TestData.ES256Params,
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

                var assertChallenge = Ctap2TestData.GenerateChallenge();
                byte[] assertPinToken;
                if (supportsPermissions)
                {
                    assertPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        Ctap2TestData.PinUtf8,
                        PinUvAuthTokenPermissions.GetAssertion,
                        Ctap2TestData.RpId);
                }
                else
                {
                    assertPinToken = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
                }

                var assertPinUvAuthParam = Ctap2TestHelpers.ComputeGetAssertionAuthParam(
                    clientPin.Protocol, assertPinToken, assertChallenge);

                var discoverableResult = await session.GetAssertionAsync(
                    rpId: Ctap2TestData.RpId,
                    clientDataHash: assertChallenge,
                    options: new GetAssertionOptions
                    {
                        PinUvAuthParam = assertPinUvAuthParam,
                        PinUvAuthProtocol = clientPin.Protocol.Version
                    });

                // Per CTAP 2.x: a regular pinUvAuthToken must be regenerated for each
                // authenticator invocation (PPUAT is the read-only exception, not used here).
                // Re-mint the token + auth param before the second GetAssertion call.
                byte[] allowListPinToken;
                if (supportsPermissions)
                {
                    allowListPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        Ctap2TestData.PinUtf8,
                        PinUvAuthTokenPermissions.GetAssertion,
                        Ctap2TestData.RpId);
                }
                else
                {
                    allowListPinToken = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
                }

                var allowListPinUvAuthParam = Ctap2TestHelpers.ComputeGetAssertionAuthParam(
                    clientPin.Protocol, allowListPinToken, assertChallenge);

                var withAllowListResult = await session.GetAssertionAsync(
                    rpId: Ctap2TestData.RpId,
                    clientDataHash: assertChallenge,
                    options: new GetAssertionOptions
                    {
                        AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                        PinUvAuthParam = allowListPinUvAuthParam,
                        PinUvAuthProtocol = clientPin.Protocol.Version
                    });

                Assert.NotNull(withAllowListResult);
                Assert.NotNull(withAllowListResult.AuthenticatorData);
            }
            finally
            {
                await Ctap2TestHelpers.DeleteAllCredentialsForRpAsync(session, Ctap2TestData.RpId, Ctap2TestData.PinUtf8);
            }
        });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task CredProtect_Level3_RequiresUserVerification(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
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
                using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

                var rp = Ctap2TestData.CreateRelyingParty();
                var user = Ctap2TestData.CreateUser();
                var makeChallenge = Ctap2TestData.GenerateChallenge();

                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                byte[] makePinToken;
                if (supportsPermissions)
                {
                    makePinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        Ctap2TestData.PinUtf8,
                        PinUvAuthTokenPermissions.MakeCredential,
                        Ctap2TestData.RpId);
                }
                else
                {
                    makePinToken = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
                }

                var makePinUvAuthParam = Ctap2TestHelpers.ComputeMakeCredentialAuthParam(
                    clientPin.Protocol, makePinToken, makeChallenge);

                var extensions = new ExtensionBuilder()
                    .WithCredProtect(CredProtectPolicy.UserVerificationRequired)
                    .Build();

                var makeResult = await session.MakeCredentialAsync(
                    clientDataHash: makeChallenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: Ctap2TestData.ES256Params,
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

                var assertChallenge = Ctap2TestData.GenerateChallenge();
                byte[] assertPinToken;
                if (supportsPermissions)
                {
                    assertPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        Ctap2TestData.PinUtf8,
                        PinUvAuthTokenPermissions.GetAssertion,
                        Ctap2TestData.RpId);
                }
                else
                {
                    assertPinToken = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
                }

                var assertPinUvAuthParam = Ctap2TestHelpers.ComputeGetAssertionAuthParam(
                    clientPin.Protocol, assertPinToken, assertChallenge);

                var assertionResult = await session.GetAssertionAsync(
                    rpId: Ctap2TestData.RpId,
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
                await Ctap2TestHelpers.DeleteAllCredentialsForRpAsync(session, Ctap2TestData.RpId, Ctap2TestData.PinUtf8);
            }
        });
}