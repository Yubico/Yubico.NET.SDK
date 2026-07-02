# Phase 32 Same-Criteria Quality Reassessment ISA

## Problem

Phase 32 is the stop-gate phase from the Phase 20 quality-convergence program. Phases 21 through 31 improved Core/API alignment, shared test harnesses, byte-level applet coverage, sensitive APDU lifecycle, FIDO2/WebAuthn/OATH locality, SecurityDomain STORE DATA coverage, Management read-path confidence, and docs QA CI wiring. The existing final reassessment was written at Phase 19 and no longer reflects the completed Phase 20-31 program.

Before composite YubiKey discovery can be discussed, the branch needs a same-criteria reassessment against the original baseline metrics that records whether active surfaces reached the Phase 20 `B+` readiness gate. The phase must not start composite YubiKey design, even if the gate passes.

## Vision

The final reassessment should be a source-backed, honest quality snapshot: what improved, what remains below gate, which areas are excluded, and what decision is required next. It should use the same categories as the baseline assessment: Overall, Complexity, Maturity, DRY, Rolling Own, Maintainability, and top consolidation target. It should explicitly exclude `Tests.TestProject` from readiness scoring, record the docs QA CI gate, and stop for owner interviews before any composite-device design.

## Out of Scope

- No source module changes.
- No new broad refactor or cleanup phase.
- No composite YubiKey API design or implementation.
- No owner-interview answers invented by agents.
- No hard external package compatibility gate.
- No `Tests.TestProject` scoring in the composite-readiness gate.
- No human-coordinated FIDO2/WebAuthn UP/UV ceremony execution.

## Principles

- Use the original baseline metrics and compare against documented phase evidence.
- Prefer honest amber/red findings over inflated grades.
- Gate composite readiness on active library surfaces and `Tests.Shared`, not temporary/demo test projects.
- Separate readiness gate status from future improvement backlog.
- Make the stop gate explicit at the end of the reassessment.

## Constraints

- Branch must be `yubikit-consolidation` before reassessment edits and verification.
- Required phase inputs: baseline assessment, current final reassessment, SDK house style, master consolidation ISA, Phase 20 program ISA, and Phase 21-31 learning notes.
- Use `dotnet toolchain.cs`; never raw `dotnet build` or `dotnet test`.
- Docs command: `dotnet toolchain.cs -- docs-qa`.
- Reassessment review must use Cato or record an approved waiver because Phase 20 names final reassessment artifacts for Cato review.
- Stage only intended Phase 32 documentation files.

## Goal

Update the final reassessment for the completed Phase 20-31 program, determine whether the composite-readiness quality gate passed, record remaining risks and next actions, write Phase 32 learnings, run review/verification, commit, and stop before composite YubiKey design.

## Criteria

