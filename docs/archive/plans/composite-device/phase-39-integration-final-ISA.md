# Phase 39 ISA: Integration, Docs, Migration, And Final Verification

This is the **final phase** of the composite-device program. It does not add new product behavior; it
documents the physical-device model for consumers (master ISC-29), runs the safe hardware smoke that proves
discovery and typed connection opening end-to-end (master ISC-28), runs the final verification gate and
Cato program audit (master ISC-30), and reconciles the master ISA — checking every criterion (ISC-1..27,
ISC-31..32) against evidence from the completed phases and populating the master Verification section.

Per the owner's direction, Phase 39 uses a **light ISA + one final-program Cato audit** (no multi-round
pre-edit Cato gate, because there is no API-surface source change), documentation lives in a **new
architecture doc plus Core README/CLAUDE updates**, and the final review runs as an **interim Cato now with
the GPT-5.5 final Cato and the backlog DevTeam reviews queued**.

Read this together with:

- `docs/plans/composite-device/ISA.md` (master — ISC-28, ISC-29, ISC-30; full criterion list to reconcile)
- All phase learnings notes 33 through 38.5 in `docs/plans/composite-device/` (the reconciliation evidence)
- `src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Core/CompositeDiscoveryIntegrationTests.cs` (ISC-28 smoke)
- `src/Core/CLAUDE.md`, `src/Core/README.md` (ISC-29 doc targets)

## Problem

Phases 33–38.5 implemented and verified the composite-device model (Core-owned read-only metadata,
physical `IYubiKey`, PID-based composite discovery, applet smart defaults + overrides + held-transport
fallback), each with its own ISA, learnings note, and review evidence. But two things remain before the
program can be called done:

1. **No active consumer documentation** explains the physical-device model. `docs-qa` validates root docs,
   `docs/{,usage,troubleshooting,architecture}`, and every `src/**/README.md` + `CLAUDE.md`, but none of
   those currently describe one-`IYubiKey`-per-physical-device semantics, Core metadata ownership, the
   per-applet smart defaults/overrides/fallback, or how a v1 per-interface-handle caller migrates.
2. **The master ISA is unreconciled.** Only the Phase 38/38.5 criteria (ISC-21..24, ISC-23.1) are checked;
   ISC-1..20 and ISC-25..32 are still unchecked even though their phases are complete, and the master
   Verification section is a placeholder. The master ISA cannot be declared complete without an
   evidence-backed pass and the final gate (docs QA, focused tests, safe hardware smoke, DevTeam review,
   Cato final audit — master ISC-30).

## Vision

A consumer can read one architecture document (and the Core README/CLAUDE) and understand: an `IYubiKey`
is one physical device exposing one or more interfaces; how to discover it, query its read-only metadata,
check supported connections, and open a typed connection; what transport each applet session picks by
default, how to override it, and when it falls back; and how to migrate code written against the old
per-interface-handle model. The master ISA is fully reconciled: every criterion is checked with a phase
citation that is true at HEAD, the Verification section records the final evidence, and the final Cato
program audit has run (interim now, GPT-5.5 queued). No earlier-phase behavior is changed.

## Out of Scope

- Any new product behavior or public API change. Phase 39 is documentation, verification, and reconciliation
  only. (Doc-comment clarifications are allowed; signature/behavior changes are not.)
- The deferred downstream `DeviceInfo`-promotion capability audit (master ISC-31): it stays recorded and
  deferred; Phase 39 confirms it was recorded and not implemented early (ISC-32), and implements none of it.
- HID-held fallback, SCP-implied transport selection, and the YubiOtp `Xunit.SkippableFact` harness fix —
  all carried-over deferred candidates; not addressed here.
- Clearing the queued GPT-5.5 reviews — they remain queued until quota returns; Phase 39 runs the interim
  Cato and records the queue.

## Principles

- **Reconciliation is verification, not formatting.** Each master criterion is re-checked against evidence
  that holds at HEAD (build/test results, structural greps, hardware smoke, learnings notes) before its box
  is checked. Master ISC-30 explicitly forbids claiming readiness without the gates.
- **Honesty over completeness.** If the final audit finds a real gap in an earlier phase, Phase 39 stops and
  surfaces it (possibly a small follow-up phase) rather than checking the box.
- **Docs are for consumers, not a changelog.** The architecture doc explains the model and migration, not a
  phase-by-phase history (that lives in the plan/learnings).
- **No source churn.** Production source stays as Phase 38.5 left it; only docs, the master ISA, and Phase 39
  artifacts change.

## Constraints

- Execute on branch `yubikit-composite-device-new`.
- Use `dotnet toolchain.cs`; never raw `dotnet build`/`dotnet test`.
- Hardware smoke uses serial 103 (composite OTP+FIDO+CCID); free the CCID with `gpgconf --kill scdaemon`
  first; **NEVER run `gpg --card-status`**. `src/Tests.Shared/appsettings.json` already authorizes serial
  103; no allow-list edit.
- New/updated docs must pass `docs-qa` (valid code fences, no known-stale patterns, valid local links). A new
  doc under `docs/architecture/` is auto-discovered by `docs-qa`.
