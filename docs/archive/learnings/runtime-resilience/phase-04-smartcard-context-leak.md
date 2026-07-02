# Phase 4: SmartCard Context Leak Invariant

## What We Found

- Repeated `Start(); Stop(); Start(); Stop(); Dispose();` on `DesktopSmartCardDeviceListener` established two PC/SC contexts but released only one.
- The previous context remained assigned after `Stop()` and was overwritten by the next `Start()`.
- This was a real no-hardware resource leak in the listener lifecycle, not just a hypothetical OS handle metric.

## How We Found It

- We inspected the Phase 1 listener lifecycle while looking for a cross-platform handle/fd invariant.
- Live OS handle counts were rejected for the default gate because they are platform-specific and noisy.
- A fake `SCardContext` release counter gave a deterministic substitute for “handles return to baseline.”

## What Got Better

- Added `WhenListenerRestarts_PreviousContextsAreDisposed` to assert fake context releases equal fake context establishments after restart and dispose.
- Fixed `StopListening()` to dispose the stopped context after the listener thread exits.
- Preserved safety on join timeout: if the listener thread does not exit, the context is intentionally not disposed to avoid use-after-free on the background thread.

## What Worked

- Red-green proof was immediate: the new invariant failed with 2 established and 1 released, then passed after the disposal fix.
- Counting fake context releases was more stable than reading live process handle/fd counts.
- DevTeam review validated the race safety and the “bounded leak beats use-after-free” decision on join timeout.

## What Did Not Work

- A generic live fd/handle diagnostic would have been premature without a runner and without a second resource class to compare.
- The fake invariant does not cover every lifecycle edge, such as a permanently wedged listener thread.

## Review Findings And Fixes

- DevTeam returned `pass` with no blocking findings.
- DevTeam suggested documenting the join-timeout branch. We added a comment explaining that the listener may still be using the native context, so the code prefers a bounded leak over disposing an active handle.

## Remaining Risk

- The join-timeout path intentionally leaks one context rather than risking use-after-free.
- Recovery-swap behavior is covered by Phase 1 tests, but not by a release-count stress loop.
- Live fd/handle diagnostics remain deferred until Phase 5 gives us a runner shape.

## Verification

- Focused SmartCard resilience tests: 5 passed, 0 failed, 0 skipped.
- Core gate: 523 total, 521 succeeded, 2 expected hardware skips, 0 failed.
- Format verification: clean except existing `Tests.TestProject` IL2026/IL3050 warnings.
