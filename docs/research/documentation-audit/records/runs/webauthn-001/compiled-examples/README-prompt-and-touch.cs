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
// Alternative configuration: insert before the demo's single CreateWebAuthnClientAsync call.
clientOptions = clientOptions with { CredentialPrompt = new PinPrompt(ReadPinAsync) };
// Alternative factory call: replace the demo's call; do not append another live client.
await using var client = await device.CreateWebAuthnClientAsync(
    origin, clientOptions, sessionOptions: new SessionCreationOptions { UserPresencePrompt = new TouchPrompt() });


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
    var registration = await client.MakeCredentialAsync(registrationOptions, pinBytes: null);

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
    var matches = await client.GetAssertionAsync(requestOptions, pinBytes: null);
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

static Task<byte[]> ReadPinAsync(CredentialPromptContext context, CancellationToken cancellationToken) => throw new NotImplementedException("Application-supplied input; compile only.");
internal sealed class PinPrompt(Func<CredentialPromptContext, CancellationToken, Task<byte[]>> readPin) : ICredentialPrompt
{
    public async ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(
        CredentialPromptContext context, CancellationToken cancellationToken)
    {
        byte[] pin = await readPin(context, cancellationToken);
        try { return DisposableArrayPoolBuffer.CreateFromSpan(pin); }   // exactly sized; ownership transfers
        finally { CryptographicOperations.ZeroMemory(pin); }
    }
}
sealed class TouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) =>
        new(Console.Out.WriteLineAsync($"Touch your YubiKey for {context.Application} {context.Scope}."));
}
