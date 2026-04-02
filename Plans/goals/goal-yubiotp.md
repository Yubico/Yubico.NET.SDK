# GOAL: Implement YubiOTP Applet for Yubico.NET.SDK

## Context

This is the Yubico.NET.SDK (YubiKit), a .NET 10 / C# 14 SDK for YubiKey devices. You are implementing the **YubiOTP** applet (Yubico OTP, HOTP, static passwords, challenge-response). This is a 2.0 effort on `yubikit-*` branches -- do NOT touch `develop` or `main`.

YubiOTP is unique because it supports **dual transport** (SmartCard CCID and OTP HID), requiring the Backend pattern (like Management).

## MANDATORY: Read These Files First

Before writing ANY code, you MUST read and internalize these files line by line:

1. **`CLAUDE.md`** (repository root) - All coding standards, memory management, security, modern C# patterns, build/test
2. **`Yubico.YubiKit.Management/CLAUDE.md`** - Session patterns, Backend pattern, DI, IYubiKey extensions, test infrastructure
3. **`Yubico.YubiKit.SecurityDomain/CLAUDE.md`** - Session initialization, reset patterns, SCP integration
4. **`docs/TESTING.md`** - Test infrastructure, xUnit v2/v3 differences, `[WithYubiKey]` attribute, test categories

## MANDATORY: Study These Reference Implementations

### Canonical Protocol Reference (Python) - READ EVERY LINE
**File:** `/Users/Dennis.Dyall/Code/y/yubikey-manager/yubikit/yubiotp.py` (928 lines)

### Secondary Reference (Java)
**Directory:** `/Users/Dennis.Dyall/Code/y/yubikit-android/yubiotp/src/main/java/com/yubico/yubikit/yubiotp/`

### Architecture Reference (Existing C# Applets)
Study these for the EXACT patterns to replicate:
- `Yubico.YubiKit.Management/src/ManagementSession.cs` - Session + Backend pattern
- `Yubico.YubiKit.Management/src/IManagementBackend.cs` - Backend interface
- `Yubico.YubiKit.Management/src/SmartCardBackend.cs` - SmartCard backend
- `Yubico.YubiKit.Management/src/OtpBackend.cs` - OTP HID backend (same transport!)
- `Yubico.YubiKit.Management/src/DependencyInjection.cs` - Factory delegate + C# 14 `extension()` syntax
- `Yubico.YubiKit.Management/src/IYubiKeyExtensions.cs` - C# 14 extensions
- `Yubico.YubiKit.Management/src/IManagementSession.cs` - Interface extending IApplicationSession

## Architecture Requirements

### Source Files (in `Yubico.YubiKit.YubiOtp/src/`)

YubiOTP requires the **Backend pattern** because it works over SmartCard AND OTP HID:

1. **`IYubiOtpSession.cs`** - Public interface extending `IApplicationSession`
2. **`YubiOtpSession.cs`** - Main session class:
   - `sealed class` extending `ApplicationSession`, implementing `IYubiOtpSession`
   - Private constructor + `static async Task<YubiOtpSession> CreateAsync(IConnection, ProtocolConfiguration?, ScpKeyParameters?, CancellationToken)`
   - Two-phase init: constructor creates protocol + backend, `InitializeAsync` selects OTP app + reads status
   - Detects connection type: `SmartCardConnection` → `SmartCardBackend`, `OtpConnection` → `OtpHidBackend`
   - Static logger: `private static readonly ILogger Logger = LoggingFactory.CreateLogger<YubiOtpSession>();`
3. **`IYubiOtpBackend.cs`** - Internal backend interface with two primitives:
   - `ValueTask<byte[]> WriteUpdateAsync(ConfigSlot slot, byte[] data, CancellationToken ct)`
   - `ValueTask<byte[]> SendAndReceiveAsync(ConfigSlot slot, byte[] data, int expectedLen, CancellationToken ct)`
