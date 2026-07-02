# Phase 13 ISA: Core FirmwareVersion / Feature Firmware Gates

## Problem

Firmware sentinel handling is split between `FirmwareVersion.IsAlphaOrBeta` and direct `Major == 0` / `Major != 0` checks. `ApplicationSession.IsSupported(...)` already treats every `Major == 0` firmware as modern, but `FirmwareVersion.IsAlphaOrBeta` currently only treats `0.0.0` as sentinel firmware.

## Scope

- Make `FirmwareVersion.IsAlphaOrBeta` the single source of truth for firmware sentinel handling.
- Treat every `Major == 0` firmware version as alpha/beta/test sentinel firmware.
- Add `Feature.IsSupportedByFirmware(FirmwareVersion firmwareVersion)` for firmware-only feature gates.
- Make `ApplicationSession.IsSupported(Feature)` delegate to `Feature`.
- Replace direct `Major == 0` / `Major != 0` sentinel checks in approved Core and module-local firmware gates.
- Run focused unit verification plus read-only 5.8-key integration smoke tests.

## Out Of Scope

- No FIDO2 SmartCard transport provenance changes; Phase 14 owns `FidoSession.EnsureSmartCardTransportSupported(...)`.
- No hardware tests requiring User Presence, UV, touch, insert/remove, reset, persistent-state mutation, or destructive behavior.
- No public API redesign beyond adding firmware-only support checking on `Feature`.

## Approved Direct Replacements

- `src/Core/src/YubiKey/ApplicationSession.cs`
- `src/Core/src/SmartCard/PcscProtocol.cs`
- `src/Core/src/SmartCard/Scp/ScpInitializer.cs`
- `src/YubiOtp/src/SlotConfiguration.cs`
- `src/YubiOtp/src/ConfigState.cs`
- `src/OpenPgp/src/OpenPgpSession.Config.cs`
- `src/YubiHsm/src/HsmAuthSession.cs`

Deferred to Phase 14:

- `src/Fido2/src/FidoSession.cs` SmartCard transport gate

## Criteria

- `ISC-13.1`: `FirmwareVersion.IsAlphaOrBeta` returns true for every version whose `Major == 0`, including `0.0.0`, `0.0.1`, `0.1.0`, and `0.255.255`.
- `ISC-13.2`: `FirmwareVersion.CompareTo`, `IsAtLeast`, and `IsLessThan` treat all `Major == 0` sentinel versions as latest/modern.
- `ISC-13.3`: `Feature.IsSupportedByFirmware(...)` returns true for sentinel firmware, false below threshold, true at threshold, and true above threshold.
- `ISC-13.4`: `ApplicationSession.IsSupported(...)` delegates to `Feature.IsSupportedByFirmware(...)`.
- `ISC-13.5`: Approved direct `Major == 0` / `Major != 0` firmware gates are replaced with `IsAlphaOrBeta` or `Feature.IsSupportedByFirmware(...)`.
- `ISC-13.6`: FIDO2 direct `Major == 0` transport gate remains intentionally deferred to Phase 14 and is recorded as such.

## Likely Files

- `src/Core/src/YubiKey/FirmwareVersion.cs`
- `src/Core/src/YubiKey/Feature.cs`
- `src/Core/src/YubiKey/ApplicationSession.cs`
- `src/Core/src/SmartCard/PcscProtocol.cs`
- `src/Core/src/SmartCard/Scp/ScpInitializer.cs`
- `src/YubiOtp/src/SlotConfiguration.cs`
- `src/YubiOtp/src/ConfigState.cs`
- `src/OpenPgp/src/OpenPgpSession.Config.cs`
- `src/YubiHsm/src/HsmAuthSession.cs`
- `src/Management/tests/Yubico.YubiKit.Management.UnitTests/FirmwareVersionTests.cs`
- Core/unit tests for `Feature` and `ApplicationSession`
- Module unit tests as needed for touched module-local gates

## Verification

- `dotnet toolchain.cs -- build --project Core`: passed, 0 warnings, 0 errors.
- `dotnet toolchain.cs -- test --project Core`: passed, 327 succeeded, 2 skipped.
- `dotnet toolchain.cs -- test --project Management`: passed, 115 succeeded.
- `dotnet toolchain.cs -- test --project YubiOtp`: passed, 106 succeeded.
- `dotnet toolchain.cs -- test --project OpenPgp`: passed, 88 succeeded.
- `dotnet toolchain.cs -- test --project YubiHsm`: passed, 51 succeeded.
- `dotnet format --verify-no-changes --include <touched files>`: passed after normalizing patched line endings.
- `git diff --check`: passed; git emitted existing line-ending normalization warnings only.
- Direct sentinel grep: only `FirmwareVersion.IsAlphaOrBeta` and deferred `FidoSession` transport gate remain.
- DevTeam cross-vendor review: Vertex Opus 4.8 returned PASS WITH NOTES; the medium equality/hash-contract note was fixed by making `FirmwareVersion.Equals` / `==` use exact `Major.Minor.Patch` identity while keeping sentinel-aware ordering.

## Integration Smoke Scope

Run selected read-only 5.8-key integration checks after unit verification. Approved constraints: no User Presence, no UV, no touch, no insert/remove, no reset, no persistent-state mutation, no destructive behavior.

Candidate smoke tests:

- OATH read-only beta-key metadata checks using `BetaSerial103Filter`.
- SecurityDomain read-only session/SCP creation smoke if existing tests do not mutate Security Domain state.
- Management read-only device info check if an exact non-mutating test is available.

Any candidate requiring reset, credential/key creation, password changes, configuration changes, User Presence, or persistent state is skipped and recorded in the learning note.

Executed read-only 5.8.0 SmartCard integration smoke tests:

- `ManagementSessionTests.GetDeviceInfo_ReturnsValidInformation`: passed, 1 test.
- `YubiOtpSessionIntegrationTests.GetConfigState_ReturnsValidState`: passed, 1 test.
- `OpenPgpSessionTests.GetAlgorithmInformation_ReturnsSupportedAlgorithms`: passed, 1 test.

The filtered integration toolchain commands exit nonzero because they also run the xUnit v3 unit test project with `--minimum-expected-tests 0`, which the xUnit v3 runner rejects before executing any unit test. The integration sections themselves build and pass through `dotnet toolchain.cs -- test --integration --project <module> --filter <read-only-test>`.

## Review

Use `/DevTeam` with active GPT-5.5 primary as Engineer and Vertex Opus 4.8 as cross-vendor Reviewer.
