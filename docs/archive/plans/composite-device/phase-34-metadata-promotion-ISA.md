# Phase 34 ISA: Promote Read-Only Device Metadata To Core

This phase promotes read-only physical-device metadata from `Management` into `Core` so later composite-device phases can make `IYubiKey` a physical-device abstraction without introducing a `Core` -> `Management` dependency.

Read this together with:

- `docs/plans/composite-device/ISA.md`
- `docs/plans/composite-device/phase-33-composite-device-program-learnings.md`
- `src/Core/CLAUDE.md`
- `src/Management/CLAUDE.md`
- `src/Core/README.md`
- `src/Management/README.md`

## Problem

`DeviceInfo` currently lives in `Yubico.YubiKit.Management`, but the composite-device model requires Core `IYubiKey` to expose firmware, serial, form factor, capabilities, and version qualifier facts before callers choose a Management session. Core cannot reference Management because Management already references Core.

Moving `DeviceInfo` is not a one-file rename. `DeviceInfo` exposes `FormFactor`, `DeviceCapabilities`, `DeviceFlags`, `VersionQualifier`, and `VersionQualifierType`, all currently declared under the Management namespace. `Tests.Shared`, CLI shared/commands, examples, and Management tests compile against those types today.

## Vision

Core owns read-only physical-device metadata. Management still owns sessions, configuration writes, reset, lock codes, reboots, and backend protocol operations. After this phase, consumers that only need to inspect a YubiKey's identity can reference Core metadata types, while consumers that need management behavior still reference Management.

## Out of Scope

- No physical/composite `IYubiKey` implementation in Phase 34.
- No Core device-info reader in Phase 34.
- No discovery merge behavior in Phase 34.
- No extension-method smart default changes in Phase 34.
- No CLI command-family redesign; only mandatory compile/API migration for moved metadata types.
- No richer `Tests.Shared` filtering beyond mandatory compile migration.
- No downstream capability audit implementation.

## Principles

- Metadata promotion should be mechanical and minimal: move read-only types, update namespaces/usings, preserve behavior.
- Mutating Management concepts remain in Management.
- A v2 source-breaking namespace move is acceptable when explicitly recorded and verified; accidental extra public-surface drift is not.
- Mandatory compile migration is not optional downstream enhancement work.
- Tests should prove the moved parsing/value behavior still works, not just that the projects compile.
- A namespace move is solution-wide fallout. Focused project builds are useful diagnostics, but the unscoped solution build is the authoritative compile gate before commit.

## Constraints

- Execute on branch `yubikit-composite-device-new`.
- Use `/DevTeam` review/fix workflow after implementation.
- Cato review must audit the Phase 34 ISA before source edits because this is a broad API-boundary phase.
- Use `dotnet toolchain.cs`; never raw `dotnet build` or `dotnet test`.
- Commit only intended files after learning note and verification are complete.
- Do not introduce a `Core` project reference to `Management`.
- Do not leave applet libraries depending on `Management` solely for physical-device metadata.

## Goal

Move read-only device metadata types from `Yubico.YubiKit.Management` into `Yubico.YubiKit.Core.YubiKey`, update Management and mandatory compile consumers, preserve Management behavior, verify metadata parsing/value semantics in Core, verify no Core-to-Management dependency is introduced, run DevTeam review, write a learning note, and commit Phase 34 only.

## Criteria

