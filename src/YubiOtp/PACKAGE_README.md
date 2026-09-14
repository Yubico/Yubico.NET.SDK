# Yubico.YubiKit.YubiOtp

The YubiOTP application owns the two programmable slots on a YubiKey. This package programs those slots with
a Yubico OTP, static password, OATH-HOTP, or HMAC-SHA1 challenge-response configuration, reads slot state,
runs challenge-response, and sets the NFC NDEF record. Stored TOTP and HOTP credentials are a different
application: use `Yubico.YubiKit.Oath` for those.

> ## ALPHA - NOT FOR PRODUCTION
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Requirements

- .NET 10 on Windows, macOS, or Linux. Linux also needs PC/SC and udev rules.
- A YubiKey with the OTP application enabled.
- SmartCard or HID OTP transport, selected in that order unless you override it.

Feature floors are per operation: serial read and challenge-response need firmware 2.2.0, slot update and
swap need 2.3.0, and NDEF configuration needs 3.0.0.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.YubiOtp --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Getting started

```csharp
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.YubiOtp;

var devices = await YubiKeyManager.FindAllAsync();
IYubiKey device = devices[0];
await using var session = await device.CreateYubiOtpSessionAsync();

ConfigState state = session.GetConfigState();
Console.WriteLine($"OTP applet firmware {state.FirmwareVersion}");
```

Reading the cached slot state needs no access code and no touch. Note that `state.IsConfigured(Slot.One)`
throws below firmware 2.1.0.

## Documentation

- [YubiOTP module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/YubiOtp/README.md) covers
  slot programming, challenge-response, touch, access codes, and destructive operations.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK) covers the other modules, building
  from source, and release notes.
