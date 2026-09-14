# Yubico.YubiKit.Fido2

The FIDO2 application on a YubiKey is a CTAP (Client to Authenticator Protocol) 2.1/2.3 authenticator. This
package speaks CTAP directly: read authenticator capabilities, create and assert credentials, manage the
PIN, and drive the credential-management, biometric-enrollment, large-blob, and authenticator-config
sub-systems. For the higher-level W3C ceremony API, use `Yubico.YubiKit.WebAuthn` instead.

> ## ALPHA - NOT FOR PRODUCTION
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Requirements

- .NET 10 on Windows, macOS, or Linux. Linux also needs PC/SC and udev rules.
- A YubiKey 5 series, Security Key series, or YubiKey Bio series device with firmware 5.0.0 or later.
- HID FIDO over USB by default, or SmartCard where the FIDO2 AID is exposed: NFC always, USB on 5.8.0+.

Some features need newer firmware: credential management and biometric enrollment need 5.2.0, authenticator
config needs 5.4.0, and credBlob needs 5.5.0.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Fido2 --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Getting started

```csharp
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Fido2;

var devices = await YubiKeyManager.FindAllAsync();
IYubiKey device = devices[0];
await using var session = await device.CreateFidoSessionAsync();

var info = await session.GetInfoAsync();
Console.WriteLine($"CTAP versions: {string.Join(", ", info.Versions)}");
```

Reading authenticator information needs no touch and no PIN.

## Documentation

- [FIDO2 module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/Fido2/README.md) covers
  credential creation and assertion, PIN and user verification, extensions, transport rules, and secret handling.
- [WebAuthn module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/WebAuthn/README.md) covers
  the higher-level client built on this package.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK) covers the other modules, building
  from source, and release notes.
