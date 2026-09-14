# Yubico.YubiKit.Management

Management application support for YubiKey devices: read device information and configure which
applications are enabled per transport, timeouts, device flags, NFC restriction, and the configuration
lock code.

> ## ALPHA - NOT FOR PRODUCTION
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Management --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Documentation

- [Management module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/Management/README.md): requirements, getting started, common operations, constraints, and security notes.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit): all modules, building from source, and release notes.
