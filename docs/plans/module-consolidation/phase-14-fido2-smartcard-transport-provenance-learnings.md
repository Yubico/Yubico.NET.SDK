# Phase 14 Learnings: FIDO2 SmartCard Transport Provenance

Use this note as the handoff record for Phase 14 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Scope: detect the current PC/SC SmartCard transport from ATR evidence and use that provenance for the FIDO2 SmartCard firmware gate
- Phase ISA: `docs/plans/module-consolidation/phase-14-fido2-smartcard-transport-provenance-ISA.md`
- Source files changed: Core ATR access/classification, PC/SC discovery kind assignment, SmartCard transport mapping, FIDO2 SmartCard support gate, and FIDO2 docs
- Test files changed: Core ATR classifier tests, Core SmartCard transport mapping tests, and FIDO2 SmartCard firmware-gate tests
- Integration tests requiring reset, touch, User Presence, UV, PIN, or persistent-state mutation: not run
- Result: implementation verified by Core/FIDO2 builds, Core/FIDO2 unit tests, formatting, whitespace check, DevTeam cross-vendor review, and read-only FIDO2 SmartCard integration smokes
- Implementation commit: `5d9ee1dd fix(fido2): detect smart card transport provenance`
- Learning note status: backfilled after implementation commit because the next-phase gate caught the missing learning artifact
- `/Ping` status: pending

## What Changed

- Added `PcscConnectionKindDetector` as a pure Core ATR classifier.
- Added an internal `AnswerToReset.Bytes` span accessor so classification can inspect the ATR without reparsing or cloning.
- `FindPcscDevices` now assigns `Kind = PcscConnectionKindDetector.Detect(atr)` for emitted PC/SC YubiKey devices instead of hardcoding USB.
- `UsbSmartCardConnection.Transport` now maps the carried `IPcscDevice.Kind` through an explicit switch.
- `PscsConnectionKind.Nfc` maps to `Transport.Nfc`.
- `PscsConnectionKind.Usb` maps to `Transport.Usb`.
- `PscsConnectionKind.Unknown`, `PscsConnectionKind.Any`, and unexpected values map conservatively to `Transport.Usb` so they do not bypass the USB SmartCard FIDO2 firmware gate.
- `FidoSession.EnsureSmartCardTransportSupported(...)` now short-circuits actual NFC SmartCard transport before applying the USB SmartCard firmware gate.
- FIDO2 USB SmartCard support now uses a named `Feature` with `Feature.IsSupportedByFirmware(...)` for the `5.8.0+` firmware boundary.
- FIDO2 documentation now distinguishes current NFC transport from device NFC capability.

## Why This Shape

- The support question depends on two facts: the current transport and firmware. `Feature` owns only the firmware portion.
- The transport fact belongs in Core PC/SC discovery and connection mapping because FIDO2 should not infer current transport from model names or NFC capability metadata.
- `yubikey-manager` informed the ATR rule: USB PC/SC ATRs have second byte high nibble `0xF?`; discovered YubiKey PC/SC ATRs without that nibble are NFC.
- The .NET implementation adapts the rule as a small classifier rather than copying manager code or adding broad reader-management abstractions.
- Unknown transport fails closed as USB for FIDO2 gating because treating unknown as NFC would allow unsupported USB SmartCard FIDO2 on older firmware.
- The existing `UsbSmartCardConnection` type name was left unchanged to avoid expanding internal churn; the behavior now reflects actual `IPcscDevice.Kind`.

## Verification Evidence

- Build command: `dotnet toolchain.cs -- build --project Core`
- Build result: passed, 0 warnings, 0 errors.
- Build command: `dotnet toolchain.cs -- build --project Fido2`
- Build result: passed, 0 warnings, 0 errors.
- Core unit command: `dotnet toolchain.cs -- test --project Core`
- Core unit result: 336 succeeded, 2 skipped.
- FIDO2 unit command: `dotnet toolchain.cs -- test --project Fido2`
- FIDO2 unit result: 395 succeeded.
- Format command: `dotnet format --verify-no-changes --include <Phase 14 touched files>`
- Format result: passed after line-ending normalization.
- Whitespace command: `git diff --check`
- Whitespace result: passed; git emitted line-ending normalization warnings under the then-current CRLF/LF config mismatch.
- FIDO2 sentinel grep: no direct `Major == 0` firmware sentinel remains in FIDO2.
- Core transport grep: no Core SmartCard hardcode remains for `Kind = PscsConnectionKind.Usb` or `Transport => Transport.Usb` in the relevant paths.

## Integration Lifecycle

