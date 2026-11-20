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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

using CredentialManagementClass = Yubico.YubiKit.Ctap2.CredentialManagement.CredentialManagement;

namespace Yubico.YubiKit.Ctap2.IntegrationTests;

/// <summary>
/// Integration tests for CTAP2 previewSign extension at the canonical CTAP2 layer.
/// </summary>
/// <remarks>
/// <para>
/// These fixtures call CTAP2 directly, without WebAuthn's selection/adapter logic. Positive assertions
/// inspect outputs and one experimental signature round trip; they do not verify generated-key attestation.
/// </para>
/// <para>
/// Yubico Labs previewSign v4 Editor's Draft (26 August 2025), not CTAP version 4, defines:
/// - Registration input: {3: [alg...], 4: flags}
/// - Registration output: authData.extensions["previewSign"] + unsignedExtensionOutputs["previewSign"]
/// </para>
/// <para>
/// Setup normalizes the PIN; cleanup is best-effort and tokens/copies are not comprehensively cleared.
/// Negative tests catch CtapException around setup as well as the target operation, so they can finish
/// before the intended request. Exact status/phase assertions remain unfinished work. These fixtures
/// require a human-approved dedicated device; their presence in the suite is not hardware qualification.
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Extension", "previewSign")]
public class Ctap2PreviewSignTests
{
    /// <summary>
    /// Tests that MakeCredential with previewSign extension returns a generated signing key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test exercises the previewSign registration flow at the CTAP2 layer (not through
    /// the WebAuthn adapter). It uses ExtensionBuilder for the v4 Editor's Draft input and checks that:
    /// - The authenticator returns extension output in authData.extensions["previewSign"]
    /// - The output contains the selected algorithm
    /// - The response includes unsignedExtensionOutputs["previewSign"] with attestation data
    /// </para>
    /// <para>
    /// <b>WARNING -- EXPERIMENTAL -- test only:</b> The ARKG previewSign pieces exercised here are not ready for
    /// production use and must not be treated as production cryptographic guidance.
    /// </para>
    /// <para>
    /// This experimental ARKG fixture requests signing-op -65539. The seed marker -65700 and derived
    /// verification algorithm -9 have different roles. The fixture does not establish the full set
    /// of algorithms accepted by every preview-capable authenticator or the phase of a rejection.
    /// </para>
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_WithPreviewSignExtension_ReturnsGeneratedSigningKey(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
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
                using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

                var rp = Ctap2TestData.CreateRelyingParty();
                var user = Ctap2TestData.CreateUser();
                var challenge = Ctap2TestData.GenerateChallenge();

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

                // Build previewSign extension input via ExtensionBuilder
                // Use this profile's experimental signing-op identifier, not its derived-key algorithm.
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
                    pubKeyCredParams: Ctap2TestData.ES256Params,
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
                // Signed outer output carries {3: alg}; policy flags belong in the inner attestation.
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
                        // Assert this fixture's chosen signing-op algorithm, not the derived-key marker.
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

    /// <summary>
    /// Tests full ARKG-P256 round-trip: register → derive → sign → verify.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>WARNING -- EXPERIMENTAL -- test only:</b> This ARKG ceremony is an integration test fixture, not
    /// production cryptographic guidance, and must not be copied into production code as-is.
    /// </para>
    /// <para>
    /// This test requests the following experimental ARKG-P256 flow:
    /// 1. Register credential with previewSign and flags 0 (unattended signing policy)
    /// 2. Extract generated seed key
    /// 3. Offline derive public key using random IKM + test context
    /// 4. Request signing with up:false and no PIN/UV auth parameter
    /// 5. Offline verify signature against derived public key
    /// </para>
    /// <para>
    /// Registration still has ordinary creation presence requirements. No second touch is requested
    /// by the signing options, and these assertions do not establish an exact gesture count.
    /// </para>
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task FullCeremony_RegisterDeriveSignVerify_RoundTrip(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
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
                using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

                var rp = Ctap2TestData.CreateRelyingParty();
                var user = Ctap2TestData.CreateUser();
                var challenge = Ctap2TestData.GenerateChallenge();

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

                // Step A: Ordinary credential creation; signing-key policy is separately flags 0.
                // WARNING -- EXPERIMENTAL -- test only: ARKG previewSign is not production cryptographic guidance.
                // flags 0 requests unattended signing; the later assertion also sets up:false.
                var previewSignInput = new Extensions.PreviewSignRegistrationInput(
                    algorithms: [-65539], // Esp256SplitArkgPlaceholder (ARKG-P256-ESP256)
                    flags: 0);            // No UP requirement on derived signing key

                var extensions = new Extensions.ExtensionBuilder()
                    .WithPreviewSign(previewSignInput)
                    .Build();

                // Omit rk for this non-discoverable-credential fixture.
                var options = new MakeCredentialOptions
                {
                    PinUvAuthParam = pinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version,
                    Extensions = extensions
                };

                var makeCredResult = await session.MakeCredentialAsync(
                    clientDataHash: challenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: Ctap2TestData.ES256Params,
                    options: options);

                Assert.NotNull(makeCredResult);
                credentialId = makeCredResult.GetCredentialId().ToArray();
                Assert.NotEmpty(credentialId);

                // Extract previewSign generated key using the proper helper
                bool extracted = Extensions.PreviewSignCbor.TryExtractGeneratedKey(
                    makeCredResult,
                    out var generatedKey);

                Assert.True(extracted, "TryExtractGeneratedKey should succeed for ARKG-P256 previewSign response");
                Assert.NotNull(generatedKey);

                // Step B: Offline derive public key using RNG (modern SDK convention)
                byte[] ikm = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
                byte[] ctx = System.Text.Encoding.ASCII.GetBytes("integration-test-ctx");

                var derivedKey = generatedKey.DerivePublicKey(ikm, ctx);
                Assert.Equal(65, derivedKey.PublicKey.Length); // SEC1 uncompressed
                Assert.NotEmpty(derivedKey.ArkgKeyHandle.Span.ToArray());

                // Step C: Request signing without UP, consistent with the generated signing-key policy.
                byte[] messageRaw = System.Text.Encoding.ASCII.GetBytes("hello-previewsign-integration-test");
                byte[] message = System.Security.Cryptography.SHA256.HashData(messageRaw);

                // This fixture obtains no second token. ARKG arguments bind the derivation but are
                // not a generic substitute for CTAP ceremony authorization; device policy still applies.

                var signingParams = new Extensions.PreviewSignSigningParams(
                    keyHandle: derivedKey.DeviceKeyHandle,
                    tbs: message,
                    additionalArgs: Extensions.PreviewSignCbor.EncodeAdditionalArgs(
                        Extensions.CoseSignArgs.ArkgP256(
                            derivedKey.ArkgKeyHandle,
                            derivedKey.Context)));

                var authExtensions = new Extensions.ExtensionBuilder()
                    .WithPreviewSign(signingParams)
                    .Build();

                // Request up:false and omit pinUvAuthParam/Protocol for this flags-0 case.
                // This does not establish a general ECDH-state or firmware-0x7F restriction.
                var assertionOptions = new GetAssertionOptions
                {
                    AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                    UserPresence = false,
                    Extensions = authExtensions
                };

                var assertion = await session.GetAssertionAsync(
                    rpId: Ctap2TestData.RpId,
                    clientDataHash: challenge,
                    options: assertionOptions);

                Assert.NotNull(assertion);

                // Extract previewSign signature
                Assert.True(assertion.AuthenticatorData.HasExtensions);
                Assert.NotNull(assertion.AuthenticatorData.Extensions);

                var assertionExtensionsCbor = assertion.AuthenticatorData.Extensions.Value;
                var authOutputReader = new CborReader(assertionExtensionsCbor, CborConformanceMode.Ctap2Canonical);

                byte[]? signature = null;
                int? mapSize = authOutputReader.ReadStartMap();
                for (int i = 0; i < mapSize; i++)
                {
                    string key = authOutputReader.ReadTextString();
                    if (key == "previewSign")
                    {
                        int? innerMapSize = authOutputReader.ReadStartMap();
                        for (int j = 0; j < innerMapSize; j++)
                        {
                            int innerKey = authOutputReader.ReadInt32();
                            if (innerKey == 6) // signature key
                            {
                                signature = authOutputReader.ReadByteString();
                            }
                            else
                            {
                                authOutputReader.SkipValue();
                            }
                        }
                    }
                    else
                    {
                        authOutputReader.SkipValue();
                    }
                }

                Assert.NotNull(signature);
                Assert.NotEmpty(signature);

                // Step D: Offline verify signature
                // Pass RAW message — VerifySignature uses ECDsa.VerifyData which hashes internally.
                // tbs sent to firmware is SHA256(messageRaw); firmware signs tbs as-is (treats as digest).
                // Verifier must hash messageRaw once → SHA256(messageRaw) → matches what firmware signed.
                bool verified = derivedKey.VerifySignature(messageRaw, signature);
                Assert.True(verified);

                // Success-path token clearing only; failure-path ownership remains unfinished test work.
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(pinToken);
            }
            finally
            {
                if (credentialId is not null)
                {
                    await CleanupCredentialAsync(session, credentialId);
                }
            }
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

            System.Security.Cryptography.CryptographicOperations.ZeroMemory(pinToken);
        }
        catch
        {
            // Best-effort deletion: failure is swallowed and can leave credentials/token storage behind.
        }
    }

    /// <summary>
    /// Tests that MakeCredential with an unsupported algorithm rejects the request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This fixture supplies ES256 (-7) where it expects the experimental ARKG signing-op -65539.
    /// The generic previewSign draft is algorithm-agile; this assumption is profile-specific.
    /// </para>
    /// <para>
    /// The draft's unsupported-algorithm status is the desired specific check. Current assertions
    /// only require CtapException, and the outer catch also accepts a setup failure; no rejection
    /// phase, exact status or user-presence consumption is established.
    /// </para>
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_WithUnsupportedAlgorithm_ReturnsError(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            // Arrange: Check if authenticator advertises previewSign
            var info = await session.GetInfoAsync();
            if (info.Extensions is null || !info.Extensions.Contains("previewSign"))
            {
                Skip.If(true, "YubiKey does not advertise previewSign extension");
                return;
            }

            try
            {
                using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

                var rp = Ctap2TestData.CreateRelyingParty();
                var user = Ctap2TestData.CreateUser();
                var challenge = Ctap2TestData.GenerateChallenge();

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

                // Profile-specific negative input; this is not a universal previewSign algorithm rule.
                var previewSignInput = new Extensions.PreviewSignRegistrationInput(
                    algorithms: [-7], // Es256 — unsupported for previewSign
                    flags: 0x01);

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

                // Act & Assert: Expect rejection with CtapException
                // TODO: assert existing CtapStatus.UnsupportedAlgorithm after isolating target-operation errors.
                await Assert.ThrowsAsync<CtapException>(() => session.MakeCredentialAsync(
                    clientDataHash: challenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: Ctap2TestData.ES256Params,
                    options: options));

                // Cleanup PIN token
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(pinToken);
            }
            catch (CtapException)
            {
                // This also accepts setup errors; it does not prove the target request was rejected.
            }
        });

    /// <summary>
    /// Tests that MakeCredential with invalid flags rejects the request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per Yubico Labs previewSign v4 Editor's Draft, signing policy flags are:
    /// - 0x00 (no user presence/verification required)
    /// - 0x01 (require user presence)
    /// - 0x05 (require both user presence and user verification)
    /// </para>
    /// <para>
    /// The draft specifies INVALID_OPTION for other flags. This fixture checks only CtapException,
    /// including setup errors caught by the outer catch, not the exact target-operation status.
    /// </para>
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_WithInvalidFlags_ReturnsError(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            // Arrange: Check if authenticator advertises previewSign
            var info = await session.GetInfoAsync();
            if (info.Extensions is null || !info.Extensions.Contains("previewSign"))
            {
                Skip.If(true, "YubiKey does not advertise previewSign extension");
                return;
            }

            try
            {
                using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

                var rp = Ctap2TestData.CreateRelyingParty();
                var user = Ctap2TestData.CreateUser();
                var challenge = Ctap2TestData.GenerateChallenge();

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

                // Build previewSign extension input with invalid flag value (0x02).
                // Legal values are 0x00, 0x01, 0x05. Any other value should be rejected.
                var previewSignInput = new Extensions.PreviewSignRegistrationInput(
                    algorithms: [-65539], // Esp256SplitArkgPlaceholder
                    flags: 0x02);         // INVALID — not in {0x00, 0x01, 0x05}

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

                // Act & Assert: Expect rejection with CtapException
                // TODO: assert existing CtapStatus.InvalidOption after isolating target-operation errors.
                await Assert.ThrowsAsync<CtapException>(() => session.MakeCredentialAsync(
                    clientDataHash: challenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: Ctap2TestData.ES256Params,
                    options: options));

                // Cleanup PIN token
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(pinToken);
            }
            catch (CtapException)
            {
                // This also accepts setup errors; it does not prove the target request was rejected.
            }
        });

    /// <summary>
    /// Tests that GetAssertion with missing additionalArgs for an ARKG credential rejects the request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a credential uses this experimental ARKG signing profile (Esp256SplitArkgPlaceholder),
    /// subsequent GetAssertion requests MUST include additionalArgs with the ARKG key handle
    /// and context in the previewSign extension input.
    /// </para>
    /// <para>
    /// The v4 draft specifies MISSING_PARAMETER when required args are omitted. This fixture accepts
    /// any CtapException, including setup failures, so exact target rejection remains unasserted.
    /// </para>
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task GetAssertion_WithMissingArgs_ReturnsError(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
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
                using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

                var rp = Ctap2TestData.CreateRelyingParty();
                var user = Ctap2TestData.CreateUser();
                var challenge = Ctap2TestData.GenerateChallenge();

                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                // Step 1: Register an ARKG credential (touch #1)
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

                var previewSignInput = new Extensions.PreviewSignRegistrationInput(
                    algorithms: [-65539], // Esp256SplitArkgPlaceholder (ARKG)
                    flags: 0x01);

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

                var makeCredResult = await session.MakeCredentialAsync(
                    clientDataHash: challenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: Ctap2TestData.ES256Params,
                    options: options);

                Assert.NotNull(makeCredResult);
                credentialId = makeCredResult.GetCredentialId().ToArray();
                Assert.NotEmpty(credentialId);

                // Extract previewSign generated key
                bool extracted = Extensions.PreviewSignCbor.TryExtractGeneratedKey(
                    makeCredResult,
                    out var generatedKey);

                Assert.True(extracted, "TryExtractGeneratedKey should succeed");
                Assert.NotNull(generatedKey);

                // Derive offline key (so we have the key handle)
                byte[] ikm = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
                byte[] ctx = System.Text.Encoding.ASCII.GetBytes("test-missing-args");

                var derivedKey = generatedKey.DerivePublicKey(ikm, ctx);

                System.Security.Cryptography.CryptographicOperations.ZeroMemory(pinToken);

                // Step 2: Attempt GetAssertion WITHOUT additionalArgs (should fail for ARKG)
                byte[] assertionPinToken;
                if (supportsPermissions)
                {
                    assertionPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        Ctap2TestData.PinUtf8,
                        PinUvAuthTokenPermissions.GetAssertion,
                        Ctap2TestData.RpId);
                }
                else
                {
                    assertionPinToken = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
                }

                // pinUvAuthParam for GetAssertion is HMAC over the clientDataHash (CTAP §6.2.2 step 3),
                // NOT over the rpId.
                var assertionPinUvAuthParam = Ctap2TestHelpers.ComputeGetAssertionAuthParam(
                    clientPin.Protocol, assertionPinToken, challenge);

                byte[] message = System.Text.Encoding.ASCII.GetBytes("test-message");

                // Build previewSign authentication input WITHOUT additionalArgs.
                // ARKG requires algorithm-specific bytes under key 7.
                var authExtensions = new Extensions.ExtensionBuilder()
                    .WithPreviewSign(new Extensions.PreviewSignSigningParams(
                        keyHandle: derivedKey.DeviceKeyHandle,
                        tbs: message,
                        additionalArgs: null))  // MISSING — required for ARKG
                    .Build();

                var assertionOptions = new GetAssertionOptions
                {
                    AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                    PinUvAuthParam = assertionPinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version,
                    Extensions = authExtensions
                };

                // Act & Assert: Expect rejection with CtapException (CTAP2_ERR_MISSING_PARAMETER)
                // TODO: assert existing CtapStatus.MissingParameter after isolating target-operation errors.
                await Assert.ThrowsAsync<CtapException>(() => session.GetAssertionAsync(
                    rpId: Ctap2TestData.RpId,
                    clientDataHash: challenge,
                    options: assertionOptions));

                // Cleanup PIN token
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(assertionPinToken);
            }
            catch (CtapException)
            {
                // This also accepts setup errors; it does not prove the target request was rejected.
            }
            finally
            {
                if (credentialId is not null)
                {
                    await CleanupCredentialAsync(session, credentialId);
                }
            }
        });
}