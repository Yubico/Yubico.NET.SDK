---
task: "Evaluate and (if approved) migrate device monitoring from long-lived platform listeners to a polling loop"
slug: 20260724-monitor-polling-migration
project: Yubico.NET.SDK
branch: TBD
pull_request: TBD
effort: TBD
effort_source: draft
phase: draft
progress: 0/0
mode: interactive
status: DRAFT — NOT APPROVED. No code changes are authorized from this document yet.
started: 2026-07-24
updated: 2026-07-24
---

> **This is a draft for review.** It captures the decision, the cost/benefit, the hard
> constraints, and candidate ideal-state criteria for a possible migration. It authorizes
> **no implementation**. The predecessor work (graceful listener degradation) already
> shipped on `yubikit-concurrency-fixes` and is the baseline this builds on.

## Problem

Core detects YubiKey hot-plug changes with three platform-specific, long-lived HID
device listeners (Windows `CM_Register_Notification`, macOS `IOHIDManager` + CFRunLoop
thread, Linux udev monitor + eventfd + `poll` thread) plus a PC/SC listener that is
itself a polling thread, plus a 5s interval fallback rescan. The listener stack is the
highest-risk area of the module: every monitor finding in PR #528 originated there
(thread lifetime, handle abandonment, TCC, stale-context recovery, signed-fd/EINTR).

The canonical yubikit implementations do **not** use long-lived listeners at all.
Rust (`crates/yubikit/src/platform/device.rs`) and Python (`packages/yubikit/yubikit/device.py`)
detect change by polling a stateless `scan_devices()` snapshot and diffing it; ykman's
"monitor" is literally `while True: scan_devices(); sleep(1)`. Each transport is
enumerated independently and best-effort.

After the graceful-degradation change, our listeners are already **optional latency
accelerators** rather than correctness dependencies — device truth is the full
`FindAllAsync` + repository diff. This raises the question this ISA exists to answer:
is the ~2,400 LOC of cross-platform listener machinery worth its latency benefit, or
should monitoring move to a canonical-style polling loop and delete most of it?

## Vision

Monitoring detects device changes through one cross-platform polling loop that reuses
the existing `RescanCoreAsync` → `FindAllAsync` → `YubiKeyDeviceRepository` diff, with
per-transport scan isolation and no false removals. The three event-driven HID
listeners and their bespoke native threading/handle-lifetime code are removed (or kept
only as an opt-in accelerator behind a clean seam). The module matches canonical
yubikit's model and sheds its highest-bug-density code.

## Out of Scope

- Any implementation in this document — it is a draft for a go/no-go decision.
- Changing the public `YubiKeyManager` monitoring API surface (`StartMonitoring`,
  `StopMonitoring`, `DeviceChanges`, `IsMonitoring`) — the migration must be
  behavior-compatible at the public boundary.
- Removing shared native interop used by discovery/connections (Cfgmgr32/udev/IOKit/
  CoreFoundation/SCard) — only listener-exclusive code is a removal candidate.
- The graceful-degradation change (already shipped) — this builds on it.

## Principles

- Canonical alignment: prefer the Rust/Python polling model unless a concrete platform
  requirement forbids it.
- Correctness over latency: never emit a spurious add/remove to gain responsiveness.
- Delete the risk, not just the symptom: the goal is to remove the bespoke listener
  lifecycle code, not to wrap it.
- Reversible, incremental migration: land behind a seam/flag, validate on hardware,
  then delete.

## Constraints (hard)

- **False-removal invariant (critical).** PR #528 deliberately made a failed/saturated
  PC/SC enumeration *throw* rather than return an empty list, so a failed probe is never
  committed as an empty snapshot that emits spurious removals. Canonical's naive
  "swallow the error and continue with []" is therefore **not directly portable** — a
  polling loop must distinguish "transport enumerated and is empty" from "transport
  failed to enumerate" and preserve last-known state for the failed transport.
- **Per-transport scan isolation.** `FindYubiKeys.FindAllAsync` currently enumerates
  PC/SC then HID sequentially; a PC/SC enumeration throw aborts the whole scan before
  HID. A polling design must enumerate transports independently so one transport's
  failure neither aborts the others nor produces a false diff.
- **Public API compatibility.** `IsMonitoring`, `DeviceChanges` semantics, and the
  repository-diff contract must be preserved.
- **No self-skipping resilience tests.** New coverage must use fakes/seams and run
  without hardware or PC/SC (per repo test policy).
- **Toolchain.** Build/test only via `dotnet toolchain.cs`; run `-- resilience --fast`
  for loop/lifecycle changes.

## Goal

