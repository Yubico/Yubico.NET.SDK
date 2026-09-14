# Yubico.YubiKit.YubiOtp

YubiOTP support for YubiKey devices: program the two slots with Yubico OTP, static password, OATH-HOTP,
or HMAC-SHA1 challenge-response, run challenge-response, and set the NFC NDEF record. Stored TOTP and
HOTP credentials are a different application; use `Yubico.YubiKit.Oath` for those.

> ## ALPHA - NOT FOR PRODUCTION
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.YubiOtp --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Documentation

- [YubiOTP module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/YubiOtp/README.md): requirements, getting started, common operations, constraints, and security notes.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit): all modules, building from source, and release notes.
