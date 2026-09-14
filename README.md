# Yubico.NET.SDK

A .NET SDK for YubiKey hardware security devices. It provides one package per YubiKey application:
PIV, FIDO2, WebAuthn, OATH, YubiOTP, OpenPGP, Security Domain (SCP03/SCP11), YubiHSM Auth, and
device management, all on a shared Core.

> ## ALPHA - NOT FOR PRODUCTION
>
> The v2 SDK on the `yubikit` branch is a pre-release alpha. It is subject to change and has **not yet
> completed Yubico's formal security audit**. No security guarantees are made until that audit is
> complete. Packages are unsigned, and package names and namespaces may change. Provided for evaluation
> only.

## Requirements

- .NET 10 on Windows, macOS, or Linux. Linux also needs PC/SC and udev rules ([Linux setup](docs/linux-setup.md)).
- A YubiKey 4 series, YubiKey 5 series, or Security Key series device. Each module lists its own
  firmware floor and transports.

## Installation

Alpha packages come from a public, anonymous feed rather than nuget.org. Add the feed once, then install
the module you need; `Yubico.YubiKit.Core` and the native shims resolve transitively, so keep nuget.org
enabled.

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Management --prerelease
```

Substitute the package for your application from the list below. The [feed website](https://yubico.github.io/Yubico.NET.SDK/)
shows what is currently published; [release notes](scripts/alpha/RELEASE_NOTES.md) have the details.

## Getting started

```csharp
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Management;

var devices = await YubiKeyManager.FindAllAsync();
IYubiKey device = devices[0];

await using var session = await device.CreateManagementSessionAsync();
var info = await session.GetDeviceInfoAsync();
Console.WriteLine($"YubiKey {info.FirmwareVersion}, serial {info.SerialNumber}");
```

One `IYubiKey` is one physical key. Every application follows the same shape: discover, create a session
with `device.Create<Application>SessionAsync()`, call it, and let `await using` dispose it. Each module
README opens with a read-only call like this one, then covers the common operations for that application.

## Modules

Each module README covers requirements, installation, getting started, common operations, user
interaction, constraints, and security notes.

| Package | Purpose |
|---|---|
| [Yubico.YubiKit.Core](src/Core/README.md) | Device discovery, connections, sessions, and SCP; installed with every module |
| [Yubico.YubiKit.Management](src/Management/README.md) | Device information and configuration |
| [Yubico.YubiKit.Piv](src/Piv/README.md) | PIV smart card: keys, certificates, signing, decryption |
| [Yubico.YubiKit.Fido2](src/Fido2/README.md) | FIDO2 CTAP authenticator operations |
| [Yubico.YubiKit.WebAuthn](src/WebAuthn/README.md) | WebAuthn client API over FIDO2 |
| [Yubico.YubiKit.Oath](src/Oath/README.md) | TOTP and HOTP credentials |
| [Yubico.YubiKit.YubiOtp](src/YubiOtp/README.md) | OTP slot configuration and challenge-response |
| [Yubico.YubiKit.OpenPgp](src/OpenPgp/README.md) | OpenPGP card keys, PINs, signing, decryption |
| [Yubico.YubiKit.SecurityDomain](src/SecurityDomain/README.md) | Secure channel (SCP03/SCP11) key management |
| [Yubico.YubiKit.YubiHsm](src/YubiHsm/README.md) | YubiHSM Auth applet on a YubiKey |

Interactive sample tools live under `src/<Module>/examples/`.

## Documentation

- [Physical device model](docs/architecture/physical-device-model.md): discovery, transport selection, and session and connection ownership.
- [User interaction](docs/usage/user-interaction.md): touch notification and credential prompting across applications.
- [Device discovery guarantees](docs/architecture/device-discovery-guarantees.md): grouping guarantees and platform bounds.
- [Native AOT](docs/NATIVE-AOT.md): every library package publishes AOT compatibility metadata; the evidence matrix and deployment guidance are there.
- [API reference](https://docs.yubico.com/yesdk/).

## Building from source

```bash
dotnet toolchain.cs build
dotnet toolchain.cs test
dotnet toolchain.cs pack
```

See [TOOLCHAIN.md](TOOLCHAIN.md) for the full target list, and [docs/DEV-GUIDE.md](docs/DEV-GUIDE.md) for
contributor guidance.

Unit tests use xUnit v3 on the Microsoft Testing Platform and are not yet discovered by the VS Code C#
extensions; run them from the CLI. Integration tests remain on xUnit v2 and do appear in Test Explorer.
