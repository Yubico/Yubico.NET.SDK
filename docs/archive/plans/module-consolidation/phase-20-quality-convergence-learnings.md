# Phase 20 Learnings: Quality Convergence Program ISA

Use this note as the handoff record for Phase 20 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Scope: create the quality-convergence program ISA before composite YubiKey design
- Phase ISA: `docs/plans/module-consolidation/phase-20-quality-convergence-before-composite-yubikey-ISA.md`
- Source files changed: none
- Test files changed: none
- Integration tests: not applicable for Phase 20; later source phases may run documented integration tests against the connected 5.8 beta key
- Result: Phase 20 program artifacts created, reviewed, patched, and docs-only verification completed
- Commit: this commit
- `/Ping` status: to be sent after commit

## Owner Decisions Captured

- `Tests.TestProject` is excluded from the quality program because it was a temporary DI test project and DI was removed to avoid premature optimization.
- Integration tests are allowed. A connected YubiKey 5.8 beta key is available, and documented tests with reset/init harnesses may run when a phase ISA names them.
- FIDO2/WebAuthn UP, UV, touch, and ceremony tests still need explicit coordination when physical presence is required.
- CLI work is deferred until library modules are stronger.
- `dotnet toolchain.cs -- docs-qa` should become a CI gate.
- Composite YubiKey design is blocked until `Core`, every applet module, and `Tests.Shared` are at least `B+`.
- Package validation remains audit-only because there is no broad external .NET user base yet.
- Public API shape should stay aligned with the Yubico SDK family: `yubikit-swift`, Python `yubikey-manager` / `yubikit`, and `yubikit-android`, with intentional .NET 10 / C# 14 divergences allowed when clearly better.
- Composite YubiKey questions are saved for a later owner interview; agents must stop before starting that design.

## What Changed

- Created a Phase 20 program ISA with all source phases sequenced through the final quality reassessment.
- Reframed the old API/package compatibility topic as a public API shape and SDK-family alignment audit.
- Recorded that package validation is audit-only, not an external-consumer compatibility gate.
- Added the composite YubiKey stop gate and saved the interview questions for later.
- Made `Tests.Shared` part of the composite-readiness quality gate.
- Kept CLI outside the composite-readiness gate while deferring broad CLI work until library modules are stronger.
- Carried forward the extended APDU follow-up closure so it is no longer treated as an open investigation.

## Why This Shape

- Composite YubiKey discovery is a public Core device-model decision, so it should wait until applet and test-harness quality are consistent.
- The current real consumers are CLI and integration tests, but users will benefit if the public API remains conceptually familiar across Yubico SDKs.
- Forcing package/API baseline compatibility now would freeze unstable design too early; auditing SDK-family alignment preserves discipline without freezing the wrong shape.
- `Tests.Shared` is an enabler for quality convergence and should stay at or above `B+` before the composite program starts.

## Verification Evidence

- Branch check: `git status --short` showed only intended docs changes for Phase 20 before staging.
- Phase 20 ISA readback: DevTeam and Cato reruns read `docs/plans/module-consolidation/phase-20-quality-convergence-before-composite-yubikey-ISA.md`.
- Master ISA readback: DevTeam and Cato reruns read `docs/plans/module-consolidation/ISA.md`.
- DevTeam rerun: `/tmp/opencode/phase20-devteam-rerun.jsonl` returned `pass` with info-only skim-proofing suggestions.
- Cato rerun: `/tmp/opencode/phase20-cato-rerun.jsonl` returned `pass` with info-only skim-proofing suggestions.
- Local verification: `dotnet toolchain.cs -- docs-qa` succeeded and validated 54 active documentation files.
- Local verification: `git diff --check` passed.
- Source-diff check: `git diff --name-only` listed only `docs/**` tracked files, and `git status --short` listed only intended documentation edits and the two new Phase 20 documentation files.

## Integration Lifecycle

- Hardware target: connected YubiKey 5.8 beta key is available for later phases.
- Phase 20 Management preflight: not applicable; Phase 20 is documentation/governance only.
- Integration scope: none for Phase 20.
- Persistent state changed: no.
- Destructive tests: none.

## Review Evidence

- DevTeam route: `google-vertex-anthropic/claude-opus-4-8@default`, resolved through `AgentHarnessRouter.ts` with primary model `openai/gpt-5.5`.
- DevTeam output: `/tmp/opencode/phase20-devteam-rerun.jsonl`.
- DevTeam verdict: `pass`; info-only suggestions were applied for skim-proof clarity.
- Cato route: `google-vertex-anthropic/claude-opus-4-8@default`, resolved through `AgentHarnessRouter.ts` with primary model `openai/gpt-5.5`.
- Cato output: `/tmp/opencode/phase20-cato-rerun.jsonl`.
- Cato verdict: `pass`; info-only suggestions were applied for skim-proof clarity.
- Findings resolved: stale API/package enforcement wording removed, stale extended-APDU deferred-risk wording closed by commit `90a41b26`, and Phase 19 historical text now points to the closure note.

## Deferred Future Improvements

- Start composite YubiKey design only after Phase 32 reassessment passes and owner interviews complete.
- Reconsider DI only after constructors and device composition stabilize.
- Revisit broader CLI cleanup after library modules reach the target quality gate.
- Decide whether package validation should become a hard external release gate only when there is a real preview/release baseline to protect.

## Cross-Module Implications

- Core Phase 21 becomes a design-alignment audit rather than a package-freeze phase.
- Tests.Shared Phase 22 is now part of the composite-readiness gate, not just optional infrastructure polish.
- Applet phases should prioritize byte-level behavior tests and maintainability improvements that move `B` modules toward `B+`.
- CLI does not block composite readiness, but CLI behavior remains a current consumer of public SDK shape.

## Generalization Check

- Pattern classification: program-control ISA pattern is reusable for future multi-phase cleanup programs.
- Reusable lesson: package compatibility and SDK-family API alignment are related but not identical.
- Not promoted to shared code: no code should be touched in Phase 20.

## Compact Summary

- Goal: define quality convergence before composite YubiKey design.
- Files changed: Phase 20 ISA, master ISA, final reassessment, Phase 19 learning/ISA notes, and stale Phase 10/14 extended-APDU carry-forward notes.
- Final pattern: phase ISA, DevTeam/review/learn/commit loop, stop before composite.
- Rejected approaches: hard API freeze, immediate composite design, broad CLI cleanup.
- Tests passed: `dotnet toolchain.cs -- docs-qa`; `git diff --check`.
- Integration lifecycle: none; docs-only phase.
- Shared/Core candidates: Core API alignment, Tests.Shared recorder decision.
- Deferred future improvements: composite YubiKey interviews and design.
- House-style update needed: none now.
- Next phase recommendation: Core A- readiness and SDK-family API alignment audit.
- Learning note path: `docs/plans/module-consolidation/phase-20-quality-convergence-learnings.md`
- Commit: this commit.
- `/Ping` status: to be sent after commit.
