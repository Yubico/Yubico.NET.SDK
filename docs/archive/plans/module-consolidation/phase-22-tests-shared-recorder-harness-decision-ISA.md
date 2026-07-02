# Phase 22 ISA: Tests.Shared Recorder And Harness Decision

This ISA governs Phase 22 of the module-consolidation quality-convergence program.

Read this together with:

- `docs/plans/module-consolidation/ISA.md`
- `docs/plans/module-consolidation/phase-20-quality-convergence-before-composite-yubikey-ISA.md`
- `docs/plans/module-consolidation/phase-20-quality-convergence-learnings.md`
- `docs/plans/module-consolidation/phase-21-core-a-readiness-sdk-api-alignment-learnings.md`
- `src/Tests.Shared/README.md`
- `src/Tests.Shared/CLAUDE.md`

## Problem

Phase 20 made `Tests.Shared` part of the composite-readiness gate, and Phase 21 handed off the current static Core discovery model. The remaining Tests.Shared question is whether repeated fake SmartCard recorder patterns across module unit tests are now duplicated enough to justify shared infrastructure, or whether promotion would add dependency and abstraction risk without enough benefit.

Current source evidence shows nearly identical private `RecordingSmartCardConnection` implementations in PIV, OATH, and SecurityDomain unit tests. Those helpers enqueue raw SmartCard responses, record transmitted command APDUs, report USB SmartCard metadata, return a no-op transaction, and do not support extended APDUs.

## Vision

Phase 22 should leave Tests.Shared easier to use for byte-level applet tests without weakening hardware safety, xUnit runner compatibility, or the current `B+` posture. The outcome should be either a small shared recorder with proven module adoption or an explicit deferral that explains why local copies are still better.

## Out Of Scope

- No integration-test hardware run unless a later finding requires it.
- No changes to allow-list hard-fail policy.
- No changes to `[WithYubiKey]`, lazy device binding, xUnit discovery behavior, or user-presence coordination lanes.
- No broad fake protocol framework or fluent APDU DSL.
- No cross-module session-helper redesign beyond the recorder decision.
- No composite YubiKey design.

## Principles

- Promote only source-proven repetition, not hypothetical reuse.
- A shared test helper must make byte assertions clearer, not hide protocol flow.
- Tests.Shared must remain safe for hardware integration tests and compatible with mixed xUnit v2/v3 projects.
- Keep the helper xUnit-free if unit-test projects consume it.
- Prefer a minimal recorder over a configurable fake framework.

## Constraints

- Execute on branch `yubikit-consolidation`.
- Use `dotnet toolchain.cs ...`; never raw `dotnet test`.
- Unit test projects use xUnit v3; `Tests.Shared` currently carries xUnit v2 extensibility dependencies for integration attributes.
- If a `Tests.Shared` project reference breaks unit-test restore/build/test compatibility, back out source adoption and record the deferral.
- Stage only intended Phase 22 files.

## Goal

Make and verify the Phase 22 Tests.Shared recorder/harness decision: either promote the repeated SmartCard recording connection into a narrow shared helper and adopt it in the repeated module tests, or explicitly defer promotion with source-backed rationale.

## Criteria

