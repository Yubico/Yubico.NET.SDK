---
name: build
description: Use when compiling, testing, or packaging .NET code - runs build.cs targets (NEVER use dotnet test directly)
---

# .NET Build System Skill

## Purpose

Use this skill when building, testing, or packaging the Yubico.NET.SDK project. This project uses a custom C# build script (`build.cs`) with Bullseye task runner instead of standard MSBuild commands.

## Use when

**ALWAYS use this skill when:**
- Building the solution or specific projects
- Running unit tests or integration tests
- Collecting code coverage
- Creating or publishing NuGet packages
- Cleaning build artifacts

**DO NOT use** `dotnet build`, `dotnet test`, or `dotnet pack` directly - use the build script instead.

## Core Build Command

```bash
dotnet build.cs [target] [options]
```

## Available Targets

| Target | Description | Dependencies |
|--------|-------------|--------------|
| `clean` | Remove artifacts directory | None |
| `restore` | Restore NuGet dependencies | clean |
| `build` | Build the solution or specific project | restore |
| `test` | Run unit tests with summary output | build |
| `coverage` | Run tests with code coverage | build |
| `pack` | Create NuGet packages | build |
| `setup-feed` | Configure local NuGet feed | None |
| `publish` | Publish packages to local feed | pack, setup-feed |
| `default` | Run tests and publish | test, publish |

## Key Options

- `--project <name>` - Build/test specific project (partial match, e.g., `Piv`)
- `--filter <expression>` - Test filter (e.g., `"FullyQualifiedName~MyTest"`)
- `--clean` - Run `dotnet clean` before build
- `--package-version <version>` - Override package version
- `--dry-run` - Show what would be published without publishing
- `--include-docs` - Include XML documentation in packages

## Common Workflows

### Build Everything
```bash
dotnet build.cs build
```

### Build Specific Project
```bash
# Partial match on project name
dotnet build.cs build --project Piv
dotnet build.cs build --project Fido2
```

### Run All Tests
```bash
dotnet build.cs test
```

### Run Tests for Specific Project
```bash
dotnet build.cs test --project Piv
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

### Create Packages
```bash
# Default version from project files
dotnet build.cs pack

# Custom version
dotnet build.cs publish --package-version 1.0.0-preview.1
```

### Clean Build from Scratch
```bash
dotnet build.cs build --clean
```

## Test Filter Syntax

The `--filter` option supports various patterns:

- `FullyQualifiedName~MyClass` - Tests containing 'MyClass' in full name
- `Name=MyTestMethod` - Exact test method name
- `ClassName~Integration` - Classes containing 'Integration'
- `Name!=SkipMe` - Exclude tests named 'SkipMe'
- `Category=Unit` - Tests with `[Trait("Category", "Unit")]`

## Project Discovery

The build script automatically discovers:
- **Packable projects**: `Yubico.YubiKit.*/src/*.csproj`
- **Test projects**: `Yubico.YubiKit.*.UnitTests/*.csproj` and `Yubico.YubiKit.*.IntegrationTests/*.csproj`

To see discovered projects:
```bash
dotnet build.cs -- --help
```

## Output Locations

- **Packages**: `artifacts/packages/*.nupkg`
- **Coverage reports**: `artifacts/coverage/**/coverage.cobertura.xml`
- **Local NuGet feed**: `artifacts/nuget-feed/`

## Important Notes

### The `--` Separator

Use `--` when arguments might conflict with dotnet's own options:

```bash
# REQUIRED for --help
dotnet build.cs -- --help

# Optional but safer when using multiple options
dotnet build.cs -- build --project Piv --clean
```

**Rule of thumb**: If commands don't work as expected, add `--` before arguments.

### Build Execution Time

For long-running builds/tests, use appropriate `initial_wait`:

```bash
# Building (may take 30-60 seconds)
dotnet build.cs build

# Testing (may take 60-120 seconds)
dotnet build.cs test

# Coverage (may take 120+ seconds)
dotnet build.cs coverage
```

When using the bash tool with `mode="sync"`, set `initial_wait` to 60-120 seconds for build/test operations.

### Test Output

The build script provides:
- Colored output (✓ green for pass, ✗ red for fail)
- Per-project test results
- Summary table with totals
- Better error messages than default test runner

### Project Name Matching

The `--project` option uses partial matching:
- `--project Piv` matches `Yubico.YubiKit.Piv`
- `--project Fido` matches `Yubico.YubiKit.Fido2`
- Case-insensitive matching

If no match is found, the script lists available projects.

## Integration with Development Workflow

1. **Initial build**: `dotnet build.cs build`
2. **Make changes**: Edit code files
3. **Test changes**: `dotnet build.cs test --project <affected-project>`
4. **Run specific tests**: `dotnet build.cs test --filter "ClassName~MyTests"`
5. **Verify coverage**: `dotnet build.cs coverage`
6. **Clean rebuild**: `dotnet build.cs build --clean`

## Troubleshooting

### Build Failures
```bash
# Clean and rebuild
dotnet build.cs clean
dotnet build.cs build --clean
```

### Test Failures
```bash
# Run with verbose output to see details
dotnet build.cs test --project <project>

# Run specific failing test
dotnet build.cs test --filter "Name=FailingTestMethod"
```

### Package Issues
```bash
# Dry run to verify
dotnet build.cs publish --dry-run

# Clean and repack
dotnet build.cs clean
dotnet build.cs pack
```

## Best Practices

1. **Use `--project` for focused work**: Faster iteration when working on specific components
2. **Use `--filter` for debugging**: Zero in on failing tests quickly
3. **Run `coverage` before PRs**: Ensure new code is tested
4. **Use `--dry-run` first**: Verify package contents before publishing
5. **Clean periodically**: Remove stale artifacts with `dotnet build.cs clean`

## Skill Activation

This skill should be automatically invoked when:
- User mentions "build", "compile", or "building"
- User mentions "test", "testing", or "unit test"
- User mentions "package", "NuGet", or "publishing"
- User mentions "coverage" or "code coverage"
- Agent needs to verify changes don't break builds/tests

Always prefer `build.cs` over direct `dotnet` commands for this repository.
