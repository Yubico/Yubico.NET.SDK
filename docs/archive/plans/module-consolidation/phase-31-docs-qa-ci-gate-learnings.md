# Phase 31 Learnings: Docs QA CI Gate

Use this note as the handoff record for Phase 31 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`.
- Scope: add the existing docs QA target to the primary CI workflow.
- Phase ISA: `docs/plans/module-consolidation/phase-31-docs-qa-ci-gate-ISA.md`.
- Workflow changed: `.github/workflows/build.yml` now runs `dotnet toolchain.cs -- docs-qa` as `Validate active documentation`.
- Source changed: none.
- Toolchain changed: none.
- Docs changed: Phase 31 ISA and this learning note.

## Source Audit

- `.github/workflows/build.yml` already has one `build-and-test` job that checks out source, sets up .NET 10, caches NuGet packages, installs PC/SC dependencies, builds, runs unit tests, packs, and publishes on push.
- `toolchain.cs` already defines `docs-qa` as a Bullseye target that validates active documentation files.
- `DiscoverActiveDocumentationFiles()` includes root markdown, top-level docs, selected active docs subtrees, and module README/CLAUDE files.
- `IsArchivedOrPlanningDoc()` excludes planning, archived, completed, research, review, spec, and template docs, so CI validation remains bounded to active docs.

## What Changed

- Added a `Validate active documentation` workflow step after .NET setup/cache.
- The step runs `dotnet toolchain.cs -- docs-qa`.
- The step runs before PC/SC dependency installation, build, tests, pack, and publish.
- Branch triggers, workflow permissions, package publishing, build/test commands, and PC/SC setup were not changed.

## Why This Shape

- Reusing `toolchain.cs` keeps CI behavior identical to local agent/developer verification.
- Placing the step before PC/SC install makes documentation failures fast and independent of hardware-library setup.
- Keeping the gate inside the existing workflow avoids unnecessary CI topology changes.
- Leaving `toolchain.cs` unchanged prevents Phase 31 from expanding into documentation policy changes.

## Verification Evidence

- Branch check command: `git status --short --branch`.
- Branch check result: `## yubikit-consolidation...origin/yubikit-consolidation [ahead 10]` before workflow edits.
- Local docs QA command: `dotnet toolchain.cs -- docs-qa`.
- Local docs QA result: passed; 54 active documentation files validated.
- Workflow source inspection: added step is correctly indented under `jobs.build-and-test.steps` and appears before PC/SC install, build, tests, pack, and publish.
- Diff whitespace command: `git diff --check`.
- Diff whitespace result: passed with no output.
- Local YAML linter check: `actionlint` was not available, so workflow syntax evidence is source inspection plus DevTeam review.
- DevTeam review route: `openai/gpt-5.5` primary routed reviewer to `google-vertex-anthropic/claude-opus-4-8@default`.
- DevTeam review result: `PASS`; low ISA evidence-staleness notes only and no required workflow changes.

## What Did Not Work

- A local `actionlint` binary was not available, so this phase did not run a dedicated GitHub Actions YAML linter.
- The first DevTeam review saw unchecked ISA criteria and pending verification text because review ran before final evidence capture; those documentation notes were resolved after review.

## Reusable Patterns

- CI gates should call repository toolchain targets rather than duplicating validation logic in workflow YAML.
- Put cheap validation gates before environment setup when they do not depend on that setup.
- Keep CI changes minimal in consolidation phases: one bounded step is easier to review than a workflow topology rewrite.

## Deferred Candidates

- Add `actionlint` or an equivalent workflow syntax check to local tooling if GitHub Actions workflow edits become frequent.
- Consider moving docs QA immediately after checkout only if future measurement shows package cache setup dominates failure latency; current placement is safe and simple.

## Next Phase Inputs

- Required reading before next phase: this learning note.
- Phase 32 is the same-criteria quality reassessment from the Phase 20 program.
- Phase 32 should regrade active surfaces with the original metrics, exclude `Tests.TestProject`, and record whether the composite-readiness gate passed.
- Phase 32 must stop before composite YubiKey interviews/design even if the quality gate passes.

## Compact Summary

- Goal: make active-doc QA a CI gate.
- Main fix: add `dotnet toolchain.cs -- docs-qa` workflow step.
- Source/toolchain: unchanged.
- Verification: local docs QA, diff check, source inspection, and DevTeam review passed.
