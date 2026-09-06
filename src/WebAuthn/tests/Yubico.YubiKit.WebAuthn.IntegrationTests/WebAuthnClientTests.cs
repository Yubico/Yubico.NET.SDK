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
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
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
            Rp = new PublicKeyCredentialRpEntity(TestRpId, "Example Corp"),
            User = new PublicKeyCredentialUserEntity(userId.ToArray(), "testuser@example.com", "Test User"
            ),
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
            Encoding.UTF8.GetBytes("11234567"));

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
            Encoding.UTF8.GetBytes("11234567"));

        Assert.NotNull(response);
        Assert.True(response.CredentialId.Length > 0);
        Assert.NotEqual(Guid.Empty, response.Aaguid.Value);
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
            Encoding.UTF8.GetBytes("11234567"));

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
                new PublicKeyCredentialDescriptor(credentialId)
            ],
            UserVerification = UserVerificationPreference.Discouraged
        };

        var matches = await authClient.GetAssertionAsync(
            authOptions,
            Encoding.UTF8.GetBytes("11234567"));

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
            Encoding.UTF8.GetBytes("11234567"));

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
            Encoding.UTF8.GetBytes("11234567"));

        Assert.NotEmpty(matches);

        var match = matches.First(m => m.Id.Span.SequenceEqual(regResponse.CredentialId.Span));
        Assert.NotNull(match.User);

        var authResponse = await match.SelectAsync();
        Assert.NotNull(authResponse);
        Assert.True(authResponse.Signature.Length > 0);
    }

    /// <summary>
    /// Registration with <see cref="UserVerificationPreference.Discouraged"/> and no PIN must
    /// succeed and produce a credential that was NOT user-verified.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The relying party asking for <c>discouraged</c> is asking for no user verification. Demanding
    /// a PIN anyway would make a non-UV credential impossible to create, so the ceremony proceeds
    /// with no <c>pinUvAuthParam</c> on the wire.
    /// </para>
    /// <para>
    /// The flag assertions are the point of the test: user presence and user verification are
    /// independent. Touch is still required (CTAP never lets a client set <c>up</c> on
    /// makeCredential, so the authenticator default of true applies), while UV must stay clear.
    /// </para>
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_UvDiscouragedAndNoPinProvided_CreatesNonVerifiedCredential(
        YubiKeyTestState state)
    {
        await using var session = await state.Device
            .CreateFidoSessionAsync();

        // A PIN is deliberately set on the authenticator. Discouraged must still skip verification.
        await NormalizePinAsync(session);

        await using var client = CreateClient(session);

        var options = CreateRegistrationOptions();

        var response = await client.MakeCredentialAsync(options, pinBytes: null);

        Assert.NotNull(response);
        Assert.True(response.CredentialId.Length > 0, "Credential ID should not be empty");

        Assert.True(
            response.AuthenticatorData.UserPresent,
            "User presence must be set: makeCredential always requires touch, independent of UV.");
        Assert.False(
            response.AuthenticatorData.UserVerified,
            "User verification must be clear: the relying party asked for UV=Discouraged.");
    }
}