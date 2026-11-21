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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Ctap2.IntegrationTests;

/// <summary>
/// Registration and ordinary-assertion fixtures for the hmac-secret substrate of WebAuthn PRF.
/// </summary>
/// <remarks>
/// There is no CTAP "prf" extension key. These tests do not hash PRF inputs, exchange encrypted salts,
/// decode a PRF result or compare deterministic outputs, despite the retained test names. The first
/// checks extension presence only conditionally; the second makes one ordinary assertion.
/// Setup normalizes the known PIN and cleanup deletes all credentials for this RP. Tokens are not
/// comprehensively cleared; these are human-approved dedicated-device fixtures, not production examples.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Extension", "prf")]
public class Ctap2PrfTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task Prf_MakeCredential_IndicatesSupport(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            // Retained fixture gate, not a universal PRF/hmac-secret protocol floor.
            // This request emits hmac-secret:true, not hmac-secret-mc.
            if (!info.Versions.Contains("FIDO_2_1") && !info.Versions.Contains("FIDO_2_1_PRE"))
            {
                Skip.If(true, "PRF extension requires FIDO 2.1 — this authenticator only supports CTAP 2.0");
                return;
            }

            // PRF requires hmac-secret support on the authenticator
            if (!info.Extensions.Contains(ExtensionIdentifiers.HmacSecret))
            {
                Skip.If(true, "YubiKey does not support hmac-secret (required for PRF)");
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

                // Request the underlying hmac-secret credential support, without PRF input processing.
                var extensions = new ExtensionBuilder()
                    .WithHmacSecret()
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
                Assert.NotNull(makeResult);

                // Conditional presence check only; this does not assert the hmac-secret boolean value.
                if (makeResult.ExtensionOutputs.HasValue)
                {
                    var extOutput = ExtensionOutput.DecodeWithRawData(makeResult.ExtensionOutputs.Value);
                    // The standard registration output is boolean; this assertion only checks any output.
                    Assert.True(extOutput.HasExtensions, "Extension outputs should be present");
                }
            }
            finally
            {
                await Ctap2TestHelpers.DeleteAllCredentialsForRpAsync(session, Ctap2TestData.RpId, Ctap2TestData.PinUtf8);
            }
        });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task Prf_DeterministicOutputs_SameSaltProducesSameResult(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            // Retained fixture version gate; advertisement, not this gate, identifies hmac-secret support.
            if (!info.Versions.Contains("FIDO_2_1") && !info.Versions.Contains("FIDO_2_1_PRE"))
            {
                Skip.If(true, "PRF extension requires FIDO 2.1 — this authenticator only supports CTAP 2.0");
                return;
            }

            if (!info.Extensions.Contains(ExtensionIdentifiers.HmacSecret))
            {
                Skip.If(true, "YubiKey does not support hmac-secret (required for PRF)");
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

                // Create credential with hmac-secret support
                var makeExtensions = new ExtensionBuilder()
                    .WithHmacSecret()
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
                        Extensions = makeExtensions
                    });

                credentialId = makeResult.GetCredentialId().ToArray();

                // One ordinary assertion follows registration; no salts or PRF results are supplied.
                // Deterministic evaluation needs encrypted salts and comparison of actual outputs;
                // those assertions remain separate unfinished test work.
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
            }
            finally
            {
                await Ctap2TestHelpers.DeleteAllCredentialsForRpAsync(session, Ctap2TestData.RpId, Ctap2TestData.PinUtf8);
            }
        });
}