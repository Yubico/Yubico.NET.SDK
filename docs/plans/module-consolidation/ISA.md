# ISA: Sequential Module Consolidation

This ISA defines the execution plan for working through `docs/MODULE-CONSOLIDATION-ASSESSMENT.md` in a step-by-step, reviewable way.

Read this together with:

- `CLAUDE.md`
- `docs/SDK-HOUSE-STYLE.md`
- `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`
- the relevant module `CLAUDE.md`
- the previous phase learning note, when present

## Problem

The SDK needs module consolidation without recreating the v1 problem of over-abstracted command classes, hidden protocol flow, and inconsistent helper depth.

Parallel module-wide refactors are risky because they lose learning between modules, create inconsistent local abstractions, and miss opportunities to promote patterns into `Core`, `Tests.Shared`, or `Cli.Shared` at the right time.

## Vision

Each module improves through a small, reviewable phase. One phase teaches the next phase. Learning is captured on disk, committed with the implementation, and used as required input before the next module begins.

The end state is an SDK that feels like one senior developer executed one architectural plan: flat protocol flow, minimal helper depth, explicit security cleanup, shared Core primitives, and tests that prove behavior.

## Out Of Scope

- No parallel all-module refactor pass.
- No operation-named command types for applet operations. This includes, but is not limited to, `AuthenticateCommand`, `PutKeyCommand`, `GetDataCommand`, `VerifyPinCommand`, `GenerateKeyCommand`, `CalculateCommand`, `SignCommand`, or `DeriveKeyCommand`.
- No operation-named `*Command` type that performs protocol validation, protocol encoding, APDU/CTAP construction, transmission, parsing, or state mutation, even if it has no `ExecuteAsync` method.
- `ApduCommand` and plain DTOs are allowed only when they are generic data carriers, not operation-specific behavior holders.
- No broad helper layer that hides APDU/CTAP construction.
- No source-code changes without phase-specific human approval.
- No integration tests requiring User Presence, UV, physical touch, insert/remove, persistent state, or destructive behavior.

## Goal

Run consolidation on the dedicated `yubikit-consolidation` branch as a sequence of phase ISAs, one module or concern at a time, with the primary agent as orchestrator, `/DevTeam` handling implementation/review loops, cross-vendor review where available, focused verification, scoped read-only integration testing on the YubiKey 5.8 beta key, learning capture, a commit after each successful phase, and `/Ping` after each successful phase.

## Criteria

- `ISC-1`: Every phase has a human-approved phase ISA before source-code changes begin.
- `ISC-1.1`: Every phase verifies it is executing on branch `yubikit-consolidation` before source-code changes, build/test commands, or agent delegation begin.
- `ISC-2`: No phase introduces operation-named command classes, operation-named `*Command` types, command-like types with execution methods, or command objects with protocol logic. Forbidden behavior includes protocol validation, protocol encoding, APDU/CTAP construction, transmission, parsing, state mutation, or method names such as `Execute`, `ExecuteAsync`, `Run`, `RunAsync`, `Transmit`, and `TransmitAsync` on command-like abstractions.
- `ISC-3`: Protocol flow remains visible in session methods or narrowly scoped backend methods.
- `ISC-4`: Helpers are shallow, pure where possible, and justified by readability, testability, or security.
- `ISC-5`: Sensitive source, intermediate, encoded APDU/CTAP/config payload, and response-derived sensitive buffers are zeroed where the phase touches sensitive data.
- `ISC-6`: Focused build, unit-test, and integration-test commands are recorded with command text, exit result, and relevant filter/project arguments.
- `ISC-7`: Beta-key integration phases record Management preflight evidence before applet tests run, including applet-reported firmware when observable.
- `ISC-8`: Each phase ISA pre-declares exact agent-runnable integration test IDs, filters, or approved skip rationale. User Presence, UV, touch, insert/remove, slow, persistent-state, or destructive tests are not run by agents.
- `ISC-9`: Cross-vendor review is completed through the resolved opposite-family route, or a human-approved waiver is recorded with reason, scope, and tooling failure evidence.
- `ISC-10`: Core, `Tests.Shared`, and `Cli.Shared` promotion candidates are listed in the phase ISA up front, then explicitly accepted, rejected, or deferred with rationale at phase close.
- `ISC-11`: Every completed or aborted phase produces and commits a learning note before the next phase begins.
- `ISC-12`: Every phase produces a compact summary before the next phase begins.
- `ISC-13`: Every successful phase sends `/Ping` after the phase commit and compact summary are complete.
- `Anti-1`: A phase proceeds to the next phase without a learning note and commit.
- `Anti-1.1`: Refactor implementation, verification, or delegation occurs on any branch other than `yubikit-consolidation`.
- `Anti-2`: A refactor makes protocol flow harder to inspect in pursuit of DRY.
- `Anti-3`: A phase claims integration coverage without recorded Management firmware preflight when beta-key applet behavior is involved.

