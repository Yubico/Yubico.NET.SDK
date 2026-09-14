# Yubico.YubiKit.Fido2

The FIDO2 application on a YubiKey is a CTAP 2.1/2.3 authenticator. This package speaks CTAP directly: read
capabilities, create and assert credentials, manage the PIN, and drive the credential, biometric, large-blob,
and config sub-systems. `Yubico.YubiKit.WebAuthn` builds the WebAuthn client API on top of it.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Requirements

- .NET 10. On Linux, install PC/SC and the udev rules described in [linux-setup.md](../../docs/linux-setup.md).
- YubiKey 5 series, Security Key series, or YubiKey Bio series with firmware 5.0.0 or later.
- Transports: HID FIDO over USB, or SmartCard (CCID) where the FIDO2 AID is exposed - NFC always, USB on 5.8.0+.

| Feature | Minimum firmware |
|---------|------------------|
| FIDO2 (CTAP2) | 5.0.0 |
| Credential management, fingerprint bio enrollment | 5.2.0 |
| Authenticator config, hmac-secret-mc extension | 5.4.0 |
| credBlob extension | 5.5.0 |
| FIDO2 over USB SmartCard | 5.8.0 |

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Fido2 --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Getting started

```csharp
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.Fido2.Pin;

var devices = await YubiKeyManager.FindAllAsync();
IYubiKey device = devices[0];
await using var session = await device.CreateFidoSessionAsync();

var info = await session.GetInfoAsync();
Console.WriteLine($"CTAP versions: {string.Join(", ", info.Versions)}");
```

Later snippets assume these directives and a `device` obtained the same way. `GetInfoAsync` needs no touch and no PIN;
`info.Options`, `info.Extensions`, and `info.Aaguid` describe the rest. For a single read, the one-shot `device.GetFidoInfoAsync()` opens and closes a session for you.

## Common operations

### Create a credential

This asks for a touch and nothing else; `UserVerification` and credProtect need a PIN token, so they appear in the PIN flow below.

```csharp
byte[] clientDataHash = SHA256.HashData(Encoding.UTF8.GetBytes("""{"challenge":"replace-me"}"""));
var rp = new PublicKeyCredentialRpEntity("example.com", "Example Corp");
var user = new PublicKeyCredentialUserEntity(
    RandomNumberGenerator.GetBytes(32), "user@example.com", "Example User");

var response = await session.MakeCredentialAsync(
    clientDataHash, rp, user, [PublicKeyCredentialParameters.CreateES256()],
    new MakeCredentialOptions { ResidentKey = true });
ReadOnlyMemory<byte> credentialId = response.AuthenticatorData.AttestedCredentialData!.CredentialId;
```

### Get an assertion

```csharp
var assertion = await session.GetAssertionAsync("example.com", clientDataHash, new GetAssertionOptions());
Console.WriteLine($"Signature: {Convert.ToHexString(assertion.Signature.Span)}");
```

Leave `AllowList` unset to search discoverable credentials; if `NumberOfCredentials` exceeds one, call `GetNextAssertionAsync`.

### Verify the user with a PIN

A verified ceremony carries a PIN token: acquire one for the permission you need, authenticate the same
`clientDataHash` you send, and set both `PinUvAuthParam` and `PinUvAuthProtocol`. Setting `UserVerification` alone is rejected. `ClientPin` also has `SetPinAsync`, `ChangePinAsync`, and `GetPinRetriesAsync`.

```csharp
byte[] pin = Encoding.UTF8.GetBytes("123456");
byte[]? pinToken = null;
try
{
    using var protocol = new PinUvAuthProtocolV2();
    using var clientPin = new ClientPin(session, protocol);
    pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
        pin, PinUvAuthTokenPermissions.MakeCredential, "example.com");
    var options = new MakeCredentialOptions
    {
        ResidentKey = true,
        Extensions = new ExtensionBuilder().WithCredProtect(CredProtectPolicy.UserVerificationRequired).Build()
    };
    options.WithPinUvAuth(protocol.Authenticate(pinToken, clientDataHash), protocol.Version);
    options.WithUserVerification(true);
    await session.MakeCredentialAsync(
        clientDataHash, rp, user, [PublicKeyCredentialParameters.CreateES256()], options);
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
    CryptographicOperations.ZeroMemory(pinToken);
}
```

