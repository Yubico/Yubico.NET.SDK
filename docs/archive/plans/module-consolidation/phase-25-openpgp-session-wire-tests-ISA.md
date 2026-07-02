# Phase 25 ISA: OpenPGP Session Wire Tests

## Problem

OpenPGP has strong model tests, but session-level APDU behavior is under-tested compared to the PIV and YubiHsm byte-level coverage added in Phases 23 and 24. The module already keeps protocol flow visible in `OpenPgpSession` partials, so Phase 25 should add meaningful wire assertions without introducing command classes or broad helper layers.

## Vision

OpenPGP remains a rich domain-model module, while its public session behavior gains byte-level tests for representative APDU flows. A reviewer should be able to see the intended OpenPGP wire shape directly in test assertions and source, with no public module API churn.

## Out Of Scope

- No public OpenPGP API changes.
- No operation-specific command classes.
- No broad OpenPGP protocol emulator or APDU DSL.
- No DER, DigestInfo, or OID Core promotion unless source evidence proves cross-module value during implementation.
- No Composite YubiKey design or implementation.

## Constraints

- Execute only on branch `yubikit-consolidation`.
- Follow `docs/SDK-HOUSE-STYLE.md`, `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`, and the master consolidation ISA.
- Use `RecordingSmartCardConnection` from `Tests.Shared` for unit APDU capture.
- Keep OpenPGP protocol flow visible in `OpenPgpSession` methods or shallow local helpers.
- Core APIs may change if ownership/security/design clarity requires it, but no Core API change is expected for this phase.
- Public module APIs remain stable unless explicitly approved.
- All self-runnable OpenPGP integration tests are allowed after beta-key preflight; exclude only UP/UV/touch/insert-remove/human-coordinated tests if found.

## Goal

Add focused OpenPGP session wire tests that prove representative APDU byte shapes through public session methods, verify the module with unit/build/integration coverage, complete DevTeam review, capture learnings, and commit the phase.

## Criteria

- [ ] ISC-1: Branch check shows `## yubikit-consolidation` before source edits and verification.
- [ ] ISC-2: OpenPGP unit tests reference `Tests.Shared` only as needed for `RecordingSmartCardConnection`.
- [ ] ISC-3: A public-session initialization test records SELECT, GET VERSION, and application-related GET DATA APDUs.
- [ ] ISC-4: A public-session GET DATA test asserts ordered APDU header bytes for a representative data object.
- [ ] ISC-5: A public-session PUT DATA or configuration test asserts ordered APDU header bytes and exact short-APDU data bytes for a representative object.
- [ ] ISC-6: A PIN-related public-session test asserts VERIFY APDU P2 and PIN payload ordering while avoiding validation-only coverage.
- [ ] ISC-7: Tests assert exact ordered bytes or exact header/data fields, not tag presence only.
- [ ] ISC-8: Production OpenPGP source remains unchanged unless implementation reveals a source-backed defect or Core API clarity issue.
- [ ] ISC-9: No operation-specific command classes or command-like protocol objects are introduced.
- [ ] ISC-10: No public OpenPGP API shape changes.
- [ ] ISC-11: Core, `Tests.Shared`, and public module API candidates are accepted, rejected, or deferred with rationale.
- [ ] ISC-12: Focused OpenPGP unit tests pass.
- [ ] ISC-13: `dotnet toolchain.cs -- build --project OpenPgp` passes.
- [ ] ISC-14: `dotnet toolchain.cs -- test --project OpenPgp` passes.
- [ ] ISC-15: Beta-key preflight records serial and firmware before OpenPGP integration tests.
- [ ] ISC-16: Self-runnable OpenPGP integration scope is run or excluded tests are listed with human-coordination rationale.
- [ ] ISC-17: `dotnet toolchain.cs -- docs-qa` passes if docs are touched.
- [ ] ISC-18: `git diff --check` reports no whitespace errors.
- [ ] ISC-19: DevTeam cross-vendor review completes through the resolved reviewer route, or a waiver is recorded only if the exact route fails.
- [ ] ISC-20: Learning note records changed files, review evidence, verification evidence, integration result, and Phase 26 recommendation.
- [ ] ISC-21: Phase 25 changes are committed explicitly with no unrelated files staged.

## Test Strategy