## Core Constraints

- Follow `docs/SDK-HOUSE-STYLE.md`.
- Execute refactor work only on branch `yubikit-consolidation`, based from `yubikit-applets` at commit `bfc6bdd5`.
- If the worktree is not on `yubikit-consolidation`, stop before any source-code change, build/test command, or agent delegation.
- Keep protocol flow flat and visible.
- Use plain `ApduCommand` or equivalent DTOs.
- Extract only small pure encode/parse helpers.
- Prefer module-local helpers until reuse is proven.
- Promote to `Core`, `Tests.Shared`, or `Cli.Shared` only after review confirms cross-module value.
- Preserve public API unless explicitly approved.
- Keep every phase small enough to review thoroughly.

## Standard Phase Lifecycle

```text
Plan -> branch check -> approve -> implement -> review -> verify -> integration -> learning note -> commit -> compact -> /Ping -> next phase
```

### 0. Branch Check

All consolidation refactors execute on `yubikit-consolidation`.

Before implementation, review, verification, integration, or agent delegation for any phase, run:

```bash
git status --short --branch
```

The command must show `## yubikit-consolidation`. If it does not, stop and ask for a branch decision before continuing.

The learning note records:

- branch name
- base branch
- base commit, when known
- whether unrelated worktree changes were present
- confirmation that no refactor work ran off-branch

### 1. Phase ISA

Before code changes, create or present a phase-specific ISA that defines:

- scope
- files likely touched
- house-style constraints
- ideal state criteria
- test strategy
- integration-test scope
- human decisions needed
- whether `Core`, `Tests.Shared`, or `Cli.Shared` promotion is in scope

### 2. Human Approval

The human must approve the phase scope before implementation starts.

If the phase might promote code into `Core`, `Tests.Shared`, or `Cli.Shared`, that promotion must be explicitly approved or deferred.

### 3. DevTeam Implementation

Implementation should use `/DevTeam` as the execution loop when available.

The orchestrator provides the phase ISA, house style, module docs, and previous phase learning to the implementing agents.

The implementation must remain minimal and reviewable.

### 4. Review

Each phase requires review focused on:

- flat protocol flow
- helper depth
- security and memory ownership
- API compatibility
- test value
- cross-module reuse opportunities
- AI slop risk

Use cross-vendor review where available, including `gpt-5.5` and `opus 4.8` roles/settings when the tooling supports it.

If cross-vendor review cannot run, the waiver must be approved by the human and recorded in the phase learning note with:

- waived review type
- reason
- approver
- scope of waiver
- exact tooling failure or unavailability evidence
- fallback review performed instead, if any
- whether the waiver applies only to this phase or to a repeated tooling outage

A single-vendor phase is not considered fully reviewed unless the human explicitly approves the waiver after seeing the failed route/tooling evidence.

If two consecutive phases require cross-vendor review waivers for the same tooling reason, stop before the next implementation phase and ask for a program-level decision: fix routing, accept a temporary single-vendor program mode, or pause consolidation.

### 5. Verification

Each phase runs focused verification only:

- branch check before verification
- focused build for affected project(s)
- focused unit tests
- scoped read-only integration tests when feasible
- no broad full-suite runs unless phase scope requires it

Use repository toolchain commands. Never use raw `dotnet build` or raw `dotnet test`.

