# Phase 20 ISA: Quality Convergence Before Composite YubiKey

This ISA defines the next module-consolidation program after the final reassessment and the extended APDU follow-up. It is a program-control artifact: it authorizes Phase 20 documentation work and defines the phase sequence that later source phases must follow, but each later source phase still needs its own phase ISA before code changes begin.

Read this together with:

- `docs/SDK-HOUSE-STYLE.md`
- `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`
- `docs/MODULE-CONSOLIDATION-FINAL-REASSESSMENT.md`
- `docs/plans/module-consolidation/ISA.md`
- `docs/plans/module-consolidation/phase-19-final-reassessment-learnings.md`

## Problem

The consolidation branch is materially stronger than the original baseline, but the active SDK still does not look uniformly like one senior .NET 10 / C# 14 developer implemented it with one coherent set of practices. Core and `Tests.Shared` are at `B+`; `YubiOtp` and `OpenPgp` are at `B+`; most applet modules are at `B`; `WebAuthn` still has maintainability pressure; and CLI remains the weakest active surface.

The next major Core feature, aggregated/composite YubiKey discovery, will change how callers think about device identity and connection availability. Starting that before the applet modules and shared test harness converge would build a public device model on top of uneven module style.

The program therefore needs one more source-backed quality pass before composite-device design begins.

## Vision

The active SDK should feel like one experienced Yubico .NET developer built it: flat protocol flow, shallow helpers, SDK-family-familiar public concepts, modern .NET memory and async practices, source-backed documentation, and tests that prove behavior at the byte level where protocol regressions matter.

When this pass is complete, the composite YubiKey discussion starts from a stable library baseline instead of from cleanup debt.

## Out of Scope

- No composite YubiKey implementation in this program.
- No composite-device API design interviews until the quality gate passes.
- No `Tests.TestProject` work, scoring, or gate participation.
- No broad CLI consolidation while the library modules still need quality work.
- No new dependency-injection system; DI can be reconsidered later when constructors and device composition are stable.
- No public API freeze against a historical .NET package baseline.
- No operation-specific protocol command classes or command-like protocol executors.
- No all-module rewrite or style-only churn that lacks a current risk from the reassessment.

## Principles

- Same architectural rhythm matters more than identical architecture. Modules can differ internally if they read consistently at the protocol-flow level.
- The active compatibility surface is currently the CLI, integration tests, package packability, and SDK-family familiarity, not external .NET users.
- Public APIs may still change, but they should remain recognizable to users of `yubikit-swift`, Python `yubikey-manager` / `yubikit`, and `yubikit-android` unless .NET offers a clearly better construct.
- Tests are not optional polish. Byte-level fake tests and documented hardware integration tests are how this program prevents architectural cleanup from becoming aesthetic churn.
- Phase learning is part of the product. Every phase must produce a learning note that feeds the final reassessment.

## Constraints

- Execute on branch `yubikit-consolidation`.
- Use `dotnet toolchain.cs` commands, never raw `dotnet build` or `dotnet test`.
- Each phase runs through `/DevTeam` for implementation/review or an explicitly documented docs-only review path.
- Each source phase has a phase-specific ISA before code changes begin.
- Each phase commits only intended files before the next phase begins.
- Each phase writes a learning note before commit and before moving on.
- `/Cato` review runs for Phase 20 planning artifacts, later broad architectural artifacts, and final reassessment artifacts.
- Integration tests are allowed on the connected YubiKey 5.8 beta key when relevant and documented. Tests with reset/init harnesses may run when the phase ISA names them.
- FIDO2/WebAuthn UP, UV, touch, and ceremony tests require explicit coordination if physical presence is needed.
- Composite YubiKey work stops for owner interviews after the quality gate; no agent proceeds into that design automatically.

## Goal

Run a sequential quality-convergence program that raises `Core`, all applet modules, and `Tests.Shared` to at least `B+` using the original assessment metrics, updates CI docs validation, preserves SDK-family public API familiarity, and then stops before composite YubiKey device design.

