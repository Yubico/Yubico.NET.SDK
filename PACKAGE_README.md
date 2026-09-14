# Yubico .NET SDK v2

Fallback package readme. Every shipping module has its own `src/<Module>/PACKAGE_README.md`, which is packed
as `README.md`; this file is used only by a packable project that does not have one yet.

> ## ALPHA - NOT FOR PRODUCTION
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

The v2 SDK provides type-safe .NET APIs for YubiKey hardware security devices, covering PIV, FIDO2,
WebAuthn, OATH, YubiOTP, OpenPGP, Security Domain, YubiHSM Auth, and device management.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Core --prerelease
```

Alpha packages are distributed from a public, anonymous feed; no authentication is required.

## Documentation

- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK) covers every module, building from
  source, and release notes.
- License: Apache-2.0
