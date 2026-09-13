# Yubico.NET.SDK

> ## ⚠️ v2 ALPHA — NOT FOR PRODUCTION
>
> The v2 SDK (`yubikit` branch) is a **pre-release alpha**. It is
> **subject to change** and has **not yet completed Yubico's formal security audit**.
>
> - **No security guarantees** are made until that audit is complete.
> - Packages are **unsigned**.
> - **Package names and namespaces may change** before the stable release.
> - Provided for **evaluation only**.

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
> `Yubico.NativeShims`) resolve. See the [feed website](https://yubico.github.io/Yubico.NET.SDK/)
> for the current package list, or the [release notes](scripts/alpha/RELEASE_NOTES.md)
> for full details.

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
dotnet add package Yubico.YubiKit.YubiHsm --prerelease
```

## Quick Start

### Basic Device Detection

```csharp
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Piv;

// Discover connected YubiKeys
var devices = await YubiKeyManager.FindAllAsync();
if (devices.Count == 0)
{
    return;
}

IYubiKey device = devices[0];

await using var managementSession = await device.CreateManagementSessionAsync();
var deviceInfo = await managementSession.GetDeviceInfoAsync();

Console.WriteLine($"YubiKey {deviceInfo.FirmwareVersion}");
Console.WriteLine($"Serial: {deviceInfo.SerialNumber}");
```

Later snippets assume these directives and a `device` obtained the same way.

### PIV Digital Signature

```csharp
await using var pivSession = await device.CreatePivSessionAsync();

// The YubiKey signs a digest, not a message. This overload reads the slot's
// algorithm from metadata, which needs firmware 5.3.0 or later; the slot's PIN
// and touch policies still apply, so verify the PIN first.
byte[] pin = Encoding.UTF8.GetBytes("123456");
try
{
    await pivSession.VerifyPinAsync(pin);
    byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes("Hello, YubiKey!"));
    ReadOnlyMemory<byte> signature = await pivSession.SignOrDecryptAsync(PivSlot.Authentication, digest);
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
}
```

### FIDO2 Registration

```csharp
await using var fidoSession = await device.CreateFidoSessionAsync();

// Query authenticator capabilities without requiring user presence
var info = await fidoSession.GetInfoAsync();
Console.WriteLine(string.Join(", ", info.Versions));
```

## Project Structure

Each module README covers requirements, installation, a first working call, common operations, and constraints.

- **[Yubico.YubiKit.Core](src/Core/README.md)** - Device discovery, connection management, APDU protocol handling
- **[Yubico.YubiKit.Management](src/Management/README.md)** - Device information and capability configuration
- **[Yubico.YubiKit.Piv](src/Piv/README.md)** - PIV smart card operations
- **[Yubico.YubiKit.Fido2](src/Fido2/README.md)** - FIDO2 CTAP authenticator operations
- **[Yubico.YubiKit.WebAuthn](src/WebAuthn/README.md)** - WebAuthn client API over FIDO2
- **[Yubico.YubiKit.Oath](src/Oath/README.md)** - TOTP/HOTP one-time passwords
- **[Yubico.YubiKit.YubiOtp](src/YubiOtp/README.md)** - Yubico OTP slot configuration and challenge-response
- **[Yubico.YubiKit.OpenPgp](src/OpenPgp/README.md)** - OpenPGP card implementation
- **[Yubico.YubiKit.SecurityDomain](src/SecurityDomain/README.md)** - Secure channel (SCP03/SCP11) and key management
- **[Yubico.YubiKit.YubiHsm](src/YubiHsm/README.md)** - YubiHSM Auth applet operations on YubiKey

## Native AOT

All SDK library packages (`Core`, `Management`, `Piv`, `Fido2`, `WebAuthn`, `Oath`, `OpenPgp`,
`SecurityDomain`, `YubiOtp`, `YubiHsm`) publish Native AOT compatibility metadata and participate in
the repository's analyzer and link-verification gates. Runtime evidence varies by platform and
module; see [`docs/NATIVE-AOT.md`](docs/NATIVE-AOT.md) for the evidence matrix and deployment
guidance. CLI tools and test projects are outside the support surface.

## Documentation

- **[Developer Guide](docs/)** - Detailed documentation for each module
- **[Physical Device Model](docs/architecture/physical-device-model.md)** - Discovery, transport selection, and session/connection ownership
- **[Device Discovery Guarantees](docs/architecture/device-discovery-guarantees.md)** - Exact grouping guarantees, conservative splits, and platform bounds
- **[Native AOT Support](docs/NATIVE-AOT.md)** - Native AOT support contract, platform matrix, and deployment guidance
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
