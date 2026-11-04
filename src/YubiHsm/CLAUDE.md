# CLAUDE.md - YubiHSM Auth Module

This file provides Claude-specific guidance for working with the YubiHSM Auth module. For overall repo conventions, see the repository root [CLAUDE.md](../../CLAUDE.md).

## Documentation Maintenance

> **Important:** This documentation is subject to change. When working on this module:
> - **Notable changes** to APIs, patterns, or behavior should be documented in both CLAUDE.md and README.md
> - **New features** should include usage examples
> - **Breaking changes** require updates with migration guidance
> - **Test infrastructure changes** should be reflected in the test pattern sections below

## Module Context

The YubiHSM Auth module manages the **YubiHSM Auth applet** on a YubiKey. It enrolls credentials and
calculates session keys used to authenticate to a YubiHSM 2. The application owns matching HSM object
enrollment, connector communication and handshake completion. LIST and retry reads return public metadata;
enrollment and calculation paths handle management keys, credential passwords and cryptographic material.

**Key characteristics:**
1. **SmartCard-only** — No HID or OTP transport; `HsmAuthBackend` owns applet select/send mechanics over `ISmartCardProtocol`
2. **Applet-owned backend** — Session code keeps APDU construction, retry extraction, and sensitive-buffer lifecycle; backend remains a thin SmartCard transport wrapper
3. **Two credential types** — Symmetric (AES-128, all firmware) and Asymmetric (EC P256, firmware 5.6.0+)
4. **Owned-buffer cleanup** — Session methods clear their padded passwords, encoded request buffers and derived keys in `finally`; `SessionKeys` clears its three owned arrays on disposal. Caller-owned inputs and additional copies have separate lifetimes.
5. **User presence** — Session-key calculations retain the caller-owned `SessionCreationOptions.UserPresencePrompt` for notifications around CALCULATE.

**Key Files:**
- [`HsmAuthSession.cs`](src/HsmAuthSession.cs) - Main session class (all APDU operations)
- [`HsmAuthBackend.cs`](src/Backend/HsmAuthBackend.cs) - SmartCard select/send backend for the YubiHSM Auth applet
- [`IHsmAuthSession.cs`](src/IHsmAuthSession.cs) - Public interface contract
- [`SessionKeys.cs`](src/SessionKeys.cs) - Disposable session key container (S-ENC, S-MAC, S-RMAC)
- [`HsmAuthAlgorithm.cs`](src/HsmAuthAlgorithm.cs) - Algorithm enum with C# 14 extension properties
- [`HsmAuthCredential.cs`](src/HsmAuthCredential.cs) - Credential record (from LIST response)
- [`IYubiKeyExtensions.cs`](src/IYubiKeyExtensions.cs) - `IYubiKey` convenience methods

## Firmware Feature Gates

Asymmetric, challenge and password-change operations use feature gates; base operations rely on successful
applet selection. Initialization uses the caller's firmware override or the SELECT applet version. The
backend falls back to 5.4.3 when a three-byte version value is unavailable. Reset reselects over the existing
protocol and retains resolved version policy. Device metadata and development-build labels alone establish
neither an operation's availability nor a suitable override.

| Feature | Version | Methods |
|---------|---------|---------|
| `FeatureHsmAuth` | 5.4.3 | Base applet support |
| `FeatureAsymmetric` | 5.6.0 | `PutCredentialAsymmetricAsync`, `GenerateCredentialAsymmetricAsync`, `CalculateSessionKeysAsymmetricAsync`, `GetPublicKeyAsync` |
| `FeatureGetChallenge` | 5.6.0 | `GetChallengeAsync` |
| `FeatureGetChallengeWithPassword` | 5.7.1 | `GetChallengeAsync` with credential password |
| `FeaturePasswordChange` | 5.8.0 | `ChangeCredentialPasswordAsync`, `ChangeCredentialPasswordAdminAsync` |

