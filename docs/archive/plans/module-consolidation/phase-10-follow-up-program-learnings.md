# Phase 10 Learnings: Follow-Up Program Work Plan

Use this note as the handoff record for Phase 10 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Base branch: `yubikit-applets`
- Base commit: `bfc6bdd5`, per consolidation ISA
- Branch check command/result: `git status --short --branch` showed `## yubikit-consolidation`
- Unrelated worktree changes present: yes, two untracked Core YubiKey note files remained unstaged
- Refactor work ran only on `yubikit-consolidation`: yes
- Scope: create a docs-only follow-up program work plan and reference it from the consolidation ISA
- Criteria satisfied: yes, after Cato-surfaced firmware/enum guardrails were folded into the work plan and ISA
- Criteria deferred: source implementation for Phases 11-19
- Promotion candidates declared up front: none; documentation-only phase
- Files changed: this learning note, Phase 10 work plan, consolidation ISA
- Tests run: none
- Builds run: none
- Integration tests run: none
- Result: passed docs-only verification; Cato returned `pass` with info-level follow-up guardrails
- Commit: `2e5c3537 docs: add module consolidation follow-up plan`
- `/Ping` sent after successful phase: pending closeout summary

## What Worked

- Pattern that improved readability: recording follow-up work as a phase sequence instead of a single broad cleanup bucket.
- Pattern that improved testability: each future phase has a distinct verification surface rather than one blended final pass.
- Pattern that improved security/memory hygiene: CLI secret-policy work is split into policy plus one named OATH password unlock migration.

## What Did Not Work

- Rejected approach: combine all deferred concerns into one final follow-up implementation phase.
- Rejected approach rationale: concern families have different owners, risks, and verification needs.
- Helpers or abstractions that were too deep: none; no source helpers were added.
- Changes that looked DRY but harmed readability: none; this phase is documentation-only.

## House Style Updates

- Existing house-style rule confirmed: keep protocol flow flat and avoid command-object abstractions.
- Existing house-style rule confirmed: `Feature` should not grow into a generic support-policy object.
- Rule that needs clarification: complete support decisions belong at the narrowest layer with all facts, not in firmware metadata.

## Core / Shared Promotion Candidates

- Candidate: none
- Declared in phase ISA up front: yes
- Should move to: not applicable
- Evidence: no source or shared helper changes were made
- Risk: none
- Decision: rejected
- Decision rationale: documentation-only phase
- Revisit trigger: next source phase ISA

## Cross-Module Implications

- Modules affected: all modules through follow-up planning only
- Next module should copy: phase-specific source approval before implementation starts
- Next module should avoid: treating Phase 10 as approval to edit source code
- Potential API compatibility concern: none in Phase 10; Phase 16 will check branch-level API/package risk

## Verification Evidence

- Branch check commands: `git status --short --branch`
- Branch check exit result: passed; branch was `yubikit-consolidation`
- Build commands: none
- Build exit result: not applicable
- Unit test commands: none
- Unit test exit result: not applicable
- Integration test commands: none
- Integration test exit result: not applicable
- Command filters/projects: not applicable
- Cross-module verification plan, if shared infrastructure changed: not applicable; docs-only phase
- Manual review notes: diff is documentation-only; unrelated Core YubiKey note files remained unstaged

## Review Summary

- DevTeam engineer result: not run; documentation-only phase
- DevTeam reviewer result: not run; final review uses Cato
- Cross-vendor review result: completed through `google-vertex-anthropic/claude-opus-4-8@default`; final verdict `pass`, criticality `medium`, with info-level findings only
- Cross-vendor review waiver, if any: none
- Cato prompt/output: workspace-local temporary files `.cato-phase10-follow-up-plan-prompt.txt` and `.cato-phase10-follow-up-plan-audit-4.jsonl` used because the routed auditor could not read `/tmp`; temporary files removed before commit
- Findings fixed: made the Phase 13 `IsAlphaOrBeta` behavior change explicit; added `FirmwareVersionTests` update requirement; added comparison/ordering consumer guardrail; added `PcscProtocol` sentinel consumer inventory; added exact `ConnectionType` equality/numeric-value guidance; clarified Phase 15 policy scope
- Findings deferred: none from final Cato pass; info-level `Transport` inspection is recorded as inspect-only unless explicitly approved
- Human decisions: approved Phase 10 work-plan creation and approved Phase 13 firmware-gate design direction

