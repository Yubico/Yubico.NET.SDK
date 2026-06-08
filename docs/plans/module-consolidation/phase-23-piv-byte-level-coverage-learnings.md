# Phase 23 Learnings: PIV Byte-Level Coverage

Use this note as the handoff record for Phase 23 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Scope: PIV byte-level unit coverage and approved full PIV integration baselines
- Phase ISA: `docs/plans/module-consolidation/phase-23-piv-byte-level-coverage-ISA.md`
- Source files changed: none
- Test files changed: `src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/PivSessionTests.cs`
- Project files changed: `src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/Yubico.YubiKit.Piv.IntegrationTests.csproj`
- Documentation files changed: master consolidation ISA and Phase 23 ISA/learning note
- Integration tests: full PIV integration suite run with explicit approval and 20-minute shell timeout
- Result: PIV byte-level coverage accepted, reviewed, and locally verified
- Commit: `7a55772a test(piv): add byte-level protocol coverage`
- `/Ping` status: compact summary produced before Phase 24 setup; no separate shell command is available in this harness

## What Changed

- Added focused PIV unit coverage for generate-key command encoding using `RecordingSmartCardConnection`.
- Added focused PIV unit coverage for sign/decrypt command encoding and parsed response handling.
- Added focused PIV unit coverage for calculate-secret command encoding and parsed response handling.
- Added a security-status error-path unit test for SW `0x6982` preserving the public `InvalidOperationException` shape.
- Tightened the new byte-level tests after DevTeam review so APDU data-field assertions verify ordered TLV shape instead of only tag presence.
- Added a direct `Xunit.SkippableFact` package reference to the PIV integration project because `Tests.Shared` now keeps its xUnit v2 packages private while its hardware skip infrastructure can throw `Xunit.SkipException` at runtime.
- Updated the master consolidation ISA to distinguish deferred unattended FIDO/FIDO2/WebAuthn User Presence flows from phase-approved persistent-state applet integration runs.
- Added the Phase 23 ISA to record explicit PIV full-suite integration approval, reset expectations, marker scans, and stop gates.

## Key Findings

- PIV key and crypto APDU command bytes are now unit-tested without adding a new private SmartCard recorder or APDU DSL.
- `RecordingSmartCardConnection` records the final short APDU bytes; for the tested commands, `command[1]`, `command[2]`, and `command[3]` are INS, P1, and P2, and `command[4]` is the short APDU Lc byte.
- Generate-key policy encoding uses ordered command data `AC 09 80 01 <algorithm> AA 01 <pin-policy> AB 01 <touch-policy>` when non-default policies are provided.
- Sign/decrypt dynamic authentication uses ordered command data beginning `7C 24 82 00 81 20` for an ECC P-256 challenge.
- Calculate-secret dynamic authentication uses ordered command data beginning `7C 45 82 00 85 41`, followed by the 65-byte uncompressed peer public point.
- The `PivMetadata.cs` move candidate was evaluated and not moved. It uses root namespace `Yubico.YubiKit.Piv`, while `src/Piv/src/Metadata/` uses `Yubico.YubiKit.Piv.Metadata`; namespace preservation and avoiding mixed folder conventions were more important than folder locality.
- Phase 23 did not add PIV integration tests, so pre/post full-module integration command scope stayed comparable.

## Integration Preflight Evidence

- Branch check: `git status --short --branch` showed `## yubikit-consolidation...origin/yubikit-consolidation`.
- Allow-list check: `src/Tests.Shared/appsettings.json` includes serial `103`; `AllowUnknownSerials` is `true`.
- Device preflight: `ykman list --serials` returned `103`.
- Device preflight: `ykman info` reported serial `103`, firmware `5.8.0.beta.0`, USB interfaces `OTP, FIDO, CCID`, and PIV enabled.
- PIV marker scan before integration found no `RequiresUserPresence`, touch-policy wait, `PermanentDeviceState`, or manual prompt markers. It found expected `Slow` markers in `PivImportTests`, `PivDecryptTests`, and `PivCryptoTests`, which were intentionally included by the approved non-smoke run.
- Hardware test output repeatedly selected serial `103` over SmartCard with firmware `5.8.0` and authorized three transports for the same key.

## Verification Evidence

- Pre-implementation reset setup: `dotnet toolchain.cs -- test --integration --project Piv --filter "FullyQualifiedName~PivResetTests"` passed 3/3 reset tests.
- Pre-implementation full PIV integration baseline: `dotnet toolchain.cs -- test --integration --project Piv` passed 61/61 unit tests and 70/70 integration tests before source changes.
- Focused unit test after adding coverage: `dotnet toolchain.cs -- test --project Piv --filter "FullyQualifiedName~PivSessionTests"` passed 17/17 PIV session tests.
- Build after implementation/review fixes: `dotnet toolchain.cs -- build --project Piv` succeeded with 0 warnings and 0 errors.
- Unit tests after implementation/review fixes: `dotnet toolchain.cs -- test --project Piv` passed 65/65 PIV unit tests.
- Post-implementation reset setup: `dotnet toolchain.cs -- test --integration --project Piv --filter "FullyQualifiedName~PivResetTests"` passed 3/3 reset tests.
- Final-current full PIV integration baseline: `dotnet toolchain.cs -- test --integration --project Piv` passed 65/65 unit tests and 70/70 integration tests in 4m46s.
- Docs QA: `dotnet toolchain.cs -- docs-qa` succeeded and validated 54 active documentation files before the learning note was added.
- Whitespace: `git diff --check` passed; Git emitted the known line-ending normalization warning for the touched PIV integration csproj but no whitespace errors.
- Targeted formatting: `dotnet format src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/Yubico.YubiKit.Piv.UnitTests.csproj --include src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/PivSessionTests.cs --verify-no-changes` passed after newline normalization.
- Full solution formatting note: `dotnet format --verify-no-changes` is currently blocked by pre-existing unrelated format/import issues across Core, FIDO2, WebAuthn, Management, YubiHsm, YubiOtp, CLI, and example projects; these were not introduced or modified by Phase 23.

