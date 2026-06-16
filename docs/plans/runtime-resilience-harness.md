# Runtime Resilience Harness Plan

## Purpose

Create a local-first workflow that catches real SDK production risks before they ship: native listener spin loops, sleep-first polling regressions, handle/file-descriptor leaks, recovery retry storms, and foreground operation performance regressions.

The harness must prove value incrementally. Each slice must catch a known bug class or produce a concrete new finding before we build the next layer.

## Operating Principles

- Prefer hard pass/fail invariants over dashboards of raw metrics.
- Prefer no-hardware deterministic tests before live-hardware diagnostics.
- Prefer existing test and benchmark projects before creating new infrastructure.
- Promote to a dedicated diagnostics project only after reuse across at least three modules or bug classes is proven.
- Do not add public SDK APIs solely for diagnostics.
- Do not make destructive live YubiKey operations part of the default path.
- Keep the fast local path under 90 seconds.

## Phase Closeout Gate

Before starting the next phase:

- Write a learning document under `docs/learnings/runtime-resilience/` covering what we found, how we got better at finding it, what worked, what failed, what review changed, and what remains risky.
- Run focused verification for the phase and the broader Core gate when code changed.
- Run cross-vendor review when the phase changes runtime behavior, proof quality, or scanner logic.
- Commit the phase as a logical work area before moving on.
- Do not let later-phase exploration accumulate uncommitted changes on top of an unclosed phase.

## Cato Corrections

- Split the plan into two tracks: CI-friendly no-hardware gates and local-only live-hardware diagnostics.
- Wire no-hardware fault-injection tests into ordinary CI eventually; do not let lack of hardware CI block those gates.
- Start from invariants, then collect only metrics needed to prove them.
- Avoid absolute live-hardware CPU/memory/latency thresholds unless they are baseline-relative and stable.
- Prioritize handle/file-descriptor leak invariants because they are high-signal and low-noise.
- Defer a new diagnostics project and reusable skill until the smaller harness catches enough real issues to justify them.

## Known Bug Seeds

- SmartCard listener stale-context spin: persistent `SCARD_E_INVALID_HANDLE` from `SCardGetStatusChange` could trigger rapid calls.
- OTP HID sleep-first polling: fixed 50 ms delay before readiness checks caused second-level management calls.

## Phase 0: Prove The SmartCard Bug Class

- [x] Add a temporary pre-fix diagnostic proving old listener behavior can exceed 100 immediate `SCardGetStatusChange` calls within 500 ms when `SCARD_E_INVALID_HANDLE` returns immediately.
- [x] Add no-hardware SmartCard fault-injection tests using an internal SCard seam.
- [x] Cover persistent `SCardGetStatusChange` invalid-handle behavior.
- [x] Cover persistent `SCardListReaders` invalid-handle behavior.
- [x] Cover failed context re-establishment so recovery cannot spin on `SCardEstablishContext`.
- [x] Run Cato on the SmartCard listener plan and incorporate concerns.
- [x] Run DevTeam cross-vendor review and incorporate actionable findings.
- [x] Verify focused SmartCard listener tests pass.
- [x] Verify Core tests pass without user-presence tests.

Evidence:
- Pre-fix temp worktree diagnostic passed: old listener exceeded 100 `SCardGetStatusChange` calls within 500 ms under simulated invalid-handle returns.
- Focused regression tests passed: `DesktopSmartCardDeviceListenerSCardErrorTests`, 3 total, 3 succeeded.
- Core tests passed: 514 total, 512 succeeded, 2 skipped.
- Core build passed: 3 Core projects, 0 warnings, 0 errors.
- Format verification passed with only existing `Tests.TestProject` IL2026/IL3050 warnings.

## Phase 1: Make The First Gate CI-Friendly

- [x] Keep the SmartCard no-hardware fault-injection tests in the normal Core unit test path.
- [x] Confirm they do not self-skip when PC/SC is unavailable.
- [x] Add comments or documentation explaining the regression invariant: persistent native failures must be backoff-bounded.
- [x] Add a checklist item for future listener/native-loop PRs: every native-loop error path must block, back off, exit, or throttle.
- [x] Decide whether to tag these tests with a dedicated trait such as `RuntimeResilience` without excluding them from default unit test runs.

