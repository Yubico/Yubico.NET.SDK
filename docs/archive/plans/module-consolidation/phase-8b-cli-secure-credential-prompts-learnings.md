# Phase 8B Learnings: CLI Secure Credential Prompt Foundation

Use this note as the handoff record for Phase 8B of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Base branch: `yubikit-applets`
- Base commit: `bfc6bdd5`, per consolidation ISA
- Branch check command/result: `git status --short --branch` showed `## yubikit-consolidation`
- Unrelated worktree changes present: yes, two untracked Core YubiKey note files remained unstaged
- Refactor work ran only on `yubikit-consolidation`: yes
- Scope: add a `Cli.Shared` disposable credential-byte primitive and migrate only PIV PIN/PUK access commands to it
- Criteria satisfied: yes
- Criteria deferred: command-line argv string lifetime, remaining FIDO/OpenPGP/OATH/HsmAuth prompt paths, management-key hex prompt model, broad CLI parser/session consolidation
- Promotion candidates declared up front: `Cli.Shared` secure credential primitive accepted; broader `Cli.Commands` helpers deferred
- Files changed: `src/Cli.Shared/src/Output/SecureCredential.cs`, `src/Cli.Shared/src/Output/PinPrompt.cs`, `src/Cli.Shared/src/Yubico.YubiKit.Cli.Shared.csproj`, `src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Yubico.YubiKit.Cli.Shared.UnitTests.csproj`, `src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Output/SecureCredentialTests.cs`, `src/Cli.Commands/src/Piv/PivCommands.cs`, this learning note
- Tests run: RED/GREEN `Cli.Shared` unit test cycle, `Cli.Commands` unit tests, scoped format verification, focused CLI builds
- Integration tests run: none
- Result: passed with Cato concerns; concerns are accepted/deferred as documented below
- Commit: `fc79b80e feat(cli): add secure credential prompts`
- `/Ping` sent after successful phase: yes, included after commit and compact summary

## Phase Scope Decision

- Accepted slice: create the owned credential buffer foundation and migrate PIV PIN/PUK access commands only.
- Split rationale: full CLI credential cleanup touches FIDO, OpenPGP, OATH, HSM Auth, OTP static-password, management-key hex parsing, and CLI option binding. One command family provides a safer first migration.
- Explicit limitation: command-line option values still arrive as managed `string` values from argv/Spectre binding. Phase 8B mitigates downstream byte ownership only; it does not claim argv strings can be zeroed.

## Final Behavior

- Interactive PIV access prompts now use `SecureCredential.Prompt(...)`, which reads masked console input into a byte buffer instead of returning a `Spectre.Console` string.
- `SecureCredential` owns credential bytes and zeros the full buffer in `Dispose()`.
- `SecureCredential.Memory` throws after disposal.
- PIV `change-pin`, `change-puk`, and `unblock-pin` commands pass `ReadOnlyMemory<byte>` from disposable credentials into existing PIV APIs.
- Command-line `--pin`, `--new-pin`, `--puk`, and `--new-puk` still bind as strings, then immediately convert to owned bytes and dispose after use.
- Prompt backspace handling tracks UTF-8 byte counts per entered character so multibyte input is removed as a whole character.

## Hardware Target

- Device: YubiKey 5.8 beta
- Serial: `103`
- Firmware source of truth: Management `GetDeviceInfoAsync`
- Management firmware observed: not re-run in this phase
- Applet firmware observed, if observable: not applicable
- Applet firmware caveat observed: not applicable

## Integration Lifecycle

- Management preflight command/result: skipped
- Management preflight evidence captured before applet tests: not applicable; no applet integration tests ran
- Agent-runnable integration test allowlist: none
- Integration scope was read-only: not applicable
- Tests skipped: PIV PIN/PUK CLI hardware flows
- Skip reason: PIV change PIN/PUK/unblock PIN mutate persistent credential state and require human coordination/reset discipline
- Skip approved by: approved Phase 8B plan and consolidation ISA read-only integration rule
- Selected tests mutate persistent state: yes, if run; therefore skipped
- User Presence / UV required: no, but persistent state mutation is enough to skip
- Human-coordinated hardware needed: optional future secure-prompt CLI smoke only
- Persistent state changed: no
- Destructive tests skipped completely: yes
- Reset/cleanup performed: no
- Result: unit tests, builds, format, and Cato review are final verification for this phase

