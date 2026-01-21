---
name: test-project
description: REQUIRED for running tests - NEVER use dotnet test directly
---

# Running .NET Tests

## Overview

Run tests for Yubico.NET.SDK using the custom `build.cs` script. This handles xUnit v2/v3 differences automatically.

**Core principle:** Always use `dotnet build.cs test` - never `dotnet test` directly.

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
dotnet build.cs test [options]
```

## Available Targets

| Target | Description | Dependencies |
|--------|-------------|--------------|
| `test` | Run unit tests with summary output | build |
| `coverage` | Run tests with code coverage | build |
| `default` | Run tests and publish | test, publish |

## Key Options

| Option | Description |
|--------|-------------|
| `--project <name>` | Test specific project (partial match, e.g., `Piv`) |
| `--filter <expression>` | Test filter (e.g., `"FullyQualifiedName~MyTest"`) |

## Common Workflows

### Run All Tests

```bash
dotnet build.cs test
```

### Run Tests for Specific Project

```bash
dotnet build.cs test --project Piv
dotnet build.cs test --project Fido2
dotnet build.cs test --project SecurityDomain
```

### Run Specific Test(s) with Filter

```bash
# Single test method
dotnet build.cs test --filter "FullyQualifiedName~MyTestMethod"

# All tests in a class
dotnet build.cs test --filter "ClassName~SignatureTests"

# Combine project and filter
dotnet build.cs test --project Piv --filter "Method~Sign"
```

### Run Tests with Coverage

```bash
dotnet build.cs coverage
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

## Project Discovery

The build script automatically discovers:
- **Test projects**: `Yubico.YubiKit.*.UnitTests/*.csproj`
- **Integration tests**: `Yubico.YubiKit.*.IntegrationTests/*.csproj`

## Output Locations

| Output | Location |
|--------|----------|
| Coverage reports | `artifacts/coverage/**/coverage.cobertura.xml` |

## Test Output

The build script provides:
- Colored output (✓ green for pass, ✗ red for fail)
- Per-project test results
- Summary table with totals
- Better error messages than default test runner

## Test Execution Time

For AI tools using bash with `mode="sync"`:
- Testing: set `initial_wait` to 60-120 seconds
- Coverage: set `initial_wait` to 120+ seconds

## Project Name Matching

The `--project` option uses partial, case-insensitive matching:
- `--project Piv` matches `Yubico.YubiKit.Piv.UnitTests`
- `--project Fido` matches `Yubico.YubiKit.Fido2.UnitTests`

If no match is found, the script lists available projects.

## Troubleshooting

### Test Failures

```bash
# Run with project filter to see details
dotnet build.cs test --project <project>

# Run specific failing test
dotnet build.cs test --filter "Name=FailingTestMethod"
```

### Build Required First

If tests fail to run, ensure the project builds:
```bash
dotnet build.cs build
dotnet build.cs test
```

## Verification

Tests completed successfully when:
- [ ] Exit code is 0
- [ ] All tests pass (green ✓)
- [ ] No unexpected warnings or errors

## Related Skills

- `build-project` - For building without testing
- `tdd` - For test-driven development workflow
