# CLAUDE.md - YubiOTP Module

This file provides Claude-specific guidance for working with the YubiOTP module.

## Module Context

The YubiOTP module implements the Yubico OTP application for .NET. It is unique among SDK modules because it supports **dual transport**: SmartCard (CCID/NFC) and OTP HID (USB keyboard interface).

**Key Files:**
- [`YubiOtpSession.cs`](src/YubiOtpSession.cs) - Main session class with factory pattern
- [`IYubiOtpSession.cs`](src/IYubiOtpSession.cs) - Public session interface
- [`IYubiOtpBackend.cs`](src/Backend/IYubiOtpBackend.cs) - Internal backend abstraction (2 primitives)
- [`SmartCardBackend.cs`](src/Backend/SmartCardBackend.cs) - APDU-based backend with prog_seq validation
- [`HidBackend.cs`](src/Backend/HidBackend.cs) - Feature report backend with CRC validation
- [`SlotConfiguration.cs`](src/SlotConfiguration.cs) - Abstract base for 52-byte config struct
- [`IYubiKeyExtensions.cs`](src/IYubiKeyExtensions.cs) - C# 14 extension convenience methods

## Architecture: Backend Pattern

YubiOtpSession delegates all transport-specific work to `IYubiOtpBackend`:

```
YubiOtpSession (public API, orchestration)
    |
    +-- IYubiOtpBackend (internal, 2 methods)
        |-- SmartCardBackend (APDUs, prog_seq validation)
        |-- HidBackend (feature reports, data-response prefix CRC validation)
```

Backend has only two primitives:
- `WriteUpdateAsync` — write config/NDEF/swap/delete, returns status bytes
- `SendAndReceiveAsync` — challenge-response, serial read, returns data

The backend borrows the protocol and owns no disposable state, so `IYubiOtpBackend` is deliberately not `IDisposable` (`YubiOtpBackendLifecycleTests` pins this). The session disposes the protocol via `ApplicationSession`; whoever created the connection disposes it — see [Core CLAUDE.md](../Core/CLAUDE.md) gotcha #2.

## Session Initialization

The shared `ApplicationSession` factory binds the session and handles failure cleanup. Its protocol preparation
step rejects SCP on OTP HID before reading status; backend programming-sequence state remains applet-owned.
Static `ReadInitialStateAsync` helpers on each concrete backend perform discovery without constructing a backend.
`IYubiOtpBackend` exposes only operational commands, not initialization.

SmartCard path:
1. SELECT `AID.MANAGEMENT` (for NEO version reliability)
2. SELECT `AID.OTP` → store status bytes
3. Use Management's version when available; when OTP major is 3, use the higher Management/OTP version.
   This Management probe runs for both USB CCID and NFC. Cached ConfigState still exposes raw OTP status.
4. `ConfigureProtocolAsync` → configure protocol, optionally establish SCP
5. Construct the SmartCard backend once with the effective protocol, firmware, and SELECT programming sequence

OTP HID path:
1. `ReadStatusAsync()` → store status bytes
2. Extract firmware version from protocol or status bytes
3. `ConfigureProtocolAsync` → configure protocol
4. Construct the HID backend with the effective protocol

## User-presence notifications

When `SessionCreationOptions.UserPresencePrompt` is supplied, only HMAC-SHA1 and Yubico OTP
challenge-response operations can notify it. OTP HID waits until the device reports the touch-wait flag,
then the Core protocol requests the operation's shared handle once with `DeviceWaiting`; `HidBackend` owns
terminal resolution after response-length and CRC validation. SmartCard has no in-flight wait signal, so
`SmartCardBackend` owns both the `PolicyRequires` request and terminal resolution when cached OTP status
identifies the selected slot as touch-triggered. Unknown and non-touch SmartCard slot state remains silent.
Only expiration of an OTP HID wait after the device reports the touch-wait flag resolves as `TimedOut`;
transport and readiness timeouts, including all SmartCard timeouts, resolve as `Failed`. Challenge-response
output remains backend-owned until successful resolution and is cleared if the callback prevents transfer.
Notification context uses application `YubiOTP` and the slot enum name as its scope; configuration and status
operations never notify. The cached trigger indicator describes keyboard activation, not a read-back of
challenge-response RequireTouch. Treat SmartCard's PolicyRequires as the current implementation heuristic,
not proven policy certainty; unknown/non-triggered state remains silent. Prefer explicit OTP HID for USB
challenge recipes instead of assuming SmartCard command parity across devices/interfaces.

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
- Null content sends an all-zero disable request

