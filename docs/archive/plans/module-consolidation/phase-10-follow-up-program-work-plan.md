# Phase 10: Follow-Up Program Work Plan

This work plan closes the original sequential module consolidation sequence and opens the follow-up improvement program. It is a governance artifact only. It does not authorize source-code changes by itself.

Read this together with:

- `docs/plans/module-consolidation/ISA.md`
- `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`
- `docs/SDK-HOUSE-STYLE.md`
- `docs/plans/module-consolidation/phase-9-documentation-repair-learnings.md`
- `docs/plans/module-consolidation/phase-10-follow-up-program-learnings.md`

## Purpose

Phases 1-9 repaired the main consolidation backlog from `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`. Those phases intentionally deferred several medium/high concerns so individual module phases stayed reviewable and did not expand into broad cleanup.

Phase 10 records the transition from the original consolidation program into a smaller follow-up program. Its job is to say what was fixed, what still deserves a phase, what is deferred, what is rejected, and how the final reassessment will be run without mixing audit work into implementation work.

## What Phases 1-9 Fixed

| Phase | Area | Fixed |
| --- | --- | --- |
| Phase 1 | Sensitive payload lifecycle | YubiHsm and Management encoded sensitive payload cleanup patterns were repaired and recorded. |
| Phase 2 | Tests.Shared harness | Shared integration-test harness patterns were consolidated enough to support later phases. |
| Phase 3 | OATH chained response | OATH moved to Core configured chained-response handling, with fake APDU tests for `0xA5`. |
| Phase 4 | PIV flat-flow cleanup | PIV session shape was simplified while preserving visible protocol flow and touch behavior. |
| Phase 5 | SecurityDomain locality | SecurityDomain locality improved without introducing operation-specific command classes. |
| Phase 6 | FIDO2 CTAP requests | FIDO2 request construction and sensitive CTAP buffer handling were made more consistent. |
| Phase 7 | WebAuthn API coherence | WebAuthn public construction and production backend access were repaired. |
| Phase 8A | CLI device targeting | Unified CLI `--serial` and `--transport` options now affect device selection. |
| Phase 8B | CLI secure prompts | `Cli.Shared` gained disposable credential bytes and PIV PIN/PUK prompts migrated to it. |
| Phase 9 | Documentation repair | Active root/module docs were source-backed and stale high-confidence guidance was removed. |

## Deferred Concern Inventory

| Family | Concern | Disposition |
| --- | --- | --- |
| Core protocol correctness | SCP response chaining under response-chaining conditions needs byte-level characterization. | Fix now. |
| Core connection semantics | `ConnectionType` is marked `[Flags]`, but `HidOtp = 3` is exactly equal to `Hid | HidFido`, and `All` redundantly ORs the overlapping value. | Fix now. |
| Core firmware gates | Alpha/beta firmware sentinel handling is duplicated through direct `Major == 0` checks. | Fix now. |
| FIDO2 SmartCard transport | USB/NFC SmartCard FIDO2 provenance and firmware gating need a source-backed boundary. | Fix now, after firmware-gate cleanup. |
| Core SmartCard extended APDU | `UsbSmartCardConnection.SupportsExtendedApdu()` returned `true` unconditionally during Phase 10 planning. | Closed later by commit `90a41b26 fix(core): restrict extended apdu support to usb`. |
| CLI credential security | Remaining secret paths still use strings and argv-bound secrets need an explicit policy. | Fix now in a narrow slice. |
| API/package compatibility | Branch-level compatibility should be checked before changing test/tooling infrastructure. | Fix now as a checkpoint. |
| Test runner/tooling | xUnit v3 focused-filter behavior and FIDO/WebAuthn UP coordination remain friction points. | Fix now, after source-risk phases. |
| Docs QA/tooling | Active docs need bounded validation; no docfx/link-check config exists today. | Fix now as bounded tooling/docs phase. |
| Final grading | The branch needs same-criteria reassessment against the original health matrix. | Separate final audit phase. |

## Fix Now

These items are medium/high enough to justify follow-up phases now.

