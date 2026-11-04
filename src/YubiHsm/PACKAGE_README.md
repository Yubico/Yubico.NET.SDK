# Yubico.YubiKit.YubiHsm

YubiHSM Auth applet support for YubiKey devices. The applet stores credentials used to authenticate to a
YubiHSM 2 and derives session keys. Your application provisions the matching HSM authentication object
and uses an HSM library to complete the handshake and exchange protected commands with the HSM.

> ## Alpha - not for production
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.YubiHsm --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Documentation

- [YubiHSM Auth module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/YubiHsm/README.md): requirements, getting started, common operations, constraints, and security notes.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit): all modules, building from source, and release notes.
