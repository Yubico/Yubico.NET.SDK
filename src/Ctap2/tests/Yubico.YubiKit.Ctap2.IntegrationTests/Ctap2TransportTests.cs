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
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Ctap2.IntegrationTests;

/// <summary>
/// Integration tests for CTAP2 transport-related behavior including
/// session creation over different transports and credential metadata.
/// </summary>
[Trait("Category", "Integration")]
public class Ctap2TransportTests
{
    /// <summary>
    /// Tests that a CTAP2 session created over SmartCard (CCID) transport
    /// can successfully create and verify a credential end-to-end.
    /// This validates the full SmartCardBackend path for credential operations.
    /// </summary>
    /// <remarks>
    /// CTAP2 over SmartCard is supported when the connected authenticator exposes the FIDO AID.
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
            await state.WithCtap2SessionAsync(async session =>
            {
                try
                {
                    using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

                    var rp = Ctap2TestData.CreateRelyingParty();
                    var user = Ctap2TestData.CreateUser();
                    var challenge = Ctap2TestData.GenerateChallenge();

                    var info = await session.GetInfoAsync();
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

                    var result = await session.MakeCredentialAsync(
                        clientDataHash: challenge,
                        rp: rp,
                        user: user,
                        pubKeyCredParams: Ctap2TestData.ES256Params,
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
                    await Ctap2TestHelpers.DeleteAllCredentialsForRpAsync(
                        session, Ctap2TestData.RpId, Ctap2TestData.PinUtf8);
                }
            }, preferredConnection: ConnectionType.SmartCard);
        }
        catch (NotSupportedException)
        {
            Skip.If(true,
                "CTAP2 SmartCard session failed because the connected authenticator did not expose the FIDO AID or does not support USB SmartCard CTAP2 on this firmware.");
        }
    }

    /// <summary>
    /// Tests that a non-discoverable (non-resident) credential can be created
    /// and used for assertion via the allow list pattern. This validates the
    /// server-side credential flow where the credential ID is stored externally.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task NonDiscoverableCredential_WithAllowList_AssertionSucceeds(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            ClientPin clientPin;
            try
            {
                clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);
            }
            catch (CtapException ex) when (ex.Status is CtapStatus.PinBlocked or CtapStatus.PinAuthBlocked)
            {
                Skip.If(true,
                    "PIN is blocked — CTAP2 reset required (re-insert YubiKey and reset within 10s of power-up)");
                return;
            }

            using var _ = clientPin;

            var rp = Ctap2TestData.CreateRelyingParty();
            var user = Ctap2TestData.CreateUser();
            var makeChallenge = Ctap2TestData.GenerateChallenge();

            var info = await session.GetInfoAsync();
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

            // Create a non-discoverable credential
            var makeResult = await session.MakeCredentialAsync(
                clientDataHash: makeChallenge,
                rp: rp,
                user: user,
                pubKeyCredParams: Ctap2TestData.ES256Params,
                options: new MakeCredentialOptions
                {
                    ResidentKey = false,
                    PinUvAuthParam = makePinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version
                });

            var credentialId = makeResult.GetCredentialId().ToArray();
            Assert.NotEmpty(credentialId);

            // Use the credential ID in an allow list for assertion
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
            Assert.True(assertionResult.Signature.Length > 0,
                    "Assertion signature should not be empty");
        });
}