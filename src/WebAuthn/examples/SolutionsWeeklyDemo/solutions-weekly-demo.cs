#!/usr/bin/env dotnet
// Solutions Weekly demo: passkey registration, sign-in and previewSign with the YubiKit .NET SDK v2.
// Run: dotnet run solutions-weekly-demo.cs
#:package Yubico.YubiKit.WebAuthn@2.0.0-alpha.115
#:package Yubico.YubiKit.Management@2.0.0-alpha.115
#:property NoWarn=$(NoWarn);YKEXP001

using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Arkg = Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.WebAuthn;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;
using Yubico.YubiKit.WebAuthn.Preferences;

// 1. Wait for a YubiKey to be plugged in.
Console.WriteLine("Plug in a YubiKey...");
var yubiKey = await WaitForYubiKeyAsync();

// 2. Ask the key what it is.
var info = await yubiKey.GetDeviceInfoAsync();
Console.WriteLine($"YubiKey {info.FormFactor}, firmware {info.FirmwareVersion}");

// 3. Open a WebAuthn client for our (pretend) website.
if (!WebAuthnOrigin.TryParse("https://example.com", out var origin))
{
    throw new InvalidOperationException("Invalid WebAuthn origin.");
}

var sessionOptions = new SessionCreationOptions { UserPresencePrompt = new ConsoleTouchPrompt() };
var clientOptions = new WebAuthnClientOptions
{
    // The app decides which websites may use which RP IDs. Demo: exactly one pair.
    RpIdValidator = (o, rpId) => o.StringValue == "https://example.com" && rpId == "example.com"
};

await using var client = await yubiKey.CreateWebAuthnClientAsync(origin, clientOptions, sessionOptions);

// 4. Register a passkey. Also ask the key for a separate signing key (previewSign, experimental).
// Challenges normally come from your server, fresh for every ceremony.
var registration = await client.MakeCredentialAsync(new PublicKeyCredentialCreationOptions
{
    Challenge = RandomNumberGenerator.GetBytes(32),
    Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
    User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "ada", "Ada"),
    PubKeyCredParams = [CoseAlgorithm.Es256],
    UserVerification = UserVerificationPreference.Discouraged,
    Extensions = new RegistrationExtensionInputs(
        PreviewSign: PreviewSignRegistrationInput.GenerateKey(CoseAlgorithm.ArkgP256))
});

Console.WriteLine($"Registered passkey {Convert.ToHexString(registration.CredentialId.Span)[..16]}...");

// The key silently skips extensions it doesn't support, so check before relying on it.
var signingKey = registration.ClientExtensionResults?.PreviewSign?.GeneratedKey;
Console.WriteLine(signingKey is null
    ? "This key does not support previewSign; skipping the document signature."
    : "Got a separate signing key bound to the passkey (previewSign).");

// 5. The server prepares a document for the key to sign alongside the sign-in.
var document = Encoding.UTF8.GetBytes("I approve transfer #4711");
var signRequest = signingKey is null ? null : PretendServer.PrepareSignRequest(signingKey, document);

// 6. Sign in. The SDK knows *what* the key is doing (a sign-in);
// only the app knows *why*, so the app attaches its intent around the call.
var signInChallenge = RandomNumberGenerator.GetBytes(32);
IReadOnlyList<MatchedCredential> matches;
using (UserPresenceIntent.BeginScope("unlock your notes"))
{
    matches = await client.GetAssertionAsync(new PublicKeyCredentialRequestOptions
    {
        Challenge = signInChallenge,
        RpId = "example.com",
        AllowCredentials = [new PublicKeyCredentialDescriptor(registration.CredentialId)],
        UserVerification = UserVerificationPreference.Discouraged,
        Extensions = signRequest is null
            ? null
            : new AuthenticationExtensionInputs(
                PreviewSign: PreviewSignAuthenticationInput.CreateSignByCredential(
                    new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>
                    {
                        [registration.CredentialId] = signRequest.SigningParams
                    }))
    });
}

var assertion = await matches[0].SelectAsync();

// 7. Your server does this part. We do it here so you can see it check out.
var signedIn = PretendServer.VerifyAssertion(registration.PublicKey, signInChallenge, assertion);
Console.WriteLine(signedIn ? "Unlocked. Sign-in signature verified." : "Sign-in signature did NOT verify.");

var documentSignature = assertion.ClientExtensionResults?.PreviewSign?.Signature;
if (signRequest is not null)
{
    Console.WriteLine(documentSignature is { } sig && signRequest.Verify(document, sig.Span)
        ? "Document signature verified."
        : "Document signature missing or did NOT verify.");
}

static async Task<Yubico.YubiKit.Core.Abstractions.IYubiKey> WaitForYubiKeyAsync()
{
    await foreach (var change in YubiKeyManager.WatchAsync())
    {
        if (change is DeviceAdded added)
        {
            Console.WriteLine("YubiKey detected.");
            return added.Device;
        }
    }

    throw new InvalidOperationException("Device monitoring stopped.");
}

