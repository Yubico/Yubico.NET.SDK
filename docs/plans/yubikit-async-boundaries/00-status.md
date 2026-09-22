# Status: YubiKit async boundaries

- Gate 1 — Product: APPROVED 2026-09-22 for the revised single-key-first scope.
- Gate 2 — Architecture: APPROVED 2026-09-22 for the revised single-key design.
- First smart-card lifetime slice: implemented and verified 2026-09-22; code review PASS WITH NOTES.
- Next checkpoint: present this slice's evidence and choose the next bounded implementation step.
- Whole-effort program/slice specification: deferred to implementation effort at the user's request.

## Authority and approvals

The [master acceptance plan](../../../2026-09-21-yubikit-async-boundaries-ISA.md)
owns the 72 stable acceptance criteria, detailed requirements, evidence, and decisions.
This file records approved direction, slice checkpoints and implementation progress.
Gate documents are review views of the master plan, not independent specifications or
replacement acceptance registries. Reconcile an approved change into the master plan
before dispatching affected work.

The user approved the earlier broader Gates 1 and 2 on 2026-09-22, then requested a
single-key first iteration while retaining the multi-key planning. Reopen the affected
gates rather than treating the earlier approvals as authority for a different scope.
Gate 3 was positively received with this scope correction, not unconditionally approved.
The user approved revised Gates 1 and 2, then explicitly requested incremental delivery
instead of waterfall specification of the whole effort. Review first-slice pseudocode,
then implement with tests, independent review and a consistency check. Non-blocking
details are **deferred to implementation effort** and handled live. Do not impose a
separate whole-epic Gate 3/Gate 4 approval before that slice. Material public-contract,
architecture or safety changes still require discussion before implementation.

## Product review

- [Gate 1 product](01-product.md): approved for one selected key at a time on 2026-09-22.
- Already approved: breaking v2 changes are permitted if equivalent public applet
  contracts remain consistent; unnecessary public implementation types may become
  internal (master decision D15).
- Already approved: raw sessions and raw connection operations remain public,
  including consumer connection implementations and caller-owned connection reuse
  (master decision D16).
- Non-blocking exact signatures, helper/seam layout and measurement details are deferred
  to implementation effort. The approved behavior, public tiers and safety rules remain binding.

## Architecture review

- [Revised Gate 2 architecture](02-architecture.md): approved after independent
  review. It selects a connection-lifetime owner, one lazy worker for blocking native
  calls, no ordinary-operation backlog, and coalesced transaction-end/shutdown intents.
  Native completion/readiness and explicit macOS IOKit expansion remain.
- The complete [earlier architecture](addenda/earlier-multi-key-architecture.md) is
  archived. Shared pools, credits and cross-key progress guarantees stay deferred.
- Retain the current between-read FIDO cancellation sequence; the proposal imposes no
  new simultaneous-send/read obligation on external raw-connection implementations.
- Native lifetime, safe release, strong rooting, late-completion handling, visible
  unrecovered state and synchronous-disposal wait-graph requirements still apply to
  one key. Spare-worker guarantees for other keys and shared reservation topology do not.

## First-slice pseudocode

- [First-slice pseudocode](03-program-design.md) proposes the existing smart-card
  open/request/close lifetime for one key, with necessary transaction-release coordination
  and a small set of controlled lifecycle tests. The user approved it on 2026-09-22.
- The full [earlier program design](addenda/earlier-multi-key-program-design.md) is archived.
  Its review and findings remain useful; it is not the current implementation checklist.
- [Multi-key addendum](addenda/multi-key-capacity.md) records deferred scheduling,
  capacity, cross-key isolation, and measurement work. Do not implement it during the
  first iteration merely because the earlier review passed.
- The user allowed any attached key for the read-only check. The test selected the
  authorized 5.7.4 USB-C key (K1), verified real metadata, and passed three connection
  cycles with two transaction/read cycles each. Other keys were not acceptance fixtures.
- Review corrected pre-open lease/reservation plumbing, internal smart-card visibility,
  native-release versus cleanup-error results, finite cleanup saturation, finalizer roots,
  and discovery quarantine waking connection waiters without releasing unsafe ownership.
- Reuse relevant findings without inheriting shared pools, credits or unnecessary types.
  Resolve routine details against failing tests and surrounding conventions during the slice.

## Incremental delivery

Retain public applet/raw access, nonblocking native execution, teardown release evidence,
and tests beneath production adapters. Re-slice around one key's complete lifecycle
instead of building the earlier shared-capacity foundation first.

The first slice is implemented with 38 passing boundary tests, Core 1,335 passed/3
existing skipped, PublicApi 22 passed and resilience 77 passed. Its hardware test passed
one selected key; macOS arm64 native ahead-of-time publish/discovery smoke passed.
Independent cross-vendor review finished PASS WITH NOTES after three rounds. The
broader S0/S0b/S1–S6/X1–X3 graph remains reference; do not dispatch it unchanged or finish
its detailed specification before starting the approved slice. Subsequent details are
deferred to implementation effort with evidence mapped to the master as work lands.
Known limits: unproven native close errors retain ownership until process exit; Windows/
Linux and interruption/touch hardware scenarios remain unverified. The hardware run's
discovery initialization skipped the non-selected 5.4.3 key on an unresolved HID read;
recorded for follow-up, not silently treated as multi-key success. Reviewer notes on
diagnostic detail, the internal thread-start test seam, child-kill diagnostics, and
centralizing provider/slot resolution are deferred to implementation effort.
The S0 intelligence pass has source/artifact findings and existing-test results;
S0 is not complete and the master still has 0/72 criteria checked.

## Notes for a fresh session

- Worktree: `/Users/Dennis.Dyall/Code/y/worktrees/yubikit-async-boundaries`.
- Branch: `yubikit-async-boundaries`; last fetched base on 2026-09-22:
  `a7f2cae8c32ad6e0ada55e404f85442f6a266f6c`, matching `origin/yubikit`.
  Refresh before further work and revalidate affected evidence if the base changes.
- One architect/orchestrator owns planning, inventory acceptance, measurements,
  integration, and master acceptance. Engineers own implementation; shared-file
  ownership and handoffs are explicit. Final cross-applet/Core consistency is mandatory.
- The user wants to know before decisions on new code, architecture, or seams.
  Existing proposals in the master plan are not blanket approval.
- Initial iteration scope is one selected physical key at a time; the prior simultaneous
  three-key target is deferred. Exact supported system
  floors, verified firmware, other platform/reader fixtures, frozen numerical budgets,
  and native package-producing provenance remain open. Preserve evidence grades;
  managed test passes and binary inspection do not establish hardware behavior.
- The first smart-card lifetime iteration is the checkpoint for this implementation
  and its planning/evidence documents. The user authorized committing it together.
- Before the next implementation slice, review what this smart-card iteration teaches
  about HID ownership, cancellation, completion, and teardown; reconcile the master
  design with those lessons and obtain approval for the next slice.
- Read the current scope/architecture, first-slice pseudocode and master decisions;
  resume the pending slice checkpoint under the user's incremental workflow. Do not
  restart whole-effort gates or repeat settled D15/D16 choices.
- Before replacing a retained broader gate document with its reduced-scope successor,
  archive the complete earlier version and update the addendum's reference links.
- Revised Gate 2's independent review passed after clarifying bounded transaction-release
  intent, per-route abort limits, borrowed-buffer ownership and synchronous reentrancy.
  This is proposal review only; no native/hardware or implementation evidence was added.
