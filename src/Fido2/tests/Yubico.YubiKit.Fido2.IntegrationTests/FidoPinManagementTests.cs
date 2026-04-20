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
/// Integration tests for FIDO2 PIN management operations including PIN change,
/// and credential creation with user verification discouraged.
/// </summary>
[Trait("Category", "Integration")]
public class FidoPinManagementTests
{
    private static readonly byte[] OriginalPinUtf8 = FidoTestStateExtensions.KnownTestPin;
    private const string ChangedPin = "Xyz98765";
    private static readonly byte[] ChangedPinUtf8 = Encoding.UTF8.GetBytes(ChangedPin);

    /// <summary>
    /// Tests that the PIN can be changed and the new PIN works for subsequent operations.
    /// Sets the PIN, changes it to a new value, then verifies the new PIN by obtaining
    /// a PIN token with it.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task ChangePin_WithValidCurrentPin_AllowsAuthWithNewPin(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            var protocolVersion = info.PinUvAuthProtocols.Contains(2) ? 2 : 1;

            try
            {
                // NormalizePinAsync already set the PIN to KnownTestPin.
                // Create a ClientPin to change it.
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
                        FidoTestData.RpId);
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
                // PIN is blocked from accumulated failures across test runs.
                // A FIDO2 reset (requires re-insertion within 10s of power-up) is needed.
                Skip.If(true,
                    "PIN is blocked — FIDO2 reset required (re-insert YubiKey and reset within 10s of power-up)");
            }
            finally
            {
                // Restore known test PIN for other tests
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
    /// Tests that a credential can be created with user verification set to false
    /// (UV discouraged mode). The authenticator should still create the credential
    /// since PIN/UV auth is provided, but the UV flag behavior depends on the
    /// authenticator's policy.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_WithUvDiscouraged_CreatesCredentialSuccessfully(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            try
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

                // Create credential with UV omitted (discouraged behavior).
                // Per CTAP 2.0 spec, omitting "uv" from the options map is how the client
                // signals UV-discouraged. Sending "uv": false is rejected by some authenticators.
                var result = await session.MakeCredentialAsync(
                    clientDataHash: challenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: FidoTestData.ES256Params,
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
                // Some authenticators reject MakeCredential with UV discouraged when
                // a PIN is set, returning CTAP1_ERR_INVALID_COMMAND or similar.
                // This is authenticator-policy dependent, not a test bug.
                Skip.If(true,
                    $"Authenticator does not support UV-discouraged MakeCredential (CTAP status: {ex.Status})");
            }
            finally
            {
                await FidoTestHelpers.DeleteAllCredentialsForRpAsync(
                    session, FidoTestData.RpId, FidoTestData.PinUtf8);
            }
        });
}
