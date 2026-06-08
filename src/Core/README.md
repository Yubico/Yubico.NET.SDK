# Yubico.YubiKit.Core

Core foundational library for the Yubico.NET.SDK. This module provides device management, connection abstractions, protocol handling, and platform interop for all YubiKey applications.

## Overview

Yubico.YubiKit.Core is the foundation that all other SDK modules build upon. It handles the low-level details of communicating with YubiKey devices across different transport types (SmartCard/PC/SC and HID) and operating systems (Windows, macOS, Linux).

**Key Capabilities:**
- 🔍 **Device Discovery** - Automatic detection and monitoring of connected YubiKeys
- 🔌 **Connection Management** - Unified abstraction over SmartCard (PC/SC) and HID transports
- 📡 **Protocol Handling** - ISO 7816-4 APDU processing with automatic command chaining
- 🔐 **Secure Channel Protocol (SCP)** - SCP03, SCP11a/b/c support for secure communication
- 🖥️ **Platform Interop** - Cross-platform native library loading and device enumeration
- 🛠️ **Utilities** - TLV processing, cryptographic key types, COSE encoding

## Installation

```bash
dotnet add package Yubico.YubiKit.Core
```

This package is automatically included when you install any application-specific package (PIV, FIDO2, etc.).

## Quick Start

### Device Discovery

```csharp
using Yubico.YubiKit.Core;
// Get currently connected devices
var devices = await YubiKeyManager.FindAllAsync();

foreach (var device in devices)
{
    Console.WriteLine($"Found YubiKey: {device.SerialNumber}");
}

// Force a rescan when device topology may have changed
var freshDevices = await YubiKeyManager.FindAllAsync(forceRescan: true);

// Filter discovery. ConnectionType.Hid includes HID FIDO and HID OTP interfaces.
var hidDevices = await YubiKeyManager.FindAllAsync(ConnectionType.Hid);
var fidoDevices = await YubiKeyManager.FindAllAsync(ConnectionType.HidFido);
```

### Opening a Connection

```csharp
using Yubico.YubiKit.Core.Connections;

// Open SmartCard connection
await using var smartCardConnection = await device.ConnectAsync<ISmartCardConnection>();

// Open HID FIDO connection
await using var fidoConnection = await device.ConnectAsync<IFidoHidConnection>();

// Open HID OTP connection  
await using var otpConnection = await device.ConnectAsync<IOtpHidConnection>();
```

### Protocol Communication

```csharp
using Yubico.YubiKit.Core.SmartCard;

// Create protocol from connection
var protocol = PcscProtocolFactory<ISmartCardConnection>.Create().Create(smartCardConnection);

// Select an application (e.g., PIV)
await protocol.SelectAsync(ApplicationIds.Piv, cancellationToken);

// Configure for firmware version
protocol.Configure(firmwareVersion);

// Send APDU commands
var command = new ApduCommand
{
    Cla = 0x00,
    Ins = 0xA4,  // SELECT
    P1 = 0x04,
    P2 = 0x00,
    Data = applicationId
};

var responseData = await protocol.TransmitAndReceiveAsync(command, cancellationToken);
```

### Secure Channel Protocol (SCP)

```csharp
using Yubico.YubiKit.Core.SmartCard.Scp;

// Establish SCP03 session
var staticKeys = new StaticKeys(
    keyRef: 0x01,
    encKey: encKeyBytes,
    macKey: macKeyBytes,
    dekKey: dekKeyBytes
);

var scp03Params = new Scp03KeyParameters(keyRef, staticKeys);
var scpProtocol = await protocol.WithScpAsync(scp03Params, cancellationToken);

// Now all commands are encrypted/authenticated
await scpProtocol.SelectAsync(ApplicationIds.SecurityDomain, cancellationToken);

// Always zero sensitive key material
staticKeys.Dispose();
```

### TLV Processing

