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
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for FIDO2 transport-related behavior including
/// session creation over different transports and credential metadata.
/// </summary>
[Trait("Category", "Integration")]
public class FidoTransportTests
{
    /// <summary>
    /// Tests that a FIDO2 session created over SmartCard (CCID) transport
    /// can successfully create and verify a credential end-to-end.
    /// This validates the full SmartCardBackend path for credential operations.
    /// </summary>
    /// <remarks>
    /// FIDO2 over SmartCard is supported when the connected authenticator exposes the FIDO2 AID.
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_OverSmartCard_CreatesCredentialSuccessfully(YubiKeyTestState state)
    {
        if (state.ConnectionType is not ConnectionType.SmartCard)
        {
            Skip.If(true,
                "This test requires a SmartCard connection, but the device is connected via " +
                $"{state.ConnectionType}.");
            return;
        }

        try
        {
            await state.WithFidoSessionAsync(async session =>
            {
                try
                {
                    using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.PinUtf8);

                    var rp = FidoTestData.CreateRelyingParty();
                    var user = FidoTestData.CreateUser();
                    var challenge = FidoTestData.GenerateChallenge();

                    var info = await session.GetInfoAsync();
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

                    Assert.NotNull(result);
                    Assert.NotNull(result.AuthenticatorData);
                    Assert.True(result.GetCredentialId().Length > 0,
                        "Credential ID should not be empty for SmartCard credential");
                }
                finally
                {
                    await FidoTestHelpers.DeleteAllCredentialsForRpAsync(
                        session, FidoTestData.RpId, FidoTestData.PinUtf8);
                }
            }, preferredConnection: ConnectionType.SmartCard);
        }
        catch (NotSupportedException)
        {
            Skip.If(true,
                "FIDO2 SmartCard session failed because the connected authenticator did not expose the FIDO2 AID or does not support USB SmartCard FIDO2 on this firmware.");
        }
    }

    /// <summary>
    /// Tests that a non-discoverable (non-resident) credential can be created
    /// and used for assertion via the allow list pattern. This validates the
    /// server-side credential flow where the credential ID is stored externally.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task NonDiscoverableCredential_WithAllowList_AssertionSucceeds(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            ClientPin clientPin;
            try
            {
                clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.PinUtf8);
            }
            catch (CtapException ex) when (ex.Status is CtapStatus.PinBlocked or CtapStatus.PinAuthBlocked)
            {
                Skip.If(true,
                    "PIN is blocked — FIDO2 reset required (re-insert YubiKey and reset within 10s of power-up)");
                return;
            }

            using var _ = clientPin;

            var rp = FidoTestData.CreateRelyingParty();
            var user = FidoTestData.CreateUser();
            var makeChallenge = FidoTestData.GenerateChallenge();

            var info = await session.GetInfoAsync();
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

            // Create a non-discoverable credential
            var makeResult = await session.MakeCredentialAsync(
                clientDataHash: makeChallenge,
                rp: rp,
                user: user,
                pubKeyCredParams: FidoTestData.ES256Params,
                options: new MakeCredentialOptions
                {
                    ResidentKey = false,
                    PinUvAuthParam = makePinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version
                });

            var credentialId = makeResult.GetCredentialId().ToArray();
            Assert.NotEmpty(credentialId);

            // Use the credential ID in an allow list for assertion
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
            Assert.True(assertionResult.Signature.Length > 0,
                    "Assertion signature should not be empty");
        });
}