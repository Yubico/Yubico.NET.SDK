# Phase 22 Learnings: Tests.Shared Recorder And Harness Decision

Use this note as the handoff record for Phase 22 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Scope: Tests.Shared recorder/harness decision and narrow recorder promotion
- Phase ISA: `docs/plans/module-consolidation/phase-22-tests-shared-recorder-harness-decision-ISA.md`
- Audit artifact: `docs/plans/module-consolidation/phase-22-tests-shared-recorder-harness-decision-audit.md`
- Source files changed: Tests.Shared helper and affected unit-test project references/usages
- Test files changed: PIV, OATH, and SecurityDomain unit tests now use the shared recorder
- Integration tests: not run; Phase 22 does not change hardware behavior or integration-test allow-list/lazy-binding flow
- Result: recorder promotion accepted, reviewed, and locally verified
- Commit: finalized by the Phase 22 repository commit
- `/Ping` status: not required for Phase 22 completion

## What Changed

- Added `RecordingSmartCardConnection` to `Tests.Shared` as a small xUnit-free SmartCard APDU recorder.
- Replaced the private duplicate recorder classes in PIV, OATH, and SecurityDomain unit tests.
- Added explicit `Tests.Shared` references to the three affected xUnit v3 unit-test projects.
- Marked Tests.Shared xUnit v2 dependencies as `PrivateAssets="all"` so they do not flow into xUnit v3 unit-test consumers.
- Documented the recorder in Tests.Shared README/CLAUDE as a byte-level unit-test helper, not an integration-test hardware abstraction.
- Recorded the source-backed decision in the Phase 22 audit artifact.

## Key Findings

- The recorder duplication was real and current: PIV, OATH, and SecurityDomain each had nearly identical private `RecordingSmartCardConnection` plus `NullDisposable` implementations.
- Directly referencing `Tests.Shared` from xUnit v3 unit-test projects initially caused `FactAttribute`/`TheoryAttribute` ambiguity because xUnit v2 dependencies flowed transitively.
- `PrivateAssets="all"` on `xunit.core`, `xunit.abstractions`, and `Xunit.SkippableFact` fixed the xUnit v2/v3 boundary while preserving integration projects, which already directly reference xUnit v2.
- Core's existing `FakeSmartCardConnection`/`FakeApduProcessor` remain separate because they serve Core protocol-pipeline tests and are not duplicate module-session recorders.

## Deferred Future Improvements

- If Core and module recorder shapes converge later, evaluate naming/semantics in a dedicated test-support phase.
- Phase 23 can use `RecordingSmartCardConnection` for PIV byte-level coverage without adding another private fake.
- No integration harness policy changes are needed from Phase 22.

## Verification Evidence

- Initial dependency probe: adding `Tests.Shared` references to xUnit v3 unit tests failed with xUnit v2/v3 `FactAttribute` ambiguity before `PrivateAssets="all"` was applied.
- Build: `dotnet toolchain.cs -- build --project Tests.Shared` succeeded with 0 warnings and 0 errors.
- Build: `dotnet toolchain.cs -- build --project Oath` succeeded with 0 warnings and 0 errors after the dependency fix.
- Build: `dotnet toolchain.cs -- build --project SecurityDomain` succeeded with 0 warnings and 0 errors after the dependency fix.
- Build: `dotnet toolchain.cs -- build --project Piv` succeeded with 0 warnings and 0 errors after the dependency fix.
- Unit tests: `dotnet toolchain.cs -- test --project Oath` passed 81/81.
- Unit tests: `dotnet toolchain.cs -- test --project SecurityDomain` passed 28/28.
- Unit tests: `dotnet toolchain.cs -- test --project Piv` passed 61/61.
- Docs QA: `dotnet toolchain.cs -- docs-qa` succeeded and validated 54 active documentation files.
- Whitespace: `git diff --check` passed; Git emitted line-ending normalization warnings for touched files but no whitespace errors.
- Source check: grep found no private `RecordingSmartCardConnection` or `NullDisposable` left in the three adopted unit-test directories.
- Source check: repo-wide grep found exactly one `RecordingSmartCardConnection` definition, in `src/Tests.Shared/RecordingSmartCardConnection.cs`.

## Review Evidence

- DevTeam route: `google-vertex-anthropic/claude-opus-4-8@default` via `opencode run`.
- DevTeam output: `/tmp/opencode/phase22-devteam-review.jsonl`.
- DevTeam verdict: `pass`.
- Findings resolved: no material findings. Info-only notes were accepted; the audit now explicitly says Core's existing fake SmartCard helpers are intentionally excluded from this promotion.

## Integration Lifecycle

- Hardware target: connected YubiKey 5.8 beta key remains available for later phases.
- Phase 22 Management preflight: not applicable; no hardware or applet runtime behavior changed.
- Integration scope: none.
- Persistent state changed: no.
- Destructive tests: none.

## Cross-Module Implications

- PIV, OATH, and SecurityDomain now share one byte-level SmartCard recorder for unit tests.
- Tests.Shared remains safe for integration tests because allow-list, lazy binding, and `[WithYubiKey]` behavior were not changed.
- Future applet byte-level coverage should prefer the shared recorder before adding module-local queue-and-record fakes.

## Compact Summary

- Goal: decide and implement Tests.Shared recorder promotion.
- Files changed: Tests.Shared helper/docs/project file, three unit-test project files, three unit-test source files, Phase 22 ISA/audit/learnings.
- Final pattern: small xUnit-free recorder, xUnit v2 deps private, module tests consume shared helper.
- Rejected approaches: broad fake APDU DSL, integration harness rewrite, leaving three private copies.
- Tests passed: affected builds plus OATH, SecurityDomain, and PIV unit suites.
- Integration lifecycle: none; no hardware behavior changed.
- Shared/Core candidates: Core fake convergence remains deferred.
- Deferred future improvements: use recorder in Phase 23 PIV byte-level coverage.
- House-style update needed: none now.
- Next phase recommendation: Phase 23 PIV byte-level coverage.
- Learning note path: `docs/plans/module-consolidation/phase-22-tests-shared-recorder-harness-decision-learnings.md`
- Commit: finalized by the Phase 22 repository commit.
- `/Ping` status: not required for Phase 22 completion.
