# Deferred multi-key capacity and scheduling work

Status: retained design reference after the single-key-first scope change on 2026-09-22.
This is a later-iteration backlog, not an implementation requirement for iteration 1.

The complete, independently reviewed [earlier program design](earlier-multi-key-program-design.md)
is archived. The original [architecture](earlier-multi-key-architecture.md) and the
[master plan](../../../../2026-09-21-yubikit-async-boundaries-ISA.md) retain their evidence
and decisions. No findings, stable acceptance identifiers, or prior approvals have
been erased; their applicability to the reduced iteration is being revalidated.

## Why defer this work

First establish a predictable operation/lifetime model for one physical YubiKey:
open, initialize, operate, cancel, drain/recover, dispose, and reopen only when safe.
Cross-key capacity policy should build on that demonstrated model instead of forcing
a process-wide scheduler and admission framework into the first implementation.

The earlier program-design review remains useful evidence about that proposed design.
It is not approval of the design, measurements of its limits, or a requirement to
implement every proposed type before a single-key path works.

## Preserved proposals for a later iteration

| Topic | Earlier proposal / finding to retain |
|---|---|
| Shared native execution | Four interactive workers and four discovery workers, each with sixteen queued jobs. These were unmeasured calibration values. |
| Whole metadata workflows | Four admitted asynchronous probes and sixteen waiting probes, distinct from native-call execution. |
| Cross-key cleanup progress | Two cleanup workers so one stalled cleanup still leaves progress for another key; no worker slot reclaimed before native return. |
| Pre-acquisition cleanup credits | Eight interactive and eight discovery reservations, one coalesced release obligation each; maximum sixteen ready entries, including late/quarantined owners. |
| Capacity consequences | Eight retained interactive obligations prevent a ninth such native open; persistent failures can exhaust the process's finite credits. This must be presented honestly and calibrated. |
| Priority and origin isolation | Discovery cannot consume interactive reservations; direct raw factories need an explicit origin and share the stated capacity rather than an invented separate pool. |
| Cross-key saturation tests | One blocked key while healthy keys operate; multiple occupied cleanup workers; discovery saturation under concurrent interactive work; full queued/admission bounds. |
| Multi-device performance | Simultaneous K1–K3 workloads, resource scaling, fairness, throughput, idle behavior and finite failure-tolerance measurements. |
| Scheduling-specific public errors | Exact resource-capacity exception and reservation plumbing should be revisited with the later topology rather than imposed on iteration 1. |

Do not silently reduce these numbers to one and retain the entire framework. The
single-key architecture should earn its mechanisms from its own required behaviors.
A later architecture review decides whether to reuse, simplify or replace these proposals.

## Safety that must remain in iteration 1

- Existing one-live-connection/session ownership and refusal of overlapping logical
  exchanges on the selected key. No queueing of overlapping public applet operations.
- Cancellation before dispatch, cancellation after dispatch, terminal native completion,
  retained borrowed/native memory, and exactly-once outcome/cleanup.
- Same-key discovery versus connection ownership, disposal while work is pending,
  callback teardown, generation-safe removal/replug, and safe sequential reuse.
- No replay of uncertain mutations. A fault or cancellation is not evidence of native
  release. Root unresolved resources and make the connection's unusable state visible.
- No unbounded operation queue or replacement threads created around a hung call on
  the selected key. A bounded inbound report buffer still needs an overflow policy.
- Existing public raw sessions, typed raw operations and consumer implementations.
  Breaking changes must retain consistent applet contracts.
- Production-adapter responsiveness probes, relevant native/runtime evidence and
  preserved protocol/ownership/prompt/zeroing regressions.

The old proposed 256-report receive buffer is a **per-connection** question, not a
multi-key scheduling requirement. Its need and limit must be re-evaluated for the
single-key native input path rather than automatically deferred or accepted.

## Acceptance and existing users

The master retains all 72 epic criteria. In particular, discovery/interactive reserved
capacity (ISC-23), process-wide capacity/saturation facets of ISC-21/22/25, scaling
measurements in ISC-62, and the associated complete-production closure in ISC-64 are
not discharged by demonstrating a single key. Their single-operation lifetime/boundedness
parts still apply where relevant. The later gates must map iteration-1 evidence to
specific rows without checking a universal epic criterion prematurely.

Same-key overlap refusal (ISC-14), recovery ownership (ISC-15), callback isolation
(ISC-26), report overflow (ISC-27), old-generation isolation (ISC-28), and independent
monitor/connection context ownership (ISC-39) are not deferred merely because their
wording involves concurrency or multiple contexts.

Iteration 1 is a delivery/verification scope, not a new global one-device restriction.
Preserve current enumeration and existing multi-device behavior; do not introduce a
process-wide single-key lock or knowingly break other attached keys. The iteration
does not claim newly verified fairness, throughput or failure isolation between keys.

## Fixtures and restart point

The prior operating-system observation found three Yubico `1050:0407` devices (K1–K3);
their descriptor values are preserved in the earlier program design. Use one selected
key at a time for iteration 1. The other keys can later provide sequential compatibility
coverage; simultaneous three-key stress belongs here. Actual firmware and capabilities
still need verification before device scenarios run.

Resume this backlog after the single-key lifecycle is demonstrated, or by explicit
scope approval. Revisit the topology and capacities using measured evidence; do not
treat this addendum as authority to add shared pools during the first iteration.

ISC — ideal state criterion, the stable acceptance identifiers in the master plan.
