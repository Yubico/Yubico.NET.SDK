#!/usr/bin/env dotnet

#:project src/WebAuthn/src/Yubico.YubiKit.WebAuthn.csproj

using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.WebAuthn;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;
using Yubico.YubiKit.WebAuthn.Preferences;
using Fido2Extensions = Yubico.YubiKit.Fido2.Extensions;

// Experimental previewSign and ARKG demonstration. Do not use as production cryptographic guidance.
const string relyingPartyId = "example.com";
_ = WebAuthnOrigin.TryParse("https://example.com", out WebAuthnOrigin? origin);

IYubiKey yubiKey = (await YubiKeyManager.FindAllAsync(ConnectionType.HidFido))
    .FirstOrDefault()
    ?? throw new InvalidOperationException("Connect a previewSign-capable YubiKey over USB.");

await using WebAuthnClient client = await yubiKey.CreateWebAuthnClientAsync(
    origin ?? throw new InvalidOperationException("The demo origin is invalid."),
    isPublicSuffix: domain => domain is "com" or "org" or "net");

Console.WriteLine("Touch the YubiKey to create a credential and an ARKG signing key.");

RegistrationResponse registration = await client.MakeCredentialAsync(new RegistrationOptions
{
    Challenge = RandomNumberGenerator.GetBytes(32),
    Rp = new PublicKeyCredentialRpEntity(relyingPartyId, "previewSign demo"),
    User = new PublicKeyCredentialUserEntity(
        RandomNumberGenerator.GetBytes(16),
        "demo@example.com",
        "Demo User"),
    PubKeyCredParams = [CoseAlgorithm.Es256],
    UserVerification = UserVerificationPreference.Discouraged,
    Extensions = new RegistrationExtensionInputs(
        PreviewSign: PreviewSignRegistrationInput.GenerateKey(CoseAlgorithm.ArkgP256)),
});

GeneratedSigningKey generatedKey = registration.ClientExtensionResults?.PreviewSign?.GeneratedKey
    ?? throw new InvalidOperationException("The YubiKey did not return a previewSign key.");
var seedKey = generatedKey.PublicKey as CoseArkgP256SeedKey
    ?? throw new InvalidOperationException("The generated key is not an ARKG-P256 seed key.");

byte[] inputKeyMaterial = RandomNumberGenerator.GetBytes(32);
byte[] context = Encoding.UTF8.GetBytes("webauthn-demo");
Fido2Extensions.PreviewSignDerivedKey derivedKey;
try
{
    derivedKey = Fido2Extensions.PreviewSignGeneratedKey
        .FromArkgSeedKey(generatedKey.KeyHandle, seedKey)
        .DerivePublicKey(inputKeyMaterial, context);
}
finally
{
    CryptographicOperations.ZeroMemory(inputKeyMaterial);
}

byte[] message = Encoding.UTF8.GetBytes("Hello from YubiKit .NET v2!");
byte[] toBeSigned = SHA256.HashData(message);
byte[] additionalArgs = Fido2Extensions.PreviewSignCbor.EncodeAdditionalArgs(
    Fido2Extensions.CoseSignArgs.ArkgP256(derivedKey.ArkgKeyHandle, derivedKey.Context));

var signByCredential = new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>
{
    [registration.CredentialId] = new PreviewSignSigningParams(
        generatedKey.KeyHandle,
        toBeSigned,
        additionalArgs),
};

Console.WriteLine("Touch the YubiKey again to sign the message with previewSign.");

IReadOnlyList<MatchedCredential> matches = await client.GetAssertionAsync(new AuthenticationOptions
{
    Challenge = RandomNumberGenerator.GetBytes(32),
    RpId = relyingPartyId,
    AllowCredentials = [new PublicKeyCredentialDescriptor(registration.CredentialId)],
    UserVerification = UserVerificationPreference.Discouraged,
    Extensions = new AuthenticationExtensionInputs(
        PreviewSign: new PreviewSignAuthenticationInput(signByCredential)),
});

AuthenticationResponse assertion = await matches.Single().SelectAsync();
ReadOnlyMemory<byte> signature = assertion.ClientExtensionResults?.PreviewSign?.Signature
    ?? throw new InvalidOperationException("The YubiKey did not return a previewSign signature.");

Console.WriteLine($"Message:   {Encoding.UTF8.GetString(message)}");
Console.WriteLine($"Signature: {Convert.ToHexString(signature.Span)}");