## Criteria

- [ ] ISC-1: Branch check shows `## yubikit-consolidation` before Phase 20 artifact edits and before each later phase begins.
- [ ] ISC-2: Phase 20 ISA exists at `docs/plans/module-consolidation/phase-20-quality-convergence-before-composite-yubikey-ISA.md`.
- [ ] ISC-3: Phase 20 learning note exists at `docs/plans/module-consolidation/phase-20-quality-convergence-learnings.md`.
- [ ] ISC-4: `docs/plans/module-consolidation/ISA.md` references the Phase 20+ sequence.
- [ ] ISC-5: The phase sequence explicitly excludes `Tests.TestProject` from scoring, phase scope, and composite readiness gates.
- [ ] ISC-6: The phase sequence defers broad CLI work until after library-module convergence.
- [ ] ISC-7: The phase sequence includes a CI gate phase for `dotnet toolchain.cs -- docs-qa`.
- [ ] ISC-8: The phase sequence replaces hard API/package compatibility enforcement with a public API shape and SDK-family alignment audit.
- [ ] ISC-9: The API alignment policy names `yubikit-swift`, Python `yubikey-manager` / `yubikit`, and `yubikit-android` as reference families.
- [ ] ISC-10: The API alignment policy allows .NET-specific divergence only when the divergence is intentional, source-backed, and better for .NET 10 / C# 14.
- [ ] ISC-11: Composite YubiKey discovery is explicitly deferred until after the quality gate passes.
- [ ] ISC-12: The composite-device follow-up questions are saved in this ISA without starting design work.
- [ ] ISC-13: The quality gate requires `Core`, every applet module, and `Tests.Shared` to be at least `B+` before composite YubiKey design begins.
- [ ] ISC-14: Each source phase is required to use `/DevTeam` implementation and review before completion.
- [ ] ISC-15: Each phase is required to commit only intended files before the next phase begins.
- [ ] ISC-16: Each phase is required to write a learning note before the next phase begins.
- [ ] ISC-17: Each phase is required to feed learnings into the final reassessment.
- [ ] ISC-18: Phase 20 runs `/Cato` or records a structured Cato failure before commit.
- [ ] ISC-19: Phase 20 verification includes `dotnet toolchain.cs -- docs-qa`.
- [ ] ISC-20: Phase 20 verification includes `git diff --check`.
- [ ] ISC-21: The final reassessment update records that extended APDU support detection is closed by commit `90a41b26`.
- [ ] ISC-22: Phase 19 learning carry-forward no longer lists extended APDU support detection as an open deferred investigation.
- [ ] ISC-23: Anti: Phase 20 changes source code.
- [ ] ISC-24: Anti: Phase 20 starts composite YubiKey API design.
- [ ] ISC-25: Anti: Phase 20 creates a hard external package compatibility gate.
- [ ] ISC-26: Anti: any later phase proceeds without a phase ISA, learning note, review, verification, and commit.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | branch | Check current branch | `## yubikit-consolidation` | `git status --short --branch` |
| ISC-2 | file | Phase 20 ISA exists | title present | Read |
| ISC-3 | file | Phase 20 learning note exists | title present | Read |
| ISC-4 | content | Master ISA references Phase 20+ | Phase 20 and composite stop present | Grep/Read |
| ISC-5 | content | `Tests.TestProject` exclusion present | exclusion named in ISA and learning | Grep/Read |
| ISC-6 | content | CLI deferral present | broad CLI deferred until library stronger | Grep/Read |
| ISC-7 | content | docs QA CI phase present | CI gate phase named | Grep/Read |
| ISC-8 | content | API/package policy reframed | SDK-family audit wording present | Grep/Read |
| ISC-9 | content | SDK references named | Swift, Python, Android named | Grep/Read |
| ISC-10 | content | .NET divergence policy present | intentional better .NET clause present | Grep/Read |
| ISC-11 | content | Composite deferred | stop gate present | Grep/Read |
| ISC-12 | content | Composite questions saved | question list present | Read |
| ISC-13 | content | Quality gate present | Core + applets + Tests.Shared `B+` | Grep/Read |
| ISC-14 | content | DevTeam required | `/DevTeam` appears in lifecycle | Grep/Read |
| ISC-15 | content | commit required | phase lifecycle names commit | Grep/Read |
| ISC-16 | content | learning required | phase lifecycle names learning note | Grep/Read |
| ISC-17 | content | reassessment feed present | learning feeds final reassessment | Grep/Read |
| ISC-18 | review | Cato output exists | pass or resolved concerns | Cato output JSONL |
| ISC-19 | docs validation | active docs validate | exit 0 | `dotnet toolchain.cs -- docs-qa` |
| ISC-20 | whitespace | diff has no whitespace errors | exit 0 | `git diff --check` |
| ISC-21 | content | final reassessment updated | `90a41b26` present | Grep/Read |
| ISC-22 | content | stale learning removed | no open extended APDU investigation wording | Grep/Read |
| ISC-23 | git | source diff absent | no `src/**` changes | `git diff --name-only` |
| ISC-24 | content | no composite design start | only saved questions and stop gate | Read |
| ISC-25 | content | no package hard gate | audit-only policy present | Read |
| ISC-26 | content | later lifecycle gate present | phase lifecycle states ISA/review/learn/commit | Read |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Phase 20 artifacts | Write this ISA and the Phase 20 learning note. | ISC-2, ISC-3, ISC-5, ISC-6, ISC-8, ISC-13 | branch check | false |
| Master ISA update | Update the original consolidation ISA with Phase 20+ ordering and the composite stop gate. | ISC-4, ISC-11, ISC-12, ISC-14, ISC-15, ISC-16, ISC-17, ISC-26 | Phase 20 artifacts | false |
| Phase 19 carry-forward cleanup | Update final reassessment and learning carry-forward so extended APDU is no longer treated as open. | ISC-21, ISC-22 | Phase 20 artifacts | false |
| Review and verification | Run Cato, docs QA, whitespace checks, and git-source-diff checks. | ISC-18, ISC-19, ISC-20, ISC-23, ISC-24, ISC-25 | artifacts complete | false |
| Future source phases | Execute each later phase through phase ISA, `/DevTeam`, review, verification, learning note, commit, and final reassessment feed. | ISC-14, ISC-15, ISC-16, ISC-17, ISC-26 | Phase 20 commit | true |

