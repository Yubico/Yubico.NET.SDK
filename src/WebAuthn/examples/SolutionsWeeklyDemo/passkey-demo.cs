#!/usr/bin/env dotnet
// Solutions Weekly demo: passkey registration and sign-in with the YubiKit .NET SDK v2.
// Run: dotnet run passkey-demo.cs
#:project DemoSupport/DemoSupport.csproj

using System.Security.Cryptography;
using DemoSupport;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;

// 1. Wait for a YubiKey and ask it what it is.
var yubiKey = await Demo.WaitForYubiKeyAsync();

var info = await yubiKey.GetDeviceInfoAsync();
Console.WriteLine($"YubiKey {info.FormFactor}, firmware {info.FirmwareVersion}");

// 2. Open a WebAuthn client for example.com, with one touch prompt for every operation.
await using var client = await Demo.OpenClientAsync(yubiKey);

// 3. Register a passkey.
RegistrationResponse registration;
using (UserPresenceIntent.BeginScope("create your passkey"))
{
    // Demo.Registration: standard WebAuthn creation options for example.com (RP, user, ES256, touch only).
    registration = await client.MakeCredentialAsync(Demo.Registration());
}
Console.WriteLine($"Registered passkey {Convert.ToHexString(registration.CredentialId.Span)[..16]}...");

// 4. Sign in. The SDK knows *what* the key is doing; only the app knows *why*, so the app adds its intent.
var challenge = RandomNumberGenerator.GetBytes(32); // normally from your server, fresh every time

IReadOnlyList<MatchedCredential> matches;
using (UserPresenceIntent.BeginScope("unlock your notes"))
{
    // Demo.SignIn: standard WebAuthn request options for example.com, allowing only this passkey.
    matches = await client.GetAssertionAsync(Demo.SignIn(registration.CredentialId, challenge: challenge));
}
var assertion = await matches[0].SelectAsync();

// 5. Your server checks the signature. We do it here so you can see it check out.
var signedIn = PretendServer.VerifySignIn(registration.PublicKey, challenge, assertion);
Console.WriteLine($"Sign-in verified: {signedIn}");
