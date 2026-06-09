# ISA: Composite YubiKey Device Model

This ISA defines the v2 composite-device program. It begins after the module-consolidation quality gate passed in Phase 32 and after the owner approved starting composite-device planning on a dedicated branch.

Read this together with:

- `docs/SDK-HOUSE-STYLE.md`
- `docs/MODULE-CONSOLIDATION-FINAL-REASSESSMENT.md`
- `docs/plans/module-consolidation/phase-20-quality-convergence-before-composite-yubikey-ISA.md`
- `docs/plans/module-consolidation/phase-32-same-criteria-quality-reassessment-learnings.md`
- `../yubikey-manager` on branch `experiment/rust`

## Problem

The current Core `IYubiKey` abstraction is closer to a connection handle than a physical device. It exposes `DeviceId`, one concrete `ConnectionType`, and `ConnectAsync<TConnection>()`, but it does not expose the physical key's device information, firmware version, serial, form factor, capabilities, or the set of available interfaces. A normal USB YubiKey can appear as several independent SDK devices when CCID, FIDO HID, and OTP HID are visible.

That model makes physical-device selection, cache events, capability-aware session defaults, and user-facing discovery semantics harder than they should be. The Rust `yubikey-manager` `experiment/rust` branch already models this better: one local device owns optional SmartCard, OTP HID, and FIDO HID paths plus read-only `DeviceInfo`.

The .NET SDK needs the same conceptual move without mechanically porting Rust and without breaking the .NET package boundary where `Core` is below `Management` and all applet modules.

## Vision

`YubiKeyManager.FindAllAsync()` returns one logical object per physical YubiKey. That object tells callers what the key is, what firmware it runs, what capabilities and interfaces are visible, and which typed connections can be opened. Applet-specific `IYubiKeyExtensions` remain the ergonomic entry point for module sessions, but they can now make smart, documented defaults because the physical key carries real device facts.

The result should feel obvious to a .NET v2 SDK user: select a physical key once, inspect it once, then open the app/session you need with either smart defaults or explicit transport control.

## Out of Scope

- No CLI command-family redesign in this program unless a later phase explicitly promotes a narrow CLI follow-up.
- No CLI command-family redesign in this program. Minimum compile/API migration for CLI consumers of moved Core metadata is in scope when required by the metadata move.
- No broad applet refactor beyond extension-method and test changes required by the new physical-device model.
- No dependency from `Core` to `Management`.
- No applet-module dependency on `Management` solely to inspect physical-device metadata.
- No mechanical Rust port. Rust is the reference behavior and edge-case inventory, not the target architecture.
- No unattended FIDO2/WebAuthn User Presence, UV, touch, or insert/remove ceremony tests.
- No destructive Management configuration tests in this program unless explicitly isolated and human-approved.

## Principles

- `IYubiKey` represents a physical key, not one interface handle.
- Read-only device identity belongs at the Core layer because discovery, filtering, applet extensions, tests, and callers need it before choosing a module session.
- Mutating device configuration belongs in `Management` because writes, lock codes, reset, reboot, and mode changes are management operations, not Core discovery facts.
- Raw connection APIs should be explicit; smart defaults belong in app/module extension methods where intent is known.
- Rust reference behavior should inform edge cases, especially same-PID merging, NFC separation, and discovery fallbacks, but the .NET shape must respect assembly boundaries and existing extension ergonomics.
- Each phase must compile, test, review, learn, and commit independently.

## Constraints

- Execute on branch `yubikit-composite-device-new`.
- Use `dotnet toolchain.cs` commands only; never raw `dotnet build` or raw `dotnet test`.
- Each phase has a phase ISA before source changes.
- Each implementation phase uses `/DevTeam` implementation/review/fix workflow or records an explicit docs-only review path.
- Cato review is required for Phase 33 planning, any broad API-boundary decision phase, and the final program verification phase.
- Commit only intended files after each phase; never use `git add .`, `git add -A`, or `git commit -a`.
- Public module extension ergonomics should be preserved unless a phase ISA records and justifies a breaking shape.
- Keep `ConnectionType` semantics source-backed and tested when moving from per-interface devices to capability/filter semantics.
- Phase 34 must decide and document the public namespace/API migration strategy for `DeviceInfo` and supporting types before moving them. Supporting types include at least `FormFactor`, `DeviceCapabilities`, `DeviceFlags`, `VersionQualifier`, and `VersionQualifierType`.
- Phase 34 must migrate mandatory compile consumers in the same phase as metadata promotion. This includes `Management`, `Tests.Shared`, and CLI consumers that read `DeviceInfo` today. Optional downstream capabilities remain deferred.
- Phase 36 must decide the disposition of scalar `IYubiKey.ConnectionType`: remove, obsolete, repurpose as a primary-transport hint, or replace with an available-connections property. The decision must be explicit before source edits.

