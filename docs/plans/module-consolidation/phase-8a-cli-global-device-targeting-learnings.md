# Phase 8A Learnings: CLI Global Device Targeting

Use this note as the handoff record for Phase 8A of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Base branch: `yubikit-applets`
- Base commit: `bfc6bdd5`, per consolidation ISA
- Branch check command/result: `git status --short --branch` showed `## yubikit-consolidation`
- Unrelated worktree changes present: yes, two untracked Core YubiKey note files remained unstaged
- Refactor work ran only on `yubikit-consolidation`: yes
- Scope: make unified CLI global `--serial` and `--transport` device-targeting options affect device selection
- Criteria satisfied: yes
- Criteria deferred: `--interactive` menu behavior; secure credential prompt overhaul; broad CLI command/helper consolidation
- Promotion candidates declared up front: `Cli.Shared` selector seam accepted; `Core`, `Tests.Shared`, and broader `Cli.Shared` promotions deferred
- Files changed: `src/Cli.Shared/src/Device/DeviceSelectorBase.cs`, `src/Cli.Commands/src/Infrastructure/YkCommandBase.cs`, `src/Cli.Commands/src/Infrastructure/YkDeviceSelector.cs`, `src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/Yubico.YubiKit.Cli.Commands.UnitTests.csproj`, `src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/Infrastructure/YkDeviceSelectorTests.cs`, this learning note
- Tests run: RED/GREEN CLI unit test cycle, scoped format verification, focused CLI builds
- Integration tests run: none
- Result: passed; Cato verdict `pass`, with info-level notes and one deferred pre-existing `ConnectionType` warning
- Commit: pending
- `/Ping` sent after successful phase: pending commit and compact summary

## Phase Scope Decision

- Accepted slice: `--serial` and `--transport` targeting only.
- Split rationale: CLI consolidation has several independent risks; device targeting is a small, high-value slice, while secure credential handling touches sensitive-data ownership across many commands.
- `--interactive` rationale: existing option is parsed but not used. Implementing it requires root applet menu routing and UX decisions, so it remains deferred.

## Final Behavior

- `--serial <number>` strictly filters discovered supported devices by Management-reported serial number.
- `--transport smartcard|fido|otp` strictly narrows selection to a requested applet-supported transport.
- `--transport ccid`, `--transport hidfido`, `--transport fido-hid`, `--transport hidotp`, and `--transport otp-hid` are accepted aliases.
- Invalid transport strings return `ExitCode.GenericError` with a clear error message.
- Valid transport values unsupported by the selected command return `ExitCode.FeatureUnsupported` before device discovery.
- Default behavior without `--serial` or `--transport` remains the existing applet transport preference order.

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
- Management preflight exception path used: no
- Alternate identity proof, if preflight skipped: not applicable
- Agent-runnable integration test allowlist: none
- Integration scope was read-only: not applicable
- Tests skipped: CLI hardware smoke and applet integration tests
- Skip reason: no CLI integration project exists; Phase 8A behavior is unit-testable through selector seams without touching hardware state
- Skip approved by: approved Phase 8A plan and consolidation ISA read-only integration rule
- Selected tests mutate persistent state: no
- User Presence / UV required: no
- Human-coordinated hardware needed: no
- Persistent state changed: no
- Destructive tests skipped completely: yes
- Reset/cleanup performed: no
- Result: unit tests and builds provide the final verification for this phase

## What Worked

- Pattern that improved readability: `YkCommandBase` parses global targeting options once, then passes concrete targeting values into `YkDeviceSelector`.
- Pattern that improved testability: `DeviceSelectorBase.FindAllDevicesAsync` is virtual, so selector filtering can be tested without hardware or static `YubiKeyManager` calls.
- Pattern that improved UX correctness: valid but unsupported transports fail before device discovery instead of silently selecting an applet's default transport.

## What Did Not Work

- Rejected approach: implement all CLI consolidation targets in one phase.
- Rejected approach rationale: secure prompt ownership, command splitting, global targeting, and interactive menus are separate risk domains.
- Helpers or abstractions that were too deep: no new service/interceptor layer was added.
- Changes that looked DRY but harmed readability: no broad parser/session helper consolidation attempted.

## House Style Updates

- Existing house-style rule confirmed: keep shared helpers shallow and justified by testability or visible behavior.
- Existing house-style rule confirmed: avoid broad CLI/helper consolidation until specific duplication has a proven target.
- Rule that needs clarification: global CLI options should be either honored or removed; parsed-but-ignored flags are misleading.
- Possible addition to `docs/SDK-HOUSE-STYLE.md`: CLI global options must have executable tests for their cross-command behavior.

