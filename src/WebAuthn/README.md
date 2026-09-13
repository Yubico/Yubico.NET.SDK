# Yubico.YubiKit.WebAuthn

This package is a W3C Web Authentication client over `Yubico.YubiKit.Fido2`. It reduces registration and
authentication to two calls, handling client data JSON, RP ID validation, and PIN/UV token acquisition on the way to
CTAP2; every ceremony runs over an `IFidoSession` that the client owns and disposes. It requires a WebAuthn origin and
a public-suffix checker backed by Public Suffix List data, because RP ID validation is what stops one site from
claiming another site's credentials.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Requirements

- .NET 10. On Linux, install PC/SC and the udev rules described in [linux-setup.md](../../docs/linux-setup.md).
- YubiKey 5 series, Security Key series, or YubiKey Bio series with firmware 5.0.0 or later. This module adds no
  firmware gates of its own; [../Fido2/README.md](../Fido2/README.md) lists the per-feature minimums.
- Transports: HID FIDO over USB by default, or SmartCard where the FIDO2 AID is exposed - NFC always, USB on 5.8.0+.
- An origin that is a secure context: `https`, or `http` on `localhost`.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.WebAuthn --prerelease
```

`Yubico.YubiKit.Core` is installed transitively, as is `Yubico.YubiKit.Fido2`.

## Getting started

```csharp
using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.WebAuthn;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Preferences;

var devices = await YubiKeyManager.FindAllAsync();
IYubiKey device = devices[0];
if (!WebAuthnOrigin.TryParse("https://example.com", out var origin))
    throw new InvalidOperationException("The origin is not a secure context.");

// Back this with Public Suffix List data in production.
PublicSuffixChecker isPublicSuffix = domain => domain is "com" or "net" or "org" or "co.uk";
await using var client = await device.CreateWebAuthnClientAsync(origin, isPublicSuffix);
```

Later snippets assume these directives and a `device` obtained the same way. The client exposes only the two ceremonies
and both need a touch, so there is no read-only WebAuthn call to try first; read authenticator capabilities with FIDO2 `GetInfoAsync` instead.

## Common operations

### Register a credential

```csharp
var registrationOptions = new RegistrationOptions
{
    Challenge = RandomNumberGenerator.GetBytes(32),
    Rp = new PublicKeyCredentialRpEntity("example.com", "Example Corp"),
    User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "Example User"),
    PubKeyCredParams = [CoseAlgorithm.Es256],
    ResidentKey = ResidentKeyPreference.Required,
    UserVerification = UserVerificationPreference.Required
};
byte[] pin = Encoding.UTF8.GetBytes("11234567");   // a PIN the application already holds; the buffer is borrowed
ReadOnlyMemory<byte> credentialId;
try
{
    var registration = await client.MakeCredentialAsync(registrationOptions, pin);
    credentialId = registration.CredentialId;
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
}
```

### Authenticate with an allow list

```csharp
var authenticationOptions = new AuthenticationOptions
{
    Challenge = RandomNumberGenerator.GetBytes(32),
    RpId = "example.com",
    AllowCredentials = [new PublicKeyCredentialDescriptor(credentialId)],
    UserVerification = UserVerificationPreference.Discouraged
};
var matches = await client.GetAssertionAsync(authenticationOptions, pinBytes: null);
var assertion = await matches[0].SelectAsync();
```

`GetAssertionAsync` returns every credential that matched without finishing the ceremony, so show a picker when more than one did; `SelectAsync` needs no further touch. An empty list means nothing matched, which is not an exception.

### Let the client ask for the PIN

Set `WebAuthnClientOptions.CredentialPrompt` and leave `pinBytes` null; the client then asks only when a ceremony needs a PIN, and it owns the retry loop. `ReadPinAsync` below is your own input routine, which `ConsoleCredentialReader` can back.

```csharp
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
```

```csharp
await using var promptingClient = await device.CreateWebAuthnClientAsync(
    origin, isPublicSuffix, new WebAuthnClientOptions { CredentialPrompt = new PinPrompt(ReadPinAsync) });