4. **`SmartCardBackend.cs`** - APDU backend:
   - Uses INS_CONFIG=0x01 (slot as P1) for writes
   - Uses INS_YK2_STATUS=0x03 for status queries
   - Validates prog_seq increment on writes (matching Python's `_YubiOtpSmartCardBackend.write_update`)
5. **`OtpHidBackend.cs`** - OTP HID backend:
   - Uses `IOtpHidProtocol.SendAndReceiveAsync(slot, data)` from Core
   - CRC validation on responses (the backend does this, not the protocol)
6. **`DependencyInjection.cs`** - Factory delegate `YubiOtpSessionFactory` + `AddYubiOtp()` extension using C# 14 `extension(IServiceCollection services)` syntax
7. **`IYubiKeyExtensions.cs`** - Convenience extensions using C# 14 `extension(IYubiKey yubiKey)`:
   - `CreateYubiOtpSessionAsync(...)` for multi-operation scenarios
   - `GetConfigStateAsync()` convenience method

### Model Files (one per type):
- `Slot.cs` - enum (One=1, Two=2) with static `Map<T>(Slot, T one, T two)` helper
- `ConfigSlot.cs` - enum (Config1=1, Nav=2, Config2=3, Update1=4, Update2=5, Swap=6, Ndef1=8, Ndef2=9, DeviceSerial=0x10, DeviceConfig=0x11, ScanMap=0x12, ChalOtp1=0x20, ChalOtp2=0x28, ChalHmac1=0x30, ChalHmac2=0x38)
- `TicketFlag.cs` - `[Flags] enum` (TabFirst=0x01, AppendTab1=0x02, AppendTab2=0x04, AppendDelay1=0x08, AppendDelay2=0x10, AppendCr=0x20, OathHotp=0x40, ChalResp=0x40, ProtectCfg2=0x80)
- `ConfigFlag.cs` - `[Flags] enum` (SendRef=0x01, ShortTicket=0x02, Pacing10ms=0x04, Pacing20ms=0x08, StrongPw1=0x10, StaticTicket=0x20, ChalYubico=0x20, StrongPw2=0x40, ChalHmac=0x22, HmacLt64=0x04, ChalBtnTrig=0x08, ManUpdate=0x80)
- `ExtendedFlag.cs` - `[Flags] enum` (SerialBtnVisible=0x01, SerialUsbVisible=0x02, SerialApiVisible=0x04, UseNumericKeypad=0x08, FastTrig=0x10, AllowUpdate=0x20, Dormant=0x40, LedInv=0x80)
- `ConfigState.cs` - `readonly struct` wrapping CFGSTATE flags with semantic properties:
  - `IsConfigured(Slot)` - checks SLOT1_VALID/SLOT2_VALID (requires 2.1.0+)
  - `IsTouchTriggered(Slot)` - checks SLOT1_TOUCH/SLOT2_TOUCH (requires 3.0.0+)
  - `IsLedInverted` property
- `NdefType.cs` - enum (Text='T', Uri='U')
- `SlotConfiguration.cs` - abstract base class with fluent builder pattern:
  - Protected `_fixed`, `_uid`, `_key` fields
  - Protected `_flags` dictionary keyed by flag type
  - `GetConfig(byte[]? accCode)` builds the 52-byte struct
  - Common fluent methods: `SerialApiVisible(bool)`, `AllowUpdate(bool)`, `Dormant(bool)`, `InvertLed(bool)`, `ProtectSlot2(bool)`
- `HmacSha1SlotConfiguration.cs` - HMAC-SHA1 challenge-response (key packed into key+uid, sets CHAL_RESP + CHAL_HMAC + HMAC_LT64 flags, fluent: `RequireTouch(bool)`, `Lt64(bool)`)
- `HotpSlotConfiguration.cs` - HOTP configuration (key in key+uid, sets OATH_HOTP, fluent: `Digits8(bool)`, `TokenId(bytes, modhex flags)`, `Imf(int)`)
- `StaticPasswordSlotConfiguration.cs` - Static password (scan codes packed into fixed+uid+key)
- `YubiOtpSlotConfiguration.cs` - Yubico OTP (fixed+uid+key, fluent: `Tabs(before, afterFirst, afterSecond)`, `Delay(afterFirst, afterSecond)`, `SendReference(bool)`)
- `StaticTicketSlotConfiguration.cs` - Static ticket (sets STATIC_TICKET, fluent: `ShortTicket(bool)`, `StrongPassword(upper, digit, special)`, `ManualUpdate(bool)`)
- `UpdateConfiguration.cs` - Update-only config (restricted flag masks, validates flags in `_update_flags` override)

### Wire Protocol Details

**SmartCard Backend:**
- INS_CONFIG = 0x01 (slot as P1, config data as payload)
- INS_YK2_STATUS = 0x03 (read status)
- prog_seq validation: `status[3]` must increment by 1 after write, or be 0 with previous > 0 (wraparound)
- Special case: firmware (5, 0, 0) to (5, 4, 3) — programming state doesn't update, accept regardless

**OTP HID Backend:**
- Uses `IOtpHidProtocol.SendAndReceiveAsync(slot, data)` directly
- CRC validation: `check_crc(response[:expectedLen + 2])` — response has 2-byte CRC appended
- CRC calculation matches `calculate_crc()` from Core OTP module

**Configuration Binary Format (52 bytes total):**
```
fixed[16] + uid[6] + key[16] + accCode[6] + fixedSize[1] + extFlags[1] + tktFlags[1] + cfgFlags[1] + rfu[2] + crc[2]
```
- CRC: `0xFFFF & ~calculate_crc(buf_without_crc)` (inverted CRC16)
- Access code: 6 bytes, or zeros if none

**NDEF Record Encoding:**
- URI type (0x55): 1-byte prefix code + encoded URI, padded to 54 bytes
- Text type (0x54): `\x02en` + text, padded to 54 bytes
- URI prefix table: 36 entries (http://www., https://www., http://, https://, tel:, mailto:, etc.)
- Default NDEF URI: "https://my.yubico.com/yk/#"

**Update Flag Masks:**
- TKTFLAG_UPDATE_MASK: TAB_FIRST | APPEND_TAB1 | APPEND_TAB2 | APPEND_DELAY1 | APPEND_DELAY2 | APPEND_CR
- CFGFLAG_UPDATE_MASK: PACING_10MS | PACING_20MS
- EXTFLAG_UPDATE_MASK: all EXTFLAG values

**Data sizes:**
- FIXED_SIZE=16, UID_SIZE=6, KEY_SIZE=16, ACC_CODE_SIZE=6, CONFIG_SIZE=52
- NDEF_DATA_SIZE=54, HMAC_KEY_SIZE=20, HMAC_CHALLENGE_SIZE=64, HMAC_RESPONSE_SIZE=20

**Key implementation details from Python:**
- HMAC key shortening: if key > SHA1_BLOCK_SIZE (64), hash with SHA1; if > HMAC_KEY_SIZE (20) but <= 64, throw NotSupportedError
- HMAC challenge padding: pad to 64 bytes with byte that differs from last byte (prevents ambiguity)
- SmartCard version detection over NFC: try reading management version first (more reliable), fall back to OTP version
- NEO version handling: if management version major==3, use max(mgmt_version, otp_version)
- Access code version gate: (4, 3, 2) to (4, 3, 6) cannot update access codes (must delete+reconfigure)

**Operations to implement:**
- `GetSerialAsync()` - Read serial via CONFIG_SLOT.DEVICE_SERIAL, 4-byte big-endian response
- `GetConfigState()` - Parse touch_level from status bytes: `struct.unpack("<H", status[4:6])`
- `PutConfigurationAsync(slot, config, accCode?, curAccCode?)` - Write slot config
- `UpdateConfigurationAsync(slot, config, accCode?, curAccCode?)` - Update existing config
- `SwapSlotsAsync()` - Swap slots 1 and 2
- `DeleteSlotAsync(slot, curAccCode?)` - Write CONFIG_SIZE zeros to delete
- `SetScanMapAsync(scanMap, curAccCode?)` - Write keyboard scan map
- `SetNdefConfigurationAsync(slot, uri?, curAccCode?, ndefType)` - Configure NFC NDEF
- `CalculateHmacSha1Async(slot, challenge)` - HMAC-SHA1 challenge-response (requires 2.2.0+)

### Security Requirements

- Zero ALL access codes after use: `CryptographicOperations.ZeroMemory()` on curAccCode and accCode buffers
- Zero HMAC-SHA1 keys in configuration builders (in `Dispose` or after write)
- Zero HMAC challenge-response data after calculation
- Zero static password scan codes after configuration
- ArrayPool buffers zeroed in `finally` blocks before return
- Use `CryptographicOperations.FixedTimeEquals()` for any crypto comparisons
- NEVER log access codes, HMAC keys, or OTP secrets — only log slot numbers, flag values

### Test Files

**Unit tests** in `Yubico.YubiKit.YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/`:
- Test configuration binary format building (verify 52-byte output matches expected bytes)
- Test CRC calculation against known values
- Test NDEF encoding (URI prefix compression, text record encoding, max length enforcement)
- Test flag mask validation in `UpdateConfiguration` (rejects unsupported flags)
- Test `ConfigState` property parsing from status bytes
- Test HMAC key shortening (> SHA1 block size, > HMAC key size, within limits)
- Test HMAC challenge padding logic

**Integration tests** in `Yubico.YubiKit.YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/`:
- Use `[Theory] [WithYubiKey]` attribute pattern
- Create `YubiOtpTestStateExtensions.cs` with `WithYubiOtpSessionAsync` helper
- Test: get config state (check slot configured/not configured)
- Test: put HMAC-SHA1 configuration to slot 2, verify configured, calculate HMAC, delete slot
- Test: put Yubico OTP configuration to slot 2, verify configured, delete slot
- Test: swap slots (configure one, swap, verify)
- Test: get serial number
- Test: set NDEF configuration for slot
- Skip touch-triggered tests: `[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]`

**Testing rules:**
- ALWAYS use `dotnet build.cs test` (NEVER `dotnet test`)
- `[WithYubiKey]` + `[InlineData]` is INCOMPATIBLE - use separate test methods
- Skip user-presence tests: `--filter "Category!=RequiresUserPresence"`

### CLI Tool (in `Yubico.YubiKit.YubiOtp/examples/OtpTool/`)

```
OtpTool/
├── OtpTool.csproj
├── Program.cs                       # FigletText banner + main menu
├── Cli/
│   ├── Output/OutputHelpers.cs
│   ├── Prompts/DeviceSelector.cs
│   └── Menus/
│       ├── StatusMenu.cs            # View slot status, serial number
│       ├── YubiOtpMenu.cs           # Configure Yubico OTP
│       ├── HmacMenu.cs              # Configure/calculate HMAC-SHA1
│       ├── StaticMenu.cs            # Configure static password
│       ├── HotpMenu.cs              # Configure HOTP
│       ├── SlotMenu.cs              # Swap/delete slots
│       └── NdefMenu.cs              # Configure NDEF
└── OtpExamples/
    ├── GetSlotStatus.cs
    ├── GetSerial.cs
    ├── ConfigureHmac.cs
    ├── CalculateHmac.cs
    ├── ConfigureYubiOtp.cs
    ├── ConfigureStaticPassword.cs
    ├── ConfigureHotp.cs
    ├── SwapSlots.cs
    ├── DeleteSlot.cs
    └── ConfigureNdef.cs
```

The CLI tool MUST support **command-line parameters** (not just interactive menus) so automated testing can drive it. Examples:
- `OtpTool status` - show slot configuration status
- `OtpTool serial` - show serial number
- `OtpTool hmac --slot 2 --challenge "test"` - calculate HMAC
- `OtpTool delete --slot 2` - delete slot configuration

### Module CLAUDE.md

Create `Yubico.YubiKit.YubiOtp/CLAUDE.md` following the structure of `Yubico.YubiKit.Management/CLAUDE.md`.

## Coding Standards Checklist

Every file MUST:
- [ ] Use file-scoped namespaces (`namespace Yubico.YubiKit.YubiOtp;`)
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

- Branch: `yubikit-yubiotp` (already created for you)
- Commit messages: `feat(yubiotp): description` / `test(yubiotp): description`
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
2. Backend pattern matches Management's IManagementBackend/SmartCardBackend/OtpBackend
3. `dotnet build.cs build` succeeds with zero warnings
4. `dotnet build.cs test` passes all unit tests
5. Integration tests pass with physical YubiKey (skip user-presence tests)
6. CLI tool runs and demonstrates all YubiOTP operations with command-line parameters
7. `Yubico.YubiKit.YubiOtp/CLAUDE.md` exists with comprehensive module documentation
8. Code looks like it was written by the same developer who wrote Management/SecurityDomain
9. All sensitive data (access codes, HMAC keys, OTP secrets) properly zeroed
10. No anti-patterns present