`FeatureGetChallengeNoPassword` is an obsolete alias of `FeatureGetChallengeWithPassword`; test basic
challenge availability with `FeatureGetChallenge` and password transmission with the correctly named field.

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
| 0x07 | GET_VERSION | 0 | 0 | Protocol instruction; this SDK resolves version from SELECT instead |
| 0x08 | PUT_MANAGEMENT_KEY | 0 | 0 | Change management key |
| 0x09 | GET_MANAGEMENT_KEY_RETRIES | 0 | 0 | Get retry counter |
| 0x0A | GET_PUBLIC_KEY | 0 | 0 | Get EC public key |
| 0x0B | CHANGE_CREDENTIAL_PASSWORD | 0/1 | 0 | Change password (P1=0 user, P1=1 admin) |

**TLV Tags:** 0x71 (Label), 0x72 (LabelList), 0x73 (CredentialPassword), 0x74 (Algorithm), 0x75 (KeyEnc), 0x76 (KeyMac), 0x77 (Context), 0x78 (Response/CardCryptogram), 0x79 (Version), 0x7A (Touch), 0x7B (ManagementKey), 0x7C (PublicKey), 0x7D (PrivateKey).

## Security Patterns

### Credential Password Handling

Credential passwords cross the public API as **UTF-8 `ReadOnlyMemory<byte>`**. Mutable byte storage lets
the caller clear its secret; .NET strings retain immutable storage. Parameters are named plainly (`credentialPassword`,
`derivationPassword`, `currentPassword`, `newPassword`) with no `Utf8` suffix; the UTF-8 encoding
contract lives in the XML documentation for each parameter, not in its name.

- **The caller owns the input buffer** and is responsible for zeroing it. The SDK never zeroes a
  buffer it does not own.
- `ValidateCredentialPassword(ReadOnlySpan<byte>)` is the single guard: **at most** 16 bytes.
- `ParseCredentialPassword(ReadOnlySpan<byte>)` validates, then copies into a fresh 16-byte
  buffer, null-padding the remainder.
- The padded copy is always zeroed in a `finally` block.
- Never log password values — log operation metadata only.

Keep acquisition and all later failures inside the caller's clearing scope. The README's typed enrollment
sketch borrows management/password inputs while owning the generated K-ENC/K-MAC buffer. Its provisioning
callback completes matching HSM enrollment and recovery storage before that buffer is cleared.

`label` is public LIST metadata and stays `string`.

### Management Key Verification
- Failed management key attempts return SW 0x63Cx (x = remaining retries)
- `ThrowOnManagementKeyFailure()` extracts retry count and throws `HsmAuthRetryException`, an `ApduException`
- Always call with `throwOnError: false` to allow retry extraction
- Enrollment, DELETE, management-key/password changes and CALCULATE use the retry helper.
  GET_CHALLENGE uses ordinary backend error handling, including when a password is transmitted.
- Yubico's descriptive applet overview says successful authentication before exhaustion restores the
  corresponding counter to eight. Exhausted credential retries delete that credential; exhausted
  management retries block management operations until destructive applet reset.

### Session Keys Lifecycle
Keep the returned `SessionKeys` in a `using` scope through the host's HSM task. `Parse` copies three
16-byte slices into owned arrays. Disposal clears those arrays, including storage viewed by existing spans,
and rejects later property access. The Parse input and any additional caller/connector copies require
separate clearing. Calculation returns key material; the HSM library completes host authentication.
The session passes the full CALCULATE response to the strict 48-byte key parser. A supplied symmetric
cryptogram can yield 56 bytes and fail parsing, as observed on a development key. Vendor Python has
handled 56-byte input since 2023 and now ignores the suffix; vendor C withholds an 8-byte symmetric
cryptogram and keeps strict 48-byte parsing. Preserve this request/response distinction without assigning
the extra bytes a meaning or firmware boundary. Qualify supplied-cryptogram examples until the response
contract is verified; omitted-cryptogram KDF checks establish no authenticated HSM handshake.

For symmetric and asymmetric CALCULATE operations, a listed credential with
`TouchRequired=true` maps to `PolicyRequires`; `false` is silent. A missing credential is also silent;
unknown touch values on a found credential and LIST failures map conservatively to `PolicyMayRequire`. Requests use
`Application="YubiHSM Auth"` and the credential label as scope, occur immediately before the
CALCULATE APDU, and are resolved once with `CancellationToken.None`. Do not report `TimedOut`
without a hardware status that confirms a touch timeout.
LIST cancellation propagates. A session without a prompt skips this notification lookup. Keep callbacks
prompt and non-reentrant. After cancellation is requested, await the operation's terminal result and
cleanup; an admitted SmartCard exchange can drain until a device response and end with an APDU failure.
Follow that outcome for recovery rather than promising native device interruption.

