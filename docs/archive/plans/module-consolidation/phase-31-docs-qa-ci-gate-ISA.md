# Phase 31 Docs QA CI Gate ISA

## Problem

Phase 18 added the `docs-qa` toolchain target and later phases have used it as local verification, but CI still runs build, unit tests, pack, and publish without validating active documentation. Phase 20 names Phase 31 as the bounded step that wires `dotnet toolchain.cs -- docs-qa` into CI before the final same-criteria reassessment.

Without CI coverage, stale active docs can re-enter after local phase verification. The gate should validate only active docs, preserve the existing workflow shape, and avoid broad documentation policy changes during this phase.

## Vision

CI should reject active documentation hygiene regressions with the same toolchain command agents and developers already run locally. The workflow should remain simple: checkout, setup, cache/dependencies, docs QA, build, tests, pack, publish. Planning, archive, spec, and review documents remain excluded by the existing `toolchain.cs` active-doc discovery rules.

## Out of Scope

- No changes to the active documentation validator unless the CI gate cannot run the existing target.
- No new GitHub Actions workflow file unless the existing build workflow cannot host the bounded gate.
- No branch trigger changes.
- No broad documentation cleanup.
- No source module changes.
- No composite YubiKey design work.

## Principles

- Use the existing toolchain target instead of duplicating doc validation logic in YAML.
- Keep the gate bounded to active docs as defined by `toolchain.cs`.
- Put the gate early enough to fail quickly, before build/test/pack work.
- Avoid changing publish semantics or package permissions.
- Capture verification and review evidence before committing.

## Constraints

- Branch must be `yubikit-consolidation` before edits and verification.
- Required phase inputs: master ISA, Phase 20 program ISA, Phase 30 learning note, `.github/workflows/build.yml`, and `toolchain.cs` docs-qa implementation.
- Use `dotnet toolchain.cs`; never raw `dotnet build` or `dotnet test`.
- Docs command: `dotnet toolchain.cs -- docs-qa`.
- Workflow syntax check should use source inspection if no local GitHub Actions runner exists.
- Stage only intended Phase 31 files.

## Goal

Add `dotnet toolchain.cs -- docs-qa` to CI as a bounded active-documentation gate and record evidence that the command passes locally and the workflow change is minimal.

## Criteria

- [x] ISC-1: Phase 31 ISA exists before workflow edits.
- [x] ISC-2: Branch check confirms `yubikit-consolidation` before workflow edits.
- [x] ISC-3: Required phase inputs and current CI/toolchain files were read.
- [x] ISC-4: CI workflow includes a bounded docs QA step that runs `dotnet toolchain.cs -- docs-qa`.
- [x] ISC-5: Docs QA step runs before build/test/pack to fail fast.
- [x] ISC-6: CI branch triggers, permissions, package publishing, and PC/SC setup semantics remain unchanged.
- [x] ISC-7: No `toolchain.cs` validator behavior changes are made unless required by CI verification.
- [x] ISC-8: Local docs QA passes through toolchain after the workflow edit.
- [x] ISC-9: Workflow YAML is source-inspected for valid placement and indentation.
- [x] ISC-10: Diff whitespace check passes.
- [x] ISC-11: DevTeam/cross-vendor review completes or an approved waiver is recorded.
- [x] ISC-12: Phase 31 learning note records findings, verification, and Phase 32 input.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | file | Read ISA path | Exists | Read |
| ISC-2 | git | `git status --short --branch` | Shows `yubikit-consolidation` | Bash |
| ISC-3 | file | Required input reads | Context loaded | Read/Grep |
| ISC-4 | YAML | Workflow docs QA step | Exact command present | Read/Grep |
| ISC-5 | YAML | Step ordering | Docs QA before Build | Read |
| ISC-6 | diff | Workflow non-goals unchanged | Only docs QA step added | git diff |
| ISC-7 | diff | Toolchain unchanged unless justified | No `toolchain.cs` diff | git diff |
| ISC-8 | command | Docs QA | 0 failures | Bash |
| ISC-9 | syntax | YAML indentation/source inspection | Valid step shape | Read |
| ISC-10 | whitespace | Diff whitespace | No errors | `git diff --check` |
| ISC-11 | review | DevTeam/cross-vendor review | Pass or approved waiver | DevTeam |
| ISC-12 | file | Learning note exists | Contains evidence | Read |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| CI Docs QA Step | Add a build-workflow step that runs the existing docs QA target before build. | ISC-4, ISC-5, ISC-6 | branch check | false |
| Phase Evidence | Run docs QA, diff checks, review, and learning capture. | ISC-8..ISC-12 | workflow edit | false |

## Decisions

- 2026-06-08: Use the existing `.github/workflows/build.yml` job instead of a separate workflow because Phase 20 names a CI gate, not a new CI topology.
- 2026-06-08: Add the docs QA step before build so documentation failures stop CI before expensive compile/test/pack work.
- 2026-06-08: Leave `toolchain.cs` active-doc discovery unchanged because it already excludes planning/archive/spec/review docs and validates 54 active documentation files locally.

## Verification

- Branch check: `git status --short --branch` showed `## yubikit-consolidation...origin/yubikit-consolidation [ahead 10]` before workflow edits.
- Required inputs read: master consolidation ISA, Phase 20 program ISA, Phase 30 learning note, `.github/workflows/build.yml`, and `toolchain.cs` docs-qa implementation.
- Workflow edit: `.github/workflows/build.yml` now adds `Validate active documentation` with `dotnet toolchain.cs -- docs-qa` after checkout/setup/cache and before PC/SC install, build, tests, pack, and publish.
- Workflow scope: branch triggers, job permissions, publish behavior, package commands, build/test commands, and PC/SC setup remain unchanged.
- Toolchain scope: `toolchain.cs` was not changed; active-doc boundedness remains owned by `DiscoverActiveDocumentationFiles()` and `IsArchivedOrPlanningDoc()`.
- Local docs QA command: `dotnet toolchain.cs -- docs-qa`.
- Local docs QA result: passed; 54 active documentation files validated.
- Workflow syntax evidence: source inspection confirmed the added step is correctly indented under `jobs.build-and-test.steps`; `actionlint` was not available locally.
- Diff whitespace command: `git diff --check`.
- Diff whitespace result: passed with no output.
- DevTeam review route: OpenAI primary routed reviewer to `google-vertex-anthropic/claude-opus-4-8@default`.
- DevTeam review result: `PASS`; low documentation-staleness notes only and no required workflow changes.