- [x] ISC-1: Phase 32 ISA exists before reassessment edits.
- [x] ISC-2: Branch check confirms `yubikit-consolidation` before reassessment edits.
- [x] ISC-3: Required baseline, final reassessment, house-style, program, and Phase 31 inputs were read.
- [x] ISC-4: Phase 21-31 learning notes are used as evidence inputs.
- [x] ISC-5: Updated final reassessment uses the baseline categories: Overall, Complexity, Maturity, DRY, Rolling Own, Maintainability, and top target.
- [x] ISC-6: Active readiness gate excludes `Tests.TestProject` explicitly.
- [x] ISC-7: Active readiness gate covers Core, applet/library modules including WebAuthn, and `Tests.Shared`; CLI active surfaces are tracked as non-gate follow-up work.
- [x] ISC-8: Reassessment records whether every active gate surface reached at least `B+`.
- [x] ISC-9: Reassessment records Phase 31 docs QA CI gate completion.
- [x] ISC-10: Reassessment records remaining non-gate risks separately from gate blockers.
- [x] ISC-11: Reassessment explicitly stops before composite YubiKey design/interviews.
- [ ] ISC-12: Docs QA passes after reassessment edits.
- [ ] ISC-13: Diff whitespace check passes.
- [ ] ISC-14: Cato/final reassessment review completes or an approved waiver is recorded.
- [x] ISC-15: Phase 32 learning note records findings, verification, gate result, and stop-gate status.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | file | Read ISA path | Exists | Read |
| ISC-2 | git | `git status --short --branch` | Shows `yubikit-consolidation` | Bash |
| ISC-3 | file | Required input reads | Context loaded | Read |
| ISC-4 | file | Phase 21-31 learning evidence | Notes considered | Read/Grep/Agents |
| ISC-5 | content | Matrix categories | Same categories present | Read/Grep |
| ISC-6 | content | TestProject exclusion | Explicit exclusion | Read/Grep |
| ISC-7 | content | Active surface coverage | Gate surfaces named and CLI tracked as non-gate | Read |
| ISC-8 | content | Gate result | Pass/fail recorded | Read |
| ISC-9 | content | Docs QA CI gate | Phase 31 named | Read/Grep |
| ISC-10 | content | Risks separated | Gate blockers vs follow-ups | Read |
| ISC-11 | content | Stop gate | Composite design not started | Read/Grep |
| ISC-12 | command | Docs QA | 0 failures | Bash |
| ISC-13 | whitespace | Diff whitespace | No errors | `git diff --check` |
| ISC-14 | review | Cato/final review | Pass or waiver | Cato |
| ISC-15 | file | Learning note exists | Contains evidence | Read |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Evidence Synthesis | Read Phase 21-31 learning notes and summarize grade impact. | ISC-4, ISC-7, ISC-8 | branch check | true |
| Final Reassessment Update | Update final reassessment matrix, narrative, gate result, risks, and stop gate. | ISC-5..ISC-11 | evidence synthesis | false |
| Phase Evidence | Run docs QA, diff check, Cato/review, and learning capture. | ISC-12..ISC-15 | reassessment update | false |

## Decisions

- 2026-06-08: Phase 32 is documentation/reassessment only; source changes would exceed the stop-gate scope.
- 2026-06-08: `Tests.TestProject` remains excluded from composite-readiness scoring because Phase 20 explicitly excluded it.
- 2026-06-08: Composite design stops after the reassessment result; owner interviews are required before any device-model proposal.

## Verification

- Branch check: `git status --short --branch` showed `## yubikit-consolidation...origin/yubikit-consolidation [ahead 11]` before Phase 32 reassessment edits.
- Required initial inputs read: baseline assessment, current final reassessment, SDK house style, master consolidation ISA, Phase 20 program ISA, and Phase 31 learning note.
- Evidence synthesis: three parallel assessment agents reviewed Core/tooling, SmartCard applets, and FIDO2/WebAuthn/CLI against the original categories and phase learning notes.
- Reassessment update: `docs/MODULE-CONSOLIDATION-FINAL-REASSESSMENT.md` now contains a Phase 32 post-quality-convergence addendum with a current health matrix and composite-readiness gate result.
- Gate result recorded: Core, every applet/library module including WebAuthn, and `Tests.Shared` are at least `B+`; CLI surfaces remain non-gate follow-up work; `Tests.TestProject` is excluded.
- Phase 31 docs QA CI gate recorded: commit `e82d02fb` added `dotnet toolchain.cs -- docs-qa` to `.github/workflows/build.yml`.
- Stop gate recorded: reassessment stops before composite YubiKey design and points to owner interviews.
- Docs QA command: `dotnet toolchain.cs -- docs-qa`.
- Docs QA result: passed; 54 active documentation files validated.
- Diff whitespace command: `git diff --check`.
- Diff whitespace result: passed with no output.
- Cato route: OpenAI primary routed final reassessment audit to `google-vertex-anthropic/claude-opus-4-8@default`.
- Cato result: `pass`; info notes only. One ISC-7 wording note was addressed by clarifying that CLI is tracked as non-gate follow-up work.