Evidence:
- `DesktopSmartCardDeviceListenerSCardErrorTests` uses fake `ISCardApi` and fake sleeper handshakes, so it does not depend on PC/SC, HID, live YubiKeys, or platform services.
- Tests are tagged `Category=RuntimeResilience` for targeted execution, but they remain normal Core unit tests and are not excluded by default.
- `AGENTS.md` and `src/Core/CLAUDE.md` now document the listener/native-loop invariant for future PR review.
- `SCARD_E_SERVICE_STOPPED` and `SCARD_E_NO_SERVICE` are intentionally recoverable after an established context becomes stale; failed re-establishment still transitions the listener to `Error`.

Proof of value required before moving on:
- Reverting the SmartCard listener fix must make the no-hardware tests fail or the pre-fix diagnostic pass as a bug detector.

## Phase 2: Catch The OTP Sleep-First Bug Class

- [x] Add a regression invariant for OTP HID ready-to-write polling.
- [x] Decide the best detector: existing unit timing assertion, BenchmarkDotNet baseline comparison, or a deterministic fake-call-count test.
- [x] Avoid fragile absolute wall-clock thresholds where call-count or fake sleeper assertions can prove the same behavior.
- [x] Record before/after evidence from the existing BenchmarkDotNet run: ~1.039 s to ~28 ms and ~2.075 s to ~56 ms.

Decision:
- Use the no-hardware unit timing guard in `OtpHidProtocolTests.SendAndReceiveAsync_WhenReadyToWriteImmediately_DoesNotSleepBeforePolling` and tag it `Category=RuntimeResilience`.
- Do not use BenchmarkDotNet as the default regression gate; it is evidence for the optimization, but too expensive and environmental for every unit-test run.
- Do not use fake call count as the primary detector; the sleep-first regression preserves the same HID report count and only changes when the first read occurs.
- The 200ms budget is intentionally loose relative to the old 10 x 50ms minimum write-side delay, so it catches the seeded regression without being a microbenchmark.

Evidence:
- Existing BenchmarkDotNet evidence: `CreateManagementSessionOverOtpHid` improved from ~1.039 s to ~28 ms, and `GetDeviceInfoOverOtpHid` improved from ~2.075 s to ~56 ms.
- Reintroducing a 50ms sleep before each ready-to-write poll across the 10 frame reports in the fake unit path would add at least 500ms and violate the 200ms runtime-resilience budget.
- Red-green proof: temporarily reintroducing the 50ms sleep-first ready-to-write delay made the focused test fail at 526ms; restoring the implementation made it pass again.

Proof of value required before moving on:
- Reintroducing sleep-first polling must fail a test or exceed a stored benchmark budget. Satisfied by the red-green no-hardware unit budget above.

## Phase 3: Static Native-Loop Screening

- [x] Prototype a lightweight scanner for high-risk loop shapes.
- [x] Flag ignored native return values inside loops.
- [x] Flag `continue` paths after native failures without sleep/backoff/exit.
- [x] Flag `catch` plus retry loops without sleep/backoff/exit.
- [x] Flag fixed sleeps in protocol polling paths for manual review.
- [x] Keep output small: file, line, risk category, reason.

Decision:
- Keep the prototype as Core no-hardware unit tests for now, not a separate diagnostics project or toolchain target.
- Seeded-source tests prove the scanner flags the historical SmartCard ignored-native-result loop shape and the OTP ready-to-write sleep-before-poll shape.
- A current-source scan over `src/Core/src/Protocols` and `src/Core/src/Transports` is a smoke gate only; explicit safe negative fixtures carry the false-positive proof for now.
- The native-failure detector intentionally uses a small line-window heuristic at prototype stage; promote to block-aware scanning only if Phase 5/6 turns this into a durable runner.