## Reusable Patterns

- Pattern: virtual discovery seam on a shared selector base for hardware-free unit tests.
- Generalization class: candidate for shared promotion already accepted in `Cli.Shared`.
- Where it applies: selector behavior that depends on discovered devices but not on live applet operations.
- Where it should not apply: protocol/session methods where fake backends or fake connections already provide better seams.
- Example files: `src/Cli.Shared/src/Device/DeviceSelectorBase.cs`, `src/Cli.Commands/src/Infrastructure/YkDeviceSelector.cs`

## Core / Shared Promotion Candidates

- Candidate: `DeviceSelectorBase.FindAllDevicesAsync` hardware-free test seam
- Declared in phase ISA up front: yes, `Cli.Shared` selector support was in scope
- Should move to: accepted in `Cli.Shared`
- Evidence: `YkDeviceSelectorTests` exercise serial and transport filtering without real hardware
- Risk: low; production default still delegates to `YubiKeyManager.FindAllAsync`
- Decision: accepted
- Decision rationale: this is a shallow testing seam, not a new architecture layer
- Revisit trigger: if future CLI tests need deeper fake selection behavior than discovery injection
- Demotion/reversal needed for previous shared helper: no
- Demotion/reversal rationale: not applicable

## Cross-Module Implications

- Modules likely affected: unified CLI command library and any example CLI code that later adopts `Cli.Shared` selector behavior
- Next module should copy: narrow global-option wiring with tests before broad CLI refactors
- Next module should avoid: using the interceptor as a global state bag without a specific tested behavior
- Potential API compatibility concern: none for SDK applet packages; CLI commands are not packable SDK API

## Verification Evidence

- Branch check commands: `git status --short --branch`
- Branch check exit result: passed; branch was `yubikit-consolidation`
- RED test command: `dotnet toolchain.cs -- test --project Cli.Commands.UnitTests`
- RED test exit result: failed as expected before implementation because `YkDeviceSelector` was sealed and lacked the planned constructor/discovery seams
- Build commands: `dotnet toolchain.cs -- build --project Cli.Shared`; `dotnet toolchain.cs -- build --project Cli.Commands`; `dotnet toolchain.cs -- build --project Cli.YkTool`
- Build exit result: passed, 0 warnings, 0 errors for all three focused builds
- Unit test commands: `dotnet toolchain.cs -- test --project Cli.Commands.UnitTests`
- Unit test exit result: passed, 3/3
- Integration test commands: none
- Integration test exit result: not applicable
- Command filters/projects: `Cli.Shared`, `Cli.Commands`, `Cli.YkTool`, `Cli.Commands.UnitTests`
- Cross-module verification plan, if shared infrastructure changed: build `Cli.Shared`, `Cli.Commands`, and `Cli.YkTool`; run new CLI selector unit tests
- Results: all focused builds and tests passed; scoped formatting passed after applying format
- Manual review notes: diff limited to intended CLI selector/global-option files, new CLI unit tests, and this learning note; unrelated Core YubiKey note files remained unstaged
- Reviewer concerns resolved: no required code fixes after Cato

## Review Summary

- DevTeam engineer result: not run; single-author implementation within approved narrow scope
- DevTeam reviewer result: not run; final self-review/diff inspection completed for narrow approved scope
- Cross-vendor review result: completed; verdict `pass`, criticality `medium`, auditor `google-vertex-anthropic/claude-opus-4-8@default`
- Cross-vendor review waiver, if any: none
- Waiver approved by: not applicable
- Waiver reason and scope: not applicable
- Waiver tooling failure/unavailability evidence: not applicable
- Cato prompt/output: `/tmp/opencode/cato-phase8a-cli-audit.txt`, `/tmp/opencode/cato-phase8a-cli-audit.jsonl`
- Findings fixed: none required
- Findings deferred: redundant Management info query optimization; precise CLI usage-error exit code; pre-existing `ConnectionType` flags-value issue
- Human decisions: approved Phase 8A strict `--serial` / strict `--transport` semantics and deferral of `--interactive` plus secure credential prompts

## Cato Findings

