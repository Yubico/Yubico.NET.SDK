using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
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

    private static RegistrationOptions PrfRegistration(string userName, PrfInput? prf = null) => new()
    {
        Challenge = RandomNumberGenerator.GetBytes(32),
        Rp = new PublicKeyCredentialRpEntity(TestRpId, "Audit PRF"),
        User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), userName, userName),
        PubKeyCredParams = [CoseAlgorithm.Es256],
        ResidentKey = ResidentKeyPreference.Discouraged,
        UserVerification = UserVerificationPreference.Preferred,
        Extensions = new RegistrationExtensionInputs(Prf: prf ?? new PrfInput())
    };

    /// <summary>
    ///     Extension-processing R4 on hardware: with a two-entry allow list the client probes (up:false), asserts with
    ///     the one credential the key holds, and evalByCredential keyed on it is what gets evaluated: the PRF result
    ///     equals the result of a plain eval with the same input for that credential.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("Audit", "YESDK-1634")]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task R4_AllowListSelection_PicksTheHeldCredentialAndItsPrfInputs(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreateFidoSessionAsync();
        var info = await session.GetInfoAsync();
        Skip.IfNot(info.Extensions.Contains("hmac-secret"), "Authenticator lacks hmac-secret");
        await NormalizePinAsync(session);

        byte[] held;
        await using (var client = CreateClient(session))
        {
            held = (await client.MakeCredentialAsync(PrfRegistration("r4-selection"), KnownTestPin)).CredentialId.ToArray();
        }

        var absent = new PublicKeyCredentialDescriptor(RandomNumberGenerator.GetBytes(32));
        byte[] evalInput = "selected-credential-input"u8.ToArray();
        static string B64(byte[] id) => Convert.ToBase64String(id).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        await using var assertionSession = await state.Device.CreateFidoSessionAsync();
        await using var assertionClient = CreateClient(assertionSession);
        var byCredential = await assertionClient.GetAssertionAsync(new AuthenticationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            RpId = TestRpId,
            AllowCredentials = [absent, new PublicKeyCredentialDescriptor(held)], // the held one is second on purpose
            UserVerification = UserVerificationPreference.Preferred,
            Extensions = new AuthenticationExtensionInputs(Prf: new PrfInput
            {
                EvalByCredential = new Dictionary<string, PrfInputValues>
                {
                    [B64(absent.Id.ToArray())] = new() { First = "wrong-credential-input"u8.ToArray() },
                    [B64(held)] = new() { First = evalInput }
                }
            })
        }, KnownTestPin);
        var selectedResponse = await Assert.Single(byCredential).SelectAsync();
        Assert.Equal(held, selectedResponse.CredentialId.ToArray());

        var plain = await assertionClient.GetAssertionAsync(new AuthenticationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            RpId = TestRpId,
            AllowCredentials = [new PublicKeyCredentialDescriptor(held)],
            UserVerification = UserVerificationPreference.Preferred,
            Extensions = new AuthenticationExtensionInputs(Prf: new PrfInput { First = evalInput })
        }, KnownTestPin);
        var plainResponse = await Assert.Single(plain).SelectAsync();

        Assert.Equal(plainResponse.ClientExtensionResults!.Prf!.Results.First.ToArray(),
            selectedResponse.ClientExtensionResults!.Prf!.Results.First.ToArray());
    }

    /// <summary>
    ///     Measured 2026-10-19 on YubiKey 5 NFC 5.8.0-beta (serial 103): the same PRF input evaluated with and
    ///     without user verification gives DIFFERENT outputs, i.e. the key keeps CTAP §12.7's CredRandomWithUV and
    ///     CredRandomWithoutUV apart. The client does not implement the WebAuthn L3 §10.1.4 UV override (maintainer
    ///     decision: parity with python-fido2 and yubikit-android), so relying parties must send
    ///     userVerification: required for stable PRF output on a PIN-protected key. This test keeps that fact in the suite.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("Audit", "YESDK-1609")]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task Prf_WithAndWithoutUserVerification_YieldsDifferentOutputs(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreateFidoSessionAsync();
        var info = await session.GetInfoAsync();
        Skip.IfNot(info.Extensions.Contains("hmac-secret"), "Authenticator lacks hmac-secret");
        await NormalizePinAsync(session);

        byte[] held;
        await using (var client = CreateClient(session))
        {
            held = (await client.MakeCredentialAsync(PrfRegistration("prf-uv-probe"), KnownTestPin)).CredentialId.ToArray();
        }

        byte[] input = "uv-comparison-input"u8.ToArray();
        await using var assertionSession = await state.Device.CreateFidoSessionAsync();
        await using var assertionClient = CreateClient(assertionSession);

        async Task<(byte[] Result, bool UvFlag)> AssertAsync(UserVerificationPreference uv, ReadOnlyMemory<byte>? pin)
        {
            var matches = await assertionClient.GetAssertionAsync(new AuthenticationOptions
            {
                Challenge = RandomNumberGenerator.GetBytes(32),
                RpId = TestRpId,
                AllowCredentials = [new PublicKeyCredentialDescriptor(held)],
                UserVerification = uv,
                Extensions = new AuthenticationExtensionInputs(Prf: new PrfInput { First = input })
            }, pin);
            var response = await Assert.Single(matches).SelectAsync();
            return (response.ClientExtensionResults!.Prf!.Results.First.ToArray(), response.AuthenticatorData.UserVerified);
        }

        var withUv = await AssertAsync(UserVerificationPreference.Preferred, KnownTestPin);
        var withoutUv = await AssertAsync(UserVerificationPreference.Discouraged, pin: null);

        Assert.True(withUv.UvFlag, "the verified assertion must carry uv=1");
        Assert.False(withoutUv.UvFlag, "the discouraged assertion without a PIN must carry uv=0");
        Assert.NotEqual(withUv.Result, withoutUv.Result); // CredRandomWithUV ≠ CredRandomWithoutUV on this key
    }
}