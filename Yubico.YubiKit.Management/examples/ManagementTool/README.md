# ManagementTool

YubiKey Management Tool - SDK Example Application for the Management module.

## Overview

ManagementTool is an interactive CLI application that demonstrates how to use the `Yubico.YubiKit.Management` SDK to manage YubiKey device configuration. It supports all Management operations across SmartCard, FIDO HID, and OTP HID transports.

## Features

- **Device Info** - Display comprehensive device information (serial, firmware, form factor, capabilities)
- **USB Capabilities** - Configure which applications are enabled over USB
- **NFC Capabilities** - Configure which applications are enabled over NFC
- **Timeouts** - Set auto-eject and challenge-response timeouts
- **Device Flags** - Configure device flags (eject, remote wakeup)
- **Lock Code** - Set, change, or remove the configuration lock code
- **Factory Reset** - Reset device to factory defaults (firmware 5.6.0+)

## Project Structure

```
ManagementTool/
├── Program.cs                    # Main entry point with menu loop
├── ManagementTool.csproj         # Project file
├── README.md                     # This file
├── Cli/
│   ├── Menus/                    # Menu handlers for each operation
│   │   ├── DeviceInfoMenu.cs
│   │   ├── CapabilitiesMenu.cs
│   │   ├── TimeoutsMenu.cs
│   │   ├── DeviceFlagsMenu.cs
│   │   ├── LockCodeMenu.cs
│   │   └── ResetMenu.cs
│   ├── Output/                   # Output formatting helpers
│   │   └── OutputHelpers.cs
│   └── Prompts/                  # User input prompts
│       ├── DeviceSelector.cs
│       └── LockCodePrompt.cs
└── ManagementExamples/           # Pure SDK example classes (no UI dependencies)
    ├── DeviceInfoQuery.cs
    ├── DeviceConfiguration.cs
    ├── DeviceReset.cs
    └── Results/                  # Immutable result types
        ├── DeviceInfoResult.cs
        ├── ConfigResult.cs
        └── ResetResult.cs
```

## Usage

```bash
# Build and run
cd Yubico.YubiKit.Management/examples/ManagementTool
dotnet run
```

## Security Considerations

- **Lock Codes** are handled securely with memory zeroing using `CryptographicOperations.ZeroMemory()`
- Lock codes are never logged or displayed
- All destructive operations require explicit confirmation
- Factory reset requires double confirmation (yes/no + type "RESET")

## Supported Transports

The tool can connect to YubiKeys via:
- **SmartCard (CCID)** - Standard smart card interface
- **FIDO HID** - FIDO/WebAuthn HID interface  
- **OTP HID** - Yubico OTP HID interface

## Requirements

- .NET 10.0 or later
- YubiKey 5 series or later
- Factory Reset requires firmware 5.6.0 or later

## Dependencies

- `Yubico.YubiKit.Management` - Management session and device configuration
- `Yubico.YubiKit.Core` - Core device discovery and connection abstractions
- `Spectre.Console` - Rich console output and prompts
