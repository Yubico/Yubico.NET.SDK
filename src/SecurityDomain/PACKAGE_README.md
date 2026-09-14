# Yubico.YubiKit.SecurityDomain

The Security Domain is the GlobalPlatform root security application on a YubiKey. It owns the Secure Channel
Protocol (SCP) keys that every other applet session can use to encrypt and authenticate its APDU traffic.
This package inspects, generates, imports, rotates, and deletes those keys, manages SCP11 certificates and
allow lists, and factory-resets the application. Other modules then consume the keys through
`SessionCreationOptions.ScpKeyParameters`.

> ## ALPHA - NOT FOR PRODUCTION
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Requirements

- .NET 10 on Windows, macOS, or Linux. Linux also needs PC/SC and udev rules.
- A YubiKey with firmware 5.3.0 or later for SCP03. Key attestation and allow lists need 5.7.0, and
  SCP11a, SCP11b, and SCP11c need 5.7.2.
- SmartCard transport only (USB CCID or NFC).

The application cannot report its own firmware version, so the session assumes 5.3.0 and skips firmware
gating unless you set `SessionCreationOptions.FirmwareVersionOverride`.

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
