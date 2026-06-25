# CLAUDE.md - OATH Module

This file provides Claude-specific guidance for working with the OATH module. **Read the root [CLAUDE.md](../CLAUDE.md) first** for project-wide conventions.

## Module Context

The OATH module implements TOTP (RFC 6238) and HOTP (RFC 4226) one-time password management for YubiKey devices. It communicates via ISO 7816-4 APDUs through the SmartCard transport only.

**Key characteristics:**
- **SmartCard-only** — no FIDO/OTP backend abstraction needed (like SecurityDomain)
- **Firmware 5.0.0+** — SDK 2.0 targets modern devices only, no NEO workarounds
- **Sealed session** — `OathSession` is sealed, extended via `IOathSession` interface
- **Two-phase init** — private constructor + static `CreateAsync` (matches Management/SecurityDomain)

## Key Files

| File | Purpose |
|------|---------|
| `src/OathType.cs` | TOTP/HOTP enum (`0x10`/`0x20`) |
| `src/HashAlgorithm.cs` | SHA1/SHA256/SHA512 enum (`0x01`/`0x02`/`0x03`) |
| `src/Credential.cs` | Immutable credential identity, equality on `(DeviceId, Id)` |
| `src/CredentialData.cs` | Credential creation data with `ParseUri()` and key processing |
| `src/Code.cs` | Calculated OTP code result |
| `src/OathConstants.cs` | TLV tags, INS bytes, protocol constants |

## Wire Protocol

### Credential ID Format

The credential ID encodes metadata into a UTF-8 byte string:

```
TOTP (default 30s period):  "issuer:name" or "name"
TOTP (custom period):       "{period}/issuer:name" or "{period}/name"
HOTP:                       "issuer:name" or "name" (no period prefix)
```

Parsing regex: `^((\d+)/)?(([^:]+):)?(.+)$`

### TLV Tag Constants

| Tag | Value | Purpose |
|-----|-------|---------|
| TAG_NAME | 0x71 | Credential ID / salt in SELECT |
| TAG_NAME_LIST | 0x72 | Credential listing entries |
| TAG_KEY | 0x73 | Secret key data |
| TAG_CHALLENGE | 0x74 | Challenge bytes |
| TAG_RESPONSE | 0x75 | HMAC response |
| TAG_TRUNCATED | 0x76 | Truncated OTP result |
| TAG_HOTP | 0x77 | HOTP indicator |
| TAG_PROPERTY | 0x78 | Credential properties |
| TAG_VERSION | 0x79 | Firmware version |
| TAG_IMF | 0x7A | Initial Moving Factor |
| TAG_TOUCH | 0x7C | Touch required indicator |

### Key Processing

1. **HMAC key shortening** (RFC 2104): If key length > hash block size, hash the key
2. **Secret padding**: Pad to minimum 14 bytes with zeroes
3. **TAG_KEY format**: `[type_byte][digits_byte][secret...]` where type_byte = `oath_type | hash_algorithm`

### Access Key Derivation

`PBKDF2-HMAC-SHA1(password_utf8, salt, 1000 iterations, 16 bytes)` — dictated by hardware.

## Version-Gated Features

| Feature | Minimum Version |
|---------|----------------|
| Rename credential | 5.3.1 |
| SCP03 for OATH | 5.6.3 |

## Reference Implementation

**Python (authoritative):** `yubikit/oath.py` in yubikey-manager — follow this for exact wire format details.

## Testing

- Unit tests: `tests/Yubico.YubiKit.Oath.UnitTests/`
- Integration tests: `tests/Yubico.YubiKit.Oath.IntegrationTests/`
- **ALWAYS use `dotnet toolchain.cs test`** — never `dotnet test` directly
