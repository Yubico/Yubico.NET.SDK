# Yubico.YubiKit.Oath

The OATH application stores TOTP (RFC 6238) and HOTP (RFC 4226) credentials on the YubiKey and calculates their codes
on the device. This package adds, renames, lists, deletes, and calculates those credentials and manages the optional
password protecting them. The programmable OTP slots belong to [Yubico.YubiKit.YubiOtp](../YubiOtp/README.md) instead.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Requirements

- .NET 10. On Linux, install PC/SC and the udev rules described in [docs/linux-setup.md](../../docs/linux-setup.md).
- YubiKey 5 or later, firmware 5.0 and up; no NEO or YubiKey 4 workarounds. Two operations need newer firmware:

  | Feature | Minimum firmware |
  |---------|------------------|
  | Rename credential | 5.3.1 |
  | SCP03 for OATH | 5.6.3 |

- Transport: SmartCard only, over USB CCID or NFC. Setting `PreferredConnectionType` to anything other than
  `ConnectionType.SmartCard` throws `ArgumentException`.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Oath --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Getting started

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Oath;

IReadOnlyList<IYubiKey> devices = await YubiKeyManager.FindAllAsync();
IYubiKey device = devices[0];
await using var session = await device.CreateOathSessionAsync();
IReadOnlyList<Credential> credentials = await session.ListCredentialsAsync();
Console.WriteLine(string.Join('\n', credentials.Select(c => $"{c.Issuer}:{c.Name} ({c.OathType})")));
```

Later snippets assume these directives and a `device` obtained the same way. The one-shot
`device.ListOathCredentialsAsync()` extension does the same listing and owns its own session.

## Common operations

### Add a TOTP credential from an otpauth URI

`CredentialData` owns the decoded secret, so dispose it. `requireTouch: true` makes every calculation wait for touch.

```csharp
using var credentialData = CredentialData.ParseUri(
    "otpauth://totp/GitHub:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=GitHub", requireTouch: false);
await using var session = await device.CreateOathSessionAsync();
await session.PutCredentialAsync(credentialData);
```

### Calculate codes

`CalculateAllAsync` returns a `null` code for HOTP and touch-requiring credentials; get those individually. A TOTP
`Code.ValidTo` is a Unix timestamp, but a HOTP code never expires and reports `long.MaxValue`, so do not format it.

```csharp
await using var session = await device.CreateOathSessionAsync();
foreach ((Credential credential, Code? code) in await session.CalculateAllAsync())
{
    Console.WriteLine($"{credential.Issuer}:{credential.Name} = {code?.Value ?? "(not calculated)"}");
}

Credential hotp = (await session.ListCredentialsAsync()).First(c => c.OathType == OathType.Hotp);
Code hotpCode = await session.CalculateCodeAsync(hotp);
Console.WriteLine($"HOTP code {hotpCode.Value}");
```

### Protect the application with a password

`DeriveKey` runs PBKDF2 over the device salt, so it needs a live session. Both buffers are yours to zero.

```csharp
byte[] password = Encoding.UTF8.GetBytes("correct horse battery staple");
byte[] accessKey = [];
try
{
    await using var session = await device.CreateOathSessionAsync();
    accessKey = session.DeriveKey(password);
    await (session.IsLocked
        ? session.ValidateAsync(accessKey)
        : session.SetKeyAsync(accessKey));
}
finally
{
    CryptographicOperations.ZeroMemory(password);
    CryptographicOperations.ZeroMemory(accessKey);
}
```

`UnsetKeyAsync` removes the password. `AuthenticateAndRetryAsync` retries a locked operation once after your callback supplies the password.

### Delete a credential

```csharp
await using var session = await device.CreateOathSessionAsync();
Credential credential = (await session.ListCredentialsAsync()).First(c => c.Name == "user@example.com");
await session.DeleteCredentialAsync(credential);
```

## User interaction

Touch is a per-credential policy fixed when the credential is stored (`CredentialData.RequireTouch`). There is no PIN
or management key; the optional password is handled by `ValidateAsync`, which never needs touch. `CalculateAsync` and
`CalculateCodeAsync` notify your `IUserPresencePrompt` implementation only when the
credential's `TouchRequired` metadata is explicitly `true`; `false` and unknown (`null`) stay silent.

```csharp
sealed class TouchPrompt : IUserPresencePrompt
{
    public ValueTask OnUserPresenceRequestedAsync(UserPresenceContext context, CancellationToken cancellationToken) =>
        new(Console.Out.WriteLineAsync($"Touch your YubiKey for {context.Application} {context.Scope}."));
}
```

```csharp
await using var session = await device.CreateOathSessionAsync(
    new SessionCreationOptions { UserPresencePrompt = new TouchPrompt() });
```

The context uses application `OATH`, basis `PolicyRequires`, and the public `issuer:name` identity.
`CalculateAllAsync` is silent; cancelling the token passed to a calculation resolves its notification as cancelled.

## Constraints

- SmartCard transport only. A physical YubiKey admits one live connection and one session per connection; a second
  attempt throws `ConnectionInUseException`. Whoever opens a connection disposes it.
- A session from `device.CreateOathSessionAsync(...)` owns the connection it opened; `OathSession.CreateAsync(connection, ...)` borrows yours. Use `await using` for both.
- `ResetAsync` erases every credential and the access key; `DeleteCredentialAsync`, `SetKeyAsync`, and `UnsetKeyAsync` are also irreversible.
- `RenameCredentialAsync` throws `NotSupportedException` through `EnsureSupports` below firmware 5.3.1, and so does
  passing `ScpKeyParameters` below 5.6.3. While the applet is locked, protected operations fail with an `OathException`
  whose `Reason` is `OathFailureReason.Locked` until `ValidateAsync` succeeds.

## Security notes

- Secrets cross the API as `byte[]` or `ReadOnlyMemory<byte>`. You own the input buffers: zero them with `CryptographicOperations.ZeroMemory` in a `finally`.
- `CredentialData` is `IDisposable` and zeroes its `Secret` on disposal. Always `using` it.
- `DeriveKey` takes a borrowed UTF-8 password and returns a 16-byte key you own and must zero; `AuthenticateAndRetryAsync` zeroes the key it derives, not the bytes your callback returned.
- Never log secrets, passwords, derived access keys, or calculated codes.

## Example

The interactive OathTool sample lives at src/Oath/examples/OathTool/.

```bash
dotnet run --project src/Oath/examples/OathTool/OathTool.csproj
```

## Related

- [../Core/README.md](../Core/README.md) - device discovery, connections, and session plumbing.
- [../YubiOtp/README.md](../YubiOtp/README.md) - programmable OTP slots and challenge-response.
- [../../docs/usage/user-interaction.md](../../docs/usage/user-interaction.md) - user-presence notifications.
- [../../docs/usage/device-discovery.md](../../docs/usage/device-discovery.md) - finding and watching YubiKeys.
- [YubiKey OATH protocol](https://developers.yubico.com/OATH/YKOATH_Protocol.html) - wire-level reference.
- [Developer guide](../../docs/DEV-GUIDE.md): building, testing, and contributing.
