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

using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Preferences;
using static Yubico.YubiKit.WebAuthn.IntegrationTests.WebAuthnTestHelpers;
using CredentialManagementClass = Yubico.YubiKit.Ctap2.CredentialManagement.CredentialManagement;

namespace Yubico.YubiKit.WebAuthn.IntegrationTests;

/// <summary>
/// Stress tests for large WebAuthn exclude lists. These tests create many credentials
/// and verify that the exclude list mechanism works at scale.
/// </summary>
/// <remarks>
/// <para>
/// Requires a freshly reset FIDO2 application on a dedicated test YubiKey. Reset destroys FIDO
/// credentials: only an operator who has explicitly approved this destructive run should use
/// <c>ykman fido reset</c> and perform the required physical reinsert before invoking the suite.
/// The test also deletes all credentials for its test RP during setup and cleanup.
/// </para>
/// <para>
/// Available discoverable-credential capacity may be insufficient on a device already in use.
/// The test reads credential-management metadata and skips when the reported capacity is too small;
/// it does not assume that unrelated FIDO settings or fingerprint enrollments consume credential slots.
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait(TestCategories.Category, TestCategories.Slow)]
[Trait("RequiresReset", "true")]
public class WebAuthnExcludeListStressTests
{
    /// <summary>
    /// Number of credentials to create before the excluded request. This exercises an exclude list
    /// larger than typical small-list limits. Discoverable capacity varies by model and firmware;
    /// devices with about 25 slots can accommodate 17+1, but reported metadata is the actual guard.
    /// </summary>
    private const int CredentialCount = 17;

