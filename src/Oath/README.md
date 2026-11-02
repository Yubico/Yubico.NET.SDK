# Yubico.YubiKit.Oath

The OATH application stores TOTP (RFC 6238) and HOTP (RFC 4226) credentials on the YubiKey and calculates their codes
on the device. This package adds, renames, lists, deletes, and calculates those credentials and manages the optional
password protecting them. The programmable OTP slots belong to [Yubico.YubiKit.YubiOtp](../YubiOtp/README.md) instead.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.

## Requirements

- .NET 10. On Linux, a working PC/SC service and reader support must make the YubiKey available over
  USB CCID or an NFC reader. The [Linux HID setup guide](../../docs/linux-setup.md) does not cover PC/SC setup.
- A YubiKey 4 or 5 series device with OATH enabled. Feature requirements and SDK gates are:

  | Feature | Firmware condition |
  |---------|--------------------|
  | Touch-required credentials | Device requirement: 4.2.4+; not preflight-checked by the SDK |
  | SHA-512 credentials | Device requirement: 4.3.1+; not preflight-checked by the SDK |
  | Rename credential | SDK gate: effective version 5.3.1+ |
  | Secure-channel session | SDK gate: effective version 5.6.3+ for any SCP parameters; SCP11 also requires 5.7.2+ in Core |

  OATH has no general session-open firmware floor. SDK gates use the detected applet version unless
  `SessionCreationOptions.FirmwareVersionOverride` supplies the effective version. The 5.6.3 SCP gate
  describes current SDK behavior, not a confirmed earliest hardware version for OATH over SCP.

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

IYubiKey device = await YubiKeyManager.FindFirstAsync();
await using var session = await device.CreateOathSessionAsync();
IReadOnlyList<Credential> credentials = await session.ListCredentialsAsync();
Console.WriteLine(string.Join('\n', credentials.Select(c => $"{c.Issuer}:{c.Name} ({c.OathType})")));
```

Later snippets assume these directives and a `device` obtained the same way. The one-shot
`device.ListOathCredentialsAsync()` extension does the same listing and owns its own session.

## Common operations

### Add a TOTP credential from an otpauth URI

`ParseUri` creates a new decoded secret array; dispose the returned `CredentialData` to clear that array.
`PutCredentialAsync` adds the credential or replaces one with the same raw credential ID, produced by `GetId()`.
Set `requireTouch: true` to require touch for individual calculations (firmware 4.2.4+). Bulk calculations return
`null` for these entries, as described below.

```csharp
using var credentialData = CredentialData.ParseUri(
    "otpauth://totp/GitHub:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=GitHub", requireTouch: false);
await using var session = await device.CreateOathSessionAsync();
await session.PutCredentialAsync(credentialData);
```

### Calculate codes

`CalculateAllAsync` returns a `null` code for HOTP and touch-required credentials; get those individually with
`CalculateCodeAsync`. Both methods accept an optional `timestamp` in Unix seconds, defaulting to the current UTC
time. It determines the TOTP challenge and nominal generation window. For an individual HOTP calculation, the
device uses its stored counter; the timestamp only sets `Code.ValidFrom`.

For TOTP, `Code.ValidFrom` and `Code.ValidTo` are the inclusive start and exclusive end of the nominal generation
window, in Unix seconds. For HOTP, `ValidTo` is `long.MaxValue`, a no-time-expiry sentinel, not a date to format.
These values do not guarantee verifier acceptance: the verifier applies its own time-window, replay, or counter rules.

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

Use `DeriveKey` on an open OATH session to derive an access key from a UTF-8-encoded password. The method uses
the device’s salt.

You own both the password buffer and the returned key. Clear each with `CryptographicOperations.ZeroMemory`
when it is no longer needed, including when an operation fails. The example below uses a `finally` block to clear
both buffers.

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
`CalculateAllAsync` is silent. Cancelling the token before the calculation's SmartCard exchange is admitted
resolves a requested notification as cancelled; once admitted, the exchange runs to completion and cancellation
does not interrupt the device or necessarily change the notification outcome.

## Constraints

- SmartCard transport only. A physical YubiKey admits one live connection and one session per connection; a second
  attempt throws `ConnectionInUseException`. Whoever opens a connection disposes it.
- A session from `device.CreateOathSessionAsync(...)` owns the connection it opened; `OathSession.CreateAsync(connection, ...)` borrows yours. Use `await using` for both.
- `ResetAsync` erases every credential and the access key. `DeleteCredentialAsync` erases the selected credential.
  `SetKeyAsync` sets or changes password protection; `UnsetKeyAsync` removes it. Neither changes stored credentials.
- `RenameCredentialAsync` throws `NotSupportedException` through `EnsureSupports` below effective version 5.3.1,
  as does passing any non-null `ScpKeyParameters` below 5.6.3. Core additionally requires 5.7.2+ for SCP11.
  While the applet is locked, protected operations fail with an `OathException`
  whose `Reason` is `OathFailureReason.Locked` until `ValidateAsync` succeeds.

## Security notes

- Secrets cross the API as `byte[]` or `ReadOnlyMemory<byte>`. You own the input buffers: zero them with `CryptographicOperations.ZeroMemory` in a `finally`.
- `CredentialData` properties are assigned only during initialization. Directly assigning `Secret` retains the
  supplied array without copying it; the property exposes the same mutable array. `ParseUri` creates a new decoded
  array instead. Use `using` to clear `Secret` on disposal; this does not clear the URI or encoded secret strings.
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
