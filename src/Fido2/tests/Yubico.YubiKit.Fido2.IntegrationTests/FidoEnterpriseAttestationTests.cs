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

using Xunit;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Config;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for FIDO2 enterprise attestation.
/// Enterprise attestation allows an authenticator to return a uniquely identifying
/// attestation certificate when requested by an enterprise relying party.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "EnterpriseAttestation")]
public class FidoEnterpriseAttestationTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task EnterpriseAttestation_VendorFacilitated_ReturnsAttestationStatement(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
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
                using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.PinUtf8);

                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                // Enable enterprise attestation via authenticatorConfig
                if (supportsPermissions)
                {
                    var configPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        FidoTestData.PinUtf8,
                        PinUvAuthTokenPermissions.AuthenticatorConfig);

                    var config = new AuthenticatorConfig(session, clientPin.Protocol, configPinToken);
                    await config.EnableEnterpriseAttestationAsync();
                }

                var rp = FidoTestData.CreateRelyingParty();
                var user = FidoTestData.CreateUser();
                var makeChallenge = FidoTestData.GenerateChallenge();

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

                // Request vendor-facilitated enterprise attestation (type 1)
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
                await FidoTestHelpers.DeleteAllCredentialsForRpAsync(session, FidoTestData.RpId, FidoTestData.PinUtf8);
            }
        });
}
