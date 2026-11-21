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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

using CredentialManagementClass = Yubico.YubiKit.Ctap2.CredentialManagement.CredentialManagement;

namespace Yubico.YubiKit.Ctap2.IntegrationTests;

/// <summary>
/// Integration tests for MakeCredential operations.
/// </summary>
/// <remarks>
/// These tests exercise credential creation workflows on real YubiKeys.
/// Tests requiring user presence (touch) are marked with the appropriate trait.
/// Use a human-coordinated dedicated device with known PIN state; setup can normalize the PIN.
/// Test hashes are synthetic, not serialized-client-data bindings. These assertions inspect parsed
/// fields/status, not attestation trust. Deletion is best effort and not every test deletes credentials;
/// token/auth buffers lack comprehensive finally-zeroing, so this is not a secure application template.
/// </remarks>
[Trait("Category", "Integration")]
public class Ctap2MakeCredentialTests
{
    /// <summary>
    /// Requests a non-discoverable credential and checks parsed fields and a nonempty credential ID.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_NonResidentKey_ReturnsValidAttestation(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            // Arrange
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

            var options = new MakeCredentialOptions
            {
                ResidentKey = false,
                PinUvAuthParam = pinUvAuthParam,
                PinUvAuthProtocol = clientPin.Protocol.Version
            };

            // Act
            var result = await session.MakeCredentialAsync(
                clientDataHash: challenge,
                rp: rp,
                user: user,
                pubKeyCredParams: Ctap2TestData.ES256Params,
                options: options);

