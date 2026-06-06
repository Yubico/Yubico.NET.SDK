# Phase 9 Learnings: Documentation Repair Pass

Use this note as the handoff record for Phase 9 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Base branch: `yubikit-applets`
- Base commit: `bfc6bdd5`, per consolidation ISA
- Branch check command/result: `git status --short --branch` showed `## yubikit-consolidation`
- Unrelated worktree changes present: yes, two untracked Core YubiKey note files remained unstaged
- Refactor work ran only on `yubikit-consolidation`: yes
- Scope: repair high-confidence active documentation drift after Phase 8B, with no SDK source-code changes
- Criteria satisfied: yes, after Cato-surfaced FIDO2 and PIV documentation fixes
- Criteria deferred: exhaustive archived docs/plans cleanup, docfx/link-check validation because no config was present, broad sample snippet compilation beyond focused module builds
- Promotion candidates declared up front: none; documentation-only phase
- Files changed: root README, selected module READMEs, selected module/test `CLAUDE.md` files, Phase 9 ISA, this learning note
- Tests run: none
- Builds run: Core, Fido2, Management, SecurityDomain, and Piv via `dotnet toolchain.cs -- build --project <Module>`
- Integration tests run: none
- Result: passed focused grep/build verification; Cato returned concerns and the concrete finding was fixed
- Commit: `a20afb55 docs: repair module consolidation guidance`
- `/Ping` sent after successful phase: pending closeout summary

## Hardware Target

- Device: YubiKey 5.8 beta
- Serial: `103`
- Firmware source of truth: Management `GetDeviceInfoAsync`
- Management firmware observed: not re-run in this phase
- Applet firmware observed, if observable: not applicable
- Applet firmware caveat observed: not applicable

## Integration Lifecycle

- Management preflight command/result: skipped
- Management preflight evidence captured before applet tests: not applicable; documentation-only phase
- Management preflight exception path used: no
- Alternate identity proof, if preflight skipped: not needed
- Agent-runnable integration test allowlist: none
- Integration scope was read-only: not applicable
- Tests run: none
- Tests skipped: all hardware/integration tests
- Skip reason: phase repaired documentation only and did not change SDK behavior
- Skip approved by: consolidation ISA read-only integration rule and Phase 9 documentation-only ISA
- Selected tests mutate persistent state: not applicable
- User Presence / UV required: not applicable
- Human-coordinated hardware needed: no
- Persistent state changed: no
- Destructive tests skipped completely: yes
- Reset/cleanup performed: no
- Result: module builds and grep probes are final technical verification for this phase

## What Worked

- Pattern that improved readability: replacing stale long-form docs with shorter source-backed module guides in `Piv` and `Tests.Shared`.
- Pattern that improved testability: grep probes targeted exact stale names and command forms from the inventory.
- Pattern that improved security/memory hygiene: active docs now reinforce secret zeroing and avoid stale KeyCollector-era examples.

## What Did Not Work

- Rejected approach: exhaustive rewrite of every README discovered by inventory.
- Rejected approach rationale: broad README rewrites would require snippet-by-snippet API validation and would exceed a reviewable Phase 9 slice.
- Helpers or abstractions that were too deep: none; no source helpers were added.
- Changes that looked DRY but harmed readability: none; documentation was shortened rather than generalized.

## House Style Updates

- Existing house-style rule confirmed: documentation must stay source-backed and flat-flow aligned.
- Existing house-style rule confirmed: user-facing commands should use `dotnet toolchain.cs` for build/test guidance.
- Rule that needs clarification: example projects are not discoverable by the current toolchain project matcher, so example READMEs should avoid claiming `--project FidoTool`-style toolchain builds unless tooling changes.
- Possible addition to `docs/SDK-HOUSE-STYLE.md`: active module docs should avoid compile-looking snippets unless the referenced API was verified against source.

## Reusable Patterns

