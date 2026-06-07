# Phase 17 ISA: Test Runner And Hardware Coordination

## Problem

Phase 17 needs to close two test-governance gaps before final reassessment: xUnit v3 focused filters must have an evidenced toolchain path, and FIDO2/WebAuthn hardware tests that require User Presence or User Verification must be documented as human-coordinated checks rather than unattended agent gates.

## Vision

Agents can run focused unit and smoke checks through `dotnet toolchain.cs test` without knowing whether the target project uses xUnit v2 or xUnit v3. FIDO2/WebAuthn UP/UV tests are easy to find, clearly marked, and explicitly excluded from agent-runnable gates unless a human is present and has approved the exact run.

## Out Of Scope

- No source-code behavior changes.
- No new FIDO2/WebAuthn integration tests.
- No unattended User Presence, User Verification, PIN, touch, reset, insert/remove, persistent-state, or destructive hardware runs.
- No changes to the `WithYubiKey` discovery model.
- No archived `docs/plans/**` cleanup outside Phase 17 artifacts.

## Constraints

- Branch must be `yubikit-consolidation`.
- Use `dotnet toolchain.cs test`; never raw `dotnet test`.
- Agent-runnable hardware commands must use `--smoke` or an equivalent `Category!=RequiresUserPresence` filter.
- FIDO2/WebAuthn UP/UV checks require explicit human coordination before execution.
- Active docs should use current `TestCategories.Category` trait semantics: `Category=RequiresUserPresence`, not `RequiresUserPresence=true`.

## Goal

Document the Phase 17 coordination policy in active testing docs, capture it in a phase artifact, and verify representative focused xUnit v3 filter behavior through the repository toolchain.

## Criteria

- `ISC-17.1`: Branch check records `## yubikit-consolidation` before Phase 17 edits and verification.
- `ISC-17.2`: `docs/TESTING.md` explains xUnit v3 focused-filter handling, no-match skip behavior, and FIDO2/WebAuthn UP/UV coordination lanes.
- `ISC-17.3`: FIDO2 active test guidance uses `Category!=RequiresUserPresence` and classifies read-only, UP, UV/PIN, reset/destructive, and insert/remove lanes.
- `ISC-17.4`: WebAuthn active guidance makes `Category=RequiresUserPresence` human-coordinated and keeps `--smoke` as the agent default.
- `ISC-17.5`: A Phase 17 coordination artifact records agent-runnable commands, human-coordinated commands, and excluded/destructive commands.
- `ISC-17.6`: Focused xUnit v3 verification runs through `dotnet toolchain.cs test --project Fido2 --filter "Method~ExtensionBuilder"` or a narrower equivalent and exits 0.
- `ISC-17.7`: A representative mixed unit+integration focused-filter verification confirms non-matching projects do not run hardware tests when at least one selected project matches; all-selected xUnit v3 preflight no-match remains a clear toolchain failure.
- `ISC-17.8`: Integration tests that require UP/UV/touch/reset are not run by the agent during this phase.
- `ISC-17.9`: DevTeam cross-vendor review is run on the Phase 17 artifacts and active-doc changes, or a human-approved waiver is recorded.
- `ISC-17.10`: Phase 17 learning note records verification, review route, skipped integration scope, and deferred follow-ups.

## Test Strategy

Planned verification commands:

```bash
git status --short --branch
dotnet toolchain.cs -- test --project Fido2 --filter "Method~ExtensionBuilder"
dotnet toolchain.cs -- test --integration --project Fido2 --filter "Method~ExtensionBuilder"
dotnet format --verify-no-changes --include "toolchain.cs"
git diff --check
```

Integration tests are skipped for Phase 17 because the phase is documentation/tooling coordination only. FIDO2/WebAuthn UP/UV checks are specifically not run by the agent.

## Promotion Candidates

- `Tests.Shared`: no code promotion in scope.
- `Core`: no code promotion in scope.
- `Cli.Shared`: no code promotion in scope.
- Active docs: update `docs/TESTING.md`, `src/Fido2/CLAUDE.md`, `src/Fido2/tests/CLAUDE.md`, and `src/WebAuthn/CLAUDE.md`.
