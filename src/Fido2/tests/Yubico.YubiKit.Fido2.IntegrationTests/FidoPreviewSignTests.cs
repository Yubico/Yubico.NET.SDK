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
    /// <b>WARNING -- EXPERIMENTAL -- test only:</b> The ARKG previewSign pieces exercised here are not ready for
    /// production use and must not be treated as production cryptographic guidance.
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

    /// <summary>
    /// Tests full ARKG-P256 round-trip: register → derive → sign → verify.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>WARNING -- EXPERIMENTAL -- test only:</b> This ARKG ceremony is an integration test fixture, not
    /// production cryptographic guidance, and must not be copied into production code as-is.
    /// </para>
    /// <para>
    /// This test exercises the complete previewSign ARKG-P256 ceremony:
    /// 1. Register credential with previewSign (touch #1)
    /// 2. Extract generated seed key
    /// 3. Offline derive public key using random IKM + test context
    /// 4. Sign arbitrary message via GetAssertion with ARKG key handle (touch #2)
    /// 5. Offline verify signature against derived public key
    /// </para>
    /// <para>
    /// Mirrors Legacy SDK test at Yubico.NET.SDK-Legacy/.../PreviewSignTests.cs:62-109.
    /// </para>
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task FullCeremony_RegisterDeriveSignVerify_RoundTrip(YubiKeyTestState state) =>
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

                // Step A: Register with previewSign extension (touch #1)
                // WARNING -- EXPERIMENTAL -- test only: ARKG previewSign is not production cryptographic guidance.
                // flags: 0 matches python-fido2 reference (silent assertion at GetAssertion time)
                var previewSignInput = new Extensions.PreviewSignRegistrationInput(
                    algorithms: [-65539], // Esp256SplitArkgPlaceholder (ARKG-P256-ESP256)
                    flags: 0);            // No UP requirement on derived signing key

                var extensions = new Extensions.ExtensionBuilder()
                    .WithPreviewSign(previewSignInput)
                    .Build();

                // Phase D: mirror python-fido2 reference — non-resident credential, no rk option
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
                    pubKeyCredParams: FidoTestData.ES256Params,
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

                // Step C: Sign with derived credential (touch #2)
                byte[] messageRaw = System.Text.Encoding.ASCII.GetBytes("hello-previewsign-integration-test");
                byte[] message = System.Security.Cryptography.SHA256.HashData(messageRaw);

                // Phase D: skip second PIN token acquisition (python-fido2 doesn't do this).
                // The previewSign extension carries its own auth via the inner args field;
                // re-running ClientPin protocol after MakeCredential resets ECDH state and
                // appears to invalidate the firmware's previewSign verification context.

                var comparer = new MemoryByteEqualityComparer();
                var signByCredential = new Dictionary<ReadOnlyMemory<byte>, Extensions.PreviewSignSigningParams>(comparer)
                {
                    [credentialId] = new Extensions.PreviewSignSigningParams(
                        keyHandle: derivedKey.DeviceKeyHandle,
                        tbs: message,
                        additionalArgs: Extensions.PreviewSignCbor.EncodeAdditionalArgs(
                            Extensions.CoseSignArgs.ArkgP256(
                                derivedKey.ArkgKeyHandle,
                                derivedKey.Context)))
                };

                var authInput = new Extensions.PreviewSignAuthenticationInput(signByCredential);
                var authExtensions = new Extensions.ExtensionBuilder()
                    .WithPreviewSign(authInput)
                    .Build();

                // Phase D root-cause fix: mirror python-fido2 reference exactly.
                // - previewSign extension carries its own auth via inner args field
                // - omit CTAP-level pinUvAuthParam/Protocol (firmware returns 0x7F otherwise)
                // - options={up:false} for silent assertion (matches flags=0 registration)
                var assertionOptions = new GetAssertionOptions
                {
                    AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                    UserPresence = false,
                    Extensions = authExtensions
                };

                var assertion = await session.GetAssertionAsync(
                    rpId: FidoTestData.RpId,
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

                // Cleanup PIN tokens
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

    /// <summary>
    /// Equality comparer for ReadOnlyMemory&lt;byte&gt; that compares byte sequences.
    /// </summary>
    private sealed class MemoryByteEqualityComparer : IEqualityComparer<ReadOnlyMemory<byte>>
    {
        public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
        {
            return x.Span.SequenceEqual(y.Span);
        }

        public int GetHashCode(ReadOnlyMemory<byte> obj)
        {
            var hash = new HashCode();
            foreach (byte b in obj.Span)
            {
                hash.Add(b);
            }
            return hash.ToHashCode();
        }
    }

    /// <summary>
    /// Tests that MakeCredential with an unsupported algorithm rejects the request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Mirrors python-fido2 test_register_unsupported_alg (test_sign_extension_v4.py:407-413).
    /// The YubiKey 5.8.0-beta firmware accepts ONLY Esp256SplitArkgPlaceholder (-65539)
    /// as the request algorithm for previewSign+ARKG. Sending a standard FIDO2 algorithm
    /// like Es256 (-7) yields an "Unsupported algorithm" rejection.
    /// </para>
    /// <para>
    /// Expected behavior: The authenticator rejects at protocol-decode time with a
    /// CTAP2_ERR_UNSUPPORTED_ALGORITHM error. This may occur before user touch is consumed,
    /// depending on firmware implementation.
    /// </para>
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_WithUnsupportedAlgorithm_ReturnsError(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
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

                // Build previewSign extension input with Es256 (-7) — a standard FIDO2 algorithm
                // that the previewSign extension does NOT accept. YubiKey 5.8.0-beta firmware
                // only accepts Esp256SplitArkgPlaceholder (-65539).
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
                // TODO: tighten error-code assertion when CTAP2_ERR_UNSUPPORTED_ALGORITHM constant is added
                await Assert.ThrowsAsync<CtapException>(() => session.MakeCredentialAsync(
                    clientDataHash: challenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: FidoTestData.ES256Params,
                    options: options));

                // Cleanup PIN token
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(pinToken);
            }
            catch (CtapException)
            {
                // Expected - test passes if we get CtapException
            }
        });

    /// <summary>
    /// Tests that MakeCredential with invalid flags rejects the request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Mirrors python-fido2 test_register_invalid_flags (test_sign_extension_v4.py:416-424).
    /// Per CTAP v4 draft, previewSign flags must be one of:
    /// - 0x00 (no user presence/verification required)
    /// - 0x01 (require user presence)
    /// - 0x05 (require user verification)
    /// </para>
    /// <para>
    /// Values outside this set (e.g., 0x02) are rejected with CTAP2_ERR_INVALID_OPTION.
    /// </para>
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_WithInvalidFlags_ReturnsError(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
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
                // TODO: tighten error-code assertion when CTAP2_ERR_INVALID_OPTION constant is added
                await Assert.ThrowsAsync<CtapException>(() => session.MakeCredentialAsync(
                    clientDataHash: challenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: FidoTestData.ES256Params,
                    options: options));

                // Cleanup PIN token
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(pinToken);
            }
            catch (CtapException)
            {
                // Expected - test passes if we get CtapException
            }
        });

    /// <summary>
    /// Tests that GetAssertion with missing additionalArgs for an ARKG credential rejects the request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Mirrors python-fido2 test_assert_missing_args (test_sign_extension_v4.py:578-596).
    /// When a credential is created with an ARKG algorithm (Esp256SplitArkgPlaceholder),
    /// subsequent GetAssertion requests MUST include additionalArgs with the ARKG key handle
    /// and context in the previewSign extension input.
    /// </para>
    /// <para>
    /// Omitting additionalArgs when it's required yields CTAP2_ERR_MISSING_PARAMETER.
    /// </para>
    /// </remarks>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task GetAssertion_WithMissingArgs_ReturnsError(YubiKeyTestState state) =>
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

                // Step 1: Register an ARKG credential (touch #1)
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
                    pubKeyCredParams: FidoTestData.ES256Params,
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
                        FidoTestData.PinUtf8,
                        PinUvAuthTokenPermissions.GetAssertion,
                        FidoTestData.RpId);
                }
                else
                {
                    assertionPinToken = await clientPin.GetPinTokenAsync(FidoTestData.PinUtf8);
                }

                // pinUvAuthParam for GetAssertion is HMAC over the clientDataHash (CTAP §6.2.2 step 3),
                // NOT over the rpId.
                var assertionPinUvAuthParam = FidoTestHelpers.ComputeGetAssertionAuthParam(
                    clientPin.Protocol, assertionPinToken, challenge);

                byte[] message = System.Text.Encoding.ASCII.GetBytes("test-message");

                // Build previewSign authentication input WITHOUT additionalArgs.
                // ARKG requires algorithm-specific bytes under key 7.
                var comparer = new MemoryByteEqualityComparer();
                var signByCredential = new Dictionary<ReadOnlyMemory<byte>, Extensions.PreviewSignSigningParams>(comparer)
                {
                    [credentialId] = new Extensions.PreviewSignSigningParams(
                        keyHandle: derivedKey.DeviceKeyHandle,
                        tbs: message,
                        additionalArgs: null)  // MISSING — required for ARKG
                };

                var authInput = new Extensions.PreviewSignAuthenticationInput(signByCredential);
                var authExtensions = new Extensions.ExtensionBuilder()
                    .WithPreviewSign(authInput)
                    .Build();

                var assertionOptions = new GetAssertionOptions
                {
                    AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                    PinUvAuthParam = assertionPinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version,
                    Extensions = authExtensions
                };

                // Act & Assert: Expect rejection with CtapException (CTAP2_ERR_MISSING_PARAMETER)
                // TODO: tighten error-code assertion when CTAP2_ERR_MISSING_PARAMETER constant is added
                await Assert.ThrowsAsync<CtapException>(() => session.GetAssertionAsync(
                    rpId: FidoTestData.RpId,
                    clientDataHash: challenge,
                    options: assertionOptions));

                // Cleanup PIN token
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(assertionPinToken);
            }
            catch (CtapException)
            {
                // Expected - test passes if we get CtapException
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