## What Worked

- Pattern that improved security: one owned disposable byte buffer replaces prompt-returned immutable strings for the migrated interactive PIV flows.
- Pattern that improved testability: `SecureCredential.FromConsoleKeysForTesting(...)` gives unit tests a prompt-path seam without replacing the production console path.
- Pattern that improved locality: `PivHelpers.GetCredential(...)` keeps the PIV command methods flat while centralizing option-vs-prompt credential ownership.

## What Did Not Work

- Rejected approach: migrate every CLI prompt in one phase.
- Rejected approach rationale: remaining secret paths differ by transport, credential type, parsing format, and command semantics.
- Helpers or abstractions that were too deep: no global credential service or command interceptor state bag was added.
- Changes that looked DRY but harmed readability: no broad session/parser helper consolidation attempted.

## House Style Updates

- Existing house-style rule confirmed: secret input should become owned byte buffers as early as feasible and be zeroed in finally/dispose.
- Existing house-style rule confirmed: command-line secret options are inherently weaker than interactive prompts because argv strings cannot be zeroed.
- Possible addition to `docs/SDK-HOUSE-STYLE.md`: CLI credential prompts should prefer disposable byte ownership; command-line secret options must be documented as less secure.

## Reusable Patterns

- Pattern: sealed disposable `SecureCredential` with `ReadOnlyMemory<byte>` exposure and zero-on-dispose.
- Generalization class: accepted for further CLI prompt migrations.
- Where it applies: CLI PIN/PUK/password prompts and argv-bound secrets that must be converted to bytes for SDK APIs.
- Where it should not apply: long-lived SDK secret owners, protocol payload builders, or public SDK API types.
- Example files: `src/Cli.Shared/src/Output/SecureCredential.cs`, `src/Cli.Commands/src/Piv/PivCommands.cs`

## Core / Shared Promotion Candidates

- Candidate: `SecureCredential`
- Declared in phase ISA up front: yes, `Cli.Shared` secure prompt foundation was in scope
- Should move to: accepted in `Cli.Shared`
- Evidence: PIV access commands now consume the primitive; `Cli.Shared` tests prove UTF-8 exposure, disposal zeroing, disposed access guard, empty-value rejection, and multibyte backspace behavior
- Risk: medium; `ReadOnlyMemory<byte>` slices captured before disposal can still observe the underlying buffer after disposal. Callers must not retain memory beyond the credential lifetime.
- Decision: accepted
- Decision rationale: the primitive is a shallow ownership helper, not a new architecture layer, and it improves migrated prompt paths immediately
- Revisit trigger: if more commands need safer scoped usage than raw `ReadOnlyMemory<byte>` exposure can provide

## Cross-Module Implications

- Modules likely affected next: FIDO CLI, OpenPGP CLI, OATH CLI, HSM Auth CLI, OTP static-password CLI
- Next module should copy: migrate one credential family at a time with tests before changing broad command helpers
- Next module should avoid: claiming command-line secrets can be fully scrubbed from managed memory
- Potential API compatibility concern: none for SDK packages; `Cli.Shared` is non-packable CLI infrastructure

## Verification Evidence

- Branch check commands: `git status --short --branch`
- Branch check exit result: passed; branch was `yubikit-consolidation`
- RED test command: `dotnet toolchain.cs -- test --project Cli.Shared.UnitTests`
- RED test exit result: failed first because `SecureCredential` did not exist, then failed for the missing prompt test seam before the multibyte backspace fix
- Build commands: `dotnet toolchain.cs -- build --project Cli.Shared`; `dotnet toolchain.cs -- build --project Cli.Commands`; `dotnet toolchain.cs -- build --project Cli.YkTool`
- Build exit result: passed, 0 warnings, 0 errors for all three focused builds
- Unit test commands: `dotnet toolchain.cs -- test --project Cli.Shared.UnitTests`; `dotnet toolchain.cs -- test --project Cli.Commands.UnitTests`
- Unit test exit result: passed, `Cli.Shared.UnitTests` 5/5 and `Cli.Commands.UnitTests` 3/3
- Format command: `dotnet format --verify-no-changes --include src/Cli.Shared/src/Yubico.YubiKit.Cli.Shared.csproj src/Cli.Shared/src/Output/SecureCredential.cs src/Cli.Shared/src/Output/PinPrompt.cs src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Yubico.YubiKit.Cli.Shared.UnitTests.csproj src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Output/SecureCredentialTests.cs src/Cli.Commands/src/Piv/PivCommands.cs`
- Format exit result: passed after scoped `dotnet format`
- Integration test commands: none
- Integration test exit result: not applicable
- Cross-module verification plan, if shared infrastructure changed: build `Cli.Shared`, `Cli.Commands`, and `Cli.YkTool`; run `Cli.Shared.UnitTests` and `Cli.Commands.UnitTests`
- Results: all focused builds, tests, and scoped format verification passed
- Manual review notes: diff limited to approved CLI files and this learning note; unrelated Core YubiKey note files remained unstaged

