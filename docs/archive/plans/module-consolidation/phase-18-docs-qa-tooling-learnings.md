# Phase 18 Learnings: Docs QA Tooling

Use this note as the handoff record for Phase 18 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Scope: bounded active-doc QA target and active-doc repairs exposed by that target
- Phase ISA: `docs/plans/module-consolidation/phase-18-docs-qa-tooling-ISA.md`
- Source files changed: `toolchain.cs`
- Active docs changed: `TOOLCHAIN.md`, root/module active markdown files with stale local links
- Test files changed: none
- Integration tests: not run; Phase 18 is docs/tooling only
- Result: `docs-qa` target added and passing on 54 active documentation files
- Commit: recorded by the Phase 18 commit containing this learning note
- `/Ping` status: pending

## What Changed

- Added standalone `docs-qa` target to `toolchain.cs`.
- Added active documentation discovery for root docs, top-level `docs/*.md`, selected active docs subdirectories, and module `README.md`/`CLAUDE.md` files.
- Explicitly excluded archived/planning/research/spec/template material from the active-doc gate.
- Added validation for balanced fenced code blocks.
- Added validation for local markdown links outside fenced code examples that should resolve to files or directories.
- Added validation for known stale FIDO2 User Presence trait patterns.
- Documented `docs-qa` in `TOOLCHAIN.md`, including the scan boundary and snippet-compilation non-goal.
- Repaired active-doc link drift exposed by the first `docs-qa` run.

## Why This Shape

- A standalone target gives maintainers an executable guard without changing CI/default behavior mid-consolidation.
- Link/fence/stale-pattern checks are deterministic and fast enough to run often.
- Archived plans and research are intentionally out of scope because they preserve historical state and would create noisy cleanup work unrelated to active SDK guidance.
- README snippets are not compiled because snippet extraction/compilation requires a larger design decision about partial examples, expected setup, hardware requirements, and package references.

## Verification Evidence

- Branch check command: `git status --short --branch`
- Branch check result: `## yubikit-consolidation`
- Initial docs QA command: `dotnet toolchain.cs -- docs-qa`
- Initial docs QA result: failed with 15 active-doc link issues.
- Active-doc repairs: updated stale root build link, stripped-module directory links, test-doc `docs/TESTING.md` relative paths, and `WithManagementAsync` shared-helper path.
- Final docs QA command: `dotnet toolchain.cs -- docs-qa`
- Final docs QA result: passed; validated 54 active documentation files.
- Post-review docs QA command: `dotnet toolchain.cs -- docs-qa`
- Post-review docs QA result: passed; validated 54 active documentation files.
- Formatting command: `dotnet format --verify-no-changes --include "toolchain.cs"`
- Formatting result: passed.
- Whitespace command: `git diff --check`
- Whitespace result: passed.

## Integration Lifecycle

- Hardware target: not used.
- Management preflight: not applicable.
- Integration tests run: none.
- User Presence / UV / touch / reset / insert-remove tests: none.
- Persistent state changed: no.
- Skip reason: Phase 18 changes documentation and the documentation validation target only.

## Review Evidence

- DevTeam route: Vertex Opus 4.8 via `google-vertex-anthropic/claude-opus-4-8@default`.
- Initial DevTeam output: `/tmp/opencode/devteam-phase18-review.jsonl`
- Initial DevTeam verdict: `PASS_WITH_NOTES`.
- Initial low finding: local markdown link validation checked fenced code examples as live links, creating a latent false-positive risk.
- Resolution: `ValidateLocalMarkdownLinks(...)` now tracks fenced-code state and skips local link validation inside fenced code examples.
- Non-regression: stale FIDO2 User Presence trait pattern validation still scans all lines, including code examples.
- DevTeam re-review attempts before Vertex reauth: `/tmp/opencode/devteam-phase18-rereview.jsonl` and `/tmp/opencode/devteam-phase18-rereview-2.jsonl` failed with `invalid_grant` / `invalid_rapt` Vertex reauth errors.
- DevTeam re-review output after Vertex reauth: `/tmp/opencode/devteam-phase18-rereview-3.jsonl`
- DevTeam re-review verdict: `PASS` with no findings.

## Deferred Future Improvements

- Decide whether to add `docs-qa` to CI after the consolidation branch settles.
- Design a separate README snippet compilation/extraction phase if executable examples become a release requirement.
- Consider adding anchor validation after active docs converge on stable heading conventions.
- Consider validating command examples for `dotnet toolchain.cs --` separator consistency in a future docs pass.

## Cross-Module Implications

- Module `README.md` and `CLAUDE.md` files are now covered by active-doc link/fence/stale-pattern validation.
- Module directory links must use the current stripped directory names under `src/` rather than old `Yubico.YubiKit.*` folder names.
- Test CLAUDE files now link to `../../../docs/TESTING.md` from `src/<Module>/tests/`.

## Generalization Check

- Pattern classification: docs QA should begin with cheap structural invariants and avoid broad style policing.
- Reusable lesson: executable docs checks expose real drift immediately, but their scan boundary must be explicit to avoid accidental archive cleanup.
- Not promoted to shared code: docs QA lives in the existing toolchain script.

## Compact Summary

- Goal: add bounded active-doc validation.
- Files changed: `toolchain.cs`, `TOOLCHAIN.md`, active docs with stale links, Phase 18 ISA/learning note.
- Final pattern: standalone `docs-qa` validates active docs only.
- Rejected approaches: CI wiring, snippet compilation, archived docs cleanup.
- Tests passed: `dotnet toolchain.cs -- docs-qa`.
- Integration lifecycle: skipped; docs/tooling only.
- Shared/Core candidates: none.
- Deferred future improvements: CI, snippet compilation, anchor validation, separator consistency.
- House-style update needed: none now.
- Next phase recommendation: Phase 19 addendum and final Cato.
- Learning note path: `docs/plans/module-consolidation/phase-18-docs-qa-tooling-learnings.md`
- Commit: recorded by Phase 18 commit.
- `/Ping` status: pending
