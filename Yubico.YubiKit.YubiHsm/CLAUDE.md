# CLAUDE.md - YubiHSM Auth Module

This file provides Claude-specific guidance for working with the YubiHSM Auth module. For overall repo conventions, see the repository root [CLAUDE.md](../CLAUDE.md).

## Documentation Maintenance

> **Important:** This documentation is subject to change. When working on this module:
> - **Notable changes** to APIs, patterns, or behavior should be documented in both CLAUDE.md and README.md
> - **New features** should include usage examples
> - **Breaking changes** require updates with migration guidance
> - **Test infrastructure changes** should be reflected in the test pattern sections below

## Module Context

The YubiHSM Auth module implements the **YubiHSM Auth applet** for authenticating to YubiHSM 2 hardware security modules. This is the **most security-sensitive applet** in the SDK — every operation involves management keys, session keys, credential passwords, or EC private keys.

**Key characteristics:**
1. **SmartCard-only** — No HID or OTP transport; uses direct `ISmartCardProtocol` calls (SecurityDomain pattern)
2. **No backend abstraction** — Single transport eliminates need for Backend pattern
3. **Two credential types** — Symmetric (AES-128, all firmware) and Asymmetric (EC P256, firmware 5.6.0+)
4. **Strict security** — All sensitive buffers zeroed in `finally` blocks; `SessionKeys` is `IDisposable`

**Key Files:**
- [`HsmAuthSession.cs`](src/HsmAuthSession.cs) - Main session class (all APDU operations)
- [`IHsmAuthSession.cs`](src/IHsmAuthSession.cs) - Public interface contract
- [`SessionKeys.cs`](src/SessionKeys.cs) - Disposable session key container (S-ENC, S-MAC, S-RMAC)
- [`HsmAuthAlgorithm.cs`](src/HsmAuthAlgorithm.cs) - Algorithm enum with C# 14 extension properties
- [`HsmAuthCredential.cs`](src/HsmAuthCredential.cs) - Credential record (from LIST response)
- [`DependencyInjection.cs`](src/DependencyInjection.cs) - `AddHsmAuth()` DI registration
- [`IYubiKeyExtensions.cs`](src/IYubiKeyExtensions.cs) - `IYubiKey` convenience methods

## Firmware Feature Gates

All firmware-gated operations use `EnsureSupports(Feature)` at method entry:

| Feature | Version | Methods |
|---------|---------|---------|
| `FeatureHsmAuth` | 5.4.3 | Base applet support |
| `FeatureAsymmetric` | 5.6.0 | `PutCredentialAsymmetricAsync`, `GenerateCredentialAsymmetricAsync`, `CalculateSessionKeysAsymmetricAsync`, `GetPublicKeyAsync` |
| `FeatureGetChallenge` | 5.6.0 | `GetChallengeAsync` |
| `FeatureGetChallengeNoPassword` | 5.7.1 | `GetChallengeAsync` without credential password |
| `FeaturePasswordChange` | 5.8.0 | `ChangeCredentialPasswordAsync`, `ChangeCredentialPasswordAdminAsync` |

## Wire Protocol

All communication uses ISO 7816-4 APDUs with CLA=0x00.

**Instruction Set:**

| INS | Name | P1 | P2 | Description |
|-----|------|----|----|-------------|
| 0x01 | PUT | 0 | 0 | Store credential (symmetric or asymmetric) |
| 0x02 | DELETE | 0 | 0 | Delete credential |
| 0x03 | CALCULATE | 0 | 0 | Calculate session keys |
| 0x04 | GET_CHALLENGE | 0 | 0 | Get host challenge / EPK-OCE |
| 0x05 | LIST | 0 | 0 | List credentials |
| 0x06 | RESET | 0xDE | 0xAD | Factory reset |
| 0x07 | GET_VERSION | 0 | 0 | Get applet version |
| 0x08 | PUT_MANAGEMENT_KEY | 0 | 0 | Change management key |
| 0x09 | GET_MANAGEMENT_KEY_RETRIES | 0 | 0 | Get retry counter |
| 0x0A | GET_PUBLIC_KEY | 0 | 0 | Get EC public key |
| 0x0B | CHANGE_CREDENTIAL_PASSWORD | 0/1 | 0 | Change password (P1=0 user, P1=1 admin) |