## Review Summary

- DevTeam engineer result: not run; single-author implementation within approved narrow scope
- DevTeam reviewer result: not run; final self-review/diff inspection completed for narrow approved scope
- Cross-vendor review result: completed; verdict `concerns`, criticality `medium`, auditor `google-vertex-anthropic/claude-opus-4-8@default`
- Cross-vendor review waiver, if any: none
- Cato prompt/output: `/tmp/opencode/cato-phase8b-cli-secure-credentials-audit.txt`, `/tmp/opencode/cato-phase8b-cli-secure-credentials-audit-retry.jsonl`, `/tmp/opencode/cato-phase8b-cli-secure-credentials-json-only.txt`, `/tmp/opencode/cato-phase8b-cli-secure-credentials-json-only.jsonl`
- Findings fixed during review: multibyte backspace handling and prompt-path test seam; `FromUtf8String` no longer computes UTF-8 byte count separately after allocation
- Findings deferred: argv string lifetime, retained `ReadOnlyMemory<byte>` slice semantics, remaining CLI secret paths, optional `.sln` registration for CLI unit tests
- Findings resolved by existing evidence: underlying PIV PIN/PUK APDU payloads are zeroed in existing PIV protocol finally blocks

## Cato Findings

| Severity | Finding | Disposition |
| --- | --- | --- |
| warning | Source argv/Spectre-bound credential strings persist as managed strings and cannot be zeroed. | Accepted/deferred. This was explicitly inside the approved limitation; Phase 8B only converts immediately to owned bytes and disposes those bytes. Future CLI UX can discourage or remove command-line secret options. |
| warning | Callers can retain a `ReadOnlyMemory<byte>` value before `SecureCredential.Dispose()`, then observe zeroed memory after disposal instead of getting a hard fault. | Accepted/deferred. PIV migrated calls do not retain memory, and underlying PIV protocol copies before await. A future helper shape could offer scoped callback usage if broader migrations need stronger lifetime discipline. |
| info | Underlying PIV protocol ArrayPool payload zeroing was not asserted in gathered facts. | Resolved by evidence. `PivAuthenticationProtocol.ChangePinAsync` zeroes `pinData.AsSpan(0, 16)` in finally; `PivMetadataProtocol.ChangePukAsync` and `UnblockPinAsync` zero `pukPair` / `pukPinPair` in finally. |
| info | New `Cli.Shared.UnitTests` project is discovered by toolchain but not in `.sln`. | Deferred. Existing `Cli.Commands.UnitTests` follows the same shape; toolchain test/build discovery covered this phase. Consider solution registration as a CLI test-infrastructure cleanup. |
| info | `PivCommands.cs` final newline was not enforced by `dotnet format`. | Non-blocking. Scoped format verification passed. |
| info | PIV PIN/PUK integration tests skipped. | Accepted. These flows mutate persistent state and require human coordination; unit/build/review coverage is sufficient for this phase. |

## Deferred Future Improvements

- Title: Migrate remaining CLI prompt paths to `SecureCredential`
- Source phase: Phase 8B CLI Secure Credential Prompt Foundation
- Rationale: FIDO, OpenPGP, OATH, HSM Auth, and OTP still have prompt or secret string paths outside the approved PIV slice.
- Why it is deferred: each applet has different credential semantics and should be migrated in small slices.
- Likely owning area: `Cli.Commands` / `Cli.Shared`
- Suggested timing: Phase 9 candidate or later CLI credential hardening pass
- Needs human approval, hardware coordination, or Cato review: human approval and Cato review; hardware only for human-coordinated smoke if mutating state

