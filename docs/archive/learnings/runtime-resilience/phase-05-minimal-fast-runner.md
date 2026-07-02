# Phase 5: Minimal Fast Runner

## What We Found

- Phases 1-4 produced enough useful no-hardware resilience gates to justify one local runner.
- The existing test infrastructure already had the needed primitive: `Category=RuntimeResilience`.
- A separate diagnostics project would still be premature.

## How We Found It

- We verified the individual gates first: SmartCard listener backoff/recovery, OTP polling, static scanner, and SmartCard context release.
- We then added one toolchain target that composes those gates instead of adding new test infrastructure.
- Red-green checks proved the runner fails on both an OTP sleep-first regression and a SmartCard context leak regression.

## What Got Better

- Added `dotnet toolchain.cs -- resilience --fast` as the single fast local command.
- The runner executes Core unit tests tagged `Category=RuntimeResilience`.
- The output remains pass/fail with concise evidence and elapsed time, not a dashboard.
- The runner requires `--fast` because no slower or live-hardware mode exists yet.

## What Worked

- Reusing xUnit traits and `toolchain.cs` avoided a new diagnostics project.
- The runner stayed well under the 90-second target, passing 13 runtime-resilience tests in 3.2s after final hardening.
- A single command caught both seeded regression classes tested in this phase.

## What Did Not Work

- Treating `--fast` as advisory was misleading. We changed it to a required flag.
- Mutating the shared `testFilter` without restoration was a future footgun. We restored it in a `finally` block after the runner completes.
- BenchmarkDotNet remains too expensive for the default fast path.

## Review Findings And Fixes

- DevTeam returned `pass` with non-blocking findings.
- We fixed the shared `testFilter` mutation risk by restoring the previous filter in `finally`.
- We fixed the `--fast` no-op risk by making the target fail immediately without `--fast`.

## Remaining Risk

- The runner currently hardcodes Core because all runtime-resilience gates are Core tests.
- `--project` and `--integration` are intentionally ignored by this fixed gate.
- Live-hardware diagnostics remain deferred.

## Verification

- `dotnet toolchain.cs -- resilience --fast`: 13 runtime-resilience tests passed, 0 failed, elapsed 3.2s after final hardening.
- `dotnet toolchain.cs -- resilience`: failed immediately with guidance because `--fast` is required.
- Temporary OTP regression: runner failed with scanner and timing failures.
- Temporary SmartCard leak regression: runner failed with the context-release invariant.
- Core gate: 523 total, 521 succeeded, 2 expected hardware skips, 0 failed before final docs update.
- Format verification: clean except existing `Tests.TestProject` IL2026/IL3050 warnings before final docs update.
