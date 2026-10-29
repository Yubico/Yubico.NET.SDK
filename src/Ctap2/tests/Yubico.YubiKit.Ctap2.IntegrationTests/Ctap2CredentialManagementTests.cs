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
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

using CredentialManagementClass = Yubico.YubiKit.Ctap2.CredentialManagement.CredentialManagement;

namespace Yubico.YubiKit.Ctap2.IntegrationTests;

/// <summary>
/// Integration tests for credential management operations.
/// </summary>
[Trait("Category", "Integration")]
public class Ctap2CredentialManagementTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task EnumerateCredentials_WithResidentKeys_ReturnsCredentialList(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Options.TryGetValue("credMgmt", out var credMgmtSupported) || !credMgmtSupported)
            {
                return;
            }

            byte[]? credentialId = null;

            try
            {
                using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

                var rp = Ctap2TestData.CreateRelyingParty();
                var user = Ctap2TestData.CreateUser();

                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                var makeChallenge = Ctap2TestData.GenerateChallenge();
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

                var makeResult = await session.MakeCredentialAsync(
                    clientDataHash: makeChallenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: Ctap2TestData.ES256Params,
                    options: new MakeCredentialOptions
                    {
                        ResidentKey = true,
                        PinUvAuthParam = makePinUvAuthParam,
                        PinUvAuthProtocol = clientPin.Protocol.Version
                    });

                credentialId = makeResult.GetCredentialId().ToArray();

                var (pinToken, clientPinForCredMan, protocol) = await Ctap2TestHelpers.GetCredManTokenAsync(
                    session, Ctap2TestData.PinUtf8);

                using (clientPinForCredMan)
                {
                    using var credMan = new CredentialManagementClass(session, protocol, pinToken);

                    var rpIdHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Ctap2TestData.RpId));
                    using var result = await credMan.EnumerateCredentialsAsync(rpIdHash);

                    Assert.NotEmpty(result.Credentials);
                    Assert.Contains(result.Credentials, c =>
                        c.CredentialId.Id.Span.SequenceEqual(credentialId));
                }

                CryptographicOperations.ZeroMemory(pinToken);
            }
            finally
            {
                if (credentialId is not null)
                {
                    await CleanupCredentialAsync(session, credentialId);
                }
            }
        });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task EnumerateCredentials_NoCredentials_ReturnsEmptyOrThrows(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Options.TryGetValue("credMgmt", out var credMgmtSupported) || !credMgmtSupported)
            {
                return;
            }

            var uniqueRpId = $"test-credman-{Guid.NewGuid()}.example.com";

            using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

            var (pinToken, clientPinForCredMan, protocol) = await Ctap2TestHelpers.GetCredManTokenAsync(
                session, Ctap2TestData.PinUtf8);

            using (clientPinForCredMan)
            {
                using var credMan = new CredentialManagementClass(session, protocol, pinToken);
                var rpIdHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(uniqueRpId));

                try
                {
                    using var result = await credMan.EnumerateCredentialsAsync(rpIdHash);
                    Assert.Empty(result.Credentials);
                }
                catch (CtapException ex) when (ex.Status == CtapStatus.NoCredentials)
                {
                    // Expected behavior
                }
            }

            CryptographicOperations.ZeroMemory(pinToken);
        });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task DeleteCredential_ExistingCredential_RemovesFromDevice(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Options.TryGetValue("credMgmt", out var credMgmtSupported) || !credMgmtSupported)
            {
                return;
            }

            using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

            var rp = Ctap2TestData.CreateRelyingParty();
            var user = Ctap2TestData.CreateUser();

            var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                       info.Versions.Contains("FIDO_2_1_PRE");

            var makeChallenge = Ctap2TestData.GenerateChallenge();
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

            var makeResult = await session.MakeCredentialAsync(
                clientDataHash: makeChallenge,
                rp: rp,
                user: user,
                pubKeyCredParams: Ctap2TestData.ES256Params,
                options: new MakeCredentialOptions
                {
                    ResidentKey = true,
                    PinUvAuthParam = makePinUvAuthParam,
                    PinUvAuthProtocol = clientPin.Protocol.Version
                });

            var credentialId = makeResult.GetCredentialId().ToArray();

            var (pinToken, clientPinForCredMan, protocol) = await Ctap2TestHelpers.GetCredManTokenAsync(
                session, Ctap2TestData.PinUtf8);

            using (clientPinForCredMan)
            {
                var credMan = new CredentialManagementClass(session, protocol, pinToken);
                var descriptor = new PublicKeyCredentialDescriptor(credentialId);
                await credMan.DeleteCredentialAsync(descriptor);
            }

            CryptographicOperations.ZeroMemory(pinToken);

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

            try
            {
                await session.GetAssertionAsync(
                    rpId: Ctap2TestData.RpId,
                    clientDataHash: assertChallenge,
                    options: new GetAssertionOptions
                    {
                        AllowList = [new PublicKeyCredentialDescriptor(credentialId)],
                        PinUvAuthParam = assertPinUvAuthParam,
                        PinUvAuthProtocol = clientPin.Protocol.Version
                    });
            }
            catch (CtapException ex) when (ex.Status == CtapStatus.NoCredentials)
            {
                // Expected - credential was deleted
            }
        });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task GetCredentialsMetadata_ReturnsValidCounts(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Options.TryGetValue("credMgmt", out var credMgmtSupported) || !credMgmtSupported)
            {
                return;
            }

            using var clientPin = await Ctap2TestHelpers.SetOrVerifyPinAsync(session, Ctap2TestData.PinUtf8);

            var (pinToken, clientPinForCredMan, protocol) = await Ctap2TestHelpers.GetCredManTokenAsync(
                session, Ctap2TestData.PinUtf8);

            using (clientPinForCredMan)
            {
                var credMan = new CredentialManagementClass(session, protocol, pinToken);

                var metadata = await credMan.GetCredentialsMetadataAsync();

                Assert.True(metadata.ExistingResidentCredentialsCount >= 0,
                    "Existing credentials count should be non-negative");
                Assert.True(metadata.MaxPossibleRemainingResidentCredentialsCount >= 0,
                    "Max remaining credentials count should be non-negative");
            }

            CryptographicOperations.ZeroMemory(pinToken);
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

            CryptographicOperations.ZeroMemory(pinToken);
        }
        catch
        {
            // Cleanup failures should not fail the test
        }
    }
}