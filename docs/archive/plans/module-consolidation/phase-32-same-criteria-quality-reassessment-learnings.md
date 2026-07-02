# Phase 32 Learnings: Same-Criteria Quality Reassessment

Use this note as the handoff record for Phase 32 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`.
- Scope: update the final reassessment after the Phase 20-31 quality-convergence program.
- Phase ISA: `docs/plans/module-consolidation/phase-32-same-criteria-quality-reassessment-ISA.md`.
- Source changed: none.
- Reassessment changed: `docs/MODULE-CONSOLIDATION-FINAL-REASSESSMENT.md` now has a Phase 32 post-quality-convergence gate addendum.
- Gate result: active library/composite-readiness gate passes; CLI remains non-gating and below B+.

## Evidence Inputs

- Baseline assessment: `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`.
- Prior final reassessment: `docs/MODULE-CONSOLIDATION-FINAL-REASSESSMENT.md`.
- House style: `docs/SDK-HOUSE-STYLE.md`.
- Master consolidation ISA: `docs/plans/module-consolidation/ISA.md`.
- Phase 20 program ISA: `docs/plans/module-consolidation/phase-20-quality-convergence-before-composite-yubikey-ISA.md`.
- Phase 21-31 learning notes and commits.
- Parallel assessment agents for Core/tooling, SmartCard applets, and FIDO2/WebAuthn/CLI.

## What Changed

- Added a Phase 32 addendum to the final reassessment rather than rewriting Phase 19 history.
- Added a current health matrix using the original baseline categories.
- Marked active library surfaces and `Tests.Shared` as passing the Phase 20 `B+` readiness gate.
- Marked `Cli.Shared`, `Cli`, and `Cli.Commands` as non-gate surfaces with remaining work.
- Marked `Tests.TestProject` as excluded from readiness scoring.
- Recorded Phase 31 CI docs QA completion.
- Added an explicit stop gate before composite YubiKey design.

## Gate Result

- `Core`: `B+`, pass.
- Applet/library modules: `Management`, `Piv`, `Fido2`, `WebAuthn`, `Oath`, `YubiOtp`, `OpenPgp`, `SecurityDomain`, and `YubiHsm` are all `B+` overall, pass.
- `Tests.Shared`: `B+`, pass.
- `Cli.Shared`, `Cli`, and `Cli.Commands`: non-gate surfaces; still below B+ in places and preserved as follow-up work.
- `Tests.TestProject`: excluded by Phase 20 policy.
- Composite-readiness quality gate: pass for the active library surfaces, with mandatory stop before owner interviews/design.

## Remaining Risks

- Core CRC/API-shape cleanup remains the most relevant A-range library follow-up.
- FIDO2 manual CBOR surfaces remain outside the highest-value Phase 26 paths.
- WebAuthn passes overall but still has a large `WebAuthnClient` orchestration surface.
- CLI command-family secret handling and duplicated parsing/session helpers remain below B+ and should be handled one command family at a time.
- FIDO2/WebAuthn UP/UV/touch ceremonies remain human-coordinated and were not converted into unattended gates.
- `docs-qa` is now in CI but does not compile README snippets.

## Verification Evidence

- Branch check command: `git status --short --branch`.
- Branch check result: `## yubikit-consolidation...origin/yubikit-consolidation [ahead 11]` before Phase 32 edits.
- Evidence synthesis: three parallel assessment agents returned grade recommendations and gate interpretations for Core/tooling, SmartCard applets, and FIDO2/WebAuthn/CLI.
- Docs QA command: `dotnet toolchain.cs -- docs-qa`.
- Docs QA result: passed; 54 active documentation files validated.
- Diff whitespace command: `git diff --check`.
- Diff whitespace result: passed with no output.
- Cato route: `openai/gpt-5.5` primary routed final reassessment audit to `google-vertex-anthropic/claude-opus-4-8@default`.
- Cato result: `pass`; info notes only.
- Cato follow-up: tightened ISA ISC-7 wording so CLI cannot be read as a composite-readiness gate surface.

## What Did Not Work

- The parallel assessments disagreed on whether CLI should be counted as a composite-readiness gate surface. Phase 20 resolves this: broad CLI is deferred, so CLI remains a tracked active consumer but not a composite-library gate blocker.

## Reusable Patterns

- For final reassessments, preserve historical reassessment text and add a dated addendum when later phases supersede it.
- Separate gate surfaces from non-gate active consumers to avoid inflating readiness or blocking on explicitly deferred work.
- Use parallel assessment agents for broad grade synthesis, then reconcile against the controlling ISA.

## Deferred Candidates

- Core CRC/API-shape follow-up.
- CLI command-family secret and parsing consolidation.
- Optional `actionlint` or workflow syntax tooling for future CI edits.
- README snippet compilation if the owner wants executable examples beyond docs hygiene.

## Next Phase Inputs

- There is no next autonomous implementation phase in this program.
- Stop for owner interviews before composite YubiKey discovery design.
- Saved interview questions live in `docs/plans/module-consolidation/phase-20-quality-convergence-before-composite-yubikey-ISA.md`.

## Compact Summary

- Goal: regrade after quality convergence.
- Gate result: library composite-readiness passes.
- CLI result: non-gate follow-up remains.
- Stop: owner interviews before composite design.
