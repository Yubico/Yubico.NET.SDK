# Phase 21 Learnings: Core A- Readiness And SDK-Family API Alignment

Use this note as the handoff record for Phase 21 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Scope: Core DI documentation drift repair, checksum duplication audit, and SDK-family public API shape audit
- Phase ISA: `docs/plans/module-consolidation/phase-21-core-a-readiness-sdk-api-alignment-ISA.md`
- Audit artifact: `docs/plans/module-consolidation/phase-21-core-a-readiness-sdk-api-alignment-audit.md`
- Source files changed: applet DI XML comments only
- Test files changed: Security Domain test documentation only
- Integration tests: not applicable; Phase 21 does not touch runtime behavior or hardware paths
- Result: Phase 21 artifacts and DI drift repairs reviewed, patched, verified, and ready to commit
- Commit: this commit
- `/Ping` status: to be sent after commit

## What Changed

- Repaired root agent guidance that still claimed Core had an `AddYubiKeyManagerCore()` DI entry point.
- Repaired Core logging/setup docs to use static `YubiKeyManager` and `YubiKitLogging.Configure(...)`.
- Repaired applet DI XML comments so module DI extensions are described as factory registrations, not Core-service chains.
- Repaired the FIDO2 DI XML comment missed by the first pass; the original verification grep was too narrow because it searched for `AddYubiKeyManagerCore(` and missed no-parenthesis prose.
- Repaired Security Domain test docs to show the current DI factory registration shape.
- Created a Phase 21 audit artifact that records the duplicate CRC13239 utilities and defers consolidation until an approved public API decision.
- Created a source-backed SDK-family API alignment audit against Python, Android, and Swift references without starting composite design.

## Key Findings

- Core DI drift was broader than `src/Core/README.md`; active root guidance, Core docs, Security Domain test docs, and applet XML comments also had stale setup claims.
- `ChecksumUtils` and `Crc13239` duplicate the same CRC13239 polynomial but expose different public shapes, so consolidation is a public API decision.
- Python and Android both expose `supports/open connection` and identity/fingerprint concepts on YubiKey device references.
- Swift is more connection/session oriented, which supports keeping .NET connection clarity even if a later composite model aggregates physical USB interfaces.

## Deferred Future Improvements

- Decide a canonical public CRC13239 helper shape in a later approved API phase.
- Decide whether `IYubiKey` remains per-interface or becomes a physical-device abstraction during the composite YubiKey owner interview.
- Decide whether .NET should add public `SupportsConnection(...)`, fingerprint, PID, or DeviceInfo-like properties to Core device references.

## Verification Evidence

- Initial build: `dotnet toolchain.cs -- build --project Core` succeeded with 0 warnings and 0 errors across Core, Core integration tests, and Core unit tests.
- Final build after the FIDO2 XML-comment fix and learning-note update: `dotnet toolchain.cs build` succeeded for the full solution with 0 warnings and 0 errors.
- Docs QA: `dotnet toolchain.cs -- docs-qa` succeeded and validated 54 active documentation files.
- Whitespace: `git diff --check` passed.
- Initial stale Core DI source check: grep of `src/**` for `AddYubiKeyManagerCore(` found no files, but this pattern was too narrow and missed no-parenthesis prose in `src/Fido2/src/DependencyInjection.cs`.
- Reviewer finding: `src/Fido2/src/DependencyInjection.cs` still claimed `AddYubiKeyFido2()` called `AddYubiKeyManagerCore`; this was patched to match the module-local factory-delegate registration model.
- Final source check: `rg -n "AddYubiKeyManagerCore" "src" --glob '!**/bin/**' --glob '!**/obj/**'` returned no output.
- Final stale DI check: DevTeam verified no `IYubiKeyManager` DI resolution in active source/Core docs.
- Logging doc check: grep of `docs/LOGGING.md` for `IYubiKeyManager|AddYubiKeyManagerCore(` found no files.
- Review output: DevTeam and Cato reruns passed after ADC refresh and the FIDO2 XML-comment fix.

## Review Evidence

- DevTeam route: `google-vertex-anthropic/claude-opus-4-8@default`, resolved through `AgentHarnessRouter.ts` with primary model `openai/gpt-5.5`.
- DevTeam output: `/tmp/opencode/phase21-devteam-review.jsonl` initially errored on Vertex `invalid_grant` / `invalid_rapt`; `/tmp/opencode/phase21-devteam-review-rerun.jsonl` reran after ADC refresh; `/tmp/opencode/phase21-devteam-after-fido2-fix.jsonl` reran after the FIDO2 patch.
- DevTeam verdict: `pass` after the FIDO2 patch, with info-only historical/migration-context notes.
- Cato route: `google-vertex-anthropic/claude-opus-4-8@default`, resolved through `AgentHarnessRouter.ts` with primary model `openai/gpt-5.5`.
- Cato output: `/tmp/opencode/phase21-cato-audit.jsonl` initially errored on Vertex `invalid_grant` / `invalid_rapt`; `/tmp/opencode/phase21-cato-audit-rerun.jsonl` reran after ADC refresh; `/tmp/opencode/phase21-cato-after-fido2-fix.jsonl` reran after the FIDO2 patch.
- Cato verdict: `pass` after the FIDO2 patch, with info-only historical/migration-context notes.
- Findings resolved: the missed FIDO2 XML comment was patched; remaining `AddYubiKeyManagerCore` mentions are historical/spec/research/old-plan content, explicit migration examples that instruct removal, or Phase 19/20 assessment notes that identify the drift as a risk.

## Integration Lifecycle

- Hardware target: connected YubiKey 5.8 beta key remains available for later phases.
- Phase 21 Management preflight: not applicable; no runtime behavior changed.
- Integration scope: none.
- Persistent state changed: no.
- Destructive tests: none.

## Cross-Module Implications

- Applet DI extensions remain useful for session-factory injection, but they no longer imply Core device discovery is DI-based.
- Phase 22 should inherit the current static `YubiKeyManager` understanding when it evaluates Tests.Shared recorder/harness patterns.
- Future API alignment work should be explicit about whether it is public API design or documentation-only cleanup.

## Compact Summary

- Goal: repair Core setup drift and audit API alignment.
- Files changed: root/Core/logging docs, applet DI XML comments, Security Domain test docs, and Phase 21 ISA/audit/learnings.
- Final pattern: static Core discovery, explicit logging setup, module-local DI factories.
- Rejected approaches: restoring Core DI, removing public checksum helpers, starting composite design.
- Tests passed: `dotnet toolchain.cs -- build --project Core`; `dotnet toolchain.cs build`; `dotnet toolchain.cs -- docs-qa`; `git diff --check`.
- Integration lifecycle: none; docs/XML-comment/audit phase.
- Shared/Core candidates: CRC13239 canonical helper, future device identity surface.
- Deferred future improvements: composite YubiKey owner interviews and CRC API decision.
- House-style update needed: none yet.
- Next phase recommendation: Tests.Shared recorder and harness decision.
- Learning note path: `docs/plans/module-consolidation/phase-21-core-a-readiness-sdk-api-alignment-learnings.md`
- Commit: this commit.
- `/Ping` status: to be sent after commit.
