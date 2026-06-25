# CLAUDE.md - YubiOTP Module

This file provides Claude-specific guidance for working with the YubiOTP module.

## Module Context

The YubiOTP module implements the Yubico OTP application for .NET. It is unique among SDK modules because it supports **dual transport**: SmartCard (CCID/NFC) and OTP HID (USB keyboard interface).

**Key Files:**
- [`YubiOtpSession.cs`](src/YubiOtpSession.cs) - Main session class with factory pattern
- [`IYubiOtpSession.cs`](src/IYubiOtpSession.cs) - Public session interface
- [`IYubiOtpBackend.cs`](src/IYubiOtpBackend.cs) - Internal backend abstraction (2 primitives)
- [`SmartCardBackend.cs`](src/SmartCardBackend.cs) - APDU-based backend with prog_seq validation
- [`OtpHidBackend.cs`](src/OtpHidBackend.cs) - Feature report backend with CRC validation
- [`SlotConfiguration.cs`](src/SlotConfiguration.cs) - Abstract base for 52-byte config struct
- [`IYubiKeyExtensions.cs`](src/IYubiKeyExtensions.cs) - C# 14 extension convenience methods
- [`DependencyInjection.cs`](src/DependencyInjection.cs) - `AddYubiOtp()` DI registration

## Architecture: Backend Pattern

YubiOtpSession delegates all transport-specific work to `IYubiOtpBackend`:

```
YubiOtpSession (public API, orchestration)
    |
    +-- IYubiOtpBackend (internal, 2 methods)
        |-- SmartCardBackend (APDUs, prog_seq validation)
        |-- OtpHidBackend (feature reports, CRC validation)
```

Backend has only two primitives:
- `WriteUpdateAsync` — write config/NDEF/swap/delete, returns status bytes
- `SendAndReceiveAsync` — challenge-response, serial read, returns data

## Session Initialization

SmartCard path:
1. SELECT `AID.MANAGEMENT` (for NEO version reliability)
2. SELECT `AID.OTP` → store status bytes
3. Extract firmware version (NEO workaround: use max of management/OTP versions)
4. `InitializeCoreAsync` → configure protocol, optionally establish SCP
5. If SCP: recreate backend with wrapped protocol

OTP HID path:
1. `ReadStatusAsync()` → store status bytes
2. Extract firmware version from protocol or status bytes
3. `InitializeCoreAsync` → configure protocol

## Slot Configuration Model

52-byte wire format struct assembled by `SlotConfiguration.GetConfig()`:
```
[fixed:16][uid:6][key:16][acc_code:6][fixed_size:1][ext:1][tkt:1][cfg:1][rfu:2][crc:2]
```

Configuration hierarchy:
- `SlotConfiguration` (abstract base)
  - `HmacSha1SlotConfiguration`
  - `KeyboardSlotConfiguration` (abstract, adds CR/tabs/pacing)
    - `YubiOtpSlotConfiguration`
    - `HotpSlotConfiguration`
    - `StaticPasswordSlotConfiguration`
    - `StaticTicketSlotConfiguration`
  - `UpdateConfiguration` (restricted flag masks)

## NDEF Encoding

`SetNdefConfigurationAsync` builds a 56-byte NDEF payload:
- URI type (`0x55`): Compresses prefixes using 36-entry NFC Forum URI prefix table
- Text type (`0x54`): Language header `[0x02]["en"]` + content
- Null content disables NDEF (all zeros)

## HMAC-SHA1 Challenge Padding

Challenges are padded to 64 bytes. The pad byte must differ from the last byte
of the challenge so the YubiKey can detect actual data length:
- Last byte non-zero → pad with `0x00`
- Last byte zero → pad with `0x01`
- Empty challenge → pad with `0x01`

## Security Considerations

- Access codes zeroed via `CryptographicOperations.ZeroMemory()` in `finally` blocks
- HMAC keys zeroed in `SlotConfiguration.Dispose()`
- Challenge data zeroed after HMAC operation
- Never log keys, access codes, or challenge data

## Test Infrastructure

### Unit Tests
- NDEF encoding: URI prefix compression, text records, null handling
- Challenge padding: pad-byte-differs-from-last logic
- Slot mapping: all slot × operation combinations
- Config struct assembly: flag encoding, CRC calculation

### Integration Tests
- Use `[WithYubiKey]` attribute with firmware filters
- Touch-triggered tests marked with `[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]`
- Always clean up: delete programmed slots in `finally` blocks
- Use slot 2 for test programming to avoid disrupting slot 1 OTP

## Common Operations

### Program a slot
```csharp
await using var session = await YubiOtpSession.CreateAsync(connection);
using var config = new HmacSha1SlotConfiguration(key);
await session.PutConfigurationAsync(Slot.Two, config);
```

### Challenge-response
```csharp
var response = await session.CalculateHmacSha1Async(Slot.Two, challenge);
```

### Check slot state
```csharp
var state = session.GetConfigState();
bool isConfigured = state.IsConfigured(Slot.One);
```

## Related Modules

- **Core** — `ApplicationSession`, `ISmartCardProtocol`, `IOtpHidProtocol`, `ChecksumUtils`
- **Management** — Reference for backend pattern, session factory, DI extensions
