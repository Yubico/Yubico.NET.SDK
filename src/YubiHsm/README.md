# Yubico.YubiKit.YubiHsm

The YubiHSM Auth applet stores the credentials used to authenticate to a YubiHSM 2 hardware security module, so
the long-lived HSM authentication keys live on a YubiKey instead of on the host. This package manages those
credentials and derives the S-ENC, S-MAC, and S-RMAC keys for an HSM session; it does not speak to a connector.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Requirements

- .NET 10 on Windows, macOS, or Linux; Linux also needs PC/SC and udev rules ([Linux setup](../../docs/linux-setup.md)).
- SmartCard transport only, over USB CCID or NFC. There is no HID or OTP path.

| Feature | Minimum firmware |
| --- | --- |
| YubiHSM Auth applet | 5.4.3 |
| Asymmetric credentials, `GetChallengeAsync` | 5.6.0 |
| `GetChallengeAsync` with a credential password | 5.7.1 |
| Credential password change | 5.8.0 |

A credential is identified by a label of 1 to 64 UTF-8 bytes, which is not a secret and stays a plain `string`. A
symmetric credential holds an AES-128 K-ENC and K-MAC pair, an asymmetric one an EC P-256 private key, and each is
protected by a credential password of at most 16 UTF-8 bytes. The 16-byte management key authorizes add and delete.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.YubiHsm --prerelease
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
using Yubico.YubiKit.YubiHsm;