Evidence:
- `RuntimeResilienceStaticScanTests.Scanner_FlagsIgnoredNativeStatusChangeResultInsideLoop` catches the old SmartCard-style ignored `SCardGetStatusChange` result inside a listener loop.
- `RuntimeResilienceStaticScanTests.Scanner_FlagsNativeFailureContinueWithoutBackoff` catches a native failure path that immediately continues the loop.
- `RuntimeResilienceStaticScanTests.Scanner_DoesNotFlagNativeFailureContinueAfterHandler` proves the matching safe handler/break/continue shape stays quiet.
- `RuntimeResilienceStaticScanTests.Scanner_FlagsCatchRetryWithoutBackoff` catches a catch/retry loop with no visible backoff or exit.
- `RuntimeResilienceStaticScanTests.Scanner_DoesNotFlagCatchRetryWithBackoff` proves the matching catch/backoff/continue shape stays quiet.
- `RuntimeResilienceStaticScanTests.Scanner_FlagsSleepBeforeReadyToWritePoll` catches a sleep-first `AwaitReadyToWriteAsync` shape.
- `RuntimeResilienceStaticScanTests.Scanner_CurrentCoreSource_HasNoFindings` verifies current Core Protocols/Transports source roots exist, cover files, and currently produce zero scanner findings.

Proof of value required before moving on:
- Scanner must flag at least the old SmartCard shape or the OTP sleep-first pattern with low false-positive volume. Satisfied at prototype level by seeded positive fixtures, paired safe negative fixtures, and a non-empty current-source smoke scan.

## Phase 4: Handle And File Descriptor Leak Invariant

- [x] Define a cross-platform metric for handles/file descriptors where possible.
- [x] Add a small no-hardware fake test if live handle counts are too platform-specific.
- [ ] Add a live optional diagnostic for repeated connect/disconnect/listener start/stop cycles.
- [x] Assert handles/fds return to baseline within a strict tolerance.

Decision:
- Use fake `SCardContext` release counts as the Phase 4 cross-platform metric. OS handle/fd counts are platform-specific and too noisy for the default no-hardware gate.
- Defer live optional diagnostics until there is a runner in Phase 5 or a second handle/fd class to exercise.
- Treat `ReleasedContextCalls == EstablishContextCalls` after restart/dispose as the strict no-hardware baseline-return invariant.

Evidence:
- `WhenListenerRestarts_PreviousContextsAreDisposed` initially failed with 2 contexts established and only 1 released.
- `StopListening()` now disposes the stopped context only after the listener thread has joined; if join times out, it intentionally leaves the context alive to avoid disposing a handle the background thread may still use.
- After the fix, focused SmartCard tests passed: 5 total, 5 succeeded, 0 skipped.
- Core tests passed: 523 total, 521 succeeded, 2 skipped.
- DevTeam review returned `pass` and confirmed the leak-vs-use-after-free tradeoff.

Proof of value required before moving on:
- A deliberately leaked connection/listener handle must be detected locally. Satisfied by the red-green fake-context release invariant above.

## Phase 5: Minimal Local Runner

- [x] Add a single toolchain entry point only after Phases 1-4 prove useful.
- [x] Suggested shape: `dotnet toolchain.cs -- resilience --fast`.
- [x] Fast mode should run no-hardware resilience tests, static scanner, and selected benchmark budget checks.
- [x] Output should be pass/fail with paths to evidence, not a dashboard.
- [x] Keep the fast path under 90 seconds.

Decision:
- Add `resilience` as a `toolchain.cs` target instead of a separate diagnostics project.
- Require `--fast` because only the no-hardware fast mode exists today.
- Implement the runner by executing Core unit tests tagged `Category=RuntimeResilience`.
- Do not add BenchmarkDotNet to the default runner; the OTP unit timing guard is the fast budget check, and BenchmarkDotNet remains supporting evidence.

Evidence:
- `dotnet toolchain.cs -- resilience --fast` passed 13 runtime-resilience tests in 3.2s after final hardening.
- Running `dotnet toolchain.cs -- resilience` fails immediately with guidance because `--fast` is required.
- A temporary OTP sleep-first regression made the runner fail with both scanner and OTP timing failures.
- A temporary SmartCard context leak regression made the runner fail with the context-release invariant.
- DevTeam review returned `pass`; we hardened the target afterward by restoring the captured `testFilter` in `finally` and making `--fast` required rather than advisory.

