# Phase 16 ISA: API And Package Compatibility Checkpoint

## Problem

The consolidation branch has accumulated source-risk changes across Core, applet modules, CLI support, tests, and docs. Before more tooling and documentation phases, the branch needs a compatibility checkpoint that distinguishes real package/API risk from acceptable v2 preview churn.

## Vision

The branch should have a sober package-facing risk record: packable projects are known, package metadata is verified, public API concerns are named, and compatibility enforcement gaps are explicit rather than rediscovered during release prep.

## Out Of Scope

- No broad public API redesign.
- No source refactor for compatibility polish unless a release-blocking issue is found.
- No changing package IDs or target frameworks.
- No comparing against external NuGet packages unless credentials/feed state already exists.
- No hardware or integration testing.

## Constraints

- Execute only on branch `yubikit-consolidation`.
- Use repository toolchain commands, not raw `dotnet build` or `dotnet test`.
- Preserve the original baseline assessment.
- Keep this phase reviewable: checkpoint artifact and any minimal package metadata guard only.
- Leave unrelated untracked files unstaged.

## Goal

Create a Phase 16 package/API compatibility checkpoint that verifies the current packable surface can build and pack, records compatibility risks after phases 1-15, and defines concrete next release-readiness actions without broadening the consolidation program.

## Criteria

- [x] ISC-1: Branch check shows `## yubikit-consolidation` before Phase 16 work.
- [x] ISC-2: Phase 16 artifact records all current packable projects discovered from `src/*/src/*.csproj`.
- [x] ISC-3: Phase 16 artifact records central package metadata and dependency-version posture from `Directory.Build.props` and `Directory.Packages.props`.
- [x] ISC-4: Phase 16 artifact records public API compatibility risk from Phase 12 `ConnectionType` numeric changes.
- [x] ISC-5: Phase 16 artifact records additive public API changes from Phase 13/14/15 separately from breaking-risk items.
- [x] ISC-6: Phase 16 artifact records package-validation/API-compat enforcement status.
- [x] ISC-7: Pack verification command is run and recorded, or a concrete failure is recorded with follow-up.
- [x] ISC-8: Phase 16 explicitly decides whether to add package-validation enforcement now or defer it.
- [x] ISC-9: DevTeam cross-vendor review runs for the Phase 16 artifact and any changed files.
- [x] ISC-10: Phase 16 learning note exists.
- [x] ISC-11: `git diff --check` passes.
- [x] ISC-12: Anti: Phase 16 changes public APIs while attempting only to audit compatibility.
- [x] ISC-13: Anti: Phase 16 claims package/API compatibility is fully enforced without a baseline or validation gate.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | branch | `git status --short --branch` | shows `## yubikit-consolidation` | bash |
| ISC-2 | content | read checkpoint artifact | packable project list present | read |
| ISC-3 | content | read checkpoint artifact | package metadata/dependency posture present | read |
| ISC-4 | content | read checkpoint artifact | `ConnectionType` risk recorded | read |
| ISC-5 | content | read checkpoint artifact | additive changes separated | read |
| ISC-6 | content | read checkpoint artifact | enforcement status present | read |
| ISC-7 | command | `dotnet toolchain.cs pack --package-version 2.0.0-preview.phase16` | exit 0 or recorded failure | bash |
| ISC-8 | content | read checkpoint artifact | enforce/defer decision present | read |
| ISC-9 | review | DevTeam output | pass or findings resolved/deferred | bash/read |
| ISC-10 | file | read learning note | file exists with title | read |
| ISC-11 | whitespace | `git diff --check` | exit 0 | bash |
| ISC-12 | git | `git diff --name-only` | no public source API files changed | bash |
| ISC-13 | content | read checkpoint artifact | no false full-enforcement claim | read |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Package surface inventory | Record packable projects and metadata/dependency posture. | ISC-2, ISC-3 | branch check | true |
| Compatibility risk classification | Classify breaking-risk, additive, documentation, and enforcement gaps. | ISC-4, ISC-5, ISC-6, ISC-8, ISC-13 | inventory | true |
| Pack verification | Run package creation through repository toolchain. | ISC-7 | inventory | false |
| DevTeam review and learning | Cross-vendor review, learning note, commit. | ISC-9, ISC-10, ISC-11, ISC-12 | artifact | false |

## Decisions

- 2026-06-07: Phase 16 is a checkpoint phase, not a public API cleanup phase.
- 2026-06-07: Package validation enforcement requires a stable baseline package or explicit release policy, so this phase may document the gap rather than enabling a noisy gate prematurely.

## Verification

- ISC-1: `git status --short --branch` showed `## yubikit-consolidation`.
- ISC-2: `docs/plans/module-consolidation/phase-16-api-package-compatibility-checkpoint.md` records the 10 SDK package projects and explicitly non-packable CLI projects.
- ISC-3: The checkpoint records package metadata from `Directory.Build.props` and central dependency posture from `Directory.Packages.props`.
- ISC-4: The checkpoint records Phase 12 `ConnectionType` numeric changes as an accepted v2 preview breaking-risk item.
- ISC-5: The checkpoint separates additive/low-risk changes from breaking-risk changes.
- ISC-6: The checkpoint records that no package-validation baseline, API compatibility baseline, package lock, or `EnablePackageValidation` gate is configured.
- ISC-7: `dotnet toolchain.cs pack --package-version 2.0.0-preview.phase16` passed after Phase 16 filtered pack targets to actual packable projects; 10 packages were created.
- ISC-8: Phase 16 defers package-validation enforcement until a release baseline/package policy is chosen.
- ISC-9: DevTeam review routed to Vertex Opus 4.8 via `google-vertex-anthropic/claude-opus-4-8@default`; verdict was `PASS WITH NOTES`.
- ISC-10: Readback confirmed `docs/plans/module-consolidation/phase-16-api-package-compatibility-learnings.md` exists.
- ISC-11: `git diff --check` passed.
- ISC-12: Phase 16 changed package tooling and docs, not public API source files.
- ISC-13: The checkpoint explicitly says package/API compatibility is not fully enforced yet.
- Review follow-up: `dotnet toolchain.cs -- build --project YkTool` passed, proving Phase 16 preserved the broader build-project discovery set.
- Review follow-up: `dotnet toolchain.cs -- pack --package-version 2.0.0-preview.phase16` passed, proving pack targets remain filtered to 10 SDK packages after the regex hardening.
