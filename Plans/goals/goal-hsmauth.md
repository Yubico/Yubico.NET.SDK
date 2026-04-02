# GOAL: Implement HsmAuth Applet for Yubico.NET.SDK

## Context

This is the Yubico.NET.SDK (YubiKit), a .NET 10 / C# 14 SDK for YubiKey devices. You are implementing the **YubiHSM Auth** applet (credential-based authentication for YubiHSM 2). This is a 2.0 effort on `yubikit-*` branches -- do NOT touch `develop` or `main`.

YubiHSM Auth stores credentials used to authenticate to YubiHSM 2 devices. It supports symmetric (AES-128) and asymmetric (EC P256) credential types. This is the MOST security-sensitive applet — every operation involves management keys, session keys, or credential passwords.

## MANDATORY: Read These Files First

Before writing ANY code, you MUST read and internalize these files line by line:

1. **`CLAUDE.md`** (repository root) - All coding standards, memory management, security, modern C# patterns, build/test
2. **`Yubico.YubiKit.Management/CLAUDE.md`** - Session patterns, DI, IYubiKey extensions, test infrastructure
3. **`Yubico.YubiKit.SecurityDomain/CLAUDE.md`** - Session initialization, reset patterns, SCP integration
4. **`docs/TESTING.md`** - Test infrastructure, xUnit v2/v3 differences, `[WithYubiKey]` attribute, test categories

## MANDATORY: Study These Reference Implementations

### Canonical Protocol Reference (Python) - READ EVERY LINE
**File:** `/Users/Dennis.Dyall/Code/y/yubikey-manager/yubikit/hsmauth.py` (718 lines)

### Architecture Reference (Existing C# Applets)
Study these for the EXACT patterns to replicate:
- `Yubico.YubiKit.Management/src/ManagementSession.cs` - Session pattern (private ctor, static CreateAsync, two-phase init)
- `Yubico.YubiKit.Management/src/DependencyInjection.cs` - Factory delegate + C# 14 `extension()` syntax
- `Yubico.YubiKit.Management/src/IYubiKeyExtensions.cs` - Convenience extensions with C# 14 `extension(IYubiKey)` syntax
- `Yubico.YubiKit.Management/src/IManagementSession.cs` - Interface extending IApplicationSession
- `Yubico.YubiKit.SecurityDomain/src/SecurityDomainSession.cs` - SmartCard-only session (no backend), direct protocol calls

## Architecture Requirements

### Source Files (in `Yubico.YubiKit.YubiHsm/src/`)

SmartCard-only — follow SecurityDomainSession's direct protocol call pattern (no Backend abstraction).

1. **`IHsmAuthSession.cs`** - Public interface extending `IApplicationSession`
2. **`HsmAuthSession.cs`** - Main session class:
   - `sealed class` extending `ApplicationSession`, implementing `IHsmAuthSession`
   - Private constructor + `static async Task<HsmAuthSession> CreateAsync(IConnection, ProtocolConfiguration?, ScpKeyParameters?, CancellationToken)`
   - Two-phase init: constructor creates protocol, `InitializeAsync` selects HSMAUTH app + reads version
   - Parse SELECT response: version from TAG_VERSION TLV
   - Static logger: `private static readonly ILogger Logger = LoggingFactory.CreateLogger<HsmAuthSession>();`
   - Feature constants: `private static readonly Feature FeatureAsymmetric = new("Asymmetric credentials", 5, 6, 0);`
3. **`DependencyInjection.cs`** - Factory delegate `HsmAuthSessionFactory` + `AddHsmAuth()` extension using C# 14 `extension(IServiceCollection services)` syntax
4. **`IYubiKeyExtensions.cs`** - Convenience extensions using C# 14 `extension(IYubiKey yubiKey)`:
   - `CreateHsmAuthSessionAsync(...)` for multi-operation scenarios
   - `ListHsmAuthCredentialsAsync()` high-level convenience

