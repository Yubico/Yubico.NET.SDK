# Yubico.YubiKit.Piv

PIV (Personal Identity Verification) support for YubiKey devices: PIN, PUK, and management-key
authentication, key and certificate management, signing, decryption, key agreement, and attestation.

> ## Alpha - not for production
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Piv --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Documentation

- [PIV module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/Piv/README.md): requirements, getting started, common operations, constraints, and security notes.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit): all modules, building from source, and release notes.