```

## User interaction

Both ceremonies require a touch, and CTAP gives a client no way to suppress it for registration. Touch notification comes from the FIDO2 session, so pass your `IUserPresencePrompt` in `sessionOptions`. It is informational; cancel the token you passed to the ceremony to give up on it.

```csharp
sealed class TouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) =>
        new(Console.Out.WriteLineAsync($"Touch your YubiKey for {context.Application} {context.Scope}."));
}
```

```csharp
await using var notifyingClient = await device.CreateWebAuthnClientAsync(
    origin, isPublicSuffix, sessionOptions: new SessionCreationOptions { UserPresencePrompt = new TouchPrompt() });
```

User verification is separate from touch, and `UserVerification` on either options record decides whether a ceremony obtains a PIN/UV token:

| Preference | Behavior |
|---|---|
| `Required` | Always obtains a token; throws `WebAuthnClientError` with code `NotAllowed` when the key has no verification configured and no PIN is available. |
| `Preferred` (default) | Obtains a token only when the key has verification configured, and otherwise proceeds unverified. |
| `Discouraged` | Obtains no token unless the key forces one, for example `alwaysUv` is enabled or a registration runs on a key that does not advertise `makeCredUvNotRqd`. |

## Constraints

- The client owns the `IFidoSession` in both paths: the one `CreateWebAuthnClientAsync` opens and one handed to the `WebAuthnClient` constructor. Use `await using` on the client and do not dispose that session yourself.
- A physical YubiKey admits one live connection across all of its interfaces, and one session per connection; a second session throws `ConnectionInUseException` until the first is disposed.
- Transport is fixed when the FIDO2 session is created and is never renegotiated after a failure. Override it with `SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard }`, which is also how to reach SCP.
- The RP ID must be a registrable suffix of the origin's effective domain, and public suffixes such as `com` and `co.uk` are rejected before any CTAP command. `WebAuthnClientOptions.EnterpriseRpIds` exempts only the IDs it names.
- The client asks for nothing but a PIN, supplied as `pinBytes` or by the prompt. A ceremony that needs one and has neither fails with `NotAllowed` rather than prompting.
- `ResidentKeyPreference.Required` consumes persistent credential slots. This package deletes nothing, and firmware gates live in the FIDO2 layer, which throws `NotSupportedException`. The experimental `previewSign` extension is reachable through `Extensions` on either options record and is not covered here.

## Security notes

- A PIN passed as `pinBytes` is borrowed; zero it with `CryptographicOperations.ZeroMemory` in a `finally`.
- A PIN returned from `ICredentialPrompt` transfers ownership to the SDK, which zeroes and disposes it, including a buffer returned late after cancellation. Return memory sized exactly to the secret, not a raw pooled buffer.
- The SDK owns the retry loop: it zeroes a rejected PIN, never resubmits it, and re-prompts with `IsRetry` set and `RetriesRemaining` refreshed, up to `MaxPromptAttempts` (default 3). Do not retry inside a prompt. Returning `null` means the user declined, and nothing else; report cancellation by throwing.
- Never log PINs. Credential IDs, RP IDs, and AAGUIDs are public identifiers.

## Related

- [../Core/README.md](../Core/README.md) - device discovery, connections, and the credential contracts.
- [../Fido2/README.md](../Fido2/README.md) - the CTAP layer this package builds on, and [FidoTool](../Fido2/examples/FidoTool/), the interactive sample; this package has none of its own.
- [user-interaction.md](../../docs/usage/user-interaction.md) and [device-discovery.md](../../docs/usage/device-discovery.md).
- The [WebAuthn Level 2](https://www.w3.org/TR/webauthn-2/) specification and the [Public Suffix List](https://publicsuffix.org/).
- [CLAUDE.md](CLAUDE.md) - contributor guidance, internals, and test infrastructure.
