# Phase 7: Audit Skill Deferred

## What We Found

- A reusable audit skill is not justified yet.
- The current runtime-resilience workflow has one stable command: `dotnet toolchain.cs -- resilience --fast`.
- Without a diagnostics project, live diagnostic mode, or multi-command audit workflow, a skill would only wrap a single command.

## How We Got Better At Finding

- Earlier phases showed that useful gates should start as concrete tests and toolchain commands.
- Phase 6 prevented an empty diagnostics project; Phase 7 applies the same standard to skill creation.
- The closeout gate now treats “do not build yet” as a valid phase outcome when evidence does not justify more infrastructure.

## What Worked

- The command-line runner is simple enough that a skill would not reduce cognitive load yet.
- Keeping the workflow in `toolchain.cs` makes verification executable and repo-local.
- Deferring the skill avoids duplicating instructions that would drift from the runner.

## What Did Not Work

- Creating a skill before multiple commands or diagnostic modes exist would create maintenance surface without new detection power.
- A report recommendation layer would be speculative because the runner already gives direct pass/fail output.

## Review Findings And Fixes

- DevTeam validated the defer decision and found no concrete skill capability that adds value beyond `dotnet toolchain.cs -- resilience --fast` today.
- DevTeam noted that discoverability is the only current residual value a skill might add, which is a documentation concern rather than orchestration.
- We linked the reopen trigger to Phase 6 reopening because a diagnostics project or live diagnostic would change the skill calculus.

## Remaining Risk

- Users must know the direct command until a skill becomes useful.
- If Phase 6 reopens, live diagnostics are added, or multiple runner modes are added later, the skill decision should be revisited.

## Verification

- Phase 5 runner evidence remains the proof of value: `dotnet toolchain.cs -- resilience --fast` catches the known seeded regressions and runs under 90 seconds.
- No skill files were created in this phase.
