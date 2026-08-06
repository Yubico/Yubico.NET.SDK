# Yubico.NET.SDK

> ## ⚠️ v2 ALPHA — NOT FOR PRODUCTION
>
> The v2 SDK (`2.0.0-alpha.2`, `yubikit` branch) is a **pre-release alpha**. It is
> **subject to change** and has **not yet completed Yubico's formal security audit**.
>
> - **No security guarantees** are made until that audit is complete.
> - Packages are **unsigned**.
> - **Package names and namespaces may change** before the stable release.
> - Provided for **evaluation only**.

[![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/Yubico/Yubico.NET.SDK/badge)](https://securityscorecards.dev/viewer/?uri=github.com/Yubico/Yubico.NET.SDK)

A .NET SDK for YubiKey hardware security devices. It provides APIs for YubiKey
applications including PIV, FIDO2, WebAuthn, OATH, YubiOTP, OpenPGP, Security
Domain (SCP03/SCP11), YubiHSM Auth, and device management.

See [Project Structure](#project-structure) for the per-module breakdown.

## Requirements

- **.NET 10.0** or later
- **Supported Platforms:** Windows, macOS, Linux
- **YubiKey** hardware device (YubiKey 4, YubiKey 5, Security Key series, or YubiHSM 2)

## Installation

> **Alpha:** the prerelease packages are distributed from a public, anonymous
> feed (not nuget.org). Add the feed first, then install with `--prerelease` to
> get the latest alpha. Keep nuget.org enabled so transitive dependencies (e.g.
> `Yubico.NativeShims`) resolve. Full details in the [release notes](scripts/alpha/RELEASE_NOTES.md).

```bash
# 1. Add the anonymous alpha feed (one time)
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha

# 2. Core library (required)
dotnet add package Yubico.YubiKit.Core --prerelease

# 3. Application modules (install as needed)
dotnet add package Yubico.YubiKit.Piv --prerelease
dotnet add package Yubico.YubiKit.Fido2 --prerelease
dotnet add package Yubico.YubiKit.WebAuthn --prerelease
dotnet add package Yubico.YubiKit.Oath --prerelease
dotnet add package Yubico.YubiKit.YubiOtp --prerelease
dotnet add package Yubico.YubiKit.OpenPgp --prerelease
dotnet add package Yubico.YubiKit.SecurityDomain --prerelease
dotnet add package Yubico.YubiKit.Management --prerelease

# Or pin an explicit version instead of --prerelease:
dotnet add package Yubico.YubiKit.Core --version 2.0.0-alpha.2
```

## Quick Start

### Basic Device Detection

```csharp
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Management;

// Discover connected YubiKeys
var devices = await YubiKeyManager.FindAllAsync();

foreach (var device in devices)
{
    await using var session = await device.CreateManagementSessionAsync();
    var deviceInfo = await session.GetDeviceInfoAsync();
    
    Console.WriteLine($"YubiKey {deviceInfo.FirmwareVersion}");
    Console.WriteLine($"Serial: {deviceInfo.SerialNumber}");
}
```

### PIV Digital Signature

```csharp
using Yubico.YubiKit.Piv;

await using var pivSession = await device.CreatePivSessionAsync();

// Sign data with PIV slot
byte[] dataToSign = Encoding.UTF8.GetBytes("Hello, YubiKey!");
byte[] signature = await pivSession.SignOrDecryptAsync(PivSlot.Authentication, dataToSign);
```

### FIDO2 Registration

```csharp
using Yubico.YubiKit.Fido2;

await using var fidoSession = await device.CreateFidoSessionAsync();

// Query authenticator capabilities without requiring user presence
var info = await fidoSession.GetInfoAsync();
Console.WriteLine(string.Join(", ", info.Versions));
```

## Project Structure

- **Yubico.YubiKit.Core** - Device discovery, connection management, APDU protocol handling
- **Yubico.YubiKit.Management** - Device information and capability queries
- **Yubico.YubiKit.Piv** - PIV smart card operations
- **Yubico.YubiKit.Fido2** - FIDO2/WebAuthn authentication
- **Yubico.YubiKit.WebAuthn** - WebAuthn API over FIDO2
- **Yubico.YubiKit.Oath** - TOTP/HOTP one-time passwords
- **Yubico.YubiKit.YubiOtp** - Yubico OTP configuration
- **Yubico.YubiKit.OpenPgp** - OpenPGP card implementation
- **Yubico.YubiKit.SecurityDomain** - Secure channel (SCP03/SCP11) and key management
- **Yubico.YubiKit.YubiHsm** - YubiHSM Auth applet operations on YubiKey

## Documentation

- **[Developer Guide](docs/)** - Detailed documentation for each module
- **[API Reference](https://docs.yubico.com/yesdk/)** - Complete API documentation
- Module examples live under `src/<Module>/examples/`

## Building from Source

```bash
# Build the solution
dotnet toolchain.cs build

# Run tests
dotnet toolchain.cs test

# Create NuGet packages
dotnet toolchain.cs pack
```

See [TOOLCHAIN.md](TOOLCHAIN.md) for detailed build instructions.

## Test Runner Support in IDEs

- Unit test projects use xUnit v3 with the Microsoft Testing Platform (`<UseMicrosoftTestingPlatformRunner>true`). Run them via `dotnet run --project ... --no-build` or use the build script (`dotnet toolchain.cs test`).
- Integration test projects remain on xUnit v2 with `Microsoft.NET.Test.Sdk`, so they will appear in VS Code’s Test Explorer.
- VS Code’s C# extensions do **not** yet discover xUnit v3 / Testing Platform projects. Until Microsoft ships support, the unit tests are invisible in the Testing tab even though they run fine from the CLI.
