# GOAL: Implement OATH Applet for Yubico.NET.SDK

## Context

This is the Yubico.NET.SDK (YubiKit), a .NET 10 / C# 14 SDK for YubiKey devices. You are implementing the **OATH** applet (TOTP/HOTP one-time password generation). The project lives at the repository root. This is a 2.0 effort on `yubikit-*` branches -- do NOT touch `develop` or `main`.

The OATH application implements the TOTP (RFC 6238) and HOTP (RFC 4226) standards, allowing YubiKeys to store and generate one-time passwords.

## MANDATORY: Read These Files First

Before writing ANY code, you MUST read and internalize these files line by line:

1. **`CLAUDE.md`** (repository root) - All coding standards, memory management, security, modern C# patterns, build/test
2. **`Yubico.YubiKit.Management/CLAUDE.md`** - Session patterns, backend abstraction, DI, IYubiKey extensions, test infrastructure
3. **`Yubico.YubiKit.SecurityDomain/CLAUDE.md`** - Session initialization, reset patterns, SCP integration
4. **`docs/TESTING.md`** - Test infrastructure, xUnit v2/v3 differences, `[WithYubiKey]` attribute, test categories

## MANDATORY: Study These Reference Implementations

### Canonical Protocol Reference (Python) - READ EVERY LINE
**File:** `/Users/Dennis.Dyall/Code/y/yubikey-manager/yubikit/oath.py` (566 lines)

This is the authoritative wire protocol specification. Extract ALL:
- TLV tags (TAG_NAME=0x71 through TAG_TOUCH=0x7C)
- INS bytes (INS_LIST=0xA1 through INS_SEND_REMAINING=0xA5)
- Enums (OATH_TYPE, HASH_ALGORITHM)
- Constants (MASK_ALGO, MASK_TYPE, DEFAULT_PERIOD, etc.)
- Credential ID formatting (_format_cred_id, _parse_cred_id)
- HMAC key shortening, PBKDF2 key derivation

### Secondary Reference (Java)
**Directory:** `/Users/Dennis.Dyall/Code/y/yubikit-android/oath/src/main/java/com/yubico/yubikit/oath/`

### Architecture Reference (Existing C# Applets)
Study these for the EXACT patterns to replicate:
- `Yubico.YubiKit.Management/src/ManagementSession.cs` - Session pattern (private ctor, static CreateAsync, two-phase init)
- `Yubico.YubiKit.Management/src/DependencyInjection.cs` - Factory delegate + C# 14 `extension()` syntax
- `Yubico.YubiKit.Management/src/IYubiKeyExtensions.cs` - Convenience extensions with C# 14 `extension(IYubiKey)` syntax
- `Yubico.YubiKit.Management/src/IManagementSession.cs` - Interface extending IApplicationSession
- `Yubico.YubiKit.SecurityDomain/src/SecurityDomainSession.cs` - Single-backend session, reset pattern

## Architecture Requirements

### Source Files (in `Yubico.YubiKit.Oath/src/`)

The OATH applet is SmartCard-only (no HID backends), so no Backend pattern needed.

1. **`IOathSession.cs`** - Public interface extending `IApplicationSession`
2. **`OathSession.cs`** - Main session class:
   - `sealed class` extending `ApplicationSession`, implementing `IOathSession`
   - Private constructor + `static async Task<OathSession> CreateAsync(IConnection, ProtocolConfiguration?, ScpKeyParameters?, CancellationToken)`
   - Two-phase init: constructor creates protocol, `InitializeAsync` selects OATH app + configures
   - Parse SELECT response to get version, salt, challenge
   - Static logger: `private static readonly ILogger Logger = LoggingFactory.CreateLogger<OathSession>();`
   - Feature constants for version gating (e.g., rename requires 5.3.1)
3. **`DependencyInjection.cs`** - Factory delegate `OathSessionFactory` + `AddOath()` extension using C# 14 `extension(IServiceCollection services)` syntax
4. **`IYubiKeyExtensions.cs`** - Convenience extensions using C# 14 `extension(IYubiKey yubiKey)`:
   - `CreateOathSessionAsync(...)` for multi-operation scenarios
   - High-level single-operation methods like `ListOathCredentialsAsync()`, `CalculateAllOathCodesAsync()`
5. **Model files (one per type):**
   - `OathType.cs` - enum (Hotp=0x10, Totp=0x20)
   - `HashAlgorithm.cs` - enum (Sha1=0x01, Sha256=0x02, Sha512=0x03)
   - `Credential.cs` - credential record (deviceId, id, issuer, name, oathType, period, touchRequired)
   - `CredentialData.cs` - credential data for creation (name, oathType, hashAlgorithm, secret, digits, period, counter, issuer) with `ParseUri(string uri)` for otpauth:// URIs
   - `Code.cs` - code record (value, validFrom, validTo)

### Wire Protocol Details

**Application ID:** OATH AID (from ApplicationIds in Core)

**TLV Tags:**
- TAG_NAME = 0x71
- TAG_NAME_LIST = 0x72
- TAG_KEY = 0x73
- TAG_CHALLENGE = 0x74
- TAG_RESPONSE = 0x75
- TAG_TRUNCATED = 0x76
- TAG_HOTP = 0x77
- TAG_PROPERTY = 0x78
- TAG_VERSION = 0x79
- TAG_IMF = 0x7A
- TAG_TOUCH = 0x7C

