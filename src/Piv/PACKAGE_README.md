# Yubico.YubiKit.Piv

PIV (Personal Identity Verification, NIST SP 800-73) support for YubiKey devices. This package turns a
YubiKey into a smart card holding private keys and X.509 certificates in numbered slots, with PIN, PUK,
and management-key authentication, key and certificate management, signing, decryption, ECDH key
agreement, and attestation.

> ## ALPHA - NOT FOR PRODUCTION
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Requirements

- .NET 10 on Windows, macOS, or Linux. Linux also needs PC/SC and udev rules.
- A YubiKey 4 or 5 series device with PIV enabled.
- SmartCard transport only (USB CCID or NFC). PIV is never available over HID.

Some operations need newer firmware: metadata reads need 5.3.0, an AES management key needs 5.4.0, and
move/delete key, Curve25519, and RSA 3072/4096 need 5.7.0.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Piv --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Getting started

```csharp
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Piv;

var devices = await YubiKeyManager.FindAllAsync();
IYubiKey device = devices[0];
await using var session = await device.CreatePivSessionAsync();

int pinAttempts = await session.GetPinAttemptsAsync();
Console.WriteLine($"PIN attempts remaining: {pinAttempts}");
```

Reading the PIN attempt counter needs no PIN, touch, or management key.

## Documentation

- [PIV module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/Piv/README.md) covers
  authentication, key and certificate operations, touch policy, constraints, and secret handling.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit) covers the other modules, building
  from source, and release notes.
