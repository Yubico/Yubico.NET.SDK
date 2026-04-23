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
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Client.Status;
using Yubico.YubiKit.WebAuthn.Cose;
using Yubico.YubiKit.WebAuthn.Preferences;
using static Yubico.YubiKit.WebAuthn.IntegrationTests.WebAuthnTestHelpers;

namespace Yubico.YubiKit.WebAuthn.IntegrationTests;

[Trait("Category", "Integration")]
public class WebAuthnClientTests
{
    private static RegistrationOptions CreateRegistrationOptions(
        ReadOnlyMemory<byte>? challenge = null,
        ResidentKeyPreference residentKey = ResidentKeyPreference.Discouraged)
    {
        Span<byte> challengeBytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(challengeBytes);

        Span<byte> userId = stackalloc byte[16];
        RandomNumberGenerator.Fill(userId);

        return new RegistrationOptions
        {
            Challenge = challenge ?? challengeBytes.ToArray(),
            Rp = new WebAuthnRelyingParty { Id = TestRpId, Name = "Example Corp" },
            User = new WebAuthnUser
            {
                Id = userId.ToArray(),
                Name = "testuser@example.com",
                DisplayName = "Test User"
            },
            PubKeyCredParams = [CoseAlgorithm.Es256],
            ResidentKey = residentKey,
            UserVerification = UserVerificationPreference.Discouraged
        };
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_NonResident_ReturnsValidResponse(YubiKeyTestState state)
    {
        await using var session = await state.Device
            .CreateFidoSessionAsync();

        await NormalizePinAsync(session);

        await using var client = CreateClient(session);

        var options = CreateRegistrationOptions();

        var response = await client.MakeCredentialAsync(
            options,
            pin: "11234567",
            useUv: false);

        Assert.NotNull(response);
        Assert.True(response.CredentialId.Length > 0, "Credential ID should not be empty");
        Assert.NotNull(response.PublicKey);
        Assert.NotNull(response.AttestationObject);
        Assert.NotNull(response.AuthenticatorData);
        Assert.True(response.RawAttestationObject.Length > 0);
        Assert.True(response.RawAuthenticatorData.Length > 0);
        Assert.NotNull(response.ClientData);
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_ResidentKey_ReturnsCredentialWithAaguid(YubiKeyTestState state)
    {
        await using var session = await state.Device
            .CreateFidoSessionAsync();

        await NormalizePinAsync(session);

        await using var client = CreateClient(session);

        var options = CreateRegistrationOptions(residentKey: ResidentKeyPreference.Required);

        var response = await client.MakeCredentialAsync(
            options,
            pin: "11234567",
            useUv: false);

        Assert.NotNull(response);
        Assert.True(response.CredentialId.Length > 0);
        Assert.NotEqual(Guid.Empty, response.Aaguid.Value);
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredentialStream_EmitsProcessingThenFinished(YubiKeyTestState state)
    {
        await using var session = await state.Device
            .CreateFidoSessionAsync();

        await NormalizePinAsync(session);

        await using var client = CreateClient(session);

        var options = CreateRegistrationOptions();
        var statuses = new List<WebAuthnStatus>();

        await foreach (var status in client.MakeCredentialStreamAsync(options))
        {
            statuses.Add(status);

            switch (status)
            {
                case WebAuthnStatusRequestingPin requestingPin:
                    await requestingPin.SubmitPin(KnownTestPin);
                    break;
                case WebAuthnStatusRequestingUv requestingUv:
                    await requestingUv.SetUseUv(false);
                    break;
                case WebAuthnStatusFailed failed:
                    throw failed.Error;
            }
        }

        Assert.Contains(statuses, s => s is WebAuthnStatusProcessing);
        Assert.Contains(statuses, s => s is WebAuthnStatusFinished<RegistrationResponse>);

        var finished = statuses.OfType<WebAuthnStatusFinished<RegistrationResponse>>().Single();
        Assert.True(finished.Result.CredentialId.Length > 0);
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task FullCeremony_RegisterThenAuthenticate_Succeeds(YubiKeyTestState state)
    {
        await using var session = await state.Device
            .CreateFidoSessionAsync();

        await NormalizePinAsync(session);

        // --- Registration ---
        await using var regClient = CreateClient(session);

        var regOptions = CreateRegistrationOptions(residentKey: ResidentKeyPreference.Required);

        var regResponse = await regClient.MakeCredentialAsync(
            regOptions,
            pin: "11234567",
            useUv: false);

        Assert.NotNull(regResponse);
        var credentialId = regResponse.CredentialId;
        Assert.True(credentialId.Length > 0);

        // Dispose the registration client (releases session ownership)
        await regClient.DisposeAsync();

        // --- Authentication ---
        // Need a new session since the backend took ownership
        await using var session2 = await state.Device
            .CreateFidoSessionAsync();

        await using var authClient = CreateClient(session2);

        var authOptions = new AuthenticationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            RpId = TestRpId,
            AllowCredentials =
            [
                new WebAuthnCredentialDescriptor(credentialId)
            ],
            UserVerification = UserVerificationPreference.Discouraged
        };

        var matches = await authClient.GetAssertionAsync(
            authOptions,
            pin: "11234567",
            useUv: false);

        Assert.NotEmpty(matches);

        var selected = matches[0];
        Assert.True(selected.Id.Length > 0);

        var authResponse = await selected.SelectAsync();
        Assert.NotNull(authResponse);
        Assert.True(authResponse.Signature.Length > 0);
        Assert.True(authResponse.RawAuthenticatorData.Length > 0);
        Assert.NotNull(authResponse.ClientData);
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task GetAssertion_DiscoverableCredential_ReturnsUserInfo(YubiKeyTestState state)
    {
        await using var session = await state.Device
            .CreateFidoSessionAsync();

        await NormalizePinAsync(session);

        // Register a discoverable credential first
        await using var regClient = CreateClient(session);

        var regOptions = CreateRegistrationOptions(residentKey: ResidentKeyPreference.Required);

        var regResponse = await regClient.MakeCredentialAsync(
            regOptions,
            pin: "11234567",
            useUv: false);

        await regClient.DisposeAsync();

        // Authenticate without allow list (discoverable)
        await using var session2 = await state.Device
            .CreateFidoSessionAsync();

        await using var authClient = CreateClient(session2);

        var authOptions = new AuthenticationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            RpId = TestRpId,
            UserVerification = UserVerificationPreference.Discouraged
        };

        var matches = await authClient.GetAssertionAsync(
            authOptions,
            pin: "11234567",
            useUv: false);

        Assert.NotEmpty(matches);

        var match = matches.First(m => m.Id.Span.SequenceEqual(regResponse.CredentialId.Span));
        Assert.NotNull(match.User);

        var authResponse = await match.SelectAsync();
        Assert.NotNull(authResponse);
        Assert.True(authResponse.Signature.Length > 0);
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_NoPinProvided_ThrowsNotAllowed(YubiKeyTestState state)
    {
        await using var session = await state.Device
            .CreateFidoSessionAsync();

        await NormalizePinAsync(session);

        await using var client = CreateClient(session);

        var options = CreateRegistrationOptions();

        var ex = await Assert.ThrowsAsync<WebAuthnClientError>(() =>
            client.MakeCredentialAsync(
                options,
                pin: (string?)null,
                useUv: false));

        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, ex.Code);
    }
}