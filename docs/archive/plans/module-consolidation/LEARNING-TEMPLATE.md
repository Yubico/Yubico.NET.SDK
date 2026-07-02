# Phase N Learnings: <Phase Name>

Use this template at the end of every module-consolidation phase.

Learning notes are required phase artifacts. A phase is not complete until its learning note is written and committed with the implementation.

Expected path:

```text
docs/plans/module-consolidation/phase-N-<slug>-learnings.md
```

## Phase Summary

- Branch:
- Base branch:
- Base commit:
- Branch check command/result:
- Unrelated worktree changes present: yes/no
- Refactor work ran only on `yubikit-consolidation`: yes/no
- Scope:
- Criteria satisfied:
- Criteria deferred:
- Promotion candidates declared up front:
- Files changed:
- Tests run:
- Integration tests run:
- Result:
- Commit:
- `/Ping` sent after successful phase: yes/no

## Hardware Target

- Device: YubiKey 5.8 beta
- Serial: 103
- Firmware source of truth: Management `GetDeviceInfoAsync`
- Management firmware observed:
- Applet firmware observed, if observable:
- Applet firmware caveat observed: yes/no

## Integration Lifecycle

- Management preflight command/result:
- Management preflight evidence captured before applet tests: yes/no
- Management preflight exception path used: yes/no
- Alternate identity proof, if preflight skipped:
- Agent-runnable integration test allowlist:
- Integration scope was read-only: yes/no
- Tests run:
- Tests skipped:
- Skip reason:
- Skip approved by:
- Selected tests mutate persistent state: yes/no
- User Presence / UV required:
- Human-coordinated hardware needed:
- Human-coordinated hardware scheduled/deferred/replaced:
- Persistent state changed:
- Destructive tests skipped completely: yes/no
- Reset/cleanup performed:
- Result:

## What Worked

- Patterns that improved readability:
- Patterns that improved testability:
- Patterns that improved security/memory hygiene:

## What Did Not Work

- Rejected approaches:
- Helpers or abstractions that were too deep:
- Changes that looked DRY but harmed readability:

## House Style Updates

- Existing house-style rule confirmed:
- Rule that needs clarification:
- Possible addition to `docs/SDK-HOUSE-STYLE.md`:

## Reusable Patterns

- Pattern:
- Generalization class: module-specific / candidate for one more module trial / candidate for shared promotion / rejected outside this phase
- Where it applies:
- Where it should not apply:
- Example files:

## Core / Shared Promotion Candidates

- Candidate:
- Declared in phase ISA up front: yes/no
- Should move to: `Core` / `Tests.Shared` / `Cli.Shared` / stay module-local
- Evidence:
- Risk:
- Decision: accepted / rejected / deferred
- Decision rationale:
- Revisit trigger:
- Demotion/reversal needed for previous shared helper: yes/no
- Demotion/reversal rationale:

## Cross-Module Implications

- Modules likely affected:
- Next module should copy:
- Next module should avoid:
- Potential API compatibility concern:

## Verification Evidence

- Branch check commands:
- Branch check exit result:
- Build commands:
- Build exit result:
- Unit test commands:
- Unit test exit result:
- Integration test commands:
- Integration test exit result:
- Command filters/projects:
- Cross-module verification plan, if shared infrastructure changed:
- Results:
- Manual review notes:
- Reviewer concerns resolved:

## Review Summary

- DevTeam engineer result:
- DevTeam reviewer result:
- Cross-vendor review result:
- Cross-vendor review waiver, if any:
- Waiver approved by:
- Waiver reason and scope:
- Waiver tooling failure/unavailability evidence:
- Fallback review performed:
- Findings fixed:
- Findings deferred:
- Human decisions:

## Abort / Split Assessment

- Wrong branch detected: yes/no
- Phase exceeded approved scope: yes/no
- Public API change required: yes/no
- Helper depth concern found: yes/no
- Protocol flow became harder to inspect: yes/no
- Verification failed twice for different root causes: yes/no
- Unapproved hardware coordination required: yes/no
- Persistent-state or destructive integration required: yes/no
- Core/shared promotion became unavoidable: yes/no
- Abort learning note required: yes/no
- Abort learning note committed with human approval: yes/no/not applicable
- Outcome: continue / split / revise ISA / defer / abandon

## Next Phase Inputs

- Required reading before next phase:
- Patterns to apply:
- Risks to watch:
- Open questions for human approval:

## Compact Summary

- Goal:
- Files changed:
- Final pattern:
- Rejected approaches:
- Tests passed:
- Integration lifecycle:
- Shared/Core candidates:
- House-style update needed:
- Next phase recommendation:
- Learning note path:
- Commit:
- `/Ping` status:
