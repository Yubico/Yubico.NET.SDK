# Phase 14 ISA: FIDO2 SmartCard Transport Provenance

## Problem

FIDO2 SmartCard transport gating currently depends on `ISmartCardConnection.Transport`, but Core PC/SC discovery has been hardcoding YubiKey SmartCard devices as USB. That makes the FIDO2 NFC exemption only as good as the upstream connection-kind evidence.

Phase 14 must clarify current connection transport without confusing it with YubiKey NFC capability. NFC capability means a device/model/app may support NFC. Transport means this specific PC/SC connection is currently USB or NFC.

## Vision

FIDO2 SmartCard support becomes source-backed and boring: Core classifies the current PC/SC connection transport from ATR evidence, FIDO2 gates only the USB SmartCard case by firmware, and docs/tests stop implying that "NFC-capable YubiKey" equals "current NFC transport."

## Out of Scope

- No inference from YubiKey model name, form factor, or management NFC capability to current NFC transport.
- No public API redesign.
- No broad platform reader-management rewrite beyond the transport classification needed here.
- No User Presence, UV, touch, reset, PIN normalization, credential creation, or destructive integration tests without explicit approval.
- No unattended integration test run without first telling the user exactly what will run.

## Principles

- Current connection transport is runtime evidence, not device capability metadata.
- ATR-based transport classification is stronger than model/capability inference; the adapted `yubikey-manager` rule is USB when ATR byte 1 has high nibble `0xF?`, otherwise NFC for discovered YubiKey PC/SC cards.
- Firmware-only predicates belong in `Feature`.
- Transport, AID exposure, and runtime session facts stay local to FIDO2 session logic.
- `yubikey-manager` is a reference model for detection strategy, not a copy-paste target.

## Constraints

- Use `dotnet toolchain.cs ...`; never raw `dotnet test`.
- Preserve Phase 13 firmware sentinel semantics.
- Invalid or too-short ATRs must classify as `Unknown`; `Unknown` or `Any` connection kind must map to a non-NFC transport value and must not bypass the USB SmartCard FIDO2 firmware gate.
- The ATR classifier must be a named, separately testable Core function; inline LINQ classification is not enough because invalid ATR paths are filtered before discovery results are emitted.
- `FindPcscDevices` must assign the classifier result into each emitted `PcscDevice.Kind`; leaving `Kind` unset silently defaults to `PscsConnectionKind.Any` and weakens provenance even though it fails closed.
- Integration tests are saved for last and must be announced before execution.

## Goal

FIDO2 SmartCard transport gating uses source-backed current-connection transport evidence. NFC SmartCard FIDO2 remains allowed when the current PC/SC connection is actually NFC; USB SmartCard FIDO2 requires firmware `5.8.0+` or sentinel firmware via the Phase 13 firmware predicate.

## Criteria

- [ ] ISC-14.1: `FindPcscDevices` no longer hardcodes every YubiKey PC/SC device as `PscsConnectionKind.Usb`.
- [ ] ISC-14.2: ATRs whose second byte has high nibble `0xF?` classify as `PscsConnectionKind.Usb`.
- [ ] ISC-14.3: Discovered YubiKey ATRs whose second byte does not have high nibble `0xF?` classify as `PscsConnectionKind.Nfc`.
- [ ] ISC-14.4: Too-short or missing ATR classification returns `PscsConnectionKind.Unknown`, not a guessed NFC or USB kind.
- [ ] ISC-14.5: Classification uses current ATR/connection evidence, not management NFC capability.
- [ ] ISC-14.5a: The ATR classifier is a named Core function that can be unit-tested without opening a PC/SC connection.
- [ ] ISC-14.5b: `FindPcscDevices` assigns the classifier output to the emitted `PcscDevice.Kind` property.
- [ ] ISC-14.5c: Tests cover omitted/default `PcscDevice.Kind` as `PscsConnectionKind.Any` and verify it fails closed through transport mapping.
- [ ] ISC-14.6: `ISmartCardConnection.Transport` reflects `IPcscDevice.Kind`.
- [ ] ISC-14.7: `PscsConnectionKind.Nfc` maps to `Transport.Nfc`.
- [ ] ISC-14.8: `PscsConnectionKind.Usb` maps to `Transport.Usb`.
- [ ] ISC-14.9: `PscsConnectionKind.Unknown` and `PscsConnectionKind.Any` map conservatively to `Transport.Usb`, not `Transport.Nfc`.
- [ ] ISC-14.10: FIDO2 USB SmartCard gate uses a `Feature` firmware predicate for `5.8.0+`.
- [ ] ISC-14.10a: `FidoSession.EnsureSmartCardTransportSupported(...)` preserves the NFC success branch before evaluating the USB firmware `Feature` predicate.
- [ ] ISC-14.11: FIDO2 contains no direct `firmwareVersion.Major == 0` sentinel check.
- [ ] ISC-14.12: USB SmartCard with firmware `5.7.2` still throws `NotSupportedException`.
- [ ] ISC-14.13: USB SmartCard with firmware `5.8.0` succeeds.
- [ ] ISC-14.14: USB SmartCard with sentinel firmware `0.x` succeeds through `Feature.IsSupportedByFirmware(...)`.
- [ ] ISC-14.15: NFC SmartCard with firmware below `5.8.0` succeeds.
- [ ] ISC-14.16: FIDO2 docs distinguish NFC capability from current NFC transport.
- [ ] ISC-14.17: Phase 14 ISA records `yubikey-manager` inspiration and the adapted ATR transport rule.
- [ ] ISC-14.18: Discovery matching may use `ProductAtrs.AllYubiKeys`, but kind classification uses ATR byte evidence rather than model, form factor, or management NFC capability.
- [ ] ISC-14.19: `UsbSmartCardConnection.Transport` consumes the already-carried `IPcscDevice.Kind` instead of returning hardcoded `Transport.Usb`.
- [ ] ISC-14.19a: `UsbSmartCardConnection.Transport` is valid before `InitializeAsync(...)` so unit tests can verify mapping without real PC/SC hardware.
- [ ] ISC-14.20: `PscsConnectionKind` to `Transport` mapping uses an explicit switch, not numeric casts or enum value assumptions.
- [ ] ISC-14.21: Unit verification passes before any hardware integration tests run.
- [ ] ISC-14.22: Final integration verification is explicitly user-coordinated and runs only read-only SmartCard FIDO2 checks unless broader hardware interaction is approved.

