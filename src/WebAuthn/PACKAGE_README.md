# Yubico.YubiKit.WebAuthn

This package is a W3C Web Authentication client over `Yubico.YubiKit.Fido2`. It reduces registration and
authentication to two calls, handling client data, relying-party ID validation, and PIN/UV token acquisition
for you. It requires a WebAuthn origin and a public-suffix checker backed by Public Suffix List data.

> ## ALPHA - NOT FOR PRODUCTION
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Requirements

- .NET 10 on Windows, macOS, or Linux. Linux also needs PC/SC and udev rules.
- A YubiKey 5 series, Security Key series, or YubiKey Bio series device with firmware 5.0.0 or later.
- HID FIDO or SmartCard transport (USB or NFC).
- An origin that is a secure context: `https`, or `http` on `localhost`.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.WebAuthn --prerelease
```

`Yubico.YubiKit.Core` and `Yubico.YubiKit.Fido2` are installed transitively.

## Getting started

```csharp
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.WebAuthn;
using Yubico.YubiKit.WebAuthn.Client;

var devices = await YubiKeyManager.FindAllAsync();
IYubiKey device = devices[0];
if (!WebAuthnOrigin.TryParse("https://example.com", out var origin))
    throw new InvalidOperationException("The origin is not a secure context.");

// Back this with Public Suffix List data in production.
PublicSuffixChecker isPublicSuffix = domain => domain is "com" or "net" or "org" or "co.uk";
await using var client = await device.CreateWebAuthnClientAsync(origin, isPublicSuffix);
```

The client owns and disposes the FIDO2 session it creates. Both ceremonies need a touch, so there is no
read-only call to try first; read authenticator capabilities with FIDO2 `GetInfoAsync` instead.

## Documentation

- [WebAuthn module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/WebAuthn/README.md) covers
  registration, authentication, PIN prompting, user-verification preferences, and constraints.
- [FIDO2 module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/Fido2/README.md) covers
  the CTAP layer underneath, including per-feature firmware minimums.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit) covers the other modules, building
  from source, and release notes.