    /// <summary>
    /// Creates 17 resident credentials for the same RP, builds an exclude list
    /// containing all of them, then attempts to create another credential for the
    /// same RP with a new random user ID. The WebAuthn Client should reject the request with
    /// <see cref="WebAuthnClientError"/> (code: <see cref="WebAuthnClientErrorCode.InvalidState"/>)
    /// because the authenticator recognizes a listed credential for that RP, regardless of the new user ID.
    /// </summary>
    /// <remarks>
    /// This test is marked as Slow because creating 17 credentials requires
    /// multiple user touches and takes significant time. See class-level remarks
    /// for the operator-Reset precondition; the test will Skip on lived-in
    /// devices with insufficient remaining discoverable-credential capacity.
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task CreateCredential_WithLargeExcludeList_RejectsExcludedCredential(YubiKeyTestState state)
    {
        // Note: WebAuthnClient.DisposeAsync cascades to dispose its backend, which
        // disposes the underlying Ctap2Session. Don't share a single session between
        // the client (which owns disposal) and post-test cleanup. Use one session
        // for the test body, a fresh one for cleanup.
        var createdCredentialIds = new List<ReadOnlyMemory<byte>>();

        try
        {
            // Use a separate setup session for PIN normalization, RP cleanup, and the capacity probe.
            // Release its connection before the client opens and owns the test body's session.
            int remainingCapacity;
            {
                await using var setupSession = await state.Device.CreateCtap2SessionAsync();

                await NormalizePinAsync(setupSession);

                var info = await setupSession.GetInfoAsync();
                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                await DeleteAllCredentialsForRpAsync(setupSession, TestRpId);

                // Capacity guard: the test needs CredentialCount + 1 free slots so
                // the final excluded CreateCredential reaches the exclude-list check
                // instead of hitting LimitExceeded. Skip cleanly when insufficient.
                var protocolVersion = info.PinUvAuthProtocols.Contains(2) ? 2 : 1;
                IPinUvAuthProtocol protocol = protocolVersion == 2
                    ? new PinUvAuthProtocolV2()
                    : new PinUvAuthProtocolV1();

                using var clientPin = new ClientPin(setupSession, protocol);

                byte[] capacityToken;
                if (supportsPermissions)
                {
                    capacityToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        KnownTestPin,
                        PinUvAuthTokenPermissions.CredentialManagement);
                }
                else
                {
                    capacityToken = await clientPin.GetPinTokenAsync(KnownTestPin);
                }

                using (protocol)
                {
                    var credMan = new CredentialManagementClass(setupSession, protocol, capacityToken);
                    var metadata = await credMan.GetCredentialsMetadataAsync();
                    remainingCapacity = metadata.MaxPossibleRemainingResidentCredentialsCount;
                }
                CryptographicOperations.ZeroMemory(capacityToken);
            } // Release the setup connection before opening the test body's session.

            Skip.If(remainingCapacity < CredentialCount + 1,
                $"Insufficient FIDO2 RK capacity ({remainingCapacity} remaining, " +
                $"need {CredentialCount + 1}). Run `ykman fido reset` and reinsert " +
                "the YubiKey to restore full capacity.");

            // BODY SESSION: fresh session for the WebAuthnClient. The client
            // owns its disposal and will dispose this session when it disposes.
            {
                await using var session = await state.Device.CreateCtap2SessionAsync();
                await using var client = CreateClient(session);

                // Create CredentialCount resident credentials
                for (var i = 0; i < CredentialCount; i++)
                {
                    var userId = RandomNumberGenerator.GetBytes(16);
                    var userName = $"user{i}@example.com";
                    var challenge = RandomNumberGenerator.GetBytes(32);

                    var options = new PublicKeyCredentialCreationOptions
                    {
                        Challenge = challenge,
                        Rp = new PublicKeyCredentialRpEntity(TestRpId, "Example Corp"),
                        User = new PublicKeyCredentialUserEntity(userId, userName, userName),
                        PubKeyCredParams = [CoseAlgorithm.Es256],
                        ResidentKey = ResidentKeyPreference.Required,
                        UserVerification = UserVerificationPreference.Discouraged
                    };

                    var response = await client.MakeCredentialAsync(
                        options,
                        Encoding.UTF8.GetBytes("11234567"));

                    createdCredentialIds.Add(response.CredentialId);
                }

                Assert.Equal(CredentialCount, createdCredentialIds.Count);

                // Build the exclude list from all created credentials
                var excludeList = createdCredentialIds
                    .Select(id => new PublicKeyCredentialDescriptor(id.ToArray()))
                    .ToList();

                // Attempt to create a new credential for the same RP with the exclude list.
                // The WebAuthnClient should map CredentialExcluded to InvalidState error.
                var finalUserId = RandomNumberGenerator.GetBytes(16);
                var finalChallenge = RandomNumberGenerator.GetBytes(32);

                var finalOptions = new PublicKeyCredentialCreationOptions
                {
                    Challenge = finalChallenge,
                    Rp = new PublicKeyCredentialRpEntity(TestRpId, "Example Corp"),
                    User = new PublicKeyCredentialUserEntity(finalUserId, "finaluser@example.com", "Final User"),
                    PubKeyCredParams = [CoseAlgorithm.Es256],
                    ResidentKey = ResidentKeyPreference.Required,
                    UserVerification = UserVerificationPreference.Discouraged,
                    ExcludeCredentials = excludeList
                };

                var ex = await Assert.ThrowsAsync<WebAuthnClientError>(async () =>
                {
                    await client.MakeCredentialAsync(
                        finalOptions,
                        Encoding.UTF8.GetBytes("11234567"));
                });

                Assert.True(
                    ex.Code == WebAuthnClientErrorCode.InvalidState,
                    $"Expected InvalidState. Got Code={ex.Code}, Message='{ex.Message}', " +
                    $"InnerType={ex.InnerException?.GetType().Name ?? "<none>"}, " +
                    $"InnerMsg='{ex.InnerException?.Message ?? "<none>"}'");
            }
        }
        finally
        {
            // Cleanup uses a fresh session because the test body's session is
            // disposed transitively when WebAuthnClient.DisposeAsync runs.
            try
            {
                await using var cleanupSession = await state.Device.CreateCtap2SessionAsync();
                await DeleteAllCredentialsForRpAsync(cleanupSession, TestRpId);
            }
            catch
            {
                // Cleanup is best-effort; swallow to surface the real test outcome.
            }
        }
    }
}