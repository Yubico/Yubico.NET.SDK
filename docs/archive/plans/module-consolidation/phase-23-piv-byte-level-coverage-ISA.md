# Phase 23 ISA: PIV Byte-Level Coverage

This ISA governs Phase 23 of the module-consolidation quality-convergence program.

Read this together with:

- `docs/plans/module-consolidation/ISA.md`
- `docs/plans/module-consolidation/phase-20-quality-convergence-before-composite-yubikey-ISA.md`
- `docs/plans/module-consolidation/phase-22-tests-shared-recorder-harness-decision-learnings.md`
- `src/Piv/README.md`
- `src/Piv/CLAUDE.md`
- `src/Tests.Shared/README.md`

## Problem

Phase 20 identified PIV as a module that needs stronger byte-level proof before composite YubiKey design starts. Phase 22 promoted a shared SmartCard recorder specifically so applet modules can add focused APDU/TLV unit coverage without adding new private fakes.

Current PIV unit coverage already checks initialization, metadata, object GET, and touch notification command flow. The highest remaining value is focused coverage around key and crypto operation encodings, where subtle TLV, slot, policy, algorithm, padding, or sensitive-buffer mistakes can regress behavior while still looking structurally correct. The module also still has a few root-level files, such as `src/Piv/src/PivMetadata.cs`, whose types may now belong closer to feature-specific code if the move improves locality without changing public namespaces or hiding protocol flow.

## Vision

PIV should keep its flat public-session rhythm while gaining source-level confidence that high-risk key and crypto operations emit the expected APDUs. The work should feel like the Phase 22 recorder was promoted for exactly this: small tests, visible bytes, no new abstraction layer. Where source files are clearly feature-local, the tree should become easier to navigate by moving those files only when the move preserves namespace/API shape and does not create confusing folder/namespace mismatches.

## Out of Scope

- No composite YubiKey design or implementation.
- No PIV public API changes unless a source-backed bug requires them and the user approves.
- No broad PIV refactor or helper extraction for style alone.
- No operation-specific command classes or command-like protocol executors.
- No APDU DSL, fake protocol framework, or assertion layer that hides raw command bytes.
- No file moves that change public namespaces, public type names, package identity, or API compatibility.
- No root-to-folder file moves when the target folder's namespace convention conflicts with the file's public namespace, unless the phase explicitly documents why a mixed-namespace folder is clearer than leaving the file in place. Namespace preservation wins over folder locality.
- No PIV integration exclusions: the user explicitly approved running all PIV integration tests for this phase, including tests that reset or mutate the PIV applet on the beta key.

## Principles

- Tests prove protocol bytes; they should keep command shape inspectable.
- Add the fewest tests that materially improve confidence in high-risk PIV paths.
- Prefer asserting APDU class, instruction, P1/P2, and key TLV tags over duplicating full implementation logic.
- Sensitive buffer handling remains a first-class verification concern where touched.
- Existing PIV source flow is preserved unless a failing test exposes a real defect.
- File moves are acceptable only when they improve feature locality without changing namespace/API shape or creating architectural symmetry for its own sake.

## Constraints

