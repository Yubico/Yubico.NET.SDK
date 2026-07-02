# Phase 2 Learnings: Tests.Shared Harness

Use this note as the handoff record for Phase 2 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Base branch: `yubikit-applets`
- Base commit: `bfc6bdd5`
- Branch check command/result: `git status --short --branch` showed `## yubikit-consolidation`
- Unrelated worktree changes present: yes, pre-existing untracked files under `src/Core/src/YubiKey/`
- Refactor work ran only on `yubikit-consolidation`: yes
- Scope: promote duplicated non-owning SmartCard connection wrappers from YubiHsm, OpenPgp, and SecurityDomain integration tests into `Tests.Shared`
- Criteria satisfied: shared wrapper promoted, duplicate helpers removed, app-specific session helpers stayed module-local, test-only dependency direction preserved, build/unit verification passed, Cato audit passed
- Criteria deferred: hardware integration execution deferred by approved scope
- Promotion candidates declared up front: `SharedSmartCardConnection` to `Tests.Shared`
- Files changed: new shared wrapper, three integration helper imports/usages, three deleted duplicate helper files, `Tests.Shared` docs, this learning note
- Tests run: focused format verification, shared/module builds, module unit tests
- Integration tests run: none
- Result: implementation, focused review, verification, and Cato audit passed
- Commit: pending at note write time; next action after staging approved files
- `/Ping` sent after successful phase: queued after commit and compact summary

## Hardware Target

- Device: YubiKey 5.8 beta
- Serial: 103
- Firmware source of truth: Management `GetDeviceInfoAsync`
- Management firmware observed: not observed in this phase
- Applet firmware observed, if observable: not observed in this phase
- Applet firmware caveat observed: not applicable

## Integration Lifecycle

- Management preflight command/result: not run
- Management preflight evidence captured before applet tests: no
- Management preflight exception path used: no
- Alternate identity proof, if preflight skipped: serial 103 recorded in phase governance, but no hardware command executed
- Agent-runnable integration test allowlist: none for this phase
- Integration scope was read-only: yes by policy; no integration command executed
- Tests run: none
- Tests skipped: all integration tests
- Skip reason: this was a test-harness refactor verified by compile/unit coverage; destructive and persistent-state tests remain banned
- Skip approved by: human instruction to skip destructive tests completely and current phase scope
- Selected tests mutate persistent state: no selected tests
- User Presence / UV required: no selected tests
- Human-coordinated hardware needed: no
- Human-coordinated hardware scheduled/deferred/replaced: replaced by focused build/unit verification and static review
- Persistent state changed: no
- Destructive tests skipped completely: yes
- Reset/cleanup performed: not applicable
- Result: integration skipped by approved scope

## What Worked

- Patterns that improved readability: promoting only the non-owning connection wrapper removed three identical helper classes without hiding app-specific reset/session flow.
- Patterns that improved testability: the wrapper compiles through existing `Tests.Shared` project references in all three integration test projects.
- Patterns that improved security/memory hygiene: no sensitive-data behavior changed; the wrapper keeps physical connection lifetime explicit and owner-controlled.
- Patterns that improved maintainability: documenting the wrapper in `Tests.Shared` gives future multi-session helpers one canonical disposal pattern.

## What Did Not Work

- Rejected approaches: app-specific reset/session extension methods were not promoted because they encode app lifecycle behavior, not generic harness behavior.
- Helpers or abstractions that were too deep: no command/session orchestration helper was introduced.
- Changes that looked DRY but harmed readability: moving PIV or other future helpers into this phase would have exceeded the observed duplication set.
- Tooling issue: parallel builds over shared `Core` outputs produced file-lock failures; sequential reruns passed cleanly.
- Tooling issue: Cato noted missing trailing newlines in two files, but scoped `dotnet format --verify-no-changes` accepts the current formatter-required state, so no manual newline-only edit was made.

## House Style Updates

