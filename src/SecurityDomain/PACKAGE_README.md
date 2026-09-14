# Yubico.YubiKit.SecurityDomain

Security Domain support for YubiKey devices: inspect, generate, import, rotate, and delete the Secure
Channel Protocol (SCP) keys other applet sessions use, manage SCP11 certificates and allow lists, and
factory-reset the application.

> ## ALPHA - NOT FOR PRODUCTION
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.SecurityDomain --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Documentation

- [Security Domain module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/SecurityDomain/README.md): requirements, getting started, common operations, constraints, and security notes.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit): all modules, building from source, and release notes.
