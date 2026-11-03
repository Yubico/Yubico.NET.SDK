// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Diagnostics;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Preferences;
using static Yubico.YubiKit.WebAuthn.IntegrationTests.WebAuthnTestHelpers;

namespace Yubico.YubiKit.WebAuthn.IntegrationTests.AuditV2;

public class WebAuthnAuditHardwareReproTests
{
    /// <summary>
    ///     Exercises the cooperative registration timer on HID hardware. DO NOT TOUCH the key: this test
    ///     expects cancellation while the authenticator waits for user presence, then checks session reuse.
    ///     HID acts on the signal at a keep-alive; CTAP2.3 §11.2.9.1.7 says keep-alives SHOULD be sent at
    ///     least every 100 ms, not that SDK return latency is guaranteed. The two-second slack below is a
    ///     test assumption. Success racing cancellation is possible in general, but is not the case tested here.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("Audit", "YESDK-1617")]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task YESDK1617_TimeoutWithoutTouchReturnsNotAllowedAndSessionStaysUsable(YubiKeyTestState state)
    {
        var deadline = TimeSpan.FromSeconds(5);
        var keepAliveAndCancelSlack = TimeSpan.FromSeconds(2);

        await using var session = await state.Device.CreateCtap2SessionAsync();
        await NormalizePinAsync(session);
        await using var client = CreateClient(session);

        var started = Stopwatch.StartNew();
        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.MakeCredentialAsync(new PublicKeyCredentialCreationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity(TestRpId, "Example"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "audit", "Audit"),
            PubKeyCredParams = [CoseAlgorithm.Es256],
            UserVerification = UserVerificationPreference.Preferred,
            Timeout = deadline
        }, KnownTestPin));
        started.Stop();

        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, error.Code);
        Assert.InRange(started.Elapsed, deadline, deadline + keepAliveAndCancelSlack);

        // The channel is clean: an exchange that needs no touch succeeds on the same session.
        var info = await session.GetInfoAsync();
        Assert.NotEmpty(info.Versions);
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("Audit", "YESDK-1615")]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task YESDK1615_PreferredResidentKeyCanBeDiscoveredWithoutAllowList(YubiKeyTestState state)
    {
        await using var registrationSession = await state.Device.CreateCtap2SessionAsync();
        await NormalizePinAsync(registrationSession);
        var info = await registrationSession.GetInfoAsync();
        Skip.If(!info.Options.TryGetValue("rk", out var capable) || !capable,
            "The authenticator does not support discoverable credentials.");

        ReadOnlyMemory<byte> credentialId;
        await using (var client = CreateClient(registrationSession))
        {
            var registration = await client.MakeCredentialAsync(new PublicKeyCredentialCreationOptions
            {
                Challenge = RandomNumberGenerator.GetBytes(32),
                Rp = new PublicKeyCredentialRpEntity(TestRpId, "Example"),
                User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "audit", "Audit"),
                PubKeyCredParams = [CoseAlgorithm.Es256],
                ResidentKey = ResidentKeyPreference.Preferred,
                // Exercise Preferred resident-key discovery; Required UV is covered separately below.
                UserVerification = UserVerificationPreference.Preferred
            }, KnownTestPin);
            credentialId = registration.CredentialId;
        }

        await using var assertionSession = await state.Device.CreateCtap2SessionAsync();
        await using var assertionClient = CreateClient(assertionSession);
        var matches = await assertionClient.GetAssertionAsync(new PublicKeyCredentialRequestOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            RpId = TestRpId,
            UserVerification = UserVerificationPreference.Preferred
            // Deliberately omit AllowCredentials: only discoverable credentials can be found.
        }, KnownTestPin);

        Assert.Contains(matches, match => match.Id.Span.SequenceEqual(credentialId.Span));
    }

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("Audit", "YESDK-1610")]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task YESDK1610_RequiredUvRegistrationWithPinSucceeds(YubiKeyTestState state)
    {
        // Required UV with a PIN uses a pinUvAuthToken and omits the "uv" option key, which
        // CTAP2.3 §6.1 forbids alongside pinUvAuthParam.
        await using var session = await state.Device.CreateCtap2SessionAsync();
        await NormalizePinAsync(session);
        await using var client = CreateClient(session);
        var registration = await client.MakeCredentialAsync(new PublicKeyCredentialCreationOptions
        {
            Challenge = RandomNumberGenerator.GetBytes(32),
            Rp = new PublicKeyCredentialRpEntity(TestRpId, "Example"),
            User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "audit-uv", "Audit UV"),
            PubKeyCredParams = [CoseAlgorithm.Es256],
            ResidentKey = ResidentKeyPreference.Discouraged,
            UserVerification = UserVerificationPreference.Required
        }, KnownTestPin);
        Assert.False(registration.CredentialId.IsEmpty);
    }
}