### Model Files (one per type):
- `HsmAuthAlgorithm.cs` - enum (Aes128YubicoAuthentication=38, EcP256YubicoAuthentication=39) with `KeyLen` property (16 for AES128, 32 for EC P256) and `PubKeyLen` property (64 for EC P256)
- `HsmAuthCredential.cs` - `record` or `readonly record struct` (Label: string, Algorithm: HsmAuthAlgorithm, Counter: int, TouchRequired: bool?) with ordering by label (case-insensitive)
- `SessionKeys.cs` - `sealed class` implementing `IDisposable`:
  - Private `byte[]` fields for keySEnc[16], keySMac[16], keySRmac[16]
  - `ReadOnlySpan<byte>` properties to expose keys
  - `static SessionKeys Parse(ReadOnlySpan<byte> response)` factory — splits 48-byte response
  - `Dispose()` zeros all three key arrays with `CryptographicOperations.ZeroMemory()`
  - Callers use `using var keys = await session.CalculateSessionKeysSymmetricAsync(...)`

### Wire Protocol Details

**Application ID:** HSMAUTH AID (from ApplicationIds in Core)

**TLV Tags:**
- TAG_LABEL = 0x71
- TAG_LABEL_LIST = 0x72
- TAG_CREDENTIAL_PASSWORD = 0x73
- TAG_ALGORITHM = 0x74
- TAG_KEY_ENC = 0x75
- TAG_KEY_MAC = 0x76
- TAG_CONTEXT = 0x77
- TAG_RESPONSE = 0x78
- TAG_VERSION = 0x79
- TAG_TOUCH = 0x7A
- TAG_MANAGEMENT_KEY = 0x7B
- TAG_PUBLIC_KEY = 0x7C
- TAG_PRIVATE_KEY = 0x7D

**INS Bytes:**
- INS_PUT = 0x01
- INS_DELETE = 0x02
- INS_CALCULATE = 0x03
- INS_GET_CHALLENGE = 0x04
- INS_LIST = 0x05
- INS_RESET = 0x06
- INS_GET_VERSION = 0x07
- INS_PUT_MANAGEMENT_KEY = 0x08
- INS_GET_MANAGEMENT_KEY_RETRIES = 0x09
- INS_GET_PUBLIC_KEY = 0x0A
- INS_CHANGE_CREDENTIAL_PASSWORD = 0x0B

**Constants:**
- MANAGEMENT_KEY_LEN = 16
- CREDENTIAL_PASSWORD_LEN = 16
- MIN_LABEL_LEN = 1
- MAX_LABEL_LEN = 64
- DEFAULT_MANAGEMENT_KEY = 16 bytes of 0x00
- INITIAL_RETRY_COUNTER = 8

**Operations to implement:**
- `ResetAsync()` - Factory reset (INS=0x06, P1=0xDE, P2=0xAD), then re-SELECT and refresh state
- `ListCredentialsAsync()` - Parse TAG_LABEL_LIST TLVs: algorithm[1] + touchRequired[1] + label[N] + counter[1]
- `PutCredentialSymmetricAsync(mgmtKey, label, keyEnc, keyMac, credPw, touch)` - AES-128 symmetric credential
- `PutCredentialDerivedAsync(mgmtKey, label, derivationPw, credPw, touch)` - PBKDF2-derived symmetric
- `PutCredentialAsymmetricAsync(mgmtKey, label, privateKey, credPw, touch)` - EC P256 import (requires 5.6.0+)
- `GenerateCredentialAsymmetricAsync(mgmtKey, label, credPw, touch)` - Generate EC P256 on device (5.6.0+)
- `GetPublicKeyAsync(label)` - Get EC public key (5.6.0+), returns uncompressed point
- `DeleteCredentialAsync(mgmtKey, label)` - Delete credential (mgmt key authenticated)
- `ChangeCredentialPasswordAsync(label, currentPw, newPw)` - Change credential password (5.8.0+)
- `ChangeCredentialPasswordAdminAsync(label, mgmtKey, newPw)` - Admin change (5.8.0+, P1=1)
- `PutManagementKeyAsync(mgmtKey, newMgmtKey)` - Change management key
- `GetManagementKeyRetriesAsync()` - Get remaining retries (parse response as big-endian int)
- `CalculateSessionKeysSymmetricAsync(label, context, credPw, cardCrypto?)` - Symmetric session keys
- `CalculateSessionKeysAsymmetricAsync(label, context, publicKey, credPw, cardCrypto)` - Asymmetric (5.6.0+)
- `GetChallengeAsync(label, credPw?)` - Get host challenge / EPK-OCE (5.6.0+)