- [ ] ISC-1: Branch check shows `## yubikit-composite-device-new` before source edits, review delegation, build/test, or commit.
- [ ] ISC-2: Cato reviews this Phase 34 ISA before source edits and returns pass or all concerns are resolved.
- [ ] ISC-3: `DeviceInfo` lives in Core under namespace `Yubico.YubiKit.Core.YubiKey`.
- [ ] ISC-4: `FormFactor` lives in Core under namespace `Yubico.YubiKit.Core.YubiKey`.
- [ ] ISC-5: `DeviceCapabilities` lives in Core under namespace `Yubico.YubiKit.Core.YubiKey`.
- [ ] ISC-6: `DeviceFlags` lives in Core under namespace `Yubico.YubiKit.Core.YubiKey`.
- [ ] ISC-7: `VersionQualifier` and `VersionQualifierType` live in Core under namespace `Yubico.YubiKit.Core.YubiKey`.
- [ ] ISC-8: `CapabilityMapper` or equivalent parsing support moves with `DeviceInfo` and does not remain in Management.
- [ ] ISC-8.1: Existing `CapabilityMapperTests` migrate from Management unit tests to Core unit tests, or are replaced by equivalent Core tests before commit.
- [ ] ISC-8.2: `CapabilityMapper.FromFips` disposition is explicit: keep it internal in Core if the existing tests prove useful capability decoding behavior, or delete it with tests removed if no production/future Core reader need exists.
- [ ] ISC-9: `ManagementSession.GetDeviceInfoAsync` returns the Core `DeviceInfo` type without changing behavior.
- [ ] ISC-10: `DeviceConfig`, Management backends, reset/config/lock/reboot behavior, and Management session ownership remain in Management.
- [ ] ISC-11: Core production project still has no `ProjectReference` to Management.
- [ ] ISC-12: Management production project still references Core.
- [ ] ISC-13: `Tests.Shared` compiles against Core metadata types after mandatory migration.
- [ ] ISC-14: CLI shared/commands and example compile consumers of `DeviceInfo` use Core metadata types where they only need metadata.
- [ ] ISC-14.1: Applet integration tests and applet example tools that compile against moved metadata types are migrated or verified by full-solution build.
- [ ] ISC-15: Applet libraries do not gain Management references solely for metadata.
- [ ] ISC-16: Core unit tests cover `DeviceInfo.CreateFromTlvs` success behavior for representative required TLVs.
- [ ] ISC-17: Core unit tests cover `DeviceInfo.CreateFromTlvs` version qualifier behavior.
- [ ] ISC-18: Core unit tests cover invalid version qualifier data or malformed required TLV behavior where current behavior is defined.
- [ ] ISC-19: Existing Management device-info page sequencing tests continue to pass with the Core metadata type.
- [ ] ISC-20: Focused Core build passes.
- [ ] ISC-21: Focused Management build/tests pass.
- [ ] ISC-22: Focused Tests.Shared or representative dependent test/build verification passes.
- [ ] ISC-23: Focused CLI shared/commands build or representative compile verification passes.
- [ ] ISC-23.1: Full solution build passes with `dotnet toolchain.cs build` after namespace migration.
- [ ] ISC-23.2: Phase 34 records a public API/source-surface check showing only intended public metadata ownership changes and no accidental extra public-surface drift.
- [ ] ISC-24: DevTeam cross-vendor review returns pass or all findings are fixed.
- [ ] ISC-25: Phase 34 learning note exists and records source changes, review, verification, deferred candidates, and next phase inputs.
- [ ] ISC-26: Anti: Phase 34 changes `IYubiKey` physical-device semantics.
- [ ] ISC-27: Anti: Phase 34 implements richer Tests.Shared filtering, smarter CLI selection, or extension smart defaults beyond required compile migration.
- [ ] ISC-28: Anti: Phase 34 creates duplicate public `DeviceInfo` models in Core and Management.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | branch | Check active branch | `## yubikit-composite-device-new` | `git status --short --branch` |
| ISC-2 | review | Cato ISA review | pass or resolved concerns | Cato JSONL |
| ISC-3 to ISC-8 | source | Verify moved types and namespaces | types in Core namespace; no stale Management declarations | Grep/Read |
| ISC-8.1 to ISC-8.2 | unit migration | Verify `CapabilityMapper` test/disposition | tests moved/replaced, `FromFips` kept or removed intentionally | Grep/Read/tests |
| ISC-9 to ISC-10 | source | Verify Management behavior boundary | session returns Core type; mutating types stay in Management | Grep/Read/tests |
| ISC-11 to ISC-12 | dependency | Verify project references | Core has no Management ref; Management has Core ref | `.csproj` grep |
| ISC-13 to ISC-15 | compile migration | Verify mandatory consumers compile/use Core metadata | no metadata-only Management references introduced | Grep/build/tests |
| ISC-14.1 | compile migration | Verify applet integration/example consumers | solution build passes | `dotnet toolchain.cs build` |
| ISC-16 to ISC-18 | unit | Core metadata parsing tests | focused tests pass | `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~DeviceInfo"` |
| ISC-19 | unit | Management device-info tests | focused tests pass | `dotnet toolchain.cs -- test --project Management --filter "FullyQualifiedName~ManagementSessionTests"` |
| ISC-20 | build | Core build | exit 0 | `dotnet toolchain.cs -- build --project Core` |
| ISC-21 | build/test | Management build/tests | exit 0 | `dotnet toolchain.cs -- build --project Management`; focused test |
| ISC-22 | build/test | Tests.Shared dependent verification | exit 0 | `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~YubiKeyDeviceRepositoryTests"` or narrower consumer test |
| ISC-23 | build | CLI compile consumer verification | exit 0 | `dotnet toolchain.cs -- build --project Cli.Shared`; `dotnet toolchain.cs -- build --project Cli.Commands` |
| ISC-23.1 | build | Full solution compile | exit 0 | `dotnet toolchain.cs build` |
| ISC-23.2 | API surface | Verify public/source-surface drift | only intended metadata ownership changes | public API/source grep or diff review |
| ISC-24 | review | DevTeam review | pass or resolved findings | review output |
| ISC-25 | file | Learning note exists | title and evidence present | Read |
| ISC-26 to ISC-28 | scope | Scope guard | no physical model/default/filter feature work | Git diff review |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Phase 34 ISA and Cato | Write this ISA and run Cato before source edits. | ISC-1, ISC-2 | Phase 33 | false |
| Metadata type move | Move `DeviceInfo`, `FormFactor`, `DeviceCapabilities`, `DeviceFlags`, `VersionQualifier`, `VersionQualifierType`, and parsing support into Core; migrate `CapabilityMapper` tests or delete dead helper code intentionally. | ISC-3, ISC-4, ISC-5, ISC-6, ISC-7, ISC-8, ISC-8.1, ISC-8.2 | Cato pass | false |
| Management migration | Update Management source/tests/docs to use Core metadata while preserving mutating behavior. | ISC-9, ISC-10, ISC-19, ISC-21 | Metadata type move | false |
| Mandatory consumer migration | Update `Tests.Shared`, CLI shared/commands, applet integration tests, examples, and tests needed to compile against Core metadata. | ISC-13, ISC-14, ISC-14.1, ISC-15, ISC-22, ISC-23, ISC-23.1, ISC-23.2 | Metadata type move | true |
| Core metadata tests | Add focused Core tests for DeviceInfo parsing and version qualifier behavior. | ISC-16, ISC-17, ISC-18, ISC-20 | Metadata type move | false |
| Review, learning, commit | Run DevTeam review, fix findings, verify, write learning note, and commit intended files. | ISC-24, ISC-25, ISC-26, ISC-27, ISC-28 | implementation complete | false |

