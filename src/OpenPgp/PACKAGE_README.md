# Yubico.YubiKit.OpenPgp

This package implements the OpenPGP card application on a YubiKey. It manages the
signature, decryption, and authentication key slots and their certificates, signs, decrypts, and
authenticates on the device, and administers the PINs, reset code, and touch policy. It is a smart-card
applet beside PIV; the two share no credentials.

> ## ALPHA - NOT FOR PRODUCTION
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Requirements

- .NET 10 on Windows, macOS, or Linux. Linux also needs PC/SC and udev rules.
- A YubiKey with the OpenPGP application enabled.
- SmartCard transport only (USB CCID or NFC).

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.OpenPgp --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Getting started

```csharp
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.OpenPgp;

var devices = await YubiKeyManager.FindAllAsync();
IYubiKey device = devices[0];
await using var session = await device.CreateOpenPgpSessionAsync();

ApplicationRelatedData appData = await session.GetApplicationRelatedDataAsync();
Console.WriteLine($"OpenPGP card {appData.Aid.Version.Major}.{appData.Aid.Version.Minor}, serial {appData.Aid.Serial}");
```

Reading application-related data needs no PIN and no touch.

## Documentation

- [OpenPGP module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/OpenPgp/README.md) covers
  PIN verification, key generation, signing, decryption, touch policy, and secret handling.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit) covers the other modules, building
  from source, and release notes.
