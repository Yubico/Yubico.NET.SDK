# Runtime Resilience Learnings

This folder records phase closeouts for the runtime resilience workstream. Each phase captures what we found, how we got better at finding it, what worked, what did not, what review changed, and what remains risky.

## Phases

- [Phase 1: SmartCard Listener Recovery](phase-01-smartcard-listener.md)
- [Phase 2: OTP HID Ready-To-Write Polling](phase-02-otp-polling.md)
- [Phase 3: Static Runtime-Resilience Scanner](phase-03-static-scanner.md)
- [Phase 4: SmartCard Context Leak Invariant](phase-04-smartcard-context-leak.md)
- [Phase 5: Minimal Fast Runner](phase-05-minimal-fast-runner.md)
- [Phase 6: Diagnostics Project Deferred](phase-06-diagnostics-project-deferred.md)
- [Phase 7: Audit Skill Deferred](phase-07-audit-skill-deferred.md)

## Closeout Rule

Each future phase should close in this order:

1. Implement the smallest useful slice.
2. Verify with focused and broader project gates.
3. Run cross-vendor review when the change affects runtime behavior or proof quality.
4. Write the phase learning document.
5. Commit the phase before starting the next one.

## What The Next Work Should Do

- Use `dotnet toolchain.cs -- resilience --fast` as the default runtime-resilience verification command.
- Start from a concrete bug seed or missing invariant, not from a desire to add infrastructure.
- Prefer no-hardware proof first: fake seams, deterministic timing/resource-release invariants, or seeded scanner fixtures.
- Use BenchmarkDotNet for discovery and evidence, then convert proven foreground regressions into cheap gates.
- Keep the diagnostics project and audit skill deferred until the workflow grows beyond the current single-command runner.
