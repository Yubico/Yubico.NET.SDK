// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Preferences;

using static Yubico.YubiKit.WebAuthn.IntegrationTests.WebAuthnTestHelpers;

namespace Yubico.YubiKit.WebAuthn.IntegrationTests;

[Trait("Category", "Integration")]
public class WebAuthnClientFactoryTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task CreateWebAuthnClientAsync_WithSmartCard_CreatesDisposableClient(YubiKeyTestState state)
    {
        var origin = ParseOrigin(TestOriginUrl);

        try
        {
            await using var client = await state.Device.CreateWebAuthnClientAsync(
                origin,
                isPublicSuffix: domain => domain is "com" or "org" or "net" or "co.uk",
                preferredConnection: ConnectionType.SmartCard);

            Assert.NotNull(client);
        }
        catch (NotSupportedException)
        {
            Skip.If(true,
                "FIDO2 SmartCard session failed because the connected authenticator did not expose the FIDO2 AID or does not support USB SmartCard FIDO2 on this firmware.");
        }
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task CreateWebAuthnClientAsync_WithSmartCard_MakeCredentialReturnsResponse(YubiKeyTestState state)
    {
        FidoSession setupSession;
        try
        {
            setupSession = await state.Device.CreateFidoSessionAsync(
                preferredConnection: ConnectionType.SmartCard);
        }
        catch (NotSupportedException)
        {
            Skip.If(true,
                "FIDO2 SmartCard session failed because the connected authenticator did not expose the FIDO2 AID or does not support USB SmartCard FIDO2 on this firmware.");
            return;
        }

        await using (setupSession)
        {
            await NormalizePinAsync(setupSession);
        }

        var origin = ParseOrigin(TestOriginUrl);
        await using var client = await state.Device.CreateWebAuthnClientAsync(
            origin,
            isPublicSuffix: domain => domain is "com" or "org" or "net" or "co.uk",
            preferredConnection: ConnectionType.SmartCard);

        var options = new RegistrationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity(TestRpId, "Example Corp"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "testuser@example.com", "Test User"),
            PubKeyCredParams = [CoseAlgorithm.Es256],
            ResidentKey = ResidentKeyPreference.Discouraged,
            UserVerification = UserVerificationPreference.Discouraged
        };

        var response = await client.MakeCredentialAsync(
            options,
            pin: "11234567",
            useUv: false,
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.True(response.CredentialId.Length > 0, "Credential ID should not be empty");
        Assert.NotNull(response.PublicKey);
        Assert.NotNull(response.AttestationObject);
    }

    private static WebAuthnOrigin ParseOrigin(string url)
    {
        Assert.True(WebAuthnOrigin.TryParse(url, out var origin));
        return origin;
    }
}