/// <summary>
/// Tells the user to touch the key. One prompt, registered once per session, works for
/// PIV, FIDO2/WebAuthn, OATH, OpenPGP, YubiOTP and YubiHSM Auth.
/// </summary>
internal sealed class ConsoleTouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken)
    {
        // What the key is doing. The SDK tells us.
        var operation = context.Operation switch
        {
            UserPresenceOperations.Ctap2.MakeCredential => "passkey registration",
            UserPresenceOperations.Ctap2.GetAssertion => "sign-in",
            _ => "key operation"
        };

        // Why the user is doing it. Only the app knows; it comes from UserPresenceIntent.BeginScope.
        Console.WriteLine(context.Intent is { } intent
            ? $"  -> Touch your YubiKey to {intent} ({operation})"
            : $"  -> Touch your YubiKey to confirm the {operation}");

        return ValueTask.CompletedTask;
    }

    public ValueTask OnUserPresenceResolvedAsync(
        UserPresenceContext context,
        UserPresenceOutcome outcome,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

/// <summary>
/// A stand-in for the relying-party server, only so the demo can show the signatures checking out.
/// A real server also checks the origin, RP ID hash, flags and sign count.
/// Use a maintained server-side WebAuthn library for that; the SDK is client-side only.
/// </summary>
internal static class PretendServer
{
    public static bool VerifyAssertion(CoseKey publicKey, ReadOnlySpan<byte> expectedChallenge, AuthenticationResponse assertion)
    {
        // Is this a sign-in, for the challenge we just issued? (An old, replayed response fails here.)
        using var clientData = JsonDocument.Parse(assertion.ClientData.JsonBytes);
        var type = clientData.RootElement.GetProperty("type").GetString();
        var challenge = clientData.RootElement.GetProperty("challenge").GetString();
        if (type != "webauthn.get" || challenge != Base64Url.EncodeToString(expectedChallenge))
        {
            return false;
        }

        if (publicKey is not CoseEc2Key ec2)
        {
            return false;
        }

        using var ecdsa = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = ec2.X.ToArray(), Y = ec2.Y.ToArray() }
        });

        // WebAuthn signs authenticatorData || SHA-256(clientDataJSON).
        byte[] signedData = [.. assertion.RawAuthenticatorData.Span, .. assertion.ClientData.Hash.Span];

        return ecdsa.VerifyData(signedData, assertion.Signature.Span, HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);
    }

    /// <summary>
    /// ARKG: from the seed key the YubiKey returned at registration, the server derives a fresh public key
    /// and tells the key how to sign with the matching private key. EXPERIMENTAL; not production guidance.
    /// </summary>
    public static SignRequest PrepareSignRequest(GeneratedSigningKey signingKey, byte[] document)
    {
        var seed = (CoseArkgP256SeedKey)signingKey.PublicKey;
        var derived = ArkgBridge.ToCtap2(signingKey.KeyHandle, seed)
            .DerivePublicKey(RandomNumberGenerator.GetBytes(32), "solutions-weekly"u8);

        var signingParams = new PreviewSignSigningParams(
            signingKey.KeyHandle,
            SHA256.HashData(document),
            Arkg.PreviewSignCbor.EncodeAdditionalArgs(Arkg.CoseSignArgs.ArkgP256(derived.ArkgKeyHandle, derived.Context)));

        return new SignRequest(signingParams, derived);
    }

    internal sealed record SignRequest(PreviewSignSigningParams SigningParams, Arkg.PreviewSignDerivedKey DerivedKey)
    {
        public bool Verify(byte[] document, ReadOnlySpan<byte> signature) => DerivedKey.VerifySignature(document, signature);
    }
}

/// <summary>
/// Demo-only workaround: the WebAuthn layer exposes the ARKG seed key, but the type that derives public
/// keys from it has an internal constructor in this alpha. Mirrors the SDK's own integration test.
/// </summary>
internal static class ArkgBridge
{
    public static Arkg.PreviewSignGeneratedKey ToCtap2(ReadOnlyMemory<byte> keyHandle, CoseArkgP256SeedKey seed)
    {
        var ctor = typeof(Arkg.PreviewSignGeneratedKey).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            [typeof(ReadOnlyMemory<byte>), typeof(ReadOnlyMemory<byte>), typeof(ReadOnlyMemory<byte>), typeof(CoseAlgorithm)],
            null) ?? throw new InvalidOperationException("Arkg.PreviewSignGeneratedKey constructor not found.");

        return (Arkg.PreviewSignGeneratedKey)ctor.Invoke([keyHandle, seed.BlPublicKey, seed.KemPublicKey, seed.Algorithm]);
    }
}
