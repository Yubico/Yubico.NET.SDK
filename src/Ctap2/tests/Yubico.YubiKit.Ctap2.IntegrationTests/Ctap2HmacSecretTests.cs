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
using Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Ctap2.IntegrationTests;

/// <summary>
/// Integration tests for CTAP2 hmac-secret extension.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Extension", "hmac-secret")]
public class Ctap2HmacSecretTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetInfo_ReportsHmacSecretSupport(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();

            Assert.NotNull(info.Extensions);

            var supportsHmacSecret = info.Extensions.Contains(ExtensionIdentifiers.HmacSecret);

            if (supportsHmacSecret)
            {
                Assert.Contains(ExtensionIdentifiers.HmacSecret, info.Extensions);
            }
            else
            {
                Skip.If(true, "YubiKey does not support hmac-secret extension");
            }
        });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task MakeCredential_WithHmacSecretEnabled_ReturnsHmacSecretExtension(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Extensions.Contains(ExtensionIdentifiers.HmacSecret))
            {
                Skip.If(true, "YubiKey does not support hmac-secret extension");
                return;
            }

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

            var extensions = new ExtensionBuilder()
                .WithHmacSecretMakeCredential()
                .Build();

            var options = new MakeCredentialOptions
            {
                ResidentKey = false,
                PinUvAuthParam = pinUvAuthParam,
                PinUvAuthProtocol = clientPin.Protocol.Version,
                Extensions = extensions
            };

            var result = await session.MakeCredentialAsync(
                clientDataHash: challenge,
                rp: rp,
                user: user,
                pubKeyCredParams: Ctap2TestData.ES256Params,
                options: options);

            Assert.NotNull(result);

            if (result.ExtensionOutputs.HasValue)
            {
                var extOutput = ExtensionOutput.Decode(result.ExtensionOutputs.Value);
                if (extOutput.HasExtensions)
                {
                    Assert.Contains(ExtensionIdentifiers.HmacSecret, extOutput.ExtensionIds);
                }
            }

            Assert.True(result.GetCredentialId().Length > 0, "Credential ID should not be empty");
        });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task GetAssertion_WithHmacSecret_ReturnsDerivedSecret(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Extensions.Contains(ExtensionIdentifiers.HmacSecret))
            {
                Skip.If(true, "YubiKey does not support hmac-secret extension");
                return;
            }

            byte[]? credentialId = null;

            try
            {
                using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

                var rp = Ctap2TestData.CreateRelyingParty();
                var user = Ctap2TestData.CreateUser();
                var makeChallenge = Ctap2TestData.GenerateChallenge();

                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                byte[] makePinToken;
                if (supportsPermissions)
                {
                    makePinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        Ctap2TestData.PinUtf8,
                        PinUvAuthTokenPermissions.MakeCredential,
                        Ctap2TestData.RpId);
                }
                else
                {
                    makePinToken = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
                }

                var makePinUvAuthParam = Ctap2TestHelpers.ComputeMakeCredentialAuthParam(
                    clientPin.Protocol, makePinToken, makeChallenge);

                var makeExtensions = new ExtensionBuilder()
                    .WithHmacSecretMakeCredential()
                    .Build();

                var makeResult = await session.MakeCredentialAsync(
                    clientDataHash: makeChallenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: Ctap2TestData.ES256Params,
                    options: new MakeCredentialOptions
                    {
                        ResidentKey = true,
                        PinUvAuthParam = makePinUvAuthParam,
                        PinUvAuthProtocol = clientPin.Protocol.Version,
                        Extensions = makeExtensions
                    });

                credentialId = makeResult.GetCredentialId().ToArray();

                var assertChallenge = Ctap2TestData.GenerateChallenge();
                byte[] assertPinToken;
                if (supportsPermissions)
                {
                    assertPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        Ctap2TestData.PinUtf8,
                        PinUvAuthTokenPermissions.GetAssertion,
                        Ctap2TestData.RpId);
                }
                else
                {
                    assertPinToken = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
                }

                var assertPinUvAuthParam = Ctap2TestHelpers.ComputeGetAssertionAuthParam(
                    clientPin.Protocol, assertPinToken, assertChallenge);

                var authenticatorKeyAgreement = await clientPin.GetKeyAgreementAsync();
                var (platformKeyAgreement, sharedSecret) = clientPin.Protocol.Encapsulate(authenticatorKeyAgreement);

                var salt1 = new byte[32];
                RandomNumberGenerator.Fill(salt1);

                var assertExtensions = new ExtensionBuilder()
                    .WithHmacSecret(clientPin.Protocol, sharedSecret, platformKeyAgreement, salt1)
                    .Build();

                var assertionResult = await session.GetAssertionAsync(
                    rpId: Ctap2TestData.RpId,
                    clientDataHash: assertChallenge,
                    options: new GetAssertionOptions
                    {
                        AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                        PinUvAuthParam = assertPinUvAuthParam,
                        PinUvAuthProtocol = clientPin.Protocol.Version,
                        Extensions = assertExtensions
                    });

                Assert.NotNull(assertionResult);
                Assert.NotNull(assertionResult.ExtensionOutputs);

                var extOutput = ExtensionOutput.Decode(assertionResult.ExtensionOutputs.Value);
                Assert.True(extOutput.TryGetHmacSecret(out var hmacOutput), "hmac-secret output should be present");
                Assert.NotNull(hmacOutput);

                var decrypted = clientPin.Protocol.Decrypt(sharedSecret, hmacOutput!.Output.Span);
                Assert.Equal(32, decrypted.Length);
                Assert.False(decrypted.ToArray().All(b => b == 0), "Derived secret should not be all zeros");

                CryptographicOperations.ZeroMemory(sharedSecret);
                CryptographicOperations.ZeroMemory(decrypted);
            }
            finally
            {
                await Ctap2TestHelpers.DeleteAllCredentialsForRpAsync(session, Ctap2TestData.RpId, Ctap2TestData.PinUtf8);
            }
        });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task GetAssertion_WithSameSalt_ReturnsSameSecret(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Extensions.Contains(ExtensionIdentifiers.HmacSecret))
            {
                Skip.If(true, "YubiKey does not support hmac-secret extension");
                return;
            }

            byte[]? credentialId = null;

            try
            {
                using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

                var rp = Ctap2TestData.CreateRelyingParty();
                var user = Ctap2TestData.CreateUser();
                var makeChallenge = Ctap2TestData.GenerateChallenge();

                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                byte[] makePinToken;
                if (supportsPermissions)
                {
                    makePinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        Ctap2TestData.PinUtf8,
                        PinUvAuthTokenPermissions.MakeCredential,
                        Ctap2TestData.RpId);
                }
                else
                {
                    makePinToken = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
                }

                var makePinUvAuthParam = Ctap2TestHelpers.ComputeMakeCredentialAuthParam(
                    clientPin.Protocol, makePinToken, makeChallenge);

                var makeExtensions = new ExtensionBuilder()
                    .WithHmacSecretMakeCredential()
                    .Build();

                var makeResult = await session.MakeCredentialAsync(
                    clientDataHash: makeChallenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: Ctap2TestData.ES256Params,
                    options: new MakeCredentialOptions
                    {
                        ResidentKey = true,
                        PinUvAuthParam = makePinUvAuthParam,
                        PinUvAuthProtocol = clientPin.Protocol.Version,
                        Extensions = makeExtensions
                    });

                credentialId = makeResult.GetCredentialId().ToArray();

                var salt1 = new byte[32];
                RandomNumberGenerator.Fill(salt1);

                byte[] secret1, secret2;

                // First assertion
                {
                    var assertChallenge = Ctap2TestData.GenerateChallenge();
                    byte[] assertPinToken;
                    if (supportsPermissions)
                    {
                        assertPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                            Ctap2TestData.PinUtf8,
                            PinUvAuthTokenPermissions.GetAssertion,
                            Ctap2TestData.RpId);
                    }
                    else
                    {
                        assertPinToken = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
                    }

                    var assertPinUvAuthParam = Ctap2TestHelpers.ComputeGetAssertionAuthParam(
                        clientPin.Protocol, assertPinToken, assertChallenge);

                    var authenticatorKeyAgreement = await clientPin.GetKeyAgreementAsync();
                    var (platformKeyAgreement, sharedSecret) = clientPin.Protocol.Encapsulate(authenticatorKeyAgreement);

                    var assertExtensions = new ExtensionBuilder()
                        .WithHmacSecret(clientPin.Protocol, sharedSecret, platformKeyAgreement, salt1)
                        .Build();

                    var assertionResult = await session.GetAssertionAsync(
                        rpId: Ctap2TestData.RpId,
                        clientDataHash: assertChallenge,
                        options: new GetAssertionOptions
                        {
                            AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                            PinUvAuthParam = assertPinUvAuthParam,
                            PinUvAuthProtocol = clientPin.Protocol.Version,
                            Extensions = assertExtensions
                        });

                    Assert.NotNull(assertionResult.ExtensionOutputs);
                    var extOutput = ExtensionOutput.Decode(assertionResult.ExtensionOutputs.Value);
                    Assert.True(extOutput.TryGetHmacSecret(out var hmacOutput));

                    secret1 = clientPin.Protocol.Decrypt(sharedSecret, hmacOutput!.Output.Span);
                    CryptographicOperations.ZeroMemory(sharedSecret);
                }

                // Second assertion with same salt
                {
                    var assertChallenge = Ctap2TestData.GenerateChallenge();
                    byte[] assertPinToken;
                    if (supportsPermissions)
                    {
                        assertPinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                            Ctap2TestData.PinUtf8,
                            PinUvAuthTokenPermissions.GetAssertion,
                            Ctap2TestData.RpId);
                    }
                    else
                    {
                        assertPinToken = await clientPin.GetPinTokenAsync(Ctap2TestData.PinUtf8);
                    }

                    var assertPinUvAuthParam = Ctap2TestHelpers.ComputeGetAssertionAuthParam(
                        clientPin.Protocol, assertPinToken, assertChallenge);

                    var authenticatorKeyAgreement = await clientPin.GetKeyAgreementAsync();
                    var (platformKeyAgreement, sharedSecret) = clientPin.Protocol.Encapsulate(authenticatorKeyAgreement);

                    var assertExtensions = new ExtensionBuilder()
                        .WithHmacSecret(clientPin.Protocol, sharedSecret, platformKeyAgreement, salt1)
                        .Build();

                    var assertionResult = await session.GetAssertionAsync(
                        rpId: Ctap2TestData.RpId,
                        clientDataHash: assertChallenge,
                        options: new GetAssertionOptions
                        {
                            AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                            PinUvAuthParam = assertPinUvAuthParam,
                            PinUvAuthProtocol = clientPin.Protocol.Version,
                            Extensions = assertExtensions
                        });

                    Assert.NotNull(assertionResult.ExtensionOutputs);
                    var extOutput = ExtensionOutput.Decode(assertionResult.ExtensionOutputs.Value);
                    Assert.True(extOutput.TryGetHmacSecret(out var hmacOutput));

                    secret2 = clientPin.Protocol.Decrypt(sharedSecret, hmacOutput!.Output.Span);
                    CryptographicOperations.ZeroMemory(sharedSecret);
                }

                Assert.Equal(32, secret1.Length);
                Assert.Equal(32, secret2.Length);
                Assert.True(secret1.SequenceEqual(secret2), "Same salt should produce same derived secret");

                CryptographicOperations.ZeroMemory(secret1);
                CryptographicOperations.ZeroMemory(secret2);
            }
            finally
            {
                await Ctap2TestHelpers.DeleteAllCredentialsForRpAsync(session, Ctap2TestData.RpId, Ctap2TestData.PinUtf8);
            }
        });
}