**Key implementation details from Python:**
- Management key retry extraction from SW: `sw & 0xFFF0 == SW.VERIFY_FAIL_NO_RETRY` → `retries = sw & ~0xFFF0`
- If retries extracted, throw `InvalidPinError(attempts_remaining=retries)` with message
- Credential password parsing: string → encode to UTF-8 → pad with null bytes to 16 bytes; byte[] → must be exactly 16 bytes
- Label parsing: encode string to UTF-8, validate 1-64 bytes
- PBKDF2 key derivation: HMAC-SHA256, salt=b"Yubico", 10000 iterations, 32 bytes → split into keyEnc[0:16] + keyMac[16:32]
- EC P256 public key format: uncompressed point (0x04 + x[32] + y[32] = 65 bytes)
- EC P256 private key: 32-byte big-endian integer
- `_put_credential` is internal helper that all put_credential variants call
- Touch required: TLV(TAG_TOUCH, 0x01) or TLV(TAG_TOUCH, 0x00) — always sent (not omitted when false)
- Version check: `require_version(self.version, (5, 6, 0))` for asymmetric operations
- Credential password for get_challenge: only sent on firmware >= (5, 7, 1) or major version 0

### CRITICAL Security Requirements

This applet handles the MOST sensitive data in the SDK. Every buffer containing sensitive data MUST be zeroed:

- **Management keys** (16 bytes) — Zero after EVERY use in `finally` block
- **Credential passwords** (16 bytes) — Zero after EVERY use in `finally` block
- **Session keys** (3x16=48 bytes) — `SessionKeys` implements `IDisposable`, zeros in `Dispose()`
- **EC private keys** (32 bytes) — Zero after import operation completes
- **PBKDF2-derived keys** (32 bytes) — Zero after splitting into keyEnc + keyMac
- **Context/challenge data** — Zero after calculation
- Use `CryptographicOperations.ZeroMemory()` on EVERY sensitive buffer
- Use `CryptographicOperations.FixedTimeEquals()` for any comparisons of key material
- ArrayPool buffers MUST be zeroed in `finally` blocks before return
- NEVER log management keys, credential passwords, session keys, or private keys
- Only log: labels, algorithm types, retry counts, operation names

### Test Files

**Unit tests** in `Yubico.YubiKit.YubiHsm/tests/Yubico.YubiKit.YubiHsm.UnitTests/`:
- Test credential password parsing: string padding (short string padded to 16, exact 16 bytes, too long throws)
- Test label validation: empty throws, > 64 bytes throws, valid label encodes correctly
- Test SessionKeys parsing: 48-byte response split correctly into 3x16-byte keys
- Test SessionKeys IDisposable: keys zeroed after dispose
- Test management key retry extraction from SW word
- Test HsmAuthCredential ordering (case-insensitive label comparison)
- Test PBKDF2 key derivation against known test vector

**Integration tests** in `Yubico.YubiKit.YubiHsm/tests/Yubico.YubiKit.YubiHsm.IntegrationTests/`:
- Use `[Theory] [WithYubiKey]` attribute pattern
- Create `HsmAuthTestStateExtensions.cs` with `WithHsmAuthSessionAsync` helper
- Test: reset, list credentials (empty), verify no credentials
- Test: put symmetric credential with default mgmt key, list (has credential), delete, list (empty)
- Test: put derived credential, verify listed with correct algorithm
- Test: put management key (change from default to custom, then back)
- Test: get management key retries (should be 8 after reset)
- Test: calculate session keys symmetric (verify 48 bytes returned)
- Test (5.6.0+): generate asymmetric credential, get public key (verify 65 bytes)
- Test (5.8.0+): change credential password
- Skip touch-requiring tests: `[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]`

**Testing rules:**
- ALWAYS use `dotnet build.cs test` (NEVER `dotnet test`)
- `[WithYubiKey]` + `[InlineData]` is INCOMPATIBLE - use separate test methods
- Skip user-presence tests: `--filter "Category!=RequiresUserPresence"`
- Version-gated tests: `[WithYubiKey(MinFirmware = "5.6.0")]` for asymmetric, `[WithYubiKey(MinFirmware = "5.8.0")]` for password change

### CLI Tool (in `Yubico.YubiKit.YubiHsm/examples/HsmAuthTool/`)