Null sends an all-zero disable request, distinct from an empty URI/text configuration; on-tap effects
need target-device verification. URI remainder allows 53 UTF-8 bytes after longest ordinal/case-sensitive
prefix matching; text allows 51 UTF-8 bytes. The block configures an existing compatible slot on NFC hardware,
not a complete NDEF record; legacy URI output can append OTP. There is no NFC read-back or arbitrary language/
UTF-16 option. Undefined enum values are not validated; do not describe the encoder as full URI validation.

## HMAC-SHA1 Challenge Padding

Challenges are padded to 64 bytes using the same encoding as yubikey-manager (ykman).
For a nonempty short challenge, the pad byte differs from its last byte:
- Last byte non-zero → pad with `0x00`
- Last byte zero → pad with `0x01`
- Empty challenge → pad with `0x00`
- Exactly 64 bytes → send unchanged

`UseShortChallenge` is enabled by default. In that mode, the device uses the final
transmitted byte as a marker and removes the trailing run of that value, retaining at
least one byte. Inputs of 1–63 bytes hash as given; a 64-byte input loses at least its
last byte. An all-equal **transmitted payload** hashes as one byte, so empty input
produces HMAC of one `0x00`, not HMAC of an empty message. Repeated bytes in a nonempty
short input are preserved because the padding differs from its last byte.

With `UseShortChallenge(false)`, the device hashes all 64 transmitted bytes. Callers
should pass exactly 64 bytes using their verifier's padding convention. The session
does not know or enforce the slot mode. In either mode, a short input and its explicit
64-byte padded form produce identical requests; use one length convention per slot.

Unit tests in `YubiOtpSessionTests.HmacChallengeSemantics` pin both zero- and one-padding
collisions through the public SmartCard session and the 63/64-byte boundary. Device
marker behavior is checked against host HMAC by `YubiOtpHmacChallengeKnownAnswerTests` when those integration
tests execute. Minimum-one-byte interpretation is not established by wire-only tests or a universal firmware
guarantee; prefer 1–63 bytes in short mode and verify empty/64-byte boundaries on target hardware.

## Security Considerations

- Configurations copy inputs; Dispose clears private fixed/UID/key fields, not source buffers or GetConfig results.
- Caller owns source buffers, every GetConfig array, and successful calculation output. Clear them separately.
- HMAC clears its padded copy; AES borrows the unpadded six-byte input. Keep borrowed inputs stable until await ends.
- Immutable password/CLI strings cannot be deterministically erased. Raw translated scan codes permit mutable-source cleanup.
- Normal WriteConfig cleanup clears assembled blocks, but invalid new/current-code paths allocate secret copies
  before validation/try and can strand them. No-I/O rejection is not an all-path erasure guarantee.
- Never log keys, access codes, or challenge data

## Current implementation constraints

- Updates send complete masked values, not patches. Defaults are SerialApiVisible/AllowUpdate, other update
  flags clear. New code omission sends zeros; repeat current code in both code arguments to retain protection.
- HOTP nonzero IMF divides by 65,536 instead of the vendor 16; recommend zero, not the erroneous accepted range.
- Scan-map API enforces 38 bytes versus the vendor 45-entry map; do not recommend truncation/full-map programming.
- Type/operation minimums do not validate every legacy flag, password length, preset counter, or hardware feature.
- SmartCard write sequence checks bypass 5.0.0 <= effective version < 5.5.0, accept any returned zero, and
  silently accept status shorter than six bytes after fallback. These are limitations, not intended validation.