## Deferred Future Improvements

- Title: Implement Phase 11 Core SCP chained-response follow-up
- Source phase: Phase 10 Follow-Up Program Work Plan
- Rationale: potential protocol correctness concern needs byte-level characterization before any fix.
- Why it is deferred: Phase 10 is documentation-only governance.
- Likely owning area: `Core` / `Oath`
- Suggested timing: Phase 11
- Needs human approval, hardware coordination, or Cato review: human approval and Cato review; hardware optional/read-only unless explicitly approved

## Cato Findings

| Severity | Finding | Disposition |
| --- | --- | --- |
| warning | Initial audit found Phase 13 described `IsAlphaOrBeta => Major == 0` without saying this intentionally contradicts current `0.0.0`-only source/tests. | Fixed by documenting the behavior/test change and requiring `FirmwareVersionTests` updates. |
| warning | Initial audit found `ApplicationSession.IsSupported` already uses `Major == 0` while `IsAlphaOrBeta` is stricter, making Phase 13 a real semantic reconciliation. | Fixed by documenting that Phase 13 preserves current session sentinel behavior while moving it behind one name. |
| info | Final audit noted Phase 12 should record exact equality (`HidOtp == Hid | HidFido`) and explicit numeric-value risk. | Fixed by adding exact-equality, explicit-value, and compatibility coverage requirements. |
| info | Final audit noted Phase 13 must also cover `CompareTo`, `IsAtLeast`, `IsLessThan`, `PcscProtocol`, and `Feature.Version` comparison behavior. | Fixed by adding those guardrails to the work plan and ISA. |
| info | Final audit noted Phase 15 policy is program-wide although one migration slice is OATH unlock only. | Fixed by making the argv policy program-wide while keeping the implementation slice narrow. |

## Abort / Split Assessment

- Wrong branch detected: no
- Phase exceeded approved scope: no
- Public API change required: no
- Helper depth concern found: no source helpers changed
- Protocol flow became harder to inspect: no source changed
- Verification failed twice for different root causes: no
- Unapproved hardware coordination required: no
- Persistent-state or destructive integration required: no
- Core/shared promotion became unavoidable: no
- Abort learning note required: no
- Abort learning note committed with human approval: not applicable
- Outcome: continue

## Next Phase Inputs

- Required reading before next phase: `docs/SDK-HOUSE-STYLE.md`
- Required reading before next phase: `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`
- Required reading before next phase: `docs/plans/module-consolidation/ISA.md`
- Required reading before next phase: `docs/plans/module-consolidation/phase-10-follow-up-program-work-plan.md`
- Required reading before next phase: this learning note
- Pattern to apply: start Phase 11 with a phase-specific ISA before source changes.
- Risk to watch: do not merge independent Core concerns into one broad cleanup phase.
- Open questions for human approval: Phase 11 source scope and integration-test allowlist.

## Compact Summary

- Goal: document follow-up program after completed module consolidation phases
- Files changed: Phase 10 work plan, Phase 10 learning note, consolidation ISA
- Final pattern: docs-only triage with explicit fix/defer/reject/final-audit boundaries
- Rejected approaches: broad final cleanup, generic support-policy abstraction, final assessment rewrite
- Tests passed: none; docs-only phase
- Builds passed: none; docs-only phase
- Integration lifecycle: skipped because docs-only and no hardware behavior changed
- Shared/Core candidates: none
- Deferred future improvements: all Phase 11-19 source/audit phases
- House-style update needed: consider documenting layered support ownership after Phase 13
- Next phase recommendation: Phase 11 Core SCP chained-response phase ISA
- Learning note path: `docs/plans/module-consolidation/phase-10-follow-up-program-learnings.md`
- Commit: `2e5c3537 docs: add module consolidation follow-up plan`
- `/Ping` status: pending closeout summary
