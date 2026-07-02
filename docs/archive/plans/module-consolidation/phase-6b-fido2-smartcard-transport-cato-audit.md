# Phase 6B Cato Audit: FIDO2 SmartCard Transport

Use this note as the cross-vendor audit record for commit `8e48b4f3 refactor(fido2): support smart card transport on modern firmware`.

## Audit Summary

- Branch: `yubikit-consolidation`
- Audited commit: `8e48b4f3`
- Commit range: `8e48b4f3^..8e48b4f3`
- Primary executor model: `openai/gpt-5.5`
- Cross-vendor auditor: `google-vertex-anthropic/claude-opus-4-8@default`
- Cato prompt: `/tmp/opencode/cato-prior-fido2-smartcard-audit.txt`
- Cato output: `/tmp/opencode/cato-prior-fido2-smartcard-audit.jsonl`
- Verdict: `concerns`
- Criticality: `medium`
- Result after follow-up: warnings addressed with explicit documentation and test renaming; no protocol behavior changed in the audit follow-up

## Findings And Disposition

| Severity | Finding | Disposition |
| --- | --- | --- |
| warning | NFC exemption in `EnsureSmartCardTransportSupported` depends on `Transport.Nfc`, but current PC/SC SmartCard connections may report `Transport.Usb`, making pre-5.8 NFC support unverified. | Documented in `src/Fido2/CLAUDE.md`, `src/Fido2/tests/CLAUDE.md`, and `FidoSession.EnsureSmartCardTransportSupported`; deferred Core transport detection as a separate future change. |
| warning | SmartCard firmware gate runs after FIDO2 AID SELECT/GetInfo, so pre-5.8 USB SmartCard receives a SELECT before being rejected. | Documented as intentional because FIDO firmware is obtained from CTAP GetInfo over the selected app. No behavior change made. |
| info | No phase-specific cross-vendor audit document existed for `8e48b4f3`. | This document records the completed audit and dispositions. |
| info | Test names still claimed NFC coverage after filters changed to generic SmartCard. | Renamed integration test class/methods from NFC-specific names to SmartCard-specific names. |

## Deferred Work

- Implement reliable SmartCard NFC-vs-USB transport detection in Core before relying on `Transport.Nfc` for pre-5.8 FIDO2 SmartCard gating.
- Consider a pre-SELECT firmware gate only if a trusted firmware source is available before selecting the FIDO2 AID; current FIDO2 SmartCard flow obtains firmware from CTAP GetInfo after selection.

## Verification Plan

- `dotnet format --verify-no-changes --include ...`: passed
- `dotnet toolchain.cs -- build --project Fido2`: passed, 0 warnings, 0 errors
- `dotnet toolchain.cs -- test --project Fido2`: passed, 387/387
- `dotnet toolchain.cs -- test --integration --project Fido2.IntegrationTests --smoke --filter "FullyQualifiedName~FidoSmartCardTests"`: passed, 3/3

## Compact Summary

- Goal: complete missing cross-vendor review for the prior FIDO2 SmartCard transport phase.
- Findings: medium concerns around NFC transport detection, post-SELECT gating, missing audit documentation, and misleading NFC test names.
- Fixed now: audit documentation, FIDO2 module/test docs, SmartCard test naming, and transport-gate caveat comments.
- Deferred: Core-level NFC transport detection.