- Existing house-style rule confirmed: promote small test infrastructure only when multiple modules already prove the same helper shape.
- Existing house-style rule confirmed: keep protocol/session lifecycle flow flat and visible in module integration helpers.
- Rule that needs clarification: shared test harness helpers should stay mechanical and non-owning unless a phase explicitly approves lifecycle ownership changes.
- Possible addition to `docs/SDK-HOUSE-STYLE.md`: prefer `Tests.Shared` for mechanically identical test-only wrappers, but keep app-specific reset/authentication/session choreography module-local.

## Reusable Patterns

- Pattern: non-owning SmartCard connection wrapper that forwards all operations and ignores disposal.
- Generalization class: accepted shared promotion
- Where it applies: integration helpers that need reset-then-test or multiple sessions over one physical SmartCard connection.
- Where it should not apply: production SDK code, helpers that need to own/dispose the physical connection, or app-specific lifecycle orchestration.
- Example files: `src/Tests.Shared/SharedSmartCardConnection.cs`, `src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.IntegrationTests/TestExtensions/HsmAuthTestStateExtensions.cs`, `src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/TestExtensions/OpenPgpTestStateExtensions.cs`, `src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs`

## Core / Shared Promotion Candidates

- Candidate: `SharedSmartCardConnection`
- Declared in phase ISA up front: yes
- Should move to: `Tests.Shared`
- Evidence: three integration test projects carried identical non-owning wrappers with identical forwarding/no-op disposal semantics.
- Risk: accidental expansion into app-specific lifecycle helpers would obscure reset/authentication flow.
- Decision: accepted
- Decision rationale: the helper is small, mechanical, test-only, and already proven by three duplicate implementations.
- Revisit trigger: additional modules need multi-session SmartCard sharing or the wrapper starts accumulating app-specific behavior.
- Demotion/reversal needed for previous shared helper: no
- Demotion/reversal rationale: not applicable

## Cross-Module Implications

- Modules likely affected: PIV, OATH, and future integration tests that perform reset-then-test flows over one SmartCard connection.
- Next module should copy: use the shared wrapper only at the point where a nested reset session must not dispose the owner connection.
- Next module should avoid: promoting reset/authentication/session helper methods without a separate phase and explicit evidence.
- Potential API compatibility concern: none; this is test-only infrastructure.

## Verification Evidence

- Branch check commands: `git status --short --branch`
- Branch check exit result: success, showed `## yubikit-consolidation`
- Format commands: `dotnet format --verify-no-changes --include src/Tests.Shared/SharedSmartCardConnection.cs src/Tests.Shared/CLAUDE.md src/Tests.Shared/README.md src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.IntegrationTests/TestExtensions/HsmAuthTestStateExtensions.cs src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/TestExtensions/OpenPgpTestStateExtensions.cs src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs`
- Format exit result: passed
- Build commands: `dotnet toolchain.cs -- build --project Tests.Shared`
- Build exit result: passed, 0 warnings, 0 errors
- Build commands: `dotnet toolchain.cs -- build --project OpenPgp`
- Build exit result: passed on sequential rerun, 0 warnings, 0 errors
- Build commands: `dotnet toolchain.cs -- build --project YubiHsm`
- Build exit result: passed on sequential rerun, 0 warnings, 0 errors
- Build commands: `dotnet toolchain.cs -- build --project SecurityDomain`
- Build exit result: passed, 0 warnings, 0 errors
- Unit test commands: `dotnet toolchain.cs -- test --project YubiHsm`
- Unit test exit result: passed, 51 total, 51 succeeded, 0 failed, 0 skipped
- Unit test commands: `dotnet toolchain.cs -- test --project OpenPgp`
- Unit test exit result: passed, 88 total, 88 succeeded, 0 failed, 0 skipped
- Unit test commands: `dotnet toolchain.cs -- test --project SecurityDomain`
- Unit test exit result: passed, 20 total, 20 succeeded, 0 failed, 0 skipped
- Integration test commands: none
- Integration test exit result: skipped by approved scope
- Command filters/projects: project filters only; no hardware integration filters executed
- Cross-module verification plan, if shared infrastructure changed: build `Tests.Shared`, build all three consuming modules, run all three consuming module unit test suites
- Results: focused source verification and consuming module verification passed
- Manual review notes: focused reviewer found no blocking issues and confirmed shared-wrapper scope
- Reviewer concerns resolved: unrelated untracked Core files kept out of scope; trailing newline suggestion rejected in favor of formatter-passing state

