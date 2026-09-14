# Yubico.YubiKit.Core

Core is the foundation of the Yubico .NET SDK. It discovers YubiKeys, opens and owns connections over
SmartCard (PC/SC) and HID, runs the ISO 7816-4 APDU pipeline with command chaining, implements Secure
Channel Protocol (SCP03 and SCP11), and supplies the shared device metadata, cryptography, and TLV types
every application module builds on.

> ## ALPHA - NOT FOR PRODUCTION
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.

## Requirements

- .NET 10 on Windows, macOS, or Linux. Linux also needs PC/SC and udev rules.
- A YubiKey 4 series, YubiKey 5 series, or Security Key series device.
- SmartCard (USB CCID and NFC), HID FIDO, and HID OTP transports.

## Installation

Most applications install an application package instead and get Core transitively. Install it directly
only for device discovery, raw sessions, or SCP key parameters without a specific applet.

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Core --prerelease
```

## Getting started

```csharp
using Yubico.YubiKit.Core.Devices;

var devices = await YubiKeyManager.FindAllAsync();

foreach (var device in devices)
{
    Console.WriteLine($"{device.DeviceId}: {device.AvailableConnections}");
}
```

One `IYubiKey` represents one physical YubiKey, even when it exposes several interfaces at once.
Discovery needs no PIN, touch, or open session.

## Documentation

- [Core module documentation](https://github.com/Yubico/Yubico.NET.SDK/blob/yubikit/src/Core/README.md) covers
  device discovery and monitoring, connection and session ownership, access tiers, SCP, and TLV handling.
- [Yubico.NET.SDK on GitHub](https://github.com/Yubico/Yubico.NET.SDK) covers the application modules, building
  from source, and release notes.
