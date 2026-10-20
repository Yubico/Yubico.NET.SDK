---
name: test-project
description: REQUIRED for running tests - NEVER use dotnet test directly
---

# Running .NET Tests

## Overview

Run tests for Yubico.NET.SDK using the custom `toolchain.cs` script. This handles xUnit v2/v3 differences automatically.

**Core principle:** Always use `dotnet toolchain.cs test` - never `dotnet test` directly.

## Use when

**MANDATORY - use this skill when:**
- Running unit tests or integration tests
- Running specific test(s) with filters
- Collecting code coverage
- Verifying changes don't break existing tests

**NEVER use `dotnet test` directly.** The codebase uses mixed xUnit v2/v3 which requires special handling.

**Don't use when:**
- Only building without testing (use `build-project` skill instead)
- Creating packages (use `build-project` skill instead)

## Core Command

```bash
dotnet toolchain.cs test [options]
```

## Available Targets

| Target | Description | Dependencies |
|--------|-------------|--------------|
| `test` | Run unit tests with summary output | build |
| `coverage` | Run tests with code coverage | build |
| `default` | Run tests and publish (only when named; a bare `dotnet toolchain.cs` just prints usage) | test, publish |

## Key Options

| Option | Description |
|--------|-------------|
| `--project <name>` | Test specific project (partial match, e.g., `Piv`) |
| `--filter <expression>` | Test filter (e.g., `"FullyQualifiedName~MyTest"`) |

## Common Workflows

### Run All Tests

```bash
dotnet toolchain.cs test
```

### Run Tests for Specific Project

```bash
dotnet toolchain.cs -- test --project Piv
dotnet toolchain.cs -- test --project Fido2
dotnet toolchain.cs -- test --project SecurityDomain
```

### Run Specific Test(s) with Filter

```bash
# Single test method
dotnet toolchain.cs -- test --filter "FullyQualifiedName~MyTestMethod"

# All tests in a class
dotnet toolchain.cs -- test --filter "ClassName~SignatureTests"

# Combine project and filter
dotnet toolchain.cs -- test --project Piv --filter "Method~Sign"
```

### Run Tests with Coverage

```bash
dotnet toolchain.cs coverage
```

## Test Filter Syntax

The `--filter` option supports various patterns:

| Pattern | Description |
|---------|-------------|
| `FullyQualifiedName~MyClass` | Tests containing 'MyClass' in full name |
| `Name=MyTestMethod` | Exact test method name |
| `ClassName~Integration` | Classes containing 'Integration' |
| `Name!=SkipMe` | Exclude tests named 'SkipMe' |
| `Category=Unit` | Tests with `[Trait("Category", "Unit")]` |
| `Category!=RequiresUserPresence` | Exclude tests needing user interaction |
| `Category!=RequiresHardware` | Exclude tests needing physical YubiKey |
| `Category!=Slow` | Exclude slow tests (>5 seconds) |

### Reading the result — two traps

**Judge a run by the RESULT block at the end of the output, and by the exit code.**
`RESULT: PASSED` (exit code 0) or `RESULT: FAILED`, then `tests: N executed, N failed, N skipped`.
A project that executed nothing (filter matched nothing, or every test skipped) is `○ NO-TESTS`
and fails the target, so a green result always means tests ran:

```bash
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~MyThing" 2>&1 | tail -15
```

**`Method~` is not a VSTest property.** Unit projects run xUnit v3 / Microsoft Testing Platform;
integration projects run xUnit v2 under VSTest, which only knows `FullyQualifiedName`,
`DisplayName` and traits. The toolchain normalises `Method~`/`Name~` to `FullyQualifiedName~` for
those projects so both runners accept one syntax, and a filter matching zero tests now fails the
target rather than reporting green. Prefer `FullyQualifiedName~` when you want to be exact.

### Test Category Traits

Tests are categorized using `TestCategories` constants from `Yubico.YubiKit.Tests.Shared.Infrastructure`:

| Category | Constant | Description |
|----------|----------|-------------|
| `RequiresHardware` | `TestCategories.RequiresHardware` | Needs physical YubiKey connected |
| `RequiresUserPresence` | `TestCategories.RequiresUserPresence` | Needs user to insert/remove/touch device |
| `Slow` | `TestCategories.Slow` | Takes >5 seconds (delays, performance) |
| `Integration` | `TestCategories.Integration` | Exercises multiple components |

