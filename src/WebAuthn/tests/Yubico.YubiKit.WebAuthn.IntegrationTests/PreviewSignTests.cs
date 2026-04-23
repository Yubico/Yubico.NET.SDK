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
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Cose;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;
using Yubico.YubiKit.WebAuthn.Preferences;
using static Yubico.YubiKit.WebAuthn.IntegrationTests.WebAuthnTestHelpers;

namespace Yubico.YubiKit.WebAuthn.IntegrationTests;

[Trait("Category", "Integration")]
public class PreviewSignTests
{
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task Registration_WithPreviewSign_ReturnsGeneratedSigningKey(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreateFidoSessionAsync();

        Skip.IfNot(await SupportsPreviewSignAsync(session),
            "YubiKey does not advertise previewSign extension");

        await NormalizePinAsync(session);

        await using var client = CreateClient(session);

        var regOptions = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new WebAuthnRelyingParty { Id = TestRpId, Name = "Example Corp" },
            User = new WebAuthnUser
            {
                Id = RandomNumberGenerator.GetBytes(16),
                Name = "testuser@example.com",
                DisplayName = "Test User"
            },
            PubKeyCredParams = [CoseAlgorithm.Es256],
            ResidentKey = ResidentKeyPreference.Required,
            UserVerification = UserVerificationPreference.Discouraged,
            Extensions = new RegistrationExtensionInputs(
                PreviewSign: PreviewSignRegistrationInput.GenerateKey(
                    CoseAlgorithm.Esp256, CoseAlgorithm.EdDsa, CoseAlgorithm.Es256,
                    CoseAlgorithm.Esp256SplitArkgPlaceholder))
        };

        var response = await client.MakeCredentialAsync(
            regOptions,
            pin: "11234567",
            useUv: false);

        Assert.NotNull(response);
        Assert.True(response.CredentialId.Length > 0);

        var previewSignOutput = response.ClientExtensionResults?.PreviewSign;
        Assert.NotNull(previewSignOutput);

        var generatedKey = previewSignOutput.GeneratedKey;
        Assert.True(generatedKey.KeyHandle.Length > 0, "KeyHandle should not be empty");
        Assert.NotNull(generatedKey.PublicKey);
        Assert.True(generatedKey.Algorithm.IsKnown, $"Algorithm {generatedKey.Algorithm} should be known");
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature(YubiKeyTestState state)
    {
        // TODO Phase 9.3: Hardware verification with user presence
        // This test requires physical touch (two times: registration + authentication).
        // The wire format has been verified byte-for-byte against the Rust reference (cnh-authenticator-rs).
        // See PreviewSignCborEncodingTests for deterministic byte-level assertions.
        //
        // Note: Esp256 (-9) is an ARKG algorithm requiring additional_args (COSE_Sign_Args with arkg_kh + ctx).
        // ARKG support is deferred to Phase 10. For Phase 9.2/9.3, this test should use non-ARKG algorithms
        // (Es256 or EdDsa) to avoid requiring ARKG additional_args during authentication.

        // --- Phase 1: Registration with previewSign key generation ---
        await using var session1 = await state.Device.CreateFidoSessionAsync();

        Skip.IfNot(await SupportsPreviewSignAsync(session1),
            "YubiKey does not advertise previewSign extension");

        await NormalizePinAsync(session1);

        await using var regClient = CreateClient(session1);

        var regOptions = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new WebAuthnRelyingParty { Id = TestRpId, Name = "Example Corp" },
            User = new WebAuthnUser
            {
                Id = RandomNumberGenerator.GetBytes(16),
                Name = "signer@example.com",
                DisplayName = "Signer"
            },
            PubKeyCredParams = [CoseAlgorithm.Es256],
            ResidentKey = ResidentKeyPreference.Required,
            UserVerification = UserVerificationPreference.Discouraged,
            Extensions = new RegistrationExtensionInputs(
                PreviewSign: PreviewSignRegistrationInput.GenerateKey(
                    CoseAlgorithm.Es256, CoseAlgorithm.EdDsa))
                // Phase 9.2: Using non-ARKG algorithms only. Esp256 and Esp256SplitArkgPlaceholder
                // require ARKG additional_args during authentication (deferred to Phase 10).
        };

        var regResponse = await regClient.MakeCredentialAsync(
            regOptions,
            pin: "11234567",
            useUv: false);

        Assert.NotNull(regResponse.ClientExtensionResults?.PreviewSign);

        var credentialId = regResponse.CredentialId;
        var generatedKey = regResponse.ClientExtensionResults!.PreviewSign!.GeneratedKey;
        var keyHandle = generatedKey.KeyHandle;

        Assert.True(keyHandle.Length > 0);

        await regClient.DisposeAsync();

        // --- Phase 2: Authentication with previewSign signing ---
        await using var session2 = await state.Device.CreateFidoSessionAsync();

        await using var authClient = CreateClient(session2);

        var messageBytes = Encoding.UTF8.GetBytes("Hello from previewSign integration test!");
        var toBeSigned = SHA256.HashData(messageBytes);

        var signByCredential = new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>(
            ByteArrayKeyComparer.Instance)
        {
            [credentialId] = new PreviewSignSigningParams(
                keyHandle: keyHandle,
                tbs: toBeSigned)
        };

        var authOptions = new AuthenticationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            RpId = TestRpId,
            AllowCredentials = [new WebAuthnCredentialDescriptor(credentialId)],
            UserVerification = UserVerificationPreference.Discouraged,
            Extensions = new AuthenticationExtensionInputs(
                PreviewSign: new PreviewSignAuthenticationInput(signByCredential))
        };

        var matches = await authClient.GetAssertionAsync(
            authOptions,
            pin: "11234567",
            useUv: false);

        Assert.NotEmpty(matches);

        var match = matches[0];
        var authResponse = await match.SelectAsync();

        Assert.NotNull(authResponse);
        Assert.True(authResponse.Signature.Length > 0, "Standard assertion signature should be present");

        var previewSignOutput = authResponse.ClientExtensionResults?.PreviewSign;
        Assert.NotNull(previewSignOutput);
        Assert.True(previewSignOutput.Signature.Length > 0,
            "previewSign signature over TBS data should not be empty");
    }
}