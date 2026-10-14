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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.Ctap2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Ctap2.IntegrationTests;

/// <summary>
/// Integration tests for the CTAP2 credBlob extension.
/// credBlob allows storing a small blob of data (up to maxCredBlobLength bytes)
/// alongside a discoverable credential, retrievable during assertions.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Extension", "credBlob")]
public class Ctap2CredBlobTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task CredBlob_StoreAndRetrieve_ReturnsStoredData(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Extensions.Contains(ExtensionIdentifiers.CredBlob))
            {
                Skip.If(true, "YubiKey does not support credBlob extension");
                return;
            }

            byte[]? credentialId = null;

            try
            {
                // Guard against known SDK HID transport bug where
                // FidoHidConnection.ReceiveAsync can throw NullReferenceException
                // when the macOS HID RunLoop fails to dequeue a report in time.
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

                // Store a small blob with the credential
                var blobData = "hello-credblob"u8.ToArray();

                var extensions = new ExtensionBuilder()
                    .WithCredBlob(blobData)
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
                        Extensions = extensions
                    });

                credentialId = makeResult.GetCredentialId().ToArray();

                // Verify the blob was stored
                if (makeResult.ExtensionOutputs.HasValue)
                {
                    var extOutput = ExtensionOutput.DecodeWithRawData(makeResult.ExtensionOutputs.Value);
                    if (extOutput.TryGetCredBlobStored(out var stored))
                    {
                        Assert.True(stored, "credBlob should be stored successfully");
                    }
                }

                // Retrieve the blob via GetAssertion
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

                // Request credBlob retrieval during assertion
                var assertExtensions = new ExtensionBuilder()
                    .WithCredBlob(ReadOnlyMemory<byte>.Empty)
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

                // Verify we got the blob back
                if (assertionResult.ExtensionOutputs.HasValue)
                {
                    var assertExtOutput = ExtensionOutput.DecodeWithRawData(assertionResult.ExtensionOutputs.Value);
                    if (assertExtOutput.TryGetCredBlob(out var retrievedBlob))
                    {
                        Assert.Equal(blobData, retrievedBlob.ToArray());
                    }
                }
            }
            catch (Exception ex) when (
                (ex is NullReferenceException || ex is InvalidOperationException)
                && (ex.StackTrace?.Contains("FidoHidConnection") == true
                    || ex.StackTrace?.Contains("FidoHidProtocol") == true
                    || ex.StackTrace?.Contains("MacOSHidIOReport") == true))
            {
                Skip.If(true,
                    "Known SDK HID transport issue: macOS HID RunLoop failed to dequeue a report " +
                    $"({ex.GetType().Name}). This is an intermittent platform-level issue.");
            }
            finally
            {
                await Ctap2TestHelpers.DeleteAllCredentialsForRpAsync(session, Ctap2TestData.RpId, Ctap2TestData.PinUtf8);
            }
        });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task CredBlob_MaxLength_StoresSuccessfully(YubiKeyTestState state) =>
        await state.WithCtap2SessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Extensions.Contains(ExtensionIdentifiers.CredBlob))
            {
                Skip.If(true, "YubiKey does not support credBlob extension");
                return;
            }

            // Use maxCredBlobLength from authenticator info, default to 32
            var maxBlobLength = info.MaxCredBlobLength ?? 32;

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

                // Create a blob at the maximum allowed length
                var maxBlob = new byte[maxBlobLength];
                System.Security.Cryptography.RandomNumberGenerator.Fill(maxBlob);

                var extensions = new ExtensionBuilder()
                    .WithCredBlob(maxBlob)
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
                        Extensions = extensions
                    });

                Assert.NotNull(makeResult);

                // Verify the blob was stored
                if (makeResult.ExtensionOutputs.HasValue)
                {
                    var extOutput = ExtensionOutput.DecodeWithRawData(makeResult.ExtensionOutputs.Value);
                    if (extOutput.TryGetCredBlobStored(out var stored))
                    {
                        Assert.True(stored, "Max-length credBlob should be stored successfully");
                    }
                }
            }
            catch (Exception ex) when (
                (ex is NullReferenceException || ex is InvalidOperationException)
                && (ex.StackTrace?.Contains("FidoHidConnection") == true
                    || ex.StackTrace?.Contains("FidoHidProtocol") == true
                    || ex.StackTrace?.Contains("MacOSHidIOReport") == true))
            {
                Skip.If(true,
                    "Known SDK HID transport issue: macOS HID RunLoop failed to dequeue a report " +
                    $"({ex.GetType().Name}). This is an intermittent platform-level issue.");
            }
            finally
            {
                await Ctap2TestHelpers.DeleteAllCredentialsForRpAsync(session, Ctap2TestData.RpId, Ctap2TestData.PinUtf8);
            }
        });
}