The assertion side is the same shape with a `GetAssertion` token:

```csharp
// Inside the same try block: a token is bound to its permission, so acquire a fresh one.
CryptographicOperations.ZeroMemory(pinToken);
pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
    pin, PinUvAuthTokenPermissions.GetAssertion, "example.com");
var assertionOptions = new GetAssertionOptions();
assertionOptions.WithPinUvAuth(protocol.Authenticate(pinToken, clientDataHash), protocol.Version);
assertionOptions.WithUserVerification(true);
var verified = await session.GetAssertionAsync("example.com", clientDataHash, assertionOptions);
```

`CredentialManagement`, `FingerprintBioEnrollment`, `LargeBlobStorage`, and `AuthenticatorConfig` are built the same way: session, protocol, and a token with the matching permission.

## User interaction

`MakeCredentialAsync`, `GetAssertionAsync`, `SelectionAsync`, and `ResetAsync` require a touch; `GetInfoAsync`
and the `ClientPin` commands are silent. Cancel a pending operation with its `cancellationToken`.

```csharp
sealed class TouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) =>
        new(Console.Out.WriteLineAsync($"Touch your YubiKey for {context.Application} {context.Scope}."));
}
```

```csharp
await using var promptingSession = await device.CreateFidoSessionAsync(
    new SessionCreationOptions { UserPresencePrompt = new TouchPrompt() });
```

The prompt shows the indication and returns without waiting. Over HID the request carries `UserPresenceBasis.DeviceWaiting` on the first keep-alive; SmartCard has no in-flight signal, so MakeCredential and GetAssertion report `PolicyRequires` before the command.

## Constraints

- The default transport is HID FIDO, falling back to SmartCard; `SessionCreationOptions.PreferredConnectionType`
  forces one. `ConnectionType.HidOtp` throws `ArgumentException`, an unexposed one `NotSupportedException`.
- SCP runs only over SmartCard. `ScpKeyParameters` without an explicit preference selects SmartCard; pairing
  them with `ConnectionType.HidFido` throws `NotSupportedException` during initialization.
- A physical YubiKey admits one live connection across all of its interfaces, and one session per connection;
  a second session throws `ConnectionInUseException` until the first is disposed.
- Sessions from `device.CreateFidoSessionAsync()` own the connection they opened; `FidoSession.CreateAsync`
  borrows yours and you dispose it. Use `await using` for both; there is no finalizer backstop.
- `ResetAsync` erases every credential and the PIN, irreversibly, and is refused unless it arrives within five
  seconds of insertion. Firmware gates throw `NotSupportedException`.

## Security notes

- PINs and PIN tokens are borrowed buffers; zero yours with `CryptographicOperations.ZeroMemory` in a `finally`.
- `ClientPin` zeroes what it derives internally and `PinUvAuthProtocolV2` zeroes the raw ECDH output.
- Never log PINs, PIN tokens, or secret-derived extension outputs such as hmac-secret and PRF results.

## Example

The interactive FidoTool sample lives at src/Fido2/examples/FidoTool/.

```bash
dotnet run --project src/Fido2/examples/FidoTool/FidoTool.csproj
```

## Related

- [../Core/README.md](../Core/README.md) - device discovery, connections, and session creation.
- [../WebAuthn/README.md](../WebAuthn/README.md) - the higher-level WebAuthn client built on this module.
- [user-interaction.md](../../docs/usage/user-interaction.md) and [device-discovery.md](../../docs/usage/device-discovery.md).
- The [CTAP 2.1](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html) and [WebAuthn Level 2](https://www.w3.org/TR/webauthn-2/) specifications.
- [Developer guide](../../docs/DEV-GUIDE.md): building, testing, and contributing.
