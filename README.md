# Yubico .NET SDK v2

A .NET SDK for YubiKey hardware security devices, for applications that need to talk to a YubiKey
directly: enterprise tooling, provisioning, custom authenticators, and services that verify or manage
keys. One package per YubiKey application, all on a shared core.

This is v2, developed on the `yubikit` branch. v1 lives on `develop`; see the
[migration guide](docs/migration/v1-to-v2.md) if you are coming from it.

> ## ALPHA - NOT FOR PRODUCTION
>
> The v2 SDK is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are unsigned,
> and package names and namespaces may change. Provided for evaluation only.

## Requirements

- .NET 10 on Windows, macOS, or Linux. Linux also needs PC/SC and udev rules ([Linux setup](docs/linux-setup.md)).
- A YubiKey 4 series, YubiKey 5 series, or Security Key series device. Firmware floors and transports vary
  by application; each package lists its own.

## Installation

Alpha packages come from a public, anonymous feed rather than nuget.org. Add the feed once, then install
the package for the YubiKey application you need. `Yubico.YubiKit.Core` and the native shims resolve
transitively, so keep nuget.org enabled.

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Piv --prerelease
```

Substitute the package from the table below. The [feed website](https://yubico.github.io/Yubico.NET.SDK/)
lists what is currently published.

## Getting started

Every application follows one shape: discover a device, create a session for the application, call it, and
let `await using` dispose it. Discovery alone needs only Core:

```csharp
using Yubico.YubiKit.Core.Devices;

var devices = await YubiKeyManager.FindAllAsync();

foreach (var device in devices)
{
    Console.WriteLine($"Serial {device.SerialNumber}: {device.AvailableConnections}");
}
```

One `IYubiKey` is one physical key, whichever interfaces it exposes. From here, an application package adds
`device.Create<Application>SessionAsync()`; each package README opens with that step and a first read-only
call, then covers the common operations. For applet specifics and features, see that documentation.

## Packages

| Package | YubiKey application |
|---|---|
| [Yubico.YubiKit.Piv](src/Piv/README.md) | PIV smart card: keys, certificates, signing, decryption |
| [Yubico.YubiKit.Fido2](src/Fido2/README.md) | FIDO2 CTAP authenticator operations |
| [Yubico.YubiKit.WebAuthn](src/WebAuthn/README.md) | WebAuthn client API over FIDO2 |
| [Yubico.YubiKit.Oath](src/Oath/README.md) | TOTP and HOTP credentials |
| [Yubico.YubiKit.YubiOtp](src/YubiOtp/README.md) | OTP slot configuration and challenge-response |
| [Yubico.YubiKit.OpenPgp](src/OpenPgp/README.md) | OpenPGP card keys, PINs, signing, decryption |
| [Yubico.YubiKit.SecurityDomain](src/SecurityDomain/README.md) | Secure channel (SCP03/SCP11) key management |
| [Yubico.YubiKit.YubiHsm](src/YubiHsm/README.md) | YubiHSM Auth applet on a YubiKey |
| [Yubico.YubiKit.Management](src/Management/README.md) | Device information and configuration |

[Yubico.YubiKit.Core](src/Core/README.md) underlies all of them: discovery, connections, sessions, and
secure channel. Interactive sample tools live under `src/<Module>/examples/`.

## Documentation

- [Physical device model](docs/architecture/physical-device-model.md): one `IYubiKey` per key, transport selection, connection and session ownership.
- [User interaction](docs/usage/user-interaction.md): touch notification and credential prompting across applications.
- [Migrating from v1](docs/migration/v1-to-v2.md) and [what changed in v2](docs/v2-highlights.md).
- [Native AOT](docs/NATIVE-AOT.md): every library package publishes AOT compatibility metadata.
- [API reference](https://docs.yubico.com/yesdk/).

## Contributing

Build, test, and packaging instructions are in the [developer guide](docs/DEV-GUIDE.md) and
[TOOLCHAIN.md](TOOLCHAIN.md).

## License

Apache License 2.0. See [LICENSE.txt](LICENSE.txt).