**TLV Tags:** 0x71 (Label), 0x72 (LabelList), 0x73 (CredentialPassword), 0x74 (Algorithm), 0x75 (KeyEnc), 0x76 (KeyMac), 0x77 (Context), 0x78 (Response/CardCryptogram), 0x79 (Version), 0x7A (Touch), 0x7B (ManagementKey), 0x7C (PublicKey), 0x7D (PrivateKey).

## Security Patterns

### Credential Password Handling
- String passwords are UTF-8 encoded and null-padded to 16 bytes via `ParseCredentialPassword()`
- Password bytes are always zeroed in `finally` blocks
- Never log password values — log operation metadata only

### Management Key Verification
- Failed management key attempts return SW 0x63Cx (x = remaining retries)
- `ThrowOnManagementKeyFailure()` extracts retry count and throws `ApduException`
- Always call with `throwOnError: false` to allow retry extraction

### Session Keys Lifecycle
```csharp
using var keys = await session.CalculateSessionKeysSymmetricAsync(label, context, password);
// Use keys.SEnc, keys.SMac, keys.SRmac
// All key material zeroed automatically on dispose
```

### EC Private Key Handling
- Private keys are never generated or stored in managed memory longer than necessary
- `PutCredentialAsymmetricAsync` zeros credential password but relies on caller to manage private key memory
- `GenerateCredentialAsymmetricAsync` generates on-device — private key never leaves the YubiKey

## PBKDF2 Key Derivation

`PutCredentialDerivedAsync` derives symmetric keys from a password:
- Algorithm: PBKDF2-HMAC-SHA256
- Salt: `"Yubico"` (UTF-8)
- Iterations: 10,000
- Output: 32 bytes → K-ENC[0..16], K-MAC[16..32]
- Derived key buffer zeroed in `finally`

## Test Infrastructure

### Unit Tests
Located in `tests/Yubico.YubiKit.YubiHsm.UnitTests/`. Test pure logic that doesn't require APDU transmission:

- `SessionKeysTests` — Parse/dispose/zeroing behavior
- `CredentialPasswordTests` — UTF-8 encoding, padding, validation
- `LabelValidationTests` — Label length and encoding constraints
- `RetryExtractionTests` — Status word 0x63Cx parsing
- `HsmAuthCredentialTests` — Record equality and sorting
- `Pbkdf2DerivationTests` — Known-answer PBKDF2 derivation

**Run tests:** `dotnet build.cs test` (never `dotnet test` directly)

### Integration Tests
Located in `tests/Yubico.YubiKit.YubiHsm.IntegrationTests/`. Require a physical YubiKey with firmware 5.4.3+.

## Common Anti-Patterns

**❌ Don't skip `finally` blocks for password/key zeroing**
```csharp
// BAD — password leaks on exception
var pw = ParseCredentialPassword(password);
await SendApdu(pw);
CryptographicOperations.ZeroMemory(pw); // Never reached on exception
```

**❌ Don't use `throwOnError: true` with management-key-gated operations**
```csharp
// BAD — 0x63Cx is treated as generic error, retry count lost
var response = await _protocol.TransmitAndReceiveAsync(command, cancellationToken: ct);
```

**❌ Don't forget firmware gates on asymmetric/password-change operations**
```csharp
// BAD — crashes on firmware < 5.6.0 with confusing APDU error
await PutCredentialAsymmetricAsync(...);

// GOOD — clear error message
EnsureSupports(FeatureAsymmetric);
await PutCredentialAsymmetricAsync(...);
```