1. Core SCP chained-response correctness.
2. Core `ConnectionType` semantics.
3. Core `FirmwareVersion` / `Feature` firmware-gate semantics.
4. FIDO2 SmartCard USB/NFC transport provenance.
5. CLI secret policy plus one named migration: OATH password unlock path.
6. Public API and package compatibility checkpoint.
7. Test runner and human User Presence coordination.
8. Bounded active documentation QA/tooling.
9. Separate final reassessment audit using the original grading criteria.

## Defer

These are real concerns, but not worth pulling into the immediate follow-up sequence.

- Redundant CLI Management info queries: defer unless fake tests show material user-visible cost or confusion.
- CLI usage-error exit code: defer until CLI UX/error taxonomy becomes a phase.
- CLI unit-test solution registration: defer unless IDE, CI, or packaging tooling requires `.sln` inclusion.
- `SecureCredential` scoped callback API: defer until at least two more command-family migrations show retained-memory risk in practice.
- Shared fake SmartCard recorder: defer until more modules prove the duplication is costly.
- Bundled WebAuthn Public Suffix List verifier: defer to a WebAuthn hardening program.
- Broader PIV crypto APDU matrix: defer until future PIV crypto protocol work.
- Archived docs/plans cleanup: defer to a separate documentation archive program.
- `UsbSmartCardConnection.SupportsExtendedApdu()` reference comparison: closed by commit `90a41b26`; confirmed USB can use extended APDUs while NFC/unknown/wildcard PC/SC connections fall back to short APDU command chaining.

## Reject

These are explicitly out of the follow-up program.

- One giant Core cleanup phase.
- Broad "make all modules consistent" cleanup without a concrete risk.
- Operation-specific command classes or command-like protocol executors.
- Broad CLI parser/session/helper consolidation without repeated evidence.
- Moving transport, device capability, applet state, or authentication-state support policy into `Feature`.
- Making FIDO2/WebAuthn User Presence or User Verification tests unattended gates.
- Rewriting `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`; it remains the baseline.

## Upcoming Phase Order

| Phase | Name | Purpose |
| --- | --- | --- |
| Phase 10 | Follow-Up Program Work Plan | This docs-only transition artifact. |
| Phase 11 | Core SCP Chained Response | Characterize and, if needed, minimally correct SCP response-chaining order. |
| Phase 12 | Core `ConnectionType` Semantics | Repair or constrain `ConnectionType` flags semantics with unit coverage, explicit numeric values, and compatibility checks. |
| Phase 13 | Core `FirmwareVersion` / `Feature` Firmware Gates | Centralize alpha/beta firmware-gate semantics without over-broad support abstraction. |
| Phase 14 | FIDO2 SmartCard Transport Provenance | Clarify USB/NFC SmartCard FIDO2 support boundaries using the Phase 13 firmware primitive. |
| Phase 15 | CLI Secret Policy + OATH Unlock Migration | Decide argv secret policy, then migrate the OATH password unlock path only. |
| Phase 16 | API And Package Compatibility Checkpoint | Check public API/package dependency risk before tooling changes. |
| Phase 17 | Test Runner And Hardware Coordination | Fix focused test runner friction and define manual UP/UV coordination lanes. |
| Phase 18 | Docs QA Tooling | Add bounded active-doc validation or document why it remains manual. |
| Phase 19 | Final Reassessment Audit | Re-grade modules with the same criteria as the original assessment. |

## Phase 13 Firmware Gate Design

Approved direction:

- `FirmwareVersion` remains a mostly dumb version value.
- `FirmwareVersion.IsAlphaOrBeta` becomes the single source of truth for alpha/beta/test firmware sentinel handling and should return `true` when `Major == 0`.
- This is an intentional behavior and test change, not a no-op rename. Current source defines `IsAlphaOrBeta` as `0.0.0` only, while `ApplicationSession.IsSupported` already treats any `Major == 0` version, including applet-reported `0.0.1`, as the sentinel behavior for feature gates.
- Phase 13 must preserve the existing `ApplicationSession` sentinel behavior while moving it behind the single `IsAlphaOrBeta` name, and must update `FirmwareVersionTests` that currently assert `0.0.1` / `0.1.0` are not alpha/beta.
- `Feature` remains firmware-only. It does not gain connection type, `DeviceInfo`, capability, applet state, or authentication-state requirements.
- Add `Feature.IsSupportedByFirmware(FirmwareVersion firmwareVersion)` as the low-level firmware predicate.
- `ApplicationSession.IsSupported(Feature feature)` delegates to `feature.IsSupportedByFirmware(FirmwareVersion)`.
- Session subclasses use `IsSupported(...)` / `EnsureSupports(...)` for firmware-only gates where applicable.
- Module-specific support stays local when it depends on transport, connection type, FIDO2 AID exposure, hardware configuration, applet state, authentication state, or runtime session state.
- Standalone value objects may use `Feature.IsSupportedByFirmware(...)` when they model a named firmware feature; otherwise they may use explicit local minimum-version logic plus `IsAlphaOrBeta`.

The complete support question belongs at the narrowest place that has all the facts. `Feature` answers only the firmware portion.

Phase 13 acceptance criteria must include a source-backed regression check that no session-level firmware gate loses the existing `Major == 0` sentinel allowance during the move to `Feature.IsSupportedByFirmware(...)`.

Phase 13 must also check `FirmwareVersion` comparison and ordering consumers. Broadening `IsAlphaOrBeta` from `0.0.0` to any `Major == 0` affects `IsAtLeast(...)`, `IsLessThan(...)`, and `CompareTo(...)`, not only `ApplicationSession.IsSupported(...)`.

Phase 13 must inventory all direct `Major == 0` firmware sentinel consumers, including `ApplicationSession` and `PcscProtocol`, before editing. `PcscProtocol` is not an `ApplicationSession`, so it should use the `FirmwareVersion` sentinel fact directly rather than depending on session helpers.

Phase 13 must account for `Feature.Version` being a computed `FirmwareVersion` over `VersionMajor`, `VersionMinor`, and `VersionRevision`. `Feature.IsSupportedByFirmware(...)` must compare through the same `FirmwareVersion` semantics that Phase 13 is changing.

If Phase 13 is split or deferred, Phase 14 must not invent a separate firmware sentinel rule. It must either wait for Phase 13 or explicitly preserve the current FIDO2-local gate as a temporary dependency fallback.

Phase 15's argv secret policy is program-wide even though its implementation slice migrates only the OATH password unlock path. Later CLI credential-family migrations must inherit the Phase 15 policy unless a future phase explicitly revises it.

`Transport` is also a `[Flags]` enum, but it is not automatically in Phase 12 scope. Phase 12 may inspect it to avoid repeating mistakes, but any `Transport` behavior change must be explicitly approved or deferred.

## Final Reassessment Audit Guardrail

Phase 19 is separate, read-only, and uses the same grading criteria as `docs/MODULE-CONSOLIDATION-ASSESSMENT.md`:

- Overall
- Complexity
- Maturity
- DRY
- Rolling Own
- Maintainability
- Top Consolidation Target

The final reassessment should create a new artifact, such as `docs/MODULE-CONSOLIDATION-FINAL-REASSESSMENT.md`. It should not rewrite the original baseline assessment.

## Verification Plan

Phase 10 verification is documentation-only:

- Confirm this work plan exists.
- Confirm `docs/plans/module-consolidation/ISA.md` references this work plan.
- Confirm the work plan contains fixed, fix-now, defer, reject, upcoming-phase, firmware-gate, and final-audit sections.
- Confirm the diff contains no source-code changes.

## Cato Review Plan

Run Cato as a direct artifact audit before committing Phase 10.

Cato should verify:

- Phase 10 stays docs-only.
- The plan follows `docs/plans/module-consolidation/ISA.md`.
- The phase order is coherent and does not imply false dependencies.
- Medium/high concerns are not buried.
- Low-value work is deferred or rejected.
- Phase 13 keeps firmware policy low-coupling and high-cohesion.
- Phase 19 remains a separate read-only reassessment audit.
