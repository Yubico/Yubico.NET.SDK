# CLAUDE.md - OpenPGP Module

This file provides module-specific guidance for working in **Yubico.YubiKit.OpenPgp**.
For overall repo conventions, see the repository root [CLAUDE.md](../CLAUDE.md).

## Module Context

OpenPGP implements the OpenPGP card application (v3.4 specification) for YubiKey devices. It provides:
- **Key Management**: Generate, import, delete, and attest RSA and EC keys across four key slots
- **Cryptographic Operations**: Sign, decrypt, and authenticate using on-device private keys
- **PIN Management**: Verify, change, and reset User PINs, Admin PINs, and Reset Codes with KDF support
- **Certificate Storage**: Store and retrieve X.509 certificates associated with key slots
- **Device Configuration**: Configure touch policy (UIF), algorithm attributes, and KDF settings

## Architecture

### Transport
SmartCard-only (`ISmartCardConnection`). OpenPGP spec is SmartCard/CCID only. No backend abstraction needed.

### Session Structure
`OpenPgpSession` is a `sealed partial class` split across 7 files:

| File | Responsibility |
|------|----------------|
| `OpenPgpSession.cs` | Core lifecycle, `CreateAsync`, `GetData`/`PutData`, cached state |
| `OpenPgpSession.Pin.cs` | PIN verify/change/reset, KDF integration |
| `OpenPgpSession.Keys.cs` | Key generate/import/delete/attest |
| `OpenPgpSession.Certificates.cs` | Certificate CRUD |
| `OpenPgpSession.Config.cs` | UIF, algorithm attributes, KDF configuration |
| `OpenPgpSession.Crypto.cs` | Sign, decrypt, authenticate |
| `OpenPgpSession.Reset.cs` | Factory reset sequence |

### Shared Session State
- `ISmartCardProtocol _protocol` — send APDUs
- `FirmwareVersion _version` — firmware for feature gating
- `ApplicationRelatedData _appData` — cached card state (refreshable)
- `Kdf? _kdf` — active KDF configuration (lazy-loaded)

## Key Patterns

### Initialization Sequence
1. SELECT OpenPGP AID → handle 0x6285/0x6985 with ACTIVATE → re-SELECT
2. GET_VERSION (INS=0xF1) → BCD decode; default to 1.0.0 if CONDITIONS_NOT_SATISFIED
3. Cache `ApplicationRelatedData` for feature detection and KDF state

### KDF (Iterated-Salted-S2K)
**NOT PBKDF2.** The `iteration_count` is total bytes to hash, not rounds:
```
data = salt + pin_bytes
data_count, trailing = divmod(iteration_count, len(data))
hash data data_count times, then data[:trailing]
```
Supports SHA256 (0x08) and SHA512 (0x0A).

### Factory Reset Sequence
1. Get PIN status (remaining attempts)
2. Block User PIN: VERIFY with invalid PIN until attempts exhausted
3. Block Admin PIN: same
4. TERMINATE (INS=0xE6)
5. ACTIVATE (INS=0x44)

## Model Layer

### Protocol Enums (internal)
- `Ins` — Instruction bytes
- `Crt` — Control Reference Templates
- `DataObject` — All Data Object tags

### Domain Enums (public)
- `KeyRef` — Key slots with computed DO tag properties
- `Pw` — PIN identifiers (User=0x81, Reset=0x82, Admin=0x83)
- `Uif` — User Interaction Flag with `IsFixed`/`IsCached`
- `PinPolicy`, `KeyStatus`, `RsaSize`, `RsaImportFormat`, `EcImportFormat`
- `CurveOid` — EC curve OID values with OID byte arrays
- `ExtendedCapabilityFlags`, `GeneralFeatureManagement` — `[Flags]` enums

### Data Models (public)
- `AlgorithmAttributes` (abstract) → `RsaAttributes`, `EcAttributes`
- `ApplicationRelatedData` — Top-level parsed card state
- `OpenPgpAid` — Parsed AID with BCD-decoded version
- `PwStatus` — PIN attempt counters and length limits
- `Kdf` (abstract) → `KdfNone`, `KdfIterSaltedS2k`
- `PrivateKeyTemplate` (abstract) → `RsaKeyTemplate`, `RsaCrtKeyTemplate`, `EcKeyTemplate`

## Firmware Compatibility

| Feature | Min Firmware | Gating |
|---------|-------------|--------|
| Basic OpenPGP | 1.0.0 | Always available |
| Factory reset | 1.0.6 | `EnsureSupports` |
| UIF (touch policy) | 4.2.0 | `EnsureSupports` |
| EC key support | 5.2.0 | `EnsureSupports` |
| Certificates | 5.2.0 | `EnsureSupports` |
| Key attestation | 5.2.0 | `EnsureSupports` |
| Unverify PIN | 5.6.0 | `EnsureSupports` |

## Security Requirements

- Zero ALL PIN bytes after use: `CryptographicOperations.ZeroMemory()`
- Zero private key material after import
- Zero KDF-derived bytes after use
- Use `CryptographicOperations.FixedTimeEquals()` for comparisons
- NEVER log PINs, keys, or credentials — only slot names, algorithm types, lengths

## Test Infrastructure

See `tests/CLAUDE.md` for test runner requirements.

**Unit tests** focus on model parsing/encoding:
- AlgorithmAttributes round-trip, ApplicationRelatedData TLV parsing
- KDF process against test vectors, OpenPgpAid BCD decoding
- PrivateKeyTemplate TLV encoding, PwStatus parsing, CurveOid mapping

**Integration tests** use `[Theory] [WithYubiKey]` with firmware version gating.
