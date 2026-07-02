# Phase 22 Audit: Tests.Shared Recorder And Harness Decision

## Scope

Phase 22 evaluated whether repeated module-local fake SmartCard recorder patterns justify a shared `Tests.Shared` helper.

This audit covers:

- current Tests.Shared harness shape
- repeated private `RecordingSmartCardConnection` implementations
- xUnit v2/v3 dependency compatibility
- hardware safety boundaries

This audit does not change integration-test hardware policy or composite YubiKey design.

## Current Tests.Shared Harness Shape

Tests.Shared already owns integration-test hardware concerns:

- `[WithYubiKey]` returns placeholders during xUnit discovery and binds real authorized devices during execution.
- `YubiKeyTestInfrastructure` discovers devices lazily through static `YubiKeyManager`.
- `AllowList` remains the safety boundary for authorized hardware.
- `SharedSmartCardConnection` is a non-owning integration helper for reset-then-test flows that need one physical SmartCard connection.

Those pieces are not changed by Phase 22.

## Recorder Duplication Evidence

Three active unit-test files contained nearly identical private SmartCard recorder implementations:

| Module | File | Evidence |
| --- | --- | --- |
| PIV | `src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/PivSessionTests.cs` | private `RecordingSmartCardConnection`, queued responses, `TransmittedCommands`, USB SmartCard metadata, no extended APDU |
| OATH | `src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/OathSessionTests.cs` | same queue-and-record shape for chained-response APDU assertions |
| SecurityDomain | `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecurityDomainSessionTests.cs` | same queue-and-record shape for GlobalPlatform APDU assertions |

The repeated behavior is small and stable:

- enqueue raw response APDUs
- record transmitted raw command APDUs in order
- throw when a command is transmitted without a queued response
- report `Transport.Usb` and `ConnectionType.SmartCard`
- return a no-op transaction
- report `SupportsExtendedApdu() == false`

Core unit tests also have `FakeSmartCardConnection` and `FakeApduProcessor` helpers for Core protocol-pipeline testing. Those are intentionally left alone because they are Core-internal, use different APIs such as `QueueResponse` / `SentCommands`, and are not duplicate module-session recorder copies.

## Decision

Promote a narrow `RecordingSmartCardConnection` to `Tests.Shared` and adopt it in PIV, OATH, and SecurityDomain unit tests.

Rationale:

- The repetition exists in three active module unit-test files, not just a planned future PIV phase.
- The helper is xUnit-free and SmartCard-only, so it does not pull test-runner concepts into APDU assertions.
- The helper makes Phase 23+ byte-level applet coverage easier without introducing a fake protocol framework.

Rejected alternatives:

- Leave private copies: preserves locality but keeps repeated mechanics in every module that adds byte-level SmartCard tests.
- Build a fluent APDU recorder/assertion DSL: too much abstraction and would hide bytes that module tests should keep visible.
- Move integration `[WithYubiKey]` behavior: out of scope and unnecessary.

## Dependency Compatibility Finding

Initial adoption by xUnit v3 unit-test projects exposed the expected package conflict: `Tests.Shared` carried xUnit v2 dependencies transitively for integration attribute support, and unit tests already reference `xunit.v3`.

Fix applied:

- `xunit.core`, `xunit.abstractions`, and `Xunit.SkippableFact` are now `PrivateAssets="all"` in `src/Tests.Shared/Yubico.YubiKit.Tests.Shared.csproj`.

This keeps xUnit v2 extensibility available inside `Tests.Shared` while preventing it from flowing into xUnit v3 unit-test consumers. Integration test projects still compile because they directly reference xUnit v2.

## Applied Changes

- Added `src/Tests.Shared/RecordingSmartCardConnection.cs`.
- Updated Tests.Shared README/CLAUDE guidance with the recorder's unit-test-only scope.
- Added explicit `Tests.Shared` project references to PIV, OATH, and SecurityDomain unit-test projects.
- Replaced private module-local recorder classes in the three affected unit-test files.

## Hardware Safety Boundary

Phase 22 does not change:

- `AllowedSerialNumbers` allow-list behavior
- `Environment.Exit(-1)` hard-fail policy for invalid allow-list inputs
- lazy device binding through `YubiKeyTestState`
- `[WithYubiKey]` discovery/execution behavior
- `RequiresUserPresence`, `Slow`, or integration smoke-filter guidance

`RecordingSmartCardConnection` is explicitly documented as a byte-level unit-test helper, not an integration-test hardware abstraction.

## Phase 22 Result

Tests.Shared retains or improves its `B+` posture: the shared harness remains safe for integration tests, while repeated byte-level SmartCard unit-test recorder mechanics are now centralized in one small helper.