## Review Evidence

- Cato plan review evidence: `/tmp/opencode/phase23-cato-plan-review-concrete.jsonl`.
- Cato verdict: pass after the Phase 23 ISA and master ISA were tightened for explicit full PIV integration approval, reset comparability, serial proof, marker scans, timeout handling, and post-baseline failure stop gates.
- DevTeam reviewer route: `google-vertex-anthropic/claude-opus-4-8@default` via `opencode run`, selected by `AgentHarnessRouter.ts` because the primary model is OpenAI GPT-5.5.
- DevTeam first review output: `/tmp/opencode/phase23-devteam-review.md`.
- DevTeam first verdict: `PASS WITH NOTES`; material note was that presence-only TLV assertions could pass with misordered command data.
- DevTeam resolution: PIV tests now assert ordered APDU data-field shape with `CommandData` and byte-exact or prefix assertions.
- DevTeam re-review output: `/tmp/opencode/phase23-devteam-rereview.md`.
- DevTeam re-review verdict: `PASS WITH NOTES`; it confirmed the TLV-ordering finding was resolved. Remaining low note about `Xunit.SkippableFact` appearing unused was addressed with a csproj comment documenting the `Tests.Shared` runtime skip dependency after `PrivateAssets`.

## Integration Lifecycle

- Hardware target: YubiKey serial `103`, firmware `5.8.0` beta, SmartCard/PIV enabled.
- Persistent state changed: yes. Phase 23 intentionally ran the full PIV integration suite, including reset, PIN/PUK, management-key, key, certificate, object, and retry-counter flows.
- Destructive tests: yes, within approved PIV scope and reset expectations.
- Post-suite state guarantee: none. Later phases must perform their own module-specific hardware preflight/setup and must not infer readiness from Phase 23's final PIV applet state.
- FIDO/FIDO2/WebAuthn User Presence status: not verified by Phase 23; unattended UP/UV/touch flows remain deferred unless explicitly human-coordinated.

## Cross-Module Implications

- The master consolidation ISA now allows phase-approved persistent-state applet integration tests while still blocking unattended FIDO/FIDO2/WebAuthn User Presence flows by default.
- Future phases that need persistent-state applet integration must explicitly record human approval, command shape, reset expectations, and timeout bounds in the phase ISA.
- Future integration projects that consume `Tests.Shared` skip infrastructure may need direct `Xunit.SkippableFact` references if `Tests.Shared` keeps that package private to avoid xUnit v2/v3 leakage.
- Future applet byte-level coverage should prefer `RecordingSmartCardConnection` and ordered APDU data assertions over presence-only tag checks.

## Deferred Future Improvements

- Consider a small test helper in `Tests.Shared` for extracting short APDU data fields only if another module repeats this exact pattern; Phase 23 intentionally kept it local until reuse is proven.
- Do not move `PivMetadata.cs` unless a later API/locality phase establishes a namespace-preserving folder strategy for root-namespace PIV metadata types.
- Track the unrelated repo-wide `dotnet format --verify-no-changes` failures as a separate formatting/import cleanup, not part of PIV byte-level coverage.
- Phase 24 should apply the same byte-level pattern to YubiHsm without assuming PIV hardware state is reusable.

## Compact Summary

- Goal: add focused PIV APDU/TLV byte-level coverage.
- Files changed: master ISA, Phase 23 ISA, PIV unit tests, PIV integration csproj, learning note.
- Final pattern: shared recorder, ordered APDU data assertions, no production source change.
- Rejected approaches: APDU DSL, command classes, source moves, integration-test scope expansion beyond approved PIV full suite.
- Tests passed: PIV build, 65/65 unit tests, reset setup 3/3, full PIV integration 70/70.
- Integration lifecycle: persistent-state PIV suite approved; no post-state guarantee.
- Review result: Cato pass; DevTeam pass with notes resolved or documented.
- Deferred future improvements: short APDU helper only after reuse, repo-wide format cleanup separately.
- Next phase recommendation: Phase 24 YubiHsm byte-level coverage.
- Learning note path: `docs/plans/module-consolidation/phase-23-piv-byte-level-coverage-learnings.md`
- Commit: `7a55772a test(piv): add byte-level protocol coverage`.
- `/Ping` status: compact summary produced before Phase 24 setup; no separate shell command is available in this harness.
