# Phase 4 Learnings: PIV De-Partialization

Use this note as the handoff record for Phase 4 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Base branch: `yubikit-applets`
- Base commit: `bfc6bdd5`
- Branch check command/result: `git status --short --branch` showed `## yubikit-consolidation`
- Unrelated worktree changes present: yes, pre-existing untracked files under `src/Core/src/YubiKey/`
- Refactor work ran only on `yubikit-consolidation`: yes
- Scope: remove `PivSession` partial classes while preserving `PivSession` as the single public facade over `IPivSession`
- Criteria satisfied: no `PivSession` partials remain, no operation-specific command classes were added, public API shape is preserved, PIV protocol areas moved into shallow feature namespaces, fake APDU tests cover representative facade delegation paths, full PIV unit and integration baselines passed
- Criteria deferred: broader reset/auth/default-credential helper cleanup, possible shared fake smart-card recorder, deeper PIV crypto APDU matrix coverage
- Promotion candidates declared up front: none accepted; possible shared test recorder deferred until another module proves reuse
- Files changed: PIV session source, PIV feature helper source files, PIV unit tests, PIV module guidance, consolidation ISA, this learning note
- Tests run: scoped format verification, PIV build, PIV unit tests, full PIV integration tests
- Integration tests run: full `Piv.IntegrationTests` baseline before and after refactor on serial `103`
- Result: implementation, verification, Cato review, and Cato follow-up completed; Cato's touch-notification finding was fixed and covered by a new regression test
- Commit: pending at note update time; next action after staging approved files
- `/Ping` sent after successful phase: queued after commit and compact summary

## Hardware Target

- Device: YubiKey 5.8 beta
- Serial: `103`
- Firmware source of truth: Management `GetDeviceInfoAsync` / integration infrastructure device discovery
- Management firmware observed: `5.8.0`
- Applet firmware observed, if observable: full PIV integration test discovery selected SmartCard device serial `103`, firmware `5.8.0`
- Applet firmware caveat observed: Management/device discovery remains source of truth for beta hardware identity

## Integration Lifecycle

- Baseline integration command/result before refactor: `dotnet toolchain.cs -- test --integration --project Piv.IntegrationTests` passed, 70 total, 70 passed
- Post-refactor integration command/result: `dotnet toolchain.cs -- test --integration --project Piv.IntegrationTests` passed, 70 total, 70 passed
- Management/device identity evidence captured before applet tests: yes, integration infrastructure reported serial `103`, firmware `5.8.0`, SmartCard transport authorized
- Management preflight exception path used: no
- Alternate identity proof, if preflight skipped: not applicable
- Agent-runnable integration test allowlist: human approved the full PIV integration suite for this phase on beta serial `103`
- Integration scope was read-only: no
- Tests run: full PIV integration suite including reset, PIN/PUK, key generation/import/delete/move, certificates, metadata, signing, decrypt, and key agreement flows
- Tests skipped: none from `Piv.IntegrationTests`
- Skip reason: not applicable
- Skip approved by: not applicable
- Selected tests mutate persistent state: yes, human-approved for beta hardware serial `103`
- User Presence / UV required: no selected output indicated physical touch, UV, insert/remove, or human-only interaction
- Human-coordinated hardware needed: beta key was present and authorized before test run
- Human-coordinated hardware scheduled/deferred/replaced: destructive/mutating PIV tests were explicitly approved for this phase instead of replaced by smoke-only checks
- Persistent state changed: yes, by design inside PIV integration tests that reset and rewrite applet state
- Destructive tests skipped completely: no, approved full suite was run
- Reset/cleanup performed: covered by integration tests' own reset/default-state flows
- Result: post-refactor baseline matched pre-refactor baseline, 70/70 passed

## What Worked

- Patterns that improved readability: `PivSession` now shows public lifecycle/state and one-hop delegation, while protocol-heavy code sits under shallow feature namespaces.
- Patterns that improved testability: fake APDU tests now prove representative facade calls transmit the expected SELECT, GET METADATA, and GET DATA APDUs without hardware.
- Patterns that improved security/memory hygiene: existing sensitive buffer handling stayed inside feature helpers; the refactor did not introduce new owned secret buffers or logging surfaces.
- Patterns that improved maintainability: replacing partial files with named feature areas makes ownership explicit without creating command objects or a service layer.

## What Did Not Work

- Rejected approaches: preserving partial classes would keep compile-time scattering and make it harder to distinguish public facade responsibilities from protocol implementation areas.
- Helpers or abstractions that were too deep: no `Piv*Command`, service/manager layer, DI surface, or operation-specific execution class was introduced.
- Changes that looked DRY but harmed readability: reset/auth/default-credential helper consolidation was deferred rather than folded into this structural pass.
- Tooling issue: focused PIV unit filters hit the known xUnit v3/toolchain `--minimum-expected-tests` argument problem; the full PIV unit project command passed and is the recorded evidence.
- Coverage limitation: the added fake APDU tests are representative, not an exhaustive APDU matrix for every PIV cryptographic operation.