## Review Summary

- DevTeam engineer result: primary implementation completed by GPT-5.5/OpenCode
- DevTeam reviewer result: focused reviewer reported `Assessment: Ready to proceed`, with no critical findings
- Cross-vendor review result: Cato passed via `model_used: claude-opus-4-8`, output `/tmp/opencode/cato-phase-2-tests-shared-audit.jsonl`
- Cross-vendor review waiver, if any: none used
- Waiver approved by: not applicable
- Waiver reason and scope: not applicable
- Waiver tooling failure/unavailability evidence: not applicable
- Fallback review performed: same-session focused review before Cato
- Findings fixed: none required after Cato
- Findings deferred: none; Cato warning about trailing newlines is documented as formatter-tolerated and non-blocking
- Human decisions: Phase 2 scope kept narrow; no PIV helper and no app-specific helper promotion

## Abort / Split Assessment

- Wrong branch detected: no
- Phase exceeded approved scope: no
- Public API change required: no SDK public API; test helper is public only for cross-assembly integration test use
- Helper depth concern found: no
- Protocol flow became harder to inspect: no
- Verification failed twice for different root causes: no
- Unapproved hardware coordination required: no
- Persistent-state or destructive integration required: no
- Core/shared promotion became unavoidable: yes, but it was declared and approved up front for `Tests.Shared`
- Abort learning note required: no
- Abort learning note committed with human approval: not applicable
- Outcome: continue to commit, compact summary, and `/Ping`

## Next Phase Inputs

- Required reading before next phase: `docs/SDK-HOUSE-STYLE.md`
- Required reading before next phase: `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`
- Required reading before next phase: `docs/plans/module-consolidation/ISA.md`
- Required reading before next phase: this learning note
- Patterns to apply: promote only mechanically identical, test-only helpers after multi-module proof.
- Patterns to apply: keep app-specific protocol/session choreography local unless explicitly approved.
- Risks to watch: future PIV/OATH consolidation may be tempted to move lifecycle helpers prematurely.
- Open questions for human approval: approve Phase 3 scope before SDK source refactors continue.

## Compact Summary

- Goal: consolidate duplicated integration-test SmartCard sharing wrappers into `Tests.Shared`
- Files changed: new shared wrapper, three consuming extension helpers, three deleted duplicate helpers, `Tests.Shared` docs, learning note
- Final pattern: public test-only non-owning `SharedSmartCardConnection` forwards operations and ignores disposal
- Rejected approaches: app-specific session helper promotion, PIV helper addition, production/Core promotion
- Tests passed: scoped format verification, `Tests.Shared` build, OpenPgp/YubiHsm/SecurityDomain builds, OpenPgp/YubiHsm/SecurityDomain unit tests
- Integration lifecycle: skipped by approved scope; no persistent state changed
- Shared/Core candidates: `SharedSmartCardConnection` accepted for `Tests.Shared`; no Core promotion
- House-style update needed: clarify mechanical test-wrapper promotion versus app lifecycle helper promotion
- Next phase recommendation: after commit and `/Ping`, request approval for Phase 3 scope before proceeding
- Learning note path: `docs/plans/module-consolidation/phase-2-tests-shared-harness-learnings.md`
- Commit: pending at note write time
- `/Ping` status: queued after commit and compact summary