| Severity | Finding | Disposition |
| --- | --- | --- |
| info | The `--serial` path can query Management info more than once: once during filtering, once for `DeviceSelection`, and once for command context enrichment. | Deferred. It is functionally correct, scoped, and avoids broad cache/ownership changes in this phase. Consider a later CLI/device-selection efficiency pass. |
| info | Invalid transport strings return `GenericError`, while valid-but-command-unsupported transports return `FeatureUnsupported`. | Deferred. The behavior is clear enough for Phase 8A; a future CLI UX/error-code pass can introduce a dedicated usage error if desired. |
| warning | Pre-existing `ConnectionType` is a `[Flags]` enum where `HidOtp = 3`, which overlaps `Hid | HidFido`; this phase is safe because filtering uses exact equality. | Deferred as a future Core follow-up candidate. Not introduced by this phase and not required for Phase 8A correctness. |

## Deferred Future Improvements

- Title: Review `ConnectionType` flags semantics
- Source phase: Phase 8A CLI Global Device Targeting
- Rationale: Cato identified that `ConnectionType.HidOtp = 3` overlaps `Hid | HidFido`; exact comparisons keep this phase safe, but future `HasFlag`-style filtering could regress.
- Why it is deferred: Core enum semantics are broader than CLI global-option wiring.
- Likely owning area: `Core`
- Suggested timing: final follow-up improvement pass unless a transport-filter bug appears sooner
- Needs human approval, hardware coordination, or Cato review: human approval and Cato review recommended; hardware coordination likely not required for enum/unit-test review

- Title: Avoid redundant Management info queries in CLI device selection
- Source phase: Phase 8A CLI Global Device Targeting
- Rationale: Serial filtering can query device info before `DeviceSelection` and context enrichment query again.
- Why it is deferred: Solving cleanly requires caching/selection-shape decisions beyond the approved narrow slice.
- Likely owning area: `Cli.Shared` / `Cli.Commands`
- Suggested timing: later CLI consolidation pass
- Needs human approval, hardware coordination, or Cato review: human approval recommended; hardware smoke optional

- Title: Add dedicated CLI usage-error exit code
- Source phase: Phase 8A CLI Global Device Targeting
- Rationale: Invalid option values currently return `GenericError`; scripts may benefit from a distinct usage error.
- Why it is deferred: Exit-code taxonomy is broader CLI UX policy.
- Likely owning area: `Cli.Commands`
- Suggested timing: later CLI UX/error-handling pass
- Needs human approval, hardware coordination, or Cato review: human approval recommended; no hardware needed

## Abort / Split Assessment

- Wrong branch detected: no
- Phase exceeded approved scope: no
- Public API change required: no SDK public API change; CLI internal command infrastructure changed
- Helper depth concern found: no
- Protocol flow became harder to inspect: no protocol flow changed
- Verification failed twice for different root causes: no
- Unapproved hardware coordination required: no
- Persistent-state or destructive integration required: no
- Core/shared promotion became unavoidable: no beyond approved `Cli.Shared` selector seam
- Abort learning note required: no
- Abort learning note committed with human approval: not applicable
- Outcome: continue

## Next Phase Inputs

- Required reading before next phase: `docs/SDK-HOUSE-STYLE.md`
- Required reading before next phase: `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`
- Required reading before next phase: `docs/plans/module-consolidation/ISA.md`
- Required reading before next phase: this learning note
- Pattern to apply: split CLI work into small behavior slices with unit tests before touching broad command helpers.
- Risk to watch: secure credential prompt work must not keep PINs/passwords in immutable strings longer than necessary.
- Open questions for human approval: whether Phase 8B should focus on secure credential prompts or on command parsing/session helper consolidation.

## Compact Summary

- Goal: make unified CLI `--serial` and `--transport` affect device selection
- Files changed: CLI selector base, unified command base, unified selector, new CLI unit test project, learning note
- Final pattern: command base parses global targeting; selector applies strict transport and serial filters; shared selector has a hardware-free discovery seam
- Rejected approaches: implement `--interactive`; overhaul secure prompts; broad CLI helper consolidation; interceptor state bag
- Tests passed: RED failed as expected, GREEN passed 3/3, focused builds passed, scoped formatting passed
- Integration lifecycle: skipped; no CLI integration project and no hardware-state change needed
- Shared/Core candidates: `Cli.Shared` selector seam accepted; Core `ConnectionType` flags review deferred
- Deferred future improvements: `ConnectionType` flags semantics, redundant device-info queries, CLI usage-error exit code
- House-style update needed: global CLI options should be honored or removed and covered by executable tests
- Next phase recommendation: Phase 8B secure credential prompt ownership, or Phase 8B command parsing/session helper consolidation after human choice
- Learning note path: `docs/plans/module-consolidation/phase-8a-cli-global-device-targeting-learnings.md`
- Commit: pending
- `/Ping` status: pending
