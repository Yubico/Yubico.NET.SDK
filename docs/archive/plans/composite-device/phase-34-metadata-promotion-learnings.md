# Phase 34 Learnings: Core Metadata Promotion

Use this note as the handoff record for Phase 34 of the composite-device program.

## Phase Summary

- Branch: `yubikit-composite-device-new`.
- Scope: promote read-only device metadata models from Management to Core and migrate mandatory consumers.
- Phase ISA: `docs/plans/composite-device/phase-34-metadata-promotion-ISA.md`.
- Source changed: Core, Management, Tests.Shared, CLI shared/commands, and examples.
- Next phase after owner command: Phase 35, share read-only device-info retrieval without reintroducing Core-to-Management coupling.

## What Changed

- Added Core metadata models under `src/Core/src/YubiKey/`:
  - `DeviceInfo`
  - `DeviceCapabilities`
  - `DeviceFlags`
  - `FormFactor`
  - `VersionQualifier`
  - `VersionQualifierType`
- Moved internal `CapabilityMapper` with `DeviceInfo` into Core.
- Removed the old Management-owned metadata model files.
- Updated Management session/interface/extensions to return Core `DeviceInfo`.
- Updated `Tests.Shared`, CLI shared/commands, PIV example, and Management example imports to use `Yubico.YubiKit.Core.YubiKey`.
- Migrated `CapabilityMapperTests` from Management unit tests to Core unit tests.
- Added Core `DeviceInfoTests` for required TLV parsing, alpha version qualifiers, invalid qualifier length, missing qualifier fields, and invalid UTF-8 part-number fallback.
- Updated Management/Core docs to record that read-only metadata types live in Core while Management owns configuration/reset/session behavior.

## Review Evidence

- DevTeam reviewer route command: `bun ~/.claude/PAI/TOOLS/AgentHarnessRouter.ts --surface devteam --role reviewer --primary-model openai/gpt-5.5 --cwd /home/dyallo/Code/y/Yubico.NET.SDK --dry-run --json`.
- DevTeam reviewer route result: `google-vertex-anthropic/claude-opus-4-8@default` selected for OpenAI primary.
- DevTeam review output: `/tmp/opencode/devteam-review-phase34.md`.
- DevTeam verdict: `PASS`.
- DevTeam low notes:
  - `CapabilityMapper.FromFips` remains internal and test-covered but has no non-test caller; this was known in the Phase 34 ISA and preserved intentionally.
  - Required-tag parsing still throws generic dictionary/index exceptions for absent or short required TLVs; unchanged from the original Management implementation.
  - Additional Core parser tests were added for missing version-qualifier fields and invalid part-number UTF-8 fallback.

## Verification Evidence

- Full build command: `dotnet toolchain.cs build`.
- Full build result: passed with 0 warnings and 0 errors.
- Core metadata test command: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~CoreYubiKey"`.
- Core metadata test result: passed; 55 tests.
- Management focused test command: `dotnet toolchain.cs -- test --project Management --filter "FullyQualifiedName~ManagementSessionTests"`.
- Management focused test result: passed; 5 tests.
- Changed-file format command: `dotnet format --verify-no-changes --include $(git diff --name-only --diff-filter=ACM -- '*.cs')`.
- Changed-file format result: passed.
- Docs QA command: `dotnet toolchain.cs -- docs-qa`.
- Docs QA result: passed; 54 active documentation files validated.
- Whitespace command: `git diff --check`.
- Whitespace result: passed with no output.
- Old namespace check: no `Management.(DeviceInfo|DeviceCapabilities|DeviceFlags|FormFactor|VersionQualifier)` source references remain.
- Coupling check: no Core project reference to Management.
- Whole-repo `dotnet format --verify-no-changes` is still blocked by pre-existing unrelated formatting issues outside this phase, so changed-file formatting was used for phase verification.

## What Did Not Work

- Running full build concurrently with focused tests caused a transient file-lock warning; rerunning full build alone produced a clean 0-warning result.
- Initial full-solution builds surfaced staggered namespace fallout in CLI shared, CLI commands, Management examples, and PIV examples; full build was necessary because focused Core/Management builds did not cover those example projects.
- A moved test namespace named `Yubico.YubiKit.Core.UnitTests.YubiKey` shadowed existing `YubiKey.FirmwareVersion` references in another Core test, so the metadata test namespace was renamed to `Yubico.YubiKit.Core.UnitTests.CoreYubiKey`.

## Reusable Patterns

- For package-boundary moves, first move source and tests, then rely on full solution build to find consumer namespace fallout.
- Keep promoted parser behavior byte-for-byte identical unless the phase explicitly scopes behavior hardening.
- Use changed-file `dotnet format --verify-no-changes --include ...` when the repository has unrelated whole-repo format failures.
- Avoid test namespaces that collide with imported domain namespaces.

## Deferred Candidates

- Decide whether `CapabilityMapper.FromFips` should be deleted or retained with a future production caller once downstream FIPS capability semantics are audited.
- Consider typed/descriptive required-TLV error handling for `DeviceInfo.CreateFromTlvs` in a future parser-hardening phase.
- Run the downstream capability audit recorded in the master composite-device ISA after physical-device modeling is implemented.

## Next Phase Inputs

- Phase 35 should preserve the new Core metadata ownership and avoid any Core dependency on Management.
- Phase 35 should share read-only device-info retrieval logic across connection paths without changing physical/composite `IYubiKey` semantics prematurely.
- Phase 35 should continue to distinguish read-only metadata from Management-owned mutating configuration/reset behavior.
- Phase 35 should run full solution build, not only Core/Management focused builds, because examples and CLI projects catch package-boundary fallout.

## Compact Summary

- Goal: promote read-only device metadata to Core.
- Branch: `yubikit-composite-device-new`.
- Scope: source move, mandatory consumers, docs, tests.
- Status: implementation verified; ready for staging/commit when requested.
