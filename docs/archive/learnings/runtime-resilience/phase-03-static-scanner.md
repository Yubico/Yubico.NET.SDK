# Phase 3: Static Runtime-Resilience Scanner

## What We Found

- A small static scanner can catch several high-risk runtime-resilience shapes without building a dedicated diagnostics project.
- The useful prototype shapes are ignored native results inside loops, native failure `continue` paths without visible backoff/exit/handler, catch/retry loops without backoff, and sleep-before-ready-to-write polling.

## How We Found It

- We started with seeded source fixtures instead of scanning the entire repository blindly.
- We added a current-source smoke check only after positive fixtures existed.
- DevTeam review exposed that green scanner tests can be misleading unless paired safe-negative fixtures prove the detector can distinguish safe from unsafe shapes.

## What Got Better

- Added `RuntimeResilienceStaticScanTests` with positive fixtures for all four target shapes.
- Added paired safe negative fixtures for native failure handling and catch retry with backoff.
- Added a non-empty current-source scan over Core Protocols and Transports so source-root moves or empty scans fail loudly.
- Documented that the current-source scan is a smoke gate only, not proof of comprehensive false-positive quality.

## What Worked

- Keeping the scanner test-local avoided premature toolchain or diagnostics infrastructure.
- Positive fixtures plus paired negative fixtures gave better proof quality than scanning live source alone.
- Cross-vendor review was especially useful for test-meaningfulness, not just implementation correctness.

## What Did Not Work

- The first scanner version overclaimed proof from a current-source zero-finding test.
- The first native-failure detector lacked a safe negative fixture, so it proved only that a regex fired, not that it distinguished safe handler paths.
- Raw line/brace scanning is brittle. It is acceptable for a prototype but not a durable analyzer.

## Review Findings And Fixes

- DevTeam found the current-source zero-finding test was near-vacuous. We added source-root existence and non-empty file-count guards and softened the plan language.
- DevTeam found the native-failure detector lacked a paired safe-negative fixture. We added `Scanner_DoesNotFlagNativeFailureContinueAfterHandler`.
- Follow-up review passed and called out one remaining prototype limitation: the native-failure detector uses a small line-window heuristic instead of block-aware analysis. We documented that limitation in the plan.

## Remaining Risk

- The scanner is line-window based and can miss long failure blocks or over-flag unrelated nearby native calls.
- The sleep-before-poll detector is currently name-coupled to `AwaitReadyToWriteAsync`.
- Promotion to a real runner should use block-aware scanning or Roslyn-style syntax analysis if this proves useful across more phases.

## Verification

- Focused scanner suite: 7 passed, 0 failed, 0 skipped.
- Core gate: 522 total, 520 succeeded, 2 expected hardware skips, 0 failed.
- Format verification: clean except existing `Tests.TestProject` IL2026/IL3050 warnings.