- Execute on branch `yubikit-consolidation`.
- Use `dotnet toolchain.cs ...`; never raw `dotnet build` or `dotnet test`.
- PIV module guidance requires reading `src/Piv/README.md` and `src/Piv/CLAUDE.md` before source changes.
- Source edits require this Phase 23 ISA to be approved first.
- `RecordingSmartCardConnection` from `Tests.Shared` is the preferred fake connection for new byte-level SmartCard unit tests.
- The user approved running all PIV integration tests in Phase 23. Treat this as explicit approval for persistent-state PIV reset/key/certificate/PIN choreography on the YubiKey 5.8 beta test key.
- Run Management preflight before PIV integration commands and record the beta key identity/firmware evidence in the learning note.
- The approved PIV integration command intentionally omits `--smoke`, so `Slow`-categorized PIV integration tests run. Cato verified the current PIV integration suite has no `RequiresUserPresence`, touch-policy, or `PermanentDeviceState` markers.
- This non-smoke full-module PIV integration run is the explicit exception allowed by the master ISA's rule that an open-ended module integration run is allowed only when the human explicitly approves a full-module integration run.
- Non-smoke PIV integration commands must run with an explicit shell timeout. Use 20 minutes as the default bound; timeout is a verification failure that must be root-caused before continuing.
- Slow markers are expected in the current PIV integration suite and are intentionally included by the non-smoke run; the marker re-scan must record both the absence of unattended UP/touch markers and the presence of any Slow markers.
- Reset expectation: the full PIV integration suite owns the PIV applet state for this phase, may reset or mutate it, and does not promise a reusable post-suite PIV state. Later PIV work must perform its own reset/preflight before relying on PIV defaults.
- Cross-phase state expectation: Phase 23 creates no guarantee for later phases' hardware state. Phase 24 through Phase 32 must use their own module-specific preflight/setup and must not infer readiness from Phase 23's final PIV applet state.
- Baseline comparability expectation: before each full PIV integration baseline, run the same PIV reset setup command as a state-preparation step, then run the identical full non-smoke PIV integration command as the measured baseline. The reset setup may be a subset of the full suite; it is not counted as the baseline result.
- Confirm the beta key allow-list configuration before Management preflight; the pass condition is that serial `103` appears in `YubiKeyTests:AllowedSerialNumbers` in `src/Tests.Shared/appsettings.json` or an explicitly documented active override config. If the allow-list is missing or does not include the beta serial, stop before running integration tests.
- Because `src/Tests.Shared/appsettings.json` currently allows unknown serials, Management preflight must record connected/selected serial evidence and prove the target is serial `103` before PIV integration runs.
- Phase 23 adds unit tests and may move source files only; it does not add new PIV integration tests, so the pre/post full integration command scope remains comparable.
- Run Cato review on this plan and the master consolidation ISA before continuing to source implementation.
- Stage only intended Phase 23 files.

## Goal

Add focused PIV unit coverage for high-risk key and crypto APDU/TLV encodings using the shared `RecordingSmartCardConnection`, evaluate feature-local source file moves and perform only those that preserve namespace/API clarity, run all approved PIV integration tests, and preserve public API shape plus flat protocol flow.

## Criteria

