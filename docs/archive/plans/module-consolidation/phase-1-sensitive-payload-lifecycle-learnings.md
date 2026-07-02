# Phase 1 Learnings: Sensitive Payload Lifecycle

Use this note as the handoff record for Phase 1 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Base branch: `yubikit-applets`
- Base commit: `bfc6bdd5`
- Branch check command/result: `git status --short --branch` showed `## yubikit-consolidation`
- Unrelated worktree changes present: yes, pre-existing untracked files under `src/Core/src/YubiKey/` and pre-existing consolidation docs from Phase 0
- Refactor work ran only on `yubikit-consolidation`: yes
- Scope: zero encoded sensitive APDU/config payloads in `YubiHsm` and `Management`; update consolidation governance for branch, serial, destructive-test ban, and `/Ping`
- Criteria satisfied: `ISC-1.1`, `ISC-2`, `ISC-3`, `ISC-5`, `ISC-6`, `ISC-8`, `ISC-9`, `ISC-10`, `ISC-13` governance added
- Criteria deferred: none for implementation; commit and `/Ping` remain final phase-gate actions after this note is staged
- Promotion candidates declared up front: none expected
- Files changed: `HsmAuthSession.cs`, `SessionKeysTests.cs`, Management backend/session/config/test files, consolidation docs
- Tests run: focused builds, module unit tests with added zeroing assertions, scoped format verification
- Integration tests run: none
- Result: implementation, behavior tests, local review, and Cato audit passed with process concerns addressed or queued for the final gate
- Commit: pending at note write time; next action after staging approved files
- `/Ping` sent after successful phase: queued after commit and compact summary

## Hardware Target

- Device: YubiKey 5.8 beta
- Serial: 103
- Firmware source of truth: Management `GetDeviceInfoAsync`
- Management firmware observed: not observed in this phase
- Applet firmware observed, if observable: not observed in this phase
- Applet firmware caveat observed: not applicable

## Integration Lifecycle

- Management preflight command/result: not run
- Management preflight evidence captured before applet tests: no
- Management preflight exception path used: no
- Alternate identity proof, if preflight skipped: serial 103 recorded, but allow-list convention not confirmed
- Agent-runnable integration test allowlist: none for this phase
- Integration scope was read-only: yes, by policy; no integration command executed
- Tests run: none
- Tests skipped: all integration tests
- Skip reason: destructive and persistent-state tests are banned; read-only beta allow-list convention was not confirmed
- Skip approved by: human instruction, “Serial is 103. Yes skip destructive tests completely”
- Selected tests mutate persistent state: no selected tests
- User Presence / UV required: no selected tests
- Human-coordinated hardware needed: no
- Human-coordinated hardware scheduled/deferred/replaced: replaced by focused build/unit verification
- Persistent state changed: no
- Destructive tests skipped completely: yes
- Reset/cleanup performed: not applicable
- Result: integration skipped by approved scope

## What Worked

- Patterns that improved readability: the existing `PutCredentialAsymmetricAsync` `Memory<byte> data = default` plus `finally` zeroing pattern scaled cleanly to neighboring YubiHsm methods.
- Patterns that improved testability: keeping protocol construction in-place allowed existing unit/build coverage to validate the change without test-only hooks.
- Patterns that improved security/memory hygiene: encoded command payloads and Management config payloads now have explicit post-transmit zeroing boundaries.
- Patterns that improved security/memory hygiene: `CALCULATE` response raw buffers are zeroed after copying into disposable `SessionKeys`.
- Patterns that improved testability: focused unit tests now assert practical zeroing boundaries for Management config buffers and YubiHsm APDU response raw storage.

## What Did Not Work

- Rejected approaches: no new sensitive-buffer wrapper was introduced because local `try/finally` was simpler and kept flow visible.
- Helpers or abstractions that were too deep: none introduced.
- Changes that looked DRY but harmed readability: no shared helper promoted to `Core`; the only new helper is module-local `ZeroApduResponse`.
- Tooling issue: filtered `dotnet toolchain.cs -- test --project <Module> --filter "FullyQualifiedName~...UnitTests"` failed before tests ran because the toolchain passed `--minimum-expected-tests 0` to xUnit v3.
- Tooling issue: filtered `dotnet toolchain.cs -- test --project <Module> --filter "Name=..."` also failed before tests ran for xUnit v3 projects because the toolchain passed `--minimum-expected-tests 0`; full module tests were used for final verification.
- Tooling issue: full `dotnet format --verify-no-changes` failed on unrelated pre-existing files; scoped format verification was used for Phase 1 files.

## House Style Updates

- Existing house-style rule confirmed: sensitive APDU/CTAP payloads should be zeroed after transmit while preserving visible protocol construction.
- Existing house-style rule confirmed: avoid command objects for protocol operations.
- Rule that needs clarification: response-derived sensitive buffers may need cleanup even when copied into disposable objects.
- Possible addition to `docs/SDK-HOUSE-STYLE.md`: note that response wrappers holding secret-derived bytes should be zeroed or disposed after copying into owned secure containers.

## Reusable Patterns

- Pattern: `Memory<byte> data = default` before `try`, assign encoded payload inside `try`, transmit, zero `data.Span` in `finally`.
- Generalization class: candidate for one more module trial
- Where it applies: encoded APDU/CTAP/config payloads that contain secrets and are caller-owned until transmit completes
- Where it should not apply: non-sensitive payloads where zeroing adds noise without security value
- Example files: `src/YubiHsm/src/HsmAuthSession.cs`, `src/Management/src/ManagementSession.cs`

- Pattern: zero APDU response raw storage after copying secret-derived response bytes into disposable owned containers.
- Generalization class: candidate for one more module trial
- Where it applies: response wrappers that hold session keys, derived secrets, PIN/UV auth material, or secret-derived cryptograms
- Where it should not apply: ordinary metadata responses and public-key responses
- Example files: `src/YubiHsm/src/HsmAuthSession.cs`

## Core / Shared Promotion Candidates

- Candidate: shared sensitive response cleanup helper
- Declared in phase ISA up front: no
- Should move to: stay module-local
- Evidence: only YubiHsm needed this specific `ApduResponse.RawData` cleanup in Phase 1
- Risk: promoting now would create a premature abstraction around one proven use
- Decision: rejected
- Decision rationale: wait for at least one more module trial before considering `Core`
- Revisit trigger: another module needs to zero sensitive `ApduResponse.RawData` after copying into an owned container
- Demotion/reversal needed for previous shared helper: no
- Demotion/reversal rationale: not applicable

## Cross-Module Implications

- Modules likely affected: PIV, OATH, FIDO2, WebAuthn, SecurityDomain where response-derived secret material may exist
- Next module should copy: explicit `try/finally` ownership boundaries around encoded sensitive payloads
- Next module should avoid: command classes or broad lifecycle wrappers that hide wire construction
- Potential API compatibility concern: changing async methods from non-async Task-returning to `async Task` can change synchronous exception timing; preserve timing by validating and serializing before returning a local async helper task

## Verification Evidence

- Branch check commands: `git status --short --branch`
- Branch check exit result: success, showed `## yubikit-consolidation`
- Build commands: `dotnet toolchain.cs -- build --project YubiHsm`
- Build exit result: passed, built 3 project(s) matching `YubiHsm`, 0 warnings, 0 errors
- Build commands: `dotnet toolchain.cs -- build --project Management`
- Build exit result: passed, built 3 project(s) matching `Management`, 0 warnings, 0 errors
- Unit test commands: `dotnet toolchain.cs -- test --project YubiHsm`
- Unit test exit result: passed, 51 total, 51 succeeded, 0 failed, 0 skipped
- Unit test commands: `dotnet toolchain.cs -- test --project Management`
- Unit test exit result: passed, 114 total, 114 succeeded, 0 failed, 0 skipped
- Added behavior test commands: `dotnet toolchain.cs -- test --project YubiHsm`; `dotnet toolchain.cs -- test --project Management`
- Added behavior test exit result: passed; new `ZeroApduResponse_ClearsOwnedRawResponseStorage` and `SetDeviceConfigAsync_ZeroesEncodedConfigAfterBackendWrite` assertions included in full module runs
- Format commands: `dotnet format --verify-no-changes --include src/YubiHsm/src/HsmAuthSession.cs src/Management/src/ManagementSession.cs src/Management/src/DeviceConfig.cs src/Management/src/IManagementBackend.cs src/Management/src/SmartCardBackend.cs src/Management/src/FidoHidBackend.cs src/Management/src/OtpBackend.cs`
- Format exit result: passed after scoped formatting of changed source files
- Integration test commands: none
- Integration test exit result: skipped by approved scope
- Command filters/projects: project filters only; method/name filters failed due toolchain/xUnit v3 `--minimum-expected-tests 0` issue before tests ran
- Cross-module verification plan, if shared infrastructure changed: not applicable
- Results: focused source verification and behavior-test verification passed
- Manual review notes: pre-edit risk review completed; final static review returned no findings
- Reviewer concerns resolved: `CALCULATE` response raw buffer zeroing, `SetDeviceConfigAsync` synchronous exception timing, persistent/destructive docs wording

