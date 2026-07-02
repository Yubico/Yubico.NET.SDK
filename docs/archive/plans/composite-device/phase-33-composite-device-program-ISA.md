# Phase 33 ISA: Composite Device Program Planning

This phase starts the composite-device program. It writes the plan artifacts and verifies them before any source implementation begins.

Read this together with:

- `docs/plans/composite-device/ISA.md`
- `docs/plans/module-consolidation/phase-20-quality-convergence-before-composite-yubikey-ISA.md`
- `docs/plans/module-consolidation/phase-32-same-criteria-quality-reassessment-learnings.md`
- `docs/MODULE-CONSOLIDATION-FINAL-REASSESSMENT.md`
- `../yubikey-manager` on branch `experiment/rust`

## Problem

The module-consolidation program stopped correctly before composite YubiKey design. Owner discussion now clarified the intended v2 direction: `IYubiKey` should represent a physical device with firmware, read-only device info, available connection types, and app-specific smart defaults through extension methods.

That design has enough package-boundary and API consequences that implementation must not start as a single broad source change. The work needs a new branch, a new long-lived ISA, a phase sequence, Cato review, and explicit stop after planning.

## Vision

Phase 33 leaves the repository ready for implementation but does not implement. The branch exists, the composite-device program ISA defines professional .NET phases, the Rust reference branch is recorded, downstream deferred opportunities are captured, and Cato has reviewed the plan before source files change.

## Out of Scope

- No source-code changes in Phase 33.
- No `DeviceInfo` movement in Phase 33.
- No `IYubiKey` API changes in Phase 33.
- No Core, Management, applet, CLI, or test project edits except plan documentation.
- No implementation delegation to `/DevTeam` in Phase 33; later source phases use `/DevTeam`.

## Principles

- Planning artifacts are part of the product because they bind future implementation phases.
- Phase 33 should be docs-only, reviewable, and independently commit-ready.
- The plan must explicitly separate package-boundary work, discovery work, physical model work, extension ergonomics, integration, and deferred downstream opportunity mining.
- Cato should audit the plan before implementation to catch missed architecture, test, and workflow concerns.

## Constraints

- Execute on branch `yubikit-composite-device-new`.
- Use `dotnet toolchain.cs -- docs-qa` for active docs verification.
- Use `git diff --check` for whitespace verification.
- Resolve Cato through `AgentHarnessRouter.ts`; OpenCode/OpenAI primary must route to Vertex Opus 4.8.
- Commit only `docs/plans/composite-device/ISA.md`, this file, and the Phase 33 learning note.
- Stop after the Phase 33 commit and wait for the owner's command before Phase 34.

## Goal

Create and verify the composite-device program planning artifacts on `yubikit-composite-device-new`, including the master ISA, Phase 33 ISA, Phase 33 learning note, Rust reference evidence, deferred downstream capability audit item, Cato review, docs QA, whitespace verification, and a docs-only commit; then stop before source implementation.

## Criteria

