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
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;
using Yubico.YubiKit.WebAuthn.Preferences;
using Fido2Extensions = Yubico.YubiKit.Fido2.Extensions;
using static Yubico.YubiKit.WebAuthn.IntegrationTests.WebAuthnTestHelpers;

namespace Yubico.YubiKit.WebAuthn.IntegrationTests;

[Trait("Category", "Integration")]
public class PreviewSignTests
{
    [SkippableTheory]
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
            Rp = new PublicKeyCredentialRpEntity(TestRpId, "Example Corp"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "testuser@example.com", "Test User"
            ),
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

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task FullCeremony_RegisterWithPreviewSign_ThenSign_ReturnsSignature(YubiKeyTestState state)
    {
        // SKIPPED for automated CI: this test exercises the full ARKG-P256 previewSign
        // authentication ceremony end-to-end. It requires:
        //   - Physical YubiKey 5.8.0-beta over USB HID
        //   - User touch (presence) at the GetAssertion call
        //   - A real ARKG (arkg_kh, ctx) pair derived from the registration's COSE seed key.
        //
        // Phase 10 §3 (this PRD) ships the typed CoseSignArgs builder so the body below can be
        // written. ARKG seed-key derivation (which produces arkg_kh and ctx) lives in a separate
        // Phase 10 follow-up (Yubico.Core port of ArkgPrimitivesOpenSsl.cs) — until that lands,
        // the placeholder bytes below will not survive firmware verification, so the test stays
        // skipped in CI. Dennis runs this manually against hardware once a real (kh, ctx) pair
        // is available.
        //
        // Verified Phase 10 §3 builder invariants (covered by unit tests):
        //   ✅ Wire alg = -65539 (CoseAlgorithm.ArkgP256), NOT -9 (output sig alg)
        //   ✅ KH must be exactly 81 bytes (16-byte HMAC tag || 65-byte SEC1 P-256 point)
        //   ✅ CTX must be ≤64 bytes (HKDF length-byte prefix bound)
        //   ✅ COSE_Sign_Args map = {3: -65539, -1: kh, -2: ctx}, CTAP2-canonical order
        //   ✅ Wrapped as bstr at outer authentication input key 7
        //
        // Engineer-implemented (Phase 10 §3 typed CoseSignArgs builder),
        // awaiting Dennis hardware verification once ARKG seed-key derivation lands.
        Skip.If(true,
            "previewSign FullCeremony requires hardware (USB HID + user touch) AND a real " +
            "ARKG (kh, ctx) pair. Engineer-implemented (Phase 10 §3 typed CoseSignArgs builder), " +
            "awaiting Dennis hardware verification.");

        // --- Phase 1: Registration with previewSign ARKG-P256 key generation ---
        await using var session1 = await state.Device.CreateFidoSessionAsync();

        Skip.IfNot(await SupportsPreviewSignAsync(session1),
            "YubiKey does not advertise previewSign extension");

        await NormalizePinAsync(session1);

        await using var regClient = CreateClient(session1);

        var regOptions = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity(TestRpId, "Example Corp"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "signer@example.com", "Signer"
            ),
            PubKeyCredParams = [CoseAlgorithm.Es256],
            ResidentKey = ResidentKeyPreference.Required,
            UserVerification = UserVerificationPreference.Discouraged,
            // ARKG-P256 (-65539) is the only algorithm YK 5.8.0-beta accepts for the auth path.
            // -9 (Esp256) is the OUTPUT signature alg, not the request alg — sending -9 here
            // is the bug class the typed CoseSignArgs builder makes unrepresentable.
            Extensions = new RegistrationExtensionInputs(
                PreviewSign: PreviewSignRegistrationInput.GenerateKey(
                    CoseAlgorithm.ArkgP256))
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

        // TODO(Phase 10 follow-up): replace placeholder ARKG (kh, ctx) with a real pair derived
        // from generatedKey.PublicKey via the (forthcoming) Yubico.Core ARKG port. Until then
        // the firmware will reject this auth. The encoder shape itself is exercised by unit tests.
        byte[] arkgKeyHandle = new byte[81];
        arkgKeyHandle[16] = 0x04; // SEC1 leading byte
        byte[] arkgContext = "ARKG-P256.test vectors"u8.ToArray();

        await regClient.DisposeAsync();

        // --- Phase 2: Authentication with typed CoseSignArgs (Phase 10 §3 builder) ---
        await using var session2 = await state.Device.CreateFidoSessionAsync();

        await using var authClient = CreateClient(session2);

        var messageBytes = Encoding.UTF8.GetBytes("Hello from previewSign integration test!");
        var toBeSigned = SHA256.HashData(messageBytes);

        var signByCredential = new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>(
            ByteArrayKeyComparer.Instance)
        {
            [credentialId] = new PreviewSignSigningParams(
                keyHandle: keyHandle,
                tbs: toBeSigned,
                coseSignArgs: Fido2Extensions.CoseSignArgs.ArkgP256(arkgKeyHandle, arkgContext))
        };

        var authOptions = new AuthenticationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            RpId = TestRpId,
            AllowCredentials = [new PublicKeyCredentialDescriptor(credentialId)],
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