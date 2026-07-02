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
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;
using Yubico.YubiKit.WebAuthn.Preferences;
using static Yubico.YubiKit.WebAuthn.IntegrationTests.WebAuthnTestHelpers;
using Fido2Extensions = Yubico.YubiKit.Fido2.Extensions;

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
        // Full ARKG-P256 previewSign authentication ceremony end-to-end:
        // WARNING -- EXPERIMENTAL -- test only: ARKG previewSign is not ready for production use and must not be
        // treated as production cryptographic guidance.
        //   - Register with previewSign ARKG-P256 key generation (touch #1)
        //   - Offline derive public key using ARKG primitives
        //   - Sign arbitrary message via GetAssertion (touch #2)
        //   - Offline verify signature against derived public key
        //
        // Requires:
        //   - Physical previewSign-capable YubiKey over USB HID
        //   - User touch (presence) at both MakeCredential and GetAssertion
        //   - Real ARKG (kh, ctx) pair derived from registration COSE seed key

        // --- Registration with previewSign ARKG-P256 key generation ---
        await using var session1 = await state.Device.CreateFidoSessionAsync();

        Skip.IfNot(await SupportsPreviewSignAsync(session1),
            "YubiKey does not advertise previewSign extension");

        // Verified end-to-end on YK 5.8.0 after switching generic previewSign auth to raw
        // additionalArgs and using the ARKG bridge to build the algorithm-specific payload.

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
            // WARNING -- EXPERIMENTAL -- test only: ARKG-P256 (-65539) is the request signing algorithm for this ARKG path.
            // -9 (Esp256) is the OUTPUT signature alg, not the request alg — sending -9 here
            // is the bug class the ARKG additionalArgs bridge makes explicit.
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

        // Extract ARKG seed key from WebAuthn layer's CoseKey wrapper
        var arkgSeedKey = generatedKey.PublicKey as Fido2.Cose.CoseArkgP256SeedKey;
        Assert.NotNull(arkgSeedKey);

        // Offline derive public key using ARKG primitives.
        byte[] ikm = RandomNumberGenerator.GetBytes(32);
        byte[] ctx = Encoding.ASCII.GetBytes("integration-test-ctx");

        // Convert WebAuthn GeneratedSigningKey to Fido2 PreviewSignGeneratedKey
        var fido2GeneratedKey = ConvertToFido2GeneratedKey(keyHandle, arkgSeedKey);

        var derivedKey = fido2GeneratedKey.DerivePublicKey(ikm, ctx);
        Assert.Equal(65, derivedKey.PublicKey.Length); // SEC1 uncompressed
        Assert.NotEmpty(derivedKey.ArkgKeyHandle.Span.ToArray());

        await regClient.DisposeAsync();

        // --- Authentication with ARKG additionalArgs bridge ---
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
                additionalArgs: Fido2Extensions.PreviewSignCbor.EncodeAdditionalArgs(
                    Fido2Extensions.CoseSignArgs.ArkgP256(
                        derivedKey.ArkgKeyHandle,
                        derivedKey.Context)))
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

        // --- Offline verify signature ---
        bool verified = derivedKey.VerifySignature(messageBytes, previewSignOutput.Signature.Span);
        Assert.True(verified, "Signature verification should succeed for derived public key");
    }

    /// <summary>
    /// Helper to convert WebAuthn GeneratedSigningKey to Fido2 PreviewSignGeneratedKey.
    /// </summary>
    /// <remarks>
    /// WARNING -- EXPERIMENTAL -- test only: this ARKG bridge exists for integration-test coverage and must not be
    /// treated as production cryptographic guidance.
    /// <para>
    /// This bridges the WebAuthn layer (which exposes CoseKey directly) to the Fido2 layer
    /// (which has the DerivePublicKey method). Uses reflection because PreviewSignGeneratedKey's
    /// constructor is internal to the Fido2 assembly.
    /// </para>
    /// </remarks>
    private static Fido2.Extensions.PreviewSignGeneratedKey ConvertToFido2GeneratedKey(
        ReadOnlyMemory<byte> keyHandle,
        Fido2.Cose.CoseArkgP256SeedKey arkgSeedKey)
    {
        var generatedKeyType = typeof(Fido2.Extensions.PreviewSignGeneratedKey);
        var constructor = generatedKeyType.GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            [typeof(ReadOnlyMemory<byte>), typeof(ReadOnlyMemory<byte>), typeof(ReadOnlyMemory<byte>), typeof(Fido2.Cose.CoseAlgorithm)],
            null);

        if (constructor is null)
        {
            throw new InvalidOperationException(
                "PreviewSignGeneratedKey constructor not found. This indicates a breaking change in the Fido2 layer.");
        }

        return (Fido2.Extensions.PreviewSignGeneratedKey)constructor.Invoke([
            keyHandle,
            arkgSeedKey.BlPublicKey,
            arkgSeedKey.KemPublicKey,
            arkgSeedKey.Algorithm
        ]);
    }
}