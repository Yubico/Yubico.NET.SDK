# Yubico.YubiKit.Oath

The OATH application stores TOTP and HOTP credentials on a YubiKey and calculates their codes on the
device. This package adds, renames, lists, deletes, and calculates those credentials and manages the
optional password protecting them. The two programmable OTP slots are a different application: use
`Yubico.YubiKit.YubiOtp` for those.

> ## ALPHA - NOT FOR PRODUCTION
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Requirements

- .NET 10 on Windows, macOS, or Linux. Linux also needs PC/SC and udev rules.
- A YubiKey 5 series device or later with OATH enabled.
- SmartCard transport only (USB CCID or NFC).

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Oath --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Getting started

```csharp
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Oath;

var devices = await YubiKeyManager.FindAllAsync();
IYubiKey device = devices[0];
await using var session = await device.CreateOathSessionAsync();

foreach (var credential in await session.ListCredentialsAsync())
{
    Console.WriteLine($"{credential.Issuer}:{credential.Name} ({credential.OathType})");
}
```

Listing credentials needs no touch, and no password unless one has been set.

## Documentation

- [OATH module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/Oath/README.md) covers
  adding credentials from an otpauth URI, calculating codes, password protection, touch, and constraints.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit) covers the other modules, building
  from source, and release notes.
