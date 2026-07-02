# Phase 24 ISA: YubiHsm Byte-Level Coverage

This ISA governs Phase 24 of the module-consolidation quality-convergence program.

Read this together with:

- `docs/plans/module-consolidation/ISA.md`
- `docs/plans/module-consolidation/phase-23-piv-byte-level-coverage-learnings.md`
- `docs/SDK-HOUSE-STYLE.md`
- `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`
- `docs/TESTING.md`
- `src/YubiHsm/CLAUDE.md`
- `src/YubiHsm/tests/CLAUDE.md`

## Problem

Phase 20 identified YubiHsm as a `B-` module whose primary consolidation risk is sensitive APDU payload lifecycle. The module is intentionally SmartCard-only and flat, but current unit coverage focuses on pure helpers: credential-password parsing, label validation, retry extraction, PBKDF2, credential records, and session-key disposal. There is little unit-level proof that public session operations build the expected YubiHSM Auth APDU/TLV payloads or that sensitive encoded payloads remain zeroed after transmission.

Phase 23 proved the right pattern for this stage: use `Tests.Shared.RecordingSmartCardConnection`, assert short APDU header/data bytes directly, avoid production refactors unless a failing test proves a defect, and comment magic protocol bytes where they are not self-explanatory.

## Vision

YubiHsm should gain narrow byte-level tests around the most security-sensitive APDU paths without changing public APIs, introducing operation-specific command classes, or hiding protocol flow behind a new test DSL. The tests should make it harder to regress management-key, credential-password, key material, and calculate-session payload encodings while preserving the module's existing flat `HsmAuthSession` shape.

## Out of Scope

- No Composite YubiKey design or implementation.
- No YubiHsm public API changes unless a source-backed bug requires them and the user approves.
- No broad YubiHsm refactor or file split.
- No operation-specific command classes or command-like protocol executors.
- No APDU DSL, fake protocol framework, or broad assertion layer that hides raw command bytes.
- No integration tests that reset or mutate the YubiHSM Auth applet unless a narrower phase revision records explicit approval and reset expectations.
- No human-coordinated hardware ceremonies.
- No local example CLI parsing work unless source review proves it is still a material Phase 24 risk.

## Principles

- Prove protocol bytes at the public session boundary.
- Keep assertions ordered and field-position-aware, not tag-presence-only.
- Prefer exact or prefix assertions over full encoder duplication.
- Use test data that is obviously synthetic and not real secrets.
- Verify sensitive command payload lifecycle where touched, especially encoded APDU data zeroing after transmit.
- Keep helpers local unless another module repeats the pattern.
- Comment magic YubiHSM Auth constants in tests when the source constant name is not adjacent to the byte assertion.

## Constraints

- Execute on branch `yubikit-consolidation`.
- Use `dotnet toolchain.cs ...`; never raw `dotnet build` or raw `dotnet test`.
- Source edits require this Phase 24 ISA to exist first; the user has approved autonomous continuation through Phase 32 unless a stop condition is reached.
- `RecordingSmartCardConnection` from `Tests.Shared` is the preferred fake connection for new byte-level SmartCard unit tests.
- YubiHsm unit tests currently do not reference `Tests.Shared`; adding that project reference is allowed if the xUnit v2/v3 boundary remains clean.
- If an integration smoke test is useful, it must be read-only or explicitly justified; persistent-state/destructive YubiHsm Auth integration is deferred by default.
- YubiHsm has no module README at `src/YubiHsm/README.md`; `src/YubiHsm/CLAUDE.md` is the module-specific guidance source for this phase.
- If `Tests.Shared` xUnit v2 package privacy affects YubiHsm integration runtime skip behavior, use the Phase 23 package-boundary pattern and document the rationale.

## Goal

Add focused YubiHsm unit coverage for high-risk APDU/TLV command encodings and sensitive encoded payload zeroing using the shared SmartCard recorder, while preserving public API shape, flat protocol flow, and hardware safety boundaries.

## Criteria