```csharp
using Yubico.YubiKit.Core.Tlv;

// Parse TLV data
var tlvs = TlvHelper.ParseMany(responseData);
var certificateTlv = tlvs.FirstOrDefault(t => t.Tag == 0x53);

// Build TLV structure
using var builder = new TlvBuilder();
builder.Add(0x5C, new byte[] { 0x5F, 0xC1, 0x02 });  // Tag list
builder.Add(0x53, certificateData);  // Certificate
var encodedData = builder.ToArray();

// Nested TLV
using var nestedBuilder = new TlvBuilder();
using (var nested = nestedBuilder.AddNested(0x7F49))  // Public key template
{
    nested.Add(0x81, modulusBytes);   // RSA modulus
    nested.Add(0x82, exponentBytes);  // RSA exponent
}
```

## Architecture

### Connection Abstraction

```
IYubiKeyDevice
    ↓
IConnection
    ├── ISmartCardConnection (PC/SC)
    ├── IFidoHidConnection (HID FIDO)
    └── IOtpHidConnection (HID OTP)
```

### APDU Processing Pipeline

```
ApduCommand
    ↓
[ChainedApduTransmitter]         ← Splits large commands
    ↓
[ApduFormatterShort/Extended]    ← Formats for wire protocol
    ↓
ISmartCardConnection
    ↓
[ChainedResponseReceiver]        ← Reassembles responses
    ↓
ApduResponse
```

### Platform Support

The Core module provides platform-specific implementations for:
- **Windows**: HidD, Cfgmgr32, WinSCard APIs
- **macOS**: IOKit, CoreFoundation, PC/SC
- **Linux**: udev, libpcsclite

Platform detection is automatic via `SdkPlatformInfo.OperatingSystem`.

## Key Classes

| Class | Purpose |
|-------|---------|
| `YubiKeyManager` | Static entry point for YubiKey discovery and cache management |
| `IYubiKey` | Represents a physical or virtual YubiKey device |
| `ISmartCardConnection` | SmartCard (PC/SC) transport connection |
| `IFidoHidConnection` | HID FIDO transport connection |
| `IOtpHidConnection` | HID OTP transport connection |
| `PcscProtocol` | ISO 7816-4 APDU protocol implementation |
| `ApduCommand` / `ApduResponse` | APDU command/response representations |
| `ScpProtocol` | Secure Channel Protocol wrapper (SCP03, SCP11) |
| `TlvHelper` / `TlvBuilder` | TLV parsing and construction utilities |
| `ApplicationSession` | Base class for application-specific sessions |

## Logging

Configure logging at application startup:

```csharp
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;

YubiKitLogging.LoggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

With dependency injection, configure YubiKit logging from the DI-provided logger factory during startup:

```csharp
services.AddLogging(builder => builder.AddConsole());

using var provider = services.BuildServiceProvider();
YubiKitLogging.Configure(provider.GetRequiredService<ILoggerFactory>());
```

## Firmware Version Considerations

Different YubiKey firmware versions have different capabilities:

```csharp
// Check firmware version
if (firmwareVersion.IsAtLeast(FirmwareVersion.V4_0_0))
{
    // Extended APDUs supported (up to 2048 bytes)
}

if (firmwareVersion.IsAtLeast(FirmwareVersion.V5_3_0))
{
    // SCP03 available
}

if (firmwareVersion.IsAtLeast(FirmwareVersion.V5_7_2))
{
    // SCP11 protocols available
}
```

## Security Considerations

- **Key Zeroing**: Always zero sensitive key material with `CryptographicOperations.ZeroMemory()` or dispose `StaticKeys`
- **Connection Lifetime**: Don't share connections across threads without synchronization
- **SCP Keys**: Store SCP keys securely; never log or persist them unencrypted
- **APDU Logging**: Disable trace logging in production to avoid leaking sensitive APDUs

## Related Modules

- **[Yubico.YubiKit.Management](../Management/)** - Device information and capability queries
- **[Yubico.YubiKit.Piv](../Piv/)** - PIV smart card operations
- **[Yubico.YubiKit.Fido2](../Fido2/)** - FIDO2/WebAuthn authentication
- **[Yubico.YubiKit.SecurityDomain](../SecurityDomain/)** - SCP key management

## Developer Documentation

For in-depth patterns, test infrastructure, and implementation details, see [CLAUDE.md](CLAUDE.md).
