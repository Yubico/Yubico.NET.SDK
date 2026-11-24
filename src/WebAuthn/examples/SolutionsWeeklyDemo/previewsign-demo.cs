#!/usr/bin/env dotnet
// Solutions Weekly demo: previewSign (EXPERIMENTAL) with the YubiKit .NET SDK v2.
// The YubiKey signs raw bytes with a key bound to a passkey, so ordinary ECDSA verifiers can check it.
// Run: dotnet run previewsign-demo.cs
#:project DemoSupport/DemoSupport.csproj
#:property NoWarn=$(NoWarn);YKEXP001

using System.Security.Cryptography;
using System.Text;
using DemoSupport;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

var yubiKey = await Demo.WaitForYubiKeyAsync();
await using var client = await Demo.OpenClientAsync(yubiKey);

// 1. Register a passkey and ask for a separate signing key bound to it.
// Nothing is stored on the key: it re-derives the signing key on each use, and keeps no export path.
var registration = await client.MakeCredentialAsync(Demo.Registration(
    new RegistrationExtensionInputs(
        PreviewSign: PreviewSignRegistrationInput.GenerateKey(CoseAlgorithm.ArkgP256))));

var signingKey = registration.ClientExtensionResults!.PreviewSign!.GeneratedKey;
Console.WriteLine("Got a signing key bound to the passkey.");

// 2. Without the YubiKey: derive a fresh public key (ARKG). Repeat for as many unlinkable keys as you need.
var publicKey = Arkg.DerivePublicKey(signingKey, context: "solutions-weekly");
Console.WriteLine("Derived a fresh public key, no YubiKey involved.");

// 3. Ask the YubiKey to sign raw bytes from any protocol. The key never shows the data to the user.
var payload = Encoding.UTF8.GetBytes("""{"iss":"wallet","nonce":"8f2c"}""");
var signRequest = new PreviewSignSigningParams(
    publicKey.KeyHandle,
    SHA256.HashData(payload), // this algorithm signs a digest
    publicKey.SignArgs);

// The sign-in signature itself isn't checked here: the extension works within an existing session.
var matches = await client.GetAssertionAsync(Demo.SignIn(
    registration.CredentialId,
    new AuthenticationExtensionInputs(
        PreviewSign: PreviewSignAuthenticationInput.CreateSignByCredential(
            new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>
            {
                [registration.CredentialId] = signRequest
            }))));

var signature = (await matches[0].SelectAsync()).ClientExtensionResults!.PreviewSign!.Signature;

// 4. Verify with plain ECDSA. No WebAuthn data structures involved.
var valid = publicKey.PublicKey.VerifyData(
    payload, signature.Span, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
Console.WriteLine($"Payload signature verified with plain ECDSA: {valid}");