**AI Agents:** Always exclude `RequiresUserPresence` tests (you cannot insert/remove devices):

```bash
# Run tests without user presence requirement
dotnet toolchain.cs -- test --filter "Category!=RequiresUserPresence"

# Run only fast unit tests (no hardware, no user presence, not slow)
dotnet toolchain.cs -- test --filter "Category!=RequiresHardware&Category!=RequiresUserPresence&Category!=Slow"
```

**When writing new tests**, apply traits using constants:

```csharp
using Yubico.YubiKit.Tests.Shared.Infrastructure;

[Fact]
[Trait(TestCategories.Category, TestCategories.RequiresHardware)]
public async Task MyHardwareTest() { }

[Fact]
[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
[Trait(TestCategories.Category, TestCategories.Slow)]
public async Task MyDeviceInsertionTest() { }
```

### xUnit v3 Direct Runner (Advanced)

When running xUnit v3 test projects directly (not through `toolchain.cs`), filter syntax differs:

| xUnit v2 (`dotnet test`) | xUnit v3 Direct | Notes |
|--------------------------|-----------------|-------|
| `--filter "FullyQualifiedName~Class"` | `--filter-class ClassName` | Class filter |
| `--filter "Name=Method"` | `--filter-method MethodName` | Method filter |
| No matches → 0 tests run | No matches → **test failure** | v3 fails on empty results |

**Avoid this complexity** - use `dotnet toolchain.cs test` which handles xUnit v2/v3 differences automatically.

## Project Discovery

The build script automatically discovers:
- **Test projects**: `Yubico.YubiKit.*.UnitTests/*.csproj`
- **Integration tests**: `Yubico.YubiKit.*.IntegrationTests/*.csproj`

## Output Locations

| Output | Location |
|--------|----------|
| Coverage reports | `artifacts/coverage/**/coverage.cobertura.xml` |
| Run logs (5 newest) | `artifacts/toolchain/runs/<time>-<targets>/`, newest at `artifacts/toolchain/latest/` |

## Test Output

- Test runner output is **not** printed; each project's full output is saved to a step log
  (`output: <path>` under its `▶ RUN` line). Pass `--verbose` to stream it.
- Each project ends with `✓ PASS (N executed, N skipped)`, `✗ FAIL (reason)` or `○ NO-TESTS (reason)`.
- The RESULT block lists each failing test (`- Namespace.Class.Method: message`) and a `Next:` command
  that re-runs only those tests. `result.json` beside `run.log` holds the same data as JSON.
- Run cut short (timeout) or new session? `dotnet toolchain.cs -- last` reprints the newest RESULT block;
  the first output line of every run names its log. Earlier runs: `ls artifacts/toolchain/runs`.

## Test Execution Time

For AI tools using bash with `mode="sync"`:
- Testing: set `initial_wait` to 60-120 seconds
- Coverage: set `initial_wait` to 120+ seconds

## Project Name Matching

The `--project` option uses partial, case-insensitive matching:
- `--project Piv` matches `Yubico.YubiKit.Piv.UnitTests`
- `--project Fido` matches `Yubico.YubiKit.Fido2.UnitTests`

If no match is found, the target fails and lists the available project names.

## Troubleshooting

### Test Failures

```bash
# Run the Next: command from the RESULT block; it re-runs only the failing tests
dotnet toolchain.cs -- test --project <project> --filter "FullyQualifiedName~Namespace.Class.Method"

# Need the full runner output (stack traces)? Read the step log named in the RESULT block, or:
dotnet toolchain.cs -- test --project <project> --verbose
```

### Build Required First

If tests fail to run, ensure the project builds:
```bash
dotnet toolchain.cs build
dotnet toolchain.cs test
```

## Verification

Tests completed successfully when:
- [ ] Exit code is 0 and the output ends with `RESULT: PASSED`
- [ ] `tests: N executed` is the count you expected (not 0)
- [ ] `Warnings: 0` in the RESULT block and no unexpected `! WARN` lines

## Related Skills

- `build-project` - For building without testing
- `tdd` - For test-driven development workflow
