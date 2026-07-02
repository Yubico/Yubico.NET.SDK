# Phase 12 ISA: Core ConnectionType Semantics

## Problem

`ConnectionType` is marked with `[Flags]`, but its implicit enum values make `HidOtp` equal to `Hid | HidFido`. That makes flag checks misleading and allows filtering behavior to depend on accidental numeric overlap.

## Scope

- Repair `ConnectionType` as true flags with explicit numeric values.
- Define `ConnectionType.Hid` as a group filter for HID interfaces.
- Preserve specific discovered device values as `HidFido`, `HidOtp`, and `SmartCard`.
- Add Core unit tests for enum values, filter semantics, repository filtering, and HID interface mapping.
- Inspect `Transport` and leave it unchanged unless evidence shows a defect.

## Out Of Scope

- No applet/module refactors.
- No CLI selector redesign.
- No integration tests requiring hardware state changes.
- No changes to `Transport` beyond source-backed documentation in the learning note.

## Approved Direction

Use explicit `ConnectionType` flag values:

- `Unknown = 0`
- `Hid = 1`
- `HidFido = 2`
- `HidOtp = 4`
- `SmartCard = 8`
- `All = Hid | HidFido | HidOtp | SmartCard`

The human approved the public numeric value change for `HidOtp`, `SmartCard`, and `All` because this is v2/pre-release consolidation and the current `[Flags]` values are invalid.

## Criteria

- `ISC-12.1`: `ConnectionType` members have explicit power-of-two flag values except `Unknown` and `All`.
- `ISC-12.2`: `ConnectionType.Hid` matches both HID FIDO and HID OTP discovered devices.
- `ISC-12.3`: `ConnectionType.HidFido`, `ConnectionType.HidOtp`, and `ConnectionType.SmartCard` match only their concrete transport/interface.
- `ISC-12.4`: `ConnectionType.Unknown` matches no devices.
- `ISC-12.5`: `FindYubiKeys` scans HID when the filter includes `Hid`, `HidFido`, or `HidOtp`, and scans SmartCard when the filter includes `SmartCard`.
- `ISC-12.6`: `YubiKeyDeviceRepository.GetAll(...)` uses the same filter semantics as discovery.
- `ISC-12.7`: `ConnectionTypeMapper` maps HID interfaces to concrete public connection types and recognizes group filters.
- `ISC-12.8`: `Transport` is inspected and recorded as unchanged because its `[Flags]` values are already valid.

## Likely Files

- `src/Core/src/YubiKey/ConnectionType.cs`
- `src/Core/src/YubiKey/FindYubiKeys.cs`
- `src/Core/src/YubiKey/YubiKeyDeviceRepository.cs`
- `src/Core/src/Hid/ConnectionTypeMapper.cs`
- `src/Core/src/YubiKey/YubiKeyManager.cs`
- `src/Core/src/YubiKey/IYubiKeyDeviceRepository.cs`
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/YubiKey/ConnectionTypeTests.cs`
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Hid/ConnectionTypeMapperTests.cs`
- existing Core device repository/manager tests as needed

## Verification

- `dotnet toolchain.cs build --project Core`
- `dotnet toolchain.cs -- test --project Core`
- `dotnet format --verify-no-changes --include <touched files>`
- `git diff --check`

## Integration Scope

Skipped by default. This phase changes enum/filter semantics and can be proven with Core unit tests. No hardware behavior, applet state, User Presence, UV, touch, insert/remove, reset, or persistent-state mutation is required.

## Review

Use `/DevTeam` with the active GPT-5.5 primary as Engineer and Vertex Opus 4.8 as cross-vendor Reviewer.