- Title: Decide CLI command-line secret-option policy
- Source phase: Phase 8B CLI Secure Credential Prompt Foundation
- Rationale: argv-bound strings cannot be zeroed and may appear in shell history or process listings.
- Why it is deferred: removing or discouraging options is CLI UX/API policy, not just memory hygiene.
- Likely owning area: `Cli.Commands`
- Suggested timing: later CLI UX/security phase
- Needs human approval, hardware coordination, or Cato review: human approval and Cato review; no hardware needed

- Title: Consider scoped callback API for `SecureCredential`
- Source phase: Phase 8B CLI Secure Credential Prompt Foundation
- Rationale: `ReadOnlyMemory<byte>` can be retained by callers before dispose. A callback API could make intended lifetime harder to misuse.
- Why it is deferred: current PIV migrated calls copy before await and do not retain; broader migrations should provide more evidence.
- Likely owning area: `Cli.Shared`
- Suggested timing: after two or more additional command-family migrations
- Needs human approval, hardware coordination, or Cato review: human approval and Cato review; no hardware needed

- Title: Register CLI unit test projects in the solution if required by tooling
- Source phase: Phase 8B CLI Secure Credential Prompt Foundation
- Rationale: `toolchain.cs` discovers CLI test projects by glob, but `.sln`-based IDE/build tooling will not see them.
- Why it is deferred: existing `Cli.Commands.UnitTests` has the same shape, and all repository verification used toolchain discovery.
- Likely owning area: CLI test infrastructure
- Suggested timing: later tooling cleanup
- Needs human approval, hardware coordination, or Cato review: no hardware; human approval recommended

## Abort / Split Assessment

- Wrong branch detected: no
- Phase exceeded approved scope: no
- Public API change required: no SDK public API change; non-packable CLI infrastructure changed
- Helper depth concern found: no
- Protocol flow became harder to inspect: no protocol flow changed
- Verification failed twice for different root causes: no
- Unapproved hardware coordination required: no
- Persistent-state or destructive integration required: no for implemented verification; hardware mutation tests skipped
- Core/shared promotion became unavoidable: no beyond approved `Cli.Shared` secure credential primitive
- Abort learning note required: no
- Abort learning note committed with human approval: not applicable
- Outcome: continue

## Next Phase Inputs

- Required reading before next phase: `docs/SDK-HOUSE-STYLE.md`
- Required reading before next phase: `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`
- Required reading before next phase: `docs/plans/module-consolidation/ISA.md`
- Required reading before next phase: this learning note
- Pattern to apply: migrate the next CLI credential family in a narrow slice; do not combine with broad session/parser cleanup.
- Risk to watch: command-line options for secrets remain weaker than interactive prompts.
- Open questions for human approval: whether Phase 9 should continue CLI credential hardening or switch to the next consolidation backlog item.

## Compact Summary

- Goal: add CLI disposable credential bytes and migrate PIV PIN/PUK access prompts
- Files changed: `SecureCredential`, `PinPrompt`, `Cli.Shared` test project, PIV access command credential handling, learning note
- Final pattern: prompt or argv value becomes `SecureCredential`; command passes `Memory`; `Dispose` zeros bytes
- Tests passed: RED/GREEN `Cli.Shared` tests, `Cli.Commands` tests, focused builds, scoped format
- Integration lifecycle: skipped because PIV PIN/PUK flows mutate persistent hardware state
- Shared/Core candidates: `SecureCredential` accepted in `Cli.Shared`; broader scoped callback API deferred
- Cato: `concerns`/medium; argv string lifetime accepted, Memory-retention risk deferred, PIV APDU zeroing resolved by existing evidence
- Deferred future improvements: remaining CLI prompt migrations, command-line secret policy, callback lifetime API, optional solution registration
- Learning note path: `docs/plans/module-consolidation/phase-8b-cli-secure-credential-prompts-learnings.md`
- Commit: `fc79b80e feat(cli): add secure credential prompts`
- `/Ping` status: sent