- Pattern: phase-local stale-doc grep set built from read-only inventory agents.
- Generalization class: candidate for one more documentation phase trial.
- Where it applies: active README/CLAUDE documentation repair after API-shape changes.
- Where it should not apply: archived plans, historical specs, or old design documents without explicit archival cleanup scope.
- Example files: `src/Piv/CLAUDE.md`, `src/Piv/README.md`, `src/Tests.Shared/README.md`

## Core / Shared Promotion Candidates

- Candidate: none
- Declared in phase ISA up front: yes
- Should move to: not applicable
- Evidence: no source or shared helper changes were made
- Risk: none
- Decision: rejected
- Decision rationale: documentation-only phase
- Revisit trigger: not applicable
- Demotion/reversal needed for previous shared helper: no
- Demotion/reversal rationale: not applicable

## Cross-Module Implications

- Modules affected: Core, Fido2, Management, Oath, OpenPgp, Piv, SecurityDomain, Tests.Shared, YubiHsm, YubiOtp, root docs
- Next module should copy: source-backed docs repair with grep probes and module builds.
- Next module should avoid: asserting APIs from memory or from stale docs without source verification.
- Potential API compatibility concern: none; no SDK source or public API changed.

## Verification Evidence

- Branch check commands: `git status --short --branch`
- Branch check exit result: passed; branch was `yubikit-consolidation`
- Build commands: `dotnet toolchain.cs -- build --project Core`; `dotnet toolchain.cs -- build --project Fido2`; `dotnet toolchain.cs -- build --project Management`; `dotnet toolchain.cs -- build --project SecurityDomain`; `dotnet toolchain.cs -- build --project Piv`
- Build exit result: passed, 0 warnings, 0 errors for all five focused module builds
- Unit test commands: none
- Unit test exit result: not applicable
- Integration test commands: none
- Integration test exit result: not applicable
- Command filters/projects: Core, Fido2, Management, SecurityDomain, Piv builds only
- Cross-module verification plan, if shared infrastructure changed: build affected owning modules and run stale-pattern grep probes; no behavior tests because docs only
- Grep result: `^dotnet (test|build)(\s|$)` under `src/**/*.md` returned no matches
- Grep result: stale active names under `src/**/*.md` returned no matches for the Phase 9 pattern set
- Grep result: PIV legacy patterns under `src/Piv/**/*.md` returned no matches
- Diff result: `git diff --name-only -- "*.cs"` returned no files
- Doc tooling result: no `docfx*.json` or link-check config found, so doc generator/link checker verification was unavailable
- Manual review notes: diff is documentation-only plus Phase 9 plan artifacts; unrelated Core YubiKey note files remained unstaged
- Reviewer concerns resolved: Cato found FIDO2 README/CLAUDE transport contradiction and a partial-audit PIV `GetPublicKeyAsync` claim; both were fixed

## Review Summary

- DevTeam engineer result: not run; documentation-only phase with read-only inventory agents
- DevTeam reviewer result: not run; final review used advisor plus Cato
- Advisor result: warned to check doc tooling, diff scope, stale namespaces, and sample/API claims; docfx/link-check config was absent; stale active claims were grep-checked
- Cross-vendor review result: completed through `google-vertex-anthropic/claude-opus-4-8@default`; first run timed out with useful partial audit, retry returned `concerns`/medium
- Cross-vendor review waiver, if any: none
- Cato prompt/output: `/tmp/opencode/cato-phase9-docs-prompt.txt`, `/tmp/opencode/cato-phase9-docs-audit.jsonl`, `/tmp/opencode/cato-phase9-docs-prompt-retry.txt`, `/tmp/opencode/cato-phase9-docs-audit-retry.jsonl`
- Findings fixed: removed nonexistent PIV `GetPublicKeyAsync` from active guide; reconciled FIDO2 README transport text with module CLAUDE/source
- Findings deferred: no docfx/link-check verification because no repo config was found; exhaustive archived docs cleanup remains out of scope
- Human decisions: none needed after concrete Cato findings were fixed

