# Testing Platform Findings

This is the canonical findings document for the xUnit v2, xUnit v3, VSTest, Microsoft Testing Platform, and MSTest confusion in this repository.

## Summary

The repository should move toward one test framework and one test platform:

- Framework: xUnit v3
- Platform: Microsoft Testing Platform (MTP)
- Command path: `dotnet toolchain.cs test` until the migration is complete

Do not migrate the repository to MSTest just to solve the runner confusion. MSTest is a valid MTP-native framework, but switching frameworks would be a larger rewrite than completing the existing xUnit v3 migration.

## Current State

The repository currently mixes two xUnit generations and two runner platforms:

| Project Type | Framework | Platform | Typical Runner |
|--------------|-----------|----------|----------------|
| Unit tests | xUnit v3 | Microsoft Testing Platform | `dotnet run --project ...` through `toolchain.cs` |
| Integration tests | xUnit v2 | VSTest | `dotnet test ...` through `toolchain.cs` |

This split is why agents and developers confuse xUnit v2, xUnit v3, VSTest, MTP, and MSTest.

## Root Cause

The main blocker is not normal test code. The blocker is `src/Tests.Shared`, which still uses xUnit v2 extensibility APIs.

Known v2-specific dependencies and patterns include:

- `xunit.core`
- `xunit.abstractions`
- `Xunit.SkippableFact`
- `Xunit.Abstractions.IXunitSerializable`
- v2 trait discoverer APIs
- `WithYubiKeyAttribute` as a custom `DataAttribute`
- lazy hardware binding serialized through xUnit v2 theory data

The integration test projects stay on xUnit v2 because they depend on that shared hardware-test infrastructure.

## Upstream Findings

The previous assumption that xUnit v3 was not far enough along is now outdated.

Current upstream state:

- xUnit v3 has stable releases and active documentation.
- xUnit v3 has native Microsoft Testing Platform support.
- xUnit v3 exposes explicit MTP package variants, including `xunit.v3.mtp-v2`.
- .NET 10 supports native MTP mode for `dotnet test` via `global.json`.
- Microsoft documentation recommends not mixing VSTest and MTP projects in the same solution or run configuration.

Important distinction:

- xUnit is a test framework.
- MSTest is a different test framework.
- VSTest is the legacy test platform/runner.
- Microsoft Testing Platform is the newer test platform/runner.

The desired simplification is not "xUnit vs MSTest". It is "stop mixing VSTest and MTP, and stop mixing xUnit v2 and xUnit v3".

## Recommended Direction

Standardize the repository on xUnit v3 plus Microsoft Testing Platform.

Migration sequence:

1. Upgrade xUnit v3 packages to a current stable version.
2. Prefer `xunit.v3.mtp-v2` for MTP v2 support.
3. Port `src/Tests.Shared` from xUnit v2 extensibility to xUnit v3 extensibility.
4. Convert integration test projects to xUnit v3 executable test projects.
5. Add .NET 10 MTP runner configuration through `global.json` when the whole solution is ready.
6. Simplify `toolchain.cs` after all test projects use one framework/platform path.
7. Update docs so agents only learn one model.

## Fallback Option

`YTest.MTP.XUnit2` can run xUnit v2 projects on Microsoft Testing Platform.

Use it only as a temporary bridge if the `Tests.Shared` migration is blocked. It is third-party support for xUnit v2 on MTP, not the preferred end state.

Bridge outcome:

- Removes the VSTest/MTP runner split.
- Keeps the xUnit v2/xUnit v3 framework split.
- Still requires later migration to xUnit v3 for full simplification.

## Not Recommended

Do not move this repository to MSTest as the primary fix.

Reasons:

- The repository already has substantial xUnit test code.
- The custom hardware-test infrastructure is xUnit-shaped.
- MSTest would require broad test source migration.
- MSTest would not by itself solve the existing `Tests.Shared` design questions.

Do not introduce TUnit as the primary fix.

Reasons:

- TUnit is MTP-native and promising, but it would be another framework migration.
- The repository does not need a new framework; it needs a completed xUnit v3/MTP standardization.

## Agent Rules

Until the migration is complete:

- Always run tests through `dotnet toolchain.cs test`.
- Do not run `dotnet test` directly.
- Do not assume a test project is MSTest because it uses Microsoft Testing Platform.
- Treat MTP as the runner platform, not the test framework.
- Treat xUnit v2 vs xUnit v3 as a framework-generation split, not a platform split.
- If changing integration test infrastructure, inspect `src/Tests.Shared` first.

After the migration is complete, this document should be updated to remove transitional guidance and point to the single supported test command path.

## Desired End State

One simple mental model:

- All tests use xUnit v3.
- All tests run on Microsoft Testing Platform.
- `Tests.Shared` uses xUnit v3 extensibility only.
- `toolchain.cs` no longer needs v2/v3 runner detection.
- Documentation no longer teaches both VSTest and MTP paths except as historical context.
