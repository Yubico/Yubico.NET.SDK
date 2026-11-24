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

// 1. Wait for a YubiKey and ask it what it is.
Console.WriteLine("Plug in a YubiKey...");
var yubiKey = (await YubiKeyManager.WatchAsync().OfType<DeviceAdded>().FirstAsync()).Device;

var info = await yubiKey.GetDeviceInfoAsync();
Console.WriteLine($"YubiKey {info.FormFactor}, firmware {info.FirmwareVersion}");

// 2. Open a WebAuthn client, with one touch prompt for every operation.
WebAuthnOrigin.TryParse("https://example.com", out var origin); // demo: known-good origin
var clientOptions = new WebAuthnClientOptions
{
    // The app decides which websites may use which RP IDs. Demo: exactly one pair.
    RpIdValidator = (o, rpId) => o.StringValue == "https://example.com" && rpId == "example.com"
};
var sessionOptions = new SessionCreationOptions { UserPresencePrompt = new ConsoleTouchPrompt() };

await using var client = await yubiKey.CreateWebAuthnClientAsync(origin!, clientOptions, sessionOptions);

// 3. Register a passkey, plus a separate signing key bound to it (previewSign, experimental).
var registration = await client.MakeCredentialAsync(new PublicKeyCredentialCreationOptions
{
    Challenge = RandomNumberGenerator.GetBytes(32), // normally from your server
    Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
    User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "ada", "Ada"),
    PubKeyCredParams = [CoseAlgorithm.Es256],
    UserVerification = UserVerificationPreference.Discouraged, // touch only, no PIN
    Extensions = new RegistrationExtensionInputs(
        PreviewSign: PreviewSignRegistrationInput.GenerateKey(CoseAlgorithm.ArkgP256))
});
Console.WriteLine($"Registered passkey {Convert.ToHexString(registration.CredentialId.Span)[..16]}...");

var signingKey = registration.ClientExtensionResults!.PreviewSign!.GeneratedKey;

// 4. Sign in, and sign a document in the same touch.
// The SDK knows *what* the key is doing; only the app knows *why*, so the app adds its intent.
var document = "I approve transfer #4711";
var signRequest = PretendServer.PrepareSignRequest(signingKey, document);
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
        Extensions = new AuthenticationExtensionInputs(
            PreviewSign: signRequest.ForCredential(registration.CredentialId))
    });
}
var assertion = await matches[0].SelectAsync();

// 5. Your server checks both signatures. We do it here so you can see them check out.
var signedIn = PretendServer.VerifySignIn(registration.PublicKey, signInChallenge, assertion);
var documentSigned = signRequest.Verify(assertion.ClientExtensionResults!.PreviewSign!.Signature);

Console.WriteLine($"Sign-in verified: {signedIn}");
Console.WriteLine($"Document \"{document}\" signature verified: {documentSigned}");

/// <summary>
/// Tells the user to touch the key. One prompt, registered once per session, works for
/// PIV, FIDO2/WebAuthn, OATH, OpenPGP, YubiOTP and YubiHSM Auth.
/// </summary>
internal sealed class ConsoleTouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken)
    {
        // Operation: what the key is doing (from the SDK). Intent: why (from the app's BeginScope).
        Console.WriteLine($"  -> Touch your YubiKey to {context.Intent ?? "continue"} ({context.Operation})");
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
    public static bool VerifySignIn(CoseKey publicKey, ReadOnlySpan<byte> expectedChallenge, AuthenticationResponse assertion)
    {
        // Is this a sign-in, for the challenge we just issued? (An old, replayed response fails here.)
        using var clientData = JsonDocument.Parse(assertion.ClientData.JsonBytes);
        var isFreshSignIn =
            clientData.RootElement.GetProperty("type").GetString() == "webauthn.get" &&
            clientData.RootElement.GetProperty("challenge").GetString() == Base64Url.EncodeToString(expectedChallenge);

        // WebAuthn signs authenticatorData || SHA-256(clientDataJSON) with the passkey's P-256 key.
        var key = (CoseEc2Key)publicKey;
        using var ecdsa = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = key.X.ToArray(), Y = key.Y.ToArray() }
        });
        byte[] signedData = [.. assertion.RawAuthenticatorData.Span, .. assertion.ClientData.Hash.Span];

        return isFreshSignIn && ecdsa.VerifyData(signedData, assertion.Signature.Span,
            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
    }

    /// <summary>
    /// ARKG: from the seed key the YubiKey returned at registration, the server derives a fresh public key
    /// and tells the key how to sign with the matching private key. EXPERIMENTAL; not production guidance.
    /// </summary>
    public static SignRequest PrepareSignRequest(GeneratedSigningKey signingKey, string document)
    {
        var seed = (CoseArkgP256SeedKey)signingKey.PublicKey;
        var derived = ArkgBridge.ToCtap2(signingKey.KeyHandle, seed)
            .DerivePublicKey(RandomNumberGenerator.GetBytes(32), "solutions-weekly"u8);

        var signingParams = new PreviewSignSigningParams(
            signingKey.KeyHandle,
            SHA256.HashData(Encoding.UTF8.GetBytes(document)),
            Arkg.PreviewSignCbor.EncodeAdditionalArgs(Arkg.CoseSignArgs.ArkgP256(derived.ArkgKeyHandle, derived.Context)));

        return new SignRequest(document, signingParams, derived);
    }

    internal sealed record SignRequest(string Document, PreviewSignSigningParams SigningParams, Arkg.PreviewSignDerivedKey DerivedKey)
    {
        public PreviewSignAuthenticationInput ForCredential(ReadOnlyMemory<byte> credentialId) =>
            PreviewSignAuthenticationInput.CreateSignByCredential(
                new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams> { [credentialId] = SigningParams });

        public bool Verify(ReadOnlyMemory<byte> signature) =>
            DerivedKey.VerifySignature(Encoding.UTF8.GetBytes(Document), signature.Span);
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
            null)!;

        return (Arkg.PreviewSignGeneratedKey)ctor.Invoke([keyHandle, seed.BlPublicKey, seed.KemPublicKey, seed.Algorithm]);
    }
}
