# ManagementTool

YubiKey Management Tool - SDK Example Application for the Management module.

## Overview

ManagementTool demonstrates device-information queries and persistent configuration changes with
`Yubico.YubiKit.Management`. Start it without arguments for menus, or use the read-only `info` and
`config` commands below. These commands still prompt for device selection; this is not an unattended
or serial-selectable command-line client.

The active selector currently lists only SmartCard-capable devices. The SDK itself also supports
device-info/configuration operations over HID FIDO and HID OTP. Reset remains SmartCard-only.

## Features

- **Device Info** - Display comprehensive device information (serial, firmware, form factor, capabilities)
- **USB Capabilities** - Configure which applications are enabled over USB
- **NFC Capabilities** - Configure which applications are enabled over NFC
- **Timeouts** - Set auto-eject and challenge-response timeouts
- **Device Flags** - Configure device flags (eject, remote wakeup)
- **Lock Code** - Set, change, or remove the configuration lock code (DANGEROUS, CAN LEAD TO BEING LOCKED OUT OF DEVICE)
- **Combined Reset** - Reset Bio Multi-protocol Edition FIDO/PIV applications and shared PIN over SmartCard

The reusable `DeviceConfiguration.SetNfcRestrictedAsync` helper is not reachable from the current menus
or command dispatch. The tool does not expose every SDK feature simply because a feature helper exists.

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
│       ├── LockCodePrompt.cs
│       └── ManagementDeviceSelector.cs  # Three-transport selector, not used by current callers
├── Commands/
│   ├── InfoCommand.cs             # Read-only device information
│   └── ConfigCommand.cs           # Read-only configuration
└── Features/           # Pure SDK example classes (no UI dependencies)
    ├── DeviceInfoQuery.cs
    ├── DeviceConfiguration.cs
    ├── DeviceReset.cs
    └── Results/                  # Immutable result types
        ├── DeviceInfoResult.cs
        ├── ConfigResult.cs
        └── ResetResult.cs
```

## Usage

Run from the repository root with .NET 10 and the platform setup described in the
[module README](../../README.md):

```bash
# Interactive menus
dotnet run --project src/Management/examples/ManagementTool/ManagementTool.csproj

# Read device information (device selection can still prompt)
dotnet run --project src/Management/examples/ManagementTool/ManagementTool.csproj -- info

# Read configuration; this command does not change it
dotnet run --project src/Management/examples/ManagementTool/ManagementTool.csproj -- config
```

The data commands are `info` and `config`. Top-level `-h`, `--help` and `help` print usage. Dispatch uses
only the first token and ignores extra command arguments, so do not rely on the printed `command --help`
suggestion: `info --help` can query a device instead of displaying command-specific help. Use the menu
for configuration changes. Monitoring starts before dispatch, including for top-level help.

## Configuration tasks and recovery

Before writing, confirm the selected physical key and save the current settings. USB/NFC capability
selections replace the complete enabled set for that transport; unchecked applications are disabled.
Keep at least one supported USB application and a recovery route. The SDK rejects exact USB `None`
but does not comprehensively validate supported masks. Disabling the last SmartCard-dependent application
can make this tool unable to select the key even when SDK HID configuration remains possible.

Timeout input ranges (0–3600 auto-eject seconds and 0–60 challenge-response seconds) are SDK limits, not
firmware maxima. Zero disables auto-eject or requests the challenge-response default (normally 15 seconds).
Auto-eject is a CCID inactivity timer. Touch-eject works only in CCID-only mode: OTP, U2F and FIDO2 must
be disabled; assigning a timeout does not set the flag automatically.

The configuration wrappers request reboot by default for capabilities, timeouts and flags. End old
scopes after reboot, then rediscover the intended key and open a fresh session. The result property
`RebootRequired` currently carries the requested reboot flag; it is not an observed pending-reboot state
or confirmation that re-enumeration has finished. Readback of persisted values does not prove active
interfaces have already changed.

If calling the NFC-restriction helper directly, use NFC-capable hardware with firmware 5.7 or later.
Restriction leaves the configured NDEF URL readable. Documented recovery is USB power for three seconds,
including from a charger; no physical touch is needed. A false value encodes a zero-valued request but
is not a verified recovery route. The SDK does not preflight those field-specific requirements.

The tool's reset eligibility display checks firmware only. The documented combined reset applies to
**Bio Multi-protocol Edition**, not every 5.6+ key or every Bio key. It deletes FIDO credentials,
fingerprints, the shared PIN and PIV keys/certificates/user data, except the F9 attestation slot. It
cannot be undone and is not a documented way to recover a lost Management configuration lock code.
The menu's broader erasure/default-PIN wording must not be used as a product-support check.

## Security Considerations

- The sample masks lock-code input and clears returned mutable byte arrays in the callers' `finally`
  blocks. Confirmation uses a fixed-time byte comparison. The input and normalized copies are immutable
  strings and cannot be erased by clearing the arrays. If the confirmation prompt throws before returning,
  the first parsed array can also escape cleanup. Do not use this prompt as a production secret-input primitive.
- Keep borrowed lock-code buffers valid and unchanged until the awaited write completes; clear owned
  secrets even when a write fails or is cancelled. Never log codes. Authorizing one write with the
  current code does not remove the lock; removal sends the current code and sixteen zero replacement bytes.
- Configuration menus ask for confirmation; reset asks twice (yes/no and typing `RESET`). The feature
  helpers themselves do not prompt, so direct callers must authorize the operation first.
- Cancellation is not consistent end to end: menu callbacks discard the console token, and feature
  wrappers/metadata lookup can turn cancellation exceptions into ordinary failure results. This is a
  sample limitation, not the SDK session's cancellation contract. Cancellation never rolls back a sent write.

## Metadata display limits

The current UI labels the manufacturing `IsFips` indicator as “FIPS Mode”; it is not an active operating
mode. Core also currently decodes FIPS capability/approval tags with the wrong bit mapping, so displayed
masks and derived filters cannot establish application approval. Product flag bits can remain in
`FormFactor`, so base-enum equality and formatting may miss flagged devices. These limitations are about
metadata interpretation, not a claim about a device's certification status.

## Supported Transports

The current menus and both local command handlers use the static SmartCard-only `DeviceSelector`.
`ManagementDeviceSelector` permits all three SDK transports but is not wired into those paths. The
no-device message still lists HID transports; that message does not reflect the active filter.

## Requirements

- .NET 10.0 or later
- A SmartCard-capable key; supported device-info firmware starts at 4.1.0, writes at 5.0.0. Not every
  field applies to every model. Follow the platform's PC/SC permissions/service setup.
- Combined reset: Bio MPE, SmartCard and the SDK's effective-firmware 5.6.0 gate. A passing firmware
  comparison does not establish product support; the device can reject the command.

## Dependencies

- `Yubico.YubiKit.Management` - Management session and device configuration
- `Yubico.YubiKit.Core` - Core device discovery and connection abstractions
- `Spectre.Console` - Rich console output and prompts