## Test Strategy

| ISC | Type | Check | Tool |
| --- | --- | --- | --- |
| ISC-14.1 | grep | No hardcoded `Kind = PscsConnectionKind.Usb` in `FindPcscDevices` | `grep` |
| ISC-14.2 | unit | ATR byte 1 high nibble `0xF?` returns USB kind | `dotnet toolchain.cs -- test --project Core --filter ...` |
| ISC-14.3 | unit | Discovered YubiKey ATR byte 1 high nibble not `0xF?` returns NFC kind | `dotnet toolchain.cs -- test --project Core --filter ...` |
| ISC-14.4 | unit | Missing/too-short ATR returns `PscsConnectionKind.Unknown` | Core unit test |
| ISC-14.5 | inspection | Classifier takes ATR/current connection evidence, not `DeviceInfo` capability | `read` / `grep` |
| ISC-14.5a | unit | Named classifier can be called directly with synthetic ATRs | Core unit test |
| ISC-14.5b | read/unit | `FindPcscDevices` assigns classifier result to `PcscDevice.Kind` | `read` / Core unit test |
| ISC-14.5c | unit | Default `PcscDevice.Kind` (`Any`) maps to USB transport | Core unit test |
| ISC-14.6 | unit | Fake `IPcscDevice.Kind` propagates to connection transport | Core unit test |
| ISC-14.7 | unit | NFC kind maps to NFC transport | Core unit test |
| ISC-14.8 | unit | USB kind maps to USB transport | Core unit test |
| ISC-14.9 | unit | Unknown and Any map to USB transport | Core unit test |
| ISC-14.10 | inspection | FIDO2 USB SmartCard gate calls `Feature.IsSupportedByFirmware(...)` | `grep` |
| ISC-14.10a | read/unit | NFC branch returns before USB firmware predicate | `read` / FIDO2 unit test |
| ISC-14.11 | grep | No FIDO2 direct `Major == 0` sentinel remains | `grep` |
| ISC-14.12 | unit | USB `5.7.2` throws | FIDO2 unit test |
| ISC-14.13 | unit | USB `5.8.0` succeeds | FIDO2 unit test |
| ISC-14.14 | unit | USB `0.x` sentinel succeeds | FIDO2 unit test |
| ISC-14.15 | unit | NFC pre-5.8 succeeds | FIDO2 unit test |
| ISC-14.16 | docs | Docs separate capability from active transport | `read` |
| ISC-14.17 | docs | ISA cites `yubikey-manager` ATR model | `read` |
| ISC-14.18 | inspection/unit | Discovery uses `AllYubiKeys`; classification uses ATR byte evidence only | `read` / Core unit test |
| ISC-14.19 | unit | `UsbSmartCardConnection.Transport` reads `IPcscDevice.Kind` | Core unit test |
| ISC-14.19a | unit | Transport getter works on uninitialized connection instance | Core unit test |
| ISC-14.20 | inspection/unit | Kind-to-transport mapping is explicit switch, not enum cast | `read` / Core unit test |
| ISC-14.21 | command | Core/FIDO2 unit tests pass before integration | toolchain |
| ISC-14.22 | integration | User-approved read-only SmartCard GetInfo tests pass/skip with reason | toolchain |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Core ATR classifier | Add a named pure Core classifier and wire its output into `FindPcscDevices` emitted `PcscDevice.Kind` values | ISC-14.1-14.5c, ISC-14.18 | none | false |
| SmartCard transport propagation | Make `UsbSmartCardConnection.Transport` consume `IPcscDevice.Kind` through an explicit switch | ISC-14.6-14.9, ISC-14.19-14.20 | Core ATR classifier | false |
| FIDO2 firmware gate cleanup | Replace local sentinel logic with Phase 13 `Feature` predicate while preserving NFC short-circuit ordering | ISC-14.10-14.15 | none | true |
| Docs and ISA update | Record capability-vs-transport distinction and yubikey-manager inspiration in `src/Fido2/CLAUDE.md`, `src/Fido2/tests/CLAUDE.md`, `src/Fido2/README.md`, and this ISA | ISC-14.16-14.17 | Core/FIDO2 decisions | true |
| Verification and integration coordination | Run unit tests first, then announced read-only integration tests last | ISC-14.21-14.22 | implementation | false |