## Decisions

- 2026-06-07: The user approved execution of the Phase 20 artifacts and the subsequent quality-convergence program.
- 2026-06-07: `Tests.TestProject` is excluded because it was a temporary DI test project and DI was removed to avoid premature optimization.
- 2026-06-07: Integration tests are allowed on the connected YubiKey 5.8 beta key when a phase ISA names them and the tests are documented or harnessed for reset/init cleanup.
- 2026-06-07: CLI work is deferred until the library modules are stronger; CLI should not block composite readiness.
- 2026-06-07: `docs-qa` should become a CI gate.
- 2026-06-07: Composite YubiKey readiness requires `Core`, every applet module, and `Tests.Shared` to be at least `B+`.
- 2026-06-07: Package validation remains audit-only because there is no broad external .NET user base yet.
- 2026-06-07: Public API changes are still bounded by SDK-family familiarity with `yubikit-swift`, Python `yubikey-manager` / `yubikit`, and `yubikit-android`, except where .NET 10 / C# 14 offers a clearly better construct.
- 2026-06-07: Composite YubiKey design stops for owner interviews after the quality gate; no agent proceeds into that design automatically.

## Changelog

- conjectured: The next program should enforce package/API compatibility before further cleanup.
  refuted by: The owner clarified there is no meaningful external .NET user base yet and that CLI/integration tests are the real current consumers.
  learned: Public API compatibility should be an SDK-family alignment audit, not a hard external baseline gate.
  criterion now: ISC-8 and ISC-25 require audit-only SDK-family public API alignment.
