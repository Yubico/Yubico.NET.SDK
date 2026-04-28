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

using System.Formats.Cbor;
using Xunit;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

using CredentialManagementClass = Yubico.YubiKit.Fido2.CredentialManagement.CredentialManagement;
using CtapException = Yubico.YubiKit.Fido2.Ctap.CtapException;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for FIDO2 previewSign extension at the canonical Fido2 layer.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that the Fido2 layer correctly handles previewSign extension inputs
/// and outputs, independent of the WebAuthn-layer adapter logic.
/// </para>
/// <para>
/// The previewSign extension uses CTAP v4 draft wire format with integer-keyed CBOR maps:
/// - Registration input: {3: [alg...], 4: flags}
/// - Registration output: authData.extensions["previewSign"] + unsignedExtensionOutputs["previewSign"]
/// </para>
/// <para>
/// Per the architectural principle: "Fido2 is the canonical FIDO2 resource. WebAuthn integration
/// tests should be supplementary at best." This file closes the gap surfaced in Phase 9.5 where
/// WebAuthn proved previewSign registration on hardware but Fido2 did not have an equivalent test.
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Extension", "previewSign")]
public class FidoPreviewSignTests
{
    /// <summary>
    /// Tests that MakeCredential with previewSign extension returns a generated signing key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test exercises the previewSign registration flow at the Fido2 layer (not through
    /// the WebAuthn adapter). It manually constructs the CBOR-encoded extension input per
    /// CTAP v4 draft specification and verifies that:
    /// - The authenticator returns extension output in authData.extensions["previewSign"]
    /// - The output contains the selected algorithm
    /// - The response includes unsignedExtensionOutputs["previewSign"] with attestation data
    /// </para>
    /// <para>
    /// YubiKey 5.8.0-beta firmware accepts only Esp256SplitArkgPlaceholder
    /// (COSE algorithm -65539, "ARKG-P256-ESP256") as the request alg for previewSign.
    /// Esp256 (-9) describes the *output signature* algorithm internally — it must NEVER appear
    /// on the wire as the request alg. Sending -9 yields an "Unsupported algorithm" rejection
    /// at firmware protocol-decode time. Verified across python-fido2, cnh-authenticator-rs,
    /// and the Yubico.NET.SDK-Legacy preview-sign branch (commit fe82b007).
    /// </para>
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_WithPreviewSignExtension_ReturnsGeneratedSigningKey(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            // Arrange: Check if authenticator advertises previewSign
            var info = await session.GetInfoAsync();
            if (info.Extensions is null || !info.Extensions.Contains("previewSign"))
            {
                Skip.If(true, "YubiKey does not advertise previewSign extension");
                return;
            }

            byte[]? credentialId = null;

            try
            {
                using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.PinUtf8);

                var rp = FidoTestData.CreateRelyingParty();
                var user = FidoTestData.CreateUser();
                var challenge = FidoTestData.GenerateChallenge();

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

                // Build previewSign extension input via ExtensionBuilder
                // Using Esp256SplitArkgPlaceholder (-65539) — the only request alg YubiKey
                // 5.8.0-beta accepts for previewSign+ARKG. Sending -9 (Esp256) here yields
                // an "Unsupported algorithm" rejection at protocol-decode time.
                var previewSignInput = new Extensions.PreviewSignRegistrationInput(
                    algorithms: [-65539], // Esp256SplitArkgPlaceholder (ARKG-P256-ESP256)
                    flags: 0x01);         // RequireUserPresence

                var extensions = new Extensions.ExtensionBuilder()
                    .WithPreviewSign(previewSignInput)
                    .Build();

                var options = new MakeCredentialOptions
                {
                    ResidentKey = true,
                    PinUvAuthParam = pinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version,
                    Extensions = extensions
                };

                // Act
                var result = await session.MakeCredentialAsync(
                    clientDataHash: challenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: FidoTestData.ES256Params,
                    options: options);

                // Assert
                Assert.NotNull(result);
                Assert.NotNull(result.AuthenticatorData);
                credentialId = result.GetCredentialId().ToArray();
                Assert.NotEmpty(credentialId);

                // Verify previewSign extension output is present in authenticator data
                Assert.True(result.AuthenticatorData.HasExtensions,
                    "AuthenticatorData should have extensions flag set");
                Assert.NotNull(result.AuthenticatorData.Extensions);

                // Decode the extensions CBOR to verify previewSign is present
                // Extensions is a CBOR map: {"previewSign": {3: alg, 4: flags}}
                var extensionsReader = new CborReader(
                    result.AuthenticatorData.Extensions.Value,
                    CborConformanceMode.Ctap2Canonical);

                bool foundPreviewSign = false;
                int? mapSize = extensionsReader.ReadStartMap();
                for (int i = 0; i < mapSize; i++)
                {
                    string key = extensionsReader.ReadTextString();
                    if (key == "previewSign")
                    {
                        foundPreviewSign = true;
                        // Decode the previewSign output to verify algorithm.
                        // YK 5.8.0-beta echoes back the negotiated request alg (-65539,
                        // Esp256SplitArkgPlaceholder), NOT the internal output sig alg (-9, Esp256).
                        var algorithm = DecodePreviewSignAlgorithm(extensionsReader);
                        Assert.Equal(-65539, algorithm); // Esp256SplitArkgPlaceholder (ARKG-P256-ESP256)
                    }
                    else
                    {
                        extensionsReader.SkipValue();
                    }
                }

                Assert.True(foundPreviewSign, "previewSign extension output not found in authenticator data");

                // Verify unsignedExtensionOutputs contains previewSign (attestation object)
                Assert.NotNull(result.UnsignedExtensionOutputs);
                Assert.True(result.UnsignedExtensionOutputs.ContainsKey("previewSign"),
                    "unsignedExtensionOutputs should contain previewSign attestation data");
                Assert.True(result.UnsignedExtensionOutputs["previewSign"].Length > 0,
                    "previewSign attestation data should not be empty");
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
    /// Decodes the algorithm from previewSign extension output CBOR.
    /// </summary>
    /// <param name="reader">CborReader positioned at the previewSign value (a CBOR map).</param>
    /// <returns>The COSE algorithm identifier.</returns>
    private static int DecodePreviewSignAlgorithm(CborReader reader)
    {
        int? mapSize = reader.ReadStartMap();

        for (int i = 0; i < mapSize; i++)
        {
            int key = reader.ReadInt32();
            if (key == 3) // algorithm
            {
                return reader.ReadInt32();
            }
            reader.SkipValue();
        }

        throw new InvalidOperationException("previewSign output missing algorithm (key 3)");
    }

    private static async Task CleanupCredentialAsync(FidoSession session, byte[] credentialId)
    {
        try
        {
            var (pinToken, clientPin, protocol) = await FidoTestHelpers.GetCredManTokenAsync(
                session, FidoTestData.PinUtf8);

            using (clientPin)
            {
                var credMan = new CredentialManagementClass(session, protocol, pinToken);
                var descriptor = new PublicKeyCredentialDescriptor(credentialId);
                await credMan.DeleteCredentialAsync(descriptor);
            }

            System.Security.Cryptography.CryptographicOperations.ZeroMemory(pinToken);
        }
        catch
        {
            // Cleanup failures should not fail the test
        }
    }
}
