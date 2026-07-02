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
- No unattended FIDO/FIDO2/WebAuthn integration tests requiring User Presence, UV, physical touch, or insert/remove coordination. User Presence tests fail unattended after roughly 30 seconds and must be deferred or explicitly human-coordinated.
- No public module API changes without explicit approval. Core API changes are allowed when they improve ownership, security, explicitness, or architectural clarity.

## Goal

Run consolidation on the dedicated `yubikit-consolidation` branch as a sequence of phase ISAs, one module or concern at a time, with the primary agent as orchestrator, `/DevTeam` handling implementation/review loops, cross-vendor review where available, focused verification, all self-runnable integration testing on the YubiKey 5.8 beta key that does not require UP/UV/touch/insert-remove help, learning capture, a commit after each successful phase, and `/Ping` after each successful phase.

## Criteria

- `ISC-1`: Every phase has a phase ISA before source-code changes begin. Phase 25 through Phase 32 are covered by the approved autonomous program scope unless the phase ISA exceeds that scope or hits a stop gate.
- `ISC-1.1`: Every phase verifies it is executing on branch `yubikit-consolidation` before source-code changes, build/test commands, or agent delegation begin.
- `ISC-2`: No phase introduces operation-named command classes, operation-named `*Command` types, command-like types with execution methods, or command objects with protocol logic. Forbidden behavior includes protocol validation, protocol encoding, APDU/CTAP construction, transmission, parsing, state mutation, or method names such as `Execute`, `ExecuteAsync`, `Run`, `RunAsync`, `Transmit`, and `TransmitAsync` on command-like abstractions.
- `ISC-3`: Protocol flow remains visible in session methods or narrowly scoped backend methods.
- `ISC-4`: Helpers are shallow, pure where possible, and justified by readability, testability, or security.
- `ISC-5`: Sensitive source, intermediate, encoded APDU/CTAP/config payload, and response-derived sensitive buffers are zeroed where the phase touches sensitive data.
- `ISC-6`: Focused build, unit-test, and integration-test commands are recorded with command text, exit result, and relevant filter/project arguments.
- `ISC-7`: Beta-key integration phases record Management preflight evidence before applet tests run, including applet-reported firmware when observable.
- `ISC-8`: Each phase ISA pre-declares exact self-runnable integration test IDs, filters, full-module integration commands, or skip rationale. Self-runnable integration tests are allowed even when they reset or mutate applet state, provided they do not require UP/UV/touch/insert-remove help and their cleanup/reset behavior is either self-contained or documented. FIDO/FIDO2/WebAuthn User Presence, UV, touch, and insert/remove tests are deferred or human-coordinated because unattended User Presence waits fail after roughly 30 seconds.
- `ISC-9`: Cross-vendor review is completed through the resolved opposite-family route, or a human-approved waiver is recorded with reason, scope, and tooling failure evidence.
- `ISC-10`: Core, `Tests.Shared`, and `Cli.Shared` promotion candidates are listed in the phase ISA up front, then explicitly accepted, rejected, or deferred with rationale at phase close.
- `ISC-11`: Every completed or aborted phase produces and commits a learning note before the next phase begins.
- `ISC-12`: Every phase produces a compact summary before the next phase begins.
- `ISC-13`: Every successful phase sends `/Ping` after the phase commit and compact summary are complete.
- `ISC-14`: Every phase records deferred future improvement candidates separately from phase-scope work, so possible end-of-program refinements are preserved without interrupting sequential module consolidation.
- `ISC-15`: Remaining module refactor phases capture pre-refactor and post-refactor integration-test baselines using the phase-recorded integration scope, excluding unattended FIDO/FIDO2/WebAuthn tests that require User Presence, UV, physical touch, insert/remove coordination, or human-only hardware interaction.
- `ISC-16`: Core APIs may change when the change makes ownership, memory cleanup, protocol flow, security, or design clarity better. Such changes must be explicit in the phase ISA and covered by affected-module verification.
- `ISC-17`: Public module APIs remain stable by default and may change only with an explicit phase decision or later human approval.
- `ISC-18`: Meaningful tests may be added when they prove protocol bytes, security cleanup, state transitions, integration behavior, or regression-prone behavior. Validation-only tests that only prove framework guard clauses remain out of scope.
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
- Core APIs may change when they improve ownership, security, explicitness, or architectural clarity; public module APIs remain stable unless explicitly approved.
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

