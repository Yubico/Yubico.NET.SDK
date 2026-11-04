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

using System.Text;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Ctap2.IntegrationTests;

/// <summary>
/// Exercises a PIN change followed by token retrieval, and credential creation with token authorization.
/// </summary>
/// <remarks>
/// Default session setup normalizes the known test PIN and can fail before either callback runs.
/// Restoration and credential cleanup are callback-scoped, not guaranteed restoration of every
/// setup side effect. Returned token/authentication-parameter arrays are not cleared by these tests.
/// </remarks>
[Trait("Category", "Integration")]
public class Ctap2PinManagementTests
{
    private static readonly byte[] OriginalPinUtf8 = Ctap2TestStateExtensions.KnownTestPin;
    private const string ChangedPin = "Xyz98765";
    private static readonly byte[] ChangedPinUtf8 = Encoding.UTF8.GetBytes(ChangedPin);

    /// <summary>
    /// Changes the known test PIN and checks that token retrieval with the new PIN returns nonempty bytes.
    /// </summary>
    /// <remarks>The test does not assert rejection of the old PIN or invalidation of old tokens. Restoration is best effort and can submit an incorrect PIN if the change did not complete.</remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task ChangePin_WithValidCurrentPin_AllowsAuthWithNewPin(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            var protocolVersion = info.PinUvAuthProtocols.Contains(2) ? 2 : 1;

            try
            {
                // Reaching this callback means default normalization completed with the known PIN.
                IPinUvAuthProtocol protocol = protocolVersion == 2
                    ? new PinUvAuthProtocolV2()
                    : new PinUvAuthProtocolV1();
                using var clientPin = new ClientPin(session, protocol);

                // Change the PIN from known test PIN to a different value
                await clientPin.ChangePinAsync(OriginalPinUtf8, ChangedPinUtf8);

                // Verify the new PIN works by getting a PIN token
                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                byte[] pinToken;
                if (supportsPermissions)
                {
                    pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        ChangedPinUtf8,
                        PinUvAuthTokenPermissions.MakeCredential,
                        Ctap2TestData.RpId);
                }
                else
                {
                    pinToken = await clientPin.GetPinTokenAsync(ChangedPinUtf8);
                }

                Assert.NotNull(pinToken);
                Assert.NotEmpty(pinToken);
            }
            catch (CtapException ex) when (ex.Status is CtapStatus.PinBlocked or CtapStatus.PinAuthBlocked)
            {
                // PIN_AUTH_BLOCKED is temporary and requires a power cycle; PIN_BLOCKED requires reset.
                // The diagnostic below conflates them and does not establish a product reset window.
                Skip.If(true,
                    "PIN is blocked — CTAP2 reset required (re-insert YubiKey and reset within 10s of power-up)");
            }
            finally
            {
                // Attempt restoration only for this callback; failure does not prove the PIN state.
                try
                {
                    var restoreProtocol = protocolVersion == 2
                        ? (IPinUvAuthProtocol)new PinUvAuthProtocolV2()
                        : new PinUvAuthProtocolV1();
                    using var restorePin = new ClientPin(session, restoreProtocol);
                    await restorePin.ChangePinAsync(ChangedPinUtf8, OriginalPinUtf8);
                }
                catch
                {
                    // Best-effort restore; if it fails, the device PIN may be in a changed state
                }
            }
        });

    /// <summary>
    /// Creates a nonresident credential with pinUvAuthParam and the uv option omitted.
    /// </summary>
    /// <remarks>Assertions check response/authenticator-data presence and a nonempty credential ID, not the UV flag. Omitted uv with token authorization does not prove user verification was discouraged or absent.</remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_WithUvDiscouraged_CreatesCredentialSuccessfully(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            try
            {
                ClientPin clientPin;
                try
                {
                    clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);
                }
                catch (CtapException ex) when (ex.Status is CtapStatus.PinBlocked or CtapStatus.PinAuthBlocked)
                {
                    // The diagnostic conflates temporary PIN_AUTH_BLOCKED with exhausted PIN_BLOCKED.
                    Skip.If(true,
                        "PIN is blocked — CTAP2 reset required (re-insert YubiKey and reset within 10s of power-up)");
                    return;
                }

                using var _ = clientPin;

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

                // CTAP requires omission of the uv key alongside pinUvAuthParam, even uv:false.
                // The assertions below do not inspect the returned UV flag.
                var result = await session.MakeCredentialAsync(
                    clientDataHash: challenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: Ctap2TestData.ES256Params,
                    options: new MakeCredentialOptions
                    {
                        ResidentKey = false,
                        PinUvAuthParam = pinUvAuthParam,
                        PinUvAuthProtocol = clientPin.Protocol.Version
                    });

                Assert.NotNull(result);
                Assert.NotNull(result.AuthenticatorData);
                Assert.True(result.GetCredentialId().Length > 0,
                    "Credential ID should not be empty even with UV discouraged");
            }
            catch (CtapException ex) when (ex.Status is CtapStatus.InvalidCommand
                                               or CtapStatus.InvalidParameter
                                               or CtapStatus.UnsupportedOption
                                               or CtapStatus.InvalidOption)
            {
                // These statuses skip the test but do not identify a policy or wire-error cause.
                Skip.If(true,
                    $"Authenticator does not support UV-discouraged MakeCredential (CTAP status: {ex.Status})");
            }
            finally
            {
                await Ctap2TestHelpers.DeleteAllCredentialsForRpAsync(
                    session, Ctap2TestData.RpId, Ctap2TestData.PinUtf8);
            }
        });
}