- [ ] ISC-1: Branch check shows `## yubikit-consolidation` before implementation, review, verification, or delegation.
- [ ] ISC-2: Phase 23 ISA exists and defines PIV byte-level coverage scope.
- [ ] ISC-3: PIV README and CLAUDE guidance were read before source edits.
- [ ] ISC-4: Phase 22 learning note is used as input for recorder usage.
- [ ] ISC-5: New or updated unit tests use `RecordingSmartCardConnection` rather than a new private SmartCard recorder.
- [ ] ISC-6: Generate-key coverage verifies `INS 0x47`, slot P2, algorithm TLV `0x80`, and optional PIN/touch policy TLVs where applicable.
- [ ] ISC-7: Sign/decrypt coverage verifies `INS 0x87`, algorithm P1, slot P2, template tag `0x7C`, expected response tag `0x82`, and challenge tag `0x81`.
- [ ] ISC-8: Calculate-secret coverage verifies `INS 0x87`, algorithm P1, slot P2, template tag `0x7C`, expected response tag `0x82`, and peer-public-key tag `0x85`.
- [ ] ISC-9: At least one new or updated unit test covers non-default PIV policy encoding with `RecordingSmartCardConnection`.
- [ ] ISC-10: At least one test covers an error response path that preserves the public exception shape without hiding APDU bytes.
- [ ] ISC-11: Tests assert only source-backed command details and do not duplicate complete implementation encoders.
- [ ] ISC-12: Tests avoid real PINs, PUKs, management keys, private keys, or persistent applet mutations.
- [ ] ISC-13: Any source fix, if needed, is minimal and directly explained by a failing test.
- [ ] ISC-14: No operation-specific command classes or command-like protocol executors are introduced.
- [ ] ISC-15: No broad APDU DSL, fake protocol framework, or broad assertion helper is introduced.
- [ ] ISC-16: Any PIV file move preserves the `Yubico.YubiKit.Piv` namespace, public type names, and package API shape.
- [ ] ISC-17: Any PIV file move has source-backed locality rationale and explicitly handles folder namespace convention. `PivMetadata.cs` must not move into `src/Piv/src/Metadata/` if doing so creates an unjustified mismatch between the root public namespace and the existing `Yubico.YubiKit.Piv.Metadata` folder convention.
- [ ] ISC-18: Cato review runs against this Phase 23 ISA and `docs/plans/module-consolidation/ISA.md`, and material planning findings are resolved before source implementation continues.
- [ ] ISC-18.1: Beta key allow-list configuration is confirmed before Management preflight; serial `103` must appear in `YubiKeyTests:AllowedSerialNumbers` in `src/Tests.Shared/appsettings.json` or an explicitly documented active override config, and missing or mismatched allow-list blocks integration.
- [ ] ISC-18.2: Before the non-smoke PIV integration run, PIV integration sources are re-scanned for `RequiresUserPresence`, touch-policy waits, `PermanentDeviceState`, manual prompts, and `Slow` markers; any newly discovered unattended touch/user-presence requirement blocks the non-smoke run until the phase ISA is revised, and any `Slow` markers are recorded as intentionally included.
- [ ] ISC-18.3: Management preflight records the actual bound device serial returned by Management `GetDeviceInfoAsync` and confirms the integration target is serial `103`; if target selection is ambiguous because unknown serials are allowed, stop before PIV integration.
- [ ] ISC-19: Management preflight runs before PIV integration tests and records beta-key identity/firmware evidence.
- [ ] ISC-19.1: Before both pre-implementation and post-implementation full PIV integration baselines, the same PIV reset setup command is run as belt-and-suspenders state preparation with `dotnet toolchain.cs -- test --integration --project Piv --filter "FullyQualifiedName~PivResetTests"` or an equivalent approved reset setup command recorded in the learning note. This setup command is not the measured baseline; it only establishes comparable starting state, even if many individual PIV integration tests also reset internally.
- [ ] ISC-19.2: Phase 23 does not add new PIV integration tests; any pressure to add integration tests requires a revised phase ISA before implementation continues.
- [ ] ISC-20: `dotnet toolchain.cs -- build --project Piv` succeeds.
- [ ] ISC-21: `dotnet toolchain.cs -- test --project Piv` succeeds so new byte-level unit tests are included regardless of class name.
- [ ] ISC-22: Pre-implementation full approved PIV integration baseline `dotnet toolchain.cs -- test --integration --project Piv` succeeds before source changes. This command intentionally omits `--smoke` so `Slow` PIV integration tests run. A failing pre-baseline blocks implementation unless the user explicitly approves a known-failing-baseline waiver with comparison semantics.
- [ ] ISC-22.1: Post-implementation full approved PIV integration baseline uses the identical command `dotnet toolchain.cs -- test --integration --project Piv` after the same clean PIV reset setup and succeeds. Any post-baseline failure stops the phase for root cause and user decision; the phase does not complete on an agent-deferred post-baseline failure.
- [ ] ISC-22.2: Non-smoke PIV integration commands run with an explicit 20-minute timeout; timeout is treated as verification failure, not as a skipped or deferred pass.
- [ ] ISC-23: `dotnet toolchain.cs -- docs-qa` succeeds if docs/plans artifacts are changed.
- [ ] ISC-24: `git diff --check` succeeds.
- [ ] ISC-25: DevTeam review runs and material findings are resolved or explicitly deferred.
- [ ] ISC-26: Learning note records changed files, Cato evidence, DevTeam review evidence, verification evidence, integration result, file-move rationale, and Phase 24 recommendation.
- [ ] ISC-27: Commit contains only intended Phase 23 files.
- [ ] ISC-27.1: Compact summary is produced after commit and before Phase 24 begins.
- [ ] ISC-27.2: `/Ping` is sent only after implementation, review, verification, learning note, commit, and compact summary are complete.
- [ ] ISC-28: Anti: Phase 23 changes hardware allow-list, `[WithYubiKey]`, lazy binding, or global integration-test coordination policy beyond the approved master ISA update.
- [ ] ISC-29: Anti: Phase 23 claims FIDO/FIDO2/WebAuthn User Presence behavior was verified by unattended tests.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | branch | Check current branch | `## yubikit-consolidation` | `git status --short --branch` |
| ISC-2 | file | Read Phase 23 ISA | title and scope present | Read |
| ISC-3 | file | Read PIV docs | README/CLAUDE loaded | Read |
| ISC-4 | file | Read Phase 22 learning note | recorder decision referenced | Read |
| ISC-5 | source | Grep new tests | shared recorder used, no private duplicate | Grep |
| ISC-6 | unit | Generate-key APDU assertions | bytes verified | test output |
| ISC-7 | unit | Sign/decrypt APDU assertions | bytes verified | test output |
| ISC-8 | unit | Calculate-secret APDU assertions | bytes verified | test output |
| ISC-9 | unit | Non-default policy path | one unit test covers it with recorder | test output/read |
| ISC-10 | unit | Error response path | exception shape verified | test output |
| ISC-11 | source | Inspect test assertions | no full encoder duplicate | Read/diff |
| ISC-12 | source | Inspect test data | no real secrets or mutations | Read/diff |
| ISC-13 | source | Inspect source diff | minimal fix only if needed | diff |
| ISC-14 | source | Grep command-like types | no forbidden classes | Grep |
| ISC-15 | source | Inspect helper additions | no DSL/framework | diff |
| ISC-16 | source/API | Inspect moved files and public API diff | namespace/API unchanged | diff/build |
| ISC-17 | source | Inspect file-move rationale | locality documented | learning note/read |
| ISC-18 | review | Cato plan review | pass or resolved findings | Cato output |
| ISC-18.1 | integration preflight | Confirm beta key allow-list | serial 103 present in `src/Tests.Shared/appsettings.json` or documented override, else stop | read/bash |
| ISC-18.2 | integration preflight | Re-scan PIV integration sources for unattended touch/UP markers and Slow markers | no UP/touch markers; Slow markers recorded | Grep |
| ISC-18.3 | integration preflight | Record actual bound Management device serial despite `AllowUnknownSerials` | bound serial 103 proven or stop | bash/test output |
| ISC-19 | integration | Management preflight | beta identity/firmware recorded | bash/test output |
| ISC-19.1 | integration setup | Run identical PIV reset setup before each full baseline as state preparation, not as measured baseline | reset setup succeeds or stop | bash |
| ISC-19.2 | scope | Confirm no new PIV integration tests were added | no new integration test files/methods | diff/Grep |
| ISC-20 | build | Build PIV | exit 0 | bash |
| ISC-21 | tests | Full PIV unit tests | exit 0 | bash |
| ISC-22 | integration | Pre-implementation full approved PIV integration baseline, intentionally non-smoke | exit 0, or user-approved known-failing-baseline waiver | bash |
| ISC-22.1 | integration | Post-implementation full approved PIV integration baseline, intentionally non-smoke and identical command to pre-baseline after clean reset setup | exit 0 or stop for user decision | bash |
| ISC-22.2 | integration | Non-smoke PIV integration timeout bound | 20-minute timeout used; timeout fails | bash |
| ISC-23 | docs | Docs QA | exit 0 | bash |
| ISC-24 | whitespace | Diff check | exit 0 | bash |
| ISC-25 | review | DevTeam review | pass or resolved findings | review output |
| ISC-26 | learning | Read learning note | evidence present | Read |
| ISC-27 | git | Inspect staged files | intended Phase 23 only | `git status`, `git diff --cached --name-only` |
| ISC-27.1 | handoff | Produce compact summary | summary present | response |
| ISC-27.2 | handoff | Send `/Ping` after complete phase | ping only after compact | response/tool |
| ISC-28 | anti | Inspect Tests.Shared/integration policy diff | no unapproved global policy change | diff |
| ISC-29 | anti | Learning note/read logs | no false FIDO UP claim | Read |