- [ ] ISC-1: Branch check shows `## yubikit-composite-device-new` before artifact edits.
- [ ] ISC-2: `docs/plans/composite-device/ISA.md` exists and defines the composite-device program.
- [ ] ISC-3: This Phase 33 ISA exists and declares docs-only scope.
- [ ] ISC-4: Phase 33 learning note exists at `docs/plans/composite-device/phase-33-composite-device-program-learnings.md`.
- [ ] ISC-5: The master ISA records `../yubikey-manager` branch `experiment/rust` as the Rust reference.
- [ ] ISC-6: The master ISA records that `IYubiKey` should represent a physical device.
- [ ] ISC-7: The master ISA records that read-only physical-device metadata belongs in Core and mutating configuration remains in Management.
- [ ] ISC-8: The master ISA records that applet `IYubiKeyExtensions` should remain the ergonomic session-entry surface.
- [ ] ISC-9: The master ISA splits implementation into logical phases instead of one broad implementation phase.
- [ ] ISC-10: The master ISA requires `/DevTeam` review/fix/commit workflow for source phases.
- [ ] ISC-11: The master ISA includes a deferred plan item for `DeviceInfo` promotion downstream capability audit.
- [ ] ISC-12: Phase 33 Cato route resolves to `google-vertex-anthropic/claude-opus-4-8@default` or an explicit structured routing failure is recorded.
- [ ] ISC-13: Phase 33 Cato audit returns pass or all findings are resolved before commit.
- [ ] ISC-13.1: Cato findings on `DeviceInfo` namespace/API migration fallout are reflected in the master ISA before commit.
- [ ] ISC-13.2: Cato findings on scalar `IYubiKey.ConnectionType` disposition are reflected in the master ISA before commit.
- [ ] ISC-13.3: Cato findings on mandatory `Tests.Shared` and CLI consumer migration are reflected in the master ISA before commit.
- [ ] ISC-14: `dotnet toolchain.cs -- docs-qa` passes after artifact edits.
- [ ] ISC-15: `git diff --check` passes after artifact edits.
- [ ] ISC-16: `git diff --name-only` shows only Phase 33 docs before commit.
- [ ] ISC-17: Anti: Phase 33 changes files under `src/`.
- [ ] ISC-18: Anti: Phase 33 starts Phase 34 implementation.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | branch | Verify active branch | `## yubikit-composite-device-new` | `git status --short --branch` |
| ISC-2 to ISC-4 | file | Verify artifacts exist | title present | Read/Grep |
| ISC-5 to ISC-11 | content | Verify design and workflow decisions | required wording present | Read/Grep |
| ISC-12 | review routing | Resolve Cato route | Vertex Opus 4.8 selected | `AgentHarnessRouter.ts --dry-run --json` |
| ISC-13 to ISC-13.3 | review | Run Cato audit and resolve findings | pass or all concerns reflected in plan | Cato output JSONL + Read |
| ISC-14 | docs | Active docs validate | exit 0 | `dotnet toolchain.cs -- docs-qa` |
| ISC-15 | whitespace | Diff has no whitespace errors | exit 0 | `git diff --check` |
| ISC-16 to ISC-18 | git scope | Diff contains only intended docs | no `src/` paths | `git diff --name-only` |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Branch setup | Create/switch to `yubikit-composite-device-new` from current consolidation HEAD. | ISC-1 | Phase 32 commit | false |
| Master ISA | Write `docs/plans/composite-device/ISA.md` with goals, criteria, test strategy, phases, decisions, and deferred item. | ISC-2, ISC-5, ISC-6, ISC-7, ISC-8, ISC-9, ISC-10, ISC-11 | Branch setup | false |
| Phase 33 artifacts | Write this ISA and the Phase 33 learning note. | ISC-3, ISC-4, ISC-17, ISC-18 | Master ISA | false |
| Cato review | Route and run cross-vendor Cato review of the plan. | ISC-12, ISC-13 | artifacts complete | false |
| Verification and commit | Run docs QA, whitespace, scope check, stage intended files, and commit. | ISC-14, ISC-15, ISC-16 | Cato resolved | false |

## Decisions

- 2026-06-09: Phase 33 is docs-only and exists to prevent broad unreviewed source churn.
- 2026-06-09: Later source phases will use `/DevTeam` review/fix/commit workflow, but Phase 33 uses Cato docs-plan review rather than implementation review.
- 2026-06-09: The composite-device program receives a new plan directory, `docs/plans/composite-device/`, instead of extending module-consolidation phase numbering in the old directory.
- 2026-06-09: The program records a deferred downstream audit because Core `DeviceInfo` may unlock useful capabilities beyond the minimum physical-device model.
- 2026-06-09: Initial Cato review returned concerns on namespace/API migration, scalar `IYubiKey.ConnectionType`, extension-method assumptions, and mandatory consumer migration; these concerns must be reflected before commit.

## Changelog

- conjectured: The next action after approval should be implementing `IYubiKey` changes.
  refuted by: The owner asked to split the effort into logical professional phases and begin with plan artifacts.
  learned: Source implementation must wait behind a composite-device program ISA, Phase 33 review, and a commit.
  criterion now: ISC-2 through ISC-18 define docs-only Phase 33 completion.

## Verification

Verification evidence is recorded in the Phase 33 learning note before commit.
