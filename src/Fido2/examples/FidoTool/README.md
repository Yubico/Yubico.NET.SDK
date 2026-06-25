# FidoTool - YubiKey FIDO2 CLI

A Spectre.Console CLI application that exposes the full FIDO2/CTAP2 API surface of the Yubico.NET.SDK. The CLI command structure mirrors [ykman](https://docs.yubico.com/software/yubikey/tools/ykman/FIDO_Commands.html)'s `fido` command group.

## Quick Start

```bash
# Build
dotnet build

# Interactive mode (menu-driven)
dotnet run

# CLI mode (ykman-compatible commands)
dotnet run -- info
dotnet run -- access change-pin --pin 12345678 --new-pin 87654321
```

## Commands

### info

Display general status of the FIDO2 application.

```bash
FidoTool info
```

### reset

Reset all FIDO applications. Permanently deletes all credentials, PINs, and settings.

```bash
# Interactive reset (requires confirmation + touch within 5s of insertion)
FidoTool reset

# Skip confirmation prompts (for CI/testing)
FidoTool reset -f
```

### access

Manage the FIDO2 PIN.

```bash
# Change PIN (prompts interactively if --pin/--new-pin omitted)
FidoTool access change-pin --pin 12345678 --new-pin 87654321

# Verify current PIN
FidoTool access verify-pin --pin 12345678
```

### config

Configure authenticator settings. Requires firmware 5.4+.

```bash
# Toggle always-UV setting
FidoTool config toggle-always-uv --pin 12345678

# Enable enterprise attestation (irreversible without reset)
FidoTool config enable-ep-attestation --pin 12345678
```

### credentials

Manage discoverable credentials. Requires firmware 5.2+.

```bash
# List all stored credentials
FidoTool credentials list --pin 12345678

# Delete a credential by ID (prompts for confirmation unless -f)
FidoTool credentials delete <hex-credential-id> --pin 12345678
FidoTool credentials delete <hex-credential-id> --pin 12345678 -f
```

### fingerprints

Manage fingerprint enrollments. Requires firmware 5.2+ with biometric hardware.

```bash
# List enrolled fingerprints
FidoTool fingerprints list --pin 12345678

# Enroll new fingerprint with a name
FidoTool fingerprints add "Right index" --pin 12345678

# Delete a fingerprint enrollment
FidoTool fingerprints delete <hex-fingerprint-id> --pin 12345678 -f

# Rename a fingerprint enrollment
FidoTool fingerprints rename <hex-fingerprint-id> "Left thumb" --pin 12345678
```

## Global Options

| Option | Description |
|--------|-------------|
| `--pin PIN` | Provide PIN on the command line. If omitted, the tool prompts interactively. |
| `-f`, `--force` | Skip confirmation prompts for destructive operations. |

## Interactive Mode

Run without arguments to launch the interactive menu:

```bash
FidoTool
```

The interactive menu provides guided flows for all operations including additional features not exposed as CLI verbs:

- Set PIN (first time)
- PIN/UV retry counts
- Create credential (MakeCredential)
- Get assertion (GetAssertion)
- Update user info on stored credentials
- Sensor info for biometric hardware
- Min PIN length configuration

## Firmware Feature Gating

| Feature | Min Firmware | Commands |
|---------|-------------|----------|
| FIDO2 base | 5.0 | info, reset, access |
| Credential Management | 5.2 | credentials list/delete |
| Bio Enrollment | 5.2 | fingerprints list/add/delete/rename |
| Authenticator Config | 5.4 | config toggle-always-uv/enable-ep-attestation |

Operations targeting unsupported firmware will return a clear error message.

## Transport Support

- **FIDO HID** (USB) - Primary transport
- **SmartCard** (NFC only) - Secondary transport

FIDO2 is NOT available over USB CCID/SmartCard or OTP HID.

## Device Selection

When only one YubiKey is connected, it is selected automatically. When multiple devices are present, the tool prompts for device selection.

## Security Notes

- PINs are zeroed from memory immediately after use via `CryptographicOperations.ZeroMemory()`
- PIN tokens are zeroed after each operation
- PINs are never logged
- `clientDataHash` values are random 32-byte arrays for testing purposes; production WebAuthn implementations must use the SHA-256 of the actual clientData JSON
- The `-f` flag on destructive operations is intended for automated testing only
