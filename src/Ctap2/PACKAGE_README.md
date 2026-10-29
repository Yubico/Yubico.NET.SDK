# Yubico.YubiKit.Ctap2

CTAP2 authenticator support for YubiKey devices: read capabilities, create and assert
credentials, manage the PIN, and drive credential management, biometric enrollment, large blobs, and
authenticator configuration. For the higher-level WebAuthn ceremony API, use `Yubico.YubiKit.WebAuthn`.

The authenticator retains its FIDO application identity and FIDO2 PIN. CTAPHID is the shared USB
transport; this package supplies CTAP2 operations, not a replacement identity for the transport.

> ## Alpha - not for production
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Ctap2 --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Documentation

- [CTAP2 module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/Ctap2/README.md): requirements, getting started, common operations, constraints, and security notes.
- [WebAuthn module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/WebAuthn/README.md): the higher-level client built on this package.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit): all modules, building from source, and release notes.
