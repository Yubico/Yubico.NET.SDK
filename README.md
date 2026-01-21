# Yubico.NET.SDK

[![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/Yubico/Yubico.NET.SDK/badge)](https://securityscorecards.dev/viewer/?uri=github.com/Yubico/Yubico.NET.SDK)

A comprehensive .NET SDK for interacting with YubiKey hardware security devices. This SDK provides high-level APIs for YubiKey's various applications including PIV, FIDO2, OATH, OpenPGP, and more.

## Overview

YubiKey is a hardware authentication device that supports multiple protocols and applications for strong authentication, encryption, and digital signatures. This SDK enables .NET developers to integrate YubiKey functionality into their applications with a modern, type-safe API.

**Key Features:**
- 🔐 **PIV (Personal Identity Verification)** - Smart card functionality for digital signatures and encryption
- 🔑 **FIDO2/WebAuthn** - Passwordless authentication with modern web standards
- ⏱️ **OATH** - TOTP/HOTP one-time password generation
- 🔒 **YubiOTP** - Yubico's proprietary OTP protocol
- 📧 **OpenPGP** - Email encryption and code signing
- 🛡️ **Security Domain (SCP03)** - Secure channel protocol for key management
- 🔧 **Device Management** - Query capabilities, firmware version, and configuration

## Requirements

- **.NET 10.0** or later
- **Supported Platforms:** Windows, macOS, Linux
- **YubiKey** hardware device (YubiKey 4, YubiKey 5, Security Key series, or YubiHSM 2)

## Installation

Install packages from NuGet for the specific YubiKey applications you need:

```bash
# Core library (required)
dotnet add package Yubico.YubiKit.Core

# Application modules (install as needed)
dotnet add package Yubico.YubiKit.Piv
dotnet add package Yubico.YubiKit.Fido2
dotnet add package Yubico.YubiKit.Oath
dotnet add package Yubico.YubiKit.Management
```

## Quick Start

### Basic Device Detection

```csharp
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Management;

// Discover connected YubiKeys
var deviceRepository = new DeviceRepository();
var devices = await deviceRepository.GetDevicesAsync();

foreach (var device in devices)
{
    using var connection = await device.ConnectAsync();
    var session = new ManagementSession(connection);
    var deviceInfo = await session.GetDeviceInfoAsync();
    
    Console.WriteLine($"YubiKey {deviceInfo.FirmwareVersion}");
    Console.WriteLine($"Serial: {deviceInfo.SerialNumber}");
}
```

### PIV Digital Signature

```csharp
using Yubico.YubiKit.Piv;

using var connection = await device.ConnectAsync();
var pivSession = new PivSession(connection);

// Sign data with PIV slot
byte[] dataToSign = Encoding.UTF8.GetBytes("Hello, YubiKey!");
byte[] signature = await pivSession.SignAsync(PivSlot.Authentication, dataToSign);
```

### FIDO2 Registration

```csharp
using Yubico.YubiKit.Fido2;

using var connection = await device.ConnectAsync();
var fido2Session = new Fido2Session(connection);

// Create a new FIDO2 credential
var makeCredentialParams = new MakeCredentialParameters
{
    RelyingParty = new PublicKeyCredentialRpEntity { Id = "example.com", Name = "Example" },
    User = new PublicKeyCredentialUserEntity { Id = userId, Name = "user@example.com" }
};

var credential = await fido2Session.MakeCredentialAsync(makeCredentialParams);
```

## Project Structure

- **Yubico.YubiKit.Core** - Device discovery, connection management, APDU protocol handling
- **Yubico.YubiKit.Management** - Device information and capability queries
- **Yubico.YubiKit.Piv** - PIV smart card operations
- **Yubico.YubiKit.Fido2** - FIDO2/WebAuthn authentication
- **Yubico.YubiKit.Oath** - TOTP/HOTP one-time passwords
- **Yubico.YubiKit.YubiOtp** - Yubico OTP configuration
- **Yubico.YubiKit.OpenPgp** - OpenPGP card implementation
- **Yubico.YubiKit.SecurityDomain** - Secure channel (SCP03) and key management
- **Yubico.YubiKit.YubiHsm** - YubiHSM 2 hardware security module

## Documentation

- **[Developer Guide](docs/)** - Detailed documentation for each module
- **[API Reference](https://docs.yubico.com/yesdk/)** - Complete API documentation
- **[Examples](examples/)** - Sample applications and code snippets
- **[Contributing](CONTRIBUTING.md)** - Guidelines for contributors

## Building from Source

```bash
# Build the solution
dotnet build.cs build

# Run tests
dotnet build.cs test

# Create NuGet packages
dotnet build.cs pack
```

See [BUILD.md](BUILD.md) for detailed build instructions.

## Test Runner Support in IDEs

- Unit test projects use xUnit v3 with the Microsoft Testing Platform (`<UseMicrosoftTestingPlatformRunner>true`). Run them via `dotnet run --project ... --no-build` or use the build script (`dotnet build.cs test`).
- Integration test projects remain on xUnit v2 with `Microsoft.NET.Test.Sdk`, so they will appear in VS Code’s Test Explorer.
- VS Code’s C# extensions do **not** yet discover xUnit v3 / Testing Platform projects. Until Microsoft ships support, the unit tests are invisible in the Testing tab even though they run fine from the CLI.
