# Phase 16 Learnings: API And Package Compatibility Checkpoint

Use this note as the handoff record for Phase 16 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Scope: package/API compatibility checkpoint after source-risk phases
- Phase ISA: `docs/plans/module-consolidation/phase-16-api-package-compatibility-ISA.md`
- Checkpoint artifact: `docs/plans/module-consolidation/phase-16-api-package-compatibility-checkpoint.md`
- Source files changed: `toolchain.cs`
- Test files changed: none
- Integration tests: not run; Phase 16 is package/API tooling and docs only
- Result: package surface audited, `toolchain.cs pack` filtered to actually packable projects, pack verification passed
- Commit: recorded by the Phase 16 commit containing this learning note
- `/Ping` status: pending

## What Changed

- Added a Phase 16 ISA and compatibility checkpoint artifact.
- Classified `ConnectionType` numeric changes as an accepted v2 preview breaking-risk item that needs release-note treatment.
- Recorded additive or low-risk changes from firmware/support-gate phases separately from breaking-risk changes.
- Recorded that package/API compatibility enforcement is not yet configured because no baseline package/policy exists.
- Fixed `toolchain.cs pack` discovery so projects marked `<IsPackable>false</IsPackable>` are excluded from pack targets.

## Why This Shape

- The phase is a checkpoint, not an API redesign phase.
- Enabling package validation without an approved baseline would create noise or false confidence.
- Filtering pack targets is safe and directly tied to Phase 16 because it makes package verification reflect the real package surface.

## Verification Evidence

- Branch check command: `git status --short --branch`
- Branch check result: `## yubikit-consolidation`
- Initial pack command: `dotnet toolchain.cs pack --package-version 2.0.0-preview.phase16`
- Initial pack result: succeeded, but attempted non-packable CLI/test projects before producing 10 SDK packages.
- Tooling fix: `toolchain.cs` filters discovered pack targets through whitespace/attribute-tolerant project-file `<IsPackable>false</IsPackable>` markers while preserving the broader source-project set for `build --project`.
- Final pack command: `dotnet toolchain.cs pack --package-version 2.0.0-preview.phase16`
- Final pack result: passed; only the 10 SDK package projects were packed, with 10 packages created.
- Review follow-up build command: `dotnet toolchain.cs -- build --project YkTool`
- Review follow-up build result: passed; `build --project` still uses the broader source-project discovery set.
- Review follow-up pack command: `dotnet toolchain.cs -- pack --package-version 2.0.0-preview.phase16`
- Review follow-up pack result: passed; only the 10 SDK package projects were packed.

## Integration Lifecycle

- Hardware target: not used.
- Management preflight: not applicable.
- Integration scope was read-only: not applicable.
- Tests run: none.
- Tests skipped: all hardware/integration checks.
- Skip reason: Phase 16 does not touch applet behavior.
- Persistent state changed: no.
- Destructive tests skipped completely: yes.
- Reset/cleanup performed: none.

## Review Evidence

- DevTeam route: Vertex Opus 4.8 via `google-vertex-anthropic/claude-opus-4-8@default`.
- DevTeam output: `/tmp/opencode/devteam-phase16-review.jsonl`
- DevTeam verdict: `PASS WITH NOTES`.
- DevTeam low finding: literal-string `<IsPackable>false</IsPackable>` matching was fragile.
- Resolution: switched to a whitespace/attribute-tolerant regex.
- DevTeam low finding: checkpoint inventory underreported non-packable projects by mentioning only CLI support projects.
- Resolution: checkpoint now records CLI support/tool and test/support project groups.
- DevTeam low finding: filtering the shared `packableProjects` variable narrowed `build --project` discovery.
- Resolution: split `buildableProjects` from `packableProjects` so only pack/publish targets are filtered.

## Deferred Future Improvements

- Choose an API/package compatibility baseline before a stable release candidate.
- Add release notes for approved v2 preview public enum changes, especially `ConnectionType` numeric semantics.
- Resolve `AddYubiKeyManagerCore()` active documentation drift by either restoring source support or removing stale docs/comments.
- Decide whether package readmes should be attached to NuGet packages before release.

## Cross-Module Implications

- Package verification now reflects the actual SDK package surface instead of non-packable tests/tools.
- CLI shared/commands remain non-packable support projects.
- Tests.Shared and test projects should remain out of package verification unless explicitly made packable later.

## Generalization Check

- Pattern classification: package verification should use actual packability, not path/name alone.
- Reusable lesson: checkpoint phases can legitimately fix tooling that makes the checkpoint noisy or misleading.
- Not promoted to shared code: no application code changed.

## Compact Summary

- Goal: verify package/API compatibility posture after source-risk phases.
- Files changed: `toolchain.cs`, Phase 16 ISA, checkpoint artifact, learning note.
- Final pattern: inspect compatibility risk, fix noisy pack discovery, defer baseline enforcement.
- Rejected approaches: blind `EnablePackageValidation`, public API cleanup, release-baseline invention.
- Tests passed: `dotnet toolchain.cs pack --package-version 2.0.0-preview.phase16`.
- Integration lifecycle: skipped; package checkpoint only.
- Shared/Core candidates: Core DI docs/API drift remains high-priority follow-up.
- Deferred future improvements: package validation baseline, release notes, package readmes.
- House-style update needed: none now.
- Next phase recommendation: Phase 17 test runner and hardware coordination.
- Learning note path: `docs/plans/module-consolidation/phase-16-api-package-compatibility-learnings.md`
- Commit: recorded by Phase 16 commit.
- `/Ping` status: pending
