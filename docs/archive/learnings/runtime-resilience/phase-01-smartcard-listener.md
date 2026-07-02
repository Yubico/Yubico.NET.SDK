# Phase 1: SmartCard Listener Recovery

## What We Found

- `DesktopSmartCardDeviceListener` ignored native PC/SC return values in listener paths.
- Persistent `SCardGetStatusChange` or `SCardListReaders` failures could create retry storms without an explicit backoff.
- The original `Stop()` shape could hold `_syncLock` while joining a listener thread that also needed the same lock during recovery.

## How We Found It

- We traced the SmartCard listener loop after the BenchmarkDotNet pass showed no remaining large foreground performance target.
- Cato pushed the plan toward both `SCardGetStatusChange` and `SCardListReaders`, bounded recovery, deterministic no-hardware tests, and safe `_context` handling.
- A temporary pre-fix worktree proved the old shape could exceed 100 immediate `SCardGetStatusChange` calls in 500ms under simulated `SCARD_E_INVALID_HANDLE`.

## What Got Better

- Added an internal `ISCardApi` seam so native PC/SC failures can be injected without PC/SC services or YubiKey hardware.
- Added fake sleeper handshakes to prove the listener enters backoff before retry or recovery.
- Added `Category=RuntimeResilience` to make this gate targetable while keeping it in default test runs.

## What Worked

- No-hardware fault injection was faster and more reliable than trying to reproduce native service failure live.
- Cross-vendor review caught important concurrency and semantic gaps before commit.
- Reducing the invariant to “every native failure loop must back off, exit, or recover” made the tests specific and stable.

## What Did Not Work

- A broad diagnostics harness would have been premature; the first useful slice was just a seam and focused unit tests.
- Raw metrics alone were less useful than a hard behavioral invariant.

## Review Findings And Fixes

- DevTeam found a potential `Stop()`/`Join()` lock stall. We moved thread join outside the lock.
- DevTeam found the recovery-success path was untested. We added a test proving backoff, context re-establishment, baseline rebuild, and resumed status monitoring.
- DevTeam flagged that `SCARD_E_SERVICE_STOPPED` and `SCARD_E_NO_SERVICE` changed from terminal to recoverable after stale contexts. We documented that intent in the plan.

## Remaining Risk

- Recovery is deliberately bounded but can keep trying once per second if context establishment succeeds while later calls fail.
- `Status` remains a simple property written from multiple threads. Existing behavior is preserved, but stronger memory semantics could be considered separately.

## Verification

- Focused SmartCard resilience tests: 4 passed, 0 failed, 0 skipped.
- Core gate: 522 total, 520 succeeded, 2 expected hardware skips, 0 failed after all phase work.
- Format verification: clean except existing `Tests.TestProject` IL2026/IL3050 warnings.