## Decisions

- 2026-06-09: Phase 34 accepts a v2 source namespace move: read-only metadata types move from `Yubico.YubiKit.Management` to `Yubico.YubiKit.Core.YubiKey`.
- 2026-06-09: No duplicate wrapper `DeviceInfo` remains in Management during Phase 34 because duplicate public models would make later physical-device work ambiguous.
- 2026-06-09: No type-forwarding or compatibility shim is added in Phase 34 because the approved v2 direction prioritizes a clear Core ownership boundary and there is no protected external package baseline yet.
- 2026-06-09: Mandatory consumer compile migration includes `Tests.Shared`, CLI shared/commands, examples, and tests that use metadata-only types; richer behavior stays deferred.
- 2026-06-09: `ChallengeResponseTimeout` remains `ReadOnlyMemory<byte>` in Phase 34 to preserve behavior; any scalar conversion is deferred unless tests reveal an ownership bug.
- 2026-06-09: `CapabilityMapper.FromFips` has no production caller today but existing tests cover it. Phase 34 may keep it internal in Core with migrated tests if useful for future Core read-info work, or delete it with test removal if implementation review judges it dead code.
- 2026-06-09: Full solution build is required before commit because the namespace move can break applet integration tests and example tools outside the focused Core/Management/CLI project set.
- 2026-06-09: Phase 34 API-surface checking is source-diff based unless an existing public API analyzer is found; the required result is that only the approved metadata ownership move changes public source shape.

## Changelog

- conjectured: Metadata promotion might be a simple move of `DeviceInfo` only.
  refuted by: Cato and source inspection showed `DeviceInfo` publicly exposes several Management namespace types and mandatory consumers compile against them.
  learned: Phase 34 must move the whole read-only metadata surface and migrate required consumers together.
  criterion now: ISC-3 through ISC-15 govern the move and mandatory compile migration.

## Verification

Verification is populated in the Phase 34 learning note before commit.
