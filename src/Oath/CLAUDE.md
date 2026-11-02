# CLAUDE.md - OATH Module

This file provides Claude-specific guidance for working with the OATH module. **Read the root [CLAUDE.md](../../CLAUDE.md) first** for project-wide conventions.

## Module Context

The OATH module implements TOTP (RFC 6238) and HOTP (RFC 4226) one-time password management for YubiKey devices. It communicates via ISO 7816-4 APDUs through the SmartCard transport only.

**Key characteristics:**
- **SmartCard-only** — no FIDO/OTP backend abstraction needed (like SecurityDomain)
- **No general session-open firmware floor** — SDK gates use the effective version for rename and secure-channel creation; touch and SHA-512 device requirements are not preflight-checked. See the table below.
- **Sealed session** — `OathSession` is sealed, extended via `IOathSession` interface
- **Two-phase init** — private constructor + static `CreateAsync` (matches Management/SecurityDomain)
- **Credential policy ownership** — `CredentialData.RequireTouch` travels with creation policy
- **Read-only aggregate results** — `CalculateAllAsync` returns `IReadOnlyDictionary<Credential, Code?>`
- **User-presence notifications** — individual calculations notify only when `Credential.TouchRequired` is `true`;
  `CalculateAllAsync` remains silent because it reports touch-required credentials without waiting on them

## Key Files

| File | Purpose |
|------|---------|
| `src/OathType.cs` | OATH type enum: HOTP = `0x10`, TOTP = `0x20` |
| `src/HashAlgorithm.cs` | Hash enum: SHA1 = `0x01`, SHA256 = `0x02`, SHA512 = `0x03` |
| `src/Credential.cs` | Immutable credential identity, equality on `(DeviceId, Id)` |
| `src/CredentialData.cs` | Credential creation data with `ParseUri()` and key processing |
| `src/Code.cs` | Calculated OTP code result |
| `src/OathConstants.cs` | TLV tags, INS bytes, protocol constants |

## Wire Protocol

### Credential ID Format

The SDK uses Yubico's host-side naming convention to encode metadata into a UTF-8 credential ID for
interoperability with Yubico Authenticator. YKOATH itself does not define this name grammar:

```
TOTP (default 30s period):  "issuer:name" or "name"
TOTP (custom period):       "{period}/issuer:name" or "{period}/name"
HOTP:                       "issuer:name" or "name" (no period prefix)
```

See `TotpIdPattern` and `ParseCredentialId` in `src/Credential.cs` for parsing. A recognized period prefix
uses ASCII digits followed by `/` and must fit a positive `Int32`; `-30/name` is a name, not a negative
period prefix. Using `[0-9]` rather than `\d` avoids recognizing Unicode decimal digits that integer
parsing would reject.

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
2. **SDK storage padding**: Append zeroes to secrets shorter than 14 bytes. This threshold is not an HMAC minimum key length or entropy requirement.
3. **TAG_KEY format**: `[type_byte][digits_byte][secret...]` where type_byte = `oath_type | hash_algorithm`

### Access Key Derivation

The SDK derives access keys on the host using
`PBKDF2-HMAC-SHA1(password_utf8, salt, 1000 iterations, 16 bytes)`, with the salt from the OATH SELECT response.
This follows the [YKOATH SET CODE](https://developers.yubico.com/OATH/YKOATH_Protocol.html) password
interoperability convention: UTF-8 input, PBKDF2 with 1000 iterations, the SELECT ID as salt, and 16 output bytes.
The protocol page does not explicitly name the PBKDF2 PRF; the SDK uses HMAC-SHA1.
The device receives the derived key, not the password or iteration count.

`IOathSession.DeriveKey` takes the password as a borrowed UTF-8 `ReadOnlyMemory<byte>` named
`password` — no `Utf8` suffix. The UTF-8 contract and the caller's obligation to zero both the
input buffer and the returned key live in the XML documentation for the member.

## Firmware Requirements and SDK Gates

| Feature | Firmware condition |
|---------|--------------------|
| Touch-required credentials | Device requirement: 4.2.4+; no SDK preflight gate |
| SHA-512 credentials | Device requirement: 4.3.1+; no SDK preflight gate |
| Rename credential | SDK gate: effective version 5.3.1+ |
| Secure-channel session | OATH gate: effective version 5.6.3+ for any non-null SCP parameters; Core also gates SCP11 at 5.7.2+ |

Touch and SHA-512 requirements come from the YKOATH protocol. Gates use the detected applet version unless
`FirmwareVersionOverride` supplies the effective version. The OATH gate applies to all SCP parameters despite
the `FeatureScp03` name; Core's SCP03 gate is 5.3.0 and its SCP11 gate is 5.7.2. The OATH 5.6.3 gate is current
SDK policy, not a verified earliest hardware version. Do not infer a general session-open firmware floor or
an SDK `NotSupportedException` for touch/SHA-512 credential options from these gates.

## Reference Implementation

**Python (authoritative):** `yubikit/oath.py` in yubikey-manager — follow this for exact wire format details.

## Testing

- Unit tests: `tests/Yubico.YubiKit.Oath.UnitTests/`
- Integration tests: `tests/Yubico.YubiKit.Oath.IntegrationTests/`
- **ALWAYS use `dotnet toolchain.cs test`** — never `dotnet test` directly

## User-presence notifications

When `SessionCreationOptions.UserPresencePrompt` is supplied, `CalculateAsync` and `CalculateCodeAsync`
issue `PolicyRequires` immediately before CALCULATE only for credentials whose `TouchRequired` value is
`true`. The scope is the public display identity (`issuer:name` or `name`); secret material is never included.
Successful, cancelled, and failed operations resolve as `Completed`, `Cancelled`, and `Failed`. OATH status
words do not currently provide verified evidence for mapping a failure to `TimedOut`. `CalculateCodeAsync`
validates the truncated response before successful resolution; `CalculateAsync` currently checks only that
the required response tag is present, not the full response value's structure. Sensitive response copies
from these individual calculations are cleared if resolution fails, and the immutable `Code.Value` string
is not formatted until the callback succeeds.