Proof of value required before moving on:
- One command catches at least the SmartCard and OTP seeded regressions. Satisfied by the red-green runner checks above.

## Phase 6: Dedicated Diagnostics Project, Only If Justified

- [x] Evaluate promotion gate for `diagnostics/Yubico.YubiKit.RuntimeDiagnostics`.
- [ ] Create `diagnostics/Yubico.YubiKit.RuntimeDiagnostics` only if the same runner patterns are reused across at least three modules or bug classes.
- [ ] Add scenario registry only after multiple scenarios exist.
- [ ] Add JSON/markdown reports only for asserted invariants.
- [ ] Record device serial, model, firmware, and transport for every live result.
- [ ] Default to read-only, non-destructive operations.

Decision:
- Do not create `diagnostics/Yubico.YubiKit.RuntimeDiagnostics` yet.
- Current gates cover multiple bug classes, but they are all no-hardware Core unit-test gates and already run through `dotnet toolchain.cs -- resilience --fast`.
- There is no live-hardware scenario registry yet, no repeated cross-module runner pattern, and no asserted JSON/markdown report that would add value over test output.
- Reopen this phase after a live OS-handle/fd diagnostic is approved, at least one live optional diagnostic exists, or another module-specific runtime-resilience gate needs orchestration outside unit tests.

Evidence:
- The fast runner already catches the known SmartCard and OTP seeded regressions in under 90 seconds.
- No default live scenario is approved yet, so a diagnostics project would currently be empty orchestration.
- Keeping diagnostics deferred follows the Phase Closeout Gate and avoids building shelfware.
- DevTeam review validated the defer decision and corrected the checklist representation so deferred deliverables remain unchecked.

Do not build yet:
- Cross-module all-applet orchestration.
- General CPU/memory/GC graphing without invariants.
- Live-hardware CI integration.
- A reusable local audit skill.

## Phase 7: Reusable Local Audit Skill, Last

- [x] Evaluate promotion gate for a reusable `yubikit-runtime-audit` skill.
- [ ] Create a `yubikit-runtime-audit` skill only after the toolchain/local runner shape stabilizes.
- [ ] The skill should orchestrate existing commands, not invent parallel behavior.
- [ ] It should produce a concise report and recommend next probes.

Decision:
- Do not create a `yubikit-runtime-audit` skill yet.
- The stable user interface is currently one command: `dotnet toolchain.cs -- resilience --fast`.
- A skill would mostly wrap that single command and restate the plan; it would not add meaningful orchestration until diagnostics or multiple runner modes exist.
- Reopen this phase after Phase 6 reopens, a diagnostics project exists, a live optional diagnostic exists, or a multi-command audit workflow exists.

Evidence:
- The command-line harness is already useful without a skill.
- Phase 6 deferred the diagnostics project, so there is no scenario registry or report layer for a skill to orchestrate.
- Avoiding the skill follows the same anti-shelfware rule used for the diagnostics project.
- DevTeam review validated the defer decision and found no concrete skill capability that adds value beyond the single runner command today.

Proof of value required:
- The command-line harness is already useful without the skill. Satisfied by Phase 5 red-green runner evidence.

## Live-Hardware Safety Rules

- [ ] Default live scenarios must be read-only.
- [ ] Mutating scenarios require an explicit opt-in flag.
- [ ] PIN, PUK, credential, reset, delete, and key-generation scenarios require a disposable test key declaration.
- [ ] Any test requiring touch/user presence must be marked and excluded from fast mode.
- [ ] Results must label hardware coverage as single-device unless a matrix was run.

## Definition Of Useful

A slice is useful only if it does at least one of these:

- Catches a known seeded regression.
- Finds a new bug or suspicious runtime behavior.
- Blocks a realistic class of production regression with a stable invariant.
- Reduces manual investigation time with a clear pass/fail report.

If a slice only produces metrics that nobody acts on, it is not useful and should be cut.