## Decisions

- Current connection transport must be detected from runtime PC/SC evidence, not inferred from YubiKey NFC capability.
- `yubikey-manager` informs the detection strategy: USB PC/SC ATRs have second byte high nibble `0xF?`; non-USB PC/SC ATRs are NFC. Phase 14 adapts this as an ATR-byte classifier for discovered YubiKey PC/SC cards.
- The .NET repo already has `ProductAtrs` with USB ATRs like `3B-FC`, `3B-F8`, `3B-FD` and NFC ATRs like `3B-8C`, `3B-8D`.
- Discovery and classification are separate operations: discovery may continue matching `ProductAtrs.AllYubiKeys`, while kind classification must use ATR byte evidence and must not infer from model, form factor, or management NFC capability.
- Unknown transport should fail closed for FIDO2 SmartCard gating by not being treated as NFC; `PscsConnectionKind.Unknown` and `PscsConnectionKind.Any` map to `Transport.Usb` until stronger evidence exists.
- `PcscDevice.Kind` is `init` but not `required`, so omitted `Kind` defaults to `PscsConnectionKind.Any`; tests must cover that silent default and the implementation must still fail closed.
- The implementation may keep the `UsbSmartCardConnection` type name in Phase 14 if changing it would expand public or internal churn; the correctness requirement is that `src/Core/src/SmartCard/UsbSmartCardConnection.cs` changes its `Transport` getter to reflect the carried `IPcscDevice.Kind`.
- `PscsConnectionKind` and `Transport` numeric values do not align; the implementation must use an explicit switch and must not cast between these enums.
- FIDO2 must add an explicit firmware feature for USB SmartCard support, for example `FeatureFido2UsbSmartCard = new("FIDO2 over USB SmartCard", 5, 8, 0)`, and use `Feature.IsSupportedByFirmware(...)`.
- The FIDO2 gate must keep `transport == Transport.Nfc` as the first successful branch. The new `Feature` predicate applies only after the connection is known not to be NFC.
- The Core classifier should live near PC/SC discovery, likely in `src/Core/src/SmartCard/PcscConnectionKindDetector.cs`, so `FindPcscDevices` calls it and unit tests can exercise synthetic ATRs directly.
- `FindPcscDevices` must not merely stop hardcoding USB; it must set `Kind = PcscConnectionKindDetector.Detect(reader.GetAtr())` or equivalent on each emitted `PcscDevice`.

## Verification

Planned commands:

- `dotnet toolchain.cs -- build --project Core`
- `dotnet toolchain.cs -- build --project Fido2`
- `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~<new classifier tests>"`
- `dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~FidoSessionTests"`
- `dotnet toolchain.cs -- test --project Fido2`
- `dotnet format --verify-no-changes --include <touched files>`
- `git diff --check`

Final integration verification, only after unit verification passes and after user coordination:

- `FidoSessionSimpleTests.CreateFidoSession_With_SmartCard_CreateAsync`
- `FidoSmartCardTests.GetInfo_Over_SmartCard_ReturnsValidFido2Version`
- `FidoSmartCardTests.GetInfo_Over_SmartCard_ReturnsSupportedAlgorithms`

Integration constraints:

- SmartCard/CCID required.
- Read-only `GetInfoAsync` only.
- No touch.
- No PIN.
- No UV.
- No reset.
- No credential creation.
- Avoid default `WithFidoSessionAsync(...)` unless `normalizePin: false`.
