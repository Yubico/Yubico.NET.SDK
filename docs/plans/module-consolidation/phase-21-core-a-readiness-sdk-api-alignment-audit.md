# Phase 21 Audit: Core A- Readiness And SDK-Family API Alignment

## Scope

Phase 21 audited and repaired the Core setup surface named by the Phase 19/20 handoff:

- current Core DI/logging documentation
- applet dependency-injection XML comments that claimed a removed Core prerequisite
- duplicate Core CRC13239/checksum utilities
- public device/discovery concepts compared against Swift, Python, and Android references

This audit intentionally does not design or implement composite YubiKey discovery.

## Core DI Documentation Drift

### Source Reality

- `src/Core/src/YubiKey/YubiKeyManager.cs` is a static API and explicitly says no dependency injection is required.
- `src/Core/src/YubiKitLogging.cs` exposes `YubiKitLogging.Configure(ILoggerFactory?)` for explicit startup logging setup.
- Grep found no current `src/**` implementation of `AddYubiKeyManagerCore()`.
- Applet DI extensions such as `AddOath()`, `AddOpenPgp()`, `AddYubiOtp()`, `AddYubiKeySecurityDomain()`, `AddHsmAuth()`, `AddYubiKeyFido2()`, and `AddYubiKeyManager()` register module-local session factory delegates.

### Repair Applied

- Root agent guidance now says Core discovery uses static `YubiKeyManager` and no Core DI registration is required.
- `src/Core/README.md`, `src/Core/CLAUDE.md`, and `docs/LOGGING.md` now direct logging setup through `YubiKitLogging.Configure(...)`.
- Applet DI XML comments now say each extension registers only its module session factory or factory delegates for callers that use dependency injection.
- Security Domain test docs now show only `AddYubiKeySecurityDomain()` for the DI factory path.

### Historical References Left Alone

Historical specs, completed design notes, research notes, and old phase artifacts still mention `AddYubiKeyManagerCore()` because they describe earlier designs or migrations. Current active source/docs should not use those as setup instructions.

## Duplicate CRC13239 / Checksum Utilities

### Evidence

- `src/Core/src/Utils/Crc13239.cs` exposes `Crc13239.Calculate(ReadOnlySpan<byte>)` and returns `short`.
- `src/Core/src/Hid/Otp/ChecksumUtils.cs` exposes `ChecksumUtils.CalculateCrc(ReadOnlySpan<byte>, int)`, `ChecksumUtils.CheckCrc(...)`, and `ChecksumUtils.ValidResidue`.
- `src/Core/src/Hid/Otp/OtpHidProtocol.cs`, `src/YubiOtp/src/OtpHidBackend.cs`, and YubiOTP tests use `ChecksumUtils`.
- Both implementations use the CRC13239/YubiKey polynomial `0x8408` and initial value `0xFFFF`.

### Disposition

Do not consolidate in Phase 21.

Reason:

- `ChecksumUtils` is public under the Core HID OTP namespace and has verification/residue helpers that `Crc13239` does not expose.
- `Crc13239.Calculate(...)` returns `short`; `ChecksumUtils.CalculateCrc(...)` returns `ushort` and accepts a length parameter.
- Removing or reshaping either helper would be a public API decision, not a silent quality cleanup.

Recommended future action:

- In a future approved API-shape phase, choose one canonical public CRC13239 helper shape.
- If compatibility does not matter yet, prefer a single Core utility that exposes calculate and residue-check APIs with unsigned return type and span slicing instead of a separate length parameter.
- Keep OTP HID behavior tests as the gate for any consolidation.

## SDK-Family Public API Alignment

### .NET Current Surface

- `YubiKeyManager` is static discovery/monitoring/cache lifecycle.
- `IYubiKey` exposes `DeviceId`, `ConnectionType`, and `ConnectAsync<TConnection>()`.
- `ConnectionType` is a `[Flags]` enum where `Hid` is a group filter, while `HidFido`, `HidOtp`, and `SmartCard` are concrete interface types.
- Current USB enumeration returns per-interface `IYubiKey` instances: `PcscYubiKey` for PC/SC, `HidYubiKey` for HID FIDO/OTP.
- Repository identity currently keys on `IYubiKey.DeviceId`, for example `pcsc:{readerName}` or `hid:{readerName}:{usage}`.

### Python Reference

Sampled files:

- `../yubikey-manager/packages/yubikit/yubikit/core/device.py`
- `../yubikey-manager/packages/yubikit/yubikit/device.py`

Relevant concepts:

- `YubiKeyDevice` is a device reference with `transport`, `pid`, `name`, `info`, `fingerprint`, `supports_connection(...)`, and `open_connection(...)`.
- `fingerprint` is explicitly an identity value for deciding whether references from different enumerations represent the same physical YubiKey, but it is not stable after unplug/replug.
- Native enumeration can expose one device reference that supports multiple connection types.

Alignment observation:

- .NET already has the generic open-connection shape through `ConnectAsync<TConnection>()`.
- .NET lacks a public `supports connection` method and lacks explicit public `fingerprint`, `pid`, `name`, or `DeviceInfo` on the Core device abstraction.
- Do not add those now; record them for the composite YubiKey owner interview.

### Android Reference

Sampled files:

- `../yubikit-android/core/src/main/java/com/yubico/yubikit/core/YubiKeyDevice.java`
- `../yubikit-android/desktop/src/main/java/com/yubico/yubikit/desktop/CompositeDevice.java`
- `../yubikit-android/desktop/src/main/java/com/yubico/yubikit/desktop/UsbYubiKeyDevice.java`

Relevant concepts:

- `YubiKeyDevice` is documented as a reference to a physical YubiKey.
- It exposes `getTransport()`, `supportsConnection(Class<?>)`, `requestConnection(...)`, and `openConnection(...)`.
- Desktop has `CompositeDevice` that delegates supported connections through a `UsbPidGroup` and key.
- `UsbYubiKeyDevice` adds `getFingerprint()` and `getPid()`.

Alignment observation:

- .NET's `IYubiKey` is currently closer to a per-interface handle than Android's physical-device abstraction.
- Android strongly supports the later composite direction, but Phase 20 requires owner interviews before .NET chooses that shape.

### Swift Reference

Sampled files under `../yubikit-swift/YubiKit/YubiKit` show a connection/session-oriented style:

- `Session/SmartCard/SmartCardSession.swift`
- `Session/SmartCard/SmartCardInterface.swift`
- `Management/ManagementSession.swift`

Relevant concepts:

- Sessions and interfaces are central.
- Management can work over SmartCard and FIDO interfaces.
- The sampled public surface is less about desktop physical-device aggregation and more about typed connections/interfaces.

Alignment observation:

- .NET's session and connection-first style is not out of family for Swift.
- The future .NET composite design must balance Android/Python physical-device patterns with Swift-like typed connection/session clarity.

## Package Compatibility Policy

Package validation remains audit-only. Phase 21 made documentation/XML-comment repairs and recorded API risks, but did not freeze a historical .NET package baseline or enable a release gate.

## Composite Stop Gate

Phase 21 records these tensions for later owner interviews only:

- whether `IYubiKey` should remain per-interface or become a physical-device abstraction
- whether to add `SupportsConnection(...)` or richer identity properties
- how to expose fingerprint/PID/device-info concepts without false NFC/USB aggregation

No composite YubiKey design decision is made here.

## Phase 21 Result

Core readiness improves because the current setup story no longer points users and agents at removed Core DI infrastructure. Core is not yet `A-` solely from this phase because checksum consolidation and composite-device API shape remain intentional future decisions.