### 2. Scope Approval

The phase scope must be approved before implementation starts. Phase 25 through Phase 32 are covered by the approved autonomous program scope when the phase ISA stays within this master ISA, the original assessment, prior learning notes, and the phase's listed targets.

If the phase might promote code into `Core`, `Tests.Shared`, or `Cli.Shared`, that promotion must be explicitly accepted, rejected, or deferred in the phase ISA. Core API changes are allowed when they improve ownership, security, explicitness, or architectural clarity; public module API changes still require explicit approval.

### 3. DevTeam Implementation

Implementation should use `/DevTeam` as the execution loop when available.

The orchestrator provides the phase ISA, house style, module docs, and previous phase learning to the implementing agents.

The implementation must remain minimal and reviewable.

### 4. Review

Each phase requires review focused on:

- flat protocol flow
- helper depth
- security and memory ownership
- API compatibility, including whether a Core API change would make ownership or security clearer and whether any public module API shape changed
- test value
- cross-module reuse opportunities
- AI slop risk
- architectural elegance: whether a simpler, more zen-like refactor exists that preserves behavior with less machinery, fewer names, flatter flow, or clearer ownership

Use cross-vendor review where available, including `gpt-5.5` and `opus 4.8` roles/settings when the tooling supports it.

Cato audits must evaluate more than correctness. They must also ask whether the completed refactor is the most elegant shape available within the approved scope: could the same ideal state be reached with fewer abstractions, less cleverness, clearer protocol flow, or a more natural Core/module boundary? If Cato sees a materially simpler or more graceful design, record it as a finding even when the current implementation is correct.

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

### 4.1 Deferred Future Improvements

Each phase may discover improvements that are real but not appropriate for the approved phase scope. These are not failures and should not cause local scope creep.

Record deferred improvement candidates when review, verification, Cato, DevTeam, or implementation reveals:

- a more elegant Core/module boundary that would require a broader phase
- a possible shared abstraction that needs more module evidence
- a latent protocol or SCP concern outside the current applet scope
- a naming/API polish opportunity that is not worth churn during the phase
- a useful hardware or integration test that requires human-coordinated state

Deferred candidates must be captured in the phase learning note with:

- title
- source phase
- rationale
- why it is deferred
- likely owning area (`Core`, `Tests.Shared`, `Cli.Shared`, module, docs, or tooling)
- suggested timing, usually "after all module refactors" unless urgent
- whether it needs human approval, hardware coordination, or Cato review

If a deferred candidate is substantial enough to guide future work, save a follow-up plan under:

```text
docs/plans/module-consolidation/follow-up-<slug>.md
```

These candidates are worked through after all planned module refactor phases complete, unless the human explicitly promotes one into an earlier approved phase.

### 5. Verification

Each phase runs focused verification only:

- branch check before verification
- focused build for affected project(s)
- focused unit tests
- pre-refactor and post-refactor integration baselines for remaining module phases using the approved phase scope
- scoped or full-module self-runnable integration tests when feasible, excluding only tests that require UP/UV/touch/insert-remove help or other human coordination
- no broad full-suite runs unless phase scope requires it

Use repository toolchain commands. Never use raw `dotnet build` or raw `dotnet test`.

Each phase ISA must name the exact focused command shapes before implementation starts. Record command text, project/filter arguments, exit result, and any output path in the phase learning note.