The symmetric context is `hostChallenge[8] || hsmChallenge[8]`; the HSM challenge and optional
card cryptogram are generated by the HSM and relayed by its connector. The asymmetric context is
`epkOce[65] || epkSd[65]`. Validate exact context lengths before calculation I/O and use the actual HSM
half for a live handshake. The asymmetric static public-key input identifies the trusted HSM; GET_PUBLIC_KEY
exports the YubiKey credential's PK-OCE for enrollment. Optional symmetric cryptogram omission skips that
applet verification step. GET_CHALLENGE response shape and asymmetric point/cryptogram validation remain
partly delegated to the integration layer/device; preserve that distinction in public contracts.

### EC Private Key Handling
- `PutCredentialAsymmetricAsync` clears its padded password/request copies; the caller clears imported private-key storage
- `GenerateCredentialAsymmetricAsync` generates on-device — private key never leaves the YubiKey

## PBKDF2 Key Derivation

`PutCredentialDerivedAsync` derives symmetric keys from a password:
- Algorithm: PBKDF2-HMAC-SHA256
- Salt: `"Yubico"` (UTF-8)
- Iterations: 10,000
- Output: 32 bytes → K-ENC[0..16], K-MAC[16..32]
- Derived key buffer zeroed in `finally`
- `DeriveKeys(ReadOnlyMemory<byte>)` passes the password span to PBKDF2 without an explicit helper-owned
  input copy. The caller clears the input. `Pbkdf2DerivationTests` recomputes expected output through the
  same .NET primitive using independently stated constants; it checks parameters rather than an external KAT.

## Test Infrastructure

### Unit Tests
Located in `tests/Yubico.YubiKit.YubiHsm.UnitTests/`. Pure helpers and recording-connection tests run without hardware:

- `SessionKeysTests` — Parse/dispose/zeroing behavior
- `CredentialPasswordTests` — UTF-8 encoding, padding, validation
- `LabelValidationTests` — Label length and encoding constraints
- `RetryExtractionTests` — Status word 0x63Cx parsing
- `HsmAuthCredentialTests` — Label ordering and all-property record equality
- `Pbkdf2DerivationTests` — Derivation parameters, split shape and repeatability
- `HsmAuthSessionByteLevelTests` — Headers, selected TLV bytes/order, context guards and reset override policy
- `HsmAuthRetryExceptionTests` — Typed retry translation for nine operation paths, excluding GET_CHALLENGE
- `TouchNotificationTests` — Recording-prompt policy mapping, request/resolution lifecycle, and no-hardware CALCULATE ordering

**Run tests:** `dotnet toolchain.cs test` (never `dotnet test` directly)

### Integration Tests
Located in `tests/Yubico.YubiKit.YubiHsm.IntegrationTests/`. Require a physical YubiKey with firmware 5.4.3+.
Read each assertion for its scope: symmetric calculation checks key lengths, derived enrollment checks
algorithm metadata, and the two touch-tagged cases check stored policy only. The asymmetric dummy flow
contains several invalid handshake inputs and checks an exact general APDU exception with nonzero status.
These are applet tests; use a real HSM handshake to establish authenticated interoperability. The helper
resets before the body by default and disposes resources afterward; arrange explicit post-run applet
cleanup/readback when persistent credentials or a replacement management key remain.

## Maintenance constraints

Place secret acquisition and cleanup in one failure-safe scope; clearing after an awaited send alone
leaves exception paths uncovered. The diagnostic example has pre-cleanup parsing/early-return paths, so
qualify its cleanup claims and preserve its evaluation warning. It prints long-lived/session keys and
accepts some secrets as visible strings. Its calculation menu always dispatches to the symmetric API.

Use the retry-checking send helper for operations whose public contract exposes typed retry failures.
Ordinary backend error throwing bypasses the helper's status parsing, as the current GET_CHALLENGE path
illustrates. Keep feature gates aligned with the actual operation and resolved applet policy.

Vendor manuals describe product behavior and pinned vendor encoders corroborate wire choices. Preserve
that source strength: neither establishes conformance to a versioned normative applet specification.
