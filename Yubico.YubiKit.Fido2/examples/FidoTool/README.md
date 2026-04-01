# FidoTool - YubiKey FIDO2 CLI

A Spectre.Console CLI application that exposes the full FIDO2/CTAP2 API surface of the Yubico.NET.SDK. Serves as both a developer reference and an automated testing driver.

## Quick Start

```bash
# Build
dotnet build

# Interactive mode (menu-driven)
dotnet run

# CLI mode (verb-driven)
dotnet run -- info
dotnet run -- pin set --pin 12345678
```

## Commands

### info

Display authenticator capabilities, extensions, options, and limits.

```bash
FidoTool info
```

### pin

PIN management operations.

```bash
# Set initial PIN
FidoTool pin set --pin 12345678

# Change existing PIN
FidoTool pin change --old 12345678 --new 87654321

# View PIN and UV retry counts
FidoTool pin retries
```

### credential

Create and manage FIDO2 credentials.

```bash
# Create a discoverable credential (ES256, rk=true)
FidoTool credential make --rp example.com --user user@example.com --pin 12345678

# Create with custom display name
FidoTool credential make --rp example.com --user user@example.com --display "Jane Doe" --pin 12345678

# Get assertion (authenticate)
FidoTool credential assert --rp example.com --pin 12345678

# List all stored credentials (requires firmware 5.2+)
FidoTool credential list --pin 12345678

# Delete a credential by ID
FidoTool credential delete --id <hex-credential-id> --pin 12345678
```

### bio

Biometric (fingerprint) enrollment operations. Requires firmware 5.2+ with biometric hardware.

```bash
# Enroll a fingerprint
FidoTool bio enroll --pin 12345678 --name "Right index"

# List enrolled fingerprints
FidoTool bio list --pin 12345678

# Rename an enrollment
FidoTool bio rename --id <hex-template-id> --name "Left thumb" --pin 12345678

# Remove an enrollment
FidoTool bio remove --id <hex-template-id> --pin 12345678
```

### config

Authenticator configuration. Requires firmware 5.4+.

```bash
# Enable enterprise attestation (irreversible without reset)
FidoTool config enterprise --pin 12345678

# Toggle always-UV setting
FidoTool config always-uv --pin 12345678

# Set minimum PIN length
FidoTool config min-pin --length 8 --pin 12345678
```

### reset

Factory reset the FIDO2 application. **Destructive** - permanently deletes all credentials, PINs, and settings.

```bash
# Interactive reset (requires confirmation + touch within 5s of insertion)
FidoTool reset

# Automated reset (for CI/testing - skips confirmation prompts)
FidoTool reset --force
```

## Interactive Mode

Run without arguments to launch the interactive menu:

```bash
FidoTool
```

The menu provides guided flows for all operations with device selection, input prompts, and progress indicators.

## Firmware Feature Gating

| Feature | Min Firmware | Commands |
|---------|-------------|----------|
| FIDO2 base | 5.0 | info, pin, credential make/assert |
| Credential Management | 5.2 | credential list/delete |
| Bio Enrollment | 5.2 | bio enroll/list/rename/remove |
| Authenticator Config | 5.4 | config enterprise/always-uv/min-pin |

Operations targeting unsupported firmware will return a clear error message.

## Transport Support

- **FIDO HID** (USB) - Primary transport
- **SmartCard** (NFC only) - Secondary transport

FIDO2 is NOT available over USB CCID/SmartCard or OTP HID.

## Security Notes

- PINs are zeroed from memory immediately after use via `CryptographicOperations.ZeroMemory()`
- PIN tokens are zeroed after each operation
- PINs are never logged
- `clientDataHash` values are random 32-byte arrays for testing purposes; production WebAuthn implementations must use the SHA-256 of the actual clientData JSON
- The `--force` flag on reset is intended for automated testing only
