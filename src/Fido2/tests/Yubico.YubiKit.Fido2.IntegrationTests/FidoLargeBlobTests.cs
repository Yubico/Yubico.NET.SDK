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
using Xunit;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Fido2.LargeBlobs;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Integration tests for the FIDO2 largeBlob extension and large blob storage.
/// Tests credential-associated large blob data storage and retrieval.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Extension", "largeBlob")]
public class FidoLargeBlobTests
{
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("RequiresUserPresence", "true")]
    public async Task LargeBlob_StoreAndRetrieve_RoundTripsData(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Options.TryGetValue("largeBlobs", out var largeBlobsSupported) || !largeBlobsSupported)
            {
                Skip.If(true, "YubiKey does not support largeBlobs");
                return;
            }

            byte[]? credentialId = null;

            try
            {
                using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.PinUtf8);

                var rp = FidoTestData.CreateRelyingParty();
                var user = FidoTestData.CreateUser();
                var makeChallenge = FidoTestData.GenerateChallenge();

                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                byte[] makePinToken;
                if (supportsPermissions)
                {
                    makePinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        FidoTestData.PinUtf8,
                        PinUvAuthTokenPermissions.MakeCredential,
                        FidoTestData.RpId);
                }
                else
                {
                    makePinToken = await clientPin.GetPinTokenAsync(FidoTestData.PinUtf8);
                }

                var makePinUvAuthParam = FidoTestHelpers.ComputeMakeCredentialAuthParam(
                    clientPin.Protocol, makePinToken, makeChallenge);

                // Create credential with largeBlobKey CTAP extension.
                // At CTAP level, "largeBlobKey: true" instructs the authenticator to
                // return a 32-byte large blob key alongside the credential response.
                // This is distinct from the WebAuthn "largeBlob" extension.
                var extensions = new ExtensionBuilder()
                    .WithLargeBlobKey()
                    .Build();

                var makeResult = await session.MakeCredentialAsync(
                    clientDataHash: makeChallenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: FidoTestData.ES256Params,
                    options: new MakeCredentialOptions
                    {
                        ResidentKey = true,
                        PinUvAuthParam = makePinUvAuthParam,
                        PinUvAuthProtocol = clientPin.Protocol.Version,
                        Extensions = extensions
                    });

                credentialId = makeResult.GetCredentialId().ToArray();
                Assert.NotNull(makeResult.LargeBlobKey);

                var largeBlobKey = makeResult.LargeBlobKey.Value;

                // Get a token with LargeBlobWrite permission
                byte[] writePinToken;
                if (supportsPermissions)
                {
                    writePinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        FidoTestData.PinUtf8,
                        PinUvAuthTokenPermissions.LargeBlobWrite);
                }
                else
                {
                    writePinToken = await clientPin.GetPinTokenAsync(FidoTestData.PinUtf8);
                }

                // Write data to the large blob
                var testData = "large-blob-test-data-certificate"u8.ToArray();
                var largeBlobStorage = new LargeBlobStorage(
                    session, clientPin.Protocol, writePinToken);

                await largeBlobStorage.SetBlobAsync(largeBlobKey, testData);

                // Read back the data
                var readStorage = new LargeBlobStorage(session);
                var retrieved = await readStorage.GetBlobAsync(largeBlobKey);

                Assert.NotNull(retrieved);
                Assert.Equal(testData, retrieved);

                CryptographicOperations.ZeroMemory(writePinToken);
            }
            finally
            {
                await FidoTestHelpers.DeleteAllCredentialsForRpAsync(session, FidoTestData.RpId, FidoTestData.PinUtf8);
            }
        });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("RequiresUserPresence", "true")]
    public async Task LargeBlob_DeleteBlob_RemovesData(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Options.TryGetValue("largeBlobs", out var largeBlobsSupported) || !largeBlobsSupported)
            {
                Skip.If(true, "YubiKey does not support largeBlobs");
                return;
            }

            byte[]? credentialId = null;

            try
            {
                using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.PinUtf8);

                var rp = FidoTestData.CreateRelyingParty();
                var user = FidoTestData.CreateUser();
                var makeChallenge = FidoTestData.GenerateChallenge();

                var supportsPermissions = info.Versions.Contains("FIDO_2_1") ||
                                           info.Versions.Contains("FIDO_2_1_PRE");

                byte[] makePinToken;
                if (supportsPermissions)
                {
                    makePinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        FidoTestData.PinUtf8,
                        PinUvAuthTokenPermissions.MakeCredential,
                        FidoTestData.RpId);
                }
                else
                {
                    makePinToken = await clientPin.GetPinTokenAsync(FidoTestData.PinUtf8);
                }

                var makePinUvAuthParam = FidoTestHelpers.ComputeMakeCredentialAuthParam(
                    clientPin.Protocol, makePinToken, makeChallenge);

                var extensions = new ExtensionBuilder()
                    .WithLargeBlobKey()
                    .Build();

                var makeResult = await session.MakeCredentialAsync(
                    clientDataHash: makeChallenge,
                    rp: rp,
                    user: user,
                    pubKeyCredParams: FidoTestData.ES256Params,
                    options: new MakeCredentialOptions
                    {
                        ResidentKey = true,
                        PinUvAuthParam = makePinUvAuthParam,
                        PinUvAuthProtocol = clientPin.Protocol.Version,
                        Extensions = extensions
                    });

                credentialId = makeResult.GetCredentialId().ToArray();
                Assert.NotNull(makeResult.LargeBlobKey);
                var largeBlobKey = makeResult.LargeBlobKey.Value;

                // Write, then delete, then verify gone
                byte[] writePinToken;
                if (supportsPermissions)
                {
                    writePinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        FidoTestData.PinUtf8,
                        PinUvAuthTokenPermissions.LargeBlobWrite);
                }
                else
                {
                    writePinToken = await clientPin.GetPinTokenAsync(FidoTestData.PinUtf8);
                }

                var largeBlobStorage = new LargeBlobStorage(
                    session, clientPin.Protocol, writePinToken);

                var testData = "data-to-delete"u8.ToArray();
                await largeBlobStorage.SetBlobAsync(largeBlobKey, testData);

                var deleted = await largeBlobStorage.DeleteBlobAsync(largeBlobKey);
                Assert.True(deleted, "DeleteBlobAsync should return true when a blob was deleted");

                var readStorage = new LargeBlobStorage(session);
                var retrieved = await readStorage.GetBlobAsync(largeBlobKey);
                Assert.Null(retrieved);

                CryptographicOperations.ZeroMemory(writePinToken);
            }
            finally
            {
                await FidoTestHelpers.DeleteAllCredentialsForRpAsync(session, FidoTestData.RpId, FidoTestData.PinUtf8);
            }
        });
}