| Criterion | Type | Check | Threshold | Tool |
|---|---|---|---|---|
| ISC-1 | branch | `git status --short --branch` | `yubikit-consolidation` | bash |
| ISC-2 | source | unit csproj contains `Tests.Shared` reference | exact file diff | read/diff |
| ISC-3 | unit | create-session APDU assertions | SELECT, GET VERSION, GET DATA observed | `dotnet toolchain.cs -- test --project OpenPgp --filter "ClassName~OpenPgpSessionWireTests"` |
| ISC-4 | unit | GET DATA APDU assertion | exact CLA/INS/P1/P2 | same |
| ISC-5 | unit | PUT DATA/config APDU assertion | exact header and data | same |
| ISC-6 | unit | VERIFY APDU assertion | exact P2 and payload | same |
| ISC-7 | review | inspect assertions | ordered bytes used | read/diff |
| ISC-8 | review | source diff | no production OpenPGP changes unless justified | `git diff --name-only` |
| ISC-9 | source search | command-class ban | no forbidden new types | grep |
| ISC-10 | API review | public OpenPGP API diff | no public module API change | diff/review |
| ISC-11 | learnings | candidate disposition | recorded | learning note |
| ISC-12 | focused unit | focused class filter | pass | bash |
| ISC-13 | build | OpenPGP build | pass | bash |
| ISC-14 | unit | OpenPGP unit suite | pass | bash |
| ISC-15 | preflight | beta key identity | serial 103, firmware 5.8.x | ykman/tool output |
| ISC-16 | integration | OpenPGP integration suite/filter | pass or exclusions recorded | bash |
| ISC-17 | docs | docs QA | pass if docs touched | bash |
| ISC-18 | whitespace | diff check | no errors | bash |
| ISC-19 | review | DevTeam output | no unresolved material findings | opencode/router output |
| ISC-20 | learning | note exists | complete sections | read |
| ISC-21 | commit | log/status | intended files only | git |

## Features

| Feature | Satisfies | Depends On | Parallelizable |
|---|---|---|---|
| Phase ISA and preflight | ISC-1, ISC-15 | none | false |
| OpenPGP recorder reference | ISC-2 | branch check | false |
| Session wire tests | ISC-3, ISC-4, ISC-5, ISC-6, ISC-7 | recorder reference | false |
| Verification and integration | ISC-12, ISC-13, ISC-14, ISC-16, ISC-18 | tests implemented | false |
| DevTeam review | ISC-19 | implementation diff | false |
| Learning and commit | ISC-20, ISC-21 | verification and review | false |

## Decisions

- 2026-06-08: User approved autonomous Phase 25 through Phase 32 execution and removed persistent/destructive integration-test gating for self-runnable tests.
- 2026-06-08: Phase 25 will add unit APDU wire coverage before considering production OpenPGP refactors.
- 2026-06-08: No Core API change is planned, but Core API changes remain allowed if they improve ownership, security, explicitness, or clarity.

## Verification

- Branch check before source edits: `git status --short --branch` showed `## yubikit-consolidation...origin/yubikit-consolidation [ahead 4]`.
- Focused wire tests: `dotnet toolchain.cs -- test --project OpenPgp --filter "ClassName~OpenPgpSessionWireTests"` passed 4/4.
- OpenPGP build: `dotnet toolchain.cs -- build --project OpenPgp` passed after adding the OpenPGP integration `Xunit.SkippableFact` reference.
- OpenPGP unit suite: `dotnet toolchain.cs -- test --project OpenPgp` passed 92/92.
- Docs QA: `dotnet toolchain.cs -- docs-qa` passed, 54 active documentation files validated.
- Targeted formatting: `dotnet format src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/Yubico.YubiKit.OpenPgp.UnitTests.csproj --include src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpSessionWireTests.cs --verify-no-changes` passed after applying targeted formatting.
- Beta-key preflight: `ykman list --serials` returned `103`; `ykman info` reported serial `103`, firmware `5.8.0.beta.0`, USB interfaces OTP/FIDO/CCID, and OpenPGP enabled.
- OpenPGP integration first attempt: `dotnet toolchain.cs -- test --integration --project OpenPgp` failed 48/48 because the integration project lacked direct runtime access to `Xunit.SkippableFact` after `Tests.Shared` keeps xUnit v2 packages private.
- OpenPGP integration final: `dotnet toolchain.cs -- test --integration --project OpenPgp` passed 92/92 unit tests and 48/48 integration tests after adding the direct `Xunit.SkippableFact` package reference.
- DevTeam review: Vertex Opus 4.8 route completed with no material defects; evidence at `/tmp/opencode/phase25-devteam-review.md`.
- Whitespace: `git diff --check` emitted only line-ending normalization warnings for the two touched OpenPGP csproj files; no whitespace errors.