## Features

| Feature | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Phase 23 setup | Create ISA and confirm branch/docs/Phase 22 inputs. | ISC-1, ISC-2, ISC-3, ISC-4 | none | false |
| Key-operation APDU tests | Add focused generated-key and policy encoding assertions. | ISC-5, ISC-6, ISC-9, ISC-11, ISC-12 | setup | false |
| Crypto APDU tests | Add focused sign/decrypt and calculate-secret command assertions. | ISC-5, ISC-7, ISC-8, ISC-10, ISC-11, ISC-12 | setup | false |
| Feature-local file moves | Evaluate root-level PIV files for feature-folder moves, and move only where locality clearly improves and namespace conventions do not conflict. | ISC-16, ISC-17 | setup | false |
| PIV integration baseline | Confirm allow-list, prove target serial 103 despite unknown-serial allowance, re-scan for unattended UP/touch markers, run Management preflight, establish clean PIV state before each baseline, keep integration scope unchanged, run pre-implementation full PIV integration baseline, and run post-implementation full PIV integration baseline with the identical timed command. | ISC-18.1, ISC-18.2, ISC-18.3, ISC-19, ISC-19.1, ISC-19.2, ISC-22, ISC-22.1, ISC-22.2 | setup/tests complete | false |
| Safety boundary | Keep global hardware policy unchanged except the approved master ISA integration clarification. | ISC-28, ISC-29 | setup | false |
| Review and verification | Run Cato plan review, DevTeam review, focused build/tests, integration, docs QA, whitespace, learning, commit, compact summary, and `/Ping`. | ISC-18, ISC-20, ISC-21, ISC-22, ISC-22.1, ISC-23, ISC-24, ISC-25, ISC-26, ISC-27, ISC-27.1, ISC-27.2 | tests complete | false |