## Goal

Build the v2 composite YubiKey device model in staged, reviewable phases so Core discovery returns physical YubiKeys with read-only device metadata and available connection flags, applet extension methods preserve ergonomic smart defaults with explicit override paths, Management retains mutating configuration ownership, and the final SDK behavior is verified with unit tests, safe hardware smoke tests, docs, DevTeam review, Cato review, and per-phase learning notes.

## Criteria

### Program Governance

- [ ] ISC-1: Branch check shows `## yubikit-composite-device-new` before any composite-device edits, build/test commands, review delegation, or commit.
- [ ] ISC-2: `../yubikey-manager` reference branch is recorded as `experiment/rust` before design decisions cite Rust behavior.
- [ ] ISC-3: Every phase has a phase ISA before source changes begin.
- [ ] ISC-4: Every implementation phase records `/DevTeam` review, fixes findings, and records review output or waiver before commit.
- [ ] ISC-5: Cato review is completed for Phase 33 planning, broad API-boundary phases, and final program verification.
- [ ] ISC-6: Every phase writes a learning note and commits only intended files before the next phase begins.
- [ ] ISC-7: Anti: source changes occur on any branch other than `yubikit-composite-device-new`.

### Architecture

- [ ] ISC-8: `IYubiKey` represents one physical YubiKey rather than one concrete interface handle.
- [ ] ISC-9: Core owns read-only physical-device metadata needed by `IYubiKey`, including firmware version, serial, form factor, capabilities, and version qualifier facts.
- [ ] ISC-9.1: Phase 34 records the public namespace/API migration strategy for `DeviceInfo`, `FormFactor`, `DeviceCapabilities`, `DeviceFlags`, `VersionQualifier`, and `VersionQualifierType` before moving or splitting types.
- [ ] ISC-9.2: Phase 34 verification includes an API-surface or source-compatibility check appropriate for the approved v2 break policy, so unintended namespace/API fallout is visible before commit.
- [ ] ISC-10: Management owns mutating device configuration, reset, lock, reboot, and mode behavior.
- [ ] ISC-11: Core does not reference `Yubico.YubiKit.Management`.
- [ ] ISC-11.1: Phase 35 verifies any shared read-info logic still preserves the Core-to-Management dependency direction and does not introduce direct, indirect, or helper-mediated Core reliance on Management.
- [ ] ISC-12: Applet modules do not reference `Yubico.YubiKit.Management` solely for physical-device metadata.
- [ ] ISC-13: `IYubiKey` exposes available connection flags and a `SupportsConnection(...)`-style predicate or equivalent.
- [ ] ISC-13.1: Phase 36 records the disposition of the existing scalar `IYubiKey.ConnectionType` property and updates all production call sites that assume one interface per `IYubiKey`.
- [ ] ISC-13.2: Phase 36 verification includes a grep or API check proving no production code still relies on scalar `yubiKey.ConnectionType` for composite-device routing unless that usage is explicitly approved.
- [ ] ISC-14: Typed `ConnectAsync<TConnection>()` routes to the requested concrete interface when available and fails clearly when unsupported.
- [ ] ISC-15: Raw untyped/default connection APIs do not silently choose surprising transports for composite devices.
- [ ] ISC-15.1: Phase 36 or Phase 38 includes focused tests proving raw/default connection behavior is either explicit, unsupported, or documented as an app-specific smart default.

### Discovery And Identity

- [ ] ISC-16: `YubiKeyManager.FindAllAsync(ConnectionType.All)` returns one logical device per physical USB YubiKey when CCID, FIDO HID, and OTP HID are all visible.
- [ ] ISC-17: `ConnectionType` filters return devices capable of the requested connection, not duplicate per-interface rows.
- [ ] ISC-18: NFC PC/SC devices are never merged with USB HID or USB CCID devices.
- [ ] ISC-19: Multiple same-PID USB keys are not collapsed unless identity evidence is strong enough.
- [ ] ISC-20: Repository add/remove events are keyed by physical-device identity rather than per-interface identity.