**INS Bytes:**
- INS_LIST = 0xA1
- INS_PUT = 0x01
- INS_DELETE = 0x02
- INS_SET_CODE = 0x03
- INS_RESET = 0x04
- INS_RENAME = 0x05
- INS_CALCULATE = 0xA2
- INS_VALIDATE = 0xA3
- INS_CALCULATE_ALL = 0xA4
- INS_SEND_REMAINING = 0xA5

**Operations to implement:**
- `ResetAsync()` - Factory reset (INS=0x04, P1=0xDE, P2=0xAD), then re-select
- `DeriveKey(password)` - PBKDF2-HMAC-SHA1, salt from SELECT, 1000 iterations, 16 bytes
- `ValidateAsync(key)` - HMAC-SHA1 mutual auth with challenge-response
- `SetKeyAsync(key)` / `UnsetKeyAsync()` - Access key management
- `PutCredentialAsync(credentialData, touchRequired)` - Add TOTP/HOTP credential
- `RenameCredentialAsync(credentialId, name, issuer)` - Rename (requires 5.3.1+)
- `ListCredentialsAsync()` - List all credentials
- `CalculateAsync(credentialId, challenge)` - Raw calculate
- `CalculateCodeAsync(credential, timestamp)` - Calculate with formatting
- `CalculateAllAsync(timestamp)` - Calculate all TOTP codes (returns dict of Credential->Code)
- `DeleteCredentialAsync(credentialId)` - Delete credential

**Key implementation details from Python:**
- Credential IDs encode period/issuer/name: `"{period}/{issuer}:{name}"` (period only if non-default, issuer only if present)
- HMAC key shortening per RFC 2104 (hash if > block size)
- Secret padding to minimum 14 bytes
- TOTP challenge = big-endian int64 of `timestamp / period`
- Code formatting: `(truncated_bytes & 0x7FFFFFFF) % 10^digits`, right-justified with zeros
- `_neo_unlock_workaround` for firmware < 3.0.0 (re-select and validate after set_key)
- Device ID = base64(sha256(salt)[:16]) with padding stripped

### Security Requirements

- Zero ALL key material after use: `CryptographicOperations.ZeroMemory()`
- Zero PBKDF2-derived keys, HMAC results, challenge bytes
- Use `CryptographicOperations.FixedTimeEquals()` for HMAC comparison in validate
- Never log key values, only metadata
- ArrayPool buffers zeroed in `finally` blocks

### Test Files

**Unit tests** in `Yubico.YubiKit.Oath/tests/Yubico.YubiKit.Oath.UnitTests/`:
- Test credential ID formatting/parsing (edge cases: no issuer, non-default period, HOTP)
- Test otpauth:// URI parsing (valid, invalid, missing fields)
- Test code formatting
- Test HMAC key shortening
- Use `FakeSmartCardConnection` for protocol tests if available

**Integration tests** in `Yubico.YubiKit.Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/`:
- Use `[Theory] [WithYubiKey]` attribute pattern
- Create `OathTestStateExtensions.cs` with `WithOathSessionAsync` helper
- Reset OATH app before each test
- Test: list (empty), put credential, list (has credential), calculate, delete, list (empty again)
- Test: set access key, validate, unset access key
- Test: rename credential (version-gated to 5.3.1+)
- Test: calculate all
- Skip touch-requiring tests with `[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]`

**Testing rules:**
- ALWAYS use `dotnet build.cs test` (NEVER `dotnet test`)
- `[WithYubiKey]` + `[InlineData]` is INCOMPATIBLE - use separate test methods
- Skip user-presence tests: `--filter "Category!=RequiresUserPresence"`

### CLI Tool (in `Yubico.YubiKit.Oath/examples/OathTool/`)

Follow the PivTool/ManagementTool structure:
```
OathTool/
├── OathTool.csproj
├── Program.cs                    # FigletText banner + main menu
├── Cli/
│   ├── Output/OutputHelpers.cs
│   ├── Prompts/DeviceSelector.cs
│   └── Menus/
│       ├── CredentialMenu.cs     # Add/list/delete/rename credentials
│       ├── CodeMenu.cs           # Calculate single/all codes
│       └── AccessKeyMenu.cs      # Set/unset/validate access key
└── OathExamples/
    ├── ListCredentials.cs
    ├── AddCredential.cs
    ├── CalculateCode.cs
    ├── CalculateAll.cs
    ├── DeleteCredential.cs
    ├── SetAccessKey.cs
    └── ResetOath.cs
```

The CLI tool MUST support **command-line parameters** (not just interactive menus) so automated testing can drive it. Example: `OathTool list`, `OathTool add --uri "otpauth://..."`, `OathTool calculate --name "GitHub"`.

### Module CLAUDE.md

Create `Yubico.YubiKit.Oath/CLAUDE.md` following the structure of `Yubico.YubiKit.Management/CLAUDE.md`.

## Coding Standards Checklist

Every file MUST:
- [ ] Use file-scoped namespaces (`namespace Yubico.YubiKit.Oath;`)
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

- Branch: `yubikit-oath` (already created for you)
- Commit messages: `feat(oath): description` / `test(oath): description`
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
5. CLI tool runs and demonstrates all OATH operations
6. `Yubico.YubiKit.Oath/CLAUDE.md` exists with comprehensive module documentation
7. Code looks like it was written by the same developer who wrote Management/SecurityDomain
8. All sensitive data properly zeroed
9. No anti-patterns present
