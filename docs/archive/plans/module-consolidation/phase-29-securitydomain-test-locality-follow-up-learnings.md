# Phase 29 Learnings: SecurityDomain Test And Locality Follow-Up

Use this note as the handoff record for Phase 29 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`.
- Scope: add recorder-backed SecurityDomain STORE DATA APDU proof without production source changes.
- Phase ISA: `docs/plans/module-consolidation/phase-29-securitydomain-test-locality-follow-up-ISA.md`.
- Source changed: none.
- Tests changed: SecurityDomain unit tests now assert byte-exact STORE DATA payloads for `StoreAllowListAsync`, `ClearAllowListAsync`, and `StoreCaIssuerAsync`.
- Test config changed: SecurityDomain integration tests now reference `Xunit.SkippableFact` directly so runtime skips from `Tests.Shared` resolve after that dependency remains private there.

## Source Audit

- `SecurityDomainSession` remains the correct owner for visible GlobalPlatform APDU flow; no partial-class split or operation command objects were justified.
- `StoreAllowListAsync` and `ClearAllowListAsync` build nested `A6/83` key references plus `70/93` serial-list TLVs that are valuable to pin at the wire level.
- `StoreCaIssuerAsync` builds an `A6` CA issuer payload containing KLCC (`80`), SKI (`42`), and key reference (`83`) TLVs; using an SCP11b key reference covers the non-default KLCC branch.
- The generic `StoreDataAsync` command path already had APDU coverage, so Phase 29 targeted the higher-level payload helpers instead of changing production code.

## What Changed

- Added `StoreAllowListAsync_TransmitsStoreDataWithSerialList` with an exact APDU assertion for two serials.
- Added `ClearAllowListAsync_TransmitsStoreDataWithEmptySerialList` with an exact `70 00` empty allow-list assertion.
- Added `StoreCaIssuerAsync_TransmitsStoreDataWithKlccSkiAndKeyReference` with an exact CA issuer payload assertion for `KeyReference(0x13, 0x02)`.
- Added direct `Xunit.SkippableFact` reference to `Yubico.YubiKit.SecurityDomain.IntegrationTests.csproj` to match the established integration-test dependency pattern.
- Ran scoped formatter cleanup on touched SecurityDomain unit-test files.

## Why This Shape

- Recorder-backed APDU tests improve confidence without altering the low-level security module's public facade or production locality.
- The tests assert full command bytes, including `STORE DATA` header, Lc, TLV ordering, nested lengths, and payload values.
- Direct `Xunit.SkippableFact` references are required in integration projects because `Tests.Shared` marks that dependency `PrivateAssets="all"`.
- The no-final-newline style surfaced by formatter is repository-local behavior for these touched files; Phase 29 follows the formatter rather than imposing broader style cleanup.

## Verification Evidence

- Format command: `dotnet format src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/Yubico.YubiKit.SecurityDomain.UnitTests.csproj --verify-no-changes`.
- Format result: passed after scoped formatting of touched SecurityDomain unit-test files.
- Diff whitespace command: `git diff --check`.
- Diff whitespace result: passed; only a CRLF conversion warning was reported for the integration test project file.
- Focused SecurityDomain session command: `dotnet toolchain.cs -- test --project SecurityDomain --filter "FullyQualifiedName~SecurityDomainSessionTests"`.
- Focused SecurityDomain session result: passed 25/25.
- Full SecurityDomain unit command: `dotnet toolchain.cs -- test --project SecurityDomain`.
- Full SecurityDomain unit result: passed 31/31.
- SecurityDomain build command: `dotnet toolchain.cs -- build --project SecurityDomain`.
- SecurityDomain build result: source, integration tests, and unit tests built with 0 warnings and 0 errors.
- Docs QA command: `dotnet toolchain.cs -- docs-qa`.
- Docs QA result: passed, 54 active documentation files validated.
- Read-only SecurityDomain integration smoke command: `dotnet toolchain.cs -- test --integration --project SecurityDomain --filter "FullyQualifiedName~SecurityDomainSession_Scp03Tests.GetData_Unauthenticated_Succeeds" --smoke`.
- Read-only SecurityDomain integration smoke result: passed 1/1 on authorized serial `103`, firmware `5.8.0`.
- DevTeam review command route: `openai/gpt-5.5` primary routed reviewer to `google-vertex-anthropic/claude-opus-4-8@default`.
- DevTeam review result: `PASS`; low notes only and no required changes.

## What Did Not Work

- The initial scoped format verification failed on mixed line endings in `SecurityDomainSessionTests.cs`; running the scoped formatter resolved it.
- Formatting the unit-test project also normalized whitespace in `DependencyInjectionTests.cs`. That change is whitespace-only and required for the scoped format check to pass.
- The formatter expects no final newline in the touched SecurityDomain unit-test files, so re-adding final newlines caused `FINALNEWLINE` diagnostics.

## Reusable Patterns

- For SecurityDomain byte-level tests, target helper methods that assemble meaningful TLV payloads and assert the complete post-select APDU at `connection.TransmittedCommands[1]`.
- Use SCP11b (`Kid=0x13`) when a CA issuer test needs to prove the KLCC non-default branch.
- Treat direct `Xunit.SkippableFact` package references as required integration-project setup while `Tests.Shared` keeps that dependency private.
- Prefer test evidence over production refactoring when a flat low-level session facade remains readable and policy-compliant.

## Deferred Candidates

- Broader SecurityDomain integration suite remains human/hardware coordinated because many tests reset, import, rotate, delete, or generate persistent SecurityDomain state.
- Additional SecurityDomain wire tests can target delete/generate/key import payload edge cases only if the next phase finds a meaningful uncovered byte path.
- Repository-wide CRLF/final-newline cleanup remains a separate hygiene task, not a module-consolidation phase requirement.

## Next Phase Inputs

- Required reading before next phase: this learning note.
- Phase 30 should move to the next highest-value module locality or wire-coverage gap rather than expanding SecurityDomain further.
- Preserve the Phase 29 rule: do not split SecurityDomain into command/executor abstractions unless a repeated cross-operation pattern proves it is needed.
- Check remaining integration projects for direct skip/runtime dependencies before assuming `Tests.Shared` private packages flow transitively.

## Compact Summary

- Goal: strengthen SecurityDomain STORE DATA payload evidence.
- Main fix: three recorder-backed byte APDU tests.
- Production code: unchanged.
- Test config: direct `Xunit.SkippableFact` reference added.
- Verification: focused/full units, build, read-only smoke, docs QA, scoped format, diff check, and DevTeam review passed.
