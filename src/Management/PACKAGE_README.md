# Yubico.YubiKit.Management

The Management application is the device-level control surface of a YubiKey. This package reads device
information (serial number, firmware, form factor, supported and enabled capabilities) and writes device
configuration: which applications are enabled per transport, timeouts, device flags, NFC restriction, and
the configuration lock code. Other modules talk to one application; this one decides which applications
exist.

> ## ALPHA - NOT FOR PRODUCTION
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Requirements

- .NET 10 on Windows, macOS, or Linux. Linux also needs PC/SC and udev rules.
- A YubiKey 4.1.0 or later. Writing configuration needs 5.0.0, and device reset needs 5.6.0.
- SmartCard, HID FIDO, or HID OTP transport, selected in that order unless you override it.

## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Management --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.

## Getting started

```csharp
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Management;

var devices = await YubiKeyManager.FindAllAsync();
IYubiKey device = devices[0];
await using var session = await device.CreateManagementSessionAsync();

var info = await session.GetDeviceInfoAsync();
Console.WriteLine($"Serial {info.SerialNumber}, firmware {info.FirmwareVersion}, {info.FormFactor}");
```

Reading device information needs no PIN, touch, or lock code.

## Documentation

- [Management module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/Management/README.md) covers
  transport selection, capability configuration, reboot handling, the lock code, and connection ownership.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK/tree/yubikit) covers the other modules, building
  from source, and release notes.