            // No attestation verification. Guid.Empty also encodes to 16 bytes, so length alone does not prove AAGUID presence.
            Assert.NotNull(result);
            Assert.NotNull(result.AuthenticatorData);
            Assert.NotNull(result.Format);
            Assert.True(result.GetCredentialId().Length > 0, "Credential ID should not be empty");
            Assert.Equal(16, result.GetAaguid().ToByteArray().Length);
        });

    /// <summary>
    /// Requests discoverable creation and checks attested data and a nonempty ID, not later RP-ID discovery.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_ResidentKey_ReturnsCredentialId(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            byte[]? credentialId = null;

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

                var options = new MakeCredentialOptions
                {
                    ResidentKey = true,
                    PinUvAuthParam = pinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version
                };

                var result = await session.MakeCredentialAsync(
                    clientDataHash: challenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: Ctap2TestData.ES256Params,
                    options: options);

                Assert.NotNull(result);
                Assert.NotNull(result.AuthenticatorData.AttestedCredentialData);
                credentialId = result.GetCredentialId().ToArray();
                Assert.NotEmpty(credentialId);
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
    /// Tests that MakeCredential with exclude list throws CredentialExcluded when credential exists.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_WithExcludeList_ThrowsCredentialExcluded(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            byte[]? credentialId = null;

            try
            {
                using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

                var rp = Ctap2TestData.CreateRelyingParty();
                var user = Ctap2TestData.CreateUser();

                var info = await session.GetInfoAsync();
                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                // Create first credential
                var challenge1 = Ctap2TestData.GenerateChallenge();
                byte[] pinToken1;
                if (supportsPermissions)
                {
                    pinToken1 = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        Ctap2TestData.PinUtf8,
                        PinUvAuthTokenPermissions.MakeCredential,
                        Ctap2TestData.RpId);
                }
                else
                {
                    pinToken1 = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
                }

                var pinUvAuthParam1 = Ctap2TestHelpers.ComputeMakeCredentialAuthParam(
                    clientPin.Protocol, pinToken1, challenge1);

                var firstResult = await session.MakeCredentialAsync(
                    clientDataHash: challenge1,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: Ctap2TestData.ES256Params,
                    options: new MakeCredentialOptions
                    {
                        ResidentKey = true,
                        PinUvAuthParam = pinUvAuthParam1,
                        PinUvAuthProtocol = clientPin.Protocol.Version
                    });

                credentialId = firstResult.GetCredentialId().ToArray();

                // Act & Assert: Try to create with same credential in exclude list
                var excludeList = new[] { new PublicKeyCredentialDescriptor(credentialId) };

                var challenge2 = Ctap2TestData.GenerateChallenge();
                byte[] pinToken2;
                if (supportsPermissions)
                {
                    pinToken2 = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        Ctap2TestData.PinUtf8,
                        PinUvAuthTokenPermissions.MakeCredential,
                        Ctap2TestData.RpId);
                }
                else
                {
                    pinToken2 = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
                }

                var pinUvAuthParam2 = Ctap2TestHelpers.ComputeMakeCredentialAuthParam(
                    clientPin.Protocol, pinToken2, challenge2);

                var ex = await Assert.ThrowsAsync<CtapException>(async () =>
                {
                    await session.MakeCredentialAsync(
                        clientDataHash: challenge2,
                        rp: rp,
                        user: user,
                        pubKeyCredParams: Ctap2TestData.ES256Params,
                        options: new MakeCredentialOptions
                        {
                            ResidentKey = true,
                            ExcludeList = excludeList,
                            PinUvAuthParam = pinUvAuthParam2,
                            PinUvAuthProtocol = clientPin.Protocol.Version
                        });
                });

                Assert.Equal(CtapStatus.CredentialExcluded, ex.Status);
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
    /// Tests that MakeCredential fails when PIN/UV auth is required but not provided.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Configures/verifies a PIN, then requests discoverable creation without token authentication.
    /// The assertion accepts one of four CTAP statuses; it does not establish which status every
    /// firmware must return or prove rejection for every other operation.
    /// </para>
    /// <para>
    /// Setup can fail or skip before the target request. PIN_AUTH_BLOCKED is temporary and can require
    /// a power cycle; PIN_BLOCKED is exhausted and may require reset. The unchanged skip string does
    /// not distinguish these states or the product-specific reset interaction window.
    /// </para>
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_WhenPinUvAuthRequiredButNotProvided_ThrowsCtapError(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            // Establish PIN state for this discoverable-creation request, not a universal PIN requirement.
            try
            {
                using var _ = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);
            }
            catch (CtapException ex) when (ex.Status is CtapStatus.PinBlocked or CtapStatus.PinAuthBlocked)
            {
                Skip.If(true,
                    "PIN is blocked — CTAP2 reset required (re-insert YubiKey and reset within 10s of power-up)");
                return;
            }

            var rp = Ctap2TestData.CreateRelyingParty();
            var user = Ctap2TestData.CreateUser();
            var challenge = Ctap2TestData.GenerateChallenge();

            // Options WITHOUT pinUvAuthParam (no PIN provided)
            var options = new MakeCredentialOptions
            {
                ResidentKey = true,
                // PinUvAuthParam = null,  // Explicitly NOT providing PIN auth
                // PinUvAuthProtocol = null
            };

            // Act & Assert: Expect a CTAP error when PIN/UV auth is required but absent.
            var ctapEx = await Assert.ThrowsAsync<CtapException>(async () =>
            {
                await session.MakeCredentialAsync(
                    clientDataHash: challenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: Ctap2TestData.ES256Params,
                    options: options);
            });

            // The accepted four-code set bounds this test; it is not per-firmware status qualification.
            Assert.True(
                ctapEx.Status is CtapStatus.PuatRequired or CtapStatus.PinInvalid or CtapStatus.PinAuthInvalid or CtapStatus.MissingParameter,
                $"Expected PuatRequired/PinInvalid/PinAuthInvalid/MissingParameter, got {ctapEx.Status}");
        });

    private static async Task CleanupCredentialAsync(Ctap2Session session, byte[] credentialId)
    {
        try
        {
            var (pinToken, clientPin, protocol) = await Ctap2TestHelpers.GetCredManTokenAsync(
                session, Ctap2TestData.PinUtf8);

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
            // Best-effort deletion preserves the primary result; failure can leave credentials and token bytes.
        }
    }
}