```
HsmAuthTool/
├── HsmAuthTool.csproj
├── Program.cs                        # FigletText banner + main menu
├── Cli/
│   ├── Output/OutputHelpers.cs
│   ├── Prompts/DeviceSelector.cs
│   └── Menus/
│       ├── CredentialMenu.cs         # List/add/delete credentials
│       ├── SessionKeyMenu.cs         # Calculate session keys
│       ├── ManagementKeyMenu.cs      # Management key operations
│       └── ResetMenu.cs             # Reset application
└── HsmAuthExamples/
    ├── ListCredentials.cs
    ├── AddSymmetricCredential.cs
    ├── AddDerivedCredential.cs
    ├── DeleteCredential.cs
    ├── CalculateSessionKeys.cs
    ├── ChangeManagementKey.cs
    ├── GetManagementKeyRetries.cs
    └── ResetHsmAuth.cs
```

The CLI tool MUST support **command-line parameters** (not just interactive menus) so automated testing can drive it. Examples:
- `HsmAuthTool list` - list all credentials
- `HsmAuthTool add-symmetric --label "test" --password "mypass"` - add symmetric credential
- `HsmAuthTool delete --label "test"` - delete credential
- `HsmAuthTool reset` - factory reset
- `HsmAuthTool retries` - show management key retries

### Module CLAUDE.md

Create `Yubico.YubiKit.YubiHsm/CLAUDE.md` following the structure of `Yubico.YubiKit.Management/CLAUDE.md`.

## Coding Standards Checklist

Every file MUST:
- [ ] Use file-scoped namespaces (`namespace Yubico.YubiKit.YubiHsm;`)
- [ ] Use `is null` / `is not null` (NEVER `== null`)
- [ ] Use switch expressions (NEVER old switch statements)
- [ ] Use collection expressions `[..]`
- [ ] Use `Span<byte>` with `stackalloc` for sync <=512 bytes
- [ ] Use `ArrayPool<byte>.Shared.Rent()` for sync >512 bytes with try/finally
- [ ] Zero sensitive data with `CryptographicOperations.ZeroMemory()`
- [ ] Use `CryptographicOperations.FixedTimeEquals()` for crypto comparisons
- [ ] Use `readonly` on fields that don't change
- [ ] Use `{ get; init; }` for immutable properties
- [ ] Handle `CancellationToken` in all async methods
- [ ] Use `.ConfigureAwait(false)` on all awaits
- [ ] NO `#region`, NO `.ToArray()` unless data must escape scope
- [ ] Static logger: `LoggingFactory.CreateLogger<T>()` (NEVER inject ILogger)

## Anti-Patterns (FORBIDDEN)

- `== null` (use `is null`)
- `#region` (split large classes instead)
- `.ToArray()` in hot paths
- Injected `ILogger` (use static `LoggingFactory`)
- `dotnet test` (use `dotnet build.cs test`)
- `git add .` or `git add -A`
- Old switch statements
- Exceptions for control flow
- Nullable warnings suppressed with `!` without justification

## Git

- Branch: `yubikit-hsmauth` (already created for you)
- Commit messages: `feat(hsmauth): description` / `test(hsmauth): description`
- NEVER use `git add .` or `git add -A` - add files explicitly

## Build & Test

```bash
dotnet build.cs build    # Must succeed with zero warnings
dotnet build.cs test     # Must pass all unit tests
dotnet build.cs test --filter "Category!=RequiresUserPresence"  # For integration tests
dotnet format            # Must produce no changes
```

## Definition of Done

1. All source files follow patterns from Management/SecurityDomain exactly
2. `dotnet build.cs build` succeeds with zero warnings
3. `dotnet build.cs test` passes all unit tests
4. Integration tests pass with physical YubiKey (skip user-presence tests)
5. CLI tool runs and demonstrates all HsmAuth operations with command-line parameters
6. `Yubico.YubiKit.YubiHsm/CLAUDE.md` exists with comprehensive module documentation
7. Code looks like it was written by the same developer who wrote Management/SecurityDomain
8. ALL sensitive data properly zeroed (management keys, credential passwords, session keys, private keys)
9. Security audit: grep for ZeroMemory confirms every sensitive buffer is zeroed
10. No anti-patterns present
