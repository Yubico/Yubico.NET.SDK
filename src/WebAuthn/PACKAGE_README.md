# Yubico.YubiKit.WebAuthn

Client-side WebAuthn registration and authentication for YubiKey devices, built on
`Yubico.YubiKit.Ctap2`. The client builds client data, orchestrates CTAP2 operations and typed
extensions, and handles PIN/UV token acquisition. It requires a supported WebAuthn origin and
`WebAuthnClientOptions` with an application-supplied `RpIdValidator`.

The validator owns RP validity, including syntax, and authorization of the exact origin/RP pair; a
Public Suffix List is not required for a narrow trusted allowlist. The RP supplies production ceremony
options and independently verifies responses before completing registration or sign-in. CTAP2 owns
direct authenticator operations, including credential administration; FIDO2 remains the application
identity. See the module guide for supported origins and current extension/protocol limitations.

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

`Yubico.YubiKit.Core` and `Yubico.YubiKit.Ctap2` are installed transitively.

## Documentation

- [WebAuthn module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/WebAuthn/README.md): requirements, getting started, common operations, constraints, and security notes.
- [CTAP2 module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/Ctap2/README.md): the authenticator operations layer underneath, including transport rules and firmware minimums.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit): all modules, building from source, and release notes.