## Decisions

- 2026-06-08: The user approved running all PIV integration tests for this phase, including tests that reset or mutate the PIV applet on the beta test key.
- 2026-06-08: Use Phase 22's `RecordingSmartCardConnection`; do not add another private recorder or broader APDU fake.
- 2026-06-08: Evaluate feature-local PIV files such as `PivMetadata.cs` for feature-folder moves, but move only when locality improves without changing public namespaces or creating confusing folder/namespace mismatches.
- 2026-06-08: Run Cato on the revised Phase 23 plan and master consolidation ISA before source implementation continues.
- 2026-06-08: The approved full PIV integration command intentionally omits `--smoke`; this includes `Slow` PIV integration tests and is allowed because the user approved all PIV integration tests.
- 2026-06-08: Cato found that `PivMetadata.cs` uses root namespace `Yubico.YubiKit.Piv` while the existing `Metadata/` folder uses `Yubico.YubiKit.Piv.Metadata`; namespace preservation takes precedence, so this move is a candidate to evaluate, not a mandate.
- 2026-06-08: A red pre-implementation PIV integration baseline blocks source implementation unless the user approves a known-failing-baseline waiver with explicit post-change comparison semantics.
- 2026-06-08: Any unresolved post-implementation PIV integration failure stops Phase 23 for user decision; completion requires either a passing post-baseline or a user-approved ISA revision.
- 2026-06-08: Pre- and post-implementation PIV integration baselines must use the identical non-smoke full-module command; any unresolved post-baseline failure sign-off must say whether it reproduces after clean PIV reset/preflight.
- 2026-06-08: Cato found destructive suite state can weaken baseline comparison; Phase 23 now requires the same clean PIV reset setup before both full integration baselines and does not complete on an agent-deferred post-baseline failure.
- 2026-06-08: The beta key allow-list source for this phase is `src/Tests.Shared/appsettings.json` unless an active override config is explicitly documented; serial `103` is the required allowed serial.
- 2026-06-08: Phase 23 does not establish reusable hardware state for later phases; every later phase owns its own hardware preflight/setup.
- 2026-06-08: Because `AllowUnknownSerials` is currently true, Management preflight must record selected serial evidence and prove serial `103` is the integration target.
- 2026-06-08: Non-smoke PIV integration commands use an explicit 20-minute timeout; timeout is a failed verification requiring root cause.
- 2026-06-08: Phase 23 does not add new PIV integration tests; integration scope remains comparable between pre- and post-baselines.
- 2026-06-08: Non-default policy byte coverage is mandatory in unit tests, not a conditional best-effort criterion.

## Verification

Verification is populated in `docs/plans/module-consolidation/phase-23-piv-byte-level-coverage-learnings.md` as the phase executes.