Decide go/no-go on polling-only monitoring, and if go, define the ideal end state:
one polling loop, per-transport isolation, no false removals, listener stack removed
or reduced to an opt-in accelerator, full canonical parity, validated on all three
platforms.

## Criteria (candidate ISCs — to be finalized on approval)

- ISC-D1: A single cross-platform polling loop drives all rescans; no production code
  path depends on an event-driven HID listener for correctness.
- ISC-D2: Each transport (PC/SC, HID) is enumerated independently; one transport's
  enumeration failure does not abort or alter the diff for the others.
- ISC-D3: A failed/errored transport enumeration never produces a removal event for
  devices on that transport (false-removal invariant preserved); last-known state is
  retained until a successful enumeration supersedes it. RED test proves this against a
  naive swallow-and-empty implementation.
- ISC-D4: Detection latency is bounded by a documented, tunable interval; the default is
  chosen deliberately (candidate: 1s like ykman, or adaptive) with a recorded
  CPU/USB-wake cost measurement.
- ISC-D5: The three platform HID listeners and their listener-exclusive native
  safe-handles are removed, OR retained only behind an explicit opt-in accelerator seam
  with no correctness dependency; net managed LOC reduction is recorded.
- ISC-D6: Public monitoring API (`StartMonitoring`/`StopMonitoring`/`DeviceChanges`/
  `IsMonitoring`) behavior is unchanged; existing consumer/integration tests pass
  unmodified.
- ISC-D7: No-hardware deterministic coverage exists for the poll loop: interval cadence,
  per-transport isolation, false-removal prevention, cancellation/disposal bounds.
- ISC-D8: Hardware validation on Windows, macOS, and Linux: plug/insert and remove are
  detected within the documented latency bound; NFC tap; two-key; and a saturation/PC-SC
  failure injection shows no spurious removals.
- ISC-D9: `-- resilience --fast`, full `test`, whitespace+style format, and
  `git diff --check` all clean; cross-vendor review returns no HIGH findings.

## Cost / benefit (evidence gathered 2026-07-24)

| Dimension | Long-lived listeners (current) | Polling loop (canonical) |
|---|---|---|
| Dedicated code | ~2,400–2,665 LOC / 11 files (Win 315, macOS 504, Linux 359+427, SmartCard 483, base+hints, +2 native safe-handles) | Reuses existing `RescanCoreAsync`; ~0 net new |
| Platform surface | 3 native mechanisms + threads/handles/abandonment | 1 mechanism (snapshot+diff+sleep) |
| Correctness state | None — hints are diagnostic only; every hint triggers the same full rescan+diff | Same diff is the only path |
| Detection latency | Near-instant | Up to interval (5s today; ykman 1s) |
| Idle cost | ~0 until event | Full enumeration every interval (PC/SC + HID) |
| Bug density | Highest in module (all PR #528 monitor findings) | Low (one loop) |
| Canonical parity | Diverges | Matches Rust + Python |

Key facts: (1) listeners add zero correctness — the 5s timer already finds everything;
(2) polling already coexists (interval fallback) and the SmartCard "listener" is already
a poll loop; (3) removing listeners reclaims mostly managed LOC — native interop is
shared with discovery/connections and mostly stays.

## Decisions (to confirm on approval)

- D1: Full removal of HID listeners vs opt-in accelerator seam. Recommendation: full
  removal for maximum simplicity/canonical parity; the graceful-degradation baseline
  already means nothing breaks if event-driven hints disappear.
- D2: Poll interval and cadence (fixed vs adaptive/backoff). Recommendation: start with
  a fixed 1s foreground interval (ykman parity), measure, then consider adaptive backoff
  when idle.
- D3: Where per-transport isolation + last-known-state retention lives (`FindYubiKeys`
  vs `YubiKeyDeviceRepository.UpdateCache`). This is the core engineering task and the
  main risk; it must preserve the false-removal invariant.

## Risks

- Latency regression for consumers expecting near-instant detection (mitigate via
  interval choice / adaptive cadence; document).
- Increased idle CPU / USB wakeups / power draw from periodic enumeration (measure;
  consider backoff when no subscribers or no recent change).
- macOS PC/SC zero-timeout status calls can block behind in-flight transactions; a
  tighter poll cadence may surface this more often (validate on hardware).
- Reintroducing false removals if per-transport isolation is done naively (guarded by
  ISC-D3 RED test).
- Losing the hard-won correctness of the current listeners if partially removed; prefer
  clean full removal over half-measures.

## Changelog

- 2026-07-24: Draft created after graceful-degradation shipped (`e8284828`) and Copilot
  CLI (gpt-5.5) cross-vendor review flagged scan-layer per-transport isolation as the
  remaining canonical gap. Cost/benefit and the false-removal constraint recorded.
