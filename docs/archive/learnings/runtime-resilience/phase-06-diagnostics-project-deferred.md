# Phase 6: Diagnostics Project Deferred

## What We Found

- The fast runner is useful without a separate diagnostics project.
- Current resilience gates cover several bug classes, but they all live naturally as Core no-hardware unit tests.
- A diagnostics project would not yet add a new invariant, runner pattern, live scenario, or report that changes decisions.

## How We Got Better At Finding

- The previous phases improved detection by adding focused tests and a single toolchain target, not by adding infrastructure.
- The Phase 5 runner gave us the orchestration needed for the current gates.
- The Phase 6 decision gate prevented us from mistaking “we could build a project” for “a project is useful.”

## What Worked

- The plan’s promotion rule worked: build diagnostics only after reuse across modules or live scenarios justifies it.
- Existing unit-test output already gives the pass/fail signal we need.
- The `Category=RuntimeResilience` trait is enough scenario selection for now.

## What Did Not Work

- A scenario registry would be empty or duplicate test names today.
- JSON/markdown diagnostics reports would repeat test output without adding an asserted invariant.
- Live-hardware diagnostics are still undefined and should not be invented just to populate a project.

## Review Findings And Fixes

- DevTeam validated the defer decision but found the initial checklist representation misleading because deferred deliverables were marked complete.
- We changed Phase 6 to mark only the promotion-gate evaluation complete and leave the deferred diagnostics-project deliverables unchecked.
- DevTeam identified live OS-handle/fd diagnostics as the leading reopen trigger, so the plan now names that explicitly.

## Remaining Risk

- Deferring diagnostics means live fd/handle checks remain manual until a real live scenario is defined.
- The fast runner is Core-only. If another module adds runtime-resilience gates, the runner may need to widen before a diagnostics project is still justified.

## Verification

- Phase 5 runner evidence remains the verification substrate: `dotnet toolchain.cs -- resilience --fast` passed 13 runtime-resilience tests in under 90 seconds.
- No new code was added in this phase.