- Hardware target: SmartCard/CCID connection on firmware `5.8.0`.
- Integration scope was read-only: yes.
- Tests requiring touch, PIN, UV, reset, credential creation, deletion, or persistent-state mutation: skipped.
- Integration command: `dotnet toolchain.cs -- test --integration --project Fido2.Integration --smoke --filter "FullyQualifiedName~CreateFidoSession_With_SmartCard_CreateAsync"`
- Integration result: passed.
- Integration command: `dotnet toolchain.cs -- test --integration --project Fido2.Integration --smoke --filter "FullyQualifiedName~GetInfo_Over_SmartCard_ReturnsValidFido2Version"`
- Integration result: passed.
- Integration command: `dotnet toolchain.cs -- test --integration --project Fido2.Integration --smoke --filter "FullyQualifiedName~GetInfo_Over_SmartCard_ReturnsSupportedAlgorithms"`
- Integration result: passed.
- Persistent state changed: no.
- Reset/cleanup performed: none.

## Review Evidence

- Cato audit outputs:
- `/tmp/opencode/cato-p14-plan-audit.jsonl`
- `/tmp/opencode/cato-p14-plan-audit-r2.jsonl`
- `/tmp/opencode/cato-p14-plan-audit-r3.jsonl`
- `/tmp/opencode/cato-p14-plan-audit-r4-retry.jsonl`
- DevTeam review output: `/tmp/opencode/devteam-p14-review.jsonl`
- DevTeam reviewer route: `google-vertex-anthropic/claude-opus-4-8@default`
- DevTeam verdict: no blocking issues.
- Findings fixed before implementation: Cato plan feedback drove explicit criteria for classifier purity, default `Kind` fail-closed behavior, NFC-before-firmware branch ordering, no numeric enum casts, and source-backed `yubikey-manager` ATR inspiration.
- Findings deferred: `UsbSmartCardConnection.SupportsExtendedApdu()` remains a separate final follow-up investigation, not part of Phase 14 transport provenance.

## Tooling Notes

- Phase 14 exposed a focused xUnit v3/MTP filter problem in `toolchain.cs` when mixed unit/integration projects did not all match the requested filter.
- The toolchain issue was fixed separately in `08ff4aec fix(toolchain): skip unmatched mtp filters` after Phase 14.
- Future focused integration commands can rely on the toolchain skipping zero-match included MTP projects and failing clearly when no selected project matches the filter.
- The CRLF/LF warning source was later removed by `b2636d6f chore: set csharp line endings to lf`.

## Deferred Future Improvements

- Investigate `src/Core/src/SmartCard/UsbSmartCardConnection.cs` `SupportsExtendedApdu()` against the `../yubikit-manager` SDK reference implementation.
- Decide whether extended-APDU support should be based on transport, reader capability, platform behavior, firmware, active protocol, or a bounded probe.
- Save the extended-APDU investigation for the final follow-up improvement pass unless a future phase explicitly promotes it earlier.
- Consider whether the `UsbSmartCardConnection` type name should eventually become transport-neutral; Phase 14 intentionally fixed behavior without broad renaming.

## Cross-Module Implications

- Modules likely affected: Core, FIDO2, and any future SmartCard transport-dependent feature gate.
- Next module should copy: keep firmware-only gates in `Feature`, but keep transport/app/device-state facts local to the layer that has the evidence.
- Next module should avoid: inferring runtime transport from model, form factor, NFC capability, enum numeric casts, or unset default values.
- Potential API compatibility concern: none; Phase 14 changed internal classification and gating behavior without public API shape changes.

## Generalization Check

- Pattern classification: Core-local evidence classification with module-local support policy.
- Reusable lesson: classify runtime facts once at discovery/connection boundaries, then let module support gates consume that evidence without re-inference.
- Not promoted to shared code: no broader capability/support framework was added because this phase needed only a narrow ATR classifier and FIDO2 gate cleanup.

## Compact Summary

- Goal: make FIDO2 SmartCard USB/NFC support depend on current transport provenance.
- Fix: ATR classifier plus `IPcscDevice.Kind` to `Transport` mapping and FIDO2 `Feature` firmware gate.
- Tests passed: Core build, FIDO2 build, Core unit tests, FIDO2 unit tests, formatting, and whitespace check.
- Integration lifecycle: read-only FIDO2 SmartCard create/GetInfo smokes passed; no touch, PIN, UV, reset, or persistent-state mutation.
- Review: Cato plan audit incorporated; DevTeam Vertex Opus 4.8 review found no blocking issues.
- Deferred: `SupportsExtendedApdu()` investigation against `../yubikit-manager`, saved for final follow-up.
- Next phase recommendation: Phase 15 CLI Secret Policy + OATH Unlock Migration.
- Learning note path: `docs/plans/module-consolidation/phase-14-fido2-smartcard-transport-provenance-learnings.md`
- Implementation commit: `5d9ee1dd fix(fido2): detect smart card transport provenance`
- `/Ping` status: pending