- The final program review is an interim Cato (GPT-5.4, read-only) via `scripts/interim-cross-vendor-review.sh`;
  the GPT-5.5 final Cato and the backlog DevTeam reviews (Phases 35, 36, 37, 37.5, 38, 38.5) are queued.
- Commit only intended files; never `git add .`/`-A`/`commit -a`. Do not introduce a `Core` -> `Management`
  dependency.

## Goal

Write a consumer-facing physical-device architecture doc and update the Core README/CLAUDE (ISC-29); run the
safe composite-discovery + typed-connect hardware smoke on serial 103 (ISC-28); run the final verification
gate — full build, focused unit suites for all modules, docs QA, format/diff, and structural-invariant greps;
reconcile every master criterion (ISC-1..27, ISC-31..32) with an evidence-backed phase citation and populate
the master Verification section; run the interim Cato final program audit and queue GPT-5.5 + backlog
DevTeam reviews; write the Phase 39 learning note; and commit only the intended docs/ISA/learnings files.

## Criteria

### Governance

- [ ] ISC-1: Branch check shows `## yubikit-composite-device-new` before edits and commit.
- [ ] ISC-2: This Phase 39 ISA exists before the integration/docs/reconciliation work begins.
- [ ] ISC-3: The final program review is run (interim Cato) and its output recorded; the GPT-5.5 final Cato and the backlog DevTeam reviews are recorded as queued.

### Documentation (master ISC-29)

- [ ] ISC-4: A new active doc `docs/architecture/physical-device-model.md` exists and explains: (a) physical `IYubiKey` semantics (one device, one or more interfaces; `AvailableConnections` + `SupportsConnection`); (b) read-only metadata ownership (`DeviceInfo`, `FormFactor`, `DeviceCapabilities`, `DeviceFlags`, `VersionQualifier`, `VersionQualifierType` in Core; mutating configuration/reset/lock/mode in Management); (c) typed `ConnectAsync<TConnection>()` routing and the ambiguity-throwing parameterless connect; (d) per-applet smart defaults, explicit `preferredConnection` overrides, and held-transport fallback; (e) migration from the v1 per-interface-handle model.
- [ ] ISC-5: `src/Core/README.md` and `src/Core/CLAUDE.md` are updated so their connection/`IYubiKey` guidance reflects the physical-device model (not a per-interface handle) and link the new architecture doc; no stale per-interface framing remains in the connection-semantics sections.
- [ ] ISC-6: `docs-qa` validates all active documentation (including the new doc); changed-file formatting and `git diff --check` are clean.

### Safe Hardware Smoke (master ISC-28)

- [ ] ISC-7: With the CCID free, the Core composite-discovery integration smoke on serial 103 confirms (a) `FindAllAsync(ConnectionType.All)` returns ONE logical device for the physical key, (b) per-connection filters return the same physical device, and (c) typed `ConnectAsync<ISmartCardConnection>()`, `<IFidoHidConnection>()`, and `<IOtpHidConnection>()` each open on the merged device — all without UP/UV/touch. Evidence (commands + results) is recorded in the learning note; if the smoke cannot run, a recorded skip rationale is provided.

### Final Verification Gate (master ISC-30)

- [ ] ISC-8: Full solution build passes with 0 warnings / 0 errors.
- [ ] ISC-9: Focused unit suites pass for Core, Management, YubiOtp, Fido2, WebAuthn (and a sanity run of the single-transport applets Piv/Oath/OpenPgp/SecurityDomain/YubiHsm).
- [ ] ISC-10: Structural invariants verified by grep/inspection at HEAD: no `Core` -> `Management` ProjectReference; no production code routes on a scalar `IYubiKey.ConnectionType`; applet modules do not reference `Yubico.YubiKit.Management` solely for physical-device metadata.
- [ ] ISC-11: `dotnet format --verify-no-changes` is clean on changed files and `git diff --check` is clean.

### Master ISA Reconciliation

- [ ] ISC-12: Every master criterion ISC-1 through ISC-27 and ISC-31 through ISC-32 is re-verified against evidence true at HEAD and checked with a phase citation; any criterion that cannot be substantiated is left unchecked and surfaced as a finding (not rubber-stamped).
- [ ] ISC-13: The master ISA Verification section is populated with the final evidence (docs QA result, build/test summary, hardware smoke evidence or skip rationale, review status), replacing the placeholder.
- [ ] ISC-14: The deferred downstream `DeviceInfo`-promotion capability audit (master ISC-31) is confirmed recorded (Phase 33/34 docs) and confirmed not implemented early (master ISC-32); none of it is implemented in Phase 39.

### Closeout

- [ ] ISC-15: The Phase 39 learning note records the docs added, the hardware smoke evidence, the final gate results, the reconciliation outcome, the interim Cato result, and the queued GPT-5.5/DevTeam follow-ups, and marks the composite-device program complete (GPT-5.5 reviews queued).
- [ ] ISC-16: Commit includes only intended files (new architecture doc, Core README/CLAUDE, master ISA, this Phase 39 ISA, Phase 39 learnings); no production source behavior change; no `git add .`/`-A`.