### Extension Ergonomics

- [ ] ISC-21: Existing applet `IYubiKeyExtensions` remain the primary ergonomic session-entry surface.
- [ ] ISC-21.1: Phase 38 explicitly rewrites extension-method transport selection away from scalar `IYubiKey.ConnectionType` assumptions, with FIDO2 called out because it currently switches on `yubiKey.ConnectionType`.
- [ ] ISC-22: Smart defaults are app-specific and documented: SmartCard applets prefer SmartCard, FIDO2/WebAuthn prefer HID FIDO when available, Management can prefer SmartCard then FIDO HID then OTP HID.
- [ ] ISC-23: Explicit connection preference or override is available where a module can reasonably use more than one transport.
- [ ] ISC-24: Existing extension-method unit tests are updated to verify both default selection and explicit override behavior.

### Tests And Verification

- [ ] ISC-25: Unit tests cover metadata promotion and parsing after read-only types move into Core.
- [ ] ISC-25.1: Phase 34 includes mandatory compile migration for `Tests.Shared` and CLI consumers of moved metadata; optional richer test filtering and smarter CLI selection stay deferred.
- [ ] ISC-26: Unit tests cover Core device-info read behavior over fake SmartCard/FIDO/OTP paths where feasible.
- [ ] ISC-27: Unit tests cover single-PID merge, same-PID conservative no-collapse behavior, NFC no-merge behavior, filter semantics, and repository event diffs.
- [ ] ISC-28: Safe integration smoke verifies physical-device discovery and typed connection opening on allowed hardware without UP/UV/touch ceremony requirements.
- [ ] ISC-29: Active docs explain physical-device semantics, read-only metadata ownership, smart defaults, and migration from per-interface handles.
- [ ] ISC-30: Anti: final verification claims composite readiness without docs QA, focused tests, safe hardware smoke or skip rationale, DevTeam review, and Cato review.

### Deferred Improvements

- [ ] ISC-31: A deferred downstream capability audit is recorded for opportunities unlocked by promoting `DeviceInfo` to Core.
- [ ] ISC-31.1: Mandatory consumer migration is not classified as deferred downstream capability work.
- [ ] ISC-32: Anti: downstream capability opportunities are implemented during metadata promotion before the physical-device model is stable.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | branch | Verify active branch | `## yubikit-composite-device-new` | `git status --short --branch` |
| ISC-2 | branch | Verify sibling repo reference branch | `## experiment/rust` | `git -C ../yubikey-manager status --short --branch` |
| ISC-3 to ISC-6 | governance | Phase artifacts and review records exist | phase ISA, learning note, review evidence present | Read/Grep/Cato output |
| ISC-8 to ISC-15 | API shape | Inspect Core source and public API tests | metadata in Core, no Core->Management ref, typed connect tested | Grep/Read/tests |
| ISC-11.1 | dependency | Verify shared read-info implementation does not couple Core to Management | no Core production reference to Management and no shared helper owned by Management used by Core | Grep/project refs |
| ISC-9.1 to ISC-9.2 | API migration | Verify namespace/API migration strategy and API-surface check | approved break policy recorded and checked | Read/Grep/API check |
| ISC-13.1 to ISC-13.2 | API migration | Verify scalar `IYubiKey.ConnectionType` disposition and call-site migration | no unapproved scalar routing remains | Grep/API tests |
| ISC-15.1 | connection defaults | Verify raw/default behavior | explicit/unsupported/default behavior tested | Core/app extension unit tests |
| ISC-16 to ISC-20 | discovery semantics | Unit tests over fake device inventories | one physical device, correct filters/events | `dotnet toolchain.cs -- test --project Core --filter ...` |
| ISC-21 to ISC-24 | extension behavior | Applet extension unit tests | defaults and overrides pass | focused module tests |
| ISC-25 to ISC-27 | unit coverage | Core/Management/Tests.Shared/CLI compile migration and tests | focused tests pass, consumers compile | `dotnet toolchain.cs -- test --project ... --filter ...` |
| ISC-28 | integration | Safe hardware smoke or recorded skip | pass or explicit skip rationale | `dotnet toolchain.cs -- test --integration --project Core --smoke --filter ...` |
| ISC-29 | docs | Active documentation validates | exit 0 | `dotnet toolchain.cs -- docs-qa` |
| ISC-30 | final review | Cato final audit | pass or resolved concerns | Cato output JSONL |
| ISC-31 to ISC-32 | deferred scope | Deferred item recorded and not implemented early | note present, no premature source scope | Read/Git diff |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Phase 33 program planning | Create branch, write this ISA, write Phase 33 ISA/learnings, map Rust reference, run Cato, commit docs only. | ISC-1, ISC-2, ISC-3, ISC-5, ISC-31, ISC-32 | Phase 32 gate | false |
| Phase 34 metadata promotion | Move or split read-only device metadata types from Management into Core while preserving Management behavior and migrating mandatory Tests.Shared/CLI consumers. | ISC-8, ISC-9, ISC-9.1, ISC-9.2, ISC-10, ISC-11, ISC-12, ISC-25, ISC-25.1, ISC-31.1 | Phase 33 | false |
| Phase 35 Core device-info reader | Add Core-owned read-info paths used by discovery without Management dependency and verify any shared read-info logic preserves dependency direction. | ISC-9, ISC-10, ISC-11, ISC-11.1, ISC-26 | Phase 34 | false |
| Phase 36 physical device model | Implement physical `IYubiKey` shape with metadata, available connections, support checks, typed connection routing, explicit scalar `IYubiKey.ConnectionType` disposition, and raw/default connection behavior tests. | ISC-8, ISC-13, ISC-13.1, ISC-13.2, ISC-14, ISC-15, ISC-15.1 | Phase 35 | false |
| Phase 37 composite discovery | Merge partial PC/SC, OTP HID, and FIDO HID discoveries into physical devices with correct filtering and events. | ISC-16, ISC-17, ISC-18, ISC-19, ISC-20, ISC-27 | Phase 36 | false |
| Phase 38 extension defaults | Preserve app-specific extension ergonomics, remove scalar-connection assumptions, and add smart defaults plus explicit overrides where needed. | ISC-21, ISC-21.1, ISC-22, ISC-23, ISC-24 | Phase 37 | true |
| Phase 39 integration and final verification | Update docs, run safe hardware smoke, final tests, final Cato, and final learning note. | ISC-28, ISC-29, ISC-30 | Phase 38 | false |