Each phase ISA must name the exact focused command shapes before implementation starts. Record command text, project/filter arguments, exit result, and any output path in the phase learning note.

Default command shapes:

```bash
dotnet toolchain.cs build --project <Module>
dotnet toolchain.cs test --project <Module> --filter "<focused filter>"
dotnet toolchain.cs -- test --integration --project <Module> --smoke --filter "<focused filter>"
```

Use narrower commands when possible. If a command shape does not apply, the phase learning note must say why.

If toolchain arguments behave unexpectedly, run:

```bash
dotnet toolchain.cs -- --help
```

Then update the phase ISA with the corrected command shape before continuing.

### 6. Integration Lifecycle

Each phase that touches applet behavior should include a scoped integration lifecycle against the YubiKey 5.8 beta key when feasible.

Because non-Management applets on the beta key may report firmware as `0.0.0` or `0.0.1`, integration lifecycle starts with a Management preflight.

```text
Management session -> GetDeviceInfoAsync -> confirm target serial -> confirm firmware 5.8.x
```

Do not trust applet-reported firmware for the beta key unless the phase explicitly tests applet firmware reporting.

Preflight evidence must be recorded before applet tests run:

- command or test/helper used for Management preflight
- beta key serial
- Management-reported firmware
- applet-reported firmware, if observed
- timestamp or phase verification note

If a Management preflight cannot run because the test context cannot open the required Management-capable transport, the phase ISA must record an exception path before applet tests run:

- why Management preflight cannot run
- alternate identity proof, such as allow-list serial plus device selection evidence
- whether the phase is allowed to proceed without firmware confirmation
- human approval for the exception

For FIDO2 and WebAuthn phases, where many meaningful flows require User Presence or UV, the phase ISA must classify integration checks as one of:

- agent-runnable smoke check
- human-coordinated hardware check
- explicitly skipped with human-approved rationale

If a human-coordinated FIDO2/WebAuthn check is required, the phase ISA must say whether it is scheduled now, deferred, or replaced by unit/fake-backend verification for this phase.

### 7. Learning Note

Every phase ends with a learning note before moving on.

Learning notes live at:

```text
docs/plans/module-consolidation/phase-N-<slug>-learnings.md
```

No learning note, no next phase.

### 8. Commit

A successful phase ends with a commit containing:

- implementation changes
- tests
- updated phase learning document

Do not commit unrelated files. Stage files explicitly.

Commit message should be concise and phase-specific, for example:

```text
Consolidate YubiHsm sensitive payload cleanup
```

### 9. Context Compaction

After each committed phase, produce a compact continuation payload:

```markdown
## Phase N Compact Summary
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
```

### 10. Phase Ping

After each successful phase, send `/Ping` only after all of these are complete:

- implementation and tests are done
- required review is complete or waived with approval
- learning note is written
- commit is created
- compact summary is produced

Do not send `/Ping` for draft plans, aborted phases, failed verification, or partially complete work.

## Beta Key Integration Rule

Each integration-capable phase records the hardware target:

```markdown
## Hardware Target
- Device: YubiKey 5.8 beta
- Serial: 103
- Firmware source of truth: Management `GetDeviceInfoAsync`
- Applet firmware caveat: applets may report 0.0.0 / 0.0.1
- Allowed agent-run tests: read-only checks only; no User Presence, no UV, no touch, no insert/remove, no persistent state, no destructive tests
```

The beta key serial is 103. The `appsettings.json` allow-list convention still needs confirmation before integration preflight commands run.

## Agent-Runnable Integration Tests

Agents may run integration tests only when all are true:

- Scoped to the YubiKey 5.8 beta key.
- Scoped to serial 103 when serial filtering is available.
- Read-only.
- No User Presence.
- No UV.
- No physical touch.
- No insert/remove coordination.
- No persistent state.
- No destructive test.
- Uses `dotnet toolchain.cs test`, never `dotnet test`.

Default command shape:

```bash
dotnet toolchain.cs -- test --integration --project <Module> --smoke --filter "<focused filter>"
```

`--smoke` is the default because it skips `Slow` and `RequiresUserPresence`.

