# Runtime Resilience Learnings

This folder records phase closeouts for the runtime resilience workstream. Each phase captures what we found, how we got better at finding it, what worked, what did not, what review changed, and what remains risky.

## Phases

- [Phase 1: SmartCard Listener Recovery](phase-01-smartcard-listener.md)
- [Phase 2: OTP HID Ready-To-Write Polling](phase-02-otp-polling.md)
- [Phase 3: Static Runtime-Resilience Scanner](phase-03-static-scanner.md)
- [Phase 4: SmartCard Context Leak Invariant](phase-04-smartcard-context-leak.md)

## Closeout Rule

Each future phase should close in this order:

1. Implement the smallest useful slice.
2. Verify with focused and broader project gates.
3. Run cross-vendor review when the change affects runtime behavior or proof quality.
4. Write the phase learning document.
5. Commit the phase before starting the next one.