var devices = await YubiKeyManager.FindAllAsync();
IYubiKey device = devices[0];
await using var session = await device.CreateHsmAuthSessionAsync();
foreach (var credential in await session.ListCredentialsAsync())
{
    Console.WriteLine($"{credential.Label}: {credential.Algorithm}, {credential.RetriesRemaining} retries");
}
```

Listing credentials needs no password, management key, or touch. `device.ListHsmAuthCredentialsAsync()` does
the same in one shot. Later snippets assume these directives and a `device` obtained the same way.

## Common operations

### Store a symmetric credential

```csharp
await using var session = await device.CreateHsmAuthSessionAsync();
byte[] managementKey = new byte[16]; // The factory default is 16 zero bytes; rotate it before production use.
byte[] credentialPassword = Encoding.UTF8.GetBytes("hsm-password");
byte[] keyMaterial = RandomNumberGenerator.GetBytes(32); // K-ENC || K-MAC; escrow it before sending.
try
{
    await session.PutCredentialSymmetricAsync(managementKey, "hsm-prod", keyMaterial.AsMemory(0, 16),
        keyMaterial.AsMemory(16, 16), credentialPassword, touchRequired: true);
}
finally
{
    CryptographicOperations.ZeroMemory(keyMaterial);
    CryptographicOperations.ZeroMemory(credentialPassword);
}
```

`PutCredentialDerivedAsync` takes a derivation password instead of explicit K-ENC and K-MAC values, while
`GenerateCredentialAsymmetricAsync` stores an EC P-256 credential whose private key never leaves the device.

### Calculate session keys

The symmetric context is the 8-byte host challenge followed by the 8-byte HSM challenge. Send the host challenge
to the connector and pass back that exact value plus the HSM challenge and card cryptogram it returned.

```csharp
await using var session = await device.CreateHsmAuthSessionAsync();
ReadOnlyMemory<byte> hostChallenge = await session.GetChallengeAsync("hsm-prod");
// Your connector exchange: send hostChallenge, receive the HSM challenge and card cryptogram.
(ReadOnlyMemory<byte> hsmChallenge, ReadOnlyMemory<byte> cardCryptogram) = await ExchangeWithConnectorAsync(hostChallenge);
byte[] context = new byte[16];
byte[] credentialPassword = Encoding.UTF8.GetBytes("hsm-password");
try
{
    hostChallenge.CopyTo(context);
    hsmChallenge.CopyTo(context.AsMemory(8));
    // sessionKeys.SEnc, sessionKeys.SMac, and sessionKeys.SRmac drive the connector session.
    using var sessionKeys = await session.CalculateSessionKeysSymmetricAsync("hsm-prod", context, credentialPassword, cardCryptogram);
}
finally
{
    CryptographicOperations.ZeroMemory(context);
    CryptographicOperations.ZeroMemory(credentialPassword);
}
```

`ExchangeWithConnectorAsync` is your code against the YubiHSM connector; the SDK does not implement that handshake.
`GetChallengeAsync` needs firmware 5.6.0; on older keys use `RandomNumberGenerator.GetBytes(8)`. The asymmetric
counterpart takes a 130-byte context of EPK-OCE then EPK-SD, the device public key, and a required cryptogram.

### Rotate the management key

```csharp
await using var session = await device.CreateHsmAuthSessionAsync();
Console.WriteLine($"Management key retries: {await session.GetManagementKeyRetriesAsync()}");
byte[] currentManagementKey = new byte[16];
byte[] newManagementKey = RandomNumberGenerator.GetBytes(16);
try
{
    await session.PutManagementKeyAsync(currentManagementKey, newManagementKey);
}
finally
{
    CryptographicOperations.ZeroMemory(currentManagementKey);
    CryptographicOperations.ZeroMemory(newManagementKey);
}
```

## User interaction

A credential stored with `touchRequired: true` needs a physical touch during a session-key calculation; no
operation needs a PIN. Supply an `IUserPresencePrompt` to be notified around the blocking CALCULATE exchange.

```csharp
sealed class TouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) =>
        new(Console.Out.WriteLineAsync($"Touch your YubiKey for {context.Application} {context.Scope}."));
}
```

```csharp
var options = new SessionCreationOptions { UserPresencePrompt = new TouchPrompt() };
await using var session = await device.CreateHsmAuthSessionAsync(options);
```

`TouchRequired = true` maps to `UserPresenceBasis.PolicyRequires`; `false` and a missing credential are silent;
an unknown touch value or a failed LIST maps to `PolicyMayRequire`. Without a prompt the calculation does not
issue LIST just to notify. Pass a `CancellationToken` to abandon a wait.

## Constraints

- Requesting a non-SmartCard transport through `SessionCreationOptions { PreferredConnectionType = ... }` throws.
- One live connection per physical YubiKey, and one session per connection. Whoever creates a connection
  disposes it with `await using`; a session from `CreateHsmAuthSessionAsync` owns the one it opened.
- `ResetAsync` (no undo), `DeleteCredentialAsync`, and `PutManagementKeyAsync` change persistent applet state.
- Firmware gates throw `NotSupportedException`; below 5.7.1 `GetChallengeAsync` silently drops a supplied password.
- A wrong management key or credential password throws `HsmAuthRetryException` (an `ApduException`) carrying
  `RetriesRemaining`; a context of the wrong length throws `ArgumentException` before any device I/O.
- A credential starts at 8 attempts, and the device permanently deletes it once the counter reaches zero.

## Security notes

- Credential passwords, management keys, and EC private keys cross the API as `ReadOnlyMemory<byte>`, never
  `string`. The SDK zeroes its padded copies, never yours; use `CryptographicOperations.ZeroMemory` in a `finally`.
- `SessionKeys` is `IDisposable` and zeroes S-ENC, S-MAC, and S-RMAC on disposal. Always wrap it in `using`.
- `PutCredentialDerivedAsync` derives K-ENC and K-MAC with PBKDF2-HMAC-SHA256, salt `"Yubico"`, 10,000 iterations.
- Never log passwords, key material, challenges, or cryptograms. Log labels, algorithms, and status words only.

## Example

The interactive HsmAuthTool sample lives at `src/YubiHsm/examples/HsmAuthTool/`.

```bash
dotnet run --project src/YubiHsm/examples/HsmAuthTool/HsmAuthTool.csproj
```

## Related

- [Core](../Core/README.md) - device discovery, SmartCard protocol, and logging configuration.
- [Security Domain](../SecurityDomain/README.md) - SCP keys for running this session over a secure channel.
- [User interaction](../../docs/usage/user-interaction.md) and [device discovery](../../docs/usage/device-discovery.md) - cross-module guides.
- [YubiHSM Auth](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-yubihsm-auth.html) - the applet.
- [Developer guide](../../docs/DEV-GUIDE.md): building, testing, and contributing.