`--smoke` is not sufficient by itself. Each phase ISA must also list the exact test IDs or filters the agent intends to run, and must classify whether any selected test mutates persistent device state. Agent-run integration filters are an allowlist, not an open-ended module run.

For this program, persistent state means any change that can remain after the test process exits, including applet reset, slot programming, key/certificate/object creation or deletion, PIN/PUK/password/access-code changes, management-key changes, credential enrollment, authenticator configuration, allowlist/certificate storage, or Security Domain key changes. Tests with persistent state are not agent-runnable during this consolidation program.

For FIDO2 and WebAuthn:

- `GetInfo`-style tests are usually agent-runnable.
- MakeCredential, GetAssertion, reset, PIN, UV, biometric, and user-presence flows are not agent-runnable unless a human explicitly coordinates.
- FIDO/WebAuthn phases should primarily use unit tests plus non-UP/UV integration smoke checks.

## Phase Gate

A phase is not complete until:

- [ ] Branch check shows `yubikit-consolidation` before implementation/delegation.
- [ ] Phase ISA approved by human.
- [ ] Phase ISA maps work to `ISC-*` criteria.
- [ ] Phase ISA lists candidate `Core`, `Tests.Shared`, or `Cli.Shared` promotions, or states none expected.
- [ ] Phase ISA lists exact agent-runnable integration test IDs/filters, or approved skip rationale.
- [ ] Beta key serial/scope confirmed if integration tests apply; serial is 103 unless superseded by human decision.
- [ ] Management preflight evidence is captured if integration tests apply, including applet firmware when observable.
- [ ] DevTeam implementation complete.
- [ ] DevTeam review complete.
- [ ] Cross-vendor review complete or human-approved waiver recorded.
- [ ] Focused build passes.
- [ ] Focused unit tests pass.
- [ ] Scoped integration tests pass, or skip rationale approved.
- [ ] Verification command text, filters, and results are recorded.
- [ ] Promotion candidates are accepted, rejected, or deferred with rationale.
- [ ] Learning document updated.
- [ ] Compact summary produced.
- [ ] Commit created.
- [ ] `/Ping` sent after successful phase completion.
- [ ] Next phase inputs updated from learning document.

For shared-infrastructure phases such as `Tests.Shared` or `Cli.Shared`, the phase ISA must include a cross-module verification plan before implementation starts. That plan can be narrow, but it must name affected modules and explain why broader integration is or is not required.

If a promoted shared abstraction later proves wrong, the next phase ISA must explicitly decide whether to keep, revise, or demote it back to module-local code. Promotion is reversible; do not preserve a shared helper only because a previous phase promoted it.

## Abort / Split Criteria

A phase must stop and return for human decision when any of these occur:

- The implementation requires broader source changes than the approved phase ISA allowed.
- The worktree is not on `yubikit-consolidation` before implementation, verification, integration, or delegation.
- The implementation needs public API changes not already approved.
- The phase creates pressure to introduce operation-specific command classes or deep helper layers.
- Review finds the protocol flow became harder to inspect.
- Focused verification fails twice for different root causes.
- Integration testing needs User Presence, UV, touch, insert/remove, slow, or destructive behavior not previously approved.
- Integration testing needs persistent-state or destructive behavior.
- A Core, `Tests.Shared`, or `Cli.Shared` promotion decision becomes unavoidable but was not approved.

The allowed outcomes are:

- split the phase into a smaller phase ISA
- revise the phase ISA and request approval
- defer the risky part with a written rationale
- abandon the phase and restore the previous implementation state

If any implementation, review, or verification work occurred before the abort, write an abort learning note. The note should capture what failed, what was reverted or left unchanged, and what the next phase should avoid. Commit the abort learning note only when the human approves preserving that artifact.

## Generalization Check

Before a phase learning note can influence the next phase, classify each learned pattern as one of:

- module-specific
- candidate for one more module trial
- candidate for shared promotion
- rejected outside this phase

Phase 1 is especially constrained by this rule because it starts with `YubiHsm` and `Management`, not the strongest model modules. Do not treat Phase 1 patterns as general SDK style until they survive at least one additional module or explicit human approval.

## DevTeam Brief Template