## House Style Updates

- Existing house-style rule confirmed: public session facade remains the API anchor; helper extraction must keep protocol flow inspectable.
- Existing house-style rule confirmed: avoid operation-specific command classes even when splitting a large session implementation.
- Rule clarified in module guidance: PIV should not use `PivSession` partial classes; use shallow feature namespaces behind the single facade.
- Possible addition to `docs/SDK-HOUSE-STYLE.md`: large session facades may delegate to shallow feature protocol helpers when partial classes obscure ownership, but the facade remains the public API and no command layer is introduced.

## Reusable Patterns

- Pattern: single public applet session facade plus shallow internal feature protocol helpers.
- Generalization class: candidate for one more module trial, not automatic SDK-wide rule.
- Where it applies: modules where partial classes have become a structural dumping ground and feature boundaries are already natural.
- Where it should not apply: modules where partials still make protocol flow easier to inspect, or where splitting would create command-like operation wrappers.
- Example files: `src/Piv/src/PivSession.cs`, `src/Piv/src/Authentication/PivAuthenticationProtocol.cs`, `src/Piv/src/Metadata/PivMetadataProtocol.cs`

## Core / Shared Promotion Candidates

- Candidate: shared fake smart-card recording helper
- Declared in phase ISA up front: not as an accepted promotion; discovered during PIV unit test work
- Should move to: possibly `Tests.Shared`
- Evidence: PIV now has a small `RecordingSmartCardConnection` for APDU assertions; similar needs may recur in SecurityDomain or FIDO-related fake protocol tests
- Risk: premature sharing could create another test abstraction with too many knobs before a second module proves the shape
- Decision: deferred
- Decision rationale: one module is not enough evidence for a shared test primitive under the consolidation generalization rule
- Revisit trigger: another module adds a similar APDU recording fake or duplicates the same connection wrapper behavior
- Demotion/reversal needed for previous shared helper: no
- Demotion/reversal rationale: not applicable

## Cross-Module Implications

- Modules likely affected: SecurityDomain if it uses partials or has protocol-heavy session areas; future phases should decide from local readability rather than copying PIV blindly.
- Next module should copy: the one-hop facade delegation pattern only when it reduces compile-time scattering and keeps operation flow visible.
- Next module should avoid: turning each applet operation into its own class or hiding APDU construction behind broad service abstractions.
- Potential API compatibility concern: none observed; `PivSession` remains the public facade and `IPivSession` remains the public contract.

## Verification Evidence

- Branch check commands: `git status --short --branch`
- Branch check exit result: success, showed `## yubikit-consolidation`
- Format commands: `dotnet format --verify-no-changes --include src/Piv/src src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/PivSessionTests.cs src/Piv/CLAUDE.md docs/plans/module-consolidation/ISA.md`
- Format exit result: passed after scoped `dotnet format`
- Build commands: `dotnet toolchain.cs -- build --project Piv`
- Build exit result: passed, 0 warnings, 0 errors across `Yubico.YubiKit.Piv`, `Yubico.YubiKit.Piv.IntegrationTests`, and `Yubico.YubiKit.Piv.UnitTests`
- Unit test commands: `dotnet toolchain.cs -- test --project Piv`
- Unit test exit result: passed, 61 total, 61 succeeded, 0 failed, 0 skipped
- Baseline integration command before refactor: `dotnet toolchain.cs -- test --integration --project Piv.IntegrationTests`
- Baseline integration exit result before refactor: passed, 70 total, 70 passed
- Post-refactor integration commands: `dotnet toolchain.cs -- test --integration --project Piv.IntegrationTests`
- Post-refactor integration exit result: passed, 70 total, 70 passed, total time 4.6327 minutes on the final post-Cato-fix run
- Command filters/projects: `Piv`, `Piv.IntegrationTests`; no filter used for final unit or integration evidence
- Cross-module verification plan, if shared infrastructure changed: no shared source changed in this phase
- Results: all focused verification passed and post-refactor integration matched baseline
- Manual review notes: Cato initially found a real `DecryptAsync` touch-notification regression plus stale placeholder/blank-line cleanup nits; all findings were fixed and rechecked
- Reviewer concerns resolved: `DecryptAsync` now passes `NotifyTouchIfRequiredAsync` into the crypto helper, `PivCryptographicOperations.DecryptAsync` invokes it immediately before the raw private-key operation, the stale Phase 2 comments/blank-line clusters were removed, and Cato follow-up returned `verdict: pass`

## Review Summary