## Review Summary

- DevTeam engineer result: primary orchestrator implemented after delegated pre-edit review
- DevTeam reviewer result: initial reviewer found three issues; final reviewer found no findings
- Cross-vendor review result: completed via Cato after OpenCode restart exposed `google-vertex-anthropic/claude-opus-4-8@default`
- Cross-vendor review waiver, if any: none used
- Waiver approved by: not applicable
- Waiver reason and scope: not applicable
- Waiver tooling failure/unavailability evidence: initial `AgentHarnessRouter.ts` route failed before restart; final Cato run wrote `/tmp/opencode/cato-phase-1-sensitive-payload-audit.jsonl`
- Fallback review performed: same-session delegated static review through the available general agent before Cato route was fixed
- Findings fixed: first review findings fixed; Cato behavior-test warning addressed by added unit tests; process findings are commit and `/Ping` gate actions
- Findings deferred: none
- Human decisions: none remaining for Phase 1 completion

## Abort / Split Assessment

- Wrong branch detected: no
- Phase exceeded approved scope: no
- Public API change required: no
- Helper depth concern found: no
- Protocol flow became harder to inspect: no
- Verification failed twice for different root causes: no
- Unapproved hardware coordination required: no
- Persistent-state or destructive integration required: no
- Core/shared promotion became unavoidable: no
- Abort learning note required: no
- Abort learning note committed with human approval: not applicable
- Outcome: continue to commit, compact summary, and `/Ping`

## Next Phase Inputs

- Required reading before next phase: `docs/SDK-HOUSE-STYLE.md`
- Required reading before next phase: `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`
- Required reading before next phase: `docs/plans/module-consolidation/ISA.md`
- Required reading before next phase: this learning note
- Patterns to apply: explicit post-transmit zeroing for encoded sensitive payloads
- Patterns to apply: preserve synchronous exception timing when adding async cleanup around existing Task-returning APIs
- Risks to watch: response-derived sensitive buffers retained inside immutable/read-only response wrappers
- Open questions for human approval: none for Phase 1

## Compact Summary

- Goal: zero encoded sensitive payloads in YubiHsm and Management
- Files changed: YubiHsm session/tests, Management config/session/backends/tests, consolidation governance docs
- Final pattern: visible `try/finally` cleanup around caller-owned encoded payloads and response raw storage
- Rejected approaches: command classes, shared lifecycle wrappers, Core promotion
- Tests passed: YubiHsm build, Management build, YubiHsm unit tests, Management unit tests, added zeroing assertions, scoped format verification
- Integration lifecycle: skipped by approved read-only/destructive-test policy
- Shared/Core candidates: response cleanup helper rejected for now; needs one more module trial
- House-style update needed: clarify response-derived sensitive buffer cleanup
- Next phase recommendation: after commit and `/Ping`, move to Tests.Shared harness consolidation
- Learning note path: `docs/plans/module-consolidation/phase-1-sensitive-payload-lifecycle-learnings.md`
- Commit: pending at note write time
- `/Ping` status: queued after commit and compact summary
