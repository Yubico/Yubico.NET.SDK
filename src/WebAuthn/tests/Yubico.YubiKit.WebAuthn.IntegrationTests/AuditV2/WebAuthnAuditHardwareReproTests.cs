// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Preferences;
using static Yubico.YubiKit.WebAuthn.IntegrationTests.WebAuthnTestHelpers;

namespace Yubico.YubiKit.WebAuthn.IntegrationTests.AuditV2;

public class WebAuthnAuditHardwareReproTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("Audit", "YESDK-1615")]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task YESDK1615_PreferredResidentKeyCanBeDiscoveredWithoutAllowList(YubiKeyTestState state)
    {
        await using var registrationSession = await state.Device.CreateFidoSessionAsync();
        await NormalizePinAsync(registrationSession);
        var info = await registrationSession.GetInfoAsync();
        Skip.If(!info.Options.TryGetValue("rk", out var capable) || !capable,
            "The authenticator does not support discoverable credentials.");

        ReadOnlyMemory<byte> credentialId;
        await using (var client = CreateClient(registrationSession))
        {
            var registration = await client.MakeCredentialAsync(new RegistrationOptions
            {
                Challenge = RandomNumberGenerator.GetBytes(32),
                Rp = new PublicKeyCredentialRpEntity(TestRpId, "Example"),
                User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "audit", "Audit"),
                PubKeyCredParams = [CoseAlgorithm.Es256],
                ResidentKey = ResidentKeyPreference.Preferred,
                // Preferred (not Required) isolates #7 from #2: UV=Required currently fails on a
                // PIN-only YubiKey with CTAP2_ERR_INVALID_OPTION (see YESDK1610 test below).
                UserVerification = UserVerificationPreference.Preferred
            }, KnownTestPin);
            credentialId = registration.CredentialId;
        }

        await using var assertionSession = await state.Device.CreateFidoSessionAsync();
        await using var assertionClient = CreateClient(assertionSession);
        var matches = await assertionClient.GetAssertionAsync(new AuthenticationOptions
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
        // Spec-correct behaviour: UV=Required with a PIN obtains a pinUvAuthToken and omits the
        // "uv" option key (CTAP 2.3 §6.1). Today the SDK sends both and the device rejects it.
        await using var session = await state.Device.CreateFidoSessionAsync();
        await NormalizePinAsync(session);
        await using var client = CreateClient(session);
        var registration = await client.MakeCredentialAsync(new RegistrationOptions
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
