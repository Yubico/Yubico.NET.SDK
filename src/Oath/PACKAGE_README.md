# Yubico.YubiKit.Oath

OATH support for YubiKey devices: store TOTP and HOTP credentials on the device, calculate their codes,
and manage the optional password. The two programmable OTP slots are a different application; use
`Yubico.YubiKit.YubiOtp` for those.

> ## ALPHA - NOT FOR PRODUCTION
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Oath --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Documentation

- [OATH module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/Oath/README.md): requirements, getting started, common operations, constraints, and security notes.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit): all modules, building from source, and release notes.