## Cato Findings

| Severity | Finding | Disposition |
| --- | --- | --- |
| warning | `src/Piv/CLAUDE.md` listed nonexistent `GetPublicKeyAsync(...)` after the PIV doc replacement. | Fixed by removing the method from the current public operations list. |
| warning | `src/Fido2/README.md` contradicted `src/Fido2/CLAUDE.md` by saying USB CCID was unsupported while module docs/source allow SmartCard FIDO2 on firmware 5.8.0+ when the AID is exposed. | Fixed by replacing the top transport warning with the same qualified SmartCard guidance used by the transport table. |

## Deferred Future Improvements

- Title: Exhaustive active README snippet validation
- Source phase: Phase 9 Documentation Repair Pass
- Rationale: Several module READMEs still contain broad examples that deserve full compile-backed snippet review.
- Why it is deferred: this phase targeted high-confidence stale guidance and avoided speculative broad rewrites.
- Likely owning area: docs / module READMEs
- Suggested timing: later dedicated docs QA pass
- Needs human approval, hardware coordination, or Cato review: Cato review recommended; no hardware unless examples require applet mutation

- Title: Add documentation build or link-check tooling
- Source phase: Phase 9 Documentation Repair Pass
- Rationale: Advisor requested docfx/link validation, but no repo config was present.
- Why it is deferred: adding doc tooling is infrastructure scope, not documentation repair.
- Likely owning area: tooling / docs
- Suggested timing: after module consolidation or as a focused tooling phase
- Needs human approval, hardware coordination, or Cato review: human approval recommended; no hardware

- Title: Reconcile AGENTS.md FIDO2 SmartCard gotcha with current source
- Source phase: Phase 9 Documentation Repair Pass
- Rationale: AGENTS.md still says SmartCard FIDO transport is NFC-only, while Fido2 module source and tests include a firmware 5.8+ USB SmartCard path.
- Why it is deferred: AGENTS.md governance update is broader than active module doc repair and may need project-level approval.
- Likely owning area: root agent guidance / Fido2 docs
- Suggested timing: next documentation-governance cleanup
- Needs human approval, hardware coordination, or Cato review: human approval and Cato review recommended

## Abort / Split Assessment

- Wrong branch detected: no
- Phase exceeded approved scope: no; scope expanded within documentation-only bounds to fix active PIV docs after advisor/Cato evidence
- Public API change required: no
- Helper depth concern found: no source helpers changed
- Protocol flow became harder to inspect: no source changed; docs now reinforce flat-flow guidance
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
- Required reading before next phase: this learning note
- Pattern to apply: use source-backed grep probes before replacing compile-looking examples.
- Risk to watch: root/project agent guidance may still lag source reality in places like FIDO2 SmartCard transport.
- Open questions for human approval: whether to run a dedicated docs QA/tooling pass or proceed to final follow-up improvement triage.

## Compact Summary

- Goal: repair high-confidence stale active documentation after consolidation phases
- Files changed: root README, module READMEs, module/test CLAUDE files, Phase 9 ISA, learning note
- Final pattern: source-backed concise docs plus exact stale-pattern grep verification
- Rejected approaches: broad archived docs cleanup, speculative README rewrites, hardware checks
- Tests passed: none; docs-only phase
- Builds passed: Core, Fido2, Management, SecurityDomain, Piv
- Integration lifecycle: skipped because docs-only and no hardware behavior changed
- Shared/Core candidates: none
- Deferred future improvements: full snippet validation, doc/link tooling, AGENTS FIDO2 SmartCard guidance reconciliation
- House-style update needed: document example-tool build guidance and source-backed snippet rule
- Next phase recommendation: final follow-up improvement pass or docs QA tooling decision
- Learning note path: `docs/plans/module-consolidation/phase-9-documentation-repair-learnings.md`
- Commit: `a20afb55 docs: repair module consolidation guidance`
- `/Ping` status: pending