## Decisions

- 2026-06-09: Composite-device work runs on dedicated branch `yubikit-composite-device-new` branched from the completed module-consolidation quality gate.
- 2026-06-09: `IYubiKey` should represent a physical device in v2, not a single interface handle.
- 2026-06-09: Rust `../yubikey-manager` branch `experiment/rust` is the reference implementation for composite-device discovery behavior.
- 2026-06-09: Rust is a same-crate design; .NET must adapt the concept without introducing a `Core` -> `Management` dependency cycle.
- 2026-06-09: Read-only device metadata needed by physical discovery belongs in Core; mutating configuration and management-session behavior remain in Management.
- 2026-06-09: Existing applet `IYubiKeyExtensions` are valued and must be preserved as the ergonomic app/session entry points.
- 2026-06-09: Raw connection selection should stay explicit, while applet extensions may provide smart defaults because they know the application intent.
- 2026-06-09: Implementation phases use `/DevTeam` review/fix/commit workflow; Phase 33 uses docs-only Cato review before commit.
- 2026-06-09: Deferred downstream audit is required because promoting `DeviceInfo` to Core may unlock better capability-aware APIs, extension defaults, test filtering, CLI selection, docs examples, and future feature gates.
- 2026-06-09: Cato identified that `DeviceInfo` promotion carries namespace/API fallout because supporting public types currently live in `Yubico.YubiKit.Management`; Phase 34 must choose and verify the migration strategy explicitly.
- 2026-06-09: Cato identified that scalar `IYubiKey.ConnectionType` is not merely additive debt; it is a core breaking-change decision for the physical-device model and current extension methods.
- 2026-06-09: Cato identified that `Tests.Shared` and CLI consumers of `DeviceInfo` require mandatory compile migration when metadata moves; optional richer behavior remains deferred.
- 2026-06-09: Cato follow-up passed and surfaced two info-level tightenings: give raw/default connection behavior explicit test ownership and verify Phase 35 read-info sharing does not reintroduce Core-to-Management coupling.

## Changelog

