# Yubico.YubiKit.YubiHsm

This package is for the **YubiHSM Auth application on a YubiKey**, not for a YubiHSM 2 device itself. The
applet stores the credentials used to authenticate to a YubiHSM 2 hardware security module, so the
long-lived HSM authentication keys live on a YubiKey instead of on the host. It manages those credentials
and derives the S-ENC, S-MAC, and S-RMAC session keys; your own code then uses those keys to talk to the
HSM through a connector. This package never contacts the HSM.

> ## ALPHA - NOT FOR PRODUCTION
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Requirements

- .NET 10 on Windows, macOS, or Linux. Linux also needs PC/SC and udev rules.
- A YubiKey with firmware 5.4.3 or later and the YubiHSM Auth application enabled.
- SmartCard transport only (USB CCID or NFC). There is no HID or OTP path.

Some operations need newer firmware: asymmetric credentials and challenge retrieval need 5.6.0, challenge
retrieval with a credential password needs 5.7.1, and credential password changes need 5.8.0.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.YubiHsm --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Getting started

```csharp
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.YubiHsm;

var devices = await YubiKeyManager.FindAllAsync();
IYubiKey device = devices[0];
await using var session = await device.CreateHsmAuthSessionAsync();

foreach (var credential in await session.ListCredentialsAsync())
{
    Console.WriteLine($"{credential.Label}: {credential.Algorithm}, {credential.RetriesRemaining} retries");
}
```

Listing credentials needs no password, management key, or touch.

## Documentation

- [YubiHSM Auth module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/YubiHsm/README.md) covers
  storing credentials, deriving session keys, the management key, error handling, and secret handling.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit) covers the other modules, building
  from source, and release notes.
