global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using System.IO;
global using System.Net.Http;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.WebAuthn;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Preferences;

IYubiKey device = await YubiKeyManager.FindFirstAsync();
if (!WebAuthnOrigin.TryParse("https://example.com", out var origin))
    throw new InvalidOperationException("The origin is not supported.");

// A complete, deliberately narrow allowlist: this origin may use exactly this known-valid RP ID.
var clientOptions = new WebAuthnClientOptions
{
    RpIdValidator = (requestOrigin, rpId) =>
        requestOrigin.StringValue == "https://example.com" && rpId == "example.com"
};
await using var client = await device.CreateWebAuthnClientAsync(origin, clientOptions);

// Local demo inputs only. Production options and user handles come from the RP.
var registrationOptions = new PublicKeyCredentialCreationOptions
{
    Challenge = RandomNumberGenerator.GetBytes(32),
    Rp = new PublicKeyCredentialRpEntity("example.com", "Example Corp"),
    User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "Example User"),
    PubKeyCredParams = [CoseAlgorithm.Es256],
    ResidentKey = ResidentKeyPreference.Required,
    UserVerification = UserVerificationPreference.Required,
    Extensions = new RegistrationExtensionInputs(CredProps: new CredPropsInput())
};
byte[] demoPin = Encoding.UTF8.GetBytes("11234567"); // Dedicated demo key's existing PIN; never embed a production PIN.
try
{
    var registration = await client.MakeCredentialAsync(registrationOptions, pinBytes: demoPin);

    // Keep the original result: do not register again just to obtain metadata.
    string[] transportNames = [.. registration.Transports.Select(transport => transport.Value)];
    bool? residentKey = registration.ClientExtensionResults?.CredProps?.ResidentKey;
    Console.WriteLine($"Created a credential; discoverable: {residentKey}.");

    var requestOptions = new PublicKeyCredentialRequestOptions
    {
        Challenge = RandomNumberGenerator.GetBytes(32), // A separate challenge for this demo assertion.
        RpId = "example.com",
        AllowCredentials = [new PublicKeyCredentialDescriptor(registration.CredentialId)],
        UserVerification = UserVerificationPreference.Required
    };
    var matches = await client.GetAssertionAsync(requestOptions, pinBytes: demoPin);
    if (matches.Count == 0)
    {
        Console.WriteLine("No matching credential was returned.");
        return;
    }

    // This single-entry allow-list demo needs no account picker.
    var assertion = await matches[0].SelectAsync();
    Console.WriteLine("Retrieved an assertion; RP verification is still required.");
}
finally
{
    CryptographicOperations.ZeroMemory(demoPin);
}