For remaining module refactor phases, integration baseline comparison is required unless the module has no integration project or the available tests require unattended FIDO/FIDO2/WebAuthn User Presence, UV, touch, insert/remove coordination, or human-only hardware interaction. Self-runnable integration tests may run even when they reset or mutate applet state, provided they do not require human coordination and their cleanup/reset behavior is self-contained or documented. When integration baseline is skipped or filtered, the phase learning note must record the excluded test classes or categories and the rationale.

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

Each phase that touches applet behavior should include a scoped or full-module self-runnable integration lifecycle against the YubiKey 5.8 beta key when feasible.

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
- Deferred future improvements:
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
- Allowed agent-run tests: all self-runnable integration checks; unattended FIDO/FIDO2/WebAuthn User Presence, UV, touch, and insert/remove tests are deferred or human-coordinated because User Presence waits fail after roughly 30 seconds
```

The beta key serial is 103. The `appsettings.json` allow-list convention still needs confirmation before integration preflight commands run.

## Agent-Runnable Integration Tests

Agents may run integration tests only when all are true:

- Scoped to the YubiKey 5.8 beta key.
- Scoped to serial 103 when serial filtering is available.
- The phase ISA explicitly records the integration scope and command shape.
- No unattended FIDO/FIDO2/WebAuthn User Presence.
- No unattended FIDO/FIDO2/WebAuthn UV.
- No unattended physical touch.
- No insert/remove coordination without human coordination.
- Uses `dotnet toolchain.cs test`, never `dotnet test`.

Default command shape:

```bash
dotnet toolchain.cs -- test --integration --project <Module> --smoke --filter "<focused filter>"
```

`--smoke` is the default because it skips `Slow` and `RequiresUserPresence`.

`--smoke` is not sufficient by itself. Each phase ISA must also list the exact test IDs, filters, or full-module integration command the agent intends to run, and must classify whether any selected test requires human coordination. Agent-run integration filters are an allowlist unless the phase ISA records a full-module integration run.

Applet reset, slot programming, key/certificate/object creation or deletion, PIN/PUK/password/access-code changes, management-key changes, credential enrollment, authenticator configuration, allowlist/certificate storage, or Security Domain key changes are not blocked merely because they mutate state. They are agent-runnable when the test is self-contained, can run unattended, and either restores or documents its cleanup/reset behavior.

For FIDO2 and WebAuthn:

- `GetInfo`-style tests are usually agent-runnable.
- MakeCredential, GetAssertion, reset, PIN, UV, biometric, and user-presence flows are not agent-runnable unless a human explicitly coordinates. Unattended User Presence waits fail after roughly 30 seconds, so defer them by default.
- FIDO/WebAuthn phases should primarily use unit tests plus non-UP/UV integration smoke checks.

## Phase Gate

A phase is not complete until:

- [ ] Branch check shows `yubikit-consolidation` before implementation/delegation.
- [ ] Phase ISA exists and stays within the approved autonomous program scope, or separately records explicit approval for any scope expansion.
- [ ] Phase ISA maps work to `ISC-*` criteria.
- [ ] Phase ISA lists candidate `Core`, `Tests.Shared`, or `Cli.Shared` promotions, or states none expected.
- [ ] Phase ISA lists exact self-runnable integration test IDs/filters/full-module commands, or skip rationale.
- [ ] Phase ISA records any Core API changes as intentional design/security/ownership improvements, and confirms no public module API change unless explicitly approved.
- [ ] Beta key serial/scope confirmed if integration tests apply; serial is 103 unless superseded by human decision.
- [ ] Management preflight evidence is captured if integration tests apply, including applet firmware when observable.
- [ ] DevTeam implementation complete.
- [ ] DevTeam review complete.
- [ ] Cross-vendor review complete or human-approved waiver recorded.
- [ ] Focused build passes.
- [ ] Focused unit tests pass.
- [ ] Pre-refactor and post-refactor integration baselines match or improve, excluding only deferred unattended FIDO/FIDO2/WebAuthn User Presence / UV / touch / insert-remove tests or other human-coordinated flows.
- [ ] Scoped or full-module self-runnable integration tests pass, or skip rationale is recorded.
- [ ] Verification command text, filters, and results are recorded.
- [ ] Core, `Tests.Shared`, `Cli.Shared`, and public module API candidates are accepted, rejected, or deferred with rationale.
- [ ] Deferred future improvement candidates are recorded in the learning note or explicitly marked none.
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
- The implementation needs public module API changes not already approved.
- The phase creates pressure to introduce operation-specific command classes or deep helper layers.
- Review finds the protocol flow became harder to inspect.
- Focused verification fails twice for different root causes.
- Integration testing needs FIDO/FIDO2/WebAuthn User Presence, UV, touch, insert/remove, or other human coordination not previously approved.
- A public module API change decision becomes unavoidable but was not approved.

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
- Core APIs may change when they improve ownership, security, explicitness, or architectural clarity.
- Public module APIs remain stable unless explicitly approved.
- Add behavior tests only.
- Add meaningful tests when they prove protocol bytes, state transitions, security cleanup, integration behavior, or regression-prone behavior.
- Zero sensitive buffers, including encoded command payloads.
- Run all phase-recorded self-runnable integration tests; exclude only tests requiring UP/UV/touch/insert-remove help or other human coordination.
- If FIDO/FIDO2/WebAuthn integration is run, defer User Presence / UV / touch flows unless explicitly human-coordinated because unattended User Presence waits fail after roughly 30 seconds.

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

Preserve one public `PivSession` facade while moving protocol-heavy areas into shallow feature namespaces.

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

### Phase 10: Follow-Up Program Work Plan

Status: in progress.

Artifacts:

- `docs/plans/module-consolidation/phase-10-follow-up-program-work-plan.md`
- `docs/plans/module-consolidation/phase-10-follow-up-program-learnings.md`

Purpose:

- record what Phases 1-9 fixed
- classify deferred concerns as fix now, defer, reject, or final-audit-only
- define the follow-up phase order before source work resumes
- preserve the final same-criteria reassessment as a separate read-only phase

Phase 10 is documentation-only. It does not approve source-code changes for later phases.

### Phase 11: Core SCP Chained Response

Targets:

- characterize plain and SCP response chaining at byte level
- decide whether send-remaining APDUs are correctly wrapped or raw under SCP
- preserve OATH `INS_SEND_REMAINING` (`0xA5`) behavior
- avoid introducing a broad APDU framework or operation-specific command classes

### Phase 11 Detour: Application Session Lifecycle Audit

Artifact:

- `docs/plans/module-consolidation/application-session-lifecycle-audit-plan.md`

Outcome:

- Lifecycle ownership fix committed as `2e167a3f fix(fido2): remove backend protocol ownership`.
- Mechanical FIDO2 backend rename committed separately as `bf6ce564 refactor(fido2): rename transport backends`.
- Current working model: sessions own protocols, `ApplicationSession.Protocol` owns final effective protocol disposal, SCP wrappers own SCP processor state, and FIDO2 backends borrow their protocols.
- Deferred decision: revisit whether `ApplicationSession` should own connection lifecycle, whether direct callers should retain ownership, or whether ownership should be configurable through an explicit `leaveOpen` / `ownsConnection` option.

### Phase 12: Core `ConnectionType` Semantics

Targets:

- review and repair or explicitly constrain `[Flags]` semantics
- address that `HidOtp = 3` is exactly equal to `Hid | HidFido`, and that `All` redundantly ORs the overlapping value
- pin explicit numeric values before any enum-shape change so reordering cannot silently alter serialized or public values
- add unit coverage for filtering semantics, exact values, and compatibility assumptions that could regress through `HasFlag`-style use
- inspect `Transport` as a related `[Flags]` enum but do not change it unless Phase 12 explicitly expands scope or defers it with rationale

### Phase 13: Core `FirmwareVersion` / `Feature` Firmware Gates

Targets:

- make `FirmwareVersion.IsAlphaOrBeta` the single source of truth for alpha/beta/test firmware sentinel handling
- explicitly reconcile current source behavior: `ApplicationSession.IsSupported` already treats any `Major == 0` version as the sentinel, while `FirmwareVersion.IsAlphaOrBeta` currently only treats `0.0.0` that way
- update existing `FirmwareVersionTests` that assert `0.0.1` / `0.1.0` are not alpha/beta, because Phase 13 intentionally broadens that semantic
- check `FirmwareVersion` comparison and ordering consumers because broadening `IsAlphaOrBeta` affects `IsAtLeast(...)`, `IsLessThan(...)`, and `CompareTo(...)`
- inventory all direct `Major == 0` sentinel consumers, including `ApplicationSession` and non-session protocol code such as `PcscProtocol`
- ensure `Feature.IsSupportedByFirmware(...)` compares through `Feature.Version` consistently with the `FirmwareVersion` comparison semantics Phase 13 changes
- add `Feature.IsSupportedByFirmware(FirmwareVersion firmwareVersion)` for firmware-only gates
- make `ApplicationSession.IsSupported(Feature feature)` delegate to `Feature`
- keep transport, hardware configuration, applet state, auth state, and runtime session facts out of `Feature`
- replace duplicated direct `Major == 0` / `Major != 0` firmware-gate checks where applicable

### Phase 14: FIDO2 SmartCard Transport Provenance

Targets:

- clarify USB/NFC SmartCard FIDO2 support boundaries
- consume the Phase 13 firmware-gate primitive rather than inventing another local sentinel rule
- if Phase 13 is split or deferred, preserve the current FIDO2-local firmware gate as an explicit temporary dependency fallback instead of creating a second abstraction
- keep transport, FIDO2 AID exposure, and firmware evidence local to FIDO2 session logic

### Phase 15: CLI Secret Policy + OATH Unlock Migration

Targets:

- decide command-line argv secret policy before migrating more command families
- migrate exactly one named path: OATH password unlock
- make the argv secret policy program-wide, even though this phase migrates only one command-family path
- do not combine with broad parser/session helper consolidation

### Phase 16: API And Package Compatibility Checkpoint

Targets:

- inspect public API and package/dependency risk after source-risk phases
- catch compatibility drift before test/tooling infrastructure changes

### Phase 17: Test Runner And Hardware Coordination

Targets:

- address xUnit v3 focused-filter/toolchain friction
- define FIDO2/WebAuthn User Presence and User Verification manual coordination lanes
- do not make UP/UV tests unattended gates

### Phase 18: Docs QA Tooling

Targets:

- add bounded active-doc validation if feasible
- validate or document active README/example snippet limits
- exclude archived docs/plans cleanup unless separately approved

### Phase 19: Final Reassessment Audit

Targets:

- perform a read-only reassessment using the same grading criteria as `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`
- create a new final reassessment artifact instead of rewriting the baseline
- record grade deltas, evidence, remaining risks, and next recommended targets

### Phase 20: Quality Convergence Program ISA

Artifacts:

- `docs/plans/module-consolidation/phase-20-quality-convergence-before-composite-yubikey-ISA.md`
- `docs/plans/module-consolidation/phase-20-quality-convergence-learnings.md`

Targets:

- define the quality-convergence program before composite YubiKey device design
- exclude `Tests.TestProject` from scoring, phase scope, and composite-readiness gates
- require `Core`, every applet module, and `Tests.Shared` to reach at least `B+` before composite design starts
- reframe package/API work as SDK-family public API shape alignment, not a hard external package compatibility gate
- save composite YubiKey questions for later owner interviews and stop before that design begins

### Phase 21: Core A- Readiness And SDK-Family API Alignment Audit

Targets:

- repair Core DI documentation drift around removed entry points
- audit duplicate CRC/checksum utilities and consolidate only if source evidence shows mechanical duplication
- compare public Core/device/session concepts against `yubikit-swift`, Python `yubikey-manager` / `yubikit`, and `yubikit-android`
- keep package validation audit-only unless a later owner decision establishes a baseline to protect

### Phase 22: Tests.Shared Recorder And Harness Decision

Targets:

- decide whether repeated fake smart-card recorder patterns justify shared test infrastructure
- preserve or improve the current `Tests.Shared` `B+` posture
- keep integration-test allow-list and hardware safety guidance source-backed

### Phase 23: PIV Byte-Level Coverage

Targets:

- add focused fake APDU coverage for high-risk crypto/key-operation encodings
- simplify reset/auth/default-credential integration choreography only where maintainability improves
- evaluate moving root-level PIV files closer to feature folders when locality improves and namespace/API shape stays unchanged; namespace preservation wins when a target folder uses a different namespace convention
- run the phase-approved PIV integration suite after Management preflight
- preserve flat PIV protocol flow and public API unless explicitly approved

### Phase 24: YubiHsm Byte-Level Coverage

Targets:

- add fake APDU byte-level tests for credential operations
- verify sensitive APDU payload lifecycle remains explicit
- defer local example CLI parsing work unless it is still a material module risk

### Phase 25: OpenPGP Session Wire Tests

Targets:

- add fake APDU tests around session-level wire behavior
- preserve OpenPGP-specific model richness
- consider shared DER, DigestInfo, or OID helpers only if reuse is proven

### Phase 26: FIDO2 Remaining CTAP Consistency

Targets:

- apply the canonical request-construction pattern beyond MakeCredential and GetAssertion
- prioritize PIN, credential management, config, bio enrollment, large blobs, and extension paths
- resolve sensitive CBOR/auth-param copy ownership where touched

### Phase 27: WebAuthn Maintainability Split

Targets:

- split ceremony orchestration, validation, PIN/UV token flow, request mapping, error mapping, and response building only where readability improves
- preserve FIDO2 delegation
- keep the public construction story stable unless a phase ISA explicitly approves a public change

### Phase 28: OATH Locality Polish

Targets:

- reduce monolithic session pressure with pure encode/parse helpers where clarity improves
- preserve Core configured chained-response behavior for `INS_SEND_REMAINING = 0xA5`
- avoid a broad OATH rewrite

### Phase 29: SecurityDomain Test And Locality Follow-Up

Targets:

- use the Phase 22 test-harness decision
- add coverage or locality improvements only where they still improve maintainability
- keep the ban on operation-specific `PutKeyCommand` / `GetDataCommand` style protocol objects

### Phase 30: YubiOtp And Management Stability Polish

Targets:

- keep stable modules stable
- extract YubiOtp protocol codecs only if current session noise is materially harming readability
- tighten Management backend payload/read-path clarity only if source review finds remaining ambiguity

### Phase 31: Docs QA CI Gate

Targets:

- wire `dotnet toolchain.cs -- docs-qa` into CI as a bounded active-doc validation gate
- keep README snippet compilation separate unless a phase ISA explicitly approves it

### Phase 32: Same-Criteria Quality Reassessment

Targets:

- regrade active surfaces using the same criteria as `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`
- exclude `Tests.TestProject`
- record whether `Core`, every applet module, and `Tests.Shared` are at least `B+`
- feed every Phase 20+ learning note into the reassessment

### Stop Gate: Composite YubiKey Interviews

After Phase 32, stop and wait for owner interviews before designing or implementing composite YubiKey discovery.

Saved interview questions:

- should `IYubiKey` remain a per-connection interface or become a physical-device abstraction?
- how should `ConnectionType` filters behave when one physical key supports multiple interfaces?
- what identity/cache key should be used: serial, PID plus fingerprint, reader/path group, or resolved `DeviceInfo` identity?
- how should NFC avoid false USB aggregation?
- should per-interface discovery remain available for advanced callers?
- how closely should .NET follow Python's `_PidGroup` / `_UsbCompositeDevice` model versus using more explicit .NET types?

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
- phase-scoped documented beta-key integration when useful
- learning note before next phase
- commit after success
- `/Ping` after each successful phase

## Final Constraint

No learning note, no commit. No commit, no next phase.
