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
using Yubico.YubiKit.Ctap2.Config;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Ctap2.IntegrationTests;

/// <summary>
/// Integration tests for CTAP2 enterprise attestation.
/// Enterprise attestation allows an authenticator to return a uniquely identifying
/// attestation certificate when requested by an enterprise relying party.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "EnterpriseAttestation")]
public class Ctap2EnterpriseAttestationTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task EnterpriseAttestation_VendorFacilitated_ReturnsAttestationStatement(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            // Check if enterprise attestation is supported (ep option)
            if (!info.Options.TryGetValue("ep", out var epSupported) || !epSupported)
            {
                Skip.If(true, "YubiKey does not support enterprise attestation (ep option)");
                return;
            }

            byte[]? credentialId = null;

            try
            {
                using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                // Enable enterprise attestation via authenticatorConfig
                if (supportsPermissions)
                {
                    var configPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        Ctap2TestData.PinUtf8,
                        PinUvAuthTokenPermissions.AuthenticatorConfig);

                    var config = new AuthenticatorConfig(session, clientPin.Protocol, configPinToken);
                    await config.EnableEnterpriseAttestationAsync();
                }

                var rp = Ctap2TestData.CreateRelyingParty();
                var user = Ctap2TestData.CreateUser();
                var makeChallenge = Ctap2TestData.GenerateChallenge();

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

                // Request vendor-facilitated enterprise attestation (type 1)
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
                        EnterpriseAttestation = 1
                    });

                credentialId = makeResult.GetCredentialId().ToArray();

                Assert.NotNull(makeResult);
                Assert.NotNull(makeResult.AttestationStatement);

                // With enterprise attestation, the format should be "packed" with a certificate
                // (not "none" / self-attestation)
                if (makeResult.EnterpriseAttestation.HasValue)
                {
                    Assert.True(makeResult.EnterpriseAttestation.Value,
                        "Enterprise attestation flag should be true when ep is enabled");
                }
            }
            finally
            {
                await Ctap2TestHelpers.DeleteAllCredentialsForRpAsync(session, Ctap2TestData.RpId, Ctap2TestData.PinUtf8);
            }
        });
}