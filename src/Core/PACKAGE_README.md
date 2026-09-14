# Yubico.YubiKit.Core

Core is the foundation of the Yubico .NET SDK: device discovery, connections over SmartCard and HID, the
APDU and Secure Channel Protocol layers, and the shared types every application module builds on.
Most applications install an application package instead and get Core with it.

> ## Alpha - not for production
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Core --prerelease
```

## Documentation

- [Core module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/Core/README.md): device discovery, connections and sessions, access tiers, SCP, and security considerations.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit): all modules, building from source, and release notes.