- DevTeam engineer result: primary implementation completed by GPT-5.5/OpenCode with DevTeam-style exploratory subagents for refactor map and test classification
- DevTeam reviewer result: DevTeam-style exploratory review completed through subagents before implementation; final cross-vendor review handled by Cato
- Cross-vendor review result: Cato initial audit returned concerns for lost `DecryptAsync` touch notification; after fix and cleanup, Cato follow-up returned `verdict: pass`, `criticality: high`, with only informational findings
- Cross-vendor review waiver, if any: none planned
- Waiver approved by: not applicable
- Waiver reason and scope: not applicable
- Waiver tooling failure/unavailability evidence: not applicable
- Fallback review performed: not needed; required Cato route was available via Vertex Opus 4.8
- Findings fixed: restored `DecryptAsync` touch notification and added `DecryptAsync_WithTouchPolicyAlways_NotifiesBeforePrivateKeyOperation`; removed stale Phase 2 placeholder comments and excessive blank-line clusters in `PivSession.cs`
- Findings deferred: none from final Cato pass; phase-scope improvement candidates remain deferred below
- Human decisions: full mutating/destructive PIV integration suite approved for beta serial `103`

## Deferred Future Improvement Candidates

- Title: PIV reset/auth/default-credential helper cleanup
- Source phase: Phase 4 PIV de-partialization
- Rationale: integration tests and session flows still contain repeated default-state choreography that may be simplified after structural ownership settles
- Why deferred: expanding helper cleanup during de-partialization would blur the phase boundary and risk changing behavior
- Likely owning area: `Piv` tests and possibly module-local test helpers
- Suggested timing: after all module refactors or as a focused PIV test-helper phase
- Needs human approval/hardware/Cato: human approval and likely full PIV integration baseline

- Title: Shared fake smart-card recording helper
- Source phase: Phase 4 PIV de-partialization
- Rationale: PIV APDU regression tests needed a small recording connection; similar byte-level protocol tests may benefit from a shared primitive
- Why deferred: one module is insufficient evidence for `Tests.Shared` promotion
- Likely owning area: `Tests.Shared`
- Suggested timing: after another module independently needs the same helper shape
- Needs human approval/hardware/Cato: human approval for shared test infrastructure; no hardware expected

- Title: Broader PIV cryptographic APDU regression matrix
- Source phase: Phase 4 PIV de-partialization
- Rationale: representative fake APDU tests prove facade/protocol wiring, but crypto/sign/decrypt/key-agreement paths have many algorithm-specific encodings
- Why deferred: full hardware integration already proved behavior for this phase; exhaustive fake coverage would expand scope substantially
- Likely owning area: `Piv` unit tests
- Suggested timing: after module consolidation or before any future PIV crypto protocol rewrite
- Needs human approval/hardware/Cato: human approval; hardware optional if paired with fake vectors

## Abort / Split Assessment

- Wrong branch detected: no
- Phase exceeded approved scope: no, after explicit human approval for complete PIV de-partialization and full PIV integration baseline
- Public API change required: no
- Helper depth concern found: no blocking concern
- Protocol flow became harder to inspect: no
- Verification failed twice for different root causes: no
- Unapproved hardware coordination required: no
- Persistent-state or destructive integration required: yes, but explicitly approved by the human for beta serial `103`
- Core/shared promotion became unavoidable: no
- Abort learning note required: no
- Abort learning note committed with human approval: not applicable
- Outcome: continue to final review, commit, compact summary, and `/Ping`

## Next Phase Inputs

- Required reading before next phase: `docs/SDK-HOUSE-STYLE.md`
- Required reading before next phase: `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`
- Required reading before next phase: `docs/plans/module-consolidation/ISA.md`
- Required reading before next phase: this learning note
- Patterns to apply: treat PIV de-partialization as a module-specific success unless another module proves the same structure improves readability.
- Patterns to apply: capture pre/post integration baselines for remaining module refactors and record any human-approved hardware-state exceptions explicitly.
- Risks to watch: command-like helper pressure during SecurityDomain and FIDO2 phases; avoid copying PIV's shape unless it makes local protocol flow easier to inspect.
- Open questions for human approval: approve the next consolidation phase and decide whether SecurityDomain or FIDO2 should be next.

## Compact Summary

- Goal: replace PIV partial session implementation with one facade and shallow feature protocol helpers
- Files changed: PIV session/source helpers, PIV unit tests, PIV module guidance, consolidation ISA, learning note
- Final pattern: `PivSession` remains the public facade; feature helpers own local APDU/TLV protocol areas
- Rejected approaches: keeping partials, adding command classes, adding service/manager/DI layers, broad helper cleanup
- Tests passed: scoped format verification, PIV build, PIV unit tests 61/61, PIV integration baseline 70/70 before and after
- Integration lifecycle: full PIV integration suite approved and passed on beta serial `103`, firmware `5.8.0`
- Shared/Core candidates: shared fake smart-card recorder deferred pending second-module evidence
- Deferred future improvements: PIV reset/auth/default test helpers, shared recorder, broader PIV crypto fake APDU matrix
- House-style update needed: consider documenting facade-plus-shallow-feature-helper pattern as an alternative to partial classes when readability improves
- Next phase recommendation: run final review, commit Phase 4, send `/Ping`, then request approval for SecurityDomain locality cleanup or FIDO2 CTAP consistency
- Learning note path: `docs/plans/module-consolidation/phase-4-piv-departialization-learnings.md`
- Commit: pending at note update time
- `/Ping` status: queued after commit and compact summary
