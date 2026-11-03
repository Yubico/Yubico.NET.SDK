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

### Credential secret ownership

`CredentialData.Secret` initialization copies a `ReadOnlyMemory<byte>` input into private owned storage. Its getter
borrows the same read-only view without allocating per read. Callers clear their original input buffers; disposal
clears private storage and existing views, not independent copies. Do not dispose concurrently with consumers.

`GetProcessedSecret` always returns independent owned storage, including the 14..block-size range that needs neither
padding nor hashing. PUT clears only that temporary and its payloads, never the credential seed, so Locked → VALIDATE
→ PUT can reuse the object. `ParseUri` and CLI decoders clear temporary decoded bytes on all relevant paths.

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

## Reset and timestamp boundaries

`ResetAsync` is always terminal after confirmed RESET success, for plain and SCP sessions. Keep the RESET
APDU visible and await Core's `DisposeAsync` immediately after its successful exchange. Do not SELECT,
refresh metadata, retain creation keys or rehandshake. Disposal clears readiness/authentication, protocol
and derived SCP state, salt and challenge; it ignores later caller cancellation. Teardown errors propagate
with the session still terminal and RESET already completed. Pre-admission cancellation/overlap refusal
must not tear down a healthy session or another admitted operation. A send/device error is not confirmed
success and does not prove no mutation. Borrowed connections remain caller-owned: callers close/reopen
them before creating a fresh session; convenience-owned connections are disposed by Core. Do not poison
connections globally. This deliberate OATH-only policy differs from Python's same-connection reselect/SCP
rehandshake; it is not a request to change other applets.

`WithOathSessionAsync` uses a separate reset session/owned connection, completes teardown, then opens a
fresh session/connection for the body. Preserve the first failure if later disposal/independent cleanup
also fails; do not combine explicit disposal with an implicit owner that can replay a failed completion.

Formatted calculations use nonnegative Unix seconds, with zero preserved as the epoch. `Code.GetTotpWindow`
checks that the exclusive end fits `Int64` before addition. Individual calculations validate before CALCULATE;
aggregate calculations validate the default period before transmission and custom periods before followup
CALCULATE, inside the existing raw-response cleanup. HOTP timestamps are metadata only and may reach `Int64.MaxValue`.
No `DateTimeOffset` calendar bound is imposed.

Failed verification of the device's VALIDATE HMAC is `BadResponseException`, not `WrongPassword`.
The existing device-rejection category still preserves raw status words; neither its compatibility name nor
the status alone proves that a user's password was incorrect. Post-verification-failure session policy is unchanged.

## User-presence notifications

When `SessionCreationOptions.UserPresencePrompt` is supplied, `CalculateAsync` and `CalculateCodeAsync`
issue `PolicyRequires` immediately before CALCULATE only for credentials whose `TouchRequired` value is
`true`. The scope is the public display identity (`issuer:name` or `name`); secret material is never included.
Successful, cancelled, and failed operations resolve as `Completed`, `Cancelled`, and `Failed`. OATH status
words do not currently provide verified evidence for mapping a failure to `TimedOut`. `CalculateCodeAsync`
validates the truncated response before successful resolution; `CalculateAsync` requires the full-response
tag, a digit header, and a nonempty HMAC payload. This is structural validation, not verification of a
particular digest length. It returns an independent copy of the HMAC only, without the digit header,
after the callback succeeds. The caller owns and clears that returned storage; caller inputs remain unchanged.
Raw VALIDATE and calculation response buffers are cleared in `finally`, and decoded TLV copies are disposed.
The immutable `Code.Value` string is not formatted until the individual-calculation callback succeeds.

CALCULATE ALL requests truncated responses. Validate known wire values before applying the host-side
credential-name omission policy or recalculating a custom period. Known metadata and full-response tags
are not future result markers. Do not require HOTP/touch marker values to be empty: the
[official legacy APDU example](https://docs.yubico.com/yesdk/users-manual/application-oath/oath-commands.html)
includes the digit-bearing HOTP marker `77 01 06`. Unknown named markers and standalone extensions retain their existing handling;
this does not establish a broader semantic policy for them or for duplicate entries.
