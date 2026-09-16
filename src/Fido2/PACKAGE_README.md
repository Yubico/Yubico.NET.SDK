# Yubico.YubiKit.Fido2

FIDO2 (CTAP) authenticator support for YubiKey devices: read capabilities, create and assert
credentials, manage the PIN, and drive credential management, biometric enrollment, large blobs, and
authenticator configuration. For the higher-level WebAuthn ceremony API, use `Yubico.YubiKit.WebAuthn`.

> ## Alpha - not for production
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Fido2 --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Documentation

- [FIDO2 module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/Fido2/README.md): requirements, getting started, common operations, constraints, and security notes.
- [WebAuthn module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/WebAuthn/README.md): the higher-level client built on this package.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit): all modules, building from source, and release notes.
