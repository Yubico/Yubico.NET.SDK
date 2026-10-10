using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Preferences;
using static Yubico.YubiKit.WebAuthn.IntegrationTests.WebAuthnTestHelpers;

namespace Yubico.YubiKit.WebAuthn.IntegrationTests.AuditV2;

public class WebAuthnAuditHardwareTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("Audit", "YESDK-1609")]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task YESDK1609_PrfRoundTripProducesStable32ByteResult(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreateFidoSessionAsync();
        var info = await session.GetInfoAsync();
        Skip.IfNot(info.Extensions.Contains("hmac-secret"), "Authenticator lacks hmac-secret");
        await NormalizePinAsync(session);

        byte[] credentialId;
        await using (var client = CreateClient(session))
        {
            var registration = await client.MakeCredentialAsync(new RegistrationOptions
            {
                Challenge = RandomNumberGenerator.GetBytes(32),
                Rp = new PublicKeyCredentialRpEntity(TestRpId, "Audit PRF"),
                User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "audit-prf@example.com", "Audit PRF"),
                PubKeyCredParams = [CoseAlgorithm.Es256],
                ResidentKey = ResidentKeyPreference.Discouraged,
                UserVerification = UserVerificationPreference.Preferred, // Preferred isolates PRF from #2 (Required fails with 0x2C)
                Extensions = new RegistrationExtensionInputs(Prf: new PrfInput())
            }, KnownTestPin);
            credentialId = registration.CredentialId.ToArray();
            Assert.True(registration.ClientExtensionResults?.Prf?.Enabled);
        }

        await using var assertionSession = await state.Device.CreateFidoSessionAsync();
        await using var assertionClient = CreateClient(assertionSession);
        byte[]? previous = null;
        for (var i = 0; i < 2; i++)
        {
            var matches = await assertionClient.GetAssertionAsync(new AuthenticationOptions
            {
                Challenge = RandomNumberGenerator.GetBytes(32),
                RpId = TestRpId,
                AllowCredentials = [new PublicKeyCredentialDescriptor(credentialId)],
                UserVerification = UserVerificationPreference.Preferred, // Preferred isolates PRF from #2 (Required fails with 0x2C)
                Extensions = new AuthenticationExtensionInputs(Prf: new PrfInput { First = "same-prf-input"u8.ToArray() })
            }, KnownTestPin);
            Assert.NotEmpty(matches);
            var response = await matches[0].SelectAsync();
            var results = response.ClientExtensionResults?.Prf?.Results;
            Assert.NotNull(results);
            Assert.Equal(32, results.First.Length);
            if (previous is not null)
            {
                Assert.Equal(previous, results.First.ToArray());
            }
            previous = results.First.ToArray();
        }
    }
}
