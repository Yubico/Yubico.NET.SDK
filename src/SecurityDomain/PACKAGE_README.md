# Yubico.YubiKit.SecurityDomain

The Security Domain is the root security application on a YubiKey. It owns the Secure Channel Protocol
(SCP) keys that other applet sessions use to encrypt and authenticate their traffic. This package inspects,
generates, imports, rotates, and deletes those keys, manages SCP11 certificates and allow lists, and
factory-resets the application.

> ## ALPHA - NOT FOR PRODUCTION
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Requirements

- .NET 10 on Windows, macOS, or Linux. Linux also needs PC/SC and udev rules.
- A YubiKey with firmware 5.3.0 or later.
- SmartCard transport only (USB CCID or NFC).

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.SecurityDomain --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Getting started

```csharp
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.SecurityDomain;

var devices = await YubiKeyManager.FindAllAsync();
IYubiKey device = devices[0];
await using var session = await device.CreateSecurityDomainSessionAsync();

foreach (var keyInfo in await session.GetKeyInfoAsync())
{
    Console.WriteLine(keyInfo.KeyReference);
}
```

Reading key information needs no secure channel, so this works on an untouched device.

## Documentation

- [Security Domain module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/SecurityDomain/README.md) covers
  SCP03 and SCP11 setup, key rotation, certificates and allow lists, reset, and key handling.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit) covers the other modules, building
  from source, and release notes.