- conjectured: Integration tests should be avoided by default during consolidation.
  refuted by: The owner clarified that a connected 5.8 beta key is available and documented integration tests can run.
  learned: Integration tests are phase-appropriate verification tools, not exceptional events, while UP/UV/touch still need coordination.
  criterion now: The constraints allow documented integration tests and require phase ISAs to name selected filters.

## Verification

Verification is populated as Phase 20 executes. A phase is not complete until every checked ISC has tool-backed evidence recorded here or in the Phase 20 learning note.

## Phase Order

### Phase 20: Quality Convergence Program ISA

Write the program-control ISA, update the master consolidation ISA, run Cato, verify docs, commit, and capture learnings.

### Phase 21: Core A- Readiness And SDK-Family API Alignment Audit

Repair Core DI documentation drift, audit duplicate CRC/checksum utilities, audit public API shape against Swift/Python/Android concepts, and keep package validation audit-only unless a later owner decision chooses a hard baseline.

### Phase 22: Tests.Shared Recorder And Harness Decision

Decide whether a shared fake smart-card recorder or app-session helper improvement is justified by repeated module patterns. Preserve or improve the current `B+` rating.

### Phase 23: PIV Byte-Level Coverage

Add focused fake APDU coverage for high-risk crypto/key-operation encodings and simplify reset/auth/default-credential integration choreography only where it improves maintainability.

### Phase 24: YubiHsm Byte-Level Coverage

Add fake APDU byte-level tests for credential operations and verify sensitive APDU payload lifecycle remains explicit.

### Phase 25: OpenPGP Session Wire Tests

Add fake APDU tests around session-level wire behavior while preserving OpenPGP-specific model richness.

### Phase 26: FIDO2 Remaining CTAP Consistency

Extend the canonical request-construction and sensitive-copy lifecycle pattern beyond MakeCredential/GetAssertion into the highest-value remaining CTAP paths.

### Phase 27: WebAuthn Maintainability Split

Split ceremony orchestration, validation, token flow, request mapping, error mapping, and response building only where it improves readability without hiding FIDO2 delegation.

### Phase 28: OATH Locality Polish

Reduce monolithic session pressure with pure encode/parse helpers where clarity improves. Preserve Core configured chained-response behavior for `INS_SEND_REMAINING = 0xA5`.

### Phase 29: SecurityDomain Test And Locality Follow-Up

Use the Phase 22 test-harness decision and add coverage or locality improvements only where they still move maintainability.

### Phase 30: YubiOtp And Management Stability Polish

Keep these stable modules stable. Make only source-backed polish that preserves their `B+` trajectory: YubiOtp protocol-codec noise if real, Management backend payload/read-path clarity if real.

### Phase 31: Docs QA CI Gate

Wire `dotnet toolchain.cs -- docs-qa` into CI as a bounded active-doc validation gate.

### Phase 32: Same-Criteria Quality Reassessment

Regrade active surfaces with the original metrics, excluding `Tests.TestProject`, and record whether the composite-readiness gate passed.

### Stop Gate: Composite YubiKey Interviews

Stop and wait for owner interviews before designing or implementing composite YubiKey discovery.

Saved questions for that later interview:

- Should `IYubiKey` remain a per-connection interface or become a physical-device abstraction?
- How should `ConnectionType` filters behave when one physical key supports multiple interfaces?
- What identity/cache key should be used: serial, PID plus fingerprint, reader/path group, or resolved `DeviceInfo` identity?
- How should NFC avoid false USB aggregation?
- Should per-interface discovery remain available for advanced callers?
- How closely should .NET follow Python's `_PidGroup` / `_UsbCompositeDevice` model versus using more explicit .NET types?