- [ ] ISC-1: Branch check shows `## yubikit-consolidation` before implementation, review, verification, or delegation.
- [ ] ISC-2: Phase 24 ISA exists and defines YubiHsm byte-level scope.
- [ ] ISC-3: Required context was read: house style, consolidation assessment, master ISA, Phase 23 learning note, `docs/TESTING.md`, YubiHsm module/test CLAUDE files.
- [ ] ISC-4: New byte-level unit tests use `RecordingSmartCardConnection` instead of a new private SmartCard recorder.
- [ ] ISC-5: Unit test project references `Tests.Shared` only if needed, without leaking xUnit v2 packages into xUnit v3 tests.
- [ ] ISC-6: PUT symmetric credential coverage verifies `INS 0x01`, P1/P2 defaults, and ordered TLVs for management key `0x7B`, label `0x71`, algorithm `0x74`, K-ENC `0x75`, K-MAC `0x76`, credential password `0x73`, and touch `0x7A`.
- [ ] ISC-7: CALCULATE symmetric coverage verifies `INS 0x03` and ordered TLVs for label `0x71`, context `0x77`, optional card cryptogram/response `0x78`, and credential password `0x73`.
- [ ] ISC-8: DELETE or PUT management-key coverage verifies management-key-gated command encoding and retry-check path uses `throwOnError: false` semantics through existing public behavior.
- [ ] ISC-9: At least one new unit test verifies encoded APDU command data containing sensitive values is zeroed after transmit returns or throws.
- [ ] ISC-10: At least one error-path test covers SW `0x63Cx` retry extraction without hiding APDU bytes.
- [ ] ISC-11: Tests assert source-backed command details without duplicating complete implementation encoders.
- [ ] ISC-12: Tests avoid real management keys, credential passwords, EC private keys, or persistent applet mutations.
- [ ] ISC-13: Any source fix, if needed, is minimal and explained by a failing test.
- [ ] ISC-14: No operation-specific command classes or command-like protocol executors are introduced.
- [ ] ISC-15: No broad APDU DSL, fake protocol framework, or broad assertion helper is introduced.
- [ ] ISC-16: Integration scope is either skipped with rationale or limited to an explicitly safe read-only smoke; no destructive YubiHsm Auth integration run occurs without revised approval.
- [ ] ISC-17: `dotnet toolchain.cs -- build --project YubiHsm` succeeds.
- [ ] ISC-18: `dotnet toolchain.cs -- test --project YubiHsm` succeeds.
- [ ] ISC-19: If integration is run, command text, scope, hardware target, and result are recorded.
- [ ] ISC-20: `dotnet toolchain.cs -- docs-qa` succeeds if docs/plans artifacts are changed.
- [ ] ISC-21: `git diff --check` succeeds.
- [ ] ISC-22: DevTeam cross-vendor review runs and material findings are resolved or explicitly deferred.
- [ ] ISC-23: Learning note records changed files, review evidence, verification evidence, integration decision/result, sensitive-buffer findings, and Phase 25 recommendation.
- [ ] ISC-24: Commit contains only intended Phase 24 files.
- [ ] ISC-25: Compact summary is produced after commit and before Phase 25 begins.
- [ ] ISC-26: Anti: Phase 24 runs reset/destructive YubiHsm Auth integration tests without revised phase approval.
- [ ] ISC-27: Anti: Phase 24 claims FIDO/FIDO2/WebAuthn User Presence behavior was verified.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | branch | Check current branch | `## yubikit-consolidation` | `git status --short --branch` |
| ISC-2 | file | Read Phase 24 ISA | title and scope present | Read |
| ISC-3 | file | Read required context | all listed context loaded | Read |
| ISC-4 | source | Grep tests | shared recorder used; no private duplicate | Grep |
| ISC-5 | build | Unit test dependency boundary | no xUnit v2/v3 ambiguity | build/test |
| ISC-6 | unit | PUT symmetric command | ordered APDU/TLV bytes verified | test output |
| ISC-7 | unit | CALCULATE symmetric command | ordered APDU/TLV bytes verified | test output |
| ISC-8 | unit | Management-key-gated command/error path | command and retry handling verified | test output |
| ISC-9 | unit | Sensitive encoded data zeroing | command data buffer zeroed after transmit | test output/source |
| ISC-10 | unit | SW `0x63Cx` path | retry extraction preserves public shape | test output |
| ISC-11 | source | Inspect assertions | no full encoder duplicate | diff/read |
| ISC-12 | source | Inspect test data | synthetic only, no real secrets | diff/read |
| ISC-13 | source | Inspect source diff | minimal fix only if needed | diff |
| ISC-14 | source | Grep forbidden command types | none introduced | Grep |
| ISC-15 | source | Inspect helpers | no DSL/framework | diff |
| ISC-16 | integration | Integration decision | skipped or safe smoke only | learning note/bash |
| ISC-17 | build | Build YubiHsm | exit 0 | bash |
| ISC-18 | tests | YubiHsm unit tests | exit 0 | bash |
| ISC-19 | integration | Optional integration | result recorded or rationale recorded | bash/learning |
| ISC-20 | docs | Docs QA | exit 0 | bash |
| ISC-21 | whitespace | Diff check | exit 0 | bash |
| ISC-22 | review | DevTeam review | pass or resolved findings | review output |
| ISC-23 | learning | Read learning note | evidence present | Read |
| ISC-24 | git | Inspect staged files | intended Phase 24 only | `git status`, `git diff --cached --name-only` |
| ISC-25 | handoff | Compact summary | summary present | response |
| ISC-26 | anti | Integration logs | no destructive unapproved run | learning/bash |
| ISC-27 | anti | Learning/read logs | no false FIDO UP claim | Read |

## Features

| Feature | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Phase 24 setup | Confirm branch, create ISA, read required context, inspect current YubiHsm shape. | ISC-1, ISC-2, ISC-3 | none | false |
| Shared recorder adoption | Add `Tests.Shared` reference if needed and use `RecordingSmartCardConnection` for APDU capture. | ISC-4, ISC-5 | setup | false |
| Credential APDU tests | Add PUT symmetric and management-key-gated byte-level tests. | ISC-6, ISC-8, ISC-9, ISC-10, ISC-11, ISC-12 | setup | false |
| Calculate APDU tests | Add CALCULATE symmetric byte-level tests. | ISC-7, ISC-9, ISC-11, ISC-12 | setup | false |
| Hardware boundary | Decide integration scope and avoid unapproved destructive YubiHsm runs. | ISC-16, ISC-19, ISC-26, ISC-27 | setup | false |
| Review and verification | Run build/tests/docs/diff/review, write learning, commit, compact summary. | ISC-17-ISC-25 | tests complete | false |

## Decisions

- 2026-06-08: The user approved autonomous continuation through Phase 32, with owner interviews still saved for the Composite YubiKey stop gate after Phase 32.
- 2026-06-08: Phase 24 uses the Phase 23 shared-recorder byte-level pattern and ordered APDU data assertions.
- 2026-06-08: YubiHsm persistent/destructive integration is deferred by default. Unit/fake-connection coverage is the primary verification lane for this phase unless source inspection finds a safe read-only smoke with clear value.
- 2026-06-08: No YubiHsm module README exists; no README update is required unless Phase 24 changes public API, behavior, or documented test infrastructure.

## Verification

Verification is populated in `docs/plans/module-consolidation/phase-24-yubihsm-byte-level-coverage-learnings.md` as the phase executes.
