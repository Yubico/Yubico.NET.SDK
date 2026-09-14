# Yubico.YubiKit.WebAuthn

W3C Web Authentication client for YubiKey devices, built on `Yubico.YubiKit.Fido2`. It reduces
registration and authentication to two calls and requires a WebAuthn origin and a Public Suffix List
backed checker.

> ## Alpha - not for production
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.WebAuthn --prerelease
```

`Yubico.YubiKit.Core` and `Yubico.YubiKit.Fido2` are installed transitively.

## Documentation

- [WebAuthn module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/WebAuthn/README.md): requirements, getting started, common operations, constraints, and security notes.
- [FIDO2 module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/Fido2/README.md): the CTAP layer underneath, including transport rules and firmware minimums.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit): all modules, building from source, and release notes.