```markdown
Implement this phase only.

First run `git status --short --branch` and confirm the branch is `yubikit-consolidation`. If it is not, stop.

Read:
- CLAUDE.md
- docs/SDK-HOUSE-STYLE.md
- docs/MODULE-CONSOLIDATION-ASSESSMENT.md
- docs/plans/module-consolidation/ISA.md
- relevant module CLAUDE.md
- previous phase learning note, if present

Constraints:
- Execute only on branch `yubikit-consolidation`.
- Keep protocol flow flat.
- Do not add operation-specific command classes.
- Do not add command classes with ExecuteAsync.
- Minimize helper depth.
- Prefer small, reviewable changes.
- Preserve public API unless explicitly approved.
- Add behavior tests only.
- Zero sensitive buffers, including encoded command payloads.
- Do not run persistent-state or destructive integration tests.
- If integration is run, use only read-only checks scoped to beta serial 103 where possible.

Return:
- Files changed
- Branch check result
- Why each change is necessary
- Tests run
- Integration lifecycle result or approved skip rationale
- Whether `/Ping` should be sent now
- Risks
- Whether anything should move to Core, Tests.Shared, or Cli.Shared
```

## Phase Order

### Phase 0: Governance

Status: in progress / mostly complete.

Artifacts:

- `docs/SDK-HOUSE-STYLE.md`
- `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`
- `CLAUDE.md` reference to house style
- this ISA
- `docs/plans/module-consolidation/LEARNING-TEMPLATE.md`

### Phase 1: Sensitive Payload Lifecycle

Modules: `YubiHsm`, then `Management`.

Why first: high security value, low architecture churn.

Goal: encoded sensitive APDU/config payloads are zeroed after transmit.

Constraints:

- No new command classes.
- No hidden transmit abstraction unless command remains visible.
- Tests should prove behavior where practical, not just guard clauses.

### Phase 2: Tests.Shared Harness Consolidation

Targets:

- duplicated `SharedSmartCardConnection`
- shared module session helper shape
- possible PIV helper

Why second: safer integration tests make later phases easier.

### Phase 3: OATH Chained Response Investigation

Targets:

- confirm or fix OATH `INS_SEND_REMAINING` behavior
- add focused fake protocol/APDU tests

No broad OATH rewrite.

### Phase 4: PIV Flat Flow Cleanup

Targets:

- high-risk inline APDU/TLV paths
- fake APDU tests
- reset/auth test helper opportunities

Preserve partial-session readability.

### Phase 5: SecurityDomain Locality Cleanup

Targets:

- feature partials or pure parse/encode helpers only where locality improves
- test certificate helper consolidation

Do not introduce `PutKeyCommand`, `GetDataCommand`, or equivalent classes.

### Phase 6: FIDO2 CTAP Request Consistency

Targets:

- one visible CTAP request-building convention
- duplicate CBOR copy/parser helpers
- sensitive auth-param zeroing contracts

### Phase 7: WebAuthn API Coherence

Targets:

- public client factory/session creation story
- stale docs
- ceremony readability without hiding Fido2 delegation

### Phase 8: CLI Consolidation

Targets:

- implement/remove unused global options
- centralize parsing/prompting/session/token helpers
- secure credential handling

### Phase 9: Documentation Repair Pass

Targets:

- stale module READMEs
- stale module `CLAUDE.md` files
- examples that reference old/nonexistent API shapes
- accumulated phase learning notes that no longer match final source reality

Only run after source patterns settle.

## Next Phase Input Rule

Every new phase starts by reading:

- `docs/SDK-HOUSE-STYLE.md`
- `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`
- `docs/plans/module-consolidation/ISA.md`
- previous phase learning note
- relevant module `CLAUDE.md`

## Quality Strategy

Quality comes from sequence and evidence, not from agent volume.

Use:

- dedicated branch `yubikit-consolidation`
- one phase at a time
- narrow scope
- source-backed ISA
- house-style checklist
- DevTeam implementation/review loop
- cross-vendor review where available
- focused verification
- scoped read-only beta-key integration
- learning note before next phase
- commit after success
- `/Ping` after each successful phase

## Final Constraint

No learning note, no commit. No commit, no next phase.
