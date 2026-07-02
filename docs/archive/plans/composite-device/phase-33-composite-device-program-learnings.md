# Phase 33 Learnings: Composite Device Program Planning

Use this note as the handoff record for Phase 33 of the composite-device program.

## Phase Summary

- Branch: `yubikit-composite-device-new`.
- Scope: write and verify composite-device planning artifacts only.
- Master ISA: `docs/plans/composite-device/ISA.md`.
- Phase ISA: `docs/plans/composite-device/phase-33-composite-device-program-ISA.md`.
- Source changed: none intended.
- Next phase after owner command: Phase 34, promote read-only device metadata to Core.

## Evidence Inputs

- Module-consolidation final reassessment: `docs/MODULE-CONSOLIDATION-FINAL-REASSESSMENT.md`.
- Phase 20 stop-gate ISA: `docs/plans/module-consolidation/phase-20-quality-convergence-before-composite-yubikey-ISA.md`.
- Phase 32 learning note: `docs/plans/module-consolidation/phase-32-same-criteria-quality-reassessment-learnings.md`.
- Core current discovery source: `src/Core/src/YubiKey/`.
- Current Management `DeviceInfo` source: `src/Management/src/DeviceInfo.cs`.
- Rust reference repo: `../yubikey-manager`.
- Rust reference branch: `experiment/rust`.
- Rust reference files: `crates/yubikit/src/platform/device.rs`, `crates/yubikit/src/device.rs`, `crates/yubikit/src/management.rs`, `crates/yubikit/src/core.rs`.

## What Changed

- Added `docs/plans/composite-device/ISA.md` as the long-lived composite-device program artifact.
- Added `docs/plans/composite-device/phase-33-composite-device-program-ISA.md` as the docs-only planning phase ISA.
- Added this learning note to capture review, verification, and next-phase inputs.
- Recorded that `IYubiKey` should represent a physical device, not a per-interface handle.
- Recorded that read-only physical-device metadata belongs in Core while mutating configuration remains in Management.
- Recorded that applet `IYubiKeyExtensions` should remain the ergonomic session-entry surface.
- Recorded a deferred downstream capability audit unlocked by Core `DeviceInfo` promotion.

## Review Evidence

- Cato route command: `bun ~/.claude/PAI/TOOLS/AgentHarnessRouter.ts --surface cato --role auditor --primary-model "openai/gpt-5.5" --cwd "$(pwd)" --dry-run --json`.
- Cato route result: `google-vertex-anthropic/claude-opus-4-8@default` selected for OpenAI primary.
- Initial Cato audit output: `/tmp/opencode/phase33-cato-audit.jsonl`.
- Initial Cato verdict: `concerns`, high criticality.
- Initial Cato findings addressed in the plan:
  - Added explicit Phase 34 namespace/API migration governance for `DeviceInfo`, `FormFactor`, `DeviceCapabilities`, `DeviceFlags`, `VersionQualifier`, and `VersionQualifierType`.
  - Added explicit Phase 36 scalar `IYubiKey.ConnectionType` disposition and call-site migration requirements.
  - Added explicit Phase 38 requirement to remove scalar-connection routing assumptions from extension methods, including FIDO2.
  - Added mandatory `Tests.Shared` and CLI compile consumer migration in the same phase as metadata promotion.
  - Split mandatory consumer migration from the deferred downstream capability audit.
- Focused Cato follow-up output: `/tmp/opencode/phase33-cato-followup.jsonl`.
- Focused Cato follow-up verdict: `pass` with info-only notes.
- Focused Cato info notes addressed in the plan:
  - Added explicit test ownership for raw/default connection behavior.
  - Added explicit Phase 35 verification that shared read-info logic does not reintroduce Core-to-Management coupling.

## Verification Evidence

- Branch check command: `git status --short --branch`.
- Branch check result before artifact edits: `## yubikit-composite-device-new`.
- Rust reference branch command: `git -C ../yubikey-manager status --short --branch`.
- Rust reference branch result from planning context: `## experiment/rust...origin/experiment/rust`.
- Initial docs QA command: `dotnet toolchain.cs -- docs-qa`.
- Initial docs QA result: passed; 54 active documentation files validated.
- Initial whitespace command: `git diff --check`.
- Initial whitespace result: passed with no output.
- Final docs QA command: `dotnet toolchain.cs -- docs-qa`.
- Final docs QA result: passed; 54 active documentation files validated.
- Final whitespace command: `git diff --check`.
- Final whitespace result: passed with no output.
- Final scope command: `git status --short --branch`.
- Final scope result before staging: `## yubikit-composite-device-new` with only `?? docs/plans/composite-device/`.

## What Did Not Work

- Initial plan under-specified namespace/API fallout from promoting `DeviceInfo` and supporting public Management types into Core.
- Initial plan did not explicitly govern the fate of scalar `IYubiKey.ConnectionType`, even though current `IYubiKey.ConnectAsync()` and FIDO2 extensions depend on the single-interface model.
- Initial deferred downstream item mixed mandatory compile migration for `Tests.Shared` and CLI consumers with optional future capability improvements.

## Reusable Patterns

- Separate composite-device work into package-boundary, read-info, physical-model, discovery, extension-default, and final-integration phases.
- Keep smart connection defaults in applet extension methods where application intent is known.
- Use Rust as behavior reference while preserving .NET package boundaries.

## Deferred Candidates

- DeviceInfo Promotion Downstream Capability Audit: after the physical-device model is implemented and verified, inventory capability-aware defaults, richer test filtering, smarter CLI selection, simpler docs examples, and future feature gates unlocked by moving read-only device metadata into Core.

## Next Phase Inputs

- Phase 34 should move or split read-only `DeviceInfo` and supporting metadata types from Management to Core.
- Phase 34 must not implement downstream capabilities unlocked by the move; it records those for the later deferred audit.
- Phase 34 must preserve Management's mutating configuration/session ownership.
- Phase 34 should use `/DevTeam` implementation/review/fix workflow.
- Phase 34 should run focused Core and Management build/tests before commit.

## Compact Summary

- Goal: plan composite-device program.
- Branch: `yubikit-composite-device-new`.
- Scope: docs only.
- Stop: wait for owner before Phase 34.
