# Phase 17 Learnings: Test Runner And Hardware Coordination

Use this note as the handoff record for Phase 17 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Scope: test-runner focused-filter behavior and FIDO2/WebAuthn hardware coordination lanes
- Phase ISA: `docs/plans/module-consolidation/phase-17-test-runner-hardware-coordination-ISA.md`
- Coordination artifact: `docs/plans/module-consolidation/phase-17-test-runner-hardware-coordination.md`
- Active docs changed: `docs/TESTING.md`, `src/Fido2/CLAUDE.md`, `src/Fido2/tests/CLAUDE.md`, `src/WebAuthn/CLAUDE.md`
- Source files changed: none
- Test files changed: none
- Integration tests: no UP/UV/touch/reset/PIN/destructive hardware tests run
- Result: focused xUnit v3 behavior documented, active trait guidance corrected, FIDO2/WebAuthn human-coordination lanes recorded
- Commit: recorded by the Phase 17 commit containing this learning note
- `/Ping` status: pending

## What Changed

- Added a Phase 17 ISA and hardware coordination artifact.
- Documented xUnit v3/Microsoft.Testing.Platform positive-filter preflight behavior.
- Recorded that xUnit v3 no-match preflight marks individual projects as `no matching tests`, while all-selected xUnit v3 preflight no-match fails clearly with `No tests matched the specified filter`.
- Corrected FIDO2 active documentation from old `RequiresUserPresence=true` trait examples to current `TestCategories.Category` / `TestCategories.RequiresUserPresence` semantics.
- Added FIDO2/WebAuthn lane classifications for read-only smoke, User Presence, User Verification/PIN, reset/destructive, and insert/remove/touch timing checks.
- Marked `Category=RequiresUserPresence` commands as human-coordinated only.

## Why This Shape

- Phase 17 is a coordination and documentation phase, not a test infrastructure rewrite.
- The existing toolchain behavior is useful: focused filters can avoid running non-matching xUnit v3 projects, but an all-no-match focused run still fails instead of silently passing.
- FIDO2/WebAuthn value comes from making human-only hardware boundaries explicit, not from adding unattended gates that agents cannot satisfy safely.
- Active docs are the right place for this phase because agents read them before touching FIDO2/WebAuthn tests.

## Verification Evidence

- Branch check command: `git status --short --branch`
- Branch check result: `## yubikit-consolidation`
- Initial focused command without separator: `dotnet toolchain.cs test --project Fido2 --filter "Method~ExtensionBuilder"`
- Initial focused command result: failed with `Cannot use the --project and --file options together`; Phase 17 command shapes use the `--` separator where needed.
- Focused xUnit v3 command: `dotnet toolchain.cs -- test --project Fido2 --filter "Method~ExtensionBuilder"`
- Focused xUnit v3 result: passed; `Yubico.YubiKit.Fido2.UnitTests` ran 20 tests, 0 failures.
- All-no-match xUnit v3 command: `dotnet toolchain.cs -- test --project Fido2 --filter "Method~DefinitelyNoPhase17Match"`
- All-no-match xUnit v3 result: failed clearly with `No tests matched the specified filter`; docs now record this as intended xUnit v3 preflight behavior.
- Mixed unit+integration command: `dotnet toolchain.cs -- test --integration --project Fido2 --filter "Method~ExtensionBuilder"`
- Mixed unit+integration result: passed; unit project ran the 20 matching FIDO2 tests, integration project discovered no matching tests, and no hardware test bodies executed.
- Formatting command: `dotnet format --verify-no-changes --include "toolchain.cs"`
- Formatting result: passed.
- Whitespace command: `git diff --check`
- Whitespace result: passed.

## Integration Lifecycle

- Hardware target: not used.
- Management preflight: not applicable; no applet behavior or hardware state was exercised.
- Integration scope was read-only: discovery/build only for a non-matching FIDO2 integration filter.
- User Presence tests run: none.
- User Verification/PIN tests run: none.
- Reset/destructive tests run: none.
- Insert/remove/touch timing tests run: none.
- Persistent state changed: no.
- Skip reason: Phase 17 defines coordination lanes and verifies runner behavior; it does not require hardware ceremonies.

## Review Evidence

- DevTeam route: Vertex Opus 4.8 via `google-vertex-anthropic/claude-opus-4-8@default`.
- Initial DevTeam output: `/tmp/opencode/devteam-phase17-review.jsonl`
- Initial DevTeam verdict: `PASS_WITH_NOTES`.
- Initial low finding: Phase 17 ISA active-doc list omitted `src/Fido2/CLAUDE.md` even though it was modified.
- Resolution: added `src/Fido2/CLAUDE.md` to the active-doc list.
- Initial info note: all-no-match wording should be xUnit-v3-preflight-specific because v2 projects do not use the MTP preflight path.
- Resolution: tightened the ISA, artifact, and `docs/TESTING.md` wording to selected xUnit v3 projects preflighted by positive filters.
- DevTeam re-review output: `/tmp/opencode/devteam-phase17-rereview.jsonl`
- DevTeam re-review verdict: `PASS` with no findings.

## Deferred Future Improvements

- Consider whether `dotnet toolchain.cs` examples should consistently use the `--` separator when passing `--project`, because the no-separator form can conflict with `dotnet` host argument parsing.
- Consider a future toolchain help/docs pass that documents xUnit v2 no-match behavior separately from xUnit v3 MTP preflight behavior.
- Consider adding a small docs-only checklist for human-coordinated FIDO2/WebAuthn runs that records serial, PIN state, allowed mutation, and approval.

## Cross-Module Implications

- FIDO2 and WebAuthn now share the same UP/UV coordination language.
- `Category=RequiresUserPresence` is the active trait/filter shape for both modules.
- Integration smoke remains the agent-runnable default because `--smoke` injects `Category!=Slow&Category!=RequiresUserPresence`.

## Generalization Check

- Pattern classification: hardware-human interaction should be represented as explicit lanes, not hidden in ad-hoc test comments.
- Reusable lesson: focused-filter docs must describe both useful no-match skips and intentional all-no-match failures.
- Not promoted to shared code: no source-code changes were needed for Phase 17.

## Compact Summary

- Goal: close test-runner and hardware-coordination governance gaps.
- Files changed: active testing docs plus Phase 17 ISA, artifact, learning note.
- Final pattern: xUnit v3 preflight behavior documented; UP/UV checks human-coordinated.
- Rejected approaches: source rewrite, unattended UP/UV gates, destructive hardware tests.
- Tests passed: focused FIDO2 xUnit v3 filter and mixed unit+integration focused filter.
- Integration lifecycle: no hardware bodies executed; no state changed.
- Shared/Core candidates: none.
- Deferred future improvements: separator examples, v2 no-match docs, human-run checklist.
- House-style update needed: none now.
- Next phase recommendation: Phase 18 docs QA tooling.
- Learning note path: `docs/plans/module-consolidation/phase-17-test-runner-hardware-coordination-learnings.md`
- Commit: recorded by Phase 17 commit.
- `/Ping` status: pending