- conjectured: Composite-device implementation could begin immediately after Phase 32 because the quality gate passed.
  refuted by: Owner discussion surfaced unresolved API ownership questions around `IYubiKey`, `DeviceInfo`, Management/Core boundaries, smart defaults, and extension-method preservation.
  learned: The program needs a dedicated composite-device ISA and staged implementation phases before source changes.
  criterion now: ISC-3, ISC-5, and the Phase 33 feature require design artifacts and Cato review before implementation.
- conjectured: Applet modules could depend on Management to access `DeviceInfo`.
  refuted by: `IYubiKey` lives in Core, and applet dependencies on Management would not solve Core-facing physical-device metadata without creating awkward package coupling.
  learned: Read-only metadata required by physical discovery belongs in Core; Management should keep mutating operations.
  criterion now: ISC-9, ISC-10, ISC-11, and ISC-12 govern the package boundary.

## Verification

Verification is populated by each phase learning note. This master ISA is not complete until Phase 39 records final docs QA, focused build/test evidence, safe integration smoke or skip rationale, DevTeam review, Cato final audit, and commit evidence.

## Phase Order

### Phase 33: Composite Device Program ISA

Create/switch to `yubikit-composite-device-new`, write the master ISA and Phase 33 artifacts, record Rust reference branch evidence, run Cato against the plan, verify docs, commit docs only, and stop for the owner's next command before source implementation.

### Phase 34: Promote Read-Only Device Metadata To Core

Move or split read-only physical-device metadata from Management into Core, including `DeviceInfo` and supporting value types. Before source edits, decide the public namespace/API migration strategy for `DeviceInfo`, `FormFactor`, `DeviceCapabilities`, `DeviceFlags`, `VersionQualifier`, and `VersionQualifierType`. Preserve Management behavior and update mandatory consumers including `Management`, `Tests.Shared`, and CLI compile consumers. Do not implement optional downstream capability opportunities during this phase.

### Phase 35: Core Device Info Reader

Add Core-owned read-only device-info discovery over SmartCard, FIDO HID, and OTP HID paths without depending on Management. Preserve ManagementSession behavior by sharing or delegating read-info logic where practical, but verify the final dependency direction explicitly: Core must not reference Management, and Core must not consume a helper owned by Management.

### Phase 36: Physical YubiKey Model

Introduce the physical-device `IYubiKey` shape with `DeviceInfo`, `FirmwareVersion`, available connection flags, support predicates, and typed connection routing. This phase must decide what happens to the existing scalar `IYubiKey.ConnectionType` property and update all production routing assumptions that depended on one interface per device. It must also bind raw/default connection behavior to tests: either the behavior is explicit, unsupported for composite devices, or documented as an app-specific smart default owned by an extension method.

### Phase 37: Composite Discovery And Repository Semantics

Implement merge behavior so CCID, FIDO HID, and OTP HID interfaces for one physical USB key become one SDK device. Update filtering, repository cache keys, and add/remove events.

### Phase 38: Extension Method Smart Defaults

Update applet extension methods to preserve current ergonomics while using physical-device facts for app-specific smart defaults and explicit connection overrides. This phase must explicitly remove scalar `IYubiKey.ConnectionType` routing assumptions from current extension implementations, including FIDO2.

### Phase 39: Integration, Docs, Migration, Final Cato

Run safe integration smoke, update docs and migration notes, run final focused builds/tests/docs QA, run Cato final audit, commit, and record remaining deferred follow-ups.

## Mandatory Consumer Migration Versus Deferred Capability Audit

The metadata move itself has mandatory consumer migration work. `Management`, `Tests.Shared`, and CLI code that currently compile against `Management.DeviceInfo` or supporting Management metadata types must be migrated in the phase that moves those types. This is not deferred work and is not a CLI redesign.

## Deferred Follow-Up: DeviceInfo Promotion Downstream Capability Audit

Promoting read-only `DeviceInfo` to Core may unlock downstream capabilities that are valuable but not part of metadata promotion itself:

- capability-aware extension defaults beyond the minimum needed for composite-device correctness
- richer `Tests.Shared` filtering and state objects beyond the mandatory compile migration
- smarter CLI selection and display beyond the mandatory compile migration once the library surface is stable
- simpler docs examples that no longer need a Management session just to identify a key
- future feature-gating APIs that combine firmware, transport, and capability facts

These opportunities are intentionally deferred until after the physical-device model is implemented and verified. The later audit should inventory the new Core metadata surface and decide which downstream features deserve their own focused phase.