### Anti-Criteria

- [ ] ISC-17: Anti: a master criterion is checked without evidence true at HEAD.
- [ ] ISC-18: Anti: Phase 39 changes production behavior or public API, or implements any deferred downstream capability.
- [ ] ISC-19: Anti: final completion is claimed without docs QA, focused tests, safe hardware smoke (or skip rationale), and the (interim) Cato audit.

## Test Strategy

| ISC | Type | Check | Threshold | Tool |
| --- | --- | --- | --- | --- |
| ISC-1 | branch | Active branch | `## yubikit-composite-device-new` | `git status --short --branch` |
| ISC-2 | design | This ISA present before work | present | Read |
| ISC-3 | review | Interim Cato program audit; GPT-5.5 + DevTeam queued | pass/resolved; queue recorded | `scripts/interim-cross-vendor-review.sh` |
| ISC-4, ISC-5 | docs | New architecture doc + Core README/CLAUDE updated | content present, links resolve | Read |
| ISC-6 | docs | docs-qa, format, whitespace | exit 0 / clean | `dotnet toolchain.cs -- docs-qa`; `dotnet format --verify-no-changes`; `git diff --check` |
| ISC-7 | integration | Composite discovery + typed connect smoke (serial 103) | one device, typed connects succeed, no UP/UV | `dotnet toolchain.cs -- test --integration --project Core --smoke --filter ...` |
| ISC-8 | build | Full build | 0 warn / 0 err | `dotnet toolchain.cs build` |
| ISC-9 | unit | Focused unit suites all modules | pass | `dotnet toolchain.cs -- test --project <M> --filter ...` |
| ISC-10 | dependency/grep | No Core->Mgmt ref; no scalar ConnectionType routing; no applet->Mgmt metadata coupling | clean | Grep/Read |
| ISC-11 | format | Format + whitespace | clean | `dotnet format --verify-no-changes`; `git diff --check` |
| ISC-12, ISC-13 | reconcile | Master criteria checked w/ citations; Verification populated | all substantiated checked; placeholder replaced | Read/Edit |
| ISC-14 | scope | Deferred audit recorded, not implemented early | note present; no early impl | Read/Git diff |
| ISC-15, ISC-16 | file | Learning note; intended-files-only commit | present; clean staging | Read/git status |
| ISC-17 to ISC-19 | anti | No unsubstantiated check; no behavior change; no premature completion | enforced | Read/Git diff/review |

## Features

| Name | Description | Satisfies | Depends On | Parallelizable |
| --- | --- | --- | --- | --- |
| Phase 39 ISA | Write this ISA. | ISC-1, ISC-2 | Phase 38.5 | false |
| Consumer docs | New `docs/architecture/physical-device-model.md` + Core README/CLAUDE updates. | ISC-4, ISC-5, ISC-6 | Phase 39 ISA | false |
| Hardware smoke | Composite discovery + typed connect smoke on serial 103. | ISC-7 | Phase 39 ISA | true |
| Final gate | Build, unit suites, structural greps, format/diff. | ISC-8, ISC-9, ISC-10, ISC-11 | docs + smoke | false |
| Reconcile + audit | Check master ISC-1..27/31..32, populate Verification, interim Cato, queue GPT-5.5/DevTeam, learnings, commit. | ISC-3, ISC-12, ISC-13, ISC-14, ISC-15, ISC-16, ISC-17, ISC-18, ISC-19 | final gate | false |

## Decisions

- 2026-06-11: **Light ISA + one final-program Cato** (owner-selected). No multi-round pre-edit Cato gate for
  Phase 39 because there is no API-surface source change; the heavy review is the single final-program Cato
  audit over the whole program state (satisfies master ISC-5 "final program verification").
- 2026-06-11: **ISC-29 docs go in a new `docs/architecture/physical-device-model.md` plus Core README/CLAUDE
  updates** (owner-selected) — best discoverability and one canonical reference; the architecture directory
  is auto-validated by `docs-qa`.
- 2026-06-11: **Interim Cato now, GPT-5.5 queued** (owner-selected) — consistent with every prior phase; the
  program reaches "verified, GPT-5.5 final Cato + backlog DevTeam reviews queued".
- 2026-06-11: **Reconciliation is evidence-backed.** Each master box is checked only after re-verifying the
  claim at HEAD; a gap found during the audit stops the phase and is surfaced rather than checked.

## Changelog

- conjectured: Phase 39 is a quick checkbox/formatting pass over the master ISA.
  refuted by: master ISC-30 requires the final claim to rest on docs QA, focused tests, safe hardware smoke,
  DevTeam review, and Cato; and ISC-29 has no existing consumer documentation to point at.
  learned: Phase 39 must produce real consumer docs, run the safe hardware smoke and the final gate, and
  reconcile each criterion against evidence true at HEAD — not rubber-stamp.
  criterion now: ISC-4..ISC-13 and the anti-criteria ISC-17..ISC-19 govern the work.

## Verification

Populated in the Phase 39 learning note before commit.
