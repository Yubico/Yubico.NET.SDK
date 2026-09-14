# Yubico.YubiKit.OpenPgp

OpenPGP card support for YubiKey devices: manage the signature, decryption, and authentication keys and
their certificates, sign, decrypt, and authenticate on the device, and administer PINs and touch policy.

> ## ALPHA - NOT FOR PRODUCTION
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.OpenPgp --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Documentation

- [OpenPGP module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/OpenPgp/README.md): requirements, getting started, common operations, constraints, and security notes.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit): all modules, building from source, and release notes.