- [ ] ISC-1: Phase 22 ISA exists and names Tests.Shared recorder/harness scope.
- [ ] ISC-2: Audit artifact identifies every current private SmartCard recorder copy in active module tests.
- [ ] ISC-3: Decision records whether recorder promotion is accepted or deferred, with dependency rationale.
- [ ] ISC-4: If accepted, `Tests.Shared` contains a narrow xUnit-free recording SmartCard helper.
- [ ] ISC-5: If accepted, PIV, OATH, and SecurityDomain unit tests use the shared helper instead of private duplicate recorder classes.
- [ ] ISC-6: If accepted, affected unit-test project references are explicit and build-compatible.
- [ ] ISC-7: Tests.Shared README and CLAUDE guidance document the recorder only as a byte-level unit-test helper, not an integration-test hardware abstraction.
- [ ] ISC-8: Allow-list, lazy binding, `[WithYubiKey]`, and hardware-safety guidance remain unchanged except for additive recorder documentation.
- [ ] ISC-9: `dotnet toolchain.cs build --project Tests.Shared` succeeds.
- [ ] ISC-10: Focused affected unit tests pass for PIV, OATH, and SecurityDomain.
- [ ] ISC-11: `dotnet toolchain.cs -- docs-qa` succeeds.
- [ ] ISC-12: `git diff --check` succeeds.
- [ ] ISC-13: DevTeam or Cato review runs, and material findings are resolved or explicitly deferred.
- [ ] ISC-14: Learning note records changed files, review evidence, verification evidence, and Phase 23 recommendation.
- [ ] ISC-15: Anti: Phase 22 introduces a broad APDU DSL, fake protocol framework, or recorder behavior that obscures command bytes.
- [ ] ISC-16: Anti: Phase 22 changes hardware allow-list behavior, user-presence policy, or composite YubiKey design.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | file | read Phase 22 ISA | file exists with title | Read |
| ISC-2 | audit | grep/read module tests | PIV, OATH, SecurityDomain copies identified | Grep/Read |
| ISC-3 | audit | read decision section | accepted/deferred with rationale | Read |
| ISC-4 | source | read shared helper | xUnit-free, SmartCard-only recorder | Read |
| ISC-5 | source | grep private recorder names | no private duplicate copies in adopted tests | Grep |
| ISC-6 | build | build affected projects | exit 0 | bash |
| ISC-7 | docs | read Tests.Shared docs | recorder scope documented | Read |
| ISC-8 | diff | inspect Tests.Shared safety guidance | no safety-policy changes | bash/read |
| ISC-9 | build | `dotnet toolchain.cs build --project Tests.Shared` | exit 0 | bash |
| ISC-10 | tests | `dotnet toolchain.cs test --project Piv/Oath/SecurityDomain` | affected unit suites pass | bash |
| ISC-11 | docs | `dotnet toolchain.cs -- docs-qa` | exit 0 | bash |
| ISC-12 | whitespace | `git diff --check` | exit 0 | bash |
| ISC-13 | review | review output | pass or findings resolved | bash/read |
| ISC-14 | learning | read learning note | evidence recorded | Read |
| ISC-15 | diff | inspect helper shape | no broad fake framework | bash/read |
| ISC-16 | diff | inspect safety docs/source | no hardware-policy/composite changes | bash/read |

## Features

| Feature | Description | ISC |
| --- | --- | --- |
| Recorder duplication audit | Identify repeated module-local recorder implementations and dependency risks. | ISC-2, ISC-3 |
| Narrow recorder promotion | Add a minimal `RecordingSmartCardConnection` only if project compatibility verifies. | ISC-4, ISC-6, ISC-15 |
| Module adoption | Replace duplicated private recorders in the three source-proven test files. | ISC-5, ISC-10 |
| Tests.Shared documentation | Document the helper scope without changing hardware-safety guidance. | ISC-7, ISC-8, ISC-16 |
| Learning handoff | Record review and verification evidence for Phase 23. | ISC-13, ISC-14 |

## Decisions

- 2026-06-08: Reuse evidence is sufficient to attempt a narrow recorder promotion because PIV, OATH, and SecurityDomain repeat the same queue-and-record SmartCard fake.
- 2026-06-08: The recorder must not depend on xUnit because the consuming unit-test projects use xUnit v3 while existing integration Tests.Shared infrastructure uses xUnit v2 extensibility.
- 2026-06-08: If the project-reference dependency shape causes xUnit conflicts, the correct Phase 22 output is a documented deferral, not a new compatibility workaround.

## Changelog

- conjectured: Tests.Shared recorder promotion might be premature because only future PIV byte-level coverage needs it.
  refuted by: active PIV, OATH, and SecurityDomain unit tests already contain nearly identical private recorders.
  learned: the promotion can be justified now if it stays xUnit-free and tiny.
  criterion now: ISC-4 through ISC-6.

## Verification

Verification is populated in `docs/plans/module-consolidation/phase-22-tests-shared-recorder-harness-decision-learnings.md` as the phase executes.
