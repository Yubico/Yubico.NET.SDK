# Phase 2: OTP HID Ready-To-Write Polling

## What We Found

- The OTP HID ready-to-write path had already produced a large benchmark win by avoiding sleep-first polling.
- The existing unit guard was useful, but it needed clearer resilience intent and proof that it catches the seeded regression.

## How We Found It

- BenchmarkDotNet showed `CreateManagementSessionOverOtpHid` improved from about 1.039s to about 28ms and `GetDeviceInfoOverOtpHid` from about 2.075s to about 56ms.
- We inspected `AwaitReadyToWriteAsync` and the fake HID test path. The fake path writes exactly ten feature reports for an empty payload frame.

## What Got Better

- Renamed the test to `SendAndReceiveAsync_WhenReadyToWriteImmediately_DoesNotSleepBeforePolling`.
- Added `Category=RuntimeResilience` so the gate can be selected with the rest of the resilience suite.
- Added `SentReports.Count == 10` so the timing budget is tied to a known fake workload shape.
- Red-green proved the guard: temporarily adding a 50ms sleep before each write-readiness poll failed the focused test at 526ms; restoring the implementation made it pass.

## What Worked

- A loose no-hardware unit budget was enough for this specific regression because the old write-side sleep-first behavior adds at least 500ms across ten writes.
- The timing check is not a microbenchmark; it is a seeded-regression tripwire with large headroom.

## What Did Not Work

- Fake call counts alone cannot catch this regression because the old and new write paths read and write the same number of HID reports.
- BenchmarkDotNet is good evidence for the optimization, but it is too expensive and environment-dependent as the default unit-test gate.

## Review Findings And Fixes

- DevTeam noted the plan overclaimed proof by arithmetic. We performed a red-green check and recorded the failure/pass evidence.
- DevTeam noted the class summary was stale. We updated it to describe behavior and runtime-resilience invariants.
- DevTeam noted the guard specifically covers write-readiness sleep-first regression, not every possible read-side polling regression. The plan now scopes the evidence accordingly.

## Remaining Risk

- The guard is intentionally tied to `AwaitReadyToWriteAsync`, not a general detector for all OTP read-side sleeps.
- If future refactors change frame chunking, the `SentReports.Count == 10` assertion will force the test to be revisited.

## Verification

- Focused OTP resilience test: 1 passed, 0 failed, 0 skipped after restoration.
- Temporary regression check: focused OTP test failed at 526ms with the injected 50ms sleep-first delay.
- Core gate: 522 total, 520 succeeded, 2 expected hardware skips, 0 failed after all phase work.
- Format verification: clean except existing `Tests.TestProject` IL2026/IL3050 warnings.
