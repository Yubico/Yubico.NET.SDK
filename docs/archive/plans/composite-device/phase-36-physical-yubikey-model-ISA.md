# Phase 36 ISA: Physical YubiKey Model

This phase reshapes Core `IYubiKey` from a single-interface connection handle into a physical-device abstraction: it exposes the set of available connections, a support predicate, a safe default-connect behavior, and typed connection routing, and it removes the scalar `IYubiKey.ConnectionType` that assumed one interface per device. It does not merge per-interface discoveries into one device, populate device info, or redesign applet extension transport selection (those are Phases 37 and 38).

Read this together with:

- `docs/plans/composite-device/ISA.md`
- `docs/plans/composite-device/phase-35-core-device-info-reader-learnings.md`
- `src/Core/CLAUDE.md`
- `src/Fido2/CLAUDE.md`
- `../yubikey-manager` on branch `experiment/rust`

## Problem

`IYubiKey` currently models one interface, not one physical key: it exposes a scalar `ConnectionType ConnectionType { get; }` (exactly one of `SmartCard`, `HidFido`, `HidOtp`) and a default `ConnectAsync()` that switches on that scalar. Production code routes on the scalar (`FindYubiKeys`, `YubiKeyDeviceRepository` filtering, CLI selectors/prompts, and the FIDO2 extension's `yubiKey.ConnectionType switch`). A real USB YubiKey exposes several interfaces, so the scalar cannot describe a physical device, and any code that assumes "one interface per `IYubiKey`" blocks the composite-device model.

The matching helper `ConnectionTypeExtensions.MatchesDevice` is also written for a scalar discovered interface; its `HasFlag(set)` logic is wrong once a device reports a combined capability set.

## Vision

`IYubiKey` describes a physical key: which connections it exposes (`AvailableConnections`), whether it supports a given connection (`SupportsConnection`), how to open a specific typed connection (`ConnectAsync<TConnection>()`), and a safe default-connect that never silently picks a surprising transport. The scalar per-interface `ConnectionType` is gone. Discovery still returns one `IYubiKey` per discovered interface in this phase; merging them into one physical device and reading/caching `DeviceInfo` is Phase 37.

## Out of Scope

- No composite discovery merge, repository physical-identity keying, or add/remove event reshaping (Phase 37).
- No device-info read method, cached `DeviceInfo`, or `FirmwareVersion` property on `IYubiKey`, and no new `DeviceInfoReader` overload. Exposing device info on the physical device (and eager discovery population) is deferred to Phase 37.
- No applet extension smart-default redesign or explicit override API (Phase 38); the FIDO2 extension gets only the minimal mechanical migration required to compile and preserve single-interface behavior, with no multi-transport preference introduced.
- No change to parameterless-`ConnectAsync()` consumers beyond the Core default behavior itself; applet extensions that call `yubiKey.ConnectAsync()` are recorded as deferred default-connect consumers for Phase 38.
- No mutating Management operations move into Core; no new Core dependency on Management.

## Principles

- `IYubiKey` is a physical key, not an interface handle.
- The scalar `ConnectionType` is removed, not repurposed, because a hint invites the same one-interface routing assumption this phase eliminates.
- `AvailableConnections` holds only concrete openable connect bits (`SmartCard`, `HidFido`, `HidOtp`); it never stores the `Hid` group flag or `All`.
- The filter-matching helper must be corrected to compare a requested filter against a combined capability SET, not a scalar interface, before any multi-connection device exists.
- Raw/default connect must be explicit: resolve only when unambiguous, fail clearly when ambiguous.
- Behavior parity for single-interface devices: in this phase each implementation still backs exactly one interface, so observable discovery/connect behavior must not regress, and no multi-transport policy is baked in.

## Constraints

- Execute on branch `yubikit-composite-device-new`.
- Decide and record the scalar `IYubiKey.ConnectionType` disposition in this ISA before any source edit (see Decisions).
- This is a broad API-boundary phase: a Cato review of this ISA is required before source edits. While GPT-5.5 is rate-limited, run the interim opposite-family Cato review via `scripts/interim-cross-vendor-review.sh` (GPT-5.4, high reasoning) and queue the GPT-5.5/Cato review.
- Use the `/DevTeam` review/fix workflow after implementation; interim GPT-5.4 reviewer is acceptable with the GPT-5.5 review queued. Record review output or waiver before commit.
- Use `dotnet toolchain.cs`; never raw `dotnet build` or `dotnet test`.
- Commit only intended files after the learning note and verification are complete.
- Do not introduce a `Core` reference to `Management`.

## Goal

Replace scalar `IYubiKey.ConnectionType` with `AvailableConnections` plus a `SupportsConnection` predicate, add a safe default `ConnectAsync()`, correct the set-based filter-matching helper, keep typed `ConnectAsync<TConnection>()` behavior, migrate every production scalar call site (libraries, CLI, and example tools) and all test doubles, prove no production code routes on a scalar per-interface connection type off an `IYubiKey`, cover the new behavior (including multi-bit cases) with unit tests, verify the full solution, run interim Cato and DevTeam reviews with the GPT-5.5 reviews queued, write a learning note, and commit Phase 36 only.

## Criteria

- [ ] ISC-1: Branch check shows `## yubikit-composite-device-new` before source edits, review delegation, build/test, or commit.
- [ ] ISC-2: `../yubikey-manager` reference branch is confirmed as `experiment/rust` before citing Rust composite-device behavior.
- [ ] ISC-3: This ISA records the explicit scalar `IYubiKey.ConnectionType` disposition before source edits.
- [ ] ISC-4: Interim Cato review of this ISA runs before source edits and returns pass or all concerns are resolved; the GPT-5.5 Cato review is queued.
- [ ] ISC-5: `IYubiKey` no longer exposes a scalar per-interface `ConnectionType` property.
- [ ] ISC-6: `IYubiKey` exposes `AvailableConnections` (a `ConnectionType` flags value holding only concrete connect bits) describing the connections the device exposes.
- [ ] ISC-7: `IYubiKey` exposes a `SupportsConnection(ConnectionType)` predicate with defined semantics: `true` only when the argument is a concrete openable type (`SmartCard`, `HidFido`, `HidOtp`) present in `AvailableConnections`; `Hid` means "`HidFido` or `HidOtp` present"; `Unknown`, `All`, and any other multi-bit combination return `false`.
- [ ] ISC-7.1: The filter-matching helper (used by discovery/repository and `ConnectionTypeMapper.SupportsConnectionType`) matches a requested filter against a combined `AvailableConnections` SET: a device matches if it shares any requested concrete connect bit (with `Hid` expanded to `HidFido|HidOtp`, and `All` matching any non-empty capability set). It does not use `flags.HasFlag(set)` semantics that break for multi-bit sets. The fix is verified across all current helper consumers (`FindYubiKeys`, `YubiKeyDeviceRepository`, `ConnectionTypeMapper`).
- [ ] ISC-8: Typed `ConnectAsync<TConnection>()` routes to the requested concrete interface when available and throws `NotSupportedException` (message naming the unsupported connection type) when unsupported.
- [ ] ISC-9: The parameterless default `ConnectAsync()` resolves deterministically only when exactly one connection is available; it throws `NotSupportedException` when none are available and `InvalidOperationException` (message containing "multiple"/"ambiguous") when the device exposes multiple connections (no silent surprising-transport selection).
- [ ] ISC-10: `PcscYubiKey` and `HidYubiKey` implement the new shape; single-interface `AvailableConnections` values are correct (`SmartCard`; `HidFido`/`HidOtp`).
- [ ] ISC-11: `SupportsConnection` and the default `ConnectAsync()` are default-interface members on `IYubiKey` defined in terms of `AvailableConnections`, so implementers supply only `DeviceId`, `AvailableConnections`, and typed `ConnectAsync<TConnection>()`.
- [ ] ISC-12: All production scalar `IYubiKey.ConnectionType` consumers are migrated. The inventory covers libraries (`FindYubiKeys`, `YubiKeyDeviceRepository`), shared/monolith CLI (`Cli.Shared/src/Device/DeviceSelectorBase.cs`, `Cli.Commands/src/Infrastructure/YkDeviceSelector.cs`), the FIDO2 extension (`Fido2/src/IYubiKeyExtensions.cs`), and every example tool selector/prompt/device-helper that reads an `IYubiKey`'s connection type (PivTool, OtpTool, OathTool, OpenPgpTool, HsmAuthTool, FidoTool, ManagementTool).
- [ ] ISC-13: A broad production scan for `\.ConnectionType\b` across `src/**` (production code; `src/Tests.Shared` and `*/tests/*` test-support code excluded and covered by ISC-15) returns only entries on an explicit exemption allowlist: `DeviceSelection.ConnectionType` (per-selection state), `IConnection.Type` and concrete-connection `.Type` (per-opened-connection state, not a device-capability routing replacement), the `ConnectionType` enum/type references themselves, and the new `AvailableConnections` API. No remaining entry reads a scalar per-interface connection type off an `IYubiKey`.
- [ ] ISC-14: The FIDO2 extension migration is strictly mechanical and parity-preserving: it resolves the single available FIDO-capable transport (`HidFido` or `SmartCard`); if a device exposes both it throws `NotSupportedException` (message referencing explicit selection in Phase 38) rather than baking in a preference. No multi-transport default is introduced in Phase 36.
- [ ] ISC-14.1: A focused FIDO2 test locks the migration: a single FIDO-capable transport succeeds, a dual-capable (`HidFido|SmartCard`) device throws, and no implicit HID-vs-SmartCard preference is exercised.
- [ ] ISC-15: All `IYubiKey` test doubles and test-support consumers across the solution implement/use the new shape and the suite builds. This explicitly includes `src/Tests.Shared` (`YubiKeyTestState` construction and cache key, `YubiKeyTestInfrastructure` device filtering) migrating off `device.ConnectionType` to `AvailableConnections`/`SupportsConnection` or the corrected matching helper; `YubiKeyTestState`'s own stored connection-type value is per-test-device state and may be retained.
- [ ] ISC-16: Core does not reference `Yubico.YubiKit.Management` (no project reference, no namespace usage).
- [ ] ISC-17: Unit tests cover `SupportsConnection`/`AvailableConnections` semantics, including a multi-connection (multi-bit) device, the `Hid` group input, and invalid inputs (`Unknown`, `All`, and a mixed multi-bit value such as `SmartCard|HidFido` returning `false`).
- [ ] ISC-17.1: Unit tests cover the corrected filter-matching helper against multi-bit `AvailableConnections` sets (e.g. filter `SmartCard` matches a `HidFido|SmartCard` device; filter `Hid` matches `HidOtp`; filter `All` matches any non-empty set).
- [ ] ISC-18: Unit tests cover default `ConnectAsync()` resolving for a single-connection device, throwing `InvalidOperationException` for a multi-connection device, and throwing `NotSupportedException` for a device with no available connections (`AvailableConnections == Unknown`).
- [ ] ISC-19: Unit tests cover typed `ConnectAsync<TConnection>()` success and unsupported-type failure.
- [ ] ISC-20: Discovery filtering behavior is unchanged for single-interface devices (existing `FindYubiKeys`/repository filter tests pass against `AvailableConnections`).
- [ ] ISC-21: Focused Core build and tests pass.
- [ ] ISC-22: Full solution build passes with `dotnet toolchain.cs build`.
- [ ] ISC-23: Active documentation validates with `dotnet toolchain.cs -- docs-qa` and changed-file formatting verifies clean.
- [ ] ISC-24: DevTeam review (interim GPT-5.4 acceptable) returns pass or all findings fixed; GPT-5.5 review queued; review output or waiver recorded.
- [ ] ISC-25: Phase 36 learning note exists and records the disposition, source changes, deferred default-connect consumers, review status, verification, deferred candidates, and next phase inputs.
- [ ] ISC-26: Anti: Phase 36 implements composite discovery merge, repository physical-identity keying, or add/remove event reshaping.
- [ ] ISC-26.1: Anti: Phase 36 adds any device-info member on `IYubiKey` (e.g. `GetDeviceInfoAsync`/`DeviceInfo`/`FirmwareVersion`) or any connection-owning `DeviceInfoReader` overload/equivalent; that surface is deferred to Phase 37.
- [ ] ISC-27: Anti: Phase 36 introduces a multi-transport smart default or explicit-override API (Phase 38 work), including in the FIDO2 extension.
- [ ] ISC-28: Anti: Phase 36 introduces a Core reference to Management.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | branch | Active branch | `## yubikit-composite-device-new` | `git status --short --branch` |
| ISC-2 | branch | Sibling repo branch | `## experiment/rust` | `git -C ../yubikey-manager status --short --branch` |
| ISC-3 | design | Disposition recorded | decision present before edits | Read |
| ISC-4 | review | Interim Cato ISA review | pass or resolved | `scripts/interim-cross-vendor-review.sh` output |
| ISC-5 to ISC-11 | source | Inspect IYubiKey + implementers | new shape present, scalar removed, semantics defined | Grep/Read/tests |
| ISC-12 | source | Migrate full production inventory | all named consumers migrated | Grep/Read |
| ISC-13 | source | No scalar IYubiKey connection-type routing remains | broad `src/**` check clean (exempting `DeviceSelection.ConnectionType`, `IConnection.Type`) | Grep |
| ISC-14 | source | FIDO2 migration mechanical only | single-interface parity; both-transport throws; no preference | Read |
| ISC-14.1 | unit | FIDO2 migration behavior locked | single succeeds; dual throws; no preference | `dotnet toolchain.cs -- test --project Fido2 --filter ...` |
| ISC-15 | build | Test doubles updated | suite builds | `dotnet toolchain.cs build` |
| ISC-16 | dependency | Core has no Management dep | no ref/usage | `.csproj` + grep |
| ISC-17 to ISC-20 | unit | New behavior + filter parity tests (incl. multi-bit) | focused tests pass | `dotnet toolchain.cs -- test --project Core --filter ...` |
| ISC-21 | build/test | Core build/tests | exit 0 | `dotnet toolchain.cs -- build/test --project Core` |
| ISC-22 | build | Full solution build | exit 0 | `dotnet toolchain.cs build` |
| ISC-23 | docs/format | Docs QA + changed-file format | exit 0 | `dotnet toolchain.cs -- docs-qa`; `dotnet format --verify-no-changes --include <changed>` |
| ISC-24 | review | DevTeam review | pass/fixed/waiver | review output |
| ISC-25 | file | Learning note | present | Read |
| ISC-26 to ISC-28 | scope/dependency | Scope and coupling guard (incl. 26.1 no device-info member) | no Phase 37/38 work; no device-info member; no Core->Management | Git diff / grep |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Phase 36 ISA + interim Cato | Write this ISA, record disposition, run interim Cato before edits. | ISC-1, ISC-2, ISC-3, ISC-4 | Phase 35 | false |
| IYubiKey reshape | Replace scalar with `AvailableConnections` + `SupportsConnection` (default members); add safe default connect; keep typed connect; correct filter-matching helper. | ISC-5, ISC-6, ISC-7, ISC-7.1, ISC-8, ISC-9, ISC-11 | interim Cato pass | false |
| Implementers | Update `PcscYubiKey`/`HidYubiKey` to the new shape with correct `AvailableConnections`. | ISC-10, ISC-16 | IYubiKey reshape | false |
| Call-site + test-double migration | Migrate libraries, CLI, example tools, FIDO2 extension (mechanical), and all `IYubiKey` doubles. | ISC-12, ISC-13, ISC-14, ISC-15, ISC-20 | IYubiKey reshape | true |
| Behavior tests | Add Core tests for predicate, multi-bit matching, default-connect ambiguity, and typed connect. | ISC-17, ISC-17.1, ISC-18, ISC-19, ISC-21 | implementers | true |
| Verify, review, learn, commit | Full build, docs/format, interim DevTeam review, learning note, commit. | ISC-22, ISC-23, ISC-24, ISC-25, ISC-26, ISC-27, ISC-28 | implementation complete | false |

## Decisions

- 2026-06-09: Scalar `IYubiKey.ConnectionType` is REMOVED (not obsoleted or repurposed). It is replaced by `ConnectionType AvailableConnections { get; }` (concrete connect bits only) and a default-interface predicate `bool SupportsConnection(ConnectionType connectionType)`. A v2 clean break is consistent with Phases 34/35 (no compatibility shim); a "primary transport hint" was rejected because it preserves the one-interface routing assumption this phase removes.
- 2026-06-09: `SupportsConnection` and the parameterless `ConnectAsync()` are default interface methods on `IYubiKey` defined in terms of `AvailableConnections`, so implementers only supply `DeviceId`, `AvailableConnections`, and typed `ConnectAsync<TConnection>()`.
- 2026-06-09: `SupportsConnection` semantics: concrete openable types only (`SmartCard`/`HidFido`/`HidOtp`); `Hid` means HidFido or HidOtp present; `Unknown` and `All` return false. `AvailableConnections` stores only concrete bits.
- 2026-06-09: The discovery filter-matching helper is corrected to compare a requested filter against a combined capability set (shared-concrete-bit match with `Hid` expansion; `All` matches any non-empty set), because the existing `MatchesDevice` `HasFlag(set)` logic is wrong for multi-bit sets. Multi-bit cases are tested in Phase 36 even though discovery still yields single-interface devices until Phase 37.
- 2026-06-09: The default `ConnectAsync()` resolves only when exactly one connection is available; for multi-connection devices it throws (ambiguous). In Phase 36 every device is still single-interface, so this is observably unchanged; it becomes the safety guard once Phase 37 produces multi-connection devices.
- 2026-06-09: The FIDO2 `IYubiKeyExtensions` transport switch is migrated mechanically with no preference: it opens the single available FIDO-capable transport (`HidFido` or `SmartCard`); if both are available it throws a clear "explicit FIDO transport selection is defined in Phase 38" exception. This preserves current single-interface behavior and does not bake in a Phase 38 smart default.
- 2026-06-09: A device-info read method on `IYubiKey` is deferred to Phase 37 (which owns discovery merge and eager `DeviceInfo` population), avoiding a Phase 36 collision with the existing `Management.IYubiKeyExtensions.GetDeviceInfoAsync` extension and avoiding connection-ownership/disposal hazards. Phase 36 does not add a `DeviceInfoReader` `IConnection` overload.
- 2026-06-09: `CLI` `DeviceSelection` keeps its own `ConnectionType` field because it records the transport chosen for a specific selection, a per-selection scalar, not the physical device's interface set. The ISC-13 proof exempts it and `IConnection.Type`.
- 2026-06-09: Parameterless-`ConnectAsync()` consumers in applet extensions (`Management/src/IYubiKeyExtensions.cs`, `YubiOtp/src/IYubiKeyExtensions.cs`, and the FIDO2 extension) are recorded as deferred default-connect consumers; they keep working for single-interface devices in Phase 36, and Phase 38 owns their multi-connection behavior. Sequencing rule (also recorded in the master ISA): Phase 37 must not ship merged multi-connection physical devices until these parameterless default-connect consumers are rewritten or gated by Phase 38, because a merged device makes the parameterless default connect ambiguous.

## Changelog

- conjectured: Phase 36 could keep scalar `ConnectionType` as a primary-transport hint to minimize call-site churn.
  refuted by: A hint still lets callers route on a single assumed interface, which is exactly the assumption the physical-device model must remove (program ISC-13.2).
  learned: Remove the scalar and migrate call sites to `AvailableConnections`/`SupportsConnection`.
  criterion now: ISC-5, ISC-12, ISC-13 govern removal and migration.
- conjectured: Reusing `MatchesDevice`, a literal `yubiKey.ConnectionType` grep, and adding `IYubiKey.GetDeviceInfoAsync` in Phase 36 would be sufficient and safe.
  refuted by: Interim Cato review (GPT-5.4) found `MatchesDevice` is wrong for multi-bit sets, the inventory/grep missed CLI selectors and example prompts, a FIDO2 preference would leak Phase 38 policy, and an `IYubiKey.GetDeviceInfoAsync` would shadow the Management extension and create connection-disposal hazards.
  learned: Define set-correct matching and `SupportsConnection` semantics now (with multi-bit tests), broaden the inventory and proof, keep FIDO2 migration preference-free, and defer device-info-on-IYubiKey to Phase 37.
  criterion now: ISC-7, ISC-7.1, ISC-12, ISC-13, ISC-14 and the deferral in Out of Scope govern these.

## Verification

Verification is populated in the Phase 36 learning note before commit.