- SmartCard data reads return the expected prefix, allowing excess data. HID data reads validate CRC over
  expected length plus two bytes and return the expected prefix; HID writes only check status length here.
- OtpTool's static command uses UTF-8 bytes as raw scan codes and ignores layout. Its TOTP flag is not code
  generation, settings are not patches, and generated human output intentionally includes secrets. See README.

## Test Infrastructure

### Unit Tests

`YubiOtpSessionLinuxCrossLayerTests` uses actual Linux slots/`YubiKeyDevice` and the production OTP
owner with fake feature bytes underneath. It pins held initial status without blocking task return,
borrowed sequential sessions, exact serial/CRC framing, touch cancellation held through acknowledged
abort before serial recovery, and completed/failed terminal presence after applet CRC validation.
No real slot is provisioned. The existing internal `OtpHidProtocol.Configure` synchronous initialization
wait remains: public YubiOTP awaits ReadStatus before Configure; the test's exact report queue rejects
extra hidden Configure I/O. See [L6a managed evidence](../../docs/research/linux-managed-acceptance.md).
- NDEF encoding: URI prefix compression, text records, null handling
- Challenge padding: pad-byte-differs-from-last logic
- Slot mapping: all slot × operation combinations
- Config struct assembly: flag encoding, CRC calculation

### Integration Tests
- Use `[WithYubiKey]` attribute with firmware filters
- Touch-triggered tests marked with `[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]`
- Existing tests are destructive: slot-2 programming deletes that slot, swap affects both slots, and the
  protection suite replaces slot 1 as well. Original unreadable secrets are not restored. Some cleanup is not
  in finally; a second swap attempts restoration but is not guaranteed after failure.
- For new tests, prefer dedicated slot 2 and finally cleanup, but inspect the entire fixture and operation
  sequence rather than treating those guidelines as guarantees of the current suite.
- Configured-bit/response-length assertions do not prove keyboard output, digit count, ticket validity,
  NFC read-back, or real-device compatibility. Reflection zeroing tests inspect fields; disposal rejection alone does not.

## Common Operations

### Program a slot

Composable fragment: `IConnection connection` is a caller-owned open SmartCard or OTP HID connection;
`byte[] key` is a caller-owned 20-byte secret. Import System.Security.Cryptography,
Yubico.YubiKit.Core.Abstractions and Yubico.YubiKit.YubiOtp. This task clears key even if session creation fails;
the caller disposes connection after the session scope ends.

```csharp
try
{
    await using var session = await YubiOtpSession.CreateAsync(connection);
    using var config = new HmacSha1SlotConfiguration(key);
    await session.PutConfigurationAsync(Slot.Two, config);
}
finally
{
    CryptographicOperations.ZeroMemory(key);
}
```

### Challenge-response

Composable fragment within an already open `YubiOtpSession session` for a correctly provisioned slot.
`byte[] challenge` is owned by this task. Import System, System.Runtime.InteropServices and
System.Security.Cryptography plus the module namespace. AsMemory below is only for the caller-owned SDK output.

```csharp
ReadOnlyMemory<byte> response = default;
try
{
    response = await session.CalculateHmacSha1Async(Slot.Two, challenge);
    // Consume with the application's verifier.
}
finally
{
    CryptographicOperations.ZeroMemory(challenge);
    CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(response).Span);
}
```

### Check slot state

Composable fragment within an open `YubiOtpSession session` on firmware supporting configured status:

```csharp
var state = session.GetConfigState();
bool isConfigured = state.IsConfigured(Slot.One);
```

## Related Modules

- **Core** — `ApplicationSession`, `ISmartCardProtocol`, `IOtpHidProtocol`, `ChecksumUtils`
- **Management** — Reference for backend pattern and session factories
